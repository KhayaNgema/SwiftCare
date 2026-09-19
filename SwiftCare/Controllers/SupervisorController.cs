using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.Services.VolunteerContracts;
using SwiftCare.Services.VolunteerOnboarding;
using System;
using System.Linq;
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
        // VOLUNTEER DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VolunteerProfile(
            string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var volunteer = await _context.Volunteers
                .AsNoTracking()
                .Include(v => v.Supervisor)
                .Include(v => v.Rosters)
                .Include(v => v.Tasks)
                .FirstOrDefaultAsync(
                    v =>
                        v.Id == id &&
                        v.SupervisorId == supervisorId,
                    cancellationToken);

            if (volunteer == null)
                return NotFound();

            return View(volunteer);
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
                .Include(r => r.Tasks)
                .Where(r => r.SupervisorId == supervisorId)
                .OrderByDescending(r => r.StartDate)
                .ThenBy(r => r.Volunteer!.LastName)
                .ToListAsync(cancellationToken);

            return View(rosters);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRoster(
            VolunteerRoster roster,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(roster.VolunteerId))
            {
                ModelState.AddModelError(
                    nameof(roster.VolunteerId),
                    "Please select a volunteer.");
            }

            if (roster.StartDate == default)
            {
                ModelState.AddModelError(
                    nameof(roster.StartDate),
                    "Please select a start date.");
            }

            // One roster represents exactly one week.
            var startDate = roster.StartDate.Date;
            var endDate = startDate.AddDays(6);

            // Force the roster to be exactly seven calendar days.
            roster.StartDate = startDate;
            roster.EndDate = endDate;

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(
                    v =>
                        v.Id == roster.VolunteerId &&
                        v.SupervisorId == supervisorId,
                    cancellationToken);

            if (volunteer == null)
            {
                ModelState.AddModelError(
                    nameof(roster.VolunteerId),
                    "The selected volunteer is not assigned to you.");
            }

            // Prevent two rosters for the same volunteer
            // covering the same week.
            if (volunteer != null)
            {
                var overlappingRoster =
                    await _context.VolunteerRosters
                        .AsNoTracking()
                        .AnyAsync(
                            r =>
                                r.VolunteerId == volunteer.Id &&
                                r.SupervisorId == supervisorId &&
                                r.StartDate.Date <= endDate &&
                                r.EndDate.Date >= startDate &&
                                r.Status != VolunteerRosterStatus.Cancelled,
                            cancellationToken);

                if (overlappingRoster)
                {
                    ModelState.AddModelError(
                        "",
                        "This volunteer already has a roster covering this week.");
                }
            }

            if (!ModelState.IsValid)
            {
                var volunteers = await _context.Volunteers
                    .AsNoTracking()
                    .Where(v =>
                        v.SupervisorId == supervisorId &&
                        v.Status == VolunteerStatus.Active)
                    .OrderBy(v => v.LastName)
                    .ThenBy(v => v.FirstName)
                    .ToListAsync(cancellationToken);

                ViewBag.Volunteers = volunteers;
                ViewBag.SelectedVolunteerId = roster.VolunteerId;

                return View(roster);
            }

            roster.SupervisorId = supervisorId;
            roster.Status = VolunteerRosterStatus.Draft;
            roster.CreatedDate = DateTime.UtcNow;
            roster.ModifiedDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(roster.RosterName))
            {
                roster.RosterName =
                    $"{volunteer!.FirstName} {volunteer.LastName} - " +
                    $"{startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
            }

            _context.VolunteerRosters.Add(roster);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Supervisor {SupervisorId} created roster {RosterId} for volunteer {VolunteerId}.",
                supervisorId,
                roster.VolunteerRosterId,
                roster.VolunteerId);

            return RedirectToAction(
                nameof(RosterDetails),
                new { id = roster.VolunteerRosterId });
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
                .Include(r => r.Tasks)
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
        // ADD TASK - GET
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

            ViewBag.Roster = roster;

            return View();
        }

        // ============================================================
        // ADD TASK - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(
            VolunteerTask task,
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var roster = await _context.VolunteerRosters
                .Include(r => r.Volunteer)
                .FirstOrDefaultAsync(
                    r =>
                        r.VolunteerRosterId == task.VolunteerRosterId &&
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

            task.VolunteerId = roster.VolunteerId;
            task.VolunteerRosterId = roster.VolunteerRosterId;
            task.AssignedById = supervisorId;

            var taskDate = task.TaskDate.Date;

            if (taskDate < roster.StartDate.Date ||
                taskDate > roster.EndDate.Date)
            {
                ModelState.AddModelError(
                    nameof(task.TaskDate),
                    "The task date must fall inside the roster week.");
            }

            if (task.EndTime <= task.StartTime)
            {
                ModelState.AddModelError(
                    nameof(task.EndTime),
                    "The task end time must be after the start time.");
            }

            if (string.IsNullOrWhiteSpace(task.Title))
            {
                ModelState.AddModelError(
                    nameof(task.Title),
                    "A task title is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roster = roster;
                return View(task);
            }

            task.TaskDate = taskDate;
            task.Status = VolunteerTaskStatus.Assigned;

            _context.VolunteerTasks.Add(task);

            roster.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Supervisor {SupervisorId} added task {TaskId} to roster {RosterId}.",
                supervisorId,
                task.VolunteerTaskId,
                roster.VolunteerRosterId);

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
                .Include(r => r.Tasks)
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

            if (!roster.Tasks.Any())
            {
                TempData["ErrorMessage"] =
                    "You must add at least one task before publishing the roster.";

                return RedirectToAction(
                    nameof(RosterDetails),
                    new { id });
            }

            roster.Status = VolunteerRosterStatus.Published;
            roster.ModifiedDate = DateTime.UtcNow;

            foreach (var task in roster.Tasks)
            {
                task.Status = VolunteerTaskStatus.Assigned;
                task.VolunteerId = roster.VolunteerId;
                task.AssignedById = supervisorId;
            }

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
        // ALL SUPERVISOR TASKS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Tasks(
            CancellationToken cancellationToken)
        {
            var supervisorId = await GetCurrentSupervisorIdAsync(
                cancellationToken);

            if (supervisorId == null)
                return Forbid();

            var tasks = await _context.VolunteerTasks
                .AsNoTracking()
                .Include(t => t.Volunteer)
                .Include(t => t.VolunteerRoster)
                .Where(t =>
                    t.Volunteer != null &&
                    t.Volunteer.SupervisorId == supervisorId)
                .OrderBy(t => t.TaskDate)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);

            return View(tasks);
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

            var tasks = await _context.VolunteerTasks
                .AsNoTracking()
                .Include(t => t.Volunteer)
                .Include(t => t.VolunteerRoster)
                .Where(t =>
                    t.TaskDate.Date == selectedDate &&
                    t.Volunteer != null &&
                    t.Volunteer.SupervisorId == supervisorId)
                .OrderBy(t => t.StartTime)
                .ThenBy(t => t.Volunteer!.LastName)
                .ToListAsync(cancellationToken);

            ViewBag.SelectedDate = selectedDate;

            return View(tasks);
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

            var tasks = await _context.VolunteerTasks
                .AsNoTracking()
                .Include(t => t.Volunteer)
                .Where(t =>
                    t.Volunteer != null &&
                    t.Volunteer.SupervisorId == supervisorId)
                .OrderByDescending(t => t.TaskDate)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);

            return View(tasks);
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