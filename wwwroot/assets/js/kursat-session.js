// Oturum ve API katmanı. Sayfalar fetch'i kendileri çağırmıyor, token ve istekler hep buradan geçiyor.
window.PromptForgeSession = (() => {
  "use strict";

  const TOKEN_KEY = "promptforge.token";
  const USER_KEY = "promptforge.user";
  const APPEARANCE_KEY = "promptforge.appearance";
  const FLASH_KEY = "promptforge.flash";

  // Beni hatırla → localStorage, değilse sessionStorage (sekme kapanınca gidiyor).
  // TODO: token JS'ten okunabilir yerde, XSS'e açık. İleride httpOnly cookie'ye taşımak lazım.
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

  // Ad değişince token'a dokunmadan sadece kullanıcı bilgisini güncelle.
  function updateUser(user) {
    for (const store of stores()) {
      try { if (store.getItem(TOKEN_KEY)) store.setItem(USER_KEY, JSON.stringify(user ?? {})); } catch { /* yok say */ }
    }
  }

  // JWT'nin orta parçası base64 JSON; içinden exp'i okuyorum, süresi dolmuşsa hiç istek atmadan çıkış.
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

  // app.html'den auth/sign-in.html, auth sayfalarının içinden sign-in.html
  function loginUrl() {
    return location.pathname.includes("/auth/") ? "sign-in.html" : "auth/sign-in.html";
  }

  function requireAuth(url = loginUrl()) {
    if (!getToken()) window.location.replace(url);
  }

  // Sayfa değişirken bırakılan tek seferlik mesaj (örn. şifre sıfırlandıktan sonra giriş ekranında).
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

  // Görünüm tercihini yerelde de tutuyorum; yoksa sayfa önce açık temada açılıp sonra kararıyor.
  function saveAppearance(appearance) {
    try { localStorage.setItem(APPEARANCE_KEY, JSON.stringify(appearance)); } catch { /* yok say */ }
  }

  function readAppearance() {
    try { return JSON.parse(localStorage.getItem(APPEARANCE_KEY) || "null"); } catch { return null; }
  }

  // Her istek { ok, status, data } dönüyor, çağıran taraf fetch detaylarıyla uğraşmıyor.
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

    // Token vardı ama 401 geldi: süresi dolmuş ya da hesap silinmiş. Girişe dön.
    if (response.status === 401 && token) {
      clearSession();
      setFlash("Oturumun sona erdi. Lütfen tekrar giriş yap.", "error");
      window.location.replace(loginUrl());
    }
    const data = response.status === 204 ? null : await response.json().catch(() => null);
    // 429 iki şey olabilir: aylık kota (code: quota_exceeded, içinde usage var) ya da dakikalık istek sınırı (boş gövde).
    if (response.status === 429 && data?.code !== "quota_exceeded") {
      return { ok: false, status: 429, data: { message: "Çok hızlı istek gönderdin. Bir dakika bekleyip tekrar dene." } };
    }
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
