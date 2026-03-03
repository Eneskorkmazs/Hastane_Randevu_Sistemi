namespace HastaneRandevuSistemi.Models
{
    public enum AppointmentStatus
    {
        Bekliyor = 0,    // Onay bekliyor
        Onaylandi = 1,   // Doktor/Sistem onayladı
        Iptal = 2,       // İptal edildi
        Tamamlandi = 3   // Muayene bitti
    }
}