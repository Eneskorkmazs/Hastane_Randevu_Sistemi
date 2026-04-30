using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.Services
{
    public class SymptomCheckerService : ISymptomCheckerService
    {
        private sealed record Rule(string DepartmentName, string Icon, string Reason, int Weight);

        private static readonly IReadOnlyDictionary<string, Rule[]> Rules = new Dictionary<string, Rule[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["bas_agrisi"] = [new("Nöroloji", "fa-brain", "Baş ağrısı ve sinir sistemi kaynaklı yakınmalar için uygundur.", 34)],
            ["bas_donmesi"] = [new("Nöroloji", "fa-brain", "Baş dönmesi denge ve nörolojik değerlendirme gerektirebilir.", 32), new("Kulak Burun Boğaz", "fa-ear-listen", "İç kulak ve denge sistemi açısından da değerlendirilebilir.", 22)],
            ["migren"] = [new("Nöroloji", "fa-brain", "Migren tipi ağrılarda nöroloji ilk başvuru bölümüdür.", 45)],
            ["bilinc_bulanikligi"] = [new("Nöroloji", "fa-brain", "Bilinç değişiklikleri acil ve nörolojik değerlendirme gerektirir.", 50)],
            ["ates"] = [new("Enfeksiyon Hastalıkları", "fa-virus", "Ateş enfeksiyon hastalıkları açısından araştırılmalıdır.", 36), new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Genel sistemik değerlendirme gerekebilir.", 20)],
            ["titreme"] = [new("Enfeksiyon Hastalıkları", "fa-virus", "Titreme ve üşüme enfeksiyon bulgusu olabilir.", 30)],
            ["yorgunluk"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Uzun süren yorgunluk genel dahili değerlendirme gerektirir.", 28)],
            ["gece_terleme"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Gece terlemesi sistemik nedenler açısından incelenmelidir.", 28), new("Enfeksiyon Hastalıkları", "fa-virus", "Enfeksiyon kaynaklı gece terlemeleri görülebilir.", 24)],
            ["gogus_agrisi"] = [new("Kardiyoloji", "fa-heart-pulse", "Göğüs ağrısı kalp-damar sistemi açısından önceliklidir.", 55)],
            ["nefes_darligi"] = [new("Göğüs Hastalıkları", "fa-lungs", "Nefes darlığı solunum sistemi açısından değerlendirilmelidir.", 42), new("Kardiyoloji", "fa-heart-pulse", "Kalp kaynaklı nefes darlığı da olabilir.", 28)],
            ["carpinti"] = [new("Kardiyoloji", "fa-heart-pulse", "Çarpıntı ritim bozukluğu açısından incelenmelidir.", 45)],
            ["kan_basinci_yuksek"] = [new("Kardiyoloji", "fa-heart-pulse", "Tansiyon ve kalp-damar riski için uygundur.", 38), new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Hipertansiyon takibi dahiliye tarafından da yapılabilir.", 26)],
            ["karin_agrisi"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Karın ağrısında genel dahili değerlendirme gerekir.", 32), new("Genel Cerrahi", "fa-user-doctor", "Cerrahi nedenler dışlanmalıdır.", 22)],
            ["bulanti_kusma"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Bulantı ve kusma sindirim sistemi ile ilişkili olabilir.", 28)],
            ["ishal"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "İshal ve sıvı kaybı için dahiliye uygundur.", 30), new("Enfeksiyon Hastalıkları", "fa-virus", "Enfeksiyöz ishal olasılığı değerlendirilmelidir.", 24)],
            ["kabizlik"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Kabızlık sindirim sistemi değerlendirmesi gerektirir.", 25)],
            ["sarilik"] = [new("Dahiliye (İç Hastalıkları)", "fa-stethoscope", "Sarılık karaciğer ve safra yolları açısından incelenmelidir.", 44)],
            ["eklem_agrisi"] = [new("Ortopedi ve Travmatoloji", "fa-bone", "Eklem ağrıları kas-iskelet sistemi ile ilişkilidir.", 34), new("Fizik Tedavi ve Rehabilitasyon", "fa-person-walking", "Kronik eklem ağrılarında rehabilitasyon gerekebilir.", 24)],
            ["sirt_agrisi"] = [new("Fizik Tedavi ve Rehabilitasyon", "fa-person-walking", "Bel ve sırt ağrıları için ilk değerlendirme uygundur.", 34), new("Ortopedi ve Travmatoloji", "fa-bone", "Travma veya yapısal sorunlar açısından ortopedi gerekebilir.", 24)],
            ["kas_agrisi"] = [new("Fizik Tedavi ve Rehabilitasyon", "fa-person-walking", "Kas ağrılarında fizik tedavi değerlendirmesi yararlı olabilir.", 28)],
            ["bogaz_agrisi"] = [new("Kulak Burun Boğaz", "fa-ear-listen", "Boğaz ağrısı KBB alanına girer.", 38)],
            ["kulak_agrisi"] = [new("Kulak Burun Boğaz", "fa-ear-listen", "Kulak ağrısı KBB değerlendirmesi gerektirir.", 42)],
            ["burun_tikanikligi"] = [new("Kulak Burun Boğaz", "fa-ear-listen", "Burun tıkanıklığı KBB tarafından değerlendirilir.", 35)],
            ["ses_kisikligi"] = [new("Kulak Burun Boğaz", "fa-ear-listen", "Ses kısıklığı gırtlak ve ses telleri açısından incelenmelidir.", 34)],
            ["goz_kizarikligi"] = [new("Göz Hastalıkları", "fa-eye", "Göz kızarıklığı göz hastalıkları değerlendirmesi gerektirir.", 38)],
            ["gorme_bozuklugu"] = [new("Göz Hastalıkları", "fa-eye", "Görme bozukluğu için göz muayenesi önceliklidir.", 45)],
            ["dokuntu"] = [new("Dermatoloji", "fa-hand-dots", "Cilt döküntüleri dermatoloji alanına girer.", 40)],
            ["kasinti"] = [new("Dermatoloji", "fa-hand-dots", "Kaşıntı cilt hastalıkları veya alerji ile ilişkili olabilir.", 32)],
            ["anksiyete"] = [new("Psikiyatri", "fa-brain", "Kaygı belirtileri psikiyatri tarafından değerlendirilmelidir.", 42)],
            ["depresyon"] = [new("Psikiyatri", "fa-brain", "Depresyon belirtileri profesyonel ruh sağlığı desteği gerektirir.", 45)],
            ["uyku_bozuklugu"] = [new("Psikiyatri", "fa-brain", "Uyku bozuklukları ruhsal ve davranışsal nedenlerle ilişkili olabilir.", 30)],
            ["adet_duzensizligi"] = [new("Kadın Hastalıkları ve Doğum", "fa-venus", "Adet düzensizliği kadın sağlığı alanına girer.", 45)],
            ["idrar_yanmasi"] = [new("Üroloji", "fa-droplet", "İdrar yanması üriner sistem açısından değerlendirilmelidir.", 38), new("Enfeksiyon Hastalıkları", "fa-virus", "İdrar yolu enfeksiyonu olasılığı vardır.", 22)],
            ["sik_idrar"] = [new("Üroloji", "fa-droplet", "Sık idrar üriner sistem değerlendirmesi gerektirir.", 34)]
        };

        private readonly ApplicationDbContext _context;

        public SymptomCheckerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DepartmentSuggestion> Analyze(IEnumerable<string> symptomKeys)
        {
            var scores = new Dictionary<string, (int Score, string Icon, List<string> Reasons)>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in symptomKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Rules.TryGetValue(key, out var rules))
                {
                    continue;
                }

                foreach (var rule in rules)
                {
                    (int Score, string Icon, List<string> Reasons) current = scores.TryGetValue(rule.DepartmentName, out var existing)
                        ? existing
                        : (0, rule.Icon, new List<string>());

                    current.Score += rule.Weight;
                    current.Reasons.Add(rule.Reason);
                    scores[rule.DepartmentName] = current;
                }
            }

            var departments = _context.Departments
                .AsEnumerable()
                .ToDictionary(d => d.Name, d => d.Id, StringComparer.OrdinalIgnoreCase);

            return scores
                .Select(item => new DepartmentSuggestion
                {
                    DepartmentName = item.Key,
                    DepartmentId = departments.TryGetValue(item.Key, out var id) ? id : 0,
                    ConfidenceScore = Math.Clamp(item.Value.Score, 20, 95),
                    Icon = item.Value.Icon,
                    Reasoning = string.Join(" ", item.Value.Reasons.Distinct().Take(2))
                })
                .OrderByDescending(s => s.ConfidenceScore)
                .ThenBy(s => s.DepartmentName)
                .Take(4)
                .ToList();
        }
    }
}
