// Şablon galerisi ve {{değişken}} doldurma penceresi. Boş bırakılan alan {{…}} olarak kalıyor,
// sonra editörde doldurulabiliyor.
(() => {
  "use strict";

  const categories = {
    all: "Tümü",
    code: "Yazılım",
    content: "İçerik ve pazarlama",
    business: "İş ve verimlilik",
    learn: "Öğrenme",
    research: "Analiz ve araştırma",
    daily: "Günlük"
  };

  // model: şablonun en iyi çalıştığı hedef; seçilince editörde o model aktif oluyor.
  const templates = [
    { id: "code-review", cat: "code", title: "Kod incelemesi", desc: "Hataları, güvenlik açıklarını ve okunabilirlik sorunlarını önceliğe göre sıralar.", model: "claude",
      text: `{{dil}} ile yazılmış aşağıdaki kodu kıdemli bir yazılımcı gözüyle incele.

Kod:
{{kod}}

Şunlara bak: hatalar, güvenlik açıkları, performans sorunları ve okunabilirlik.
Mevcut davranışı değiştiren öneriler verme; değiştirmek şartsa nedenini açıkça yaz.

Çıktı: Bulguları "Kritik / Önemli / İyileştirme" başlıkları altında listele. Her bulgu için satırı, sorunu ve düzeltilmiş kodu ver.` },
    { id: "bug-hunt", cat: "code", title: "Hata ayıklama", desc: "Hata mesajından kök nedeni bulur, düzeltmeyi adım adım anlatır.", model: "gpt",
      text: `Uygulamamda bir hata alıyorum. Kök nedeni bul ve düzeltmeyi öner.

Ortam: {{dil ve framework}}
Beklenen davranış: {{beklenen}}
Gerçekleşen davranış: {{gerçekleşen}}
Hata mesajı:
{{hata mesajı}}

İlgili kod:
{{kod}}

Önce olası nedenleri olasılık sırasıyla yaz, sonra en olası neden için düzeltilmiş kodu ver. Emin olmadığın yerde hangi bilgiye ihtiyacın olduğunu sor.` },
    { id: "unit-tests", cat: "code", title: "Birim testi yaz", desc: "Sınır durumlarını da kapsayan okunabilir testler üretir.", model: "gpt",
      text: `Aşağıdaki fonksiyon için {{test kütüphanesi}} ile birim testleri yaz.

{{kod}}

Kapsa: normal kullanım, sınır değerleri, hatalı girdiler ve boş/null durumlar.
Her testin adı neyi doğruladığını açıkça söylesin. Dış bağımlılıkları taklit et (mock).

Çıktı: Tek bir çalıştırılabilir test dosyası ve altında hangi durumları kapsadığını anlatan 3–5 maddelik özet.` },
    { id: "sql", cat: "code", title: "SQL sorgusu", desc: "Tablo yapısından doğru ve performanslı sorgu yazar.", model: "deepseek",
      text: `{{veritabanı}} için bir SQL sorgusu yaz.

Tablolar:
{{tablo yapıları}}

İstenen sonuç: {{ne listelenecek}}

Kurallar: Sadece gerekli kolonları seç, SELECT * kullanma. Büyük tablolarda performansı düşünerek gerekli index önerisini ayrıca yaz.
Çıktı: Sorgu, altında 2–3 cümlelik açıklama.` },
    { id: "refactor", cat: "code", title: "Kodu sadeleştir", desc: "Davranışı bozmadan okunabilirliği artırır.", model: "claude",
      text: `Aşağıdaki kodu davranışını değiştirmeden yeniden düzenle (refactor).

{{kod}}

Hedefler: daha kısa fonksiyonlar, anlamlı isimler, tekrarların kaldırılması.
Genel API'yi (fonksiyon imzaları, dönüş tipleri) değiştirme.

Çıktı: Yeni kod ve ardından yaptığın her değişikliği tek satırla açıklayan bir liste.` },

    { id: "landing", cat: "content", title: "Açılış sayfası metni", desc: "Başlık, fayda maddeleri ve çağrı (CTA) içeren ikna edici metin.", model: "gpt",
      text: `{{ürün adı}} için bir açılış sayfası metni yaz.

Ürün ne yapıyor: {{kısa açıklama}}
Hedef kitle: {{kitle}}
En önemli 3 fayda: {{faydalar}}
Ton: {{ton}}

Çıktı sırası: 3 başlık alternatifi, alt başlık, 3 fayda bölümü (başlık + 2 cümle), sık sorulan 3 soru ve cevabı, 2 CTA buton metni.
Abartılı vaatler ve "devrim niteliğinde" gibi klişeler kullanma.` },
    { id: "social", cat: "content", title: "Sosyal medya paketi", desc: "Aynı mesajı her kanal için ayrı tonda uyarlar.", model: "gemini",
      text: `Şu duyuruyu sosyal medya için uyarla: {{duyuru}}

Marka tonu: {{ton}}
Hedef kitle: {{kitle}}

Her kanal için ayrı çıktı ver:
- LinkedIn: profesyonel, en fazla 120 kelime
- Instagram: samimi, 3–5 hashtag ile
- X: en fazla 260 karakter, 2 alternatif

Emoji kullanımını her kanalın doğasına göre ayarla.` },
    { id: "blog", cat: "content", title: "Blog yazısı taslağı", desc: "Araştırma sorusundan SEO uyumlu yazı iskeleti çıkarır.", model: "claude",
      text: `"{{konu}}" hakkında bir blog yazısı taslağı hazırla.

Okuyucu: {{okuyucu}}
Anahtar kelime: {{anahtar kelime}}
Hedef uzunluk: {{kelime sayısı}} kelime

Çıktı: SEO başlığı (60 karakteri geçmesin), meta açıklama (155 karakter), H2/H3 başlık yapısı ve her başlık altında 1–2 cümlelik içerik notu.
Bilgi uydurma; emin olmadığın istatistikleri "[kaynak gerekli]" diye işaretle.` },
    { id: "email", cat: "content", title: "Tanıtım e-postası", desc: "Konu satırı alternatifleriyle kısa ve etkili e-posta.", model: "gpt",
      text: `{{ürün veya kampanya}} için bir tanıtım e-postası yaz.

Hedef kitle: {{kitle}}
Tek cümlelik fayda: {{fayda}}
İstenen aksiyon: {{aksiyon}}

Kurallar: En fazla 150 kelime, samimi ama profesyonel ton, tek ve net bir çağrı.
Çıktı: 3 konu satırı, 3 önizleme metni ve e-posta gövdesi.` },
    { id: "product-desc", cat: "content", title: "Ürün açıklaması", desc: "E-ticaret için somut ve satışa dönük ürün metni.", model: "gemini",
      text: `E-ticaret sitemiz için {{ürün}} ürün açıklaması yaz.

Özellikler: {{özellikler}}
Müşteri: {{müşteri profili}}
Ton: {{ton}}

En fazla 120 kelime. Sonunda 3 maddelik "öne çıkan özellikler" listesi olsun.
Kullanılmayacak kelimeler: {{yasaklı kelimeler}}` },

    { id: "meeting", cat: "business", title: "Toplantı notu özeti", desc: "Dağınık notlardan kararları ve görevleri çıkarır.", model: "claude",
      text: `Aşağıdaki toplantı notlarını özetle.

{{notlar}}

Çıktı biçimi:
1. Tek paragraflık özet
2. Alınan kararlar (madde listesi)
3. Görevler tablosu: Görev | Sorumlu | Tarih
4. Açık kalan sorular

Notlarda olmayan bilgi ekleme. Sorumlu veya tarih belirtilmemişse "belirtilmedi" yaz.` },
    { id: "pro-email", cat: "business", title: "Profesyonel e-posta", desc: "Zor konuları nazik ve net bir dille yazar.", model: "gpt",
      text: `{{kime}} adlı kişiye bir e-posta yaz.

Amaç: {{amaç}}
Bağlam: {{arka plan}}
Ton: nazik, net ve profesyonel

En fazla 180 kelime. Konu satırı ekle. Karşı taraftan ne beklediğini son paragrafta tek cümleyle belirt.` },
    { id: "swot", cat: "business", title: "SWOT analizi", desc: "Bir fikir veya şirket için yapılandırılmış değerlendirme.", model: "deepseek",
      text: `{{şirket veya fikir}} için SWOT analizi yap.

Sektör: {{sektör}}
Bilinen durum: {{mevcut bilgiler}}

Her başlık (Güçlü yönler, Zayıf yönler, Fırsatlar, Tehditler) için 3–5 madde yaz.
Sonunda bu analizden çıkan en önemli 3 stratejik öneriyi sırala.
Genel geçer maddelerden kaçın; her madde verilen bağlama özgü olsun.` },
    { id: "okr", cat: "business", title: "Hedef ve OKR", desc: "Belirsiz hedefi ölçülebilir sonuçlara böler.", model: "gpt",
      text: `Şu hedef için OKR (Objectives and Key Results) hazırla: {{hedef}}

Ekip: {{ekip}}
Süre: {{çeyrek veya dönem}}

1 amaç (objective) ve 3–4 ölçülebilir anahtar sonuç yaz. Her anahtar sonucun başlangıç değeri ve hedef değeri olsun.
Sonunda bu sonuçlara ulaşmak için ilk 2 haftada yapılacak 3 somut işi listele.` },

    { id: "explain", cat: "learn", title: "Basitçe anlat", desc: "Zor bir konuyu seviyene göre örnekle anlatır.", model: "claude",
      text: `Bana {{konu}} konusunu anlat.

Seviyem: {{seviye (ör. hiç bilmiyorum)}}
Neden öğreniyorum: {{amaç}}

Önce günlük hayattan bir benzetmeyle başla, sonra kavramı adım adım açıkla.
Teknik terim kullandığında hemen yanında açıklamasını yaz.
Sonunda anladığımı kontrol etmem için 3 kısa soru sor; cevapları en altta ver.` },
    { id: "study-plan", cat: "learn", title: "Çalışma planı", desc: "Hedefe göre haftalık, gerçekçi öğrenme programı.", model: "gemini",
      text: `{{konu}} öğrenmek için bir çalışma planı hazırla.

Şu anki seviyem: {{seviye}}
Haftada ayırabileceğim süre: {{saat}}
Hedefim: {{hedef}} ({{süre}} içinde)

Planı haftalara böl. Her hafta için: öğrenilecek konular, yapılacak bir uygulama ve kendimi test etme yöntemi.
Ücretsiz kaynak öner; bağlantı uydurma, kaynağın adını yaz.` },
    { id: "quiz", cat: "learn", title: "Kendini test et", desc: "Bir metinden farklı zorlukta sorular üretir.", model: "gpt",
      text: `Aşağıdaki metinden {{soru sayısı}} soruluk bir test hazırla.

{{metin}}

Soruların üçte biri kolay, üçte biri orta, üçte biri zor olsun. Çoktan seçmeli (4 şık) ve açık uçlu soruları karıştır.
Cevap anahtarını en sonda, her cevabın kısa gerekçesiyle ver.` },

    { id: "summary", cat: "research", title: "Uzun metni özetle", desc: "Ana fikirleri ve önemli rakamları kaybetmeden özetler.", model: "claude",
      text: `Aşağıdaki metni {{okuyucu}} için özetle.

{{metin}}

Çıktı: 3 cümlelik yönetici özeti, ardından en önemli 5 bulgu (madde listesi). Metindeki rakamları ve isimleri aynen koru.
Metinde olmayan yorum ekleme; kendi değerlendirmen varsa ayrı bir "Yorum" başlığı altında yaz.` },
    { id: "compare", cat: "research", title: "Seçenekleri karşılaştır", desc: "Kriterlere göre tablo ve net bir öneri üretir.", model: "gpt",
      text: `Şu seçenekleri karşılaştır: {{seçenekler}}

Benim durumum: {{ihtiyaç ve kısıtlar}}
Önemli kriterler: {{kriterler}}

Çıktı: Kriterlere göre bir karşılaştırma tablosu, her seçeneğin en büyük artısı ve eksisi, ve benim durumuma göre tek bir önerin (gerekçesiyle).
Bilmediğin güncel fiyat veya sürüm bilgisini tahmin etme; "kontrol edilmeli" diye belirt.` },
    { id: "data", cat: "research", title: "Veriden içgörü", desc: "Tablodaki eğilimleri ve dikkat çeken noktaları bulur.", model: "deepseek",
      text: `Aşağıdaki veriyi analiz et.

{{veri (CSV veya tablo)}}

Soru: {{cevap aradığım soru}}

Önce verinin yapısını 2 cümleyle özetle. Sonra eğilimleri, aykırı değerleri ve dikkat çeken ilişkileri yaz.
Her iddiayı verideki somut sayılarla destekle. Korelasyonu nedensellik gibi sunma.` },
    { id: "feedback", cat: "research", title: "Kullanıcı yorumlarını sınıfla", desc: "Yorumları temalara ayırır ve öncelik çıkarır.", model: "gemini",
      text: `Aşağıdaki kullanıcı yorumlarını analiz et.

{{yorumlar}}

Yorumları temalara ayır (ör. fiyat, performans, destek). Her tema için: yorum sayısı, genel duygu (olumlu/olumsuz/karışık) ve temsil eden 1 alıntı.
Sonunda ürün ekibine öncelik sırasıyla 3 öneri ver.
Çıktı biçimi: {{tablo veya madde listesi}}` },

    { id: "trip", cat: "daily", title: "Gezi planı", desc: "Bütçe ve ilgi alanına göre gün gün program.", model: "gemini",
      text: `{{şehir}} için {{gün sayısı}} günlük bir gezi planı hazırla.

Bütçe: {{bütçe}}
İlgi alanlarım: {{ilgi alanları}}
Seyahat eden: {{kişi ve yaş}}

Her gün için sabah, öğle ve akşam önerisi; aralarındaki ulaşımı ve yaklaşık süreyi yaz.
Kesin olmayan açılış saatleri ve fiyatlar için "kontrol et" notu ekle.` },
    { id: "recipe", cat: "daily", title: "Evdekilerle yemek", desc: "Elindeki malzemelerle yapılabilecek tarifler.", model: "gpt",
      text: `Elimde şu malzemeler var: {{malzemeler}}

Kaç kişilik: {{kişi sayısı}}
Ayırabileceğim süre: {{süre}}
Diyet/alerji: {{kısıtlar}}

Bu malzemelerle yapabileceğim 3 tarif öner. Her tarif için: toplam süre, eksikse en fazla 2 ek malzeme ve adım adım yapılış.` },
    { id: "decision", cat: "daily", title: "Karar vermeme yardım et", desc: "Artı-eksi ve senaryolarla düşünmeni yapılandırır.", model: "claude",
      text: `Bir karar vermem gerekiyor: {{karar}}

Seçenekler: {{seçenekler}}
Benim için önemli olanlar: {{değerler ve öncelikler}}
Kaygılarım: {{kaygılar}}

Kararı benim yerime verme. Her seçenek için artıları, eksileri ve 1 yıl sonraki olası durumu yaz.
Sonunda kararımı netleştirecek 3 soru sor.` }
  ];

  let activeCat = "all";
  let query = "";

  const esc = value => PF.escape(value);
  const modelName = key => ({ gpt: "GPT", claude: "Claude", gemini: "Gemini", deepseek: "DeepSeek", universal: "Evrensel" }[key] || key);

  // Sırayı koruyup tekrarları at.
  function variablesOf(text) {
    const seen = new Set();
    for (const match of String(text).matchAll(/\{\{\s*([^{}]+?)\s*\}\}/g)) seen.add(match[1]);
    return [...seen];
  }

  function fill(text, values) {
    return String(text).replace(/\{\{\s*([^{}]+?)\s*\}\}/g, (all, name) => values[name]?.trim() ? values[name].trim() : all);
  }

  function filtered() {
    const needle = query.trim().toLocaleLowerCase("tr-TR");
    return templates.filter(t => (activeCat === "all" || t.cat === activeCat)
      && (!needle || `${t.title} ${t.desc} ${categories[t.cat]}`.toLocaleLowerCase("tr-TR").includes(needle)));
  }

  function renderFilters() {
    const host = document.querySelector("#kursat-template-filters");
    if (!host) return;
    host.innerHTML = Object.entries(categories).map(([key, label]) => {
      const count = key === "all" ? templates.length : templates.filter(t => t.cat === key).length;
      return `<button type="button" class="kursat-chip${key === activeCat ? " is-active" : ""}" data-kursat-template-cat="${key}">${esc(label)}<small>${count}</small></button>`;
    }).join("");
  }

  function renderGrid() {
    const host = document.querySelector("#kursat-template-grid");
    if (!host) return;
    const list = filtered();
    host.innerHTML = list.length ? list.map(t => {
      const vars = variablesOf(t.text).length;
      return `<button type="button" class="kursat-template-card" data-kursat-template="${t.id}">
        <span class="kursat-template-meta"><span class="kursat-badge">${esc(categories[t.cat])}</span><span class="kursat-template-model">${esc(modelName(t.model))} için</span></span>
        <strong>${esc(t.title)}</strong>
        <span class="kursat-template-desc">${esc(t.desc)}</span>
        <span class="kursat-template-foot"><span>${vars} boşluk</span><span class="kursat-template-open">Kullan <svg viewBox="0 0 24 24"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></span></span>
      </button>`;
    }).join("") : `<div class="kursat-empty kursat-template-empty"><div class="kursat-empty-icon"><svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="6"/><path d="m16 16 4 4"/></svg></div><h3>Şablon bulunamadı</h3><p>Farklı bir kelime dene veya "Tümü" kategorisine dön.</p></div>`;
  }

  // Hem şablonlar hem editördeki değişken rozetleri bu pencereyi kullanıyor.
  function openFill({ title, text, model, source }) {
    const vars = variablesOf(text);
    const body = document.querySelector("#kursat-panel-body");
    document.querySelector("#kursat-panel-title").textContent = title;
    const values = {};
    body.innerHTML = `
      <p class="kursat-modal-copy">${vars.length ? "Boşlukları doldur. Boş bıraktığın alanlar <code>{{…}}</code> olarak kalır; sonra editörde doldurabilirsin." : "Bu promptta doldurulacak boşluk yok."}</p>
      ${vars.length ? `<div class="kursat-fill-fields">${vars.map((name, i) => `
        <div class="kursat-field"><label for="kursat-fill-${i}">${esc(name)}</label>
        <textarea class="kursat-textarea kursat-fill-input" id="kursat-fill-${i}" data-kursat-fill="${esc(name)}" rows="1" placeholder="${esc(name)}"></textarea></div>`).join("")}</div>` : ""}
      <div class="kursat-builder-preview"><header>Önizleme</header><pre id="kursat-fill-preview"></pre></div>
      <div class="kursat-actions-end">
        <button class="kursat-button" type="button" data-kursat-panel-close>Vazgeç</button>
        <button class="kursat-button" type="button" id="kursat-fill-to-forge">Editöre aktar</button>
        <button class="kursat-button kursat-button--accent" type="button" id="kursat-fill-optimize">Aktar ve optimize et</button>
      </div>`;

    const preview = body.querySelector("#kursat-fill-preview");
    const update = () => { preview.innerHTML = esc(fill(text, values)).replace(/\{\{[^{}]+\}\}/g, m => `<mark>${m}</mark>`); };
    body.querySelectorAll("[data-kursat-fill]").forEach(input => input.addEventListener("input", () => {
      values[input.dataset.kursatFill] = input.value;
      input.style.height = "auto";
      input.style.height = `${Math.min(input.scrollHeight, 180)}px`;
      update();
    }));
    update();

    const send = optimize => {
      PF.closeBackdrop(document.querySelector("#kursat-panel-backdrop"));
      if (model && source === "template") document.dispatchEvent(new CustomEvent("kursat:defaults-changed", { detail: { model } }));
      document.dispatchEvent(new CustomEvent("kursat:prompt-to-forge", { detail: fill(text, values) }));
      document.dispatchEvent(new CustomEvent("kursat:milestone", { detail: source === "template" ? "template" : "variables" }));
      if (optimize) setTimeout(() => document.querySelector("#kursat-forge-button")?.click(), 180);
    };
    body.querySelector("#kursat-fill-to-forge").addEventListener("click", () => send(false));
    body.querySelector("#kursat-fill-optimize").addEventListener("click", () => send(true));
    PF.openBackdrop(document.querySelector("#kursat-panel-backdrop"));
  }

  function bind() {
    document.querySelector("#kursat-template-filters")?.addEventListener("click", event => {
      const button = event.target.closest("[data-kursat-template-cat]");
      if (!button) return;
      activeCat = button.dataset.kursatTemplateCat;
      renderFilters();
      renderGrid();
    });
    document.querySelector("#kursat-template-search")?.addEventListener("input", event => { query = event.target.value; renderGrid(); });
    document.querySelector("#kursat-template-grid")?.addEventListener("click", event => {
      const card = event.target.closest("[data-kursat-template]");
      const template = templates.find(t => t.id === card?.dataset.kursatTemplate);
      if (template) openFill({ title: template.title, text: template.text, model: template.model, source: "template" });
    });
    document.querySelector("#kursat-variable-list")?.addEventListener("click", event => {
      if (!event.target.closest(".kursat-variable-chip")) return;
      const text = document.querySelector("#kursat-prompt-input")?.value || "";
      if (variablesOf(text).length) openFill({ title: "Değişkenleri doldur", text, source: "editor" });
    });
  }

  renderFilters();
  renderGrid();
  bind();
  window.PFTemplates = { list: templates, categories, open: id => { const t = templates.find(x => x.id === id); if (t) openFill({ title: t.title, text: t.text, model: t.model, source: "template" }); } };
})();
