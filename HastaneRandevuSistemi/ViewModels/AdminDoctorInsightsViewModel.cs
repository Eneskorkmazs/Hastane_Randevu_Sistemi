using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HastaneRandevuSistemi.ViewModels
{
    public class AdminDoctorInsightsViewModel
    {
        public int? DoctorId { get; set; }
        public string Period { get; set; } = "monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string SelectedDoctorName { get; set; } = "Tum Doktorlar";
        public string SelectedDepartmentName { get; set; } = "Tum Bolumler";

        public int TotalAppointments { get; set; }
        public int UniquePatientCount { get; set; }
        public int CompletedAppointments { get; set; }
        public int ApprovedAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public decimal CollectionRate { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }

        public IReadOnlyList<SelectListItem> DoctorOptions { get; set; } = Array.Empty<SelectListItem>();
        public IReadOnlyList<DoctorInsightTrendItem> Trend { get; set; } = Array.Empty<DoctorInsightTrendItem>();
        public IReadOnlyList<DoctorStatusBreakdownItem> StatusBreakdown { get; set; } = Array.Empty<DoctorStatusBreakdownItem>();
        public IReadOnlyList<DoctorDepartmentBreakdownItem> DepartmentBreakdown { get; set; } = Array.Empty<DoctorDepartmentBreakdownItem>();
        public IReadOnlyList<DoctorTopPatientItem> TopPatients { get; set; } = Array.Empty<DoctorTopPatientItem>();
        public IReadOnlyList<DoctorRecentAppointmentItem> RecentAppointments { get; set; } = Array.Empty<DoctorRecentAppointmentItem>();
        public IReadOnlyList<DoctorRankingItem> DoctorRanking { get; set; } = Array.Empty<DoctorRankingItem>();
    }

    public class DoctorInsightTrendItem
    {
        public string Label { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class DoctorStatusBreakdownItem
    {
        public string StatusLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DoctorDepartmentBreakdownItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int UniquePatientCount { get; set; }
        public decimal CollectedRevenue { get; set; }
    }

    public class DoctorTopPatientItem
    {
        public string PatientName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
    }

    public class DoctorRecentAppointmentItem
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public bool IsCollected { get; set; }
    }

    public class DoctorRankingItem
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int CompletedCount { get; set; }
        public int UniquePatientCount { get; set; }
        public decimal CollectedRevenue { get; set; }
    }
}
