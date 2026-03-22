using System;
using System.Collections.Generic;

namespace HastaneRandevuSistemi.Models
{
    public class DashboardViewModel
    {
        public int TotalDoctors { get; set; }      // Toplam Doktor
        public int TotalDepartments { get; set; }  // Toplam Bölüm
        public int TotalAppointments { get; set; } // Toplam Randevu
        public int PendingAppointments { get; set; } // Bekleyen (Onaylanmamış)
        public int TodaysAppointments { get; set; }  // Bugünkü Randevular

        public int ThisWeekAppointments { get; set; }  // Bu Haftaki Randevular
        public int ApprovedAppointments { get; set; }  // Onaylanan Randevular
        public int CompletedAppointments { get; set; } // Tamamlanan Randevular
        public int CancelledAppointments { get; set; } // Iptal edilen randevular

        public IReadOnlyList<Appointment> UpcomingAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Notification> LatestNotifications { get; set; } = Array.Empty<Notification>();
        public IReadOnlyList<DepartmentStatItem> DepartmentStats { get; set; } = Array.Empty<DepartmentStatItem>();
        public IReadOnlyList<TrendPointItem> WeeklyTrend { get; set; } = Array.Empty<TrendPointItem>();
    }

    public class DepartmentStatItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int DoctorCount { get; set; }
    }

    public class TrendPointItem
    {
        public string Label { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }
}

