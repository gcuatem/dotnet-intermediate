
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Data;
using Ticket.WebApp.Data;
using Ticketing.Core.Data;
using Ticketing.Core.Model;
using Ticketing.Core.Repositories;

namespace Ticketing.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers()
      .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("The DefaultConnection connection string is missing.");
    builder.Services.AddScoped<IDbConnection>(_ =>
    {
      var connection = new SqlConnection(connectionString);
      connection.Open();
      return connection;
    });
    builder.Services.AddScoped<IVenueRepository, VenueRepository>();
    builder.Services.AddScoped<ISectionRepository, SectionRepository>();
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<IBookingRepository, BookingRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

    builder.Services.AddDbContext<WebIdentityDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
      options.Password.RequireDigit = false;
      options.Password.RequireLowercase = false;
      options.Password.RequireUppercase = false;
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequiredLength = 1;
      options.User.RequireUniqueEmail = true;
    }).AddEntityFrameworkStores<WebIdentityDbContext>();


    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
      await IdentitySeeder.SeedAsync(scope.ServiceProvider);
    }


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.MapOpenApi();
      app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();


    app.MapControllers();

    app.Run();
  }
}
