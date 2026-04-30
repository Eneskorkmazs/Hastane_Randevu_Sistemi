namespace HastaneRandevuSistemi.ViewModels
{
    public class AdminHospitalReviewsViewModel
    {
        public int TotalCount { get; set; }
        public double AverageRating { get; set; }
        public int OneStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int FiveStarCount { get; set; }
        public int? SelectedRating { get; set; }
        public string Search { get; set; } = string.Empty;
        public IReadOnlyList<AdminHospitalReviewItemViewModel> Reviews { get; set; } = Array.Empty<AdminHospitalReviewItemViewModel>();
    }

    public class AdminHospitalReviewItemViewModel
    {
        public int Id { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string? AdminReply { get; set; }
        public DateTime? AdminReplyDate { get; set; }
    }
}
