using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VMSProject.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public CompanyModel? Company { get; set; }

        [StringLength(100)]
        public string? DepartmentName { get; set; }
    }
}
