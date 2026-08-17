using ErzurumFlight.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ErzurumFlight.Server.Data;

/// <summary>
/// SQLite üzerinde çalışan tek EF Core DbContext'i. İlk sürümde ayrı bir repository katmanı
/// veya CQRS altyapısı kullanılmaz; servisler doğrudan bu context üzerinden çalışır.
/// IdentityDbContext, Admin girişi için gerekli Identity tablolarını da (AspNetUsers vb.) ekler.
/// </summary>
public class FlightDbContext : IdentityDbContext<ApplicationUser>
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options)
    {
    }

    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<FlightSchedule> FlightSchedules => Set<FlightSchedule>();
    public DbSet<FlightInstance> FlightInstances => Set<FlightInstance>();
    public DbSet<FlightOperation> FlightOperations => Set<FlightOperation>();
    public DbSet<AircraftPosition> AircraftPositions => Set<AircraftPosition>();
    public DbSet<DataSource> DataSources => Set<DataSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Airport>(e =>
        {
            e.HasIndex(x => x.IataCode).IsUnique();
            e.HasIndex(x => x.IcaoCode).IsUnique();
        });

        modelBuilder.Entity<Airline>(e =>
        {
            e.HasIndex(x => x.IcaoCode);
        });

        modelBuilder.Entity<Aircraft>(e =>
        {
            e.HasIndex(x => x.IcaoHex).IsUnique();
        });

        modelBuilder.Entity<FlightSchedule>(e =>
        {
            e.HasOne(x => x.OriginAirport)
                .WithMany(a => a.DepartureSchedules)
                .HasForeignKey(x => x.OriginAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.DestinationAirport)
                .WithMany(a => a.ArrivalSchedules)
                .HasForeignKey(x => x.DestinationAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Airline)
                .WithMany(a => a.Schedules)
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Source)
                .WithMany()
                .HasForeignKey(x => x.SourceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.OriginAirportId, x.DestinationAirportId, x.IsActive });
        });

        modelBuilder.Entity<FlightInstance>(e =>
        {
            // Şartname 6. bölüm: FlightDate + FlightNumber + OriginAirportId + DestinationAirportId tekil olmalı.
            e.HasIndex(x => new { x.FlightDate, x.FlightNumber, x.OriginAirportId, x.DestinationAirportId }).IsUnique();

            e.HasOne(x => x.FlightSchedule)
                .WithMany(s => s.Instances)
                .HasForeignKey(x => x.FlightScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Airline)
                .WithMany()
                .HasForeignKey(x => x.AirlineId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.OriginAirport)
                .WithMany()
                .HasForeignKey(x => x.OriginAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.DestinationAirport)
                .WithMany()
                .HasForeignKey(x => x.DestinationAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Source)
                .WithMany()
                .HasForeignKey(x => x.SourceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.ScheduledDepartureUtc);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<FlightOperation>(e =>
        {
            e.HasIndex(x => x.FlightInstanceId).IsUnique();

            e.HasOne(x => x.FlightInstance)
                .WithOne(i => i.Operation)
                .HasForeignKey<FlightOperation>(x => x.FlightInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Aircraft)
                .WithMany(a => a.Operations)
                .HasForeignKey(x => x.AircraftId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AircraftPosition>(e =>
        {
            e.HasOne(x => x.FlightOperation)
                .WithMany(o => o.Positions)
                .HasForeignKey(x => x.FlightOperationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Source)
                .WithMany()
                .HasForeignKey(x => x.SourceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.FlightOperationId, x.TimestampUtc });
        });

        modelBuilder.Entity<DataSource>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });
    }
}
