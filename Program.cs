using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PromptForge.Api.Data;
using PromptForge.Api.Services;
using PromptForge.Api.Services.Ai;

/// ====================================================================
/// PromptForge ASP.NET Core Web API - Uygulama Başlangıç Noktası
/// ====================================================================
///
/// GÖREV: Servisleri kaydetmek, istek sırasını (middleware) kurmak ve uygulamayı başlatmak.
///
/// AKIŞ:
/// 1. WebApplication.CreateBuilder() → Servisleri ekle (Dependency Injection)
/// 2. app.Build() → Middleware sırasını kur
/// 3. app.Run() → Uygulamayı başlat ve HTTP isteklerini bekle
///
/// ====================================================================

var builder = WebApplication.CreateBuilder(args);

// Canlı ortamda örnek JWT anahtarıyla çalışmayı engelle: bu anahtar GitHub'da herkese açık.
const string SampleJwtSecret = "your-super-secret-key-minimum-32-characters-long-here-12345678";
if (!builder.Environment.IsDevelopment() && builder.Configuration["Jwt:Secret"] == SampleJwtSecret)
    throw new InvalidOperationException("Jwt:Secret canlı ortamda değiştirilmeli (user secrets veya ortam değişkeni ile).");

// ===== 1. API VE SWAGGER =====

builder.Services.AddControllers();
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

// ===== 2. KİMLİK DOĞRULAMA (JWT) =====

/// Gelen isteklerdeki "Authorization: Bearer {token}" başlığını otomatik kontrol eder:
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "")),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ===== 3. RATE LIMITING (deneme sınırı) =====

/// Giriş, kayıt ve şifre sıfırlama endpoint'lerinde aynı IP'den dakikada en fazla 10 istek.
/// Şifre tahmin (brute force) ve e-posta bombardımanı saldırılarını yavaşlatır. Aşılırsa 429 döner.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// ===== 4. VERİTABANI =====

/// appsettings.json'daki bağlantı adresiyle EF Core'u SQL Server'a bağla.
/// EnableRetryOnFailure: anlık bağlantı kopmalarında sorguyu 5 kez tekrar dener.
builder.Services.AddDbContext<PromptForgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 5)));

/// Data Protection: 2FA gizli anahtarlarını veritabanına şifreli yazmak için kullanılır.
/// Şifreleme anahtarları proje klasörü dışında, Windows kullanıcı profilinde saklanır.
builder.Services.AddDataProtection();

// ===== 5. SERVİS KATMANI =====

/// AddScoped: Her HTTP isteği için servisin yeni bir kopyası oluşur (DbContext ile aynı ömür).
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PromptLibraryService>();
builder.Services.AddScoped<UsageService>();
builder.Services.AddScoped<OneTimeCodeService>();
builder.Services.AddSingleton<TwoFactorService>();

/// E-posta: SMTP ayarı varsa gerçek e-posta, yoksa geliştirme modu (içerik konsola yazılır).
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]))
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

// ===== 6. AI SAĞLAYICILARI =====

/// Tüm sağlayıcılar aynı arayüzle (IAiProvider) kaydedilir; PromptOptimizerService hepsini liste olarak alır
/// ve anahtarı tanımlı olanı seçer. Anahtarlar kodda değil user secrets'ta durur (bkz. README).
builder.Services.AddHttpClient("ai", client => client.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<IAiProvider, AnthropicProvider>();
builder.Services.AddSingleton<IAiProvider, OpenAiProvider>();
builder.Services.AddSingleton<IAiProvider, GeminiProvider>();
builder.Services.AddSingleton<IAiProvider, DeepSeekProvider>();
builder.Services.AddSingleton<PromptOptimizerService>();
builder.Services.AddSingleton<PromptAnalysisService>();

// ===== 7. UYGULAMAYI OLUŞTUR =====

var app = builder.Build();

// ===== 8. MIDDLEWARE SIRASI =====

/// Gelen her istek yukarıdan aşağıya bu adımlardan geçer.

if (app.Environment.IsDevelopment())
{
    // http://localhost:5299/swagger → API test sayfası (sadece geliştirmede).
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Canlıda beklenmeyen hatalarda kullanıcıya iç detay (stack trace) gösterme.
    app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        Results.Problem("Beklenmeyen bir hata oluştu.").ExecuteAsync(context)));
    app.UseHsts();
}

app.UseHttpsRedirection();

/// wwwroot klasöründeki arayüzü (HTML, CSS, JS) sun. Site ve API aynı adreste çalıştığı için CORS gerekmez.
app.UseDefaultFiles();
// no-cache: tarayıcı dosyayı kullanmadan önce sunucuya "değişti mi?" diye sorar. Değişmediyse cevap 304 (boş) olur;
// böylece arayüz güncellenince kullanıcılar eski JS/CSS ile çalışmaya devam etmez.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
});

app.UseRateLimiter();

/// Önce "sen kimsin?" (Authentication: token'ı oku ve doğrula),
/// sonra "buna yetkin var mı?" (Authorization: [Authorize] kuralını uygula).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
