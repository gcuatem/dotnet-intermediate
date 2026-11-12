using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Model;
using Ticketing.Core.Repositories;

namespace Ticketing.WebApp
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

      // Add services to the container.
      builder.Services.AddRazorPages();
      builder.Services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));
      builder.Services.AddScoped<IEventRepository, EventRepository>();
      builder.Services.AddScoped<IBookingRepository, BookingRepository>();
      builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
      builder.Services.AddDbContext<WebIdentityDbContext>(options =>
        options.UseSqlServer(connectionString,
        sqlServerOptionsAction: sqlOptions =>
        {
          sqlOptions.MigrationsAssembly("Ticketing.WebApp");
        }
        ));

      builder.Services.AddIdentity<User, IdentityRole>(options =>
      {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 1;
        options.User.RequireUniqueEmail = true;
      }).AddEntityFrameworkStores<WebIdentityDbContext>();

      builder.Services.ConfigureApplicationCookie(options =>
      {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
      });

      builder.Services.AddAuthorization();

      var app = builder.Build();
      //using (var scope = app.Services.CreateScope())
      //{
      //  await IdentitySeeder.SeedAsync(scope.ServiceProvider);
      //}

      // Configure the HTTP request pipeline.
      if (!app.Environment.IsDevelopment())
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();

      app.UseRouting();
      app.UseAuthentication();
      app.UseAuthorization();

      app.MapStaticAssets();
      app.MapRazorPages()
         .WithStaticAssets();

      app.Run();
    }
  }
}
