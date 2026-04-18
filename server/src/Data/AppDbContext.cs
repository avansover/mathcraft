using Mathcraft.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FamilyAccount> FamilyAccounts => Set<FamilyAccount>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FamilyAccount>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Email).IsRequired().HasMaxLength(255);
            e.HasIndex(a => a.Email).IsUnique();
            e.Property(a => a.PasswordHash).IsRequired();

            e.HasMany(a => a.PlayerProfiles)
                .WithOne(p => p.FamilyAccount)
                .HasForeignKey(p => p.FamilyAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(a => a.RefreshTokens)
                .WithOne(t => t.FamilyAccount)
                .HasForeignKey(t => t.FamilyAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(a => a.PasswordResetTokens)
                .WithOne(t => t.FamilyAccount)
                .HasForeignKey(t => t.FamilyAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.TokenHash).IsRequired();
        });

        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.TokenHash).IsRequired();
        });
    }
}
