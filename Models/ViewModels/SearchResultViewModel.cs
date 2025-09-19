namespace AcademicManagementSystemV4.Models.ViewModels;

public class SearchResultViewModel
{
    public string Query { get; set; } = string.Empty;
    public List<SearchResultItem> Results { get; set; } = new();
    public int TotalResults => Results.Count;
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Term, Course, Assessment
    public string Url { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}