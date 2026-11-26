using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models.Admin
{
    public class AdminServiceViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service name is required")]
        [StringLength(100)]
        [Display(Name = "Service Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 100000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(15, 240)]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Barber ID")]
        public int BarberId { get; set; }

        [Display(Name = "Barber Name")]
        public string? BarberName { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Total Bookings")]
        public int TotalBookings { get; set; }
    }
}