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

        // GET: /Admin/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(Admin admin)
        {
            if (!ModelState.IsValid)
            {
                return View(admin);
            }

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

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

                object? result = command.ExecuteScalar();

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

        // GET: /Admin/Dashboard
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

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")!;


            // ==========================================
            // TOTAL CUSTOMERS
            // ==========================================

            int totalCustomers = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT COUNT(*) FROM dbo.Customers";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                totalCustomers =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.TotalCustomers =
                    totalCustomers;
            }
            catch (SqlException)
            {
                ViewBag.TotalCustomers = 0;
            }


            // ==========================================
            // TOTAL APPOINTMENTS
            // ==========================================

            int totalAppointments = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT COUNT(*) FROM dbo.Appointments";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                totalAppointments =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.TotalAppointments =
                    totalAppointments;
            }
            catch (SqlException)
            {
                ViewBag.TotalAppointments = 0;
            }


            // ==========================================
            // TOTAL SERVICES / HAIRSTYLES
            // ==========================================

            int totalServices = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT COUNT(*) FROM dbo.Hairstyles";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                totalServices =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.TotalServices =
                    totalServices;
            }
            catch (SqlException)
            {
                ViewBag.TotalServices = 0;
            }

            // ==========================================
            // PENDING APPOINTMENTS
            // ==========================================

            int pendingAppointments = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query = @"
        SELECT COUNT(*)
        FROM dbo.Appointments
        WHERE Status = @Status";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Status",
                    "Pending");

                pendingAppointments =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.PendingAppointments =
                    pendingAppointments;
            }
            catch (SqlException)
            {
                ViewBag.PendingAppointments = 0;
            }
            // ==========================================
            // AVAILABLE SERVICES
            // ==========================================

            int availableServices = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT COUNT(*) FROM dbo.Hairstyles";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                availableServices =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.AvailableServices =
                    availableServices;
            }
            catch (SqlException)
            {
                ViewBag.AvailableServices = 0;
            }


            // ==========================================
            // ACTIVE BARBERS
            // ==========================================

            int activeBarbers = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT COUNT(*) FROM dbo.Barbers";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                activeBarbers =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.ActiveBarbers =
                    activeBarbers;
            }
            catch (SqlException)
            {
                ViewBag.ActiveBarbers = 0;
            }


            // ==========================================
            // COMPLETED BOOKINGS
            // ==========================================

            int completedBookings = 0;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query = @"
        SELECT COUNT(*)
        FROM dbo.Appointments
        WHERE Status = @Status";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Status",
                    "Completed");

                completedBookings =
                    Convert.ToInt32(command.ExecuteScalar());

                ViewBag.CompletedBookings =
                    completedBookings;
            }
            catch (SqlException)
            {
                ViewBag.CompletedBookings = 0;
            }
            return View();

        }

        // POST: /Admin/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}