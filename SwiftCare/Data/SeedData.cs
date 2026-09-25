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
                "Maintainance Person",
                "Applicant",
                "Supervisor",
                "Human Resources Manager"};

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
            var hrManager = await userManager.FindByEmailAsync("hr@gmail.com");
            if (hrManager == null)
            {
                var defaultUser = new Nurse
                {
                    FirstName = "SwiftCare",
                    LastName = "HR",
                    UserName = "hr@gmail.com",
                    Email = "hr@gmail.com",
                    PhoneNumber = "0678987890",
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
                    await userManager.AddToRoleAsync(defaultUser, "Human Resources Manager");
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


            // ============================================================
            // VOLUNTEER QUESTION BANK
            // 5 QUESTIONS PER CATEGORY
            // 5 QUESTIONS PER AREA
            // ============================================================

            var volunteerQuestions = new List<VolunteerQuestion>
{
    // ========================================================
    // GENERAL QUESTIONS
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to volunteer with SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = null,
        IsActive = true,
        DisplayOrder = 1
    },

    new VolunteerQuestion
    {
        Question = "Have you volunteered before? If yes, please describe your experience.",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = false,
        Category = null,
        Area = null,
        IsActive = true,
        DisplayOrder = 2
    },

    new VolunteerQuestion
    {
        Question = "What skills or strengths would you bring to this volunteer opportunity?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = null,
        IsActive = true,
        DisplayOrder = 3
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable working as part of a team?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = null,
        IsActive = true,
        DisplayOrder = 4
    },

    new VolunteerQuestion
    {
        Question = "Is there anything else you would like SwiftCare to know about you?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = false,
        Category = null,
        Area = null,
        IsActive = true,
        DisplayOrder = 5
    },


    // ========================================================
    // RESIDENT SUPPORT
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in supporting residents at SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.ResidentSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 10
    },

    new VolunteerQuestion
    {
        Question = "Have you previously interacted with or supported older adults?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.ResidentSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 11
    },

    new VolunteerQuestion
    {
        Question = "How would you respond if a resident became upset or frustrated?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.ResidentSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 12
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable communicating respectfully with residents who may have different needs?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.ResidentSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 13
    },

    new VolunteerQuestion
    {
        Question = "What does treating an elderly resident with dignity and respect mean to you?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.ResidentSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 14
    },


    // ========================================================
    // ACTIVITIES
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you enjoy helping residents participate in activities?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Activities,
        Area = null,
        IsActive = true,
        DisplayOrder = 20
    },

    new VolunteerQuestion
    {
        Question = "Have you ever organised or assisted with a group activity?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Activities,
        Area = null,
        IsActive = true,
        DisplayOrder = 21
    },

    new VolunteerQuestion
    {
        Question = "What type of activities would you enjoy organising for residents?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Activities,
        Area = null,
        IsActive = true,
        DisplayOrder = 22
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable encouraging residents to participate in activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Activities,
        Area = null,
        IsActive = true,
        DisplayOrder = 23
    },

    new VolunteerQuestion
    {
        Question = "How would you adapt an activity for a resident who has limited mobility?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Activities,
        Area = null,
        IsActive = true,
        DisplayOrder = 24
    },


    // ========================================================
    // COMPANIONSHIP
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in providing companionship to residents?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Companionship,
        Area = null,
        IsActive = true,
        DisplayOrder = 30
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable spending time talking and listening to residents?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Companionship,
        Area = null,
        IsActive = true,
        DisplayOrder = 31
    },

    new VolunteerQuestion
    {
        Question = "How would you make a resident feel comfortable when meeting them for the first time?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Companionship,
        Area = null,
        IsActive = true,
        DisplayOrder = 32
    },

    new VolunteerQuestion
    {
        Question = "Are you a good listener?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Companionship,
        Area = null,
        IsActive = true,
        DisplayOrder = 33
    },

    new VolunteerQuestion
    {
        Question = "What would you do if a resident wanted to talk about something personal?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Companionship,
        Area = null,
        IsActive = true,
        DisplayOrder = 34
    },


    // ========================================================
    // EVENTS
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to help with SwiftCare events?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Events,
        Area = null,
        IsActive = true,
        DisplayOrder = 40
    },

    new VolunteerQuestion
    {
        Question = "Have you previously helped organise an event?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Events,
        Area = null,
        IsActive = true,
        DisplayOrder = 41
    },

    new VolunteerQuestion
    {
        Question = "What type of event would you enjoy helping SwiftCare organise?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Events,
        Area = null,
        IsActive = true,
        DisplayOrder = 42
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable helping guests and residents during an event?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Events,
        Area = null,
        IsActive = true,
        DisplayOrder = 43
    },

    new VolunteerQuestion
    {
        Question = "How would you handle an unexpected problem during an event?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Events,
        Area = null,
        IsActive = true,
        DisplayOrder = 44
    },


    // ========================================================
    // ADMINISTRATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in administrative volunteering?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Administration,
        Area = null,
        IsActive = true,
        DisplayOrder = 50
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable working with computers and basic office software?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Administration,
        Area = null,
        IsActive = true,
        DisplayOrder = 51
    },

    new VolunteerQuestion
    {
        Question = "What administrative or office experience do you have?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = false,
        Category = VolunteerCategory.Administration,
        Area = null,
        IsActive = true,
        DisplayOrder = 52
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable handling information confidentially?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Administration,
        Area = null,
        IsActive = true,
        DisplayOrder = 53
    },

    new VolunteerQuestion
    {
        Question = "How do you make sure that administrative work is accurate?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Administration,
        Area = null,
        IsActive = true,
        DisplayOrder = 54
    },


    // ========================================================
    // RECREATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to help residents with recreational activities?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Recreation,
        Area = null,
        IsActive = true,
        DisplayOrder = 60
    },

    new VolunteerQuestion
    {
        Question = "Do you enjoy participating in games or recreational activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Recreation,
        Area = null,
        IsActive = true,
        DisplayOrder = 61
    },

    new VolunteerQuestion
    {
        Question = "What recreational activities would you suggest for residents?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Recreation,
        Area = null,
        IsActive = true,
        DisplayOrder = 62
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable encouraging residents to try new activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Recreation,
        Area = null,
        IsActive = true,
        DisplayOrder = 63
    },

    new VolunteerQuestion
    {
        Question = "How would you modify a recreational activity for residents with different abilities?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Recreation,
        Area = null,
        IsActive = true,
        DisplayOrder = 64
    },


    // ========================================================
    // FOOD SERVICE
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in volunteering in food service?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.FoodService,
        Area = null,
        IsActive = true,
        DisplayOrder = 70
    },

    new VolunteerQuestion
    {
        Question = "Do you have previous experience preparing or serving food?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.FoodService,
        Area = null,
        IsActive = true,
        DisplayOrder = 71
    },

    new VolunteerQuestion
    {
        Question = "Why is cleanliness important when handling food?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.FoodService,
        Area = null,
        IsActive = true,
        DisplayOrder = 72
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable following food preparation and hygiene instructions?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.FoodService,
        Area = null,
        IsActive = true,
        DisplayOrder = 73
    },

    new VolunteerQuestion
    {
        Question = "How would you handle a situation where you were unsure whether a food item was safe to serve?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.FoodService,
        Area = null,
        IsActive = true,
        DisplayOrder = 74
    },


    // ========================================================
    // GARDENING
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in gardening at SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Gardening,
        Area = null,
        IsActive = true,
        DisplayOrder = 80
    },

    new VolunteerQuestion
    {
        Question = "Do you have previous gardening experience?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Gardening,
        Area = null,
        IsActive = true,
        DisplayOrder = 81
    },

    new VolunteerQuestion
    {
        Question = "What gardening activities are you comfortable performing?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Gardening,
        Area = null,
        IsActive = true,
        DisplayOrder = 82
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable working outdoors for extended periods?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Gardening,
        Area = null,
        IsActive = true,
        DisplayOrder = 83
    },

    new VolunteerQuestion
    {
        Question = "How could a garden improve the experience of residents at SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Gardening,
        Area = null,
        IsActive = true,
        DisplayOrder = 84
    },


    // ========================================================
    // MAINTENANCE
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in helping with maintenance tasks?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Maintenance,
        Area = null,
        IsActive = true,
        DisplayOrder = 90
    },

    new VolunteerQuestion
    {
        Question = "Do you have experience with basic maintenance or repairs?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Maintenance,
        Area = null,
        IsActive = true,
        DisplayOrder = 91
    },

    new VolunteerQuestion
    {
        Question = "What maintenance skills do you have?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = false,
        Category = VolunteerCategory.Maintenance,
        Area = null,
        IsActive = true,
        DisplayOrder = 92
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable following safety instructions when performing maintenance tasks?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Maintenance,
        Area = null,
        IsActive = true,
        DisplayOrder = 93
    },

    new VolunteerQuestion
    {
        Question = "What would you do if you discovered a maintenance problem that you could not safely repair?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Maintenance,
        Area = null,
        IsActive = true,
        DisplayOrder = 94
    },


    // ========================================================
    // FUNDRAISING
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to help with fundraising for SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Fundraising,
        Area = null,
        IsActive = true,
        DisplayOrder = 100
    },

    new VolunteerQuestion
    {
        Question = "Have you previously participated in fundraising activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Fundraising,
        Area = null,
        IsActive = true,
        DisplayOrder = 101
    },

    new VolunteerQuestion
    {
        Question = "What fundraising ideas could you suggest for SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Fundraising,
        Area = null,
        IsActive = true,
        DisplayOrder = 102
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable speaking with members of the public about fundraising?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Fundraising,
        Area = null,
        IsActive = true,
        DisplayOrder = 103
    },

    new VolunteerQuestion
    {
        Question = "How would you encourage someone to support a SwiftCare fundraising campaign?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Fundraising,
        Area = null,
        IsActive = true,
        DisplayOrder = 104
    },


    // ========================================================
    // TRANSPORTATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in volunteering in transportation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Transportation,
        Area = null,
        IsActive = true,
        DisplayOrder = 110
    },

    new VolunteerQuestion
    {
        Question = "Do you have previous experience assisting people with transportation?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = VolunteerCategory.Transportation,
        Area = null,
        IsActive = true,
        DisplayOrder = 111
    },

    new VolunteerQuestion
    {
        Question = "How would you help ensure that a resident feels comfortable during transportation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Transportation,
        Area = null,
        IsActive = true,
        DisplayOrder = 112
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable following transportation safety procedures?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Transportation,
        Area = null,
        IsActive = true,
        DisplayOrder = 113
    },

    new VolunteerQuestion
    {
        Question = "What would you do if a resident became uncomfortable or distressed during transportation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Transportation,
        Area = null,
        IsActive = true,
        DisplayOrder = 114
    },


    // ========================================================
    // SOCIAL SUPPORT
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in providing social support to residents?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.SocialSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 120
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable communicating with residents from different backgrounds?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.SocialSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 121
    },

    new VolunteerQuestion
    {
        Question = "How would you help a resident who appears lonely or socially isolated?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.SocialSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 122
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable listening to residents and offering friendly conversation?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.SocialSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 123
    },

    new VolunteerQuestion
    {
        Question = "What qualities do you think are important when providing social support?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.SocialSupport,
        Area = null,
        IsActive = true,
        DisplayOrder = 124
    },


    // ========================================================
    // OTHER
    // ========================================================

    new VolunteerQuestion
    {
        Question = "What interests you about this volunteer opportunity?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Other,
        Area = null,
        IsActive = true,
        DisplayOrder = 130
    },

    new VolunteerQuestion
    {
        Question = "Do you have any skills or experience that may be useful in this role?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Other,
        Area = null,
        IsActive = true,
        DisplayOrder = 131
    },

    new VolunteerQuestion
    {
        Question = "Please describe any relevant skills, interests or experience you have.",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = false,
        Category = VolunteerCategory.Other,
        Area = null,
        IsActive = true,
        DisplayOrder = 132
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable learning new tasks as part of this volunteer role?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = VolunteerCategory.Other,
        Area = null,
        IsActive = true,
        DisplayOrder = 133
    },

    new VolunteerQuestion
    {
        Question = "How would you approach a task that you have never performed before?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = VolunteerCategory.Other,
        Area = null,
        IsActive = true,
        DisplayOrder = 134
    },


    // ========================================================
    // AREA: RESIDENT CARE
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to volunteer in Resident Care?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ResidentCare,
        IsActive = true,
        DisplayOrder = 200
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable spending time assisting residents with approved activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ResidentCare,
        IsActive = true,
        DisplayOrder = 201
    },

    new VolunteerQuestion
    {
        Question = "What does compassionate care mean to you?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ResidentCare,
        IsActive = true,
        DisplayOrder = 202
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable asking staff for assistance when you are unsure about a resident-related task?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ResidentCare,
        IsActive = true,
        DisplayOrder = 203
    },

    new VolunteerQuestion
    {
        Question = "How would you protect a resident's privacy and dignity while volunteering?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ResidentCare,
        IsActive = true,
        DisplayOrder = 204
    },


    // ========================================================
    // AREA: ACTIVITIES & RECREATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "What activities would you suggest for residents in Activities & Recreation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ActivitiesAndRecreation,
        IsActive = true,
        DisplayOrder = 210
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable leading or assisting with group activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ActivitiesAndRecreation,
        IsActive = true,
        DisplayOrder = 211
    },

    new VolunteerQuestion
    {
        Question = "How would you encourage a resident who does not want to participate in an activity?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ActivitiesAndRecreation,
        IsActive = true,
        DisplayOrder = 212
    },

    new VolunteerQuestion
    {
        Question = "Do you enjoy planning creative or recreational activities?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = null,
        Area = VolunteerArea.ActivitiesAndRecreation,
        IsActive = true,
        DisplayOrder = 213
    },

    new VolunteerQuestion
    {
        Question = "How would you make an activity enjoyable for residents with different abilities?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.ActivitiesAndRecreation,
        IsActive = true,
        DisplayOrder = 214
    },


    // ========================================================
    // AREA: DINING SERVICES
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in volunteering in Dining Services?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.DiningServices,
        IsActive = true,
        DisplayOrder = 220
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable assisting with dining-area preparation and cleaning?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.DiningServices,
        IsActive = true,
        DisplayOrder = 221
    },

    new VolunteerQuestion
    {
        Question = "Why is hygiene especially important in a nursing-home dining environment?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.DiningServices,
        IsActive = true,
        DisplayOrder = 222
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable following instructions regarding resident meals?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.DiningServices,
        IsActive = true,
        DisplayOrder = 223
    },

    new VolunteerQuestion
    {
        Question = "What would you do if you noticed that a resident had received the wrong meal?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.DiningServices,
        IsActive = true,
        DisplayOrder = 224
    },


    // ========================================================
    // AREA: ADMINISTRATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "What attracts you to administrative work at SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Administration,
        IsActive = true,
        DisplayOrder = 230
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable using a computer to complete administrative tasks?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Administration,
        IsActive = true,
        DisplayOrder = 231
    },

    new VolunteerQuestion
    {
        Question = "How do you organise multiple administrative tasks?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Administration,
        IsActive = true,
        DisplayOrder = 232
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable keeping confidential information private?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Administration,
        IsActive = true,
        DisplayOrder = 233
    },

    new VolunteerQuestion
    {
        Question = "What would you do if you noticed an error in an administrative record?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Administration,
        IsActive = true,
        DisplayOrder = 234
    },


    // ========================================================
    // AREA: GARDEN & OUTDOOR
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to volunteer in Garden & Outdoor activities?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.GardenAndOutdoor,
        IsActive = true,
        DisplayOrder = 240
    },

    new VolunteerQuestion
    {
        Question = "Do you have experience with gardening or outdoor work?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = null,
        Area = VolunteerArea.GardenAndOutdoor,
        IsActive = true,
        DisplayOrder = 241
    },

    new VolunteerQuestion
    {
        Question = "What outdoor activities would you enjoy helping with?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.GardenAndOutdoor,
        IsActive = true,
        DisplayOrder = 242
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable working outdoors?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.GardenAndOutdoor,
        IsActive = true,
        DisplayOrder = 243
    },

    new VolunteerQuestion
    {
        Question = "How could outdoor spaces be used to improve residents' quality of life?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.GardenAndOutdoor,
        IsActive = true,
        DisplayOrder = 244
    },


    // ========================================================
    // AREA: EVENTS
    // ========================================================

    new VolunteerQuestion
    {
        Question = "What interests you about helping with SwiftCare events?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Events,
        IsActive = true,
        DisplayOrder = 250
    },

    new VolunteerQuestion
    {
        Question = "Have you helped organise an event before?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = null,
        Area = VolunteerArea.Events,
        IsActive = true,
        DisplayOrder = 251
    },

    new VolunteerQuestion
    {
        Question = "What role would you enjoy performing during an event?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Events,
        IsActive = true,
        DisplayOrder = 252
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable interacting with guests during events?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Events,
        IsActive = true,
        DisplayOrder = 253
    },

    new VolunteerQuestion
    {
        Question = "How would you handle an unexpected issue during an event?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Events,
        IsActive = true,
        DisplayOrder = 254
    },


    // ========================================================
    // AREA: TRANSPORTATION
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why are you interested in helping with Transportation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Transportation,
        IsActive = true,
        DisplayOrder = 260
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable following transportation safety procedures?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Transportation,
        IsActive = true,
        DisplayOrder = 261
    },

    new VolunteerQuestion
    {
        Question = "How would you help a resident feel safe and comfortable during transportation?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Transportation,
        IsActive = true,
        DisplayOrder = 262
    },

    new VolunteerQuestion
    {
        Question = "Do you have experience assisting people with transportation?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = null,
        Area = VolunteerArea.Transportation,
        IsActive = true,
        DisplayOrder = 263
    },

    new VolunteerQuestion
    {
        Question = "What would you do if a transportation situation became unsafe?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.Transportation,
        IsActive = true,
        DisplayOrder = 264
    },


    // ========================================================
    // AREA: COMMUNITY OUTREACH
    // ========================================================

    new VolunteerQuestion
    {
        Question = "Why would you like to participate in Community Outreach?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.CommunityOutreach,
        IsActive = true,
        DisplayOrder = 270
    },

    new VolunteerQuestion
    {
        Question = "Are you comfortable interacting with members of the community?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.CommunityOutreach,
        IsActive = true,
        DisplayOrder = 271
    },

    new VolunteerQuestion
    {
        Question = "What community activities would you suggest for SwiftCare?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.CommunityOutreach,
        IsActive = true,
        DisplayOrder = 272
    },

    new VolunteerQuestion
    {
        Question = "Have you participated in a community project or outreach programme before?",
        QuestionType = VolunteerQuestionType.YesNo,
        IsRequired = false,
        Category = null,
        Area = VolunteerArea.CommunityOutreach,
        IsActive = true,
        DisplayOrder = 273
    },

    new VolunteerQuestion
    {
        Question = "How would you represent SwiftCare professionally when interacting with the community?",
        QuestionType = VolunteerQuestionType.LongAnswer,
        IsRequired = true,
        Category = null,
        Area = VolunteerArea.CommunityOutreach,
        IsActive = true,
        DisplayOrder = 274
    }
};


            foreach (var question in volunteerQuestions)
            {
                var exists = await _context.VolunteerQuestions
                    .AnyAsync(q =>
                        q.Question == question.Question &&
                        q.Category == question.Category &&
                        q.Area == question.Area);

                if (!exists)
                {
                    _context.VolunteerQuestions.Add(question);
                }
            }


            await _context.SaveChangesAsync();
        }
    }
}
