namespace PromptForge.Api.Services
{
    /// <summary>
    /// Kullanıcı işlemleri için servis arayüzü (sözleşme).
    ///
    /// NEDEN INTERFACE?
    /// - Controller'lar concrete class yerine interface'e bağımlı olur.
    /// - Unit test'te fake implementation kullanabiliriz.
    /// - Implementation değişse, Controller kodu değişmez.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Yeni kullanıcıyı kaydeder.
        ///
        /// ADIMLAR:
        /// 1. Email zaten var mı kontrol et
        /// 2. Şifreyi hash'le (Bcrypt)
        /// 3. Veritabanına kaydet
        /// 4. Token dön (otomatik login)
        ///
        /// DÖNÜŞ:
        /// - Success: (true, token, null)
        /// - Hata: (false, null, error message)
        /// </summary>
        Task<(bool success, string? token, string? error)> RegisterAsync(
            string email,
            string password,
            string displayName);

        /// <summary>
        /// Kullanıcı girişi.
        ///
        /// ADIMLAR:
        /// 1. Email ile kullanıcı bul
        /// 2. Şifre hash'ini kıyasla
        /// 3. Eşleşirse token dön
        /// 4. Değilse hata dön
        /// </summary>
        Task<(bool success, string? token, string? error)> LoginAsync(
            string email,
            string password);

        /// <summary>
        /// JWT token'ı doğrula.
        /// Middleware'in her istek'te bunu çağırması.
        /// </summary>
        Task<(bool valid, string? userId, string? error)> ValidateTokenAsync(string token);

        /// <summary>
        /// User ID'den kullanıcıyı getir.
        /// Controller'ların profil bilgisi lazım olduğunda kullanır.
        /// </summary>
        Task<dynamic?> GetUserByIdAsync(string userId);
    }
}
