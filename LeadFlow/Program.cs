using LeadFlow.Application.Leads;
using LeadFlow.Domain.Entities;
using LeadFlow.Infrastructure.Identity;
using LeadFlow.Infrastructure.Persistence;
using LeadFlow.Infrastructure.Telegram;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<LeadFlowOptions>(builder.Configuration.GetSection(LeadFlowOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ILeadCreationService, LeadCreationService>();
builder.Services.AddScoped<ILeadListQueryService, LeadListQueryService>();
builder.Services.AddLeadNotifications(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var initialManagerEmail = builder.Configuration["InitialManager:Email"];
var initialManagerPassword = builder.Configuration["InitialManager:Password"];
InitialManagerConfiguration.Validate(initialManagerEmail, initialManagerPassword, connectionString);

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
}
builder.Services.Configure<InitialManagerOptions>(builder.Configuration.GetSection(InitialManagerOptions.SectionName));
builder.Services.AddScoped<InitialManagerInitializer>();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<InitialManagerInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

public partial class Program;
