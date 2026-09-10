using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Models;
using SwiftCare.Models.VolunteerCV;
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

        }

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

        public DbSet<SwiftCare.Models.VolunteerOnboarding> VolunteerOnboardings { get; set; }

        public DbSet<SwiftCare.Models.VolunteerDocument> VolunteerDocuments { get; set; }

        public DbSet<VolunteerAgreement> VolunteerAgreements { get; set; }

        public DbSet<VolunteerScreening> VolunteerScreenings { get; set; }

        public DbSet<VolunteerOrientation> VolunteerOrientations { get; set; }
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
    }
}
