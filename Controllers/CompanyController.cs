using Microsoft.AspNetCore.Mvc;
using VMSProject.Models;
using Microsoft.AspNetCore.Http; // For Session
using System.Linq;

namespace VMSProject.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public CompanyController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // COMPANY LIST
        public IActionResult Index()
        {
            // Example of reading session if needed
            ViewBag.Message = HttpContext.Session.GetString("CompanyMessage");
            HttpContext.Session.Remove("CompanyMessage");

            var companies = _context.Companies.ToList();
            return View("~/Views/User/Company.cshtml", companies);
        }

        public IActionResult Edit(int id, bool success = false)
        {
            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            ViewBag.Success = success;
            if (company == null)
            {
                return NotFound();
            }
            return View("~/Views/User/CompanyEdit.cshtml", company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CompanyModel model)
        {
            if (model.CompanyLogo != null && model.CompanyLogo.Length > 0)
            {
                string folder = System.IO.Path.Combine(_env.WebRootPath, "images", "logos");
                System.IO.Directory.CreateDirectory(folder);
                string uniqueFileName = System.Guid.NewGuid().ToString() + "_" + model.CompanyLogo.FileName;
                string filePath = System.IO.Path.Combine(folder, uniqueFileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    model.CompanyLogo.CopyTo(stream);
                }
                model.LogoPath = "/images/logos/" + uniqueFileName;
            }
            else
            {
                // Preserve existing logo if no new logo is uploaded
                var existingLogoPath = _context.Companies
                    .Where(c => c.CompanyId == model.CompanyId)
                    .Select(c => c.LogoPath)
                    .FirstOrDefault();

                model.LogoPath = existingLogoPath;
            }

            // Using EF Core Update
            _context.Companies.Update(model);
            _context.SaveChanges();

            // Example of setting session
            HttpContext.Session.SetString("CompanyMessage", "Company updated successfully.");

            return RedirectToAction("Edit", new { id = model.CompanyId, success = true });
        }

        public IActionResult Delete(int id)
        {
            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == id);
            if (company != null)
            {
                _context.Companies.Remove(company);
                _context.SaveChanges();
                HttpContext.Session.SetString("CompanyMessage", "Company deleted successfully.");
            }
            return RedirectToAction("Index");
        }
    }
}