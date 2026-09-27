# PromptForge — Frontend / Backend Entegrasyon Haritası

Bu dosya backend implementasyonu değildir. Yalnızca final frontend yüzeylerinin daha sonra hangi dinamik veri kaynaklarına bağlanacağını tanımlar.

## Dinamikleştirilecek ana alanlar

- Kimlik doğrulama: giriş, kayıt, şifre sıfırlama, e-posta doğrulama, 2FA ve çıkış.
- Kullanıcı profili: ad, avatar, plan ve kullanıcı tercihleri.
- Prompt optimizasyonu: hedef model, hedef/profil ayarları, prompt girdisi, işlem durumu ve sonuç.
- Prompt analizi: skorlar, kontroller, uyarılar ve token metrikleri.
- Geçmiş: optimizasyon kayıtları, arama, filtreleme ve silme.
- Favoriler: ekleme, kaldırma ve yeniden açma.
- Karşılaştırma: iki kayıt arasındaki metrik ve içerik karşılaştırması.
- Ayarlar: varsayılan model/hedef, görünüm ve diğer hesap tercihleri.

## Frontend sınırı

Bu pakette API endpointi, access token, refresh token, kullanıcı parolası, veritabanı bağlantısı veya servis credential'ı bulunmaz.

Frontend şu an yalnızca etkileşimleri ve ekran durumlarını gösterebilmek için sayfa belleğinde demo state kullanır. Backend fazında bu state servis katmanından beslenecek şekilde değiştirilebilir.

## Önerilen entegrasyon prensibi

UI bileşenleri doğrudan `fetch` çağrılarına bağlanmak yerine ayrı bir servis/adaptör katmanına bağlanmalı. Böylece mevcut tasarım ve event akışları korunurken veri kaynağı değiştirilebilir.
