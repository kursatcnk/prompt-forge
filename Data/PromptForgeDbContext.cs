using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Models;

namespace PromptForge.Api.Data
{
    /// <summary>
    /// PromptForge'un Entity Framework Core DbContext'i.
    ///
    /// GÖREV: C# nesneleri ile SQL Server veritabanı arasında köprü kurmak.
    ///
    /// NASIL ÇALIŞIR:
    /// 1. DbSet<T> özelliği = Veritabanı tablosu
    ///    Örn: DbSet<User> = Users tablosuna karşılık
    ///
    /// 2. LINQ sorgusu C#'da yazılır:
    ///    var users = _context.Users.Where(u => u.IsActive).ToListAsync();
    ///
    /// 3. EF Core otomatik olarak SQL'e çevirir:
    ///    SELECT * FROM Users WHERE IsActive = 1;
    ///
    /// 4. Sonuçlar C# objeleri olarak döner.
    ///
    /// AVANTAJLAR:
    /// - SQL yazmaya gerek yok (güvenli)
    /// - Type-safe sorgular (compile-time checking)
    /// - Database-agnostic (SQL Server, PostgreSQL, SQLite'a geçişi kolay)
    /// </summary>
    public class PromptForgeDbContext : DbContext, IDataProtectionKeyContext
    {
        /// <summary>
        /// DbContext'i oluşturur. Veritabanı bağlantısı bilgisi DbContextOptions'dan gelir.
        ///
        /// Dependency Injection üzerinden kullanılır:
        /// Örn: public UserController(PromptForgeDbContext context) { _context = context; }
        /// </summary>
        public PromptForgeDbContext(DbContextOptions<PromptForgeDbContext> options) : base(options)
        {
        }

        // ===== DbSets: Veritabanı Tabloları =====

        /// <summary>
        /// Tüm kullanıcıları temsil eder.
        /// DbSet<User> → Users tablosu
        /// Kayıtlı olan, aktif ve inaktif tüm kullanıcıları içerir.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Tüm prompt optimizasyon kayıtlarını temsil eder (geçmiş).
        /// DbSet<PromptOptimization> → PromptOptimizations tablosu
        /// Her kaydı sorgulamak, filtrelemek, gruplama yapmak mümkün.
        /// </summary>
        public DbSet<PromptOptimization> PromptOptimizations { get; set; }

        /// <summary>
        /// Kullanıcıların favoriye eklediği promptları temsil eder.
        /// DbSet<FavoritePrompt> → FavoritePrompts tablosu
        /// İki tablo arasında many-to-many ilişki kurar (junction table).
        /// </summary>
        public DbSet<FavoritePrompt> FavoritePrompts { get; set; }

        /// <summary>
        /// Tüm kullanıcıların ayarlarını temsil eder.
        /// DbSet<UserSettings> → UserSettings tablosu
        /// Tema, dil, varsayılan model gibi kişisel tercihleri içerir.
        /// </summary>
        public DbSet<UserSettings> UserSettings { get; set; }

        /// <summary>
        /// Tek kullanımlık kodlar: e-posta doğrulama, şifre sıfırlama, 2 adımlı giriş bileti.
        /// DbSet<UserToken> → UserTokens tablosu
        /// </summary>
        public DbSet<UserToken> UserTokens { get; set; }

        /// <summary>
        /// Kullanıcıların API kullanım ve maliyet kayıtlarını temsil eder.
        /// DbSet<UsageTracking> → UsageTracking tablosu
        /// Token sayısı, sağlayıcı ve maliyeti takip eder.
        /// Faturalandırma ve maliyet analizi için kullanılır.
        /// </summary>
        public DbSet<UsageTracking> UsageTracking { get; set; }

        /// <summary>
        /// Data Protection şifreleme anahtarları (2FA gizli anahtarlarını çözmek için gerekir).
        /// Veritabanında durduğu için sunucu yeniden başlasa da anahtarlar kaybolmaz.
        /// </summary>
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        /// <summary>
        /// Database şemasını ve ilişkileri konfigüre eder.
        /// Entity Framework'e tablolar, indeksler ve constraints'leri söyler.
        ///
        /// YAPILANI:
        /// - Foreign key ilişkileri (1:N, 1:1)
        /// - Cascade delete (parent silinince children de silinsin)
        /// - Açık konfigürasyon (fluent API)
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Users: alan kuralları =====
            // Email zorunlu, en fazla 255 karakter ve her email sadece bir kez kayıtlı olabilir.
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.DisplayName).HasMaxLength(255);
                entity.Property(u => u.Plan).HasMaxLength(20).HasDefaultValue("free");
            });

            // ===== PromptOptimizations: kısa metin alanları =====
            // Model adı, senaryo gibi alanlar kısa değerler tutar; nvarchar(max) yerine 50 karakter yeterli.
            modelBuilder.Entity<PromptOptimization>(entity =>
            {
                entity.Property(p => p.OriginalContent).IsRequired();
                entity.Property(p => p.TargetModel).HasMaxLength(50);
                entity.Property(p => p.UseCase).HasMaxLength(50);
                entity.Property(p => p.OutputFormat).HasMaxLength(50);
                entity.Property(p => p.OptimizationTarget).HasMaxLength(50);
                entity.Property(p => p.ResponseLanguage).HasMaxLength(20);
                entity.Property(p => p.Engine).HasMaxLength(100);
                entity.HasIndex(p => p.CreatedAt);
            });

            // ===== UserTokens: tek kullanımlık kodlar =====
            // Kullanıcı silinince kodları da silinir. Kod aramaları (UserId + Purpose) üzerinden yapıldığı için index var.
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

            // ===== UsageTracking: para alanı hassasiyeti =====
            // decimal(10,4) → örn. 123456.7890 dolar. Belirtmezsek EF uyarı verir ve küsurat kaybolabilir.
            modelBuilder.Entity<UsageTracking>(entity =>
            {
                entity.Property(t => t.Provider).HasMaxLength(50);
                entity.Property(t => t.Cost).HasPrecision(10, 4);
            });

            // ===== UserSettings: kısa metin alanları =====
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
            });

            // ===== PromptOptimization → User (N:1) =====
            // Bir user birçok prompt optimization'a sahip.
            // User silinirse tüm optimizasyonları silin (cascade).
            modelBuilder.Entity<PromptOptimization>()
                .HasOne(p => p.User)
                .WithMany(u => u.PromptOptimizations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== FavoritePrompt → User (N:1) =====
            // Bir user birçok favoriye sahip.
            // User silinirse tüm favorileri silin.
            modelBuilder.Entity<FavoritePrompt>()
                .HasOne(f => f.User)
                .WithMany(u => u.FavoritePrompts)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== FavoritePrompt → PromptOptimization (N:1) =====
            // SQL Server aynı satıra iki farklı "cascade delete" yolu olmasına izin vermez
            // (User → Favori ve User → Prompt → Favori). Bu yüzden bu yolda otomatik silme kapalı;
            // bir prompt silinmeden önce favorileri kodda silinmelidir.
            modelBuilder.Entity<FavoritePrompt>()
                .HasOne(f => f.PromptOptimization)
                .WithMany()
                .HasForeignKey(f => f.PromptOptimizationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Aynı kullanıcı aynı promptu iki kez favoriye ekleyemez.
            modelBuilder.Entity<FavoritePrompt>()
                .HasIndex(f => new { f.UserId, f.PromptOptimizationId })
                .IsUnique();

            // ===== UserSettings → User (1:1) =====
            // Bir user'ın tam olarak bir Settings'i var.
            // User silinirse settings'i de silin.
            modelBuilder.Entity<UserSettings>()
                .HasOne(s => s.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== UsageTracking → User (N:1) =====
            // Bir user birçok usage kaydına sahip.
            // User silinirse tüm kullanım kayıtlarını silin.
            modelBuilder.Entity<UsageTracking>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
