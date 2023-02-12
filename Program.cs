using System.Reflection;
using System.Text.Json.Serialization;
using Druware.API;
using Druware.Server;
using Druware.Server.Entities;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// dotnet ef migrations add Init

const string connectionString = "Host=localhost;Database=druware;Username=postgres;Password=gr8orthan0";

var builder = WebApplication.CreateBuilder(args);

// Some Custom Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Will probably want AutoMapper ( NuGet: AutoMapper )
builder.Services.AddAutoMapper(typeof(Program));

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

// DbContext stuff
// Repeat the following for each library context added

builder.Services.AddDbContext<Druware.Server.ServerContext>(conf =>
{
    conf.UseNpgsql(connectionString);
});
builder.Services.AddDbContext<Druware.Server.Content.ContentContext>(conf =>
{
    conf.UseNpgsql(connectionString);
});


// This API is using the IdentityFramework, so go ahead and configure that as
// well, note, this is all implemented in the OpenBCM.API.Authentication.dll
// library

builder.Services.AddIdentity<User, Role>(config =>
{
    config.Password.RequiredLength = 8;
    config.User.RequireUniqueEmail = true;
    config.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<Druware.Server.ServerContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(config =>
{
    config.Cookie.Name = "Druware.API";
    config.LoginPath = "/login/";
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

