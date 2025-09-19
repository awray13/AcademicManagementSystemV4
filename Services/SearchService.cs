using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AcademicManagementSystemV4.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SearchResultViewModel> SearchAsync(string query, string userId)
    {
        var results = new List<SearchResultItem>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResultViewModel { Query = query, Results = results };
        }

        // Search terms
        var terms = await _context.Terms
            .Where(t => t.UserId == userId &&
                       (t.Name.Contains(query) || t.Description.Contains(query)))
            .ToListAsync();

        foreach (var term in terms)
        {
            results.Add(new SearchResultItem
            {
                Title = term.Name,
                Description = term.Description,
                Type = "Term",
                Url = $"/Terms/Details/{term.Id}",
                Date = term.StartDate
            });
        }

        // Search courses
        var courses = await _context.Courses
            .Include(c => c.Term)
            .Where(c => c.Term.UserId == userId &&
                       (c.Title.Contains(query) || c.Description.Contains(query) || c.CourseNumber.Contains(query)))
            .ToListAsync();

        foreach (var course in courses)
        {
            results.Add(new SearchResultItem
            {
                Title = $"{course.CourseNumber} - {course.Title}",
                Description = course.Description,
                Type = "Course",
                Url = $"/Courses/Details/{course.Id}",
                Date = course.StartDate
            });
        }

        // Search assessments
        var assessments = await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .Where(a => a.Course.Term.UserId == userId &&
                       (a.Name.Contains(query) || a.Description.Contains(query)))
            .ToListAsync();

        foreach (var assessment in assessments)
        {
            results.Add(new SearchResultItem
            {
                Title = assessment.Name,
                Description = $"{assessment.Course.CourseNumber} - {assessment.Description}",
                Type = "Assessment",
                Url = $"/Assessments/Details/{assessment.Id}",
                Date = assessment.DueDate
            });
        }

        // Sort results by relevance and date
        results = results.OrderBy(r => r.Date).ToList();

        return new SearchResultViewModel
        {
            Query = query,
            Results = results
        };
    }
}
