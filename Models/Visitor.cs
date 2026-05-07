using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VMSProject.Models
{
    public class Visitor
    {
        [Key]
        public int VisitorId { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? VehicleNumber { get; set; }

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [StringLength(150)]
        public string? CompanyAddress { get; set; }

        [StringLength(150)]
        public string? WhomeToMeet { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? PurposeOfVisit { get; set; }

        [StringLength(250)]
        public string? UploadId { get; set; }

        [StringLength(250)]
        public string? CapturePhoto { get; set; }

        // Permanent QR pass code — generated once, reused for all future visits
        [StringLength(50)]
        public string? QRCode { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public CompanyModel? Company { get; set; }
    }
}
