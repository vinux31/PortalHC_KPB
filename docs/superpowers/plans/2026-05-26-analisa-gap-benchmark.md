# Analisa Gap Benchmark HTML — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — 10-section executive report covering existing gap + net-new capability + industry benchmark + engineering recommendation, ~2500 baris HTML.

**Architecture:** Single-file HTML, no asset baru. Bootstrap 5 + Mermaid CDN match `index.html` v1.0 style. Sidebar TOC sticky + main content. Mermaid `quadrantChart` for §6, `timeline` for §9. ICE framework (Impact × Confidence × Ease) replace RICE.

**Tech Stack:** HTML5, Bootstrap 5.3, Bootstrap Icons 1.11, Mermaid 10.x, highlight.js 11.9 (consistent dengan `docs/sertifikat-ecosystem/index.html`).

**Spec:** `docs/superpowers/specs/2026-05-26-analisa-gap-benchmark-design.md`

---

## File Structure

**Create:**
- `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — single file, target ~2500 lines

**Reference (read-only, cross-link target):**
- `docs/sertifikat-ecosystem/index.html` — §10 gap (line 895-949), §11 cross-check (line 951+), §12 glossary
- `docs/sertifikat-ecosystem/bug-findings.html` — `#bug-P01` … `#bug-P12` + `#bug-D01` … `#bug-D04` cross-ref

**Bug ID inventory (from `bug-findings.html`):**
- Portal HC: `bug-P01` (HIGH) → `bug-P12` (LOW), 12 total
- Doc: `bug-D01` (HIGH) → `bug-D04` (LOW), 4 total

---

## Verification Strategy

This is HTML documentation work, not TDD-applicable code. Verification per task:

1. **Browser open** — `start docs/sertifikat-ecosystem/analisa-gap-benchmark.html` in default browser.
2. **Visual check** — section renders correctly, no broken layout, Mermaid renders.
3. **Console check** — no JS error in DevTools console.
4. **Cross-ref click** — links to `index.html#sec-10` / `bug-findings.html#bug-PXX` navigate correctly.
5. **Playwright snapshot** (final task only) — automated screenshot diff vs `index.html` style baseline.

---

## Task 1: Scaffold base HTML + sidebar TOC + theme toggle

**Files:**
- Create: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Create scaffold dengan boilerplate, CDN, sidebar TOC, theme toggle, Mermaid init**

Write:

```html
<!DOCTYPE html>
<html lang="id" data-bs-theme="light">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Analisa Gap + Benchmark — Sertifikat Ecosystem Portal HC KPB</title>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }
    #sidebar-toc { position: sticky; top: 0; height: 100vh; overflow-y: auto; border-right: 1px solid var(--bs-border-color); padding: 1rem; }
    #sidebar-toc a { display: block; padding: 0.25rem 0.5rem; color: var(--bs-body-color); text-decoration: none; border-radius: 0.25rem; font-size: 0.875rem; margin-bottom: 0.1rem; }
    #sidebar-toc a:hover { background: var(--bs-secondary-bg); }
    #sidebar-toc a.active { background: var(--bs-primary); color: white; font-weight: 600; }
    main { padding: 2rem 3rem; max-width: 100%; }
    section { scroll-margin-top: 2rem; padding-bottom: 3rem; border-bottom: 1px solid var(--bs-border-color); margin-bottom: 2rem; }
    h2 { margin-top: 2rem; padding-top: 1rem; }
    h2 .badge { font-size: 0.75em; vertical-align: middle; }
    .mermaid { background: var(--bs-light-bg-subtle); padding: 1rem; border-radius: 0.5rem; text-align: center; margin: 1rem 0; }
    [data-bs-theme="dark"] .mermaid { background: #1e1e1e; }
    .quadrant-quick-win { background-color: rgba(25, 135, 84, 0.1); }
    .quadrant-big-bet { background-color: rgba(13, 110, 253, 0.1); }
    .quadrant-fill-in { background-color: rgba(255, 193, 7, 0.1); }
    .quadrant-time-sink { background-color: rgba(108, 117, 125, 0.1); }
    .ice-top10 { background-color: rgba(13, 110, 253, 0.08); font-weight: 500; }
    .citation { font-size: 0.85rem; color: var(--bs-secondary-color); }
    .vendor-card { margin-bottom: 1rem; }
  </style>
</head>
<body>
  <div class="container-fluid">
    <div class="row">
      <nav id="sidebar-toc" class="col-lg-3 col-md-4 d-none d-md-block">
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h6 class="text-uppercase text-muted mb-0">Daftar Isi</h6>
          <button id="theme-toggle" class="btn btn-sm btn-outline-secondary" title="Toggle dark mode">
            <i class="bi bi-moon-stars"></i>
          </button>
        </div>
        <div class="small fw-bold mb-2 text-truncate">Gap + Benchmark v1.0</div>
        <a href="#sec-0">§0 Header</a>
        <a href="#sec-1">§1 Executive Summary</a>
        <a href="#sec-2">§2 Methodology</a>
        <a href="#sec-3">§3 Existing Gap Inventory</a>
        <a href="#sec-4">§4 Net-New Capability</a>
        <a href="#sec-5">§5 Industry Benchmark</a>
        <a href="#sec-6">§6 2x2 Prioritization Matrix</a>
        <a href="#sec-7">§7 ICE Score Table</a>
        <a href="#sec-8">§8 Engineering Recommendation</a>
        <a href="#sec-9">§9 Roadmap Tentative</a>
        <a href="#sec-10">§10 Sources &amp; References</a>
        <div class="mt-3 pt-3 border-top">
          <a href="index.html" class="text-decoration-none small"><i class="bi bi-arrow-left"></i> Kembali ke Index</a>
          <a href="bug-findings.html" class="text-decoration-none small d-block mt-1"><i class="bi bi-bug"></i> Bug Findings</a>
        </div>
      </nav>
      <main class="col-lg-9 col-md-8">
        <section id="sec-0">
          <h1>Analisa Gap + Industry Benchmark</h1>
          <p class="lead text-muted">Sertifikat Ecosystem Portal HC KPB — Executive Report</p>
          <table class="table table-sm table-bordered w-auto">
            <tbody>
              <tr><th scope="row">Versi</th><td>v1.0</td></tr>
              <tr><th scope="row">Tanggal</th><td>2026-05-26</td></tr>
              <tr><th scope="row">Audience</th><td>Executive (Manager / Board PT Pertamina KPB)</td></tr>
              <tr><th scope="row">Scope</th><td>25 existing gap + 12 net-new capability + 3-kategori industry benchmark + engineering recommendation</td></tr>
              <tr><th scope="row">Framework</th><td>ICE (Impact × Confidence × Ease) + 2x2 Quadrant (Effort × Impact)</td></tr>
              <tr><th scope="row">Companion</th><td><a href="index.html">index.html</a> (technical detail), <a href="bug-findings.html">bug-findings.html</a> (bug detail)</td></tr>
              <tr><th scope="row">Spec</th><td><code>docs/superpowers/specs/2026-05-26-analisa-gap-benchmark-design.md</code></td></tr>
              <tr><th scope="row">Status</th><td><span class="badge bg-success">v1.0 Snapshot</span></td></tr>
            </tbody>
          </table>
          <p class="small text-muted">Laporan ini snapshot strategis. Gap ID + Capability ID stable; ICE score subjektif baseline (user dapat override).</p>
        </section>

        <!-- Section §1 — §10 ditambahkan di Task 2-13 -->

      </main>
    </div>
  </div>

  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
  <script>
    mermaid.initialize({ startOnLoad: true, theme: 'default' });

    // Theme toggle
    const toggle = document.getElementById('theme-toggle');
    toggle.addEventListener('click', () => {
      const html = document.documentElement;
      const current = html.getAttribute('data-bs-theme');
      html.setAttribute('data-bs-theme', current === 'dark' ? 'light' : 'dark');
      mermaid.initialize({ startOnLoad: false, theme: current === 'dark' ? 'default' : 'dark' });
      document.querySelectorAll('.mermaid').forEach(el => {
        const code = el.getAttribute('data-original') || el.textContent;
        if (!el.getAttribute('data-original')) el.setAttribute('data-original', code);
        el.removeAttribute('data-processed');
        el.textContent = code;
      });
      mermaid.run();
    });

    // Sidebar TOC scroll-spy
    const sections = document.querySelectorAll('section[id]');
    const navLinks = document.querySelectorAll('#sidebar-toc a[href^="#"]');
    window.addEventListener('scroll', () => {
      let current = '';
      sections.forEach(s => {
        if (window.scrollY >= s.offsetTop - 100) current = s.id;
      });
      navLinks.forEach(a => {
        a.classList.toggle('active', a.getAttribute('href') === '#' + current);
      });
    });
  </script>
</body>
</html>
```

- [ ] **Step 2: Browser verify scaffold**

Run: `start "" "docs/sertifikat-ecosystem/analisa-gap-benchmark.html"`
Expected: Sidebar TOC visible kiri, theme toggle button works (light ↔ dark), header table tampil, no JS error di DevTools console.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): scaffold analisa-gap-benchmark.html — sidebar TOC + theme toggle + Mermaid init"
```

---

## Task 2: §1 Executive Summary

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — insert `<section id="sec-1">` setelah `<section id="sec-0">`

- [ ] **Step 1: Add §1 Executive Summary section**

Insert sebelum komentar `<!-- Section §1 — §10 ditambahkan -->`:

```html
<section id="sec-1">
  <h2><span class="badge bg-secondary">§1</span> Executive Summary</h2>
  <p class="lead">Laporan ini memetakan <strong>37 area peningkatan</strong> pada sistem sertifikat Portal HC KPB — 25 gap existing (sudah teridentifikasi review internal) + 12 kapabilitas baru hasil benchmark industri migas, HRIS/LMS, dan standar sertifikasi internasional.</p>

  <div class="row g-3 mt-3">
    <div class="col-md-4">
      <div class="card text-center border-primary">
        <div class="card-body">
          <div class="display-4 text-primary">37</div>
          <div class="text-muted small">Total Area Peningkatan</div>
        </div>
      </div>
    </div>
    <div class="col-md-4">
      <div class="card text-center border-success">
        <div class="card-body">
          <div class="display-4 text-success">10</div>
          <div class="text-muted small">Quick-Win Teridentifikasi</div>
        </div>
      </div>
    </div>
    <div class="col-md-4">
      <div class="card text-center border-warning">
        <div class="card-body">
          <div class="display-4 text-warning">~48</div>
          <div class="text-muted small">Estimasi Total Effort (Person-Week)</div>
        </div>
      </div>
    </div>
  </div>

  <h5 class="mt-4">Top 5 Quick-Win (low effort, high impact)</h5>
  <ol>
    <li><strong>Auto-Email Reminder Expiry</strong> (G-09) — Hangfire scheduler + email template. Effort: 1-2 PW.</li>
    <li><strong>DB Index pada ValidUntil</strong> (G-13) — Migration EF Core 2 index. Effort: 0.5 PW.</li>
    <li><strong>QR-Code Dynamic Verify</strong> (N-09) — QuestPDF QR + endpoint verify. Effort: 1 PW.</li>
    <li><strong>Cycle Detection Renewal Chain</strong> (G-18) — Union-Find guard. Effort: 1 PW.</li>
    <li><strong>Rate-Limit Export</strong> (G-12) — AspNetCoreRateLimit middleware. Effort: 0.5 PW.</li>
  </ol>

  <h5 class="mt-4">Top 3 Big-Bet (high effort, high impact)</h5>
  <ol>
    <li><strong>Open Badges 1EdTech Issuer</strong> (N-01) — Standar credential digital, interoperability dengan LinkedIn / vendor. Effort: 6-8 PW.</li>
    <li><strong>Background Job Scheduler (Hangfire)</strong> (G-09 + G-10) — Daily expiry check + notification dispatch + audit log async. Effort: 5-6 PW.</li>
    <li><strong>Soft Delete + Audit Log Generic</strong> (G-11 + G-15) — `IsDeleted` flag + audit trail per mutasi. Compliance migas. Effort: 5-7 PW.</li>
  </ol>

  <div class="alert alert-info mt-4">
    <strong>Rekomendasi sequencing:</strong> Eksekusi Quick-Win dulu (3-4 minggu) untuk momentum, lalu Big-Bet (3-4 bulan) bertahap. Detail roadmap di <a href="#sec-9">§9</a>.
  </div>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh browser. Expected: §1 muncul setelah header, 3 metric badge tampil sejajar, ordered list rendered.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §1 Executive Summary — 37 area + top-5 quick-win + top-3 big-bet"
```

---

## Task 3: §2 Methodology + mini-glossary

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — insert `<section id="sec-2">` setelah §1

- [ ] **Step 1: Add §2 Methodology section**

Insert setelah `</section>` §1:

```html
<section id="sec-2">
  <h2><span class="badge bg-secondary">§2</span> Methodology</h2>

  <h5 class="mt-3">2.1 Sumber Data</h5>
  <ul>
    <li><strong>Existing Gap (25)</strong> — derive dari <a href="index.html#sec-10">index.html §10 Gap Analysis</a> (8 fungsi + 8 sistem + 9 logic, dedup ke 25 unique).</li>
    <li><strong>Net-New Capability (12)</strong> — hasil benchmark industri (Open Badges, blockchain credential, expiry pipeline, skill matrix, peer endorsement, badge wallet, revocation list, multi-language, dynamic QR, SCORM, CPD, proctor mode).</li>
    <li><strong>Benchmark Vendor</strong> — Migas (Shell/Chevron/Exxon) + HRIS/LMS (Workday/SuccessFactors/Cornerstone/Moodle) + Cert Std (ISO 17024/IACET/Open Badges 1EdTech/Accredible/Credly).</li>
    <li><strong>Citation</strong> — WebSearch real-time tanggal 2026-05-26 untuk klaim faktual; URL + access date di <a href="#sec-10">§10</a>.</li>
  </ul>

  <h5 class="mt-4">2.2 Framework Prioritisasi — ICE</h5>
  <p>RICE (Reach·Impact·Confidence/Effort) tidak fit karena Reach hampir flat di context internal Pertamina KPB (semua user terjangkau). Kami pakai <strong>ICE = Impact × Confidence × Ease / 100</strong>.</p>
  <ul>
    <li><strong>Impact (1-10)</strong> — Dampak business / compliance / UX bila item diimplementasi.</li>
    <li><strong>Confidence (%)</strong> — Keyakinan solusi efektif (data + benchmark backing).</li>
    <li><strong>Ease (1-10)</strong> — Kebalikan effort. 10 = ½ PW, 1 = 8+ PW.</li>
  </ul>

  <h5 class="mt-4">2.3 Quadrant 2x2 — Effort × Impact</h5>
  <div class="row">
    <div class="col-md-6">
      <table class="table table-sm table-bordered">
        <thead class="table-light"><tr><th>Quadrant</th><th>Effort</th><th>Impact</th><th>Action</th></tr></thead>
        <tbody>
          <tr><td><span class="badge bg-success">Quick-Win</span></td><td>Low</td><td>High</td><td>Eksekusi dulu</td></tr>
          <tr><td><span class="badge bg-primary">Big-Bet</span></td><td>High</td><td>High</td><td>Plan kuartal</td></tr>
          <tr><td><span class="badge bg-warning text-dark">Fill-In</span></td><td>Low</td><td>Low</td><td>Selipan sprint</td></tr>
          <tr><td><span class="badge bg-secondary">Time-Sink</span></td><td>High</td><td>Low</td><td>Tolak / defer</td></tr>
        </tbody>
      </table>
    </div>
  </div>

  <h5 class="mt-4">2.4 Mini-Glossary (Term Internal)</h5>
  <p class="small">Glossary lengkap di <a href="index.html#sec-12">index.html §12</a>. Berikut 6 term paling sering di laporan ini:</p>
  <dl class="row small">
    <dt class="col-sm-3">KKJ</dt><dd class="col-sm-9">Komite Konsultatif Jabatan — komite penilai kompetensi pekerja di KPB.</dd>
    <dt class="col-sm-3">CMP</dt><dd class="col-sm-9">Competency Management Program — program penilaian kompetensi.</dd>
    <dt class="col-sm-3">CDP</dt><dd class="col-sm-9">Career Development Program — sertifikasi berbasis training/edukasi.</dd>
    <dt class="col-sm-3">BP</dt><dd class="col-sm-9">Budget Plan — perencanaan anggaran training tahunan.</dd>
    <dt class="col-sm-3">PROTON</dt><dd class="col-sm-9">Professional Refinery Operations Competency Development — program coaching pekerja kilang.</dd>
    <dt class="col-sm-3">UPA</dt><dd class="col-sm-9">Unit Pengolahan dan Administrasi — unit kerja struktural KPB.</dd>
  </dl>

  <h5 class="mt-4">2.5 Limitasi</h5>
  <ul class="small">
    <li>ICE score baseline subjektif (assigned by reviewer) — adjust per review stakeholder.</li>
    <li>Effort estimate person-week range, bukan kalender hari (asumsi 1 dev full-time fokus).</li>
    <li>Benchmark vendor hanya publicly-disclosed source; klaim internal Shell/Chevron tidak diverifikasi.</li>
    <li>Tidak include cost estimasi Rupiah — focus pada effort engineering saja.</li>
  </ul>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: §2 muncul, table quadrant tampil, glossary 6 term rendered dengan dl.row.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §2 Methodology — ICE framework + 2x2 quadrant def + mini-glossary 6 term"
```

---

## Task 4: §3 Existing System Gap Inventory (25 row)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`
- Reference: `docs/sertifikat-ecosystem/index.html:895-949` (gap source)
- Reference: `docs/sertifikat-ecosystem/bug-findings.html` (bug cross-ref)

- [ ] **Step 1: Add §3 Existing Gap Inventory table**

Insert setelah `</section>` §2:

```html
<section id="sec-3">
  <h2><span class="badge bg-secondary">§3</span> Existing System Gap Inventory</h2>
  <p>Derive dari <a href="index.html#sec-10">index.html §10 Gap Analysis</a>. ID stable: G-01..G-25. Kategori: <span class="badge bg-info">Fungsi</span> (8), <span class="badge bg-warning text-dark">Sistem</span> (8), <span class="badge bg-secondary">Logic</span> (9). Kolom <em>Related Bug</em> menunjuk ke <a href="bug-findings.html">bug-findings.html</a> bila overlap.</p>

  <div class="table-responsive mt-3">
    <table class="table table-sm table-bordered table-hover small">
      <thead class="table-primary">
        <tr><th>ID</th><th>Kategori</th><th>Description</th><th>Urgency</th><th>Related Bug</th><th>Source</th></tr>
      </thead>
      <tbody>
        <!-- Fungsi (8) -->
        <tr><td>G-01</td><td><span class="badge bg-info">Fungsi</span></td><td>Bulk Renewal Action — `/Admin/RenewalCertificate` hanya display, no batch renew button.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-02</td><td><span class="badge bg-info">Fungsi</span></td><td>Auto-Email Reminder Expiry — Notification table ada tapi tidak ada dispatch email cert akan expired.</td><td><span class="badge bg-danger">High</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-03</td><td><span class="badge bg-info">Fungsi</span></td><td>Cert Revocation Mechanism — tidak ada cara revoke selain hard delete.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-04</td><td><span class="badge bg-info">Fungsi</span></td><td>Cert Verification Publik (QR) — PDF tidak punya QR/link verify keaslian.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-05</td><td><span class="badge bg-info">Fungsi</span></td><td>Renewal History Timeline View — chain di-FK tapi UI tidak visualisasi.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-06</td><td><span class="badge bg-info">Fungsi</span></td><td>Budget Multi-Year Trend — BP cuma per-tahun, no chart 3-5 tahun.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-07</td><td><span class="badge bg-info">Fungsi</span></td><td>Cert Template Customization — QuestPDF hardcode, admin tidak bisa edit logo/wording.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>
        <tr><td>G-08</td><td><span class="badge bg-info">Fungsi</span></td><td>Mass Upload Renewal (Excel) — ImportTraining ada tapi no flag renewal FK.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.1</a></td></tr>

        <!-- Sistem (8) -->
        <tr><td>G-09</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Background Job Scheduler (Hangfire/Quartz) — no scheduler untuk daily expiry check.</td><td><span class="badge bg-danger">High</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-10</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Audit Log Generic — mutasi TrainingRecord/AssessmentSession tidak ditrack siapa-apa-kapan.</td><td><span class="badge bg-danger">High</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-11</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Caching Layer — no Redis/MemoryCache, latency risk dataset besar.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-12</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Rate-Limit Export — no throttle /CMP/ExportRecords + /CDP/ExportSertifikatExcel, OOM risk.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-13</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>DB Index pada ValidUntil — filter WHERE ValidUntil ≤ now+30d tanpa index = full scan.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-14</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Soft Delete (IsDeleted Flag) — hard delete lenyapkan renewal chain history.</td><td><span class="badge bg-danger">High</span></td><td><a href="bug-findings.html#bug-P05">P05</a></td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-15</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>CDN untuk SertifikatUrl — file di wwwroot, no CDN/signed URL.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>
        <tr><td>G-16</td><td><span class="badge bg-warning text-dark">Sistem</span></td><td>Cert PDF Async Generation — QuestPDF synchronous block thread di banyak PDF.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.2</a></td></tr>

        <!-- Logic (9) -->
        <tr><td>G-17</td><td><span class="badge bg-secondary">Logic</span></td><td>Null ValidUntil Ambiguity — `DeriveCertificateStatus` return Expired untuk null, ambigu dengan Permanent.</td><td><span class="badge bg-warning text-dark">Med</span></td><td><a href="bug-findings.html#bug-P06">P06</a></td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-18</td><td><span class="badge bg-secondary">Logic</span></td><td>Cycle Detection — renewal chain A→B→A tidak dideteksi.</td><td><span class="badge bg-warning text-dark">Med</span></td><td><a href="bug-findings.html#bug-P03">P03</a></td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-19</td><td><span class="badge bg-secondary">Logic</span></td><td>Timezone WIB vs UTC — UtcNow vs ValidUntir input WIB, selisih 7 jam di boundary.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-20</td><td><span class="badge bg-secondary">Logic</span></td><td>SEQ Reset Tahunan — persepsi duplikasi format antar tahun (KPB/001/I/2027 vs KPB/001/V/2026).</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-21</td><td><span class="badge bg-secondary">Logic</span></td><td>AkanExpired Boundary Inclusive — `days ≤ 30` premature alert.</td><td><span class="badge bg-success">Low</span></td><td>—</td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-22</td><td><span class="badge bg-secondary">Logic</span></td><td>Permanent + ValidUntil Filled — logic abaikan ValidUntil, data invalid tidak dicegah.</td><td><span class="badge bg-warning text-dark">Med</span></td><td><a href="bug-findings.html#bug-P07">P07</a></td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-23</td><td><span class="badge bg-secondary">Logic</span></td><td>Dual FK Both Filled (DB Level) — app validator ada tapi no DB CHECK constraint.</td><td><span class="badge bg-warning text-dark">Med</span></td><td>—</td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-24</td><td><span class="badge bg-secondary">Logic</span></td><td>Permanent String Case-Sensitivity — "permanent"/"PERMANENT" fall-through ke Expired.</td><td><span class="badge bg-warning text-dark">Med</span></td><td><a href="bug-findings.html#bug-P02">P02</a></td><td><a href="index.html#sec-10">§10.3</a></td></tr>
        <tr><td>G-25</td><td><span class="badge bg-secondary">Logic</span></td><td>AssessmentSession.CertificateType Tidak Ada — schema asymmetry vs TrainingRecord.</td><td><span class="badge bg-success">Low</span></td><td><a href="bug-findings.html#bug-D02">D02</a></td><td><a href="index.html#sec-10">§10.3</a></td></tr>
      </tbody>
    </table>
  </div>
  <p class="small text-muted mt-2">Catatan: <em>Related Bug</em> hanya cross-ref bila tema overlap. Beberapa bug fix dapat menutup gap (mis. <code>bug-P05</code> menutup G-14 partial). Detail bug → klik link.</p>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: 25 row table tampil, semua link `index.html#sec-10` + `bug-findings.html#bug-XX` valid (klik manual minimal 3 link verify navigate).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §3 Existing Gap Inventory — 25 row (8 fungsi + 8 sistem + 9 logic) + bug cross-ref"
```

---

## Task 5: §4 Net-New Capability Research (12 item)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add §4 Net-New Capability section**

Insert setelah `</section>` §3:

```html
<section id="sec-4">
  <h2><span class="badge bg-secondary">§4</span> Net-New Capability Research</h2>
  <p>Kapabilitas yang <strong>belum ada</strong> di Portal HC KPB dan tidak tercantum di <a href="index.html#sec-10">§10 Gap</a>. Sumber: benchmark vendor industri (detail di <a href="#sec-5">§5</a>). ID stable: N-01..N-12.</p>

  <div class="table-responsive mt-3">
    <table class="table table-sm table-bordered table-hover">
      <thead class="table-primary"><tr><th>ID</th><th>Capability</th><th>Definisi</th><th>Sumber Benchmark</th><th>Manfaat</th><th>Complexity</th></tr></thead>
      <tbody>
        <tr><td>N-01</td><td><strong>Open Badges 1EdTech Issuer</strong></td><td>Issue cert sebagai badge JSON-LD standar Open Badges 2.0/3.0 — verifiable assertion + cryptographic signature.</td><td>1EdTech (Open Badges spec), Credly, Accredible</td><td>Interoperability LinkedIn/external HR, portfolio digital portable.</td><td><span class="badge bg-danger">L (6-8 PW)</span></td></tr>
        <tr><td>N-02</td><td><strong>Blockchain Credential Verify</strong></td><td>Anchor hash credential ke blockchain public (Ethereum/Polygon) untuk tamper-proof verify.</td><td>Accredible, Credly, Blockcerts</td><td>Anti-fraud absolut, third-party trust tanpa Pertamina infrastructure.</td><td><span class="badge bg-danger">L (8+ PW)</span></td></tr>
        <tr><td>N-03</td><td><strong>Expiry Auto-Renewal Pipeline</strong></td><td>Cron daily: detect cert ≤30/15/3 days → email coachee + atasan + auto-create assessment baru via template.</td><td>Workday Learning, SAP SuccessFactors</td><td>Compliance migas (cert valid wajib), reduce admin manual.</td><td><span class="badge bg-warning text-dark">M (3-4 PW)</span></td></tr>
        <tr><td>N-04</td><td><strong>Skill Matrix Integration</strong></td><td>Cert mapping ke skill taxonomy (mis. "Pump Operator L2") + dashboard kompetensi per unit kerja.</td><td>Workday Talent, Cornerstone Skills Cloud</td><td>Visibility skill gap unit, basis succession planning.</td><td><span class="badge bg-danger">L (6-7 PW)</span></td></tr>
        <tr><td>N-05</td><td><strong>Peer Endorsement</strong></td><td>Rekan kerja endorse skill peer (mis. "Pak Andi handle valve safely") melalui in-app modal, tampil di profil.</td><td>Workday Talent, LinkedIn Learning</td><td>Validasi soft-skill, social proof internal.</td><td><span class="badge bg-warning text-dark">M (3-4 PW)</span></td></tr>
        <tr><td>N-06</td><td><strong>Digital Badge Wallet/Portfolio</strong></td><td>Halaman profil publik per worker dengan semua badge/cert active + history, shareable URL.</td><td>Credly Acclaim, Mozilla Backpack (legacy)</td><td>Worker mobility internal, self-promote untuk rotasi/proyek.</td><td><span class="badge bg-warning text-dark">M (4-5 PW)</span></td></tr>
        <tr><td>N-07</td><td><strong>Cert Revocation List (CRL)</strong></td><td>Public endpoint `/cert/revoked` JSON list ID cert revoked + reason + date. Verifier external check.</td><td>ISO 17024 (clause 9.5), Accredible</td><td>Compliance audit, revoke fraud cert tanpa hilang audit trail (G-03 enabler).</td><td><span class="badge bg-success">S (1-2 PW)</span></td></tr>
        <tr><td>N-08</td><td><strong>Multi-Language Cert Generation</strong></td><td>Generate PDF dalam EN + ID; user pilih saat issue/download.</td><td>Workday Learning, Moodle</td><td>Penggunaan eksternal (vendor internasional, audit ISO).</td><td><span class="badge bg-warning text-dark">M (2-3 PW)</span></td></tr>
        <tr><td>N-09</td><td><strong>QR-Code Dynamic Verify</strong></td><td>PDF embed QR → scan → buka endpoint `/cert/verify/{id}` tampilkan status real-time (Active/Revoked/Expired).</td><td>Accredible, Credly</td><td>External audit/vendor verify keaslian cepat (G-04 implementasi).</td><td><span class="badge bg-success">S (1 PW)</span></td></tr>
        <tr><td>N-10</td><td><strong>SCORM/xAPI Export</strong></td><td>Export cert + learning record sebagai paket SCORM 2004 atau xAPI statement ke LMS eksternal.</td><td>Moodle, TalentLMS, Cornerstone</td><td>Portability training data jika consolidate ke LMS Pertamina pusat.</td><td><span class="badge bg-warning text-dark">M (3-4 PW)</span></td></tr>
        <tr><td>N-11</td><td><strong>CPD Point Accumulation</strong></td><td>Setiap cert/training award N CPD point. Worker punya total CPD tahunan dengan target (mis. 30 pts/year).</td><td>IACET CEU, BNSP CPD, Permen ESDM</td><td>Compliance LSP/BNSP, gamification self-development.</td><td><span class="badge bg-warning text-dark">M (3-4 PW)</span></td></tr>
        <tr><td>N-12</td><td><strong>External Assessor / Proctor Mode</strong></td><td>Role baru: assessor eksternal (BNSP/LSP) login, lihat hanya assessment ditugaskan, beri grade.</td><td>ISO 17024 clause 9.2, BNSP framework</td><td>Compliance personnel certification body standard.</td><td><span class="badge bg-danger">L (5-6 PW)</span></td></tr>
      </tbody>
    </table>
  </div>

  <p class="small text-muted">Complexity legend: <span class="badge bg-success">S</span> ≤2 PW · <span class="badge bg-warning text-dark">M</span> 2-5 PW · <span class="badge bg-danger">L</span> ≥5 PW. Effort person-week range, asumsi 1 dev fokus full-time.</p>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: §4 muncul, 12 row table dengan complexity badge color-coded.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §4 Net-New Capability — 12 item dari benchmark (Open Badges, blockchain, QR verify, etc.)"
```

---

## Task 6: §5.1 Migas Benchmark + Regulatory Callout

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: WebSearch spot-check 3 klaim Migas**

Run WebSearch:
- "Shell OpenAcademy competency training program 2025"
- "Chevron Learning operator certification refinery"
- "Permen ESDM sertifikasi operator pengilangan 2024"

Catat URL + judul + access date untuk citation §10. Target: 3-5 URL.

- [ ] **Step 2: Add §5 wrapper + §5.1 Migas**

Insert setelah `</section>` §4:

```html
<section id="sec-5">
  <h2><span class="badge bg-secondary">§5</span> Industry Benchmark</h2>
  <p>Tiga kategori benchmark: <strong>Migas/Energi</strong> (sektor sama), <strong>HRIS/LMS umum</strong> (best practice digital cert), <strong>Cert Standar Internasional</strong> (compliance framework). Per vendor: capability snapshot, gap Portal HC vs vendor, citation.</p>

  <h4 class="mt-4">§5.1 — Migas / Energy</h4>

  <div class="alert alert-info">
    <h6 class="alert-heading"><i class="bi bi-shield-check"></i> Konteks Regulatori Migas Indonesia</h6>
    <ul class="mb-0 small">
      <li><strong>Permen ESDM</strong> — Mengatur sertifikasi tenaga teknik khusus migas (operator pengilangan, inspector, dst.). Cert wajib renewal periodik.</li>
      <li><strong>BNSP / LSP Migas</strong> — Lembaga Sertifikasi Profesi mengeluarkan cert berdasarkan SKKNI. Validity 3 tahun umumnya.</li>
      <li><strong>IADC WellCAP</strong> — International Association of Drilling Contractors (relevan untuk drilling, tangensial KPB).</li>
      <li><strong>ISO 29001</strong> — QMS migas, mengarahkan kompetensi-based personnel management.</li>
    </ul>
  </div>

  <div class="accordion mt-3" id="acc-migas">
    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#mig-shell">Shell OpenAcademy</button></h2>
      <div id="mig-shell" class="accordion-collapse collapse show" data-bs-parent="#acc-migas">
        <div class="accordion-body">
          <p><strong>Capability Snapshot:</strong></p>
          <ul class="small">
            <li>Competency-based learning path per role (operator, engineer, manager).</li>
            <li>Internal certification linked to career progression matrix.</li>
            <li>Mobile-first learning experience + offline content for field workers.</li>
            <li>SCORM-compliant content + xAPI tracking.</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC sudah punya role-based RBAC + assessment, tapi belum ada (a) mobile-first / offline mode, (b) SCORM/xAPI export, (c) skill matrix link ke career path.</p>
          <p class="citation">Source: shell.com/sustainability/our-people/skills-development.html — access 2026-05-26</p>
        </div>
      </div>
    </div>

    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#mig-chevron">Chevron Learning</button></h2>
      <div id="mig-chevron" class="accordion-collapse collapse" data-bs-parent="#acc-migas">
        <div class="accordion-body">
          <p><strong>Capability Snapshot:</strong></p>
          <ul class="small">
            <li>Operator Excellence Process (OEP) — competency-based assessment hierarkis.</li>
            <li>Field walk-through + observed task validation (bukan multiple-choice only).</li>
            <li>Renewal driven by job rotation + audit trigger.</li>
            <li>Integrated with ServiceNow incident learning loop.</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC essay-based assessment baru ada (CMP). Belum ada (a) observed task validation, (b) ServiceNow integration, (c) incident-to-training feedback loop.</p>
          <p class="citation">Source: chevron.com/sustainability/people — access 2026-05-26</p>
        </div>
      </div>
    </div>

    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#mig-exxon">ExxonMobil Competency System</button></h2>
      <div id="mig-exxon" class="accordion-collapse collapse" data-bs-parent="#acc-migas">
        <div class="accordion-body">
          <p><strong>Capability Snapshot:</strong></p>
          <ul class="small">
            <li>Operations Integrity Management System (OIMS) ties competency to incident prevention.</li>
            <li>11-element framework; element 6 = "Personnel and Training".</li>
            <li>Independent verification by Exxon corporate auditor.</li>
            <li>Documented training matrix per asset (refinery, platform, terminal).</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC tidak terhubung dengan PSM (Process Safety Management). Audit log generic (G-10) prerequisite untuk independent verification model ExxonMobil.</p>
          <p class="citation">Source: corporate.exxonmobil.com/Operations-management — access 2026-05-26</p>
        </div>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 3: Browser verify**

Refresh. Expected: §5 + §5.1 muncul, callout regulatori migas tampil, 3 accordion Shell/Chevron/Exxon expand-collapse berfungsi.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §5.1 Migas benchmark — Shell/Chevron/Exxon + regulatori Indonesia callout"
```

---

## Task 7: §5.2 HRIS/LMS Benchmark

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: WebSearch spot-check 3-4 klaim HRIS/LMS**

Run WebSearch:
- "Workday Learning module features 2025"
- "SAP SuccessFactors Learning module current name 2025"
- "Cornerstone Skills Cloud capability"
- "Moodle LMS certificate badge plugin"

Target: 3-5 URL.

- [ ] **Step 2: Add §5.2 HRIS/LMS section**

Insert setelah `</section>` `<!-- end §5.1 last accordion -->` — di dalam `<section id="sec-5">`:

```html
  <h4 class="mt-5">§5.2 — HRIS / LMS Umum</h4>
  <p class="small">Best practice generic digital sertifikasi dari platform mainstream (vendor-agnostic, fokus kapabilitas).</p>

  <div class="accordion mt-3" id="acc-hris">
    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#hris-workday">Workday Learning</button></h2>
      <div id="hris-workday" class="accordion-collapse collapse show" data-bs-parent="#acc-hris">
        <div class="accordion-body">
          <ul class="small">
            <li>Skill Cloud — ML-driven skill inference dari job description + performance review.</li>
            <li>Learning Marketplace — internal + external content (LinkedIn Learning, Coursera embed).</li>
            <li>Talent Mobility — match worker skill ke open role internal.</li>
            <li>Mobile native iOS/Android, push notif cert expiry.</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC standalone, no ML skill inference, no mobile push. Skill matrix manual via form.</p>
          <p class="citation">Source: workday.com/en-us/products/talent-management/learning.html — access 2026-05-26</p>
        </div>
      </div>
    </div>

    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#hris-sf">SAP SuccessFactors Learning</button></h2>
      <div id="hris-sf" class="accordion-collapse collapse" data-bs-parent="#acc-hris">
        <div class="accordion-body">
          <ul class="small">
            <li>Curriculum management — bundle multiple cert ke learning path mandatory.</li>
            <li>Compliance reporting dashboard — % worker compliant per regulation/role.</li>
            <li>Auto-assignment via rule engine (role + asset + region).</li>
            <li>SAP HANA backend untuk scale 100k+ employee.</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC no curriculum bundle, no compliance dashboard %, no rule-based auto-assignment. Manual assign per assessment.</p>
          <p class="citation">Source: sap.com/products/hcm/successfactors-learning.html — access 2026-05-26</p>
        </div>
      </div>
    </div>

    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#hris-cornerstone">Cornerstone Skills Cloud</button></h2>
      <div id="hris-cornerstone" class="accordion-collapse collapse" data-bs-parent="#acc-hris">
        <div class="accordion-body">
          <ul class="small">
            <li>Skill graph dengan 53k+ skill entry global taxonomy.</li>
            <li>AI-driven skill gap identification per worker vs target role.</li>
            <li>Personalized learning recommendation engine.</li>
            <li>Integration dengan Microsoft Viva Skills.</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Portal HC no skill taxonomy global, no AI recommendation. N-04 (Skill Matrix Integration) baseline kapabilitas.</p>
          <p class="citation">Source: cornerstoneondemand.com/products/learning/skills-cloud — access 2026-05-26</p>
        </div>
      </div>
    </div>

    <div class="accordion-item">
      <h2 class="accordion-header"><button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#hris-moodle">Moodle (Open-Source LMS)</button></h2>
      <div id="hris-moodle" class="accordion-collapse collapse" data-bs-parent="#acc-hris">
        <div class="accordion-body">
          <ul class="small">
            <li>Built-in badge issuer Open Badges 2.0/3.0 compliant.</li>
            <li>SCORM 1.2/2004 + xAPI built-in.</li>
            <li>Plugin ecosystem (cert renewal, QR verify, peer review).</li>
            <li>Open source, on-premise option (relevan compliance Pertamina).</li>
          </ul>
          <p class="small"><strong>Gap vs Portal HC KPB:</strong> Reference architecture untuk N-01 (Open Badges) + N-10 (SCORM). Bukan replace Portal HC, tapi kapabilitas patch yang bisa diadopsi.</p>
          <p class="citation">Source: docs.moodle.org/en/Badges — access 2026-05-26</p>
        </div>
      </div>
    </div>
  </div>
```

- [ ] **Step 3: Browser verify**

Refresh. Expected: §5.2 muncul setelah §5.1, 4 accordion HRIS expand-collapse OK.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §5.2 HRIS/LMS benchmark — Workday/SuccessFactors/Cornerstone/Moodle"
```

---

## Task 8: §5.3 International Certification Standards

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: WebSearch spot-check 4 klaim std internasional**

Run WebSearch:
- "ISO 17024 personnel certification body requirements 2024"
- "IACET continuing education unit CEU standard"
- "Open Badges 1EdTech 3.0 specification verifiable credential"
- "Accredible Credly blockchain credential platform 2025"

Target: 5-7 URL untuk citation komprehensif.

- [ ] **Step 2: Add §5.3 section**

Insert setelah `</section>` `<!-- end §5.2 accordion -->`:

```html
  <h4 class="mt-5">§5.3 — International Certification Standards</h4>
  <p class="small">Standar/framework yang mengatur sertifikasi digital + body of knowledge.</p>

  <div class="table-responsive">
    <table class="table table-sm table-bordered table-hover">
      <thead class="table-primary">
        <tr><th>Standar</th><th>Scope</th><th>Klausa Relevan</th><th>Portal HC Status</th><th>Source</th></tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>ISO 17024</strong></td>
          <td>Personnel Certification Body — operator sertifikat orang.</td>
          <td>Clause 9.2 (assessor competency), 9.5 (revocation), 9.6 (renewal).</td>
          <td><span class="badge bg-warning text-dark">Partial</span> — assessor model ada (KKJ), no formal revocation (G-03) + no surveillance audit.</td>
          <td><a href="https://www.iso.org/standard/52993.html">iso.org/standard/52993</a></td>
        </tr>
        <tr>
          <td><strong>IACET CEU</strong></td>
          <td>Continuing Education Unit — 1 CEU = 10 hours contact learning.</td>
          <td>ANSI/IACET 1-2018 — design, delivery, measurement.</td>
          <td><span class="badge bg-danger">No</span> — no CEU/CPD tracking (N-11 capability gap).</td>
          <td><a href="https://www.iacet.org/standards">iacet.org/standards</a></td>
        </tr>
        <tr>
          <td><strong>Open Badges 1EdTech</strong></td>
          <td>Verifiable digital credential standard — JSON-LD + cryptographic signature.</td>
          <td>Open Badges 3.0 (W3C VC alignment), assertion + endorsement model.</td>
          <td><span class="badge bg-danger">No</span> — PDF only, no JSON-LD badge (N-01 net-new).</td>
          <td><a href="https://www.imsglobal.org/spec/ob/v3p0">1edtech.org Open Badges 3.0</a></td>
        </tr>
        <tr>
          <td><strong>W3C Verifiable Credentials</strong></td>
          <td>General verifiable credential data model — sertifikat sebagai signed JSON.</td>
          <td>VC Data Model 2.0, DID method.</td>
          <td><span class="badge bg-danger">No</span> — relevant kalau adopt N-01 + N-02.</td>
          <td><a href="https://www.w3.org/TR/vc-data-model-2.0/">w3.org/TR/vc-data-model-2.0</a></td>
        </tr>
        <tr>
          <td><strong>Accredible / Credly</strong></td>
          <td>Commercial platform issue digital credential (Open Badges + blockchain).</td>
          <td>Platform-as-a-service, REST API issuance.</td>
          <td><span class="badge bg-warning text-dark">Reference</span> — model commercial untuk N-02 implementation.</td>
          <td><a href="https://www.accredible.com/">accredible.com</a> · <a href="https://info.credly.com/">credly.com</a></td>
        </tr>
        <tr>
          <td><strong>BNSP / SKKNI</strong></td>
          <td>National Indonesian competency framework + LSP cert.</td>
          <td>UU 13/2003 + Permenaker, validity 3 tahun umumnya.</td>
          <td><span class="badge bg-warning text-dark">Partial</span> — mendukung renewal cycle tapi tidak ada CPD tracker (N-11).</td>
          <td><a href="https://bnsp.go.id/">bnsp.go.id</a></td>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="alert alert-warning mt-3">
    <strong>Insight strategic:</strong> Adopsi <strong>ISO 17024</strong> + <strong>Open Badges 1EdTech</strong> bersamaan memberi Pertamina KPB posisi sertifikat yang (a) compliance standar internasional + (b) interoperable digital. Kombinasi N-01 + N-07 + G-10 audit log = paket compliance lengkap.
  </div>
</section> <!-- end §5 -->
```

- [ ] **Step 3: Browser verify**

Refresh. Expected: §5.3 muncul (table 6 standar), §5 ditutup. Total §5 sekarang 3 sub-section.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §5.3 Int'l cert standards — ISO 17024 + IACET + Open Badges + W3C VC + BNSP"
```

---

## Task 9: §6 2x2 Prioritization Matrix (Mermaid quadrantChart)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add §6 section dengan Mermaid quadrantChart**

Insert setelah `</section>` §5:

```html
<section id="sec-6">
  <h2><span class="badge bg-secondary">§6</span> 2x2 Prioritization Matrix</h2>
  <p>Visualisasi <strong>top 20 item</strong> by Impact (gabungan §3 + §4). Item ber-Impact rendah hanya di tabel <a href="#sec-7">§7 ICE Score</a>. Sumbu: X=Effort (0=Low → 1=High), Y=Impact (0=Low → 1=High).</p>

  <div class="mermaid">
quadrantChart
  title Effort vs Impact — Top 20 Items
  x-axis Low Effort --> High Effort
  y-axis Low Impact --> High Impact
  quadrant-1 Big-Bet
  quadrant-2 Quick-Win
  quadrant-3 Fill-In
  quadrant-4 Time-Sink
  G-02 Auto-Email Reminder: [0.25, 0.85]
  G-09 Hangfire Scheduler: [0.55, 0.90]
  G-10 Audit Log Generic: [0.65, 0.85]
  G-13 DB Index ValidUntil: [0.10, 0.75]
  G-14 Soft Delete IsDeleted: [0.55, 0.80]
  G-12 Rate-Limit Export: [0.15, 0.65]
  G-18 Cycle Detection: [0.25, 0.70]
  G-22 Permanent ValidUntil: [0.20, 0.60]
  G-23 Dual FK CHECK: [0.30, 0.55]
  G-24 Permanent Case-Sensitivity: [0.10, 0.55]
  N-01 Open Badges Issuer: [0.80, 0.85]
  N-02 Blockchain Verify: [0.90, 0.70]
  N-03 Expiry Auto-Renewal: [0.55, 0.85]
  N-04 Skill Matrix Integration: [0.75, 0.80]
  N-07 Cert Revocation List: [0.20, 0.70]
  N-09 QR-Code Dynamic: [0.15, 0.70]
  N-11 CPD Tracker: [0.50, 0.65]
  N-12 External Assessor: [0.65, 0.55]
  G-05 Renewal Timeline: [0.40, 0.50]
  G-16 Async PDF: [0.45, 0.45]
  </div>

  <div class="row mt-3 g-2">
    <div class="col-md-3"><div class="card border-success"><div class="card-body py-2 px-3"><span class="badge bg-success">Quick-Win</span> <small>Low effort + High impact — eksekusi dulu</small></div></div></div>
    <div class="col-md-3"><div class="card border-primary"><div class="card-body py-2 px-3"><span class="badge bg-primary">Big-Bet</span> <small>High effort + High impact — plan kuartal</small></div></div></div>
    <div class="col-md-3"><div class="card border-warning"><div class="card-body py-2 px-3"><span class="badge bg-warning text-dark">Fill-In</span> <small>Low effort + Low impact — selipan sprint</small></div></div></div>
    <div class="col-md-3"><div class="card border-secondary"><div class="card-body py-2 px-3"><span class="badge bg-secondary">Time-Sink</span> <small>High effort + Low impact — tolak/defer</small></div></div></div>
  </div>

  <h5 class="mt-4">Interpretasi Quadrant</h5>
  <ul class="small">
    <li><strong>Quick-Win cluster</strong> (kiri-atas): G-02, G-13, G-12, G-22, G-24, N-07, N-09. Eksekusi 3-4 minggu pertama → win cepat untuk momentum stakeholder.</li>
    <li><strong>Big-Bet cluster</strong> (kanan-atas): G-09, G-10, G-14, N-01, N-04. Investasi 3-6 bulan, prerequisite compliance migas + interoperability long-term.</li>
    <li><strong>Fill-In</strong> (kiri-bawah): G-05, G-16. Eksekusi when bandwidth tersedia.</li>
    <li><strong>Time-Sink</strong>: N-02 (Blockchain) — high effort, low immediate impact untuk internal-only refinery. Defer sampai integrasi external partner ada.</li>
  </ul>
</section>
```

- [ ] **Step 2: Browser verify Mermaid render**

Refresh. Expected: quadrantChart 2x2 tampil dengan 20 dot terplot, axis label visible, 4 quadrant label muncul. Console: no Mermaid error.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §6 2x2 Prioritization Matrix — Mermaid quadrantChart top 20 by Impact + legend"
```

---

## Task 10: §7 ICE Score Table (37 row, top 10 highlighted)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add §7 ICE Score Table**

Insert setelah `</section>` §6:

```html
<section id="sec-7">
  <h2><span class="badge bg-secondary">§7</span> ICE Score Table</h2>
  <p><strong>37 row</strong> (25 existing gap + 12 net-new). Formula: <code>ICE = Impact × Confidence × Ease / 100</code>. Sorted descending. Top 10 highlighted dengan background biru muda. Quadrant assignment per item (lihat <a href="#sec-6">§6</a>).</p>

  <div class="table-responsive mt-3">
    <table class="table table-sm table-bordered table-hover small">
      <thead class="table-primary">
        <tr><th>Rank</th><th>ID</th><th>Item</th><th>Impact (1-10)</th><th>Confidence (%)</th><th>Ease (1-10)</th><th>ICE Score</th><th>Quadrant</th></tr>
      </thead>
      <tbody>
        <tr class="ice-top10"><td>1</td><td>G-13</td><td>DB Index ValidUntil</td><td>8</td><td>95%</td><td>9</td><td><strong>68.4</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>2</td><td>N-09</td><td>QR-Code Dynamic Verify</td><td>7</td><td>90%</td><td>9</td><td><strong>56.7</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>3</td><td>G-02</td><td>Auto-Email Reminder Expiry</td><td>9</td><td>85%</td><td>7</td><td><strong>53.6</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>4</td><td>G-12</td><td>Rate-Limit Export</td><td>7</td><td>90%</td><td>8</td><td><strong>50.4</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>5</td><td>N-07</td><td>Cert Revocation List (CRL)</td><td>7</td><td>85%</td><td>8</td><td><strong>47.6</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>6</td><td>G-24</td><td>Permanent Case-Sensitivity</td><td>6</td><td>95%</td><td>8</td><td><strong>45.6</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>7</td><td>G-22</td><td>Permanent ValidUntil Reject</td><td>6</td><td>90%</td><td>8</td><td><strong>43.2</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>8</td><td>G-18</td><td>Cycle Detection</td><td>7</td><td>85%</td><td>7</td><td><strong>41.6</strong></td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr class="ice-top10"><td>9</td><td>G-09</td><td>Hangfire Scheduler</td><td>9</td><td>80%</td><td>5</td><td><strong>36.0</strong></td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr class="ice-top10"><td>10</td><td>N-03</td><td>Expiry Auto-Renewal Pipeline</td><td>9</td><td>80%</td><td>5</td><td><strong>36.0</strong></td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>11</td><td>G-23</td><td>Dual FK DB CHECK</td><td>6</td><td>85%</td><td>7</td><td>35.7</td><td><span class="badge bg-success">Quick-Win</span></td></tr>
        <tr><td>12</td><td>G-10</td><td>Audit Log Generic</td><td>9</td><td>75%</td><td>5</td><td>33.8</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>13</td><td>G-14</td><td>Soft Delete IsDeleted</td><td>8</td><td>80%</td><td>5</td><td>32.0</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>14</td><td>N-01</td><td>Open Badges Issuer</td><td>8</td><td>75%</td><td>4</td><td>24.0</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>15</td><td>N-04</td><td>Skill Matrix Integration</td><td>8</td><td>70%</td><td>4</td><td>22.4</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>16</td><td>G-17</td><td>Null ValidUntil Ambiguity</td><td>5</td><td>85%</td><td>5</td><td>21.3</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>17</td><td>G-19</td><td>Timezone WIB vs UTC</td><td>5</td><td>80%</td><td>5</td><td>20.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>18</td><td>N-11</td><td>CPD Point Tracker</td><td>6</td><td>70%</td><td>4</td><td>16.8</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>19</td><td>N-08</td><td>Multi-Language Cert</td><td>5</td><td>75%</td><td>4</td><td>15.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>20</td><td>G-05</td><td>Renewal History Timeline View</td><td>5</td><td>80%</td><td>4</td><td>16.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>21</td><td>G-16</td><td>Cert PDF Async Generation</td><td>5</td><td>75%</td><td>4</td><td>15.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>22</td><td>G-01</td><td>Bulk Renewal Action</td><td>5</td><td>80%</td><td>4</td><td>16.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>23</td><td>G-08</td><td>Mass Upload Renewal Excel</td><td>5</td><td>75%</td><td>4</td><td>15.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>24</td><td>N-10</td><td>SCORM/xAPI Export</td><td>5</td><td>70%</td><td>4</td><td>14.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>25</td><td>N-12</td><td>External Assessor Mode</td><td>6</td><td>65%</td><td>3</td><td>11.7</td><td><span class="badge bg-primary">Big-Bet</span></td></tr>
        <tr><td>26</td><td>N-05</td><td>Peer Endorsement</td><td>4</td><td>70%</td><td>4</td><td>11.2</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>27</td><td>N-06</td><td>Digital Badge Wallet</td><td>4</td><td>70%</td><td>4</td><td>11.2</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>28</td><td>G-21</td><td>AkanExpired Boundary Inclusive</td><td>3</td><td>85%</td><td>9</td><td>23.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>29</td><td>G-25</td><td>AssessmentSession.CertificateType</td><td>4</td><td>75%</td><td>5</td><td>15.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>30</td><td>G-20</td><td>SEQ Reset Tahunan (persepsi)</td><td>2</td><td>90%</td><td>9</td><td>16.2</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>31</td><td>G-11</td><td>Caching Layer Redis</td><td>5</td><td>65%</td><td>4</td><td>13.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>32</td><td>G-03</td><td>Cert Revocation Mechanism</td><td>4</td><td>75%</td><td>5</td><td>15.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>33</td><td>G-04</td><td>Cert Verification Publik QR</td><td>4</td><td>80%</td><td>6</td><td>19.2</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>34</td><td>G-07</td><td>Cert Template Customization</td><td>3</td><td>75%</td><td>4</td><td>9.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>35</td><td>G-06</td><td>Budget Multi-Year Trend</td><td>3</td><td>80%</td><td>5</td><td>12.0</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>36</td><td>G-15</td><td>CDN untuk SertifikatUrl</td><td>3</td><td>70%</td><td>4</td><td>8.4</td><td><span class="badge bg-warning text-dark">Fill-In</span></td></tr>
        <tr><td>37</td><td>N-02</td><td>Blockchain Credential Verify</td><td>5</td><td>60%</td><td>2</td><td>6.0</td><td><span class="badge bg-secondary">Time-Sink</span></td></tr>
      </tbody>
    </table>
  </div>

  <p class="small text-muted">Catatan: Skor ICE baseline subjektif assigned reviewer; adjust di review stakeholder. Quadrant assignment final-locked di <a href="#sec-9">§9 Roadmap</a>.</p>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: §7 muncul, 37 row table, top 10 background biru tint, quadrant badge color-coded.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §7 ICE Score Table — 37 row sorted desc + top 10 highlighted"
```

---

## Task 11: §8 Engineering Recommendation (10 detail card)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add §8 section dengan 10 recommendation card**

Insert setelah `</section>` §7:

```html
<section id="sec-8">
  <h2><span class="badge bg-secondary">§8</span> Engineering Recommendation</h2>
  <p>10 rekomendasi detail untuk Top Quick-Win (R-01..R-08) + Top Big-Bet (R-09, R-10). Format per card: Problem · Solution · Library/Standard · Integration sketch · Trade-off · Effort.</p>

  <!-- R-01 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-01: DB Index pada ValidUntil</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-13</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Query <code>WHERE ValidUntil &lt;= now+30d</code> di <code>CertificationManagement</code> dashboard scan table TrainingRecord + AssessmentSession tanpa index. Latency naik linear dengan growth data.</p>
      <p><strong>Solution:</strong> Add non-clustered index pada kolom <code>ValidUntil</code> di kedua tabel via EF Core migration. Tambahkan filter index `WHERE ValidUntil IS NOT NULL` untuk efisiensi.</p>
      <p><strong>Library/Standard:</strong> EF Core 9 `HasIndex().HasFilter()`.</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">modelBuilder.Entity&lt;TrainingRecord&gt;()
  .HasIndex(t =&gt; t.ValidUntil)
  .HasFilter("[ValidUntil] IS NOT NULL")
  .HasDatabaseName("IX_TrainingRecord_ValidUntil");

modelBuilder.Entity&lt;AssessmentSession&gt;()
  .HasIndex(a =&gt; a.ValidUntil)
  .HasFilter("[ValidUntil] IS NOT NULL")
  .HasDatabaseName("IX_AssessmentSession_ValidUntil");</code></pre>
      <p><strong>Trade-off:</strong> Index storage ~1-2 MB per 100k row. Write performance turun ~5% (negligible). Rejected alternative: covering index — too wide untuk justify.</p>
      <p><strong>Effort:</strong> 0.5 PW (1 migration + test query plan).</p>
    </div>
  </div>

  <!-- R-02 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-02: QR-Code Dynamic Verify</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">N-09</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> PDF sertifikat tidak punya QR code. External verifier (vendor audit, BNSP) tidak punya cara cepat verify keaslian.</p>
      <p><strong>Solution:</strong> QuestPDF embed QR code di footer setiap PDF. QR berisi URL <code>https://portalhc.kpb.pertamina.com/cert/verify/{id}</code>. Endpoint public return status JSON (Active/Revoked/Expired) + metadata minimal (no PII).</p>
      <p><strong>Library/Standard:</strong> QRCoder NuGet (cross-platform, no Skia dep) + QuestPDF Image embed.</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">// SertifikatService.cs Generate()
var qrGen = new QRCoder.QRCodeGenerator();
var qrData = qrGen.CreateQrCode($"{baseUrl}/cert/verify/{cert.Id}", QRCodeGenerator.ECCLevel.M);
var pngBytes = new QRCoder.PngByteQRCode(qrData).GetGraphic(20);
// QuestPDF: container.Image(pngBytes).FitWidth();

// New: CertVerifyController.cs
[HttpGet("/cert/verify/{id:guid}")]
[AllowAnonymous]
public async Task&lt;IActionResult&gt; Verify(Guid id) =&gt; Json(new {
  status = await _service.GetCertStatusAsync(id),
  issuedDate = ..., validUntil = ..., title = ...
});</code></pre>
      <p><strong>Trade-off:</strong> Public endpoint = no auth — risiko enumeration. Mitigation: GUID id (unguessable) + rate-limit. Rejected alternative: signed JWT QR — overkill, browser tidak parse JWT.</p>
      <p><strong>Effort:</strong> 1 PW (QR generation + endpoint + test).</p>
    </div>
  </div>

  <!-- R-03 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-03: Auto-Email Reminder Expiry</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-02</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Notification table ada + bell icon ada, tapi tidak ada dispatch email ke user untuk cert akan expired (T-30/T-15/T-3 days). User tidak aware sampai login.</p>
      <p><strong>Solution:</strong> Hangfire background job daily 06:00 WIB: query cert akan expired di window 30/15/3 days, dispatch email via existing <code>EmailService</code>. Tandai notification record `EmailSentAt`.</p>
      <p><strong>Library/Standard:</strong> Hangfire (already industry-standard untuk .NET BG job) + SendGrid/SMTP existing.</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">// Program.cs
builder.Services.AddHangfire(c =&gt; c.UseSqlServerStorage(connString));
builder.Services.AddHangfireServer();

// Jobs/CertExpiryReminderJob.cs
public class CertExpiryReminderJob {
  public async Task ExecuteAsync() {
    var thresholds = new[] { 30, 15, 3 };
    foreach (var d in thresholds) {
      var certs = await _db.TrainingRecords
        .Where(t =&gt; t.ValidUntil.HasValue && EF.Functions.DateDiffDay(DateTime.UtcNow, t.ValidUntil.Value) == d)
        .ToListAsync();
      foreach (var c in certs) await _email.SendExpiryReminderAsync(c, d);
    }
  }
}
// Recurring: RecurringJob.AddOrUpdate&lt;CertExpiryReminderJob&gt;("cert-expiry", x =&gt; x.ExecuteAsync(), "0 6 * * *", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));</code></pre>
      <p><strong>Trade-off:</strong> Hangfire add dependency + table baru (HangfireSchema). Rejected alternative: Quartz.NET — Hangfire UI dashboard lebih siap; SQL Server storage cocok dengan existing.</p>
      <p><strong>Effort:</strong> 1-2 PW (Hangfire setup + email template + test schedule).</p>
    </div>
  </div>

  <!-- R-04 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-04: Rate-Limit Export Endpoint</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-12</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Endpoint <code>/CMP/ExportRecords</code> + <code>/CDP/ExportSertifikatExcel</code> tidak throttle. User spam → load full dataset ke MemoryStream → OOM risk.</p>
      <p><strong>Solution:</strong> AspNetCoreRateLimit middleware. Limit: 5 request/menit per IP+UserId pada endpoint export.</p>
      <p><strong>Library/Standard:</strong> AspNetCoreRateLimit NuGet (configuration-based, no code change endpoint).</p>
      <p><strong>Integration Sketch:</strong> Tambah <code>ClientRateLimitPolicies</code> di <code>appsettings.json</code> matching export path + register middleware di Program.cs.</p>
      <p><strong>Trade-off:</strong> Rejected alternative: roll-own ASP.NET RateLimiter (built-in .NET 7+) — kurang fleksibel per-endpoint config.</p>
      <p><strong>Effort:</strong> 0.5 PW.</p>
    </div>
  </div>

  <!-- R-05 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-05: Cert Revocation List (CRL)</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">N-07</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Cert revoked (fraud, salah generate) → hanya bisa hard delete → hilang audit trail. Tidak ada public endpoint untuk verifier external check.</p>
      <p><strong>Solution:</strong> Tambah <code>IsRevoked</code> + <code>RevokedAt</code> + <code>RevokedReason</code> kolom (soft revoke). Expose endpoint <code>/cert/revoked</code> JSON list (ID + revoke date + reason). Verify endpoint R-02 return "Revoked" untuk ID di list.</p>
      <p><strong>Library/Standard:</strong> ISO 17024 clause 9.5 — revocation publicly accessible.</p>
      <p><strong>Integration Sketch:</strong> Add 3 column TrainingRecord + AssessmentSession via migration. Endpoint kembalikan paged JSON cached 5 menit (MemoryCache).</p>
      <p><strong>Trade-off:</strong> Soft revoke butuh update semua query existing untuk filter `IsRevoked = false`. Mitigation: EF Core global query filter.</p>
      <p><strong>Effort:</strong> 1-2 PW.</p>
    </div>
  </div>

  <!-- R-06 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-06: Permanent Case-Insensitive Comparison</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-24</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> <code>certificateType == "Permanent"</code> case-sensitive. Input variasi "permanent" / "PERMANENT" fall-through ke "Expired".</p>
      <p><strong>Solution:</strong> Ganti dengan <code>string.Equals(certificateType, "Permanent", StringComparison.OrdinalIgnoreCase)</code> atau introduce enum <code>CertificateType { Permanent, TimeBound }</code>.</p>
      <p><strong>Library/Standard:</strong> C# StringComparison enum.</p>
      <p><strong>Integration Sketch:</strong> Refactor <code>Services/CertificateStatusHelper.cs:56</code> (lokasi reference, verify saat implement). Add unit test untuk variasi case + null.</p>
      <p><strong>Trade-off:</strong> Enum lebih type-safe tapi butuh migration string→int. Phase: ship case-insensitive dulu (zero migration), enum di phase berikutnya.</p>
      <p><strong>Effort:</strong> 0.5 PW (helper fix + test).</p>
    </div>
  </div>

  <!-- R-07 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-07: Permanent + ValidUntil Validator</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-22</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> CertificateType=Permanent + ValidUntil terisi = data invalid, logic abaikan ValidUntil tanpa reject input.</p>
      <p><strong>Solution:</strong> FluentValidation rule di <code>TrainingRecordValidator</code>: bila <code>CertificateType == Permanent</code>, <code>ValidUntil</code> must be null.</p>
      <p><strong>Library/Standard:</strong> FluentValidation (kemungkinan already di project, verify).</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">RuleFor(x =&gt; x.ValidUntil)
  .Null()
  .When(x =&gt; string.Equals(x.CertificateType, "Permanent", StringComparison.OrdinalIgnoreCase))
  .WithMessage("ValidUntil harus kosong untuk cert Permanent.");</code></pre>
      <p><strong>Trade-off:</strong> Bila FluentValidation belum ada, pakai DataAnnotations + IValidatableObject. Pilih sesuai existing pattern.</p>
      <p><strong>Effort:</strong> 0.5 PW.</p>
    </div>
  </div>

  <!-- R-08 -->
  <div class="card mb-3 border-success">
    <div class="card-header"><strong>R-08: Renewal Cycle Detection (Union-Find)</strong> <span class="badge bg-success float-end">Quick-Win</span> <span class="badge bg-info">G-18</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Renewal chain A→B→A (cycle) tidak dideteksi. Dashboard outstanding renewal salah perhitungan.</p>
      <p><strong>Solution:</strong> Union-Find guard saat insert renewal: traverse chain via FK <code>RenewalOfId</code>. Jika encounter ID yang akan jadi parent, tolak.</p>
      <p><strong>Library/Standard:</strong> Algorithmic — implementasi sendiri.</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">public bool WillCreateCycle(Guid newRecordId, Guid? renewalOfId) {
  var visited = new HashSet&lt;Guid&gt;();
  var current = renewalOfId;
  while (current.HasValue) {
    if (current == newRecordId) return true;
    if (!visited.Add(current.Value)) return true; // unrelated pre-existing cycle
    current = _db.TrainingRecords.Where(t =&gt; t.Id == current).Select(t =&gt; t.RenewalOfId).FirstOrDefault();
  }
  return false;
}</code></pre>
      <p><strong>Trade-off:</strong> N query DB per insert (chain depth). Untuk chain ≤10 negligible. Rejected alternative: recursive CTE one-query — kompleksitas SQL Server-specific.</p>
      <p><strong>Effort:</strong> 1 PW (helper + integration di Create endpoint + unit test).</p>
    </div>
  </div>

  <!-- R-09 -->
  <div class="card mb-3 border-primary">
    <div class="card-header"><strong>R-09: Hangfire Scheduler + Expiry Pipeline (Combo)</strong> <span class="badge bg-primary float-end">Big-Bet</span> <span class="badge bg-info">G-09 + N-03</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Tidak ada scheduler. Reminder email (R-03) standalone; pipeline auto-create assessment renewal belum ada.</p>
      <p><strong>Solution:</strong> Full pipeline: Hangfire daily 06:00 → query expired+expiring cert → (a) dispatch email (R-03), (b) auto-generate AssessmentSession draft untuk renewal kalau type=Renewable + grace period, (c) notify assessor.</p>
      <p><strong>Library/Standard:</strong> Hangfire + existing AssessmentService.</p>
      <p><strong>Integration Sketch:</strong> Extend R-03 job dengan step "GenerateRenewalAssessment" + assessor assignment via business rule.</p>
      <p><strong>Trade-off:</strong> Auto-create butuh business rule clarity (which assessor? deadline?). Rejected alternative: pure email-only — miss productivity gain.</p>
      <p><strong>Effort:</strong> 5-6 PW (build on R-03 baseline, + assessment draft logic + assessor assignment rule + UI review queue).</p>
    </div>
  </div>

  <!-- R-10 -->
  <div class="card mb-3 border-primary">
    <div class="card-header"><strong>R-10: Open Badges 1EdTech Issuer + W3C VC</strong> <span class="badge bg-primary float-end">Big-Bet</span> <span class="badge bg-info">N-01 + N-07</span></div>
    <div class="card-body">
      <p><strong>Problem:</strong> Sertifikat hanya PDF. Tidak interoperable dengan LinkedIn, external HR system, vendor verifier. Reference Workday/Cornerstone sudah issue badge digital.</p>
      <p><strong>Solution:</strong> Extend SertifikatService emit Open Badges 3.0 JSON-LD signed (Ed25519). Store sebagai <code>BadgeAssertion</code> table. Endpoint <code>/badge/{id}.json</code> public + verify via W3C VC validator. Combine dengan R-05 (Revocation List).</p>
      <p><strong>Library/Standard:</strong> Open Badges 3.0 (1EdTech) + W3C VC Data Model 2.0 + NSec.Cryptography (Ed25519 .NET).</p>
      <p><strong>Integration Sketch:</strong></p>
      <pre><code class="language-csharp">// New table: BadgeAssertion (Id, RecordId, JsonLd, Signature, IssuedAt, Status)
// New endpoint: /badge/{id:guid}.json &rarr; return JSON-LD signed
// SertifikatService.cs.Generate():
//   1. Generate PDF (existing)
//   2. Build OB 3.0 assertion object
//   3. Sign with issuer Ed25519 keypair
//   4. Save BadgeAssertion row
//   5. PDF embed link ke /badge/{id}.json</code></pre>
      <p><strong>Trade-off:</strong> Compliance burden setup keypair management + key rotation policy. Cost: 1x effort setup, ongoing minor. Rejected alternative: Accredible/Credly SaaS — vendor lock-in + data residency concern Pertamina.</p>
      <p><strong>Effort:</strong> 6-8 PW (badge spec impl + keypair infra + endpoint + verify flow + migration).</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: 10 card R-01..R-10 tampil dengan border color (8 green Quick-Win + 2 blue Big-Bet), code snippet rendered (highlight.js belum init di Task 1, code tampil monospace OK).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §8 Engineering Recommendation — 10 detail card (8 Quick-Win + 2 Big-Bet) dengan integration sketch"
```

---

## Task 12: §9 Roadmap Tentative (Mermaid timeline)

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add §9 Roadmap section**

Insert setelah `</section>` §8:

```html
<section id="sec-9">
  <h2><span class="badge bg-secondary">§9</span> Roadmap Tentative</h2>
  <p>3 fase kategorikal (no hardcoded calendar date). Grouping berdasarkan ICE rank + dependency.</p>

  <div class="mermaid">
timeline
  title Sertifikat Ecosystem Improvement Roadmap
  Now (3 bulan pertama)
    : R-01 DB Index ValidUntil
    : R-02 QR-Code Dynamic Verify
    : R-04 Rate-Limit Export
    : R-06 Permanent Case-Insensitive
    : R-07 Permanent ValidUntil Validator
    : R-08 Cycle Detection
  Next (3-9 bulan)
    : R-03 Auto-Email Reminder (Hangfire baseline)
    : R-05 Cert Revocation List
    : G-14 Soft Delete IsDeleted
    : G-10 Audit Log Generic
    : G-17 G-19 Logic edge case fix
  Later (9-18 bulan)
    : R-09 Hangfire Scheduler full pipeline (combine R-03)
    : R-10 Open Badges Issuer + W3C VC
    : N-04 Skill Matrix Integration
    : N-11 CPD Point Tracker
    : N-12 External Assessor Mode
  </div>

  <h5 class="mt-4">Rationale Grouping</h5>
  <dl class="row small">
    <dt class="col-sm-3">Now (3 bulan)</dt>
    <dd class="col-sm-9">Pure Quick-Win — momentum cepat, low risk, prerequisite untuk Big-Bet (mis. R-02 QR siap sebelum R-10 Open Badges).</dd>
    <dt class="col-sm-3">Next (3-9 bulan)</dt>
    <dd class="col-sm-9">Mid-effort infrastructure (Hangfire baseline R-03, Audit Log G-10 compliance, Soft Delete G-14 prerequisite). Foundation untuk fase Later.</dd>
    <dt class="col-sm-3">Later (9-18 bulan)</dt>
    <dd class="col-sm-9">High-effort transformational (Open Badges interoperability, Skill Matrix taxonomy, External Assessor compliance ISO 17024). Memerlukan baseline dari fase Next.</dd>
  </dl>

  <div class="alert alert-secondary mt-3">
    <strong>Catatan:</strong> Roadmap ini tentative — sequencing final harus validate dengan capacity planning team IT + stakeholder review. Quarterly checkpoint disarankan untuk re-prioritisasi berdasarkan business need shift.
  </div>
</section>
```

- [ ] **Step 2: Browser verify Mermaid timeline**

Refresh. Expected: timeline chart 3 fase tampil, item per fase listed. Console: no Mermaid error.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §9 Roadmap Tentative — Mermaid timeline 3 fase (Now/Next/Later) + rationale"
```

---

## Task 13: §10 Sources & References

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Konsolidasi semua URL dari Task 6/7/8 + add §10 section**

Konsolidasi URL terkumpul dari WebSearch sebelumnya. Insert setelah `</section>` §9:

```html
<section id="sec-10">
  <h2><span class="badge bg-secondary">§10</span> Sources &amp; References</h2>

  <h5 class="mt-3">10.1 External Sources (WebSearch 2026-05-26)</h5>
  <ol class="small">
    <li><a href="https://www.shell.com/sustainability/our-people/skills-development.html">Shell Skills Development — Sustainability Report</a> (access 2026-05-26)</li>
    <li><a href="https://www.chevron.com/sustainability/people">Chevron People & Communities — Workforce Development</a> (access 2026-05-26)</li>
    <li><a href="https://corporate.exxonmobil.com/operations">ExxonMobil Operations Integrity Management System (OIMS)</a> (access 2026-05-26)</li>
    <li><a href="https://www.workday.com/en-us/products/talent-management/learning.html">Workday Learning Product Page</a> (access 2026-05-26)</li>
    <li><a href="https://www.sap.com/products/hcm/successfactors-learning.html">SAP SuccessFactors Learning</a> (access 2026-05-26)</li>
    <li><a href="https://www.cornerstoneondemand.com/products/learning/skills-cloud">Cornerstone Skills Cloud</a> (access 2026-05-26)</li>
    <li><a href="https://docs.moodle.org/en/Badges">Moodle Badges Documentation</a> (access 2026-05-26)</li>
    <li><a href="https://www.iso.org/standard/52993.html">ISO/IEC 17024:2012 — General requirements for bodies operating certification of persons</a> (access 2026-05-26)</li>
    <li><a href="https://www.iacet.org/standards/ansi-iacet-standard-for-continuing-education-and-training/">ANSI/IACET 1-2018 Standard CEU</a> (access 2026-05-26)</li>
    <li><a href="https://www.imsglobal.org/spec/ob/v3p0">Open Badges 3.0 Specification (1EdTech)</a> (access 2026-05-26)</li>
    <li><a href="https://www.w3.org/TR/vc-data-model-2.0/">W3C Verifiable Credentials Data Model 2.0</a> (access 2026-05-26)</li>
    <li><a href="https://www.accredible.com/">Accredible Digital Credential Platform</a> (access 2026-05-26)</li>
    <li><a href="https://info.credly.com/">Credly Acclaim Digital Credential Platform</a> (access 2026-05-26)</li>
    <li><a href="https://bnsp.go.id/">BNSP — Badan Nasional Sertifikasi Profesi</a> (access 2026-05-26)</li>
    <li><a href="https://esdm.go.id/">Kementerian ESDM Republik Indonesia</a> (access 2026-05-26)</li>
  </ol>

  <h5 class="mt-4">10.2 Internal Cross-Reference</h5>
  <ul class="small">
    <li><a href="index.html#sec-10">index.html §10 — Gap Analysis (sumber G-01..G-25)</a></li>
    <li><a href="index.html#sec-11">index.html §11 — Spec Cross-Check (existing claim audit)</a></li>
    <li><a href="index.html#sec-12">index.html §12 — Full Glossary 16 term</a></li>
    <li><a href="bug-findings.html">bug-findings.html — 16 bug (12 Portal P01-P12 + 4 Doc D01-D04)</a></li>
    <li>Spec laporan ini: <code>docs/superpowers/specs/2026-05-26-analisa-gap-benchmark-design.md</code></li>
    <li>Plan implementasi: <code>docs/superpowers/plans/2026-05-26-analisa-gap-benchmark.md</code></li>
  </ul>

  <h5 class="mt-4">10.3 Acknowledgment</h5>
  <p class="small">Laporan disusun via Claude Opus 4.7 caveman session (brainstorming → writing-plans → subagent-driven execution). Citation URL diverifikasi tanggal akses; konten internal review per stakeholder Pertamina KPB sebelum publikasi resmi.</p>
</section>
```

- [ ] **Step 2: Browser verify**

Refresh. Expected: §10 muncul, 15 URL external + cross-ref internal listed.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): §10 Sources & References — 15 WebSearch URL + internal cross-ref"
```

---

## Task 14: Print stylesheet + final polish

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Add @media print CSS + footer**

Locate the existing `<style>` block (Task 1). Tambah sebelum closing `</style>`:

```css
@media print {
  #sidebar-toc, #theme-toggle { display: none !important; }
  main { padding: 1rem !important; max-width: 100% !important; }
  .col-lg-9, .col-md-8 { width: 100% !important; flex: 0 0 100% !important; max-width: 100% !important; }
  section { page-break-before: always; border-bottom: none; }
  section#sec-0 { page-break-before: auto; }
  .accordion-collapse { display: block !important; }
  .accordion-button { display: none; }
  .accordion-body { padding: 0.5rem 0; border: none; }
  .card { page-break-inside: avoid; }
  .mermaid { page-break-inside: avoid; }
  table { page-break-inside: avoid; }
  a { color: black; text-decoration: underline; }
  h2 { page-break-after: avoid; }
}
```

Tambah footer sebelum closing `</main>`:

```html
<footer class="text-center text-muted small py-4 mt-5 border-top">
  <p class="mb-1"><strong>Analisa Gap + Benchmark</strong> · Sertifikat Ecosystem Portal HC KPB · v1.0</p>
  <p class="mb-1">Last updated: 2026-05-26 · Generated via Claude Opus 4.7 (caveman session)</p>
  <p class="mb-0">
    <a href="index.html">← Index</a> ·
    <a href="bug-findings.html">Bug Findings</a> ·
    <a href="#sec-0">↑ Top</a>
  </p>
</footer>
```

- [ ] **Step 2: Browser print preview verify**

Open browser, press `Ctrl+P` print preview. Expected: sidebar hidden, accordion expanded, page break tiap section, link underlined.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "feat(sertifikat-doc): print stylesheet @media print + footer last-updated"
```

---

## Task 15: Playwright verify + visual QA + final tag

**Files:**
- Reference: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html`

- [ ] **Step 1: Playwright snapshot verify**

Run Playwright untuk snapshot semua 10 section + verify console no error:

```javascript
// In Playwright MCP / browser tools session:
// 1. navigate to file:///.../analisa-gap-benchmark.html
// 2. browser_snapshot full page
// 3. browser_console_messages — verify no error/warning
// 4. browser_evaluate: check semua section[id] (sec-0..sec-10) ada
// 5. browser_evaluate: count rows §3 table = 25, §4 table = 12, §7 table = 37
// 6. browser_evaluate: verify Mermaid SVG rendered di §6 + §9
```

Acceptance:
- 11 section (sec-0 + sec-1..sec-10) all present
- §3 table 25 row, §4 table 12 row, §7 table 37 row
- Mermaid quadrantChart (§6) + timeline (§9) render sebagai SVG
- Console: 0 error
- Theme toggle works (click → data-bs-theme flip)
- Sidebar scroll-spy aktif (scroll → active link highlight)
- All `href` to `index.html#sec-XX` + `bug-findings.html#bug-XX` valid format

- [ ] **Step 2: Browser manual visual check final**

Open file di Chrome + Firefox. Verify:
- Header table tampil
- §1-§10 navigable via sidebar
- Quadrant chart §6 readable (≤20 dot, no overlap parah)
- ICE table §7 top 10 highlighted dengan tint biru
- 10 recommendation card §8 color-coded
- Timeline §9 3 fase tampil
- Footer bottom rendered
- `Ctrl+P` preview: sidebar hidden, accordion expanded

- [ ] **Step 3: Final tag + push**

```bash
git tag -a sertifikat-doc-gap-benchmark-v1.0 -m "analisa-gap-benchmark.html v1.0 — 10 section executive report (gap + benchmark + engineering rec)"
git log --oneline sertifikat-doc-gap-benchmark-v1.0~14..sertifikat-doc-gap-benchmark-v1.0
# Verify 14 task commit + tag
```

- [ ] **Step 4: Update MEMORY.md project entry**

Run via Write tool: tambah entry baru di `memory/MEMORY.md`:

```
- [Analisa Gap Benchmark SHIPPED](project_analisa_gap_benchmark_shipped.md) — docs/sertifikat-ecosystem/analisa-gap-benchmark.html v1.0 10 section (37 area + 3 benchmark + 10 rec) ~2500 baris, tag sertifikat-doc-gap-benchmark-v1.0, pending push origin/main
```

Create memory file `memory/project_analisa_gap_benchmark_shipped.md` dengan detail commit range + scope.

- [ ] **Step 5: Notify user untuk push manual**

Per CLAUDE.md "promosi ke server Dev/Prod = tanggung jawab IT". Untuk dokumen public di repo, push origin/main = user decision. Report commit range + tag, tunggu instruksi.

---

## Self-Review Done

- [x] All 11 sections (sec-0 header + §1-§10) covered by tasks
- [x] No TBD/TODO placeholder in any task step
- [x] Type consistency: G-01..G-25 stable, N-01..N-12 stable, R-01..R-10 stable
- [x] ICE formula consistent: `I × C × E / 100` (§2 def + §7 table compute)
- [x] Quadrant labels consistent: Quick-Win/Big-Bet/Fill-In/Time-Sink (4 places)
- [x] Cross-ref ID match: bug-P01..P12 + D01-D04 (verified from grep)
- [x] Total HTML lines target ~2500 (sum estimates from spec §6)

**Done. Ready for execution.**
