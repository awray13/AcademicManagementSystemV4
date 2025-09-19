using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AcademicManagementSystemV4.Models.ViewModels;

/// <summary>
/// View model for password change functionality
/// Implements security best practices, validation, and user experience features
/// </summary>
public class ChangePasswordViewModel : IValidatableObject
{
    /// <summary>
    /// User's current password for verification
    /// </summary>
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "Current password cannot exceed 100 characters.")]
    [Display(Name = "Current Password")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password the user wants to set
    /// </summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "New password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to log out from all devices after password change
    /// </summary>
    [Display(Name = "Log out from all devices after changing password")]
    public bool LogoutFromAllDevices { get; set; } = true;

    /// <summary>
    /// Indicates if the password change was successful
    /// </summary>
    public bool PasswordChanged { get; set; } = false;

    /// <summary>
    /// Custom error message for password change failures
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp of the last password change (for display)
    /// </summary>
    public DateTime? LastPasswordChangeDate { get; set; }

    /// <summary>
    /// Number of failed password change attempts
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Maximum allowed failed attempts before temporary lockout
    /// </summary>
    public const int MaxFailedAttempts = 3;

    /// <summary>
    /// Password strength level of the new password
    /// </summary>
    public PasswordStrength NewPasswordStrengthLevel => CalculatePasswordStrength(NewPassword);

    /// <summary>
    /// Collection of suggestions for improving the new password
    /// </summary>
    public IEnumerable<string> PasswordSuggestions => GetPasswordSuggestions(NewPassword);

    /// <summary>
    /// Security score of the new password (0-100)
    /// </summary>
    public int SecurityScore => CalculateSecurityScore(NewPassword);

    /// <summary>
    /// Indicates if the user has exceeded maximum failed attempts
    /// </summary>
    public bool IsTemporarilyLocked => FailedAttempts >= MaxFailedAttempts;

    /// <summary>
    /// Calculates how long until the user can attempt again
    /// </summary>
    public TimeSpan? LockoutTimeRemaining { get; set; }

    /// <summary>
    /// Validates that the new password is different from the old password
    /// </summary>
    /// <returns>True if passwords are different</returns>
    public bool IsNewPasswordDifferent()
    {
        if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword))
            return false;

        return !OldPassword.Equals(NewPassword, StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates that the new password meets all security requirements
    /// </summary>
    /// <returns>True if password meets requirements</returns>
    public bool IsNewPasswordSecure()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
            return false;

        // Check basic requirements
        var hasMinLength = NewPassword.Length >= 8;
        var hasMaxLength = NewPassword.Length <= 100;
        var hasLowercase = Regex.IsMatch(NewPassword, @"[a-z]");
        var hasUppercase = Regex.IsMatch(NewPassword, @"[A-Z]");
        var hasDigit = Regex.IsMatch(NewPassword, @"\d");
        var hasSpecialChar = Regex.IsMatch(NewPassword, @"[^\da-zA-Z]");

        return hasMinLength && hasMaxLength && hasLowercase && hasUppercase && hasDigit && hasSpecialChar;
    }

    /// <summary>
    /// Calculates the strength level of a given password
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    /// <returns>Password strength level</returns>
    private static PasswordStrength CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return PasswordStrength.None;

        var score = 0;

        // Length scoring
        if (password.Length >= 8)
            score += 1;
        if (password.Length >= 12)
            score += 1;
        if (password.Length >= 16)
            score += 1;

        // Character variety scoring
        if (Regex.IsMatch(password, @"[a-z]"))
            score += 1;
        if (Regex.IsMatch(password, @"[A-Z]"))
            score += 1;
        if (Regex.IsMatch(password, @"\d"))
            score += 1;
        if (Regex.IsMatch(password, @"[^\da-zA-Z]"))
            score += 1;

        // Advanced patterns
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+=\[\]{}|;:,.<>?]"))
            score += 1;

        // Deduct points for weaknesses
        if (Regex.IsMatch(password, @"(.)\1{2,}"))
            score -= 2; // Repeated characters
        if (Regex.IsMatch(password, @"123|abc|qwe|password|admin", RegexOptions.IgnoreCase))
            score -= 2;
        if (Regex.IsMatch(password, @"^\d+$|^[a-zA-Z]+$"))
            score -= 1; // Only numbers or letters

        return score switch
        {
            <= 1 => PasswordStrength.Weak,
            <= 3 => PasswordStrength.Fair,
            <= 5 => PasswordStrength.Good,
            _ => PasswordStrength.Strong
        };
    }

    /// <summary>
    /// Calculates a detailed security score for the password (0-100)
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    /// <returns>Security score from 0-100</returns>
    private static int CalculateSecurityScore(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        var score = 0;

        // Length contribution (0-30 points)
        score += Math.Min(30, password.Length * 2);

        // Character variety (0-40 points)
        if (Regex.IsMatch(password, @"[a-z]"))
            score += 10;
        if (Regex.IsMatch(password, @"[A-Z]"))
            score += 10;
        if (Regex.IsMatch(password, @"\d"))
            score += 10;
        if (Regex.IsMatch(password, @"[^\da-zA-Z]"))
            score += 10;

        // Complexity bonus (0-20 points)
        if (password.Length >= 12)
            score += 5;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+=\[\]{}|;:,.<>?]"))
            score += 5;
        if (!Regex.IsMatch(password, @"(.)\1{1,}"))
            score += 5; // No repeated chars
        if (!Regex.IsMatch(password, @"123|abc|qwe|password", RegexOptions.IgnoreCase))
            score += 5;

        // Deduct for common weaknesses (up to -10 points)
        if (Regex.IsMatch(password, @"(.)\1{2,}"))
            score -= 5;
        if (Regex.IsMatch(password, @"^\d+$|^[a-zA-Z]+$"))
            score -= 3;
        if (password.Length < 8)
            score -= 2;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Gets improvement suggestions for the given password
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    /// <returns>Collection of improvement suggestions</returns>
    private static IEnumerable<string> GetPasswordSuggestions(string password)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            suggestions.Add("Password is required");
            return suggestions;
        }

        if (password.Length < 8)
            suggestions.Add("Use at least 8 characters");
        else if (password.Length < 12)
            suggestions.Add("Consider using 12+ characters for better security");

        if (!Regex.IsMatch(password, @"[a-z]"))
            suggestions.Add("Include at least one lowercase letter");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            suggestions.Add("Include at least one uppercase letter");

        if (!Regex.IsMatch(password, @"\d"))
            suggestions.Add("Include at least one number");

        if (!Regex.IsMatch(password, @"[^\da-zA-Z]"))
            suggestions.Add("Include at least one special character");

        if (Regex.IsMatch(password, @"(.)\1{2,}"))
            suggestions.Add("Avoid repeating characters");

        if (Regex.IsMatch(password, @"123|abc|qwe|password|admin|user", RegexOptions.IgnoreCase))
            suggestions.Add("Avoid common words and patterns");

        if (Regex.IsMatch(password, @"^\d+$"))
            suggestions.Add("Don't use only numbers");

        if (Regex.IsMatch(password, @"^[a-zA-Z]+$"))
            suggestions.Add("Don't use only letters");

        if (suggestions.Count == 0)
        {
            var strength = CalculatePasswordStrength(password);
            suggestions.Add(strength switch
            {
                PasswordStrength.Strong => "Excellent password strength! 🔒",
                PasswordStrength.Good => "Good password strength! Consider making it longer.",
                PasswordStrength.Fair => "Fair password. Add more character variety.",
                _ => "Password needs improvement."
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Validates that passwords don't contain personal information
    /// </summary>
    /// <param name="email">User's email for comparison</param>
    /// <param name="firstName">User's first name for comparison</param>
    /// <param name="lastName">User's last name for comparison</param>
    /// <returns>True if password doesn't contain personal info</returns>
    public bool IsPasswordPersonalInfoFree(string email = null, string firstName = null, string lastName = null)
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
            return false;

        var passwordLower = NewPassword.ToLowerInvariant();

        // Check against email
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailParts = email.ToLowerInvariant().Split('@');
            if (emailParts.Length > 0 && passwordLower.Contains(emailParts[0]))
                return false;
        }

        // Check against first name
        if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length >= 3)
        {
            if (passwordLower.Contains(firstName.ToLowerInvariant()))
                return false;
        }

        // Check against last name
        if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length >= 3)
        {
            if (passwordLower.Contains(lastName.ToLowerInvariant()))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the time since the last password change in a user-friendly format
    /// </summary>
    /// <returns>Human-readable time since last change</returns>
    public string GetTimeSinceLastChange()
    {
        if (!LastPasswordChangeDate.HasValue)
            return "Unknown";

        var timeSpan = DateTime.UtcNow - LastPasswordChangeDate.Value;

        if (timeSpan.TotalDays >= 365)
            return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago";

        if (timeSpan.TotalDays >= 30)
            return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";

        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} day(s) ago";

        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours} hour(s) ago";

        return "Less than an hour ago";
    }

    /// <summary>
    /// Determines if a password change is recommended based on age
    /// </summary>
    /// <returns>True if password change is recommended</returns>
    public bool IsPasswordChangeRecommended()
    {
        if (!LastPasswordChangeDate.HasValue)
            return true;

        // Recommend change if password is older than 90 days
        var daysSinceChange = (DateTime.UtcNow - LastPasswordChangeDate.Value).TotalDays;
        return daysSinceChange >= 90;
    }

    /// <summary>
    /// Gets security recommendations based on password age and strength
    /// </summary>
    /// <returns>Collection of security recommendations</returns>
    public IEnumerable<string> GetSecurityRecommendations()
    {
        var recommendations = new List<string>();

        if (IsPasswordChangeRecommended())
        {
            recommendations.Add("Consider changing your password regularly for better security");
        }

        if (SecurityScore < 60)
        {
            recommendations.Add("Use a stronger password with more complexity");
        }

        if (LogoutFromAllDevices)
        {
            recommendations.Add("You'll be logged out from all devices after changing your password");
        }
        else
        {
            recommendations.Add("Consider logging out from all devices for maximum security");
        }

        recommendations.Add("Never share your password with others");
        recommendations.Add("Use a unique password that you don't use elsewhere");

        return recommendations;
    }

    /// <summary>
    /// Clears sensitive data from the model
    /// </summary>
    public void ClearSensitiveData()
    {
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>
    /// Creates a safe copy of the model for logging (without passwords)
    /// </summary>
    /// <returns>Safe copy for audit logging</returns>
    public ChangePasswordViewModel CreateSafeCopy()
    {
        return new ChangePasswordViewModel
        {
            LogoutFromAllDevices = LogoutFromAllDevices,
            PasswordChanged = PasswordChanged,
            LastPasswordChangeDate = LastPasswordChangeDate,
            FailedAttempts = FailedAttempts,
            LockoutTimeRemaining = LockoutTimeRemaining
            // Passwords intentionally excluded
        };
    }

    /// <summary>
    /// Custom validation for the change password model
    /// </summary>
    /// <param name="validationContext">Validation context</param>
    /// <returns>Collection of validation results</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Ensure new password is different from old password
        if (!string.IsNullOrWhiteSpace(OldPassword) && !string.IsNullOrWhiteSpace(NewPassword))
        {
            if (!IsNewPasswordDifferent())
            {
                results.Add(new ValidationResult(
                    "New password must be different from your current password.",
                    new[] { nameof(NewPassword) }));
            }
        }

        // Validate password confirmation
        if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword != ConfirmPassword)
        {
            results.Add(new ValidationResult(
                "The new password and confirmation password do not match.",
                new[] { nameof(ConfirmPassword) }));
        }

        // Check password security requirements
        if (!string.IsNullOrWhiteSpace(NewPassword) && !IsNewPasswordSecure())
        {
            results.Add(new ValidationResult(
                "New password does not meet security requirements. Please follow the password guidelines.",
                new[] { nameof(NewPassword) }));
        }

        // Check for temporary lockout
        if (IsTemporarilyLocked)
        {
            var timeRemaining = LockoutTimeRemaining?.ToString(@"mm\:ss") ?? "a few minutes";
            results.Add(new ValidationResult(
                $"Too many failed attempts. Please wait {timeRemaining} before trying again.",
                new[] { nameof(OldPassword) }));
        }

        return results;
    }

    /// <summary>
    /// Gets a color class for the password strength indicator
    /// </summary>
    /// <returns>CSS class name for styling</returns>
    public string GetPasswordStrengthColorClass()
    {
        return NewPasswordStrengthLevel switch
        {
            PasswordStrength.Strong => "text-success",
            PasswordStrength.Good => "text-info",
            PasswordStrength.Fair => "text-warning",
            PasswordStrength.Weak => "text-danger",
            _ => "text-muted"
        };
    }

    /// <summary>
    /// Gets a progress bar width percentage for password strength
    /// </summary>
    /// <returns>Width percentage (0-100)</returns>
    public int GetPasswordStrengthWidth()
    {
        return NewPasswordStrengthLevel switch
        {
            PasswordStrength.Strong => 100,
            PasswordStrength.Good => 75,
            PasswordStrength.Fair => 50,
            PasswordStrength.Weak => 25,
            _ => 0
        };
    }

    /// <summary>
    /// Gets display text for the password strength
    /// </summary>
    /// <returns>Human-readable strength description</returns>
    public string GetPasswordStrengthText()
    {
        return NewPasswordStrengthLevel switch
        {
            PasswordStrength.Strong => "Strong 💪",
            PasswordStrength.Good => "Good ✓",
            PasswordStrength.Fair => "Fair ⚠️",
            PasswordStrength.Weak => "Weak ❌",
            _ => "No password"
        };
    }
}

