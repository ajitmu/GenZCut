using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace GenZCut.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        public string? CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int BarberId { get; set; }

        public int ServiceId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public DateTime? AppointmentDateTime { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public AppointmentStatus Status { get; set; }
            = AppointmentStatus.Pending;

        public string? Notes { get; set; }

        // Navigation Properties
        public Service? Service { get; set; }

        public Barber? Barber { get; set; }
    }

    public enum AppointmentStatus
    {
        Pending,
        Accepted,
        Rejected,
        Completed,
        Cancelled
    }
}