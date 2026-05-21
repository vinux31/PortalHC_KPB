# Sosialisasi HC Operasional — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Produce `docs/Sosialisasi-HC-Operasional.html` — 23 slide deck 16:9 untuk presentasi training tim HC. Fresh teal+amber palette, sticky controls, dark mode, print-PDF support. Complement Panduan Operasional HC v1.0 sebagai reference.

**Architecture:** Standalone HTML, inline CSS+JS, no build step. Slide-based (1 slide = 1 div absolute-centered). Keyboard nav + button nav + slide counter + dark mode toggle.

**Spec:** `docs/superpowers/specs/2026-05-21-sosialisasi-hc-operasional-design.md`

---

## File Structure

**Created:** `docs/Sosialisasi-HC-Operasional.html` (~2000-2500 lines)
**Modified:** none

## Conventions

**Slide skeleton**:
```html
<div class="slide default-deco" data-slide="N">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">SECTION NAME</p>
      <h1 class="slide-title">Judul <span class="accent">Slide</span></h1>
      <p class="slide-subtitle">Subjudul singkat</p>
    </div>
    <div class="slide-badge">SLIDE N / 23</div>
  </div>
  <div class="slide-body">
    <!-- content max 3-4 element -->
  </div>
</div>
```

**Color palette** (fresh, teal + amber):
- `--teal: #0d9488` (primary)
- `--teal-dark: #0f766e`
- `--amber: #f59e0b`
- `--amber-dark: #d97706`
- `--green: #10b981`
- `--orange: #ea580c`
- `--red: #dc2626`
- `--slate: #64748b`
- Light bg: `#f0fdfa`
- Dark bg: `#0f2027`

**Commit format**: `docs(sosialisasi-hc): <slide-group> — <summary>`

---

## Task 1: Scaffold — Style + Cover + Closing + Controls + Script

**Files:** Create `docs/Sosialisasi-HC-Operasional.html`

- [ ] Step 1: Write file dengan struktur:
  - `<head>` + `<style>` block dengan palette teal/amber + slide CSS (.slide, .slide.active, .slide-header, .slide-title, .slide-badge, .slide-body, .section-eyebrow, .controls, .ctrl-btn, .slide-counter, .utility-bar, .util-btn, .progress-bar, .slide.cover, .cover-content, .slide.penutup, .penutup-title, body.dark variants)
  - Animation: `@keyframes slideIn` cross-fade
  - 16:9 aspect-ratio, max-width 1280px, centered
  - Sticky bottom controls (prev/next/counter)
  - Top-right utility bar (dark mode toggle, fullscreen, print)
  - Progress bar di top
  - 2 slide initial: Cover (slide 1) + Penutup (slide 23) sebagai placeholder
  - `<script>` keyboard nav (arrow keys + space + esc fullscreen) + button click + dark mode toggle + state slide counter

- [ ] Step 2: Test buka di browser, cek slide 1 active, navigate ke slide 23 via arrow key, dark mode toggle work

- [ ] Step 3: Commit
```bash
git add docs/Sosialisasi-HC-Operasional.html
git commit -m "docs(sosialisasi-hc): scaffold — style palette + cover + penutup + controls"
```

---

## Task 2: INTRO Slides 1-4

Slides:
1. Cover (sudah ada di scaffold, polish)
2. Selamat Datang — agenda + duration
3. Role HC di Portal — visual L2 dalam ladder 6-level + 4 area authority
4. Alur Kerja Harian HC — flow diagram big picture

- [ ] Step 1: Edit slide 1 cover — title "Sosialisasi Operasional HC", subtitle "Portal HC KPB", role badge HC, date Mei 2026

- [ ] Step 2: Insert slide 2 — Selamat Datang. Format: agenda list 6 section + total duration "~45 menit"

- [ ] Step 3: Insert slide 3 — Role HC. Visual: ladder 6-level dengan HC di L2 highlighted + 4 card area authority (CMP/CDP/Data/Admin)

- [ ] Step 4: Insert slide 4 — Alur Kerja Harian. Flow horizontal 5 step: Check Notif → Review Evidence → Monitor Assessment → Schedule Renewal → Resolve Issues. Pakai diagram CSS (arrow + box)

- [ ] Step 5: Update slide badge count (semua jadi "/ 23")

- [ ] Step 6: Test navigate slide 1→4 di browser

- [ ] Step 7: Commit
```bash
git commit -m "docs(sosialisasi-hc): Intro 4 slide — cover, agenda, role HC, alur harian"
```

---

## Task 3: CMP Slides 5-8

5. CMP Overview — 4 sub-modul icon grid (Records, Analytics, Pre/Post, Budget)
6. Records Team — monitoring tim + filter cascade visual
7. Analytics Dashboard — compliance + fail rate + trend + expiring soon (4 metric card)
8. Pre/Post Test + Gain Score — flow 5 step: Pre → Training → Post → Gain → Item Analysis

- [ ] Step 1-4: Insert 4 slide dengan layout grid card / flow diagram. Max 3-4 element per slide. Cross-ref ke Panduan §2.x di footer slide.

- [ ] Step 5: Commit `docs(sosialisasi-hc): CMP 4 slide — overview, Records Team, Analytics, Pre/Post`

---

## Task 4: CDP Slides 9-12

9. CDP Overview + Reviewer Chain — visual chain Sr Spv → Section Head → **HC (Final)** badge prominent
10. Coaching Proton Dashboard — global view + drill to coachee + bottleneck tab callout
11. Histori Proton + Export — riwayat per periode + tombol export visual
12. Renewal Certificate Lifecycle — flow expired → schedule → assess → certificate baru

- [ ] Step 1-4: Insert 4 slide. Slide 9 hero = reviewer chain diagram big. Slide 12 = lifecycle circular/linear diagram.

- [ ] Step 5: Commit `docs(sosialisasi-hc): CDP 4 slide — overview chain, dashboard, histori, renewal lifecycle`

---

## Task 5: KELOLA DATA Slides 13-14

13. Silabus + Guidance Files — visual split 2 card: Silabus (CRUD + Import) + Guidance (Upload + History)
14. Override Data Pekerja — diagram kapan dipakai + warning card "sensitif, ter-audit"

- [ ] Step 1-2: Insert 2 slide. Slide 14 highlight warning callout.

- [ ] Step 3: Commit `docs(sosialisasi-hc): Kelola Data 2 slide — silabus+guidance, override`

---

## Task 6: ADMIN PANEL Slides 15-19

15. Admin Panel Map — 16 menu grid visual (kategori: Workers, Assessment, Mapping, Files, Maintenance)
16. Kelola Pekerja + Bank Soal — split slide 2 fitur paling sering
17. Create + Monitoring Assessment — alur 4 step: Create → Assign → Monitor → Force-Close
18. Coach-Coachee Mapping + Workload — split slide
19. Maintenance + Audit Log — 2 panel: Maintenance scope All/Specific + Audit investigation playbook 5 step

- [ ] Step 1-5: Insert 5 slide. Slide 15 hero = 16-cell grid dengan icon per menu. Slide 19 = 2 card panel.

- [ ] Step 6: Commit `docs(sosialisasi-hc): Admin Panel 5 slide — map, pekerja+soal, create+monitor, mapping, maintenance+audit`

---

## Task 7: CLOSING Slides 20-23

20. Notifikasi & Workflow — bell icon visual + 9 event matrix singkat
21. Tugas HC Cepat — daily/weekly/monthly checklist visual 3 kolom
22. Reference Card — link ke Panduan Operasional HC + URL cheatsheet (link relatif `Panduan-Operasional-HC-PortalHC-KPB.html`)
23. Q&A / Terima Kasih — thank you + kontak + decorative

- [ ] Step 1-4: Insert 4 slide. Slide 22 = card grid 3 reference doc dengan link relatif.

- [ ] Step 5: Commit `docs(sosialisasi-hc): Closing 4 slide — notifikasi, tugas cepat, reference, penutup`

---

## Task 8: Visual Review + Print Test + Tag

- [ ] Step 1: Buka file di browser, scroll/navigate semua 23 slide. Cek visual:
  - Slide active visible, sliding transition smooth
  - Slide badge "N / 23" correct di tiap slide
  - Sticky bottom controls work (prev/next disabled di edges)
  - Top utility bar (dark mode toggle, print) work
  - Dark mode flip color OK semua slide
  - Keyboard nav arrow/space/esc work

- [ ] Step 2: Print preview (Ctrl+P) — cek 23 halaman terhasil, 1 slide = 1 halaman A4 landscape

- [ ] Step 3: Visual checklist:
  - [ ] No wall of text (>5 bullet per slide)
  - [ ] Cross-ref ke Panduan ada di ≥5 slide complex
  - [ ] Color palette konsisten (teal primary, amber accent)
  - [ ] Tidak ada endpoint URL `/Controller/Action` di body slide

- [ ] Step 4: Final commit + tag
```bash
git commit -m "docs(sosialisasi-hc): final visual + print polish — v1.0" --allow-empty
git tag sosialisasi-hc-operasional-v1.0
```

- [ ] Step 5: Update memory (`project_sosialisasi_hc_operasional_v1.md` + index)

---

## Self-Review

**Spec coverage:** semua 23 slide ter-cover di Task 2-7. Acceptance criteria ter-cover di Task 8.

**Placeholder scan:** Slide content high-level descriptions di task. Implementer kelas tinggi: tulis konten konkret saat compose (judul, bullet, diagram CSS) — bukan placeholder, tapi "design intent". OK karena single-author execution.

**Type consistency:** Slide counter `data-slide="N"` numerik. Badge text "N / 23" konsisten. Anchor ID `slide-N` kalau perlu.
