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

        // 1. Login Page Show Karein
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. Login Form Submit Logic
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                // Users aur Barbers table se detail fetch karein
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

                            // Session Values Store Karein
                            HttpContext.Session.SetInt32("UserId", userId);
                            HttpContext.Session.SetString("UserName", fullName);
                            HttpContext.Session.SetString("UserRole", role);

                            // Role ke hisab se redirection
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
                                // Customer ke liye
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

        // 3. Register Page Show Karein
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // 4. Register Form Submit Logic (Barber, Customer & Admin)
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

                // Check if email already exists
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

                // Insert into Users table and get new UserId
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

                // Role ke hisab se respective table mein entry karein
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
                // Note: Agar Role == "Admin" hai toh sirf Users table mein entry kafi hai

                TempData["SuccessMsg"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
        }

        // 5. Logout Logic
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}