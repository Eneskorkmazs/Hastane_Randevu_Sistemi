using System;
using System.Collections.Generic;

namespace HastaneRandevuSistemi.Models
{
    public class DashboardViewModel
    {
        public int TotalDoctors { get; set; }      // Toplam Doktor
        public int TotalDepartments { get; set; }  // Toplam Bölüm
        public int TotalAppointments { get; set; } // Toplam Randevu
        public int PendingAppointments { get; set; } // Bekleyen (Onaylanmamýþ)
        public int TodaysAppointments { get; set; }  // Bugünkü Randevular

        public int ThisWeekAppointments { get; set; }  // Bu Haftaki Randevular
        public int ApprovedAppointments { get; set; }  // Onaylanan Randevular
        public int CompletedAppointments { get; set; } // Tamamlanan Randevular

        public IReadOnlyList<Appointment> UpcomingAppointments { get; set; } = Array.Empty<Appointment>();
        public IReadOnlyList<Notification> LatestNotifications { get; set; } = Array.Empty<Notification>();
    }
}
