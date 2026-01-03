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


// WOW I am an idiot. Because this configuraiton is used elsewhere, this MUST 
// be handling using the configuration manager, and inside the AppSettings itself

Console.WriteLine($"Checking Command Line Args:");

string? altAppSettings = null;
for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    Console.WriteLine($"Arg: {arg}");
    if (arg.StartsWith("--settings", StringComparison.CurrentCultureIgnoreCase))
    {
        altAppSettings = args[i + 1];
        // Console.WriteLine($"Found Settings Arg: {altAppSettings}");
        break;
    }
}
var appSettings = string.IsNullOrEmpty(altAppSettings) ? "appsettings.json" : altAppSettings;
Console.WriteLine($"Checking for settings file: {appSettings}");
if (!File.Exists(appSettings))
{
    Console.WriteLine($"Unable to find file settings file: {appSettings}");
    throw new Exception("Unable to find settings file.");
}

// Some Custom Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile(appSettings, optional: false)
    .AddCommandLine(args)
    .Build();

var settings = new AppSettings(configuration);
var cs = settings.ConnectionString;

// Will probably want AutoMapper ( NuGet: AutoMapper )
builder.Services.AddAutoMapper(typeof(Program));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

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
//builder.Services.AddSwaggerGen();

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

// Run the migrations this API calls
app.MigrateDatabase();

app.Run();

