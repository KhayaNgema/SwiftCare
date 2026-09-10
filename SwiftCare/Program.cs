using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Security.Cryptography;

public class Program
{
    public static void Main(string[] args)
    {
        GenerateAndDisplayAesKeys();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                "Logs/myapp-.txt",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("[SWIFTCARE PROGRAM] Building host...");
            Console.WriteLine("============================================================");

            var host = CreateHostBuilder(args).Build();

            Console.WriteLine("============================================================");
            Console.WriteLine("[SWIFTCARE PROGRAM] Host built successfully.");
            Console.WriteLine("[SWIFTCARE PROGRAM] Starting host...");
            Console.WriteLine("============================================================");

            host.Run();

            Console.WriteLine("============================================================");
            Console.WriteLine("[SWIFTCARE PROGRAM] Host stopped.");
            Console.WriteLine("============================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("[SWIFTCARE PROGRAM] HOST STARTUP FAILED");
            Console.WriteLine("============================================================");

            Console.WriteLine(
                $"Exception Type: {ex.GetType().FullName}");

            Console.WriteLine(
                $"Exception Message: {ex.Message}");

            Console.WriteLine(
                "------------------------------------------------------------");

            Console.WriteLine(
                $"FULL EXCEPTION:");

            Console.WriteLine(ex.ToString());

            Console.WriteLine("============================================================");

            Log.Fatal(
                ex,
                "Host terminated unexpectedly");
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


                //webBuilder.ConfigureKestrel(options =>
                //{
                //    options.ListenAnyIP(7102, listenOptions =>
                //    {
                //        listenOptions.UseHttps(
                //            "C:\\Users\\User\\Downloads\\Knssoftware.com\\knssoftware.pfx",
                //            "Khaya@12"); 
                //    });
                //});


                //webBuilder.ConfigureKestrel(options =>
                //{
                //    options.ListenAnyIP(7102, listenOptions =>
                //    {
                //        listenOptions.UseHttps(
                //            @"C:\Certs\knssoftware.pfx", 
                //            "Khaya@12");
                //    });
                //});
            });

    private static void GenerateAndDisplayAesKeys()
    {
        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();

            string keyBase64 = Convert.ToBase64String(aes.Key);
            string ivBase64 = Convert.ToBase64String(aes.IV);

            System.IO.File.WriteAllText("aes-keys.txt", $"AES Key (Base64): {keyBase64}\nAES IV (Base64): {ivBase64}");

            Console.WriteLine("AES Key (Base64): " + keyBase64);
            Console.WriteLine("AES IV (Base64): " + ivBase64);
        }
    }
}
