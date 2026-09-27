(() => {
  "use strict";

  const session = window.PromptForgeSession;
  const APP_URL = "../app.html#forge";

  function showMessage(text, type="success") { const box=document.querySelector("#kursat-auth-message"); if(!box)return; box.textContent=text; box.className=`kursat-auth-message is-visible ${type === "success" ? "is-success" : "is-error"}`; }

  // Butonu "yükleniyor" durumuna al; hata olursa eski yazısına geri döndürebilmek için orijinal metni saklıyoruz.
  function setLoading(form, text="Açılıyor...") { const button=form?.querySelector("button[type=submit]"); if(!button)return; button.dataset.kursatLabel ??= button.textContent; button.disabled=true; button.textContent=text; }
  function resetLoading(form) { const button=form?.querySelector("button[type=submit]"); if(!button)return; button.disabled=false; if(button.dataset.kursatLabel)button.textContent=button.dataset.kursatLabel; }
  function navigate(form, href, message="Çalışma alanı açılıyor.") { showMessage(message,"success"); setLoading(form); setTimeout(()=>window.location.href=href,220); }
  const valueOf = selector => document.querySelector(selector)?.value.trim() || "";

  // Zaten giriş yapmış biri giriş/kayıt sayfasını açarsa doğrudan çalışma alanına gönder.
  if (session?.getToken() && document.querySelector("#kursat-login-form, #kursat-register-form")) window.location.replace(APP_URL);

  document.querySelectorAll("[data-kursat-password-toggle]").forEach(button => button.addEventListener("click", () => { const input=document.querySelector(`#${button.dataset.kursatPasswordToggle}`); if(!input)return; input.type=input.type==="password"?"text":"password"; button.setAttribute("aria-label", input.type==="password"?"Şifreyi göster":"Şifreyi gizle"); }));

  // Ortak akış: API'ye gönder → başarılıysa token'ı sakla ve çalışma alanına geç → değilse hatayı göster.
  async function submitAuth(form, path, body, remember, successMessage) {
    setLoading(form, "Kontrol ediliyor...");
    const { ok, data } = await session.request(path, { method: "POST", body });
    if (!ok || !data?.token) {
      showMessage(data?.message || "İşlem tamamlanamadı. Lütfen tekrar dene.", "error");
      resetLoading(form);
      return;
    }
    session.saveSession(data.token, data.user, remember);
    navigate(form, APP_URL, successMessage);
  }

  // GİRİŞ: POST /api/auth/login
  document.querySelector("#kursat-login-form")?.addEventListener("submit", event => {
    event.preventDefault();
    const form = event.currentTarget;
    const email = valueOf("#kursat-login-user");
    const password = document.querySelector("#kursat-login-password")?.value || "";
    if (!email || !password) { showMessage("E-posta ve şifre gerekli.", "error"); return; }
    const remember = document.querySelector("#kursat-login-remember")?.checked ?? true;
    submitAuth(form, "/api/auth/login", { email, password }, remember, "Giriş başarılı. Çalışma alanına yönlendiriliyorsun.");
  });

  // KAYIT: POST /api/auth/register (kayıt sonrası otomatik giriş yapılır)
  document.querySelector("#kursat-register-form")?.addEventListener("submit", event => {
    event.preventDefault();
    const form = event.currentTarget;
    const displayName = valueOf("#kursat-register-name");
    const email = valueOf("#kursat-register-email");
    const password = document.querySelector("#kursat-register-password")?.value || "";
    if (!email || !password) { showMessage("E-posta ve şifre gerekli.", "error"); return; }
    if (password.length < 6) { showMessage("Şifre en az 6 karakter olmalı.", "error"); return; }
    submitAuth(form, "/api/auth/register", { email, password, displayName }, true, "Hesabın oluşturuldu. Çalışma alanına yönlendiriliyorsun.");
  });

  // Aşağıdakiler henüz backend'e bağlı değil (şifre sıfırlama, e-posta doğrulama, 2FA); sadece ekran akışını gösterir.
  document.querySelector("#kursat-forgot-form")?.addEventListener("submit", event => { event.preventDefault(); navigate(event.currentTarget,"recover-password.html","Şifre sıfırlama akışına yönlendiriliyorsun."); });
  document.querySelector("#kursat-recover-form")?.addEventListener("submit", event => { event.preventDefault(); const p1=document.querySelector("#kursat-recover-password")?.value||""; const p2=document.querySelector("#kursat-recover-confirm")?.value||""; if(p1!==p2){showMessage("Şifre alanları eşleşmiyor.","error");return;} navigate(event.currentTarget,"sign-in.html","Şifren güncellendi. Giriş ekranına yönlendiriliyorsun."); });
  document.querySelector("#kursat-confirm-form")?.addEventListener("submit", event => { event.preventDefault(); navigate(event.currentTarget,"../app.html#settings","Doğrulama tamamlandı."); });

  const otpInputs=[...document.querySelectorAll("[data-kursat-otp]")];
  otpInputs.forEach((input,index)=>{input.addEventListener("input",()=>{input.value=input.value.replace(/\D/g,"").slice(0,1);if(input.value&&otpInputs[index+1])otpInputs[index+1].focus();});input.addEventListener("keydown",event=>{if(event.key==="Backspace"&&!input.value&&otpInputs[index-1])otpInputs[index-1].focus();});});
  document.querySelector("#kursat-otp-form")?.addEventListener("submit",event=>{event.preventDefault();const code=otpInputs.map(v=>v.value).join("");if(code.length!==6){showMessage("Lütfen 6 haneli doğrulama kodunu gir.","error");return;}navigate(event.currentTarget,"../app.html#forge","Doğrulama tamamlandı.");});
})();
