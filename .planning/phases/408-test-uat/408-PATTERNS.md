# Phase 408: Test & UAT - Pattern Map

**Mapped:** 2026-06-22
**Files analyzed:** 3 (all NEW test artifacts; 0 production code)
**Analogs found:** 3 / 3 (all exact or strong matches; all in-repo VERIFIED)

> Capstone test/UAT untuk milestone v32.4 Ujian Ulang (RTK-14). 408 = KOMPOSISI test (jahit jalur lulus→cert + lifecycle browser) + secure gate. JANGAN tulis ulang test hijau. Read-only — file ini satu-satunya output.

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|----------|------|-----------|----------------|---------------|
| `HcPortal.Tests/RetakeThenPassCertTests.cs` | test (xUnit integration, real-SQL) | CRUD + transform (grade) | `HcPortal.Tests/RetakeServiceTests.cs` (fixture+seed) + `HcPortal.Tests/SubmitResurrectionTests.cs` (GradingService ctor) | exact (two-analog stitch) |
| `tests/e2e/retake-lifecycle-408.spec.ts` | test (Playwright e2e) | request-response + event-driven (browser) | `tests/e2e/retake-worker-407.spec.ts` (serial + snapshot/seed) + `tests/e2e/exam-taking.spec.ts` Flow A + `tests/e2e/helpers/examTypes.ts` (`submitExamTwoStep`) | exact (two-analog stitch) |
| `tests/sql/retake-lifecycle-408-seed.sql` | test fixture (SQL seed) | file-I/O (seed script) | `tests/sql/retake-worker-407-seed.sql` | exact (extend, not copy) |

---

## Pattern Assignments

### `HcPortal.Tests/RetakeThenPassCertTests.cs` (xUnit integration, real-SQL)

**Primary analog:** `HcPortal.Tests/RetakeServiceTests.cs` (fixture + seed helpers)
**Secondary analog (RESOLVES OQ-1 — GradingService ctor):** `HcPortal.Tests/SubmitResurrectionTests.cs:68-76`

> **KEY UNBLOCK:** OQ-1 ("bisakah GradingService diinstansiasi di test?") is RESOLVED. Two existing integration tests already construct `GradingService` directly with hand-stubs (`SubmitResurrectionTests.cs:75`, `GradingDedupeTests.cs:82`). The new test copies that exact ctor recipe — **no Opsi-B/Opsi-C fallback needed.**

**Imports pattern** (`RetakeServiceTests.cs:17-31` + add `System.Text.Json` per `SubmitResurrectionTests.cs:13`):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;            // JsonSerializer.Serialize untuk ShuffledQuestionIds
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;  // status konstanta

namespace HcPortal.Tests;
```

**Trait + fixture pattern** (`RetakeServiceTests.cs:102-111` — REUSE `RetakeServiceFixture` existing, do NOT define new fixture):
```csharp
[Trait("Category", "Integration")]   // → SQL-less CI skip via --filter "Category!=Integration"
public class RetakeThenPassCertTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeThenPassCertTests(RetakeServiceFixture f) => _fixture = f;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static RetakeService NewRetake(ApplicationDbContext ctx) =>
        new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);
```
> `RetakeServiceFixture` (disposable DB `HcPortalDB_Test_{guid}` @SQLEXPRESS, `MigrateAsync` full chain incl `AddRetakeColumnsAndArchive`, drop-on-dispose) and `NoOpHubContext` are in the SAME assembly/namespace (`HcPortal.Tests`) — directly reusable. Do not re-declare.

**GradingService construction pattern — CORE GAP-1 UNBLOCK** (`SubmitResurrectionTests.cs:68-76`, copy verbatim; all stubs `FakeNotificationService` / `FakeWorkerDataService` already exist in `HcPortal.Tests/`):
```csharp
private static GradingService NewGrading(ApplicationDbContext ctx)
{
    var fakeNotif = new FakeNotificationService();                  // HcPortal.Tests/FakeNotificationService.cs:10
    var audit = new AuditLogService(ctx);
    var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
    var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
    var worker = new FakeWorkerDataService();                       // HcPortal.Tests/FakeWorkerDataService.cs:11
    return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
}
```

**Seed-session pattern — MUST extend with `generateCertificate` param** (base `RetakeServiceTests.cs:122-140`; Pitfall 2: model default `GenerateCertificate=false`):
```csharp
// Copy SeedSessionAsync from RetakeServiceTests.cs:122-140 and ADD a param:
//   bool generateCertificate = false      → set s.GenerateCertificate = generateCertificate
// For the cert path: pass generateCertificate:true, isPassed:false (start failed),
//   assessmentType:"PostTest", cooldownHours:0, completedAt: past, PassPercentage low (default model).
// NOTE: SeedSessionAsync sets Score=50/Progress=100 — irrelevant once re-graded.
```

**Seed-package-with-correct-responses pattern** (`RetakeServiceTests.cs:143-174` — REUSE as-is; every MC question gets one IsCorrect=true option which the response selects → re-grade yields 100% → isPassed). Note it also seeds `UserPackageAssignment.ShuffledQuestionIds` which `GradeAndCompleteAsync` reads via `GetShuffledQuestionIds()`.

**Core stitch pattern — the GAP-1 invariant** (research GAP-1 skeleton + Pitfall 1; cert path = GradingService NOT RetakeService):
```csharp
[Fact]
public async Task RetakeThenPass_IssuesExactlyOneCertificate()
{
    await using var ctx = NewCtx();
    var userId = await SeedUserAsync(ctx);                                   // RetakeServiceTests.cs:114
    var sid = await SeedSessionAsync(ctx, userId, "CertTitle", "Post",
        status:"Completed", isPassed:false, allowRetake:true, maxAttempts:2,
        cooldownHours:0, completedAt:DateTime.UtcNow.AddDays(-2),
        assessmentType:"PostTest", generateCertificate:true /* new param */);
    await SeedPackageWithResponsesAsync(ctx, sid, 3);                        // all-correct responses

    // 1. Retake: reset-ONLY (deletes responses+assignment, archives snapshot). NO cert here.
    await NewRetake(ctx).ExecuteAsync(sid, userId, "Tester", "RetakeAssessment", "worker_retake");

    // 2. Simulate retake-pass: re-seed package+assignment+correct responses post-reset, set Completed→
    //    GradeAndCompleteAsync grades from DB. (ExecuteAsync removed responses/assignment — restore to grade.)
    //    PITFALL 1: assert cert==null straight after ExecuteAsync would always fail — cert lives in step 2.
    await SeedPackageWithResponsesAsync(ctx, sid, 3);                        // re-seed correct path
    var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
    await NewGrading(ctx).GradeAndCompleteAsync(session);                    // GradingService.cs:287-312 issues cert

    // 3. CORE assert: exactly 1 NomorSertifikat (anti-double-cert guard: retry 3x WHERE NomorSertifikat==null).
    await using var verify = NewCtx();
    int certCount = await verify.AssessmentSessions.CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
    Assert.Equal(1, certCount);
    var cert = await verify.AssessmentSessions.Where(a => a.Id == sid).Select(a => a.NomorSertifikat).SingleAsync();
    Assert.Matches(@"^KPB/\d{3}/[IVX]+/\d{4}$", cert);                       // CertNumberHelper.cs:20-21 format
}
```

**Anti-double-cert guard being proven** (`Services/GradingService.cs:287-312` — DO NOT modify; test exercises it):
```csharp
if (session.GenerateCertificate && isPassed) {
    // retry 3x + filtered WHERE NomorSertifikat == null (idempotent, anti dobel)
    await _context.AssessmentSessions
        .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
        .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
}
// Format: KPB/{seq:D3}/{RomanMonth}/{year}  e.g. "KPB/005/VI/2026"  — Helpers/CertNumberHelper.cs:20-21
```

**Counting no-conflate (D-02 / GAP-2 — DO NOT duplicate):** `RetakeServiceTests.Counting_PrePostSameTitle_NoConflate` (`RetakeServiceTests.cs:336-358`) already covers `(UserId,Title,Category)` Pre≠Post. Per CONTEXT D-02 + research Pitfall 4: **reference it**, optionally add ONE assert inside the GAP-1 test that `CanRetakeAsync` counting is unaffected post-grade. No new counting test.

---

### `tests/e2e/retake-lifecycle-408.spec.ts` (Playwright e2e lifecycle)

**Primary analog (harness):** `tests/e2e/retake-worker-407.spec.ts` (serial + beforeAll backup/seed + afterAll restore + login + sid-by-Title)
**Secondary analog (take-and-pass):** `tests/e2e/exam-taking.spec.ts:103-165` Flow A + `tests/e2e/helpers/examTypes.ts:327-337` (`submitExamTwoStep`)

**Imports + serial harness pattern** (`retake-worker-407.spec.ts:31-43`; add `submitExamTwoStep`):
```typescript
import { test, expect, type Page } from '@playwright/test';
import { resolve } from 'path';
import * as db from '../helpers/dbSnapshot';            // tests/helpers/dbSnapshot.ts
import { login } from '../helpers/auth';                // tests/helpers/auth.ts
import { submitExamTwoStep } from './helpers/examTypes';// tests/e2e/helpers/examTypes.ts:327

const SEED_SQL = resolve(__dirname, '..', 'sql', 'retake-lifecycle-408-seed.sql');
let snapshotPath: string;
let sidFailed = 0;                                       // sesi gagal cooldown=0 (lifecycle target)
test.describe.configure({ mode: 'serial' });            // WAJIB serial + --workers=1
```

**SEED_WORKFLOW snapshot/seed/restore pattern** (`retake-worker-407.spec.ts:46-82` — copy verbatim; only change Title prefix to `[RETAKE408]`):
```typescript
test.beforeAll(async () => {
  const dir = (await db.queryString(
    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
  )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre408-${ts}.bak`;
  await db.backup(snapshotPath);                         // dbSnapshot.ts:67 BACKUP WITH INIT,FORMAT
  await db.execScript(SEED_SQL);                         // dbSnapshot.ts:105 sqlcmd -i (idempotent WIPE+INSERT)
  sidFailed = await db.queryScalar(
    "SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '[RETAKE408] Lifecycle Fail-to-Pass'"
  );
  expect(sidFailed, 'sesi lifecycle ter-seed').toBeGreaterThan(0);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  let restoreError: unknown = null;
  try {
    await db.restore(snapshotPath);                      // dbSnapshot.ts:80 SINGLE_USER ROLLBACK IMMEDIATE
    const fs = await import('node:fs');
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  } catch (e) { restoreError = e; }
  if (restoreError) throw restoreError;
});
```

**pageerror guard pattern (lesson 413 — WAJIB)** (`retake-worker-407.spec.ts:85-90, 116-117`):
```typescript
const errors: string[] = [];
page.on('pageerror', (err) => errors.push(err.message));   // arm BEFORE navigation
await login(page, 'coachee');                              // auth.ts:4 — coachee fixture (worker self-service)
await page.goto(`/CMP/Results/${sidFailed}`);
await page.waitForLoadState('networkidle');
// ... at each step:
expect(errors, `pageerror: ${errors.join(' | ')}`).toEqual([]);  // lesson 413 monFlashRow bug class
```

**Leak-safe verdict assert pattern** (`retake-worker-407.spec.ts:99-114` — ShowWrongFlagsOnly, kunci hidden):
```typescript
await expect(page.locator('[role="status"]', { hasText: 'Kunci jawaban disembunyikan' })).toBeVisible();
const bodyHtml = await page.content();
expect(bodyHtml, 'tidak ada label "(Jawaban Benar)"').not.toContain('(Jawaban Benar)');
await expect(page.locator('.list-group-item-success')).toHaveCount(0);
```

**Retake modal → StartExam redirect pattern** (`retake-worker-407.spec.ts:141-147` modal + `RetakeExam` controller `CMPController.cs:2527-2554` redirects to StartExam):
```typescript
await page.locator('#btnRetake').click();
const modal = page.locator('#retakeConfirmModal');
await expect(modal).toBeVisible();
await expect(modal.locator('form input[name="__RequestVerificationToken"]')).toHaveCount(1);  // antiforgery
await Promise.all([
  page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 }),     // RetakeExam re-arms token, redirects StartExam
  modal.locator('button[type="submit"]').click(),                  // "Ya, Ujian Ulang"
]);
```

**Answer-correct (shuffle-safe label-by-text) pattern — CRITICAL** (`exam-taking.spec.ts:122-137`; NEVER positional `.nth()`, shuffle default ON v27.0):
```typescript
const questionCards = page.locator('[id^="qcard_"]');
const qCount = await questionCards.count();
for (let i = 0; i < qCount; i++) {
  const qCard = questionCards.nth(i);
  const qText = await qCard.locator('h6, .fw-bold').first().textContent() ?? '';
  // Map question text → known correct option text (seed defines correct answers deterministically).
  let correctText = '<CORRECT_FOR_DEFAULT>';
  if (qText.includes('<marker2>')) correctText = '<correct2>';
  await qCard.locator('label[id^="lbl_"]').filter({ hasText: correctText }).first().click();  // by TEXT, not nth
  await page.waitForTimeout(700);
}
await expect(page.locator('#answeredProgress')).toContainText(`${qCount}/${qCount}`);
```

**Submit two-step helper** (`tests/e2e/helpers/examTypes.ts:327-337` — REUSE, do not hand-roll; dialog armed before Kumpulkan click, waits `/CMP/Results`):
```typescript
await submitExamTwoStep(page);   // reviewSubmitBtn → ExamSummary → "Kumpulkan Ujian" + confirm() accept → /CMP/Results
```
> Note: `exam-taking.spec.ts` Flow A handles `#resumeConfirmModal` (lines 114-119) — lifecycle 408 enters StartExam FRESH after a retake-reset (not Resume), so the resume modal likely will NOT appear. Keep a defensive dismiss (`waitFor visible 8s .catch(() => {})`) only if StartExam renders it; otherwise omit.

**Final lulus + cert assert pattern** (research GAP-3 skeleton; cert# format from `CertNumberHelper.cs:20-21`):
```typescript
await expect(page.locator('body')).toContainText('LULUS');
await expect(page.locator('body')).toContainText(/KPB\/\d+\/[IVX]+\/\d{4}/);  // exactly-1 cert proven by xUnit GAP-1
expect(errors).toEqual([]);
```

---

### `tests/sql/retake-lifecycle-408-seed.sql` (SQL seed fixture)

**Analog:** `tests/sql/retake-worker-407-seed.sql` (EXTEND, not duplicate — 407 seeds failed sessions but NO pass-path; 408 needs cooldown=0 + clear correct-answer path so the retake can PASS)

**Header + classification pattern** (`retake-worker-407-seed.sql:1-44` — adapt prefix to `[RETAKE408]`, classification temporary+local-only, run via db.execScript):
```sql
-- Phase 408 RTK-14 — Lifecycle e2e Seed (gagal → Ujian Ulang → lulus → 1 cert)
-- Klasifikasi: temporary + local-only. Prefix Title '[RETAKE408]'. JANGAN promosikan ke Data/SeedData.cs.
-- Run: db.execScript di beforeAll (db.backup snapshot sebelum + db.restore sesudah — SEED_WORKFLOW).
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture, sama 407).
SET NOCOUNT ON;
```

**Idempotent FK-safe cleanup pattern** (`retake-worker-407-seed.sql:46-76` — copy block, swap prefix to `[[]RETAKE408%`; order: archive→history→response→assignment→option→question→package→session).

**Pre-condition THROW pattern** (`retake-worker-407-seed.sql:78-84`):
```sql
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51408, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;
```

**Failed-session insert pattern — KEY DIFFS vs 407** (base `retake-worker-407-seed.sql:91-101` [407A], change for pass-path):
```sql
-- Same column list as 407A. CRITICAL DIFFS for lifecycle pass:
--   GenerateCertificate = 1   (407 used 0; cert MUST be enabled — Pitfall 2)
--   IsPassed            = 0   (start failed)
--   PassPercentage      low (e.g. 50) so answering all-correct → LULUS
--   RetakeCooldownHours = 0   (cooldown=0 → #btnRetake aktif langsung — Pitfall 5)
--   AllowRetake=1, MaxAttempts>=2, AllowAnswerReview=1, AssessmentType='PostTest'
--   CompletedAt = DATEADD(DAY,-2,GETUTCDATE())   (past → no cooldown gate)
```

**Package + MC questions with KNOWN correct answer pattern** (base `retake-worker-407-seed.sql:104-153`):
```sql
-- Seed 2-3 SingleAnswer/MultipleChoice questions. For EACH question:
--   one PackageOption IsCorrect=1 with a UNIQUE, deterministic OptionText (e.g. 'BENAR408_Q1_<topic>')
--   so the e2e spec selects it BY TEXT (shuffle-safe). Question text contains a unique marker
--   the spec maps to the correct option (exam-taking.spec.ts:130-132 pattern).
-- Seed UserPackageAssignment with ShuffledQuestionIds = '[q1,q2,...]' (RetakeExam→StartExam needs assignment).
-- Seed CURRENT responses (the prior failed attempt: at least one WRONG so Results shows ✗ verdict pre-retake).
```

**Attempt-archive pattern (optional, controls counting)** (`retake-worker-407-seed.sql:155-164`): seed ≥0 prior `AssessmentAttemptHistory` with child `AssessmentAttemptResponseArchives` so `currentAttempt < MaxAttempts` (eligible). For a clean single-pass lifecycle, 0 prior era-retake archives (currentAttempt=1) + MaxAttempts>=2 is simplest.

**Output pattern** (`retake-worker-407-seed.sql:221-222`):
```sql
SELECT @sid AS SidFailed;   -- spec resolves by Title; output for manual run visibility
```

---

## Shared Patterns

### Real-SQL integration fixture (xUnit)
**Source:** `HcPortal.Tests/RetakeServiceTests.cs:34-67` (`RetakeServiceFixture`)
**Apply to:** GAP-1 test (REUSE existing fixture — `IClassFixture<RetakeServiceFixture>`, do not redeclare).
- Disposable DB `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync` full chain (incl `AddRetakeColumnsAndArchive`), drop on dispose, `XunitException` on chain-break.
- `[Trait("Category", "Integration")]` so SQL-less CI skips via `--filter "Category!=Integration"`.

### Service-with-deps construction (xUnit, no Moq)
**Source:** `HcPortal.Tests/SubmitResurrectionTests.cs:68-76` (GradingService) + `RetakeServiceTests.cs:110-111` (RetakeService)
**Apply to:** GAP-1 test.
- Project has NO Moq/NSubstitute. Use hand-stubs already in `HcPortal.Tests/`: `NoOpHubContext` (`RetakeServiceTests.cs:70-100`), `FakeNotificationService` (`FakeNotificationService.cs:10`), `FakeWorkerDataService` (`FakeWorkerDataService.cs:11`). Loggers = `NullLogger<T>.Instance`. `AuditLogService` real over test ctx.

### SEED_WORKFLOW snapshot/restore (Playwright e2e)
**Source:** `tests/e2e/retake-worker-407.spec.ts:46-82` + `tests/helpers/dbSnapshot.ts`
**Apply to:** GAP-3 spec.
- `db.backup` (InstanceDefaultBackupPath, WITH INIT,FORMAT) in `beforeAll` → `db.execScript(seed)` → `db.restore` (SINGLE_USER ROLLBACK IMMEDIATE) in `afterAll`. localhost-only guard built in. Mode `serial` + run `--workers=1`. Set `E2E_BASE_URL=http://localhost:5270` (branch ITHandoff; config default 5277).

### pageerror assert (lesson 354/413 — Razor/JS WAJIB real-browser)
**Source:** `tests/e2e/retake-worker-407.spec.ts:86-89, 116-117`
**Apply to:** GAP-3 spec at every step (load Results, open modal, StartExam, submit).
- `page.on('pageerror', e => errors.push(e.message))` armed before nav; `expect(errors).toEqual([])`. Caught the `monFlashRow` ReferenceError in Phase 413 that grep+build+runtime-smoke missed.

### Shuffle-safe answer selection (Playwright e2e)
**Source:** `tests/e2e/exam-taking.spec.ts:129-133`
**Apply to:** GAP-3 spec take-and-pass section.
- Shuffle options default ON (v27.0) → positional `.nth()` is WRONG. Select `label[id^="lbl_"]` filtered `{ hasText: correctText }`. Seed must give deterministic unique correct OptionText.

### Login (Playwright e2e)
**Source:** `tests/helpers/auth.ts:4-11` + `tests/helpers/accounts.ts`
**Apply to:** GAP-3 spec — `await login(page, 'coachee')` (worker self-service fixture = `rino.prasetyo@pertamina.com`, dev-local). Lands on `/Home/*`; `/CMP/Results/{id}` gated by ownership (seed session belongs to fixture → access OK).

### Cert number format (assertion target)
**Source:** `Helpers/CertNumberHelper.cs:20-21`
**Apply to:** both GAP-1 (`@"^KPB/\d{3}/[IVX]+/\d{4}$"`) and GAP-3 (`/KPB\/\d+\/[IVX]+\/\d{4}/`). Format `KPB/{seq:D3}/{RomanMonth}/{year}`.

---

## No Analog Found

None. All 3 new files have strong in-repo analogs. The only "new" composition is stitching `RetakeService.ExecuteAsync` + `GradingService.GradeAndCompleteAsync` in one xUnit test (GAP-1) and stitching the 407 worker-results harness + exam-taking Flow A in one e2e spec (GAP-3) — both component patterns already exist and are VERIFIED.

> **Note for planner — secure-phase 408 (D-03):** This is a doc gate, not a code file, so it has no analog here. The plan MUST include a `<threat_model>` block consolidating 406+407 threats. Source the threat register from `.planning/phases/407-.../407-SECURITY.md` (T-407-idor/csrf/bypass/leak/token + AR-407-01/02) and `.planning/phases/406-.../406-SECURITY.md` (admin config surface), plus the cert-uniqueness invariant now covered by GAP-1 (`GradingService.cs:287-312`, unique index `IX_AssessmentSessions_NomorSertifikat`). See research §Security Domain for the full consolidated table. Accepted-risk to carry: AR-407-01 (double-archive race) + AR-407-02 (eraRetakeArchives duplicated 3 places — IN-03 backlog, DO NOT refactor in 408).

## Metadata

**Analog search scope:** `HcPortal.Tests/` (xUnit fixtures + GradingService instantiation), `tests/e2e/` + `tests/e2e/helpers/` + `tests/helpers/` (Playwright harness/helpers), `tests/sql/` (seed scripts), `Services/` (GradingService, RetakeService, Proton* ctors), `Helpers/` (CertNumberHelper), `Controllers/CMPController.cs` (RetakeExam→StartExam flow).
**Files scanned:** ~12 (all VERIFIED by direct read).
**Key resolution:** OQ-1 (GradingService ctor in test) RESOLVED — `SubmitResurrectionTests.cs:68-76` + `GradingDedupeTests.cs:82` already do it; no Opsi-B/C fallback needed.
**Pattern extraction date:** 2026-06-22
