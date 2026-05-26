# Kickoff PROTON Part 2 — Assessment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Append 10 slide Part 2 (Sl22-31) ke `docs/Kickoff-PROTON.html` (v3.0 21-slide → v4.0 31-slide) menjelaskan sistem ujian assessment Portal HC KPB (CMP + PROTON) berdasarkan grounding codebase actual.

**Architecture:** Single-file HTML deck append-only. Reuse CSS class existing (`.slide`, `.slide-header`, `.slide-title`, `.slide-body`, `.section-eyebrow`, `.slide-badge`, `.accent`). Setiap slide ditambah sebagai `<div class="slide default-deco" data-slide="N">...</div>` di antara Sl21 dan deck-closing `</div>`. JS controller constant `TOTAL = 21` di-update ke `31` + counter UI sync.

**Tech Stack:** HTML5, CSS3 inline (no external lib), vanilla JS (existing controller). Bootstrap Icons CDN (sudah dipakai existing slide). Playwright untuk visual verify.

**Target file:** `docs/Kickoff-PROTON.html`
**Spec:** `docs/superpowers/specs/2026-05-26-kickoff-proton-part2-assessment-design.md`
**Insertion point:** Setelah line 2989 (close `</div>` Sl21), sebelum line 2992 (deck wrapper `</div>`)
**JS constants update:** Line 2996 (`1 / 21` → `1 / 31`), line 3002 (`TOTAL = 21` → `TOTAL = 31`)

---

## File Structure

| File | Responsibility |
|------|----------------|
| `docs/Kickoff-PROTON.html` | Single-file deck. Tambah 10 slide div + update 2 JS const + update counter UI |

**Tidak ada file baru.** Append-only edits.

---

## Constants (Reference per Task)

**Color palette (existing CSS vars):**
- `--teal: #0d9488` — primary brand
- `--teal-dark: #0f766e` — slide title, header
- `--teal-light: #14b8a6` — accent
- `--amber: #f59e0b`, `--amber-dark: #d97706` — accent secondary (eyebrow, .accent span)
- `--green: #10b981` — auto-grade panel
- `--orange: #ea580c` — manual-grade panel, warning
- `--red: #dc2626` — cancel/error
- `--slate: #64748b`, `--slate-dark: #334155` — neutral text
- `--text: #0f172a`, `--text-muted: #475569` — body text
- `--border: #e2e8f0`

**Slide dimensions:** 1280×720 (CSS fixed). Padding 36px × 50px.

**Common slide template:**
```html
<!-- ================= SLIDE N: TITLE ================= -->
<div class="slide default-deco" data-slide="N">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Title <span class="accent">accent</span></h1>
    </div>
    <div class="slide-badge">N / 31</div>
  </div>
  <div class="slide-body">
    <!-- content -->
  </div>
</div>
```

---

## Task 1: Update JS Controller Framework (TOTAL + Counter)

**Goal:** Bump `TOTAL = 21` ke `31` + counter UI initial text `1 / 21` ke `1 / 31`. Tanpa ini, navigation berhenti di slide 21 meski slide 22-31 ada di DOM.

**Files:**
- Modify: `docs/Kickoff-PROTON.html:2996` (counter text)
- Modify: `docs/Kickoff-PROTON.html:3002` (TOTAL const)

- [ ] **Step 1: Update counter UI text**

```html
<!-- Line 2996 OLD: -->
<span class="slide-counter" id="slideCounter">1 / 21</span>
<!-- Line 2996 NEW: -->
<span class="slide-counter" id="slideCounter">1 / 31</span>
```

- [ ] **Step 2: Update TOTAL constant**

```javascript
// Line 3002 OLD:
const TOTAL = 21;
// Line 3002 NEW:
const TOTAL = 31;
```

- [ ] **Step 3: Verify navigation extends to slide 31**

Open `docs/Kickoff-PROTON.html` di browser, tekan `End` (jump to last). Expected: counter shows `31 / 31`. Tapi karena slide 22-31 belum ada di DOM, akan tampil blank — itu expected sebelum Task 2+. Verify navigation **bisa** ke 31 (counter update) tanpa error JS console.

Run: `start docs/Kickoff-PROTON.html` (Windows) atau Playwright snapshot
Expected: counter text "31 / 31" saat tekan End, console error 0

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "chore(kickoff-proton): bump TOTAL 21→31 untuk Part 2 append"
```

---

## Task 2: Slide 22 — Cover Part 2 (Divider)

**Goal:** Visual divider antara Part 1 (Sl1-21) dan Part 2 (Sl22-31). Mirror style cover Sl1 dengan accent berbeda.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — insert setelah line 2989 (close Sl21 div), sebelum deck wrapper close

- [ ] **Step 1: Tambahkan Sl22 HTML**

Insert tepat setelah baris `</div>` penutup Sl21 (line 2989):

```html

<!-- ================= SLIDE 22: COVER PART 2 ================= -->
<div class="slide default-deco" data-slide="22">
  <div class="slide-body" style="justify-content: center; align-items: center; text-align: center; min-height: 100%;">
    <div style="font-size: 84pt; line-height: 1;">📝</div>
    <div style="font-size: 12pt; font-weight: 800; letter-spacing: 4px; color: var(--amber-dark); text-transform: uppercase; margin-top: 24px;">Bagian 2</div>
    <h1 style="font-size: 56pt; font-weight: 900; color: var(--teal-dark); line-height: 1.05; margin: 16px 0; letter-spacing: -1px; max-width: 1000px;">Sistem Ujian <span style="color: var(--amber-dark);">Assessment</span> Portal HC KPB</h1>
    <p style="font-size: 18pt; color: var(--text-muted); margin-top: 8px; max-width: 900px; line-height: 1.4;">Alur &middot; Arsitektur &middot; Cara Ujian</p>
    <div style="display: flex; gap: 18px; margin-top: 40px;">
      <div style="background: var(--teal); color: #fff; padding: 10px 22px; border-radius: 20px; font-size: 11pt; font-weight: 700;">CMP Assessment</div>
      <div style="background: var(--amber-dark); color: #fff; padding: 10px 22px; border-radius: 20px; font-size: 11pt; font-weight: 700;">PROTON Tahap Assessment</div>
    </div>
    <p style="font-size: 10pt; color: var(--text-muted); margin-top: 50px; font-style: italic;">10 slide &middot; Slide 22 &ndash; 31</p>
  </div>
</div>
```

- [ ] **Step 2: Verify Sl22 di browser**

Open file, navigate ke slide 22. Expected: cover divider dengan emoji 📝, eyebrow "BAGIAN 2", title "Sistem Ujian Assessment Portal HC KPB", subtitle "Alur · Arsitektur · Cara Ujian", 2 pill badge CMP+PROTON, counter "22 / 31".

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl22 cover divider Part 2 Assessment"
```

---

## Task 3: Slide 23 — 2 Jenis Assessment + Hirarki PROTON

**Goal:** Anchor mental model — 2-col comparison CMP vs PROTON (atas) + tree diagram hirarki PROTON (bawah).

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl22

- [ ] **Step 1: Tambahkan Sl23 HTML**

```html

<!-- ================= SLIDE 23: 2 JENIS ASSESSMENT + HIRARKI PROTON ================= -->
<div class="slide default-deco" data-slide="23">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">2 Jenis Assessment &amp; <span class="accent">Hirarki PROTON</span></h1>
    </div>
    <div class="slide-badge">23 / 31</div>
  </div>
  <div class="slide-body">
    <!-- 2-col comparison -->
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 18px;">
      <div style="background: linear-gradient(135deg, #ccfbf1 0%, #f0fdfa 100%); border-left: 5px solid var(--teal-dark); border-radius: 10px; padding: 16px 20px;">
        <div style="font-size: 9pt; font-weight: 800; letter-spacing: 2px; color: var(--teal-dark); text-transform: uppercase;">Jenis A</div>
        <h3 style="font-size: 18pt; font-weight: 800; color: var(--teal-dark); margin-top: 4px;">CMP Assessment</h3>
        <table style="margin-top: 10px; font-size: 10.5pt; width: 100%;">
          <tr><td style="font-weight: 700; padding: 4px 0; width: 80px; color: var(--text-muted);">Tujuan</td><td style="padding: 4px 0;">Ujian kompetensi multi-tahap (Pre/Mid/Post)</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Trigger</td><td style="padding: 4px 0;">HC create Session + Package</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Grading</td><td style="padding: 4px 0;">Auto MC/MA + Manual Essay (HC)</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Output</td><td style="padding: 4px 0;"><strong>NomorSertifikat</strong> (score-based)</td></tr>
        </table>
      </div>
      <div style="background: linear-gradient(135deg, #fef3c7 0%, #fffbeb 100%); border-left: 5px solid var(--amber-dark); border-radius: 10px; padding: 16px 20px;">
        <div style="font-size: 9pt; font-weight: 800; letter-spacing: 2px; color: var(--amber-dark); text-transform: uppercase;">Jenis B</div>
        <h3 style="font-size: 18pt; font-weight: 800; color: var(--amber-dark); margin-top: 4px;">PROTON Tahap Assessment</h3>
        <table style="margin-top: 10px; font-size: 10.5pt; width: 100%;">
          <tr><td style="font-weight: 700; padding: 4px 0; width: 80px; color: var(--text-muted);">Tujuan</td><td style="padding: 4px 0;">Ujian akhir per <strong>Tahun</strong> cycle</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Trigger</td><td style="padding: 4px 0;">Deliverable approval lengkap (3 role)</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Grading</td><td style="padding: 4px 0;">MC online (T1-2) atau Interview (T3)</td></tr>
          <tr><td style="font-weight: 700; padding: 4px 0; color: var(--text-muted);">Output</td><td style="padding: 4px 0;"><strong>CompetencyLevel 0-5</strong> + Sertifikasi Mahir (T3)</td></tr>
        </table>
      </div>
    </div>
    <!-- Hirarki PROTON tree -->
    <div style="background: #f8fafc; border: 1px solid var(--border); border-radius: 10px; padding: 14px 20px;">
      <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--slate-dark); text-transform: uppercase; margin-bottom: 10px;">Hirarki PROTON &mdash; Track vs Bagian (Dimensi Berbeda)</div>
      <div style="display: flex; align-items: center; justify-content: space-around; font-size: 10.5pt;">
        <div style="background: var(--teal-dark); color: #fff; padding: 10px 14px; border-radius: 8px; font-weight: 700; text-align: center;">ProtonTrack<br><span style="font-size: 9pt; font-weight: 500; opacity: 0.9;">TrackType + Tahun</span><br><span style="font-size: 8.5pt; opacity: 0.85;">Operator/Panelman &times; T1/T2/T3</span></div>
        <div style="font-size: 20pt; color: var(--slate);">&rarr;</div>
        <div style="background: var(--teal); color: #fff; padding: 10px 14px; border-radius: 8px; font-weight: 700; text-align: center;">Kompetensi<br><span style="font-size: 9pt; font-weight: 500; opacity: 0.9;">Bagian + Unit</span></div>
        <div style="font-size: 20pt; color: var(--slate);">&rarr;</div>
        <div style="background: var(--teal-light); color: #fff; padding: 10px 14px; border-radius: 8px; font-weight: 700; text-align: center;">SubKompetensi</div>
        <div style="font-size: 20pt; color: var(--slate);">&rarr;</div>
        <div style="background: var(--amber); color: #fff; padding: 10px 14px; border-radius: 8px; font-weight: 700; text-align: center;">Deliverable<br><span style="font-size: 8.5pt; font-weight: 500; opacity: 0.95;">leaf submission</span></div>
      </div>
      <div style="font-size: 9.5pt; color: var(--text-muted); margin-top: 10px; font-style: italic; text-align: center;">⚠️ Assessment scope = <strong>Track (Tahun-level)</strong>. Bagian = sub-unit kompetensi <em>dalam</em> track, bukan dimensi assessment.</div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 23. Expected: 2 card side-by-side (CMP teal kiri, PROTON amber kanan), hirarki tree 4-node horizontal di bawah, callout warning bedakan Tahun vs Bagian. Counter "23 / 31".

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl23 jenis assessment + hirarki PROTON"
```

---

## Task 4: Slide 24 — Alur Assessment CMP (E2E)

**Goal:** Horizontal flowchart 7-step alur CMP dengan status badge.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl23

- [ ] **Step 1: Tambahkan Sl24 HTML**

```html

<!-- ================= SLIDE 24: ALUR ASSESSMENT CMP ================= -->
<div class="slide default-deco" data-slide="24">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Alur Assessment <span class="accent">CMP</span> &mdash; End-to-End</h1>
    </div>
    <div class="slide-badge">24 / 31</div>
  </div>
  <div class="slide-body">
    <!-- 7-step flowchart -->
    <div style="display: grid; grid-template-columns: repeat(7, 1fr); gap: 8px; align-items: stretch;">
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px 8px; text-align: center; position: relative;">
        <div style="background: var(--teal-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">1</div>
        <div style="font-size: 10pt; font-weight: 700; color: var(--text); line-height: 1.3;">HC Create</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">Session + Package</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--teal-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">2</div>
        <div style="font-size: 10pt; font-weight: 700;">Coachee Lihat</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">List Assessment</div>
        <div style="font-size: 8pt; background: #e0f2fe; color: #075985; padding: 2px 6px; border-radius: 10px; margin-top: 6px; display: inline-block;">Open / Upcoming</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--teal-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">3</div>
        <div style="font-size: 10pt; font-weight: 700;">Start Exam</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">Timer mulai</div>
        <div style="font-size: 8pt; background: #fef3c7; color: #92400e; padding: 2px 6px; border-radius: 10px; margin-top: 6px; display: inline-block;">InProgress</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--teal-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">4</div>
        <div style="font-size: 10pt; font-weight: 700;">Jawab Soal</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">MC / MA / Essay</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--teal-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">5</div>
        <div style="font-size: 10pt; font-weight: 700;">Submit</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">Auto saat timer habis</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--amber-dark); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--amber-dark); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">6</div>
        <div style="font-size: 10pt; font-weight: 700;">Grading</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">Auto + Essay manual</div>
        <div style="font-size: 8pt; background: #fed7aa; color: #9a3412; padding: 2px 6px; border-radius: 10px; margin-top: 6px; display: inline-block;">Menunggu Penilaian</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--green); border-radius: 10px; padding: 12px 8px; text-align: center;">
        <div style="background: var(--green); color: #fff; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; margin: 0 auto 8px;">7</div>
        <div style="font-size: 10pt; font-weight: 700;">Result + Sertifikat</div>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">NomorSertifikat jika IsPassed</div>
        <div style="font-size: 8pt; background: #d1fae5; color: #065f46; padding: 2px 6px; border-radius: 10px; margin-top: 6px; display: inline-block;">Completed</div>
      </div>
    </div>
    <!-- Footer caption -->
    <div style="background: #f8fafc; border-left: 4px solid var(--teal-dark); padding: 12px 18px; border-radius: 6px;">
      <div style="font-size: 11pt; color: var(--text); line-height: 1.5;">
        🔧 <strong>Engine:</strong> <code style="background: #e0f2fe; padding: 2px 6px; border-radius: 4px; font-size: 10pt;">CMPController.StartExam</code> &rarr; <code style="background: #e0f2fe; padding: 2px 6px; border-radius: 4px; font-size: 10pt;">SubmitExam</code> &rarr; <code style="background: #e0f2fe; padding: 2px 6px; border-radius: 4px; font-size: 10pt;">GradingService</code> &rarr; <code style="background: #e0f2fe; padding: 2px 6px; border-radius: 4px; font-size: 10pt;">Results / Certificate</code>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 24. Expected: 7 card horizontal dengan number badge teal, status pill (Open/Upcoming, InProgress, Menunggu Penilaian, Completed), card 7 hijau, footer code engine path.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl24 alur CMP E2E 7-step flowchart"
```

---

## Task 5: Slide 25 — Alur Assessment PROTON (Tahun 1-2 vs Tahun 3)

**Goal:** 2-column parallel flowchart bedakan PROTON Tahun 1-2 (MC online) vs Tahun 3 (Interview offline).

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl24

- [ ] **Step 1: Tambahkan Sl25 HTML**

```html

<!-- ================= SLIDE 25: ALUR ASSESSMENT PROTON ================= -->
<div class="slide default-deco" data-slide="25">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Alur Assessment <span class="accent">PROTON</span> &mdash; Tahun 1-2 vs Tahun 3</h1>
    </div>
    <div class="slide-badge">25 / 31</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
      <!-- Kiri: Tahun 1-2 -->
      <div style="background: linear-gradient(135deg, #ccfbf1 0%, #f0fdfa 100%); border: 2px solid var(--teal-dark); border-radius: 12px; padding: 14px 18px;">
        <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--teal-dark); text-transform: uppercase;">Tahun 1 &amp; Tahun 2</div>
        <h3 style="font-size: 17pt; font-weight: 800; color: var(--teal-dark); margin: 4px 0 12px;">MC Online (Auto-Grade)</h3>
        <div style="display: flex; flex-direction: column; gap: 8px;">
          <div style="background: #fff; border-left: 4px solid var(--teal-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--teal-dark);">1.</strong> Coachee submit <strong>Deliverables</strong> (ProtonDeliverableProgress)</div>
          <div style="background: #fff; border-left: 4px solid var(--orange); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--orange);">2.</strong> <strong>3-Role Approval:</strong> HC + SrSpv + SectionHead</div>
          <div style="background: #fff; border-left: 4px solid var(--teal-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--teal-dark);">3.</strong> HC create Session <code style="font-size: 9.5pt;">Category=Assessment Proton</code> + ProtonTrackId</div>
          <div style="background: #fff; border-left: 4px solid var(--teal-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--teal-dark);">4.</strong> Coachee ujian MC online (engine sama CMP)</div>
          <div style="background: #fff; border-left: 4px solid var(--teal-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--teal-dark);">5.</strong> Auto-grade via GradingService</div>
          <div style="background: #fff; border-left: 4px solid var(--green); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--green);">6.</strong> HC create <strong>ProtonFinalAssessment</strong></div>
        </div>
      </div>
      <!-- Kanan: Tahun 3 -->
      <div style="background: linear-gradient(135deg, #fef3c7 0%, #fffbeb 100%); border: 2px solid var(--amber-dark); border-radius: 12px; padding: 14px 18px;">
        <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--amber-dark); text-transform: uppercase;">Tahun 3</div>
        <h3 style="font-size: 17pt; font-weight: 800; color: var(--amber-dark); margin: 4px 0 12px;">Interview Offline (Manual)</h3>
        <div style="display: flex; flex-direction: column; gap: 8px;">
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">1.</strong> Coachee submit <strong>Deliverables</strong></div>
          <div style="background: #fff; border-left: 4px solid var(--orange); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--orange);">2.</strong> <strong>3-Role Approval:</strong> HC + SrSpv + SectionHead</div>
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">3.</strong> Interview offline (HC + coachee)</div>
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">4.</strong> HC <code style="font-size: 9.5pt;">SubmitInterviewResults()</code> &rarr; <code style="font-size: 9.5pt;">InterviewResultsJson</code></div>
          <div style="background: #fff; border-left: 4px solid var(--amber-dark); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--amber-dark);">5.</strong> HC create ProtonFinalAssessment + <strong>CompetencyLevel 0-5</strong></div>
          <div style="background: #fff; border-left: 4px solid var(--green); padding: 8px 12px; border-radius: 4px;"><strong style="color: var(--green);">6.</strong> 🏅 <strong>Sertifikasi Mahir</strong> granted</div>
        </div>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 25. Expected: 2 column parallel, kiri teal (Tahun 1-2 MC) 6-step, kanan amber (Tahun 3 Interview) 6-step, step terakhir hijau (ProtonFinalAssessment / Sertifikasi Mahir). 3-Role Approval orange di step 2 di kedua kolom.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl25 alur PROTON 2-col (T1-2 MC vs T3 Interview)"
```

---

## Task 6: Slide 26 — Sistem Ujian — Anatomi Soal (ER Diagram)

**Goal:** ER diagram horizontal: AssessmentSession → Package → Question → Option. Label field penting di samping.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl25

- [ ] **Step 1: Tambahkan Sl26 HTML**

```html

<!-- ================= SLIDE 26: SISTEM UJIAN — ANATOMI SOAL ================= -->
<div class="slide default-deco" data-slide="26">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Sistem Ujian &mdash; <span class="accent">Anatomi Soal</span></h1>
    </div>
    <div class="slide-badge">26 / 31</div>
  </div>
  <div class="slide-body">
    <!-- ER Diagram horizontal -->
    <div style="display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; gap: 16px; align-items: stretch;">
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 12px;">
        <div style="background: var(--teal-dark); color: #fff; font-size: 11pt; font-weight: 800; padding: 6px 10px; border-radius: 5px; text-align: center;">AssessmentSession</div>
        <ul style="margin-top: 10px; padding-left: 16px; font-size: 9.5pt; color: var(--text); line-height: 1.5;">
          <li><strong>Status</strong></li>
          <li>DurationMinutes</li>
          <li>ExtraTimeMinutes</li>
          <li>PassPercentage</li>
          <li>GenerateCertificate</li>
          <li>NomorSertifikat</li>
          <li>ProtonTrackId</li>
          <li>TahunKe</li>
          <li>Category</li>
          <li>InterviewResultsJson</li>
        </ul>
      </div>
      <div style="background: #fff; border: 2px solid var(--teal); border-radius: 10px; padding: 12px;">
        <div style="background: var(--teal); color: #fff; font-size: 11pt; font-weight: 800; padding: 6px 10px; border-radius: 5px; text-align: center;">AssessmentPackage</div>
        <ul style="margin-top: 10px; padding-left: 16px; font-size: 9.5pt; color: var(--text); line-height: 1.5;">
          <li>Name</li>
          <li>Order</li>
          <li>WeightPercentage</li>
          <li><em>multi-package per session</em></li>
        </ul>
        <div style="font-size: 9pt; color: var(--text-muted); margin-top: 12px; text-align: center; font-style: italic;">1 Session &harr; many Package</div>
      </div>
      <div style="background: #fff; border: 2px solid var(--amber); border-radius: 10px; padding: 12px;">
        <div style="background: var(--amber); color: #fff; font-size: 11pt; font-weight: 800; padding: 6px 10px; border-radius: 5px; text-align: center;">PackageQuestion</div>
        <ul style="margin-top: 10px; padding-left: 16px; font-size: 9.5pt; color: var(--text); line-height: 1.5;">
          <li><strong>Type:</strong><br>&middot; MultipleChoice<br>&middot; MultipleAnswer<br>&middot; Essay</li>
          <li>ScoreValue</li>
          <li><strong>Rubrik</strong> (Essay only)</li>
          <li>QuestionText</li>
        </ul>
      </div>
      <div style="background: #fff; border: 2px solid var(--amber-dark); border-radius: 10px; padding: 12px;">
        <div style="background: var(--amber-dark); color: #fff; font-size: 11pt; font-weight: 800; padding: 6px 10px; border-radius: 5px; text-align: center;">PackageOption</div>
        <ul style="margin-top: 10px; padding-left: 16px; font-size: 9.5pt; color: var(--text); line-height: 1.5;">
          <li>OptionText</li>
          <li><strong>IsCorrect</strong> (auto-grade key)</li>
          <li>Order</li>
          <li><em>untuk MC/MA only</em></li>
        </ul>
      </div>
    </div>
    <!-- Relasi caption -->
    <div style="background: #f8fafc; border: 1px solid var(--border); padding: 10px 16px; border-radius: 8px; font-size: 10.5pt; color: var(--text); text-align: center;">
      <strong>Relasi:</strong> AssessmentSession <span style="color: var(--teal-dark); font-weight: 800;">1&mdash;*</span> AssessmentPackage <span style="color: var(--teal); font-weight: 800;">1&mdash;*</span> PackageQuestion <span style="color: var(--amber); font-weight: 800;">1&mdash;*</span> PackageOption
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 26. Expected: 4 entity card horizontal (Session teal-dark, Package teal, Question amber, Option amber-dark), field list di tiap card, relasi caption footer.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl26 ER diagram anatomi soal"
```

---

## Task 7: Slide 27 — Sistem Ujian — Grading Engine

**Goal:** Split panel Auto (kiri hijau) vs Manual (kanan orange). Footer PassPercentage threshold.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl26

- [ ] **Step 1: Tambahkan Sl27 HTML**

```html

<!-- ================= SLIDE 27: SISTEM UJIAN — GRADING ENGINE ================= -->
<div class="slide default-deco" data-slide="27">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Sistem Ujian &mdash; <span class="accent">Grading Engine</span></h1>
    </div>
    <div class="slide-badge">27 / 31</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
      <!-- Auto -->
      <div style="background: linear-gradient(135deg, #d1fae5 0%, #ecfdf5 100%); border: 2px solid var(--green); border-radius: 12px; padding: 16px 20px;">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="font-size: 28pt;">⚙️</div>
          <div>
            <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--green-dark); text-transform: uppercase;">Auto Grading</div>
            <h3 style="font-size: 17pt; font-weight: 800; color: var(--green-dark);">Mesin Otomatis</h3>
          </div>
        </div>
        <div style="display: flex; flex-direction: column; gap: 8px; margin-top: 14px;">
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--green);">
            <strong style="color: var(--green-dark);">MultipleChoice (MC)</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">Match <code>PackageOption.IsCorrect</code> &rarr; full ScoreValue</span>
          </div>
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--green);">
            <strong style="color: var(--green-dark);">MultipleAnswer (MA)</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">Exact-set match seluruh option ber-IsCorrect=true</span>
          </div>
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--green);">
            <strong style="color: var(--green-dark);">ElemenTeknisScore</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">Breakdown skor per grup KKJ (kompetensi reference)</span>
          </div>
        </div>
      </div>
      <!-- Manual -->
      <div style="background: linear-gradient(135deg, #fed7aa 0%, #ffedd5 100%); border: 2px solid var(--orange); border-radius: 12px; padding: 16px 20px;">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="font-size: 28pt;">✍️</div>
          <div>
            <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--orange); text-transform: uppercase;">Manual Grading</div>
            <h3 style="font-size: 17pt; font-weight: 800; color: var(--orange);">HC Input Required</h3>
          </div>
        </div>
        <div style="display: flex; flex-direction: column; gap: 8px; margin-top: 14px;">
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--orange);">
            <strong style="color: var(--orange);">Essay</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">HC input <code>EssayScore</code> 0..ScoreValue via <strong>Rubrik</strong></span>
          </div>
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--orange);">
            <strong style="color: var(--orange);">Interview Tahun 3 (PROTON)</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">HC input <code>InterviewResultsDto</code> per kompetensi</span>
          </div>
          <div style="background: #fff; padding: 10px 14px; border-radius: 6px; border-left: 4px solid var(--orange);">
            <strong style="color: var(--orange);">CompetencyLevel (PROTON)</strong><br>
            <span style="font-size: 10pt; color: var(--text-muted);">HC grant <code>CompetencyLevelGranted</code> [Range(0, 5)]</span>
          </div>
        </div>
      </div>
    </div>
    <!-- Threshold footer -->
    <div style="background: var(--teal-dark); color: #fff; padding: 12px 20px; border-radius: 8px; font-size: 12pt; text-align: center;">
      <strong>Threshold:</strong> <code style="background: rgba(255,255,255,0.2); padding: 2px 8px; border-radius: 4px;">TotalScore &ge; PassPercentage</code> &rarr; <strong style="color: #fef3c7;">IsPassed = true</strong> &rarr; trigger NomorSertifikat
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 27. Expected: 2-col split panel (Auto hijau kiri 3 item: MC/MA/ElemenTeknis, Manual orange kanan 3 item: Essay/Interview/CompetencyLevel), footer banner teal-dark threshold formula.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl27 grading engine auto vs manual"
```

---

## Task 8: Slide 28 — Sistem Ujian — Status Lifecycle & Timer

**Goal:** State machine diagram + timer mockup. Status enum string value exact (Indonesian "Menunggu Penilaian").

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl27

- [ ] **Step 1: Tambahkan Sl28 HTML**

```html

<!-- ================= SLIDE 28: STATUS LIFECYCLE & TIMER ================= -->
<div class="slide default-deco" data-slide="28">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Status Lifecycle &amp; <span class="accent">Timer</span></h1>
    </div>
    <div class="slide-badge">28 / 31</div>
  </div>
  <div class="slide-body">
    <!-- State machine -->
    <div style="background: #f8fafc; border: 1px solid var(--border); border-radius: 10px; padding: 16px 20px;">
      <div style="font-size: 10pt; font-weight: 800; letter-spacing: 2px; color: var(--slate-dark); text-transform: uppercase; margin-bottom: 14px;">State Machine &mdash; 6 Status (string value exact)</div>
      <div style="display: flex; align-items: center; justify-content: space-between; gap: 6px; font-size: 10pt; flex-wrap: nowrap;">
        <div style="background: #e0e7ff; color: #3730a3; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center; min-width: 90px;">Upcoming</div>
        <div style="font-size: 18pt; color: var(--slate);">&rarr;</div>
        <div style="background: #e0f2fe; color: #075985; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center; min-width: 90px;">Open</div>
        <div style="font-size: 18pt; color: var(--slate);">&rarr;</div>
        <div style="background: #fef3c7; color: #92400e; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center; min-width: 90px;">InProgress</div>
        <div style="font-size: 18pt; color: var(--slate);">&rarr;</div>
        <div style="display: flex; flex-direction: column; gap: 6px;">
          <div style="background: #fed7aa; color: #9a3412; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center; font-size: 9.5pt;">"Menunggu Penilaian"<br><span style="font-size: 8pt; font-weight: 500;">(PendingGrading)</span></div>
          <div style="background: #d1fae5; color: #065f46; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center;">Completed</div>
          <div style="background: #fee2e2; color: #991b1b; padding: 8px 12px; border-radius: 6px; font-weight: 700; text-align: center;">Cancelled</div>
        </div>
      </div>
      <div style="font-size: 9.5pt; color: var(--text-muted); margin-top: 10px; font-style: italic; text-align: center;">
        ⚠️ Note: <code>PendingGrading</code> string value Indonesian (<strong>"Menunggu Penilaian"</strong>), bukan English. Reference: <code>Models/AssessmentConstants.cs:18</code>
      </div>
    </div>
    <!-- Timer panel -->
    <div style="display: grid; grid-template-columns: 1.4fr 1fr; gap: 16px;">
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 14px 18px;">
        <h4 style="font-size: 13pt; font-weight: 800; color: var(--teal-dark);">⏱️ Timer Control</h4>
        <ul style="margin-top: 8px; padding-left: 18px; font-size: 10.5pt; line-height: 1.7;">
          <li><strong>DurationMinutes</strong> &mdash; server-enforced, fixed per session</li>
          <li><strong>ExtraTimeMinutes</strong> (Phase 302) &mdash; akomodasi khusus per coachee</li>
          <li>Client countdown real-time + server validation saat submit</li>
          <li><strong>Auto-submit</strong> saat timer habis &mdash; partial jawaban tetap dinilai</li>
        </ul>
      </div>
      <div style="background: linear-gradient(135deg, var(--teal-dark), var(--teal)); color: #fff; border-radius: 10px; padding: 14px 18px; text-align: center; display: flex; flex-direction: column; justify-content: center;">
        <div style="font-size: 9.5pt; font-weight: 700; opacity: 0.9; letter-spacing: 2px;">SISA WAKTU</div>
        <div style="font-size: 48pt; font-weight: 900; letter-spacing: -2px; margin-top: 6px; font-family: 'Courier New', monospace;">00:47:23</div>
        <div style="font-size: 9pt; opacity: 0.85; margin-top: 4px;">⏰ ExtraTime +15 min applied</div>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 28. Expected: state machine horizontal 6 status (Upcoming/Open/InProgress fork ke Menunggu Penilaian + Completed + Cancelled), note warning Indonesian string, panel timer dengan mockup countdown 48pt mono font.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl28 status lifecycle + timer (6 state)"
```

---

## Task 9: Slide 29 — Cara Ujian — Coachee POV

**Goal:** 8-step mockup numbered grid (2 baris × 4 panel).

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl28

- [ ] **Step 1: Tambahkan Sl29 HTML**

```html

<!-- ================= SLIDE 29: CARA UJIAN — COACHEE POV ================= -->
<div class="slide default-deco" data-slide="29">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Cara Ujian &mdash; <span class="accent">Coachee POV</span></h1>
    </div>
    <div class="slide-badge">29 / 31</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: repeat(4, 1fr); grid-template-rows: 1fr 1fr; gap: 10px;">
      <div style="background: #fff; border: 1.5px solid var(--border); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--teal-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 1</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">🔐</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Login Portal</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">SSO Pertamina</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--border); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--teal-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 2</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">📚</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Menu CMP / PROTON</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Buka Assessment</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--border); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--teal-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 3</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">📋</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Lihat List</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Open / Upcoming / Completed</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--border); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--teal-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 4</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">▶️</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Klik Start Exam</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Status InProgress</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--amber); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--amber-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 5</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">⚠️</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Confirm Modal</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Timer akan mulai</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--amber); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--amber-dark); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 6</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">✏️</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Jawab Soal</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">+ Countdown timer</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--green); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--green); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 7</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">📤</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Submit</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Auto saat timer habis</div>
      </div>
      <div style="background: #fff; border: 1.5px solid var(--green); border-radius: 8px; padding: 10px 12px; position: relative;">
        <div style="position: absolute; top: -10px; left: 10px; background: var(--green); color: #fff; font-weight: 800; padding: 3px 10px; border-radius: 10px; font-size: 9pt;">STEP 8</div>
        <div style="font-size: 22pt; text-align: center; margin-top: 8px;">🏆</div>
        <div style="font-size: 11pt; font-weight: 700; text-align: center; margin-top: 4px;">Lihat Result</div>
        <div style="font-size: 9pt; color: var(--text-muted); text-align: center; margin-top: 2px;">Score + Sertifikat</div>
      </div>
    </div>
    <div style="background: #f0fdfa; border-left: 4px solid var(--teal-dark); padding: 8px 16px; border-radius: 4px; font-size: 10pt; color: var(--text); text-align: center;">
      💡 <strong>Tip:</strong> Step 5 modal confirm tidak bisa di-cancel mid-exam. Pastikan koneksi stabil sebelum Start Exam.
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 29. Expected: 8 panel 2-row × 4-col, step 1-4 teal-dark border, step 5-6 amber, step 7-8 green. Setiap panel ada step badge, emoji, judul, sub-text. Tip footer di-style callout.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl29 cara ujian coachee POV 8-step"
```

---

## Task 10: Slide 30 — Cara Grading — HC POV + NomorSertifikat

**Goal:** HC workflow + sample NomorSertifikat format real.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl29

- [ ] **Step 1: Tambahkan Sl30 HTML**

```html

<!-- ================= SLIDE 30: CARA GRADING — HC POV ================= -->
<div class="slide default-deco" data-slide="30">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Cara Grading &mdash; <span class="accent">HC POV</span></h1>
    </div>
    <div class="slide-badge">30 / 31</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1.3fr 1fr; gap: 16px;">
      <!-- HC Workflow -->
      <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 10px; padding: 14px 18px;">
        <h4 style="font-size: 14pt; font-weight: 800; color: var(--teal-dark);">🛠️ HC Grading Workflow</h4>
        <div style="display: flex; flex-direction: column; gap: 8px; margin-top: 12px;">
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--teal-dark); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">1</div><div style="font-size: 11pt;">Buka dashboard <strong>Admin Assessment</strong></div></div>
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--teal-dark); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">2</div><div style="font-size: 11pt;">Filter status <span style="background: #fed7aa; color: #9a3412; padding: 2px 8px; border-radius: 8px; font-size: 9.5pt;"><strong>"Menunggu Penilaian"</strong></span></div></div>
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--teal-dark); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">3</div><div style="font-size: 11pt;">Buka jawaban <strong>Essay</strong> coachee</div></div>
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--teal-dark); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">4</div><div style="font-size: 11pt;">Input <code>EssayScore</code> 0..ScoreValue via <strong>Rubrik</strong></div></div>
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--teal-dark); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">5</div><div style="font-size: 11pt;">Finalize session &rarr; trigger <code>CertNumberHelper.Build()</code></div></div>
          <div style="display: flex; gap: 10px; align-items: flex-start;"><div style="background: var(--green); color: #fff; min-width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 10pt;">6</div><div style="font-size: 11pt;">NomorSertifikat auto-generated + retry 3x idempotent</div></div>
        </div>
      </div>
      <!-- NomorSertifikat sample -->
      <div style="background: linear-gradient(135deg, var(--teal-dark), var(--teal)); color: #fff; border-radius: 10px; padding: 14px 18px;">
        <div style="font-size: 9.5pt; font-weight: 800; letter-spacing: 2px; opacity: 0.9; text-transform: uppercase;">Format NomorSertifikat</div>
        <div style="font-family: 'Courier New', monospace; font-size: 18pt; font-weight: 800; margin-top: 10px; padding: 10px 14px; background: rgba(255,255,255,0.15); border-radius: 6px; text-align: center;">KPB / {seq:D3} / {Roman} / {Year}</div>
        <div style="font-size: 10pt; opacity: 0.9; margin-top: 10px; font-weight: 700;">Contoh real:</div>
        <ul style="font-family: 'Courier New', monospace; font-size: 11.5pt; padding-left: 20px; margin-top: 6px; line-height: 1.7;">
          <li>KPB/001/V/2026</li>
          <li>KPB/042/XII/2026</li>
          <li>KPB/127/III/2027</li>
        </ul>
        <div style="font-size: 8.5pt; opacity: 0.85; margin-top: 10px; font-style: italic;">seq = 3-digit zero-padded sequence per year<br>Roman = bulan Romawi (I&hellip;XII)</div>
      </div>
    </div>
    <!-- Idempotent guarantee -->
    <div style="background: #f8fafc; border-left: 4px solid var(--green); padding: 10px 16px; border-radius: 4px; font-size: 10pt; color: var(--text);">
      🔒 <strong>Race-condition safe:</strong> <code>GradingService</code> pakai <code>SetProperty + WHERE NomorSertifikat IS NULL</code> (retry 3x). Concurrent grading tidak duplicate cert.
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 30. Expected: 2-col, kiri HC workflow 6-step (step 6 hijau highlight), kanan sample NomorSertifikat dengan format box mono + 3 example, footer race-condition guarantee.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl30 cara grading HC POV + NomorSertifikat format"
```

---

## Task 11: Slide 31 — Outcome — Sertifikat & CompetencyLevel

**Goal:** Closing slide 2-col CMP vs PROTON outcome + tie-back ke Sl19.

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — append setelah Sl30

- [ ] **Step 1: Tambahkan Sl31 HTML**

```html

<!-- ================= SLIDE 31: OUTCOME — SERTIFIKAT & COMPETENCYLEVEL ================= -->
<div class="slide default-deco" data-slide="31">
  <div class="slide-header">
    <div>
      <div class="section-eyebrow">PART 2 &mdash; ASSESSMENT</div>
      <h1 class="slide-title">Outcome &mdash; <span class="accent">Sertifikat &amp; CompetencyLevel</span></h1>
    </div>
    <div class="slide-badge">31 / 31</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
      <!-- CMP outcome -->
      <div style="background: linear-gradient(135deg, #ccfbf1 0%, #f0fdfa 100%); border: 2px solid var(--teal-dark); border-radius: 12px; padding: 16px 20px;">
        <div style="font-size: 9pt; font-weight: 800; letter-spacing: 2px; color: var(--teal-dark); text-transform: uppercase;">Output CMP</div>
        <h3 style="font-size: 17pt; font-weight: 800; color: var(--teal-dark); margin-top: 4px;">🏆 NomorSertifikat Resmi</h3>
        <!-- Sertifikat mockup -->
        <div style="background: #fff; border: 2px solid var(--teal-dark); border-radius: 8px; padding: 14px; margin-top: 12px; text-align: center;">
          <div style="font-size: 8.5pt; color: var(--text-muted); letter-spacing: 2px; font-weight: 700;">SERTIFIKAT KOMPETENSI</div>
          <div style="font-size: 9pt; color: var(--text-muted); margin-top: 4px;">Diberikan kepada</div>
          <div style="font-size: 14pt; font-weight: 800; color: var(--teal-dark); margin-top: 4px;">Andi Pratama</div>
          <div style="font-size: 9pt; color: var(--text-muted); margin-top: 8px;">Atas pencapaian kompetensi</div>
          <div style="font-size: 11pt; font-weight: 700; margin-top: 4px;">Operator Senior &mdash; Refining</div>
          <div style="background: var(--teal-dark); color: #fff; font-family: 'Courier New', monospace; font-size: 10pt; font-weight: 700; padding: 6px 10px; border-radius: 4px; margin-top: 10px; display: inline-block;">KPB/001/V/2026</div>
        </div>
        <ul style="font-size: 10pt; color: var(--text); margin-top: 12px; padding-left: 18px; line-height: 1.6;">
          <li>Score breakdown per <strong>Elemen Teknis</strong></li>
          <li>IsPassed boolean &ge; PassPercentage</li>
        </ul>
      </div>
      <!-- PROTON outcome -->
      <div style="background: linear-gradient(135deg, #fef3c7 0%, #fffbeb 100%); border: 2px solid var(--amber-dark); border-radius: 12px; padding: 16px 20px;">
        <div style="font-size: 9pt; font-weight: 800; letter-spacing: 2px; color: var(--amber-dark); text-transform: uppercase;">Output PROTON</div>
        <h3 style="font-size: 17pt; font-weight: 800; color: var(--amber-dark); margin-top: 4px;">🏅 CompetencyLevel &amp; Sertifikasi Mahir</h3>
        <!-- Level badge mockup -->
        <div style="background: #fff; border: 2px solid var(--amber-dark); border-radius: 8px; padding: 14px; margin-top: 12px;">
          <div style="font-size: 9pt; color: var(--text-muted); text-align: center; font-weight: 700;">CompetencyLevelGranted</div>
          <div style="display: flex; gap: 4px; justify-content: center; margin-top: 8px;">
            <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--amber-dark); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800;">0</div>
            <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--amber-dark); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800;">1</div>
            <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--amber-dark); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800;">2</div>
            <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--amber-dark); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800;">3</div>
            <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--amber-dark); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800;">4</div>
            <div style="width: 44px; height: 44px; border-radius: 50%; background: var(--green); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 14pt; box-shadow: 0 4px 12px rgba(16,185,129,0.4); transform: scale(1.1);">5</div>
          </div>
          <div style="font-size: 10pt; text-align: center; margin-top: 8px; font-weight: 700; color: var(--green);">Level 5 &mdash; Mahir</div>
          <div style="text-align: center; font-size: 24pt; margin-top: 6px;">🏅</div>
          <div style="font-size: 10pt; text-align: center; font-weight: 800; color: var(--amber-dark);">Sertifikasi Mahir</div>
          <div style="font-size: 8.5pt; text-align: center; color: var(--text-muted);">(Tahun 3 Final Assessment)</div>
        </div>
        <ul style="font-size: 10pt; color: var(--text); margin-top: 12px; padding-left: 18px; line-height: 1.6;">
          <li>Linked <code>KkjMatrixItem</code> reference</li>
          <li>Granted by HC manual approval</li>
        </ul>
      </div>
    </div>
    <!-- Tie-back -->
    <div style="background: var(--teal-dark); color: #fff; padding: 10px 18px; border-radius: 8px; font-size: 11pt; text-align: center;">
      🔗 <strong>Tie-back:</strong> Lihat <strong>Slide 19 (Outcome &mdash; Sertifikasi Mahir &amp; IDP)</strong> di Part 1 untuk konteks bisnis lengkap
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verify di browser**

Navigate ke slide 31. Expected: 2-col CMP (kiri teal) + PROTON (kanan amber), kiri sertifikat mock dengan nomor KPB/001/V/2026, kanan level badge 0-5 (level 5 hijau highlight + scale 1.1), tie-back banner teal-dark footer.

- [ ] **Step 3: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): Sl31 outcome sertifikat + CompetencyLevel + tie-back Sl19"
```

---

## Task 12: Playwright Visual Verification + Part 1 Regression Check

**Goal:** Snapshot semua 31 slide via Playwright + verify Sl1-21 tidak regress.

**Files:**
- Tidak modify file — read-only verification

- [ ] **Step 1: Buka file di Playwright + iterate semua slide**

Pakai Playwright MCP tool:

```javascript
// Navigate ke file
browser_navigate("file:///C:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/Kickoff-PROTON.html")

// Verify counter awal
browser_snapshot()  // Expect "1 / 31"

// Loop ke slide 22-31 (yang baru) via tekan End atau klik Next
browser_press_key("End")  // Jump to last
browser_snapshot()  // Expect counter "31 / 31"

// Manual check tiap slide baru — tekan ArrowLeft mundur dari Sl31 → Sl22
for (let i = 0; i < 9; i++) {
  browser_press_key("ArrowLeft")
  browser_snapshot()  // Visual verify
}
```

- [ ] **Step 2: Cek console error**

```javascript
browser_console_messages()
// Expected: 0 error, 0 warning critical
```

- [ ] **Step 3: Spot-check Part 1 regression (Sl1, Sl11, Sl21)**

```javascript
// Home key → Sl1
browser_press_key("Home")
browser_snapshot()  // Verify Sl1 visual unchanged

// Jump to Sl11 (middle Part 1)
browser_press_key("ArrowRight").repeat(10)  // pseudo
browser_snapshot()  // Verify Sl11 visual unchanged

// Jump to Sl21
// ... navigate
browser_snapshot()  // Verify Sl21 "Terima Kasih" unchanged
```

Expected: Sl1, Sl11, Sl21 visually identical dengan baseline pre-Task 1. Jika regress (CSS bleed), revert dan isolate slide yang menyebabkan.

- [ ] **Step 4: Verifikasi style consistency Sl22-31**

Check setiap slide baru:
- `.slide-header` ada (kecuali Sl22 cover yang pakai center layout)
- `.section-eyebrow` text "PART 2 — ASSESSMENT"
- `.slide-badge` text "N / 31"
- `.accent` span warna amber-dark
- Font teal-dark untuk title
- Default-deco blob top-right + bottom-left visible

- [ ] **Step 5: Tidak commit (read-only task)**

Jika ada finding, fix di task berikutnya. Tidak commit.

---

## Task 13: Tag Release v4.0 + Update Sl21 Versi Footer

**Goal:** Update versi footer di Sl21 + tag release.

**Files:**
- Modify: `docs/Kickoff-PROTON.html:2986` — update versi footer

- [ ] **Step 1: Update versi text di Sl21**

```html
<!-- Line 2986 OLD: -->
Kickoff PROTON &middot; Versi 1.0 &middot; Mei 2026
<!-- Line 2986 NEW: -->
Kickoff PROTON &middot; Versi 4.0 (Part 1 + Part 2 Assessment) &middot; Mei 2026
```

- [ ] **Step 2: Commit versi update**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "chore(kickoff-proton): bump versi footer Sl21 → v4.0 (Part 1+2)"
```

- [ ] **Step 3: Tag release**

```bash
git tag -a kickoff-proton-v4.0-part2 -m "Kickoff PROTON v4.0 — Part 2 Assessment (10 slide Sl22-31)

Append 10 slide baru ke Kickoff-PROTON.html:
- Sl22 Cover Part 2 (divider)
- Sl23 2 Jenis Assessment + Hirarki PROTON (anchor)
- Sl24 Alur CMP E2E (7-step flowchart)
- Sl25 Alur PROTON (T1-2 MC vs T3 Interview 2-col parallel)
- Sl26 ER diagram Session→Package→Question→Option
- Sl27 Grading Engine (Auto vs Manual split panel)
- Sl28 Status Lifecycle 6-state + Timer
- Sl29 Cara Ujian Coachee POV (8-step grid)
- Sl30 Cara Grading HC POV + NomorSertifikat format
- Sl31 Outcome Sertifikat vs CompetencyLevel + tie-back Sl19

Grounding ke codebase actual:
- Models/AssessmentSession.cs (Status enum, ProtonTrackId, TahunKe, NomorSertifikat)
- Models/AssessmentConstants.cs (status string Indonesian 'Menunggu Penilaian')
- Models/ProtonModels.cs (hirarki Track→Kompetensi→SubKomp→Deliverable)
- Helpers/CertNumberHelper.cs (format KPB/{seq:D3}/{Roman}/{Year})
- Services/GradingService.cs (auto + manual + cert retry idempotent)
- 3-role approval (HC + SrSpv + SectionHead)
- CompetencyLevel 0-5 (ProtonFinalAssessment)

Spec: docs/superpowers/specs/2026-05-26-kickoff-proton-part2-assessment-design.md
Plan: docs/superpowers/plans/2026-05-26-kickoff-proton-part2-assessment.md"
```

- [ ] **Step 4: Verify tag**

```bash
git tag -l "kickoff-proton-v4.0*"
git log --oneline -15
```

Expected: tag `kickoff-proton-v4.0-part2` muncul di list. Recent commits show Task 1-13 commit history.

- [ ] **Step 5: NOT push (user explicit consent needed for push)**

Tidak `git push` otomatis. User akan trigger push setelah review (sama pattern dgn v3.0 yang pending push).

---

## Self-Review Checklist (Pre-Handoff)

| Item | Status |
|------|--------|
| Spec §4 outline 10 slide → semua task ada (Task 2-11) | ✓ |
| Spec §6.1 in-scope semua tercakup | ✓ |
| Spec §6.2 out-of-scope tidak dilanggar (1 file, no PDF, no real screenshot) | ✓ |
| Spec §7 acceptance criteria #1 (31 slide total) → Task 1 + 2-11 | ✓ |
| #2 (Sl22 visual distinct) → Task 2 cover layout custom | ✓ |
| #3 (CSS inherit) → semua task pakai class existing | ✓ |
| #4 (no regression Sl1-21) → Task 12 step 3 | ✓ |
| #5 (status enum Indonesian "Menunggu Penilaian") → Task 4 step 1 + Task 8 step 1 | ✓ |
| #6 (NomorSertifikat format KPB/seq:D3/Roman/Year) → Task 10 step 1 | ✓ |
| #7 (Sl23 hirarki 4 level) → Task 3 step 1 tree diagram | ✓ |
| #8 (Sl25 2 sub-flow side-by-side) → Task 5 step 1 grid 2-col | ✓ |
| #9 (3-role approval HC+SrSpv+SectionHead) → Task 5 step 2 explicit | ✓ |
| #10 (Playwright + console error 0) → Task 12 step 2 | ✓ |
| No placeholder TBD/TODO | ✓ |
| Type consistency: `KPB/{seq:D3}/{Roman}/{Year}` konsisten di Task 10 + Task 11 | ✓ |
| Method names: `StartExam`, `SubmitExam`, `GradingService`, `CertNumberHelper.Build`, `SubmitInterviewResults` konsisten | ✓ |
| Status enum value: "Open", "Upcoming", "InProgress", "Completed", "Menunggu Penilaian", "Cancelled" konsisten | ✓ |
