using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.HealthChecks;
using SKS_Blazor.Infrastructure.Hubs;
using SKS_Blazor.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ----- Serilog bootstrap -----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ----- MVC / Razor Pages / Blazor Server -----
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddLocalization();
builder.Services.AddAntiforgery();

// ----- Database (existing SQL Server database — database-first, no migrations) -----
// CRITICAL: this is the ONLY DbContext registration for UpgradedDbContext.
// Every generated service injects IDbContextFactory<UpgradedDbContext> and calls
// CreateDbContextAsync() — never the bare context type — so AddDbContextFactory alone
// is sufficient and avoids the "scoped service consumed by singleton" ValidateOnBuild failure.
builder.Services.AddDbContextFactory<UpgradedDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- Health checks (custom check resolves IDbContextFactory<T>, not the bare context) -----
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// ----- Rate limiting (global limiter pattern — no named-policy overload package needed) -----
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseAntiforgery();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.MapHealthChecks("/health");
app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<StockNotificationHub>("/hubs/stock");
app.MapHub<OrderWorkflowNotificationHub>("/hubs/orderworkflow");
app.MapFallbackToPage("/_Host");

app.Run();

