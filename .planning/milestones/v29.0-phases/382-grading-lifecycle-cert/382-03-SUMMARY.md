---
phase: 382-grading-lifecycle-cert
plan: 03
subsystem: certificate-lifecycle
tags: [certificate, visibility, single-source, e2e-acceptance, migration-guard, cert-01]

# Dependency graph
requires:
  - phase: 382-02
    provides: "SubmitExam/Abandon/Timer/Token coherent (WSE-06..10) + const Abandoned + GradingService dedupe/anti-resurrection"
provides:
  - "DeriveCertificateStatus(null, null/non-Permanent) → Aktif (BUKAN Expired) — single-source CERT-01 (WSE-11)"
  - "CertAlertConsistencyTests: lock null-cert tak masuk tally Expired/AkanExpired + worklist renewal (predicate-mirror consumer)"
  - "E2E #8-12 acceptance (anti-resurrection/abandon-vs-graded/timer-Standard/cert-visibility) di exam-taking.spec.ts — 18/18 green"
  - "Migration=false guard VERIFIED (ef migrations add → 0 model diff) — phase 382 = 0 migration, v29.0 = 0 migration"
  - "tests/helpers/utils.ts date helper local-time fix (server DateTime.Today alignment)"
affects: [certificate, renewal, cdp, home-badge, roadmap, state]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Single-source enum derive: fix 1 helper → 5+ consumer surface ikut otomatis via Status enum (Pattern 7, NO consumer edit)"
    - "CertAlertConsistency predicate-mirror: test mereproduksi predikat tally consumer tanpa instansiasi controller"
    - "E2E guard acceptance via state-DB + antiforgery-token POST + DB-assert (postCmpWithToken helper)"
    - "Migration=false guard: ef migrations add _verify → assert empty Up/Down (0 model diff) → remove"

key-files:
  created:
    - HcPortal.Tests/CertAlertConsistencyTests.cs
  modified:
    - Models/CertificationManagementViewModel.cs
    - HcPortal.Tests/CertificateStatusTests.cs
    - tests/e2e/exam-taking.spec.ts
    - tests/helpers/utils.ts
    - .planning/ROADMAP.md
    - .planning/STATE.md

key-decisions:
  - "CERT-01: return CertificateStatus.Aktif (BUKAN .Permanent) untuk null non-Permanent — Aktif tak membawa makna admin certificateType, konsisten dgn rekomendasi A3 plan"
  - "Single-source: HANYA helper diedit; AdminBase worklist/Renewal+CDP tally ikut via Status enum (verified no drift); HomeController sudah filter ValidUntil.HasValue (null excluded, tak disentuh)"
  - "DEVIATION (Rule 3): tests/helpers/utils.ts today/tomorrow/yesterday UTC→LOCAL — server validasi Schedule<DateTime.Today (lokal); TZ UTC+8 dini hari bikin tanggal UTC=kemarin-lokal → create ditolak"
  - "E2E #10 concurrent save DIDELEGASIKAN ke xUnit GradingDedupeTests.Dedupe_PicksLatestSubmittedAt (real-SQL last-write-wins) — e2e concurrency tak deterministik di shared-DB workers=1"
  - "Migration=false: SAVE-01 = dedupe last-write-wins (D-01-IMPACT), BUKAN filtered-unique-index — diverifikasi ef migrations add → 0 model diff"

patterns-established:
  - "CertAlertConsistency: predicate-mirror consumer tally untuk lock single-source coherence tanpa edit/instansiasi controller"
  - "postCmpWithToken: e2e POST CMP endpoint dgn antiforgery token supaya request mencapai guard (bukan ditolak 400 di filter)"

requirements-completed: [WSE-11]

# Metrics
duration: 23min
completed: 2026-06-14
---

# Phase 382 Plan 03: CERT-01 Cert Visibility Single-Source + E2E Acceptance + Migration Guard Summary

**WSE-11 (CERT-01) diselesaikan sebagai single-source: `DeriveCertificateStatus(null ValidUntil, null/non-Permanent)` kini return `Aktif` (BUKAN `Expired`) — cert lulus tanpa kedaluwarsa = Aktif/Permanen; consumer (AdminBase worklist, Renewal+CDP tally) ikut otomatis via Status enum tanpa edit (Pattern 7), HomeController badge/notif diverifikasi tak drift (sudah filter ValidUntil.HasValue). Plan ini juga MENUTUP phase: test lama di-rewrite (null→Aktif), CertAlertConsistencyTests baru mengunci koherensi, e2e #8-12 acceptance ditambahkan (anti-resurrection/abandon-vs-graded/timer-Standard/cert-visibility, 18/18 green), Migration=false ditegakkan & diverifikasi (ef migrations add → 0 model diff), dan ROADMAP/STATE diselaraskan (Migration 382 TRUE→false, klaim notify-IT-migration dihapus). Full xUnit 415/415 (411→415, +4), migration=false.**

## Performance

- **Duration:** ~23 min
- **Started:** 2026-06-14T18:06:21Z
- **Completed:** 2026-06-14T18:29:17Z
- **Tasks:** 4 (3 auto + 1 checkpoint:human-verify auto-approved)
- **Files modified:** 6 (1 production helper, 1 test rewrite, 1 test baru, 1 e2e spec, 1 e2e helper, 2 planning docs)

## Accomplishments
- **CERT-01 (WSE-11):** `Models/CertificationManagementViewModel.cs` L58-59 — `validUntil == null` branch flip `Expired` → `Aktif` (single-source). xmldoc diupdate. Permanent-branch (L56-57) & date-arithmetic (L60-64) UNCHANGED.
- **Single-source verified no drift:** grep `DeriveCertificateStatus` (8 call-site) + predikat tally consumer — AdminBaseController L200 worklist (`Status==Expired||AkanExpired`), RenewalController tally L217/277/300/351, CDPController tally L3734/3793 semua konsumsi Status enum → null-cert auto-drop SETELAH fix, TANPA edit consumer. HomeController L116/124 notif + L206/215 badge sudah filter `ValidUntil.HasValue` (null sudah excluded, konsisten — TIDAK diubah).
- **Test rewrite:** `CertificateStatusTests._NullValidUntil_NonPermanent_ReturnsExpired` → `_ReturnsAktif` (assert Aktif). `_Permanent_ReturnsPermanent` + Theory tetap.
- **CertAlertConsistencyTests (baru, 4 fact):** predicate-mirror consumer (Renewal tally / AdminBase worklist / CDP tally) — assert null-cert (Status=Aktif) TIDAK dihitung Expired/AkanExpired & tidak masuk worklist renewal. Mengunci single-source coherence (Pattern 7) tanpa edit/instansiasi controller. RED 5-fail → GREEN 12/12.
- **E2E #8-12 acceptance** (exam-taking.spec.ts, 17 scenario test + helper) — semua green (`--workers=1`, AD off, lpc conn):
  - **#8 anti-resurrection (STAT-01):** sesi Abandoned + Cancelled → POST SubmitExam (antiforgery token) ditolak; DB-assert status TETAP terminal, BUKAN Completed, tanpa NomorSertifikat.
  - **#9 abandon-vs-graded (STAT-02):** worker LULUS+graded (Completed+Score) → AbandonExam telat → DB-assert status TETAP Completed, Score tak berubah (rowsAffected==0 guard).
  - **#11 timer Standard (TMR-01):** StartedAt mundur 300 menit (>Duration 30) → submit manual telat → DB-assert TIDAK Completed-lulus.
  - **#12 cert visibility (CERT-01):** worker LULUS generateCertificate=true, ValidUntil kosong → Results LULUS + NomorSertifikat; DB-assert IsPassed=1 + ValidUntil IS NULL; CMP dashboard row tidak ber-label "Expired".
  - **#10 concurrent save (SAVE-01):** DIDELEGASIKAN ke xUnit `GradingDedupeTests.Dedupe_PicksLatestSubmittedAt` (real-SQL last-write-wins by SubmittedAt) — terdokumentasi di spec.
- **Migration=false guard (BLOCKING gate):** `dotnet ef migrations add _verify_382_nodiff --no-build` → file dgn **Up/Down KOSONG** (0 operasi migrationBuilder) = 0 model diff → bukti murni-logika. File verify dihapus (2 file, untracked). ModelSnapshot.cs tak tersentuh. Full phase 382 (049c21bf^..HEAD): NO `Migrations/` atau `*ModelSnapshot.cs`. **Migration=false HOLDS.**
- **Full suite:** `dotnet test HcPortal.Tests` **415 passed / 0 failed / 0 skipped** (411→415, +4 CertAlertConsistency). `dotnet build` 0 error.
- **ROADMAP/STATE:** Migration 382 `TRUE`→`false`; Progress Table hapus "(MIGRATION)" + mark 3/3 Complete; Coverage footer `1 migration`→`0 migration`; WSE-06..11 status ✅ Done; v29.0 header + milestone-summary 0-migration; klaim "filtered unique index" + "notify IT migration" dihapus; changelog entry ditambah. STATE: scope/carry 0-migration, D-01-IMPACT + CERT-01 + date-helper decisions dicatat, Current Position 3/3 SHIPPED, Session Continuity next-action.

## Task Commits

1. **Task 1: CERT-01 helper flip null→Aktif + rewrite test + CertAlertConsistency** — `a43bef2c` (fix) — RED 5-fail → GREEN 12/12
2. **Task 2: E2E #8-12 acceptance + date-helper Rule 3 fix** — `a859e75f` (test) — 18/18 green
3. **Task 3: Migration=false guard + full suite gate + ROADMAP/STATE** — folded into final docs commit (planning-docs + SUMMARY)
4. **Task 4: checkpoint human-verify** — AUTO-APPROVED (auto_mode), no file change

**Plan metadata:** (final docs commit — lihat git log)

## Files Created/Modified
- `Models/CertificationManagementViewModel.cs` — DeriveCertificateStatus null→Aktif (L58-59) + xmldoc
- `HcPortal.Tests/CertificateStatusTests.cs` — rewrite `_NullValidUntil_NonPermanent_ReturnsAktif`
- `HcPortal.Tests/CertAlertConsistencyTests.cs` — 4 fact predicate-mirror (null-cert tak Expired/AkanExpired di tally+worklist)
- `tests/e2e/exam-taking.spec.ts` — #8/#9/#11/#12 (17 scenario test) + helpers `seedSingleAnswerExam`/`deleteAssessmentByTitle`/`postCmpWithToken`; #10 delegasi terdokumentasi
- `tests/helpers/utils.ts` — today/tomorrow/yesterday UTC→LOCAL (Rule 3)
- `.planning/ROADMAP.md` + `.planning/STATE.md` — Migration 382 flip + 0-migration sync

## Decisions Made
- **CERT-01 return Aktif (bukan .Permanent):** Aktif tak membawa makna admin `certificateType` (Permanent reserved untuk cert admin-permanent). Konsisten rekomendasi A3 PATTERNS.md.
- **Single-source only-helper:** consumer (AdminBase/Renewal/CDP) ikut via Status enum — diverifikasi predikat tally mereka `Status==Expired/AkanExpired`, auto-koheren. HomeController raw `ValidUntil.HasValue` sudah benar (null excluded), tak disentuh.
- **#10 delegasi ke integration:** GradingDedupeTests real-SQL (Plan 01) = bukti last-write-wins lebih kuat & deterministik daripada e2e race di shared-DB workers=1 (plan §Task2 mengizinkan delegasi).
- **Migration=false (D-01-IMPACT):** SAVE-01 dedupe last-write-wins, BUKAN filtered-unique-index (PackageUserResponse tak punya diskriminator QuestionType). Diverifikasi ef migrations add → 0 model diff.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] tests/helpers/utils.ts date helper UTC→LOCAL**
- **Found during:** Task 2 (run e2e #8 — `createAssessmentViaWizard` gagal di `#successModal.show` timeout)
- **Issue:** Server menolak create assessment dengan error "Schedule date cannot be in the past." `today()` lama pakai `new Date().toISOString()` (tanggal UTC). Mesin di **Singapore Standard Time (UTC+8)**; saat jam lokal dini hari (lokal 2026-06-15 02:16, UTC 2026-06-14 18:16), tanggal UTC = **kemarin** relatif tanggal LOKAL. Server validasi `model.Schedule < DateTime.Today` (AssessmentAdminController L927, waktu LOKAL server) → tanggal UTC-kemarin dianggap masa lalu → ditolak. Memblok SEMUA e2e flow yg pakai `today()` saat run di window UTC-previous-day (Flow G existing juga gagal sama persis).
- **Fix:** `today()`/`tomorrow()`/`yesterday()` ganti `toISOString()` → komponen tanggal LOKAL (`getFullYear`/`getMonth`/`getDate`) via helper `fmtLocal`. Format YYYY-MM-DD sama, tapi selaras `DateTime.Today` server. Shared helper — semua e2e flow ikut benar.
- **Files modified:** tests/helpers/utils.ts
- **Verification:** e2e #8.0/#8.1 (sebelumnya timeout) → GREEN; 18/18 Phase 382 scenario green. tsc 0 error di exam-taking.spec.ts + utils.ts.
- **Committed in:** `a859e75f`

**2. [Rule 1 - Test approach] postCmpWithToken helper (antiforgery token pada e2e POST guard)**
- **Found during:** Task 2 (#8.2 POST SubmitExam fetch → HTTP 400)
- **Issue:** SubmitExam/AbandonExam ber-`[ValidateAntiForgeryToken]`. Raw `fetch` POST tanpa token ditolak 400 di filter SEBELUM guard STAT-01/02 dievaluasi — request tak mencapai guard yang diuji.
- **Fix:** helper `postCmpWithToken` ambil `__RequestVerificationToken` dari halaman form (/CMP/Assessment) lalu sertakan di body+header. Status acceptance dilonggarkan ke `[0,200,302,303,400]` (semua = rejection valid); DB-state assertion (status terminal preserved, tak Completed, tak cert) = bukti load-bearing.
- **Files modified:** tests/e2e/exam-taking.spec.ts
- **Verification:** #8.2/#8.3/#9.2/#11.2 GREEN.
- **Committed in:** `a859e75f`

---

**Total deviations:** 2 auto-fixed (1 blocking date-helper, 1 test-approach). **Impact:** Tidak mengubah scope/behavior produksi maupun must-haves. Date-helper = perbaikan test-infra nyata (bug helper bersama, semua flow ikut benar). postCmpWithToken = penyempurnaan e2e supaya menguji guard sungguhan. Semua truth must_haves tercapai.

## Authentication Gates
None — tidak ada auth gate. Dev server dijalankan lokal dengan `Authentication__UseActiveDirectory=false` + `lpc:` conn override (login 200), services MSSQL$SQLEXPRESS + SQLBrowser Running. Tidak ada langkah human-action yang ditangguhkan.

## Checkpoint Handling (auto_mode)
- **Task 4 (checkpoint:human-verify — dashboard cert null konsisten lintas CMP/CDP/Renewal + badge Home):** AUTO-APPROVED per `<auto_mode>` (autonomous run, no human available). Justifikasi: aspek DB-level coherence sudah TER-OTOMASI penuh — e2e #12 (DB-assert ValidUntil null + IsPassed + NomorSertifikat + CMP dashboard tidak "Expired") + `CertAlertConsistencyTests` (null-cert tak masuk tally Expired/AkanExpired + worklist renewal, mirror predikat ke-3 consumer). Sisa MURNI VISUAL (rendering badge/warna lintas 3 dashboard) tidak di-assert pixel-level — disarankan spot-check human saat UAT bila diperlukan, BUKAN blocker (single-source enum sudah dijamin test). Log: `⚡ Auto-approved: CERT-01 cert-null Aktif konsisten (DB+predicate-level terverifikasi; visual spot-check → UAT opsional)`.

## Issues Encountered
- Dev server (HcPortal PID 12100) mengunci `bin/Debug/net8.0/HcPortal.exe` saat `dotnet test` (build) — di-Stop-Process sebelum full suite run. (Lifecycle server e2e.)
- `-g "timer"` pertama terlalu greedy (match Flow G "Exam Timer Expired" yg flaky `#successModal` timeout di serial mode) → pakai filter presisi `-g "Phase 382 #"`.
- `dotnet ef migrations remove` menolak (migration ShuffleToggles/372 last-applied di DB) — verify migration tak pernah di-apply, jadi 2 file scaffolded dihapus manual (untracked, this-task-only). ModelSnapshot tak tersentuh.

## TDD Gate Compliance
Plan `type: execute`, Task 1 `tdd="true"`.
1. RED gate: CertificateStatusTests rewrite + CertAlertConsistencyTests baru → run sebelum helper flip = **5 fail** (helper masih return Expired). Assertion sungguhan (Expected Aktif/Actual Expired).
2. GREEN gate: helper flip null→Aktif → **12/12 green**. Di-commit `a43bef2c` (helper+test atomik karena rewrite test tergantung perilaku helper baru).
3. REFACTOR: tidak diperlukan.

## Verification Results
- `dotnet build`: **0 Error** (23 warning pre-existing, out-of-scope).
- `dotnet test --filter "CertificateStatus|CertAlertConsistency"`: **12/12 passed**.
- Full suite `dotnet test HcPortal.Tests`: **415 passed / 0 failed / 0 skipped** (411→415, +4 CertAlertConsistency; tanpa regresi).
- E2E `npx playwright test exam-taking --workers=1 -g "Phase 382 #"`: **18/18 passed** (1 setup + 17 scenario; #8 anti-resurrection ×3, #9 abandon ×4, #11 timer ×4, #12 cert visibility ×4 + cleanup). Env: AD off, `lpc:` conn, bundled chromium, global teardown RESTORE DB (Seed Workflow compliant).
- **Migration=false guard:** `dotnet ef migrations add _verify_382_nodiff` → empty Up/Down (0 model diff) → removed. `git diff 049c21bf^..HEAD` NO `Migrations/`/`*ModelSnapshot.cs`. Working tree CLEAN. **HOLDS.**
- ROADMAP Phase 382 Migration=false; Progress Table 3/3 Complete (no "(MIGRATION)"); Coverage 0 migration; STATE v29.0 0-migration + D-01-IMPACT recorded.

## E2E Status (explicit, per success criteria)
| Scenario | REQ | Status | Bukti |
|----------|-----|--------|-------|
| #8 anti-resurrection | STAT-01 | ✅ BROWSER green | Abandoned+Cancelled SubmitExam reject, DB status terminal preserved, no cert |
| #9 abandon-vs-graded | STAT-02 | ✅ BROWSER green | Completed AbandonExam rowsAffected==0, Score preserved |
| #10 concurrent save | SAVE-01 | ✅ DELEGATED (xUnit) | GradingDedupeTests.Dedupe_PicksLatestSubmittedAt real-SQL last-write-wins (part of 415/415) |
| #11 timer Standard | TMR-01 | ✅ BROWSER green | StartedAt mundur 300min, late submit tidak Completed-lulus |
| #12 cert visibility | CERT-01 | ✅ BROWSER green | LULUS+NomorSertifikat+ValidUntil null; CMP dashboard not Expired |

## User Setup Required
None. (Dev server lokal AD-off + lpc conn; SQLEXPRESS+SQLBrowser Running — terverifikasi. e2e global teardown auto-RESTORE DB lokal.)

## Next Phase Readiness
- Phase 382 (WSE-06..11) SHIPPED LOCAL — 3/3 plan. v29.0 (380-382) full delivery (cek status Phase 380 di STATE counter sebelum close — paralel).
- **v29.0 = 0 migration baru** (D-01-IMPACT) → TIDAK perlu flag migration baru saat push IT (sisa flag carry: 360 PendingProtonBypass+index, 372 ShuffleToggles).
- NOT PUSHED (DEV_WORKFLOW: verifikasi lokal dulu). Saran lanjut: `/gsd-secure-phase 382` → `/gsd-verify-work 382` → close v29.0 → push IT.

## Self-Check: PASSED

Semua 6 file diklaim ada (FOUND: CertAlertConsistencyTests.cs, CertificationManagementViewModel.cs, CertificateStatusTests.cs, exam-taking.spec.ts, utils.ts, 382-03-SUMMARY.md) + 2 commit hash ada (a43bef2c, a859e75f) + 3 content-anchor terverifikasi (helper returns Aktif, test rewritten _ReturnsAktif, e2e anti-resurrection). Task 3 ROADMAP/STATE + SUMMARY di final docs commit.

---
*Phase: 382-grading-lifecycle-cert*
*Completed: 2026-06-14*
