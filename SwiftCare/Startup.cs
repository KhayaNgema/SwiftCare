using Azure;
using Azure.AI.DocumentIntelligence;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SwiftCare.Configuration;
using SwiftCare.Data;
using SwiftCare.Helpers;
using SwiftCare.Hubs;
using SwiftCare.Interfaces;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.Services.AI;
using SwiftCare.Services.VolunteerActivation;
using SwiftCare.Services.VolunteerATS;
using SwiftCare.Services.VolunteerATS.CV;
using SwiftCare.Services.VolunteerOnboarding;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Startup constructor called.");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Environment: " +
            $"{Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown"}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Database configured: " +
            $"{!string.IsNullOrWhiteSpace(
                Configuration.GetConnectionString("DefaultConnection"))}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Azure DI Endpoint configured: " +
            $"{!string.IsNullOrWhiteSpace(
                Configuration["AzureDocumentIntelligence:Endpoint"])}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Azure DI API Key configured: " +
            $"{!string.IsNullOrWhiteSpace(
                Configuration["AzureDocumentIntelligence:ApiKey"])}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Azure DI Model ID configured: " +
            $"{!string.IsNullOrWhiteSpace(
                Configuration["AzureDocumentIntelligence:ModelId"])}");

        Console.WriteLine(
            "============================================================");
    }

    public IConfiguration Configuration { get; }


    // ================================================================
    // CONFIGURE SERVICES
    // ================================================================

    public void ConfigureServices(IServiceCollection services)
    {
        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] ConfigureServices STARTED");

        Console.WriteLine(
            "============================================================");


        // ============================================================
        // LOGGING
        // Register this early.
        // ============================================================

        services.AddLogging(
            logging =>
            {
                logging.ClearProviders();

                logging.AddConsole();

                logging.AddDebug();

                logging.SetMinimumLevel(
                    LogLevel.Information);
            });


        // ============================================================
        // CONNECTION STRING
        // ============================================================

        var connectionString =
            Configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine(
                "[SWIFTCARE STARTUP] ERROR: DefaultConnection is missing.");
        }
        else
        {
            Console.WriteLine(
                "[SWIFTCARE STARTUP] Database connection configured: TRUE");
        }


        // ============================================================
        // AES CONFIGURATION
        // ============================================================

        var key =
            Configuration["AES_KEY"];

        var iv =
            Configuration["AES_IV"];


        byte[] keyBytes;

        byte[] ivBytes;


        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "AES_KEY is not configured.");
        }


        if (string.IsNullOrWhiteSpace(iv))
        {
            throw new InvalidOperationException(
                "AES_IV is not configured.");
        }


        try
        {
            keyBytes =
                Convert.FromBase64String(key);

            ivBytes =
                Convert.FromBase64String(iv);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "AES_KEY or AES_IV is not valid Base64.",
                ex);
        }


        Console.WriteLine(
            "[SWIFTCARE STARTUP] AES configuration loaded successfully.");


        // ============================================================
        // FORM UPLOAD LIMIT
        // ============================================================

        services.Configure<FormOptions>(
            options =>
            {
                options.MultipartBodyLengthLimit =
                    268435456;
            });


        // ============================================================
        // ENCRYPTION
        // ============================================================

        services.AddSingleton(
            new EncryptionConfiguration
            {
                Key = keyBytes,
                Iv = ivBytes
            });

        services.AddScoped<
            IEncryptionService,
            EncryptionService>();


        // ============================================================
        // AUTHORIZATION
        // ============================================================

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    "AnyRole",
                    policy =>
                        policy.RequireAssertion(
                            context =>
                                context.User.Identity != null &&
                                context.User.Identity.IsAuthenticated &&
                                context.User.Claims.Any(
                                    c =>
                                        c.Type ==
                                        ClaimTypes.Role)));
            });


        // ============================================================
        // SESSION
        // ============================================================

        services.AddDistributedMemoryCache();

        services.AddSession(
            options =>
            {
                options.IdleTimeout =
                    TimeSpan.FromHours(2);

                options.Cookie.HttpOnly =
                    true;

                options.Cookie.IsEssential =
                    true;

                options.Cookie.Name =
                    ".SwiftCare.Session";
            });


        // ============================================================
        // DATABASE
        // ============================================================

        services.AddDbContext<SwiftCareDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString);
            });


        // ============================================================
        // IDENTITY
        // ============================================================

        services.AddDefaultIdentity<UserBaseModel>(
            options =>
            {
                options.SignIn.RequireConfirmedAccount =
                    true;
            })
            .AddDefaultTokenProviders()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SwiftCareDbContext>();


        services.Configure<DataProtectionTokenProviderOptions>(
            options =>
            {
                options.TokenLifespan =
                    TimeSpan.FromHours(72);
            });


        // ============================================================
        // IDENTITY SERVICES
        // ============================================================

        services.AddScoped<
            SignInManager<UserBaseModel>>();

        services.AddScoped<
            UserManager<UserBaseModel>>();


        // ============================================================
        // AUTOMAPPER
        // ============================================================

        services.AddAutoMapper(
            cfg =>
            {
            },
            typeof(Startup).Assembly);


        // ============================================================
        // MVC / RAZOR
        // ============================================================

        services.AddControllersWithViews();

        services.AddRazorPages();

        services.AddHttpContextAccessor();


        // ============================================================
        // CULTURE
        // ============================================================

        SetCulture("en-US");


        // ============================================================
        // GENERAL SWIFTCARE SERVICES
        // ============================================================

        services.AddScoped<FileUploadService>();

        services.AddScoped<
            RandomPasswordGeneratorService>();

        services.AddScoped<EmailService>();

        services.AddScoped<ReceiveEquipmentOrder>();

        services.AddScoped<OrderNumberGenerator>();

        services.AddScoped<SmsService>();

        services.AddScoped<VolunteerContractReferenceGenerator>();

        services.AddScoped<
            VolunteerContractReferenceGenerator>();

        services.AddScoped<
            IVolunteerContractGenerationService,
            VolunteerContractGenerationService>();

        services.AddScoped<VolunteerContractReferenceGenerator>();

        services.AddScoped<
            IVolunteerContractGenerationService,
            VolunteerContractGenerationService>();

        services.AddScoped<
            IVolunteerContractContentBuilder,
            VolunteerContractContentBuilder>();

        services.AddScoped<
            IVolunteerContractPdfGenerator,
            VolunteerContractPdfGenerator>();

        services.AddScoped<IVolunteerContractService,VolunteerContractService>();

        services.AddScoped<BarcodeService>();

        services.AddScoped<QrCodeService>();

        services.AddScoped<IVolunteerActivationService, VolunteerActivationService>();

        services.AddScoped<IVolunteerOnboardingService, VolunteerOnboardingService>();

        services.AddScoped<IVolunteerInterviewEvaluationEngine,VolunteerInterviewEvaluationEngine>();
        services.Configure<SwiftCareLlmOptions>(
            Configuration.GetSection(SwiftCareLlmOptions.SectionName));

        services.AddSingleton<ISwiftCareLlmService, SwiftCareLlmService>();

        services.AddHttpClient();


        // ============================================================
        // VOLUNTEER ATS
        // ============================================================

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Registering Volunteer ATS services...");


        services.AddScoped<
            VolunteerEligibilityEngine>();

        services.AddScoped<
            VolunteerScoringEngine>();

        services.AddScoped<
            VolunteerRankingEngine>();

        services.AddScoped<
            VolunteerATSAuditService>();

        services.AddScoped<
            IVolunteerAIEvaluationEngine,
            VolunteerAIEvaluationEngine>();

        services.AddScoped<
            VolunteerShortlistingPipeline>();


        // ============================================================
        // CV DOCUMENT ENGINE
        // ============================================================

        services.AddScoped<
            IVolunteerCVDocumentEngine,
            VolunteerCVDocumentEngine>();


        // ============================================================
        // CV EXTRACTION ENGINE
        // ============================================================

        services.AddScoped<
            IVolunteerCVExtractionEngine,
            VolunteerCVExtractionEngine>();

        services.AddSignalR();
        // ============================================================
        // CV PARSING ENGINE
        // ============================================================

        services.AddScoped<
            IVolunteerCVParsingEngine,
            VolunteerCVParsingEngine>();


        // ============================================================
        // CV PROCESSING ENGINE
        // ============================================================

        services.AddScoped<
            IVolunteerCVProcessingEngine,
            VolunteerCVProcessingEngine>();


        Console.WriteLine(
            "[SWIFTCARE STARTUP] Volunteer ATS services registered.");


        // ============================================================
        // AZURE DOCUMENT INTELLIGENCE SERVICE
        // ============================================================

        services.AddScoped<
            IAzureDocumentIntelligenceCvService,
            AzureDocumentIntelligenceCvService>();


        // ============================================================
        // AZURE DOCUMENT INTELLIGENCE CLIENT
        // ============================================================

        services.AddSingleton<DocumentIntelligenceClient>(
            serviceProvider =>
            {
                Console.WriteLine(
                    "============================================================");

                Console.WriteLine(
                    "[SWIFTCARE AZURE DI] Creating DocumentIntelligenceClient");


                var configuration =
                    serviceProvider.GetRequiredService<
                        IConfiguration>();


                var endpoint =
                    configuration[
                        "AzureDocumentIntelligence:Endpoint"];


                var apiKey =
                    configuration[
                        "AzureDocumentIntelligence:ApiKey"];


                var modelId =
                    configuration[
                        "AzureDocumentIntelligence:ModelId"];


                Console.WriteLine(
                    $"[SWIFTCARE AZURE DI] Endpoint configured: " +
                    $"{!string.IsNullOrWhiteSpace(endpoint)}");


                Console.WriteLine(
                    $"[SWIFTCARE AZURE DI] API Key configured: " +
                    $"{!string.IsNullOrWhiteSpace(apiKey)}");


                Console.WriteLine(
                    $"[SWIFTCARE AZURE DI] Model ID configured: " +
                    $"{!string.IsNullOrWhiteSpace(modelId)}");


                // ----------------------------------------------------
                // VALIDATE ENDPOINT
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    throw new InvalidOperationException(
                        "AzureDocumentIntelligence:Endpoint is not configured.");
                }


                // ----------------------------------------------------
                // VALIDATE API KEY
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "AzureDocumentIntelligence:ApiKey is not configured.");
                }


                // ----------------------------------------------------
                // VALIDATE MODEL
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(modelId))
                {
                    throw new InvalidOperationException(
                        "AzureDocumentIntelligence:ModelId is not configured.");
                }


                // ----------------------------------------------------
                // VALIDATE URI
                // ----------------------------------------------------

                if (!Uri.TryCreate(
                    endpoint.Trim(),
                    UriKind.Absolute,
                    out var endpointUri))
                {
                    throw new InvalidOperationException(
                        "AzureDocumentIntelligence:Endpoint is not a valid absolute URI.");
                }


                // ----------------------------------------------------
                // CREATE CLIENT
                // ----------------------------------------------------

                var client =
                    new DocumentIntelligenceClient(
                        endpointUri,
                        new AzureKeyCredential(
                            apiKey.Trim()));


                Console.WriteLine(
                    "[SWIFTCARE AZURE DI] DocumentIntelligenceClient created successfully.");

                Console.WriteLine(
                    "============================================================");


                return client;
            });


        // ============================================================
        // SIGNALR
        // ============================================================

        services.AddSignalR(
            options =>
            {
                options.EnableDetailedErrors =
                    false;
            });


        // ============================================================
        // HANGFIRE
        // ============================================================

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Configuring Hangfire...");


        services.AddHangfire(
            config =>
            {
                config
                    .SetDataCompatibilityLevel(
                        CompatibilityLevel.Version_170)

                    .UseSimpleAssemblyNameTypeSerializer()

                    .UseRecommendedSerializerSettings()

                    .UseSqlServerStorage(
                        connectionString,
                        new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout =
                                TimeSpan.FromMinutes(5),

                            SlidingInvisibilityTimeout =
                                TimeSpan.FromMinutes(5),

                            QueuePollInterval =
                                TimeSpan.Zero,

                            UseRecommendedIsolationLevel =
                                true,

                            DisableGlobalLocks =
                                true,

                            UsePageLocksOnDequeue =
                                true,

                            SchemaName =
                                "hangfire"
                        });
            });


        services.AddHangfireServer();


        // ============================================================
        // FORWARDED HEADERS
        // ============================================================

        services.Configure<ForwardedHeadersOptions>(
            options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;
            });


        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] ConfigureServices COMPLETED");

        Console.WriteLine(
            "============================================================");
    }


    // ================================================================
    // CONFIGURE HTTP PIPELINE
    // ================================================================

    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Configure STARTED");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Environment: " +
            $"{env.EnvironmentName}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] WebRootPath: " +
            $"{env.WebRootPath}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] ContentRootPath: " +
            $"{env.ContentRootPath}");

        Console.WriteLine(
            "============================================================");


        // ============================================================
        // SERILOG
        // ============================================================

        Log.Logger =
            new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File(
                    "Logs/myapp-.txt",
                    rollingInterval:
                        RollingInterval.Day)
                .CreateLogger();


        Console.WriteLine(
            "[SWIFTCARE STARTUP] Serilog configured.");


        // ============================================================
        // DEVELOPMENT / PRODUCTION
        // ============================================================

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler(
                "/Home/Error");

            app.UseHsts();
        }


        // ============================================================
        // FORWARDED HEADERS
        // ============================================================

        app.UseForwardedHeaders();


        // ============================================================
        // HTTPS
        // ============================================================

        app.UseHttpsRedirection();


        // ============================================================
        // STATIC FILES
        // ============================================================

        app.UseStaticFiles();


        // ============================================================
        // ROUTING
        // ============================================================

        app.UseRouting();


        // ============================================================
        // SESSION
        // ============================================================

        app.UseSession();


        // ============================================================
        // STRICT COOKIE
        // ============================================================

        app.Use(
            async (context, next) =>
            {
                context.Response.Cookies.Append(
                    "StrictlyNecessaryCookie",
                    "Value",
                    new CookieOptions
                    {
                        HttpOnly = true,

                        Secure = true,

                        SameSite =
                            SameSiteMode.Strict
                    });

                await next.Invoke();
            });


        // ============================================================
        // AUTHENTICATION
        // ============================================================

        app.UseAuthentication();


        // ============================================================
        // AUTHORIZATION
        // ============================================================

        app.UseAuthorization();


        // ============================================================
        // HANGFIRE
        // ============================================================

        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions
            {
                Authorization =
                    new[]
                    {
                        new HangfireAuthorizationFilter()
                    }
            });


        // ============================================================
        // SERILOG REQUEST LOGGING
        // ============================================================

        app.UseSerilogRequestLogging();


        // ============================================================
        // ENDPOINTS
        // ============================================================

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern:
                        "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
                endpoints.MapHub<VolunteerInterviewHub>("/volunteerInterviewHub");
            });


        // ============================================================
        // AZURE CONFIGURATION CHECK
        // ============================================================

        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] FINAL AZURE DOCUMENT INTELLIGENCE CHECK");

        var azureEndpoint =
            Configuration[
                "AzureDocumentIntelligence:Endpoint"];

        var azureApiKey =
            Configuration[
                "AzureDocumentIntelligence:ApiKey"];

        var azureModelId =
            Configuration[
                "AzureDocumentIntelligence:ModelId"];


        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Endpoint configured: " +
            $"{!string.IsNullOrWhiteSpace(azureEndpoint)}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] API Key configured: " +
            $"{!string.IsNullOrWhiteSpace(azureApiKey)}");

        Console.WriteLine(
            $"[SWIFTCARE STARTUP] Model ID configured: " +
            $"{!string.IsNullOrWhiteSpace(azureModelId)}");


        if (!string.IsNullOrWhiteSpace(
            azureEndpoint))
        {
            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Endpoint: " +
                $"{azureEndpoint}");
        }


        if (!string.IsNullOrWhiteSpace(
            azureModelId))
        {
            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Model ID: " +
                $"{azureModelId}");
        }


        Console.WriteLine(
            "============================================================");


        // ============================================================
        // CREATE ROLES / DEFAULT USER
        // ============================================================

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Creating roles/default user...");


        try
        {
            app.ApplicationServices
                .CreateRolesAndDefaultUser()
                .Wait();


            Console.WriteLine(
                "[SWIFTCARE STARTUP] Roles/default user completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "[SWIFTCARE STARTUP] FAILED TO CREATE ROLES/DEFAULT USER");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Exception Type: " +
                $"{ex.GetType().FullName}");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Exception Message: " +
                $"{ex.Message}");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Full Exception: " +
                $"{ex}");

            Console.WriteLine(
                "============================================================");

            throw;
        }
        // ============================================================
        // SEED VOLUNTEER POLICIES
        // ============================================================

        Console.WriteLine(
            "[SWIFTCARE STARTUP] Seeding volunteer onboarding policies...");

        try
        {
            using (var scope =
                app.ApplicationServices.CreateScope())
            {
                var context =
                    scope.ServiceProvider
                        .GetRequiredService<SwiftCareDbContext>();

                VolunteerPolicySeedData
                    .SeedAsync(context)
                    .GetAwaiter()
                    .GetResult();
            }

            Console.WriteLine(
                "[SWIFTCARE STARTUP] Volunteer onboarding policies seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "[SWIFTCARE STARTUP] FAILED TO SEED VOLUNTEER POLICIES");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Exception Type: " +
                $"{ex.GetType().FullName}");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Exception Message: " +
                $"{ex.Message}");

            Console.WriteLine(
                $"[SWIFTCARE STARTUP] Full Exception: " +
                $"{ex}");

            Console.WriteLine(
                "============================================================");

            throw;
        }

        // ============================================================
        // APPLICATION READY
        // ============================================================

        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "[SWIFTCARE STARTUP] APPLICATION STARTUP COMPLETE");

        Console.WriteLine(
            "============================================================");


    }


    // ================================================================
    // CULTURE
    // ================================================================

    private void SetCulture(
        string cultureCode)
    {
        var culture =
            CultureInfo.CreateSpecificCulture(
                cultureCode);

        Thread.CurrentThread.CurrentCulture =
            culture;

        Thread.CurrentThread.CurrentUICulture =
            culture;
    }
}