using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models;

/// <summary>
/// Template for creating standardized courses with predefined assessments
/// </summary>
public class CourseTemplate : BaseEntity
{
    [Required]
    [StringLength(10)]
    public string CourseNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 6)]
    public int CreditHours { get; set; }

    // Navigation properties
    public virtual ICollection<AssessmentTemplate> AssessmentTemplates { get; set; } = new List<AssessmentTemplate>();

    // Display property for dropdown
    public string DisplayName => $"{CourseNumber} - {Title} ({CreditHours} credits)";
}
