
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Model;

namespace Ticketing.Core.Data;

public class WebIdentityDbContext : IdentityDbContext
{

  public WebIdentityDbContext(DbContextOptions<WebIdentityDbContext> options) : base(options)
  {
  }

  public DbSet<Event> Events { get; set; } = null!;
  public DbSet<Seat> Seats { get; set; } = null!;
  public DbSet<Booking> Bookings { get; set; } = null!;
  public DbSet<BookingItem> BookingItems { get; set; } = null!;
  public DbSet<Payment> Payments { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    // Configuración de llaves primarias y relaciones explícitas
    builder.Entity<Event>().HasKey(e => e.EventId);
    builder.Entity<Seat>().HasKey(s => s.SeatId);
    builder.Entity<Booking>().HasKey(b => b.BookingId);
    builder.Entity<BookingItem>().HasKey(bi => bi.ItemId);
    builder.Entity<Payment>().HasKey(p => p.PaymentId);

    builder.Entity<Payment>()
    .Property(p => p.Amount)
    .HasColumnType("decimal(18,2)");

    builder.Entity<Seat>()
    .Property(p => p.Price)
    .HasColumnType("decimal(18,2)");

    // Índices para optimizar consultas
    builder.Entity<Seat>().HasIndex(s => new { s.EventId, s.Price }).HasDatabaseName("IX_Seats_EventId_Price");

    // Relación Evento -> Asientos (1 a muchos)
    builder.Entity<Seat>()
        .HasOne(s => s.Event)
        .WithMany(e => e.Seats)
        .HasForeignKey(s => s.EventId)
        .OnDelete(DeleteBehavior.Cascade);

    // Relación Booking -> BookingItems (1 a muchos)
    builder.Entity<BookingItem>()
        .HasOne(bi => bi.Booking)
        .WithMany(b => b.Items)
        .HasForeignKey(bi => bi.BookingId)
        .OnDelete(DeleteBehavior.Cascade);

    // Relación BookingItem -> Seat (1 a 1)
    builder.Entity<BookingItem>()
        .HasOne(bi => bi.Seat)
        .WithMany()
        .HasForeignKey(bi => bi.SeatId)
        .OnDelete(DeleteBehavior.Restrict);

    // Relación Booking -> Payment (1 a 1)
    builder.Entity<Payment>()
        .HasOne(p => p.Booking)
        .WithOne(b => b.Payment)
        .HasForeignKey<Payment>(p => p.BookingId)
        .OnDelete(DeleteBehavior.Cascade);


  }
}
