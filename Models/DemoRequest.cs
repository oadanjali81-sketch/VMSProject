using System;
using System.ComponentModel.DataAnnotations;

namespace VMSProject.Models
{
    public class DemoRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ContactedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New"; // New, Contacted, Completed
    }
}
