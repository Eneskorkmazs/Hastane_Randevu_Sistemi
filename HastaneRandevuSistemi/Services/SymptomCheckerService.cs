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

        public ChatResponse ProcessChat(string message, List<string> history)
        {
            var allText = string.Join(" ", history) + " " + message;
            var lowerText = allText.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            
            var matchedKeys = new List<string>();
            
            // Nöroloji / Baş
            if (lowerText.Contains("baş") && (lowerText.Contains("ağrı") || lowerText.Contains("agri") || lowerText.Contains("çatlıyor") || lowerText.Contains("patlıyor"))) matchedKeys.Add("bas_agrisi");
            if (lowerText.Contains("baş") && (lowerText.Contains("dön") || lowerText.Contains("don"))) matchedKeys.Add("bas_donmesi");
            if (lowerText.Contains("migren")) matchedKeys.Add("migren");
            if (lowerText.Contains("bilinç") || lowerText.Contains("bayıl") || lowerText.Contains("bayil") || lowerText.Contains("uyuşma")) matchedKeys.Add("bilinc_bulanikligi");
            
            // Enfeksiyon / Dahiliye
            if (lowerText.Contains("ateş") || lowerText.Contains("ates") || lowerText.Contains("yanıyo") || lowerText.Contains("yaniyo") || lowerText.Contains("sıcak")) matchedKeys.Add("ates");
            if (lowerText.Contains("üşüt") || lowerText.Contains("soğuk algınlığı") || lowerText.Contains("grip") || lowerText.Contains("titre") || lowerText.Contains("usume") || lowerText.Contains("üşüme")) matchedKeys.Add("titreme");
            if (lowerText.Contains("halsiz") || lowerText.Contains("yorgun") || lowerText.Contains("bitkin") || lowerText.Contains("kalkamıyorum")) matchedKeys.Add("yorgunluk");
            if (lowerText.Contains("terli") || lowerText.Contains("terleme")) matchedKeys.Add("gece_terleme");
            
            // Kardiyoloji / Göğüs
            if (lowerText.Contains("göğüs") || lowerText.Contains("gogus") || lowerText.Contains("kalp") || lowerText.Contains("çarpıntı") || lowerText.Contains("carpinti") || lowerText.Contains("tansiyon") || lowerText.Contains("nabız")) {
                if (lowerText.Contains("çarpıntı") || lowerText.Contains("carpinti")) matchedKeys.Add("carpinti");
                else if (lowerText.Contains("tansiyon")) matchedKeys.Add("kan_basinci_yuksek");
                else matchedKeys.Add("gogus_agrisi");
            }
            if (lowerText.Contains("nefes") || lowerText.Contains("daral") || lowerText.Contains("tıkan") || lowerText.Contains("astım")) matchedKeys.Add("nefes_darligi");
            
            // Sindirim Sistemi / Dahiliye
            if (lowerText.Contains("mide") && (lowerText.Contains("bulant") || lowerText.Contains("bulan") || lowerText.Contains("kustu") || lowerText.Contains("kusma") || lowerText.Contains("yanma") || lowerText.Contains("eksi"))) matchedKeys.Add("bulanti_kusma");
            if (lowerText.Contains("karın") || lowerText.Contains("karin") || lowerText.Contains("karnım") || lowerText.Contains("sancı") || lowerText.Contains("kramp")) matchedKeys.Add("karin_agrisi");
            if (lowerText.Contains("ishal") || lowerText.Contains("cırcır") || lowerText.Contains("su gibi")) matchedKeys.Add("ishal");
            if (lowerText.Contains("kabız") || lowerText.Contains("tuvalete çıkam")) matchedKeys.Add("kabizlik");
            if (lowerText.Contains("sarar") || lowerText.Contains("sarılık")) matchedKeys.Add("sarilik");

            // KBB
            if (lowerText.Contains("boğaz") || lowerText.Contains("bogaz") || lowerText.Contains("yutkun") || lowerText.Contains("bademcik")) matchedKeys.Add("bogaz_agrisi");
            if (lowerText.Contains("kulak") || lowerText.Contains("çınla") || lowerText.Contains("cinla") || lowerText.Contains("duyma") || lowerText.Contains("işitme")) matchedKeys.Add("kulak_agrisi");
            if (lowerText.Contains("burun") || lowerText.Contains("akıntı") || lowerText.Contains("akinti") || lowerText.Contains("tıkanık") || lowerText.Contains("nezle") || lowerText.Contains("hapşır")) matchedKeys.Add("burun_tikanikligi");
            if (lowerText.Contains("ses") && (lowerText.Contains("kısıl") || lowerText.Contains("kisil") || lowerText.Contains("çıkmı"))) matchedKeys.Add("ses_kisikligi");
            if (lowerText.Contains("öksür") || lowerText.Contains("oksur") || lowerText.Contains("balgam")) matchedKeys.Add("nefes_darligi");

            // Göz
            if (lowerText.Contains("göz") || lowerText.Contains("goz") || lowerText.Contains("görm") || lowerText.Contains("bulanık") || lowerText.Contains("kamaş")) {
                if (lowerText.Contains("kızar") || lowerText.Contains("kizar") || lowerText.Contains("kanlan") || lowerText.Contains("çapak")) matchedKeys.Add("goz_kizarikligi");
                else matchedKeys.Add("gorme_bozuklugu");
            }

            // Dermatoloji
            if (lowerText.Contains("kaşın") || lowerText.Contains("kasin") || lowerText.Contains("döküntü") || lowerText.Contains("dokuntu") || lowerText.Contains("kızarıklık") || lowerText.Contains("sivilce") || lowerText.Contains("leke") || lowerText.Contains("yara") || lowerText.Contains("ben ")) {
                if (lowerText.Contains("kaşın") || lowerText.Contains("kasin")) matchedKeys.Add("kasinti");
                else matchedKeys.Add("dokuntu");
            }

            // Fizik Tedavi / Ortopedi
            if (lowerText.Contains("sırt") || lowerText.Contains("sirt") || lowerText.Contains("bel ") || lowerText.Contains("belim") || lowerText.Contains("boyun") || lowerText.Contains("omuz")) matchedKeys.Add("sirt_agrisi");
            if (lowerText.Contains("eklem") || lowerText.Contains("kemik") || lowerText.Contains("diz") || lowerText.Contains("ayak") || lowerText.Contains("bilek") || lowerText.Contains("kırık") || lowerText.Contains("çıkık")) matchedKeys.Add("eklem_agrisi");
            if (lowerText.Contains("kas ") || lowerText.Contains("kasım") || lowerText.Contains("et kesiği")) matchedKeys.Add("kas_agrisi");

            // Psikiyatri
            if (lowerText.Contains("stres") || lowerText.Contains("kaygı") || lowerText.Contains("anksiyete") || lowerText.Contains("panik") || lowerText.Contains("korku")) matchedKeys.Add("anksiyete");
            if (lowerText.Contains("depresyon") || lowerText.Contains("üzgün") || lowerText.Contains("mutsuz") || lowerText.Contains("ağlama")) matchedKeys.Add("depresyon");
            if (lowerText.Contains("uyku") || lowerText.Contains("uyuyamı") || lowerText.Contains("uyanamı")) matchedKeys.Add("uyku_bozuklugu");

            // Kadın Doğum & Üroloji
            if (lowerText.Contains("adet") || lowerText.Contains("regl") || lowerText.Contains("kanama") || lowerText.Contains("gebe") || lowerText.Contains("hamile")) matchedKeys.Add("adet_duzensizligi");
            if (lowerText.Contains("idrar") || lowerText.Contains("işe") || lowerText.Contains("çiş") || lowerText.Contains("yanma") || lowerText.Contains("bobrek") || lowerText.Contains("böbrek")) {
                if (lowerText.Contains("yanma")) matchedKeys.Add("idrar_yanmasi");
                else matchedKeys.Add("sik_idrar");
            }

            bool hasDuration = lowerText.Contains("gün") || lowerText.Contains("gun") || lowerText.Contains("hafta") || lowerText.Contains("ay") || lowerText.Contains("saat") || lowerText.Contains("beri") || lowerText.Contains("zaman") || lowerText.Contains("yıldır") || lowerText.Contains("süredir") || lowerText.Contains("suredir") || lowerText.Contains("yeni") || lowerText.Contains("bugün") || lowerText.Contains("dun") || lowerText.Contains("dün");

            if (matchedKeys.Count == 0) {
                return new ChatResponse {
                    Message = "Anlıyorum. Size en doğru şekilde yardımcı olabilmem için şikayetinizden biraz daha detaylı bahseder misiniz? İsterseniz aşağıdaki seçeneklerden birini de seçebilirsiniz:",
                    IsFinished = false,
                    QuickReplies = new List<string> { "Başım Ağrıyor", "Ateşim Var", "Midem Bulanıyor", "Öksürüyorum", "Göğsüm Ağrıyor", "Karnım Ağrıyor" }
                };
            }

            if (!hasDuration && history.Count < 2) {
                return new ChatResponse {
                    Message = "Geçmiş olsun, anladım. Peki bu şikayetleriniz yaklaşık ne zamandır devam ediyor?",
                    IsFinished = false,
                    QuickReplies = new List<string> { "Sabahtan beri", "1-2 Gündür", "1 Haftadır", "Uzun Zamandır" }
                };
            }

            var suggestions = Analyze(matchedKeys);
            if (suggestions.Count > 0) {
                var topDept = suggestions.First().DepartmentName;
                return new ChatResponse {
                    Message = $"Anlattıklarınızı değerlendirdim. Şikayetlerinizin **{topDept}** polikliniğinin uzmanlık alanına girdiğini öngörüyorum. Sizin için en uygun bölümleri aşağıda listeledim, dilerseniz hemen randevu alabilirsiniz. Çok geçmiş olsun, acil şifalar dilerim!",
                    IsFinished = true,
                    Suggestions = suggestions
                };
            }
            
            return new ChatResponse {
                Message = "Anlattıklarınızı inceledim ancak kesin bir branş belirleyemedim. Kapsamlı bir muayene ve genel değerlendirme için **Dahiliye (İç Hastalıkları)** bölümüne görünmeniz en doğrusu olacaktır. Çok geçmiş olsun.",
                IsFinished = true,
                Suggestions = Analyze(new[] { "ates" }) 
            };
        }
    }
}
