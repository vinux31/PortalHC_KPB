---
phase: 296-data-foundation-gradingservice-extraction
plan: "02"
subsystem: api
tags: [grading, assessment, service-extraction, ef-core, dependency-injection]

# Dependency graph
requires:
  - phase: 291-controller-split
    provides: "AssessmentAdminController dan CMPController sebagai terpisah, siap di-refactor"
provides:
  - "GradingService concrete class dengan GradeAndCompleteAsync (7 langkah)"
  - "QuestionType nullable string field di PackageQuestion model"
  - "DI registration GradingService di Program.cs"
affects:
  - 296-03
  - 297
  - 298-multitype-assessment

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Concrete class service tanpa interface (pola AuditLogService)"
    - "GradeAndCompleteAsync returns bool — true=sukses, false=race condition skip"
    - "ExecuteUpdateAsync + WHERE Status != 'Completed' sebagai race condition guard"
    - "switch-case per QuestionType dengan NotImplementedException untuk tipe future"

key-files:
  created:
    - Services/GradingService.cs
  modified:
    - Models/AssessmentPackage.cs
    - Program.cs

key-decisions:
  - "GradingService sebagai concrete class + AddScoped (D-01) — konsisten dengan AuditLogService"
  - "GradeAndCompleteAsync grade dari DB (bukan form POST) — per RESEARCH.md anti-pattern"
  - "SignalR push dan _cache.Remove tetap di controller — bukan tanggung jawab GradingService"
  - "QuestionType nullable string di PackageQuestion — null default ke MultipleChoice (backward compatible)"
  - "NotImplementedException untuk MultipleAnswer dan Essay — diimplementasi Phase 298"

patterns-established:
  - "GradingService: inject di controller, panggil GradeAndCompleteAsync(session), cek return bool untuk race"
  - "QuestionType switch default fallback ke MultipleChoice untuk backward compatibility"

requirements-completed: [FOUND-01, FOUND-06]

# Metrics
duration: 15min
completed: 2026-04-06
---

# Phase 296 Plan 02: GradingService Extraction Summary

**GradingService concrete class dengan GradeAndCompleteAsync 7-langkah diekstrak dari dua controller, QuestionType ditambah ke PackageQuestion, dan DI registration siap di Program.cs**

## Performance

- **Duration:** ~15 menit
- **Started:** 2026-04-06T07:40:00Z
- **Completed:** 2026-04-06T07:55:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- GradingService.cs dibuat dengan GradeAndCompleteAsync yang menggabungkan logika dari AssessmentAdminController.GradeFromSavedAnswers() dan CMPController.SubmitExam()
- Switch-case per QuestionType per D-08: MultipleChoice implemented, MultipleAnswer+Essay throw NotImplementedException untuk Phase 298
- Race condition handling via ExecuteUpdateAsync + WHERE Status != "Completed" (T-296-02)
- NomorSertifikat retry loop 3x + WHERE NomorSertifikat == null (T-296-03)
- TrainingRecord duplicate guard via AnyAsync sebelum insert (T-296-04)
- DI registration builder.Services.AddScoped<HcPortal.Services.GradingService>() di Program.cs
- Build sukses 0 errors, 70 warnings (warnings pre-existing, bukan dari perubahan ini)

## Task Commits

1. **Task 1: Buat Services/GradingService.cs** - `6044e3d4` (feat)
2. **Task 2: Register GradingService di DI** - `dc6e3543` (feat)

## Files Created/Modified
- `Services/GradingService.cs` - Concrete class GradingService dengan GradeAndCompleteAsync 7-langkah
- `Models/AssessmentPackage.cs` - Tambah QuestionType nullable string ke PackageQuestion
- `Program.cs` - DI registration AddScoped<HcPortal.Services.GradingService>()

## Decisions Made
- GradingService returns bool (true=sukses, false=race condition) — caller bisa distinguish silent skip vs error
- SaveChanges dulu untuk ET scores sebelum status claim (pola CMPController) — konsisten
- QuestionType ditambah ke model sebagai prerequisite untuk switch-case dapat dikompilasi

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Tambah QuestionType ke PackageQuestion model**
- **Found during:** Task 1 (membuat GradingService.cs)
- **Issue:** GradingService.cs menggunakan `q.QuestionType` tapi property tidak ada di model PackageQuestion — build akan gagal
- **Fix:** Tambah `public string? QuestionType { get; set; }` ke PackageQuestion dengan komentar dokumentasi D-06
- **Files modified:** Models/AssessmentPackage.cs
- **Verification:** dotnet build sukses 0 errors
- **Committed in:** 6044e3d4 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix wajib — GradingService tidak bisa dikompilasi tanpa QuestionType di model. Plan 296-01 (migration) mungkin juga menambah property ini, namun berjalan paralel (wave 1). Merge conflict minimal karena penambahan di property yang berbeda.

## Issues Encountered
- Worktree reset ke `de0f2bfc` menyebabkan planning files terlihat sebagai "deleted" di git status — ini expected behavior dari worktree execution, bukan issue
- File dibuat di main repo path terlebih dahulu kemudian di-copy ke worktree path — resolved dengan cp

## Known Stubs
Tidak ada stub. GradingService.cs memiliki implementasi penuh untuk MultipleChoice. NotImplementedException untuk MultipleAnswer dan Essay adalah desain eksplisit (D-08) — bukan stub, melainkan placeholder yang akan diimplementasi di Phase 298.

## Next Phase Readiness
- GradingService siap di-inject ke AssessmentAdminController (Plan 296-03) dan CMPController (future plan)
- QuestionType ada di model — migration Plan 296-01 tinggal menambah kolom ke DB
- Build bersih — siap untuk integration di controller

---
*Phase: 296-data-foundation-gradingservice-extraction*
*Completed: 2026-04-06*
