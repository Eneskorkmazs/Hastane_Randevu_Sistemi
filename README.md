# 🏥 HRS Hastane Randevu Sistemi (HRS)

ASP.NET Core MVC, Entity Framework Core ve ASP.NET Identity kullanılarak geliştirilmiş, çok katmanlı, modern ve tam donanımlı bir hastane randevu ve yönetim platformu.

## 🚀 Güncel Kapsam ve Gelişmeler

### Hafta 1
- `AppUser` modeli TC, telefon, doğum tarihi, cinsiyet ve adres alanları ile genişletildi.
- Kayıt ekranı ve `RegisterViewModel` yeni alanları destekleyecek şekilde güncellendi.
- Yeni kullanıcılar kayıt olduktan sonra doğrudan hasta paneline yönlendirilir.
- Ana hasta akışları için gerekli bildirim servisi ve temel profil altyapısı eklendi.

### Hafta 2
- Hasta paneli eklendi: `Dashboard`, `Profile`, `Notifications`.
- Randevular hasta kullanıcıları ile Cells ilişkilendirildi.
- Hasta tarafından randevu geçmişi ve uygun durumlarda iptal akışları destekleniyor.
- Randevu oluşturma, onay, tamamlama ve iptal hareketleri için bildirim kayıtları üretiliyor.

### Hafta 3
- Doktor dashboard'u: bugün ve gelecek hafta özetleri, durum bazlı kartlar, filtrelenebilir randevu listesi.
- Admin dashboard'u: genel metrik kartları, son aktivite/bildirim akışı.
- Randevu oluşturma formunda tarih/saat validasyon mesajları iyileştirildi.

### Hafta 4
- Admin dashboard'u detaylandırıldı: haftalık trend, poliklinik yoğunluk kartları, iptal metrikleri.
- Yeni raporlama ekranı eklendi: tarih aralıkları, poliklinik ve durum filtreleri, yazdırılabilir özet.
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
- Reçete modülü sekreter tarafından tanımlandı (sadece tamamlanan randevular).
- Muhasebe ve tahsilat ekranları güçlendirildi.
- Hizmet analizi bağlamı etkileşimli hale getirildi.

### Hafta 9
- **AI Semptom Kontrolcüsü** eklendi: kural tabanlı semptom atıf poliklinik yönlendirme.
- **QR Kod ile Randevu Doğrulama**: randevu detay sayfasında QR bilet gösterimi.
- **Dijital Reçete PDF Çıktısı**: yazdır butonu + QR imzalı reçete önizleme.
- Serilog loglama altyapısı aktif edildi (`Logs/hrs-.log`).
- xUnit integration testleri genişletildi.

### Hafta 10 (Final Sürümü ve İyileştirmeler)
- **ENS Asistan (Yapay Zeka Yönlendirme)**: Hastaların şikayetlerini yazarak hangi bölüme randevu almaları gerektiğini kolayca öğrenebildiği akıllı asistan eklendi.
- **Eczane Nöbetçi ve Mesafe Sistemi**: Şanlıurfa'daki eczane verileri sisteme entegre edildi.
- **Canlı GPS ve Akıllı Sıralama**: Tarayıcı konumu üzerinden Haversine formülü ile kullanıcının mevcut konumuna göre en yakın eczaneyi hesaplayan ve anlık sıralayan sistem eklendi.
- **Çift Katmanlı Hastane Değerlendirme Modülü**: Hastaların hastane hakkında tek seferlik yorum ve puan bırakabileceği, adminin yanıtlayabileceği, hem uygulama hem de UNIQUE constraint ile korunan sistem eklendi.
- **Doktor Paneli Reçete Yazma Mantığı Düzeltildi**: Doktor panelinde diğer doktorların hastalarına da "Reçete Yaz" butonunun çıkması ve 404 hatası vermesi sorunu giderildi. Reçete yazma butonu yalnızca doktorun kendi randevuları için görünür hale getirildi.
- **Admin Bildirimleri ve Destek Talepleri İyileştirildi**:
  - Bildirimlerin okundu olarak işaretlenmesini engelleyen hata giderildi.
  - Tek bir destek talebi gönderildiğinde admin panelinde iki aynı bildirimin düşmesine neden olan hata (çift tıklama ve veritabanı yansımaları) `HashSet` kullanılarak giderildi.
- **Navbar Güvenlik Zırhı ve Cache Fix**: Çıkış sonrası linklerin ve sayfaların temizlenmesini garanti altına alan `isAuthenticated` kontrolleri ve NoStore politikaları applied.
- **Yerelleştirme ve Kodlama Revizyonu**: Sistem genelinde görülen tüm Türkçe karakter bozulmaları (mojibake) tamamen temizlendi.

---

## 📅 10 Haftalık Raporlar ve Dokümantasyon

| Hafta | Konu | Rapor Bağlantısı |
| :--- | :--- | :--- |
| 1 | Model Standartları, Data Annotation, Ana sayfa UI/UX | [📄 PDF](Reports/Hafta1_Raporu.pdf) |
| 2 | Hasta paneli, Profil Güncelleme, Bildirim altyapısı | [📄 PDF](Reports/Hafta2_Raporu.pdf) |
| 3 | Doktor paneli, Atama takvimi, Reçete temel yapısı | [📄 PDF](Reports/Hafta3_Raporu.pdf) |
| 4 | Admin grafik/istatistik ekranları, Raporlama sistemi | [📄 PDF](Reports/Hafta4_Raporu.pdf) |
| 5 | Gelişmiş arama/filtreleme, UX iyileştirmeleri | [📄 PDF](Reports/Hafta5_Raporu.pdf) |
| 6 | API katmanı, Tahsilat sayfası, Randevu fiyat alanı | [📄 PDF](Reports/Hafta6_Raporu.pdf) |
| 7 | Dosya Yükleme, Tıbbi geçmiş modülü, Dijital reçete | [📄 PDF](Reports/Hafta7_Raporu.pdf) |
| 8 | Güvenlik (CSRF/XSS), Caching, Dark Mode | [📄 PDF](Reports/Hafta8_Raporu.pdf) |
| 9 | AI Semptom Kontrolcüsü, QR Kod, Reçete PDF, Serilog, Testler | [📄 PDF](Reports/Hafta9_Raporu.pdf) |
| **10** | **Final: Eczane GPS Sistemi, Çift Katmanlı Değerlendirme, Güvenlik Zırhı** | [📄 Final PDF](Reports/Hafta10_Raporu.pdf) |

### 📝 Özet / Genel Raporlar
(Reports/Ilk5Hafta_Genel_Rapor.pdf)[HASTANE RANDEVU SİSTEMİ GENEL RAPORU.pdf](https://github.com/user-attachments/files/28368832/HASTANE.RANDEVU.SISTEMI.GENEL.RAPORU.pdf)



(Genel_Rapor.pdf) (https://github.com/user-attachments/files/28368796/genel.1den.10.hafta.rapor.pdf)

> **Hazırlayan:** Enes | **Teknoloji:** ASP.NET Core 8.0 MVC / EF Core | **Veritabanı:** SQLite

### 🗺️ Proje Yol Haritası
Hafta  1 ──> Model Standartları & Proje Altyapısı
Hafta  2 ──> Hasta Paneli & Profil Yönetimi
Hafta  3 ──> Doktor Paneli & Atama Takvimi
Hafta  4 ──> Admin Panel & Grafik / İstatistik Ekranları
Hafta  5 ──> Gelişmiş Arama / Filtreleme & UX İyileştirmeleri
Hafta  6 ──> API Katmanı & Finansal Altyapı
Hafta  7 ──> Dosya Yönetimi & Tıbbi Kayıt Sistemi
Hafta  8 ──> Güvenlik, Caching & Dark Mode
Hafta  9 ──> Sekreter Paneli & Muhasebe & Bildirimler
Hafta 10 ──> Yapay Zeka, Eczane Yönetimi & Final
