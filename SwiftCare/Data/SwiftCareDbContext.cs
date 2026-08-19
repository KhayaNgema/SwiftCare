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
        }

        public DbSet<SwiftCare.Models.Administrator> Administrators { get; set; } = default!;
        public DbSet<SwiftCare.Models.Doctor>Doctors { get; set; } = default!;

        public DbSet<SwiftCare.Models.Nurse> Nurses{ get; set; } = default!;

        public DbSet<SwiftCare.Models.SupplierAdmin> SupplierAdmins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Driver> Drivers { get; set; } = default!;

        public DbSet<SwiftCare.Models.MaintenancePerson> MaintenancePersons { get; set; } = default!;

        public DbSet<SwiftCare.Models.Chef> Chefs { get; set; } = default!;

        public DbSet<SwiftCare.Models.NextOfKin>NextOfKins { get; set; } = default!;

        public DbSet<SwiftCare.Models.Resident> Residents { get; set; } = default!;
    }
}
