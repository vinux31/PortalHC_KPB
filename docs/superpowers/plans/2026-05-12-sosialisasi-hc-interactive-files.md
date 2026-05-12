# Sosialisasi PortalHC 4-File Interactive — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build 4 file HTML interactive (Hub + Slide + Panduan + Mockup) distribusi offline zip untuk sosialisasi PortalHC ke Tim HC operasional KPB.

**Architecture:** 4 file HTML self-contained dalam folder `sosialisasi-portalhc/`. Vendor (Alpine.js + Tailwind) bundled lokal, no CDN runtime. Cross-link inter-file via anchor. Mockup state pakai Alpine `$persist` localStorage dengan schema versioning. Screenshot annotated PNG+SVG overlay. Distribusi zip via PowerShell `Compress-Archive`.

**Tech Stack:** HTML5 + Tailwind CSS + Alpine.js + Alpine `$persist` plugin. Bundle tool PowerShell. Test browser Chrome/Edge desktop.

**Spec Reference:** `docs/superpowers/specs/2026-05-12-sosialisasi-hc-interactive-files-design.md`

---

## Phase 0 — Setup & Foundation

### Task 0.1: Create root folder + vendor bundle

**Files:**
- Create: `sosialisasi-portalhc/assets/vendor/alpinejs.min.js`
- Create: `sosialisasi-portalhc/assets/vendor/alpine-persist.min.js`
- Create: `sosialisasi-portalhc/assets/vendor/tailwind.min.css`

- [ ] **Step 1: Create folder structure**

Run:
```bash
mkdir -p sosialisasi-portalhc/assets/vendor
mkdir -p sosialisasi-portalhc/assets/screenshots/daily-ops
mkdir -p sosialisasi-portalhc/assets/screenshots/master-data
mkdir -p sosialisasi-portalhc/assets/screenshots/cmp
mkdir -p sosialisasi-portalhc/assets/screenshots/cdp
mkdir -p sosialisasi-portalhc/assets/screenshots/analytics
```

Verify: `ls sosialisasi-portalhc/assets/` shows `vendor/` + `screenshots/`

- [ ] **Step 2: Download Alpine.js core**

Run (PowerShell, requires internet):
```powershell
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/alpinejs@3.13.0/dist/cdn.min.js" -OutFile "sosialisasi-portalhc/assets/vendor/alpinejs.min.js"
```

Verify: file exists, size ~40-50KB.

- [ ] **Step 3: Download Alpine `$persist` plugin**

Run:
```powershell
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/@alpinejs/persist@3.13.0/dist/cdn.min.js" -OutFile "sosialisasi-portalhc/assets/vendor/alpine-persist.min.js"
```

Verify: file exists, size ~3-5KB.

- [ ] **Step 4: Download Tailwind CSS pre-built**

Use Play CDN minified untuk simplicity:
```powershell
Invoke-WebRequest -Uri "https://cdn.tailwindcss.com/3.4.0" -OutFile "sosialisasi-portalhc/assets/vendor/tailwind.min.js"
```

Note: Tailwind Play CDN = JS file (not CSS) yang inject styles. Filename `tailwind.min.js`. Bukan ideal production, tapi cukup untuk offline static HTML.

Verify: file ~150-200KB.

- [ ] **Step 5: Commit vendor bundle**

Run:
```bash
git add sosialisasi-portalhc/assets/vendor/
git commit -m "feat(sosialisasi): vendor bundle Alpine.js + Tailwind for offline distribution"
```

---

### Task 0.2: Shared CSS design tokens + utility

**Files:**
- Create: `sosialisasi-portalhc/assets/css/shared.css`

- [ ] **Step 1: Create shared.css**

```css
:root {
  --navy: #002e6d;
  --navy-dark: #001c44;
  --red: #ed1c24;
  --red-dark: #b0121a;
  --green: #009640;
  --amber: #f59e0b;
  --slate: #64748b;
  --bg-light: #f0f2f5;
  --bg-dark: #0f172a;
  --text-light: #1a1a1a;
  --text-dark: #f1f5f9;
}

body {
  font-family: 'Segoe UI', Tahoma, sans-serif;
  background: var(--bg-light);
  color: var(--text-light);
}

body.dark {
  background: var(--bg-dark);
  color: var(--text-dark);
}

.brand-navy { color: var(--navy); }
.brand-red { color: var(--red); }
.bg-brand-navy { background: var(--navy); }
.bg-brand-red { background: var(--red); }

.card-hover {
  transition: transform 200ms, box-shadow 200ms;
}
.card-hover:hover {
  transform: translateY(-4px);
  box-shadow: 0 10px 30px rgba(0, 46, 109, 0.15);
}

.slide-fade-enter { animation: slide-fade 300ms ease-out; }
@keyframes slide-fade {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}

.shake { animation: shake 300ms; }
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25% { transform: translateX(-6px); }
  75% { transform: translateX(6px); }
}

.checkmark-pop { animation: pop 400ms; }
@keyframes pop {
  0% { transform: scale(0); }
  60% { transform: scale(1.2); }
  100% { transform: scale(1); }
}
```

- [ ] **Step 2: Commit shared CSS**

```bash
git add sosialisasi-portalhc/assets/css/
git commit -m "feat(sosialisasi): shared CSS design tokens + utility animations"
```

---

### Task 0.3: README + CHANGELOG scaffold

**Files:**
- Create: `sosialisasi-portalhc/README.md`
- Create: `sosialisasi-portalhc/CHANGELOG.md`

- [ ] **Step 1: Write README.md**

```markdown
# Sosialisasi PortalHC KPB

Materi sosialisasi interaktif untuk Tim HC operasional Pertamina KPB.

## Cara Pakai

1. Extract semua isi zip ke folder lokal
2. Buka `index.html` di Chrome atau Edge (desktop)
3. Klik card untuk navigasi:
   - **Sosialisasi** — slide overview portal
   - **Panduan** — tutorial step-by-step
   - **Praktik** — mockup drill interactive

## Catatan

- File self-contained, no internet required setelah extract
- Browser disarankan Chrome / Edge versi terbaru
- Resolusi proyektor/laptop: 1366×768 minimum, 1920×1080 disarankan
- Praktik mockup pakai data fake (no DB real), refresh aman (state preserve)

## Versi

v1.0 — Initial release. Lihat `CHANGELOG.md` untuk detail.
```

- [ ] **Step 2: Write CHANGELOG.md**

```markdown
# Changelog

## v1.0 — 2026-05-12

### Added
- Hub landing (index.html) dengan hero + 3 card navigasi
- Slide overview (sosialisasi.html) 15 slide
- Panduan tutorial (panduan.html) 22 use case dengan Quick Start + 4 cluster
- Mockup praktik (praktik.html) 8 workflow drill interactive
- 5 quiz concept-check di Panduan
- Cross-link Panduan ↔ Praktik
- Offline zip distribution

### Tech
- Alpine.js 3.13 + `$persist` plugin
- Tailwind CSS 3.4 (Play CDN bundled)
- LocalStorage schema v1
```

- [ ] **Step 3: Commit README + CHANGELOG**

```bash
git add sosialisasi-portalhc/README.md sosialisasi-portalhc/CHANGELOG.md
git commit -m "docs(sosialisasi): README + CHANGELOG scaffold for v1.0"
```

---

## Phase 1 — Hub Landing (index.html)

### Task 1.1: Hub scaffold + hero + 3 card

**Files:**
- Create: `sosialisasi-portalhc/index.html`

- [ ] **Step 1: Write index.html**

```html
<!DOCTYPE html>
<html lang="id">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Sosialisasi PortalHC KPB — Hub</title>
  <link rel="stylesheet" href="assets/css/shared.css">
  <script src="assets/vendor/tailwind.min.js"></script>
  <script defer src="assets/vendor/alpine-persist.min.js"></script>
  <script defer src="assets/vendor/alpinejs.min.js"></script>
</head>
<body class="min-h-screen flex flex-col">
  <!-- Hero -->
  <header class="bg-brand-navy text-white py-12 px-6">
    <div class="max-w-5xl mx-auto">
      <h1 class="text-4xl font-bold mb-3">Sosialisasi <span class="text-amber-400">PortalHC</span> KPB</h1>
      <p class="text-lg opacity-90">Materi interaktif untuk Tim HC operasional — pilih bagian di bawah untuk mulai.</p>
    </div>
  </header>

  <!-- Cards -->
  <main class="flex-1 max-w-5xl mx-auto px-6 py-12 grid md:grid-cols-3 gap-6">
    <a href="sosialisasi.html" class="card-hover bg-white rounded-xl shadow-md p-6 block">
      <div class="text-4xl mb-3">📊</div>
      <h2 class="text-xl font-bold brand-navy mb-2">Sosialisasi</h2>
      <p class="text-sm text-slate-600">Overview PortalHC — apa itu, modul, role, akses. 15 slide presentasi.</p>
    </a>

    <a href="panduan.html" class="card-hover bg-white rounded-xl shadow-md p-6 block">
      <div class="text-4xl mb-3">📖</div>
      <h2 class="text-xl font-bold brand-navy mb-2">Panduan</h2>
      <p class="text-sm text-slate-600">Tutorial step-by-step 22 use case daily/weekly + 5 quiz concept-check.</p>
    </a>

    <a href="praktik.html" class="card-hover bg-white rounded-xl shadow-md p-6 block">
      <div class="text-4xl mb-3">🧪</div>
      <h2 class="text-xl font-bold brand-navy mb-2">Praktik</h2>
      <p class="text-sm text-slate-600">Mockup drill 8 workflow interactive — latihan tanpa rusak data real.</p>
    </a>
  </main>

  <!-- Footer -->
  <footer class="bg-slate-100 text-slate-600 py-6 px-6 text-center text-sm">
    Sosialisasi PortalHC KPB v1.0 · Tim HC Operasional Pertamina KPB
  </footer>
</body>
</html>
```

- [ ] **Step 2: Manual verify di browser**

Buka `sosialisasi-portalhc/index.html` di Chrome.
Expected:
- Hero navy dengan judul + tagline
- 3 card layout horizontal (atau stack di sempit)
- Hover card → lift + shadow
- Klik card → navigate (404 sementara karena file lain belum ada — OK)

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/index.html
git commit -m "feat(sosialisasi): hub landing dengan hero + 3 card navigasi"
```

---

## Phase 2 — Slide Overview (sosialisasi.html)

### Task 2.1: Slide scaffold + navigation logic

**Files:**
- Create: `sosialisasi-portalhc/sosialisasi.html`

- [ ] **Step 1: Write sosialisasi.html base structure**

```html
<!DOCTYPE html>
<html lang="id">
<head>
  <meta charset="UTF-8">
  <title>Sosialisasi PortalHC — Slide Overview</title>
  <link rel="stylesheet" href="assets/css/shared.css">
  <script src="assets/vendor/tailwind.min.js"></script>
  <script defer src="assets/vendor/alpine-persist.min.js"></script>
  <script defer src="assets/vendor/alpinejs.min.js"></script>
  <style>
    .slide { display: none; }
    .slide.active { display: block; }
  </style>
</head>
<body class="min-h-screen bg-slate-50" x-data="slideNav()" @keydown.window.right.prevent="next()" @keydown.window.left.prevent="prev()" @keydown.window.space.prevent="next()">
  <!-- Top nav -->
  <nav class="bg-white shadow-sm px-6 py-3 flex justify-between items-center sticky top-0 z-10">
    <a href="index.html" class="text-sm brand-navy hover:underline">← Hub</a>
    <span class="text-xs text-slate-500">Slide <span x-text="current"></span> / 15</span>
    <div class="flex gap-2">
      <button @click="prev()" :disabled="current === 1" class="px-3 py-1 bg-slate-200 rounded disabled:opacity-50">‹ Prev</button>
      <button @click="next()" :disabled="current === 15" class="px-3 py-1 bg-brand-navy text-white rounded disabled:opacity-50">Next ›</button>
    </div>
  </nav>

  <!-- Slides container -->
  <main class="max-w-5xl mx-auto px-6 py-8">
    <!-- Slide 1: Cover -->
    <section class="slide slide-fade-enter" :class="{ active: current === 1 }">
      <div class="bg-gradient-to-br from-brand-navy to-blue-900 text-white rounded-2xl p-12 text-center">
        <h1 class="text-5xl font-bold mb-4">HC Portal <span class="text-amber-400">KPB</span></h1>
        <p class="text-xl opacity-90">Sosialisasi Aplikasi untuk Tim HC Operasional</p>
        <p class="mt-8 text-sm opacity-75">Pertamina Kilang Balikpapan · 2026</p>
      </div>
    </section>

    <!-- Slides 2-15 di-add di task berikutnya -->
  </main>

  <script>
    function slideNav() {
      return {
        current: 1,
        total: 15,
        next() { if (this.current < this.total) this.current++ },
        prev() { if (this.current > 1) this.current-- }
      }
    }
  </script>
</body>
</html>
```

- [ ] **Step 2: Manual verify**

Buka di Chrome.
Expected:
- Slide 1 (cover) tampil
- Klik Next button → counter naik tapi belum ada slide 2 (next task akan tambah)
- Klik Prev → counter turun ke 1 minimum
- Keyboard arrow keys + space working

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/sosialisasi.html
git commit -m "feat(sosialisasi): slide scaffold + Alpine.js nav + cover slide"
```

---

### Task 2.2: Slide 2-5 (Agenda + Latar Belakang + Apa Itu + 3 Pilar)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi.html` (insert sections sebelum `</main>`)

- [ ] **Step 1: Append slide 2-5 sebelum `<!-- Slides 2-15 -->` comment**

```html
<!-- Slide 2: Agenda -->
<section class="slide slide-fade-enter" :class="{ active: current === 2 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Agenda <span class="text-amber-500">Presentasi</span></h2>
    <p class="text-slate-500 mb-6">Apa saja yang akan dibahas hari ini</p>
    <ol class="space-y-3 list-decimal list-inside text-lg">
      <li>Latar belakang & tujuan PortalHC</li>
      <li>3 Pilar Manfaat</li>
      <li>Struktur Role pengguna (6 level akses)</li>
      <li>Modul CMP — Assessment & Sertifikasi</li>
      <li>Modul CDP — Coaching & IDP</li>
      <li>Dashboard Analytics & Reporting</li>
      <li>Integrasi & Keamanan</li>
      <li>Cara mengakses + Q&A</li>
    </ol>
  </div>
</section>

<!-- Slide 3: Latar Belakang -->
<section class="slide slide-fade-enter" :class="{ active: current === 3 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-6">Latar <span class="text-amber-500">Belakang</span></h2>
    <div class="grid md:grid-cols-2 gap-6">
      <div class="bg-red-50 border-l-4 border-red-500 p-5 rounded">
        <h3 class="font-bold text-red-700 mb-2">⚠️ Sebelum PortalHC</h3>
        <ul class="text-sm space-y-1 list-disc list-inside text-slate-700">
          <li>Data assessment scatter di Excel berbagai folder</li>
          <li>Audit retensi sertifikasi susah dilacak</li>
          <li>Coaching record manual, sulit di-rollup</li>
          <li>Laporan Analytics jadi PR bulanan HC</li>
        </ul>
      </div>
      <div class="bg-green-50 border-l-4 border-green-500 p-5 rounded">
        <h3 class="font-bold text-green-700 mb-2">✓ Setelah PortalHC</h3>
        <ul class="text-sm space-y-1 list-disc list-inside text-slate-700">
          <li>1 platform terintegrasi untuk semua HC ops</li>
          <li>Audit trail otomatis tiap action</li>
          <li>Coaching tracked end-to-end</li>
          <li>Dashboard real-time + export 1-klik</li>
        </ul>
      </div>
    </div>
  </div>
</section>

<!-- Slide 4: Apa Itu HC Portal -->
<section class="slide slide-fade-enter" :class="{ active: current === 4 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-4">Apa Itu <span class="text-amber-500">HC Portal</span> KPB?</h2>
    <p class="text-lg text-slate-700 mb-6">Sistem informasi berbasis web untuk Tim Human Capital Kilang Pertamina Balikpapan dalam mengelola <strong>kompetensi dan pengembangan pekerja</strong> secara terintegrasi, transparan, dan data-driven.</p>
    <div class="grid md:grid-cols-3 gap-4 text-center">
      <div class="bg-slate-50 p-5 rounded-lg">
        <div class="text-3xl mb-2">🎯</div>
        <h3 class="font-bold brand-navy">Terintegrasi</h3>
        <p class="text-sm text-slate-600 mt-1">Semua modul HC dalam 1 portal</p>
      </div>
      <div class="bg-slate-50 p-5 rounded-lg">
        <div class="text-3xl mb-2">🔍</div>
        <h3 class="font-bold brand-navy">Transparan</h3>
        <p class="text-sm text-slate-600 mt-1">Audit trail + role-based access</p>
      </div>
      <div class="bg-slate-50 p-5 rounded-lg">
        <div class="text-3xl mb-2">📈</div>
        <h3 class="font-bold brand-navy">Data-Driven</h3>
        <p class="text-sm text-slate-600 mt-1">Analytics + report real-time</p>
      </div>
    </div>
  </div>
</section>

<!-- Slide 5: 3 Pilar Manfaat -->
<section class="slide slide-fade-enter" :class="{ active: current === 5 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">3 Pilar <span class="text-amber-500">Manfaat</span></h2>
    <p class="text-slate-500 mb-6">Nilai utama PortalHC untuk operasional KPB</p>
    <div class="space-y-4">
      <div class="flex gap-4 items-start bg-blue-50 p-5 rounded-lg">
        <div class="text-4xl">⚙️</div>
        <div>
          <h3 class="font-bold text-lg brand-navy">1. Efisiensi Operasional</h3>
          <p class="text-sm text-slate-700">Bulk import worker, auto-grading MC, certificate PDF instant. Tugas yang dulu 1 hari, sekarang 1 jam.</p>
        </div>
      </div>
      <div class="flex gap-4 items-start bg-amber-50 p-5 rounded-lg">
        <div class="text-4xl">🔒</div>
        <div>
          <h3 class="font-bold text-lg brand-navy">2. Compliance & Audit</h3>
          <p class="text-sm text-slate-700">AuditLog otomatis tiap action, soft delete preserve history, export ke Excel untuk audit eksternal.</p>
        </div>
      </div>
      <div class="flex gap-4 items-start bg-green-50 p-5 rounded-lg">
        <div class="text-4xl">📊</div>
        <div>
          <h3 class="font-bold text-lg brand-navy">3. Insight Pengembangan</h3>
          <p class="text-sm text-slate-700">Dashboard fail-rate, gain score Pre-Post, coaching progress — manajemen punya data untuk decide intervention.</p>
        </div>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify Next/Prev nav melewati slide 1→5**

Expected: slide 1-5 render benar, transisi fade smooth.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/sosialisasi.html
git commit -m "feat(sosialisasi): slide 2-5 (Agenda, Latar Belakang, Apa Itu, 3 Pilar)"
```

---

### Task 2.3: Slide 6-10 (Role + CMP + Kategori + CDP + Coaching Journey)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi.html` (append sections)

- [ ] **Step 1: Append slide 6-10**

```html
<!-- Slide 6: Struktur Role (FIX accuracy: 6 level, 10 role total) -->
<section class="slide slide-fade-enter" :class="{ active: current === 6 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Struktur <span class="text-amber-500">Role</span> Pengguna</h2>
    <p class="text-slate-500 mb-6">6 level akses (10 role total) — makin tinggi tangga, makin luas authority</p>
    <div class="space-y-2">
      <div class="flex gap-3 items-center bg-red-50 p-3 rounded"><span class="font-bold w-20 text-red-700">Level 1</span><span class="font-mono">Admin</span><span class="text-xs text-slate-500 ml-auto">Full system control</span></div>
      <div class="flex gap-3 items-center bg-orange-50 p-3 rounded"><span class="font-bold w-20 text-orange-700">Level 2</span><span class="font-mono">HC</span><span class="text-xs text-slate-500 ml-auto">HC operational lead</span></div>
      <div class="flex gap-3 items-center bg-amber-50 p-3 rounded"><span class="font-bold w-20 text-amber-700">Level 3</span><span class="font-mono">Direktur · VP · Manager</span><span class="text-xs text-slate-500 ml-auto">Management (3 role)</span></div>
      <div class="flex gap-3 items-center bg-yellow-50 p-3 rounded"><span class="font-bold w-20 text-yellow-700">Level 4</span><span class="font-mono">SectionHead · SrSupervisor</span><span class="text-xs text-slate-500 ml-auto">Section supervisory (2 role)</span></div>
      <div class="flex gap-3 items-center bg-green-50 p-3 rounded"><span class="font-bold w-20 text-green-700">Level 5</span><span class="font-mono">Coach · Supervisor</span><span class="text-xs text-slate-500 ml-auto">Coaching (2 role)</span></div>
      <div class="flex gap-3 items-center bg-blue-50 p-3 rounded"><span class="font-bold w-20 text-blue-700">Level 6</span><span class="font-mono">Coachee</span><span class="text-xs text-slate-500 ml-auto">Operational (default)</span></div>
    </div>
  </div>
</section>

<!-- Slide 7: Modul CMP -->
<section class="slide slide-fade-enter" :class="{ active: current === 7 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Modul <span class="text-amber-500">CMP</span></h2>
    <p class="text-slate-500 mb-6">Competency Management Program — assessment & sertifikasi kompetensi pekerja</p>
    <div class="grid md:grid-cols-2 gap-4">
      <div class="bg-slate-50 p-4 rounded">
        <h3 class="font-bold brand-navy mb-2">📝 Question & Assessment</h3>
        <ul class="text-sm text-slate-700 space-y-1 list-disc list-inside">
          <li>Question bank per package + bulk import Excel</li>
          <li>Create Assessment single / Pre-Post / Mixed mode</li>
          <li>Schedule + assign user + grading auto/manual</li>
          <li>Live monitoring sesi berjalan</li>
        </ul>
      </div>
      <div class="bg-slate-50 p-4 rounded">
        <h3 class="font-bold brand-navy mb-2">🏆 Result & Certification</h3>
        <ul class="text-sm text-slate-700 space-y-1 list-disc list-inside">
          <li>Results page MA mixed (Manual + Auto)</li>
          <li>Pre-Post gain score analysis</li>
          <li>Certificate PDF auto-generate</li>
          <li>Renewal tracking + ExpiringSoon alert</li>
        </ul>
      </div>
    </div>
  </div>
</section>

<!-- Slide 8: 6 Kategori Assessment -->
<section class="slide slide-fade-enter" :class="{ active: current === 8 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">6 Kategori <span class="text-amber-500">Assessment</span></h2>
    <p class="text-slate-500 mb-6">Cakupan business reference — admin dapat tambah/edit kategori</p>
    <div class="grid grid-cols-2 md:grid-cols-3 gap-3">
      <div class="bg-blue-50 p-4 rounded text-center"><div class="text-2xl mb-1">🛠️</div><strong class="text-blue-800">OJ</strong><p class="text-xs text-slate-600">On Job</p></div>
      <div class="bg-purple-50 p-4 rounded text-center"><div class="text-2xl mb-1">🏫</div><strong class="text-purple-800">IHT</strong><p class="text-xs text-slate-600">In-House Training</p></div>
      <div class="bg-amber-50 p-4 rounded text-center"><div class="text-2xl mb-1">📜</div><strong class="text-amber-800">Licencor</strong><p class="text-xs text-slate-600">Vendor license</p></div>
      <div class="bg-green-50 p-4 rounded text-center"><div class="text-2xl mb-1">🎮</div><strong class="text-green-800">OTS</strong><p class="text-xs text-slate-600">Operator Training Simulator</p></div>
      <div class="bg-red-50 p-4 rounded text-center"><div class="text-2xl mb-1">⛑️</div><strong class="text-red-800">HSSE</strong><p class="text-xs text-slate-600">Safety & environment</p></div>
      <div class="bg-indigo-50 p-4 rounded text-center"><div class="text-2xl mb-1">⚛️</div><strong class="text-indigo-800">Proton</strong><p class="text-xs text-slate-600">Coaching program</p></div>
    </div>
  </div>
</section>

<!-- Slide 9: Modul CDP -->
<section class="slide slide-fade-enter" :class="{ active: current === 9 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Modul <span class="text-amber-500">CDP</span></h2>
    <p class="text-slate-500 mb-6">Continuous Development Program — coaching, IDP, sertifikasi pekerja</p>
    <div class="space-y-3">
      <div class="bg-slate-50 p-4 rounded flex gap-3 items-start"><span class="text-3xl">👥</span><div><h3 class="font-bold brand-navy">Coaching Proton</h3><p class="text-sm text-slate-700">Manage coaching session, submit deliverable, evidence approval flow coach-coachee</p></div></div>
      <div class="bg-slate-50 p-4 rounded flex gap-3 items-start"><span class="text-3xl">🎯</span><div><h3 class="font-bold brand-navy">Plan IDP (Individual Development Plan)</h3><p class="text-sm text-slate-700">Setup target kompetensi per worker, silabus-based mapping, monitor progress</p></div></div>
      <div class="bg-slate-50 p-4 rounded flex gap-3 items-start"><span class="text-3xl">🔗</span><div><h3 class="font-bold brand-navy">Coach-Coachee Mapping</h3><p class="text-sm text-slate-700">Assign coach ke coachee, threshold workload monitoring, bulk import Excel</p></div></div>
    </div>
  </div>
</section>

<!-- Slide 10: Coaching Proton Journey -->
<section class="slide slide-fade-enter" :class="{ active: current === 10 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Coaching Proton <span class="text-amber-500">Journey</span></h2>
    <p class="text-slate-500 mb-6">Perjalanan pengembangan pekerja Tahun 1 → Tahun 3</p>
    <div class="space-y-3">
      <div class="flex gap-3 items-start"><div class="bg-blue-500 text-white w-10 h-10 rounded-full flex items-center justify-center font-bold flex-shrink-0">1</div><div class="bg-blue-50 p-3 rounded flex-1"><strong>Tahun 1 — Fundamental:</strong> Onboarding, basic kompetensi, IDP awal, coach mapping</div></div>
      <div class="flex gap-3 items-start"><div class="bg-amber-500 text-white w-10 h-10 rounded-full flex items-center justify-center font-bold flex-shrink-0">2</div><div class="bg-amber-50 p-3 rounded flex-1"><strong>Tahun 2 — Develop:</strong> Advanced kompetensi, deliverable submission, mid-cycle assessment</div></div>
      <div class="flex gap-3 items-start"><div class="bg-green-500 text-white w-10 h-10 rounded-full flex items-center justify-center font-bold flex-shrink-0">3</div><div class="bg-green-50 p-3 rounded flex-1"><strong>Tahun 3 — Certify:</strong> Final assessment, sertifikasi, transition ke role baru</div></div>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify slide 6-10**

Expected: 6 level akses tampil benar dengan 10 role names; 6 kategori tampil OJ/IHT/Licencor/OTS/HSSE/Proton.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/sosialisasi.html
git commit -m "feat(sosialisasi): slide 6-10 (Role, CMP, Kategori, CDP, Coaching Journey)"
```

---

### Task 2.4: Slide 11-15 (Dashboard + Integrasi FIX + Progress + Akses FIX + Penutup)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi.html` (append sections)

- [ ] **Step 1: Append slide 11-15 dengan FIX accuracy AD**

```html
<!-- Slide 11: Dashboard Analytics -->
<section class="slide slide-fade-enter" :class="{ active: current === 11 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Dashboard <span class="text-amber-500">Analytics</span></h2>
    <p class="text-slate-500 mb-6">Insight real-time untuk keputusan pengembangan pekerja</p>
    <div class="grid md:grid-cols-2 gap-4">
      <div class="bg-red-50 p-4 rounded"><h3 class="font-bold text-red-700">📉 Fail Rate Analysis</h3><p class="text-sm text-slate-700 mt-1">Identifikasi kategori dengan fail rate tinggi, drill-down ke section/worker, export Excel</p></div>
      <div class="bg-amber-50 p-4 rounded"><h3 class="font-bold text-amber-700">📊 Trend & ET Breakdown</h3><p class="text-sm text-slate-700 mt-1">Pass/fail trend, distribusi exam type</p></div>
      <div class="bg-green-50 p-4 rounded"><h3 class="font-bold text-green-700">📈 Gain Score Pre-Post</h3><p class="text-sm text-slate-700 mt-1">Effectiveness training (selisih nilai sebelum vs sesudah)</p></div>
      <div class="bg-orange-50 p-4 rounded"><h3 class="font-bold text-orange-700">⏰ Expiring Soon</h3><p class="text-sm text-slate-700 mt-1">Sertifikat akan expire — renewal alert untuk HR</p></div>
    </div>
  </div>
</section>

<!-- Slide 12: Integrasi & Keamanan (FIX: drop AD claim, akurat current state) -->
<section class="slide slide-fade-enter" :class="{ active: current === 12 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Integrasi & <span class="text-amber-500">Keamanan</span></h2>
    <p class="text-slate-500 mb-6">Fitur pendukung untuk integritas data dan kemudahan akses</p>
    <div class="space-y-3">
      <div class="bg-slate-50 p-4 rounded flex gap-3"><span class="text-2xl">🔐</span><div><strong class="brand-navy">Login Form (Email + Password)</strong><p class="text-sm text-slate-700">Authentication berbasis ASP.NET Identity. <em>Catatan: AD/LDAP support tersedia di codebase, akan diaktifkan IT secara terpisah.</em></p></div></div>
      <div class="bg-slate-50 p-4 rounded flex gap-3"><span class="text-2xl">🛡️</span><div><strong class="brand-navy">Role-Based Access (RBAC)</strong><p class="text-sm text-slate-700">6 level akses, menu otomatis filter sesuai role user</p></div></div>
      <div class="bg-slate-50 p-4 rounded flex gap-3"><span class="text-2xl">📝</span><div><strong class="brand-navy">Audit Log Otomatis</strong><p class="text-sm text-slate-700">Setiap action admin tercatat (siapa, kapan, apa) — export Excel kapan saja</p></div></div>
      <div class="bg-slate-50 p-4 rounded flex gap-3"><span class="text-2xl">📤</span><div><strong class="brand-navy">Import & Export Excel</strong><p class="text-sm text-slate-700">Worker, Question, Training records, Coach mapping — semua bulk import</p></div></div>
    </div>
  </div>
</section>

<!-- Slide 13: Progress Status (REFRESH konten Mei 2026 atau remove) -->
<section class="slide slide-fade-enter" :class="{ active: current === 13 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Status <span class="text-amber-500">Penyiapan</span></h2>
    <p class="text-slate-500 mb-6">Per Mei 2026</p>
    <div class="space-y-3">
      <div class="flex gap-3 items-center bg-green-50 p-3 rounded"><span class="text-2xl">✅</span><strong>Modul Inti CMP</strong><span class="ml-auto text-sm text-green-700">Production-ready</span></div>
      <div class="flex gap-3 items-center bg-green-50 p-3 rounded"><span class="text-2xl">✅</span><strong>Modul CDP Coaching</strong><span class="ml-auto text-sm text-green-700">Production-ready</span></div>
      <div class="flex gap-3 items-center bg-green-50 p-3 rounded"><span class="text-2xl">✅</span><strong>Analytics & Reporting</strong><span class="ml-auto text-sm text-green-700">Production-ready</span></div>
      <div class="flex gap-3 items-center bg-amber-50 p-3 rounded"><span class="text-2xl">🚧</span><strong>AD/LDAP Integration</strong><span class="ml-auto text-sm text-amber-700">Siap, menunggu enable IT</span></div>
      <div class="flex gap-3 items-center bg-blue-50 p-3 rounded"><span class="text-2xl">🎓</span><strong>Sosialisasi Tim HC</strong><span class="ml-auto text-sm text-blue-700">Acara hari ini</span></div>
    </div>
  </div>
</section>

<!-- Slide 14: Cara Mengakses (FIX: Login Email + Password, bukan AD) -->
<section class="slide slide-fade-enter" :class="{ active: current === 14 }">
  <div class="bg-white rounded-2xl p-10 shadow">
    <h2 class="text-3xl font-bold brand-navy mb-2">Cara <span class="text-amber-500">Mengakses</span> HC Portal</h2>
    <p class="text-slate-500 mb-6">2 environment: Development untuk testing, Production untuk user</p>
    <div class="grid md:grid-cols-2 gap-4">
      <div class="bg-blue-50 border-2 border-blue-300 p-5 rounded">
        <h3 class="font-bold text-blue-800 mb-2">🧪 Development</h3>
        <p class="text-sm mb-1"><strong>URL:</strong> <code class="bg-white px-2 py-0.5 rounded text-xs">http://10.55.3.3/KPB-PortalHC</code></p>
        <p class="text-sm mb-1"><strong>Untuk:</strong> Test fitur, training, sosialisasi</p>
        <p class="text-sm"><strong>Login:</strong> Email + Password (akun di-provision IT)</p>
      </div>
      <div class="bg-green-50 border-2 border-green-300 p-5 rounded">
        <h3 class="font-bold text-green-800 mb-2">🚀 Production</h3>
        <p class="text-sm mb-1"><strong>URL:</strong> <em>(TBD per IT)</em></p>
        <p class="text-sm mb-1"><strong>Untuk:</strong> Operational user</p>
        <p class="text-sm"><strong>Login:</strong> Email + Password (sama)</p>
      </div>
    </div>
    <p class="text-xs text-slate-500 mt-4 italic">Catatan: AD/LDAP integration sudah disiapkan di codebase, akan diaktifkan IT terpisah. Untuk sementara login pakai form email/password.</p>
  </div>
</section>

<!-- Slide 15: Penutup -->
<section class="slide slide-fade-enter" :class="{ active: current === 15 }">
  <div class="bg-gradient-to-br from-brand-navy to-blue-900 text-white rounded-2xl p-12 text-center">
    <h2 class="text-4xl font-bold mb-4">Terima <span class="text-amber-400">Kasih</span></h2>
    <p class="text-lg opacity-90 mb-8">Sosialisasi PortalHC KPB — Tim HC Operasional</p>
    <div class="grid md:grid-cols-2 gap-4 max-w-2xl mx-auto">
      <a href="panduan.html" class="bg-white text-brand-navy py-4 px-6 rounded-lg font-bold hover:bg-amber-100 transition">📖 Lanjut ke Panduan</a>
      <a href="praktik.html" class="bg-amber-400 text-brand-navy py-4 px-6 rounded-lg font-bold hover:bg-amber-300 transition">🧪 Drill di Praktik</a>
    </div>
    <p class="mt-8 text-sm opacity-75">Pertanyaan? Hubungi Tim HC Ops atau buka file Panduan.</p>
  </div>
</section>
```

- [ ] **Step 2: Manual verify slide 11-15**

Verify:
- Slide 12 TIDAK mention AD as primary login — akurat
- Slide 14 Login tertulis "Email + Password"
- Slide 15 link working ke panduan.html dan praktik.html

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/sosialisasi.html
git commit -m "feat(sosialisasi): slide 11-15 dengan AD claim accuracy fix (slide 12/14)"
```

---

## Phase 3 — Panduan Tutorial (panduan.html)

### Task 3.1: Panduan scaffold + tab switcher

**Files:**
- Create: `sosialisasi-portalhc/panduan.html`

- [ ] **Step 1: Write panduan.html scaffold**

```html
<!DOCTYPE html>
<html lang="id">
<head>
  <meta charset="UTF-8">
  <title>Panduan PortalHC — Tutorial Step-by-Step</title>
  <link rel="stylesheet" href="assets/css/shared.css">
  <script src="assets/vendor/tailwind.min.js"></script>
  <script defer src="assets/vendor/alpine-persist.min.js"></script>
  <script defer src="assets/vendor/alpinejs.min.js"></script>
</head>
<body class="min-h-screen bg-slate-50" x-data="{ activeTab: 'quickstart' }">
  <!-- Top nav -->
  <nav class="bg-white shadow-sm px-6 py-3 flex justify-between items-center sticky top-0 z-10">
    <a href="index.html" class="text-sm brand-navy hover:underline">← Hub</a>
    <h1 class="font-bold brand-navy">Panduan PortalHC</h1>
    <a href="praktik.html" class="text-sm bg-amber-400 px-3 py-1 rounded font-bold hover:bg-amber-300">Praktik →</a>
  </nav>

  <!-- Tab switcher cluster -->
  <div class="bg-white border-b px-6 py-3 sticky top-14 z-10 overflow-x-auto">
    <div class="max-w-5xl mx-auto flex gap-2 whitespace-nowrap">
      <button @click="activeTab = 'quickstart'" :class="activeTab === 'quickstart' ? 'bg-brand-navy text-white' : 'bg-slate-100 text-slate-700'" class="px-4 py-2 rounded font-bold text-sm">⚡ Quick Start</button>
      <button @click="activeTab = 'master-data'" :class="activeTab === 'master-data' ? 'bg-brand-navy text-white' : 'bg-slate-100 text-slate-700'" class="px-4 py-2 rounded font-bold text-sm">🗂️ Master Data</button>
      <button @click="activeTab = 'cmp'" :class="activeTab === 'cmp' ? 'bg-brand-navy text-white' : 'bg-slate-100 text-slate-700'" class="px-4 py-2 rounded font-bold text-sm">📝 CMP Assessment</button>
      <button @click="activeTab = 'cdp'" :class="activeTab === 'cdp' ? 'bg-brand-navy text-white' : 'bg-slate-100 text-slate-700'" class="px-4 py-2 rounded font-bold text-sm">🎯 CDP Coaching</button>
      <button @click="activeTab = 'analytics'" :class="activeTab === 'analytics' ? 'bg-brand-navy text-white' : 'bg-slate-100 text-slate-700'" class="px-4 py-2 rounded font-bold text-sm">📊 Analytics</button>
    </div>
  </div>

  <main class="max-w-5xl mx-auto px-6 py-8">
    <!-- Quick Start section -->
    <section x-show="activeTab === 'quickstart'" x-transition class="space-y-8">
      <h2 class="text-2xl font-bold brand-navy">⚡ Quick Start — Daily Ops</h2>
      <p class="text-slate-600">4 fitur dasar untuk semua role. Mulai di sini kalau baru kenal portal.</p>
      <!-- Content di Task 3.2 -->
    </section>

    <!-- Cluster sections di Task 3.3-3.6 -->
  </main>

  <footer class="bg-slate-100 text-slate-600 py-6 px-6 text-center text-sm mt-12">
    <p>Panduan terpisah per modul (akses portal real): <a href="http://10.55.3.3/KPB-PortalHC/documents/guides/" class="brand-navy hover:underline">wwwroot/documents/guides/</a> (9 file HTML)</p>
  </footer>
</body>
</html>
```

- [ ] **Step 2: Manual verify**

Expected: tab switcher render 5 tab. Klik tab → active state change (Quick Start section visible default).

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan scaffold + tab switcher 5 cluster"
```

---

### Task 3.2: Quick Start section — 4 items (Login, Navbar, Panduan icon, Notif Bell)

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (replace Quick Start section content)

- [ ] **Step 1: Replace Quick Start section dengan content 4 item**

```html
<section x-show="activeTab === 'quickstart'" x-transition class="space-y-8">
  <h2 class="text-2xl font-bold brand-navy">⚡ Quick Start — Daily Ops</h2>
  <p class="text-slate-600">4 fitur dasar untuk semua role. Mulai di sini kalau baru kenal portal.</p>

  <!-- Item 1: Login -->
  <article id="login" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">DAILY · SEMUA ROLE</span>
      <a href="praktik.html#login" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">1. Login ke Portal</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Buka URL Dev <code class="bg-slate-100 px-1.5 py-0.5 rounded text-xs">http://10.55.3.3/KPB-PortalHC</code> di Chrome/Edge</li>
      <li>Halaman login muncul — input <strong>Email kantor</strong> + <strong>Password</strong></li>
      <li>Klik tombol <strong>Sign In</strong></li>
      <li>Berhasil → masuk ke Dashboard</li>
      <li>Lupa password? Hubungi admin (Tim IT KPB)</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Bookmark URL di browser. AD/LDAP belum aktif — saat ini login pakai form email/password biasa.
    </div>
    <!-- Screenshot placeholder: <img src="assets/screenshots/daily-ops/01-login.png" alt="Login form"> -->
  </article>

  <!-- Item 2: Navbar / Dashboard -->
  <article id="navbar" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">DAILY · SEMUA ROLE</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">2. Navigasi Dashboard + Top Navbar</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Setelah login, landing di Dashboard</li>
      <li>Top navbar punya menu sesuai role kamu (role-based)</li>
      <li>Avatar pojok kanan = dropdown Profile + Settings + Logout</li>
      <li>Notification bell di sebelah avatar = alert sistem</li>
      <li>Icon <strong>?</strong> di navbar = link ke Panduan portal lengkap</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Menu "Kelola Data" hanya muncul kalau role kamu = Admin. Kalau tidak muncul, bukan bug — cek badge role samping nama.
    </div>
  </article>

  <!-- Item 3: Panduan icon ? -->
  <article id="panduan-icon" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-bold">SELF-HELP · SEMUA ROLE</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">3. Buka Panduan via Icon ?</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Klik icon ? di top navbar</li>
      <li>Buka halaman index 9 panduan HTML (Admin, Assessment, Coaching, dll)</li>
      <li>Pilih topik yang dibutuhkan</li>
      <li>Search by keyword di halaman index kalau panduan banyak</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Bookmark URL <code class="bg-white px-1.5 py-0.5 rounded text-xs">documents/guides/</code> kalau sering buka panduan.
    </div>
  </article>

  <!-- Item 4: Notification bell -->
  <article id="notif-bell" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">DAILY · SEMUA ROLE</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">4. Notification Bell</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Klik icon 🔔 di top navbar (samping avatar)</li>
      <li>Dropdown muncul dengan list notifikasi terbaru</li>
      <li>Badge angka = jumlah unread</li>
      <li>Klik notifikasi → navigate ke halaman terkait (assessment, coaching, dll)</li>
      <li>Notifikasi otomatis di-mark read setelah dibuka</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Cek bell setiap login — notifikasi assessment baru atau follow-up coaching biasanya muncul di sini.
    </div>
  </article>

  <!-- Quiz 1: Quick Start -->
  <div x-data="{ answered: false, correct: null, choice: null }" class="bg-indigo-50 border-2 border-indigo-300 rounded-lg p-6">
    <h3 class="font-bold text-indigo-900 mb-2">🎯 Quiz — Quick Start</h3>
    <p class="text-sm mb-4">Kamu lihat menu "Kelola Data" hilang dari navbar. Kemungkinan penyebab?</p>
    <div class="space-y-2">
      <button @click="answered = true; choice = 'a'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'a' ? 'border-red-500 bg-red-50' : ''">A. Sistem bug, lapor ke IT</button>
      <button @click="answered = true; choice = 'b'; correct = true" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'b' ? 'border-green-500 bg-green-50' : ''">B. Role kamu bukan Admin (cek badge samping nama)</button>
      <button @click="answered = true; choice = 'c'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'c' ? 'border-red-500 bg-red-50' : ''">C. Browser tidak compatible</button>
    </div>
    <div x-show="answered" x-transition class="mt-4 p-3 rounded" :class="correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
      <p x-show="correct"><strong>✓ Betul!</strong> Menu di navbar di-filter sesuai role. Cek role badge dulu sebelum lapor bug.</p>
      <p x-show="!correct"><strong>✗ Bukan.</strong> Jawaban benar: <strong>B</strong>. Menu "Kelola Data" hanya muncul kalau role = Admin. Cek badge role samping nama di pojok kanan.</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify Quick Start section**

Expected: 4 article render + 1 quiz interactive. Klik quiz answer → feedback muncul.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan Quick Start 4 item + quiz 1"
```

---

### Task 3.3: Master Data cluster (3 items: #8, #9, #13) + Quiz 2

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (append section)

- [ ] **Step 1: Append Master Data section setelah Quick Start section**

```html
<!-- Master Data section -->
<section x-show="activeTab === 'master-data'" x-transition class="space-y-8">
  <h2 class="text-2xl font-bold brand-navy">🗂️ Master Data</h2>
  <p class="text-slate-600">Setup data fondasi: Worker, Coach mapping. Untuk role Admin / HC.</p>

  <!-- Item 8: Worker CRUD -->
  <article id="worker-crud" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-red-100 text-red-800 px-2 py-1 rounded text-xs font-bold">ADMIN</span>
      <a href="praktik.html#worker" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">8. Worker CRUD Manual</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Login sebagai Admin → klik "Kelola Data" di navbar</li>
      <li>Pilih "Manage Workers" → list worker existing</li>
      <li>Tombol "Create Worker" → form isi NIP, Email, FullName, Role, Section</li>
      <li>Edit existing → klik baris → "Edit Worker"</li>
      <li>Untuk soft delete: klik "Deactivate" (worker jadi inactive, data preserved)</li>
      <li>Untuk hard delete: klik "Delete" (permanen, hilang dari DB — pakai dengan hati-hati)</li>
      <li>Reactivate: filter Inactive → klik worker → "Reactivate"</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Untuk audit retention, selalu pakai <strong>Deactivate</strong> dulu, bukan Delete. Hard delete cuma untuk data typo yang belum dipakai assessment.
    </div>
  </article>

  <!-- Item 9: Worker Excel Import -->
  <article id="worker-import" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-red-100 text-red-800 px-2 py-1 rounded text-xs font-bold">ADMIN</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">9. Worker Bulk Import Excel</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Manage Workers" → tombol "Import Excel"</li>
      <li>Klik "Download Template" → buka file Excel kosong dengan kolom NIP/Email/FullName/Role/Section</li>
      <li>Isi worker data sesuai template (10-100 row sekaligus OK)</li>
      <li>Upload file Excel via tombol "Choose File"</li>
      <li>Preview muncul → review data + validation result (warna merah = error, hijau = OK)</li>
      <li>Klik "Commit" kalau OK, atau "Cancel" kalau perlu fix Excel dulu</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Kalau preview merah, hover error untuk detail. NIP duplikat = paling sering. Cek di Excel sebelum upload ulang.
    </div>
  </article>

  <!-- Item 13: Coach Mapping -->
  <article id="coach-mapping" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-red-100 text-red-800 px-2 py-1 rounded text-xs font-bold">ADMIN</span>
      <a href="praktik.html#coach-mapping" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">13. Coach-Coachee Mapping</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Kelola Data" → "Coach-Coachee Mapping"</li>
      <li>Tombol "Add Mapping" → pilih Coach + Coachee dari dropdown</li>
      <li>Klik "Assign" → mapping tersimpan</li>
      <li>Cek workload coach via menu "Coach Workload" — threshold warning kalau coach load > limit</li>
      <li>Edit existing mapping: klik baris → ganti coach</li>
      <li>Bulk import via tombol "Import Mapping Excel" (alternatif manual)</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Threshold warning <strong>signal saja</strong>, bukan hard block. Coach overload tetap bisa di-assign manual, sistem cuma kasih warning.
    </div>
  </article>

  <!-- Quiz 2: Master Data -->
  <div x-data="{ answered: false, correct: null, choice: null }" class="bg-indigo-50 border-2 border-indigo-300 rounded-lg p-6">
    <h3 class="font-bold text-indigo-900 mb-2">🎯 Quiz — Master Data</h3>
    <p class="text-sm mb-4">Pak Admin mau hapus 1 worker yang sudah punya record assessment. Cara aman?</p>
    <div class="space-y-2">
      <button @click="answered = true; choice = 'a'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'a' ? 'border-red-500 bg-red-50' : ''">A. Klik tombol Delete — sistem otomatis preserve history</button>
      <button @click="answered = true; choice = 'b'; correct = true" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'b' ? 'border-green-500 bg-green-50' : ''">B. Klik Deactivate (soft delete) — data preserve untuk audit</button>
      <button @click="answered = true; choice = 'c'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'c' ? 'border-red-500 bg-red-50' : ''">C. Hapus dari DB pakai SQL Server Management Studio</button>
    </div>
    <div x-show="answered" x-transition class="mt-4 p-3 rounded" :class="correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
      <p x-show="correct"><strong>✓ Betul!</strong> Deactivate = soft delete. Record assessment preserved untuk audit retention.</p>
      <p x-show="!correct"><strong>✗ Bukan.</strong> Jawaban benar: <strong>B Deactivate</strong>. Hard delete (A) hapus permanen — record assessment hilang. SSMS (C) bypass audit log, jangan dilakukan.</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify Master Data tab**

Expected: tab "Master Data" → 3 article + quiz render.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan Master Data cluster 3 item + quiz 2"
```

---

### Task 3.4: CMP Assessment cluster (9 items: 19, 20, 21, 22, 23, 25, 26, 29, 31) + Quiz 3

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (append section)

- [ ] **Step 1: Append CMP Assessment section**

```html
<!-- CMP Assessment section -->
<section x-show="activeTab === 'cmp'" x-transition class="space-y-8">
  <h2 class="text-2xl font-bold brand-navy">📝 CMP Assessment</h2>
  <p class="text-slate-600">Workflow inti CMP — buat soal, sesi assessment, monitoring, grading, results.</p>

  <!-- Item 19: ManagePackageQuestions CRUD -->
  <article id="question-crud" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
      <a href="praktik.html#question" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">19. Manage Package Questions (Bank Soal)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Kelola Data" → "Manage Package Questions" → pilih package</li>
      <li>"Create Question" → isi: text soal, type (MC/Essay/Interview), choices, kunci jawaban</li>
      <li>Edit existing → klik soal → "Edit Question"</li>
      <li>Delete kalau ada typo (sebelum dipakai assessment)</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Untuk tipe MC, <strong>kunci jawaban wajib di-set</strong> sebelum publish. Tanpa kunci, auto-grade gagal.
    </div>
  </article>

  <!-- Item 20: Import Questions Excel -->
  <article id="question-import" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">20. Import Questions Excel (Bulk)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Di Manage Package Questions → "Import Excel"</li>
      <li>Download template → isi soal MC bulk</li>
      <li>Upload → preview validasi → commit</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Per package, jangan campur tipe soal (MC + Essay) di-import bareng. Bikin batch terpisah biar template tidak conflict.
    </div>
  </article>

  <!-- Item 21: Manage Assessment list + tabs -->
  <article id="manage-assessment" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">21. Manage Assessment (List + Tabs)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Kelola Data" → "Manage Assessment"</li>
      <li>3 tab tersedia: <strong>Assessment</strong> (aktif), <strong>Training</strong> (record manual), <strong>History</strong> (archived)</li>
      <li>Filter by date range, status, package</li>
      <li>Klik baris → detail assessment + actions (Edit, Monitor, Export Results)</li>
    </ol>
  </article>

  <!-- Item 22: Create Assessment single -->
  <article id="create-assessment" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
      <a href="praktik.html#create-assessment" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">22. Create Assessment — Single Mode</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Manage Assessment" → tombol "Create Assessment"</li>
      <li>Form: nama sesi, package, kategori (OJ/IHT/dll), schedule date, durasi</li>
      <li>Pilih mode: <strong>Single</strong> (1 sesi, MC auto-grade)</li>
      <li>Assign users — pilih dari dropdown atau bulk by section</li>
      <li>Optional: set passing score, manual grading toggle untuk Essay</li>
      <li>Save → assessment masuk list dengan status "Scheduled"</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> <strong>Mode "Mixed"</strong> = MC auto + Essay manual grading. Pilih ini kalau soal bercampur.
    </div>
  </article>

  <!-- Item 23: Create Assessment Pre-Post -->
  <article id="create-prepost" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
      <a href="praktik.html#prepost" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">23. Create Assessment — Pre-Post Mode</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Create Assessment" → pilih mode <strong>Pre-Post</strong></li>
      <li>Setup Pre group (assessment sebelum training)</li>
      <li>Setup Post group (assessment sesudah training) — soal sama atau ekivalen</li>
      <li>Pre-Post pair link otomatis via <code class="bg-slate-100 px-1 rounded">PrePostGroupId</code></li>
      <li>Save → 2 sesi terbuat, linked untuk gain score analysis</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Pre dan Post harus pakai <strong>soal level kompetensi sama</strong>. Beda soal → gain score tidak valid.
    </div>
  </article>

  <!-- Item 25: Assessment Monitoring Live -->
  <article id="monitoring" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
      <span class="bg-red-100 text-red-800 px-2 py-1 rounded text-xs font-bold">LIVE DEMO</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">25. Assessment Monitoring (Live)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Manage Assessment" → klik sesi yang sedang berjalan → "Monitor"</li>
      <li>Real-time view: list taker, status (Not Started / In Progress / Submitted)</li>
      <li>Refresh otomatis tiap 30 detik (atau manual via tombol "Refresh")</li>
      <li>Untuk Essay/Interview: klik "Grade" → manual grading per submission</li>
    </ol>
    <div class="mt-4 bg-blue-50 border-l-4 border-blue-400 p-3 rounded text-sm">
      <strong>📺 Catatan:</strong> Live demo dinamis — paling baik dilihat di portal real saat acara. Mockup tidak menangkap esensi real-time.
    </div>
  </article>

  <!-- Item 26: Manual Grading -->
  <article id="manual-grading" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-purple-100 text-purple-800 px-2 py-1 rounded text-xs font-bold">ASESOR</span>
      <a href="praktik.html#manual-grading" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">26. Manual Grading (Essay / Interview)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Monitor" sesi → tab "Pending Grading" → list submission Essay/Interview</li>
      <li>Klik 1 submission → view jawaban + rubric</li>
      <li>Input skor (0-100) + feedback text</li>
      <li>Save → status berubah ke "Graded", masuk hasil agregat</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Konsistensi penilaian penting. Sebelum grading batch, baca rubric + sample 2-3 submission dulu untuk kalibrasi.
    </div>
  </article>

  <!-- Item 29: Results Page -->
  <article id="results" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">SEMUA ROLE (terkait)</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">29. Results Page (MA Mixed Mode)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Manage Assessment" → klik sesi → "View Results"</li>
      <li>List taker dengan skor, grade, pass/fail status</li>
      <li>Untuk Pre-Post: tampil gain score (selisih Post − Pre)</li>
      <li>Untuk Mixed mode (MC auto + Essay manual): skor total agregat</li>
      <li>Export tombol → Excel detail per taker</li>
    </ol>
  </article>

  <!-- Item 31: Certificate PDF -->
  <article id="certificate" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">SEMUA ROLE</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">31. Certificate PDF Export</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"View Results" → klik taker yang Pass → tombol "Generate Certificate"</li>
      <li>PDF certificate ter-generate (template Pertamina branding)</li>
      <li>Download / Print langsung dari browser</li>
      <li>Atau bulk: tombol "Export All Certificates" → ZIP file</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Certificate auto-include valid until date (renewal). Cek expiry dashboard untuk renewal alert.
    </div>
  </article>

  <!-- Quiz 3: CMP -->
  <div x-data="{ answered: false, correct: null, choice: null }" class="bg-indigo-50 border-2 border-indigo-300 rounded-lg p-6">
    <h3 class="font-bold text-indigo-900 mb-2">🎯 Quiz — CMP Assessment</h3>
    <p class="text-sm mb-4">Asesor buat sesi assessment campuran MC + Essay. Mode apa yang tepat?</p>
    <div class="space-y-2">
      <button @click="answered = true; choice = 'a'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'a' ? 'border-red-500 bg-red-50' : ''">A. Single — auto-grade semua</button>
      <button @click="answered = true; choice = 'b'; correct = true" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'b' ? 'border-green-500 bg-green-50' : ''">B. Mixed — MC auto + Essay manual grading</button>
      <button @click="answered = true; choice = 'c'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'c' ? 'border-red-500 bg-red-50' : ''">C. Pre-Post — linked group</button>
    </div>
    <div x-show="answered" x-transition class="mt-4 p-3 rounded" :class="correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
      <p x-show="correct"><strong>✓ Betul!</strong> Mode Mixed = MC auto-grade + Essay perlu manual grading via "Pending Grading" tab.</p>
      <p x-show="!correct"><strong>✗ Bukan.</strong> Jawaban benar: <strong>B Mixed</strong>. Single (A) tidak support Essay. Pre-Post (C) untuk training effectiveness, bukan grading hybrid.</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify CMP tab**

Expected: tab "CMP Assessment" → 9 article + quiz.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan CMP Assessment cluster 9 item + quiz 3"
```

---

### Task 3.5: CDP Coaching cluster (2 items: 32, 33) + Quiz 4

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (append section)

- [ ] **Step 1: Append CDP Coaching section**

```html
<!-- CDP Coaching section -->
<section x-show="activeTab === 'cdp'" x-transition class="space-y-8">
  <h2 class="text-2xl font-bold brand-navy">🎯 CDP Coaching</h2>
  <p class="text-slate-600">Modul coaching: Proton session + IDP planning. Untuk role Coach.</p>

  <!-- Item 32: Coaching Proton -->
  <article id="coaching-proton" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-bold">COACH</span>
      <a href="praktik.html#coaching-proton" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">32. Coaching Proton Session</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Login sebagai Coach → menu "CDP" → "Coaching Proton"</li>
      <li>List coachee yang di-assign (per CoachCoacheeMapping)</li>
      <li>Pilih 1 coachee → "Create Session" → input topik, deliverable, due date</li>
      <li>Coachee submit evidence (file/text) di portal</li>
      <li>Coach review → approve / request revision / reject</li>
      <li>Status berubah ke "Completed" setelah evidence di-approve</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> Approval bertingkat: Coach approve → Supervisor verify → Manager final sign-off (untuk milestone besar).
    </div>
  </article>

  <!-- Item 33: Plan IDP -->
  <article id="plan-idp" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-bold">COACH</span>
      <a href="praktik.html#plan-idp" class="ml-auto text-sm text-amber-600 hover:underline">🧪 Drill di Praktik →</a>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">33. Plan IDP (Individual Development Plan)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"CDP" → "Plan IDP" → pilih coachee</li>
      <li>View silabus kompetensi (per role/section)</li>
      <li>Map target kompetensi yang dikembangkan tahun ini</li>
      <li>Set timeline + milestone per kompetensi</li>
      <li>Save → IDP tersimpan, jadi acuan Coaching Proton sessions</li>
    </ol>
    <div class="mt-4 bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm">
      <strong>💡 Tip:</strong> IDP biasanya di-review awal tahun + mid-year. Update IDP kalau ada rotasi role coachee.
    </div>
  </article>

  <!-- Quiz 4: CDP -->
  <div x-data="{ answered: false, correct: null, choice: null }" class="bg-indigo-50 border-2 border-indigo-300 rounded-lg p-6">
    <h3 class="font-bold text-indigo-900 mb-2">🎯 Quiz — CDP Coaching</h3>
    <p class="text-sm mb-4">Coach overload assign coachee ke-11 (threshold 8). Sistem react bagaimana?</p>
    <div class="space-y-2">
      <button @click="answered = true; choice = 'a'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'a' ? 'border-red-500 bg-red-50' : ''">A. Block assign, error message</button>
      <button @click="answered = true; choice = 'b'; correct = true" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'b' ? 'border-green-500 bg-green-50' : ''">B. Warn dengan visual indicator, assign tetap berhasil</button>
      <button @click="answered = true; choice = 'c'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'c' ? 'border-red-500 bg-red-50' : ''">C. Auto-reassign ke coach lain dengan load lebih ringan</button>
    </div>
    <div x-show="answered" x-transition class="mt-4 p-3 rounded" :class="correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
      <p x-show="correct"><strong>✓ Betul!</strong> Threshold = signal warn, bukan hard block. Admin tetap bisa assign manual + sistem tampilkan warning.</p>
      <p x-show="!correct"><strong>✗ Bukan.</strong> Jawaban benar: <strong>B Warn saja</strong>. Threshold = soft constraint. Block (A) dan auto-reassign (C) tidak diterapkan agar admin punya control.</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify CDP tab**

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan CDP Coaching cluster 2 item + quiz 4"
```

---

### Task 3.6: Analytics cluster (4 items: 36, 40, 43, 44) + Quiz 5

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (append section)

- [ ] **Step 1: Append Analytics section**

```html
<!-- Analytics section -->
<section x-show="activeTab === 'analytics'" x-transition class="space-y-8">
  <h2 class="text-2xl font-bold brand-navy">📊 Analytics & Reporting</h2>
  <p class="text-slate-600">Dashboard insight + export Excel laporan untuk HR, manager, compliance.</p>

  <!-- Item 36: Analytics Dashboard -->
  <article id="dashboard" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">MANAGER + ADMIN</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">36. Analytics Dashboard</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>Menu "Analytics" → "Dashboard"</li>
      <li>Chart utama: fail rate, trend, ET breakdown, expiring soon</li>
      <li>Klik chart bagian → drill-down detail (per category, per worker, per section)</li>
      <li>Filter date range + category di top dashboard</li>
      <li>Tombol "Export Excel" per chart untuk laporan</li>
    </ol>
  </article>

  <!-- Item 40: Expiring Soon -->
  <article id="expiring-soon" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-red-100 text-red-800 px-2 py-1 rounded text-xs font-bold">COMPLIANCE</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">40. Expiring Soon (Certificate Renewal)</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Analytics" → "Expiring Soon"</li>
      <li>List sertifikat yang expire dalam 30/60/90 hari</li>
      <li>Filter by category, section, worker</li>
      <li>Tombol "Schedule Renewal Assessment" → langsung create assessment baru</li>
      <li>Export list untuk follow-up HR</li>
    </ol>
    <div class="mt-4 bg-red-50 border-l-4 border-red-400 p-3 rounded text-sm">
      <strong>⚠️ Compliance critical:</strong> Cek minimal mingguan. Sertifikat expire tanpa renewal = pekerja tidak boleh kerja di area regulated.
    </div>
  </article>

  <!-- Item 43: Certification Management -->
  <article id="certification" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">HR · REPORTING</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">43. Certification Management</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Analytics" → "Certification Management"</li>
      <li>List semua sertifikat valid per worker</li>
      <li>Filter by status (Valid / Expired / Revoked), category, section</li>
      <li>Tombol "Export Sertifikat" → Excel detail full</li>
      <li>Tombol "Export Detail" → Excel dengan certificate metadata + tanggal</li>
    </ol>
  </article>

  <!-- Item 44: Export Assessment Results -->
  <article id="export-results" class="bg-white rounded-lg shadow p-6">
    <div class="flex items-start gap-3 mb-4">
      <span class="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs font-bold">ASESOR · REPORTING</span>
    </div>
    <h3 class="text-xl font-bold brand-navy mb-3">44. Export Assessment Results</h3>
    <ol class="space-y-2 text-sm list-decimal list-inside">
      <li>"Manage Assessment" → klik sesi → tombol "Export Results"</li>
      <li>Excel file ter-download dengan kolom: NIP, FullName, Section, Skor, Grade, Status, Tanggal</li>
      <li>Atau bulk: "Manage Assessment" → tombol "Export All" → semua sesi dalam date range</li>
    </ol>
  </article>

  <!-- Quiz 5: Analytics -->
  <div x-data="{ answered: false, correct: null, choice: null }" class="bg-indigo-50 border-2 border-indigo-300 rounded-lg p-6">
    <h3 class="font-bold text-indigo-900 mb-2">🎯 Quiz — Analytics</h3>
    <p class="text-sm mb-4">Compliance officer mau cek pekerja dengan sertifikat expire dalam 60 hari. Buka menu apa?</p>
    <div class="space-y-2">
      <button @click="answered = true; choice = 'a'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'a' ? 'border-red-500 bg-red-50' : ''">A. Manage Assessment → filter expired</button>
      <button @click="answered = true; choice = 'b'; correct = false" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'b' ? 'border-red-500 bg-red-50' : ''">B. Worker CRUD → cek expiry per worker manual</button>
      <button @click="answered = true; choice = 'c'; correct = true" :disabled="answered" class="block w-full text-left p-3 rounded border bg-white hover:bg-slate-50" :class="answered && choice === 'c' ? 'border-green-500 bg-green-50' : ''">C. Analytics → Expiring Soon, filter 60 hari</button>
    </div>
    <div x-show="answered" x-transition class="mt-4 p-3 rounded" :class="correct ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
      <p x-show="correct"><strong>✓ Betul!</strong> Expiring Soon dirancang khusus untuk compliance renewal tracking dengan filter 30/60/90 hari.</p>
      <p x-show="!correct"><strong>✗ Bukan.</strong> Jawaban benar: <strong>C Expiring Soon</strong>. (A) Manage Assessment buat sesi, bukan tracking expiry. (B) Worker CRUD untuk data worker, tidak efisien manual.</p>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Manual verify Analytics tab**

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): panduan Analytics cluster 4 item + quiz 5"
```

---

## Phase 4 — Mockup Praktik (praktik.html)

### Task 4.1: Praktik scaffold + workflow nav + Alpine $persist setup

**Files:**
- Create: `sosialisasi-portalhc/praktik.html`

- [ ] **Step 1: Write praktik.html scaffold**

```html
<!DOCTYPE html>
<html lang="id">
<head>
  <meta charset="UTF-8">
  <title>Praktik PortalHC — Mockup Drill</title>
  <link rel="stylesheet" href="assets/css/shared.css">
  <script src="assets/vendor/tailwind.min.js"></script>
  <script defer src="assets/vendor/alpine-persist.min.js"></script>
  <script defer src="assets/vendor/alpinejs.min.js"></script>
</head>
<body class="min-h-screen bg-slate-50" x-data="{ activeMockup: 'login' }">
  <!-- Top nav -->
  <nav class="bg-white shadow-sm px-6 py-3 flex justify-between items-center sticky top-0 z-10">
    <a href="index.html" class="text-sm brand-navy hover:underline">← Hub</a>
    <h1 class="font-bold brand-navy">Praktik — Mockup Drill</h1>
    <a href="panduan.html" class="text-sm bg-amber-400 px-3 py-1 rounded font-bold hover:bg-amber-300">Panduan →</a>
  </nav>

  <!-- Workflow nav sidebar + content -->
  <div class="max-w-7xl mx-auto px-6 py-8 grid grid-cols-1 md:grid-cols-[240px_1fr] gap-6">
    <!-- Sidebar -->
    <aside class="space-y-1">
      <h2 class="font-bold brand-navy mb-3">Pilih Workflow</h2>
      <button @click="activeMockup = 'login'" :class="activeMockup === 'login' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">🔐 #1 Login</button>
      <button @click="activeMockup = 'worker'" :class="activeMockup === 'worker' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">👤 #8 Worker CRUD</button>
      <button @click="activeMockup = 'question'" :class="activeMockup === 'question' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">📝 #19 Question CRUD</button>
      <button @click="activeMockup = 'create-assessment'" :class="activeMockup === 'create-assessment' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">📋 #22 Create Assessment</button>
      <button @click="activeMockup = 'prepost'" :class="activeMockup === 'prepost' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">🔁 #23 Pre-Post</button>
      <button @click="activeMockup = 'manual-grading'" :class="activeMockup === 'manual-grading' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">✍️ #26 Manual Grading</button>
      <button @click="activeMockup = 'coaching-proton'" :class="activeMockup === 'coaching-proton' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">🎯 #32 Coaching Proton</button>
      <button @click="activeMockup = 'plan-idp'" :class="activeMockup === 'plan-idp' ? 'bg-brand-navy text-white' : 'bg-white text-slate-700'" class="block w-full text-left px-3 py-2 rounded text-sm">📋 #33 Plan IDP</button>
    </aside>

    <!-- Main content -->
    <main>
      <p class="bg-amber-50 border-l-4 border-amber-400 p-3 rounded text-sm mb-6">
        <strong>ℹ️ Info:</strong> Mockup simulasi — data fake, no DB connect. Refresh aman (state preserve via localStorage). Klik "Drill Ulang" untuk reset.
      </p>
      <!-- Mockup sections di Task 4.2 - 4.9 -->
    </main>
  </div>
</body>
</html>
```

- [ ] **Step 2: Manual verify**

Expected: sidebar nav render dengan 8 button. Klik button → activeMockup state change.

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): praktik scaffold + workflow nav 8 mockup"
```

---

### Task 4.2: Mockup #1 Login form

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup section)

- [ ] **Step 1: Append login mockup**

```html
<!-- Mockup #1: Login -->
<section x-show="activeMockup === 'login'" x-data="loginMock()" id="login" class="bg-white rounded-lg shadow p-8 max-w-md mx-auto">
  <h2 class="text-2xl font-bold brand-navy mb-2">🔐 #1 Login</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: simulasi login form. Coba email/password fake, lihat behavior.</p>

  <form @submit.prevent="submit()" x-show="!submitted">
    <label class="block mb-3">
      <span class="text-sm font-bold">Email</span>
      <input type="email" x-model="email" class="mt-1 w-full px-3 py-2 border rounded" placeholder="admin@pertamina.com">
    </label>
    <label class="block mb-3">
      <span class="text-sm font-bold">Password</span>
      <input type="password" x-model="password" class="mt-1 w-full px-3 py-2 border rounded" placeholder="••••••">
    </label>
    <button type="submit" class="w-full bg-brand-navy text-white py-2 rounded font-bold hover:bg-navy-dark">Sign In</button>
    <p x-show="error" x-text="error" class="mt-3 text-sm text-red-600 shake"></p>
  </form>

  <div x-show="submitted" x-transition>
    <div class="text-center py-6">
      <div class="text-6xl mb-3 checkmark-pop">✅</div>
      <h3 class="text-xl font-bold text-green-700 mb-2">Login Berhasil (Simulasi)</h3>
      <p class="text-sm text-slate-600">Welcome, <strong x-text="email"></strong>!</p>
      <p class="text-xs text-slate-500 mt-2">Data fake — tidak terhubung ke portal real.</p>
    </div>
    <button @click="reset()" class="w-full mt-4 bg-amber-400 py-2 rounded font-bold">🔄 Drill Ulang</button>
  </div>
</section>

<script>
  function loginMock() {
    return {
      email: $persist('').as('mockup_v1_login_email'),
      password: $persist('').as('mockup_v1_login_password'),
      submitted: $persist(false).as('mockup_v1_login_submitted'),
      error: '',
      submit() {
        if (!this.email || !this.email.includes('@')) {
          this.error = 'Email tidak valid';
          return;
        }
        if (!this.password || this.password.length < 6) {
          this.error = 'Password min 6 karakter';
          return;
        }
        this.error = '';
        this.submitted = true;
      },
      reset() {
        this.email = '';
        this.password = '';
        this.submitted = false;
        this.error = '';
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify login mockup**

Test:
- Email kosong → error "Email tidak valid"
- Password < 6 → error "Password min 6 karakter"
- Valid input → success state dengan checkmark
- Refresh → state preserve (email/password remembered)
- "Drill Ulang" → reset state

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #1 Login form dengan validation + persist + reset"
```

---

### Task 4.3: Mockup #8 Worker CRUD

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append worker CRUD mockup**

```html
<!-- Mockup #8: Worker CRUD -->
<section x-show="activeMockup === 'worker'" x-data="workerMock()" id="worker" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">👤 #8 Worker CRUD</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: tambah/edit/deactivate worker. Test perbedaan Delete vs Deactivate.</p>

  <!-- List -->
  <div class="mb-6 overflow-x-auto">
    <table class="w-full text-sm">
      <thead class="bg-slate-100">
        <tr><th class="p-2 text-left">NIP</th><th class="p-2 text-left">Nama</th><th class="p-2 text-left">Section</th><th class="p-2 text-left">Status</th><th class="p-2">Aksi</th></tr>
      </thead>
      <tbody>
        <template x-for="w in workers" :key="w.id">
          <tr :class="w.active ? '' : 'opacity-50 bg-slate-50'" class="border-b">
            <td class="p-2" x-text="w.nip"></td>
            <td class="p-2" x-text="w.name"></td>
            <td class="p-2" x-text="w.section"></td>
            <td class="p-2"><span :class="w.active ? 'bg-green-100 text-green-700' : 'bg-slate-200 text-slate-600'" class="px-2 py-0.5 rounded text-xs" x-text="w.active ? 'Active' : 'Inactive'"></span></td>
            <td class="p-2 text-right space-x-1">
              <button @click="toggle(w)" class="text-xs px-2 py-1 rounded" :class="w.active ? 'bg-amber-400' : 'bg-green-400'" x-text="w.active ? 'Deactivate' : 'Reactivate'"></button>
              <button @click="hardDelete(w)" class="text-xs px-2 py-1 bg-red-400 rounded">Delete</button>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
  </div>

  <!-- Add new form -->
  <div class="bg-slate-50 p-4 rounded">
    <h3 class="font-bold mb-3">Tambah Worker Baru</h3>
    <div class="grid md:grid-cols-3 gap-3 mb-3">
      <input x-model="newNip" placeholder="NIP" class="px-3 py-2 border rounded text-sm">
      <input x-model="newName" placeholder="Nama Lengkap" class="px-3 py-2 border rounded text-sm">
      <input x-model="newSection" placeholder="Section (RFCC/DHT/dll)" class="px-3 py-2 border rounded text-sm">
    </div>
    <button @click="add()" class="bg-brand-navy text-white px-4 py-2 rounded font-bold">+ Tambah</button>
    <p x-show="message" x-text="message" :class="messageType === 'success' ? 'text-green-600' : 'text-red-600'" class="text-sm mt-2"></p>
  </div>

  <button @click="reset()" class="mt-4 bg-amber-400 px-4 py-2 rounded font-bold text-sm">🔄 Drill Ulang</button>
</section>

<script>
  function workerMock() {
    return {
      workers: $persist([
        { id: 1, nip: 'W001', name: 'Asesor 1', section: 'RFCC', active: true },
        { id: 2, nip: 'W002', name: 'Coach 1', section: 'DHT', active: true },
        { id: 3, nip: 'W003', name: 'Worker 003', section: 'NGP', active: false }
      ]).as('mockup_v1_worker_list'),
      newNip: '',
      newName: '',
      newSection: '',
      message: '',
      messageType: 'success',
      add() {
        if (!this.newNip || !this.newName || !this.newSection) {
          this.message = 'Semua field wajib diisi';
          this.messageType = 'error';
          return;
        }
        if (this.workers.find(w => w.nip === this.newNip)) {
          this.message = 'NIP duplikat — sudah ada di list';
          this.messageType = 'error';
          return;
        }
        this.workers.push({ id: Date.now(), nip: this.newNip, name: this.newName, section: this.newSection, active: true });
        this.message = `✓ Worker ${this.newName} ditambah`;
        this.messageType = 'success';
        this.newNip = ''; this.newName = ''; this.newSection = '';
      },
      toggle(w) {
        w.active = !w.active;
        this.message = w.active ? `${w.name} di-reactivate` : `${w.name} di-deactivate (soft delete, data preserved)`;
        this.messageType = 'success';
      },
      hardDelete(w) {
        if (!confirm(`Hard delete ${w.name}? Data hilang permanen. Untuk audit retention, pakai Deactivate.`)) return;
        this.workers = this.workers.filter(x => x.id !== w.id);
        this.message = `${w.name} di-hard delete (data hilang)`;
        this.messageType = 'success';
      },
      reset() {
        localStorage.removeItem('mockup_v1_worker_list');
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify worker CRUD**

Test:
- 3 worker pre-seeded
- Tambah worker baru → list update + success message
- Tambah dengan NIP duplikat → error message
- Deactivate worker → status berubah Inactive (row opacity berkurang)
- Reactivate → status Active
- Hard Delete → confirm dialog, lalu row hilang
- Refresh → state preserve

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #8 Worker CRUD dengan soft/hard delete drill"
```

---

### Task 4.4: Mockup #19 Question CRUD (multi-field + type selector)

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append question CRUD mockup**

```html
<!-- Mockup #19: Question CRUD -->
<section x-show="activeMockup === 'question'" x-data="questionMock()" id="question" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">📝 #19 Manage Package Questions</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: buat soal MC dengan choices + kunci jawaban. Coba juga tipe Essay/Interview.</p>

  <!-- Question list -->
  <div class="mb-6">
    <h3 class="font-bold mb-2">Bank Soal (Package "Test Sosialisasi")</h3>
    <template x-if="questions.length === 0">
      <p class="text-sm text-slate-500 italic">Belum ada soal. Tambah via form bawah.</p>
    </template>
    <template x-for="q in questions" :key="q.id">
      <div class="border rounded p-3 mb-2 bg-slate-50">
        <div class="flex justify-between mb-1">
          <span class="text-xs bg-purple-100 text-purple-700 px-2 py-0.5 rounded" x-text="q.type"></span>
          <button @click="remove(q)" class="text-xs text-red-600 hover:underline">Hapus</button>
        </div>
        <p class="text-sm font-medium" x-text="q.text"></p>
        <template x-if="q.type === 'MC'">
          <ol class="text-xs mt-2 space-y-1 list-decimal list-inside">
            <template x-for="(c, i) in q.choices" :key="i">
              <li :class="i === q.correctIndex ? 'font-bold text-green-700' : ''" x-text="c + (i === q.correctIndex ? ' ✓' : '')"></li>
            </template>
          </ol>
        </template>
      </div>
    </template>
  </div>

  <!-- Add form -->
  <div class="bg-slate-50 p-4 rounded">
    <h3 class="font-bold mb-3">Tambah Soal</h3>
    <label class="block mb-3">
      <span class="text-sm font-bold">Tipe Soal</span>
      <select x-model="newType" class="mt-1 w-full px-3 py-2 border rounded">
        <option>MC</option>
        <option>Essay</option>
        <option>Interview</option>
      </select>
    </label>
    <label class="block mb-3">
      <span class="text-sm font-bold">Pertanyaan</span>
      <textarea x-model="newText" rows="2" class="mt-1 w-full px-3 py-2 border rounded text-sm" placeholder="Contoh: Apa fungsi RFCC?"></textarea>
    </label>

    <!-- MC choices -->
    <div x-show="newType === 'MC'">
      <span class="text-sm font-bold">Pilihan Jawaban (klik radio untuk kunci jawaban)</span>
      <template x-for="(c, i) in newChoices" :key="i">
        <div class="flex gap-2 mt-1 items-center">
          <input type="radio" name="correct" :value="i" x-model="newCorrectIndex" :checked="newCorrectIndex == i">
          <input x-model="newChoices[i]" :placeholder="'Pilihan ' + (i+1)" class="flex-1 px-3 py-1 border rounded text-sm">
        </div>
      </template>
    </div>

    <button @click="add()" class="mt-4 bg-brand-navy text-white px-4 py-2 rounded font-bold">+ Tambah Soal</button>
    <p x-show="message" x-text="message" :class="messageType === 'success' ? 'text-green-600' : 'text-red-600'" class="text-sm mt-2"></p>
  </div>

  <button @click="reset()" class="mt-4 bg-amber-400 px-4 py-2 rounded font-bold text-sm">🔄 Drill Ulang</button>
</section>

<script>
  function questionMock() {
    return {
      questions: $persist([]).as('mockup_v1_question_list'),
      newType: 'MC',
      newText: '',
      newChoices: ['', '', '', ''],
      newCorrectIndex: null,
      message: '',
      messageType: 'success',
      add() {
        if (!this.newText) {
          this.message = 'Pertanyaan wajib diisi';
          this.messageType = 'error';
          return;
        }
        if (this.newType === 'MC') {
          if (this.newChoices.filter(c => c).length < 2) {
            this.message = 'Minimal 2 pilihan jawaban';
            this.messageType = 'error';
            return;
          }
          if (this.newCorrectIndex === null || this.newCorrectIndex === '') {
            this.message = 'WAJIB pilih kunci jawaban (radio button) — tanpa kunci, auto-grade gagal';
            this.messageType = 'error';
            return;
          }
        }
        this.questions.push({
          id: Date.now(),
          type: this.newType,
          text: this.newText,
          choices: this.newType === 'MC' ? [...this.newChoices] : [],
          correctIndex: this.newType === 'MC' ? parseInt(this.newCorrectIndex) : null
        });
        this.message = `✓ Soal ${this.newType} ditambah`;
        this.messageType = 'success';
        this.newText = ''; this.newChoices = ['', '', '', '']; this.newCorrectIndex = null;
      },
      remove(q) {
        this.questions = this.questions.filter(x => x.id !== q.id);
      },
      reset() {
        localStorage.removeItem('mockup_v1_question_list');
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify question CRUD**

Test:
- Tipe MC default → 4 input pilihan + radio kunci jawaban
- Tambah soal MC tanpa kunci jawaban → error "WAJIB pilih kunci jawaban"
- Tambah soal MC valid → muncul di list dengan kunci highlighted
- Switch tipe Essay → tidak ada pilihan jawaban form
- Hapus soal → row hilang
- Refresh → state preserve

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #19 Question CRUD MC/Essay/Interview type selector"
```

---

### Task 4.5: Mockup #22 Create Assessment single (complex form)

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append create assessment mockup**

```html
<!-- Mockup #22: Create Assessment Single -->
<section x-show="activeMockup === 'create-assessment'" x-data="createAssessmentMock()" id="create-assessment" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">📋 #22 Create Assessment — Single</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: setup sesi assessment complete. Workflow paling complex — banyak field, tapi logical flow.</p>

  <form @submit.prevent="submit()" x-show="!submitted">
    <!-- Basic info -->
    <fieldset class="border rounded p-4 mb-4">
      <legend class="font-bold px-2">1. Info Sesi</legend>
      <label class="block mb-2">
        <span class="text-sm font-bold">Nama Sesi</span>
        <input x-model="name" class="mt-1 w-full px-3 py-2 border rounded text-sm" placeholder="Assessment OJ Kuartal 2">
      </label>
      <div class="grid md:grid-cols-2 gap-3">
        <label class="block">
          <span class="text-sm font-bold">Package</span>
          <select x-model="pkg" class="mt-1 w-full px-3 py-2 border rounded text-sm">
            <option value="">-- Pilih Package --</option>
            <option>Package OJ-2026</option>
            <option>Package IHT-Maintenance</option>
            <option>Package HSSE-Safety</option>
          </select>
        </label>
        <label class="block">
          <span class="text-sm font-bold">Kategori</span>
          <select x-model="category" class="mt-1 w-full px-3 py-2 border rounded text-sm">
            <option value="">-- Pilih Kategori --</option>
            <option>OJ</option><option>IHT</option><option>Licencor</option><option>OTS</option><option>HSSE</option><option>Proton</option>
          </select>
        </label>
      </div>
    </fieldset>

    <!-- Schedule -->
    <fieldset class="border rounded p-4 mb-4">
      <legend class="font-bold px-2">2. Schedule</legend>
      <div class="grid md:grid-cols-3 gap-3">
        <label class="block">
          <span class="text-sm font-bold">Tanggal</span>
          <input type="date" x-model="date" class="mt-1 w-full px-3 py-2 border rounded text-sm">
        </label>
        <label class="block">
          <span class="text-sm font-bold">Jam Mulai</span>
          <input type="time" x-model="time" class="mt-1 w-full px-3 py-2 border rounded text-sm">
        </label>
        <label class="block">
          <span class="text-sm font-bold">Durasi (menit)</span>
          <input type="number" x-model.number="duration" min="15" max="180" class="mt-1 w-full px-3 py-2 border rounded text-sm">
        </label>
      </div>
    </fieldset>

    <!-- Mode + Settings -->
    <fieldset class="border rounded p-4 mb-4">
      <legend class="font-bold px-2">3. Mode & Settings</legend>
      <label class="block mb-2">
        <span class="text-sm font-bold">Mode</span>
        <select x-model="mode" class="mt-1 w-full px-3 py-2 border rounded text-sm">
          <option value="Single">Single (MC auto-grade)</option>
          <option value="Mixed">Mixed (MC auto + Essay manual)</option>
        </select>
      </label>
      <label class="block mb-2">
        <span class="text-sm font-bold">Passing Score (%)</span>
        <input type="number" x-model.number="passingScore" min="0" max="100" class="mt-1 w-full px-3 py-2 border rounded text-sm">
      </label>
    </fieldset>

    <!-- Assign Users -->
    <fieldset class="border rounded p-4 mb-4">
      <legend class="font-bold px-2">4. Assign Users</legend>
      <div class="space-y-1">
        <template x-for="u in availableUsers" :key="u.id">
          <label class="flex gap-2 items-center text-sm">
            <input type="checkbox" :value="u.id" x-model="assignedIds">
            <span x-text="u.nip + ' — ' + u.name + ' (' + u.section + ')'"></span>
          </label>
        </template>
      </div>
    </fieldset>

    <button type="submit" class="bg-brand-navy text-white px-6 py-3 rounded font-bold">💾 Save Assessment</button>
    <p x-show="error" x-text="error" class="mt-3 text-sm text-red-600 shake"></p>
  </form>

  <!-- Success state -->
  <div x-show="submitted" x-transition class="text-center py-8">
    <div class="text-6xl mb-3 checkmark-pop">✅</div>
    <h3 class="text-xl font-bold text-green-700 mb-2">Assessment Created (Simulasi)</h3>
    <div class="bg-slate-50 p-4 rounded inline-block text-left text-sm space-y-1">
      <p><strong>Nama:</strong> <span x-text="name"></span></p>
      <p><strong>Package:</strong> <span x-text="pkg"></span></p>
      <p><strong>Kategori:</strong> <span x-text="category"></span></p>
      <p><strong>Schedule:</strong> <span x-text="date + ' ' + time"></span></p>
      <p><strong>Mode:</strong> <span x-text="mode"></span></p>
      <p><strong>Passing:</strong> <span x-text="passingScore + '%'"></span></p>
      <p><strong>Assigned:</strong> <span x-text="assignedIds.length + ' user'"></span></p>
    </div>
    <p class="text-xs text-slate-500 mt-3">Status: Scheduled — siap di-monitor saat jadwal.</p>
    <button @click="reset()" class="mt-4 bg-amber-400 px-6 py-2 rounded font-bold">🔄 Drill Ulang</button>
  </div>
</section>

<script>
  function createAssessmentMock() {
    return {
      name: $persist('').as('mockup_v1_ca_name'),
      pkg: $persist('').as('mockup_v1_ca_pkg'),
      category: $persist('').as('mockup_v1_ca_category'),
      date: $persist('').as('mockup_v1_ca_date'),
      time: $persist('').as('mockup_v1_ca_time'),
      duration: $persist(60).as('mockup_v1_ca_duration'),
      mode: $persist('Single').as('mockup_v1_ca_mode'),
      passingScore: $persist(70).as('mockup_v1_ca_passing'),
      assignedIds: $persist([]).as('mockup_v1_ca_assigned'),
      submitted: $persist(false).as('mockup_v1_ca_submitted'),
      error: '',
      availableUsers: [
        { id: 1, nip: 'W001', name: 'Asesor 1', section: 'RFCC' },
        { id: 2, nip: 'W002', name: 'Coach 1', section: 'DHT' },
        { id: 3, nip: 'W003', name: 'Worker 003', section: 'NGP' },
        { id: 4, nip: 'W004', name: 'Worker 004', section: 'GAST' },
      ],
      submit() {
        if (!this.name || !this.pkg || !this.category || !this.date) {
          this.error = 'Field wajib (Nama, Package, Kategori, Tanggal) belum lengkap';
          return;
        }
        if (this.assignedIds.length === 0) {
          this.error = 'Assign minimal 1 user';
          return;
        }
        this.error = '';
        this.submitted = true;
      },
      reset() {
        ['mockup_v1_ca_name','mockup_v1_ca_pkg','mockup_v1_ca_category','mockup_v1_ca_date','mockup_v1_ca_time','mockup_v1_ca_duration','mockup_v1_ca_mode','mockup_v1_ca_passing','mockup_v1_ca_assigned','mockup_v1_ca_submitted'].forEach(k => localStorage.removeItem(k));
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify create assessment**

Test:
- Submit kosong → error "Field wajib belum lengkap"
- Submit tanpa user → error "Assign minimal 1 user"
- Submit valid → success state dengan summary
- Refresh → state preserve

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #22 Create Assessment single complex form"
```

---

### Task 4.6: Mockup #23 Pre-Post variant (extension #22)

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append Pre-Post mockup**

```html
<!-- Mockup #23: Create Assessment Pre-Post -->
<section x-show="activeMockup === 'prepost'" x-data="prePostMock()" id="prepost" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">🔁 #23 Create Assessment — Pre-Post</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: setup Pre-Post group. 2 sesi linked untuk measure gain score training.</p>

  <form @submit.prevent="submit()" x-show="!submitted">
    <fieldset class="border rounded p-4 mb-4">
      <legend class="font-bold px-2">Pre-Post Group</legend>
      <label class="block mb-2">
        <span class="text-sm font-bold">Nama Group</span>
        <input x-model="groupName" class="mt-1 w-full px-3 py-2 border rounded text-sm" placeholder="Training Maintenance Q2 2026">
      </label>
      <label class="block mb-2">
        <span class="text-sm font-bold">Package</span>
        <select x-model="pkg" class="mt-1 w-full px-3 py-2 border rounded text-sm">
          <option value="">-- Pilih --</option>
          <option>Package OJ-2026</option>
          <option>Package IHT-Maintenance</option>
        </select>
      </label>
    </fieldset>

    <fieldset class="border rounded p-4 mb-4 bg-blue-50">
      <legend class="font-bold px-2 text-blue-800">📋 Pre Assessment</legend>
      <label class="block mb-2">
        <span class="text-sm font-bold">Tanggal Pre</span>
        <input type="date" x-model="preDate" class="mt-1 w-full px-3 py-2 border rounded text-sm">
      </label>
    </fieldset>

    <fieldset class="border rounded p-4 mb-4 bg-amber-50">
      <legend class="font-bold px-2 text-amber-800">📋 Post Assessment</legend>
      <label class="block mb-2">
        <span class="text-sm font-bold">Tanggal Post (harus setelah Pre)</span>
        <input type="date" x-model="postDate" class="mt-1 w-full px-3 py-2 border rounded text-sm">
      </label>
    </fieldset>

    <div class="bg-slate-50 p-3 rounded text-sm mb-4">
      <strong>💡 Catatan:</strong> Pre dan Post pakai soal level kompetensi sama (auto-linked via PrePostGroupId). Gain score = Post − Pre dihitung otomatis di Results.
    </div>

    <button type="submit" class="bg-brand-navy text-white px-6 py-3 rounded font-bold">💾 Save Pre-Post Group</button>
    <p x-show="error" x-text="error" class="mt-3 text-sm text-red-600 shake"></p>
  </form>

  <div x-show="submitted" x-transition class="text-center py-8">
    <div class="text-6xl mb-3 checkmark-pop">✅</div>
    <h3 class="text-xl font-bold text-green-700 mb-2">Pre-Post Group Created (Simulasi)</h3>
    <div class="bg-slate-50 p-4 rounded inline-block text-left text-sm space-y-1">
      <p><strong>Group:</strong> <span x-text="groupName"></span></p>
      <p><strong>Package:</strong> <span x-text="pkg"></span></p>
      <p><strong>Pre:</strong> <span x-text="preDate"></span></p>
      <p><strong>Post:</strong> <span x-text="postDate"></span></p>
      <p><strong>Linked:</strong> ✓ Auto-link via PrePostGroupId</p>
    </div>
    <button @click="reset()" class="mt-4 bg-amber-400 px-6 py-2 rounded font-bold">🔄 Drill Ulang</button>
  </div>
</section>

<script>
  function prePostMock() {
    return {
      groupName: $persist('').as('mockup_v1_pp_name'),
      pkg: $persist('').as('mockup_v1_pp_pkg'),
      preDate: $persist('').as('mockup_v1_pp_pre'),
      postDate: $persist('').as('mockup_v1_pp_post'),
      submitted: $persist(false).as('mockup_v1_pp_submitted'),
      error: '',
      submit() {
        if (!this.groupName || !this.pkg || !this.preDate || !this.postDate) {
          this.error = 'Semua field wajib diisi';
          return;
        }
        if (new Date(this.postDate) <= new Date(this.preDate)) {
          this.error = 'Tanggal Post harus SETELAH Pre';
          return;
        }
        this.error = '';
        this.submitted = true;
      },
      reset() {
        ['mockup_v1_pp_name','mockup_v1_pp_pkg','mockup_v1_pp_pre','mockup_v1_pp_post','mockup_v1_pp_submitted'].forEach(k => localStorage.removeItem(k));
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify Pre-Post**

Test:
- Post date sebelum Pre → error "Post harus SETELAH Pre"
- Valid → success dengan linked confirmation

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #23 Pre-Post group dengan date order validation"
```

---

### Task 4.7: Mockup #26 Manual Grading

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append manual grading mockup**

```html
<!-- Mockup #26: Manual Grading -->
<section x-show="activeMockup === 'manual-grading'" x-data="manualGradingMock()" id="manual-grading" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">✍️ #26 Manual Grading (Essay)</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: grade Essay submission. Konsistensi penilaian penting — baca rubric dulu sebelum batch grading.</p>

  <!-- Rubric reference -->
  <div class="bg-blue-50 border-l-4 border-blue-400 p-3 rounded text-sm mb-4">
    <strong>📋 Rubric Sample:</strong>
    <ul class="list-disc list-inside mt-1 text-xs">
      <li>90-100: Jawaban lengkap, struktur clear, contoh konkret</li>
      <li>70-89: Jawaban substansial, minor missing</li>
      <li>50-69: Jawaban basic, lack detail</li>
      <li>0-49: Jawaban tidak adekuat</li>
    </ul>
  </div>

  <!-- Submission list -->
  <div class="space-y-3">
    <template x-for="s in submissions" :key="s.id">
      <div class="border rounded p-4" :class="s.graded ? 'bg-green-50 border-green-300' : 'bg-white'">
        <div class="flex justify-between mb-2">
          <span class="text-sm font-bold" x-text="s.nip + ' — ' + s.name"></span>
          <span :class="s.graded ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'" class="px-2 py-0.5 rounded text-xs font-bold" x-text="s.graded ? 'Graded' : 'Pending'"></span>
        </div>
        <p class="text-xs text-slate-600 mb-2"><strong>Soal:</strong> <span x-text="s.question"></span></p>
        <p class="text-sm bg-slate-50 p-2 rounded italic mb-2">"<span x-text="s.answer"></span>"</p>

        <div x-show="!s.graded" class="grid md:grid-cols-2 gap-2 mt-2">
          <input type="number" min="0" max="100" x-model.number="s.tempScore" placeholder="Skor 0-100" class="px-3 py-2 border rounded text-sm">
          <input x-model="s.tempFeedback" placeholder="Feedback singkat" class="px-3 py-2 border rounded text-sm">
        </div>
        <div x-show="!s.graded" class="mt-2">
          <button @click="grade(s)" class="bg-brand-navy text-white px-4 py-1.5 rounded text-sm font-bold">Save Grade</button>
        </div>

        <div x-show="s.graded" class="text-sm mt-2 space-y-1">
          <p><strong>Skor:</strong> <span x-text="s.tempScore"></span> / 100</p>
          <p><strong>Feedback:</strong> <span x-text="s.tempFeedback"></span></p>
          <button @click="s.graded = false" class="text-xs text-amber-600 hover:underline">↻ Re-grade</button>
        </div>
      </div>
    </template>
  </div>

  <button @click="reset()" class="mt-6 bg-amber-400 px-4 py-2 rounded font-bold text-sm">🔄 Drill Ulang</button>
</section>

<script>
  function manualGradingMock() {
    return {
      submissions: $persist([
        { id: 1, nip: 'W001', name: 'Asesor 1', question: 'Jelaskan fungsi unit RFCC', answer: 'RFCC mengolah residu menjadi LPG dan propylene dengan catalytic cracking. Tekanan reaktor sekitar 1.5 bar, suhu 540°C. Output naik 95% dari feed.', graded: false, tempScore: null, tempFeedback: '' },
        { id: 2, nip: 'W002', name: 'Coach 1', question: 'Jelaskan fungsi unit RFCC', answer: 'RFCC = crack residu', graded: false, tempScore: null, tempFeedback: '' },
        { id: 3, nip: 'W003', name: 'Worker 003', question: 'Jelaskan fungsi unit RFCC', answer: 'Unit untuk memproses residu vacuum. Catalytic cracking jadi LPG, naphtha, propylene. Bagian penting kilang KPB.', graded: false, tempScore: null, tempFeedback: '' },
      ]).as('mockup_v1_grading_subs'),
      grade(s) {
        if (s.tempScore === null || s.tempScore < 0 || s.tempScore > 100) {
          alert('Skor harus 0-100');
          return;
        }
        s.graded = true;
      },
      reset() {
        localStorage.removeItem('mockup_v1_grading_subs');
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify manual grading**

Test:
- Grade tanpa skor → alert
- Grade dengan skor valid → status berubah Graded
- Re-grade button → kembali ke editable state

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #26 Manual grading Essay dengan rubric reference"
```

---

### Task 4.8: Mockup #32 Coaching Proton (multi-state approval)

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append coaching proton mockup**

```html
<!-- Mockup #32: Coaching Proton -->
<section x-show="activeMockup === 'coaching-proton'" x-data="coachingMock()" id="coaching-proton" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">🎯 #32 Coaching Proton Session</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: setup session + simulasi evidence submission + approval flow. Multi-state workflow.</p>

  <!-- Session list -->
  <div class="space-y-3">
    <template x-for="s in sessions" :key="s.id">
      <div class="border rounded p-4 bg-white">
        <div class="flex justify-between mb-2">
          <h3 class="font-bold" x-text="s.topic"></h3>
          <span :class="stateColor(s.state)" class="px-2 py-0.5 rounded text-xs font-bold" x-text="s.state"></span>
        </div>
        <p class="text-sm text-slate-600 mb-2">Coachee: <strong x-text="s.coachee"></strong> · Due: <span x-text="s.due"></span></p>
        <p class="text-sm mb-2"><strong>Deliverable:</strong> <span x-text="s.deliverable"></span></p>

        <!-- Action per state -->
        <div x-show="s.state === 'Draft'" class="bg-slate-50 p-2 rounded text-xs">
          <button @click="s.state = 'Pending Submission'" class="bg-brand-navy text-white px-3 py-1 rounded font-bold">Publish Session</button>
        </div>

        <div x-show="s.state === 'Pending Submission'" class="bg-amber-50 p-3 rounded text-sm">
          <p class="mb-2">Coachee submit evidence:</p>
          <textarea x-model="s.evidence" rows="2" class="w-full px-2 py-1 border rounded text-xs"></textarea>
          <button @click="submitEvidence(s)" class="mt-2 bg-amber-500 text-white px-3 py-1 rounded font-bold text-xs">Submit Evidence</button>
        </div>

        <div x-show="s.state === 'Pending Review'" class="bg-blue-50 p-3 rounded text-sm">
          <p class="mb-2"><strong>Evidence dari coachee:</strong></p>
          <p class="bg-white p-2 rounded italic text-xs mb-2" x-text="s.evidence"></p>
          <div class="space-x-2">
            <button @click="s.state = 'Approved'" class="bg-green-500 text-white px-3 py-1 rounded font-bold text-xs">✓ Approve</button>
            <button @click="s.state = 'Pending Submission'; s.evidence = ''" class="bg-amber-500 text-white px-3 py-1 rounded font-bold text-xs">↻ Request Revision</button>
            <button @click="s.state = 'Rejected'" class="bg-red-500 text-white px-3 py-1 rounded font-bold text-xs">✗ Reject</button>
          </div>
        </div>

        <div x-show="s.state === 'Approved'" class="bg-green-50 p-3 rounded text-sm">
          <p>✓ Session Completed. Evidence: <span class="italic" x-text="s.evidence"></span></p>
        </div>

        <div x-show="s.state === 'Rejected'" class="bg-red-50 p-3 rounded text-sm">
          <p>✗ Session Rejected. Coach memberikan feedback negatif.</p>
        </div>
      </div>
    </template>
  </div>

  <button @click="reset()" class="mt-6 bg-amber-400 px-4 py-2 rounded font-bold text-sm">🔄 Drill Ulang</button>
</section>

<script>
  function coachingMock() {
    return {
      sessions: $persist([
        { id: 1, topic: 'Belajar Procedure RFCC Startup', coachee: 'Worker 003', due: '2026-06-15', deliverable: 'Tulis SOP startup RFCC 2 halaman', state: 'Draft', evidence: '' },
        { id: 2, topic: 'Maintenance Inspection Pumps', coachee: 'Worker 004', due: '2026-06-20', deliverable: 'Lakukan 5 inspeksi + report', state: 'Pending Submission', evidence: '' },
      ]).as('mockup_v1_coaching_sessions'),
      stateColor(state) {
        return {
          'Draft': 'bg-slate-200 text-slate-700',
          'Pending Submission': 'bg-amber-100 text-amber-700',
          'Pending Review': 'bg-blue-100 text-blue-700',
          'Approved': 'bg-green-100 text-green-700',
          'Rejected': 'bg-red-100 text-red-700'
        }[state];
      },
      submitEvidence(s) {
        if (!s.evidence || s.evidence.length < 10) {
          alert('Evidence min 10 karakter');
          return;
        }
        s.state = 'Pending Review';
      },
      reset() {
        localStorage.removeItem('mockup_v1_coaching_sessions');
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify coaching proton multi-state**

Test:
- Draft → Publish → Pending Submission
- Pending Submission → submit evidence (>10 char) → Pending Review
- Pending Review → Approve / Request Revision / Reject → state berubah

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #32 Coaching Proton multi-state approval flow"
```

---

### Task 4.9: Mockup #33 Plan IDP (competency mapping)

**Files:**
- Modify: `sosialisasi-portalhc/praktik.html` (append mockup)

- [ ] **Step 1: Append plan IDP mockup**

```html
<!-- Mockup #33: Plan IDP -->
<section x-show="activeMockup === 'plan-idp'" x-data="idpMock()" id="plan-idp" class="bg-white rounded-lg shadow p-8">
  <h2 class="text-2xl font-bold brand-navy mb-2">📋 #33 Plan IDP — Individual Development Plan</h2>
  <p class="text-sm text-slate-600 mb-6">Drill: map kompetensi target + timeline untuk coachee. Concept: competency tree + milestone.</p>

  <label class="block mb-4">
    <span class="text-sm font-bold">Coachee</span>
    <select x-model="coachee" class="mt-1 w-full px-3 py-2 border rounded text-sm">
      <option>Worker 003 (Section NGP)</option>
      <option>Worker 004 (Section GAST)</option>
    </select>
  </label>

  <div class="mb-4">
    <h3 class="font-bold mb-2">Pilih Kompetensi Target (dari Silabus)</h3>
    <div class="space-y-2">
      <template x-for="comp in availableCompetencies" :key="comp.id">
        <label class="flex gap-3 items-start bg-slate-50 p-3 rounded text-sm">
          <input type="checkbox" :value="comp.id" x-model="selectedComps" class="mt-1">
          <div class="flex-1">
            <strong x-text="comp.name"></strong>
            <p class="text-xs text-slate-600" x-text="comp.desc"></p>
          </div>
          <input type="month" x-show="selectedComps.includes(comp.id)" x-model="milestones[comp.id]" class="px-2 py-1 border rounded text-xs">
        </label>
      </template>
    </div>
  </div>

  <button @click="save()" class="bg-brand-navy text-white px-6 py-2 rounded font-bold">💾 Save IDP</button>
  <p x-show="message" x-text="message" :class="messageType === 'success' ? 'text-green-600' : 'text-red-600'" class="text-sm mt-3"></p>

  <button @click="reset()" class="mt-4 bg-amber-400 px-4 py-2 rounded font-bold text-sm">🔄 Drill Ulang</button>
</section>

<script>
  function idpMock() {
    return {
      coachee: $persist('Worker 003 (Section NGP)').as('mockup_v1_idp_coachee'),
      selectedComps: $persist([]).as('mockup_v1_idp_selected'),
      milestones: $persist({}).as('mockup_v1_idp_milestones'),
      message: '',
      messageType: 'success',
      availableCompetencies: [
        { id: 1, name: 'Process Safety Management', desc: 'Pahami principles PSM, MOC, incident investigation' },
        { id: 2, name: 'Equipment Reliability', desc: 'Inspection methodology, RCA, predictive maintenance' },
        { id: 3, name: 'Refining Process Fundamentals', desc: 'Crude distillation, hydrotreating, conversion units' },
        { id: 4, name: 'Quality Control & Lab Analysis', desc: 'Sampling, testing methods, spec verification' },
        { id: 5, name: 'Environmental Compliance', desc: 'Emisi regulation, waste mgmt, ISO 14001' },
      ],
      save() {
        if (this.selectedComps.length === 0) {
          this.message = 'Pilih minimal 1 kompetensi target';
          this.messageType = 'error';
          return;
        }
        const missingMilestone = this.selectedComps.find(id => !this.milestones[id]);
        if (missingMilestone) {
          this.message = `Kompetensi pilihan harus punya milestone tanggal (kompetensi ID ${missingMilestone} belum di-set)`;
          this.messageType = 'error';
          return;
        }
        this.message = `✓ IDP saved — ${this.selectedComps.length} kompetensi target untuk ${this.coachee}`;
        this.messageType = 'success';
      },
      reset() {
        ['mockup_v1_idp_coachee','mockup_v1_idp_selected','mockup_v1_idp_milestones'].forEach(k => localStorage.removeItem(k));
        location.reload();
      }
    }
  }
</script>
```

- [ ] **Step 2: Manual verify plan IDP**

Test:
- Save tanpa pilih kompetensi → error
- Pilih kompetensi tapi tanpa milestone date → error
- Pilih + set milestone → success

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/praktik.html
git commit -m "feat(sosialisasi): mockup #33 Plan IDP competency mapping + milestone"
```

---

## Phase 5 — Screenshot Capture & Annotation

### Task 5.1: Capture Daily Ops + Master Data screenshots

**Files:**
- Create: `sosialisasi-portalhc/assets/screenshots/daily-ops/*.png` (4 file)
- Create: `sosialisasi-portalhc/assets/screenshots/master-data/*.png` (3 file)

- [ ] **Step 1: Start portal lokal**

Run:
```bash
dotnet run
```

Wait sampai `Now listening on: http://localhost:5277`

- [ ] **Step 2: Login + capture daily-ops screenshots**

Buka Chrome di `http://localhost:5277`. Login pakai admin@pertamina.com.

Capture screenshot dengan Snipping Tool (Windows) atau extension:
- `01-login-form.png` — halaman login form
- `02-dashboard.png` — dashboard landing post-login (highlight navbar dengan SVG overlay nanti)
- `03-panduan-icon.png` — top navbar dengan icon ? highlighted
- `04-notif-bell.png` — bell dropdown expanded

Save ke `sosialisasi-portalhc/assets/screenshots/daily-ops/`.

- [ ] **Step 3: Capture master-data screenshots**

Navigate ke `/Admin/ManageWorkers`:
- `08-worker-list.png` — worker list
- `08-worker-create.png` — create worker form

Navigate ke `/Admin/ImportWorkers`:
- `09-worker-import-preview.png` — Excel import preview step

Navigate ke `/Admin/CoachCoacheeMapping`:
- `13-coach-mapping.png` — mapping list

Save ke `sosialisasi-portalhc/assets/screenshots/master-data/`.

- [ ] **Step 4: Commit screenshots**

```bash
git add sosialisasi-portalhc/assets/screenshots/daily-ops/ sosialisasi-portalhc/assets/screenshots/master-data/
git commit -m "feat(sosialisasi): capture screenshots daily ops + master data clusters"
```

---

### Task 5.2: Capture CMP + CDP + Analytics screenshots

**Files:**
- Create: `sosialisasi-portalhc/assets/screenshots/cmp/*.png` (9 multi-step)
- Create: `sosialisasi-portalhc/assets/screenshots/cdp/*.png` (2)
- Create: `sosialisasi-portalhc/assets/screenshots/analytics/*.png` (4)

- [ ] **Step 1: Capture CMP cluster screenshots**

Navigate sesuai panduan:
- `/Admin/ManagePackageQuestions` → `19-question-crud.png`
- `/Admin/ImportPackageQuestions` → `20-question-import.png`
- `/Admin/ManageAssessment` → `21-manage-tabs.png` (capture 3 tabs)
- `/Admin/ManageAssessment/CreateAssessment` → `22-create-single.png`
- Pilih mode Pre-Post → `23-create-prepost.png`
- `/Admin/AssessmentMonitoring/{id}` (kalau ada sesi aktif) → `25-monitoring.png`
- Grading interface → `26-manual-grading.png`
- `/CMP/Results/{id}` → `29-results.png`
- `/CMP/CertificatePdf/{id}` (preview) → `31-certificate.png`

- [ ] **Step 2: Capture CDP cluster screenshots**

- `/CDP/CoachingProton` → `32-coaching-proton.png`
- `/CDP/PlanIdp` → `33-plan-idp.png`

- [ ] **Step 3: Capture Analytics cluster screenshots**

- `/CMP/AnalyticsDashboard` → `36-dashboard.png`
- `ExpiringSoon` view → `40-expiring-soon.png`
- `/CMP/CertificationManagement` → `43-certification.png`
- ExportResults button area → `44-export-results.png`

- [ ] **Step 4: Commit screenshots**

```bash
git add sosialisasi-portalhc/assets/screenshots/cmp/ sosialisasi-portalhc/assets/screenshots/cdp/ sosialisasi-portalhc/assets/screenshots/analytics/
git commit -m "feat(sosialisasi): capture screenshots CMP + CDP + Analytics clusters"
```

---

### Task 5.3: SVG overlay annotation per screenshot

**Files:**
- Modify: `sosialisasi-portalhc/panduan.html` (insert `<img>` + SVG overlay per article)

- [ ] **Step 1: Add SVG annotation per Quick Start article**

Per article di Quick Start, insert sebelum `</article>`:

```html
<div class="mt-4 relative inline-block">
  <img src="assets/screenshots/daily-ops/01-login-form.png" alt="Login form" class="rounded border max-w-full">
  <svg class="absolute inset-0 w-full h-full pointer-events-none" viewBox="0 0 800 600" preserveAspectRatio="none">
    <!-- Panah ke email field -->
    <path d="M 100 200 L 250 280" stroke="#ed1c24" stroke-width="3" fill="none" marker-end="url(#arrow)"/>
    <text x="80" y="195" fill="#ed1c24" font-size="14" font-weight="bold">Input email kantor</text>
    <!-- Marker definitions -->
    <defs>
      <marker id="arrow" markerWidth="10" markerHeight="10" refX="9" refY="3" orient="auto">
        <path d="M 0 0 L 10 3 L 0 6 z" fill="#ed1c24"/>
      </marker>
    </defs>
  </svg>
</div>
```

Ulangi untuk semua article (sesuaikan coordinates per screenshot).

- [ ] **Step 2: Manual verify annotation tampil**

Buka panduan.html di Chrome. Verify:
- Screenshot render dengan SVG overlay
- Panah merah + text annotation visible

- [ ] **Step 3: Commit**

```bash
git add sosialisasi-portalhc/panduan.html
git commit -m "feat(sosialisasi): SVG overlay annotation untuk 22 item panduan screenshots"
```

---

## Phase 6 — Bundle & QA

### Task 6.1: Build-zip PowerShell script

**Files:**
- Create: `sosialisasi-portalhc/build-zip.ps1`

- [ ] **Step 1: Write build-zip.ps1**

```powershell
# Build offline zip distribution
$version = "v1.0"
$outputName = "sosialisasi-portalhc-$version.zip"
$sourceDir = $PSScriptRoot

# Cleanup existing zip
if (Test-Path $outputName) {
    Remove-Item $outputName -Force
    Write-Host "Removed existing $outputName"
}

# Items to include
$items = @(
    "index.html",
    "sosialisasi.html",
    "panduan.html",
    "praktik.html",
    "README.md",
    "CHANGELOG.md",
    "assets"
)

$itemPaths = $items | ForEach-Object { Join-Path $sourceDir $_ }

# Compress
Compress-Archive -Path $itemPaths -DestinationPath $outputName -CompressionLevel Optimal

$size = (Get-Item $outputName).Length / 1MB
Write-Host "✓ Built $outputName ($([math]::Round($size, 2)) MB)"

# Verify size warning
if ($size -gt 50) {
    Write-Warning "Zip lebih dari 50MB. Pertimbangkan optimize screenshots."
}
```

- [ ] **Step 2: Test build script**

Run di PowerShell:
```powershell
cd sosialisasi-portalhc
.\build-zip.ps1
```

Expected: `sosialisasi-portalhc-v1.0.zip` ter-create, size info displayed.

- [ ] **Step 3: Commit build script**

```bash
git add sosialisasi-portalhc/build-zip.ps1
git commit -m "feat(sosialisasi): PowerShell build-zip script untuk distribusi offline"
```

---

### Task 6.2: Test extract + buka di fresh location

**Files:** —

- [ ] **Step 1: Extract zip ke folder temp**

```powershell
$temp = "$env:TEMP\sosialisasi-test"
Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive -Path "sosialisasi-portalhc-v1.0.zip" -DestinationPath $temp
```

- [ ] **Step 2: Buka index.html di Chrome dari folder temp**

```powershell
Start-Process "$temp\index.html"
```

Verify:
- Hub landing render
- 3 card clickable
- Klik card "Sosialisasi" → buka sosialisasi.html
- Slide navigation working (next/prev + keyboard)
- Klik card "Panduan" → buka panduan.html
- Tab switcher working
- Quiz interactive
- Klik card "Praktik" → buka praktik.html
- Sidebar nav working
- Mockup forms working + state persist

- [ ] **Step 3: Cleanup temp**

```powershell
Remove-Item $temp -Recurse -Force
```

---

### Task 6.3: Cross-browser test + final commit + release tag

**Files:** —

- [ ] **Step 1: Test di Chrome + Edge**

Extract zip lagi → buka index.html di Chrome → run melalui semua 4 file → verify no JS error di console.

Repeat di Edge. Verify rendering identik.

- [ ] **Step 2: Update CHANGELOG dengan release date**

Edit `sosialisasi-portalhc/CHANGELOG.md` — confirm v1.0 entry tanggal release.

- [ ] **Step 3: Final commit + tag**

```bash
git add sosialisasi-portalhc/CHANGELOG.md
git commit -m "release(sosialisasi): v1.0 — siap distribusi peserta"
git tag -a sosialisasi-v1.0 -m "Sosialisasi PortalHC v1.0 — 4 file interactive HTML"
```

- [ ] **Step 4: Distribusi**

Distribute zip via email/shared drive/USB ke peserta H-1 acara. Sertakan README extract instruction.

---

## Verification (End-to-End)

Setelah semua phase done:

- [ ] **Manual smoke test** — extract fresh zip, buka index.html, navigate 4 file, test 8 mockup, test 5 quiz, verify state persist on refresh
- [ ] **Cross-browser** — Chrome + Edge desktop, no console errors
- [ ] **Screenshot accuracy** — verify capture sesuai workflow real portal
- [ ] **Content accuracy** — slide 12/14 AD claim sudah di-fix
- [ ] **Quiz answers** — semua quiz reveal jawaban + alasan
- [ ] **Mockup persist** — state survive refresh
- [ ] **Reset buttons** — semua mockup punya "Drill Ulang"
- [ ] **Cross-link** — Panduan → Praktik anchor working
- [ ] **Footer link** — Panduan link ke `wwwroot/documents/guides/` (cek saat akses portal LAN)

---

## Self-Review Notes

Coverage spec vs plan:
- ✅ Hub (Phase 1) — match spec §2.1
- ✅ Slide overview 15 slide (Phase 2) — match spec §2.2 dengan AD fix
- ✅ Panduan 22 item + 5 quiz (Phase 3) — match spec §2.3
- ✅ Mockup 8 workflow + persist (Phase 4) — match spec §2.4
- ✅ Screenshot annotation (Phase 5) — match spec §7
- ✅ Bundle PowerShell script (Phase 6) — match spec §4
- ✅ Definition of Done — tercakup di verification checklist + commit gates
- ✅ Versioning — sebagai v1.0 tag + CHANGELOG

Type consistency: localStorage key follow pattern `mockup_v1_<workflow>_<field>` konsisten across mockup.

No placeholders detected. All steps have concrete code or commands.
