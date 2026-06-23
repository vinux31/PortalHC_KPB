---
phase: 415
title: Section Foundation + Import Excel Diperluas
review_type: code-review (xhigh recall, multi-agent workflow)
status: reviewed
date: 2026-06-23
base_commit: dca134bc
head_commit: 9a5afd33
passes:
  - pass-1: 10 finder angles (Altitude+Conventions+sweep gagal koneksi) -> 41 raw -> 37 verified -> 15 final
  - pass-2 (gap-fill): Altitude+Conventions+Sweep re-run -> 16 raw -> 11 verified
scope_files:
  - Controllers/AssessmentAdminController.cs
  - Controllers/CMPController.cs
  - Data/ApplicationDbContext.cs
  - Models/AssessmentPackage.cs
  - Views/Admin/ImportPackageQuestions.cshtml
  - Views/Admin/ManagePackageQuestions.cshtml
summary_counts:
  high: 4
  medium: 5
  low: 6
---

# Phase 415 Code Review — Consolidated

Dua pass workflow (xhigh recall). Pass-1 cover 5 angle correctness + Reuse/Simplification/Efficiency.
Pass-2 (gap-fill) re-run Altitude + Conventions + Sweep yang gagal koneksi di pass-1.
Hasil di-dedup per root-cause: **2 akar dominan** + isu satelit.

**Akar A — sibling grouping/sync tak Pre/Post + SamePackage-aware** (H1, H4, M1, M3, M4, M5, L3, L4)
**Akar B — opsi A–F dinamis belum konsisten lintas-surface** (H2, H3, L5/L6 sebagian)

---

## 🔴 HIGH

### H1 — Guard Section TIDAK Pre/Post-aware → salah-blok mulai ujian
**`Controllers/CMPController.cs:1074-1119`** (StartExam D-13 re-guard) + kembar **`AssessmentAdminController.cs:7115-7128`** (validasi import).

Sibling di-grup pakai `Title+Category+Schedule.Date` TANPA `AssessmentType`. Assignment ujian sebenarnya (`CMPController.cs:1038-1042`) pakai `SiblingSessionQuery.SiblingPrePostAwarePredicate` yang **memisahkan Pre/Post**. Guard membandingkan paket Pre lawan Post — perbandingan yang assignment tak pernah lakukan.

- **Trigger:** Pre & Post **tanggal sama** (diizinkan: validasi cuma `PostSchedule > PreSchedule`) + struktur Section beda sah antar Pre/Post.
- **Dampak:** worker di-hard-block `"struktur Section antar-paket tidak identik"` padahal paket Pre konsisten internal.
- **Fix (drop-in, ditemukan pass-2):** ganti `.Where(...)` inline dengan `SiblingSessionQuery.SiblingPrePostAwarePredicate(title, category, scheduleDate, assessmentType)` — helper SUDAH ada & dipakai 30 baris di atas. Terapkan ke guard CMP + validasi import.
- Confidence: **CONFIRMED** (4 verifier independen).

### H2 — Huruf benar E/F tak cek opsi non-kosong → soal ungradeable senyap
**`AssessmentAdminController.cs:7268-7274`** (whitelist MC/MA dilebarkan A→F) + **`:7100`** (`RowIsValid`, sama).

Opsi dibangun cuma dari cell non-kosong (`.Where(!IsNullOrWhiteSpace)`). Huruf benar nunjuk opsi blank → flag `IsCorrect` tak ke-apply.

- **Trigger:** baris MC, Opsi A-D terisi, E/F blank, `Jawaban Benar='E'` → ke-import (`added++`) dengan **0 opsi IsCorrect** → semua worker skor 0. MA `'A,E'` E blank → kunci jawaban salah. `RowIsValid` lolos sama → count validation pun lolos.
- **Fix:** sesudah build opsi, assert ≥1 `IsCorrect` DAN tiap huruf benar memetakan ke opsi non-kosong; tolak baris kalau tidak. Selaraskan `RowIsValid`.
- Confidence: **CONFIRMED**.

### H3 — Surface A–F tak konsisten: edit (controller + view + JS) cap A–D → double-correct & data un-editable
**`AssessmentAdminController.cs:7862-7880`** (loop `for i<4`) + **`Views/Admin/ManagePackageQuestions.cshtml:374`** (form render A-D) + **JS `loadEditForm` ~673-687** (`if (i<4)` drop opsi idx≥4).

Import + `SyncPackagesToPost` bikin soal 5-6 opsi; SEMUA jalur edit cap A-D.

- **Trigger:** import MA 6-opsi benar `{E}` (slot 4 IsCorrect=true). Admin buka edit (E tak ke-render/tak kelihatan tercentang), centang A, simpan. Loop set A=true, **tak sentuh slot E** → soal punya **A+E dua-duanya benar** → grading korup; admin tak bisa lihat/perbaiki kunci asli.
- **Fix:** thread satu konstanta `MaxOptions=6` lewat build/validate/edit-controller/edit-JS/preview. Minimal sementara: di `EditQuestion` hapus/overwrite slot ≥4 sebelum SaveChanges, render E/F di form. (Komentar kode menunda render E/F ke Phase 418 → minimal kasih guard agar tak korup data hari ini.)
- Confidence: **CONFIRMED** (controller + view + JS, pass-1 & pass-2).

### H4 — `SyncPackagesToPost` ABSEN di semua surface mutasi struktur (import + 4 Section CRUD) → Post SamePackage basi
**Import:** `AssessmentAdminController.cs:~7333` (`UploadPackageQuestions`).
**Section CRUD (pass-2, BARU):** `CreateSection ~6285`, `EditSection ~6322`, `DeleteSection ~6354`, `SetAllSectionsNewPage ~6391`.

Tiap mutasi soal (`CreateQuestion:7666`, `EditQuestion:7944`, dll) panggil `SyncPackagesToPost` saat PreTest+SamePackage. **5 endpoint di atas tidak** — satu-satunya region mutasi yang lupa sync.

- **Dampak:** tambah/rename/hapus Section atau import Excel ke paket Pre SamePackage → Post tetap struktur lama (section row + SectionId divergen), sampai HC kebetulan edit soal (satu-satunya jalur re-sync). Langgar invariant SamePackage; ganggu pagination (417) + scoped shuffle (416). Bila Pre/Post se-tanggal → kombinasi H1 → hard-block ujian.
- **Fix:** tambah blok `if (PreTest && linkedPost.SamePackage) await SyncPackagesToPost(...)` di akhir `UploadPackageQuestions` + 4 endpoint Section CRUD. (Idealnya 1 helper sync dipanggil dari semua mutasi.)
- Confidence: **CONFIRMED**. Inti (Post basi) solid; eskalasi "blokir total" kondisional (butuh se-tanggal, lihat H1).

---

## 🟠 MEDIUM

### M1 — `incomingCounts` dihitung sebelum dedup fingerprint
**`AssessmentAdminController.cs:7133`**. Count per-Section pakai `rows.Where(RowIsValid)` termasuk duplikat; dedup fingerprint skip duplikat saat insert. Count check lolos (10==10) tapi cuma 9 ke-insert. **Fix:** dedup dulu baru hitung, atau re-validasi pasca-insert. CONFIRMED.

### M2 — Deteksi format Excel by jumlah kolom
**`AssessmentAdminController.cs:6993-6994`** — `isNewFormat = colCount > 9` via `LastCellUsed()`. Konten nyasar di header kolom 10+ (file lama 9-kolom) → ke-deteksi format baru → peta kolom geser (Jawaban Benar dibaca jadi Opsi E, dst) → korupsi senyap. **Fix:** validasi **nama header**, bukan jumlah kolom. CONFIRMED.

### M3 — Validasi count import abaikan soal existing paket target (additive import) — BARU pass-2
**`AssessmentAdminController.cs:7133`** (distinct dari M1). Import bersifat additive (`order = pkg.Questions.Count + 1`), tapi `incomingCounts` cuma hitung baris Excel INCOMING, tak tambah soal pre-existing paket target.

- **Trigger:** Paket B sudah punya 2 soal Section-1; sibling punya 2. Import 2 baris Section-1 lagi → `incoming=2==sibling=2` lolos, tapi pasca-insert B = 4 vs sibling 2 → StartExam guard blok seluruh grup.
- **Fix:** masukkan `pkg.Questions` grouped-by-section ke baseline `incomingCounts`. CONFIRMED.

### M4 — Logika banding per-Section di-duplikat import vs guard, sudah divergen — BARU pass-2 (altitude)
**`AssessmentAdminController.cs:7155-7176`** vs **`CMPController.cs:1091-1111`**. Blok `GroupBy(q=>q.Section?.SectionNumber).ToDictionary(...)` + pairwise-compare ada di 2 tempat dengan bentuk beda: import = `incoming-vs-EVERY-sibling` + full mismatchList; guard = `ref-vs-each-sibling` + short-circuit `break`. Sudah out-of-sync → import & StartExam bisa **tak sepakat** soal valid/tidak.

- **Fix:** ekstrak `CompareSectionStructure(...)` shared (kembali mismatch list; guard cek `.Any()`). Sekaligus tutup akar H1 (sibling key 3 salinan). CONFIRMED.

### M5 — Section re-guard cuma di jalur first-build (`assignment == null`) — BARU pass-2 (PLAUSIBLE)
**`CMPController.cs:1067`**. Guard drift nested di `if (assignment == null)`; jalur resume (`assignment != null`) lewat tanpa cek Section.

- **Catatan verifier:** ada safety-net independen `SavedQuestionCount != currentQuestionCount` → "Soal ujian telah berubah" (`:1178-1191`), dan tak re-blok worker mid-exam itu defensible. **Low-confidence → keputusan desain, bukan bug pasti.** Verifikasi saat UAT. PLAUSIBLE.

---

## 🟡 LOW

### L1 — `FindOrCreateSection` tak backfill Nama section existing
**`AssessmentAdminController.cs:7203-7209`**. Guard `newSections.Contains(existing)` selalu false untuk row DB → re-import tak rename section lama. Komentar kode bilang **disengaja** ("section SUDAH ada keep Nama existing"). → design smell/UX-surprise. **Konfirmasi: apakah re-label via Excel memang sengaja no-op?** CONFIRMED (mekanisme), by-design (intent).

### L2 — `CreateSection`/`EditSection` race → 500
**`AssessmentAdminController.cs:6275/6312`**. TOCTOU `AnyAsync` lalu `SaveChangesAsync` tanpa try/catch di sekitar unique index `(AssessmentPackageId, SectionNumber)`. Dua submit barengan → `DbUpdateException` → 500 (bukan pesan ramah). Komentar nyebut "Phase 404 lesson" tapi cuma pre-check. **Fix:** try/catch `DbUpdateException` → friendly TempData. CONFIRMED.

### L3 — Cabang legacy (all-null) count pakai stop-at-first — BARU pass-2
**`AssessmentAdminController.cs:7147`**. Cabang legacy banding cuma vs `siblingPackagesWithQuestions.First()`; cabang Section-aware banding SEMUA sibling. Asimetri → import legacy lolos walau sibling ke-3 beda count. Butuh pre-existing inconsistency. CONFIRMED, LOW.

### L4 — Tak ada validasi sibling-vs-sibling — BARU pass-2 (PLAUSIBLE)
**`AssessmentAdminController.cs:7159`**. Banding cuma incoming-vs-tiap-sibling, tak pernah sibling-vs-sibling. Drift antar-2-sibling existing tak ter-surface; grup tak bisa diperbaiki via import (import benar pun ke-tolak vs sibling drift). Ada runtime safety-net (StartExam guard). PLAUSIBLE, LOW.

### L5 — View re-derive grouping section (O(sections×questions)) — BARU pass-2 (simplification)
**`Views/Admin/ManagePackageQuestions.cshtml:~265`**. `questions.Where(q => q.SectionId == s.Id)` per-section + pass ungrouped terpisah — salinan ke-3 logika "group by section, Lainnya last" (selain controller + import guard). Bisa drift dari controller; quadratic. **Fix:** shape view-model di controller. CONFIRMED, LOW.

### L6 — Format label Section tak konsisten antar elemen UI — BARU pass-2 (conventions)
**`ManagePackageQuestions.cshtml`** — header grouped-list `:268` (`"Section 1 — Pompa"`) vs dropdown assign `:328` (`"1. Pompa"`) vs tabel CRUD `:126` (`"1"`). Section sama tampil 3 format di 1 halaman. Kosmetik. **Fix:** pakai `sectionLabel(s)` helper konsisten. CONFIRMED, LOW.

---

## Rekomendasi urutan fix

1. **H1** (reuse `SiblingPrePostAwarePredicate` di guard + import) — sekalian **M4** (ekstrak `CompareSectionStructure`).
2. **H4** (sync di import + 4 Section CRUD) — blocking untuk 416/417.
3. **H2** (assert correct-letter ↔ opsi non-kosong) + **M1/M3** (perbaiki baseline count: dedup + soal existing).
4. **H3** (edit A–F: controller+view+JS) — minimal guard anti-corrupt sekarang, full UI di 418.
5. **M2** (validasi header name). **L2** (catch race).
6. Sisanya (L1 konfirmasi by-design, L3/L4/L5/L6) → backlog/polish.

**Catatan gate:** H1–H4 + M1–M3 blocking sebelum lanjut Phase 416 & real-browser UAT. M5/L1/L4 perlu keputusan desain (bukan auto-fix).
