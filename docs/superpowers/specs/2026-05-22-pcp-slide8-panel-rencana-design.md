# Design Spec: PCP Slide 8 — Panel #2 Rencana Pembuatan (System Focus)

**Tanggal:** 2026-05-22
**Penulis:** Rino A P (via brainstorming session)
**Status:** Approved (awaiting written-spec review gate)
**Konteks:** PCP SMART 2026 APQ — Risalah Web.pptx slide 8, placeholder #2 "Rencana Pembuatan metode/system/fabrikasi alat"

---

## 1. Tujuan

Membuat HTML compact untuk insert sebagai PNG ke placeholder #2 slide 8 Risalah Web.pptx. Fokus konten **System** (drop "metode" prefix dan "fabrikasi alat" karena project = web digital, bukan alat fisik).

Spec ini fokus **hanya placeholder #2 (Rencana Pembuatan)**. Placeholder #1 (Gambar Desain) sudah ship (`pcp-hcportal-3.4-slide8-v1.0`). Placeholder #3 (Standard Design) di spec terpisah.

## 2. Audience + Use Case

| Audience | Konteks |
|---|---|
| Reviewer PCP Pertamina | Verify sistem yang akan dibangun konkret + realistic |
| Engineering audit | Pahami tech stack + deployment topology |
| Management HC | Quick scan: dibangun siapa + berapa lama |

## 3. Area Slide 8 Placeholder #2 + Dimensi Target

PowerPoint box "Rencana Pembuatan" bottom-left slide 8:
- Width: **11.92 cm = 450 px** @96 DPI
- Height: **3.93 cm = 148 px** @96 DPI
- Aspect ratio: **3.03:1 landscape**

**HTML render target:**
- Body width fixed `450px`, height auto target `~148px`
- Retina export `@2x` scale → effective `900×296` PNG

## 4. Layout: 2 Strip Horizontal

```
+--------- 450 × 148 px ----------+
| STRIP 1 — Tech Stack            |  ~65px
|   6 badge horizontal            |
+---------------------------------+
| STRIP 2 — Deployment + Metode   |  ~70px
|   Flow 3 node + footer 1 line   |
+---------------------------------+
```

## 5. Konten Per Strip

### Strip 1: Tech Stack (6 badge)

Source: `HcPortal.csproj` + `Program.cs` (verified).

| Badge | Tech | Color suggest |
|---|---|---|
| 1 | `.NET 8` | red (Pertamina red) |
| 2 | `ASP.NET Core MVC` | purple |
| 3 | `EF Core 8` | blue |
| 4 | `SQL Server` | dark red |
| 5 | `SignalR` (real-time) | orange |
| 6 | `Bootstrap` (UI) | purple-dark |

Layout: flex-wrap horizontal, gap 4-6px, padding 2px 6px tiap badge, border-radius 3px, font ~9px.

Optional 7th badge `Identity + LDAP/AD` kalau muat (depends row width). Drop kalau wrap 2 baris.

### Strip 2: Deployment Flow + Metode

**Sub-strip 2a (top, ~35-40px):** Deployment flow horizontal 3 node + arrow

```
💻 Lokal  →  🛠️ Dev (10.55.3.3)  →  🚀 Prod (IIS Windows)
```

- 3 node box, gap dengan arrow `→` di tengah
- Node text: emoji + label + (env detail)
- Lokal = development workstation
- Dev = staging server 10.55.3.3
- Prod = IIS Windows production

**Sub-strip 2b (bottom, ~25-30px):** Metode 1-liner footer

```
👥 Internal Gugus PROTON  ·  ⏱️ ~1 bulan build  ·  📋 SOP DEV_WORKFLOW
```

Font lebih kecil (~8px), italic optional, color gray neutral.

## 6. Styling Spec

### Token (re-use dari `versi-p-compact.html`)

```css
:root {
  --pertamina-red: #C8102E;
  --pertamina-red-light: #fce8eb;
  --pertamina-blue: #00558C;
  --pertamina-blue-dark: #003D63;
  --pertamina-blue-light: #e6f0f7;
  --pertamina-green: #00A551;
  --pertamina-green-light: #d4f0dd;
  --pertamina-yellow: #FFC72C;
  --pertamina-yellow-light: #fff5d6;
  --pertamina-orange-light: #ffe8d1;
  --neutral-gray: #6b7280;
  --neutral-light: #d1d5db;
  --bg: #f6f7fb;

  /* Tech stack badge colors (mapped) */
  --stack-net: #C8102E;      /* .NET 8 — red */
  --stack-mvc: #6f42c1;      /* ASP.NET MVC — purple */
  --stack-ef: #00558C;       /* EF Core — blue */
  --stack-sql: #003D63;      /* SQL Server — dark blue */
  --stack-signalr: #f97316;  /* SignalR — orange */
  --stack-bs: #4c1d95;       /* Bootstrap — purple-dark */
}
```

### Komponen

| Komponen | Style |
|---|---|
| Body | `width: 450px`, padding 0, margin 0, font-family Pertamina sans |
| Card wrap | bg white, border-radius .35rem, padding 4px, box-shadow 0 1px 4px rgba(0,0,0,.06), border-top 3px solid `--pertamina-yellow` (signal "rencana/planning") |
| Strip 1 (stack) | flex-wrap, gap 4px, padding 4px 2px, border-bottom 1px dashed `--neutral-light` |
| Badge | padding 2px 6px, border 1px solid (color var), bg color-light, border-radius 3px, font-size 8.5px, white-space nowrap, color matching dark variant |
| Strip 2a (flow) | flex horizontal, gap 6px, padding 3px 2px, align center, justify center, font-size 9px |
| Flow node | bg `--pertamina-blue-light`, border 1px solid `--pertamina-blue`, padding 2px 6px, border-radius 3px, color `--pertamina-blue-dark` |
| Flow arrow | font-size 12px bold, color `--neutral-gray` |
| Strip 2b (metode footer) | text-align center, font-size 7.5px italic, color `--neutral-gray`, padding 2px 4px |

### Border-top color

Yellow (`--pertamina-yellow`) untuk signal "planning/rencana" — beda dengan placeholder #1 yang dual red+green. Audience-color consistency.

## 7. Export Workflow

Sama dengan placeholder #1: html2canvas @2x retina.

Tombol `📸 Export PNG` corner top-right (hidden saat capture).

```javascript
async function exportPNG() {
  const btn = document.getElementById('export-btn');
  btn.style.display = 'none';
  const target = document.getElementById('panel-wrap');
  const canvas = await html2canvas(target, { scale: 2, backgroundColor: '#ffffff', useCORS: true });
  btn.style.display = '';
  const link = document.createElement('a');
  link.download = 'panel-rencana-slide8.png';
  link.href = canvas.toDataURL('image/png');
  link.click();
}
```

## 8. Lokasi File + Naming

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/
├── versi-p-compact.html              (placeholder #1, SHIPPED)
├── panel-rencana-compact.html        (NEW, output spec ini)
├── README.md                          (update: add panel-rencana entry)
└── exports/                           (optional, PNG output dir)
```

## 9. Yang Di-DROP / Skip

| Yang tidak masuk | Alasan |
|---|---|
| Arsitektur 3-tier diagram | Terlalu makan space; pakai stack badges saja |
| Service layer detail (AuditLog, Grading, Notification, etc) | Over-detail untuk slide PCP |
| Auth strategy detail (LDAP/AD hybrid toggle) | Over-detail; bisa di pendukung dokumen |
| Cost estimate | Slide 7 sudah cover ("Internal development, 1 bulan") |
| Migration SOP | Over-detail; SOP di `DEV_WORKFLOW.md` |
| QuestPDF/ClosedXML/SkiaSharp libraries | Tier 2 dependency, drop dari badge |
| Title bar dalam panel | Slide template sudah punya title strip di atas box |

## 10. Verifikasi Konten vs Codebase

Semua claim factual dari spec ini harus match codebase actual:

| Claim | Source verify |
|---|---|
| .NET 8 | `HcPortal.csproj` line 4: `<TargetFramework>net8.0</TargetFramework>` ✓ |
| ASP.NET Core MVC | `Program.cs` line 13: `AddControllersWithViews()` ✓ |
| EF Core 8 | `HcPortal.csproj` line 15-16: `Microsoft.EntityFrameworkCore.{SqlServer,Sqlite} 8.0.0` ✓ |
| SQL Server | `Program.cs` line 28: `options.UseSqlServer(connectionString)` ✓ |
| SignalR | `Hubs/` folder exists (from imports `using HcPortal.Hubs`) ✓ |
| Bootstrap | `wwwroot/lib/bootstrap/` + `Views/Shared/_Layout.cshtml` ✓ |
| Dev server 10.55.3.3 | `CLAUDE.md` Dev workflow ✓ |
| IIS Windows | `HcPortal.csproj` line 8: NoWarn CA1416 + comment "App deploys to IIS Windows only" ✓ |
| Gugus PROTON | `CLAUDE.md` + slide 7 reference ✓ |
| 1 bulan build | Slide 7 reference: "Dilaksanakan melalui internal development · 1 Bulan" ✓ |

## 11. Recovery + Versioning

- File baru terpisah, no modify file lain di slide8/
- Commit message: `feat(pcp-slide8): panel rencana compact (stack + deploy + metode)`
- Tag setelah ship: `pcp-hcportal-3.4-slide8-v1.1` (increment minor)

## 12. Test + Verify Checklist

- [ ] HTML render bersih di Chrome (no console error)
- [ ] Body width exact 450px (DevTools verify)
- [ ] Total height ≤ 160px (close to box 148)
- [ ] Tombol Export PNG download `panel-rencana-slide8.png` dimensi ~900×~296
- [ ] PNG inserted ke slide 8 placeholder #2 fit-to-box
- [ ] 6 tech badge visible, colors distinct
- [ ] Deployment flow 3 node + 2 arrow readable
- [ ] Metode footer 1-liner visible
- [ ] Border-top yellow signal "planning"
- [ ] Font readable saat slide projected

## 13. Out of Scope

- ❌ Spec placeholder #3 (Standard Design) — separate spec
- ❌ Modify `versi-p-compact.html` placeholder #1
- ❌ Print stylesheet
- ❌ Responsive breakpoints (fixed 450px)
- ❌ Architecture deep-dive diagram
- ❌ Service-by-service breakdown
- ❌ Promotion pipeline automation (manual per `DEV_WORKFLOW`)

## 14. Referensi

- Placeholder #1 spec: `docs/superpowers/specs/2026-05-22-pcp-slide8-versi-p-compact-design.md`
- Slide 8 image reference: `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8
- Project tech facts: `HcPortal.csproj`, `Program.cs`, `Data/ApplicationDbContext.cs`
- Dev workflow: `docs/DEV_WORKFLOW.md`
- Slide 7 (alternative analysis + cost): from `Risalah Web.pptx` extracted text
