---
phase: 410-add-participant-backend-live
plan: 01
subsystem: api
tags: [assessment, participant, backend, ajax, ef-core, signalr-deferred, eager-upa]

# Dependency graph
requires:
  - phase: 409-data-foundation-re-entry-guards-exclude-removed-query
    provides: "kolom removal AssessmentSession (RemovedAt/RemovedBy/RemovalReason) + definisi sesi aktif RemovedAt==null"
  - phase: 391
    provides: "DeriveReadyStatus (status siap-mulai Open/Upcoming dari schedule+window, WIB=UTC+7)"
provides:
  - "GetEligibleParticipantsToAdd (HttpGet) — picker pekerja eligible, D-01 exclude sesi APAPUN + D-02 no unit/section filter"
  - "AddParticipantsLive (HttpPost) — tambah peserta live atomic: ready-status + eager UPA + Pre/Post pair + Proton/window guard + JSON added[]/skipped[]"
  - "BuildReadyParticipantSession (private helper) — pabrik AssessmentSession siap-mulai (inherit rep, kolom removal null)"
  - "CreateEagerAssignmentsAsync (private helper) — eager UserPackageAssignment per sesi baru (mirror StartExam)"
affects: [411-remove-restore-participant, 412-signalr-ui-panel, 413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Eager UPA-creation di luar StartExam (A1) — reuse ShuffleEngine + SiblingSessionQuery dalam transaksi add"
    - "Server-authoritative batch resolve dari sessionId representatif (anti-tampering, tak trust batchKey client)"

key-files:
  created: []
  modified:
    - "Controllers/AssessmentAdminController.cs — +2 endpoint AJAX + 2 helper privat (241 baris)"

key-decisions:
  - "A1 EAGER UPA (LOCKED orchestrator): UserPackageAssignment dibuat DALAM transaksi add via CreateEagerAssignmentsAsync (cermin StartExam :1038-1117), BUKAN lazy — atomic dengan sesi"
  - "D-01: idempotency Add + eligible exclude = sesi APAPUN (tanpa filter RemovedAt) — user removed hanya balik via Restore (411)"
  - "D-02 (OVERRIDE spec §B4): eligible = semua IsActive minus sesi di batch, tanpa filter unit/section"
  - "D-04: NO SignalR/_hubContext di 410 — broadcast participantAdded di-defer ke Phase 412"
  - "Pre/Post pair pakai DeriveReadyStatus (ganti hardcoded 'Upcoming' di analog :1956/:1977) — PART-06 ready-status"

patterns-established:
  - "BuildReadyParticipantSession: factory sesi ready-status reusable (dipakai jalur Standard + Pre/Post; tersedia untuk 411)"
  - "CreateEagerAssignmentsAsync: eager UPA per sesi baru dengan workerIndex type-aware (Pre/Post terisolasi via SiblingPrePostAwarePredicate)"

requirements-completed: [PART-06, PART-07]

# Metrics
duration: 5min
completed: 2026-06-21
---

# Phase 410 Plan 01: Add-Participant Backend Live Summary

**Dua endpoint AJAX (`GetEligibleParticipantsToAdd` GET + `AddParticipantsLive` POST) + 2 helper privat untuk penambahan peserta live: sesi ready-status + UserPackageAssignment EAGER dalam transaksi atomic, idempotent (sesi APAPUN), Pre/Post pair cross-linked, reject Proton/window, JSON added[]/skipped[] — tanpa SignalR (defer 412).**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-21T03:31:32Z
- **Completed:** 2026-06-21T03:36:05Z
- **Tasks:** 2 (keduanya `tdd="true"`; test write-path di-defer ke Plan 02 + Phase 413 per plan verification)
- **Files modified:** 1 (`Controllers/AssessmentAdminController.cs`)

## Accomplishments
- **`GetEligibleParticipantsToAdd` (HttpGet, Admin/HC):** picker pekerja eligible. Resolve rep server-side dari `sessionId` (anti-tampering); exclude user dengan sesi APAPUN di batch (D-01, TANPA filter `RemovedAt`); semua `IsActive` tanpa filter unit/section (D-02); JSON `[{id, fullName, nip}]`.
- **`AddParticipantsLive` (HttpPost, Admin/HC, antiforgery):** alur 9-langkah — validasi input + cap-50 → resolve rep → Proton reject → window guard ("Window ujian sudah tutup, tidak bisa tambah peserta.", 400) → idempotency skip (sesi APAPUN) → validate users exist → resolve actor → transaksi atomic (BuildReadyParticipantSession + Pre/Post pair cross-link + EAGER UPA + audit `AddParticipantLive`, commit/rollback) → notif `ASMT_ASSIGNED` post-commit → JSON `added[]`/`skipped[]` + counts.
- **`BuildReadyParticipantSession` (private):** factory sesi siap-mulai inherit dari rep, `Status = DeriveReadyStatus` (Open/Upcoming, NEVER InProgress), `AssessmentType ?? "Standard"`, kolom removal null.
- **`CreateEagerAssignmentsAsync` (private, A1):** eager UPA per sesi baru — cermin `StartExam :1038-1117` (siblingSessionIds + workerIndex + `ShuffleEngine.BuildQuestionAssignment`/`BuildOptionShuffle` + sentinel package). Tanpa packages → skip (mode non-paket).
- **Verifikasi:** build 0 error; fast-suite 394/394 hijau (tak regresi); app boot bersih @5277; kedua route terdaftar (HTTP 302 auth-challenge, BUKAN 404); migration=FALSE (tak ada `Migrations/` diff).

## Task Commits

Each task committed atomically:

1. **Task 1: GetEligibleParticipantsToAdd + BuildReadyParticipantSession** - `01e6251f` (feat)
2. **Task 2: AddParticipantsLive (HttpPost) + CreateEagerAssignmentsAsync** - `422b4359` (feat)

_Catatan: kedua task `tdd="true"`, namun per plan `<verification>` ("endpoint baru belum di-test, ditambah Plan 02") + orchestrator scope, file test write-path (PART-06/07) adalah tanggung jawab Plan 02 (`410-02`) + Phase 413. Plan 01 = implementasi endpoint + helper; verifikasi via build + fast-suite-no-regression + app-boot. Tidak ada commit `test(...)` di plan ini — lihat TDD Gate Compliance._

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` — Disisipkan setelah `DeriveReadyStatus` (`:2283`): `GetEligibleParticipantsToAdd` (`:2294`), `BuildReadyParticipantSession` (`:2322`), `AddParticipantsLive` (`:2356`), `CreateEagerAssignmentsAsync` (`:2488`). Total +241 baris, 0 baris dihapus.

## Decisions Made
- **A1 EAGER UPA diterapkan** sesuai LOCKED orchestrator (OVERRIDE rekomendasi RESEARCH opsi-A lazy): UPA dibuat dalam transaksi add. Field/method ShuffleEngine + AssessmentPackage diverifikasi via Read sebelum implement:
  - `ShuffleEngine.BuildQuestionAssignment(List<AssessmentPackage>, bool, int, Random)` → `List<int>` (`Helpers/ShuffleEngine.cs:39`)
  - `ShuffleEngine.BuildOptionShuffle(IEnumerable<PackageQuestion>, bool, Random)` → `Dictionary<int,List<int>>` (`:67`)
  - `AssessmentPackage.PackageNumber` + `AssessmentSessionId` (VERIFIED)
  - `SiblingSessionQuery.SiblingPrePostAwarePredicate(string, string, DateTime, string?)` (VERIFIED — Pre/Post terisolasi, workerIndex konsisten)
  - `IsPrePostSession` = `public static` di base `AdminBaseController:248` (dipanggil langsung).
- **D-01/D-02/D-04** diterapkan verbatim (lihat frontmatter key-decisions).
- **Pre/Post pair** pakai `DeriveReadyStatus` (bukan hardcoded `"Upcoming"` di analog `:1956/:1977`) untuk ready-status PART-06.

## Deviations from Plan

None - plan dieksekusi persis sesuai tulisan. Semua nilai konkret (signature, atribut, pesan locked, langkah 1-9, helper) mengikuti `<action>` plan. Tidak ada deviasi Rule 1-4; tidak ada perubahan arsitektur; tidak ada auto-fix.

Catatan implementasi (bukan deviasi): plan `<action>` menulis `AccessToken = rep.AccessToken` di body BULK ASSIGN tapi acceptance/excerpt helper menulis `AccessToken = rep.AccessToken ?? ""` — diterapkan versi `?? ""` (sesuai blok `BuildReadyParticipantSession` di plan Task 1b) karena `AccessToken` adalah `string` non-null (default `""`); konsisten dengan model.

## TDD Gate Compliance

Plan frontmatter `type: execute` (BUKAN `type: tdd`), namun kedua task ber-`tdd="true"`. Test write-path (PART-06/07) secara eksplisit di-defer:
- Plan `<verification>` butir 2: _"endpoint baru belum di-test, ditambah Plan 02"_.
- RESEARCH §Validation: file test `FlexibleParticipantAddLiveTests` = Wave 0 gap untuk Plan 02 / Phase 413.

Karena itu **tidak ada commit `test(...)` (RED gate) di Plan 01** — ini sesuai desain plan, bukan pelanggaran. Gate `feat(...)` (GREEN) hadir 2× (`01e6251f`, `422b4359`). Test ASLI yang menjalankan logika produksi (anti-tautology, lesson 999.12) akan ditambah di Plan 02 yang meng-assert UPA EAGER ada setelah AddParticipantsLive.

## Issues Encountered
- Background `dotnet run` task ter-report exit code 1 — penyebabnya `taskkill` manual untuk menghentikan app setelah verifikasi route, BUKAN boot failure. App sudah boot sukses ("Now listening on: http://localhost:5277" + "Application started") dan melayani kedua route (HTTP 302). Tidak ada masalah nyata.

## User Setup Required
None - tidak ada konfigurasi external service. `user_setup: []` di plan frontmatter.

## Next Phase Readiness
- **Plan 02 (410-02):** siap menulis test write-path (`FlexibleParticipantAddLiveTests`) — assert ready-status, idempotent, window-reject, Pre/Post pair + LinkedSessionId, Proton reject, dan **UPA EAGER tercipta** (A1). Eligible/idempotency via InMemory real-controller (pola `ParticipantRemovalExcludeTests`); write-path via SQLEXPRESS disposable.
- **Phase 411 (Remove/Restore):** `BuildReadyParticipantSession` helper tersedia untuk reuse. File `AssessmentAdminController.cs` di-edit sequential — re-verify file:line bila perlu.
- **Phase 412 (SignalR + UI):** endpoint return JSON `added[]/skipped[]` siap dikonsumsi DOM inject; broadcast `participantAdded` + handler client ditambah di sini (D-04).
- **Build + fast-suite hijau, migration=FALSE** — siap notify IT (migration=FALSE) saat fase di-secure/promosi. **NOT pushed** (lokal saja, deploy bareng milestone v32.5).

## Self-Check: PASSED

- FOUND: `Controllers/AssessmentAdminController.cs` (modified, 4/4 new signatures present)
- FOUND: `.planning/phases/410-add-participant-backend-live/410-01-SUMMARY.md`
- FOUND commit: `01e6251f` (Task 1 — GetEligibleParticipantsToAdd + BuildReadyParticipantSession)
- FOUND commit: `422b4359` (Task 2 — AddParticipantsLive + CreateEagerAssignmentsAsync)
- Build: succeeded, 0 error · Fast-suite: 394/394 · App boot: OK @5277 · Routes: 302 (registered) · migration=FALSE

---
*Phase: 410-add-participant-backend-live*
*Completed: 2026-06-21*
