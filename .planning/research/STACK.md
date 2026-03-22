# Technology Stack Analysis

**Project:** Portal HC KPB — Assessment & Training Management Gap Analysis
**Researched:** 2026-03-21
**Confidence:** HIGH — berdasarkan direct csproj inspection + runtime verification

---

## Stack yang Sudah Ada (JANGAN GANTI)

| Technology | Version | Role | Status |
|------------|---------|------|--------|
| ASP.NET Core MVC | net8.0 | Web framework, controllers, Razor views | SOLID — tidak ada alasan ganti |
| Entity Framework Core | 8.0.0 | ORM, migrations, LINQ queries | SOLID |
| SQLite | via EF | Database (development + production) | ADEQUATE untuk skala ini |
| SignalR | ASP.NET Core built-in | Real-time exam monitoring | SOLID |
| QuestPDF | 2026.2.2 | PDF certificate generation | SOLID, Community license aktif |
| ClosedXML | 0.105.0 | Excel import/export | SOLID |
| Bootstrap 5 | CDN | UI framework | SOLID |
| jQuery | CDN | DOM manipulation, AJAX | SOLID |
| ASP.NET Core Identity | built-in | Authentication, roles | SOLID |

---

## Tambahan yang Dibutuhkan untuk Gap Features

### Untuk Analytics Dashboard

| Technology | How to Add | Purpose | Rationale |
|------------|-----------|---------|-----------|
| Chart.js 4.x | CDN script tag | Bar charts, histograms, doughnut charts | Zero npm install; 60KB; sudah industry standard; lebih ringan dari ApexCharts |

```html
<!-- Tambah di layout atau view yang butuh chart -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
```

Tidak butuh npm, tidak butuh build pipeline. Cukup satu CDN tag.

---

### Untuk Email Notification (Sertifikat Expired)

| Technology | How to Add | Purpose | Rationale |
|------------|-----------|---------|-----------|
| `MailKit` NuGet | `dotnet add package MailKit` | SMTP email sending | Industry standard untuk .NET email; lebih reliable dari built-in SmtpClient yang deprecated |

Alternatif: ASP.NET Core `IEmailSender` dengan implementasi custom via `MailKit`. Register di Program.cs.

```csharp
// Program.cs
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<CertificateExpiryReminderService>();
```

**CATATAN PENTING:** Cek dulu apakah production environment Pertamina KPB punya:
- SMTP relay (Exchange? SendGrid? Office 365?)
- IP whitelist requirement untuk SMTP

Cek `appsettings.Production.json` untuk konfigurasi email yang sudah ada.

---

### Untuk Question Bank

Tidak butuh package baru. Cukup:
- Model baru: `QuestionBank`, `QuestionBankItem`, `QuestionBankOption`
- EF migration
- Admin CRUD (copy ManageWorkers pattern)

---

### Untuk Training Compliance Matrix

Tidak butuh package baru. Cukup:
- Model baru: `RequiredTraining`
- EF migration
- Admin CRUD + compliance gap view

---

## Alternatif yang Dipertimbangkan (dan Alasan Tidak Dipilih)

| Kategori | Recommended | Alternatif | Kenapa Tidak |
|----------|-------------|------------|--------------|
| Charts | Chart.js (CDN) | ApexCharts | ApexCharts lebih besar (600KB+); Chart.js cukup untuk bar/histogram/doughnut |
| Charts | Chart.js (CDN) | D3.js | D3 terlalu low-level untuk kebutuhan chart standar; learning curve tinggi |
| Email | MailKit | System.Net.Mail | System.Net.Mail deprecated di .NET modern; MailKit adalah pengganti resmi |
| Email | MailKit | FluentEmail | FluentEmail menambah abstraksi yang tidak dibutuhkan; MailKit langsung cukup |
| Background Job | ASP.NET Core IHostedService | Hangfire | Hangfire butuh DB persistence tambahan; IHostedService built-in ASP.NET Core sudah cukup untuk daily email job yang simpel |
| Question Search | Full-text search SQL | Elasticsearch | Elasticsearch over-engineering untuk question bank internal; SQLite LIKE query cukup |

---

## Tidak Direkomendasikan (Jangan Ditambahkan)

| Jangan Tambah | Kenapa |
|---------------|--------|
| React/Vue frontend | Overkill; existing jQuery + Razor sudah works; tambah build complexity |
| Redis cache | Volume data terlalu kecil untuk cache layer; EF query sudah cukup cepat |
| Serilog/structured logging | Nice to have tapi bukan gap operasional; default ASP.NET logging sudah ada |
| MediatR / CQRS | Over-engineering untuk skala ini; direct controller → EF pattern sudah proven |
| AutoMapper | Tidak sesuai style codebase; explicit mapping lebih readable untuk maintenance |

---

## Konfigurasi yang Perlu Dicek Sebelum Email Implementation

```json
// Tambahkan ke appsettings.json
{
  "EmailSettings": {
    "SmtpHost": "smtp.pertamina.com",  // sesuaikan
    "SmtpPort": 587,
    "UseSsl": true,
    "FromAddress": "hc-portal@kpb.pertamina.com",
    "FromName": "Portal HC KPB",
    "Username": "",  // dari secret
    "Password": ""   // dari secret
  },
  "NotificationSettings": {
    "ExpiryWarningDays": [90, 30, 7],
    "EnableEmailNotifications": false  // toggle untuk testing
  }
}
```

---

## Sources

- `HcPortal.csproj` — confirmed installed packages dan versions
- `Program.cs` — QuestPDF Community license, services registration pattern
- Chart.js docs: https://www.chartjs.org/ — CDN distribution confirmed
- MailKit GitHub: https://github.com/jstedfast/MailKit — .NET recommended SMTP library
- ASP.NET Core BackgroundService docs — IHostedService pattern untuk daily job

---
*Stack research untuk: Portal HC KPB — Assessment & Training Management Gap Analysis*
*Researched: 2026-03-21*
