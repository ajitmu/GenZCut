using Microsoft.AspNetCore.Identity;
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

        private bool IsLoggedIn()
        {
            string? adminEmail = HttpContext.Session.GetString("AdminEmail");
            string? userRole = HttpContext.Session.GetString("UserRole");
            int? userId = HttpContext.Session.GetInt32("UserId");

            return !string.IsNullOrEmpty(adminEmail)
                || (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && userId.HasValue);
        }

        private static bool VerifyPassword(string storedPassword, string enteredPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            var hasher = new PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword(new object(), storedPassword, enteredPassword);

            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }

            return storedPassword == enteredPassword;
        }

        // 1. Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            string cleanEmail = (email ?? "").Trim();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                // 1) Check dbo.Users where Role = 'Admin'
                string userQuery = @"
                    SELECT TOP 1 UserId, FullName, Email, Password
                    FROM dbo.Users
                    WHERE Email = @Email AND Role = 'Admin'";

                using (SqlCommand userCmd = new SqlCommand(userQuery, conn))
                {
                    userCmd.Parameters.AddWithValue("@Email", cleanEmail);

                    using (SqlDataReader reader = userCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["Password"].ToString() ?? "";
                            if (VerifyPassword(storedHash, password))
                            {
                                string adminEmail = reader["Email"].ToString() ?? cleanEmail;
                                string fullName = reader["FullName"].ToString() ?? "Admin";
                                int userId = Convert.ToInt32(reader["UserId"]);

                                HttpContext.Session.SetString("AdminEmail", adminEmail);
                                HttpContext.Session.SetString("AdminName", fullName);
                                HttpContext.Session.SetString("UserName", fullName);
                                HttpContext.Session.SetString("UserRole", "Admin");
                                HttpContext.Session.SetInt32("UserId", userId);

                                return RedirectToAction("Dashboard");
                            }
                        }
                    }
                }

                // 2) Fallback: Check dbo.Admins if table exists
                string adminQuery = @"
                    SELECT TOP 1 Email, Password
                    FROM dbo.Admins
                    WHERE Email = @Email";

                using (SqlCommand adminCmd = new SqlCommand(adminQuery, conn))
                {
                    adminCmd.Parameters.AddWithValue("@Email", cleanEmail);

                    try
                    {
                        using (SqlDataReader reader = adminCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader["Password"].ToString() ?? "";
                                if (VerifyPassword(storedPassword, password))
                                {
                                    string adminEmail = reader["Email"].ToString() ?? cleanEmail;

                                    HttpContext.Session.SetString("AdminEmail", adminEmail);
                                    HttpContext.Session.SetString("UserRole", "Admin");

                                    return RedirectToAction("Dashboard");
                                }
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        // Agar dbo.Admins table nahi hai to ignore karke invalid credentials dikhayega
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
            HttpContext.Session.Remove("AdminName");
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminUserId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserName");
            return RedirectToAction("Index", "Home");
        }

        // 2. Dashboard
        public IActionResult Dashboard()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                ViewBag.AdminEmail = HttpContext.Session.GetString("AdminEmail")
                    ?? HttpContext.Session.GetString("UserName")
                    ?? "Admin";
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