# Phase 372: Data Foundation + Propagasi Toggle - Context

**Gathered:** 2026-06-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Pondasi data untuk fitur Shuffle Toggle (Acak Soal & Acak Pilihan). Phase ini HANYA:

1. **SHUF-01** — Tambah 2 kolom `ShuffleQuestions` + `ShuffleOptions` (bool) di entity `AssessmentSession` + migration `AddShuffleTogglesToAssessmentSession` (`bit NOT NULL DEFAULT 1` → baris LAMA otomatis ON dua-duanya).
2. **SHUF-02** — Form `CreateAssessment` set kedua flag eksplisit dari form (default checked) di SEMUA loop create session (standard / Pre / Post) — hindari EF bool-false trap; + toggle di wizard `CreateAssessment.cshtml` Langkah 3.
3. **SHUF-03** — Ubah toggle di `EditAssessment` POST propagate ke SEMUA sibling grup (pola `foreach` existing).

**BUKAN bagian phase ini (downstream):**
- Engine baca shuffle di `StartExam` / reshuffle / round-robin index-stabil → **Phase 373**.
- UI toggle di ManagePackages + endpoint `UpdateShuffleSettings` + lock + warning + reminder Pre/Post + hide Proton/Manual → **Phase 374**.
- Test xUnit + Playwright UAT → **Phase 375**.

</domain>

<decisions>
## Implementation Decisions

### Wizard Toggle (Langkah 3 CreateAssessment)
- **D-01:** Penempatan = **Grup B "Pengaturan Ujian"** di Langkah 3, sejajar `IsTokenRequired`. Pakai pola `form-check form-switch` (lihat `CreateAssessment.cshtml:505-508`). TANPA card/grup baru — konsisten.
- **D-02:** Label + **penjelasan detail** (form-text edukatif buat HC non-teknis), bukan label saja. Contoh arah:
  - "Acak Soal" → jelaskan: urutan & pemilihan soal diacak berbeda per peserta (kalau OFF, semua peserta dapat urutan soal sama).
  - "Acak Pilihan Jawaban" → jelaskan: urutan opsi A/B/C/D diacak per soal per peserta (kalau OFF, urutan opsi ikut urutan database / sama untuk semua).
  - Copy final = diskresi Claude saat planning, tapi WAJIB cukup detail agar HC paham efek ON vs OFF.
- **D-03:** Default kedua toggle = **checked (ON)** di wizard (sejalan default data baru di spec §2).

### Cakupan Pre/Post saat Create
- **D-04:** Saat create Pre-Post Test, **1 pasang toggle** di Langkah 3 → nilai sama di-set ke loop Pre DAN loop Post. Ikut pola field assessment-level lain (PassPercentage dsb). Divergensi Pre≠Post terjadi BELAKANGAN di ManagePackages (Phase 374), BUKAN di wizard. Tidak ada toggle terpisah Pre/Post di wizard.

### Langkah 4 Konfirmasi
- **D-05:** Status ON/OFF kedua toggle **DITAMPILKAN** di summary Langkah 4 (bareng Status, Pass%, Token). HC bisa review sebelum submit.

### Locked dari Spec (tidak dibahas ulang — sumber: `2026-06-13-shuffle-toggle-design.md`)
- **D-06:** Default dua-duanya ON via `defaultValue:true` — janji "data lama tak berubah".
- **D-07:** Kolom `bit NOT NULL DEFAULT 1` untuk kedua kolom; nama migration `AddShuffleTogglesToAssessmentSession`.
- **D-08:** EF bool trap — `bool` default C# = `false`; migration defaultValue cuma benerin baris LAMA. Form WAJIB set eksplisit di SEMUA loop create, kalau tidak assessment BARU malah OFF.
- **D-09:** Propagasi sibling ikut pola `foreach` `EditAssessment` POST (`AssessmentAdminController.cs:2007-2031`) — field assessment-level disimpan di SETIAP baris sibling.
- **D-10:** Acak Soal & Acak Pilihan = **independen** (boleh beda).
- **D-11:** Grading aman — pakai `PackageOption.Id` (bukan posisi huruf); shuffle tidak pernah pengaruh nilai (spec §13). Phase 372 cuma nambah kolom, tak sentuh grading.

### Claude's Discretion
- Display attribute / property naming exact di entity (`[Display(Name=...)]` per spec §4).
- Copy/teks final penjelasan toggle (selama cukup detail per D-02).
- Nama field form / binding di view ↔ model (`name="ShuffleQuestions"` dll).
- Format visual summary Langkah 4 (badge/teks).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec utama (SEMUA keputusan terkunci)
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` — design spec lengkap. Relevan Phase 372: §2 (keputusan locked), §4 (data model + EF bool trap WAJIB), §5 (propagasi create + EditAssessment), §13 (grading safety). §6/§7/§8 = scope 373/374 (referensi forward only).

### Requirements
- `.planning/REQUIREMENTS.md:59-61` — SHUF-01, SHUF-02, SHUF-03 (acceptance kriteria Phase 372).

### Roadmap
- `.planning/ROADMAP.md:95` — deskripsi Phase 372 + line-ref loop create (standard ~1424, Pre ~1216, Post ~1250).
- `.planning/ROADMAP.md:89-114` — blok v27.0 + ⚠️ catatan koordinasi file-overlap v25.0 (WAJIB baca sebelum execute).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola `form-check form-switch`** — `Views/Admin/CreateAssessment.cshtml:505-508` (`IsTokenRequired`). Toggle shuffle reuse pola ini di Grup B "Pengaturan Ujian" (D-01).
- **Pola propagasi sibling** — `AssessmentAdminController.cs:2007-2031` (`EditAssessment` POST `foreach` siblings set field assessment-level). SHUF-03 ikut pola ini.

### Established Patterns
- **Field assessment-level disimpan di SETIAP baris sibling** (bukan satu representative). PassPercentage/AllowAnswerReview/dll di-set di loop create + di-propagate di EditAssessment. Kolom shuffle ikut pola sama.
- **Wizard 4-langkah** `CreateAssessment.cshtml`: Langkah 1 Kategori&Judul, 2 Peserta, 3 Settings (Grup A Jadwal&Waktu, Grup B Pengaturan Ujian), 4 Konfirmasi. Toggle masuk Langkah 3 Grup B (D-01); summary masuk Langkah 4 (D-05).
- **AssessmentSession = per-peserta**; satu "assessment" logis = grup sibling key `(Title, Category, Schedule.Date)`. Pre & Post = grup terpisah (Schedule beda).

### Integration Points
- Entity `AssessmentSession` — tambah 2 properti bool (spec §4).
- Migration baru `AddShuffleTogglesToAssessmentSession`.
- `AssessmentAdminController.cs` — 3 loop create (standard/Pre/Post) set flag + `EditAssessment` POST `foreach` propagate.
- `Views/Admin/CreateAssessment.cshtml` — Langkah 3 Grup B (input toggle) + Langkah 4 (summary).

### ⚠️ Constraint Koordinasi (WAJIB buat planner/executor)
- **File-overlap v25.0 AKTIF:** `AssessmentAdminController.cs` dipakai Phase 367/368 (sedang/akan dieksekusi sesi lain). JANGAN `/gsd-execute-phase 372` sebelum 367/368 ship atau merge dikoordinasi — hindari konflik lintas-sesi.
- STATE.md sengaja pinned `v25.0` (roadmap v27.0 append-only). Jangan `/gsd-new-milestone` / `/gsd-complete-milestone` vanilla (clobber STATE/phases v25.0).
- Sequential strict v27.0: 372 → 373 → 374 → 375.

</code_context>

<specifics>
## Specific Ideas

- Penjelasan toggle harus benar-benar mendidik HC (D-02) — bukan jargon. HC mungkin tak paham istilah "Acak Soal" vs "Acak Pilihan"; teks harus jelaskan efek nyata ON vs OFF dengan bahasa awam.
- 1 migration di phase ini → notifikasi IT WAJIB sertakan flag migration + commit hash (per DEV_WORKFLOW). Bundle dengan carry-over IT yang ada.

</specifics>

<deferred>
## Deferred Ideas

- **Toggle terpisah Pre vs Post di wizard** — ditolak (D-04). Divergensi Pre≠Post dilakukan di ManagePackages Phase 374, bukan di wizard create.
- Semua logic baca/reshuffle/UI ManagePackages/lock/warning/reminder = scope Phase 373/374/375 (bukan deferred, tapi out-of-scope phase ini).

None lain — diskusi tetap dalam scope phase.

</deferred>

---

*Phase: 372-data-foundation-propagasi-toggle*
*Context gathered: 2026-06-13*
