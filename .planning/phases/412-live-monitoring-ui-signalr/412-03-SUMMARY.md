---
phase: 412-live-monitoring-ui-signalr
plan: 03
subsystem: ui
tags: [signalr, vanilla-js, bootstrap-modal, razor, force-kick, worker-ui]

# Dependency graph
requires:
  - phase: 412-01-signalr-broadcast
    provides: "Server kirim examRemoved {reason} via Clients.User(session.UserId) HANYA bila wasInProgress (StartedAt!=null && CompletedAt==null && RemovedAt==null), post-commit RemoveParticipantLive"
provides:
  - "Handler client examRemoved di StartExam.cshtml (mirror examClosed): kunci UI ujian (timer/save stop, onbeforeunload clear) + modal non-dismissable #examRemovedModal + redirect ke daftar Assessment (D-02)"
  - "Modal #examRemovedModal (force-kick worker, banner verbatim 'Anda telah dikeluarkan dari ujian ini.', countdown 5 detik, reason XSS-safe via textContent)"
affects: [413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Force-kick worker handler mirror examClosed: reuse var examClosed sebagai dual-trigger guard (JANGAN deklarasi ulang); clearInterval(timerInterval/saveInterval) + window.onbeforeunload=null; bootstrap.Modal non-dismissable; countdown 5s -> redirect"
    - "reason dari admin di-render via .textContent (XSS-safe, T-412-14) — BUKAN innerHTML"
    - "redirect force-kick ke @Url.Action('Assessment','CMP') (daftar ujian) — BEDA dari examClosed yang ke Results/SESSION_ID (D-02)"

key-files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "Mirror examClosed VERBATIM (struktur + guard) tapi 2 perbedaan disengaja: (1) redirect ke daftar Assessment bukan Results (D-02 force-kick beda dari ujian-diakhiri), (2) tampilkan payload.reason di modal bila ada"
  - "Reuse var examClosed (:1249) sebagai guard idempotent (T-412-16) — sesi force-kick + examClosed dual-fire aman, modal pertama menang"
  - "Modal #examRemovedModal sisip setelah #sessionResetModal; handler examRemoved sisip antara handler examClosed dan sessionReset (titik insert sesuai plan/interfaces)"

patterns-established:
  - "Worker force-kick UI = mirror pola lock-UI existing (examClosed/sessionReset): non-dismissable modal + lepas timer/save/onbeforeunload + countdown redirect"

requirements-completed: [PRMV-02]

# Metrics
duration: 10min
completed: 2026-06-21
---

# Phase 412 Plan 03: StartExam Force-Kick examRemoved Handler + Modal Summary

**Handler client `examRemoved` (mirror `examClosed`) + modal non-dismissable `#examRemovedModal` di `StartExam.cshtml` — worker yang sedang InProgress menerima force-kick dari Admin/HC: UI ujian terkunci (timer/save stop, onbeforeunload clear) + modal "Anda telah dikeluarkan dari ujian ini." + redirect ke daftar Assessment setelah countdown 5 detik (PRMV-02 sisi-worker, D-02)**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-21T08:28:24Z
- **Completed:** 2026-06-21T08:38:23Z
- **Tasks:** 2
- **Files modified:** 1 (StartExam.cshtml)

## Accomplishments
- Modal `#examRemovedModal` (non-dismissable `data-bs-backdrop="static" data-bs-keyboard="false"`, `z-index:9999`) disisipkan setelah `#sessionResetModal` — header `bg-danger text-white` + ikon `bi-person-x` + judul "Dikeluarkan dari Ujian", body banner verbatim "Anda telah dikeluarkan dari ujian ini." + baris alasan (`#examRemovedReasonText`/`#examRemovedReasonValue`, hidden default) + countdown `#removedCountdown`, footer tombol `#btnExamRemovedKembali` "Kembali ke Daftar Ujian".
- Handler `examRemoved` (mirror `examClosed` verbatim) disisipkan antara handler `examClosed` dan `sessionReset`: reuse var `examClosed` sebagai dual-trigger guard, `clearInterval(timerInterval/saveInterval)`, `window.onbeforeunload = null`, tampilkan `payload.reason` via `.textContent` (XSS-safe) bila ada, `bootstrap.Modal(...).show()`, redirect ke `@Url.Action("Assessment","CMP")` (daftar — BUKAN Results) via tombol + countdown 5 detik.
- Build 0 error. Runtime smoke @5277 (Playwright) membuktikan handler ter-register tanpa JS error + modal di DOM + Razor `@Url.Action` ter-kompilasi (lesson Phase 354: build+grep tak cukup untuk Razor/JS dinamis).

## Task Commits

1. **Task 1: Modal #examRemovedModal + handler examRemoved (mirror examClosed)** - `50dbef64` (feat)
2. **Task 2: Build + runtime smoke @5277** - tidak ada commit kode (verifikasi only; smoke spec scratch dihapus pasca-run, lihat di bawah)

## Files Created/Modified
- `Views/CMP/StartExam.cshtml` - +modal `#examRemovedModal` (setelah `#sessionResetModal`) +handler `examRemoved` (antara `examClosed` dan `sessionReset`). 66 insertions, 1 file changed.

## Decisions Made
- **Mirror `examClosed` dengan 2 perbedaan disengaja** — (1) redirect ke `@Url.Action("Assessment","CMP")` (daftar ujian) bukan `Results/SESSION_ID` karena force-kick = peserta dikeluarkan, bukan ujian selesai (D-02); (2) tampilkan `payload.reason` dari admin di modal (`#examRemovedReasonText` di-unhide bila reason ada).
- **Reuse var `examClosed` (:1249) sebagai guard** — idempotent (T-412-16); bila `examClosed` SignalR + `examRemoved` dual-fire, modal pertama menang, tak ada double-redirect/double-modal. JANGAN deklarasi ulang `var examClosed` (verified grep == 1).
- **reason via `.textContent`** (T-412-14, Pitfall XSS) — `RemovalReason` bebas-teks dari admin di-encode di klien; BUKAN `innerHTML`.

## Deviations from Plan

None - plan executed exactly as written. Markup modal + handler JS mengikuti 412-UI-SPEC §9 verbatim; titik insert sesuai `<interfaces>` plan (modal setelah `#sessionResetModal`, handler antara `examClosed` :1284 dan `sessionReset` :1287).

## Verification Results

**Build:** `dotnet build HcPortal.csproj` → Build succeeded, 0 Error(s).

**grep acceptance criteria (semua PASS):**
- `examRemovedModal` = 2 (≥2: modal id + handler getElementById) ✓
- `on('examRemoved'` = 1 (==1) ✓
- `Anda telah dikeluarkan dari ujian ini` = 1 (≥1, verbatim) ✓
- `Url.Action("Assessment", "CMP")` = 2 (≥1: handler examRemoved + resetKembaliBtn existing) ✓
- `examRemovedReasonValue` = 2 (≥2: modal `<em id>` :398 + `.textContent` set :1328) — XSS-safe via `.textContent`, BUKAN innerHTML ✓
- `var examClosed = false` = 1 (==1, tidak diduplikasi) ✓
- `data-bs-backdrop="static"` = 6 (≥2: examClosed + examRemoved + sessionReset + timeUpWarning + lainnya) ✓

**Runtime smoke @5277 (Playwright, AD-off):** PASSED 1/1 (2.2s).
- Login admin@pertamina.com → load `/CMP/StartExam/171` (sesi InProgress dengan paket soal).
- `#examRemovedModal` count == 1 di DOM; `#examRemovedReasonValue` + `#removedCountdown` hadir; modal berisi banner verbatim "Anda telah dikeluarkan dari ujian ini." ✓
- `window.assessmentHub` defined (SignalR wired) — handler `examRemoved` ter-register tanpa JS error / pageerror ✓
- HTML mengandung `examRemoved` dan TIDAK mengandung literal `@Url.Action` (Razor ter-kompilasi) ✓
- NOL JS console error saat load StartExam (var `examClosed` tak konflik) ✓
- **CATATAN:** e2e force-kick LENGKAP 2-context (admin hapus peserta InProgress → worker terima modal + redirect live) = Phase 413. Smoke ini hanya buktikan handler+modal+kompilasi-Razor ter-load tanpa error.

**migration=FALSE:** `git status Migrations/ Data/` kosong (kolom RemovedAt/RemovedBy/RemovalReason sudah dari Phase 409). ✓

## Runtime Smoke Setup (Seed Workflow — non-destruktif, cleaned)
Sesi InProgress existing semua ber-jendela-ujian EXPIRED; sesi 150 (admin) tak punya paket soal ("Sesi ujian ini tidak memiliki paket soal"). Untuk merender StartExam: BACKUP `HcPortalDB_Dev` → UPDATE in-place sesi 171 (Completed→InProgress, StartedAt=GETDATE(), CompletedAt=NULL, ExamWindowCloseDate=+1hari, UPA.IsCompleted=0; sesi 171 punya paket soal) → smoke PASS → RESTORE snapshot (baseline verified: 150 StartedAt=2026-05-11, 171 Status=Completed CompletedAt=2026-06-15). Dicatat di `docs/SEED_JOURNAL.md` (status `cleaned`). Snapshot `.bak` + smoke spec scratch (`tests/e2e/examremoved-smoke-412-03.spec.ts`) + temp config dihapus pasca-run — TIDAK menjadi deliverable (Phase 413 memiliki e2e force-kick lengkap). Tidak ada `git clean`/`git reset --hard` dijalankan.

## User Setup Required
None - migration=FALSE, tidak ada konfigurasi service eksternal. Local only — TIDAK di-push (deploy bundle v32.5 oleh IT; carry migration=TRUE Phase 409 hash `01cd7dd0`). Dev/Prod TIDAK disentuh.

## Next Phase Readiness
- **Plan 413 (Test + UAT)** siap: e2e Playwright multi-context force-kick (admin `RemoveParticipantLive` peserta InProgress → worker StartExam terima `examRemoved` → modal `#examRemovedModal` muncul + UI terkunci + redirect ke `/CMP/Assessment`). Handler + modal sudah live di StartExam.cshtml.
- Banner D-02 (TempData["Error"] = "Anda telah dikeluarkan dari ujian ini." di halaman /CMP/Assessment) — guard re-entry server 409 (`IsParticipantRemoved` :924-928) sudah set TempData identik saat worker mencoba re-enter StartExam sesi soft-removed; verifikasi render banner penuh = Phase 413 (best-effort, tidak menambah scope view Assessment di fase ini).

## Self-Check: PASSED

- Files: Views/CMP/StartExam.cshtml, .planning/phases/412-live-monitoring-ui-signalr/412-03-SUMMARY.md — FOUND
- Commit: 50dbef64 — FOUND in git history
- Build: 0 error; grep acceptance 7/7 PASS; runtime smoke @5277 1/1 PASS; migration=FALSE

---
*Phase: 412-live-monitoring-ui-signalr*
*Completed: 2026-06-21*
