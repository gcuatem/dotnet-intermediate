
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Model;

namespace Ticketing.Core.Data;

public class WebIdentityDbContext : IdentityDbContext<User>
{

  public WebIdentityDbContext(DbContextOptions<WebIdentityDbContext> options) : base(options)
  {
  }

  public DbSet<Event> Events { get; set; } = null!;
  public DbSet<Venue> Venues { get; set; } = null!;
  public DbSet<Section> Sections { get; set; } = null!;
  public DbSet<Seat> Seats { get; set; } = null!;
  public DbSet<EventSeat> EventSeats { get; set; } = null!;
  public DbSet<Booking> Bookings { get; set; } = null!;
  public DbSet<BookingItem> BookingItems { get; set; } = null!;
  public DbSet<Payment> Payments { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    // Configuración de llaves primarias y relaciones explícitas
    builder.Entity<Event>().HasKey(e => e.EventId);
    builder.Entity<Venue>().HasKey(v => v.VenueId);
    builder.Entity<Section>().HasKey(s => s.SectionId);
    builder.Entity<Seat>().HasKey(s => s.SeatId);
    builder.Entity<EventSeat>().HasKey(es => es.EventSeatId);
    builder.Entity<Booking>().HasKey(b => b.BookingId);
    builder.Entity<BookingItem>().HasKey(bi => bi.ItemId);
    builder.Entity<Payment>().HasKey(p => p.PaymentId);

    builder.Entity<Payment>()
    .Property(p => p.Amount)
    .HasColumnType("decimal(18,2)");

    builder.Entity<BookingItem>()
    .Property(item => item.Price)
    .HasColumnType("decimal(18,2)");

    builder.Entity<EventSeat>()
    .Property(es => es.Price)
    .HasColumnType("decimal(18,2)");

    // Índices para optimizar consultas
    builder.Entity<EventSeat>().HasIndex(es => new { es.EventId, es.SeatId }).IsUnique();

    builder.Entity<Event>()
        .HasOne(e => e.Venue)
        .WithMany(v => v.Events)
        .HasForeignKey(e => e.VenueId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Section>()
        .HasOne(s => s.Venue)
        .WithMany(v => v.Sections)
        .HasForeignKey(s => s.VenueId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Seat>()
        .HasOne(s => s.Section)
        .WithMany(section => section.Seats)
        .HasForeignKey(s => s.SectionId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<EventSeat>()
        .HasOne(es => es.Event)
        .WithMany(e => e.EventSeats)
        .HasForeignKey(es => es.EventId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<EventSeat>()
        .HasOne(es => es.Seat)
        .WithMany(s => s.EventSeats)
        .HasForeignKey(es => es.SeatId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Booking>()
        .HasOne(b => b.User)
        .WithMany()
        .HasForeignKey(b => b.UserId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Restrict);

    // Relación Booking -> BookingItems (1 a muchos)
    builder.Entity<BookingItem>()
        .HasOne(bi => bi.Booking)
        .WithMany(b => b.Items)
        .HasForeignKey(bi => bi.BookingId)
        .OnDelete(DeleteBehavior.Cascade);

    // Relación BookingItem -> EventSeat (muchos a uno)
    builder.Entity<BookingItem>()
        .HasOne(bi => bi.EventSeat)
        .WithMany()
        .HasForeignKey(bi => bi.EventSeatId)
        .OnDelete(DeleteBehavior.Restrict);

    // Relación Booking -> Payment (1 a 1)
    builder.Entity<Payment>()
        .HasOne(p => p.Booking)
        .WithOne(b => b.Payment)
        .HasForeignKey<Payment>(p => p.BookingId)
        .OnDelete(DeleteBehavior.Cascade);


  }
}
