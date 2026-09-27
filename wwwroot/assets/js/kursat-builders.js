(() => {
  "use strict";
  const PF = window.PF;
  if (!PF) return;

  const routes = [
    { id:"produce", title:"Bir şey üret", desc:"İçerik, kod, plan veya belge oluştur.", fields:["Ne üretilecek?","Kimin için?","Hangi biçimde teslim edilmeli?"], icon:"M4 5h16v14H4zM8 9h8m-8 4h5" },
    { id:"improve", title:"Bir şeyi iyileştir", desc:"Mevcut bir çıktıyı netleştir, sadeleştir veya güçlendir.", fields:["Neyi iyileştiriyoruz?","Neyi kesinlikle korumalı?","Nasıl daha iyi olmalı?"], icon:"M5 19 19 5M14 5h5v5M5 14v5h5" },
    { id:"solve", title:"Bir sorunu çöz", desc:"Sorunu tanımla, olası nedenleri daralt ve çözüm iste.", fields:["Sorun nedir?","Elindeki kanıtlar neler?","Hangi kısıtlar var?"], icon:"M9 18h6M10 22h4M8 14c-1.5-1-2.5-2.8-2.5-4.5a5.5 5.5 0 0 1 11 0C16.5 11.2 15.5 13 14 14H8Z" },
    { id:"decide", title:"Karar ver", desc:"Seçenekleri kriterlere göre karşılaştır ve belirsizliği azalt.", fields:["Hangi karar verilecek?","Seçenekler neler?","En önemli kriterler hangileri?"], icon:"M6 4h12v16H6zM9 8h6m-6 4h6m-6 4h3" },
    { id:"learn", title:"Bir şeyi öğren", desc:"Konuyu seviyene ve hedef sürene göre öğrenme planına dönüştür.", fields:["Konu nedir?","Mevcut seviyen nedir?","Sonunda ne yapabiliyor olmak istiyorsun?"], icon:"m3 10 9-5 9 5-9 5-9-5Zm4 2v5c3 2 7 2 10 0v-5" },
    { id:"transform", title:"Bir şeyi dönüştür", desc:"Metni, yapıyı veya formatı başka bir biçime çevir.", fields:["Kaynak içerik nedir?","Hedef biçim nedir?","Neler değişmemeli?"], icon:"M7 7h11l-3-3m3 3-3 3M17 17H6l3 3m-3-3 3-3" }
  ];

  const flows = [
    { id:"debug", title:"Hata ayıklama", desc:"Belirtiyi kanıttan ayır ve en küçük düzeltmeye ulaş.", steps:["Belirtiyi net tanımla","Son çalışan durumu belirt","İlgili hata/log/çıktıyı ekle","Olası nedenleri önem sırasına koy","En küçük güvenli düzeltmeyi iste","Düzeltmenin yan etkilerini kontrol et"] },
    { id:"research", title:"Araştırma", desc:"Soruyu daralt, kanıt standardını belirle ve sonucu yapılandır.", steps:["Araştırma sorusunu sınırla","Gerekli zaman aralığını belirt","Güvenilir kaynak standardını tanımla","Karşıt veya belirsiz bulguları ayır","Sonucu kısa bir sentezle bitir"] },
    { id:"code-review", title:"Kod inceleme", desc:"Davranışı bozmadan riskleri ve gereksiz karmaşıklığı bul.", steps:["Kodun amacını belirt","Değişmemesi gereken davranışları sabitle","Hata ve güvenlik risklerini incele","Performans ve okunabilirlik sorunlarını ayır","Öncelikli düzeltmeleri sırala","Gerekli kod örneklerini ver"] },
    { id:"plan", title:"Teknik plan", desc:"Bir fikri uygulanabilir adımlara ve kabul kriterlerine çevir.", steps:["Hedefi tek cümlede tanımla","Kapsam içi ve dışını ayır","Bağımlılıkları belirle","Uygulama sırasını çıkar","Riskleri ve geri dönüş planını ekle","Bitti sayılma kriterlerini yaz"] },
    { id:"decision", title:"Karar karşılaştırma", desc:"Seçenekleri aynı kriter setiyle karşılaştır.", steps:["Seçenekleri listele","Karar kriterlerini ağırlıklandır","Her seçeneğin artı ve eksilerini ayır","Bilinmeyenleri açıkça işaretle","Kararı değiştirecek eşikleri belirt"] },
    { id:"content", title:"İçerik üretimi", desc:"Amaç, hedef kitle ve ton üzerinden tutarlı içerik üret.", steps:["İçeriğin amacını belirle","Hedef kitleyi tanımla","Ana mesajı sabitle","Ton ve uzunluğu belirt","Kaçınılacak klişeleri yaz","Son düzenleme kontrolünü iste"] }
  ];

  const blocks = [
    { id:"goal", title:"Amaç", desc:"Modelin neyi başarması gerektiğini sabitler.", text:"AMAÇ\n[Buraya görevin tek ve açık amacını yaz.]", icon:"M12 3a9 9 0 1 0 9 9M12 7v5l3 2" },
    { id:"context", title:"Bağlam", desc:"Karar vermek için gereken mevcut durumu ekler.", text:"BAĞLAM\n[Modelin bilmesi gereken mevcut durumu, girdileri veya sınırları yaz.]", icon:"M4 5h16v14H4zM8 9h8m-8 4h8" },
    { id:"constraints", title:"Kısıtlar", desc:"Değişmemesi gereken kuralları ayrı tutar.", text:"ZORUNLU KISITLAR\n- [Korunması gereken kural]\n- [Yapılmaması gereken şey]", icon:"M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3Z" },
    { id:"quality", title:"Kalite ölçütü", desc:"İyi sonucun nasıl anlaşılacağını tanımlar.", text:"KALİTE ÖLÇÜTLERİ\n- [Çıktının karşılaması gereken kalite şartı]\n- [Kontrol edilmesi gereken risk]", icon:"m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9-5.4 2.9 1-6.1-4.4-4.3 6.1-.9Z" },
    { id:"output", title:"Çıktı biçimi", desc:"Sonucun biçimini ve uzunluğunu netleştirir.", text:"ÇIKTI\n[İstenen formatı, uzunluğu ve bölümleri yaz.]", icon:"M5 4h14v16H5zM8 8h8m-8 4h8m-8 4h5" },
    { id:"decision", title:"Karar kuralı", desc:"Belirsizlik varsa modelin nasıl karar vereceğini söyler.", text:"KARAR KURALI\n[Belirsizlik olduğunda hangi kriterin öncelikli olacağını yaz.]", icon:"M8 5h8m-8 7h8m-8 7h5M5 3h14v18H5z" }
  ];

  let activeRoute = null;
  let activeFlow = null;
  const blockState = new Map();

  function routeIcon(path) { return `<span class="kursat-route-icon"><svg viewBox="0 0 24 24"><path d="${path}"/></svg></span>`; }

  function renderRoutes() {
    const host = document.querySelector("#kursat-route-grid");
    host.innerHTML = routes.map(route => `<button class="kursat-route-card" type="button" data-kursat-route="${route.id}">${routeIcon(route.icon)}<h3>${PF.escape(route.title)}</h3><p>${PF.escape(route.desc)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-route]").forEach(btn => btn.addEventListener("click", () => openRoute(btn.dataset.kursatRoute)));
  }

  function openRoute(id) {
    activeRoute = routes.find(item => item.id === id); if (!activeRoute) return;
    const builder = document.querySelector("#kursat-workshop-builder");
    builder.classList.remove("kursat-hidden");
    builder.classList.remove("is-opening");
    void builder.offsetWidth;
    builder.classList.add("is-opening");
    builder.innerHTML = `<div class="kursat-card-head"><div><h3>${PF.escape(activeRoute.title)}</h3><p>${PF.escape(activeRoute.desc)}</p></div></div><div class="kursat-card-body"><div class="kursat-form-grid">${activeRoute.fields.map((field,index) => `<div class="kursat-field"><label for="kursat-route-field-${index}">${PF.escape(field)}</label><textarea class="kursat-textarea" id="kursat-route-field-${index}" data-kursat-route-field="${index}" placeholder="Kısa ve somut yaz..."></textarea></div>`).join("")}</div><div class="kursat-builder-preview"><header>Canlı prompt</header><pre id="kursat-route-preview"></pre></div><div class="kursat-actions-end"><button class="kursat-button" id="kursat-route-to-blocks" type="button">Yapı taşlarına çevir</button><button class="kursat-button kursat-button--primary" id="kursat-route-to-forge" type="button">Editöre aktar</button></div></div>`;
    builder.querySelectorAll("[data-kursat-route-field]").forEach(input => input.addEventListener("input", updateRoutePreview));
    builder.querySelector("#kursat-route-to-forge").addEventListener("click", () => sendPrompt(updateRoutePreview()));
    builder.querySelector("#kursat-route-to-blocks").addEventListener("click", () => routeToBlocks());
    updateRoutePreview();
    setTimeout(() => builder.classList.remove("is-opening"), 420);
    builder.scrollIntoView({ behavior:PF.state.motion === "off" ? "auto" : "smooth", block:"center" });
  }

  function updateRoutePreview() {
    if (!activeRoute) return "";
    const values = activeRoute.fields.map((field,index) => document.querySelector(`#kursat-route-field-${index}`)?.value.trim() || `[${field}]`);
    const text = [`AMAÇ\n${values[0]}`, values[1] ? `BAĞLAM\n${values[1]}` : "", values[2] ? `ÇIKTI / KISIT\n${values[2]}` : ""].filter(Boolean).join("\n\n");
    const preview = document.querySelector("#kursat-route-preview"); if (preview) preview.textContent = text;
    return text;
  }

  function routeToBlocks() {
    const text = updateRoutePreview();
    blockState.clear();
    blockState.set("goal", { ...blocks.find(v => v.id === "goal"), text: text.split("\n\n")[0] || text });
    blockState.set("context", { ...blocks.find(v => v.id === "context"), text: text.split("\n\n")[1] || blocks.find(v => v.id === "context").text });
    blockState.set("output", { ...blocks.find(v => v.id === "output"), text: text.split("\n\n")[2] || blocks.find(v => v.id === "output").text });
    renderBlocks(); renderComposer(); PF.openView("blocks");
  }

  function renderFlows() {
    const host = document.querySelector("#kursat-flow-grid");
    host.innerHTML = flows.map(flow => `<button class="kursat-flow-card" type="button" data-kursat-flow="${flow.id}"><span class="kursat-badge">${flow.steps.length} adım</span><h3>${PF.escape(flow.title)}</h3><p>${PF.escape(flow.desc)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-flow]").forEach(btn => btn.addEventListener("click", () => openFlow(btn.dataset.kursatFlow)));
  }

  function openFlow(id) {
    activeFlow = flows.find(item => item.id === id); if (!activeFlow) return;
    const builder = document.querySelector("#kursat-flow-builder");
    builder.classList.remove("kursat-hidden");
    builder.classList.remove("is-opening");
    void builder.offsetWidth;
    builder.classList.add("is-opening");
    builder.innerHTML = `<div class="kursat-card-head"><div><h3>${PF.escape(activeFlow.title)}</h3><p>${PF.escape(activeFlow.desc)}</p></div></div><div class="kursat-card-body"><div class="kursat-field"><label for="kursat-flow-task">Bu akışta çözülecek görev</label><textarea class="kursat-textarea" id="kursat-flow-task" placeholder="Görevi veya problemi yaz..."></textarea></div><div class="kursat-flow-steps">${activeFlow.steps.map((step,index) => `<label class="kursat-flow-step"><input type="checkbox" checked data-kursat-flow-step="${index}"><span><strong>${index+1}. adım</strong><span>${PF.escape(step)}</span></span></label>`).join("")}</div><div class="kursat-builder-preview"><header>Oluşan akış promptu</header><pre id="kursat-flow-preview"></pre></div><div class="kursat-actions-end"><button class="kursat-button kursat-button--primary" id="kursat-flow-to-forge" type="button">Editöre aktar</button></div></div>`;
    builder.querySelector("#kursat-flow-task").addEventListener("input", updateFlowPreview);
    builder.querySelectorAll("[data-kursat-flow-step]").forEach(input => input.addEventListener("change", updateFlowPreview));
    builder.querySelector("#kursat-flow-to-forge").addEventListener("click", () => sendPrompt(updateFlowPreview()));
    updateFlowPreview(); setTimeout(() => builder.classList.remove("is-opening"), 420); builder.scrollIntoView({ behavior:PF.state.motion === "off" ? "auto" : "smooth", block:"center" });
  }

  function updateFlowPreview() {
    if (!activeFlow) return "";
    const task = document.querySelector("#kursat-flow-task")?.value.trim() || "[Görev]";
    const enabled = activeFlow.steps.filter((_,index) => document.querySelector(`[data-kursat-flow-step="${index}"]`)?.checked);
    const text = `GÖREV\n${task}\n\nÇALIŞMA AKIŞI\n${enabled.map((step,index) => `${index+1}. ${step}`).join("\n")}\n\nÇIKTI\nHer adımı uygulayıp yalnızca gerekli sonucu ver.`;
    const preview = document.querySelector("#kursat-flow-preview"); if (preview) preview.textContent = text;
    return text;
  }

  function renderBlocks() {
    const host = document.querySelector("#kursat-block-grid");
    host.innerHTML = blocks.map(block => `<button class="kursat-block-card ${blockState.has(block.id) ? "is-selected" : ""}" type="button" data-kursat-block="${block.id}">${routeIcon(block.icon)}<h3>${PF.escape(block.title)}</h3><p>${PF.escape(block.desc)}</p></button>`).join("");
    host.querySelectorAll("[data-kursat-block]").forEach(btn => btn.addEventListener("click", () => {
      const block = blocks.find(v => v.id === btn.dataset.kursatBlock); if (!block) return;
      if (blockState.has(block.id)) blockState.delete(block.id); else blockState.set(block.id, { ...block });
      renderBlocks(); renderComposer();
    }));
  }

  function renderComposer() {
    const stack = document.querySelector("#kursat-composer-stack");
    const items = [...blockState.values()];
    stack.innerHTML = items.length ? items.map(block => `<div class="kursat-composer-item"><div class="kursat-composer-item-head"><strong>${PF.escape(block.title)}</strong><button class="kursat-button kursat-button--ghost" type="button" data-kursat-remove-block="${block.id}">Kaldır</button></div><textarea class="kursat-textarea" data-kursat-block-text="${block.id}">${PF.escape(block.text)}</textarea></div>`).join("") : `<div class="kursat-empty-mini">Yukarıdan bir veya daha fazla yapı taşı seç.</div>`;
    stack.querySelectorAll("[data-kursat-remove-block]").forEach(btn => btn.addEventListener("click", () => { blockState.delete(btn.dataset.kursatRemoveBlock); renderBlocks(); renderComposer(); }));
    stack.querySelectorAll("[data-kursat-block-text]").forEach(input => input.addEventListener("input", () => { const item = blockState.get(input.dataset.kursatBlockText); if (item) item.text = input.value; updateBlockPreview(); }));
    updateBlockPreview();
  }

  function updateBlockPreview() {
    const text = [...blockState.values()].map(item => item.text.trim()).filter(Boolean).join("\n\n");
    document.querySelector("#kursat-block-preview").textContent = text || "Seçtiğin parçalar burada birleşecek.";
    return text;
  }

  function sendPrompt(text) {
    if (!text || !text.trim()) { PF.toast("İçerik eksik", "Önce birkaç alan doldur."); return; }
    document.dispatchEvent(new CustomEvent("kursat:prompt-to-forge", { detail:text }));
  }

  document.querySelector("#kursat-blocks-to-forge").addEventListener("click", () => sendPrompt(updateBlockPreview()));
  renderRoutes(); renderFlows(); renderBlocks(); renderComposer();
})();
