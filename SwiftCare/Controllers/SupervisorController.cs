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
using System.Collections.Generic;
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(cancellationToken);

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
            var supervisor =
                await _userManager.GetUserAsync(User);

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
            // CURRENT WEEK
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
                .Include(r => r.Shifts)
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
                .Include(r => r.Shifts)
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
            // CURRENT WEEK SHIFTS
            // --------------------------------------------------------

            var currentWeekShifts = currentRoster?.Shifts
                .Where(s =>
                    s.ShiftDate.Date >= currentWeekStart &&
                    s.ShiftDate.Date <= currentWeekEnd)
                .OrderBy(s => s.ShiftDate)
                .ToList()
                ?? new List<VolunteerRosterShift>();

            // --------------------------------------------------------
            // UPCOMING WEEK SHIFTS
            // --------------------------------------------------------

            var upcomingWeekShifts = upcomingRoster?.Shifts
                .Where(s =>
                    s.ShiftDate.Date >= upcomingWeekStart &&
                    s.ShiftDate.Date <= upcomingWeekEnd)
                .OrderBy(s => s.ShiftDate)
                .ToList()
                ?? new List<VolunteerRosterShift>();

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

                CurrentRosterShifts = currentWeekShifts,
                UpcomingRosterShifts = upcomingWeekShifts,

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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var rosters = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .Include(r => r.Shifts)
                .Include(r => r.TaskAssignments)
                    .ThenInclude(a => a.VolunteerTask)
                .Where(r => r.SupervisorId == supervisorId)
                .OrderByDescending(r => r.StartDate)
                .ThenBy(r => r.Volunteer!.LastName)
                .ToListAsync(cancellationToken);

            return View(rosters);
        }


        // ============================================================
        // CREATE ROSTER + DAILY SHIFTS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoster(
            VolunteerRoster model,
            string weekType,
            List<DateTime> shiftDates,
            List<TimeSpan> shiftStartTimes,
            List<TimeSpan> shiftEndTimes,
            List<int> shiftStatuses,
            CancellationToken cancellationToken)
        {
            var supervisorId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Your session has expired. Please log in again."
                });
            }

            // --------------------------------------------------------
            // VALIDATE VOLUNTEER
            // --------------------------------------------------------

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
                    message =
                        "The volunteer could not be found."
                });
            }

            // --------------------------------------------------------
            // NORMALISE DATES
            // --------------------------------------------------------

            var startDate = model.StartDate.Date;
            var endDate = model.EndDate.Date;

            // --------------------------------------------------------
            // VALIDATE DATE RANGE
            // --------------------------------------------------------

            if (endDate < startDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The roster end date cannot be before the start date."
                });
            }

            var numberOfDays =
                (endDate - startDate).Days + 1;

            // --------------------------------------------------------
            // ROSTER = MONDAY - FRIDAY
            // --------------------------------------------------------

            if (numberOfDays != 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "A roster must cover exactly 5 days, Monday to Friday."
                });
            }

            if (startDate.DayOfWeek != DayOfWeek.Monday)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "A weekly roster must start on a Monday."
                });
            }

            if (endDate.DayOfWeek != DayOfWeek.Friday)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "A weekly roster must end on a Friday."
                });
            }

            // --------------------------------------------------------
            // VALIDATE DAILY SHIFT DATA
            // --------------------------------------------------------

            if (shiftDates == null ||
                shiftStartTimes == null ||
                shiftEndTimes == null ||
                shiftStatuses == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Daily shift information is required."
                });
            }

            if (shiftDates.Count != 5 ||
                shiftStartTimes.Count != 5 ||
                shiftEndTimes.Count != 5 ||
                shiftStatuses.Count != 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The roster must contain exactly 5 daily shifts."
                });
            }

            // --------------------------------------------------------
            // EXPECTED MONDAY - FRIDAY DATES
            // --------------------------------------------------------

            var expectedDates = Enumerable
                .Range(0, 5)
                .Select(i =>
                    startDate.AddDays(i))
                .ToList();

            var suppliedDates = shiftDates
                .Select(d => d.Date)
                .OrderBy(d => d)
                .ToList();

            if (!expectedDates.SequenceEqual(suppliedDates))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The daily shifts must contain exactly one shift for each weekday from Monday to Friday."
                });
            }

            // --------------------------------------------------------
            // CHECK OVERLAPPING ROSTERS
            // --------------------------------------------------------

            var overlappingRoster =
                await _context.VolunteerRosters
                    .AsNoTracking()
                    .AnyAsync(
                        r =>
                            r.VolunteerId == model.VolunteerId &&
                            r.Status !=
                                VolunteerRosterStatus.Cancelled &&
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
            // CREATE ROSTER
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

                StartDate =
                    startDate,

                EndDate =
                    endDate,

                Status =
                    VolunteerRosterStatus.Draft,

                Notes =
                    string.IsNullOrWhiteSpace(model.Notes)
                        ? null
                        : model.Notes.Trim(),

                VolunteerId =
                    volunteer.Id,

                SupervisorId =
                    supervisorId,

                CreatedDate =
                    DateTime.Now,

                ModifiedDate =
                    DateTime.Now
            };

            _context.VolunteerRosters.Add(roster);

            // --------------------------------------------------------
            // CREATE THE 5 DAILY SHIFTS
            // --------------------------------------------------------

            for (var i = 0; i < 5; i++)
            {
                var shiftDate =
                    shiftDates[i].Date;

                var status =
                    (VolunteerRosterShiftStatus)shiftStatuses[i];

                // ----------------------------------------------------
                // MAKE SURE DATE IS PART OF THIS ROSTER
                // ----------------------------------------------------

                if (shiftDate < startDate ||
                    shiftDate > endDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            $"The shift date {shiftDate:dd MMMM yyyy} is outside the roster date range."
                    });
                }

                // ----------------------------------------------------
                // DAY OFF
                // ----------------------------------------------------

                if (status ==
                    VolunteerRosterShiftStatus.DayOff)
                {
                    var dayOffShift =
                        new VolunteerRosterShift
                        {
                            ShiftDate =
                                shiftDate,

                            StartTime =
                                TimeSpan.Zero,

                            EndTime =
                                TimeSpan.Zero,

                            Status =
                                VolunteerRosterShiftStatus.DayOff,

                            Notes =
                                null,

                            CreatedDate =
                                DateTime.Now,

                            ModifiedDate =
                                DateTime.Now
                        };

                    // Attach through roster navigation
                    roster.Shifts.Add(dayOffShift);

                    continue;
                }

                // ----------------------------------------------------
                // SCHEDULED SHIFT
                // ----------------------------------------------------

                var startTime =
                    shiftStartTimes[i];

                var endTime =
                    shiftEndTimes[i];

                if (startTime >= endTime)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            $"The shift on {shiftDate:dddd, dd MMMM yyyy} has an invalid time range. Start time must be before end time."
                    });
                }

                var scheduledShift =
                    new VolunteerRosterShift
                    {
                        ShiftDate =
                            shiftDate,

                        StartTime =
                            startTime,

                        EndTime =
                            endTime,

                        Status =
                            VolunteerRosterShiftStatus.Scheduled,

                        Notes =
                            null,

                        CreatedDate =
                            DateTime.Now,

                        ModifiedDate =
                            DateTime.Now
                    };

                // Attach through roster navigation
                roster.Shifts.Add(scheduledShift);
            }

            // --------------------------------------------------------
            // SAVE ROSTER + ALL 5 SHIFTS TOGETHER
            // --------------------------------------------------------

            await _context.SaveChangesAsync(
                cancellationToken);

            // --------------------------------------------------------
            // LOG
            // --------------------------------------------------------

            _logger.LogInformation(
                "Supervisor {SupervisorId} created 5-day roster {RosterId} with 5 daily shifts for volunteer {VolunteerId}.",
                supervisorId,
                roster.VolunteerRosterId,
                volunteer.Id);

            // --------------------------------------------------------
            // RETURN RESPONSE
            // --------------------------------------------------------

            return Json(new
            {
                success = true,

                message =
                    "Roster and daily shifts were created successfully.",

                rosterId =
                    roster.VolunteerRosterId,

                rosterName =
                    roster.RosterName,

                description =
                    roster.Description,

                notes =
                    roster.Notes,

                startDate =
                    roster.StartDate.ToString("yyyy-MM-dd"),

                endDate =
                    roster.EndDate.ToString("yyyy-MM-dd"),

                status =
                    roster.Status.GetDisplayName(),

                statusClass =
                    roster.Status
                        .ToString()
                        .ToLowerInvariant(),

                shiftCount =
                    roster.Shifts.Count,

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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .Include(r => r.Supervisor)
                .Include(r => r.Shifts)
                    .ThenInclude(s => s.Attendance)
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .AsNoTracking()
                .Include(r => r.Volunteer)
                .Include(r => r.Shifts)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == rosterId &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status !=
                VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Tasks can only be added while the roster is in Draft status.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id = rosterId });
            }

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
            // ========================================================
            // CURRENT SUPERVISOR
            // ========================================================

            var supervisorId = _userManager.GetUserId(User);

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

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A volunteer must be selected."
                });
            }

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
            // NORMALISE TASK DATE
            // ========================================================

            var taskDateOnly = taskDate.Date;

            // ========================================================
            // VALIDATE TASK TIME
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
            // ONLY MONDAY - FRIDAY
            // ========================================================

            if (taskDateOnly.DayOfWeek == DayOfWeek.Saturday ||
                taskDateOnly.DayOfWeek == DayOfWeek.Sunday)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tasks can only be scheduled from Monday to Friday."
                });
            }

            // ========================================================
            // CURRENT / UPCOMING WEEK
            // ========================================================

            var today = DateTime.Today;

            var daysFromMonday =
                ((int)today.DayOfWeek -
                 (int)DayOfWeek.Monday +
                 7) % 7;

            var currentWeekStart =
                today.AddDays(-daysFromMonday).Date;

            var currentWeekEnd =
                currentWeekStart.AddDays(4).Date;

            var upcomingWeekStart =
                currentWeekStart.AddDays(7).Date;

            var upcomingWeekEnd =
                upcomingWeekStart.AddDays(4).Date;

            // ========================================================
            // TASK MUST BE IN CURRENT OR UPCOMING WEEK
            // ========================================================

            if (taskDateOnly < currentWeekStart ||
                taskDateOnly > upcomingWeekEnd)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The task date must be within the current or upcoming Monday to Friday week."
                });
            }

            // ========================================================
            // TASK CANNOT BE IN THE PAST
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

            // ========================================================
            // TODAY MUST BE AT LEAST 1 HOUR FROM NOW
            // ========================================================

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
            // FIND TASK
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
                    message = "The selected task could not be found."
                });
            }

            // ========================================================
            // VALIDATE VOLUNTEER AREA
            // ========================================================

            var volunteerAreaValue =
                Convert.ToString(volunteer.PreferredArea)?.Trim()
                ?? string.Empty;

            var normalizedVolunteerArea =
                NormalizeArea(volunteerAreaValue);

            var taskArea =
                NormalizeArea(task.Area.ToString());

            var taskDisplayArea =
                NormalizeArea(task.Area.GetDisplayName());

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
            // FIND VOLUNTEER ROSTER
            // ========================================================

            var roster = await _context.VolunteerRosters
                .Include(r => r.Shifts)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerId == volunteerId &&
                        r.SupervisorId == supervisorId &&
                        r.Status != VolunteerRosterStatus.Cancelled &&
                        r.StartDate.Date <= taskDateOnly &&
                        r.EndDate.Date >= taskDateOnly,
                    cancellationToken);

            if (roster == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "There is no roster covering this date. Create the volunteer's roster first."
                });
            }

            // ========================================================
            // FIND DAILY SHIFT
            // ========================================================

            var shift = roster.Shifts
                .FirstOrDefault(s =>
                    s.ShiftDate.Date == taskDateOnly);

            // ========================================================
            // NO SHIFT = NO TASK
            // ========================================================

            if (shift == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This volunteer has no shift configured for the selected date. The task cannot be assigned."
                });
            }

            // ========================================================
            // DAY OFF = ABSOLUTELY NO TASK
            // ========================================================

            if (shift.Status ==
                VolunteerRosterShiftStatus.DayOff)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This volunteer is scheduled for a day off on the selected date. A task cannot be assigned."
                });
            }

            // ========================================================
            // CANCELLED SHIFT = NO TASK
            // ========================================================

            if (shift.Status ==
                VolunteerRosterShiftStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The volunteer's shift for this date has been cancelled. A task cannot be assigned."
                });
            }

            // ========================================================
            // SHIFT MUST BE A VALID WORKING SHIFT
            // ========================================================

            if (shift.EndTime <= shift.StartTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The volunteer's roster shift has invalid working hours. The task cannot be assigned."
                });
            }

            // ========================================================
            // TASK MUST FIT COMPLETELY INSIDE SHIFT
            // ========================================================
            //
            // Example:
            //
            // Shift: 08:00 - 16:00
            //
            // Valid:
            // 08:00 - 09:00
            // 10:00 - 12:00
            // 15:00 - 16:00
            //
            // Invalid:
            // 07:00 - 09:00
            // 15:00 - 17:00
            // 07:00 - 17:00
            //
            // ========================================================

            if (startTime < shift.StartTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"The task cannot start before the volunteer's shift. " +
                        $"The shift starts at {shift.StartTime:hh\\:mm}."
                });
            }

            if (endTime > shift.EndTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"The task cannot end after the volunteer's shift. " +
                        $"The shift ends at {shift.EndTime:hh\\:mm}."
                });
            }

            // ========================================================
            // FINAL SHIFT BOUNDARY CHECK
            // ========================================================

            if (startTime < shift.StartTime ||
                endTime > shift.EndTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"The task must fall completely within the roster shift " +
                        $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}."
                });
            }

            // ========================================================
            // FIND ANY OVERLAPPING TASK
            // ========================================================

            var overlappingAssignment =
                await _context.VolunteerTaskAssignments
                    .Include(a => a.VolunteerTask)
                    .Where(a =>
                        a.VolunteerId == volunteerId &&
                        a.TaskDate == taskDateOnly &&
                        a.Status !=
                            VolunteerTaskAssignmentStatus.Cancelled &&
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

            var sameTaskAssignment =
                await _context.VolunteerTaskAssignments
                    .Where(a =>
                        a.VolunteerId == volunteerId &&
                        a.VolunteerTaskId == volunteerTaskId &&
                        a.TaskDate == taskDateOnly &&
                        a.Status !=
                            VolunteerTaskAssignmentStatus.Cancelled &&
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

            _context.VolunteerTaskAssignments.Add(
                assignment);

            await _context.SaveChangesAsync(
                cancellationToken);

            // ========================================================
            // WEEK TYPE
            // ========================================================

            var isCurrentWeek =
                taskDateOnly >= currentWeekStart &&
                taskDateOnly <= currentWeekEnd;

            // ========================================================
            // SUCCESS
            // ========================================================

            return Json(new
            {
                success = true,

                message =
                    $"{task.Title} was assigned successfully.",

                volunteerTaskId =
                    assignment.VolunteerTaskId,

                assignmentId =
                    assignment.VolunteerTaskAssignmentId,

                rosterId =
                    roster.VolunteerRosterId,

                rosterShiftId =
                    shift.VolunteerRosterShiftId,

                rosterName =
                    roster.RosterName,

                taskDate =
                    taskDateOnly.ToString("yyyy-MM-dd"),

                startTime =
                    startTime.ToString(@"hh\:mm"),

                endTime =
                    endTime.ToString(@"hh\:mm"),

                shiftStartTime =
                    shift.StartTime.ToString(@"hh\:mm"),

                shiftEndTime =
                    shift.EndTime.ToString(@"hh\:mm"),

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


        // ============================================================
        // REMOVE TASK ASSIGNMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTaskAssignment(
            int volunteerTaskId)
        {
            try
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "You are not authorized."
                    });
                }

                var assignment =
                    await _context.VolunteerTaskAssignments
                        .FirstOrDefaultAsync(
                            x =>
                                x.VolunteerTaskId ==
                                volunteerTaskId);

                if (assignment == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Task assignment could not be found."
                    });
                }

                _context.VolunteerTaskAssignments.Remove(
                    assignment);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    volunteerTaskId =
                        volunteerTaskId,
                    message =
                        "Task removed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing task assignment {VolunteerTaskId}.",
                    volunteerTaskId);

                return Json(new
                {
                    success = false,
                    message =
                        "An error occurred while removing the task."
                });
            }
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            if (!assignment.VolunteerRosterId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerRosterId),
                    "A roster is required.");

                return View(assignment);
            }

            var roster = await _context.VolunteerRosters
                .Include(r => r.Volunteer)
                .Include(r => r.Shifts)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId ==
                            assignment.VolunteerRosterId.Value &&
                        r.SupervisorId ==
                            supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status !=
                VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Tasks can only be added to Draft rosters.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new
                    {
                        id = roster.VolunteerRosterId
                    });
            }

            var task = await _context.VolunteerTasks
                .FirstOrDefaultAsync(
                    t =>
                        t.VolunteerTaskId ==
                            assignment.VolunteerTaskId &&
                        t.IsActive,
                    cancellationToken);

            if (task == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.VolunteerTaskId),
                    "The selected task could not be found.");
            }

            var taskDate =
                assignment.TaskDate.Date;

            if (taskDate < roster.StartDate.Date ||
                taskDate > roster.EndDate.Date)
            {
                ModelState.AddModelError(
                    nameof(assignment.TaskDate),
                    "The task date must fall inside the roster week.");
            }

            if (assignment.EndTime <=
                assignment.StartTime)
            {
                ModelState.AddModelError(
                    nameof(assignment.EndTime),
                    "The task end time must be after the start time.");
            }

            // --------------------------------------------------------
            // FIND DAILY SHIFT
            // --------------------------------------------------------

            var shift = roster.Shifts
                .FirstOrDefault(s =>
                    s.ShiftDate.Date == taskDate);

            if (shift == null)
            {
                ModelState.AddModelError(
                    nameof(assignment.TaskDate),
                    "A daily roster shift must be configured for this date.");
            }
            else if (shift.Status ==
                     VolunteerRosterShiftStatus.DayOff)
            {
                ModelState.AddModelError(
                    nameof(assignment.TaskDate),
                    "The volunteer is scheduled for a day off on this date.");
            }
            else if (assignment.StartTime < shift.StartTime ||
                     assignment.EndTime > shift.EndTime)
            {
                ModelState.AddModelError(
                    nameof(assignment.StartTime),
                    $"The task must fall within the roster shift " +
                    $"{shift.StartTime:hh\\:mm} - " +
                    $"{shift.EndTime:hh\\:mm}.");
            }

            assignment.VolunteerId =
                roster.VolunteerId;

            assignment.VolunteerRosterId =
                roster.VolunteerRosterId;

            assignment.AssignedById =
                supervisorId;

            assignment.Status =
                VolunteerTaskAssignmentStatus.Assigned;

            assignment.TaskDate =
                taskDate;

            assignment.CreatedDate =
                DateTime.UtcNow;

            assignment.ModifiedDate =
                null;

            if (!ModelState.IsValid)
            {
                var tasks =
                    await _context.VolunteerTasks
                        .AsNoTracking()
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.Area)
                        .ThenBy(t => t.Title)
                        .ToListAsync(cancellationToken);

                ViewBag.Roster =
                    roster;

                ViewBag.Tasks =
                    tasks;

                return View(assignment);
            }

            _context.VolunteerTaskAssignments.Add(
                assignment);

            roster.ModifiedDate =
                DateTime.UtcNow;

            await _context.SaveChangesAsync(
                cancellationToken);

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
                new
                {
                    id = roster.VolunteerRosterId
                });
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .Include(r => r.TaskAssignments)
                .Include(r => r.Shifts)
                .Include(r => r.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == id &&
                        r.SupervisorId == supervisorId,
                    cancellationToken);

            if (roster == null)
                return NotFound();

            if (roster.Status !=
                VolunteerRosterStatus.Draft)
            {
                TempData["ErrorMessage"] =
                    "Only Draft rosters can be published.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            // --------------------------------------------------------
            // REQUIRE ALL 7 DAILY SHIFTS
            // --------------------------------------------------------

            if (roster.Shifts.Count != 7)
            {
                TempData["ErrorMessage"] =
                    "You must configure all 7 daily roster shifts before publishing.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            var expectedDates = Enumerable
                .Range(0, 7)
                .Select(i =>
                    roster.StartDate.Date.AddDays(i))
                .ToHashSet();

            var actualDates = roster.Shifts
                .Select(s => s.ShiftDate.Date)
                .ToHashSet();

            if (!expectedDates.SetEquals(actualDates))
            {
                TempData["ErrorMessage"] =
                    "The roster must contain one shift for every day of the roster week.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            // --------------------------------------------------------
            // VALIDATE SHIFTS
            // --------------------------------------------------------

            foreach (var shift in roster.Shifts)
            {
                if (shift.Status ==
                    VolunteerRosterShiftStatus.Cancelled)
                {
                    continue;
                }

                if (shift.Status ==
                    VolunteerRosterShiftStatus.DayOff)
                {
                    continue;
                }

                if (shift.EndTime <=
                    shift.StartTime)
                {
                    TempData["ErrorMessage"] =
                        $"The shift on {shift.ShiftDate:dd MMMM yyyy} has an invalid time range.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }
            }

            // --------------------------------------------------------
            // REQUIRE TASKS
            // --------------------------------------------------------

            if (!roster.TaskAssignments.Any())
            {
                TempData["ErrorMessage"] =
                    "You must add at least one task before publishing the roster.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            // --------------------------------------------------------
            // VALIDATE TASKS AGAINST DAILY SHIFTS
            // --------------------------------------------------------

            foreach (var assignment in
                     roster.TaskAssignments)
            {
                if (assignment.TaskDate.Date <
                        roster.StartDate.Date ||
                    assignment.TaskDate.Date >
                        roster.EndDate.Date)
                {
                    TempData["ErrorMessage"] =
                        "One or more tasks are outside the roster week.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                if (assignment.EndTime <=
                    assignment.StartTime)
                {
                    TempData["ErrorMessage"] =
                        "One or more tasks have an invalid time range.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                var shift =
                    roster.Shifts.FirstOrDefault(
                        s =>
                            s.ShiftDate.Date ==
                            assignment.TaskDate.Date);

                if (shift == null)
                {
                    TempData["ErrorMessage"] =
                        $"No roster shift exists for {assignment.TaskDate:dd MMMM yyyy}.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                if (shift.Status ==
                    VolunteerRosterShiftStatus.DayOff)
                {
                    TempData["ErrorMessage"] =
                        $"A task is scheduled on the volunteer's day off on {assignment.TaskDate:dd MMMM yyyy}.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                if (assignment.StartTime <
                        shift.StartTime ||
                    assignment.EndTime >
                        shift.EndTime)
                {
                    TempData["ErrorMessage"] =
                        $"A task on {assignment.TaskDate:dd MMMM yyyy} falls outside its roster shift.";

                    return RedirectToAction(
                        nameof(RosterDetails),
                        new { id });
                }

                assignment.VolunteerId =
                    roster.VolunteerId;

                assignment.AssignedById =
                    supervisorId;

                if (assignment.Status ==
                    VolunteerTaskAssignmentStatus.Cancelled)
                {
                    continue;
                }

                assignment.Status =
                    VolunteerTaskAssignmentStatus.Assigned;

                assignment.ModifiedDate =
                    DateTime.UtcNow;
            }

            // --------------------------------------------------------
            // PUBLISH
            // --------------------------------------------------------

            roster.Status =
                VolunteerRosterStatus.Published;

            roster.ModifiedDate =
                DateTime.UtcNow;

            await _context.SaveChangesAsync(
                cancellationToken);

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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
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
                        a.Volunteer.SupervisorId ==
                            supervisorId)
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var selectedDate =
                date?.Date ?? DateTime.Today;

            var assignments =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(a => a.VolunteerTask)
                    .Include(a => a.Volunteer)
                    .Include(a => a.VolunteerRoster)
                        .ThenInclude(r => r!.Shifts)
                    .Where(a =>
                        a.TaskDate.Date ==
                            selectedDate &&
                        a.Volunteer != null &&
                        a.Volunteer.SupervisorId ==
                            supervisorId)
                    .OrderBy(a => a.StartTime)
                    .ThenBy(a => a.Volunteer!.LastName)
                    .ToListAsync(cancellationToken);

            ViewBag.SelectedDate =
                selectedDate;

            return View(assignments);
        }


        // ============================================================
        // ATTENDANCE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Attendance(
            CancellationToken cancellationToken)
        {
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
                    cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var volunteers = await _context.Volunteers
                .AsNoTracking()
                .Where(v =>
                    v.SupervisorId == supervisorId)
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
            var supervisorId =
                await GetCurrentSupervisorIdAsync(
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
                        a.Volunteer.SupervisorId ==
                            supervisorId)
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
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var supervisorExists =
                await _context.Supervisors
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


        // ============================================================
        // HELPERS
        // ============================================================

        private static string NormalizeArea(
            string? value)
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


        private static string GetDisplayName(
            Enum value)
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
    }
}