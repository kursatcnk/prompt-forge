# PromptForge

Dağınık promptları hedef AI modeline göre daha net, düzenli ve uygulanabilir hale getiren prompt optimizasyon çalışma alanı.

ASP.NET Core 8 Web API + SQL Server + `wwwroot` içinde HTML/CSS/JS arayüz. Site ve API aynı adreste çalışır: `http://localhost:5299`.

## Çalıştırma

1. Visual Studio'da `PromptForge.sln` dosyasını aç.
2. Üstteki profil menüsünden **http** seçili olsun ve **Ctrl+F5** ile başlat (ya da terminalde `dotnet run`).

Tablolar açılışta migration'larla kendiliğinden oluşuyor. Bağlantı adresi `appsettings.json` → `ConnectionStrings:DefaultConnection` (`localhost\SQLEXPRESS`).
`/health` adresi uygulamanın ve veritabanı bağlantısının ayakta olup olmadığını döner.

## Arkadaşlarla paylaşma (Cloudflare tüneli)

`Paylas.bat` dosyasına çift tıkla: API'yi ve Cloudflare tünelini ayrı pencerelerde açar. Tünel penceresinde çıkan
`https://....trycloudflare.com` linki paylaşılır. Bilgisayar ve pencereler açık kaldıkça çalışır, her açılışta link değişir.
Tünel arkasında gerçek kullanıcı IP'si `X-Forwarded-For` başlığından okunur, deneme sınırları herkese ayrı işler.

## AI anahtarı ekleme

Anahtar olmadan da uygulama çalışır; optimizasyonlar yerel kural motoruyla yapılır ve üst barda **Yerel mod** yazar.
Gerçek AI için en az bir sağlayıcının anahtarını **user secrets** ile ekle. User secrets proje klasörünün dışında saklanır, GitHub'a gitmez.

Visual Studio: Solution Explorer'da projeye sağ tık → **Manage User Secrets** → açılan `secrets.json` dosyasına yaz:

```json
{
  "AI:Anthropic:ApiKey": "sk-ant-..."
}
```

veya terminalde:

```bash
dotnet user-secrets set "AI:Anthropic:ApiKey" "sk-ant-..."
```

Desteklenen anahtarlar: `AI:Anthropic:ApiKey`, `AI:OpenAI:ApiKey`, `AI:Gemini:ApiKey`, `AI:DeepSeek:ApiKey`.
Hedef model seçimine uygun sağlayıcı tanımlıysa o kullanılır, değilse tanımlı olan ilk sağlayıcı. Model adları `appsettings.json` → `AI` bölümünden değiştirilebilir.

## E-postalar

Varsayılan **geliştirme modunda** e-postalar gönderilmez; doğrulama kodu ve şifre sıfırlama bağlantısı API'nin konsol penceresine yazılır.
Gerçek e-posta için user secrets'a SMTP ayarlarını ekle (Gmail için bir "uygulama şifresi" gerekir):

```json
{
  "Email:Smtp:Host": "smtp.gmail.com",
  "Email:Smtp:Port": "587",
  "Email:Smtp:Username": "adres@gmail.com",
  "Email:Smtp:Password": "uygulama-sifresi",
  "Email:Smtp:From": "adres@gmail.com"
}
```

## Proje yapısı

| Klasör | İçerik |
|---|---|
| `Controllers/` | API uç noktaları: Auth, Account, Prompts, Favorites, Users (sadece geliştirmede ve bu bilgisayardan) |
| `Services/` | İş mantığı: kullanıcı/hesap, kütüphane, kota, tek kullanımlık kodlar, 2FA, e-posta, arka plan temizliği |
| `Services/Ai/` | AI sağlayıcı adaptörleri (Claude, GPT, Gemini, DeepSeek) ve optimize servisi |
| `Models/`, `Data/` | Veritabanı modelleri, DbContext ve EF Core migration'ları |
| `Dtos/` | API'ye gelen ve dönen veri paketleri |
| `wwwroot/` | Arayüz: `index.html` tanıtım sayfası, `app.html` çalışma alanı, `auth/` giriş sayfaları |
| `docs/` | Tasarımla gelen backend entegrasyon notu |

## Güvenlik notları

- Şifreler BCrypt ile, tek kullanımlık kodlar SHA-256 ile saklanır; 2FA secret'ları Data Protection ile şifrelidir (anahtarlar DB'de).
- Giriş/kayıt/şifre işlemleri IP başına dakikada 10, AI istekleri kullanıcı başına dakikada 12 istekle sınırlı.
- Aylık kota son hakta bile aşılamaz (`sp_getapplock` ile kullanıcı başına kilit).
- Yanıtlarda `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` başlıkları var; canlıda HSTS açık.
- JWT tarayıcıda localStorage'da duruyor; ileride httpOnly cookie'ye taşınması planlanıyor.

## Canlıya alma (ücretsiz: MonsterASP.NET)

Canlı sunucu ayarları `appsettings.Production.json` dosyasındadır. Bu dosya `.gitignore`'da olduğu için GitHub'a gitmez
ama Visual Studio yayınlarken sunucuya kopyalanır. İçinde bağlantı adresi, JWT anahtarı, AI anahtarı ve SMTP ayarları bulunur.

1. monsterasp.net'e kaydol, Control Panel'de bir **website** ve bir **MSSQL database** oluştur.
2. Veritabanının bağlantı adresini (connection string) kopyala ve `appsettings.Production.json` içindeki
   `DefaultConnection` değerine yapıştır.
3. Website ayarlarında **WebDeploy** hesabını aç, `.publishSettings` dosyasını indir.
4. Visual Studio → projeye sağ tık → **Publish** → **Import Profile** → indirdiğin dosyayı seç → **Publish**.
5. Tablolar ilk açılışta otomatik oluşur (`Database.Migrate()`); elle SQL çalıştırmaya gerek yoktur.
6. Control Panel → HTTPS bölümünden ücretsiz Let's Encrypt sertifikasını etkinleştir (ücretsiz planda 90 günde bir elle yenilenir).

- Ödeme sistemi henüz yok; Pro plan "Yakında" olarak görünür.
