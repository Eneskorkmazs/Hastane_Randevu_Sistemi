using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doktor adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")] // Sınır
        [Display(Name = "Ad")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Doktor soyadı zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")] // Sınır
        [Display(Name = "Soyad")]
        public string Surname { get; set; }

        [StringLength(20)] // Unvan (Prof. Dr. vs) çok uzun olamaz
        [Display(Name = "Unvan")]
        public string Title { get; set; } = "Uzm. Dr.";

        [Display(Name = "Poliklinik/Bölüm")]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        public string? UserId { get; set; } 

        public virtual ICollection<Appointment>? Appointments { get; set; }
    }
}