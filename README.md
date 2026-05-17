# 🏥 Hastane Randevu Sistemi (HRS)

ASP.NET Core MVC, Entity Framework Core ve ASP.NET Identity kullanılarak geliştirilmiş, çok katmanlı, modern ve tam donanımlı bir hastane randevu ve yönetim platformu.

## 🌟 Güncel Kapsam ve Gelişmeler

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
- **Dijital Reçete PDF Çıktısı**: yazdır butonu + QR imzalı reçete önizleme.
- Serilog loglama altyapısı aktif edildi (`Logs/hrs-.log`).
- xUnit integration testleri genişletildi.

### Hafta 10 (Final Sürümü)
- **Eczane Keşif ve Mesafe Sistemi**: Şanlıurfa (Acı Biber, Karaköprü, Siverek, Birecik, Viranşehir, Meydan Ecz.), Sinop ve diğer büyük şehirleri kapsayan eczane verileri eklendi.
- **Canlı GPS ve Akıllı Sıralama**: Tarayıcı konumu üzerinden Haversine formülü ile kullanıcının mevcut konumuna göre en yakın eczaneyi hesaplayan ve anlık sıralayan sistem eklendi.
- **Çift Katmanlı Hastane Değerlendirme Modülü**: Hastaların hastane hakkında tek seferlik yorum ve puan bırakabileceği, adminin yanıtlayabileceği, hem uygulama hem de UNIQUE constraint ile korunan sistem eklendi.
- **Navbar Güvenlik Zırhı ve Cache Fix**: Çıkış sonrası linklerin ve sayfaların temizlenmesini garanti altına alan `isAuthenticated` kontrolleri ve NoStore politikaları uygulandı.
- **Yerelleştirme ve Kodlama Revizyonu**: Sistem genelinde (Muhasebe tahsilat tabloları, bölüm adları, durum etiketleri) görülen tüm Türkçe karakter bozulmaları (mojibake) temizlendi.

## 📄 Haftalık Raporlar ve Dokümantasyon

| Hafta | Konu | Rapor Bağlantısı |
|:---:|:---|:---|
| 1 | Model sınıfları, Data Annotation, Ana sayfa UI/UX | [📄 Hafta 1 Raporu](Reports/Hafta1_Raporu.html) |
| 2 | Hasta paneli, Profil güncelleme, Bildirim altyapısı | [📄 Hafta 2 Raporu](Reports/Hafta2_Raporu.html) |
| 3 | Doktor paneli, Çalışma takvimi, Reçete temel yapısı | [📄 Hafta 3 Raporu](Reports/Hafta3_Raporu.html) |
| 4 | Admin grafik/istatistik ekranları, Raporlama sistemi | [📄 Hafta 4 Raporu](Reports/Hafta4_Raporu.html) |
| 5 | Gelişmiş arama/filtreleme, UX iyileştirmeleri | [📄 Hafta 5 Raporu](Reports/Hafta5_Raporu.html) |
| 6 | API katmanı, Tahsilat sayfası, Randevu fiyat alanı | [📄 Hafta 6 Raporu](Reports/Hafta6_Raporu.html) |
| 7 | Dosya yükleme, Tıbbi geçmiş modülü, Dijital reçete | [📄 Hafta 7 Raporu](Reports/Hafta7_Raporu.html) |
| 8 | Güvenlik (CSRF/XSS), Caching, Dark Mode | [📄 Hafta 8 Raporu](Reports/Hafta8_Raporu.html) |
| 9 | AI Semptom Kontrolcüsü, QR Kod, Reçete PDF, Serilog, Testler | [📄 Hafta 9 Raporu](Reports/Hafta9_Raporu.html) |
| **10** | **Final: Eczane GPS Sistemi, Çift Katmanlı Değerlendirme, Güvenlik Zırhı** | [📄 Final Raporu](Reports/Hafta10_Raporu.html) |

- [📄 İlk 5 Hafta Genel Rapor (PDF)](Reports/Ilk5Hafta_Genel_Rapor.pdf)

## 🔐 Sistem Rolleri
- `Admin`
- `Doktor`
- `Sekreter`
- `Hasta`

