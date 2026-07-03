using System.Text.RegularExpressions;

namespace DbManager.Wpf.Helpers;

/// <summary>
/// 轻量 SQL 格式化：关键字大写 + 主要子句换行 + JOIN/AND/OR 缩进。
/// 非完整解析器，面向常见 SQL 提升可读性；不处理字符串字面量内的关键字。
/// </summary>
public static class SqlFormatter
{
    // 需大写的关键字
    private static readonly string[] Keywords =
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER BY", "GROUP BY", "HAVING",
        "INSERT INTO", "UPDATE", "DELETE", "INTO", "VALUES", "SET",
        "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "CROSS JOIN", "JOIN", "ON",
        "CREATE", "TABLE", "ALTER", "DROP", "INDEX", "PRIMARY KEY", "FOREIGN KEY", "REFERENCES",
        "NOT NULL", "NULL", "DEFAULT", "LIKE", "IN", "BETWEEN", "IS", "AS", "DISTINCT",
        "CASE", "WHEN", "THEN", "ELSE", "END", "UNION ALL", "UNION", "EXCEPT", "INTERSECT",
        "LIMIT", "OFFSET", "FETCH", "BEGIN", "COMMIT", "ROLLBACK", "TRANSACTION"
    };

    // 独占一行、顶格的主要子句
    private static readonly string[] MajorClauses =
    {
        "FROM", "WHERE", "GROUP BY", "ORDER BY", "HAVING", "LIMIT", "UNION ALL", "UNION", "VALUES", "SET"
    };

    // 换行并缩进的连接/条件
    private static readonly string[] IndentClauses =
    {
        "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "CROSS JOIN", "JOIN", "AND", "OR"
    };

    public static string Format(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        // 1) 折叠多余空白为单空格
        var text = Regex.Replace(sql.Trim(), @"\s+", " ");

        // 2) 关键字大写（长词优先，避免 JOIN 抢先匹配 INNER JOIN）
        foreach (var kw in Keywords.OrderByDescending(k => k.Length))
        {
            text = Regex.Replace(text, $@"(?<![\w]){Regex.Escape(kw)}(?![\w])", kw, RegexOptions.IgnoreCase);
        }

        // 3) 主要子句前换行
        foreach (var clause in MajorClauses)
        {
            text = Regex.Replace(text, $@"\s+{Regex.Escape(clause)}(?![\w])", $"\n{clause}");
        }

        // 4) JOIN/AND/OR 前换行 + 缩进
        foreach (var clause in IndentClauses)
        {
            text = Regex.Replace(text, $@"\s+{Regex.Escape(clause)}(?![\w])", $"\n  {clause}");
        }

        // 5) 语句分隔符后换行
        text = Regex.Replace(text, @";\s*", ";\n");

        return text.Trim();
    }
}
