using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-Posta zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-Posta")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public required string Password { get; set; }

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }

        [Display(Name = "Hesap Türü")]
        public string? SelectedRole { get; set; }
    }
}
