using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Models;

namespace SwiftCare.Data
{
    public static class SupervisorSeedData
    {
        // ============================================================
        // DEFAULT SUPERVISOR PASSWORD
        // ============================================================

        private const string SupervisorPassword = "Swift@123";


        // ============================================================
        // SEED SUPERVISORS
        // ============================================================

        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            using var scope = serviceProvider.CreateScope();

            var services = scope.ServiceProvider;

            var context =
                services.GetRequiredService<SwiftCareDbContext>();

            var userManager =
                services.GetRequiredService<UserManager<UserBaseModel>>();

            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            // --------------------------------------------------------
            // Make sure Supervisor role exists
            // --------------------------------------------------------

            await SeedSupervisorRoleAsync(
                roleManager,
                cancellationToken);

            // --------------------------------------------------------
            // Seed supervisors
            // --------------------------------------------------------

            await SeedSupervisorsAsync(
                context,
                userManager,
                cancellationToken);
        }


        // ============================================================
        // SUPERVISOR ROLE
        // ============================================================

        private static async Task SeedSupervisorRoleAsync(
            RoleManager<IdentityRole> roleManager,
            CancellationToken cancellationToken)
        {
            const string roleName = "Supervisor";

            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var role = new IdentityRole(roleName);

            var result =
                await roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {errors}");
            }
        }


        // ============================================================
        // SUPERVISORS
        // ============================================================

        private static async Task SeedSupervisorsAsync(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            CancellationToken cancellationToken)
        {
            var supervisors = new[]
            {
                new SupervisorSeedModel
                {
                    FirstName = "Thandi",
                    LastName = "Mokoena",
                    Email = "sup.residentcare@swiftcare.com",
                    JobTitle = "Resident Care Supervisor",
                    Bio = "Responsible for supervising volunteers supporting resident care activities.",
                    Area = VolunteerArea.ResidentCare
                },

                new SupervisorSeedModel
                {
                    FirstName = "Sipho",
                    LastName = "Dlamini",
                    Email = "sup.activities@swiftcare.com",
                    JobTitle = "Activities & Recreation Supervisor",
                    Bio = "Responsible for supervising volunteers supporting activities, recreation and resident engagement.",
                    Area = VolunteerArea.ActivitiesAndRecreation
                },

                new SupervisorSeedModel
                {
                    FirstName = "Nomsa",
                    LastName = "Khumalo",
                    Email = "sup.dining@swiftcare.com",
                    JobTitle = "Dining Services Supervisor",
                    Bio = "Responsible for supervising volunteers supporting dining and food service activities.",
                    Area = VolunteerArea.DiningServices
                },

                new SupervisorSeedModel
                {
                    FirstName = "Lerato",
                    LastName = "Mthembu",
                    Email = "sup.administration@swiftcare.com",
                    JobTitle = "Administration Supervisor",
                    Bio = "Responsible for supervising volunteers assisting with administrative activities.",
                    Area = VolunteerArea.Administration
                },

                new SupervisorSeedModel
                {
                    FirstName = "Sibusiso",
                    LastName = "Ndlovu",
                    Email = "sup.garden@swiftcare.com",
                    JobTitle = "Garden & Outdoor Supervisor",
                    Bio = "Responsible for supervising volunteers supporting gardening and outdoor activities.",
                    Area = VolunteerArea.GardenAndOutdoor
                },

                new SupervisorSeedModel
                {
                    FirstName = "Ayanda",
                    LastName = "Zulu",
                    Email = "sup.events@swiftcare.com",
                    JobTitle = "Events Supervisor",
                    Bio = "Responsible for supervising volunteers supporting SwiftCare events and special activities.",
                    Area = VolunteerArea.Events
                },

                new SupervisorSeedModel
                {
                    FirstName = "Themba",
                    LastName = "Maseko",
                    Email = "sup.transportation@swiftcare.com",
                    JobTitle = "Transportation Supervisor",
                    Bio = "Responsible for supervising volunteers supporting transportation-related activities.",
                    Area = VolunteerArea.Transportation
                },

                new SupervisorSeedModel
                {
                    FirstName = "Naledi",
                    LastName = "Molefe",
                    Email = "sup.outreach@swiftcare.com",
                    JobTitle = "Community Outreach Supervisor",
                    Bio = "Responsible for supervising volunteers supporting community outreach and engagement.",
                    Area = VolunteerArea.CommunityOutreach
                }
            };

            foreach (var supervisorData in supervisors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await CreateSupervisorIfNotExistsAsync(
                    context,
                    userManager,
                    supervisorData,
                    cancellationToken);
            }
        }


        // ============================================================
        // CREATE SUPERVISOR
        // ============================================================

        // ============================================================
        // CREATE / REPAIR SUPERVISOR
        // ============================================================

        private static async Task CreateSupervisorIfNotExistsAsync(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            SupervisorSeedModel data,
            CancellationToken cancellationToken)
        {
            // ============================================================
            // CHECK WHETHER THE ACCOUNT ALREADY EXISTS
            // ============================================================

            var existingUser =
                await userManager.FindByEmailAsync(data.Email);


            // ============================================================
            // EXISTING SUPERVISOR
            // ============================================================

            if (existingUser != null)
            {
                Console.WriteLine(
                    $"[SupervisorSeedData] Existing supervisor found: {data.Email}");


                // --------------------------------------------------------
                // MAKE SURE EMAIL IS CONFIRMED
                // --------------------------------------------------------

                if (!existingUser.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;

                    Console.WriteLine(
                        $"[SupervisorSeedData] Email confirmed: {data.Email}");
                }


                // --------------------------------------------------------
                // MAKE SURE USERNAME IS CORRECT
                // --------------------------------------------------------

                if (existingUser.UserName != data.Email)
                {
                    existingUser.UserName = data.Email;
                    existingUser.NormalizedUserName =
                        userManager.NormalizeName(data.Email);

                    Console.WriteLine(
                        $"[SupervisorSeedData] Username repaired: {data.Email}");
                }


                // --------------------------------------------------------
                // MAKE SURE SUPERVISOR ROLE EXISTS
                // --------------------------------------------------------

                if (!await userManager.IsInRoleAsync(
                        existingUser,
                        "Supervisor"))
                {
                    var existingUserRoleResult =
                        await userManager.AddToRoleAsync(
                            existingUser,
                            "Supervisor");

                    if (!existingUserRoleResult.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            existingUserRoleResult.Errors.Select(e =>
                                $"{e.Code}: {e.Description}"));

                        throw new InvalidOperationException(
                            $"Failed to assign Supervisor role to '{data.Email}': {errors}");
                    }

                    Console.WriteLine(
                        $"[SupervisorSeedData] Supervisor role added: {data.Email}");
                }


                // --------------------------------------------------------
                // RESET PASSWORD
                //
                // Development seeding:
                // Make sure all seeded Supervisor accounts use the
                // configured SupervisorPassword.
                // --------------------------------------------------------

                var passwordValid =
                    await userManager.CheckPasswordAsync(
                        existingUser,
                        SupervisorPassword);

                if (!passwordValid)
                {
                    var removePasswordResult =
                        await userManager.RemovePasswordAsync(
                            existingUser);

                    if (!removePasswordResult.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            removePasswordResult.Errors.Select(e =>
                                $"{e.Code}: {e.Description}"));

                        throw new InvalidOperationException(
                            $"Failed to remove existing password for '{data.Email}': {errors}");
                    }

                    var addPasswordResult =
                        await userManager.AddPasswordAsync(
                            existingUser,
                            SupervisorPassword);

                    if (!addPasswordResult.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            addPasswordResult.Errors.Select(e =>
                                $"{e.Code}: {e.Description}"));

                        throw new InvalidOperationException(
                            $"Failed to reset password for '{data.Email}': {errors}");
                    }

                    Console.WriteLine(
                        $"[SupervisorSeedData] Password reset successfully: {data.Email}");
                }


                // --------------------------------------------------------
                // UPDATE SUPERVISOR DATA IF THIS IS A SUPERVISOR ENTITY
                // --------------------------------------------------------

                if (existingUser is Supervisor existingSupervisor)
                {
                    existingSupervisor.FirstName = data.FirstName;
                    existingSupervisor.LastName = data.LastName;
                    existingSupervisor.JobTitle = data.JobTitle;
                    existingSupervisor.Bio = data.Bio;
                    existingSupervisor.Area = data.Area;
                    existingSupervisor.IsActive = true;

                    existingSupervisor.ModifiedDateTime =
                        DateTime.Now;

                    existingSupervisor.ModifiedBy =
                        "SupervisorSeedData";

                    SetEmailVerifiedIfAvailable(
                        existingSupervisor);
                }


                await context.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine(
                    $"[SupervisorSeedData] Existing supervisor repaired successfully: {data.Email}");

                return;
            }


            // ============================================================
            // CREATE NEW SUPERVISOR
            // ============================================================

            var supervisor = new Supervisor
            {
                FirstName = data.FirstName,
                LastName = data.LastName,

                Email = data.Email,
                UserName = data.Email,

                EmailConfirmed = true,

                DateOfBirth = new DateTime(
                    1990,
                    1,
                    1),

                ProfilePicture =
                    "Images/default_profile_image.jpg",

                CreatedDateTime = DateTime.Now,
                ModifiedDateTime = DateTime.Now,

                CreatedBy = "SupervisorSeedData",
                ModifiedBy = "SupervisorSeedData",

                JobTitle = data.JobTitle,
                Bio = data.Bio,
                Area = data.Area,

                IsActive = true,

                AssignedDate = DateTime.Now,
                EndDate = null
            };


            // ============================================================
            // EMAIL VERIFIED
            // ============================================================

            SetEmailVerifiedIfAvailable(
                supervisor);


            // ============================================================
            // CREATE IDENTITY ACCOUNT
            // ============================================================

            var createResult =
                await userManager.CreateAsync(
                    supervisor,
                    SupervisorPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create supervisor '{data.Email}': {errors}");
            }


            // ============================================================
            // ASSIGN SUPERVISOR ROLE
            // ============================================================

            var newSupervisorRoleResult =
                await userManager.AddToRoleAsync(
                    supervisor,
                    "Supervisor");

            if (!newSupervisorRoleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    newSupervisorRoleResult.Errors.Select(e =>
                        $"{e.Code}: {e.Description}"));

                throw new InvalidOperationException(
                    $"Failed to assign Supervisor role to '{data.Email}': {errors}");
            }


            // ============================================================
            // SAVE
            // ============================================================

            await context.SaveChangesAsync(
                cancellationToken);


            Console.WriteLine(
                $"[SupervisorSeedData] New supervisor created successfully: {data.Email}");
        }

        // ============================================================
        // EMAIL VERIFIED
        // ============================================================

        private static void SetEmailVerifiedIfAvailable(
            UserBaseModel user)
        {
            var property =
                user.GetType().GetProperty("EmailVerified");

            if (property != null &&
                property.CanWrite &&
                property.PropertyType == typeof(bool))
            {
                property.SetValue(user, true);
            }
        }


        // ============================================================
        // SUPERVISOR SEED MODEL
        // ============================================================

        private sealed class SupervisorSeedModel
        {
            public string FirstName { get; set; } = string.Empty;

            public string LastName { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string JobTitle { get; set; } = string.Empty;

            public string Bio { get; set; } = string.Empty;

            public VolunteerArea Area { get; set; }
        }
    }
}