# Design Spec — Slide 8 Risalah Web (Opsi II + IV)

> **Tanggal:** 2026-05-24
> **Konteks:** PCP SMART 2026, Risalah Web.pptx slide 8 "3. Solusi Terpilih (PC/FT/I Prove)"
> **Output:** 2 HTML mockup full-polish v3.7 + PNG export untuk redraw PowerPoint
> **Folder target:** `docs/pcp-HCPortal-2026/slide8-risalah/`

## 1. Latar Belakang

Slide 8 Risalah Web saat ini menggunakan layout adaptasi Versi P (Workflow Topology Purdue-style + DMZ-analog Buffer Zone) dari `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html`. Audit ulang terhadap referensi akademis di folder `referensi slide 8 solusi terpilih/` menemukan gap:

- Tidak ada **Flow Proses** eksplisit
- Tidak ada **Formula** kuantitatif
- Zero **theoretical grounding** meski 4 jurnal + 3 laporan korelasi sudah disusun
- Metafora "Buffer Zone DMZ" salah domain (cyber security IT/OT vs HR competency)
- Issue markers A-F generik, bukan dari FMEA RPN riil

Spec ini menghasilkan **2 versi** untuk slide 8 Risalah Web yang menambal gap di atas, mengangkat referensi jurnal ke dalam visual, dan comply dengan 7 slot wajib PCP template page 8.

## 2. Tujuan & Out-of-Scope

### In-Scope
- 2 file HTML mockup di folder baru `docs/pcp-HCPortal-2026/slide8-risalah/`
- Full polish v3.7 style (header bar, meta bar, toolbar, A3 landscape print CSS)
- PNG export tiap mockup untuk paste ke PowerPoint manual
- README master + index.html viewer di folder baru
- 7/7 slot PCP page 8 compliance

### Out-of-Scope
- Tidak otomasi redraw PowerPoint (user manual paste PNG)
- Tidak ubah Versi P existing di folder `3.4-solusi-terpilih/` (Versi P tetap utuh sebagai tag `pcp-hcportal-3.4-v3.7`)
- Tidak Opsi I (Hybrid 3-Panel) atau Opsi III (Dynamic Capabilities) — di-defer kalau dibutuhkan
- Tidak tambah PDF lampiran referensi (PDF tetap di folder `referensi slide 8 solusi terpilih/`)

## 3. Arsitektur Output

```
docs/pcp-HCPortal-2026/slide8-risalah/
├─ README.md                              (index + 2 versi rationale + reference mapping)
├─ index.html                             (master viewer hero + 2 version card)
├─ pipeline-outcome.html                  (Opsi II — primary alternative)
├─ pipeline-outcome.png                   (PNG export untuk PPT paste)
├─ workflow-topology-refined.html         (Opsi IV — refined Versi P)
└─ workflow-topology-refined.png          (PNG export)
```

## 4. Reference Master List (Inventory)

| Kode | Sumber | Pakai untuk | Dipakai di |
|------|--------|-------------|------------|
| **R1** | Ellström & Kock (2008) — *Competence Development in the Workplace* (Asia Pacific Education Review 9(1)) | Integrated Strategy framing | IV footer |
| **R2** | Staškeviča (2019) — *Importance of Competency Model Development* (Acta Oeconomica Pragensia 27(2)) | Optional (not used in either opsi) | — |
| **R3** | Ruggiero et al (2026) — *Unveiling Competencies in Smart Working* (IJPSM, DOI 10.1108/IJPSM-10-2025-0455) | Optional (deferred to Opsi III) | — |
| **R4** | **Ogoun & Tamunosiki-Amadi (2023)** — *Competence Monitoring and Employee Responsiveness* (IJSMES 2(5)) | Pipeline 3-stage + R-coefficient Formula | **II PRIMARY, IV callout** |
| **RL1** | Korelasi Jurnal & PPT KPB (docx) | Bridging narrative R1↔R4 ke project | Both footer |
| **RL2** | Laporan Korelasi Kuantitatif V2 (docx) | Bridging R4 → Panca Mutu mapping | Both formula |
| **RL3** | Laporan Korelasi Strategis Baku (docx) | Bridging R3 → bias organisasional | (not used) |
| **P1** | Risalah Inovasi PROTON (Fishbone+FMEA) | Issue codes A-F + RPN values | Both |
| **P2** | Risalah Panca Mutu (Q/C/D/HSSE/Moral) | Outcome formula mapping | Both |
| **SE1** | ISO/IEC 27001:2022 | Standar External — ISMS | Both |
| **SE2** | OWASP Top 10 2021 + ASVS 4.0.3 | Standar External — Web Security | Both |
| **SE3** | WCAG 2.2 (W3C, 2023) | Standar External — Accessibility | Both |
| **SI1** | Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 | Standar Internal Pertamina | Both |
| **SI2** | TKO B5.3-04/K20100/2025-S9 | Standar Internal Pertamina | Both |
| **SI3** | Kamus Direktori Kompetensi Teknis Pertamina | Standar Internal — IDP alignment | Both |

## 5. Opsi II — `pipeline-outcome.html`

### 5.1 Layout (top-to-bottom)

1. **Header bar** (v3.7 style)
   - Logo Pertamina kiri, judul tengah ("§3.4 Solusi Terpilih — HC Portal · Pemantauan Kompetensi Pipeline"), badge PCP SMART 2026 kanan
   - border-top 5px solid green (`#00A551`)

2. **Meta bar** (1 row, 2 cols)
   - Kiri: 🎯 Audience: Reviewer PCP, HC management, OPEX
   - Kanan: 📌 Tujuan: Pemantauan kompetensi 3-stage → Panca Mutu

3. **Toolbar** (1 row)
   - Note left + Print button right

4. **SEBELUM panel — FULL 5-LAYER** (decision #3-C: vertical stack)
   - Border-top red, header "❌ Sebelum (Kondisi Aktual)"
   - 5 layer rows: Lv5 Manajemen → Lv4 HC → Lv3 Atasan → Lv2 Coach → Lv1 Pekerja (identik dengan Versi P existing)
   - Markers A-F (issue codes dari FMEA)
   - Legend table issue
   - Tagline: "Workflow manual — RPN Method=140 / Machine=140 / Man=90 (Risalah PROTON FMEA)"

5. **Transformation arrow** ▼

6. **3-ZONE MAIN LAYOUT** (grid 200px | 1fr | 200px)

   **Zone kiri — 5 Aktor Hierarchy:**
   - Stack vertikal 5 box (Lv5-Lv1) dengan ikon aktor
   - Compact display, no portal feature list

   **Zone tengah — PIPELINE 3-STAGE (HERO):**
   - Label "🔄 Pemantauan Kompetensi Pipeline (Ogoun & Tamunosiki-Amadi 2023, Zeb-Obipi 2017)"
   - 3 stage box horizontal connected dengan arrow:
     - **① Information Gathering & Evaluation** (full term, decision #5)
       - Sub: Self-Assessment · Directed Assessment · Shop-Floor Assessment
       - HC Portal features: 📝 Assessment Online · 📊 KKJ Matrix · 🎯 PROTON IDP
     - **② Activity Auditing**
       - Sub: Evidence Gathering
       - HC Portal features: 📎 Upload Evidence · ✅ Approval Workflow · 🔍 Audit Log · 🔐 RBAC (split, decision #6)
     - **③ Feedback Loop**
       - Sub: Real-time + Transparent
       - HC Portal features: 🔥 Heatmap Gap · 🏆 Certificate Download · 🔔 Notifikasi In-App
   - Sub-caption: "↑ HC Portal sebagai Single Source of Truth — semua stage di 1 platform"

   **Zone kanan — Outcome Matrix:**
   - Tabel 4 kolom: Dimensi | R Spearman | p-value | Target Panca Mutu
   - Row 1: Timeliness | 0.777 | 0.000 | **Delivery** (signifikan)
   - Row 2: Innovativeness | 0.610 | 0.040 | **Cost** + **Quality** (signifikan)
   - Row 3: Task Alertness | 0.190 | 0.089 | **Moral** (tidak signifikan)
   - Row 4 (decision #1-B): HSSE | — | — | (qualitative inference, no R) — *implied via passing grade + competency gate*
   - Footer: "⚙️ Formula: Spearman R · p<0.05 = signifikan"

7. **Issue A-F + Improvement 1-7 row** (grid 1fr | 1fr)
   - Kiri: ❌ Issue codes (compact pill list)
   - Kanan: ✅ Improvement codes (compact pill list)
   - Tabel legend dapat di-collapse atau full mengikuti Versi P existing

8. **Tech stack + Standar row** (grid 1fr | 1fr)
   - Kiri: Rencana Pembuatan (tech stack + dev workflow Lokal→Dev→Prod)
   - Kanan: Standar (External: SE1-SE3, Internal: SI1-SI3)

9. **"Coret yang tidak digunakan" section** (decision #2-A)
   - Label kecil + list:
     - USED: ✓ FMEA · ✓ Technical Reference
     - CORET: ~~Cost & Benefit Analysist~~ · ~~Scatter Diagram~~ · ~~P&ID~~ · ~~As Built Drawing~~ · ~~PFD~~ · ~~Gambar Teknik (mekanikal)~~ · ~~Mekanika Teknik~~ · ~~Others~~
   - Rationale per item di tooltip atau footnote

10. **Theory footer bar** (1 row)
    - "Theory: Ogoun & Tamunosiki-Amadi (2023) Competence Monitoring → Employee Responsiveness · Zeb-Obipi (2017) 3-stage activity model · Pertamina Panca Mutu Framework"

### 5.2 Color Palette
Identical to Versi P existing (5 token max):
- `#C8102E` Red — Sebelum / pain
- `#00558C` Blue — Portal / digital
- `#00A551` Green — improvement / Sesudah
- `#FFC72C` Yellow — buffer / pipeline accent
- `#6b7280` Gray — neutral metadata

### 5.3 Typography
Identical to Versi P existing v3.7 token scale:
- `--fs-xs: 0.75rem` (floor)
- `--fs-sm: 0.85rem`
- `--fs-base: 0.95rem`
- `--fs-md: 1.05rem`
- `--fs-lg: 1.3rem`
- `--fs-xl: 2rem`

### 5.4 Print CSS
- `@page { size: A3 landscape; margin: 1cm; }`
- `print-color-adjust: exact` untuk preserve colors
- `page-break-inside: avoid` di tiap section utama

## 6. Opsi IV — `workflow-topology-refined.html`

### 6.1 Layout (modifikasi Versi P existing)

Mulai dari Versi P existing (`docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html`), **tidak ubah** struktur utama. Tambah:

1. **Header bar** — sub-title diganti: "Versi P-Refined: Workflow Topology + Theory Grounding"

2. **Sebelum panel** — UNCHANGED dari Versi P existing

3. **🆕 CALLOUT 1 — Flow Proses Pemantauan Kompetensi** (di antara Sebelum-Sesudah, sebelum transformation arrow)
   - Box yellow border-dashed
   - Label: "🆕 Flow Proses Pemantauan Kompetensi (Ogoun & Tamunosiki-Amadi 2023)"
   - 3 stage horizontal: ① Information Gathering & Evaluation → ② Activity Auditing → ③ Feedback Loop
   - Sub-caption: "Bridging 'DMZ Buffer Zone' (IT/OT analog) ↔ HR Competence Monitoring framework"

4. **Transformation arrow** ▼ — UNCHANGED

5. **Sesudah panel** — UNCHANGED dari Versi P existing (Buffer Zone HC Portal + 5 layer + markers 1-7)

6. **Tabel komparasi 10 aspek** — UNCHANGED

7. **🆕 CALLOUT 2 — Formula Korelasi → Panca Mutu** (di antara tabel komparasi dan footer)
   - Box yellow border-dashed
   - Label: "🆕 Formula Korelasi Empiris → Panca Mutu (Ogoun §4 + Risalah PROTON Panca Mutu)"
   - Tabel 4 kolom: Dimensi | R Spearman (p<0.05) | Target Panca Mutu | Fitur HC Portal
     - Timeliness | R=0.777 (p=0.000) Sangat Kuat | Delivery | Real-time DB + SignalR
     - Innovativeness | R=0.610 (p=0.040) Kuat | Cost + Quality | In-house development
     - Task Alertness | R=0.190 (p=0.089) Lemah | Moral | Passing grade pre-CSU
     - HSSE | — | (qualitative inference, no R) | Audit-ready evidence

8. **Standards row** (modify existing — add Internal column)
   - Standar External (existing): SE1-SE3
   - 🆕 Standar Internal (Pertamina): SI1-SI3 — full nomor pedoman

9. **🆕 "Coret yang tidak digunakan" section** (decision #2-A)
   - Sama dengan Opsi II §5.1.9

10. **🆕 Theory footer bar**
    - "Theory grounding: Ellström & Kock (2008) Integrated Strategy · Ogoun & Tamunosiki-Amadi (2023) Competence Monitoring · Pertamina Panca Mutu Framework"

### 6.2 Visual Style
- Preserve seluruh CSS Versi P v3.7 existing
- Tambahan: callout box style — `background: #fffbeb; border: 2px dashed #FFC72C; border-radius: 0.5rem;`
- Border-top header bar tetap biru `#00558C` (differentiate dari Opsi II green)

## 7. PCP Template 7-Slot Compliance Check

| Slot | Opsi II | Opsi IV |
|------|---------|---------|
| 1. Design / Gambar Teknik | ✅ 3-zone pipeline | ✅ 5-layer Purdue adaptation |
| 2. Sebelum/Sesudah | ✅ Full 5-layer Sebelum + zone Sesudah pipeline | ✅ Full 5-layer Sebelum + Sesudah (existing) |
| 3. Aspect Table | ✅ Outcome matrix 4x4 (R × Panca Mutu) | ✅ Komparasi 10 aspek (existing) |
| 4. Flow Proses | ✅ PRIMARY — Pipeline hero | ✅ NEW callout 1 |
| 5. Formula | ✅ PRIMARY — R-coefficient matrix | ✅ NEW callout 2 |
| 6. Issue A-F + Improvement 1-7 | ✅ Compact row + legend | ✅ UNCHANGED dari Versi P |
| 7. Standar External + Internal | ✅ Compact row | ✅ Existing External + NEW Internal |
| **Bonus: Coret yang tidak digunakan** | ✅ | ✅ |

**Score: 8/7 keduanya** (7 wajib + 1 bonus).

## 8. Approved Default Decisions (dari Self-Review)

| # | Decision | Implementation |
|---|----------|----------------|
| 1 | HSSE qualitative inference | Add row HSSE di outcome matrix, tandai "(qualitative inference, no R)" |
| 2 | "Coret" section | Tambah section "Coret yang tidak digunakan" di kedua opsi |
| 3 | Sebelum Opsi II vertical stack | Full 5-layer Sebelum di atas, 3-zone main di bawah |
| 4 | Versi II vs P-refined coexist | Keduanya di `slide8-risalah/`, user pilih saat redraw PPT |
| 5 | Full Ogoun term | "Information Gathering & Evaluation" (legend); badge boleh "Info Gathering" |
| 6 | Audit Log + RBAC split | 2 fitur terpisah di Activity Auditing zone |
| 7 | PROTON definisi resmi | "Professional Refinery Operations Competency Development" |
| 8 | Naming generic | `pipeline-outcome.html` + `workflow-topology-refined.html` |

## 9. PNG Export Process

Per file HTML:
1. Open di browser
2. Print → Save as PDF (A3 landscape, scale 100%)
3. Convert PDF page 1 → PNG (resolution 300 DPI minimum)
4. Save sebagai `<filename>.png` di folder yang sama

Atau: gunakan Playwright `browser_take_screenshot` dengan `fullPage: true, type: 'png'`.

## 10. Implementation Sequence (untuk plan)

1. **Setup folder** — `mkdir docs/pcp-HCPortal-2026/slide8-risalah/`
2. **Create `pipeline-outcome.html`** — Opsi II (~700 line HTML+CSS, modeled after `versi-p-workflow-topology.html`)
3. **Create `workflow-topology-refined.html`** — Opsi IV (copy + modify Versi P existing)
4. **Create `index.html`** — master viewer 2 card
5. **Create `README.md`** — rationale, reference mapping, recovery
6. **PNG export** — Playwright screenshot both HTML
7. **Visual QA** — Playwright navigate + snapshot both files
8. **Commit + tag** — `slide8-risalah-v1.0`

## 11. Acceptance Criteria

- [ ] Both HTML files render correctly di browser modern (Chrome/Edge)
- [ ] Print preview A3 landscape clean (no clipping, no overflow)
- [ ] PNG export 300 DPI, file size < 2MB each
- [ ] 7/7 PCP slots compliance verified
- [ ] All 15 reference items (R1-SI3) cited where applicable
- [ ] No typo, no broken layout @ breakpoint 1200px / 900px
- [ ] All 8 approved default decisions applied
- [ ] README.md punya reference mapping table + recovery instructions

## 12. Recovery & Versioning

- Tag git: `slide8-risalah-v1.0` setelah PNG export selesai
- Branch: kerja di `main` (low risk, tidak ubah Versi P existing)
- Recovery: `git checkout slide8-risalah-v1.0 -- docs/pcp-HCPortal-2026/slide8-risalah/`

## 13. Next Step

Setelah spec ini approved → invoke `writing-plans` skill untuk decompose ke implementation plan dengan task atomik per file.
