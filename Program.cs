using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PromptForge.Api.Services;

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
builder.Services.AddSwaggerGen(options =>
{
    // Swagger sayfasına "Authorize" butonu ekler: token'ı bir kez yapıştırırsın,
    // kilitli endpoint'leri tarayıcıdan test ederken otomatik gönderilir.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login'den aldığın token'ı yapıştır (başına 'Bearer' yazma)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

/// ===== KİMLİK DOĞRULAMA (JWT) =====

/// Gelen isteklerdeki "Authorization: Bearer {token}" başlığını otomatik kontrol eder.
/// Token'ı üretirken kullandığımız ayarların aynısıyla doğrular (UserService.GenerateJwtToken):
/// - İmza bizim gizli anahtarımızla mı atılmış? (sahte token'ı engeller)
/// - Issuer/Audience doğru mu? (başka bir uygulamanın token'ını engeller)
/// - Süresi dolmuş mu?
/// [Authorize] etiketi olan endpoint'ler geçerli token yoksa 401 döner.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "")),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

/// ===== 2. VERİTABANI BAĞLANTISI =====

/// appsettings.json'dan connection string'i oku.
/// Örn: "Server=localhost;Database=PromptForge;..."
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

/// Entity Framework Core'u SQL Server ile ekle.
/// DbContext'i dependency injection container'ına kaydet.
/// Daha sonra controllers'da şu şekilde kullanılır:
/// public UserController(PromptForgeDbContext context) => _context = context;
builder.Services.AddDbContext<PromptForgeDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5)));

/// ===== SERVIS KATMANI =====

/// UserService'i ekle (Authentication işlemleri).
/// IUserService interface'ini uygulamak için UserService sınıfını kaydet.
/// Şu şekilde çağırılır: public AuthController(IUserService userService) => _userService = userService;
builder.Services.AddScoped<IUserService, UserService>();

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

/// wwwroot klasöründeki arayüz dosyalarını (HTML, CSS, JS) sun.
/// UseDefaultFiles: http://localhost:5299/ adresine gelince index.html'i açar.
/// UseStaticFiles: /app.html, /auth/sign-in.html gibi dosyaları olduğu gibi gönderir.
/// Böylece site ve API aynı adreste çalışır.
app.UseDefaultFiles();
app.UseStaticFiles();

/// Önceki CORS politikasını uygula.
/// Frontend'in API'ye erişebilmesini sağla.
app.UseCors("AllowAll");

/// Önce "sen kimsin?" (Authentication: token'ı oku ve doğrula),
/// sonra "buna yetkin var mı?" (Authorization: [Authorize] kuralını uygula).
/// Sıra önemli: kim olduğunu bilmeden yetkiyi kontrol edemeyiz.
app.UseAuthentication();
app.UseAuthorization();

/// Controller'ları route'la.
/// Örn: GET /api/users → UsersController.GetUsers()
app.MapControllers();

// ===== 6. API'YI BAŞLAT =====

/// Uygulama çalışmaya başlar ve HTTP istekleri bekler.
/// Durdur: Ctrl+C
app.Run();
