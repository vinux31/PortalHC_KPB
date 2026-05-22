# Sosialisasi-Internal-Tim-HC Review & Fix — Design

**Date:** 2026-05-22
**File:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` (41 slides, 3756 baris)
**Trigger:** Post-merge audit. File hasil merge Sosialisasi-Aplikasi → Internal-Tim-HC (per memory `project_merge_sosialisasi_aplikasi_to_internal_hc`). Risiko redundansi + struktur BAGIAN broken.

---

## Goal

Audit menyeluruh + fix penuh terhadap 41 slide:
1. Konten salah (klaim faktual vs source code)
2. Redundansi antar slide
3. Urutan/struktur BAGIAN

User scope decision: **Opsi 1 = fix semua finding (K1-K5 + R1-R5 + U1-U4 + N1-N4)**, equal-weight tiga dimensi, cross-check vs kode aplikasi.

---

## Verification Method

Klaim numerik/struktural di slide dibandingkan terhadap:
- `Models/UserRoles.cs` (role count)
- `Views/Admin/Index.cshtml` (Admin Panel menu count)
- `Migrations/20260317113635_AddAssessmentCategoriesTable.cs` (kategori assessment)
- `Models/CoachingSession.cs` (reviewer chain workflow)
- `Controllers/` (BP platform existence check)

---

## Findings & Fixes

### Section A — Konten Salah (5 items)

| ID | Slide | Lokasi | Problem | Fix |
|---|---|---|---|---|
| **K1** | 8 | line 2168 | "Admin Panel 16 menu operasional" | Ganti ke **14 menu** |
| **K2** | 32 | line 3318 | "4 section ABCD — total 14 menu yang HC kelola" | **No-op (sudah benar)** — verifikasi ulang vs `Views/Admin/Index.cshtml`: A=4, B=4, C=5, D=1 = 14 |
| **K3** | ~~32 footer~~ | — | ~~"Bab 5 Admin Panel (16 task lengkap)"~~ | **DROPPED** — re-verify Panduan: Bab 5 = 5.1–5.16 = **16 task** ✓ benar. Angka 16 merujuk Panduan task count, bukan menu count. |
| **K4** | 17 + 25 | line 2562-2611 + 2982 | Kontradiksi: slide 17 sequential strict vs slide 25 step 5 "Coach+SrSpv+SH+HC paralel" | Resolve: kode tidak punya state machine paralel. Pilih **sequential** sebagai canonical (slide 17 versi benar). Edit slide 25 step 5 jadi "Coach submit → SrSpv → SH → HC sequential (sesuai chain slide 17/22 baru)". |
| **K5** | ~~32~~ | — | ~~Section C list 5 menu vs kode 6~~ | **DROPPED** — recount kode (`Views/Admin/Index.cshtml`): Section C = 5 menu (Manage A&T, Monitoring, Audit Log, Categories, Renewal). Subagent earlier miscount. Slide listing benar. |
| **K6** | 40 | line 3664 | "Reference lengkap **38 task** per modul + Glossary + Troubleshooting + URL Cheatsheet" | Ganti ke **42 task** — Panduan recount: Bab 1=4, Bab 2=8, Bab 3=8, Bab 4=3, Bab 5=16, Bab 6=3 → total 42 |
| **K7** | 30 | line 3243 | Subtitle "Tabel silabus per Bagian/Unit/Track + Coaching Guidance (**3 tab**)" — wording misleading. 3 tab = parent page (Status, Silabus, Guidance), bukan 3 sub-tab dalam Guidance. | Reword: "ProtonData Index — 3 tab: Status Sync · Silabus · Coaching Guidance" |

### Section B — Redundansi (5 items)

| ID | Slide(s) | Problem | Fix |
|---|---|---|---|
| **R1** | 6 + 8 | Tangga Role L1-L6 ditampilkan dua kali (overlap 90%) | Merge: slide 8 dibuang ladder repeat, fokus AREA HC (CMP/CDP/Kelola Data/Admin Panel cards) + cross-ref ke slide 6 untuk hierarki. |
| **R2** | 18 + 21 | Dua intro "Program 3 tahun" mirip | Differentiate subtitle: slide 18 fokus **Assessment side** ("Format penilaian 3 tahun"), slide 21 fokus **Coaching side** ("Program pendampingan 3 tahun") |
| **R3** | 24 + 25 + 26 | Th 1-2 vs Th 3 split diulang 3 kali (tabel, swim-lane reguler, swim-lane mahir) | Pertahankan (bentuk visual beda). Tambah cross-ref eksplisit di slide 25 dan 26: "Komparasi 5-aspek lengkap → slide 24" |
| **R4** | 17 vs 25 | Reviewer Chain standalone (slide 17) muncul sebelum Coaching intro + diulang di slide 25 step 5 | **Pindahkan slide 17 ke posisi setelah slide 20 (Alur Proton Th 3)**, jadikan transition card sebelum Coaching block. Bukan dihapus, repositioned. |
| **R5** | 22 | IDP & Training Records terjepit dalam narasi Coaching (antara 21 Dual Track dan 23 Hierarki) | **Pindah slide 22 ke setelah slide 29 (Renewal)**, sebagai komponen pelengkap CDP non-coaching. |

### Section C — Urutan Salah (4 items)

**Restruktur BAGIAN — full reorder slide 17-40:**

| ID | Problem | Fix |
|---|---|---|
| **U1** | BAGIAN 3 dipakai 2 konteks beda (Coaching slide 21-26 + Kelola Data slide 30-31) | Pisahkan: Coaching tetap di BAGIAN 3, Kelola Data → sub-section di BAGIAN 4 Admin Panel (section B Proton mgmt sudah ada slot) |
| **U2** | BAGIAN bounce 1→2→3→**2**→**3**→4 (slide 27-29 regresi ke BAGIAN 2) | Naikkan slide 27-29 ke BAGIAN 3 (mereka Coaching monitoring tools, bukan Assessment Proton) |
| **U3** | Slide 17 (Reviewer Chain) di BAGIAN 2 sebelum reader tahu apa itu Coaching | Repositioned per R4 |
| **U4** | Slide 39 (Integrasi & Keamanan) di akhir tanpa BAGIAN | Pindah ke awal — letakkan setelah slide 7 (Cara Mengakses) sebagai foundational |

**Target struktur BAGIAN baru (sequential clean):**

```
BAGIAN 0 — Pengenalan (slide 1-9):
  1. Cover
  2. Agenda
  3. Latar Belakang
  4. Apa Itu HC Portal
  5. 3 Platform CMP/CDP/BP
  6. Struktur Role (10/6) ← merged ladder
  7. Cara Mengakses
  8. Integrasi & Keamanan ← moved from old #39 (foundation extends akses)
  9. Posisi HC: AREA cards (no ladder) ← ex-#8 trimmed (audience-specific transition)
  10. Alur Kerja Harian HC ← ex-#9 (concrete HC workflow segue ke BAGIAN 1)

BAGIAN 1 — CMP (slide 11-17):
  11. CMP Overview (ex-#10)
  12. Records Team (ex-#11)
  13. Analytics Dashboard (ex-#12)
  14. Sistem Assessment (ex-#13)
  15. 5 Kategori (ex-#14)
  16. Alur Assessment 7-Step (ex-#15)
  17. Pre/Post Test (ex-#16)

BAGIAN 2 — Assessment Proton (slide 18-20):
  18. Assessment Proton intro (ex-#18, subtitle differentiated)
  19. Alur Proton Th 1-2 (ex-#19)
  20. Alur Proton Th 3 (ex-#20)

BAGIAN 3 — Coaching Proton / CDP (slide 21-29):
  21. Coaching Dual Track (ex-#21, subtitle differentiated)
  22. CDP Reviewer Chain (ex-#17, repositioned)
  23. Hierarki Kompetensi (ex-#23)
  24. Progresi per Tahun (ex-#24)
  25. Alur Coaching Reguler (ex-#25, step 5 fixed)
  26. Alur Coaching Mahir (ex-#26)
  27. Coaching Dashboard (ex-#27)
  28. Histori Proton (ex-#28)
  29. Renewal Lifecycle (ex-#29)
  30. IDP & Training Records (ex-#22, moved)

BAGIAN 4 — Admin Panel + Kelola Data (slide 31-37):
  31. Admin Panel Landing (ex-#32, no count fix needed — already "14 menu")
  32. Manajemen Pekerja (ex-#33)
  33. Assessment Monitoring (ex-#34)
  34. Coach Mapping (ex-#35)
  35. Silabus + Guidance (ex-#30, moved here as part of section B)
  36. Override Data Pekerja (ex-#31, moved here)
  37. Maintenance + Audit Log (ex-#36)

BAGIAN 5 — Closing (slide 38-41):
  38. Notifikasi & Workflow (ex-#37)
  39. Tugas HC Cepat (ex-#38)
  40. Reference Card (ex-#40)
  41. Penutup (ex-#41)
```

Total tetap **41 slide** (no add/remove kecuali R1 merge yg compensated oleh slide 10 baru). Renumber data-slide + badge SLIDE X/41 + section-eyebrow BAGIAN N.

### Section D — Nice-to-Have (4 items)

| ID | Slide | Problem | Fix |
|---|---|---|---|
| **N1** | 11, 12, 33, 34, 35, 36, 37 (post-renumber: 12, 13, 32, 33, 34, 37, 38) | Subtitle bake-in live numeric data (rot risk) | Generalisasi: angka spesifik → frase ("snapshot dev env", "metric real-time"). Pertahankan demo angka di slide-body, hapus dari subtitle. |
| **N2** | comment line 3686 | `<!-- SLIDE 23: PENUTUP -->` (relic 22-slide version) | Update ke `<!-- SLIDE 41: PENUTUP -->` + audit semua comment `SLIDE N:` agar konsisten |
| **N3** | MERGED CLUSTER comments + placeholder IDs (901-918) line 1840, 2330, 2613, 3604 | Relic merge process, clutter, non-user-visible | Hapus/simplify ke comment singkat per cluster |
| **N4** | slide 33 subtitle "12 user aktif: 1 Admin, 1 HC, 5 Coachee..." | Live data bake-in | Generalisasi ("snapshot user dev env") |

---

## Out of Scope

- **Tidak menyentuh CSS/JS** kecuali wajib karena renumber (TOTAL constant tetap 41).
- **Tidak menambah/hapus slide** kecuali R1 merge (kompensasi oleh AREA-only slide 10 baru).
- **Tidak edit file `Sosialisasi-Aplikasi-PortalHC-KPB.html`** (ref masih 22 slide, tetap valid).
- **Tidak edit file `Panduan-Operasional-HC-PortalHC-KPB.html`** — review terpisah jika dibutuhkan.
- **Tidak ubah screenshot** (`docs/sosialisasi-screenshots/*.png`).

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Renumber 41 data-slide attribute + 39 badge "SLIDE X/41" rentan typo | Bulk find/replace per slide; verify counter JS still hits TOTAL=41; manual scan post-edit |
| Pemindahan slide bisa pecah cross-ref dalam isi slide ("lihat slide N") | Grep `slide ` references; update sebelum/sesudah reorder |
| User mau preview sebelum commit massive change | Commit per-section bertahap (A → B → C → D), checkpoint user review per section |
| BAGIAN renumber mungkin tidak diterima audience yg sudah baca v1.1 | Pertahankan judul slide sama, hanya badge BAGIAN + section-eyebrow yg berubah |

---

## Implementation Order (Execute Plan Preview)

Berurutan, masing-masing 1 commit atomik:

1. **Phase A — Konten Salah** (K1-K5): edit angka + reviewer chain text. Low-risk text changes.
2. **Phase B — Redundansi minor** (R2, R3): subtitle differentiation + add cross-refs. No move.
3. **Phase C — Restruktur** (R1 merge, R4 move, R5 move, U1-U4 reorder + BAGIAN relabel): heavy move. Single big commit.
4. **Phase D — Nice-to-Have** (N1-N4): cleanup subtitle + comment.
5. **Phase E — Verify**: open `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB.html` di browser via Playwright MCP, smoke-test navigation 1→41, verify counter + BAGIAN labels.

---

## Success Criteria

- Tidak ada klaim numerik yang kontradiktif (Admin Panel 15 di semua tempat).
- Reviewer chain konsisten (sequential di slide 17 lama + step 5 alur coaching).
- BAGIAN label monotonic 0→5 tanpa regression.
- Tidak ada redundansi 90% antar slide.
- Counter `1 / 41` ... `41 / 41` benar di browser.
- Tiap slide punya BAGIAN label yang sesuai posisi.
- Tidak ada comment `<!-- SLIDE 23: PENUTUP -->` yang salah.
