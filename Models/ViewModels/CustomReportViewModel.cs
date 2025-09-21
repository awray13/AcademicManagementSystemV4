using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models.ViewModels;

/// <summary>
/// View model for custom report generation
/// </summary>
public class CustomReportViewModel
{
    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-3);

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "Include Terms")]
    public bool IncludeTerms { get; set; } = true;

    [Display(Name = "Include Courses")]
    public bool IncludeCourses { get; set; } = true;

    [Display(Name = "Include Assessments")]
    public bool IncludeAssessments { get; set; } = true;

    [Required]
    [Display(Name = "Output Format")]
    public string Format { get; set; } = "txt";

    [Required]
    [StringLength(100)]
    [Display(Name = "Report Title")]
    public string Title { get; set; } = "Custom Report";

    public bool IsValid => StartDate <= EndDate && (IncludeTerms || IncludeCourses || IncludeAssessments);
}
