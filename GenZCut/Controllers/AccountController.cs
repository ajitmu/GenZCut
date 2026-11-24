using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using GenZCut.Models;

namespace GenZCut.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        // ==========================================
        // 1. COMMON PORTAL (Admin, Customer, Barber)
        // ==========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT u.UserId, u.FullName, u.Role, b.BarberId 
                    FROM Users u 
                    LEFT JOIN Barbers b ON u.UserId = b.UserId 
                    WHERE u.Email = @Email AND u.Password = @Password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string role = reader["Role"].ToString() ?? "";
                            int userId = Convert.ToInt32(reader["UserId"]);
                            string fullName = reader["FullName"].ToString() ?? "";

                            HttpContext.Session.SetInt32("UserId", userId);
                            HttpContext.Session.SetString("UserName", fullName);
                            HttpContext.Session.SetString("UserRole", role);

                            if (role == "Barber")
                            {
                                if (reader["BarberId"] != DBNull.Value)
                                {
                                    int barberId = Convert.ToInt32(reader["BarberId"]);
                                    HttpContext.Session.SetInt32("BarberId", barberId);
                                }
                                return RedirectToAction("Dashboard", "Barber");
                            }
                            else if (role == "Admin")
                            {
                                return RedirectToAction("Dashboard", "Admin");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            ViewBag.Error = "Invalid Email or Password!";
                        }
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", model.Email);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ViewBag.Error = "Email is already registered!";
                        return View(model);
                    }
                }

                string insertUserQuery = @"
                    INSERT INTO Users (FullName, Email, Phone, Password, Role) 
                    VALUES (@FullName, @Email, @Phone, @Password, @Role);
                    SELECT SCOPE_IDENTITY();";

                int newUserId;
                using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    cmd.Parameters.AddWithValue("@Role", model.Role);

                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (model.Role == "Barber")
                {
                    string insertBarberQuery = "INSERT INTO Barbers (UserId, IsAvailable) VALUES (@UserId, 1)";
                    using (SqlCommand barberCmd = new SqlCommand(insertBarberQuery, conn))
                    {
                        barberCmd.Parameters.AddWithValue("@UserId", newUserId);
                        barberCmd.ExecuteNonQuery();
                    }
                }
                else if (model.Role == "Customer")
                {
                    string insertCustomerQuery = "INSERT INTO Customers (UserId) VALUES (@UserId)";
                    using (SqlCommand customerCmd = new SqlCommand(insertCustomerQuery, conn))
                    {
                        customerCmd.Parameters.AddWithValue("@UserId", newUserId);
                        customerCmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMsg"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
        }

        // ==========================================
        // 2. BARBER PARTNER PORTAL (Barber Only)
        // ==========================================

        [HttpGet]
        public IActionResult BarberLogin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult BarberLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT u.UserId, u.FullName, u.Role, b.BarberId 
                    FROM Users u 
                    LEFT JOIN Barbers b ON u.UserId = b.UserId 
                    WHERE u.Email = @Email AND u.Password = @Password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string role = reader["Role"].ToString() ?? "";

                            // Sirf Barber ko hi allow karein
                            if (role != "Barber")
                            {
                                ViewBag.Error = "Access Denied! This portal is only for Barbers.";
                                return View(model);
                            }

                            int userId = Convert.ToInt32(reader["UserId"]);
                            string fullName = reader["FullName"].ToString() ?? "";

                            HttpContext.Session.SetInt32("UserId", userId);
                            HttpContext.Session.SetString("UserName", fullName);
                            HttpContext.Session.SetString("UserRole", role);

                            if (reader["BarberId"] != DBNull.Value)
                            {
                                int barberId = Convert.ToInt32(reader["BarberId"]);
                                HttpContext.Session.SetInt32("BarberId", barberId);
                            }

                            return RedirectToAction("Dashboard", "Barber");
                        }
                        else
                        {
                            ViewBag.Error = "Invalid Email or Password!";
                        }
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult BarberRegister()
        {
            return View();
        }

        [HttpPost]
        public IActionResult BarberRegister(RegisterViewModel model)
        {
            // Force Role to Barber
            model.Role = "Barber";
            ModelState.Remove("Role");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", model.Email);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ViewBag.Error = "Email is already registered!";
                        return View(model);
                    }
                }

                string insertUserQuery = @"
                    INSERT INTO Users (FullName, Email, Phone, Password, Role) 
                    VALUES (@FullName, @Email, @Phone, @Password, 'Barber');
                    SELECT SCOPE_IDENTITY();";

                int newUserId;
                using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string insertBarberQuery = "INSERT INTO Barbers (UserId, IsAvailable) VALUES (@UserId, 1)";
                using (SqlCommand barberCmd = new SqlCommand(insertBarberQuery, conn))
                {
                    barberCmd.Parameters.AddWithValue("@UserId", newUserId);
                    barberCmd.ExecuteNonQuery();
                }

                TempData["SuccessMsg"] = "Barber registration successful! Please login.";
                return RedirectToAction("BarberLogin");
            }
        }

        // ==========================================
        // 3. LOGOUT LOGIC
        // ==========================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}