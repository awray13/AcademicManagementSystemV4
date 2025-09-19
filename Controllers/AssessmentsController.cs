using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// Handles CRUD operations for Assessments
/// Scaffolded with custom security, validation, and business logic
/// </summary>
[Authorize]
public class AssessmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AssessmentsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET: Assessments
    /// <summary>
    /// Displays all assessments for the current user with filtering and sorting
    /// </summary>
    public async Task<IActionResult> Index(string sortOrder, string statusFilter, string typeFilter,
        string searchString, int? courseFilter, bool showOverdueOnly = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            // Set up view data for sorting and filtering
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
            ViewData["TypeSortParm"] = sortOrder == "Type" ? "type_desc" : "Type";

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["TypeFilter"] = typeFilter;
            ViewData["CourseFilter"] = courseFilter;
            ViewData["ShowOverdueOnly"] = showOverdueOnly;

            // Start with user's assessments
            var assessmentsQuery = _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Where(a => a.Course.Term.UserId == user.Id);

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                assessmentsQuery = assessmentsQuery.Where(a =>
                    a.Name.Contains(searchString) ||
                    a.Description.Contains(searchString) ||
                    a.Course.CourseNumber.Contains(searchString) ||
                    a.Course.Title.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<AssessmentStatus>(statusFilter, out var status))
            {
                assessmentsQuery = assessmentsQuery.Where(a => a.Status == status);
            }

            if (!string.IsNullOrEmpty(typeFilter) && Enum.TryParse<AssessmentType>(typeFilter, out var type))
            {
                assessmentsQuery = assessmentsQuery.Where(a => a.Type == type);
            }

            if (courseFilter.HasValue)
            {
                assessmentsQuery = assessmentsQuery.Where(a => a.CourseId == courseFilter.Value);
            }

            if (showOverdueOnly)
            {
                assessmentsQuery = assessmentsQuery.Where(a => a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed);
            }

            // Apply sorting
            assessmentsQuery = sortOrder switch
            {
                "name_desc" => assessmentsQuery.OrderByDescending(a => a.Name),
                "Date" => assessmentsQuery.OrderBy(a => a.DueDate),
                "date_desc" => assessmentsQuery.OrderByDescending(a => a.DueDate),
                "Status" => assessmentsQuery.OrderBy(a => a.Status),
                "status_desc" => assessmentsQuery.OrderByDescending(a => a.Status),
                "Type" => assessmentsQuery.OrderBy(a => a.Type),
                "type_desc" => assessmentsQuery.OrderByDescending(a => a.Type),
                _ => assessmentsQuery.OrderBy(a => a.DueDate).ThenBy(a => a.Name)
            };

            var assessments = await assessmentsQuery.ToListAsync();

            // Load data for filter dropdowns
            await LoadFilterDataAsync(user.Id);

            return View(assessments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessments for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while loading your assessments.";
            return View(new List<Assessment>());
        }
    }

    // GET: Assessments/Details/5
    /// <summary>
    /// Shows detailed view of a specific assessment
    /// </summary>
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

            if (assessment == null)
            {
                _logger.LogWarning("User {UserId} attempted to access assessment {AssessmentId} they don't own", user.Id, id);
                return NotFound();
            }

            return View(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessment details for ID {AssessmentId}", id);
            TempData["Error"] = "An error occurred while loading assessment details.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Assessments/Create
    /// <summary>
    /// Shows the create assessment form
    /// </summary>
    public async Task<IActionResult> Create(int? courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        await LoadCoursesSelectListAsync(user.Id);

        var model = new Assessment
        {
            DueDate = DateTime.Today.AddDays(7), // Default to one week from today
            Type = AssessmentType.Assignment,
            Status = AssessmentStatus.NotStarted,
            MaxPoints = 100
        };

        // Pre-select course if provided
        if (courseId.HasValue)
        {
            var course = await _context.Courses
                .Include(c => c.Term)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.Term.UserId == user.Id);

            if (course != null)
            {
                model.CourseId = courseId.Value;
                ViewBag.PreselectedCourse = $"{course.CourseNumber} - {course.Title}";
            }
        }

        return View(model);
    }

    // POST: Assessments/Create
    /// <summary>
    /// Creates a new assessment for the current user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,Type,DueDate,Status,Score,MaxPoints,CourseId")] Assessment assessment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Custom validation
        await ValidateAssessmentAsync(assessment, user.Id, ModelState);

        if (ModelState.IsValid)
        {
            try
            {
                assessment.CreatedAt = DateTime.UtcNow;
                assessment.UpdatedAt = DateTime.UtcNow;

                _context.Add(assessment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created assessment {AssessmentName} for course {CourseId}",
                    user.Id, assessment.Name, assessment.CourseId);

                TempData["Success"] = $"Assessment '{assessment.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assessment for user {UserId}", user.Id);
                ModelState.AddModelError("", "An error occurred while creating the assessment.");
            }
        }

        await LoadCoursesSelectListAsync(user.Id);
        return View(assessment);
    }

    // GET: Assessments/Edit/5
    /// <summary>
    /// Shows the edit form for an existing assessment
    /// </summary>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var assessment = await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

        if (assessment == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit assessment {AssessmentId} they don't own", user.Id, id);
            return NotFound();
        }

        await LoadCoursesSelectListAsync(user.Id);
        return View(assessment);
    }

    // POST: Assessments/Edit/5
    /// <summary>
    /// Updates an existing assessment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Type,DueDate,Status,Score,MaxPoints,CourseId,CreatedAt")] Assessment assessment)
    {
        if (id != assessment.Id)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Verify ownership through course and term
        var originalAssessment = await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

        if (originalAssessment == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit assessment {AssessmentId} they don't own", user.Id, id);
            return NotFound();
        }

        await ValidateAssessmentAsync(assessment, user.Id, ModelState, id);

        if (ModelState.IsValid)
        {
            try
            {
                assessment.UpdatedAt = DateTime.UtcNow;

                _context.Update(assessment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated assessment {AssessmentId}", user.Id, id);
                TempData["Success"] = $"Assessment '{assessment.Name}' updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await AssessmentExistsAsync(assessment.Id, user.Id))
                {
                    return NotFound();
                }

                _logger.LogError(ex, "Concurrency error updating assessment {AssessmentId}", id);
                ModelState.AddModelError("", "The assessment was updated by another process. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assessment {AssessmentId}", id);
                ModelState.AddModelError("", "An error occurred while updating the assessment.");
            }
        }

        await LoadCoursesSelectListAsync(user.Id);
        return View(assessment);
    }

    // GET: Assessments/Delete/5
    /// <summary>
    /// Shows confirmation page for assessment deletion
    /// </summary>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var assessment = await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

        if (assessment == null)
        {
            _logger.LogWarning("User {UserId} attempted to delete assessment {AssessmentId} they don't own", user.Id, id);
            return NotFound();
        }

        return View(assessment);
    }

    // POST: Assessments/Delete/5
    /// <summary>
    /// Deletes an assessment
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

            if (assessment != null)
            {
                _context.Assessments.Remove(assessment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted assessment {AssessmentId}", user.Id, id);
                TempData["Success"] = $"Assessment '{assessment.Name}' deleted successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment {AssessmentId}", id);
            TempData["Error"] = "An error occurred while deleting the assessment.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Validates assessment business rules
    /// </summary>
    private async Task ValidateAssessmentAsync(Assessment assessment, string userId, ModelStateDictionary modelState, int? excludeAssessmentId = null)
    {
        // Validate that the selected course belongs to the user
        var course = await _context.Courses
            .Include(c => c.Term)
            .FirstOrDefaultAsync(c => c.Id == assessment.CourseId && c.Term.UserId == userId);

        if (course == null)
        {
            modelState.AddModelError("CourseId", "Please select a valid course.");
            return;
        }

        // Validate due date is within course dates
        if (assessment.DueDate < course.StartDate || assessment.DueDate > course.EndDate.AddDays(7)) // Allow 1 week grace period
        {
            modelState.AddModelError("DueDate",
                $"Assessment due date should be within the course dates ({course.StartDate:MM/dd/yyyy} - {course.EndDate:MM/dd/yyyy}).");
        }

        // Validate score is within max points range if both are provided
        if (assessment.Score.HasValue && assessment.Score.Value > assessment.MaxPoints)
        {
            modelState.AddModelError("Score", "Score cannot exceed maximum points.");
        }

        // Validate score is non-negative
        if (assessment.Score.HasValue && assessment.Score.Value < 0)
        {
            modelState.AddModelError("Score", "Score cannot be negative.");
        }

        // Business rule: If score is provided, status should be Completed or Graded
        if (assessment.Score.HasValue &&
            assessment.Status != AssessmentStatus.Completed &&
            assessment.Status != AssessmentStatus.Graded)
        {
            modelState.AddModelError("Status", "Status should be 'Completed' or 'Graded' when a score is provided.");
        }

        // Check for duplicate assessment names within the same course
        var duplicateQuery = _context.Assessments
            .Where(a => a.CourseId == assessment.CourseId &&
                       a.Name.ToUpper() == assessment.Name.ToUpper());

        if (excludeAssessmentId.HasValue)
        {
            duplicateQuery = duplicateQuery.Where(a => a.Id != excludeAssessmentId.Value);
        }

        var duplicateAssessment = await duplicateQuery.FirstOrDefaultAsync();
        if (duplicateAssessment != null)
        {
            modelState.AddModelError("Name",
                $"An assessment named '{assessment.Name}' already exists in this course.");
        }

        // Validate reasonable max points range
        if (assessment.MaxPoints <= 0 || assessment.MaxPoints > 1000)
        {
            modelState.AddModelError("MaxPoints", "Maximum points must be between 1 and 1000.");
        }

        // Warning for past due dates on new assessments
        if (!excludeAssessmentId.HasValue && assessment.DueDate < DateTime.Today)
        {
            modelState.AddModelError("DueDate", "Warning: Due date is in the past.");
        }
    }

    /// <summary>
    /// Loads courses for dropdown selection
    /// </summary>
    private async Task LoadCoursesSelectListAsync(string userId)
    {
        var courses = await _context.Courses
            .Include(c => c.Term)
            .Where(c => c.Term.UserId == userId)
            .OrderBy(c => c.Term.StartDate)
            .ThenBy(c => c.CourseNumber)
            .Select(c => new {
                c.Id,
                DisplayName = $"{c.CourseNumber} - {c.Title} ({c.Term.Name})"
            })
            .ToListAsync();

        ViewData["CourseId"] = new SelectList(courses, "Id", "DisplayName");
    }

    /// <summary>
    /// Loads filter dropdown data
    /// </summary>
    private async Task LoadFilterDataAsync(string userId)
    {
        // Load courses for filtering
        var courses = await _context.Courses
            .Include(c => c.Term)
            .Where(c => c.Term.UserId == userId)
            .Select(c => new {
                c.Id,
                DisplayName = $"{c.CourseNumber} - {c.Title}"
            })
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        ViewBag.Courses = new SelectList(courses, "Id", "DisplayName");

        // Load assessment statuses
        ViewBag.AssessmentStatuses = new SelectList(
            Enum.GetValues<AssessmentStatus>()
                .Select(s => new { Value = s.ToString(), Text = s.ToString() }),
            "Value", "Text");

        // Load assessment types
        ViewBag.AssessmentTypes = new SelectList(
            Enum.GetValues<AssessmentType>()
                .Select(t => new { Value = t.ToString(), Text = t.ToString() }),
            "Value", "Text");
    }

    /// <summary>
    /// Checks if an assessment exists for the current user
    /// </summary>
    private async Task<bool> AssessmentExistsAsync(int id, string userId)
    {
        return await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .AnyAsync(a => a.Id == id && a.Course.Term.UserId == userId);
    }

    /// <summary>
    /// AJAX endpoint to update assessment status
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int assessmentId, AssessmentStatus status, double? score = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .FirstOrDefaultAsync(a => a.Id == assessmentId && a.Course.Term.UserId == user.Id);

            if (assessment == null)
                return NotFound();

            assessment.Status = status;
            assessment.UpdatedAt = DateTime.UtcNow;

            // Update score if provided and status is appropriate
            if (score.HasValue && (status == AssessmentStatus.Completed || status == AssessmentStatus.Graded))
            {
                if (score.Value >= 0 && score.Value <= assessment.MaxPoints)
                {
                    assessment.Score = score.Value;
                }
                else
                {
                    return Json(new { success = false, message = $"Score must be between 0 and {assessment.MaxPoints}" });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated assessment {AssessmentId} status to {Status}",
                user.Id, assessmentId, status);

            return Json(new
            {
                success = true,
                message = $"Assessment status updated to {status}",
                newScore = assessment.Score
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment status for {AssessmentId}", assessmentId);
            return Json(new { success = false, message = "Error updating assessment status" });
        }
    }

    /// <summary>
    /// AJAX endpoint to get upcoming assessments
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUpcomingAssessments(int days = 7)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var upcomingDate = DateTime.Now.AddDays(days);

        var assessments = await _context.Assessments
            .Include(a => a.Course)
            .Where(a => a.Course.Term.UserId == user.Id &&
                       a.DueDate >= DateTime.Now &&
                       a.DueDate <= upcomingDate &&
                       a.Status != AssessmentStatus.Completed)
            .OrderBy(a => a.DueDate)
            .Select(a => new {
                a.Id,
                a.Name,
                a.Type,
                a.DueDate,
                a.Status,
                courseName = a.Course.Title,
                courseNumber = a.Course.CourseNumber,
                daysUntilDue = (a.DueDate.Date - DateTime.Now.Date).Days,
                isOverdue = a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed
            })
            .ToListAsync();

        return Json(assessments);
    }

    /// <summary>
    /// AJAX endpoint to get overdue assessments
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOverdueAssessments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var assessments = await _context.Assessments
            .Include(a => a.Course)
            .Where(a => a.Course.Term.UserId == user.Id &&
                       a.DueDate < DateTime.Now &&
                       a.Status != AssessmentStatus.Completed)
            .OrderBy(a => a.DueDate)
            .Select(a => new {
                a.Id,
                a.Name,
                a.Type,
                a.DueDate,
                a.Status,
                courseName = a.Course.Title,
                courseNumber = a.Course.CourseNumber,
                daysOverdue = Math.Abs((a.DueDate.Date - DateTime.Now.Date).Days)
            })
            .ToListAsync();

        return Json(assessments);
    }

    /// <summary>
    /// AJAX endpoint for bulk status updates
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BulkUpdateStatus(List<int> assessmentIds, AssessmentStatus status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            var assessments = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Where(a => assessmentIds.Contains(a.Id) && a.Course.Term.UserId == user.Id)
                .ToListAsync();

            foreach (var assessment in assessments)
            {
                assessment.Status = status;
                assessment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} bulk updated {Count} assessments to status {Status}",
                user.Id, assessments.Count, status);

            return Json(new
            {
                success = true,
                message = $"{assessments.Count} assessment(s) updated to {status}",
                updatedCount = assessments.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk update for user {UserId}", user.Id);
            return Json(new { success = false, message = "Error updating assessments" });
        }
    }

    /// <summary>
    /// Gets assessment statistics for dashboard/reporting
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAssessmentStats()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            var totalAssessments = await _context.Assessments
                .CountAsync(a => a.Course.Term.UserId == user.Id);

            var completedAssessments = await _context.Assessments
                .CountAsync(a => a.Course.Term.UserId == user.Id && a.Status == AssessmentStatus.Completed);

            var overdueAssessments = await _context.Assessments
                .CountAsync(a => a.Course.Term.UserId == user.Id &&
                               a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed);

            var upcomingAssessments = await _context.Assessments
                .CountAsync(a => a.Course.Term.UserId == user.Id &&
                               a.DueDate >= DateTime.Now &&
                               a.DueDate <= DateTime.Now.AddDays(7) &&
                               a.Status != AssessmentStatus.Completed);

            var averageScore = await _context.Assessments
                .Where(a => a.Course.Term.UserId == user.Id && a.Score.HasValue)
                .AverageAsync(a => a.Score.Value);

            var stats = new
            {
                totalAssessments,
                completedAssessments,
                overdueAssessments,
                upcomingAssessments,
                completionRate = totalAssessments > 0 ? Math.Round((double)completedAssessments / totalAssessments * 100, 1) : 0,
                averageScore = double.IsNaN(averageScore) ? 0 : Math.Round(averageScore, 1),
                byType = await GetAssessmentsByTypeAsync(user.Id),
                byStatus = await GetAssessmentsByStatusAsync(user.Id)
            };

            return Json(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assessment stats for user {UserId}", user.Id);
            return Json(new { success = false, message = "Error retrieving statistics" });
        }
    }

    /// <summary>
    /// Helper method to get assessments grouped by type
    /// </summary>
    private async Task<object> GetAssessmentsByTypeAsync(string userId)
    {
        var assessmentsByType = await _context.Assessments
            .Where(a => a.Course.Term.UserId == userId)
            .GroupBy(a => a.Type)
            .Select(g => new {
                type = g.Key.ToString(),
                count = g.Count(),
                completed = g.Count(a => a.Status == AssessmentStatus.Completed)
            })
            .ToListAsync();

        return assessmentsByType;
    }

    /// <summary>
    /// Helper method to get assessments grouped by status
    /// </summary>
    private async Task<object> GetAssessmentsByStatusAsync(string userId)
    {
        var assessmentsByStatus = await _context.Assessments
            .Where(a => a.Course.Term.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new {
                status = g.Key.ToString(),
                count = g.Count()
            })
            .ToListAsync();

        return assessmentsByStatus;
    }

    /// <summary>
    /// Export assessments to CSV (for reporting)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportToCsv()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            var assessments = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Where(a => a.Course.Term.UserId == user.Id)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Assessment Name,Type,Course,Term,Due Date,Status,Score,Max Points,Created Date");

            foreach (var assessment in assessments)
            {
                csv.AppendLine($"\"{assessment.Name}\"," +
                              $"{assessment.Type}," +
                              $"\"{assessment.Course.CourseNumber} - {assessment.Course.Title}\"," +
                              $"\"{assessment.Course.Term.Name}\"," +
                              $"{assessment.DueDate:yyyy-MM-dd}," +
                              $"{assessment.Status}," +
                              $"{assessment.Score?.ToString("F1") ?? ""}," +
                              $"{assessment.MaxPoints}," +
                              $"{assessment.CreatedAt:yyyy-MM-dd}");
            }

            var fileName = $"Assessments_{DateTime.Now:yyyyMMdd}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting assessments for user {UserId}", user.Id);
            TempData["Error"] = "Error exporting assessments.";
            return RedirectToAction(nameof(Index));
        }
    }
}
