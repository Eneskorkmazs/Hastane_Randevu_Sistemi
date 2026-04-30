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
- Doktor dashboard'u: bugün ve gelecek hafta özetleri, durum bazlı kartlar, filtrelenebilir randevu listesi.
- Admin dashboard'u: genel metrik kartları, son aktivite/bildirim akışı.
- Randevu oluşturma formunda tarih/saat validasyon mesajları iyileştirildi.

### Hafta 4
- Admin dashboard'u detaylandırıldı: haftalık trend, poliklinik yoğunluk kartları, iptal metrikleri.
- Yeni raporlama ekranı eklendi: tarih aralığı, poliklinik ve durum filtreleri, yazdırılabilir özet.
- Duyuru ve bilgilendirme modülü eklendi: tüm kullanıcılar veya rol bazlı hedefleme.

### Hafta 5
- Randevu listesinde gelişmiş arama ve filtreleme eklendi.
- Randevu alma deneyimi iyileştirildi: en yakın uygun saat önerileri, hızlı slot seçimi.
- Randevu durum akışı iyileştirildi: otomatik tamamlama, bekleniyor gösterimi.

### Hafta 6
- Admin Tahsilat sayfası eklendi.
- Doktor takviminde günlük detay ekranı eklendi.
- REST API katmanı oluşturuldu: `Api/AppointmentApiController`.
- `MedicalReport` modeli ve randevu detay sayfası eklendi.

### Hafta 7
- Tıbbi geçmiş modülü eklendi (`Patient/MedicalHistory`).
- Dosya yükleme işlemleri tıbbi geçmiş akışına entegre edildi.
- FluentValidation altyapısı aktif edildi.
- JWT token endpoint'i eklendi, xUnit test projesi oluşturuldu.
- Arka plan hatırlatma servisi eklendi.

### Hafta 8
- Reçete modülü sekreter tarafına taşındı (sadece tamamlanan randevular).
- Muhasebe ve tahsilat ekranları güçlendirildi.
- Hizmet analizi bölümü etkileşimli hale getirildi.

### Hafta 9
- **AI Semptom Kontrolcüsü** eklendi: kural tabanlı semptom → poliklinik yönlendirme.
- **QR Kod ile Randevu Doğrulama**: randevu detay sayfasında QR bilet gösterimi.
- **Dijital Reçete PDF çıktısı**: yazdır butonu + QR imzalı reçete önizleme.
- Serilog loglama altyapısı aktif edildi (`Logs/hrs-.log`).
- xUnit integration testleri genişletildi (15+ senaryo).
- Kod refactoring: doktor erişim isteği kaldırıldı, navbar sadeleştirildi.

### Hafta 10
- **Doktor Değerlendirme Sistemi**: yıldız puanlama ve yorum yapabilme (`DoctorReview`).
- **Chart.js Canlı Dashboard**: admin panelinde donut + bar grafikleri.
- **Dark Mode** desteği: tüm sayfalarda tema değiştirme.
- Erişilebilirlik (accessibility) iyileştirmeleri: ARIA etiketleri, skip link.
- Proje dokümantasyonu güncellendi.
- Final testleri tamamlandı.
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

### Hafta 9
- `Serilog` entegrasyonu eklendi:
  - console, debug ve gunluk dosya loglama,
  - loglar `HastaneRandevuSistemi/Logs` altina yazilir.
- Test kapsamı genişletildi:
  - mevcut validator testlerine ek olarak temel integration smoke testleri eklendi,
  - ana sayfa, giris sayfasi ve JWT korumali API erisimi dogrulaniyor.
- Kod tabani test edilebilirlik icin guncellendi:
  - `Program` sinifi integration testlerde kullanilabilecek sekilde acildi.

### Hafta 10
- Erisilebilirlik iyilestirmeleri eklendi:
  - skip-link ile klavye kullanicilari icin hizli icerik gecisi,
  - auth ekranlarinda `aria-live`, `role`, `aria-pressed` ve uygun `autocomplete` tanimlari,
  - dis baglantilar icin daha guvenli ve ekran okuyucu dostu etiketler.
- Dokumantasyon guncellendi:
  - hafta 9-10 kapsami README icine eklendi,
  - test ve loglama davranisi dokumante edildi.

## Haftalık Raporlar

| Hafta | Konu | Rapor |
|-------|------|-------|
| 1 | Model sınıfları, Data Annotation, Ana sayfa UI/UX | [📄 PDF](Reports/Hafta1_Raporu.pdf) |
| 2 | Hasta paneli, Profil güncelleme, Bildirim altyapısı | [📄 PDF](Reports/Hafta2_Raporu.pdf) |
| 3 | Doktor paneli, Çalışma takvimi, Reçete temel yapısı | [📄 PDF](Reports/Hafta3_Raporu.pdf) |
| 4 | Admin grafik/istatistik ekranları, Raporlama sistemi | [📄 PDF](Reports/Hafta4_Raporu.pdf) |
| 5 | Gelişmiş arama/filtreleme, UX iyileştirmeleri | [📄 PDF](Reports/Hafta5_Raporu.pdf) |
| 6 | API katmanı, Tahsilat sayfası, Randevu fiyat alanı | [📄 PDF](Reports/Hafta6_Raporu.pdf) |
| 7 | Dosya yükleme, Tıbbi geçmiş modülü, Dijital reçete | [📄 HTML](Reports/Hafta7_Raporu.html) |
| 8 | Güvenlik (CSRF/XSS), Caching, Dark Mode | [📄 HTML](Reports/Hafta8_Raporu.html) |
| 9 | Sekreter Paneli, Muhasebe iyileştirmeleri, Bildirimler | [📄 HTML](Reports/Hafta9_Raporu.html) |
| 10 | ENS Yapay Zeka Asistanı, Hasta Paneli modernizasyonu, Final | [📄 HTML](Reports/Hafta10_Raporu.html) |

- [📄 İlk 5 Hafta Genel Rapor (PDF)](Reports/Ilk5Hafta_Genel_Rapor.pdf)

## Test ve Loglama
- Testleri calistirmak icin: `dotnet test .\HastaneRandevuSistemi.Tests\HastaneRandevuSistemi.Tests.csproj`
- Uygulama loglari: `HastaneRandevuSistemi\Logs\hrs-*.log`

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
