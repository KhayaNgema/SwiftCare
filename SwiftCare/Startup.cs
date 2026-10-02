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
using SwiftCare.Configuration;
using SwiftCare.Data;
using SwiftCare.Helpers;
using SwiftCare.Hubs;
using SwiftCare.Interfaces;
using SwiftCare.Models;
using SwiftCare.Services;
using SwiftCare.Services.AI;
using SwiftCare.Services.IdentityVerification;
using SwiftCare.Services.VolunteerActivation;
using SwiftCare.Services.VolunteerATS;
using SwiftCare.Services.VolunteerATS.CV;
using SwiftCare.Services.VolunteerContracts;
using SwiftCare.Services.VolunteerOnboarding;
using SwiftCare.Services.Volunteers;
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
    }

    public IConfiguration Configuration { get; }


    // ================================================================
    // CONFIGURE SERVICES
    // ================================================================

    public void ConfigureServices(IServiceCollection services)
    {
        // ============================================================
        // CONNECTION STRING
        // ============================================================

        var connectionString =
            Configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is missing.");
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

        services.Configure<DiditOptions>(
            Configuration.GetSection(
                DiditOptions.SectionName));

        services.AddScoped<
            IDiditService,
            DiditService>();

        services.AddScoped<
            IVolunteerIdentityVerificationService,
            VolunteerIdentityVerificationService>();

        services.AddScoped<
            IDiditWebhookSignatureValidator,
            DiditWebhookSignatureValidator>();

        services.AddScoped<
            IDiditWebhookService,
            DiditWebhookService>();

        services.AddScoped<
            RandomPasswordGeneratorService>();

        services.AddScoped<EmailService>();

        services.AddScoped<ReceiveEquipmentOrder>();

        services.AddScoped<OrderNumberGenerator>();

        services.AddScoped<SmsService>();

        services.AddScoped<
            VolunteerContractReferenceGenerator>();

        services.AddScoped<
            IVolunteerContractService,
            VolunteerContractService>();

        services.AddScoped<
            IVolunteerAccountService,
            VolunteerAccountService>();

        services.AddScoped<
            IVolunteerContractContentBuilder,
            VolunteerContractContentBuilder>();

        services.AddScoped<
            IVolunteerContractPdfGenerator,
            VolunteerContractPdfGenerator>();

        services.AddScoped<BarcodeService>();

        services.AddScoped<QrCodeService>();

        services.AddScoped<
            IVolunteerActivationService,
            VolunteerActivationService>();

        services.AddScoped<
            IVolunteerOnboardingService,
            VolunteerOnboardingService>();

        services.AddScoped<
            IVolunteerInterviewEvaluationEngine,
            VolunteerInterviewEvaluationEngine>();

        services.Configure<SwiftCareLlmOptions>(
            Configuration.GetSection(
                SwiftCareLlmOptions.SectionName));

        services.AddSingleton<
            ISwiftCareLlmService,
            SwiftCareLlmService>();

        services.AddHttpClient();


        // ============================================================
        // VOLUNTEER ATS
        // ============================================================

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


        // ============================================================
        // SIGNALR
        // ============================================================

        services.AddSignalR();


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

                return new DocumentIntelligenceClient(
                    endpointUri,
                    new AzureKeyCredential(
                        apiKey.Trim()));
            });


        // ============================================================
        // HANGFIRE
        // ============================================================

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

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit =
                500L * 1024L * 1024L;
        });
    }


    // ================================================================
    // CONFIGURE HTTP PIPELINE
    // ================================================================

    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env)
    {
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

                endpoints.MapHub<VolunteerInterviewHub>(
                    "/volunteerInterviewHub");
            });


        // ============================================================
        // CREATE ROLES / DEFAULT USER
        // ============================================================

        app.ApplicationServices
            .CreateRolesAndDefaultUser()
            .GetAwaiter()
            .GetResult();


        // ============================================================
        // SEED VOLUNTEER POLICIES
        // ============================================================

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


        // ============================================================
        // SEED SUPERVISORS
        // ============================================================

        using (var scope =
            app.ApplicationServices.CreateScope())
        {
            SupervisorSeedData
                .SeedAsync(scope.ServiceProvider)
                .GetAwaiter()
                .GetResult();
        }


        // ============================================================
        // SEED VOLUNTEER TASK LIBRARY
        // ============================================================

        using (var scope =
            app.ApplicationServices.CreateScope())
        {
            var context =
                scope.ServiceProvider
                    .GetRequiredService<SwiftCareDbContext>();

            TaskSeeder
                .SeedAsync(context)
                .GetAwaiter()
                .GetResult();
        }
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