# Kickoff-PROTON Slide 25/26/30 Koreksi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aplikasikan koreksi v4.1 di `docs/Kickoff-PROTON.html`: edit slide 25 (3 sub-koreksi), drop slide 26 + 30, renumber 32→30, update TOTAL constant.

**Architecture:** Edit incremental per-task; verifikasi tiap commit lewat grep + visual browser; tidak ada test framework (file HTML standalone — manual verification + grep sebagai contract test). Tag v4.1 di akhir.

**Tech Stack:** Static HTML + inline CSS + vanilla JS counter; tooling = `Edit` tool + `Grep` + Playwright MCP untuk visual verification.

**Spec Reference:** `docs/superpowers/specs/2026-05-28-kickoff-proton-slide25-26-30-koreksi-design.md`

---

## File Structure

**Modify only:**
- `docs/Kickoff-PROTON.html` — single file, all changes scoped here

**No new files. No tests file (HTML deck has no test harness).**

---

## Task 1: Slide 25 Step 1 — Edit kedua kolom

**Files:**
- Modify: `docs/Kickoff-PROTON.html` line ~3128 (Tahun 1-2) dan ~3140 (Tahun 3)

- [ ] **Step 1: Verifikasi state awal lewat grep**

Run: `grep -n "Coachee submit <strong>Deliverables" docs/Kickoff-PROTON.html`
Expected output:
```
3128:          <div style="background: #fff; border-left: 4px solid var(--teal-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--teal-dark);">1.</strong> Coachee submit <strong>Deliverables</strong> (ProtonDeliverableProgress)</div>
3140:          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">1.</strong> Coachee submit <strong>Deliverables</strong></div>
```

- [ ] **Step 2: Replace Tahun 1-2 step 1**

Edit `docs/Kickoff-PROTON.html`:
- old: `<strong style="color: var(--teal-dark);">1.</strong> Coachee submit <strong>Deliverables</strong> (ProtonDeliverableProgress)`
- new: `<strong style="color: var(--teal-dark);">1.</strong> <strong>Coach Submit Evidence Coaching</strong>`

- [ ] **Step 3: Replace Tahun 3 step 1**

Edit `docs/Kickoff-PROTON.html`:
- old: `<strong style="color: var(--amber-dark);">1.</strong> Coachee submit <strong>Deliverables</strong>`
- new: `<strong style="color: var(--amber-dark);">1.</strong> <strong>Coach Submit Evidence Coaching</strong>`

- [ ] **Step 4: Verifikasi grep**

Run: `grep -n "Coach Submit Evidence Coaching" docs/Kickoff-PROTON.html`
Expected: 2 matches (line ~3128, ~3140).

Run: `grep -n "Coachee submit" docs/Kickoff-PROTON.html`
Expected: 0 matches (sudah tidak ada).

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): slide 25 step 1 aktor Coach Submit Evidence Coaching kedua kolom"
```

---

## Task 2: Slide 25 Step 2 — Urutan approval

**Files:**
- Modify: `docs/Kickoff-PROTON.html` line ~3129 (Tahun 1-2) dan ~3141 (Tahun 3)

Text step 2 identik di kedua kolom: `<strong>3-Role Approval:</strong> HC + SrSpv + SectionHead`. Pakai `replace_all`.

- [ ] **Step 1: Verifikasi state awal**

Run: `grep -n "HC + SrSpv + SectionHead" docs/Kickoff-PROTON.html`
Expected: 2 matches (line ~3129, ~3141).

- [ ] **Step 2: Replace pakai replace_all**

Edit `docs/Kickoff-PROTON.html` dengan `replace_all=true`:
- old: `<strong>3-Role Approval:</strong> HC + SrSpv + SectionHead`
- new: `<strong>3-Role Approval:</strong> Sr Spv &rarr; Section Head &rarr; HC (Final Review)`

- [ ] **Step 3: Verifikasi grep**

Run: `grep -n "Sr Spv &rarr; Section Head &rarr; HC (Final Review)" docs/Kickoff-PROTON.html`
Expected: 2 matches.

Run: `grep -n "HC + SrSpv + SectionHead" docs/Kickoff-PROTON.html`
Expected: 0 matches.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): slide 25 step 2 approval order Sr Spv->Section Head->HC Final Review"
```

---

## Task 3: Slide 25 Tahun 3 Step 6 — Drop Sertifikasi Mahir

**Files:**
- Modify: `docs/Kickoff-PROTON.html` line ~3145 (Tahun 3 kolom kanan, step 6 baris)

Tahun 3 jadi 5 step. Tahun 1-2 tetap 6 step (step 6 = HC create ProtonFinalAssessment, biarkan).

- [ ] **Step 1: Verifikasi state awal**

Run: `grep -n "Sertifikasi Mahir" docs/Kickoff-PROTON.html`
Expected output (3 matches):
```
3145:          <div style="background: #fff; border-left: 4px solid var(--green); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--green);">6.</strong> 🏅 <strong>Sertifikasi Mahir</strong> granted</div>
3453: ...slide 31 card kanan...
3466: ...slide 31 card kanan...
```

Catat: hanya line 3145 yang harus dihapus. Line 3453 + 3466 tetap (slide 31 outcome card).

- [ ] **Step 2: Hapus baris step 6 Tahun 3**

Edit `docs/Kickoff-PROTON.html` (hapus 1 baris step 6, anchor pakai step 5 di atasnya untuk unique match):
- old:
```
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">5.</strong> HC create ProtonFinalAssessment + <strong>CompetencyLevel 0-5</strong></div>
          <div style="background: #fff; border-left: 4px solid var(--green); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--green);">6.</strong> 🏅 <strong>Sertifikasi Mahir</strong> granted</div>
```
- new:
```
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">5.</strong> HC create ProtonFinalAssessment + <strong>CompetencyLevel 0-5</strong></div>
```

- [ ] **Step 3: Verifikasi grep**

Run: `grep -n "Sertifikasi Mahir" docs/Kickoff-PROTON.html`
Expected: 2 matches (line ~3452 dan ~3465 — slide 31 saja).

Run: `grep -c "Sertifikasi Mahir" docs/Kickoff-PROTON.html`
Expected: `2`.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): slide 25 Tahun 3 step 6 Mahir drop duplicate slide 31"
```

---

## Task 4: Drop Slide 26 — Anatomi Soal

**Files:**
- Modify: `docs/Kickoff-PROTON.html` line ~3152–3211 (block `data-slide="26"` + comment marker)

- [ ] **Step 1: Lokasi exact block**

Run: `grep -n 'data-slide="26"\|SLIDE 26:' docs/Kickoff-PROTON.html`
Expected output:
```
3152:<!-- ================= SLIDE 26: SISTEM UJIAN — ANATOMI SOAL ================= -->
3153:<div class="slide default-deco" data-slide="26">
```

Baca exact content line 3152 sampai closing `</div>` block — line ~3211 (verifikasi via `Read` tool offset 3152 limit 60 dulu).

- [ ] **Step 2: Hapus full block 26**

Edit `docs/Kickoff-PROTON.html`:
- old: full block dari `<!-- ================= SLIDE 26: SISTEM UJIAN — ANATOMI SOAL ================= -->` sampai closing `</div>` paling akhir block 26 (sebelum `<!-- ================= SLIDE 27:`).
- new: (empty string — hapus full)

Pastikan blank line setelah hapus tidak meninggalkan double blank antara block 25 dan block 27.

- [ ] **Step 3: Verifikasi grep**

Run: `grep -n 'data-slide="26"\|SLIDE 26:\|Anatomi Soal' docs/Kickoff-PROTON.html`
Expected: 0 matches.

Run: `grep -c 'class="slide ' docs/Kickoff-PROTON.html`
Expected: `31` (32 - 1).

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): drop slide 26 Anatomi Soal ER terlalu teknis kickoff"
```

---

## Task 5: Drop Slide 30 — Cara Grading HC POV

**Files:**
- Modify: `docs/Kickoff-PROTON.html` line ~3384–3422 (block `data-slide="30"` + comment marker)

**Catat:** line number sudah bergeser akibat Task 4 drop ~60 baris; locate ulang.

- [ ] **Step 1: Lokasi exact block**

Run: `grep -n 'data-slide="30"\|SLIDE 30:' docs/Kickoff-PROTON.html`
Expected output: 2 matches (comment line + div line, sekitar line ~3324 setelah Task 4 shift).

Baca exact content lewat `Read` tool offset = grep line, limit 45.

- [ ] **Step 2: Hapus full block 30**

Edit `docs/Kickoff-PROTON.html`:
- old: full block dari `<!-- ================= SLIDE 30: CARA GRADING — HC POV ================= -->` sampai closing `</div>` paling akhir block 30 (sebelum `<!-- ================= SLIDE 31:`).
- new: (empty string — hapus full)

- [ ] **Step 3: Verifikasi grep**

Run: `grep -n 'data-slide="30"\|SLIDE 30:\|Cara Grading' docs/Kickoff-PROTON.html`
Expected: 0 matches.

Run: `grep -c 'class="slide ' docs/Kickoff-PROTON.html`
Expected: `30` (31 - 1).

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): drop slide 30 Cara Grading HC POV not for coachee"
```

---

## Task 6: Renumber data-slide attribute (27→26, 28→27, 29→28, 31→29, 32→30)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — 5 slide affected

Urutan critical: rename harus dari **terkecil ke terbesar** untuk hindari collision (27→26 dulu karena 26 sudah kosong; sebaliknya kalau 32→30 dulu sebelum 30 dihapus akan tabrakan — di sini 30 sudah dihapus jadi OK, tapi tetap pakai urutan strict ascending untuk reproducibility).

- [ ] **Step 1: Verifikasi state awal**

Run: `grep -n 'data-slide=' docs/Kickoff-PROTON.html | head -40`
Expected: 30 entries dengan urutan attribute lama: 1..25, 27, 28, 29, 31, 32 (gap di 26 dan 30).

- [ ] **Step 2: Renumber 27 → 26**

Edit `docs/Kickoff-PROTON.html`:
- old: `<div class="slide default-deco" data-slide="27">`
- new: `<div class="slide default-deco" data-slide="26">`

Update badge slide tsb (di header block):
- old: `<div class="slide-badge">27 / 32</div>`
- new: `<div class="slide-badge">26 / 30</div>`

Update comment marker:
- old: `<!-- ================= SLIDE 27: SISTEM UJIAN — GRADING ENGINE ================= -->`
- new: `<!-- ================= SLIDE 26: SISTEM UJIAN — GRADING ENGINE ================= -->`

- [ ] **Step 3: Renumber 28 → 27**

Edit `docs/Kickoff-PROTON.html`:
- old: `<div class="slide default-deco" data-slide="28">`
- new: `<div class="slide default-deco" data-slide="27">`

Update badge:
- old: `<div class="slide-badge">28 / 32</div>`
- new: `<div class="slide-badge">27 / 30</div>`

Update comment:
- old: `<!-- ================= SLIDE 28: STATUS LIFECYCLE & TIMER ================= -->`
- new: `<!-- ================= SLIDE 27: STATUS LIFECYCLE & TIMER ================= -->`

- [ ] **Step 4: Renumber 29 → 28**

Edit `docs/Kickoff-PROTON.html`:
- old: `<div class="slide default-deco" data-slide="29">`
- new: `<div class="slide default-deco" data-slide="28">`

Update badge:
- old: `<div class="slide-badge">29 / 32</div>`
- new: `<div class="slide-badge">28 / 30</div>`

Update comment:
- old: `<!-- ================= SLIDE 29: CARA UJIAN — COACHEE POV ================= -->`
- new: `<!-- ================= SLIDE 28: CARA UJIAN — COACHEE POV ================= -->`

- [ ] **Step 5: Renumber 31 → 29**

Edit `docs/Kickoff-PROTON.html`:
- old: `<div class="slide default-deco" data-slide="31">`
- new: `<div class="slide default-deco" data-slide="29">`

Update badge:
- old: `<div class="slide-badge">31 / 32</div>`
- new: `<div class="slide-badge">29 / 30</div>`

Update comment:
- old: `<!-- ================= SLIDE 31: OUTCOME — SERTIFIKAT & COMPETENCYLEVEL ================= -->`
- new: `<!-- ================= SLIDE 29: OUTCOME — SERTIFIKAT & COMPETENCYLEVEL ================= -->`

- [ ] **Step 6: Renumber 32 → 30 (tidak ada badge slide 32)**

Edit `docs/Kickoff-PROTON.html`:
- old: `<div class="slide default-deco" data-slide="32">`
- new: `<div class="slide default-deco" data-slide="30">`

Update comment:
- old: `<!-- ================= SLIDE 32: PENUTUP / TERIMA KASIH ================= -->`
- new: `<!-- ================= SLIDE 30: PENUTUP / TERIMA KASIH ================= -->`

- [ ] **Step 7: Verifikasi sequential data-slide**

Run: `grep -oE 'data-slide="[0-9]+"' docs/Kickoff-PROTON.html | sort -t'"' -k2 -n | uniq`
Expected: data-slide="1" sampai data-slide="30" sequential tanpa gap.

Run: `grep -c 'data-slide=' docs/Kickoff-PROTON.html`
Expected: `30` (+ 1 untuk querySelector di JS = 31 total, tapi querySelector pakai `'[data-slide="' + n + '"]'` jadi tidak match exact regex literal — verifikasi:)

Run: `grep -cE 'data-slide="[0-9]+"' docs/Kickoff-PROTON.html`
Expected: `30`.

- [ ] **Step 8: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): renumber data-slide + badge 27->26 28->27 29->28 31->29 32->30"
```

---

## Task 7: Update badge denominator slide 1-25 (/ 32 → / 30)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — 25 badge slide 1 sampai 25 (slide 32 awal tidak punya badge, slide 26-30 baru sudah diupdate Task 6)

- [ ] **Step 1: Verifikasi state awal**

Run: `grep -cE '<div class="slide-badge">[0-9]+ / 32</div>' docs/Kickoff-PROTON.html`
Expected: `25` (slide 1-25 belum di-renumber Task 6).

- [ ] **Step 2: Replace denominator pakai replace_all**

Edit `docs/Kickoff-PROTON.html` dengan `replace_all=true`:
- old: ` / 32</div>`
- new: ` / 30</div>`

**Catat:** scope replace_all = seluruh file, tapi setelah Task 6 sudah tidak ada lagi `/ 32</div>` di slide 26-29 (Task 6 mengubah `27/32` jadi `26/30` etc). Jadi replace_all hanya kena 25 badge slide 1-25.

- [ ] **Step 3: Verifikasi**

Run: `grep -cE '<div class="slide-badge">[0-9]+ / 32</div>' docs/Kickoff-PROTON.html`
Expected: `0`.

Run: `grep -cE '<div class="slide-badge">[0-9]+ / 30</div>' docs/Kickoff-PROTON.html`
Expected: `29` (25 dari slide 1-25 + 4 dari slide 26-29 = 29; slide 30 Terima Kasih tidak punya badge).

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): badge denominator / 32 -> / 30 slide 1-25"
```

---

## Task 8: Update JS TOTAL constant + footer counter + versi label

**Files:**
- Modify: `docs/Kickoff-PROTON.html` JS line ~3506 + footer counter line ~3500 + versi label line ~3490

- [ ] **Step 1: Verifikasi state awal**

Run: `grep -n 'const TOTAL\|slide-counter\|Versi 4.0' docs/Kickoff-PROTON.html`
Expected:
```
~3490:      Kickoff PROTON &middot; Versi 4.0 (Part 1 + Part 2 Assessment) &middot; Mei 2026
~3500:  <span class="slide-counter" id="slideCounter">1 / 32</span>
~3506:    const TOTAL = 32;
```

- [ ] **Step 2: Update TOTAL constant**

Edit `docs/Kickoff-PROTON.html`:
- old: `const TOTAL = 32;`
- new: `const TOTAL = 30;`

- [ ] **Step 3: Update footer counter init**

Edit `docs/Kickoff-PROTON.html`:
- old: `<span class="slide-counter" id="slideCounter">1 / 32</span>`
- new: `<span class="slide-counter" id="slideCounter">1 / 30</span>`

- [ ] **Step 4: Update versi label**

Edit `docs/Kickoff-PROTON.html`:
- old: `Kickoff PROTON &middot; Versi 4.0 (Part 1 + Part 2 Assessment) &middot; Mei 2026`
- new: `Kickoff PROTON &middot; Versi 4.1 (Koreksi Slide 25/26/30) &middot; Mei 2026`

- [ ] **Step 5: Verifikasi**

Run: `grep -n 'TOTAL = 30\|1 / 30</span>\|Versi 4.1' docs/Kickoff-PROTON.html`
Expected: 3 matches.

Run: `grep -n 'TOTAL = 32\|1 / 32</span>\|Versi 4.0' docs/Kickoff-PROTON.html`
Expected: 0 matches.

- [ ] **Step 6: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton-v4.1): JS TOTAL=30 + counter 1/30 + versi label v4.1"
```

---

## Task 9: Browser verification Playwright + tag

**Files:** verification only

- [ ] **Step 1: Buka file di browser via Playwright**

Run via MCP: `browser_navigate file:///C:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/Kickoff-PROTON.html`

- [ ] **Step 2: Verifikasi counter awal**

Snapshot atau screenshot. Konfirmasi footer counter menampilkan `1 / 30`.

- [ ] **Step 3: Navigate ke slide 25**

Press `End` atau panah kanan 24x. Verifikasi:
- Badge `25 / 30`
- Step 1 kedua kolom: "Coach Submit Evidence Coaching"
- Step 2 kedua kolom: "3-Role Approval: Sr Spv → Section Head → HC (Final Review)"
- Tahun 1-2 kolom kiri: 6 step terlihat
- Tahun 3 kolom kanan: 5 step terlihat (tidak ada baris hijau "Sertifikasi Mahir granted")

- [ ] **Step 4: Navigate slide 26 (baru)**

Verifikasi tampil "Sistem Ujian — Grading Engine" (mantan slide 27), badge `26 / 30`.

- [ ] **Step 5: Navigate slide 28 (baru)**

Verifikasi tampil "Cara Ujian — Coachee POV" (mantan slide 29), badge `28 / 30`.

- [ ] **Step 6: Navigate slide 29 (baru)**

Verifikasi tampil "Outcome — Sertifikat & CompetencyLevel" (mantan slide 31), badge `29 / 30`. Card kanan "🏅 Sertifikasi Mahir" tetap ada (outcome konsep).

- [ ] **Step 7: Navigate slide 30 (baru, terakhir)**

Verifikasi tampil "Terima Kasih" (mantan slide 32). Footer counter `30 / 30`. Tombol next disabled.

- [ ] **Step 8: Verifikasi versi label slide terakhir**

Slide 30 footer text: `Kickoff PROTON · Versi 4.1 (Koreksi Slide 25/26/30) · Mei 2026`.

- [ ] **Step 9: Tag tag v4.1**

```bash
git tag kickoff-proton-v4.1-koreksi-slide25-26-30
```

(Tag tidak push ke origin tanpa user approval explicit — sesuai CLAUDE.md.)

- [ ] **Step 10: Final status check**

```bash
git log --oneline -10
git status
```

Expected: 8 commit baru (Task 1-8) di atas `b38f42e7` spec commit, tag `kickoff-proton-v4.1-koreksi-slide25-26-30` pointing ke commit terakhir, working tree clean.

---

## Notes Reproduksi

- Tiap task atomic commit untuk gampang revert single task kalau ada koreksi user setelah review.
- Line number di plan mengacu ke state awal pre-Task; setelah drop slide 26 (Task 4) dan slide 30 (Task 5), line number subsequent bergeser ±60 baris dan ±40 baris. Plan instruct grep ulang sebelum tiap Edit untuk dapat line number current.
- Tidak ada perubahan logic JS selain `TOTAL` constant. Slide navigation script `go(n)` tidak diubah — sudah handle TOTAL dinamis.
- Tag v4.1 dibuat LOKAL only; push tag = decision user (sesuai CLAUDE.md workflow).
