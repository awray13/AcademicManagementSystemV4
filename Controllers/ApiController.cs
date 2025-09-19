using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// RESTful API Controller for external integrations and mobile applications
/// Provides JSON endpoints for all major functionality with proper HTTP status codes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ApiController> _logger;

    public ApiController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<ApiController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Dashboard Endpoints

    /// <summary>
    /// GET: api/api/dashboard
    /// Returns dashboard data with upcoming/overdue assessments and statistics
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardApiResponse>> GetDashboardData()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized API access attempt to dashboard");
                return Unauthorized(new { error = "User not found" });
            }

            var upcomingDate = DateTime.Now.AddDays(7);

            var dashboardData = new DashboardApiResponse
            {
                User = new UserApiModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!
                },

                UpcomingAssessments = await _context.Assessments
                    .Include(a => a.Course)
                    .Where(a => a.Course.Term.UserId == user.Id &&
                               a.DueDate >= DateTime.Now &&
                               a.DueDate <= upcomingDate &&
                               a.Status != AssessmentStatus.Completed)
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .Select(a => new AssessmentApiModel
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Type = a.Type.ToString(),
                        DueDate = a.DueDate,
                        Status = a.Status.ToString(),
                        CourseName = a.Course.Title,
                        CourseNumber = a.Course.CourseNumber,
                        DaysUntilDue = (a.DueDate.Date - DateTime.Now.Date).Days,
                        IsOverdue = false
                    })
                    .ToListAsync(),

                OverdueAssessments = await _context.Assessments
                    .Include(a => a.Course)
                    .Where(a => a.Course.Term.UserId == user.Id &&
                               a.DueDate < DateTime.Now &&
                               a.Status != AssessmentStatus.Completed)
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .Select(a => new AssessmentApiModel
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Type = a.Type.ToString(),
                        DueDate = a.DueDate,
                        Status = a.Status.ToString(),
                        CourseName = a.Course.Title,
                        CourseNumber = a.Course.CourseNumber,
                        DaysUntilDue = (a.DueDate.Date - DateTime.Now.Date).Days,
                        IsOverdue = true
                    })
                    .ToListAsync(),

                Statistics = new StatisticsApiModel
                {
                    TotalTerms = await _context.Terms.CountAsync(t => t.UserId == user.Id),
                    TotalCourses = await _context.Courses.CountAsync(c => c.Term.UserId == user.Id),
                    TotalAssessments = await _context.Assessments.CountAsync(a => a.Course.Term.UserId == user.Id),
                    CompletedAssessments = await _context.Assessments
                        .CountAsync(a => a.Course.Term.UserId == user.Id && a.Status == AssessmentStatus.Completed),
                    OverdueAssessments = await _context.Assessments
                        .CountAsync(a => a.Course.Term.UserId == user.Id &&
                                       a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed)
                }
            };

            // Calculate completion rate
            dashboardData.Statistics.CompletionRate = dashboardData.Statistics.TotalAssessments > 0
                ? Math.Round((double)dashboardData.Statistics.CompletedAssessments / dashboardData.Statistics.TotalAssessments * 100, 1)
                : 0;

            _logger.LogInformation("Dashboard data retrieved for user {UserId}", user.Id);
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Terms Endpoints

    /// <summary>
    /// GET: api/api/terms
    /// Returns all terms for the authenticated user
    /// </summary>
    [HttpGet("terms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TermApiModel>>> GetTerms()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var terms = await _context.Terms
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.StartDate)
                .Select(t => new TermApiModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Description = t.Description,
                    CourseCount = t.Courses.Count(),
                    IsActive = t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now
                })
                .ToListAsync();

            _logger.LogInformation("Terms retrieved for user {UserId}: {Count} items", user.Id, terms.Count);
            return Ok(terms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terms");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET: api/api/terms/{id}
    /// Returns a specific term with courses
    /// </summary>
    [HttpGet("terms/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TermDetailApiModel>> GetTerm(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var term = await _context.Terms
                .Include(t => t.Courses)
                .Where(t => t.Id == id && t.UserId == user.Id)
                .Select(t => new TermDetailApiModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Description = t.Description,
                    IsActive = t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now,
                    Courses = t.Courses.Select(c => new CourseApiModel
                    {
                        Id = c.Id,
                        CourseNumber = c.CourseNumber,
                        Title = c.Title,
                        CreditHours = c.CreditHours,
                        Status = c.Status.ToString(),
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        CompletionPercentage = c.CompletionPercentage
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (term == null)
            {
                _logger.LogWarning("Term {TermId} not found for user {UserId}", id, user.Id);
                return NotFound(new { error = "Term not found" });
            }

            return Ok(term);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving term {TermId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Courses Endpoints

    /// <summary>
    /// GET: api/api/courses
    /// Returns all courses for the authenticated user
    /// </summary>
    [HttpGet("courses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CourseApiModel>>> GetCourses(int? termId = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var query = _context.Courses
                .Include(c => c.Term)
                .Where(c => c.Term.UserId == user.Id);

            if (termId.HasValue)
            {
                query = query.Where(c => c.TermId == termId.Value);
            }

            var courses = await query
                .OrderBy(c => c.Term.StartDate)
                .ThenBy(c => c.StartDate)
                .Select(c => new CourseApiModel
                {
                    Id = c.Id,
                    CourseNumber = c.CourseNumber,
                    Title = c.Title,
                    Description = c.Description,
                    CreditHours = c.CreditHours,
                    Status = c.Status.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    TermId = c.TermId,
                    TermName = c.Term.Name,
                    AssessmentCount = c.Assessments.Count(),
                    CompletionPercentage = c.CompletionPercentage
                })
                .ToListAsync();

            _logger.LogInformation("Courses retrieved for user {UserId}: {Count} items", user.Id, courses.Count);
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Assessments Endpoints

    /// <summary>
    /// GET: api/api/assessments
    /// Returns all assessments for the authenticated user
    /// </summary>
    [HttpGet("assessments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<AssessmentApiModel>>> GetAssessments(
        int? courseId = null,
        string status = null,
        bool overdueOnly = false)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var query = _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .Where(a => a.Course.Term.UserId == user.Id);

            // Apply filters
            if (courseId.HasValue)
            {
                query = query.Where(a => a.CourseId == courseId.Value);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AssessmentStatus>(status, out var statusEnum))
            {
                query = query.Where(a => a.Status == statusEnum);
            }

            if (overdueOnly)
            {
                query = query.Where(a => a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed);
            }

            var assessments = await query
                .OrderBy(a => a.DueDate)
                .Select(a => new AssessmentApiModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type.ToString(),
                    DueDate = a.DueDate,
                    Status = a.Status.ToString(),
                    Score = a.Score,
                    MaxPoints = a.MaxPoints,
                    CourseId = a.CourseId,
                    CourseName = a.Course.Title,
                    CourseNumber = a.Course.CourseNumber,
                    TermName = a.Course.Term.Name,
                    IsOverdue = a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed,
                    DaysUntilDue = (a.DueDate.Date - DateTime.Now.Date).Days
                })
                .ToListAsync();

            _logger.LogInformation("Assessments retrieved for user {UserId}: {Count} items", user.Id, assessments.Count);
            return Ok(assessments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessments");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// PUT: api/api/assessments/{id}/status
    /// Updates the status of a specific assessment
    /// </summary>
    [HttpPut("assessments/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> UpdateAssessmentStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Term)
                .FirstOrDefaultAsync(a => a.Id == id && a.Course.Term.UserId == user.Id);

            if (assessment == null)
            {
                _logger.LogWarning("Assessment {AssessmentId} not found for user {UserId}", id, user.Id);
                return NotFound(new ApiResponse { Success = false, Message = "Assessment not found" });
            }

            if (!Enum.TryParse<AssessmentStatus>(request.Status, out var newStatus))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid status value" });
            }

            assessment.Status = newStatus;
            assessment.UpdatedAt = DateTime.UtcNow;

            // Update score if provided
            if (request.Score.HasValue)
            {
                if (request.Score.Value < 0 || request.Score.Value > assessment.MaxPoints)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Score must be between 0 and {assessment.MaxPoints}"
                    });
                }
                assessment.Score = request.Score.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assessment {AssessmentId} status updated to {Status} by user {UserId}",
                id, newStatus, user.Id);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = $"Assessment status updated to {newStatus}",
                Data = new { newStatus = newStatus.ToString(), newScore = assessment.Score }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment {AssessmentId} status", id);
            return StatusCode(500, new ApiResponse { Success = false, Message = "Internal server error" });
        }
    }

    #endregion

    #region Search Endpoints

    /// <summary>
    /// GET: api/api/search
    /// Performs a search across terms, courses, and assessments
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchApiResponse>> Search([FromQuery] string query, [FromQuery] string type = null, [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Query parameter is required" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var results = new List<SearchResultApiModel>();

            // Search terms (if no type filter or type is "term")
            if (string.IsNullOrEmpty(type) || type.Equals("term", StringComparison.OrdinalIgnoreCase))
            {
                var terms = await _context.Terms
                    .Where(t => t.UserId == user.Id &&
                               (t.Name.Contains(query) || t.Description.Contains(query)))
                    .Select(t => new SearchResultApiModel
                    {
                        Id = t.Id,
                        Type = "term",
                        Title = t.Name,
                        Description = t.Description,
                        Date = t.StartDate,
                        Url = $"/Terms/Details/{t.Id}"
                    })
                    .ToListAsync();

                results.AddRange(terms);
            }

            // Search courses (if no type filter or type is "course")
            if (string.IsNullOrEmpty(type) || type.Equals("course", StringComparison.OrdinalIgnoreCase))
            {
                var courses = await _context.Courses
                    .Include(c => c.Term)
                    .Where(c => c.Term.UserId == user.Id &&
                               (c.Title.Contains(query) || c.Description.Contains(query) || c.CourseNumber.Contains(query)))
                    .Select(c => new SearchResultApiModel
                    {
                        Id = c.Id,
                        Type = "course",
                        Title = $"{c.CourseNumber} - {c.Title}",
                        Description = c.Description,
                        Date = c.StartDate,
                        Url = $"/Courses/Details/{c.Id}"
                    })
                    .ToListAsync();

                results.AddRange(courses);
            }

            // Search assessments (if no type filter or type is "assessment")
            if (string.IsNullOrEmpty(type) || type.Equals("assessment", StringComparison.OrdinalIgnoreCase))
            {
                var assessments = await _context.Assessments
                    .Include(a => a.Course)
                        .ThenInclude(c => c.Term)
                    .Where(a => a.Course.Term.UserId == user.Id &&
                               (a.Name.Contains(query) || a.Description.Contains(query)))
                    .Select(a => new SearchResultApiModel
                    {
                        Id = a.Id,
                        Type = "assessment",
                        Title = a.Name,
                        Description = $"{a.Course.CourseNumber} - {a.Description}",
                        Date = a.DueDate,
                        Url = $"/Assessments/Details/{a.Id}"
                    })
                    .ToListAsync();

                results.AddRange(assessments);
            }

            // Apply limit and sort by date
            results = results.OrderBy(r => r.Date).Take(limit).ToList();

            var response = new SearchApiResponse
            {
                Query = query,
                Type = type,
                TotalResults = results.Count,
                Results = results
            };

            _logger.LogInformation("Search performed by user {UserId}: '{Query}', {ResultCount} results",
                user.Id, query, results.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for query: {Query}", query);
            return StatusCode(500, new ApiResponse { Success = false, Message = "Internal server error" });
        }
    }

    #endregion

    #region Statistics Endpoints

    /// <summary>
    /// GET: api/api/statistics
    /// Returns comprehensive statistics for the authenticated user
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserStatisticsApiModel>> GetUserStatistics()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            var stats = new UserStatisticsApiModel
            {
                TotalTerms = await _context.Terms.CountAsync(t => t.UserId == user.Id),
                ActiveTerms = await _context.Terms
                    .CountAsync(t => t.UserId == user.Id && t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now),
                TotalCourses = await _context.Courses.CountAsync(c => c.Term.UserId == user.Id),
                InProgressCourses = await _context.Courses
                    .CountAsync(c => c.Term.UserId == user.Id && c.Status == CourseStatus.InProgress),
                CompletedCourses = await _context.Courses
                    .CountAsync(c => c.Term.UserId == user.Id && c.Status == CourseStatus.Completed),
                TotalAssessments = await _context.Assessments.CountAsync(a => a.Course.Term.UserId == user.Id),
                CompletedAssessments = await _context.Assessments
                    .CountAsync(a => a.Course.Term.UserId == user.Id && a.Status == AssessmentStatus.Completed),
                OverdueAssessments = await _context.Assessments
                    .CountAsync(a => a.Course.Term.UserId == user.Id &&
                                   a.DueDate < DateTime.Now && a.Status != AssessmentStatus.Completed),
                UpcomingAssessments = await _context.Assessments
                    .CountAsync(a => a.Course.Term.UserId == user.Id &&
                                   a.DueDate > DateTime.Now &&
                                   a.DueDate <= DateTime.Now.AddDays(7) &&
                                   a.Status != AssessmentStatus.Completed)
            };

            // Calculate completion rates
            stats.CourseCompletionRate = stats.TotalCourses > 0
                ? Math.Round((double)stats.CompletedCourses / stats.TotalCourses * 100, 1) : 0;

            stats.AssessmentCompletionRate = stats.TotalAssessments > 0
                ? Math.Round((double)stats.CompletedAssessments / stats.TotalAssessments * 100, 1) : 0;

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion
}

#region API Models

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Dashboard API response model
/// </summary>
public class DashboardApiResponse
{
    public UserApiModel User { get; set; } = null!;
    public List<AssessmentApiModel> UpcomingAssessments { get; set; } = new();
    public List<AssessmentApiModel> OverdueAssessments { get; set; } = new();
    public StatisticsApiModel Statistics { get; set; } = null!;
}

/// <summary>
/// User API model
/// </summary>
public class UserApiModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Statistics API model
/// </summary>
public class StatisticsApiModel
{
    public int TotalTerms { get; set; }
    public int TotalCourses { get; set; }
    public int TotalAssessments { get; set; }
    public int CompletedAssessments { get; set; }
    public int OverdueAssessments { get; set; }
    public double CompletionRate { get; set; }
}

/// <summary>
/// Term API model
/// </summary>
public class TermApiModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CourseCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Term detail API model with courses
/// </summary>
public class TermDetailApiModel : TermApiModel
{
    public List<CourseApiModel> Courses { get; set; } = new();
}

/// <summary>
/// Course API model
/// </summary>
public class CourseApiModel
{
    public int Id { get; set; }
    public string CourseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public int AssessmentCount { get; set; }
    public double CompletionPercentage { get; set; }
}

/// <summary>
/// Assessment API model
/// </summary>
public class AssessmentApiModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? Score { get; set; }
    public double MaxPoints { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseNumber { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}

/// <summary>
/// Search API response model
/// </summary>
public class SearchApiResponse
{
    public string Query { get; set; } = string.Empty;
    public string Type { get; set; }
    public int TotalResults { get; set; }
    public List<SearchResultApiModel> Results { get; set; } = new();
}

/// <summary>
/// Search result API model
/// </summary>
public class SearchResultApiModel
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// User statistics API model
/// </summary>
public class UserStatisticsApiModel
{
    public int TotalTerms { get; set; }
    public int ActiveTerms { get; set; }
    public int TotalCourses { get; set; }
    public int InProgressCourses { get; set; }
    public int CompletedCourses { get; set; }
    public int TotalAssessments { get; set; }
    public int CompletedAssessments { get; set; }
    public int OverdueAssessments { get; set; }
    public int UpcomingAssessments { get; set; }
    public double CourseCompletionRate { get; set; }
    public double AssessmentCompletionRate { get; set; }
}

/// <summary>
/// Request model for updating assessment status
/// </summary>
public class UpdateStatusRequest
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Score must be non-negative")]
    public double? Score { get; set; }
}

#endregion
