namespace VMSProject.Models
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        public string Action { get; set; } = "Index";
        public string? Controller { get; set; }
        
        // Dictionary for extra route values (search, filters, etc.)
        public System.Collections.Generic.Dictionary<string, string> RouteValues { get; set; } = new();
    }
}
