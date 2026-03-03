namespace HastaneRandevuSistemi.Models
{
    public class DashboardViewModel
    {
        public int TotalDoctors { get; set; }      // Toplam Doktor
        public int TotalDepartments { get; set; }  // Toplam Bölüm
        public int TotalAppointments { get; set; } // Toplam Randevu
        public int PendingAppointments { get; set; } // Bekleyen (Onaylanmamış)
        public int TodaysAppointments { get; set; }  // Bugünkü Randevular
    }
}