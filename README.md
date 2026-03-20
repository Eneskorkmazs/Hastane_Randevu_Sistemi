# Hastane Randevu Sistemi

ASP.NET Core MVC ve Entity Framework Core ile hazırlanmış bir hastane randevu uygulaması.

## Güncel Kapsam

### Hafta 1
- `AppUser` modeli TC, telefon, doğum tarihi, cinsiyet ve adres alanları ile genişletildi.
- Kayıt ekranı ve `RegisterViewModel` yeni alanları destekleyecek şekilde güncellendi.
- Yeni kullanıcılar kayıt olduktan sonra doğrudan hasta paneline yönlendirilir.
- Ana hasta akışı için gerekli bildirim servisi ve temel profil altyapısı eklendi.

### Hafta 2
- Hasta paneli eklendi: `Dashboard`, `Profile`, `Notifications`.
- Randevular hasta kullanıcısı ile ilişkilendirildi.
- Hasta tarafında randevu geçmişi ve uygun durumlarda iptal akışı destekleniyor.
- Randevu oluşturma, onay, tamamlama ve iptal hareketleri için bildirim kayıtları üretiliyor.

### Hafta 3
- Randevu akışı güvenliği: Admin ve doktor yetkileri için rol bazlı filtreler sertleştirilecek.
- Doktor dashboard'u:
  - bugün ve gelecek hafta özetleri,
  - durum bazlı (bekleyen/onaylı/tamamlanan/iptal) kartları,
  - filtrelenebilir randevu listesi.
- Admin dashboard'u:
  - genel metrik kartlarını detaylandırma (bölüm + doktor + randevu trendi),
  - son aktivite/bildirim akışı.
- Kullanıcı deneyimi:
  - randevu oluşturma formunda tarih/saat validasyon mesajları iyileştirilecek,
  - bildirimlerde okunmamış durumunun daha net göstergesi eklenecek.
- Teknik alt yapı:
  - hata senaryoları için geri bildirim mesajları standardize edilecek,
  - en önemli entity'ler için basit birimler eklenecek.

### Hafta 4
- Hafta 4 kapsamındaki tamamlanan güncellemeler:
- Admin dashboard'u detaylandırıldı:
  - haftalık trend gösterimi,
  - poliklinik yoğunluk kartları,
  - iptal edilen randevu metrikleri.
- Yeni raporlama ekranı eklendi:
  - tarih aralığı,
  - poliklinik ve durum filtreleri,
  - yazdırılabilir yönetim özeti.
- Duyuru ve bilgilendirme modülü eklendi:
  - tüm kullanıcılar veya rol bazlı hedefleme,
  - bildirim kutusuna anlık duyuru gönderimi.

### Hafta 5
- Hafta 5 kapsamındaki tamamlanan güncellemeler:
- Randevu listesinde gelişmiş arama ve filtreleme:
  - poliklinik,
  - sıralama,
  - sadece gelecek randevular seçeneği.
- Liste ekranına hızlı özet kartları eklendi.
- Randevu alma deneyimi iyileştirildi:
  - doktora göre en yakın uygun saat önerileri,
  - hızlı slot seçimi.

## Teknolojiler
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server / LocalDB
- Bootstrap 5

## Başlatma
1. `appsettings.json` içindeki bağlantı bilgisini kontrol edin.
2. Veritabanı daha önce eski şema ile oluşturulduysa sıfırlayın veya migration uygulayın.
3. Projeyi çalıştırın:

```powershell
dotnet run --project .\HastaneRandevuSistemi\HastaneRandevuSistemi.csproj
```

## Haftalık Raporlar
- [Hafta 1 Raporu PDF](Reports/Hafta1_Raporu.pdf)
- [Hafta 2 Raporu PDF](Reports/Hafta2_Raporu.pdf)
- [Hafta 3 Raporu PDF](Reports/Hafta3_Raporu.pdf)
- [Hafta 4 Raporu PDF](Reports/Hafta4_Raporu.pdf)
- [Hafta 5 Raporu PDF](Reports/Hafta5_Raporu.pdf)

## Varsayılan Roller
- `Admin`
- `Doktor`
- `Hasta`

## Notlar
- Yeni migration dosyası proje içine eklendi: `20260303120000_AddPatientPortalAndIdentityFields`
- Hasta paneline giriş yapan kullanıcılar `Patient/Dashboard` üzerinden yönlendirilir.

