using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcademicManagementSystemV4.Models;

/// <summary>
/// Template for creating standardized assessments within course templates
/// </summary>
public class AssessmentTemplate : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public AssessmentType Type { get; set; }

    [Range(0, 1000)]
    public double MaxPoints { get; set; } = 100;

    /// <summary>
    /// Number of days from course start date when this assessment is due
    /// </summary>
    public int DaysFromCourseStart { get; set; }

    // Foreign key
    public int CourseTemplateId { get; set; }

    // Navigation properties
    [ForeignKey("CourseTemplateId")]
    public virtual CourseTemplate CourseTemplate { get; set; } = null!;
}
