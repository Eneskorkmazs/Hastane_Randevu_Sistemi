using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HastaneRandevuSistemi.ViewModels
{
    public class SymptomViewModel
    {
        [Required(ErrorMessage = "Lütfen en az bir semptom seçiniz.")]
        [Display(Name = "Semptomlarınız")]
        public List<string> SelectedSymptoms { get; set; } = new();

        [Display(Name = "Ek Notlar (İsteğe Bağlı)")]
        [StringLength(500, ErrorMessage = "Ek notlar en fazla 500 karakter olabilir.")]
        public string? AdditionalNotes { get; set; }

        public static readonly IReadOnlyList<SymptomOption> AvailableSymptoms = new List<SymptomOption>
        {
            new("bas_agrisi", "Baş Ağrısı", "fa-head-side-virus"),
            new("bas_donmesi", "Baş Dönmesi", "fa-dizzy"),
            new("migren", "Migren", "fa-head-side-cough"),
            new("bilinc_bulanikligi", "Bilinç Bulanıklığı", "fa-brain"),
            new("ates", "Ateş", "fa-temperature-high"),
            new("titreme", "Titreme / Üşüme", "fa-snowflake"),
            new("yorgunluk", "Aşırı Yorgunluk", "fa-bed"),
            new("gece_terleme", "Gece Terlemesi", "fa-droplet"),
            new("gogus_agrisi", "Göğüs Ağrısı", "fa-heart-pulse"),
            new("nefes_darligi", "Nefes Darlığı", "fa-lungs"),
            new("carpinti", "Çarpıntı", "fa-heartbeat"),
            new("kan_basinci_yuksek", "Yüksek Tansiyon Şikayeti", "fa-gauge-high"),
            new("karin_agrisi", "Karın Ağrısı", "fa-stomach"),
            new("bulanti_kusma", "Bulantı / Kusma", "fa-face-nauseated"),
            new("ishal", "İshal", "fa-toilet"),
            new("kabizlik", "Kabızlık", "fa-ban"),
            new("sarilik", "Sarılık", "fa-face-sad-tear"),
            new("eklem_agrisi", "Eklem Ağrısı", "fa-bone"),
            new("sirt_agrisi", "Sırt / Bel Ağrısı", "fa-person-walking"),
            new("kas_agrisi", "Kas Ağrısı", "fa-dumbbell"),
            new("bogaz_agrisi", "Boğaz Ağrısı", "fa-throat"),
            new("kulak_agrisi", "Kulak Ağrısı", "fa-ear"),
            new("burun_tikanikligi", "Burun Tıkanıklığı", "fa-nose"),
            new("ses_kisikligi", "Ses Kısıklığı", "fa-microphone-slash"),
            new("goz_kizarikligi", "Göz Kızarıklığı", "fa-eye"),
            new("gorme_bozuklugu", "Görme Bozukluğu", "fa-eye-slash"),
            new("dokuntu", "Cilt Döküntüsü", "fa-allergies"),
            new("kasinti", "Kaşıntı", "fa-hand-dots"),
            new("anksiyete", "Kaygı / Anksiyete", "fa-face-anxious-sweat"),
            new("depresyon", "Depresyon Belirtileri", "fa-cloud-rain"),
            new("uyku_bozuklugu", "Uyku Bozukluğu", "fa-moon"),
            new("adet_duzensizligi", "Adet Düzensizliği", "fa-venus"),
            new("idrar_yanmasi", "İdrar Yanması", "fa-fire-flame-curved"),
            new("sik_idrar", "Sık İdrar", "fa-restroom"),
        };
    }

    public record SymptomOption(string Key, string Label, string Icon);
}
