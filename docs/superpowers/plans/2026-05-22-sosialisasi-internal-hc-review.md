# Sosialisasi-Internal-Tim-HC Review & Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Audit + fix 17 finding (5 konten salah, 5 redundansi, 4 urutan/BAGIAN, 3 nice-to-have) di `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` tanpa mengubah jumlah slide (tetap 41).

**Architecture:** 5-phase edit sequence — Phase A text fixes (no move), Phase B structural moves, Phase C renumber+relabel, Phase D Playwright browser verify. Sequential karena moves invalidate line numbers untuk Phase A.

**Tech Stack:** HTML5 static deck (Bootstrap-like custom CSS), vanilla JS counter `TOTAL=41`, Playwright MCP untuk browser verify.

**Spec ref:** `docs/superpowers/specs/2026-05-22-sosialisasi-internal-hc-review-design.md`

**Branch note:** Bekerja di branch saat ini (`feature/phase-321-edit-jawaban`). User pilih Opsi C (full fix dalam pass sama). Commit per task untuk atomic rollback.

---

## File Structure

**Modified (single file):**
- `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (3756 baris, 41 slide)

**Not touched:**
- `docs/Panduan-Operasional-HC-PortalHC-KPB.html`
- `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`
- `docs/sosialisasi-screenshots/*.png`
- CSS/JS dalam HTML file (kecuali `TOTAL=41` constant tetap 41)

---

## PHASE A — TEXT FIXES (no structural moves)

Lakukan semua text edit dulu sebelum move slide, karena move akan invalidate line numbers.

### Task 1: K1 — Fix slide 8 menu count

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2168`

- [ ] **Step 1: Edit area-desc**

Find di line 2168:
```html
          <div class="area-desc">16 menu operasional</div>
```
Replace dengan:
```html
          <div class="area-desc">14 menu operasional</div>
```

- [ ] **Step 2: Verify edit**

Run: `grep -n "menu operasional" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected output: `2168:          <div class="area-desc">14 menu operasional</div>`

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 8 menu count 16 → 14 (K1)

Klaim slide 8 'Admin Panel 16 menu operasional' tidak sesuai Views/Admin/Index.cshtml
(Section A=4, B=4, C=5, D=1 = 14 menu)."
```

---

### Task 2: K6 — Fix slide 40 task count

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3664`

- [ ] **Step 1: Edit ref-desc**

Find di line 3664:
```html
          <div class="ref-desc">Reference lengkap 38 task per modul + Glossary + Troubleshooting + URL Cheatsheet</div>
```
Replace dengan:
```html
          <div class="ref-desc">Reference lengkap 42 task per modul + Glossary + Troubleshooting + URL Cheatsheet</div>
```

- [ ] **Step 2: Verify**

Run: `grep -n "42 task" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: line 3664 match.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 40 Panduan task count 38 → 42 (K6)

Panduan Bab 1=4, 2=8, 3=8, 4=3, 5=16, 6=3 → total 42 task."
```

---

### Task 3: K7 — Reword slide 30 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3243`

- [ ] **Step 1: Edit subtitle**

Find di line 3243:
```html
        <p class="slide-subtitle">Tabel silabus per Bagian/Unit/Track + Coaching Guidance (3 tab)</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">ProtonData Index — 3 tab: Status Sync · Silabus · Coaching Guidance</p>
```

- [ ] **Step 2: Verify**

Run: `grep -n "ProtonData Index" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: line 3243 match.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 30 subtitle wording (K7)

'Coaching Guidance (3 tab)' misleading — 3 tab itu parent page,
bukan sub-tab dalam Guidance. Reword ke 3 tab parent explicit."
```

---

### Task 4: K4 — Fix slide 25 step 5 reviewer chain wording

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2982-2984`

- [ ] **Step 1: Edit step-desc**

Find di line 2980-2984:
```html
            <div class="swim-step">
              <span class="step-num-mini">5</span>
              <span class="step-icon-mini">&#128064;</span>
              <div class="step-title">Review Multi-Role</div>
              <div class="step-desc">Coach + SrSpv + SH + HC paralel</div>
```
Replace dengan:
```html
            <div class="swim-step">
              <span class="step-num-mini">5</span>
              <span class="step-icon-mini">&#128064;</span>
              <div class="step-title">Review Sequential Chain</div>
              <div class="step-desc">SrSpv &rarr; SH &rarr; HC sequential (chain enforced)</div>
```

- [ ] **Step 2: Verify**

Run: `grep -n "Sequential Chain" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1 match line ~2983.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 25 step 5 sequential not parallel (K4)

Slide 17 dan slide 25 sebelumnya kontradiksi (sequential vs paralel).
Pilih sequential sebagai canonical — selaras kode dan slide 17."
```

---

### Task 5: R2 — Differentiate slide 18 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2621`

- [ ] **Step 1: Edit subtitle**

Find di line 2621:
```html
        <p class="slide-subtitle">Program 3 tahun &middot; 1 track per role (Panelman / Operator)</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Format penilaian per tahun &middot; 1 track per role (Panelman / Operator)</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 18 subtitle assessment-side framing (R2)

Differentiate dari slide 21 Coaching Proton intro yang juga 'Program 3 tahun'."
```

---

### Task 6: R2 — Differentiate slide 21 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2742`

- [ ] **Step 1: Edit subtitle**

Find di line 2742:
```html
        <p class="slide-subtitle">Program 3 tahun pengembangan kompetensi &middot; 2 track independen</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Program pendampingan 3 tahun &middot; 2 track independen (Panelman + Operator)</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 21 subtitle coaching-side framing (R2)"
```

---

### Task 7: R3 — Add cross-ref di slide 25

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3017`

- [ ] **Step 1: Edit output-bar**

Find di line 3017:
```html
      <div class="output-bar">&#9989; <strong>Output:</strong> sertifikat tahun + eligible naik tahun berikutnya</div>
```
Replace dengan:
```html
      <div class="output-bar">&#9989; <strong>Output:</strong> sertifikat tahun + eligible naik tahun berikutnya &middot; Komparasi 5-aspek Th 1/2/3 &rarr; slide Progresi Kompetensi</div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 25 add cross-ref to Progresi slide (R3)"
```

---

### Task 8: R3 — Add cross-ref di slide 26

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3109`

- [ ] **Step 1: Edit output-bar**

Find di line 3109:
```html
      <div class="output-bar" style="background:linear-gradient(90deg,var(--amber),#b45309);">&#127942; <strong>Output:</strong> Pekerja kompeten penuh + sertifikasi final + eligible role advance</div>
```
Replace dengan:
```html
      <div class="output-bar" style="background:linear-gradient(90deg,var(--amber),#b45309);">&#127942; <strong>Output:</strong> Pekerja kompeten penuh + sertifikasi final + eligible role advance &middot; Komparasi Mahir vs Th 1-2 &rarr; slide Progresi Kompetensi</div>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 26 add cross-ref to Progresi slide (R3)"
```

---

### Task 9: R1 — Trim slide 8 (remove ladder repeat, keep AREA cards only)

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2122-2172`

- [ ] **Step 1: Read current slide 8**

Run: `sed -n '2113,2173p' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: slide 8 dengan ladder-wrap (L1-L6) + area-grid (4 card).

- [ ] **Step 2: Replace slide-body block**

Find lines 2122-2171 (everything between `<div class="slide-body">` opening and `</div>` closing of body):
```html
    <div class="slide-body">
      <div class="ladder-wrap">
        <div class="ladder-step l6">
          <div class="step-num">L6</div>
          <div class="step-role">Coachee</div>
        </div>
        <div class="ladder-step l5">
          <div class="step-num">L5</div>
          <div class="step-role">Coach / Supervisor</div>
        </div>
        <div class="ladder-step l4">
          <div class="step-num">L4</div>
          <div class="step-role">Section Head / Sr Spv</div>
        </div>
        <div class="ladder-step l3">
          <div class="step-num">L3</div>
          <div class="step-role">Direktur / VP / Manager</div>
        </div>
        <div class="ladder-step l2 highlight">
          <div class="step-num">L2</div>
          <div class="step-role">HC</div>
        </div>
        <div class="ladder-step l1">
          <div class="step-num">L1</div>
          <div class="step-role">Admin</div>
        </div>
      </div>
      <div class="area-grid">
        <div class="area-card">
          <div class="area-icon">&#128202;</div>
          <div class="area-name">CMP</div>
          <div class="area-desc">Monitor lintas section</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#127919;</div>
          <div class="area-name">CDP</div>
          <div class="area-desc">Final reviewer chain</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#128193;</div>
          <div class="area-name">Kelola Data</div>
          <div class="area-desc">Silabus &amp; Override</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#9881;</div>
          <div class="area-name">Admin Panel</div>
          <div class="area-desc">14 menu operasional</div>
        </div>
      </div>
    </div>
```

Replace dengan (ladder removed, only AREA cards + cross-ref):
```html
    <div class="slide-body">
      <div class="area-grid" style="grid-template-columns:repeat(2,1fr);gap:18px;max-width:760px;margin:30px auto;">
        <div class="area-card">
          <div class="area-icon">&#128202;</div>
          <div class="area-name">CMP</div>
          <div class="area-desc">Monitor lintas section</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#127919;</div>
          <div class="area-name">CDP</div>
          <div class="area-desc">Final reviewer chain</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#128193;</div>
          <div class="area-name">Kelola Data</div>
          <div class="area-desc">Silabus &amp; Override</div>
        </div>
        <div class="area-card">
          <div class="area-icon">&#9881;</div>
          <div class="area-name">Admin Panel</div>
          <div class="area-desc">14 menu operasional</div>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:18px;">&#128161; Hierarki role lengkap (10 role · 6 level) &rarr; lihat slide Struktur Role di awal deck.</div>
    </div>
```

- [ ] **Step 3: Verify**

Run: `grep -cn "ladder-step" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: `0` (no more ladder-step references — was 6).

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 8 trim role ladder repeat (R1)

Slide 6 sudah punya tangga 10 role · 6 level. Slide 8 sebelumnya ulang
ladder yang sama. Trim ke AREA cards only + cross-ref ke slide 6."
```

---

### Task 10: N1 — Generalize slide 11 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2268`

- [ ] **Step 1: Edit subtitle**

Find di line 2268:
```html
        <p class="slide-subtitle">12 pekerja monitored real-time &mdash; filter cascade + export</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Team view real-time &mdash; filter cascade + export Excel per pekerja</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 11 generalize subtitle (N1)

Remove '12 pekerja' bake-in (snapshot data, akan rot)."
```

---

### Task 11: N1 — Generalize slide 12 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:2302`

- [ ] **Step 1: Edit subtitle**

Find di line 2302:
```html
        <p class="slide-subtitle">25 Sesi &middot; 56% Pass Rate &middot; 6 tab chart &middot; export Excel per chart</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">4 metric global &middot; 6 tab chart &middot; export Excel per chart</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 12 generalize subtitle (N1)

Remove '25 Sesi · 56% Pass Rate' bake-in (snapshot)."
```

---

### Task 12: N1+N4 — Generalize slide 33 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3352`

- [ ] **Step 1: Edit subtitle**

Find di line 3352:
```html
        <p class="slide-subtitle">12 user aktif: 1 Admin, 1 HC, 5 Coachee, sisanya Coach/Manager/Section Head</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">CRUD pekerja + import/export Excel &middot; role badge color-coded</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 33 generalize subtitle (N1+N4)

Remove '12 user aktif: 1 Admin, 1 HC, 5 Coachee' bake-in (snapshot)."
```

---

### Task 13: N1 — Generalize slide 34 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3387`

- [ ] **Step 1: Edit subtitle**

Find di line 3387:
```html
        <p class="slide-subtitle">Real-time tracking 2 grup aktif: ojt test (Open) + Pre-Post UAT (Upcoming)</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Real-time tracking grup assessment &middot; force-close + reset + extra time controls</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 34 generalize subtitle (N1)"
```

---

### Task 14: N1 — Generalize slide 35 subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3421`

- [ ] **Step 1: Edit subtitle**

Find di line 3421:
```html
        <p class="slide-subtitle">Rustam Santiko (GAST) coaching Rino Operator Alkylation &mdash; aktif sejak 10 Apr</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Group by coach &middot; expandable row &middot; bulk import/export Excel</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 35 generalize subtitle (N1)

Remove 'Rustam Santiko... aktif sejak 10 Apr' bake-in (snapshot)."
```

---

### Task 15: N1 — Generalize slide 36 audit log entries

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3465-3470`

- [ ] **Step 1: Edit URL + heading**

Find di line 3465:
```html
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/AuditLog &mdash; 352 entries</span>
```
Replace dengan:
```html
            <span class="mockup-url">localhost:5277/KPB-PortalHC/Admin/AuditLog</span>
```

Find di line 3470:
```html
          <h4>&#128269; Audit Log: 352 Entries Live</h4>
```
Replace dengan:
```html
          <h4>&#128269; Audit Log &mdash; Riwayat Aksi Sistem</h4>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 36 generalize audit log entry count (N1)

Remove '352 entries' bake-in (snapshot)."
```

---

### Task 16: N1 — Generalize slide 37 notif count

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3491`

- [ ] **Step 1: Edit subtitle**

Find di line 3491:
```html
        <p class="slide-subtitle">Bell icon di navbar &mdash; 7 unread notif untuk Admin KPB saat sosialisasi ini</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Bell icon di navbar &mdash; entry point notifikasi terpusat</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 37 generalize subtitle (N1)

Remove '7 unread notif' bake-in (snapshot)."
```

---

### Task 17: N1 — Generalize slide 28 Histori subtitle

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3159`

- [ ] **Step 1: Edit subtitle**

Find di line 3159:
```html
        <p class="slide-subtitle">2 coachee Operator Alkylation Unit aktif &mdash; progress visual Th1/2/3</p>
```
Replace dengan:
```html
        <p class="slide-subtitle">Histori coaching per pekerja &mdash; progress visual Th 1/2/3 + export Excel</p>
```

- [ ] **Step 2: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): slide 28 generalize subtitle (N1)"
```

---

### Task 18: N2 — Fix HTML comment slide 41

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html:3686`

- [ ] **Step 1: Edit comment**

Find di line 3686:
```html
  <!-- ================= SLIDE 23: PENUTUP ================= -->
```
Replace dengan:
```html
  <!-- ================= SLIDE 41: PENUTUP ================= -->
```

- [ ] **Step 2: Audit other SLIDE N comments**

Run: `grep -nE "<!-- =+ SLIDE [0-9]+:" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

Cek setiap comment "SLIDE N:" untuk verify N sesuai posisi slide aktual. Daftar yang TIDAK perlu di-update (sudah benar dengan posisi awal sebelum Phase B move):
- baris 1781 `SLIDE 2: SELAMAT DATANG` (data-slide=2 ✓)
- baris 2112 `SLIDE 3: ROLE HC` (data-slide=8) → mismatch. Fix ke `SLIDE 8: ROLE HC AREA`
- baris 2175 `SLIDE 4: ALUR KERJA HARIAN` (data-slide=9) → fix ke `SLIDE 9`
- baris 2224 `SLIDE 5: CMP OVERVIEW` (data-slide=10) → fix ke `SLIDE 10`
- baris 2262 `SLIDE 6: RECORDS TEAM` (data-slide=11) → fix ke `SLIDE 11`
- baris 2296 `SLIDE 7: ANALYTICS DASHBOARD` (data-slide=12) → fix ke `SLIDE 12`
- baris 2508 `SLIDE 8: PRE/POST TEST` (data-slide=16) → fix ke `SLIDE 16`
- baris 2557 `SLIDE 9: CDP OVERVIEW + REVIEWER CHAIN` (data-slide=17) → fix ke `SLIDE 17`
- baris 3119 `SLIDE 10: COACHING DASHBOARD` (data-slide=27) → fix ke `SLIDE 27`
- baris 3153 `SLIDE 11: HISTORI PROTON + EXPORT` (data-slide=28) → fix ke `SLIDE 28`
- baris 3188 `SLIDE 12: RENEWAL LIFECYCLE` (data-slide=29) → fix ke `SLIDE 29`
- baris 3237 `SLIDE 13: SILABUS + GUIDANCE` (data-slide=30) → fix ke `SLIDE 30`
- baris 3272 `SLIDE 14: OVERRIDE` (data-slide=31) → fix ke `SLIDE 31`
- baris 3312 `SLIDE 15: ADMIN PANEL MAP` (data-slide=32) → fix ke `SLIDE 32`
- baris 3346 `SLIDE 16: KELOLA PEKERJA` (data-slide=33) → fix ke `SLIDE 33`
- baris 3381 `SLIDE 17: ASSESSMENT MONITORING` (data-slide=34) → fix ke `SLIDE 34`
- baris 3415 `SLIDE 18: COACH MAPPING` (data-slide=35) → fix ke `SLIDE 35`
- baris 3450 `SLIDE 19: MAINTENANCE + AUDIT LOG` (data-slide=36) → fix ke `SLIDE 36`
- baris 3485 `SLIDE 20: NOTIFIKASI` (data-slide=37) → fix ke `SLIDE 37`
- baris 3555 `SLIDE 21: TUGAS HC CEPAT` (data-slide=38) → fix ke `SLIDE 38`
- baris 3649 `SLIDE 22: REFERENCE CARD` (data-slide=40) → fix ke `SLIDE 40`
- baris 3686 `SLIDE 23: PENUTUP` (data-slide=41) → fix ke `SLIDE 41`

Update tiap comment dengan Edit. Bulk approach: gunakan sed-like find/replace per comment satu per satu.

- [ ] **Step 3: Verify post-edit**

Run: `grep -nE "<!-- =+ SLIDE [0-9]+:" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | awk '{print $1, $4}'`

Verify angka N di comment match data-slide aktual.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "fix(sosialisasi-hc): align HTML SLIDE N: comments to data-slide (N2)

Sebelumnya comment masih pakai numbering versi 22-slide lama.
Semua comment kini match attribute data-slide aktual."
```

---

### Task 19: N3 — Cleanup MERGED CLUSTER comments + placeholder IDs

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` lines 1840, 2330, 2613, 3604

- [ ] **Step 1: Replace 4 cluster comments**

Find di line 1840:
```html
  <!-- ================= MERGED CLUSTER: KONTEKS (5 slides from File 2 #3-#6, #21, placeholders 901-905 — renumber Task 6) ================= -->
```
Replace dengan:
```html
  <!-- ================= CLUSTER: PENGENALAN KONTEKS ================= -->
```

Find di line 2330:
```html
  <!-- ================= MERGED CLUSTER: ASSESSMENT LIFECYCLE (3 slides from File 2 #7-9, placeholders 906-908 — renumber Task 6) ================= -->
```
Replace dengan:
```html
  <!-- ================= CLUSTER: ASSESSMENT LIFECYCLE ================= -->
```

Find di line 2613:
```html
  <!-- ================= MERGED CLUSTER: PROTON + COACHING (9 slides from File 2 #11-#18 narrative order, placeholders 909-917 — renumber Task 6) ================= -->
```
Replace dengan:
```html
  <!-- ================= CLUSTER: PROTON + COACHING ================= -->
```

Find di line 3604:
```html
  <!-- ================= MERGED: INTEGRASI & KEAMANAN (from File 2 #20) — placeholder 918, renumber Task 6 ================= -->
```
Replace dengan:
```html
  <!-- ================= CLUSTER: INTEGRASI & KEAMANAN ================= -->
```

Juga cari comment placeholder per-slide (`<!-- 901: ...`, `<!-- 902: ...`, dst):

Run: `grep -nE "<!-- 9[0-9]+:" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

Untuk tiap match, Edit hapus seluruh baris atau ganti ke comment singkat `<!-- slide N -->` di mana N = data-slide aktual.

- [ ] **Step 2: Verify**

Run: `grep -nE "placeholder|renumber Task 6|MERGED CLUSTER|<!-- 9[0-9]+:" docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 hasil.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "chore(sosialisasi-hc): cleanup merge process comments (N3)

Hapus MERGED CLUSTER + placeholder ID 901-918 + 'renumber Task 6'
relic. Ganti dengan comment singkat per cluster."
```

---

## PHASE B — STRUCTURAL MOVES

⚠️ **Setelah Phase A selesai, line numbers berubah karena edits. Re-find lokasi sebelum tiap move via grep `data-slide`.**

### Task 20: R4 — Move slide 17 (CDP Reviewer Chain) ke after slide 20

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Locate slide 17**

Run: `grep -n 'data-slide="17"\|data-slide="18"\|data-slide="20"\|data-slide="21"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

Catat: line A = awal slide 17, line B = awal slide 18 (= akhir slide 17 + 1), line C = awal slide 21 (= akhir slide 20 + 1).

- [ ] **Step 2: Read slide 17 block**

Run: `sed -n '${A},$((B-1))p' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (substitusi A & B). Simpan output sebagai blok teks SLIDE_17.

- [ ] **Step 3: Cut slide 17 + paste setelah slide 20**

Pakai Edit dengan `old_string` = blok SLIDE_17 + comment header SLIDE 17, `new_string` = empty (hapus dari posisi lama).

Lalu Edit dengan `old_string` = baris terakhir slide 20 (sebelum slide 21 opening), `new_string` = blok SLIDE_17 + newline + baris terakhir slide 20.

- [ ] **Step 4: Verify slide 17 only appears once**

Run: `grep -cn 'data-slide="17"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): move slide 17 CDP Reviewer Chain after slide 20 (R4)

Sebelumnya Reviewer Chain standalone muncul sebelum Coaching Proton
diperkenalkan — reader bingung 'reviewer for what?'. Pindah ke
transition card sebelum Coaching block (data-slide akan di-renumber
ke 22 di Phase C)."
```

---

### Task 21: R5 — Move slide 22 (IDP & Training Records) ke after slide 29

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Locate slide 22**

Run: `grep -n 'data-slide="22"\|data-slide="23"\|data-slide="29"\|data-slide="30"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 2: Cut slide 22 block + paste after slide 29**

Sama pattern dengan Task 20.

- [ ] **Step 3: Verify**

Run: `grep -cn 'data-slide="22"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): move slide 22 IDP+Training after slide 29 (R5)

IDP+Training Records adalah komponen pelengkap CDP non-coaching.
Sebelumnya terjepit antara Coaching Dual Track dan Hierarki Kompetensi
yang memutus narasi coaching. Pindah ke akhir CDP block."
```

---

### Task 22: U4 — Move slide 39 (Integrasi & Keamanan) ke setelah slide 7

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Locate slide 39 + slide 7**

Run: `grep -n 'data-slide="7"\|data-slide="8"\|data-slide="39"\|data-slide="40"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 2: Cut slide 39 block + paste after slide 7 (before original slide 8)**

- [ ] **Step 3: Verify**

Run: `grep -cn 'data-slide="39"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 1

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): move slide 39 Integrasi+Keamanan to BAGIAN 0 (U4)

Sebelumnya di akhir tanpa BAGIAN label. Konten foundation
(LDAP/Anti-Copy/Audit/RBAC), lebih cocok awal sebagai bagian
pengenalan. Pindah ke setelah Cara Mengakses."
```

---

### Task 23: U1+U2 — Move slide 30 + 31 (Kelola Data) ke dalam Admin Panel cluster

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

Tujuan: slide 30 (Silabus+Guidance) + slide 31 (Override Data) pindah ke posisi setelah slide 35 (Coach Mapping), karena Kelola Data merupakan sub-section Admin Panel Section B.

- [ ] **Step 1: Locate slides**

Run: `grep -n 'data-slide="30"\|data-slide="31"\|data-slide="32"\|data-slide="35"\|data-slide="36"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 2: Cut blok slide 30+31 (2 slide kontigu)**

Cut dari awal slide 30 sampai awal slide 32 (exclusive). Simpan blok.

- [ ] **Step 3: Paste setelah slide 35, sebelum slide 36**

Insert blok di antara akhir slide 35 dan awal slide 36.

- [ ] **Step 4: Verify**

Run: `grep -cn 'data-slide="30"\|data-slide="31"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 2 (each 1)

Sequence check:
Run: `grep -n 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | awk -F'"' '{print $2}' | head -45`

Expected sekuens data-slide (sebelum renumber): 1, 2, 3, 4, 5, 6, 7, 39, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 19, 20, 17, 21, 23, 24, 25, 26, 27, 28, 29, 22, 32, 33, 34, 35, 30, 31, 36, 37, 38, 40, 41

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): move Kelola Data into Admin Panel cluster (U1+U2)

Slide 30 Silabus+Guidance + slide 31 Override Data sebelumnya BAGIAN 3
(label collision dengan Coaching BAGIAN 3). Pindah ke Admin Panel
BAGIAN 4 karena merupakan sub-section B Proton Management."
```

---

## PHASE C — RENUMBER & RELABEL

Setelah semua move selesai, ada 41 slide di physical order baru tapi data-slide attribute + badge + BAGIAN eyebrow masih punya angka lama.

### Task 24: Renumber data-slide attributes sequentially 1-41

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Dump current order**

Run: `grep -nE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | awk -F'"' '{print NR, $2}'`

Hasil: 41 baris, kolom 1 = posisi physical (1-41), kolom 2 = old data-slide.

- [ ] **Step 2: Edit per slide**

Untuk setiap slide di posisi N, ganti `data-slide="OLD"` jadi `data-slide="N"`. Karena nilai OLD tidak unik secara global (akan duplikasi temporary), gunakan strategy: edit dari slide 1 ke 41 berurutan, dengan `old_string` mencakup context unik (e.g., comment header sebelumnya atau title untuk diferensiasi).

Atau pakai approach 2-pass:
1. Pass 1: ganti semua `data-slide="N"` jadi `data-slide="N_OLD"` (tambah suffix)
2. Pass 2: di physical order, ganti `data-slide="N_OLD"` jadi `data-slide="N_NEW"`

Pass 1 commands (PowerShell atau bash):
```bash
sed -i 's/data-slide="\([0-9]\+\)"/data-slide="\1_OLD"/g' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

Pass 2: extract original mapping dari Phase B verify output (Step 4 Task 23), generate sed expressions:
```bash
# Mapping example based on expected sequence:
sed -i \
  -e 's/data-slide="1_OLD"/data-slide="1"/g' \
  -e 's/data-slide="2_OLD"/data-slide="2"/g' \
  -e 's/data-slide="3_OLD"/data-slide="3"/g' \
  -e 's/data-slide="4_OLD"/data-slide="4"/g' \
  -e 's/data-slide="5_OLD"/data-slide="5"/g' \
  -e 's/data-slide="6_OLD"/data-slide="6"/g' \
  -e 's/data-slide="7_OLD"/data-slide="7"/g' \
  -e 's/data-slide="39_OLD"/data-slide="8"/g' \
  -e 's/data-slide="8_OLD"/data-slide="9"/g' \
  -e 's/data-slide="9_OLD"/data-slide="10"/g' \
  -e 's/data-slide="10_OLD"/data-slide="11"/g' \
  -e 's/data-slide="11_OLD"/data-slide="12"/g' \
  -e 's/data-slide="12_OLD"/data-slide="13"/g' \
  -e 's/data-slide="13_OLD"/data-slide="14"/g' \
  -e 's/data-slide="14_OLD"/data-slide="15"/g' \
  -e 's/data-slide="15_OLD"/data-slide="16"/g' \
  -e 's/data-slide="16_OLD"/data-slide="17"/g' \
  -e 's/data-slide="18_OLD"/data-slide="18"/g' \
  -e 's/data-slide="19_OLD"/data-slide="19"/g' \
  -e 's/data-slide="20_OLD"/data-slide="20"/g' \
  -e 's/data-slide="17_OLD"/data-slide="21"/g' \
  -e 's/data-slide="21_OLD"/data-slide="22"/g' \
  -e 's/data-slide="23_OLD"/data-slide="23"/g' \
  -e 's/data-slide="24_OLD"/data-slide="24"/g' \
  -e 's/data-slide="25_OLD"/data-slide="25"/g' \
  -e 's/data-slide="26_OLD"/data-slide="26"/g' \
  -e 's/data-slide="27_OLD"/data-slide="27"/g' \
  -e 's/data-slide="28_OLD"/data-slide="28"/g' \
  -e 's/data-slide="29_OLD"/data-slide="29"/g' \
  -e 's/data-slide="22_OLD"/data-slide="30"/g' \
  -e 's/data-slide="32_OLD"/data-slide="31"/g' \
  -e 's/data-slide="33_OLD"/data-slide="32"/g' \
  -e 's/data-slide="34_OLD"/data-slide="33"/g' \
  -e 's/data-slide="35_OLD"/data-slide="34"/g' \
  -e 's/data-slide="30_OLD"/data-slide="35"/g' \
  -e 's/data-slide="31_OLD"/data-slide="36"/g' \
  -e 's/data-slide="36_OLD"/data-slide="37"/g' \
  -e 's/data-slide="37_OLD"/data-slide="38"/g' \
  -e 's/data-slide="38_OLD"/data-slide="39"/g' \
  -e 's/data-slide="40_OLD"/data-slide="40"/g' \
  -e 's/data-slide="41_OLD"/data-slide="41"/g' \
  docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

**Catatan:** jalankan via Bash tool. Jika sed pre-7 tidak support newlines properly, jalankan satu per satu.

- [ ] **Step 3: Verify sequential**

Run: `grep -nE 'data-slide="[0-9]+"' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | awk -F'"' '{print $2}' | head -45`
Expected: 1 2 3 4 5 ... 41 (monotonic).

Run: `grep -cE 'data-slide=' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 41

Run: `grep -cE '_OLD' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Expected: 0 (no stale suffixes)

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): renumber data-slide attributes 1-41 sequential

Setelah Phase B moves selesai, renumber agar slide N di physical
order N punya data-slide=N. JS counter dan keyboard nav bergantung
pada attribute ini."
```

---

### Task 25: Update SLIDE X/41 badges to match new data-slide

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: List all badges**

Run: `grep -nE 'slide-badge">SLIDE [0-9]+ / 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

39 hasil (slide 1 cover + slide 41 penutup tidak punya badge format ini; slide 20 + slide 26 pakai badge khusus `OFFLINE MODE` / `LEVEL MAHIR`).

- [ ] **Step 2: Edit each badge**

Untuk tiap baris hasil, cek `data-slide="N"` attribute di blok parent slide (cari ke atas dari posisi badge). Update `SLIDE X / 41` → `SLIDE N / 41` di mana N = data-slide aktual.

Cara efisien:
```bash
# Get pairs (data-slide line, badge line) per slide
awk '
  /data-slide="/ { match($0, /data-slide="([0-9]+)"/, m); current=m[1] }
  /slide-badge">SLIDE / { print current, NR, $0 }
' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
```

Untuk tiap pair (current, line), gunakan Edit untuk ganti badge text.

- [ ] **Step 3: Special badges**

Slide dengan badge khusus:
- Slide 20 (di old order) "🎙 OFFLINE MODE" — verify slide ini di new order (post Phase B) data-slide=20 (Alur Proton Th 3). Badge tetap "🎙 OFFLINE MODE" tanpa nomor.
- Slide 26 (old) "🎯 LEVEL MAHIR" — verify di new order = 26 (Alur Coaching Mahir). Badge tetap "🎯 LEVEL MAHIR".

No change needed for special badges.

- [ ] **Step 4: Verify**

Run: `grep -nE 'slide-badge">SLIDE [0-9]+ / 41' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html | awk -F'[">]' '{for(i=1;i<=NF;i++) if($i ~ /SLIDE/) print $i}'`
Spot-check: angka X di "SLIDE X / 41" harus naik monotonik sesuai physical order (kecuali slide khusus).

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): update SLIDE X/41 badges to new numbering"
```

---

### Task 26: Re-label section-eyebrow BAGIAN

**Files:** Modify `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 1: Map slide N → BAGIAN per new structure**

Per spec:
```
BAGIAN 0 — Pengenalan (slide 1-10)
BAGIAN 1 — CMP (slide 11-17)
BAGIAN 2 — Assessment Proton (slide 18-20)
BAGIAN 3 — Coaching Proton / CDP (slide 21-30)
BAGIAN 4 — Admin Panel + Kelola Data (slide 31-37)
BAGIAN 5 — Closing (slide 38-41)
```

- [ ] **Step 2: List current eyebrows**

Run: `grep -nE 'section-eyebrow">' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 3: Edit each eyebrow**

Untuk tiap match, identifikasi data-slide parent. Update `section-eyebrow` text sesuai BAGIAN mapping. Format:
- BAGIAN 0: `<p class="section-eyebrow">BAGIAN 0 — PENGENALAN</p>`
- BAGIAN 1: `<p class="section-eyebrow">BAGIAN 1 — CMP</p>`
- BAGIAN 2: `<p class="section-eyebrow">BAGIAN 2 — ASSESSMENT PROTON</p>`
- BAGIAN 3: `<p class="section-eyebrow">BAGIAN 3 — COACHING PROTON / CDP</p>`
- BAGIAN 4: `<p class="section-eyebrow">BAGIAN 4 — ADMIN PANEL</p>`
- BAGIAN 5: `<p class="section-eyebrow">BAGIAN 5 — CLOSING</p>`

Slide tanpa eyebrow (1, 2, 3, 4, 5, 6, 7, 8) tetap tanpa eyebrow (judul slide sudah jelas).

Slide 41 (Penutup) pakai class `slide penutup`, tidak butuh eyebrow.

- [ ] **Step 4: Verify**

Run: `grep -nE 'section-eyebrow">' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`
Spot-check: BAGIAN nomor naik monotonik 0→5.

Run: `grep -cE 'BAGIAN [0-9]' docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html
git commit -m "refactor(sosialisasi-hc): re-label section-eyebrow BAGIAN to clean 0-5 (U1+U2+U3)

Sebelumnya BAGIAN bouncing 1→2→3→2→3→4. Sekarang monotonik 0→5
sesuai restruktur spec."
```

---

## PHASE D — BROWSER VERIFY

### Task 27: Playwright smoke test

**Files:** No code change. Browser verification only.

- [ ] **Step 1: Start lokal HTTP server**

Run di terminal background:
```bash
cd docs && python -m http.server 8765
```
Expected: `Serving HTTP on 0.0.0.0 port 8765`.

- [ ] **Step 2: Navigate to deck**

Pakai Playwright MCP `browser_navigate` ke `http://localhost:8765/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`.

- [ ] **Step 3: Verify initial state**

Pakai `browser_snapshot` atau `browser_evaluate` JS:
```javascript
({
  total: document.querySelectorAll('.slide').length,
  counter: document.getElementById('slideCounter').textContent,
  firstSlideActive: document.querySelector('.slide.active')?.dataset.slide
})
```
Expected:
- `total`: 41
- `counter`: "1 / 41"
- `firstSlideActive`: "1"

- [ ] **Step 4: Navigate slide 1 → 41 sequentially**

JS loop via `browser_evaluate`:
```javascript
const results = [];
for (let i = 1; i <= 41; i++) {
  document.querySelector('[data-slide="' + i + '"]') ? null : results.push('MISSING slide ' + i);
}
results.length === 0 ? 'OK all 41' : results
```
Expected: `'OK all 41'`.

- [ ] **Step 5: Verify counter updates pada navigasi**

Tekan tombol nextBtn 5 kali via `browser_click` selector `#nextBtn`. Cek counter text setelah tiap klik.

Atau JS:
```javascript
const next = document.getElementById('nextBtn');
const results = [];
for (let i = 0; i < 5; i++) {
  next.click();
  results.push(document.getElementById('slideCounter').textContent);
}
results
```
Expected: `["2 / 41", "3 / 41", "4 / 41", "5 / 41", "6 / 41"]`.

- [ ] **Step 6: Verify BAGIAN labels monotonic**

JS:
```javascript
const eyebrows = Array.from(document.querySelectorAll('.section-eyebrow'))
  .map(e => {
    const slide = e.closest('.slide')?.dataset.slide;
    const text = e.textContent.trim();
    const bagian = text.match(/BAGIAN (\d)/)?.[1];
    return { slide: parseInt(slide), bagian: bagian ? parseInt(bagian) : null, text };
  })
  .filter(e => e.bagian !== null)
  .sort((a, b) => a.slide - b.slide);

// Check monotonic
let lastBagian = -1;
const violations = [];
for (const e of eyebrows) {
  if (e.bagian < lastBagian) violations.push(e);
  lastBagian = e.bagian;
}
violations.length === 0 ? eyebrows : { violations, eyebrows };
```
Expected: BAGIAN naik 0,0,0,...,1,1,...,2,2,2,3,3,...,4,4,...,5,5,5,5 — monotonik tanpa drop.

- [ ] **Step 7: Verify konten salah resolved**

JS check K1 + K6:
```javascript
({
  K1_admin_menu_count: document.body.innerHTML.includes('14 menu operasional') && !document.body.innerHTML.includes('16 menu operasional'),
  K6_panduan_task_count: document.body.innerHTML.includes('42 task per modul') && !document.body.innerHTML.includes('38 task per modul'),
  K4_no_paralel_claim: !document.body.innerHTML.includes('SrSpv + SH + HC paralel')
})
```
Expected: all `true`.

- [ ] **Step 8: Screenshot key slides untuk visual review**

Pakai `browser_take_screenshot` di slide 1 (cover), slide 8 (R1 trimmed), slide 22 (R4 repositioned Reviewer Chain), slide 30 (R5 repositioned IDP), slide 41 (penutup).

- [ ] **Step 9: Stop HTTP server**

Kill background bash process dari Step 1.

- [ ] **Step 10: Final report**

Tulis 1-2 paragraf ringkas:
- Total slide: 41 ✓
- BAGIAN monotonik 0-5 ✓
- Counter, navigasi nextBtn/prevBtn berfungsi ✓
- 4 konten salah resolved (K1, K4, K6, K7) ✓
- Screenshot key slide attached ✓

Tidak ada commit di task ini (verification only).

---

## Self-Review

**Spec coverage check:**

| Spec finding | Plan task |
|---|---|
| K1 (slide 8 menu 16→14) | Task 1 |
| K2 (slide 32 no-op) | n/a (no fix needed) |
| K4 (slide 25 sequential) | Task 4 |
| K6 (slide 40 38→42) | Task 2 |
| K7 (slide 30 subtitle) | Task 3 |
| R1 (slide 8 trim ladder) | Task 9 |
| R2 (slide 18 + 21 subtitle) | Task 5 + Task 6 |
| R3 (slide 25 + 26 cross-ref) | Task 7 + Task 8 |
| R4 (slide 17 move) | Task 20 |
| R5 (slide 22 move) | Task 21 |
| U1+U2 (Kelola Data move) | Task 23 |
| U3 (slide 17 reposition for context) | Task 20 (same as R4) |
| U4 (slide 39 move) | Task 22 |
| N1 (subtitle generalize × 7 slides) | Task 10-17 |
| N2 (slide 23: PENUTUP → 41) | Task 18 |
| N3 (MERGED CLUSTER cleanup) | Task 19 |
| N4 (slide 33 subtitle) | Task 12 (combined with N1) |
| BAGIAN relabel | Task 26 |
| data-slide renumber | Task 24 |
| Badge SLIDE X/41 update | Task 25 |
| Browser verify | Task 27 |

**Coverage:** semua 17 finding mapped ke task. Phase C task (24-26) cover bookkeeping after Phase B moves.

**Placeholder scan:** Tidak ada TBD/TODO. Setiap step punya kode konkret atau command konkret.

**Type consistency:** N/A (HTML edit, no type system).

---

## Risks Recap

1. **Phase A line numbers shift** setelah tiap edit — selalu `grep -n` ulang sebelum next Edit. Pakai `old_string` dengan context unik untuk tahan terhadap shift.
2. **Phase B moves** — siapkan strategy 2-pass (cut + paste) atau atomic Write replace. Verify via `grep -c 'data-slide="N"'` = 1 setelah tiap move.
3. **Phase C renumber** pakai `_OLD` suffix trick untuk avoid value collision.
4. **Phase D Playwright** — jika sandbox blocked, fallback ke manual user verification.

---

## Success Criteria

- Counter "1 / 41" ... "41 / 41" benar di browser.
- 41 data-slide attribute sequential 1-41.
- 4 BAGIAN section-eyebrow monotonic 0-5.
- 0 instance `16 menu operasional`, `38 task per modul`, `Coach + SrSpv + SH + HC paralel`.
- Slide 8 tidak punya ladder L1-L6 lagi.
- Slide 17 (CDP Reviewer Chain) physical posisi setelah slide 20 (now data-slide=21).
- Slide 22 (IDP+Training) physical posisi setelah slide 29 (now data-slide=30).
- Slide 39 (Integrasi+Keamanan) physical posisi setelah slide 7 (now data-slide=8).
- Slide 30+31 (Kelola Data) physical posisi setelah slide 35 (now data-slide=35-36).
- HTML comment `SLIDE N:` match data-slide aktual.
- No `placeholder 9XX` / `renumber Task 6` / `MERGED CLUSTER` artifacts.
- Snapshot data subtitle (12 pekerja, 25 Sesi, 352 entries, dll) di-generalize.
