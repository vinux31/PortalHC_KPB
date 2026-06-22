---
phase: 420-form-create-edit-persistensi-field-ux-pre-post
plan: 01
subsystem: api
tags: [aspnet-mvc, razor, ef-core, asp-for, form-binding, assessment, persistence, retake, shuffle]

# Dependency graph
requires:
  - phase: 405-attempt-retake-backend
    provides: "Kolom AssessmentSession.AllowRetake/MaxAttempts/RetakeCooldownHours + RetakeServiceFixture (real-SQL)"
  - phase: 372-shuffle-toggle
    provides: "ShuffleQuestions/ShuffleOptions + idiom asp-for switch CreateAssessment.cshtml:536-551"
provides:
  - "Persistensi penuh field form Create/Edit: shuffle (render Edit), retake×3 (3 jalur Create + Edit std loop), ValidUntil (Edit std loop)"
  - "FormPersistence420Tests.cs — 5 test real-SQL invariant persistensi (template untuk fase grading/retake berikutnya)"
affects: [421-retake-hardening, 423-certificate-issuance, 424-grading-gating, 420-02-guard-redirect, 420-03-ux-prepost]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Penyalinan eksplisit field config (anti silent EF-default) di SETIAP object-init + sibling-loop"
    - "Math.Clamp server-authoritative (V5) di setiap tulis MaxAttempts[1,5]/RetakeCooldownHours[0,168]"
    - "asp-for render = field yang ditulis controller WAJIB dirender (hindari silent reset E-01)"

key-files:
  created:
    - "HcPortal.Tests/FormPersistence420Tests.cs"
  modified:
    - "Views/Admin/EditAssessment.cshtml"
    - "Controllers/AssessmentAdminController.cs"

key-decisions:
  - "Pre baseline (D-03): AllowRetake=false EKSPLISIT di Create Pre walau model true; MaxAttempts/RetakeCooldownHours tetap disalin untuk konsistensi grup"
  - "Shuffle dirender SEKALI di Group settings shared EditAssessment (A1 verified: Pre-Post hanya beda tab jadwal :213-380, Group 'Pengaturan Assessment' :384 di luar conditional)"
  - "ValidUntil pakai DateOnly? (bukan DateTime? seperti tertulis di RESEARCH interface) — TZ-01 v19.0 refactor"

patterns-established:
  - "Pattern: field editable yang dirender WAJIB punya jalur tulis (FORM-03), field yang ditulis WAJIB dirender (FORM-01)"
  - "Pattern: persistence test = replika SHAPE object-init/sibling-loop controller VERBATIM atas real-SQL (bukan WebApplicationFactory)"

requirements-completed: [FORM-01, FORM-02, FORM-03, FORM-04]

# Metrics
duration: 9min
completed: 2026-06-22
---

# Phase 420 Plan 01: Form Create/Edit — Persistensi Field Summary

**Tutup pola "field dirender/ditulis tapi tak konsisten": render shuffle di EditAssessment (akar E-01), salin retake eksplisit di 3 jalur Create + Edit std loop, dan tulis ValidUntil di Edit std loop — tanpa mengubah perilaku mode Standard.**

## Performance

- **Duration:** 9 min
- **Started:** 2026-06-22T13:38:49Z
- **Completed:** 2026-06-22T13:48:27Z
- **Tasks:** 3
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- **FORM-01:** Render dua toggle `asp-for="ShuffleQuestions"`/`asp-for="ShuffleOptions"` di EditAssessment.cshtml (akar bug E-01: view tak merender shuffle → POST bind `false` → reset OFF tiap Edit). Write controller sudah benar; ini murni RENDER. State terisi dari `Model` (entity tersimpan).
- **FORM-02:** Penyalinan retake eksplisit di TIGA jalur build Create (Pitfall #1 checklist tuntas): std penuh, Pre `AllowRetake=false` baseline (D-03) + retake disalin grup, Post penuh — semua dengan `Math.Clamp`.
- **FORM-03:** Edit std loop kini menulis `AllowRetake/MaxAttempts/RetakeCooldownHours` ke semua sibling (sebelumnya no-op; satu-satunya writer = `UpdateRetakeSettings`).
- **FORM-04:** Edit std loop kini menulis `sibling.ValidUntil` (sebelumnya absent → tak tersimpan jalur std).
- **Test:** FormPersistence420Tests.cs (5 Fact real-SQL) — 5/5 GREEN @SQLEXPRESS membuktikan invariant persistensi (clamp upper/lower, Pre baseline OFF, sibling propagation, shuffle write benar).

## Task Commits

Each task was committed atomically:

1. **Task 0: Wave-0 persistence test stubs (real-SQL)** - `8bda9e5f` (test)
2. **Task 1: FORM-01 render shuffle Edit + FORM-04/03 ValidUntil+retake Edit std loop** - `d72b304a` (feat)
3. **Task 2: FORM-02 retake eksplisit 3 jalur build Create** - `9ec4d657` (feat)

**Plan metadata:** (final docs commit below)

## Files Created/Modified
- `HcPortal.Tests/FormPersistence420Tests.cs` (created) - 5 test real-SQL: Create std/Pre/Post retake persist + clamp, Edit std loop ValidUntil+retake ke semua sibling, FORM-01 invariant shuffle write.
- `Views/Admin/EditAssessment.cshtml` (modified) - Sisip blok "Pengacakan Soal & Jawaban" (2 form-switch asp-for) di Group "Pengaturan Assessment", berdampingan kartu Ujian Ulang. Copy idiom + copy text dari CreateAssessment.cshtml:536-551 (gaya label `fw-semibold` ikut idiom Edit).
- `Controllers/AssessmentAdminController.cs` (modified) - 4 lokasi penyalinan eksplisit: Create std (:1467) + Pre (:1243, AllowRetake=false) + Post (:1279) retake×3 dengan clamp; Edit std loop (:2072) +ValidUntil +retake×3 dengan clamp.

## Decisions Made
- **Pre baseline = retake OFF eksplisit (D-03):** Create Pre set `AllowRetake = false` literal (bukan dari model) karena Pre = baseline murni tanpa lulus/gagal/retake. MaxAttempts/RetakeCooldownHours tetap disalin (clamp) untuk konsistensi grup — perilaku OFF tapi nilai konsisten dengan Post. Test 2 membuktikan `AllowRetake==false` walau model `true`.
- **Render shuffle SEKALI (A1 verified):** Group "Pengaturan Assessment" EditAssessment (`:384`) berada DI LUAR conditional `@if (isPrePostGroup)` (`:213`, yang hanya membungkus tab jadwal Pre/Post `:230-380`). Jadi render shuffle satu kali di Group ini menjangkau single-mode DAN Pre-Post. Tidak perlu render dua kali.
- **Math.Clamp di setiap tulis retake (V5 / T-420-01):** Defense-in-depth — `[Range]` model = lapisan kedua, clamp server = lapisan terakhir terhadap raw POST outlier. Diterapkan di 4 lokasi tulis (3 Create + Edit std loop) + di test (Test 3 outlier 9→5, 999→168; Test 4 9→5, 200→168).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `ValidUntil` bertipe `DateOnly?`, bukan `DateTime?`**
- **Found during:** Task 0 (penulisan FormPersistence420Tests.cs)
- **Issue:** RESEARCH `<interfaces>` mencantumkan `public DateTime? ValidUntil` (:84), tetapi tipe aktual di `Models/AssessmentSession.cs:84` = `DateOnly?` (akibat TZ-01 v19.0 DateOnly refactor). Test gagal compile (CS0029/CS1503) saat memakai `new DateTime(2027,1,31)` untuk `model.ValidUntil` + assertion.
- **Fix:** Ganti `new DateTime(2027,1,31)` → `new DateOnly(2027,1,31)` di assignment model + assertion Test 4. Sisi controller TIDAK terdampak (`sibling.ValidUntil = model.ValidUntil` type-aman — kedua sisi DateOnly?).
- **Files modified:** HcPortal.Tests/FormPersistence420Tests.cs
- **Verification:** `dotnet build` 0 error; FormPersistence420 5/5 GREEN @SQLEXPRESS.
- **Committed in:** `8bda9e5f` (Task 0 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — type mismatch RESEARCH vs source aktual)
**Impact on plan:** Koreksi tipe minor di test; tidak mengubah scope atau perilaku produksi. Controller-side ValidUntil sudah type-aman (model VM = AssessmentSession entity, DateOnly? konsisten). No scope creep.

## Issues Encountered
None - selain deviasi tipe ValidUntil (di atas), semua task berjalan sesuai plan. Build hijau di tiap task; non-integration suite 448/0/2 stabil (no regresi baseline v32.4); integration FormPersistence420 5/5 GREEN.

## Backward-Compat Verification (mode Standard)
- Edit std loop: HANYA menambah 4 baris (ValidUntil + retake×3); field existing (Title/Category/Schedule/Status/Shuffle/GenerateCertificate/ExamWindowCloseDate dll) TIDAK diubah/dihapus.
- Create std object-init: HANYA menambah 3 baris retake setelah ShuffleOptions; field existing utuh.
- Shuffle render Edit = field BARU yang seharusnya ada (akar E-01); `asp-for` emit hidden fallback `value="false"` → unchecked bind benar (bukan data-loss). Payload Standard existing tidak berubah selain field shuffle yang kini hadir (memang seharusnya ada).
- `[Authorize(Roles="Admin, HC")]` (:1806) + `[ValidateAntiForgeryToken]` (:1807) DIPERTAHANKAN (tidak disentuh).

## User Setup Required
None - no external service configuration required. migration=FALSE (tidak ada perubahan schema).

## Next Phase Readiness
- **Plan 420-02** (guard lock Completed FORM-05 + redirect manual FORM-06) siap — Edit std loop yang baru di-hardening tidak berbenturan dengan guard yang akan diangkat sebelum cabang Pre-Post.
- **Plan 420-03** (UX Pre-Post FORM-07..11 + rename FORM-10) siap — render shuffle Edit sudah terpisah dari redesign Create.
- **Catatan untuk verifier/e2e (Plan 03):** FORM-01 lifecycle penuh (create shuffle ON → Edit save → reopen → masih ON) ditegaskan via Playwright di Plan 03; di sini data-level + render sudah ditegaskan (xUnit Test 5 + grep asp-for).
- migration=FALSE → notify IT saat bundle deploy dengan flag migration=FALSE untuk plan ini.

## Self-Check: PASSED

- Files: FOUND HcPortal.Tests/FormPersistence420Tests.cs, Views/Admin/EditAssessment.cshtml, Controllers/AssessmentAdminController.cs, 420-01-SUMMARY.md
- Commits: FOUND 8bda9e5f (test), d72b304a (feat), 9ec4d657 (feat)
- Build: 0 error; non-integration suite 448/0/2; FormPersistence420 5/5 GREEN @SQLEXPRESS

---
*Phase: 420-form-create-edit-persistensi-field-ux-pre-post*
*Completed: 2026-06-22*
