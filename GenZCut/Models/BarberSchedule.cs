using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GenZCut.Models
{
    public class BarberSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BarberId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsClosed { get; set; } = false;

        // Navigation Property
        [ForeignKey(nameof(BarberId))]
        public Barber? Barber { get; set; }
    }
}