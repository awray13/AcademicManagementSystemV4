using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcademicManagementSystemV4.Models;

public class Assessment : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public AssessmentType Type { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.NotStarted;

    [Range(0, 100)]
    public double? Score { get; set; }

    [Range(0, 100)]
    public double MaxPoints { get; set; } = 100;

    // Foreign key
    public int CourseId { get; set; }

    // Navigation properties
    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    // Calculated properties
    [NotMapped]
    public bool IsOverdue => DateTime.Now > DueDate && Status != AssessmentStatus.Completed;

    [NotMapped]
    public int DaysUntilDue => (DueDate.Date - DateTime.Now.Date).Days;
}

// Enum for assessment type
public enum AssessmentType
{
    Objective,
    Performance,
    Project,
    Exam,
    Quiz,
    Assignment
}

// Enum for assessment status
public enum AssessmentStatus
{
    NotStarted,
    InProgress,
    Completed,
    Submitted,
    Graded
}
