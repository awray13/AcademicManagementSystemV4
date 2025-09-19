using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models;

public class Term : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // Foreign key
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    // Validation
    public bool IsValidDateRange => StartDate < EndDate;
}
