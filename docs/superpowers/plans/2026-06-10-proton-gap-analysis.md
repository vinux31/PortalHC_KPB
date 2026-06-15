# PROTON Gap Analysis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun dokumen HTML interaktif `docs/proton-gap-analysis/index.html` yang memetakan 14 page PROTON (inventaris) lalu menyaring gap lewat 6 lensa, lengkap ringkasan eksekutif + roadmap rekomendasi.

**Architecture:** Deliverable analisa (BUKAN kode aplikasi). Tiap page di-inventaris hybrid: baca controller action + view file (read-only) → crawl runtime via Playwright (Razor dynamic wajib live) → isi accordion. Setelah 14 page terpeta, jalankan analisa 6 lensa → panel gap + matrix + ringkasan + roadmap. Output 1 file HTML offline (Bootstrap CDN-vendored pola doc sebelumnya).

**Tech Stack:** HTML + Bootstrap 5 (offline/CDN pola `docs/sertifikat-ecosystem`), Playwright MCP (crawl), Read/Grep (baca kode). Target app: `http://localhost:5277` (instance existing, jangan start kedua).

**Constraint paralel (dari spec):** Ada plan-phase sesi lain di branch sama. Read-only kode+DB. Commit CUMA `docs/proton-gap-analysis/`. No Workflow fan-out. No dotnet kedua. Login `admin@pertamina.com`/`123456` → `Admin/Impersonate` (no config edit).

---

## File Structure

- Create: `docs/proton-gap-analysis/index.html` — satu-satunya deliverable (semua section inline: ringkasan, flow, accordion 14, matrix, panel gap, roadmap)
- Create: `docs/proton-gap-analysis/data/inventory.json` — data mentah inventaris per-page (sumber tunggal; HTML render dari sini saat assembly). Memisah data dari presentasi → tiap page task nambah 1 entry, assembly task baca semua.
- Read-only rujukan kode:
  - `Controllers/CDPController.cs` (8 view actions + Export/Filter/Approve/HCReview/UploadEvidence)
  - `Controllers/ProtonDataController.cs` (Index/Override/ImportSilabus + Silabus CRUD; class `[Authorize(Roles="Admin,HC")]`)
  - `Controllers/CoachMappingController.cs` (CoachCoacheeMapping L39 + CoachWorkload L1649 + 15 AJAX action)
  - `Models/UserRoles.cs` (RolesCoachAndAbove / RolesReviewerAndAbove)
  - Views: `Views/CDP/*.cshtml` (8), `Views/ProtonData/*.cshtml` (3), `Views/Admin/CoachCoacheeMapping.cshtml` + `Views/Admin/CoachWorkload.cshtml` (2)

---

## Per-Page Inventory Procedure (dipakai Task 2–15, identik)

Tiap page task jalankan 4 langkah ini, hasil ditulis sebagai 1 objek ke `data/inventory.json` array `pages[]`:

1. **Baca kode** (Read controller action + Grep `[Authorize]`/`[HttpPost]` attribute di sekitar action; Read view `.cshtml`). Catat: route, action name, view file, `[Authorize]` gate, partial/AJAX endpoint yang dipanggil view.
2. **Crawl live** Playwright: navigate ke route (impersonate role yang sesuai), `browser_snapshot`. Konfirmasi elemen render runtime (filter, tabel + kolom, tombol, modal, export). Razor dynamic = WAJIB live (lesson Phase 354: grep+build tak cukup).
3. **Isi schema** (objek JSON):
```json
{
  "id": "<kebab-route>",
  "name": "<nama page>",
  "route": "<route>",
  "action": "<Controller.Action>",
  "view": "<path .cshtml>",
  "purpose": "<1 kalimat>",
  "flow": { "from": ["<entry>"], "to": ["<drill/keluar>"] },
  "roles": "<tier: Coachee | Coach+ | Reviewer+ | HC+Admin>",
  "filters": ["<field>"],
  "table": { "columns": ["<kol>"], "source": "<entity/query>" },
  "actions": [{ "label": "<tombol>", "endpoint": "<route AJAX>" }],
  "modals": ["<nama modal/form>"],
  "export": ["<Excel|PDF|none>"],
  "dataSource": "<entity/query utama>",
  "gaps": []
}
```
4. **Append** objek ke `data/inventory.json` → `git add docs/proton-gap-analysis/data/inventory.json && git commit -m "docs(proton-gap): inventaris <name>"`.

> `gaps: []` dibiarkan kosong saat inventaris. Diisi di Task 18 (analisa 6 lensa) setelah semua page terpeta — gap butuh perbandingan antar-page.

---

## Task 1: Scaffold output + data store

**Files:**
- Create: `docs/proton-gap-analysis/index.html`
- Create: `docs/proton-gap-analysis/data/inventory.json`

- [ ] **Step 1: Buat data store kosong**

`docs/proton-gap-analysis/data/inventory.json`:
```json
{
  "generatedAt": "2026-06-10",
  "module": "PROTON",
  "pages": [],
  "gaps": [],
  "executiveSummary": { "totalGaps": 0, "bySeverity": {}, "top5": [] },
  "roadmap": []
}
```

- [ ] **Step 2: Buat HTML skeleton offline**

`docs/proton-gap-analysis/index.html` — pakai pola `docs/sertifikat-ecosystem/index.html` (Bootstrap 5 + Bootstrap Icons via CDN, fallback offline). Section placeholder dengan anchor id:
```html
<!doctype html>
<html lang="id">
<head>
  <meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
  <title>PROTON Gap Analysis — Portal HC KPB</title>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    .sev-critical{background:#dc3545;color:#fff}.sev-high{background:#fd7e14;color:#fff}
    .sev-medium{background:#ffc107}.sev-low{background:#6c757d;color:#fff}
    .lens-badge{font-size:.72rem}
  </style>
</head>
<body class="bg-light">
  <nav class="navbar navbar-dark bg-dark"><div class="container"><span class="navbar-brand">PROTON Gap Analysis</span></div></nav>
  <main class="container my-4">
    <section id="executive-summary"><!-- Task 19 --></section>
    <section id="flow-diagram"><!-- Task 17 --></section>
    <section id="page-accordion"><!-- Task 16 --></section>
    <section id="consistency-matrix"><!-- Task 17 --></section>
    <section id="gap-panel"><!-- Task 18 --></section>
    <section id="roadmap"><!-- Task 20 --></section>
  </main>
  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
```

- [ ] **Step 3: Verify render**

Playwright `browser_navigate` ke `file:///<abs>/docs/proton-gap-analysis/index.html` → `browser_snapshot`. Expected: navbar "PROTON Gap Analysis" + 6 section kosong, no console error.

- [ ] **Step 4: Commit**

```bash
git add docs/proton-gap-analysis/index.html docs/proton-gap-analysis/data/inventory.json
git commit -m "docs(proton-gap): scaffold output HTML + data store"
```

---

## Task 1b: Prasyarat crawl — app jalan + login admin

- [ ] **Step 1: Pastikan app hidup di 5277**

Playwright `browser_navigate` ke `http://localhost:5277`. Kalau gagal/timeout → app belum jalan: cek apakah sesi paralel sudah start (`netstat -ano | findstr :5277` via PowerShell). Kalau kosong, start SATU instance: `dotnet run` (background). **Jangan** start kalau sudah ada.

- [ ] **Step 2: Login admin + verify impersonate tersedia**

Playwright: login `admin@pertamina.com` / `123456`. Navigate `Admin/Impersonate` (atau halaman user list yang punya tombol impersonate). Snapshot konfirmasi tombol impersonate ada. (No commit — langkah environment.)

---

## Task 2: Inventaris CDP/Index (hub — anchor flow)

**Files:** Read `Controllers/CDPController.cs` (action `Index`), `Views/CDP/Index.cshtml`. Append ke `data/inventory.json`.

- [ ] **Step 1–4:** Jalankan Per-Page Inventory Procedure. Role crawl: **Coachee** (impersonate). Fokus: 4 kartu hub (PlanIdp/CoachingProton/HistoriProton/Dashboard) = sumber flow tier-1. Catat link tiap kartu di `flow.to`. Commit pesan: `docs(proton-gap): inventaris CDP/Index hub`.

---

## Task 3: Inventaris CDP/PlanIdp

**Files:** Read `CDPController.cs` action `PlanIdp`, `Views/CDP/PlanIdp.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Coachee**. Catat filter silabus + Guidance* endpoint kalau ada. Commit: `docs(proton-gap): inventaris CDP/PlanIdp`.

---

## Task 4: Inventaris CDP/CoachingProton (core)

**Files:** Read `CDPController.cs` action `CoachingProton`, `Views/CDP/CoachingProton.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role crawl 2x: **Coachee** lalu **Coach** (impersonate beda) — page core, fitur beda per role (UploadEvidence/SubmitEvidence Coach+; Approve Reviewer+). Catat perbedaan di `roles` + `actions`. Commit: `docs(proton-gap): inventaris CDP/CoachingProton`.

---

## Task 5: Inventaris CDP/HistoriProton

**Files:** Read `CDPController.cs` action `HistoriProton`, `Views/CDP/HistoriProton.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Coachee** + cek **Reviewer+** (ExportHistori gated `RolesReviewerAndAbove`). Catat export. Commit: `docs(proton-gap): inventaris CDP/HistoriProton`.

---

## Task 6: Inventaris CDP/Dashboard

**Files:** Read `CDPController.cs` action `Dashboard`, `Views/CDP/Dashboard.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Coachee**. Catat ada/tidaknya export+filter (bandingkan vs HistoriProton untuk lensa konsistensi nanti). Commit: `docs(proton-gap): inventaris CDP/Dashboard`.

---

## Task 7: Inventaris CDP/Deliverable/{id}

**Files:** Read `CDPController.cs` action `Deliverable`, `Views/CDP/Deliverable.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Coachee** + **Coach** (UploadEvidence). Drill dari Coaching/Dashboard — catat `flow.from`. Cek tombol balik (lensa flow-buntu). Commit: `docs(proton-gap): inventaris CDP/Deliverable`.

---

## Task 8: Inventaris CDP/HistoriProtonDetail/{userId}

**Files:** Read `CDPController.cs` action `HistoriProtonDetail`, `Views/CDP/HistoriProtonDetail.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Reviewer+** (lihat detail orang lain). Drill dari HistoriProton. Cek tombol balik. Commit: `docs(proton-gap): inventaris CDP/HistoriProtonDetail`.

---

## Task 9: Inventaris CDP/EditCoachingSession/{id}

**Files:** Read `CDPController.cs` action `EditCoachingSession`, `Views/CDP/EditCoachingSession.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Coach+** (gated `RolesCoachAndAbove`). Catat form field + simpan endpoint. Commit: `docs(proton-gap): inventaris CDP/EditCoachingSession`.

---

## Task 10: Inventaris CDP/CertificationManagement

**Files:** Read `CDPController.cs` action `CertificationManagement`, `Views/CDP/CertificationManagement.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **HC+Admin**. Dicapai dari Admin/Home (BUKAN hub CDP) — catat `flow.from` = Admin dashboard. Commit: `docs(proton-gap): inventaris CDP/CertificationManagement`.

---

## Task 11: Inventaris ProtonData/Index (Master Silabus)

**Files:** Read `ProtonDataController.cs` action `Index`, `Views/ProtonData/Index.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Admin+HC** (class-level `[Authorize(Roles="Admin,HC")]`). Catat CRUD silabus action (SilabusSave/Delete/Deactivate) + filter Silabus/Guidance. **Catatan lensa data-integrity:** cek Status bolong / filter mati (known issue Dev = data org belum normalisasi, lihat memory). Commit: `docs(proton-gap): inventaris ProtonData/Index`.

---

## Task 12: Inventaris ProtonData/Override

**Files:** Read `ProtonDataController.cs` action `Override`, `Views/ProtonData/Override.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Admin+HC**. Catat fungsi override + endpoint. Commit: `docs(proton-gap): inventaris ProtonData/Override`.

---

## Task 13: Inventaris ProtonData/ImportSilabus

**Files:** Read `ProtonDataController.cs` action `ImportSilabus`, `Views/ProtonData/ImportSilabus.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Admin+HC**. Catat upload form + template Excel + validasi. Commit: `docs(proton-gap): inventaris ProtonData/ImportSilabus`.

---

## Task 14: Inventaris CoachCoacheeMapping

**Files:** Read `CoachMappingController.cs` action `CoachCoacheeMapping` (L39), `Views/Admin/CoachCoacheeMapping.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Admin**. Catat 15 AJAX action (Assign/Edit/Deactivate/Reactivate/DeletePreview/Delete/Export/Import + ProtonTrackAssignment). Overlap audit Phase 356 — link `[[project_356_coach_coachee_assign_audit]]` di gap nanti. Commit: `docs(proton-gap): inventaris CoachCoacheeMapping`.

---

## Task 15: Inventaris CoachWorkload

**Files:** Read `CoachMappingController.cs` action `CoachWorkload` (L1649), `Views/Admin/CoachWorkload.cshtml`.

- [ ] **Step 1–4:** Per-Page Procedure. Role: **Admin**. Catat filter section + SetWorkloadThreshold + ExportCoachWorkload. Commit: `docs(proton-gap): inventaris CoachWorkload`.

---

## Task 16: Render accordion 14 page dari inventory.json

**Files:** Modify `docs/proton-gap-analysis/index.html` (section `#page-accordion`).

- [ ] **Step 1: Tulis accordion**

Baca `data/inventory.json` → render `<div class="accordion">` 14 item. Tiap item header = nama+route+badge role; body = purpose, flow from→to, tabel elemen in-page (filter/tabel/aksi/modal/export), data source. Susun urut: 8 CDP → 1 cert → 3 ProtonData → 2 CoachMapping.

- [ ] **Step 2: Verify**

Playwright navigate `file://.../index.html` → snapshot. Expected: 14 accordion item, semua expand/collapse jalan, no console error.

- [ ] **Step 3: Commit**

```bash
git add docs/proton-gap-analysis/index.html
git commit -m "docs(proton-gap): render accordion 14 page"
```

---

## Task 17: Render flow diagram + matrix konsistensi

**Files:** Modify `index.html` (section `#flow-diagram`, `#consistency-matrix`).

- [ ] **Step 1: Flow diagram 3-tier**

Dari `flow.from`/`flow.to` tiap page → diagram HTML/CSS (atau Mermaid inline kalau pola doc lain pakai): tier-0 menu CDP → tier-1 hub 4 kartu → tier-2 drill; branch admin (Cert + ProtonData) terpisah; branch setup (CoachMapping). Tandai page tanpa entry-point (calon gap flow-buntu) dengan warna beda.

- [ ] **Step 2: Matrix konsistensi**

`<table>`: baris = 14 page, kolom = `Filter | Tabel | Export | Modal | Role`. Sel = ✓/✗ dari inventory. Spot kosong = kandidat gap konsistensi.

- [ ] **Step 3: Verify + commit**

Playwright snapshot konfirmasi diagram + tabel render. `git add index.html && git commit -m "docs(proton-gap): flow diagram + matrix konsistensi"`.

---

## Task 18: Analisa 6 lensa → isi gaps[]

**Files:** Modify `data/inventory.json` (array `gaps[]` + tiap `pages[].gaps`), lalu render `#gap-panel` di `index.html`.

- [ ] **Step 1: Jalankan 6 lensa lintas-page**

Untuk tiap lensa, bandingkan inventory antar-page, hasilkan objek gap:
```json
{ "id":"G-01", "lens":"konsistensi", "severity":"high",
  "pages":["CDP/Dashboard","CDP/HistoriProton"],
  "title":"<ringkas>", "detail":"<bukti dari inventory/crawl>" }
```
Lensa:
1. **Konsistensi** — page sejenis beda fitur (mis. Dashboard tanpa export tapi HistoriProton ada).
2. **Flow buntu** — page tanpa entry-point / drill tanpa tombol balik (dari Task 17).
3. **Role mismatch** — fitur kebuka role salah / hilang utk role (silang `roles` vs `actions`).
4. **Dead/half feature** — endpoint di controller tapi tak ke-link view, atau handler kosong (silang action controller vs `actions` view).
5. **Data integrity** — field/kolom kosong, enum status bolong, query rusak data ref (cek temuan crawl ProtonData khususnya).
6. **UX/usability** — langkah berlebih, konfirmasi hilang sebelum aksi destruktif (mis. Delete tanpa modal konfirmasi), feedback gagal absen, responsive.

Tiap gap kasih severity Critical/High/Medium/Low.

- [ ] **Step 2: Render panel gap**

`#gap-panel`: kartu per gap, badge severity (class `.sev-*`) + badge lensa (`.lens-badge`), list page terdampak, detail bukti. Kelompokkan per severity (Critical dulu).

- [ ] **Step 3: Verify + commit**

Playwright snapshot konfirmasi panel render + badge warna benar. `git add docs/proton-gap-analysis/ && git commit -m "docs(proton-gap): analisa 6 lensa + panel gap"`.

---

## Task 19: Ringkasan eksekutif

**Files:** Modify `data/inventory.json` (`executiveSummary`), render `#executive-summary`.

- [ ] **Step 1: Hitung + tulis**

Dari `gaps[]`: total gap, breakdown per severity (count), top-5 gap kritis (sort severity Critical>High>Medium>Low, ambil 5). Tulis ke `executiveSummary`.

- [ ] **Step 2: Render**

`#executive-summary`: kartu angka (total + per severity) + list top-5 (judul + page + badge). Letak paling atas (buat HC pimpinan).

- [ ] **Step 3: Verify + commit**

Snapshot konfirmasi angka konsisten dgn panel gap. Commit: `docs(proton-gap): ringkasan eksekutif`.

---

## Task 20: Roadmap rekomendasi

**Files:** Modify `data/inventory.json` (`roadmap[]`), render `#roadmap`.

- [ ] **Step 1: Tiap gap → rekomendasi**

```json
{ "gapId":"G-01", "fix":"<usulan ringkas>", "effort":"S|M|L", "priority":1 }
```
Effort S(<½ hari)/M(½–2 hari)/L(>2 hari). Priority = urut severity × (1/effort) kasaran. Kelompokkan 3 bucket: Quick win (S, high sev) / Terjadwal (M) / Besar (L).

- [ ] **Step 2: Render**

`#roadmap`: tabel/kartu per bucket — gap, fix, effort, prioritas. Link balik ke gap id.

- [ ] **Step 3: Verify + commit**

Snapshot konfirmasi tiap gap punya rekomendasi. Commit: `docs(proton-gap): roadmap rekomendasi`.

---

## Task 21: Final review + UAT browser

- [ ] **Step 1: Full render check**

Playwright navigate `file://.../index.html` → snapshot penuh. Konfirmasi 6 section terisi: ringkasan → flow → accordion 14 → matrix → panel gap → roadmap. No console error. Cek 1 accordion expand, 1 link flow→accordion (kalau ada anchor).

- [ ] **Step 2: Konsistensi data**

Verify: jumlah top-5 ≤ total gap; setiap gap punya entry roadmap; setiap page punya accordion. Perbaiki mismatch inline.

- [ ] **Step 3: Human UAT**

Tampilkan ke user (buka di browser). Tunggu approve.

- [ ] **Step 4: Commit final**

```bash
git add docs/proton-gap-analysis/
git commit -m "docs(proton-gap): final review + UAT pass"
```

---

## Catatan eksekusi

- **Restore state Playwright:** kalau impersonate, kembalikan ke admin / logout di akhir (jangan tinggalkan session impersonate buat sesi paralel).
- **Severity & lensa** subjektif — saat ragu, turunkan severity + tulis alasan di `detail` (bukti > klaim).
- **Phase 356 overlap:** gap CoachCoacheeMapping yang sudah dialamatkan audit AF-1..7 → tandai "sudah teridentifikasi Phase 356" di detail, jangan dobel-hitung sebagai temuan baru.
