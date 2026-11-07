using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StudioSport.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // קודם מחרוזת החיבור, ואם אין - הרכבה ידנית ממשתני סביבה
        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = config["DB_HOST"] ?? "localhost";
            var database = config["DB_NAME"] ?? "StudioSport";
            var user = config["DB_USER"] ?? "postgres";
            var password = config["DB_PASSWORD"];

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "No connection string found. Set ConnectionStrings:Default or DB_PASSWORD.");

            connectionString = $"Host={host};Database={database};Username={user};Password={password}";
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}