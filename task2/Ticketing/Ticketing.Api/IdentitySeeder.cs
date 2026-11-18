using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Model;

namespace Ticket.WebApp.Data
{
  public static class IdentitySeeder
  {
    public static async Task SeedAsync(IServiceProvider services)
    {
      var db = services.GetRequiredService<WebIdentityDbContext>();
      db.Database.Migrate();

      var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
      var userManager = services.GetRequiredService<UserManager<User>>();

      string[] roles = ["EventManager", "Customer"];

      foreach (var roleName in roles)
      {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
          await roleManager.CreateAsync(new IdentityRole(roleName));
        }
      }

      var seedUsers = new[]
      {
        new { Role = "EventManager", UserName = "event1", Email = "event1@gmail.com", Password = "event1", FirstName="Jerry",LastName="McGuirre" ,Address="Home",IdentificationNumber="888"},
        new { Role = "Customer", UserName = "client1", Email = "client1@gmail.com", Password = "client1",FirstName="Bruce",LastName="Wayne" ,Address="Gotham",IdentificationNumber="777" }
      };

      foreach (var seedUser in seedUsers)
      {
        var user = await userManager.FindByNameAsync(seedUser.UserName);

        if (user is null)
        {
          user = new User
          {
            UserName = seedUser.UserName,
            Email = seedUser.Email,
            EmailConfirmed = true,
            FirstName = seedUser.FirstName,
            LastName = seedUser.LastName,
            Address = seedUser.Address,
            IdentificationNumber = seedUser.IdentificationNumber
          };

          var result = await userManager.CreateAsync(user, seedUser.Password);
          if (!result.Succeeded)
          {
            throw new InvalidOperationException($"Failed to create default user '{seedUser.UserName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
          }
        }

        if (!await userManager.IsInRoleAsync(user, seedUser.Role))
        {
          await userManager.AddToRoleAsync(user, seedUser.Role);
        }
      }
    }
  }
}
