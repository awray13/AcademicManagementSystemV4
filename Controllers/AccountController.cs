using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// Handles user authentication and account management
/// Complements ASP.NET Core Identity with custom functionality
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: Account/Register
    /// Shows user registration form
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    /// <summary>
    /// POST: Account/Register
    /// Creates a new user account
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign default role
                await _userManager.AddToRoleAsync(user, "Student");

                _logger.LogInformation("User {Email} created account successfully", model.Email);

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["Success"] = $"Welcome to Academic Management System, {user.FirstName}! Your account has been created successfully.";

                return RedirectToLocal(returnUrl);
            }

            // Add registration errors to ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for email {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
        }

        return View(model);
    }

    /// <summary>
    /// GET: Account/Login
    /// Shows login form
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Index", "Home");
        }

        // Clear any existing external cookie
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    /// <summary>
    /// POST: Account/Login
    /// Authenticates user credentials
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                _logger.LogInformation("User {Email} logged in successfully", model.Email);

                TempData["Success"] = $"Welcome back, {user?.FirstName}!";
                return RedirectToLocal(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", model.Email);
                ModelState.AddModelError(string.Empty, "This account has been locked out due to multiple failed login attempts.");
                return View(model);
            }

            // Login failed
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            _logger.LogWarning("Failed login attempt for email {Email}", model.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for email {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        }

        return View(model);
    }

    /// <summary>
    /// POST: Account/Logout
    /// Signs out the current user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userEmail = User.Identity?.Name;
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {Email} logged out", userEmail);
            TempData["Success"] = "You have been logged out successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout");
            TempData["Error"] = "An error occurred during logout.";
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// GET: Account/Profile
    /// Shows user profile management page
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user profile.");
            }

            var model = new ProfileViewModel
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                Roles = await _userManager.GetRolesAsync(user)
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profile for user {UserId}", _userManager.GetUserId(User));
            TempData["Error"] = "Error loading your profile.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// POST: Account/Profile
    /// Updates user profile information
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user profile.");
            }

            // Update user properties
            var hasChanges = false;

            if (user.FirstName != model.FirstName.Trim())
            {
                user.FirstName = model.FirstName.Trim();
                hasChanges = true;
            }

            if (user.LastName != model.LastName.Trim())
            {
                user.LastName = model.LastName.Trim();
                hasChanges = true;
            }

            if (user.PhoneNumber != model.PhoneNumber?.Trim())
            {
                user.PhoneNumber = model.PhoneNumber?.Trim();
                hasChanges = true;
            }

            if (hasChanges)
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("User {UserId} updated profile successfully", user.Id);
                    TempData["Success"] = "Your profile has been updated successfully.";
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            else
            {
                TempData["Info"] = "No changes were made to your profile.";
            }

            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", _userManager.GetUserId(User));
            ModelState.AddModelError(string.Empty, "An error occurred while updating your profile.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: Account/ChangePassword
    /// Shows password change form
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    /// <summary>
    /// POST: Account/ChangePassword
    /// Updates user password
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("User {UserId} changed password successfully", user.Id);
                TempData["Success"] = "Your password has been changed successfully.";

                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", _userManager.GetUserId(User));
            ModelState.AddModelError(string.Empty, "An error occurred while changing your password.");
        }

        return View(model);
    }

    /// <summary>
    /// GET: Account/AccessDenied
    /// Shows access denied page
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// GET: Account/Lockout
    /// Shows account lockout page
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
        return View();
    }

    /// <summary>
    /// AJAX endpoint to check email availability
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmailAvailability(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email is required." });
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            var isAvailable = existingUser == null;

            return Json(new
            {
                available = isAvailable,
                message = isAvailable ? "Email is available." : "Email is already in use."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability for {Email}", email);
            return Json(new { available = false, message = "Error checking email availability." });
        }
    }

    /// <summary>
    /// AJAX endpoint to validate password strength
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ValidatePassword(string password)
    {
        try
        {
            var result = new
            {
                isValid = !string.IsNullOrEmpty(password) && password.Length >= 6,
                strength = CalculatePasswordStrength(password),
                suggestions = GetPasswordSuggestions(password)
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password strength");
            return Json(new { isValid = false, strength = "weak", suggestions = new[] { "Error validating password." } });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Redirects to local URL or default location
    /// </summary>
    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Calculates password strength rating
    /// </summary>
    private static string CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "weak";

        var score = 0;

        // Length
        if (password.Length >= 8)
            score++;
        if (password.Length >= 12)
            score++;

        // Character variety
        if (password.Any(char.IsUpper))
            score++;
        if (password.Any(char.IsLower))
            score++;
        if (password.Any(char.IsDigit))
            score++;
        if (password.Any(c => !char.IsLetterOrDigit(c)))
            score++;

        return score switch
        {
            <= 2 => "weak",
            <= 4 => "medium",
            _ => "strong"
        };
    }

    /// <summary>
    /// Provides password improvement suggestions
    /// </summary>
    private static List<string> GetPasswordSuggestions(string password)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrEmpty(password))
            return new List<string> { "Password is required." };

        if (password.Length < 8)
            suggestions.Add("Use at least 8 characters.");

        if (!password.Any(char.IsUpper))
            suggestions.Add("Include at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            suggestions.Add("Include at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            suggestions.Add("Include at least one number.");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            suggestions.Add("Include at least one special character.");

        return suggestions;
    }

    /// <summary>
    /// Placeholder for two-factor authentication code sending
    /// </summary>
    private IActionResult SendCode(string returnUrl, bool rememberMe)
    {
        // Two-factor authentication implementation would go here
        // For now, redirect to login
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    #endregion
}

#region View Models

/// <summary>
/// View model for user profile management
/// </summary>
public class ProfileViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    public bool IsEmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}

#endregion
