using Microsoft.EntityFrameworkCore;

namespace VMSProject.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CompanyModel> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<CompanyRegistrationRequest> CompanyRegistrationRequests { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<DemoRequest> DemoRequests { get; set; }
    }
}
