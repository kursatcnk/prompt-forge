using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Models;

namespace PromptForge.Api.Data
{
    public class PromptForgeDbContext : DbContext
    {
        public PromptForgeDbContext(DbContextOptions<PromptForgeDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PromptOptimization> PromptOptimizations { get; set; }
        public DbSet<FavoritePrompt> FavoritePrompts { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<UsageTracking> UsageTracking { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PromptOptimization>()
                .HasOne(p => p.User)
                .WithMany(u => u.PromptOptimizations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoritePrompt>()
                .HasOne(f => f.User)
                .WithMany(u => u.FavoritePrompts)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSettings>()
                .HasOne(s => s.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsageTracking>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
