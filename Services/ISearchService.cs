using AcademicManagementSystemV4.Models.ViewModels;

namespace AcademicManagementSystemV4.Services
{
    public interface ISearchService
    {
        Task<SearchResultViewModel> SearchAsync(string query, string userId);
    }
}
