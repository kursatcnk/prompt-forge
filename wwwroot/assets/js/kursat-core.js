(() => {
  "use strict";

  // Ortak state burada; diğer dosyalar PF.state üzerinden okuyup yazıyor.
  const kursatState = {
    model: "gpt",
    goal: "balanced",
    history: [],
    favorites: [],
    draftEnabled: false,
    theme: "light",
    motion: "on",
    density: "comfortable",
    useCase: "general",
    responseFormat: "auto",
    responseLanguage: "prompt",
    askClarifying: true,
    exposeAssumptions: true,
    pendingConfirm: null,
    currentResult: null,
    currentCommandIndex: 0,
    previousFocus: null
  };

  const kursatPageMeta = {
    forge: ["Çalışma alanı", "Optimize Et"],
    templates: ["Oluştur", "Şablonlar"],
    workshop: ["Oluştur", "Atölye"],
    flows: ["Oluştur", "Akışlar"],
    blocks: ["Oluştur", "Yapı Taşları"],
    history: ["Kütüphane", "Geçmiş"],
    favorites: ["Kütüphane", "Favoriler"],
    compare: ["Araçlar", "Karşılaştır"],
    health: ["Araçlar", "Prompt Analizi"],
    guide: ["Öğren", "Rehber"],
    settings: ["Tercihler", "Ayarlar"]
  };

  const kursatCommands = [
    ["Optimize Et", "Prompt editörünü aç", "forge"],
    ["Şablonlar", "Hazır prompt şablonlarından seç", "templates"],
    ["Atölye", "Sıfırdan prompt oluştur", "workshop"],
    ["Akışlar", "Görevi adımlara böl", "flows"],
    ["Yapı Taşları", "Modüler prompt oluştur", "blocks"],
    ["Geçmiş", "Önceki optimizasyonları gör", "history"],
    ["Favoriler", "Kaydettiğin promptları gör", "favorites"],
    ["Karşılaştır", "İki sürümü yan yana incele", "compare"],
    ["Prompt Analizi", "Prompt sağlığını ölç", "health"],
    ["Rehber", "Kullanım rehberi ve iyi prompt dersleri", "guide"],
    ["Ayarlar", "Çalışma alanını yönet", "settings"]
  ];

  const session = window.PromptForgeSession;
  // SettingsDto ile birebir aynı adlar.
  const kursatSettingKeys = ["model", "goal", "theme", "density", "motion", "useCase", "responseFormat", "responseLanguage", "askClarifying", "exposeAssumptions"];
  let kursatSaveTimer = 0;

  // İlk boyama için yereldeki görünüm; asıl ayarlar bootstrap'te sunucudan geliyor.
  function kursatLoadStorage() {
    const appearance = session?.readAppearance();
    if (!appearance) return;
    if (["light", "dark", "system"].includes(appearance.theme)) kursatState.theme = appearance.theme;
    if (["comfortable", "compact"].includes(appearance.density)) kursatState.density = appearance.density;
    if (["on", "off"].includes(appearance.motion)) kursatState.motion = appearance.motion;
  }

  // 600 ms debounce: art arda tıklamalarda her seferinde istek gitmesin.
  function kursatSaveStorage() {
    session?.saveAppearance({ theme: kursatState.theme, density: kursatState.density, motion: kursatState.motion });
    window.clearTimeout(kursatSaveTimer);
    kursatSaveTimer = window.setTimeout(async () => {
      const body = Object.fromEntries(kursatSettingKeys.map(key => [key, kursatState[key]]));
      const { ok } = await session.api.put("/api/account/settings", body);
      if (!ok) kursatToast("Ayarlar kaydedilemedi", "Bağlantını kontrol edip tekrar dene.");
    }, 600);
  }

  // Üçü birbirini beklemesin.
  async function kursatBootstrap() {
    const [me, history, favorites] = await Promise.all([
      session.api.get("/api/account/me"),
      session.api.get("/api/prompts"),
      session.api.get("/api/favorites")
    ]);

    if (me.ok && me.data) {
      kursatState.account = me.data;
      kursatSettingKeys.forEach(key => { if (me.data.settings?.[key] !== undefined) kursatState[key] = me.data.settings[key]; });
      session.saveAppearance({ theme: kursatState.theme, density: kursatState.density, motion: kursatState.motion });
      kursatRenderUser(me.data.user);
      kursatApplyAppearance();
      document.dispatchEvent(new CustomEvent("kursat:defaults-changed", { detail: { model: kursatState.model, goal: kursatState.goal } }));
      document.dispatchEvent(new CustomEvent("kursat:account-loaded", { detail: me.data }));
    } else if (me.status !== 401) {
      kursatToast("Hesap bilgileri yüklenemedi", me.data?.message || "Sayfayı yenilemeyi dene.");
    }

    if (history.ok && Array.isArray(history.data)) kursatState.history = history.data;
    if (favorites.ok && Array.isArray(favorites.data)) kursatState.favorites = favorites.data;
    document.dispatchEvent(new CustomEvent("kursat:data-changed"));
  }

  // Üst bardaki avatar ve ad.
  function kursatRenderUser(user) {
    if (!user?.displayName) return;
    const userButton = document.querySelector(".kursat-user-button");
    const initials = user.displayName.trim().split(/\s+/).slice(0, 2).map(word => word[0]).join("").toLocaleUpperCase("tr-TR");
    const avatar = userButton?.querySelector(".kursat-avatar");
    const label = userButton?.querySelector("span:not(.kursat-avatar)");
    if (avatar) avatar.textContent = initials;
    if (label) label.textContent = user.displayName;
    userButton?.setAttribute("title", user.email || user.displayName);
    session?.updateUser({ id: user.id, email: user.email, displayName: user.displayName });

    // Optimize Et başlığında saate göre selamlama.
    const hour = new Date().getHours();
    const greeting = hour < 5 ? "İyi geceler" : hour < 12 ? "Günaydın" : hour < 18 ? "İyi günler" : "İyi akşamlar";
    const title = document.querySelector("#kursat-forge-title");
    if (title) title.textContent = `${greeting}, ${user.displayName.trim().split(/\s+/)[0]}`;
    const eyebrow = document.querySelector("#kursat-forge-eyebrow");
    if (eyebrow) eyebrow.textContent = "Optimize Et";
  }

  // Tema/yoğunluk/hareket sadece burada uygulanıyor, CSS geri kalanını data-* attribute'larından okuyor.
  function kursatResolveTheme() {
    if (kursatState.theme === "dark") return "dark";
    if (kursatState.theme === "light") return "light";
    return window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  function kursatApplyAppearance() {
    document.documentElement.dataset.kursatTheme = kursatResolveTheme();
    document.documentElement.dataset.kursatThemePref = kursatState.theme;
    document.documentElement.dataset.kursatMotion = kursatState.motion;
    document.documentElement.dataset.kursatDensity = kursatState.density;
    document.querySelectorAll("[data-kursat-theme-choice]").forEach(button => button.classList.toggle("is-active", button.dataset.kursatThemeChoice === kursatState.theme));
    document.querySelectorAll("[data-kursat-density-choice]").forEach(button => button.classList.toggle("is-active", button.dataset.kursatDensityChoice === kursatState.density));
    document.querySelector("#kursat-motion-toggle")?.classList.toggle("is-on", kursatState.motion === "on");
  }

  function kursatToast(title, message = "") {
    const stack = document.querySelector("#kursat-toast-stack");
    if (!stack) return;
    const item = document.createElement("div");
    item.className = "kursat-toast";
    item.innerHTML = `<strong>${kursatEscape(title)}</strong>${message ? `<span>${kursatEscape(message)}</span>` : ""}`;
    stack.appendChild(item);
    const remove = () => {
      if (!item.isConnected) return;
      item.classList.add("is-leaving");
      setTimeout(() => item.remove(), 180);
    };
    setTimeout(remove, 3200);
  }

  function kursatEscape(value = "") {
    return String(value).replace(/[&<>'"]/g, ch => ({"&":"&amp;","<":"&lt;",">":"&gt;","'":"&#39;",'"':"&quot;"}[ch]));
  }

  function kursatUid(prefix = "pf") {
    return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
  }

  function kursatFormatDate(value) {
    const date = value ? new Date(value) : new Date();
    return new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" }).format(date);
  }

  // Sadece ana blokları sırayla getiriyorum, her küçük elemana animasyon koymak göz yoruyor.
  function kursatAnimateView(target) {
    if (!target || kursatState.motion === "off") return;
    target.classList.remove("is-entering");
    void target.offsetWidth;
    target.classList.add("is-entering");
    const parts = [
      target.querySelector(":scope > .kursat-page-head"),
      target.querySelector(":scope > .kursat-model-bar"),
      target.querySelector(":scope > .kursat-forge-layout"),
      target.querySelector(":scope > .kursat-grid-3"),
      target.querySelector(":scope > .kursat-toolbar"),
      target.querySelector(":scope > .kursat-settings-grid")
    ].filter(Boolean);
    parts.forEach((part, index) => part.style.setProperty("--kursat-stagger", `${index * 45}ms`));
    setTimeout(() => target.classList.remove("is-entering"), 720);
  }

  function kursatOpenView(name, options = {}) {
    const target = document.querySelector(`#kursat-view-${name}`);
    if (!target) return;
    document.querySelectorAll(".kursat-view").forEach(el => el.classList.toggle("is-active", el === target));
    document.querySelectorAll(".kursat-nav-button[data-kursat-view]").forEach(btn => btn.classList.toggle("is-active", btn.dataset.kursatView === name));
    const meta = kursatPageMeta[name] || ["PromptForge", "Çalışma alanı"];
    document.querySelector("#kursat-context-group").textContent = meta[0];
    document.querySelector("#kursat-context-title").textContent = meta[1];
    document.title = `${meta[1]} · PromptForge`;
    history.replaceState(null, "", `#${name}`);
    kursatCloseSidebar();
    kursatAnimateView(target);
    if (!options.keepScroll) window.scrollTo({ top: 0, behavior: kursatState.motion === "off" ? "auto" : "smooth" });
    document.dispatchEvent(new CustomEvent("kursat:view", { detail: { name } }));
  }

  function kursatOpenConfirm(title, message, onConfirm) {
    kursatState.pendingConfirm = onConfirm;
    document.querySelector("#kursat-confirm-title").textContent = title;
    document.querySelector("#kursat-confirm-message").textContent = message;
    kursatOpenBackdrop(document.querySelector("#kursat-confirm-backdrop"));
  }

  function kursatOpenBackdrop(backdrop) {
    if (!backdrop) return;
    kursatState.previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    backdrop.classList.remove("is-closing");
    backdrop.classList.add("is-open");
    backdrop.setAttribute("aria-hidden", "false");
    setTimeout(() => {
      const first = backdrop.querySelector('input,button,select,textarea,[tabindex]:not([tabindex="-1"])');
      first?.focus();
    }, 15);
  }

  function kursatCloseBackdrop(backdrop, afterClose) {
    if (!backdrop || !backdrop.classList.contains("is-open")) { afterClose?.(); return; }
    if (kursatState.motion === "off") {
      backdrop.classList.remove("is-open", "is-closing");
      backdrop.setAttribute("aria-hidden", "true");
      const previous = kursatState.previousFocus;
      kursatState.previousFocus = null;
      previous?.focus?.();
      afterClose?.();
      return;
    }
    backdrop.classList.add("is-closing");
    setTimeout(() => {
      backdrop.classList.remove("is-open", "is-closing");
      backdrop.setAttribute("aria-hidden", "true");
      const previous = kursatState.previousFocus;
      kursatState.previousFocus = null;
      previous?.focus?.();
      afterClose?.();
    }, 145);
  }

  function kursatCloseConfirm() {
    const backdrop = document.querySelector("#kursat-confirm-backdrop");
    kursatCloseBackdrop(backdrop, () => { kursatState.pendingConfirm = null; });
  }

  function kursatOpenCommand() {
    const backdrop = document.querySelector("#kursat-command-backdrop");
    kursatState.currentCommandIndex = 0;
    kursatOpenBackdrop(backdrop);
    kursatRenderCommands("");
    setTimeout(() => document.querySelector("#kursat-command-search")?.focus(), 20);
  }

  function kursatCloseCommand() {
    kursatCloseBackdrop(document.querySelector("#kursat-command-backdrop"));
  }

  function kursatCommandMatches(query) {
    const needle = String(query || "").trim().toLocaleLowerCase("tr-TR");
    return kursatCommands.filter(item => !needle || `${item[0]} ${item[1]}`.toLocaleLowerCase("tr-TR").includes(needle));
  }

  function kursatRenderCommands(query) {
    const list = kursatCommandMatches(query);
    if (kursatState.currentCommandIndex >= list.length) kursatState.currentCommandIndex = Math.max(0, list.length - 1);
    const host = document.querySelector("#kursat-command-list");
    host.innerHTML = list.length ? list.map((item, index) => `<button class="kursat-command-item${index === kursatState.currentCommandIndex ? " is-current" : ""}" type="button" data-kursat-command-view="${item[2]}" data-kursat-command-index="${index}"><div><strong>${kursatEscape(item[0])}</strong><span>${kursatEscape(item[1])}</span></div><kbd>↵</kbd></button>`).join("") : `<div class="kursat-empty-mini">Eşleşen sonuç bulunamadı.</div>`;
  }

  function kursatMoveCommand(step) {
    const input = document.querySelector("#kursat-command-search");
    const list = kursatCommandMatches(input?.value || "");
    if (!list.length) return;
    kursatState.currentCommandIndex = (kursatState.currentCommandIndex + step + list.length) % list.length;
    kursatRenderCommands(input?.value || "");
    document.querySelector(".kursat-command-item.is-current")?.scrollIntoView({ block: "nearest" });
  }

  function kursatRunCurrentCommand() {
    const current = document.querySelector(".kursat-command-item.is-current");
    if (!current) return;
    kursatCloseCommand();
    kursatOpenView(current.dataset.kursatCommandView);
  }

  function kursatOpenSidebar() {
    document.querySelector("#kursat-sidebar")?.classList.add("is-open");
    document.body.classList.add("kursat-sidebar-open");
  }

  function kursatCloseSidebar() {
    document.querySelector("#kursat-sidebar")?.classList.remove("is-open");
    document.body.classList.remove("kursat-sidebar-open");
  }

  function kursatToggleQuickTheme() {
    const resolved = kursatResolveTheme();
    kursatState.theme = resolved === "dark" ? "light" : "dark";
    kursatApplyAppearance();
    kursatSaveStorage();
    kursatToast(kursatState.theme === "dark" ? "Koyu tema açık" : "Açık tema açık");
  }

  function kursatSetupAppearanceControls() {
    document.querySelector("#kursat-theme-quick")?.addEventListener("click", kursatToggleQuickTheme);
    document.querySelectorAll("[data-kursat-theme-choice]").forEach(button => button.addEventListener("click", () => {
      kursatState.theme = button.dataset.kursatThemeChoice;
      kursatApplyAppearance();
      kursatSaveStorage();
    }));
    document.querySelectorAll("[data-kursat-density-choice]").forEach(button => button.addEventListener("click", () => {
      kursatState.density = button.dataset.kursatDensityChoice;
      kursatApplyAppearance();
      kursatSaveStorage();
    }));
    document.querySelector("#kursat-motion-toggle")?.addEventListener("click", () => {
      kursatState.motion = kursatState.motion === "on" ? "off" : "on";
      kursatApplyAppearance();
      kursatSaveStorage();
      kursatToast(kursatState.motion === "on" ? "Hareket açık" : "Hareket kapalı");
    });
    window.matchMedia?.("(prefers-color-scheme: dark)").addEventListener?.("change", () => { if (kursatState.theme === "system") kursatApplyAppearance(); });
  }

  function kursatTrapModalFocus(event) {
    if (event.key !== "Tab") return;
    const backdrop = [...document.querySelectorAll(".kursat-modal-backdrop.is-open")].at(-1);
    if (!backdrop) return;
    const focusables = [...backdrop.querySelectorAll('a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])')].filter(el => !el.hidden && el.offsetParent !== null);
    if (!focusables.length) return;
    const first = focusables[0], last = focusables[focusables.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }

  function kursatBindCore() {
    document.addEventListener("click", event => {
      const viewButton = event.target.closest("[data-kursat-view]");
      if (viewButton) kursatOpenView(viewButton.dataset.kursatView);
      const commandView = event.target.closest("[data-kursat-command-view]");
      if (commandView) { kursatCloseCommand(); kursatOpenView(commandView.dataset.kursatCommandView); }
      if (event.target === document.querySelector("#kursat-command-backdrop")) kursatCloseCommand();
      if (event.target === document.querySelector("#kursat-confirm-backdrop")) kursatCloseConfirm();
      if (event.target === document.querySelector("#kursat-panel-backdrop") || event.target.closest("[data-kursat-panel-close]")) kursatCloseBackdrop(document.querySelector("#kursat-panel-backdrop"));
    });

    document.querySelector("#kursat-mobile-menu")?.addEventListener("click", kursatOpenSidebar);
    document.querySelector("#kursat-sidebar-close")?.addEventListener("click", kursatCloseSidebar);
    document.querySelector("#kursat-sidebar-backdrop")?.addEventListener("click", kursatCloseSidebar);
    document.querySelector("#kursat-open-command")?.addEventListener("click", kursatOpenCommand);
    document.querySelector("#kursat-command-search")?.addEventListener("input", event => { kursatState.currentCommandIndex = 0; kursatRenderCommands(event.target.value); });
    document.querySelector("#kursat-command-search")?.addEventListener("keydown", event => {
      if (event.key === "ArrowDown") { event.preventDefault(); kursatMoveCommand(1); }
      if (event.key === "ArrowUp") { event.preventDefault(); kursatMoveCommand(-1); }
      if (event.key === "Enter") { event.preventDefault(); kursatRunCurrentCommand(); }
    });
    document.querySelector("#kursat-confirm-close")?.addEventListener("click", kursatCloseConfirm);
    document.querySelector("#kursat-confirm-cancel")?.addEventListener("click", kursatCloseConfirm);
    document.querySelector("#kursat-confirm-ok")?.addEventListener("click", () => {
      const action = kursatState.pendingConfirm;
      kursatCloseConfirm();
      if (typeof action === "function") setTimeout(action, kursatState.motion === "off" ? 0 : 150);
    });
    document.querySelector("#kursat-signout")?.addEventListener("click", async () => {
      await window.PromptForgeSession?.signOut();
      window.location.href = "auth/log-out.html";
    });

    // /me gelene kadar girişte saklanan adı göster, boş avatar görünmesin.
    kursatRenderUser(session?.getUser());

    window.addEventListener("keydown", event => {
      kursatTrapModalFocus(event);
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") { event.preventDefault(); kursatOpenCommand(); }
      if (event.key === "Escape") {
        if (document.body.classList.contains("kursat-editor-focus")) document.dispatchEvent(new CustomEvent("kursat:close-editor-focus"));
        kursatCloseCommand();
        kursatCloseConfirm();
        kursatCloseBackdrop(document.querySelector("#kursat-panel-backdrop"));
        kursatCloseSidebar();
      }
    });

    window.addEventListener("hashchange", () => {
      const name = location.hash.replace("#", "");
      if (kursatPageMeta[name]) kursatOpenView(name, { keepScroll: true });
    });

    let scrolled = false;
    window.addEventListener("scroll", () => {
      const next = window.scrollY > 8;
      if (next === scrolled) return;
      scrolled = next;
      document.querySelector(".kursat-topbar")?.classList.toggle("is-scrolled", next);
    }, { passive: true });
  }

  kursatLoadStorage();
  kursatApplyAppearance();
  kursatBindCore();
  kursatSetupAppearanceControls();
  const initial = location.hash.replace("#", "");
  kursatOpenView(kursatPageMeta[initial] ? initial : "forge", { keepScroll: true });

  window.PF = {
    state: kursatState,
    save: kursatSaveStorage,
    toast: kursatToast,
    escape: kursatEscape,
    uid: kursatUid,
    formatDate: kursatFormatDate,
    openView: kursatOpenView,
    confirm: kursatOpenConfirm,
    closeConfirm: kursatCloseConfirm,
    applyAppearance: kursatApplyAppearance,
    openBackdrop: kursatOpenBackdrop,
    closeBackdrop: kursatCloseBackdrop,
    renderUser: kursatRenderUser,
    api: session.api
  };

  // Diğer script'ler PF'yi tanımladıktan sonra yükle, yoksa event'leri kaçırıyorlar.
  document.addEventListener("DOMContentLoaded", kursatBootstrap);
})();
