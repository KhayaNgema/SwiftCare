using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using SwiftCare.Models;
using SwiftCare.Data;
using Microsoft.EntityFrameworkCore;
using Polly;

public static class SeedData
{
    public static async Task CreateRolesAndDefaultUser(this IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserBaseModel>>();
            var _context = scope.ServiceProvider.GetRequiredService<SwiftCareDbContext>();

            string[] roleNames = { "Administrator",
                "Doctor", 
                "Head Doctor", 
                "Nurse", 
                "Head Nurse", 
                "Chef", 
                "Resident",
                "Cleaner",
                "Next Of Kin",
                "Supplier Admin",
                "Driver",
                "Maintainance Person"};

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var admin = await userManager.FindByEmailAsync("admin@swiftcare.com");
            if (admin == null)
            {
                var defaultUser = new Administrator
                {
                    FirstName = "SwiftCare",
                    LastName = "Admin",
                    UserName = "admin@swiftcare.com",
                    Email = "admin@swiftcare.com",
                    PhoneNumber = "0678987834",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "admin.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Administrator");
                }
            }

            var nurse = await userManager.FindByEmailAsync("nurse@gmail.com");
            if (nurse == null)
            {
                var defaultUser = new Nurse
                {
                    FirstName = "SwiftCare",
                    LastName = "Nurse",
                    UserName = "nurse@gmail.com",
                    Email = "nurse@gmail.com",
                    PhoneNumber = "0678987877",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Nurse@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Nurse");
                }
            }

            var resident1 = await userManager.FindByEmailAsync("khaya@gmail.com");
            if (resident1 == null)
            {
                var defaultUser = new Resident
                {
                    FirstName = "Khaya",
                    LastName = "Ngema",
                    UserName = "khaya@gmail.com",
                    Email = "khaya@gmail.com",
                    PhoneNumber = "0765623458",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id",
                    RoomNumber = Room.Room08A
                };

                var result = await userManager.CreateAsync(defaultUser, "Resident@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Resident");
                }
            }


            var resident2 = await userManager.FindByEmailAsync("ngema@gmail.com");
            if (resident2 == null)
            {
                var defaultUser = new Resident
                {
                    FirstName = "Xoliswa",
                    LastName = "Ngema",
                    UserName = "ngema@gmail.com",
                    Email = "ngema@gmail.com",
                    PhoneNumber = "07656234580",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id",
                    RoomNumber = Room.Room08A
                };

                var result = await userManager.CreateAsync(defaultUser, "Resident@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Resident");
                }
            }


            var supplierAdmin = await userManager.FindByEmailAsync("sadmin@gmail.com");
            if (supplierAdmin == null)
            {
                var defaultUser = new SupplierAdmin
                {
                    FirstName = "Supplier",
                    LastName = "Admin",
                    UserName = "sadmin@gmail.com",
                    Email = "sadmin@gmail.com",
                    PhoneNumber = "0645676567",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Supplier@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Supplier Admin");
                }
            }


            var driver = await userManager.FindByEmailAsync("driver@gmail.com");
            if (driver == null)
            {
                var defaultUser = new SupplierAdmin
                {
                    FirstName = "Supplier",
                    LastName = "Driver",
                    UserName = "driver@gmail.com",
                    Email = "driver@gmail.com",
                    PhoneNumber = "0645612345",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Driver@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Driver");
                }
            }

            var maintPerson = await userManager.FindByEmailAsync("maintenance@gmail.com");
            if (maintPerson == null)
            {
                var defaultUser = new SupplierAdmin
                {
                    FirstName = "Maintenance",
                    LastName = "Person",
                    UserName = "maintenance@gmail.com",
                    Email = "maintenance@gmail.com",
                    PhoneNumber = "0789899098",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Maintenance@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Maintainance Person");
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
