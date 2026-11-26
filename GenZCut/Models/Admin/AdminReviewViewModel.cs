using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models.Admin
{
    public class AdminReviewViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Customer ID")]
        public string? CustomerId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000)]
        [Display(Name = "Review Comment")]
        public string? Comment { get; set; }

        [Display(Name = "Review Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Barber Name")]
        public string? BarberName { get; set; }

        [Display(Name = "Service Name")]
        public string? ServiceName { get; set; }

        [Display(Name = "Review Status")]
        public bool IsVisible { get; set; } = true;
    }
}