# Design — Kickoff-PROTON: 4 Corporate Slide Integration

**Date:** 2026-05-23
**File target:** `docs/Kickoff-PROTON.html`
**Author:** Rino (brainstorm session)
**Status:** Approved for implementation planning

---

## 1. Goal

Integrate 4 corporate-source slides (from Pertamina Dir SDM PPT) into the existing `Kickoff-PROTON.html` deck while preserving narrative flow and visual consistency. Two existing slides are removed to avoid duplication.

| Corporate slide | Topic |
|---|---|
| Img 6 | Strengthening Workforce Competency — From Compliance to Excellence |
| Img 7 | Integrated Digital Competency Platform — Flowchart CMP & CDP |
| Img 8 | PROTON Methodology — Blended Learning 70-20-10 |
| Img 9 | Kompetensi PROTON per Tahun (KKJ 2023) |

---

## 2. Scope of changes

| Action | Count | Detail |
|---|---|---|
| Insert new slide | 4 | Img 6, 7, 8, 9 |
| Delete existing slide | 2 | Sl 9 lama (2 Track × 3 Tahun), Sl 10 lama (Komponen 4 Pilar) |
| Renumber `data-slide` | 21 slot | sequential 1..21 |
| Update JS `TOTAL` | 1 line | `19 → 21` |
| Update `.slide-badge` text | 20–21 instance | `"N / 19" → "N / 21"` |
| Update `#slideCounter` initial | 1 line | `"1 / 19" → "1 / 21"` |
| Copy image asset | 1 file | `image-cache/.../7.png` → `docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png` |

Net slide count: 19 → **21 slide**.

---

## 3. Style strategy (decided: Option C — Hybrid)

- **Layout/visual:** adapt to existing Kickoff-PROTON style. Use existing CSS variables (`--teal`, `--amber`, `--green`, etc.), `.slide-header`, `.slide-title`, `.slide-badge`, card gradients consistent with Sl 4 / Sl 8 / Sl 10 (old) patterns.
- **Content authority:** preserve corporate-source wording for technical citations (TKO numbers, kompetensi list, KKJ reference). Footer cite line per new slide.
- **Asset rule:** pure HTML/CSS for Img 6 / 8 / 9 (mosaic of cards). Screenshot PNG for Img 7 (flowchart complexity makes HTML recreation cost-prohibitive).

---

## 4. Final slide sequence (display order)

| Slot | Title | Source | Eyebrow PART |
|---|---|---|---|
| 1 | Cover | existing | — |
| 2 | Apa itu Portal HC KPB? | existing | PART 1 · APA ITU WEB HC |
| 3 | Strengthening Workforce Competency | **NEW Img 6** | PART 1 · APA ITU WEB HC |
| 4 | Kenapa Portal HC dibangun? | existing | PART 1 · APA ITU WEB HC |
| 5 | Integrated Digital Competency Platform | **NEW Img 7** | PART 1 · APA ITU WEB HC |
| 6 | 3 Pilar Portal HC: CMP · CDP · BP | existing | PART 2 · FITUR PORTAL |
| 7 | CMP — Competency Management Platform | existing | PART 2 · FITUR PORTAL |
| 8 | CDP — Competency Development Platform | existing | PART 2 · FITUR PORTAL |
| 9 | BP — Business Partner | existing | PART 2 · FITUR PORTAL |
| 10 | PROTON (intro SMART) | existing | PART 3 · APA ITU PROTON |
| 11 | PROTON Methodology — 70-20-10 | **NEW Img 8** | PART 3 · APA ITU PROTON |
| 12 | Kompetensi PROTON per Tahun | **NEW Img 9** | PART 3 · APA ITU PROTON |
| 13 | KKJ — Kebutuhan Kompetensi Jabatan | existing | PART 3 · APA ITU PROTON |
| 14 | Silabus PROTON | existing | PART 3 · APA ITU PROTON |
| 15 | Coaching Guidance | existing | PART 3 · APA ITU PROTON |
| 16 | 4 Peran dalam Chain Coaching | existing | PART 3 · APA ITU PROTON |
| 17 | Alur PROTON End-to-End | existing | PART 3 · APA ITU PROTON |
| 18 | Manfaat PROTON Per Role | existing | PART 4 · KENAPA UNTUK SAYA |
| 19 | Outcome — Sertifikasi Mahir & IDP | existing | PART 4 · KENAPA UNTUK SAYA |
| 20 | Akses Portal — Cek Progres Saya | existing | PART 4 · KENAPA UNTUK SAYA |
| 21 | Penutup (Terima Kasih) | existing | — |

### Deletion impact

- **Sl 9 lama "2 Track × 3 Tahun":** info "2 track Operator vs Panelman" still surfaced in Sl 14 (Silabus, "2 track: Panelman & Operator"). Acceptable.
- **Sl 10 lama "Komponen 4 Pilar":** intro card for next 3 zoom-in slides removed. Slides 13 (KKJ), 14 (Silabus), 15 (Coaching Guidance) each have self-contained intro. Narrative loses scaffold but stays comprehensible.

### Renumber mapping (`data-slide` lama → baru)

```
Cover           : 1  → 1
Apa itu Portal  : 2  → 2
NEW Img 6       : –  → 3
Kenapa Portal   : 3  → 4
NEW Img 7       : –  → 5
3 Pilar         : 4  → 6
CMP             : 5  → 7
CDP             : 6  → 8
BP              : 7  → 9
PROTON intro    : 8  → 10
NEW Img 8       : –  → 11
NEW Img 9       : –  → 12
2 Track 3 Tahun : 9  → DELETE
Komponen 4 Pilar: 10 → DELETE
KKJ             : 11 → 13
Silabus         : 12 → 14
Coaching Guide  : 13 → 15
4 Peran         : 14 → 16
Alur E2E        : 15 → 17
Manfaat         : 16 → 18
Outcome         : 17 → 19
Akses Portal    : 18 → 20
Penutup         : 19 → 21
```

File order: rearrange `<div class="slide">` blocks dalam urutan slot 1..21. Saat ini Sl 14 lama (`data-slide="14"`, 4 Peran Chain Coaching) ditempatkan antara `data-slide="8"` dan `data-slide="9"` di file — gunakan kesempatan ini untuk normalize file order = display order.

---

## 5. New slide specifications

### 5.1 NEW Slide 3 — Strengthening Workforce Competency (Img 6)

```
Eyebrow  : "PART 1 · APA ITU WEB HC"
Title    : "Strengthening Workforce Competency"
           accent: " — Compliance ke Excellence"
Subtitle : "Mandat corporate yang melandasi Portal HC KPB"
Body     : 3-card grid horizontal (Background / Challenge / Solution)
```

| Card | Color theme | Label | Bullets |
|---|---|---|---|
| 1 | teal | BACKGROUND | • Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9 (26 Feb 2025) — Pengelolaan Kompetensi<br>• TKO Talent Mgmt No. B5.3-04/K20100/2025-S9 (20 Mar 2025) — Pengelolaan Kompetensi Teknis |
| 2 | amber | CHALLENGE | • TKO HCD-CPDP update terakhir 2018, tidak penuhi ketentuan korporat baru<br>• KPB belum punya Competency Management Platform & Competency Development Platform<br>• Risiko ketidakpatuhan peraturan korporat |
| 3 | green | SOLUTION | • Bangun Competency Management Platform (CMP)<br>• Bangun Competency Development Platform (CDP) — Operator & Panelman, blended learning<br>• 1 aplikasi terintegrasi untuk Operasional + Human Capital |

Footer cite: *"Ref: Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9 rev 0 + TKO Talent Mgmt No. B5.3-04/K20100/2025-S9 rev 0"*

---

### 5.2 NEW Slide 5 — Integrated Digital Competency Platform Flowchart (Img 7)

```
Eyebrow  : "PART 1 · APA ITU WEB HC"
Title    : "Integrated Digital Competency Platform"
           accent: " — Flowchart CMP & CDP"
Subtitle : "Peta proses end-to-end dari penyusunan SME sampai blended learning"
Body     : 2-col split
  Left  (~65%) : <img src="sosialisasi-screenshots/proton/flowchart-cmp-cdp.png"
                       alt="Flowchart Integrated CMP & CDP">
  Right (~35%) : 2 legend card stack + 1 caption block
```

Legend card 1 (blue dashed border):
> **CMP Scope** (Blue line) — SME + Penentuan KKJ + Asesmen Kompetensi Teknis + 4 branch hasil lulus (Asesor LSP / SME / Coach / New Exposure) + Penyepakatan Silabus

Legend card 2 (red dashed border):
> **CDP Scope** (Red line) — Eksekusi Program Pengembangan Kompetensi metode Blended Learning

Caption block (bottom right):
> CMP define + asses + bikin Silabus → CDP eksekusi Blended Learning. Handoff terjadi lewat Silabus, bukan asesmen.

Footer cite: *"Ref: Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9 rev 0"*

---

### 5.3 NEW Slide 11 — PROTON Methodology 70-20-10 (Img 8)

```
Eyebrow  : "PART 3 · APA ITU PROTON"
Title    : "PROTON Methodology"
           accent: " — Blended Learning 70-20-10"
Subtitle : "Mayoritas belajar dari kerja nyata, bukan kelas"
Body     : 2-col split
```

Left col (~50%): vertical stacked big-number rows
- **70%** Assignment — kerja + upload bukti deliverable
- **20%** Coaching — sesi terjadwal dengan coach
- **10%** Self-Study — baca materi mandiri (Coaching Guidance)

Right col (~50%): 3 circle equilateral diagram (Assignment ↔ Coaching ↔ Self-Study) — pure CSS pakai `position: absolute` + connector lines (border-bottom + border-right). Warna ikuti convention: Assignment=teal, Coaching=amber, Self-Study=green.

Footer cite: *"Sumber: Pedoman HCM Pertamina 2025"*

**Note:** corporate-source header "Professional Refinery Operations – Technical Competency Center" sengaja dropped di slide ini karena Sl 10 (PROTON intro) sudah expand akronim PROTON dengan gloss berbeda ("Professional Refinery Operations Competency Development"). Menampilkan 2 gloss berbeda di deck yang sama membingungkan.

---

### 5.4 NEW Slide 12 — Kompetensi PROTON per Tahun (Img 9)

```
Eyebrow  : "PART 3 · APA ITU PROTON"
Title    : "Kompetensi PROTON"
           accent: " — Per Tahun"
Subtitle : "Mengacu Kamus Kompetensi Jabatan (KKJ) Tahun 2023"
Body     : 3-col grid horizontal (TAHUN 1 / TAHUN 2 / TAHUN 3)
```

| Kolom | Color theme | Konten |
|---|---|---|
| TAHUN 1 | teal | • **Kompetensi 1** — Safe Work Practice & Lifesaving Rules<br>• **Kompetensi 5.1** — Refinery Process Operations & Optimization |
| TAHUN 2 | amber | • **Kompetensi 2** — Energy Management<br>• **Kompetensi 3** — Catalyst & Chemical Management<br>• **Kompetensi 5.2** — Refinery Process Operations & Optimization |
| TAHUN 3 | green | • **Kompetensi 4** — Process Control & Computer Operations<br>• **Kompetensi 5.3** — Refinery Process Operations & Optimization |

Footer cite: *"Sumber: Kamus Kompetensi Jabatan (KKJ) Pertamina Tahun 2023"*

---

## 6. JS / counter updates

```javascript
// Line ~2902
const TOTAL = 21;   // was 19

// Line ~2896 (HTML)
<span class="slide-counter" id="slideCounter">1 / 21</span>   // was "1 / 19"
```

All `.slide-badge` inside each slide header: update text from `"N / 19"` to new slot-number `"N / 21"`.

Keyboard nav (`ArrowRight`/`ArrowLeft`/`Space`/`PageDown`/`PageUp`/`Home`/`End`) auto-works because logic reads `TOTAL` constant. No additional change.

---

## 7. Asset preparation

Source: `C:\Users\Administrator\.claude\image-cache\619beac5-8327-4072-98cd-6b32c0409630\7.png` (resolution ~1626x917, sharp, no compression artifacts)

Target: `docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png`

Action: file copy + git add. Pure HTML/CSS slides (3, 11, 12 baru) no asset needed.

---

## 8. Validation plan

1. **Static check** — grep `data-slide` count = 21 unique values, all 1..21 contiguous; `TOTAL = 21`; `slideCounter` text `"1 / 21"`; every `.slide-badge` matches slot.
2. **Local render** — open `docs/Kickoff-PROTON.html` in Chrome. Walk through 21 slides via keyboard. Confirm content matches spec section 5.
3. **Visual QA**:
   - Slide 5 image not blur, fit container
   - Slide 3 / 11 / 12 card layout fit 1280x720 (no overflow)
   - Footer cite per slide baru tampil
   - Eyebrow PART transition jelas (Sl 5 → 6 = Part 1 → Part 2)
4. **Dark mode toggle** — `darkToggle` button, verify card readable di dark mode (existing CSS variables = teal-light/amber-dark redefined di `body.dark`).
5. **Fullscreen** — verify scaling correct 21 slides.
6. **Print mode** — verify 21 page break.
7. **Optional Playwright** — snapshot 21 slides ke `playwright-screenshots/` for visual diff per slide.

---

## 9. Risk register

| ID | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | Sl 5 image blur saat fullscreen 1280-wide | Low | Source PNG 1626x917 (>1280 width). Verify pasca-copy. Fallback: re-export from PPT pada 2x density. |
| R2 | Hapus Sl 10 (Komponen 4 Pilar) putus narasi ke 3 zoom-in slide | Medium | Slide 13 (KKJ), 14 (Silabus), 15 (Coaching Guidance) lama punya subtitle self-contained. Risk minimal — Validasi: read transition slide 12 → 13 untuk cek bridge tetap mengalir. |
| R3 | Eyebrow PART 1 di Sl 5 conflict konseptual (flowchart isinya CMP+CDP yang feature-domain Part 2) | Low | User decision firm — Part 1 dimaksudkan briefing/overview. Tidak ada visual issue. |
| R4 | Dark mode tidak diuji untuk 4 slide baru | Low | Pakai CSS variable existing (`--teal-*`, `--amber-*`, `--green-*`) yang sudah punya dark-mode override. Tetap test eksplisit di validation step. |
| R5 | Cover slide (Sl 1) layout unik — badge handling tidak konsisten | Low | Cek pasca-implement: kalau Sl 1 tidak punya badge, tidak perlu update. |

---

## 10. Out of scope

- Refactor slide existing yang tidak terkena delete (no edits to Sl 2, 3, 4, 6, 7, 8, 13, 14, 15, 16, 17, 18, 19, 20, 21 baru except `.slide-badge` text + `data-slide` renumber).
- Translate corporate English ke Bahasa Indonesia di luar konteks 4 slide baru.
- Tambah slide selain 4 corporate ini.
- Promosi ke Dev/Prod (responsibility IT Team per CLAUDE.md workflow).

---

## 11. Out-deliverable

- Updated `docs/Kickoff-PROTON.html` (21 slides, all renumbered, 4 new content blocks)
- New asset: `docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png`
- Manual UAT in browser passed
- Git commit per atomic step (asset copy + each new slide insertion + deletion + renumber as separate commits if possible)

---

## 12. Open items

None — all design questions resolved during brainstorm.
