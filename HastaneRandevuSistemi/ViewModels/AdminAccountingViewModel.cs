namespace HastaneRandevuSistemi.ViewModels
{
    public class AdminAccountingViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; }
        public int? DoctorId { get; set; }

        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int CancelledAppointments { get; set; }

        public decimal EstimatedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CancelledRevenue { get; set; }
        public decimal AverageTicket { get; set; }

        public string TopDepartmentByAppointments { get; set; } = "-";
        public int TopDepartmentAppointmentCount { get; set; }
        public string TopDoctorByAppointments { get; set; } = "-";
        public int TopDoctorAppointmentCount { get; set; }
        public string TopDepartmentByRevenue { get; set; } = "-";
        public decimal TopDepartmentRevenue { get; set; }
        public string TopDoctorByRevenue { get; set; } = "-";
        public decimal TopDoctorRevenue { get; set; }

        public AccountingSelectedDepartmentSummary? SelectedDepartmentSummary { get; set; }

        public IReadOnlyList<AccountingDepartmentItem> DepartmentBreakdown { get; set; } = Array.Empty<AccountingDepartmentItem>();
        public IReadOnlyList<AccountingDoctorItem> DoctorBreakdown { get; set; } = Array.Empty<AccountingDoctorItem>();
        public IReadOnlyList<AccountingLedgerItem> RecentTransactions { get; set; } = Array.Empty<AccountingLedgerItem>();
        public IReadOnlyList<AccountingMonthlyDepartmentStat> MonthlyDepartmentStats { get; set; } = Array.Empty<AccountingMonthlyDepartmentStat>();
        public IReadOnlyList<AccountingLedgerItem> PendingCollectionQueue { get; set; } = Array.Empty<AccountingLedgerItem>();
        public decimal PendingCollectionQueueTotal { get; set; }
        public int PendingCollectionQueueCount { get; set; }
        public IReadOnlyList<AccountingTrendPoint> RevenueTrend { get; set; } = Array.Empty<AccountingTrendPoint>();
    }

    public class AccountingDepartmentItem
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int DoctorCount { get; set; }
        public int AppointmentCount { get; set; }
        public int UniquePatientCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal RevenueSharePercent { get; set; }
    }

    public class AccountingDoctorItem
    {
        public int? DoctorId { get; set; }
        public int? DepartmentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int UniquePatientCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CancelledRevenue { get; set; }
    }

    public class AccountingLedgerItem
    {
        public int AppointmentId { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsCollected { get; set; }
        public bool IsAppointmentFinished { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayLabel { get; set; } = string.Empty;
    }

    public class AccountingSelectedDepartmentSummary
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int DoctorCount { get; set; }
        public int AppointmentCount { get; set; }
        public int UniquePatientCount { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public string TopDoctorName { get; set; } = "-";
        public int TopDoctorAppointmentCount { get; set; }
        public decimal TopDoctorRevenue { get; set; }
    }

    public class AccountingMonthlyDepartmentStat
    {
        public string Label { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int UniquePatientCount { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
    }

    public class AccountingTrendPoint
    {
        public string Label { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public decimal CollectedRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
    }
}
