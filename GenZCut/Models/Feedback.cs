using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int BarberId { get; set; }

        public int? Rating { get; set; }

        [StringLength(300)]
        public string? Comment { get; set; }
    }
}