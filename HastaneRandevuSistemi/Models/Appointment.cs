using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Randevu Tarihi")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        [Display(Name = "Hasta Adı")]
        public string PatientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        [Display(Name = "Hasta Soyadı")]
        public string PatientSurname { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "Telefon numarası çok uzun.")]
        [Display(Name = "Telefon")]
        public string? PatientPhone { get; set; }

        [Display(Name = "Hasta Kullanıcısı")]
        public string? PatientUserId { get; set; }

        [ForeignKey(nameof(PatientUserId))]
        public virtual AppUser? PatientUser { get; set; }

        [Display(Name = "Doktor")]
        public int DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public virtual Doctor? Doctor { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Bekliyor;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
