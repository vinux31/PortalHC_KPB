# Design Spec — PCP SMART 2026 §3.4 HC Portal — Versi X + Versi Z

**Tanggal:** 2026-05-22
**Versi:** Tambahan ke v3.0 (P + C sudah SHIPPED)
**Topik:** 2 diagram landscape baru untuk audience Process Improvement / Lean Six Sigma
**Status:** Draft — pending user review
**Referensi visual:** `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/pendukung/reference-pcp-page8.png` (PCP SMART 2026 Final.pdf page 8)

---

## 1. Konteks

Existing v3.0 sudah SHIPPED dengan 2 diagram:
- **Versi P** — Workflow Topology (Purdue-style adaptasi, 5 layer aktor + DMZ analog HC Portal Buffer)
- **Versi C** — Comparison Dashboard (Card-grid 7 fitur, executive view)

Audience yang **belum ter-cover**:
- Process Improvement / BPM / SOP review → butuh **workflow-focused diagram**
- OPEX / Lean Six Sigma / CI → butuh **quantified value stream**

Purdue Model (versi P) sah untuk slide PCP cybersec IT/OT (page 8 reference), tapi **HC Portal domain = workflow management**, bukan ICS network. Adaptasi Purdue di versi P sedikit forced semantik (DMZ = security buffer ≠ HC Portal hub).

Versi X dan Z mengadopsi **diagram framework yang native untuk workflow improvement**:
- **BPMN 2.0** (ISO 19510) — standard de-facto business process modeling
- **Value Stream Map (VSM)** — standard Lean Manufacturing / Six Sigma

## 2. Tujuan

Tambahkan 2 versi diagram §3.4 yang:

1. **Native domain-fit** — pakai notasi standard untuk workflow improvement, bukan adaptasi forced
2. **Hero workflow deep-dive** — fokus 1 fitur representatif (Assessment) untuk story kuat
3. **Quantified** — cycle time + lead time + VA ratio per step (Lean framing)
4. **PCP-faithful layout** — dense single landscape, side-by-side Sebelum/Sesudah, tabel inline

Skip:
- Modifikasi versi P atau C existing (untouched, tetap available)
- Coverage 7 fitur lengkap di diagram (sudah ter-cover versi C, di versi X+Z hanya tabel inline)
- Interaktif kompleks JS (tetap standalone HTML + tooltip native)

## 3. Workflow Hero — Assessment (Rekonstruksi Cycle Time)

Pick: **Assessment** sebagai hero workflow.

**Alasan:**
- Δ-67% step paling balanced (bukan ekstrem)
- Handoff terkaya (Excel master + form manual + cross-check + approval + arsip)
- Audience HC paling familiar
- Issue cover A B C D (4 dari 6 issue total) → diagram banyak marker

### Sebelum (As-Is) — 6 Step Manual

| # | Step | Aktor | Tools | CT (active) | LT (wait) | Issue | VA? |
|---|------|-------|-------|:---:|:---:|:---:|:---:|
| 1 | HC siapkan form Excel + distribusi email | HC | Excel + Email | 1j | 0,5j | A, B | N |
| 2 | Atasan forward ke pekerja | Atasan | Email/WA | 0,5j | 24j | A | N |
| 3 | Pekerja isi form manual, kirim balik | Pekerja | Excel/paper | 1j | 16j | A | **Y** |
| 4 | HC input hasil + cross-check KKJ/Sertifikat | HC | 4 Excel files | 4j | 0,5j | A, B, D | N |
| 5 | HC lapor atasan via Word + email | HC | Word + Email | 2j | 8j | C, D | N |
| 6 | Arsip Excel/Word/paperwork | HC | File share | 1j | 0 | C | N |
| | **TOTAL** | | **4-5 tools** | **9,5j** | **49j** | **A B C D** | **1 VA / 6** |

**Total Lead Time Sebelum: 58,5j (~7 hari kerja)**
**VA ratio of CT: 10,5%** (1j VA / 9,5j active)
**VA ratio of LT: 1,7%** (1j VA / 58,5j total)

Footnote: 1 siklus = batch 1 paket assessment, ~10-20 pekerja.

### Sesudah (To-Be) — 2 Step via HC Portal

| # | Step | Aktor | Tools | CT (active) | LT (wait) | Improvement | VA? |
|---|------|-------|-------|:---:|:---:|:---:|:---:|
| 1 | HC trigger di portal + auto-notif + form auto-generated | HC | HC Portal | 0,1j (6 mnt) | 0 | 2, 4 | N |
| 2 | Pekerja submit di portal + audit log + notif balik | Pekerja | HC Portal | 0,25j (15 mnt) | 8j | 3, 7 | **Y** |
| | **TOTAL** | | **1 tool** | **0,35j** | **8j** | **2, 3, 4, 7** | **1 VA / 2** |

**Total Lead Time Sesudah: 8,35j (~1 hari kerja)**
**VA ratio of CT: 71,4%** (0,25j VA / 0,35j active)
**VA ratio of LT: 3%** (0,25j VA / 8,35j total)

### Delta Terukur

| Metric | Sebelum | Sesudah | Δ |
|--------|:---:|:---:|:---:|
| Step count | 6 | 2 | **-67%** |
| Tools | 4-5 | 1 | **-80%** |
| Active CT | 9,5j | 0,35j | **-96%** |
| Lead Time | 58,5j | 8,35j | **-86%** |
| Calendar | ~7 hari | ~1 hari | **-86%** |
| VA ratio of CT | 10,5% | 71,4% | **+581% lift** |

Active CT -96% ≈ konsisten dengan klaim existing `~95% waktu`.

### Konsistensi Coverage

Cross-check dengan versi C coverage matrix Assessment: A B C D ✅ (no E/F)
Cross-check Improvement: 2, 3, 4, 7 (subset dari 1-7) — Improvement 1 (Analytics) tidak relevan workflow Assessment per-orang, tapi muncul di Manajemen lane Sesudah sebagai dashboard receiver.

## 4. Versi X — BPMN Swimlane Murni

### 4.1 Konsep

**BPMN 2.0 As-Is/To-Be** — 4 lane aktor + 1 system lane (HC Portal). Side-by-side Sebelum kiri / Sesudah kanan. Native workflow notation, no Purdue adaptation.

### 4.2 Lane Structure

| Lane | Sebelum | Sesudah |
|------|---------|---------|
| Manajemen | Passive (terima laporan akhir) | Dashboard self-service (Improvement 1) |
| HC | Aktif — 4 task (1, 4, 5, 6) | Aktif — 1 task (trigger) |
| Atasan | Aktif — 1 task (forward) | Passive — auto-notif view-only |
| Pekerja | Aktif — 1 task (isi manual) | Aktif — 1 task (submit) |
| **HC Portal (System)** | — (tidak ada) | **Aktif** — Form auto-gen + Audit Log + Notif broadcast |

**Posisi System lane:** **central** (antara HC dan Atasan) di Sesudah panel — visualisasi portal sebagai hub absorb semua handoff.

### 4.3 BPMN Notation

| Element | Visual | Meaning |
|---------|:---:|---------|
| Start event | ● filled circle | Trigger workflow |
| End event | ○ thick circle | Workflow complete |
| Task | □ rounded rect | Activity step |
| Gateway | ◇ diamond | Decision (Sebelum: approval lisan implisit) |
| Sequence flow | → solid arrow | Within lane |
| Message flow | ⤳ dashed arrow | Cross-lane handoff |
| Data object | 📄 page icon | Excel/Word/file |
| Annotation | [A]/[1] badge | Issue/Improvement marker dengan tooltip |
| System lane | ⚙ gear icon | HC Portal (Sesudah only) |

### 4.4 Visual Convention

- **Sebelum panel:** background `#FEE` (red tint), task box red border `#C8102E`, sequence/message lines red, multi data object 📄
- **Sesudah panel:** background `#EEF` (blue tint), task box blue/green border, lines biru terstruktur, 1 data object terpusat
- **System lane** (Sesudah): background gradient `#00558C`→`#EEF`, label italic, icon ⚙ prominent
- **Issue markers** (A-F): badge bulat merah `#C8102E`, tooltip `title=""` dengan deskripsi
- **Improvement markers** (1-7): badge bulat hijau `#00A551`, tooltip deskripsi
- **Cycle time label:** italic kecil bawah task box, format `1j+0,5j` (active+wait)
- **Total Lead Time:** stripe footer per panel, font besar prominent
- **Crisscross visual** (Sebelum): message flow diagonal merah HC→Atasan→Pekerja→HC bikin spaghetti — intentional pain visualization

### 4.5 Data Object Treatment (Hybrid)

- **Sebelum Step 1** ("Siapkan Excel + distribusi"): render **5 icon Excel terpisah** 📄📄📄📄📄 → visual fragmentasi prominent
- **Sebelum Step 4** ("Input + cross-check"): render **4 icon Excel** (Pekerja, Assessment, KKJ, Sertifikat) cross-check arrows
- **Sebelum Step 5** ("Lapor Word/Email"): render **1 Word + 1 Email icon**
- **Sebelum Step 6** ("Arsip"): single 📄 abstract
- **Sesudah:** 1 data object icon ⚙ HC Portal terpusat di System lane

### 4.6 Layout (ASCII Reference)

Lihat Section 2 percakapan brainstorming. Implementasi pakai **CSS Grid + SVG inline** untuk swimlane + connection lines.

### 4.7 File

`docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

## 5. Versi Z — Hybrid BPMN + VSM

### 5.1 Konsep

**Lean Six Sigma framing** — 3 zona vertikal:
1. **Zona 1: BPMN Swimlane compact** (~30% height) — adaptasi compact dari Versi X
2. **Zona 2: VSM Strip** (~40% height) — process box + data box + inventory triangle + timeline stripe
3. **Zona 3: Metric Cards Quantified** (~15% height) — 5 KPI cards

Sisanya: header (~5%), tabel + footer (~10%).

### 5.2 VSM Notation

| Element | Visual | Meaning |
|---------|:---:|---------|
| Process box | ┌──┐ rect | Activity (CT inside) |
| Data box | small grid | CT / Tool / VA / Issue / Improvement |
| Inventory triangle | ▽ | Wait/queue (LT label inside) |
| Push arrow | ▶▶ striped | Manual handoff (Sebelum) |
| Pull arrow | ── solid | System-pulled (Sesudah) |
| Communication line | ↯ lightning | Electronic (Portal) |
| Timeline stripe | █░▒ pattern | VA (filled) / NVA (dashed) / Wait (dotted) |
| Kaizen burst | ✦ star | Improvement opportunity marker |

### 5.3 VA Classification

Sebelum: 1 VA (Step 3: Pekerja isi form = creating assessment data) + 5 NVA (setup, transport, rework, over-processing, storage). Step 4 (HC cross-check) = **murni NVA** (re-entry data sama = waste mutlak per Lean orthodox).

Sesudah: 1 VA (Step 2: Pekerja submit = creating data) + 1 NVA (Step 1: trigger setup).

### 5.4 Timeline Stripe Scale

**Shared linear scale 0–58,5j.** Sesudah strip jadi ~14% panjang dari Sebelum (visual impact brutal). Tick marker per 10j untuk readability.

Saw-tooth pattern:
- VA segment: solid green `#00A551`
- NVA segment: diagonal stripe grey `#6B7280`
- Wait segment: dotted pattern light red `#FEE`

### 5.5 Metric Cards (Zona 3)

5 cards horizontal, urutan prominence:

| Card | Sebelum | Sesudah | Δ | Badge |
|------|:---:|:---:|:---:|:---:|
| Lead Time | 58,5j | 8,35j | -86% | ✅ Lean |
| Active CT | 9,5j | 0,35j | -96% | — |
| VA Ratio (of CT) | 10,5% | 71,4% | +581% lift | ✅ >Lean 25% target |
| Tools | 5 | 1 | -80% | — |
| Step | 6 | 2 | -67% | — |

Style: card border `#00A551` kalau badge ✅, font angka besar `2.5rem`, delta % di bawah, badge top-right.

### 5.6 Layout (ASCII Reference)

Lihat Section 3 percakapan brainstorming. Implementasi pakai **CSS Grid 3 baris** untuk zona, **SVG horizontal bar chart** untuk timeline stripe.

### 5.7 File

`docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

## 6. Konvensi Umum (Both Versions)

### 6.1 Color Palette

| Warna | Hex | Pakai |
|-------|-----|-------|
| Pertamina Red | `#C8102E` | Pain / Issue / Sebelum |
| Pertamina Blue | `#00558C` | Portal / Digital / Sesudah |
| Pertamina Green | `#00A551` | Improvement / VA / Success |
| Pertamina Yellow | `#FFC72C` | Transition / Decision gateway |
| Neutral Grey | `#6B7280` | NVA / passive lane |
| BG light | `#F8F9FA` | Canvas |

### 6.2 Tech Stack

- **Standalone HTML5** — single file, no build, no external dep selain Bootstrap Icons CDN
- **Inline CSS** dengan custom properties (`--color-*`)
- **SVG inline** untuk diagram (swimlane lane separators, BPMN shapes, connection arrows, VSM bars)
- **Tooltip:** `title=""` attribute (native browser, no JS)
- **Print:** `@media print` — A3 landscape, hide button, preserve color, page-break control
- **Responsive minimum:** 1366px landscape; target optimal: 1920x1080

### 6.3 Accessibility

- Semantic HTML5 (`<header>`, `<main>`, `<section>`, `<footer>`)
- `aria-label` di SVG diagram + role descriptions
- Contrast WCAG AA (red on white ≥ 4.5:1 ✅)
- Tooltip text mirror di tabel inline (screen reader friendly)

### 6.4 File Structure (Updated)

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/
├── index.html                          ← UPDATE (4 versi)
├── README.md                           ← UPDATE (4 versi)
├── versi-p-workflow-topology.html      (existing untouched)
├── versi-c-comparison-dashboard.html   (existing untouched)
├── versi-x-bpmn-swimlane.html          ← NEW
├── versi-z-bpmn-vsm-hybrid.html        ← NEW
├── archive/
├── flow-proses/
├── gambar-teknik/
└── pendukung/
    ├── reference-pcp-page8.png         (sudah saved)
    └── reference-pcp-page8.txt
```

### 6.5 Git Tag Scheme

**Single combined tag:** `pcp-hcportal-3.4-v3.6` untuk release kedua versi sekaligus (atomic milestone, efisien git history).

### 6.6 Index.html Updates

Tambah 2 card baru (Versi X + Versi Z) ke grid existing. Tambah **matriks "Audience → Versi":**

| Audience | Versi rekomendasi |
|----------|:---:|
| Engineering / Operations PCP main | Versi P |
| Executive / Management showcase | Versi C |
| Process Improvement / BPM / SOP | **Versi X** |
| OPEX / Lean Six Sigma / CI / Data-driven | **Versi Z** |

### 6.7 README.md Updates

- Update "Cakupan §3.4 v3": "2 diagram" → "4 diagram"
- Tambah subsection "Versi X — BPMN Swimlane (TERTIARY)" + "Versi Z — BPMN+VSM Hybrid (QUATERNARY)"
- Update tag list

## 7. Commit Strategy (Atomic, 15 commits)

1. Scaffold versi X HTML skeleton + CSS variables
2. Versi X header + container layout
3. Versi X Sebelum swimlane (4 lane + 6 task + crisscross)
4. Versi X Sesudah swimlane (5 lane termasuk System + 2 task)
5. Versi X data objects hybrid (5/4/1/1 icons)
6. Versi X tables (Aspek + Issue + Improvement) + tooltip
7. Versi X print CSS + footer
8. Scaffold versi Z HTML skeleton
9. Versi Z Zona 1 BPMN compact
10. Versi Z Zona 2 VSM Sebelum (process boxes + data box + timeline stripe linear)
11. Versi Z Zona 2 VSM Sesudah (compact)
12. Versi Z Zona 3 metric cards (5 KPI)
13. Versi Z tables + print CSS + footer
14. Update index.html (4 versi + audience matrix)
15. Update README.md + tag `pcp-hcportal-3.4-v3.6`

## 8. Acceptance Criteria

### Versi X
- [ ] BPMN swimlane 4 lane Sebelum + 5 lane Sesudah render dengan SVG lane separator
- [ ] 6 task box Sebelum + 2 task box Sesudah + start/end event
- [ ] Crisscross diagonal message flow merah di Sebelum (visual spaghetti)
- [ ] 5 icon Excel di Sebelum Step 1 (hybrid treatment)
- [ ] System lane "HC Portal" central position di Sesudah
- [ ] Issue marker A-D + Improvement marker 2,3,4,7 dengan tooltip
- [ ] Cycle time label di tiap task (`Xj+Yj`)
- [ ] Total Lead Time stripe per panel
- [ ] Tabel Aspek/Issue/Improvement inline
- [ ] Print A3 landscape OK
- [ ] Coverage match versi C: Assessment cover A B C D

### Versi Z
- [ ] Zona 1 BPMN compact (Versi X simplified)
- [ ] Zona 2 VSM Sebelum: 6 process box + 5 inventory triangle + data box per process
- [ ] Zona 2 VSM Sesudah: 2 process box + 1 inventory triangle + data box
- [ ] Timeline stripe linear shared scale 0-58,5j dengan tick
- [ ] VA/NVA/Wait pattern saw-tooth correct
- [ ] Zona 3 5 metric cards (LT/CT/VA/Tools/Step) dengan delta + badge Lean
- [ ] Step 3 Sebelum + Step 2 Sesudah highlighted VA hijau
- [ ] Step 4 Sebelum = NVA (Lean orthodox)
- [ ] Tabel inline + print CSS A3
- [ ] Konsistensi angka dengan Section 3 spec

### Index + README
- [ ] index.html link 4 versi + audience matrix render
- [ ] README.md describe 4 versi + tag list updated

### Git
- [ ] 15 commit atomic dengan message format `feat(pcp-3.4-v3.6): <task>`
- [ ] Tag `pcp-hcportal-3.4-v3.6` annotated dengan summary

## 9. Out-of-Scope

- Modifikasi versi P / C existing
- C4 model atau Service Blueprint variant
- Swimlane untuk 6 fitur lain selain Assessment (hanya Assessment yang deep-dive di X+Z)
- Interactive JS (drag, zoom, filter)
- PDF export programmatic (gunakan browser print → save PDF)
- Animation / transition
- Mobile responsive < 1366px

## 10. Recovery & Rollback

Versi P + C tidak disentuh — recoverable trivial. Tag `pcp-hcportal-3.4-v3.5` (existing) masih intact.

Kalau X atau Z tidak diterima reviewer PCP, cukup hapus 2 file HTML + revert index/README + delete tag v3.6 → kembali ke state v3.5.
