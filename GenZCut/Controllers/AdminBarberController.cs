using GenZCut.Data;
using GenZCut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminBarberController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBarberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminBarber
        public async Task<IActionResult> Index(string search)
        {
            var barbers = _context.Barbers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                barbers = barbers.Where(b =>
                    b.Name.Contains(search));
            }

            ViewBag.Search = search;

            return View(await barbers.ToListAsync());
        }

        // GET: Admin/AdminBarber/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var barber = await _context.Barbers
                .FirstOrDefaultAsync(b => b.Id == id);

            if (barber == null)
                return NotFound();

            return View(barber);
        }

        // GET: Admin/AdminBarber/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminBarber/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Barber barber)
        {
            if (!ModelState.IsValid)
                return View(barber);

            _context.Barbers.Add(barber);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Barber added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminBarber/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var barber = await _context.Barbers.FindAsync(id);

            if (barber == null)
                return NotFound();

            return View(barber);
        }

        // POST: Admin/AdminBarber/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Barber barber)
        {
            if (id != barber.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(barber);

            var existingBarber = await _context.Barbers.FindAsync(id);

            if (existingBarber == null)
                return NotFound();

            existingBarber.Name = barber.Name;

            // Add other editable properties
            // according to your Barber model.

            await _context.SaveChangesAsync();

            TempData["Success"] = "Barber updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/AdminBarber/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var barber = await _context.Barbers.FindAsync(id);

            if (barber == null)
                return NotFound();

            // Prevent deleting barbers with existing appointments
            bool hasAppointments = await _context.Appointments
                .AnyAsync(a => a.BarberId == id);

            if (hasAppointments)
            {
                TempData["Error"] =
                    "Cannot delete barber because appointments exist.";

                return RedirectToAction(nameof(Index));
            }

            _context.Barbers.Remove(barber);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Barber deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}