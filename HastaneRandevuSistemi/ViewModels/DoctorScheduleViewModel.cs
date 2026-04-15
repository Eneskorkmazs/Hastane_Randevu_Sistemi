namespace HastaneRandevuSistemi.ViewModels
{
    public class DoctorScheduleViewModel
    {
        public string DoctorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Title { get; set; } = "Uzm. Dr.";
        public int CurrentMonthOffset { get; set; }
        public string MonthTitle { get; set; } = string.Empty;
        public int AppointmentCountThisMonth { get; set; }
        public int TodayAppointmentCount { get; set; }
        public int UpcomingAppointmentCount { get; set; }
        public string DailyCapacityText { get; set; } = string.Empty;
        public IReadOnlyList<string> WorkingDays { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> WorkingHours { get; set; } = Array.Empty<string>();
        public string Summary { get; set; } = string.Empty;
        public IReadOnlyList<string> NextAvailableSlots { get; set; } = Array.Empty<string>();
        public IReadOnlyList<DoctorCalendarWeekViewModel> Weeks { get; set; } = Array.Empty<DoctorCalendarWeekViewModel>();
        public IReadOnlyList<DoctorCalendarDayDetailViewModel> DayDetails { get; set; } = Array.Empty<DoctorCalendarDayDetailViewModel>();
        public IReadOnlyList<DoctorCalendarAppointmentViewModel> TodayAppointments { get; set; } = Array.Empty<DoctorCalendarAppointmentViewModel>();
        public IReadOnlyList<DoctorCalendarAppointmentViewModel> UpcomingAppointments { get; set; } = Array.Empty<DoctorCalendarAppointmentViewModel>();
        public IReadOnlyList<DoctorPrescriptionWeekViewModel> PrescriptionWeeks { get; set; } = Array.Empty<DoctorPrescriptionWeekViewModel>();
    }

    public class DoctorCalendarWeekViewModel
    {
        public IReadOnlyList<DoctorCalendarDayViewModel> Days { get; set; } = Array.Empty<DoctorCalendarDayViewModel>();
    }

    public class DoctorCalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsSunday { get; set; }
        public bool IsWorkingDay { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayLabel { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public IReadOnlyList<DoctorCalendarAppointmentViewModel> Appointments { get; set; } = Array.Empty<DoctorCalendarAppointmentViewModel>();
    }

    public class DoctorCalendarDayDetailViewModel
    {
        public string DayKey { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsSunday { get; set; }
        public bool IsWorkingDay { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayLabel { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public IReadOnlyList<DoctorCalendarAppointmentViewModel> Appointments { get; set; } = Array.Empty<DoctorCalendarAppointmentViewModel>();
    }

    public class DoctorPrescriptionWeekViewModel
    {
        public string WeekLabel { get; set; } = string.Empty;
        public int UpcomingCount { get; set; }
    }

    public class DoctorCalendarAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }
}
