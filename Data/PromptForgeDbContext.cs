using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Models;

namespace PromptForge.Api.Data
{
    public class PromptForgeDbContext : DbContext, IDataProtectionKeyContext
    {
        public PromptForgeDbContext(DbContextOptions<PromptForgeDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PromptOptimization> PromptOptimizations { get; set; }
        public DbSet<FavoritePrompt> FavoritePrompts { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<UsageTracking> UsageTracking { get; set; }

        // Data Protection anahtarları (2FA secret'larını çözmek için). DB'de olunca sunucu değişse de kaybolmuyor.
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.DisplayName).HasMaxLength(255);
                entity.Property(u => u.Plan).HasMaxLength(20).HasDefaultValue("free");
            });

            // Kısa kod değerleri; nvarchar(max) gereksiz.
            modelBuilder.Entity<PromptOptimization>(entity =>
            {
                entity.Property(p => p.OriginalContent).IsRequired();
                entity.Property(p => p.TargetModel).HasMaxLength(50);
                entity.Property(p => p.UseCase).HasMaxLength(50);
                entity.Property(p => p.OutputFormat).HasMaxLength(50);
                entity.Property(p => p.OptimizationTarget).HasMaxLength(50);
                entity.Property(p => p.ResponseLanguage).HasMaxLength(20);
                entity.Property(p => p.Engine).HasMaxLength(100);
                // Geçmiş hep "bu kullanıcının kayıtları, en yeni önce" diye okunuyor.
                entity.HasIndex(p => new { p.UserId, p.CreatedAt });
            });

            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.Property(t => t.Purpose).IsRequired().HasMaxLength(30);
                entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(t => new { t.UserId, t.Purpose });
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UsageTracking>(entity =>
            {
                entity.Property(t => t.Provider).HasMaxLength(50);
                // Belirtmeyince EF uyarı veriyor ve küsurat kırpılabiliyor.
                entity.Property(t => t.Cost).HasPrecision(10, 4);
                // Kota her istekte "bu kullanıcının bu ayki satırları" diye sayılıyor.
                entity.HasIndex(t => new { t.UserId, t.Date });
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.Property(s => s.DefaultModel).HasMaxLength(50);
                entity.Property(s => s.DefaultOptimizationTarget).HasMaxLength(50);
                entity.Property(s => s.Theme).HasMaxLength(50);
                entity.Property(s => s.Density).HasMaxLength(50);
                entity.Property(s => s.Language).HasMaxLength(10);
                entity.Property(s => s.UseCase).HasMaxLength(50);
                entity.Property(s => s.ResponseFormat).HasMaxLength(50);
                entity.Property(s => s.ResponseLanguage).HasMaxLength(20);
                entity.HasOne(s => s.User)
                    .WithOne(u => u.Settings)
                    .HasForeignKey<UserSettings>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Kullanıcı silinince geçmişi, favorileri, ayarları, kodları ve kullanım kayıtları da gidiyor.
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

            // SQL Server aynı satıra iki cascade yolu olmasına izin vermiyor (User → Favori ve User → Prompt → Favori).
            // Bu yol NoAction; prompt silmeden önce favorisini kodda siliyorum (PromptLibraryService).
            modelBuilder.Entity<FavoritePrompt>()
                .HasOne(f => f.PromptOptimization)
                .WithMany()
                .HasForeignKey(f => f.PromptOptimizationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FavoritePrompt>()
                .HasIndex(f => new { f.UserId, f.PromptOptimizationId })
                .IsUnique();
        }
    }
}
