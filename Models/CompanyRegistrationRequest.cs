using System;
using System.ComponentModel.DataAnnotations;

namespace VMSProject.Models
{
    public class CompanyRegistrationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Person Name is required")]
        [StringLength(100)]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(150)]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        public string? CompanyAddress { get; set; }

        // New fields to capture full registration details
        public string? Industry { get; set; }
        public string? GSTNumber { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public string? AdminNotes { get; set; }
    }
}
