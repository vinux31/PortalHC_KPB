# Coaching Proton Mockup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Buat 1 file HTML interaktif `docs/mockup-presentasi/coaching-proton-mockup.html` yang mereplika 80-100% UI fitur CoachingProton + 5 sub-layar + 3 modal Portal HC KPB untuk presentasi ke atasan.

**Architecture:** Single-page HTML dengan 6 section layar yang di-toggle via Vanilla JS (display:none), Bootstrap 5.3.0 + Bootstrap Icons 1.10.0 embed lokal di `vendor/`, data hardcoded inline JS, 3 modal Bootstrap interactive, toast feedback untuk mock action. Tidak ada backend, tidak ada API, tidak ada build tool.

**Tech Stack:** HTML5, Bootstrap 5.3.0 (CSS+JS bundle), Bootstrap Icons 1.10.0, Vanilla JS, jQuery 3.7.1 (optional, match Layout).

**Spec reference:** `docs/superpowers/specs/2026-05-26-coaching-proton-mockup-design.md`

---

## File Structure

```
docs/mockup-presentasi/
├── coaching-proton-mockup.html    (file utama, ±2000 baris)
└── vendor/
    ├── bootstrap.min.css           (Bootstrap 5.3.0)
    ├── bootstrap.bundle.min.js     (Bootstrap 5.3.0 with Popper)
    ├── bootstrap-icons.css         (Bootstrap Icons 1.10.0)
    └── fonts/
        ├── bootstrap-icons.woff2
        └── bootstrap-icons.woff
```

**File responsibilities:**
- `coaching-proton-mockup.html` — semua HTML/CSS/JS inline (chrome + 6 layar + 3 modal + data + navigation + toast)
- `vendor/*` — Bootstrap & Icons offline (no edit)

---

## Task 1: Setup Folder + Download Vendor Assets

**Files:**
- Create: `docs/mockup-presentasi/vendor/bootstrap.min.css`
- Create: `docs/mockup-presentasi/vendor/bootstrap.bundle.min.js`
- Create: `docs/mockup-presentasi/vendor/bootstrap-icons.css`
- Create: `docs/mockup-presentasi/vendor/fonts/bootstrap-icons.woff2`
- Create: `docs/mockup-presentasi/vendor/fonts/bootstrap-icons.woff`

- [ ] **Step 1: Buat folder**

```bash
mkdir -p docs/mockup-presentasi/vendor/fonts
```

- [ ] **Step 2: Download Bootstrap 5.3.0 CSS**

```bash
curl -L -o docs/mockup-presentasi/vendor/bootstrap.min.css \
  https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css
```

Expected: file ~230 KB

- [ ] **Step 3: Download Bootstrap 5.3.0 JS bundle**

```bash
curl -L -o docs/mockup-presentasi/vendor/bootstrap.bundle.min.js \
  https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js
```

Expected: file ~80 KB

- [ ] **Step 4: Download Bootstrap Icons 1.10.0 CSS**

```bash
curl -L -o docs/mockup-presentasi/vendor/bootstrap-icons.css \
  https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css
```

Expected: file ~120 KB

- [ ] **Step 5: Download Bootstrap Icons fonts**

```bash
curl -L -o docs/mockup-presentasi/vendor/fonts/bootstrap-icons.woff2 \
  https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/fonts/bootstrap-icons.woff2

curl -L -o docs/mockup-presentasi/vendor/fonts/bootstrap-icons.woff \
  https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/fonts/bootstrap-icons.woff
```

Expected: 2 font files ~100 KB + ~120 KB

- [ ] **Step 6: Fix font path di bootstrap-icons.css**

Bootstrap Icons CSS expects fonts at relative path `./fonts/`. Edit `vendor/bootstrap-icons.css`:

Cari pattern `url("./fonts/bootstrap-icons.woff2")` dan `url("./fonts/bootstrap-icons.woff")` — verifikasi sudah benar (default sudah `./fonts/`).

Kalau path beda (mis. `../fonts/`), replace dengan `./fonts/`.

- [ ] **Step 7: Verifikasi files**

```bash
ls -lh docs/mockup-presentasi/vendor/ docs/mockup-presentasi/vendor/fonts/
```

Expected output mencakup:
- `bootstrap.min.css` ~230 KB
- `bootstrap.bundle.min.js` ~80 KB
- `bootstrap-icons.css` ~120 KB
- `fonts/bootstrap-icons.woff2` ~100 KB
- `fonts/bootstrap-icons.woff` ~120 KB

- [ ] **Step 8: Commit**

```bash
git add docs/mockup-presentasi/vendor/
git commit -m "feat(mockup): tambah Bootstrap 5.3.0 + Icons 1.10.0 vendor offline"
```

---

## Task 2: Query PROTON Kompetensi Real dari DB Lokal

**Files:** none (data discovery only, hasil dipakai Task 3)

- [ ] **Step 1: Cek status SQL Server lokal**

```bash
sqlcmd -S . -E -Q "SELECT name FROM sys.databases WHERE name LIKE '%HcPortal%'"
```

Expected: list database HcPortal*.

Kalau gagal/server tidak jalan, lewati ke Step 3 (fallback).

- [ ] **Step 2: Query 3 kompetensi + 6 sub + 6 deliverable**

```bash
sqlcmd -S . -E -d HcPortal -Q "SELECT TOP 3 Id, NamaKompetensi FROM ProtonKompetensi ORDER BY Id"
```

Catat 3 nama kompetensi.

```bash
sqlcmd -S . -E -d HcPortal -Q "SELECT TOP 6 sk.Id, sk.NamaSubKompetensi, k.NamaKompetensi FROM ProtonSubKompetensi sk INNER JOIN ProtonKompetensi k ON sk.ProtonKompetensiId = k.Id ORDER BY sk.ProtonKompetensiId, sk.Id"
```

Catat 6 sub-kompetensi + parent kompetensi.

```bash
sqlcmd -S . -E -d HcPortal -Q "SELECT TOP 6 d.Id, d.NamaDeliverable, sk.NamaSubKompetensi FROM ProtonDeliverable d INNER JOIN ProtonSubKompetensi sk ON d.ProtonSubKompetensiId = sk.Id ORDER BY d.Id"
```

Catat 6 deliverable + parent sub.

- [ ] **Step 3: Fallback kalau DB tidak ada**

Pakai placeholder generic (tetap berlabel "*sample data*" di komentar):

```
KOMPETENSI:
1. Pemeliharaan Mekanikal (sample data)
2. Operasi Kilang Lanjut (sample data)
3. Quality Assurance Inspeksi (sample data)

SUB-KOMPETENSI:
1.1 Pemeliharaan Rotating Equipment
1.2 Pemeliharaan Static Equipment
2.1 Operasi Crude Distillation Unit
3.1 Inspeksi Korosi Pipa
3.2 Inspeksi Bejana Tekan

DELIVERABLE:
- Laporan Inspeksi Mingguan Rotating Equipment
- Dokumen Troubleshooting Static Equipment
- Standard Operating Procedure CDU
- Laporan Hasil Inspeksi Pipa Bulan Berjalan
- Sertifikat Inspeksi Bejana Tekan
- Resume Coaching Quality Assurance
```

- [ ] **Step 4: Simpan hasil discovery sebagai komentar di file mockup nanti**

Catatan dipakai di Task 3 untuk fill `KOMPETENSI` JS object.

Tidak ada commit di task ini (data discovery only).

---

## Task 3: Skeleton HTML + Chrome (Navbar + Footer Nav)

**Files:**
- Create: `docs/mockup-presentasi/coaching-proton-mockup.html`

- [ ] **Step 1: Tulis skeleton awal**

```html
<!DOCTYPE html>
<html lang="id">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Mockup Coaching Proton - Portal HC KPB</title>
    <link rel="stylesheet" href="./vendor/bootstrap.min.css" />
    <link rel="stylesheet" href="./vendor/bootstrap-icons.css" />
    <style>
        /* Chrome ala Portal real */
        .navbar { box-shadow: 0 2px 10px rgba(0,0,0,0.05); }
        .nav-link { font-weight: 500; }
        .dropdown-item:active { background-color: #0d6efd; }

        /* Footer navigasi sticky */
        .mockup-footer-nav {
            position: fixed;
            bottom: 0;
            left: 0;
            right: 0;
            background: #fff;
            border-top: 1px solid #dee2e6;
            box-shadow: 0 -2px 10px rgba(0,0,0,0.05);
            padding: 12px 24px;
            z-index: 1040;
        }
        .mockup-screen-dots { display: flex; gap: 8px; }
        .mockup-screen-dot {
            width: 10px; height: 10px; border-radius: 50%;
            background: #ced4da; cursor: pointer; transition: background 0.2s;
        }
        .mockup-screen-dot.active { background: var(--bs-primary); }
        .mockup-screen-dot:hover { background: #6c757d; }

        /* Padding bawah body supaya footer tidak nutupin konten */
        body { padding-bottom: 70px; }

        /* Sembunyikan layar non-aktif */
        .mockup-screen { display: none; }
        .mockup-screen.screen-active { display: block; }

        /* Hotspot mati: cursor not-allowed */
        .demo-disabled { cursor: not-allowed !important; }

        /* Avatar bulatan match Portal */
        .navbar-avatar {
            width: 36px; height: 36px; font-size: 0.85em;
        }
    </style>
</head>
<body class="bg-light">

    <!-- ============ HEADER NAVBAR PERTAMINA ============ -->
    <header>
        <nav class="navbar navbar-expand-lg navbar-light bg-white border-bottom mb-4 sticky-top">
            <div class="container-fluid px-4">
                <a class="navbar-brand fw-bold text-primary d-flex align-items-center demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">
                    <i class="bi bi-buildings-fill me-2 fs-4"></i> HC Portal
                </a>
                <div class="collapse navbar-collapse" id="mainNav">
                    <ul class="navbar-nav me-auto mb-2 mb-lg-0 ms-lg-4">
                        <li class="nav-item">
                            <a class="nav-link text-dark demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">CMP</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">CDP</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">
                                <i class="bi bi-question-circle me-1"></i>Panduan
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">
                                <i class="bi bi-gear-fill me-1"></i>Kelola Data
                            </a>
                        </li>
                    </ul>
                    <div class="d-flex align-items-center border-start ps-lg-4 ms-lg-2 mt-3 mt-lg-0">
                        <div class="text-end me-3 d-none d-lg-block">
                            <div class="fw-bold text-dark small" id="navUserName">Eko Wibowo</div>
                            <div class="d-flex align-items-center justify-content-end gap-1">
                                <span class="badge bg-primary bg-opacity-10 text-primary" id="navUserRole">Coach</span>
                            </div>
                        </div>
                        <div class="dropdown">
                            <a href="#" class="d-flex align-items-center text-decoration-none dropdown-toggle" data-bs-toggle="dropdown">
                                <div class="bg-primary text-white rounded-circle d-flex justify-content-center align-items-center fw-bold navbar-avatar" id="navUserInitials">EW</div>
                            </a>
                            <ul class="dropdown-menu dropdown-menu-end shadow border-0" style="min-width: 200px;">
                                <li><a class="dropdown-item demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()"><i class="bi bi-person me-2"></i> My Profile</a></li>
                                <li><a class="dropdown-item demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()"><i class="bi bi-gear me-2"></i> Settings</a></li>
                                <li><hr class="dropdown-divider"></li>
                                <li>
                                    <a class="dropdown-item text-danger fw-bold demo-disabled" href="#" onclick="event.preventDefault(); showDemoToast()">
                                        <i class="bi bi-box-arrow-right me-2"></i> Logout
                                    </a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </nav>
    </header>

    <!-- ============ KONTAINER 6 LAYAR ============ -->
    <div class="container-fluid pb-5">
        <!-- Layar 1: Coach View -->
        <div id="screen-1" class="mockup-screen screen-active">
            <!-- TODO Task 4 -->
        </div>
        <!-- Layar 2: Detail Pending -->
        <div id="screen-2" class="mockup-screen">
            <!-- TODO Task 6 -->
        </div>
        <!-- Layar 3: SrSpv View -->
        <div id="screen-3" class="mockup-screen">
            <!-- TODO Task 7 -->
        </div>
        <!-- Layar 4: Detail Approved -->
        <div id="screen-4" class="mockup-screen">
            <!-- TODO Task 9 -->
        </div>
        <!-- Layar 5: HC View -->
        <div id="screen-5" class="mockup-screen">
            <!-- TODO Task 10 -->
        </div>
        <!-- Layar 6: Edit Session -->
        <div id="screen-6" class="mockup-screen">
            <!-- TODO Task 12 -->
        </div>
    </div>

    <!-- ============ FOOTER NAVIGATION ============ -->
    <div class="mockup-footer-nav d-flex justify-content-between align-items-center">
        <button id="btnPrev" class="btn btn-outline-secondary" disabled>
            <i class="bi bi-arrow-left me-1"></i>Sebelumnya
        </button>
        <div class="d-flex align-items-center gap-3">
            <div class="mockup-screen-dots" id="screenDots">
                <span class="mockup-screen-dot active" data-screen="1"></span>
                <span class="mockup-screen-dot" data-screen="2"></span>
                <span class="mockup-screen-dot" data-screen="3"></span>
                <span class="mockup-screen-dot" data-screen="4"></span>
                <span class="mockup-screen-dot" data-screen="5"></span>
                <span class="mockup-screen-dot" data-screen="6"></span>
            </div>
            <small class="text-muted">Layar <span id="screenCurrent">1</span> dari 6</small>
        </div>
        <button id="btnNext" class="btn btn-primary">
            Berikutnya<i class="bi bi-arrow-right ms-1"></i>
        </button>
    </div>

    <!-- ============ TOAST CONTAINER ============ -->
    <div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index:1100">
        <div id="actionToast" class="toast align-items-center border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body text-white" id="actionToastBody"></div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    </div>

    <!-- ============ MODAL PLACEHOLDERS ============ -->
    <!-- Modal C1: Submit Evidence (Task 5) -->
    <!-- Modal C2: Tinjau Deliverable (Task 8) -->
    <!-- Modal C3: Batch HC Approve (Task 11) -->

    <script src="./vendor/bootstrap.bundle.min.js"></script>
    <script>
        // === TOAST HELPER ===
        function showToast(message, type) {
            type = type || 'success';
            const toast = document.getElementById('actionToast');
            const toastBody = document.getElementById('actionToastBody');
            if (!toast || !toastBody) return;
            toastBody.textContent = message;
            toast.className = 'toast align-items-center border-0';
            toast.classList.add(type === 'success' ? 'bg-success' : 'bg-danger');
            const bsToast = bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 });
            bsToast.show();
        }

        function showDemoToast() {
            showToast('Demo mode — fitur ini aktif di Portal produksi', 'danger');
        }

        // === NAVIGATION ===
        let currentScreen = 1;
        const TOTAL_SCREENS = 6;

        function showScreen(n) {
            if (n < 1 || n > TOTAL_SCREENS) return;
            document.querySelectorAll('.mockup-screen').forEach(s => s.classList.remove('screen-active'));
            document.getElementById('screen-' + n).classList.add('screen-active');
            document.querySelectorAll('.mockup-screen-dot').forEach(d => d.classList.remove('active'));
            document.querySelector('.mockup-screen-dot[data-screen="' + n + '"]').classList.add('active');
            document.getElementById('screenCurrent').textContent = n;
            document.getElementById('btnPrev').disabled = (n === 1);
            document.getElementById('btnNext').disabled = (n === TOTAL_SCREENS);
            updateUserPov(n);
            currentScreen = n;
            window.scrollTo(0, 0);
        }

        // === POV (user dropdown) per layar ===
        function updateUserPov(n) {
            const povMap = {
                1: { name: 'Eko Wibowo', role: 'Coach', initials: 'EW' },
                2: { name: 'Eko Wibowo', role: 'Coach', initials: 'EW' },
                3: { name: 'Fajar Hidayat', role: 'Sr Supervisor', initials: 'FH' },
                4: { name: 'Eko Wibowo', role: 'Coach', initials: 'EW' },
                5: { name: 'Hadi Nugroho', role: 'HC', initials: 'HN' },
                6: { name: 'Eko Wibowo', role: 'Coach', initials: 'EW' }
            };
            const pov = povMap[n];
            document.getElementById('navUserName').textContent = pov.name;
            document.getElementById('navUserRole').textContent = pov.role;
            document.getElementById('navUserInitials').textContent = pov.initials;
        }

        // === EVENT LISTENERS ===
        document.getElementById('btnPrev').addEventListener('click', () => showScreen(currentScreen - 1));
        document.getElementById('btnNext').addEventListener('click', () => showScreen(currentScreen + 1));
        document.querySelectorAll('.mockup-screen-dot').forEach(dot => {
            dot.addEventListener('click', () => showScreen(parseInt(dot.dataset.screen)));
        });
        document.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowLeft') showScreen(currentScreen - 1);
            else if (e.key === 'ArrowRight') showScreen(currentScreen + 1);
        });

        // === DEMO-DISABLED hotspot ===
        document.addEventListener('click', (e) => {
            if (e.target.closest('.demo-disabled') && !e.defaultPrevented) {
                e.preventDefault();
                showDemoToast();
            }
        });

        // Init
        showScreen(1);
    </script>
</body>
</html>
```

- [ ] **Step 2: Buka file di Chrome**

```bash
start chrome "file:///$(pwd -W)/docs/mockup-presentasi/coaching-proton-mockup.html"
```

(Atau buka manual via File Explorer)

Expected:
- Navbar Pertamina tampak (logo HC Portal biru, nav CMP/CDP/Panduan/Kelola Data, user dropdown "Eko Wibowo Coach EW")
- Footer sticky di bawah: tombol Prev disabled + 6 dot (dot 1 aktif) + "Layar 1 dari 6" + tombol Next aktif
- Body kosong (6 layar belum diisi)

- [ ] **Step 3: Test navigasi**

- Klik tombol Next → "Layar 2 dari 6", dot ke-2 aktif, user dropdown tetap "Eko Wibowo Coach EW" (POV 1,2,4,6 sama)
- Klik dot ke-3 → "Layar 3 dari 6", user dropdown berubah jadi "Fajar Hidayat Sr Supervisor FH"
- Klik dot ke-5 → user dropdown "Hadi Nugroho HC HN"
- Klik dot ke-6 → tombol Next disabled
- Keyboard ←/→ kerja
- Klik link navbar (CMP/CDP/Panduan/Logo) → toast merah "Demo mode — fitur ini aktif di Portal produksi"

- [ ] **Step 4: Test offline**

- Matikan wifi
- Refresh browser
- Expected: tetap render sempurna, no console errors

- [ ] **Step 5: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): skeleton HTML + chrome navbar Pertamina + footer nav 6 layar"
```

---

## Task 4: Layar [1] Coach View — CoachingProton + Data Inline

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 4 -->` di `#screen-1`)

- [ ] **Step 1: Tambah data inline JS sebelum function showScreen**

Cari `// === NAVIGATION ===` di `<script>`. Sebelum baris itu, tambahkan:

```javascript
// === MASTER DATA INLINE ===
// Kompetensi PROTON (ambil dari Task 2 discovery, ganti dengan data real DB lokal)
const KOMPETENSI = [
    {
        id: 1, nama: 'Pemeliharaan Mekanikal', // GANTI dengan hasil Task 2 Step 2
        sub: [
            { id: 11, nama: 'Pemeliharaan Rotating Equipment', deliverable: [
                { id: 101, nama: 'Laporan Inspeksi Mingguan Rotating Equipment' },
                { id: 102, nama: 'SOP Pemeliharaan Pompa Sentrifugal' }
            ]},
            { id: 12, nama: 'Pemeliharaan Static Equipment', deliverable: [
                { id: 103, nama: 'Dokumen Troubleshooting Static Equipment' },
                { id: 104, nama: 'Resume Coaching Bejana Tekan' }
            ]}
        ]
    },
    {
        id: 2, nama: 'Operasi Kilang Lanjut',
        sub: [
            { id: 21, nama: 'Operasi Crude Distillation Unit', deliverable: [
                { id: 201, nama: 'Standard Operating Procedure CDU' },
                { id: 202, nama: 'Laporan Performance CDU Bulanan' }
            ]}
        ]
    },
    {
        id: 3, nama: 'Quality Assurance Inspeksi',
        sub: [
            { id: 31, nama: 'Inspeksi Korosi Pipa', deliverable: [
                { id: 301, nama: 'Laporan Hasil Inspeksi Pipa Bulan Berjalan' }
            ]},
            { id: 32, nama: 'Inspeksi Bejana Tekan', deliverable: [
                { id: 302, nama: 'Sertifikat Inspeksi Bejana Tekan' }
            ]}
        ]
    }
];

const COACHEES = [
    { id: 'c1', nama: 'Ahmad Budiman', track: 'PROTON Maintenance', tahunKe: 'Tahun 1', unit: 'Maintenance', seksi: 'Mekanikal' },
    { id: 'c2', nama: 'Citra Lestari', track: 'PROTON Operasi', tahunKe: 'Tahun 2', unit: 'Operasi', seksi: 'Kilang' },
    { id: 'c3', nama: 'Dimas Pratama', track: 'PROTON Maintenance', tahunKe: 'Tahun 1', unit: 'Maintenance', seksi: 'Mekanikal' },
    { id: 'c4', nama: 'Bayu Saputra', track: 'PROTON Maintenance', tahunKe: 'Tahun 2', unit: 'Maintenance', seksi: 'Listrik' },
    { id: 'c5', nama: 'Rini Astuti', track: 'PROTON Operasi', tahunKe: 'Tahun 1', unit: 'Operasi', seksi: 'Utility' }
];
```

- [ ] **Step 2: Replace `<!-- TODO Task 4 -->` di `#screen-1` dengan konten layar Coach View**

```html
            <!-- ===== BREADCRUMB ===== -->
            <nav aria-label="breadcrumb" class="mb-2">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" class="demo-disabled">CDP</a></li>
                    <li class="breadcrumb-item active">Coaching Proton</li>
                </ol>
            </nav>

            <!-- ===== HEADER ===== -->
            <div class="d-flex justify-content-between align-items-center mb-2">
                <div>
                    <h2><i class="bi bi-graph-up me-2"></i>Coaching Proton</h2>
                    <p class="text-muted mb-0">Monitor deliverable progress and approval status</p>
                </div>
                <a href="#" class="btn btn-outline-secondary demo-disabled">
                    <i class="bi bi-arrow-left me-1"></i>Kembali
                </a>
            </div>

            <!-- ===== FILTER BAR ===== -->
            <div class="card shadow-sm mb-3" style="border:1px solid #e0e0e0;">
                <div class="card-body py-2 px-3">
                    <small class="text-muted fw-semibold text-uppercase d-block mb-2" style="font-size:0.7rem;letter-spacing:0.5px;">Filter</small>
                    <div class="d-flex flex-wrap gap-2 align-items-center">
                        <!-- Coachee dropdown (Coach level 5 muncul) -->
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:180px">
                            <option value="">— Pilih Coachee —</option>
                            <option value="c1">Ahmad Budiman [Mekanikal]</option>
                            <option value="c2">Citra Lestari [Kilang]</option>
                            <option value="c3">Dimas Pratama [Mekanikal]</option>
                        </select>
                        <!-- Track dropdown -->
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:140px">
                            <option value="">Semua Track</option>
                            <option>PROTON Maintenance</option>
                            <option>PROTON Operasi</option>
                        </select>
                        <!-- Tahun dropdown -->
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:130px">
                            <option value="">Semua Tahun</option>
                            <option>Tahun 1</option>
                            <option>Tahun 2</option>
                        </select>
                        <!-- Search box (HIDUP) -->
                        <div class="position-relative" style="min-width:200px">
                            <span class="position-absolute top-50 start-0 translate-middle-y ps-2 text-muted">
                                <i class="bi bi-search"></i>
                            </span>
                            <input type="text" class="form-control form-control-sm ps-4 search-input"
                                   data-target="#table-screen-1" placeholder="Cari kompetensi..." autocomplete="off">
                        </div>
                        <button class="btn btn-outline-secondary btn-sm demo-disabled">
                            <i class="bi bi-arrow-counterclockwise"></i> Reset
                        </button>
                    </div>
                </div>
            </div>

            <!-- ===== 3 STAT CARD ===== -->
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-primary) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-graph-up me-1"></i>Progress</h6>
                            <h3 class="mb-0 text-primary">65%</h3>
                            <div class="progress mt-2" style="height: 6px;">
                                <div class="progress-bar" role="progressbar" style="width: 65%"></div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-warning) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-exclamation-triangle me-1"></i>Pending Actions</h6>
                            <h3 class="mb-0 text-warning">2</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-info) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-hourglass-split me-1"></i>Pending Approvals</h6>
                            <h3 class="mb-0 text-info">0</h3>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ===== TABEL DELIVERABLE ===== -->
            <small class="text-muted mb-2 d-block">
                Menampilkan <span class="rows-counter">1-8</span> dari <span class="rows-total">12</span> deliverable
            </small>
            <div class="table-responsive">
                <table class="table table-bordered table-hover" id="table-screen-1">
                    <thead style="background-color: #e8f0fe; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.5px;">
                        <tr>
                            <th>Kompetensi</th>
                            <th>Sub Kompetensi</th>
                            <th>Deliverable</th>
                            <th>Evidence</th>
                            <th>Approval Sr. Spv</th>
                            <th>Approval Section Head</th>
                            <th>Approval HC</th>
                            <th>Detail</th>
                        </tr>
                    </thead>
                    <tbody class="table-page-1">
                        <!-- Ahmad Budiman: Kompetensi 1 (Mekanikal), 2 sub, 4 deliverable -->
                        <tr data-search="pemeliharaan mekanikal rotating equipment laporan inspeksi mingguan">
                            <td rowspan="4" class="align-middle fw-bold">Pemeliharaan Mekanikal</td>
                            <td rowspan="2" class="align-middle">Pemeliharaan Rotating Equipment</td>
                            <td>Laporan Inspeksi Mingguan Rotating Equipment</td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalSubmitEvidence" data-deliverable="Laporan Inspeksi Mingguan Rotating Equipment">Submit Evidence</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="pemeliharaan mekanikal rotating equipment sop pompa sentrifugal">
                            <td>SOP Pemeliharaan Pompa Sentrifugal</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="pemeliharaan mekanikal static equipment troubleshooting">
                            <td rowspan="2" class="align-middle">Pemeliharaan Static Equipment</td>
                            <td>Dokumen Troubleshooting Static Equipment</td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalSubmitEvidence" data-deliverable="Dokumen Troubleshooting Static Equipment">Submit Evidence</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="pemeliharaan mekanikal static equipment coaching bejana tekan">
                            <td>Resume Coaching Bejana Tekan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <!-- Citra Lestari: Kompetensi 2 (Operasi), 1 sub, 2 deliverable -->
                        <tr data-search="operasi kilang crude distillation sop">
                            <td rowspan="2" class="align-middle fw-bold">Operasi Kilang Lanjut</td>
                            <td rowspan="2" class="align-middle">Operasi Crude Distillation Unit</td>
                            <td>Standard Operating Procedure CDU</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="operasi kilang crude distillation performance bulanan">
                            <td>Laporan Performance CDU Bulanan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <!-- Dimas Pratama: Kompetensi 3 (QA), 1 sub, 2 deliverable -->
                        <tr data-search="quality assurance inspeksi korosi pipa">
                            <td rowspan="2" class="align-middle fw-bold">Quality Assurance Inspeksi</td>
                            <td class="align-middle">Inspeksi Korosi Pipa</td>
                            <td>Laporan Hasil Inspeksi Pipa Bulan Berjalan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="quality assurance inspeksi bejana tekan sertifikat">
                            <td class="align-middle">Inspeksi Bejana Tekan</td>
                            <td>Sertifikat Inspeksi Bejana Tekan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                    </tbody>
                    <tbody class="table-page-2" style="display:none">
                        <!-- Halaman 2: 4 baris dummy beda -->
                        <tr data-search="pemeliharaan mekanikal rotating equipment alignment shaft">
                            <td rowspan="2" class="align-middle fw-bold">Pemeliharaan Mekanikal</td>
                            <td class="align-middle">Pemeliharaan Rotating Equipment</td>
                            <td>Laporan Alignment Shaft Pompa</td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalSubmitEvidence" data-deliverable="Laporan Alignment Shaft Pompa">Submit Evidence</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="pemeliharaan mekanikal static equipment heat exchanger">
                            <td class="align-middle">Pemeliharaan Static Equipment</td>
                            <td>Resume Coaching Heat Exchanger</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="operasi kilang utility steam generator">
                            <td class="fw-bold">Operasi Utility</td>
                            <td>Operasi Steam Generator</td>
                            <td>SOP Steam Generator Start-up</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                        <tr data-search="quality assurance ndt ultrasonic testing">
                            <td class="fw-bold">Quality Assurance Inspeksi</td>
                            <td>NDT Ultrasonic Testing</td>
                            <td>Laporan UT Pipa Header</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center">
                                <a href="#" onclick="event.preventDefault(); showScreen(2)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Pagination 2 halaman -->
            <nav class="mt-4 d-flex justify-content-between align-items-center">
                <small class="text-muted">Halaman <span class="page-current">1</span> dari 2</small>
                <ul class="pagination pagination-sm mb-0" data-pagination-target="#table-screen-1">
                    <li class="page-item disabled"><span class="page-link">«</span></li>
                    <li class="page-item active" aria-current="page"><a class="page-link" href="#" data-page="1">1</a></li>
                    <li class="page-item"><a class="page-link" href="#" data-page="2">2</a></li>
                    <li class="page-item"><a class="page-link page-next" href="#">»</a></li>
                </ul>
            </nav>
```

- [ ] **Step 3: Tambah filter dropdown handler & search handler di `<script>`**

Sebelum `// Init`, tambah:

```javascript
// === FILTER DROPDOWN demo only ===
document.addEventListener('change', (e) => {
    if (e.target.classList.contains('filter-demo')) {
        showToast('Filter aktif di Portal produksi — demo ini menampilkan data tetap', 'danger');
        e.target.value = '';
    }
});

// === SEARCH BOX (hidup) ===
document.querySelectorAll('.search-input').forEach(input => {
    input.addEventListener('input', () => {
        const tableSelector = input.dataset.target;
        const table = document.querySelector(tableSelector);
        if (!table) return;
        const keyword = input.value.toLowerCase().trim();
        const activeBody = table.querySelector('tbody:not([style*="display:none"])') || table.querySelector('tbody');
        activeBody.querySelectorAll('tr[data-search]').forEach(tr => {
            const haystack = tr.dataset.search || '';
            tr.style.display = (!keyword || haystack.includes(keyword)) ? '' : 'none';
        });
    });
});

// === PAGINATION (2 halaman dummy) ===
document.querySelectorAll('.pagination[data-pagination-target]').forEach(pag => {
    pag.querySelectorAll('a.page-link[data-page]').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const page = parseInt(link.dataset.page);
            const tableSelector = pag.dataset.paginationTarget;
            const table = document.querySelector(tableSelector);
            if (!table) return;
            table.querySelectorAll('tbody').forEach((tb, i) => {
                tb.style.display = (i === page - 1) ? '' : 'none';
            });
            pag.querySelectorAll('.page-item').forEach(li => li.classList.remove('active', 'disabled'));
            pag.querySelectorAll('.page-item').forEach((li, i) => {
                if (li.querySelector('[data-page="' + page + '"]')) li.classList.add('active');
            });
            // Prev disabled di hal 1, Next disabled di hal terakhir
            const totalPages = table.querySelectorAll('tbody').length;
            pag.querySelector('.page-item:first-child').classList.toggle('disabled', page === 1);
            pag.querySelector('.page-item:last-child').classList.toggle('disabled', page === totalPages);
            // Update counter "Halaman X dari N"
            const counter = pag.parentElement.querySelector('.page-current');
            if (counter) counter.textContent = page;
        });
    });
});
```

- [ ] **Step 4: Test di browser**

- Refresh
- Layar 1 tampak konten lengkap: breadcrumb, header, filter bar, 3 stat card, tabel 8 baris, pagination
- Klik tombol "Submit Evidence" → modal tidak buka (belum dibuat Task 5) — tidak apa, akan errornya silent karena modal `#modalSubmitEvidence` belum ada
- Ketik di search box "operasi" → tabel terfilter
- Klik halaman 2 → tabel swap ke 4 baris berbeda
- Klik filter dropdown "Track" → toast merah "Filter aktif di Portal produksi"
- Klik link "Lihat Detail" → pindah ke Layar 2 (masih kosong, tidak masalah)
- Klik tombol "Kembali" / Reset → toast "Demo mode"

- [ ] **Step 5: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 1 Coach View — tabel 12 baris + filter + search + pagination"
```

---

## Task 5: Modal C1 Submit Evidence & Coaching Report

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- Modal C1: Submit Evidence (Task 5) -->`)

- [ ] **Step 1: Replace placeholder dengan modal HTML**

```html
    <!-- Modal C1: Submit Evidence -->
    <div class="modal fade" id="modalSubmitEvidence" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Submit Evidence &amp; Coaching Report</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <!-- Deliverable selector -->
                    <div class="mb-3">
                        <label class="form-label fw-bold">Deliverable</label>
                        <div class="border rounded p-2" style="max-height:200px;overflow-y:auto">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="evDel1" checked>
                                <label class="form-check-label" for="evDel1" id="modalDeliverableLabel">
                                    Laporan Inspeksi Mingguan Rotating Equipment
                                </label>
                            </div>
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="evDel2">
                                <label class="form-check-label" for="evDel2">
                                    Laporan Alignment Shaft Pompa
                                </label>
                            </div>
                        </div>
                    </div>
                    <hr/>
                    <div class="mb-3">
                        <label class="form-label">Tanggal</label>
                        <input type="date" class="form-control" value="2026-05-15"/>
                    </div>
                    <!-- Card Acuan -->
                    <div class="card mb-3">
                        <div class="card-header bg-light">Acuan</div>
                        <div class="card-body">
                            <div class="mb-3">
                                <label class="form-label">Pedoman</label>
                                <textarea class="form-control" rows="2">Pedoman Pemeliharaan Mekanikal KPB Rev. 4 — Bab 3: Pompa Sentrifugal</textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">TKO / TKI / TKPA</label>
                                <textarea class="form-control" rows="2">TKO-MNT-RTE-001: Inspeksi Mingguan Rotating Equipment</textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Best Practice</label>
                                <textarea class="form-control" rows="2">Pertamina RU Best Practice: Vibration analysis tools (CSI 2140) wajib digunakan saat reading vibrasi pompa kritis</textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Dokumen</label>
                                <textarea class="form-control" rows="2">Form Check-Sheet RTE-CS-2026-05, Logbook Operator Pompa Area 12</textarea>
                            </div>
                        </div>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Catatan Coach</label>
                        <textarea class="form-control" rows="3">Coachee menunjukkan pemahaman baik saat membaca vibration spectrum. Perlu pendalaman pada interpretasi misalignment vs unbalance signature. Direkomendasikan praktik langsung pada P-101 minggu depan.</textarea>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Kesimpulan</label>
                        <select class="form-select">
                            <option value="">-- Pilih --</option>
                            <option selected>Kompeten secara mandiri</option>
                            <option>Masih perlu dikembangkan</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Result</label>
                        <select class="form-select">
                            <option value="">-- Pilih --</option>
                            <option>Need Improvement</option>
                            <option>Suitable</option>
                            <option selected>Good</option>
                            <option>Excellence</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">File Evidence (opsional, PDF/JPG/PNG, max 10MB)</label>
                        <input type="file" class="form-control" accept=".pdf,.jpg,.jpeg,.png"/>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                    <button type="button" class="btn btn-primary" id="btnSubmitEvidenceMock">Submit</button>
                </div>
            </div>
        </div>
    </div>
```

- [ ] **Step 2: Tambah handler modal C1 di `<script>`**

Sebelum `// Init`, tambah:

```javascript
// === MODAL C1 Submit Evidence ===
const modalSubmitEv = document.getElementById('modalSubmitEvidence');
if (modalSubmitEv) {
    // Update label deliverable saat modal buka berdasarkan tombol trigger
    modalSubmitEv.addEventListener('show.bs.modal', (event) => {
        const trigger = event.relatedTarget;
        if (trigger && trigger.dataset.deliverable) {
            const label = document.getElementById('modalDeliverableLabel');
            if (label) label.textContent = trigger.dataset.deliverable;
        }
    });
    // Submit mock
    const btn = document.getElementById('btnSubmitEvidenceMock');
    if (btn) {
        btn.addEventListener('click', () => {
            const modal = bootstrap.Modal.getInstance(modalSubmitEv);
            if (modal) modal.hide();
            showToast('Mock: Evidence terkirim — di production akan masuk antrian approval Sr Supervisor', 'success');
        });
    }
}
```

- [ ] **Step 3: Test**

- Refresh
- Layar 1: klik tombol "Submit Evidence" baris pertama (Laporan Inspeksi Mingguan)
- Modal terbuka size-lg, label deliverable = "Laporan Inspeksi Mingguan Rotating Equipment", semua field pre-filled
- Klik Submit → modal tutup → toast hijau di pojok kanan-bawah "Mock: Evidence terkirim..."
- Klik "Submit Evidence" baris ketiga (Dokumen Troubleshooting) → label deliverable berubah sesuai

- [ ] **Step 4: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Modal C1 Submit Evidence dengan form pre-filled + toast mock"
```

---

## Task 6: Layar [2] Detail Pending B1

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 6 -->` di `#screen-2`)

- [ ] **Step 1: Replace placeholder dengan konten Detail Pending**

```html
            <!-- ===== BREADCRUMB ===== -->
            <nav aria-label="breadcrumb" class="mb-3">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" onclick="event.preventDefault(); showScreen(1)">Coaching Proton</a></li>
                    <li class="breadcrumb-item active">Deliverable</li>
                </ol>
            </nav>

            <!-- ===== ROW 1: Side-by-side cards ===== -->
            <div class="row g-3 mb-3">
                <!-- Card 1: Detail Coachee -->
                <div class="col-md-7">
                    <div class="card border-0 shadow-sm mb-3">
                        <div class="card-header bg-white border-bottom">
                            <h6 class="mb-0 fw-semibold">
                                <i class="bi bi-person-lines-fill me-2 text-primary"></i>Detail Coachee &amp; Kompetensi
                            </h6>
                        </div>
                        <div class="card-body">
                            <div class="mb-3"><small class="text-muted">Coachee</small><div class="fw-semibold">Ahmad Budiman</div></div>
                            <div class="mb-3"><small class="text-muted">Track</small><div class="fw-semibold">PROTON Maintenance Tahun 1</div></div>
                            <div class="mb-3"><small class="text-muted">Kompetensi</small><div class="fw-semibold">Pemeliharaan Mekanikal</div></div>
                            <div class="mb-3"><small class="text-muted">Sub Kompetensi</small><div class="fw-semibold">Pemeliharaan Rotating Equipment</div></div>
                            <div class="mb-3"><small class="text-muted">Deliverable</small><div class="fw-semibold">Laporan Inspeksi Mingguan Rotating Equipment</div></div>
                            <div class="mb-3"><small class="text-muted">Anda masuk sebagai</small><div><span class="badge bg-info text-white">Coach</span></div></div>
                            <div class="small text-muted">
                                <i class="bi bi-shield-lock me-1"></i>Hak akses: Coachee (data sendiri), Coach, Sr. Supervisor, Section Head (satu seksi), HC/Admin (semua)
                            </div>
                        </div>
                    </div>
                </div>
                <!-- Card 2: Approval Chain (Pending) -->
                <div class="col-md-5">
                    <div class="card border-0 shadow-sm mb-3">
                        <div class="card-header bg-white border-bottom d-flex justify-content-between align-items-center">
                            <h6 class="mb-0 fw-semibold">
                                <i class="bi bi-diagram-3 me-2 text-primary"></i>Approval Chain
                            </h6>
                            <span class="badge bg-primary px-3 py-2">Submitted</span>
                        </div>
                        <div class="card-body">
                            <div class="position-relative ps-4">
                                <div style="position:absolute;left:11px;top:8px;bottom:8px;width:2px;background:var(--bs-border-color);"></div>
                                <div class="d-flex align-items-start mb-3 position-relative">
                                    <i class="bi bi-circle text-secondary position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">Sr. Supervisor <span class="badge bg-secondary ms-2">Pending</span></div>
                                    </div>
                                </div>
                                <div class="d-flex align-items-start mb-3 position-relative">
                                    <i class="bi bi-circle text-secondary position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">Section Head <span class="badge bg-secondary ms-2">Pending</span></div>
                                    </div>
                                </div>
                                <div class="d-flex align-items-start position-relative">
                                    <i class="bi bi-circle text-secondary position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">HC Review <span class="badge bg-secondary ms-2">Pending</span></div>
                                    </div>
                                </div>
                            </div>
                            <!-- POV Coach: TIDAK ada section Tindakan Approval -->
                        </div>
                    </div>
                </div>
            </div>

            <!-- ===== Card 3: Evidence Coach ===== -->
            <div class="card border-0 shadow-sm mb-3">
                <div class="card-header bg-white border-bottom">
                    <h6 class="mb-0 fw-semibold">
                        <i class="bi bi-file-earmark-text me-2 text-primary"></i>Evidence Coach
                    </h6>
                </div>
                <div class="card-body">
                    <div class="mb-3 p-3 bg-light rounded">
                        <div class="d-flex align-items-center justify-content-between">
                            <div>
                                <i class="bi bi-file-earmark me-2 text-primary"></i>
                                <strong>Evidence:</strong> Dokumen-Inspeksi-Ahmad-15Mei2026.pdf
                            </div>
                            <button class="btn btn-sm btn-outline-primary demo-disabled">
                                <i class="bi bi-download me-1"></i>Download
                            </button>
                        </div>
                        <div class="mt-1"><small class="text-muted">Diupload: 15 Mei 2026 14:32</small></div>
                    </div>
                    <div class="border rounded p-3 mb-2">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <strong><i class="bi bi-person me-1"></i>Eko Wibowo</strong>
                            <small class="text-muted">15 Mei 2026</small>
                            <span>
                                <a href="#" onclick="event.preventDefault(); showScreen(6)" class="btn btn-sm btn-outline-primary me-1" title="Edit Session">
                                    <i class="bi bi-pencil"></i>
                                </a>
                                <button class="btn btn-sm btn-outline-danger demo-disabled" title="Hapus Session">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </span>
                        </div>
                        <table class="table table-sm table-borderless mb-0">
                            <tr><td class="text-muted">Pedoman</td><td>Pedoman Pemeliharaan Mekanikal KPB Rev. 4 — Bab 3: Pompa Sentrifugal</td></tr>
                            <tr><td class="text-muted">TKO / TKI / TKPA</td><td>TKO-MNT-RTE-001: Inspeksi Mingguan Rotating Equipment</td></tr>
                            <tr><td class="text-muted">Best Practice</td><td>Pertamina RU Best Practice: Vibration analysis tools (CSI 2140)</td></tr>
                            <tr><td class="text-muted">Dokumen</td><td>Form Check-Sheet RTE-CS-2026-05, Logbook Operator Pompa Area 12</td></tr>
                            <tr><td class="text-muted">Catatan Coach</td><td>Coachee menunjukkan pemahaman baik saat membaca vibration spectrum. Perlu pendalaman pada interpretasi misalignment vs unbalance signature.</td></tr>
                            <tr><td class="text-muted">Kesimpulan</td><td>Kompeten secara mandiri</td></tr>
                            <tr><td class="text-muted">Result</td><td><span class="badge bg-success">Good</span></td></tr>
                        </table>
                    </div>
                    <div class="mt-3">
                        <button class="btn btn-success demo-disabled">
                            <i class="bi bi-file-pdf me-1"></i>PDF Evidence Report
                        </button>
                    </div>
                </div>
            </div>

            <!-- ===== Card 4: Riwayat Status ===== -->
            <div class="card border-0 shadow-sm mb-3">
                <div class="card-header bg-white border-bottom">
                    <h6 class="mb-0 fw-semibold"><i class="bi bi-clock-history me-2 text-primary"></i>Riwayat Status</h6>
                </div>
                <div class="card-body p-0">
                    <div class="list-group list-group-flush">
                        <div class="list-group-item d-flex align-items-start py-2">
                            <i class="bi bi-plus-circle text-secondary me-3 mt-1" style="font-size:1.1em"></i>
                            <div class="flex-grow-1">
                                <div>Deliverable dibuat</div>
                                <small class="text-muted">10 Mei 2026 08:00</small>
                            </div>
                        </div>
                        <div class="list-group-item d-flex align-items-start py-2">
                            <i class="bi bi-upload text-primary me-3 mt-1" style="font-size:1.1em"></i>
                            <div class="flex-grow-1">
                                <div>Evidence diupload oleh Eko Wibowo (Coach)</div>
                                <small class="text-muted">15 Mei 2026 14:32</small>
                            </div>
                        </div>
                        <div class="list-group-item d-flex align-items-start py-2">
                            <i class="bi bi-chat-square-text text-primary me-3 mt-1" style="font-size:1.1em"></i>
                            <div class="flex-grow-1">
                                <div>Coaching session oleh Eko Wibowo — Kompeten secara mandiri</div>
                                <small class="text-muted">15 Mei 2026 14:45</small>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <button class="btn btn-outline-secondary mt-3 demo-disabled">
                <i class="bi bi-arrow-left me-1"></i>Kembali ke Coaching Proton
            </button>
```

- [ ] **Step 2: Test**

- Refresh, dari Layar 1 klik "Lihat Detail" baris pertama → pindah ke Layar 2
- Layar 2 tampak: breadcrumb 2-level, 2 card row (Detail Coachee + Approval Chain semua Pending), Card Evidence (file + 1 session card + tombol Edit pencil aktif + tombol Hapus mati + tombol PDF Report mati), Card Riwayat 3 event
- Klik tombol Edit pencil → pindah ke Layar 6 (kosong, ok)
- Klik tombol Download / Hapus / PDF Report / Kembali → toast "Demo mode"

- [ ] **Step 3: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 2 Detail Pending B1 POV Coach — chain Pending semua"
```

---

## Task 7: Layar [3] Sr Supervisor View

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 7 -->` di `#screen-3`)

- [ ] **Step 1: Replace dengan konten SrSpv view**

Struktur mirip Layar 1, dengan beda:
- Filter bar tambah dropdown Unit
- Tabel 15 baris (pagination 8+7)
- Kolom Approval SrSpv 3 baris tombol "Tinjau" aktif → trigger modal C2 (akan dibuat Task 8)
- Kolom showCoacheeColumn tampak (Coachee nama di kolom pertama group)

```html
            <!-- ===== BREADCRUMB ===== -->
            <nav aria-label="breadcrumb" class="mb-2">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" class="demo-disabled">CDP</a></li>
                    <li class="breadcrumb-item active">Coaching Proton</li>
                </ol>
            </nav>

            <!-- ===== HEADER ===== -->
            <div class="d-flex justify-content-between align-items-center mb-2">
                <div>
                    <h2><i class="bi bi-graph-up me-2"></i>Coaching Proton</h2>
                    <p class="text-muted mb-0">Monitor deliverable progress and approval status</p>
                </div>
                <a href="#" class="btn btn-outline-secondary demo-disabled">
                    <i class="bi bi-arrow-left me-1"></i>Kembali
                </a>
            </div>

            <!-- ===== FILTER BAR (SrSpv: + Unit) ===== -->
            <div class="card shadow-sm mb-3" style="border:1px solid #e0e0e0;">
                <div class="card-body py-2 px-3">
                    <small class="text-muted fw-semibold text-uppercase d-block mb-2" style="font-size:0.7rem;letter-spacing:0.5px;">Filter</small>
                    <div class="d-flex flex-wrap gap-2 align-items-center">
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:180px">
                            <option value="">Semua Unit</option>
                            <option>Maintenance</option>
                            <option>Operasi</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:180px">
                            <option value="">— Pilih Coachee —</option>
                            <option>Ahmad Budiman [Mekanikal]</option>
                            <option>Citra Lestari [Kilang]</option>
                            <option>Dimas Pratama [Mekanikal]</option>
                            <option>Bayu Saputra [Listrik]</option>
                            <option>Rini Astuti [Utility]</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:140px">
                            <option value="">Semua Track</option>
                            <option>PROTON Maintenance</option>
                            <option>PROTON Operasi</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:130px">
                            <option value="">Semua Tahun</option>
                            <option>Tahun 1</option>
                            <option>Tahun 2</option>
                        </select>
                        <div class="position-relative" style="min-width:200px">
                            <span class="position-absolute top-50 start-0 translate-middle-y ps-2 text-muted">
                                <i class="bi bi-search"></i>
                            </span>
                            <input type="text" class="form-control form-control-sm ps-4 search-input"
                                   data-target="#table-screen-3" placeholder="Cari kompetensi..." autocomplete="off">
                        </div>
                        <button class="btn btn-outline-secondary btn-sm demo-disabled">
                            <i class="bi bi-arrow-counterclockwise"></i> Reset
                        </button>
                    </div>
                </div>
            </div>

            <!-- 3 Stat card (sama struktur Layar 1, beda angka) -->
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-primary) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-graph-up me-1"></i>Progress</h6>
                            <h3 class="mb-0 text-primary">72%</h3>
                            <div class="progress mt-2" style="height: 6px;"><div class="progress-bar" style="width: 72%"></div></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-warning) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-exclamation-triangle me-1"></i>Pending Actions</h6>
                            <h3 class="mb-0 text-warning">5</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-info) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-hourglass-split me-1"></i>Pending Approvals</h6>
                            <h3 class="mb-0 text-info">3</h3>
                        </div>
                    </div>
                </div>
            </div>

            <small class="text-muted mb-2 d-block">
                Menampilkan <span class="rows-counter">1-8</span> dari 15 deliverable
            </small>
            <div class="table-responsive">
                <table class="table table-bordered table-hover" id="table-screen-3">
                    <thead style="background-color: #e8f0fe; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.5px;">
                        <tr>
                            <th>Coachee</th>
                            <th>Kompetensi</th>
                            <th>Sub Kompetensi</th>
                            <th>Deliverable</th>
                            <th>Evidence</th>
                            <th>Approval Sr. Spv</th>
                            <th>Approval Section Head</th>
                            <th>Approval HC</th>
                            <th>Detail</th>
                        </tr>
                    </thead>
                    <tbody class="table-page-1">
                        <!-- Ahmad Budiman 2 rows -->
                        <tr data-search="ahmad budiman pemeliharaan mekanikal rotating equipment">
                            <td rowspan="2" class="align-middle fw-bold">Ahmad Budiman</td>
                            <td rowspan="2" class="align-middle fw-bold">Pemeliharaan Mekanikal</td>
                            <td class="align-middle">Pemeliharaan Rotating Equipment</td>
                            <td>Laporan Inspeksi Mingguan Rotating Equipment</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-warning text-dark" data-bs-toggle="modal" data-bs-target="#modalTinjau"
                                        data-kompetensi="Pemeliharaan Mekanikal" data-subkompetensi="Pemeliharaan Rotating Equipment"
                                        data-deliverable="Laporan Inspeksi Mingguan Rotating Equipment">Tinjau</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="ahmad budiman pemeliharaan mekanikal static equipment troubleshooting">
                            <td class="align-middle">Pemeliharaan Static Equipment</td>
                            <td>Dokumen Troubleshooting Static Equipment</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-warning text-dark" data-bs-toggle="modal" data-bs-target="#modalTinjau"
                                        data-kompetensi="Pemeliharaan Mekanikal" data-subkompetensi="Pemeliharaan Static Equipment"
                                        data-deliverable="Dokumen Troubleshooting Static Equipment">Tinjau</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <!-- Citra Lestari 2 rows (sudah approved) -->
                        <tr data-search="citra lestari operasi kilang crude distillation sop">
                            <td rowspan="2" class="align-middle fw-bold">Citra Lestari</td>
                            <td rowspan="2" class="align-middle fw-bold">Operasi Kilang Lanjut</td>
                            <td rowspan="2" class="align-middle">Operasi Crude Distillation Unit</td>
                            <td>Standard Operating Procedure CDU</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="citra lestari operasi kilang performance bulanan">
                            <td>Laporan Performance CDU Bulanan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center">
                                <button class="btn btn-sm btn-warning text-dark" data-bs-toggle="modal" data-bs-target="#modalTinjau"
                                        data-kompetensi="Operasi Kilang Lanjut" data-subkompetensi="Operasi Crude Distillation Unit"
                                        data-deliverable="Laporan Performance CDU Bulanan">Tinjau</button>
                            </td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <!-- Dimas Pratama 2 rows -->
                        <tr data-search="dimas pratama quality assurance inspeksi korosi pipa">
                            <td rowspan="2" class="align-middle fw-bold">Dimas Pratama</td>
                            <td rowspan="2" class="align-middle fw-bold">Quality Assurance Inspeksi</td>
                            <td class="align-middle">Inspeksi Korosi Pipa</td>
                            <td>Laporan Hasil Inspeksi Pipa Bulan Berjalan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="dimas pratama quality assurance inspeksi bejana tekan">
                            <td class="align-middle">Inspeksi Bejana Tekan</td>
                            <td>Sertifikat Inspeksi Bejana Tekan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <!-- Bayu Saputra 2 rows -->
                        <tr data-search="bayu saputra pemeliharaan listrik motor induksi">
                            <td rowspan="2" class="align-middle fw-bold">Bayu Saputra</td>
                            <td rowspan="2" class="align-middle fw-bold">Pemeliharaan Listrik</td>
                            <td class="align-middle">Pemeliharaan Motor Induksi</td>
                            <td>Laporan IR Test Motor Pompa</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="bayu saputra pemeliharaan listrik transformator">
                            <td class="align-middle">Pemeliharaan Transformator</td>
                            <td>Resume Coaching Transformator</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                    </tbody>
                    <tbody class="table-page-2" style="display:none">
                        <!-- Halaman 2: 7 baris dummy beda (Rini Astuti + sisanya) -->
                        <tr data-search="rini astuti operasi utility steam generator">
                            <td rowspan="3" class="align-middle fw-bold">Rini Astuti</td>
                            <td rowspan="2" class="align-middle fw-bold">Operasi Utility</td>
                            <td rowspan="2" class="align-middle">Operasi Steam Generator</td>
                            <td>SOP Steam Generator Start-up</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="rini astuti operasi utility steam generator shutdown">
                            <td>Laporan Shutdown Steam Generator</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="rini astuti operasi utility cooling water tower">
                            <td class="fw-bold">Operasi Cooling Water</td>
                            <td>Pengaturan Cooling Tower</td>
                            <td>Logbook Operasi Cooling Tower</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="ahmad budiman pemeliharaan rotating equipment alignment">
                            <td class="fw-bold">Ahmad Budiman</td>
                            <td class="fw-bold">Pemeliharaan Mekanikal</td>
                            <td>Pemeliharaan Rotating Equipment</td>
                            <td>Laporan Alignment Shaft Pompa</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="citra lestari operasi kilang vacuum distillation">
                            <td class="fw-bold">Citra Lestari</td>
                            <td class="fw-bold">Operasi Kilang Lanjut</td>
                            <td>Operasi Vacuum Distillation Unit</td>
                            <td>SOP Vacuum Distillation Unit</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="dimas pratama quality assurance ndt ultrasonic">
                            <td class="fw-bold">Dimas Pratama</td>
                            <td class="fw-bold">Quality Assurance Inspeksi</td>
                            <td>NDT Ultrasonic Testing</td>
                            <td>Laporan UT Pipa Header</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="bayu saputra pemeliharaan listrik panel mcc">
                            <td class="fw-bold">Bayu Saputra</td>
                            <td class="fw-bold">Pemeliharaan Listrik</td>
                            <td>Pemeliharaan Panel MCC</td>
                            <td>Resume Coaching Panel MCC</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <nav class="mt-4 d-flex justify-content-between align-items-center">
                <small class="text-muted">Halaman <span class="page-current">1</span> dari 2</small>
                <ul class="pagination pagination-sm mb-0" data-pagination-target="#table-screen-3">
                    <li class="page-item disabled"><span class="page-link">«</span></li>
                    <li class="page-item active" aria-current="page"><a class="page-link" href="#" data-page="1">1</a></li>
                    <li class="page-item"><a class="page-link" href="#" data-page="2">2</a></li>
                    <li class="page-item"><a class="page-link">»</a></li>
                </ul>
            </nav>
```

- [ ] **Step 2: Test**

- Refresh, navigasi ke Layar 3
- Tampak: filter bar dengan Unit dropdown, 3 stat card (Progress 72%, Pending 5, Approvals 3), tabel 8 baris hal 1, kolom Coachee tampak
- Tombol "Tinjau" warna kuning 3 buah pada baris status Submitted → klik = modal belum ada (silent, akan dibuat Task 8)
- Klik "Lihat Detail" → pindah Layar 4

- [ ] **Step 3: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 3 SrSpv View — 15 baris + Unit dropdown + tombol Tinjau"
```

---

## Task 8: Modal C2 Tinjau Deliverable

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- Modal C2: Tinjau Deliverable (Task 8) -->`)

- [ ] **Step 1: Replace placeholder dengan modal HTML**

```html
    <!-- Modal C2: Tinjau Deliverable -->
    <div class="modal fade" id="modalTinjau" tabindex="-1" aria-labelledby="modalTinjauLabel">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="modalTinjauLabel">Tinjau Deliverable</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <p><strong>Kompetensi:</strong> <span id="tinjauKompetensi"></span></p>
                    <p><strong>Sub-Kompetensi:</strong> <span id="tinjauSubKompetensi"></span></p>
                    <p><strong>Deliverable:</strong> <span id="tinjauDeliverable"></span></p>
                    <p><strong>Evidence:</strong> <a href="#" class="demo-disabled" id="tinjauEvidenceLink">Lihat Evidence</a></p>
                    <div class="mb-3">
                        <label class="form-label">Aksi</label>
                        <select class="form-select" id="tinjauAction">
                            <option value="">-- Pilih Aksi --</option>
                            <option value="approve">Approve</option>
                            <option value="reject">Reject</option>
                        </select>
                    </div>
                    <div class="mb-3 d-none" id="tinjauCommentGroup">
                        <label class="form-label">Komentar <span id="tinjauCommentRequired" class="text-danger d-none">*</span></label>
                        <textarea class="form-control" id="tinjauComment" rows="3" placeholder="Komentar..."></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                    <button type="button" class="btn btn-primary" id="btnTinjauSubmit" disabled>Submit</button>
                </div>
            </div>
        </div>
    </div>
```

- [ ] **Step 2: Tambah handler modal C2 di `<script>`**

```javascript
// === MODAL C2 Tinjau ===
const modalTinjau = document.getElementById('modalTinjau');
const tinjauActionEl = document.getElementById('tinjauAction');
const tinjauCommentGroup = document.getElementById('tinjauCommentGroup');
const tinjauCommentRequired = document.getElementById('tinjauCommentRequired');
const tinjauCommentEl = document.getElementById('tinjauComment');
const btnTinjauSubmit = document.getElementById('btnTinjauSubmit');

if (modalTinjau) {
    modalTinjau.addEventListener('show.bs.modal', (event) => {
        const trigger = event.relatedTarget;
        if (!trigger) return;
        document.getElementById('tinjauKompetensi').textContent = trigger.dataset.kompetensi || '';
        document.getElementById('tinjauSubKompetensi').textContent = trigger.dataset.subkompetensi || '';
        document.getElementById('tinjauDeliverable').textContent = trigger.dataset.deliverable || '';
        // Reset state
        tinjauActionEl.value = '';
        tinjauCommentGroup.classList.add('d-none');
        tinjauCommentEl.value = '';
        btnTinjauSubmit.disabled = true;
    });
}

if (tinjauActionEl) {
    tinjauActionEl.addEventListener('change', function() {
        const action = this.value;
        if (action === 'reject') {
            tinjauCommentGroup.classList.remove('d-none');
            tinjauCommentRequired.classList.remove('d-none');
            tinjauCommentEl.placeholder = 'Masukkan alasan penolakan...';
        } else if (action === 'approve') {
            tinjauCommentGroup.classList.remove('d-none');
            tinjauCommentRequired.classList.add('d-none');
            tinjauCommentEl.placeholder = 'Komentar (opsional)';
        } else {
            tinjauCommentGroup.classList.add('d-none');
        }
        btnTinjauSubmit.disabled = !action;
    });
}

if (btnTinjauSubmit) {
    btnTinjauSubmit.addEventListener('click', () => {
        const action = tinjauActionEl.value;
        const comment = tinjauCommentEl.value.trim();
        if (action === 'reject' && !comment) {
            showToast('Alasan penolakan wajib diisi', 'danger');
            return;
        }
        const modal = bootstrap.Modal.getInstance(modalTinjau);
        if (modal) modal.hide();
        if (action === 'approve') {
            showToast('Mock: Deliverable disetujui Sr Supervisor — selanjutnya menunggu Section Head', 'success');
        } else {
            showToast('Mock: Deliverable ditolak — Coach perlu re-submit', 'danger');
        }
    });
}
```

- [ ] **Step 3: Test**

- Refresh, navigasi ke Layar 3
- Klik tombol "Tinjau" kuning baris 1 → modal terbuka, info "Pemeliharaan Mekanikal / Pemeliharaan Rotating Equipment / Laporan Inspeksi Mingguan"
- Submit disabled (Aksi belum dipilih)
- Pilih Approve → komentar muncul (opsional), Submit aktif
- Klik Submit → modal tutup → toast hijau "Mock: Deliverable disetujui..."
- Buka lagi → pilih Reject tanpa komentar → klik Submit → toast merah "Alasan penolakan wajib diisi" (modal masih buka)
- Isi komentar + Submit → modal tutup → toast merah "Mock: Deliverable ditolak..."

- [ ] **Step 4: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Modal C2 Tinjau dengan Approve/Reject + validation komentar"
```

---

## Task 9: Layar [4] Detail Approved B1

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 9 -->` di `#screen-4`)

- [ ] **Step 1: Replace placeholder**

Struktur 100% sama Layar 2 tapi Approval Chain 3 hijau + Riwayat 6 event:

```html
            <!-- ===== BREADCRUMB ===== -->
            <nav aria-label="breadcrumb" class="mb-3">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" onclick="event.preventDefault(); showScreen(3)">Coaching Proton</a></li>
                    <li class="breadcrumb-item active">Deliverable</li>
                </ol>
            </nav>

            <div class="row g-3 mb-3">
                <!-- Card 1: Detail Coachee (sama Layar 2) -->
                <div class="col-md-7">
                    <div class="card border-0 shadow-sm mb-3">
                        <div class="card-header bg-white border-bottom">
                            <h6 class="mb-0 fw-semibold"><i class="bi bi-person-lines-fill me-2 text-primary"></i>Detail Coachee &amp; Kompetensi</h6>
                        </div>
                        <div class="card-body">
                            <div class="mb-3"><small class="text-muted">Coachee</small><div class="fw-semibold">Ahmad Budiman</div></div>
                            <div class="mb-3"><small class="text-muted">Track</small><div class="fw-semibold">PROTON Maintenance Tahun 1</div></div>
                            <div class="mb-3"><small class="text-muted">Kompetensi</small><div class="fw-semibold">Pemeliharaan Mekanikal</div></div>
                            <div class="mb-3"><small class="text-muted">Sub Kompetensi</small><div class="fw-semibold">Pemeliharaan Static Equipment</div></div>
                            <div class="mb-3"><small class="text-muted">Deliverable</small><div class="fw-semibold">Resume Coaching Bejana Tekan</div></div>
                            <div class="mb-3"><small class="text-muted">Anda masuk sebagai</small><div><span class="badge bg-info text-white">Coach</span></div></div>
                            <div class="small text-muted">
                                <i class="bi bi-shield-lock me-1"></i>Hak akses: Coachee (data sendiri), Coach, Sr. Supervisor, Section Head (satu seksi), HC/Admin (semua)
                            </div>
                        </div>
                    </div>
                </div>
                <!-- Card 2: Approval Chain APPROVED -->
                <div class="col-md-5">
                    <div class="card border-0 shadow-sm mb-3">
                        <div class="card-header bg-white border-bottom d-flex justify-content-between align-items-center">
                            <h6 class="mb-0 fw-semibold"><i class="bi bi-diagram-3 me-2 text-primary"></i>Approval Chain</h6>
                            <span class="badge bg-success px-3 py-2">Approved</span>
                        </div>
                        <div class="card-body">
                            <div class="position-relative ps-4">
                                <div style="position:absolute;left:11px;top:8px;bottom:8px;width:2px;background:var(--bs-border-color);"></div>
                                <div class="d-flex align-items-start mb-3 position-relative">
                                    <i class="bi bi-check-circle-fill text-success position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">Sr. Supervisor <span class="badge bg-success ms-2">Approved</span></div>
                                        <small class="text-muted">Fajar Hidayat</small>
                                        <small class="text-muted ms-2">16 Mei 2026 10:30</small>
                                    </div>
                                </div>
                                <div class="d-flex align-items-start mb-3 position-relative">
                                    <i class="bi bi-check-circle-fill text-success position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">Section Head <span class="badge bg-success ms-2">Approved</span></div>
                                        <small class="text-muted">Gita Sari</small>
                                        <small class="text-muted ms-2">17 Mei 2026 09:15</small>
                                    </div>
                                </div>
                                <div class="d-flex align-items-start position-relative">
                                    <i class="bi bi-check-circle-fill text-success position-absolute" style="left:-27px;top:2px;font-size:1.2em;background:#fff;z-index:1;"></i>
                                    <div>
                                        <div class="fw-semibold">HC Review <span class="badge bg-success ms-2">Reviewed</span></div>
                                        <small class="text-muted">Hadi Nugroho</small>
                                        <small class="text-muted ms-2">18 Mei 2026 14:00</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Card 3: Evidence (sama Layar 2) -->
            <div class="card border-0 shadow-sm mb-3">
                <div class="card-header bg-white border-bottom">
                    <h6 class="mb-0 fw-semibold"><i class="bi bi-file-earmark-text me-2 text-primary"></i>Evidence Coach</h6>
                </div>
                <div class="card-body">
                    <div class="mb-3 p-3 bg-light rounded">
                        <div class="d-flex align-items-center justify-content-between">
                            <div><i class="bi bi-file-earmark me-2 text-primary"></i><strong>Evidence:</strong> Dokumen-Coaching-Bejana-Ahmad.pdf</div>
                            <button class="btn btn-sm btn-outline-primary demo-disabled"><i class="bi bi-download me-1"></i>Download</button>
                        </div>
                        <div class="mt-1"><small class="text-muted">Diupload: 15 Mei 2026 14:32</small></div>
                    </div>
                    <div class="border rounded p-3 mb-2">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <strong><i class="bi bi-person me-1"></i>Eko Wibowo</strong>
                            <small class="text-muted">15 Mei 2026</small>
                            <span>
                                <a href="#" onclick="event.preventDefault(); showScreen(6)" class="btn btn-sm btn-outline-primary me-1" title="Edit Session"><i class="bi bi-pencil"></i></a>
                                <button class="btn btn-sm btn-outline-danger demo-disabled" title="Hapus Session"><i class="bi bi-trash"></i></button>
                            </span>
                        </div>
                        <table class="table table-sm table-borderless mb-0">
                            <tr><td class="text-muted">Pedoman</td><td>Pedoman Inspeksi Bejana Tekan KPB Rev. 3</td></tr>
                            <tr><td class="text-muted">TKO / TKI / TKPA</td><td>TKO-INSP-BJN-001: Visual Inspection Bejana Tekan</td></tr>
                            <tr><td class="text-muted">Best Practice</td><td>API 510 In-service Inspection Pressure Vessels</td></tr>
                            <tr><td class="text-muted">Dokumen</td><td>Inspection Record Sheet IRS-BJN-2026-014</td></tr>
                            <tr><td class="text-muted">Catatan Coach</td><td>Coachee paham prosedur visual inspection bejana tekan dengan baik, mampu identifikasi tanda-tanda korosi external dan internal. Demonstrasi praktik di V-102 berjalan lancar.</td></tr>
                            <tr><td class="text-muted">Kesimpulan</td><td>Kompeten secara mandiri</td></tr>
                            <tr><td class="text-muted">Result</td><td><span class="badge bg-success">Good</span></td></tr>
                        </table>
                    </div>
                    <div class="mt-3">
                        <button class="btn btn-success demo-disabled"><i class="bi bi-file-pdf me-1"></i>PDF Evidence Report</button>
                    </div>
                </div>
            </div>

            <!-- Card 4: Riwayat (6 event) -->
            <div class="card border-0 shadow-sm mb-3">
                <div class="card-header bg-white border-bottom">
                    <h6 class="mb-0 fw-semibold"><i class="bi bi-clock-history me-2 text-primary"></i>Riwayat Status</h6>
                </div>
                <div class="card-body p-0">
                    <div class="list-group list-group-flush">
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-plus-circle text-secondary me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>Deliverable dibuat</div><small class="text-muted">08 Mei 2026 08:00</small></div></div>
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-upload text-primary me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>Evidence diupload oleh Eko Wibowo (Coach)</div><small class="text-muted">15 Mei 2026 14:32</small></div></div>
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-chat-square-text text-primary me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>Coaching session oleh Eko Wibowo — Kompeten secara mandiri</div><small class="text-muted">15 Mei 2026 14:45</small></div></div>
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-check-circle text-success me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>Sr. Supervisor (Fajar Hidayat) — disetujui</div><small class="text-muted">16 Mei 2026 10:30</small></div></div>
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-check-circle text-success me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>Section Head (Gita Sari) — disetujui</div><small class="text-muted">17 Mei 2026 09:15</small></div></div>
                        <div class="list-group-item d-flex align-items-start py-2"><i class="bi bi-person-check text-info me-3 mt-1" style="font-size:1.1em"></i><div class="flex-grow-1"><div>HC Review (Hadi Nugroho)</div><small class="text-muted">18 Mei 2026 14:00</small></div></div>
                    </div>
                </div>
            </div>

            <button class="btn btn-outline-secondary mt-3 demo-disabled"><i class="bi bi-arrow-left me-1"></i>Kembali ke Coaching Proton</button>
```

- [ ] **Step 2: Test**

- Refresh, navigasi ke Layar 4
- Tampak: Approval Chain 3 step semua hijau dengan nama approver + timestamp, Card Evidence dengan badge Result Good, Card Riwayat 6 event
- Klik tombol Edit pencil session → pindah Layar 6

- [ ] **Step 3: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 4 Detail Approved B1 — chain 3 step hijau + riwayat 6 event"
```

---

## Task 10: Layar [5] HC View + HC Pending Review Panel

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 10 -->` di `#screen-5`)

- [ ] **Step 1: Replace placeholder**

```html
            <!-- BREADCRUMB -->
            <nav aria-label="breadcrumb" class="mb-2">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" class="demo-disabled">CDP</a></li>
                    <li class="breadcrumb-item active">Coaching Proton</li>
                </ol>
            </nav>

            <div class="d-flex justify-content-between align-items-center mb-2">
                <div>
                    <h2><i class="bi bi-graph-up me-2"></i>Coaching Proton</h2>
                    <p class="text-muted mb-0">Monitor deliverable progress and approval status</p>
                </div>
                <a href="#" class="btn btn-outline-secondary demo-disabled"><i class="bi bi-arrow-left me-1"></i>Kembali</a>
            </div>

            <!-- FILTER BAR (HC: + Bagian) -->
            <div class="card shadow-sm mb-3" style="border:1px solid #e0e0e0;">
                <div class="card-body py-2 px-3">
                    <small class="text-muted fw-semibold text-uppercase d-block mb-2" style="font-size:0.7rem;letter-spacing:0.5px;">Filter</small>
                    <div class="d-flex flex-wrap gap-2 align-items-center">
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:150px">
                            <option value="">Semua Bagian</option>
                            <option>Operasi Kilang</option>
                            <option>Maintenance</option>
                            <option>Quality Assurance</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:180px">
                            <option value="">Semua Unit</option>
                            <option>Maintenance</option>
                            <option>Operasi</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:180px">
                            <option value="">— Pilih Coachee —</option>
                            <option>Ahmad Budiman</option>
                            <option>Citra Lestari</option>
                            <option>Dimas Pratama</option>
                            <option>Bayu Saputra</option>
                            <option>Rini Astuti</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:140px">
                            <option value="">Semua Track</option>
                            <option>PROTON Maintenance</option>
                            <option>PROTON Operasi</option>
                        </select>
                        <select class="form-select form-select-sm filter-demo" style="width:auto;min-width:130px">
                            <option value="">Semua Tahun</option>
                            <option>Tahun 1</option>
                            <option>Tahun 2</option>
                        </select>
                        <div class="position-relative" style="min-width:200px">
                            <span class="position-absolute top-50 start-0 translate-middle-y ps-2 text-muted"><i class="bi bi-search"></i></span>
                            <input type="text" class="form-control form-control-sm ps-4 search-input" data-target="#table-screen-5" placeholder="Cari kompetensi..." autocomplete="off">
                        </div>
                        <button class="btn btn-outline-secondary btn-sm demo-disabled"><i class="bi bi-arrow-counterclockwise"></i> Reset</button>
                    </div>
                    <!-- Export HC tools -->
                    <hr class="my-2">
                    <span class="text-muted small me-2">Laporan:</span>
                    <button class="btn btn-sm btn-outline-warning me-1 demo-disabled"><i class="bi bi-file-earmark-excel me-1"></i>Bottleneck Report</button>
                    <button class="btn btn-sm btn-outline-warning me-1 demo-disabled"><i class="bi bi-file-earmark-excel me-1"></i>Coaching Tracking</button>
                    <button class="btn btn-sm btn-outline-warning demo-disabled"><i class="bi bi-file-earmark-excel me-1"></i>Workload Summary</button>
                </div>
            </div>

            <!-- 3 Stat card -->
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-primary) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-graph-up me-1"></i>Progress</h6>
                            <h3 class="mb-0 text-primary">68%</h3>
                            <div class="progress mt-2" style="height: 6px;"><div class="progress-bar" style="width: 68%"></div></div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-warning) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-exclamation-triangle me-1"></i>Pending Actions</h6>
                            <h3 class="mb-0 text-warning">8</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm" style="border:none; border-left: 4px solid var(--bs-info) !important;">
                        <div class="card-body text-center">
                            <h6 class="text-muted mb-1"><i class="bi bi-hourglass-split me-1"></i>Pending Approvals</h6>
                            <h3 class="mb-0 text-info">3</h3>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Batch Approve button (muncul kalau ada centang) -->
            <div class="mb-2">
                <button id="batchApproveBtn" class="btn btn-success btn-sm" style="display:none;" data-bs-toggle="modal" data-bs-target="#modalBatchApprove">
                    Approve Selected (<span id="batchSelectedCount">0</span>)
                </button>
            </div>

            <small class="text-muted mb-2 d-block">Menampilkan 1-8 dari 12 deliverable</small>
            <div class="table-responsive">
                <table class="table table-bordered table-hover" id="table-screen-5">
                    <thead style="background-color: #e8f0fe; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.5px;">
                        <tr>
                            <th style="width:36px"><input type="checkbox" id="selectAllHc" title="Pilih semua"/></th>
                            <th>Coachee</th>
                            <th>Kompetensi</th>
                            <th>Sub Kompetensi</th>
                            <th>Deliverable</th>
                            <th>Evidence</th>
                            <th>Approval Sr. Spv</th>
                            <th>Approval Section Head</th>
                            <th>Approval HC</th>
                            <th>Detail</th>
                        </tr>
                    </thead>
                    <tbody class="table-page-1">
                        <tr data-search="ahmad budiman pemeliharaan mekanikal static equipment bejana">
                            <td class="text-center align-middle"><input type="checkbox" class="batch-check" data-deliverable="Resume Coaching Bejana Tekan" data-coachee="Ahmad Budiman"></td>
                            <td class="fw-bold">Ahmad Budiman</td>
                            <td class="fw-bold">Pemeliharaan Mekanikal</td>
                            <td>Pemeliharaan Static Equipment</td>
                            <td>Resume Coaching Bejana Tekan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span><br><button class="btn btn-sm btn-outline-success btnHcReviewMock mt-1">Review</button></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="citra lestari operasi kilang crude distillation performance">
                            <td class="text-center align-middle"><input type="checkbox" class="batch-check" data-deliverable="Laporan Performance CDU Bulanan" data-coachee="Citra Lestari"></td>
                            <td class="fw-bold">Citra Lestari</td>
                            <td class="fw-bold">Operasi Kilang Lanjut</td>
                            <td>Operasi Crude Distillation Unit</td>
                            <td>Laporan Performance CDU Bulanan</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span><br><button class="btn btn-sm btn-outline-success btnHcReviewMock mt-1">Review</button></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="bayu saputra pemeliharaan listrik motor induksi">
                            <td class="text-center align-middle"><input type="checkbox" class="batch-check" data-deliverable="Laporan IR Test Motor Pompa" data-coachee="Bayu Saputra"></td>
                            <td class="fw-bold">Bayu Saputra</td>
                            <td class="fw-bold">Pemeliharaan Listrik</td>
                            <td>Pemeliharaan Motor Induksi</td>
                            <td>Laporan IR Test Motor Pompa</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><span class="badge bg-secondary">Pending</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="rini astuti operasi utility cooling water">
                            <td class="text-center align-middle"></td>
                            <td class="fw-bold">Rini Astuti</td>
                            <td class="fw-bold">Operasi Utility</td>
                            <td>Operasi Cooling Water</td>
                            <td>Logbook Operasi Cooling Tower</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                        <tr data-search="dimas pratama quality assurance ndt ultrasonic">
                            <td class="text-center align-middle"></td>
                            <td class="fw-bold">Dimas Pratama</td>
                            <td class="fw-bold">Quality Assurance Inspeksi</td>
                            <td>NDT Ultrasonic Testing</td>
                            <td>Laporan UT Pipa Header</td>
                            <td class="text-center"><span class="badge bg-success">Sudah Upload</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Approved</span></td>
                            <td class="text-center"><span class="badge bg-success">Reviewed</span></td>
                            <td class="text-center"><a href="#" onclick="event.preventDefault(); showScreen(4)" class="btn btn-sm btn-outline-secondary">Lihat Detail</a></td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- HC Pending Review Panel -->
            <div class="mt-4">
                <div class="card border-0 shadow-sm">
                    <div class="card-header d-flex justify-content-between align-items-center py-2" style="cursor:pointer" data-bs-toggle="collapse" data-bs-target="#hcReviewPanelBody" aria-expanded="true">
                        <h6 class="mb-0">
                            <i class="bi bi-clipboard-check me-2 text-success"></i>Antrian Review HC
                            <span class="badge bg-warning text-dark ms-2">3 pending</span>
                        </h6>
                        <i class="bi bi-chevron-down text-muted small"></i>
                    </div>
                    <div class="collapse show" id="hcReviewPanelBody">
                        <div class="card-body p-0">
                            <div class="table-responsive">
                                <table class="table table-sm table-hover mb-0">
                                    <thead style="background-color: #e8f0fe; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.5px;">
                                        <tr>
                                            <th>Coachee</th>
                                            <th>Kompetensi</th>
                                            <th>Sub-Kompetensi</th>
                                            <th>Deliverable</th>
                                            <th class="text-center">Status</th>
                                            <th class="text-center">Tanggal Submit</th>
                                            <th class="text-center">Aksi</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr>
                                            <td>Ahmad Budiman</td>
                                            <td class="small text-muted">Pemeliharaan Mekanikal</td>
                                            <td class="small text-muted">Pemeliharaan Static Equipment</td>
                                            <td>Resume Coaching Bejana Tekan</td>
                                            <td class="text-center"><span class="badge bg-primary">Submitted</span></td>
                                            <td class="text-center small text-muted">15 Mei 2026</td>
                                            <td class="text-center"><button class="btn btn-sm btn-outline-success btnHcReviewPanelMock"><i class="bi bi-check-circle me-1"></i>Review</button></td>
                                        </tr>
                                        <tr>
                                            <td>Citra Lestari</td>
                                            <td class="small text-muted">Operasi Kilang Lanjut</td>
                                            <td class="small text-muted">Operasi Crude Distillation Unit</td>
                                            <td>Laporan Performance CDU Bulanan</td>
                                            <td class="text-center"><span class="badge bg-primary">Submitted</span></td>
                                            <td class="text-center small text-muted">17 Mei 2026</td>
                                            <td class="text-center"><button class="btn btn-sm btn-outline-success btnHcReviewPanelMock"><i class="bi bi-check-circle me-1"></i>Review</button></td>
                                        </tr>
                                        <tr>
                                            <td>Bayu Saputra</td>
                                            <td class="small text-muted">Pemeliharaan Listrik</td>
                                            <td class="small text-muted">Pemeliharaan Transformator</td>
                                            <td>Resume Coaching Transformator</td>
                                            <td class="text-center"><span class="badge bg-primary">Submitted</span></td>
                                            <td class="text-center small text-muted">18 Mei 2026</td>
                                            <td class="text-center"><button class="btn btn-sm btn-outline-success btnHcReviewPanelMock"><i class="bi bi-check-circle me-1"></i>Review</button></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div class="px-3 py-2 text-muted small border-top">
                                <i class="bi bi-info-circle me-1"></i>Setelah diklik Review, deliverable ditandai sebagai sudah diperiksa HC. Status tidak berubah.
                            </div>
                        </div>
                    </div>
                </div>
            </div>
```

- [ ] **Step 2: Tambah handler checkbox batch + tombol Review HC di `<script>`**

```javascript
// === HC View: Checkbox batch + tombol Review inline ===
document.addEventListener('change', (e) => {
    if (e.target.classList.contains('batch-check')) {
        const checked = document.querySelectorAll('.batch-check:checked').length;
        const btn = document.getElementById('batchApproveBtn');
        const counter = document.getElementById('batchSelectedCount');
        if (btn && counter) {
            counter.textContent = checked;
            btn.style.display = checked > 0 ? 'inline-block' : 'none';
        }
    }
    if (e.target.id === 'selectAllHc') {
        const checkAll = e.target.checked;
        document.querySelectorAll('.batch-check').forEach(cb => cb.checked = checkAll);
        const checked = document.querySelectorAll('.batch-check:checked').length;
        const btn = document.getElementById('batchApproveBtn');
        const counter = document.getElementById('batchSelectedCount');
        if (btn && counter) {
            counter.textContent = checked;
            btn.style.display = checked > 0 ? 'inline-block' : 'none';
        }
    }
});

// Tombol Review HC inline
document.querySelectorAll('.btnHcReviewMock, .btnHcReviewPanelMock').forEach(btn => {
    btn.addEventListener('click', () => {
        showToast('Mock: Deliverable ditandai sudah di-review HC', 'success');
    });
});
```

- [ ] **Step 3: Test**

- Refresh, navigasi ke Layar 5
- Tampak: filter bar lengkap (Bagian + Unit + Coachee + Track + Tahun + search), tombol export HC, 3 stat card, tabel 5 baris dengan checkbox di kolom 1, HC Pending Review Panel terbuka dengan 3 baris
- Centang 1 checkbox → tombol "Approve Selected (1)" muncul hijau
- Centang 3 checkbox → tombol jadi "(3)"
- Klik "Review" inline di tabel utama atau di panel → toast "Mock: di-review"
- Klik selectAll → semua tercentang

- [ ] **Step 4: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 5 HC View — checkbox batch + HC Pending Review Panel"
```

---

## Task 11: Modal C3 Batch HC Approve

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- Modal C3: Batch HC Approve (Task 11) -->`)

- [ ] **Step 1: Replace placeholder dengan modal HTML**

```html
    <!-- Modal C3: Batch HC Approve -->
    <div class="modal fade" id="modalBatchApprove" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Konfirmasi Batch Approve HC</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <p>Approve <strong><span id="batchApproveModalCount">0</span></strong> deliverable berikut?</p>
                    <ul id="batchApproveModalList" style="max-height:200px;overflow-y:auto;font-size:0.9em"></ul>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
                    <button type="button" class="btn btn-success btn-sm" id="btnBatchApproveConfirm">Approve</button>
                </div>
            </div>
        </div>
    </div>
```

- [ ] **Step 2: Tambah handler modal C3 di `<script>`**

```javascript
// === MODAL C3 Batch HC Approve ===
const modalBatchApprove = document.getElementById('modalBatchApprove');
if (modalBatchApprove) {
    modalBatchApprove.addEventListener('show.bs.modal', () => {
        const checked = document.querySelectorAll('.batch-check:checked');
        document.getElementById('batchApproveModalCount').textContent = checked.length;
        const list = document.getElementById('batchApproveModalList');
        list.innerHTML = '';
        checked.forEach(cb => {
            const li = document.createElement('li');
            li.textContent = (cb.dataset.coachee || '?') + ' — ' + (cb.dataset.deliverable || '?');
            list.appendChild(li);
        });
    });
    const btnConfirm = document.getElementById('btnBatchApproveConfirm');
    if (btnConfirm) {
        btnConfirm.addEventListener('click', () => {
            const checked = document.querySelectorAll('.batch-check:checked');
            const count = checked.length;
            // Reset checkboxes + hide batch button
            checked.forEach(cb => cb.checked = false);
            const selectAllCb = document.getElementById('selectAllHc');
            if (selectAllCb) selectAllCb.checked = false;
            const batchBtn = document.getElementById('batchApproveBtn');
            if (batchBtn) batchBtn.style.display = 'none';
            // Hide modal + toast
            const modal = bootstrap.Modal.getInstance(modalBatchApprove);
            if (modal) modal.hide();
            showToast('Mock: ' + count + ' deliverable ditandai sudah di-review HC', 'success');
        });
    }
}
```

- [ ] **Step 3: Test**

- Refresh, navigasi ke Layar 5
- Centang 3 checkbox → klik "Approve Selected (3)" → modal terbuka, list 3 nama coachee + deliverable
- Klik Approve → modal tutup → toast "Mock: 3 deliverable ditandai..." → checkbox reset + tombol Batch hilang

- [ ] **Step 4: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Modal C3 Batch HC Approve — list + confirm + reset checkbox"
```

---

## Task 12: Layar [6] Edit Session B2 (Read-Only Pre-Filled)

**Files:**
- Modify: `docs/mockup-presentasi/coaching-proton-mockup.html` (replace `<!-- TODO Task 12 -->` di `#screen-6`)

- [ ] **Step 1: Replace placeholder**

```html
            <!-- BREADCRUMB -->
            <nav aria-label="breadcrumb" class="mb-3">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="#" onclick="event.preventDefault(); showScreen(1)">Coaching Proton</a></li>
                    <li class="breadcrumb-item"><a href="#" onclick="event.preventDefault(); showScreen(2)">Deliverable</a></li>
                    <li class="breadcrumb-item active">Edit Sesi Coaching</li>
                </ol>
            </nav>

            <div class="card shadow-sm">
                <div class="card-header bg-primary text-white">
                    <h5 class="mb-0"><i class="bi bi-pencil-square me-2"></i>Edit Sesi Coaching</h5>
                </div>
                <div class="card-body">
                    <!-- Read-only block -->
                    <div class="row mb-4">
                        <div class="col-md-6"><div class="mb-3"><label class="form-label fw-semibold text-muted">Tanggal Sesi</label><p class="form-control-plaintext">15 Mei 2026</p></div></div>
                        <div class="col-md-6"><div class="mb-3"><label class="form-label fw-semibold text-muted">Kompetensi</label><p class="form-control-plaintext">Pemeliharaan Mekanikal</p></div></div>
                        <div class="col-md-6"><div class="mb-3"><label class="form-label fw-semibold text-muted">Sub Kompetensi</label><p class="form-control-plaintext">Pemeliharaan Static Equipment</p></div></div>
                        <div class="col-md-6"><div class="mb-3"><label class="form-label fw-semibold text-muted">Deliverable</label><p class="form-control-plaintext">Resume Coaching Bejana Tekan</p></div></div>
                    </div>
                    <hr/>
                    <!-- Form pre-filled (display only) -->
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Catatan Coach</label>
                        <textarea class="form-control" rows="5" readonly>Coachee paham prosedur visual inspection bejana tekan dengan baik, mampu identifikasi tanda-tanda korosi external dan internal. Demonstrasi praktik di V-102 berjalan lancar. Direkomendasikan pendalaman pada interpretasi RT/UT report minggu depan.</textarea>
                        <div class="form-text">Isi catatan pembinaan, observasi, dan rekomendasi untuk coachee.</div>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Kesimpulan</label>
                        <select class="form-select" disabled>
                            <option selected>Kompeten</option>
                            <option>Perlu Pengembangan</option>
                        </select>
                    </div>
                    <div class="mb-4">
                        <label class="form-label fw-semibold">Result</label>
                        <select class="form-select" disabled>
                            <option>Need Improvement</option>
                            <option>Suitable</option>
                            <option selected>Good</option>
                            <option>Excellence</option>
                        </select>
                    </div>
                    <div class="d-flex gap-2">
                        <button type="button" class="btn btn-primary demo-disabled"><i class="bi bi-save me-1"></i>Simpan Perubahan</button>
                        <button type="button" class="btn btn-outline-secondary demo-disabled"><i class="bi bi-x-circle me-1"></i>Batal</button>
                    </div>
                </div>
            </div>
```

- [ ] **Step 2: Test**

- Refresh, navigasi ke Layar 6
- Tampak: breadcrumb 3-level, header biru, 4 read-only field, textarea Catatan (readonly), 2 dropdown disabled, tombol Simpan & Batal mati
- Klik Simpan/Batal → toast "Demo mode"
- Coba klik di textarea → tidak bisa diedit (readonly)
- Klik dropdown → tidak bisa diubah (disabled)
- Klik breadcrumb "Deliverable" → pindah Layar 2

- [ ] **Step 3: Commit**

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "feat(mockup): Layar 6 Edit Session B2 read-only pre-filled"
```

---

## Task 13: Verifikasi End-to-End & Fidelity Check

**Files:** none (manual verification only)

- [ ] **Step 1: Buka file di Chrome**

```bash
start chrome "file:///$(pwd -W)/docs/mockup-presentasi/coaching-proton-mockup.html"
```

- [ ] **Step 2: Walkthrough lengkap dari Layar 1**

- Layar 1: filter dropdown alert demo, search ketik "operasi" → tabel filter, pagination ke hal 2 → swap rows, klik "Submit Evidence" → modal C1 → isi + Submit → toast hijau di kanan-bawah
- Layar 2: chain pending semua, klik Edit pencil → Layar 6
- Layar 3: tombol Tinjau modal C2 → pilih Approve → toast, buka lagi pilih Reject tanpa komentar → validation, isi komentar → toast
- Layar 4: chain 3 hijau, riwayat 6 event, Edit pencil → Layar 6
- Layar 5: checkbox batch → tombol "Approve Selected" muncul, klik → modal C3 list 3 → Approve → toast + reset, klik Review inline → toast, HC Panel terbuka 3 baris
- Layar 6: 4 read-only field, textarea readonly, dropdown disabled, tombol mati → toast

- [ ] **Step 3: Test keyboard + dot**

- Tekan → 6 kali dari Layar 1 sampai Layar 6
- Tekan ← 6 kali kembali ke Layar 1
- Klik dot ke-4 langsung → ke Layar 4

- [ ] **Step 4: Test offline**

- Matikan wifi
- Refresh
- Expected: semua tetap render, no console 404, Bootstrap Icons font tampil

- [ ] **Step 5: Cek total payload**

```bash
du -sh docs/mockup-presentasi/
```

Expected: ~1.5–2 MB

- [ ] **Step 6: Cek console (no errors)**

- Buka DevTools Console
- Expected: tidak ada error merah

- [ ] **Step 7: Fidelity check side-by-side**

- Jalankan: `dotnet build && dotnet run` (background)
- Tunggu sampai `http://localhost:5277` aktif
- Login: `admin@pertamina.com` (dev credentials dari memory)
- Buka `http://localhost:5277/CDP/CoachingProton` di tab baru
- Bandingkan side-by-side dengan Layar 5 mockup (HC view)
- Target visual match: ≥80% (layout grid, warna badge, spacing, table style)
- Acceptable drift: data dummy beda, filter dropdown mockup mati, modal feedback toast vs redirect Portal real

- [ ] **Step 8: Stop dotnet run**

Ctrl+C di terminal yang menjalankan dotnet.

- [ ] **Step 9: Commit final** (kalau ada perbaikan dari fidelity check, commit terpisah)

Kalau verification passes tanpa fix → no commit needed di task ini.

Kalau ada minor adjustment (spacing, color, etc) → commit:

```bash
git add docs/mockup-presentasi/coaching-proton-mockup.html
git commit -m "fix(mockup): adjust fidelity ringan setelah side-by-side compare"
```

---

## Success Criteria (Final Verification)

- [ ] File `docs/mockup-presentasi/coaching-proton-mockup.html` ada, ±2000 baris
- [ ] Folder `docs/mockup-presentasi/vendor/` berisi 5 file (Bootstrap CSS+JS, Icons CSS, 2 font)
- [ ] Buka file di Chrome offline → render sempurna, no 404
- [ ] 6 layar bisa diakses via footer Next/Prev + dot + keyboard ←/→
- [ ] User dropdown nama+role berubah per layar (Eko Wibowo Coach / Fajar Hidayat SrSpv / Hadi Nugroho HC)
- [ ] 3 modal interactive: C1 (Submit Evidence) di Layar 1, C2 (Tinjau) di Layar 3, C3 (Batch HC Approve) di Layar 5 — semua bisa buka, isi, submit, toast muncul
- [ ] Search box filter rows real-time di Layar 1/3/5
- [ ] Pagination 2 halaman dummy bisa di-swap di Layar 1/3
- [ ] Shortcut "Lihat Detail" Layar 1→2, 3→4, 5→4 bekerja
- [ ] Shortcut Edit pencil session Layar 2/4 → Layar 6 bekerja
- [ ] Tombol mati (Reset, Export, Download, Kembali, Logout, Profile, Settings, Simpan Edit, Batal Edit) trigger toast "Demo mode"
- [ ] Filter dropdown trigger toast "Filter aktif di Portal produksi"
- [ ] Total payload < 2 MB
- [ ] No console errors saat load offline
- [ ] Fidelity side-by-side dengan Portal real ≥80% match
