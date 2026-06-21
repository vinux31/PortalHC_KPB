---
phase: 409-data-foundation-re-entry-guards-exclude-removed-query
plan: 02
subsystem: assessment-read-path
tags: [soft-remove, re-entry-guard, exclude-removed, signalr, monitoring, security, server-authoritative, de-tautology]

# Dependency graph
requires:
  - phase: 409-01
    provides: "3 kolom RemovedAt/RemovedBy/RemovalReason + invarian soft-removed <=> RemovedAt != null + migration applied HcPortalDB_Dev"
provides:
  - "Guard re-entry server-authoritative: StartExam + SubmitExam blok sesi RemovedAt != null (pesan locked, redirect, no-mark/no-grade)"
  - "CMPController.IsParticipantRemoved(session) — seam tunggal deteksi removed (RemovedAt != null), testable (pola IsResultsAuthorized)"
  - "AssessmentHub.JoinBatch + SaveTextAnswer + SaveMultipleAnswer predikat += RemovedAt == null (silent-skip, A1 defense-in-depth)"
  - "Exclude RemovedAt != null di 3 query monitoring batch-aktif (Tab/Monitoring/Detail + semua count) — PLIV-01 foundation"
  - "Boundary terjamin: UserAssessmentHistory per-pekerja TETAP tampil removed (anti over-exclude D-01a)"
  - "HcPortal.Tests/ParticipantRemovalGuardTests.cs — 6 fact de-tautologis (3 guard + 2 exclude + 1 boundary)"
affects: [410 (idempotency add cek sesi aktif RemovedAt==null), 411 (tulis/clear kolom removal + reuse IsParticipantRemoved), 412 (panel Peserta Dikeluarkan baca removed + force-kick examRemoved), 413 (test+UAT live regression)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Seam guard testable static (CMPController.IsParticipantRemoved) cermin IsResultsAuthorized — single-source deteksi removed via RemovedAt (BUKAN Status, akar D-04)"
    - "Per-surface .Where(RemovedAt == null) eksplisit — BUKAN EF global HasQueryFilter (FORBIDDEN: akan sembunyikan removed dari riwayat pekerja + panel 412)"
    - "De-tautology test (999.12): drive action/query produksi ASLI (InMemory real-controller exclude/boundary + SQLEXPRESS disposable guard + EF AnyAsync nyata JoinBatch), bukan replica predikat"

key-files:
  created:
    - "HcPortal.Tests/ParticipantRemovalGuardTests.cs"
  modified:
    - "Controllers/CMPController.cs"
    - "Hubs/AssessmentHub.cs"
    - "Controllers/AssessmentAdminController.cs"

key-decisions:
  - "Ekstrak seam CMPController.IsParticipantRemoved(AssessmentSession)=>RemovedAt != null (deviasi minor dari verbatim inline plan) agar guard StartExam/SubmitExam testable de-tautologis tanpa WebApplicationFactory (proyek tak punya infra-nya). Behavior + pesan locked byte-identik."
  - "A1 IN scope (orchestrator resolved): SaveTextAnswer + SaveMultipleAnswer predikat session-load += RemovedAt == null (defense-in-depth PRMV-03 'jawaban tak terhitung') — JoinBatch guard tak menendang koneksi SignalR yang sudah join."
  - "A2 OUT scope: ExportAssessmentResults/BulkExportPdf/GetDeleteImpact TIDAK di-exclude di 409 (blast-radius minimal D-01a). Revisit 412/413 bila perlu."
  - "NO EF global HasQueryFilter — exclude per-surface eksplisit 3 query monitoring saja (D-01)."

patterns-established:
  - "Guard re-entry soft-remove: IsParticipantRemoved seam + TempData+redirect (pola block Abandoned) SEBELUM mark-InProgress (StartExam) / SEBELUM grading (SubmitExam)"
  - "Hub answer-write defense-in-depth: tambah RemovedAt==null ke predikat session-load FirstOrDefaultAsync, pertahankan silent return-if-null existing"

requirements-completed: [PRMV-03]

# Metrics
duration: 20min
completed: 2026-06-21
---

# Phase 409 Plan 02: Re-entry Guards + Exclude-Removed Query Summary

**Invarian soft-remove read-path lengkap: guard server-authoritative anti-resume/resubmit (StartExam/SubmitExam) + silent-skip re-join/answer-write (Hub JoinBatch/SaveTextAnswer/SaveMultipleAnswer A1) + exclude `RemovedAt != null` dari 3 query monitoring batch-aktif, dengan boundary UserAssessmentHistory tetap menampilkan removed (anti over-exclude) — divalidasi 6 test de-tautologis yang menjalankan action/query produksi ASLI.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-06-21T01:22:34Z
- **Completed:** 2026-06-21T01:42:47Z
- **Tasks:** 3 (Task 1 test scaffold Wave-0, Task 2 guard re-entry, Task 3 exclude-removed)
- **Files modified:** 4 (1 created test, 3 modified kode produksi)

## Accomplishments

- **Guard re-entry server-authoritative (PRMV-03 / T-409-01, T-409-02):** `CMPController.StartExam` (guard SEBELUM auto-transition Upcoming→Open & SEBELUM mark-InProgress) + `SubmitExam` (guard SEBELUM `ShouldGateMissingStart`/grading) → sesi `RemovedAt != null` di-redirect ke "Assessment" + `TempData["Error"]="Anda telah dikeluarkan dari ujian ini."` (locked verbatim, tepat 2x). Sesi removed TIDAK PERNAH ter-mark InProgress / di-grade.
- **Seam tunggal `CMPController.IsParticipantRemoved(session) => session.RemovedAt != null`** (cermin pola `IsResultsAuthorized`) — deteksi removed eksplisit via `RemovedAt`, BUKAN `Status` (akar D-04: soft-remove tak mutasi Status). Dipanggil kedua guard inline + dipakai test de-tautologis.
- **Hub silent-skip (T-409-03 + A1 T-409-08):** `JoinBatch` `AnyAsync` += `&& s.RemovedAt == null` (silent `return` dipertahankan, no throw) + `SaveTextAnswer`/`SaveMultipleAnswer` predikat `FirstOrDefaultAsync` += `&& s.RemovedAt == null` (silent return-if-null existing dipertahankan, log/pesan tak diubah). Worker removed mid-exam dgn koneksi SignalR hidup tak bisa re-join ATAU tulis jawaban via Hub langsung.
- **Exclude-removed 3 query monitoring (PLIV-01 foundation):** `managementQuery` (Tab grouping), `AssessmentMonitoring.query` (list aggregate), `AssessmentMonitoringDetail.query` (predikat += `&& a.RemovedAt == null` → menggerakkan SEMUA count termasuk `InProgressCount`/`TotalCount`). Grouping/count blok inherit otomatis (tak disentuh).
- **Boundary anti over-exclude (T-409-09 / D-01a):** `UserAssessmentHistory` (`:5263`), export/impact, `WorkerDataService` TIDAK disentuh. NO EF global `HasQueryFilter`. Test boundary `UserAssessmentHistory_StillShows_RemovedSession` membuktikan sesi removed bersertifikat TETAP muncul di riwayat pekerja (sertifikat utuh & reversibel).
- **6 test de-tautologis (lesson 999.12):** exclude/boundary via InMemory real-controller (`AssessmentAdminController` ASLI, baca `ViewData["ManagementData"]`/`MonitoringGroupViewModel`/`UserAssessmentHistoryViewModel`); guard via SQLEXPRESS disposable (helper produksi `IsParticipantRemoved` atas entitas dimuat dari SQL nyata) + EF `AnyAsync` NYATA terhadap schema riil (JoinBatch). TIDAK ada test yang menulis-ulang predikat lalu meng-assert-nya.

## Task Commits

Each task was committed atomically:

1. **Task 1: test scaffold de-tautologis (6 facts Wave-0)** - `cf7838b5` (test)
2. **Task 2: guard re-entry StartExam+SubmitExam+JoinBatch+Save\* (A1)** - `a0afd785` (feat)
3. **Task 3: exclude-removed 3 query monitoring batch-aktif** - `2baf7402` (feat)

**Plan metadata:** (final docs commit — STATE.md + ROADMAP.md + REQUIREMENTS.md + SUMMARY.md)

## Files Created/Modified

- `HcPortal.Tests/ParticipantRemovalGuardTests.cs` - NEW; 2 kelas: `ParticipantRemovalExcludeTests` (InMemory real-controller: Tab exclude + Detail count exclude + UserHistory boundary) + `ParticipantRemovalGuardTests` ([Trait Category=Integration] SQLEXPRESS disposable: StartExam/SubmitExam helper + JoinBatch EF AnyAsync nyata) + `ParticipantRemovalGuardFixture`.
- `Controllers/CMPController.cs` - +seam `IsParticipantRemoved` (`:2531`) + guard StartExam (`:915`, sebelum mark-InProgress `:1008`) + guard SubmitExam (`:1602`, sebelum grading).
- `Hubs/AssessmentHub.cs` - JoinBatch AnyAsync (`:30`) + SaveTextAnswer FirstOrDefaultAsync (`:144`) + SaveMultipleAnswer FirstOrDefaultAsync (`:210`) — masing-masing += `&& s.RemovedAt == null` (3 occurrence).
- `Controllers/AssessmentAdminController.cs` - +`.Where(a => a.RemovedAt == null)` di managementQuery (`:121`) + AssessmentMonitoring.query (`:2825`) + AssessmentMonitoringDetail.query predikat (`:3335`).

## Decisions Made

- **Seam `IsParticipantRemoved` (deviasi minor verbatim):** Plan memberi guard inline `if (assessment.RemovedAt != null)`. Karena `StartExam`/`SubmitExam` dereference `_userManager`/`_impersonationService`/`GetCurrentUserRoleLevelAsync` SEBELUM titik guard (tak mungkin di-construct di unit test; proyek TAK punya WebApplicationFactory — dikonfirmasi grep + komentar `RecordCascadeUiTests`), guard di-bungkus seam static `IsParticipantRemoved(session) => session.RemovedAt != null` (cermin `CMPController.IsResultsAuthorized` yang sudah jadi pola codebase untuk authz testable). Behavior + pesan locked byte-identik; test de-tautologis kini memanggil REAL production code. Lihat Deviations Rule 2.
- **A1 IN scope:** `SaveTextAnswer`/`SaveMultipleAnswer` += `RemovedAt == null` (orchestrator resolved A1=IN, RESEARCH §Open Questions). Defense-in-depth: `JoinBatch` guard tak menendang koneksi SignalR yang sudah join → Save* langsung masih bisa tulis jawaban tanpa guard ini.
- **A2 OUT scope:** export/impact tak di-exclude (lihat Deferred).
- **NO global HasQueryFilter:** per-surface `.Where` eksplisit (D-01) — global filter akan menyembunyikan removed dari riwayat pekerja (langgar D-01a) + panel 412 (yang JUSTRU butuh baca removed).

## Deviations from Plan

### Auto-fixed / Adjusted

**1. [Rule 2 - Missing critical functionality] Ekstrak seam `CMPController.IsParticipantRemoved` agar guard testable de-tautologis**
- **Found during:** Task 1/Task 2 (perancangan test guard StartExam/SubmitExam)
- **Issue:** Plan memberi guard inline verbatim, tapi mandat de-tautology (999.12) mewajibkan test menjalankan logika produksi ASLI — sementara `StartExam`/`SubmitExam` tak bisa dijalankan via unit test (deref `_userManager`/`_impersonationService`) DAN proyek tak punya WebApplicationFactory/TestServer. Tanpa seam, satu-satunya cara uji guard = replica predikat (FORBIDDEN).
- **Fix:** Tambah static `public static bool IsParticipantRemoved(AssessmentSession session) => session.RemovedAt != null;` (pola eksak `IsResultsAuthorized`). Guard inline jadi `if (IsParticipantRemoved(assessment))` — behavior & pesan locked identik. Test memuat entitas dari SQLEXPRESS nyata lalu memanggil seam produksi ini.
- **Files modified:** `Controllers/CMPController.cs`
- **Commit:** `cf7838b5` (seam stub Wave-0 RED), `a0afd785` (implement + wire guard)

**2. [Rule 3 - Blocking test infra] ActionDescriptor.ActionName + StubUrlHelper + seed-order untuk InMemory real-controller**
- **Found during:** Task 2/Task 3 (test exclude/boundary)
- **Issue:** (a) controller override `View(model)` mereferensi `ControllerContext.ActionDescriptor.ActionName` (NRE bila null); (b) `AssessmentMonitoringDetail` memanggil `Url.Action` (NRE bila `Url` null); (c) `ctx.AssessmentSessions.First(...)` sebelum `SaveChanges` tak melihat entitas unsaved di EF InMemory ("Sequence contains no elements").
- **Fix:** `MakeController(actionName)` set `ActionDescriptor.ActionName` + `ctrl.Url = StubUrlHelper`; seed `StartedAt` di objek sebelum `AddRange`. Murni test-harness (tak ubah produksi).
- **Files modified:** `HcPortal.Tests/ParticipantRemovalGuardTests.cs`
- **Commit:** `a0afd785`, `2baf7402`

**Total deviations:** 2 (1 produksi minor seam Rule 2, 1 test-infra Rule 3). Tidak ada scope creep — keduanya untuk memenuhi mandat de-tautology + menjalankan action ASLI.

## Deferred Issues / Out of Scope

- **A2 — `ExportAssessmentResults` / `BulkExportPdf` / `GetDeleteImpact.certCount` TIDAK di-exclude di 409** (orchestrator resolved A2=OUT, blast-radius minimal D-01a). Sesi removed masih bisa muncul di ekspor batch (impact rendah: ekspor jarang, removed jarang). **Revisit Phase 412/413 bila muncul kebutuhan nyata.**
- **T-409-10 — RemovalReason free-text XSS-at-render = concern Phase 412.** Phase 409 read-path only (EF parameterized, no render baru). Penulisan `RemovalReason` = Phase 411; render di panel "Peserta Dikeluarkan" = Phase 412 → escape/encode WAJIB di sana.

## Threat Flags

Tidak ada threat surface baru di luar `<threat_model>` plan. 3 mitigasi high-severity (T-409-01/02 server-authoritative blocking + T-409-03/08 Hub silent-skip) GREEN; T-409-09 boundary GREEN; T-409-10 di-defer eksplisit ke 412.

## Known Stubs

None. Tidak ada placeholder/hardcoded-empty yang mengalir ke UI. (Catatan: `IsParticipantRemoved` sempat di-commit sebagai stub `return false` di Task 1 untuk kontrak Wave-0 RED, lalu diimplementasi penuh `RemovedAt != null` di Task 2 — bukan stub residual.)

## Verification

- `dotnet build HcPortal.csproj` → **0 error** (warning pre-existing only).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ParticipantRemoval"` → **6/6 GREEN** (3 guard + 2 exclude + 1 boundary).
- Full suite `dotnet test HcPortal.Tests` → **569/569 GREEN, 0 failed, 0 skipped** (termasuk Integration @SQLEXPRESS + AssessmentWindowRemovalTests + guard Phase 391 FlexibleParticipantAdd + 398.1 — NO regression).
- `dotnet run` (ASPNETCORE_ENVIRONMENT=Development) → `Now listening on: http://localhost:5277`, `Application started`, HTTP 200 `/`, no fatal error.
- Counts: "Anda telah dikeluarkan dari ujian ini." tepat **2x** di CMPController; `RemovedAt == null` **3x** di AssessmentHub; `a.RemovedAt == null` **3x** di AssessmentAdminController; `HasQueryFilter` **0x** di ApplicationDbContext.

## Migration Notes

**migration=FALSE** untuk Plan 02 (no `Migrations/`/`Data/ApplicationDbContext.cs` diff — guard + exclude read-path only). Migration `AddParticipantRemovalColumns` (Plan 01, hash `01cd7dd0`) tetap satu-satunya migration Phase 409 yang perlu di-notify IT.

## User Setup Required

None.

## Next Phase Readiness

- **Phase 410** (Add backend live) dapat reuse invarian `RemovedAt == null` untuk idempotency cek sesi aktif.
- **Phase 411** (Remove/Restore backend) dapat reuse `CMPController.IsParticipantRemoved` seam + menulis/meng-clear `RemovedAt/RemovedBy/RemovalReason`.
- **Phase 412** (UI+SignalR) harus tangani T-409-10 (escape RemovalReason saat render panel) + force-kick `examRemoved`.
- **Phase 413** (test+UAT) jangan regresi 6 test ParticipantRemoval + guard 391/398.1.
- Tidak ada blocker.

## Self-Check: PASSED

- Files created/modified verified present: HcPortal.Tests/ParticipantRemovalGuardTests.cs, Controllers/CMPController.cs, Hubs/AssessmentHub.cs, Controllers/AssessmentAdminController.cs — all FOUND.
- Commits verified in git log: cf7838b5 (Task 1), a0afd785 (Task 2), 2baf7402 (Task 3) — all FOUND.
- Full suite 569/569 GREEN; build 0 error; run @5277 boots; 6 ParticipantRemoval tests GREEN; message 2x / Hub RemovedAt==null 3x / Admin RemovedAt==null 3x / HasQueryFilter 0x.

---
*Phase: 409-data-foundation-re-entry-guards-exclude-removed-query*
*Completed: 2026-06-21*
