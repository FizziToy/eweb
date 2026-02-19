using eweb.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eweb.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = configuration["SeedAdmin:Email"]
        ?? throw new Exception("SeedAdmin:Email not configured");

        var adminPassword = configuration["SeedAdmin:Password"]
            ?? throw new Exception("SeedAdmin:Password not configured");

        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
            await roleManager.CreateAsync(new IdentityRole(RoleNames.Admin));

        if (!await roleManager.RoleExistsAsync(RoleNames.User))
            await roleManager.CreateAsync(new IdentityRole(RoleNames.User));

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, adminPassword);
            await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
        }
    }
}