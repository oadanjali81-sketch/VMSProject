using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace VMSProject.Controllers
{
    public class HomeController(VMSProject.Models.ApplicationDbContext context) : Controller
    {
        private readonly VMSProject.Models.ApplicationDbContext _context = context;

        public IActionResult Index()
        {
            return View(); // MVC will look for Views/Home/Index.cshtml by default
        }

        public IActionResult About()
        {
            return View(); // MVC will look for Views/Home/About.cshtml
        }

        public IActionResult Contact()
        {
            return View(); // MVC will look for Views/Home/Contact.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Name and Email are required." });
            }

            var contactMessage = new VMSProject.Models.ContactMessage
            {
                Name = name,
                Email = email,
                Subject = subject,
                Message = message,
                SubmittedAt = DateTime.Now,
                IsRead = false
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Your message has been sent successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitDemoRequest(string name, string email, string company, string phone, string message)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(company) || string.IsNullOrEmpty(phone))
            {
                return Json(new { success = false, message = "All required fields must be filled." });
            }

            var demoRequest = new VMSProject.Models.DemoRequest
            {
                Name = name,
                Email = email,
                Company = company,
                Phone = phone,
                Message = message,
                CreatedAt = DateTime.Now,
                Status = "New"
            };

            _context.DemoRequests.Add(demoRequest);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Demo request submitted successfully! We will contact you soon." });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return RedirectToAction("Dashboard", "User");
        }
    }
}
