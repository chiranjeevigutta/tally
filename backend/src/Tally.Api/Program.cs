using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Tally.Api.Data;
using Tally.Api.Infrastructure;
using Tally.Api.Models;
using Tally.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// some hosts pass the port in an env variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

QuestPDF.Settings.License = LicenseType.Community;

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=tally.db";

// on Azure the db lives in /home/data, which may not exist yet
var dbFolder = Path.GetDirectoryName(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource);
if (!string.IsNullOrEmpty(dbFolder))
    Directory.CreateDirectory(dbFolder);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must be set and at least 32 characters long (set the Jwt__Key environment variable).");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = TallyClaims.Name,
            RoleClaimType = TallyClaims.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference("/api-docs", options => options.WithTitle("Tally API"));

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// anything that isn't an API route goes to the React app
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
