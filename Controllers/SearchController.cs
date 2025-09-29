using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AcademicManagementSystemV4.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SearchController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Search or /Search?query=keyword
        public async Task<IActionResult> Index(string query)
        {
            ViewData["Title"] = string.IsNullOrEmpty(query) ? "Search" : $"Search results for '{query}'";
            
            var viewModel = new SearchResultViewModel 
            { 
                Query = query ?? string.Empty 
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var searchTerm = query.ToLower().Trim();
                var results = new List<SearchResultItem>();
                
                // Search Terms
                results.AddRange(await SearchTermsAsync(user.Id, searchTerm));
                
                // Search Courses
                results.AddRange(await SearchCoursesAsync(user.Id, searchTerm));
                
                // Search Assessments
                results.AddRange(await SearchAssessmentsAsync(user.Id, searchTerm));
                
                // Search Reports (static data)
                results.AddRange(SearchReports(searchTerm));
                
                viewModel.Results = results;
                
                _logger.LogInformation("Search performed by user {UserId} for '{Query}' with {Count} results", 
                    user.Id, query, viewModel.TotalResults);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for '{Query}'", query);
                ModelState.AddModelError(string.Empty, "An error occurred during search. Please try again.");
                return View(viewModel);
            }
        }

        private async Task<List<SearchResultItem>> SearchTermsAsync(string userId, string query)
        {
            var terms = await _context.Terms
                .Where(t => t.UserId == userId && 
                           (t.Name.ToLower().Contains(query) || 
                            (t.Description != null && t.Description.ToLower().Contains(query))))
                .OrderByDescending(t => t.StartDate)
                .Take(10)
                .Select(t => new SearchResultItem
                {
                    Title = t.Name,
                    Description = !string.IsNullOrEmpty(t.Description) && t.Description.Length > 100 
                        ? t.Description.Substring(0, 97) + "..." 
                        : t.Description ?? string.Empty,
                    Type = "Term",
                    Url = Url.Action("Details", "Terms", new { id = t.Id }),
                    Date = t.StartDate
                })
                .ToListAsync();

            return terms;
        }

        private async Task<List<SearchResultItem>> SearchCoursesAsync(string userId, string query)
        {
            var courses = await _context.Courses
                .Include(c => c.Term)
                .Where(c => c.Term.UserId == userId && 
                           (c.Title.ToLower().Contains(query) || 
                            c.CourseNumber.ToLower().Contains(query) || 
                            (c.Description != null && c.Description.ToLower().Contains(query))))
                .OrderBy(c => c.Title)
                .Take(10)
                .Select(c => new SearchResultItem
                {
                    Title = $"{c.Title} ({c.CourseNumber})",
                    Description = !string.IsNullOrEmpty(c.Description) && c.Description.Length > 100 
                        ? c.Description.Substring(0, 97) + "..." 
                        : c.Description ?? string.Empty,
                    Type = "Course",
                    Url = Url.Action("Details", "Courses", new { id = c.Id }),
                    Date = c.Term.StartDate
                })
                .ToListAsync();

            return courses;
        }

        private async Task<List<SearchResultItem>> SearchAssessmentsAsync(string userId, string query)
        {
            var assessments = await _context.Assessments
                .Include(a => a.Course)
                .ThenInclude(c => c.Term)
                .Where(a => a.Course.Term.UserId == userId && 
                           (a.Name.ToLower().Contains(query) || 
                            (a.Description != null && a.Description.ToLower().Contains(query))))
                .OrderByDescending(a => a.DueDate)
                .Take(10)
                .Select(a => new SearchResultItem
                {
                    Title = $"{a.Name} - {a.Course.Title}",
                    Description = !string.IsNullOrEmpty(a.Description) && a.Description.Length > 100 
                        ? a.Description.Substring(0, 97) + "..." 
                        : a.Description ?? string.Empty,
                    Type = "Assessment",
                    Url = Url.Action("Details", "Assessments", new { id = a.Id }),
                    Date = a.DueDate
                })
                .ToListAsync();

            return assessments;
        }

        private List<SearchResultItem> SearchReports(string query)
        {
            // Static report types that can be searched
            var reportTypes = new[] {
                new { Name = "Progress Report", Description = "Overall academic progress across all terms", Url = Url.Action("Index", "Reports") },
                new { Name = "Term Report", Description = "Detailed report for a specific academic term", Url = Url.Action("Index", "Reports") },
                new { Name = "Assessment Report", Description = "List of all assessments with due dates", Url = Url.Action("Index", "Reports") },
                new { Name = "Custom Report", Description = "Build a custom report with specific filters", Url = Url.Action("Custom", "Reports") }
            };

            return reportTypes
                .Where(r => r.Name.ToLower().Contains(query) || r.Description.ToLower().Contains(query))
                .Select(r => new SearchResultItem
                {
                    Title = r.Name,
                    Description = r.Description,
                    Type = "Report",
                    Url = r.Url,
                    Date = DateTime.Now
                })
                .ToList();
        }
    }
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
