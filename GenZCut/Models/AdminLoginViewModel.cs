using System.ComponentModel.DataAnnotations;

namespace GenZCut.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Admin email required hai.")]
        [EmailAddress(ErrorMessage = "Valid email address daalein.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password required hai.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}