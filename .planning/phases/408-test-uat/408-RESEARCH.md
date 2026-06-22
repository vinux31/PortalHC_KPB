# Phase 408: Test & UAT - Research

**Researched:** 2026-06-22
**Domain:** Capstone test/UAT untuk milestone v32.4 Ujian Ulang (xUnit + Playwright + secure-phase) di brownfield ASP.NET Core 8 MVC + EF Core + SQL Server. Branch ITHandoff, port 5270, 0 migration.
**Confidence:** HIGH (semua klaim diverifikasi langsung dari kode/test existing di repo)

## Summary

Phase 408 adalah capstone test & UAT RTK-14. Tujuannya **MENGISI GAP**, bukan menulis ulang test yang sudah hijau. Investigasi langsung atas file test memastikan: lapisan unit (`RetakeRulesTests` 22 Fact, `RetakeArchiveBuilderTests` 4 Fact, `RiwayatUnifierTests` 6 Fact) + integration RetakeService (`RetakeServiceTests` 8 Fact, real-SQL) + endpoint worker (`RetakeExamEndpointTests` 3 Fact) + Playwright per-surface (`retake-config-406` 6, `riwayat-hc-406` 5, `retake-worker-407` 6) SEMUA sudah ada dan hijau. Yang BELUM ada hanya tiga hal konkret.

**GAP-1 (integration retake-then-PASS → 1 cert):** GENUINE GAP. `RetakeServiceTests` membuktikan archive/reset/counting, TAPI **tidak ada satu pun test yang men-drive jalur lulus pasca-retake hingga sertifikat terbit**. Penyebab: `RetakeService.ExecuteAsync` HANYA mereset sesi (Status→Open, hapus responses); sertifikat di-issue jauh kemudian oleh `GradingService.GradeAndCompleteAsync` saat worker submit ujian-ulang. Test baru harus menjahit kedua jalur: `ExecuteAsync` → seed responses-benar → `GradeAndCompleteAsync` → assert `NomorSertifikat != null` tepat 1.

**GAP-2 (counting no-conflate Pre/Post):** **SUDAH ADA** — `RetakeServiceTests.Counting_PrePostSameTitle_NoConflate` (baris 336-358). CONTEXT D-02 memberi diskresi: "kalau ya, cukup tambah assert eksplisit." Rekomendasi: JANGAN duplikasi; cukup referensikan test existing di plan + (opsional) tambahkan satu assertion dalam test GAP-1 baru yang menegaskan counting tetap benar pasca-grade.

**GAP-3 (Playwright lifecycle penuh):** GENUINE GAP. Smoke 406/407 menguji per-surface dan **sengaja menghindari exam-taking flow**. Lifecycle 408 harus benar-benar mengeksekusi: seed sesi gagal (cooldown=0) → Results (skor+✓/✗ tanpa kunci) → klik "Ujian Ulang" → modal → POST RetakeExam → redirect StartExam → jawab benar → submit → lulus → cert# muncul. Helper exam-taking SUDAH ADA dan teruji di `exam-taking.spec.ts` Flow A (reuse `submitExamTwoStep`, label-by-text shuffle-safe).

**Primary recommendation:** 3 plan terfokus — (1) satu xUnit integration test baru `RetakeThenPassCertTests` (mirror `RetakeServiceFixture`, gabung ExecuteAsync+GradeAndCompleteAsync, assert 1 cert + counting), (2) satu Playwright spec baru `retake-lifecycle-408.spec.ts` (seed gagal cooldown=0 + reuse helper exam-taking), (3) plan secure-phase 408 dengan `<threat_model>` konsolidasi 406+407. JANGAN sentuh test yang sudah hijau.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Aturan kelayakan retake (pure) | Helper (`RetakeRules`) | — | Sudah pure + unit-tested; 408 hanya konfirmasi tak regresi |
| Reset+archive engine | Service (`RetakeService`) | DB (SQL real) | ExecuteAsync = mutasi DB transaksional; integration butuh SQL real |
| Issue sertifikat | Service (`GradingService`) | DB (unique index + retry) | Cert lahir di GradeAndCompleteAsync, BUKAN di RetakeService — kunci GAP-1 |
| Endpoint worker self-service | Controller (`CMPController.RetakeExam`) | Service | RBAC/CSRF/ownership + re-cek server-side; sudah ter-test endpoint-level |
| Lifecycle gagal→ulang→lulus→cert | UI/Browser (Razor + JS) | Controller + Service + DB | Razor/JS/SignalR WAJIB real-browser (lesson 354/413); GAP-3 |
| Threat consolidation milestone | Doc (secure-phase) | — | Gerbang formal D-03; bukan kode baru |

## Standard Stack

Stack test SUDAH terpasang dan stabil — 408 tidak menambah dependency. Versi diverifikasi dari `HcPortal.Tests/*.csproj` dan `tests/playwright.config.ts`.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xunit | 2.9.3 | Unit + integration test framework | [VERIFIED: HcPortal.Tests.csproj] — framework existing seluruh suite |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner host | [VERIFIED: csproj] |
| xunit.runner.visualstudio | 3.0.1 | VS/dotnet test discovery | [VERIFIED: csproj] |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQL integration (disposable DB @SQLEXPRESS) | [VERIFIED: csproj] — dipakai `RetakeServiceFixture` |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Unit test in-memory (pure helpers) | [VERIFIED: csproj] |
| @playwright/test | (existing) | E2E lifecycle browser | [VERIFIED: tests/playwright.config.ts] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.Logging.Abstractions (NullLogger) | net8 | Logger stub di service test | [VERIFIED: RetakeServiceTests.cs:111] — `NullLogger<RetakeService>.Instance` |
| NoOpHubContext (hand-stub, in-repo) | — | IHubContext stub (tak ada Moq di project) | [VERIFIED: RetakeServiceTests.cs:70-100] — reuse |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Real-SQL integration (GAP-1) | EF-InMemory | InMemory TIDAK enforce unique index `IX_AssessmentSessions_NomorSertifikat` (anti-double-cert guard) → test palsu-hijau. WAJIB SQL real (lesson v32.3 Phase 404). `[ASSUMED]` ditolak. |
| Playwright lifecycle (GAP-3) | xUnit-only | Razor/JS/SignalR + cert# visual TIDAK ter-cover unit (lesson 354/413). Lifecycle WAJIB real-browser. |
| Spec baru `retake-lifecycle-408.spec.ts` | Extend `retake-worker-407.spec.ts` | 407 spec sudah hijau + per-surface; menambah exam-taking ke sana mencampur concern + risiko regresi. Spec baru = isolasi bersih. |

**Installation:** Tidak ada. `dotnet test` + `npx playwright test` dengan toolchain existing.

**Version verification:** Tidak ada package baru untuk diverifikasi via `npm view`/`dotnet add` — semua sudah terpasang [VERIFIED: csproj + playwright.config.ts].

## Architecture Patterns

### System Architecture Diagram (Jalur yang DIBUKTIKAN 408)

```
GAP-1 (xUnit integration, real-SQL @SQLEXPRESS):
  Seed sesi GAGAL (Completed, IsPassed=false, AllowRetake, cooldown=0)
        │
        ▼
  RetakeService.ExecuteAsync ──► claim-atomik Status→Open + snapshot archive + delete responses
        │
        ▼
  [simulasi ambil-ulang] seed responses BENAR + restore assignment/package
        │
        ▼
  GradingService.GradeAndCompleteAsync ──► hitung skor → isPassed=true
        │                                      │
        │                                      ▼
        │                         (GenerateCertificate && isPassed)
        │                         retry 3× WHERE NomorSertifikat==null  ◄── anti-double-cert guard
        ▼                                      │
  ASSERT: COUNT(NomorSertifikat != null) == 1  ◄── core GAP-1
  ASSERT: counting (UserId,Title,Category) Pre≠Post tak konflasi

GAP-3 (Playwright lifecycle, real-browser @5270):
  seed sesi GAGAL (cooldown=0)  ──►  login coachee  ──►  /CMP/Results/{id}
        │                                                       │
        │                                       skor + ✓/✗ TANPA kunci (ShowWrongFlagsOnly)
        ▼                                                       │
  klik #btnRetake ──► #retakeConfirmModal ──► POST CMP/RetakeExam (antiforgery)
        │
        ▼
  redirect StartExam ──► jawab BENAR (label-by-text) ──► submitExamTwoStep ──► /CMP/Results
        │
        ▼
  ASSERT: badge LULUS + cert# "KPB/xxx/VI/2026" muncul (1 cert)
        │
        ▼ (cleanup) db.restore snapshot (SEED_WORKFLOW)
```

### Recommended File Structure (additive — JANGAN ubah existing)
```
HcPortal.Tests/
└── RetakeThenPassCertTests.cs      # GAP-1 baru: [Trait("Category","Integration")], IClassFixture<RetakeServiceFixture>
tests/
├── e2e/
│   └── retake-lifecycle-408.spec.ts   # GAP-3 baru: mode serial + db.backup/restore
└── sql/
    └── retake-lifecycle-408-seed.sql  # seed 1 sesi gagal cooldown=0 + 1 paket 2-3 soal MC
```

### Pattern 1: Reuse RetakeServiceFixture untuk GAP-1
**What:** Disposable DB `HcPortalDB_Test_{guid}` @SQLEXPRESS, `MigrateAsync` full chain (incl `AddRetakeColumnsAndArchive`), drop on dispose.
**When to use:** Integration test apa pun yang menyentuh DB real retake.
**Example:**
```csharp
// Source: HcPortal.Tests/RetakeServiceTests.cs:34-67, 102-111 [VERIFIED]
[Trait("Category", "Integration")]
public class RetakeThenPassCertTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeThenPassCertTests(RetakeServiceFixture f) => _fixture = f;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
    private static RetakeService NewRetake(ApplicationDbContext ctx) =>
        new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);
    // GradingService butuh ctor-deps tambahan (lihat Open Question OQ-1).
}
```
**Catatan:** `RetakeServiceFixture`, `NoOpHubContext`, dan seed helper (`SeedUserAsync`, `SeedSessionAsync`, `SeedPackageWithResponsesAsync`) berada di assembly test yang SAMA (`namespace HcPortal.Tests`) — bisa langsung dipakai/disalin. `SeedSessionAsync` perlu `GenerateCertificate=true` (default model false) agar cert path aktif — tambahkan param.

### Pattern 2: Cert path = GradingService, BUKAN RetakeService
**What:** `RetakeService.ExecuteAsync` tidak pernah issue cert — hanya reset. Cert lahir di `GradingService.GradeAndCompleteAsync` step 6.
**Example (anti-double-cert guard — invariant yang dibuktikan GAP-1):**
```csharp
// Source: Services/GradingService.cs:285-312 [VERIFIED]
if (session.GenerateCertificate && isPassed) {
    // retry 3× + WHERE NomorSertifikat == null (idempotent, anti dobel)
    await _context.AssessmentSessions
        .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
        .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
}
// Format cert#: KPB/{seq:D3}/{RomanMonth}/{year}  e.g. "KPB/005/VI/2026"
// Source: Helpers/CertNumberHelper.cs:20-21 [VERIFIED]
```

### Pattern 3: Playwright exam-taking reuse (GAP-3)
**What:** Helper kanonik exam-taking sudah teruji di Flow A.
**Example:**
```typescript
// Source: tests/e2e/exam-taking.spec.ts:103-165 + tests/e2e/helpers/examTypes.ts:327-337 [VERIFIED]
// Jawab benar (shuffle-safe: pilih label BY TEXT, bukan positional nth):
await qCard.locator('label[id^="lbl_"]').filter({ hasText: correctText }).first().click();
// Submit dua langkah (reviewSubmitBtn → ExamSummary → Kumpulkan Ujian dialog):
import { submitExamTwoStep } from './helpers/examTypes';   // → menanti /CMP/Results
// Login: import { login } from '../helpers/auth';  await login(page, 'coachee');  [VERIFIED: tests/helpers/auth.ts]
```

### Anti-Patterns to Avoid
- **Menulis ulang test hijau:** `RetakeRulesTests`/`RetakeArchiveBuilderTests`/`RetakeServiceTests`/`RiwayatUnifierTests`/`RetakeExamEndpointTests` sudah lengkap. Plan 408 hanya RUN ulang (regresi) + tambah GAP. Duplikasi = waste + risiko drift.
- **Menduplikasi counting no-conflate:** sudah ada (`Counting_PrePostSameTitle_NoConflate`). Cukup referensi.
- **EF-InMemory untuk cert/counting:** unique index + filtered `WHERE NomorSertifikat==null` tak ter-enforce InMemory → palsu-hijau. WAJIB SQL real.
- **Positional `.nth()` untuk pilih jawaban:** shuffle opsi default ON (v27.0) → posisi acak. Pilih label by TEXT.
- **Lifecycle e2e tanpa db.backup/restore:** seed menulis DB lokal → WAJIB snapshot→restore (SEED_WORKFLOW, CLAUDE.md).
- **Hard-sleep wall-clock:** gunakan `waitForURL`/`waitForFunction` event-driven (pola Flow G/H).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Disposable test DB | Setup/teardown SQL manual | `RetakeServiceFixture` existing | Sudah handle MigrateAsync full-chain + drop + chain-break diagnostics |
| IHubContext mock | Tambah Moq/NSubstitute | `NoOpHubContext` existing | Project test sengaja tanpa Moq; hand-stub sudah ada |
| Cert number generation | Format string sendiri | `CertNumberHelper.Build` | Format kanonik + GetNextSeq + duplicate-key detect |
| Cert issuance di test | Set NomorSertifikat manual | `GradingService.GradeAndCompleteAsync` | Membuktikan guard ASLI (retry+WHERE null), bukan jalur palsu |
| Exam submit (e2e) | Klik form manual | `submitExamTwoStep` helper | reviewSubmit→ExamSummary→Kumpulkan + dialog accept teruji |
| DB snapshot (e2e) | sqlcmd backup ad-hoc | `helpers/dbSnapshot.ts` (`backup/restore/execScript/queryScalar/queryString`) | SEED_WORKFLOW compliant, default backup path resolve |
| Login (e2e) | Isi form login manual | `helpers/auth.ts login(page,'coachee'\|'hc')` | Kredensial dev terpusat di `accounts.ts` |

**Key insight:** Seluruh infrastruktur test retake sudah dibangun di fase 405-407. 408 = komposisi (jahit jalur lulus+cert) + secure gate, bukan pembangunan ulang.

## Runtime State Inventory

Phase 408 adalah test/UAT murni — **tidak ada rename/refactor/migrasi produksi**. Yang relevan hanya state test-seeding:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Seed sesi gagal e2e di `HcPortalDB_Dev` (prefix `[RETAKE408]`); fixture GAP-1 di disposable DB `HcPortalDB_Test_{guid}` | e2e: db.backup→restore (SEED_WORKFLOW); xUnit: auto-drop on dispose. None permanen. |
| Live service config | None — verified (test/UAT, tak sentuh n8n/Datadog/Tailscale) | None |
| OS-registered state | None — verified (tak ada Task Scheduler/pm2) | None |
| Secrets/env vars | `E2E_BASE_URL=http://localhost:5270` (override config default 5277) — env run-time, bukan secret; `Authentication__UseActiveDirectory=false` saat dotnet run lokal | Set saat menjalankan, JANGAN commit |
| Build artifacts | None — 0 migration, tak ada package rename | None (full-suite run = regresi check) |

## Common Pitfalls

### Pitfall 1: Mengira RetakeService meng-issue cert (GAP-1 salah-arah)
**What goes wrong:** Test GAP-1 cuma panggil `ExecuteAsync` lalu assert cert → cert TIDAK akan pernah ada (ExecuteAsync hanya reset).
**Why it happens:** Nama "retake-then-pass" menyiratkan satu langkah; nyatanya cert lahir di langkah grading terpisah.
**How to avoid:** Test harus menjahit `ExecuteAsync` → re-seed responses BENAR + restore assignment/package → `GradeAndCompleteAsync(session)` → baru assert `NomorSertifikat`. `GradeAndCompleteAsync` grade dari DB (bukan form POST).
**Warning signs:** Assert cert==null padahal isPassed; lupa restore package/assignment yang dihapus ExecuteAsync.

### Pitfall 2: Cert path butuh GenerateCertificate=true + AssessmentType != PreTest
**What goes wrong:** Sesi seed dengan `GenerateCertificate` default false → cert tak terbit walau lulus.
**Why it happens:** `SeedSessionAsync` existing tak set `GenerateCertificate`; model default false; guard `if (session.GenerateCertificate && isPassed)`.
**How to avoid:** Seed `GenerateCertificate=true` + `AssessmentType="PostTest"` (graded, non-PreTest, retakeable). Cooldown=0.
**Warning signs:** isPassed true tapi NomorSertifikat null.

### Pitfall 3: GradingService ctor-deps tidak diketahui (blocker GAP-1)
**What goes wrong:** Tak bisa instansiasi `GradingService` di test karena dependency belum dipetakan.
**Why it happens:** RESEARCH ini belum membaca ctor `GradingService` lengkap (lihat OQ-1).
**How to avoid:** Plan WAJIB baca `Services/GradingService.cs` ctor dulu; kemungkinan butuh `ApplicationDbContext` + logger + (mungkin) notification/SignalR/UserManager. Pakai NullLogger + NoOp stub pola existing. Bila deps berat → alternatif Opsi-B di OQ-1.
**Warning signs:** Compile error ctor; deps yang men-deref objek non-null saat grade.

### Pitfall 4: Counting no-conflate sudah ada — jangan duplikasi (D-02)
**What goes wrong:** Plan membuat test counting baru yang identik `Counting_PrePostSameTitle_NoConflate`.
**How to avoid:** Referensi test existing; tambah assert counting di test GAP-1 hanya jika menambah nilai (mis. memastikan grade pasca-retake tak menggeser counting).
**Warning signs:** Dua test dengan body hampir identik.

### Pitfall 5: Lifecycle e2e — cooldown harus 0 + sesi harus punya soal nyata
**What goes wrong:** Tombol "Ujian Ulang" disabled (cooldown aktif) ATAU StartExam tak punya soal untuk dijawab.
**Why it happens:** Seed 407 fixture A punya soal current tapi cooldown logic; seed lifecycle harus cooldown=0 + paket+soal lengkap supaya retake→StartExam→jawab→lulus.
**How to avoid:** Seed `RetakeCooldownHours=0`, `CompletedAt` lampau, AllowRetake=1, MaxAttempts≥2, + 1 paket 2-3 soal MC dengan opsi benar diketahui (untuk jawab benar pasca-retake). PassPercentage rendah agar jawab benar = lulus.
**Warning signs:** #btnRetake disabled; StartExam kosong; tak pernah lulus.

### Pitfall 6: Razor/JS lifecycle WAJIB real-browser (lesson 354/413)
**What goes wrong:** Grep+build hijau tapi `pageerror` (ReferenceError) meng-abort handler-attach di browser nyata.
**How to avoid:** Spec lifecycle WAJIB `page.on('pageerror')` assert empty di tiap langkah (pola 407 spec). MEMORY: bug produksi `monFlashRow` ReferenceError ketangkap HANYA via real-browser Playwright (Phase 413).
**Warning signs:** Modal tak buka; submit tak jalan; uncaught error di console.

### Pitfall 7: eraRetakeArchives counting diduplikasi 3 tempat (IN-03 backlog)
**What goes wrong:** Logika `eraRetakeArchives` ada di controller `Results` + `CanRetakeAsync` + `ExecuteAsync` (AR-407-02). Drift bila salah satu diubah.
**How to avoid:** 408 = test phase, JANGAN refactor (out-of-scope per CONTEXT). Cukup pastikan test menutup ketiga jalur (sudah: `RetakeServiceTests` + `RetakeExamEndpointTests` + e2e). Catat sebagai accepted-risk di secure-phase.
**Warning signs:** Plan mencoba refactor single-source (scope creep).

## Code Examples

### GAP-1 skeleton (xUnit integration, jahit ExecuteAsync + GradeAndCompleteAsync)
```csharp
// Mengikuti pola RetakeServiceTests (real-SQL). PSEUDO — plan finalisasi ctor GradingService (OQ-1).
[Fact]
public async Task RetakeThenPass_IssuesExactlyOneCertificate()
{
    await using var ctx = NewCtx();
    var userId = await SeedUserAsync(ctx);                            // helper existing
    // Seed gagal + GenerateCertificate=true + PostTest + cooldown 0:
    var sid = await SeedSessionAsync(ctx, userId, "CertTitle", "Post",
        status:"Completed", isPassed:false, allowRetake:true, maxAttempts:2,
        cooldownHours:0, completedAt:DateTime.UtcNow.AddDays(-2),
        assessmentType:"PostTest" /*, generateCertificate:true (param baru) */);
    var qIds = await SeedPackageWithResponsesAsync(ctx, sid, 3);      // semua opsi benar dipilih

    await NewRetake(ctx).ExecuteAsync(sid, userId, "Tester", "RetakeAssessment", "worker_retake");

    // simulasi ambil-ulang LULUS: re-seed assignment/package + responses BENAR, set Completed.
    // (ExecuteAsync menghapus responses/assignment; restore untuk grade.)
    // ... re-seed ...
    var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
    await NewGrading(ctx).GradeAndCompleteAsync(session);            // step 6: issue cert (retry+WHERE null)

    await using var verify = NewCtx();
    var certCount = await verify.AssessmentSessions
        .CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
    Assert.Equal(1, certCount);                                      // CORE: tepat 1 cert
}
```

### GAP-3 skeleton (Playwright lifecycle)
```typescript
// Pola: retake-worker-407.spec.ts (serial + db.backup/restore) + exam-taking.spec.ts (take-and-pass).
test.describe.configure({ mode: 'serial' });
test('lifecycle: gagal → Ujian Ulang → lulus → 1 cert', async ({ page }) => {
  const errors: string[] = []; page.on('pageerror', e => errors.push(e.message));
  await login(page, 'coachee');
  await page.goto(`/CMP/Results/${sidFailed}`);
  // 1. skor + verdict tanpa kunci (ShowWrongFlagsOnly):
  await expect(page.locator('[role="status"]', { hasText: 'Kunci jawaban disembunyikan' })).toBeVisible();
  // 2. retake → modal → submit:
  await page.locator('#btnRetake').click();
  await expect(page.locator('#retakeConfirmModal')).toBeVisible();
  await Promise.all([ page.waitForURL('**/CMP/StartExam/**'),
    page.locator('#retakeConfirmModal button[type="submit"]').click() ]);
  // 3. jawab BENAR (label by text) → submit:
  // ... pilih opsi benar per qcard (shuffle-safe) ...
  await submitExamTwoStep(page);                                      // → /CMP/Results
  // 4. assert LULUS + cert#:
  await expect(page.locator('body')).toContainText('LULUS');
  await expect(page.locator('body')).toContainText(/KPB\/\d+\/[IVX]+\/\d{4}/);
  expect(errors).toEqual([]);                                         // lesson 413
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Cert di tabel Certificates terpisah | Cert = `AssessmentSession.NomorSertifikat` scalar + unique index | (sejak Phase 227 CLEN-04) | GAP-1 assert kolom session, BUKAN tabel terpisah |
| Positional `.nth()` pilih opsi | Label by-text (shuffle-safe) | v27.0 shuffle default ON | e2e WAJIB by-text |
| InMemory cukup untuk semua test | Real-SQL untuk index/cert/counting | v32.3 P404 lesson | GAP-1 WAJIB SQL real |
| Grep+build cukup verifikasi Razor | Real-browser Playwright wajib | Phase 354/413 | GAP-3 WAJIB browser + pageerror assert |

**Deprecated/outdated:** Tidak ada di scope 408.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `GradeAndCompleteAsync` dapat dipanggil langsung di test dengan deps stub (NullLogger + NoOp) | Pitfall 3 / Code Examples | Sedang — bila ctor butuh deps berat (UserManager/IHubContext/notification real), GAP-1 perlu Opsi-B (lihat OQ-1). Plan WAJIB baca ctor dulu. |
| A2 | Re-seed responses+assignment pasca-ExecuteAsync cukup untuk membuat `GradeAndCompleteAsync` menghasilkan isPassed=true | Code Examples GAP-1 | Sedang — perlu konfirmasi field yang dibaca GradingService (ScoreValue, IsCorrect option, status guard). Plan baca GradingService grade-loop. |
| A3 | `SeedSessionAsync` perlu tambah param `generateCertificate` (model default false) | Pitfall 2 | Rendah — verified model field exists (`GenerateCertificate` di seed 407 SQL kolom). Hanya perlu expose di helper C#. |

**Catatan:** Tidak ada assumed-fact tentang compliance/retention/security standard. Semua klaim teknis lain [VERIFIED] dari kode.

## Open Questions

1. **OQ-1: Apa ctor `GradingService` dan bisakah diinstansiasi di test dengan stub ringan?**
   - What we know: cert path = `GradeAndCompleteAsync` step 6 [VERIFIED: GradingService.cs:285-312]; grade dari DB; tak panggil SignalR/_cache (dibiarkan di controller per docblock :52-53).
   - What's unclear: daftar lengkap ctor-deps (logger pasti; mungkin notification service / UserManager).
   - Recommendation: Plan 408 (task xUnit) WAJIB baca `Services/GradingService.cs` ctor + grade-loop SEBELUM menulis test. Bila deps berat → **Opsi-B:** drive cert via endpoint controller (`SubmitExam`/grade) atas RetakeServiceFixture (pola `RetakeExamEndpointTests` controller-over-fixture), atau **Opsi-C:** verifikasi 1-cert via Playwright lifecycle (GAP-3) + integration hanya assert reset+counting (cert visual sudah di smoke 406). CONTEXT D-02 mengunci jalur xUnit untuk anti-double-cert, jadi Opsi-B diutamakan jika ctor berat.

2. **OQ-2: Apakah cleanup lifecycle e2e cukup mengandalkan db.restore, atau perlu seed user worker fixture?**
   - What we know: seed 407 pakai user `rino.prasetyo@pertamina.com` existing (pre-condition THROW bila absen) [VERIFIED: retake-worker-407-seed.sql:79-81].
   - What's unclear: apakah DB lokal saat run 408 punya user itu + coachee mapping.
   - Recommendation: reuse pre-condition pattern + jalankan `global.setup.ts` (login state). Sama seperti 407 — sudah terbukti.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| SQL Server (SQLEXPRESS) | GAP-1 integration (disposable DB) | Asumsi ✓ (dipakai 405/407 integration) | — | Tak ada — integration di-skip via `--filter "Category!=Integration"` di CI SQL-less |
| HcPortalDB_Dev lokal | GAP-3 e2e seed/restore | Asumsi ✓ | — | Tak ada — e2e butuh DB lokal |
| App running @5270 | GAP-3 e2e | Run-time (developer start) | — | `E2E_BASE_URL` override |
| Playwright browsers | GAP-3 | Asumsi ✓ (suite e2e existing jalan) | — | `npx playwright install` |
| .NET 8 SDK | semua | ✓ | net8.0 | — |

**Missing dependencies with no fallback:** None terkonfirmasi missing — semua tool sudah dipakai suite v32.4 existing. Verifikasi run-time: `dotnet build` + `dotnet test` + `npx playwright test`.

**Missing dependencies with fallback:** Integration test SQL-less → otomatis skip (Trait filter).

## Validation Architecture

> nyquist_validation = true (config.json) → section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (unit+integration) + @playwright/test (e2e) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` ; `tests/playwright.config.ts` (baseURL default 5277, override `E2E_BASE_URL=http://localhost:5270`) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (unit only, SQL-less, < ~30s) |
| Full suite command | `dotnet test` (incl Integration @SQLEXPRESS) + `E2E_BASE_URL=http://localhost:5270 npx playwright test --workers=1` (app @5270 running) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RTK-14 (unit RetakeRules) | semua cabang CanRetake + ShouldHideRetakeToggle + ResolveReviewMode | unit | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ✅ (22 Fact) |
| RTK-14 (unit ArchiveBuilder) | snapshot verdict via IsQuestionCorrect + essay full-text | unit | `dotnet test --filter "FullyQualifiedName~RetakeArchiveBuilderTests"` | ✅ (4 Fact) |
| RTK-14 (unit Riwayat) | unify attempt DESC + grouping strict | unit | `dotnet test --filter "FullyQualifiedName~RiwayatUnifierTests"` | ✅ (6 Fact) |
| RTK-14 (integ RetakeService) | claim-atomik, snapshot-before-delete, D-01 counting, no-conflate | integration | `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` | ✅ (8 Fact, incl `Counting_PrePostSameTitle_NoConflate`) |
| RTK-14 (endpoint worker) | IDOR Forbid / not-eligible redirect / success clear-token | integration | `dotnet test --filter "FullyQualifiedName~RetakeExamEndpointTests"` | ✅ (3 Fact) |
| **RTK-14 (integ retake→pass→1 cert)** | retake → grade lulus → tepat 1 NomorSertifikat (anti-double-cert) | integration | `dotnet test --filter "FullyQualifiedName~RetakeThenPassCertTests"` | ❌ **Wave 0 (GAP-1)** |
| **RTK-14 (e2e lifecycle penuh)** | gagal→skor+✓/✗ no-key→Ujian Ulang→modal→StartExam→jawab benar→lulus→cert# | e2e | `npx playwright test retake-lifecycle-408.spec.ts --workers=1` | ❌ **Wave 0 (GAP-3)** |
| RTK-14 (smoke per-surface) | config 406 / riwayat HC 406 / worker 407 leak-safety | e2e | `npx playwright test retake-config-406 riwayat-hc-406 retake-worker-407 --workers=1` | ✅ (6+5+6) |
| RTK-14 (security gate) | RBAC/CSRF/cooldown-cap revalidation/no-leak konsolidasi | secure-phase | `gsd-secure-phase 408` (doc gate) | ❌ **Wave 0 (GAP secure)** |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (unit quick) + build.
- **Per wave merge:** full `dotnet test` (incl Integration @SQLEXPRESS) + spec e2e baru @5270.
- **Phase gate:** seluruh suite hijau (598+ existing tak regresi + GAP-1) + lifecycle e2e hijau + secure 408 `threats_open:0` sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/RetakeThenPassCertTests.cs` — covers RTK-14 (retake→pass→1 cert; reuse RetakeServiceFixture; resolve OQ-1 ctor GradingService).
- [ ] `tests/e2e/retake-lifecycle-408.spec.ts` + `tests/sql/retake-lifecycle-408-seed.sql` — covers RTK-14 (lifecycle penuh; seed gagal cooldown=0 + paket+soal; db.backup/restore).
- [ ] Plan secure-phase 408 dengan `<threat_model>` konsolidasi (lihat Security Domain).
- Framework install: NONE — toolchain existing lengkap (xUnit + Playwright + EF SqlServer).

## Security Domain

> security_enforcement diasumsikan enabled (tidak ada `false` eksplisit di config). CONTEXT D-03 secara eksplisit meminta secure-phase 408 dengan `<threat_model>` block konsolidasi. Plan 408 WAJIB sertakan block ini.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Identity + `[Authorize]`; worker login (coachee) |
| V3 Session Management | yes | TempData token `TokenVerified_{id}` cleared pasca-retake (T-407-token) |
| V4 Access Control | yes | Ownership `assessment.UserId == effectiveUser.Id` → Forbid (IDOR, T-407-idor); RBAC Admin/HC untuk config (RTK-04) |
| V5 Input Validation | yes | `id` route int; clamp range MaxAttempts/cooldown (server, RTK-04); CanRetakeAsync server-authoritative |
| V6 Cryptography | no | Tidak ada operasi kripto baru di retake (cert# = sequence, bukan crypto) |
| V7/V8 (Logging/Data Protection) | partial | Audit `RetakeAssessment`/`ResetAssessment` via AuditLogService; archive snapshot verdict-only (no key leak) |

### Known Threat Patterns for ASP.NET MVC retake surface (konsolidasi 406+407 untuk `<threat_model>` 408)
| Pattern | STRIDE | Standard Mitigation | Sumber existing |
|---------|--------|---------------------|-----------------|
| IDOR — worker memicu retake sesi orang lain | Elevation / Access Control | `if (assessment.UserId != user.Id) return Forbid();` sebelum mutasi | T-407-idor (closed), `RetakeExam_NonOwner_ReturnsForbid` [VERIFIED] |
| CSRF pada POST RetakeExam | Tampering | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` modal | T-407-csrf (closed) |
| Bypass cooldown/cap via DevTools | Tampering / Logic Bypass | `CanRetakeAsync` server-authoritative SEBELUM ExecuteAsync; countdown JS UX-only | T-407-bypass (closed), `RetakeExam_NotEligible_...` [VERIFIED] |
| Answer-key leak saat retake-eligible | Information Disclosure | `ShowWrongFlagsOnly` tier tak render kunci; archive verdict-only | T-407-leak (closed), e2e leak-safety 407 [VERIFIED] |
| Stale token re-entry | Tampering / Auth Bypass | `TempData.Remove($"TokenVerified_{id}")` pasca-ExecuteAsync | T-407-token (closed) |
| Double-submit double-archive | Tampering / Race | claim-atomik (Open→no-op; ExecuteUpdate WHERE Status NOT IN(Cancelled,Open)) | T-407-doublearchive (accept) |
| XSS user-content riwayat | Tampering / XSS | Razor `@` auto-encode; ZERO Html.Raw di partial retake | T-407-xss (closed) |
| JS handler-abort (ReferenceError) | Availability | guard `if(!btn)return`; Playwright pageerror assert | T-407-jsabort (closed), lesson 413 |
| Config tamper (MaxAttempts/cooldown out-of-range) | Tampering | RBAC Admin/HC + clamp range server (RTK-04) | 406-SECURITY (admin surface) |
| Double-cert pada retake-then-pass | Tampering / Logic | unique index `IX_AssessmentSessions_NomorSertifikat` + retry 3× WHERE NomorSertifikat==null | GradingService.cs:285-312 [VERIFIED] — **dibuktikan GAP-1** |

**Disposisi 408:** Secure-phase 408 = gerbang FORMAL konsolidasi; tidak ada surface kode BARU (408 hanya test). `<threat_model>` block menegaskan seluruh threat 406+407 tetap closed/accepted tanpa regresi + 1 invariant cert-uniqueness yang kini di-cover GAP-1. Accepted-risk yang dibawa: AR-407-01 (double-archive race, dikelola service) + AR-407-02 (eraRetakeArchives duplikat 3-tempat, IN-03 backlog — JANGAN refactor di 408).

## Sources

### Primary (HIGH confidence — kode/test repo, dibaca langsung)
- `HcPortal.Tests/RetakeServiceTests.cs` (8 Fact, real-SQL fixture, counting no-conflate baris 336-358) [VERIFIED]
- `HcPortal.Tests/RetakeRulesTests.cs` (22 Fact) ; `RetakeArchiveBuilderTests.cs` (4 Fact) ; `RiwayatUnifierTests.cs` (6 Fact) ; `RetakeExamEndpointTests.cs` (3 Fact) [VERIFIED]
- `Services/RetakeService.cs` (ExecuteAsync reset-only, CanRetakeAsync counting) [VERIFIED]
- `Services/GradingService.cs:49-52, 285-312` (cert step 6, retry+WHERE null) [VERIFIED]
- `Helpers/CertNumberHelper.cs` (format KPB/seq/roman/year + duplicate-key detect) [VERIFIED]
- `Controllers/CMPController.cs:904-959 (StartExam), 2527-2555 (RetakeExam)` [VERIFIED]
- `tests/e2e/exam-taking.spec.ts` (Flow A take-and-pass-and-cert) + `tests/e2e/helpers/examTypes.ts:327-337 (submitExamTwoStep)` [VERIFIED]
- `tests/e2e/retake-worker-407.spec.ts` + `tests/sql/retake-worker-407-seed.sql` (pola serial + db.backup/restore + seed gagal) [VERIFIED]
- `tests/helpers/auth.ts` (login) ; `tests/helpers/dbSnapshot.ts` (backup/restore/execScript/queryScalar) [VERIFIED]
- `.planning/phases/407-.../407-SECURITY.md` (threat register T-407-* + AR-407-01/02) [VERIFIED]
- `.planning/config.json` (nyquist_validation:true, commit_docs:true) ; `HcPortal.Tests/*.csproj` (versi) [VERIFIED]
- `.planning/phases/408-test-uat/408-CONTEXT.md` (D-01/02/03) ; `.planning/REQUIREMENTS.md` (RTK-14) ; `.planning/ROADMAP.md` (Phase 408 SC 1-4) [CITED]

### Secondary (MEDIUM)
- MEMORY.md lessons (354/413 real-browser wajib; v32.3 P404 SQL-real wajib) — cross-confirmed dengan komentar test in-repo.

### Tertiary (LOW)
- None — semua klaim diverifikasi dari sumber primer in-repo. Tidak ada WebSearch (brownfield internal, tak perlu).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi dari csproj/playwright.config.ts (terpasang, bukan training).
- Existing coverage inventory: HIGH — tiap test file dibaca utuh; gap diidentifikasi by-absence.
- GAP-1 cert path: HIGH untuk mekanisme (GradingService dibaca), MEDIUM untuk eksekusi test (ctor GradingService = OQ-1).
- GAP-3 lifecycle: HIGH — helper exam-taking + seed pattern existing terbukti.
- Security domain: HIGH — threat register 407 dibaca verbatim.

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (brownfield stabil; re-cek bila GradingService/CMPController berubah)
