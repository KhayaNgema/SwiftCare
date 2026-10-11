using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                "Logs/myapp-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        try
        {
            Log.Information("============================================================");
            Log.Information("[SWIFTCARE PROGRAM] Application starting...");
            Log.Information("[SWIFTCARE PROGRAM] Environment: {Environment}",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            Log.Information("[SWIFTCARE PROGRAM] Current directory: {CurrentDirectory}",
                Environment.CurrentDirectory);
            Log.Information("[SWIFTCARE PROGRAM] Base directory: {BaseDirectory}",
                AppContext.BaseDirectory);
            Log.Information("============================================================");

            var host = CreateHostBuilder(args).Build();

            Log.Information("============================================================");
            Log.Information("[SWIFTCARE PROGRAM] Host built successfully.");
            Log.Information("[SWIFTCARE PROGRAM] Starting host...");
            Log.Information("============================================================");

            host.Run();

            Log.Information("============================================================");
            Log.Information("[SWIFTCARE PROGRAM] Host stopped.");
            Log.Information("============================================================");
        }
        catch (Exception ex)
        {
            Log.Fatal(
                ex,
                "[SWIFTCARE PROGRAM] HOST STARTUP FAILED");

            Console.WriteLine(
                "[SWIFTCARE PROGRAM] HOST STARTUP FAILED");

            Console.WriteLine(ex);

            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}