using Microsoft.AspNetCore.Mvc;
using VMSProject.Models;
using Microsoft.AspNetCore.Http; // For Session
using System.Linq;

namespace VMSProject.Controllers
{
    public class AccountController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env = env;

        // ================= REGISTER (GET) =================
        public IActionResult Register(bool success = false)
        {
            ViewBag.Success = success;
            return View();
        }

        // ================= REGISTER (POST) =================
        [HttpPost]
        public IActionResult Register(CompanyModel model)
        {
            // Note: We use CompanyModel as the binding model for simplicity in the view, 
            // but we save it as a CompanyRegistrationRequest.
            if (ModelState.IsValid)
            {
                var request = new CompanyRegistrationRequest
                {
                    CompanyName = model.CompanyName ?? "Unknown",
                    ContactPersonName = model.AdminName ?? "Unknown",
                    ContactEmail = model.AdminEmail ?? "—",
                    ContactPhone = model.AdminMobile ?? "—",
                    CompanyAddress = model.Address,
                    Industry = model.Industry,
                    GSTNumber = model.GSTNumber,
                    City = model.City,
                    Pincode = model.Pincode,
                    Status = "Pending",
                    RequestDate = DateTime.Now,
                    AdminNotes = "Password: " + model.Password
                };

                _context.CompanyRegistrationRequests.Add(request);
                _context.SaveChanges();

                return RedirectToAction("Register", new { success = true });
            }

            // Log validation errors for debugging if needed
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.Errors = errors;

            return View(model);
        }

        // ================= LOGIN (GET) =================
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (string.Equals(role, "Superadmin", System.StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Dashboard", "SuperAdmin");
                
                return RedirectToAction("Dashboard", "User");
            }

            if (Request.Cookies["AdminEmail"] != null)
            {
                ViewBag.AdminEmail = Request.Cookies["AdminEmail"];
                ViewBag.Password = Request.Cookies["Password"];
                ViewBag.RememberMe = true;
            }
            return View();
        }

        // ================= LOGIN (POST) =================
        [HttpPost]
        public IActionResult Login(string AdminEmail, string Password, bool RememberMe = false)
        {
            if (_context.Companies == null)
            {
                ViewBag.Error = "Database error: Companies table not found.";
                return View();
            }

            var email = AdminEmail?.Trim() ?? string.Empty;
            var pwd = Password?.Trim() ?? string.Empty;

            var user = _context.Companies.FirstOrDefault(c => 
                c.AdminEmail == email && c.Password == pwd);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ViewBag.Error = "This account has been deactivated. Please contact the platform owner.";
                    return View();
                }

                // ── ROLE ASSIGNMENT ──
                // Check if user is the hardcoded master admin or has the "Superadmin" role in DB
                bool isSuperAdmin = string.Equals(user.AdminEmail, "anjali@gmail.com", StringComparison.OrdinalIgnoreCase) || 
                                   string.Equals(user.Role, "Superadmin", StringComparison.OrdinalIgnoreCase);

                string assignedRole = isSuperAdmin ? "Superadmin" : "Receptionist";

                HttpContext.Session.SetString("AdminName", user.AdminName ?? "Authorized User");
                HttpContext.Session.SetString("AdminEmail", user.AdminEmail ?? "-");
                HttpContext.Session.SetString("UserRole", assignedRole);
                HttpContext.Session.SetInt32("CompanyId", user.CompanyId);

                LogActivity("Login", "Authentication", "Success");

                if (RememberMe)
                {
                    CookieOptions options = new() { Expires = DateTime.Now.AddDays(30) };
                    Response.Cookies.Append("AdminEmail", email, options);
                    Response.Cookies.Append("Password", pwd, options);
                }
                else
                {
                    Response.Cookies.Delete("AdminEmail");
                    Response.Cookies.Delete("Password");
                }

                if (isSuperAdmin)
                {
                    return RedirectToAction("Dashboard", "SuperAdmin");
                }

                return RedirectToAction("Dashboard", "User");
            }

            LogActivity("Login", "Authentication", "Failed", "Invalid Email or Password: " + AdminEmail);

            // If not found in Companies, check if they have a pending request
            var pendingRequest = _context.CompanyRegistrationRequests
                .FirstOrDefault(r => r.ContactEmail == AdminEmail);

            if (pendingRequest != null)
            {
                if (pendingRequest.Status == "Pending")
                {
                    ViewBag.Error = "Registration Pending: Your organization's request is currently being reviewed by our Super Admin. Please wait for approval.";
                    return View();
                }
                else if (pendingRequest.Status == "Rejected")
                {
                    ViewBag.Error = "Registration Rejected: " + (pendingRequest.AdminNotes ?? "Your request did not meet our criteria.");
                    return View();
                }
            }

            ViewBag.Error = "Invalid Email or Password";

            if (RememberMe)
            {
                ViewBag.AdminEmail = AdminEmail;
                ViewBag.Password = Password;
                ViewBag.RememberMe = true;
            }

            return View();
        }

        public IActionResult Logout()
        {
            LogActivity("Logout", "Authentication", "Success");
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        private void LogActivity(string action, string module, string status, string? details = null)
        {
            try
            {
                _context.UserActivityLogs.Add(new UserActivityLog
                {
                    Timestamp = DateTime.Now,
                    UserEmail = HttpContext.Session.GetString("AdminEmail") ?? "Unknown",
                    UserName = HttpContext.Session.GetString("AdminName") ?? "Unknown",
                    Role = HttpContext.Session.GetString("UserRole") ?? "-",
                    CompanyId = HttpContext.Session.GetInt32("CompanyId"),
                    Action = action,
                    Module = module,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    Status = status,
                    Details = details
                });
                _context.SaveChanges();
            }
            catch { /* Ignore logging errors */ }
        }
    }
}