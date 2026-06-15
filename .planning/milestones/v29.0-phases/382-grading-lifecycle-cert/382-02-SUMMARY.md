---
phase: 382-grading-lifecycle-cert
plan: 02
subsystem: assessment-lifecycle
tags: [submit-exam, abandon, timer, token-gate, race-safety, coherent-single-stream, pure-helper]

# Dependency graph
requires:
  - phase: 382-01
    provides: "AssessmentConstants.AssessmentStatus.Abandoned const + GradingService dedupe/anti-resurrection (KEDUA branch)"
provides:
  - "SubmitExam coherent: SAVE-01 final-write-wins OrderBy (push==stored Score) + STAT-01 terminal-set guard+audit + TOK-02 StartedAt-gate + TMR-02 server-timer-authority incomplete-gate"
  - "SaveAnswer TOK-02 StartedAt-gate (Json reject)"
  - "AbandonExam STAT-02 atomic guarded ExecuteUpdate (ownership in WHERE, rowsAffected==0 reject) — TOCTOU dihapus"
  - "EnsureCanSubmitExamAsync TMR-01 blocklist (skip hanya Manual/null, Standard di-enforce) + TMR-03 token-defer-to-grading-success"
  - "4 pure static decision helper CMPController (ShouldEnforceSubmitTimer, EvaluateSubmitTimerDecision, ShouldGateMissingStart, ShouldConsumeAutoSubmitToken) — single-source, ter-uji unit"
  - "4 file test xUnit baru (AbandonGuard real-SQL, EnsureCanSubmitStandard, AutoSubmitTokenRetry, TokenGate)"
affects: [382-03, CMPController, submit-exam, abandon, certificate, lifecycle]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pure static decision helper di controller ber-ctor 14-dep (uji via helper, bukan instansiasi controller) — pola VerifyTokenTests/Phase 380/363"
    - "Atomic guarded ExecuteUpdateAsync WHERE (UserId && status-set) + rowsAffected==0 reject (anti-TOCTOU + anti-spoof)"
    - "Token one-shot: validate (peek/TempData.Keep) di guard, consume (TempData.Remove) HANYA di success path pasca-grading"
    - "Final-write-wins dedupe: GroupBy(...).OrderByDescending(SubmittedAt).First() agar push Score == GradingService Score"

key-files:
  created:
    - HcPortal.Tests/AbandonGuardTests.cs
    - HcPortal.Tests/EnsureCanSubmitStandardTests.cs
    - HcPortal.Tests/AutoSubmitTokenRetryTests.cs
    - HcPortal.Tests/TokenGateTests.cs
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "DEVIATION (Rule 3): logika keputusan di-extract ke 4 PURE STATIC HELPER (anti-drift), bukan uji controller langsung — CMPController ber-ctor 14-dep (konvensi repo VerifyTokenTests.cs:3 'Controller construction is infeasible')"
  - "DEVIATION (Rule 3): AbandonGuardTests pakai real-SQL disposable fixture (Category=Integration), bukan InMemory — ExecuteUpdateAsync tak didukung EF8 InMemory + race/atomic guard hanya terbukti di SQL nyata"
  - "TMR-02: gate incomplete pakai serverTimerExpired sebagai SATU-SATUNYA otoritas bypass (client isAutoSubmit dihapus dari kondisi gate); submit on-time lengkap tetap lolos (answeredCount==total tak masuk branch)"
  - "TMR-03: EnsureCanSubmitExamAsync pakai TempData.Keep (peek) — token dikonsumsi via TempData.Remove di SubmitExam HANYA setelah GradeAndCompleteAsync sukses"

patterns-established:
  - "CMPController pure decision helpers: ShouldEnforceSubmitTimer / EvaluateSubmitTimerDecision / ShouldGateMissingStart / ShouldConsumeAutoSubmitToken"

requirements-completed: [WSE-06, WSE-07, WSE-08, WSE-09, WSE-10]

# Metrics
duration: 16min
completed: 2026-06-14
---

# Phase 382 Plan 02: SubmitExam/Abandon/Timer/Token Coherent Single-Stream Summary

**Seluruh mutasi sisi `CMPController.cs` untuk WSE-06..10 dikerjakan dalam SATU plan koheren (urutan commit mandated SAVE-01→STAT-01→STAT-02→TMR-01/03→TOK-02→TMR-02): SubmitExam kini baca jawaban FINAL (push==stored Score), menolak resurrection sesi terminal + audit, mempercayai server-timer untuk gate incomplete, dan men-gate sesi token belum-mulai; AbandonExam jadi single atomic guarded UPDATE (ownership di WHERE); timer "Standard" ditegakkan; token auto-submit dikonsumsi hanya pasca-grading — semua keputusan dipusatkan di 4 pure helper ter-uji, full xUnit 411/411, migration=false.**

## Performance

- **Duration:** ~16 min
- **Tasks:** 5 (Task 1 TDD RED → Task 2-5 GREEN per region)
- **Files modified:** 5 (1 production `CMPController.cs`, 4 test baru)

## Accomplishments
- **SAVE-01 (WSE-06):** SubmitExam GroupBy dedupe pakai `OrderByDescending(r => r.SubmittedAt).First()` (SATU site) → baris FINAL per soal, push Score (SignalR) == Score GradingService (tak divergen saat race multi-tab).
- **STAT-01 (WSE-07):** early guard SubmitExam diperluas dari `== "Completed"` ke `{Completed, Abandoned, Cancelled, PendingGrading}` + audit `SubmitExamBlocked` (reuse `WriteSubmitBlockedAuditAsync`, try/catch-swallow) + pesan BI + redirect. Resurrection (Abandoned/Cancelled POST→Completed-lulus+cert) tertutup.
- **STAT-02 (WSE-08):** AbandonExam TOCTOU (read-check status → change-tracker → SaveChanges) DIGANTI single atomic `ExecuteUpdateAsync` WHERE `Id && UserId==owner && (InProgress||Open)`; `rowsAffected==0 → reject`. Verdict graded tak ter-overwrite oleh late abandon; spoof sesi orang lain diblok di WHERE (ownership dipertahankan, Pitfall 2). `Forbid()` early dipertahankan untuk 403 semantics.
- **TMR-01 (WSE-09):** `EnsureCanSubmitExamAsync` allowlist (Online/PreTest/PostTest) DIBALIK ke blocklist via `ShouldEnforceSubmitTimer` — skip HANYA Manual/null/kosong; "Standard" (AssessmentType Normal, string literal di data) kini di-enforce (sebelumnya dead-code skip).
- **TMR-03 (WSE-09):** `TempData.Remove(tempKey)` pre-grading DIHAPUS dari guard → diganti `TempData.Keep` (peek); konsumsi token (`TempData.Remove`) dipindah ke SubmitExam success path SETELAH `GradeAndCompleteAsync` sukses (`ShouldConsumeAutoSubmitToken(graded)`). Retry pasca-DB-hiccup tak permanent-reject. D-05 dijaga: on-time auto-submit (server-approved) tetap lolos ke grading.
- **TOK-02 (WSE-10):** gate `ShouldGateMissingStart(IsTokenRequired, StartedAt)` di SaveAnswer (return Json `success=false`) + SubmitExam (return `RedirectToAction("Assessment")`), setelah owner/lifecycle check, sebelum mutasi. Sesi token-required && StartedAt==null = belum lewat lobby token → reject. Non-token tak ter-gate.
- **TMR-02 (WSE-07 half):** gate incomplete-submit `if (!isAutoSubmit && !serverTimerExpired)` → `if (!serverTimerExpired)`. `serverTimerExpired` (server-computed) jadi SATU-SATUNYA otoritas bypass; client `isAutoSubmit` mentah (bisa di-spoof DevTools) tak lagi cukup untuk lolos submit incomplete. Submit on-time lengkap tetap lolos (tak masuk branch incomplete).
- **4 pure helper** dipusatkan di CMPController (anti-drift, ter-uji unit) + **4 file test** (16 fact baru). Full suite **411/411** (dari 395/395 Plan 01), **migration=false** dikonfirmasi (0 file Migrations/snapshot tersentuh di 5 commit).

## Task Commits

Urutan commit mengikuti mandate plan (single-stream, no reorder):

1. **Task 1: Wave 0 RED — 4 test + helper stub** — `e0b47e7b` (test)
2. **Task 2: SAVE-01 OrderBy + STAT-01 guard + TMR-02** — `f733a20e` (fix)
3. **Task 3: STAT-02 AbandonExam atomic ExecuteUpdate** — `ab86acba` (fix)
4. **Task 4: TMR-01 inversion + TMR-03 token-defer** — `398eeaec` (fix)
5. **Task 5: TOK-02 StartedAt-gate SaveAnswer+SubmitExam** — `86d9a9f5` (fix)

**Plan metadata:** (final docs commit — lihat git log)

## Files Created/Modified
- `Controllers/CMPController.cs` — 4 pure helper baru + SubmitExam (SAVE-01 OrderBy, STAT-01 guard+audit, TOK-02 gate, TMR-02 server-timer-authority, TMR-03 token-consume success-path) + SaveAnswer (TOK-02 gate) + AbandonExam (STAT-02 atomic ExecuteUpdate) + EnsureCanSubmitExamAsync (TMR-01 blocklist, TMR-03 Keep) + `using S` alias
- `HcPortal.Tests/AbandonGuardTests.cs` — real-SQL fixture (disposable GUID DB, Category=Integration): Completed→0 rows + non-owner→0 rows + owner-InProgress→1 row Abandoned
- `HcPortal.Tests/EnsureCanSubmitStandardTests.cs` — `ShouldEnforceSubmitTimer` (Standard/Online/PrePost enforce; Manual/null skip) + `EvaluateSubmitTimerDecision` (tier-1/tier-2/pass + D-05 server-approved)
- `HcPortal.Tests/AutoSubmitTokenRetryTests.cs` — `ShouldConsumeAutoSubmitToken` (fail→not consumed; success→consumed)
- `HcPortal.Tests/TokenGateTests.cs` — `ShouldGateMissingStart` (token-required+not-started→gated; started/non-token→not gated)

## Decisions Made
- **Pure helper extraction** (vs uji controller langsung) — konvensi repo eksplisit: CMPController ber-ctor 14-dependency, "Controller construction is infeasible" (`VerifyTokenTests.cs:3`). Helper = sumber kebenaran tunggal; controller mendelegasikan → anti-drift, mirror Phase 380 (`AccessTokenMatches`) / 363 / 365 / 366 shared-core discipline.
- **TMR-02 server-timer-authority** — gate incomplete pakai `serverTimerExpired` sebagai satu-satunya bypass; client `isAutoSubmit` dihapus dari kondisi gate (bukan dari signature — masih dipakai EnsureCanSubmitExamAsync). Submit on-time lengkap tetap lolos.
- **TMR-03 peek-then-consume-on-success** — `TempData.Keep` di guard (validate tanpa remove); `TempData.Remove` di SubmitExam HANYA setelah grading commit sukses. Pilih pendekatan eksplisit (vs andalkan idempotency) agar jelas + ter-uji kontraknya.
- **STAT-02 ownership in WHERE + early Forbid** — kombinasi: `Forbid()` early (403 semantics untuk non-owner) + `UserId` di WHERE ExecuteUpdate (atomicity & anti-race). Const `S.Abandoned` (Plan 01) dipakai, bukan literal.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Logika keputusan di-extract ke 4 pure static helper (bukan uji controller langsung)**
- **Found during:** Task 1 (penulisan Wave-0 facts)
- **Issue:** Plan menyarankan menguji `EnsureCanSubmitExamAsync` (private) + gate SaveAnswer/SubmitExam via reflection / TestServer / instansiasi controller. CMPController ber-ctor 14-dependency (UserManager, SignInManager, HubContext, ScopeFactory, dst.) + handler baca TempData/HttpContext/return IActionResult → instansiasi controller untuk unit test rapuh & melawan konvensi repo eksplisit (`VerifyTokenTests.cs:3` "Controller construction is infeasible (14-dep ctor)").
- **Fix:** Extract 4 pure static helper (`ShouldEnforceSubmitTimer`, `EvaluateSubmitTimerDecision`, `ShouldGateMissingStart`, `ShouldConsumeAutoSubmitToken`); controller mendelegasikan ke helper. Test menguji helper (pola Phase 380 `AccessTokenMatches`). Tidak mengubah behavior produksi — hanya memindahkan keputusan ke fungsi murni yang dipanggil controller.
- **Files modified:** Controllers/CMPController.cs (4 helper), 3 file test unit
- **Verification:** RED terbukti via assertion sungguhan (stub helper meniru perilaku bug → fail), GREEN setelah fix body. Full suite 411/411.
- **Committed in:** `e0b47e7b` (stub) + `398eeaec` / `86d9a9f5` (fix body)

**2. [Rule 3 - Blocking] AbandonGuardTests pakai real-SQL disposable fixture, bukan InMemory**
- **Found during:** Task 1
- **Issue:** Plan menyebut opsi InMemory. `ExecuteUpdateAsync` tidak didukung EF Core 8 InMemory provider, DAN kontrak atomic/race guard hanya bisa dibuktikan terhadap SQL nyata.
- **Fix:** Disposable real-SQL fixture (`HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, MigrateAsync, drop on dispose, `[Trait("Category","Integration")]`) — mirror `SubmitResurrectionFixture` (Plan 01) / `ProtonCompletionFixture`. DB lokal `HcPortalDB_Dev` TAK tersentuh (tidak melanggar CLAUDE.md). Test mengeksekusi pola guarded-UPDATE IDENTIK dengan fix Task 3 langsung terhadap context.
- **Files modified:** AbandonGuardTests.cs
- **Verification:** 3/3 GREEN (Completed→0 rows status preserved; non-owner→0 rows; owner-InProgress→1 row Abandoned). SQLEXPRESS + SQLBrowser lokal Running.
- **Committed in:** `e0b47e7b`

---

**Total deviations:** 2 auto-fixed (keduanya blocking — pemilihan strategi test). **Impact:** Tidak mengubah scope/behavior produksi maupun must-haves. Semua truth must_haves tercapai. Test `Category=Integration` (AbandonGuard) bisa di-skip via `--filter "Category!=Integration"` di CI SQL-less (konsisten Plan 01 / Phase 358/376).

## Issues Encountered
- Line-number drift dari anchor plan (Phase 381 mendarat dulu) — semua edit dilakukan by-content, bukan by-line; semua anchor terverifikasi.
- Stray PowerShell error "Persero not recognized" saat start service (nama dir bocor ke invokasi) — tidak mempengaruhi; service `MSSQL$SQLEXPRESS` + `SQLBrowser` terkonfirmasi Running, Integration test jalan.

## TDD Gate Compliance
Plan `type: execute`, Task 1 `tdd="true"`.
1. RED gate: `test(382-02)` `e0b47e7b` — 4 fact FAIL (Standard-enforce, token-gate ×2, token-retry) via stub bug-behavior; 9 fact green (negatif/regresi) + AbandonGuard 3/3 (contract).
2. GREEN gate: `fix(382-02)` `f733a20e`/`ab86acba`/`398eeaec`/`86d9a9f5` — semua RED fact GREEN, full suite 411/411.
3. REFACTOR: tidak diperlukan (helper minimal sudah bersih).

## Verification Results
- `dotnet build`: **0 Error** (24 warning pre-existing, out-of-scope).
- Subset per task: AbandonGuard 3/3 · EnsureCanSubmitStandard 5/5 · AutoSubmitTokenRetry 2/2 · TokenGate 4/4 (= 16 fact baru).
- Full suite `dotnet test HcPortal.Tests` (incl. Integration + Plan 01 GradingDedupe/SubmitResurrection): **411 passed / 0 failed / 0 skipped** (1m43s). Tanpa regresi (395→411, +16).
- must_haves key_links terverifikasi di source: SubmitExam GroupBy `OrderByDescending(r => r.SubmittedAt)` (L1693); AbandonExam `ExecuteUpdateAsync` + `rowsAffected == 0` WHERE `UserId && (InProgress||Open)` (L1283-1290); EnsureCanSubmit `ShouldEnforceSubmitTimer` blocklist `AssessmentType.Manual` (L4471/4514); `TempData.Remove($"AutoSubmitToken_{id}")` HANYA di SubmitExam success path (L1764, BUKAN di EnsureCanSubmitExamAsync — pakai `TempData.Keep`).
- Migration: **0 file** `Migrations/` atau `*ModelSnapshot.cs` tersentuh di 5 commit (`git diff e74b3e8c..HEAD -- Migrations/` kosong). migration=false. **0 deletion** tracked file.

## User Setup Required
None. (Test `Category=Integration` AbandonGuard butuh `localhost\SQLEXPRESS` + SQLBrowser saat run — terverifikasi Running di mesin dev. CI SQL-less skip via filter.)

## Next Phase Readiness
- Sisi CMPController WSE-06..10 selesai. SubmitExam/SaveAnswer/AbandonExam/EnsureCanSubmit kini coherent + race-safe + timer-enforced + token-gated.
- Plan 03 (CERT-01 `DeriveCertificateStatus` ValidUntil=null → Aktif/Permanen konsisten) = sisa fase 382; +1 migration (filtered-unique-index PackageUserResponse single-answer) sesuai PROJECT.md scope — notify IT saat fase tuntas.
- NOT PUSHED (DEV_WORKFLOW: verifikasi lokal dulu). migration=false untuk plan ini.

## Self-Check: PASSED

Semua 6 file diklaim ada (FOUND: 4 test + CMPController.cs + SUMMARY.md) + 5 commit hash ada (e0b47e7b, f733a20e, ab86acba, 398eeaec, 86d9a9f5).

---
*Phase: 382-grading-lifecycle-cert*
*Completed: 2026-06-14*
