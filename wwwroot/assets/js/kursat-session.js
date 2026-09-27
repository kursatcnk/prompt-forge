// PromptForge oturum ve API katmanı.
// Sayfalar fetch'i doğrudan çağırmaz; token saklama ve API istekleri tek yerden yönetilir.
window.PromptForgeSession = (() => {
  "use strict";

  const TOKEN_KEY = "promptforge.token";
  const USER_KEY = "promptforge.user";

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

  // Giriş yapılmamışsa (veya token süresi dolmuşsa) giriş sayfasına gönderir.
  function requireAuth(loginUrl) {
    if (!getToken()) window.location.replace(loginUrl);
  }

  // Tüm API istekleri buradan geçer: JSON gönderir, varsa token'ı ekler, cevabı tek formatta döner.
  async function request(path, { method = "GET", body } = {}) {
    const headers = { "Accept": "application/json" };
    if (body !== undefined) headers["Content-Type"] = "application/json";
    const token = getToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;

    try {
      const response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
      const data = await response.json().catch(() => null);
      return { ok: response.ok, status: response.status, data };
    } catch {
      return { ok: false, status: 0, data: { message: "Sunucuya ulaşılamadı. API çalışıyor mu?" } };
    }
  }

  return { getToken, getUser, saveSession, clearSession, requireAuth, request };
})();
