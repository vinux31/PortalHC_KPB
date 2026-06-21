---
phase: 411-remove-restore-backend-live
plan: 01
subsystem: api
tags: [aspnet-core-mvc, ef-core, soft-delete, cascade-delete, audit, rbac, antiforgery, idempotent]

# Dependency graph
requires:
  - phase: 409-data-foundation-reentry-guards
    provides: "Kolom RemovedAt/RemovedBy/RemovalReason (migration AddParticipantRemovalColumns) + definisi soft-removed (RemovedAt!=null) + guard re-entry yang andalkan RemovedAt"
  - phase: 410-add-participant-backend-live
    provides: "Eager-UPA (UserPackageAssignment saat add) + Pre/Post cross-link via LinkedSessionId + IsPrePostSession reuse"
provides:
  - "RemoveParticipantCoreAsync (private shared core) — hybrid by-state hard/soft + Pre/Post pair-as-unit via LinkedSessionId + reason-gate D-02 + audit"
  - "SessionHasDataAsync (private) — has-data detection D-01 (StartedAt!=null OR PackageUserResponse; UPA tak dihitung)"
  - "RemoveParticipantLive (HttpPost JSON) — wrapper untuk UI 412: Proton-reject + idempotency noop + JSON outcome {sessionId, mode, linkedSessionId}"
  - "RestoreParticipantLive (HttpPost JSON) — clear 3 kolom session+partner, soft-removed-only (PRMV-04)"
  - "DeleteAssessmentPeserta (HttpPost redirect) — fix stub mati EditAssessment.cshtml:666 (D-04), delegasi core"
affects: [412-live-monitoring-ui-signalr, 413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Private shared core (orkestrasi) dibungkus 3 endpoint thin-wrapper (JSON x2 + redirect x1) — satu sumber kebenaran D-04"
    - "Hybrid by-state delete: hard (RecordCascadeDeleteService via RequestServices) vs soft (set 3 kolom) — keputusan 100% server-side dari kolom DB"
    - "Pre/Post pair-as-unit via LinkedSessionId (per-peserta), BUKAN LinkedGroupId/DeletePrePostGroup (batch lintas-user)"
    - "Reason-gate D-02 ditempatkan SETELAH evaluasi jalur (wajib hanya pada soft)"

key-files:
  created:
    - ".planning/phases/411-remove-restore-backend-live/411-01-SUMMARY.md"
  modified:
    - "Controllers/AssessmentAdminController.cs (+181: RemoveOutcome struct + SessionHasDataAsync + RemoveParticipantCoreAsync + 3 endpoint)"
    - "Views/Admin/EditAssessment.cshtml (-1: hapus style=display:none form deletePesertaForm)"

key-decisions:
  - "RemoveParticipantCoreAsync sebagai private method di controller (bukan service class) — konsisten dgn 410 (CreateEagerAssignmentsAsync/BuildReadyParticipantSession), D-04 cuma butuh 1 sumber kebenaran"
  - "D-01: UPA eager (410) SENGAJA tak dihitung 'data' → not-started tetap hard-delete; RecordCascadeDeleteService.ExecuteAsync SUDAH hapus UPA (:221-222), service TIDAK diperluas"
  - "DeleteAssessmentPeserta (form lama tanpa field reason) = jalur HARD; peserta berdata (butuh reason) → TempData[Error] arahkan ke kontrol Monitoring Detail 412 (Open Q1 resolusi)"
  - "Keputusan #5: TIDAK panggil EnsureCanDeleteAsync di core (soft dipilih untuk Completed/cert; hard hanya not-started → guard no-op natural); mitigasi = soft + audit wajib"
  - "D-03: TIDAK sentuh _hubContext (broadcast participantRemoved/examRemoved = Phase 412)"

patterns-established:
  - "Pattern: thin endpoint wrapper (load+Proton-reject+idempotency+actor) → delegasi private core → JSON/redirect outcome"
  - "Pattern: Pre/Post pair resolve via session.LinkedSessionId.HasValue + evaluasi gabungan has-data (soft keduanya / hard keduanya)"

requirements-completed: [PRMV-01, PRMV-04, PRMV-05, PLIV-03]

# Metrics
duration: 11min
completed: 2026-06-21
---

# Phase 411 Plan 01: Remove + Restore Backend Live Summary

**Backend hapus+pulihkan peserta live — private core `RemoveParticipantCoreAsync` (hybrid hard-delete cascade / soft-remove set-3-kolom + Pre/Post pair via LinkedSessionId + reason-gate D-02 + audit) dibungkus 3 endpoint RBAC (Remove JSON, Restore JSON, DeletePeserta redirect), plus hidupkan tombol hapus per-peserta yang sebelumnya mati.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-06-21T05:56:46Z
- **Completed:** 2026-06-21T06:07:10Z
- **Tasks:** 2
- **Files modified:** 2 (Controllers/AssessmentAdminController.cs, Views/Admin/EditAssessment.cshtml)

## Accomplishments
- **`RemoveParticipantCoreAsync` (JANTUNG fase)** — orkestrasi hybrid by-state: resolve partner Pre/Post via `LinkedSessionId` → evaluasi gabungan has-data → reason-gate D-02 (soft saja) → jalur SOFT (set 3 kolom removal, JANGAN sentuh Score/IsPassed/NomorSertifikat/Status/response) atau jalur HARD (`RecordCascadeDeleteService.ExecuteAsync` via `HttpContext.RequestServices`, cascade hapus UPA otomatis) → audit double-log. Return `RemoveOutcome` (Ok/Mode/Message/PartnerId).
- **`SessionHasDataAsync`** — has-data detection D-01: `StartedAt!=null` OR ada `PackageUserResponse`; UPA eager (410) SENGAJA tak dihitung.
- **3 endpoint thin-wrapper** semua `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` (PLIV-03): `RemoveParticipantLive` (JSON, Proton-reject + idempotency noop + JSON `{sessionId, mode, linkedSessionId}`), `RestoreParticipantLive` (JSON, guard `RemovedAt==null` → 400, clear 3 kolom session+partner simetri, audit), `DeleteAssessmentPeserta` (redirect, D-04 fix stub mati).
- **View un-hide** — `EditAssessment.cshtml:666` form `deletePesertaForm` dihapus `style="display:none;"`; tombol hapus per-peserta (handler JS existing :696-697) jadi hidup, POST delegasi core.
- **Verifikasi runtime** — app boot bersih @5277, 3 route baru terdaftar (POST → 302 auth-challenge, BUKAN 404; control route → 404). Fast suite 581/581 hijau (no regression 409 guard + 410 add).

## Task Commits

Each task was committed atomically:

1. **Task 1: RemoveParticipantCoreAsync + SessionHasDataAsync (private shared core)** - `764516d0` (feat)
2. **Task 2: 3 endpoint wrapper (Remove/Restore JSON + DeletePeserta redirect) + un-hide form** - `220382ec` (feat)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` - +181 baris: `RemoveOutcome` struct (private nested) + `SessionHasDataAsync` + `RemoveParticipantCoreAsync` + 3 endpoint (Remove/Restore/DeletePeserta). Disisip setelah `CreateEagerAssignmentsAsync` (:2542), sebelum `// --- DELETE ASSESSMENT ---`.
- `Views/Admin/EditAssessment.cshtml` - hapus `style="display:none;"` dari form `deletePesertaForm` (tombol hapus per-peserta hidup).
- `.planning/phases/411-remove-restore-backend-live/411-01-SUMMARY.md` - this file.

## Decisions Made
- **Core sebagai private method (bukan service class)** — konsisten dengan pola 410, D-04 hanya butuh 1 sumber kebenaran lintas wrapper (bukan reusability lintas-controller).
- **D-01 verifikasi cascade UPA** — `RecordCascadeDeleteService.ExecuteAsync` SUDAH `RemoveRange(UserPackageAssignments)` di :221-222; service TIDAK diperluas (sesuai RESEARCH VERIFIED).
- **DeleteAssessmentPeserta = jalur HARD** (Open Q1) — form lama tak punya field `reason`; bila core butuh reason (peserta berdata) → `outcome.Ok==false`, `TempData["Error"]` di-suffix " Gunakan kontrol Hapus di Monitoring Detail (butuh alasan)." + redirect. Juga tambah idempotency guard `RemovedAt!=null` → `TempData["Success"]="Peserta sudah dikeluarkan."` (parity dengan endpoint JSON noop).
- **Keputusan #5** — tidak panggil `EnsureCanDeleteAsync` di core (soft untuk Completed/cert; hard hanya not-started → guard no-op). Mitigasi = soft + audit wajib.
- **D-03** — tidak ada `_hubContext` di seluruh kode 411 (broadcast = 412).

## Deviations from Plan

None - plan executed exactly as written.

Catatan: `DeleteAssessmentPeserta` menerima satu penambahan kecil sesuai instruksi plan task 2(c) — idempotency guard `RemovedAt != null` (mengembalikan `TempData["Success"]="Peserta sudah dikeluarkan."`) untuk parity dengan endpoint JSON. Ini tertulis eksplisit di plan (Task 2 langkah c.3), bukan deviasi.

## Issues Encountered
- **Warning CS1998 pra-eksisting** (`AssessmentAdminController.cs:68` `ManageAssessment` lacks `await`) muncul di output build — INI BUKAN kode 411 (method pra-eksisting, di luar scope per SCOPE BOUNDARY). Tidak disentuh. Semua method baru 411 genuinely menggunakan `await`.
- Tidak ada masalah pada pekerjaan terencana.

## User Setup Required
None - no external service configuration required. migration=FALSE (set/clear kolom existing 409 + cascade existing; `git status Migrations/ Data/` kosong).

## Next Phase Readiness
- **Plan 02 (411-02)** siap: test write-path lengkap (`FlexibleParticipantRemoveTests.cs` NEW) — PRMV-01/04/05 + PLIV-03 de-tautologis. Catat **gap test-infra terbesar 411**: jalur hard-delete `RemoveParticipantCoreAsync` panggil `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()` → test write-path WAJIB set `ControllerContext.HttpContext.RequestServices` ke mini-DI ber-`RecordCascadeDeleteService` (+ `ProtonCompletionService`, `AuditLogService`, `IWebHostEnvironment` stub WebRootPath temp). Lihat 411-PATTERNS.md §"TEST-INFRA GAP TERBESAR".
- **Phase 412** konsumsi JSON outcome (`{sessionId, mode, linkedSessionId}`) + tambah SignalR `participantRemoved`/`examRemoved` + modal keras + panel "Peserta Dikeluarkan" + escape/encode `RemovalReason` saat render (carry T-409-10 XSS-at-render).
- Notify IT: Phase 411 = migration=FALSE (tak ada schema baru). Branch main, NOT pushed (deploy bareng v32.5 bundle).

## Self-Check: PASSED

- FOUND: `.planning/phases/411-remove-restore-backend-live/411-01-SUMMARY.md`
- FOUND commit: `764516d0` (Task 1)
- FOUND commit: `220382ec` (Task 2)
- Code symbols present (RemoveParticipantCoreAsync/SessionHasDataAsync/RemoveParticipantLive/RestoreParticipantLive/DeleteAssessmentPeserta): 18 references
- Build: succeeded, 0 errors
- Fast suite: 581/581 passed (0 failed, 0 skipped)
- Routes registered: 3 × 302 (auth-challenge), control 404 → confirms registration
- migration=FALSE (Migrations/ Data/ clean)

---
*Phase: 411-remove-restore-backend-live*
*Completed: 2026-06-21*
