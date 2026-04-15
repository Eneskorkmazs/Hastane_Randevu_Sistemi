namespace HastaneRandevuSistemi.ViewModels
{
    public class PatientPrescriptionsViewModel
    {
        public IReadOnlyList<PatientPrescriptionItemViewModel> Items { get; set; } = Array.Empty<PatientPrescriptionItemViewModel>();
    }

    public class PatientPrescriptionItemViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
