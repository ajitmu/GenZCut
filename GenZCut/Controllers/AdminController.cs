using GenZCut.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

            var vm = new AdminDashboardViewModel
            {
                AdminEmail = HttpContext.Session.GetString("AdminEmail")
                    ?? HttpContext.Session.GetString("UserName")
                    ?? "Admin"
            };

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                vm.TotalCustomers = GetCount(conn, "SELECT COUNT(*) FROM dbo.Customers");
                vm.TotalBarbers = GetCount(conn, "SELECT COUNT(*) FROM dbo.Barbers");
                vm.ActiveBarbers = GetCount(conn, "SELECT COUNT(*) FROM dbo.Barbers WHERE Status = 'Active'");
                vm.TotalHairstyles = GetCount(conn, "SELECT COUNT(*) FROM dbo.Hairstyles");
                vm.TotalAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments");
                vm.PendingAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments WHERE ISNULL(Status, 'Pending') = 'Pending'");
                vm.ConfirmedAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments WHERE Status = 'Confirmed'");
                vm.CompletedAppointments = GetCount(conn, "SELECT COUNT(*) FROM dbo.Appointments WHERE Status = 'Completed'");

                vm.CompletedRevenue = GetDecimal(conn, @"
                    SELECT ISNULL(SUM(h.Price), 0)
                    FROM dbo.Appointments a
                    INNER JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    WHERE a.Status = 'Completed'");

                vm.ProjectedRevenue = GetDecimal(conn, @"
                    SELECT ISNULL(SUM(h.Price), 0)
                    FROM dbo.Appointments a
                    INNER JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    WHERE ISNULL(a.Status, 'Pending') IN ('Pending', 'Confirmed', 'Completed')");

                string recentQuery = @"
                    SELECT TOP 8
                        a.AppointmentId,
                        uC.FullName AS CustomerName,
                        ISNULL(uC.Phone, '') AS CustomerPhone,
                        uB.FullName AS BarberName,
                        h.StyleName,
                        ISNULL(h.Price, 0) AS Price,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        ISNULL(a.Status, 'Pending') AS Status
                    FROM dbo.Appointments a
                    JOIN dbo.Customers c ON a.CustomerId = c.CustomerId
                    JOIN dbo.Users uC ON c.UserId = uC.UserId
                    JOIN dbo.Barbers b ON a.BarberId = b.BarberId
                    JOIN dbo.Users uB ON b.UserId = uB.UserId
                    JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    ORDER BY a.AppointmentId DESC";

                using (SqlCommand cmd = new SqlCommand(recentQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        vm.RecentAppointments.Add(new AdminAppointmentRowViewModel
                        {
                            AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                            CustomerName = reader["CustomerName"]?.ToString() ?? "",
                            CustomerPhone = reader["CustomerPhone"]?.ToString() ?? "",
                            BarberName = reader["BarberName"]?.ToString() ?? "",
                            StyleName = reader["StyleName"]?.ToString() ?? "",
                            Price = reader["Price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Price"]),
                            AppointmentDate = reader["AppointmentDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["AppointmentDate"]),
                            AppointmentTime = reader["AppointmentTime"] == DBNull.Value
                                ? TimeSpan.Zero
                                : reader["AppointmentTime"] is TimeSpan ts
                                    ? ts
                                    : TimeSpan.TryParse(reader["AppointmentTime"].ToString(), out TimeSpan parsed) ? parsed : TimeSpan.Zero,
                            Status = reader["Status"]?.ToString() ?? "Pending"
                        });
                    }
                }

                try
                {
                    string feedbackQuery = @"
                        SELECT TOP 4
                            f.FeedbackId,
                            ISNULL(uC.FullName, 'Customer') AS CustomerName,
                            ISNULL(uB.FullName, 'Barber') AS BarberName,
                            ISNULL(f.Rating, 5) AS Rating,
                            ISNULL(f.Comment, '') AS Comment
                        FROM dbo.Feedback f
                        LEFT JOIN dbo.Customers c ON f.CustomerId = c.CustomerId
                        LEFT JOIN dbo.Users uC ON c.UserId = uC.UserId
                        LEFT JOIN dbo.Barbers b ON f.BarberId = b.BarberId
                        LEFT JOIN dbo.Users uB ON b.UserId = uB.UserId
                        ORDER BY f.FeedbackId DESC";

                    using (SqlCommand fbCmd = new SqlCommand(feedbackQuery, conn))
                    using (SqlDataReader fbReader = fbCmd.ExecuteReader())
                    {
                        while (fbReader.Read())
                        {
                            vm.RecentFeedback.Add(new AdminFeedbackRowViewModel
                            {
                                FeedbackId = Convert.ToInt32(fbReader["FeedbackId"]),
                                CustomerName = fbReader["CustomerName"]?.ToString() ?? "Customer",
                                BarberName = fbReader["BarberName"]?.ToString() ?? "Barber",
                                Rating = fbReader["Rating"] == DBNull.Value ? 5 : Convert.ToInt32(fbReader["Rating"]),
                                Comment = fbReader["Comment"]?.ToString() ?? ""
                            });
                        }
                    }
                }
                catch (SqlException)
                {
                }
            }

            return View(vm);
        }

        // 3. Barbers Management
        public IActionResult Barbers()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            var barbers = new List<AdminBarberRowViewModel>();
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT
                        b.BarberId,
                        b.UserId,
                        ISNULL(u.FullName, '') AS FullName,
                        ISNULL(u.Email, '') AS Email,
                        ISNULL(u.Phone, '') AS Phone,
                        ISNULL(b.Experience, 0) AS Experience,
                        ISNULL(b.Specialization, '') AS Specialization,
                        ISNULL(b.Status, 'Inactive') AS Status,
                        (SELECT COUNT(*) FROM dbo.Appointments a WHERE a.BarberId = b.BarberId) AS TotalAppointments
                    FROM dbo.Barbers b
                    INNER JOIN dbo.Users u ON b.UserId = u.UserId
                    WHERE u.Role = 'Barber'
                    ORDER BY b.BarberId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            barbers.Add(new AdminBarberRowViewModel
                            {
                                BarberId = Convert.ToInt32(reader["BarberId"]),
                                UserId = Convert.ToInt32(reader["UserId"]),
                                FullName = reader["FullName"]?.ToString() ?? "",
                                Email = reader["Email"]?.ToString() ?? "",
                                Phone = reader["Phone"]?.ToString() ?? "",
                                Experience = reader["Experience"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Experience"]),
                                Specialization = reader["Specialization"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "Inactive",
                                TotalAppointments = reader["TotalAppointments"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalAppointments"])
                            });
                        }
                    }
                }
            }

            return View(barbers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleBarberStatus(int barberId, string newStatus)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            string cleanStatus = string.Equals(newStatus, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : "Inactive";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "UPDATE dbo.Barbers SET Status = @Status WHERE BarberId = @BarberId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", cleanStatus);
                    cmd.Parameters.AddWithValue("@BarberId", barberId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = $"Barber status updated to {cleanStatus}.";
            return RedirectToAction("Barbers");
        }

        // 4. Appointments Management
        public IActionResult Appointments()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            var appointments = new List<AdminAppointmentRowViewModel>();
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT
                        a.AppointmentId,
                        uC.FullName AS CustomerName,
                        ISNULL(uC.Phone, '') AS CustomerPhone,
                        uB.FullName AS BarberName,
                        h.StyleName,
                        ISNULL(h.Price, 0) AS Price,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        ISNULL(a.Status, 'Pending') AS Status
                    FROM dbo.Appointments a
                    JOIN dbo.Customers c ON a.CustomerId = c.CustomerId
                    JOIN dbo.Users uC ON c.UserId = uC.UserId
                    JOIN dbo.Barbers b ON a.BarberId = b.BarberId
                    JOIN dbo.Users uB ON b.UserId = uB.UserId
                    JOIN dbo.Hairstyles h ON a.StyleId = h.StyleId
                    ORDER BY a.AppointmentId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appointments.Add(new AdminAppointmentRowViewModel
                            {
                                AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                                CustomerName = reader["CustomerName"]?.ToString() ?? "",
                                CustomerPhone = reader["CustomerPhone"]?.ToString() ?? "",
                                BarberName = reader["BarberName"]?.ToString() ?? "",
                                StyleName = reader["StyleName"]?.ToString() ?? "",
                                Price = reader["Price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Price"]),
                                AppointmentDate = reader["AppointmentDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["AppointmentDate"]),
                                AppointmentTime = reader["AppointmentTime"] == DBNull.Value
                                    ? TimeSpan.Zero
                                    : reader["AppointmentTime"] is TimeSpan ts
                                        ? ts
                                        : TimeSpan.TryParse(reader["AppointmentTime"].ToString(), out TimeSpan parsed) ? parsed : TimeSpan.Zero,
                                Status = reader["Status"]?.ToString() ?? "Pending"
                            });
                        }
                    }
                }
            }

            return View(appointments);
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

        private static decimal GetDecimal(SqlConnection conn, string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                object? val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? 0m : Convert.ToDecimal(val);
            }
        }
    }
}