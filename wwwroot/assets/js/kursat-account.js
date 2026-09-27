(() => {
  "use strict";
  // Hesap, güvenlik, plan/kota kartları; plan penceresi; 2 adımlı doğrulama kurulumu ve üst bardaki AI durum göstergesi.
  const PF = window.PF;
  if (!PF) return;

  const account = () => PF.state.account;
  const esc = PF.escape;

  // Form altındaki mesaj satırı (hata kırmızı, başarı yeşil).
  function setMessage(el, text, type) {
    if (!el) return;
    el.textContent = text || "";
    el.className = `kursat-form-message${type ? ` is-${type}` : ""}`;
  }

  async function withBusy(button, action) {
    const label = button.textContent;
    button.disabled = true;
    button.textContent = "Bekle…";
    try { return await action(); }
    finally { button.disabled = false; button.textContent = label; }
  }

  // ===== Hesap kartı: ad değiştirme ve şifre değiştirme =====
  function renderAccountCard() {
    const host = document.querySelector("#kursat-account-card");
    const me = account();
    if (!host || !me) return;
    const user = me.user;
    host.innerHTML = `<h3>Hesap</h3><p>Profil bilgilerin ve giriş şifren.</p>
      <div class="kursat-field"><label for="kursat-profile-name">Görünen ad</label><input class="kursat-input" id="kursat-profile-name" maxlength="100" value="${esc(user.displayName || "")}"></div>
      <div class="kursat-actions-end"><button class="kursat-button" id="kursat-profile-save" type="button">Adı kaydet</button></div>
      <p class="kursat-form-message" id="kursat-profile-message"></p>
      <div class="kursat-setting-row"><div><strong>E-posta</strong><span>${esc(user.email)}</span></div><span class="kursat-badge ${user.emailConfirmed ? "kursat-badge--success" : "kursat-badge--warning"}">${user.emailConfirmed ? "Doğrulandı" : "Doğrulanmadı"}</span></div>
      <div class="kursat-setting-row"><div><strong>Şifre</strong><span>Giriş için kullandığın şifreyi değiştir.</span></div><button class="kursat-button" id="kursat-password-toggle" type="button">Değiştir</button></div>
      <form class="kursat-inline-form" id="kursat-password-form" hidden>
        <div class="kursat-field"><label for="kursat-password-current">Mevcut şifre</label><input class="kursat-input" id="kursat-password-current" type="password" autocomplete="current-password" required></div>
        <div class="kursat-field"><label for="kursat-password-new">Yeni şifre</label><input class="kursat-input" id="kursat-password-new" type="password" autocomplete="new-password" placeholder="En az 8 karakter" required></div>
        <div class="kursat-field"><label for="kursat-password-confirm">Yeni şifre (tekrar)</label><input class="kursat-input" id="kursat-password-confirm" type="password" autocomplete="new-password" required></div>
        <p class="kursat-form-message" id="kursat-password-message"></p>
        <div class="kursat-actions-end"><button class="kursat-button kursat-button--primary" type="submit">Şifreyi güncelle</button></div>
      </form>`;

    host.querySelector("#kursat-profile-save").addEventListener("click", event => withBusy(event.currentTarget, async () => {
      const message = host.querySelector("#kursat-profile-message");
      const { ok, data } = await PF.api.put("/api/account/profile", { displayName: host.querySelector("#kursat-profile-name").value });
      if (!ok) { setMessage(message, data?.message || "Ad kaydedilemedi.", "error"); return; }
      me.user = { ...me.user, ...data };
      PF.renderUser(me.user);
      setMessage(message, "Ad güncellendi.", "success");
    }));

    const form = host.querySelector("#kursat-password-form");
    host.querySelector("#kursat-password-toggle").addEventListener("click", () => {
      form.hidden = !form.hidden;
      if (!form.hidden) form.querySelector("input").focus();
    });
    form.addEventListener("submit", event => {
      event.preventDefault();
      const message = form.querySelector("#kursat-password-message");
      const currentPassword = form.querySelector("#kursat-password-current").value;
      const newPassword = form.querySelector("#kursat-password-new").value;
      if (newPassword.length < 8) { setMessage(message, "Yeni şifre en az 8 karakter olmalı.", "error"); return; }
      if (newPassword !== form.querySelector("#kursat-password-confirm").value) { setMessage(message, "Yeni şifreler eşleşmiyor.", "error"); return; }
      withBusy(form.querySelector("button[type=submit]"), async () => {
        const { ok, data } = await PF.api.post("/api/account/change-password", { currentPassword, newPassword });
        if (!ok) { setMessage(message, data?.message || "Şifre değiştirilemedi.", "error"); return; }
        form.reset();
        form.hidden = true;
        PF.toast("Şifren güncellendi");
      });
    });
  }

  // ===== Güvenlik kartı: e-posta doğrulama ve 2 adımlı doğrulama =====
  function renderSecurityCard() {
    const host = document.querySelector("#kursat-security-card");
    const me = account();
    if (!host || !me) return;
    const user = me.user;
    host.innerHTML = `<h3>Güvenlik</h3><p>E-posta doğrulama ve iki adımlı doğrulama.</p>
      <div class="kursat-setting-row"><div><strong>E-posta doğrulama</strong><span>${user.emailConfirmed ? "E-posta adresin doğrulandı." : "Gönderilen 6 haneli kodla adresini doğrula."}</span></div>${user.emailConfirmed ? `<span class="kursat-badge kursat-badge--success">Tamam</span>` : `<a class="kursat-button" href="auth/email-verification.html">Doğrula</a>`}</div>
      <div class="kursat-setting-row"><div><strong>İki adımlı doğrulama</strong><span>${user.twoFactorEnabled ? "Açık. Girişte authenticator kodu istenir." : "Kapalı. Google/Microsoft Authenticator ile hesabını koru."}</span></div>${user.twoFactorEnabled ? `<a class="kursat-button" href="auth/approve-password.html?action=disable-2fa">Kapat</a>` : `<button class="kursat-button kursat-button--primary" id="kursat-2fa-start" type="button">Aç</button>`}</div>`;
    host.querySelector("#kursat-2fa-start")?.addEventListener("click", event => withBusy(event.currentTarget, startTwoFactorSetup));
  }

  // 2FA kurulumu: sunucudan QR al → kullanıcı uygulamaya okutur → ilk kodu girer → açılır.
  async function startTwoFactorSetup() {
    const { ok, data } = await PF.api.post("/api/account/two-factor/setup");
    if (!ok) { PF.toast("Kurulum başlatılamadı", data?.message || "Tekrar dene."); return; }
    openPanel("İki adımlı doğrulamayı aç", `
      <p class="kursat-modal-copy">1. Telefonundaki authenticator uygulamasıyla (Google Authenticator, Microsoft Authenticator vb.) bu QR kodu okut.</p>
      <div class="kursat-qr"><img src="${data.qrDataUrl}" alt="İki adımlı doğrulama QR kodu"></div>
      <p class="kursat-modal-copy">QR okutamıyorsan bu anahtarı uygulamaya elle gir:</p>
      <div class="kursat-secret">${esc(data.secret)}</div>
      <form class="kursat-inline-form" id="kursat-2fa-form">
        <div class="kursat-field"><label for="kursat-2fa-code">2. Uygulamadaki 6 haneli kodu gir</label><input class="kursat-input" id="kursat-2fa-code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" placeholder="123456" required></div>
        <p class="kursat-form-message" id="kursat-2fa-message"></p>
        <div class="kursat-actions-end"><button class="kursat-button" data-kursat-panel-close type="button">Vazgeç</button><button class="kursat-button kursat-button--primary" type="submit">Etkinleştir</button></div>
      </form>`);
    const form = document.querySelector("#kursat-2fa-form");
    form.addEventListener("submit", event => {
      event.preventDefault();
      withBusy(form.querySelector("button[type=submit]"), async () => {
        const response = await PF.api.post("/api/account/two-factor/enable", { code: form.querySelector("#kursat-2fa-code").value });
        if (!response.ok) { setMessage(form.querySelector("#kursat-2fa-message"), response.data?.message || "Kod doğrulanamadı.", "error"); return; }
        account().user.twoFactorEnabled = true;
        closePanel();
        renderSecurityCard();
        PF.toast("İki adımlı doğrulama açıldı", "Bir sonraki girişte authenticator kodu istenecek.");
      });
    });
  }

  // ===== Plan kartı, plan penceresi ve kenar çubuğu =====
  function usageBar(usage) {
    const percent = usage.limit ? Math.min(100, Math.round(usage.used / usage.limit * 100)) : 0;
    const state = percent >= 100 ? "is-full" : percent >= 80 ? "is-warning" : "";
    return `<div class="kursat-usage-head"><span>Bu ay kullanılan</span><span><strong>${usage.used}</strong> / ${usage.limit}</span></div>
      <div class="kursat-progress"><span class="${state}" style="width:${percent}%"></span></div>
      <p class="kursat-form-message">Kota ${esc(PF.formatDate(usage.resetsAt))} tarihinde yenilenir.</p>`;
  }

  function planName(key) {
    return account()?.plans?.find(plan => plan.key === key)?.name || key;
  }

  function renderPlanCard() {
    const host = document.querySelector("#kursat-plan-card");
    const me = account();
    if (!host || !me) return;
    const ai = me.ai || {};
    host.innerHTML = `<h3>Plan ve kullanım</h3><p>Aylık optimizasyon kotan ve AI motoru durumu.</p>
      <div class="kursat-setting-row"><div><strong>Mevcut plan</strong><span>${esc(planName(me.usage.plan))}</span></div><button class="kursat-button" id="kursat-plan-open" type="button">Planları gör</button></div>
      ${usageBar(me.usage)}
      <div class="kursat-setting-row"><div><strong>AI motoru</strong><span>${ai.enabled ? `${esc(ai.provider)} · ${esc(ai.model)}` : "AI anahtarı tanımlı değil; optimizasyonlar yerel kural motoruyla yapılıyor."}</span></div><span class="kursat-badge ${ai.enabled ? "kursat-badge--success" : "kursat-badge--warning"}">${ai.enabled ? "Bağlı" : "Yerel mod"}</span></div>`;
    host.querySelector("#kursat-plan-open").addEventListener("click", openPlans);
  }

  function openPlans() {
    const me = account();
    if (!me) return;
    openPanel("Planlar", `
      <p class="kursat-modal-copy">Kotan her ayın başında yenilenir. Ödeme sistemi henüz aktif olmadığı için Pro plana geçiş şu an mümkün değil.</p>
      ${usageBar(me.usage)}
      <div class="kursat-plan-grid">${me.plans.map(plan => {
        const current = plan.key === me.usage.plan;
        return `<article class="kursat-plan-card${current ? " is-current" : ""}">
          <div><h3>${esc(plan.name)}</h3><span class="kursat-plan-price">${esc(plan.price)}</span></div>
          <ul>${plan.features.map(feature => `<li>${esc(feature)}</li>`).join("")}</ul>
          ${current ? `<span class="kursat-badge kursat-badge--accent">Mevcut planın</span>` : `<button class="kursat-button" type="button" disabled>Yakında</button>`}
        </article>`;
      }).join("")}</div>`);
  }

  function renderSidebarPlan() {
    const me = account();
    if (!me) return;
    const title = document.querySelector("#kursat-sidebar-plan-title");
    const copy = document.querySelector("#kursat-sidebar-plan-copy");
    if (title) title.textContent = me.usage.plan === "pro" ? "PromptForge Pro" : `${planName(me.usage.plan)} plan`;
    if (copy) copy.textContent = `Bu ay ${me.usage.used}/${me.usage.limit} hak kullanıldı.`;
    // Kota çubuğu: %80'den sonra uyarı rengi, dolunca kırmızı.
    const bar = document.querySelector("#kursat-sidebar-plan-bar");
    if (bar) {
      const pct = Math.min(100, Math.round(me.usage.used / Math.max(1, me.usage.limit) * 100));
      bar.style.width = `${pct}%`;
      bar.classList.toggle("is-warning", pct >= 80 && pct < 100);
      bar.classList.toggle("is-full", pct >= 100);
    }
  }

  // Üst bardaki gösterge: AI bağlıysa yeşil nokta + sağlayıcı adı, değilse sarı nokta + "Yerel mod".
  function renderEngineStatus() {
    const pill = document.querySelector("#kursat-engine-status");
    const ai = account()?.ai;
    if (!pill || !ai) return;
    pill.classList.toggle("is-local", !ai.enabled);
    pill.querySelector("span").textContent = ai.enabled ? `AI · ${ai.provider}` : "Yerel mod";
    pill.title = ai.enabled ? `Optimizasyonlar ${ai.provider} (${ai.model}) ile yapılıyor.` : "AI anahtarı tanımlı değil; yerel kural motoru kullanılıyor.";
  }

  // ===== Ortak pencere (modal) =====
  function openPanel(title, html) {
    document.querySelector("#kursat-panel-title").textContent = title;
    document.querySelector("#kursat-panel-body").innerHTML = html;
    PF.openBackdrop(document.querySelector("#kursat-panel-backdrop"));
  }

  function closePanel() {
    PF.closeBackdrop(document.querySelector("#kursat-panel-backdrop"));
  }

  // Kota dolduysa Optimize Et butonunu kilitle ve yenilenme zamanını editörün altında göster.
  function applyQuota() {
    const usage = account()?.usage;
    if (!usage) return;
    const exhausted = usage.used >= usage.limit;
    PF.state.quotaExhausted = exhausted;
    const button = document.querySelector("#kursat-forge-button");
    const note = document.querySelector("#kursat-quota-note");
    if (button && !button.classList.contains("is-loading")) button.disabled = exhausted;
    if (button) button.title = exhausted ? "Aylık kotan doldu" : "Ctrl + Enter";
    if (note) note.textContent = exhausted
      ? `Aylık ${usage.limit} hakkının tamamını kullandın. Kotan ${PF.formatDate(usage.resetsAt)} tarihinde yenilenecek.`
      : usage.limit - usage.used <= 5 ? `Bu ay ${usage.limit - usage.used} hakkın kaldı.` : "";
  }

  function renderAll() {
    renderAccountCard();
    renderSecurityCard();
    renderPlanCard();
    renderSidebarPlan();
    renderEngineStatus();
    applyQuota();
  }

  document.querySelector("#kursat-open-plans")?.addEventListener("click", openPlans);
  document.addEventListener("kursat:account-loaded", renderAll);
  // Her optimizasyondan sonra kota bilgisi güncellenir.
  document.addEventListener("kursat:usage-changed", event => {
    if (!account() || !event.detail) return;
    account().usage = event.detail;
    renderPlanCard();
    renderSidebarPlan();
    applyQuota();
  });
})();
