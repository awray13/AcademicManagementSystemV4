using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models;

/// <summary>
/// Represents an academic course, including its identifying information, schedule, status, and related assessments.
/// </summary>
/// <remarks>A course includes details such as its number, title, description, credit hours, and the term in which
/// it is offered. It also tracks the course's start and end dates, current status, and associated assessments. The
/// class provides navigation properties for related entities, enabling integration with term and assessment data. This
/// type is typically used in educational management systems to model course offerings and their progress.</remarks>
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
