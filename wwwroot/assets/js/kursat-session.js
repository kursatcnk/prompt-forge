// PromptForge oturum ve API katmanı.
// Sayfalar fetch'i doğrudan çağırmaz; token saklama ve API istekleri tek yerden yönetilir.
window.PromptForgeSession = (() => {
  "use strict";

  const TOKEN_KEY = "promptforge.token";
  const USER_KEY = "promptforge.user";
  const APPEARANCE_KEY = "promptforge.appearance";
  const FLASH_KEY = "promptforge.flash";

  // "Beni hatırla" seçiliyse localStorage (tarayıcı kapansa da kalır),
  // değilse sessionStorage (sekme kapanınca silinir) kullanılır.
  // Not: Token'ı JS ile okunabilir yerde tutmak basit ama XSS'e açıktır; ileride httpOnly cookie'ye taşınabilir.
  function stores() {
    const list = [];
    try { list.push(window.localStorage); } catch { /* depolama kapalı olabilir */ }
    try { list.push(window.sessionStorage); } catch { /* depolama kapalı olabilir */ }
    return list;
  }

  function read(key) {
    for (const store of stores()) {
      try { const value = store.getItem(key); if (value) return value; } catch { /* yok say */ }
    }
    return null;
  }

  function clearSession() {
    for (const store of stores()) {
      try { store.removeItem(TOKEN_KEY); store.removeItem(USER_KEY); } catch { /* yok say */ }
    }
  }

  function saveSession(token, user, remember) {
    clearSession();
    const store = remember ? stores()[0] : stores()[1];
    try {
      store?.setItem(TOKEN_KEY, token);
      store?.setItem(USER_KEY, JSON.stringify(user ?? {}));
    } catch { /* depolama doluysa oturum sadece bu sayfada yaşar */ }
  }

  // Giriş yapmış kullanıcının bilgisi değişince (ad değişikliği gibi) token'a dokunmadan günceller.
  function updateUser(user) {
    for (const store of stores()) {
      try { if (store.getItem(TOKEN_KEY)) store.setItem(USER_KEY, JSON.stringify(user ?? {})); } catch { /* yok say */ }
    }
  }

  // JWT'nin ortadaki parçası (payload) base64 ile kodlanmış JSON'dur; içinden son kullanma zamanını (exp) okuruz.
  function isExpired(token) {
    try {
      const payload = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
      const json = JSON.parse(atob(payload.padEnd(payload.length + (4 - payload.length % 4) % 4, "=")));
      return typeof json.exp === "number" && json.exp * 1000 <= Date.now();
    } catch {
      return true;
    }
  }

  function getToken() {
    const token = read(TOKEN_KEY);
    if (!token) return null;
    if (isExpired(token)) { clearSession(); return null; }
    return token;
  }

  function getUser() {
    try { return JSON.parse(read(USER_KEY) || "null"); } catch { return null; }
  }

  // Giriş sayfasının adresi: app.html'den "auth/sign-in.html", auth sayfalarından "sign-in.html".
  function loginUrl() {
    return location.pathname.includes("/auth/") ? "sign-in.html" : "auth/sign-in.html";
  }

  // Giriş yapılmamışsa (veya token süresi dolmuşsa) giriş sayfasına gönderir.
  function requireAuth(url = loginUrl()) {
    if (!getToken()) window.location.replace(url);
  }

  // Sayfalar arası tek seferlik mesaj (örn. "Şifren güncellendi" → giriş ekranında gösterilir).
  function setFlash(message, type = "success") {
    try { sessionStorage.setItem(FLASH_KEY, JSON.stringify({ message, type })); } catch { /* yok say */ }
  }

  function takeFlash() {
    try {
      const value = JSON.parse(sessionStorage.getItem(FLASH_KEY) || "null");
      sessionStorage.removeItem(FLASH_KEY);
      return value;
    } catch { return null; }
  }

  // Tema/yoğunluk/hareket tercihi yerelde de tutulur; sayfa açılırken sunucu cevabını beklemeden doğru tema boyanır.
  function saveAppearance(appearance) {
    try { localStorage.setItem(APPEARANCE_KEY, JSON.stringify(appearance)); } catch { /* yok say */ }
  }

  function readAppearance() {
    try { return JSON.parse(localStorage.getItem(APPEARANCE_KEY) || "null"); } catch { return null; }
  }

  // Tüm API istekleri buradan geçer: JSON gönderir, varsa token'ı ekler, cevabı tek formatta döner.
  async function request(path, { method = "GET", body } = {}) {
    const headers = { "Accept": "application/json" };
    if (body !== undefined) headers["Content-Type"] = "application/json";
    const token = getToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;

    let response;
    try {
      response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    } catch {
      return { ok: false, status: 0, data: { message: "Sunucuya ulaşılamadı. API çalışıyor mu?" } };
    }

    // Token gönderdik ama 401 geldiyse oturum geçersizleşmiş (süresi dolmuş veya hesap silinmiş): girişe dön.
    if (response.status === 401 && token) {
      clearSession();
      setFlash("Oturumun sona erdi. Lütfen tekrar giriş yap.", "error");
      window.location.replace(loginUrl());
    }
    if (response.status === 429 && !path.startsWith("/api/prompts")) {
      return { ok: false, status: 429, data: { message: "Çok fazla deneme yaptın. Bir dakika bekleyip tekrar dene." } };
    }

    const data = response.status === 204 ? null : await response.json().catch(() => null);
    return { ok: response.ok, status: response.status, data };
  }

  const api = {
    get: path => request(path),
    post: (path, body = {}) => request(path, { method: "POST", body }),
    put: (path, body = {}) => request(path, { method: "PUT", body }),
    del: path => request(path, { method: "DELETE" })
  };

  return { getToken, getUser, updateUser, saveSession, clearSession, requireAuth, request, api, setFlash, takeFlash, saveAppearance, readAppearance };
})();
