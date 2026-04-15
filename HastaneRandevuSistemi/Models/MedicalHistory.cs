using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class MedicalHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Diagnosis { get; set; }

        [StringLength(500)]
        public string? Medications { get; set; }

        [StringLength(500)]
        public string? AllergyInfo { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public DateTime VisitDate { get; set; } = DateTime.Today;

        [StringLength(255)]
        public string? AttachmentName { get; set; }

        [StringLength(1000)]
        public string? AttachmentPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
