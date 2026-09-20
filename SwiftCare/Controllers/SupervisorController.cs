using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerContracts;
using SwiftCare.Services.VolunteerOnboarding;
using SwiftCare.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ILogger<SupervisorController> _logger;
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IVolunteerOnboardingService _onboardingService;
        private readonly IVolunteerContractService _contractService;
        private readonly FileUploadService _fileUploadService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public SupervisorController(
            ILogger<SupervisorController> logger,
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IVolunteerOnboardingService onboardingService,
            IVolunteerContractService contractService,
            FileUploadService fileUploadService,
            EmailService emailService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _onboardingService = onboardingService;
            _contractService = contractService;
            _fileUploadService = fileUploadService;
            _emailService = emailService;
            _configuration = configuration;
            _environment = environment;
        }

        // ============================================================
        // MY VOLUNTEERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Volunteers(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .Where(v => v.SupervisorId == supervisorId)
                .OrderBy(v => v.LastName)
                .ThenBy(v => v.FirstName)
                .ToListAsync(cancellationToken);

            return View(volunteers);
        }


        // ============================================================
        // VOLUNTEER PROFILE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VolunteerProfile(
            string id,
            CancellationToken cancellationToken)
        {
            var supervisor = await _userManager.GetUserAsync(User);

            if (supervisor == null)
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // GET VOLUNTEER
            // --------------------------------------------------------

            var volunteer = await _context.Volunteers
                .AsNoTracking()
                .Include(v => v.Supervisor)
                .FirstOrDefaultAsync(
                    v =>
                        v.Id == id &&
                        v.SupervisorId == supervisor.Id,
                    cancellationToken);

            if (volunteer == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // CALCULATE CURRENT WEEK
            // Monday -> Sunday
            // --------------------------------------------------------

            var today = DateTime.Today;

            var daysFromMonday =
                ((int)today.DayOfWeek -
                 (int)DayOfWeek.Monday +
                 7) % 7;

            var currentWeekStart =
                today.AddDays(-daysFromMonday).Date;

            var currentWeekEnd =
                currentWeekStart.AddDays(6).Date;

            // --------------------------------------------------------
            // UPCOMING WEEK
            // --------------------------------------------------------

            var upcomingWeekStart =
                currentWeekStart.AddDays(7).Date;

            var upcomingWeekEnd =
                upcomingWeekStart.AddDays(6).Date;

            // --------------------------------------------------------
            // CURRENT ROSTER
            // --------------------------------------------------------

            var currentRoster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Supervisor)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.VolunteerTask)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerId == volunteer.Id &&
                        r.SupervisorId == supervisor.Id &&
                        r.Status != VolunteerRosterStatus.Cancelled &&
                        r.StartDate <= currentWeekEnd &&
                        r.EndDate >= currentWeekStart,
                    cancellationToken);

            // --------------------------------------------------------
            // UPCOMING ROSTER
            // --------------------------------------------------------

            var upcomingRoster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Supervisor)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.VolunteerTask)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerId == volunteer.Id &&
                        r.SupervisorId == supervisor.Id &&
                        r.Status != VolunteerRosterStatus.Cancelled &&
                        r.StartDate <= upcomingWeekEnd &&
                        r.EndDate >= upcomingWeekStart,
                    cancellationToken);

            // --------------------------------------------------------
            // CURRENT + UPCOMING ASSIGNMENTS
            // --------------------------------------------------------

            var assignments = await _context.VolunteerTaskAssignments
                .AsNoTracking()
                .Include(a => a.VolunteerTask)
                .Include(a => a.VolunteerRoster)
                .Where(a =>
                    a.VolunteerId == volunteer.Id &&
                    a.TaskDate >= currentWeekStart &&
                    a.TaskDate < upcomingWeekEnd.AddDays(1) &&
                    a.Status != VolunteerTaskAssignmentStatus.Cancelled)
                .OrderBy(a => a.TaskDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.EndTime)
                .ThenBy(a => a.VolunteerTaskId)
                .ToListAsync(cancellationToken);

            // --------------------------------------------------------
            // SPLIT ASSIGNMENTS BY WEEK
            // --------------------------------------------------------

            var currentAssignments = assignments
                .Where(a =>
                    a.TaskDate.Date >= currentWeekStart &&
                    a.TaskDate.Date <= currentWeekEnd)
                .ToList();

            var upcomingAssignments = assignments
                .Where(a =>
                    a.TaskDate.Date >= upcomingWeekStart &&
                    a.TaskDate.Date <= upcomingWeekEnd)
                .ToList();

            // --------------------------------------------------------
            // DETERMINE VOLUNTEER AREA
            // --------------------------------------------------------

            var volunteerAreaValue =
                Convert.ToString(volunteer.PreferredArea)?.Trim()
                ?? string.Empty;

            var normalizedVolunteerArea =
                NormalizeArea(volunteerAreaValue);

            // --------------------------------------------------------
            // GET AREA DISPLAY NAME
            // --------------------------------------------------------

            string areaName = "Volunteer Area";

            if (volunteer.PreferredArea is Enum preferredAreaEnum)
            {
                areaName =
                    preferredAreaEnum.GetDisplayName();
            }
            else if (!string.IsNullOrWhiteSpace(volunteerAreaValue))
            {
                areaName = volunteerAreaValue;
            }

            // --------------------------------------------------------
            // GET ACTIVE TASK LIBRARY
            // --------------------------------------------------------

            var activeTasks = await _context.VolunteerTasks
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Area)
                .ThenBy(t => t.Title)
                .ToListAsync(cancellationToken);

            // --------------------------------------------------------
            // AUTOMATICALLY FILTER TASKS BY VOLUNTEER AREA
            //
            // No area dropdown.
            // No task dropdown.
            // --------------------------------------------------------

            var areaTasks = activeTasks
                .Where(task =>
                {
                    var enumName =
                        NormalizeArea(task.Area.ToString());

                    var displayName =
                        NormalizeArea(task.Area.GetDisplayName());

                    return
                        enumName == normalizedVolunteerArea ||
                        displayName == normalizedVolunteerArea;
                })
                .ToList();

            // --------------------------------------------------------
            // BUILD VIEW MODEL
            // --------------------------------------------------------

            var model = new VolunteerProfileViewModel
            {
                Volunteer = volunteer,

                AreaName = areaName,

                CurrentWeekStart = currentWeekStart,
                CurrentWeekEnd = currentWeekEnd,

                UpcomingWeekStart = upcomingWeekStart,
                UpcomingWeekEnd = upcomingWeekEnd,

                CurrentRoster = currentRoster,
                UpcomingRoster = upcomingRoster,

                CurrentWeekAssignments = currentAssignments,
                UpcomingWeekAssignments = upcomingAssignments,

                AreaTasks = areaTasks
            };

            return View(model);
        }

        // ============================================================
        // ROSTERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Rosters(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var rosters = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.VolunteerTask)
                .Where(r => r.SupervisorId == supervisorId)
                .OrderByDescending(r => r.StartDate)
                .ThenBy(r => r.Volunteer!.LastName)
                .ToListAsync(cancellationToken);

            return View(rosters);
        }
        // ============================================================
        // CREATE ROSTER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoster(
            VolunteerRoster model,
            string weekType,
            CancellationToken cancellationToken)
        {
            var supervisorId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Your session has expired. Please log in again."
                });
            }

            if (string.IsNullOrWhiteSpace(model.VolunteerId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A volunteer is required."
                });
            }

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(
                    v =>
                        v.Id == model.VolunteerId &&
                        v.SupervisorId == supervisorId,
                    cancellationToken);

            if (volunteer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The volunteer could not be found."
                });
            }

            var startDate = model.StartDate.Date;
            var endDate = model.EndDate.Date;

            // --------------------------------------------------------
            // Validate date range
            // --------------------------------------------------------

            if (endDate < startDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The roster end date cannot be before the start date."
                });
            }

            var numberOfDays =
                (endDate - startDate).Days + 1;

            if (numberOfDays != 7)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A roster must cover exactly 7 days."
                });
            }

            // --------------------------------------------------------
            // Ensure roster starts on Monday
            // --------------------------------------------------------

            if (startDate.DayOfWeek != DayOfWeek.Monday)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A weekly roster must start on a Monday."
                });
            }

            // --------------------------------------------------------
            // Check overlapping rosters
            // --------------------------------------------------------

            var overlappingRoster =
                await _context.VolunteerRosters
                    .AsNoTracking()
                    .AnyAsync(
                        r =>
                            r.VolunteerId == model.VolunteerId &&
                            r.Status != VolunteerRosterStatus.Cancelled &&
                            r.StartDate <= endDate &&
                            r.EndDate >= startDate,
                        cancellationToken);

            if (overlappingRoster)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "This volunteer already has a roster covering some or all of these dates."
                });
            }

            // --------------------------------------------------------
            // Create roster
            // --------------------------------------------------------

            var roster = new VolunteerRoster
            {
                RosterName =
                    string.IsNullOrWhiteSpace(model.RosterName)
                        ? $"{volunteer.FirstName} {volunteer.LastName} - Weekly Roster"
                        : model.RosterName.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),

                StartDate = startDate,
                EndDate = endDate,

                Status = VolunteerRosterStatus.Draft,

                Notes =
                    string.IsNullOrWhiteSpace(model.Notes)
                        ? null
                        : model.Notes.Trim(),

                VolunteerId = volunteer.Id,
                SupervisorId = supervisorId,

                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _context.VolunteerRosters.Add(roster);

            await _context.SaveChangesAsync(cancellationToken);

            return Json(new
            {
                success = true,

                message = "Roster created successfully.",

                rosterId = roster.VolunteerRosterId,

                rosterName = roster.RosterName,

                description = roster.Description,

                notes = roster.Notes,

                startDate = roster.StartDate.ToString("yyyy-MM-dd"),

                endDate = roster.EndDate.ToString("yyyy-MM-dd"),

                status = roster.Status.GetDisplayName(),

                statusClass =
                    roster.Status
                        .ToString()
                        .ToLowerInvariant(),

                weekType =
                    string.Equals(
                        weekType,
                        "upcoming",
                        StringComparison.OrdinalIgnoreCase)
                        ? "upcoming"
                        : "current"
            });
        }

        // ============================================================
        // ROSTER DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> RosterDetails(
            int id,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .Include(r => r.Supervisor)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.VolunteerTask)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == id &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            return View(roster);
        }

        // ============================================================
        // ADD TASK ASSIGNMENT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AddTask(
            int rosterId,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == rosterId &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status != VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Tasks can only be added while the roster is in Draft status.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id = rosterId });
            }

            // Get reusable task library.
            var tasks = await _context.VolunteerTasks
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Area)
                .ThenBy(t => t.Title)
                .ToListAsync(cancellationToken);

            ViewBag.Roster = roster;
            ViewBag.Tasks = tasks;

            return View();
        }
        // ============================================================
        // ASSIGN TASK FROM VOLUNTEER PROFILE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTaskFromProfile(
            string volunteerId,
            int volunteerTaskId,
            DateTime taskDate,
            TimeSpan startTime,
            TimeSpan endTime,
            CancellationToken cancellationToken)
        {
            var supervisorId = _userManager.GetUserId(User);

            // ========================================================
            // VALIDATE SUPERVISOR
            // ========================================================

            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Your session has expired. Please log in again."
                });
            }

            // ========================================================
            // VALIDATE VOLUNTEER
            // ========================================================

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(
                    v =>
                        v.Id == volunteerId &&
                        v.SupervisorId == supervisorId,
                    cancellationToken);

            if (volunteer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The volunteer could not be found."
                });
            }

            // ========================================================
            // VALIDATE TIME RANGE
            // ========================================================

            if (endTime <= startTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "End time must be after start time."
                });
            }

            // ========================================================
            // CALCULATE CURRENT / UPCOMING WEEK
            // ========================================================

            var today = DateTime.Today;

            var daysFromMonday =
                ((int)today.DayOfWeek -
                 (int)DayOfWeek.Monday +
                 7) % 7;

            var currentWeekStart =
                today.AddDays(-daysFromMonday).Date;

            var currentWeekEnd =
                currentWeekStart.AddDays(6).Date;

            var upcomingWeekStart =
                currentWeekStart.AddDays(7).Date;

            var upcomingWeekEnd =
                upcomingWeekStart.AddDays(6).Date;

            var taskDateOnly = taskDate.Date;

            // ========================================================
            // WEEKEND VALIDATION
            // ========================================================

            if (taskDateOnly.DayOfWeek == DayOfWeek.Saturday ||
                taskDateOnly.DayOfWeek == DayOfWeek.Sunday)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Tasks can only be scheduled from Monday to Friday."
                });
            }

            // ========================================================
            // DATE MUST BE CURRENT OR UPCOMING WEEK
            // ========================================================

            if (taskDateOnly < currentWeekStart ||
                taskDateOnly > upcomingWeekEnd)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The task date must be within the current or upcoming week."
                });
            }

            // ========================================================
            // PREVENT PAST / TOO-SOON TASKS
            // ========================================================
            //
            // A task scheduled for TODAY must start at least
            // 1 hour from the current server time.
            //
            // Example:
            //
            // Current time: 07:00
            //
            // 07:30 -> ❌
            // 07:59 -> ❌
            // 08:00 -> ❌
            // 08:30 -> ❌
            // 09:00 -> ✅
            //
            // Future dates are not subject to this 1-hour rule.
            //
            // ========================================================

            var now = DateTime.Now;

            if (taskDateOnly < now.Date)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "You cannot assign a task for a date that has already passed."
                });
            }

            if (taskDateOnly == now.Date)
            {
                var taskStartDateTime =
                    taskDateOnly.Add(startTime);

                var minimumAllowedStart =
                    now.AddHours(1);

                if (taskStartDateTime <= minimumAllowedStart)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            $"Tasks must be scheduled at least 1 hour from now. " +
                            $"The earliest available start time is " +
                            $"{minimumAllowedStart:HH:mm}."
                    });
                }
            }

            // ========================================================
            // GET TASK
            // ========================================================

            var task = await _context.VolunteerTasks
                .FirstOrDefaultAsync(
                    t =>
                        t.VolunteerTaskId == volunteerTaskId &&
                        t.IsActive,
                    cancellationToken);

            if (task == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "The selected task could not be found."
                });
            }

            // ========================================================
            // VALIDATE VOLUNTEER AREA
            // ========================================================

            var volunteerAreaValue =
                Convert.ToString(
                    volunteer.PreferredArea)?.Trim()
                ?? string.Empty;

            var normalizedVolunteerArea =
                NormalizeArea(volunteerAreaValue);

            var taskArea =
                NormalizeArea(task.Area.ToString());

            var taskDisplayArea =
                NormalizeArea(
                    task.Area.GetDisplayName());

            if (taskArea != normalizedVolunteerArea &&
                taskDisplayArea != normalizedVolunteerArea)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task does not belong to the volunteer's area."
                });
            }

            // ========================================================
            // FIND ROSTER COVERING THIS DATE
            // ========================================================

            var roster =
                await _context.VolunteerRosters
                    .FirstOrDefaultAsync(
                        r =>
                            r.VolunteerId == volunteerId &&
                            r.SupervisorId == supervisorId &&
                            r.Status != VolunteerRosterStatus.Cancelled &&
                            r.StartDate <= taskDateOnly &&
                            r.EndDate >= taskDateOnly,
                        cancellationToken);

            if (roster == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "There is no roster covering this date. Create the roster first."
                });
            }

            // ========================================================
            // FIND ANY OVERLAPPING TASK
            // ========================================================
            //
            // This prevents:
            //
            // Existing: 08:00 - 09:00
            //
            // 08:00 - 08:30  ❌
            // 08:10 - 08:25  ❌
            // 08:15 - 08:28  ❌
            // 08:30 - 08:45  ❌
            // 08:59 - 09:30  ❌
            //
            // But:
            //
            // 07:00 - 08:00  ✅
            // 09:00 - 10:00  ✅
            //
            // ========================================================

            var overlappingAssignment =
                await _context.VolunteerTaskAssignments
                    .Include(a => a.VolunteerTask)
                    .Where(a =>
                        a.VolunteerId == volunteerId &&
                        a.TaskDate == taskDateOnly &&
                        a.Status != VolunteerTaskAssignmentStatus.Cancelled &&

                        startTime < a.EndTime &&
                        endTime > a.StartTime)
                    .OrderBy(a => a.StartTime)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (overlappingAssignment != null)
            {
                var existingTaskTitle =
                    overlappingAssignment.VolunteerTask?.Title
                    ?? "another task";

                return Conflict(new
                {
                    success = false,

                    message =
                        $"This volunteer already has '{existingTaskTitle}' " +
                        $"scheduled from " +
                        $"{overlappingAssignment.StartTime:hh\\:mm} to " +
                        $"{overlappingAssignment.EndTime:hh\\:mm}. " +
                        "The selected time overlaps with an existing task."
                });
            }

            // ========================================================
            // SAME TASK DUPLICATE CHECK
            // ========================================================
            //
            // Explicitly prevent the same VolunteerTask from being
            // assigned to the same volunteer on the same date during
            // an overlapping time period.
            //
            // ========================================================

            var sameTaskAssignment =
                await _context.VolunteerTaskAssignments
                    .Where(a =>
                        a.VolunteerId == volunteerId &&
                        a.VolunteerTaskId == volunteerTaskId &&
                        a.TaskDate == taskDateOnly &&
                        a.Status != VolunteerTaskAssignmentStatus.Cancelled &&

                        startTime < a.EndTime &&
                        endTime > a.StartTime)
                    .OrderBy(a => a.StartTime)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (sameTaskAssignment != null)
            {
                return Conflict(new
                {
                    success = false,

                    message =
                        $"This task is already assigned to this volunteer " +
                        $"from {sameTaskAssignment.StartTime:hh\\:mm} to " +
                        $"{sameTaskAssignment.EndTime:hh\\:mm}."
                });
            }

            // ========================================================
            // CREATE ASSIGNMENT
            // ========================================================

            var assignment =
                new VolunteerTaskAssignment
                {
                    VolunteerTaskId =
                        task.VolunteerTaskId,

                    VolunteerId =
                        volunteerId,

                    VolunteerRosterId =
                        roster.VolunteerRosterId,

                    TaskDate =
                        taskDateOnly,

                    StartTime =
                        startTime,

                    EndTime =
                        endTime,

                    Status =
                        VolunteerTaskAssignmentStatus.Assigned,

                    AssignedById =
                        supervisorId,

                    CreatedDate =
                        DateTime.UtcNow
                };

            _context.VolunteerTaskAssignments.Add(assignment);

            await _context.SaveChangesAsync(
                cancellationToken);

            // ========================================================
            // DETERMINE WEEK
            // ========================================================

            var isCurrentWeek =
                taskDateOnly >= currentWeekStart &&
                taskDateOnly <= currentWeekEnd;

            // ========================================================
            // RETURN AJAX RESPONSE
            // ========================================================

            return Json(new
            {
                success = true,

                message =
                    $"{task.Title} was assigned successfully.",

                // IMPORTANT FOR CALENDAR JAVASCRIPT
                volunteerTaskId =
                    assignment.VolunteerTaskId,

                // Keep assignment ID available if needed elsewhere
                assignmentId =
                    assignment.VolunteerTaskAssignmentId,

                rosterId =
                    roster.VolunteerRosterId,

                rosterName =
                    roster.RosterName,

                taskDate =
                    taskDateOnly.ToString("yyyy-MM-dd"),

                startTime =
                    startTime.ToString(@"hh\:mm"),

                endTime =
                    endTime.ToString(@"hh\:mm"),

                title =
                    task.Title,

                description =
                    task.Description,

                area =
                    task.Area.GetDisplayName(),

                priority =
                    task.Priority.GetDisplayName(),

                priorityClass =
                    task.Priority
                        .ToString()
                        .ToLowerInvariant(),

                status =
                    assignment.Status.GetDisplayName(),

                statusClass =
                    assignment.Status
                        .ToString()
                        .ToLowerInvariant(),

                weekType =
                    isCurrentWeek
                        ? "current"
                        : "upcoming"
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTaskAssignment(int volunteerTaskId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You are not authorized."
                    });
                }

                var assignment = await _context.VolunteerTaskAssignments
                    .FirstOrDefaultAsync(x =>
                        x.VolunteerTaskId == volunteerTaskId);

                if (assignment == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task assignment could not be found."
                    });
                }

                _context.VolunteerTaskAssignments.Remove(assignment);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    volunteerTaskId = volunteerTaskId,
                    message = "Task removed successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while removing the task."
                });
            }
        }
        // ============================================================
        // HELPERS
        // ============================================================

        private static string NormalizeArea(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(
                    value
                        .Where(char.IsLetterOrDigit)
                        .ToArray())
                .ToLowerInvariant();
        }

        private static string GetDisplayName(Enum value)
        {
            var member = value
                .GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var displayAttribute =
                member?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name
                   ?? value.ToString();
        }
        // ============================================================
        // ADD TASK ASSIGNMENT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(
            VolunteerTaskAssignment assignment,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            // --------------------------------------------------------
            // GET ROSTER
            // --------------------------------------------------------

            if (!assignment.VolunteerRosterId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerRosterId),
                    "A roster is required.");

                return View(assignment);
            }

            var roster = await _context.VolunteerRosters
                .Include(r => r.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId ==
                        assignment.VolunteerRosterId.Value &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status != VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Tasks can only be added to Draft rosters.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id = roster.VolunteerRosterId });
            }

            // --------------------------------------------------------
            // GET TASK TEMPLATE
            // --------------------------------------------------------

            var task = await _context.VolunteerTasks
                .FirstOrDefaultAsync(
                    t =>
                        t.VolunteerTaskId == assignment.VolunteerTaskId &&
                        t.IsActive,
                    cancellationToken);

            if (task == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerTaskId),
                    "The selected task could not be found.");
            }

            // --------------------------------------------------------
            // DATE VALIDATION
            // --------------------------------------------------------

            var taskDate = assignment.TaskDate.Date;

            if (taskDate < roster.StartDate.Date ||
                taskDate > roster.EndDate.Date)
            {
                ModelState.AddModelError(
                    nameof(assignment.TaskDate),
                    "The task date must fall inside the roster week.");
            }

            // --------------------------------------------------------
            // TIME VALIDATION
            // --------------------------------------------------------

            if (assignment.EndTime <= assignment.StartTime)
            {
                ModelState.AddModelError(
                    nameof(assignment.EndTime),
                    "The task end time must be after the start time.");
            }

            // --------------------------------------------------------
            // RESTORE ROSTER VOLUNTEER
            // --------------------------------------------------------

            assignment.VolunteerId = roster.VolunteerId;
            assignment.VolunteerRosterId = roster.VolunteerRosterId;

            // Supervisor is the person assigning the task.
            assignment.AssignedById = supervisorId;

            // New assignment always starts as Assigned.
            assignment.Status =
                VolunteerTaskAssignmentStatus.Assigned;

            assignment.TaskDate = taskDate;
            assignment.CreatedDate = DateTime.UtcNow;
            assignment.ModifiedDate = null;

            // --------------------------------------------------------
            // CHECK MODEL
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                var tasks = await _context.VolunteerTasks
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Area)
                    .ThenBy(t => t.Title)
                    .ToListAsync(cancellationToken);

                ViewBag.Roster = roster;
                ViewBag.Tasks = tasks;

                return View(assignment);
            }

            // --------------------------------------------------------
            // CREATE ASSIGNMENT
            // --------------------------------------------------------

            _context.VolunteerTaskAssignments.Add(assignment);

            roster.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Supervisor {SupervisorId} assigned task {TaskId} to volunteer {VolunteerId} on {TaskDate}. AssignmentId={AssignmentId}.",
                supervisorId,
                assignment.VolunteerTaskId,
                assignment.VolunteerId,
                assignment.TaskDate,
                assignment.VolunteerTaskAssignmentId);

            TempData["SuccessMessage"] =
                "Task assigned successfully.";

            return RedirectToAction(
                nameof(RosterDetails),
                new { id = roster.VolunteerRosterId });
        }

        // ============================================================
        // PUBLISH ROSTER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishRoster(
            int id,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .Include(r => r.TaskAssignments)
                .Include(r => r.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == id &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status != VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Only Draft rosters can be published.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            if (!roster.TaskAssignments.Any())
            {
                TempData["ErrorMessage"] =
                    "You must add at least one task before publishing the roster.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            // Validate every assignment belongs to this roster's week.
            foreach (var assignment in roster.TaskAssignments)
            {
                if (assignment.TaskDate.Date < roster.StartDate.Date ||
                    assignment.TaskDate.Date > roster.EndDate.Date)
                {
                    TempData["ErrorMessage"] =
                        "One or more tasks are outside the roster week.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                if (assignment.EndTime <= assignment.StartTime)
                {
                    TempData["ErrorMessage"] =
                        "One or more tasks have an invalid time range.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                // Ensure the assignment belongs to this roster's volunteer.
                assignment.VolunteerId = roster.VolunteerId;
                assignment.AssignedById = supervisorId;

                if (assignment.Status ==
                    VolunteerTaskAssignmentStatus.Cancelled)
                {
                    continue;
                }

                assignment.Status =
                    VolunteerTaskAssignmentStatus.Assigned;

                assignment.ModifiedDate = DateTime.UtcNow;
            }

            roster.Status = VolunteerRosterStatus.Published;
            roster.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Supervisor {SupervisorId} published roster {RosterId}.",
                supervisorId,
                roster.VolunteerRosterId);

            TempData["SuccessMessage"] =
                "The volunteer roster has been published successfully.";

            return RedirectToAction(
                nameof(RosterDetails),
                new { id });
        }

        // ============================================================
        // ALL SUPERVISOR TASK ASSIGNMENTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Tasks(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var assignments = await _context.VolunteerTaskAssignments
                .AsNoTracking()
                .Include(a => a.VolunteerTask)
                .Include(a => a.Volunteer)
                .Include(a => a.VolunteerRoster)
                .Where(a =>
                    a.Volunteer != null &&
                    a.Volunteer.SupervisorId == supervisorId)
                .OrderBy(a => a.TaskDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);

            return View(assignments);
        }

        // ============================================================
        // SCHEDULE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Schedule(
            DateTime? date,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var selectedDate = date?.Date ?? DateTime.Today;

            var assignments =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(a => a.VolunteerTask)
                    .Include(a => a.Volunteer)
                    .Include(a => a.VolunteerRoster)
                    .Where(a =>
                        a.TaskDate.Date == selectedDate &&
                        a.Volunteer != null &&
                        a.Volunteer.SupervisorId == supervisorId)
                    .OrderBy(a => a.StartTime)
                    .ThenBy(a => a.Volunteer!.LastName)
                    .ToListAsync(cancellationToken);

            ViewBag.SelectedDate = selectedDate;

            return View(assignments);
        }

        // ============================================================
        // ATTENDANCE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Attendance(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .Where(v => v.SupervisorId == supervisorId)
                .OrderBy(v => v.LastName)
                .ThenBy(v => v.FirstName)
                .ToListAsync(cancellationToken);

            return View(volunteers);
        }

        // ============================================================
        // TASK PROGRESS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> TaskProgress(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var assignments =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(a => a.VolunteerTask)
                    .Include(a => a.Volunteer)
                    .Include(a => a.VolunteerRoster)
                    .Where(a =>
                        a.Volunteer != null &&
                        a.Volunteer.SupervisorId == supervisorId)
                    .OrderByDescending(a => a.TaskDate)
                    .ThenBy(a => a.StartTime)
                    .ToListAsync(cancellationToken);

            return View(assignments);
        }

        // ============================================================
        // CURRENT SUPERVISOR
        // ============================================================

        private async Task<string?> GetCurrentSupervisorIdAsync(
            CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var supervisorExists = await _context.Supervisors
                .AsNoTracking()
                .AnyAsync(
                    s =>
                        s.Id == userId &&
                        s.IsActive,
                    cancellationToken);

            return supervisorExists
                ? userId
                : null;
        }
    }
}