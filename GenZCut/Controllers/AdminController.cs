using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GenZCut.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection")!;

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminEmail"));

        // 1. Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "SELECT TOP 1 Email FROM dbo.Admins WHERE Email = @Email AND Password = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email.Trim());
                    cmd.Parameters.AddWithValue("@Password", password);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        HttpContext.Session.SetString("AdminEmail", result.ToString()!);
                        return RedirectToAction("Dashboard");
                    }
                }
            }

            ViewBag.Error = "Invalid Email ya Password";
            return View();
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminEmail");
            return RedirectToAction("Login");
        }

        // 2. Dashboard
        public IActionResult Dashboard()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                ViewBag.AdminEmail = HttpContext.Session.GetString("AdminEmail");
                ViewBag.TotalCustomers = GetCount(conn, "SELECT COUNT(*) FROM dbo.Customers");
                ViewBag.TotalBarbers = GetCount(conn, "SELECT COUNT(*) FROM dbo.Barbers");
                ViewBag.TotalHairstyles = GetCount(conn, "SELECT COUNT(*) FROM dbo.Hairstyles");
                ViewBag.TotalAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments");
                ViewBag.PendingAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments WHERE ISNULL(Status, 'Pending') = 'Pending'");

                DataTable dtRecent = new DataTable();
                string recentQuery = @"
                    SELECT TOP 8 a.AppointmentId, uC.FullName AS CustomerName, uB.FullName AS BarberName,
                           h.StyleName, a.AppointmentDate, a.AppointmentTime, ISNULL(a.Status, 'Pending') AS Status
                    FROM dbo.Appointments a
                    JOIN dbo.Customers c ON a.CustomerId = c.CustomerId
                    JOIN dbo.Users uC ON c.UserId = uC.UserId
                    JOIN dbo.Barbers b ON a.BarberId = b.BarberId
                    JOIN dbo.Users uB ON b.UserId = uB.UserId
                    JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    ORDER BY a.AppointmentId DESC";

                using (SqlDataAdapter da = new SqlDataAdapter(recentQuery, conn))
                {
                    da.Fill(dtRecent);
                }

                return View(dtRecent);
            }
        }

        // 3. Appointments Management
        public IActionResult Appointments()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT a.AppointmentId, uC.FullName AS CustomerName, uC.Phone,
                           uB.FullName AS BarberName, h.StyleName, h.Price,
                           a.AppointmentDate, a.AppointmentTime, ISNULL(a.Status, 'Pending') AS Status
                    FROM dbo.Appointments a
                    JOIN dbo.Customers c ON a.CustomerId = c.CustomerId
                    JOIN dbo.Users uC ON c.UserId = uC.UserId
                    JOIN dbo.Barbers b ON a.BarberId = b.BarberId
                    JOIN dbo.Users uB ON b.UserId = uB.UserId
                    JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    ORDER BY a.AppointmentId DESC";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.Fill(dt);
                }
            }

            return View(dt);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int appointmentId, string status)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "UPDATE dbo.Appointments SET Status = @Status WHERE AppointmentId = @AppointmentId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Appointments");
        }

        private static int GetCount(SqlConnection conn, string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
        }
    }
}