using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VMSProject.Models
{
    public class UserActivityLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        
        public int? CompanyId { get; set; }
        
        [ForeignKey("CompanyId")]
        public virtual CompanyModel? Company { get; set; }

        public string? Action { get; set; } // Login, Logout, Module Access, etc.
        public string? Module { get; set; } // Visitor Management, Employee Management, etc.
        public string? IPAddress { get; set; }
        public string? Status { get; set; } // Success, Failed
        public string? Details { get; set; }
        public string? SessionDuration { get; set; }
    }
}
