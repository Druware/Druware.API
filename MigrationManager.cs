using System;
using Druware.Server;
using Druware.Server.Content;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Druware.API
{
    public static class MigrationManager
    {

        public static WebApplication MigrateDatabase(this WebApplication host)
        {
            // can we get to a settings object? 
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            var settings = new AppSettings(configuration);
            
            using var scope = host.Services.CreateScope();

            // get the correct context.
            DbContext? serverContext;
            DbContext? contentContext;
            switch (settings.DbType)
            {
                case DbContextType.Microsoft:
                    serverContext = scope.ServiceProvider
                        .GetRequiredService<ServerContextMicrosoft>();
                    contentContext = scope.ServiceProvider
                        .GetRequiredService<ContentContextMicrosoft>();
                    break;
                case DbContextType.PostgreSql:
                    serverContext = scope.ServiceProvider
                        .GetRequiredService<ServerContextPostgreSql>();
                    contentContext = scope.ServiceProvider
                        .GetRequiredService<ContentContextPostgreSql>();
                    break;
                case DbContextType.Sqlite:
                    serverContext = scope.ServiceProvider
                        .GetRequiredService<ServerContextSqlite>();
                    contentContext = scope.ServiceProvider
                        .GetRequiredService<ContentContextSqlite>();
                    break;
                default:
                    throw new Exception("Unknown DbType");
            }

            if (serverContext == null) 
                throw new Exception("No ServerContext is available");
            if (contentContext == null) 
                throw new Exception("No ContentContext is available");
            
            serverContext.Database.Migrate();
            contentContext.Database.Migrate();

            // Followup with Security Roles that we want in place
            // -- contentContext.ConfigureSecurityRoles(serveContext);

            return host;
        }
    }
}

