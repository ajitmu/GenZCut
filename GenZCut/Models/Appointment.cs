using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a barber.")]
        public int BarberId { get; set; }

        [Required(ErrorMessage = "Please select a hairstyle/service.")]
        public int StyleId { get; set; }

        [Required(ErrorMessage = "Please select an appointment date.")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select an appointment time.")]
        public TimeSpan AppointmentTime { get; set; }

        public string Status { get; set; } = "Pending";

        // Display information for My Appointments
        public string StyleName { get; set; } = "";

        public string BarberName { get; set; } = "";

        public decimal Price { get; set; }

        public int Duration { get; set; }

        public string Specialization { get; set; } = "";

        public int Experience { get; set; }
    }
}