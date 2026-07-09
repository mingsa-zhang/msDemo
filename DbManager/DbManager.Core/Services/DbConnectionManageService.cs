using DbManager.Core.Adapters;
using DbManager.Core.Interfaces;
using DbManager.Core.Models;
using DbManager.Common;

namespace DbManager.Core.Services;

public class DbConnectionManageService
{
    private readonly IConnRepository _repository;

    public DbConnectionManageService(IConnRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DbConnectionModel>> GetAllConnectionsAsync()
    {
        return await _repository.GetAllConnectionsAsync();
    }

    public async Task<DbConnectionModel?> GetConnectionByIdAsync(int id)
    {
        return await _repository.GetConnectionByIdAsync(id);
    }

    public async Task<int> AddConnectionAsync(DbConnectionModel connection)
    {
        EncryptSecrets(connection);
        return await _repository.AddConnectionAsync(connection);
    }

    public async Task<int> UpdateConnectionAsync(DbConnectionModel connection)
    {
        EncryptSecrets(connection);
        connection.UpdatedTime = DateTime.Now;
        return await _repository.UpdateConnectionAsync(connection);
    }

    /// <summary>
    /// 落库前加密各类明文凭据（已加密则跳过）。
    /// </summary>
    private static void EncryptSecrets(DbConnectionModel connection)
    {
        if (!string.IsNullOrEmpty(connection.Password) && !PasswordEncryptHelper.IsEncrypted(connection.Password))
            connection.Password = PasswordEncryptHelper.Encrypt(connection.Password);
        if (!string.IsNullOrEmpty(connection.RedisPassword) && !PasswordEncryptHelper.IsEncrypted(connection.RedisPassword))
            connection.RedisPassword = PasswordEncryptHelper.Encrypt(connection.RedisPassword);
        if (!string.IsNullOrEmpty(connection.SshPassword) && !PasswordEncryptHelper.IsEncrypted(connection.SshPassword))
            connection.SshPassword = PasswordEncryptHelper.Encrypt(connection.SshPassword);
        if (!string.IsNullOrEmpty(connection.SshPassphrase) && !PasswordEncryptHelper.IsEncrypted(connection.SshPassphrase))
            connection.SshPassphrase = PasswordEncryptHelper.Encrypt(connection.SshPassphrase);
    }

    public async Task<int> DeleteConnectionAsync(int id)
    {
        return await _repository.DeleteConnectionAsync(id);
    }

    public async Task<int> DeleteConnectionsAsync(List<int> ids)
    {
        return await _repository.DeleteConnectionsAsync(ids);
    }

    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(DbConnectionModel connection)
    {
        var factory = new DbConnectionFactory();
        try
        {
            using var dbConn = factory.CreateConnection(connection);
            await dbConn.OpenAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<DbConnectionGroupModel>> GetAllGroupsAsync()
    {
        return await _repository.GetAllGroupsAsync();
    }

    public async Task<int> AddGroupAsync(DbConnectionGroupModel group)
    {
        return await _repository.AddGroupAsync(group);
    }

    public async Task<int> UpdateGroupAsync(DbConnectionGroupModel group)
    {
        return await _repository.UpdateGroupAsync(group);
    }

    public async Task<int> DeleteGroupAsync(int id)
    {
        return await _repository.DeleteGroupAsync(id);
    }

    public async Task<string> ExportConnectionsToJsonAsync()
    {
        var connections = await GetAllConnectionsAsync();
        var exportList = connections.Select(conn =>
        {
            var clone = conn.Clone();
            if (!string.IsNullOrEmpty(clone.Password))
                clone.Password = PasswordEncryptHelper.Decrypt(clone.Password);
            if (!string.IsNullOrEmpty(clone.RedisPassword))
                clone.RedisPassword = PasswordEncryptHelper.Decrypt(clone.RedisPassword);
            return clone;
        }).ToList();
        return Newtonsoft.Json.JsonConvert.SerializeObject(exportList, Newtonsoft.Json.Formatting.Indented);
    }

    public async Task<int> ImportConnectionsFromJsonAsync(string json)
    {
        var connections = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DbConnectionModel>>(json) ?? new();
        var count = 0;
        foreach (var conn in connections)
        {
            await AddConnectionAsync(conn);
            count++;
        }
        return count;
    }
}