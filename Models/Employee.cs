using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace VMSProject.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [StringLength(100)]
        public string? Designation { get; set; }

        // Stored path in DB
        [StringLength(250)]
        public string? PhotoPath { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public CompanyModel? Company { get; set; }

        // File upload — not stored in DB
        [NotMapped]
        public IFormFile? Photo { get; set; }
    }
}
