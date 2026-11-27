using Microsoft.AspNetCore.Identity;
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

        private static bool VerifyUserPassword(string storedPassword, string enteredPassword)
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

            string emailInput = model.Email.Trim();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                string query = @"
                    SELECT u.UserId, u.FullName, u.Email, u.Password, u.Role, b.BarberId, c.CustomerId
                    FROM Users u 
                    LEFT JOIN Barbers b ON u.UserId = b.UserId 
                    LEFT JOIN Customers c ON u.UserId = c.UserId
                    WHERE u.Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", emailInput);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader["Password"].ToString() ?? "";
                            if (!VerifyUserPassword(storedPassword, model.Password))
                            {
                                ViewBag.Error = "Invalid Email or Password!";
                                return View(model);
                            }

                            string role = (reader["Role"].ToString() ?? "").Trim();
                            int userId = Convert.ToInt32(reader["UserId"]);
                            string fullName = reader["FullName"].ToString() ?? "";
                            string email = reader["Email"].ToString() ?? "";

                            HttpContext.Session.SetInt32("UserId", userId);
                            HttpContext.Session.SetString("UserName", fullName);
                            HttpContext.Session.SetString("UserRole", role);

                            if (string.Equals(role, "Barber", StringComparison.OrdinalIgnoreCase))
                            {
                                if (reader["BarberId"] != DBNull.Value)
                                {
                                    HttpContext.Session.SetInt32("BarberId", Convert.ToInt32(reader["BarberId"]));
                                }
                                return RedirectToAction("Dashboard", "Barber");
                            }
                            else if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                HttpContext.Session.SetString("AdminEmail", email);
                                HttpContext.Session.SetString("AdminName", fullName);
                                HttpContext.Session.SetInt32("AdminId", userId);
                                return RedirectToAction("Dashboard", "Admin");
                            }
                            else
                            {
                                HttpContext.Session.SetString("CustomerEmail", email);
                                HttpContext.Session.SetString("CustomerName", fullName);
                                if (reader["CustomerId"] != DBNull.Value)
                                {
                                    HttpContext.Session.SetInt32("CustomerId", Convert.ToInt32(reader["CustomerId"]));
                                }
                                return RedirectToAction("Dashboard", "Customer");
                            }
                        }
                    }
                }

                ViewBag.Error = "Invalid Email or Password!";
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? role = null)
        {
            var model = new RegisterViewModel();
            if (!string.IsNullOrWhiteSpace(role))
            {
                model.Role = role;
            }
            return View(model);
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
                    checkCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ViewBag.Error = "Email is already registered!";
                        return View(model);
                    }
                }

                var hasher = new PasswordHasher<object>();
                string hashedPassword = hasher.HashPassword(new object(), model.Password);

                string insertUserQuery = @"
                    INSERT INTO Users (FullName, Email, Phone, Password, Role) 
                    VALUES (@FullName, @Email, @Phone, @Password, @Role);
                    SELECT SCOPE_IDENTITY();";

                int newUserId;
                using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Role", model.Role);

                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (model.Role == "Barber")
                {
                    string insertBarberQuery = "INSERT INTO Barbers (UserId, Status) VALUES (@UserId, 'Active')";
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
                return RedirectToAction(model.Role == "Admin" ? "AdminLogin" : "Login");
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
                    SELECT u.UserId, u.FullName, u.Password, u.Role, b.BarberId 
                    FROM Users u 
                    LEFT JOIN Barbers b ON u.UserId = b.UserId 
                    WHERE u.Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", model.Email.Trim());

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader["Password"].ToString() ?? "";
                            if (!VerifyUserPassword(storedPassword, model.Password))
                            {
                                ViewBag.Error = "Invalid Email or Password!";
                                return View(model);
                            }

                            string role = reader["Role"].ToString() ?? "";

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
                    checkCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ViewBag.Error = "Email is already registered!";
                        return View(model);
                    }
                }

                var hasher = new PasswordHasher<object>();
                string hashedPassword = hasher.HashPassword(new object(), model.Password);

                string insertUserQuery = @"
                    INSERT INTO Users (FullName, Email, Phone, Password, Role) 
                    VALUES (@FullName, @Email, @Phone, @Password, 'Barber');
                    SELECT SCOPE_IDENTITY();";

                int newUserId;
                using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);

                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string insertBarberQuery = "INSERT INTO Barbers (UserId, Status) VALUES (@UserId, 'Active')";
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
        // 2B. ADMIN PORTAL (Admin Only)
        // ==========================================

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string emailInput = model.Email.Trim();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = @"
                    SELECT UserId, FullName, Email, Password, Role 
                    FROM Users 
                    WHERE Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", emailInput);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader["Password"].ToString() ?? "";
                            if (!VerifyUserPassword(storedPassword, model.Password))
                            {
                                ViewBag.Error = "Invalid Email or Password!";
                                return View(model);
                            }

                            string role = (reader["Role"].ToString() ?? "").Trim();

                            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                ViewBag.Error = "Access Denied! This portal is only for Admins.";
                                return View(model);
                            }

                            int userId = Convert.ToInt32(reader["UserId"]);
                            string fullName = reader["FullName"].ToString() ?? "";
                            string email = reader["Email"].ToString() ?? emailInput;

                            HttpContext.Session.SetInt32("UserId", userId);
                            HttpContext.Session.SetString("UserName", fullName);
                            HttpContext.Session.SetString("UserRole", "Admin");
                            HttpContext.Session.SetString("AdminId", userId.ToString());
                            HttpContext.Session.SetInt32("AdminUserId", userId);
                            HttpContext.Session.SetString("AdminEmail", email);
                            HttpContext.Session.SetString("AdminName", fullName);

                            return RedirectToAction("Dashboard", "Admin");
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