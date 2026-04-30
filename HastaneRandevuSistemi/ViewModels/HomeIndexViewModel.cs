using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class HomeIndexViewModel
    {
        [Range(1, 5, ErrorMessage = "Lütfen 1-5 arasında puan seçin.")]
        [Display(Name = "Puan")]
        public int Rating { get; set; }

        [Display(Name = "Ad Soyad")]
        [StringLength(120, ErrorMessage = "Ad-soyad en fazla 120 karakter olabilir.")]
        public string ReviewerName { get; set; } = string.Empty;

        [Display(Name = "Yorum")]
        [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
        public string? Comment { get; set; }

        public int TotalReviewCount { get; set; }
        public double AverageRating { get; set; }
        public bool HasCurrentUserReview { get; set; }
        public IReadOnlyList<HomeReviewItemViewModel> RecentReviews { get; set; } = Array.Empty<HomeReviewItemViewModel>();
    }

    public class HomeReviewItemViewModel
    {
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AdminReply { get; set; }
        public DateTime? AdminReplyDate { get; set; }
    }
}
