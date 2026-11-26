using GenZCut.Data;
using GenZCut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminAppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminAppointmentController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminAppointment
        public async Task<IActionResult> Index(
            string? status,
            DateTime? date)
        {
            var appointments = _context.Appointments
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<AppointmentStatus>(
                    status, true, out var parsedStatus))
            {
                appointments = appointments.Where(
                    a => a.Status == parsedStatus);
            }

            // Filter by appointment date
            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);

                appointments = appointments.Where(a =>
                    a.AppointmentDate >= startDate &&
                    a.AppointmentDate < endDate);
            }

            ViewBag.Status = status;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(await appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync());
        }

        // GET: Admin/AdminAppointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }

        // POST: Admin/AdminAppointment/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            AppointmentStatus status)
        {
            var appointment = await _context.Appointments
                .FindAsync(id);

            if (appointment == null)
                return NotFound();

            appointment.Status = status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Appointment status updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}