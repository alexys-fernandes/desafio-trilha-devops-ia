using HabitApp.Domain.Entities;
using HabitApp.Infrastructure.Data.Utils;
using Microsoft.EntityFrameworkCore;

namespace HabitApp.Infrastructure.Data.Context;

public class SqliteContext(DbContextOptions<SqliteContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitCompletion> HabitCompletions { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Icon).IsRequired();
            entity.Property(e => e.Color).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.RecurrenceType).IsRequired();
            entity.Property(e => e.ReminderTimezone).IsRequired();
            entity.Property(e => e.ReminderType).IsRequired();
            entity.HasOne(h => h.User)
                  .WithMany(u => u.Habits)
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitCompletion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Habit)
                  .WithMany(h => h.Completions)
                  .HasForeignKey(e => e.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.HabitId, e.UserId, e.CompletedDate })
                  .IsUnique()
                  .HasFilter("\"IsDeleted\" = 0");
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DefaultReminderType).IsRequired();
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<UserNotificationPreference>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<User>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<Habit>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<HabitCompletion>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<UserNotificationPreference>().HasQueryFilter(h => !h.IsDeleted);
    }

    public override int SaveChanges()
    {
        ConfigureDates();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConfigureDates();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ConfigureDates()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var brazilDatetime = DateTimeUtils.GetHorarioBrasilia();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = brazilDatetime;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.ModifiedAt = brazilDatetime;
            }
        }
    }
}
