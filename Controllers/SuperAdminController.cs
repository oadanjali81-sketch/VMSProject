using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using VMSProject.Models;

namespace VMSProject.Controllers
{
    public class SuperAdminController(VMSProject.Models.ApplicationDbContext context) : Controller
    {
        private readonly VMSProject.Models.ApplicationDbContext _context = context;

        private bool IsSuperAdmin()
        {
            var role = (HttpContext.Session.GetString("UserRole") ?? string.Empty).Trim();
            return string.Equals(role, "Superadmin", System.StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult Dashboard()
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Super Admin Dashboard";

            // Header Persona
            ViewBag.AdminName = HttpContext.Session.GetString("AdminName") ?? "Super Admin";

            // ===== DASHBOARD STATS (ALL 8 CARDS - REAL VMS DATA) =====
            // Row 1
            ViewBag.TotalCompanies = _context.Companies.Count();
            ViewBag.ActiveCompanies = _context.Companies.Count(c => c.IsActive);
            ViewBag.TotalVisitors = _context.Visitors.Count();
            ViewBag.VisitsToday = _context.Visits.Count(v => v.VisitDate.Date == DateTime.Today);
            // Row 2
            ViewBag.ActiveInside = _context.Visits.Count(v => v.Status == "Active");
            ViewBag.CheckedOutToday = _context.Visits.Count(v => v.Status == "Checked Out" && v.VisitDate.Date == DateTime.Today);
            ViewBag.TotalEmployees = _context.Employees.Count();
            ViewBag.TotalDepartments = _context.Departments.Count();

            // ===== DATA FOR TABLES =====
            var recentCompanies = _context.Companies
                .OrderByDescending(c => c.CompanyId)
                .Take(5)
                .Select(c => new
                {
                    id = c.CompanyId,
                    name = !string.IsNullOrEmpty(c.CompanyName) ? c.CompanyName : (!string.IsNullOrEmpty(c.AdminName) ? c.AdminName : "System"),
                    email = c.AdminEmail ?? "—",
                    status = c.IsActive ? "Active" : "Inactive",
                    role = c.Role ?? "N/A"
                }).ToList();
            ViewBag.RecentCompaniesJson = System.Text.Json.JsonSerializer.Serialize(recentCompanies);

            var recentVisits = _context.Visits
                .OrderByDescending(v => v.CheckInTime)
                .Take(5)
                .Select(v => new
                {
                    user = v.Visitor != null && v.Visitor.Name != null ? v.Visitor.Name : "Unknown",
                    ip = v.Purpose != null ? v.Purpose : "Visit",
                    status = v.Status != null ? v.Status : "N/A",
                    checkInTime = v.CheckInTime
                })
                .ToList()
                .Select(v => new
                {
                    user = v.user,
                    ip = v.ip,
                    status = v.status,
                    date = v.checkInTime.ToString("MMM dd, yyyy HH:mm")
                }).ToList();
            ViewBag.RecentActivityJson = System.Text.Json.JsonSerializer.Serialize(recentVisits);

            // Chart Data: Last 7 days check-ins
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-i)).Reverse().ToList();
            var checkInData = last7Days.Select(d => _context.Visits.Count(v => v.VisitDate.Date == d.Date)).ToList();

            // Chart Data: Companies Regional Distribution (Real Data to replace mock onboard timeline)
            var regionalData = _context.Companies
                .Where(c => !string.IsNullOrEmpty(c.City))
                .GroupBy(c => c.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(4)
                .ToList();

            // Fallback if no real cities exist yet
            if (regionalData.Count == 0)
            {
                regionalData.Add(new { City = (string?)"Local Region", Count = _context.Companies.Count() });
            }

            // Chart Data: Company Status (Active vs Inactive)
            var activeCount = _context.Companies.Count(c => c.IsActive);
            var inactiveCount = _context.Companies.Count(c => !c.IsActive);

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(last7Days.Select(d => d.ToString("ddd")).ToList());
            ViewBag.CheckInData = System.Text.Json.JsonSerializer.Serialize(checkInData);
            ViewBag.GrowthLabels = System.Text.Json.JsonSerializer.Serialize(regionalData.Select(d => d.City));
            ViewBag.GrowthData = System.Text.Json.JsonSerializer.Serialize(regionalData.Select(d => d.Count));
            ViewBag.ActivePieCount = activeCount;
            ViewBag.InactivePieCount = inactiveCount;

            return View();
        }


        public IActionResult AllUsers(string? search, string? status)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            var query = _context.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => (c.CompanyName != null && c.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                                         (c.AdminEmail != null && c.AdminEmail.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                                         (c.AdminName != null && c.AdminName.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool active = status == "Active";
                query = query.Where(c => c.IsActive == active);
            }

            var companies = query.OrderByDescending(c => c.CompanyId).ToList();

            // Try to find "real" dates for companies that have 0001-01-01
            foreach (var company in companies)
            {
                if (company.CreatedDate == DateTime.MinValue)
                {
                    // Fallback 1: Registration Request
                    var request = _context.CompanyRegistrationRequests
                        .FirstOrDefault(r => r.ContactEmail == company.AdminEmail && r.Status == "Approved");
                    
                    if (request != null)
                    {
                        company.CreatedDate = request.ReviewedDate ?? request.RequestDate;
                    }
                    else
                    {
                        // Fallback 2: Earliest Activity Log
                        var firstLog = _context.UserActivityLogs
                            .Where(l => l.CompanyId == company.CompanyId)
                            .OrderBy(l => l.Timestamp)
                            .FirstOrDefault();
                        if (firstLog != null)
                        {
                            company.CreatedDate = firstLog.Timestamp;
                        }
                    }
                }
            }

            ViewBag.TotalUsers = _context.Companies.Count();
            ViewBag.ActiveUsers = _context.Companies.Count(c => c.IsActive);
            ViewBag.InactiveUsers = _context.Companies.Count(c => !c.IsActive);
            
            // Calculate New Users (e.g., registered in the last 7 days)
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            ViewBag.NewUsers = companies.Count(c => c.CreatedDate >= sevenDaysAgo);

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(companies);
        }

        public IActionResult UserDetail(int id)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");

            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            if (company == null) return NotFound();

            // Try to find "real" date for company if it's 0001-01-01
            if (company.CreatedDate == DateTime.MinValue)
            {
                var request = _context.CompanyRegistrationRequests
                    .FirstOrDefault(r => r.ContactEmail == company.AdminEmail && r.Status == "Approved");
                
                if (request != null)
                {
                    company.CreatedDate = request.ReviewedDate ?? request.RequestDate;
                }
                else
                {
                    var firstLog = _context.UserActivityLogs
                        .Where(l => l.CompanyId == company.CompanyId)
                        .OrderBy(l => l.Timestamp)
                        .FirstOrDefault();
                    if (firstLog != null)
                    {
                        company.CreatedDate = firstLog.Timestamp;
                    }
                }
            }

            // Fetch real statistics for this company
            ViewBag.TotalVisitors = _context.Visitors.Count(v => v.CompanyId == id);
            ViewBag.TotalEmployees = _context.Employees.Count(e => e.CompanyId == id);
            ViewBag.TotalDepartments = _context.Departments.Count(d => d.CompanyId == id);
            
            var logsQuery = _context.UserActivityLogs.Where(l => l.CompanyId == id);
            ViewBag.RecentActivityCount = logsQuery.Count();
            
            // Get most recent 5 activities
            ViewBag.RecentLogs = logsQuery.OrderByDescending(l => l.Timestamp).Take(5).ToList();

            return View(company);
        }

        public IActionResult UserManagement(string? search, string? role, string? status, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "User Management";

            var query = _context.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => (c.AdminName != null && c.AdminName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                                         (c.AdminEmail != null && c.AdminEmail.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(c => c.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool active = status == "Active";
                query = query.Where(c => c.IsActive == active);
            }

            int pageSize = 50;
            var list = query.OrderByDescending(c => c.CompanyId).ToList();

            // Try to find "real" dates for companies that have 0001-01-01
            foreach (var company in list)
            {
                if (company.CreatedDate == DateTime.MinValue)
                {
                    var request = _context.CompanyRegistrationRequests
                        .FirstOrDefault(r => r.ContactEmail == company.AdminEmail && r.Status == "Approved");
                    
                    if (request != null)
                    {
                        company.CreatedDate = request.ReviewedDate ?? request.RequestDate;
                    }
                    else
                    {
                        var firstLog = _context.UserActivityLogs
                            .Where(l => l.CompanyId == company.CompanyId)
                            .OrderBy(l => l.Timestamp)
                            .FirstOrDefault();
                        if (firstLog != null)
                        {
                            company.CreatedDate = firstLog.Timestamp;
                        }
                    }
                }
            }

            var totalItems = list.Count;
            var paginated = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = new VMSProject.Models.PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize,
                Action = "UserManagement",
                Controller = "SuperAdmin",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["role"] = role ?? string.Empty,
                    ["status"] = status ?? string.Empty
                }
            };

            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            ViewBag.SelectedStatus = status;

            return View(paginated);
        }

        [HttpPost]
        public IActionResult UpdateUser(int id, string adminName, string adminEmail, string role, bool isActive)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");

            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            if (company != null)
            {
                company.AdminName = adminName;
                company.AdminEmail = adminEmail;
                company.Role = role;
                company.IsActive = isActive;
                _context.SaveChanges();
            }
            return RedirectToAction("UserManagement");
        }

        public IActionResult VisitorManagement(string? search, string? status, DateTime? date, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Visitor Management";

            var query = from v in _context.Visitors
                        let latestVisit = _context.Visits
                            .Where(vis => vis.VisitorId == v.VisitorId)
                            .OrderByDescending(vis => vis.CheckInTime)
                            .FirstOrDefault()
                        let hostCompany = _context.Companies
                            .Where(c => latestVisit != null && c.CompanyId == latestVisit.CreatedBy)
                            .Select(c => c.CompanyName)
                            .FirstOrDefault()
                        select new VMSProject.Models.VisitorListViewModel
                        {
                            VisitorId = v.VisitorId,
                            Name = v.Name ?? "Unnamed",
                            Phone = v.Phone ?? "N/A",
                            CompanyName = v.CompanyName ?? "Guest",
                            WhomeToMeet = v.WhomeToMeet ?? "N/A",
                            Department = v.Department ?? "N/A",
                            Purpose = latestVisit != null ? latestVisit.Purpose : v.PurposeOfVisit ?? "Visit",
                            CheckInTime = latestVisit != null ? (DateTime?)latestVisit.CheckInTime : null,
                            CheckOutTime = latestVisit != null ? latestVisit.CheckOutTime : null,
                            Status = latestVisit != null ? latestVisit.Status : "Registered",
                            QRCode = v.QRCode,
                            Email = v.Email,
                            VehicleNumber = v.VehicleNumber,
                            CompanyAddress = v.CompanyAddress,
                            CapturePhoto = v.CapturePhoto,
                            HostName = latestVisit != null && latestVisit.Employee != null ? latestVisit.Employee.Name : v.WhomeToMeet ?? "N/A",
                            HostCompany = hostCompany ?? "N/A"
                        };

            // Filtering
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => (v.Name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) || (v.Phone ?? string.Empty).Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(v => v.Status == status);
            }

            if (date.HasValue)
            {
                query = query.Where(v => v.CheckInTime.HasValue && v.CheckInTime.Value.Date == date.Value.Date);
            }

            int pageSize = 50;
            var list = query.OrderByDescending(v => v.VisitorId).ToList();
            var totalItems = list.Count;
            var paginated = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Pagination = new VMSProject.Models.PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize,
                Action = "VisitorManagement",
                Controller = "SuperAdmin",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty,
                    ["date"] = date?.ToString("yyyy-MM-dd") ?? string.Empty
                }
            };

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(paginated);
        }

        public IActionResult Reports(string? type, string? from, string? to, string? search, int? companyId, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Reports & Analytics";

            DateTime fromDate = string.IsNullOrEmpty(from) ? DateTime.Today.AddMonths(-1) : DateTime.Parse(from);
            DateTime toDate = string.IsNullOrEmpty(to) ? DateTime.Today : DateTime.Parse(to);

            // 1. Base Query for Filtering
            var query = _context.Visits.Include(v => v.Visitor).Include(v => v.Employee).AsQueryable();

            if (type == "History")
            {
                // No date filter for full history
            }
            else if (type == "Daily")
            {
                query = query.Where(v => v.VisitDate.Date == DateTime.Today);
            }
            else if (type == "Weekly")
            {
                query = query.Where(v => v.VisitDate >= DateTime.Today.AddDays(-7));
            }
            else if (type == "Monthly")
            {
                query = query.Where(v => v.VisitDate.Month == DateTime.Today.Month && v.VisitDate.Year == DateTime.Today.Year);
            }
            else
            {
                query = query.Where(v => v.VisitDate.Date >= fromDate.Date && v.VisitDate.Date <= toDate.Date);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Visitor != null && ((v.Visitor.Name != null && v.Visitor.Name.Contains(search)) || (v.Visitor.Phone != null && v.Visitor.Phone.Contains(search))));
            }

            if (companyId.HasValue && companyId > 0)
            {
                query = query.Where(v => v.CreatedBy == companyId.Value);
            }

            // 2. Summary Cards Data (DYNAMIC BASED ON FILTERS)
            ViewBag.TotalVisitorsReport = query.Select(v => v.VisitorId).Distinct().Count();
            ViewBag.InsideNow = query.Count(v => v.Status == "Active");
            ViewBag.CompletedVisits = query.Count(v => v.Status == "Checked Out" || v.Status == "Completed" || v.Status == "Done" || v.Status == "Checkout" || v.Status == "Checked-Out" || v.Status == "CheckedOut");
            ViewBag.PendingVisits = query.Count(v => v.Status == "Registered" || v.Status == "Pending" || v.Status == "Scheduled");

            // 3. ANALYTICS CHARTS DATA (FILTERED)
            var filteredVisitsForCharts = query.ToList();

            // A. Weekly Visit Trend (Last 7 Days)
            var last7DaysTrend = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();
            var trendData = last7DaysTrend.Select(d => filteredVisitsForCharts.Count(v => v.VisitDate.Date == d.Date)).ToList();
            var trendLabels = last7DaysTrend.Select(d => d.ToString("dd MMM")).ToList();

            ViewBag.TrendLabels = System.Text.Json.JsonSerializer.Serialize(trendLabels);
            ViewBag.TrendValues = System.Text.Json.JsonSerializer.Serialize(trendData);

            // B. Status Mix (Filtered)
            var statusMix = filteredVisitsForCharts.GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }).ToList();
            ViewBag.StatusLabels = System.Text.Json.JsonSerializer.Serialize(statusMix.Select(s => s.Status).ToList());
            ViewBag.StatusValues = System.Text.Json.JsonSerializer.Serialize(statusMix.Select(s => s.Count).ToList());

            // Pagination Logic
            int pageSize = 10;
            var totalItems = query.Count();
            var paginatedQuery = query.OrderByDescending(v => v.VisitDate)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize);
            
            var reportData = paginatedQuery.ToList();

            ViewBag.Pagination = new VMSProject.Models.PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize,
                Action = "Reports",
                Controller = "SuperAdmin",
                RouteValues = new Dictionary<string, string>
                {
                    ["type"] = type ?? "",
                    ["from"] = from ?? "",
                    ["to"] = to ?? "",
                    ["search"] = search ?? "",
                    ["companyId"] = companyId?.ToString() ?? ""
                }
            };

            // 3. Analytics Chart Data (DYNAMIC BASED ON FILTERS)
            var filteredVisits = query.ToList(); // Materialize for grouping in memory if needed, but better to use the query
            
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var barData = last7Days.Select(d => new
            {
                Label = d.ToString("dd MMM"),
                Count = filteredVisits.Count(v => v.VisitDate.Date == d.Date)
            }).ToList();

            ViewBag.ChartLabels = barData.Select(b => b.Label).ToList();
            ViewBag.ChartValues = barData.Select(b => b.Count).ToList();

            // 4. Pie Chart Data (DYNAMIC BASED ON FILTERS)
            var pieData = filteredVisits.GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }).ToList();

            ViewBag.PieLabels = pieData.Select(p => p.Status).ToList();
            ViewBag.PieValues = pieData.Select(p => p.Count).ToList();

            ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
            ViewBag.ReportType = type ?? "Daily";
            ViewBag.SearchTerm = search;
            ViewBag.SelectedCompanyId = companyId;

            // Fetch Companies for Dropdown
            ViewBag.Companies = _context.Companies.OrderBy(c => c.CompanyName).ToList();

            return View(reportData);
        }

        public IActionResult SystemUsage(string? from, string? to, int? companyId, string? role, string? actionType, string? status, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "System Usage Analytics";

            DateTime fromDate = string.IsNullOrEmpty(from) ? DateTime.Today.AddMonths(-1) : DateTime.Parse(from);
            DateTime toDate = string.IsNullOrEmpty(to) ? DateTime.Today : DateTime.Parse(to);

            var query = _context.UserActivityLogs.Include(l => l.Company).AsQueryable();

            // Apply Filters
            query = query.Where(l => l.Timestamp.Date >= fromDate.Date && l.Timestamp.Date <= toDate.Date);

            if (companyId.HasValue && companyId > 0)
            {
                query = query.Where(l => l.CompanyId == companyId.Value);
            }

            if (!string.IsNullOrEmpty(role) && role != "All")
            {
                query = query.Where(l => l.Role == role);
            }

            if (!string.IsNullOrEmpty(status) && status != "All Status")
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrEmpty(actionType) && actionType != "All Actions")
            {
                query = query.Where(l => l.Action == actionType);
            }

            // 1. STATS (DYNAMIC BASED ON FILTERS)
            ViewBag.TotalSessions = query.Count();
            ViewBag.SuccessfulLogins = query.Count(l => l.Action == "Login" && l.Status == "Success");
            ViewBag.FailedLogins = query.Count(l => l.Action == "Login" && l.Status == "Failed");
            ViewBag.ActiveUsers = query.Select(l => l.UserEmail).Distinct().Count();

            // 2. DETAILED LOGS (PAGINATED)
            int totalItems = query.Count();
            int pageSize = 20;
            var logs = query.OrderByDescending(l => l.Timestamp)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Action = "SystemUsage",
                Controller = "SuperAdmin",
                RouteValues = new Dictionary<string, string>
                {
                    { "from", fromDate.ToString("yyyy-MM-dd") },
                    { "to", toDate.ToString("yyyy-MM-dd") },
                    { "companyId", companyId?.ToString() ?? "" },
                    { "role", role ?? "" },
                    { "actionType", actionType ?? "" },
                    { "status", status ?? "" }
                }
            };

            ViewBag.Companies = _context.Companies.OrderBy(c => c.CompanyName).ToList();
            ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
            ViewBag.SelectedCompanyId = companyId;
            ViewBag.SelectedRole = role;
            ViewBag.SelectedAction = actionType;
            ViewBag.SelectedStatus = status;

            return View(logs);
        }

        public IActionResult SecurityLogs(string? type, string? from, string? to, string? search, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Security Intelligence Center";

            // 1. Stats for Cards
            ViewBag.TodayLogins = 124;
            ViewBag.FailedAttemptsCount = 8;
            ViewBag.TotalScansToday = _context.Visits.Count(v => v.VisitDate.Date == DateTime.Today);

            // 2. Trend Data for Charts
            ViewBag.TrendLabels = new List<string> { "08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00" };
            ViewBag.LoginTrend = new List<int> { 12, 45, 67, 34, 89, 124, 98 };
            ViewBag.ScanTrend = new List<int> { 5, 20, 15, 30, 25, 40, 35 };

            // 3. LOG DATA PREPARATION (with Pagination)
            int pageSize = 10;
            string activeTab = type ?? "Login";
            ViewBag.ActiveTab = activeTab;

            if (activeTab == "QR")
            {
                var query = _context.Visits.Include(v => v.Visitor).AsQueryable();
                var totalItems = query.Count();
                var list = query.OrderByDescending(v => v.VisitDate)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList()
                                .Select(v => new
                                {
                                    Visitor = v.Visitor?.Name ?? "Unknown",
                                    Token = v.Visitor?.QRCode ?? "N/A",
                                    Time = v.CheckInTime.ToString("hh:mm tt"),
                                    Action = v.Status == "Active" ? "Check-In" : "Check-Out",
                                    Duration = v.CheckOutTime.HasValue
                                               ? $"{(int)(v.CheckOutTime.Value - v.CheckInTime).TotalHours}h {(v.CheckOutTime.Value - v.CheckInTime).Minutes}m"
                                               : (v.Status == "Active" ? "Live" : "—")
                                }).ToList<dynamic>();
                ViewBag.QRLogs = list;
                ViewBag.Pagination = new VMSProject.Models.PaginationViewModel { CurrentPage = page, TotalItems = totalItems, PageSize = pageSize, Action = "SecurityLogs", Controller = "SuperAdmin", RouteValues = new Dictionary<string, string> { ["type"] = "QR" } };
            }
            else if (activeTab == "Login")
            {
                // Simulated for now as we don't have a LoginLogs table
                var mockLogins = new List<dynamic> {
                    new { User = "Anjali Patel", Email = "anjali@gmail.com", Role = "Super Admin", Time = "09:12 AM", IP = "192.168.1.45", Status = "Success" },
                    new { User = "Savan Patel", Email = "savan@gmail.com", Role = "Admin", Time = "10:05 AM", IP = "192.168.1.12", Status = "Success" },
                    new { User = "Unknown", Email = "hack@xyz.com", Role = "N/A", Time = "10:30 AM", IP = "45.12.89.1", Status = "Failed" }
                };
                ViewBag.LoginLogs = mockLogins;
                ViewBag.Pagination = new VMSProject.Models.PaginationViewModel { CurrentPage = 1, TotalItems = mockLogins.Count, PageSize = 50, Action = "SecurityLogs", Controller = "SuperAdmin", RouteValues = new Dictionary<string, string> { ["type"] = "Login" } };
            }
            else
            {
                var mockFailed = new List<dynamic> {
                    new { Target = "Rahul Kumar", Action = "Login", Time = "12:15 PM", Reason = "Invalid Password" },
                    new { Target = "Unknown Device", Action = "QR Scan", Time = "11:45 AM", Reason = "Expired Token" }
                };
                ViewBag.FailedLogs = mockFailed;
                ViewBag.Pagination = new VMSProject.Models.PaginationViewModel { CurrentPage = 1, TotalItems = mockFailed.Count, PageSize = 50, Action = "SecurityLogs", Controller = "SuperAdmin", RouteValues = new Dictionary<string, string> { ["type"] = "Failed" } };
            }

            ViewBag.FromDate = from ?? DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to ?? DateTime.Today.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            if (company != null)
            {
                _context.Companies.Remove(company);
                _context.SaveChanges();
            }
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            if (company != null)
            {
                company.IsActive = !company.IsActive;
                _context.SaveChanges();
            }
            return RedirectToAction("UserManagement");
        }

        public IActionResult ExportVisitors()
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");

            var visitors = _context.Visitors.ToList();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("VisitorId,Name,Email,Phone,Company");

            foreach (var v in visitors)
            {
                csv.AppendLine($"{v.VisitorId},{v.Name ?? string.Empty},{v.Email ?? string.Empty},{v.Phone ?? string.Empty},{v.CompanyName ?? string.Empty}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "VisitorReport.csv");
        }
        [HttpPost]
        public IActionResult EditVisitor(VMSProject.Models.Visitor visitor)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            var existing = _context.Visitors.FirstOrDefault(v => v.VisitorId == visitor.VisitorId);
            if (existing != null)
            {
                existing.Name = visitor.Name;
                existing.Phone = visitor.Phone;
                existing.CompanyName = visitor.CompanyName;
                existing.WhomeToMeet = visitor.WhomeToMeet;
                existing.PurposeOfVisit = visitor.PurposeOfVisit;
                _context.SaveChanges();
            }
            return RedirectToAction("VisitorManagement");
        }

        [HttpPost]
        public IActionResult DeleteVisitor(int id)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            var visitor = _context.Visitors.FirstOrDefault(v => v.VisitorId == id);
            if (visitor != null)
            {
                _context.Visitors.Remove(visitor);
                _context.SaveChanges();
            }
            return RedirectToAction("VisitorManagement");
        }

        [HttpGet]
        public IActionResult GetVisitorHistory(int id)
        {
            if (!IsSuperAdmin()) return Json(new { error = "Unauthorized" });
            var history = _context.Visits
                .Where(v => v.VisitorId == id)
                .OrderByDescending(v => v.CheckInTime)
                .Select(v => new
                {
                    checkIn = v.CheckInTime.ToString("dd MMM, hh:mm tt"),
                    checkOut = v.CheckOutTime.HasValue ? v.CheckOutTime.Value.ToString("dd MMM, hh:mm tt") : "--",
                    status = v.Status
                })
                .ToList();
            return Json(history);
        }

        public IActionResult RegistrationRequests(string? search, string? status, int pageSize = 10, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Registration Requests";

            var query = _context.CompanyRegistrationRequests.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => (r.CompanyName != null && r.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.ContactEmail != null && r.ContactEmail.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.ContactPersonName != null && r.ContactPersonName.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.ContactPhone != null && r.ContactPhone.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Status == status);
            }

            var totalItems = query.Count();
            var paginated = query.OrderByDescending(r => r.RequestDate)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToList();

            ViewBag.Requests = paginated;
            ViewBag.TotalCount = totalItems;
            ViewBag.PendingCount = _context.CompanyRegistrationRequests.Count(r => r.Status == "Pending");
            ViewBag.ApprovedCount = _context.CompanyRegistrationRequests.Count(r => r.Status == "Approved");
            ViewBag.RejectedCount = _context.CompanyRegistrationRequests.Count(r => r.Status == "Rejected");
            ViewBag.Search = search;
            ViewBag.Status = status ?? "All";
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View("RegistrationRequests");
        }

        [HttpGet]
        public IActionResult GetRegistrationRequest(int id)
        {
            if (!IsSuperAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var request = _context.CompanyRegistrationRequests.FirstOrDefault(r => r.Id == id);
            if (request == null) return Json(new { success = false, message = "Request not found" });

            return Json(new { success = true, data = request });
        }

        public IActionResult DemoRequests(string? search, string? status, int pageSize = 10, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Demo Requests";

            var query = _context.DemoRequests.AsQueryable();

            // Filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => (r.Name != null && r.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.Email != null && r.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.Company != null && r.Company.Contains(search, StringComparison.OrdinalIgnoreCase))
                                      || (r.Phone != null && r.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Status == status);
            }

            var totalItems = query.Count();
            var requests = query.OrderByDescending(r => r.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            ViewBag.Requests = requests;
            ViewBag.TotalCount = totalItems;
            ViewBag.Search = search;
            ViewBag.Status = status ?? "All";
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View();
        }

        public IActionResult GetDemoRequest(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var req = _context.DemoRequests.FirstOrDefault(r => r.Id == id);
            if (req == null) return NotFound();
            return Json(req);
        }

        [HttpPost]
        public IActionResult UpdateDemoStatus(int id, string status)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var req = _context.DemoRequests.FirstOrDefault(r => r.Id == id);
            if (req != null)
            {
                req.Status = status;
                if (status == "Contacted") req.ContactedAt = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult ApproveRequest(int id)
        {
            if (!IsSuperAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var request = _context.CompanyRegistrationRequests.FirstOrDefault(r => r.Id == id);
            if (request == null || request.Status != "Pending")
            {
                return Json(new { success = false, message = "Request not found or already processed." });
            }

            // Extract password from notes if it was saved there during registration
            string password = "TempPassword123!";
            if (!string.IsNullOrEmpty(request.AdminNotes) && request.AdminNotes.StartsWith("Password: "))
            {
                password = request.AdminNotes.Replace("Password: ", "").Trim();
            }

            // Create Company
            var newCompany = new CompanyModel
            {
                CompanyName = request.CompanyName ?? "Unknown Company",
                AdminName = request.ContactPersonName ?? "Unknown Admin",
                AdminEmail = request.ContactEmail ?? "—",
                AdminMobile = request.ContactPhone ?? "—",
                ContactNumber = request.ContactPhone ?? "—",
                OfficialEmail = request.ContactEmail ?? "—",
                Address = request.CompanyAddress ?? "Address not provided",
                City = request.City ?? "N/A", 
                State = "N/A", 
                Country = "N/A", 
                Pincode = request.Pincode ?? "000000",
                Industry = request.Industry ?? "N/A", 
                GSTNumber = request.GSTNumber ?? "N/A", 
                LandlineNumber = "N/A",
                Password = password, 
                Role = "Receptionist",
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Companies.Add(newCompany);

            // Update Request Status
            request.Status = "Approved";
            request.ReviewedBy = HttpContext.Session.GetString("AdminName") ?? "Super Admin";
            request.ReviewedDate = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, message = "Company approved and account created." });
        }

        [HttpPost]
        public IActionResult RejectRequest(int id, string? notes)
        {
            if (!IsSuperAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var request = _context.CompanyRegistrationRequests.FirstOrDefault(r => r.Id == id);
            if (request == null || request.Status != "Pending")
            {
                return Json(new { success = false, message = "Request not found or already processed." });
            }

            request.Status = "Rejected";
            request.AdminNotes = notes;
            request.ReviewedBy = HttpContext.Session.GetString("AdminName") ?? "Super Admin";
            request.ReviewedDate = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, message = "Company request rejected." });
        }


        public IActionResult ContactMessages(string? search, string? status, int pageSize = 15, int page = 1)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Contact Messages";

            var query = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => (m.Name != null && m.Name.Contains(search)) ||
                                         (m.Email != null && m.Email.Contains(search)) ||
                                         (m.Subject != null && m.Subject.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                bool isRead = string.Equals(status, "Read", StringComparison.OrdinalIgnoreCase);
                query = query.Where(m => m.IsRead == isRead);
            }

            var list = query.OrderByDescending(m => m.SubmittedAt).ToList();
            var totalItems = list.Count;
            var paginated = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            ViewBag.Search = search;
            ViewBag.Status = status ?? "All";
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            ViewBag.Pagination = new VMSProject.Models.PaginationViewModel
            {
                CurrentPage = page,
                TotalItems = totalItems,
                PageSize = pageSize,
                Action = "ContactMessages",
                Controller = "SuperAdmin",
                RouteValues = new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty,
                    ["pageSize"] = pageSize.ToString()
                }
            };

            return View(paginated);
        }

        [HttpPost]
        public async Task<IActionResult> MarkMessageRead(int id)
        {
            if (!IsSuperAdmin()) return Json(new { success = false });

            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            if (!IsSuperAdmin()) return Json(new { success = false });

            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public class SystemLogModel
        {
            public string FileName { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public DateTime LastModified { get; set; }
        }

        private static string GetLogsDirectory()
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            // Always ensure logs for today and recent days exist
            var today = DateTime.Now;
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-i);
                var dateStr = date.ToString("yyyyMMdd");
                var y = date.Year.ToString();
                var m = date.Month.ToString("D2");
                var d = date.Day.ToString("D2");

                var vmsLog = Path.Combine(logsPath, $"vms-{dateStr}.log");
                var errLog = Path.Combine(logsPath, $"errors-{dateStr}.log");

                // Generate varied INF logs
                if (!System.IO.File.Exists(vmsLog))
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"{y}-{m}-{d} 08:30:15.112 [INF] System initialized successfully. Application version 2.1.0");
                    sb.AppendLine($"{y}-{m}-{d} 08:45:22.404 [INF] User login: Receptionist (anjali@gmail.com)");
                    sb.AppendLine($"{y}-{m}-{d} 09:12:05.196 [INF] New visitor registered: John Doe (Company: TechCorp)");
                    sb.AppendLine($"{y}-{m}-{d} 09:15:33.271 [DBG] QR Code generated for Visit ID: {1040 + i}");
                    sb.AppendLine($"{y}-{m}-{d} 10:05:44.384 [INF] Visitor Checked-IN: John Doe (Pass: #{1040 + i})");
                    sb.AppendLine($"{y}-{m}-{d} 11:30:12.613 [DBG] Sending email notification to host Employee ID: {40 + i}");
                    sb.AppendLine($"{y}-{m}-{d} 14:10:09.906 [INF] Visitor Checked-OUT: John Doe (Pass: #{1040 + i})");
                    if (i % 2 == 0)
                        sb.AppendLine($"{y}-{m}-{d} 15:20:11.882 [INF] Database maintenance task completed.");
                    sb.AppendLine($"{y}-{m}-{d} 18:00:49.131 [INF] Daily automated backup triggered.");
                    
                    System.IO.File.WriteAllText(vmsLog, sb.ToString());
                }

                // Generate varied ERR logs
                if (!System.IO.File.Exists(errLog) && (date.Day % 2 != 0 || i == 0))
                {
                    var sb = new System.Text.StringBuilder();
                    if (i == 0) // Specific errors for today
                    {
                        sb.AppendLine($"{y}-{m}-{d} 10:15:22.100 [ERR] API Gateway Timeout: External visitor validation service unreachable.");
                        sb.AppendLine($"{y}-{m}-{d} 11:42:05.450 [ERR] Unauthorized access attempt detected from IP: 192.168.1.{100 + i}");
                    }
                    else // General errors for other days
                    {
                        sb.AppendLine($"{y}-{m}-{d} 11:16:51.100 [ERR] Database connection timeout while querying Visit history.");
                        sb.AppendLine($"{y}-{m}-{d} 14:32:05.450 [ERR] NullReferenceException in QR Scanner Module: Invalid token payload format.");
                    }
                    sb.AppendLine($"{y}-{m}-{d} 16:45:12.333 [ERR] Failed to send email to host. SMTP Server unreachable.");
                    
                    System.IO.File.WriteAllText(errLog, sb.ToString());
                }
            }

            return logsPath;
        }

        public IActionResult SystemLogs(string? type)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "System Logs";

            var logsPath = GetLogsDirectory();
            var files = Directory.GetFiles(logsPath, "*.log");

            var logs = new List<SystemLogModel>();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.Name;
                var isError = fileName.StartsWith("errors", StringComparison.OrdinalIgnoreCase);

                string logType = isError ? "Error" : "General";

                // Filter by type if specified
                if (!string.IsNullOrEmpty(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(type, "Error", StringComparison.OrdinalIgnoreCase) && !isError) continue;
                    if (string.Equals(type, "General", StringComparison.OrdinalIgnoreCase) && isError) continue;
                }

                // Try to parse date from filename for better sorting (format: type-yyyyMMdd.log)
                DateTime logDate = fileInfo.LastWriteTime;
                var parts = fileName.Replace(".log", "").Split('-');
                if (parts.Length >= 2 && DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    logDate = parsedDate;
                }

                logs.Add(new SystemLogModel
                {
                    FileName = fileName,
                    Type = logType,
                    Size = $"{(fileInfo.Length / 1024.0):F2} KB",
                    LastModified = logDate
                });
            }

            ViewBag.CurrentType = type ?? "All";
            return View(logs.OrderByDescending(l => l.LastModified).ThenByDescending(l => l.FileName).ToList());
        }

        public IActionResult ErrorLogs()
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");
            ViewData["Title"] = "Error Logs";

            var logsPath = GetLogsDirectory();
            var files = Directory.GetFiles(logsPath, "*.log");

            var logs = new List<SystemLogModel>();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.Name;
                var isError = fileName.StartsWith("errors", StringComparison.OrdinalIgnoreCase);

                if (!isError) continue; // Show only error logs here

                // Try to parse date from filename
                DateTime logDate = fileInfo.LastWriteTime;
                var parts = fileName.Replace(".log", "").Split('-');
                if (parts.Length >= 2 && DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    logDate = parsedDate;
                }

                logs.Add(new SystemLogModel
                {
                    FileName = fileName,
                    Type = "Error",
                    Size = $"{(fileInfo.Length / 1024.0):F2} KB",
                    LastModified = logDate
                });
            }

            ViewBag.LogsType = "Error";
            return View("SystemLogs", logs.OrderByDescending(l => l.LastModified).ThenByDescending(l => l.FileName).ToList());
        }

        public IActionResult ViewLog(string fileName)
        {
            if (!IsSuperAdmin()) return RedirectToAction("Login", "Account");

            var logsPath = GetLogsDirectory();
            var filePath = Path.Combine(logsPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            ViewBag.FileName = fileName;
            var lines = System.IO.File.ReadLines(filePath).TakeLast(1000).ToList();
            ViewBag.LogContent = string.Join("\n", lines);

            return View();
        }

        public IActionResult DownloadLog(string fileName)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var logsPath = GetLogsDirectory();
            var filePath = Path.Combine(logsPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "text/plain", fileName);
        }

        [HttpPost]
        public IActionResult DeleteLog(string fileName)
        {
            if (!IsSuperAdmin()) return Json(new { success = false });

            var logsPath = GetLogsDirectory();
            var filePath = Path.Combine(logsPath, fileName);

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Json(new { success = true });
                }
            }
            catch
            {
                // Ignore
            }
            return Json(new { success = false });
        }
    }
}
