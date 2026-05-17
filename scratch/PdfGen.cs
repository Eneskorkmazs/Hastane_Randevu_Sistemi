using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class Program {
    public static void Main() {
        var lines = new List<string> {
            "Hastane Randevu Sistemi (HRS)",
            "-------------------------------------------------------------",
            "10. HAFTA VE BUYUK FINAL RAPORU",
            "Tarih: 18 Mayis 2026",
            "Sorumlu Gelistirici: Enes Korkmaz",
            "Sistem Surumu: v1.0 Final Release",
            "",
            "1. PROJE FINAL OZETI",
            "-------------------------------------------------------------",
            "10 haftalik gelistirme sureci sonunda Hastane Randevu Sistemi;",
            "temel randevu motorundan yapay zeka destekli semptom analizine,",
            "tarayici GPS'i kullanan eczane kesif modulunden cift katmanli",
            "hastane degerlendirme zirkina kadar tam donanimli bir saglik",
            "platformuna donusturulmustur.",
            "",
            "2. HAFTA 10: ONE CIKAN SON GUNCELLEMELER",
            "-------------------------------------------------------------",
            "A) Canli GPS Eczane Kesif Sistemi & Sanliurfa Genislemesi:",
            "Kullanici profilindeki adres kisitlamasi kaldirilarak tarayici",
            "GPS'i (mevcut konum) uzerinden anlik mesafe (Haversine) siralama",
            "ozelligi devreye alindi. Sanliurfa geneli icin 9 farkli eczane",
            "(Karakopru, Siverek, Birecik, Viransehir, Eyyubiye) tohumlandi.",
            "",
            "B) Cift Katmanli Hastane Degerlendirme Modulu:",
            "Hastalarin hastane hakkinda puan ve yorum birakabilecegi yeni",
            "modul eklendi. Mukerrer yorumlari engelleyen hem uygulama",
            "katmani hem de veritabani UNIQUE kisitlamasi devreye alindi.",
            "",
            "C) Yerellestirme ve Karakter Revizyonu (Mojibake Fix):",
            "Muhasebe tahsilat tablolari, bolum adlari, durum etiketleri ve",
            "duyurulardaki tum Turkce karakter bozulmalari temizlendi.",
            "",
            "D) Navbar Guvenlik Zirhi & Onbellek Yonetimi:",
            "Cikis sonrasi oturum cerezlerinin temizlenmesi ve geri tusuyla",
            "eski sayfalara erisilmesini onleyen NoStore onbellek politikalari",
            "tum sistem kontrolculerine uygulandi.",
            "",
            "3. TEKNIK ALTYAPI VE STACK",
            "-------------------------------------------------------------",
            "Backend : .NET 8 Core, Entity Framework Core, ASP.NET Identity",
            "Frontend: Razor Views, Bootstrap 5, Vanilla JS, FontAwesome 6",
            "Veritabani: SQLite (Gelistirme) / PostgreSQL (Uretim)",
            "Araclar : Serilog, QRCoder, Geolocation API, iText",
            "",
            "Proje tum hedefleriyle basariyla tamamlanmistir.",
            "HRS Hastane Randevu Sistemi (c) 2026 - Tum Haklari Saklidir."
        };
        byte[] pdf = GeneratePdf(lines);
        File.WriteAllBytes(@"c:\Users\Enes\Desktop\Hastane_Randevu_Sistemi-main\HastaneRandevuSistemi (2)\HastaneRandevuSistemi\Reports\Hafta10_Raporu.pdf", pdf);
        Console.WriteLine("Hafta10_Raporu.pdf basariyla uretildi.");
    }

    private static byte[] GeneratePdf(List<string> lines) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BT /F1 11 Tf 40 800 Td");
        foreach (var line in lines) {
            sb.AppendLine("0 -16 Td (" + line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)") + ") Tj");
        }
        sb.AppendLine("ET");
        string stream = sb.ToString();
        string content = "<< /Length " + stream.Length + " >>\nstream\n" + stream + "\nendstream";
        
        string[] objects = {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            content
        };

        using (MemoryStream ms = new MemoryStream())
        using (StreamWriter sw = new StreamWriter(ms, Encoding.ASCII)) {
            sw.Write("%PDF-1.4\n");
            long[] offsets = new long[objects.Length];
            for (int i = 0; i < objects.Length; i++) {
                sw.Flush();
                offsets[i] = ms.Position;
                sw.Write((i + 1) + " 0 obj\n" + objects[i] + "\nendobj\n");
            }
            sw.Flush();
            long xrefOffset = ms.Position;
            sw.Write("xref\n0 " + (objects.Length + 1) + "\n0000000000 65535 f \n");
            foreach (long offset in offsets) sw.Write(offset.ToString("0000000000") + " 00000 n \n");
            sw.Write("trailer\n<< /Size " + (objects.Length + 1) + " /Root 1 0 R >>\nstartxref\n" + xrefOffset + "\n%%EOF");
            sw.Flush();
            return ms.ToArray();
        }
    }
}
