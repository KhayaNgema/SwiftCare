using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
using SwiftCare.Models.VolunteerOnboarding;
using System.Reflection.Emit;

namespace SwiftCare.Data
{
    public class SwiftCareDbContext : IdentityDbContext<UserBaseModel>
    {
        public SwiftCareDbContext(DbContextOptions<SwiftCareDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VolunteerTaskAssignment>()
                .HasOne(a => a.VolunteerRoster)
                .WithMany(r => r.TaskAssignments)
                .HasForeignKey(a => a.VolunteerRosterId)
                .OnDelete(DeleteBehavior.SetNull);
            // ============================================================
            // TRANSPORT REQUEST -> NURSE
            // ============================================================

            modelBuilder.Entity<TransportRequest>()
        .HasOne(r => r.Nurse)
        .WithMany()
        .HasForeignKey(r => r.RequestedById)
        .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // TRANSPORT REQUEST -> RESIDENT
            // ============================================================

            modelBuilder.Entity<TransportRequest>()
                .HasOne(r => r.Resident)
                .WithMany()
                .HasForeignKey(r => r.RequestedForId)
                .OnDelete(DeleteBehavior.Restrict);

            //------------------------------------------------------------
            //VOLUNTEER ATTENDANCE -> VOLUNTEER
            //------------------------------------------------------------


            modelBuilder.Entity<VolunteerAttendance>()
                .HasIndex(a => a.VolunteerRosterShiftId)
                .IsUnique();


            // ============================================================
            // TRANSPORT REQUEST -> DRIVER
            // ============================================================

            modelBuilder.Entity<TransportRequest>()
                .HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);


            // ============================================================
            // TRANSPORT REQUEST -> VEHICLE
            // ============================================================

            modelBuilder.Entity<TransportRequest>()
                .HasOne(r => r.Vehicle)
                .WithMany()
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);


            // ============================================================
            // TRANSPORT REQUEST -> TRIP
            // ONE REQUEST = ONE TRIP
            // ============================================================

            modelBuilder.Entity<TransportRequest>()
                .HasOne(r => r.Trip)
                .WithOne(t => t.TransportRequest)
                .HasForeignKey<Trip>(t => t.RequestId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<PackageOrder>()
                .HasIndex(x => x.QrCodeValue)
                .IsUnique();

            modelBuilder.Entity<PackageOrder>()
                   .HasOne(p => p.Order)
                   .WithMany()
                   .HasForeignKey(p => p.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageOrder>()
                .HasOne(p => p.OrderItem)
                .WithOne(o => o.PackageOrder)
                .HasForeignKey<PackageOrder>(p => p.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<VolunteerApplication>()
    .HasIndex(a => new
    {
        a.ApplicantId,
        a.VolunteerOpportunityId
    })
    .IsUnique();

            modelBuilder.Entity<VolunteerApplicationEvaluation>()
                .HasOne(x => x.VolunteerApplication)
                .WithOne(x => x.Evaluation)
                .HasForeignKey<VolunteerApplicationEvaluation>(
                    x => x.VolunteerApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // PARSED CV -> CERTIFICATIONS
            // ============================================================

            modelBuilder.Entity<ParsedCv>()
                .HasMany(x => x.Certifications)
                .WithOne(x => x.ParsedCv)
                .HasForeignKey(x => x.ParsedCvId)
                .OnDelete(DeleteBehavior.Cascade);


            // ============================================================
            // PARSED CV -> EDUCATION
            // ============================================================

            modelBuilder.Entity<ParsedCv>()
                .HasMany(x => x.Education)
                .WithOne(x => x.ParsedCv)
                .HasForeignKey(x => x.ParsedCvId)
                .OnDelete(DeleteBehavior.Cascade);


            // ============================================================
            // PARSED CV -> EXPERIENCE
            // ============================================================

            modelBuilder.Entity<ParsedCv>()
                .HasMany(x => x.Experience)
                .WithOne(x => x.ParsedCv)
                .HasForeignKey(x => x.ParsedCvId)
                .OnDelete(DeleteBehavior.Cascade);


            // ============================================================
            // PRIMARY KEYS
            // ============================================================

            modelBuilder.Entity<CvCertification>()
                .HasKey(x => x.CvCertificationId);

            modelBuilder.Entity<CvEducation>()
                .HasKey(x => x.CvEducationId);

            modelBuilder.Entity<CvExperience>()
                .HasKey(x => x.CvExperienceId);

            modelBuilder.Entity<VolunteerVoiceInterviewSession>()
                .HasOne(x => x.VolunteerApplication)
                .WithOne()
                .HasForeignKey<VolunteerVoiceInterviewSession>(
                    x => x.VolunteerApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VolunteerVoiceInterviewSession>()
                .HasIndex(x => x.SessionToken)
                .IsUnique();


            modelBuilder.Entity<VolunteerInterviewEvaluation>()
                .HasIndex(x => x.VolunteerApplicationId)
                .IsUnique();

            modelBuilder.Entity<VolunteerInterviewEvaluation>()
                .HasOne(x => x.VolunteerApplication)
                .WithMany()
                .HasForeignKey(x => x.VolunteerApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VolunteerInterviewEvaluation>()
                .HasOne(x => x.VolunteerVoiceInterviewSession)
                .WithMany()
                .HasForeignKey(x => x.VolunteerVoiceInterviewSessionId)
                .OnDelete(DeleteBehavior.SetNull);


            modelBuilder.Entity<VolunteerInterviewSkillEvaluation>()
                .HasOne(x => x.VolunteerInterviewEvaluation)
                .WithMany(x => x.SkillEvaluations)
                .HasForeignKey(x => x.VolunteerInterviewEvaluationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VolunteerInterviewSkillEvaluation>()
                .HasOne(x => x.VolunteerRequiredSkill)
                .WithMany()
                .HasForeignKey(x => x.VolunteerRequiredSkillId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<VolunteerInterviewCompetencyEvaluation>()
                .HasOne(x => x.VolunteerInterviewEvaluation)
                .WithMany(x => x.CompetencyEvaluations)
                .HasForeignKey(x => x.VolunteerInterviewEvaluationId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<VolunteerOnboarding>(entity =>
            {
                entity.HasKey(x => x.VolunteerOnboardingId);

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.Property(x => x.CurrentStep)
                    .IsRequired();

                entity.Property(x => x.ApplicantId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.CreatedDate)
                    .IsRequired();

                entity.Property(x => x.ModifiedDate)
                    .IsRequired();

                entity.HasOne(x => x.VolunteerApplication)
                    .WithOne()
                    .HasForeignKey<VolunteerOnboarding>(
                        x => x.VolunteerApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Applicant)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicantId)
                    .OnDelete(DeleteBehavior.Restrict);


                entity.HasMany(x => x.Offers)
                    .WithOne(x => x.VolunteerOnboarding)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.PolicyAcceptances)
                    .WithOne(x => x.VolunteerOnboarding)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Documents)
                    .WithOne(x => x.VolunteerOnboarding)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Requirements)
                    .WithOne(x => x.VolunteerOnboarding)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Volunteer)
                    .WithOne(x => x.Onboarding)
                    .HasForeignKey<Volunteer>(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            modelBuilder.Entity<VolunteerOffer>(entity =>
            {
                entity.HasKey(x => x.VolunteerOfferId);

                entity.Property(x => x.OfferReference)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PositionTitle)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.ApplicantId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.AcceptanceTokenHash)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasIndex(x => x.OfferReference)
                    .IsUnique();

                entity.HasIndex(x => x.AcceptanceTokenHash)
                    .IsUnique();

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany(x => x.Offers)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.VolunteerApplication)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VolunteerOpportunity)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerOpportunityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<VolunteerPolicy>(entity =>
            {
                entity.HasKey(x => x.VolunteerPolicyId);

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Version)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.IsRequired)
                    .IsRequired();

                entity.Property(x => x.IsActive)
                    .IsRequired();

                entity.HasIndex(x => new
                {
                    x.Title,
                    x.Version
                });
            });



            modelBuilder.Entity<VolunteerPolicyAcceptance>(entity =>
            {
                entity.HasKey(x => x.VolunteerPolicyAcceptanceId);

                entity.Property(x => x.ApplicantId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.PolicyVersion)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.Accepted)
                    .IsRequired();

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany(x => x.PolicyAcceptances)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.VolunteerPolicy)
                    .WithMany(x => x.Acceptances)
                    .HasForeignKey(x => x.VolunteerPolicyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.VolunteerOnboardingId,
                    x.VolunteerPolicyId
                })
                .IsUnique();
            });


            modelBuilder.Entity<VolunteerOnboardingDocument>(entity =>
            {
                entity.HasKey(x => x.VolunteerOnboardingDocumentId);

                entity.Property(x => x.ApplicantId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.DocumentName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.FileName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.FilePath)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.ContentType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.Property(x => x.DocumentType)
                    .IsRequired();

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany(x => x.Documents)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new
                {
                    x.VolunteerOnboardingId,
                    x.DocumentType
                });
            });



            modelBuilder.Entity<VolunteerOnboardingRequirement>(entity =>
            {
                entity.HasKey(x => x.VolunteerOnboardingRequirementId);

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.RequirementType)
                    .IsRequired();

                entity.Property(x => x.IsRequired)
                    .IsRequired();

                entity.Property(x => x.IsCompleted)
                    .IsRequired();

                entity.Property(x => x.DisplayOrder)
                    .IsRequired();

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany(x => x.Requirements)
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new
                {
                    x.VolunteerOnboardingId,
                    x.DisplayOrder
                });
            });



            modelBuilder.Entity<Volunteer>(entity =>
            {
                entity.Property(x => x.VolunteerOnboardingId)
                    .IsRequired(false);

                entity.HasIndex(x => x.VolunteerOnboardingId)
                    .IsUnique()
                    .HasFilter("[VolunteerOnboardingId] IS NOT NULL");

                entity.HasOne(x => x.Onboarding)
                    .WithOne(x => x.Volunteer)
                    .HasForeignKey<Volunteer>(
                        x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // VOLUNTEER CONTRACT
            // ============================================================

            modelBuilder.Entity<VolunteerContract>(entity =>
            {
                entity.HasKey(x => x.VolunteerContractId);

                entity.HasIndex(x => x.ContractReference)
                    .IsUnique();

                entity.HasIndex(x => x.VolunteerOnboardingId);

                entity.HasIndex(x => x.VolunteerApplicationId);

                entity.HasIndex(x => x.VolunteerOpportunityId);

                entity.HasIndex(x => x.VolunteerOfferId);

                entity.HasIndex(x => x.ApplicantId);

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VolunteerApplication)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VolunteerOpportunity)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerOpportunityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.VolunteerOffer)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerOfferId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Applicant)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.ContractReference)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ContractVersion)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PositionTitle)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.RoleDescription)
                    .HasMaxLength(3000);

                entity.Property(x => x.CityName)
                    .HasMaxLength(150);

                entity.Property(x => x.Province)
                    .HasMaxLength(150);

                entity.Property(x => x.RequirementsSnapshot)
                    .HasMaxLength(10000);

                entity.Property(x => x.ResponsibilitiesSnapshot)
                    .HasMaxLength(10000);

                entity.Property(x => x.RequiredSkillsSnapshot)
                    .HasMaxLength(10000);

                entity.Property(x => x.GeneratedFileName)
                    .HasMaxLength(255);

                entity.Property(x => x.GeneratedFilePath)
                    .HasMaxLength(500);

                entity.Property(x => x.SignedFileName)
                    .HasMaxLength(255);

                entity.Property(x => x.SignedFilePath)
                    .HasMaxLength(500);

                entity.Property(x => x.RejectionReason)
                    .HasMaxLength(2000);

                entity.Property(x => x.CreatedBy)
                    .HasMaxLength(450);

                entity.Property(x => x.ModifiedBy)
                    .HasMaxLength(450);

                entity.Property(x => x.ReviewedByAdminId)
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<VolunteerIdentityVerification>(entity =>
            {
                entity.ToTable("VolunteerIdentityVerifications");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Provider)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ProviderVerificationId)
                    .HasMaxLength(200);

                entity.Property(x => x.ProviderPersonId)
                    .HasMaxLength(200);

                entity.Property(x => x.FailureReason)
                    .HasMaxLength(2000);

                entity.Property(x => x.ReviewNotes)
                    .HasMaxLength(2000);

                entity.Property(x => x.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.HasIndex(x => x.VolunteerOnboardingId);

                entity.HasIndex(x => x.VolunteerId);

                entity.HasIndex(x => x.IdentityDocumentId);

                entity.HasIndex(x => x.ProviderVerificationId)
                    .IsUnique()
                    .HasFilter("[ProviderVerificationId] IS NOT NULL");

                entity.HasIndex(x => x.ProviderPersonId)
                    .IsUnique()
                    .HasFilter("[ProviderPersonId] IS NOT NULL");

                entity.HasOne(x => x.VolunteerOnboarding)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerOnboardingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Volunteer)
                    .WithMany()
                    .HasForeignKey(x => x.VolunteerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.IdentityDocument)
                    .WithMany()
                    .HasForeignKey(x => x.IdentityDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.ProviderSessionId)
                    .HasMaxLength(200);

                entity.Property(x => x.VerificationUrl)
                    .HasMaxLength(2000);

                entity.HasIndex(x => x.ProviderSessionId)
                    .IsUnique()
                    .HasFilter("[ProviderSessionId] IS NOT NULL");
            });

            modelBuilder.Entity<DiditWebhookLog>(entity =>
            {
                entity.ToTable("DiditWebhookLogs");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.EventId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.WebhookType)
                    .HasMaxLength(100);

                entity.Property(x => x.SessionId)
                    .HasMaxLength(100);

                entity.Property(x => x.VendorData)
                    .HasMaxLength(100);

                entity.Property(x => x.Status)
                    .HasMaxLength(100);

                entity.Property(x => x.Environment)
                    .HasMaxLength(100);

                entity.Property(x => x.ProcessingError)
                    .HasMaxLength(4000);

                entity.Property(x => x.RawBody)
                    .IsRequired();

                entity.HasIndex(x => x.EventId)
                    .IsUnique();

                entity.HasIndex(x => x.SessionId);

                entity.HasIndex(x => x.WebhookType);

                entity.HasIndex(x => x.Processed);


            });

            // ============================================================
            // SUPERVISOR RELATIONSHIPS
            // ============================================================

            // Volunteer -> Supervisor
            modelBuilder.Entity<Volunteer>()
                .HasOne(v => v.Supervisor)
                .WithMany(s => s.Volunteers)
                .HasForeignKey(v => v.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // VolunteerRoster -> Supervisor
            modelBuilder.Entity<VolunteerRoster>()
                .HasOne(r => r.Supervisor)
                .WithMany(s => s.Rosters)
                .HasForeignKey(r => r.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VolunteerRoster>()
                .HasOne(r => r.Volunteer)
                .WithMany(v => v.Rosters)
                .HasForeignKey(r => r.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // VOLUNTEER TASK -> TASK ASSIGNMENTS
            // ============================================================

            modelBuilder.Entity<VolunteerTask>()
                .HasMany(t => t.Assignments)
                .WithOne(a => a.VolunteerTask)
                .HasForeignKey(a => a.VolunteerTaskId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // VOLUNTEER TASK ASSIGNMENT -> VOLUNTEER
            // ============================================================

            modelBuilder.Entity<VolunteerTaskAssignment>()
                .HasOne(a => a.Volunteer)
                .WithMany()
                .HasForeignKey(a => a.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // VOLUNTEER TASK ASSIGNMENT -> ROSTER
            // ============================================================

            modelBuilder.Entity<VolunteerTaskAssignment>()
                .HasOne(a => a.VolunteerRoster)
                .WithMany(r => r.TaskAssignments)
                .HasForeignKey(a => a.VolunteerRosterId)
                .OnDelete(DeleteBehavior.SetNull);


            // ============================================================
            // VOLUNTEER TASK ASSIGNMENT -> ASSIGNED BY
            // ============================================================

            modelBuilder.Entity<VolunteerTaskAssignment>()
                .HasOne(a => a.AssignedBy)
                .WithMany()
                .HasForeignKey(a => a.AssignedById)
                .OnDelete(DeleteBehavior.SetNull);

            // ============================================================
            // VOLUNTEER ROSTER
            // ============================================================

            modelBuilder.Entity<VolunteerRoster>(entity =>
            {
                entity.HasKey(r => r.VolunteerRosterId);

                entity.Property(r => r.RosterName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(r => r.Description)
                    .HasMaxLength(2000);

                entity.Property(r => r.Notes)
                    .HasMaxLength(2000);

                // --------------------------------------------------------
                // Volunteer
                // --------------------------------------------------------

                entity.HasOne(r => r.Volunteer)
                    .WithMany(v => v.Rosters)
                    .HasForeignKey(r => r.VolunteerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // --------------------------------------------------------
                // Supervisor
                // --------------------------------------------------------

                entity.HasOne(r => r.Supervisor)
                    .WithMany()
                    .HasForeignKey(r => r.SupervisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // --------------------------------------------------------
                // Roster -> Daily Shifts
                // --------------------------------------------------------

                entity.HasMany(r => r.Shifts)
                    .WithOne(s => s.VolunteerRoster)
                    .HasForeignKey(s => s.VolunteerRosterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ============================================================
            // VOLUNTEER ROSTER SHIFT
            // ============================================================

            modelBuilder.Entity<VolunteerRosterShift>(entity =>
            {
                entity.HasKey(s => s.VolunteerRosterShiftId);

                entity.Property(s => s.Notes)
                    .HasMaxLength(1000);

                // --------------------------------------------------------
                // Roster -> Shift
                // --------------------------------------------------------

                entity.HasOne(s => s.VolunteerRoster)
                    .WithMany(r => r.Shifts)
                    .HasForeignKey(s => s.VolunteerRosterId)
                    .OnDelete(DeleteBehavior.Cascade);

                // --------------------------------------------------------
                // One shift can have one attendance record
                // --------------------------------------------------------

                entity.HasOne(s => s.Attendance)
                    .WithOne(a => a.VolunteerRosterShift)
                    .HasForeignKey<VolunteerAttendance>(
                        a => a.VolunteerRosterShiftId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ============================================================
            // VOLUNTEER ATTENDANCE
            // ============================================================

            modelBuilder.Entity<VolunteerAttendance>(entity =>
            {
                entity.HasKey(a => a.AttendanceId);

                entity.Property(a => a.Status)
                    .IsRequired();

                entity.Property(a => a.VerificationProvider)
                    .HasMaxLength(50);

                entity.Property(a => a.VerificationReference)
                    .HasMaxLength(500);

                entity.Property(a => a.Notes)
                    .HasMaxLength(2000);

                entity.Property(a => a.CreatedBy)
                    .HasMaxLength(450);

                entity.Property(a => a.ModifiedBy)
                    .HasMaxLength(450);

                // --------------------------------------------------------
                // Volunteer -> Attendance
                // --------------------------------------------------------

                entity.HasOne(a => a.Volunteer)
                    .WithMany()
                    .HasForeignKey(a => a.VolunteerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // --------------------------------------------------------
                // Shift -> Attendance
                // --------------------------------------------------------

                entity.HasOne(a => a.VolunteerRosterShift)
                    .WithOne(s => s.Attendance)
                    .HasForeignKey<VolunteerAttendance>(
                        a => a.VolunteerRosterShiftId)
                    .OnDelete(DeleteBehavior.Cascade);

                // --------------------------------------------------------
                // Prevent duplicate attendance for the same shift
                // --------------------------------------------------------

                entity.HasIndex(a => a.VolunteerRosterShiftId)
                    .IsUnique();
            });

        }

        public DbSet<Supervisor> Supervisors { get; set; }
        public DbSet<VolunteerRoster> VolunteerRosters { get; set; }
        public DbSet<DiditWebhookLog> DiditWebhookLogs
        {
            get; set;
        }
        public DbSet<VolunteerIdentityVerification> VolunteerIdentityVerifications
        {
            get; set;
        }
        public DbSet<VolunteerOnboarding> VolunteerOnboardings
        {
            get;
            set;
        }

        public DbSet<SwiftCare.Models.VolunteerOnboarding.VolunteerOffer> VolunteerOffers
        {
            get;
            set;
        }


        public DbSet<VolunteerRosterShift> VolunteerRosterShifts { get; set; }

        public DbSet<VolunteerAttendance> VolunteerAttendances { get; set; }

        public DbSet<SwiftCare.Models.VolunteerOnboarding.VolunteerPolicy> VolunteerPolicies
        {
            get;
            set;
        }

        public DbSet<SwiftCare.Models.VolunteerOnboarding.VolunteerPolicyAcceptance> VolunteerPolicyAcceptances
        {
            get;
            set;
        }

        public DbSet<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboardingDocument> VolunteerOnboardingDocuments
        {
            get;
            set;
        }

        public DbSet<SwiftCare.Models.VolunteerOnboarding.VolunteerOnboardingRequirement> VolunteerOnboardingRequirements
        {
            get;
            set;
        }
        public DbSet<VolunteerInterviewEvaluation>VolunteerInterviewEvaluations{ get; set; }

        public DbSet<VolunteerPolicyItem> VolunteerPolicyItems { get; set; }

        public DbSet<VolunteerInterviewSkillEvaluation> VolunteerInterviewSkillEvaluations{ get; set; }

        public DbSet<VolunteerInterviewCompetencyEvaluation>VolunteerInterviewCompetencyEvaluations{ get; set; }
        public DbSet<VolunteerApplicationEvaluation>VolunteerApplicationEvaluations{ get; set; }

        public DbSet<VolunteerApplicationAudit>VolunteerApplicationAudits { get; set; }

        public DbSet<VolunteerVoiceInterviewSession> VolunteerVoiceInterviewSessions { get; set; }

        public DbSet<VolunteerOpportunityScoringConfiguration>VolunteerOpportunityScoringConfigurations{ get; set; }
        public DbSet<SwiftCare.Models.Applicant> Applicants { get; set; }

        public DbSet<SwiftCare.Models.VolunteerQuestion> VolunteerQuestions{ get; set; }

        public DbSet<SwiftCare.Models.VolunteerOpportunityQuestion>VolunteerOpportunityQuestions{ get; set; }

        public DbSet<SwiftCare.Models.VolunteerApplicationAnswer>VolunteerApplicationAnswers{ get; set; }
        public DbSet<SwiftCare.Models.DailyMaintenance> DailyMaintenance { get; set; }
        public DbSet<SwiftCare.Models.DailyCare> DailyCare { get; set; }
        public DbSet<SwiftCare.Models.PackageOrder> PackageOrders { get; set; }

        public DbSet<SwiftCare.Models.City> Cities { get; set; }

        public DbSet<SwiftCare.Models.VolunteerRequiredSkill> VolunteerRequiredSkills { get; set; }

        public DbSet<SwiftCare.Models.VolunteerRequirement> VolunteerRequirements { get; set; }

        public DbSet<SwiftCare.Models.Administrator> Administrators { get; set; } = default!;
        public DbSet<SwiftCare.Models.VolunteerOpportunity> VolunteerOpportunities { get; set; } = default!;

        public DbSet<SwiftCare.Models.Volunteer> Volunteers { get; set; }

        public DbSet<SwiftCare.Models.VolunteerResponsibility> VolunteerResponsibilities { get; set; }

        public DbSet<SwiftCare.Models.VolunteerApplication> VolunteerApplications { get; set; }

        public DbSet<SwiftCare.Models.VolunteerExperience> VolunteerExperiences { get; set; }

        public DbSet<SwiftCare.Models.VolunteerSkill> VolunteerSkills { get; set; }

        public DbSet<SwiftCare.Models.VolunteerAvailability> VolunteerAvailabilities { get; set; }

        public DbSet<SwiftCare.Models.VolunteerTask> VolunteerTasks { get; set; }

        public DbSet<SwiftCare.Models.VolunteerTaskAssignment> VolunteerTaskAssignments { get; set; }

        public DbSet<SwiftCare.Models.VolunteerDocument> VolunteerDocuments { get; set; }

        public DbSet<VolunteerAgreement> VolunteerAgreements { get; set; }

        public DbSet<VolunteerScreening> VolunteerScreenings { get; set; }

        public DbSet<SwiftCare.Models.TransportRequest> TransportRequests { get; set; } = default!;
        public DbSet<SwiftCare.Models.Doctor> Doctors { get; set; } = default!;
        public DbSet<SwiftCare.Models.Nurse> Nurses{ get; set; } = default!;

        public DbSet<SwiftCare.Models.Equipment> Equipment { get; set; } = default!;

        public DbSet<SwiftCare.Models.Cart> Carts { get; set; } = default!;

        public DbSet<SwiftCare.Models.Trip> Trips { get; set; } = default!;

        public DbSet<SwiftCare.Models.Vehicle> Vehicles { get; set; } = default!;

        public DbSet<SwiftCare.Models.Order> Orders { get; set; } = default!;

        public DbSet<SwiftCare.Models.CartItem> CartItems{ get; set; } = default!;

        public DbSet<SwiftCare.Models.OurEquipment> OurEquipment { get; set; } = default!;

        public DbSet<SwiftCare.Models.OrderItem> OrderItems { get; set; } = default!;

        public DbSet<SwiftCare.Models.MaintenanceRequest> MaintenanceRequests { get; set; } = default!;

        public DbSet<SwiftCare.Models.SupplierAdmin> SupplierAdmins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Driver> Drivers { get; set; } = default!;

        public DbSet<SwiftCare.Models.MaintenancePerson> MaintenancePersons { get; set; } = default!;

        public DbSet<SwiftCare.Models.Chef> Chefs { get; set; } = default!;

        public DbSet<SwiftCare.Models.NextOfKin>NextOfKins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Resident> Residents { get; set; } = default!;

        public DbSet<VolunteerContract> VolunteerContracts { get; set; }
    }
}
