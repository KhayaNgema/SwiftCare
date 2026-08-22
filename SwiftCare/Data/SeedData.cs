using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using SwiftCare.Data;
using SwiftCare.Models;
using System;
using System.Threading.Tasks;
using static Twilio.Rest.Intelligence.V3.ConfigurationResource;

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

            var admin = await userManager.FindByEmailAsync("lwandiled181@gmail.com");

            if (admin == null)
            {
                var defaultUser = new Administrator
                {
                    FirstName = "Lwandile",
                    LastName = "Dlamini",
                    UserName = "lwandiled181@gmail.com",
                    Email = "lwandiled181@gmail.com",
                    PhoneNumber = "0845802946",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "admin.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Administrator");

                    // ============================================================
                    // CREATE ADMIN CART
                    // ============================================================

                    var adminCart = new Cart
                    {
                        UserId = defaultUser.Id,
                        CreatedAt = DateTime.Now
                    };

                    _context.Carts.Add(adminCart);

                    await _context.SaveChangesAsync();
                }
            }


            else
            {
                // ============================================================
                // ADMIN ALREADY EXISTS
                // MAKE SURE THE ADMIN HAS A CART
                // ============================================================

                var existingCart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == admin.Id);

                if (existingCart == null)
                {
                    var adminCart = new Cart
                    {
                        UserId = admin.Id,
                        CreatedAt = DateTime.Now
                    };

                    _context.Carts.Add(adminCart);

                    await _context.SaveChangesAsync();
                }
            }

            var admin2 = await userManager.FindByEmailAsync("yandymbatha05@gmail.com");

            if (admin2 == null)
            {
                var defaultUser = new Administrator
                {
                    FirstName = "Lwandile",
                    LastName = "Dlamini",
                    UserName = "yandymbatha05@gmail.com",
                    Email = "yandymbatha05@gmail.com",
                    PhoneNumber = "0845802990",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "admin.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Administrator");

                    // ============================================================
                    // CREATE ADMIN CART
                    // ============================================================

                    var adminCart = new Cart
                    {
                        UserId = defaultUser.Id,
                        CreatedAt = DateTime.Now
                    };

                    _context.Carts.Add(adminCart);

                    await _context.SaveChangesAsync();
                }
            }


            else
            {
                // ============================================================
                // ADMIN ALREADY EXISTS
                // MAKE SURE THE ADMIN HAS A CART
                // ============================================================

                var existingCart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == admin.Id);

                if (existingCart == null)
                {
                    var adminCart = new Cart
                    {
                        UserId = admin.Id,
                        CreatedAt = DateTime.Now
                    };

                    _context.Carts.Add(adminCart);

                    await _context.SaveChangesAsync();
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

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Nurse");
                }
            }
            // ============================================================
            // SEED DEFAULT VEHICLES
            // ============================================================

            var vehicles = new List<Vehicle>
{
    new Vehicle
    {
        VehicleName = "SwiftCare Transport 01",
        RegNumber = "SC 001 GP",
        Model = "Toyota Quantum",
        Year = "2023",
        Status = VehicleStatus.Available
    },

    new Vehicle
    {
        VehicleName = "SwiftCare Transport 02",
        RegNumber = "SC 002 GP",
        Model = "Toyota Hiace",
        Year = "2022",
        Status = VehicleStatus.Available
    },

    new Vehicle
    {
        VehicleName = "SwiftCare Medical Van 01",
        RegNumber = "SC 003 GP",
        Model = "Mercedes-Benz Sprinter",
        Year = "2024",
        Status = VehicleStatus.Available
    },

    new Vehicle
    {
        VehicleName = "SwiftCare Transport 03",
        RegNumber = "SC 004 GP",
        Model = "Ford Transit",
        Year = "2021",
        Status = VehicleStatus.Unavailable
    },

    new Vehicle
    {
        VehicleName = "SwiftCare Emergency Transport",
        RegNumber = "SC 005 GP",
        Model = "Volkswagen Crafter",
        Year = "2023",
        Status = VehicleStatus.Available
    }
};


            // ============================================================
            // ADD VEHICLES ONLY IF THEY DO NOT ALREADY EXIST
            // ============================================================

            foreach (var vehicle in vehicles)
            {
                var vehicleExists =
                    await _context.Vehicles
                        .AnyAsync(v =>
                            v.RegNumber == vehicle.RegNumber);

                if (!vehicleExists)
                {
                    await _context.Vehicles.AddAsync(vehicle);
                }
            }


            // ============================================================
            // SAVE VEHICLES
            // ============================================================

            await _context.SaveChangesAsync();
            var resident1 = await userManager.FindByEmailAsync("lunga@gmail.com");
            if (resident1 == null)
            {
                var defaultUser = new Resident
                {
                    FirstName = "Lunga",
                    LastName = "Buthelezi",
                    UserName = "lunga@gmail.com",
                    Email = "lunga@gmail.com",
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

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

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


            var supplierAdmin = await userManager.FindByEmailAsync("subadmin@gmail.com");
            if (supplierAdmin == null)
            {
                var defaultUser = new SupplierAdmin
                {
                    FirstName = "Supplier",
                    LastName = "Admin",
                    UserName = "subadmin@gmail.com",
                    Email = "subadmin@gmail.com",
                    PhoneNumber = "0645676567",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Supplier Admin");
                }
            }


            var driver = await userManager.FindByEmailAsync("sdriver@gmail.com");
            if (driver == null)
            {
                var defaultUser = new Driver
                {
                    FirstName = "Swift",
                    LastName = "Driver",
                    UserName = "sdriver@gmail.com",
                    Email = "sdriver@gmail.com",
                    PhoneNumber = "0645612390",
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

            var driver2 = await userManager.FindByEmailAsync("driver2@gmail.com");
            if (driver2 == null)
            {
                var defaultUser = new SupplierAdmin
                {
                    FirstName = "SwiftCare",
                    LastName = "Driver",
                    UserName = "driver2@gmail.com",
                    Email = "driver2@gmail.com",
                    PhoneNumber = "0695612395",
                    DateOfBirth = DateTime.Now,
                    ProfilePicture = "Images/default_profile_image.jpg",
                    EmailConfirmed = true,
                    CreatedDateTime = DateTime.Now,
                    ModifiedDateTime = DateTime.Now,
                    CreatedBy = "default-user-id",
                    ModifiedBy = "default-user-id"
                };

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

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

                var result = await userManager.CreateAsync(defaultUser, "Swift@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Maintainance Person");
                }
            }

            // ============================================================
            // SWIFTCARE NURSING HOME EQUIPMENT
            // ============================================================

            var equipmentList = new List<Equipment>
{
    new Equipment
    {
        EquipmentName = "Wheelchair",
        ImageUrl = "Images/equipment/wheelchair.jpg",
        Description = "A comfortable mobility chair designed to assist residents who have difficulty walking or standing for extended periods."
    },

    new Equipment
    {
        EquipmentName = "Hospital Bed",
        ImageUrl = "Images/equipment/hospital_bed.jpg",
        Description = "An adjustable nursing bed designed to provide comfort, support and easier care for residents requiring extended bed rest."
    },

    new Equipment
    {
        EquipmentName = "Walking Stick",
        ImageUrl = "Images/equipment/walking_stick.jpg",
        Description = "A lightweight mobility aid that provides additional balance and support for residents while walking."
    },

    new Equipment
    {
        EquipmentName = "Walking Frame",
        ImageUrl = "Images/equipment/walking_frame.png",
        Description = "A sturdy walking aid designed to provide increased stability and support for residents with limited mobility."
    },

    new Equipment
    {
        EquipmentName = "Bedside Commode",
        ImageUrl = "Images/equipment/bedside_commode.jpg",
        Description = "A portable toileting aid designed for residents who have difficulty reaching the bathroom independently."
    },

    new Equipment
    {
        EquipmentName = "Patient Transfer Chair",
        ImageUrl = "Images/equipment/patient_transfer_chair.jpg",
        Description = "A supportive transfer chair designed to assist caregivers when safely moving residents between beds, chairs and other areas."
    },

    new Equipment
    {
        EquipmentName = "Shower Chair",
        ImageUrl = "Images/equipment/shower_chair.jpg",
        Description = "A stable and comfortable bathing chair designed to provide seated support for residents while showering."
    },

    new Equipment
    {
        EquipmentName = "Overbed Table",
        ImageUrl = "Images/equipment/overbed_table.jpg",
        Description = "An adjustable table designed to provide residents with a convenient surface for meals, reading and everyday activities while in bed."
    },

    new Equipment
    {
        EquipmentName = "Pressure Relief Mattress",
        ImageUrl = "Images/equipment/pressure_relief_matress.jpg",
        Description = "A supportive mattress designed to improve comfort and help reduce prolonged pressure on areas of the body while residents are resting."
    },

    new Equipment
    {
        EquipmentName = "Patient Lifting Hoist",
        ImageUrl = "Images/equipment/patient_lifting_hoist.jpg",
        Description = "A mechanical lifting aid designed to assist trained caregivers with safely transferring residents who have significant mobility limitations."
    }
};

            // ============================================================
            // ADD EQUIPMENT IF IT DOES NOT ALREADY EXIST
            // ============================================================

            foreach (var equipment in equipmentList)
            {
                var existingEquipment = await _context.Equipment
                    .FirstOrDefaultAsync(e =>
                        e.EquipmentName == equipment.EquipmentName);

                if (existingEquipment == null)
                {
                    _context.Equipment.Add(equipment);
                }
            }


            await _context.SaveChangesAsync();
        }
    }
}
