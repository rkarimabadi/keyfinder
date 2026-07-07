# KeyFinder

اسکنر حرفه‌ای نشت کلیدهای API در گیت‌هاب، سازگار با دات‌نت 9 (ویندوز فرم)

KeyFinder یک ابزار امنیتی برای شناسایی کلیدهای API لو رفته در مخازن عمومی گیت‌هاب است. این نرم‌افزار با الهام از KeyHunter (نوشته شده با Rust) طراحی شده و با معماری WinForms و دات‌نت 9 پیاده‌سازی شده است.

---

## قابلیت‌ها

- اسکن همزمان بیش از 25 سرویس مختلف از جمله OpenAI، Anthropic، Google، GitHub، Stripe، Discord، Telegram، AWS و غیره
- جستجوی چندمرحله‌ای برای هر سرویس (جستجوی الگو، جستجوی فایل env، جستجوی متغیر محیطی)
- فیلتر هوشمند کلیدهای جعلی و Placeholder
- نمایش کلیدها به صورت ماسک‌شده (امن)
- تایید زنده بودن کلیدها با ارسال درخواست واقعی به API سرویس‌دهنده
- خروجی به فرمت JSON و CSV
- ذخیره خودکار نتایج با تاریخ و زمان
- نمایش لاگ شبکه شامل وضعیت HTTP، هدرها و خطاها
- پشتیبانی از توقف امن عملیات در هر لحظه
- قابلیت کپی کلید اصلی (unmasked) با یک کلیک
- ذخیره تنظیمات و توکن در مسیر `%LOCALAPPDATA%/KeyFinder`

---

## سرویس‌های پشتیبانی شده

دسته‌بندی | سرویس‌ها
--- | ---
هوش مصنوعی | OpenAI, Anthropic, Google AI, xAI Grok, DeepSeek, HuggingFace, Replicate, Perplexity, Groq, Fireworks
ابر | AWS (Access Key)
پرداخت | Stripe (Live & Restricted)
ارتباطات | Twilio, SendGrid, Mailgun
پلتفرم توسعه | GitHub Token, GitLab, NPM, PyPI
شبکه‌های اجتماعی | Slack Bot, Discord Bot, Telegram Bot
پایگاه داده | MongoDB, PostgreSQL
سایر | New Relic, Mapbox, Sentry, PlanetScale, Doppler, Private Key

---

## پیش‌نیازها

- دات‌نت 9.0 SDK یا بالاتر
- یک توکن گیت‌هاب با دسترسی `public_repo`

---

## نصب و اجرا

1. **دریافت توکن گیت‌هاب**  
   به آدرس https://github.com/settings/tokens مراجعه کرده و یک توکن جدید با دسترسی `public_repo` بسازید.

2. **ساخت پروژه**

   ```bash
   dotnet build
   ```

3. **اجرا**

   ```bash
   dotnet run --project KeyFinder
   ```

   یا فایل `KeyFinder.exe` را در پوشه `bin/Debug/net9.0-windows` اجرا کنید.

4. **تنظیم توکن**  
   توکن گیت‌هاب را در فیلد مربوطه در برنامه وارد کنید. تنظیمات به صورت خودکار در `%LOCALAPPDATA%/KeyFinder/settings.json` ذخیره می‌شود.

---

## راهنمای استفاده

1. توکن گیت‌هاب را وارد کنید.
2. سرویس مورد نظر را از لیست انتخاب کنید (یا `all` برای همه سرویس‌ها).
3. حداکثر نتایج را تنظیم کنید.
4. دکمه `Scan` را بزنید.
5. پس از اتمام اسکن، می‌توانید کلیدها را با دکمه `Verify Selected` تایید کنید.
6. با کلیک روی دکمه Copy در هر سطر، کلید اصلی (unmasked) در کلیپ‌بورد کپی می‌شود.
7. با دکمه `Export JSON` نتایج را به فرمت JSON یا CSV ذخیره کنید.

---

## ساختار پروژه

```
KeyFinder/
  Program.cs              -- نقطه ورود برنامه
  MainForm.cs             -- رابط کاربری اصلی
  Models/
    AppConfig.cs          -- مدل تنظیمات (توکن، سرویس‌ها، خروجی)
    KeyFinding.cs         -- مدل کلید کشف شده و نتیجه تایید
    KeyPattern.cs         -- مدل الگوی regex هر سرویس
  Services/
    PatternProvider.cs    -- تعریف الگوهای بیش از 25 سرویس
    GitHubService.cs      -- سرویس گیت‌هاب با چرخش توکن و مدیریت محدودیت نرخ
    ScannerService.cs     -- موتور اسکن چندمرحله‌ای
    VerifierService.cs    -- تایید کلیدها با درخواست به API واقعی
```

---

## مجوز

این پروژه تحت مجوز MIT منتشر شده است. استفاده از این ابزار تنها برای مقاصد امنیتی و پژوهشی مجاز است.
