using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models.Admin
{
    public class AdminAppointmentViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Customer ID")]
        public string? CustomerId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        [Phone]
        [Display(Name = "Customer Phone")]
        public string? CustomerPhone { get; set; }

        [EmailAddress]
        [Display(Name = "Customer Email")]
        public string? CustomerEmail { get; set; }

        [Display(Name = "Barber ID")]
        public int BarberId { get; set; }

        [Display(Name = "Barber Name")]
        public string? BarberName { get; set; }

        [Display(Name = "Service ID")]
        public int ServiceId { get; set; }

        [Display(Name = "Service Name")]
        public string? ServiceName { get; set; }

        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        [Range(0, 100000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Appointment Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }
    }
}