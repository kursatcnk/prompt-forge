// Rehber sayfası ve ilk girişteki tanıtım turu.
// Görevlerin çoğu gerçek veriden hesaplanıyor (geçmiş, favori, 2FA); "şablon kullandı", "analiz yaptı" gibi
// sunucuda karşılığı olmayanları localStorage'da tutuyorum.
(() => {
  "use strict";

  const esc = value => PF.escape(value);
  const userId = () => PF.state.account?.user?.id || window.PromptForgeSession?.getUser()?.id || "anon";
  const storeKey = name => `promptforge.${name}.${userId()}`;

  function readMilestones() {
    try { return JSON.parse(localStorage.getItem(storeKey("milestones")) || "{}"); } catch { return {}; }
  }
  function markMilestone(name) {
    const data = readMilestones();
    if (data[name]) return;
    data[name] = Date.now();
    try { localStorage.setItem(storeKey("milestones"), JSON.stringify(data)); } catch { /* depolama kapalı olabilir */ }
    render();
  }

  function tasks() {
    const user = PF.state.account?.user || {};
    const m = readMilestones();
    return [
      { done: !!user.emailConfirmed, title: "E-posta adresini doğrula", copy: "Şifreni unutursan hesabını kurtarabilmen için gerekli.", view: "settings", action: "Ayarlara git" },
      { done: PF.state.history.length > 0, title: "İlk promptunu optimize et", copy: "Bir prompt yaz, hedef modeli seç ve Optimize Et'e bas.", view: "forge", action: "Editörü aç" },
      { done: !!m.template, title: "Bir şablon kullan", copy: "Hazır bir şablonu seç, boşlukları doldur ve editöre aktar.", view: "templates", action: "Şablonlara bak" },
      { done: !!m.analysis, title: "Promptunu analiz ettir", copy: "6 ölçütteki puanını ve nasıl düzelteceğini öğren.", view: "health", action: "Analize git" },
      { done: PF.state.favorites.length > 0, title: "Bir sonucu favorilere ekle", copy: "Beğendiğin promptları sonra tek tıkla tekrar kullan.", view: "history", action: "Geçmişi aç" },
      { done: !!user.twoFactorEnabled, title: "İki adımlı doğrulamayı aç", copy: "Hesabını şifre dışında telefonundaki kodla da koru.", view: "settings", action: "Güvenliği aç" }
    ];
  }

  const tools = [
    ["forge", "Optimize Et", "Elinde bir prompt varsa.", "Promptunu seçtiğin modele göre yeniden yazar. Hedefi “En iyi sonuç” seçersen eksik bağlamı da tamamlar."],
    ["templates", "Şablonlar", "Ne yazacağını biliyor ama nasıl başlayacağını bilmiyorsan.", "Denenmiş bir yapıyı seç, sadece boşlukları doldur."],
    ["workshop", "Atölye", "Elinde hiçbir şey yoksa.", "Amacını seç ve birkaç soruyu yanıtla; prompt sen yazdıkça oluşur."],
    ["flows", "Akışlar", "İş birden fazla adımdan oluşuyorsa.", "Modelin sırayla izleyeceği bir plan çıkarır; adımları kendin düzenlersin."],
    ["blocks", "Yapı Taşları", "Promptun parçalarını tek tek kontrol etmek istiyorsan.", "Rol, bağlam, kısıt, çıktı gibi parçaları ekleyip sıralarsın."],
    ["health", "Prompt Analizi", "Promptunun neden iyi ya da kötü çalıştığını öğrenmek istiyorsan.", "6 ölçütte puanlar ve her eksik için somut düzeltme önerir."],
    ["compare", "Karşılaştır", "İki sürüm arasında kararsızsan.", "Geçmişten iki kaydı yan yana koyar, puan ve metin farkını gösterir."]
  ];

  // Prompt Analizi'ndeki 6 ölçütle aynı: [ad, kural, zayıf örnek, güçlü örnek]
  const lessons = [
    ["Netlik", "Tek bir işi, tek bir fiille iste.", "Bunu daha iyi yap.", "Aşağıdaki e-postayı daha kısa ve daha resmi olacak şekilde yeniden yaz."],
    ["Bağlam", "Kim için ve ne amaçla olduğunu söyle.", "Bir ürün açıklaması yaz.", "25–40 yaş kadın müşterilere satış yapan e-ticaret sitemiz için ürün açıklaması yaz."],
    ["Özgüllük", "Modelin tahmin etmek zorunda kalacağı her şeyi somutlaştır.", "Trençkot için güzel bir metin.", "Trençkot: %100 pamuk gabardin, 3 renk, kemerli kesim."],
    ["Kısıtlar", "Sınırları ve yapılmaması gerekenleri yaz.", "Kısa olsun.", "En fazla 120 kelime. “Lüks” kelimesini kullanma. Mevcut davranışı değiştirme."],
    ["Çıktı biçimi", "Sonucun şeklini tarif et.", "Bana fikir ver.", "5 fikir ver; her biri başlık + tek cümle açıklama olsun, tablo hâlinde."],
    ["Verimlilik", "Tekrarları ve dolgu cümleleri çıkar.", "Lütfen eğer mümkünse, gerçekten çok iyi, detaylı ve kaliteli bir şekilde…", "Kaliteli bir sonuç için şu 3 kurala uy: …"]
  ];

  const faq = [
    ["Optimize ettiğim promptu nerede kullanacağım?", "Sonucu kopyalayıp ChatGPT, Claude, Gemini gibi istediğin araca yapıştır. PromptForge promptu hazırlar, cevabı o araç üretir."],
    ["Hangi modeli seçmeliyim?", "Promptu hangi araçta kullanacaksan onu seç. Emin değilsen veya birden fazla araçta kullanacaksan “Evrensel” seç."],
    ["“En iyi sonuç”, “Dengeli”, “En kısa” farkı ne?", "En iyi sonuç eksik bağlamı ekleyerek genişletir. Dengeli aynı uzunlukta düzenler. En kısa kuralları koruyarak kısaltır; sık kullandığın promptlarda maliyeti düşürür."],
    ["{{…}} işaretleri ne anlama geliyor?", "Bunlar değişkenlerdir: her kullanımda değişecek bilgilerin yeri. Editörün altındaki değişken rozetine tıklayarak hepsini tek pencerede doldurabilirsin."],
    ["Aylık hakkım neye göre düşüyor?", "Her optimizasyon ve her AI analizi bir hak kullanır. Şablonlar, Atölye, Akışlar, Yapı Taşları ve kütüphane hak harcamaz."],
    ["Verilerimi nasıl silerim?", "Geçmiş sayfasında “Geçmişi temizle” ile tüm kayıtları, Ayarlar > Tehlikeli bölge'den de hesabını tamamen silebilirsin."]
  ];

  function renderChecklist() {
    const host = document.querySelector("#kursat-guide-checklist");
    if (!host) return;
    const list = tasks();
    const done = list.filter(t => t.done).length;
    const pct = Math.round(done / list.length * 100);
    host.innerHTML = `
      <div class="kursat-card-head"><div><h2>Başlarken</h2><p>${done === list.length ? "Hepsini tamamladın. PromptForge'u tüm yönleriyle kullanıyorsun." : "Bu adımlar PromptForge'un en faydalı kısımlarını tanıtır."}</p></div><span class="kursat-badge ${done === list.length ? "kursat-badge--success" : "kursat-badge--accent"}">${done}/${list.length}</span></div>
      <div class="kursat-guide-progress"><div class="kursat-progress"><span style="width:${pct}%"></span></div></div>
      <ol class="kursat-checklist">${list.map(t => `
        <li class="${t.done ? "is-done" : ""}">
          <span class="kursat-check-mark" aria-hidden="true">${t.done ? `<svg viewBox="0 0 24 24"><path d="m6 12 4 4 8-8"/></svg>` : ""}</span>
          <div><strong>${esc(t.title)}</strong><span>${esc(t.copy)}</span></div>
          ${t.done ? `<span class="kursat-check-state">Tamamlandı</span>` : `<button class="kursat-button" type="button" data-kursat-view="${t.view}">${esc(t.action)}</button>`}
        </li>`).join("")}</ol>`;

    // Menüdeki 2/6 rozeti; hepsi bitince kayboluyor.
    const badge = document.querySelector("#kursat-guide-badge");
    if (badge) { badge.textContent = `${done}/${list.length}`; badge.classList.toggle("kursat-hidden", done === list.length); }
  }

  function renderStatic() {
    const toolsHost = document.querySelector("#kursat-guide-tools");
    if (toolsHost && !toolsHost.childElementCount) toolsHost.innerHTML = tools.map(([view, title, when, what]) => `
      <button type="button" class="kursat-guide-tool" data-kursat-view="${view}">
        <strong>${esc(title)}</strong><span class="kursat-guide-when">${esc(when)}</span><span>${esc(what)}</span>
        <span class="kursat-template-open">Aç <svg viewBox="0 0 24 24"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></span>
      </button>`).join("");

    const lessonHost = document.querySelector("#kursat-guide-lessons");
    if (lessonHost && !lessonHost.childElementCount) lessonHost.innerHTML = lessons.map(([name, rule, weak, strong], i) => `
      <article class="kursat-lesson">
        <header><b>${String(i + 1).padStart(2, "0")}</b><div><strong>${esc(name)}</strong><span>${esc(rule)}</span></div></header>
        <div class="kursat-lesson-pair">
          <div class="kursat-lesson-weak"><small>Zayıf</small><p>${esc(weak)}</p></div>
          <div class="kursat-lesson-strong"><small>Güçlü</small><p>${esc(strong)}</p></div>
        </div>
      </article>`).join("");

    const faqHost = document.querySelector("#kursat-guide-faq");
    if (faqHost && !faqHost.childElementCount) faqHost.innerHTML = faq.map(([q, a]) => `<details class="kursat-faq-item"><summary>${esc(q)}</summary><p>${esc(a)}</p></details>`).join("");
  }

  function render() {
    renderChecklist();
    renderStatic();
  }

  // İlk girişte bir kez açılan 3 adımlık tur.
  const tourSteps = [
    { kicker: "Hoş geldin", title: "PromptForge promptlarını güçlendirir.", copy: "Yazdığın promptu okur, eksik bağlamı ve belirsiz ifadeleri bulur, seçtiğin modelin en iyi anladığı biçimde yeniden yazar.", visual: `<div class="kursat-tour-visual"><div class="kursat-tour-before">Blog yazısı yaz.</div><svg viewBox="0 0 24 24"><path d="M5 12h14m-5-5 5 5-5 5"/></svg><div class="kursat-tour-after"><b>Rol</b> · <b>Görev</b> · <b>Bağlam</b> · <b>Kurallar</b> · <b>Çıktı</b></div></div>` },
    { kicker: "Nasıl kullanılır", title: "Üç adım yeterli.", copy: "", visual: `<ol class="kursat-tour-steps"><li><b>1</b><div><strong>Yaz</strong><span>Promptunu editöre yaz ya da bir şablonla başla.</span></div></li><li><b>2</b><div><strong>Seç</strong><span>Hedef modeli (GPT, Claude, Gemini…) ve hedefini seç.</span></div></li><li><b>3</b><div><strong>Kullan</strong><span>Sonucu kopyala, favorile, sürümleri karşılaştır.</span></div></li></ol>` },
    { kicker: "Nereden başlamalı", title: "Sana uygun bir başlangıç seç.", copy: "Bu turu dilediğin zaman soldaki menüden Rehber'e girerek tekrar açabilirsin.", visual: `<div class="kursat-tour-choices"><button type="button" data-kursat-tour-go="templates"><strong>Şablonla başla</strong><span>Hazır yapıyı seç, boşlukları doldur.</span></button><button type="button" data-kursat-tour-go="forge"><strong>Kendim yazacağım</strong><span>Doğrudan editöre geç.</span></button><button type="button" data-kursat-tour-go="guide"><strong>Önce öğreneyim</strong><span>Rehberi ve iyi prompt derslerini aç.</span></button></div>` }
  ];

  function openTour(step = 0) {
    const s = tourSteps[step];
    document.querySelector("#kursat-panel-title").textContent = "PromptForge'a hoş geldin";
    const body = document.querySelector("#kursat-panel-body");
    body.innerHTML = `
      <div class="kursat-tour">
        <div class="kursat-tour-dots">${tourSteps.map((_, i) => `<i class="${i === step ? "is-active" : ""}"></i>`).join("")}</div>
        <p class="kursat-tour-kicker">${esc(s.kicker)}</p>
        <h3 class="kursat-tour-title">${esc(s.title)}</h3>
        ${s.copy ? `<p class="kursat-modal-copy">${esc(s.copy)}</p>` : ""}
        ${s.visual}
      </div>
      <div class="kursat-actions-end">
        ${step > 0 ? `<button class="kursat-button kursat-button--ghost" type="button" data-kursat-tour-step="${step - 1}">Geri</button>` : `<button class="kursat-button kursat-button--ghost" type="button" data-kursat-panel-close>Atla</button>`}
        ${step < tourSteps.length - 1 ? `<button class="kursat-button kursat-button--primary" type="button" data-kursat-tour-step="${step + 1}">Devam</button>` : ""}
      </div>`;
    const backdrop = document.querySelector("#kursat-panel-backdrop");
    if (!backdrop.classList.contains("is-open")) PF.openBackdrop(backdrop);
    try { localStorage.setItem(storeKey("welcomed"), "1"); } catch { /* yok say */ }
  }

  function bind() {
    document.querySelector("#kursat-panel-body")?.addEventListener("click", event => {
      const step = event.target.closest("[data-kursat-tour-step]");
      if (step) { openTour(Number(step.dataset.kursatTourStep)); return; }
      const go = event.target.closest("[data-kursat-tour-go]");
      if (go) PF.closeBackdrop(document.querySelector("#kursat-panel-backdrop"), () => PF.openView(go.dataset.kursatTourGo));
    });
    document.querySelector("#kursat-guide-tour")?.addEventListener("click", () => openTour(0));

    document.addEventListener("kursat:milestone", event => markMilestone(event.detail));
    document.addEventListener("kursat:view", event => { if (event.detail.name === "guide") render(); });
    document.addEventListener("kursat:account-loaded", render);
    // data-changed bootstrap'in en sonunda geliyor; geçmiş yüklenmeden karar verirsem eski kullanıcılara da tur açılıyor.
    let tourChecked = false;
    document.addEventListener("kursat:data-changed", () => {
      render();
      if (tourChecked || !PF.state.account) return;
      tourChecked = true;
      let welcomed = false;
      try { welcomed = !!localStorage.getItem(storeKey("welcomed")); } catch { welcomed = true; }
      if (!welcomed && PF.state.history.length === 0) setTimeout(() => openTour(0), 500);
    });
  }

  bind();
  render();
  window.PFGuide = { openTour, markMilestone };
})();
