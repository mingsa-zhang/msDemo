namespace DbManager.Core.Models;

public class TableColumnModel
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public long MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsAutoIncrement { get; set; }
    public string? DefaultValue { get; set; }
    public string? Comment { get; set; }
    public int OrdinalPosition { get; set; }

    // 编辑状态
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public TableColumnModel Clone()
    {
        return new TableColumnModel
        {
            ColumnName = ColumnName,
            DataType = DataType,
            MaxLength = MaxLength,
            IsNullable = IsNullable,
            IsPrimaryKey = IsPrimaryKey,
            IsAutoIncrement = IsAutoIncrement,
            DefaultValue = DefaultValue,
            Comment = Comment,
            OrdinalPosition = OrdinalPosition,
            IsNew = IsNew,
            IsDeleted = IsDeleted
        };
    }
}