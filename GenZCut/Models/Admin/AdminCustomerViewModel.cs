using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models.Admin
{
    public class AdminCustomerViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime? RegistrationDate { get; set; }

        [Display(Name = "Total Appointments")]
        public int TotalAppointments { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;
    }
}