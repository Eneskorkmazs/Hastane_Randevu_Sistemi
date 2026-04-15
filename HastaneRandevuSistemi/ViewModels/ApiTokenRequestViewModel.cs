using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class ApiTokenRequestViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
