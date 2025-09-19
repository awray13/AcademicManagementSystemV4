using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// Handles the main dashboard and home page functionality
/// </summary>
[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<HomeController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main dashboard showing user's academic overview
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when accessing dashboard");
                return Challenge();
            }

            var viewModel = await BuildDashboardViewModelAsync(user);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while building dashboard");
            TempData["Error"] = "An error occurred while loading your dashboard.";
            return View(new DashboardViewModel { User = await _userManager.GetUserAsync(User) });
        }
    }

    /// <summary>
    /// Privacy policy page - accessible to anonymous users
    /// </summary>
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Error page - accessible to anonymous users
    /// </summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var errorViewModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        return View(errorViewModel);
    }

    /// <summary>
    /// Builds the dashboard view model with all necessary data
    /// </summary>
    private async Task<DashboardViewModel> BuildDashboardViewModelAsync(ApplicationUser user)
    {
        var viewModel = new DashboardViewModel
        {
            User = user
        };

        // Get active terms (terms that are currently running)
        viewModel.ActiveTerms = await _context.Terms
            .Where(t => t.UserId == user.Id &&
                       t.StartDate <= DateTime.Now &&
                       t.EndDate >= DateTime.Now)
            .Include(t => t.Courses)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();

        // Get upcoming assessments (next 7 days)
        var upcomingDate = DateTime.Now.AddDays(7);
        viewModel.UpcomingAssessments = await _context.Assessments
            .Include(a => a.Course)
            .Where(a => a.Course.Term.UserId == user.Id &&
                       a.DueDate >= DateTime.Now &&
                       a.DueDate <= upcomingDate &&
                       a.Status != AssessmentStatus.Completed)
            .OrderBy(a => a.DueDate)
            .Take(5)
            .ToListAsync();

        // Get overdue assessments
        viewModel.OverdueAssessments = await _context.Assessments
            .Include(a => a.Course)
            .Where(a => a.Course.Term.UserId == user.Id && a.IsOverdue)
            .OrderBy(a => a.DueDate)
            .Take(5)
            .ToListAsync();

        // Get in-progress courses
        viewModel.InProgressCourses = await _context.Courses
            .Include(c => c.Term)
            .Include(c => c.Assessments)
            .Where(c => c.Term.UserId == user.Id && c.Status == CourseStatus.InProgress)
            .ToListAsync();

        // Calculate statistics
        await CalculateStatisticsAsync(viewModel, user.Id);

        return viewModel;
    }

    /// <summary>
    /// Calculates dashboard statistics
    /// </summary>
    private async Task CalculateStatisticsAsync(DashboardViewModel viewModel, string userId)
    {
        viewModel.TotalTerms = await _context.Terms.CountAsync(t => t.UserId == userId);

        viewModel.TotalCourses = await _context.Courses
            .CountAsync(c => c.Term.UserId == userId);

        viewModel.TotalAssessments = await _context.Assessments
            .CountAsync(a => a.Course.Term.UserId == userId);

        var completedAssessments = await _context.Assessments
            .CountAsync(a => a.Course.Term.UserId == userId &&
                           a.Status == AssessmentStatus.Completed);

        viewModel.OverallCompletionRate = viewModel.TotalAssessments > 0
            ? Math.Round((double)completedAssessments / viewModel.TotalAssessments * 100, 1)
            : 0;
    }

    /// <summary>
    /// AJAX endpoint to refresh dashboard data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RefreshDashboard()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var viewModel = await BuildDashboardViewModelAsync(user);
            return PartialView("_DashboardContent", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing dashboard");
            return Json(new { success = false, message = "Failed to refresh dashboard" });
        }
    }
}
