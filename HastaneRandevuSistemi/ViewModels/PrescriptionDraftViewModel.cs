using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class PrescriptionDraftViewModel
    {
        public int AppointmentId { get; set; }

        [Display(Name = "Hasta Adı")]
        public string PatientName { get; set; } = string.Empty;

        [Display(Name = "Hasta Soyadı")]
        public string PatientSurname { get; set; } = string.Empty;

        [Display(Name = "Doktor")]
        public string DoctorName { get; set; } = string.Empty;

        [Display(Name = "Poliklinik")]
        public string DepartmentName { get; set; } = string.Empty;

        [Display(Name = "Tanı")]
        [Required(ErrorMessage = "Tanı alanı zorunludur.")]
        [StringLength(180, ErrorMessage = "Tanı en fazla 180 karakter olabilir.")]
        public string Diagnosis { get; set; } = string.Empty;

        [Display(Name = "İlaçlar")]
        [Required(ErrorMessage = "İlaçlar alanı zorunludur.")]
        [StringLength(400, ErrorMessage = "İlaçlar en fazla 400 karakter olabilir.")]
        public string Medications { get; set; } = string.Empty;

        [Display(Name = "Notlar")]
        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
        public string? Notes { get; set; }

        public DateTime PrescriptionDate { get; set; } = DateTime.Now;
    }
}

