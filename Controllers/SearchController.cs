using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Models.ViewModels;
using AcademicManagementSystemV4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcademicManagementSystemV4.Controllers;

/// <summary>
/// Handles global search functionality across Terms, Courses, and Assessments
/// Implements comprehensive search with filtering and result ranking
/// </summary>
[Authorize]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        UserManager<ApplicationUser> userManager,
        ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main search page - shows empty search form
    /// </summary>
    public IActionResult Index()
    {
        return View(new SearchResultViewModel());
    }

    /// <summary>
    /// POST: Execute search and return results
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            TempData["Warning"] = "Please enter a search term.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            _logger.LogInformation("User {UserId} searching for: {Query}", user.Id, query);

            var results = await _searchService.SearchAsync(query, user.Id);

            // Log search analytics
            _logger.LogInformation("Search completed for user {UserId}. Query: '{Query}', Results: {Count}",
                user.Id, query, results.TotalResults);

            return View("Results", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for user {UserId}, query: {Query}", user.Id, query);
            TempData["Error"] = "An error occurred while searching. Please try again.";
            return View("Index", new SearchResultViewModel { Query = query });
        }
    }

    /// <summary>
    /// GET: Execute search via query string (for direct links and AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(string q, string type = null, string sort = "relevance")
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View("Index", new SearchResultViewModel());
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            var results = await _searchService.SearchAsync(q, user.Id);

            // Apply type filter if specified
            if (!string.IsNullOrEmpty(type))
            {
                results.Results = results.Results
                    .Where(r => r.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply sorting
            results.Results = sort switch
            {
                "date" => results.Results.OrderByDescending(r => r.Date).ToList(),
                "name" => results.Results.OrderBy(r => r.Title).ToList(),
                "type" => results.Results.OrderBy(r => r.Type).ThenBy(r => r.Title).ToList(),
                _ => results.Results.ToList() // Default: relevance (already sorted by service)
            };

            ViewData["CurrentSort"] = sort;
            ViewData["CurrentType"] = type;

            return View("Results", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for user {UserId}, query: {Query}", user.Id, q);
            TempData["Error"] = "An error occurred while searching. Please try again.";
            return View("Index", new SearchResultViewModel { Query = q });
        }
    }

    /// <summary>
    /// AJAX endpoint for autocomplete/suggestions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Suggestions(string term, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(new List<object>());
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new List<object>());

        try
        {
            var results = await _searchService.SearchAsync(term, user.Id);

            var suggestions = results.Results
                .Take(maxResults)
                .Select(r => new {
                    label = $"{r.Title} ({r.Type})",
                    value = r.Title,
                    type = r.Type,
                    url = r.Url,
                    description = TruncateDescription(r.Description, 100)
                })
                .ToList();

            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for user {UserId}, term: {Term}", user.Id, term);
            return Json(new List<object>());
        }
    }

    /// <summary>
    /// AJAX endpoint for quick search (lightweight results)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> QuickSearch(string q, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new { success = false, message = "Query required" });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Unauthorized" });

        try
        {
            var results = await _searchService.SearchAsync(q, user.Id);

            var quickResults = results.Results
                .Take(limit)
                .Select(r => new {
                    title = r.Title,
                    type = r.Type,
                    url = r.Url,
                    description = TruncateDescription(r.Description, 80),
                    date = r.Date.ToString("MM/dd/yyyy")
                })
                .ToList();

            return Json(new
            {
                success = true,
                query = q,
                results = quickResults,
                totalResults = results.TotalResults,
                hasMore = results.TotalResults > limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in quick search for user {UserId}, query: {Query}", user.Id, q);
            return Json(new { success = false, message = "Search error" });
        }
    }

    /// <summary>
    /// Advanced search page with filters and options
    /// </summary>
    public IActionResult Advanced()
    {
        var model = new AdvancedSearchViewModel();
        return View(model);
    }

    /// <summary>
    /// POST: Execute advanced search with filters
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Advanced(AdvancedSearchViewModel model)
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
            // Build query from advanced search criteria
            var query = BuildAdvancedQuery(model);

            if (string.IsNullOrWhiteSpace(query))
            {
                TempData["Warning"] = "Please enter search criteria.";
                return View(model);
            }

            _logger.LogInformation("User {UserId} performing advanced search: {Query}", user.Id, query);

            var results = await _searchService.SearchAsync(query, user.Id);

            // Apply advanced filters
            results = ApplyAdvancedFilters(results, model);

            return View("Results", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search for user {UserId}", user.Id);
            TempData["Error"] = "An error occurred during advanced search.";
            return View(model);
        }
    }

    /// <summary>
    /// Export search results to various formats
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportResults(string query, string format = "csv")
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            TempData["Error"] = "No search query provided.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            var results = await _searchService.SearchAsync(query, user.Id);

            return format.ToLower() switch
            {
                "csv" => ExportToCsv(results),
                "json" => ExportToJson(results),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting search results for user {UserId}", user.Id);
            TempData["Error"] = "Error exporting search results.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Get search statistics for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSearchStats()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        try
        {
            // This could be expanded to include search history, popular searches, etc.
            var stats = new
            {
                message = "Search statistics feature available for future enhancement",
                searchesSupported = new[] { "Terms", "Courses", "Assessments" },
                featuresAvailable = new[] { "Basic Search", "Advanced Search", "Quick Search", "Export Results" }
            };

            return Json(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search stats for user {UserId}", user.Id);
            return Json(new { error = "Unable to retrieve search statistics" });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Truncates description text for display
    /// </summary>
    private static string TruncateDescription(string description, int maxLength)
    {
        if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
            return description;

        return description[..maxLength] + "...";
    }

    /// <summary>
    /// Builds query string from advanced search model
    /// </summary>
    private static string BuildAdvancedQuery(AdvancedSearchViewModel model)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(model.Keywords))
            queryParts.Add(model.Keywords.Trim());

        if (!string.IsNullOrWhiteSpace(model.Title))
            queryParts.Add(model.Title.Trim());

        if (!string.IsNullOrWhiteSpace(model.Description))
            queryParts.Add(model.Description.Trim());

        return string.Join(" ", queryParts);
    }

    /// <summary>
    /// Applies advanced filters to search results
    /// </summary>
    private static SearchResultViewModel ApplyAdvancedFilters(SearchResultViewModel results, AdvancedSearchViewModel model)
    {
        var filteredResults = results.Results.AsEnumerable();

        // Filter by type if specified
        if (!string.IsNullOrWhiteSpace(model.ContentType) && model.ContentType != "All")
        {
            filteredResults = filteredResults.Where(r =>
                r.Type.Equals(model.ContentType, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by date range if specified
        if (model.StartDate.HasValue)
        {
            filteredResults = filteredResults.Where(r => r.Date >= model.StartDate.Value);
        }

        if (model.EndDate.HasValue)
        {
            filteredResults = filteredResults.Where(r => r.Date <= model.EndDate.Value);
        }

        results.Results = filteredResults.ToList();
        return results;
    }

    /// <summary>
    /// Exports search results to CSV format
    /// </summary>
    private FileResult ExportToCsv(SearchResultViewModel results)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Title,Type,Description,Date,URL");

        foreach (var result in results.Results)
        {
            csv.AppendLine($"\"{result.Title}\"," +
                          $"{result.Type}," +
                          $"\"{result.Description}\"," +
                          $"{result.Date:yyyy-MM-dd}," +
                          $"\"{result.Url}\"");
        }

        var fileName = $"SearchResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    /// <summary>
    /// Exports search results to JSON format
    /// </summary>
    private FileResult ExportToJson(SearchResultViewModel results)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            query = results.Query,
            totalResults = results.TotalResults,
            exportDate = DateTime.UtcNow,
            results = results.Results.Select(r => new
            {
                title = r.Title,
                type = r.Type,
                description = r.Description,
                date = r.Date,
                url = r.Url
            })
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var fileName = $"SearchResults_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    #endregion
}

/// <summary>
/// View model for advanced search functionality
/// </summary>
public class AdvancedSearchViewModel
{
    public string Keywords { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentType { get; set; } = "All";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "relevance";
}
