using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Models;
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

        }
        
        public DbSet<SwiftCare.Models.Administrator> Administrators { get; set; } = default!;
        public DbSet<SwiftCare.Models.TransportRequest> TransportRequests { get; set; } = default!;
        public DbSet<SwiftCare.Models.Doctor> Doctors { get; set; } = default!;
        public DbSet<SwiftCare.Models.Nurse> Nurses{ get; set; } = default!;

        public DbSet<SwiftCare.Models.Equipment> Equipment { get; set; } = default!;

        public DbSet<SwiftCare.Models.Cart> Carts { get; set; } = default!;

        public DbSet<SwiftCare.Models.Trip> Trips { get; set; } = default!;

        public DbSet<SwiftCare.Models.Vehicle> Vehicles { get; set; } = default!;

        public DbSet<SwiftCare.Models.Order> Orders { get; set; } = default!;

        public DbSet<SwiftCare.Models.CartItem> CartItems{ get; set; } = default!;

        public DbSet<SwiftCare.Models.OrderItem> OrderItems { get; set; } = default!;

        public DbSet<SwiftCare.Models.SupplierAdmin> SupplierAdmins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Driver> Drivers { get; set; } = default!;

        public DbSet<SwiftCare.Models.MaintenancePerson> MaintenancePersons { get; set; } = default!;

        public DbSet<SwiftCare.Models.Chef> Chefs { get; set; } = default!;

        public DbSet<SwiftCare.Models.NextOfKin>NextOfKins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Resident> Residents { get; set; } = default!;
    }
}
