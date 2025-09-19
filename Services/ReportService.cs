using Microsoft.EntityFrameworkCore;
using System.Text;
using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using AcademicManagementSystemV4.Services;

namespace AcademicManagementSystemV4.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateTermReportAsync(int termId, string userId)
    {
        var term = await _context.Terms
            .Include(t => t.Courses)
                .ThenInclude(c => c.Assessments)
            .FirstOrDefaultAsync(t => t.Id == termId && t.UserId == userId);

        if (term == null)
            return Array.Empty<byte>();

        var report = new StringBuilder();
        report.AppendLine($"ACADEMIC TERM REPORT");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Term: {term.Name}");
        report.AppendLine($"Period: {term.StartDate:yyyy-MM-dd} to {term.EndDate:yyyy-MM-dd}");
        report.AppendLine();

        report.AppendLine("COURSES:");
        report.AppendLine("Course Number\tTitle\t\t\tCredit Hours\tStatus\t\tCompletion %");
        report.AppendLine(new string('-', 80));

        foreach (var course in term.Courses)
        {
            report.AppendLine($"{course.CourseNumber}\t\t{course.Title}\t{course.CreditHours}\t\t{course.Status}\t{course.CompletionPercentage:F1}%");
        }

        report.AppendLine();
        report.AppendLine("ASSESSMENTS:");
        report.AppendLine("Course\t\tAssessment\t\tType\t\tDue Date\t\tStatus\t\tScore");
        report.AppendLine(new string('-', 80));

        foreach (var course in term.Courses)
        {
            foreach (var assessment in course.Assessments)
            {
                var score = assessment.Score?.ToString("F1") ?? "N/A";
                report.AppendLine($"{course.CourseNumber}\t\t{assessment.Name}\t{assessment.Type}\t{assessment.DueDate:yyyy-MM-dd}\t{assessment.Status}\t{score}");
            }
        }

        return Encoding.UTF8.GetBytes(report.ToString());
    }

    public async Task<byte[]> GenerateStudentProgressReportAsync(string userId)
    {
        var terms = await _context.Terms
            .Include(t => t.Courses)
                .ThenInclude(c => c.Assessments)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();

        var report = new StringBuilder();
        report.AppendLine($"STUDENT PROGRESS REPORT");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        var totalCourses = terms.SelectMany(t => t.Courses).Count();
        var completedCourses = terms.SelectMany(t => t.Courses).Count(c => c.Status == CourseStatus.Completed);
        var overallProgress = totalCourses > 0 ? (double)completedCourses / totalCourses * 100 : 0;

        report.AppendLine($"SUMMARY:");
        report.AppendLine($"Total Terms: {terms.Count}");
        report.AppendLine($"Total Courses: {totalCourses}");
        report.AppendLine($"Completed Courses: {completedCourses}");
        report.AppendLine($"Overall Progress: {overallProgress:F1}%");
        report.AppendLine();

        foreach (var term in terms)
        {
            report.AppendLine($"TERM: {term.Name} ({term.StartDate:yyyy-MM-dd} to {term.EndDate:yyyy-MM-dd})");
            report.AppendLine($"Courses: {term.Courses.Count}");
            report.AppendLine();
        }

        return Encoding.UTF8.GetBytes(report.ToString());
    }

    public async Task<byte[]> GenerateAssessmentReportAsync(string userId)
    {
        var assessments = await _context.Assessments
            .Include(a => a.Course)
                .ThenInclude(c => c.Term)
            .Where(a => a.Course.Term.UserId == userId)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        var report = new StringBuilder();
        report.AppendLine($"ASSESSMENT REPORT");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        var upcomingCount = assessments.Count(a => a.DueDate > DateTime.Now && a.Status != AssessmentStatus.Completed);
        var overdueCount = assessments.Count(a => a.IsOverdue);
        var completedCount = assessments.Count(a => a.Status == AssessmentStatus.Completed);

        report.AppendLine($"SUMMARY:");
        report.AppendLine($"Total Assessments: {assessments.Count}");
        report.AppendLine($"Completed: {completedCount}");
        report.AppendLine($"Upcoming: {upcomingCount}");
        report.AppendLine($"Overdue: {overdueCount}");
        report.AppendLine();

        report.AppendLine("ALL ASSESSMENTS:");
        report.AppendLine("Course\t\tAssessment\t\tType\t\tDue Date\t\tStatus\t\tScore");
        report.AppendLine(new string('-', 80));

        foreach (var assessment in assessments)
        {
            var score = assessment.Score?.ToString("F1") ?? "N/A";
            report.AppendLine($"{assessment.Course.CourseNumber}\t\t{assessment.Name}\t{assessment.Type}\t{assessment.DueDate:yyyy-MM-dd}\t{assessment.Status}\t{score}");
        }

        return Encoding.UTF8.GetBytes(report.ToString());
    }
}
