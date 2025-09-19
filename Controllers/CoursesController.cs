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
/// Handles CRUD operations for Courses
/// Scaffolded with custom security and validation
/// </summary>
[Authorize]
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CoursesController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET: Courses
    /// <summary>
    /// Displays all courses for the current user, organized by term
    /// </summary>
    public async Task<IActionResult> Index(string sortOrder, string searchString, int? termFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            // Set up sorting parameters
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
            ViewData["CurrentFilter"] = searchString;
            ViewData["TermFilter"] = termFilter;

            // Start with user's courses
            var coursesQuery = _context.Courses
                .Include(c => c.Term)
                .Include(c => c.Assessments)
                .Where(c => c.Term.UserId == user.Id);

            // Apply term filter if specified
            if (termFilter.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.TermId == termFilter.Value);
            }

            // Apply search filter if specified
            if (!string.IsNullOrEmpty(searchString))
            {
                coursesQuery = coursesQuery.Where(c =>
                    c.Title.Contains(searchString) ||
                    c.CourseNumber.Contains(searchString) ||
                    c.Description.Contains(searchString));
            }

            // Apply sorting
            coursesQuery = sortOrder switch
            {
                "name_desc" => coursesQuery.OrderByDescending(c => c.Title),
                "Date" => coursesQuery.OrderBy(c => c.StartDate),
                "date_desc" => coursesQuery.OrderByDescending(c => c.StartDate),
                "Status" => coursesQuery.OrderBy(c => c.Status),
                "status_desc" => coursesQuery.OrderByDescending(c => c.Status),
                _ => coursesQuery.OrderBy(c => c.Term.StartDate).ThenBy(c => c.StartDate)
            };

            var courses = await coursesQuery.ToListAsync();

            // Load terms for filter dropdown
            await LoadTermsForFilterAsync(user.Id);

            return View(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while loading your courses.";
            return View(new List<Course>());
        }
    }

    // GET: Courses/Details/5
    /// <summary>
    /// Shows detailed view of a specific course with assessments
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
            var course = await _context.Courses
                .Include(c => c.Term)
                .Include(c => c.Assessments.OrderBy(a => a.DueDate))
                .FirstOrDefaultAsync(c => c.Id == id && c.Term.UserId == user.Id);

            if (course == null)
            {
                _logger.LogWarning("User {UserId} attempted to access course {CourseId} they don't own", user.Id, id);
                return NotFound();
            }

            return View(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course details for ID {CourseId}", id);
            TempData["Error"] = "An error occurred while loading course details.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Courses/Create
    /// <summary>
    /// Shows the create course form
    /// </summary>
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        await LoadTermsSelectListAsync(user.Id);

        var model = new Course
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = CourseStatus.NotStarted,
            CreditHours = 3
        };

        return View(model);
    }

    // POST: Courses/Create
    /// <summary>
    /// Creates a new course for the current user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseNumber,Title,Description,CreditHours,StartDate,EndDate,Status,TermId")] Course course)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Custom validation
        await ValidateCourseAsync(course, user.Id, ModelState);

        if (ModelState.IsValid)
        {
            try
            {
                course.CreatedAt = DateTime.UtcNow;
                course.UpdatedAt = DateTime.UtcNow;

                _context.Add(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created course {CourseNumber} - {CourseTitle}",
                    user.Id, course.CourseNumber, course.Title);

                TempData["Success"] = $"Course '{course.CourseNumber} - {course.Title}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course for user {UserId}", user.Id);
                ModelState.AddModelError("", "An error occurred while creating the course.");
            }
        }

        await LoadTermsSelectListAsync(user.Id);
        return View(course);
    }

    // GET: Courses/Edit/5
    /// <summary>
    /// Shows the edit form for an existing course
    /// </summary>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var course = await _context.Courses
            .Include(c => c.Term)
            .FirstOrDefaultAsync(c => c.Id == id && c.Term.UserId == user.Id);

        if (course == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit course {CourseId} they don't own", user.Id, id);
            return NotFound();
        }

        await LoadTermsSelectListAsync(user.Id);
        return View(course);
    }

    // POST: Courses/Edit/5
    /// <summary>
    /// Updates an existing course
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CourseNumber,Title,Description,CreditHours,StartDate,EndDate,Status,TermId,CreatedAt")] Course course)
    {
        if (id != course.Id)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Verify ownership through term
        var originalCourse = await _context.Courses
            .Include(c => c.Term)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Term.UserId == user.Id);

        if (originalCourse == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit course {CourseId} they don't own", user.Id, id);
            return NotFound();
        }

        await ValidateCourseAsync(course, user.Id, ModelState, id);

        if (ModelState.IsValid)
        {
            try
            {
                course.UpdatedAt = DateTime.UtcNow;

                _context.Update(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated course {CourseId}", user.Id, id);
                TempData["Success"] = $"Course '{course.CourseNumber} - {course.Title}' updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CourseExistsAsync(course.Id, user.Id))
                {
                    return NotFound();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", id);
                ModelState.AddModelError("", "An error occurred while updating the course.");
            }
        }

        await LoadTermsSelectListAsync(user.Id);
        return View(course);
    }

    // GET: Courses/Delete/5
    /// <summary>
    /// Shows confirmation page for course deletion
    /// </summary>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var course = await _context.Courses
            .Include(c => c.Term)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == id && c.Term.UserId == user.Id);

        if (course == null)
        {
            _logger.LogWarning("User {UserId} attempted to delete course {CourseId} they don't own", user.Id, id);
            return NotFound();
        }

        return View(course);
    }

    // POST: Courses/Delete/5
    /// <summary>
    /// Deletes a course and all associated assessments
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
            var course = await _context.Courses
                .Include(c => c.Term)
                .Include(c => c.Assessments)
                .FirstOrDefaultAsync(c => c.Id == id && c.Term.UserId == user.Id);

            if (course != null)
            {
                var assessmentCount = course.Assessments.Count;

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted course {CourseId} with {AssessmentCount} assessments",
                    user.Id, id, assessmentCount);

                TempData["Success"] = $"Course '{course.CourseNumber} - {course.Title}' and {assessmentCount} assessment(s) deleted successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course {CourseId}", id);
            TempData["Error"] = "An error occurred while deleting the course.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Validates course business rules
    /// </summary>
    private async Task ValidateCourseAsync(Course course, string userId, ModelStateDictionary modelState, int? excludeCourseId = null)
    {
        // Validate that the selected term belongs to the user
        var term = await _context.Terms
            .FirstOrDefaultAsync(t => t.Id == course.TermId && t.UserId == userId);

        if (term == null)
        {
            modelState.AddModelError("TermId", "Please select a valid term.");
            return;
        }

        // Validate date range
        if (course.StartDate >= course.EndDate)
        {
            modelState.AddModelError("EndDate", "Course end date must be after start date.");
        }

        // Validate course dates are within term dates
        if (course.StartDate < term.StartDate || course.EndDate > term.EndDate)
        {
            modelState.AddModelError("StartDate",
                $"Course dates must be within the term dates ({term.StartDate:MM/dd/yyyy} - {term.EndDate:MM/dd/yyyy}).");
        }

        // Check for duplicate course numbers within the same term
        var duplicateQuery = _context.Courses
            .Where(c => c.TermId == course.TermId &&
                       c.CourseNumber.ToUpper() == course.CourseNumber.ToUpper());

        if (excludeCourseId.HasValue)
        {
            duplicateQuery = duplicateQuery.Where(c => c.Id != excludeCourseId.Value);
        }

        var duplicateCourse = await duplicateQuery.FirstOrDefaultAsync();
        if (duplicateCourse != null)
        {
            modelState.AddModelError("CourseNumber",
                $"A course with number '{course.CourseNumber}' already exists in this term.");
        }

        // Validate credit hours range
        if (course.CreditHours < 1 || course.CreditHours > 6)
        {
            modelState.AddModelError("CreditHours", "Credit hours must be between 1 and 6.");
        }

        // Validate course duration (between 1 week and term length)
        var courseDuration = (course.EndDate - course.StartDate).Days;
        var termDuration = (term.EndDate - term.StartDate).Days;

        if (courseDuration < 7)
        {
            modelState.AddModelError("EndDate", "Course must be at least one week long.");
        }
        else if (courseDuration > termDuration)
        {
            modelState.AddModelError("EndDate", "Course cannot be longer than its term.");
        }
    }

    /// <summary>
    /// Loads terms for dropdown selection
    /// </summary>
    private async Task LoadTermsSelectListAsync(string userId)
    {
        var terms = await _context.Terms
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new {
                t.Id,
                DisplayName = $"{t.Name} ({t.StartDate:MM/dd/yyyy} - {t.EndDate:MM/dd/yyyy})"
            })
            .ToListAsync();

        ViewData["TermId"] = new SelectList(terms, "Id", "DisplayName");
    }

    /// <summary>
    /// Loads terms for filtering dropdown
    /// </summary>
    private async Task LoadTermsForFilterAsync(string userId)
    {
        var terms = await _context.Terms
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new {
                t.Id,
                t.Name
            })
            .ToListAsync();

        ViewBag.Terms = new SelectList(terms, "Id", "Name");
    }

    /// <summary>
    /// Checks if a course exists for the current user
    /// </summary>
    private async Task<bool> CourseExistsAsync(int id, string userId)
    {
        return await _context.Courses
            .Include(c => c.Term)
            .AnyAsync(c => c.Id == id && c.Term.UserId == userId);
    }

    /// <summary>
    /// AJAX endpoint to get courses for a specific term
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCoursesByTerm(int termId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var courses = await _context.Courses
            .Include(c => c.Term)
            .Where(c => c.TermId == termId && c.Term.UserId == user.Id)
            .Select(c => new {
                c.Id,
                c.CourseNumber,
                c.Title,
                DisplayName = $"{c.CourseNumber} - {c.Title}"
            })
            .OrderBy(c => c.CourseNumber)
            .ToListAsync();

        return Json(courses);
    }

    /// <summary>
    /// AJAX endpoint to update course status
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int courseId, CourseStatus status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            var course = await _context.Courses
                .Include(c => c.Term)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.Term.UserId == user.Id);

            if (course == null)
                return NotFound();

            course.Status = status;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated course {CourseId} status to {Status}",
                user.Id, courseId, status);

            return Json(new { success = true, message = $"Course status updated to {status}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course status for {CourseId}", courseId);
            return Json(new { success = false, message = "Error updating course status" });
        }
    }

    /// <summary>
    /// AJAX endpoint to get course progress statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourseProgress(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var course = await _context.Courses
            .Include(c => c.Term)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.Term.UserId == user.Id);

        if (course == null)
            return NotFound();

        var progress = new
        {
            courseId = course.Id,
            courseTitle = $"{course.CourseNumber} - {course.Title}",
            status = course.Status.ToString(),
            totalAssessments = course.Assessments.Count,
            completedAssessments = course.Assessments.Count(a => a.Status == AssessmentStatus.Completed),
            overdueAssessments = course.Assessments.Count(a => a.IsOverdue),
            completionPercentage = course.CompletionPercentage,
            nextAssessment = course.Assessments
                .Where(a => a.Status != AssessmentStatus.Completed && a.DueDate >= DateTime.Now)
                .OrderBy(a => a.DueDate)
                .Select(a => new { a.Name, a.DueDate, a.Type })
                .FirstOrDefault()
        };

        return Json(progress);
    }
}
