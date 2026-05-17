namespace HastaneRandevuSistemi.Models
{
    public enum PrescriptionPharmacyStatus
    {
        Yok = 0,               // Reçete yok veya eczaneye gönderilmedi
        Bekliyor = 1,          // Eczaneye iletildi, henüz işlem yapılmadı
        Hazirlaniyor = 2,      // Eczacı ilaçları hazırlıyor
        TeslimEdildi = 3,      // Hastaya teslim edildi
        Iptal = 4              // İptal edildi
    }
}
