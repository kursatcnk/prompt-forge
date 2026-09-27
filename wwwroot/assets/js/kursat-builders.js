(() => {
  "use strict";
  // Oluştur bölümü: Atölye (rehberli sorular), Akışlar (düzenlenebilir adımlar), Yapı Taşları (parça parça prompt).
  // Amaç sadece prompt üretmek değil, kullanıcıya iyi promptun neden iyi olduğunu da öğretmek: her alanın yanında "neden" ipucu var.
  const PF = window.PF;
  if (!PF) return;
  const esc = PF.escape;

  // ===== ATÖLYE: her amaç için ona özel sorular =====
  // required: prompt için olmazsa olmaz alan. hint: kullanıcıya bu bilginin modele neden gerektiğini anlatır.
  const routes = [
    {
      id: "produce", title: "Bir şey üret", desc: "Metin, kod, plan veya belge oluştur.",
      icon: "M4 5h16v14H4zM8 9h8m-8 4h5",
      fields: [
        { key: "what", label: "Ne üretilecek?", required: true, placeholder: "Örn: Yeni kahve markamız için Instagram gönderi metni", hint: "Tek ve net bir görev, modelin neye odaklanacağını belirler." },
        { key: "audience", label: "Kimin için?", placeholder: "Örn: 25-35 yaş, şehirde yaşayan, üçüncü dalga kahve seven kişiler", hint: "Hedef kitle; ton, kelime seçimi ve detay seviyesini değiştirir." },
        { key: "goal", label: "Okuyan ne yapmalı veya hissetmeli?", placeholder: "Örn: Açılış haftası indiriminden haberdar olup mağazaya gelmeli", hint: "Amaç belli olunca model içeriği o sonuca göre kurar." },
        { key: "format", label: "Biçim ve uzunluk", placeholder: "Örn: En fazla 3 kısa paragraf ve 5 hashtag", hint: "Biçim söylenmezse model uzunluğu tahmin eder, genelde fazla uzun yazar." },
        { key: "avoid", label: "Nelerden kaçınmalı?", placeholder: "Örn: Klişe ifadeler, emoji kalabalığı", hint: "Yasaklar, istemediğin sonuçları baştan eler." }
      ],
      build: v => sentences(
        `${v.what}.`,
        v.audience && `Hedef kitle: ${v.audience}.`,
        v.goal && `İçeriği okuyan kişi şunu yapmalı veya hissetmeli: ${v.goal}.`,
        v.format && `Biçim ve uzunluk: ${v.format}.`,
        v.avoid && `Şunlardan kaçın: ${v.avoid}.`)
    },
    {
      id: "improve", title: "Bir şeyi iyileştir", desc: "Mevcut bir metni, kodu veya planı daha iyi hale getir.",
      icon: "M5 19 19 5M14 5h5v5M5 14v5h5",
      fields: [
        { key: "source", label: "İyileştirilecek içerik", required: true, multiline: true, placeholder: "Metni veya kodu buraya yapıştır", hint: "Modelin üzerinde çalışacağı asıl malzeme; olmadan tahmin yürütür." },
        { key: "direction", label: "Hangi yönden daha iyi olmalı?", required: true, placeholder: "Örn: Daha kısa, daha net ve daha ikna edici", hint: "\"Daha iyi yap\" belirsizdir; ölçülebilir bir yön vermek sonucu belirler." },
        { key: "keep", label: "Ne kesinlikle korunmalı?", placeholder: "Örn: Ürün adları, fiyatlar ve yasal uyarı cümlesi", hint: "Korunacakları söylemezsen model bunları da değiştirebilir." },
        { key: "audience", label: "Kimin için?", placeholder: "Örn: Teknik olmayan yöneticiler", hint: "İyileştirme okuyana göre değişir." }
      ],
      build: v => sentences(
        `Aşağıdaki içeriği şu yönden iyileştir: ${v.direction}.`,
        v.audience && `İçerik şu kişiler için: ${v.audience}.`,
        v.keep && `Şunları kesinlikle değiştirme: ${v.keep}.`,
        "Önce iyileştirilmiş hâli ver, ardından yaptığın önemli değişiklikleri kısa maddelerle açıkla.",
        `İçerik:\n"""\n${v.source}\n"""`)
    },
    {
      id: "solve", title: "Bir sorunu çöz", desc: "Hata veya problemi tanımla, nedenini bulup çözüm iste.",
      icon: "M9 18h6M10 22h4M8 14c-1.5-1-2.5-2.8-2.5-4.5a5.5 5.5 0 0 1 11 0C16.5 11.2 15.5 13 14 14H8Z",
      fields: [
        { key: "problem", label: "Sorun nedir?", required: true, placeholder: "Örn: Giriş yaptıktan sonra sayfa boş açılıyor", hint: "Belirti ne kadar net olursa neden o kadar hızlı bulunur." },
        { key: "expected", label: "Ne olması gerekiyordu?", placeholder: "Örn: Kullanıcı panoya yönlendirilmeli", hint: "Beklenen ile olan arasındaki fark, sorunun tanımıdır." },
        { key: "evidence", label: "Elindeki kanıtlar", multiline: true, placeholder: "Hata mesajı, log satırları, ilgili kod parçası", hint: "Kanıt yoksa model genel tavsiye verir; kanıt varsa noktaya iner." },
        { key: "tried", label: "Neleri denedin?", placeholder: "Örn: Önbelleği temizledim, farklı tarayıcıda denedim", hint: "Denenmişleri yazmak aynı önerilerin tekrar gelmesini engeller." },
        { key: "constraints", label: "Kısıtlar", placeholder: "Örn: Veritabanı şemasını değiştiremem", hint: "Uygulayamayacağın çözümler baştan elenir." }
      ],
      build: v => sentences(
        `Şu sorunu çözmeme yardım et: ${v.problem}.`,
        v.expected && `Beklenen davranış: ${v.expected}.`,
        v.tried && `Şunları denedim ve işe yaramadı: ${v.tried}.`,
        v.constraints && `Kısıtlar: ${v.constraints}.`,
        "Olası nedenleri olasılık sırasına göre listele, en olası neden için adım adım ve en küçük güvenli çözümü öner.",
        v.evidence && `Kanıtlar:\n"""\n${v.evidence}\n"""`)
    },
    {
      id: "decide", title: "Karar ver", desc: "Seçenekleri kriterlere göre karşılaştır.",
      icon: "M6 4h12v16H6zM9 8h6m-6 4h6m-6 4h3",
      fields: [
        { key: "decision", label: "Hangi karar verilecek?", required: true, placeholder: "Örn: Yeni projede hangi veritabanını kullanacağız?", hint: "Kararı tek soru olarak yazmak modelin dağılmasını önler." },
        { key: "options", label: "Seçenekler (her satıra bir tane)", required: true, multiline: true, placeholder: "SQL Server\nPostgreSQL\nMongoDB", hint: "Seçenekler verilmezse model kendi listesini üretir." },
        { key: "criteria", label: "Kriterler (önem sırasına göre)", placeholder: "Örn: Maliyet, ekibin deneyimi, ölçeklenebilirlik", hint: "Sıralı kriterler, eşitlikte neyin ağır basacağını belirler." },
        { key: "context", label: "Bağlam", placeholder: "Örn: 3 kişilik ekip, küçük bütçe, 2 ay içinde yayına çıkmalı", hint: "Aynı karar farklı koşullarda farklı sonuç verir." }
      ],
      build: v => sentences(
        `Şu kararı vermeme yardım et: ${v.decision}.`,
        v.context && `Bağlam: ${v.context}.`,
        `Seçenekler:\n${lines(v.options)}`,
        v.criteria && `Kriterler (önem sırasına göre): ${v.criteria}.`,
        "Seçenekleri her kritere göre karşılaştıran bir tablo hazırla, bilinmeyenleri ayrıca belirt ve sonunda gerekçeli bir öneri yap.")
    },
    {
      id: "learn", title: "Bir şey öğren", desc: "Konuyu seviyene ve süreye göre öğrenme planına çevir.",
      icon: "m3 10 9-5 9 5-9 5-9-5Zm4 2v5c3 2 7 2 10 0v-5",
      fields: [
        { key: "topic", label: "Konu nedir?", required: true, placeholder: "Örn: ASP.NET Core ile Web API geliştirme", hint: "Konu dar oldukça plan daha uygulanabilir olur." },
        { key: "level", label: "Mevcut seviyen", placeholder: "Örn: C# ve MVC biliyorum, API hiç yazmadım", hint: "Seviye bilinmezse ya çok temel ya çok ileri başlanır." },
        { key: "goal", label: "Sonunda ne yapabilmek istiyorsun?", placeholder: "Örn: Giriş sistemi olan küçük bir API yazabilmek", hint: "Somut hedef, gereksiz konuları elemeyi sağlar." },
        { key: "time", label: "Ne kadar zamanın var?", placeholder: "Örn: 2 hafta, günde 1 saat", hint: "Süre, planın derinliğini ve temposunu belirler." }
      ],
      build: v => sentences(
        `Bana şu konuyu öğretecek bir öğrenme planı hazırla: ${v.topic}.`,
        v.level && `Mevcut seviyem: ${v.level}.`,
        v.goal && `Hedefim: ${v.goal}.`,
        v.time && `Zamanım: ${v.time}.`,
        "Planı günlere veya haftalara böl; her bölümde öğrenilecek konuyu, küçük bir uygulama görevini ve öğrendiğimi nasıl kontrol edeceğimi yaz.")
    },
    {
      id: "transform", title: "Bir şeyi dönüştür", desc: "İçeriği başka bir biçime, dile veya tona çevir.",
      icon: "M7 7h11l-3-3m3 3-3 3M17 17H6l3 3m-3-3 3-3",
      fields: [
        { key: "source", label: "Kaynak içerik", required: true, multiline: true, placeholder: "Dönüştürülecek metni buraya yapıştır", hint: "Model yalnızca verdiğin içerik üzerinde çalışır." },
        { key: "target", label: "Hedef biçim", required: true, placeholder: "Örn: LinkedIn gönderisi, JSON, İngilizce e-posta", hint: "Hedef ne kadar somutsa sonuç o kadar doğrudan kullanılır." },
        { key: "keep", label: "Ne değişmemeli?", placeholder: "Örn: Rakamlar, tarihler ve özel isimler", hint: "Dönüşümde bilgi kaybını önler." },
        { key: "tone", label: "Ton ve uzunluk", placeholder: "Örn: Profesyonel, en fazla 150 kelime", hint: "Aynı içerik farklı tonda bambaşka okunur." }
      ],
      build: v => sentences(
        `Aşağıdaki içeriği şu biçime dönüştür: ${v.target}.`,
        v.tone && `Ton ve uzunluk: ${v.tone}.`,
        v.keep && `Şunlar değişmemeli: ${v.keep}.`,
        "Sadece dönüştürülmüş içeriği ver.",
        `İçerik:\n"""\n${v.source}\n"""`)
    }
  ];

  // ===== AKIŞLAR: adımları düzenlenebilir görev planları =====
  const flows = [
    { id: "debug", title: "Hata ayıklama", desc: "Belirtiyi kanıttan ayır, en küçük düzeltmeye ulaş.", steps: ["Belirtiyi net tanımla", "Son çalışan durumu belirt", "İlgili hata, log veya çıktıyı incele", "Olası nedenleri önem sırasına koy", "En küçük güvenli düzeltmeyi öner", "Düzeltmenin yan etkilerini kontrol et"] },
    { id: "research", title: "Araştırma", desc: "Soruyu daralt, kanıtı değerlendir, sonucu sentezle.", steps: ["Araştırma sorusunu sınırla", "Gerekli zaman aralığını belirt", "Güvenilir kaynak standardını tanımla", "Karşıt veya belirsiz bulguları ayır", "Sonucu kısa bir sentezle bitir"] },
    { id: "code-review", title: "Kod inceleme", desc: "Davranışı bozmadan riskleri ve karmaşıklığı bul.", steps: ["Kodun amacını özetle", "Değişmemesi gereken davranışları belirle", "Hata ve güvenlik risklerini incele", "Performans ve okunabilirlik sorunlarını ayır", "Düzeltmeleri önceliğe göre sırala", "Gerekli kod örneklerini ver"] },
    { id: "plan", title: "Teknik plan", desc: "Bir fikri adımlara ve kabul kriterlerine çevir.", steps: ["Hedefi tek cümlede tanımla", "Kapsam içini ve dışını ayır", "Bağımlılıkları belirle", "Uygulama sırasını çıkar", "Riskleri ve geri dönüş planını ekle", "Bitti sayılma kriterlerini yaz"] },
    { id: "decision", title: "Karar karşılaştırma", desc: "Seçenekleri aynı kriterlerle karşılaştır.", steps: ["Seçenekleri listele", "Karar kriterlerini ağırlıklandır", "Her seçeneğin artı ve eksilerini çıkar", "Bilinmeyenleri işaretle", "Kararı değiştirecek eşikleri belirt"] },
    { id: "content", title: "İçerik üretimi", desc: "Amaç, kitle ve ton üzerinden tutarlı içerik.", steps: ["İçeriğin amacını belirle", "Hedef kitleyi tanımla", "Ana mesajı sabitle", "Ton ve uzunluğu belirle", "Taslağı yaz", "Klişe ve tekrarları temizleyerek son düzenlemeyi yap"] },
    { id: "learning", title: "Öğrenme planı", desc: "Bir konuyu seviyeye göre adım adım öğren.", steps: ["Mevcut seviyemi birkaç soruyla ölç", "Konuyu küçük alt başlıklara böl", "Her alt başlık için kısa bir açıklama ve örnek ver", "Her bölümün sonunda küçük bir alıştırma sor", "Cevabımı değerlendirip eksiklerimi söyle"] },
    { id: "message", title: "E-posta / mesaj", desc: "Hassas veya önemli bir mesajı doğru tonda yaz.", steps: ["Mesajın amacını ve alıcıyı netleştir", "Alıcının bilmesi gereken bağlamı özetle", "Ana mesajı ilk iki cümleye koy", "Tonu alıcıyla ilişkime göre ayarla", "Net bir sonraki adım veya istekle bitir"] },
    { id: "data", title: "Veri analizi", desc: "Veriden soru, bulgu ve öneri çıkar.", steps: ["Verinin yapısını ve alanlarını özetle", "Eksik veya hatalı değerleri tespit et", "Soruyu yanıtlayacak metrikleri hesapla", "Öne çıkan bulguları ve istisnaları açıkla", "Bulgulara dayalı somut öneriler ver"] }
  ];

  // ===== YAPI TAŞLARI: iyi bir promptun parçaları =====
  // text: kullanıcı boş şablonla değil, düzenleyebileceği gerçek bir örnekle başlar.
  const blocks = [
    { id: "role", title: "Rol", why: "Modele bir uzmanlık bakış açısı verir; cevabın derinliği ve dili buna göre değişir.", text: "Deneyimli bir yazılım mimarı gibi düşün." },
    { id: "goal", title: "Amaç", why: "Modelin tam olarak neyi başarması gerektiğini tek cümlede sabitler.", text: "Görevin: Mevcut API'nin yavaş çalışan endpointlerini bul ve hızlandırma önerileri ver." },
    { id: "context", title: "Bağlam", why: "Model senin durumunu bilmez; bağlam olmadan genel geçer cevap verir.", text: "Bağlam: ASP.NET Core 8 ve SQL Server kullanan, günde 10.000 isteği olan bir e-ticaret API'si." },
    { id: "audience", title: "Hedef kitle", why: "Cevabı kimin okuyacağı; terim seçimini ve detay seviyesini belirler.", text: "Cevabı okuyacak kişi: veritabanı konusunda orta seviye bir geliştirici." },
    { id: "constraints", title: "Kısıtlar", why: "Uyulması zorunlu kuralları ayrı yazmak, modelin bunları atlamasını önler.", text: "Kurallar:\n- Mevcut API sözleşmesini değiştirme.\n- Yeni bir kütüphane ekleme." },
    { id: "tone", title: "Ton ve stil", why: "Aynı içerik farklı tonda bambaşka etki yaratır.", text: "Ton: Sade, doğrudan ve teknik; gereksiz giriş cümlesi kurma." },
    { id: "example", title: "Örnek", why: "Tek bir iyi örnek, uzun bir açıklamadan daha iyi anlatır (few-shot).", text: "Beklediğim öneri formatına örnek:\n\"Sorun: N+1 sorgu · Etki: Yüksek · Çözüm: Include ile tek sorguda yükle\"" },
    { id: "output", title: "Çıktı biçimi", why: "Biçim tanımlanmazsa model uzunluğu ve yapıyı kendisi seçer.", text: "Çıktı: Önem sırasına göre en fazla 5 öneri; her biri için sorun, etki ve kod örneği." },
    { id: "quality", title: "Kalite ölçütü", why: "İyi sonucun tanımı; model teslim etmeden önce kendini buna göre kontrol eder.", text: "İyi bir cevap: Her öneri ölçülebilir bir kazanç içermeli ve mevcut davranışı bozmamalı." },
    { id: "decision", title: "Belirsizlik kuralı", why: "Bilgi eksik olduğunda modelin tahmin mi edeceğini yoksa soru mu soracağını söyler.", text: "Bir bilgi eksikse tahmin etme; önce en fazla 3 kısa soru sor." }
  ];

  let activeRoute = null;
  let activeFlow = null;
  let flowSteps = [];
  let composed = []; // Yapı taşlarında seçilen parçalar, sıralı: [{ id, title, why, text }]

  // ----- Ortak yardımcılar -----
  function sentences(...parts) { return parts.filter(Boolean).join("\n\n").replace(/\.\./g, ".").trim(); }
  function lines(text) { return String(text || "").split(/\n+/).map(v => v.trim()).filter(Boolean).map(v => `- ${v}`).join("\n"); }
  function icon(path) { return `<span class="kursat-route-icon"><svg viewBox="0 0 24 24"><path d="${path}"/></svg></span>`; }

  // Canlı kalite göstergesi: Optimize ekranındaki analizle aynı ölçüm.
  function meter(text) {
    const analysis = window.PFForge?.analyzePrompt(text) || { score: 0, issues: [] };
    const tip = analysis.issues[0];
    return `<div class="kursat-live-meter"><div><span>Kalite</span><strong>${analysis.score}</strong></div><div class="kursat-progress"><span style="width:${analysis.score}%"></span></div><p>${tip ? `${esc(tip[0])}: ${esc(tip[1])}` : "Temel yapı iyi görünüyor."}</p></div>`;
  }

  function actions(prefix) {
    return `<div class="kursat-actions-end"><button class="kursat-button" type="button" id="${prefix}-copy">Kopyala</button><button class="kursat-button" type="button" id="${prefix}-send">Editöre aktar</button><button class="kursat-button kursat-button--accent" type="button" id="${prefix}-optimize">Aktar ve optimize et</button></div>`;
  }

  function bindActions(prefix, getText) {
    document.querySelector(`#${prefix}-send`)?.addEventListener("click", () => sendPrompt(getText(), false));
    document.querySelector(`#${prefix}-optimize`)?.addEventListener("click", () => sendPrompt(getText(), true));
    document.querySelector(`#${prefix}-copy`)?.addEventListener("click", async () => {
      try { await navigator.clipboard.writeText(getText()); PF.toast("Kopyalandı"); } catch { PF.toast("Kopyalama başarısız"); }
    });
  }

  function sendPrompt(text, optimize) {
    if (!text || !text.trim()) { PF.toast("İçerik eksik", "Önce zorunlu alanları doldur."); return; }
    document.dispatchEvent(new CustomEvent("kursat:prompt-to-forge", { detail: text }));
    if (optimize) setTimeout(() => document.querySelector("#kursat-forge-button")?.click(), 150);
  }

  function reveal(builder) {
    builder.classList.remove("kursat-hidden", "is-opening");
    void builder.offsetWidth;
    builder.classList.add("is-opening");
    setTimeout(() => builder.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block: "start" }), 30);
  }

  // ----- Atölye -----
  function renderRoutes() {
    const host = document.querySelector("#kursat-route-grid");
    host.innerHTML = routes.map(route => `<button class="kursat-route-card${activeRoute?.id === route.id ? " is-selected" : ""}" type="button" data-kursat-route="${route.id}">${icon(route.icon)}<h3>${esc(route.title)}</h3><p>${esc(route.desc)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-route]").forEach(btn => btn.addEventListener("click", () => openRoute(btn.dataset.kursatRoute)));
  }

  function openRoute(id) {
    activeRoute = routes.find(item => item.id === id); if (!activeRoute) return;
    renderRoutes();
    const builder = document.querySelector("#kursat-workshop-builder");
    builder.innerHTML = `<div class="kursat-card-head"><div><h2>${esc(activeRoute.title)}</h2><p>Zorunlu alanlar * ile işaretli. Diğerleri boş kalabilir ama her biri sonucu iyileştirir.</p></div></div>
      <div class="kursat-builder-layout">
        <div class="kursat-builder-form">${activeRoute.fields.map(field => `<div class="kursat-field"><label for="kursat-route-${field.key}">${esc(field.label)}${field.required ? " *" : ""}</label>${field.multiline
          ? `<textarea class="kursat-textarea" id="kursat-route-${field.key}" data-kursat-route-field="${field.key}" placeholder="${esc(field.placeholder)}"></textarea>`
          : `<input class="kursat-input" id="kursat-route-${field.key}" data-kursat-route-field="${field.key}" placeholder="${esc(field.placeholder)}">`}<small class="kursat-field-hint">${esc(field.hint)}</small></div>`).join("")}</div>
        <div class="kursat-builder-side"><div class="kursat-builder-preview"><header>Oluşan prompt</header><pre id="kursat-route-preview"></pre></div><div id="kursat-route-meter"></div>${actions("kursat-route")}</div>
      </div>`;
    builder.querySelectorAll("[data-kursat-route-field]").forEach(input => input.addEventListener("input", updateRoutePreview));
    bindActions("kursat-route", routeText);
    updateRoutePreview();
    reveal(builder);
    builder.querySelector("[data-kursat-route-field]")?.focus({ preventScroll: true });
  }

  function routeValues() {
    return Object.fromEntries(activeRoute.fields.map(field => [field.key, document.querySelector(`[data-kursat-route-field="${field.key}"]`)?.value.trim() || ""]));
  }

  // Zorunlu alanlar boşsa prompt üretilmez; önizlemede neyin eksik olduğu yazar.
  function routeText() {
    if (!activeRoute) return "";
    const values = routeValues();
    const missing = activeRoute.fields.filter(field => field.required && !values[field.key]);
    return missing.length ? "" : activeRoute.build(values);
  }

  function updateRoutePreview() {
    const text = routeText();
    const values = activeRoute ? routeValues() : {};
    const missing = activeRoute ? activeRoute.fields.filter(field => field.required && !values[field.key]).map(field => field.label) : [];
    document.querySelector("#kursat-route-preview").textContent = text || `Prompt oluşması için doldur: ${missing.join(", ")}`;
    document.querySelector("#kursat-route-meter").innerHTML = text ? meter(text) : "";
    return text;
  }

  // ----- Akışlar -----
  function renderFlows() {
    const host = document.querySelector("#kursat-flow-grid");
    host.innerHTML = flows.map(flow => `<button class="kursat-flow-card${activeFlow?.id === flow.id ? " is-selected" : ""}" type="button" data-kursat-flow="${flow.id}"><span class="kursat-badge">${flow.steps.length} adım</span><h3>${esc(flow.title)}</h3><p>${esc(flow.desc)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-flow]").forEach(btn => btn.addEventListener("click", () => openFlow(btn.dataset.kursatFlow)));
  }

  function openFlow(id) {
    activeFlow = flows.find(item => item.id === id); if (!activeFlow) return;
    flowSteps = [...activeFlow.steps];
    renderFlows();
    const builder = document.querySelector("#kursat-flow-builder");
    builder.innerHTML = `<div class="kursat-card-head"><div><h2>${esc(activeFlow.title)}</h2><p>Adımları kendi işine göre düzenle. Model bu sırayı izleyecek.</p></div></div>
      <div class="kursat-builder-layout">
        <div class="kursat-builder-form">
          <div class="kursat-field"><label for="kursat-flow-task">Görev *</label><textarea class="kursat-textarea" id="kursat-flow-task" placeholder="Görevi veya problemi birkaç cümleyle yaz"></textarea><small class="kursat-field-hint">Adımlar ne kadar iyi olursa olsun, görev belirsizse sonuç da belirsiz olur.</small></div>
          <div class="kursat-field"><label>Adımlar</label><div class="kursat-step-list" id="kursat-step-list"></div><button class="kursat-button kursat-button--ghost kursat-add-step" id="kursat-add-step" type="button">+ Adım ekle</button><small class="kursat-field-hint">Büyük bir işi adımlara bölmek, modelin atlama yapmadan sırayla düşünmesini sağlar.</small></div>
        </div>
        <div class="kursat-builder-side"><div class="kursat-builder-preview"><header>Oluşan prompt</header><pre id="kursat-flow-preview"></pre></div><div id="kursat-flow-meter"></div>${actions("kursat-flow")}</div>
      </div>`;
    builder.querySelector("#kursat-flow-task").addEventListener("input", updateFlowPreview);
    builder.querySelector("#kursat-add-step").addEventListener("click", () => { flowSteps.push(""); renderSteps(); document.querySelector("#kursat-step-list .kursat-step:last-child input")?.focus(); });
    bindActions("kursat-flow", flowText);
    renderSteps();
    reveal(builder);
  }

  function renderSteps() {
    const host = document.querySelector("#kursat-step-list");
    host.innerHTML = flowSteps.map((step, index) => `<div class="kursat-step"><span class="kursat-step-index">${index + 1}</span><input class="kursat-input" value="${esc(step)}" data-kursat-step="${index}" placeholder="Adımı yaz"><div class="kursat-step-actions"><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-step-up="${index}" aria-label="Yukarı taşı" ${index === 0 ? "disabled" : ""}><svg viewBox="0 0 24 24"><path d="m6 15 6-6 6 6"/></svg></button><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-step-down="${index}" aria-label="Aşağı taşı" ${index === flowSteps.length - 1 ? "disabled" : ""}><svg viewBox="0 0 24 24"><path d="m6 9 6 6 6-6"/></svg></button><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-step-remove="${index}" aria-label="Adımı sil"><svg viewBox="0 0 24 24"><path d="m6 6 12 12M18 6 6 18"/></svg></button></div></div>`).join("");
    host.querySelectorAll("[data-kursat-step]").forEach(input => input.addEventListener("input", () => { flowSteps[+input.dataset.kursatStep] = input.value; updateFlowPreview(); }));
    host.querySelectorAll("[data-kursat-step-up]").forEach(btn => btn.addEventListener("click", () => moveStep(+btn.dataset.kursatStepUp, -1)));
    host.querySelectorAll("[data-kursat-step-down]").forEach(btn => btn.addEventListener("click", () => moveStep(+btn.dataset.kursatStepDown, 1)));
    host.querySelectorAll("[data-kursat-step-remove]").forEach(btn => btn.addEventListener("click", () => { flowSteps.splice(+btn.dataset.kursatStepRemove, 1); renderSteps(); }));
    updateFlowPreview();
  }

  function moveStep(index, direction) {
    const target = index + direction;
    if (target < 0 || target >= flowSteps.length) return;
    [flowSteps[index], flowSteps[target]] = [flowSteps[target], flowSteps[index]];
    renderSteps();
  }

  function flowText() {
    const task = document.querySelector("#kursat-flow-task")?.value.trim();
    const steps = flowSteps.map(step => step.trim()).filter(Boolean);
    if (!task || !steps.length) return "";
    return `${task}\n\nBu görevi aşağıdaki adımları sırayla izleyerek yap. Her adımın sonucunu kısa bir başlık altında göster, bir adımı atlaman gerekiyorsa nedenini yaz:\n${steps.map((step, index) => `${index + 1}. ${step}`).join("\n")}\n\nSon olarak tüm adımlardan çıkan sonucu birkaç cümleyle özetle.`;
  }

  function updateFlowPreview() {
    const text = flowText();
    document.querySelector("#kursat-flow-preview").textContent = text || "Prompt oluşması için görevi yaz ve en az bir adım bırak.";
    document.querySelector("#kursat-flow-meter").innerHTML = text ? meter(text) : "";
    return text;
  }

  // ----- Yapı taşları -----
  function renderBlocks() {
    const host = document.querySelector("#kursat-block-grid");
    const selected = new Set(composed.map(item => item.id));
    host.innerHTML = blocks.map(block => `<button class="kursat-block-card${selected.has(block.id) ? " is-selected" : ""}" type="button" data-kursat-block="${block.id}"><div class="kursat-block-card-head"><h3>${esc(block.title)}</h3><span class="kursat-block-state">${selected.has(block.id) ? "Eklendi" : "+ Ekle"}</span></div><p>${esc(block.why)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-block]").forEach(btn => btn.addEventListener("click", () => {
      const block = blocks.find(v => v.id === btn.dataset.kursatBlock); if (!block) return;
      const index = composed.findIndex(item => item.id === block.id);
      if (index >= 0) composed.splice(index, 1); else composed.push({ ...block });
      renderBlocks(); renderComposer();
    }));
  }

  function renderComposer() {
    const stack = document.querySelector("#kursat-composer-stack");
    stack.innerHTML = composed.length ? composed.map((block, index) => `<div class="kursat-composer-item"><div class="kursat-composer-item-head"><strong>${esc(block.title)}</strong><div class="kursat-step-actions"><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-block-up="${index}" aria-label="Yukarı taşı" ${index === 0 ? "disabled" : ""}><svg viewBox="0 0 24 24"><path d="m6 15 6-6 6 6"/></svg></button><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-block-down="${index}" aria-label="Aşağı taşı" ${index === composed.length - 1 ? "disabled" : ""}><svg viewBox="0 0 24 24"><path d="m6 9 6 6 6-6"/></svg></button><button class="kursat-editor-action kursat-editor-action--icon" type="button" data-kursat-remove-block="${index}" aria-label="Kaldır"><svg viewBox="0 0 24 24"><path d="m6 6 12 12M18 6 6 18"/></svg></button></div></div><textarea class="kursat-textarea" data-kursat-block-text="${index}">${esc(block.text)}</textarea><small class="kursat-field-hint">${esc(block.why)}</small></div>`).join("")
      : `<div class="kursat-empty-mini">Soldaki kartlardan parça ekle. Başlamak için Amaç, Bağlam ve Çıktı biçimi iyi bir üçlüdür.</div>`;
    stack.querySelectorAll("[data-kursat-block-text]").forEach(input => input.addEventListener("input", () => { composed[+input.dataset.kursatBlockText].text = input.value; updateBlockPreview(); }));
    stack.querySelectorAll("[data-kursat-remove-block]").forEach(btn => btn.addEventListener("click", () => { composed.splice(+btn.dataset.kursatRemoveBlock, 1); renderBlocks(); renderComposer(); }));
    stack.querySelectorAll("[data-kursat-block-up]").forEach(btn => btn.addEventListener("click", () => moveBlock(+btn.dataset.kursatBlockUp, -1)));
    stack.querySelectorAll("[data-kursat-block-down]").forEach(btn => btn.addEventListener("click", () => moveBlock(+btn.dataset.kursatBlockDown, 1)));
    updateBlockPreview();
  }

  function moveBlock(index, direction) {
    const target = index + direction;
    if (target < 0 || target >= composed.length) return;
    [composed[index], composed[target]] = [composed[target], composed[index]];
    renderComposer();
  }

  function blockText() { return composed.map(item => item.text.trim()).filter(Boolean).join("\n\n"); }

  function updateBlockPreview() {
    const text = blockText();
    document.querySelector("#kursat-block-preview").textContent = text || "Seçtiğin parçalar burada birleşecek.";
    const meterHost = document.querySelector("#kursat-block-meter");
    if (meterHost) meterHost.innerHTML = text ? meter(text) : "";
    return text;
  }

  document.querySelector("#kursat-blocks-to-forge")?.addEventListener("click", () => sendPrompt(blockText(), false));
  document.querySelector("#kursat-blocks-optimize")?.addEventListener("click", () => sendPrompt(blockText(), true));
  renderRoutes(); renderFlows(); renderBlocks(); renderComposer();
})();
