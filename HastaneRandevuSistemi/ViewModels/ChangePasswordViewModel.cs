using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut sifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Sifre")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni sifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Sifre")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni sifre tekrari zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Sifre Tekrar")]
        [Compare(nameof(NewPassword), ErrorMessage = "Yeni sifreler uyusmuyor.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
