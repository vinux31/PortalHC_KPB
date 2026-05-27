# Flow Sistem Sertifikat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bikin `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — visualisasi end-to-end 25 flow distinct sistem sertifikat Portal HC KPB sekarang untuk audience awam HC.

**Architecture:** Single static HTML file, Bootstrap 5.3 + Bootstrap Icons + Mermaid 11 (semua CDN). 1 master lifecycle diagram + 8 fase section dengan Mermaid diagram + tabel detail per flow. Theme toggle + Mermaid re-render on theme change. Print CSS lengkap (PDF-friendly).

**Tech Stack:** HTML5, Bootstrap 5.3, Bootstrap Icons 1.11, Mermaid 11, vanilla JS. No test framework (visual verify browser only).

**Spec reference:** `docs/superpowers/specs/2026-05-27-flow-sistem-sertifikat-design.md`

**File Structure:**
- **Create**: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` (~700 baris target, maks 900)
- **Modify**: `docs/sertifikat-ecosystem/overview-awam.html` (update §1.4 cross-ref tambah link ke flow-sistem)

**Test approach:** Visual verify browser via Playwright (load via HTTP server, check Mermaid render, theme toggle, mobile responsive). Tidak ada Playwright automation.

---

### Task 1: Scaffold HTML skeleton + Mermaid init + theme toggle + print CSS lengkap

**Files:**
- Create: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`

- [ ] **Step 1: Write skeleton file**

Skeleton harus include:
- `<head>` Bootstrap 5.3 CDN + Bootstrap Icons + Mermaid 11 CDN
- `<style>` body + section + mini-nav + audience-banner + status-card border colors + Mermaid container styling + **print CSS lengkap** (font 10pt, color-adjust:exact, page-break per section, Mermaid svg max-width preserved, accordion expand-if-any, link black underline, header/footer placeholder for @page)
- Sticky mini-nav 9 link (`#sec-master` + `#sec-a` .. `#sec-i`) + theme toggle button
- `<main>` dengan header (h1 + audience banner cross-ref + versi) + 9 section placeholder + footer placeholder
- `<script>` Bootstrap bundle + Mermaid init dengan startOnLoad:true, theme detection (light/dark) + re-render on theme toggle
- Theme toggle script: localStorage `flow-sistem-theme`, update Mermaid config + re-render diagrams saat switch

```html
<!DOCTYPE html>
<html lang="id" data-bs-theme="light">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Flow Sistem Sertifikat — Portal HC KPB</title>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bs-body-bg); }
    main.content { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem 4rem; }
    section { scroll-margin-top: 5rem; padding-bottom: 2.5rem; margin-bottom: 2rem; border-bottom: 1px solid var(--bs-border-color); }
    section:last-of-type { border-bottom: none; }
    h2 { margin-top: 2rem; padding-top: 1rem; }
    h2 .badge { font-size: 0.7em; vertical-align: middle; }
    .mini-nav { position: sticky; top: 0; z-index: 1020; background: var(--bs-body-bg); border-bottom: 1px solid var(--bs-border-color); padding: 0.5rem 1rem; }
    .mini-nav a { color: var(--bs-body-color); text-decoration: none; padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-size: 0.875rem; }
    .mini-nav a:hover { background: var(--bs-secondary-bg); }
    .audience-banner { background: var(--bs-info-bg-subtle); border-left: 4px solid var(--bs-info); padding: 1rem; margin: 1.5rem 0; border-radius: 0.25rem; }
    .mermaid { background: var(--bs-body-bg); padding: 1rem; border-radius: 0.25rem; text-align: center; overflow-x: auto; }
    .mermaid svg { max-width: 100%; height: auto; }
    .gap-callout { background: var(--bs-danger-bg-subtle); border-left: 4px solid var(--bs-danger); padding: 1rem; margin: 1rem 0; border-radius: 0.25rem; }
    .hidden-gap-callout { background: var(--bs-warning-bg-subtle); border-left: 4px solid var(--bs-warning); padding: 1rem; margin: 1rem 0; border-radius: 0.25rem; }
    .flow-id { font-family: 'SFMono-Regular', Consolas, monospace; font-size: 0.75rem; opacity: 0.7; }
    @page { margin: 1.5cm 1.2cm; }
    @media print {
      :root { print-color-adjust: exact; -webkit-print-color-adjust: exact; }
      body { font-size: 10pt; }
      .mini-nav, #theme-toggle { display: none !important; }
      main { padding: 0.5rem !important; max-width: 100% !important; }
      section { page-break-before: always; border-bottom: none; padding-bottom: 0.5rem; margin-bottom: 0.5rem; }
      section#sec-master { page-break-before: auto; }
      .audience-banner, .gap-callout, .hidden-gap-callout { break-inside: avoid; }
      .card { page-break-inside: avoid; }
      table { page-break-inside: auto; }
      tr { page-break-inside: avoid; }
      a { color: black; text-decoration: underline; }
      a[href^="http"]:after { content: " (" attr(href) ")"; font-size: 0.7em; word-break: break-all; }
      a[href^="./"]:after { content: ""; }
      a[href^="#"]:after { content: ""; }
      h2 { page-break-after: avoid; font-size: 14pt; }
      h3, h4, h5 { page-break-after: avoid; }
      .mermaid svg { max-width: 100% !important; height: auto !important; page-break-inside: avoid; }
    }
  </style>
</head>
<body>
  <nav class="mini-nav d-none d-md-flex justify-content-between align-items-center">
    <div class="d-flex flex-wrap gap-1">
      <a href="#sec-master">§0 Master</a>
      <a href="#sec-a">§A Terbit</a>
      <a href="#sec-b">§B Grading</a>
      <a href="#sec-c">§C Status</a>
      <a href="#sec-d">§D Notif</a>
      <a href="#sec-e">§E Renewal</a>
      <a href="#sec-f">§F View</a>
      <a href="#sec-g">§G Manage</a>
      <a href="#sec-i">§H+I Verifikasi+Audit</a>
    </div>
    <button id="theme-toggle" class="btn btn-sm btn-outline-secondary" title="Toggle dark mode">
      <i class="bi bi-moon-stars"></i>
    </button>
  </nav>

  <main class="content">
    <header class="mb-4">
      <h1 class="display-6">Flow Sistem Sertifikat Portal HC KPB</h1>
      <p class="lead text-muted">Visualisasi 25 flow lengkap — referensi awam untuk Tim HC</p>
      <div class="audience-banner">
        <strong><i class="bi bi-info-circle"></i> Dokumen referensi awam untuk Staff HC admin Portal.</strong>
        Tampilkan semua proses end-to-end sistem sertifikat — siapa trigger, kapan, hasil apa.
        Companion: <a href="./overview-awam.html"><code>overview-awam.html</code></a> (ringkasan),
        <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a> (struktur 4-kotak),
        <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a> (gap detail).
      </div>
      <p class="small text-muted">Versi 1.0 — 2026-05-27 — Audit codebase per tanggal ini.</p>
    </header>

    <section id="sec-master"><h2><span class="badge bg-secondary">§0</span> Master Lifecycle Diagram</h2><p class="text-muted"><em>(Diisi Task 2)</em></p></section>
    <section id="sec-a"><h2><span class="badge bg-secondary">§A</span> Terbit / Lahir Sertifikat</h2><p class="text-muted"><em>(Diisi Task 3)</em></p></section>
    <section id="sec-b"><h2><span class="badge bg-secondary">§B</span> Essay Grading Conditional</h2><p class="text-muted"><em>(Diisi Task 4)</em></p></section>
    <section id="sec-c"><h2><span class="badge bg-secondary">§C</span> Lifecycle Status</h2><p class="text-muted"><em>(Diisi Task 5)</em></p></section>
    <section id="sec-d"><h2><span class="badge bg-secondary">§D</span> Notifikasi</h2><p class="text-muted"><em>(Diisi Task 6)</em></p></section>
    <section id="sec-e"><h2><span class="badge bg-secondary">§E</span> Renewal</h2><p class="text-muted"><em>(Diisi Task 7)</em></p></section>
    <section id="sec-f"><h2><span class="badge bg-secondary">§F</span> View / Read</h2><p class="text-muted"><em>(Diisi Task 8)</em></p></section>
    <section id="sec-g"><h2><span class="badge bg-secondary">§G</span> Edit / Manage</h2><p class="text-muted"><em>(Diisi Task 9)</em></p></section>
    <section id="sec-i"><h2><span class="badge bg-secondary">§H+I</span> Verifikasi + Audit</h2><p class="text-muted"><em>(Diisi Task 10)</em></p></section>

    <footer class="text-center text-muted small mt-5 pt-4 border-top">
      <p class="text-muted"><em>(Footer placeholder — diisi Task 11)</em></p>
    </footer>
  </main>

  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  <script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
  <script>
    (function () {
      const html = document.documentElement;
      const btn = document.getElementById('theme-toggle');
      const icon = btn.querySelector('i');
      const stored = localStorage.getItem('flow-sistem-theme');

      function initMermaid(theme) {
        mermaid.initialize({ startOnLoad: false, theme: theme === 'dark' ? 'dark' : 'default', securityLevel: 'loose', flowchart: { useMaxWidth: true, htmlLabels: true } });
      }

      function renderAllMermaid() {
        document.querySelectorAll('.mermaid').forEach((el, i) => {
          if (el.getAttribute('data-processed') === 'true') {
            const src = el.getAttribute('data-source') || el.textContent;
            el.removeAttribute('data-processed');
            el.innerHTML = src;
            el.setAttribute('data-source', src);
          } else {
            el.setAttribute('data-source', el.textContent);
          }
        });
        mermaid.run({ querySelector: '.mermaid' });
      }

      if (stored) {
        html.setAttribute('data-bs-theme', stored);
        icon.className = stored === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
      }
      initMermaid(html.getAttribute('data-bs-theme'));
      renderAllMermaid();

      btn.addEventListener('click', function () {
        const current = html.getAttribute('data-bs-theme');
        const next = current === 'dark' ? 'light' : 'dark';
        html.setAttribute('data-bs-theme', next);
        icon.className = next === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        localStorage.setItem('flow-sistem-theme', next);
        initMermaid(next);
        renderAllMermaid();
      });
    })();
  </script>
</body>
</html>
```

- [ ] **Step 2: Verify browser**

Start HTTP server `python -m http.server 8765 --bind 127.0.0.1` di background, navigate ke `http://127.0.0.1:8765/docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`. Expected:
- Mini-nav sticky 9 link + theme toggle
- Header + audience banner + 3 link companion
- 9 section placeholder visible
- Theme toggle switch dark/light persist

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): scaffold flow-sistem-sertifikat.html — skeleton + Mermaid CDN + print CSS lengkap"
```

---

### Task 2: §0 Master Lifecycle Diagram

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §0 placeholder

- [ ] **Step 1: Replace §0 placeholder dengan intro + Mermaid master**

Master diagram = overview 8 fase as connected zones, no detail individual flow. Tampilkan lifecycle siklik dari Terbit → Status → Notif → Renewal kembali ke Terbit, dengan Grading sebagai conditional branch, View sebagai cross-cutting, Manage sebagai admin action, Audit + Verifikasi sebagai compliance layer.

```html
    <section id="sec-master">
      <h2><span class="badge bg-secondary">§0</span> Master Lifecycle Diagram</h2>
      <p>Diagram berikut overview <strong>8 fase lifecycle sertifikat</strong> di Portal HC KPB. Click section per fase di bawah untuk dive ke detail flow per fase.</p>
      <div class="mermaid">
flowchart TB
    subgraph A["A. TERBIT (6 flow)"]
      A1[Assessment Online]
      A2[HC Manual Upload]
      A3[Bulk Import Excel]
    end
    subgraph B["B. ESSAY GRADING (3 flow)"]
      B1[PendingGrading]
      B2[Submit + Finalize]
    end
    subgraph C["C. STATUS (2 flow)"]
      C1[Aktif → AkanExpired → Expired]
      C2[Permanent]
    end
    subgraph D["D. NOTIFIKASI (1 flow)"]
      D1[On-Login Passive Trigger]
    end
    subgraph E["E. RENEWAL (3 + 1 gap)"]
      E1[HC-Trigger]
      E2[Chain Link FK]
    end
    subgraph F["F. VIEW (3 flow + RBAC)"]
      F1[Coachee/HC/Manager view]
    end
    subgraph G["G. EDIT / MANAGE (4 flow)"]
      G1[Edit/Delete/Export/Re-issue]
    end
    subgraph HI["H+I. VERIFIKASI + AUDIT (1 gap + 1 flow)"]
      H1[Internal log]
      H2[External verify GAP]
    end

    A --> B
    A --> C
    B --> C
    C --> D
    D --> E
    E --> A
    F -.lihat.-> A
    F -.lihat.-> C
    F -.lihat.-> E
    G -.kelola.-> A
    G -.kelola.-> C
    HI -.compliance.-> A
    HI -.compliance.-> C
    HI -.compliance.-> E

    style HI fill:#fee,stroke:#c00
      </div>
      <p class="small text-muted"><i class="bi bi-info-circle"></i> Solid arrow = flow utama. Dotted arrow = cross-cutting (RBAC view, admin manage, compliance).</p>
    </section>
```

- [ ] **Step 2: Verify browser** — refresh, Mermaid render master diagram dengan 8 subgraph + arrow connection.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §0 Master Lifecycle Diagram flow-sistem"
```

---

### Task 3: §A Terbit (6 flow) — diagram + tabel

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §A placeholder

- [ ] **Step 1: Replace §A placeholder**

```html
    <section id="sec-a">
      <h2><span class="badge bg-secondary">§A</span> Terbit / Lahir Sertifikat</h2>
      <p>Sertifikat lahir dari <strong>3 jalur input</strong> ditambah branching khusus (pre-post test sibling, NOT-terbit). Total <strong>6 flow distinct</strong> di fase ini.</p>
      <div class="mermaid">
flowchart TB
    Start([Pekerja butuh sertifikat])
    Start --> J1{Jalur input?}

    J1 --> |Assessment online| AO[Coachee jalani Assessment]
    AO --> AOL{Lulus + Flag<br/>Generate Cert ON?}
    AOL -- "Ya, no essay" --> AOC[Sertifikat OTOMATIS terbit<br/>Format KPB/SEQ/ROMAWI/YYYY]
    AOL -- "Ya, ada essay" --> AOE[Status PendingGrading<br/>→ Fase B Essay Grading]
    AOL -- "Tidak lulus" --> XF[NOT terbit — Failed]
    AOL -- "Flag OFF" --> XO[NOT terbit — Flag OFF]

    J1 --> |HC Manual Upload| HU[HC AddTraining<br/>EditTraining endpoint]
    HU --> HUI[Input nomor sertifikat manual<br/>+ upload bukti PDF]
    HU --> HMA[HC AddManualAssessment<br/>EditManualAssessment]
    HMA --> HMAI[Input score assessment external<br/>+ upload bukti PDF]

    J1 --> |Bulk Import| BI[HC ImportTraining]
    BI --> BIE[Upload Excel batch<br/>multi-pekerja sekaligus]

    PP[Pre-Post Test Group]
    PP --> PPS[Pre + Post coupled<br/>flag GenerateCertificate override]

    AOC --> Done([Sertifikat di database])
    HUI --> Done
    HMAI --> Done
    BIE --> Done
    PPS --> Done

    style AOC fill:#d4edda
    style XF fill:#f8d7da
    style XO fill:#fff3cd
      </div>
      <h5 class="mt-4">Detail 6 Flow Fase Terbit</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Trigger / Endpoint</th><th>Hasil</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>Assessment Online → Sertifikat Otomatis</strong></td><td>Coachee + Sistem</td><td>Lulus assessment + flag GenerateCertificate ON + no essay pending → <code>GradingService.GradeAndCompleteAsync</code></td><td>Sertifikat auto-generate dengan nomor format <code>KPB/SEQ/ROMAWI/YYYY</code></td></tr>
            <tr><td>2</td><td><strong>HC Upload Bukti Training External</strong></td><td>HC/Admin</td><td>Menu Admin → AddTraining / EditTraining</td><td>Sertifikat manual entry, nomor input manual, file PDF tersimpan</td></tr>
            <tr><td>3</td><td><strong>HC Manual Assessment Entry</strong></td><td>HC/Admin</td><td>Menu Admin → AddManualAssessment / EditManualAssessment</td><td>Assessment external tercatat di sistem + bukti PDF</td></tr>
            <tr><td>4</td><td><strong>Training Record Bulk Import Excel</strong></td><td>HC/Admin</td><td>Menu Admin → ImportTraining (upload .xlsx)</td><td>Batch import multi-pekerja sekaligus</td></tr>
            <tr><td>5</td><td><strong>Pre-Post Test Sibling Link</strong></td><td>Sistem</td><td>Saat assessment group pre/post → coupling otomatis</td><td>Flag GenerateCertificate override sesuai logic sibling</td></tr>
            <tr><td>6</td><td><strong>Assessment → NOT-Terbit (3 alasan)</strong></td><td>Sistem (branch)</td><td>(a) Gagal lulus, (b) Flag OFF, (c) Pending essay grading</td><td>Sertifikat tidak terbit — alasan tertampil di UI</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render branching multi-jalur, tabel 6 baris.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §A Terbit 6 flow + Mermaid branching flow-sistem"
```

---

### Task 4: §B Essay Grading (3 flow)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §B placeholder

- [ ] **Step 1: Replace §B placeholder**

```html
    <section id="sec-b">
      <h2><span class="badge bg-secondary">§B</span> Essay Grading Conditional</h2>
      <p>Kalau assessment ada soal essay, sertifikat <strong>belum terbit</strong> sebelum HC selesai grading. 2-step proses: <strong>Submit per essay → Finalize</strong>.</p>
      <div class="mermaid">
flowchart LR
    Start([Coachee selesai assessment]) --> CK{Ada soal essay?}
    CK -- Tidak --> Direct[Cert otomatis<br/>lihat §A flow #1]
    CK -- Ya --> PG[Status: PendingGrading]
    PG --> HC[HC buka grading queue]
    HC --> Sub[SubmitEssayScore<br/>per essay]
    Sub --> More{Essay lain<br/>belum dinilai?}
    More -- Ya --> Sub
    More -- Tidak --> Fin[FinalizeEssayGrading]
    Fin --> Eval{IsPassed = true?}
    Eval -- Ya --> Cert[Sertifikat terbit<br/>GradingService.RegradeAfterEditAsync]
    Eval -- Tidak --> NoCert[Tidak terbit — Failed]

    style PG fill:#fff3cd
    style Cert fill:#d4edda
    style NoCert fill:#f8d7da
      </div>
      <h5 class="mt-4">Detail 3 Flow Fase Grading</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Trigger / Endpoint</th><th>Hasil</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>Assessment Interim Status: PendingGrading</strong></td><td>Sistem</td><td>Auto saat assessment selesai dengan essay yang belum dinilai</td><td>Status sertifikat tertunda, masuk antrian grading HC</td></tr>
            <tr><td>2</td><td><strong>HC Submit Essay Score (per essay)</strong></td><td>HC/Admin</td><td>Endpoint <code>SubmitEssayScore</code> via Admin grading queue</td><td>Skor essay tersimpan, status tetap PendingGrading sampai semua essay dinilai</td></tr>
            <tr><td>3</td><td><strong>HC Finalize Essay Grading</strong></td><td>HC/Admin</td><td>Endpoint <code>FinalizeEssayGrading</code> (rowsAffected-gated audit log)</td><td>Trigger cert generation kalau IsPassed=true (via GradingService.RegradeAfterEditAsync)</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render lifecycle Submit → Finalize branching IsPassed.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §B Essay Grading 3 flow + Mermaid 2-step flow-sistem"
```

---

### Task 5: §C Lifecycle Status (2 flow) — state diagram

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §C placeholder

- [ ] **Step 1: Replace §C placeholder**

```html
    <section id="sec-c">
      <h2><span class="badge bg-secondary">§C</span> Lifecycle Status</h2>
      <p>Status sertifikat <strong>dihitung sistem real-time</strong>, bukan disimpan di database. Logic di <code>DeriveCertificateStatus</code> berdasarkan <code>ValidUntil</code> + jenis (Permanent atau bukan).</p>
      <div class="mermaid">
stateDiagram-v2
    [*] --> Aktif: Sertifikat terbit
    Aktif --> AkanExpired: ≤30 hari ke ValidUntil
    AkanExpired --> Expired: Tanggal ValidUntil lewat
    AkanExpired --> Aktif: Renewal sukses<br/>(chain link ke baru)
    Expired --> Aktif: Renewal sukses<br/>(chain link ke baru)
    Aktif --> Aktif: Edit data sertifikat
    [*] --> Permanent: Tag jenis Permanent
    Permanent --> Permanent: No transition
      </div>
      <div class="alert alert-light border mt-3">
        <strong><i class="bi bi-info-circle"></i> Catatan teknis ringan:</strong> Status <strong>bukan kolom database</strong> — di-derive real-time setiap halaman dibuka. Implikasi: tidak ada cron job yang "menggeser status" — pure formula. Performa hit minimal karena cuma cek timestamp.
      </div>
      <h5 class="mt-4">Detail 2 Flow Fase Status</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Trigger / Logic</th><th>Hasil</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>Aktif → AkanExpired → Expired</strong> (auto real-time)</td><td>Sistem</td><td><code>DeriveCertificateStatus(ValidUntil, jenis)</code> dihitung tiap render</td><td>Status update tanpa intervensi manual</td></tr>
            <tr><td>2</td><td><strong>Permanent</strong> (no transition)</td><td>Sistem</td><td>Tag jenis sertifikat Permanent saat input</td><td>Tidak pernah expired, tidak masuk daftar outstanding renewal</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid state-diagram render Aktif/AkanExpired/Expired/Permanent transitions.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §C Lifecycle Status 2 flow + Mermaid state-diagram flow-sistem"
```

---

### Task 6: §D Notifikasi (1 flow + hidden gap callout)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §D placeholder

- [ ] **Step 1: Replace §D placeholder dengan flow + hidden gap warning**

```html
    <section id="sec-d">
      <h2><span class="badge bg-secondary">§D</span> Notifikasi</h2>
      <p>Notif expiry sertifikat <strong>bukan cron job</strong> — di-trigger saat user login lewat <code>HomeController.TriggerCertExpiredNotificationsAsync</code> dengan cache 1-jam + dedup hash. <strong>Implikasi penting:</strong> kalau tidak ada user login dalam window kritis, notif tidak terkirim.</p>
      <div class="mermaid">
flowchart TB
    Login[User login Portal HC] --> Home[HomeController.Index]
    Home --> Cache{Cache 1-jam<br/>masih valid?}
    Cache -- Ya --> Skip[Skip — tidak fire notif]
    Cache -- Tidak --> Trigger[TriggerCertExpiredNotificationsAsync]
    Trigger --> Scan[Scan sertifikat<br/>ValidUntil ≤ 30 hari]
    Scan --> Dedup{Sudah pernah<br/>kirim notif?}
    Dedup -- Ya --> Skip2[Skip — dedup via hash]
    Dedup -- Tidak --> Send[NotificationService.SendAsync<br/>type CERT_EXPIRED]
    Send --> Recv[Notif diterima<br/>HC + Admin via email + in-app]

    NoLogin[Tidak ada user login<br/>dalam window kritis] -.-> Miss[Notif TIDAK fire!<br/>⚠ Hidden gap operasional]

    style Miss fill:#f8d7da,stroke:#c00
    style NoLogin fill:#fff3cd
      </div>
      <div class="hidden-gap-callout">
        <strong><i class="bi bi-exclamation-triangle-fill text-warning"></i> Hidden Gap Operasional</strong>
        <p class="mb-1 mt-2">Sistem notif sekarang <strong>passive on-login</strong> — kalau tidak ada user login dalam jam kritis (mis. weekend / cuti panjang), notif expiry tidak terkirim. Sertifikat bisa lewat expired tanpa pernah ada peringatan.</p>
        <p class="mb-0"><strong>Rekomendasi:</strong> tambah dedicated scheduler (Hangfire / IHostedService background) untuk decouple notif trigger dari user activity. Lihat <a href="./analisa-gap-benchmark.html#sec-3"><code>analisa-gap-benchmark.html §3 — Gap #2 Multi-channel notif + scheduler</code></a>.</p>
      </div>
      <h5 class="mt-4">Detail 1 Flow Fase Notifikasi</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Trigger / Mekanisme</th><th>Channel</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>Cert Expiry Notif — On-Login Passive Trigger</strong></td><td>Sistem (passive saat user login)</td><td><code>HomeController.TriggerCertExpiredNotificationsAsync</code> dengan cache 1-jam + dedup hash</td><td>Email + in-app, hanya HC + Admin</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render + hidden-gap callout warna kuning warning visible.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §D Notifikasi 1 flow + hidden gap on-login warning flow-sistem"
```

---

### Task 7: §E Renewal (3 flow + 1 gap)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §E placeholder

- [ ] **Step 1: Replace §E placeholder**

```html
    <section id="sec-e">
      <h2><span class="badge bg-secondary">§E</span> Renewal</h2>
      <p>Renewal saat ini <strong>HC-driven only</strong>. Self-service coachee belum ada (gap top kritis #1 di <code>analisa-gap-benchmark</code>). Renewal chain di-link via FK <code>RenewsSessionId</code> / <code>RenewsTrainingId</code>, di-resolve via Union-Find algorithm di <code>CertificateHistory</code> modal.</p>
      <div class="mermaid">
flowchart TB
    Notif[Notif expiry / HC proaktif] --> Menu[HC buka menu<br/>Admin / RenewalCertificate]
    Menu --> Filter[FilterRenewalCertificate<br/>FilterRenewalCertificateGroup<br/>group by judul]
    Filter --> Pick[HC pilih sertifikat<br/>per-record klik]
    Pick --> Form[Form Renewal Certificate]
    Form --> Trigger[Trigger Training/Assessment Baru]
    Trigger --> NewCert[Sertifikat Baru terbit<br/>lihat §A]
    NewCert --> ChainFK[Set FK RenewsSessionId<br/>atau RenewsTrainingId<br/>link ke sertifikat lama]
    ChainFK --> Hide[Sertifikat lama auto-hide<br/>dari dashboard outstanding]
    Hide --> History[Bisa di-view di<br/>CertificateHistory modal<br/>Union-Find resolution chain]

    Gap[Self-service Coachee Renewal] -.BELUM ADA.-> NoSelf[Coachee hanya bisa menunggu HC]

    style Gap fill:#fee,stroke:#c00,stroke-dasharray: 5 5
    style NoSelf fill:#f8d7da
      </div>
      <div class="gap-callout">
        <strong><i class="bi bi-x-circle-fill text-danger"></i> Gap Top Kritis #1 — Self-Service Renewal</strong>
        <p class="mb-0 mt-2">Pekerja yang sertifikatnya mau expired hanya bisa menunggu HC bertindak. Tidak ada cara coachee trigger renewal sendiri. Beban HC tinggi, pekerja pasif. Lihat <a href="./analisa-gap-benchmark.html#sec-4"><code>analisa-gap-benchmark.html §4 Top-5 Gap Kritis #1</code></a>.</p>
      </div>
      <h5 class="mt-4">Detail 4 Flow Fase Renewal (3 ada + 1 gap)</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Trigger / Endpoint</th><th>Status</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>HC-Trigger Renewal Certificate</strong></td><td>HC/Admin</td><td><code>/Admin/RenewalCertificate</code> + Filter + Group by judul</td><td>✓ Ada</td></tr>
            <tr><td>2</td><td><strong>Renewal Chain Link</strong></td><td>Sistem</td><td>Auto-set FK <code>RenewsSessionId</code> / <code>RenewsTrainingId</code> saat sertifikat baru terbit dari renewal</td><td>✓ Ada</td></tr>
            <tr><td>3</td><td><strong>Certificate History Modal</strong></td><td>HC/Admin (view)</td><td><code>/Admin/CertificateHistory</code> dengan Union-Find chain resolution</td><td>✓ Ada</td></tr>
            <tr><td>4</td><td><strong>~~Self-Service Coachee Renewal~~</strong></td><td>— (gap)</td><td>—</td><td>❌ <strong>BELUM ADA</strong> (Gap #1 kritis)</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render + gap callout merah visible.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §E Renewal 3 flow + 1 gap callout flow-sistem"
```

---

### Task 8: §F View / Read (3 flow + RBAC swim-lane)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §F placeholder

- [ ] **Step 1: Replace §F placeholder**

```html
    <section id="sec-f">
      <h2><span class="badge bg-secondary">§F</span> View / Read</h2>
      <p>View flow dipengaruhi <strong>role-tier scoping L1-L5</strong>. L5 coachee lihat data sendiri, L4+ HC/Manager/Admin lihat aggregate per Bagian/Unit via <code>BuildSertifikatRowsAsync</code>.</p>
      <div class="mermaid">
flowchart TB
    subgraph Coachee["👤 Coachee (L5)"]
      C1[CMP/Records<br/>list sertifikat sendiri]
      C2[CMP/Certificate<br/>view detail + download PDF]
      C3[RecordsWorkerDetail<br/>own data only]
    end
    subgraph HC["🔧 HC/Admin (L1-L2)"]
      H1[CDP/CertificationManagement<br/>dashboard aggregate]
      H2[FilterCertificationManagement<br/>filter by Bagian/Unit/status]
      H3[CertificatePdf<br/>download per coachee]
    end
    subgraph Manager["👔 Manager/SectionHead (L3-L4)"]
      M1[CDP/CertificationManagement<br/>scope Bagian/Unit anggota tim]
      M2[Records<br/>tim section saja]
    end

    RBAC[Role-Tier Scoping<br/>BuildSertifikatRowsAsync<br/>L1=Admin all, L2=HC all,<br/>L3-L4=Bagian/Unit scope,<br/>L5=own data only]

    RBAC --> Coachee
    RBAC --> HC
    RBAC --> Manager
      </div>
      <div class="alert alert-info">
        <strong><i class="bi bi-shield-lock"></i> RBAC Tier (Role-Based Access Control):</strong>
        <ul class="mb-0 small mt-2">
          <li><strong>L1 Admin</strong> — full access semua data</li>
          <li><strong>L2 HC</strong> — full access data sertifikat semua coachee</li>
          <li><strong>L3 Manager / L4 SectionHead</strong> — scope ke Bagian/Unit tim sendiri</li>
          <li><strong>L5 Coachee / Coach</strong> — hanya data sendiri</li>
        </ul>
      </div>
      <h5 class="mt-4">Detail 3 Flow Fase View</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Endpoint</th><th>Scope</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>Certificate View + Download PDF</strong></td><td>Coachee / HC / Admin</td><td><code>CMP/Certificate</code> + <code>CMP/CertificatePdf</code></td><td>Per-record</td></tr>
            <tr><td>2</td><td><strong>Coachee Records Unified View</strong></td><td>Coachee + HC</td><td><code>CMP/Records</code> + <code>CMP/RecordsWorkerDetail</code> (merge Assessment + Training via <code>WorkerDataService</code>)</td><td>L5: own only, L4+: aggregate</td></tr>
            <tr><td>3</td><td><strong>HC Dashboard CDP CertificationManagement</strong></td><td>HC / Manager / Admin</td><td><code>/CDP/CertificationManagement</code> + <code>FilterCertificationManagement</code></td><td>L3-L4: Bagian/Unit, L1-L2: all</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render 3 subgraph + RBAC node connection.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §F View 3 flow + RBAC swim-lane flow-sistem"
```

---

### Task 9: §G Edit / Manage (4 flow)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §G placeholder

- [ ] **Step 1: Replace §G placeholder**

```html
    <section id="sec-g">
      <h2><span class="badge bg-secondary">§G</span> Edit / Manage</h2>
      <p>HC kelola data sertifikat: 4 aksi utama — Edit, Delete cascade, Export, Re-issue PDF.</p>
      <div class="mermaid">
flowchart LR
    HC([HC/Admin]) --> Pick{Action?}
    Pick --> E1[Edit data sertifikat<br/>EditTraining / EditAssessment]
    Pick --> D1[Delete Training<br/>simple delete + file cleanup]
    Pick --> D2[Delete Assessment<br/>EXPLICIT cascade<br/>EditLog + Responses<br/>+ Packages + Options]
    Pick --> EX[Export Excel<br/>CDP/ExportSertifikatExcel<br/>CMP/ExportRecords]
    Pick --> RI[Re-issue PDF<br/>regenerate tanpa<br/>ubah data]

    D2 --> CertFK[Sertifikat FK<br/>ON DELETE SET NULL<br/>renewal chain preserved]
    D1 --> File[File PDF<br/>di filesystem dihapus]

    style D2 fill:#fff3cd
    style CertFK fill:#d4edda
      </div>
      <h5 class="mt-4">Detail 4 Flow Fase Manage</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Endpoint</th><th>Side Effect</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>HC Edit Data Sertifikat</strong></td><td>HC/Admin</td><td><code>EditTraining</code> / <code>EditAssessment</code></td><td>Edit nomor/tanggal/link PDF (gap: tidak ada audit trail per-record)</td></tr>
            <tr><td>2</td><td><strong>Hard Delete + Cascade</strong></td><td>HC/Admin</td><td><code>DeleteTraining</code> (simple + file cleanup) / <code>DeleteAssessment</code> (explicit cascade EditLog + Responses + Packages + Options + status guard)</td><td>Sertifikat FK <code>ON DELETE SET NULL</code> → renewal chain preserved (Phase 323 cascade hardening)</td></tr>
            <tr><td>3</td><td><strong>Export Excel</strong></td><td>HC/Admin</td><td><code>/CDP/ExportSertifikatExcel</code>, <code>/CMP/ExportRecords</code></td><td>Excel per-record/filter (gap: tidak ada bulk export advance + rate limit)</td></tr>
            <tr><td>4</td><td><strong>Re-issue PDF Sertifikat</strong></td><td>HC/Admin</td><td>Regenerate PDF dari template tanpa ubah data DB</td><td>File PDF baru overwrite lama</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — Mermaid render 4 branch action + cascade detail.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §G Edit/Manage 4 flow + cascade detail flow-sistem"
```

---

### Task 10: §H+I Verifikasi + Audit (1 gap + 1 flow)

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace §H+I placeholder

- [ ] **Step 1: Replace §H+I placeholder**

```html
    <section id="sec-i">
      <h2><span class="badge bg-secondary">§H+I</span> Verifikasi + Audit / Compliance</h2>
      <p>Fase compliance saat ini <strong>minimal</strong>. External verifikasi belum ada (gap top kritis #2). Audit log hanya per-action sebagian + file log umum aplikasi (gap top kritis #5).</p>
      <div class="row g-3 my-3">
        <div class="col-md-6">
          <div class="gap-callout h-100">
            <strong><i class="bi bi-x-circle-fill text-danger"></i> Gap Top Kritis #2 — Public Verify QR/URL</strong>
            <p class="mb-0 mt-2 small">Sertifikat PDF punya nomor unik tapi <strong>tidak ada cara external</strong> (rekruter, vendor, auditor BPK) verify keaslian. Mereka harus phone/email HC manual.</p>
            <p class="mb-0 mt-2 small"><a href="./analisa-gap-benchmark.html#sec-4"><i class="bi bi-arrow-right-circle"></i> Detail rekomendasi (QR code + public verify endpoint)</a></p>
          </div>
        </div>
        <div class="col-md-6">
          <div class="gap-callout h-100">
            <strong><i class="bi bi-x-circle-fill text-danger"></i> Gap Top Kritis #5 — Audit Trail Defensible</strong>
            <p class="mb-0 mt-2 small">Aksi di Portal HC ter-log umum di file log aplikasi, tapi <strong>tidak ada audit trail per-sertifikat</strong> yang siap diserahkan ke regulator (BPK, audit internal Pertamina). Tidak ada record "siapa edit, kapan, dari IP mana".</p>
            <p class="mb-0 mt-2 small"><a href="./analisa-gap-benchmark.html#sec-4"><i class="bi bi-arrow-right-circle"></i> Detail rekomendasi (tabel audit log dedicated)</a></p>
          </div>
        </div>
      </div>
      <h5 class="mt-4">Detail 2 Flow Fase Verifikasi + Audit</h5>
      <div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle">
          <thead class="table-dark"><tr><th>#</th><th>Nama Flow</th><th>Aktor</th><th>Mekanisme</th><th>Status</th></tr></thead>
          <tbody>
            <tr><td>1</td><td><strong>~~External Public Verify QR/URL~~</strong></td><td>— (gap)</td><td>—</td><td>❌ <strong>BELUM ADA</strong> (Gap #2 kritis)</td></tr>
            <tr><td>2</td><td><strong>File Log Umum + Per-Action Log</strong></td><td>Sistem</td><td>Application log general + per-action: <code>FinalizeEssayGrading</code> (rowsAffected-gated, Phase 310 D-07), <code>DeleteAssessment</code> / <code>DeleteTraining</code></td><td>⚠ Sebagian (gap audit trail defensible per-record)</td></tr>
          </tbody>
        </table>
      </div>
    </section>
```

- [ ] **Step 2: Verify browser** — 2 gap callout merah parallel + tabel 2 baris.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §H+I Verifikasi+Audit 2 gap callout + 1 flow log flow-sistem"
```

---

### Task 11: Footer + cross-ref + final verify browser

**Files:**
- Modify: `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` — replace footer placeholder

- [ ] **Step 1: Replace footer placeholder**

```html
      <p class="mb-1">Flow Sistem Sertifikat — Portal HC KPB · Versi 1.0 · 2026-05-27</p>
      <p class="mb-2">
        <strong>Audit codebase:</strong> 25 flow distinct (22 ada + 3 gap kritis). Sumber: caveman-investigator agent 2026-05-27.
      </p>
      <p class="mb-0">
        Companion docs:
        <a href="./overview-awam.html"><code>overview-awam.html</code></a> (ringkasan) ·
        <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a> (struktur 4-kotak) ·
        <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a> (50 gap roadmap) ·
        <a href="./index.html"><code>index.html</code></a> (versi teknis dev)
      </p>
```

- [ ] **Step 2: Final verify browser end-to-end**

Start HTTP server. Navigate ke `http://127.0.0.1:8765/docs/sertifikat-ecosystem/flow-sistem-sertifikat.html`. Cek:
- Mini-nav 9 link smooth-scroll ke setiap section
- §0 master diagram render Mermaid 8 subgraph
- §A diagram branching + tabel 6 baris
- §B diagram 2-step + tabel 3 baris
- §C state-diagram + tabel 2 baris
- §D diagram on-login + hidden-gap-callout kuning
- §E diagram chain + gap-callout merah
- §F diagram swim-lane 3 lane + RBAC info alert
- §G diagram 4-branch + tabel cascade detail
- §H+I 2 gap-callout parallel + tabel 2 baris
- Footer 4 link companion
- Theme toggle switch dark↔light + Mermaid re-render dengan theme baru
- Mobile responsive ≤768px (mini-nav hidden, Mermaid overflow-x scroll)

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/flow-sistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis footer cross-ref flow-sistem — file v1.0 complete"
```

---

### Task 12: Cross-ref update di overview-awam.html

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — update §1.4 footer link existing tambah link ke flow-sistem

- [ ] **Step 1: Locate §1.4 closing link paragraph**

```bash
grep -n "Detail lengkap (alur Mermaid" docs/sertifikat-ecosystem/overview-awam.html
```

- [ ] **Step 2: Replace paragraf cross-ref existing**

Cari paragraf existing:
```html
      <p class="small text-muted"><i class="bi bi-arrow-right-circle"></i> Detail lengkap (alur Mermaid + tabel RBAC 6-peran + glosarium 15 istilah) → <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a></p>
```

Replace dengan:
```html
      <p class="small text-muted"><i class="bi bi-arrow-right-circle"></i> Detail lengkap struktur (alur Mermaid + tabel RBAC 6-peran + glosarium 15 istilah) → <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a>. Visualisasi 25 flow end-to-end → <a href="./flow-sistem-sertifikat.html"><code>flow-sistem-sertifikat.html</code></a>.</p>
```

- [ ] **Step 3: Verify browser** — open overview-awam.html, link baru ke flow-sistem muncul di akhir §1.4.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): cross-ref overview-awam §1.4 → flow-sistem-sertifikat"
```

---

## Self-Review

**Spec coverage check (vs `2026-05-27-flow-sistem-sertifikat-design.md`):**
- §3 Header + audience banner + cross-ref 3 file → Task 1
- §3 Mini-nav 9 link + theme toggle → Task 1
- §3 §0 Master Lifecycle Diagram → Task 2
- §3 §A Terbit 6 flow + branching Mermaid → Task 3
- §3 §B Essay Grading 3 flow + 2-step Mermaid → Task 4
- §3 §C Lifecycle Status 2 flow + state-diagram → Task 5
- §3 §D Notifikasi 1 flow + hidden gap callout → Task 6
- §3 §E Renewal 3 flow + 1 gap callout → Task 7
- §3 §F View 3 flow + RBAC swim-lane + tier info → Task 8
- §3 §G Edit/Manage 4 flow + cascade detail → Task 9
- §3 §H+I Verifikasi+Audit 2 gap callout + 1 flow → Task 10
- §3 Footer cross-ref → Task 11
- §4 Style decisions (Bootstrap 5.3 + Mermaid + dark toggle + print lengkap + max-width 1100px) → Task 1
- §5 Cross-reference companion + in-link overview-awam → Task 11 + 12

All spec sections mapped to tasks. No gap.

**Placeholder scan:** No "TBD" / "TODO" / "implement later". All code blocks complete.

**Type consistency:** Section ID `#sec-master/#sec-a..#sec-i` konsisten di mini-nav (Task 1) + section heading per task. Mermaid container class `.mermaid` konsisten. `gap-callout` + `hidden-gap-callout` CSS class defined Task 1, used Task 6 + 7 + 10. localStorage key `flow-sistem-theme` Task 1 only.

Plan ready.
