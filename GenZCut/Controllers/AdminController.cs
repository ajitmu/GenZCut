using GenZCut.Models;
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

        // =====================================================
        // CONNECTION STRING
        // =====================================================

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")!;


        // =====================================================
        // ADMIN LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =====================================================
        // ADMIN LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(Admin admin)
        {
            if (!ModelState.IsValid)
            {
                return View(admin);
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(ConnectionString);

                connection.Open();

                string query = @"
                    SELECT AdminId
                    FROM Admins
                    WHERE Email = @Email
                    AND Password = @Password";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Email",
                    admin.Email);

                command.Parameters.AddWithValue(
                    "@Password",
                    admin.Password);

                object? result =
                    command.ExecuteScalar();

                if (result != null)
                {
                    HttpContext.Session.SetInt32(
                        "AdminId",
                        Convert.ToInt32(result));

                    HttpContext.Session.SetString(
                        "AdminEmail",
                        admin.Email);

                    return RedirectToAction("Dashboard");
                }

                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(admin);
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the database.");

                return View(admin);
            }
        }


        // =====================================================
        // ADMIN DASHBOARD
        // =====================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            int? adminId =
                HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.AdminEmail =
                HttpContext.Session.GetString("AdminEmail");


            // ==========================================
            // TOTAL CUSTOMERS
            // ==========================================

            ViewBag.TotalCustomers = GetCount(
                "SELECT COUNT(*) FROM dbo.Customers");


            // ==========================================
            // TOTAL APPOINTMENTS
            // ==========================================

            ViewBag.TotalAppointments = GetCount(
                "SELECT COUNT(*) FROM dbo.Appointments");


            // ==========================================
            // TOTAL SERVICES
            // ==========================================

            ViewBag.TotalServices = GetCount(
                "SELECT COUNT(*) FROM dbo.Hairstyles");


            // ==========================================
            // PENDING APPOINTMENTS
            // ==========================================

            ViewBag.PendingAppointments = GetCount(
                @"SELECT COUNT(*)
                  FROM dbo.Appointments
                  WHERE Status = 'Pending'");


            // ==========================================
            // AVAILABLE SERVICES
            // ==========================================

            ViewBag.AvailableServices = GetCount(
                "SELECT COUNT(*) FROM dbo.Hairstyles");


            // ==========================================
            // ACTIVE BARBERS
            // ==========================================

            ViewBag.ActiveBarbers = GetCount(
                @"SELECT COUNT(*)
                  FROM dbo.Barbers
                  WHERE Status = 'Active'");


            // ==========================================
            // COMPLETED BOOKINGS
            // ==========================================

            ViewBag.CompletedBookings = GetCount(
                @"SELECT COUNT(*)
                  FROM dbo.Appointments
                  WHERE Status = 'Completed'");


            return View();
        }


        // =====================================================
        // HELPER - GET COUNT
        // =====================================================

        private int GetCount(string query)
        {
            try
            {
                using SqlConnection connection =
                    new SqlConnection(ConnectionString);

                connection.Open();

                using SqlCommand command =
                    new SqlCommand(query, connection);

                return Convert.ToInt32(
                    command.ExecuteScalar());
            }
            catch (SqlException)
            {
                return 0;
            }
        }


        // =====================================================
        // APPOINTMENT MANAGEMENT - GET
        // =====================================================

        [HttpGet]
        public IActionResult Appointments()
        {
            int? adminId =
                HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }

            List<AdminAppointmentViewModel> appointments =
                new List<AdminAppointmentViewModel>();

            try
            {
                using SqlConnection connection =
                    new SqlConnection(ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        a.AppointmentId,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        a.Status,

                        u.FullName AS CustomerName,
                        u.Phone AS CustomerPhone,

                        b.Specialization,
                        b.Experience,

                        bu.FullName AS BarberName,

                        h.StyleName,
                        h.Price,
                        h.Duration

                    FROM Appointments a

                    INNER JOIN Customers c
                        ON a.CustomerId = c.CustomerId

                    INNER JOIN Users u
                        ON c.UserId = u.UserId

                    INNER JOIN Barbers b
                        ON a.BarberId = b.BarberId

                    INNER JOIN Users bu
                        ON b.UserId = bu.UserId

                    INNER JOIN Hairstyles h
                        ON a.StyleId = h.StyleId

                    ORDER BY
                        a.AppointmentDate DESC,
                        a.AppointmentTime DESC";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                using SqlDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    appointments.Add(
                        new AdminAppointmentViewModel
                        {
                            AppointmentId =
                                Convert.ToInt32(
                                    reader["AppointmentId"]),

                            AppointmentDate =
                                Convert.ToDateTime(
                                    reader["AppointmentDate"]),

                            AppointmentTime =
                                (TimeSpan)
                                    reader["AppointmentTime"],

                            Status =
                                reader["Status"] == DBNull.Value
                                    ? "Pending"
                                    : reader["Status"].ToString()
                                      ?? "Pending",

                            CustomerName =
                                reader["CustomerName"] == DBNull.Value
                                    ? ""
                                    : reader["CustomerName"].ToString()
                                      ?? "",

                            CustomerPhone =
                                reader["CustomerPhone"] == DBNull.Value
                                    ? ""
                                    : reader["CustomerPhone"].ToString()
                                      ?? "",

                            BarberName =
                                reader["BarberName"] == DBNull.Value
                                    ? ""
                                    : reader["BarberName"].ToString()
                                      ?? "",

                            Specialization =
                                reader["Specialization"] == DBNull.Value
                                    ? ""
                                    : reader["Specialization"].ToString()
                                      ?? "",

                            Experience =
                                reader["Experience"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        reader["Experience"]),

                            StyleName =
                                reader["StyleName"] == DBNull.Value
                                    ? ""
                                    : reader["StyleName"].ToString()
                                      ?? "",

                            Price =
                                reader["Price"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        reader["Price"]),

                            Duration =
                                reader["Duration"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        reader["Duration"])
                        });
                }
            }
            catch (SqlException)
            {
                ViewBag.Error =
                    "Unable to load appointments.";
            }

            return View(appointments);
        }


        // =====================================================
        // UPDATE APPOINTMENT STATUS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAppointmentStatus(
            int appointmentId,
            string status)
        {
            int? adminId =
                HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login");
            }


            // Only allowed statuses

            string[] allowedStatuses =
            {
                "Pending",
                "Confirmed",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] =
                    "Invalid appointment status.";

                return RedirectToAction("Appointments");
            }


            try
            {
                using SqlConnection connection =
                    new SqlConnection(ConnectionString);

                connection.Open();

                string query = @"
                    UPDATE Appointments
                    SET Status = @Status
                    WHERE AppointmentId = @AppointmentId";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Status",
                    status);

                command.Parameters.AddWithValue(
                    "@AppointmentId",
                    appointmentId);

                int rows =
                    command.ExecuteNonQuery();

                if (rows > 0)
                {
                    TempData["Success"] =
                        "Appointment status updated successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Appointment not found.";
                }
            }
            catch (SqlException)
            {
                TempData["Error"] =
                    "Unable to update appointment status.";
            }

            return RedirectToAction("Appointments");
        }


        // =====================================================
        // ADMIN LOGOUT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}