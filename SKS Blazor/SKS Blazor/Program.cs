using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using System.Threading.RateLimiting;

using SKS_Blazor.Infrastructure;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.Engines;
using SKS_Blazor.Infrastructure.Helpers;
using SKS_Blazor.Infrastructure.HealthChecks;
using SKS_Blazor.Infrastructure.Hubs;
using SKS_Blazor.Infrastructure.Logging;
using SKS_Blazor.Infrastructure.Security;
using SKS_Blazor.Middleware;

using SKS_Blazor.Application.Services.About;
using SKS_Blazor.Application.Services.Action;
using SKS_Blazor.Application.Services.Adjust;
using SKS_Blazor.Application.Services.Customers;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Login;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Application.Services.Product;
using SKS_Blazor.Application.Services.Products;
using SKS_Blazor.Application.Services.Providers;
using SKS_Blazor.Application.Services.Reception;
using SKS_Blazor.Application.Services.Request;
using SKS_Blazor.Application.Services.Search;
using SKS_Blazor.Application.Services.Stock;
using SKS_Blazor.Application.Services.Users;

using SKS_Blazor.Pages.About;
using SKS_Blazor.Pages.Action;
using SKS_Blazor.Pages.Adjust;
using SKS_Blazor.Pages.Customers;
using SKS_Blazor.Pages.Login;
using SKS_Blazor.Pages.Main;
using SKS_Blazor.Pages.Order;
using SKS_Blazor.Pages.Product;
using SKS_Blazor.Pages.Products;
using SKS_Blazor.Pages.Providers;
using SKS_Blazor.Pages.Reception;
using SKS_Blazor.Pages.Request;
using SKS_Blazor.Pages.Search;
using SKS_Blazor.Pages.Splash;
using SKS_Blazor.Pages.Stock;
using SKS_Blazor.Pages.Users;

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

// ----- Database (existing SQLite Orders.db from the legacy app — database-first, no migrations) -----
// CRITICAL: this is the ONLY DbContext registration for UpgradedDbContext.
// Every generated service injects IDbContextFactory<UpgradedDbContext> and calls
// CreateDbContextAsync() — never the bare context type — so AddDbContextFactory alone
// is sufficient and avoids the "scoped service consumed by singleton" ValidateOnBuild failure.
// A relative Data Source resolves against the content root, and Mode=ReadWrite fails fast when the
// file is missing (the default ReadWriteCreate would silently create an empty database).
var sqlite = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing."));
if (!Path.IsPathRooted(sqlite.DataSource))
    sqlite.DataSource = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource));
sqlite.Mode = SqliteOpenMode.ReadWrite;
var connectionString = sqlite.ToString();
builder.Services.AddDbContextFactory<UpgradedDbContext>(options =>
    options.UseSqlite(connectionString, o => o.CommandTimeout(120)));

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

// ----- Application state (per-user mutable state — MUST be Scoped, never Singleton) -----
builder.Services.AddScoped<AppState>();

// ----- Infrastructure helper / engine classes -----
builder.Services.AddScoped<AuditLogger>();
builder.Services.AddScoped<ReportExportHelper>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<StockLevelEngine>();

// ----- Application services (one per domain, per plan's service map) -----
builder.Services.AddScoped<IAboutService, AboutService>();
builder.Services.AddScoped<IActionOrderReceptionService, ActionOrderReceptionService>();
builder.Services.AddScoped<IActionOrderRequestService, ActionOrderRequestService>();
builder.Services.AddScoped<IAddProductToService, AddProductToService>();
builder.Services.AddScoped<IAddStockManualService, AddStockManualService>();
builder.Services.AddScoped<IAdjustStockManualService, AdjustStockManualService>();
builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<IFunctionsService, FunctionsService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IOrderReceptionService, OrderReceptionService>();
builder.Services.AddScoped<IOrderRequestService, OrderRequestService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IProvidersService, ProvidersService>();
builder.Services.AddScoped<IReceptionApprovalService, ReceptionApprovalService>();
builder.Services.AddScoped<IRequestApprovalService, RequestApprovalService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IUsersManageService, UsersManageService>();

// ----- Page / dialog components resolvable via DI -----
builder.Services.AddTransient<AboutDialog>();
builder.Services.AddTransient<ActionOrderReception>();
builder.Services.AddTransient<ActionOrderRequest>();
builder.Services.AddTransient<AddProductTo>();
builder.Services.AddTransient<AddStockManual>();
builder.Services.AddTransient<AdjustStockManual>();
builder.Services.AddTransient<Customers>();
builder.Services.AddTransient<LoginDialog>();
builder.Services.AddTransient<Main>();
builder.Services.AddTransient<OrderReception>();
builder.Services.AddTransient<OrderRequest>();
builder.Services.AddTransient<Products>();
builder.Services.AddTransient<Providers>();
builder.Services.AddTransient<ReceptionApproval>();
builder.Services.AddTransient<RequestApproval>();
builder.Services.AddTransient<Search>();
builder.Services.AddTransient<SplashDialog>();
builder.Services.AddTransient<UsersManage>();

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
