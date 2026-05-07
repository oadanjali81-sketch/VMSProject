using System;

namespace VMSProject.Models
{
    public class VisitorListViewModel
    {
        public int VisitorId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? WhomeToMeet { get; set; }
        public string? Department { get; set; }
        public string? Purpose { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Status { get; set; } 
        public string? QRCode { get; set; }
        public string? Email { get; set; }
        public string? VehicleNumber { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CapturePhoto { get; set; }
        public string? UploadId { get; set; }
        
        // For View Modal - Host Name (Employee Name)
        public string? HostName { get; set; }

        // For Super Admin - The company they are visiting
        public string? HostCompany { get; set; }
    }
}
