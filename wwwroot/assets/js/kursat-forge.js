(() => {
  "use strict";
  const PF = window.PF;
  if (!PF) return;

  const modelProfiles = {
    gpt: { label:"GPT", provider:"OpenAI", hint:"Net görev, açık kısıtlar ve tanımlı çıktı yapısı." },
    claude: { label:"Claude", provider:"Anthropic", hint:"Açık bağlam, ayrılmış kısıtlar ve gereksiz düşünme talimatlarından kaçınma." },
    gemini: { label:"Gemini", provider:"Google", hint:"Uzun bağlamı düzenli taşı, asıl görevi ve beklenen çıktıyı açık bırak." },
    deepseek: { label:"DeepSeek", provider:"DeepSeek", hint:"Kısa görev tanımı, stabil bağlam ve tekrarsız talimat yapısı." },
    universal: { label:"Evrensel", provider:"Model bağımsız", hint:"Model bağımsız, taşınabilir ve sade talimat yapısı." }
  };

  const useCaseProfiles = {
    general: { label:"Genel", instruction:"Görevi doğrudan, açık ve uygulanabilir biçimde tamamla." },
    coding: { label:"Kod & mühendislik", instruction:"Mevcut davranışı koru; değişiklikleri dosya, neden ve doğrulama adımlarıyla uygulanabilir hale getir." },
    research: { label:"Araştırma & analiz", instruction:"İddiaları kanıttan ayır; belirsizlikleri, kaynak ihtiyacını ve karşıt bulguları görünür tut." },
    content: { label:"İçerik & marka", instruction:"Hedef kitleyi, tonu, mesaj hiyerarşisini ve istenen aksiyonu tutarlı biçimde koru." },
    data: { label:"Veri & yapılandırma", instruction:"Şemayı tutarlı tut; alan adlarını, türleri, eksik değer davranışını ve doğrulama kurallarını açıkla." }
  };

  const formatProfiles = {
    auto:"Göreve en uygun, kolay taranabilir çıktı biçimini seç.",
    markdown:"Çıktıyı açık başlıklar ve gerektiğinde listeler içeren Markdown olarak ver.",
    json:"Yalnızca geçerli JSON döndür; kod bloğu, önsöz veya sonsöz ekleme.",
    table:"Karşılaştırılabilir bilgiyi kısa ve okunabilir bir tablo halinde ver.",
    checklist:"Çıktıyı eyleme dönük, doğrulanabilir bir kontrol listesi halinde ver.",
    code:"Çalıştırılabilir kodu dosya adlarıyla ver; açıklamayı yalnızca kritik kararlarla sınırla."
  };

  const languageProfiles = {
    prompt:"Promptun baskın dilinde yanıt ver.",
    tr:"Yanıtı Türkçe ver.",
    en:"Respond in English."
  };

  const promptInput = document.querySelector("#kursat-prompt-input");
  const result = document.querySelector("#kursat-result");
  const editorCard = document.querySelector(".kursat-editor-card");
  const processCard = document.querySelector("#kursat-forge-process");
  const forgeButton = document.querySelector("#kursat-forge-button");
  const forgeButtonLabel = forgeButton?.querySelector(".kursat-forge-button-label");
  const draftStatus = document.querySelector("#kursat-draft-status");
  const undoButton = document.querySelector("#kursat-undo");
  const redoButton = document.querySelector("#kursat-redo");
  const errorCard = document.querySelector("#kursat-forge-error");
  let draftTimer = 0;
  let snapshotTimer = 0;
  let promptSnapshots = [];
  let promptSnapshotIndex = -1;
  let processTimers = [];

  // Editördeki önemli değişiklikleri kendi snapshot zincirimde tutuyorum; tek tek tuş vuruşlarını kaydetmiyorum.
  function syncSnapshotButtons() {
    if (undoButton) undoButton.disabled = promptSnapshotIndex <= 0;
    if (redoButton) redoButton.disabled = promptSnapshotIndex < 0 || promptSnapshotIndex >= promptSnapshots.length - 1;
  }

  function resetSnapshots(value = "") {
    window.clearTimeout(snapshotTimer);
    promptSnapshots = [String(value)];
    promptSnapshotIndex = 0;
    syncSnapshotButtons();
  }

  function commitSnapshot(value = promptInput.value) {
    const next = String(value);
    if (promptSnapshots[promptSnapshotIndex] === next) return;
    promptSnapshots = promptSnapshots.slice(0, promptSnapshotIndex + 1);
    promptSnapshots.push(next);
    if (promptSnapshots.length > 40) promptSnapshots.shift();
    promptSnapshotIndex = promptSnapshots.length - 1;
    syncSnapshotButtons();
  }

  function scheduleSnapshot() {
    window.clearTimeout(snapshotTimer);
    snapshotTimer = window.setTimeout(() => commitSnapshot(promptInput.value), 520);
  }

  function applySnapshot(index) {
    if (index < 0 || index >= promptSnapshots.length) return;
    promptSnapshotIndex = index;
    promptInput.value = promptSnapshots[index];
    updatePromptMeta({ silent:true });
    syncSnapshotButtons();
    promptInput.focus();
  }

  function setPromptValue(value, options = {}) {
    promptInput.value = String(value || "");
    updatePromptMeta({ silent: Boolean(options.silent) });
    if (options.reset !== false) resetSnapshots(promptInput.value);
    else commitSnapshot(promptInput.value);
  }

  function buildSemanticChanges(record) {
    const items = [];
    const issueNames = new Set((record.issues || []).map(item => item[0]));
    if (issueNames.has("Bağlam az")) items.push(["Bağlam düzenlendi", "Görev, bağlam ve beklenen çıktı birbirinden ayrıldı."]);
    if (issueNames.has("Yapı zayıf")) items.push(["Yapı netleştirildi", "Tek blok talimat görev, bağlam ve çıktı bölümlerine ayrıldı."]);
    if (issueNames.has("Çıktı tanımı eksik")) items.push(["Çıktı sözleşmesi eklendi", "Modelin nasıl bir sonuç döndürmesi gerektiği açık hale getirildi."]);
    if (issueNames.has("Tekrar var")) items.push(["Tekrarlar azaltıldı", "Aynı talimatı yeniden söyleyen ifadeler tek kurala indirildi."]);
    if (issueNames.has("Belirsiz referans")) items.push(["Belirsizlik azaltıldı", "Referansı zayıf ifadeler daha açık bir görev yapısına taşındı."]);
    if (issueNames.has("Başarı ölçütü eksik")) items.push(["Kalite kapısı eklendi", "Teslim öncesi sessiz doğrulama adımı tanımlandı."]);
    if (record.requirements?.length) items.push(["Kısıtlar korundu", `${record.requirements.length} zorunlu gereksinim optimize edilmiş sürümde ayrı tutuldu.`]);
    if (record.variables?.length) items.push(["Değişkenler korundu", `${record.variables.length} şablon değişkeni çalıştırma zamanı girdisi olarak tanımlandı.`]);
    const profile = modelProfiles[record.model] || modelProfiles.universal;
    items.push([`${profile.label} için uyarlandı`, profile.hint]);
    const goalText = record.goal === "quality" ? "İlk yanıt kalitesine öncelik verildi." : record.goal === "lean" ? "Gereksiz çıktı ve tekrar azaltıldı." : "Kalite ve kullanım dengesi korundu.";
    items.push(["Optimizasyon hedefi uygulandı", goalText]);
    return items.slice(0, 6);
  }

  function renderSemanticChanges(record) {
    const host = document.querySelector("#kursat-semantic-changes");
    if (!host) return;
    const items = buildSemanticChanges(record);
    host.innerHTML = items.map(([title, copy], index) => `<article class="kursat-change-item"><span class="kursat-change-index">${String(index + 1).padStart(2, "0")}</span><div><strong>${PF.escape(title)}</strong><p>${PF.escape(copy)}</p></div></article>`).join("");
  }

  function showForgeError(title = "İşlem tamamlanamadı", message = "Bağlantıyı veya model ayarlarını kontrol edip tekrar deneyebilirsin.") {
    stopProcessTimers();
    setForgeLoading(false);
    processCard?.classList.add("kursat-hidden");
    result.classList.add("kursat-hidden");
    errorCard?.classList.remove("kursat-hidden");
    const titleEl = document.querySelector("#kursat-forge-error-title");
    const messageEl = document.querySelector("#kursat-forge-error-message");
    if (titleEl) titleEl.textContent = title;
    if (messageEl) messageEl.textContent = message;
    errorCard?.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block: "nearest" });
  }

  function hideForgeError() { errorCard?.classList.add("kursat-hidden"); }

  function estimateTokens(text) {
    const clean = String(text || "").trim();
    if (!clean) return 0;
    const words = clean.split(/\s+/).length;
    const punctuation = (clean.match(/[.,;:!?(){}\[\]<>]/g) || []).length;
    return Math.max(1, Math.round(words * 1.34 + punctuation * .16));
  }

  function detectRequirements(text) {
    const lines = String(text || "").split(/\n+/).map(v => v.trim()).filter(Boolean);
    const markers = /(must|should|do not|don't|never|always|keep|preserve|without|yalnızca|mutlaka|kesinlikle|değiştirme|koru|olmalı|olmamalı|kullanma|bırakma|çıktı|format|responsive|yorum|adlandır)/i;
    const results = [];
    lines.forEach(line => {
      const sentences = line.split(/(?<=[.!?])\s+/).filter(Boolean);
      sentences.forEach(sentence => {
        if (markers.test(sentence) || /^[-*•]/.test(sentence)) results.push(sentence.replace(/^[-*•]\s*/, "").trim());
      });
    });
    return [...new Set(results)].slice(0, 12);
  }

  function detectVariables(text) {
    const matches = String(text || "").matchAll(/\{\{\s*([\wçğıöşüÇĞİÖŞÜ.-]+)\s*\}\}|\[([A-ZÇĞİÖŞÜ][A-ZÇĞİÖŞÜ0-9 _.-]{2,})\]/g);
    return [...new Set([...matches].map(match => (match[1] || match[2] || "").trim()))].slice(0, 12);
  }

  function analyzePrompt(text) {
    const raw = String(text || "").trim();
    if (!raw) return { score:0, issues:[], requirements:[], variables:[], tokens:0, checks:{} };
    const issues = [];
    const requirements = detectRequirements(raw);
    const variables = detectVariables(raw);
    const unresolved = (raw.match(/\[[^\]\n]{3,}\]/g) || []).filter(item => !/^\[[A-ZÇĞİÖŞÜ0-9 _.-]+\]$/.test(item));
    const tokens = estimateTokens(raw);
    const words = raw.split(/\s+/);
    const duplicates = words.filter((word, index) => words.indexOf(word) !== index && word.length > 7).length;
    const checks = {
      objective: raw.length >= 28 && /\b(yap|oluştur|üret|incele|analiz|yaz|dönüştür|karşılaştır|optimize|tasarla|build|create|analyze|write|review|compare|generate)\w*/i.test(raw),
      context: raw.length >= 120 || /(bağlam|context|mevcut|şu anda|hedef kitle|arka plan|background)/i.test(raw),
      output: /(çıktı|output|format|json|liste|tablo|kod|dosya|cevap|sonuç|markdown|schema)/i.test(raw),
      constraints: requirements.length > 0,
      quality: /(kalite|başarı|kriter|doğrula|test|kontrol|quality|acceptance|verify|validation)/i.test(raw),
      examples: /(örnek|example|örneğin|input:|output:|girdi:|çıktı:)/i.test(raw)
    };
    if (!checks.context) issues.push(["Bağlam az", "Görevin amacı veya mevcut durum yeterince açıklanmamış olabilir."]);
    if (!checks.objective || !/[.!?\n]/.test(raw)) issues.push(["Yapı zayıf", "Ana görev tek, eyleme dönük bir amaç cümlesi olarak ayrılabilir."]);
    if (!checks.output) issues.push(["Çıktı tanımı eksik", "Modelin nasıl bir sonuç döndürmesi gerektiği açık değil."]);
    if (!checks.quality && raw.length > 90) issues.push(["Başarı ölçütü eksik", "İyi bir sonucun nasıl doğrulanacağı belirtilmemiş."]);
    if (unresolved.length) issues.push(["Tamamlanmamış alan", `${unresolved.length} iskelet alanı gerçek içerikle doldurulmayı bekliyor.`]);
    if (duplicates > 5 || /(kesinlikle.{0,60}kesinlikle|değiştirme.{0,80}değiştirme)/i.test(raw)) issues.push(["Tekrar var", "Aynı kural birden fazla kez vurgulanmış olabilir."]);
    if (/(bunu|şunu|orası|buradaki|şöyle yap|daha iyi yap)/i.test(raw) && raw.length < 240) issues.push(["Belirsiz referans", "Bazı ifadelerin neye referans verdiği açık olmayabilir."]);
    const weights = { objective:22, context:18, output:18, constraints:16, quality:16, examples:10 };
    let score = Object.entries(checks).reduce((sum, [key, ready]) => sum + (ready ? weights[key] : 0), 0);
    if (raw.length >= 80) score += 4;
    if (raw.length >= 220) score += 3;
    if (variables.length) score += 2;
    if (duplicates > 5) score -= 6;
    score -= Math.min(30, unresolved.length * 8);
    score = Math.max(18, Math.min(98, score));
    return { score, issues, requirements, variables, unresolved, tokens, checks };
  }

  function buildOptimizedPrompt(text, model, goal, analysis) {
    const profile = modelProfiles[model] || modelProfiles.universal;
    const useCase = useCaseProfiles[PF.state.useCase] || useCaseProfiles.general;
    const requirements = analysis.requirements;
    const trimmed = String(text || "").replace(/\s+/g, " ").trim();
    const objective = trimmed.split(/(?<=[.!?])\s+/)[0] || trimmed;
    const requirementSet = new Set(requirements.map(item => item.replace(/\s+/g, " ").trim()));
    const rest = trimmed.slice(objective.length).trim().split(/(?<=[.!?])\s+/).filter(sentence => !requirementSet.has(sentence.trim())).join(" ");
    const modeLine = goal === "quality"
      ? "Öncelik: İlk yanıtta eksiksiz ve uygulanabilir sonuç üret."
      : goal === "lean"
        ? "Öncelik: Gereksiz açıklama ve tekrar üretmeden, yalnızca gerekli çıktıyı ver."
        : "Öncelik: Kaliteyi korurken gereksiz bağlamı ve tekrarları azalt.";

    const policies = [
      PF.state.askClarifying ? "Görevi doğru tamamlamak için kritik bilgi eksikse üretime başlamadan önce en fazla 3 kısa netleştirme sorusu sor." : "Makul varsayımlarla ilerle; netleştirme sorusu sorma.",
      PF.state.exposeAssumptions ? "Sonucu etkileyen varsayımları kısa ve açık biçimde belirt." : "Gereksiz varsayım açıklaması ekleme."
    ];

    const parts = [
      `GÖREV\n${objective}`,
      rest ? `BAĞLAM\n${rest}` : "",
      requirements.length ? `ZORUNLU KURALLAR\n${requirements.map(item => `- ${item}`).join("\n")}` : "",
      analysis.variables.length ? `DEĞİŞKENLER\n${analysis.variables.map(item => `- {{${item}}}: Çalıştırma anında sağlanacak değer.`).join("\n")}` : "",
      `ÇALIŞMA BİÇİMİ\n${useCase.instruction}\n${modeLine}\n${policies.map(item => `- ${item}`).join("\n")}`,
      `MODEL UYUMU\nHedef: ${profile.label} (${profile.provider}). ${profile.hint}`,
      `ÇIKTI SÖZLEŞMESİ\n- ${languageProfiles[PF.state.responseLanguage] || languageProfiles.prompt}\n- ${formatProfiles[PF.state.responseFormat] || formatProfiles.auto}\n- Görevi doğrudan tamamla; gereksiz giriş, tekrar veya talimat özeti ekleme.`,
      `SON KONTROL\nYanıtı teslim etmeden önce zorunlu kuralların tamamını karşıladığını ve biçimin geçerli olduğunu sessizce doğrula.`
    ].filter(Boolean);

    return parts.join("\n\n");
  }

  function renderPreflight(analysis) {
    const dot = document.querySelector("#kursat-preflight-dot");
    const title = document.querySelector("#kursat-preflight-title");
    const subtitle = document.querySelector("#kursat-preflight-subtitle");
    const list = document.querySelector("#kursat-issue-list");
    const score = document.querySelector("#kursat-live-score");
    const orbit = document.querySelector("#kursat-score-orbit");
    const checksHost = document.querySelector("#kursat-structure-checks");
    const completeButton = document.querySelector("#kursat-complete-structure");
    const checkLabels = { objective:"Net amaç", context:"Yeterli bağlam", output:"Çıktı biçimi", constraints:"Kısıtlar", quality:"Kalite ölçütü", examples:"Örnek / referans" };
    if (score) score.textContent = analysis.score || 0;
    if (orbit) orbit.style.setProperty("--score", analysis.score || 0);
    if (checksHost) checksHost.innerHTML = Object.entries(checkLabels).map(([key, label]) => `<span class="${analysis.checks?.[key] ? "is-ready" : ""}"><i>${analysis.checks?.[key] ? "✓" : "·"}</i>${label}</span>`).join("");
    if (completeButton) completeButton.disabled = !promptInput.value.trim() || Object.values(analysis.checks || {}).every(Boolean);
    if (!promptInput.value.trim()) {
      dot.className = "kursat-status-dot";
      title.textContent = "Henüz analiz yapılmadı";
      subtitle.textContent = "Prompt yazdıkça kontrol başlayacak.";
      list.innerHTML = `<div class="kursat-empty-mini">Eksik bağlam, belirsiz ifade, tekrar ve çıktı tanımı burada gösterilecek.</div>`;
      return;
    }
    dot.className = `kursat-status-dot ${analysis.issues.length ? "is-warning" : "is-ready"}`;
    title.textContent = analysis.issues.length ? `${analysis.issues.length} nokta iyileştirilebilir` : "Temel yapı iyi görünüyor";
    subtitle.textContent = analysis.issues.length ? "Optimize ederken bu noktalar ele alınacak." : "Belirgin bir yapısal risk bulunmadı.";
    list.innerHTML = analysis.issues.length
      ? analysis.issues.map(issue => `<div class="kursat-issue"><strong>${PF.escape(issue[0])}</strong><p>${PF.escape(issue[1])}</p></div>`).join("")
      : `<div class="kursat-empty-mini">Model uyumu ve gereksiz tekrarlar yine de optimize edilebilir.</div>`;
  }

  function renderEditorInsights(analysis) {
    const summary = document.querySelector("#kursat-anatomy-summary");
    const variables = document.querySelector("#kursat-variable-list");
    if (!summary || !variables) return;
    if (!promptInput.value.trim()) {
      summary.textContent = "Yazmaya başladığında yapı ve değişkenler burada görünür.";
      variables.innerHTML = `<span class="kursat-variable-empty">Değişken yok</span>`;
      return;
    }
    const ready = Object.values(analysis.checks || {}).filter(Boolean).length;
    summary.textContent = `${ready}/6 yapı sinyali · ${analysis.requirements.length} kısıt · ${analysis.unresolved?.length || 0} bekleyen alan · ${analysis.tokens} token`;
    variables.innerHTML = analysis.variables.length
      ? analysis.variables.map(item => `<span class="kursat-variable-chip">{{${PF.escape(item)}}}</span>`).join("")
      : `<span class="kursat-variable-empty">Dinamik değişken yok</span>`;
  }

  function completePromptStructure() {
    const current = promptInput.value.trim();
    if (!current) { promptInput.focus(); return; }
    const analysis = analyzePrompt(current);
    const additions = [];
    if (!analysis.checks.context) additions.push("BAĞLAM\n[Hedef kitleyi, mevcut durumu ve gerekli referansları buraya ekle.]");
    if (!analysis.checks.constraints) additions.push("KISITLAR\n- [Kesinlikle korunması gereken kural]\n- [Kaçınılması gereken durum]");
    if (!analysis.checks.output) additions.push("ÇIKTI BİÇİMİ\n[Formatı, uzunluğu ve gerekli bölümleri tanımla.]");
    if (!analysis.checks.quality) additions.push("KALİTE ÖLÇÜTLERİ\n- [Başarılı çıktının ölçülebilir şartı]\n- [Teslimden önce doğrulanacak risk]");
    if (!analysis.checks.examples) additions.push("REFERANS / ÖRNEK\n[Varsa iyi bir örnek veya beklenen örüntü ekle.]");
    if (!additions.length) { PF.toast("İskelet hazır", "Temel prompt bileşenlerinin tamamı zaten mevcut."); return; }
    setPromptValue(`${current}\n\n${additions.join("\n\n")}`);
    promptInput.focus();
    PF.toast("İskelet tamamlandı", `${additions.length} eksik bölüm düzenlenmek üzere eklendi.`);
  }

  function showDraftStatus(message = "Taslak kaydedildi") {
    if (!draftStatus) return;
    draftStatus.textContent = message;
    draftStatus.classList.add("is-visible");
    window.clearTimeout(draftTimer);
    draftTimer = window.setTimeout(() => draftStatus.classList.remove("is-visible"), 1450);
  }

  function updatePromptMeta(options = {}) {
    const analysis = analyzePrompt(promptInput.value);
    document.querySelector("#kursat-token-count").textContent = `${analysis.tokens} tahmini token`;
    renderPreflight(analysis);
    renderEditorInsights(analysis);
    if (PF.state.draftEnabled) {
      // Frontend-only önizlemede taslak kalıcı olarak saklanmaz.
      if (!options.silent) showDraftStatus(promptInput.value.trim() ? "Taslak kaydedildi" : "Taslak temizlendi");
    }
  }

  function stopProcessTimers() {
    processTimers.forEach(timer => clearTimeout(timer));
    processTimers = [];
  }

  // Bu geçiş backend geldiğinde de aynı kalacak; yalnızca aşamaları gerçek API durumlarıyla besleyeceğim.
  function beginProcess() {
    stopProcessTimers();
    processCard?.classList.remove("kursat-hidden");
    processCard?.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block: "nearest" });
    const track = document.querySelector("#kursat-process-track");
    const percent = document.querySelector("#kursat-process-percent");
    const title = document.querySelector("#kursat-process-title");
    const steps = [...document.querySelectorAll("[data-kursat-process-step]")];
    const phases = [
      [0, 18, "Prompt yapısı okunuyor"],
      [1, 43, "Gereksinimler ayrıştırılıyor"],
      [2, 72, "Model profiline uyarlanıyor"],
      [3, 92, "Son kontrol yapılıyor"]
    ];
    steps.forEach(step => step.classList.remove("is-active", "is-done"));
    if (track) track.style.width = "0%";
    if (percent) percent.textContent = "0%";
    phases.forEach(([index, value, text], phaseIndex) => {
      const timer = setTimeout(() => {
        steps.forEach((step, i) => {
          step.classList.toggle("is-active", i === index);
          step.classList.toggle("is-done", i < index);
        });
        if (title) title.textContent = text;
        if (track) track.style.width = `${value}%`;
        if (percent) percent.textContent = `${value}%`;
      }, phaseIndex * 150);
      processTimers.push(timer);
    });
  }

  function finishProcess() {
    const track = document.querySelector("#kursat-process-track");
    const percent = document.querySelector("#kursat-process-percent");
    const title = document.querySelector("#kursat-process-title");
    const steps = [...document.querySelectorAll("[data-kursat-process-step]")];
    if (track) track.style.width = "100%";
    if (percent) percent.textContent = "100%";
    if (title) title.textContent = "Hazır";
    steps.forEach(step => { step.classList.remove("is-active"); step.classList.add("is-done"); });
    setTimeout(() => processCard?.classList.add("kursat-hidden"), PF.state.motion === "off" ? 0 : 260);
  }

  function setForgeLoading(isLoading) {
    if (!forgeButton) return;
    forgeButton.disabled = isLoading;
    forgeButton.classList.toggle("is-loading", isLoading);
    if (forgeButtonLabel) forgeButtonLabel.textContent = isLoading ? "İşleniyor" : "Optimize Et";
  }

  function forge() {
    const text = promptInput.value.trim();
    if (!text) { PF.toast("Prompt boş", "Optimize etmek için önce bir prompt yaz."); promptInput.focus(); return; }
    setForgeLoading(true);
    beginProcess();
    result.classList.add("kursat-hidden");
    result.classList.remove("is-revealed");
    const before = analyzePrompt(text);
    hideForgeError();
    setTimeout(() => {
      try {
      const optimized = buildOptimizedPrompt(text, PF.state.model, PF.state.goal, before);
      const after = analyzePrompt(optimized);
      const requirements = before.requirements;
      const record = {
        id: PF.uid("forge"),
        createdAt: new Date().toISOString(),
        model: PF.state.model,
        goal: PF.state.goal,
        original: text,
        optimized,
        beforeTokens: before.tokens,
        afterTokens: estimateTokens(optimized),
        beforeHealth: before.score,
        afterHealth: Math.max(after.score, Math.min(98, before.score + 14)),
        requirements,
        issues: before.issues,
        variables: before.variables,
        checks: before.checks,
        profile: {
          useCase: PF.state.useCase,
          responseFormat: PF.state.responseFormat,
          responseLanguage: PF.state.responseLanguage,
          askClarifying: PF.state.askClarifying,
          exposeAssumptions: PF.state.exposeAssumptions
        }
      };
      PF.state.currentResult = record;
      PF.state.history.unshift(record);
      PF.state.history = PF.state.history.slice(0, 80);
      PF.save();
      finishProcess();
      renderResult(record);
      document.dispatchEvent(new CustomEvent("kursat:data-changed"));
      setForgeLoading(false);
      PF.toast("Prompt hazır", "Optimize edilmiş sürüm oluşturuldu.");
      } catch (error) {
        console.error(error);
        showForgeError("Optimizasyon sırasında hata oluştu", "Promptu kontrol edip tekrar deneyebilirsin.");
      }
    }, 650);
  }

  function updateFavoriteButton(record) {
    const button = document.querySelector("#kursat-save-favorite");
    if (!button || !record) return;
    const exists = PF.state.favorites.some(item => item.id === record.id);
    button.textContent = exists ? "Favorilerde ✓" : "Favoriye ekle";
    button.classList.toggle("is-saved", exists);
  }

  function renderResult(record) {
    record.requirements = Array.isArray(record.requirements) ? record.requirements : [];
    record.issues = Array.isArray(record.issues) ? record.issues : [];
    record.variables = Array.isArray(record.variables) ? record.variables : detectVariables(record.original || "");
    record.checks = record.checks || analyzePrompt(record.original || "").checks;
    record.profile = record.profile || {};
    record.original = String(record.original || "");
    record.optimized = String(record.optimized || "");
    result.classList.remove("kursat-hidden", "is-revealed");
    void result.offsetWidth;
    result.classList.add("is-revealed");
    document.querySelector("#kursat-metric-health").textContent = `${record.beforeHealth} → ${record.afterHealth}`;
    const delta = record.beforeTokens ? Math.round((1 - record.afterTokens / record.beforeTokens) * 100) : 0;
    document.querySelector("#kursat-metric-token").textContent = delta > 0 ? `-%${delta}` : `${record.beforeTokens} → ${record.afterTokens}`;
    document.querySelector("#kursat-metric-rules").textContent = `${record.requirements.length}/${record.requirements.length}`;
    document.querySelector("#kursat-metric-model").textContent = modelProfiles[record.model]?.label || "Evrensel";
    document.querySelector("#kursat-result-code").textContent = record.optimized;
    document.querySelector("#kursat-diff-original").textContent = record.original;
    document.querySelector("#kursat-diff-optimized").textContent = record.optimized;
    renderSemanticChanges(record);
    const ruleList = document.querySelector("#kursat-rule-list");
    ruleList.innerHTML = record.requirements.length
      ? record.requirements.map(item => `<li><span class="kursat-check">✓</span><span>${PF.escape(item)}</span></li>`).join("")
      : `<li><span class="kursat-check">✓</span><span>Belirgin zorunlu kural bulunmadı; görev ve çıktı yapısı korundu.</span></li>`;
    const diagnostics = document.querySelector("#kursat-diagnostic-grid");
    const diagnosticItems = [
      ["Yapısal bütünlük", `${Object.values(record.checks).filter(Boolean).length}/6 sinyal`, "Amaç, bağlam ve teslim beklentisi ayrı katmanlara derlendi."],
      ["Kural koruması", `${record.requirements.length} kural`, record.requirements.length ? "Zorunlu ifadeler ayrı blokta korunuyor." : "Açık zorunlu kural bulunmadı; görev anlamı korundu."],
      ["Değişken güvenliği", `${record.variables.length} değişken`, record.variables.length ? "Şablon değişkenleri değiştirilmeden taşındı." : "Dinamik şablon değişkeni algılanmadı."],
      ["Yanıt sözleşmesi", formatProfiles[record.profile.responseFormat || PF.state.responseFormat] ? (record.profile.responseFormat || PF.state.responseFormat).toUpperCase() : "AUTO", "Dil, biçim ve teslim öncesi kontrol politikası eklendi."],
      ["Belirsizlik yönetimi", (record.profile.askClarifying ?? PF.state.askClarifying) ? "Soru sor" : "Varsay", (record.profile.askClarifying ?? PF.state.askClarifying) ? "Kritik bilgi eksikse üretimden önce kısa sorular sorulur." : "Eksik bilgiler makul varsayımlarla tamamlanır."],
      ["Model uyumu", modelProfiles[record.model]?.label || "Evrensel", modelProfiles[record.model]?.hint || modelProfiles.universal.hint]
    ];
    if (diagnostics) diagnostics.innerHTML = diagnosticItems.map(([label, value, copy]) => `<article class="kursat-diagnostic-card"><span>${PF.escape(label)}</span><strong>${PF.escape(value)}</strong><p>${PF.escape(copy)}</p></article>`).join("");
    document.querySelector("#kursat-result-badge").textContent = "Gereksinimler korundu";
    updateFavoriteButton(record);
    setTimeout(() => result.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block:"start" }), 60);
  }

  function loadRecord(record) {
    PF.state.currentResult = record;
    setPromptValue(record.original || record.optimized || "", { silent:true });
    PF.state.model = modelProfiles[record.model] ? record.model : "universal";
    PF.state.goal = ["quality","balanced","lean"].includes(record.goal) ? record.goal : "balanced";
    if (record.profile) {
      if (useCaseProfiles[record.profile.useCase]) PF.state.useCase = record.profile.useCase;
      if (formatProfiles[record.profile.responseFormat]) PF.state.responseFormat = record.profile.responseFormat;
      if (languageProfiles[record.profile.responseLanguage]) PF.state.responseLanguage = record.profile.responseLanguage;
      if (typeof record.profile.askClarifying === "boolean") PF.state.askClarifying = record.profile.askClarifying;
      if (typeof record.profile.exposeAssumptions === "boolean") PF.state.exposeAssumptions = record.profile.exposeAssumptions;
    }
    syncControls();
    updatePromptMeta({ silent:true });
    if (record.optimized) renderResult(record);
    PF.openView("forge");
  }

  function pulseControl(button) {
    if (!button || PF.state.motion === "off") return;
    button.classList.remove("is-pulse");
    void button.offsetWidth;
    button.classList.add("is-pulse");
    setTimeout(() => button.classList.remove("is-pulse"), 300);
  }

  function syncControls() {
    document.querySelectorAll("[data-kursat-model]").forEach(btn => btn.classList.toggle("is-active", btn.dataset.kursatModel === PF.state.model));
    document.querySelectorAll("[data-kursat-goal]").forEach(btn => btn.classList.toggle("is-active", btn.dataset.kursatGoal === PF.state.goal));
    const modelSetting = document.querySelector("#kursat-setting-model");
    const goalSetting = document.querySelector("#kursat-setting-goal");
    if (modelSetting) modelSetting.value = PF.state.model;
    if (goalSetting) goalSetting.value = PF.state.goal;
    const useCase = document.querySelector("#kursat-use-case");
    const responseFormat = document.querySelector("#kursat-response-format");
    const responseLanguage = document.querySelector("#kursat-response-language");
    if (useCase) useCase.value = PF.state.useCase;
    if (responseFormat) responseFormat.value = PF.state.responseFormat;
    if (responseLanguage) responseLanguage.value = PF.state.responseLanguage;
    [["#kursat-ask-clarifying", PF.state.askClarifying], ["#kursat-expose-assumptions", PF.state.exposeAssumptions]].forEach(([selector, active]) => {
      const button = document.querySelector(selector);
      button?.classList.toggle("is-on", active);
      button?.setAttribute("aria-pressed", String(active));
    });
  }

  function toggleFocusMode(force) {
    const open = typeof force === "boolean" ? force : !document.body.classList.contains("kursat-editor-focus");
    document.body.classList.toggle("kursat-editor-focus", open);
    document.querySelector("#kursat-focus-editor")?.setAttribute("aria-pressed", String(open));
    if (open) setTimeout(() => promptInput.focus(), 50);
  }

  function bindDropzone() {
    if (!editorCard) return;
    const stop = event => { event.preventDefault(); event.stopPropagation(); };
    ["dragenter","dragover"].forEach(type => editorCard.addEventListener(type, event => { stop(event); editorCard.classList.add("is-dragging"); }));
    ["dragleave","drop"].forEach(type => editorCard.addEventListener(type, event => { stop(event); if (type === "dragleave" && editorCard.contains(event.relatedTarget)) return; editorCard.classList.remove("is-dragging"); }));
    editorCard.addEventListener("drop", async event => {
      const file = event.dataTransfer?.files?.[0];
      if (!file) return;
      const allowed = /\.(txt|md|json)$/i.test(file.name) || ["text/plain","text/markdown","application/json"].includes(file.type);
      if (!allowed) { PF.toast("Dosya desteklenmiyor", ".txt, .md veya .json kullanabilirsin."); return; }
      try { setPromptValue(await file.text()); PF.toast("Dosya yüklendi", file.name); }
      catch { PF.toast("Dosya okunamadı"); }
    });
  }

  document.querySelectorAll("[data-kursat-model]").forEach(btn => btn.addEventListener("click", () => {
    PF.state.model = btn.dataset.kursatModel;
    PF.save();
    syncControls();
    updatePromptMeta({ silent:true });
    pulseControl(btn);
  }));
  document.querySelectorAll("[data-kursat-goal]").forEach(btn => btn.addEventListener("click", () => {
    PF.state.goal = btn.dataset.kursatGoal;
    PF.save();
    syncControls();
    pulseControl(btn);
  }));
  [["#kursat-use-case", "useCase"], ["#kursat-response-format", "responseFormat"], ["#kursat-response-language", "responseLanguage"]].forEach(([selector, key]) => {
    document.querySelector(selector)?.addEventListener("change", event => {
      PF.state[key] = event.target.value;
      PF.save();
      updatePromptMeta({ silent:true });
    });
  });
  document.querySelector("#kursat-ask-clarifying")?.addEventListener("click", () => {
    PF.state.askClarifying = !PF.state.askClarifying;
    PF.save(); syncControls();
  });
  document.querySelector("#kursat-expose-assumptions")?.addEventListener("click", () => {
    PF.state.exposeAssumptions = !PF.state.exposeAssumptions;
    PF.save(); syncControls();
  });
  document.querySelector("#kursat-complete-structure")?.addEventListener("click", completePromptStructure);
  promptInput.addEventListener("input", () => { updatePromptMeta(); scheduleSnapshot(); });
  forgeButton.addEventListener("click", forge);
  document.querySelector("#kursat-load-example").addEventListener("click", () => {
    setPromptValue("Mevcut ASP.NET Core API endpointini incele. Gereksiz veritabanı sorgularını azalt, mevcut davranışı ve response sözleşmesini değiştirme. Yapacağın değişiklikleri uygulanabilir kod örnekleriyle ver. Gereksiz açıklama ekleme.");
    promptInput.focus();
  });
  document.querySelectorAll("[data-kursat-prompt-template]").forEach(card => card.addEventListener("click", () => {
    setPromptValue(card.dataset.kursatPromptTemplate || "");
    promptInput.focus();
    promptInput.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block: "center" });
  }));
  document.querySelector("#kursat-clear-prompt").addEventListener("click", () => {
    setPromptValue("");
    // Kalıcı taslak bulunmadığı için yalnızca editörü temizlemek yeterli.
    result.classList.add("kursat-hidden");
    processCard?.classList.add("kursat-hidden");
  });
  document.querySelector("#kursat-paste").addEventListener("click", async () => {
    try { setPromptValue(await navigator.clipboard.readText()); promptInput.focus(); }
    catch { PF.toast("Panoya erişilemedi", "Metni Ctrl/Cmd + V ile yapıştırabilirsin."); }
  });
  document.querySelector("#kursat-import").addEventListener("click", () => document.querySelector("#kursat-file-input").click());
  document.querySelector("#kursat-file-input").addEventListener("change", async event => {
    const file = event.target.files?.[0]; if (!file) return;
    try { setPromptValue(await file.text()); PF.toast("Dosya yüklendi", file.name); }
    catch { PF.toast("Dosya okunamadı"); }
    event.target.value = "";
  });
  document.querySelector("#kursat-focus-editor")?.addEventListener("click", () => toggleFocusMode());
  document.querySelector("#kursat-focus-exit")?.addEventListener("click", () => toggleFocusMode(false));
  undoButton?.addEventListener("click", () => applySnapshot(promptSnapshotIndex - 1));
  redoButton?.addEventListener("click", () => applySnapshot(promptSnapshotIndex + 1));
  document.querySelector("#kursat-forge-retry")?.addEventListener("click", forge);
  document.addEventListener("kursat:close-editor-focus", () => toggleFocusMode(false));
  bindDropzone();

  document.querySelectorAll("[data-kursat-result-tab]").forEach(btn => btn.addEventListener("click", () => {
    const name = btn.dataset.kursatResultTab;
    document.querySelectorAll("[data-kursat-result-tab]").forEach(item => item.classList.toggle("is-active", item === btn));
    document.querySelectorAll("[data-kursat-result-panel]").forEach(panel => panel.classList.toggle("is-active", panel.dataset.kursatResultPanel === name));
  }));
  document.querySelector("#kursat-copy-result").addEventListener("click", async event => {
    const text = PF.state.currentResult?.optimized; if (!text) return;
    const button = event.currentTarget;
    const original = button.textContent;
    try {
      await navigator.clipboard.writeText(text);
      button.textContent = "Kopyalandı ✓";
      PF.toast("Kopyalandı");
      setTimeout(() => { button.textContent = original; }, 1300);
    } catch { PF.toast("Kopyalama başarısız"); }
  });
  document.querySelector("#kursat-use-result")?.addEventListener("click", () => {
    const text = PF.state.currentResult?.optimized; if (!text) return;
    setPromptValue(text);
    result.classList.add("kursat-hidden");
    promptInput.focus();
    promptInput.scrollIntoView({ behavior: PF.state.motion === "off" ? "auto" : "smooth", block: "center" });
    PF.toast("Editöre aktarıldı", "Optimize edilmiş sürüm yeni çalışma metni oldu.");
  });
  document.querySelector("#kursat-download-result")?.addEventListener("click", () => {
    const text = PF.state.currentResult?.optimized; if (!text) return;
    const blob = new Blob([text], { type:"text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `promptforge-${new Date().toISOString().slice(0,10)}.txt`;
    document.body.appendChild(link); link.click(); link.remove(); URL.revokeObjectURL(url);
    PF.toast("Dosya hazır", "Optimize edilmiş prompt indirildi.");
  });

  document.querySelector("#kursat-save-favorite").addEventListener("click", () => {
    const record = PF.state.currentResult; if (!record) return;
    if (!PF.state.favorites.some(item => item.id === record.id)) PF.state.favorites.unshift(record);
    PF.save();
    updateFavoriteButton(record);
    document.dispatchEvent(new CustomEvent("kursat:data-changed"));
    PF.toast("Favorilere eklendi");
  });

  document.querySelector("#kursat-health-analyze")?.addEventListener("click", () => {
    const text = document.querySelector("#kursat-health-input").value.trim();
    const host = document.querySelector("#kursat-health-results");
    if (!text) { PF.toast("Prompt boş"); return; }
    const analysis = analyzePrompt(text);
    const criteria = [
      ["Netlik", Math.min(100, analysis.score + 2)],
      ["Bağlam", text.length > 180 ? 86 : 58],
      ["Kısıtlar", analysis.requirements.length ? Math.min(96, 65 + analysis.requirements.length * 5) : 48],
      ["Çıktı tanımı", /(çıktı|output|format|json|liste|tablo|kod)/i.test(text) ? 90 : 52],
      ["Özgüllük", text.length > 120 ? 82 : 60],
      ["Verimlilik", analysis.issues.some(v => v[0] === "Tekrar var") ? 60 : 88]
    ];
    host.innerHTML = `<div class="kursat-score-grid">${criteria.slice(0,3).map(([name,score]) => `<div class="kursat-score-card"><small>${name}</small><strong>${score}</strong><div class="kursat-progress"><span style="width:${score}%"></span></div></div>`).join("")}</div><div class="kursat-grid-3">${criteria.slice(3).map(([name,score]) => `<div class="kursat-score-card"><small>${name}</small><strong>${score}</strong><div class="kursat-progress"><span style="width:${score}%"></span></div></div>`).join("")}</div>${analysis.issues.length ? `<article class="kursat-card kursat-analysis-card"><h3 class="kursat-analysis-title">İyileştirilebilecek noktalar</h3><div class="kursat-issue-list">${analysis.issues.map(v => `<div class="kursat-issue"><strong>${PF.escape(v[0])}</strong><p>${PF.escape(v[1])}</p></div>`).join("")}</div></article>` : ""}`;
    if (PF.state.motion === "on") {
      host.querySelectorAll(".kursat-progress span").forEach((bar, index) => {
        const width = bar.style.width;
        bar.style.width = "0%";
        setTimeout(() => { bar.style.width = width; }, 60 + index * 45);
      });
    }
  });

  document.addEventListener("kursat:load-record", event => loadRecord(event.detail));
  document.addEventListener("kursat:defaults-changed", event => {
    if (event.detail?.model && modelProfiles[event.detail.model]) PF.state.model = event.detail.model;
    if (["quality","balanced","lean"].includes(event.detail?.goal)) PF.state.goal = event.detail.goal;
    syncControls();
    updatePromptMeta({ silent:true });
  });
  document.addEventListener("kursat:prompt-to-forge", event => {
    setPromptValue(event.detail || "", { silent:true });
    PF.openView("forge");
    setTimeout(() => promptInput.focus(), 80);
  });

  window.addEventListener("keydown", event => {
    const mod = event.metaKey || event.ctrlKey;
    if (mod && event.key === "Enter") { event.preventDefault(); forge(); }
    if (mod && event.shiftKey && event.key.toLowerCase() === "c" && PF.state.currentResult?.optimized) {
      event.preventDefault();
      navigator.clipboard?.writeText(PF.state.currentResult.optimized).then(() => PF.toast("Kopyalandı")).catch(() => PF.toast("Kopyalama başarısız"));
    }
    if (mod && event.shiftKey && event.key.toLowerCase() === "f") { event.preventDefault(); toggleFocusMode(); }
  });

  document.addEventListener("kursat:forge-error", event => showForgeError(event.detail?.title, event.detail?.message));

  resetSnapshots(promptInput.value);
  syncControls();
  updatePromptMeta({ silent:true });
  window.PFForge = { analyzePrompt, estimateTokens, modelProfiles, loadRecord, showError: showForgeError, hideError: hideForgeError };
})();
