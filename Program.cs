using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PromptForge.Api.Data;
using PromptForge.Api.Services;
using PromptForge.Api.Services.Ai;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json'daki örnek anahtar GitHub'da açık duruyor, canlıda onunla ayağa kalkmasın.
// HS256 en az 32 byte istiyor; kısa anahtarda hata ilk girişte değil burada çıksın.
const string SampleJwtSecret = "your-super-secret-key-minimum-32-characters-long-here-12345678";
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "";
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("Jwt:Secret en az 32 karakter olmalı.");
if (!builder.Environment.IsDevelopment() && jwtSecret == SampleJwtSecret)
    throw new InvalidOperationException("Canlı ortamda Jwt:Secret değiştirilmeli (user secrets veya appsettings.Production.json).");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger'da Authorize butonu çıksın, token'ı bir kere yapıştırınca tüm isteklerde gitsin.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login'den dönen token (başına Bearer yazmadan)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Giriş/kayıt/şifre sıfırlama: IP başına dakikada 10. Brute force ve mail bombardımanı için.
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // AI çağrıları: aylık kota zaten var ama birinin 50 hakkı 10 saniyede yakmasını da istemiyorum.
    options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddDbContext<PromptForgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 5)));

// 2FA secret'larını şifreliyor. Anahtarları DB'de tutuyorum; paylaşımlı hosting'te
// uygulama yeniden başlayınca anahtar kaybolursa herkesin 2FA'sı bozuluyor.
builder.Services.AddDataProtection()
    .SetApplicationName("PromptForge")
    .PersistKeysToDbContext<PromptForgeDbContext>();

builder.Services.AddHealthChecks().AddDbContextCheck<PromptForgeDbContext>("database");

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PromptLibraryService>();
builder.Services.AddScoped<UsageService>();
builder.Services.AddScoped<OneTimeCodeService>();
builder.Services.AddSingleton<TwoFactorService>();
builder.Services.AddHostedService<CleanupService>();

// SMTP tanımlı değilse mailler terminale düşüyor, geliştirirken işimi görüyor.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]))
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

// Hepsi IAiProvider olarak kayıtlı; optimizer anahtarı tanımlı olanı kendisi seçiyor.
builder.Services.AddHttpClient("ai", client => client.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<IAiProvider, AnthropicProvider>();
builder.Services.AddSingleton<IAiProvider, OpenAiProvider>();
builder.Services.AddSingleton<IAiProvider, GeminiProvider>();
builder.Services.AddSingleton<IAiProvider, DeepSeekProvider>();
builder.Services.AddSingleton<PromptOptimizerService>();
builder.Services.AddSingleton<PromptAnalysisService>();

var app = builder.Build();

// Bekleyen migration'lar açılışta uygulanıyor, sunucuda elle SQL çalıştırmaya gerek kalmıyor.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PromptForgeDbContext>().Database.Migrate();
}

// Tünel/proxy arkasında her istek 127.0.0.1'den geliyor gibi görünüyor. Gerçek IP ve https bilgisini
// X-Forwarded-* başlıklarından al; yoksa rate limit herkes için ortak işliyor, maildeki linkler localhost'a gidiyor.
// Varsayılan ayarda sadece loopback'ten gelen başlıklara güveniliyor.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Canlıda stack trace dışarı sızmasın.
    app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        Results.Problem("Beklenmeyen bir hata oluştu.").ExecuteAsync(context)));
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseResponseCompression();

// Site ile API aynı origin'de, CORS'a gerek yok.
app.UseDefaultFiles();
// no-cache: tarayıcı her seferinde "değişti mi" diye soruyor, değişmediyse 304 geliyor.
// Arayüzü güncellediğimde kimse eski JS ile kalmasın diye.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
});

app.UseAuthentication();
app.UseAuthorization();
// "ai" limiti kullanıcı id'sine göre çalışıyor, o yüzden authentication'dan sonra.
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
