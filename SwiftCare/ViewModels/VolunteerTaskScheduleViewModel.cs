using System;
using System.Collections.Generic;

namespace SwiftCare.ViewModels
{
    public class VolunteerScheduleViewModel
    {
        public string VolunteerId { get; set; } = string.Empty;

        public string VolunteerName { get; set; } = string.Empty;

        public string VolunteerNumber { get; set; } = string.Empty;

        public string SupervisorId { get; set; } = string.Empty;

        public string SupervisorName { get; set; } = string.Empty;

        public string? VolunteerStatus { get; set; }

        public VolunteerRosterScheduleViewModel? CurrentRoster { get; set; }

        public VolunteerRosterShiftScheduleViewModel? TodayShift { get; set; }

        public int TodayTaskCount { get; set; }

        public int WeekTaskCount { get; set; }

        public int WeekShiftCount { get; set; }

        public List<VolunteerRosterShiftScheduleViewModel> UpcomingShifts { get; set; }
            = new();
    }

    public class VolunteerRosterScheduleViewModel
    {
        public int VolunteerRosterId { get; set; }

        public string RosterName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class VolunteerRosterShiftScheduleViewModel
    {
        public int VolunteerRosterShiftId { get; set; }

        public int VolunteerRosterId { get; set; }

        public DateTime ShiftDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public int TaskCount { get; set; }

        public List<VolunteerTaskScheduleViewModel> Tasks { get; set; }
            = new();
    }

    public class VolunteerTaskScheduleViewModel
    {
        public int VolunteerTaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime TaskDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string? Location { get; set; }

        public string? CompletionNotes { get; set; }
    }
}