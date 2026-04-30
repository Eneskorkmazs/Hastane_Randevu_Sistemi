using HastaneRandevuSistemi.Models;

namespace HastaneRandevuSistemi.ViewModels
{
    public class QueueItemViewModel
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentTime { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsPending { get; set; }
        public int QueuePosition { get; set; }
        /// <summary>Tahmini bekleme süresi (dakika)</summary>
        public int EstimatedWaitMinutes { get; set; }
    }
}
