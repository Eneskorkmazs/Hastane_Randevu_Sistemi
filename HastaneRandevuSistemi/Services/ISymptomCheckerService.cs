using HastaneRandevuSistemi.ViewModels;
using System.Collections.Generic;

namespace HastaneRandevuSistemi.Services
{
    /// <summary>
    /// Semptomları analiz ederek uygun departman önerileri döndüren servis arayüzü.
    /// </summary>
    public interface ISymptomCheckerService
    {
        /// <summary>
        /// Seçilen semptom anahtarlarına göre bölüm önerilerini döndürür.
        /// </summary>
        /// <param name="symptomKeys">Kullanıcının seçtiği semptom key listesi.</param>
        /// <returns>Güven skoruna göre azalan sırada sıralanmış öneri listesi.</returns>
        List<DepartmentSuggestion> Analyze(IEnumerable<string> symptomKeys);
        
        /// <summary>
        /// Doğal dil ile yazılan mesajları analiz eder ve sohbet botu mantığı ile cevap verir.
        /// </summary>
        ChatResponse ProcessChat(string message, List<string> history);
    }
}
