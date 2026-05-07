namespace VMSProject.Models
{
    public class VisitorPassViewModel
    {
        public Visitor Visitor { get; set; } = new Visitor();
        public string? LatestPassNumber { get; set; }
        public string? HostName { get; set; }
    }
}
