using HairSalonBooking.Data;
using HairSalonBooking.Models;
using HairSalonBooking.Services;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

EnsureSqliteDataDirectory(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddValidation();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailSender, IdentityEmailSenderAdapter>();


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.InitializeAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();

static void EnsureSqliteDataDirectory(string connectionString, string contentRootPath)
{
    var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);

    if (string.IsNullOrWhiteSpace(sqliteConnection.DataSource) || sqliteConnection.DataSource == ":memory:")
    {
        return;
    }

    var databasePath = sqliteConnection.DataSource;

    if (!Path.IsPathRooted(databasePath))
    {
        databasePath = Path.Combine(contentRootPath, databasePath);
    }

    var directory = Path.GetDirectoryName(databasePath);

    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

file sealed class IdentityEmailSenderAdapter : IEmailSender
{
    private readonly IEmailService _emailService;

    public IdentityEmailSenderAdapter(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return _emailService.SendAsync(email, subject, htmlMessage);
    }
}
