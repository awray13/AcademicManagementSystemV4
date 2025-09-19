using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AcademicManagementSystemV4.Models.ViewModels;

/// <summary>
/// View model for user registration functionality
/// Implements comprehensive validation, security best practices, and user experience features
/// </summary>
public class RegisterViewModel : IValidatableObject
{
    /// <summary>
    /// User's email address (will be used as username)
    /// </summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, apostrophes, and periods.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, apostrophes, and periods.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's password for the account
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation for validation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number for account security
    /// </summary>
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number (Optional)")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// User agreement to terms and conditions
    /// </summary>
    [Required(ErrorMessage = "You must agree to the terms and conditions.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions to create an account.")]
    [Display(Name = "I agree to the Terms and Conditions and Privacy Policy")]
    public bool AgreeToTerms { get; set; } = false;

    /// <summary>
    /// Optional newsletter subscription
    /// </summary>
    [Display(Name = "Subscribe to academic news and updates")]
    public bool SubscribeToNewsletter { get; set; } = false;

    /// <summary>
    /// URL to redirect to after successful registration
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Indicates if email is available for registration
    /// </summary>
    public bool? IsEmailAvailable { get; set; }

    /// <summary>
    /// Password strength indicator
    /// </summary>
    public PasswordStrength PasswordStrengthLevel => CalculatePasswordStrength();

    /// <summary>
    /// Collection of password improvement suggestions
    /// </summary>
    public IEnumerable<string> PasswordSuggestions => GetPasswordSuggestions();

    /// <summary>
    /// Validates the email domain against allowed educational domains
    /// </summary>
    /// <returns>True if email domain is acceptable</returns>
    public bool IsValidEducationalEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return false;

        try
        {
            var emailParts = Email.Split('@');
            if (emailParts.Length != 2)
                return false;

            var domain = emailParts[1].ToLowerInvariant();

            // Allow common educational domains and major email providers for this system
            var allowedDomains = new HashSet<string>
                {
                    // Educational domains
                    "edu", "university", "college", "wgu.edu", "student.wgu.edu",
                    // Major email providers (for accessibility)
                    "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "live.com"
                };

            return allowedDomains.Any(allowedDomain =>
                domain == allowedDomain || domain.EndsWith($".{allowedDomain}"));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitizes all input fields
    /// </summary>
    public void SanitizeInputs()
    {
        Email = Email?.Trim().ToLowerInvariant() ?? string.Empty;
        FirstName = SanitizeName(FirstName);
        LastName = SanitizeName(LastName);
        PhoneNumber = SanitizePhoneNumber(PhoneNumber);
    }

    /// <summary>
    /// Calculates password strength based on complexity
    /// </summary>
    /// <returns>Password strength level</returns>
    private PasswordStrength CalculatePasswordStrength()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return PasswordStrength.None;

        var score = 0;

        // Length scoring
        if (Password.Length >= 8)
            score += 1;
        if (Password.Length >= 12)
            score += 1;
        if (Password.Length >= 16)
            score += 1;

        // Character variety scoring
        if (Regex.IsMatch(Password, @"[a-z]"))
            score += 1;
        if (Regex.IsMatch(Password, @"[A-Z]"))
            score += 1;
        if (Regex.IsMatch(Password, @"\d"))
            score += 1;
        if (Regex.IsMatch(Password, @"[^\da-zA-Z]"))
            score += 1;

        // Complexity patterns
        if (Regex.IsMatch(Password, @"(.)\1{2,}"))
            score -= 1; // Repeated characters
        if (Regex.IsMatch(Password, @"123|abc|qwe", RegexOptions.IgnoreCase))
            score -= 1; // Common patterns

        return score switch
        {
            <= 2 => PasswordStrength.Weak,
            <= 4 => PasswordStrength.Fair,
            <= 6 => PasswordStrength.Good,
            _ => PasswordStrength.Strong
        };
    }

    /// <summary>
    /// Gets suggestions for improving password strength
    /// </summary>
    /// <returns>Collection of improvement suggestions</returns>
    private IEnumerable<string> GetPasswordSuggestions()
    {
        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(Password))
        {
            suggestions.Add("Password is required");
            return suggestions;
        }

        if (Password.Length < 8)
            suggestions.Add("Use at least 8 characters");

        if (Password.Length < 12)
            suggestions.Add("Consider using 12 or more characters for better security");

        if (!Regex.IsMatch(Password, @"[a-z]"))
            suggestions.Add("Include at least one lowercase letter");

        if (!Regex.IsMatch(Password, @"[A-Z]"))
            suggestions.Add("Include at least one uppercase letter");

        if (!Regex.IsMatch(Password, @"\d"))
            suggestions.Add("Include at least one number");

        if (!Regex.IsMatch(Password, @"[^\da-zA-Z]"))
            suggestions.Add("Include at least one special character (!@#$%^&*)");

        if (Regex.IsMatch(Password, @"(.)\1{2,}"))
            suggestions.Add("Avoid repeating the same character multiple times");

        if (Regex.IsMatch(Password, @"123|abc|qwe|password", RegexOptions.IgnoreCase))
            suggestions.Add("Avoid common patterns or dictionary words");

        if (suggestions.Count == 0)
            suggestions.Add("Strong password! Good job!");

        return suggestions;
    }

    /// <summary>
    /// Sanitizes name input by removing invalid characters and proper casing
    /// </summary>
    /// <param name="name">Name to sanitize</param>
    /// <returns>Sanitized name</returns>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove multiple spaces and trim
        name = Regex.Replace(name.Trim(), @"\s+", " ");

        // Proper case conversion
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
    }

    /// <summary>
    /// Sanitizes phone number by removing non-digit characters
    /// </summary>
    /// <param name="phoneNumber">Phone number to sanitize</param>
    /// <returns>Sanitized phone number</returns>
    private static string SanitizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters except + at the beginning
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Ensure + is only at the beginning
        if (cleaned.Contains('+') && !cleaned.StartsWith('+'))
        {
            cleaned = cleaned.Replace("+", "");
        }

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    /// <summary>
    /// Gets the full display name (first + last)
    /// </summary>
    /// <returns>Full name for display purposes</returns>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Clears sensitive data from the model
    /// </summary>
    public void ClearSensitiveData()
    {
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    /// <summary>
    /// Custom validation logic for the entire model
    /// </summary>
    /// <param name="validationContext">Validation context</param>
    /// <returns>Collection of validation results</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Email domain validation
        if (!string.IsNullOrWhiteSpace(Email) && !IsValidEducationalEmail())
        {
            results.Add(new ValidationResult(
                "Please use a valid educational email address or a major email provider.",
                new[] { nameof(Email) }));
        }

        // Password confirmation validation
        if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
        {
            results.Add(new ValidationResult(
                "The password and confirmation password do not match.",
                new[] { nameof(ConfirmPassword) }));
        }

        // Name similarity validation (prevent obviously fake names)
        if (!string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName))
        {
            if (FirstName.Equals(LastName, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult(
                    "First name and last name cannot be the same.",
                    new[] { nameof(FirstName), nameof(LastName) }));
            }
        }

        // Phone number format validation (if provided)
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            var phonePattern = @"^[\+]?[\d\s\-\(\)\.]{10,20}$";
            if (!Regex.IsMatch(PhoneNumber, phonePattern))
            {
                results.Add(new ValidationResult(
                    "Please enter a valid phone number format.",
                    new[] { nameof(PhoneNumber) }));
            }
        }

        return results;
    }

    /// <summary>
    /// Creates a safe copy for logging (without sensitive data)
    /// </summary>
    /// <returns>Safe copy of the model</returns>
    public RegisterViewModel CreateSafeCopy()
    {
        return new RegisterViewModel
        {
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            PhoneNumber = PhoneNumber,
            AgreeToTerms = AgreeToTerms,
            SubscribeToNewsletter = SubscribeToNewsletter,
            ReturnUrl = ReturnUrl,
            IsEmailAvailable = IsEmailAvailable
            // Password fields intentionally excluded
        };
    }
}

/// <summary>
/// Enumeration for password strength levels
/// </summary>
public enum PasswordStrength
{
    None = 0,
    Weak = 1,
    Fair = 2,
    Good = 3,
    Strong = 4
}
