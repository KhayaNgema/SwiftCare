using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using SwiftCare.Data;

public class SwiftCareDbContextFactory : IDesignTimeDbContextFactory<SwiftCareDbContext>
{
    public SwiftCareDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<SwiftCareDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SwiftCareDbContext(optionsBuilder.Options);
    }
}
