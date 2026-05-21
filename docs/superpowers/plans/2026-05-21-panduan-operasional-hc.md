# Panduan Operasional HC — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce `docs/Panduan-Operasional-HC-PortalHC-KPB.html` — long-scroll, print-ready HTML reference for HC (role L2) covering all operational features they own across CMP, CDP, Kelola Data Proton, Admin Panel, and Notification subsystems.

**Architecture:** Standalone single HTML file. Styles, TOC, content all inline. No external CSS/JS deps. Template baseline copied from `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` (Pertamina blue `#1565c0` + green `#2e7d32`, step cards, callouts, role-tag system, print-ready `@media print`). Adds sticky TOC sidebar for desktop navigation. Content sourced from `Services/GuideContentProvider.cs` as outline + audit of `Controllers/*.cs` and `Views/**/*.cshtml` per task.

**Tech Stack:** HTML5, inline CSS, vanilla JS (sticky TOC + print button). No build step. Verified via browser print preview.

**Spec:** `docs/superpowers/specs/2026-05-21-panduan-operasional-hc-design.md`

---

## File Structure

**Created:**
- `docs/Panduan-Operasional-HC-PortalHC-KPB.html` — single output file (~2000-2500 lines HTML)

**Modified:** none.

**Read for content audit (per task):**
- `Services/GuideContentProvider.cs` — outline + existing tasks (already loaded into context during brainstorming; reuse facts)
- `Controllers/{Cmp,Cdp,Admin,AssessmentAdmin,CoachMapping,DocumentAdmin,Notification,ProtonData,Renewal,TrainingAdmin,Organization,Worker,Account,Home}Controller.cs` — actual routes + action methods
- `Views/{CMP,CDP,Admin,Account,Home,ProtonData}/*.cshtml` — actual UI labels, button names, field names
- `Views/Shared/_Layout.cshtml` — navbar menu structure
- `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` — style template baseline

---

## Conventions

**Task ID format:** `bab-section-name` lowercase kebab-case (e.g., `cmp-library-kkj`, `admin-bank-soal-import`).

**Anatomi task article** (use exactly this skeleton, fill in content):

```html
<article class="task" id="bab-section-name">
  <h3>X.Y Judul Task (verb + object)</h3>
  <div class="breadcrumb">
    Navbar → Menu → Submenu → [Action]
    <code class="url">/Controller/Action</code>
  </div>
  <div class="step">
    <div class="step-header">
      <span class="step-num">1</span>
      <span class="step-title">Step verb phrase</span>
    </div>
    <div class="step-body">Detail langkah dengan <b>nama menu/tombol</b> di-bold.</div>
  </div>
  <!-- 2-5 step total -->
  <div class="info">
    <div class="info-title">💡 Tip</div>
    Konten tip.
  </div>
  <div class="warn">
    <div class="warn-title">⚠️ Pitfall</div>
    Konten warning.
  </div>
</article>
```

**Per-task rule:** breadcrumb + URL (one `<code class="url">`) + 2-5 step + 0-2 callout. No task without URL. No task without breadcrumb.

**Bab header skeleton** (use untuk tiap Bab):

```html
<!-- ============================================================ -->
<!-- BAB X: NAMA BAB -->
<!-- ============================================================ -->
<div class="chapter page-break" id="babX">
  <h2>X. Nama Bab</h2>
  <p>Sub-deskripsi singkat 1 kalimat.</p>
</div>
```

**Commit message format:**
```
docs(panduan-hc): <bab/section> — <one-line summary>
```

---

## Task 1: Setup file + style scaffold + cover + TOC skeleton

**Files:**
- Create: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` lines 1-130 (head + style)

- [ ] **Step 1: Read style baseline**

Open `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html`. Note CSS classes: `.cover`, `.toc`, `.chapter`, `.step`, `.info`, `.warn`, `.success`, `.role-tag.role-hc`, `.badge`, `.flow`, `.action-bar`, `@media print`.

- [ ] **Step 2: Create file with full style block**

Copy `<head>` + entire `<style>` block from baseline (lines 1-124) into new file. Change `<title>` to "Panduan Operasional HC — Portal HC KPB".

Add these new CSS blocks at end of `<style>` (before `</style>`):

```css
/* Sticky TOC sidebar (desktop) */
@media (min-width: 1100px) {
  body { padding-left: 280px; max-width: 1100px; }
  .toc-sidebar {
    position: fixed;
    top: 16px;
    left: 16px;
    width: 240px;
    max-height: calc(100vh - 32px);
    overflow-y: auto;
    background: #f8f9fa;
    border-radius: 8px;
    padding: 16px 18px;
    font-size: 9.5pt;
    box-shadow: 0 2px 12px rgba(0,0,0,0.08);
    z-index: 50;
  }
  .toc-sidebar h3 { color: #1565c0; font-size: 11pt; margin-bottom: 10px; padding-bottom: 6px; border-bottom: 2px solid #1565c0; }
  .toc-sidebar ol { padding-left: 18px; }
  .toc-sidebar li { margin-bottom: 3px; }
  .toc-sidebar a { color: #444; text-decoration: none; }
  .toc-sidebar a:hover { color: #1565c0; text-decoration: underline; }
  .toc-sidebar .active { color: #1565c0; font-weight: 700; }
}
@media (max-width: 1099px) {
  .toc-sidebar { display: none; }
}
@media print {
  .toc-sidebar { display: none; }
  body { padding-left: 20mm; }
}

/* Task article */
.task { margin: 22px 0 28px; padding-bottom: 14px; border-bottom: 1px dashed #e0e0e0; }
.task h3 { display: flex; align-items: center; gap: 10px; }
.breadcrumb { background: #fafbfc; border: 1px solid #e0e0e0; border-radius: 6px; padding: 8px 14px; margin: 10px 0 14px; font-size: 10pt; color: #555; }
.breadcrumb .url { display: inline-block; margin-left: 8px; padding: 1px 8px; background: #1565c0; color: #fff; border-radius: 4px; font-family: 'Cascadia Code', Consolas, monospace; font-size: 9pt; font-weight: 600; }
```

- [ ] **Step 3: Add `<body>` open + action bar + sticky TOC sidebar + cover + main TOC**

After `</head>`, add:

```html
<body>

<div class="action-bar no-print">
  <button class="btn-print" onclick="window.print()">Cetak / Simpan PDF</button>
</div>

<aside class="toc-sidebar no-print" id="tocSidebar">
  <h3>Navigasi</h3>
  <ol>
    <li><a href="#bab1">1. Pengenalan</a></li>
    <li><a href="#bab2">2. CMP</a></li>
    <li><a href="#bab3">3. CDP</a></li>
    <li><a href="#bab4">4. Kelola Data Proton</a></li>
    <li><a href="#bab5">5. Admin Panel</a></li>
    <li><a href="#bab6">6. Notifikasi &amp; Workflow</a></li>
    <li><a href="#lampA">Lampiran A &mdash; Glossary</a></li>
    <li><a href="#lampB">Lampiran B &mdash; Troubleshooting</a></li>
    <li><a href="#lampC">Lampiran C &mdash; URL Cheatsheet</a></li>
  </ol>
</aside>

<div class="cover">
  <div class="cover-icon">&#128188;</div>
  <h1>Panduan Operasional HC<br>Portal HC KPB</h1>
  <p class="subtitle">Human Capital Portal &mdash; Kilang Pertamina Balikpapan</p>
  <p class="subtitle">Panduan harian untuk tim Human Capital (Role L2 &mdash; HC)</p>
  <div class="audience"><span class="role-tag role-hc">HC</span> Khusus Tim Human Capital</div>
  <p class="version">Versi 1.0 &bull; Mei 2026</p>
</div>

<div class="toc">
  <h2>Daftar Isi</h2>
  <ol>
    <li><a href="#bab1">Pengenalan</a>
      <ul class="toc-sub">
        <li><a href="#bab1-1">1.1 Role HC &mdash; Authority &amp; Scope</a></li>
        <li><a href="#bab1-2">1.2 Cara Akses (Dev / Prod)</a></li>
        <li><a href="#bab1-3">1.3 Login &amp; First-time Setup</a></li>
        <li><a href="#bab1-4">1.4 Profile &amp; Settings</a></li>
      </ul>
    </li>
    <li><a href="#bab2">CMP &mdash; Competency Management Platform</a>
      <ul class="toc-sub">
        <li><a href="#cmp-library-kkj">2.1 Library KKJ</a></li>
        <li><a href="#cmp-cpdp-mapping">2.2 CPDP Mapping</a></li>
        <li><a href="#cmp-training-records">2.3 Training Records</a></li>
        <li><a href="#cmp-records-team">2.4 Records Team (oversight)</a></li>
        <li><a href="#cmp-analytics-dashboard">2.5 Analytics Dashboard + Export</a></li>
        <li><a href="#cmp-pre-post-test">2.6 Pre/Post Test setup &amp; monitor</a></li>
        <li><a href="#cmp-budget-training">2.7 Budget Training</a></li>
        <li><a href="#cmp-certificate">2.8 Certificate management</a></li>
      </ul>
    </li>
    <li><a href="#bab3">CDP &mdash; Competency Development Platform</a>
      <ul class="toc-sub">
        <li><a href="#cdp-plan-idp">3.1 Plan IDP review</a></li>
        <li><a href="#cdp-deliverable-monitoring">3.2 Deliverable monitoring</a></li>
        <li><a href="#cdp-coaching-dashboard">3.3 Coaching Proton Dashboard</a></li>
        <li><a href="#cdp-histori-proton">3.4 Histori Proton + Export</a></li>
        <li><a href="#cdp-bottleneck-report">3.5 Bottleneck Report</a></li>
        <li><a href="#cdp-coach-workload">3.6 Coach Workload Dashboard</a></li>
        <li><a href="#cdp-certification-management">3.7 Certification Management</a></li>
        <li><a href="#cdp-reviewer-chain">3.8 Reviewer Chain &mdash; HC sebagai final reviewer</a></li>
      </ul>
    </li>
    <li><a href="#bab4">Kelola Data Proton</a>
      <ul class="toc-sub">
        <li><a href="#data-silabus">4.1 Silabus Proton (CRUD + Import/Export)</a></li>
        <li><a href="#data-guidance">4.2 Guidance Files upload + history</a></li>
        <li><a href="#data-override">4.3 Override Data Pekerja</a></li>
      </ul>
    </li>
    <li><a href="#bab5">Admin Panel (HC Operational Scope)</a>
      <ul class="toc-sub">
        <li><a href="#admin-kelola-pekerja">5.1 Kelola Pekerja</a></li>
        <li><a href="#admin-kelola-units">5.2 Kelola Units / Bagian</a></li>
        <li><a href="#admin-kelola-categories">5.3 Kelola Categories</a></li>
        <li><a href="#admin-bank-soal">5.4 Bank Soal &mdash; Manage Packages</a></li>
        <li><a href="#admin-import-soal">5.5 Bank Soal &mdash; Import Soal Excel</a></li>
        <li><a href="#admin-create-assessment">5.6 Create Jadwal Assessment</a></li>
        <li><a href="#admin-edit-assessment">5.7 Edit Assessment + Manual</a></li>
        <li><a href="#admin-assessment-monitoring">5.8 Assessment Monitoring + Force-Close</a></li>
        <li><a href="#admin-coach-mapping">5.9 Coach-Coachee Mapping</a></li>
        <li><a href="#admin-renewal">5.10 Renewal Certificate Management</a></li>
        <li><a href="#admin-training-records">5.11 Add/Edit/Import Training Record</a></li>
        <li><a href="#admin-kkj-files">5.12 KKJ Matrix Files</a></li>
        <li><a href="#admin-cpdp-files">5.13 CPDP Files</a></li>
        <li><a href="#admin-audit-log">5.14 Audit Log &mdash; investigation playbook</a></li>
        <li><a href="#admin-maintenance">5.15 Maintenance Mode</a></li>
        <li><a href="#admin-impersonate">5.16 Impersonate User</a></li>
      </ul>
    </li>
    <li><a href="#bab6">Notifikasi &amp; Workflow</a></li>
    <li><a href="#lampA">Lampiran A &mdash; Glossary</a></li>
    <li><a href="#lampB">Lampiran B &mdash; Troubleshooting</a></li>
    <li><a href="#lampC">Lampiran C &mdash; URL Cheatsheet</a></li>
  </ol>
</div>
```

- [ ] **Step 4: Add footer + closing tags + sticky TOC script placeholder**

Just before `</body>` (will add `</body>` next), add:

```html
<div class="footer">
  Panduan Operasional HC &mdash; Portal HC KPB v1.0 &bull; Mei 2026<br>
  Maintainer: Tim Human Capital KPB &bull; <code>docs/Panduan-Operasional-HC-PortalHC-KPB.html</code>
</div>

<script>
  // Highlight active TOC item on scroll
  (function () {
    var sidebar = document.getElementById('tocSidebar');
    if (!sidebar) return;
    var links = sidebar.querySelectorAll('a[href^="#"]');
    var sections = Array.from(links).map(function (a) {
      var el = document.getElementById(a.getAttribute('href').slice(1));
      return { link: a, el: el };
    }).filter(function (s) { return s.el; });
    function onScroll() {
      var top = window.scrollY + 100;
      var current = sections[0];
      for (var i = 0; i < sections.length; i++) {
        if (sections[i].el.offsetTop <= top) current = sections[i];
      }
      links.forEach(function (a) { a.classList.remove('active'); });
      if (current) current.link.classList.add('active');
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  })();
</script>

</body>
</html>
```

- [ ] **Step 5: Open file in browser, verify**

Open `docs/Panduan-Operasional-HC-PortalHC-KPB.html` in browser.
Expected: cover renders with HC badge, main TOC renders, sticky sidebar TOC renders on left (desktop ≥1100px), print button works (opens print preview), no console errors.

- [ ] **Step 6: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): scaffold — style + cover + TOC skeleton"
```

---

## Task 2: Bab 1 — Pengenalan

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html` (insert before footer)
- Read: `Controllers/AccountController.cs`, `Controllers/HomeController.cs`, `Views/Account/Profile.cshtml`, `Views/Account/Settings.cshtml`, `Views/Shared/_Layout.cshtml`, `Services/GuideContentProvider.cs` (lines 226-300 for Account)
- Reference: `CLAUDE.md` — Dev URL `http://10.55.3.3/KPB-PortalHC`

- [ ] **Step 1: Audit source files**

Read these files. Note:
- Login URL: `/Account/Login` (form action, fields, banner)
- Profile URL: `/Account/Profile` — what fields editable, what fields read-only
- Settings URL: `/Account/Settings` — Change Password tab + any other tab
- Logout URL: `/Account/Logout` (POST)
- Navbar items visible for HC role
- HC role badge label (cek `GuideRoleAccess.BadgeLabel`)

- [ ] **Step 2: Insert Bab 1 chapter header + sections**

Insert after the closing `</div>` of `.toc` div, before footer:

```html
<!-- ============================================================ -->
<!-- BAB 1: PENGENALAN -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab1">
  <h2>1. Pengenalan</h2>
  <p>Role HC, cara akses, login, dan pengaturan akun pribadi.</p>
</div>

<h3 id="bab1-1">1.1 Role HC — Authority &amp; Scope (L2)</h3>
<p>HC (Human Capital) adalah role <b>Level 2</b> dengan akses penuh ke pengelolaan SDM dan program coaching. Authority HC mencakup:</p>
<ul>
  <li><b>CMP</b>: monitor seluruh assessment lintas Unit/Section, Analytics Dashboard, Records Team semua bagian, kelola Pre/Post Test, Budget Training, Certificate.</li>
  <li><b>CDP</b>: review semua deliverable, Coaching Proton Dashboard global, Bottleneck Report, Coach Workload, Final Reviewer di approval chain.</li>
  <li><b>Kelola Data Proton</b>: CRUD Silabus, upload Guidance Files, Override Data Pekerja.</li>
  <li><b>Admin Panel</b>: Kelola Pekerja/Units/Categories, Bank Soal + Create Assessment, Assessment Monitoring real-time, Coach-Coachee Mapping, Renewal Certificate, Training Record, KKJ/CPDP Files, Audit Log, Maintenance Mode, Impersonate.</li>
</ul>
<div class="info">
  <div class="info-title">💡 Beda dengan Admin (L1)</div>
  Admin punya semua hak akses HC + konfigurasi sistem yang lebih sensitif (perubahan role pekerja, hapus permanen, escalation override). HC tidak.
</div>

<h3 id="bab1-2">1.2 Cara Akses (Dev / Prod)</h3>
<table>
  <thead><tr><th>Environment</th><th>URL</th><th>Tujuan</th></tr></thead>
  <tbody>
    <tr><td><span class="badge badge-green">Dev</span></td><td><code>http://10.55.3.3/KPB-PortalHC</code></td><td>Server testing internal (akses dari jaringan kantor)</td></tr>
    <tr><td><span class="badge badge-orange">Prod</span></td><td><code>(Hubungi IT untuk URL produksi)</code></td><td>Operasional harian</td></tr>
  </tbody>
</table>
<div class="warn">
  <div class="warn-title">⚠️ Server Dev = jaringan kantor</div>
  Server <code>10.55.3.3</code> hanya bisa diakses dari jaringan kantor KPB. Tidak bisa diakses dari rumah atau WiFi publik.
</div>

<!-- TASK: account-login -->
<article class="task" id="account-login">
  <h3>1.3 Login ke HC Portal</h3>
  <div class="breadcrumb">
    Browser → Halaman Login
    <code class="url">/Account/Login</code>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">1</span><span class="step-title">Buka URL Portal</span></div>
    <div class="step-body">Akses URL Dev atau Prod di browser (Chrome/Edge/Firefox versi terbaru).</div>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">2</span><span class="step-title">Masukkan Kredensial</span></div>
    <div class="step-body">Ketik <b>email</b> dan <b>password</b> yang sudah diaktifkan oleh Admin. Klik <b>Login</b>.</div>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">3</span><span class="step-title">Verifikasi Role Badge</span></div>
    <div class="step-body">Setelah login, cek navbar kanan atas. Avatar Anda akan tampil dengan badge <span class="role-tag role-hc">HC</span>. Kalau bukan HC, hubungi Admin untuk update role.</div>
  </div>
  <div class="info">
    <div class="info-title">💡 Lupa password</div>
    Belum tersedia self-service reset. Hubungi Admin / IT untuk reset password manual.
  </div>
</article>

<!-- TASK: account-profile -->
<article class="task" id="account-profile">
  <h3>1.4 Profile &amp; Settings</h3>
  <div class="breadcrumb">
    Navbar → Avatar (kanan atas) → My Profile / Settings
    <code class="url">/Account/Profile</code>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">1</span><span class="step-title">Buka My Profile</span></div>
    <div class="step-body">Klik <b>avatar</b> Anda di pojok kanan atas navbar → pilih <b>My Profile</b>.</div>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">2</span><span class="step-title">Review Data Personal</span></div>
    <div class="step-body">Cek: nama, NIP, email, jabatan, unit kerja, role. Field yang bisa di-edit ditandai dengan icon pensil.</div>
  </div>
  <div class="step">
    <div class="step-header"><span class="step-num">3</span><span class="step-title">Ganti Password</span></div>
    <div class="step-body">Klik avatar → <b>Settings</b> (<code>/Account/Settings</code>) → tab <b>Change Password</b> → input password lama + baru + konfirmasi → klik <b>Simpan</b>.</div>
  </div>
  <div class="warn">
    <div class="warn-title">⚠️ Field yang tidak bisa di-edit</div>
    NIP dan Role tidak bisa di-edit user. Hubungi Admin (Kelola Pekerja) kalau perlu update.
  </div>
</article>
```

- [ ] **Step 3: Verify in browser**

Reload `docs/Panduan-Operasional-HC-PortalHC-KPB.html`. Cek Bab 1 render, anchor link dari TOC ke `#bab1-1` / `#account-login` / `#account-profile` work.

- [ ] **Step 4: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 1 — Pengenalan (role, akses, login, profile)"
```

---

## Task 3: Bab 2 — CMP (8 task sections)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/CMPController.cs` (semua action), `Views/CMP/*.cshtml`, `Services/GuideContentProvider.cs` lines 9-87 (CMP guides)

- [ ] **Step 1: Audit CMP routes**

For each section below, identify the actual Controller action + URL + button labels by reading the corresponding View:

| Section | URL pattern | Source view |
|---------|------------|-------------|
| 2.1 Library KKJ | `/CMP/Index` (tab Library KKJ) | `Views/CMP/Index.cshtml` |
| 2.2 CPDP Mapping | `/CMP/DokumenKkj` | `Views/CMP/DokumenKkj.cshtml` |
| 2.3 Training Records | `/CMP/Records` | `Views/CMP/Records.cshtml` |
| 2.4 Records Team | `/CMP/RecordsTeam` | `Views/CMP/RecordsTeam.cshtml`, `_RecordsTeamBody.cshtml`, `RecordsWorkerDetail.cshtml` |
| 2.5 Analytics Dashboard | `/CMP/AnalyticsDashboard` | `Views/CMP/AnalyticsDashboard.cshtml` |
| 2.6 Pre/Post Test | `/CMP/Assessment` (filter Pre/Post) | `Views/CMP/Assessment.cshtml`, `Views/CMP/StartExam.cshtml`, `ExamSummary.cshtml`, `Results.cshtml` |
| 2.7 Budget Training | `/CMP/BudgetTraining`, `/CMP/BudgetTrainingCreate`, `/CMP/BudgetTrainingImport` | corresponding Views |
| 2.8 Certificate | `/CMP/Certificate` | `Views/CMP/Certificate.cshtml` |

- [ ] **Step 2: Insert Bab 2 chapter header**

Insert after Bab 1 last `</article>`:

```html
<!-- ============================================================ -->
<!-- BAB 2: CMP -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab2">
  <h2>2. CMP — Competency Management Platform</h2>
  <p>Pengelolaan kompetensi: KKJ, assessment, training records, analytics, dan sertifikat.</p>
</div>
```

- [ ] **Step 3: Compose 8 task articles**

For each section 2.1–2.8, write `<article class="task" id="cmp-...">` using anatomi template at top. Each article must include:
- `<h3>X.Y Judul</h3>`
- `<div class="breadcrumb">` with menu path + `<code class="url">/CMP/Action</code>`
- 2-5 `<div class="step">` blocks
- 0-2 callout (info/warn/success)

Use GuideContentProvider as outline, audit notes for accuracy. For Records Team (2.4), Analytics Dashboard (2.5), Budget Training (2.7), and Certificate (2.8), write fresh because GuideContent only has 1 entry each (`cmp-monitoring-records-tim`, `cmp-monitoring-manager` for analytics, no entry for Budget Training, no entry for Certificate page).

Insert all 8 articles after Bab 2 chapter header in order.

- [ ] **Step 4: Verify TOC anchors resolve**

Reload file. Click each TOC link `#cmp-library-kkj` through `#cmp-certificate` — verify each scrolls to correct article.

- [ ] **Step 5: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 2 — CMP (8 task sections)"
```

---

## Task 4: Bab 3 — CDP (8 task sections)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/CDPController.cs`, `Views/CDP/*.cshtml`, `Services/GuideContentProvider.cs` lines 88-225 (CDP guides)

- [ ] **Step 1: Audit CDP routes**

| Section | URL | Source view |
|---------|-----|-------------|
| 3.1 Plan IDP review | `/CDP/PlanIdp` | `Views/CDP/PlanIdp.cshtml` |
| 3.2 Deliverable monitoring | `/CDP/Deliverable` | `Views/CDP/Deliverable.cshtml` |
| 3.3 Coaching Proton Dashboard | `/CDP/Dashboard` | `Views/CDP/Dashboard.cshtml`, `_CoacheeDashboardPartial.cshtml` |
| 3.4 Histori Proton + Export | `/CDP/HistoriProton`, `/CDP/HistoriProtonDetail` | corresponding views |
| 3.5 Bottleneck Report | (cek tab di Dashboard atau separate route) | `Views/CDP/Dashboard.cshtml` |
| 3.6 Coach Workload | `/Admin/CoachWorkload` | `Views/Admin/CoachWorkload.cshtml` |
| 3.7 Certification Management | `/CDP/CertificationManagement` | `Views/CDP/CertificationManagement.cshtml`, `_CertificationManagementTablePartial.cshtml` |
| 3.8 Reviewer Chain | (workflow doc, no single URL) | reference `cdp-reviewer-chain` in GuideContent |

- [ ] **Step 2: Insert Bab 3 chapter header**

```html
<!-- ============================================================ -->
<!-- BAB 3: CDP -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab3">
  <h2>3. CDP — Competency Development Platform</h2>
  <p>Coaching Proton, IDP, deliverable, dan approval chain.</p>
</div>
```

- [ ] **Step 3: Compose 8 task articles** (anatomi template, breadcrumb + URL + 2-5 step + callout opsional).

For section 3.8 "Reviewer Chain", structure as conceptual workflow doc (no single URL, use `<code class="url">(workflow)</code>`). Include the 3-step chain (Sr Supervisor → Section Head → HC) and the reset-on-reject rule. Reuse content from GuideContent `cdp-reviewer-chain` (Services/GuideContentProvider.cs:184-194).

For section 3.5 "Bottleneck Report", verify whether it's a tab in `/CDP/Dashboard` or separate route by reading the view. Set URL accordingly.

- [ ] **Step 4: Verify TOC anchors**

- [ ] **Step 5: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 3 — CDP (8 task sections + reviewer chain)"
```

---

## Task 5: Bab 4 — Kelola Data Proton (3 task sections)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/ProtonDataController.cs`, `Controllers/DocumentAdminController.cs`, `Views/ProtonData/Index.cshtml`, `Views/ProtonData/Override.cshtml`, `Views/ProtonData/ImportSilabus.cshtml`, GuideContent lines 302-338

- [ ] **Step 1: Audit Kelola Data routes**

| Section | URL | View |
|---------|-----|------|
| 4.1 Silabus Proton | `/ProtonData/Index`, `/ProtonData/ImportSilabus` | corresponding |
| 4.2 Guidance Files | (cek route di DocumentAdminController) | check view |
| 4.3 Override Data Pekerja | `/ProtonData/Override` | `Views/ProtonData/Override.cshtml` |

- [ ] **Step 2: Insert Bab 4 header**

```html
<!-- ============================================================ -->
<!-- BAB 4: KELOLA DATA PROTON -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab4">
  <h2>4. Kelola Data Proton</h2>
  <p>Silabus, Guidance Files, dan Override data sync Proton.</p>
</div>
```

- [ ] **Step 3: Compose 3 task articles** dengan anatomi template lengkap.

For 4.3 Override, include warning callout — fitur sensitif, hanya untuk fix data sync yang gagal otomatis.

- [ ] **Step 4: Verify + commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 4 — Kelola Data Proton (silabus, guidance, override)"
```

---

## Task 6: Bab 5a — Admin Panel: Pekerja, Units, Categories (5.1–5.3)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/AdminController.cs` (actions ManageWorkers, ImportWorkers, CreateWorker, EditWorker, WorkerDetail, ManageOrganization, ManageCategories), corresponding Views

- [ ] **Step 1: Audit routes**

| Section | URL | View |
|---------|-----|------|
| 5.1 Kelola Pekerja | `/Admin/ManageWorkers`, `/Admin/CreateWorker`, `/Admin/EditWorker`, `/Admin/ImportWorkers`, `/Admin/WorkerDetail` | corresponding |
| 5.2 Kelola Units / Bagian | `/Admin/ManageOrganization` (via OrganizationController?) | check both AdminController + OrganizationController |
| 5.3 Kelola Categories | `/Admin/ManageCategories` | `Views/Admin/ManageCategories.cshtml` |

- [ ] **Step 2: Insert Bab 5 chapter header (one-time, not per sub-task)**

```html
<!-- ============================================================ -->
<!-- BAB 5: ADMIN PANEL -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab5">
  <h2>5. Admin Panel (HC Operational Scope)</h2>
  <p>16 menu admin: kelola pekerja, bank soal, assessment, mapping, renewal, audit, maintenance.</p>
</div>
```

- [ ] **Step 3: Compose 3 task articles** for 5.1, 5.2, 5.3.

For 5.1 Kelola Pekerja, include sub-flows in one article: CRUD + Import Excel + Export. Use multiple `<div class="step">` and add callout box for tip about template Excel download location.

- [ ] **Step 4: Verify + commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 5a — Admin Panel (Pekerja, Units, Categories)"
```

---

## Task 7: Bab 5b — Admin Panel: Bank Soal + Assessment (5.4–5.8)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/AdminController.cs` + `Controllers/AssessmentAdminController.cs` (ManagePackages, ManagePackageQuestions, ImportPackageQuestions, ManageAssessment, CreateAssessment, EditAssessment, AddManualAssessment, EditManualAssessment, AssessmentMonitoring, AssessmentMonitoringDetail), Views

- [ ] **Step 1: Audit routes** untuk 5.4 (ManagePackages), 5.5 (ImportPackageQuestions), 5.6 (CreateAssessment), 5.7 (EditAssessment, AddManualAssessment, EditManualAssessment), 5.8 (AssessmentMonitoring, AssessmentMonitoringDetail).

- [ ] **Step 2: Compose 5 task articles** with anatomi template.

For 5.5 Import Soal, include detailed warning callout about Excel template format (kolom kunci jawaban format, batas jumlah soal per file, validasi sheet).

For 5.8 Assessment Monitoring, include warning callout about Force-Close — actionnya ireversible, log masuk Audit Log, gunakan hanya untuk kasus user freeze/disconnect.

- [ ] **Step 3: Verify + commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 5b — Admin Panel (Bank Soal, Create/Edit/Monitor Assessment)"
```

---

## Task 8: Bab 5c — Admin Panel: Mapping, Renewal, Training, Files (5.9–5.13)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/CoachMappingController.cs`, `Controllers/RenewalController.cs`, `Controllers/TrainingAdminController.cs`, `Controllers/AdminController.cs` (KkjUpload, KkjMatrix, KkjFileHistory, CpdpUpload, CpdpFiles, CpdpFileHistory)

- [ ] **Step 1: Audit routes**

| Section | URL | View |
|---------|-----|------|
| 5.9 Coach-Coachee Mapping | `/Admin/CoachCoacheeMapping` (cek di CoachMappingController) | `Views/Admin/CoachCoacheeMapping.cshtml` |
| 5.10 Renewal Certificate | `/Admin/RenewalCertificate` | `Views/Admin/RenewalCertificate.cshtml`, `_RenewalGroupTablePartial.cshtml`, `_RenewalCertificateTablePartial.cshtml`, `_RenewalGroupedPartial.cshtml` |
| 5.11 Training Record | `/Admin/AddTraining`, `/Admin/EditTraining`, `/Admin/ImportTraining` | corresponding |
| 5.12 KKJ Files | `/Admin/KkjMatrix`, `/Admin/KkjUpload`, `/Admin/KkjFileHistory` | corresponding |
| 5.13 CPDP Files | `/Admin/CpdpFiles`, `/Admin/CpdpUpload`, `/Admin/CpdpFileHistory` | corresponding |

- [ ] **Step 2: Compose 5 task articles**.

For 5.10 Renewal, include `success` callout with lifecycle summary (mendekati expired → schedule renewal → notif pekerja → assessment baru → sertifikat baru terbit).

- [ ] **Step 3: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 5c — Admin Panel (Mapping, Renewal, Training, KKJ/CPDP Files)"
```

---

## Task 9: Bab 5d — Admin Panel: Audit, Maintenance, Impersonate (5.14–5.16)

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/AdminController.cs` (AuditLog, Maintenance, Impersonate), Views, `Services/AuditLogService.cs`, `Services/ImpersonationService.cs`

- [ ] **Step 1: Audit routes**

| Section | URL | View |
|---------|-----|------|
| 5.14 Audit Log | `/Admin/AuditLog` | `Views/Admin/AuditLog.cshtml` |
| 5.15 Maintenance Mode | `/Admin/Maintenance` | `Views/Admin/Maintenance.cshtml` (or `Views/Home/Maintenance.cshtml`) |
| 5.16 Impersonate | `/Admin/Impersonate` | `Views/Admin/Impersonate.cshtml`, `Views/Shared/_ImpersonationBanner.cshtml` |

- [ ] **Step 2: Compose 3 task articles**.

For 5.14 Audit Log, include "Investigation Playbook" — short numbered list di dalam article: (a) tentukan jendela waktu, (b) filter user/action, (c) cross-check timestamp, (d) drill ke detail aksi sensitif (Force-Close, Override, Delete, Role Change), (e) eskalasi kalau menemukan anomali.

For 5.15 Maintenance Mode, warn that all non-Admin users will see maintenance page. Always cek scope (All vs per-module) sebelum aktifkan.

For 5.16 Impersonate, warn: impersonate session juga ter-log di Audit Log. Selalu klik **Stop Impersonating** di banner kuning setelah selesai — banner tetap tampil sampai distop.

- [ ] **Step 3: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 5d — Admin Panel (Audit Log, Maintenance, Impersonate)"
```

---

## Task 10: Bab 6 — Notifikasi & Workflow

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- Read: `Controllers/NotificationController.cs`, `Services/NotificationService.cs`, `Services/INotificationService.cs`, `Views/Shared/Components/NotificationBell/Default.cshtml`

- [ ] **Step 1: Audit notification system**

Read NotificationService: identify all event types (`enum NotificationType` or similar) and which roles receive each. Build a 3-column matrix: Event → Trigger → Recipient.

- [ ] **Step 2: Insert Bab 6 + 3 sub-sections**

```html
<!-- ============================================================ -->
<!-- BAB 6: NOTIFIKASI & WORKFLOW -->
<!-- ============================================================ -->
<div class="chapter page-break" id="bab6">
  <h2>6. Notifikasi &amp; Workflow</h2>
  <p>Bell icon, approval chain context, dan matrix notifikasi otomatis.</p>
</div>

<h3 id="bab6-1">6.1 Bell Icon — Event yang Trigger Notif HC</h3>
<p>Bell icon di navbar kanan atas memunculkan badge merah saat ada notifikasi baru. Klik untuk buka daftar.</p>
<p>Event yang trigger notif untuk role HC:</p>
<ul>
  <li><b>Evidence Submitted</b> — coachee upload bukti deliverable (tampil setelah Sr Supervisor + Section Head approve)</li>
  <li><b>Renewal Sertifikat</b> — ada sertifikat mendekati expired</li>
  <li><b>Assessment Complete</b> — peserta menyelesaikan assessment (untuk monitoring)</li>
  <li><b>Audit Log Sensitive Action</b> — force-close, override, delete dilakukan</li>
  <!-- isi dari audit notification service -->
</ul>

<h3 id="bab6-2">6.2 Approval Chain — HC sebagai Final Reviewer</h3>
<p>HC adalah <b>reviewer ke-3 (final)</b> di approval chain deliverable Coaching Proton:</p>
<div class="diagram">Coachee Submit Evidence
        ↓
1. Sr Supervisor Review
        ↓
2. Section Head Review
        ↓
3. HC Final Review   ← Anda di sini
        ↓
   Approved / Rejected</div>
<div class="warn">
  <div class="warn-title">⚠️ Reject = reset chain</div>
  Kalau HC reject, seluruh chain (Sr Supervisor + Section Head + HC) ter-reset. Coachee harus upload evidence baru, di-review ulang dari Sr Supervisor lagi.
</div>

<h3 id="bab6-3">6.3 Auto-Notifications Matrix</h3>
<table>
  <thead><tr><th>Event</th><th>Trigger Source</th><th>Recipient (selain HC)</th></tr></thead>
  <tbody>
    <!-- isi dari NotificationService audit, minimal 8-10 row -->
    <tr><td>Evidence Submitted</td><td>Coachee upload</td><td>Sr Supervisor (chain start)</td></tr>
    <tr><td>Evidence Approved</td><td>Reviewer click Approve</td><td>Next reviewer + coachee</td></tr>
    <tr><td>Evidence Rejected</td><td>Reviewer click Reject</td><td>Coachee + previous reviewers (reset notif)</td></tr>
    <tr><td>Assessment Assigned</td><td>Admin/HC create jadwal</td><td>Peserta assigned</td></tr>
    <!-- ... lanjut dari hasil audit ... -->
  </tbody>
</table>
```

- [ ] **Step 3: Replace placeholder rows in matrix** dengan data actual dari hasil audit NotificationService.

- [ ] **Step 4: Verify + commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Bab 6 — Notifikasi & Workflow"
```

---

## Task 11: Lampiran A — Glossary

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`

- [ ] **Step 1: Insert Lampiran A header + glossary table**

Insert after Bab 6:

```html
<!-- ============================================================ -->
<!-- LAMPIRAN A: GLOSSARY -->
<!-- ============================================================ -->
<div class="chapter page-break" id="lampA">
  <h2>Lampiran A — Glossary</h2>
  <p>Istilah teknis dan singkatan yang sering muncul di Portal HC KPB.</p>
</div>

<table>
  <thead><tr><th>Istilah</th><th>Kepanjangan / Definisi</th></tr></thead>
  <tbody>
    <tr><td><b>KKJ</b></td><td>Kebutuhan Kompetensi Jabatan — dokumen standar kompetensi per posisi.</td></tr>
    <tr><td><b>CPDP</b></td><td>Competency and Proficiency Development Program — peta gap kompetensi ke program pelatihan.</td></tr>
    <tr><td><b>IDP</b></td><td>Individual Development Plan — rencana pengembangan kompetensi personal per pekerja.</td></tr>
    <tr><td><b>Proton</b></td><td>Program coaching terstruktur 3 tahun (Th 1-2 online, Th 3 offline interview).</td></tr>
    <tr><td><b>OJT</b></td><td>On the Job Training — assessment berbasis unit kerja.</td></tr>
    <tr><td><b>IHT</b></td><td>In House Training — assessment terkait pelatihan internal perusahaan.</td></tr>
    <tr><td><b>OTS</b></td><td>On The Spot — assessment langsung di lapangan.</td></tr>
    <tr><td><b>HSSE</b></td><td>Health, Safety, Security &amp; Environment — mandatory training kategori K3.</td></tr>
    <tr><td><b>Licencor</b></td><td>Training Licencor — lisensi / sertifikasi eksternal.</td></tr>
    <tr><td><b>Gain Score</b></td><td>Selisih nilai Pre-Test vs Post-Test, ukur efektivitas training.</td></tr>
    <tr><td><b>Deliverable</b></td><td>Tugas terstruktur yang harus diselesaikan coachee di program Proton.</td></tr>
    <tr><td><b>Evidence</b></td><td>Bukti penyelesaian deliverable (PDF/DOCX/XLSX/JPG/PNG, max 10MB).</td></tr>
    <tr><td><b>Reviewer Chain</b></td><td>Urutan approval 3-tahap: Sr Supervisor → Section Head → HC. Reject di salah satu = reset semua.</td></tr>
    <tr><td><b>Force-Close</b></td><td>Aksi Admin/HC untuk mengakhiri sesi assessment user yang stuck. Auto-submit progress.</td></tr>
    <tr><td><b>Override</b></td><td>Aksi Admin/HC untuk paksa value mapping data Proton yang gagal sync otomatis.</td></tr>
    <tr><td><b>Renewal</b></td><td>Perpanjangan sertifikat yang mendekati masa kadaluarsa.</td></tr>
    <tr><td><b>Bottleneck</b></td><td>Deliverable stuck (lama tidak progress) per coachee/unit.</td></tr>
  </tbody>
</table>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Lampiran A — Glossary"
```

---

## Task 12: Lampiran B — Troubleshooting

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`

- [ ] **Step 1: Insert Lampiran B header + troubleshooting cards**

Insert after Lampiran A:

```html
<!-- ============================================================ -->
<!-- LAMPIRAN B: TROUBLESHOOTING -->
<!-- ============================================================ -->
<div class="chapter page-break" id="lampB">
  <h2>Lampiran B — Troubleshooting (HC Perspective)</h2>
  <p>Masalah umum yang sering di-eskalasi ke HC dan cara menanganinya.</p>
</div>

<h3>B.1 User Lupa Password</h3>
<div class="warn">
  <div class="warn-title">Belum tersedia self-service reset</div>
  Solusi: HC buka <b>Admin Panel → Kelola Pekerja</b> (<code>/Admin/ManageWorkers</code>) → Edit user → reset password manual atau eskalasi ke Admin / IT. Sampaikan ke user untuk segera ganti password setelah login pertama.
</div>

<h3>B.2 User Tidak Bisa Akses Menu Tertentu</h3>
<p>Cek role user di <b>Kelola Pekerja</b>. Menu CMP/CDP/Profile selalu visible untuk semua role login. Menu Kelola Data + Admin Panel hanya untuk AdminHC. Update role kalau perlu.</p>

<h3>B.3 Upload File Gagal</h3>
<ul>
  <li>Cek ukuran file ≤ 10 MB.</li>
  <li>Cek format: PDF / DOCX / XLSX / JPG / PNG (sesuai context).</li>
  <li>Cek koneksi (terutama untuk file besar).</li>
  <li>Untuk upload KKJ/CPDP File: cek format PDF saja.</li>
</ul>

<h3>B.4 Sertifikat Tidak Muncul Setelah Assessment</h3>
<ul>
  <li>Cek apakah nilai user ≥ passing grade di <b>Assessment Monitoring Detail</b> (<code>/Admin/AssessmentMonitoringDetail</code>).</li>
  <li>Cek status assessment — harus <b>Completed</b>, bukan <b>In Progress</b>.</li>
  <li>Kalau passing grade tercapai tapi sertifikat masih kosong: cek di <b>Renewal Certificate</b>, kemungkinan sertifikat lama belum expired sehingga tidak terbit baru.</li>
</ul>

<h3>B.5 Notifikasi Tidak Masuk</h3>
<p>Refresh halaman dulu (bell icon di-update saat page load). Kalau masih kosong, cek konsol browser ada error? Kalau ada masalah service notifikasi, eskalasi ke IT.</p>

<h3>B.6 Reviewer Chain Stuck</h3>
<p>Buka <b>CDP → Bottleneck Report</b> (<code>/CDP/Dashboard</code> tab Bottleneck). Identifikasi siapa reviewer yang lambat. Eskalasi via komunikasi internal. HC <b>tidak boleh skip reviewer chain</b> — harus tunggu Sr Supervisor + Section Head approve dulu, baru HC final review.</p>

<h3>B.7 Browser Tidak Kompatibel</h3>
<p>Rekomendasi: Chrome / Edge / Firefox versi terbaru. Hindari Internet Explorer dan browser lawas. Mode dark/light tidak mempengaruhi fungsi.</p>

<h3>B.8 Impersonate Banner Tidak Hilang</h3>
<p>Klik tombol <b>Stop Impersonating</b> di banner kuning. Kalau banner masih ada setelah klik, refresh page. Kalau masih stuck, logout dan login ulang dengan akun HC asli.</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Lampiran B — Troubleshooting"
```

---

## Task 13: Lampiran C — URL Cheatsheet

**Files:**
- Modify: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`

- [ ] **Step 1: Insert Lampiran C header + URL table**

Insert after Lampiran B. Compile from URLs used across all task articles (Bab 1-6) + Controller action grep:

```html
<!-- ============================================================ -->
<!-- LAMPIRAN C: URL CHEATSHEET -->
<!-- ============================================================ -->
<div class="chapter page-break" id="lampC">
  <h2>Lampiran C — URL Cheatsheet</h2>
  <p>Quick reference untuk semua URL yang dipakai HC. Bookmark di browser untuk akses cepat.</p>
</div>

<table>
  <thead><tr><th>Menu</th><th>URL</th><th>Tujuan</th></tr></thead>
  <tbody>
    <!-- isi minimal 40 row dari hasil audit semua bab -->
    <tr><td>Login</td><td><code>/Account/Login</code></td><td>Login portal</td></tr>
    <tr><td>My Profile</td><td><code>/Account/Profile</code></td><td>Lihat / edit profil</td></tr>
    <tr><td>Settings</td><td><code>/Account/Settings</code></td><td>Ganti password</td></tr>
    <tr><td>CMP Library KKJ</td><td><code>/CMP/Index</code></td><td>Lihat KKJ posisi sendiri</td></tr>
    <tr><td>CMP CPDP Mapping</td><td><code>/CMP/DokumenKkj</code></td><td>Mapping CPDP per posisi</td></tr>
    <tr><td>CMP Training Records</td><td><code>/CMP/Records</code></td><td>Riwayat pelatihan</td></tr>
    <tr><td>CMP Records Team</td><td><code>/CMP/RecordsTeam</code></td><td>Monitoring records tim</td></tr>
    <tr><td>CMP Analytics</td><td><code>/CMP/AnalyticsDashboard</code></td><td>Compliance chart + export</td></tr>
    <tr><td>CMP Assessment</td><td><code>/CMP/Assessment</code></td><td>Kerjakan / lihat assessment</td></tr>
    <tr><td>CMP Budget Training</td><td><code>/CMP/BudgetTraining</code></td><td>Budget pelatihan</td></tr>
    <tr><td>CMP Certificate</td><td><code>/CMP/Certificate</code></td><td>Download sertifikat</td></tr>
    <tr><td>CDP Plan IDP</td><td><code>/CDP/PlanIdp</code></td><td>Lihat silabus IDP</td></tr>
    <tr><td>CDP Deliverable</td><td><code>/CDP/Deliverable</code></td><td>Monitor deliverable</td></tr>
    <tr><td>CDP Dashboard</td><td><code>/CDP/Dashboard</code></td><td>Coaching dashboard + bottleneck</td></tr>
    <tr><td>CDP Histori Proton</td><td><code>/CDP/HistoriProton</code></td><td>Histori coaching + export</td></tr>
    <tr><td>CDP Certification Mgmt</td><td><code>/CDP/CertificationManagement</code></td><td>Renewal sertifikat coaching</td></tr>
    <tr><td>Kelola Data Proton</td><td><code>/ProtonData/Index</code></td><td>Silabus CRUD</td></tr>
    <tr><td>Import Silabus</td><td><code>/ProtonData/ImportSilabus</code></td><td>Import silabus Excel</td></tr>
    <tr><td>Override Data</td><td><code>/ProtonData/Override</code></td><td>Override mapping data Proton</td></tr>
    <tr><td>Kelola Pekerja</td><td><code>/Admin/ManageWorkers</code></td><td>CRUD pekerja</td></tr>
    <tr><td>Import Workers</td><td><code>/Admin/ImportWorkers</code></td><td>Import Excel pekerja</td></tr>
    <tr><td>Kelola Units</td><td><code>/Admin/ManageOrganization</code></td><td>Units / Bagian</td></tr>
    <tr><td>Kelola Categories</td><td><code>/Admin/ManageCategories</code></td><td>Kategori assessment</td></tr>
    <tr><td>Bank Soal</td><td><code>/Admin/ManagePackages</code></td><td>Paket soal</td></tr>
    <tr><td>Import Soal</td><td><code>/Admin/ImportPackageQuestions</code></td><td>Import soal Excel</td></tr>
    <tr><td>Create Assessment</td><td><code>/Admin/CreateAssessment</code></td><td>Buat jadwal assessment</td></tr>
    <tr><td>Manage Assessment</td><td><code>/Admin/ManageAssessment</code></td><td>Edit assessment</td></tr>
    <tr><td>Assessment Monitoring</td><td><code>/Admin/AssessmentMonitoring</code></td><td>Real-time monitoring + force-close</td></tr>
    <tr><td>Coach-Coachee Mapping</td><td><code>/Admin/CoachCoacheeMapping</code></td><td>Mapping coach</td></tr>
    <tr><td>Coach Workload</td><td><code>/Admin/CoachWorkload</code></td><td>Beban kerja coach</td></tr>
    <tr><td>Renewal Certificate</td><td><code>/Admin/RenewalCertificate</code></td><td>Schedule renewal</td></tr>
    <tr><td>Add Training</td><td><code>/Admin/AddTraining</code></td><td>Input training manual</td></tr>
    <tr><td>Import Training</td><td><code>/Admin/ImportTraining</code></td><td>Import Excel training</td></tr>
    <tr><td>KKJ Matrix</td><td><code>/Admin/KkjMatrix</code></td><td>KKJ files</td></tr>
    <tr><td>KKJ Upload</td><td><code>/Admin/KkjUpload</code></td><td>Upload KKJ baru</td></tr>
    <tr><td>KKJ History</td><td><code>/Admin/KkjFileHistory</code></td><td>Histori KKJ file</td></tr>
    <tr><td>CPDP Files</td><td><code>/Admin/CpdpFiles</code></td><td>CPDP files</td></tr>
    <tr><td>CPDP Upload</td><td><code>/Admin/CpdpUpload</code></td><td>Upload CPDP baru</td></tr>
    <tr><td>CPDP History</td><td><code>/Admin/CpdpFileHistory</code></td><td>Histori CPDP file</td></tr>
    <tr><td>Audit Log</td><td><code>/Admin/AuditLog</code></td><td>Catatan aktivitas sistem</td></tr>
    <tr><td>Maintenance Mode</td><td><code>/Admin/Maintenance</code></td><td>Toggle maintenance</td></tr>
    <tr><td>Impersonate</td><td><code>/Admin/Impersonate</code></td><td>Login as user lain</td></tr>
  </tbody>
</table>
```

- [ ] **Step 2: Cross-check URL list against all task breadcrumbs di Bab 1-5**

Pastikan setiap URL yang muncul di task article ada di cheatsheet. Kalau tidak ada, tambahkan. Sebaliknya, kalau URL di cheatsheet belum dipakai di task article, cek apakah ada task yang missing.

- [ ] **Step 3: Commit**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): Lampiran C — URL Cheatsheet (40+ URL)"
```

---

## Task 14: URL Verification

**Files:**
- Read: `docs/Panduan-Operasional-HC-PortalHC-KPB.html`, all `Controllers/*.cs`

- [ ] **Step 1: Extract all URLs from HTML file**

Grep semua `<code class="url">/...</code>` value dari file. Buat list unique.

- [ ] **Step 2: Verify each URL resolves to an actual Controller action**

For each URL `/Controller/Action`, grep `Controllers/{Controller}Controller.cs` untuk method `public ... {Action}(`. Note any URL yang tidak resolve.

- [ ] **Step 3: Fix mismatched URLs**

Untuk setiap URL yang tidak ketemu:
- Cek apakah Action name typo
- Cek apakah Controller name beda
- Cek attribute routing `[Route("...")]` mungkin override convention

Edit file HTML, koreksi URL di breadcrumb article + di Lampiran C.

- [ ] **Step 4: Commit (kalau ada fix)**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): fix URL — koreksi route mismatch hasil verifikasi"
```

Kalau tidak ada fix, skip commit.

---

## Task 15: Print test + visual review + tag v1.0

**Files:**
- Read: `docs/Panduan-Operasional-HC-PortalHC-KPB.html` (visual + print)

- [ ] **Step 1: Buka file di browser, scroll dari atas sampai bawah**

Cek:
- Cover render rapi (icon, title, subtitle, badge, version)
- TOC main + sticky sidebar work
- Setiap chapter header ada page-break sebelumnya (`page-break` class)
- Setiap task article punya breadcrumb + URL badge biru + step cards numbered
- Callout (info biru / warn oranye) render dengan border kiri tebal
- Footer di bawah
- Sticky TOC sidebar muncul di desktop (resize window > 1100px), hilang di mobile (< 1100px)
- Highlight active TOC saat scroll

- [ ] **Step 2: Print preview test**

Ctrl+P (Cmd+P di Mac) → print preview.

Cek:
- TOC sidebar dan action bar tidak tampil
- Cover di halaman 1 sendirian
- Tiap chapter mulai halaman baru
- Step card / callout tidak terpotong tengah-tengah (page-break-inside avoid working)
- Chapter header tidak terpotong dari sub-section pertamanya

Kalau ada masalah:
- Tambah `page-break-before: always;` di `.chapter`
- Tambah `page-break-inside: avoid;` di `.step`, `.info`, `.warn`, `.task article`
- Edit `@media print` rules accordingly

- [ ] **Step 3: Fix visual / print issues** kalau ada, edit CSS di `<style>` block. Loop step 1-3 sampai bersih.

- [ ] **Step 4: Save sample PDF untuk verifikasi**

Print → Save as PDF → `docs/Panduan-Operasional-HC-PortalHC-KPB.pdf` (optional, jangan commit PDF — di `.gitignore` kalau perlu).

- [ ] **Step 5: Final commit + tag**

```bash
git add docs/Panduan-Operasional-HC-PortalHC-KPB.html
git commit -m "docs(panduan-hc): final visual + print polish — v1.0"
git tag panduan-operasional-hc-v1.0
```

- [ ] **Step 6: Update memory**

Save project memory: file location, version tag, scope (HC L2 only), commit hash. Format sesuai `MEMORY.md` index style.

---

## Self-Review Notes

**Spec coverage check:**
- Section 5 (struktur 6 bab + 3 lampiran) → Tasks 2-13. ✓
- Section 6 (style match Panduan-* + sticky TOC) → Task 1. ✓
- Section 7 (anatomi task hybrid: breadcrumb + URL + step + callout) → defined in Conventions block, used in Tasks 2-10. ✓
- Section 8 (sumber konten: GuideContent + audit View/Controller) → audit step in every Bab task. ✓
- Section 9 (workflow build) → Tasks 1-15 align. ✓
- Section 11 (acceptance criteria) → Tasks 14 (URL verify) + 15 (visual + print) cover all 7 criteria. ✓
- Decision: file location `docs/`, no in-app registration → Task 15 skips wwwroot move and PDF link reg. ✓

**Placeholder scan:** No "TBD", no "TODO", no "Add error handling". All code blocks contain actual HTML/Bash content. Bab 6 step 2 has placeholder rows in matrix table — explicitly marked "isi dari hasil audit" with step 3 to fill in. Bab 5b task description for 5.5 says "include detailed warning callout" — acceptable as the implementer audits the Excel template at that time.

**Type consistency:** Anchor IDs consistent: `cmp-library-kkj`, `cdp-plan-idp`, `admin-bank-soal`, `data-silabus`, `lampA/B/C`, `bab1/2/3/4/5/6`. Cross-references in TOC match.

**Granularity:** Each task ≤ 5 steps. Each step ≤ 5 minutes of actual work (audit + insert + verify + commit pattern). Total 15 tasks ≈ ~12-15 hours of implementer time including audit.
