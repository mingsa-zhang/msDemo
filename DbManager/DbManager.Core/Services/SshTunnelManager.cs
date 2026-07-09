using DbManager.Core.Models;
using Renci.SshNet;

namespace DbManager.Core.Services;

/// <summary>
/// SSH 隧道管理：为启用 SSH 的连接建立"本地端口 → 远端库地址"的转发，并按签名缓存复用。
/// 同步 API（SSH.NET 本身同步），供连接串构建时按需拉起隧道，返回本地转发端点。
/// 传入的 SshPassword/SshPassphrase 须为明文（调用方负责解密）。
/// </summary>
public static class SshTunnelManager
{
    private sealed class TunnelEntry
    {
        public SshClient Client { get; init; } = null!;
        public ForwardedPortLocal Port { get; init; } = null!;
        public int LocalPort { get; init; }
    }

    private static readonly object _lock = new();
    private static readonly Dictionary<string, TunnelEntry> _tunnels = new();

    /// <summary>
    /// 确保到 (targetHost:targetPort) 的隧道已建立，返回本地转发端点 (127.0.0.1, 本地端口)。
    /// </summary>
    public static (string LocalHost, int LocalPort) EnsureTunnel(DbConnectionModel conn, string targetHost, int targetPort)
    {
        var key = BuildKey(conn, targetHost, targetPort);

        lock (_lock)
        {
            if (_tunnels.TryGetValue(key, out var existing))
            {
                if (existing.Client.IsConnected && existing.Port.IsStarted)
                {
                    return ("127.0.0.1", existing.LocalPort);
                }
                // 失效则清理后重建
                CloseEntry(existing);
                _tunnels.Remove(key);
            }

            var client = new SshClient(BuildConnectionInfo(conn));
            client.Connect();

            // 本地端口传 0 交由系统分配空闲端口
            var forwarded = new ForwardedPortLocal("127.0.0.1", 0, targetHost, (uint)targetPort);
            client.AddForwardedPort(forwarded);
            forwarded.Start();

            var entry = new TunnelEntry
            {
                Client = client,
                Port = forwarded,
                LocalPort = (int)forwarded.BoundPort
            };
            _tunnels[key] = entry;
            return ("127.0.0.1", entry.LocalPort);
        }
    }

    /// <summary>
    /// 关闭并移除指定连接的所有隧道（连接删除/编辑后可调用）。
    /// </summary>
    public static void CloseForConnection(int connectionId)
    {
        lock (_lock)
        {
            var prefix = connectionId + "|";
            foreach (var key in _tunnels.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                CloseEntry(_tunnels[key]);
                _tunnels.Remove(key);
            }
        }
    }

    /// <summary>
    /// 关闭全部隧道（应用退出时调用）。
    /// </summary>
    public static void CloseAll()
    {
        lock (_lock)
        {
            foreach (var entry in _tunnels.Values)
            {
                CloseEntry(entry);
            }
            _tunnels.Clear();
        }
    }

    private static ConnectionInfo BuildConnectionInfo(DbConnectionModel conn)
    {
        var sshPort = conn.SshPort > 0 ? conn.SshPort : 22;

        AuthenticationMethod auth;
        if (conn.SshUseKeyFile)
        {
            var keyFile = string.IsNullOrEmpty(conn.SshPassphrase)
                ? new PrivateKeyFile(conn.SshKeyPath)
                : new PrivateKeyFile(conn.SshKeyPath, conn.SshPassphrase);
            auth = new PrivateKeyAuthenticationMethod(conn.SshUser, keyFile);
        }
        else
        {
            auth = new PasswordAuthenticationMethod(conn.SshUser, conn.SshPassword ?? string.Empty);
        }

        return new ConnectionInfo(conn.SshHost, sshPort, conn.SshUser, auth);
    }

    private static string BuildKey(DbConnectionModel conn, string targetHost, int targetPort)
        => string.Join('|',
            conn.Id,
            conn.SshHost, conn.SshPort, conn.SshUser,
            conn.SshUseKeyFile ? "key:" + conn.SshKeyPath : "pwd",
            targetHost, targetPort);

    private static void CloseEntry(TunnelEntry entry)
    {
        try
        {
            if (entry.Port.IsStarted)
            {
                entry.Port.Stop();
            }
            entry.Port.Dispose();
        }
        catch
        {
            // 清理阶段异常忽略
        }
        try
        {
            if (entry.Client.IsConnected)
            {
                entry.Client.Disconnect();
            }
            entry.Client.Dispose();
        }
        catch
        {
            // 清理阶段异常忽略
        }
    }
}
