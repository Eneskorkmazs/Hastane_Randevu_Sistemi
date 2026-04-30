using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HastaneRandevuSistemi.ViewModels
{
    public class AdminServiceAnalysisViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; }

        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public double CompletionRate => TotalAppointments > 0 ? (double)CompletedAppointments / TotalAppointments * 100 : 0;
        public double CancellationRate => TotalAppointments > 0 ? (double)CancelledAppointments / TotalAppointments * 100 : 0;

        public IReadOnlyList<ServiceStatusItem> StatusBreakdown { get; set; } = Array.Empty<ServiceStatusItem>();
        public IReadOnlyList<ServiceDepartmentItem> DepartmentVolume { get; set; } = Array.Empty<ServiceDepartmentItem>();
        public IReadOnlyList<ServiceTrendPoint> DailyVolumeTrend { get; set; } = Array.Empty<ServiceTrendPoint>();
        public IReadOnlyList<ServicePeakHourItem> PeakHours { get; set; } = Array.Empty<ServicePeakHourItem>();
        
        public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = Array.Empty<SelectListItem>();
    }

    public class ServiceStatusItem
    {
        public string StatusLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = "#0d6efd";
    }

    public class ServiceDepartmentItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public double SharePercent { get; set; }
    }

    public class ServiceTrendPoint
    {
        public string DateLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ServicePeakHourItem
    {
        public int Hour { get; set; }
        public int Count { get; set; }
    }
}
