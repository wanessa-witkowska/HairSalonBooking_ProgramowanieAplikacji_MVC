using HairSalonBooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HairSalonBooking.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        if (context.Database.IsSqlite())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        string[] roles = ["Admin", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@hairbooker.local";
        var adminPassword = "Admin123!";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin is null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        if (!await context.Services.AnyAsync())
        {
            var services = new List<Service>
            {
                new() { Name = "Strzyżenie damskie", Description = "Klasyczne strzyżenie damskie z konsultacją.", Price = 90, DurationMinutes = 60, Category = "Strzyżenie", IsActive = true },
                new() { Name = "Strzyżenie męskie", Description = "Strzyżenie męskie z modelowaniem.", Price = 55, DurationMinutes = 30, Category = "Strzyżenie", IsActive = true },
                new() { Name = "Koloryzacja", Description = "Koloryzacja włosów z doborem odcienia.", Price = 220, DurationMinutes = 120, Category = "Koloryzacja", IsActive = true },
                new() { Name = "Modelowanie", Description = "Modelowanie i stylizacja włosów na wyjście.", Price = 70, DurationMinutes = 45, Category = "Stylizacja", IsActive = true }
            };

            context.Services.AddRange(services);
            await context.SaveChangesAsync();
        }

        if (!await context.AvailableSlots.AnyAsync())
        {
            await SeedSlotsByDurationAsync(context);
        }
    }

    private static async Task SeedSlotsByDurationAsync(ApplicationDbContext context)
    {
        var services = await context.Services
            .Where(s => s.IsActive)
            .ToListAsync();

        var slots = new List<AvailableSlot>();

        var startDate = DateTime.Today;
        const int daysToGenerate = 30;
        const int openingHour = 9;
        const int closingHour = 21;

        foreach (var service in services)
        {
            for (int day = 0; day < daysToGenerate; day++)
            {
                var currentDate = startDate.AddDays(day);
                var dayStart = currentDate.AddHours(openingHour);
                var dayEnd = currentDate.AddHours(closingHour);

                var currentStart = dayStart;

                while (currentStart.AddMinutes(service.DurationMinutes) <= dayEnd)
                {
                    slots.Add(new AvailableSlot
                    {
                        ServiceId = service.Id,
                        StartTime = currentStart,
                        EndTime = currentStart.AddMinutes(service.DurationMinutes),
                        IsBooked = false
                    });

                    currentStart = currentStart.AddMinutes(service.DurationMinutes);
                }
            }
        }

        context.AvailableSlots.AddRange(slots);
        await context.SaveChangesAsync();
    }
}
