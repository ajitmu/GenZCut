using GenZCut.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GenZCut.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string _connectionString;

        public CustomerController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO Users
                (FullName, Email, Password, Phone, Role, CreatedDate)
                VALUES
                (@FullName, @Email, @Password, @Phone, 'Customer', GETDATE())";

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@FullName", customer.FullName);

            command.Parameters.AddWithValue(
                "@Email", customer.Email);

            command.Parameters.AddWithValue(
                "@Password", customer.Password);

            command.Parameters.AddWithValue(
                "@Phone", customer.Phone);

            connection.Open();
            command.ExecuteNonQuery();

            TempData["Success"] = "Registration successful!";

            return RedirectToAction("Register");
        }
    }
}