using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VMSProject.Models
{
    public class Visit
    {
        [Key]
        public int VisitId { get; set; }

        public int VisitorId { get; set; }

        [ForeignKey("VisitorId")]
        public Visitor? Visitor { get; set; }

        public int? EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [StringLength(200)]
        public string? Purpose { get; set; }

        public DateTime VisitDate { get; set; }

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(50)]
        public string? PassNumber { get; set; }

        public int CreatedBy { get; set; }
        
        [ForeignKey("CreatedBy")]
        public CompanyModel? CreatedByCompany { get; set; }
    }
}
