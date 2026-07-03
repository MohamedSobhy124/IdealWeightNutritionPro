using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Api;

internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        using var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            if (await db.Database.CanConnectAsync(connectTimeout.Token))
            {
                await EnsureRefreshTokensTableAsync(db);
                await SeedRolesAsync(roleManager);
            }
            else
            {
                logger.LogWarning("Database is not reachable; skipping auth schema initialization.");
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Database connection timed out after 5s; skipping auth schema initialization.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization skipped.");
        }
    }

    private static async Task EnsureRefreshTokensTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
            BEGIN
                CREATE TABLE [RefreshTokens] (
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [UserId] nvarchar(450) NOT NULL,
                    [TokenHash] nvarchar(128) NOT NULL,
                    [ExpiresAt] datetime2 NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [RevokedAt] datetime2 NULL,
                    [ReplacedByTokenHash] nvarchar(128) NULL
                );
                CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens]([TokenHash]);
                CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens]([UserId]);
            END
            """);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Customer, Roles.Admin, Roles.Employee, Roles.Company })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
