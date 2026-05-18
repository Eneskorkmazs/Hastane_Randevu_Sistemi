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
-ens asistan eklendi kolayca hangi bölüme randevu alabileceğini gördü kullanıcı
- **Eczane Keşif ve Mesafe Sistemi**: Şanlıurfa  eczane verileri eklendi.
- **Canlı GPS ve Akıllı Sıralama**: Tarayıcı konumu üzerinden Haversine formülü ile kullanıcının mevcut konumuna göre en yakın eczaneyi hesaplayan ve anlık sıralayan sistem eklendi.
- **Çift Katmanlı Hastane Değerlendirme Modülü**: Hastaların hastane hakkında tek seferlik yorum ve puan bırakabileceği, adminin yanıtlayabileceği, hem uygulama hem de UNIQUE constraint ile korunan sistem eklendi.
- **Navbar Güvenlik Zırhı ve Cache Fix**: Çıkış sonrası linklerin ve sayfaların temizlenmesini garanti altına alan `isAuthenticated` kontrolleri ve NoStore politikaları uygulandı.
- **Yerelleştirme ve Kodlama Revizyonu**: Sistem genelinde (Muhasebe tahsilat tabloları, bölüm adları, durum etiketleri) görülen tüm Türkçe karakter bozulmaları (mojibake) temizlendi.
- **Çift Destek Talebi Hatası Giderildi**: Hasta tarafından tek destek talebi gönderildiğinde admin panelinde iki aynı bildirimin düşmesine neden olan hata tespit edildi ve düzeltildi.
  - `PatientController.SubmitSupportRequest` metodunda `HashSet<string>` kullanılarak admin ID'leri tekilleştirildi.
  - DB'de iki farklı Admin hesabının (`admin@havatakip.com.tr`, `admin@hastane.com`) aynı anda Admin rolünde olduğu tespit edildi; `admin@hastane.com` Admin rolünden çıkarıldı.
  - Destek formu `onsubmit` olayında buton anında devre dışı bırakılarak çift tıklama (double-submit) engeli eklendi.
  - Fallback admin email adresi güncel aktif admin ile senkronize edildi.

## 📄 Haftalık Raporlar ve Dokümantasyon

| Hafta | Konu | Rapor Bağlantısı |
|:---:|:---|:---|
| 1 | Model sınıfları, Data Annotation, Ana sayfa UI/UX | [📥 PDF](Reports/Hafta1_Raporu.pdf) |
| 2 | Hasta paneli, Profil güncelleme, Bildirim altyapısı | [📥 PDF](Reports/Hafta2_Raporu.pdf) |
| 3 | Doktor paneli, Çalışma takvimi, Reçete temel yapısı | [📥 PDF](Reports/Hafta3_Raporu.pdf) |
| 4 | Admin grafik/istatistik ekranları, Raporlama sistemi | [📥 PDF](Reports/Hafta4_Raporu.pdf) |
| 5 | Gelişmiş arama/filtreleme, UX iyileştirmeleri | [📥 PDF](Reports/Hafta5_Raporu.pdf) |
| 6 | API katmanı, Tahsilat sayfası, Randevu fiyat alanı | [📥 PDF](Reports/Hafta6_Raporu.pdf) |
| 7 | Dosya yükleme, Tıbbi geçmiş modülü, Dijital reçete | [📥 PDF](Reports/Hafta7_Raporu.pdf) |
| 8 | Güvenlik (CSRF/XSS), Caching, Dark Mode | [📥 PDF](Reports/Hafta8_Raporu.pdf) |
| 9 | AI Semptom Kontrolcüsü, QR Kod, Reçete PDF, Serilog, Testler | [📥 PDF](Reports/Hafta9_Raporu.pdf) |
| **10** | **Final: Eczane GPS Sistemi, Çift Katmanlı Değerlendirme, Güvenlik Zırhı** | [📥 Final PDF](Reports/Hafta10_Raporu.pdf) |

### 📊 Özet / Genel Raporlar

- [📄 İlk 5 Hafta Genel Rapor (PDF)](Reports/Ilk5Hafta_Genel_Rapor.pdf)
- [📄 Son 5 Hafta Genel Rapor (PDF)](Reports/Son5Hafta_Genel_Rapor.pdf)

## 🔐 Sistem Rolleri
- `Admin`
- `Doktor`
- `Sekreter`
- `Hasta`
