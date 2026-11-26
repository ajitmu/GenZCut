using GenZCut.Data;
using GenZCut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminReport
        public async Task<IActionResult> Index(
            int? year,
            int? month)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            var appointments = _context.Appointments
                .AsQueryable();

            // Total appointments
            var totalAppointments =
                await appointments.CountAsync();

            // Completed appointments
            var completedAppointments =
                await appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Completed);

            // Pending appointments
            var pendingAppointments =
                await appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Pending);

            // Rejected appointments
            var rejectedAppointments =
                await appointments.CountAsync(
                    a => a.Status == AppointmentStatus.Rejected);

            // Monthly completed appointments
            var monthlyAppointments = await appointments
                .Where(a =>
                    a.AppointmentDate.Year == selectedYear &&
                    a.Status == AppointmentStatus.Completed)
                .GroupBy(a => a.AppointmentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Count()
                })
                .ToListAsync();

            // Revenue by month
            // Assumes Appointment has a Price property.
            var monthlyRevenue = await appointments
                .Where(a =>
                    a.AppointmentDate.Year == selectedYear &&
                    a.Status == AppointmentStatus.Completed)
                .GroupBy(a => a.AppointmentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(a => a.Price)
                })
                .ToListAsync();

            // Total completed revenue
            var totalRevenue = await appointments
                .Where(a =>
                    a.Status == AppointmentStatus.Completed)
                .SumAsync(a => (decimal?)a.Price) ?? 0m;

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = month;

            ViewBag.TotalAppointments = totalAppointments;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.PendingAppointments = pendingAppointments;
            ViewBag.RejectedAppointments = rejectedAppointments;

            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.MonthlyAppointments = monthlyAppointments;
            ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }
    }
}
