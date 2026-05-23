# Kickoff-PROTON 4 Corporate Slide Integration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Insert 4 corporate-source slides (Img 6/7/8/9) into `docs/Kickoff-PROTON.html`, delete 2 redundant slides, renumber `data-slide` 1..21, ensure deck navigates 21 slides correctly.

**Architecture:** Single HTML file edit. Style hybrid (existing CSS variables + corporate content fidelity). 1 PNG asset copy. JS counter `TOTAL` constant update. Manual UAT in browser as validation (no automated tests — presentation deck).

**Tech Stack:** Static HTML/CSS/inline JS. No build step. No tests framework. Optional Playwright snapshot.

**Spec reference:** `docs/superpowers/specs/2026-05-23-kickoff-proton-4-corporate-slide-design.md`

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `docs/Kickoff-PROTON.html` | Modify | Single deck file — insert 4 slide blocks, delete 2 blocks, renumber, update JS+counter+badges |
| `docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png` | Create | Image asset for new Slide 5 (Img 7 flowchart) |

---

## Task 1: Copy flowchart image asset

**Files:**
- Create: `docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png`

- [ ] **Step 1: Verify source PNG exists**

Run:
```bash
ls -la "C:/Users/Administrator/.claude/image-cache/619beac5-8327-4072-98cd-6b32c0409630/7.png"
```
Expected: file size > 100 KB, accessible.

- [ ] **Step 2: Verify target directory exists**

Run:
```bash
ls -la "docs/sosialisasi-screenshots/proton/"
```
Expected: directory exists, contains `12-kkj.png`, `13-silabus.png`, `14-coaching-guidance-*.png`.

- [ ] **Step 3: Copy file**

Run:
```bash
cp "C:/Users/Administrator/.claude/image-cache/619beac5-8327-4072-98cd-6b32c0409630/7.png" "docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png"
```

- [ ] **Step 4: Verify copy**

Run:
```bash
ls -la "docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png"
```
Expected: file exists, same size as source.

- [ ] **Step 5: Commit**

```bash
git add docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png
git commit -m "chore(kickoff-proton): add flowchart CMP & CDP asset for Img 7 slide

Source: corporate PPT (Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9).
Used by new Slide 5 (Integrated Digital Competency Platform)."
```

---

## Task 2: Insert NEW Slide — Strengthening Workforce (Img 6)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — insert new `<div class="slide">` block AFTER existing Sl 3 (line ~2161, `data-slide="3"` end tag) and BEFORE Sl 4 (3 Pilar)

**Temporary numbering:** use `data-slide="20"` for now (will renumber in Task 7).

- [ ] **Step 1: Locate insertion point**

Run:
```bash
grep -n 'data-slide="3"\|data-slide="4"' docs/Kickoff-PROTON.html
```
Expected: line numbers for `data-slide="3"` opening tag and `data-slide="4"` opening tag. Insert new block between end of slide 3 (`</div>` closing the `data-slide="3"` block) and `<!-- ====... SLIDE 4 ... -->` comment.

- [ ] **Step 2: Insert new slide block**

Insert after line 2161 (`</div>` ending Sl 3), before `<!-- ================= SLIDE 4: 3 PILAR ====`:

```html

<!-- ================= NEW SLIDE: STRENGTHENING WORKFORCE (Img 6) ================= -->
<div class="slide default-deco" data-slide="20">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">PART 1 &middot; APA ITU WEB HC</p>
      <h1 class="slide-title">Strengthening Workforce Competency<span class="accent"> &mdash; Compliance ke Excellence</span></h1>
      <p class="slide-subtitle">Mandat corporate yang melandasi Portal HC KPB</p>
    </div>
    <div class="slide-badge">3 / 21</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-top: 20px;">
      <div style="background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; padding: 22px; border-radius: 12px;">
        <span style="display: inline-block; background: rgba(255,255,255,0.18); color: #fff; font-size: 9pt; font-weight: 800; letter-spacing: 1.5px; padding: 4px 10px; border-radius: 4px;">BACKGROUND</span>
        <ul style="font-size: 10.5pt; margin-top: 14px; line-height: 1.6; padding-left: 18px; opacity: 0.96;">
          <li><strong>Pedoman HCM Dir SDM</strong> No. A5.2-01/K20000/2025-S9 (26 Feb 2025) &mdash; Pengelolaan Kompetensi</li>
          <li><strong>TKO Talent Mgmt</strong> No. B5.3-04/K20100/2025-S9 (20 Mar 2025) &mdash; Pengelolaan Kompetensi Teknis</li>
        </ul>
      </div>
      <div style="background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; padding: 22px; border-radius: 12px;">
        <span style="display: inline-block; background: rgba(255,255,255,0.18); color: #fff; font-size: 9pt; font-weight: 800; letter-spacing: 1.5px; padding: 4px 10px; border-radius: 4px;">CHALLENGE</span>
        <ul style="font-size: 10.5pt; margin-top: 14px; line-height: 1.6; padding-left: 18px; opacity: 0.96;">
          <li>TKO HCD-CPDP update terakhir <strong>2018</strong>, tidak penuhi ketentuan korporat baru</li>
          <li>KPB belum punya <strong>CMP</strong> &amp; <strong>CDP</strong></li>
          <li>Risiko <strong>ketidakpatuhan</strong> peraturan korporat</li>
        </ul>
      </div>
      <div style="background: linear-gradient(135deg, var(--green) 0%, var(--green-dark) 100%); color: #fff; padding: 22px; border-radius: 12px;">
        <span style="display: inline-block; background: rgba(255,255,255,0.18); color: #fff; font-size: 9pt; font-weight: 800; letter-spacing: 1.5px; padding: 4px 10px; border-radius: 4px;">SOLUTION</span>
        <ul style="font-size: 10.5pt; margin-top: 14px; line-height: 1.6; padding-left: 18px; opacity: 0.96;">
          <li>Bangun <strong>Competency Management Platform</strong> (CMP)</li>
          <li>Bangun <strong>Competency Development Platform</strong> (CDP) &mdash; Operator + Panelman, blended learning</li>
          <li>1 aplikasi terintegrasi: Operasional + Human Capital</li>
        </ul>
      </div>
    </div>
    <p style="font-size: 9.5pt; color: var(--text-muted); margin-top: 28px; text-align: center; font-style: italic;">
      Ref: Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9 rev 0 &middot; TKO Talent Mgmt No. B5.3-04/K20100/2025-S9 rev 0
    </p>
  </div>
</div>

```

- [ ] **Step 3: Verify insertion**

Run:
```bash
grep -c 'data-slide="20"' docs/Kickoff-PROTON.html
```
Expected: `1`

- [ ] **Step 4: Open file in browser, navigate to slide via dev console**

Open `docs/Kickoff-PROTON.html`. In browser console:
```javascript
document.querySelector('[data-slide="20"]').classList.add('active');
document.querySelectorAll('.slide').forEach(s => { if(s.dataset.slide !== "20") s.classList.remove('active'); });
```
Expected: new slide renders. 3 colored cards visible. Layout fits 1280x720.

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): insert Strengthening Workforce slide (Img 6)

New slide for Part 1: 3-card grid Background/Challenge/Solution citing
TKO Pedoman HCM No. A5.2 + TKO Talent Mgmt B5.3-04.
Uses temp data-slide=20, will renumber to 3 in renumbering task."
```

---

## Task 3: Insert NEW Slide — Flowchart CMP & CDP (Img 7)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — insert AFTER existing Sl 3 (Kenapa Portal HC, original) and BEFORE Sl 4 (3 Pilar) — in same insertion zone as Task 2 but AFTER Task 2's block.

**Temporary numbering:** `data-slide="21"`.

- [ ] **Step 1: Locate insertion point**

Run:
```bash
grep -n 'data-slide="20"\|data-slide="4"' docs/Kickoff-PROTON.html
```
Insert AFTER closing `</div>` of `data-slide="20"` block (end of Img 6 slide from Task 2).

- [ ] **Step 2: Insert new slide block**

```html

<!-- ================= NEW SLIDE: FLOWCHART CMP & CDP (Img 7) ================= -->
<div class="slide default-deco" data-slide="21">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">PART 1 &middot; APA ITU WEB HC</p>
      <h1 class="slide-title">Integrated Digital Competency Platform<span class="accent"> &mdash; Flowchart CMP &amp; CDP</span></h1>
      <p class="slide-subtitle">Peta proses end-to-end dari penyusunan SME sampai blended learning</p>
    </div>
    <div class="slide-badge">5 / 21</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1.7fr 1fr; gap: 22px; align-items: stretch; margin-top: 8px;">
      <div class="demo-image-frame" style="aspect-ratio:auto; height: 44vh; padding: 6px;">
        <img src="sosialisasi-screenshots/proton/flowchart-cmp-cdp.png" alt="Flowchart Integrated CMP &amp; CDP" style="width:100%; height:100%; object-fit: contain;">
      </div>
      <div style="display: flex; flex-direction: column; gap: 12px;">
        <div style="border: 2px dashed #2563eb; padding: 12px 14px; border-radius: 8px; background: rgba(37,99,235,0.05);">
          <div style="font-size: 10pt; font-weight: 800; color: #1e40af; letter-spacing: 1.2px;">CMP SCOPE <span style="font-weight: 400; opacity: 0.7;">(blue line)</span></div>
          <p style="font-size: 9.5pt; color: var(--text); margin-top: 6px; line-height: 1.45;">Penyusunan SME &middot; KKJ &middot; Asesmen Kompetensi Teknis &middot; 4 branch hasil lulus (Asesor LSP / SME / Coach / New Exposure) &middot; Penyepakatan Silabus</p>
        </div>
        <div style="border: 2px dashed #dc2626; padding: 12px 14px; border-radius: 8px; background: rgba(220,38,38,0.05);">
          <div style="font-size: 10pt; font-weight: 800; color: #991b1b; letter-spacing: 1.2px;">CDP SCOPE <span style="font-weight: 400; opacity: 0.7;">(red line)</span></div>
          <p style="font-size: 9.5pt; color: var(--text); margin-top: 6px; line-height: 1.45;">Eksekusi Program Pengembangan Kompetensi metode <strong>Blended Learning</strong>.</p>
        </div>
        <div class="demo-takeaway" style="margin-top: 4px;">
          CMP define + asses + bikin Silabus &rarr; CDP eksekusi Blended Learning.
        </div>
      </div>
    </div>
    <p style="font-size: 9.5pt; color: var(--text-muted); margin-top: 18px; text-align: center; font-style: italic;">
      Ref: Pedoman HCM Dir SDM No. A5.2-01/K20000/2025-S9 rev 0
    </p>
  </div>
</div>

```

- [ ] **Step 3: Verify insertion + asset loads**

Open `docs/Kickoff-PROTON.html`, console:
```javascript
document.querySelectorAll('.slide').forEach(s => s.classList.remove('active'));
document.querySelector('[data-slide="21"]').classList.add('active');
```
Expected: flowchart PNG renders sharp, 2 legend boxes (blue dashed + red dashed) visible, takeaway block displays.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): insert Flowchart CMP & CDP slide (Img 7)

New slide for Part 1 transition: image-left + legend-right + takeaway.
Asset: sosialisasi-screenshots/proton/flowchart-cmp-cdp.png.
Temp data-slide=21, will renumber to 5."
```

---

## Task 4: Insert NEW Slide — PROTON Methodology 70-20-10 (Img 8)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — insert AFTER existing Sl 8 (PROTON intro SMART, ends at line ~2400) and BEFORE Sl 14 (4 Peran Chain Coaching, line ~2403, anomaly file position).

**Temporary numbering:** `data-slide="22"`.

- [ ] **Step 1: Locate insertion point**

Run:
```bash
grep -n 'data-slide="8"\|data-slide="14"' docs/Kickoff-PROTON.html
```
Insert AFTER `</div>` ending Sl 8 (`data-slide="8"`), BEFORE `<!-- ============... SLIDE 14 (was 9): 4 PERAN ...`.

- [ ] **Step 2: Insert new slide block**

```html

<!-- ================= NEW SLIDE: PROTON METHODOLOGY 70-20-10 (Img 8) ================= -->
<div class="slide default-deco" data-slide="22">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">PART 3 &middot; APA ITU PROTON</p>
      <h1 class="slide-title">PROTON Methodology<span class="accent"> &mdash; Blended Learning 70-20-10</span></h1>
      <p class="slide-subtitle">Mayoritas belajar dari kerja nyata, bukan dari kelas</p>
    </div>
    <div class="slide-badge">11 / 21</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 32px; align-items: center; margin-top: 12px; padding: 0 20px;">
      <div style="display: flex; flex-direction: column; gap: 16px;">
        <div style="display: grid; grid-template-columns: 130px 1fr; align-items: center; gap: 16px;">
          <div style="font-size: 56pt; font-weight: 900; color: var(--teal); line-height: 1;">70<span style="font-size: 30pt;">%</span></div>
          <div>
            <div style="font-size: 18pt; font-weight: 800; color: var(--teal-dark);">Assignment</div>
            <div style="font-size: 11pt; color: var(--text-muted); margin-top: 4px; line-height: 1.4;">Kerja + upload bukti deliverable</div>
          </div>
        </div>
        <div style="display: grid; grid-template-columns: 130px 1fr; align-items: center; gap: 16px;">
          <div style="font-size: 56pt; font-weight: 900; color: var(--amber); line-height: 1;">20<span style="font-size: 30pt;">%</span></div>
          <div>
            <div style="font-size: 18pt; font-weight: 800; color: var(--amber-dark);">Coaching</div>
            <div style="font-size: 11pt; color: var(--text-muted); margin-top: 4px; line-height: 1.4;">Sesi terjadwal dengan coach</div>
          </div>
        </div>
        <div style="display: grid; grid-template-columns: 130px 1fr; align-items: center; gap: 16px;">
          <div style="font-size: 56pt; font-weight: 900; color: var(--green); line-height: 1;">10<span style="font-size: 30pt;">%</span></div>
          <div>
            <div style="font-size: 18pt; font-weight: 800; color: var(--green-dark);">Self-Study</div>
            <div style="font-size: 11pt; color: var(--text-muted); margin-top: 4px; line-height: 1.4;">Baca materi mandiri (Coaching Guidance)</div>
          </div>
        </div>
      </div>
      <div style="position: relative; width: 100%; aspect-ratio: 1; max-width: 380px; margin: 0 auto;">
        <div style="position: absolute; top: 8%; left: 50%; transform: translateX(-50%); width: 110px; height: 110px; border-radius: 50%; background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; display: flex; flex-direction: column; align-items: center; justify-content: center; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
          <div style="font-size: 22pt;">&#128221;</div>
          <div style="font-size: 10pt; font-weight: 800; margin-top: 2px;">Assignment</div>
        </div>
        <div style="position: absolute; bottom: 8%; left: 8%; width: 110px; height: 110px; border-radius: 50%; background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; display: flex; flex-direction: column; align-items: center; justify-content: center; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
          <div style="font-size: 22pt;">&#128172;</div>
          <div style="font-size: 10pt; font-weight: 800; margin-top: 2px;">Coaching</div>
        </div>
        <div style="position: absolute; bottom: 8%; right: 8%; width: 110px; height: 110px; border-radius: 50%; background: linear-gradient(135deg, var(--green) 0%, var(--green-dark) 100%); color: #fff; display: flex; flex-direction: column; align-items: center; justify-content: center; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
          <div style="font-size: 22pt;">&#128161;</div>
          <div style="font-size: 10pt; font-weight: 800; margin-top: 2px;">Self-Study</div>
        </div>
        <svg viewBox="0 0 380 380" style="position: absolute; inset: 0; width: 100%; height: 100%; z-index: -1;" aria-hidden="true">
          <line x1="190" y1="120" x2="80" y2="280" stroke="#fbbf24" stroke-width="3" stroke-dasharray="6,6" />
          <line x1="190" y1="120" x2="300" y2="280" stroke="#10b981" stroke-width="3" stroke-dasharray="6,6" />
          <line x1="80" y1="280" x2="300" y2="280" stroke="#94a3b8" stroke-width="3" stroke-dasharray="6,6" />
        </svg>
      </div>
    </div>
    <p style="font-size: 9.5pt; color: var(--text-muted); margin-top: 28px; text-align: center; font-style: italic;">
      Sumber: Pedoman HCM Pertamina 2025
    </p>
  </div>
</div>

```

- [ ] **Step 3: Verify insertion**

Open file in browser, console:
```javascript
document.querySelectorAll('.slide').forEach(s => s.classList.remove('active'));
document.querySelector('[data-slide="22"]').classList.add('active');
```
Expected: 3 percentage rows on left (70/20/10 with labels), 3 circle nodes connected with dashed lines on right. No overflow, no clipped text.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): insert PROTON Methodology 70-20-10 slide (Img 8)

New slide for Part 3: 2-col split. Left = 3 percentage rows
(70% Assignment, 20% Coaching, 10% Self-Study). Right = 3 circle
diagram with SVG dashed connectors. Temp data-slide=22, target slot 11."
```

---

## Task 5: Insert NEW Slide — Kompetensi PROTON per Tahun (Img 9)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — insert AFTER Sl 22 block from Task 4 (still in zone between Sl 8 and Sl 14 anomaly).

**Temporary numbering:** `data-slide="23"`.

- [ ] **Step 1: Locate insertion point**

Run:
```bash
grep -n 'data-slide="22"\|data-slide="14"' docs/Kickoff-PROTON.html
```
Insert AFTER `</div>` closing `data-slide="22"`, BEFORE `<!-- ====... SLIDE 14 (was 9): 4 PERAN ...`.

- [ ] **Step 2: Insert new slide block**

```html

<!-- ================= NEW SLIDE: KOMPETENSI PROTON PER TAHUN (Img 9) ================= -->
<div class="slide default-deco" data-slide="23">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">PART 3 &middot; APA ITU PROTON</p>
      <h1 class="slide-title">Kompetensi PROTON<span class="accent"> &mdash; Per Tahun</span></h1>
      <p class="slide-subtitle">Mengacu Kamus Kompetensi Jabatan (KKJ) Tahun 2023</p>
    </div>
    <div class="slide-badge">12 / 21</div>
  </div>
  <div class="slide-body">
    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 18px; margin-top: 24px;">
      <div style="background: linear-gradient(135deg, var(--teal) 0%, var(--teal-dark) 100%); color: #fff; padding: 22px; border-radius: 12px; min-height: 280px;">
        <div style="text-align: center; padding-bottom: 12px; border-bottom: 2px solid rgba(255,255,255,0.3); margin-bottom: 14px;">
          <div style="font-size: 9pt; letter-spacing: 3px; opacity: 0.85; font-weight: 700;">TAHUN</div>
          <div style="font-size: 38pt; font-weight: 900; line-height: 1;">1</div>
        </div>
        <ul style="list-style: none; padding: 0; margin: 0; font-size: 10.5pt; line-height: 1.5;">
          <li style="margin-bottom: 12px;"><strong>Kompetensi 1</strong><br/><span style="opacity: 0.92;">Safe Work Practice &amp; Lifesaving Rules</span></li>
          <li><strong>Kompetensi 5.1</strong><br/><span style="opacity: 0.92;">Refinery Process Operations &amp; Optimization</span></li>
        </ul>
      </div>
      <div style="background: linear-gradient(135deg, var(--amber) 0%, var(--amber-dark) 100%); color: #fff; padding: 22px; border-radius: 12px; min-height: 280px;">
        <div style="text-align: center; padding-bottom: 12px; border-bottom: 2px solid rgba(255,255,255,0.3); margin-bottom: 14px;">
          <div style="font-size: 9pt; letter-spacing: 3px; opacity: 0.85; font-weight: 700;">TAHUN</div>
          <div style="font-size: 38pt; font-weight: 900; line-height: 1;">2</div>
        </div>
        <ul style="list-style: none; padding: 0; margin: 0; font-size: 10.5pt; line-height: 1.5;">
          <li style="margin-bottom: 10px;"><strong>Kompetensi 2</strong><br/><span style="opacity: 0.92;">Energy Management</span></li>
          <li style="margin-bottom: 10px;"><strong>Kompetensi 3</strong><br/><span style="opacity: 0.92;">Catalyst &amp; Chemical Management</span></li>
          <li><strong>Kompetensi 5.2</strong><br/><span style="opacity: 0.92;">Refinery Process Operations &amp; Optimization</span></li>
        </ul>
      </div>
      <div style="background: linear-gradient(135deg, var(--green) 0%, var(--green-dark) 100%); color: #fff; padding: 22px; border-radius: 12px; min-height: 280px;">
        <div style="text-align: center; padding-bottom: 12px; border-bottom: 2px solid rgba(255,255,255,0.3); margin-bottom: 14px;">
          <div style="font-size: 9pt; letter-spacing: 3px; opacity: 0.85; font-weight: 700;">TAHUN</div>
          <div style="font-size: 38pt; font-weight: 900; line-height: 1;">3</div>
        </div>
        <ul style="list-style: none; padding: 0; margin: 0; font-size: 10.5pt; line-height: 1.5;">
          <li style="margin-bottom: 12px;"><strong>Kompetensi 4</strong><br/><span style="opacity: 0.92;">Process Control &amp; Computer Operations</span></li>
          <li><strong>Kompetensi 5.3</strong><br/><span style="opacity: 0.92;">Refinery Process Operations &amp; Optimization</span></li>
        </ul>
      </div>
    </div>
    <p style="font-size: 9.5pt; color: var(--text-muted); margin-top: 22px; text-align: center; font-style: italic;">
      Sumber: Kamus Kompetensi Jabatan (KKJ) Pertamina Tahun 2023
    </p>
  </div>
</div>

```

- [ ] **Step 3: Verify insertion**

Open file, console:
```javascript
document.querySelectorAll('.slide').forEach(s => s.classList.remove('active'));
document.querySelector('[data-slide="23"]').classList.add('active');
```
Expected: 3 colored columns (teal/amber/green), each with TAHUN label + big number + kompetensi list. No overflow.

- [ ] **Step 4: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "feat(kickoff-proton): insert Kompetensi per Tahun slide (Img 9)

New slide for Part 3: 3-col grid TAHUN 1/2/3 with KKJ 2023
kompetensi list. Temp data-slide=23, target slot 12."
```

---

## Task 6: Delete redundant slides (Sl 9 + Sl 10 lama)

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — remove blocks `data-slide="9"` (2 Track × 3 Tahun, line ~2450..) and `data-slide="10"` (Komponen 4 Pilar, line ~2491..)

- [ ] **Step 1: Locate blocks to delete**

Run:
```bash
grep -n 'data-slide="9"\|data-slide="10"\|data-slide="11"' docs/Kickoff-PROTON.html
```
Expected: line numbers for opening tags. Identify start and end of each block.

- [ ] **Step 2: Delete Sl 9 block (2 Track × 3 Tahun)**

Remove from `<!-- ====... 2 TRACK 3 TAHUN ... -->` (or the previous comment) and `<div class="slide default-deco" data-slide="9">` opening tag through its closing `</div>` (including any trailing comment line before the next slide). Use Edit tool to delete exact range, ensuring next slide (`data-slide="10"` or `data-slide="11"` depending on order) starts cleanly.

- [ ] **Step 3: Delete Sl 10 block (Komponen 4 Pilar)**

Remove from `<!-- ====... KOMPONEN 4 PILAR ... -->` and `<div class="slide default-deco" data-slide="10">` through closing `</div>` and trailing comment line.

- [ ] **Step 4: Verify deletion**

Run:
```bash
grep -c 'data-slide="9"' docs/Kickoff-PROTON.html
grep -c 'data-slide="10"' docs/Kickoff-PROTON.html
```
Expected: both return `0`.

Run:
```bash
grep -c 'class="slide ' docs/Kickoff-PROTON.html
```
Expected: `21` (19 original - 2 deleted + 4 inserted = 21).

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "refactor(kickoff-proton): hapus Sl 9 (2 Track 3 Tahun) + Sl 10 (4 Pilar)

Persiapan untuk integrasi 4 slide corporate:
- Sl 9 lama redundant dengan NEW Slide Kompetensi per Tahun (Img 9)
- Sl 10 lama (Komponen 4 Pilar) drop per keputusan user
- Info 2 track Operator/Panelman masih ada di Sl Silabus (KKJ)"
```

---

## Task 7: Renumber data-slide attributes 1..21 + reorder file blocks

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — update `data-slide` attribute of every slide block to sequential 1..21 per spec section 4 mapping. Rearrange file order so file order = display order.

**Renumbering mapping (lama temp → final):**

| Slide content | data-slide lama/temp | data-slide BARU |
|---|---|---|
| Cover | 1 | 1 |
| Apa itu Portal HC | 2 | 2 |
| **NEW Img 6 Strengthening** | 20 | 3 |
| Kenapa Portal HC dibangun | 3 | 4 |
| **NEW Img 7 Flowchart** | 21 | 5 |
| 3 Pilar | 4 | 6 |
| CMP | 5 | 7 |
| CDP | 6 | 8 |
| BP | 7 | 9 |
| PROTON intro | 8 | 10 |
| **NEW Img 8 Methodology** | 22 | 11 |
| **NEW Img 9 Kompetensi/Tahun** | 23 | 12 |
| KKJ | 11 | 13 |
| Silabus | 12 | 14 |
| Coaching Guidance | 13 | 15 |
| 4 Peran Chain Coaching | 14 | 16 |
| Alur PROTON E2E | 15 | 17 |
| Manfaat Per Role | 16 | 18 |
| Outcome | 17 | 19 |
| Akses Portal | 18 | 20 |
| Penutup | 19 | 21 |

- [ ] **Step 1: Backup safety check**

Run:
```bash
git status docs/Kickoff-PROTON.html
```
Expected: clean working tree (previous task committed).

- [ ] **Step 2: Update data-slide attributes (one-pass via sed)**

⚠️ **Order matters** — to avoid collision (e.g. renaming 11→13 then 13→15 makes wrong slide become 15), use temp prefix first.

Step 2a — Add temp prefix `X` to all `data-slide` numeric values:

Run:
```bash
sed -i 's/data-slide="\([0-9]*\)"/data-slide="X\1"/g' docs/Kickoff-PROTON.html
```
Verify:
```bash
grep -c 'data-slide="X' docs/Kickoff-PROTON.html
```
Expected: `21`

Step 2b — Map each X-prefix to final number using sed:

Run sequentially (use exact mapping table):
```bash
sed -i 's/data-slide="X1"/data-slide="1"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X2"/data-slide="2"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X20"/data-slide="3"/g'  docs/Kickoff-PROTON.html
sed -i 's/data-slide="X3"/data-slide="4"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X21"/data-slide="5"/g'  docs/Kickoff-PROTON.html
sed -i 's/data-slide="X4"/data-slide="6"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X5"/data-slide="7"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X6"/data-slide="8"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X7"/data-slide="9"/g'   docs/Kickoff-PROTON.html
sed -i 's/data-slide="X8"/data-slide="10"/g'  docs/Kickoff-PROTON.html
sed -i 's/data-slide="X22"/data-slide="11"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X23"/data-slide="12"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X11"/data-slide="13"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X12"/data-slide="14"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X13"/data-slide="15"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X14"/data-slide="16"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X15"/data-slide="17"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X16"/data-slide="18"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X17"/data-slide="19"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X18"/data-slide="20"/g' docs/Kickoff-PROTON.html
sed -i 's/data-slide="X19"/data-slide="21"/g' docs/Kickoff-PROTON.html
```

- [ ] **Step 3: Verify no X-prefix remains and 21 unique slots**

Run:
```bash
grep -c 'data-slide="X' docs/Kickoff-PROTON.html
```
Expected: `0`

Run:
```bash
grep -oE 'data-slide="[0-9]+"' docs/Kickoff-PROTON.html | sort -t'"' -k2 -n | uniq -c
```
Expected: each `data-slide="N"` appears exactly `1` time for N in 1..21, no gaps, no duplicates.

- [ ] **Step 4: Reorder file blocks (optional but recommended)**

Current file has anomaly: `data-slide="16"` (formerly 14, "4 Peran") sits in file between data-slide="10" and data-slide="13" or similar non-sequential location. To normalize file order = display order:

Use Edit tool to cut the entire `<div class="slide default-deco" data-slide="16">...</div>` block (along with any comment header preceding it) from its current file position and paste it into correct chronological slot (after data-slide="15" and before data-slide="17").

If file reordering proves complex, **defer to Task 7 alternative below** — JS navigation is by attribute value, not file position, so functionality works regardless. File reordering is cosmetic only.

**Task 7 alternative (lightweight):** skip Step 4 (file reorder). Functional deck works fine. Note in commit message: "file order not normalized, JS nav by attribute".

- [ ] **Step 5: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "refactor(kickoff-proton): renumber data-slide attributes 1..21

Final slot mapping per design spec section 4. Used temp X-prefix
sed pass to avoid collision during renumber. 21 slot sequential,
no gaps, no duplicates."
```

---

## Task 8: Update slide-badge text + JS TOTAL + counter

**Files:**
- Modify: `docs/Kickoff-PROTON.html` — update every `.slide-badge` text from `"N / 19"` (or temp `"3 / 21"` etc. for new slides) to match its slot number `"N / 21"`. Update JS `const TOTAL = 19;` → `const TOTAL = 21;`. Update `<span class="slide-counter" id="slideCounter">1 / 19</span>` → `1 / 21`.

- [ ] **Step 1: Update all slide-badge text via sed**

Replace `" / 19"` with `" / 21"` in `.slide-badge` content. The badges in 4 NEW slides may already say correct slot number but wrong total. Old slides may have old slot + `/19`.

Approach: replace badge text individually per slot. First identify mismatches.

Run:
```bash
grep -nE 'class="slide-badge">[0-9]+ / [0-9]+' docs/Kickoff-PROTON.html
```

Then for each slide that has `data-slide="N"`, the corresponding `.slide-badge` (within same slide block) should read `"N / 21"`.

Edit per-slide using Edit tool. Pattern: find `<div class="slide-badge">N / 19</div>` (for renumbered slides) or `<div class="slide-badge">M / 21</div>` (for new slides where M may not equal new slot N after Task 7 renumber) → replace with `<div class="slide-badge">N / 21</div>` matching that block's `data-slide="N"`.

**Simpler approach** — single sed pass:

Run:
```bash
sed -i 's|<div class="slide-badge">[0-9]\+ / 19</div>|<div class="slide-badge">PLACEHOLDER / 21</div>|g' docs/Kickoff-PROTON.html
sed -i 's|<div class="slide-badge">[0-9]\+ / 21</div>|<div class="slide-badge">PLACEHOLDER / 21</div>|g' docs/Kickoff-PROTON.html
```
This zeroes out the slot number. Then manually walk slot-by-slot and replace `PLACEHOLDER` with the correct number using Edit tool. Less error-prone than per-slot sed which can collide.

**Alternative — script in Python:**

```python
import re
with open('docs/Kickoff-PROTON.html', 'r', encoding='utf-8') as f:
    content = f.read()

def fix_badges(text):
    # Match each slide block and update its badge
    pattern = re.compile(
        r'(<div class="slide[^"]*" data-slide="(\d+)"[^>]*>.*?'
        r'<div class="slide-badge">)\d+ / \d+(</div>)',
        re.DOTALL
    )
    def repl(m):
        prefix, num, suffix = m.group(1), m.group(2), m.group(3)
        return f'{prefix}{num} / 21{suffix}'
    return pattern.sub(repl, text)

content = fix_badges(content)
with open('docs/Kickoff-PROTON.html', 'w', encoding='utf-8') as f:
    f.write(content)
```

Run:
```bash
python -c "$(cat <<'PYEOF'
import re
with open('docs/Kickoff-PROTON.html', 'r', encoding='utf-8') as f:
    content = f.read()
pattern = re.compile(
    r'(<div class=\"slide[^\"]*\" data-slide=\"(\d+)\"[^>]*>.*?<div class=\"slide-badge\">)\d+ / \d+(</div>)',
    re.DOTALL
)
def repl(m):
    return f'{m.group(1)}{m.group(2)} / 21{m.group(3)}'
content = pattern.sub(repl, content)
with open('docs/Kickoff-PROTON.html', 'w', encoding='utf-8') as f:
    f.write(content)
print('OK')
PYEOF
)"
```

- [ ] **Step 2: Verify slide-badge updates**

Run:
```bash
grep -oE 'class="slide-badge">[0-9]+ / [0-9]+' docs/Kickoff-PROTON.html | sort -u
```
Expected: shows `class="slide-badge">1 / 21`, `... 2 / 21`, ..., `21 / 21` — each unique, all denominator `21`.

Run:
```bash
grep -c 'class="slide-badge">[0-9]\+ / 19' docs/Kickoff-PROTON.html
```
Expected: `0`

Note: cover slide (data-slide="1") may not have `.slide-badge` (different layout). Verify:
```bash
grep -B1 'class="slide-badge"' docs/Kickoff-PROTON.html | head -5
```

- [ ] **Step 3: Update JS TOTAL constant**

Use Edit tool:

```
Find:
    const TOTAL = 19;
Replace:
    const TOTAL = 21;
```

- [ ] **Step 4: Update slideCounter initial text**

Use Edit tool:

```
Find:
  <span class="slide-counter" id="slideCounter">1 / 19</span>
Replace:
  <span class="slide-counter" id="slideCounter">1 / 21</span>
```

- [ ] **Step 5: Verify JS + counter updates**

Run:
```bash
grep -nE 'TOTAL = [0-9]+|id="slideCounter"' docs/Kickoff-PROTON.html
```
Expected: `const TOTAL = 21;` and `<span ... id="slideCounter">1 / 21</span>`.

- [ ] **Step 6: Commit**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): update slide-badge + JS TOTAL + counter ke /21

- Semua .slide-badge text 'N / 19' → 'N / 21' match slot
- const TOTAL = 19 → 21
- #slideCounter initial '1 / 19' → '1 / 21'
- Navigasi prev/next + keyboard now span 21 slides"
```

---

## Task 9: Local render UAT — manual navigation check

**Files:** None modified. Browser-based testing.

- [ ] **Step 1: Open deck in browser**

Run:
```bash
start "" "docs\Kickoff-PROTON.html"
```
(Windows) — opens default browser. Or open manually.

- [ ] **Step 2: Navigate Slide 1 → 21 via arrow keys**

Walk through every slide. Check for each:
- Slide renders without overflow (1280x720 design)
- `.slide-counter` shows correct `N / 21`
- `.slide-badge` shows correct `N / 21`
- No JS console errors

Expected slot content:
| Slot | Title contains |
|---|---|
| 1 | Cover (Portal HC KPB Kickoff 2026) |
| 2 | "Apa itu Portal HC KPB" |
| 3 | "Strengthening Workforce Competency" |
| 4 | "Kenapa Portal HC dibangun" |
| 5 | "Integrated Digital Competency Platform" + flowchart image |
| 6 | "3 Pilar Portal HC" |
| 7 | "CMP — Competency Management Platform" |
| 8 | "CDP — Competency Development Platform" |
| 9 | "BP — Business Partner" |
| 10 | "PROTON" intro |
| 11 | "PROTON Methodology" 70-20-10 |
| 12 | "Kompetensi PROTON — Per Tahun" |
| 13 | "KKJ" |
| 14 | "Silabus PROTON" |
| 15 | "Coaching Guidance" |
| 16 | "4 Peran dalam Chain Coaching" |
| 17 | "Alur PROTON End-to-End" |
| 18 | "Manfaat PROTON Per Role" |
| 19 | "Outcome — Sertifikasi Mahir & IDP" |
| 20 | "Akses Portal" |
| 21 | "Terima Kasih" |

- [ ] **Step 3: Slide 5 image check**

On Slide 5, verify flowchart PNG:
- Loads (no broken-image icon)
- Sharp (not blurry)
- Fits within image frame
- Legend cards (blue/red dashed border) render correctly

- [ ] **Step 4: Slide 11 SVG check**

On Slide 11, verify 3 circle diagram:
- All 3 circles render with gradient + emoji icon + label
- SVG dashed lines connect circles in triangle
- Layout doesn't clip at any zoom level

- [ ] **Step 5: Dark mode check**

Click `☀` button (top-right). Verify:
- 4 NEW slides remain readable
- Card gradients still visible (CSS variables auto-adapt)
- Text contrast acceptable

Click back to light mode.

- [ ] **Step 6: Fullscreen + keyboard nav**

Click `⛶` (fullscreen). Press Home → Slide 1. Press End → Slide 21. Press ArrowRight 20 times → reach Slide 21. Press ArrowLeft 20 times → back to Slide 1. Confirm no skip or lockup.

- [ ] **Step 7: Document any issues found**

If any layout/render issue found, fix inline with Edit tool. Common fixes:
- Overflow: reduce `font-size` or `padding` in offending block
- Image blur: check `object-fit: contain` set on `<img>`
- SVG misalignment: adjust `viewBox` or line coordinates

After fix, return to Step 2 and re-verify.

- [ ] **Step 8: Commit any UAT fixes (if needed)**

```bash
git add docs/Kickoff-PROTON.html
git commit -m "fix(kickoff-proton): UAT polish — <describe specific fix>"
```

If no fixes needed, skip commit.

---

## Task 10: Final verification + tag readiness

**Files:** None modified. Verification only.

- [ ] **Step 1: Confirm total slide count**

Run:
```bash
grep -cE 'class="slide[^"]*" data-slide=' docs/Kickoff-PROTON.html
```
Expected: `21`

- [ ] **Step 2: Confirm sequential data-slide 1..21**

Run:
```bash
grep -oE 'data-slide="[0-9]+"' docs/Kickoff-PROTON.html | sed 's/data-slide="\([0-9]*\)"/\1/' | sort -n | uniq -c
```
Expected: each of 1..21 listed once with count `1`.

- [ ] **Step 3: Confirm asset exists**

Run:
```bash
ls -la docs/sosialisasi-screenshots/proton/flowchart-cmp-cdp.png
```
Expected: file exists, non-empty.

- [ ] **Step 4: Git log review**

Run:
```bash
git log --oneline -10
```
Expected: see commits from Task 1..9, all atomic, conventional commit messages.

- [ ] **Step 5: Compute commit hash for handoff**

Run:
```bash
git rev-parse HEAD
```
Note the hash. This is what IT Team will deploy to Dev (per CLAUDE.md workflow).

- [ ] **Step 6: Final commit message summary (optional tag)**

Optionally create annotated tag:
```bash
git tag -a kickoff-proton-v2.0 -m "Kickoff-PROTON v2.0 — 4 corporate slide integrated

Slot 3: Strengthening Workforce (Img 6 corporate)
Slot 5: Flowchart Integrated CMP & CDP (Img 7 corporate)
Slot 11: PROTON Methodology 70-20-10 (Img 8 corporate)
Slot 12: Kompetensi PROTON per Tahun (Img 9 corporate)
Removed: 2 Track 3 Tahun + Komponen 4 Pilar (redundant)
Total: 19 -> 21 slides"
```

(Tag is optional, push only if user confirms.)

- [ ] **Step 7: Report to user**

Summary message:
- "Implementation complete. 19 → 21 slides. Last commit: `<hash>`. Manual UAT passed. Awaiting user verification on browser before notifying IT Team (per CLAUDE.md dev workflow)."

---

## Self-Review Notes

**Spec coverage check:**
- ✅ Img 6 insert (Task 2)
- ✅ Img 7 insert + asset copy (Task 1, 3)
- ✅ Img 8 insert (Task 4)
- ✅ Img 9 insert (Task 5)
- ✅ Sl 9 + Sl 10 delete (Task 6)
- ✅ Renumber 1..21 (Task 7)
- ✅ JS TOTAL + counter + badges (Task 8)
- ✅ UAT plan (Task 9)
- ✅ Final verification (Task 10)
- ✅ Content fidelity miss-fix (Miss 1-5 from brainstorm) embedded in HTML blocks

**Placeholder scan:**
- ✅ No TBD / TODO / "implement later"
- ✅ All code blocks have exact content
- ✅ Sed commands have explicit before/after values
- ✅ HTML insertions are complete, ready to paste

**Type consistency:**
- ✅ CSS class names (`.slide`, `.slide-header`, `.slide-title`, `.slide-badge`, `.slide-body`, `.section-eyebrow`, `.demo-takeaway`, `.demo-image-frame`) match existing
- ✅ CSS variables (`--teal`, `--teal-dark`, `--amber`, `--amber-dark`, `--green`, `--green-dark`, `--text`, `--text-muted`) match existing
- ✅ Data attribute pattern `data-slide="N"` consistent

**Known gap / accepted trade-off:**
- Task 7 Step 4 (file block reorder) marked optional — JS nav by attribute, file order is cosmetic only. Acceptable to defer.
- No automated test (presentation deck) — manual UAT only. Acceptable.
- Cover slide (data-slide="1") may not have `.slide-badge` — Task 8 Step 2 verifies; if absent, skip badge update for slot 1.
