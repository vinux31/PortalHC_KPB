# Sosialisasi-Internal-Tim-HC v2 — Slide Compression Design

**Date:** 2026-05-23
**Source file:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (40 slide, 3955 baris)
**Target file (new):** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (root, untracked → committed as v2.0)
**Trigger:** User request — kompres 40 → 30 slide untuk session presentasi live 60 menit (~2 menit/slide).

---

## Goal

Hasilkan file v2 30-slide yang:
1. Memuat semua konten high-signal v1 tanpa kehilangan substansi
2. Memprioritaskan cluster **Assessment** (slide v1 14-18) dan **Admin Panel** (slide v1 31-37) — preserve full
3. Trim agresif cluster PROTON Coaching (v1 22-30) via merge + drop redundansi
4. Tetap match data faktual sumber kode (tidak regress klaim numerik yang sudah diperbaiki di v1.x)

User-confirmed parameters:
- **Driver:** durasi presentasi live (A)
- **Budget:** 60 menit → 30 slide (B)
- **Priority preserve:** Assessment + Menu Kelola Data (Admin Panel)
- **File strategy:** New file v2 hidup berdampingan dengan v1 (B)

---

## Source Map (v1 — 40 slide)

Per inspeksi file (data-slide + section-eyebrow):

| BAGIAN | Slide v1 | Count |
|---|---|---|
| (cover) | 1 | 1 |
| 0 PENGENALAN | 2-10 | 9 |
| 1 CMP | 11-18 | 8 |
| 2 ASSESSMENT PROTON | 19-21 | 3 |
| 3 PROTON LIFECYCLE & COACHING | 22-30 | 9 |
| 4 ADMIN PANEL | 31-37 | 7 |
| 5 CLOSING | 38-40 | 3 |
| **Total** | | **40** |

---

## Target Map (v2 — 30 slide)

| # v2 | BAGIAN | Title | Origin v1 | Action |
|---|---|---|---|---|
| 1 | (cover) | Cover — Portal HC KPB untuk Tim HC | 1 | keep |
| 2 | 0 | Selamat Datang Tim HC | 2 | keep |
| 3 | 0 | Apa Itu HC Portal + 3 Platform | **4+5 merged** | merge; **drop slide 3 Latar Belakang** |
| 4 | 0 | Struktur Role (10 role / 6 level) | 6 | keep |
| 5 | 0 | Cara Mengakses HC Portal | 7 | keep |
| 6 | 0 | Area Kerja HC di Portal | 8 | keep |
| 7 | 0 | Alur Harian + Notifikasi | **9+10 merged** | merge — left: alur, right: notif |
| 8 | 1 | CMP Overview | 11 | keep |
| 9 | 1 | Records Team + Analytics Dashboard | **12+13 merged** | merge — split panel |
| 10 | 1 | Sistem Assessment | 14 | **preserve (priority)** |
| 11 | 1 | 5 Kategori Assessment Umum | 15 | **preserve (priority)** |
| 12 | 1 | Alur Assessment 7-Step End-to-End | 16 | **preserve (priority)** |
| 13 | 1 | Pre/Post Test — Gain Score | 17 | **preserve (priority)** |
| 14 | 1 | IDP Library | 18 | **preserve (priority)** |
| 15 | 2 | Assessment PROTON (intro 3 tahun) | 19 | keep |
| 16 | 2 | Alur PROTON — Th 1-2 + Th 3 | **20+21 merged** | merge — komparasi 2 format side-by-side |
| 17 | 3 | Progresi Kompetensi per Tahun | 22 | keep (visual chart unik) |
| 18 | 3 | Coaching PROTON — Chain + Dual Track | **23+24 merged** | merge — chain di atas, dual track di bawah |
| 19 | 3 | Alur Coaching — Reguler vs Mahir 9-Step | **26+27 merged** | merge — komparasi 9-step; **drop slide 25 Hierarki Kompetensi** |
| 20 | 3 | Coaching Dashboard + Histori PROTON | **28+29 merged** | merge — dashboard top, histori+export bottom |
| 21 | 3 | Renewal Certificate Lifecycle | 30 | keep |
| 22 | 4 | Admin Panel Landing (14 menu) | 31 | **preserve (priority)** |
| 23 | 4 | Manajemen Pekerja | 32 | **preserve (priority)** |
| 24 | 4 | Coach-Coachee Mapping | 33 | **preserve (priority)** |
| 25 | 4 | Silabus + Guidance Files | 34 | **preserve (priority)** |
| 26 | 4 | Override KKJ + Mapping Silabus | 35 | **preserve (priority)** |
| 27 | 4 | Assessment Monitoring | 36 | **preserve (priority)** |
| 28 | 4 | Maintenance + Audit Log | 37 | **preserve (priority)** |
| 29 | 5 | Tugas HC Cepat + Reference Card | **38+39 merged** | merge — checklist + dokumen rujukan |
| 30 | 5 | Penutup — Terima Kasih | 40 | keep |

**Reduction summary:**
- Drop entirely: slide v1 **3** (Latar Belakang), **25** (Hierarki Kompetensi) → −2
- Merge: **4+5**, **9+10**, **12+13**, **20+21**, **23+24**, **26+27**, **28+29**, **38+39** → 16 slide kompres jadi 8 = −8
- Total cut: −10 → **40 → 30 ✓**

**Distribusi per BAGIAN v2:**

| BAGIAN | v1 | v2 | Δ |
|---|---|---|---|
| cover | 1 | 1 | 0 |
| 0 Pengenalan | 9 | 6 | −3 |
| 1 CMP | 8 | 7 | −1 |
| 2 Assessment PROTON | 3 | 2 | −1 |
| 3 PROTON Lifecycle & Coaching | 9 | 5 | −4 |
| 4 Admin Panel | 7 | 7 | 0 (preserve) |
| 5 Closing | 3 | 2 | −1 |
| **Total** | **40** | **30** | **−10** |

---

## Merge Layout Specs

Setiap merge slide butuh layout split. CSS reuse pattern existing (`.split-grid`, `.two-col`, dst — confirm di file v1 saat implementasi).

### Slide v2 #3 — Apa Itu HC Portal + 3 Platform (ex 4+5)
- **Top half:** "Apa Itu HC Portal" — paragraf 2-3 baris definisi sistem
- **Bottom half:** 3 card platform (CMP · CDP · BP) horizontal grid
- **Drop:** Slide v1 #3 Latar Belakang prose (audience tahu kontext via undangan/agenda)

### Slide v2 #7 — Alur Harian + Notifikasi (ex 9+10)
- **Left col:** 5 aktivitas inti harian (bullet list)
- **Right col:** Bell icon mockup + 3 contoh notif sample
- Hapus duplikasi opening text di slide v1 #10

### Slide v2 #9 — Records Team + Analytics Dashboard (ex 12+13)
- **Left col:** Records Team — filter cascade + table snippet
- **Right col:** Analytics Dashboard — 4 metric tile + chart thumbnail
- Subtitle gabungan: "Riwayat tim real-time + dasbor metric assessment"

### Slide v2 #16 — Alur PROTON Th 1-2 + Th 3 (ex 20+21)
- **Top row:** Alur Th 1-2 (Identik Alur Umum, 4 aspek beda) — flowchart horizontal
- **Bottom row:** Alur Th 3 (Interview Offline Tatap Muka, Panel Juri) — flowchart horizontal
- Subtitle: "Th 1-2: format online · Th 3: panel offline"

### Slide v2 #18 — Coaching Chain + Dual Track (ex 23+24)
- **Top half:** Reviewer Chain — Coach → SrSpv → SH → **HC (Final Reviewer)** — horizontal flow
- **Bottom half:** Dual Track — Panelman track + Operator track parallel, 2 column
- Subtitle: "HC = reviewer ke-3 · 2 track independen per coachee"

### Slide v2 #19 — Alur Coaching Reguler vs Mahir 9-Step (ex 26+27)
- **2-col tab/komparasi:**
  - Left col: Reguler 9-step (Persiapan Silabus → Review Multi-Role → Assessment Online → Sertifikasi)
  - Right col: Mahir 9-step (Silabus Mahir → Review Multi-Role → Interview Offline → Sertifikasi Final)
- Highlight perbedaan di step yang divergent (likely step Assessment: Online vs Offline)
- **Drop:** Slide v1 #25 Hierarki Kompetensi (info tersedia di Panduan Operasional, niche untuk presentasi live)

### Slide v2 #20 — Coaching Dashboard + Histori PROTON (ex 28+29)
- **Top half:** Dashboard mockup — 5 metric global + filter cascade
- **Bottom half:** Histori per pekerja — progress visual Th 1/2/3 + export Excel CTA

### Slide v2 #29 — Tugas HC Cepat + Reference Card (ex 38+39)
- **Left col:** Checklist harian/mingguan/bulanan (3 short list)
- **Right col:** Reference Card — dokumen rujukan + URL Cheatsheet + link Panduan

---

## Cross-Reference Updates

Cross-ref dalam isi slide yang menyebut nomor slide lain perlu update setelah renumber:

| Lokasi v1 | Reference lama | Target v2 baru |
|---|---|---|
| Line 2748 (slide v1 #19) | "di **slide Progresi Kompetensi** (awal BAGIAN 3)" | "di **slide #17 Progresi Kompetensi**" (atau biarkan tanpa nomor, sebut BAGIAN 3 saja) |

**Strategi:** Cari semua occurrence "slide " (lowercase, dalam konten) di file. Update ke nomor v2 baru ATAU drop nomor spesifik, ganti dengan reference BAGIAN/title saja (lebih tahan rot).

---

## Implementation Strategy

1. **Copy baseline:** `cp Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
2. **Update JS constant:** `TOTAL = 30` (cari di footer script v1, line ~3907)
3. **Update badge template:** "SLIDE X/30"
4. **Iterative edit per BAGIAN** (per commit, bottom-up biar nomor di atas tidak terganggu saat edit nomor di bawah):
   - Phase 1: Closing (cluster 5) — merge 38+39 → slide v2 #29
   - Phase 2: Admin Panel (cluster 4) — no merge, hanya renumber data-slide 22-28
   - Phase 3: PROTON Coaching (cluster 3) — drop 25, merge 23+24, 26+27, 28+29
   - Phase 4: PROTON Assessment (cluster 2) — merge 20+21
   - Phase 5: CMP (cluster 1) — merge 12+13
   - Phase 6: Pengenalan (cluster 0) — drop 3, merge 4+5, merge 9+10
   - Phase 7: Renumber semua data-slide attribute 1-30 + badge SLIDE X/30
   - Phase 8: Cross-ref fix (line 2748 dan sejenis)
   - Phase 9: Browser smoke-test via Playwright MCP — navigation 1→30, counter check, BAGIAN label monotonic

5. **No CSS file changes** — semua class reuse dari v1. Tambah inline class `.merge-split-v` atau `.merge-split-h` di slide merged kalau perlu split visual.

---

## Out of Scope

- **Tidak edit file v1** (`docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`) — tetap valid sebagai 40-slide deep-dive version
- **Tidak edit** `Sosialisasi-Aplikasi-PortalHC-KPB.html`, `Sosialisasi-PROTON-Operasional.html`, `Panduan-Operasional-HC-PortalHC-KPB.html`
- **Tidak ubah konten teknis/data faktual** — semua angka/klaim (14 menu Admin Panel, 5 kategori assessment, 10 role/6 level, dll) sudah diverifikasi di review v1.x, dipertahankan apa adanya
- **Tidak audit ulang vs source code** — diasumsikan v1 sudah pass review (per memory `project_panduan_sosialisasi_hc_merged`)
- **Tidak buat PDF companion** (out of scope, terpisah dari HTML)

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Merge slide 26+27 (alur 9-step reguler+mahir) overflow vertikal | Condensed step labels (numbered dot + 1-line desc), font slightly smaller di merged slide; fallback: simpan sebagai 2-tab dalam 1 slide pakai JS toggle |
| Drop Hierarki Kompetensi (slide 25) — feedback "kok hilang?" dari stakeholder | Mention eksplisit di Reference Card (slide v2 #29): "Hierarki kompetensi lengkap: lihat Panduan Operasional Bab 3.x" |
| Cross-ref slide number rot — text "lihat slide 17" jadi salah saat renumber lagi di masa depan | Strategi 2: drop nomor spesifik, ganti reference judul/BAGIAN. Lebih maintainable. |
| Konten merge 12+13 (Records + Analytics) terlalu padat di 1 slide | Split panel 50/50 dengan reduced detail per panel; full detail tetap di v1 deep-dive |
| User mau preview sebelum massive commit | Per-phase commit atomic; user bisa checkout v2 file setiap phase untuk verify visual |
| Counter JS broken setelah renumber | Smoke-test Playwright Phase 9 — verify counter "1/30" → "30/30" sequential |
| Root copy v1 (untracked) confusing — ada 2 file root dan docs/ | Tidak sentuh root copy v1; v2 baru di root saja. Setelah selesai, decide apakah delete root v1 atau biarkan. |

---

## Success Criteria

1. File `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` punya **30 slide** (data-slide 1-30 sequential).
2. Counter "1 / 30" ... "30 / 30" benar di browser; nav button work.
3. BAGIAN label monotonic: cover → 0 → 1 → 2 → 3 → 4 → 5, no regression.
4. Semua slide priority preserve full:
   - **Assessment** (v2 #10-14, ex v1 #14-18): 5/5 intact
   - **Admin Panel** (v2 #22-28, ex v1 #31-37): 7/7 intact
5. Merged slide layout readable (no overflow, font ≥ 14pt, kontras OK).
6. Cross-ref text dalam slide tidak ada nomor slide v1 yang invalid setelah renumber.
7. Tidak ada error di console browser (Playwright smoke-test).
8. Estimasi waktu presentasi 60 menit @ 2 menit/slide masuk akal (subjective check via dry-run).

---

## Tag / Commit Plan (Preview)

Tag final: `sosialisasi-internal-hc-v2.0`

Commit sequence (atomic per phase, conventional commits):
1. `feat(sosialisasi-internal-hc-v2): baseline copy from v1`
2. `feat(sosialisasi-internal-hc-v2): merge closing (Tugas+Reference)`
3. `refactor(sosialisasi-internal-hc-v2): admin panel preserve, renumber`
4. `feat(sosialisasi-internal-hc-v2): proton coaching cluster compress (drop hierarki, merge 3 pairs)`
5. `feat(sosialisasi-internal-hc-v2): proton assessment alur merge Th1-2+Th3`
6. `feat(sosialisasi-internal-hc-v2): cmp records+analytics merge`
7. `feat(sosialisasi-internal-hc-v2): pengenalan compress (drop latar, merge 2 pairs)`
8. `refactor(sosialisasi-internal-hc-v2): renumber data-slide 1-30 + TOTAL=30`
9. `fix(sosialisasi-internal-hc-v2): cross-ref slide number updates`
10. `test(sosialisasi-internal-hc-v2): playwright smoke-test passed`
