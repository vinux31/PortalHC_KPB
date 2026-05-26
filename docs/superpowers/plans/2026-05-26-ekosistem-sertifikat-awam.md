# Ekosistem Sertifikat (Versi Awam) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bikin satu file HTML `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — dokumentasi ekosistem sertifikat Portal HC KPB versi awam untuk Manager/HC non-IT.

**Architecture:** Single static HTML file, no build pipeline. Bootstrap 5.3.0 + Bootstrap Icons + Mermaid 10.x via CDN. Single-column scroll layout (`max-width: 900px` center), mini-nav sticky top (desktop only), light+dark theme toggle. 8 section narrative top-to-bottom, 3 Mermaid diagram, 0 sidebar TOC, 0 section bug/gap (drop total).

**Tech Stack:** HTML5, Bootstrap 5.3.0 (CDN), Bootstrap Icons 1.11.0 (CDN), Mermaid 10.x (CDN), vanilla JS untuk theme toggle.

**Spec:** `docs/superpowers/specs/2026-05-26-ekosistem-sertifikat-awam-design.md`

**Verification model:** Static HTML, no unit test runner. Per task = visual verify di browser. Final task = Playwright spot check (render OK, no console error, 3 Mermaid render, theme toggle works).

---

## File Structure

**File yang dibuat:**
- `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` — file utama (~500-700 baris)

**File yang dimodifikasi:** Tidak ada (`index.html` tetap as-is, tidak disentuh).

**File reference (read-only):**
- `docs/sertifikat-ecosystem/index.html` — pola CSS, theme toggle JS, Mermaid init
- `docs/superpowers/specs/2026-05-26-ekosistem-sertifikat-awam-design.md` — spec sumber

---

## Task 1: Skeleton HTML — `<head>`, audience banner, mini-nav sticky, footer

**Files:**
- Create: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`

- [ ] **Step 1: Tulis file skeleton dengan struktur 8 section kosong**

```html
<!DOCTYPE html>
<html lang="id" data-bs-theme="light">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Ekosistem Sertifikat — Panduan Awam Portal HC KPB</title>
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
    .mermaid { background: var(--bs-light-bg-subtle); padding: 1rem; border-radius: 0.5rem; text-align: center; margin: 1.5rem 0; }
    [data-bs-theme="dark"] .mermaid { background: #1e1e1e; }
    .audience-banner { background: var(--bs-info-bg-subtle); border-left: 4px solid var(--bs-info); padding: 1rem; margin: 1.5rem 0; border-radius: 0.25rem; }
    dl.glosarium dt { font-weight: 600; }
    .status-card { border-left: 4px solid; }
    .status-aktif { border-color: #198754; }
    .status-akan { border-color: #ffc107; }
    .status-expired { border-color: #dc3545; }
    .status-permanent { border-color: #0dcaf0; }
  </style>
</head>
<body>
  <nav class="mini-nav d-none d-md-flex justify-content-between align-items-center">
    <div class="d-flex flex-wrap gap-1">
      <a href="#sec-1">§1 Apa & Kenapa</a>
      <a href="#sec-2">§2 Ekosistem</a>
      <a href="#sec-3">§3 Perjalanan</a>
      <a href="#sec-4">§4 Status</a>
      <a href="#sec-5">§5 Renewal</a>
      <a href="#sec-6">§6 Peran</a>
      <a href="#sec-7">§7 Menu</a>
      <a href="#sec-8">§8 Glosarium</a>
    </div>
    <button id="theme-toggle" class="btn btn-sm btn-outline-secondary" title="Toggle dark mode">
      <i class="bi bi-moon-stars"></i>
    </button>
  </nav>

  <main class="content">
    <header class="mb-4">
      <h1 class="display-6">Ekosistem Sertifikat Portal HC KPB</h1>
      <p class="lead text-muted">Panduan Awam untuk Manager & Tim HC</p>
      <div class="audience-banner">
        <strong><i class="bi bi-info-circle"></i> Dokumen ini untuk Manager/HC non-IT.</strong>
        Versi teknis untuk developer (endpoint, controller, database schema) lihat <a href="./index.html"><code>index.html</code></a>.
      </div>
      <p class="small text-muted">Versi 1.0 — 2026-05-26 — Snapshot per tanggal ini. Bila aplikasi di-update, dokumen perlu di-refresh.</p>
    </header>

    <section id="sec-1"><h2><span class="badge bg-secondary">§1</span> Apa & Kenapa</h2><p><em>Konten Task 2</em></p></section>
    <section id="sec-2"><h2><span class="badge bg-secondary">§2</span> Ekosistem 4-Kotak</h2><p><em>Konten Task 3</em></p></section>
    <section id="sec-3"><h2><span class="badge bg-secondary">§3</span> Perjalanan Sertifikat</h2><p><em>Konten Task 4</em></p></section>
    <section id="sec-4"><h2><span class="badge bg-secondary">§4</span> Status & Kapan Berubah</h2><p><em>Konten Task 5</em></p></section>
    <section id="sec-5"><h2><span class="badge bg-secondary">§5</span> Renewal — Kenapa & Bagaimana</h2><p><em>Konten Task 6</em></p></section>
    <section id="sec-6"><h2><span class="badge bg-secondary">§6</span> Peran & Hak Akses</h2><p><em>Konten Task 7</em></p></section>
    <section id="sec-7"><h2><span class="badge bg-secondary">§7</span> Peta Menu Aplikasi</h2><p><em>Konten Task 8</em></p></section>
    <section id="sec-8"><h2><span class="badge bg-secondary">§8</span> Glosarium</h2><p><em>Konten Task 9</em></p></section>

    <footer class="text-center text-muted small mt-5 pt-4 border-top">
      <p>Ekosistem Sertifikat Portal HC KPB — Panduan Awam v1.0 — 2026-05-26<br>
      Versi teknis: <a href="./index.html">index.html</a> | © PT Pertamina (Persero) — KPB</p>
    </footer>
  </main>

  <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
  <!-- Theme toggle + Mermaid init di Task 10 -->
</body>
</html>
```

- [ ] **Step 2: Buka file di browser, verifikasi render**

Buka `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` di Chrome/Edge.
Expected: Mini-nav sticky di top, audience banner biru, 8 section placeholder, footer.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): scaffold ekosistem-sertifikat.html — skeleton + mini-nav + audience banner"
```

---

## Task 2: §1 Apa & Kenapa + 4 mini card

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-1">`)

- [ ] **Step 1: Ganti placeholder `sec-1` dengan konten lengkap**

```html
<section id="sec-1">
  <h2><span class="badge bg-secondary">§1</span> Apa & Kenapa</h2>
  <p><strong>Sertifikat di Portal HC KPB</strong> adalah bukti resmi bahwa seorang pekerja sudah punya kompetensi tertentu — entah dari lulus assessment online di sistem, atau dari training yang sudah dijalani di luar sistem dan didokumentasikan oleh HC.</p>
  <p>Kenapa penting?</p>
  <ul>
    <li><strong>Compliance pengembangan SDM KPB</strong> — bukti kompetensi pekerja per jabatan</li>
    <li><strong>Evidence coaching & training</strong> — referensi sah untuk evaluasi atasan</li>
    <li><strong>Basis renewal cycle</strong> — kompetensi tidak selalu permanent, perlu refresh berkala agar pekerja tetap kompeten</li>
  </ul>
  <div class="row text-center my-4 g-3">
    <div class="col-6 col-md-3">
      <div class="card h-100"><div class="card-body p-3">
        <h3 class="mb-0 text-primary">2</h3>
        <small class="text-muted">Sumber Sertifikat</small>
      </div></div>
    </div>
    <div class="col-6 col-md-3">
      <div class="card h-100"><div class="card-body p-3">
        <h3 class="mb-0 text-primary">4</h3>
        <small class="text-muted">Status Sertifikat</small>
      </div></div>
    </div>
    <div class="col-6 col-md-3">
      <div class="card h-100"><div class="card-body p-3">
        <h3 class="mb-0 text-primary">6</h3>
        <small class="text-muted">Peran Sistem</small>
      </div></div>
    </div>
    <div class="col-6 col-md-3">
      <div class="card h-100"><div class="card-body p-3">
        <h3 class="mb-0 text-primary">30 hari</h3>
        <small class="text-muted">Notif Pre-Expired</small>
      </div></div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Refresh browser, verifikasi 4 card render**

Expected: 4 card horizontal di desktop, 2x2 grid di mobile. Angka warna primary.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §1 Apa & Kenapa + 4 mini card"
```

---

## Task 3: §2 Ekosistem 4-Kotak + Mermaid #1

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-2">`)

- [ ] **Step 1: Ganti placeholder `sec-2`**

```html
<section id="sec-2">
  <h2><span class="badge bg-secondary">§2</span> Ekosistem 4-Kotak</h2>
  <p>Ekosistem sertifikat Portal HC terdiri dari 4 komponen utama yang saling terhubung:</p>
  <div class="mermaid">
flowchart LR
    A[Sumber<br/>Assessment Online<br/>Training Manual]
    B[Penyimpanan<br/>Database Portal HC]
    C[Status<br/>Aktif / Akan Expired<br/>Expired / Permanent]
    D[Notifikasi<br/>Pekerja + HC]
    A --> B
    B --> C
    C --> D
  </div>
  <dl class="row mt-3">
    <dt class="col-sm-3">Sumber</dt>
    <dd class="col-sm-9">2 jalur: <strong>online</strong> (otomatis dari assessment yang dijalani &amp; lulus), atau <strong>manual</strong> (HC upload bukti training external).</dd>

    <dt class="col-sm-3">Penyimpanan</dt>
    <dd class="col-sm-9">Database aplikasi menyimpan tanggal terbit, tanggal kadaluarsa, nomor sertifikat, dan link file PDF (jika ada).</dd>

    <dt class="col-sm-3">Status</dt>
    <dd class="col-sm-9">Status <strong>tidak disimpan permanen</strong> — dihitung sistem real-time setiap halaman dibuka, berdasarkan tanggal kadaluarsa + jenis sertifikat.</dd>

    <dt class="col-sm-3">Notifikasi</dt>
    <dd class="col-sm-9">Sistem kirim notif otomatis 30 hari sebelum sertifikat expired, ke pekerja terkait dan tim HC.</dd>
  </dl>
</section>
```

- [ ] **Step 2: Belum ada Mermaid init (Task 10) → skip browser render dulu**

Catatan: Mermaid baru akan render setelah Task 10. Sementara, `<div class="mermaid">` tampil sebagai text. OK.

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §2 Ekosistem 4-Kotak + Mermaid"
```

---

## Task 4: §3 Perjalanan Sertifikat + Mermaid #2 + sub "Kapan TIDAK terbit"

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-3">`)

- [ ] **Step 1: Ganti placeholder `sec-3`**

```html
<section id="sec-3">
  <h2><span class="badge bg-secondary">§3</span> Perjalanan Sertifikat</h2>
  <p>Berikut perjalanan satu sertifikat dari awal hingga renewal — happy path:</p>
  <div class="mermaid">
flowchart TD
    A[Pekerja ikut Assessment / Training] --> B{Lulus?}
    B -- Ya --> C[Sertifikat Terbit<br/>Auto atau Manual Upload]
    C --> D[Status: Aktif]
    D --> E[30 hari sebelum expired]
    E --> F[Notifikasi<br/>Pekerja + HC]
    F --> G[Renewal<br/>Training/Assessment Baru]
    G --> H[Sertifikat Baru<br/>terhubung ke sertifikat lama]
    B -- Tidak --> X[Sertifikat TIDAK terbit<br/>lihat 3 alasan di bawah]
  </div>
  <h5 class="mt-4">2 Jalur Sumber Sertifikat</h5>
  <ul>
    <li><strong>Otomatis (online)</strong> — Pekerja lulus assessment di Portal HC, sistem auto-generate nomor sertifikat format <code>KPB/{NOMOR-URUT}/{ROMAWI-BULAN}/{TAHUN}</code>. Contoh: <code>KPB/15/V/2026</code>.</li>
    <li><strong>Manual (upload)</strong> — HC/Admin upload bukti sertifikat dari training external (di luar Portal HC). Nomor sertifikat di-input manual oleh HC.</li>
  </ul>
  <h5 class="mt-4">Kapan Sertifikat TIDAK Terbit</h5>
  <p>Tim HC sering tanya: "Kenapa pekerja sudah selesai assessment tapi sertifikat tidak muncul?" Ini bukan bug — ada 3 alasan valid:</p>
  <div class="row g-3">
    <div class="col-md-4">
      <div class="card h-100 border-danger">
        <div class="card-body">
          <h6 class="card-title"><i class="bi bi-x-circle text-danger"></i> Gagal Lulus</h6>
          <p class="card-text small">Skor pekerja di bawah passing percentage yang ditetapkan. Status assessment: <strong>Failed</strong>. Sertifikat tidak terbit.</p>
        </div>
      </div>
    </div>
    <div class="col-md-4">
      <div class="card h-100 border-warning">
        <div class="card-body">
          <h6 class="card-title"><i class="bi bi-toggle-off text-warning"></i> Lulus tapi Flag Mati</h6>
          <p class="card-text small">Assessment di-konfigurasi tanpa flag <strong>Generate Certificate</strong> (misal: latihan/simulasi internal). Pekerja lulus tapi tidak ada sertifikat terbit.</p>
        </div>
      </div>
    </div>
    <div class="col-md-4">
      <div class="card h-100 border-info">
        <div class="card-body">
          <h6 class="card-title"><i class="bi bi-hourglass-split text-info"></i> Nunggu Penilaian Essay</h6>
          <p class="card-text small">Pekerja lulus pilihan ganda, tapi ada soal essay yang belum dinilai HC. Status: <strong>Pending Grading</strong>. Sertifikat menyusul setelah HC selesai grading essay.</p>
        </div>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §3 Perjalanan + sub 'Kapan TIDAK terbit'"
```

---

## Task 5: §4 Status & Kapan Berubah (4 card warna)

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-4">`)

- [ ] **Step 1: Ganti placeholder `sec-4`**

```html
<section id="sec-4">
  <h2><span class="badge bg-secondary">§4</span> Status & Kapan Berubah</h2>
  <p>Sertifikat punya 4 status yang dihitung sistem secara <strong>otomatis dan real-time</strong>:</p>
  <div class="row g-3 my-3">
    <div class="col-md-6">
      <div class="card status-card status-aktif h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-check-circle-fill text-success"></i> Aktif</h5>
          <p class="small mb-1"><strong>Kapan:</strong> Sertifikat valid, lebih dari 30 hari menuju tanggal expired.</p>
          <p class="small mb-1"><strong>Arti:</strong> Pekerja tetap kompeten, semua aman.</p>
          <p class="small mb-0"><strong>Aksi HC:</strong> Monitor saja, no action.</p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card status-card status-akan h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-exclamation-triangle-fill text-warning"></i> Akan Expired</h5>
          <p class="small mb-1"><strong>Kapan:</strong> Kurang dari atau sama dengan 30 hari menuju tanggal expired.</p>
          <p class="small mb-1"><strong>Arti:</strong> Notifikasi otomatis sudah aktif ke pekerja + HC.</p>
          <p class="small mb-0"><strong>Aksi HC:</strong> Schedule training/assessment renewal.</p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card status-card status-expired h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-x-octagon-fill text-danger"></i> Expired</h5>
          <p class="small mb-1"><strong>Kapan:</strong> Tanggal kadaluarsa sudah lewat.</p>
          <p class="small mb-1"><strong>Arti:</strong> Kompetensi expired, pekerja perlu renewal segera.</p>
          <p class="small mb-0"><strong>Aksi HC:</strong> Trigger Renewal Certificate via Admin Panel.</p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card status-card status-permanent h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-infinity text-info"></i> Permanent</h5>
          <p class="small mb-1"><strong>Kapan:</strong> Jenis sertifikat di-tag "Permanent" (training tertentu tanpa expiry).</p>
          <p class="small mb-1"><strong>Arti:</strong> Tidak pernah expired, tidak perlu renewal.</p>
          <p class="small mb-0"><strong>Aksi HC:</strong> No action.</p>
        </div>
      </div>
    </div>
  </div>
  <div class="alert alert-light border">
    <strong><i class="bi bi-lightbulb"></i> Catatan teknis ringan:</strong>
    Status sertifikat <strong>bukan kolom database</strong> — sistem menghitungnya real-time setiap kali halaman dibuka, berdasarkan tanggal kadaluarsa + jenis sertifikat. Artinya: Anda tidak perlu update status manual; sistem otomatis menggeser sertifikat dari <em>Aktif → Akan Expired → Expired</em> sesuai berjalannya waktu.
  </div>
</section>
```

- [ ] **Step 2: Refresh browser, verifikasi 4 card status warna kiri border (hijau/kuning/merah/biru)**

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §4 Status — 4 card warna + catatan teknis ringan"
```

---

## Task 6: §5 Renewal + Mermaid #3

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-5">`)

- [ ] **Step 1: Ganti placeholder `sec-5`**

```html
<section id="sec-5">
  <h2><span class="badge bg-secondary">§5</span> Renewal — Kenapa & Bagaimana</h2>
  <h5>Kenapa renewal perlu?</h5>
  <p>Kompetensi tidak permanent. Sertifikat tertentu (misal K3, safety, sertifikasi profesi) wajib di-refresh periodik agar pekerja tetap kompeten dan compliant dengan regulasi.</p>
  <h5 class="mt-3">Kapan trigger?</h5>
  <ul>
    <li><strong>Otomatis</strong> — 30 hari sebelum tanggal kadaluarsa, sistem kirim notifikasi ke pekerja + HC.</li>
    <li><strong>Manual</strong> — HC/Admin proaktif via menu <em>Renewal Certificate</em> sebelum notif otomatis keluar.</li>
  </ul>
  <h5 class="mt-3">Siapa boleh trigger?</h5>
  <p>Hanya <strong>Admin (L1)</strong> dan <strong>HC (L2)</strong>. Peran lain (Manager, SectionHead, Coach, Coachee) tidak punya akses ke menu Renewal Certificate.</p>
  <h5 class="mt-3">Alur Renewal</h5>
  <div class="mermaid">
flowchart LR
    A[Sertifikat Lama<br/>mau expired] --> B[Form Renewal<br/>diisi HC/Admin]
    B --> C[Training/Assessment<br/>Baru dijalani pekerja]
    C --> D[Sertifikat Baru terbit]
    D --> E[Terhubung ke<br/>Sertifikat Lama]
  </div>
  <h5 class="mt-3">Renewal Chain — Apa Itu?</h5>
  <p>Sertifikat baru hasil renewal <strong>"diingat" sistem terhubung ke sertifikat lama</strong>. Manfaatnya:</p>
  <ul>
    <li>HC bisa lihat riwayat lengkap kompetensi pekerja: sertifikat ke-1 → renewal ke-1 → renewal ke-2, dst</li>
    <li>Sertifikat lama yang sudah di-renew otomatis dianggap "sudah ditangani" — <strong>tidak masuk daftar outstanding</strong> meskipun statusnya Expired</li>
    <li>Dashboard CDP cuma menampilkan sertifikat outstanding yang masih perlu di-renew, bukan sertifikat lama yang sudah ada penggantinya</li>
  </ul>
</section>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §5 Renewal + Mermaid alur + renewal chain"
```

---

## Task 7: §6 Peran & Hak Akses (tabel 6 peran)

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-6">`)

- [ ] **Step 1: Ganti placeholder `sec-6`**

```html
<section id="sec-6">
  <h2><span class="badge bg-secondary">§6</span> Peran & Hak Akses</h2>
  <p>Portal HC punya 6 peran (level L1 paling tinggi, L6 paling terbatas). Berikut ringkasan hak akses terhadap sertifikat:</p>
  <div class="table-responsive">
    <table class="table table-sm table-bordered align-middle">
      <thead class="table-light">
        <tr>
          <th>Peran</th>
          <th>Level</th>
          <th class="text-center">Lihat</th>
          <th class="text-center">Buat</th>
          <th class="text-center">Edit</th>
          <th class="text-center">Hapus</th>
          <th>Scope</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>Admin</strong></td>
          <td>L1</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td><small>Full — semua section, semua role</small></td>
        </tr>
        <tr>
          <td><strong>HC</strong></td>
          <td>L2</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-success">✓</td>
          <td><small>Full — sama dengan Admin</small></td>
        </tr>
        <tr>
          <td><strong>Manager</strong></td>
          <td>L3</td>
          <td class="text-center text-success">✓</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td><small>Full lihat, terbatas tulis (read-only sertifikat)</small></td>
        </tr>
        <tr>
          <td><strong>SectionHead</strong></td>
          <td>L4</td>
          <td class="text-center text-warning">⚠ section</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td><small>Hanya pekerja di section yang sama</small></td>
        </tr>
        <tr>
          <td><strong>Coach</strong></td>
          <td>L5</td>
          <td class="text-center text-warning">⚠ mapped</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td><small>Dual-mode: coachee yang di-assign + diri sendiri</small></td>
        </tr>
        <tr>
          <td><strong>Coachee</strong></td>
          <td>L6</td>
          <td class="text-center text-warning">⚠ own</td>
          <td class="text-center text-warning">⚠ submit exam</td>
          <td class="text-center text-danger">✗</td>
          <td class="text-center text-danger">✗</td>
          <td><small>Hanya sertifikat pribadi</small></td>
        </tr>
      </tbody>
    </table>
  </div>
  <h5 class="mt-3">Ringkasan per peran</h5>
  <dl class="row">
    <dt class="col-sm-3">Admin / HC</dt>
    <dd class="col-sm-9">Operator sistem — akses penuh semua fitur sertifikat, satu-satunya yang boleh trigger renewal.</dd>
    <dt class="col-sm-3">Manager</dt>
    <dd class="col-sm-9">Pimpinan unit — bisa lihat semua sertifikat untuk keperluan reporting, tidak bisa ubah data.</dd>
    <dt class="col-sm-3">SectionHead</dt>
    <dd class="col-sm-9">Atasan section — lihat sertifikat pekerja di section yang sama saja (scope terbatas).</dd>
    <dt class="col-sm-3">Coach</dt>
    <dd class="col-sm-9">Pembimbing — lihat sertifikat coachee yang di-assign ke dia, plus sertifikat dirinya sendiri.</dd>
    <dt class="col-sm-3">Coachee</dt>
    <dd class="col-sm-9">Pekerja akhir — lihat dan unduh sertifikat pribadi, ikut assessment online untuk dapat sertifikat baru.</dd>
  </dl>
  <p class="small text-muted"><i class="bi bi-info-circle"></i> Untuk matriks hak akses lengkap per endpoint, lihat <a href="./index.html#sec-6">index.html §6</a>.</p>
</section>
```

- [ ] **Step 2: Refresh browser, verifikasi tabel 6 baris render dengan ✓/✗/⚠ berwarna**

- [ ] **Step 3: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §6 Peran — 6-row RBAC table + ringkasan per peran"
```

---

## Task 8: §7 Peta Menu Aplikasi (4 card modul)

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-7">`)

- [ ] **Step 1: Ganti placeholder `sec-7`**

```html
<section id="sec-7">
  <h2><span class="badge bg-secondary">§7</span> Peta Menu Aplikasi</h2>
  <p>Fitur sertifikat tersebar di 4 modul aplikasi Portal HC:</p>
  <div class="row g-3">
    <div class="col-md-6">
      <div class="card h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-clipboard-check text-primary"></i> 1. CMP — Competency Management Program</h5>
          <p class="small text-muted mb-2"><strong>Menu utama:</strong> Records, Budget Training, Certificate, Submit Exam, Export Records</p>
          <p class="card-text small"><strong>Di sini Anda bisa:</strong> melihat daftar sertifikat pekerja, mengelola anggaran training tahunan, melihat detail sertifikat per pekerja, ikut assessment online (Coachee), dan export Excel laporan.</p>
          <p class="small mb-0"><strong>Akses:</strong> Semua peran (dengan scope sesuai level)</p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-award text-success"></i> 2. CDP — Competency Development Program</h5>
          <p class="small text-muted mb-2"><strong>Menu utama:</strong> Certification Management, Export Sertifikat Excel</p>
          <p class="card-text small"><strong>Di sini Anda bisa:</strong> melihat dashboard utama tracking sertifikat (status, expired, renewal outstanding), export laporan sertifikat per kriteria untuk audit/review.</p>
          <p class="small mb-0"><strong>Akses:</strong> Semua peran (dengan scope sesuai level)</p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-gear text-warning"></i> 3. Admin Panel — Kelola Data</h5>
          <p class="small text-muted mb-2"><strong>Menu utama:</strong> Manage Assessment, Renewal Certificate, Add/Edit Training, Finalize Essay Grading</p>
          <p class="card-text small"><strong>Di sini Anda bisa:</strong> setup assessment baru (soal, passing score, flag generate cert), trigger renewal sertifikat, tambah/edit data training manual, nilai essay yang Pending Grading.</p>
          <p class="small mb-0"><strong>Akses:</strong> <span class="badge bg-danger">Admin + HC only</span></p>
        </div>
      </div>
    </div>
    <div class="col-md-6">
      <div class="card h-100">
        <div class="card-body">
          <h5 class="card-title"><i class="bi bi-bell text-info"></i> 4. Notifikasi</h5>
          <p class="small text-muted mb-2"><strong>Menu utama:</strong> List notifikasi user</p>
          <p class="card-text small"><strong>Di sini Anda bisa:</strong> melihat alert pre-expired (30 hari sebelum kadaluarsa), notif sertifikat baru terbit, notif renewal sudah di-trigger.</p>
          <p class="small mb-0"><strong>Akses:</strong> Semua peran (notif personal per user)</p>
        </div>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §7 Peta Menu — 4 card CMP/CDP/Admin/Notif"
```

---

## Task 9: §8 Glosarium

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (replace `<section id="sec-8">`)

- [ ] **Step 1: Ganti placeholder `sec-8`**

```html
<section id="sec-8">
  <h2><span class="badge bg-secondary">§8</span> Glosarium</h2>
  <p>Istilah-istilah yang muncul di dokumen ini:</p>
  <dl class="row glosarium">
    <dt class="col-sm-3">CMP</dt>
    <dd class="col-sm-9">Competency Management Program — modul utama untuk pengelolaan sertifikat &amp; assessment online.</dd>

    <dt class="col-sm-3">CDP</dt>
    <dd class="col-sm-9">Competency Development Program — modul dashboard tracking sertifikat (status, expired, renewal outstanding).</dd>

    <dt class="col-sm-3">KKJ</dt>
    <dd class="col-sm-9">Kompetensi Kerja Jabatan — standar kompetensi per jabatan di KPB.</dd>

    <dt class="col-sm-3">PROTON</dt>
    <dd class="col-sm-9">Professional Refinery Operations Competency Development — program besar pengembangan kompetensi RU IV Cilacap.</dd>

    <dt class="col-sm-3">Assessment</dt>
    <dd class="col-sm-9">Ujian online di Portal HC, terdiri dari soal pilihan ganda dan/atau essay. Hasil lulus dapat menerbitkan sertifikat.</dd>

    <dt class="col-sm-3">Sertifikat Permanent</dt>
    <dd class="col-sm-9">Jenis sertifikat tanpa tanggal kadaluarsa (tidak perlu renewal).</dd>

    <dt class="col-sm-3">ValidUntil</dt>
    <dd class="col-sm-9">Kolom database yang menyimpan tanggal kadaluarsa sertifikat. Tampil di UI sebagai "Berlaku sampai".</dd>

    <dt class="col-sm-3">Renewal Chain</dt>
    <dd class="col-sm-9">Rantai sertifikat hasil renewal yang saling terhubung — sertifikat ke-1 → renewal ke-1 → renewal ke-2, dst.</dd>

    <dt class="col-sm-3">Pending Grading</dt>
    <dd class="col-sm-9">Status assessment yang menunggu HC menilai soal essay-nya. Sertifikat menyusul setelah grading selesai.</dd>

    <dt class="col-sm-3">Generate Certificate flag</dt>
    <dd class="col-sm-9">Pengaturan per-assessment: apakah lulus assessment akan menerbitkan sertifikat resmi (true) atau tidak (false).</dd>

    <dt class="col-sm-3">Section</dt>
    <dd class="col-sm-9">Unit kerja di KPB. Basis scope untuk peran L4 SectionHead — hanya bisa lihat pekerja di section yang sama.</dd>

    <dt class="col-sm-3">Mapped Coachee</dt>
    <dd class="col-sm-9">Pekerja yang di-assign ke seorang Coach melalui tabel mapping di sistem.</dd>

    <dt class="col-sm-3">L1 – L6</dt>
    <dd class="col-sm-9">Level peran sistem. L1 paling tinggi (Admin), L6 paling terbatas (Coachee).</dd>

    <dt class="col-sm-3">Nomor Sertifikat</dt>
    <dd class="col-sm-9">Format <code>KPB/{nomor-urut}/{romawi-bulan}/{tahun}</code> — di-generate otomatis untuk sertifikat hasil assessment online.</dd>

    <dt class="col-sm-3">Renewal Certificate</dt>
    <dd class="col-sm-9">Menu di Admin Panel untuk trigger renewal sertifikat — hanya Admin + HC yang boleh akses.</dd>
  </dl>
</section>
```

- [ ] **Step 2: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): tulis §8 Glosarium — 15 istilah"
```

---

## Task 10: Theme toggle JS + Mermaid init + render verify

**Files:**
- Modify: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html` (add `<script>` block sebelum `</body>`)

- [ ] **Step 1: Tambah Mermaid CDN + init + theme toggle script**

Tambah sebelum `</body>` (setelah Bootstrap bundle):

```html
  <script src="https://cdn.jsdelivr.net/npm/mermaid@10.9.0/dist/mermaid.min.js"></script>
  <script>
    // Theme toggle — persist via localStorage
    const html = document.documentElement;
    const toggleBtn = document.getElementById('theme-toggle');
    const icon = toggleBtn.querySelector('i');

    function applyTheme(theme) {
      html.setAttribute('data-bs-theme', theme);
      icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
      mermaid.initialize({ startOnLoad: false, theme: theme === 'dark' ? 'dark' : 'default', securityLevel: 'loose' });
      // Re-render Mermaid setelah theme change
      document.querySelectorAll('.mermaid').forEach((el, i) => {
        el.removeAttribute('data-processed');
        if (!el.dataset.original) el.dataset.original = el.textContent;
        el.textContent = el.dataset.original;
      });
      mermaid.run();
    }

    const savedTheme = localStorage.getItem('eko-cert-theme') || 'light';
    applyTheme(savedTheme);

    toggleBtn.addEventListener('click', () => {
      const newTheme = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
      localStorage.setItem('eko-cert-theme', newTheme);
      applyTheme(newTheme);
    });
  </script>
```

- [ ] **Step 2: Refresh browser**

Expected:
- 3 Mermaid diagram render (§2, §3, §5)
- Theme toggle button top-right (moon icon di light, sun icon di dark)
- Klik toggle → background gelap, Mermaid re-render dengan theme dark
- Reload page → theme tetap (persist via localStorage)

- [ ] **Step 3: Cek browser console — no JS error**

DevTools → Console. Expected: clean, no `Uncaught` error. Boleh ada Mermaid warning info, asal bukan error.

- [ ] **Step 4: Commit**

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): wire Mermaid init + theme toggle dgn localStorage persist"
```

---

## Task 11: Final QA — Playwright spot check + line count verify

**Files:**
- Modify: tidak ada (verifikasi saja)

- [ ] **Step 1: Hitung baris file**

Run: `wc -l "docs/sertifikat-ecosystem/ekosistem-sertifikat.html"`
Expected: ~500-700 baris (acceptance criteria spec §7)

- [ ] **Step 2: Spot check semua section ada**

Run grep untuk verifikasi 8 section render:

```bash
grep -c 'id="sec-' docs/sertifikat-ecosystem/ekosistem-sertifikat.html
```

Expected: `8`

- [ ] **Step 3: Playwright spot check (kalau tersedia)**

Pakai `mcp__plugin_playwright_playwright__browser_navigate` ke `file:///{absolute-path}/docs/sertifikat-ecosystem/ekosistem-sertifikat.html`, lalu:
- `browser_snapshot` — verifikasi struktur render (8 section, mini-nav, footer)
- `browser_console_messages` — verifikasi no error
- Klik `#theme-toggle` → verifikasi dark mode aktif
- `browser_take_screenshot` (light + dark) untuk arsip visual

Kalau Playwright tidak tersedia, skip — minta user verifikasi manual di browser.

- [ ] **Step 4: Verifikasi acceptance criteria spec §7**

Checklist (manual atau Playwright):
1. ✅ File ada di `docs/sertifikat-ecosystem/`
2. ✅ 8 section lengkap (§1-§8)
3. ✅ 3 diagram Mermaid render (§2, §3, §5)
4. ✅ 6 peran di §6: Admin/HC/Manager/SectionHead/Coach/Coachee
5. ✅ Sub "Kapan sertifikat TIDAK terbit" di §3 cover 3 alasan
6. ✅ Audience banner arahkan ke `index.html`
7. ✅ Light+dark theme toggle berfungsi
8. ✅ Tidak ada section bugs/gap
9. ✅ Bahasa awam (no endpoint/controller/file:line/SQL)
10. ✅ Single-page scroll, mini-nav sticky (bukan sidebar)
11. ✅ Render OK Chrome+Edge

- [ ] **Step 5: Commit final (kalau ada perubahan)**

Kalau Playwright menghasilkan screenshot atau ada minor fix dari QA:

```bash
git add docs/sertifikat-ecosystem/ekosistem-sertifikat.html
git commit -m "docs(sertifikat-ecosystem): final QA pass — Playwright verified, ALL acceptance criteria met"
```

Kalau tidak ada perubahan, skip commit dan report ke user.

---

## Final Report

Setelah Task 11 selesai:

1. Report ke user:
   - File path final: `docs/sertifikat-ecosystem/ekosistem-sertifikat.html`
   - Total commits: 10 (Task 1-10) + opsional 1 (Task 11)
   - Acceptance criteria: 11/11 ✅
   - Screenshot Playwright (kalau ada)
2. Ingatkan user untuk:
   - Visual verify final di browser (Chrome/Edge desktop + mobile responsive)
   - Push ke `origin/main` kalau OK (`git push`)
   - Update MEMORY.md kalau perlu

