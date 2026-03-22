using HastaneRandevuSistemi.Models;

namespace HastaneRandevuSistemi.ViewModels
{
    public class AdminReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? DepartmentId { get; set; }
        public AppointmentStatus? Status { get; set; }

        public int TotalAppointments { get; set; }
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int CancelledAppointments { get; set; }
        public int CompletedAppointments { get; set; }

        public IReadOnlyList<DepartmentStatItem> DepartmentStats { get; set; } = Array.Empty<DepartmentStatItem>();
        public IReadOnlyList<Appointment> RecentAppointments { get; set; } = Array.Empty<Appointment>();
    }
}
