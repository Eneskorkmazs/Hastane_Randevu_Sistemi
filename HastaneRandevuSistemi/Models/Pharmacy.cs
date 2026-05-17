using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HastaneRandevuSistemi.Models
{
    public class Pharmacy
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Eczane adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Eczane Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres zorunludur.")]
        [StringLength(500)]
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "İl zorunludur.")]
        [StringLength(50)]
        [Display(Name = "İl")]
        public string City { get; set; } = "Sinop";

        [Required(ErrorMessage = "İlçe zorunludur.")]
        [StringLength(50)]
        [Display(Name = "İlçe")]
        public string District { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Nöbetçi mi?")]
        public bool IsOnDuty { get; set; }

        [Display(Name = "Nöbet Tarihi")]
        public DateTime? DutyDate { get; set; }

        [StringLength(10)]
        [Display(Name = "Açılış Saati")]
        public string OpenTime { get; set; } = "08:30";

        [StringLength(10)]
        [Display(Name = "Kapanış Saati")]
        public string CloseTime { get; set; } = "19:00";

        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [StringLength(200)]
        [Display(Name = "Web Sitesi")]
        public string? Website { get; set; }

        [Display(Name = "Eczane Kullanıcısı")]
        public string? UserId { get; set; }

        [Display(Name = "Enlem")]
        public double? Latitude { get; set; }

        [Display(Name = "Boylam")]
        public double? Longitude { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }
    }
}
