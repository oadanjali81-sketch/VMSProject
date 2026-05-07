using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace VMSProject.Models
{
    public class CompanyModel
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        public string? CompanyName { get; set; }

        [Required]
        public string? Industry { get; set; }

        public string? LogoPath { get; set; }

        [NotMapped]
        public IFormFile? CompanyLogo { get; set; }

        [Required]
        public string? GSTNumber { get; set; }

        [Required]
        public string? Address { get; set; }

        [Required]
        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        [Required]
        public string? Pincode { get; set; }

        public string? OfficialEmail { get; set; }

        public string? ContactNumber { get; set; }

        public string? LandlineNumber { get; set; }

        [Required]
        public string? AdminName { get; set; }

        [Required]
        public string? AdminMobile { get; set; }

        [Required]
        public string? AdminEmail { get; set; }

        [Required]
        public string? Password { get; set; }

        public string Role { get; set; } = "Receptionist";

        public bool IsActive { get; set; } = true;

        [NotMapped]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        [Compare("Password", ErrorMessage = "Password does not match")]
        public string? ConfirmPassword { get; set; }
    }
}
