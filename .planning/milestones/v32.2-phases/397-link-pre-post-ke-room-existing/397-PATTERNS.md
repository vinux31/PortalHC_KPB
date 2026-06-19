# Phase 397: Link Pre/Post ke Room Existing - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 7 surfaces (6 modified files + 1 new e2e file) across ~11 distinct code surfaces
**Analogs found:** 11 / 11 (semua exact/role-match — brownfield extend, nol greenfield)

> **VERIFIKASI:** Setiap analog di bawah sudah dibaca langsung dari live code 2026-06-18. Nomor baris yang dikutip RESEARCH.md COCOK 100% dengan kondisi file saat ini (395 & 396 masih PLAN — belum commit — sehingga belum menggeser baris di file-overlap). **Re-verifikasi nomor baris saat plan bila 395/396 ter-commit lebih dulu** (scope-lock CONTEXT: 397 sequential SETELAH 395 & 396).

---

## File Classification

| Surface (New/Modified) | File | Role | Data Flow | Closest Analog | Match Quality |
|------------------------|------|------|-----------|----------------|---------------|
| `SearchLinkTargets` JSON action (NEW) | `Controllers/InjectAssessmentController.cs` | controller (route, JSON) | request-response (read) | `AssessmentAdminController.cs:112-246` `ManageAssessmentTab_Assessment` (projection) + `InjectAssessmentController.cs:103-156` `PreviewInjectScore` (JSON return shape) | exact (projection) + exact (JSON pattern) |
| Per-worker `LinkedSessionId` + Kasus A/B `LinkedGroupId` resolution (MODIFY) | `Services/InjectAssessmentService.cs` | service | transform (in-tx write) | `AssessmentAdminController.cs:1307-1314` cross-link loop (template) + `InjectAssessmentService.cs:104-130` session construction (broadcast `:119-120` to replace) | exact (template) |
| Kasus B write-to-online (all room target sessions) (MODIFY) | `Services/InjectAssessmentService.cs` | service | transform (in-tx write to existing data) | `AssessmentAdminController.cs:1264-1315` atomic tx + cross-link; `CMPController.cs:289-294` query sesi by LinkedGroupId | role-match (write-to-online net-new logic) |
| Anti-double-link preflight (MODIFY) | `Services/InjectAssessmentService.cs` | service | request-response (validate, no write) | `InjectAssessmentService.cs:341-442` `PreflightValidateAsync` (collect-all, no early-return) | exact |
| Preview pairing summary (MODIFY) | `Controllers/InjectAssessmentController.cs` + `Models/InjectAssessmentDtos.cs` | controller + model | request-response (dry-run) | `InjectAssessmentController.cs:103-156` `PreviewInjectScore`; `Models/InjectAssessmentDtos.cs:92-120` `InjectPreviewRequest/Result` | exact |
| Audit `LinkPrePost` / `LinkPrePostUndo` (MODIFY) | `Services/InjectAssessmentService.cs` + `Controllers/InjectAssessmentController.cs` | service/controller | event-driven (audit) | `InjectAssessmentService.cs:289-307` `_context.AuditLogs.Add` in-tx; `Models/AuditLog.cs:28-30` MaxLength(50) | exact |
| `MapToRequest` populate `LinkedGroupId`/`LinkedSessionId` (MODIFY) | `Controllers/InjectAssessmentController.cs` | controller (mapping) | transform | `InjectAssessmentController.cs:336-378` `MapToRequest` (gap: link fields NOT populated) | exact (extend existing) |
| `UnlinkInjectGroup` endpoint + UnlinkAsync (NEW) | `Controllers/InjectAssessmentController.cs` + `Services/InjectAssessmentService.cs` | controller + service | request-response + transform (atomic revert) | `AssessmentAdminController.cs:2398-2537` `DeleteAssessmentGroup` (atomic + audit + partial-failure guard) | role-match (revert vs delete) |
| Room picker modal + "Cari Room" button + chip (MODIFY) | `Views/Admin/InjectAssessment.cshtml` | view (Razor + JS) | request-response (UI runtime) | `CreateAssessment.cshtml:757-834` `#successModal` (modal-lg pattern); `InjectAssessment.cshtml:143-152` input-group `#btnCheckTitle`; `:154-163` `#assessmentTypeInput` + placeholder `:162` | exact (modal) + exact (button/placeholder) |
| ViewModel hidden link fields (MODIFY) | `ViewModels/InjectAssessmentViewModel.cs` | model | data-binding | `ViewModels/InjectAssessmentViewModel.cs:13-42` setup-room scalar fields | exact |
| xUnit cross-grouping + linking + unlink tests (NEW) | `HcPortal.Tests/*.cs` | test | batch (integration real-SQL) | `HcPortal.Tests/InjectAssessmentServiceTests.cs:24-60` `InjectAssessmentFixture` (disposable real-SQL); `CMPController.cs:3412-3502` `GetGainScoreData` (assert target) | exact (fixture) |
| Playwright modal/picker/chip/preview/unlink e2e (NEW) | `tests/e2e/inject-assessment-397.spec.ts` | test | event-driven (runtime UI) | `tests/e2e/inject-assessment-395.spec.ts:1-55` (login admin, serial, dbSnapshot beforeAll/afterAll, `--workers=1`) | exact |

---

## Pattern Assignments

### Surface 1: `SearchLinkTargets` JSON action — `Controllers/InjectAssessmentController.cs` (controller, request-response)

**Analog A (projection shape):** `Controllers/AssessmentAdminController.cs:141-197` `ManageAssessmentTab_Assessment`
**Analog B (JSON return + RBAC + ValidateAntiForgery for POST, or HttpGet):** `Controllers/InjectAssessmentController.cs:103-156` `PreviewInjectScore`

**KRITIS:** `ManageAssessmentTab_Assessment` returns `PartialView("Shared/_AssessmentGroupsTab", null)` [VERIFIED :245] — HTML, BUKAN JSON. JANGAN parse HTML di modal. Salin SHAPE projection-nya tapi `return Json(...)`.

**Field projection tersedia (salin shape :143-176):**
```csharp
// Source: AssessmentAdminController.cs:143-176 — field yang dibawa picker (D-06):
.Select(a => new {
    a.Id, a.Title, a.Category, a.Schedule, ..., a.AssessmentType, a.LinkedGroupId,
    UserId = a.User != null ? a.User.Id : ""
})
// Grouping (:157-176): prePostGrouped → RepresentativeId = rep.Id (:168), UserCount (:171),
//   IsPrePostGroup = true (:173), LinkedGroupId = g.Key (:174).
//   rep = PreTest pertama OrderBy CreatedAt, fallback first (:160).
// standaloneGrouped (:178-197): LinkedGroupId = null (:195) → Kasus B; RepresentativeId = rep.Id (:189).
```

**Filter tipe-LAWAN (D-06) + JANGAN filter IsManualEntry (D-10):**
```csharp
// injectType = AssessmentType yang sedang di-inject; tampilkan tipe-LAWAN:
var oppositeType = injectType == "PreTest" ? "PostTest" : "PreTest";   // D-06 (whitelist input, V5)
// .Where(s => s.AssessmentType == oppositeType)  ← tipe-lawan; TIDAK ada .Where(IsManualEntry...) (D-10)
```

**Search pattern (reuse :123-133 — parameterized via EF, SQL-injection-safe):**
```csharp
// Source: AssessmentAdminController.cs:125-133
var lowerSearch = search.ToLower();
managementQuery = managementQuery.Where(a =>
    a.Title.ToLower().Contains(lowerSearch) || a.Category.ToLower().Contains(lowerSearch) || ...);
```

**RBAC + JSON return (Analog B :103-106, 155):**
```csharp
[HttpGet]                                  // picker = read → GET (no antiforgery; data read-only)
[Authorize(Roles = "Admin, HC")]           // VERIFIED pola :104
public async Task<IActionResult> SearchLinkTargets(string? term, string injectType) {
    ...
    return Json(rows);   // ⚠ Json — bukan PartialView. View()-override ~/Views/Admin/ hanya kena ViewResult.
}
```
Tambahkan ke output: `RepresentativeId`, `Title`, `Category`, `Schedule`/`CompletedAt`, `AssessmentType`, `LinkedGroupId` (null=Kasus B), `UserCount`, `IsPrePostGroup`, `IsManualEntry` (badge "Inject", D-10).

---

### Surface 2: Per-worker bidirectional linking — `Services/InjectAssessmentService.cs` (service, transform)

**Analog (template eksak):** `Controllers/AssessmentAdminController.cs:1307-1314` cross-link loop
**Lokasi yang DIUBAH:** `Services/InjectAssessmentService.cs:119-120` (broadcast — HARUS diganti)

**Template online (index-paired karena Pre+Post dibuat bersamaan urut UserIds):**
```csharp
// Source: AssessmentAdminController.cs:1307-1314 (VERIFIED live)
for (int i = 0; i < preSessions.Count; i++) {
    preSessions[i].LinkedGroupId = linkedGroupId;
    preSessions[i].LinkedSessionId = postSessions[i].Id;
    postSessions[i].LinkedSessionId = preSessions[i].Id;
}
```

**State sekarang (BUG untuk 397 — broadcast 1 nilai ke SEMUA sesi):**
```csharp
// Source: InjectAssessmentService.cs:119-120 (VERIFIED) — di dalam session construction loop :98-128
LinkedGroupId = req.LinkedGroupId,       // OK level-room (1 nilai untuk semua)
LinkedSessionId = req.LinkedSessionId,   // ⚠ BUG: broadcast 1 sibling.id ke semua sesi → SEMUA salah-tunjuk
```

**Adaptasi 397 (sibling resolve BY-UserId, bukan index — sisi online sudah ada di DB):**
- Ambil siblingByUserId SEKALI per batch (hindari N+1) setelah `resolvedGroupId` diketahui:
  `_context.AssessmentSessions.Where(s => s.LinkedGroupId == resolvedGroupId && s.AssessmentType == oppositeType).ToDictionaryAsync(s => s.UserId, s => s)`
  (guard `FirstOrDefault` bila ada >1 sibling per user — Assumption A2).
- Per sesi inject (di loop :98-287, setelah `session.Id` ada di :130):
  - `session.LinkedGroupId = resolvedGroupId` (SELALU set — gabung grup, D-03).
  - `if (siblingByUserId.TryGetValue(user.Id, out var sib)) { session.LinkedSessionId = sib.Id; sib.LinkedSessionId = session.Id; /* write-back tracked update dalam tx */ }`
  - `else { session.LinkedSessionId = null; }` (unpaired D-03 — tampil sisi tunggal).
- **Display pairing match by `LinkedGroupId` + `UserId`** [VERIFIED CMPController.cs:3417-3433] → `LinkedGroupId` WAJIB benar; `LinkedSessionId` = fidelitas saja.

---

### Surface 3: Kasus A vs B `LinkedGroupId` resolution + write-to-online — `Services/InjectAssessmentService.cs` (service, transform-on-existing)

**Analog (konvensi nilai):** `AssessmentAdminController.cs:1270` `int linkedGroupId = preSessions[0].Id;`
**Analog (query sesi by group):** `CMPController.cs:289-294`

```csharp
// D-01 Kasus A (target.LinkedGroupId != null): ADOPT — tak sentuh online grouping value.
//   resolvedGroupId = target.LinkedGroupId.Value
// D-01 Kasus B (target.LinkedGroupId == null): tulis stiker baru.
//   resolvedGroupId = target.RepresentativeId   // id sesi nyata representatif (konvensi :1270 = preSessions[0].Id)
//   → tulis resolvedGroupId ke SEMUA sesi room target (online), BUKAN hanya yang ter-pair (Pitfall 2).
//     Query semua sesi room target standalone → set LinkedGroupId masing-masing (dalam tx).
//   → skor/jawaban/status online TIDAK disentuh — HANYA kolom LinkedGroupId/LinkedSessionId (Anti-pattern, V Tampering).
//   → AuditLog "LinkPrePost" PER sesi online dimutasi (Surface 6).
```
**Anti-pattern (HARAM):** Jangan SetProperty `Score`/`IsPassed`/`Status`/responses sesi online. **RepresentativeId room target** = `rep.Id` (PreTest pertama atau first) [VERIFIED AssessmentAdminController.cs:160,168].

---

### Surface 4: Anti-double-link preflight (D-08) — `Services/InjectAssessmentService.cs` (service, validate)

**Analog (collect-all, no early-return):** `Services/InjectAssessmentService.cs:341-442` `PreflightValidateAsync`

```csharp
// Source pattern: PreflightValidateAsync :344-368 — mengumpulkan SEMUA error, JANGAN early-return (daftar lengkap, 396 D-09).
//   var errors = new List<InjectRowError>();  ... errors.Add(new InjectRowError { Nip=..., Message=... });
// 397: query pekerja yang sudah punya sibling tipe-SAMA di grup target (2 Pre/1 user = ambigu gain-score by UserId):
//   var existingSameType = await _context.AssessmentSessions
//       .Where(s => s.LinkedGroupId == resolvedGroupId
//                && s.AssessmentType == req.AssessmentType    // tipe SAMA dengan yang di-inject
//                && targetUserIds.Contains(s.UserId))
//       .Select(s => s.UserId).ToListAsync();
//   → setiap UserId match: errors.Add (daftar LENGKAP). Default BLOK per-pekerja (UI-SPEC N4).
```
**Penempatan:** sebelum tx menulis (pola `PreflightValidateAsync` dipanggil di :48 sebelum `BeginTransactionAsync` :88). Pola reject-all sudah ada (:49-68 set `Rejected=true` + `PerRowErrors`).

---

### Surface 5: Preview pairing summary (D-07) — `Controllers/InjectAssessmentController.cs` + `Models/InjectAssessmentDtos.cs` (controller, dry-run)

**Analog (engine + JSON dry-run):** `Controllers/InjectAssessmentController.cs:103-156` `PreviewInjectScore`
**Analog (DTO bentuk):** `Models/InjectAssessmentDtos.cs:92-120` `InjectPreviewRequest` / `InjectPreviewResult`

```csharp
// Source: PreviewInjectScore :106-155 — [HttpPost][Authorize Admin,HC][ValidateAntiForgeryToken], return Json, NO SaveChanges.
//   Engine skor = AssessmentScoreAggregator.Compute (:134) — preview == commit (D-07 dikunci 395/396).
// 397 EXTEND (pairing dry-run, NO write):
//   var targetSessions = await _context.AssessmentSessions.AsNoTracking()
//       .Where(s => s.LinkedGroupId == resolvedGroupId && s.AssessmentType == oppositeType)
//       .Select(s => new { s.UserId, s.CompletedAt }).ToListAsync();
//   var siblingUserIds = targetSessions.Select(t => t.UserId).ToHashSet();
//   int paired = injectUserIds.Count(uid => siblingUserIds.Contains(uid));   // → LinkedSessionId terisi
//   int unpaired = injectUserIds.Count - paired;                            // D-03 sisi tunggal
//   bool willTouchOnline = (target.LinkedGroupId == null);                  // Kasus B (banner D-07)
//   // D-11 dateWarn: Pre.CompletedAt > Post.CompletedAt (skip bila sibling CompletedAt null — Open Q 2)
//   // D-08 doubleLinkNips: sibling tipe-SAMA di grup target
```
**Tambahkan field pairing** ke `InjectPreviewResult` (atau response baru): `paired`, `unpaired`, `willTouchOnline`, `dateWarn`, `doubleLinkNips[]`. Tetap `return Json(result)` tanpa `SaveChanges`.

---

### Surface 6: Audit `LinkPrePost` / `LinkPrePostUndo` (D-09) — `Services/InjectAssessmentService.cs` + controller (event-driven)

**Analog (in-tx audit):** `Services/InjectAssessmentService.cs:289-307` (ManualInject `_context.AuditLogs.Add` LANGSUNG)
**Constraint:** `Models/AuditLog.cs:28-30` `[MaxLength(50)] ActionType` → "LinkPrePost"(11) / "LinkPrePostUndo"(15) AMAN.

```csharp
// Source: InjectAssessmentService.cs:297-307 (VERIFIED in-tx pattern)
foreach (var onlineSessionId in mutatedOnlineSessionIds) {
    _context.AuditLogs.Add(new AuditLog {
        ActorUserId = actorUserId, ActorName = actorDisplay,
        ActionType = "LinkPrePost",                       // ⚠ ≤ MaxLength(50)
        Description = $"Stiker grup ditulis ke sesi online {onlineSessionId} (LinkedGroupId={resolvedGroupId}). Skor/jawaban tidak diubah.",
        TargetId = onlineSessionId, TargetType = "AssessmentSession", CreatedAt = DateTime.UtcNow
    });
}
// SaveChanges + CommitAsync SEKALI di akhir tx (BUKAN per-add).
```
**KRITIS (Pitfall 3):** JANGAN `AuditLogService.LogAsync` di dalam tx — ia `await _context.SaveChangesAsync()` internal [VERIFIED AuditLogService.cs:41] → commit parsial. Catatan eksplisit ada di service [VERIFIED :289-290].

---

### Surface 7: `MapToRequest` populate link fields — `Controllers/InjectAssessmentController.cs` (controller, transform)

**Analog (extend existing):** `Controllers/InjectAssessmentController.cs:336-378` `MapToRequest`
**GAP (terverifikasi):** `req` init :343-354 dan loop worker :360-375 **TIDAK** mengisi `LinkedGroupId`/`LinkedSessionId` — meski DTO sudah punya kolomnya [VERIFIED InjectAssessmentDtos.cs:63-64].

```csharp
// Source: MapToRequest :343-354 (req init) — AssessmentType di-map :347. Tambahkan link field dari VM hidden:
var req = new InjectRequest {
    ...
    AssessmentType = vm.AssessmentType ?? "Standard",   // VERIFIED :347
    // 397: req.LinkTargetRepId / LinkCaseB / (per-worker sibling map di-resolve SERVER di InjectBatchAsync, BUKAN trust client)
};
```
**Security (V Tampering):** server re-resolve `resolvedGroupId` dari `LinkTargetRepId` (validasi room ada + tipe-lawan); JANGAN trust `LinkedGroupId` mentah dari client.

---

### Surface 8: `UnlinkInjectGroup` (D-12) — `Controllers/InjectAssessmentController.cs` + service (request-response + atomic revert)

**Analog (atomic + audit + partial-failure guard):** `Controllers/AssessmentAdminController.cs:2398-2537` `DeleteAssessmentGroup`

```csharp
// Source pattern: DeleteAssessmentGroup :2398-2537 (try/catch luar + atomic + audit SELALU ditulis + partial-aware).
[HttpPost][Authorize(Roles = "Admin, HC")][ValidateAntiForgeryToken]   // VERIFIED RBAC+CSRF pola :104-105
public async Task<IActionResult> UnlinkInjectGroup(int injectGroupId) {
    using var tx = await _context.Database.BeginTransactionAsync();     // pola InjectAssessmentService.cs:88
    try {
        // 1. ambil sesi inject ber-LinkedGroupId = injectGroupId (validasi IsManualEntry — IDOR guard)
        // 2. per sesi inject: jika LinkedSessionId != null → revert sibling.LinkedSessionId = null (bidirectional)
        // 3. set sesi inject LinkedGroupId = null, LinkedSessionId = null
        // 4. Kasus B kosong-sebelah: revert LinkedGroupId pada online di-stiker — HEURISTIK aman = revert hanya
        //    bila group jadi single-type pasca-unlink (Open Q 1, default konservatif), ATAU query audit "LinkPrePost" TargetId
        // 5. _context.AuditLogs.Add "LinkPrePostUndo" per sesi dimutasi (Surface 6 pattern, MaxLength OK)
        await _context.SaveChangesAsync(); await tx.CommitAsync();
    } catch { await tx.RollbackAsync(); /* TempData["Error"] */ }
}
```
**Scope guard (D-12):** minimal — lepas/ganti-room saja. TIDAK ada editor link umum / bulk re-link / per-sesi individual.

---

### Surface 9: Room picker modal + "Cari Room" button + chip — `Views/Admin/InjectAssessment.cshtml` (view, runtime UI)

**Analog (modal):** `Views/Admin/CreateAssessment.cshtml:757-834` `#successModal`
**Analog (input-group + tombol search):** `Views/Admin/InjectAssessment.cshtml:143-152` `#btnCheckTitle`
**Lokasi (ganti placeholder):** `Views/Admin/InjectAssessment.cshtml:154-163` blok Tipe Assessment, **ganti note `:162`** ("...tersedia pada fase berikutnya").

> **CATATAN:** Tidak ada modal apa pun di InjectAssessment.cshtml saat ini (grep modal/data-bs-toggle = 0 match) → modal picker GENUINELY net-new, SALIN pola dari CreateAssessment.

**Pola modal (REUSE verbatim struktur :757-766):**
```html
<!-- Source: CreateAssessment.cshtml:757-766 -->
<div class="modal fade" id="roomPickerModal" tabindex="-1" aria-labelledby="..." aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered modal-lg">       <!-- modal-lg: baris room muat 1 baris -->
    <div class="modal-content border-0 shadow">
      <div class="modal-header bg-light">                          <!-- bg-light netral (header sukses-hijau tak cocok picker, UI-SPEC N2a) -->
        <h5 class="modal-title fw-bold">...Cari Room Pasangan (Pre/Post)</h5>
        <button class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
      </div>
      <div class="modal-body p-4"> ... search + #roomPickerResults list-group ... </div>
    </div>
  </div>
</div>
```

**Pola tombol "Cari Room" (REUSE input-group :143-148):**
```html
<!-- Source: InjectAssessment.cshtml:145-148 (#btnCheckTitle = analog) -->
<button type="button" class="btn btn-outline-primary" id="btnCariRoom"><i class="bi bi-search me-1"></i>Cari Room Pasangan</button>
<!-- show/hide via JS pada #assessmentTypeInput 'change' (PreTest/PostTest → tampil; Standard → d-none) -->
```

**Pola placeholder note yang DIGANTI :162 (VERIFIED):**
```html
<div class="form-text text-muted"><i class="bi bi-info-circle me-1"></i>Penautan Pre/Post ke room existing tersedia pada fase berikutnya.</div>
```
→ ganti dengan `#prePostLinkBlock` (tombol + hint tipe-lawan + chip + hidden field), per UI-SPEC N1.

**Render data user (judul/kategori/NIP/Nama room) via `.textContent`** (XSS-safe, carry 395/396; UI-SPEC §Aksesibilitas). JANGAN `innerHTML`.

**WAJIB Playwright runtime** (lesson 354/392; app `AddControllersWithViews` tanpa RuntimeCompilation — view embedded). Show/hide, modal open/close, debounce search, render baris, set chip, render pairing summary, dialog unlink — grep+build TAK cukup.

---

### Surface 10: ViewModel hidden link fields — `ViewModels/InjectAssessmentViewModel.cs` (model, data-binding)

**Analog:** `ViewModels/InjectAssessmentViewModel.cs:13-42` (setup-room scalar fields, e.g. `AssessmentType :16`, `Step5Method :42` precedent untuk field VM-only).

```csharp
// Source: ViewModel scalar pattern :14-26. Tambahkan hidden field link (bentuk pasti = plan-time):
//   public int? LinkedTargetRepId { get; set; }   // RepresentativeId room target dari chip (di-resolve server)
//   public bool LinkCaseB { get; set; }           // flag standalone (Kasus B) — opsional, server boleh re-derive
// Catatan: Mode/TargetScore precedent (:82-83) = "lapisan VM/controller saja, tak masuk DTO/service" → ikuti
//   pola: client set hidden via chip JS, MapToRequest baca, server re-resolve otoritatif (Surface 7).
```

---

### Surface 11: xUnit tests (NEW) — `HcPortal.Tests/*.cs` (test, integration real-SQL)

**Analog (fixture disposable real-SQL):** `HcPortal.Tests/InjectAssessmentServiceTests.cs:24-60` `InjectAssessmentFixture`
**Analog (assert grouping target):** `Controllers/CMPController.cs:3412-3502` `GetGainScoreData` (pair by LinkedGroupId+UserId)

```csharp
// Source: InjectAssessmentFixture :24-57 — disposable HcPortalDB_Test_{guid}, MigrateAsync, EnsureDeletedAsync.
//   [Trait("Category","Integration")] :59 → skip via --filter "Category!=Integration" (fast suite).
//   ⚠ EF Core 8 ExecuteUpdate butuh REAL SQL (SQLEXPRESS), BUKAN InMemory (catatan :9). DB lokal HcPortalDB_Dev TAK tersentuh.
```
**Test files (Wave 0):** `InjectLinkPrePostTests.cs` (per-worker linking, Kasus A/B, atomic rollback, write-to-online), `InjectAntiDoubleLinkTests.cs` (D-08 daftar-lengkap), `InjectPreviewPairingTests.cs` (preview==commit), `InjectCrossGroupingTests.cs` (silang inject↔online utuh di gain-score, KRITIS spec §13), `UnlinkInjectGroupTests.cs` (revert atomic + audit).
**Sampling (RESEARCH §Validation):** per-commit `dotnet test --filter "Category!=Integration"` + `dotnet build`; per-wave `dotnet test` penuh; gate full suite green + Playwright + DB lokal verify (CLAUDE.md Develop Workflow).

---

### Surface 12: Playwright e2e (NEW) — `tests/e2e/inject-assessment-397.spec.ts` (test, runtime UI)

**Analog:** `tests/e2e/inject-assessment-395.spec.ts:1-55`

```typescript
// Source: inject-assessment-395.spec.ts:13-31
test.describe.configure({ mode: 'serial' });            // serial (DB-mutating)
async function loginAdmin(page) { /* admin@pertamina.com / 123456, helpers/accounts.ts */ }
// beforeAll: db.snapshot (CLAUDE.md Seed Workflow); afterAll: db.restore + catat SEED_JOURNAL.md.
// Run: cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1
// Server: MAIN tree localhost:5277, Authentication__UseActiveDirectory=false (lesson 354/392).
```
**Cakupan (UI-SPEC §Interaction Contracts 1-10):** tombol Cari Room kondisional tipe, modal buka + filter tipe-lawan, badge Kasus A/B, pilih → chip + skippable, ringkasan pairing di Pratinjau, WARN tanggal, anti-dobel daftar, commit byte-identik, unlink + konfirmasi destruktif (modal Bootstrap BUKAN `confirm()` native), skip penautan standalone.

---

## Shared Patterns

### Authentication / RBAC
**Source:** `Controllers/InjectAssessmentController.cs:104` `[Authorize(Roles = "Admin, HC")]`
**Apply to:** SEMUA action baru (SearchLinkTargets, UnlinkInjectGroup) + extend PreviewInjectScore/MapToRequest.
```csharp
[HttpGet] / [HttpPost]
[Authorize(Roles = "Admin, HC")]          // V2/V4 — server-authoritative; picker tak boleh leak ke non-Admin/HC
[ValidateAntiForgeryToken]                 // POST saja (CSRF V Tampering) — pola :105
```

### Atomic transaction (semua write link dalam 1 tx)
**Source:** `Services/InjectAssessmentService.cs:88-322` `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync` / catch `RollbackAsync`
**Apply to:** InjectBatchAsync (inject + write-back online Kasus B + audit), UnlinkInjectGroup.
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();   // VERIFIED :88
try { /* ... semua write ... */ await _context.SaveChangesAsync(); await transaction.CommitAsync(); }
catch { await transaction.RollbackAsync(); /* rollback TOTAL */ }          // VERIFIED :328-333
```

### Audit in-transaction (compliance, Repudiation)
**Source:** `Services/InjectAssessmentService.cs:297-307` `_context.AuditLogs.Add` LANGSUNG; constraint `Models/AuditLog.cs:29` MaxLength(50).
**Apply to:** Kasus B write-to-online ("LinkPrePost"), bidirectional online write ("LinkPrePost"), unlink ("LinkPrePostUndo").
**JANGAN** pakai `AuditLogService.LogAsync` di dalam tx (SaveChanges internal :41 → commit parsial).

### Collect-all error list (warn-but-allow + daftar lengkap)
**Source:** `Services/InjectAssessmentService.cs:341-442` `PreflightValidateAsync` (no early-return); reject-all `:49-68`.
**Apply to:** anti-double-link (D-08), unpaired warn (D-03), date warn (D-11) — semua via preview/error list. Pola 396 D-09.

### XSS-safe render (Tampering)
**Source:** UI-SPEC §Aksesibilitas (carry 395/396) — `.textContent`, BUKAN `innerHTML`.
**Apply to:** judul/kategori/NIP/Nama room di modal picker, chip, daftar error, ringkasan pairing.

### Display pairing invariant (load-bearing)
**Source:** `Controllers/CMPController.cs:3417-3433` (gain-score) + `:253-306` (Results) — pair by `LinkedGroupId` + `UserId`, BUKAN `LinkedSessionId`.
**Implikasi semua surface:** `LinkedGroupId` WAJIB benar (D-01) agar pasangan tampil; `LinkedSessionId` = fidelitas "seakan online" (D-02). Cross-grouping silang inject↔online GRATIS asal `LinkedGroupId` benar (spec §13 — test KRITIS).

---

## No Analog Found

Tidak ada surface tanpa analog. Semua 11 surface = brownfield extend dengan analog exact/role-match yang sudah diverifikasi live. Logika genuinely net-new (write-to-online Kasus B, anti-double-link query, unlink revert) tetap MEMAKAI pola host yang ada (tx atomic + audit in-tx + collect-all) — hanya QUERY-nya baru, bukan strukturnya.

**Catatan 1 area MEDIUM (RESEARCH Open Q 1):** Unlink Kasus B revert — tidak ada flag skema "stiker ditulis oleh inject". Rekomendasi default = revert HANYA bila group jadi single-type pasca-unlink (heuristik konservatif), ATAU query AuditLog "LinkPrePost" `TargetId`. Plan pilih satu; jaga minimal (D-12).

---

## Metadata

**Analog search scope:** `Controllers/` (InjectAssessmentController, AssessmentAdminController, CMPController), `Services/` (InjectAssessmentService, AuditLogService), `Models/` (InjectAssessmentDtos, AuditLog, AssessmentSession), `ViewModels/`, `Views/Admin/` (InjectAssessment, CreateAssessment), `HcPortal.Tests/`, `tests/e2e/`.
**Files scanned (read direct):** 11 source files + 3 planning docs (CONTEXT/RESEARCH/UI-SPEC).
**Line-number verification:** semua citation RESEARCH.md COCOK dengan live code 2026-06-18 (395/396 belum commit). Re-verify saat plan bila 395/396 ter-commit.
**Pattern extraction date:** 2026-06-18
