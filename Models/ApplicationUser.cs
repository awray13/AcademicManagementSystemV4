using Microsoft.AspNetCore.Identity;

namespace AcademicManagementSystemV4.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for relationships
    public virtual ICollection<Term> Terms { get; set; } = new List<Term>();
}
