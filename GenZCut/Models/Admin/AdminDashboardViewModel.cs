namespace GenZCut.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalCustomers { get; set; }

        public int TotalBarbers { get; set; }

        public int TotalServices { get; set; }

        public int TotalAppointments { get; set; }

        public int PendingAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public int CancelledAppointments { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<RecentAppointmentViewModel>
            RecentAppointments
        { get; set; } = new();
    }

    public class RecentAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string BarberName { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;

        public DateTime AppointmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}