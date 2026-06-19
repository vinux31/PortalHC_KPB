---
phase: 393-backend-core-inject
plan: 01
subsystem: testing
tags: [assessment, grading, inject, backend, service, dto, dependency-injection, xunit]

requires:
  - phase: 296-grading-service
    provides: GradingService.GradeAndCompleteAsync (delegasi grading non-transaksional)
  - phase: 382-grading-lifecycle
    provides: SubmitResurrectionFixture (pola disposable real-SQL fixture)
provides:
  - "DTO kontrak inject (InjectRequest/InjectResult/InjectWorkerSpec/InjectQuestionSpec/InjectOptionSpec/InjectAnswerSpec/InjectRowError + enum InjectCertMode) — POCO murni"
  - "InjectAssessmentService skeleton: ctor DI (ApplicationDbContext, GradingService, ILogger) + signature InjectBatchAsync(InjectRequest, actorUserId, actorName)"
  - "DI registration Program.cs (AddScoped, sejajar GradingService)"
  - "Kelas test Integration + fixture disposable HcPortalDB_Test_{guid} + NewInjectService factory + SeedUserAsync(nip) + BuildSampleRequest + 5 stub fact SC1..SC5"
affects: [393-02-backend-impl, 393-03-test-assertions, 394-controller-ui-inject]

tech-stack:
  added: []
  patterns:
    - "Interface-first foundation: DTO + skeleton + test fixture ditetapkan dulu (Wave 0) sebelum impl (Plan 02) & assertion (Plan 03)"
    - "POCO DTO (tidak ter-attach DbContext) → service standalone-testable, no mass-assignment EF"

key-files:
  created:
    - Models/InjectAssessmentDtos.cs
    - Services/InjectAssessmentService.cs
    - HcPortal.Tests/InjectAssessmentServiceTests.cs
  modified:
    - Program.cs

key-decisions:
  - "DTO POCO murni (RESEARCH Open Q#1): service terima POCO question/option lalu insert sendiri — bukan entity EF ter-attach"
  - "ctor service inject GradingService + ILogger + ApplicationDbContext SAJA; AuditLogService TIDAK di-inject (Plan 02 pakai _context.AuditLogs.Add langsung)"
  - "actorUserId/actorName = parameter eksplisit (service tak punya HttpContext, RESEARCH A4); RBAC ditegakkan di Phase 394 controller"
  - "Fixture disposable real-SQL (HcPortalDB_Test_{guid}) bukan InMemory — GradeAndCompleteAsync (delegasi Plan 02) pakai ExecuteUpdateAsync yang tak didukung EF Core 8 InMemory; DB Dev tak tersentuh"
  - "5 stub fact HIJAU placeholder (Assert.True(true)) BUKAN RED — Plan 03 ganti dengan assertion nyata"

patterns-established:
  - "InjectBatchAsync signature FINAL & stabil — Plan 02 isi body, Plan 03 panggil tanpa ubah signature"
  - "TempId memetakan jawaban worker → soal/opsi pre-persist (sebelum entity punya Id DB)"

requirements-completed: [INJ-01, INJ-02]

duration: 12 min
completed: 2026-06-17
---

# Phase 393 Plan 01: Backend Core Inject — Foundation (Interface-First) Summary

**Kontrak DTO inject + InjectAssessmentService skeleton (ctor DI + signature InjectBatchAsync) + DI registration + kelas test xUnit Integration dengan fixture disposable real-SQL + 5 stub fact SC1..SC5 — fondasi yang dikonsumsi Plan 02 (impl) & Plan 03 (assertion) tanpa eksplorasi codebase ulang.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-06-17
- **Completed:** 2026-06-17
- **Tasks:** 3
- **Files modified:** 4 (3 baru + Program.cs)

## Accomplishments
- DTO kontrak POCO lengkap (7 class + 1 enum) — interface yang dipakai service + test, struktur field FINAL
- Service skeleton ter-compile dengan ctor DI benar + signature InjectBatchAsync final + DI ter-register (build resolve)
- Kelas test Integration + fixture disposable real-SQL (DB Dev tak tersentuh) + factory real GradingService + seed helper NIP + BuildSampleRequest (MC+MA+Essay) + 5 stub fact (nama match filter VALIDATION)
- Build 0 error (main + test); fast suite 347/347 (no regression, baseline Phase 387); 0 migration

## Task Commits

Each task was committed atomically:

1. **Task 1: DTO kontrak + register DI** — `fb2bdd69` (feat)
2. **Task 2: Skeleton InjectAssessmentService** — `1cc0ff1f` (feat)
3. **Task 3: Test class + fixture + 5 stub fact** — `7599e302` (test)

## Files Created/Modified
- `Models/InjectAssessmentDtos.cs` — DTO kontrak POCO request/result service inject
- `Services/InjectAssessmentService.cs` — skeleton service + ctor DI + signature InjectBatchAsync (body stub Plan 02)
- `HcPortal.Tests/InjectAssessmentServiceTests.cs` — Integration test class + disposable SQL fixture + factory + seed + 5 stub fact
- `Program.cs` — AddScoped<InjectAssessmentService> (sejajar GradingService, setelah L54)

## Decisions Made
- DTO POCO murni (tidak ter-attach DbContext) → service standalone-testable, hindari mass-assignment EF (RESEARCH Open Q#1)
- ctor TIDAK inject AuditLogService — Plan 02 audit via `_context.AuditLogs.Add` langsung (acceptance Task 2: 0×)
- actorUserId/actorName parameter eksplisit (no HttpContext di service); RBAC di Phase 394
- Fixture disposable real-SQL bukan InMemory (ExecuteUpdateAsync di GradeAndCompleteAsync) — pola SubmitResurrectionFixture verbatim

## Deviations from Plan

None - plan executed exactly as written.

**Catatan urutan eksekusi:** Program.cs (Task 1) mereferensikan `InjectAssessmentService` yang dibuat di Task 2 (interface-first by design — acceptance Task 1 menyebut "setelah Task 2 selesai build hijau"). Service stub dibuat sebelum build pertama; verifikasi build menutup acceptance Task 1 + Task 2 bersamaan. Komit tetap atomik per-task (urutan task).

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Kontrak DTO + signature InjectBatchAsync stabil → siap dikonsumsi **Plan 02** (isi body InjectBatchAsync: pre-flight D-03 → dedup D-01/D-02 → tx D-04 → per-worker insert+grade+finalize+backdate+audit).
- 5 stub fact (HIJAU placeholder) + fixture + BuildSampleRequest siap di-upgrade jadi assertion nyata di **Plan 03**.
- 0 migration; tidak ada blocker.

---
*Phase: 393-backend-core-inject*
*Completed: 2026-06-17*
