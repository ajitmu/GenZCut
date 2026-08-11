namespace GenZCut.Models
{
    public class AdminAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public string Status { get; set; } = "Pending";

        public string CustomerName { get; set; } = "";

        public string CustomerPhone { get; set; } = "";

        public string BarberName { get; set; } = "";

        public string Specialization { get; set; } = "";

        public int Experience { get; set; }

        public string StyleName { get; set; } = "";

        public decimal Price { get; set; }

        public int Duration { get; set; }
    }
}