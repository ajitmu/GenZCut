using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GenZCut.Controllers
{
    public class BarberController : Controller
    {
        private readonly IConfiguration _configuration;

        public BarberController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        // 1. Barber Dashboard with Session Auth
        public IActionResult Dashboard()
        {
            int? barberId = HttpContext.Session.GetInt32("BarberId");

            if (barberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DataTable dtAppointments = new DataTable();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT a.AppointmentId, u.FullName AS CustomerName, u.Phone, 
                           a.AppointmentDate, a.AppointmentTime, a.Status 
                    FROM Appointments a
                    JOIN Customers c ON a.CustomerId = c.CustomerId
                    JOIN Users u ON c.UserId = u.UserId
                    WHERE a.BarberId = @BarberId
                    ORDER BY a.AppointmentDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BarberId", barberId.Value);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtAppointments);
                }
            }

            // Summary Stats Calculation
            ViewBag.Total = dtAppointments.Rows.Count;
            ViewBag.Pending = dtAppointments.Select("Status = 'Pending'").Length;
            ViewBag.Confirmed = dtAppointments.Select("Status = 'Confirmed'").Length;
            ViewBag.Rejected = dtAppointments.Select("Status = 'Rejected' OR Status = 'Cancelled'").Length;
            ViewBag.BarberId = barberId.Value;

            return View(dtAppointments);
        }

        // 2. Accept / Reject Action Update
        [HttpPost]
        public IActionResult UpdateStatus(int appointmentId, string status)
        {
            int? barberId = HttpContext.Session.GetInt32("BarberId");

            if (barberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "UPDATE Appointments SET Status = @Status WHERE AppointmentId = @AppointmentId AND BarberId = @BarberId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    cmd.Parameters.AddWithValue("@BarberId", barberId.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMsg"] = $"Appointment status updated to '{status}' successfully!";
            return RedirectToAction("Dashboard");
        }

        // 3. Barber Availability Status Toggle
        [HttpPost]
        public IActionResult ToggleAvailability(bool isAvailable)
        {
            int? barberId = HttpContext.Session.GetInt32("BarberId");

            if (barberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string statusValue = isAvailable ? "Active" : "Inactive";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "UPDATE Barbers SET Status = @Status WHERE BarberId = @BarberId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", statusValue);
                    cmd.Parameters.AddWithValue("@BarberId", barberId.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMsg"] = "Availability status updated successfully!";
            return RedirectToAction("Dashboard");
        }

        // 4. Logout Logic
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}