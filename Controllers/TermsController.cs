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
/// Handles CRUD operations for Academic Terms
/// Scaffolded with custom modifications for user security
/// </summary>
[Authorize]
public class TermsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TermsController> _logger;

    public TermsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TermsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET: Terms
    /// <summary>
    /// Displays all terms for the current user
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("Unauthorized access attempt to Terms/Index");
            return Challenge();
        }

        try
        {
            var terms = await _context.Terms
                .Where(t => t.UserId == user.Id)
                .Include(t => t.Courses)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return View(terms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terms for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred while loading your terms.";
            return View(new List<Term>());
        }
    }

    // GET: Terms/Details/5
    /// <summary>
    /// Shows detailed view of a specific term with courses and assessments
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
            var term = await _context.Terms
                .Include(t => t.Courses)
                    .ThenInclude(c => c.Assessments)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (term == null)
            {
                _logger.LogWarning("User {UserId} attempted to access term {TermId} they don't own", user.Id, id);
                return NotFound();
            }

            return View(term);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving term details for ID {TermId}", id);
            TempData["Error"] = "An error occurred while loading term details.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Terms/Create
    /// <summary>
    /// Shows the create term form
    /// </summary>
    public IActionResult Create()
    {
        var model = new Term
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(4) // Default 4-month term
        };
        return View(model);
    }

    // POST: Terms/Create
    /// <summary>
    /// Creates a new term for the current user
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,StartDate,EndDate,Description")] Term term)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Custom validation for business rules
        await ValidateTermAsync(term, user.Id, ModelState);

        if (ModelState.IsValid)
        {
            try
            {
                term.UserId = user.Id;
                term.CreatedAt = DateTime.UtcNow;
                term.UpdatedAt = DateTime.UtcNow;

                _context.Add(term);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created term {TermName}", user.Id, term.Name);
                TempData["Success"] = $"Term '{term.Name}' created successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating term for user {UserId}", user.Id);
                ModelState.AddModelError("", "An error occurred while creating the term.");
            }
        }

        return View(term);
    }

    // GET: Terms/Edit/5
    /// <summary>
    /// Shows the edit form for an existing term
    /// </summary>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var term = await _context.Terms
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (term == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit term {TermId} they don't own", user.Id, id);
            return NotFound();
        }

        return View(term);
    }

    // POST: Terms/Edit/5
    /// <summary>
    /// Updates an existing term
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartDate,EndDate,Description,CreatedAt")] Term term)
    {
        if (id != term.Id)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // Verify ownership
        var existingTerm = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (existingTerm == null)
        {
            _logger.LogWarning("User {UserId} attempted to edit term {TermId} they don't own", user.Id, id);
            return NotFound();
        }

        await ValidateTermAsync(term, user.Id, ModelState, id);

        if (ModelState.IsValid)
        {
            try
            {
                term.UserId = user.Id;
                term.UpdatedAt = DateTime.UtcNow;

                _context.Update(term);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated term {TermId}", user.Id, id);
                TempData["Success"] = $"Term '{term.Name}' updated successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await TermExistsAsync(term.Id, user.Id))
                {
                    return NotFound();
                }

                _logger.LogError(ex, "Concurrency error updating term {TermId}", id);
                ModelState.AddModelError("", "The term was updated by another process. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating term {TermId}", id);
                ModelState.AddModelError("", "An error occurred while updating the term.");
            }
        }

        return View(term);
    }

    // GET: Terms/Delete/5
    /// <summary>
    /// Shows confirmation page for term deletion
    /// </summary>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var term = await _context.Terms
            .Include(t => t.Courses)
                .ThenInclude(c => c.Assessments)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

        if (term == null)
        {
            _logger.LogWarning("User {UserId} attempted to delete term {TermId} they don't own", user.Id, id);
            return NotFound();
        }

        return View(term);
    }

    // POST: Terms/Delete/5
    /// <summary>
    /// Deletes a term and all associated courses/assessments
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
            var term = await _context.Terms
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (term != null)
            {
                var courseCount = term.Courses.Count;

                _context.Terms.Remove(term);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted term {TermId} with {CourseCount} courses",
                    user.Id, id, courseCount);

                TempData["Success"] = $"Term '{term.Name}' and {courseCount} associated course(s) deleted successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting term {TermId}", id);
            TempData["Error"] = "An error occurred while deleting the term.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Validates term business rules
    /// </summary>
    private async Task ValidateTermAsync(Term term, string userId, ModelStateDictionary modelState, int? excludeTermId = null)
    {
        // Validate date range
        if (term.StartDate >= term.EndDate)
        {
            modelState.AddModelError("EndDate", "End date must be after start date.");
        }

        // Check for overlapping terms
        var query = _context.Terms.Where(t => t.UserId == userId);

        if (excludeTermId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTermId.Value);
        }

        var overlappingTerm = await query
            .Where(t => (term.StartDate >= t.StartDate && term.StartDate <= t.EndDate) ||
                       (term.EndDate >= t.StartDate && term.EndDate <= t.EndDate) ||
                       (term.StartDate <= t.StartDate && term.EndDate >= t.EndDate))
            .FirstOrDefaultAsync();

        if (overlappingTerm != null)
        {
            modelState.AddModelError("StartDate",
                $"Date range overlaps with existing term '{overlappingTerm.Name}' " +
                $"({overlappingTerm.StartDate:MM/dd/yyyy} - {overlappingTerm.EndDate:MM/dd/yyyy})");
        }

        // Validate reasonable term length (between 1 week and 2 years)
        var termLength = (term.EndDate - term.StartDate).Days;
        if (termLength < 7)
        {
            modelState.AddModelError("EndDate", "Term must be at least one week long.");
        }
        else if (termLength > 730) // 2 years
        {
            modelState.AddModelError("EndDate", "Term cannot be longer than 2 years.");
        }
    }

    /// <summary>
    /// Checks if a term exists for the current user
    /// </summary>
    private async Task<bool> TermExistsAsync(int id, string userId)
    {
        return await _context.Terms.AnyAsync(e => e.Id == id && e.UserId == userId);
    }

    /// <summary>
    /// AJAX endpoint to get term statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTermStats(int termId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var term = await _context.Terms
            .Include(t => t.Courses)
                .ThenInclude(c => c.Assessments)
            .FirstOrDefaultAsync(t => t.Id == termId && t.UserId == user.Id);

        if (term == null)
            return NotFound();

        var stats = new
        {
            courseCount = term.Courses.Count,
            assessmentCount = term.Courses.SelectMany(c => c.Assessments).Count(),
            completedAssessments = term.Courses.SelectMany(c => c.Assessments)
                .Count(a => a.Status == AssessmentStatus.Completed),
            overdueAssessments = term.Courses.SelectMany(c => c.Assessments)
                .Count(a => a.IsOverdue)
        };

        return Json(stats);
    }
}
