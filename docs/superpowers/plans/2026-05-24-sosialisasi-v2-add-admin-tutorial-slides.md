# Sosialisasi v2 — Add 4 Admin Tutorial Slides Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tambah 4 slide tutorial Admin Panel (Manage Package Question, Create Assessment Wizard Overview, Create Assessment Field Detail, Monitoring Actions) ke `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`, ekspansi deck 30 → 34 slide.

**Architecture:** Edit in-place single HTML file. 4-pass strategi: (1) shift `data-slide` existing dari Sl26→Sl30 ke target baru (descending order avoid collision), (2) shift `slide-badge` text + cascade denominator update Sl2..Sl29 ke `/ 34`, (3) update HTML section comments shifted, (4) update JS `TOTAL` + `slideCounter` + insert 4 new slide HTML. Konsisten CSS class existing (`slide-mockup-split`, `mockup-frame`, `mr-table`, `mr-tab-strip`), HTML entity emoji (no Bootstrap Icons), no new CSS class.

**Tech Stack:** Static HTML + CSS + vanilla JS. Edit tool (replace exact strings). Browser visual verification.

**Spec reference:** `docs/superpowers/specs/2026-05-24-sosialisasi-v2-add-admin-tutorial-slides-design.md`

---

## Open Questions Assumed Default (User Boleh Override Sebelum Eksekusi)

Plan ini pakai default berikut. Kalau user override Open Question di spec, sesuaikan task content.

1. **Mockup data sample**: pakai nama real (Widodo, Andi, Citra, Budi) + NIP 754201..754204 — selaras Sl28 Audit Log existing (Widodo 754201 sudah disebut).
2. **Panduan ref §**: drop `§N` placeholder, ganti `Panduan Operasional HC — Bank Soal & Package` (without section number). Aman sampai panduan actual selesai.
3. **Renewal mode Sl28**: cukup di mockup-tip (no dedicated mini-card). Density limit 1280×720.
4. **Sl29 layout**: 3 mini-card horizontal (Step 2/3/4), pakai grid CSS inline.
5. **Tag release**: tag `sosialisasi-internal-hc-v2.1` setelah merge (Task 11).

---

## File Structure

**Modified (single file):**
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` — semua perubahan in-place
  - 4 new `<div class="slide default-deco">` blocks (Sl26, Sl28, Sl29, Sl31)
  - 5 `data-slide` shift (Sl26→27, Sl27→30, Sl28→32, Sl29→33, Sl30→34)
  - 28 `slide-badge` denominator update (Sl2..Sl29: `/ 30 → / 34`)
  - 5 shifted-slide badge text update
  - 5 HTML section comment update
  - 2 JS/UI literal update (`const TOTAL`, `slideCounter` initial)

**No new files. No CSS new class. No JS new function.**

---

## Task 1: Backup file + verifikasi state awal

**Files:**
- Read: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Snapshot baseline counts**

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'class="slide-badge"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'SLIDE [0-9]* / 30' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -n 'const TOTAL' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -n 'id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `data-slide=` count = `31` (30 slides + 1 JS selector match)
- `class="slide-badge"` count = `28` (Sl2..Sl29, exclude Sl1 cover + Sl30 penutup)
- `SLIDE [0-9]* / 30` count = `28`
- `const TOTAL = 30;` at line 3445
- `id="slideCounter"` at line 3439

Catat angka. Akan diverifikasi ulang di Task 11.

- [ ] **Step 2: Confirm working tree clean**

```bash
git status docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: clean (no uncommitted changes to file). Kalau dirty, commit/stash dulu.

- [ ] **Step 3: Confirm spec exists**

```bash
ls -la docs/superpowers/specs/2026-05-24-sosialisasi-v2-add-admin-tutorial-slides-design.md
```

Expected: file exists. Plan ini reference spec untuk content detail.

- [ ] **Step 4: Skip commit** (Task 1 = verification only, no file change)

---

## Task 2: Shift data-slide existing (Pass 1 — descending order)

**Tujuan**: Geser 5 slide existing ke posisi baru dulu, sebelum insert slide baru. Descending order avoid collision.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (5 attributes)

- [ ] **Step 1: Shift Sl30 Terima Kasih → Sl34**

Use Edit tool:
```
old_string: <div class="slide penutup" data-slide="30">
new_string: <div class="slide penutup" data-slide="34">
```

- [ ] **Step 2: Shift Sl29 Quick Reference → Sl33**

Use Edit tool:
```
old_string: <div class="slide default-deco" data-slide="29">
new_string: <div class="slide default-deco" data-slide="33">
```

- [ ] **Step 3: Shift Sl28 Maintenance → Sl32**

Use Edit tool:
```
old_string: <div class="slide default-deco" data-slide="28">
new_string: <div class="slide default-deco" data-slide="32">
```

- [ ] **Step 4: Shift Sl27 Monitoring → Sl30**

Use Edit tool:
```
old_string: <div class="slide default-deco" data-slide="27">
new_string: <div class="slide default-deco" data-slide="30">
```

- [ ] **Step 5: Shift Sl26 Override KKJ → Sl27**

Use Edit tool:
```
old_string: <div class="slide default-deco" data-slide="26">
new_string: <div class="slide default-deco" data-slide="27">
```

- [ ] **Step 6: Verify shifts**

```bash
grep -nE 'data-slide="(26|27|28|29|30|32|33|34)"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `data-slide="26"` — **0 matches** (slot kosong untuk Sl26 NEW)
- `data-slide="27"` — 1 match (Override KKJ, was Sl26)
- `data-slide="28"` — **0 matches** (slot kosong untuk Sl28 NEW)
- `data-slide="29"` — **0 matches** (slot kosong untuk Sl29 NEW)
- `data-slide="30"` — 1 match (Monitoring, was Sl27)
- `data-slide="32"` — 1 match (Maintenance, was Sl28)
- `data-slide="33"` — 1 match (Quick Ref, was Sl29)
- `data-slide="34"` — 1 match (Terima Kasih, was Sl30)

Selain itu: Sl1..Sl25 + JS selector = 26 matches lain di `data-slide=`. Total grep `data-slide=` = masih 31 (tidak berubah).

- [ ] **Step 7: Commit Pass 1**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "$(cat <<'EOF'
docs(sosialisasi-v2): shift data-slide Sl26-Sl30 untuk slot 4 slide baru

Pass 1 dari 4-pass renumber strategy:
- Sl30 Terima Kasih -> Sl34
- Sl29 Quick Reference -> Sl33
- Sl28 Maintenance -> Sl32
- Sl27 Monitoring -> Sl30
- Sl26 Override KKJ -> Sl27

Slot 26, 28, 29, 31 dikosongkan untuk slide tutorial Admin baru.
EOF
)"
```

---

## Task 3: Shift slide-badge text untuk shifted slides (Pass 2a)

**Tujuan**: Update badge text shifted slides ke nomor baru + denominator 34. Lakukan **descending** untuk avoid collision.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (5 badges)

- [ ] **Step 1: Update badge Sl30 Terima Kasih** — wait, Sl30 penutup TIDAK punya slide-badge. SKIP.

Verifikasi dengan:
```bash
grep -n 'SLIDE 30 / 30' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```
Expected: **0 matches**. Confirm skip.

- [ ] **Step 2: Update badge Sl29 Quick Reference (di slot baru Sl33)**

Use Edit tool:
```
old_string: <div class="slide-badge">SLIDE 29 / 30</div>
new_string: <div class="slide-badge">SLIDE 33 / 34</div>
```

- [ ] **Step 3: Update badge Sl28 Maintenance (di slot baru Sl32)**

Use Edit tool:
```
old_string: <div class="slide-badge">SLIDE 28 / 30</div>
new_string: <div class="slide-badge">SLIDE 32 / 34</div>
```

- [ ] **Step 4: Update badge Sl27 Monitoring (di slot baru Sl30)**

Use Edit tool:
```
old_string: <div class="slide-badge">SLIDE 27 / 30</div>
new_string: <div class="slide-badge">SLIDE 30 / 34</div>
```

- [ ] **Step 5: Update badge Sl26 Override KKJ (di slot baru Sl27)**

Use Edit tool:
```
old_string: <div class="slide-badge">SLIDE 26 / 30</div>
new_string: <div class="slide-badge">SLIDE 27 / 34</div>
```

- [ ] **Step 6: Verify shifted badges**

```bash
grep -nE 'SLIDE (27|30|32|33) / 34' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 4 matches (shifted slides). Sl34 Terima Kasih + Sl1 cover tidak punya badge.

- [ ] **Step 7: Commit Pass 2a**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): update slide-badge text untuk 4 shifted slides ke / 34"
```

---

## Task 4: Cascade denominator Sl2..Sl25 (Pass 2b)

**Tujuan**: Update denominator `/ 30 → / 34` untuk slide non-shifted (Sl2..Sl25). Slide ini posisi & nomor tidak berubah, hanya denominator.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (24 badges)

- [ ] **Step 1: Bulk replace denominator via replace_all**

Use Edit tool dengan `replace_all: true`:
```
old_string:  / 30</div>
new_string:  / 34</div>
```

**Catatan**: Pakai pattern dengan leading space ` / 30</div>` (note: space before slash) untuk hanya match slide-badge. Hindari match angka 30 dalam konten lain (CSS `30%`, time `10:30`, dll).

Actually, lebih aman dengan pattern lengkap. Cek dulu jumlah match dengan `grep -c`:

```bash
grep -c 'SLIDE [0-9]* / 30</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 24 (Sl2..Sl25, karena Sl26..Sl29 sudah shifted di Task 3).

Kalau bulk replace_all ` / 30</div>` masih risky, lakukan per slide individual via grep loop di Step 2.

- [ ] **Step 2: Alternative — per-slide loop (gunakan kalau Step 1 risky)**

```bash
for n in 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25; do
  echo "Slide $n:"
  grep -n "SLIDE $n / 30</div>" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
done
```

Lalu edit per slide via Edit tool, 24 calls. Atau pakai PowerShell single command:

```powershell
(Get-Content 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html' -Raw) -replace ' / 30</div>', ' / 34</div>' | Set-Content 'docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html' -Encoding utf8 -NoNewline
```

**Pilih Step 1 (Edit tool replace_all) dulu.** Kalau Edit tool reject karena tidak unique, fallback ke PowerShell single command.

- [ ] **Step 3: Verify denominator update**

```bash
grep -c ' / 30</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- ` / 30</div>` count = **0**
- ` / 34</div>` count = **28** (Sl2..Sl29 = 4 shifted from Task 3 + 24 dari Sl2..Sl25)

- [ ] **Step 4: Sanity check no false replacement**

```bash
grep -E 'height:30%|10:30|11:30|13:30|09:30|14:30|15:30|H-30|30 hari' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html | head -20
```

Expected: semua match harus utuh (height:30%, 10:30, dll). Pattern denominator tidak mengganggu konten lain.

- [ ] **Step 5: Commit Pass 2b**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): cascade denominator Sl2..Sl25 dari / 30 ke / 34"
```

---

## Task 5: Update JS const TOTAL + slideCounter initial (Pass 3a)

**Tujuan**: Update 2 hardcoded angka 30 di JS + UI counter.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (line 3439 + 3445)

- [ ] **Step 1: Update slideCounter initial text**

Use Edit tool:
```
old_string:   <span class="slide-counter" id="slideCounter">1 / 30</span>
new_string:   <span class="slide-counter" id="slideCounter">1 / 34</span>
```

- [ ] **Step 2: Update JS const TOTAL**

Use Edit tool:
```
old_string:     const TOTAL = 30;
new_string:     const TOTAL = 34;
```

- [ ] **Step 3: Verify**

```bash
grep -n 'const TOTAL\|id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `<span class="slide-counter" id="slideCounter">1 / 34</span>` (line ~3439)
- `const TOTAL = 34;` (line ~3445)

- [ ] **Step 4: Commit Pass 3a**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): update JS TOTAL=34 + slideCounter initial 1/34"
```

---

## Task 6: Update HTML section comments shifted (Pass 3b)

**Tujuan**: Update `<!-- SLIDE N: TITLE ===` comment header untuk shifted slides. Maintenance-only (tidak affect rendering), penting untuk grep-ability future edit.

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (5 comments)

- [ ] **Step 1: Identify current comment lines**

```bash
grep -nE '<!-- ={5,} SLIDE (26|27|28|29|30):' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 5 matches. Catat baris.

- [ ] **Step 2: Update Sl30 Terima Kasih comment → Sl34**

Use Edit tool:
```
old_string: <!-- ================= SLIDE 30: TERIMA KASIH ================= -->
new_string: <!-- ================= SLIDE 34: TERIMA KASIH ================= -->
```

- [ ] **Step 3: Update Sl29 Quick Reference → Sl33**

Use Edit tool:
```
old_string: <!-- ================= SLIDE 29: TUGAS CEPAT + REFERENCE CARD ================= -->
new_string: <!-- ================= SLIDE 33: TUGAS CEPAT + REFERENCE CARD ================= -->
```

- [ ] **Step 4: Update Sl28 Maintenance → Sl32**

Use Edit tool:
```
old_string: <!-- ================= SLIDE 28: MAINTENANCE + AUDIT LOG ================= -->
new_string: <!-- ================= SLIDE 32: MAINTENANCE + AUDIT LOG ================= -->
```

- [ ] **Step 5: Update Sl27 Monitoring → Sl30**

Use Edit tool:
```
old_string: <!-- ================= SLIDE 27: ASSESSMENT MONITORING ================= -->
new_string: <!-- ================= SLIDE 30: ASSESSMENT MONITORING ================= -->
```

- [ ] **Step 6: Update Sl26 Override KKJ → Sl27**

Use Edit tool:
```
old_string: <!-- ================= SLIDE 26: OVERRIDE KKJ + MAPPING SILABUS ================= -->
new_string: <!-- ================= SLIDE 27: OVERRIDE KKJ + MAPPING SILABUS ================= -->
```

- [ ] **Step 7: Verify**

```bash
grep -nE '<!-- ={5,} SLIDE (26|27|28|29|30|32|33|34):' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 5 matches:
- `SLIDE 27: OVERRIDE KKJ` (was Sl26)
- `SLIDE 30: ASSESSMENT MONITORING` (was Sl27)
- `SLIDE 32: MAINTENANCE + AUDIT LOG` (was Sl28)
- `SLIDE 33: TUGAS CEPAT` (was Sl29)
- `SLIDE 34: TERIMA KASIH` (was Sl30)

`SLIDE 26`, `SLIDE 28`, `SLIDE 29`, `SLIDE 31` = 0 matches (slot kosong, akan diisi Task 7-10).

- [ ] **Step 8: Commit Pass 3b**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): renumber HTML section comments shifted slides (Sl26-Sl30 -> Sl27-Sl34)"
```

---

## Task 7: Insert Sl26 NEW — Manage Package Question

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` — insert sebelum Sl27 Override KKJ comment

- [ ] **Step 1: Locate insertion point**

```bash
grep -n '<!-- ================= SLIDE 27: OVERRIDE KKJ' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 1 match. Insert slide baru di **baris kosong tepat sebelum** comment ini.

- [ ] **Step 2: Insert Sl26 block via Edit tool**

Anchor pakai existing comment + slide div sebagai context unik:

```
old_string:   <!-- ================= SLIDE 27: OVERRIDE KKJ + MAPPING SILABUS ================= -->
new_string:   <!-- ================= SLIDE 26: MANAGE PACKAGE QUESTION ================= -->
  <div class="slide default-deco" data-slide="26">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Manage Package <span class="accent">Question</span></h1>
        <p class="slide-subtitle">Bank soal per package &mdash; manual form / Import Excel / Paste from Excel</p>
      </div>
      <div class="slide-badge">SLIDE 26 / 34</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/AssessmentAdmin/ManagePackageQuestions</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
              <strong style="font-size:8.5pt;color:#0f172a;">Package: Safety Op T1 &mdash; 24 soal &middot; 240 poin</strong>
              <span><button class="mr-btn">&#128228; Import Excel</button> <button class="mr-btn secondary">&larr; Kembali</button></span>
            </div>
            <div style="display:grid;grid-template-columns:1.3fr 1fr;gap:8px;">
              <table class="mr-table">
                <thead><tr><th>#</th><th>Teks Soal</th><th>Tipe</th><th>Skor</th><th>Aksi</th></tr></thead>
                <tbody>
                  <tr><td>1</td><td>LOTO step pertama yang&hellip;</td><td><span class="mr-badge-pill mr-badge-blue">MC</span></td><td>10</td><td>&#128065; &#9999; &#128465;</td></tr>
                  <tr><td>2</td><td>Pilih semua APAR class&hellip;</td><td><span class="mr-badge-pill" style="background:#7c3aed;color:#fff;">MA</span></td><td>15</td><td>&#128065; &#9999; &#128465;</td></tr>
                  <tr><td>3</td><td>Jelaskan prosedur ESD&hellip;</td><td><span class="mr-badge-pill mr-badge-orange">Essay</span></td><td>25</td><td>&#128065; &#9999; &#128465;</td></tr>
                  <tr><td>4</td><td>Hot work permit valid&hellip;</td><td><span class="mr-badge-pill mr-badge-blue">MC</span></td><td>10</td><td>&#128065; &#9999; &#128465;</td></tr>
                </tbody>
              </table>
              <div style="border:1px solid #e2e8f0;border-radius:6px;padding:8px;background:#f8fafc;">
                <div style="font-size:8pt;font-weight:700;color:#0f766e;margin-bottom:5px;">Tambah Soal Baru</div>
                <div style="font-size:7pt;color:#475569;margin-bottom:3px;">QuestionType</div>
                <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:3px 6px;font-size:7.5pt;margin-bottom:5px;">MultipleChoice &#9662;</div>
                <div style="font-size:7pt;color:#475569;margin-bottom:3px;">Teks Soal</div>
                <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:8px;font-size:7pt;color:#94a3b8;margin-bottom:5px;height:24px;">Ketik soal&hellip;</div>
                <div style="display:grid;grid-template-columns:1fr 1fr;gap:3px;margin-bottom:5px;">
                  <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:3px 6px;font-size:7pt;color:#94a3b8;">Opsi A</div>
                  <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:3px 6px;font-size:7pt;color:#94a3b8;">Opsi B</div>
                  <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:3px 6px;font-size:7pt;color:#94a3b8;">Opsi C</div>
                  <div style="background:#fff;border:1px solid #cbd5e1;border-radius:4px;padding:3px 6px;font-size:7pt;color:#94a3b8;">Opsi D</div>
                </div>
                <button class="mr-btn" style="width:100%;">Simpan Soal</button>
              </div>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#128221; 3 Entry Mode</h4>
          <ul>
            <li><strong>Manual form</strong> &mdash; tambah per soal, langsung simpan</li>
            <li><strong>Import Excel</strong> &mdash; upload <code>.xlsx</code> 9 kolom (Pertanyaan / A / B / C / D / Benar / Elemen / Type / Rubrik)</li>
            <li><strong>Paste from Excel</strong> &mdash; copy-paste cell langsung dari Excel</li>
          </ul>
          <h4 style="margin-top:8px;">&#128218; 3 QuestionType</h4>
          <ul>
            <li><strong>MultipleChoice</strong> &mdash; default, 1 jawaban benar</li>
            <li><strong>MultipleAnswer</strong> &mdash; multi jawaban (format <code>A,C</code>)</li>
            <li><strong>Essay</strong> &mdash; opsi kosong, <strong>Rubrik wajib</strong> (manual grading)</li>
          </ul>
          <div class="mockup-tip"><strong>&#128161; 4 template Excel:</strong> MC &middot; MA &middot; Essay &middot; Universal</div>
          <div class="mockup-warn"><strong>&#9888; Cascade delete:</strong> Hapus soal cascade ke history attempt. Preview dulu via &#128065; sebelum &#128465;.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Kelola bank soal per package assessment. 3 cara input (manual / upload Excel / paste). Mix tipe MC + MA + Essay dalam 1 package.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Bank Soal &amp; Package Question</p>
  </div>

  <!-- ================= SLIDE 27: OVERRIDE KKJ + MAPPING SILABUS ================= -->
```

- [ ] **Step 3: Verify Sl26 inserted**

```bash
grep -n 'data-slide="26"\|SLIDE 26 / 34\|SLIDE 26: MANAGE PACKAGE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 3 matches: `data-slide="26"`, badge `SLIDE 26 / 34`, comment `SLIDE 26: MANAGE PACKAGE`.

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: **32** (31 baseline + 1 new Sl26).

- [ ] **Step 4: Commit Task 7**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): insert Sl26 Manage Package Question (tutorial bank soal admin)"
```

---

## Task 8: Insert Sl28 NEW — Create Assessment Wizard Overview

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` — insert sebelum Sl30 Monitoring comment

- [ ] **Step 1: Locate insertion point**

```bash
grep -n '<!-- ================= SLIDE 30: ASSESSMENT MONITORING' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 1 match.

- [ ] **Step 2: Insert Sl28 block via Edit tool**

```
old_string:   <!-- ================= SLIDE 30: ASSESSMENT MONITORING ================= -->
new_string:   <!-- ================= SLIDE 28: CREATE ASSESSMENT - WIZARD OVERVIEW ================= -->
  <div class="slide default-deco" data-slide="28">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Create <span class="accent">Assessment</span> &mdash; Wizard 4 Step</h1>
        <p class="slide-subtitle">Kategori &rarr; Peserta &rarr; Settings &rarr; Konfirmasi</p>
      </div>
      <div class="slide-badge">SLIDE 28 / 34</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/CreateAssessment</span>
          </div>
          <div class="mockup-recreated">
            <div style="font-size:7pt;color:#64748b;margin-bottom:6px;">Kelola Data &rsaquo; Manage Assessment &amp; Training &rsaquo; <strong>Buat Assessment</strong></div>
            <div class="mr-tab-strip" style="margin-bottom:8px;">
              <span class="mr-tab active">&#9679; 1. Kategori</span>
              <span class="mr-tab" style="opacity:0.4;">&#9675; 2. Peserta</span>
              <span class="mr-tab" style="opacity:0.4;">&#9675; 3. Settings</span>
              <span class="mr-tab" style="opacity:0.4;">&#9675; 4. Konfirmasi</span>
            </div>
            <div style="border:1px solid #e2e8f0;border-radius:6px;padding:10px;background:#fff;">
              <div style="font-size:8pt;font-weight:700;color:#0f172a;margin-bottom:8px;">Langkah 1: Kategori &amp; Judul</div>
              <div style="font-size:7pt;color:#475569;margin-bottom:2px;">Kategori Assessment *</div>
              <div style="background:#f8fafc;border:1px solid #cbd5e1;border-radius:4px;padding:4px 8px;font-size:7.5pt;margin-bottom:6px;">
                Operations &rsaquo; Alkylation &#9662;
              </div>
              <div style="font-size:7pt;color:#475569;margin-bottom:2px;">Judul Assessment *</div>
              <div style="background:#f8fafc;border:1px solid #cbd5e1;border-radius:4px;padding:4px 8px;font-size:7.5pt;margin-bottom:6px;">
                Operator T1 Q2 2026 <span style="float:right;color:#94a3b8;">26/255</span>
              </div>
              <div style="font-size:7pt;color:#475569;margin-bottom:2px;">Tipe Assessment</div>
              <div style="background:#f8fafc;border:1px solid #cbd5e1;border-radius:4px;padding:4px 8px;font-size:7.5pt;margin-bottom:6px;">
                Standard &#9662;
              </div>
              <div style="font-size:6.5pt;color:#64748b;font-style:italic;">Assessment standar dengan satu sesi ujian.</div>
            </div>
            <div style="text-align:right;margin-top:6px;">
              <button class="mr-btn">Lanjut ke Step 2 &rarr;</button>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#129312; Wizard 4 Step Sequential</h4>
          <ul>
            <li><strong>Step 1</strong> Kategori + Judul + Tipe &mdash; pilih scope</li>
            <li><strong>Step 2</strong> Peserta &mdash; pilih NIP target</li>
            <li><strong>Step 3</strong> Settings &mdash; durasi, passing grade, jadwal, package soal</li>
            <li><strong>Step 4</strong> Konfirmasi &mdash; review sebelum publish</li>
          </ul>
          <h4 style="margin-top:8px;">&#127919; 3 Tipe Assessment</h4>
          <ul>
            <li><span class="mr-badge-pill mr-badge-blue">Standard</span> &mdash; 1 sesi ujian, hasil langsung</li>
            <li><span class="mr-badge-pill" style="background:#7c3aed;color:#fff;">Pre-Post Test</span> &mdash; 2 sesi, ukur gain score</li>
            <li><span class="mr-badge-pill mr-badge-orange">Proton</span> &mdash; per Track (Op/Panelman, Th 1/2/3), conditional field</li>
          </ul>
          <div class="mockup-tip"><strong>&#128161; Mode Renewal:</strong> otomatis pre-fill dari sesi expired &mdash; kategori + peserta diturunkan dari sesi sumber.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Wizard 4 step untuk buat assessment baru. Sequential &mdash; step 2-4 baru aktif setelah step sebelumnya valid. 3 tipe: Standard / Pre-Post / Proton (track-aware).</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Create Assessment (Wizard) &middot; lanjut Sl29 untuk Field Detail Step 2-4</p>
  </div>

  <!-- ================= SLIDE 30: ASSESSMENT MONITORING ================= -->
```

- [ ] **Step 3: Verify**

```bash
grep -n 'data-slide="28"\|SLIDE 28 / 34\|SLIDE 28: CREATE ASSESSMENT' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 3 matches.

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: **33**.

- [ ] **Step 4: Commit Task 8**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): insert Sl28 Create Assessment Wizard Overview"
```

---

## Task 9: Insert Sl29 NEW — Create Assessment Field Detail (Step 2-4)

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` — insert tepat setelah Sl28 (sebelum Sl30 Monitoring comment)

- [ ] **Step 1: Locate insertion point**

Sl28 baru saja diinsert. Sl29 insert antara Sl28 dan Sl30 Monitoring.

```bash
grep -n '<!-- ================= SLIDE 30: ASSESSMENT MONITORING' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 1 match (sama posisi sebelum Sl30 Monitoring).

- [ ] **Step 2: Insert Sl29 block via Edit tool**

```
old_string:   <!-- ================= SLIDE 30: ASSESSMENT MONITORING ================= -->
new_string:   <!-- ================= SLIDE 29: CREATE ASSESSMENT - FIELD DETAIL ================= -->
  <div class="slide default-deco" data-slide="29">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Create Assessment <span class="accent">Field Detail</span></h1>
        <p class="slide-subtitle">Step 2 Peserta &middot; Step 3 Settings &middot; Step 4 Konfirmasi</p>
      </div>
      <div class="slide-badge">SLIDE 29 / 34</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/CreateAssessment (lanjutan)</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:grid;grid-template-columns:repeat(3,1fr);gap:6px;">
              <div style="border:1px solid #cbd5e1;border-radius:6px;padding:6px;background:#fff;">
                <div style="font-size:7pt;font-weight:700;color:#0f766e;margin-bottom:4px;">&#9679; Step 2: Peserta</div>
                <div style="display:flex;gap:2px;margin-bottom:3px;">
                  <span class="mr-filter-chip" style="font-size:6pt;">Unit &#9662;</span>
                  <span class="mr-filter-chip" style="font-size:6pt;">Bagian &#9662;</span>
                </div>
                <div style="background:#f8fafc;border:1px solid #e2e8f0;padding:3px;font-size:6.5pt;margin-bottom:3px;">&#128269; Cari NIP&hellip;</div>
                <table class="mr-table" style="font-size:6pt;"><thead><tr><th>&#9745;</th><th>NIP</th><th>Nama</th></tr></thead><tbody>
                  <tr><td>&#9745;</td><td>754201</td><td>Widodo A.</td></tr>
                  <tr><td>&#9745;</td><td>754202</td><td>Andi P.</td></tr>
                  <tr><td>&#9745;</td><td>754203</td><td>Citra R.</td></tr>
                </tbody></table>
                <div style="font-size:6pt;color:#64748b;margin-top:3px;"><strong>12 peserta</strong> dipilih</div>
              </div>
              <div style="border:1px solid #cbd5e1;border-radius:6px;padding:6px;background:#fff;">
                <div style="font-size:7pt;font-weight:700;color:#0f766e;margin-bottom:4px;">&#9679; Step 3: Settings</div>
                <div style="font-size:6pt;color:#475569;">Durasi (menit)</div>
                <div style="background:#f8fafc;border:1px solid #e2e8f0;padding:2px 4px;font-size:6.5pt;margin-bottom:3px;">60</div>
                <div style="font-size:6pt;color:#475569;">Passing Grade (%)</div>
                <div style="background:#f8fafc;border:1px solid #e2e8f0;padding:2px 4px;font-size:6.5pt;margin-bottom:3px;">70</div>
                <div style="font-size:6pt;color:#475569;">Tanggal Mulai</div>
                <div style="background:#f8fafc;border:1px solid #e2e8f0;padding:2px 4px;font-size:6.5pt;margin-bottom:3px;">2026-06-01 09:00</div>
                <div style="font-size:6pt;color:#475569;">Package Soal</div>
                <div style="background:#f8fafc;border:1px solid #e2e8f0;padding:2px 4px;font-size:6.5pt;margin-bottom:3px;">Safety Op T1 (24 soal) &#9662;</div>
                <div style="font-size:6pt;color:#475569;display:flex;justify-content:space-between;">
                  <span>Token</span><span style="background:#cbd5e1;color:#475569;padding:0 4px;border-radius:8px;">OFF</span>
                </div>
                <div style="font-size:6pt;color:#475569;display:flex;justify-content:space-between;margin-top:2px;">
                  <span>Anti-Copy</span><span style="background:#16a34a;color:#fff;padding:0 4px;border-radius:8px;">ON</span>
                </div>
              </div>
              <div style="border:1px solid #cbd5e1;border-radius:6px;padding:6px;background:#fff;">
                <div style="font-size:7pt;font-weight:700;color:#0f766e;margin-bottom:4px;">&#9679; Step 4: Konfirmasi</div>
                <div style="background:#f0fdfa;border:1px dashed #14b8a6;border-radius:4px;padding:6px;font-size:6.5pt;line-height:1.4;">
                  <div><strong>Kategori:</strong> Safety Op</div>
                  <div><strong>Judul:</strong> Op T1 Q2 2026</div>
                  <div><strong>Peserta:</strong> 12 orang</div>
                  <div><strong>Jadwal:</strong> 2026-06-01 09:00</div>
                  <div><strong>Durasi:</strong> 60 menit</div>
                  <div><strong>Package:</strong> Safety Op T1</div>
                </div>
                <button class="mr-btn" style="width:100%;margin-top:6px;background:#16a34a;color:#fff;">&#9989; Publish Assessment</button>
              </div>
            </div>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#128203; Field per Step</h4>
          <table class="mr-table" style="font-size:7.5pt;">
            <thead><tr><th>Step</th><th>Field Utama</th><th>Catatan</th></tr></thead>
            <tbody>
              <tr><td><strong>2</strong> Peserta</td><td>NIP picker + filter Unit/Bagian</td><td>Bulk per unit</td></tr>
              <tr><td><strong>3</strong> Settings</td><td>Durasi, Passing, Jadwal, Package, Token, Anti-Copy</td><td>Anti-Copy default ON</td></tr>
              <tr><td><strong>4</strong> Konfirmasi</td><td>Review semua field</td><td>Publish ireversibel</td></tr>
            </tbody>
          </table>
          <div class="mockup-tip" style="margin-top:6px;"><strong>&#128161; Token Required:</strong> tiap peserta dapat token unik &mdash; regenerate via Sl30 Monitoring.</div>
          <div class="mockup-warn"><strong>&#9888; Publish ireversibel:</strong> setelah klik, assessment muncul di portal peserta. Edit hanya boleh sebelum ada peserta submit (cek <code>AssessmentEditEligibility</code>).</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Detail field Step 2-4 wizard. Step 2 pilih peserta dari master pekerja. Step 3 set durasi/passing/jadwal/package + 2 toggle. Step 4 review + Publish.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Field Wizard Step 2-4 (lanjutan Sl28)</p>
  </div>

  <!-- ================= SLIDE 30: ASSESSMENT MONITORING ================= -->
```

- [ ] **Step 3: Verify**

```bash
grep -n 'data-slide="29"\|SLIDE 29 / 34\|SLIDE 29: CREATE ASSESSMENT - FIELD' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 3 matches.

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: **34**.

- [ ] **Step 4: Commit Task 9**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): insert Sl29 Create Assessment Field Detail (Step 2-4)"
```

---

## Task 10: Insert Sl31 NEW — Monitoring Actions

**Files:**
- Modify: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` — insert antara Sl30 Monitoring (existing shifted) dan Sl32 Maintenance

- [ ] **Step 1: Locate insertion point**

```bash
grep -n '<!-- ================= SLIDE 32: MAINTENANCE' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: 1 match. Insert sebelum comment ini.

- [ ] **Step 2: Insert Sl31 block via Edit tool**

```
old_string:   <!-- ================= SLIDE 32: MAINTENANCE + AUDIT LOG ================= -->
new_string:   <!-- ================= SLIDE 31: MONITORING ACTIONS ================= -->
  <div class="slide default-deco" data-slide="31">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
        <h1 class="slide-title">Monitoring <span class="accent">Actions</span> &mdash; Detail per Peserta</h1>
        <p class="slide-subtitle">Drill dari Sl30 overview &mdash; Reset / Akhiri / Reshuffle / Tambah Waktu</p>
      </div>
      <div class="slide-badge">SLIDE 31 / 34</div>
    </div>
    <div class="slide-body">
      <div class="slide-mockup-split">
        <div class="mockup-frame">
          <div class="mockup-bar">
            <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/AssessmentMonitoringDetail</span>
          </div>
          <div class="mockup-recreated">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
              <strong style="font-size:8pt;color:#0f172a;">Alkylation Batch 5 &mdash; 4 peserta</strong>
              <span>
                <button class="mr-btn" style="background:#f59e0b;color:#fff;">&#9201; Tambah Waktu</button>
                <button class="mr-btn" style="background:#dc2626;color:#fff;">&#10060; Akhiri Semua</button>
              </span>
            </div>
            <table class="mr-table">
              <thead><tr><th>NIP</th><th>Nama</th><th>Progress</th><th>Status</th><th>Aksi</th></tr></thead>
              <tbody>
                <tr>
                  <td>754201</td><td>Widodo A.</td><td>15/30 (50%)</td>
                  <td><span class="mr-badge-pill mr-badge-green">InProgress</span></td>
                  <td style="font-size:7pt;">
                    <a style="color:#f59e0b;">&#8634; Reset</a> &middot;
                    <a style="color:#dc2626;">&#10060; Akhiri</a> &middot;
                    <a style="color:#0f766e;">&#8644; Reshuffle</a>
                  </td>
                </tr>
                <tr>
                  <td>754202</td><td>Andi P.</td><td>30/30 (100%)</td>
                  <td><span class="mr-badge-pill mr-badge-blue">Submitted</span></td>
                  <td style="font-size:7pt;"><a style="color:#f59e0b;">&#8634; Reset</a></td>
                </tr>
                <tr>
                  <td>754203</td><td>Citra R.</td><td>0/30 (0%)</td>
                  <td><span class="mr-badge-pill" style="background:#94a3b8;color:#fff;">NotStarted</span></td>
                  <td style="font-size:7pt;">
                    <a style="color:#f59e0b;">&#8634; Reset</a> &middot;
                    <a style="color:#0f766e;">&#8644; Reshuffle</a>
                  </td>
                </tr>
                <tr>
                  <td>754204</td><td>Budi S.</td><td>8/30 (27%)</td>
                  <td><span class="mr-badge-pill mr-badge-green">InProgress</span></td>
                  <td style="font-size:7pt;">
                    <a style="color:#f59e0b;">&#8634; Reset</a> &middot;
                    <a style="color:#dc2626;">&#10060; Akhiri</a> &middot;
                    <a style="color:#0f766e;">&#8644; Reshuffle</a>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="mockup-content">
          <h4>&#127916; Action Hierarchy &mdash; 2 Level</h4>
          <ul>
            <li style="font-weight:700;color:#dc2626;">Group-Level (top bar):</li>
            <li>&#9201; <strong>Tambah Waktu</strong> &mdash; extend timer semua peserta</li>
            <li>&#10060; <strong>Akhiri Semua Ujian</strong> &mdash; force-close all, auto-submit (modal confirm)</li>
            <li>&#8644; <strong>Reshuffle All</strong> &mdash; ganti package semua peserta sekaligus</li>
          </ul>
          <ul>
            <li style="font-weight:700;color:#0f766e;">Per-Peserta:</li>
            <li>&#8634; <strong>Reset</strong> &mdash; hapus jawaban, attempt baru</li>
            <li>&#10060; <strong>Akhiri Ujian</strong> &mdash; force-submit (auto-grade), <em>InProgress</em> only</li>
            <li>&#8644; <strong>Reshuffle Worker</strong> &mdash; AJAX ganti package, no reload</li>
          </ul>
          <div class="mockup-warn"><strong>&#9888; Ireversibel:</strong> Akhiri = auto-submit jawaban saat itu. Reset = hapus progress.</div>
          <div class="mockup-tip"><strong>&#128161; Use case:</strong> Reshuffle &rarr; curiga bocor soal. Tambah Waktu &rarr; peserta disconnect / hardware issue.</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:10px;font-size:8.5pt;background:#fef3c7;border-left:3px solid #f59e0b;color:#78350f;">&#128221; <strong>Fungsi:</strong> Page Detail per grup assessment (klik dari Sl30 overview). 3 action group-level + 3 action per-peserta. Handle disconnect, freeze peserta, atau curiga kebocoran soal.</div>
    </div>
    <p class="panduan-ref"><strong>Detail:</strong> Panduan Operasional HC &mdash; Monitor / Reset / Force-Close (selaras Sl30 overview)</p>
  </div>

  <!-- ================= SLIDE 32: MAINTENANCE + AUDIT LOG ================= -->
```

- [ ] **Step 3: Verify**

```bash
grep -n 'data-slide="31"\|SLIDE 31 / 34\|SLIDE 31: MONITORING ACTIONS' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected 3 matches.

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected: **35** (34 slides + 1 JS selector match).

- [ ] **Step 4: Commit Task 10**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
git commit -m "docs(sosialisasi-v2): insert Sl31 Monitoring Actions (Detail per Peserta)"
```

---

## Task 11: Final verification + tag release

**Files:**
- Read: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`

- [ ] **Step 1: Count verifikasi**

```bash
grep -c 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c 'class="slide-badge"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c ' / 30</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -c ' / 34</div>' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
grep -n 'const TOTAL\|id="slideCounter"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html
```

Expected:
- `data-slide=` = **35** (34 slides + 1 JS)
- `class="slide-badge"` = **32** (Sl2..Sl29 base 28 + 4 new Sl26/28/29/31 = 32; Sl1 cover & Sl34 penutup tetap no badge)
- ` / 30</div>` = **0**
- ` / 34</div>` = **32**
- `const TOTAL = 34;` + `id="slideCounter">1 / 34<` present

- [ ] **Step 2: Sequential data-slide check**

```bash
grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html | sort -t'"' -k2 -n | uniq -c
```

Expected output: 34 lines, masing-masing `1 data-slide="N"` untuk N=1..34, plus 1 line dari JS selector (akan match `data-slide="' + n + '"` — beda format, mungkin tidak muncul di grep ini karena pattern berbeda).

Lebih spesifik:

```bash
for n in $(seq 1 34); do
  c=$(grep -c "data-slide=\"$n\"" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html)
  if [ "$c" != "1" ]; then echo "MISMATCH slide $n: count=$c"; fi
done
echo "Done sequential check."
```

Expected: hanya `Done sequential check.` (no mismatch).

- [ ] **Step 3: Browser visual verification**

```bash
echo "Open: file:///$(pwd | sed 's|/|\\|g')/docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html"
```

Manual steps di browser:
1. Buka URL di Chrome/Edge.
2. Counter bawah harus tampil `1 / 34`.
3. Tekan `End` → loncat ke Sl34 Terima Kasih.
4. Tekan `Home` → balik Sl1 Cover.
5. Klik next 34x, verify badge tiap slide sequential 2/34, 3/34, ..., 33/34. Sl1 cover + Sl34 penutup no badge.
6. Spot-check 4 slide baru (Sl26, Sl28, Sl29, Sl31):
   - Layout muat tanpa overflow
   - Mockup frame tidak crop
   - Tip-bar terlihat
   - Konten kanan tidak terpotong
7. Toggle dark mode (☀ button) → cek semua slide baru contrast OK.

- [ ] **Step 4: Tag release**

```bash
git tag sosialisasi-internal-hc-v2.1 -m "Sosialisasi-Internal-Tim-HC v2.1: +4 tutorial Admin slide (Package Q, Create Asm Overview, Create Asm Detail, Monitor Actions). Deck 30 -> 34 slide."
git log --oneline -15
```

Expected: 11 commit baru dari Task 1-10 + tag pointing ke commit terakhir.

- [ ] **Step 5: Skip extra commit** (Task 11 = verification only, kecuali tag)

```bash
git status
```

Expected: clean.

---

## Catatan Implementasi

### Risiko Edit Tool

- Edit tool `replace_all: true` di Task 4 mungkin reject jika pattern ` / 30</div>` muncul di context lain. **Fallback**: PowerShell single regex replace (sudah disebut di Task 4 Step 2 alternative).
- Jika file CRLF, Git warn `LF will be replaced by CRLF`. Aman, ignore.

### Density Limit

Setiap slide baru harus muat dalam frame 1280×720 (CSS `--deck-scale` auto-fit). Kalau saat browser verification ada overflow:
1. Kurangi font-size di mockup-recreated table dari `7pt` ke `6.5pt`
2. Kurangi gap grid dari `gap:8px` ke `gap:6px`
3. Compress bullet text di mockup-content (≤80 char per bullet)

### Rollback

Setiap task = 1 commit. Rollback per-task via:
```bash
git revert <commit-hash>
```
Atau rollback semua (Task 2-10):
```bash
git reset --hard <commit-before-Task-2>
```

---

## Self-Review Results

**Spec coverage:**
- ✅ Slide 26 Manage Package Question → Task 7
- ✅ Slide 28 Create Asm Wizard Overview → Task 8
- ✅ Slide 29 Create Asm Field Detail → Task 9
- ✅ Slide 31 Monitoring Actions → Task 10
- ✅ Section map shift (Sl26→27, Sl27→30, Sl28→32, Sl29→33, Sl30→34) → Task 2
- ✅ Badge text shift shifted slides → Task 3
- ✅ Cascade denominator Sl2..Sl25 → Task 4
- ✅ JS TOTAL + slideCounter update → Task 5
- ✅ HTML section comments update → Task 6
- ✅ Sl1 cover + Sl34 penutup no-badge exception → noted in Task 11 expected counts
- ✅ Verification (counts + browser nav + dark mode) → Task 11
- ✅ Tag release → Task 11 Step 4
- ✅ Icon convention HTML entity (no Bootstrap Icons) → all task inline HTML uses `&#NNNN;`
- ✅ CSS reuse (`mr-tab-strip` untuk Sl28 wizard) → Task 8 Step 2

**Placeholder scan:** None found. All inline HTML lengkap.

**Type consistency:** Class names match spec — `slide-mockup-split`, `mockup-frame`, `mockup-bar`, `mockup-recreated`, `mockup-content`, `mockup-tip`, `mockup-warn`, `tip-bar`, `panduan-ref`, `mr-table`, `mr-tab`, `mr-tab-strip`, `mr-filter-chip`, `mr-btn`, `mr-badge-pill`, `mr-badge-green/orange/blue`. Verified via Grep di spec.

---

**End of plan. Total 11 tasks. Estimated wall time: 45-60 menit (mostly Edit + verify + commit cycles).**
