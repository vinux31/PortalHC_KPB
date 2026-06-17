# Phase 393: Backend core inject - Research

**Researched:** 2026-06-17
**Domain:** .NET 8 / ASP.NET MVC backend service orchestration · EF Core 8 (SQL Server) · grading-pipeline reuse · atomic batch transaction · xUnit integration testing
**Confidence:** HIGH (semua klaim diverifikasi langsung dari source code repo + build hijau + SQLEXPRESS reachable)

## Summary

Phase 393 membangun `Services/InjectAssessmentService.cs` (BARU) — orchestrator backend yang menyusun set sesi assessment manual lengkap per pekerja, lalu **melewatkannya ke pipeline grading existing** (`GradingService.GradeAndCompleteAsync` + jalur essay-finalize + `AssessmentScoreAggregator` + `CertNumberHelper`) sehingga skor/lulus/elemen-teknis/nomor-sertifikat **dihitung mesin**, byte-identik jalur online. Tidak ada UI, tidak ada controller, 0 migration. Deliverable = service + xUnit integration tests.

Tiga temuan arsitektural kunci yang menentukan bentuk service:
1. **`GradeAndCompleteAsync` membaca dari DB, bukan dari parameter** `[VERIFIED: GradingService.cs:60-81]`. Service inject WAJIB meng-insert `AssessmentSession` + `UserPackageAssignment` (ber-`ShuffledQuestionIds`) + `PackageUserResponses` ke DB **dulu**, baru memanggil `GradeAndCompleteAsync(session)`. Method tidak membuka transaction sendiri (pakai `SaveChangesAsync` + `ExecuteUpdateAsync`) → caller (service inject) yang bungkus transaction batch.
2. **`FinalizeEssayGrading` adalah controller action ber-HTTP-coupling berat** (`_userManager.GetUserAsync(User)`, `_auditLog.LogAsync`, `_hubContext` SignalR, `return Json`) `[VERIFIED: AssessmentAdminController.cs:3637-3871]`. Logic finalize **tidak bisa dipanggil langsung**. Rekomendasi: **replikasi data-level CORE finalize di dalam InjectAssessmentService** (pola Phase 387/376 `EssayFinalizeRecomputeTests.MirrorFinalizeWriteAsync`), bukan extract-shared-service (lihat Pattern 2 untuk alasan & batas).
3. **Visibility & render "seakan online" GRATIS** — `WorkerDataService.GetUnifiedRecords` (`currentQuery`) hanya filter `Status == Completed || PendingGrading`, **tanpa filter `IsManualEntry`/`AssessmentType`** `[VERIFIED: WorkerDataService.cs:134-136]`. `CMPController.Results` **tidak punya branch `AssessmentType=="Manual"`/`IsManualEntry`** — render rincian per-soal asal ada assignment+responses+paket+`AllowAnswerReview=true` `[VERIFIED: CMPController.cs:2184-2366]`.

**Primary recommendation:** Tulis `InjectAssessmentService` sebagai service Scoped (DI seperti `GradingService`). Per batch: pre-flight validate semua row (D-03) → buka 1 transaction (D-04) → per pekerja: insert session+assignment(sentinel anchor)+responses → `GradeAndCompleteAsync` → bila essay, jalankan blok finalize data-level (D-05) → audit `_context.AuditLogs.Add` (D-11) → commit. Cert manual = set `NomorSertifikat` di session sebelum grade + tangkap unique-collision pre-flight (D-09); cert auto = sudah ditangani `GradeAndCompleteAsync` (D-08). Uji dengan disposable real-SQL DB (`HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `[Trait Category=Integration]`) meng-instantiate **real GradingService** (pola `SubmitResurrectionTests`) + assert byte-identik vs jalur online.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Orchestrasi inject (susun session set) | API/Backend service (`InjectAssessmentService`) | — | Business logic murni; tidak ada UI di 393 (Phase 394+) |
| Hitung skor/lulus/ET | Backend (reuse `GradingService`/`AssessmentScoreAggregator`) | — | Kill-drift: nol duplikasi, mesin existing satu-satunya sumber kebenaran |
| Generate nomor sertifikat | Backend (reuse `CertNumberHelper` via `GradeAndCompleteAsync`) | — | Sekuens resmi `KPB/xxx/ROMAN/year`, retry anti-collision |
| Atomicity batch | Backend (caller-managed transaction di service inject) | DB (`BeginTransactionAsync`) | `GradeAndCompleteAsync` non-transaksional → caller wajib bungkus |
| Persistensi entitas (session/assignment/responses/ET) | Database (EF Core 8 + SQL Server) | — | Semua tabel sudah ada (0 migration) |
| Audit trail | Backend (`_context.AuditLogs.Add` in-tx) | DB | Transparansi compliance; in-tx agar rollback ikut |
| Visibility /CMP/Records & /CMP/Results | (TIDAK disentuh 393) | — | Gratis otomatis — query existing tak filter `IsManualEntry` |

## Standard Stack

Tidak ada library/package BARU. Phase ini murni reuse stack existing.

### Core (existing, reuse apa-adanya)
| Komponen | Lokasi | Purpose | Catatan reuse |
|----------|--------|---------|---------------|
| `GradingService.GradeAndCompleteAsync` | `Services/GradingService.cs:57-336` | Hitung Score/IsPassed, set Status, insert ET scores, generate cert (gate isPassed) | Baca dari DB; non-transaksional; caller bungkus tx `[VERIFIED]` |
| `AssessmentScoreAggregator.Compute` | `Helpers/AssessmentScoreAggregator.cs:26-60` | Pure aggregator (MC/MA/Essay → persen+IsPassed) | EF-free, sinkron; essay tambah `EssayScore.Value` (0..ScoreValue) `[VERIFIED]` |
| `CertNumberHelper` | `Helpers/CertNumberHelper.cs` | `GetNextSeqAsync`/`Build`/`IsDuplicateKeyException` | Sekuens `KPB/{seq:D3}/{ROMAN}/{year}`, dipanggil dari dalam grading `[VERIFIED]` |
| `AssessmentConstants` | `Models/AssessmentConstants.cs` | Konstanta Status/Type | `AssessmentStatus.PendingGrading="Menunggu Penilaian"`, `Completed`, dll `[VERIFIED]` |

### Supporting (existing)
| Komponen | Lokasi | Purpose | When to Use |
|----------|--------|---------|-------------|
| `AdminBaseController.NormalizeTitleForDup` | `AdminBaseController.cs:271-272` | Normalisasi judul (`Regex \s+→" "`, lowercase invariant) | Dedup D-02 (static, reusable dari service) `[VERIFIED]` |
| `UserPackageAssignment.GetShuffledQuestionIds()` | `Models/UserPackageAssignment.cs:60-71` | Deserialize JSON `ShuffledQuestionIds` | Grading & Results baca soal via ini, bukan by-session `[VERIFIED]` |
| `_context.AuditLogs.Add` (langsung) | EF DbSet | Audit in-tx | JANGAN pakai `AuditLogService.LogAsync` di tengah tx (dia SaveChanges sendiri) `[VERIFIED: AuditLogService.cs:40-41]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Replikasi finalize data-level di service | Extract `GradingService.FinalizeEssayAsync(session)` baru + panggil dari controller & service | Lebih bersih (true zero-dup), TAPI menyentuh `AssessmentAdminController.FinalizeEssayGrading` (refactor risk + bukan file disjoint) → di luar scope-lock 393 "tidak buat engine grading baru". Planner boleh pilih, tapi default = replikasi data-level (precedent Phase 387/376). |
| `GradeAndCompleteAsync` untuk semua | Panggil grade → cek essay → finalize-block | `GradeAndCompleteAsync` SUDAH meng-handle essay (set `PendingGrading`), jadi alurnya: grade dulu (→ PendingGrading bila ada essay) → lalu jalankan finalize-block (→ Completed). Bukan alternatif, ini urutan wajib (D-05). |

**Installation:** N/A — tidak ada package baru. `dotnet build HcPortal.csproj` sudah hijau (0 error, 24 warning) `[VERIFIED: build 2026-06-17]`.

## Architecture Patterns

### System Architecture Diagram

```
InjectAssessmentService.InjectBatchAsync(InjectRequest)
  │  InjectRequest = { RoomSettings, PackageSpec(questions/options), List<WorkerInject>, CertMode }
  │
  ├─[1] PRE-FLIGHT VALIDATE (D-03 — ZERO writes bila ada invalid) ──────────┐
  │     • Resolve semua NIP → AspNetUsers (missing → reject-all)            │
  │     • Validasi opsi: PackageOptionId milik soal yg benar               │
  │     • Validasi EssayScore range 0..ScoreValue (D-07)                    │
  │     • Essay tanpa score = invalid (D-05)                               │
  │     • CompletedAt/Schedule ≤ today (D-06)                              │
  │     • Cert manual: NomorSertifikat unik (D-09, cek IX + intra-batch)   │
  │     ada error? → return InjectResult{ Rejected, PerRowErrors } ◄───────┘
  │        + AuditLog ActionType="ManualInjectRejected" (D-11c)
  │
  ├─[2] DEDUP per-pekerja (D-01/D-02 — SEBELUM tx) ────────────────────────
  │     key = UserId + NormalizeTitleForDup(Title) + Category + Schedule.Date
  │     (+ cert-aware bila generate-cert ON)
  │     match? → skip pekerja + record skip (BUKAN gagalkan batch)
  │        + AuditLog ActionType="ManualInjectSkipped" (D-11b)
  │
  ├─[3] BEGIN TRANSACTION (D-04 — caller-managed, safety-net) ─────────────
  │   FOR EACH pekerja valid & non-dup:
  │     a. INSERT AssessmentSession (IsManualEntry=true, AccessToken="INJECT",
  │            IsTokenRequired=false, AssessmentType=Standard/PreTest/PostTest,
  │            AllowAnswerReview=true, Status=Open, GenerateCertificate, ValidUntil,
  │            Schedule/StartedAt/CompletedAt = backdate, PassPercentage) → SaveChanges (dapat session.Id)
  │     b. INSERT AssessmentPackage anchored ke session pertama room (sentinel) [1× per batch]
  │            + PackageQuestion + PackageOption → SaveChanges (dapat IDs)
  │     c. INSERT UserPackageAssignment (AssessmentPackageId=sentinel.Id, UserId,
  │            ShuffledQuestionIds=JSON(question IDs urut), ShuffledOptionIdsPerQuestion="{}",
  │            SavedQuestionCount, IsCompleted=true) → SaveChanges
  │     d. INSERT PackageUserResponse per soal (MC/MA: PackageOptionId; Essay: TextAnswer+EssayScore) → SaveChanges
  │     e. (cert manual) set session.NomorSertifikat + ValidUntil pra-grade
  │     f. CALL GradingService.GradeAndCompleteAsync(session)
  │            → MC/MA dihitung, ET scores inserted, Status set:
  │               - non-essay → Completed + Score + IsPassed (+cert auto bila isPassed & GenerateCertificate)
  │               - ada essay → PendingGrading (interim score)
  │     g. (bila ada essay) FINALIZE-BLOCK data-level (D-05):
  │            agg = AssessmentScoreAggregator.Compute(questions, responses, PassPercentage)
  │            ExecuteUpdate WHERE Id && Status==PendingGrading
  │               SET Score=agg.Percentage, Status=Completed, IsPassed=agg.IsPassed, CompletedAt
  │            + cert-block (retry 3× gate isPassed & GenerateCertificate, same as GradeAndComplete)
  │     h. _context.AuditLogs.Add(ActionType="ManualInject", actor, NIP, sessionId, skor) (D-11a)
  │   SaveChanges
  ├─[4] COMMIT (sukses) / ROLLBACK (exception tak terduga → no parsial)
  │
  └─ return InjectResult{ SuccessSessionIds, SkippedWorkers, PerRowErrors=empty }
```

File-to-implementation: lihat Component Responsibilities di tabel Standard Stack. Diagram di atas adalah data-flow, bukan listing file.

### Recommended Structure (file baru tunggal)
```
Services/
└── InjectAssessmentService.cs   # service Scoped; DTO request/result boleh inline atau Models/Inject*.cs
HcPortal.Tests/
└── InjectAssessmentTests.cs     # [Trait Category=Integration] disposable DB; instantiate real GradingService
```

DI registration (Program.cs, sejajar baris 54 `AddScoped<GradingService>()`):
```csharp
// Program.cs ~L54
builder.Services.AddScoped<HcPortal.Services.InjectAssessmentService>();
```
`[CITED: Program.cs:54,68]`

### Pattern 1: Sentinel Package Anchor (reuse CMPController)
**What:** `AssessmentPackage.AssessmentSessionId` di-anchor ke SATU sesi representatif room; tiap pekerja dapat `UserPackageAssignment(AssessmentPackageId=sentinelPackage.Id, ShuffledQuestionIds=JSON)`. Grading & Results memuat soal **by question ID via `ShuffledQuestionIds`**, bukan by session.
**When to use:** Selalu — ini cara assign 1 paket ke banyak pekerja tanpa schema baru.
**Example (pola online):**
```csharp
// Source: CMPController.cs:1068-1080 (online StartExam)
var sentinelPackage = packages.First();
assignment = new UserPackageAssignment {
    AssessmentSessionId = id,
    AssessmentPackageId = sentinelPackage.Id,  // sentinel
    UserId = user.Id,
    ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
    ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict),
};
assignment.SavedQuestionCount = shuffledIds.Count;
```
`[VERIFIED: CMPController.cs:1034-1101]`. **Catatan inject:** `ShuffledQuestionIds` = urutan ID soal apa-adanya (tak perlu shuffle — inject historis); `ShuffledOptionIdsPerQuestion="{}"` (Results fallback ke urutan DB). Karena 1 paket per room (asumsi spec §14), sentinel = paket itu sendiri.

### Pattern 2: Essay Finalize Data-Level Replication (REKOMENDASI)
**What:** `FinalizeEssayGrading` (controller) tidak dapat dipanggil dari service. Replikasi CORE-nya (status-transition + recompute + cert) data-level di dalam `InjectAssessmentService`, mengikuti precedent test `MirrorFinalizeWriteAsync`.
**Why replikasi, bukan extract:** `FinalizeEssayGrading` mengandung 7 hal HTTP-spesifik yang TIDAK boleh masuk service (`_userManager.GetUserAsync(User)`, `_auditLog.LogAsync` SaveChanges-sendiri, `_hubContext` SignalR broadcast, `return Json`, race-friendly-response, `Include(s=>s.User)` untuk nama broadcast). CORE data-level hanya 4 langkah. Extract-shared-service akan menyentuh `AssessmentAdminController.cs` (bukan file disjoint, refactor risk) — di luar scope-lock 393.
**The exact CORE finalize blocks** (kutip verbatim untuk batas extraction):

Status-transition + recompute `[VERIFIED: AssessmentAdminController.cs:3728-3742]`:
```csharp
var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
int finalPercentage = agg.Percentage;
bool isPassed = agg.IsPassed;
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
        .SetProperty(r => r.IsPassed, isPassed)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow));
```
Cert generation (identik dengan `GradeAndCompleteAsync`) `[VERIFIED: AssessmentAdminController.cs:3775-3804]`:
```csharp
if (session.GenerateCertificate && isPassed) {
    var certNow = DateTime.Now; int certYear = certNow.Year;
    int certAttempts = 0; const int maxCertAttempts = 3; bool certSaved = false;
    while (!certSaved && certAttempts < maxCertAttempts) {
        certAttempts++;
        try {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
            certSaved = true;
        } catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex)) { }
    }
}
```
Pre-check semua essay ter-skor `[VERIFIED: AssessmentAdminController.cs:3696-3697]`:
```csharp
if (essayResponses.Any(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null))
    return Json(new { success = false, message = "Masih ada Essay yang belum dinilai" });
```
**Inject D-05:** karena semua essay WAJIB ber-EssayScore (pre-flight D-03 menolak essay-tanpa-skor), pre-check ini selalu lolos. Tetap jalankan finalize-block agar `PendingGrading→Completed`.

### Pattern 3: Atomic Batch (reuse BulkBackfill blueprint)
**What:** `BeginTransactionAsync` → pre-validate NIP up-front → loop add → SaveChanges → audit in-tx → CommitAsync; `catch → RollbackAsync`.
**Example:**
```csharp
// Source: TrainingAdminController.cs:905-984
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    // ... inserts + grade ...
    await _context.SaveChangesAsync();
    foreach (var s in addedSessions)
        _context.AuditLogs.Add(new AuditLog { ActorUserId=..., ActionType=..., TargetId=s.Id, ... });
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
} catch (Exception ex) {
    await transaction.RollbackAsync();
    _logger.LogError(ex, "...");
}
```
`[VERIFIED: TrainingAdminController.cs:836-985]`. Pre-validate NIP: `:889-899`; dedup pre-load existing keys: `:913-925`.

### Anti-Patterns to Avoid
- **Menulis Score/IsPassed/cert dengan tangan** — melanggar "byte-identik" + kill-drift. Selalu lewat `GradeAndCompleteAsync`/`Compute` (specifics CONTEXT.md).
- **`AssessmentType="Manual"`** — JANGAN. Ada branch `ShouldEnforceSubmitTimer` yang skip "Manual" `[VERIFIED: CMPController.cs:4444-4449]` dan helper sibling Pre/Post exclude Manual `[VERIFIED: CMPController.cs:4432]`. Pakai `Standard`/`PreTest`/`PostTest` agar grouping & render identik online. (Untuk inject sesi yang sudah Completed, timer guard tak pernah jalan, tapi tetap pakai non-Manual demi konsistensi grouping & future Pre/Post.)
- **`AuditLogService.LogAsync` di tengah transaction** — dia panggil `SaveChangesAsync` sendiri `[VERIFIED: AuditLogService.cs:40-41]` → bisa commit parsial sebelum tx commit. Pakai `_context.AuditLogs.Add` (pola BulkBackfill `:960`).
- **`ExecuteUpdateAsync` di EF Core 8 InMemory provider** — TIDAK didukung `[VERIFIED: SubmitResurrectionTests.cs:4-6 deviation note]`. Test InjectAssessmentService WAJIB disposable real-SQL DB, bukan InMemory.
- **Memanggil `GradeAndCompleteAsync` sebelum responses ter-insert** — method baca `PackageUserResponses` dari DB `[VERIFIED: GradingService.cs:79-81]`. Insert + SaveChanges responses DULU.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Hitung skor MC/MA/Essay | Loop perhitungan custom | `AssessmentScoreAggregator.Compute` | Formula D-04 LOCKED, all-or-nothing MA, essay-aware — kill-drift `[VERIFIED]` |
| Generate nomor cert | Format string sendiri | `CertNumberHelper.Build` + `GetNextSeqAsync` | Sekuens resmi, retry collision, ROMAN month `[VERIFIED]` |
| Set Status/IsPassed/ET | ExecuteUpdate tangan | `GradeAndCompleteAsync` | Race-safe guard, ET insert, cert gate isPassed `[VERIFIED]` |
| Normalisasi judul dedup | `.Trim().ToLower()` sendiri | `NormalizeTitleForDup` | Regex `\s+→" "` + invariant — match logika dedup existing `[VERIFIED]` |
| Deteksi unique-cert collision | Parse SQL error sendiri | `CertNumberHelper.IsDuplicateKeyException` | Cek nama index + error 2601/2627 `[VERIFIED]` |
| Atomic batch + rollback | Try/catch ad-hoc | Pola `BulkBackfill` (`BeginTransactionAsync`) | Blueprint teruji, pre-validate up-front `[VERIFIED]` |

**Key insight:** Seluruh mesin grading sudah ada dan teruji ratusan kali (347+ test). Nilai Phase 393 = **orchestrasi tipis yang menyusun input lalu mendelegasikan ke mesin** — bukan menghitung apa pun sendiri. Setiap baris perhitungan manual = drift risk + melanggar INJ-01.

## Runtime State Inventory

> Phase 393 = greenfield service (TIDAK ada rename/refactor/migration). Tidak ada string yang di-rename di runtime state. Seluruh entitas yang ditulis = data baru via DbContext. Tidak ada datastore eksternal/OS-registered/secret yang terdampak.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — verified: service hanya INSERT baris baru ke tabel existing (`AssessmentSessions` dst), tidak rename key/koleksi | none |
| Live service config | None — verified: tidak menyentuh n8n/Datadog/external service | none |
| OS-registered state | None — verified: tidak ada task/process/unit baru | none |
| Secrets/env vars | None — verified: tidak ada secret/env baru (connstring test = SQLEXPRESS Integrated Security) | none |
| Build artifacts | None — verified: file C# baru ter-compile ke binary normal; 0 migration → tidak ada egg-info/artifact stale | none |

## Common Pitfalls

### Pitfall 1: `GradeAndCompleteAsync` menimpa `CompletedAt` backdate dengan `DateTime.UtcNow`
**What goes wrong:** D-06 minta `CompletedAt` = tanggal ujian luring (backdate). Tapi `GradeAndCompleteAsync` non-essay branch `SetProperty(r => r.CompletedAt, DateTime.UtcNow)` `[VERIFIED: GradingService.cs:263]` DAN essay branch `:224` — keduanya overwrite `CompletedAt` ke sekarang. Finalize-block juga `SetProperty(CompletedAt, DateTime.UtcNow)` `[VERIFIED: AssessmentAdminController.cs:3742]`.
**Why it happens:** Mesin grading mengasumsikan selesai = sekarang (online).
**How to avoid:** SETELAH `GradeAndCompleteAsync` (dan finalize-block bila essay) selesai, re-apply backdate: `ExecuteUpdate WHERE Id==session.Id SET CompletedAt = backdateCompletedAt, Schedule = backdateSchedule, StartedAt = backdateStartedAt` di dalam tx yang sama. Catat di plan sebagai langkah eksplisit pasca-grade.
**Warning signs:** xUnit: setelah inject, `session.CompletedAt` ≈ now (bukan tanggal luring). Test SC harus assert backdate ter-preserve.

### Pitfall 2: Nomor cert auto pakai tanggal HARI INI, bukan tanggal exam backdate
**What goes wrong:** Cert auto `CertNumberHelper.Build(nextSeq, DateTime.Now)` → ROMAN month/year = bulan/tahun inject DIJALANKAN `[VERIFIED: GradingService.cs:289,304]`. Untuk data historis (mis. exam Mei, inject Juni), nomor jadi `KPB/xxx/VI/2026` walau exam Mei.
**Why it happens:** Online cert diterbitkan saat grading (= saat itu juga), jadi `DateTime.Now` benar untuk online.
**How to avoid:** Ini **mirror perilaku online** (cert terbit saat di-grade). Untuk inject = keputusan kebijakan: cert auto pakai tanggal terbit (today) — konsisten online, no change. JIKA HC mau ROMAN sesuai tanggal exam → itu mode cert-manual (HC ketik nomor sendiri, D-09), bukan auto. **Rekomendasi: biarkan auto pakai today (mirror online); flag ke discuss-phase bila stakeholder mau backdate nomor.** `[ASSUMED]` bahwa "today" dapat diterima — lihat Assumptions Log A1.
**Warning signs:** Stakeholder komplain nomor cert ROMAN tidak sesuai bulan ujian.

### Pitfall 3: `GetNextSeqAsync` & visibility cert in-transaction (sequential workers)
**What goes wrong:** Khawatir 2 pekerja dalam 1 batch dapat nomor cert sama.
**Why it doesn't happen (VERIFIED safe):** `GetNextSeqAsync` query `context.AssessmentSessions WHERE NomorSertifikat EndsWith /year` `[VERIFIED: CertNumberHelper.cs:23-35]`. Dalam SATU transaction pada SATU connection, INSERT/UPDATE yang belum commit **TETAP terlihat** oleh read berikutnya pada connection yang sama (read-your-own-writes). Jadi pekerja-2 `GetNextSeqAsync` melihat nomor pekerja-1 (yang di-set via `ExecuteUpdate` dalam tx) → MAX+1 benar. Plus guard `WHERE NomorSertifikat==null` + retry 3× = double-safety. Rollback → INSERT belum commit → nomor ter-reclaim, no committed gap.
**How to avoid:** Tidak perlu intervensi. Tapi PASTIKAN cert generation jalan dalam connection/tx yang sama (default: `_context` yang sama di seluruh service inject = aman). **Catatan:** karena cert pakai `ExecuteUpdate` (raw SQL, bukan change-tracker), pastikan urutan grade pekerja sekuensial (await per pekerja), bukan paralel — service inject memang loop sekuensial (D-04).
**Warning signs:** Test multi-pekerja generate-cert: assert 2 nomor distinct & berurutan.

### Pitfall 4: Cert manual collision — harus pre-flight, bukan tunggu DB error
**What goes wrong:** D-09 cert manual: HC ketik `NomorSertifikat`. Bila tabrakan dengan `IX_AssessmentSessions_NomorSertifikat_Unique` `[VERIFIED: ApplicationDbContext.cs:226-229]` saat SaveChanges → `DbUpdateException` di tengah tx → rollback-all (D-04) tapi UX buruk (semua gagal karena 1 nomor).
**Why it happens:** Unique index baru ketahuan saat write.
**How to avoid:** Pre-flight (D-03/D-09): query `AssessmentSessions WHERE NomorSertifikat IN (manualNumbers)` + cek intra-batch duplicate SEBELUM tx. Bila ada → reject-all dengan pesan per-baris. (`IsDuplicateKeyException` substring `"IX_AssessmentSessions_NomorSertifikat"` cocok dengan nama index `..._Unique` — masih match sebagai fallback safety.)
**Warning signs:** Test: 2 pekerja nomor cert manual sama → pre-flight reject, 0 writes.

### Pitfall 5: Essay yang baru di-`GradeAndCompleteAsync` masih PendingGrading
**What goes wrong:** `GradeAndCompleteAsync` untuk sesi ber-essay set `Status=PendingGrading` lalu **early-return** `[VERIFIED: GradingService.cs:207-248]` — TIDAK generate cert, TIDAK ke Completed.
**Why it happens:** Online: essay tunggu HC nilai manual via `/Admin/EssayGrading`.
**How to avoid:** Inject (D-05): EssayScore sudah ada (input HC), jadi langsung jalankan finalize-block (Pattern 2) SETELAH `GradeAndCompleteAsync` untuk sesi yang `hasEssay`. Deteksi `hasEssay` = ada `PackageQuestion.QuestionType=="Essay"` di ShuffledQuestionIds.
**Warning signs:** SC#3 — sesi essay inject berakhir `PendingGrading` (gagal). Test wajib assert `Status==Completed`.

### Pitfall 6: Insert order & SaveChanges granularity
**What goes wrong:** FK violation bila assignment/response di-insert sebelum session/package punya ID.
**Why it happens:** EF butuh PK ter-generate (identity) sebelum FK bisa di-set.
**How to avoid:** Urutan + SaveChanges bertahap (pola SubmitResurrection seed `[VERIFIED: SubmitResurrectionTests.cs:98-120]`): session→SaveChanges→package→SaveChanges→questions→SaveChanges→options→SaveChanges→assignment+responses→SaveChanges→grade. Semua di dalam SATU transaction (SaveChanges granular tetap dalam tx; commit di akhir).
**Warning signs:** `DbUpdateException` FK saat insert.

## Code Examples

### Instantiate real GradingService dalam test (gold pattern)
```csharp
// Source: SubmitResurrectionTests.cs:68-76 (VERIFIED)
private GradingService NewGradingService(ApplicationDbContext ctx) {
    var fakeNotif = new FakeNotificationService();
    var audit = new AuditLogService(ctx);
    var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
    var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
    var worker = new FakeWorkerDataService();
    return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
}
```
**InjectAssessmentService DI deps** (cermin ini): `ApplicationDbContext`, `GradingService`, `ILogger<InjectAssessmentService>`. Untuk audit in-tx, service inject pakai `_context.AuditLogs.Add` langsung (tidak butuh `AuditLogService`). Actor (userId+name) di-pass sebagai parameter dari caller (Phase 394 controller akan inject identitas; di 393 test pass string actor).

### Disposable real-SQL fixture
```csharp
// Source: SubmitResurrectionTests.cs:25-58 / EssayFinalizeRecomputeTests.cs:19-52 (VERIFIED)
public class InjectAssessmentFixture : IAsyncLifetime {
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;
    public async Task InitializeAsync() {
        var cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.MigrateAsync();   // bila gagal → migration-chain break, bukan bug
    }
    public async Task DisposeAsync() {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();
    }
}
// class test: [Trait("Category","Integration")] + IClassFixture<InjectAssessmentFixture>
```
`HcPortalDB_Dev` TAK disentuh (DB per-test isolasi). `[Trait Category=Integration]` → ter-exclude dari fast suite (`--filter Category!=Integration`).

### Byte-identik assertion (bandingkan inject vs online)
```csharp
// Pola: seed jawaban identik dua jalur, assert Score/IsPassed/ET/cert sama.
// Online path = AssessmentScoreAggregator.Compute (sumber kebenaran yang sama dipakai inject).
var aggOnline = AssessmentScoreAggregator.Compute(questions, responses, passPct);
var injected  = await injectSvc.InjectBatchAsync(req);
var s = await verify.AssessmentSessions.FindAsync(injected.SessionIds[0]);
Assert.Equal(aggOnline.Percentage, s.Score);
Assert.Equal(aggOnline.IsPassed, s.IsPassed);
// ET: bandingkan SessionElemenTeknisScores vs grouping manual
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `BulkBackfill` tulis skor agregat tangan | `InjectAssessmentService` reuse GradingService (full fidelity) | Phase 393 (v32.2) | BulkBackfill di-retire Phase 396; inject = jawaban per-soal + cert + ET |
| `AssessmentType="Manual"` + `AccessToken="BACKFILL"` | `AssessmentType=Standard/PreTest/PostTest` + `AccessToken="INJECT"` | Phase 393 | Render & grouping identik online (Manual punya branch skip) |
| `AuditLogService.LogAsync` (SaveChanges sendiri) | `_context.AuditLogs.Add` in-tx | precedent BulkBackfill | Audit ikut rollback bila tx gagal |

**Deprecated/outdated:** EF Core 8 InMemory provider untuk test grading — TIDAK dukung `ExecuteUpdateAsync` → gunakan disposable SQL Server `[VERIFIED]`.

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua teks user-facing (pesan error per-baris, audit description). Komentar kode boleh ID/EN (repo campur).
- **Develop Workflow:** Verifikasi lokal WAJIB sebelum commit — `dotnet build` (0 error) + `dotnet test` (xUnit hijau). Tidak ada `dotnet run` UI di 393 (no controller/view). Branch `main`. Push hanya bila user minta; notify IT dengan commit hash + flag `migration=FALSE`. ❌ JANGAN edit kode/DB Dev/Prod.
- **0 migration (scope-lock):** Verifikasi `dotnet ef migrations add _verify` → 0 model diff. Service hanya INSERT ke tabel existing; tidak ubah model.
- **Seed Data Workflow:** Test pakai disposable DB (`HcPortalDB_Test_{guid}`), bukan seed ke DB lokal `HcPortalDB_Dev`. Tidak perlu SEED_JOURNAL (DB per-test self-dispose).

## Validation Architecture

> nyquist_validation = true (config.json) → section WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (net8.0) `[VERIFIED: HcPortal.Tests.csproj]` |
| Config file | none (xUnit convention); providers: EF Core 8.0.0 SqlServer + InMemory |
| Quick run (fast, exclude integration) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "Category!=Integration"` |
| Full suite (incl. integration, needs SQLEXPRESS) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Integration DB | `localhost\SQLEXPRESS` (reachable, `TrustServerCertificate=True`) `[VERIFIED: sqlcmd OK 2026-06-17]` |

### Phase Requirements → Test Map (5 Success Criteria)
| SC | Behavior | Test Type | Automated Command | File |
|----|----------|-----------|-------------------|------|
| SC#1 (INJ-01) | Inject hasilkan session set lengkap + skor/lulus/ET/cert **dihitung** byte-identik online (MC/MA/Essay) | integration | `dotnet test --filter "FullyQualifiedName~InjectAssessment&Category=Integration"` | ❌ Wave 0: `InjectAssessmentTests.cs` |
| SC#2 (INJ-01) | NIP invalid / error mid-batch → rollback-all, 0 sesi parsial | integration | idem | ❌ Wave 0 |
| SC#3 (INJ-01) | Sesi essay → `Status=Completed` setelah EssayScore di-set + finalize-block | integration | idem | ❌ Wave 0 |
| SC#4 (INJ-02) | `IsManualEntry=true` + 1 AuditLog `ActionType="ManualInject"` per sesi (count = jumlah sesi) | integration | idem | ❌ Wave 0 |
| SC#5 (INJ-01/02) | build 0 error + test hijau + 0 model diff | build/cli | `dotnet build` + `dotnet test` + `dotnet ef migrations add _verify` (assert no-op) | n/a |

### Test fixtures/assertions yang membuktikan tiap SC
- **SC#1 byte-identik:** seed 3 sesi (MC murni, MA all-or-nothing, Essay ber-EssayScore). Hitung expected via `AssessmentScoreAggregator.Compute` (= mesin yang dipakai online). Assert `session.Score`, `IsPassed`, `SessionElemenTeknisScores` (CorrectCount/QuestionCount per ET), dan `NomorSertifikat` (format `KPB/xxx/ROMAN/year`). Tambah test MA partial-select=salah (all-or-nothing) + MC salah=0.
- **SC#2 atomic:** (a) batch dengan 1 NIP tak ada di AspNetUsers → assert `InjectResult.Rejected`, `AssessmentSessions.Count(inject)==0`. (b) inject exception buatan mid-batch (mis. cert manual collision pada pekerja ke-2) → assert rollback: 0 sesi, 0 assignment, 0 response, 0 audit ManualInject committed.
- **SC#3 essay→Completed:** seed essay session inject ber-EssayScore=80 → setelah `InjectBatchAsync`, assert `Status==Completed` (BUKAN PendingGrading), `Score==80`, `IsPassed==true`. Verifikasi via context baru (read-after-commit).
- **SC#4 audit count:** inject N pekerja sukses → `AuditLogs.Count(a => a.ActionType=="ManualInject")==N`; assert tiap entry punya `TargetId==sessionId`, `Description` berisi NIP+skor. Plus: 1 pekerja duplikat (skip) → `ActionType=="ManualInjectSkipped"` TIDAK menambah count ManualInject (D-11 ActionType terpisah agar count bersih).
- **SC#5 model diff:** Wave 0 boleh skip (manual gate); CI = `dotnet ef migrations add _verify --no-build` lalu assert file migration Up()/Down() kosong → hapus. Atau cukup `dotnet build` + grep no new migration.

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (fast, ~347 baseline tetap hijau — no regression).
- **Per wave merge / phase gate:** full suite (incl. `InjectAssessmentTests` integration) hijau SEBELUM `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/InjectAssessmentTests.cs` — fixture disposable DB + cover SC#1–4. Reuse `FakeWorkerDataService`, `FakeNotificationService`, `AuditLogService`, `ProtonCompletionService`, `ProtonBypassService`, `NullLogger` (sudah ada).
- [ ] Tidak perlu framework install (xUnit + SqlServer provider sudah ada).
- [ ] Helper seed (`SeedUserAsync`, seed package/questions/options) — adaptasi dari `SubmitResurrectionTests`/`EssayFinalizeRecomputeTests` (pola sudah ada, copy & sesuaikan).

## Security Domain

> security_enforcement absent di config → treat as enabled.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no (di 393) | RBAC `Admin,HC` ditegakkan di controller Phase 394 (service tak terekspos HTTP) |
| V3 Session Management | no | service murni, tak ada session web |
| V4 Access Control | partial | Service WAJIB terima actor identity sbg parameter (jangan trust caller blind); RBAC enforce di Phase 394. Audit catat actor (INJ-02). |
| V5 Input Validation | **yes** | Pre-flight D-03: NIP exists, PackageOptionId milik soal, EssayScore 0..ScoreValue (D-07), tanggal ≤ today (D-06), cert nomor unik (D-09). Manual validation (tak pakai zod/pydantic — ini C#). |
| V6 Cryptography | no | tidak ada crypto; cert nomor = sekuens, bukan secret |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Inject sesi untuk pekerja yang tak berhak / data palsu | Tampering / Repudiation | `IsManualEntry=true` + AuditLog `ManualInject` (actor+NIP+skor) — INJ-02 transparansi compliance `[VERIFIED design §10]` |
| Score di luar range / EssayScore liar | Tampering | Pre-flight reject D-03/D-07 (0..ScoreValue), grading hitung sendiri (bukan terima persen final) |
| Double-cert (nomor tabrakan) | Tampering | UNIQUE index `IX_AssessmentSessions_NomorSertifikat_Unique` + pre-flight D-09 + retry auto |
| Partial write saat error | (Integrity) | Transaction atomic D-04, rollback-all |
| Cert untuk yang tidak lulus | Tampering | `GradeAndCompleteAsync` gate cert pada `isPassed` (D-08) — mirror online, otomatis |
| Audit dropped saat rollback | Repudiation | Audit in-tx (`_context.AuditLogs.Add`), ikut commit/rollback |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Nomor cert AUTO boleh pakai tanggal terbit (today) untuk inject historis, mirror online (ROMAN month = bulan inject dijalankan, bukan bulan exam) | Pitfall 2 | MEDIUM — bila stakeholder mau ROMAN sesuai bulan ujian, perlu mode khusus / cert-manual. Sudah ada jalur cert-manual (D-09) sebagai escape. Flag ke discuss bila perlu. |
| A2 | 1 room inject = 1 paket soal untuk semua pekerja (sentinel = paket itu) | Pattern 1 | LOW — eksplisit di spec §14 asumsi + CONTEXT deferred (multi-paket out of scope). |
| A3 | Backdate `CompletedAt`/`Schedule`/`StartedAt` perlu di-re-apply pasca-grade (karena grading overwrite ke UtcNow) | Pitfall 1 | LOW — verified grading set UtcNow; re-apply = langkah aman. Bila ternyata online juga simpan CompletedAt=now, perilaku tetap konsisten; tapi D-06 minta backdate eksplisit → re-apply benar. |
| A4 | Actor identity di-pass sebagai parameter ke service (bukan resolve `User` di service) | Security V4 / Code Examples | LOW — service tak punya HttpContext; Phase 394 controller pass identitas. Standard untuk service layer. |

## Open Questions

1. **Bentuk DTO `InjectRequest`/`InjectResult` (signature service)**
   - What we know: CONTEXT D menyerahkan bentuk DTO ke discretion planner. Input = `(roomSettings, packageSpec/authored questions, List<workerAnswers eksplisit>, certMode)`.
   - What's unclear: apakah PackageSpec menerima entitas `PackageQuestion`/`PackageOption` yang belum ter-persist, atau ID paket yang sudah ada. Karena 393 = backend-only, planner bebas; tapi Phase 394 (authoring) akan men-supply soal → kemungkinan service terima POCO question/option lalu insert. Rekomendasi: service terima POCO (tidak ter-attach), insert sendiri → testable standalone.
   - Recommendation: planner definisikan DTO minimal yang cukup untuk test SC#1–4; perluasan auto-gen (395)/Excel (396) bangun di atasnya.

2. **`ValidUntil` & `CertificateType` untuk cert inject**
   - What we know: `ValidUntil` (DateOnly?) di-set saat **session creation** (line 1469 `ValidUntil=model.ValidUntil`), BUKAN oleh GradingService `[VERIFIED]`. D-10: fleksibel (date atau null=permanent).
   - What's unclear: apakah inject perlu set `CertificateType` juga (field nullable, dipakai display). Online CreateAssessment tampaknya tak selalu set. 
   - Recommendation: set `ValidUntil` di session creation per D-10. `CertificateType` opsional — boleh null (konsisten online package path); tutup bila Phase 394 perlu.

3. **Pre/Post linking (LinkedGroupId/LinkedSessionId) di 393?**
   - What we know: Field ada di model; D Field-values sebut AssessmentType bisa PreTest/PostTest. Tapi linking silang = Phase 397.
   - What's unclear: apakah 393 perlu support set LinkedGroupId saat inject Pre/Post standalone.
   - Recommendation: 393 cukup terima `AssessmentType` + optional `LinkedGroupId`/`LinkedSessionId` sebagai passthrough field (set apa adanya bila disuplai); logika picker/wiring silang = 397. Tidak ada perhitungan khusus di 393.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | build + test | ✓ | net8.0 | — |
| `dotnet build HcPortal.csproj` | SC#5 | ✓ green | 0 error/24 warn | — |
| SQL Server Express (`localhost\SQLEXPRESS`) | integration tests (disposable DB) | ✓ | reachable (TrustServerCertificate) | — (tanpa ini integration test skip — fast suite tetap jalan) |
| EF Core 8 SqlServer provider | disposable DB migrate | ✓ | 8.0.0 | — |
| xUnit + Test SDK | test runner | ✓ | 2.9.3 / 17.13.0 | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — integration tests memerlukan SQLEXPRESS; bila CI tak punya, jalankan fast suite + integration lokal (precedent Phase 387: integration verified lokal).

## Sources

### Primary (HIGH confidence — verified langsung dari source)
- `Services/GradingService.cs:57-336` — `GradeAndCompleteAsync` deps/flow/non-tx/cert/essay-branch
- `Controllers/AssessmentAdminController.cs:3637-3871` — `FinalizeEssayGrading` HTTP-coupling + CORE finalize blocks
- `Helpers/CertNumberHelper.cs:1-44` — cert seq/build/dup-detect
- `Helpers/AssessmentScoreAggregator.cs:26-138` — Compute/IsQuestionCorrect/BuildAnswerCell
- `Controllers/TrainingAdminController.cs:836-985` — BulkBackfill atomic blueprint
- `Controllers/CMPController.cs:1034-1101` (sentinel anchor), `:2184-2391` (Results no Manual-branch), `:4444-4449` (ShouldEnforceSubmitTimer skip Manual)
- `Services/WorkerDataService.cs:118-241` — GetUnifiedRecords no IsManualEntry filter
- `Models/AssessmentSession.cs`, `UserPackageAssignment.cs`, `AssessmentPackage.cs`, `PackageUserResponse.cs`, `SessionElemenTeknisScore.cs`, `AuditLog.cs` — fields/FK
- `Data/ApplicationDbContext.cs:225-229` — UNIQUE filtered index NomorSertifikat
- `HcPortal.Tests/SubmitResurrectionTests.cs`, `EssayFinalizeRecomputeTests.cs`, `FakeWorkerDataService.cs` — test pattern + GradingService wiring
- `Program.cs:54,68` — DI registration; `AuditLogService.cs:40-41` — LogAsync SaveChanges
- `AdminBaseController.cs:271-293` — NormalizeTitleForDup/FindTitleDuplicatesAsync
- `HcPortal.Tests.csproj` — framework versions
- Build hijau + SQLEXPRESS reachable — verified via Bash 2026-06-17

### Secondary / Tertiary
- N/A — semua klaim verified dari codebase (tidak ada WebSearch/external; domain = repo internal).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua komponen reuse, dibaca langsung, tidak ada library baru.
- Architecture: HIGH — pola sentinel/atomic/finalize verified dari 3 source existing + test precedent.
- Pitfalls: HIGH — 6 pitfalls semua diturunkan dari kode aktual (CompletedAt overwrite, cert DateTime.Now, in-tx cert visibility, manual collision, essay PendingGrading, insert order).
- Assumptions: 4 (A1 cert-date MEDIUM risk, sisanya LOW) — flag A1 ke discuss bila stakeholder peduli ROMAN bulan cert.

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (stable — kode internal, no fast-moving external deps; re-verify bila GradingService/FinalizeEssayGrading berubah di phase lain)
