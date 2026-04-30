using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class PatientProfileViewModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [Display(Name = "Ad")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [Display(Name = "Soyad")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "TC kimlik numarası zorunludur.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "TC kimlik numarası 11 haneli olmalıdır.")]
        [Display(Name = "TC Kimlik No")]
        public string TC { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [Phone(ErrorMessage = "Geçerli bir telefon giriniz.")]
        [StringLength(15, ErrorMessage = "Telefon en fazla 15 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string Telefon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Doğum Tarihi")]
        public DateTime? DogumTarihi { get; set; }

        [Required(ErrorMessage = "Cinsiyet zorunludur.")]
        [Display(Name = "Cinsiyet")]
        public string Cinsiyet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres zorunludur.")]
        [StringLength(250)]
        [Display(Name = "Adres")]
        public string Adres { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-Posta")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Kan Grubu")]
        public string? BloodType { get; set; }

        [Display(Name = "Alerjiler")]
        public string? Allergies { get; set; }

        [Display(Name = "Acil Durum Kişisi")]
        public string? EmergencyContact { get; set; }
    }
}

