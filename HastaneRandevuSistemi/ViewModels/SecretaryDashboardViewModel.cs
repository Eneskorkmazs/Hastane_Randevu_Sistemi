using System;
using System.Collections.Generic;
using HastaneRandevuSistemi.Models;

namespace HastaneRandevuSistemi.ViewModels
{
    public class SecretaryDashboardViewModel
    {
        public List<Appointment> PendingAppointments { get; set; } = new();
        public List<Appointment> UnsentPrescriptions { get; set; } = new();
        public List<Appointment> SentPrescriptions { get; set; } = new();
        public List<Appointment> UpcomingNext2Hours { get; set; } = new();
        public List<Appointment> RecentlyCancelled { get; set; } = new();
        
        public int TodaysTotal { get; set; }
        public int TodaysCompleted { get; set; }
        public int UrgentApprovals { get; set; }
        public int DelayedPrescriptions { get; set; }
        public int TomorrowsTotal { get; set; }
        public int OldPendingCount { get; set; }
        
        public List<DepartmentLoadItem> DepartmentLoads { get; set; } = new();
        public List<Notification> SystemAnnouncements { get; set; } = new();
    }

    public class DepartmentLoadItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int Capacity { get; set; } = 50; // Örnek kapasite
        public double LoadPercent => Capacity > 0 ? (double)AppointmentCount / Capacity * 100 : 0;
        public string StatusColor => LoadPercent > 80 ? "danger" : LoadPercent > 50 ? "warning" : "success";
    }
}
