using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminCustomerController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminCustomerController(
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Admin/AdminCustomer
        public async Task<IActionResult> Index(string search)
        {
            var users = _userManager.Users.AsQueryable();

            // Exclude Admin and Barber accounts
            var adminIds = await _userManager.GetUsersInRoleAsync("Admin");
            var barberIds = await _userManager.GetUsersInRoleAsync("Barber");

            var excludedIds = adminIds
                .Select(u => u.Id)
                .Concat(barberIds.Select(u => u.Id))
                .ToList();

            users = users.Where(u => !excludedIds.Contains(u.Id));

            // Search customers
            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.UserName.Contains(search)
                    u.Email.Contains(search));
            }

            ViewBag.Search = search;

            var customers = await users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            return View(customers);
        }

        // GET: Admin/AdminCustomer/Details/id
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var customer = await _userManager.FindByIdAsync(id);

            if (customer == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(customer, "Admin")
                await _userManager.IsInRoleAsync(customer, "Barber"))
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Admin/AdminCustomer/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var customer = await _userManager.FindByIdAsync(id);

            if (customer == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(customer, "Admin") ||
                await _userManager.IsInRoleAsync(customer, "Barber"))
            {
                return Forbid();
            }

            var result = await _userManager.DeleteAsync(customer);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to delete customer account.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Customer deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}