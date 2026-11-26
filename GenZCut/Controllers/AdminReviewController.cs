using GenZCut.Data;
using GenZCut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCut.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminReview
        public async Task<IActionResult> Index(
            int? rating,
            string? search)
        {
            var reviews = _context.Reviews.AsQueryable();

            // Filter by rating
            if (rating.HasValue)
            {
                reviews = reviews.Where(r => r.Rating == rating.Value);
            }

            // Search reviews
            if (!string.IsNullOrWhiteSpace(search))
            {
                reviews = reviews.Where(r =>
                    r.Comment.Contains(search));
            }

            ViewBag.Rating = rating;
            ViewBag.Search = search;

            var reviewList = await reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviewList);
        }

        // GET: Admin/AdminReview/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                return NotFound();

            return View(review);
        }

        // POST: Admin/AdminReview/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
                return NotFound();

            _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}