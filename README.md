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
- Randevu durumlarının farklı ekranlarda daha tutarlı yönetilebilmesi için zaman bazlı kontrol ve ortak durum senkronizasyon altyapısı güçlendirildi.

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
- Hasta paneli güncellendi:
  - bekleyen, tamamlanan ve iptal edilen sayaçlar filtreli liste ekranları ile ilişkilendirildi,
  - gerçekleşmemiş randevular bekleyen başlığı altında daha net gösterildi.
- Randevu durum akışı iyileştirildi:
  - gelecek tarihli onaylı randevular için `Tamamla` yerine `Bekleniyor` gösterimi eklendi,
  - zamanı geçen randevuların otomatik olarak `Tamamlandı` durumuna dönmesi sağlandı,
  - otomatik tamamlanan randevular için geçmiş olsun mesajı içeren bildirim desteği eklendi.

### Hafta 6
- Admin Tahsilat sayfası eklendi:
  - tarih aralığı, poliklinik ve doktor filtreli ödeme/tahsilat listesi,
  - toplam ve ortalama ücret özet kartları.
- Doktor takviminde günlük detay ekranı eklendi (`DayDetails`):
  - seçilen güne ait tüm randevuları listeler,
  - tatil ve özel günlere özel mesaj desteği.
- `Appointment` modeline `Price` (decimal) alanı eklendi; ilgili migration çalıştırıldı.
- REST API katmanı oluşturuldu: `Api/AppointmentApiController`.
- `MedicalReport` modeli eklendi.
- Randevu bireysel detay sayfası eklendi (`Views/Appointment/Details.cshtml`).
- Admin Dashboard'a tahsilat bağlantısı ve toplam ücret metriği eklendi.

### Hafta 7
- Tibbi gecmis modulu eklendi (`Patient/MedicalHistory`):
  - hasta bazli tibbi gecmis kaydi olusturma/silme,
  - tani, ilac, alerji ve not alanlari.
- Dosya yukleme islemleri tibbi gecmis akisina entegre edildi:
  - opsiyonel ek dosya yukleme,
  - dosya turu ve boyut kontrolu.
- Hasta paneli ve navigasyon gelistirildi:
  - panelde tibbi gecmis kayit sayaci,
  - hizli erisim butonu ve menude "Tibbi Gecmis" baglantisi.
- Randevu detay ekranindan tibbi gecmis sayfasina hizli gecis eklendi.
- Veri tabani tarafi icin `AddMedicalHistoryModule` migration'i olusturuldu ve
  Sqlite/Postgres ortamlarinda tablo garanti olusturma adimi seeder'a eklendi.
- Eksik tamamlama guncellemeleri:
  - FluentValidation altyapisi aktif edildi (`Register`, `ChangePassword`, API token ve tibbi gecmis validatorlari),
  - Hasta paneline gercek parola degistirme modulu eklendi (`Patient/ChangePassword`),
  - JWT token endpoint'i eklendi (`POST /api/AuthApi/token`) ve `AppointmentApi` Bearer token ile korundu,
  - `xUnit` test projesi eklendi (`HastaneRandevuSistemi.Tests`) ve temel validator testleri yazildi.
  - Arka plan hatirlatma servisi eklendi: randevuya 1 gun kala hastaya otomatik e-posta + bildirim gonderilir.

### Hafta 8
- Hasta tarafinda receteler, tibbi gecmis ile ayni deneyim altinda birlestirildi:
  - hasta hangi randevuda hangi ilacin yazildigini gorebilir,
  - saglik gecmisi ekranindan recete detayina ulasabilir.
- Doktor tarafinda olusturulan recete alanlari ve randevu bazli recete takibi kalici hale getirildi.
- Hasta randevu iptal ettiginde tahsil edilmis odeme icin otomatik iade akisi geri getirildi:
  - odeme kaydi geri alinir,
  - iptal edilen randevuda admin iade bilgisi gorunur.
- Muhasebe ve tahsilat ekranlari guclendirildi:
  - bolum gelir analizi,
  - doktor performans tablolari,
  - tahsil edilen / bekleyen / iptal edilen dagilim grafikleri,
  - daha guclu Tahsilat dashboard'u.
- Hizmet Analizi bolumu etkilesimli hale getirildi:
  - secilen bolume gore istatistikler,
  - secilen doktora gore istatistikler,
  - grafik uzerinden tablo filtreleme.
- Ozel gunlerde randevu olusturma akisinda "Musaitlik bilgisi alinamadi" yerine "Bugun resmi tatil." mesaji gosterilmeye baslandi.

## 10 Haftalık Geliştirme Planı

- Haftalık ortalama çalışma: `20-22 saat`

### Hafta 1 (günlük ~3 saat)
- Model sınıflarının genişletilmesi
- Data Annotation ve Fluent Validation iyileştirmeleri
- Ana sayfanın modern UI/UX tasarımına geçirilmesi

### Hafta 2 (günlük ~3 saat)
- Hasta panelinin detaylandırılması
- Profil güncelleme ve parola değiştirme modülü
- Bildirim sistemi altyapısı

### Hafta 3 (günlük 3-3,5 saat)
- Doktor panelinin geliştirilmesi
- Çalışma takvimi ve müsaitlik yönetimi
- Dijital reçete modülü temel yapısı

### Hafta 4 (günlük 3-3,5 saat)
- Admin paneline grafik ve istatistik ekranları
- Raporlama sistemi
- Duyuru ve bilgilendirme modülü

### Hafta 5 (günlük 3-3,5 saat)
- Gelişmiş arama ve filtreleme özellikleri
- UX iyileştirmeleri
- Yaratıcı özelliklerin ilk faz geliştirmesi

### Hafta 6 (günlük 3-3,5 saat)
- Web API katmanı oluşturulması
- JWT ile kimlik doğrulama
- E-posta bildirim sistemi
- QR kod üretim altyapısı

### Hafta 7 (günlük 3-3,5 saat)
- Dosya yükleme işlemleri
- Tıbbi geçmiş (medical history) modülü
- İleri seviye özelliklerin entegrasyonu

### Hafta 8 (günlük 3-3,5 saat)
- Güvenlik önlemleri (CSRF, XSS korumaları)
- Caching mekanizması
- Performans optimizasyonları
- Dark Mode tema desteği

### Hafta 9 (günlük 3-4 saat)
- Unit test yazımı (xUnit)
- Integration test senaryoları
- Serilog ile loglama altyapısı
- Kod refactoring ve temizlik

### Hafta 10 (günlük 3-4 saat)
- Proje dokümantasyonu
- Erişilebilirlik (accessibility) iyileştirmeleri
- Sunum hazırlığı
- Final testleri ve teslim

## Projeye Eklenecek Özellikler

- AI Semptom Kontrolcüsü: Hasta semptom seçimi yaparak uygun bölüme yönlendirilir
- Doktor Değerlendirme Sistemi: Yıldız puanlama ve yorum yapabilme
- QR Kod ile Randevu Doğrulama
- Dijital Reçete Modülü (PDF çıktısı alma)
- Canlı Dashboard (Chart.js ile animasyonlu grafikler)
- Dark Mode desteği
- Sıra Takip Ekranı ve tahmini bekleme süresi hesaplama

## Teknolojiler
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server / LocalDB
- Bootstrap 5
- Chart.js
- Swagger
- JWT
- Serilog
- xUnit
- QRCoder

## Haftalık Raporlar
- [Hafta 1 Raporu PDF](Reports/Hafta1_Raporu.pdf)
- [Hafta 2 Raporu PDF](Reports/Hafta2_Raporu.pdf)
- [Hafta 3 Raporu PDF](Reports/Hafta3_Raporu.pdf)
- [Hafta 4 Raporu PDF](Reports/Hafta4_Raporu.pdf)
- [Hafta 5 Raporu PDF](Reports/Hafta5_Raporu.pdf)
- [İlk 5 Hafta Genel Raporu HTML](Reports/Ilk5Hafta_Genel_Rapor.html) · [PDF](Reports/Ilk5Hafta_Genel_Rapor.pdf)
- [Hafta 6 Raporu HTML](Reports/Hafta6_Raporu.html) · [PDF](Reports/Hafta6_Raporu.pdf)
- [Hafta 7 Raporu HTML](Reports/Hafta7_Raporu.html)
- [Hafta 8 Raporu HTML](Reports/Hafta8_Raporu.html)

## Varsayılan Roller
- `Admin`
- `Doktor`
- `Hasta`

## Notlar
- Yeni migration dosyası proje içine eklendi: `20260303120000_AddPatientPortalAndIdentityFields`
- Hasta paneline giriş yapan kullanıcılar `Patient/Dashboard` üzerinden yönlendirilir.
- Gercek e-posta gonderimi icin `appsettings.Development.json` (veya environment variable) icinde
  `EmailSettings:Host`, `EmailSettings:Mail`, `EmailSettings:Password`, `EmailSettings:Port`, `EmailSettings:EnableSsl`
  alanlarini doldurmaniz gerekir.
- E-posta akisi:
  - Randevu olusturuldugunda hastaya aninda e-posta gonderilir.
  - Randevudan 1 gun once (yarin tarihli aktif randevular) otomatik hatirlatma e-postasi gonderilir.

## Hatasiz Baslatma (Windows)
Sabahtan beri yasadiginiz `ERR_CONNECTION_REFUSED` gibi sorunlari tekrar etmemek icin projeyi bu script ile baslatin:

```powershell
.\start-http.ps1
```

Ek not:
- Portu otomatik temizler (`5087` doluysa eski sureci kapatir).
- Dogru launch profile ile (`http`) calistirir.
- Gecici derleme atlamak isterseniz:

```powershell
.\start-http.ps1 -NoBuild
```
