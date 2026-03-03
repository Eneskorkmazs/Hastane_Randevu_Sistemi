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
        public int UpcomingAppointmentsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int CancelledAppointmentsCount { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public IReadOnlyList<Appointment> UpcomingAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Appointment> RecentAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Notification> RecentNotifications { get; set; } = Array.Empty<Notification>();
    }
}
