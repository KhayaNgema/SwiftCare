using System;
using System.Collections.Generic;
using System.Linq;
using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class VolunteerProfileViewModel
    {
        // =========================================================
        // VOLUNTEER
        // =========================================================

        public SwiftCare.Models.Volunteer Volunteer { get; set; }
            = null!;

        public string AreaName { get; set; }
            = "Volunteer Area";


        // =========================================================
        // CURRENT WEEK
        // =========================================================

        public DateTime CurrentWeekStart { get; set; }

        public DateTime CurrentWeekEnd { get; set; }


        // =========================================================
        // UPCOMING WEEK
        // =========================================================

        public DateTime UpcomingWeekStart { get; set; }

        public DateTime UpcomingWeekEnd { get; set; }


        // =========================================================
        // ROSTERS
        // =========================================================

        public VolunteerRoster? CurrentRoster { get; set; }

        public VolunteerRoster? UpcomingRoster { get; set; }


        // =========================================================
        // CURRENT ROSTER SHIFTS
        // =========================================================
        // Individual daily shifts belonging to the current
        // weekly roster.
        // =========================================================

        public List<VolunteerRosterShift> CurrentRosterShifts { get; set; }
            = new List<VolunteerRosterShift>();


        // =========================================================
        // UPCOMING ROSTER SHIFTS
        // =========================================================
        // Individual daily shifts belonging to the upcoming
        // weekly roster.
        // =========================================================

        public List<VolunteerRosterShift> UpcomingRosterShifts { get; set; }
            = new List<VolunteerRosterShift>();


        // =========================================================
        // CURRENT WEEK SHIFTS
        // =========================================================
        // Convenience properties used directly by the profile
        // calendar.
        // =========================================================

        public List<VolunteerRosterShift> CurrentWeekShifts =>
            CurrentRosterShifts
                .OrderBy(x => x.ShiftDate)
                .ToList();


        // =========================================================
        // UPCOMING WEEK SHIFTS
        // =========================================================
        // Convenience properties used directly by the profile
        // calendar.
        // =========================================================

        public List<VolunteerRosterShift> UpcomingWeekShifts =>
            UpcomingRosterShifts
                .OrderBy(x => x.ShiftDate)
                .ToList();


        // =========================================================
        // CURRENT WEEK ASSIGNMENTS
        // =========================================================

        public List<VolunteerTaskAssignment> CurrentWeekAssignments { get; set; }
            = new List<VolunteerTaskAssignment>();


        public List<VolunteerAttendance> Attendances { get; set; }
            = new();


        // =========================================================
        // UPCOMING WEEK ASSIGNMENTS
        // =========================================================

        public List<VolunteerTaskAssignment> UpcomingWeekAssignments { get; set; }
            = new List<VolunteerTaskAssignment>();


        // =========================================================
        // TASK LIBRARY
        // =========================================================

        public List<VolunteerTask> AreaTasks { get; set; }
            = new List<VolunteerTask>();


        // =========================================================
        // TASK COUNTS
        // =========================================================

        public int CurrentWeekTaskCount =>
            CurrentWeekAssignments.Count;


        public int UpcomingWeekTaskCount =>
            UpcomingWeekAssignments.Count;


        public int CompletedTaskCount =>
            CurrentWeekAssignments.Count(x =>
                x.Status ==
                VolunteerTaskAssignmentStatus.Completed)
            +
            UpcomingWeekAssignments.Count(x =>
                x.Status ==
                VolunteerTaskAssignmentStatus.Completed);


        public int InProgressTaskCount =>
            CurrentWeekAssignments.Count(x =>
                x.Status ==
                VolunteerTaskAssignmentStatus.InProgress)
            +
            UpcomingWeekAssignments.Count(x =>
                x.Status ==
                VolunteerTaskAssignmentStatus.InProgress);


        public int TotalTaskCount =>
            CurrentWeekAssignments.Count +
            UpcomingWeekAssignments.Count;


        // =========================================================
        // CURRENT SHIFT COUNTS
        // =========================================================

        public int CurrentScheduledShiftCount =>
            CurrentRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Scheduled);


        public int CurrentDayOffCount =>
            CurrentRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.DayOff);


        public int CurrentCancelledShiftCount =>
            CurrentRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Cancelled);


        public int CurrentCompletedShiftCount =>
            CurrentRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Completed);


        // =========================================================
        // UPCOMING SHIFT COUNTS
        // =========================================================

        public int UpcomingScheduledShiftCount =>
            UpcomingRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Scheduled);


        public int UpcomingDayOffCount =>
            UpcomingRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.DayOff);


        public int UpcomingCancelledShiftCount =>
            UpcomingRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Cancelled);


        public int UpcomingCompletedShiftCount =>
            UpcomingRosterShifts.Count(x =>
                x.Status ==
                VolunteerRosterShiftStatus.Completed);


        // =========================================================
        // TOTAL SHIFT COUNTS
        // =========================================================

        public int TotalScheduledShiftCount =>
            CurrentScheduledShiftCount +
            UpcomingScheduledShiftCount;


        public int TotalDayOffCount =>
            CurrentDayOffCount +
            UpcomingDayOffCount;


        public int TotalCancelledShiftCount =>
            CurrentCancelledShiftCount +
            UpcomingCancelledShiftCount;


        public int TotalCompletedShiftCount =>
            CurrentCompletedShiftCount +
            UpcomingCompletedShiftCount;
    }
}