(() => {
  "use strict";
  // Prompt Analizi: promptu öğretici biçimde değerlendirir.
  // Önce AI'a sorar (POST /api/prompts/analyze); AI yoksa, hata verirse veya kota dolduysa yerel analize düşer.
  // Yerel analiz de sabit puan vermez: promptun kelimelerine, rakamlarına, belirsiz ifadelerine bakar ve bunlara atıf yapar.
  const PF = window.PF;
  if (!PF) return;
  const esc = PF.escape;

  const input = document.querySelector("#kursat-health-input");
  const button = document.querySelector("#kursat-health-analyze");
  const host = document.querySelector("#kursat-health-results");
  if (!input || !button || !host) return;

  // ===== Yerel analiz =====
  const VAGUE = ["bunu", "şunu", "bunları", "şunları", "orayı", "burayı", "iyi bir", "güzel bir", "daha iyi", "bir şeyler", "falan", "filan", "vs.", "vb.", "gibi şeyler", "something", "stuff", "better", "nice"];
  const FILLER = ["lütfen", "rica etsem", "acaba", "mümkünse", "çok çok", "gerçekten", "aslında", "please", "kindly"];
  const TASK_VERBS = /\b(yap|yaz|oluştur|üret|hazırla|incele|analiz et|özetle|çevir|dönüştür|karşılaştır|listele|açıkla|tasarla|düzelt|iyileştir|öner|planla|hesapla|bul|write|create|generate|analy[sz]e|summari[sz]e|translate|compare|list|explain|design|fix|improve|suggest|plan)\w*/i;
  const AUDIENCE = /(hedef kitle|kitle|okuyucu|kullanıcı|müşteri|öğrenci|çalışan|yönetici|geliştirici|çocuk|uzman|başlangıç seviyesi|audience|reader|customer|user|beginner)/i;
  const SITUATION = /(mevcut|şu an|şu anda|projem|projemiz|şirket|markam|ürünüm|ürünümüz|elimde|arka plan|çünkü|amacım|amacımız|context|currently|because|our |my )/i;
  const CONSTRAINT = /(kesinlikle|mutlaka|asla|değiştirme|kullanma|olmamalı|olmalı|en fazla|en az|sadece|yalnızca|kaçın|dışında|must|never|always|only|avoid|at most|at least|do not|don't)/i;
  const FORMAT = /(liste|madde|tablo|json|markdown|paragraf|cümle|kelime|karakter|başlık|e-posta|mail|kod|dosya|özet|adım adım|format|biçim|çıktı|uzunluk|hashtag|slayt|rapor|table|list|bullet|paragraph|words|sentences|heading|output|format|length)/i;

  function wordsOf(text) { return text.split(/\s+/).filter(Boolean); }
  const scoreOf = value => Math.max(0, Math.min(10, Math.round(value)));
  const found = (list, text) => list.filter(term => text.toLocaleLowerCase("tr-TR").includes(term));

  function localAnalysis(text) {
    const words = wordsOf(text);
    const lower = text.toLocaleLowerCase("tr-TR");
    const sentences = text.split(/[.!?\n]+/).map(s => s.trim()).filter(Boolean);
    const vague = found(VAGUE, text);
    const filler = found(FILLER, text);
    const numbers = (text.match(/\d+/g) || []).length;
    const quotes = (text.match(/["“”'].+?["“”']/g) || []).length;
    const variables = (text.match(/\{\{[^}]+\}\}|\[[A-ZÇĞİÖŞÜ][^\]]+\]/g) || []).length;
    const firstSentence = sentences[0] || text;
    const hasTask = TASK_VERBS.test(text);
    const duplicateSentences = sentences.length - new Set(sentences.map(s => s.toLocaleLowerCase("tr-TR"))).size;
    const constraintHits = (lower.match(new RegExp(CONSTRAINT.source, "gi")) || []).length;
    const formatWords = [...new Set((lower.match(new RegExp(FORMAT.source, "gi")) || []).map(v => v.toLocaleLowerCase("tr-TR")))];

    const criteria = [];

    // Netlik: net bir görev fiili var mı, belirsiz ifade var mı?
    let clarity = (hasTask ? 6 : 2) + (firstSentence.split(/\s+/).length <= 25 ? 2 : 0) + (vague.length ? -2 * Math.min(vague.length, 2) : 2);
    criteria.push({
      key: "clarity", label: "Netlik", score: scoreOf(clarity),
      feedback: !hasTask ? "Promptta modelden ne yapmasını istediğini söyleyen net bir fiil yok (yaz, özetle, karşılaştır gibi)."
        : vague.length ? `"${vague.slice(0, 2).join("\", \"")}" ifadesi belirsiz; model neyi kastettiğini tahmin etmek zorunda kalır.`
        : `Görev "${firstSentence.slice(0, 70)}${firstSentence.length > 70 ? "…" : ""}" cümlesiyle açıkça belirtilmiş.`,
      fix: !hasTask ? "Promptu tek bir görev cümlesiyle başlat. Örnek: \"Bu metni 3 maddede özetle.\""
        : vague.length ? `"${vague[0]}" yerine neyi kastettiğini doğrudan yaz.` : "Görevi bu netlikte tutmaya devam et."
    });

    // Bağlam: kitle, durum, amaç bilgisi ve yeterli uzunluk.
    const hasAudience = AUDIENCE.test(text), hasSituation = SITUATION.test(text);
    criteria.push({
      key: "context", label: "Bağlam", score: scoreOf((hasAudience ? 3.5 : 0) + (hasSituation ? 3.5 : 0) + Math.min(3, words.length / 15)),
      feedback: hasAudience && hasSituation ? "Hem hedef kitle hem de mevcut durum belirtilmiş; model kararlarını buna göre verebilir."
        : words.length < 12 ? `Prompt sadece ${words.length} kelime; model senin durumunu bilmediği için genel geçer bir cevap üretecek.`
        : !hasAudience ? "Sonucun kimin için olduğu yazmıyor; ton ve detay seviyesi tahmine kalıyor."
        : "Mevcut durum veya amaç anlatılmamış; model neden bu işi istediğini bilmiyor.",
      fix: hasAudience && hasSituation ? "Bağlam yeterli görünüyor." : !hasAudience ? "Bir cümle ekle: \"Bu içerik [kimin] için ve onların [ihtiyacı] şu.\"" : "Bir cümle ekle: \"Şu an [durum]; bunu [amaç] için istiyorum.\""
    });

    // Özgüllük: rakam, alıntı, özel isim, değişken.
    const properNouns = (text.match(/(?<![.!?]\s)(?<!^)\b[A-ZÇĞİÖŞÜ][a-zçğıöşü]{2,}/g) || []).length;
    const specifics = numbers + quotes + variables + Math.min(properNouns, 3);
    criteria.push({
      key: "specificity", label: "Özgüllük", score: scoreOf(2 + specifics * 2 + Math.min(2, words.length / 30)),
      feedback: specifics >= 3 ? `Promptta ${[numbers && `${numbers} sayı`, properNouns && "özel isimler", variables && `${variables} değişken`, quotes && "alıntılar"].filter(Boolean).join(", ")} var; model somut bilgiyle çalışacak.`
        : specifics ? "Birkaç somut ayrıntı var ama çoğu ifade genel kalıyor."
        : "Hiç rakam, isim veya somut örnek yok; model boşlukları kendi varsayımlarıyla dolduracak.",
      fix: specifics >= 3 ? "Somutluğu koru." : "Genel ifadeleri rakam ve isimle değiştir. Örnek: \"kısa\" yerine \"en fazla 120 kelime\"."
    });

    // Kısıtlar: kurallar, yasaklar, sınırlar.
    criteria.push({
      key: "constraints", label: "Kısıtlar", score: scoreOf(constraintHits ? 4 + constraintHits * 2 : 1),
      feedback: constraintHits ? `${constraintHits} kural veya sınır ifadesi var ("${(lower.match(CONSTRAINT) || [""])[0]}" gibi); model bunlara uymaya çalışacak.`
        : "Uyulması gereken hiçbir kural veya sınır yazmıyor; model istemediğin şeyleri de yapabilir.",
      fix: constraintHits >= 2 ? "Kuralları ayrı satırlarda madde madde yazarsan daha az atlanır." : "En az bir kural ekle. Örnek: \"Teknik terim kullanma.\" veya \"Mevcut yapıyı değiştirme.\""
    });

    // Çıktı biçimi: format, uzunluk, yapı.
    criteria.push({
      key: "output", label: "Çıktı biçimi", score: scoreOf(formatWords.length ? 4 + formatWords.length * 2 + (numbers ? 1 : 0) : 1),
      feedback: formatWords.length ? `Beklenen çıktı "${formatWords.slice(0, 3).join("\", \"")}" ile tarif edilmiş.`
        : "Sonucun hangi biçimde ve uzunlukta olacağı yazmıyor; model bunu kendisi seçecek (genelde fazla uzun).",
      fix: formatWords.length >= 2 ? "Uzunluğu da sayıyla verirsen daha tutarlı sonuç alırsın." : "Sonuna ekle: \"Çıktıyı 5 maddelik bir liste olarak, her madde en fazla 2 cümle olacak şekilde ver.\""
    });

    // Verimlilik: tekrar, dolgu kelimesi, aşırı uzunluk.
    const efficiencyPenalty = duplicateSentences * 3 + filler.length * 1.5 + (words.length > 400 ? 2 : 0);
    criteria.push({
      key: "efficiency", label: "Verimlilik", score: scoreOf(10 - efficiencyPenalty),
      feedback: duplicateSentences ? "Aynı cümle birden fazla kez geçiyor; tekrar modele ek bilgi vermez."
        : filler.length ? `"${filler.slice(0, 2).join("\", \"")}" gibi ifadeler sonuca katkı sağlamıyor.`
        : "Gereksiz tekrar veya dolgu ifadesi yok.",
      fix: efficiencyPenalty ? "Tekrarları ve nezaket kalıplarını çıkar; model bunlara ihtiyaç duymaz." : "Böyle sade tutmaya devam et."
    });

    criteria.forEach(c => { c.status = c.score >= 8 ? "good" : c.score >= 4 ? "improve" : "missing"; });
    const weights = { clarity: 1.4, context: 1.2, specificity: 1, constraints: .9, output: 1.1, efficiency: .6 };
    const total = criteria.reduce((sum, c) => sum + c.score * weights[c.key], 0) / Object.values(weights).reduce((a, b) => a + b, 0);
    const sorted = [...criteria].sort((a, b) => a.score - b.score);
    const lessons = {
      clarity: "Model zihin okuyamaz: ne istediğini tek, net bir cümleyle söylediğinde sonuç ilk seferde doğru olur.",
      context: "Bağlam, modelin senin yerine düşünmesini sağlar; kimin için ve neden istediğini söylemek cevabı kişiselleştirir.",
      specificity: "Somut ayrıntılar (sayı, isim, örnek) belirsizliği yok eder; \"kısa\" herkese göre farklıdır, \"100 kelime\" değildir.",
      constraints: "Yapılmamasını istediğin şeyleri açıkça yaz; yazılmayan kural yok sayılır.",
      output: "Sonucun biçimini tarif etmek, cevabı doğrudan kullanabilmenin en kısa yoludur.",
      efficiency: "Kısa ve tekrarsız promptlar daha az yanlış anlaşılır ve daha az token harcar."
    };

    return {
      source: "local",
      overall: Math.round(total * 10),
      summary: sorted[0].score >= 8 ? "Prompt tüm temel kriterlerde iyi durumda." : `En zayıf nokta: ${sorted[0].label.toLocaleLowerCase("tr-TR")}. ${sorted[0].feedback}`,
      criteria,
      strengths: criteria.filter(c => c.score >= 8).map(c => `${c.label}: ${c.feedback}`),
      improvements: sorted.filter(c => c.score < 8).slice(0, 3).map(c => c.fix),
      rewrite: null,
      lesson: lessons[sorted[0].key]
    };
  }

  // ===== Görünüm =====
  function render(result, notice) {
    const statusText = { good: "İyi", improve: "Geliştirilebilir", missing: "Eksik" };
    const sourceBadge = result.source === "ai"
      ? `<span class="kursat-badge kursat-badge--success">AI analizi · ${esc(result.engine || "")}</span>`
      : `<span class="kursat-badge kursat-badge--warning">Yerel analiz</span>`;
    host.innerHTML = `
      ${notice ? `<div class="kursat-empty-mini">${esc(notice)}</div>` : ""}
      <article class="kursat-card kursat-analysis-head">
        <div class="kursat-score-orbit kursat-score-orbit--lg" style="--score:${result.overall}"><strong>${result.overall}</strong></div>
        <div class="kursat-analysis-summary"><div class="kursat-analysis-meta">${sourceBadge}</div><h2>${result.overall >= 80 ? "Güçlü bir prompt" : result.overall >= 55 ? "İyi bir başlangıç, geliştirilebilir" : "Modelin tahmin etmesi gereken çok şey var"}</h2><p>${esc(result.summary)}</p></div>
      </article>
      <div class="kursat-criteria-grid">${result.criteria.map(c => `
        <article class="kursat-card kursat-criterion is-${c.status}">
          <header><h3>${esc(c.label)}</h3><span class="kursat-criterion-score">${c.score}<small>/10</small></span></header>
          <div class="kursat-progress"><span style="width:${c.score * 10}%"></span></div>
          <span class="kursat-criterion-status">${statusText[c.status] || ""}</span>
          <p>${esc(c.feedback)}</p>
          ${c.status !== "good" && c.fix ? `<div class="kursat-criterion-fix"><strong>Nasıl düzeltirsin</strong><span>${esc(c.fix)}</span></div>` : ""}
        </article>`).join("")}</div>
      <div class="kursat-two-col">
        <article class="kursat-card kursat-analysis-card"><h3>İyi yaptıkların</h3>${result.strengths.length ? `<ul class="kursat-analysis-list">${result.strengths.map(s => `<li>${esc(s)}</li>`).join("")}</ul>` : `<p>Henüz belirgin bir güçlü yön yok; aşağıdaki iyileştirmelerle başla.</p>`}</article>
        <article class="kursat-card kursat-analysis-card"><h3>Önce bunları düzelt</h3>${result.improvements.length ? `<ol class="kursat-analysis-list">${result.improvements.map(s => `<li>${esc(s)}</li>`).join("")}</ol>` : `<p>Kritik bir eksik yok.</p>`}</article>
      </div>
      ${result.rewrite ? `<article class="kursat-card kursat-analysis-card"><div class="kursat-toolbar"><h3>Önerilerin uygulandığı hâli</h3><div class="kursat-toolbar-right"><button class="kursat-button" type="button" id="kursat-analysis-copy">Kopyala</button><button class="kursat-button kursat-button--primary" type="button" id="kursat-analysis-use">Editöre aktar</button></div></div><pre class="kursat-result-code" id="kursat-analysis-rewrite">${esc(result.rewrite)}</pre></article>`
        : `<div class="kursat-empty-mini">Önerilerin uygulandığı örnek yeni hâl sadece AI analizinde gelir.</div>`}
      ${result.lesson ? `<article class="kursat-card kursat-lesson"><span>Bu prompttan öğren</span><p>${esc(result.lesson)}</p></article>` : ""}`;

    host.querySelector("#kursat-analysis-use")?.addEventListener("click", () => document.dispatchEvent(new CustomEvent("kursat:prompt-to-forge", { detail: result.rewrite })));
    host.querySelector("#kursat-analysis-copy")?.addEventListener("click", async () => {
      try { await navigator.clipboard.writeText(result.rewrite); PF.toast("Kopyalandı"); } catch { PF.toast("Kopyalama başarısız"); }
    });
    if (PF.state.motion === "on") {
      host.querySelectorAll(".kursat-progress span").forEach((bar, index) => {
        const width = bar.style.width; bar.style.width = "0%";
        setTimeout(() => { bar.style.width = width; }, 60 + index * 40);
      });
    }
  }

  async function analyze() {
    const text = input.value.trim();
    if (!text) { PF.toast("Prompt boş", "Analiz için bir prompt yaz."); input.focus(); return; }
    const label = button.textContent;
    button.disabled = true;
    button.textContent = "Analiz ediliyor…";
    host.innerHTML = `<div class="kursat-empty-mini">Prompt değerlendiriliyor…</div>`;
    try {
      const { ok, status, data } = await PF.api.post("/api/prompts/analyze", { prompt: text });
      if (ok) {
        render(data);
        if (data.usage) document.dispatchEvent(new CustomEvent("kursat:usage-changed", { detail: data.usage }));
      } else {
        // Kota doluysa veya AI kullanılamıyorsa yine de öğretici bir sonuç göster.
        const reason = status === 429 ? data?.message : status === 503 ? `${data?.message || "AI şu an kullanılamıyor."} Yerel analiz gösteriliyor.` : "AI analizine ulaşılamadı; yerel analiz gösteriliyor.";
        if (status === 429 && data?.usage) document.dispatchEvent(new CustomEvent("kursat:usage-changed", { detail: data.usage }));
        render(localAnalysis(text), reason);
      }
      // Rehberdeki "Promptunu analiz ettir" görevini tamamla.
      document.dispatchEvent(new CustomEvent("kursat:milestone", { detail: "analysis" }));
    } finally {
      button.disabled = false;
      button.textContent = label;
    }
  }

  button.addEventListener("click", analyze);
  input.addEventListener("keydown", event => { if ((event.ctrlKey || event.metaKey) && event.key === "Enter") { event.preventDefault(); analyze(); } });
  document.querySelector("#kursat-health-from-editor")?.addEventListener("click", () => {
    const text = document.querySelector("#kursat-prompt-input")?.value.trim();
    if (!text) { PF.toast("Editör boş", "Optimize ekranında bir prompt yazınca buraya alabilirsin."); return; }
    input.value = text;
    input.focus();
  });

  window.PFAnalysis = { localAnalysis };
})();
