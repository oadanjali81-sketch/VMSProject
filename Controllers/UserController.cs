using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using VMSProject.Models;
using System.Text;

namespace VMSProject.Controllers
{
    public class UserController(ApplicationDbContext context, IWebHostEnvironment env) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName"));

        private string? SaveFile(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0) return null;
            var dir = Path.Combine(_env.WebRootPath, "images", folder);
            Directory.CreateDirectory(dir);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var path = Path.Combine(dir, fileName);
            using var stream = new FileStream(path, FileMode.Create);
            file.CopyTo(stream);
            return $"/images/{folder}/{fileName}";
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            ViewBag.TotalVisitors = _context.Visitors.Count();
            ViewBag.TotalEmployees = _context.Employees.Count();
            ViewBag.TotalDepartments = _context.Departments.Count();
            ViewBag.TodayVisits = _context.Visits.Count(v => v.VisitDate.Date == DateTime.Today);
            ViewBag.ActiveVisits = _context.Visits.Count(v => v.Status == "Active");
            ViewBag.CheckedOutToday = _context.Visits.Count(v => v.Status == "CheckedOut" && v.VisitDate.Date == DateTime.Today);
            ViewBag.PendingVisits = _context.Visits.Count(v => v.Status == "Pending");
            ViewBag.TotalPasses = _context.Visitors.Count(v => !string.IsNullOrEmpty(v.QRCode));

            // Chart Data Generation (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(last7Days.Select(d => d.ToString("ddd")));

            var checkIns = new List<int>();
            var checkOuts = new List<int>();
            foreach (var day in last7Days)
            {
                checkIns.Add(_context.Visits.Count(v => v.VisitDate.Date == day && v.Status != "Pending"));
                checkOuts.Add(_context.Visits.Count(v => v.CheckOutTime.HasValue && v.CheckOutTime.Value.Date == day));
            }
            ViewBag.CheckInData = System.Text.Json.JsonSerializer.Serialize(checkIns);
            ViewBag.CheckOutData = System.Text.Json.JsonSerializer.Serialize(checkOuts);

            ViewBag.RecentVisits = _context.Visits
                .OrderByDescending(v => v.VisitDate)
                .Take(5)
                .Select(v => new {
                    v.VisitId,
                    VisitorName = v.Visitor != null ? v.Visitor.Name : "Unknown",
                    EmployeeName = v.Employee != null ? v.Employee.Name : "General",
                    v.VisitDate,
                    v.Status
                }).ToList();

            return View();
        }

        // ================= DEPARTMENTS =================
        public IActionResult Department(string? search, int page = 1)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            var query = _context.Departments.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(d => d.DepartmentName.Contains(search));

            int pageSize = 10;
            var total = query.Count();
            var items = query.OrderBy(d => d.DepartmentName).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.EmpCounts = _context.Employees.GroupBy(e => e.DepartmentId).ToDictionary(g => g.Key, g => g.Count());
            ViewBag.Pagination = new PaginationViewModel { CurrentPage = page, TotalItems = total, PageSize = pageSize, Action = "Department", Controller = "User" };

            return View(items);
        }

        [HttpPost]
        public IActionResult AddDepartment(Department d)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(d.DepartmentName)) return RedirectToAction("Department");
            _context.Departments.Add(d);
            _context.SaveChanges();
            TempData["Success"] = "Department created successfully!";
            return RedirectToAction("Department");
        }

        [HttpPost]
        public IActionResult EditDepartment(Department d)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var existing = _context.Departments.Find(d.DepartmentId);
            if (existing != null)
            {
                existing.DepartmentName = d.DepartmentName;
                _context.SaveChanges();
                TempData["Success"] = "Department updated successfully!";
            }
            return RedirectToAction("Department");
        }

        [HttpGet]
        public IActionResult GetDepartment(int id)
        {
            var d = _context.Departments.Find(id);
            if (d == null) return NotFound();
            var count = _context.Employees.Count(e => e.DepartmentId == id);
            return Json(new { id = d.DepartmentId, name = d.DepartmentName, employeeCount = count });
        }

        // ================= EMPLOYEES =================
        public IActionResult Employee(string? search, int page = 1)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            var query = _context.Employees.Include(e => e.Department).AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(e => e.Name.Contains(search) || e.Email.Contains(search));

            int pageSize = 10;
            var total = query.Count();
            var items = query.OrderByDescending(e => e.EmployeeId).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Departments = _context.Departments.OrderBy(d => d.DepartmentName).ToList();
            ViewBag.Pagination = new PaginationViewModel { CurrentPage = page, TotalItems = total, PageSize = pageSize, Action = "Employee", Controller = "User" };

            return View(items);
        }

        [HttpPost]
        public IActionResult AddEmployee(Employee e, IFormFile? Photo)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            e.PhotoPath = SaveFile(Photo, "employees");
            _context.Employees.Add(e);
            _context.SaveChanges();
            TempData["Success"] = "Employee added successfully!";
            return RedirectToAction("Employee");
        }

        [HttpPost]
        public IActionResult EditEmployee(Employee e, IFormFile? Photo)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var existing = _context.Employees.Find(e.EmployeeId);
            if (existing != null)
            {
                existing.Name = e.Name;
                existing.Email = e.Email;
                existing.Phone = e.Phone;
                existing.Designation = e.Designation;
                existing.DepartmentId = e.DepartmentId;
                if (Photo != null) existing.PhotoPath = SaveFile(Photo, "employees");
                _context.SaveChanges();
                TempData["Success"] = "Employee updated successfully!";
            }
            return RedirectToAction("Employee");
        }

        [HttpGet]
        public IActionResult GetEmployee(int id)
        {
            var e = _context.Employees.Include(d => d.Department).FirstOrDefault(x => x.EmployeeId == id);
            if (e == null) return NotFound();
            return Json(new
            {
                id = e.EmployeeId,
                name = e.Name,
                email = e.Email,
                phone = e.Phone,
                designation = e.Designation,
                departmentId = e.DepartmentId,
                departmentName = e.Department?.DepartmentName,
                photoPath = e.PhotoPath
            });
        }

        // ================= VISIT LOGS =================
        public IActionResult Visit(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            var query = _context.Visits.Include(v => v.Visitor).Include(v => v.Employee).ThenInclude(e => e.Department).AsQueryable();

            if (!string.IsNullOrEmpty(search)) query = query.Where(v => v.Visitor.Name.Contains(search) || v.Visitor.Phone.Contains(search));
            if (!string.IsNullOrEmpty(status)) query = query.Where(v => v.Status == status);
            if (dateFrom.HasValue) query = query.Where(v => v.VisitDate.Date >= dateFrom.Value.Date);
            if (dateTo.HasValue) query = query.Where(v => v.VisitDate.Date <= dateTo.Value.Date);

            int pageSize = 15;
            var total = query.Count();
            var items = query.OrderByDescending(v => v.VisitId).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalVisits = _context.Visits.Count();
            ViewBag.ActiveVisits = _context.Visits.Count(v => v.Status == "Active");
            ViewBag.PendingVisits = _context.Visits.Count(v => v.Status == "Pending");
            ViewBag.Search = search;
            ViewBag.StatusFilter = status;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = total,
                PageSize = pageSize,
                Action = "Visit",
                Controller = "User",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search ?? "",
                    ["status"] = status ?? "",
                    ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd") ?? "",
                    ["dateTo"] = dateTo?.ToString("yyyy-MM-dd") ?? ""
                }
            };

            return View(items);
        }

        public IActionResult VisitDetail(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var visit = _context.Visits.Include(v => v.Visitor).Include(v => v.Employee).FirstOrDefault(v => v.VisitId == id);
            if (visit == null) return NotFound();
            ViewBag.VisitorHistory = _context.Visits.Include(v => v.Employee).Where(v => v.VisitorId == visit.VisitorId).OrderByDescending(v => v.VisitDate).ToList();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View(visit);
        }

        public IActionResult VisitReport(DateTime? dateFrom, DateTime? dateTo)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var from = dateFrom ?? DateTime.Today;
            var to = dateTo ?? DateTime.Today;

            var visits = _context.Visits.Include(v => v.Visitor).Include(v => v.Employee).ThenInclude(e => e.Department)
                        .Where(v => v.VisitDate.Date >= from.Date && v.VisitDate.Date <= to.Date).ToList();

            ViewBag.TotalInRange = visits.Count;
            ViewBag.CheckedIn = visits.Count(v => v.Status != "Pending");
            ViewBag.CheckedOut = visits.Count(v => v.Status == "CheckedOut");

            var completed = visits.Where(v => v.CheckOutTime.HasValue).ToList();
            ViewBag.AvgDurationMins = completed.Count > 0 ? completed.Average(v => (v.CheckOutTime!.Value - v.CheckInTime).TotalMinutes) : 0;

            ViewBag.GroupedVisits = visits.GroupBy(v => v.VisitDate.Date).OrderByDescending(g => g.Key).ToList();
            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.ToString("yyyy-MM-dd");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            return View(visits);
        }

        // ================= VISITORS =================
        public IActionResult VisitorManagement(string? search, int page = 1)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            var query = _context.Visitors.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(v => v.Name.Contains(search) || v.Phone.Contains(search) || v.CompanyName.Contains(search));

            int pageSize = 10;
            var total = query.Count();
            var items = query.OrderByDescending(v => v.VisitorId).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(v => new VisitorListViewModel
                {
                    VisitorId = v.VisitorId,
                    Name = v.Name,
                    Phone = v.Phone,
                    Email = v.Email,
                    CompanyName = v.CompanyName,
                    WhomeToMeet = v.WhomeToMeet,
                    Department = v.Department,
                    Purpose = v.PurposeOfVisit,
                    VehicleNumber = v.VehicleNumber,
                    QRCode = v.QRCode,
                    CapturePhoto = v.CapturePhoto
                }).ToList();

            ViewBag.TotalVisitors = total;
            ViewBag.Pagination = new PaginationViewModel { CurrentPage = page, TotalItems = total, PageSize = pageSize, Action = "VisitorManagement", Controller = "User" };
            ViewBag.Employees = _context.Employees.OrderBy(e => e.Name).ToList();
            ViewBag.Departments = _context.Departments.OrderBy(d => d.DepartmentName).ToList();

            return View("VisitorManagement", items);
        }

        [HttpPost]
        public IActionResult AddVisitor(Visitor v, IFormFile? PhotoFile)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            v.CapturePhoto = SaveFile(PhotoFile, "visitors");
            _context.Visitors.Add(v);
            _context.SaveChanges();
            TempData["Success"] = "Visitor registered successfully!";
            return RedirectToAction("VisitorManagement");
        }

        [HttpPost]
        public IActionResult EditVisitor(Visitor v)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var existing = _context.Visitors.Find(v.VisitorId);
            if (existing != null)
            {
                existing.Name = v.Name;
                existing.Email = v.Email;
                existing.Phone = v.Phone;
                existing.CompanyName = v.CompanyName;
                existing.WhomeToMeet = v.WhomeToMeet;
                existing.Department = v.Department;
                existing.PurposeOfVisit = v.PurposeOfVisit;
                existing.VehicleNumber = v.VehicleNumber;
                existing.CompanyAddress = v.CompanyAddress;
                _context.SaveChanges();
                TempData["Success"] = "Visitor profile updated!";
            }
            return RedirectToAction("VisitorManagement");
        }

        [HttpPost]
        public IActionResult DeleteVisitor(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var visitor = _context.Visitors.Find(id);
            if (visitor != null)
            {
                _context.Visitors.Remove(visitor);
                _context.SaveChanges();
                TempData["Success"] = "Visitor record deleted!";
            }
            return RedirectToAction("VisitorManagement");
        }

        public IActionResult GenerateQR(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var visitor = _context.Visitors.Find(id);
            if (visitor == null) return NotFound();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View("GenerateQR", visitor);
        }

        [HttpPost]
        public IActionResult GenerateVisitorQR(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var visitor = _context.Visitors.Find(id);
            if (visitor != null && string.IsNullOrEmpty(visitor.QRCode))
            {
                visitor.QRCode = "VIS-" + Guid.NewGuid().ToString("N")[..10].ToUpper();
                _context.SaveChanges();
                TempData["Success"] = "New security pass generated!";
            }
            return RedirectToAction("GenerateQR", new { id = id });
        }

        // ================= DELETE ACTIONS =================
        [HttpPost]
        public IActionResult DeleteDepartment(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var d = _context.Departments.Find(id);
            if (d != null)
            {
                _context.Departments.Remove(d);
                _context.SaveChanges();
                TempData["Success"] = "Department deleted!";
            }
            return RedirectToAction("Department");
        }

        [HttpPost]
        public IActionResult DeleteEmployee(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var e = _context.Employees.Find(id);
            if (e != null)
            {
                _context.Employees.Remove(e);
                _context.SaveChanges();
                TempData["Success"] = "Employee record removed!";
            }
            return RedirectToAction("Employee");
        }

        [HttpPost]
        public IActionResult DeleteVisit(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            var v = _context.Visits.Find(id);
            if (v != null)
            {
                _context.Visits.Remove(v);
                _context.SaveChanges();
                TempData["Success"] = "Visit record deleted!";
            }
            return RedirectToAction("Visit");
        }

        public IActionResult AllQRCodes(string? search, int page = 1, int pageSize = 12)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");

            var query = _context.Visitors.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(v => v.Name.Contains(search) || v.QRCode.Contains(search));

            var result = query.OrderByDescending(v => v.VisitorId).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(v => new VisitorPassViewModel { Visitor = v, LatestPassNumber = v.QRCode }).ToList();

            ViewBag.TotalWithQR = query.Count(v => !string.IsNullOrEmpty(v.QRCode));
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Pagination = new PaginationViewModel { CurrentPage = page, TotalItems = query.Count(), PageSize = pageSize, Action = "AllQRCodes", Controller = "User" };

            return View(result);
        }

        public IActionResult ScanQR()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName");
            return View();
        }

        // ================= AJAX / API =================
        [HttpGet]
        public IActionResult GetVisitorHistory(int id)
        {
            var history = _context.Visits.Where(v => v.VisitorId == id).OrderByDescending(v => v.VisitDate).Select(v => new {
                checkIn = v.VisitDate.ToString("dd MMM yyyy, hh:mm tt"),
                checkOut = v.CheckOutTime.HasValue ? v.CheckOutTime.Value.ToString("hh:mm tt") : "--",
                status = v.Status == "Active" ? "Inside" : (v.Status == "CheckedOut" ? "Checked Out" : v.Status)
            }).ToList();
            return Json(history);
        }

        [HttpPost]
        public IActionResult ProcessQR(string code)
        {
            if (string.IsNullOrEmpty(code)) return Json(new { success = false, message = "Invalid code" });

            // 1. Check for an Active visit (to check out)
            var activeVisit = _context.Visits.Include(v => v.Visitor)
                .FirstOrDefault(v => v.PassNumber == code && v.Status == "Active");

            if (activeVisit != null)
            {
                activeVisit.CheckOutTime = DateTime.Now;
                activeVisit.Status = "CheckedOut";
                _context.SaveChanges();
                return Json(new { success = true, message = "Checked Out Successfully", visitor = activeVisit.Visitor?.Name });
            }

            // 2. Check for a Pending visit (to check in)
            var pendingVisit = _context.Visits.Include(v => v.Visitor)
                .FirstOrDefault(v => v.PassNumber == code && v.Status == "Pending");

            if (pendingVisit != null)
            {
                pendingVisit.CheckInTime = DateTime.Now;
                pendingVisit.Status = "Active";
                _context.SaveChanges();
                return Json(new { success = true, message = "Checked In Successfully", visitor = pendingVisit.Visitor?.Name });
            }

            // 3. Check if this is a Visitor's permanent QR code (create new visit)
            var visitor = _context.Visitors.FirstOrDefault(v => v.QRCode == code);
            if (visitor != null)
            {
                // Auto-create an active visit for this permanent pass holder
                var employee = _context.Employees.FirstOrDefault(e => e.Name == visitor.WhomeToMeet);
                var newVisit = new Visit
                {
                    VisitorId = visitor.VisitorId,
                    EmployeeId = employee?.EmployeeId,
                    Purpose = visitor.PurposeOfVisit,
                    VisitDate = DateTime.Now,
                    CheckInTime = DateTime.Now,
                    Status = "Active",
                    PassNumber = code,
                    CreatedBy = 1 // Default system admin
                };
                _context.Visits.Add(newVisit);
                _context.SaveChanges();
                return Json(new { success = true, message = "Checked In Successfully (Auto-entry)", visitor = visitor.Name });
            }

            return Json(new { success = false, message = "Invalid or expired pass" });
        }
    }
}
