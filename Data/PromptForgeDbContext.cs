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
    public class PromptForgeDbContext : DbContext
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
        /// Harici AI sağlayıcılarının API anahtarlarını temsil eder.
        /// DbSet<ApiKey> → ApiKeys tablosu
        /// Güvenlik önemli: anahtarlar şifreli tutulur.
        /// </summary>
        public DbSet<ApiKey> ApiKeys { get; set; }

        /// <summary>
        /// Kullanıcıların API kullanım ve maliyet kayıtlarını temsil eder.
        /// DbSet<UsageTracking> → UsageTracking tablosu
        /// Token sayısı, sağlayıcı ve maliyeti takip eder.
        /// Faturalandırma ve maliyet analizi için kullanılır.
        /// </summary>
        public DbSet<UsageTracking> UsageTracking { get; set; }

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
