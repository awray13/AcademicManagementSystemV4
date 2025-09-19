namespace AcademicManagementSystemV4.Services;

public interface IReportService
{
    Task<byte[]> GenerateTermReportAsync(int termId, string userId);
    Task<byte[]> GenerateStudentProgressReportAsync(string userId);
    Task<byte[]> GenerateAssessmentReportAsync(string userId);
}
