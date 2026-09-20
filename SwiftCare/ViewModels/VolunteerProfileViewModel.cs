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

        public SwiftCare.Models.Volunteer Volunteer { get; set; } = null!;

        public string AreaName { get; set; } = "Volunteer Area";


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
        // CURRENT WEEK ASSIGNMENTS
        // =========================================================

        public List<VolunteerTaskAssignment> CurrentWeekAssignments { get; set; }
            = new List<VolunteerTaskAssignment>();


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
        // COUNTS
        // =========================================================

        public int CurrentWeekTaskCount =>
            CurrentWeekAssignments.Count;


        public int UpcomingWeekTaskCount =>
            UpcomingWeekAssignments.Count;


        public int CompletedTaskCount =>
            CurrentWeekAssignments.Count(x =>
                x.Status == VolunteerTaskAssignmentStatus.Completed)
            +
            UpcomingWeekAssignments.Count(x =>
                x.Status == VolunteerTaskAssignmentStatus.Completed);


        public int InProgressTaskCount =>
            CurrentWeekAssignments.Count(x =>
                x.Status == VolunteerTaskAssignmentStatus.InProgress)
            +
            UpcomingWeekAssignments.Count(x =>
                x.Status == VolunteerTaskAssignmentStatus.InProgress);


        public int TotalTaskCount =>
            CurrentWeekAssignments.Count +
            UpcomingWeekAssignments.Count;
    }
}