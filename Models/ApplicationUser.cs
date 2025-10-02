using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models;

/// <summary>
/// Extended user model for the Academic Management System
/// Inherits from IdentityUser and adds custom properties for academic tracking
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user has subscribed to newsletters and updates
    /// </summary>
    public bool SubscribeToNewsletter { get; set; } = false;

    /// <summary>
    /// Last time the user logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Whether the user has completed their profile setup
    /// </summary>
    public bool IsProfileComplete { get; set; } = false;

    /// <summary>
    /// User's preferred time zone for scheduling
    /// </summary>
    [StringLength(50)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Full name for display purposes
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Formatted name for formal contexts (Last, First)
    /// </summary>
    public string FormalName => $"{LastName}, {FirstName}".Trim();

    /// <summary>
    /// User's initials
    /// </summary>
    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpperInvariant();

    // Navigation properties for relationships
    public virtual ICollection<Term> Terms { get; set; } = new List<Term>();

    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the profile as complete
    /// </summary>
    public void CompleteProfile()
    {
        IsProfileComplete = true;
    }

    /// <summary>
    /// Validates if the user has all required information
    /// </summary>
    /// <returns>True if profile is complete</returns>
    public bool HasCompleteProfile()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Email) &&
               EmailConfirmed;
    }
}
