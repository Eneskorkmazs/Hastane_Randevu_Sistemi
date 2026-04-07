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

        public bool IsCollected { get; set; }
        public DateTime? CollectedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        public bool AdminAccessRequested { get; set; }
        public DateTime? AdminAccessRequestedDate { get; set; }

        [StringLength(450)]
        public string? AdminAccessRequestedByUserId { get; set; }

        [StringLength(200)]
        public string? AdminAccessRequestedByName { get; set; }

        public bool AdminAccessGranted { get; set; }
        public DateTime? AdminAccessGrantedDate { get; set; }

        [StringLength(450)]
        public string? AdminAccessGrantedByUserId { get; set; }

        [StringLength(200)]
        public string? AdminAccessGrantedByName { get; set; }

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        [StringLength(200)]
        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(450)]
        public string? CancelledByUserId { get; set; }

        [StringLength(200)]
        public string? CancelledByName { get; set; }

        public DateTime? CancelledDate { get; set; }

        public virtual ICollection<MedicalReport> MedicalReports { get; set; } = new List<MedicalReport>();
    }
}
