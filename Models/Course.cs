using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models;

public class Course : BaseEntity
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

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.NotStarted;

    // Foreign key
    public int TermId { get; set; }

    // Navigation properties
    public virtual Term Term { get; set; } = null!;
    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    // Calculated properties
    public double CompletionPercentage
    {
        get
        {
            if (!Assessments.Any())
                return 0;
            var completedCount = Assessments.Count(a => a.Status == AssessmentStatus.Completed);
            return (double)completedCount / Assessments.Count * 100;
        }
    }
}

// Enum for course status
public enum CourseStatus
{
    NotStarted,
    InProgress,
    Completed,
    Dropped
}
