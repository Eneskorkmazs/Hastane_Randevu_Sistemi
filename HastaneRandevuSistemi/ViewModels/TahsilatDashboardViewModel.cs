namespace HastaneRandevuSistemi.ViewModels
{
    public class TahsilatDashboardViewModel
    {
        public string? Search { get; set; }
        public int? DepartmentId { get; set; }
        public int? DoctorId { get; set; }
        public string Bucket { get; set; } = "all";

        public int PendingCount { get; set; }
        public decimal PendingTotal { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueTotal { get; set; }
        public int TodayCount { get; set; }
        public decimal TodayTotal { get; set; }
        public decimal CollectedTodayTotal { get; set; }
        public string TopDepartmentName { get; set; } = "-";
        public decimal TopDepartmentAmount { get; set; }
        public string TopDoctorName { get; set; } = "-";
        public decimal TopDoctorAmount { get; set; }

        public IReadOnlyList<AccountingLedgerItem> PendingItems { get; set; } = Array.Empty<AccountingLedgerItem>();
        public IReadOnlyList<AccountingLedgerItem> RecentCollectedItems { get; set; } = Array.Empty<AccountingLedgerItem>();
    }
}
