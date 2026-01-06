using System;
using System.Text.Json.Serialization;
using Druware.API;
using Druware.Server;
using Druware.Server.Content;
using Druware.Server.Entities;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// dotnet ef migrations add Init

var builder = WebApplication.CreateBuilder(args);

// TODO: Add a bunch of checks to determine our DB connectivity at start up and
//       provide decent logging, up to, and including launching the appplication
//       without DB connectivity, with a status service that demonstrates that 
//       the service is up, and can periodically retry the database.

// =============================================================================
// Establish all of the parameters we need for starting the application up, 
// using the appSetgtings.json and the command line.
// =============================================================================

string? altAppSettings = null;
for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    if (arg.StartsWith("--settings", StringComparison.CurrentCultureIgnoreCase))
    {
        altAppSettings = args[i + 1];
        break;
    }
}

var appSettings = string.IsNullOrEmpty(altAppSettings) ? "appsettings.json" : altAppSettings;
if (!File.Exists(appSettings))
{
    Console.WriteLine($"Unable to find file settings file: {appSettings}");
    throw new Exception("Unable to find settings file.");
}
var configuration = new ConfigurationBuilder()
    .AddJsonFile(appSettings, optional: false)
    .AddCommandLine(args)
    .Build();
var settings = new AppSettings(configuration);
var cs = settings.ConnectionString;

// DEBUG:
Console.WriteLine($"ConnectionString: {cs}");

// =============================================================================
// Now that we have the startup parameters, let is set up the application itself
// =============================================================================

// Will probably want AutoMapper ( NuGet: AutoMapper )
builder.Services.AddAutoMapper(typeof(Program));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

// This API is using the IdentityFramework, so go ahead and configure that as
// well, note, this is all implemented in the OpenBCM.API.Authentication.dll
// library 
builder.Services.AddIdentity<User, Role>(config =>
    {
        config.Password.RequiredLength = 8;
        config.User.RequireUniqueEmail = true;
        config.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ServerContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(config =>
{
    config.Cookie.Name = "Druware.API";
    config.LoginPath = "/login/";
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// DbContext stuff
// Repeat the following for each library context added
// bear in mind, this will be different depending upon the backend, so get the 
// backend right first.
switch (settings.DbType)
{
    case DbContextType.Microsoft:
        builder.Services.AddDbContext<ServerContext>(
            conf => conf.UseSqlServer(cs));
        builder.Services.AddDbContext<ContentContext>(
            conf => conf.UseSqlServer(cs));
        
        builder.Services.AddDbContext<ServerContextMicrosoft>(
            conf => conf.UseSqlServer(cs));
        builder.Services.AddDbContext<ContentContextMicrosoft>(
            conf => conf.UseSqlServer(cs));
        break;
    case DbContextType.PostgreSql:
        builder.Services.AddDbContext<ServerContext>(
            conf => conf.UseNpgsql(cs));
        builder.Services.AddDbContext<ContentContext>(
            conf => conf.UseNpgsql(cs));
        
        builder.Services.AddDbContext<ServerContextPostgreSql>(
            conf => conf.UseNpgsql(cs));
        builder.Services.AddDbContext<ContentContextPostgreSql>(
            conf => conf.UseNpgsql(cs));
        break;
    
    case DbContextType.Sqlite:
        builder.Services.AddDbContext<ServerContext>(
            conf => conf.UseSqlite(cs));
        builder.Services.AddDbContext<ContentContext>(
            conf => conf.UseSqlite(cs));
        
        builder.Services.AddDbContext<ServerContextSqlite>(
            conf => conf.UseSqlite(cs)); 
        builder.Services.AddDbContext<ContentContextSqlite>(
            conf => conf.UseSqlite(cs));
        break;
    
    default:
        throw new ArgumentOutOfRangeException();
}

// =============================================================================
// at this point, we have every thing we need to start up the application, so 
// we can move on.
// =============================================================================

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =============================================================================
// here is where things get twitchy. If the database fails for any reason, this
// fails, and in Azure it is excepptionally difficult to get the logs at this 
// point in the startup process.  With that in mind, we need to move the 
// database migration to a seperate thread that we can check for completion
// via a 'status' controller that will let us know what is really going on 
// behind the scenes. 
//
// as a short term solution, we are actually going to simply handle, and ignore
// the exceptions during migration, and let things progress on their own.
// =============================================================================

// Run the migrations this API calls
try
{
    app.MigrateDatabase();
}
catch (Exception ex)
{
    Console.WriteLine($"Unable to migrate database: {ex.Message}");
}

app.Run();

