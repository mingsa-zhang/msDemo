namespace DbManager.Common;

/// <summary>
/// 数据库错误翻译：把各驱动的原始异常信息映射为友好中文提示。
/// 基于关键字匹配，未命中时回退原始信息。
/// </summary>
public static class DbErrorTranslator
{
    /// <summary>
    /// 翻译异常为友好提示。
    /// </summary>
    public static string Translate(Exception ex) => Translate(ex.Message);

    /// <summary>
    /// 翻译原始错误信息为友好提示。
    /// </summary>
    public static string Translate(string? rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return "未知错误";
        }

        var m = rawMessage.ToLowerInvariant();

        // 认证/权限
        if (m.Contains("access denied") || m.Contains("1045") || m.Contains("password authentication failed")
            || m.Contains("login failed") || m.Contains("ora-01017"))
        {
            return $"用户名或密码错误，或该账号无权限。\n原始信息：{rawMessage}";
        }

        // 连接/网络
        if (m.Contains("timeout") || m.Contains("timed out") || m.Contains("etimedout"))
        {
            return $"连接或查询超时，请检查网络、主机端口或增大超时时间。\n原始信息：{rawMessage}";
        }
        if (m.Contains("unable to connect") || m.Contains("could not connect") || m.Contains("connection refused")
            || m.Contains("no such host") || m.Contains("actively refused") || m.Contains("network-related"))
        {
            return $"无法连接到数据库服务器，请检查主机、端口及服务是否启动。\n原始信息：{rawMessage}";
        }

        // 对象不存在
        if (m.Contains("unknown database") || m.Contains("does not exist") || m.Contains("cannot open database"))
        {
            return $"目标数据库或对象不存在。\n原始信息：{rawMessage}";
        }
        if (m.Contains("doesn't exist") || m.Contains("no such table") || m.Contains("invalid object name")
            || m.Contains("ora-00942"))
        {
            return $"表或对象不存在。\n原始信息：{rawMessage}";
        }

        // SQL 语法
        if (m.Contains("syntax error") || m.Contains("sql syntax") || m.Contains("ora-00900")
            || m.Contains("incorrect syntax"))
        {
            return $"SQL 语法错误，请检查语句。\n原始信息：{rawMessage}";
        }

        // 约束冲突
        if (m.Contains("duplicate") || m.Contains("unique") || m.Contains("violation") || m.Contains("constraint"))
        {
            return $"违反唯一性/约束限制。\n原始信息：{rawMessage}";
        }

        return rawMessage;
    }
}
