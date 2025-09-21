using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Models.ViewModels;

/// <summary>
/// View model for user login functionality
/// Implements comprehensive validation and security best practices
/// </summary>
public class LoginViewModel
{
    /// <summary>
    /// User's email address used as the username
    /// </summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to remember the user's login across browser sessions
    /// </summary>
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// URL to redirect to after successful login (for return URL functionality)
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Indicates if this is a failed login attempt (for UI feedback)
    /// </summary>
    public bool LoginFailed { get; set; } = false;

    /// <summary>
    /// Custom error message for login failures
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Number of failed login attempts (for security monitoring)
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Indicates if the account is temporarily locked due to failed attempts
    /// </summary>
    public bool IsLockedOut { get; set; } = false;

    /// <summary>
    /// Time remaining until account lockout expires
    /// </summary>
    public TimeSpan? LockoutTimeRemaining { get; set; }

    /// <summary>
    /// Indicates if two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = false;

    /// <summary>
    /// Validates the email format using a more comprehensive pattern
    /// </summary>
    /// <returns>True if email format is valid</returns>
    public bool IsEmailFormatValid()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return false;

        try
        {
            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(Email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates password meets minimum security requirements
    /// </summary>
    /// <returns>True if password meets requirements</returns>
    public bool IsPasswordValid()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return false;

        return Password.Length >= 6 && Password.Length <= 100;
    }

    /// <summary>
    /// Sanitizes email input by trimming whitespace and converting to lowercase
    /// </summary>
    public void SanitizeEmail()
    {
        if (!string.IsNullOrWhiteSpace(Email))
        {
            Email = Email.Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Clears sensitive data from the model (for security)
    /// </summary>
    public void ClearSensitiveData()
    {
        Password = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>
    /// Gets user-friendly error message based on login failure reason
    /// </summary>
    /// <returns>Appropriate error message for the user</returns>
    public string GetUserFriendlyErrorMessage()
    {
        if (IsLockedOut)
        {
            var timeRemaining = LockoutTimeRemaining?.ToString(@"mm\:ss") ?? "a few minutes";
            return $"Your account has been temporarily locked due to multiple failed login attempts. Please try again in {timeRemaining}.";
        }

        if (RequiresTwoFactor)
        {
            return "Two-factor authentication is required. Please check your phone or email for the verification code.";
        }

        if (LoginFailed)
        {
            return ErrorMessage ?? "Invalid email address or password. Please check your credentials and try again.";
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates a secure copy of the model without sensitive information
    /// </summary>
    /// <returns>Safe copy for logging or auditing</returns>
    public LoginViewModel CreateSafeCopy()
    {
        return new LoginViewModel
        {
            Email = Email,
            RememberMe = RememberMe,
            ReturnUrl = ReturnUrl,
            LoginFailed = LoginFailed,
            FailedAttempts = FailedAttempts,
            IsLockedOut = IsLockedOut,
            LockoutTimeRemaining = LockoutTimeRemaining,
            RequiresTwoFactor = RequiresTwoFactor
        };
    }

    /// <summary>
    /// Validates the entire model and returns validation results
    /// </summary>
    /// <returns>Collection of validation results</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(this);

        if (!Validator.TryValidateObject(this, context, results, true))
        {
            return results;
        }

        // Additional custom validation
        if (!IsEmailFormatValid())
        {
            results.Add(new ValidationResult(
                "Please enter a valid email address format.",
                new[] { nameof(Email) }));
        }

        if (!IsPasswordValid())
        {
            results.Add(new ValidationResult(
                "Password must be between 6 and 100 characters long.",
                new[] { nameof(Password) }));
        }

        return results;
    }

    /// <summary>
    /// Determines if the model is valid for login attempt
    /// </summary>
    /// <returns>True if model is ready for authentication</returns>
    public bool IsValid()
    {
        return !Validate().Any();
    }

    /// <summary>
    /// Gets display text for Remember Me checkbox with security note
    /// </summary>
    /// <returns>Display text with security guidance</returns>
    public string GetRememberMeDisplayText()
    {
        return "Remember me on this device (not recommended for public computers)";
    }
}
