# Sosialisasi-v2 Revisi R2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Revisi sosialisasi-v2.html R2 — merge 3 BigMenu slide jadi 1 slide (20→18), simplifikasi definisi CMP/CDP, fix grid overflow bug slide 8 & 9, plus minor tweak slide 2 & slide 6.

**Architecture:** Single HTML file edit. Mengikuti pola R1 (`sosialisasi-v2-revisi.md`): edit content dulu (no renumber), kemudian renumber x-show bottom-up untuk avoid line shift confusion, terakhir update total state Alpine. Setiap task = 1 atomic commit.

**Tech Stack:** HTML5 + Alpine.js (existing) + Tailwind CDN. No new dependencies.

---

## Context

v2.1 (tag `sosialisasi-v2.1`) sudah merge ke main 2026-05-13. User open di browser dan ada gap baru:

1. Slide 2 "For Future" tidak menonjol secara visual — perlu **bold + italic**.
2. Slide 3/4/5 (3 BigMenu terpisah, 1 slide per platform) terlalu boros 3 slide.
3. Definisi CMP/CDP yang ada sekarang gak match dengan teks resmi Pertamina (image #3 dan #4 dari user).
4. Card "Assessment OJT" di slide 3 terlalu spesifik.
5. Slide 6 sebut `Models/UserRoles.cs` di catatan kaki — audience non-teknis, harus dibuang.
6. **Audit Playwright** (2026-05-13): slide 8 (Pre/Post Test) dan slide 9 (Alur OJT) ada **bug overflow grid** — step ke-4 wrap ke baris bawah karena `grid-cols-9` dengan total `col-span` = 10. Slide 17 & 18 visually OK (grid-cols-11 math match).

Spec lengkap: `docs/superpowers/specs/2026-05-13-sosialisasi-v2-revisi-r2-design.md`.

---

## File Structure

**File yang disentuh (satu file):**
- `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — semua revisi.

**Branch baru:** `sosialisasi-v2-revisi-r2` di-cabang dari `main`.

**Output:** Tag `sosialisasi-v2.2` setelah QA pass.

---

## Slide Mapping (BEFORE → AFTER)

| # Lama | # Baru | Aksi |
|---|---|---|
| 1 | 1 | unchanged |
| 2 | 2 | minor — "For Future" bold+italic |
| 3 | 3 | **REWRITE** — merged 3 platform (CMP/CDP/BP) 3-column |
| 4 | (delete) | konten masuk slide 3 |
| 5 | (delete) | konten masuk slide 3 |
| 6 | 4 | minor — hapus catatan UserRoles.cs |
| 7 | 5 | unchanged |
| 8 | 6 | **FIX grid bug** |
| 9 | 7 | **FIX grid bug** |
| 10 | 8 | unchanged |
| 11 | 9 | unchanged |
| 12 | 10 | unchanged |
| 13 | 11 | unchanged |
| 14 | 12 | unchanged |
| 15 | 13 | unchanged |
| 16 | 14 | unchanged |
| 17 | 15 | unchanged (audit pass) |
| 18 | 16 | unchanged (audit pass) |
| 19 | 17 | unchanged |
| 20 | 18 | unchanged |

---

## Task 0: Setup Branch & Baseline

**Files:**
- Verify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`

- [ ] **Step 1: Check git clean + branch out**

```bash
git status
git checkout -b sosialisasi-v2-revisi-r2
```

Expected: `On branch sosialisasi-v2-revisi-r2`.

- [ ] **Step 2: Verify baseline 20 slide + total: 20**

```bash
grep -cE "<!-- Slide [0-9]+:" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -c "total: 20" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `20` dan `1`.

---

## Task 1: Slide 2 — "For Future" Bold+Italic

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 2 paragraph definisi.

- [ ] **Step 1: Apply bold+italic ke "For Future"**

```
old: <strong class="brand-red">BP</strong> (Business Partner &mdash; For Future).
new: <strong class="brand-red">BP</strong> (Business Partner &mdash; <strong><em>For Future</em></strong>).
```

- [ ] **Step 2: Verify**

```bash
grep -c "<strong><em>For Future</em></strong>" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `1`.

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 2 — 'For Future' bold+italic

Visual emphasis bahwa BP belum implemented."
```

---

## Task 2: Slide 6 (Role) — Hapus Catatan UserRoles.cs

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 6 footer catatan.

- [ ] **Step 1: Hapus referensi UserRoles.cs di catatan kaki**

```
old: <strong>Catatan:</strong> Hover tiap role untuk detail akses. Hierarki sesuai <code>Models/UserRoles.cs</code> &mdash; Admin (L1) akses penuh, Coachee (L6) akses operasional pekerja.
new: <strong>Catatan:</strong> Hover tiap role untuk detail akses. Admin (L1) akses penuh, Coachee (L6) akses operasional pekerja.
```

Catatan: text di file mungkin pakai `—` (em dash) bukan `&mdash;`. Cek dulu via Read sebelum Edit untuk exact match.

- [ ] **Step 2: Verify**

```bash
grep -c "Models/UserRoles.cs" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `0`.

- [ ] **Step 3: Commit**

```bash
git commit -am "style(sosialisasi-v2): slide 6 — hapus catatan UserRoles.cs

Audience non-teknis, referensi source file tidak perlu di slide."
```

---

## Task 3: Slide 8 (Pre/Post Test) — Fix Grid Overflow

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 8 flow grid.

**Bug:** `grid-cols-9` dengan 3×`col-span-2` + 1×`col-span-1` + 3 arrow = total 10 → step 4 "Gain Score" wrap baris 2.

**Fix:** Ganti ke `grid-cols-11`, 4 card semua `col-span-2` (equal width), 3 arrow `col-span-1`. Total 8+3 = 11 ✓.

- [ ] **Step 1: Replace flow grid block**

```
Find:
            <!-- Flow 4 step horizontal -->
            <div class="grid grid-cols-9 gap-2 items-stretch mb-6">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📋</div>
                <div class="font-bold brand-navy text-sm mb-1">Pre Test</div>
                <div class="text-xs text-slate-600">Sebelum training, ukur baseline kompetensi peserta</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">🎓</div>
                <div class="font-bold brand-navy text-sm mb-1">Training / OJT</div>
                <div class="text-xs text-slate-600">Sesi pembelajaran (in-class atau on-the-job)</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">✅</div>
                <div class="font-bold brand-navy text-sm mb-1">Post Test</div>
                <div class="text-xs text-slate-600">Setelah training, ujian dengan paket soal sejenis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-1 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">4</div>
                <div class="text-2xl mb-1">📊</div>
                <div class="font-bold text-sm" style="color: var(--green)">Gain Score</div>
                <div class="text-xs text-slate-600">Analisis</div>
              </div>
            </div>
```

Replace with:

```html
            <!-- Flow 4 step horizontal -->
            <div class="grid grid-cols-11 gap-2 items-stretch mb-6">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📋</div>
                <div class="font-bold brand-navy text-sm mb-1">Pre Test</div>
                <div class="text-xs text-slate-600">Sebelum training, ukur baseline kompetensi peserta</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">🎓</div>
                <div class="font-bold brand-navy text-sm mb-1">Training / OJT</div>
                <div class="text-xs text-slate-600">Sesi pembelajaran (in-class atau on-the-job)</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">✅</div>
                <div class="font-bold brand-navy text-sm mb-1">Post Test</div>
                <div class="text-xs text-slate-600">Setelah training, ujian dengan paket soal sejenis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">4</div>
                <div class="text-2xl mb-1">📊</div>
                <div class="font-bold text-sm" style="color: var(--green)">Gain Score</div>
                <div class="text-xs text-slate-600">Analisis selisih skor</div>
              </div>
            </div>
```

- [ ] **Step 2: Verify**

Run Playwright `#slide-8` screenshot, confirm 4 card single row, step 4 tidak wrap. (Atau spot-check via grep `grid-cols-11` count naik 1 dari sebelumnya.)

```bash
grep -cE "grid grid-cols-11 gap-2 items-stretch mb-6" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `1` (slide 8 baru).

- [ ] **Step 3: Commit**

```bash
git commit -am "fix(sosialisasi-v2): slide 8 grid overflow — Pre/Post Test step 4 wrap

grid-cols-9 dengan total col-span 10 → step 4 wrap baris 2.
Fix: grid-cols-11, 4 card col-span-2 (equal width), 3 arrow col-span-1.
Audit: Playwright screenshot confirm step 4 stay row 1 after fix."
```

---

## Task 4: Slide 9 (Alur OJT) — Fix Grid Overflow Row 1 + Standardize Row 2

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — slide 9 Row 1 + Row 2.

**Bug Row 1:** Same overflow `grid-cols-9` total 10 → step 4 "Monitoring" wrap.

**Fix Row 1:** `grid-cols-11`, 4 card `col-span-2` + 3 arrow `col-span-1` = 11.

**Row 2 standardize:** existing 3 card (col-span-3 + col-span-2 + col-span-2 + 2 arrow = 9). Ubah ke `grid-cols-11` dengan 3 card `col-span-3` + 2 arrow `col-span-1` = 9+2 = 11.

- [ ] **Step 1: Replace Row 1 (step 1-4)**

```
Find:
            <!-- Row 1: Step 1-4 with arrows -->
            <div class="grid grid-cols-9 gap-2 items-stretch mb-3">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📁</div>
                <div class="font-bold brand-navy mb-1 text-sm">Persiapan Data</div>
                <div class="text-xs text-slate-600">Kategori per unit, organisasi, daftar pekerja</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">📝</div>
                <div class="font-bold brand-navy mb-1 text-sm">Buat Assessment</div>
                <div class="text-xs text-slate-600">Pilih kategori, set durasi &amp; soal</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">💻</div>
                <div class="font-bold brand-navy mb-1 text-sm">Peserta Ujian</div>
                <div class="text-xs text-slate-600">Login portal, <strong>sistem random soal otomatis</strong>, kerjakan timer</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-1 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">👁️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Monitoring</div>
                <div class="text-xs text-slate-600">Pantau real-time</div>
              </div>
            </div>
```

Replace with:

```html
            <!-- Row 1: Step 1-4 with arrows -->
            <div class="grid grid-cols-11 gap-2 items-stretch mb-3">
              <div class="col-span-2 flow-step">
                <div class="step-num">1</div>
                <div class="text-2xl mb-1">📁</div>
                <div class="font-bold brand-navy mb-1 text-sm">Persiapan Data</div>
                <div class="text-xs text-slate-600">Kategori per unit, organisasi, daftar pekerja</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">2</div>
                <div class="text-2xl mb-1">📝</div>
                <div class="font-bold brand-navy mb-1 text-sm">Buat Assessment</div>
                <div class="text-xs text-slate-600">Pilih kategori, set durasi &amp; soal</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">3</div>
                <div class="text-2xl mb-1">💻</div>
                <div class="font-bold brand-navy mb-1 text-sm">Peserta Ujian</div>
                <div class="text-xs text-slate-600">Login portal, <strong>sistem random soal otomatis</strong>, kerjakan timer</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">4</div>
                <div class="text-2xl mb-1">👁️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Monitoring</div>
                <div class="text-xs text-slate-600">Pantau real-time, akhiri manual jika perlu</div>
              </div>
            </div>
```

- [ ] **Step 2: Replace Row 2 (step 5-7) — standardize ke grid-cols-11**

```
Find:
            <!-- Row 2: Step 5-7 with arrows -->
            <div class="grid grid-cols-9 gap-2 items-stretch mt-1">
              <div class="col-span-3 flow-step">
                <div class="step-num">5</div>
                <div class="text-2xl mb-1">📤</div>
                <div class="font-bold brand-navy mb-1 text-sm">Submit Ujian</div>
                <div class="text-xs text-slate-600">Manual atau auto-submit saat timer habis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step">
                <div class="step-num">6</div>
                <div class="text-2xl mb-1">⚙️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Penilaian Otomatis</div>
                <div class="text-xs text-slate-600">Skor vs passing grade otomatis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-2 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">7</div>
                <div class="text-2xl mb-1">🏆</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Hasil &amp; Laporan</div>
                <div class="text-xs text-slate-600">Sertifikasi + rekap per unit/kategori</div>
              </div>
            </div>
```

Replace with:

```html
            <!-- Row 2: Step 5-7 with arrows -->
            <div class="grid grid-cols-11 gap-2 items-stretch mt-1">
              <div class="col-span-3 flow-step">
                <div class="step-num">5</div>
                <div class="text-2xl mb-1">📤</div>
                <div class="font-bold brand-navy mb-1 text-sm">Submit Ujian</div>
                <div class="text-xs text-slate-600">Manual atau auto-submit saat timer habis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-3 flow-step">
                <div class="step-num">6</div>
                <div class="text-2xl mb-1">⚙️</div>
                <div class="font-bold brand-navy mb-1 text-sm">Penilaian Otomatis</div>
                <div class="text-xs text-slate-600">Skor vs passing grade otomatis</div>
              </div>
              <div class="flow-arrow">→</div>
              <div class="col-span-3 flow-step" style="border-color: var(--green); background: #f0fdf4;">
                <div class="step-num" style="background: var(--green);">7</div>
                <div class="text-2xl mb-1">🏆</div>
                <div class="font-bold mb-1 text-sm" style="color: var(--green)">Hasil &amp; Laporan</div>
                <div class="text-xs text-slate-600">Sertifikasi + rekap per unit/kategori</div>
              </div>
            </div>
```

- [ ] **Step 3: Verify**

```bash
grep -cE "grid grid-cols-9" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `0` (semua sudah grid-cols-11 atau grid-cols-3 untuk kasus non-alur).

- [ ] **Step 4: Commit**

```bash
git commit -am "fix(sosialisasi-v2): slide 9 Alur OJT — grid overflow Row 1 + standardize Row 2

Row 1: grid-cols-9 total 10 → step 4 'Monitoring' wrap. Fix grid-cols-11.
Row 2: standardize grid-cols-11 dengan 3 card col-span-3 + 2 arrow.
Konsistensi dengan slide 17/18 (semua alur grid-cols-11)."
```

---

## Task 5: Merge Slide 3/4/5 → Slide 3 Tiga Platform

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — replace slide 3 + delete slide 4 + delete slide 5 dengan single block.

**Strategy:** Edit dengan old_string mencakup `<!-- Slide 3: BigMenu CMP -->` sampai `</section>` slide 5 (end), replace dengan block slide 3 merged baru saja.

- [ ] **Step 1: Replace 3 slide existing dengan 1 slide merged**

Find seluruh block dari komentar `<!-- Slide 3: BigMenu CMP -->` sampai `</section>` terakhir milik slide 5 (sebelum `<!-- Slide 6: Role Piramida 6 Tier -->`). Gunakan Read tool dulu untuk capture exact existing content, lalu Edit dengan old_string panjang.

Replace with:

```html
        <!-- Slide 3: Tiga Platform PortalHC (merged CMP/CDP/BP) -->
        <section class="slide" x-show="current === 3" x-transition:enter="transition ease-out duration-500" x-transition:enter-start="opacity-0 translate-x-8" x-transition:enter-end="opacity-100 translate-x-0">
          <div class="max-w-6xl mx-auto w-full">
            <div class="text-sm uppercase tracking-wide brand-red font-bold mb-2">Big Menu</div>
            <h2 class="text-3xl font-bold brand-navy mb-1">Tiga Platform PortalHC KPB</h2>
            <p class="text-sm text-slate-500 mb-4">Manajemen · Pengembangan · Strategic Partner</p>

            <div class="grid grid-cols-3 gap-4">
              <!-- Kolom 1: CMP -->
              <div class="card-hover bg-white border-t-4 border-brand-navy rounded-lg p-4 shadow-sm">
                <h3 class="text-2xl font-bold brand-navy mb-1">CMP</h3>
                <div class="text-xs uppercase text-slate-500 mb-2">Competency Management</div>
                <p class="text-xs text-slate-700 mb-3">Platform digital untuk pengelolaan kompetensi secara terintegrasi &mdash; penyusunan kebutuhan kompetensi jabatan, pelaksanaan asesmen teknis &amp; leadership, serta IDP.</p>
                <ul class="text-xs text-slate-700 space-y-1.5 mb-3">
                  <li>📊 <strong>Assessment</strong> &mdash; Ujian online &amp; sertifikasi kompetensi</li>
                  <li>🎓 <strong>Assessment Proton</strong> &mdash; Program 3-tahun</li>
                  <li>🔄 <strong>Pre / Post Test</strong> &mdash; Ukur efektivitas training</li>
                  <li>🏆 <strong>Sertifikasi</strong> &mdash; Otomatis + renewal lifecycle</li>
                </ul>
                <div class="bg-slate-50 rounded p-2 text-xs text-slate-700">
                  <strong>Role:</strong> Admin · HC · Coachee
                </div>
              </div>

              <!-- Kolom 2: CDP -->
              <div class="card-hover bg-white border-t-4 border-brand-red rounded-lg p-4 shadow-sm">
                <h3 class="text-2xl font-bold brand-red mb-1">CDP</h3>
                <div class="text-xs uppercase text-slate-500 mb-2">Competency Development</div>
                <p class="text-xs text-slate-700 mb-3">Pembelajaran terstruktur untuk menutup gap kompetensi teknis hasil asesmen &mdash; prinsip blended Learning (Assignment, Coaching, Self Study).</p>
                <ul class="text-xs text-slate-700 space-y-1.5 mb-3">
                  <li>🎯 <strong>Coaching Proton</strong> &mdash; Silabus + deliverable + review multi-role</li>
                  <li>📋 <strong>IDP</strong> &mdash; Individual Development Plan tahunan</li>
                  <li>📚 <strong>Training Records</strong> &mdash; Riwayat training + sertifikat</li>
                </ul>
                <div class="bg-slate-50 rounded p-2 text-xs text-slate-700">
                  <strong>Role:</strong> HC · Coach · SrSpv · SH · Coachee
                </div>
              </div>

              <!-- Kolom 3: BP (For Future) -->
              <div class="bg-slate-50 border-t-4 border-dashed border-slate-400 rounded-lg p-4 shadow-sm" style="opacity: 0.75;">
                <div class="flex items-center gap-2 mb-1">
                  <h3 class="text-2xl font-bold text-slate-500">BP</h3>
                  <span class="px-2 py-0.5 bg-amber-100 text-amber-800 text-[10px] font-bold rounded-full uppercase">🚧 Coming Soon</span>
                </div>
                <div class="text-xs uppercase text-slate-500 mb-2">Business Partner</div>
                <p class="text-xs text-slate-600 mb-3">Modul <strong>HRBP</strong> &mdash; strategic partner antara HC &amp; unit operasional untuk workforce planning, employee relations, &amp; advisory.</p>
                <ul class="text-xs text-slate-600 space-y-1.5 mb-3">
                  <li style="filter: grayscale(0.4);">🤝 <strong>Workforce Planning</strong></li>
                  <li style="filter: grayscale(0.4);">👁️ <strong>Employee Relations</strong></li>
                  <li style="filter: grayscale(0.4);">💡 <strong>Strategic Advisory</strong></li>
                </ul>
                <div class="bg-amber-50 border-l-2 border-amber-400 rounded p-2 text-xs text-amber-900">
                  <strong><em>For Future</em></strong> &mdash; in roadmap
                </div>
              </div>
            </div>
          </div>
        </section>
```

(Slide 4 BigMenu CDP existing dan Slide 5 BigMenu BP existing dihapus.)

- [ ] **Step 2: Verify**

```bash
grep -c "Tiga Platform PortalHC" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -c '<!-- Slide 4: BigMenu CDP -->' sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -c '<!-- Slide 5: BigMenu BP' sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -cE "<!-- Slide [0-9]+:" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `1`, `0`, `0`, `18` (total slide turun jadi 18).

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(sosialisasi-v2): merge slide 3/4/5 → slide 3 'Tiga Platform PortalHC'

3-column layout side-by-side: CMP (navy) | CDP (red) | BP muted (For Future).
Definisi resmi Pertamina disederhanakan jadi 1 kalimat per platform.
Card 'Assessment OJT' → 'Assessment' general.
Hapus 2 slide → total 20 → 18 slide."
```

---

## Task 6: Renumber x-show 6→4 ... 20→18 (Bottom-Up)

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — 15 rename komentar `<!-- Slide N: ... -->` + `x-show="current === N"`.

**Order bottom-up untuk avoid line shift:**
- 20 → 18 (Closing)
- 19 → 17 (Timeline)
- 18 → 16 (Coaching Th3)
- 17 → 15 (Coaching Th1-2)
- 16 → 14 (Fokus)
- 15 → 13 (Hierarki)
- 14 → 12 (IDP/Training)
- 13 → 11 (Coaching CDP Overview)
- 12 → 10 (Proton Th3)
- 11 → 9 (Proton Th1-2)
- 10 → 8 (Assessment Proton)
- 9 → 7 (Alur OJT — was fixed in Task 4)
- 8 → 6 (Pre/Post Test — was fixed in Task 3)
- 7 → 5 (Sistem Assessment CMP)
- 6 → 4 (Role Piramida — was minor in Task 2)

- [ ] **Step 1: Rename slide 20 → 18 (Closing)**

```
old: <!-- Slide 20: Closing -->
new: <!-- Slide 18: Closing -->

old: <section class="slide" x-show="current === 20"
new: <section class="slide" x-show="current === 18"
```

- [ ] **Step 2: Rename slide 19 → 17 (Timeline)**

```
old: <!-- Slide 19: Timeline Summary -->
new: <!-- Slide 17: Timeline Summary -->

old: <section class="slide" x-show="current === 19"
new: <section class="slide" x-show="current === 17"
```

- [ ] **Step 3: Rename slide 18 → 16 (Coaching Th3)**

```
old: <!-- Slide 18: Alur Coaching Th 3 -->
new: <!-- Slide 16: Alur Coaching Th 3 -->

old: <section class="slide" x-show="current === 18"
new: <section class="slide" x-show="current === 16"
```

- [ ] **Step 4: Rename slide 17 → 15 (Coaching Th1-2)**

```
old: <!-- Slide 17: Alur Coaching Th 1-2 -->
new: <!-- Slide 15: Alur Coaching Th 1-2 -->

old: <section class="slide" x-show="current === 17"
new: <section class="slide" x-show="current === 15"
```

- [ ] **Step 5: Rename slide 16 → 14 (Fokus Kompetensi)**

```
old: <!-- Slide 16: Fokus Kompetensi (Table 4x5) -->
new: <!-- Slide 14: Fokus Kompetensi (Table 4x5) -->

old: <section class="slide" x-show="current === 16"
new: <section class="slide" x-show="current === 14"
```

- [ ] **Step 6: Rename slide 15 → 13 (Hierarki)**

```
old: <!-- Slide 15: Hierarki Kompetensi -->
new: <!-- Slide 13: Hierarki Kompetensi -->

old: <section class="slide" x-show="current === 15"
new: <section class="slide" x-show="current === 13"
```

- [ ] **Step 7: Rename slide 14 → 12 (IDP/Training)**

```
old: <!-- Slide 14: IDP & Training Records -->
new: <!-- Slide 12: IDP & Training Records -->

old: <section class="slide" x-show="current === 14"
new: <section class="slide" x-show="current === 12"
```

- [ ] **Step 8: Rename slide 13 → 11 (Coaching CDP Overview)**

```
old: <!-- Slide 13: Coaching CDP Overview -->
new: <!-- Slide 11: Coaching CDP Overview -->

old: <section class="slide" x-show="current === 13"
new: <section class="slide" x-show="current === 11"
```

- [ ] **Step 9: Rename slide 12 → 10 (Proton Th3)**

```
old: <!-- Slide 12: Alur Proton Th 3 -->
new: <!-- Slide 10: Alur Proton Th 3 -->

old: <section class="slide" x-show="current === 12"
new: <section class="slide" x-show="current === 10"
```

- [ ] **Step 10: Rename slide 11 → 9 (Proton Th1-2)**

```
old: <!-- Slide 11: Alur Proton Th 1-2 -->
new: <!-- Slide 9: Alur Proton Th 1-2 -->

old: <section class="slide" x-show="current === 11"
new: <section class="slide" x-show="current === 9"
```

- [ ] **Step 11: Rename slide 10 → 8 (Assessment Proton)**

```
old: <!-- Slide 10: Assessment Proton -->
new: <!-- Slide 8: Assessment Proton -->

old: <section class="slide" x-show="current === 10"
new: <section class="slide" x-show="current === 8"
```

- [ ] **Step 12: Rename slide 9 → 7 (Alur OJT)**

```
old: <!-- Slide 9: Alur Assessment OJT -->
new: <!-- Slide 7: Alur Assessment OJT -->

old: <section class="slide" x-show="current === 9"
new: <section class="slide" x-show="current === 7"
```

- [ ] **Step 13: Rename slide 8 → 6 (Pre/Post Test)**

```
old: <!-- Slide 8: Pre/Post Test -->
new: <!-- Slide 6: Pre/Post Test -->

old: <section class="slide" x-show="current === 8"
new: <section class="slide" x-show="current === 6"
```

- [ ] **Step 14: Rename slide 7 → 5 (Sistem Assessment CMP)**

```
old: <!-- Slide 7: Sistem Assessment CMP -->
new: <!-- Slide 5: Sistem Assessment CMP -->

old: <section class="slide" x-show="current === 7"
new: <section class="slide" x-show="current === 5"
```

- [ ] **Step 15: Rename slide 6 → 4 (Role Piramida)**

```
old: <!-- Slide 6: Role Piramida 6 Tier -->
new: <!-- Slide 4: Role Piramida 6 Tier -->

old: <section class="slide" x-show="current === 6"
new: <section class="slide" x-show="current === 4"
```

- [ ] **Step 16: Verify sequential 1-18**

```bash
grep -nE "<!-- Slide [0-9]+:" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected output 18 baris, sequential nomor 1-18.

```bash
grep -cE 'x-show="current === [0-9]+"' sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `18`.

- [ ] **Step 17: Commit**

```bash
git commit -am "refactor(sosialisasi-v2): renumber slide 6-20 → 4-18 (bottom-up)

Setelah merge 3/4/5 → 3, slot 4 dan 5 dipakai existing slide 6 dan 7.
Bottom-up rename untuk avoid line shift confusion."
```

---

## Task 7: Update Alpine Total + Final Verify

**Files:**
- Modify: `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html` — line ~1128 `total: 20`.

- [ ] **Step 1: Update total 20 → 18**

```
old:         current: 1,
        total: 20,
new:         current: 1,
        total: 18,
```

- [ ] **Step 2: Verify**

```bash
grep -c "total: 18" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
grep -c "total: 20" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html
```

Expected: `1`, `0`.

Final slide list check:

```bash
grep -nE "<!-- Slide [0-9]+:" sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html | sed 's/.*<!--/<!--/'
```

Expected (18 baris):
```
<!-- Slide 1: Cover -->
<!-- Slide 2: Definisi -->
<!-- Slide 3: Tiga Platform PortalHC (merged CMP/CDP/BP) -->
<!-- Slide 4: Role Piramida 6 Tier -->
<!-- Slide 5: Sistem Assessment CMP -->
<!-- Slide 6: Pre/Post Test -->
<!-- Slide 7: Alur Assessment OJT -->
<!-- Slide 8: Assessment Proton -->
<!-- Slide 9: Alur Proton Th 1-2 -->
<!-- Slide 10: Alur Proton Th 3 -->
<!-- Slide 11: Coaching CDP Overview -->
<!-- Slide 12: IDP & Training Records -->
<!-- Slide 13: Hierarki Kompetensi -->
<!-- Slide 14: Fokus Kompetensi (Table 4x5) -->
<!-- Slide 15: Alur Coaching Th 1-2 -->
<!-- Slide 16: Alur Coaching Th 3 -->
<!-- Slide 17: Timeline Summary -->
<!-- Slide 18: Closing -->
```

- [ ] **Step 3: Commit**

```bash
git commit -am "chore(sosialisasi-v2): update Alpine total 20 → 18

Slide counter, dot indicators otomatis follow total via x-for=\"n in total\"."
```

---

## Task 8: Visual QA + Tag

**Files:**
- Verify only.

- [ ] **Step 1: Start lokal HTTP server**

```bash
cd sosialisasi-portalhc/sosialisasi-v2 && python -m http.server 8765 &
```

- [ ] **Step 2: Playwright screenshot 6 slide critical**

Pakai `mcp__plugin_playwright_playwright__browser_navigate` ke:
- `http://localhost:8765/sosialisasi-v2.html#slide-2` — verify "For Future" bold+italic
- `http://localhost:8765/sosialisasi-v2.html#slide-3` — verify 3 kolom merged CMP/CDP/BP fit dalam 1 viewport, BP muted
- `http://localhost:8765/sosialisasi-v2.html#slide-4` — verify role piramida footer tidak ada teks "Models/UserRoles.cs"
- `http://localhost:8765/sosialisasi-v2.html#slide-6` — verify Pre/Post Test 4 card single row, no wrap
- `http://localhost:8765/sosialisasi-v2.html#slide-7` — verify Alur OJT Row 1 (4 card no wrap) + Row 2 (3 card)
- `http://localhost:8765/sosialisasi-v2.html#slide-18` — verify counter `18/18`, dot indicator 18 buah

Capture screenshot each: `slide-2-r2.png`, `slide-3-r2.png`, dst.

- [ ] **Step 3: Manual test (user)**

Buka file di browser, klik through semua 18 slide. Periksa:
- Slide counter `1/18` … `18/18`
- 18 dot indicator
- Tombol End → slide 18
- Hash `#slide-3` jump direct ke merged BigMenu
- Dark mode toggle: semua slide readable

- [ ] **Step 4: Stop server**

```bash
taskkill //F //IM python.exe
```

- [ ] **Step 5: Tag git**

```bash
git tag sosialisasi-v2.2 -m "Sosialisasi v2.2 — 18 slide (merged BigMenu + grid bug fix)

- Slide 2: 'For Future' bold+italic
- Slide 3: merged 3 BigMenu CMP/CDP/BP jadi 1 slide (definisi resmi 1 kalimat)
- Card 'Assessment OJT' → 'Assessment' general
- Slide 6 (was slide 8): fix grid overflow Pre/Post Test
- Slide 7 (was slide 9): fix grid overflow Alur OJT
- Slide 4 (was slide 6): hapus catatan UserRoles.cs

Total slide 20 → 18."
```

- [ ] **Step 6: Merge ke main (kalau OK)**

```bash
git checkout main
git merge sosialisasi-v2-revisi-r2 --no-ff -m "merge: sosialisasi-v2 revisi R2 → 18 slide"
git branch -d sosialisasi-v2-revisi-r2
```

---

## Verification Summary

Setelah Task 8 selesai:

- File `sosialisasi-portalhc/sosialisasi-v2/sosialisasi-v2.html`:
  - 18 slide section dengan `x-show="current === N"` sequential 1-18
  - `total: 18` di Alpine `deck()`
  - Slide 2 "For Future" bold+italic
  - Slide 3 merged 3-column CMP/CDP/BP dengan definisi resmi
  - Slide 4 role piramida tanpa catatan UserRoles.cs
  - Slide 6 Pre/Post Test 4 card single row (no wrap)
  - Slide 7 Alur OJT Row 1 4 card + Row 2 3 card semua grid-cols-11
  - Slide 17/18 (Coaching Th1-2/Th3) unchanged dari v2.1 (audit pass)
- Tag `sosialisasi-v2.2`
- Branch `sosialisasi-v2-revisi-r2` deleted setelah merge
