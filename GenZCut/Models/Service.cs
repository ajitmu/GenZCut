using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GenZCut.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Range(15, 240)]
        public int DurationMinutes { get; set; }

        public int BarberId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Property
        [ForeignKey(nameof(BarberId))]
        public Barber? Barber { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}