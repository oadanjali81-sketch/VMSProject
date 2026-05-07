using Microsoft.AspNetCore.Html;
using System.Collections.Generic;

namespace VMSProject.Models
{
    public class UserTableViewModel
    {
        public string? Title { get; set; }
        public string? Icon { get; set; }
        public List<string>? Headers { get; set; }
        public IHtmlContent? TableBody { get; set; }
        public IHtmlContent? HeaderActions { get; set; }
        public bool ShowSearch { get; set; } = true;
        public string SearchPlaceholder { get; set; } = "Search records...";
        public PaginationViewModel? Pagination { get; set; }
    }
}
