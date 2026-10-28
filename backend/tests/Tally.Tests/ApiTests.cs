using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Tally.Api.Data;
using Tally.Api.Dtos;

namespace Tally.Tests;

public class TallyApiFactory : WebApplicationFactory<Program>
{
    private const string TestKey = "integration-test-key-0123456789abcdef0123";
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tally-test-{Guid.NewGuid():N}.db");

    public TallyApiFactory()
    {
        // Program reads these before the host is built, so set them as env vars too
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("Jwt__Key", TestKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", TestKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

public class ApiTests(TallyApiFactory factory) : IClassFixture<TallyApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task Api_requires_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/api/expenses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, "nope-nope"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Demo_user_sees_seeded_dashboard()
    {
        var client = await SignInAsync(DbSeeder.DemoEmail, DbSeeder.DemoPassword);

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me", Json);
        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", Json);

        Assert.Contains("User", me!.Roles);
        Assert.Equal(6, dashboard!.Trend.Count);
        Assert.True(dashboard.Trend.Take(5).All(m => m.Spent > 0));
    }

    [Fact]
    public async Task Regular_user_cannot_open_admin_endpoints()
    {
        var client = await SignInAsync(DbSeeder.DemoEmail, DbSeeder.DemoPassword);
        var response = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_users()
    {
        var client = await SignInAsync(DbSeeder.AdminEmail, DbSeeder.AdminPassword);
        var users = await client.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users", Json);
        Assert.Contains(users!, u => u.Email == DbSeeder.DemoEmail && u.ExpenseCount > 0);
    }

    [Fact]
    public async Task Invalid_expense_returns_validation_errors()
    {
        var client = await SignInAsync(DbSeeder.DemoEmail, DbSeeder.DemoPassword);
        var response = await client.PostAsJsonAsync("/api/expenses", new { amount = -5, date = "2026-03-01", description = "", categoryId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Excel_export_returns_a_spreadsheet()
    {
        var client = await SignInAsync(DbSeeder.DemoEmail, DbSeeder.DemoPassword);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var response = await client.GetAsync($"/api/reports/expenses.xlsx?from={today.AddMonths(-1):yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
    }
}
