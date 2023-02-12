using System;
using Microsoft.EntityFrameworkCore;

namespace Druware.API
{
    public static class MigrationManager
    {
        public static WebApplication MigrateDatabase(this WebApplication host)
        {
            using (var scope = host.Services.CreateScope())
            {

                using (var appContext = scope.ServiceProvider.GetRequiredService<Druware.Server.ServerContext>())
                {
                    try
                    {
                        appContext.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        // Log errors or do anything you think it's needed
                        Console.Error.WriteLine("MigrateDatabase Failed: {0}", ex.Message);
                        throw;
                    }
                }
                using (var appContext = scope.ServiceProvider.GetRequiredService<Druware.Server.Content.ContentContext>())
                {
                    try
                    {
                        appContext.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        // Log errors or do anything you think it's needed
                        Console.Error.WriteLine("MigrateDatabase Failed: {0}", ex.Message);
                        throw;
                    }
                }
            }

            // Followup with Security Roles that we want in place
            using (var scope = host.Services.CreateScope())
            using (Druware.Server.ServerContext context = scope.ServiceProvider.GetRequiredService<Druware.Server.ServerContext>())
                if (context != null)
                    Druware.Server.Content.ContentContext.ConfigureSecurityRoles(context);

            return host;
        }
    }
}

