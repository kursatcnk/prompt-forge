using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;

/// ====================================================================
/// PromptForge ASP.NET Core Web API - Uygulama Başlangıç Noktası
/// ====================================================================
///
/// GÖREV: Backend API'ni konfigüre etmek ve başlatmak.
///
/// AKIŞ:
/// 1. WebApplication.CreateBuilder() → Services ekle
/// 2. app.Build() → Middleware'i konfigüre et
/// 3. app.Run() → API'yi başlat ve HTTP istekleri bekle
///
/// ====================================================================

var builder = WebApplication.CreateBuilder(args);

// ===== 1. SERVISLER EKLEME (Dependency Injection) =====

/// Denetleyiciler (Controllers) eklenir.
/// Controllers HTTP istekleri alıp cevap verir.
builder.Services.AddControllers();

/// Swagger/OpenAPI desteği eklenir.
/// /swagger adresinde API dokümantasyonu gösterilir.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/// ===== 2. VERİTABANI BAĞLANTISI =====

/// appsettings.json'dan connection string'i oku.
/// Örn: "Server=localhost;Database=PromptForge;..."
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

/// Entity Framework Core'u SQL Server ile ekle.
/// DbContext'i dependency injection container'ına kaydet.
/// Daha sonra controllers'da şu şekilde kullanılır:
/// public UserController(PromptForgeDbContext context) => _context = context;
builder.Services.AddDbContext<PromptForgeDbContext>(options =>
    options.UseSqlServer(connectionString));

/// ===== 3. CORS (Cross-Origin Resource Sharing) =====

/// Frontend (app.html) backend API'ye erişebilmesi için CORS açılır.
///
/// NEDEN GEREKLİ?
/// Frontend: http://localhost:3000 (tarayıcıda)
/// Backend: http://localhost:5000 (API)
/// Tarayıcı güvenlik sebebiyle farklı domain'den istek reddeder.
/// CORS bunu kontrol eder.
///
/// "AllowAll" politikası:
/// - Herhangi bir kaynaktan istek kabul et (geliştirme aşamasında).
/// - Production'da daha restrictive olmalıdır.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        /// Tüm kaynaktan (origin) istek kabul et.
        policy.AllowAnyOrigin()
              /// Tüm HTTP metodlarını (GET, POST, PUT, DELETE) kabul et.
              .AllowAnyMethod()
              /// Tüm header'ları kabul et (Authorization, Content-Type vb).
              .AllowAnyHeader();
    });
});

// ===== 4. UYGULAMAYI OLUŞTUR =====

var app = builder.Build();

// ===== 5. MIDDLEWARE PIPELINE (İstek Sırası) =====

/// AÇIKLAMA: Gelen HTTP istekleri bu sırayla işlenir:
/// 1. Swagger → 2. HTTPS → 3. CORS → 4. Auth → 5. Controller
///
/// Her middleware bir işlev yapar, sonrakine geçer.

/// Development ortamında Swagger UI'ı etkinleştir.
/// http://localhost:5000/swagger
/// API'nin tüm endpoint'lerini gösterir.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/// HTTP isteklerini HTTPS'ye yönlendir (SSL/TLS şifreleme).
/// Production'da önemlidir.
app.UseHttpsRedirection();

/// Önceki CORS politikasını uygula.
/// Frontend'in API'ye erişebilmesini sağla.
app.UseCors("AllowAll");

/// Kimlik doğrulama middleware'i (JWT, cookies vb).
/// Şu an aktif değil; daha sonra auth controller'ı yazılacak.
app.UseAuthorization();

/// Controller'ları route'la.
/// Örn: GET /api/users → UsersController.GetUsers()
app.MapControllers();

// ===== 6. API'YI BAŞLAT =====

/// Uygulama çalışmaya başlar ve HTTP istekleri bekler.
/// Durdur: Ctrl+C
app.Run();
