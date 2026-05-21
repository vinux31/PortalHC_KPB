# Panduan Operasional HC — Portal HC KPB — Design Spec

**Date**: 2026-05-21
**Author**: Rino + Claude (brainstorming session)
**Status**: Approved (pending user review of spec doc)

---

## 1. Latar Belakang

File sosialisasi existing `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (22 slide, v3) berfungsi sebagai **overview konseptual** untuk audience luas (semua role). Setelah gap analysis terhadap scope aplikasi aktual (15 Controller + 90+ View + 37 task GuideContentProvider + 34 FAQ), teridentifikasi ~30 fitur teknis operasional yang belum tercakup, terutama di area Admin Panel, Kelola Data, Analytics, Notification, dan Reviewer Chain.

Untuk memenuhi kebutuhan **detail operasional harian tim HC**, dibuat dokumen panduan terpisah dari file sosialisasi. File ini fokus pada satu role (HC, L2) supaya konten padat dan actionable tanpa overlap multi-role.

## 2. Tujuan

- Menyediakan **panduan operasional komprehensif** untuk tim HC (role L2) yang mencakup seluruh fitur portal yang menjadi tanggung jawab HC.
- Format **long-scroll panduan** yang bisa dibaca mandiri (reference doc) maupun di-print PDF.
- **Self-contained**: tidak butuh loncat ke dokumen lain untuk informasi inti.
- Style **konsisten dengan in-app guide PDF** existing supaya familiar untuk maintainer dan pembaca.

## 3. Non-Tujuan

- **BUKAN** sosialisasi general untuk semua role (sudah dicover `Sosialisasi-Aplikasi-PortalHC-KPB.html`).
- **BUKAN** panduan untuk role Admin (L1), Manager (L3), Section Head (L4), Sr Supervisor (L4), Coach (L5), Coachee (L6).
- **BUKAN** dokumentasi developer/IT (Active Directory config, API, deploy — sudah ada doc sendiri).
- **TIDAK** menyertakan checklist rutin (Daily/Weekly/Monthly) — file ini reference doc, bukan operating manual.
- **TIDAK** mengubah file sosialisasi overview existing.
- **TIDAK** di-register sebagai in-app PDF link di `GuideContentProvider.Pdfs` (file disimpan di `docs/`, di luar `wwwroot/`).

## 4. Audience

Tim Human Capital (HC) — role L2 di Portal HC KPB. Authority full untuk pengelolaan SDM, coaching program, assessment, kelola data Proton, dan operational admin panel.

## 5. File Output

- **Path**: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- **Title**: "Panduan Operasional HC — Portal HC KPB"
- **Subtitle**: "Panduan harian untuk tim Human Capital (Role L2 — HC)"
- **Format**: Standalone HTML, self-contained (no external CSS/JS deps), print-ready A4
- **Estimasi**: 2000-2500 baris HTML, ~40-45 task

## 6. Style & Visual

Reuse template dari `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html`:

- Primary color: `#1565c0` (biru)
- Accent color: `#2e7d32` (hijau)
- Step cards dengan numbered circle
- Callout boxes: `.info` (biru) / `.warn` (oranye) / `.success` (hijau)
- Role tag system: `role-hc` (biru) digunakan di cover + tiap chapter header
- Badge system untuk klasifikasi (blue/green/red/orange/purple/gray)
- Print-ready `@media print` dengan page-break control
- Action bar (tombol print) sticky di kanan atas
- Sticky TOC sidebar di kiri (desktop) untuk navigasi cepat antar bab

## 7. Struktur Konten

### Cover
- Title + Subtitle
- Role badge `role-hc`
- Versi + tanggal
- Audience statement

### TOC (Table of Contents)
- Hierarchical list dengan link anchor ke tiap section
- Sticky di sidebar kiri saat scroll (desktop only)

### Bab 1 — Pengenalan
- 1.1 Role HC — Authority & Scope (L2)
- 1.2 Cara Akses (URL Dev `http://10.55.3.3/KPB-PortalHC` & Prod)
- 1.3 Login & First-time Setup
- 1.4 Profile & Settings (edit profil, ganti password, lihat role badge)

### Bab 2 — CMP (Competency Management Platform)
- 2.1 Library KKJ
- 2.2 CPDP Mapping
- 2.3 Training Records (view + monitoring)
- 2.4 Records Team (oversight all section)
- 2.5 Analytics Dashboard + Export Excel
- 2.6 Pre/Post Test setup & monitor (Gain Score)
- 2.7 Budget Training (Index / Create / Import Excel)
- 2.8 Certificate management

### Bab 3 — CDP (Competency Development Platform)
- 3.1 Plan IDP review
- 3.2 Deliverable monitoring all-coachee
- 3.3 Coaching Proton Dashboard
- 3.4 Histori Proton + Export Excel/PDF
- 3.5 Bottleneck Report
- 3.6 Coach Workload Dashboard
- 3.7 Certification Management (renewal lifecycle)
- 3.8 Reviewer Chain — posisi HC sebagai final reviewer (3rd-level)

### Bab 4 — Kelola Data Proton
- 4.1 Silabus Proton (CRUD + Import/Export Excel)
- 4.2 Guidance Files upload + history
- 4.3 Override Data Pekerja (Proton sync fallback)

### Bab 5 — Admin Panel (HC Operational Scope)
- 5.1 Kelola Pekerja (CRUD + Import/Export Excel + role update)
- 5.2 Kelola Units / Bagian
- 5.3 Kelola Categories
- 5.4 Bank Soal — Manage Packages
- 5.5 Bank Soal — Import Soal via Excel template
- 5.6 Create Jadwal Assessment
- 5.7 Edit Assessment + Add/Edit Manual Assessment
- 5.8 Assessment Monitoring real-time + Force-Close
- 5.9 Coach-Coachee Mapping (assign / edit / deactivate)
- 5.10 Renewal Certificate Management
- 5.11 Add/Edit/Import Training Record manual
- 5.12 KKJ Matrix Files (upload + history)
- 5.13 CPDP Files (upload + history)
- 5.14 Audit Log — investigation playbook
- 5.15 Maintenance Mode (HC trigger when needed)
- 5.16 Impersonate User (debugging)

### Bab 6 — Notifikasi & Workflow
- 6.1 Bell icon — daftar event yang trigger notif HC
- 6.2 Approval Chain awareness (HC = final reviewer)
- 6.3 Auto-notifications matrix (event → recipient → trigger)

### Lampiran A — Glossary
Istilah yang sering muncul: KKJ, CPDP, IDP, Proton, OJT, IHT, OTS, HSSE, Licencor, Gain Score, Deliverable, Evidence, Reviewer Chain, Force-Close, Override, Renewal, Bottleneck.

### Lampiran B — Troubleshooting (HC perspective)
- User lupa password (HC reset path)
- User akses menu hilang (cek role assignment)
- Upload file gagal (size/format check)
- Sertifikat tidak muncul (passing grade + status check)
- Notifikasi tidak masuk
- Reviewer chain stuck (eskalasi)
- Browser kompatibilitas
- Impersonate banner tidak hilang

### Lampiran C — URL Cheatsheet
Tabel 3 kolom: `Menu → URL → Tujuan singkat` untuk semua route yang sering dipakai HC.

### Footer
Versi + tanggal + maintainer + link ke source.

## 8. Anatomi Task

Setiap task ikut struktur hybrid (text step + breadcrumb + URL + callout):

```html
<article class="task" id="hc-bank-soal-import">
  <h3>5.5 Import Soal via Excel Template</h3>

  <div class="breadcrumb">
    Navbar → Admin Panel → Kelola Paket Soal → [Pilih Paket] → Import
    <code class="url">/Admin/ImportPackageQuestions</code>
  </div>

  <div class="step">
    <div class="step-header">
      <span class="step-num">1</span>
      <span class="step-title">Download Template Excel</span>
    </div>
    <div class="step-body">
      Klik tombol <b>Download Template</b> di pojok kanan atas...
    </div>
  </div>

  <!-- step 2, 3, ... -->

  <div class="info">
    <div class="info-title">💡 Tip</div>
    Template Excel berisi sheet validation untuk format kunci jawaban...
  </div>

  <div class="warn">
    <div class="warn-title">⚠️ Pitfall</div>
    Kunci jawaban kolom F harus huruf kapital (A/B/C/D/E)...
  </div>
</article>
```

Wajib per task: breadcrumb + URL + 2-5 step + 0-2 callout.

## 9. Sumber Konten (Strategi)

**Strategi C** (sesuai diskusi): `GuideContentProvider.cs` sebagai outline base + audit per task ke actual `Controllers/*.cs` & `Views/**/*.cshtml` untuk koreksi step yang outdated atau missing detail.

Untuk fitur yang **belum ada di GuideContentProvider** (Budget Training, Records Team detail, Pre/Post Test setup, Audit Log investigation playbook, Maintenance Mode scope, Impersonate workflow, dll), tulis fresh berdasarkan analisis Controller+View.

## 10. Workflow Build

1. **Audit step**: spawn investigator agent untuk baca Controller+View per task, output catatan koreksi terhadap GuideContent + draft step untuk task baru.
2. **Compose HTML**: tulis file long-scroll mengikuti template style `Panduan-Penggunaan-Website-HC-Portal-KPB.html` + tambah sticky TOC sidebar.
3. **Verifikasi URL**: cek tiap URL `/Controller/Action` resolve ke actual route di codebase (Grep route attributes / action methods).
4. **Print test**: render PDF via browser print preview, fix page-break artifacts (avoid orphan step di akhir halaman, chapter header tidak terpisah dari konten).
5. **Internal review**: re-read full file, cek konsistensi terminologi, dead-link anchor.
6. **Commit + tag**: 1 commit untuk file panduan. Tag `panduan-operasional-hc-v1.0` setelah verify.

## 11. Acceptance Criteria

- File `docs/Panduan-Operasional-HC-PortalHC-KPB.html` exist, valid HTML5, no broken anchor.
- Semua 40-45 task ter-cover dengan template lengkap (breadcrumb + URL + step + callout opsional).
- Semua URL di breadcrumb resolve ke actual route di codebase.
- Print preview di browser menghasilkan PDF rapi tanpa page-break artifact yang mengganggu.
- TOC sticky berfungsi di desktop, regular flow di mobile.
- Style konsisten dengan `Panduan-Penggunaan-Website-HC-Portal-KPB.html` (primary `#1565c0`, step card, callout boxes).
- Role badge `role-hc` muncul di cover + tiap chapter header.

## 12. Out of Scope (Future)

- Panduan operasional terpisah untuk role Admin (L1), Manager (L3), Section Head/Sr Supervisor (L4), Coach (L5), Coachee (L6) — bisa dibuat kalau dibutuhkan, tapi tidak di scope ini.
- Translation ke Bahasa Inggris.
- Embed video tutorial / screenshot — versi v1 text-only, screenshot bisa ditambah di v2 kalau perlu.
- In-app integration (register di `GuideContentProvider.Pdfs`) — bisa dilakukan terpisah kalau diputuskan file harus dipindah ke `wwwroot/documents/guides/`.

## 13. Tradeoff & Decisions Log

| Decision | Pilihan | Alasan |
|----------|---------|--------|
| Split file overview vs operasional | SPLIT (file baru) | Audience beda, durasi rapat beda, file overview existing sudah polished |
| Format | Long-scroll panduan (B) | Pembaca mandiri / reference doc; bukan presentasi |
| Target role | HC only (revisi dari L1-L4) | Padat & actionable; multi-role akan diulang per file di future kalau dibutuhkan |
| Struktur | Per-bab modul (revisi dari per-role) | Karena single role, lebih natural per-modul |
| Overlap handling | N/A | Single role, no overlap |
| Format step | Hybrid (D) — text + breadcrumb + URL + callout | Cocok untuk power user + casual reader |
| Sumber konten | GuideContentProvider + audit View/Controller (C) | Akurat ke actual code |
| Style | Match Panduan-* existing (B) | Konsisten dengan in-app guide PDF |
| Lokasi file | `docs/` (bukan `wwwroot/`) | Project documentation, bukan in-app distribution |
| Bab Checklist rutin | Drop | Reference doc, bukan operating manual |
