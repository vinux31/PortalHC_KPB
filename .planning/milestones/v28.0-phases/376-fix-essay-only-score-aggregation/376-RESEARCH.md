# Phase 376: Fix Essay-Only Score Aggregation - Research

**Researched:** 2026-06-14
**Domain:** ASP.NET Core MVC + EF Core grading-aggregation bug-fix (backend-only, no migration)
**Confidence:** HIGH (semua klaim diverifikasi langsung dari kode; root cause perlu konfirmasi runtime di SC1 — diagnose-first)

> Catatan bahasa: bagian user-facing (pesan endpoint, audit, dokumen handoff IT) WAJIB Bahasa Indonesia per CLAUDE.md. Bagian teknis riset ini dwibahasa demi presisi istilah kode.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Forward-fix + recompute tool untuk IT. Fix kode (forward path benar) + sediakan mekanisme recompute idempotent untuk repair baris essay-only lama yang `Score=0` padahal sudah dinilai+finalized. Developer verifikasi lokal; **eksekusi di DB Dev/Prod = tanggung jawab IT** (per CLAUDE.md — developer tak edit/push DB langsung). Tool diserahkan + di-flag ke IT.
- **D-02:** Tool = **endpoint admin** (POST) yang **me-reuse helper agregasi bersama** — bukan SQL script. Ekstrak logika agregasi skor (inline di `FinalizeEssayGrading` L3535-3564) ke **helper bersama/shared core** (pola kill-drift Phase 363/365), dipakai BERSAMA forward-path finalize DAN endpoint recompute. Endpoint: gated role HC/Admin + antiforgery + audit log + **idempotent** (hanya sentuh baris kandidat: essay-only/HasManualGrading, Status=Completed, Score=0, semua EssayScore terisi).
- **D-03:** Recompute repair **HANYA `Score` + `IsPassed`**. Sertifikat + penanda Proton `Origin="Exam"` + `NotifyIfGroupCompleted` **TIDAK** auto-terbit retroaktif/massal. Bila butuh sertifikat untuk baris repaired yang lulus, **HC re-trigger per-orang manual**. (Forward-path baru TIDAK berubah.)
- **D-04:** **Formula locked = persentase int**, persis L3564: `Score = (int)((double)totalScore / maxScore * 100)`. `totalScore` = Σ `EssayScore` (+ MC/MA auto bila mixed). `maxScore` = Σ `ScoreValue` soal. `IsPassed = Score >= PassPercentage`. **Satu formula, dua jalur**.
- **D-05:** Edge `maxScore=0` → **pertahankan fallback `Score=0`** + **tambah log warning**. **TIDAK** block finalize (no regresi).
- **D-06:** **Targeted + guard defensif tipis.** Fix tepat root-cause + guard defensif tipis + log di titik agregasi. Bukan minimal-murni maupun defensif-penuh.
- **D-07:** Test **dua jalur**: essay-only (rusak) + mixed (benar). xUnit (helper agregasi bersama, real-SQL bila perlu) + e2e `tests/e2e/exam-types.spec.ts` FLOW L6 un-`.fixme`. Recompute endpoint juga di-cover (idempotency + hanya-sentuh-kandidat).

### Claude's Discretion
- Lokasi & signature persis helper agregasi bersama (Services vs Helpers).
- Mekanisme deteksi baris kandidat di endpoint recompute (query predicate).
- Bentuk/route endpoint recompute + UI trigger (atau headless POST).
- Struktur fixture/test detail.
- Root-cause persis (dikonfirmasi saat eksekusi SC1 — diagnose-first).

### Deferred Ideas (OUT OF SCOPE)
- Retroaktif generate sertifikat + penanda Proton untuk baris repaired yang lulus (HC re-trigger manual bila perlu).
- Block finalize saat `maxScore=0` (ditolak D-05; bisa fase polish terpisah).
- Full revalidasi semua jalur agregasi (defensif penuh — ditolak D-06).
- Reviewed Todo `2026-06-11-one-time-cleanup-data-test-lokal` — tidak di-fold (beda konteks).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GRADE-01 | Finalize essay-only mengagregasi skor manual essay ke `AssessmentSessions.Score` (fix Score=0). Diagnose root cause dulu. | Root cause analysis §Root Cause Analysis: jalur `FinalizeEssayGrading` L3469-3669 + dependency `GetShuffledQuestionIds()`. Forward-fix = helper agregasi bersama (§Shared Helper Design). |
| GRADE-02 | Agregasi konsisten essay-only vs mixed; regression test kedua jalur. | §Architecture Patterns (satu formula, dua jalur) + §Validation Architecture (xUnit helper dua jalur + e2e L6 un-fixme). |
</phase_requirements>

## Summary

Bug: assessment **essay-only** — setelah HC menilai semua essay lalu memanggil `FinalizeEssayGrading` (Controllers/AssessmentAdminController.cs L3469-3669), `AssessmentSessions.Score` berakhir `0` walau status menjadi `Completed`/"Sudah Dinilai". Jalur mixed/auto-graded (MC+MA tanpa essay) sudah benar karena di-grade langsung oleh `GradingService.GradeAndCompleteAsync` cabang non-essay (L236-317), tidak pernah lewat `FinalizeEssayGrading`.

Investigasi kode menemukan bahwa baik `FinalizeEssayGrading` (L3512) MAUPUN `GradingService.GradeAndCompleteAsync` (L70) membaca question-set dari **`packageAssignment.GetShuffledQuestionIds()`** yang sama. `GetShuffledQuestionIds()` (Models/UserPackageAssignment.cs L60-71) mengembalikan **list kosong** saat `ShuffledQuestionIds` null/empty/JSON rusak `[VERIFIED: Models/UserPackageAssignment.cs L62-70]`. Bila kosong → `allQuestions` kosong (L3527-3530) → `maxScore=0` dan `totalScore=0` → `finalPercentage=0` (L3564). Sekaligus guard "semua essay dinilai" (`essayResponses.Any(r => r.EssayScore == null)`, L3523) **lolos vacuously** (`Any` pada set kosong = `false`), sehingga finalize tetap jalan dan menulis `Score=0` `[VERIFIED: kode L3514-3524]`.

**Namun** ada nuansa kritis: ShuffleEngine mengisi `ShuffledQuestionIds` dengan benar untuk essay-only MAUPUN mixed selama paket punya soal (§Root Cause Analysis). Karena itu hipotesis "shuffledIds kosong" **belum cukup membedakan** essay-only dari mixed pada kasus normal. Diagnosis WAJIB dikonfirmasi via repro lokal (SC1) — kandidat root cause di-ranking di §Root Cause Analysis. **Apapun root cause persisnya, fix tetap deterministik per D-06:** ekstrak agregasi inline (L3535-3564) ke helper bersama, tambahkan derivasi question-set yang robust (fallback ke seluruh soal package bila shuffledIds kosong) + log warning saat `maxScore=0`, dipakai forward path & recompute.

**Primary recommendation:** Ekstrak helper murni `AssessmentScoreAggregator` (Helpers/) yang menerima (questions + responses + passPercentage) → (totalScore, maxScore, percentage, isPassed); pakai di `FinalizeEssayGrading` (ganti L3535-3565) dan endpoint recompute baru. Di kedua call-site, derive question-set robust: `shuffledIds` bila tidak kosong, else fallback ke semua `PackageQuestion` milik paket assignment (D-06). Recompute endpoint = batch idempotent ala `BackfillProtonPenanda` (L3795-3895), set Score+IsPassed only (D-03), `ExecuteUpdateAsync` dengan WHERE-guard kandidat.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Agregasi skor (totalScore/maxScore/percentage/isPassed) | Helper murni (Helpers/) | — | Logika murni tanpa EF/DB → unit-testable tanpa DB (pola ShuffleEngine.cs / ShuffleToggleRules.cs); single source of truth kill-drift |
| Forward finalize essay | API/Backend (AssessmentAdminController) | Helper | Action HTTP gated role; deleguasi math ke helper |
| Recompute repair baris lama | API/Backend (AssessmentAdminController, endpoint baru) | Helper | Operasi batch admin; reuse helper yang sama; eksekusi di Dev/Prod oleh IT |
| Persistensi Score/IsPassed | Database (EF `ExecuteUpdateAsync`) | — | Atomic WHERE-guard (idempotent, race-safe) — pola eksisting L3569 |
| Derivasi question-set (shuffledIds vs fallback package) | API/Backend | — | Butuh EF query (`PackageQuestions`); helper tetap murni — caller menyuplai list |

## Standard Stack

Bug-fix di codebase existing. Tidak ada library baru. Stack terverifikasi dari kode:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET / ASP.NET Core MVC | net8.0 (existing) | Controller/action grading | Stack proyek `[VERIFIED: Controllers/*.cs, HcPortal.Tests memakai net8 EF]` |
| EF Core | existing | `ExecuteUpdateAsync` atomic idempotent | Pola eksisting L3569-3575 `[VERIFIED]` |
| xUnit | existing | Unit + integration test | `HcPortal.Tests` 58 file test `[VERIFIED: Glob HcPortal.Tests/*.cs]` |
| Playwright (TS) | existing | e2e `tests/e2e/*.spec.ts` | `[VERIFIED: tests/e2e/exam-types.spec.ts]` |

### Supporting (pola internal — reuse, jangan buat baru)
| Asset | Lokasi | Purpose |
|-------|--------|---------|
| `ShuffleEngine` | Helpers/ShuffleEngine.cs | Precedent helper murni (static, no EF) — pola untuk helper agregasi `[VERIFIED]` |
| `ShuffleToggleRules` | Helpers/ (Phase 374) | Precedent helper murni di-test via `ShuffleToggleRulesTests` (pure, no fixture) `[VERIFIED]` |
| `RecordCascadeFixture` | HcPortal.Tests/RecordCascadeIntegrationTests.cs L21-54 | Pola real-SQL disposable DB (`HcPortalDB_Test_{guid}`, MigrateAsync, EnsureDeleted) `[VERIFIED]` |
| `ProtonCompletionFixture` | HcPortal.Tests/ProtonCompletionServiceTests.cs L25 | Pola IClassFixture real-SQL (dipakai 8+ test class) `[VERIFIED]` |
| `BackfillProtonPenanda` | AssessmentAdminController.cs L3795-3895 | Precedent endpoint admin idempotent batch (counters, AnyAsync guard, warn-only audit) `[VERIFIED]` |
| `BulkBackfillAssessment` | AssessmentAdminController.cs L831-985 | Precedent endpoint admin transaksional batch + audit per-row `[VERIFIED]` |

**Installation:** Tidak ada. `dotnet build` + `dotnet test` (xUnit) + `npx playwright test` per CLAUDE.md Dev Workflow.

## Root Cause Analysis (diagnose-first — konfirmasi di SC1)

### Jalur data essay-only (terverifikasi dari kode)

```
Worker StartExam (CMPController.cs L971-1024)
  └─ packages.Any() → lazy-create UserPackageAssignment
       ShuffledQuestionIds = JSON(ShuffleEngine.BuildQuestionAssignment(...))   [L986-1004]
       SavedQuestionCount  = shuffledIds.Count                                   [L1008]
                          ▼
Worker SubmitExam (CMPController.cs L1490-1683)
  └─ shuffledIds = packageAssignment.GetShuffledQuestionIds()                    [L1573]
  └─ persist PackageUserResponses (MC) / essay TextAnswer via SignalR earlier
  └─ GradingService.GradeAndCompleteAsync(assessment)                           [L1648]
       └─ hasEssay==true → Status=PendingGrading, interim Score=MC+MA only       [L197-234]
          (essay-only → interim Score = 0; ini NORMAL & benar)
                          ▼
HC SubmitEssayScore per soal (AssessmentAdminController.cs L3428-3457)
  └─ response.EssayScore = score                                                [L3446]
                          ▼
HC FinalizeEssayGrading (AssessmentAdminController.cs L3469-3669)   ← BUG DI SINI
  └─ shuffledIds = packageAssignment.GetShuffledQuestionIds()                    [L3512]
  └─ essayQuestions  = PackageQuestions WHERE shuffledIds.Contains(Id) & Essay   [L3514-3516]
  └─ GUARD: essayResponses.Any(EssayScore==null)  → vacuous-pass jika kosong     [L3523]
  └─ allQuestions    = PackageQuestions WHERE shuffledIds.Contains(Id)           [L3527-3530]
  └─ foreach allQuestions: maxScore += ScoreValue; totalScore += EssayScore      [L3537-3562]
  └─ finalPercentage = maxScore>0 ? (int)(totalScore/maxScore*100) : 0           [L3564]   ← 0 bila kosong
  └─ ExecuteUpdateAsync Score=finalPercentage WHERE Status==PendingGrading       [L3569-3575]
```

### Kenapa mixed benar, essay-only tidak (terverifikasi)

`[VERIFIED]` Mixed **tanpa essay** (MC/MA saja) di-grade oleh `GradeAndCompleteAsync` cabang non-essay (L236-260): set `Status=Completed` + Score langsung, **tidak pernah** memanggil `FinalizeEssayGrading`. Jadi "mixed Score sudah benar" = jalur auto-graded murni.

`[VERIFIED]` Mixed **ber-essay** (MC+MA+essay) DAN essay-only **sama-sama** masuk `PendingGrading` lalu `FinalizeEssayGrading`. Keduanya membaca `shuffledIds` yang sama. **Implikasi:** jika root cause murni "shuffledIds kosong", mixed-ber-essay juga akan `Score=0`. Karena spec menyatakan hanya essay-only yang rusak, root cause persis WAJIB dikonfirmasi runtime.

### Bukti repro e2e (kuat)

`[VERIFIED: tests/e2e/exam-types.spec.ts L357-420]` FLOW L = **essay-only** (`qCards` count=1, satu soal Essay `Q_MARKER`). L4 worker isi essay→PendingGrading; L5 HC grade 80 + finalize; L6 `SELECT Score FROM AssessmentSessions WHERE Id={sessionId}` → expect 80, **fixmed** karena nyatanya 0 (komentar L413-414: "essay finalize leaves Score=0 despite per-question grade(80)+finalize... MA K5 scored 100 via same column OK"). Komentar mengonfirmasi `EssayScore` column terisi benar; yang gagal adalah **agregasi ke `AssessmentSessions.Score`**.

### Kandidat root cause (ranking — uji saat SC1)

| # | Hipotesis | Mekanisme | Bukti pendukung | Cara konfirmasi SC1 |
|---|-----------|-----------|-----------------|---------------------|
| H1 (utama) | `shuffledIds` kosong/divergen untuk baris essay-only ter-affected | `GetShuffledQuestionIds()` return `[]` → allQuestions kosong → maxScore=0 → Score=0; guard L3523 vacuous-pass | `[VERIFIED]` mekanisme kode lengkap; ShuffleEngine return `[]` saat paket kosong (L41/48/57/100/110) | Repro lokal FLOW L; query `SELECT ShuffledQuestionIds FROM UserPackageAssignments WHERE AssessmentSessionId={id}` — cek apakah `[]`/`{}`/null |
| H2 | `essayQuestions`/`allQuestions` filter `QuestionType=="Essay"` vs casing/null mismatch | Jika `QuestionType` tersimpan beda case/whitespace, switch L3540 default ke MC → essay tak skor | `QuestionType` adalah `string?` (Models/AssessmentPackage.cs L48); switch L3540 fallback `?? "MultipleChoice"` | Repro; cek `SELECT DISTINCT QuestionType FROM PackageQuestions` untuk soal terkait |
| H3 | `EssayScore` tak ter-load di `allResponses` (scope query) | `allResponses` (L3531-3533) ambil semua response session; essayResp.EssayScore.HasValue (L3559) | Query benar by `AssessmentSessionId`; risiko rendah | Repro; inspect `allResponses` count + EssayScore |
| H4 | Race/replay menulis Score=0 via path lain (AkhiriUjian re-grade) | `GradeAndCompleteAsync` essay-branch set interim Score=0; jika FinalizeEssayGrading no-op (race), Score tetap 0 | `[VERIFIED]` AkhiriUjian (L4050-4109) panggil GradeAndCompleteAsync; race-guard L3577 | Repro; cek apakah finalize `rowsAffected>0` |
| H5 (ditolak sbg utama) | Hook Proton 358 menimpa Score | Hook L3643-3655 hanya untuk `Category=="Assessment Proton"` + isPassed; tak set Score | `[VERIFIED]` hook tak menyentuh `Score` | N/A untuk assessment non-Proton |

> **Diagnose-first discipline (D-06, CONTEXT specifics):** JANGAN tulis fix sebagai asumsi H1 saja. SC1 = repro lokal + identifikasi root cause persis. Fix helper + fallback derivasi question-set menetralkan H1/H2 sekaligus; guard+log menetralkan H4 (sinyal anomali). Dokumentasikan temuan SC1 di SUMMARY plan.

## Architecture Patterns

### System Architecture Diagram (fix target)

```
┌──────────────────────────────────────────────────────────────────────┐
│ FORWARD PATH (D-04 formula, satu jalur)                                │
│                                                                        │
│  HC FinalizeEssayGrading(sessionId)  [AssessmentAdminController]       │
│    │                                                                   │
│    ├─ load packageAssignment → derive questionIds (ROBUST):            │
│    │     shuffledIds = GetShuffledQuestionIds()                        │
│    │     IF empty → fallback: PackageQuestions of assignment package   │  ← D-06 fix
│    │     + log warning saat fallback / maxScore==0                     │  ← D-05
│    │                                                                   │
│    ├─ load questions(+Options) + responses                            │
│    │                                                                   │
│    ├─ AssessmentScoreAggregator.Compute(questions, responses, pass%)  │  ← HELPER BERSAMA
│    │     → (totalScore, maxScore, percentage, isPassed)               │     (Helpers/, murni)
│    │                                                                   │
│    └─ ExecuteUpdateAsync Score+Status+IsPassed WHERE Status=Pending    │  ← existing L3569 (preserve)
│       + cert/Proton/notif side-effects (UNCHANGED — D-03 forward utuh) │
└──────────────────────────────────────────────────────────────────────┘
                              │ reuse helper (single source of truth)
                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│ RECOMPUTE PATH (repair baris lama — handoff IT, D-01/D-02/D-03)        │
│                                                                        │
│  POST RecomputeEssayScores  [AssessmentAdminController, gated]         │
│    │  [Authorize HC/Admin] + [ValidateAntiForgeryToken]               │
│    │                                                                   │
│    ├─ query KANDIDAT: Status=Completed AND HasManualGrading           │  ← D-02 predicate
│    │     AND Score==0 AND (semua EssayScore terisi)                   │
│    │                                                                   │
│    ├─ foreach kandidat: derive questionIds (robust, sama spt forward) │
│    │     AssessmentScoreAggregator.Compute(...) → percentage,isPassed │  ← HELPER SAMA
│    │     ExecuteUpdateAsync Score+IsPassed ONLY WHERE Id=.. & Score=0  │  ← D-03 + idempotent
│    │                                                                   │
│    └─ NO cert / NO Proton Origin / NO NotifyIfGroupCompleted (D-03)   │
│       + audit log batch (warn-only) + counters response               │  ← pola BackfillProtonPenanda
└──────────────────────────────────────────────────────────────────────┘
```

### Recommended Helper Placement & Signature

```
Helpers/
└── AssessmentScoreAggregator.cs   # static, murni, no EF — pola ShuffleEngine.cs
```

```csharp
// Helpers/AssessmentScoreAggregator.cs  (DISCRETION — signature usulan, planner finalkan)
// Murni: hanya System/Linq/HcPortal.Models. Unit-testable tanpa DB.
namespace HcPortal.Helpers
{
    public readonly record struct ScoreAggregateResult(
        int TotalScore, int MaxScore, int Percentage, bool IsPassed);

    public static class AssessmentScoreAggregator
    {
        // questions: PackageQuestion (+Options ter-load); responses: PackageUserResponse session ini.
        // Formula D-04: percentage = maxScore>0 ? (int)((double)total/maxScore*100) : 0.
        public static ScoreAggregateResult Compute(
            IEnumerable<PackageQuestion> questions,
            IEnumerable<PackageUserResponse> responses,
            int passPercentage)
        {
            int total = 0, max = 0;
            var respList = responses.ToList();
            foreach (var q in questions)
            {
                max += q.ScoreValue;
                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice": /* opt.IsCorrect → += ScoreValue (verbatim L3542-3549) */ break;
                    case "MultipleAnswer": /* SetEquals correct → += ScoreValue (verbatim L3550-3556) */ break;
                    case "Essay":          /* += EssayScore.Value (verbatim L3557-3560) */ break;
                }
            }
            int pct = max > 0 ? (int)((double)total / max * 100) : 0;   // D-04 LOCKED
            return new(total, max, pct, pct >= passPercentage);
        }
    }
}
// Sumber pola: Helpers/ShuffleEngine.cs (static murni), L3535-3564 (logika di-port verbatim).
```

**Rekonsiliasi dengan `ComputeScoreAndETInternalAsync` (GradingService.cs L331-429):**
- `ComputeScoreAndETInternalAsync` adalah **async + EF-bound** (load assignment/questions/responses sendiri) + menghitung ET breakdown + dukung `overrideAnswers`. **Skip Essay** (case Essay L382 kosong) — TIDAK menjumlah EssayScore. **Karena itu TIDAK bisa dipakai untuk finalize essay** (akan abaikan skor manual).
- Rekomendasi (D-06, minim-risiko): helper baru **HANYA mengganti math inline L3535-3564 di FinalizeEssayGrading** + dipakai recompute. **JANGAN** refactor `ComputeScoreAndETInternalAsync`/`GradeAndCompleteAsync` (regression risk; di luar boundary). Helper essay menambah cabang Essay (jumlah EssayScore) yang absen di ComputeScoreAndETInternal.
- Opsional planner: tambahkan unit test yang membuktikan helper baru == hasil L3564 inline untuk dataset mixed (no-drift), supaya "satu formula dua jalur" (D-04) terbukti.

### Pattern: Robust question-set derivation (D-06 fallback)

```csharp
// Di kedua call-site (forward + recompute), GANTI baris `shuffledIds.Contains(q.Id)` yang rapuh:
var shuffledIds = packageAssignment.GetShuffledQuestionIds();
List<PackageQuestion> questions;
if (shuffledIds.Count > 0)
{
    questions = await _context.PackageQuestions.Include(q => q.Options)
        .Where(q => shuffledIds.Contains(q.Id)).ToListAsync();
}
else
{
    // D-06 fallback: shuffledIds kosong (root-cause H1) → derive dari paket assignment.
    // packageAssignment.AssessmentPackageId = sentinel paket pertama (lihat CMPController L1002).
    // Cross-package caveat: bila assignment lintas paket, sentinel hanya 1 paket → planner
    //   pertimbangkan derive dari SEMUA PackageUserResponse.PackageQuestionId session (lebih aman).
    _logger.LogWarning("FinalizeEssayGrading: shuffledIds kosong session {Id} — fallback derive question-set.", sessionId);
    var respQIds = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == sessionId).Select(r => r.PackageQuestionId).Distinct().ToListAsync();
    questions = await _context.PackageQuestions.Include(q => q.Options)
        .Where(q => respQIds.Contains(q.Id)).ToListAsync();
}
// + log warning saat aggregate.MaxScore == 0 (D-05) — sinyal anomali, JANGAN block.
```

> **Catatan derivasi fallback (DISCRETION untuk planner):** derive dari `PackageUserResponses` session lebih robust daripada dari `AssessmentPackage.Questions` karena (a) sentinel `AssessmentPackageId` hanya 1 paket untuk assignment cross-package, (b) menjamin hanya soal yang benar-benar dijawab/diassign yang dihitung. Tapi hati-hati: untuk MC yang tak dijawab, tidak ada response row → soal itu hilang dari maxScore. Untuk essay-only (1 soal, pasti ada TextAnswer response) ini aman. Planner WAJIB validasi pilihan derivasi saat SC1 berdasarkan data shape aktual.

### Anti-Patterns to Avoid
- **Duplikasi math di SQL/endpoint:** dilarang D-02. Recompute WAJIB reuse helper, bukan re-implement `(int)(total/max*100)` di SQL.
- **Refactor `GradeAndCompleteAsync`/`ComputeScoreAndETInternalAsync`:** di luar boundary; regression risk. Sentuh HANYA inline L3535-3564.
- **Block finalize saat maxScore=0:** ditolak D-05. Hanya log warning + Score=0 fallback.
- **Auto-cert/Proton di recompute:** ditolak D-03 (ledakan notif/cert massal di prod).
- **Edit DB Dev/Prod langsung:** dilarang CLAUDE.md. Recompute = endpoint diserahkan ke IT.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Formula persentase skor | Re-tulis `(int)(total/max*100)` di endpoint/SQL | `AssessmentScoreAggregator.Compute` (helper bersama) | D-02/D-04 single source of truth; kill-drift |
| Idempotent batch update | Loop + SaveChanges per row | `ExecuteUpdateAsync` + WHERE-guard (pola L3569) | Atomic, race-safe, idempotent |
| Endpoint admin batch | Skema baru dari nol | Pola `BackfillProtonPenanda` (L3795-3895) | Precedent gated+counters+warn-audit |
| Real-SQL test fixture | Setup DbContext manual | `RecordCascadeFixture`/`ProtonCompletionFixture` | Disposable DB + MigrateAsync sudah jadi |
| Audit log | Insert AuditLogs manual | `_auditLog.LogAsync(userId, actorName, action, desc, targetId, targetType)` | Signature terverifikasi (L3876, L4093) |

**Key insight:** Semua building block sudah ada di codebase. Phase ini = ekstraksi (kill-drift) + 1 endpoint baru (pola precedent) + test. Nyaris zero teknologi baru.

## Common Pitfalls

### Pitfall 1: Memperlakukan H1 sebagai fakta tanpa repro (melanggar diagnose-first)
**What goes wrong:** Tulis fix berbasis asumsi "shuffledIds kosong" padahal root cause H2/H4.
**Why:** Hipotesis kuat tapi belum membedakan essay-only vs mixed-ber-essay (keduanya lewat path sama).
**How to avoid:** SC1 = repro lokal FLOW L + inspect `ShuffledQuestionIds`, `QuestionType`, `EssayScore`, `rowsAffected`. Dokumentasikan temuan sebelum tulis fix.
**Warning signs:** Plan menulis "karena shuffledIds kosong" tanpa data query DB lokal.

### Pitfall 2: Merusak idempotency Phase 310 (replay guard)
**What goes wrong:** Mengubah WHERE-guard `Status==PendingGrading` (L3570) atau early-return `alreadyFinalized` (L3475-3491) → klik 2x menduplikasi cert/notif.
**Why:** Phase 310 D-03/D-06/D-07 mengandalkan `rowsAffected` capture + WHERE-guard.
**How to avoid:** Helper hanya ganti BLOK MATH (L3535-3565). PRESERVE verbatim: early-return Completed (L3475), status switch (L3493), ExecuteUpdateAsync WHERE-guard (L3569), `rowsAffected==0` race branch (L3577).
**Warning signs:** `git diff` menyentuh L3466-3534 atau L3566-3669 di luar pemanggilan helper.

### Pitfall 3: Recompute meledakkan cert/notif di prod (melanggar D-03)
**What goes wrong:** Recompute memanggil `GenerateCertificate`/`EnsureAsync`/`NotifyIfGroupCompleted` untuk ribuan baris historis.
**Why:** Menyalin forward path penuh ke recompute.
**How to avoid:** Recompute set **Score+IsPassed ONLY** via `ExecuteUpdateAsync`. NO cert, NO Proton, NO notif (D-03).
**Warning signs:** Endpoint recompute me-reference `CertNumberHelper`/`_protonCompletionService`/`NotifyIfGroupCompleted`.

### Pitfall 4: Auto-create TrainingRecord (melanggar Phase 324 D-02)
**What goes wrong:** Menambah TrainingRecord saat finalize/recompute.
**Why:** Regression lama (commit 766011b6) sudah di-remove.
**How to avoid:** JANGAN sentuh TrainingRecords. `AssessmentSession` = sole source-of-truth Records (L3602-3604 comment).
**Warning signs:** `_context.TrainingRecords.Add` di path baru.

### Pitfall 5: Regresi tampilan "Menunggu Penilaian" (Phase 345)
**What goes wrong:** Mengubah Status transition merusak display jujur passRate/average exclude-pending.
**Why:** Phase 345 display invariant; FinalizeEssayGrading set `Completed` saat selesai (benar).
**How to avoid:** Status transition PendingGrading→Completed tak berubah; recompute hanya menyentuh baris yang SUDAH `Completed`.
**Warning signs:** Recompute mengubah `Status` (seharusnya hanya Score+IsPassed).

### Pitfall 6: Proton defensive hook interaction (L3643-3655)
**What goes wrong:** Helper mengubah `isPassed`/`finalPercentage` yang dipakai hook Proton (L3647-3651) → flip penanda Proton tak sengaja.
**Why:** Hook bergantung `isPassed` hasil agregasi.
**How to avoid:** Helper menghasilkan `isPassed` via formula sama (D-04); hook forward path UNCHANGED. Recompute TIDAK panggil hook (D-03).
**Warning signs:** Output helper berbeda dari L3564-3565 untuk dataset identik.

## Code Examples (verified from codebase)

### Agregasi inline saat ini (sumber ekstraksi — L3535-3565)
```csharp
// Source: Controllers/AssessmentAdminController.cs L3535-3565 [VERIFIED]
int totalScore = 0; int maxScore = 0;
foreach (var q in allQuestions)
{
    maxScore += q.ScoreValue;
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleChoice":
            var mcResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
            if (mcResp != null) { var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
                if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue; }
            break;
        case "MultipleAnswer":
            var maSelected = allResponses.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                .Select(r => r.PackageOptionId!.Value).ToHashSet();
            var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;
            break;
        case "Essay":
            var essayResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;
            break;
    }
}
int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // D-04 LOCKED
bool isPassed = finalPercentage >= session.PassPercentage;
```

### Recompute candidate predicate (usulan — DISCRETION)
```csharp
// Kandidat: essay/manual-graded yang sudah Completed tapi Score=0 padahal semua essay dinilai.
// [VERIFIED fields: AssessmentSession.Score int? L26, IsPassed bool? L44, HasManualGrading bool L184,
//  PassPercentage int L30; AssessmentStatus.Completed L17; PackageUserResponse.EssayScore int? L32]
var candidateIds = await _context.AssessmentSessions
    .Where(s => s.Status == AssessmentConstants.AssessmentStatus.Completed
             && s.HasManualGrading
             && (s.Score == null || s.Score == 0))
    .Select(s => s.Id).ToListAsync();
// + per-kandidat: pastikan TIDAK ada PackageUserResponse essay dengan EssayScore==null (semua dinilai)
//   sebelum recompute (skip kandidat yang belum lengkap dinilai).
```

### Idempotent update (set Score+IsPassed only — D-03)
```csharp
// Source pola: AssessmentAdminController.cs L3569 [VERIFIED]; recompute hanya Score+IsPassed.
var rows = await _context.AssessmentSessions
    .Where(s => s.Id == cand.Id && (s.Score == null || s.Score == 0))   // idempotent WHERE-guard
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, result.Percentage)
        .SetProperty(r => r.IsPassed, result.IsPassed));
// NO Status change, NO cert, NO Proton, NO notif (D-03).
```

### Real-SQL fixture (pola test helper)
```csharp
// Source: HcPortal.Tests/RecordCascadeIntegrationTests.cs L21-54 [VERIFIED]
public class RecordCascadeFixture : IAsyncLifetime {
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options => _options;
    public async Task InitializeAsync() {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.MigrateAsync();   // SQLEXPRESS @localhost
    }
    public async Task DisposeAsync() {
        await using var ctx = new ApplicationDbContext(_options); await ctx.Database.EnsureDeletedAsync();
    }
}
// [Trait("Category","Integration")] → skip via --filter "Category!=Integration"
```

## Runtime State Inventory

> Phase ini bukan rename, tapi melibatkan **repair data lama** (recompute). Inventarisasi state runtime yang relevan:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `AssessmentSessions.Score=0` untuk baris essay-only historis yang sudah `Completed`+dinilai (jumlah TBD — query SC1). `PackageUserResponses.EssayScore` SUDAH terisi benar (komentar e2e L413). `UserPackageAssignments.ShuffledQuestionIds` mungkin `[]`/null untuk baris ter-affected (cek SC1). | **Data migration** via recompute endpoint (IT eksekusi Dev→Prod). Developer TIDAK edit DB langsung. |
| Live service config | Tidak ada — bug murni di kode aplikasi. | None — verified by review (no external service config touches grading). |
| OS-registered state | None — verified (tidak ada task/cron untuk grading). | None. |
| Secrets/env vars | None — fix tak butuh env baru. | None. |
| Build artifacts | Helper baru di `Helpers/` → `dotnet build` recompile (no stale artifact). Test project recompile. | `dotnet build` + `dotnet test` lokal. |

**Migration DB:** **false** (D-01/CONTEXT L11 — no schema change). Recompute = DML repair via endpoint, BUKAN EF migration. Flag ke IT: **no migration**, ada **endpoint recompute** untuk dijalankan + commit hash.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8) | build/test | ✓ (asumsi — stack proyek) | net8 | — |
| SQL Server (localhost\SQLEXPRESS) | real-SQL fixture + verifikasi DB lokal | ✓ (HcPortalDB_Dev shared, lihat MEMORY) | — | InMemory untuk unit murni (helper tak butuh DB) |
| Playwright | e2e FLOW L un-fixme | ✓ (tests/e2e existing) | — | xUnit-only bila e2e env tak siap |
| SQLBrowser + `lpc:` conn override | e2e login lokal (NTLM loopback) | conditional | — | lihat reference_local_e2e_sql_env_fix (MEMORY): start SQLBrowser, `--workers=1` |

**Missing dependencies with no fallback:** Tidak ada yang blocking. Helper murni testable tanpa DB (pola ShuffleEngineTests).
**Catatan lokal AD:** `Authentication__UseActiveDirectory=false dotnet run` untuk UAT (MEMORY: project_355).

## Validation Architecture

> nyquist_validation = **true** (config.json L15) → section ini WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (HcPortal.Tests) + Playwright (tests/e2e, TypeScript) |
| Config file | HcPortal.Tests.csproj (xUnit) ; playwright.config.ts (e2e) |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit murni, cepat) |
| Full suite command | `dotnet test` (termasuk Integration real-SQL) + `npx playwright test exam-types --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GRADE-01 | Helper agregasi essay-only: 1 essay ScoreValue=100, EssayScore=80 → percentage=80 | unit | `dotnet test --filter "FullyQualifiedName~AssessmentScoreAggregatorTests"` | ❌ Wave 0 |
| GRADE-01 | maxScore=0 → percentage=0 (D-05 fallback, no throw) | unit | idem | ❌ Wave 0 |
| GRADE-02 | Helper mixed (MC+MA+essay) == hasil formula inline L3564 (no-drift) | unit | idem | ❌ Wave 0 |
| GRADE-02 | Forward finalize essay-only real-SQL → `AssessmentSessions.Score` = agregasi (bukan 0) | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~EssayFinalize"` | ❌ Wave 0 |
| GRADE-02 | e2e essay-only finalize → Score=80 | e2e | `npx playwright test exam-types --grep "L6" --workers=1` (un-fixme L415) | ✓ ada (fixmed) |
| D-02/D-07 | Recompute endpoint idempotent: 2x run → kandidat sama tak berubah; hanya sentuh Score=0 | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~Recompute"` | ❌ Wave 0 |
| D-03 | Recompute TIDAK buat cert/Proton/TrainingRecord/notif | integration | idem | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit helper < 30s).
- **Per wave merge:** `dotnet test` penuh (incl. Integration real-SQL) + `npx playwright test exam-types --workers=1`.
- **Phase gate:** Full suite hijau + e2e FLOW L5+L6 hijau (un-fixme) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` — unit murni helper (essay-only, mixed no-drift, maxScore=0) → GRADE-01/02. Pola: `ShuffleToggleRulesTests` (pure, no fixture).
- [ ] `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` — integration real-SQL (forward finalize Score≠0 + recompute idempotent + D-03 no-side-effect). Pola: `RecordCascadeFixture`/`ProtonCompletionFixture`.
- [ ] Un-fixme `tests/e2e/exam-types.spec.ts` L415 (FLOW L6) — hapus `test.fixme(true, ...)`; assert Score=80.
- [ ] (Opsional) seed fixture essay-only `Score=0` historis untuk uji recompute candidate-detection.

*(Framework sudah ada; gap = file test baru + un-fixme.)*

## Project Constraints (from CLAUDE.md)

| Directive | Impact on Phase 376 |
|-----------|---------------------|
| Respond/dokumen user-facing **Bahasa Indonesia** | Pesan endpoint, audit description, IT handoff note, UAT WAJIB BI. |
| Lokal → Dev → Prod; developer **TAK edit/push DB Dev/Prod** | Recompute = endpoint diserahkan IT (D-01). Developer hanya verifikasi lokal + flag IT (commit hash, no-migration, "ada endpoint recompute"). |
| Verifikasi lokal: `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal (+ Playwright) | Gate sebelum commit. SC1 repro = wajib lokal. |
| Jangan push tanpa verifikasi lokal | Full suite hijau + e2e L6 hijau sebelum handoff. |
| Seed Data Workflow: snapshot DB sebelum seed temporary, restore setelah, catat `docs/SEED_JOURNAL.md` | Bila recompute test butuh seed essay-only historis lokal → snapshot+journal+restore. |

## Security Domain

> security_enforcement default = enabled. Endpoint recompute = surface baru → analisis wajib.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin, HC")]` (pola L3467, L834) — endpoint recompute WAJIB gated |
| V3 Session Management | no | Pakai ASP.NET Identity existing |
| V4 Access Control | yes | Role gate HC/Admin; recompute = operasi sensitif (ubah Score banyak baris) → pertimbangkan `Admin`-only (lebih ketat) sesuai BulkBackfill L834 |
| V5 Input Validation | yes | Endpoint headless/POST: validasi minimal (tak terima Score dari user — dihitung server dari DB). `[ValidateAntiForgeryToken]` |
| V6 Cryptography | no | Tidak ada crypto |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST recompute | Spoofing/Tampering | `[ValidateAntiForgeryToken]` (pola L3468) |
| Privilege escalation (non-admin trigger mass-recompute) | Elevation | `[Authorize(Roles=...)]` ketat; pertimbangkan Admin-only |
| Mass-state corruption (recompute salah → Score massal salah) | Tampering | Idempotent WHERE-guard `Score==0` + dry-run/preview (pola BackfillProtonPenanda counters) + audit log + IT eksekusi terkontrol |
| Info-leak via error | Info Disclosure | Pesan generik ke user, detail ke `_logger` (pola L3889 Phase 334 D6) |
| Audit gap | Repudiation | `_auditLog.LogAsync` batch (warn-only, jangan break) — pola L3876 |

## State of the Art

| Old Approach | Current Approach | When | Impact |
|--------------|------------------|------|--------|
| Math agregasi inline di FinalizeEssayGrading | Helper bersama (kill-drift Phase 363/365 pattern) | Phase 376 | Single source of truth; recompute reuse |
| Fix data via SQL script manual | Endpoint admin idempotent (pola BackfillProtonPenanda Phase 358, BulkBackfill Phase 338) | this phase | Aman, auditable, IT-executable tanpa edit DB langsung |

**Deprecated/outdated:**
- `GradeFromSavedAnswers` — dihapus Phase 296 (MEMORY: GradingService satu-satunya source grading). Jangan reintroduce.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Root cause persis = H1 (shuffledIds kosong) | Root Cause Analysis | SEDANG — fix helper+fallback menetralkan H1/H2 apapun; tapi bila H4 (race) dominan, butuh guard tambahan. **Wajib konfirmasi SC1 repro.** |
| A2 | Mixed-ber-essay TIDAK ter-affected (hanya essay-only) | Root Cause Analysis | SEDANG — keduanya lewat path sama; bila mixed-ber-essay juga rusak, scope test bertambah (tetap di-cover D-07). Konfirmasi SC1. |
| A3 | Derivasi fallback dari `PackageUserResponses` session aman untuk essay-only | Robust derivation pattern | RENDAH untuk essay-only (1 soal pasti ada response); planner validasi untuk mixed. |
| A4 | Jumlah baris historis ter-affected di Dev/Prod tak diketahui (tak boleh sentuh prod) | Runtime State Inventory | RENDAH — recompute idempotent menangani 0..N baris; query count saat IT eksekusi. |
| A5 | `Score==0` cukup sebagai predicate kandidat (vs Score IS NULL) | Recompute predicate | RENDAH — predicate include `Score==null OR Score==0`; planner verifikasi nilai aktual baris rusak di SC1 (apakah 0 atau null). |
| A6 | net8 + SQLEXPRESS lokal tersedia | Environment | RENDAH — konsisten dengan 50+ phase sebelumnya. |

## Open Questions

1. **Root cause persis (H1..H4)?**
   - Known: mekanisme H1 lengkap & terverifikasi; e2e L6 mengonfirmasi gejala Score=0 untuk essay-only.
   - Unclear: kenapa hanya essay-only (path sama dengan mixed-ber-essay). Apakah `ShuffledQuestionIds` baris ter-affected memang `[]`/null?
   - Rekomendasi: SC1 repro lokal FLOW L + query `ShuffledQuestionIds`/`QuestionType`/`EssayScore`/`rowsAffected`. Dokumentasikan sebelum tulis fix (diagnose-first D-06).

2. **Derivasi fallback: dari `AssessmentPackage.Questions` (sentinel) atau dari `PackageUserResponses` session?**
   - Known: sentinel `AssessmentPackageId` = paket pertama saja (cross-package caveat).
   - Rekomendasi: `PackageUserResponses` untuk essay-only (aman); planner finalkan untuk mixed berdasarkan data SC1.

3. **Recompute predicate: `Score==0` atau `Score IS NULL`?**
   - Known: `Score` adalah `int?`. Interim essay set `Score=interimPercentage` (0 untuk essay-only) lalu finalize set 0.
   - Rekomendasi: `(Score == null || Score == 0)` + filter "semua EssayScore terisi". Verifikasi nilai aktual di SC1.

4. **Endpoint recompute: role `Admin`-only atau `Admin, HC`?**
   - Known: BulkBackfill = Admin-only (L834); FinalizeEssayGrading = Admin,HC (L3467).
   - Rekomendasi: operasi mass-repair → **Admin-only** lebih aman (V4). Planner/discuss konfirmasi (DISCRETION).

5. **UI trigger atau headless POST?**
   - DISCRETION (CONTEXT D-02). Rekomendasi: headless POST + preview-count (pola BackfillProtonPenanda) cukup untuk handoff IT; tombol admin opsional.

## Sources

### Primary (HIGH confidence — kode terverifikasi sesi ini)
- `Controllers/AssessmentAdminController.cs` — FinalizeEssayGrading L3469-3669, SubmitEssayScore L3428-3457, AkhiriUjian L4050-4109, BackfillProtonPenanda L3795-3895, BulkBackfillAssessment L831-985, constructor/deps L19-54.
- `Services/GradingService.cs` — GradeAndCompleteAsync L56-318 (hasEssay L197-234, non-essay L236-317), ComputeScoreAndETInternalAsync L331-429, RegradeAfterEditAsync L437-554.
- `Models/UserPackageAssignment.cs` — GetShuffledQuestionIds L60-71, ShuffledQuestionIds field L31.
- `Helpers/ShuffleEngine.cs` — BuildQuestionAssignment L39-60 (return `[]` saat paket kosong).
- `Controllers/CMPController.cs` — StartExam lazy-create L971-1024, SubmitExam package path L1562-1690.
- `Models/AssessmentConstants.cs` — AssessmentStatus L13-21, IsAssessmentSubmitted L87.
- `Models/PackageUserResponse.cs` — EssayScore L32, TextAnswer L29.
- `Models/AssessmentPackage.cs` — PackageQuestion (QuestionType L48, ScoreValue L41, ElemenTeknis L51, Order L39).
- `Models/AssessmentSession.cs` — Score L26 (int?), PassPercentage L30, IsPassed L44 (bool?), HasManualGrading L184, Category/ProtonTrackId.
- `Controllers/AdminBaseController.cs` — _context/_userManager/_auditLog L16-18.
- `HcPortal.Tests/RecordCascadeIntegrationTests.cs` L21-54 (RecordCascadeFixture), `ProtonCompletionServiceTests.cs` L25 (ProtonCompletionFixture), `ShuffleToggleRulesTests.cs` (pure helper test pattern).
- `tests/e2e/exam-types.spec.ts` L357-424 (FLOW L4/L5/L6 + fixme L415).
- `.planning/config.json` (nyquist_validation=true), `.planning/STATE.md`, `.planning/ROADMAP.md` Phase 376 SC1-SC4, `.planning/REQUIREMENTS.md` GRADE-01/02, `CLAUDE.md`.

### Secondary (MEDIUM — dokumen keputusan fase terkait)
- Phase 310 (idempotency), 324 (no TrainingRecord), 345 (display jujur), 358 (Proton hook), 363/365 (kill-drift pattern), 372-373 (shuffle/`{}` fix) — via CONTEXT canonical_refs + MEMORY index.

### Tertiary (LOW)
- Tidak ada — semua klaim load-bearing diverifikasi via kode atau dokumen proyek.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua aset terverifikasi di kode.
- Architecture (helper + recompute): HIGH — pola precedent eksplisit (ShuffleEngine, BackfillProtonPenanda, RecordCascadeFixture).
- Root cause: MEDIUM — mekanisme H1 terverifikasi lengkap, tapi diferensiasi essay-only vs mixed-ber-essay perlu repro runtime (SC1, diagnose-first by design). Fix robust terhadap H1/H2/H4 apapun.
- Pitfalls: HIGH — diturunkan dari guard/invariant fase terkait yang terverifikasi.

**Research date:** 2026-06-14
**Valid until:** 2026-07-14 (codebase stabil; valid sampai struktur grading berubah). Root cause section: re-validate saat SC1 repro.
