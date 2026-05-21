# Spec — Merge Sosialisasi-Aplikasi ke Sosialisasi-Internal-Tim-HC

**Status:** WIP brainstorming. Q1-Q5 locked. Design sections pending.
**Date:** 2026-05-21
**Resume command:** `/clear` → bilang "lanjutkan brainstorming merge sosialisasi-aplikasi ke internal-tim-HC, baca spec 2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md"

---

## Konteks File

| # | File | Baris | Role | Audience |
|---|---|---|---|---|
| 1 | `docs/Panduan-Operasional-HC-PortalHC-KPB.html` | 1955 | Reference doc (6 bab + 2 lampiran) | Tim HC — operasional detail |
| 2 | `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` | 2429 | Slide deck (22 slide) — **secondary/source** | Semua pekerja KPB — sosialisasi umum |
| 3 | `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` | 1815 | Slide deck (21 slide) — **target/utama** | Tim HC internal |

User goal: konten konseptual + lifecycle + arsitektur dari **File 2** → masuk **File 3** (slide deck Internal HC). File 1 (Panduan) di luar scope merge ini.

---

## Decisions Locked

### Q1 — Target file
**File 3** (`Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`). File 1 (Panduan) tidak disentuh sesi ini.

### Q2 — Scope konten
**Opsi A**: ambil **semua** slide gap (konten File 2 yang belum ada di File 3). ~17 slide tambahan. Deck final ~38 slide.

**Gap matrix (File 2 → belum ada di File 3):**

| Kategori | Slide File 2 yang masuk gap |
|---|---|
| Konteks/Why | Latar Belakang, Apa Itu HC Portal, 3 Platform Terpadu, Cara Mengakses HC Portal |
| Role/Struktur | Struktur Role Pengguna |
| Assessment Lifecycle | Sistem Assessment, 5 Kategori Assessment Umum, Alur Assessment 7-Step E2E |
| Proton Lifecycle | Assessment Proton, Alur Proton Th 1&2, Alur Proton Th 3 |
| Coaching Architecture | Coaching Proton Dual Track, IDP & Training Records, Hierarki Kompetensi per Track, Progresi Kompetensi per Tahun |
| Coaching Workflow | Alur Coaching Reguler 9-Step, Alur Coaching Mahir 9-Step |
| Tech | Integrasi & Keamanan |

**Overlap yang skip (sudah ada di File 3):** Pre/Post Test (slide 7 File 3), CMP overview (slide 4), CDP Reviewer Chain (slide 8), Coaching Dashboard (slide 9).

### Q3 — Insertion strategy
**Opsi A — Hybrid (cluster konteks di depan + lifecycle distributed per topic).**

**Insertion plan (anchor by File 3 slide):**

1. **Setelah slide #1 "Selamat Datang Tim HC"** → sisip cluster Konteks (5 slide):
   - Latar Belakang (Manual vs Terintegrasi)
   - Apa Itu HC Portal KPB
   - 3 Platform Terpadu (CMP/CDP/BP)
   - Struktur Role Pengguna
   - Cara Mengakses HC Portal

2. **Sebelum slide #7 "Pre/Post Test" (atau dekat slide CMP/Records)** → sisip cluster Assessment Lifecycle (3 slide):
   - Sistem Assessment
   - 5 Kategori Assessment Umum
   - Alur Assessment 7-Step E2E

3. **Sebelum slide #9 "Coaching Proton Dashboard"** → sisip cluster Proton Lifecycle + Coaching Architecture + Workflow (8 slide):
   - Assessment Proton
   - Alur Proton Th 1&2
   - Alur Proton Th 3
   - Coaching Proton Dual Track
   - IDP & Training Records
   - Hierarki Kompetensi per Track
   - Progresi Kompetensi per Tahun
   - Alur Coaching Reguler 9-Step
   - Alur Coaching Mahir 9-Step

4. **Sebelum slide #21 "Reference Card"** → sisip cluster Tech (1 slide):
   - Integrasi & Keamanan

Final ordering: 1 (existing) → 5 baru → 2,3,4,5,6 (existing) → 3 baru → 7,8 (existing) → 9 baru → 9..20 (existing) → 1 baru → 21 (existing).

### Q4 — Adaptasi tone konten
**Opsi B — Verbatim + sisip "Implikasi untuk HC" callout.**

Tiap slide gap = copy konten persis dari File 2, tambah callout box di slide:
- Style: box kuning kecil di area bawah/sidebar
- Konten: 1-2 bullet point "Tanggung jawab HC pada step/topic ini: [verb operasional + konteks real]"
- Contoh: Slide "Alur Assessment 7-Step E2E" → callout "HC: setup jadwal (step 1), monitor real-time + force-close (step 5), entry manual + sertifikat (step 7)"

Visual styling File 2 dipreserve (color palette, accent, slide-title pattern).

---

### Q5 — Output Strategy
**Opsi A — In-place update** `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`, bump v1.1 → v2.0. Single source of truth, history via git.

---

## Pending Sections (post Q5)

- [ ] Design section: file structure, slide template
- [ ] Design section: callout HTML pattern (kelas CSS baru)
- [ ] Design section: visual consistency (re-use accent color palette File 3? atau preserve File 2?)
- [ ] Design section: navigation update (counter slide total 21 → 38)
- [ ] Design section: testing checklist (Playwright untuk dark mode, print PDF)
- [ ] User approval per section
- [ ] Spec self-review (placeholder/contradiction/ambiguity scan)
- [ ] User review spec
- [ ] Invoke writing-plans skill

---

## Catatan Konten Callout (draft, untuk dihaluskan post Q5)

Draft 1-line tanggung jawab HC per slide gap:

| Slide gap | Draft callout HC |
|---|---|
| Latar Belakang | HC: jembatan antara user pain "manual" → adopsi platform terintegrasi |
| Apa Itu HC Portal | HC: owner data + reviewer final di semua workflow Proton |
| 3 Platform Terpadu | HC operate di semua 3 (CMP analytics, CDP coaching, BP via integrasi) |
| Struktur Role | HC = L2 authority; scope: cross-section, final reviewer |
| Cara Mengakses | HC akses sama dengan user umum, tapi role-gated menu (Bab 1.2 Panduan) |
| Sistem Assessment | HC: setup jadwal, bank soal, monitor real-time, force-close |
| 5 Kategori Assessment | HC: assign kategori per jadwal, lihat fail rate per kategori (Analytics) |
| Alur Assessment 7-Step | HC: step 1 (setup), step 5 (monitor), step 7 (manual entry + cert) |
| Assessment Proton | HC: reviewer final chain Proton, manage silabus + guidance files |
| Alur Proton Th 1&2 | HC: jaga deliverable submission timeline, eskalasi bottleneck |
| Alur Proton Th 3 | HC: validasi mahir, certification renewal management |
| Coaching Dual Track | HC: monitor kedua track via Coaching Proton Dashboard |
| IDP & Training Records | HC: review IDP coachee, audit training records team |
| Hierarki Kompetensi | HC: gunakan KKJ matrix untuk gap analysis CPDP Mapping |
| Progresi Kompetensi per Tahun | HC: track progresi via Analytics + Bottleneck Report |
| Alur Coaching Reguler 9-Step | HC: reviewer final di step 8-9 (approval chain) |
| Alur Coaching Mahir 9-Step | HC: validation mahir + sertifikasi (Renewal Certificate Mgmt) |
| Integrasi & Keamanan | HC: tanggung jawab audit log review, impersonate dengan justifikasi |

---

## File yang Disentuh

- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` — update (pending Q5)
- `docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md` — this spec

## File Source (read-only reference)

- `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` — sumber konten gap (slide File 2)
