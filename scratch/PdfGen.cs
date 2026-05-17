using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

public class Program {
    public static void Main() {
        var lines = new List<string> {
            "Hastane Randevu Sistemi - Hafta 10 Final Raporu",
            "-----------------------------------------------",
            "Tarih: 30.04.2026",
            "Durum: Tamamlandi",
            "",
            "Yapilan Calismalar:",
            "1. Eczane Yonetimi ve Mesafe Hesaplama Sistemi",
            "2. Geolocation API ile Canli Konum Takibi",
            "3. ENS Akilli Asistan Sohbet Arayuzu",
            "4. Hasta Paneli Modernizasyonu",
            "5. Degerlendirme ve Yorum Sistemi",
            "6. 30+ Eczane Verisi ve GPS Koordinatlari",
            "",
            "Sonuc: Proje 10 haftalik surec sonunda tum",
            "fonksiyonlariyla basariyla tamamlanmistir.",
            "",
            "Gelistirici: Enes Korkmaz"
        };
        byte[] pdf = GeneratePdf(lines);
        File.WriteAllBytes(@"c:\Users\Enes\Desktop\Hastane_Randevu_Sistemi-main\HastaneRandevuSistemi (2)\HastaneRandevuSistemi\Reports\Hafta10_Raporu.pdf", pdf);
    }

    private static byte[] GeneratePdf(List<string> lines) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BT /F1 12 Tf 50 800 Td");
        foreach (var line in lines) {
            sb.AppendLine("0 -18 Td (" + line.Replace("(", "\\(").Replace(")", "\\)") + ") Tj");
        }
        sb.AppendLine("ET");
        string stream = sb.ToString();
        string content = "<< /Length " + stream.Length + " >>\nstream\n" + stream + "endstream";
        
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
