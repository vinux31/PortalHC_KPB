---
phase: 412-live-monitoring-ui-signalr
plan: 01
subsystem: api
tags: [signalr, ihubcontext, ef-core, broadcast, monitoring, viewbag, aspnet-mvc]

# Dependency graph
requires:
  - phase: 410-add-participant-backend-live
    provides: AddParticipantsLive endpoint (createdSessions + userDictionary post-commit) dengan defer-broadcast D-04
  - phase: 411-remove-restore-backend-live
    provides: RemoveParticipantLive/RestoreParticipantLive + RemoveOutcome{Mode,PartnerId} dengan defer-broadcast D-03
  - phase: 409-data-foundation
    provides: kolom RemovedAt/RemovedBy/RemovalReason + exclude-query RemovedAt==null
provides:
  - "Broadcast SignalR post-commit di 3 endpoint 410/411 (participantAdded x2, participantRemoved + Pre/Post pair, examRemoved force-kick)"
  - "ViewBag.RemovedSessions (List<RemovedParticipantViewModel> typed) di AssessmentMonitoringDetail = sumber data panel 'Peserta Dikeluarkan'"
  - "RemovedParticipantViewModel typed (Id/FullName/Nip/RemovedAt/RemovedByName/RemovalReason)"
  - "NoopHubContext test stub (IHubContext<AssessmentHub> no-op) reusable utk write-path test endpoint yang broadcast"
affects: [412-02-monitoring-view-panel, 412-03-startexam-force-kick, 413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Broadcast post-commit: _hubContext.Clients.Group($\"monitor-{batchKey}\").SendAsync(...) HANYA setelah CommitAsync/SaveChanges"
    - "Capture wasInProgress + identitas user SEBELUM core mutasi (hard-delete men-detach entity)"
    - "Resolve nama/NIP broadcast via _context.Users dict lookup (BUKAN .Include — InMemory nav-null memfilter baris)"
    - "removedSessions = kebalikan exclude-query (RemovedAt != null) + resolve RemovedBy->FullName via dictionary"

key-files:
  created:
    - HcPortal.Tests/NoopHubContext.cs
  modified:
    - Controllers/AssessmentAdminController.cs
    - Models/AssessmentMonitoringViewModel.cs
    - HcPortal.Tests/FlexibleParticipantAddLiveTests.cs
    - HcPortal.Tests/FlexibleParticipantRemoveTests.cs

key-decisions:
  - "Resolve FullName/NIP broadcast via lookup _context.Users dict (TryGetValue), BUKAN .Include(s=>s.User) — InMemory nav-null silently drop baris (regresi read-path 411-02)"
  - "Pre/Post pair → 2 event participantRemoved (session + outcome.PartnerId) agar kedua baris konsisten lintas-tab (Open-Q2 RESOLVED)"
  - "examRemoved HANYA bila wasInProgress (StartedAt!=null && CompletedAt==null && RemovedAt==null), di-capture SEBELUM core (Open-Q1 RESOLVED)"
  - "NoopHubContext stub utk write-path test (endpoint kini broadcast post-commit) — kontrak JSON TIDAK berubah"

patterns-established:
  - "Broadcast post-commit guard: SendAsync setelah commit (anti notif tx-rollback, spec §G)"
  - "Identity capture pre-core: tangkap nama/nip/wasInProgress sebelum cascade hard-delete men-detach"

requirements-completed: [PLIV-01, PLIV-02, PRMV-02]

# Metrics
duration: 35min
completed: 2026-06-21
---

# Phase 412 Plan 01: Live Monitoring SignalR Broadcast + Removed-Sessions Query Summary

**Broadcast SignalR post-commit (participantAdded/participantRemoved/examRemoved) di-wire ke 3 endpoint 410/411 + query removedSessions (RemovedAt!=null) typed via ViewBag.RemovedSessions sebagai fondasi server-side UI Monitoring live**

## Performance

- **Duration:** ~35 min
- **Started:** 2026-06-21T07:45:00Z (approx)
- **Completed:** 2026-06-21T08:06:28Z
- **Tasks:** 3
- **Files modified:** 5 (1 created, 4 modified)

## Accomplishments
- AddParticipantsLive + RestoreParticipantLive broadcast `participantAdded` post-commit ke grup `monitor-{batchKey}` (baris muncul/balik live tanpa reload)
- RemoveParticipantLive broadcast `participantRemoved` (dengan field `mode` hard/soft) + event KEDUA untuk Pre/Post pair (via `outcome.PartnerId`) + `examRemoved` force-kick HANYA untuk worker yang tadinya InProgress
- AssessmentMonitoringDetail expose `ViewBag.RemovedSessions` (List<RemovedParticipantViewModel> typed) — sumber data panel "Peserta Dikeluarkan" (nama/NIP/waktu/oleh/alasan)
- Kontrak JSON ketiga endpoint 410/411 TIDAK berubah (test 410-02/411-02 tetap hijau); broadcast murni additive post-commit

## Task Commits

Each task was committed atomically:

1. **Task 1: Broadcast post-commit ke 3 endpoint 410/411 + force-kick examRemoved** - `da1e62e5` (feat)
2. **Task 2: Query removedSessions + RemovedParticipantViewModel typed + ViewBag panel data** - `6d3f0e26` (feat)
3. **Task 3: Regression fix (.Include removal) + NoopHubContext stub + verification** - `8623e68e` (fix)

_Catatan: Task 3 menggabungkan deviasi Rule 1 (Include-regresi fix) dengan gate verifikasi karena keduanya diperlukan agar suite hijau._

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` - Broadcast post-commit di AddParticipantsLive/RemoveParticipantLive/RestoreParticipantLive (participantAdded/participantRemoved/examRemoved) + capture wasInProgress/identitas pre-core + resolve nama/NIP via dict lookup + query removedSessions (RemovedAt!=null) + ViewBag.RemovedSessions
- `Models/AssessmentMonitoringViewModel.cs` - RemovedParticipantViewModel typed (Id/FullName/Nip/RemovedAt/RemovedByName/RemovalReason)
- `HcPortal.Tests/NoopHubContext.cs` - Stub IHubContext<AssessmentHub> no-op (menelan SendAsync/Group/User) utk write-path test
- `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` - wire NoopHubContext ke factory write-path (AddParticipantsLive kini broadcast)
- `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` - wire NoopHubContext ke factory write-path (Remove/Restore kini broadcast)

## Decisions Made
- **Resolve nama/NIP via dict lookup, bukan `.Include`** — `.Include(s => s.User)` pada InMemory provider mem-filter baris ber-FK-nav-null (lihat komentar test AddLive :87-88 "baris dengan FK absen di-drop diam-diam"), memutus read-path test 411-02 yang tak men-seed `ApplicationUser`. Lookup terpisah `_context.Users` + `TryGetValue` aman di InMemory dan SQL Server.
- **Pre/Post pair = 2 event `participantRemoved`** (Open-Q2 RESOLVED) — broadcast event kedua via `outcome.PartnerId` agar kedua baris (Pre & Post, sering di tab terpisah) konsisten.
- **`examRemoved` hanya `wasInProgress`** (Open-Q1 RESOLVED) — flag di-capture SEBELUM core karena hard-delete cascade men-detach entity; force-kick irrelevan untuk Completed/not-started.
- **NoopHubContext stub** — pilihan ini (vs null-guard di produksi) menjaga kode produksi bersih; hub-null di prod tetap salah-konfigurasi yang harus gagal keras.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `.Include(s => s.User)` memutus read-path test 411-02 (InMemory nav-null filter)**
- **Found during:** Task 3 (verifikasi suite — 17 test FlexibleParticipant/Monitoring merah)
- **Issue:** Plan menginstruksikan menambah `.Include(s => s.User)` pada load session Remove/Restore agar payload broadcast punya FullName/NIP. Namun EF Core InMemory provider mem-filter baris ketika navigation Include tak punya entity tertaut (User tak ter-seed di read-path test) → `FirstOrDefaultAsync` return null → endpoint return `NotFound` alih-alih jalur yang diharapkan. Diverifikasi: baseline controller (tanpa Include) → test PASS; controller +Include → test FAIL `NotFound`.
- **Fix:** Hapus `.Include(s => s.User)` pada load session+partner di RemoveParticipantLive & RestoreParticipantLive. Ganti dengan resolve FullName/NIP via lookup `_context.Users` ke dictionary (`TryGetValue`, aman saat User tak ter-seed → string kosong). Hasil broadcast identik di runtime SQL Server.
- **Files modified:** Controllers/AssessmentAdminController.cs
- **Verification:** Read-path InMemory test 411-02 (Proton/AlreadyRemoved/RestoreNotRemoved) kembali hijau.
- **Committed in:** `8623e68e`

**2. [Rule 3 - Blocking] Write-path test NRE pada `_hubContext` null setelah endpoint mulai broadcast**
- **Found during:** Task 3 (verifikasi suite — 14 write-path test NRE @ broadcast call)
- **Issue:** Harness write-path 410-02/411-02 passing `hubContext: null!` (endpoint lama men-defer broadcast → null aman). Setelah Task 1 menambah broadcast post-commit, `_hubContext.Clients.Group(...)` melempar NullReferenceException → test tak bisa menembus jalur baru.
- **Fix:** Buat `NoopHubContext` (IHubContext<AssessmentHub> no-op: Clients.Group/User/All → proxy yang menelan SendAsync; Groups no-op). Wire ke 2 factory write-path (`MakeLiveController` di AddLive + Remove tests). Kontrak JSON & semua assertion DB tetap utuh — hanya melengkapi DI harness agar endpoint asli bisa run end-to-end termasuk broadcast.
- **Files modified:** HcPortal.Tests/NoopHubContext.cs (new), HcPortal.Tests/FlexibleParticipantAddLiveTests.cs, HcPortal.Tests/FlexibleParticipantRemoveTests.cs
- **Verification:** Write-path SQLEXPRESS test hijau; full fast-suite 597/597.
- **Committed in:** `8623e68e`

---

**Total deviations:** 2 auto-fixed (1 bug, 1 blocking)
**Impact on plan:** Kedua deviasi diperlukan agar gate no-regression terpenuhi. Deviasi #1 mengoreksi instruksi plan yang tak kompatibel dengan provider InMemory test (perilaku broadcast runtime SQL Server tak terpengaruh — hasil identik). Deviasi #2 melengkapi DI test untuk jalur broadcast baru. Tidak ada scope creep; kontrak JSON endpoint 410/411 tetap.

## Issues Encountered
- **Git stash conflict tak terkait (`docs/pcp-HCPortal-2026/Risalah Web.pptx`)** — sisa pop stash parsial memunculkan unmerged path "deleted by us" yang memblok commit Task 3. Diselesaikan non-destruktif: `git rm --cached` untuk membersihkan entry index conflict (file tetap di disk sebagai untracked, tidak masuk commit 412). Tidak ada `git clean`/`git reset --hard`/`git checkout .` dijalankan.

## User Setup Required
None - no external service configuration required. migration=FALSE (kolom RemovedAt/RemovedBy/RemovalReason sudah dari Phase 409).

## Next Phase Readiness
- **Plan 412-02** (view panel + modal + DOM handlers) siap: dapat meng-handle event `participantAdded`/`participantRemoved` (dengan `mode`) + me-render panel "Peserta Dikeluarkan" dari `ViewBag.RemovedSessions` (typed RemovedParticipantViewModel). Carry T-409-10: encode-at-render `RemovalReason`/`FullName` via Razor `@`/JS `textContent`.
- **Plan 412-03** (StartExam force-kick) siap: worker menerima `examRemoved` (reason) → mirror handler `examClosed`.
- App boot @5277 verified (monitoring action 302 auth-challenge, bukan 500). Broadcast runtime SignalR belum diuji live (defer Playwright 413 per CONTEXT).
- migration=FALSE; branch main; NOT pushed (deploy bundle v32.5 oleh IT — carry migration=TRUE Phase 409 hash `01cd7dd0`).

## Self-Check: PASSED

All claimed files exist and all task commits are present in git history.

- Files: Controllers/AssessmentAdminController.cs, Models/AssessmentMonitoringViewModel.cs, HcPortal.Tests/NoopHubContext.cs, HcPortal.Tests/FlexibleParticipantAddLiveTests.cs, HcPortal.Tests/FlexibleParticipantRemoveTests.cs, .planning/phases/412-live-monitoring-ui-signalr/412-01-SUMMARY.md — FOUND
- Commits: da1e62e5, 6d3f0e26, 8623e68e — FOUND
- Build: 0 error; Suite: full 597/597, FlexibleParticipant+Monitoring 38/38; App boot @5277 (monitoring 302); migration=FALSE

---
*Phase: 412-live-monitoring-ui-signalr*
*Completed: 2026-06-21*
