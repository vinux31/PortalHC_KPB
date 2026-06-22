---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
verified: 2026-06-21T12:00:00Z
status: passed
score: 7/7
overrides_applied: 0
human_verification_confirmed: "Both items confirmed by orchestrator 2026-06-21: (1) full suite `dotnet test` = 587 passed / 0 failed / 2 skipped (incl RetakeRules 16 + RetakeArchiveBuilder 4 + RetakeService integration 5 @SQLEXPRESS + ResetGuard 2; 2 skip = pre-existing PROTON endtoend, NOT 405). (2) DB schema confirmed via 405-01 sqlcmd: 3 columns (default false/2/24) + AssessmentAttemptResponseArchives table + FK CASCADE present in HcPortalDB_Dev."
human_verification:
  - test: "Jalankan full suite termasuk integration: dotnet test (unit + RetakeServiceTests@SQLEXPRESS). Konfirmasi 587/0 atau setara — suite lokal tak bisa dijalankan verifier."
    expected: "Semua hijau: RetakeRulesTests 16/16 + RetakeArchiveBuilderTests 4/4 + RetakeServiceTests 5/5 + ResetGuardTests 2/2 + regresi unit 436/438 (2 skipped)"
    why_human: "Verifier tidak dapat menjalankan dotnet test atau menyentuh SQLEXPRESS langsung."
  - test: "Konfirmasi DB lokal HcPortalDB_Dev telah di-update: sqlcmd -S localhost\\SQLEXPRESS -d HcPortalDB_Dev -E -C -I -Q \"SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('AssessmentSessions') AND name IN ('AllowRetake','MaxAttempts','RetakeCooldownHours'); SELECT OBJECT_ID('AssessmentAttemptResponseArchives') AS ArchiveTableId;\""
    expected: "3 baris kolom muncul + ArchiveTableId non-null (ObjectId 436196604 per SUMMARY)"
    why_human: "Verifier tidak dapat query DB lokal secara langsung — diklaim sudah applied di 405-01-SUMMARY namun tidak bisa diverifikasi live."
---

# Phase 405: Backend Core — Verification Report

**Phase Goal:** Bangun fondasi backend ujian ulang tanpa UI — model data (3 kolom config + tabel snapshot per-soal), aturan kelayakan murni (RetakeRules), mesin retake bersama (RetakeService.ExecuteAsync: claim atomik → snapshot → archive → reset → clear-token → audit), refactor ResetAssessment HC agar delegasi ke service (override bypass), dan endpoint config UpdateRetakeSettings dengan sibling propagation.
**Verified:** 2026-06-21
**Status:** human_needed (semua 7 SC VERIFIED via kode; 2 item butuh human karena butuh dotnet test + DB query live)
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (dari Roadmap Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | 3 kolom config (AllowRetake/MaxAttempts/RetakeCooldownHours) di AssessmentSession + migration applied + eksplisit copy bulk-add (RTK-01) | VERIFIED | `Models/AssessmentSession.cs:46-54` — 3 kolom dengan default false/2/24 + `[Range(1,5)]` + `[Range(0,168)]`. Migration `20260621065918_AddRetakeColumnsAndArchive.cs:14-33` — 3 AddColumn dengan `defaultValue: false/2/24`. Bulk-add `AssessmentAdminController.cs:2184-2186` — `AllowRetake/MaxAttempts/RetakeCooldownHours = savedAssessment.*`. Snapshot `ProductVersion = "8.0.0"` confirmed. |
| 2 | Tabel AssessmentAttemptResponseArchive + builder pure RetakeArchiveBuilder.Build membekukan verdict via IsQuestionCorrect + jawaban (essay full-text, bukan truncate 300) SEBELUM RemoveRange (RTK-02) | VERIFIED | `Models/AssessmentAttemptResponseArchive.cs` — semua 9 field hadir termasuk AnswerText nullable + IsCorrect bool?. `Data/ApplicationDbContext.cs:71` — DbSet; `:588-595` — FK Cascade + HasIndex. Migration `:35-63` — CreateTable + FK ReferentialAction.Cascade + CreateIndex. `Helpers/RetakeArchiveBuilder.cs:39-48` — essay path: `answerText = essayResp?.TextAnswer` (FULL, bukan BuildAnswerCell); MC/MA path: `BuildAnswerCell`. `RetakeArchiveBuilderTests.cs:88-101` — `Build_EssayLongText_NotTruncated` assert `row.AnswerText!.Length == 500`. Snapshot ditulis sebelum delete: service L135 (Build) vs L142+ (delete). |
| 3 | RetakeRules.CanRetake pure + semua 7 guard + ShouldHideRetakeToggle + attemptsUsed count (UserId,Title,Category) + cooldown deterministic (RTK-03, RTK-13) | VERIFIED | `Helpers/RetakeRules.cs:41-51` — 7 guard berurutan: !allowRetake → PreTest → isManualEntry → status!="Completed" → isPassed!=false → attemptsUsed>=maxAttempts → cooldown. `RetakeRulesTests.cs` — 12 Fact + 1 Theory(4 case) = 16 test case mengunci semua cabang termasuk WhenPendingGrading (null) + WhenCancelled + CooldownZero. nowUtc di-inject bukan internal. counting (UserId,Title,Category) ada di service (CanRetakeAsync L204-207) — helper menerima attemptsUsed sebagai param, bukan query sendiri. |
| 4 | RetakeService.ExecuteAsync — claim transisi atomik DULU (ExecuteUpdateAsync WHERE Status NOT IN Cancelled/Open + rows==0 abort), snapshot per-soal → archive → delete → audit → SignalR reason parameterized (RTK-07) | VERIFIED | `Services/RetakeService.cs:75-88` — ExecuteUpdateAsync di L77 (SEBELUM Build L135) dengan WHERE `Status != "Cancelled" && Status != "Open"`; rows==0 → abort dengan error message. Snapshot: L135 `RetakeArchiveBuilder.Build(...)` di-call SEBELUM RemoveRange L142+. Audit L163-176 try/catch warn-only. SignalR L181 `new { reason }` parameterized. Ordering verified: claim (L77) → wasCompleted check (L93) → Build (L135) → delete (L142) → audit (L163) → SignalR (L181). |
| 5 | ResetAssessment HC delegasi ke RetakeService.ExecuteAsync + clear TempData[TokenVerified_{id}] + guard HC (IsResettable/Pre-Post/status) TETAP di controller; ResetGuardTests hijau (RTK-06) | VERIFIED | `Controllers/AssessmentAdminController.cs:4193` — `IsResettable` tetap `public static bool`. `:4207-4243` — guard HC (IsResettable + PrePost block + status guard) sebelum delegasi. `:4251-4256` — `_retakeService.ExecuteAsync(... actionType: "ResetAssessment", reason: "hc_reset")`. `:4270` — `TempData.Remove($"TokenVerified_{id}")`. Grep membuktikan 0 occurrence `SendAsync("sessionReset")` di controller (dipindah ke service). Commits ResetGuardTests: `e1d5defc` mencatat "Passed! 2/2". `HcPortal.Tests/ResetGuardTests.cs` memanggil `AssessmentAdminController.IsResettable(...)` static — tidak berubah. |
| 6 | Endpoint UpdateRetakeSettings [Authorize(Admin,HC)] + [ValidateAntiForgeryToken] + sibling propagation (Title/Category/Schedule.Date) + clamp + audit UpdateRetakeSettings + PRG; tidak ada IsShuffleLocked guard (RTK-04) | VERIFIED | `Controllers/AssessmentAdminController.cs:5564-5566` — `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`. `:5573` — `ShouldHideRetakeToggle` guard PreTest/Manual. `:5580-5581` — `Math.Clamp(maxAttempts, 1, 5)` + `Math.Clamp(retakeCooldownHours, 0, 168)`. `:5585` — sibling key `Title == && Category == && Schedule.Date ==`. `:5603` — `LogAsync("UpdateRetakeSettings", ...)`. `:5609-5610` — `TempData["Success"]` + `RedirectToAction("ManagePackages")`. IsShuffleLocked: TIDAK muncul di blok 5564-5611 (hanya di UpdateShuffleSettings area 5523 + ViewBag 5698). |
| 7 | Build 0 error + migration applied DB lokal + test suite hijau (semua REQ) | VERIFIED (partial — build+commits verified; test run butuh human) | 12 commits terverifikasi di git log (`2d04f216`..`e6abb938`). SUMMARYs melaporkan "Build succeeded 0 error" pasca setiap task. VALIDATION tidak bisa dijalankan langsung oleh verifier — lihat Human Verification. RetakeService DI: `Program.cs:63` — `AddScoped<HcPortal.Services.RetakeService>()`. |

**Score:** 7/7 truths VERIFIED (kode terbukti; test run + DB query membutuhkan human konfirmasi)

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentAttemptResponseArchive.cs` | Entity snapshot per-soal (RTK-02) | VERIFIED | Semua 9 field hadir: Id/AttemptHistoryId/AttemptHistory/PackageQuestionId/QuestionText/AnswerText?/IsCorrect?/AwardedScore/ArchivedAt |
| `Models/AssessmentSession.cs` | 3 kolom config retake | VERIFIED | AllowRetake=false/MaxAttempts=2/RetakeCooldownHours=24 + [Range] annotations, sisip setelah ShuffleOptions line 42 |
| `Data/ApplicationDbContext.cs` | DbSet + FK cascade + index | VERIFIED | DbSet line 71 + entity config block 588-595 dengan OnDelete(DeleteBehavior.Cascade) + HasIndex(AttemptHistoryId) |
| `Migrations/20260621065918_AddRetakeColumnsAndArchive.cs` | 3 AddColumn + CreateTable + FK Cascade + CreateIndex | VERIFIED | Up() berisi persis: 3 AddColumn defaultValue false/2/24 + CreateTable + FK ReferentialAction.Cascade + CreateIndex AttemptHistoryId |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | ProductVersion tetap "8.0.0" | VERIFIED | Line 20: `"8.0.0"` |
| `Helpers/RetakeRules.cs` | Pure CanRetake + ShouldHideRetakeToggle (RTK-03/13) | VERIFIED | public static class, CanRetake 7 guard urut fail-fast, ShouldHideRetakeToggle = PreTest||ManualEntry |
| `Helpers/RetakeArchiveBuilder.cs` | Pure snapshot builder (RTK-02) | VERIFIED | Build() dengan verdict via IsQuestionCorrect, essay full-text TextAnswer, MC/MA via BuildAnswerCell |
| `HcPortal.Tests/RetakeRulesTests.cs` | 16 unit test semua cabang | VERIFIED | 12 Fact + 1 Theory(4 case) = 16; semua cabang CanRetake + ShouldHideRetakeToggle; fixed clock Now+EligibleCompletedAt |
| `HcPortal.Tests/RetakeArchiveBuilderTests.cs` | 4 unit test verdict freeze + essay full-text | VERIFIED | Build_MarksCorrect + Build_WrongAnswer + Build_EssayPending + Build_EssayLongText_NotTruncated (assert Length==500) |
| `Services/RetakeService.cs` | ExecuteAsync claim-first + CanRetakeAsync D-01 + RetakeResult | VERIFIED | ExecuteUpdateAsync L77 SEBELUM Build L135; D-01 EXISTS subquery L100+L207; CanRetakeAsync panggil RetakeRules.CanRetake; `readonly record struct RetakeResult(bool Success, string? Error)` |
| `Program.cs` | DI registration scoped | VERIFIED | Line 63: `builder.Services.AddScoped<HcPortal.Services.RetakeService>()` |
| `HcPortal.Tests/RetakeServiceTests.cs` | 5 integration test SQL-real + fixture MigrateAsync | VERIFIED | [Trait("Category","Integration")], RetakeServiceFixture disposable MigrateAsync, NoOpHubContext, 5 test case: Claim_DoubleExecute_SecondAborts + Snapshot_WrittenBeforeResponsesDeleted + CanRetake_LegacyArchiveWithoutSnapshot + CanRetake_RetakeEraArchiveWithSnapshot + Counting_PrePostSameTitle_NoConflate |
| `Controllers/AssessmentAdminController.cs` | ResetAssessment delegasi + UpdateRetakeSettings + bulk-add carry + ViewBag | VERIFIED | _retakeService L32/47/58; ResetAssessment L4251 delegasi; UpdateRetakeSettings L5567; bulk-add L2184; ViewBag AllowRetake/MaxAttempts/RetakeCooldownHours/HideRetakeToggle/RetakeMaxAttemptsUsedInGroup L5703-5711 |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| `Data/ApplicationDbContext.cs` | `Models/AssessmentAttemptResponseArchive.cs` | `DbSet<AssessmentAttemptResponseArchive>` + `HasForeignKey.OnDelete(Cascade)` | WIRED | DbSet L71 + entity config L588-595 |
| `Migrations/*_AddRetakeColumnsAndArchive.cs` | DB AssessmentSessions + AssessmentAttemptResponseArchives | `dotnet ef database update` (applied lokal per SUMMARY) | WIRED (human-confirm DB) | Migration file verified; SUMMARY reports sqlcmd confirmed 3 kolom + ObjectId 436196604 |
| `Helpers/RetakeArchiveBuilder.cs` | `Helpers/AssessmentScoreAggregator.cs` | `AssessmentScoreAggregator.IsQuestionCorrect(q, forQ)` (L34) | WIRED | Line 34: `bool? verdict = AssessmentScoreAggregator.IsQuestionCorrect(q, forQ)` |
| `Helpers/RetakeArchiveBuilder.cs` | `Models/AssessmentAttemptResponseArchive.cs` | `return List<AssessmentAttemptResponseArchive>` | WIRED | Return type + object initializer L51-59 |
| `Services/RetakeService.cs` | `Helpers/RetakeArchiveBuilder.cs` | `RetakeArchiveBuilder.Build(attemptHistory.Id, questions, responses)` | WIRED | Line 135: `var snapshot = RetakeArchiveBuilder.Build(...)` |
| `Services/RetakeService.cs` | `Helpers/RetakeRules.cs` | `CanRetakeAsync` membungkus `RetakeRules.CanRetake` | WIRED | Line 210: `return RetakeRules.CanRetake(...)` |
| `Services/RetakeService.cs` | `AssessmentAttemptResponseArchives` (DB) | EXISTS subquery `AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id)` | WIRED | Line 100 (ExecuteAsync) + Line 207 (CanRetakeAsync) |
| `Controllers/AssessmentAdminController.cs (ResetAssessment)` | `Services/RetakeService.cs (ExecuteAsync)` | `_retakeService.ExecuteAsync(id, ..., actionType: "ResetAssessment", reason: "hc_reset")` | WIRED | Line 4251-4256 |
| `Controllers/AssessmentAdminController.cs (UpdateRetakeSettings)` | `AssessmentSessions sibling group (DB)` | sibling key `Title == && Category == && Schedule.Date ==` → foreach → SaveChanges | WIRED | Line 5584-5597 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `Services/RetakeService.cs` | `assessment` (AssessmentSession) | `_context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == sessionId)` | DB query | FLOWING |
| `Services/RetakeService.cs` | `eraRetakeArchives` (count) | `AssessmentAttemptHistory` WHERE EXISTS `AssessmentAttemptResponseArchives.Any` | DB query (D-01 EXISTS) | FLOWING |
| `Services/RetakeService.cs` | `snapshot` (List archive rows) | `RetakeArchiveBuilder.Build(attemptHistory.Id, questions, responses)` | Real questions+responses dari DB | FLOWING |
| `Controllers/AssessmentAdminController.cs (UpdateRetakeSettings)` | siblings | `_context.AssessmentSessions.Where(sibling key).ToListAsync()` | Real DB query | FLOWING |
| `Controllers/AssessmentAdminController.cs (ManagePackages ViewBag)` | AllowRetake/MaxAttempts/RetakeCooldownHours | `assessment.AllowRetake` etc (model dari DB) | Real model values | FLOWING |

---

### Behavioral Spot-Checks

Step 7b: SKIPPED untuk dotnet build / dotnet test — tidak bisa dijalankan dari verifier. Lihat Human Verification Required.

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Commits exist in git log | `git log --oneline` grep 12 hashes | Semua 12 commit ditemukan (`2d04f216`..`e6abb938`) | PASS |
| Migration file exists + Up() berisi 3 AddColumn + CreateTable | Read file | defaultValue false/2/24 + ReferentialAction.Cascade + CreateIndex confirmed | PASS |
| claim-first ordering (ExecuteUpdateAsync L77 before Build L135) | Read RetakeService.cs | L77 claim, L135 Build, L142 delete — urutan benar | PASS |
| RetakeService DI scoped di Program.cs | grep Program.cs | Line 63 confirmed | PASS |
| UpdateRetakeSettings attributes | grep AssessmentAdminController.cs | [HttpPost]+[Authorize(Roles="Admin, HC")]+[ValidateAntiForgeryToken] di line 5564-5566 | PASS |
| IsShuffleLocked TIDAK di UpdateRetakeSettings | grep | 0 occurrence di blok UpdateRetakeSettings (5564-5611) | PASS |
| IsResettable tetap public static | grep | Line 4193: `public static bool IsResettable` | PASS |
| SendAsync("sessionReset") removed from controller | grep controller | 0 occurrence (dipindah ke service) | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| RTK-01 | 405-01, 405-04 | 3 kolom config AssessmentSession + migration + copy bulk-add | SATISFIED | Model ✓ Migration default false/2/24 ✓ DbContext ✓ bulk-add L2184 ✓ |
| RTK-02 | 405-01, 405-02 | AssessmentAttemptResponseArchive entity + RetakeArchiveBuilder.Build (verdict beku, essay full-text) | SATISFIED | Entity ✓ Builder (essay TextAnswer full, MC/MA BuildAnswerCell) ✓ FK Cascade ✓ index ✓ |
| RTK-03 | 405-02 | RetakeRules.CanRetake pure semua cabang + ShouldHideRetakeToggle | SATISFIED | 7 guard berurutan ✓ 16 unit test ✓ nowUtc inject ✓ |
| RTK-04 | 405-04 | UpdateRetakeSettings [Authorize(Admin,HC)]+AntiForgery+sibling propagation+clamp+audit | SATISFIED | Semua atribut ✓ sibling key ✓ Math.Clamp ✓ LogAsync ✓ PRG ✓ |
| RTK-06 | 405-04 | ResetAssessment HC delegasi RetakeService + guard HC tetap + clear token | SATISFIED | Delegasi ✓ guard tetap ✓ TempData.Remove ✓ IsResettable static ✓ |
| RTK-07 | 405-03 | RetakeService.ExecuteAsync claim-first+snapshot+archive+delete+audit+SignalR reason | SATISFIED | Ordering claim-first ✓ WHERE NOT IN (Cancelled,Open) ✓ rows==0 abort ✓ snapshot sebelum delete ✓ audit try/catch ✓ SignalR parameterized ✓ |
| RTK-13 | 405-02, 405-03 | Guards exclude PreTest/IsManualEntry/PendingGrading(null)/non-Completed | SATISFIED | CanRetake guard PreTest+isManualEntry+status!="Completed"+isPassed!=false (null=PendingGrading blocked) ✓ |

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| Semua file produksi | Tidak ditemukan TODO/FIXME/placeholder/return null/return {} stub patterns di code path baru | - | - |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | ProductVersion "8.0.0" (tidak drift ke 10.x) | Info | Aman — dikonfirmasi |
| `Migrations/20260621065918_AddRetakeColumnsAndArchive.cs` | defaultValue hand-fixed 0→2/24 (EF tidak menarik C# property default ke backfill) | Info | Sudah difix, nilai benar |

Tidak ada blocker atau warning anti-pattern ditemukan.

---

### Locked Decisions Verification (D-01..D-04)

| Decision | Requirement | Status | Evidence |
|----------|-------------|--------|----------|
| D-01: Era-retake counting via snapshot-presence EXISTS | Legacy AttemptHistory tanpa snapshot child TIDAK konsumsi cap | VERIFIED | `RetakeService.cs:96-101` + `CanRetakeAsync:203-208` — EXISTS subquery `AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id)`. Test `CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap` membuktikan. |
| D-02: Retroaktif (tanpa enabled-at timestamp) | Sesi yang sudah gagal langsung eligible saat AllowRetake=ON | VERIFIED | CanRetakeAsync cukup cek `s.AllowRetake` current — tidak ada enabled-at timestamp. RetakeRules.CanRetake tidak memeriksa kapan AllowRetake di-set. |
| D-03: Satu migration gabung | 3 kolom + tabel archive dalam 1 migration atomik | VERIFIED | File tunggal `20260621065918_AddRetakeColumnsAndArchive.cs` mencakup 3 AddColumn + CreateTable |
| D-04: Retain-forever (FK cascade) | Snapshot dihapus hanya saat parent AttemptHistory dihapus | VERIFIED | `OnDelete(DeleteBehavior.Cascade)` di DbContext + `ReferentialAction.Cascade` di migration |

### 3 Mandatory Corrections vs Old ResetAssessment

| Correction | Must-Fix | Status | Evidence |
|-----------|---------|--------|----------|
| Claim-FIRST (bukan archive-first) | must-fix #4 | VERIFIED | ExecuteUpdateAsync L77 mendahului RetakeArchiveBuilder.Build L135 dan AssessmentAttemptHistory.Add L116 |
| Counting (UserId,Title,Category) — anti-konflasi Pre/Post | must-fix #3 | VERIFIED | `h.Title == assessment.Title && h.Category == assessment.Category` di L98-99 + L205-206 |
| Essay full-text (bukan BuildAnswerCell truncate 300) | Pitfall 2 / D-04 | VERIFIED | RetakeArchiveBuilder.cs:43 `answerText = essayResp?.TextAnswer` (komentar "FULL-TEXT"); test Build_EssayLongText_NotTruncated assert Length==500 |

---

### Human Verification Required

#### 1. Full Test Suite Run

**Test:** Jalankan dari root project:
```
dotnet test --filter "Category!=Integration"  # unit only
dotnet test --filter "FullyQualifiedName~RetakeRules"
dotnet test --filter "FullyQualifiedName~RetakeArchiveBuilder"
dotnet test --filter "FullyQualifiedName~ResetGuardTests"
dotnet test --filter "FullyQualifiedName~RetakeServiceTests"  # butuh SQLEXPRESS
dotnet test  # full suite
```
**Expected:** RetakeRulesTests 16/16 + RetakeArchiveBuilderTests 4/4 + RetakeServiceTests 5/5 + ResetGuardTests 2/2 + unit baseline 436/438 (2 skipped) + total 587/0/2 (per context SUMMARY)
**Why human:** Verifier tidak dapat mengeksekusi dotnet test atau menyentuh SQLEXPRESS.

#### 2. DB Schema Konfirmasi

**Test:**
```
sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -Q "
SELECT name, column_default = object_definition(default_object_id)
FROM sys.columns
WHERE object_id=OBJECT_ID('AssessmentSessions')
  AND name IN ('AllowRetake','MaxAttempts','RetakeCooldownHours');
SELECT OBJECT_ID('AssessmentAttemptResponseArchives') AS ArchiveTableId;
SELECT is_cascade_delete = dc.type_desc
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('AssessmentAttemptResponseArchives')
  AND fk.referenced_object_id = OBJECT_ID('AssessmentAttemptHistory');"
```
**Expected:** 3 baris kolom dengan default `((0))`/`((2))`/`((24))` + ArchiveTableId non-null + FK dengan DELETE_CASCADE
**Why human:** Verifier tidak dapat query DB lokal secara langsung.

---

### Gaps Summary

Tidak ada gap. Semua 7 success criteria roadmap VERIFIED via kode. Dua item human verification bersifat konfirmasi eksekutor (test run + DB live) — bukan blocker kode. Jika SUMMARY melaporkan test/DB green (405-01-SUMMARY konfirmasi sqlcmd; 405-04-SUMMARY konfirmasi dotnet test 587/0/2), human verification item ini adalah formalitas prosedural.

### Noteworthy Architectural Deviation (non-blocking)

CONTEXT.md dan Roadmap SC-4 mencantumkan signature `ExecuteAsync(sessionId, initiatedBy, bypassGuards)` dengan parameter `bypassGuards:true` untuk HC override. Implementasi aktual menggunakan signature `ExecuteAsync(sessionId, actorUserId, actorName, actionType, reason)` tanpa parameter `bypassGuards` — HC "bypass" diimplementasikan via guards yang tetap di controller (PLAN 405-03/04 eksplisit mendokumentasikan keputusan ini). Intent terpenuhi: HC tidak terikat cap/cooldown (guard di controller lewati path RetakeService tanpa cap check); worker path (Phase 407) akan menggunakan `CanRetakeAsync` di controller sebelum memanggil service. Ini adalah deviasinya terhadap spec literal tetapi memenuhi goal fungsional — tidak perlu override karena keputusan ini sudah didokumentasikan di PLAN dan SUMMARY sebagai deliberate.

---

_Verified: 2026-06-21_
_Verifier: Claude (gsd-verifier)_
