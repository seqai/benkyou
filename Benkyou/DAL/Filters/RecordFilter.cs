using System.ComponentModel;
using Benkyou.DAL.Entities;

namespace Benkyou.DAL.Filters;

public class RecordFilter
{
    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<RecordType> RecordTypes { get; set; } = Array.Empty<RecordType>();

    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    
    public RecordSortField SortField = RecordSortField.Default;

    public DateFilterType DateFilterType = DateFilterType.Absolute;

    public DateTime FromDate = DateTime.Now;

    public DateTime ToDate = DateTime.Now;

    public int FromRelative = 0;

    public int ToRelative = 0;

    public bool SortDescending = false;
}

public enum DateFilterType
{
    [Description("Absolute")]
    Absolute,
    [Description("Relative Daily")]
    RelativeDay,
    [Description("Relative Weekly (from Monday)")]
    RelativeFullWeek,
    [Description("Relative Weekly (rolling)")]
    RelativeRollingWeek,
    [Description("Relative Monthly (from 1st)")]
    RelativeFullMonth,
    [Description("Relative Monthly (rolling)")]
    RelativeRollingMonth,
    [Description("Relative Yearly (from 1st of Jan)")]
    RelativeFullYear,
    [Description("Relative Yearly (rolling)")]
    RelativeRollingYear,
}