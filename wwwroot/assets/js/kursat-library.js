(() => {
  "use strict";
  const PF = window.PF;
  if (!PF) return;

  function emptyState(title, text, actionLabel = "Optimize Et", actionView = "forge") {
    return `<div class="kursat-empty"><div class="kursat-empty-icon"><svg viewBox="0 0 24 24"><path d="M4 5h16v14H4zM8 9h8m-8 4h5"/></svg></div><h3>${PF.escape(title)}</h3><p>${PF.escape(text)}</p><button class="kursat-button kursat-button--primary" type="button" data-kursat-view="${actionView}">${PF.escape(actionLabel)}</button></div>`;
  }

  function renderHistory() {
    const host = document.querySelector("#kursat-history-content");
    const search = document.querySelector("#kursat-history-search")?.value.trim().toLocaleLowerCase("tr-TR") || "";
    const model = document.querySelector("#kursat-history-model")?.value || "all";
    const items = PF.state.history.filter(item => (model === "all" || item.model === model) && (!search || `${item.original} ${item.optimized}`.toLocaleLowerCase("tr-TR").includes(search)));
    document.querySelector("#kursat-history-count").textContent = `${items.length} kayıt`;
    if (!items.length) { host.innerHTML = emptyState("Henüz geçmiş yok", "Bir prompt optimize ettiğinde sürümleri burada görebileceksin."); return; }
    host.innerHTML = `<article class="kursat-card kursat-table-card"><table class="kursat-table"><thead><tr><th>Prompt</th><th>Model</th><th>Token</th><th>Tarih</th><th></th></tr></thead><tbody>${items.map(item => `<tr><td><div class="kursat-table-title">${PF.escape((item.original || "").slice(0,72))}${(item.original || "").length > 72 ? "…" : ""}</div><small class="kursat-table-engine">${PF.escape(window.PFForge?.engineLabel(item.engine) || "")}</small></td><td><span class="kursat-badge">${PF.escape((window.PFForge?.modelProfiles[item.model]?.label) || item.model)}</span></td><td>${item.beforeTokens} → ${item.afterTokens}</td><td>${PF.escape(PF.formatDate(item.createdAt))}</td><td><div class="kursat-inline-actions"><button class="kursat-button" type="button" data-kursat-open-history="${item.id}">Aç</button><button class="kursat-button kursat-button--ghost" type="button" data-kursat-delete-history="${item.id}">Sil</button></div></td></tr>`).join("")}</tbody></table></article>`;
    host.querySelectorAll("[data-kursat-open-history]").forEach(btn => btn.addEventListener("click", () => { const record = PF.state.history.find(v => v.id === btn.dataset.kursatOpenHistory); if (record) document.dispatchEvent(new CustomEvent("kursat:load-record", { detail:record })); }));
    host.querySelectorAll("[data-kursat-delete-history]").forEach(btn => btn.addEventListener("click", () => PF.confirm("Kaydı sil", "Bu optimizasyon geçmişten kalıcı olarak kaldırılacak.", async () => {
      const id = btn.dataset.kursatDeleteHistory;
      const { ok } = await PF.api.del(`/api/prompts/${id}`);
      if (!ok) { PF.toast("Kayıt silinemedi", "Tekrar dene."); return; }
      // Sunucu favoriyi de siliyor, ekranda da iki listeden birden kaldır.
      PF.state.history = PF.state.history.filter(v => v.id !== id);
      PF.state.favorites = PF.state.favorites.filter(v => v.id !== id);
      renderAll();
      PF.toast("Kayıt silindi");
    })));
  }

  function renderFavorites() {
    const host = document.querySelector("#kursat-favorites-content");
    if (!PF.state.favorites.length) { host.innerHTML = emptyState("Henüz favori yok", "Tekrar kullanmak istediğin optimize edilmiş promptları favoriye ekleyebilirsin."); return; }
    host.innerHTML = `<div class="kursat-grid-2">${PF.state.favorites.map(item => `<article class="kursat-card kursat-favorite-card"><div class="kursat-favorite-meta"><span class="kursat-badge">${PF.escape(window.PFForge?.modelProfiles[item.model]?.label || item.model)}</span><span class="kursat-favorite-date">${PF.escape(PF.formatDate(item.createdAt))}</span></div><h3 class="kursat-favorite-title">${PF.escape((item.original || "Prompt").slice(0,80))}${(item.original || "").length > 80 ? "…" : ""}</h3><p class="kursat-favorite-preview">${PF.escape((item.optimized || "").slice(0,150))}${(item.optimized || "").length > 150 ? "…" : ""}</p><div class="kursat-favorite-actions"><button class="kursat-button kursat-button--primary" type="button" data-kursat-open-favorite="${item.id}">Aç</button><button class="kursat-button" type="button" data-kursat-remove-favorite="${item.id}">Kaldır</button></div></article>`).join("")}</div>`;
    host.querySelectorAll("[data-kursat-open-favorite]").forEach(btn => btn.addEventListener("click", () => { const record = PF.state.favorites.find(v => v.id === btn.dataset.kursatOpenFavorite); if (record) document.dispatchEvent(new CustomEvent("kursat:load-record", { detail:record })); }));
    host.querySelectorAll("[data-kursat-remove-favorite]").forEach(btn => btn.addEventListener("click", async () => {
      const id = btn.dataset.kursatRemoveFavorite;
      btn.disabled = true;
      const { ok } = await PF.api.del(`/api/favorites/${id}`);
      if (!ok) { btn.disabled = false; PF.toast("Favoriden kaldırılamadı", "Tekrar dene."); return; }
      PF.state.favorites = PF.state.favorites.filter(v => v.id !== id);
      renderAll();
      PF.toast("Favoriden kaldırıldı");
    }));
  }

  function renderCompare() {
    const host = document.querySelector("#kursat-compare-content");
    if (PF.state.history.length < 2) { host.innerHTML = emptyState("Karşılaştırmak için iki sürüm gerekiyor", "En az iki optimizasyon yaptıktan sonra sürümleri yan yana inceleyebilirsin."); return; }
    const options = PF.state.history.map((item,index) => `<option value="${item.id}">${index+1}. ${PF.escape((item.original || "Prompt").slice(0,48))}</option>`).join("");
    host.innerHTML = `<article class="kursat-card kursat-compare-card"><div class="kursat-form-grid"><div class="kursat-field"><label for="kursat-compare-a">Sürüm A</label><select class="kursat-select" id="kursat-compare-a">${options}</select></div><div class="kursat-field"><label for="kursat-compare-b">Sürüm B</label><select class="kursat-select" id="kursat-compare-b">${options}</select></div></div><div class="kursat-actions-end"><button class="kursat-button kursat-button--primary" id="kursat-compare-run" type="button">Karşılaştır</button></div></article><div class="kursat-compare-result" id="kursat-compare-result"></div>`;
    const a = host.querySelector("#kursat-compare-a"); const b = host.querySelector("#kursat-compare-b"); if (PF.state.history[1]) b.value = PF.state.history[1].id;
    host.querySelector("#kursat-compare-run").addEventListener("click", () => renderComparison(a.value, b.value));
  }

  function renderComparison(aId, bId) {
    const a = PF.state.history.find(v => v.id === aId); const b = PF.state.history.find(v => v.id === bId); const host = document.querySelector("#kursat-compare-result");
    if (!a || !b) return;
    host.innerHTML = `<div class="kursat-two-col"><article class="kursat-card"><div class="kursat-card-head"><div><h3>Sürüm A</h3><p>${PF.escape(PF.formatDate(a.createdAt))}</p></div></div><div class="kursat-compare-body"><div class="kursat-score-grid"><div class="kursat-score-card"><small>Sağlık</small><strong>${a.afterHealth}</strong></div><div class="kursat-score-card"><small>Token</small><strong>${a.afterTokens}</strong></div><div class="kursat-score-card"><small>Kural</small><strong>${a.requirements?.length || 0}</strong></div></div><pre class="kursat-result-code">${PF.escape(a.optimized)}</pre></div></article><article class="kursat-card"><div class="kursat-card-head"><div><h3>Sürüm B</h3><p>${PF.escape(PF.formatDate(b.createdAt))}</p></div></div><div class="kursat-compare-body"><div class="kursat-score-grid"><div class="kursat-score-card"><small>Sağlık</small><strong>${b.afterHealth}</strong></div><div class="kursat-score-card"><small>Token</small><strong>${b.afterTokens}</strong></div><div class="kursat-score-card"><small>Kural</small><strong>${b.requirements?.length || 0}</strong></div></div><pre class="kursat-result-code">${PF.escape(b.optimized)}</pre></div></article></div>`;
  }

  function setupSettings() {
    const model = document.querySelector("#kursat-setting-model");
    const goal = document.querySelector("#kursat-setting-goal");
    model.value = PF.state.model;
    goal.value = PF.state.goal;
    model.addEventListener("change", () => { PF.state.model = model.value; PF.save(); document.dispatchEvent(new CustomEvent("kursat:defaults-changed", { detail:{ model:PF.state.model, goal:PF.state.goal } })); PF.toast("Varsayılan model kaydedildi"); });
    goal.addEventListener("change", () => { PF.state.goal = goal.value; PF.save(); document.dispatchEvent(new CustomEvent("kursat:defaults-changed", { detail:{ model:PF.state.model, goal:PF.state.goal } })); PF.toast("Varsayılan hedef kaydedildi"); });
  }

  function renderAll() { renderHistory(); renderFavorites(); renderCompare(); }
  document.querySelector("#kursat-history-search").addEventListener("input", renderHistory);
  document.querySelector("#kursat-history-model").addEventListener("change", renderHistory);
  document.querySelector("#kursat-clear-history").addEventListener("click", () => PF.confirm("Geçmişi temizle", "Tüm optimizasyon geçmişi ve favoriler kalıcı olarak kaldırılacak.", async () => {
    const { ok } = await PF.api.del("/api/prompts");
    if (!ok) { PF.toast("Geçmiş temizlenemedi", "Tekrar dene."); return; }
    PF.state.history = [];
    PF.state.favorites = [];
    renderAll();
    PF.toast("Geçmiş temizlendi");
  }));
  document.addEventListener("kursat:data-changed", renderAll);
  document.addEventListener("kursat:view", event => { if (["history","favorites","compare"].includes(event.detail.name)) renderAll(); });
  setupSettings(); renderAll();
})();
