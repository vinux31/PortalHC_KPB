# Phase 381: Worker Entry (StartExam integrity) - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 6 source/test files diubah/dibuat (2 controller edit, 1 helper baru, 2 xUnit baru, 2 e2e extend) + 2 model/service read-only reference
**Analogs found:** 6 / 6 (semua punya analog in-repo; zero "no analog")

> Bahasa: prosa Bahasa Indonesia; kode/path/identifier English.
> **PENTING — line-number drift:** CONTEXT.md & RESEARCH.md mengutip baris lama (mis. guard 905, write-site 962/969/1012, sibling 982). File `CMPController.cs` AKTUAL sudah bergeser karena blok WSE-01/D-05 empty-package pre-check (baris 970-990). **Baris di dokumen ini = VERIFIED live read 2026-06-14.** Gunakan baris ini, bukan baris CONTEXT/RESEARCH.

## File Classification

| File (diubah/dibuat) | Role | Data Flow | Closest Analog | Match Quality |
|----------------------|------|-----------|----------------|---------------|
| `Controllers/CMPController.cs` (StartExam GET edit) | controller | request-response (read-mostly + conditional write-on-GET) | dirinya sendiri — guard precedent `CMPController.cs:911-917` (Phase 377) | exact (in-file precedent) |
| `Controllers/AssessmentAdminController.cs` (reshuffle sibling-query edit) | controller | CRUD / batch (reshuffle write) | dirinya sendiri — `ReshufflePackage` 5189-5194 + `ReshuffleAll` 5258-5263 | exact (in-file precedent) |
| Helper sibling baru `GetSiblingSessionIds*` (lokasi = discretion) | utility (pure/static EF helper) | transform (query → sorted Id list) | `AssessmentAdminController.StandardGroupSiblingPredicate` (2335-2343, Phase 367) | role-match (POLA, bukan reuse) |
| `HcPortal.Tests/SiblingPrePostFilterTests.cs` (baru) | test (unit, pure) | transform | `HcPortal.Tests/SiblingFilterTests.cs` | exact |
| `HcPortal.Tests/SiblingDeterminismTests.cs` (baru) | test (unit, pure) | transform | `HcPortal.Tests/ShuffleReshuffleTests.cs` + `ShuffleEngineTests.cs` | role-match |
| `tests/e2e/exam-taking.spec.ts` (extend) + `tests/e2e/impersonation.spec.ts` (extend) | test (e2e) | request-response + DB-assert | dirinya sendiri + `tests/helpers/dbSnapshot.ts` | exact |

**Read-only reference (tidak diedit):** `Models/AssessmentSession.cs` (AssessmentType :161, LinkedGroupId :172), `Models/UserPackageAssignment.cs` (`GetShuffledQuestionIds()`), `Helpers/ShuffleEngine.cs` (`BuildQuestionAssignment`/`BuildOptionShuffle`), `Services/ImpersonationService.cs` (`IsImpersonating()` :35). **Tidak ada model edit → `migration: false`.**

---

## Pattern Assignments

### `Controllers/CMPController.cs` — StartExam GET (controller, request-response)

**Analog:** dirinya sendiri (in-file precedent Phase 377) + sibling-query reshuffle di `AssessmentAdminController.cs`.

**Field injeksi (sudah ada — JANGAN tambah)** (`CMPController.cs:40,71`):
```csharp
private readonly ImpersonationService _impersonationService;  // line 40 (concrete type, Phase 377)
// ctor: _impersonationService = impersonationService;        // line 71
```
> `_impersonationService.IsImpersonating()` sudah dipakai 4× (486, 637, 912, + GetEffectiveRoleLevel 88). Guard baru me-reuse field ini — **tanpa edit service**.

---

#### Pattern A — Write-on-GET impersonation guard (WSE-05 / D-04) — mirror precedent

**Precedent guard (VERIFIED `CMPController.cs:911-917`)** — INILAH template persis untuk D-04 (membungkus Upcoming→Open):
```csharp
// Phase 377 (Pitfall 3 / T-377-09): write-on-GET guard — JANGAN tulis DB saat impersonasi (read-only invariant).
if (!_impersonationService.IsImpersonating())
{
    assessment.Status = "Open";
    assessment.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

**3 write-site yang HARUS dibungkus dengan `if(!_impersonationService.IsImpersonating())` (1 unit koheren, OPS-01 & TOK-03):**

**Write-site 1 — justStarted DB write (VERIFIED `CMPController.cs:992-997`):**
```csharp
bool justStarted = assessment.StartedAt == null;   // line 968 — DIHITUNG SEBELUM guard (Pitfall 1: JANGAN pindah)
// ...empty-package pre-check (970-990, WSE-01/D-05 — biarkan)...
if (justStarted)                                   // ← bungkus: if (justStarted && !_impersonationService.IsImpersonating())
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

**Write-site 2 — SignalR `workerStarted` + LogActivity (VERIFIED `CMPController.cs:1000-1008`):**
```csharp
if (justStarted)                                   // ← bungkus: if (justStarted && !_impersonationService.IsImpersonating())
{
    var startBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
    await _hubContext.Clients.Group($"monitor-{startBatchKey}").SendAsync("workerStarted",
        new { sessionId = assessment.Id, workerName = user.FullName, status = "InProgress" });
    LogActivityAsync(assessment.Id, "started");
}
```

**Write-site 3 — create UserPackageAssignment + SaveChanges (VERIFIED `CMPController.cs:1042-1087`, di dalam `if (assignment == null)` baris 1042):**
```csharp
if (assignment == null)
{
    // ... build shuffledIds + optionShuffleDict + new UserPackageAssignment {...} (1044-1071) ...
    _context.UserPackageAssignments.Add(assignment);     // line 1072 ← guard di sini
    try { await _context.SaveChangesAsync(); }           // line 1075
    catch (DbUpdateException) { /* race-recovery 1077-1086 (ChangeTracker.Clear + reload) */ }
}
```
> **Pitfall 1 (D-07):** `justStarted` (968) & `isResume` (1156, `= !justStarted`) DIHITUNG dari snapshot pra-guard. Guard hanya membungkus blok write — JANGAN ubah posisi/semantik kedua variabel. Saat impersonate, `StartedAt` tetap null → `justStarted=true`, `isResume=false` → blok SavedAnswers (1180-1205) tak fire (benar: preview = soal kosong).

---

#### Pattern B — In-memory assignment preview saat impersonasi (WSE-05 / D-06)

**Analog = blok create existing itu sendiri (VERIFIED `CMPController.cs:1044-1086`), MINUS `_context.Add`/`SaveChangesAsync`.** Diff shape (gabung dengan guard write-site 3):
```csharp
if (assignment == null)
{
    var rng = Random.Shared;                              // discretion: new Random(id) → preview stabil per-reload
    var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
        packages, assessment.ShuffleQuestions, workerIndex, rng);          // existing 1049-1050
    var assignedQuestions = packages.SelectMany(p => p.Questions)
        .Where(q => shuffledIds.Contains(q.Id));                           // existing 1054-1055
    var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(
        assignedQuestions, assessment.ShuffleOptions, rng);               // existing 1056-1057
    var sentinelPackage = packages.First();                                // existing 1060

    assignment = new UserPackageAssignment
    {
        AssessmentSessionId = id,
        AssessmentPackageId = sentinelPackage.Id,
        UserId = user.Id,                                  // effective user X bila impersonate (resolver Phase 377, line 903)
        ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
        ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict)
    };
    assignment.SavedQuestionCount = shuffledIds.Count;     // existing 1071

    if (!_impersonationService.IsImpersonating())          // ← D-04/D-06: HANYA persist saat worker asli
    {
        _context.UserPackageAssignments.Add(assignment);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { /* existing race-recovery 1077-1086 */ }
    }
    // impersonate → SKIP Add/Save; object in-memory cukup feed view (assignment.GetShuffledQuestionIds()).
}
```
> **Kenapa aman (VERIFIED):** view-build downstream (1089-1225) hanya baca `assignment.GetShuffledQuestionIds()` (1091, 1111), `assignment.ShuffledOptionIdsPerQuestion` (1214), `assignment.Id` (1151 → `vm.AssignmentId`). **TIDAK ada DB re-read assignment pasca-create.** In-memory object cukup. Konsekuensi: `vm.AssignmentId = 0` saat preview (belum persist) — submit oleh admin di luar scope (read-only); planner pastikan tak NRE di `StartExam.cshtml` (UAT Playwright runtime, Pitfall 5 / lesson 354-355).
> Stale-question check (1095, `if (assessment.StartedAt != null && ...)`): saat impersonate `StartedAt==null` → block tak fire (D-07, no extra change).

---

#### Pattern C — Sibling-query + AssessmentType discriminator (WSE-04 / D-01, D-09)

**Site VERIFIED `CMPController.cs:1012-1017`** (sibling-query yang menentukan POOL + workerIndex):
```csharp
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)     // ← TAMBAH diskriminator D-01/D-09 di sini
    .Select(s => s.Id)
    .ToListAsync();
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();   // line 1022 — order WAJIB identik dua sisi (Pitfall 2)
int workerIndex = sortedSiblingIds.IndexOf(id);                      // line 1023 — feed ShuffleEngine OFF≥2
```
> Ganti predikat dengan panggilan helper bersama (Pattern D) yang sudah meng-embed diskriminator **D-09 type-aware** (BUKAN equality ketat — aman legacy):
> ```csharp
> bool isPrePost = assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest";
> // ...Title && Category && Schedule.Date &&
> ( isPrePost
>     ? s.AssessmentType == assessment.AssessmentType
>     : (s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest") )
> ```
> **JANGAN sentuh** blok empty-package pre-check (`CMPController.cs:970-990`) yang punya sibling-query KEDUA tanpa AssessmentType (WSE-01/D-05) — di luar scope 381; sentuh hanya jika planner memutuskan konsistensi (catat eksplisit).

---

### `Controllers/AssessmentAdminController.cs` — reshuffle (controller, CRUD/batch)

**Analog:** dirinya sendiri. 4 titik sibling-query VERIFIED (live grep 2026-06-14):

| Endpoint | Baris sibling-query | Tipe | Aksi | Scope D-02/D-08 |
|----------|--------------------|------|------|-----------------|
| `ReshufflePackage(sessionId)` | **5189-5194** | by `assessment.*` | build + persist 1 assignment (workerIndex 5216-5217) | **IN — pakai helper** |
| `ReshuffleAll(title, category, scheduleDate)` | **5258-5263** | by-param | bulk persist (workerIndex loop 5315) | **IN — pakai helper** |
| `UpdateShuffleSettings(...)` | **5367-5372** | by `assessment.*` | lock-detection (`AnyAsync StartedAt!=null` 5375-5378) | **OUT (D-08) — biarkan unfiltered** |
| `ManagePackages(...)` | **5489-5494** (`shufSiblingIds`) | by `assessment.*` | lock-state read (5496-5503) | **OUT (D-08) — biarkan unfiltered** |

**ReshufflePackage core pattern (VERIFIED `5189-5217`)** — ganti sibling-query inline (5189-5194) dengan helper; `sortedSiblingIds`+`IndexOf` (5216-5217) sudah ada, biarkan:
```csharp
var siblingSessionIds = await _context.AssessmentSessions       // 5189-5194 ← REPLACE dgn helper (Title+Category+Date+AssessmentType)
    .Where(s => s.Title == assessment.Title && s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id).ToListAsync();
// ...packages query 5196-5201...
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();   // 5216 — IDENTIK StartExam:1022
int workerIndex = sortedSiblingIds.IndexOf(sessionId);              // 5217
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(packages, assessment.ShuffleQuestions, workerIndex, rng);  // 5218
```
**ReshuffleAll (VERIFIED `5258-5270`)** — query by-param (`title,category,scheduleDate`); helper harus terima param mentah ATAU caller mem-filter `sessions` per AssessmentType. `sortedSiblingIds`+`IndexOf` loop (5270/5315) sudah ada.

> **Pitfall 3 (D-08 + Open Question 1):** D-02 berkata "SEMUA reshuffle" tapi `UpdateShuffleSettings`/`ManagePackages` BUKAN reshuffle (lock-detection). D-08 koreksi: helper + AssessmentType **HANYA** ke StartExam + ReshufflePackage + ReshuffleAll. Dua titik lock DIBIARKAN group-wide (hindari ubah semantik lock v27). Planner: jangan terapkan AssessmentType filter di 5367-5372 & 5489-5494.

---

### Helper sibling baru (utility, transform) — D-02

**Analog POLA (bukan reuse):** `AssessmentAdminController.StandardGroupSiblingPredicate` (VERIFIED `2335-2343`, Phase 367):
```csharp
// #18 Phase 367: predikat sibling — single-source query EF + diuji SiblingFilterTests. JANGAN reuse:
// predicate ini EXCLUDE PreTest/PostTest + LinkedGroupId!=null + IsManualEntry (scope DeleteAssessmentGroup).
public static System.Linq.Expressions.Expression<Func<AssessmentSession, bool>> StandardGroupSiblingPredicate(
    string title, string category, DateTime scheduleDate)
    => a => a.Title == title
            && a.Category == category
            && a.Schedule.Date == scheduleDate
            && a.LinkedGroupId == null
            && a.AssessmentType != "PreTest"
            && a.AssessmentType != "PostTest"
            && !a.IsManualEntry;
```
**Helper BARU** harus INCLUDE AssessmentType-match (D-09 type-aware), BEDA dari predicate di atas. Bentuk yang direkomendasikan (signature/lokasi = Claude's discretion, selama dipakai IDENTIK dua sisi):
```csharp
// Opsi 1 — static async (return sorted Ids), dipanggil langsung di StartExam + ReshufflePackage:
public static async Task<List<int>> GetSiblingSessionIdsAsync(
    ApplicationDbContext context, string title, string category, DateTime scheduleDate, string? assessmentType)
{
    bool isPrePost = assessmentType == "PreTest" || assessmentType == "PostTest";
    var ids = await context.AssessmentSessions
        .Where(s => s.Title == title && s.Category == category && s.Schedule.Date == scheduleDate.Date &&
                    (isPrePost ? s.AssessmentType == assessmentType
                               : (s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest")))
        .Select(s => s.Id).ToListAsync();
    return ids.OrderBy(x => x).ToList();        // Pitfall 2 — sort in-memory, IDENTIK dua sisi
}

// Opsi 2 (lebih mudah diuji tanpa DB, ikut POLA Phase 367) — pure Expression + .Compile():
public static Expression<Func<AssessmentSession, bool>> SiblingPrePostAwarePredicate(
    string title, string category, DateTime scheduleDate, string? assessmentType) { /* sama Where di atas */ }
```
> **Rekomendasi:** Opsi 2 (Expression) memudahkan unit test in-memory (lihat analog `SiblingFilterTests.cs` yang pakai `.Compile()`), TAPI workerIndex/order tetap perlu `.OrderBy(x=>x)` di caller — pastikan kedua caller (StartExam + reshuffle) sort dengan cara identik.

---

### `HcPortal.Tests/SiblingPrePostFilterTests.cs` (baru) — test, unit pure

**Analog EXACT:** `HcPortal.Tests/SiblingFilterTests.cs` (VERIFIED penuh). Pola: factory `S(...)` bikin `AssessmentSession`, `Pred()` = `Predicate(...).Compile()`, lalu `[Fact]` assert include/exclude in-memory tanpa DB.
```csharp
// Pola dari SiblingFilterTests.cs:15-23 (factory + Compile)
private static AssessmentSession S(int id, string? type = null, string title = "Welding", string category = "OJ") =>
    new AssessmentSession { Id = id, UserId = "u1", Title = title, Category = category, Schedule = Sched,
                            Status = "Open", AccessToken = "", AssessmentType = type };
private static Func<AssessmentSession, bool> Pred(string? type) =>
    AssessmentAdminController.SiblingPrePostAwarePredicate("Welding", "OJ", Sched.Date, type).Compile();
// [Fact] Pre→hanya PreTest match; Post→hanya PostTest; Standard/""/null→ satu grup non-PrePost (D-09)
```
**Coverage wajib (RESEARCH Wave 0):** Pre-set ≠ Post-set; `Standard`/`""`/`null` dikelompokkan bersama (D-09 tidak terpecah); beda Title/Category → bukan sibling.

### `HcPortal.Tests/SiblingDeterminismTests.cs` (baru) — test, unit pure

**Analog:** `HcPortal.Tests/ShuffleReshuffleTests.cs` (pola pure ShuffleEngine, no DB) + `ShuffleEngineTests.cs`. Assert: untuk input sibling-set yang sama, `OrderBy(x=>x).IndexOf(id)` IDENTIK → menjamin `workerIndex` StartExam == reshuffle (invariant Phase 373 D-02). Sampling matriks {1 paket, ≥2 paket} × {Pre, Post, Standard}.

### `tests/e2e/*.spec.ts` (extend) — test, e2e + DB-assert

**Harness existing yang dipakai (VERIFIED):**
- `tests/e2e/helpers/examTypes.ts:610` — `createPrePostAssessmentViaWizard(page, opts) → {preIds, postIds}` (E2E #4 setup).
- `tests/e2e/helpers/examTypes.ts:166/240` — `createDefaultPackage`, `addQuestionViaForm` (isi paket Pre vs Post beda soal).
- `tests/e2e/impersonation.spec.ts:38/55/50` — `startUser(page,query,target)`, `resolveUser(page,query,fullName)`, `stopImpersonation(page)` via `/Admin/Impersonate` + `/Admin/SearchUsersApi` (E2E #7 setup).
- `tests/helpers/dbSnapshot.ts:116` — `queryScalar(sql)` (DB assert no-mutation); `:67/:80` `backup`/`restore` (localhost-guarded; combined run WAJIB `--workers=1`).
- `tests/helpers/accounts.ts` — worker = `coachee` (rino.prasetyo@pertamina.com) / `coachee2` (iwan3@pertamina.com); admin.

**E2E #4 (WSE-04, entry-pool only):** create PrePost same-day via wizard → isi paket Pre vs Post dengan soal beda → login worker → `GET /CMP/StartExam/{preId}` → assert jumlah & teks soal == paket Pre saja (DOM `[id^="qcard_"]`), Post tak tercampur. *Full pass/grade = acceptance pasca-382 (Deferred).*

**E2E #7 (WSE-05, no-mutation):** login admin → `startUser` worker target → `goto /CMP/StartExam/{id}` (Open, StartedAt null, non-token) → assert via `db.queryScalar`:
```sql
SELECT CASE WHEN StartedAt IS NULL THEN 1 ELSE 0 END FROM AssessmentSessions WHERE Id={id}   -- 1
SELECT COUNT(*) FROM AssessmentSessions WHERE Id={id} AND Status='Open'                       -- 1
SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId={id}                    -- 0
```
→ `stopImpersonation` → `login(page,'coachee')` → `goto /CMP/StartExam/{id}` → assert `StartedAt IS NOT NULL`=1 + `UserPackageAssignments COUNT`=1 (SC#3 deferred-start). SignalR `workerStarted` absence = konsekuensi guard, diverifikasi via 3 DB-assert (tak ada hook spy di harness).

---

## Shared Patterns

### Impersonation read-only guard
**Source:** `Controllers/CMPController.cs:911-917` (Phase 377 precedent) + field `_impersonationService` (`:40`).
**Apply to:** 3 write-site StartExam (D-04). `if (!_impersonationService.IsImpersonating()) { /* write */ }`. JANGAN guard `VerifyToken` (D-05 — hanya TempData, bukan mutasi DB).

### Sibling-set determinisme (sort + IndexOf)
**Source:** `CMPController.cs:1022-1023` (StartExam) ↔ `AssessmentAdminController.cs:5216-5217` (ReshufflePackage) ↔ `5270/5315` (ReshuffleAll).
**Apply to:** Helper baru harus mengembalikan list ter-`OrderBy(x => x)` (atau caller sort identik) → `workerIndex = IndexOf(id)` konsisten lintas StartExam ↔ reshuffle (Pitfall 2, invariant Phase 373). `ShuffleEngine.cs:58` OFF≥2 = `packagesWithQuestions[workerIndex % count]` — divergensi order = worker pindah paket.

### Pure shuffle engine (no DB)
**Source:** `Helpers/ShuffleEngine.cs:39` `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` + `:67` `BuildOptionShuffle(...)`.
**Apply to:** dipanggil apa adanya untuk preview in-memory D-06 (pure → tak perlu DB/persist). **Depends Phase 380:** ON-path `K=Min` (`ShuffleEngine.cs:~108`) belum filter paket kosong (SHF-01); OFF-path sudah filter (`:53-57` "D-02b guard SEBELUM modulo"). Land 380 dulu untuk test ON-path empty-package (Pitfall 4) — di luar scope 381.

### EF + JSON serialize untuk shuffled IDs
**Source:** `CMPController.cs:1067-1068` `JsonSerializer.Serialize(shuffledIds)` + `Models/UserPackageAssignment.cs:60` `GetShuffledQuestionIds()`.
**Apply to:** in-memory assignment D-06 set field JSON string yang sama → view-build deserialize identik (tak butuh persist).

### Pure unit test via Compile() / static-call
**Source:** `HcPortal.Tests/SiblingFilterTests.cs` (Expression `.Compile()`), `ImpersonationIdentityTests.cs` (`[Theory]+[InlineData]` static decision), `ShuffleReshuffleTests.cs` (ShuffleEngine pure).
**Apply to:** SiblingPrePostFilterTests + SiblingDeterminismTests (RED-first Wave 0; no Moq, no DB).

### sqlcmd DB assert di e2e
**Source:** `tests/helpers/dbSnapshot.ts:116` `queryScalar` (localhost-guarded `-S localhost\SQLEXPRESS`, `-b` exit-on-error).
**Apply to:** E2E #7 no-mutation assert. Combined Playwright run WAJIB `--workers=1` (DB isolation, memory: local e2e SQL env fix + SQLBrowser start manual).

## No Analog Found

Tidak ada. Seluruh perubahan 381 = menyusun ulang komponen existing + extend test harness existing. Semua punya in-repo analog/precedent.

## Bukti D-01 (AssessmentType = satu-satunya pemisah Pre/Post)

**VERIFIED `AssessmentAdminController.cs`:** Pre `AssessmentType="PreTest"` (line **1235**); Post `AssessmentType="PostTest"` (line **1271**). KEDUANYA share `LinkedGroupId = preSessions[0].Id` — Post di **1272**, Pre di **1288** (`preSessions[i].LinkedGroupId = linkedGroupId`). → LinkedGroupId TIDAK memisahkan Pre/Post (D-01). Normal create set `AssessmentType="Standard"` (RESEARCH cite :1451) → filter no-op untuk normal exam.

## Metadata

**Analog search scope:** `Controllers/` (CMPController, AssessmentAdminController), `Helpers/ShuffleEngine.cs`, `Models/` (AssessmentSession, UserPackageAssignment), `Services/ImpersonationService.cs`, `HcPortal.Tests/` (Sibling/Shuffle/Impersonation), `tests/e2e/` + `tests/helpers/`.
**Files scanned (read):** 12 (5 source, 4 test, 3 e2e-harness).
**Pattern extraction date:** 2026-06-14
**Line numbers:** VERIFIED live (drift dari CONTEXT/RESEARCH dikoreksi: guard 911-917, write-sites 992/1000/1042, sibling 1012, reshuffle 5189/5258, lock-detect 5367/5489).
