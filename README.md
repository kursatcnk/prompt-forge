# PromptForge

Dağınık promptları hedef AI modeline göre daha net, düzenli ve uygulanabilir hale getiren prompt optimizasyon çalışma alanı.

ASP.NET Core 8 Web API + SQL Server + `wwwroot` içinde HTML/CSS/JS arayüz. Site ve API aynı adreste çalışır: `http://localhost:5299`.

## Çalıştırma

1. Visual Studio'da `PromptForge.sln` dosyasını aç.
2. Veritabanını oluştur (ilk seferde): **Tools → NuGet Package Manager → Package Manager Console** → `Update-Database`
3. Üstteki profil menüsünden **http** seçili olsun ve **Ctrl+F5** ile başlat. Tarayıcıda giriş ekranı açılır.

Veritabanı bağlantısı `appsettings.json` → `ConnectionStrings:DefaultConnection` içinde (`localhost\SQLEXPRESS`).

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
| `Controllers/` | API uç noktaları: Auth, Account, Prompts, Favorites, Users (sadece geliştirmede) |
| `Services/` | İş mantığı: kullanıcı/hesap, kütüphane, kota, tek kullanımlık kodlar, 2FA, e-posta |
| `Services/Ai/` | AI sağlayıcı adaptörleri (Claude, GPT, Gemini, DeepSeek) ve optimize servisi |
| `Models/`, `Data/` | Veritabanı modelleri, DbContext ve EF Core migration'ları |
| `Dtos/` | API'ye gelen ve dönen veri paketleri |
| `wwwroot/` | Arayüz: `app.html` çalışma alanı, `auth/` giriş sayfaları |
| `docs/` | Tasarımla gelen backend entegrasyon notu |

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
