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
- Hafta 6 kapsamındaki tamamlanan güncellemeler:
- Güvenlik ve bakım iyileştirmeleri yapıldı:
  - repoda tutulan hassas bağlantı ve e-posta ayarları temizlendi,
  - giriş formunda antiforgery koruması yeniden etkinleştirildi,
  - parola politikası güçlendirildi.
- Oturum ve arayüz davranışı iyileştirildi:
  - giriş/kayıt/şifre sıfırlama ekranlarında rol bazlı üst panel gösterimi baskılandı,
  - kimlik ekranlarında oturum menüsü ile yetkili panel bağlantıları artık görünmüyor.
- Yönetim tarafında veri bütünlüğü güçlendirildi:
  - doktor düzenleme akışında `UserId` bağının korunması sağlandı,
  - seed kullanıcı şifreleri yapılandırma temelli hale getirildi,
  - geliştirme ortamı için kontrollü fallback mantığı eklendi.

## Teknolojiler
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server / LocalDB
- Bootstrap 5

## Haftalık Raporlar
- [Hafta 1 Raporu PDF](Reports/Hafta1_Raporu.pdf)
- [Hafta 2 Raporu PDF](Reports/Hafta2_Raporu.pdf)
- [Hafta 3 Raporu PDF](Reports/Hafta3_Raporu.pdf)
- [Hafta 4 Raporu PDF](Reports/Hafta4_Raporu.pdf)
- [Hafta 5 Raporu PDF](Reports/Hafta5_Raporu.pdf)
- [Hafta 6 Raporu HTML](Reports/Hafta6_Raporu.html)

## Varsayılan Roller
- `Admin`
- `Doktor`
- `Hasta`

## Notlar
- Yeni migration dosyası proje içine eklendi: `20260303120000_AddPatientPortalAndIdentityFields`
- Hasta paneline giriş yapan kullanıcılar `Patient/Dashboard` üzerinden yönlendirilir.

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

