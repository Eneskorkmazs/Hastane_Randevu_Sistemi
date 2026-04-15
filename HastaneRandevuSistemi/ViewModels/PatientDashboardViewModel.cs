using HastaneRandevuSistemi.Models;

namespace HastaneRandevuSistemi.ViewModels
{
    public class PatientDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? TC { get; set; }
        public DateTime? DogumTarihi { get; set; }
        public string? Cinsiyet { get; set; }
        public int PendingAppointmentsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int CancelledAppointmentsCount { get; set; }
        public int PrescriptionCount { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public int MedicalHistoryCount { get; set; }
        public IReadOnlyList<DepartmentFeeItem> DepartmentFees { get; set; } = Array.Empty<DepartmentFeeItem>();
        public IReadOnlyList<Appointment> PendingAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Appointment> RecentAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Notification> RecentNotifications { get; set; } = Array.Empty<Notification>();
    }

    public class DepartmentFeeItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal Fee { get; set; }
    }
}
