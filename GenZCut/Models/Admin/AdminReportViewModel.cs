namespace GenZCut.Models.Admin
{
    public class AdminReportViewModel
    {
        public int SelectedYear { get; set; }

        public int SelectedMonth { get; set; }

        public int TotalAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public int PendingAppointments { get; set; }

        public int CancelledAppointments { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal MonthlyRevenue { get; set; }

        public int TotalCustomers { get; set; }

        public int TotalBarbers { get; set; }

        public List<MonthlyReportViewModel> MonthlyReports
        {
            get;
            set;
        } = new();

        public List<BarberPerformanceViewModel> BarberPerformance
        {
            get;
            set;
        } = new();
    }

    public class MonthlyReportViewModel
    {
        public string MonthName { get; set; } = string.Empty;

        public int TotalAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public decimal Revenue { get; set; }
    }

    public class BarberPerformanceViewModel
    {
        public int BarberId { get; set; }

        public string BarberName { get; set; } = string.Empty;

        public int TotalAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public decimal TotalRevenue { get; set; }
    }
}