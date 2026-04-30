using System.Collections.Generic;

namespace HastaneRandevuSistemi.ViewModels
{
    /// <summary>
    /// AI Semptom Kontrolcüsü sonuç ekranı için ViewModel.
    /// </summary>
    public class SymptomResultViewModel
    {
        /// <summary>
        /// Kullanıcının seçtiği semptomların okunabilir listesi.
        /// </summary>
        public List<string> SelectedSymptomLabels { get; set; } = new();

        /// <summary>
        /// Öneri listesi (olasılığa göre azalan sırada).
        /// </summary>
        public List<DepartmentSuggestion> Suggestions { get; set; } = new();

        /// <summary>
        /// Servis tarafından üretilen genel uyarı mesajı (varsa).
        /// </summary>
        public string? WarningMessage { get; set; }
    }

    /// <summary>
    /// Tek bir bölüm önerisi.
    /// </summary>
    public class DepartmentSuggestion
    {
        /// <summary>
        /// Veritabanındaki departman Id'si (randevu oluşturmak için kullanılır).
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Bölüm adı.
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// 0–100 arasında güven skoru (yüzde).
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// Semptomlarla ilgili kısa açıklama.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// FontAwesome ikonu.
        /// </summary>
        public string Icon { get; set; } = "fa-hospital";

        /// <summary>
        /// Bootstrap renk sınıfı (success, warning, danger vb.)
        /// </summary>
        public string BadgeColor =>
            ConfidenceScore >= 70 ? "success" :
            ConfidenceScore >= 40 ? "warning" :
            "secondary";
    }
}
