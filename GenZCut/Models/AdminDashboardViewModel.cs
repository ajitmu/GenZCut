namespace GenZCut.Models
{
    public class AdminDashboardViewModel
    {
        public string AdminEmail { get; set; } = string.Empty;
        public int TotalCustomers { get; set; }
        public int TotalBarbers { get; set; }
        public int ActiveBarbers { get; set; }
        public int TotalHairstyles { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal ProjectedRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public List<AdminAppointmentRowViewModel> RecentAppointments { get; set; } = new();
        public List<AdminFeedbackRowViewModel> RecentFeedback { get; set; } = new();
    }

    public class AdminAppointmentRowViewModel
    {
        public int AppointmentId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string BarberName { get; set; } = string.Empty;
        public string StyleName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class AdminFeedbackRowViewModel
    {
        public int FeedbackId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string BarberName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}