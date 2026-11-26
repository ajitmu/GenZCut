using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models
{
    public class Barber
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? UserId { get; set; }

        // Navigation Properties
        public ICollection<Service> Services { get; set; }
            = new List<Service>();

        public ICollection<BarberSchedule> BarberSchedules { get; set; }
            = new List<BarberSchedule>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}