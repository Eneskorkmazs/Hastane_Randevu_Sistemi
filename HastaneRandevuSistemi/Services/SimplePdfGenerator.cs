using System.Globalization;
using System.Text;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Services
{
    public static class SimplePdfGenerator
    {
        public static byte[] CreatePrescriptionPdf(PrescriptionDraftViewModel model)
        {
            var lines = new[]
            {
                "HRS Hastanesi - Dijital Recete",
                $"Randevu No: #{model.AppointmentId}",
                $"Tarih: {model.PrescriptionDate:dd.MM.yyyy HH:mm}",
                $"Hasta: {model.PatientName} {model.PatientSurname}",
                $"Doktor: {model.DoctorName}",
                $"Poliklinik: {model.DepartmentName}",
                "",
                "Tani:",
                model.Diagnosis,
                "",
                "Ilaclar:",
                model.Medications,
                "",
                "Notlar:",
                string.IsNullOrWhiteSpace(model.Notes) ? "-" : model.Notes!,
                "",
                "Bu belge Hastane Randevu Sistemi tarafindan uretilmistir."
            };

            var content = BuildContentStream(lines.Select(NormalizePdfText));
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
            };

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write("%PDF-1.4\n");

            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                writer.Flush();
                offsets.Add(stream.Position);
                writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            writer.Flush();
            var xrefOffset = stream.Position;
            writer.Write($"xref\n0 {objects.Count + 1}\n");
            writer.Write("0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
            {
                writer.Write($"{offset.ToString("0000000000", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            writer.Flush();
            return stream.ToArray();
        }

        private static string BuildContentStream(IEnumerable<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 18 Tf");
            builder.AppendLine("50 790 Td");

            var first = true;
            foreach (var line in lines)
            {
                if (!first)
                {
                    builder.AppendLine("0 -24 Td");
                    builder.AppendLine(line is "Tani:" or "Ilaclar:" or "Notlar:" ? "/F1 13 Tf" : "/F1 11 Tf");
                }

                builder.AppendLine($"({Escape(line)}) Tj");
                first = false;
            }

            builder.AppendLine("ET");
            return builder.ToString();
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static string NormalizePdfText(string value)
        {
            return value
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ç', 'c').Replace('Ç', 'C')
                .Replace('–', '-').Replace('—', '-');
        }
    }
}
