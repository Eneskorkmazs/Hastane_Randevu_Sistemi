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
- **Hasta/Hastane Değerlendirme Sistemi** eklendi: hastalar puan ve yorum bırakabilir, admin yorumları yönetip yanıtlayabilir.
- **ENS Akıllı Asistan** sohbet tabanlı semptom yönlendirme akışına dönüştürüldü.
- Hasta panelindeki bekleyen, tamamlanan, reçete ve bildirim kartları mevcut sekmede doğru veriye yönlendirilecek şekilde düzenlendi.
- Hasta reçeteleri için ayrı `Patient/Prescriptions` sayfası eklendi.
- Navbar'a hasta için bekleyen randevular açılır listesi ve admin için yeni hasta yorumu rozeti eklendi.
- Hasta panelindeki sıradaki randevu kartında görsel taşma problemi giderildi.
- ezcane bölümü eklendi konum bilgisi eklendi

## Haftalık Raporlar

| Hafta | Konu | Rapor |
|-------|------|-------|
| 1 | Model sınıfları, Data Annotation, Ana sayfa UI/UX | [📄 PDF](Reports/Hafta1_Raporu.pdf) |
| 2 | Hasta paneli, Profil güncelleme, Bildirim altyapısı | [📄 PDF](Reports/Hafta2_Raporu.pdf) |
| 3 | Doktor paneli, Çalışma takvimi, Reçete temel yapısı | [📄 PDF](Reports/Hafta3_Raporu.pdf) |
| 4 | Admin grafik/istatistik ekranları, Raporlama sistemi | [📄 PDF](Reports/Hafta4_Raporu.pdf) |
| 5 | Gelişmiş arama/filtreleme, UX iyileştirmeleri | [📄 PDF](Reports/Hafta5_Raporu.pdf) |
| 6 | API katmanı, Tahsilat sayfası, Randevu fiyat alanı | [📄 PDF](Reports/Hafta6_Raporu.pdf) ·  |
| 7 | Dosya yükleme, Tıbbi geçmiş modülü, Dijital reçete | [📄 PDF](Reports/Hafta7_Raporu.pdf) |
| 8 | Güvenlik (CSRF/XSS), Caching, Dark Mode | [📄 PDF](Reports/Hafta8_Raporu.pdf)  |
| 9 | AI Semptom Kontrolcüsü, QR Kod, Reçete PDF, Serilog, Testler, Sekreter Reçete Yönetimi | [📄 PDF](Reports/Hafta9_Raporu.pdf) |
| 10 | Hasta/Hastane Değerlendirmesi, ENS sohbet asistanı, hasta paneli kısayolları, reçete sayfası | [📄 PDF](Reports/Hafta10_Raporu.pdf) |


- [📄 İlk 5 Hafta Genel Rapor (PDF)](Reports/Ilk5Hafta_Genel_Rapor.pdf)



## Varsayılan Roller
- `Admin`
- `Doktor`
- `Hasta`
-'sekreter'

