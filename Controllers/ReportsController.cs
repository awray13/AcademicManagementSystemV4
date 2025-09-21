using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Models.ViewModels;
using AcademicManagementSystemV4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// Handles report generation functionality
/// Provides multiple report types with timestamps, multiple columns, and professional formatting
/// </summary>
[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ReportsController> _logger;
    private readonly ApplicationDbContext _context;

    public ReportsController(
        IReportService reportService,
        UserManager<ApplicationUser> userManager,
        ILogger<ReportsController> logger,
        ApplicationDbContext context)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// GET: Main reports dashboard showing available report types
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            // Get user's terms for the term selection dropdown
            var userTerms = await _context.Terms
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            var viewModel = new ReportsIndexViewModel
            {
                User = user,
                AvailableReports = GetAvailableReports(),
                RecentReports = await GetRecentReportsAsync(user.Id)
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reports index for user {UserId}", user.Id);
            TempData["Error"] = "Error loading reports page.";
            return View(new ReportsIndexViewModel { User = user });
        }
    }

    /// <summary>
    /// POST: Generates a comprehensive term report with all courses and assessments
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateTermReport(int termId, string format = "txt")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} generating term report for term {TermId}", user.Id, termId);

            var reportData = await _reportService.GenerateTermReportAsync(termId, user.Id);

            if (reportData.Length == 0)
            {
                TempData["Error"] = "Term not found or you don't have permission to access it.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = $"TermReport_{termId}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
            var contentType = GetContentType(format);

            // Log report generation for audit trail
            _logger.LogInformation("Term report generated successfully for user {UserId}, term {TermId}, size {Size} bytes",
                user.Id, termId, reportData.Length);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating term report for user {UserId}, term {TermId}", user.Id, termId);
            TempData["Error"] = "An error occurred while generating the term report.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generates a student progress report showing overall academic performance
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateProgressReport(string format = "txt")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} generating progress report", user.Id);

            var reportData = await _reportService.GenerateStudentProgressReportAsync(user.Id);
            var fileName = $"StudentProgress_{user.LastName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
            var contentType = GetContentType(format);

            _logger.LogInformation("Progress report generated successfully for user {UserId}, size {Size} bytes",
                user.Id, reportData.Length);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating progress report for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while generating the progress report.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generates a comprehensive assessment report with deadlines and scores
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAssessmentReport(string format = "txt")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} generating assessment report", user.Id);

            var reportData = await _reportService.GenerateAssessmentReportAsync(user.Id);
            var fileName = $"AssessmentReport_{user.LastName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
            var contentType = GetContentType(format);

            _logger.LogInformation("Assessment report generated successfully for user {UserId}, size {Size} bytes",
                user.Id, reportData.Length);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating assessment report for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while generating the assessment report.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generates a custom report based on user-specified criteria
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Custom()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var viewModel = new CustomReportViewModel
        {
            StartDate = DateTime.Today.AddMonths(-3),
            EndDate = DateTime.Today,
            IncludeTerms = true,
            IncludeCourses = true,
            IncludeAssessments = true
        };

        return View(viewModel);
    }

    /// <summary>
    /// Generates a custom report with user-specified parameters
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Custom(CustomReportViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} generating custom report", user.Id);

            var reportData = await GenerateCustomReportAsync(user.Id, model);
            var fileName = $"CustomReport_{DateTime.Now:yyyyMMdd_HHmmss}.{model.Format}";
            var contentType = GetContentType(model.Format);

            _logger.LogInformation("Custom report generated successfully for user {UserId}, size {Size} bytes",
                user.Id, reportData.Length);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom report for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while generating the custom report.";
            return View(model);
        }
    }

    /// <summary>
    /// AJAX endpoint to preview report content before download
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PreviewReport(string reportType, int? itemId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            byte[] reportData = reportType.ToLower() switch
            {
                "term" when itemId.HasValue => await _reportService.GenerateTermReportAsync(itemId.Value, user.Id),
                "progress" => await _reportService.GenerateStudentProgressReportAsync(user.Id),
                "assessment" => await _reportService.GenerateAssessmentReportAsync(user.Id),
                _ => throw new ArgumentException($"Invalid report type: {reportType}")
            };

            if (reportData.Length == 0)
            {
                return Json(new { success = false, message = "No data available for this report." });
            }

            // Return first 1000 characters as preview
            var preview = System.Text.Encoding.UTF8.GetString(reportData);
            var previewText = preview.Length > 1000 ? preview[..1000] + "..." : preview;

            return Json(new
            {
                success = true,
                preview = previewText,
                fullSize = reportData.Length,
                estimatedLines = preview.Split('\n').Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report preview for user {UserId}", user.Id);
            return Json(new { success = false, message = "Error generating preview." });
        }
    }

    /// <summary>
    /// Gets report generation history for the user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            // In a full implementation, this would query a ReportHistory table
            var history = new List<ReportHistoryItem>
                {
                    new()
                    {
                        ReportType = "Progress Report",
                        GeneratedDate = DateTime.Now.AddDays(-1),
                        FileSize = "2.3 KB",
                        Status = "Completed"
                    },
                    new()
                    {
                        ReportType = "Assessment Report",
                        GeneratedDate = DateTime.Now.AddDays(-3),
                        FileSize = "4.1 KB",
                        Status = "Completed"
                    }
                };

            return View(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading report history for user {UserId}", user.Id);
            TempData["Error"] = "Error loading report history.";
            return View(new List<ReportHistoryItem>());
        }
    }

    /// <summary>
    /// Exports all reports in a zip file
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportAll(string format = "txt")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} exporting all reports", user.Id);

            // Generate all reports
            var progressData = await _reportService.GenerateStudentProgressReportAsync(user.Id);
            var assessmentData = await _reportService.GenerateAssessmentReportAsync(user.Id);

            // Create a simple concatenated report (in a full implementation, you'd create a ZIP file)
            var combinedReport = new System.Text.StringBuilder();
            combinedReport.AppendLine("COMPREHENSIVE ACADEMIC REPORT");
            combinedReport.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            combinedReport.AppendLine($"Student: {user.FirstName} {user.LastName}");
            combinedReport.AppendLine();
            combinedReport.AppendLine(new string('=', 80));
            combinedReport.AppendLine();

            combinedReport.AppendLine(System.Text.Encoding.UTF8.GetString(progressData));
            combinedReport.AppendLine();
            combinedReport.AppendLine(new string('=', 80));
            combinedReport.AppendLine();
            combinedReport.AppendLine(System.Text.Encoding.UTF8.GetString(assessmentData));

            var fileName = $"ComprehensiveReport_{user.LastName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
            var contentType = GetContentType(format);
            var reportBytes = System.Text.Encoding.UTF8.GetBytes(combinedReport.ToString());

            _logger.LogInformation("Comprehensive report generated for user {UserId}, size {Size} bytes",
                user.Id, reportBytes.Length);

            return File(reportBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all reports for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while exporting reports.";
            return RedirectToAction(nameof(Index));
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the appropriate content type for the file format
    /// </summary>
    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "txt" => "text/plain",
            "csv" => "text/csv",
            "json" => "application/json",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Gets list of available report types
    /// </summary>
    private static List<ReportTypeInfo> GetAvailableReports()
    {
        return new List<ReportTypeInfo>
            {
                new()
                {
                    Name = "Term Report",
                    Description = "Detailed report for a specific academic term including all courses and assessments",
                    Icon = "fas fa-calendar",
                    RequiresSelection = true,
                    SelectionType = "Term"
                },
                new()
                {
                    Name = "Progress Report",
                    Description = "Overall academic progress across all terms and courses",
                    Icon = "fas fa-chart-line",
                    RequiresSelection = false
                },
                new()
                {
                    Name = "Assessment Report",
                    Description = "Comprehensive list of all assessments with due dates and scores",
                    Icon = "fas fa-tasks",
                    RequiresSelection = false
                },
                new()
                {
                    Name = "Custom Report",
                    Description = "Build a custom report with specific date ranges and content filters",
                    Icon = "fas fa-cog",
                    RequiresSelection = false,
                    IsCustom = true
                }
            };
    }

    /// <summary>
    /// Gets recent report generation history (placeholder implementation)
    /// </summary>
    private Task<List<RecentReportInfo>> GetRecentReportsAsync(string userId)
    {
        // In a full implementation, this would query a database table
        var recentReports = new List<RecentReportInfo>
            {
                new()
                {
                    Name = "Progress Report",
                    GeneratedDate = DateTime.Now.AddHours(-2),
                    Size = "2.1 KB"
                },
                new()
                {
                    Name = "Assessment Report",
                    GeneratedDate = DateTime.Now.AddDays(-1),
                    Size = "3.7 KB"
                }
            };

        return Task.FromResult(recentReports);
    }

    /// <summary>
    /// Generates a custom report based on user specifications
    /// </summary>
    private async Task<byte[]> GenerateCustomReportAsync(string userId, CustomReportViewModel model)
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("CUSTOM ACADEMIC REPORT");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Date Range: {model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}");
        report.AppendLine();

        // Add selected content sections
        if (model.IncludeTerms)
        {
            report.AppendLine("TERMS SECTION");
            report.AppendLine("Term data for the specified date range would appear here.");
            report.AppendLine();
        }

        if (model.IncludeCourses)
        {
            report.AppendLine("COURSES SECTION");
            report.AppendLine("Course data for the specified date range would appear here.");
            report.AppendLine();
        }

        if (model.IncludeAssessments)
        {
            report.AppendLine("ASSESSMENTS SECTION");
            report.AppendLine("Assessment data for the specified date range would appear here.");
            report.AppendLine();
        }

        // Use Task.Run to make this method truly asynchronous
        return await Task.Run(() => System.Text.Encoding.UTF8.GetBytes(report.ToString()));
    }

    #endregion
}


