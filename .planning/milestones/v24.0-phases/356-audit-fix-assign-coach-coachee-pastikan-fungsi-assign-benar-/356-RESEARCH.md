# Phase 356: Audit Fix Assign Coach×Coachee - Research

**Researched:** 2026-06-09
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server) — audit-fix bug logic di satu controller, ditambah satu kontrak UI Vanilla-JS + xUnit. Bahasa: Bahasa Indonesia (per CLAUDE.md).
**Confidence:** HIGH (semua temuan code-verified file:line di codebase sesi ini; spec + audit sudah data-verified track id=4)

## Summary

Phase 356 adalah **audit-fix backend** atas 7 temuan (AF-1..7) di `Controllers/CoachMappingController.cs` (1694 baris) — bukan fitur baru, bukan re-audit. Spec (`2026-06-06-coach-coachee-assign-audit-fix.md`) dan CONTEXT.md sudah mengunci semua keputusan (D-01..D-15) dan sudah data-verified terhadap DB lokal (track id=4: 4 deliverable / 2 unit). Tugas riset ini bukan memvalidasi temuan, melainkan memetakan **pola implementasi existing** yang harus diikuti agar setiap fix konsisten dengan kualitas controller yang sudah TINGGI (transaksi konsisten, audit log, notif, error-handling spesifik).

Temuan paling penting: codebase sudah punya **semua pola yang dibutuhkan** sebagai contoh persis. Transaksi (`BeginTransactionAsync` di Assign L560 + Deactivate L924), cascade deactivate ProtonTrackAssignment (`CoachCoacheeMappingDeactivate` L930-940), notif warn-only (`_notificationService.SendAsync` L645/957), deteksi duplicate-key (`CertNumberHelper.IsDuplicateKeyException` L37-42 — match nama index + error number 2601/2627), dan unit-resolution per-coachee (`AutoCreateProgressForAssignment` L1342-1367). Test infrastruktur menyediakan **dua strategi** yang sudah dipakai: (1) helper static murni di-test langsung (`BuildActualCategoriesTests`), dan (2) InMemory DbContext per-test dengan null-substitusi dependency (`OrganizationControllerTests`).

**Primary recommendation:** Ekstrak logic eligibility AF-1 menjadi **method static pure** `EligibilityCalculator.IsEligiblePerUnit(...)` yang menerima list (bukan DbContext) — paling testable, mengikuti pola `CMPController.BuildActualCategories` + `CertNumberHelper`. Untuk AF-3/AF-5/AF-6 ikuti verbatim pola Deactivate/Assign/CertNumberHelper yang sudah ada. AF-2 = disable-checkbox di hook `updateAssignmentDefaults()` yang sudah punya `units` Set. Migration = FALSE (semua kolom verified ada di `Models/CoachCoacheeMapping.cs`).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| AF-1 eligibility per-unit | API/Backend (`GetEligibleCoachees` controller action) | — | Business logic + data query; helper static murni ekstrak agar testable tanpa DbContext |
| AF-3 graduate + cascade + transaksi | API/Backend (`MarkMappingCompleted`) | Database (unique-index release) | State mutation lintas-entitas wajib atomik di server |
| AF-2 guard 1-unit/batch | Browser/Client (Vanilla JS di `.cshtml`) | API (backstop opsional submit-time) | UI guard murni — backend `AutoCreateProgressForAssignment` TIDAK diubah (D-07) |
| AF-5 notifikasi reassign | API/Backend (`ApproveReassignSuggestion`) | — | Side-effect server-side via `_notificationService` |
| AF-6 pesan error duplikat | API/Backend (`CoachCoacheeMappingAssign` catch) | Database (unique-index = sumber exception) | Server menangkap `DbUpdateException` dari DB constraint |
| AF-7 batch query progression-warning | API/Backend (`CoachCoacheeMappingAssign` L497-546) | Database | Perf refactor query, zero behavior change |

## User Constraints (from CONTEXT.md)

### Locked Decisions

**AF-1 (HIGH) — Eligibility per-unit [headline]**
- **D-01:** `GetEligibleCoachees` (L1277-1334) hitung **expected deliverable per-unit coachee**, bukan total deliverable semua-unit track. Resolve unit tiap coachee (mirror `AutoCreateProgressForAssignment` L1342-1355: `AssignmentUnit` mapping aktif → fallback `User.Unit`), `expectedCount = deliverable track WHERE Unit==coacheeUnit`. Eligible bila `mine.Count == expectedCount && expectedCount > 0 && mine.All(Approved)`.
- **D-02:** Tahun 3 (track tanpa deliverable, L1298-1307) tetap **semua assigned-coachee eligible** by-design — dipertahankan verbatim.

**AF-3 (MED) — Graduated per-unit [LOCKED opsi ii, user 2026-06-06]**
- **D-03:** `MarkMappingCompleted` (L1075-1109) set `IsCompleted=true` **DAN `IsActive=false`** (+ `CompletedAt`, + `EndDate` = waktu completion). Membebaskan unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`.
- **D-04:** Cascade — deactivate `ProtonTrackAssignment` aktif coachee saat graduate (ikuti pola `CoachCoacheeMappingDeactivate` L931-939, stamp `DeactivatedAt`). **Pertahankan histori progress unit lama (jangan hapus).**
- **D-05:** Bungkus dalam **transaksi** (saat ini single `SaveChanges`).
- **D-06:** Mapping graduated muncul hanya saat `showAll=true` dengan badge "Graduated"; list default (`IsActive` only) tak menampilkannya.

**AF-2 (MED) — Batch lintas-unit = UI guard (Opsi A)**
- **D-07:** Fix via **UI guard**: batasi pemilihan coachee dalam 1 batch ke **satu unit** (disable/cegah cross-unit select di modal `CoachCoacheeMapping.cshtml` L408-449 `coacheeChecklist`). Pertahankan semantik `AssignmentUnit` eksplisit. **Backend `AutoCreateProgressForAssignment` tidak diubah** (Opsi B ditolak).

**AF-5 (LOW) — Notifikasi reassign**
- **D-08:** `ApproveReassignSuggestion` (L1614-1638) tambah `_notificationService` ke coach lama (dilepas), coach baru (ditunjuk), coachee — selaras pola COACH-02.

**AF-6 (LOW) — Pesan error duplikat spesifik**
- **D-09:** `CoachCoacheeMappingAssign` (L474-490) tangkap `DbUpdateException` yang melanggar `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` → pesan ramah spesifik.

**AF-7 (INFO) — N+1 progression-warning [USER PILIH MASUK SCOPE]**
- **D-10:** Refactor loop progression-warning (`CoachCoacheeMappingAssign` L497-546, ~4 query/coachee) jadi **batch query**. Jaga agar tak ubah perilaku warning, hanya kurangi query count.

**Migration**
- **D-12:** **Migration = FALSE.** Verified: kolom `IsActive`/`EndDate`/`CompletedAt`/`IsCompleted`/`AssignmentUnit` SEMUA sudah ada di `Models/CoachCoacheeMapping.cs`. Tidak ada schema change. (Verified ulang sesi ini: lihat Runtime State Inventory.)

**Verifikasi & Seed**
- **D-13:** **xUnit** logic-bearing eligibility per-unit (AF-1). Ekstrak logic ke helper testable.
- **D-14:** **Playwright UAT** localhost:5277: track id=4 → assign coachee → approve deliverable → CreateAssessment → coachee muncul. Plus smoke AF-2 + AF-5.
- **D-15:** **Seed fixture** track id=4 via SEED_WORKFLOW (snapshot→journal→restore, temporary+local-only). `dotnet build` 0 error + `dotnet test` hijau + tanpa regresi.

### Claude's Discretion
- Bentuk persis ekstraksi helper eligibility per-unit (signature, lokasi) — asal testable & mirror filter `AutoCreateProgressForAssignment`.
- Wording persis pesan notif AF-5 & pesan error spesifik AF-6.
- Mekanisme UI guard AF-2 (disable checkbox vs validasi submit vs filter dropdown) — asal hasil = 1 unit/batch.

### Deferred Ideas (OUT OF SCOPE)
- **AF-4 (Reactivate ±5s window refactor)** — DEFER ke backlog. Phase 356 cukup **dokumentasikan asumsi window di komentar kode** (`CoachCoacheeMappingReactivate` L1008-1020). JANGAN fix, JANGAN tambah kolom korelasi. Alasan: severity LOW-MED + hindari migration ke v24 leg yang 0-migration. (Promote via `/gsd-add-backlog` bila volume reactivate naik.)
- **Out-of-scope (JANGAN sentuh):** arsitektur transaksi/audit/file-delete atomic existing (sudah benar), unique-index invariant 1-coach-aktif/coachee, fitur image v24.0 (352-355).

## Phase Requirements

> Phase ini memakai **audit-finding IDs (AF-1..AF-7)**, bukan REQ-ID formal dari REQUIREMENTS.md. AF mapping ke decision IDs di atas.

| ID | Description | Research Support |
|----|-------------|------------------|
| AF-1 | Eligibility per-unit coachee (bukan total track) | `AutoCreateProgressForAssignment` L1342-1367 = sumber kebenaran filter unit; ekstrak helper static (pola `BuildActualCategoriesTests`); GetEligibleCoachees L1277-1334 = call-site |
| AF-2 | UI guard 1-unit/batch di modal | `updateAssignmentDefaults()` L682-711 sudah punya `units` Set + cabang multi-unit; `filterCoacheesBySection()` L670-680 (display toggle); `submitAssign()` L713 (backstop) |
| AF-3 | MarkCompleted set IsActive=false + cascade + transaksi | `CoachCoacheeMappingDeactivate` L924-944 = pola transaksi + cascade + DeactivatedAt verbatim |
| AF-5 | Notifikasi reassign 3 recipient | `_notificationService.SendAsync` L645/957-964 (warn-only try/catch); UI-SPEC mengunci EVENT_TYPE `COACH_REASSIGNED` + microcopy |
| AF-6 | Pesan error duplikat spesifik | `CertNumberHelper.IsDuplicateKeyException` L37-42 (match index name + 2601/2627); unique-index di `ApplicationDbContext.cs` L325-328 |
| AF-7 | Batch query progression-warning (zero behavior change) | Loop L508-535 = 4 query/coachee → pre-load batch dict |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET / ASP.NET Core MVC | net8.0 | Web framework, controller, Razor | [VERIFIED: `HcPortal.csproj` L4 TargetFramework=net8.0] — stack existing app |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + transaksi + LINQ query | [VERIFIED: `HcPortal.csproj` L18] |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | UserManager (resolve coach/coachee names + roles) | [VERIFIED: `HcPortal.csproj` L16] |
| Bootstrap | 5.3 | UI modal + `.form-check-input:disabled` native (AF-2) | [CITED: 356-UI-SPEC.md] app-wide via `_Layout.cshtml` |

### Supporting (test project)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xunit | 2.9.3 | Test framework | [VERIFIED: `HcPortal.Tests.csproj` L15] AF-1 eligibility unit test |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | InMemory DbContext untuk test controller-action yang touch DbContext | [VERIFIED: `HcPortal.Tests.csproj` L12] — dipakai `OrganizationControllerTests` |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQL integration test (disposable DB) | [VERIFIED: `HcPortal.Tests.csproj` L13] — dipakai `OrgLabelMigrationFixture` (TIDAK perlu untuk Phase 356, no migration) |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | [VERIFIED: `HcPortal.Tests.csproj` L14] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Helper static pure (input = list) untuk AF-1 | InMemory DbContext + memanggil `GetEligibleCoachees` langsung | Helper static lebih cepat, deterministik, no DB setup. InMemory perlu seed graph ProtonKompetensi→Sub→Deliverable+Progress (lebih verbose). **Pilih static** — D-13 minta "ekstrak logic ke helper testable" + ada precedent `BuildActualCategories` |
| Disable-checkbox AF-2 | Validasi submit-time only di `submitAssign()` | Disable lebih jelas visual (UI-SPEC kunci disable sebagai default). Submit-only = backstop opsional. **Pilih disable + backstop** |

**Installation:** Tidak ada paket baru. Semua dependency sudah terpasang. Migration = FALSE.

**Version verification:** Versi di atas dibaca langsung dari `.csproj` di codebase sesi ini (bukan training data) — semuanya pinned 8.0.0 / net8.0. Tidak ada paket yang perlu di-`npm view`/registry-check karena phase ini tidak menambah dependency.

## Architecture Patterns

### System Architecture Diagram

```
[HC/Admin Browser]
   │
   │ (1) Buka modal Assign di /Admin/CoachCoacheeMapping
   ▼
[CoachCoacheeMapping.cshtml]  ──── ViewBag.EligibleCoachees (ApplicationUser: Unit/Section/NIP/FullName)
   │                                  L148-150: coachee role aktif TANPA mapping aktif
   │ checkbox onchange
   ▼
[updateAssignmentDefaults() JS]  ◄── AF-2 GUARD di sini (units Set sudah ada L686-693)
   │  - first check → tentukan data-unit
   │  - disable .coachee-checkbox unit berbeda
   │  - clear → re-enable
   │ submitAssign() POST JSON
   ▼
[CoachCoacheeMappingAssign]  ◄── AF-6 catch DbUpdateException (unique-index)
   │                            AF-7 batch query progression-warning (L497-546)
   │ BeginTransactionAsync (L560)
   ├──► AddRange CoachCoacheeMapping
   ├──► ProtonTrackAssignment (deactivate beda-track / reuse inactive / buat baru)
   │       └──► AutoCreateProgressForAssignment(unit coachee) ──┐
   │              filter Unit==resolvedUnit (L1363-1365)          │ SUMBER KEBENARAN
   │ CommitAsync                                                  │ "expected per unit"
   ▼                                                              │
[DB: HcPortalDB_Dev / SQL Server Express]                        │
   │  ProtonDeliverableProgress (Pending → Approved oleh coach)   │
   │                                                              │
   ▼                                                              │
[CreateAssessment form, kategori Assessment Proton]              │
   │ GET /Admin/GetEligibleCoachees?protonTrackId=N              │
   ▼                                                              │
[GetEligibleCoachees]  ◄── AF-1 FIX: expectedCount per-unit ◄────┘ (mirror filter ini)
   │  - resolve unit per coachee (AssignmentUnit aktif → User.Unit)
   │  - eligible bila mine.Count == expectedCount && expectedCount>0 && all Approved
   │  - Tahun 3 (no deliverable) → semua eligible (D-02 verbatim)
   ▼
[Dropdown coachee di CreateAssessment]

[MarkMappingCompleted (graduate)]  ◄── AF-3: set IsActive=false+IsCompleted=true+CompletedAt+EndDate
   │ BeginTransactionAsync (BARU, D-05)                            +cascade deactivate ProtonTrackAssignment (D-04)
   └──► coachee kembali ke pool EligibleCoachees (unique-index bebas)

[ApproveReassignSuggestion]  ◄── AF-5: notif 3 recipient (COACH_REASSIGNED)
```

### Component Responsibilities

| Komponen | File:Line | Tanggung jawab di Phase 356 |
|----------|-----------|------------------------------|
| `GetEligibleCoachees` | `CoachMappingController.cs` L1277-1334 | AF-1 — call-site eligibility; ganti `trackDeliverableIds.Count` global jadi expectedCount per-unit |
| `AutoCreateProgressForAssignment` | L1338-1407 | AF-1 — sumber kebenaran filter unit (L1363-1365). **TIDAK diubah** (read-only reference) |
| `MarkMappingCompleted` | L1075-1109 | AF-3 — tambah IsActive=false+EndDate, cascade, transaksi |
| `CoachCoacheeMappingDeactivate` | L924-944 | Pola cascade+transaksi+DeactivatedAt untuk AF-3 (read-only reference) |
| `CoachCoacheeMappingAssign` | L474-654 | AF-6 (catch L620-628) + AF-7 (loop L497-546) |
| `ApproveReassignSuggestion` | L1614-1638 | AF-5 — tambah 3 SendAsync warn-only |
| `CoachCoacheeMappingReactivate` | L1008-1020 | AF-4 DEFER — tambah komentar asumsi window saja |
| `CoachCoacheeMapping` (list action) | L100-153 | AF-3 D-06 — badge "Graduated" saat showAll (IsCompleted sudah ada di proyeksi L114) |
| Modal `coacheeChecklist` + JS | `Views/Admin/CoachCoacheeMapping.cshtml` L407-433, L670-764 | AF-2 — guard 1-unit/batch |
| Helper baru (AF-1) | `Helpers/` (BARU) | Static pure eligibility calculator (testable) |
| `EligibilityCalculator` test | `HcPortal.Tests/` (BARU) | AF-1 xUnit |
| Unique index | `Data/ApplicationDbContext.cs` L325-328 | AF-6 sumber exception (read-only reference) |

### Pattern 1: Ekstrak logic ke helper static pure (AF-1)
**What:** Pindahkan keputusan eligibility ke method static yang menerima data primitif (list of progress + expectedCount), bukan DbContext. Controller tetap melakukan query, lalu memanggil helper untuk keputusan.
**When to use:** Saat D-13 minta "ekstrak logic ke helper testable" dan logic punya cabang (count match + all-approved + edge expectedCount==0).
**Example:**
```csharp
// Source: pola CMPController.BuildActualCategories (di-test BuildActualCategoriesTests.cs)
//         + CertNumberHelper static helper. PROPOSAL signature (Claude's discretion D-13).
namespace HcPortal.Helpers
{
    public static class CoacheeEligibilityCalculator
    {
        /// <summary>
        /// AF-1: coachee eligible bila punya tepat expectedCount progress untuk track
        /// dan semuanya Approved. expectedCount == 0 → tidak eligible (kecuali track Tahun 3
        /// tanpa deliverable, yang ditangani terpisah di call-site per D-02).
        /// </summary>
        public static bool IsEligiblePerUnit(IReadOnlyList<string> myProgressStatuses, int expectedCount)
        {
            if (expectedCount <= 0) return false;
            if (myProgressStatuses.Count != expectedCount) return false;
            return myProgressStatuses.All(s => s == "Approved");
        }
    }
}
```
**Catatan:** signature persis = discretion. Bila ingin per-coachee expectedCount yang berbeda (multi-unit dalam satu track), pertimbangkan helper menerima `Dictionary<coacheeId, (statuses, expectedCount)>` ATAU panggil `IsEligiblePerUnit` per coachee di loop. Yang penting: keputusan boolean dapat di-test tanpa DbContext.

### Pattern 2: Transaksi + cascade deactivate (AF-3) — mirror Deactivate verbatim
**What:** Bungkus mutasi multi-entitas dalam `BeginTransactionAsync`, cascade deactivate ProtonTrackAssignment dengan stamp `DeactivatedAt`, commit, baru audit log.
**When to use:** AF-3 D-04/D-05.
**Example:**
```csharp
// Source: CoachCoacheeMappingDeactivate L924-944 (verbatim pattern) — IKUTI INI
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    mapping.IsCompleted = true;
    mapping.CompletedAt = DateTime.UtcNow;
    mapping.IsActive = false;                 // AF-3 D-03: bebaskan unique-index
    mapping.EndDate = DateTime.UtcNow;        // AF-3 D-03

    var deactivationTime = mapping.EndDate.Value;
    var activeAssignments = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
        .ToListAsync();
    foreach (var a in activeAssignments)
    {
        a.IsActive = false;
        a.DeactivatedAt = deactivationTime;   // D-04: pola FIX-01, JANGAN hapus progress
    }

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    // audit log SETELAH commit (pola existing L946)
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "MarkMappingCompleted transaction failed for mapping {Id}", mappingId);
    TempData["Error"] = "Operasi gagal. Semua perubahan dibatalkan.";
    return RedirectToAction("CoachCoacheeMapping");
}
```
**Penting:** `MarkMappingCompleted` saat ini me-return `RedirectToAction` + `TempData` (BUKAN Json seperti Deactivate). Pertahankan kontrak return existing (Redirect+TempData), hanya bungkus transaksi di dalamnya. Validasi Tahun 3 (L1081-1098) tetap di luar/di awal transaksi.

### Pattern 3: Notif warn-only (AF-5)
**What:** `SendAsync(userId, EVENT_TYPE, title, body, "/CDP/CoachingProton")` dibungkus `try/catch` warn-only (tidak throw).
**Example:**
```csharp
// Source: CoachCoacheeMappingDeactivate L949-966 (COACH-03 pattern)
try
{
    var oldCoach = await _context.Users.FindAsync(oldCoachId);
    var newCoach = await _context.Users.FindAsync(newCoachId);
    var coachee  = await _context.Users.FindAsync(mapping.CoacheeId);
    var coacheeName = coachee?.FullName ?? coachee?.UserName ?? mapping.CoacheeId;
    var newCoachName = newCoach?.FullName ?? newCoach?.UserName ?? newCoachId;

    await _notificationService.SendAsync(oldCoachId, "COACH_REASSIGNED",
        "Penugasan Coaching Dialihkan",
        $"Penugasan coaching Anda dengan {coacheeName} telah dialihkan ke coach lain.",
        "/CDP/CoachingProton");
    await _notificationService.SendAsync(newCoachId, "COACH_REASSIGNED",
        "Coach Ditunjuk",
        $"Anda ditunjuk sebagai coach untuk {coacheeName}.",
        "/CDP/CoachingProton");
    await _notificationService.SendAsync(mapping.CoacheeId, "COACH_REASSIGNED",
        "Coach Anda Berubah",
        $"Coach Anda telah diganti menjadi {newCoachName}.",
        "/CDP/CoachingProton");
}
catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
```
EVENT_TYPE + microcopy persis sudah dikunci di 356-UI-SPEC.md (recipient table). `SendAsync` signature `(string userId, string type, string title, string message, string? actionUrl = null)` [VERIFIED: `Services/INotificationService.cs` L20].

### Pattern 4: Deteksi duplicate-key untuk pesan spesifik (AF-6)
**What:** Tangkap `DbUpdateException` di catch existing, cek apakah pelanggaran unique-index, kembalikan pesan ramah spesifik.
**Example:**
```csharp
// Source: CertNumberHelper.IsDuplicateKeyException L37-42 (POLA EXISTING — pertimbangkan reuse/extend)
// AF-6: tambah catch SEBELUM catch (Exception) generic di Assign L623-628
catch (DbUpdateException dbEx) when (
    dbEx.InnerException?.Message.Contains("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique") == true
    || dbEx.InnerException?.Message.Contains("2601") == true
    || dbEx.InnerException?.Message.Contains("2627") == true)
{
    _logger.LogWarning(dbEx, "Assign race: coachee already has active coach (unique-index violation)");
    await tx.RollbackAsync();
    return Json(new { success = false,
        message = "Coachee sudah memiliki coach aktif untuk unit ini. Nonaktifkan mapping lama terlebih dahulu." });
}
```
**Catatan:** `CertNumberHelper.IsDuplicateKeyException` match index `IX_AssessmentSessions_NomorSertifikat`. AF-6 butuh match index berbeda — boleh extend `CertNumberHelper` jadi generic (terima nama index param) ATAU inline `when`-filter seperti di atas (discretion). Error number SQL Server: **2601** (unique index) / **2627** (unique constraint).

### Pattern 5: AF-2 UI guard di hook existing
**What:** Manfaatkan `updateAssignmentDefaults()` yang sudah membangun `units` Set; tambah disable checkbox unit-berbeda saat pilihan pertama, re-enable saat clear.
**Example:**
```javascript
// Source: updateAssignmentDefaults() L682-711 (extend) + filterCoacheesBySection() L670-680 (jangan break)
function updateAssignmentDefaults() {
    var checked = document.querySelectorAll('.coachee-checkbox:checked');
    var hint = document.getElementById('coacheeUnitConstraintHint'); // BARU (div form-text)

    if (checked.length === 0) {
        // reset: re-enable semua, hilangkan text-muted, sembunyikan hint
        document.querySelectorAll('#coacheeChecklist .coachee-item').forEach(function (item) {
            item.querySelector('.coachee-checkbox').disabled = false;
            item.classList.remove('text-muted');
        });
        if (hint) hint.style.display = 'none';
        return;
    }

    var lockedUnit = checked[0].closest('.coachee-item').getAttribute('data-unit') || '';
    document.querySelectorAll('#coacheeChecklist .coachee-item').forEach(function (item) {
        var cb = item.querySelector('.coachee-checkbox');
        var u = item.getAttribute('data-unit') || '';
        var sameUnit = (u === lockedUnit);
        cb.disabled = !sameUnit && !cb.checked;     // jangan disable yang sudah dicentang
        item.classList.toggle('text-muted', !sameUnit && !cb.checked);
    });
    if (hint) hint.style.display = '';

    // === auto-fill existing (L686-709) tetap jalan ===
    var units = new Set(); var sections = new Set();
    checked.forEach(function (cb) { /* ...existing... */ });
    // ...existing auto-fill AssignmentSection/AssignmentUnit...
}
```
Hint copy (Bahasa Indonesia, dikunci UI-SPEC): `Satu batch assign hanya untuk satu unit. Coachee dari unit lain dinonaktifkan — kosongkan pilihan untuk berganti unit.` Gunakan `<div class="form-text text-muted">`. **Jangan sentuh `display`** (itu milik `filterCoacheesBySection()`); guard hanya toggle `disabled` + class `text-muted`. Backstop opsional di `submitAssign()`: cek `new Set(units).size === 1` sebelum `fetch`.

### Anti-Patterns to Avoid
- **Mengubah `AutoCreateProgressForAssignment` untuk AF-2:** ditolak eksplisit (D-07, Opsi B). Bikin makna `AssignmentUnit` batch kabur. Guard murni UI.
- **Menghapus histori progress unit lama saat graduate (AF-3):** D-04 melarang. Cascade hanya `IsActive=false`+`DeactivatedAt`, BUKAN `RemoveRange`.
- **Menambah kolom korelasi / migration untuk AF-4:** out-of-scope, defer. Cukup komentar asumsi.
- **Mengubah perilaku warning AF-7:** D-10 minta zero behavior change — hanya kurangi query count (loop AnyAsync/CountAsync → pre-loaded dict). Hasil `incompleteCoachees` harus identik.
- **Mengubah unique-index invariant:** out-of-scope. AF-3 sengaja MEMANFAATKAN (set IsActive=false agar index bebas), bukan mengubah index.
- **Menulis pesan/komentar dalam Bahasa Inggris untuk user-facing string:** CLAUDE.md = Bahasa Indonesia. Pesan error/notif/hint semua Bahasa Indonesia (microcopy sudah dikunci UI-SPEC).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deteksi unique-index violation | Parse string SQL manual ad-hoc | Pola `CertNumberHelper.IsDuplicateKeyException` (match index name + 2601/2627) | Sudah ada precedent teruji; konsisten |
| Transaksi + cascade deactivate | Manual rollback / flag | `CoachCoacheeMappingDeactivate` pattern (`BeginTransactionAsync`+`DeactivatedAt`) | Identik kebutuhan AF-3; copy gaya |
| Kirim notifikasi | Insert UserNotification manual | `_notificationService.SendAsync` warn-only | Sudah dipakai 4 tempat di controller ini |
| Resolusi unit per-coachee | Query baru sendiri | Mirror `AutoCreateProgressForAssignment` L1342-1355 (AssignmentUnit→User.Unit) | Harus SAMA PERSIS dengan sumber kebenaran agar count cocok |
| Test DbContext-touching action | Mock DbContext custom | `Microsoft.EntityFrameworkCore.InMemory` per-test (Guid) | Pola `OrganizationControllerTests` |
| Test logic murni | Setup DB | Ekstrak static helper + test list langsung | Pola `BuildActualCategoriesTests` / `PackageImageSyncTests` |

**Key insight:** Controller ini berkualitas TINGGI dan sudah punya contoh persis untuk SETIAP fix. Riset bukan menemukan library baru — riset = memetakan pola internal yang harus di-mirror agar fix konsisten. Planner harus menulis task sebagai "ikuti pola X di file:line Y", bukan "buat solusi baru".

## Runtime State Inventory

> Phase ini menyentuh logic + satu set nilai kolom existing. Bukan rename/migration, tapi AF-3 mengubah cara state ditulis — inventory tetap relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | DB lokal `HcPortalDB_Dev` (SQL Server Express): track id=4 (Alkylation 065=3 deliverable, RFCC NHT 053=1). Mapping graduated existing (jika ada) saat ini `IsActive=true, IsCompleted=true` — **inkonsisten dengan model baru AF-3**. | **Data migration manual opsional:** mapping yang sudah `IsCompleted=true` tapi masih `IsActive=true` perlu di-set `IsActive=false`+`EndDate` agar konsisten. Verifikasi via query saat planning; bila ada, tambah task one-off SQL UPDATE (local-only) ATAU dokumentasikan bahwa hanya berlaku ke graduate baru. CONTEXT tidak eksplisit — **flag ke planner sebagai keputusan.** |
| Live service config | None — tidak ada service eksternal (n8n/Datadog/dll). Aplikasi monolith ASP.NET Core. Verified: tidak ada referensi di controller. | None |
| OS-registered state | None — tidak ada Task Scheduler / service yang embed string fix ini. Verified: phase ini code+JS only. | None |
| Secrets/env vars | None baru. Connection string `HcPortalDB_Dev` (Integrated Security) sudah ada di `appsettings.Development.json`. AD auth note: jalankan lokal dengan `Authentication__UseActiveDirectory=false dotnet run` (per MEMORY — appsettings handoff AD=true). | None (gunakan flag AD=false saat UAT lokal) |
| Build artifacts | None — no migration berarti tidak ada perubahan `__EFMigrationsHistory`. `dotnet build` cukup. Test project rebuild otomatis. | None |

**EXISTING-DATA RISK (AF-3):** Set `IsActive=false` saat graduate mengubah invariant unique-index untuk record graduated. Mapping graduated lama (kalau ada di DB) yang masih `IsActive=true` perlu dicek. **Action:** saat planning, jalankan `SELECT Id, CoacheeId, IsActive, IsCompleted FROM CoachCoacheeMappings WHERE IsCompleted = 1` di DB lokal untuk menentukan apakah perlu data-fix one-off. Ini bukan schema migration (D-12 tetap FALSE), hanya nilai data.

## Common Pitfalls

### Pitfall 1: expectedCount per-unit tidak match karena resolusi unit berbeda
**What goes wrong:** AF-1 helper menghitung expectedCount dengan filter unit yang BERBEDA dari `AutoCreateProgressForAssignment`, sehingga `mine.Count != expectedCount` tetap (bug bergeser, bukan hilang).
**Why it happens:** `AutoCreateProgressForAssignment` resolve unit dari `AssignmentUnit` mapping AKTIF → fallback `User.Unit`, lalu filter `ProtonKompetensi.Unit.Trim() == resolvedUnit.Trim()` (L1363-1365, perhatikan `.Trim()`). Jika AF-1 lupa `.Trim()` atau pakai `User.Unit` saja, count meleset.
**How to avoid:** Mirror PERSIS: resolusi unit identik + `.Trim()` di kedua sisi. Helper expectedCount harus query `ProtonDeliverable WHERE ProtonKompetensi.Unit.Trim() == resolvedUnit.Trim() AND ProtonTrackId == track`.
**Warning signs:** Test track id=4 (Alkylation 3/3) tetap tidak eligible setelah fix.

### Pitfall 2: InMemory EF case-sensitivity vs SQL Server
**What goes wrong:** Test pakai InMemory yang case-SENSITIVE; di SQL Server (default collation) string compare case-INSENSITIVE. Test hijau lokal tapi behavior beda live.
**Why it happens:** InMemory provider tidak mensimulasi collation SQL Server. [CITED: comment `OrganizationControllerTests.cs` L4 "Pitfall 5: casing IDENTIK"].
**How to avoid:** Untuk AF-1, lebih baik test helper STATIC pure (input list status string) yang tidak bergantung provider. Bila test InMemory, gunakan casing unit yang IDENTIK dengan seed (jangan andalkan case-insensitivity).
**Warning signs:** Test status string compare ("Approved" vs "approved") berbeda hasil InMemory vs live.

### Pitfall 3: AF-7 mengubah perilaku warning saat refactor batch
**What goes wrong:** Refactor N+1 jadi batch query mengubah set `incompleteCoachees` (mis. lupa cabang `prevAssignment == null` → add, atau lupa skip `hasExistingAssignment`).
**Why it happens:** Loop L508-535 punya 3 kondisi terpisah (hasExistingAssignment skip, prevAssignment null → incomplete, allApproved false → incomplete). Batch refactor harus mereproduksi ketiganya.
**How to avoid:** Pre-load 3 dict (existing-assignment-for-track, prev-assignment-per-coachee, progress-status-per-prev-assignment) lalu evaluasi cabang yang SAMA di loop in-memory. Bandingkan output `incompleteCoachees` sebelum/sesudah dengan data sama.
**Warning signs:** Warning "X coachee belum menyelesaikan..." muncul/hilang berbeda dari sebelum refactor.

### Pitfall 4: MarkMappingCompleted return kontrak (Redirect, bukan Json)
**What goes wrong:** Menyalin pola Deactivate (yang return `Json`) ke MarkMappingCompleted (yang return `RedirectToAction`+`TempData`) → memecah UI flow existing.
**Why it happens:** Deactivate dipanggil via AJAX fetch; MarkMappingCompleted via form POST biasa (Redirect+TempData). Pola transaksi sama, tapi kontrak return berbeda.
**How to avoid:** Bungkus transaksi DI DALAM, pertahankan `TempData["Success"/"Error"]` + `RedirectToAction("CoachCoacheeMapping")` di success/catch.
**Warning signs:** Halaman tidak reload / TempData hilang setelah graduate.

### Pitfall 5: Seed temporary bocor / DB lokal kotor
**What goes wrong:** Fixture track id=4 untuk test/UAT nempel di DB lokal lewat session.
**Why it happens:** Lupa restore setelah test (SEED_WORKFLOW Step 5).
**How to avoid:** WAJIB snapshot (`BACKUP DATABASE`) sebelum seed, catat `SEED_JOURNAL.md` status `active`, restore (`RESTORE ... WITH REPLACE`) setelah test (sukses ATAU gagal), tandai `cleaned`. Klasifikasi: `temporary + local-only`. **Catatan:** track id=4 mungkin SUDAH ada di DB lokal (audit data-verified) — verifikasi dulu apakah perlu seed tambahan atau cukup pakai data existing + reset state coachee.

## Validation Architecture

> nyquist_validation = true (config.json L15). Section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (net8.0) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate runsettings) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CoacheeEligibility"` |
| Full suite command | `dotnet test` (saat ini ~131 test hijau per MEMORY Phase 355) |
| Integration filter | `dotnet test --filter "Category!=Integration"` (skip real-SQL bila tak ada SQLEXPRESS) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AF-1 | Coachee unit A: 3/3 Approved (expectedCount=3) → eligible | unit (static helper) | `dotnet test --filter "FullyQualifiedName~IsEligiblePerUnit"` | ❌ Wave 0 |
| AF-1 | Coachee unit B: 0/1 (expectedCount=1, 0 progress) → NOT eligible | unit | sama | ❌ Wave 0 |
| AF-1 | expectedCount=0 (no deliverable unit) → NOT eligible via helper; Tahun 3 by-design eligible di call-site (D-02) | unit | sama | ❌ Wave 0 |
| AF-1 | Sebagian Approved (2/3) → NOT eligible | unit | sama | ❌ Wave 0 |
| AF-3 | MarkCompleted set IsActive=false + IsCompleted=true + EndDate; cascade deactivate ProtonTrackAssignment; histori progress utuh | integration (InMemory DbContext) | `dotnet test --filter "FullyQualifiedName~MarkMappingCompleted"` | ❌ Wave 0 (opsional — bisa UAT-only) |
| AF-1 e2e | track id=4 → assign → approve → CreateAssessment → coachee muncul | manual/Playwright UAT | localhost:5277 (D-14) | manual |
| AF-2 | Cross-unit checkbox disabled; clear → re-enable | manual/Playwright UAT | localhost:5277 | manual |
| AF-5 | Reassign → 3 notif terkirim | manual/Playwright UAT | localhost:5277 | manual |
| AF-6 | Pesan spesifik saat race duplicate | unit (DbUpdateException filter) atau manual | — | ❌ Wave 0 (opsional) |
| AF-7 | `incompleteCoachees` identik sebelum/sesudah batch | unit (InMemory, regresi) | `dotnet test --filter "FullyQualifiedName~ProgressionWarning"` | ❌ Wave 0 (opsional, jaga zero behavior change) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~CoacheeEligibility"` (cepat, <5s, AF-1 helper)
- **Per wave merge:** `dotnet test` (full suite, ~131+ test)
- **Phase gate:** `dotnet build` 0 error + `dotnet test` hijau + Playwright UAT D-14 sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` — covers AF-1 (4 [Fact] minimal: full-approved-eligible, zero-progress-not-eligible, partial-not-eligible, expectedCount-zero-not-eligible)
- [ ] `Helpers/CoacheeEligibilityCalculator.cs` — static helper baru (ekstrak dari GetEligibleCoachees, D-13)
- [ ] (Opsional) `HcPortal.Tests/MarkMappingCompletedTests.cs` — AF-3 cascade+IsActive via InMemory DbContext (pola `OrganizationControllerTests.MakeController`)
- [ ] (Opsional) AF-7 regresi test `incompleteCoachees` parity — bila ingin lock zero-behavior-change otomatis
- Framework install: tidak perlu — xUnit sudah terpasang.

*Catatan: AF-2/AF-5 inherently manual (UI/notif side-effect) — Playwright UAT D-14, bukan xUnit. AF-1 helper adalah satu-satunya logic-bearing wajib-unit (D-13).*

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build + test + run | Diasumsikan ✓ (proyek aktif net8.0) | net8.0 | — |
| SQL Server Express (localhost\SQLEXPRESS) | DB lokal HcPortalDB_Dev + UAT D-14 | Diasumsikan ✓ (DB shared aktif per MEMORY) | — | InMemory untuk unit test (tidak butuh SQL) |
| sqlcmd | SEED_WORKFLOW snapshot/restore | Diasumsikan ✓ (dipakai sebelumnya, MEMORY snapshot .bak) | — | SSMS GUI backup/restore |
| Playwright | UAT D-14 (AF-1/AF-2/AF-5) | Diasumsikan ✓ (dipakai Phase 354/355 MCP) | — | UAT manual browser |
| Kestrel @ localhost:5277 | UAT lokal | ✓ (port standar app, CLAUDE.md) | — | — |

**Missing dependencies with no fallback:** None teridentifikasi — semua tooling sudah dipakai di phase sebelumnya (353-355). [ASSUMED] availability belum di-probe sesi ini (Bash tool = bash, bukan PowerShell; probe `dotnet --version` tidak dijalankan). Planner sebaiknya jalankan `dotnet build` di awal untuk konfirmasi.

**Missing dependencies with fallback:** Unit test AF-1 (helper static) TIDAK butuh SQL Server sama sekali — aman dijalankan di CI tanpa DB (sejalan filter `Category!=Integration`).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Eligibility = total deliverable track (semua unit) | Eligibility = expectedCount per-unit coachee | Phase 356 (AF-1) | Coachee track multi-unit jadi bisa eligible |
| Graduate = `IsCompleted=true` saja (IsActive tetap true) | Graduate = IsCompleted + IsActive=false + cascade | Phase 356 (AF-3) | Coachee graduated bisa re-assign unit lain |
| Reassign tanpa notif | Reassign + 3 notif COACH_REASSIGNED | Phase 356 (AF-5) | Konsisten dengan Assign/Edit/Deactivate |

**Deprecated/outdated:** Tidak ada — phase ini memperbaiki bug, bukan migrasi teknologi. EF Core 8 + ASP.NET Core 8 tetap stack aktif (LTS, valid 2026). Tidak ada API deprecation yang relevan.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Track id=4 masih ada di DB lokal dengan struktur 4 deliverable/2 unit (Alkylation 065=3, RFCC 053=1) seperti saat audit 2026-06-06 | Pitfall 5 / Validation | MEDIUM — bila data berubah, seed fixture harus dibuat ulang dari nol; UAT D-14 perlu re-verify data sebelum test |
| A2 | Tidak ada mapping graduated existing yang `IsActive=true & IsCompleted=true` di DB lokal (atau jumlahnya bisa di-data-fix) | Runtime State Inventory | MEDIUM — bila ada banyak, AF-3 menciptakan inkonsistensi; perlu keputusan data-fix one-off (flag ke planner) |
| A3 | .NET SDK 8 + SQL Server Express + sqlcmd + Playwright tersedia (tidak di-probe sesi ini) | Environment Availability | LOW — semua dipakai phase 353-355; jalankan `dotnet build` di awal untuk konfirmasi |
| A4 | Error number SQL Server 2601/2627 + nama index dalam `InnerException.Message` adalah cara deteksi duplicate-key yang reliable (pola `CertNumberHelper` masih valid untuk SqlServer provider 8.0) | Pattern 4 / AF-6 | LOW — precedent existing teruji; bila provider berubah format pesan, fallback ke `SqlException.Number` |
| A5 | Lokal harus dijalankan dengan `Authentication__UseActiveDirectory=false dotnet run` (appsettings handoff AD=true) | Runtime State / Env | LOW — dari MEMORY Phase 355; bila salah, login admin lokal gagal saat UAT |

## Open Questions

1. **Data-fix mapping graduated existing (AF-3)**
   - What we know: AF-3 set `IsActive=false` saat graduate. Mapping graduated lama mungkin masih `IsActive=true`.
   - What's unclear: Apakah ada record demikian di DB lokal, dan apakah perlu di-normalisasi.
   - Recommendation: Saat planning, jalankan `SELECT Id, CoacheeId, IsActive, IsCompleted FROM CoachCoacheeMappings WHERE IsCompleted = 1` di DB lokal. Bila ada `IsActive=1`, tambah task data-fix one-off (local-only, bukan migration) ATAU putuskan hanya berlaku untuk graduate baru. Bukan blocker — keputusan kecil.

2. **AF-6 deteksi via message-string vs SqlException.Number**
   - What we know: `CertNumberHelper` pakai `InnerException.Message.Contains("2601")`.
   - What's unclear: Apakah lebih robust cast `dbEx.InnerException as Microsoft.Data.SqlClient.SqlException` lalu cek `.Number == 2601`.
   - Recommendation: Ikuti pola existing (message-contains) untuk konsistensi — sudah teruji di codebase. Cast `.Number` adalah peningkatan opsional, bukan keharusan. Discretion executor.

3. **AF-1 helper granularity: satu coachee vs batch**
   - What we know: D-13 minta helper testable; D-01 expectedCount bisa berbeda per coachee (multi-unit).
   - What's unclear: Apakah helper menerima satu coachee (statuses + expectedCount) atau batch dict.
   - Recommendation: Mulai dengan `IsEligiblePerUnit(statuses, expectedCount)` per-coachee (paling sederhana di-test), panggil di loop call-site. Naik ke batch hanya bila call-site jadi rumit. Discretion executor.

## Security Domain

> security_enforcement default enabled. Phase ini = audit-fix internal admin-tool, bukan fitur baru. Cakupan keamanan tipis.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak diubah — auth `[Authorize(Roles="Admin, HC")]` existing dipertahankan verbatim |
| V3 Session Management | no | Tidak diubah |
| V4 Access Control | yes (preserve) | Role guard `Admin, HC` (Assign/Mark/Deactivate) + `Admin` (ApproveReassign L1612) — JANGAN longgarkan; pertahankan attribute existing |
| V5 Input Validation | yes (preserve) | `[ValidateAntiForgeryToken]` existing di semua POST; validasi Section/Unit terhadap OrganizationUnit aktif (L468-471) dipertahankan |
| V6 Cryptography | no | Tidak relevan |

### Known Threat Patterns for ASP.NET Core MVC + EF Core

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection | Tampering | EF Core LINQ parameterized (existing — JANGAN raw SQL). Pertahankan |
| TOCTOU race duplicate mapping | Tampering | Unique-index DB = backstop (AF-6 hanya memperbaiki PESAN, bukan keamanan). Invariant tetap utuh |
| CSRF | Spoofing | `[ValidateAntiForgeryToken]` existing di POST — pertahankan |
| Mass-assignment via JSON payload | Tampering | `CoachAssignRequest` DTO eksplisit (L1674) membatasi field — pertahankan |
| Info-leak via pesan error (AF-6) | Info Disclosure | Pesan AF-6 ramah TANPA bocorkan internal (cukup "coachee sudah punya coach aktif"). JANGAN expose `ex.Message` mentah ke user (pola D6 Phase 334) |

**Catatan keamanan AF-6:** pesan spesifik HARUS tidak membocorkan detail SQL/stack. Pesan dikunci UI-SPEC: `Coachee sudah memiliki coach aktif untuk unit ini...` — aman. Log detail (`_logger.LogWarning`) di server saja.

## Sources

### Primary (HIGH confidence)
- `Controllers/CoachMappingController.cs` (codebase, dibaca sesi ini) — GetEligibleCoachees L1277-1334, AutoCreateProgressForAssignment L1338-1407, MarkMappingCompleted L1075-1109, CoachCoacheeMappingDeactivate L924-944, CoachCoacheeMappingReactivate L1008-1020, CoachCoacheeMappingAssign L474-654, ApproveReassignSuggestion L1614-1638, list action L100-153
- `Models/CoachCoacheeMapping.cs` — kolom IsActive/EndDate/AssignmentUnit/IsCompleted/CompletedAt (no-migration verified)
- `Data/ApplicationDbContext.cs` L321-330 — unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (HasFilter `[IsActive]=1`)
- `Services/INotificationService.cs` L20 — `SendAsync` signature
- `Helpers/CertNumberHelper.cs` L37-42 — `IsDuplicateKeyException` (precedent AF-6)
- `Views/Admin/CoachCoacheeMapping.cshtml` L407-433 (modal) + L670-764 (JS hooks)
- `HcPortal.Tests/OrganizationControllerTests.cs` L19-49 (InMemory pattern), `BuildActualCategoriesTests.cs` (static helper pattern), `PackageImageSyncTests.cs` (mirror-clone pattern), `OrgLabelMigrationIntegrationTests.cs` (disposable real-SQL pattern)
- `HcPortal.Tests/HcPortal.Tests.csproj` + `HcPortal.csproj` — versi paket (net8.0, EF Core 8.0.0, xunit 2.9.3)
- `docs/superpowers/specs/2026-06-06-coach-coachee-assign-audit-fix.md` — spec AF-1..7 (data-verified track id=4)
- `.planning/phases/356-.../356-CONTEXT.md` (D-01..D-15) + `356-UI-SPEC.md` (AF-2 guard + AF-5 microcopy)
- `docs/SEED_WORKFLOW.md` — command sqlcmd BACKUP/RESTORE persis (§5.1/5.2), klasifikasi, journal
- `CLAUDE.md` — Develop Workflow (Lokal→Dev→Prod), Seed Workflow, Bahasa Indonesia

### Secondary (MEDIUM confidence)
- `~/.claude/.../MEMORY.md` — Phase 353/354/355 lessons (AD=false flag lokal, ~131 test hijau, track id=4 data, SEED_WORKFLOW restore bersih)

### Tertiary (LOW confidence)
- None — semua klaim factual berasal dari codebase/spec yang dibaca langsung sesi ini.

## Project Constraints (from CLAUDE.md)

- **Bahasa:** Selalu respon + tulis user-facing string (pesan error, notif, hint, komentar penting) dalam **Bahasa Indonesia**.
- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Fix di lokal, verifikasi lokal (`dotnet build` + `dotnet run` @5277 + cek DB lokal + Playwright bila ada), commit & push (flag migration), promosi Dev/Prod = tanggung jawab IT.
- **JANGAN** edit kode/DB langsung di server Dev/Prod. **JANGAN** push tanpa verifikasi lokal.
- **Seed Workflow:** Klasifikasi dulu (track id=4 fixture = `temporary + local-only`), snapshot DB (`sqlcmd BACKUP`), catat `SEED_JOURNAL.md`, restore (`RESTORE WITH REPLACE`) setelah test (sukses/gagal), tandai `cleaned`. JANGAN biarkan seed temporary nempel.
- **Migration:** D-12 = FALSE. Bila ternyata butuh migration, WAJIB sertakan file migration di commit + notif IT (CLAUDE.md). Tapi Phase 356 dirancang 0-migration agar tak nambah beban handoff bundle v19-v24.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi dibaca langsung dari `.csproj` (net8.0, EF Core 8.0.0, xunit 2.9.3)
- Architecture/patterns: HIGH — setiap pola fix punya precedent file:line di codebase (Deactivate, CertNumberHelper, SendAsync, AutoCreateProgress, test pola)
- Pitfalls: HIGH — diturunkan dari code-reading + comment existing (`OrganizationControllerTests` Pitfall casing) + spec data-verified
- Runtime state / data-fix AF-3: MEDIUM — perlu query DB lokal saat planning untuk konfirmasi (A2)
- Environment: MEDIUM — availability di-asumsi dari history phase 353-355, tidak di-probe sesi ini (A3)

**Research date:** 2026-06-09
**Valid until:** 30 hari (stack LTS stabil; codebase target satu file; risiko utama = data DB lokal track id=4 berubah → re-verify sebelum UAT)

*Phase: 356-audit-fix-assign-coach-coachee*
