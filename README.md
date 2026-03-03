# Hastane Randevu Sistemi

ASP.NET Core MVC ve Entity Framework Core ile hazırlanmış bir hastane randevu uygulaması.

## Güncel Kapsam

### Hafta 1
- `AppUser` modeli TC, telefon, dogum tarihi, cinsiyet ve adres alanlari ile genisletildi.
- Kayit ekrani ve `RegisterViewModel` yeni alanlari destekleyecek sekilde guncellendi.
- Yeni kullanicilar kayit olduktan sonra dogrudan hasta paneline yonlendirilir.
- Ana hasta akisi icin gerekli bildirim servisi ve temel profil altyapisi eklendi.

### Hafta 2
- Hasta paneli eklendi: `Dashboard`, `Profile`, `Notifications`.
- Randevular hasta kullanicisi ile iliskilendirildi.
- Hasta tarafinda randevu gecmisi ve uygun durumlarda iptal akisi destekleniyor.
- Randevu olusturma, onay, tamamlama ve iptal hareketleri icin bildirim kayitlari uretiliyor.

## Teknolojiler
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server / LocalDB
- Bootstrap 5

## Baslatma
1. `appsettings.json` icindeki baglanti bilgisini kontrol edin.
2. Veritabani daha once eski sema ile olusturulduysa sifirlayin veya migration uygulayin.
3. Projeyi calistirin:

```powershell
dotnet run --project .\HastaneRandevuSistemi\HastaneRandevuSistemi.csproj
```

## Varsayilan Roller
- `Admin`
- `Doktor`
- `Hasta`

## Notlar
- Yeni migration dosyasi proje icine eklendi: `20260303120000_AddPatientPortalAndIdentityFields`
- Hasta paneline giris yapan kullanicilar `Patient/Dashboard` uzerinden yonlendirilir.
