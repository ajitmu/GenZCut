using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models.Admin
{
    public class AdminBarberViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barber name is required")]
        [StringLength(100)]
        [Display(Name = "Barber Name")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Range(0, 50)]
        [Display(Name = "Experience (Years)")]
        public int Experience { get; set; }

        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }

        [Display(Name = "Total Appointments")]
        public int TotalAppointments { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        public string? ProfileImage { get; set; }
    }
}