using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Bölüm adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Bölüm adı en fazla 100 karakter olabilir.")] // Bölüm isimleri uzun olabilir
        [Display(Name = "Bölüm Adı")]
        public string Name { get; set; }

        [StringLength(250)] // Açıklama biraz uzun olabilir ama sınırsız olmasın
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        public virtual ICollection<Doctor>? Doctors { get; set; }
    }
}