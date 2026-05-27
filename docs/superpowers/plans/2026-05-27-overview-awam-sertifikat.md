# Overview Awam Sertifikat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bikin `docs/sertifikat-ecosystem/overview-awam.html` — 1 halaman pengantar awam HC (struktur sertifikat sekarang + top-5 gap utama) yang tarik intisari dari 2 file companion existing.

**Architecture:** Single static HTML file, no build step. Bootstrap 5.3 + Bootstrap Icons via CDN. Light/dark theme toggle dengan localStorage persist. 2 section utama (Struktur + Gap), mini-nav sticky, link cross-reference dua arah ke `ekosistem-sertifikat.html` + `analisa-gap-benchmark.html`.

**Tech Stack:** HTML5, Bootstrap 5.3 (CDN), Bootstrap Icons 1.11 (CDN), vanilla JS (theme toggle). No test framework (konsisten dengan file lain di folder — doc static).

**Spec reference:** `docs/superpowers/specs/2026-05-27-overview-awam-sertifikat-design.md`

**File Structure:**
- **Create**: `docs/sertifikat-ecosystem/overview-awam.html` (~250 baris, target maks 350)
- **Modify**: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (add in-link banner)
- **Modify**: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` (add in-link banner)

**Test approach:** Visual verify di browser lokal (mobile responsive + dark mode + print preview). Tidak ada Playwright/automation — konsisten dengan file lain di folder.

---

### Task 1: Scaffold HTML skeleton + mini-nav + theme toggle + print CSS

**Files:**
- Create: `docs/sertifikat-ecosystem/overview-awam.html`

- [ ] **Step 1: Write full skeleton file**

```html
<!DOCTYPE html>
<html lang="id" data-bs-theme="light">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Overview Sertifikat untuk Tim HC — Portal HC KPB</title>
  <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css" rel="stylesheet">
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bs-body-bg); }
    main.content { max-width: 900px; margin: 0 auto; padding: 2rem 1.5rem 4rem; }
    section { scroll-margin-top: 5rem; padding-bottom: 2.5rem; margin-bottom: 2rem; border-bottom: 1px solid var(--bs-border-color); }
    section:last-of-type { border-bottom: none; }
    h2 { margin-top: 2rem; padding-top: 1rem; }
    h2 .badge { font-size: 0.7em; vertical-align: middle; }
    .mini-nav { position: sticky; top: 0; z-index: 1020; background: var(--bs-body-bg); border-bottom: 1px solid var(--bs-border-color); padding: 0.5rem 1rem; }
    .mini-nav a { color: var(--bs-body-color); text-decoration: none; padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-size: 0.875rem; }
    .mini-nav a:hover { background: var(--bs-secondary-bg); }
    .audience-banner { background: var(--bs-info-bg-subtle); border-left: 4px solid var(--bs-info); padding: 1rem; margin: 1.5rem 0; border-radius: 0.25rem; }
    .status-card { border-left-width: 4px; }
    .status-aktif { border-left-color: var(--bs-success); }
    .status-akan { border-left-color: var(--bs-warning); }
    .status-expired { border-left-color: var(--bs-danger); }
    .status-permanent { border-left-color: var(--bs-info); }
    @media print {
      .mini-nav, #theme-toggle { display: none !important; }
      main { padding: 1rem !important; max-width: 100% !important; }
      section { page-break-before: always; border-bottom: none; padding-bottom: 1rem; margin-bottom: 1rem; }
      section#sec-struktur { page-break-before: auto; }
      .accordion-collapse { display: block !important; }
      .accordion-button { display: none; }
      .accordion-body { padding: 0.5rem 0; border: none; }
      .card { page-break-inside: avoid; }
      a { color: black; text-decoration: underline; }
      h2 { page-break-after: avoid; }
    }
  </style>
</head>
<body>
  <nav class="mini-nav d-none d-md-flex justify-content-between align-items-center">
    <div class="d-flex flex-wrap gap-1">
      <a href="#sec-struktur">§1 Struktur Sekarang</a>
      <a href="#sec-gap">§2 Gap Utama</a>
    </div>
    <button id="theme-toggle" class="btn btn-sm btn-outline-secondary" title="Toggle dark mode">
      <i class="bi bi-moon-stars"></i>
    </button>
  </nav>

  <main class="content">
    <header class="mb-4">
      <h1 class="display-6">Overview Sertifikat untuk Tim HC</h1>
      <p class="lead text-muted">Pengantar singkat: struktur sekarang + gap utama — Portal HC KPB</p>
      <div class="audience-banner">
        <strong><i class="bi bi-info-circle"></i> Untuk Staff HC admin Portal (awam non-IT).</strong>
        Halaman ini ringkas — 1 halaman scroll. Mau detail lebih dalam? Lihat
        <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a> (struktur lengkap)
        atau <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a> (50 gap lengkap).
      </div>
      <p class="small text-muted">Versi 1.0 — 2026-05-27</p>
    </header>

    <section id="sec-struktur">
      <h2><span class="badge bg-secondary">§1</span> Struktur Sistem Sertifikat Sekarang</h2>
      <p class="text-muted"><em>(Placeholder — diisi Task 2-5)</em></p>
    </section>

    <section id="sec-gap">
      <h2><span class="badge bg-secondary">§2</span> Gap Utama</h2>
      <p class="text-muted"><em>(Placeholder — diisi Task 6-7)</em></p>
    </section>

    <footer class="text-center text-muted small mt-5 pt-4 border-top">
      <p class="text-muted"><em>(Footer placeholder — diisi Task 8)</em></p>
    </footer>
  </main>

  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  <script>
    (function () {
      const html = document.documentElement;
      const btn = document.getElementById('theme-toggle');
      const icon = btn.querySelector('i');
      const stored = localStorage.getItem('overview-awam-theme');
      if (stored) {
        html.setAttribute('data-bs-theme', stored);
        icon.className = stored === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
      }
      btn.addEventListener('click', function () {
        const current = html.getAttribute('data-bs-theme');
        const next = current === 'dark' ? 'light' : 'dark';
        html.setAttribute('data-bs-theme', next);
        icon.className = next === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        localStorage.setItem('overview-awam-theme', next);
      });
    })();
  </script>
</body>
</html>
```

- [ ] **Step 2: Verify browser**

Open `docs/sertifikat-ecosystem/overview-awam.html` di browser. Expected:
- Mini-nav sticky muncul dengan 2 link + theme toggle button
- Header + audience banner render
- 2 section placeholder visible
- Click theme toggle → switch light/dark + persist setelah refresh

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): scaffold overview-awam.html — skeleton + mini-nav + theme toggle + print CSS"
```

---

### Task 2: §1.1 Ekosistem 4-Komponen — 4 mini-card

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — replace `<p class="text-muted"><em>(Placeholder — diisi Task 2-5)</em></p>` di section `#sec-struktur`

- [ ] **Step 1: Replace placeholder dengan content §1.1**

Replace baris placeholder dengan:

```html
      <p>Sistem sertifikat Portal HC KPB punya <strong>4 komponen utama</strong> yang saling terhubung:</p>
      <div class="row g-3 my-3">
        <div class="col-md-6 col-lg-3">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-box-arrow-in-down text-primary"></i> Sumber</h6>
            <p class="small mb-0">Dari mana sertifikat lahir: <strong>online</strong> (otomatis dari assessment yang dijalani &amp; lulus) atau <strong>manual</strong> (HC upload bukti training external).</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-database text-primary"></i> Penyimpanan</h6>
            <p class="small mb-0">Database aplikasi simpan: tanggal terbit, tanggal kadaluarsa, nomor sertifikat, dan link file PDF (jika ada).</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-flag text-primary"></i> Status</h6>
            <p class="small mb-0">4 status (Aktif / Akan Expired / Expired / Permanent) — dihitung sistem <strong>otomatis dan real-time</strong>, bukan disimpan di database.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-bell text-primary"></i> Notifikasi</h6>
            <p class="small mb-0">Sistem kirim notif otomatis <strong>30 hari sebelum</strong> sertifikat expired, ke pekerja terkait + tim HC.</p>
          </div></div>
        </div>
      </div>
```

- [ ] **Step 2: Verify browser**

Refresh `overview-awam.html`. Expected: 4 mini-card render di grid responsive (mobile stack 1 kolom, desktop 4 kolom).

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §1.1 Ekosistem 4-Komponen overview-awam"
```

---

### Task 3: §1.2 Perjalanan 1 Sertifikat — narasi alur ordered list

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — append setelah grid Task 2

- [ ] **Step 1: Append §1.2 setelah `</div>` penutup row Task 2**

```html
      <h5 class="mt-4">1.2 Perjalanan 1 Sertifikat (Happy Path)</h5>
      <ol class="small">
        <li>Pekerja ikut <strong>assessment / training</strong></li>
        <li><strong>Lulus</strong> → sertifikat terbit (otomatis kalau online, manual kalau external upload HC)</li>
        <li>Status awal: <strong>Aktif</strong></li>
        <li>30 hari sebelum tanggal kadaluarsa → status berubah <strong>Akan Expired</strong> + notifikasi keluar ke pekerja + HC</li>
        <li>Tanggal kadaluarsa lewat tanpa renewal → status <strong>Expired</strong></li>
        <li>Renewal: HC trigger training/assessment baru → <strong>sertifikat baru</strong> terbit, otomatis terhubung ke sertifikat lama (renewal chain)</li>
      </ol>
      <p class="small text-muted"><i class="bi bi-info-circle"></i> Sertifikat <strong>tidak selalu terbit</strong> meskipun pekerja selesai assessment. 3 alasan valid: (1) gagal lulus, (2) flag "Generate Certificate" mati di config assessment, (3) ada soal essay menunggu penilaian HC.</p>
```

- [ ] **Step 2: Verify browser**

Refresh. Expected: ordered list 6 langkah + alert note 3 alasan render rapi di bawah grid §1.1.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §1.2 Perjalanan 1 Sertifikat overview-awam"
```

---

### Task 4: §1.3 4 Status Sertifikat — card row

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — append setelah §1.2

- [ ] **Step 1: Append §1.3 setelah paragraf 3-alasan Task 3**

```html
      <h5 class="mt-4">1.3 4 Status Sertifikat</h5>
      <div class="row g-3 my-2">
        <div class="col-md-6 col-lg-3">
          <div class="card status-card status-aktif h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-check-circle-fill text-success"></i> Aktif</h6>
            <p class="small mb-1"><strong>Kapan:</strong> Lebih dari 30 hari menuju expired.</p>
            <p class="small mb-0"><strong>Arti:</strong> Aman, monitor saja.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card status-card status-akan h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-exclamation-triangle-fill text-warning"></i> Akan Expired</h6>
            <p class="small mb-1"><strong>Kapan:</strong> ≤ 30 hari menuju expired.</p>
            <p class="small mb-0"><strong>Arti:</strong> Notif aktif, schedule renewal.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card status-card status-expired h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-x-octagon-fill text-danger"></i> Expired</h6>
            <p class="small mb-1"><strong>Kapan:</strong> Tanggal kadaluarsa lewat.</p>
            <p class="small mb-0"><strong>Arti:</strong> Trigger Renewal Certificate.</p>
          </div></div>
        </div>
        <div class="col-md-6 col-lg-3">
          <div class="card status-card status-permanent h-100"><div class="card-body">
            <h6 class="card-title"><i class="bi bi-infinity text-info"></i> Permanent</h6>
            <p class="small mb-1"><strong>Kapan:</strong> Tipe sertifikat tag Permanent.</p>
            <p class="small mb-0"><strong>Arti:</strong> Tidak pernah expired, no action.</p>
          </div></div>
        </div>
      </div>
```

- [ ] **Step 2: Verify browser**

Refresh. Expected: 4 card status dengan border-left color berbeda (hijau/kuning/merah/biru) + icon Bootstrap.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §1.3 4 Status Sertifikat overview-awam"
```

---

### Task 5: §1.4 Catatan kunci HC — alert box + link companion

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — append setelah §1.3

- [ ] **Step 1: Append §1.4 setelah row card status Task 4**

```html
      <h5 class="mt-4">1.4 3 Catatan Kunci untuk Staff HC</h5>
      <div class="alert alert-light border">
        <ul class="mb-0 small">
          <li><strong>Status sertifikat bukan kolom database.</strong> Sistem hitung otomatis real-time dari tanggal kadaluarsa + jenis. Jangan coba update manual.</li>
          <li><strong>Renewal hanya bisa di-trigger Admin (L1) atau HC (L2).</strong> Peran lain (Manager, SectionHead, Coach, Coachee) tidak punya akses menu Renewal Certificate.</li>
          <li><strong>Renewal chain otomatis filter dashboard.</strong> Sertifikat lama yang sudah di-renew tidak muncul lagi di daftar outstanding meskipun statusnya Expired — dianggap "sudah ditangani".</li>
        </ul>
      </div>
      <p class="small text-muted"><i class="bi bi-arrow-right-circle"></i> Detail lengkap (alur Mermaid + tabel RBAC 6-peran + glosarium 15 istilah) → <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a></p>
```

- [ ] **Step 2: Verify browser**

Refresh. Expected: alert box dengan 3 bullet catatan + link ke ekosistem-sertifikat.html di bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §1.4 Catatan Kunci HC + link companion overview-awam"
```

---

### Task 6: §2.1 Definisi Gap + §2.2 Top-5 Accordion

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — replace `<p class="text-muted"><em>(Placeholder — diisi Task 6-7)</em></p>` di section `#sec-gap`

- [ ] **Step 1: Replace placeholder dengan §2.1 + §2.2 accordion 5 item**

```html
      <h5>2.1 Apa itu Gap?</h5>
      <p><strong>Gap</strong> = fitur / proses / kemampuan yang <strong>seharusnya ada</strong> (berdasarkan benchmark 9 platform external — 5 HRIS+LMS enterprise dunia + 4 platform Migas) tapi <strong>belum ada</strong> di Portal HC KPB. Bukan bug — fitur yang belum dikembangkan.</p>

      <h5 class="mt-4">2.2 Top-5 Gap Kritis</h5>
      <p class="small text-muted">5 gap paling penting dari total 50 gap. Severity 🔴 Kritis = blocking compliance / audit / workflow penting.</p>
      <div class="accordion" id="topGapAccordion">

        <div class="accordion-item">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gap1">
              <strong class="me-2">1. Self-Service Renewal Portal</strong>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-info text-dark">🔄 Flow</span>
            </button>
          </h2>
          <div id="gap1" class="accordion-collapse collapse" data-bs-parent="#topGapAccordion">
            <div class="accordion-body small">
              <p class="mb-2"><strong>Kondisi sekarang:</strong> Renewal hanya bisa di-trigger HC/Admin via menu Renewal Certificate. Pekerja yang sertifikatnya mau expired hanya bisa menunggu HC bertindak.</p>
              <p class="mb-2"><strong>Yang seharusnya ada:</strong> Pekerja bisa trigger renewal sendiri (self-service) selama tipe sertifikat memang allow renewal. Beban HC turun, pekerja lebih proaktif.</p>
              <p class="mb-0"><strong>Effort:</strong> <span class="badge bg-warning text-dark">Medium (3-6 bulan)</span></p>
            </div>
          </div>
        </div>

        <div class="accordion-item">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gap2">
              <strong class="me-2">2. Public Verification (QR / URL)</strong>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-primary">✨ Fitur</span>
            </button>
          </h2>
          <div id="gap2" class="accordion-collapse collapse" data-bs-parent="#topGapAccordion">
            <div class="accordion-body small">
              <p class="mb-2"><strong>Kondisi sekarang:</strong> Sertifikat PDF punya nomor unik tapi tidak ada cara pihak external (rekruter, vendor, auditor) verify keaslian. Mereka harus phone/email HC manual.</p>
              <p class="mb-2"><strong>Yang seharusnya ada:</strong> QR code di PDF sertifikat → arahkan ke halaman public verify (tanpa login). Third-party bisa cek real-time status sertifikat.</p>
              <p class="mb-0"><strong>Effort:</strong> <span class="badge bg-success">Quick Win (1-3 bulan)</span></p>
            </div>
          </div>
        </div>

        <div class="accordion-item">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gap3">
              <strong class="me-2">3. Bulk Action Suite</strong>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-info text-dark">🔄 Flow</span>
            </button>
          </h2>
          <div id="gap3" class="accordion-collapse collapse" data-bs-parent="#topGapAccordion">
            <div class="accordion-body small">
              <p class="mb-2"><strong>Kondisi sekarang:</strong> Operasi sertifikat (renewal, re-issue, export) cuma bisa per-record. Dengan ribuan pekerja KPB, HC harus klik satu-per-satu — bottleneck operasional.</p>
              <p class="mb-2"><strong>Yang seharusnya ada:</strong> Bulk checkbox + bulk action (mass renewal, mass export Excel dengan filter, batch re-issue PDF). 1 form input → ratusan record diproses sekaligus.</p>
              <p class="mb-0"><strong>Effort:</strong> <span class="badge bg-warning text-dark">Medium (3-6 bulan)</span></p>
            </div>
          </div>
        </div>

        <div class="accordion-item">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gap4">
              <strong class="me-2">4. HCM Integration (Employee Master Sync)</strong>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-secondary">🏗️ Sistem</span>
            </button>
          </h2>
          <div id="gap4" class="accordion-collapse collapse" data-bs-parent="#topGapAccordion">
            <div class="accordion-body small">
              <p class="mb-2"><strong>Kondisi sekarang:</strong> Data pekerja (NIP, jabatan, section) di-entry manual atau impor Excel periodik. Saat pekerja pindah section/jabatan, status sertifikat tidak otomatis update.</p>
              <p class="mb-2"><strong>Yang seharusnya ada:</strong> Integrasi API ke sistem HR Pertamina pusat (SAP HCM / Oracle HCM). Sync nightly NIP + jabatan + section. Pindah section → auto-update tanpa intervensi HC.</p>
              <p class="mb-0"><strong>Effort:</strong> <span class="badge bg-danger">Long-term (&gt;9 bulan, butuh koordinasi IT Pertamina pusat)</span></p>
            </div>
          </div>
        </div>

        <div class="accordion-item">
          <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#gap5">
              <strong class="me-2">5. Audit Trail Defensible</strong>
              <span class="badge bg-danger me-1">🔴 Kritis</span>
              <span class="badge bg-secondary">🏗️ Sistem</span>
            </button>
          </h2>
          <div id="gap5" class="accordion-collapse collapse" data-bs-parent="#topGapAccordion">
            <div class="accordion-body small">
              <p class="mb-2"><strong>Kondisi sekarang:</strong> Aksi di Portal HC ter-log umum di file log aplikasi, tapi tidak ada audit trail per-sertifikat yang siap diserahkan ke regulator (BPK, audit internal). Tidak ada record "siapa edit, kapan, dari IP mana".</p>
              <p class="mb-2"><strong>Yang seharusnya ada:</strong> Tabel audit log dedicated per sertifikat: UserId + Action + OldValue + NewValue + Timestamp + IP + UserAgent. Exportable PDF untuk regulator.</p>
              <p class="mb-0"><strong>Effort:</strong> <span class="badge bg-warning text-dark">Medium (3-6 bulan)</span></p>
            </div>
          </div>
        </div>

      </div>
```

- [ ] **Step 2: Verify browser**

Refresh. Expected:
- §2.1 paragraf definisi gap awam
- §2.2 accordion 5 item collapsed by default
- Click item → expand show kondisi/seharusnya/effort
- Click again → collapse

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §2.1 definisi gap + §2.2 top-5 accordion overview-awam"
```

---

### Task 7: §2.3 Ringkasan Total 50 Gap + link companion

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — append setelah accordion Task 6

- [ ] **Step 1: Append §2.3 setelah `</div>` penutup `#topGapAccordion`**

```html
      <h5 class="mt-4">2.3 Ringkasan Total Gap</h5>
      <div class="row text-center my-3 g-3">
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">50</h3>
            <small class="text-muted">Gap Total Unique</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">9</h3>
            <small class="text-muted">Platform Benchmark</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">5</h3>
            <small class="text-muted">Kategori</small>
          </div></div>
        </div>
        <div class="col-6 col-md-3">
          <div class="card h-100"><div class="card-body p-3">
            <h3 class="mb-0 text-primary">3</h3>
            <small class="text-muted">Bucket Roadmap</small>
          </div></div>
        </div>
      </div>
      <p class="small">
        <strong>5 Kategori gap:</strong> 🏗️ Sistem · 🔄 Flow · ✨ Fitur · 🔒 Compliance · ⚡ Performa<br>
        <strong>3 Bucket roadmap:</strong>
        <span class="badge bg-success">Quick Win (1-3 bulan)</span>
        <span class="badge bg-warning text-dark">Medium (3-9 bulan)</span>
        <span class="badge bg-danger">Long-term (&gt;9 bulan)</span>
      </p>
      <p class="small text-muted"><i class="bi bi-arrow-right-circle"></i> Lihat 45 gap lain + 9 platform benchmark detail + roadmap per-item → <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a></p>
```

- [ ] **Step 2: Verify browser**

Refresh. Expected: 4 stat box (50/9/5/3) + ringkasan kategori + badge bucket + link companion.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis §2.3 ringkasan total gap + link companion overview-awam"
```

---

### Task 8: Footer cross-ref + verify final

**Files:**
- Modify: `docs/sertifikat-ecosystem/overview-awam.html` — replace footer placeholder

- [ ] **Step 1: Replace `<p class="text-muted"><em>(Footer placeholder — diisi Task 8)</em></p>`**

Replace dengan:

```html
      <p class="mb-1">Overview Awam Sertifikat — Portal HC KPB · Versi 1.0 · 2026-05-27</p>
      <p class="mb-0">
        Companion docs:
        <a href="./ekosistem-sertifikat.html"><code>ekosistem-sertifikat.html</code></a> ·
        <a href="./analisa-gap-benchmark.html"><code>analisa-gap-benchmark.html</code></a> ·
        <a href="./index.html"><code>index.html</code></a> (versi teknis dev)
      </p>
```

- [ ] **Step 2: Final verify browser**

Refresh. Walk-through end-to-end:
- Header + audience banner OK
- Mini-nav click §1 → scroll smooth to struktur, click §2 → scroll smooth to gap
- §1 4-card komponen render
- §1.2 ordered list 6 langkah
- §1.3 4-card status berwarna
- §1.4 alert 3 bullet
- §2.1 definisi gap
- §2.2 accordion 5 item, expand/collapse OK
- §2.3 4 stat box + badge bucket
- Footer 3 link companion
- Theme toggle light↔dark persist after refresh
- Mobile viewport responsive (resize browser ke ~375px width — card stack OK)
- Print preview (`Ctrl+P`) — accordion expand semua, mini-nav hidden

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/overview-awam.html
git commit -m "docs(sertifikat-ecosystem): tulis footer cross-ref overview-awam — file v1.0 complete"
```

---

### Task 9: Add in-link banner di ekosistem-sertifikat.html

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — add banner setelah audience-banner existing di header

- [ ] **Step 1: Locate header audience banner**

```bash
grep -n "audience-banner" docs/sertifikat-ecosystem/ekosistem-sertifikat.html | head -3
```

Expected: line number of `<div class="audience-banner">` block.

- [ ] **Step 2: Add in-link paragraph setelah audience-banner block**

Cari closing `</div>` dari `audience-banner` di header, append paragraf setelahnya:

```html
      <p class="small text-muted mb-3"><i class="bi bi-lightbulb"></i> Baru? Mulai dari <a href="./overview-awam.html"><code>overview-awam.html</code></a> — 1 halaman pengantar singkat (struktur + top-5 gap).</p>
```

- [ ] **Step 3: Verify browser**

Open `ekosistem-sertifikat.html`. Expected: paragraf tip baru muncul di bawah audience-banner header, link ke overview-awam works.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): add in-link header → overview-awam.html"
```

---

### Task 10: Add in-link banner di analisa-gap-benchmark.html

**Files:**
- Modify: `docs/sertifikat-ecosystem/analisa-gap-benchmark.html` — add banner setelah audience-banner existing

- [ ] **Step 1: Locate audience banner**

```bash
grep -n "audience-banner" docs/sertifikat-ecosystem/analisa-gap-benchmark.html | head -3
```

- [ ] **Step 2: Append in-link paragraf setelah closing `</div>` audience-banner block**

```html
      <p class="small text-muted mb-3"><i class="bi bi-lightbulb"></i> Mau ringkasan saja? Lihat <a href="./overview-awam.html"><code>overview-awam.html</code></a> — 1 halaman pengantar awam (top-5 gap + struktur singkat).</p>
```

- [ ] **Step 3: Verify browser**

Open `analisa-gap-benchmark.html`. Expected: paragraf tip baru muncul di bawah audience-banner, link ke overview-awam works.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/analisa-gap-benchmark.html
git commit -m "docs(sertifikat-ecosystem): add in-link header → overview-awam.html"
```

---

## Self-Review

**Spec coverage check (vs `2026-05-27-overview-awam-sertifikat-design.md`):**
- §3 Header (h1 + subtitle + audience banner + versi) → Task 1
- §3 Mini-nav 2 link + theme toggle → Task 1
- §3 §1.1 Ekosistem 4-Komponen → Task 2
- §3 §1.2 Perjalanan 1 Sertifikat → Task 3
- §3 §1.3 4 Status Sertifikat → Task 4
- §3 §1.4 Catatan kunci Staff HC → Task 5
- §3 §2.1 Apa itu Gap → Task 6
- §3 §2.2 Top-5 Gap Kritis (5 items dengan effort badge) → Task 6
- §3 §2.3 Ringkasan total gap → Task 7
- §3 Footer cross-ref → Task 8
- §4 Style decisions (Bootstrap 5.3, no Mermaid, dark toggle, print CSS, max-width 900px) → Task 1
- §5 Cross-reference dua arah (in-link ekosistem + analisa-gap) → Task 9 + 10
- §6 Success criteria (1 halaman scroll, < 10 menit baca, jawab 4 pertanyaan, entry point clear) → covered overall

All spec sections mapped to tasks. No gap.

**Placeholder scan:** No "TBD" / "TODO" / "implement later". All code blocks complete.

**Type consistency:** Section ID `#sec-struktur` & `#sec-gap` konsisten di mini-nav (Task 1) + section heading (Task 1) + footer link (Task 8). Accordion ID `#topGapAccordion` + `#gap1..#gap5` konsisten Task 6. localStorage key `overview-awam-theme` Task 1 only.

Plan ready.
