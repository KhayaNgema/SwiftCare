using Microsoft.EntityFrameworkCore;
using SwiftCare.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftCare.Data
{
    public static class TaskSeeder
    {
        public static async Task SeedAsync(SwiftCareDbContext context)
        {

            var existingTitles = await context.VolunteerTasks
                .AsNoTracking()
                .Select(t => t.Title)
                .ToListAsync();

            var existingTitleSet = new HashSet<string>(
                existingTitles,
                StringComparer.OrdinalIgnoreCase);

            var tasks = new List<VolunteerTask>();


            // ============================================================
            // RESIDENT CARE
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents with morning routines",
                "Support residents with safe morning routines such as getting dressed, grooming, and preparing for the day.",
                VolunteerArea.ResidentCare,
                "Resident Rooms",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents with mobility",
                "Provide supervised assistance to residents who need help moving safely between rooms or common areas.",
                VolunteerArea.ResidentCare,
                "Resident Areas",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Provide companionship to residents",
                "Spend time with residents through conversation, reading, games, or other appropriate social interaction.",
                VolunteerArea.ResidentCare,
                "Resident Lounge",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with wheelchair mobility",
                "Help residents who use wheelchairs move safely to approved activities and common areas.",
                VolunteerArea.ResidentCare,
                "Resident Areas",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents during meal preparation",
                "Help residents get settled and ready for scheduled meals while following staff instructions.",
                VolunteerArea.ResidentCare,
                "Dining Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Escort residents to activities",
                "Safely escort residents from their rooms to scheduled recreational or social activities.",
                VolunteerArea.ResidentCare,
                "Activity Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Check resident comfort areas",
                "Observe assigned common resident areas and report comfort, safety, or environmental concerns to staff.",
                VolunteerArea.ResidentCare,
                "Resident Areas",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with reading for residents",
                "Read newspapers, books, magazines, or other approved material to residents who request assistance.",
                VolunteerArea.ResidentCare,
                "Resident Lounge",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Support resident social interaction",
                "Encourage residents to participate in appropriate social conversations and group interactions.",
                VolunteerArea.ResidentCare,
                "Common Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Report resident concerns to staff",
                "Report any resident safety, wellbeing, or environmental concerns noticed during volunteer activities.",
                VolunteerArea.ResidentCare,
                "Nursing Station",
                VolunteerTaskPriority.High);


            // ============================================================
            // ACTIVITIES & RECREATION
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Organize board games",
                "Set up and facilitate appropriate board games for residents.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Facilitate card games",
                "Prepare and facilitate suitable card games for residents who wish to participate.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Lead arts and crafts activity",
                "Prepare materials and assist residents with a supervised arts and crafts session.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize music session",
                "Prepare a suitable music session and encourage residents to participate through listening or singing.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with exercise session",
                "Help staff prepare and supervise appropriate low-impact recreational exercises for residents.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize movie afternoon",
                "Prepare the room and equipment for an approved movie or television activity.",
                VolunteerArea.ActivitiesAndRecreation,
                "Entertainment Room",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Facilitate memory games",
                "Run simple memory and cognitive engagement games suitable for participating residents.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize reading group",
                "Coordinate a group reading session using approved books, magazines, or newspapers.",
                VolunteerArea.ActivitiesAndRecreation,
                "Library",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare recreational materials",
                "Prepare, organize, and return approved recreational materials for resident activities.",
                VolunteerArea.ActivitiesAndRecreation,
                "Activity Store",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with resident birthday activity",
                "Help prepare and support an approved birthday activity for a resident.",
                VolunteerArea.ActivitiesAndRecreation,
                "Common Area",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // DINING SERVICES
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare dining tables",
                "Set up tables, chairs, and approved dining materials before a scheduled meal.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Clear dining tables",
                "Clear used dining materials after meals and place items in their designated areas.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with dining room setup",
                "Prepare the dining room before residents arrive for a scheduled meal.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents to dining area",
                "Help residents reach the dining area safely before scheduled meals.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize dining supplies",
                "Check and organize approved dining supplies and report shortages to staff.",
                VolunteerArea.DiningServices,
                "Dining Storage",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Clean dining activity surfaces",
                "Wipe and organize approved dining surfaces after meal activities according to facility procedures.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare beverage station",
                "Prepare the approved beverage station and ensure required supplies are available.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Support meal distribution",
                "Assist staff with approved non-clinical meal distribution activities.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with special dining event",
                "Help prepare the dining area for an approved special meal or resident event.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Check dining area readiness",
                "Check that the assigned dining area is clean, organized, and ready for the next scheduled activity.",
                VolunteerArea.DiningServices,
                "Dining Hall",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // ADMINISTRATION
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Organize volunteer documents",
                "Sort and organize approved volunteer administration documents according to staff instructions.",
                VolunteerArea.Administration,
                "Administration Office",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "File administrative records",
                "File approved non-confidential administrative records in their designated locations.",
                VolunteerArea.Administration,
                "Administration Office",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare volunteer sign-in materials",
                "Prepare approved sign-in sheets and other materials required for volunteer attendance.",
                VolunteerArea.Administration,
                "Reception",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize office supplies",
                "Check and organize general office supplies used by the volunteer and administration teams.",
                VolunteerArea.Administration,
                "Administration Office",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with reception organization",
                "Help keep the reception and visitor information area organized.",
                VolunteerArea.Administration,
                "Reception",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare notice board materials",
                "Prepare and organize approved notices, posters, and announcements for the facility notice board.",
                VolunteerArea.Administration,
                "Reception",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Update activity information board",
                "Assist staff with updating approved activity schedules and information displayed for residents.",
                VolunteerArea.Administration,
                "Common Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with volunteer records",
                "Help staff organize approved volunteer records and administrative paperwork.",
                VolunteerArea.Administration,
                "Administration Office",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare meeting materials",
                "Prepare approved documents, stationery, and room materials for volunteer or staff meetings.",
                VolunteerArea.Administration,
                "Meeting Room",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "General administrative support",
                "Assist administration staff with approved general office tasks that do not involve confidential information.",
                VolunteerArea.Administration,
                "Administration Office",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // GARDEN & OUTDOOR
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Water garden plants",
                "Water designated garden plants according to the facility's gardening schedule.",
                VolunteerArea.GardenAndOutdoor,
                "Garden",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Remove garden weeds",
                "Remove weeds from designated garden areas using approved tools and procedures.",
                VolunteerArea.GardenAndOutdoor,
                "Garden",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Maintain garden pathways",
                "Check designated garden paths and remove small amounts of leaves or debris.",
                VolunteerArea.GardenAndOutdoor,
                "Garden Paths",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare garden seating area",
                "Clean and arrange approved outdoor seating areas for resident use.",
                VolunteerArea.GardenAndOutdoor,
                "Garden",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with planting activity",
                "Assist with approved planting activities suitable for the facility garden.",
                VolunteerArea.GardenAndOutdoor,
                "Garden",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Check outdoor activity area",
                "Inspect the designated outdoor activity area and report hazards or maintenance concerns to staff.",
                VolunteerArea.GardenAndOutdoor,
                "Outdoor Area",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Collect fallen leaves",
                "Collect leaves and light garden debris from designated outdoor areas.",
                VolunteerArea.GardenAndOutdoor,
                "Garden",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Maintain outdoor flower beds",
                "Assist with basic approved maintenance of designated flower beds.",
                VolunteerArea.GardenAndOutdoor,
                "Flower Beds",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize garden tools",
                "Clean and return approved garden tools to their designated storage area.",
                VolunteerArea.GardenAndOutdoor,
                "Garden Storage",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare outdoor resident activity",
                "Prepare the designated outdoor area for an approved resident recreational activity.",
                VolunteerArea.GardenAndOutdoor,
                "Outdoor Area",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // EVENTS
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare event room",
                "Arrange the designated room for an approved resident or facility event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Set up event decorations",
                "Assist with setting up approved decorations for a resident or facility event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare event seating",
                "Arrange approved seating for residents attending a scheduled event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with event registration",
                "Welcome participants and assist with approved event attendance procedures.",
                VolunteerArea.Events,
                "Reception",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Support resident celebration",
                "Assist staff with an approved celebration or resident social event.",
                VolunteerArea.Events,
                "Common Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare event refreshments area",
                "Help prepare the designated refreshments area for an approved event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist event activity coordination",
                "Help organize and coordinate approved activities during a resident event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with event cleanup",
                "Help restore the event area after an approved event and return materials to storage.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare holiday event area",
                "Help prepare an approved holiday-themed area for residents.",
                VolunteerArea.Events,
                "Common Area",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Support special resident event",
                "Provide general non-clinical volunteer support during a scheduled resident event.",
                VolunteerArea.Events,
                "Event Room",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // TRANSPORTATION
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare resident transport area",
                "Prepare the designated area for residents before approved transportation activities.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents with transport check-in",
                "Assist residents with approved transportation check-in procedures.",
                VolunteerArea.Transportation,
                "Reception",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Escort resident to transport",
                "Escort a resident safely from the facility to the designated transport area.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with wheelchair transport preparation",
                "Help prepare approved mobility equipment before scheduled resident transportation.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Check transport supplies",
                "Check approved non-medical transportation supplies and report shortages to staff.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist residents returning from transport",
                "Assist residents from the designated transport area back into the facility.",
                VolunteerArea.Transportation,
                "Reception",
                VolunteerTaskPriority.High);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare transport documentation",
                "Help staff organize approved non-confidential transportation paperwork.",
                VolunteerArea.Transportation,
                "Administration Office",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Check transport waiting area",
                "Ensure the designated resident waiting area is organized and ready for scheduled transportation.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with transport scheduling materials",
                "Help organize approved transportation schedules and information for staff.",
                VolunteerArea.Transportation,
                "Administration Office",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Report transport safety concerns",
                "Report any observed transportation-area safety concerns to the responsible staff member.",
                VolunteerArea.Transportation,
                "Transport Area",
                VolunteerTaskPriority.High);


            // ============================================================
            // COMMUNITY OUTREACH
            // ============================================================

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare community outreach materials",
                "Prepare approved information and materials for a community outreach activity.",
                VolunteerArea.CommunityOutreach,
                "Administration Office",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize donation supplies",
                "Sort and organize approved donations received for the nursing home.",
                VolunteerArea.CommunityOutreach,
                "Storage Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare donation collection area",
                "Set up the designated area for an approved donation collection activity.",
                VolunteerArea.CommunityOutreach,
                "Reception",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist community visitor reception",
                "Welcome approved community visitors and direct them to the appropriate staff member.",
                VolunteerArea.CommunityOutreach,
                "Reception",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Organize community partnership materials",
                "Organize approved materials relating to community organizations and outreach partners.",
                VolunteerArea.CommunityOutreach,
                "Administration Office",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare outreach event area",
                "Prepare the designated facility area for an approved community outreach event.",
                VolunteerArea.CommunityOutreach,
                "Event Room",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with community activity",
                "Provide general non-clinical support during an approved community activity.",
                VolunteerArea.CommunityOutreach,
                "Common Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Sort community donations",
                "Sort approved donated items and place them in their designated storage or distribution areas.",
                VolunteerArea.CommunityOutreach,
                "Storage Area",
                VolunteerTaskPriority.Normal);

            AddTask(
                tasks,
                existingTitleSet,
                "Prepare community thank-you materials",
                "Prepare approved thank-you cards, notices, or materials for community supporters.",
                VolunteerArea.CommunityOutreach,
                "Administration Office",
                VolunteerTaskPriority.Low);

            AddTask(
                tasks,
                existingTitleSet,
                "Assist with community engagement event",
                "Support staff during an approved community engagement or partnership event.",
                VolunteerArea.CommunityOutreach,
                "Event Room",
                VolunteerTaskPriority.Normal);


            // ============================================================
            // SAVE
            // ============================================================

            if (tasks.Count == 0)
            {
                return;
            }

            await context.VolunteerTasks.AddRangeAsync(tasks);
            await context.SaveChangesAsync();
        }


        // ================================================================
        // HELPER METHOD
        // ================================================================

        private static void AddTask(
            List<VolunteerTask> tasks,
            HashSet<string> existingTitles,
            string title,
            string description,
            VolunteerArea area,
            string location,
            VolunteerTaskPriority priority)
        {
            if (existingTitles.Contains(title))
            {
                return;
            }

            tasks.Add(new VolunteerTask
            {
                Title = title,
                Description = description,
                Area = area,
                Location = location,
                Priority = priority,
                IsActive = true,
                VolunteerOpportunityId = null
            });

            existingTitles.Add(title);
        }
    }
}