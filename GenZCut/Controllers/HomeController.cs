using System.Diagnostics;
using GenZCut.Models;
using GenZCut.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GenZCut.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Database _database;

        public HomeController(
            ILogger<HomeController> logger,
            Database database)
        {
            _logger = logger;
            _database = database;
        }

        public IActionResult Index()
        {
            try
            {
                using (SqlConnection connection = _database.GetConnection())
                {
                    connection.Open();

                    ViewBag.Message = "Database Connected Successfully!";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Database Connection Failed: " + ex.Message;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}