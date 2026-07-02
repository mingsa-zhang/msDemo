namespace DbManager.Core.Models;

public class DbConnectionGroupModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string CreatedTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string? UpdatedTime { get; set; }
}
