using HastaneRandevuSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class PatientMedicalHistoryViewModel
    {
        [Required(ErrorMessage = "Kayit basligi zorunludur.")]
        [StringLength(120)]
        [Display(Name = "Kayit Basligi")]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Tani")]
        public string? Diagnosis { get; set; }

        [StringLength(500)]
        [Display(Name = "Ilaclar")]
        public string? Medications { get; set; }

        [StringLength(500)]
        [Display(Name = "Alerji Bilgisi")]
        public string? AllergyInfo { get; set; }

        [StringLength(2000)]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        [Display(Name = "Muayene Tarihi")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        public IReadOnlyList<MedicalHistory> Records { get; set; } = Array.Empty<MedicalHistory>();
        public IReadOnlyList<PatientPrescriptionItemViewModel> Prescriptions { get; set; } = Array.Empty<PatientPrescriptionItemViewModel>();
        public int PrescriptionCount => Prescriptions.Count;
    }
}
