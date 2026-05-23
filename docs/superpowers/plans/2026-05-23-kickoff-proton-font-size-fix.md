# Kickoff-PROTON Font Size Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development atau superpowers:executing-plans. Steps pakai checkbox.

**Goal:** Naikkan font-size body text yang <11pt di 11 slot supaya terbaca pada proyektor. Total ~11 slot affected. Per-slot atomic commit, hindari overflow.

**Architecture:** Edit in-place `docs/Kickoff-PROTON.html`. Sebagian besar font-size pakai inline `style="font-size: Npt"` (per slide), sisanya pakai CSS class. Fix per slot via Edit tool. Verify no overflow via Playwright bounding-rect check.

**Tech Stack:** Static HTML/CSS inline + class.

**Audit ref:** session brainstorm 2026-05-23.

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `docs/Kickoff-PROTON.html` | Modify | Multiple lokasi edit (inline styles + CSS class) |

---

## Font size target ladder

| Tier | Old | New | Rasional |
|---|---|---|---|
| Body bullet / row | 9.5pt | **10.5pt** | Minimum readable on projector |
| Mockup decor body | 10.5pt | **11pt** | Bumping 0.5pt minor visual |
| Card description | 10pt → 8pt critical | **10.5pt** | Match body tier |
| Metric badge / label | 8.5pt | **10pt** | Visible from back row |
| Step description (alur) | 8.5pt | **10.5pt** | Critical card desc |
| CLASSIFIED label | 8pt | **10pt** | Decorative but readable |
| Footer cite / caption | 9.5pt | tetap | Cite OK kecil |
| Eyebrow PART | 9pt | tetap | Convention OK |
| Badge "N/21" | 8.5pt | tetap | Slot counter compact OK |

---

## Affected slots inventory

| Slot | Severity | Element | Old → New |
|---|---|---|---|
| 9 | 🔴 P1 | `🔒 CLASSIFIED` label | 8pt → 10pt |
| 9 | 🔴 P1 | "Reveal saat rilis →" | 8.5pt → 10pt |
| 9 | 🟠 P2 | Card description body | 10pt → 10.5pt |
| 17 | 🔴 P1 | Step description (`.alur-step .as-desc`) | 8pt → 10.5pt |
| 17 | 🟠 P2 | Step title (`.alur-step .as-title`) | 10pt → 11pt |
| 18 | 🔴 P1 | Metric badge per role | 8.5pt → 10pt |
| 18 | 🔴 P1 | Pain/Win rows | 9.5pt → 10.5pt |
| 3 | 🟠 P2 | 3-card bullets | 10.5pt → 11pt |
| 5 | 🟠 P2 | CMP/CDP scope description | 9.5pt → 10.5pt |
| 10 | 🟠 P2 | SMART sub-text (Target jelas, dll) | 9pt → 10pt |
| 12 | 🟠 P2 | Kompetensi labels + sub-deskripsi | 10.5pt → 11pt |
| 16 | 🟠 P2 | Role description body | 10.5pt → 11pt |
| 2 | 🟡 P3 | Mockup body bullets | 10.5pt → 11pt |
| 7 | 🟡 P3 | Mockup desc bullets | 10.5pt → 11pt |
| 8 | 🟡 P3 | Mockup desc bullets | 10.5pt → 11pt |

---

## Task 1: Slot 9 (BP) — CLASSIFIED + Reveal + card desc

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot `data-slide="9"` block (BP Coming Soon)

- [ ] **Step 1: Locate slot 9 block**

Run:
```bash
grep -n 'data-slide="9"' docs/Kickoff-PROTON.html
```
Note line number; read slot 9 block (~50 lines).

- [ ] **Step 2: Identify font-size declarations**

Run within slot 9 line range:
```bash
sed -n 'START_LINE,END_LINEp' docs/Kickoff-PROTON.html | grep -nE 'font-size: ?[0-9]+(\.[0-9]+)?pt'
```
List every inline + class reference.

- [ ] **Step 3: Edit per element**

Pakai Edit tool. Untuk tiap match:

| Element pattern (find) | Target font-size |
|---|---|
| `🔒 CLASSIFIED` label container | 10pt |
| `Reveal saat rilis →` pill | 10pt |
| Card description body (Riwayat kompetensi/Kelayakan promosi/Rekomendasi/Succession) | 10.5pt |

Edit each find/replace, mempertahankan struktur style attribute.

- [ ] **Step 4: Visual verify via Playwright**

Activate slot 9, screenshot, JS bounding-rect check tidak overflow slide bounds.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 9 BP font-size critical bump

- 🔒 CLASSIFIED 8pt -> 10pt (4 card)
- Reveal saat rilis 8.5pt -> 10pt (4 card)
- Card description body 10pt -> 10.5pt (4 card)
Visible from projector back row."
```

---

## Task 2: Slot 17 (Alur 6 Langkah) — step desc + title

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot `data-slide="17"` block + CSS `.alur-step` class if used

- [ ] **Step 1: Locate**

```bash
grep -n 'data-slide="17"\|\.alur-step' docs/Kickoff-PROTON.html
```

- [ ] **Step 2: Identify which markup pattern slot 17 uses**

Read slot 17 block. Bisa pakai class `.alur-step` (CSS-defined) atau inline div.

Per CSS line 1776-1777:
- `.alur-step .as-title` 10pt
- `.alur-step .as-desc` 8pt

Tapi extract Playwright menunjukkan 8.5pt — bisa jadi inline style.

Inspect actual markup slot 17.

- [ ] **Step 3: Edit per pattern**

| Element | Target |
|---|---|
| Step description (sub-text per step card) | 10.5pt |
| Step title (HC Assign Coach / Deliverable / dll) | 11pt |

Edit class CSS dan/atau inline. Kalau pakai inline, edit per langkah card (6 instance).

- [ ] **Step 4: Playwright overflow check** — slot 17 6 card grid 3x2. Verify card content tidak ngeluapin card box.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 17 Alur 6 Langkah step desc/title bump

- Step description 8.5pt -> 10.5pt (6 card)
- Step title 10pt -> 11pt
Bench audience baca dari layar belakang."
```

---

## Task 3: Slot 18 (Manfaat Per Role) — metric + Pain/Win

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot `data-slide="18"` block (4 role card 2x2 grid)

- [ ] **Step 1: Locate**

```bash
grep -n 'data-slide="18"' docs/Kickoff-PROTON.html
```

- [ ] **Step 2: Identify font-size patterns**

Slot 18 pakai banyak inline style. Cari:
- Metric badge pill (📊 100%, ⏱️ -3 jam, ⚡ Real-time, 📈 Data-driven) — 8.5pt
- Pain row strikethrough + Win row — 9.5pt
- Role title (👤 Coachee dll) — 14pt OK

- [ ] **Step 3: Edit**

| Element | Target |
|---|---|
| Metric badge | 10pt |
| Pain row + Win row + arrow | 10.5pt |

- [ ] **Step 4: Overflow check** — slot 18 punya 4 card 2x2 dengan 3 baris Pain/Win + metric. Naikkan font bisa overflow. Kalau iya, kurangi padding atau row gap.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 18 Manfaat metric badge + Pain/Win bump

- Metric badge 8.5pt -> 10pt (4 role)
- Pain/Win row 9.5pt -> 10.5pt (12 row total)
Audience baca per-role concrete benefits."
```

---

## Task 4: Slot 3 (NEW Img 6 Strengthening) — card bullets

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — slot 3 block (3-card BG/Challenge/Solution)

- [ ] **Step 1+2+3: Edit**

Slot 3 inline style pada `<ul style="font-size: 10.5pt..."` (3 occurrences, satu per card).

Edit 3 instances: `font-size: 10.5pt` → `font-size: 11pt`.

⚠️ **Caution**: bukan global replace. Slot 3 ada `font-size: 10.5pt` di 3 card. Tapi slot lain bisa juga punya `font-size: 10.5pt`. Identify slot 3 context lewat baris number.

- [ ] **Step 4: Verify + commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 3 Strengthening card bullets 10.5pt -> 11pt"
```

---

## Task 5: Slot 5 (NEW Img 7 Flowchart) — scope desc

**Files:**
- Modify: slot 5 block

- [ ] **Step 1+2+3: Edit**

Element: 2 legend card + takeaway. Inline `font-size: 9.5pt`.

Edit: `font-size: 9.5pt` (di legend card) → `font-size: 10.5pt`.

- [ ] **Step 4: Verify slot 5 (height 460px image frame) tetap fit**

Check `fitsInSlide` via Playwright.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 5 Flowchart scope desc 9.5pt -> 10.5pt"
```

---

## Task 6: Slot 10 (PROTON intro) — SMART sub-text

**Files:**
- Modify: slot 10 (5 SMART card di grid 5-col)

- [ ] **Step 1+2+3: Edit**

Element: sub-text bawah huruf besar (S/M/A/R/T) → "Target jelas / Terukur / Bisa dicapai / Sesuai kerja / Punya jadwal".

Inline `font-size: 9pt; opacity: 0.9;` di 5 card.

Edit 5 occurrences: `font-size: 9pt` → `font-size: 10pt`.

⚠️ **Caution**: cari yang dalam slot 10 only (jangan global).

- [ ] **Step 4: Verify** — slot 10 has 5-col grid sempit, font bump cek apa text overflow card.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 10 SMART sub-text 9pt -> 10pt"
```

---

## Task 7: Slot 12 (NEW Img 9 Kompetensi/Tahun) — labels + sub

**Files:**
- Modify: slot 12 (3-col grid TAHUN 1/2/3)

- [ ] **Step 1+2+3: Edit**

Element: `<ul>` parent dalam 3 card pakai `font-size: 10.5pt`. Naikkan ke 11pt.

Edit 3 occurrences inside slot 12: `font-size: 10.5pt; line-height: 1.5` → `font-size: 11pt; line-height: 1.5`.

- [ ] **Step 4: Verify** — slot 12 ada min-height: 280px di card. Bump font berkemungkinan overflow. Cek.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 12 Kompetensi labels + sub 10.5pt -> 11pt"
```

---

## Task 8: Slot 16 (4 Peran) — role description

**Files:**
- Modify: slot 16 block (4 role card)

- [ ] **Step 1+2+3: Edit**

Element: role description text "Pekerja yang dikembangkan..." dll. Identify font-size declarations.

Estimate inline `font-size: 10.5pt` or class `.pilar-desc` di line 1073 (10pt). Cek mana yg dipakai slot 16.

Edit: bump body desc ke 11pt.

- [ ] **Step 4: Verify** — slot 16 grid 4-col. Cek tidak overflow.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 16 4 Peran role desc 10.5pt -> 11pt"
```

---

## Task 9: Slot 2 (Apa itu Portal HC) — mockup bullets

**Files:**
- Modify: slot 2 (mockup browser + bullets explainer)

- [ ] **Step 1+2+3: Edit**

Class `.mockup-content li { font-size: 9.5pt }` line 460. Tapi bullet body tampil 10.5pt — kemungkinan ada inline override.

Identify markup slot 2. Edit body bullets to 11pt.

- [ ] **Step 4: Verify + Step 5 Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 2 mockup bullets bump 10.5pt -> 11pt"
```

---

## Task 10: Slot 7 (CMP) — mockup desc bullets

**Files:**
- Modify: slot 7 block

- [ ] **Step 1+2+3: Edit**

Mockup right panel description bullets. Edit ke 11pt.

- [ ] **Step 4: Verify + Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 7 CMP desc bullets 10.5pt -> 11pt"
```

---

## Task 11: Slot 8 (CDP) — mockup desc bullets

**Files:**
- Modify: slot 8 block

- [ ] **Step 1+2+3: Edit**

Sama pola Task 10 untuk slot 8.

- [ ] **Step 4: Verify + Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): slot 8 CDP desc bullets 10.5pt -> 11pt"
```

---

## Task 12: Visual UAT + overflow verify all affected slots

**Files:** None modified. Playwright verification only.

- [ ] **Step 1: Start server (kalau belum)**

```bash
python -m http.server 8765 --bind 127.0.0.1 --directory .
```

- [ ] **Step 2: Per slot affected (2, 3, 5, 7, 8, 9, 10, 12, 16, 17, 18), Playwright check:**

Untuk tiap slot:
1. Activate slide via JS
2. Screenshot `kickoff-fontfix-slot-N.png`
3. JS bounding rect check: tiap container card vs slide bounds, tidak overflow

Bounding rect check script:
```javascript
() => {
  const slot = window.SLOT;
  const slide = document.querySelector(`[data-slide="${slot}"]`);
  const slideRect = slide.getBoundingClientRect();
  const body = slide.querySelector('.slide-body');
  const bodyRect = body.getBoundingClientRect();
  const overflowBottom = bodyRect.bottom - slideRect.bottom;
  return { slot, overflowBottom, ok: overflowBottom < 0 };
}
```

- [ ] **Step 3: Tabulate hasil**

| Slot | Overflow? | Visual OK? | Action |
|---|---|---|---|
| 2 | ... | ... | ... |
| 3 | ... | ... | ... |
| ... | ... | ... | ... |

- [ ] **Step 4: Kalau ada overflow → adjust padding/margin/line-height** (bukan turunkan font lagi)

Per slot dengan overflow, edit padding/margin inline supaya konten fit. Iterate.

- [ ] **Step 5: Close browser, kill server**

- [ ] **Step 6: Final summary commit (kalau ada adjust)**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): padding adjust post font bump untuk slot X/Y/Z"
```

---

## Task 13: Final verify + handoff

**Files:** None modified.

- [ ] **Step 1: Re-run JS font extraction** — confirm no <10pt body text

- [ ] **Step 2: Get HEAD hash**

```bash
git rev-parse HEAD
```

- [ ] **Step 3: Report ke user**

Summary 5 line:
- 11 slot font fixed (3 critical + 5 significant + 3 borderline)
- N commit baru
- HEAD: `<hash>`
- Visual UAT pass (no overflow)
- Pending: push origin/main + notif IT

---

## Self-Review Notes

**Spec coverage check:**
- ✅ Critical slot 9 (Task 1)
- ✅ Critical slot 17 (Task 2)
- ✅ Critical slot 18 (Task 3)
- ✅ Significant slot 3 (Task 4)
- ✅ Significant slot 5 (Task 5)
- ✅ Significant slot 10 (Task 6)
- ✅ Significant slot 12 (Task 7)
- ✅ Significant slot 16 (Task 8)
- ✅ Borderline slot 2 (Task 9)
- ✅ Borderline slot 7 (Task 10)
- ✅ Borderline slot 8 (Task 11)
- ✅ Visual overflow UAT (Task 12)
- ✅ Final handoff (Task 13)

**Placeholder scan:**
- ⚠️ Task 1-11 Step 2 ("Identify font-size patterns") sengaja deferred to execution time — implementer baca markup actual sebelum edit. Tidak hard-coded sed karena risiko collision global.
- ✅ Target fontsize per element fixed.

**Type consistency:**
- ✅ Target ladder konsisten (10pt → metric/label, 10.5pt → body row, 11pt → bullet/desc).

**Known gap:**
- Belum hardcode exact string find/replace per Task (Task 1-11). Implementer baca markup, ekstrak inline style, edit. Atomic per slot.
- Task 12 overflow check belum confirm — kalau ada slot yang sudah tight (e.g. slot 17, 18), padding adjust mungkin dibutuhkan.

---

## Out of Scope

- Font fix slot 1, 4, 6, 11, 13, 14, 15, 19, 20, 21 (sudah ≥11pt body)
- Refactor CSS class structure
- Restructure slide layout
- Push origin/main (user explicit)
