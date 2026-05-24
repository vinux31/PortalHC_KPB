# Sosialisasi v2.2 — Audit Gap + Improvement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tambah 7 slide tutorial Admin (G1-G7) + 3 improvement slide existing (I2/I3/I6) ke `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`, ekspansi 34 → 41 slide untuk close gap Admin Panel UI tutorial coverage.

**Architecture:** Edit in-place single HTML file. 6-pass strategy: (1) shift `data-slide` Sl6..Sl34 ke posisi baru (descending order avoid collision), (2) shift `slide-badge` text + denominator, (3) cascade denominator Sl2..Sl5, (4) update JS `TOTAL` + `slideCounter`, (5) update HTML section comments shifted, (6) insert 7 new slides + edit 3 existing. CSS class reuse, HTML entity emoji (no Bootstrap Icons), no new CSS.

**Tech Stack:** Static HTML + CSS + vanilla JS. Edit tool. Playwright browser visual verify.

**Spec reference:** `docs/superpowers/specs/2026-05-24-sosialisasi-v2.2-audit-gap-improvement-design.md`

---

## Defaults Open Question (User Boleh Override)

1. **Mockup sample data:** pakai NIP 754201..754204 (Widodo/Andi/Citra/Budi) konsisten v2.1
2. **Panduan ref §:** placeholder `Panduan Operasional HC — §5.X` (X = nomor section spec)
3. **G7 default password:** assume ASP.NET Identity email-link reset (set password via token email)
4. **G5 H-day threshold:** assume 30/60/90 hari (selaras Sl40 Monthly task)
5. **Tag release:** `sosialisasi-internal-hc-v2.2`
6. **I3 Sl4 matrix overflow risk:** font 7pt di table cells, kalau overflow → split matrix jadi 2 baris di mockup

---

## File Structure

**Modified (single file):**
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
  - **7 new `<div class="slide default-deco">`** blocks insert (G6 Sl6, G1 Sl24, G7 Sl26, G2 Sl27, G3 Sl29, G4 Sl33, G5 Sl38)
  - **29 `data-slide` shift** (Sl6..Sl34 existing → Sl7..Sl41 new positions, descending order)
  - **28 `slide-badge` shift** untuk Sl6..Sl34 existing (Sl1 cover + Sl41 penutup no badge)
  - **4 cascade denominator** Sl2..Sl5 (` / 34 → / 41` replace_all)
  - **29 HTML section comment** update untuk shifted slides
  - **2 JS/UI literal** update (`const TOTAL = 41`, `slideCounter` initial `1 / 41`)
  - **3 edit slide existing**: Sl4 (matrix), Sl8 (notif tabel), Sl40 (quick ref expand)

**No new files. No CSS new class.**

---

## Task 1: Baseline snapshot + working tree clean check

**Files:**
- Read: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Snapshot counts**

```bash
echo "=== data-slide ===" && grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== slide-badge ===" && grep -c 'class="slide-badge"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== /34 badges ===" && grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== TOTAL + slideCounter ===" && grep -nE 'const TOTAL|id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `data-slide=` = 35 (34 slides + 1 JS selector)
- `class="slide-badge"` = 32 (Sl2..Sl29 incl. 4 new from v2.1)
- `/ 34</div>` = 32
- `const TOTAL = 34;` + `slideCounter">1 / 34<`

- [ ] **Step 2: Confirm working tree clean of Sosialisasi file**

```bash
git status --short docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: empty output (file clean). CMP Guide WIP files (HomeController.cs dll) tetap di working tree — JANGAN stage.

- [ ] **Step 3: No commit** (verification only)

---

## Task 2: Pass 1 — Shift `data-slide` descending (29 edits)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Tujuan:** Geser 29 data-slide existing (Sl6..Sl34) ke posisi baru. Descending order untuk avoid collision dengan slot kosong baru.

Mapping shift:

| Old | New | Old | New | Old | New |
|---|---|---|---|---|---|
| 34 | 41 | 24 | 28 | 14 | 15 |
| 33 | 40 | 23 | 25 | 13 | 14 |
| 32 | 39 | 22 | 23 | 12 | 13 |
| 31 | 37 | 21 | 22 | 11 | 12 |
| 30 | 36 | 20 | 21 | 10 | 11 |
| 29 | 35 | 19 | 20 | 9 | 10 |
| 28 | 34 | 18 | 19 | 8 | 9 |
| 27 | 32 | 17 | 18 | 7 | 8 |
| 26 | 31 | 16 | 17 | 6 | 7 |
| 25 | 30 | 15 | 16 | | |

(Sl1..Sl5 tetap.)

- [ ] **Step 1: Edit data-slide via PowerShell single regex (efficient)**

PowerShell run untuk batch edit (atomic, no Read-Edit loop):

```powershell
$path = 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html'
$content = Get-Content $path -Raw

# Descending order — avoid collision
$shifts = @(
    @{old=34; new=41}, @{old=33; new=40}, @{old=32; new=39},
    @{old=31; new=37}, @{old=30; new=36}, @{old=29; new=35},
    @{old=28; new=34}, @{old=27; new=32}, @{old=26; new=31},
    @{old=25; new=30}, @{old=24; new=28}, @{old=23; new=25},
    @{old=22; new=23}, @{old=21; new=22}, @{old=20; new=21},
    @{old=19; new=20}, @{old=18; new=19}, @{old=17; new=18},
    @{old=16; new=17}, @{old=15; new=16}, @{old=14; new=15},
    @{old=13; new=14}, @{old=12; new=13}, @{old=11; new=12},
    @{old=10; new=11}, @{old=9;  new=10}, @{old=8;  new=9},
    @{old=7;  new=8},  @{old=6;  new=7}
)

foreach ($s in $shifts) {
    $content = $content -replace "data-slide=`"$($s.old)`"", "data-slide=`"TMP$($s.new)`""
}
# Strip TMP prefix
$content = $content -replace 'data-slide="TMP', 'data-slide="'

Set-Content $path -Value $content -Encoding utf8 -NoNewline
```

**Catatan TMP prefix:** descending order + TMP marker avoid kasus "Sl7 shift ke 8, kemudian Sl8 shift ke 9 — kalau berurutan, Sl7 hasil shift jadi 8, lalu match Sl8 dan jadi 9 (chain)". TMP prevent re-match.

- [ ] **Step 2: Verify shifts**

```bash
echo "=== Sequential check 1..41 ===" && for n in $(seq 1 41); do c=$(grep -c "data-slide=\"$n\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html); if [ "$c" -gt "1" ]; then echo "DUPLICATE slide $n: count=$c"; fi; done; echo "Done."
echo "=== Slot kosong (harus 0 match untuk new positions) ===" && for n in 6 24 26 27 29 33 38; do c=$(grep -c "data-slide=\"$n\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html); echo "data-slide=\"$n\": $c (expect 0)"; done
echo "=== Total ===" && grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- No duplicates
- Slot baru (Sl6, Sl24, Sl26, Sl27, Sl29, Sl33, Sl38) = 0 matches (kosong, ready insert)
- Total data-slide = 35 (unchanged, 34 slides + 1 JS selector)

- [ ] **Step 3: Commit Pass 1**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "$(cat <<'EOF'
docs(sosialisasi-v2.2): shift data-slide Sl6-Sl34 untuk slot 7 slide baru

Pass 1 dari 6-pass renumber strategy.
Geser 29 slide existing (Sl6-Sl34) ke posisi baru, descending order avoid collision.
Slot kosong: Sl6 (G6), Sl24 (G1), Sl26 (G7), Sl27 (G2), Sl29 (G3), Sl33 (G4), Sl38 (G5).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Pass 2 — Shift `slide-badge` text descending (28 edits)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Tujuan:** Update badge text Sl6..Sl34 ke nomor + denominator baru.

- [ ] **Step 1: Batch edit badges via PowerShell**

```powershell
$path = 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html'
$content = Get-Content $path -Raw

# Same mapping as Pass 1, descending
$shifts = @(
    @{old=34; new=41}, @{old=33; new=40}, @{old=32; new=39},
    @{old=31; new=37}, @{old=30; new=36}, @{old=29; new=35},
    @{old=28; new=34}, @{old=27; new=32}, @{old=26; new=31},
    @{old=25; new=30}, @{old=24; new=28}, @{old=23; new=25},
    @{old=22; new=23}, @{old=21; new=22}, @{old=20; new=21},
    @{old=19; new=20}, @{old=18; new=19}, @{old=17; new=18},
    @{old=16; new=17}, @{old=15; new=16}, @{old=14; new=15},
    @{old=13; new=14}, @{old=12; new=13}, @{old=11; new=12},
    @{old=10; new=11}, @{old=9;  new=10}, @{old=8;  new=9},
    @{old=7;  new=8},  @{old=6;  new=7}
)

foreach ($s in $shifts) {
    $content = $content -replace "SLIDE $($s.old) / 34", "SLIDE TMP$($s.new) / 41"
}
$content = $content -replace 'SLIDE TMP', 'SLIDE '

Set-Content $path -Value $content -Encoding utf8 -NoNewline
```

**Catatan:** Sl34 (now Sl41) penutup tidak punya badge — verified. Sl1 cover tidak punya badge.

- [ ] **Step 2: Verify**

```bash
echo "=== Shifted badges /41 ===" && grep -cE 'SLIDE (7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|25|28|30|31|32|34|35|36|37|39|40) / 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== Remaining /34 (should be 4: Sl2..Sl5) ===" && grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- Shifted /41 badges = 28
- Remaining /34 = 4 (Sl2, Sl3, Sl4, Sl5 belum di-cascade)

- [ ] **Step 3: Commit Pass 2**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): shift slide-badge text Sl6-Sl34 ke nomor baru + denominator 41

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Pass 3 — Cascade denominator Sl2..Sl5

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Tujuan:** Update denominator 4 badge remaining (Sl2..Sl5, no number shift, denominator only).

- [ ] **Step 1: replace_all ` / 34</div>` → ` / 41</div>` (only 4 matches left)**

Use Edit tool dengan `replace_all: true`:
```
old_string:  / 34</div>
new_string:  / 41</div>
```

- [ ] **Step 2: Verify**

```bash
echo "=== /34 remaining (should be 0) ===" && grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html || echo "0 (grep exit 1 = no match, OK)"
echo "=== /41 total ===" && grep -c ' / 41</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== False positive sanity ===" && grep -cE 'height:30%|10:30|13:30|H-30|30 hari' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- ` / 34</div>` = 0
- ` / 41</div>` = 32 (28 shifted + 4 cascaded)
- False positives = 4 (height:30%, time strings — utuh, not touched)

- [ ] **Step 3: Commit Pass 3**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): cascade denominator Sl2-Sl5 dari /34 ke /41

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Pass 4 — JS `TOTAL` + slideCounter initial

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Read context around line 3748 + 3754**

Use Read tool, lines 3745-3760.

- [ ] **Step 2: Edit slideCounter initial text**

```
old_string:   <span class="slide-counter" id="slideCounter">1 / 34</span>
new_string:   <span class="slide-counter" id="slideCounter">1 / 41</span>
```

- [ ] **Step 3: Edit JS TOTAL**

```
old_string:     const TOTAL = 34;
new_string:     const TOTAL = 41;
```

- [ ] **Step 4: Verify**

```bash
grep -nE 'const TOTAL|id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `<span class="slide-counter" id="slideCounter">1 / 41</span>`
- `const TOTAL = 41;`

- [ ] **Step 5: Commit Pass 4**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): update JS TOTAL=41 + slideCounter initial 1/41

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Pass 5 — HTML section comments shifted (29 edits)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Tujuan:** Update 29 comment `<!-- ================= SLIDE N: TITLE =====` untuk slide shifted.

- [ ] **Step 1: Batch edit via PowerShell (sama mapping descending)**

```powershell
$path = 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html'
$content = Get-Content $path -Raw

$shifts = @(
    @{old=34; new=41}, @{old=33; new=40}, @{old=32; new=39},
    @{old=31; new=37}, @{old=30; new=36}, @{old=29; new=35},
    @{old=28; new=34}, @{old=27; new=32}, @{old=26; new=31},
    @{old=25; new=30}, @{old=24; new=28}, @{old=23; new=25},
    @{old=22; new=23}, @{old=21; new=22}, @{old=20; new=21},
    @{old=19; new=20}, @{old=18; new=19}, @{old=17; new=18},
    @{old=16; new=17}, @{old=15; new=16}, @{old=14; new=15},
    @{old=13; new=14}, @{old=12; new=13}, @{old=11; new=12},
    @{old=10; new=11}, @{old=9;  new=10}, @{old=8;  new=9},
    @{old=7;  new=8},  @{old=6;  new=7}
)

foreach ($s in $shifts) {
    # Pattern: <!-- ================= SLIDE N: ... =================
    $content = $content -replace "(<!-- ={5,} SLIDE )$($s.old)(:)", "`$1TMP$($s.new)`$2"
}
$content = $content -replace 'SLIDE TMP', 'SLIDE '

Set-Content $path -Value $content -Encoding utf8 -NoNewline
```

- [ ] **Step 2: Verify**

```bash
echo "=== Shifted comments ===" && grep -cE '<!-- ={5,} SLIDE (7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|25|28|30|31|32|34|35|36|37|39|40|41):' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== Remaining old N (should be 0 for 6..34) ===" && grep -cE '<!-- ={5,} SLIDE (6|24|26|27|29|33|38):' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- 29 shifted comments matched
- 0 matches for slot baru (Sl6, Sl24, Sl26, Sl27, Sl29, Sl33, Sl38 — akan diisi Pass 6)

- [ ] **Step 3: Commit Pass 5**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): renumber HTML section comments Sl6-Sl34 shifted slides

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Insert G6 Sl6 — CMP Guide / Help System

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Anchor:** Insert sebelum comment `<!-- SLIDE 7: AREA KERJA HC -->`.

- [ ] **Step 1: Locate insertion point**

```bash
grep -n '<!-- ================= SLIDE 7: AREA KERJA HC' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 1 match.

- [ ] **Step 2: Insert G6 block via Edit tool**

```
old_string:   <!-- ================= SLIDE 7: AREA KERJA HC ================= -->
new_string:   <!-- ================= SLIDE 6: CMP GUIDE / HELP SYSTEM ================= -->
  <div class="slide default-deco" data-slide="6">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 0 &mdash; PENGENALAN</p>
        <h1 class="slide-title">CMP Guide <span class="accent">Help System</span></h1>
        <p class="slide-subtitle">Panduan self-service per RoleGroup &mdash; accordion 6 modul + 3 PDF download</p>
      </div>
      <div class="slide-badge">SLIDE 6 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Home/Guide</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px;">
              <strong style="font-size:9pt;color:#0f172a;">&#128218; Panduan &amp; Bantuan</strong>
              <span><button class="mr-btn">&#128229; Coachee v1.2</button> <button class="mr-btn">&#128229; Admin v2.0</button> <button class="mr-btn secondary">&#128229; Umum</button></span>
            </div>
            <div style="display:flex;gap:3px;margin-bottom:8px;flex-wrap:wrap;">
              <span class="mr-filter-chip active" style="font-size:6.5pt;">Semua</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Coachee</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Coach</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Atasan</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Manager</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Admin &amp; HC</span>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:4px;margin-bottom:4px;">
              <div style="padding:6px 10px;background:#0f766e;color:#fff;font-size:7.5pt;font-weight:700;">&#9660; CMP &mdash; Competency Management (6 sub-item)</div>
              <div style="padding:4px 14px;font-size:7pt;color:#475569;">&#8627; Assessment Overview &middot; Sistem Assessment &middot; Pre/Post Test &middot; IDP Library &middot; Records Team &middot; Analytics Dashboard</div>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:4px;margin-bottom:4px;">
              <div style="padding:6px 10px;background:#f8fafc;font-size:7.5pt;color:#475569;">&#9654; CDP &mdash; Coaching Proton</div>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:4px;margin-bottom:4px;">
              <div style="padding:6px 10px;background:#f8fafc;font-size:7.5pt;color:#475569;">&#9654; Profile &amp; Akun</div>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:4px;">
              <div style="padding:6px 10px;background:#dc2626;color:#fff;font-size:7.5pt;font-weight:700;">&#9660; Admin Panel <span style="opacity:0.8;font-weight:400;">(visible: AdminHC group)</span></div>
              <div style="padding:4px 14px;font-size:7pt;color:#475569;">&#8627; Worker Mgmt &middot; Org &middot; KKJ &middot; CPDP &middot; Coach Mapping &middot; Workload &middot; Categories &middot; Renewal &middot; Monitoring</div>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#128218; 6 RoleGroup Filter</h4>
          <ul>
            <li><strong>All</strong> &mdash; visible semua role</li>
            <li><strong>Coachee</strong> &mdash; pekerja peserta assessment</li>
            <li><strong>Coach</strong> + Supervisor &mdash; pendamping</li>
            <li><strong>Atasan</strong> + Sr Supervisor + Section Head &mdash; reviewer</li>
            <li><strong>Manager</strong> + VP + Direktur &mdash; executive view</li>
            <li><strong>AdminHC</strong> &mdash; HC + Admin (manual operasional lengkap)</li>
          </ul>
          <h4 style="margin-top:8px;">&#128221; HC Use Case</h4>
          <ul>
            <li>User tanya cara X &rarr; arahin ke Guide modul Y (deflect to self-service)</li>
            <li>PDF Admin v2.0 = manual lengkap HC, cetak untuk offline</li>
            <li>Content update via <code>GuideContentProvider.cs</code> (developer)</li>
          </ul>
          <div class="mockup-tip"><strong>&#128161; Access:</strong> Navbar top-right Profile dropdown &rarr; "Panduan" (semua role).</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Sistem panduan self-service per RoleGroup. HC arahin user ke Guide sebelum jawab manual. 3 PDF download untuk reference offline.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Bab 2 Help System &amp; Guide</p>
  </div>

  <!-- ================= SLIDE 7: AREA KERJA HC ================= -->
```

- [ ] **Step 3: Verify**

```bash
grep -nE 'data-slide="6"|SLIDE 6 / 41|SLIDE 6: CMP GUIDE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 3 matches (data-slide, badge, comment) + total `data-slide=` = 36.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl6 G6 CMP Guide / Help System (Bagian 0)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Insert G1 Sl24 — Organization Management

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

**Anchor:** Insert sebelum `<!-- SLIDE 25: MANAJEMEN PEKERJA -->` (Sl25 setelah shift, was Sl23).

- [ ] **Step 1: Locate**

```bash
grep -n '<!-- ================= SLIDE 25: MANAJEMEN PEKERJA' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

- [ ] **Step 2: Insert G1 block**

```
old_string:   <!-- ================= SLIDE 25: MANAJEMEN PEKERJA ================= -->
new_string:   <!-- ================= SLIDE 24: ORGANIZATION MANAGEMENT ================= -->
  <div class="slide default-deco" data-slide="24">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Organization <span class="accent">Management</span></h1>
        <p class="slide-subtitle">Hierarki Bagian &rarr; Unit &rarr; Section &middot; CRUD master struktur organisasi</p>
      </div>
      <div class="slide-badge">SLIDE 24 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/ManageOrganization</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px;">
              <strong style="font-size:8.5pt;color:#0f172a;">&#127970; Struktur Organisasi (3-level)</strong>
              <button class="mr-btn">+ Tambah Node</button>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:6px;padding:8px;background:#fff;font-size:7.5pt;line-height:1.8;">
              <div><span style="color:#0d6efd;">&#9660;</span> <strong>Operations</strong> <span style="background:#fef3c7;color:#b45309;padding:1px 6px;border-radius:8px;font-size:6.5pt;">3 unit</span> <span style="float:right;color:#94a3b8;">&#9999; Edit &middot; &#128465;</span></div>
              <div style="padding-left:24px;border-left:2px dashed #667eea;margin-left:8px;">
                <div><span style="color:#667eea;">&#9660;</span> Alkylation <span style="background:#fef3c7;color:#b45309;padding:1px 6px;border-radius:8px;font-size:6.5pt;">2 section</span></div>
                <div style="padding-left:24px;border-left:2px dashed #0dcaf0;margin-left:8px;">
                  <div><span style="color:#0dcaf0;">&#9679;</span> Section A</div>
                  <div><span style="color:#0dcaf0;">&#9679;</span> Section B</div>
                </div>
                <div><span style="color:#667eea;">&#9654;</span> RFCC</div>
                <div><span style="color:#667eea;">&#9654;</span> NHT</div>
              </div>
              <div><span style="color:#0d6efd;">&#9654;</span> <strong>HSSE</strong></div>
              <div><span style="color:#0d6efd;">&#9654;</span> <strong>Maintenance</strong></div>
            </div>
            <div style="background:#f0fdfa;border:1px dashed #14b8a6;border-radius:4px;padding:6px;margin-top:6px;font-size:7pt;">
              <strong style="color:#0f766e;">Add Node Form:</strong> Parent (dropdown) &middot; Nama &middot; Kode &middot; Sort &middot; Aktif
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#127970; 3-Level Hierarchy</h4>
          <ul>
            <li><strong>Bagian</strong> (root) &mdash; departemen besar (Operations, HSSE, Maintenance)</li>
            <li><strong>Unit</strong> (child) &mdash; sub-departemen (Alkylation, RFCC, NHT)</li>
            <li><strong>Section</strong> (grandchild) &mdash; team/shift granular</li>
          </ul>
          <h4 style="margin-top:8px;">&#128279; Cross-link</h4>
          <ul>
            <li>Worker Mgmt (Sl25) pakai dropdown dari sini</li>
            <li>Coach Mapping (Sl28) filter section</li>
            <li>KKJ Files (Sl27) per Bagian</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Cascade delete:</strong> Hapus Bagian = hapus semua Unit + Section di bawahnya. Pakai Nonaktif dulu.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Master data struktur organisasi 3-level. Drive dropdown unit/bagian di Worker Mgmt + Coach Mapping + Filter Monitoring.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.1 Organization</p>
  </div>

  <!-- ================= SLIDE 25: MANAJEMEN PEKERJA ================= -->
```

- [ ] **Step 3: Verify + commit**

```bash
grep -nE 'data-slide="24"|SLIDE 24 / 41|SLIDE 24: ORGANIZATION' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl24 G1 Organization Management

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total `data-slide=` = 37.

---

## Task 9: Insert G7 Sl26 — Onboarding Pekerja Baru E2E

**Anchor:** Insert sebelum `<!-- SLIDE 27: ASSESSMENT MONITORING -->` (was — wait, mapping cek).

**Recompute anchor:** Sl25 = Manajemen Pekerja (was Sl23). Sl26 NEW = Onboarding. Sl27 NEW = G2 KKJ+CPDP. Sl28 = Coach Mapping (was Sl24). 

Setelah Pass 5, current state Sl25 Manajemen Pekerja diikuti langsung Sl28 Coach Mapping (slot Sl26 + Sl27 kosong). Insert G7 di Sl26 berarti insert antara Sl25 Manajemen Pekerja dan Sl28 Coach Mapping. Anchor = `<!-- SLIDE 28: COACH-COACHEE MAPPING -->`.

- [ ] **Step 1: Locate**

```bash
grep -n '<!-- ================= SLIDE 28: COACH-COACHEE MAPPING' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

- [ ] **Step 2: Insert G7 block**

```
old_string:   <!-- ================= SLIDE 28: COACH-COACHEE MAPPING ================= -->
new_string:   <!-- ================= SLIDE 26: ONBOARDING PEKERJA BARU E2E ================= -->
  <div class="slide default-deco" data-slide="26">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Onboarding <span class="accent">Pekerja Baru</span> &mdash; E2E</h1>
        <p class="slide-subtitle">Import Excel &rarr; Validate &rarr; Assign Role &rarr; First Login</p>
      </div>
      <div class="slide-badge">SLIDE 26 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/ImportWorkers</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:6px;margin-bottom:8px;">
              <div style="border:2px solid #0d6efd;border-radius:6px;padding:8px;background:#eff6ff;text-align:center;">
                <div style="font-size:18pt;">&#128229;</div>
                <div style="font-size:7pt;font-weight:700;color:#0d6efd;">1. Download Template</div>
                <div style="font-size:6pt;color:#64748b;">Excel 6 kolom</div>
              </div>
              <div style="border:2px solid #f59e0b;border-radius:6px;padding:8px;background:#fffbeb;text-align:center;">
                <div style="font-size:18pt;">&#128228;</div>
                <div style="font-size:7pt;font-weight:700;color:#b45309;">2. Upload + Validate</div>
                <div style="font-size:6pt;color:#64748b;">Cek NIP + email</div>
              </div>
              <div style="border:2px solid #7c3aed;border-radius:6px;padding:8px;background:#faf5ff;text-align:center;">
                <div style="font-size:18pt;">&#128269;</div>
                <div style="font-size:7pt;font-weight:700;color:#7c3aed;">3. Preview Result</div>
                <div style="font-size:6pt;color:#64748b;">Success/Error/Dup</div>
              </div>
              <div style="border:2px solid #16a34a;border-radius:6px;padding:8px;background:#f0fdf4;text-align:center;">
                <div style="font-size:18pt;">&#9989;</div>
                <div style="font-size:7pt;font-weight:700;color:#16a34a;">4. Assign Role</div>
                <div style="font-size:6pt;color:#64748b;">Bulk via Worker</div>
              </div>
            </div>
            <div style="border:1px solid #cbd5e1;border-radius:4px;padding:6px;background:#f8fafc;font-size:7pt;">
              <strong style="color:#0f172a;">Template Excel kolom:</strong> NIP &middot; Nama &middot; Email &middot; Bagian &middot; Unit &middot; Role
            </div>
            <div style="margin-top:6px;">
              <table class="mr-table" style="font-size:7pt;">
                <thead><tr><th>Baris</th><th>Status</th><th>Detail</th></tr></thead>
                <tbody>
                  <tr><td>1-15</td><td><span class="mr-badge-pill mr-badge-green">Created</span></td><td>15 pekerja baru</td></tr>
                  <tr><td>16</td><td><span class="mr-badge-pill mr-badge-orange">Duplicate</span></td><td>NIP 754201 sudah ada</td></tr>
                  <tr><td>17</td><td><span class="mr-badge-pill" style="background:#dc2626;color:#fff;">Error</span></td><td>Email invalid</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#128640; Onboarding Lifecycle</h4>
          <ul>
            <li><strong>Step 1</strong> Download template <code>.xlsx</code> dari ImportWorkers UI</li>
            <li><strong>Step 2</strong> Isi data + upload &mdash; server validate NIP unik, email valid, role exist</li>
            <li><strong>Step 3</strong> Review preview &mdash; fix error di Excel + re-upload, atau partial import</li>
            <li><strong>Step 4</strong> Bulk assign role via Worker Mgmt filter "Belum ada role"</li>
          </ul>
          <h4 style="margin-top:8px;">&#128274; First Login Flow</h4>
          <ul>
            <li>Pekerja terima email link reset password (ASP.NET Identity token)</li>
            <li>Klik link &rarr; set password baru &rarr; masuk Portal</li>
            <li>Token expire 24 jam &mdash; expired? request ulang via "Lupa Password?" di login</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Email salah:</strong> Import success tapi email tidak nyampe &mdash; verify kolom email sebelum upload.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> E2E onboarding pekerja baru &mdash; dari import Excel sampai first login. 4 step, ~5 menit per batch 20 pekerja.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.2 Worker Import + First Login</p>
  </div>

  <!-- ================= SLIDE 28: COACH-COACHEE MAPPING ================= -->
```

Wait — anchor mismatch. Sl27 G2 belum di-insert. Saat ini setelah Pass 5, transition dari Sl25 langsung ke Sl28 (slot Sl26 + Sl27 kosong). Insert G7 di slot 26, lalu insert G2 di slot 27 di Task 10.

Anchor `<!-- SLIDE 28: COACH-COACHEE MAPPING -->` betul kalau insert G7 alone (G2 belum). Tapi setelah G7 insert, anchor masih ada (G7 nempel di atas comment Sl28). Lanjut G2 (Task 10) pakai anchor sama.

- [ ] **Step 3: Verify + commit**

```bash
grep -nE 'data-slide="26"|SLIDE 26 / 41|SLIDE 26: ONBOARDING' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl26 G7 Onboarding Pekerja Baru E2E

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total = 38.

---

## Task 10: Insert G2 Sl27 — KKJ Files + CPDP Sync

**Anchor:** sama dengan Task 9, `<!-- SLIDE 28: COACH-COACHEE MAPPING -->`. G2 insert SEBELUM anchor + SESUDAH G7 (slot 27).

- [ ] **Step 1: Insert G2 block**

```
old_string:   <!-- ================= SLIDE 28: COACH-COACHEE MAPPING ================= -->
new_string:   <!-- ================= SLIDE 27: KKJ FILES + CPDP SYNC ================= -->
  <div class="slide default-deco" data-slide="27">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">KKJ Files + <span class="accent">CPDP Sync</span></h1>
        <p class="slide-subtitle">Master kompetensi (KKJ) + sync data eksternal (CPDP)</p>
      </div>
      <div class="slide-badge">SLIDE 27 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">Admin/KkjUpload + Admin/CpdpUpload (2 modul)</span>
          </div>
          <div class="mockup-recreated">
            <div style="border:1px solid #cbd5e1;border-radius:6px;padding:8px;background:#fff;margin-bottom:6px;">
              <div style="font-size:8pt;font-weight:700;color:#0f766e;margin-bottom:4px;">&#128203; KKJ Files &mdash; /Admin/KkjUpload</div>
              <div style="display:flex;gap:3px;margin-bottom:4px;">
                <span class="mr-tab active" style="font-size:6.5pt;">Upload</span>
                <span class="mr-tab" style="font-size:6.5pt;">Matrix</span>
                <span class="mr-tab" style="font-size:6.5pt;">History</span>
              </div>
              <div style="font-size:6.5pt;color:#475569;">Pilih Bagian &#9662; &middot; Upload .xlsx</div>
              <table class="mr-table" style="font-size:6pt;margin-top:4px;"><thead><tr><th>Jabatan</th><th>LOTO</th><th>Hot Work</th><th>ESD</th></tr></thead><tbody>
                <tr><td>Operator T1</td><td>&#9989;</td><td>&#9989;</td><td>&#9745;</td></tr>
                <tr><td>Panelman</td><td>&#9989;</td><td>&#9745;</td><td>&#9989;</td></tr>
              </tbody></table>
            </div>
            <div style="border:1px solid #cbd5e1;border-radius:6px;padding:8px;background:#fff;">
              <div style="font-size:8pt;font-weight:700;color:#7c3aed;margin-bottom:4px;">&#128229; CPDP Sync &mdash; /Admin/CpdpUpload</div>
              <div style="background:#faf5ff;border:1px dashed #7c3aed;border-radius:4px;padding:8px;font-size:7pt;color:#7c3aed;text-align:center;">&#128228; Drop file CPDP .xlsx di sini atau klik browse</div>
              <table class="mr-table" style="font-size:6pt;margin-top:4px;"><thead><tr><th>Tanggal</th><th>File</th><th>Status</th></tr></thead><tbody>
                <tr><td>2026-05-20</td><td>cpdp-may.xlsx</td><td><span class="mr-badge-pill mr-badge-green">Success</span></td></tr>
                <tr><td>2026-05-13</td><td>cpdp-may-w2.xlsx</td><td><span class="mr-badge-pill mr-badge-green">Success</span></td></tr>
              </tbody></table>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#128202; KKJ vs CPDP &mdash; Difference</h4>
          <table class="mr-table" style="font-size:7pt;">
            <thead><tr><th>Aspek</th><th>KKJ Files</th><th>CPDP Sync</th></tr></thead>
            <tbody>
              <tr><td><strong>Isi</strong></td><td>Matrix kompetensi per jabatan</td><td>Data pekerja eksternal (HR korporat)</td></tr>
              <tr><td><strong>Source</strong></td><td>Manual Excel HC</td><td>Sistem HR korporat</td></tr>
              <tr><td><strong>Frekuensi</strong></td><td>Per perubahan jabatan</td><td>Weekly/monthly</td></tr>
              <tr><td><strong>Impact</strong></td><td>KKJ assessment requirement</td><td>Sync data pekerja terbaru</td></tr>
            </tbody>
          </table>
          <div class="mockup-tip" style="margin-top:6px;"><strong>&#128161; CPDP frequency:</strong> cek History sebelum sync ulang &mdash; hindari duplicate. KKJ Matrix update saat ada jabatan baru.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> 2 master data input untuk drive assessment. KKJ = kompetensi per jabatan. CPDP = sync pekerja eksternal.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.3 KKJ + CPDP Sync</p>
  </div>

  <!-- ================= SLIDE 28: COACH-COACHEE MAPPING ================= -->
```

- [ ] **Step 2: Verify + commit**

```bash
grep -nE 'data-slide="27"|SLIDE 27 / 41|SLIDE 27: KKJ FILES' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl27 G2 KKJ Files + CPDP Sync

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total = 39.

---

## Task 11: Insert G3 Sl29 — Coach Workload

**Anchor:** `<!-- SLIDE 30: SILABUS + GUIDANCE FILES -->` (Sl30 was Sl25 Silabus). G3 insert antara Sl28 Coach Mapping dan Sl30 Silabus. Slot 29 kosong, anchor sebelum Sl30.

- [ ] **Step 1: Insert G3 block**

```
old_string:   <!-- ================= SLIDE 30: SILABUS + GUIDANCE FILES ================= -->
new_string:   <!-- ================= SLIDE 29: COACH WORKLOAD ================= -->
  <div class="slide default-deco" data-slide="29">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Coach <span class="accent">Workload</span> &mdash; Distribusi &amp; Penyeimbangan</h1>
        <p class="slide-subtitle">Tabel beban coach + chart + warna threshold + rekomendasi redistribusi</p>
      </div>
      <div class="slide-badge">SLIDE 29 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/CoachWorkload</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;gap:6px;margin-bottom:6px;align-items:center;">
              <span style="font-size:7pt;color:#475569;">Section:</span>
              <span class="mr-filter-chip" style="font-size:7pt;">Alkylation &#9662;</span>
              <button class="mr-btn">Apply</button>
            </div>
            <div style="background:linear-gradient(to top, #14b8a6 50%, #fef3c7 70%, #dc2626 90%);border-radius:4px;padding:6px;font-size:6.5pt;color:#fff;text-align:center;margin-bottom:6px;height:32px;display:flex;align-items:center;justify-content:center;">
              &#128202; Bar Chart: Coach &times; Jumlah Coachee (canvas)
            </div>
            <table class="mr-table">
              <thead><tr><th>Nama Coach</th><th>Section</th><th>Jumlah</th><th>Status</th></tr></thead>
              <tbody>
                <tr><td>Andi P.</td><td>Alkylation</td><td><strong>8</strong></td><td><span class="mr-badge-pill" style="background:#dc2626;color:#fff;">&#128308; Overload</span></td></tr>
                <tr><td>Budi S.</td><td>Alkylation</td><td>5</td><td><span class="mr-badge-pill mr-badge-green">&#128994; Normal</span></td></tr>
                <tr><td>Citra R.</td><td>RFCC</td><td>2</td><td><span class="mr-badge-pill mr-badge-orange">&#128993; Under</span></td></tr>
                <tr><td>Dimas H.</td><td>NHT</td><td>6</td><td><span class="mr-badge-pill mr-badge-green">&#128994; Normal</span></td></tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#9878; Workload Balance Threshold</h4>
          <ul>
            <li><span class="mr-badge-pill" style="background:#dc2626;color:#fff;">&#128308; Overload &gt;7</span> &mdash; pertimbangkan redistribusi ke coach Under</li>
            <li><span class="mr-badge-pill mr-badge-green">&#128994; Normal 3-7</span> &mdash; sweet spot, biarkan</li>
            <li><span class="mr-badge-pill mr-badge-orange">&#128993; Under &lt;3</span> &mdash; bisa terima coachee tambahan</li>
          </ul>
          <h4 style="margin-top:8px;">&#128279; Cross-link</h4>
          <ul>
            <li>Weekly task (Sl40 Quick Ref) &mdash; cek setiap Senin</li>
            <li>Redistribusi via Coach Mapping (Sl28) edit assignment</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Empty state:</strong> "Belum ada mapping aktif" &rarr; arahin ke Coach Mapping dulu untuk bikin mapping.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Real-time view distribusi coach-coachee. Identifikasi over/under capacity. Trigger redistribusi via Coach Mapping.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.4 Coach Workload Balance</p>
  </div>

  <!-- ================= SLIDE 30: SILABUS + GUIDANCE FILES ================= -->
```

- [ ] **Step 2: Verify + commit**

```bash
grep -nE 'data-slide="29"|SLIDE 29 / 41|SLIDE 29: COACH WORKLOAD' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl29 G3 Coach Workload

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total = 40.

---

## Task 12: Insert G4 Sl33 — Categories CRUD

**Anchor:** `<!-- SLIDE 34: CREATE ASSESSMENT - WIZARD OVERVIEW -->` (Sl34 was Sl28). G4 insert antara Sl32 Override KKJ dan Sl34 Create Asm. Slot 33 kosong.

- [ ] **Step 1: Insert G4 block**

```
old_string:   <!-- ================= SLIDE 34: CREATE ASSESSMENT - WIZARD OVERVIEW ================= -->
new_string:   <!-- ================= SLIDE 33: CATEGORIES CRUD ================= -->
  <div class="slide default-deco" data-slide="33">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Categories <span class="accent">CRUD</span> &mdash; Master Kategori Assessment</h1>
        <p class="slide-subtitle">Parent-child hierarchy &middot; Default passing % &middot; Signatory user binding</p>
      </div>
      <div class="slide-badge">SLIDE 33 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/ManageCategories</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
              <strong style="font-size:8.5pt;color:#0f172a;">&#127991; Kategori Assessment</strong>
              <span><button class="mr-btn">&#128229; Export &#9662;</button> <button class="mr-btn">+ Tambah Kategori</button></span>
            </div>
            <table class="mr-table">
              <thead><tr><th>Nama</th><th>Pass %</th><th>Sort</th><th>Signatory</th><th>Aksi</th></tr></thead>
              <tbody>
                <tr><td><strong>HSSE</strong> <span style="font-size:6pt;color:#94a3b8;">(parent)</span></td><td>75%</td><td>1</td><td>Manager HSSE</td><td>&#9999; &#128465;</td></tr>
                <tr><td><strong>Operations</strong> <span style="font-size:6pt;color:#94a3b8;">(parent)</span></td><td>70%</td><td>2</td><td>Manager Ops</td><td>&#9999; &#128465;</td></tr>
                <tr style="background:#f8fafc;"><td style="padding-left:20px;">&#8627; Alkylation</td><td>70%</td><td>1</td><td>&mdash;</td><td>&#9999; &#128465;</td></tr>
                <tr style="background:#f8fafc;"><td style="padding-left:20px;">&#8627; RFCC</td><td>70%</td><td>2</td><td>&mdash;</td><td>&#9999; &#128465;</td></tr>
                <tr style="background:#f8fafc;"><td style="padding-left:20px;">&#8627; NHT</td><td>70%</td><td>3</td><td>&mdash;</td><td>&#9999; &#128465;</td></tr>
                <tr><td><strong>OJT</strong> <span style="font-size:6pt;color:#94a3b8;">(parent)</span></td><td>80%</td><td>3</td><td>Sr Supervisor</td><td>&#9999; &#128465;</td></tr>
              </tbody>
            </table>
            <div style="background:#f0fdfa;border:1px dashed #14b8a6;border-radius:4px;padding:6px;margin-top:6px;font-size:7pt;color:#0f766e;">
              <strong>Add Form:</strong> Nama &middot; Parent (optional) &middot; Pass % &middot; Sort &middot; Signatory User &middot; Aktif
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#127991; Field per Kategori</h4>
          <ul>
            <li><strong>Default Passing %</strong> &mdash; auto-fill saat Create Assessment Step 1</li>
            <li><strong>Signatory User</strong> &mdash; penandatangan sertifikat (kalau lulus)</li>
            <li><strong>Sort Order</strong> &mdash; urutan tampil di dropdown Create Asm</li>
            <li><strong>Parent</strong> &mdash; optional, untuk hierarchy 2-level</li>
          </ul>
          <h4 style="margin-top:8px;">&#128279; Cross-link</h4>
          <ul>
            <li>Sl34 Create Asm Step 1 dropdown pakai data dari sini (optgroup parent-child)</li>
            <li>Sl12 (5 Kategori konsep) &rarr; UI tutorial di sini</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Cascade delete:</strong> Hapus parent = hapus semua child + assessment terkait. Pakai Nonaktif dulu.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Master kategori assessment (parent-child + signatory + passing %). Drive dropdown Create Assessment + signatory sertifikat.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.5 Categories CRUD</p>
  </div>

  <!-- ================= SLIDE 34: CREATE ASSESSMENT - WIZARD OVERVIEW ================= -->
```

- [ ] **Step 2: Verify + commit**

```bash
grep -nE 'data-slide="33"|SLIDE 33 / 41|SLIDE 33: CATEGORIES' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl33 G4 Categories CRUD

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total = 41.

---

## Task 13: Insert G5 Sl38 — Certificate Renewal UI

**Anchor:** `<!-- SLIDE 39: MAINTENANCE + AUDIT LOG -->` (Sl39 was Sl32). G5 insert antara Sl37 Monitor Actions dan Sl39 Maintenance.

- [ ] **Step 1: Insert G5 block**

```
old_string:   <!-- ================= SLIDE 39: MAINTENANCE + AUDIT LOG ================= -->
new_string:   <!-- ================= SLIDE 38: CERTIFICATE RENEWAL UI ================= -->
  <div class="slide default-deco" data-slide="38">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Certificate <span class="accent">Renewal</span> UI</h1>
        <p class="slide-subtitle">Filter sertifikat near-expired + bulk schedule renewal &middot; auto-create assessment</p>
      </div>
      <div class="slide-badge">SLIDE 38 / 41</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/RenewalCertificate</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:grid;grid-template-columns:repeat(3,1fr);gap:3px;margin-bottom:6px;">
              <span class="mr-filter-chip" style="font-size:6.5pt;">Bagian &#9662;</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Unit &#9662;</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Kategori &#9662;</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Sub-Kategori &#9662;</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Tipe &#9662;</span>
              <span class="mr-filter-chip" style="font-size:6.5pt;">Status &#9662;</span>
            </div>
            <table class="mr-table">
              <thead><tr><th>&#9745;</th><th>Pekerja</th><th>Sertifikat</th><th>Expired</th><th>Sisa</th><th>Status</th></tr></thead>
              <tbody>
                <tr style="background:#fef2f2;"><td>&#9745;</td><td>Widodo A.</td><td>LOTO</td><td>2026-06-15</td><td><strong style="color:#dc2626;">H-22 &#128308;</strong></td><td>Aktif</td></tr>
                <tr style="background:#fef3c7;"><td>&#9745;</td><td>Andi P.</td><td>Hot Work</td><td>2026-07-08</td><td><strong style="color:#f59e0b;">H-45 &#128993;</strong></td><td>Aktif</td></tr>
                <tr><td>&#9744;</td><td>Citra R.</td><td>Pump Op</td><td>2026-08-20</td><td><strong style="color:#16a34a;">H-88 &#128994;</strong></td><td>Aktif</td></tr>
                <tr><td>&#9744;</td><td>Budi S.</td><td>ESD</td><td>2026-09-12</td><td><strong style="color:#94a3b8;">H-111 &#9898;</strong></td><td>Aktif</td></tr>
              </tbody>
            </table>
            <div style="display:flex;justify-content:space-between;align-items:center;margin-top:6px;">
              <span style="font-size:7pt;color:#64748b;">2 selected</span>
              <button class="mr-btn" style="background:#0f766e;color:#fff;">&#128467; Bulk Schedule Renewal</button>
            </div>
            <div style="font-size:6pt;color:#64748b;margin-top:3px;">Legend: &#128308; &lt;30 hari &middot; &#128993; 30-60 &middot; &#128994; 60-90 &middot; &#9898; &gt;90</div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#9203; Renewal Workflow</h4>
          <ul>
            <li>Filter near-expired (6 dropdown chain: Bagian &rarr; Tipe)</li>
            <li>Bulk select pekerja yang due</li>
            <li>Klik <strong>Bulk Schedule Renewal</strong> &rarr; auto-create assessment (pre-fill kategori + peserta dari renewal source)</li>
          </ul>
          <h4 style="margin-top:8px;">&#128279; Cross-link</h4>
          <ul>
            <li>Sl22 Renewal Lifecycle (konsep) &rarr; UI tutorial di sini</li>
            <li>Sl34-35 Create Asm wizard auto-pre-fill kalau dari renewal</li>
            <li>Monthly task Sl40 Quick Ref &mdash; cek H-60 awal bulan</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Tidak campur tipe:</strong> Bulk renew Assessment + Training campuran ditolak. Filter Tipe dulu untuk pisahkan.</div>
          <div class="mockup-tip"><strong>&#128161; Auto-notify:</strong> Pekerja dapat email otomatis H-30/14/7 sebelum expired (cron daily).</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Workflow renewal sertifikat near-expired. Bulk schedule untuk efisiensi. Linked Renewal Lifecycle Sl22 (konsep).</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; &sect;5.6 Certificate Renewal</p>
  </div>

  <!-- ================= SLIDE 39: MAINTENANCE + AUDIT LOG ================= -->
```

- [ ] **Step 2: Verify + commit**

```bash
grep -nE 'data-slide="38"|SLIDE 38 / 41|SLIDE 38: CERTIFICATE RENEWAL' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): insert Sl38 G5 Certificate Renewal UI

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Expected: total = 42 (41 slides + 1 JS).

---

## Task 14: I3 Edit Sl4 — Tambah Matriks Role × Menu

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` Sl4

**Tujuan:** Tambah tabel matrix di bawah stair existing.

- [ ] **Step 1: Locate Sl4 closing div**

```bash
grep -n 'SLIDE 4: STRUKTUR ROLE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -nE 'hc-callout|class="step s1"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html | head -10
```

Find Sl4 `<div class="hc-callout">` line (around 2007-2012 original, may shift). Then insert matrix below `</div>` callout, before `</div>` slide-body.

- [ ] **Step 2: Edit — replace hc-callout content with original + matrix**

Original hc-callout (verified line ~2007):
```html
<div class="hc-callout">
  <strong>Penjelasan</strong>
  <ul>
    <li>10 role dikelompokkan dalam 6 level otorisasi. HC berada di Level 2 dengan akses lintas section sebagai final reviewer.</li>
  </ul>
</div>
```

Replace dengan:
```
old_string:       <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>10 role dikelompokkan dalam 6 level otorisasi. HC berada di Level 2 dengan akses lintas section sebagai final reviewer.</li>
        </ul>
      </div>
new_string:       <div class="hc-callout">
        <strong>Penjelasan</strong>
        <ul>
          <li>10 role dikelompokkan dalam 6 level otorisasi. HC berada di Level 2 dengan akses lintas section sebagai final reviewer.</li>
        </ul>
      </div>
      <div style="margin-top:14px;">
        <h4 style="margin:0 0 6px;font-size:9.5pt;color:#0f766e;">&#128202; Matriks Role &times; Menu Visibility</h4>
        <table class="mr-table" style="font-size:7pt;">
          <thead><tr><th>Level</th><th>Role</th><th>CMP</th><th>CDP</th><th>Admin Panel</th><th>Profile</th><th>Guide RoleGroup</th></tr></thead>
          <tbody>
            <tr><td><strong>L1</strong></td><td>Admin</td><td>&#9989; All</td><td>&#9989; All</td><td>&#9989; All</td><td>&#9989;</td><td>AdminHC</td></tr>
            <tr><td><strong>L2</strong></td><td>HC</td><td>&#9989; All</td><td>&#9989; All</td><td>&#9989; All (14 menu)</td><td>&#9989;</td><td>AdminHC</td></tr>
            <tr><td><strong>L3</strong></td><td>Direktur / VP / Manager</td><td>&#9989; Dashboard</td><td>&#9989; Dashboard</td><td>&#10060;</td><td>&#9989;</td><td>Manager</td></tr>
            <tr><td><strong>L4</strong></td><td>Section Head / Sr Sup</td><td>&#9989; Section</td><td>&#9989; Section</td><td>&#10060;</td><td>&#9989;</td><td>Atasan</td></tr>
            <tr><td><strong>L5</strong></td><td>Coach / Supervisor</td><td>&#9989; Coachee</td><td>&#9989; Mapping</td><td>&#10060;</td><td>&#9989;</td><td>Coach</td></tr>
            <tr><td><strong>L6</strong></td><td>Coachee</td><td>&#9989; Self Asm</td><td>&#9989; Self IDP</td><td>&#10060;</td><td>&#9989;</td><td>Coachee</td></tr>
          </tbody>
        </table>
        <div style="font-size:7pt;color:#64748b;margin-top:4px;font-style:italic;">&#128279; Cross-ref: Guide RoleGroup (Sl6) &mdash; 6 group filter selaras matriks ini.</div>
      </div>
```

- [ ] **Step 3: Verify + commit**

```bash
grep -n 'Matriks Role' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): I3 Sl4 tambah matriks role x menu visibility (6x5)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 15: I2 Edit Sl8 — Tambah Tabel Tipe Notif

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` Sl8 (was Sl7 Alur Harian + Notifikasi)

**Tujuan:** Tambah tabel 10 tipe notif setelah mockup bell dropdown existing.

- [ ] **Step 1: Locate Sl8 panduan-ref line**

```bash
grep -n 'SLIDE 8: ALUR HARIAN' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -n 'Bab 6 Notifikasi' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

- [ ] **Step 2: Insert tabel sebelum `<p class="panduan-ref">`**

```
old_string:     <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Bab 6 Notifikasi &amp; Workflow</p>
new_string:     <div style="margin:10px 16px 0;">
      <h4 style="margin:0 0 6px;font-size:9pt;color:#0f766e;">&#128276; Tabel Tipe Notif Lengkap (10 tipe)</h4>
      <table class="mr-table" style="font-size:7pt;">
        <thead><tr><th>Kategori</th><th>Tipe</th><th>Trigger</th><th>Penerima</th></tr></thead>
        <tbody>
          <tr><td rowspan="3" style="background:#eff6ff;font-weight:700;">Evidence</td><td>&#9989; Approved by Section Head</td><td>Atasan approve</td><td>HC (final reviewer)</td></tr>
          <tr><td>&#10060; Rejected &mdash; Chain Reset</td><td>Atasan reject</td><td>HC + Coach + Coachee</td></tr>
          <tr><td>&#128228; Submitted by Coach</td><td>Coach submit deliverable</td><td>Atasan (next reviewer)</td></tr>
          <tr><td rowspan="3" style="background:#fef3c7;font-weight:700;">Renewal</td><td>&#9203; H-90 Warning</td><td>Auto cron daily</td><td>HC + Pekerja</td></tr>
          <tr><td>&#9888; H-30 Critical</td><td>Auto cron daily</td><td>HC + Pekerja + Atasan</td></tr>
          <tr><td>&#128680; H-7 Last Call</td><td>Auto cron daily</td><td>HC + Atasan eskalasi</td></tr>
          <tr><td rowspan="2" style="background:#f0fdfa;font-weight:700;">Assessment</td><td>&#127919; Created &amp; Published</td><td>HC publish</td><td>Peserta + Coach</td></tr>
          <tr><td>&#10060; Force-Close by HC</td><td>HC akhiri ujian (Sl37)</td><td>Peserta</td></tr>
          <tr><td rowspan="2" style="background:#fef2f2;font-weight:700;">System</td><td>&#9881; Override KKJ Logged</td><td>HC override manual (Sl32)</td><td>Admin (audit)</td></tr>
          <tr><td>&#128295; Maintenance Mode On</td><td>Admin enable</td><td>All users</td></tr>
        </tbody>
      </table>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Bab 6 Notifikasi &amp; Workflow</p>
```

- [ ] **Step 3: Verify + commit**

```bash
grep -n 'Tabel Tipe Notif Lengkap' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): I2 Sl8 tambah tabel tipe notif lengkap (10 tipe)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 16: I6 Edit Sl40 — Quick Ref Expand

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` Sl40 (was Sl33 Quick Reference)

**Tujuan:** Tambah keyboard shortcut + URL cheatsheet + ref-card Guide + password reset tip.

- [ ] **Step 1: Locate Sl40 ref-grid + tip-bar**

```bash
grep -n 'SLIDE 40: TUGAS CEPAT' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -n 'Self-service first' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

- [ ] **Step 2: Add CMP Guide ref-card di ref-grid existing**

```
old_string:             <div class="ref-card">
              <div class="ref-icon">&#127942;</div>
              <div class="ref-title">Sosialisasi Aplikasi Umum</div>
              <div class="ref-desc">Slide deck overview semua role (22 slide)</div>
              <a href="Sosialisasi-Aplikasi-PortalHC-KPB.html" target="_blank">Buka &rarr;</a>
            </div>
          </div>
        </div>
new_string:             <div class="ref-card">
              <div class="ref-icon">&#127942;</div>
              <div class="ref-title">Sosialisasi Aplikasi Umum</div>
              <div class="ref-desc">Slide deck overview semua role (22 slide)</div>
              <a href="Sosialisasi-Aplikasi-PortalHC-KPB.html" target="_blank">Buka &rarr;</a>
            </div>
            <div class="ref-card">
              <div class="ref-icon">&#128218;</div>
              <div class="ref-title">CMP Guide (Help System)</div>
              <div class="ref-desc">Help system in-app per RoleGroup (Sl6)</div>
              <a href="/KPB-PortalHC/Home/Guide" target="_blank">Buka &rarr;</a>
            </div>
          </div>
        </div>
        <div style="margin-top:12px;display:grid;grid-template-columns:1fr 1fr;gap:12px;">
          <div>
            <h4 style="margin:0 0 4px;font-size:9pt;color:#0f766e;">&#9000; Keyboard Shortcut Deck</h4>
            <table class="mr-table" style="font-size:7pt;">
              <thead><tr><th>Tombol</th><th>Fungsi</th></tr></thead>
              <tbody>
                <tr><td><code>&larr;</code> / <code>&rarr;</code></td><td>Slide prev/next</td></tr>
                <tr><td><code>Space</code> / <code>PageDown</code></td><td>Slide next</td></tr>
                <tr><td><code>PageUp</code></td><td>Slide prev</td></tr>
                <tr><td><code>Home</code></td><td>Slide 1 (Cover)</td></tr>
                <tr><td><code>End</code></td><td>Slide 41 (Terima Kasih)</td></tr>
                <tr><td><code>Esc</code></td><td>Exit fullscreen</td></tr>
              </tbody>
            </table>
          </div>
          <div>
            <h4 style="margin:0 0 4px;font-size:9pt;color:#0f766e;">&#127760; URL Cheatsheet HC</h4>
            <table class="mr-table" style="font-size:7pt;">
              <thead><tr><th>Route</th><th>Fungsi</th></tr></thead>
              <tbody>
                <tr><td><code>/Admin</code></td><td>Admin Panel landing</td></tr>
                <tr><td><code>/Admin/AssessmentMonitoring</code></td><td>Real-time monitor</td></tr>
                <tr><td><code>/Admin/RenewalCertificate</code></td><td>Renewal queue</td></tr>
                <tr><td><code>/Admin/CoachWorkload</code></td><td>Workload balance</td></tr>
                <tr><td><code>/Home/Guide</code></td><td>Help system</td></tr>
                <tr><td><code>/Admin/AuditLog</code></td><td>Investigation</td></tr>
              </tbody>
            </table>
          </div>
        </div>
```

- [ ] **Step 3: Update tip-bar tambah password reset tip**

```
old_string:       <div class="tip-bar" style="margin-top:12px;">
        <strong>&#128161; Self-service first:</strong> 80% pekerjaan HC harian = approve evidence + monitor compliance + schedule renewal. Untuk hal lain, cek Panduan Operasional dulu sebelum tanya senior.
      </div>
new_string:       <div class="tip-bar" style="margin-top:12px;">
        <strong>&#128161; Self-service first:</strong> 80% pekerjaan HC harian = approve evidence + monitor compliance + schedule renewal. Untuk hal lain, cek Panduan Operasional dulu sebelum tanya senior. <strong>&#128274; Password reset:</strong> user pakai "Lupa Password?" di login &mdash; HC tidak punya tool reset langsung.
      </div>
```

- [ ] **Step 4: Verify + commit**

```bash
grep -n 'Keyboard Shortcut Deck\|URL Cheatsheet HC\|Password reset:.*user pakai' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2.2): I6 Sl40 Quick Ref tambah keyboard + URL cheatsheet + Guide ref-card + password tip

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 17: Final verify + browser visual + tag

**Files:**
- Read: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Count verifikasi**

```bash
echo "=== data-slide (expect 42 = 41 slides + 1 JS) ===" && grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== slide-badge (expect 39 = Sl2..Sl40, Sl1+Sl41 no badge) ===" && grep -c 'class="slide-badge"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== /34 (expect 0) ===" && grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html || echo "0"
echo "=== /41 (expect 39) ===" && grep -c ' / 41</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
echo "=== TOTAL + slideCounter ===" && grep -nE 'const TOTAL|id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

- [ ] **Step 2: Sequential check 1..41**

```bash
for n in $(seq 1 41); do c=$(grep -c "data-slide=\"$n\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html); if [ "$c" != "1" ]; then echo "MISMATCH slide $n: count=$c"; fi; done; echo "Done."
```

Expected: hanya "Done." (no mismatch).

- [ ] **Step 3: Browser visual via Playwright**

Spawn local http server di port 8765 (background):
```bash
cd docs && python -m http.server 8765
```

Navigate via Playwright ke `http://localhost:8765/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`.

Verify:
- Counter `1 / 41` initial
- `End` key → `data-slide="41"` (Terima Kasih)
- Screenshot 7 slide baru (Sl6, Sl24, Sl26, Sl27, Sl29, Sl33, Sl38) — verify muat tanpa overflow
- Screenshot 3 slide edited (Sl4, Sl8, Sl40) — verify konten tambahan muat tanpa overflow
- Toggle dark mode — slide baru contrast OK

Kalau ada overflow → return ke task terkait untuk compress (font size 7pt → 6.5pt, gap 8px → 6px).

Stop http server setelah verify.

- [ ] **Step 4: Tag release**

```bash
git tag sosialisasi-internal-hc-v2.2 -m "Sosialisasi-Internal-Tim-HC v2.2: +7 slide gap (G1-G7) + 3 improvement (I2/I3/I6). Deck 34 -> 41 slide. Close gap 6 Admin menu UI tutorial + CMP Guide + Onboarding E2E."
git log --oneline -20
```

- [ ] **Step 5: No extra commit** (Task 17 = verification + tag only)

---

## Catatan Implementasi

### Risiko Edit Tool

- Task 4 `replace_all ` / 34</div>` aman karena Pass 2 sudah handle shifted. False positive sanity check di Step 2.
- Task 14-16 (edit existing slides) butuh Read tool dulu per hook + unique-string context. Sl4 hc-callout muncul beberapa slide — make sure context unique (gunakan lines around).

### Density Limit

Setiap slide baru muat 1280×720. Compress kalau overflow (kurangi font + gap).

### Branch Awareness

Sebelum commit Task 7+, **verify** `git status --short docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` hanya menampilkan Sosialisasi file. Kalau ada file lain ter-staged (mis. CMP Guide WIP), unstage dulu via `git restore --staged <file>`.

### Rollback

Per-task = 1 commit. Rollback via `git revert <commit-hash>` per-task atau `git reset --hard <pre-Task-1-commit>` semua.

---

## Self-Review Results

**Spec coverage:**
- ✅ G6 CMP Guide → Task 7
- ✅ G1 Organization → Task 8
- ✅ G7 Onboarding → Task 9
- ✅ G2 KKJ + CPDP → Task 10
- ✅ G3 Coach Workload → Task 11
- ✅ G4 Categories → Task 12
- ✅ G5 Cert Renewal → Task 13
- ✅ I3 Sl4 matrix → Task 14
- ✅ I2 Sl8 notif → Task 15
- ✅ I6 Sl40 expand → Task 16
- ✅ Renumber 6-pass → Task 2-6
- ✅ Verifikasi + tag → Task 17

**Placeholder scan:** None. All inline HTML lengkap.

**Type consistency:** Class names match spec — `slide-mockup-split`, `mockup-frame/bar/recreated`, `mockup-content/tip/warn`, `tip-bar`, `panduan-ref`, `mr-table`, `mr-tab`, `mr-tab-strip`, `mr-filter-chip`, `mr-btn`, `mr-badge-pill`, `mr-badge-green/orange`. PowerShell shifts mapping konsisten Pass 1/2/5.

---

**End of plan. Total 17 tasks. Estimated wall time: 60-90 menit (mostly PowerShell batch + Edit + verify + commit cycles).**
