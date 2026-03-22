using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class AnnouncementCreateViewModel
    {
        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(400)]
        public string Message { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Link { get; set; }

        [Required]
        public string TargetRole { get; set; } = "All";
    }
}
