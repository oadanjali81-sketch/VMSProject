using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using VMSProject.Models;

namespace VMSProject.Controllers
{
    [Route("api/qci")]
    [ApiController]
    [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
    public class QuickCheckinController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QuickCheckinController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/qci/qrimage/{passNumber}  — returns QR as PNG image (server-side, works offline)
        [HttpGet("qrimage/{passNumber}")]
        public IActionResult GetQRImage(string passNumber)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData      = qrGenerator.CreateQrCode(passNumber, QRCodeGenerator.ECCLevel.H);
            using var qrCode      = new PngByteQRCode(qrData);
            byte[] pngBytes = qrCode.GetGraphic(10, new byte[] { 30, 60, 114 }, new byte[] { 255, 255, 255 });
            return File(pngBytes, "image/png");
        }

        // GET /api/qci/employees
        [HttpGet("employees")]
        public IActionResult GetEmployees()
        {
            var list = _context.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.EmployeeId, e.Name, e.Designation,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : null
                }).ToList();
            return Ok(list);
        }

        // GET /api/qci/visitors
        [HttpGet("visitors")]
        public IActionResult GetVisitors()
        {
            var list = _context.Visitors
                .OrderByDescending(v => v.VisitorId)
                .Select(v => new
                {
                    v.VisitorId, v.Name, v.Phone, v.Email,
                    v.CompanyName, v.WhomeToMeet, v.PurposeOfVisit, v.Department,
                    HasQR = !string.IsNullOrEmpty(v.QRCode)
                }).ToList();
            return Ok(list);
        }

        // GET /api/qci/visitordetail/{id}  — full visitor + current visit state
        [HttpGet("visitordetail/{id}")]
        public IActionResult GetVisitorDetail(int id)
        {
            var visitor = _context.Visitors.Find(id);
            if (visitor == null) return NotFound(new { error = "Visitor not found." });

            var existingVisit = _context.Visits
                .Where(v => v.VisitorId == id && (v.Status == "Pending" || v.Status == "Active"))
                .OrderByDescending(v => v.VisitId)
                .FirstOrDefault();

            return Ok(new
            {
                visitor.VisitorId, visitor.Name, visitor.Phone, visitor.Email,
                visitor.CompanyName, visitor.Department, visitor.WhomeToMeet,
                visitor.PurposeOfVisit, visitor.VehicleNumber, visitor.CapturePhoto,
                visitor.QRCode,
                ExistingVisitId    = existingVisit?.VisitId,
                ExistingPassNumber = existingVisit?.PassNumber ?? visitor.QRCode,
                ExistingStatus     = existingVisit?.Status,
                TotalVisits = _context.Visits.Count(v => v.VisitorId == id),
                ActiveCount = _context.Visits.Count(v => v.VisitorId == id && v.Status == "Active"),
                DoneCount   = _context.Visits.Count(v => v.VisitorId == id && v.Status == "CheckedOut")
            });
        }

        // POST /api/qci/generate/{visitorId}  — reuse visitor's permanent QR or create one
        [HttpPost("generate/{visitorId}")]
        public IActionResult GenerateForVisitor(int visitorId)
        {
            var visitor = _context.Visitors.Find(visitorId);
            if (visitor == null) return NotFound(new { error = "Visitor not found." });

            try
            {
                // If visitor already has a permanent QR code, reuse it
                if (string.IsNullOrEmpty(visitor.QRCode))
                {
                    visitor.QRCode = "VIS-" + Guid.NewGuid().ToString("N")[..10].ToUpper();
                    _context.SaveChanges();
                }

                var passNumber = visitor.QRCode;

                // Create a new visit record using the same pass number
                var employee = _context.Employees.FirstOrDefault(e => e.Name == visitor.WhomeToMeet);
                var visit = new Visit
                {
                    VisitorId    = visitor.VisitorId,
                    EmployeeId   = employee?.EmployeeId,
                    Purpose      = visitor.PurposeOfVisit,
                    VisitDate    = DateTime.Now,
                    CheckInTime  = DateTime.Now,
                    CheckOutTime = null,
                    Status       = "Pending",
                    PassNumber   = passNumber,
                    CreatedBy    = 1
                };
                _context.Visits.Add(visit);
                _context.SaveChanges();

                return Ok(new
                {
                    visitId    = visit.VisitId,
                    passNumber = passNumber,
                    name       = visitor.Name,
                    mobile     = visitor.Phone,
                    isReused   = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // POST /api/qci/scan  — check-in (Pending→Active) or check-out (Active→CheckedOut)
        [HttpPost("scan")]
        public IActionResult Scan([FromBody] ScanRequest req)
        {
            var visit = _context.Visits
                .Include(v => v.Visitor)
                .FirstOrDefault(v => v.VisitId == req.VisitId);

            if (visit == null) return NotFound(new { error = "Visit not found." });

            var now = DateTime.Now;

            if (visit.Status == "Pending")
            {
                visit.Status      = "Active";
                visit.CheckInTime = now;
                visit.VisitDate   = now;
                _context.SaveChanges();
                return Ok(new { action = "checkin", name = visit.Visitor?.Name, dateTime = now.ToString("dd MMM yyyy, hh:mm tt"), visitId = visit.VisitId });
            }
            else if (visit.Status == "Active")
            {
                visit.Status       = "CheckedOut";
                visit.CheckOutTime = now;
                _context.SaveChanges();
                return Ok(new { action = "checkout", name = visit.Visitor?.Name, dateTime = now.ToString("dd MMM yyyy, hh:mm tt"), visitId = visit.VisitId });
            }

            return Ok(new { action = "already_done", name = visit.Visitor?.Name, dateTime = visit.CheckOutTime?.ToString("dd MMM yyyy, hh:mm tt"), visitId = visit.VisitId });
        }

        // POST /api/qci/scanpass  — scan by pass number (QR code content)
        [HttpPost("scanpass")]
        public IActionResult ScanByPass([FromBody] ScanPassRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.PassNumber))
                return BadRequest(new { error = "Pass number is required." });

            // Find the most recent active or pending visit with this pass number
            var visit = _context.Visits
                .Include(v => v.Visitor)
                .Include(v => v.Employee)
                .ThenInclude(e => e!.Department)
                .Where(v => v.PassNumber == req.PassNumber && (v.Status == "Pending" || v.Status == "Active"))
                .OrderByDescending(v => v.VisitId)
                .FirstOrDefault();

            // If no active visit found, check if this is a visitor's permanent QR code
            if (visit == null)
            {
                var visitor = _context.Visitors
                    .FirstOrDefault(v => v.QRCode == req.PassNumber);

                if (visitor == null)
                    return NotFound(new { error = "No active visit found for this QR code." });

                // Create a new Pending visit for this visitor using their permanent QR code
                var employee = _context.Employees.FirstOrDefault(e => e.Name == visitor.WhomeToMeet);
                visit = new Visit
                {
                    VisitorId    = visitor.VisitorId,
                    EmployeeId   = employee?.EmployeeId,
                    Purpose      = visitor.PurposeOfVisit,
                    VisitDate    = DateTime.Now,
                    CheckInTime  = DateTime.Now,
                    CheckOutTime = null,
                    Status       = "Pending",
                    PassNumber   = req.PassNumber,
                    CreatedBy    = 1
                };
                _context.Visits.Add(visit);
                _context.SaveChanges();

                // Reload with includes
                visit = _context.Visits
                    .Include(v => v.Visitor)
                    .Include(v => v.Employee)
                    .ThenInclude(e => e!.Department)
                    .FirstOrDefault(v => v.VisitId == visit.VisitId);
            }

            var now = DateTime.Now;

            if (visit == null)
                return NotFound(new { error = "Visit could not be loaded." });

            if (visit.Status == "Pending")
            {
                visit.Status      = "Active";
                visit.CheckInTime = now;
                visit.VisitDate   = now;
                _context.SaveChanges();
                return Ok(new
                {
                    action = "checkin",
                    visitId = visit.VisitId,
                    name = visit.Visitor?.Name,
                    phone = visit.Visitor?.Phone,
                    company = visit.Visitor?.CompanyName,
                    host = visit.Employee?.Name ?? visit.Visitor?.WhomeToMeet,
                    department = visit.Employee?.Department?.DepartmentName,
                    purpose = visit.Purpose,
                    passNumber = visit.PassNumber,
                    dateTime = now.ToString("dd MMM yyyy, hh:mm tt"),
                    status = "Active"
                });
            }
            else if (visit.Status == "Active")
            {
                visit.Status       = "CheckedOut";
                visit.CheckOutTime = now;
                var duration = (int)(now - visit.CheckInTime).TotalMinutes;
                _context.SaveChanges();
                return Ok(new
                {
                    action = "checkout",
                    visitId = visit.VisitId,
                    name = visit.Visitor?.Name,
                    phone = visit.Visitor?.Phone,
                    company = visit.Visitor?.CompanyName,
                    host = visit.Employee?.Name ?? visit.Visitor?.WhomeToMeet,
                    department = visit.Employee?.Department?.DepartmentName,
                    purpose = visit.Purpose,
                    passNumber = visit.PassNumber,
                    checkInTime = visit.CheckInTime.ToString("dd MMM yyyy, hh:mm tt"),
                    checkOutTime = now.ToString("dd MMM yyyy, hh:mm tt"),
                    duration = duration >= 60 ? $"{duration/60}h {duration%60}m" : $"{duration}m",
                    dateTime = now.ToString("dd MMM yyyy, hh:mm tt"),
                    status = "CheckedOut"
                });
            }

            return Ok(new { action = "already_done", name = visit.Visitor?.Name, dateTime = visit.CheckOutTime?.ToString("dd MMM yyyy, hh:mm tt") });
        }

        // POST /api/qci/register  — upsert visitor + create Pending visit
        [HttpPost("register")]
        public IActionResult Register([FromBody] QuickCheckinRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.MobileNumber))
                return BadRequest(new { error = "Name and mobile are required." });

            try
            {
                var visitor = _context.Visitors.FirstOrDefault(v => v.Phone == req.MobileNumber);
                if (visitor == null)
                {
                    visitor = new Visitor
                    {
                        Name = req.FullName, Phone = req.MobileNumber, Email = req.Email,
                        VehicleNumber = req.VehicleNumber, CompanyName = req.CompanyName,
                        WhomeToMeet = req.WhomToMeet, PurposeOfVisit = req.Purpose
                    };
                    _context.Visitors.Add(visitor);
                }
                else
                {
                    visitor.Name = req.FullName;
                    visitor.Email = req.Email ?? visitor.Email;
                    visitor.VehicleNumber = req.VehicleNumber ?? visitor.VehicleNumber;
                    visitor.CompanyName = req.CompanyName ?? visitor.CompanyName;
                    visitor.WhomeToMeet = req.WhomToMeet;
                    visitor.PurposeOfVisit = req.Purpose;
                }
                _context.SaveChanges();

                var employee = _context.Employees.FirstOrDefault(e => e.Name == req.WhomToMeet);
                var visit = new Visit
                {
                    VisitorId = visitor.VisitorId, EmployeeId = employee?.EmployeeId,
                    Purpose = req.Purpose, VisitDate = DateTime.Now, CheckInTime = DateTime.Now,
                    CheckOutTime = null, Status = "Pending",
                    PassNumber = "QR-" + Guid.NewGuid().ToString("N")[..8].ToUpper(), CreatedBy = 1
                };
                _context.Visits.Add(visit);
                _context.SaveChanges();

                return Ok(new
                {
                    visitId = visit.VisitId, visitorId = visitor.VisitorId,
                    passNumber = visit.PassNumber, name = visitor.Name, mobile = visitor.Phone
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // GET /api/qci/history  — Active + CheckedOut visits only (no Pending)
        [HttpGet("history")]
        public IActionResult GetHistory()
        {
            var list = _context.Visits
                .Include(v => v.Visitor)
                .Include(v => v.Employee)
                .Where(v => v.Status != "Pending")
                .OrderByDescending(v => v.CheckInTime)
                .Take(50)
                .Select(v => new
                {
                    v.VisitId,
                    VisitorName  = v.Visitor != null ? v.Visitor.Name : "—",
                    Mobile       = v.Visitor != null ? v.Visitor.Phone : "—",
                    Host         = v.Employee != null ? v.Employee.Name : v.Visitor != null ? v.Visitor.WhomeToMeet : "—",
                    Purpose      = v.Purpose ?? "—",
                    v.PassNumber, v.Status,
                    VisitDate    = v.VisitDate.ToString("dd MMM yyyy"),
                    CheckInTime  = v.CheckInTime.ToString("dd MMM yyyy, hh:mm tt"),
                    CheckOutTime = v.CheckOutTime.HasValue ? v.CheckOutTime.Value.ToString("dd MMM yyyy, hh:mm tt") : null,
                    DurationMins = v.CheckOutTime.HasValue ? (int)(v.CheckOutTime.Value - v.CheckInTime).TotalMinutes : (int?)null
                }).ToList();
            return Ok(list);
        }
    }

    public class QuickCheckinRequest
    {
        public string FullName     { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string? Email       { get; set; }
        public string? VehicleNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? WhomToMeet  { get; set; }
        public string? Purpose     { get; set; }
    }

    public class ScanRequest
    {
        public int VisitId { get; set; }
    }

    public class ScanPassRequest
    {
        public string PassNumber { get; set; } = "";
    }
}
