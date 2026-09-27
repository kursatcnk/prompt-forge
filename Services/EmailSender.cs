using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace PromptForge.Api.Services
{
    /// <summary>Gönderilecek e-posta: konu, düz metin hâli ve HTML hâli.</summary>
    public record EmailMessage(string Subject, string Text, string Html);

    /// <summary>E-posta gönderme sözleşmesi. Uygulama hangi yöntemle gönderildiğini bilmez.</summary>
    public interface IEmailSender
    {
        Task SendAsync(string to, EmailMessage message);
    }

    /// <summary>
    /// GELİŞTİRME MODU: E-posta gerçekten gönderilmez, düz metin hâli konsola (API'nin terminali) yazılır.
    /// </summary>
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;
        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

        public Task SendAsync(string to, EmailMessage message)
        {
            _logger.LogWarning("\n===== E-POSTA (geliştirme modu, gönderilmedi) =====\nKime : {To}\nKonu : {Subject}\n{Body}\n===================================================", to, message.Subject, message.Text);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Gerçek e-posta (SMTP). User secrets'ta "Email:Smtp:Host" tanımlıysa bu kullanılır.
    /// Gmail için: Host=smtp.gmail.com, Port=587, kullanıcı adı=Gmail adresi, şifre=Gmail "uygulama şifresi".
    /// E-posta hem HTML hem düz metin olarak gider; HTML göstermeyen programlar düz metni gösterir.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public SmtpEmailSender(IConfiguration configuration) => _configuration = configuration;

        public async Task SendAsync(string to, EmailMessage message)
        {
            var smtp = _configuration.GetSection("Email:Smtp");
            using var client = new SmtpClient(smtp["Host"], int.TryParse(smtp["Port"], out var port) ? port : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"])
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(smtp["From"] ?? smtp["Username"]!, "PromptForge"),
                Subject = message.Subject,
                Body = message.Text,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            mail.To.Add(to);
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Html, System.Text.Encoding.UTF8, MediaTypeNames.Text.Html));
            await client.SendMailAsync(mail);
        }
    }

    /// <summary>
    /// E-posta şablonları. E-posta programları harici CSS'i desteklemediği için stiller satır içi (inline) yazılır.
    /// Kullanıcıdan gelen metin (ad gibi) HTML'e eklenmeden önce kodlanır (encode); böylece e-postaya kod enjekte edilemez.
    /// </summary>
    public static class EmailTemplates
    {
        public static EmailMessage VerificationCode(string? name, string code) => new(
            "PromptForge doğrulama kodun: " + code,
            $"Merhaba {name},\n\nE-posta doğrulama kodun: {code}\n\nKod 30 dakika geçerlidir. Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin.",
            Layout(
                $"Merhaba {Encode(name)},",
                "PromptForge hesabının e-posta adresini doğrulamak için bu kodu kullan:",
                $"""<div style="margin:24px 0;padding:18px;border:1px solid #e6e6e9;border-radius:10px;background:#fafafb;text-align:center;font:600 30px/1 ui-monospace,Consolas,monospace;letter-spacing:8px;color:#111113;">{code}</div>""",
                "Kod 30 dakika geçerlidir. Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin."));

        public static EmailMessage PasswordReset(string? name, string link) => new(
            "PromptForge şifre sıfırlama",
            $"Merhaba {name},\n\nŞifreni sıfırlamak için bu bağlantıyı aç (30 dakika geçerli):\n{link}\n\nBu isteği sen yapmadıysan bu e-postayı yok sayabilirsin; şifren değişmez.",
            Layout(
                $"Merhaba {Encode(name)},",
                "Şifreni sıfırlamak için aşağıdaki butona tıkla. Bağlantı 30 dakika geçerlidir.",
                $"""<div style="margin:24px 0;"><a href="{Encode(link)}" style="display:inline-block;padding:11px 18px;border-radius:8px;background:#111113;color:#ffffff;text-decoration:none;font-weight:600;font-size:14px;">Şifremi sıfırla</a></div><p style="margin:0 0 16px;font-size:12px;color:#6b6b73;word-break:break-all;">Buton çalışmazsa bu adresi tarayıcına yapıştır:<br>{Encode(link)}</p>""",
                "Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin; şifren değişmez."));

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string Layout(string greeting, string intro, string action, string footer) => $"""
            <!doctype html>
            <html lang="tr"><body style="margin:0;padding:0;background:#f7f7f8;">
              <div style="max-width:480px;margin:0 auto;padding:32px 16px;font-family:Inter,-apple-system,'Segoe UI',Roboto,Arial,sans-serif;color:#111113;">
                <div style="font-size:15px;font-weight:600;margin-bottom:20px;">PromptForge</div>
                <div style="background:#ffffff;border:1px solid #e6e6e9;border-radius:12px;padding:28px;">
                  <p style="margin:0 0 8px;font-size:15px;font-weight:600;">{greeting}</p>
                  <p style="margin:0;font-size:14px;line-height:1.6;color:#3f3f46;">{intro}</p>
                  {action}
                  <p style="margin:0;font-size:12.5px;line-height:1.6;color:#6b6b73;">{footer}</p>
                </div>
              </div>
            </body></html>
            """;
    }
}
