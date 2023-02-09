using Benkyou.DAL.Entities;

namespace Benkyou.DAL.Filters;

public class RecordFilter
{
    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<RecordType> RecordTypes { get; set; } = Array.Empty<RecordType>();

    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    
    public RecordSortField SortField = RecordSortField.Default;

    public DateTime FromDate = DateTime.MinValue;

    public DateTime ToDate = DateTime.MaxValue;

    public bool SortDescending = false;
}