# Phase 381: Worker Entry (StartExam integrity) - Research

**Researched:** 2026-06-14
**Domain:** ASP.NET Core MVC (.NET) — exam-entry handler integrity (sibling-pool diskriminasi Pre/Post + write-on-GET impersonation guard) di aplikasi existing Portal HC KPB
**Confidence:** HIGH (semua klaim diverifikasi langsung dari source di repo; keputusan sudah LOCKED di CONTEXT.md)

> Catatan bahasa: prosa Bahasa Indonesia; istilah teknis / nama simbol / path / kode tetap English.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**WSE-04 — Diskriminator pool sibling Pre/Post**
- **D-01:** Sibling-query StartExam ditambah `s.AssessmentType == assessment.AssessmentType`. AssessmentType WAJIB & satu-satunya pemisah Pre vs Post. **LinkedGroupId TIDAK dipakai** sebagai pemisah Pre/Post — Pre & Post share nilai LinkedGroupId yang sama (`= preSessions[0].Id`, di-set di KEDUA sisi). AssessmentType bernilai `"PreTest"`/`"PostTest"`/`"Standard"`. Normal exam (`"Standard"`) tak berubah perilaku (filter `Standard==Standard` no-op).

**WSE-04 — Determinisme sibling-set (invariant Phase 373)**
- **D-02:** Extract helper bersama (mis. `GetSiblingSessionIds(assessment)`) dipakai BOTH `StartExam` GET dan SEMUA endpoint reshuffle. Filter identik (`Title + Category + Schedule.Date + AssessmentType`) + order `OrderBy(x => x)` → sibling-set & workerIndex konsisten lintas StartExam ↔ reshuffle. Menjaga invariant OFF≥2 round-robin (Phase 373).
- **D-03 (SCOPE EXPANSION eksplisit):** Fix WSE-04 WAJIB juga menyentuh sibling-query reshuffle di `AssessmentAdminController.cs`. Ini BUKAN scope creep — konsekuensi-benar agar tak memecah determinisme. Reshuffle hygiene SHF-02/03 TETAP out-of-scope: hanya samakan filter sibling, tak mengubah perilaku reshuffle.

**WSE-05 — Guard write-on-GET impersonasi (OPS-01 / TOK-03)**
- **D-04:** Bungkus 3 write site di `StartExam` GET dengan `if(!_impersonationService.IsImpersonating())`, mirror precedent Phase 377 line 905: (1) justStarted Status="InProgress"+StartedAt+SaveChanges; (2) SignalR `workerStarted` + LogActivityAsync; (3) create UserPackageAssignment+SaveChanges. SATU perubahan koheren (OPS-01 & TOK-03 = 1 fix, tak dipecah).
- **D-05:** `VerifyToken` TIDAK disentuh — hanya menulis TempData (state session per-request, BUKAN mutasi DB worker).

**WSE-05 — Render ujian saat impersonasi (assignment belum ada)**
- **D-06:** Saat impersonate DAN `assignment == null`: build `ShuffledQuestionIds` + `optionShuffleDict` DI MEMORI (panggil ShuffleEngine) TANPA `_context.Add`/`SaveChangesAsync`. Admin lihat preview soal read-only, zero mutasi DB. Saat worker asli login & StartExam → assignment baru ter-create & persist normal.
- **D-07:** Block stale-question-check tak terpengaruh — sudah dijaga `assessment.StartedAt != null`; saat impersonate-belum-mulai StartedAt null → block tak fire.

### Claude's Discretion
- Nama/signature/lokasi helper sibling (`GetSiblingSessionIds`, private method vs util bersama) — selama dipakai IDENTIK StartExam + reshuffle.
- Stabilitas RNG preview impersonasi: assignment in-memory re-shuffle tiap reload. Boleh seed stabil (derive dari `id`) atau biarkan acak — preview saja, tak memengaruhi worker.
- Cara cabang "impersonate-render in-memory" mengonsumsi `_impersonationService.IsImpersonating()`.
- Wording pesan (bila ditampilkan) saat mode impersonate.

### Deferred Ideas (OUT OF SCOPE)
- **Full PrePost pass/grade E2E** (#4 lanjutan) = acceptance test pasca-Phase 382, BUKAN gate 381.
- Reshuffle hygiene SHF-02/03 (orphan/SavedQuestionCount drop pada Abandoned).
- Grading/lifecycle/cert (SAVE-01/STAT/TMR/TOK-02/CERT-01) → Phase 382.
- Proton, essay, multi-answer.
- One-time cleanup data test/audit lokal pasca-367 (chore, tak terkait scope 381).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **WSE-04** | Worker yang mengerjakan Pre/Post same-day menerima HANYA paket ujian itu (Pre & Post tak tercampur). | §Architecture Pattern 1 (sibling helper + AssessmentType filter); §Code Examples (sibling query diff); bukti D-01 di §Standard Stack (Pre/Post create share Title+LinkedGroupId, beda hanya AssessmentType — `AssessmentAdminController.cs:1235/1271/1272/1288`). Determinisme invariant dijaga via §Pattern 2 (helper dipakai identik StartExam+reshuffle). |
| **WSE-05** | Admin yang impersonate/membuka ujian worker TIDAK memulai ujian atau membakar waktu/mengunci shuffle worker. | §Architecture Pattern 3 (3 write-site guard, mirror line 905); §Architecture Pattern 4 (in-memory preview D-06 — view-build hanya baca object `assignment`, no DB re-read pasca-create); §Pitfall 1 (justStarted vs isResume interplay); resolver Phase 377 (`user` = effective/impersonated user). |
</phase_requirements>

## Summary

Phase 381 adalah fix integritas pada SATU handler kunci — `CMPController.StartExam` GET (baris 887-1078) — plus mirror filter sibling ke `AssessmentAdminController`. Dua cacat: (WSE-04) query sibling memakai `Title+Category+Schedule.Date` TANPA `AssessmentType`, sehingga Pre & Post same-day (yang share Title + LinkedGroupId, beda hanya AssessmentType) saling memungut paket; dan (WSE-05) guard write-on-GET impersonasi dari Phase 377 hanya membungkus transisi Upcoming→Open (baris 905), sedangkan 3 mutasi lain (set StartedAt/InProgress, broadcast SignalR workerStarted, create UserPackageAssignment) tidak dijaga → admin yang impersonate worker X dan membuka ujian X membakar timer X + mengunci shuffle X.

Semua keputusan implementasi sudah LOCKED (D-01..D-07). Riset ini memvalidasi bahwa keputusan tersebut benar terhadap kode aktual dan mengunci bentuk-kode persisnya: (1) Pre/Post create (`AssessmentAdminController.cs:1235/1271/1272/1288`) mengonfirmasi AssessmentType = satu-satunya pemisah; (2) normal exam set `AssessmentType="Standard"` (`:1451`, kolom NOT NULL) sehingga filter no-op; (3) ShuffleEngine OFF≥2-path memakai `workerIndex % packagesWithQuestions.Count` (`ShuffleEngine.cs:58`) — INILAH alasan sibling-set + order harus identik antara StartExam dan reshuffle (D-02); (4) view-model build (1058-1214) hanya membaca dari object `assignment` (via `GetShuffledQuestionIds()` + field string), TIDAK ada DB re-read pasca-create, sehingga preview in-memory D-06 aman; (5) `user` di StartExam sudah effective/impersonated user (resolver `GetCurrentUserRoleLevelAsync`, Phase 377).

**Primary recommendation:** Buat satu static sibling-helper bergaya `StandardGroupSiblingPredicate` (precedent Phase 367, diuji `SiblingFilterTests.cs`) yang memfilter `Title+Category+Schedule.Date+AssessmentType` dan dipakai IDENTIK di StartExam GET + ReshufflePackage + ReshuffleAll (plus titik sibling-query lain demi konsistensi). Bungkus 3 write-site StartExam dengan `if(!_impersonationService.IsImpersonating())` (mirror 905), dan saat impersonate + assignment==null build assignment in-memory tanpa persist. Phase 380 (SHF-01 ShuffleEngine ON-path empty-filter) WAJIB landing dulu karena StartExam meng-consume `BuildQuestionAssignment` (baris 1019).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Diskriminasi pool sibling Pre/Post (WSE-04) | API/Backend (`CMPController.StartExam`) | API/Backend (`AssessmentAdminController` reshuffle) | Query EF terhadap `AssessmentSessions` — server-authoritative; klien tak boleh menentukan set soal. Helper sibling cross-controller. |
| Determinisme sibling-set + workerIndex | API/Backend (shared static helper) | — | `IndexOf(id)` pada sorted sibling list → konsumsi `ShuffleEngine` pure. Wajib identik dua sisi (StartExam=read, reshuffle=write). |
| Guard write-on-GET impersonasi (WSE-05) | API/Backend (`StartExam` GET) | Session (`ImpersonationService`) | Mutasi DB hanya boleh dari aksi worker asli; impersonate = read-only invariant (Phase 377). Keputusan baca dari session state (`IsImpersonating()`). |
| Preview soal in-memory saat impersonasi (D-06) | API/Backend (build VM tanpa SaveChanges) | Browser (render read-only) | Object `UserPackageAssignment` non-persisted feed `PackageExamViewModel`; view murni render. |
| Resolusi identitas efektif (`user`) | Session (`ImpersonationService.GetEffectiveUserAsync`) | API/Backend (`GetCurrentUserRoleLevelAsync`) | Phase 377 single-source; StartExam owner-check & UserId assignment pakai effective user. |
| Shuffle/option distribution | Pure engine (`Helpers/ShuffleEngine.cs`) | — | Tanpa DB; di-consume StartExam + reshuffle. SHF-01 fix = Phase 380 (dependency). |

## Standard Stack

Phase ini **tidak menambah dependency baru**. Semua tooling sudah ada di repo. Tabel di bawah = komponen existing yang di-reuse (bukan install baru).

### Core (existing, di-reuse)
| Komponen | Lokasi | Purpose | Kenapa dipakai |
|----------|--------|---------|----------------|
| `CMPController.StartExam` (GET) | `Controllers/CMPController.cs:887-1078` | Handler entry ujian — target utama fix WSE-04 + WSE-05 | Satu-satunya handler yang menyentuh sibling-query (982-987) + 3 write-site + consume ShuffleEngine (1019) |
| `ImpersonationService` | `Services/ImpersonationService.cs` | `IsImpersonating()` (`:35`), `GetEffectiveUserAsync` (`:172`), `GetMode()`, `GetEffectiveRoleLevel()` | Phase 377 single-source; sudah di-inject ke CMPController (`:40` concrete type `ImpersonationService`). Guard 3 write-site tanpa edit service. |
| `ShuffleEngine` (static) | `Helpers/ShuffleEngine.cs` | `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` + `BuildOptionShuffle(...)` | Pure (no DB) → dipanggil in-memory untuk preview impersonasi (D-06) tanpa persist. |
| `StandardGroupSiblingPredicate` (precedent) | `Controllers/AssessmentAdminController.cs:2327-2335` | Pola static sibling-predicate (Phase 367) | TEMPLATE persis untuk helper D-02 (Expression/method static + diuji `SiblingFilterTests.cs`). |
| `UserPackageAssignment` (model) | `Models/UserPackageAssignment.cs` | `GetShuffledQuestionIds()` (`:60`), field `ShuffledQuestionIds`/`ShuffledOptionIdsPerQuestion` (string JSON) | View-build baca dari object ini → in-memory object cukup untuk D-06 (tak butuh persist). |

### Supporting (test harness existing)
| Komponen | Lokasi | Purpose | When to Use |
|----------|--------|---------|-------------|
| xUnit harness | `HcPortal.Tests/*.cs` | Unit/integration test | Determinism helper test (pure), guard logic |
| `SiblingFilterTests.cs` | `HcPortal.Tests/SiblingFilterTests.cs` | Pola uji static predicate via `.Compile()` in-memory, no DB | TEMPLATE test sibling helper D-02 |
| `ImpersonationIdentityTests.cs` | `HcPortal.Tests/ImpersonationIdentityTests.cs` | `[Theory]+[InlineData]` pure-logic resolver decision | Pola uji keputusan impersonate (jika logika baru di-extract) |
| Playwright e2e | `tests/e2e/*.spec.ts` | E2E scenario | E2E #4 (PrePost pool) + #7 (impersonate read-only) |
| `helpers/examTypes.ts` | `tests/e2e/helpers/examTypes.ts` | `createPrePostAssessmentViaWizard` (return preIds/postIds), `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep` | Setup E2E #4 |
| `impersonation.spec.ts` harness | `tests/e2e/impersonation.spec.ts` | `startUser`/`resolveUser`/`stopImpersonation` via `/Admin/Impersonate` + `SearchUsersApi` | Setup E2E #7 |
| `helpers/dbSnapshot.ts` | `tests/helpers/dbSnapshot.ts` | `queryScalar(sql)`, `queryString(sql)`, `backup`/`restore` via sqlcmd (localhost-guarded) | DB assert E2E #7 (StartedAt null, Status Open, no UserPackageAssignment row) |
| `helpers/auth.ts` + `accounts.ts` | `tests/helpers/` | `login(page, key)`; worker = `coachee` (rino.prasetyo@pertamina.com) / `coachee2` (iwan3@pertamina.com) | Login worker/admin di e2e |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `AssessmentType` filter (D-01) | `LinkedGroupId` filter | DITOLAK locked: Pre & Post share LinkedGroupId yang sama (`preSessions[0].Id`) → tak memisahkan. Hanya berguna isolasi antar-group (edge, di luar scope). |
| Static helper baru | Reuse `StandardGroupSiblingPredicate` (`:2327`) | Predicate existing EXCLUDE PreTest/PostTest + LinkedGroupId!=null + manual (scope DeleteAssessmentGroup), JADI TIDAK COCOK untuk Pre/Post entry. Buat helper baru yang INCLUDE AssessmentType-match. Ikuti POLA-nya (static + diuji), bukan reuse-nya. |
| Build assignment in-memory (D-06) | Persist lalu rollback / skip render | DITOLAK locked: persist = mutasi DB (melanggar read-only invariant); skip render = admin tak bisa preview. In-memory = zero-mutation + preview tetap jalan. |

**Installation:** Tidak ada. Tidak ada package baru, tidak ada migration (`migration: false`).

**Version verification:** N/A — tidak ada dependency baru. Semua komponen internal repo (verified by direct file read 2026-06-14). [VERIFIED: repo grep/read]

## Architecture Patterns

### System Architecture Diagram

```
                         WORKER ASLI                          ADMIN (impersonate worker X)
                              │                                         │
                              ▼                                         ▼
                   GET /CMP/StartExam/{id}  ◄──────────────────────────┤  (buka ujian X)
                              │
                              ▼
         ┌────────────────────────────────────────────────┐
         │  (user, _) = GetCurrentUserRoleLevelAsync()      │  ← effective user (X bila impersonate)  [377]
         │  owner-check: assessment.UserId == user.Id ...   │
         └───────────────────────────┬──────────────────────┘
                                      │
                                      ▼
                 ┌──────────────── IsImpersonating()? ───────────────┐
                 │ NO (worker asli)                                  │ YES (admin impersonate)
                 ▼                                                    ▼
   ┌─────────────────────────────┐                    ┌──────────────────────────────────┐
   │ WRITE-SITE 1: justStarted    │                    │  D-04 GUARD: SKIP semua write       │
   │   StartedAt=now,Status=InProg│                    │   StartedAt tetap null              │
   │   SaveChangesAsync           │                    │   Status tetap Open                 │
   │ WRITE-SITE 2: SignalR        │                    │   NO SignalR workerStarted          │
   │   workerStarted + LogActivity│                    │   NO LogActivity                    │
   └──────────────┬──────────────┘                    └─────────────────┬──────────────────┘
                  │                                                      │
                  ▼                                                      ▼
   ┌──────────────────────────────────────────────────────────────────────────────────┐
   │ siblingSessionIds = GetSiblingSessionIds(assessment)    ← D-01/D-02 HELPER          │
   │   filter: Title + Category + Schedule.Date + AssessmentType   (NEW: AssessmentType) │
   │ sortedSiblingIds = siblingSessionIds.OrderBy(x=>x)      ← order WAJIB identik        │
   │ workerIndex = sortedSiblingIds.IndexOf(id)             ← feed ShuffleEngine OFF≥2    │
   └──────────────────────────────────┬───────────────────────────────────────────────┘
                                       ▼
                         packages = AssessmentPackages WHERE sibling
                                       │
                       ┌────── assignment == null? ──────┐
                       │ YES                              │ NO (resume / sudah ada)
                       ▼                                  ▼
   ┌───────────────────────────────┐       (read existing assignment — read-only, OK saat impersonate)
   │ ShuffleEngine.BuildQuestion... │
   │ ShuffleEngine.BuildOption...   │
   ├─ worker asli: WRITE-SITE 3:    │
   │    _context.Add + SaveChanges  │  ← D-04 GUARD
   ├─ impersonate: D-06 IN-MEMORY:  │
   │    new UserPackageAssignment{} │  ← set ShuffledQuestionIds string, NO Add/Save
   └───────────────┬───────────────┘
                   ▼
   Build PackageExamViewModel  (baca assignment.GetShuffledQuestionIds() + packages — NO DB re-read)
                   ▼
   return View(vm)   →  StartExam.cshtml render soal
```

Aliran kunci: helper sibling (D-01/D-02) menyuplai BOTH set paket (apa yang dipool) DAN `workerIndex` (paket mana untuk OFF≥2). Reshuffle endpoints di `AssessmentAdminController` membangun aliran identik di sisi tulis — INILAH kenapa helper harus dipakai dua sisi (jika divergen, `workerIndex` beda → worker pindah paket).

### Recommended Project Structure (yang disentuh — bukan struktur baru)
```
Controllers/
├── CMPController.cs               # StartExam GET (887-1078): D-01 sibling, D-04 guard ×3, D-06 in-memory
└── AssessmentAdminController.cs   # D-02/D-03: ReshufflePackage(5160), ReshuffleAll(5248), +sibling-query
                                   #   konsumen (UpdateShuffleSettings 5352, ManagePackages 5421) demi konsistensi
Helpers/ (atau Controllers/ static) # D-02: GetSiblingSessionIds helper baru (lokasi = Claude's discretion)
HcPortal.Tests/
├── SiblingFilterTests.cs (pola)   # template uji helper sibling D-02
└── (test baru determinism)
tests/e2e/
├── exam-taking.spec.ts            # E2E #4 (atau spec baru) — PrePost pool-only
└── impersonation.spec.ts          # E2E #7 — impersonate read-only (extend)
```

### Pattern 1: Sibling-query dengan diskriminator AssessmentType (WSE-04 / D-01)
**What:** Tambah `s.AssessmentType == assessment.AssessmentType` ke 4 predikat filter sibling (Title+Category+Schedule.Date).
**When to use:** Setiap query yang menentukan set paket / set worker untuk satu ujian.
**Edge yang diverifikasi:**
- Normal exam set `AssessmentType="Standard"` (kolom NOT NULL, `AssessmentAdminController.cs:1451`) → grup all-Standard → filter `Standard==Standard` no-op. [VERIFIED: read :1451]
- Pre create `AssessmentType="PreTest"` (`:1235`), Post `AssessmentType="PostTest"` (`:1271`) → filter MEMISAHKAN. [VERIFIED]
- ⚠️ Legacy null: model comment "null = tidak ditentukan (backward compat)" (`AssessmentSession.cs:158`). Jika ada sesi lama dengan `AssessmentType == null`, equality `null == null` di EF SQL menjadi `IS NULL` (C#/LINQ-to-SQL: `s.AssessmentType == assessment.AssessmentType` di-translate ke `WHERE AssessmentType = @p` yang TIDAK match NULL kecuali EF emit null-aware). Untuk grup Standard semua "Standard" → tak ada isu. Risiko hanya jika satu grup campur null+Standard (sangat tidak mungkin pasca-ISS-04). **Rekomendasi planner:** dokumentasikan asumsi "grup homogen AssessmentType" di plan; tak perlu null-coalesce karena create selalu set nilai eksplisit. [VERIFIED mekanisme; ASUMSI homogenitas data]

**Example (StartExam, baris 982-987 → diff):**
```csharp
// Source: Controllers/CMPController.cs:982-987 (VERIFIED current)
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date &&
                s.AssessmentType == assessment.AssessmentType)   // D-01 NEW
    .Select(s => s.Id)
    .ToListAsync();
```

### Pattern 2: Shared static sibling-helper untuk determinisme cross-call (WSE-04 / D-02, D-03)
**What:** Ekstrak helper sekali (signature = Claude's discretion), dipakai IDENTIK di StartExam + reshuffle. Order `OrderBy(x => x)` HARUS sama dua sisi.
**Why critical:** `ShuffleEngine.BuildQuestionAssignment` OFF≥2-path memilih paket via `packagesWithQuestions[workerIndex % count]` (`ShuffleEngine.cs:58`). `workerIndex = sortedSiblingIds.IndexOf(id)`. Jika StartExam pakai sibling-set {Pre+Post} tapi reshuffle pakai {Pre saja} (atau sebaliknya), `IndexOf(id)` beda → worker dapat paket beda saat reshuffle vs entry → memicu stale-question reset + memecah invariant Phase 373. [VERIFIED: ShuffleEngine.cs:39-60 + StartExam 992-993 + ReshufflePackage 5208-5210]
**Precedent:** Pola `StandardGroupSiblingPredicate` (`AssessmentAdminController.cs:2327`, Phase 367) — static method/Expression diuji in-memory tanpa DB (`SiblingFilterTests.cs`). JANGAN reuse predicate itu (ia exclude PreTest/PostTest) — buat helper baru yang INCLUDE match AssessmentType.
**4 titik konsumsi sibling-query di `AssessmentAdminController.cs` (verified):**
| Endpoint | Baris query | Tipe | Reshuffle write? |
|----------|-------------|------|------------------|
| `ReshufflePackage(sessionId)` | 5181-5186 | by `assessment.*` | YA (build+persist assignment) |
| `ReshuffleAll(title, category, scheduleDate)` | 5250-5255 | by-param | YA (bulk persist) |
| `UpdateShuffleSettings(...)` | 5359-5364 | by `assessment.*` | TIDAK (set flag+lock-check) |
| `ManagePackages(...)` | 5481-5486 | by `assessment.*` | TIDAK (lock-state read) |

> ⚠️ **Klarifikasi penting untuk planner:** CONTEXT menyebut "reshuffle Post ~5482" — di kode AKTUAL hanya ADA DUA endpoint reshuffle: `ReshufflePackage` (5160) dan `ReshuffleAll` (5248). Baris ~5359 = `UpdateShuffleSettings`, ~5481 = `ManagePackages` (keduanya BUKAN reshuffle, tapi konsumen sibling-query yang sama). D-03 mensyaratkan determinisme: titik yang **wajib** identik = `ReshufflePackage` + `ReshuffleAll` (mereka compute `workerIndex` & build assignment). `UpdateShuffleSettings` + `ManagePackages` memakai sibling-set untuk lock-detection (`AnyAsync StartedAt!=null` / `AnyAsync assignment`) — perubahan filter di sana mengubah SCOPE lock group. **Rekomendasi:** terapkan helper di KEEMPAT titik demi konsistensi sibling-set, TAPI verifikasi efek lock-scope di `UpdateShuffleSettings`/`ManagePackages` (lihat §Pitfall 3).

### Pattern 3: Write-on-GET impersonation guard (WSE-05 / D-04)
**What:** Bungkus 3 write-site dengan `if(!_impersonationService.IsImpersonating()) { ... }`, mirror precedent baris 905.
**Precedent (baris 905, VERIFIED):**
```csharp
// Source: Controllers/CMPController.cs:904-910
// Phase 377 (Pitfall 3 / T-377-09): write-on-GET guard — JANGAN tulis DB saat impersonasi.
if (!_impersonationService.IsImpersonating())
{
    assessment.Status = "Open";
    assessment.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```
**3 write-site yang perlu di-guard (VERIFIED current):**
1. **justStarted** — `CMPController.cs:962-967`: `assessment.Status="InProgress"; assessment.StartedAt=DateTime.UtcNow; await SaveChangesAsync();`
2. **SignalR + log** — `:969-978`: `_hubContext.Clients.Group(...).SendAsync("workerStarted", ...)` + `LogActivityAsync(assessment.Id, "started")`
3. **create assignment** — `:1012-1056`: `_context.UserPackageAssignments.Add(assignment); await SaveChangesAsync();` (di dalam `if (assignment == null)`)

### Pattern 4: In-memory assignment preview saat impersonasi (WSE-05 / D-06)
**What:** Saat `IsImpersonating()` & `assignment == null`, bangun object `UserPackageAssignment` di memori (panggil ShuffleEngine), set field JSON string, TANPA `_context.Add`/`SaveChanges`. View-build downstream (1058-1214) konsumsi object ini apa adanya.
**Why aman (VERIFIED):** Setelah create assignment, view-build hanya:
- `assignment.GetShuffledQuestionIds()` (baris 1061, 1081) — deserialize field `ShuffledQuestionIds` (string). In-memory object cukup. [VERIFIED `UserPackageAssignment.cs:60-71`]
- `assignment.ShuffledOptionIdsPerQuestion` (baris 1184) — field string. In-memory cukup.
- `assignment.Id` (baris 1121, `vm.AssignmentId`) — in-memory = 0 (belum persist). **Konsekuensi:** form submit downstream pakai `assignmentId=0`; karena ini PREVIEW read-only, submit oleh admin di luar scope (impersonate read-only). Planner: pastikan tak ada NRE bila `AssignmentId=0`.
- **TIDAK ada DB re-read assignment pasca-create** dalam path package — diverifikasi baris 1058-1214. Satu-satunya DB read kondisional = SavedAnswers block (1150-1175) yang di-gate `isResume` (lihat Pitfall 1, false saat impersonate-belum-mulai).
**Discretion:** RNG preview re-shuffle tiap reload (acak per refresh). Boleh seed stabil `new Random(id)` agar konsisten, atau biarkan acak (preview saja).

### Anti-Patterns to Avoid
- **Mengubah sibling-query satu sisi saja:** ubah StartExam tanpa reshuffle (atau sebaliknya) = bug determinisme baru (workerIndex divergen). Helper D-02 mencegah ini.
- **Pakai LinkedGroupId sebagai pemisah Pre/Post:** salah — Pre & Post share nilai sama (`:1272/:1288`).
- **Persist-lalu-rollback untuk preview:** tetap mutasi DB (transaction log, SignalR side-effect bila ada). Pakai in-memory murni (D-06).
- **Menambah guard di VerifyToken:** D-05 — VerifyToken hanya TempData, BUKAN mutasi DB. Guard di sana = noise tak perlu.
- **Lupa guard salah satu dari 3 write-site:** OPS-01/TOK-03 effectively one change — jika hanya 1-2 yang di-guard, masih ada mutasi (mis. SignalR palsu).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deteksi mode impersonate | Cek session key manual / parse cookie | `_impersonationService.IsImpersonating()` (`:35`) | Single-source Phase 377; sudah handle expired (30 min) via `GetEffectiveUserAsync`. |
| Resolusi user efektif | `_userManager.GetUserAsync(User)` langsung | `GetCurrentUserRoleLevelAsync()` (`CMPController.cs:2424`) | StartExam SUDAH pakai ini (baris 896) — `user` = effective X saat impersonate. Jangan tambah resolver baru. |
| Distribusi soal / shuffle | Loop manual / copy algoritma | `ShuffleEngine.BuildQuestionAssignment/BuildOptionShuffle` | Pure, canonical (Phase 373), di-consume StartExam+reshuffle. SHF-01 fix-nya di Phase 380. |
| Sibling-set determinisme | Inline query di tiap call-site | Shared static helper (D-02) | Inline = drift risk (sudah ada 4 copy). Helper = single source, diuji sekali. |
| Serialize shuffled IDs | Custom format | `JsonSerializer.Serialize(list)` + `GetShuffledQuestionIds()` | Model sudah parse format ini di kedua mode (grading by `PackageOption.Id` — tak terpengaruh). |
| DB assert di e2e | ORM/EF di test | `dbSnapshot.queryScalar(sql)` (`tests/helpers/dbSnapshot.ts`) | sqlcmd localhost-guarded; pola proven Phase 379 Flow K. |

**Key insight:** Hampir seluruh phase ini = **menyusun ulang pemanggilan komponen existing** (guard, helper, engine) — bukan membangun logika baru. Risiko terbesar = drift sibling-set (mitigasi: helper D-02) dan guard tak lengkap (mitigasi: 3 site = 1 unit koheren).

## Runtime State Inventory

> Phase ini sebagian besar code-only, TAPI menyentuh sibling-query yang menentukan data assignment runtime. Inventory tetap relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `UserPackageAssignment` rows (existing). Fix WSE-04 mengubah set paket yang ter-pool **untuk sesi yang BELUM punya assignment** (StartedAt==null). Sesi yang SUDAH ter-assign (in-progress/completed) TIDAK di-recompute oleh StartExam (idempotent resume, baris 1009). | Code edit only. TIDAK ada migrasi data. Sesi same-day Pre/Post yang KEBETULAN sudah tercampur sebelum fix harus di-reshuffle manual oleh HC (di luar scope auto; reshuffle endpoint kini juga AssessmentType-aware → konsisten). |
| Live service config | Tidak ada. Tidak ada n8n/Datadog/external config menyimpan string yang berubah. | None — verified by scope read (code-only fix). |
| OS-registered state | Tidak ada. | None — verified. |
| Secrets/env vars | Tidak ada. `Authentication__UseActiveDirectory` (env, untuk run lokal) tidak berubah. | None — verified. |
| Build artifacts | Tidak ada. `migration: false` → tidak ada EF migration/snapshot baru. Verifikasi `dotnet ef migrations` TIDAK ter-scaffold. | Pastikan plan tak menyentuh model (no migration). |

## Common Pitfalls

### Pitfall 1: `justStarted` vs `isResume` interplay saat impersonate (D-06)
**What goes wrong:** Setelah guard D-04, `assessment.StartedAt` tetap null saat impersonate. Variabel `bool justStarted = assessment.StartedAt == null;` (baris 961) dihitung SEBELUM 3 write-site, jadi `justStarted = true` baik untuk worker asli maupun impersonate. Lalu `bool isResume = !justStarted;` (baris 1126) → `isResume = false` saat impersonate → blok SavedAnswers (1150-1175) TIDAK fire → tak ada DB read tambahan. Ini BENAR (preview = soal kosong, belum ada jawaban). **Yang bisa salah:** jika planner memindah perhitungan `justStarted` ke SETELAH guard atau mengandalkan `StartedAt` untuk gating, logika resume bisa pecah.
**Why it happens:** `justStarted` dihitung dari snapshot `StartedAt` pra-guard; guard tidak mengubah itu (saat impersonate).
**How to avoid:** Jangan ubah posisi/semantik `justStarted` (961) & `isResume` (1126). Guard hanya membungkus blok write, tak mengubah variabel kontrol.
**Warning signs:** Test impersonate menampilkan "resume" state / NRE di SavedAnswers / DB read saat impersonate.

### Pitfall 2: SQL Server tak menjamin order tanpa ORDER BY (determinisme workerIndex)
**What goes wrong:** `siblingSessionIds` dari EF `.ToListAsync()` tanpa ORDER BY bisa beda urutan antar-query → `IndexOf(id)` beda → workerIndex beda → paket OFF≥2 beda.
**Why it happens:** Phase 373 Pitfall 2 — SQL Server tak jamin row order.
**How to avoid:** `OrderBy(x => x)` SETELAH ToList (in-memory sort), IDENTIK di StartExam (992) & reshuffle (5208/5262). Helper D-02 harus mengembalikan list TER-SORT atau caller sort dengan cara sama. [VERIFIED: existing code sudah sort dua sisi]
**Warning signs:** Worker "pindah paket" saat reshuffle vs entry; stale-question reset spuriius.

### Pitfall 3: Perubahan AssessmentType-filter mengubah lock-scope di UpdateShuffleSettings/ManagePackages
**What goes wrong:** `UpdateShuffleSettings` (5367-5370) & `ManagePackages` (5488-5491) memakai sibling-set untuk `anyStarted = AnyAsync(... StartedAt != null)` & `anyAssignment = AnyAsync(...)`. Untuk grup Pre/Post same-day, MENAMBAH `AssessmentType` mempersempit sibling-set → lock-detection jadi PER-AssessmentType (Pre dan Post di-lock terpisah), bukan gabungan. Ini mungkin BENAR (Pre lock saat Pre worker mulai; Post belum) — tapi MENGUBAH perilaku lock existing untuk Pre/Post.
**Why it happens:** sibling-set juga jadi basis lock-group, bukan hanya pool soal.
**How to avoid:** Planner harus EKSPLISIT memutuskan: (a) terapkan helper di SEMUA 4 titik (lock jadi per-AssessmentType — lebih granular, arguably benar), ATAU (b) terapkan AssessmentType-filter hanya di titik yang menentukan POOL+workerIndex (StartExam + 2 reshuffle), biarkan lock-scope di UpdateShuffleSettings/ManagePackages tetap gabungan. CONTEXT D-02 berkata "SEMUA endpoint reshuffle" — UpdateShuffleSettings/ManagePackages BUKAN reshuffle. **Rekomendasi:** Opsi (a) untuk konsistensi penuh, TAPI dokumentasikan perubahan lock-scope sebagai keputusan sadar + tambah verifikasi (test lock Pre vs Post terpisah). Untuk normal exam (all-Standard) tak ada perubahan apa pun.
**Warning signs:** HC bisa ubah shuffle Post setelah Pre worker mulai (sebelumnya ter-lock gabungan), atau sebaliknya.

### Pitfall 4: Phase 380 belum landing → test StartExam empty-package gagal
**What goes wrong:** StartExam consume `ShuffleEngine.BuildQuestionAssignment` (baris 1019). ON-path `K = packages.Min(p => p.Questions.Count)` (`ShuffleEngine.cs:108`) belum memfilter paket kosong (itu fix SHF-01 Phase 380). Jika test 381 dijalankan terhadap engine yang belum di-fix, skenario ≥2 paket satu kosong → 0 soal.
**Why it happens:** Dependency ordering — 381 depends 380.
**How to avoid:** Land Phase 380 (SHF-01) DULU, atau rebase branch 381 di atas 380. Test 381 untuk WSE-04 fokus pada **pool diskriminasi** (jumlah & teks soal Pre saja), bukan empty-package (itu domain 380). [VERIFIED: ShuffleEngine.cs:108-110 belum filter]
**Warning signs:** Test PrePost pool dapat 0 soal padahal paket Pre berisi.

### Pitfall 5: Razor view runtime error tak tertangkap grep+build (lesson Phase 354)
**What goes wrong:** Perubahan view-feed (`vm.AssignmentId=0`, dll.) bisa lolos `dotnet build` tapi NRE saat render (RuntimeBinderException pada dynamic ViewBag, dll.).
**How to avoid:** UAT Playwright runtime untuk path impersonate-preview (render StartExam saat impersonate) — bukan hanya build+grep. [Memory lesson 354/355]
**Warning signs:** 500 di StartExam saat impersonate render, hanya muncul di browser.

## Code Examples

### Sibling helper (pola, signature = discretion) — D-02
```csharp
// Source: pola dari Controllers/AssessmentAdminController.cs:2327-2335 (StandardGroupSiblingPredicate, Phase 367)
// Helper BARU (INCLUDE AssessmentType-match, BEDA dari predicate Phase 367 yang exclude Pre/Post).
// Lokasi/nama = Claude's discretion. Contoh sebagai static method yang return sorted Ids:
public static async Task<List<int>> GetSiblingSessionIdsAsync(
    ApplicationDbContext context, string title, string category, DateTime scheduleDate, string? assessmentType)
{
    var ids = await context.AssessmentSessions
        .Where(s => s.Title == title &&
                    s.Category == category &&
                    s.Schedule.Date == scheduleDate.Date &&
                    s.AssessmentType == assessmentType)   // D-01 diskriminator
        .Select(s => s.Id)
        .ToListAsync();
    return ids.OrderBy(x => x).ToList();   // Pitfall 2 — order identik dua sisi
}
// NB: alternatif pure Expression<Func<...>> (seperti StandardGroupSiblingPredicate) + .Compile() untuk test
//     in-memory tanpa DB — pilih bentuk yang paling mudah dipakai IDENTIK di StartExam + reshuffle.
```

### Guard 3 write-site (D-04) — mirror baris 905
```csharp
// Source: pattern Controllers/CMPController.cs:904-910 (precedent VERIFIED)
// Write-site 1 (962-967):
bool justStarted = assessment.StartedAt == null;
if (justStarted && !_impersonationService.IsImpersonating())   // D-04
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
// NB: hati-hati — `justStarted` tetap dihitung (Pitfall 1). Guard hanya membungkus blok write.
// Write-site 2 (969-978): bungkus blok SignalR+LogActivity dengan if(justStarted && !IsImpersonating()).
// Write-site 3 (1012-1056): di dalam if(assignment==null), pisahkan cabang persist vs in-memory (D-06).
```

### In-memory assignment (D-06)
```csharp
// Source: derive dari Controllers/CMPController.cs:1012-1042 (build path VERIFIED)
if (assignment == null)
{
    var rng = Random.Shared;   // discretion: new Random(id) untuk preview stabil
    var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
        packages, assessment.ShuffleQuestions, workerIndex, rng);
    var assignedQuestions = packages.SelectMany(p => p.Questions).Where(q => shuffledIds.Contains(q.Id));
    var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(assignedQuestions, assessment.ShuffleOptions, rng);
    var sentinelPackage = packages.First();

    assignment = new UserPackageAssignment
    {
        AssessmentSessionId = id,
        AssessmentPackageId = sentinelPackage.Id,
        UserId = user.Id,                                   // effective user (X bila impersonate)
        ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
        ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict),
        SavedQuestionCount = shuffledIds.Count
    };

    if (!_impersonationService.IsImpersonating())           // D-04 / D-06
    {
        _context.UserPackageAssignments.Add(assignment);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { /* existing race-recovery (1047-1056) */ }
    }
    // impersonate → SKIP Add/Save: object in-memory cukup feed view (assignment.GetShuffledQuestionIds()).
}
```

### E2E #4 — PrePost same-day pool-only (entry assertion only)
```typescript
// Source: harness tests/e2e/helpers/examTypes.ts (createPrePostAssessmentViaWizard → {preIds, postIds})
//          + tests/helpers/auth.ts (login) + dbSnapshot.queryScalar
// Phase 381 GATE = assert pool soal Pre == paket Pre saja (jumlah & teks), Post tak tercampur.
// Full pass/grade = acceptance pasca-382 (Deferred).
```

### E2E #7 — impersonate read-only (DB assert)
```typescript
// Source: tests/e2e/impersonation.spec.ts (startUser/resolveUser/stopImpersonation) + dbSnapshot
// 1. login admin → startUser(page, 'rino', target) → goto /CMP/StartExam/{id} (Open, StartedAt null, non-token)
// 2. ASSERT no mutation via dbSnapshot.queryScalar:
//    SELECT CASE WHEN StartedAt IS NULL THEN 1 ELSE 0 END FROM AssessmentSessions WHERE Id={id}  → 1
//    SELECT COUNT(*) FROM AssessmentSessions WHERE Id={id} AND Status='Open'                      → 1
//    SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId={id}                   → 0
//    (SignalR workerStarted absence: tak ada hook DB; assert via no LogActivity 'started' row, atau
//     terima sebagai konsekuensi guard — diverifikasi via DB tiga assert di atas.)
// 3. stopImpersonation → login worker asli (coachee) → goto /CMP/StartExam/{id}
//    ASSERT StartedAt SEKARANG ter-set: SELECT CASE WHEN StartedAt IS NOT NULL THEN 1 ELSE 0 END → 1
//            SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId={id}           → 1
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Sibling filter Title+Category+Date | + AssessmentType (D-01) | Phase 381 | Pre/Post same-day terpisah |
| Guard impersonate hanya Upcoming→Open (905) | + 3 write-site StartExam | Phase 381 (extend Phase 377) | Impersonate = read-only penuh di StartExam |
| ShuffleEngine ON-path K=Min tanpa filter | filter paket kosong (SHF-01) | Phase 380 (dependency) | StartExam tak dapat 0 soal saat paket kosong |
| Duplicated sibling-query (4 copy) | Shared static helper (D-02) | Phase 381 | Single source, no drift |

**Deprecated/outdated:**
- CONTEXT menyebut "reshuffle Post ~5482" — di kode aktual TIDAK ada endpoint reshuffle Post terpisah; baris itu = `ManagePackages` (read lock-state). Klarifikasi di §Pattern 2.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Grup sesi homogen `AssessmentType` (tak ada grup campur null+Standard pasca-ISS-04 `:1451`). Filter `AssessmentType==AssessmentType` aman tanpa null-coalesce. | Pattern 1 | Jika ada legacy session `AssessmentType==null` dalam grup Standard, equality EF tak match NULL → sesi itu ter-eksklusi dari pool-nya sendiri. Mitigasi: cek DB lokal `SELECT DISTINCT AssessmentType FROM AssessmentSessions WHERE AssessmentType IS NULL` sebelum lock. |
| A2 | Menerapkan AssessmentType-filter di `UpdateShuffleSettings`/`ManagePackages` mengubah lock-scope Pre/Post jadi per-type (granular). Diasumsikan ini perilaku yang diinginkan / dapat diterima. | Pitfall 3 | Lock behavior berubah untuk Pre/Post; HC bisa ubah shuffle satu sisi setelah sisi lain mulai. Planner harus putuskan eksplisit. |
| A3 | `vm.AssignmentId = 0` saat preview impersonate tak menyebabkan NRE di StartExam.cshtml (form submit oleh admin di luar scope read-only). | Pattern 4 | Jika view/JS asumsikan AssignmentId>0 → error render. Mitigasi: UAT Playwright runtime impersonate-render (Pitfall 5). |
| A4 | SignalR `workerStarted` absence saat impersonate dapat diverifikasi cukup via DB tiga-assert (StartedAt null, Status Open, no assignment), karena broadcast di-gate `justStarted && !IsImpersonating()`. | Code Examples E2E #7 | Jika test mau assert SignalR langsung, butuh hook/spy tambahan (tak ada di harness). Diterima sebagai konsekuensi guard. |

**Jika tabel ini kosong:** tidak — A1-A4 perlu konfirmasi/keputusan planner.

## Open Questions

1. **Lock-scope di UpdateShuffleSettings/ManagePackages (Pitfall 3 / A2)**
   - Yang kita tahu: D-02 berkata "SEMUA reshuffle". Dua titik ini BUKAN reshuffle tapi konsumen sibling-query yang sama.
   - Yang tak jelas: apakah lock Pre/Post harus gabungan atau per-type.
   - Rekomendasi: terapkan helper di keempat titik (konsisten) + dokumentasikan perubahan lock-scope sebagai keputusan sadar + test lock Pre-vs-Post. Konfirmasi ke user saat discuss/plan.

2. **Stabilitas RNG preview impersonate (discretion)**
   - Yang kita tahu: in-memory assignment re-shuffle tiap reload.
   - Yang tak jelas: apakah preview konsisten dibutuhkan UX.
   - Rekomendasi: seed `new Random(id)` untuk preview stabil (murah, tak ada downside karena preview-only).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet build`/`test`/`run`) | Build + xUnit + run lokal | ✓ (proyek aktif) | net8/9 (repo) | — |
| SQL Server Express `localhost\SQLEXPRESS` (HcPortalDB_Dev) | xUnit integration (jika ada) + e2e DB snapshot | ✓ (DB lokal aktif per memory) | — | InMemory utk pure unit (sibling/determinism) |
| SQLBrowser service | e2e login (NTLM loopback) | ⚠️ harus di-start manual | — | `lpc:` shared-memory conn override (memory: local e2e SQL env fix) |
| sqlcmd CLI | `dbSnapshot.ts` queryScalar/backup/restore | ✓ | — | — |
| Playwright + bundled chromium | e2e #4/#7 | ✓ | — | — |
| `Authentication__UseActiveDirectory=false` (env) | run lokal (login non-AD) | env-flag | — | — |

**Missing dependencies with no fallback:** Tidak ada (semua tooling lokal sudah terbukti dipakai Phase 372-379).

**Missing dependencies with fallback:** SQLBrowser (start manual; `lpc:` override). Combined Playwright run WAJIB `--workers=1` (DB isolation — memory).

## Validation Architecture

> nyquist_validation = true (config.json) → section WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests/`) + Playwright (`tests/e2e/`) |
| Config file | `tests/playwright.config.ts` (baseURL http://localhost:5277, fullyParallel false, globalTeardown RESTORE) |
| Quick run command | `dotnet test HcPortal.Tests` (pure unit cepat) |
| Full suite command | `dotnet build` + `dotnet test` + (e2e) `npx playwright test --workers=1` (dari `tests/`) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WSE-04 | Sibling helper INCLUDE AssessmentType-match: Pre-set ≠ Post-set, Standard no-op | unit (pure, no DB) | `dotnet test HcPortal.Tests --filter Sibling` | ❌ Wave 0 (pola `SiblingFilterTests.cs`) |
| WSE-04 | Determinism: helper hasilkan sibling-set+workerIndex IDENTIK StartExam vs reshuffle | unit (pure) | `dotnet test HcPortal.Tests --filter Determin` | ❌ Wave 0 |
| WSE-04 | PrePost same-day → StartExam Pre pool = paket Pre saja (jumlah & teks); Post tak tercampur | e2e | `npx playwright test exam-taking --workers=1` (atau spec baru) | ⚠️ extend (harness `createPrePostAssessmentViaWizard` ada) |
| WSE-05 | Impersonate buka Open/StartedAt-null/non-token → no mutation (StartedAt null, Status Open, 0 assignment, no workerStarted) | e2e + DB assert | `npx playwright test impersonation --workers=1` | ⚠️ extend (`impersonation.spec.ts` harness ada) |
| WSE-05 | Stop impersonate + worker asli StartExam → StartedAt ter-set + 1 assignment (timer dari nol) | e2e + DB assert | (lanjutan #7) | ⚠️ extend |
| WSE-05 | Guard logic (jika di-extract pure) | unit (opsional) | `dotnet test HcPortal.Tests` | opsional |

### Observable Signals per Success Criterion
- **SC#1 (entry-pool Pre-only):** signal = jumlah question-card di StartExam == jumlah soal paket Pre; teks soal match paket Pre (DOM assert) + (opsional) `SELECT COUNT(*)` shuffled IDs. Sampling: 1 PrePost group same-day, paket Pre berisi N soal, assert N.
- **SC#2 (no-mutation impersonate):** signal = 3 DB scalar (StartedAt IS NULL=1; Status='Open' count=1; UserPackageAssignments count=0) + tak ada LogActivity 'started' row. Sampling: 1 sesi Open non-token, satu pembukaan StartExam saat impersonate.
- **SC#3 (deferred-start):** signal = post-stop + worker login, StartedAt IS NOT NULL=1 + 1 assignment row. Sampling: lanjutan SC#2, satu StartExam worker asli.
- **Determinism invariant (StartExam==reshuffle):** signal = unit assert sibling-set list + `IndexOf(id)` identik untuk input sama. Sampling: matriks {1 paket, ≥2 paket} × {Pre, Post, Standard}.

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test HcPortal.Tests` (unit cepat <30s).
- **Per wave merge:** full `dotnet test` + e2e subset (`exam-taking` + `impersonation`) `--workers=1`.
- **Phase gate:** full xUnit green + e2e #4 (pool) + #7 (impersonate) green sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/SiblingPrePostFilterTests.cs` (atau extend `SiblingFilterTests.cs`) — covers WSE-04 (AssessmentType-match include/exclude)
- [ ] `HcPortal.Tests/SiblingDeterminismTests.cs` — covers WSE-04 determinism (sibling-set+workerIndex parity StartExam↔reshuffle)
- [ ] Extend `tests/e2e/exam-taking.spec.ts` (atau spec baru) — E2E #4 PrePost pool-only
- [ ] Extend `tests/e2e/impersonation.spec.ts` — E2E #7 read-only + deferred-start (pakai `dbSnapshot.queryScalar`)
- [ ] Framework install: tidak perlu (xUnit + Playwright sudah terpasang)

## Security Domain

> `security_enforcement` absent di config.json → treated as ENABLED.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada perubahan auth; login existing. |
| V3 Session Management | yes | Impersonation = session-backed (`ImpersonationService`, expired 30 min). Guard membaca session state — tak menambah surface. |
| V4 Access Control | yes | StartExam owner-check (`assessment.UserId != user.Id && !Admin && !HC → Forbid`, baris 898) UNCHANGED. Effective-user resolver (Phase 377) memastikan owner-check pakai X saat impersonate. Read-only invariant impersonate diperkuat (D-04). |
| V5 Input Validation | yes (minor) | `id` route param → `FindAsync`/owner-check existing. Tak ada input baru. |
| V6 Cryptography | no | Tak menyentuh crypto/token-hash. |

### Known Threat Patterns for ASP.NET Core MVC exam-entry

| Pattern | STRIDE | Standard Mitigation | Status di phase ini |
|---------|--------|---------------------|---------------------|
| Write-on-GET side-effect saat impersonate (membakar timer/state worker) | Tampering | Guard `if(!IsImpersonating())` di SEMUA mutasi GET handler | **FIX inti WSE-05/D-04** (3 write-site) |
| Cross-AssessmentType pool contamination (Pre pool Post soal) | Tampering / integrity | Diskriminator query server-side (`AssessmentType`) | **FIX inti WSE-04/D-01** |
| Determinisme divergen → worker pindah paket | Tampering | Shared sibling helper + sorted order | **D-02** (mitigasi via single-source) |
| IDOR pada `id` (akses ujian orang lain) | Elevation | Owner-check + role-gate (baris 898) | UNCHANGED (existing guard) |
| SignalR broadcast palsu (`workerStarted`) saat admin buka | Spoofing | Guard broadcast di blok `justStarted && !IsImpersonating()` | **FIX (write-site 2)** |

Tak ada hand-rolled crypto/authz baru. Effective-user resolver = single-source Phase 377 (V4).

## Sources

### Primary (HIGH confidence) — direct repo read 2026-06-14
- `Controllers/CMPController.cs:840-1078` — StartExam GET full (VerifyToken D-05, guard 905, write-site 962-967/969-978/1012-1056, sibling 982-987, workerIndex 992-993, engine 1019, view-build 1058-1214, stale-check 1065).
- `Controllers/CMPController.cs:2421-2446` — `GetCurrentUserRoleLevelAsync` (effective user resolver, Phase 377).
- `Controllers/AssessmentAdminController.cs:1230-1309` — Pre/Post create (AssessmentType 1235/1271, LinkedGroupId 1272/1288 — bukti D-01).
- `Controllers/AssessmentAdminController.cs:1430-1455` — normal create `AssessmentType="Standard"` (:1451).
- `Controllers/AssessmentAdminController.cs:2327-2335` — `StandardGroupSiblingPredicate` (pola helper D-02).
- `Controllers/AssessmentAdminController.cs:5156-5408` — ReshufflePackage (5160/5181), ReshuffleAll (5248/5252), UpdateShuffleSettings (5352/5359), ManagePackages sibling (5421/5481).
- `Helpers/ShuffleEngine.cs:1-225` — BuildQuestionAssignment (OFF≥2 `workerIndex % count` :58; ON-path K=Min :108 = SHF-01 belum-fix).
- `Models/UserPackageAssignment.cs` — GetShuffledQuestionIds :60, field string :31/:38.
- `Models/AssessmentSession.cs:156-178` — AssessmentType :161 (nullable, "null=backward compat"), LinkedGroupId :172.
- `Services/ImpersonationService.cs:25-84` — IsImpersonating :35, StartUser/Stop, IsExpired.
- `HcPortal.Tests/SiblingFilterTests.cs`, `ImpersonationIdentityTests.cs`, `ShuffleReshuffleTests.cs` — pola test.
- `tests/e2e/impersonation.spec.ts`, `tests/e2e/helpers/examTypes.ts`, `tests/helpers/dbSnapshot.ts`, `tests/helpers/auth.ts`, `tests/helpers/accounts.ts`, `tests/playwright.config.ts` — harness e2e.
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — audit source (Phase 1 detail c/d, E2E #4/#7, §VERIFIKASI risks).
- `.planning/REQUIREMENTS.md`, `.planning/config.json`, `.planning/STATE.md`.

### Secondary (MEDIUM confidence)
- Project memory (MEMORY.md): local e2e SQL env fix (SQLBrowser + `lpc:` + `--workers=1`), Phase 377 impersonation precedent, Phase 354/355 Razor-runtime lesson.

### Tertiary (LOW confidence)
- (none — semua klaim teknis diverifikasi langsung dari source.)

## Metadata

**Confidence breakdown:**
- Standard stack (komponen existing): HIGH — semua di-read langsung, tak ada dependency baru.
- Architecture (D-01..D-07 mapping ke kode): HIGH — setiap baris diverifikasi; bentuk-kode locked.
- Pitfalls: HIGH — Pitfall 1/2/4 dari kode, Pitfall 3 dari analisis lock-scope (perlu keputusan planner), Pitfall 5 dari memory lesson.
- Test strategy: HIGH — harness existing terpetakan ke #4/#7; gap Wave 0 jelas.
- Open questions A1-A4: MEDIUM — perlu konfirmasi data/keputusan planner.

**Research date:** 2026-06-14
**Valid until:** 2026-07-14 (stable — bug-fix di codebase existing; satu-satunya drift = Phase 380 landing yang mengubah `ShuffleEngine.cs:108`)
