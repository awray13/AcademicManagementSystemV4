using AcademicManagementSystemV4.Data;
using AcademicManagementSystemV4.Models;
using Microsoft.EntityFrameworkCore;

namespace AcademicManagementSystemV4.Services;

public interface ICourseTemplateService
{
    Task<List<CourseTemplate>> GetAllTemplatesAsync();
    Task<CourseTemplate?> GetTemplateWithAssessmentsAsync(int templateId);
    Task<Course> CreateCourseFromTemplateAsync(int templateId, int termId, DateTime startDate, DateTime endDate);
}

public class CourseTemplateService : ICourseTemplateService
{
    private readonly ApplicationDbContext _context;

    public CourseTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourseTemplate>> GetAllTemplatesAsync()
    {
        return await _context.CourseTemplates
            .OrderBy(ct => ct.CourseNumber)
            .ToListAsync();
    }

    public async Task<CourseTemplate?> GetTemplateWithAssessmentsAsync(int templateId)
    {
        return await _context.CourseTemplates
            .Include(ct => ct.AssessmentTemplates)
            .FirstOrDefaultAsync(ct => ct.Id == templateId);
    }

    public async Task<Course> CreateCourseFromTemplateAsync(int templateId, int termId, DateTime startDate, DateTime endDate)
    {
        var template = await GetTemplateWithAssessmentsAsync(templateId);
        if (template == null)
            throw new ArgumentException("Course template not found", nameof(templateId));

        var course = new Course
        {
            CourseNumber = template.CourseNumber,
            Title = template.Title,
            Description = template.Description,
            CreditHours = template.CreditHours,
            StartDate = startDate,
            EndDate = endDate,
            Status = CourseStatus.NotStarted,
            TermId = termId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Assessments = new List<Assessment>()
        };

        // Create assessments from templates
        foreach (var assessmentTemplate in template.AssessmentTemplates)
        {
            var dueDate = startDate.AddDays(assessmentTemplate.DaysFromCourseStart);
            
            var assessment = new Assessment
            {
                Name = assessmentTemplate.Name,
                Description = assessmentTemplate.Description,
                Type = assessmentTemplate.Type,
                DueDate = dueDate,
                Status = AssessmentStatus.NotStarted,
                MaxPoints = assessmentTemplate.MaxPoints,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            course.Assessments.Add(assessment);
        }

        return course;
    }
}
