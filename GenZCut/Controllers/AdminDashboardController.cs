using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using GenZCut.Data;
using GenZCut.Models.Admin;
using GenZCut.Models;

namespace GenZCut.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            model.TotalCustomers = await _context.Users.CountAsync();

            model.TotalBarbers = await _context.Barbers.CountAsync();

            model.TotalServices = await _context.Services.CountAsync();

            model.TotalAppointments =
                await _context.Appointments.CountAsync();

            model.PendingAppointments =
                await _context.Appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Pending);

            model.CompletedAppointments =
                await _context.Appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Completed);

            model.CancelledAppointments =
                await _context.Appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Cancelled);

            // Revenue calculation will be added after
            // verifying your actual Appointment price/payment fields.
            model.TotalRevenue = 0;

            return View(model);
        }
    }
}