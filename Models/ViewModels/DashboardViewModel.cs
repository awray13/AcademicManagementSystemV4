namespace AcademicManagementSystemV4.Models.ViewModels;

public class DashboardViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<Term> ActiveTerms { get; set; } = new();
    public List<Assessment> UpcomingAssessments { get; set; } = new();
    public List<Assessment> OverdueAssessments { get; set; } = new();
    public List<Course> InProgressCourses { get; set; } = new();

    // Statistics
    public int TotalTerms { get; set; }
    public int TotalCourses { get; set; }
    public int TotalAssessments { get; set; }
    public double OverallCompletionRate { get; set; }
}
