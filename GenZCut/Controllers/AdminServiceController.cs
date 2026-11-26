using GenZCut.Data;
using GenZCut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminService
        public async Task<IActionResult> Index(string search)
        {
            var services = _context.Services
                .Include(s => s.Barber)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                services = services.Where(s =>
                    s.Name.Contains(search));
            }

            ViewBag.Search = search;

            return View(await services.ToListAsync());
        }

        // GET: Admin/AdminService/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.Barber)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // GET: Admin/AdminService/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminService/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (!ModelState.IsValid)
                return View(service);

            _context.Services.Add(service);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminService/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: Admin/AdminService/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(service);

            var existingService = await _context.Services.FindAsync(id);

            if (existingService == null)
                return NotFound();

            existingService.Name = service.Name;
            existingService.Price = service.Price;
            existingService.DurationMinutes =
                service.DurationMinutes;
            existingService.IsActive = service.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/AdminService/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            bool hasAppointments = await _context.Appointments
                .AnyAsync(a => a.ServiceId == id);

            if (hasAppointments)
            {
                TempData["Error"] =
                    "This service has appointments. Deactivate it instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.Services.Remove(service);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service deleted successfully."; return RedirectToAction(nameof(Index));
        }
    }
}