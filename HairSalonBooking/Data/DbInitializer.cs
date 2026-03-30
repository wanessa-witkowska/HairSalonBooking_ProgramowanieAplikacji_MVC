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

        await context.Database.MigrateAsync();

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
            var services = await context.Services.ToListAsync();
            var slots = new List<AvailableSlot>();
            var baseDate = DateTime.Today.AddDays(1);

            foreach (var service in services)
            {
                for (int day = 0; day < 7; day++)
                {
                    var currentDay = baseDate.AddDays(day);

                    var hours = new[] { 9, 11, 13, 15, 17 };

                    foreach (var hour in hours)
                    {
                        var start = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, hour, 0, 0);
                        var end = start.AddMinutes(service.DurationMinutes);

                        slots.Add(new AvailableSlot
                        {
                            ServiceId = service.Id,
                            StartTime = start,
                            EndTime = end,
                            IsBooked = false
                        });
                    }
                }
            }

            context.AvailableSlots.AddRange(slots);
            await context.SaveChangesAsync();
        }
    }
}