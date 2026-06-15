# Phase 373: Shuffle Engine (read logic + reshuffle) - Context

**Gathered:** 2026-06-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Engine baca shuffle (read-logic) untuk fitur Shuffle Toggle v27.0. Phase ini HANYA:

1. **Gerbang flag di `StartExam`** — saat bangun `UserPackageAssignment` (lazy, peserta pertama buka ujian), baca `assessment.ShuffleQuestions`/`ShuffleOptions` (dari session peserta sendiri; sudah ter-propagate Phase 372) dan bangun urutan soal + opsi sesuai flag.
2. **Ekstrak core pure** (testable tanpa DB) — satu engine bersama untuk distribusi soal + option-shuffle, dipakai StartExam + reshuffle.
3. **Reshuffle hormati kedua flag** — `ReshufflePackage` + `ReshuffleAll` rebuild assignment sesuai `ShuffleQuestions` DAN `ShuffleOptions` (fix bug existing opsi hard-code `"{}"`).
4. **Cleanup** komentar stale `CMPController.cs:1054`.

REQ: SHUF-04, SHUF-05, SHUF-06, SHUF-07, SHUF-08, SHUF-09, SHUF-15. Migration: false. UI hint: no.

**BUKAN bagian phase ini (downstream):**
- UI toggle di ManagePackages + endpoint `UpdateShuffleSettings` + lock + **UI warning §9** + reminder Pre/Post + hide Proton/Manual → **Phase 374**.
- xUnit mode-matrix penuh + Playwright UAT → **Phase 375**.
- Memindah setting `SamePackage` → out of scope (spec §12).

</domain>

<decisions>
## Implementation Decisions

### Core Extraction & Dedup
- **D-01:** Ekstrak **SATU shared pure core** (mis. static `ShuffleEngine` / helper — lokasi & nama exact = diskresi planner, selama pure & testable tanpa DB) berisi: (a) distribusi soal per flag `ShuffleQuestions`, dan (b) option-shuffle per flag `ShuffleOptions`. Dipakai oleh `StartExam` (CMPController) + `ReshufflePackage` + `ReshuffleAll` (AssessmentAdminController). **HAPUS duplikasi** `BuildCrossPackageAssignment` yang kini ada VERBATIM di `CMPController.cs:1230` DAN `AssessmentAdminController.cs:5250` — keduanya delegasi ke core baru. (Pola extract-static-core project; menjamin fix bug reshuffle konsisten dgn StartExam.)
- **D-01a:** Jalur **ON WAJIB dipertahankan verbatim** (perilaku existing tak berubah — SC#1): 1 paket → Fisher-Yates `q.Order`; ≥2 paket → sampling `K=min` ET-aware (Phase 1 satu-per-ElemenTeknis + Phase 2 fill) existing. Core terima `Random` param (sudah begitu) → ON deterministik-testable via seed; OFF tak butuh rng.

### Determinisme OFF + ≥2 paket
- **D-02:** Worker→paket = **"filter dulu, baru modulo"**: bangun daftar **paket-ber-soal** (`Questions.Count > 0`, urut `OrderBy(PackageNumber)`); index worker = posisi sibling session `OrderBy(s => s.Id)`; paket = `daftarBerSoal[index % daftarBerSoal.Count]`. Isi paket urut `q.Order` (tak diacak).
- **D-02a:** Peserta baru ditambah saat sebagian sudah mulai → `Id` lebih besar → di-append di akhir urutan → **tidak menggeser** assignment worker lama (determinisme terjaga, tahan resume/reshuffle).
- **D-02b:** **Guard paket kosong** = paket `Questions.Count == 0` di-exclude SEBELUM modulo (masuk pembangunan `daftarBerSoal`), bukan di-skip setelah modulo.
- **D-02c:** **JANGAN** pakai "urutan buka" / `assignmentCount % n` — cacat: bergeser saat reshuffle/resume → false-error "soal berubah".

### Resume stale-count guard (spec §6.3)
- **D-03:** Untuk OFF multi-paket, `currentQuestionCount` = jumlah soal paket worker itu. Karena rekomputasi deterministik (D-02), guard `SavedQuestionCount` (StartExam ~`:1027`) **tidak** salah-trigger. Pertahankan mekanisme guard existing apa adanya.

### Reshuffle (M2 — hormati flag)
- **D-04:** `ReshufflePackage` (`AssessmentAdminController.cs:5065`) + `ReshuffleAll` (`:5146`) rebuild assignment via core baru, hormati **KEDUA** flag:
  - `ShuffleQuestions OFF` → rebuild **deterministik** (paket & urutan sama; tak ada perubahan urutan soal terlihat — **by design, idempotent**).
  - `ShuffleOptions ON` → bangun `optionShuffleDict` (**FIX bug** hard-code `"{}"` di `:5119` & `:5213`); OFF → `"{}"`.
- **D-04a:** **Pertahankan guard existing** "hanya Not started/Abandoned" (`ReshufflePackage:5083`, `ReshuffleAll:5191`) — sejalan konsep lock. Tidak dilonggarkan ke InProgress.

### Jumlah soal OFF + ≥2 paket
- **D-05:** Tiap worker dapat paket **UTUH** (seluruh soal, urut `q.Order`), **TIDAK** dipotong ke K-min. Konsekuensi: jumlah soal & nilai maks bisa beda antar worker bila ukuran paket beda — ditutupi **warning non-blocking §9** (UI = Phase 374, BUKAN 373). Sengaja beda dari mode ON ≥2 (yang sampling K-min). Pass% relatif → kelulusan tetap sebanding.

### Acak Pilihan (locked — spec §6.2 + D-10 carried)
- **D-06:** Independen penuh dari Acak Soal. ON → `optionShuffleDict` per soal (Fisher-Yates opsi, simpan ke `ShuffledOptionIdsPerQuestion`). OFF → `"{}"` → view fallback urutan DB (`StartExam.cshtml` via `ViewBag.OptionShuffle`, `CMPController.cs:1145-1157`). Berlaku di SEMUA mode acak soal (boleh ON walau Acak Soal OFF).
- **D-06a (grading safety, D-11 carried):** Grading pakai `PackageOption.Id` (bukan posisi huruf) → acak opsi tak pernah pengaruh nilai. `GetShuffledQuestionIds()` tetap dipakai grading di KEDUA mode (assignment selalu simpan daftar ID, ON maupun OFF) → jalur grading tak berubah.

### Cleanup (spec §10)
- **D-07:** Perbaiki komentar stale `CMPController.cs:1054` ("option shuffle removed per user decision"). Realita: opsi AKTIF (dicabut `d777d6b9`, dihidupkan lagi `e6ddffd6` via `ViewBag.OptionShuffle`); kini digerbang flag `ShuffleOptions`. Komentar harus cerminkan ini.

### Claude's Discretion
- Lokasi & nama exact shared core (`Services/ShuffleEngine.cs` static class vs helper) + signature method (input `List<AssessmentPackage>` atau DTO ringkas + flag + worker index + `Random`).
- Bentuk DTO/abstraksi input core agar pure (tanpa EF query di dalam core).
- **Pembagian test:** core HARUS pure agar 375 bisa uji mode-matrix. Apakah sebagian unit test core ditulis di 373 (Wave 0 self-check ekstraksi) atau ditahan penuh ke Phase 375 = keputusan planner (lihat Nyquist/validate). Test penuh semua mode + UAT = scope 375.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec utama (SEMUA keputusan engine terkunci)
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` — relevan Phase 373: **§6** (logika baca shuffle — 6.1 Acak Soal ON/OFF, 6.2 Acak Pilihan, 6.3 resume guard), **§8** (reshuffle hormati flag + bug opsi), **§10** (cleanup komentar), **§11** (ekstrak core pure + daftar test), **§13** (grading safety). §7/§9-UI = forward ref ke Phase 374.

### Requirements
- `.planning/REQUIREMENTS.md` — SHUF-04..SHUF-09, SHUF-15 (acceptance Phase 373).

### Roadmap
- `.planning/ROADMAP.md:132-143` — deskripsi Phase 373 + 5 Success Criteria.
- `.planning/ROADMAP.md:114` — Coverage v27.0 + ⚠️ koordinasi file-overlap v25.0 (WAJIB sebelum execute).

### Phase sebelumnya
- `.planning/phases/372-data-foundation-propagasi-toggle/372-CONTEXT.md` — D-10 (independen) + D-11 (grading safety) carried; kolom `ShuffleQuestions`/`ShuffleOptions` sudah live + ter-propagate ke sibling.

### Kode existing (verified this session — re-grep line di execute-time, bisa drift)
- `Controllers/CMPController.cs` — `StartExam` (`:860`), build assignment region (`:947-1069`: sibling lookup `:949`, packages OrderBy(PackageNumber) `:956-961`, build ON `:973-1003`, stale-count guard `:1027`, opsi base DB-order `:1054`, ViewBag.OptionShuffle `:1145-1157`), `BuildCrossPackageAssignment` (`:1230`, ET-aware).
- `Controllers/AssessmentAdminController.cs` — `ReshufflePackage` (`:5065`, guard `:5083`, hard-code `"{}"` `:5119`), `ReshuffleAll` (`:5146`, guard `:5191`, hard-code `"{}"` `:5213`), `Shuffle<T>` helper (`:5241`), `BuildCrossPackageAssignment` **DUPLIKAT** (`:5250`).
- `Models/UserPackageAssignment.cs` — `ShuffledQuestionIds`, `ShuffledOptionIdsPerQuestion`, `SavedQuestionCount`, `GetShuffledQuestionIds()`.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`BuildCrossPackageAssignment(List<AssessmentPackage>, Random)`** — sudah pure-ish (static, terima rng). Jadi basis core ON-path; harus dipindah ke shared core + dipanggil 3 call-site (StartExam + 2 reshuffle). Kini DUPLIKAT 2 tempat verbatim.
- **`Shuffle<T>(List<T>, Random)`** — Fisher-Yates helper (CMPController + AssessmentAdminController). Pindah/share di core.
- **`UserPackageAssignment.GetShuffledQuestionIds()`** — dipakai build VM + grading; tak berubah.

### Established Patterns
- **Assignment dibangun lazy** di `StartExam` saat peserta pertama buka (idempotent resume via `FirstOrDefaultAsync`). Race-guard `catch(DbUpdateException)` reload existing.
- **Packages selalu `OrderBy(p => p.PackageNumber)`** di SEMUA query (StartExam `:960`, reshuffle `:5097/:5163`) → urutan paket stabil & human-meaningful (anchor D-02).
- **Option-shuffle via ViewBag** — base opsi DB-order di VM (`:1054`), view reorder per `ViewBag.OptionShuffle` (dict dari assignment). Grading by `PackageOption.Id`.
- **Reshuffle guard** "Not started/Abandoned only" + audit log + `Random.Shared`.

### Integration Points
- `CMPController.StartExam` build-branch (`:973-1003`) — gerbang ON/OFF + panggil core.
- `AssessmentAdminController.ReshufflePackage`/`ReshuffleAll` — panggil core + isi `ShuffledOptionIdsPerQuestion` per flag (bukan `"{}"`).
- Shared core baru (file baru) — host `BuildCrossPackageAssignment` (ON existing) + OFF logic + option-shuffle.

### ⚠️ Constraint Koordinasi (WAJIB buat planner/executor)
- **File-overlap v25.0:** Phase 373 sentuh `CMPController.cs` DAN `AssessmentAdminController.cs` — keduanya area sibuk v25.0 (367/368). JANGAN `/gsd-execute-phase 373` sebelum konflik lintas-sesi clear / koordinasi merge. **Re-grep semua line number di execute-time** (367/368 bisa geser baris).
- STATE.md sengaja pinned `v25.0` (roadmap v27.0 append-only). Phase dir `373-shuffle-engine-read-logic-reshuffle` dibuat manual (init phase-op tak lihat v27.0 saat STATE v25.0 — getRoadmapPhaseInternal milestone-scoped, tanpa fallback full-roadmap). JANGAN `/gsd-new-milestone`/`/gsd-complete-milestone` vanilla.
- Sequential strict v27.0: 372 ✅ → **373** → 374 → 375.

</code_context>

<specifics>
## Specific Ideas

- **Determinisme = jantung phase.** Anti-pattern "urutan buka"/`assignmentCount % n` (D-02c) adalah jebakan utama — bikin worker dapat paket beda saat reshuffle/resume → guard `SavedQuestionCount` false-trigger "soal berubah". Anchor index-session-stabil (`OrderBy(Id)`) + filter-paket-kosong-sebelum-modulo wajib.
- **Bug opsi reshuffle** (`"{}"` di `:5119`/`:5213`) = bug existing nyata, bukan cuma fitur baru: peserta yang di-reshuffle dapat opsi TAK teracak walau jalur normal mengacak. Fix masuk scope 373.
- **Dedup wajib** demi korektnes: kalau core tidak disatukan, fix flag harus diterapkan 2x di `BuildCrossPackageAssignment` yang kembar → risiko drift.

</specifics>

<deferred>
## Deferred Ideas

- **UI warning §9** (jumlah soal antar paket beda) — render/tampilan = Phase 374. Phase 373 hanya hasilkan kondisi datanya (paket utuh beda ukuran); tak render warning.
- UI ManagePackages toggle + endpoint `UpdateShuffleSettings` + lock + reminder Pre/Post + hide Proton-Tahun3/Manual → Phase 374.
- xUnit mode-matrix penuh + Playwright UAT → Phase 375.
- Memindah setting `SamePackage` ke halaman package → out of scope (spec §12).

None lain — diskusi tetap dalam scope phase.

</deferred>

---

*Phase: 373-shuffle-engine-read-logic-reshuffle*
*Context gathered: 2026-06-13*
