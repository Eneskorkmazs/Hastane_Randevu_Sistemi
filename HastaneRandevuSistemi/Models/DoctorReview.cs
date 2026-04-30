using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class DoctorReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }

        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
        public string? Comment { get; set; }

        [Required]
        [StringLength(120)]
        public string ReviewerName { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
