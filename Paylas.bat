@echo off
rem PromptForge'u internete acar: once API'yi, sonra Cloudflare tunelini baslatir.
rem Cift tikla. Tunel penceresinde "https://....trycloudflare.com" linki cikar; onu paylas.
rem Kapatmak icin iki pencereyi de kapat (link o an calismayi birakir).

cd /d "%~dp0"
start "PromptForge API" cmd /k dotnet run --launch-profile http

echo API baslatiliyor, 20 saniye bekleniyor...
timeout /t 20 /nobreak >nul

start "PromptForge Tunel - linki burada ara" cmd /k ""C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --no-autoupdate --url http://localhost:5299"
