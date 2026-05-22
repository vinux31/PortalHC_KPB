# Sosialisasi Umum PortalHC KPB — Design Spec

**Tanggal:** 2026-05-22
**Source file:** `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (22 slide, 2429 baris)
**Target file:** `docs/Sosialisasi-Umum-PortalHC-KPB.html` (19 slide)
**Strategi:** Edit in-place + rename via `git mv`

---

## 1. Goal & Audience

**Audiens:** Pekerja Pertamina internal non-HC — campuran A (calon user, pekerja KPB non-HC: operator, supervisor produksi, engineer, manager fungsi lain) + B (penonton stakeholder internal Pertamina luar KPB: HQ Jakarta, RU lain, korporat).

**Goal:** Sosialisasi awareness + onboarding ringan Portal HC ke audiens umum Pertamina. Konten harus jawab 2 pertanyaan sekaligus:
- A: "Saya pakai ini gimana?"
- B: "Ini aplikasi apa, kenapa relevan?"

**Non-goal:**
- Training mendalam (itu domain `Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html`)
- Branding eksternal Pertamina umum / publik
- PDF export, video embed, translasi English, print stylesheet

---

## 2. File Strategy

- **Rename via `git mv`** untuk preserve history:
  - `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` → `docs/Sosialisasi-Umum-PortalHC-KPB.html`
- **Edit in-place** modif 19 slide
- **Update `<title>`:** "Sosialisasi Aplikasi HC Portal KPB" → "Sosialisasi Portal HC KPB — Untuk Pekerja Pertamina"
- File `Sosialisasi-Aplikasi-PortalHC-KPB.html` lama tak diarsipkan (belum punya tag versi-locked di git)

---

## 3. Slide Map — Final 19 Slide

| New # | Original `data-slide` | Slide | Aksi |
|-------|---|---|---|
| 1 | 1 | Cover | RETONE (subtitle + cover-meta-eyebrow) |
| 2 | 2 | Agenda | RETONE (update list section sesuai struktur baru) |
| 3 | 3 | Latar Belakang | KEEP |
| 4 | 4 | Apa Itu HC Portal KPB | RETONE (subtitle: "Tim HC" → "pekerja Kilang Pertamina Balikpapan") |
| 5 | 5 | 3 Platform Terpadu (CMP/CDP/BP) | RETONE (jelasin singkat tiap platform, hindari jargon HC murni) |
| 6 | 6 | Struktur Role Pengguna | RETONE (highlight "pekerja KPB umumnya masuk role Worker/Coachee") |
| 7 | 7 | Sistem Assessment | **MERGE+RETONE** (absorb Pre/Post via extend `tip-bar` 1-liner; lihat §4 Catatan Implementasi) |
| 8 | 8 | 5 Kategori Assessment Umum | KEEP |
| 9 | 9 | **Alur Assessment 7-Step** (FLOW) | KEEP |
| — | ~~10~~ | ~~Pre/Post Test Gain Score~~ | **CUT** (info absorbed ke slide 7) |
| 10 | 11 | Assessment Proton | RETONE (lead-in: "Proton = program kompetensi 3 tahun Panelman/Operator") |
| 11 | 12 | **Alur Proton T1&2** (FLOW) | KEEP |
| 12 | 13 | **Alur Proton T3** (FLOW) | KEEP |
| — | ~~14~~ | ~~Coaching Proton Dual Track~~ | **CUT** (detail Reviewer Chain = internal flow) |
| — | ~~15~~ | ~~Hierarki Kompetensi per Track~~ | **CUT** (revised dari merge — tree-grid + table tak feasible 1 slide; audiens umum cukup Progresi) |
| 13 | 16 | Progresi Kompetensi per Tahun | KEEP (standalone, gantikan rencana merge) |
| 14 | 17 | **Alur Coaching Pemula 9-Step** (FLOW) | KEEP |
| 15 | 18 | **Alur Coaching Mahir 9-Step** (FLOW) | KEEP |
| 16 | 19 | IDP & Training Records | RETONE (highlight benefit: sertifikat, jejak karir pekerja) |
| 17 | 20 | Integrasi & Keamanan | RETONE (angle "data pekerja aman & terotorisasi" bukan "API/middleware") |
| 18 | 21 | Cara Mengakses HC Portal | KEEP |
| 19 | 22 | Terima Kasih / Penutup | KEEP |

**Math verifikasi:** 22 original − 3 CUT (slide 10, 14, 15) = 19 ✓

**Tally aksi (22 slide accounted):**
- KEEP utuh: 10 slide (3, 8, 9, 12, 13, 16, 17, 18, 21, 22)
- RETONE: 8 slide (1, 2, 4, 5, 6, 11, 19, 20)
- MERGE+RETONE: 1 slide (7 absorb 10)
- CUT: 3 slide (10, 14, 15)
- Total: 10+8+1+3 = 22 ✓

**Flow slide intact (5):** Alur Assessment 7-step (slide 9), Alur Proton T1&2 (slide 12), Alur Proton T3 (slide 13), Alur Coaching Pemula 9-step (slide 17), Alur Coaching Mahir 9-step (slide 18). Konstrain user: "alur proses tetap" — interpretasi konservatif: **zero edit** body content, subtitle, caption. Hanya update `slide-badge` numeric (jika standard format, e.g., slide 9 `SLIDE 9 / 22` → renumber) — custom badge slide 13 (`OFFLINE MODE`) dan slide 18 (`LEVEL MAHIR`) tetap utuh.

---

## 4. Tone & Voice

**Decision:** Mirror tone Aplikasi existing (formal-impersonal-factual, bullet-driven, tanpa "Bapak/Ibu" / "Anda", tanpa storytelling). Yang diretone hanya **framing**, bukan voice fundamental.

**Substitusi framing:**

| Existing (HC-internal) | Retone (umum) |
|---|---|
| "Tim Human Capital" / "Tim HC" | "pekerja Kilang Pertamina Balikpapan" / "peserta" |
| "untuk HC" / "yang dilakukan HC" | "untuk pekerja" / "yang dialami peserta" |
| "Sistem informasi terpadu untuk Tim Human Capital..." | "Sistem informasi pengembangan kompetensi pekerja Kilang Pertamina Balikpapan" |
| "API / middleware / integrasi backend" (slide 20) | "data pekerja aman & terotorisasi sesuai role" |

**Penting:** Substitusi framing **HANYA diterapkan di 8 slide RETONE + 1 MERGE+RETONE** (slide 1, 2, 4, 5, 6, 7, 11, 19, 20 original). **Flow slide (9, 12, 13, 17, 18) zero edit** — sesuai konstrain "alur proses tetap".

**Catatan Implementasi:**

**Slide 7 absorb Pre/Post (overflow mitigation):**
- Slide 7 existing: 2 `jenis-card` (Umum + Proton) × 3 row + 1 `tip-bar` — density medium-high
- Format absorb: **extend `tip-bar` text** dengan 1-liner Pre/Post:
  - Existing: `💡 Assessment Umum untuk evaluasi reguler per batch unit / jenis kompetensi · Proton untuk program pengembangan 3 tahun`
  - Retone: `💡 Assessment Umum untuk evaluasi reguler per batch / jenis kompetensi · Proton untuk program pengembangan 3 tahun · Pre/Post Test mengukur Gain Score peserta sebelum vs sesudah pelatihan`
- Minimal-invasive, no new card / no layout shift, no overflow risk

---

## 5. Visual & Styling

- **No CSS overhaul.** Pertahankan semua kelas existing (`.slide`, `.jenis-card`, `.alur-stepper`, `.swim-row`, `.progresi-table`, `.tree-grid`, `.tip-bar`, `.section-eyebrow`, dst)
- **`section-eyebrow` & `slide-badge`:** sudah ada di Aplikasi (slide 13, 15, 16, 18 pakai eyebrow) — pertahankan existing, tidak tambah baru
- **Cover (slide 1):** ganti `cover-meta-eyebrow` "Sosialisasi" → "Sosialisasi untuk Pekerja Kilang Pertamina Balikpapan", subtitle baru
- **Tidak adopsi `hc-callout`** dari Internal-Tim-HC (overhead untuk audiens umum)

---

## 6. Implementation Approach

Urutan kerja eksekusi (preview untuk implementation plan):

1. `git mv docs/Sosialisasi-Aplikasi-PortalHC-KPB.html docs/Sosialisasi-Umum-PortalHC-KPB.html`
2. Update `<title>` HTML head
3. Patch slide 7 — extend `tip-bar` text absorb Pre/Post Test gain score 1-liner
4. **Cut 3 slide DOM block:** slide 10 (Pre/Post Test standalone), slide 14 (Coaching Dual Track), slide 15 (Hierarki Kompetensi)
5. **Reorder DOM** ke linear logical sequence (existing punya DOM-order chaos: data-slide=10 DOM-before 9, data-slide=19 IDP sandwich antara 14 dan 15)
6. **Renumber `data-slide`** sequential 1..19 sesuai linear order
7. **Update 18 standard `<div class="slide-badge">SLIDE N / 22</div>`** jadi `SLIDE N / 19` dengan N baru
8. **Custom badge tak diubah:** slide 13 (was 13 Alur Proton T3) `🎤 OFFLINE MODE`, slide 15 (was 18 Coaching Mahir) `🎯 LEVEL MAHIR`
9. Update JS hardcode:
   - `<span id="totalNum">22</span>` (line ~2356) → `19`
   - `const total = 22;` (line ~2363) → `19`
10. Retone 8 slide copy (Cover, Agenda, Apa Itu, 3 Platform, Role, Assessment Proton, IDP, Integrasi)
11. Verifikasi browser lokal:
    - Navigasi keyboard (Arrow Left/Right, Home/End, Space, PageUp/Down)
    - Klik nav button Prev/Next
    - Slide counter `N / 19` update
    - Progress bar update
    - Dark mode toggle (D), Fullscreen (F)
    - Tidak ada slide kosong / DOM duplicate

---

## 7. Risks & Mitigation

| Risk | Mitigation |
|---|---|
| Slide 7 absorb Pre/Post overflow viewport | Pakai extend `tip-bar` 1-liner (no new card / no layout shift) |
| Renumber `data-slide` break JS navigation | JS pakai `querySelector([data-slide="${n}"])` + `const total` — renumber + update const = aman |
| Hard-coded CSS rule per `data-slide` value | Audit `<style>` block via grep `data-slide=` — tak ada CSS selector ke data-slide value spesifik (hanya `.slide` umum) |
| Anchor / internal link pakai `data-slide` number | File tak punya `<a href="#slide-N">` — navigasi via JS `showSlide()` saja |
| DOM reorder break visual order | Test browser lokal urutan slide 1..19 sesuai logical sequence |
| Merge slide 15+16 overflow (sudah revised → CUT slide 15) | N/A — sudah resolved via cut Hierarki |

---

## 8. Acceptance Criteria

- [ ] File `docs/Sosialisasi-Umum-PortalHC-KPB.html` exist, `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` deleted via `git mv` (history preserved, `git log --follow` jalankan tanpa break)
- [ ] `<title>` = "Sosialisasi Portal HC KPB — Untuk Pekerja Pertamina"
- [ ] **19 slide total** di DOM, tak ada slide 10/14/15 original (Pre/Post, Coaching Dual Track, Hierarki)
- [ ] `data-slide` sequential 1..19 di DOM linear order
- [ ] DOM block order match logical sequence (no DOM-order chaos)
- [ ] `<span id="totalNum">` = `19`
- [ ] JS `const total = 19;`
- [ ] **18 standard badge** "SLIDE N / 22" updated jadi "SLIDE N / 19" dengan N baru
- [ ] **Custom badge tetap utuh:** slide Alur Proton T3 `🎤 OFFLINE MODE`, slide Coaching Mahir `🎯 LEVEL MAHIR`
- [ ] Slide 7 (Sistem Assessment) `tip-bar` extended dengan Pre/Post 1-liner, no overflow viewport
- [ ] **5 flow slide bit-identical body content** dengan original: Alur Assessment 7-step, Proton T1&2, Proton T3, Coaching Pemula 9-step, Coaching Mahir 9-step
- [ ] Tone "Tim HC" → "pekerja" diterapkan di 8 slide retone (Cover, Agenda, Apa Itu, 3 Platform, Role, Assessment Proton, IDP, Integrasi)
- [ ] Browser lokal verifikasi:
  - [ ] Navigasi Arrow Left/Right works
  - [ ] Home/End jump ke slide 1 / 19
  - [ ] Klik Prev/Next button works
  - [ ] Slide counter render `N / 19` di nav bar
  - [ ] Progress bar render proportional (1/19, 2/19, dst)
  - [ ] Dark toggle (D) + Fullscreen (F) tetap berfungsi
  - [ ] Tak ada slide kosong / DOM duplicate

---

## 9. Out-of-Scope

- PDF export (separate effort)
- Translasi English
- Cetak-friendly print stylesheet
- Video / animation embed
- Integrasi ke Portal HC live (ini file standalone HTML)
- Update file Internal-Tim-HC (file paralel, beda audiens, tak terdampak)
- Reorder slide order di luar konsekuensi cut/merge (e.g., move IDP sebelum Coaching = out-of-scope; IDP tetap di posisi after Coaching Mahir sesuai original)

---

## 10. Reference

- Source: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (2429 baris, 22 slide)
- Paralel: `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (3955 baris, 38 slide — untuk audiens Tim HC, tak terdampak)
- Spec terkait sebelumnya: `2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md` (arah berbeda: Aplikasi→Internal, sudah merged)
- CLAUDE.md project: `docs/DEV_WORKFLOW.md`, `docs/SEED_WORKFLOW.md`
