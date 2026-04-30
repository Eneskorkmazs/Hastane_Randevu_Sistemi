using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class HospitalReview
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "Ad-soyad zorunludur.")]
        [StringLength(120, ErrorMessage = "Ad-soyad en fazla 120 karakter olabilir.")]
        public string ReviewerName { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(1000)]
        public string? AdminReply { get; set; }
        
        public DateTime? AdminReplyDate { get; set; }
    }
}
