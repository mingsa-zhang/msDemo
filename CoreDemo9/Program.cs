using Microsoft.Data.SqlClient;
using MySqlConnector;
using System.Data;
using System.Text;

namespace CoreDemo9;

/// <summary>
/// SQL Server 到 MySQL 数据迁移工具
/// 支持 GUID 自增 ID 映射，保持外键关系正确关联
/// </summary>
class Program
{
    // SQL Server 连接字符串 - 请修改为实际配置
    private const string SqlServerConnectionString = @"Server=192.168.199.170,3435;Database=YYL_ExpressDB;User ID=YYLUser;Password=YYLUser;TrustServerCertificate=True;";

    // MySQL 连接字符串 - 请修改为实际配置
    private const string MySqlConnectionString = @"Server=192.168.199.194;Port=3306;Database=hys_express_center;User=root;Password=Yyy@123*;AllowLoadLocalInfile=true;";

    // 表定义（按依赖顺序排列，先迁移无外键的表）
    private static readonly List<TableDefinition> TableDefinitions = new()
    {
        new("ElectronicWaybillAccessApps", "electronic_waybill_access_app", "Id", "id"),
        new("ElectronicWaybillPlatformConfigs", "electronic_waybill_platform_config", "Id", "id"),
        new("ElectronicWaybillPlatformConfigApis", "electronic_waybill_platform_config_api", "Id", "id"),
        new("ElectronicWaybillPlatformConstants", "electronic_waybill_platform_constant", "Id", "id"),
        new("ElectronicWaybillPushConfigs", "electronic_waybill_push_config", "Id", "id"),
        new("ElectronicWaybillUrlConfigs", "electronic_waybill_url_config", "Id", "id"),
        new("ElectronicWaybillShipperLines", "electronic_waybill_shipper_line", "Id", "id"),
        new("ElectronicWaybillBusinessOrderNoPrefixConfigs", "electronic_waybill_business_order_no_prefix_config", "Id", "id"),
        new("ExpressCompanyConfigs", "express_company_config", "Id", "id"),
        new("ExpressHandlerConfigs", "express_handler_config", "Id", "id"),
        new("ExpressNameMatches", "express_name_match", "Id", "id"),
        new("ExpressUserPermissions", "express_user_permission", "Id", "id"),
        new("ElectronicWaybillBusinessOrders", "electronic_waybill_business_order", "Id", "id",
            foreignKeys: new[] { ("AccessAppId", "ElectronicWaybillAccessApps") }),
        new("ElectronicWaybillApplyRecords", "electronic_waybill_apply_record", "Id", "id",
            foreignKeys: new[] { ("AccessAppId", "ElectronicWaybillAccessApps") }),
        new("ElectronicWaybillApplyRecordDetails", "electronic_waybill_apply_record_detail", "Id", "id",
            foreignKeys: new[] { ("ElectronicWaybillApplyRecordId", "ElectronicWaybillApplyRecords") }),
        new("ElectronicWaybillApplyRecordPackageInfos", "electronic_waybill_apply_record_package_Info", "Id", "id",
            foreignKeys: new[] { ("ElectronicWaybillApplyRecordId", "ElectronicWaybillApplyRecords") }),
        new("ElectronicWaybillApplyRecordParameters", "electronic_waybill_apply_record_parameter", "Id", "id",
            foreignKeys: new[] { ("ElectronicWaybillApplyRecordId", "ElectronicWaybillApplyRecords") }),
        new("ExpressBills", "express_bill", "Id", "id"),
        new("ExpressBillBusinessOrders", "express_bill_business_order", "Id", "id",
            foreignKeys: new[]
            {
                ("ExpressBillId", "ExpressBills"),
                ("ElectronicWaybillBusinessOrderId", "ElectronicWaybillBusinessOrders")
            }),
        new("ExpressBillTrackConfig", "express_bill_track_config", "Id", "id"),
        new("ExpressModifyRecords", "express_modify_record", "Id", "id",
            foreignKeys: new[] { ("ElectronicWaybillBusinessOrderId", "ElectronicWaybillBusinessOrders") })
    };

    // ID 映射表：(源表名, 源GUID) -> 目标表自增ID
    private static readonly Dictionary<(string table, Guid sourceId), long> IdMapping = new();

    /// <summary>
    /// 将 PascalCase 列名转换为 snake_case
    /// 例如: AccessAppId -> access_app_id
    /// </summary>
    private static string PascalCaseToSnakeCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
            return pascalCase;

        var result = new StringBuilder();
        foreach (char c in pascalCase)
        {
            if (char.IsUpper(c))
            {
                // 如果不是第一个字符，添加下划线
                if (result.Length > 0)
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    static async Task Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("SQL Server -> MySQL 数据迁移工具");
        Console.WriteLine("支持 GUID 自增ID 映射，保持外键关联");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // 提示用户配置连接字符串
        Console.WriteLine("请先在代码中配置连接字符串（Program.cs）：");
        Console.WriteLine($"1. SQL Server 连接字符串: {SqlServerConnectionString}");
        Console.WriteLine($"2. MySQL 连接字符串: {MySqlConnectionString}");
        Console.WriteLine();
        Console.WriteLine("配置说明：");
        Console.WriteLine("- 将 YOUR_SQL_SERVER 替换为 SQL Server 地址");
        Console.WriteLine("- 将 YOUR_DATABASE 替换为源数据库名称");
        Console.WriteLine("- 将 YOUR_MYSQL_SERVER 替换为 MySQL 地址");
        Console.WriteLine("- 将 YOUR_USER 替换为用户名");
        Console.WriteLine("- 将 YOUR_PASSWORD 替换为密码");
        Console.WriteLine();

        Console.WriteLine("按任意键开始迁移...");
        Console.ReadKey();

        try
        {
            Console.WriteLine();
            Console.WriteLine("开始迁移...");
            Console.WriteLine();

            await MigrateAllTables();

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine($"迁移完成！");
            Console.WriteLine($"共记录 ID 映射: {IdMapping.Count} 条");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"迁移失败: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }

        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 迁移所有表
    /// </summary>
    private static async Task MigrateAllTables()
    {
        using var sqlConnection = new SqlConnection(SqlServerConnectionString);
        using var mySqlConnection = new MySqlConnection(MySqlConnectionString);

        await sqlConnection.OpenAsync();
        await mySqlConnection.OpenAsync();

        Console.WriteLine("已连接到 SQL Server 和 MySQL");
        Console.WriteLine();

        foreach (var tableDef in TableDefinitions)
        {
            try
            {
                Console.Write($"正在迁移 [{tableDef.SourceTable}] -> [{tableDef.TargetTable}]... ");

                var rowCount = await MigrateTable(sqlConnection, mySqlConnection, tableDef);
                Console.WriteLine($"完成 (迁移 {rowCount} 行)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 迁移单个表
    /// </summary>
    private static async Task<int> MigrateTable(IDbConnection sqlConnection, IDbConnection mySqlConnection, TableDefinition tableDef)
    {
        // 读取 SQL Server 表数据
        var sourceData = await ReadSqlServerTable(sqlConnection, tableDef.SourceTable);

        if (sourceData.Rows.Count == 0)
        {
            return 0;
        }

        // 获取目标表列信息
        var targetColumns = await GetMySqlTableColumns(mySqlConnection, tableDef.TargetTable);

        // 批量插入到 MySQL
        var insertedCount = await BatchInsertToMySQL(mySqlConnection, tableDef, sourceData, targetColumns);

        return insertedCount;
    }

    /// <summary>
    /// 读取 SQL Server 表数据
    /// </summary>
    private static async Task<DataTable> ReadSqlServerTable(IDbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM [{tableName}]";

        using var adapter = new SqlDataAdapter((SqlCommand)command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);

        return dataTable;
    }

    /// <summary>
    /// 获取 MySQL 表的列信息
    /// </summary>
    private static async Task<List<MySqlColumn>> GetMySqlTableColumns(IDbConnection connection, string tableName)
    {
        var columns = new List<MySqlColumn>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_KEY
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
            ORDER BY ORDINAL_POSITION";

        ((MySqlCommand)command).Parameters.AddWithValue("@tableName", tableName);

        using var reader = await ((MySqlCommand)command).ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new MySqlColumn
            {
                Name = reader.GetString("COLUMN_NAME"),
                DataType = reader.GetString("DATA_TYPE").ToLower(),
                MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : (int?)reader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                IsPrimaryKey = reader.GetString("COLUMN_KEY") == "PRI"
            });
        }

        return columns;
    }

    /// <summary>
    /// 批量插入数据到 MySQL
    /// </summary>
    private static async Task<int> BatchInsertToMySQL(IDbConnection connection, TableDefinition tableDef, DataTable sourceData, List<MySqlColumn> targetColumns)
    {
        if (sourceData.Rows.Count == 0)
        {
            return 0;
        }

        // 创建列映射（排除目标表不存在的列，主键列也跳过因为自增）
        var columnMapping = new Dictionary<string, MySqlColumn>();
        // 更安全地找到主键列（查找名为 'id' 的列）
        var primaryKeyColumn = targetColumns.FirstOrDefault(c => c.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

        foreach (DataColumn sourceColumn in sourceData.Columns)
        {
            // 如果是主键列且目标表主键是自增，跳过
            if (sourceColumn.ColumnName.Equals(tableDef.SourcePrimaryKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 如果是 MySQL 的主键列（id），跳过（因为自增）
            if (primaryKeyColumn != null && sourceColumn.ColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 将 SQL Server 列名（PascalCase）转换为 MySQL 列名（snake_case）
            var mysqlColumnName = PascalCaseToSnakeCase(sourceColumn.ColumnName);
            var targetColumn = targetColumns.FirstOrDefault(c =>
                c.Name.Equals(mysqlColumnName, StringComparison.OrdinalIgnoreCase));
            if (targetColumn != null)
            {
                columnMapping[sourceColumn.ColumnName] = targetColumn;
            }
        }

        if (columnMapping.Count == 0)
        {
            return 0;
        }

        // 构建批量插入语句（不包含主键，因为自增）
        var columnNames = columnMapping.Values.Select(c => $"`{c.Name}`").ToList();
        var parameterNames = columnMapping.Values.Select(c => $"@{c.Name}").ToList();

        var insertSql = new StringBuilder();
        insertSql.Append($"INSERT INTO `{tableDef.TargetTable}` (");
        insertSql.Append(string.Join(", ", columnNames));
        insertSql.Append(") VALUES (");
        insertSql.Append(string.Join(", ", parameterNames));
        insertSql.Append(")");

        var mySqlConnection = (MySqlConnection)connection;
        MySqlTransaction transaction = null!;
        var totalInserted = 0;

        try
        {
            transaction = await mySqlConnection.BeginTransactionAsync();

            foreach (DataRow row in sourceData.Rows)
            {
                // 获取源 ID (GUID)
                var sourceId = row[tableDef.SourcePrimaryKey] as Guid? ?? Guid.Empty;
                if (sourceId == Guid.Empty)
                {
                    // 尝试从行中获取 ID
                    if (row[tableDef.SourcePrimaryKey] != DBNull.Value)
                    {
                        sourceId = Guid.Parse(row[tableDef.SourcePrimaryKey].ToString()!);
                    }
                    else
                    {
                        continue; // 无法获取 ID，跳过此行
                    }
                }

                // 执行插入
                long newId;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertSql.ToString();
                    ((MySqlCommand)command).Transaction = transaction;

                    foreach (var (sourceColName, targetCol) in columnMapping)
                    {
                        var value = row[sourceColName];

                        // 处理外键列：使用映射表查找新 ID
                        var mappedValue = HandleForeignKey(value, sourceColName, tableDef);

                        var parameterValue = ConvertValue(mappedValue, targetCol);
                        ((MySqlCommand)command).Parameters.AddWithValue($"@{targetCol.Name}", parameterValue ?? DBNull.Value);
                    }

                    await ((MySqlCommand)command).ExecuteNonQueryAsync();

                    // 获取新插入的自增 ID
                    newId = ((MySqlCommand)command).LastInsertedId;
                }

                // 记录 ID 映射关系
                IdMapping[(tableDef.SourceTable, sourceId)] = newId;

                totalInserted++;
            }

            await transaction.CommitAsync();
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return totalInserted;
    }

    /// <summary>
    /// 处理外键列：查找映射表中的新 ID
    /// </summary>
    private static object HandleForeignKey(object? value, string columnName, TableDefinition tableDef)
    {
        // 检查是否是外键列
        if (tableDef.ForeignKeys == null || tableDef.ForeignKeys.Length == 0)
        {
            return value ?? DBNull.Value;
        }

        var fk = tableDef.ForeignKeys!.FirstOrDefault(fk => fk.Column.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        // 检查是否找到对应的外键（通过比较默认值）
        if (fk == default)
        {
            return value ?? DBNull.Value;
        }

        // 外键列：需要将 SQL Server 的 GUID 转换为 MySQL 的新自增 ID
        if (value is Guid guid)
        {
            // 从映射表中查找对应的新 ID
            if (IdMapping.TryGetValue((fk.ReferencedTable, guid), out var newId))
            {
                return newId; // 返回映射后的新 ID，保持A表和B表正确关联
            }
            // 找不到映射关系，抛出异常提示需要先迁移被引用的表
            throw new InvalidOperationException($"找不到外键映射: 表 {fk.ReferencedTable} 中的 GUID {guid} 没有对应的自增 ID。请确保表迁移顺序正确（先迁移 {fk.ReferencedTable} 再迁移 {tableDef.TargetTable}）");
        }

        // 非外键列或非 GUID 值，直接返回
        return value ?? DBNull.Value;
    }

    /// <summary>
    /// 转换数据值以适应 MySQL 数据类型
    /// </summary>
    private static object? ConvertValue(object? value, MySqlColumn column)
    {
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        // 特殊处理 bit 类型
        if (column.DataType == "tinyint" || column.DataType == "bit")
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }
        }

        // 特殊处理 datetime 类型
        if (column.DataType == "datetime" || column.DataType == "timestamp")
        {
            if (value is DateTime dateTime)
            {
                if (dateTime.Year == 1) // SQL Server 最小日期
                {
                    return DateTime.MinValue;
                }
                return dateTime;
            }
        }

        // 特殊处理 bigint（这里应该是新 ID，已经是 long 类型）
        if (column.DataType == "bigint")
        {
            if (value is long longValue)
            {
                return longValue;
            }
        }

        return value;
    }

    /// <summary>
    /// 表定义
    /// </summary>
    private class TableDefinition
    {
        public string SourceTable { get; }
        public string TargetTable { get; }
        public string SourcePrimaryKey { get; }
        public string TargetPrimaryKey { get; }
        public (string Column, string ReferencedTable)[]? ForeignKeys { get; }

        public TableDefinition(string sourceTable, string targetTable, string sourcePrimaryKey, string targetPrimaryKey,
            (string, string)[]? foreignKeys = null)
        {
            SourceTable = sourceTable;
            TargetTable = targetTable;
            SourcePrimaryKey = sourcePrimaryKey;
            TargetPrimaryKey = targetPrimaryKey;
            ForeignKeys = foreignKeys;
        }
    }

    /// <summary>
    /// MySQL 列信息
    /// </summary>
    private class MySqlColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
