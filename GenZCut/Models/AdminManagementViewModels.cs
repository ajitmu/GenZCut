using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models
{
    public class AdminAppointmentsViewModel
    {
        public string StatusFilter { get; set; } = "All";
        public string SearchTerm { get; set; } = string.Empty;
        public List<AdminAppointmentRowViewModel> Appointments { get; set; } = new();
    }

    public class BarberManageItem
    {
        public int BarberId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? Experience { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int CompletedBookings { get; set; }
    }

    public class AdminBarbersViewModel
    {
        public List<BarberManageItem> Barbers { get; set; } = new();
    }

    public class HairstyleItem
    {
        public int StyleId { get; set; }

        [Required(ErrorMessage = "Style name zaroori hai.")]
        [StringLength(100)]
        public string StyleName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Range(1, 100000, ErrorMessage = "Valid price daalein.")]
        public decimal Price { get; set; }

        [Range(5, 480, ErrorMessage = "Duration minutes mein (5-480) daalein.")]
        public int Duration { get; set; }

        [StringLength(255)]
        public string? Image { get; set; }
    }

    public class AdminHairstylesViewModel
    {
        public List<HairstyleItem> Styles { get; set; } = new();
        public HairstyleItem NewStyle { get; set; } = new();
    }
}