namespace DbManager.Common;

public class PageResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

public class PageQuery
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = AppConst.DefaultPageSize;
    public string? OrderBy { get; set; }
    public string? Filter { get; set; }
}