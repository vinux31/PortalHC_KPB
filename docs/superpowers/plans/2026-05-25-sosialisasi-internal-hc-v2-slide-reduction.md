# Sosialisasi Internal HC v2 — Slide Reduction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hapus 6 slide dari `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (44 → 38 slide), update JS navigation, badges, dan agenda Slide 2.

**Architecture:** Pure delete — tidak ada konten yang dipindahkan. Hapus blok HTML per slide (dengan komentar penanda), lalu renumber `data-slide` + `SLIDE X / 44` badge + `const TOTAL` via Python script. Update teks agenda Slide 2 yang referensi slide yang dihapus.

**Tech Stack:** HTML editing, Python 3 (utility script renumbering), Git.

---

## Peta Perubahan

**File:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

HTML `data-slide` asli → tindakan:
- data-slide 1–8: tetap (cover + 7 konten)
- data-slide 9 (`Alur Harian + Notifikasi`): **DELETE**
- data-slide 10 (`Tipe Notifikasi`): **DELETE**
- data-slide 11–18: tetap (geser jadi 9–16)
- data-slide 19 (`Alur PROTON`): **DELETE**
- data-slide 20 (`Progresi Kompetensi`): **DELETE**
- data-slide 21 (`Coaching Chain + Dual Track`): **DELETE**
- data-slide 22 (`Alur Coaching Reguler vs Mahir`): **DELETE**
- data-slide 23–44: tetap (geser jadi 17–38)

Setelah renumber: 38 slide total.

---

## Task 1: Hapus 6 Blok Slide dari HTML

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

Setiap blok slide dimulai dengan komentar `<!-- === ... SLIDE N ... ===` dan diakhiri `</div>` sebelum komentar slide berikutnya.

- [ ] **Step 1: Buka file dan hapus blok Slide 9 (Alur Harian + Notifikasi)**

  Cari baris (sekitar line 2231):
  ```
  <!-- ================= SLIDE 9: ALUR HARIAN + NOTIFIKASI ================= -->
  ```
  Hapus dari baris komentar tersebut sampai sebelum:
  ```
  <!-- ================= SLIDE 10: TIPE NOTIFIKASI ================= -->
  ```
  (termasuk blank line pemisah)

- [ ] **Step 2: Hapus blok Slide 10 (Tipe Notifikasi)**

  Cari baris (sekitar line 2298, bergeser setelah Step 1):
  ```
  <!-- ================= SLIDE 10: TIPE NOTIFIKASI ================= -->
  ```
  Hapus sampai sebelum:
  ```
  <!-- ================= SLIDE 11: CMP OVERVIEW ================= -->
  ```

- [ ] **Step 3: Hapus blok Slide 19 (Alur PROTON Th 1-2 + Th 3)**

  Cari baris:
  ```
  <!-- ================= SLIDE 19: ALUR PROTON Th 1-2 + Th 3 ================= -->
  ```
  Hapus sampai sebelum:
  ```
  <!-- ================= SLIDE 20: PROGRESI KOMPETENSI PER TAHUN ================= -->
  ```

- [ ] **Step 4: Hapus blok Slide 20 (Progresi Kompetensi per Tahun)**

  Cari:
  ```
  <!-- ================= SLIDE 20: PROGRESI KOMPETENSI PER TAHUN ================= -->
  ```
  Hapus sampai sebelum:
  ```
  <!-- ================= SLIDE 21: COACHING CHAIN + DUAL TRACK ================= -->
  ```

- [ ] **Step 5: Hapus blok Slide 21 (Coaching PROTON Chain + Dual Track)**

  Cari:
  ```
  <!-- ================= SLIDE 21: COACHING CHAIN + DUAL TRACK ================= -->
  ```
  Hapus sampai sebelum komentar Slide 22.

- [ ] **Step 6: Hapus blok Slide 22 (Alur Coaching Reguler vs Mahir)**

  Cari:
  ```
  <!-- ================= SLIDE 22: ALUR COACHING ================= -->
  ```
  (judul komentar mungkin sedikit bervariasi — cari `data-slide="22"` sebagai anchor)
  Hapus sampai sebelum komentar Slide 23 (Coaching Dashboard + Histori).

- [ ] **Step 7: Verifikasi — hitung slide tersisa**

  ```bash
  grep -c 'data-slide="' "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  ```
  Expected: `38`

- [ ] **Step 8: Commit**

  ```bash
  git add "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  git commit -m "feat(sosialisasi-hc-v2): delete 6 slides (notifikasi + PROTON detail)"
  ```

---

## Task 2: Renumber data-slide + Badges via Python Script

**Files:**
- Create (sementara): `scripts/renumber_slides.py`
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Buat script renumber**

  Buat file `scripts/renumber_slides.py`:

  ```python
  import re, sys

  path = "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  with open(path, encoding="utf-8") as f:
      html = f.read()

  # Kumpulkan semua data-slide values yang ada setelah delete Task 1
  # Expected: 1..8, 11..18, 23..44  (38 total, dengan gaps di 9-10 dan 19-22)
  existing = sorted(set(int(m) for m in re.findall(r'data-slide="(\d+)"', html)))
  print("Existing data-slide values:", existing)
  assert len(existing) == 38, f"Expected 38, got {len(existing)}"

  # Buat mapping lama → baru
  mapping = {old: new for new, old in enumerate(existing, start=1)}
  print("Mapping:", mapping)

  # Renumber data-slide attributes (descending untuk hindari double-replace)
  for old in sorted(mapping.keys(), reverse=True):
      new = mapping[old]
      html = html.replace(f'data-slide="{old}"', f'data-slide="NEWSLIDE{new}"')
  html = html.replace('data-slide="NEWSLIDE', 'data-slide="')

  # Update badge text: "SLIDE X / 44" → "SLIDE Y / 38"
  # Mapping: badge X = data-slide value sebelum rename.
  # Setelah rename, badge di slide baru Y harus menampilkan "SLIDE Y / 38"
  # Tapi badge text di HTML masih pakai angka lama.
  # Ganti semua "SLIDE \d+ / 44" → placeholder dulu, lalu assign per slide.
  # Cara: ganti setiap "SLIDE N / 44" dimana N ada di mapping lama → "SLIDE mapping[N] / 38"
  for old_n in sorted(mapping.keys(), reverse=True):
      new_n = mapping[old_n]
      html = html.replace(f'SLIDE {old_n} / 44', f'SLIDE {new_n} / 38')

  with open(path, "w", encoding="utf-8") as f:
      f.write(html)

  print("Done. Verify with: grep -o 'SLIDE [0-9]* / [0-9]*' file | sort -t/ -k1,1 -V | head -5")
  ```

- [ ] **Step 2: Jalankan script**

  ```bash
  python scripts/renumber_slides.py
  ```

  Expected output:
  ```
  Existing data-slide values: [1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16, 17, 18, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44]
  Mapping: {1: 1, 2: 2, ..., 44: 38}
  Done.
  ```

  Kalau assert gagal (bukan 38), kembali ke Task 1 dan pastikan 6 blok sudah dihapus benar.

- [ ] **Step 3: Verifikasi hasil renumber**

  ```bash
  grep -o 'data-slide="[0-9]*"' "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | sort -t'"' -k2 -n | head -5
  grep -o 'data-slide="[0-9]*"' "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | sort -t'"' -k2 -n | tail -5
  ```

  Expected head: `data-slide="1"` sampai `data-slide="5"`
  Expected tail: `data-slide="34"` sampai `data-slide="38"`

- [ ] **Step 4: Verifikasi tidak ada badge lama tersisa**

  ```bash
  grep -c '/ 44' "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  ```

  Expected: `0`

- [ ] **Step 5: Hapus script sementara**

  ```bash
  rm scripts/renumber_slides.py
  ```

- [ ] **Step 6: Commit**

  ```bash
  git add "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  git commit -m "feat(sosialisasi-hc-v2): renumber data-slide + badges (38 slides total)"
  ```

---

## Task 3: Update JS TOTAL + Counter + Progress Bar

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Update konstanta TOTAL**

  Cari (sekitar line 4355 asli, bergeser setelah penghapusan):
  ```javascript
  const TOTAL = 44;
  ```
  Ganti dengan:
  ```javascript
  const TOTAL = 38;
  ```

- [ ] **Step 2: Update initial counter text**

  Cari:
  ```html
  <span class="slide-counter" id="slideCounter">1 / 44</span>
  ```
  Ganti dengan:
  ```html
  <span class="slide-counter" id="slideCounter">1 / 38</span>
  ```

- [ ] **Step 3: Update initial progress bar width**

  Cari:
  ```html
  <div class="progress-bar" id="progressBar" style="width: 4.3%;"></div>
  ```
  Ganti dengan:
  ```html
  <div class="progress-bar" id="progressBar" style="width: 2.6%;"></div>
  ```
  (2.6% = 1/38 × 100)

- [ ] **Step 4: Verifikasi tidak ada angka "44" yang tersisa di JS/counter**

  ```bash
  grep -n '44' "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html" | grep -v '<!--\|//\|#\|font\|color\|px\|rem\|em\|rgb\|754'
  ```

  Review output — pastikan tidak ada `TOTAL = 44` atau `/ 44` yang tertinggal.

- [ ] **Step 5: Commit**

  ```bash
  git add "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  git commit -m "fix(sosialisasi-hc-v2): update JS TOTAL=38, counter, progress bar"
  ```

---

## Task 4: Update Agenda Slide 2 (Selamat Datang Tim HC)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

Slide 2 (data-slide="2") punya 5 agenda item. Item 01, 03, 04 referensi konten dari slide yang dihapus.

- [ ] **Step 1: Update agenda item 01 — hapus referensi notifikasi**

  Cari dalam blok agenda item 01:
  ```html
  <p>Authority, scope, alur kerja harian, bell icon</p>
  ```
  Ganti dengan:
  ```html
  <p>Authority, scope, area kerja HC di Portal</p>
  ```

- [ ] **Step 2: Update agenda item 03 — sederhanakan deskripsi PROTON**

  Cari dalam blok agenda item 03:
  ```html
  <p>Format per tahun, alur Tahun 1-2 online, Tahun 3 offline</p>
  ```
  Ganti dengan:
  ```html
  <p>Overview Assessment Proton, dashboard &amp; histori coaching</p>
  ```

- [ ] **Step 3: Update agenda item 04 — hapus referensi slide dihapus**

  Cari dalam blok agenda item 04:
  ```html
  <p>Reviewer Chain, Dual Track, Alur 9 step, Dashboard, Histori, Renewal</p>
  ```
  Ganti dengan:
  ```html
  <p>Dashboard, Histori, Renewal Certificate Lifecycle</p>
  ```

- [ ] **Step 4: Verifikasi visual di browser**

  Buka `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` di browser.
  Cek:
  - Slide 2: agenda item 01, 03, 04 sudah tidak referensi konten yang dihapus
  - Navigasi: Slide 1 → 2 → ... → 8 → 9 (CMP Overview, bukan Alur Harian) ✓
  - Slide 15 (Assessment Proton) → Slide 16 (Coaching Dashboard) ✓ (bukan Alur PROTON)
  - Slide terakhir = 38, tombol Next disabled di slide 38 ✓
  - Progress bar bergerak dari 2.6% ke 100% ✓

- [ ] **Step 5: Commit + tag**

  ```bash
  git add "docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
  git commit -m "feat(sosialisasi-hc-v2.3): update agenda Sl2, remove deleted slide references"
  git tag sosialisasi-internal-hc-v2.3
  ```

---

## Ringkasan Hasil

| Item | Sebelum | Sesudah |
|------|---------|---------|
| Total slide | 44 | 38 |
| JS TOTAL | 44 | 38 |
| Slide notifikasi | Sl9 + Sl10 ada | Dihapus |
| PROTON detail | Sl19–22 ada | Dihapus |
| PROTON anchor | Sl18 → Sl16 | Assessment overview + Dashboard tetap |
| Tag git | sosialisasi-internal-hc-v2.2 | sosialisasi-internal-hc-v2.3 |
