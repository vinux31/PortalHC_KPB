# Merge Sosialisasi-Aplikasi → Internal-Tim-HC Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Merge 18 slide gap dari `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sumber) ke `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (target), bump v1.1 → v2.0, total 23 + 18 = 41 slide.

**Architecture:** In-place modify file target. Insert dalam 4 cluster di posisi anchor logis (setelah #2 Selamat Datang, sebelum #8 Pre/Post, sebelum #10 Coaching Dashboard, sebelum #22 Reference Card). Setiap slide gap = verbatim copy dari sumber + adapt palette ke File 3 (teal/amber, drop merah Pertamina) + sisip `.hc-callout` "Implikasi untuk HC". Insert dilakukan REVERSE final-position order supaya `data-slide` anchor attribute existing tidak shift.

**Tech Stack:** Static HTML5 + CSS (no build, no JS framework). Inline `<style>` di `<head>`. Verification via grep + manual browser smoke.

**Spec reference:** `docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md`

---

## File Structure

**Modified (single file):**
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — target deck, all changes in-place

**Read-only reference:**
- `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` — source 18 slide content

**Source line ranges per gap slide (File 2):**

| Final Order | Slide gap | File 2 data-slide | File 2 line range |
|---|---|---|---|
| Cluster Konteks (final pos 3-7) | | | |
| 3 | Latar Belakang | 3 | 1391-1431 |
| 4 | Apa Itu HC Portal | 4 | 1432-1470 |
| 5 | 3 Platform Terpadu | 5 | 1471-1529 |
| 6 | Struktur Role Pengguna | 6 | 1530-1591 |
| 7 | Cara Mengakses HC Portal | 21 | 2289-2327 |
| Cluster Assessment (final pos 13-15) | | | |
| 13 | Sistem Assessment | 7 | 1592-1638 |
| 14 | 5 Kategori Assessment Umum | 8 | 1639-1675 |
| 15 | Alur Assessment 7-Step E2E | 9 | 1726-1799 |
| Cluster Proton+Coaching (final pos 18-26) | | | |
| 18 | Assessment Proton | 11 | 1800-1827 |
| 19 | Alur Proton Th 1&2 | 12 | 1828-1865 |
| 20 | Alur Proton Th 3 | 13 | 1866-1902 |
| 21 | Coaching Proton Dual Track | 14 | 1903-1937 |
| 22 | IDP & Training Records | 19 | 1938-1984 |
| 23 | Hierarki Kompetensi per Track | 15 | 1985-2021 |
| 24 | Progresi Kompetensi per Tahun | 16 | 2022-2077 |
| 25 | Alur Coaching Reguler 9-Step | 17 | 2078-2163 |
| 26 | Alur Coaching Mahir 9-Step | 18 | 2164-2249 |
| Cluster Tech (final pos 39) | | | |
| 39 | Integrasi & Keamanan | 20 | 2250-2288 |

**Final slide layout (41 total):**
1 Cover · 2 Selamat Datang · **3-7 Konteks (5)** · 8 Posisi HC · 9 Alur Kerja · 10 CMP · 11 Records · 12 Analytics · **13-15 Assessment (3)** · 16 Pre/Post · 17 CDP · **18-26 Proton+Coaching (9)** · 27 Coaching Dashboard · 28 Histori Proton · 29 Renewal Cert · 30 Silabus · 31 Override Pekerja · 32 Admin Landing · 33 Manajemen Pekerja · 34 Assessment Monitoring · 35 Coach-Coachee · 36 Maintenance · 37 Notifikasi · 38 Tugas HC Cepat · **39 Integrasi & Keamanan (1)** · 40 Reference Card · 41 Penutup

---

## Palette Adaptation Rules (apply to every gap slide content)

| File 2 (sumber) | File 3 (target) | Note |
|---|---|---|
| `#ed1c24` (merah Pertamina) | `var(--teal)` `#0d9488` | Primary accent |
| `var(--red)` | `var(--teal)` | CSS variable swap |
| `#b0121a` (red-dark) | `var(--teal-dark)` `#0f766e` | Hover/active |
| `var(--red-dark)` | `var(--teal-dark)` | CSS variable swap |
| `#009640` (green Pertamina) | `var(--green)` `#10b981` | Sudah ada di File 3 |
| `var(--green)` (File 2 #009640) | `var(--green)` (File 3 #10b981) | Same name, beda hex — biarkan, akan otomatis pakai File 3 root var |
| `#007030` (green-dark) | `var(--green)` darker (manual `#059669`) | Tidak ada di File 3, fallback hex |
| `#7b1fa2` (purple) | `var(--amber-dark)` `#d97706` ATAU `var(--slate-dark)` `#334155` | Pilih per konteks: accent → amber-dark, neutral → slate-dark |
| `var(--purple)` | sda | sda |
| `var(--navy)` | `var(--teal-dark)` | Header accent |

**Cara apply:** scan setiap slide HTML copied dari File 2, replace dengan `sed`-style atau Edit tool. Verify dengan grep `(#ed1c24|#b0121a|#7b1fa2|--red|--purple|--navy)` di File 3 setelah commit → harus 0 match di range slide yang baru diinsert.

---

## Callout Content per Slide

Append `<div class="hc-callout">` ke akhir `.slide-body` setiap gap slide:

| Slide gap | Callout HC content |
|---|---|
| Latar Belakang | HC: jembatan antara user pain "manual" → adopsi platform terintegrasi |
| Apa Itu HC Portal | HC: owner data + reviewer final di semua workflow Proton |
| 3 Platform Terpadu | HC operate di semua 3 (CMP analytics, CDP coaching, BP via integrasi) |
| Struktur Role Pengguna | HC = L2 authority; scope: cross-section, final reviewer |
| Cara Mengakses HC Portal | HC akses sama dengan user umum, tapi role-gated menu (Bab 1.2 Panduan) |
| Sistem Assessment | HC: setup jadwal, bank soal, monitor real-time, force-close |
| 5 Kategori Assessment Umum | HC: assign kategori per jadwal, lihat fail rate per kategori (Analytics) |
| Alur Assessment 7-Step E2E | HC: step 1 (setup), step 5 (monitor), step 7 (manual entry + cert) |
| Assessment Proton | HC: reviewer final chain Proton, manage silabus + guidance files |
| Alur Proton Th 1&2 | HC: jaga deliverable submission timeline, eskalasi bottleneck |
| Alur Proton Th 3 | HC: validasi mahir, certification renewal management |
| Coaching Proton Dual Track | HC: monitor kedua track via Coaching Proton Dashboard |
| IDP & Training Records | HC: review IDP coachee, audit training records team |
| Hierarki Kompetensi per Track | HC: gunakan KKJ matrix untuk gap analysis CPDP Mapping |
| Progresi Kompetensi per Tahun | HC: track progresi via Analytics + Bottleneck Report |
| Alur Coaching Reguler 9-Step | HC: reviewer final di step 8-9 (approval chain) |
| Alur Coaching Mahir 9-Step | HC: validation mahir + sertifikasi (Renewal Certificate Mgmt) |
| Integrasi & Keamanan | HC: tanggung jawab audit log review, impersonate dengan justifikasi |

**Markup pattern:**
```html
<div class="hc-callout">
  <strong>Implikasi untuk HC</strong>
  <ul>
    <li>[callout content dari tabel di atas]</li>
  </ul>
</div>
```

---

## Task 1: Add `.hc-callout` CSS to File 3

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — tambahkan CSS class di `<style>` block, sebelum `</style>` (sekitar line 800ish — cek dengan grep)

- [ ] **Step 1: Locate insertion point**

Run: `grep -n '</style>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match. Note line number L.

- [ ] **Step 2: Insert CSS block immediately before `</style>` line**

Add this CSS block:

```css
  /* Callout "Implikasi untuk HC" — disisip di slide gap dari merge File 2 */
  .hc-callout {
    margin-top: 20px;
    padding: 14px 18px;
    background: rgba(245, 158, 11, 0.10);
    border-left: 4px solid var(--amber-dark);
    border-radius: 8px;
    font-size: 0.92em;
    color: var(--text);
  }
  body.dark .hc-callout {
    background: rgba(245, 158, 11, 0.15);
    border-left-color: var(--amber);
  }
  .hc-callout strong {
    display: block;
    color: var(--amber-dark);
    margin-bottom: 6px;
    font-size: 0.95em;
    letter-spacing: 0.3px;
    text-transform: uppercase;
  }
  body.dark .hc-callout strong { color: var(--amber); }
  .hc-callout ul { margin: 0; padding-left: 18px; }
  .hc-callout li { margin: 3px 0; line-height: 1.5; }
```

- [ ] **Step 3: Verify CSS inserted**

Run: `grep -c '.hc-callout' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: ≥ 7 (one per CSS selector definition).

- [ ] **Step 4: Smoke browser**

Open `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` di browser. Pastikan tidak ada CSS parse error (slides masih render normal). Toggle dark mode → tidak break.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): add .hc-callout CSS for merge gap slides"
```

---

## Task 2: Insert Cluster Tech (1 slide) before `data-slide="22"`

Insert paling akhir di urutan asli, tapi PERTAMA di plan supaya inserting later clusters tidak shift anchor `data-slide="22"`.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
- Read-only: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` lines 2250-2288

**Source slide:** File 2 `data-slide="20"` (Integrasi & Keamanan)

- [ ] **Step 1: Read source slide from File 2**

Read `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` lines 2250-2288. Copy entire `<div class="slide ...">...</div>` block.

- [ ] **Step 2: Adapt content**

Apply palette transformations dari tabel "Palette Adaptation Rules" di atas (red→teal, purple→amber-dark/slate, dll). Replace `data-slide="20"` → `data-slide="918"` (placeholder; akan direnumber di Task 6). Update slide-badge text → `SLIDE 918 / 41` (placeholder).

- [ ] **Step 3: Append callout box**

Sebelum closing `</div>` slide-body, sisip:

```html
<div class="hc-callout">
  <strong>Implikasi untuk HC</strong>
  <ul>
    <li>HC: tanggung jawab audit log review, impersonate dengan justifikasi</li>
  </ul>
</div>
```

- [ ] **Step 4: Locate insertion anchor in File 3**

Run: `grep -n 'data-slide="22"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match at line ~1709. Note line number A. Insert prepared slide block IMMEDIATELY BEFORE that line.

- [ ] **Step 5: Verify insertion**

Run: `grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 24 (was 23, +1 new).

Run: `grep -n 'data-slide="918"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match, harus muncul SEBELUM line yang punya `data-slide="22"`.

Run: `grep -E '(#ed1c24|#b0121a|--red[^a-z]|--purple|--navy)' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 match (palette adapt sukses).

- [ ] **Step 6: Smoke browser**

Buka File 3 di browser. Navigate ke slide #918 (manual atau via dev console `document.querySelector('[data-slide=\"918\"]').scrollIntoView()`). Verify konten Integrasi & Keamanan tampil, palette teal/amber, callout box amber kelihatan.

- [ ] **Step 7: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): merge slide Integrasi & Keamanan from sumber"
```

---

## Task 3: Insert Cluster Proton+Coaching (9 slides) before `data-slide="10"`

9 slides as single cluster. Inserted as ONE block (not 9 separate inserts). Plan-final order = narrative order in cluster.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
- Read-only: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`

**Source slides (in plan-final narrative order, with placeholder data-slide):**

| Plan order | Placeholder | Source File 2 data-slide | Source line range | Callout content |
|---|---|---|---|---|
| 1st in block | 909 | 11 (Assessment Proton) | 1800-1827 | HC: reviewer final chain Proton, manage silabus + guidance files |
| 2nd | 910 | 12 (Alur Proton Th 1&2) | 1828-1865 | HC: jaga deliverable submission timeline, eskalasi bottleneck |
| 3rd | 911 | 13 (Alur Proton Th 3) | 1866-1902 | HC: validasi mahir, certification renewal management |
| 4th | 912 | 14 (Coaching Proton Dual Track) | 1903-1937 | HC: monitor kedua track via Coaching Proton Dashboard |
| 5th | 913 | 19 (IDP & Training Records) | 1938-1984 | HC: review IDP coachee, audit training records team |
| 6th | 914 | 15 (Hierarki Kompetensi per Track) | 1985-2021 | HC: gunakan KKJ matrix untuk gap analysis CPDP Mapping |
| 7th | 915 | 16 (Progresi Kompetensi per Tahun) | 2022-2077 | HC: track progresi via Analytics + Bottleneck Report |
| 8th | 916 | 17 (Alur Coaching Reguler 9-Step) | 2078-2163 | HC: reviewer final di step 8-9 (approval chain) |
| 9th | 917 | 18 (Alur Coaching Mahir 9-Step) | 2164-2249 | HC: validation mahir + sertifikasi (Renewal Certificate Mgmt) |

- [ ] **Step 1: For each of 9 source slides, read + transform**

Loop:
1. Read source File 2 line range.
2. Apply palette adaptation (tabel "Palette Adaptation Rules").
3. Replace `data-slide="N"` → placeholder (909..917 sesuai tabel di atas).
4. Update slide-badge text `SLIDE N / 22` → `SLIDE 9XX / 41` (placeholder).
5. Append `.hc-callout` block sebelum closing slide-body `</div>` dengan konten dari tabel.
6. Concatenate 9 adapted blocks dalam urutan plan (909..917).

- [ ] **Step 2: Locate insertion anchor in File 3**

Run: `grep -n 'data-slide="10"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match. Note line number. Insert concatenated 9-slide block IMMEDIATELY BEFORE that line.

- [ ] **Step 3: Verify insertion**

Run: `grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 33 (24 + 9).

Run: `for i in 909 910 911 912 913 914 915 916 917; do grep -c "data-slide=\"$i\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html; done`
Expected: 1 1 1 1 1 1 1 1 1.

Run: `grep -n 'data-slide="9[0-9][0-9]"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | head -20`
Expected: 909..917 muncul berurutan sebelum line `data-slide="10"`, plus 918 dari Task 2 di posisinya sendiri.

Run: `grep -E '(#ed1c24|#b0121a|--red[^a-z]|--purple|--navy)' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 match.

- [ ] **Step 4: Smoke browser**

Buka File 3. Navigate ke slide 909..917 satu-satu (dev console set data-slide). Verify semua tampil, palette teal/amber, callout amber visible, konten Proton+Coaching lengkap.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): merge 9-slide Proton+Coaching cluster from sumber"
```

---

## Task 4: Insert Cluster Assessment (3 slides) before `data-slide="8"`

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
- Read-only: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`

**Source slides:**

| Plan order | Placeholder | Source File 2 data-slide | Source line range | Callout content |
|---|---|---|---|---|
| 1st | 906 | 7 (Sistem Assessment) | 1592-1638 | HC: setup jadwal, bank soal, monitor real-time, force-close |
| 2nd | 907 | 8 (5 Kategori Assessment Umum) | 1639-1675 | HC: assign kategori per jadwal, lihat fail rate per kategori (Analytics) |
| 3rd | 908 | 9 (Alur Assessment 7-Step E2E) | 1726-1799 | HC: step 1 (setup), step 5 (monitor), step 7 (manual entry + cert) |

- [ ] **Step 1: For each of 3 source slides, read + transform**

Sama seperti Task 3 Step 1, tapi 3 slide saja. Placeholder data-slide 906..908.

- [ ] **Step 2: Locate insertion anchor**

Run: `grep -n 'data-slide="8"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match. Insert concatenated 3-slide block IMMEDIATELY BEFORE.

- [ ] **Step 3: Verify**

Run: `grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 36 (33 + 3).

Run: `for i in 906 907 908; do grep -c "data-slide=\"$i\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html; done`
Expected: 1 1 1.

Run: `grep -E '(#ed1c24|#b0121a|--red[^a-z]|--purple|--navy)' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 match.

- [ ] **Step 4: Smoke browser**

Navigate ke slide 906..908. Verify konten Assessment lifecycle tampil, callout visible.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): merge 3-slide Assessment Lifecycle cluster from sumber"
```

---

## Task 5: Insert Cluster Konteks (5 slides) after `data-slide="2"` (before original `data-slide="3"`)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
- Read-only: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`

**Source slides:**

| Plan order | Placeholder | Source File 2 data-slide | Source line range | Callout content |
|---|---|---|---|---|
| 1st | 901 | 3 (Latar Belakang) | 1391-1431 | HC: jembatan antara user pain "manual" → adopsi platform terintegrasi |
| 2nd | 902 | 4 (Apa Itu HC Portal) | 1432-1470 | HC: owner data + reviewer final di semua workflow Proton |
| 3rd | 903 | 5 (3 Platform Terpadu) | 1471-1529 | HC operate di semua 3 (CMP analytics, CDP coaching, BP via integrasi) |
| 4th | 904 | 6 (Struktur Role Pengguna) | 1530-1591 | HC = L2 authority; scope: cross-section, final reviewer |
| 5th | 905 | 21 (Cara Mengakses HC Portal) | 2289-2327 | HC akses sama dengan user umum, tapi role-gated menu (Bab 1.2 Panduan) |

- [ ] **Step 1: For each of 5 source slides, read + transform**

Sama seperti Task 3 Step 1, tapi 5 slide. Placeholder data-slide 901..905.

- [ ] **Step 2: Locate insertion anchor**

Run: `grep -n 'data-slide="3"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match (the original slide 3, "Posisi HC di Portal"). Insert concatenated 5-slide block IMMEDIATELY BEFORE.

- [ ] **Step 3: Verify**

Run: `grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 41 (36 + 5).

Run: `for i in 901 902 903 904 905; do grep -c "data-slide=\"$i\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html; done`
Expected: 1 1 1 1 1.

Run: `grep -E '(#ed1c24|#b0121a|--red[^a-z]|--purple|--navy)' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 match.

- [ ] **Step 4: Smoke browser**

Navigate ke slide 901..905. Verify konten Konteks tampil di awal deck (after Selamat Datang).

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): merge 5-slide Konteks cluster from sumber"
```

---

## Task 6: Renumber all `data-slide` to 1..41 + update slide-badge text

Setelah Task 2-5, file punya 41 slide dengan campuran nomor 1..23 + 901..918. Task ini sweep + renumber sequential berdasarkan physical position di file, plus update semua `slide-badge` text dari `SLIDE N / 23` (atau `SLIDE 9XX / 41`) → `SLIDE N / 41`.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Backup file dulu (safety)**

```bash
cp docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html.bak
```

- [ ] **Step 2: Run renumber script (PowerShell one-shot)**

Tulis script `tmp_renumber.ps1`:

```powershell
$path = 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html'
$html = Get-Content $path -Raw

# Find all data-slide attributes in physical file order (preserve order of appearance)
$matches = [regex]::Matches($html, 'data-slide="(\d+)"')
if ($matches.Count -ne 41) {
  Write-Host "ERROR: expected 41 data-slide matches, found $($matches.Count)"
  exit 1
}

# Build replacement map: each occurrence gets sequential N=1..41
# Strategy: walk through matches in order, build new string piece by piece
$result = New-Object System.Text.StringBuilder
$lastEnd = 0
$counter = 1
foreach ($m in $matches) {
  $null = $result.Append($html.Substring($lastEnd, $m.Index - $lastEnd))
  $null = $result.Append("data-slide=`"$counter`"")
  $lastEnd = $m.Index + $m.Length
  $counter++
}
$null = $result.Append($html.Substring($lastEnd))

$newHtml = $result.ToString()

# Now sweep slide-badge text: "SLIDE <anyNum> / <anyNum>" → "SLIDE <N> / 41"
# Track slide-badge in same physical order, replace with sequential
$badgeMatches = [regex]::Matches($newHtml, 'SLIDE\s+\d+\s*/\s*\d+')
$result2 = New-Object System.Text.StringBuilder
$lastEnd2 = 0
$counter2 = 1
foreach ($m in $badgeMatches) {
  $null = $result2.Append($newHtml.Substring($lastEnd2, $m.Index - $lastEnd2))
  # Skip badge counter on cover (slide 1) and penutup (slide 41) — they usually don't have slide-badge.
  # Just trust physical order: assume each badge belongs to its preceding slide.
  $null = $result2.Append("SLIDE $counter2 / 41")
  $lastEnd2 = $m.Index + $m.Length
  $counter2++
}
$null = $result2.Append($newHtml.Substring($lastEnd2))

Set-Content -Path $path -Value $result2.ToString() -NoNewline -Encoding utf8
Write-Host "Renumbered $counter data-slide attrs, $counter2 slide-badge texts."
```

Run: `pwsh -File tmp_renumber.ps1` (Windows PowerShell juga bisa: `powershell -File tmp_renumber.ps1`)
Expected output: `Renumbered 42 data-slide attrs, X slide-badge texts.` (42 = 41 + 1 because $counter++ happens AFTER last). Verify N = 41 actual matches.

> **Catatan slide-badge:** Cover (#1) dan Penutup (#41) tidak punya slide-badge text — skip otomatis karena loop hanya match "SLIDE N / M" pattern. Hitung match real sebelum lanjut. Kalau jumlah badge bukan 39 (= 41 - 2), investigasi manual sebelum Step 3.

- [ ] **Step 3: Verify renumber result**

Run: `grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -u | wc -l`
Expected: 41 (unique values 1..41, no duplicate).

Run: `grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -t'"' -k2 -n | head -5`
Expected: `data-slide="1"` ... `data-slide="5"`.

Run: `grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | sort -t'"' -k2 -n | tail -5`
Expected: `data-slide="37"` ... `data-slide="41"`.

Run: `grep -c 'SLIDE.*/ 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 39 (slide 2..40 punya badge; cover #1 + penutup #41 tidak).

Run: `grep -c 'SLIDE.*/ 23' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 (semua sudah migrate ke / 41).

- [ ] **Step 4: Cleanup temp files**

```bash
rm tmp_renumber.ps1
rm docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html.bak
```

- [ ] **Step 5: Smoke browser quick check**

Buka File 3. Navigate dari slide 1 → arrow Right sampai slide 5. Verify slide-badge text update incremental (SLIDE 2/41, 3/41, 4/41, 5/41). Pastikan urutan logis sesuai final layout (lihat header "Final slide layout").

- [ ] **Step 6: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "chore(sosialisasi-hc): renumber data-slide 1..41 + sync slide-badge / 41"
```

---

## Task 7: Update counter init text + JS const TOTAL

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` line ~1761 (counter) + ~1767 (TOTAL)

> Catatan: line number bisa shifted setelah Task 2-6. Pakai grep untuk locate.

- [ ] **Step 1: Update counter init text**

Run: `grep -n 'id="slideCounter">1 / 23' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match.

Edit via Edit tool:
- old: `<span class="slide-counter" id="slideCounter">1 / 23</span>`
- new: `<span class="slide-counter" id="slideCounter">1 / 41</span>`

- [ ] **Step 2: Update JS const TOTAL**

Run: `grep -n 'const TOTAL = 23' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match.

Edit:
- old: `    const TOTAL = 23;`
- new: `    const TOTAL = 41;`

- [ ] **Step 3: Verify no stale 23 reference**

Run: `grep -nE '\b23\b' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | grep -iE '(slide|total|counter)' | head -10`
Expected: 0 match (kalau ada — investigasi: bisa jadi konten slide bukan navigation).

- [ ] **Step 4: Smoke browser**

Buka File 3. Counter awal harus `1 / 41`. Press End key → harus jump ke slide 41 (Penutup). Press Home → ke slide 1 (Cover). Arrow Right 40x → counter `41 / 41`. Progress bar full 100% di slide 41.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "feat(sosialisasi-hc): nav counter + JS TOTAL update 23 → 41"
```

---

## Task 8: Final smoke test + manual visual review

**Files:** (no edits, verification only — atau ada small fix follow-up)

- [ ] **Step 1: Run all grep integrity checks**

```bash
# Total data-slide count
test "$(grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html)" = "41" && echo "OK count" || echo "FAIL count"

# Monotonic 1..41
grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | grep -oE '[0-9]+' | sort -n | uniq -c | awk '$1!=1 {print "DUP:" $0}'
# Expected: empty output (no duplicates)

# slide-badge / 41 count
test "$(grep -c 'SLIDE.*/ 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html)" = "39" && echo "OK badge" || echo "FAIL badge"

# No File 2 palette leftover in gap slides
grep -E '(#ed1c24|#b0121a|#7b1fa2|--red[^a-z]|--purple|--navy)' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
# Expected: empty

# Callout present
test "$(grep -c 'hc-callout' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html)" -ge "18" && echo "OK callout" || echo "FAIL callout"

# Counter + TOTAL
grep -c '1 / 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html # expect ≥1
grep -c 'TOTAL = 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html # expect 1
```

Expected output: `OK count`, no DUP lines, `OK badge`, no palette match lines, `OK callout`, counter ≥ 1, TOTAL = 1.

- [ ] **Step 2: Browser smoke — light mode**

Buka File 3 di Chrome/Edge. Step through semua 41 slide via Arrow Right (atau click Next). Untuk setiap slide periksa:
- Konten tampil utuh (no overflow, no broken layout)
- Title + badge nomor benar
- Palette teal/amber konsisten
- Untuk slide gap baru (3-7, 13-15, 18-26, 39): callout amber visible di akhir slide-body

Catat slide-slide bermasalah. Kalau ada visual issue (overflow text, broken grid, dll), fix dengan adjust CSS specific atau adjust konten slide source.

- [ ] **Step 3: Browser smoke — dark mode**

Toggle dark mode (☀ button). Repeat step through 41 slide. Periksa:
- Semua text readable di dark background
- Callout amber kontras (bg + border lebih terang dari light mode)
- Tidak ada white-on-white atau dark-on-dark text

- [ ] **Step 4: Print PDF test**

Tekan Ctrl+P. Pilih landscape A4 atau Letter. Save as PDF. Buka PDF:
- 41 page (1 page per slide)
- Callout tidak terpotong di page bottom
- `.no-print` elements (utility-bar, controls) hilang
- Color profile preserved (teal/amber, bukan grayscale)

- [ ] **Step 5: Keyboard nav stress test**

Di browser:
- Press Home → slide 1
- Press End → slide 41
- Press Space 40x → slide 41 (cek counter `41 / 41`, progress bar 100%, Next button disabled)
- Press Arrow Left 40x → slide 1 (Prev button disabled)

- [ ] **Step 6: (Opsional) Playwright automation**

Kalau project sudah punya Playwright setup, tulis spec di `tests/playwright/sosialisasi-internal-tim-hc.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test('navigates all 41 slides', async ({ page }) => {
  await page.goto('file:///' + path.resolve('docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html'));
  await expect(page.locator('#slideCounter')).toHaveText('1 / 41');
  for (let i = 0; i < 40; i++) {
    await page.keyboard.press('ArrowRight');
  }
  await expect(page.locator('#slideCounter')).toHaveText('41 / 41');
});

test('dark mode toggle on all slides', async ({ page }) => {
  await page.goto('file:///' + path.resolve('docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html'));
  await page.click('#darkToggle');
  await expect(page.locator('body')).toHaveClass(/dark/);
  for (let i = 1; i <= 41; i++) {
    await page.evaluate((n) => {
      document.querySelectorAll('.slide').forEach(s => s.classList.remove('active'));
      document.querySelector(`[data-slide="${n}"]`).classList.add('active');
    }, i);
    await page.screenshot({ path: `tests/screenshots/slide-${i}-dark.png` });
  }
});
```

Run: `npx playwright test sosialisasi-internal-tim-hc.spec.ts`
Expected: 2 pass. Review screenshots di `tests/screenshots/`.

- [ ] **Step 7: Tag version + commit (kalau ada fix dari smoke)**

Kalau Step 2-5 nemu issue dan kamu fix-nya di tempat:

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): polish from manual smoke (slide N issue Y)"
```

Tag final:

```bash
git tag -a sosialisasi-internal-tim-hc-v2.0 -m "Merge sosialisasi-aplikasi → internal-tim-HC complete (23+18=41 slide)"
```

- [ ] **Step 8: Update spec status**

Edit `docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md`, ganti:
- old: `**Status:** Brainstorming Q1-Q5 locked, Design Sections 1-5 locked, ready for user review.`
- new: `**Status:** SHIPPED <isi date hari ini, format YYYY-MM-DD>. Tag sosialisasi-internal-tim-hc-v2.0. Implementation per plan 2026-05-22.`

Mark all `Pending Sections` checkboxes as `[x]`.

Commit:

```bash
git add docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md
git commit -m "docs(spec): mark merge sosialisasi-aplikasi → internal-hc shipped v2.0"
```

---

## Done

Final state:
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` = 41 slide, v2.0
- Tag `sosialisasi-internal-tim-hc-v2.0` di local
- Spec marked SHIPPED
- ~8 commits in atomic sequence per task

User next steps:
- Push origin/main (atau biarkan IT promote)
- Notify tim HC: distribusi v2.0
- (Optional) PDF export untuk arsip non-browser
