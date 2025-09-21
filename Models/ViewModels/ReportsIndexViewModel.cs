using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Controllers;

namespace AcademicManagementSystemV4.Models.ViewModels;

/// <summary>
/// View model for the reports index page
/// </summary>
public class ReportsIndexViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<ReportTypeInfo> AvailableReports { get; set; } = new();
    public List<RecentReportInfo> RecentReports { get; set; } = new();
    public List<Term> UserTerms { get; set; } = new();
}

/// <summary>
/// Information about a report type
/// </summary>
public class ReportTypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool RequiresSelection { get; set; }
    public string SelectionType { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}

/// <summary>
/// Information about recently generated reports
/// </summary>
public class RecentReportInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string Size { get; set; } = string.Empty;
}

/// <summary>
/// Report history item
/// </summary>
public class ReportHistoryItem
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string FileSize { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
