using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Organiser> Organisers { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<EventEnrolment> EventEnrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleID);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserID);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RoleID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Organiser configuration
        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.HasKey(e => e.OrganiserID);
            entity.Property(e => e.OrganisationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.Website).HasMaxLength(200);

            entity.HasOne(e => e.User)
                  .WithOne(u => u.Organiser)
                  .HasForeignKey<Organiser>(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserID).IsUnique();
        });

        // Participant configuration
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.ParticipantID);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);

            entity.HasOne(e => e.User)
                  .WithOne(u => u.Participant)
                  .HasForeignKey<Participant>(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserID).IsUnique();
        });

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RouteDistanceKM).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Organiser)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganiserID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntryFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Gender).HasColumnType("nchar(1)");

            entity.HasOne(e => e.Event)
                  .WithMany(ev => ev.Categories)
                  .HasForeignKey(e => e.EventID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // EventEnrolment configuration
        // Using NoAction to prevent cascade cycle errors
        modelBuilder.Entity<EventEnrolment>(entity =>
        {
            entity.HasKey(e => e.EnrolmentID);
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IsPaid).HasDefaultValue(false);

            entity.HasOne(e => e.Participant)
                  .WithMany(p => p.EventEnrolments)
                  .HasForeignKey(e => e.ParticipantID)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.EventEnrolments)
                  .HasForeignKey(e => e.CategoryID)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => new { e.ParticipantID, e.CategoryID }).IsUnique();
        });

        // Result configuration
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.ResultID);
            entity.Property(e => e.Position).IsRequired();

            entity.HasOne(e => e.EventEnrolment)
                  .WithOne(ee => ee.Result)
                  .HasForeignKey<Result>(e => e.EnrolmentID)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.EnrolmentID).IsUnique();
        });

        // Seed initial roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" }
        );
    }
}