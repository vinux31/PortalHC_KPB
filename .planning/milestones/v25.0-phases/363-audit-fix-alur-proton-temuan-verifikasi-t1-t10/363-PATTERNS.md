# Phase 363: Audit Fix Alur PROTON (T1-T10) - Pattern Map

**Mapped:** 2026-06-11
**Files analyzed:** 9 (5 modified source + 1 view + 3 test)
**Analogs found:** 9 / 9 (semua analog ada di codebase ini — fase "menyamakan logic existing", bukan greenfield)

> Catatan penting: untuk T1/T2/T7/T8/T5 analog terbaik adalah **method lain di file yang sama** (akar drift = dua salinan logic). "Analog" di sini = versi gold-standard yang helper baru harus serap / di-mirror. Untuk T3/T4/T6/T9/T10 analog = pola gate / notif / audit existing.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/CDPController.cs` (T1/T2/T7 helper) | controller | request-response + event-driven (notif) | `ApproveDeliverable`/`RejectDeliverable` (same file `:818-1080`) | exact (self-analog, gold-std) |
| `Controllers/CDPController.cs` (T5 query) | controller | CRUD / read-aggregate | `ExportHistoriProton` (same file `:3290-3417`) | exact (duplicate logic) |
| `Controllers/CDPController.cs` (T8 append) | controller | file-I/O + transform | `UploadEvidence` (same file `:1280-1287`) | exact (same pattern) |
| `Controllers/CoachMappingController.cs` (T3 gate) | controller | request-response (authz gate) | existing gate `:526-552` + `AssessmentAdminController:1372-1379` | exact (mirror gate) |
| `Controllers/CoachMappingController.cs` (T9 log) | controller | request-response (defensive) | `_logger.LogWarning` pola throughout file | role-match |
| `Services/GradingService.cs` (T6) | service | transform / CRUD | `GradeAndCompleteAsync:282-287` (parity target) | exact (parity to normal path) |
| `Services/ProtonCompletionService.cs` (T4 surface) | service | CRUD + event-driven (notif/audit) | `CreateHCNotificationAsync:1111-1140` + `AuditLogService.LogAsync` | role-match (compose 2 patterns) |
| `Controllers/AssessmentAdminController.cs` (T9/T10) | controller | request-response / doc | `_logger.LogWarning` + comment-only | role-match |
| `Views/CDP/HistoriProton.cshtml` (T5) | view | request-response | self (`:143-155` badge sudah 3-cabang) | exact (no/minimal change) |
| `HcPortal.Tests/ProtonApproveRejectParityTests.cs` (new) | test | integration (SQL fixture) | `ProtonCompletionServiceTests` fixture `:25-110` | exact (fixture reuse) |
| `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` (extend) | test | integration | self `:68-90` (predikat-replikasi) | exact (extend existing) |
| `HcPortal.Tests/ProtonCompletionMissTests.cs` (new) | test | integration | `ProtonBypassServiceTests` FakeNotif `:24-47` | exact (fake notif + fixture) |

---

## Pattern Assignments

### `Controllers/CDPController.cs` — Helper `approve-core` / `reject-core` (T1/T2/T7, controller, request-response + event-driven)

**Analog:** `ApproveDeliverable` (`CDPController.cs:818-973`) + `RejectDeliverable` (`:978-1080`) — versi gold-standard. Versi divergen yang harus konvergen: `ApproveFromProgress` (`:1939-2013`), `RejectFromProgress` (`:2018-2091`).

**Ctor deps yang sudah tersedia (no DI change)** (`:33-50`):
```csharp
private readonly UserManager<ApplicationUser> _userManager;
private readonly ApplicationDbContext _context;
private readonly INotificationService _notificationService;
private readonly ILogger<CDPController> _logger;
private readonly AuditLogService _auditLog;
// → helper sebagai PRIVATE METHOD di CDPController dapat akses semua ini langsung (rekomendasi RESEARCH §Pola 1)
```

**Pattern A — per-role approval set** (gold-std `:868-880`, harus IDENTIK di kedua endpoint):
```csharp
if (userRole == UserRoles.SrSupervisor)
{
    progress.SrSpvApprovalStatus = "Approved";
    progress.SrSpvApprovedById = user.Id;
    progress.SrSpvApprovedAt = DateTime.UtcNow;
}
else if (userRole == UserRoles.SectionHead)
{
    progress.ShApprovalStatus = "Approved";
    progress.ShApprovedById = user.Id;
    progress.ShApprovedAt = DateTime.UtcNow;
}
```

**Pattern B — race-guard reload-fresh D-10 (T7 — MASUK approve-core)** (`:882-901`):
```csharp
var freshStatus = await _context.ProtonDeliverableProgresses
    .Where(p => p.Id == progressId)
    .Select(p => new { p.Status, p.SrSpvApprovalStatus, p.ShApprovalStatus })
    .AsNoTracking()
    .FirstOrDefaultAsync();
if (freshStatus == null) return NotFound();   // helper: return ok=false/error; endpoint shape response
bool stillCanApprove = freshStatus.Status == "Submitted" ||
    (freshStatus.Status == "Approved" && (
        (isSrSpv && freshStatus.SrSpvApprovalStatus != "Approved") ||
        (isSH && freshStatus.ShApprovalStatus != "Approved")));
if (!stillCanApprove) { /* "Deliverable sudah diproses oleh approver lain." */ }
```

**Pattern C — allApproved check + notif (T1 — MASUK approve-core)** (`:908-969`):
```csharp
var allProgresses = await _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable).ThenInclude(d => d!.ProtonSubKompetensi).ThenInclude(s => s!.ProtonKompetensi)
    .Where(p => p.CoacheeId == progress.CoacheeId)
    .ToListAsync();
var orderedProgresses = allProgresses
    .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.ProtonTrackId == trackId)
    .OrderBy(...).ThenBy(...).ThenBy(...).ToList();
RecordStatusHistory(progress.Id, approveStatusType, user.Id, user.FullName, approveActorRole);
bool allApproved = orderedProgresses.All(p => p.Status == "Approved");
await _context.SaveChangesAsync();
// notif coach + coachee COACH_EVIDENCE_APPROVED  (:935-964)  ← ApproveFromProgress saat ini TIDAK kirim ini (D-02 paritas)
if (allApproved) { await CreateHCNotificationAsync(progress.CoacheeId); }   // :966-969 ← yang HILANG (T1)
```

**Pattern D — full chain reset (target reject-core, T2)** (`RejectDeliverable:1021-1037`):
```csharp
progress.Status = "Rejected";
progress.RejectedAt = DateTime.UtcNow;
progress.RejectionReason = rejectionReason;
progress.ApprovedById = null; progress.ApprovedAt = null;
progress.SrSpvApprovalStatus = "Pending"; progress.SrSpvApprovedById = null; progress.SrSpvApprovedAt = null;
progress.ShApprovalStatus = "Pending";    progress.ShApprovedById = null;    progress.ShApprovedAt = null;
progress.HCApprovalStatus = "Pending";    progress.HCReviewedById = null;    progress.HCReviewedAt = null;  // ← HILANG di RejectFromProgress (T2)
```

**WAJIB perhatikan saat extract:**
- `RejectFromProgress:2057-2059` saat ini SET `SrSpvApprovedById/At` waktu REJECT (anomali). reject-core (pola RejectDeliverable) TIDAK set approver-id saat reject — drop perilaku ini.
- **Endpoint-specific (TETAP di endpoint, JANGAN masuk helper):** `_userManager.GetUserAsync(User)` + roles, authz `HasSectionAccess`, section-check (DIVERGEN — lihat Shared §Section-Check), `rejectionReason` empty-check, response shaping (`RedirectToAction`/`TempData` vs `Json`).
- **Pitfall response (T2):** `RejectFromProgress` saat ini return `newStatus = isSrSpv ? progress.SrSpvApprovalStatus : ...` — setelah full-reset jadi "Pending", bukan "Rejected". Return overall `Status="Rejected"` di JSON; cek consumer JS di `Views/CDP/CoachingProton.cshtml` SEBELUM finalize.

---

### `Controllers/CDPController.cs` — T5 Query "Belum Mulai" (controller, CRUD/read-aggregate)

**Analog:** `HistoriProton` (`:3138-3288`) primary; `ExportHistoriProton` (`:3290-3417`) MIRROR WAJIB (Pitfall 5 — logic ter-duplikat).

**Scoping saat ini HANYA dari ProtonTrackAssignments** (`:3147-3180`); perlu UNION coachee mapping-aktif tanpa assignment. **Status ternary 2-cabang** (`:3250`, identik di export `:3399`):
```csharp
string status = (latestHasAssessment && latestAllApproved) ? "Lulus" : "Dalam Proses";
// → jadi 3 cabang: + "Belum Mulai" untuk coachee mapping-aktif TANPA ProtonTrackAssignment
```

**View badge SUDAH 3-cabang** (`HistoriProton.cshtml:143-155`) + filter option SUDAH ada (`:79`) → tidak perlu ubah view. Definisi presisi (CONTEXT D-09): "Belum Mulai" = coachee ber-`CoachCoacheeMappings.IsActive` TANPA assignment track manapun (bukan semua user). Worker row baru pakai `HistoriProtonWorkerRow` dengan `Tahun{1,2,3}Done/InProgress = false`.

**Discretion (RESEARCH Open Q 4):** mirror manual ke `ExportHistoriProton` (minimal) vs ekstrak shared builder worker-rows (ideal, sefilosofi D-01).

---

### `Controllers/CDPController.cs` — T8 Append EvidencePathHistory (controller, file-I/O + transform)

**Analog:** `UploadEvidence` (`:1280-1287`) — pola persis yang `SubmitEvidenceWithCoaching` (`:2284-2292`) kurang.

```csharp
// UploadEvidence:1280-1287 — sisipkan SEBELUM progress.EvidencePath = ... di SubmitEvidenceWithCoaching:2290
if (!string.IsNullOrEmpty(progress.EvidencePath))
{
    var pathHistory = string.IsNullOrEmpty(progress.EvidencePathHistory)
        ? new List<string>()
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(progress.EvidencePathHistory) ?? new List<string>();
    pathHistory.Add(progress.EvidencePath);
    progress.EvidencePathHistory = System.Text.Json.JsonSerializer.Serialize(pathHistory);
}
```
Sisipkan di dalam blok `if (evidenceBytes != null && evidenceSafeFileName != null)` (`:2284`), SEBELUM `progress.EvidencePath = $"/uploads/..."` (`:2290`). **JANGAN tambah `File.Delete`** (kebijakan E10 keep-evidence).

---

### `Controllers/CoachMappingController.cs` — T3 Gate Reaktivasi (controller, request-response/authz)

**Analog:** gate existing di file ini (`:526-552`) + mirror `AssessmentAdminController:1372-1379`. Reuse `_protonCompletionService.IsPrevYearPassedAsync`.

**Loophole (cabang 1, `:516-528`) — `hasForRequestedTrack` TANPA filter IsActive → reaktivasi lolos:**
```csharp
var hasForRequestedTrack = (await _context.ProtonTrackAssignments
    .Where(a => coacheeIdsForWarning.Contains(a.CoacheeId) && a.ProtonTrackId == req.ProtonTrackId.Value)
    .Select(a => a.CoacheeId).Distinct().ToListAsync()).ToHashSet();   // ← tak filter IsActive
foreach (var coacheeId in coacheeIdsForWarning) {
    if (hasForRequestedTrack.Contains(coacheeId)) continue;   // ← T3: assignment inactive lama lolos gate
```

**Exempt existing (D-06 base) — HANYA match IsActive** (`:535-537`):
```csharp
bool isExemptFromCrossYear = await _context.ProtonTrackAssignments
    .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == requestedTrack.Id
                && a.IsActive && a.Origin == "Bypass");
if (isExemptFromCrossYear) continue;
```

**Gate predicate + error message (pola hard-block 359 D-05)** (`:542-551`):
```csharp
bool prevPassed = await _protonCompletionService
    .IsPrevYearPassedAsync(coacheeId, requestedTrack.TrackType, prevTrack.TahunKe);
if (!prevPassed) incompleteCoachees.Add(coacheeId);
// ...
return Json(new { success = false,
    message = $"Tidak bisa assign {requestedTrack.TahunKe}: {prevTrack.TahunKe} ({requestedTrack.TrackType}) belum lulus untuk {incompleteCoachees.Count} coachee." });
```

**Fix arah (D-05/D-06):** reaktivasi-candidate = coachee yg punya assignment inactive untuk track diminta (TANPA assignment aktif). Untuk candidate ini jalankan gate KECUALI inactive assignment ber-`Origin="Bypass"`:
```csharp
bool reactExempt = await _context.ProtonTrackAssignments
    .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == requestedTrack.Id && !a.IsActive && a.Origin == "Bypass");
```
**Rekomendasi lokasi (RESEARCH T3 notes / A5):** taruh gate reaktivasi di blok PRE-tx (tempat gate existing `:500-555`) → hard-block `return Json(false)` tanpa rollback. Reaktivasi terjadi di `:597-606` (INSIDE tx) — gate di PRE-tx lebih bersih. **Rekonsiliasi komentar `:531-534` "JANGAN ubah cabang 1"** (Open Q 1): perhalus cabang 1 supaya skip hanya assignment AKTIF; update komentar mencerminkan 363.

---

### `Controllers/CoachMappingController.cs` + `AssessmentAdminController.cs` — T9 Log-Warn (controller, defensive)

**Analog:** `_logger.LogWarning(...)` pola yang sudah tersebar (CoachMapping `:639/:806/:822`; sama di AssessmentAdmin). Kedua controller punya `ILogger` injected (CoachMapping `_logger` field `:17`; AssessmentAdmin `_logger` `:25`).

**Titik 1 — `CoachMappingController.cs:506-509`** (prev-track resolve):
```csharp
var prevTrack = await _context.ProtonTracks
    .Where(t => t.TrackType == requestedTrack.TrackType && t.Urutan == requestedTrack.Urutan - 1)
    .FirstOrDefaultAsync();
// T9: jika requestedTrack.Urutan > 1 && prevTrack == null → _logger.LogWarning (Urutan tidak kontigu). Gate tetap jalan.
```

**Titik 2 — `AssessmentAdminController.cs:1348-1352`** (prevTahunKe resolve):
```csharp
string? prevTahunKe = protonUrutan > 1
    ? await _context.ProtonTracks.Where(t => t.TrackType == trackType && t.Urutan == protonUrutan - 1)
        .Select(t => t.TahunKe).FirstOrDefaultAsync()
    : null;
// T9: jika protonUrutan > 1 && prevTahunKe == null → _logger.LogWarning. Tanpa throw/blok (Urutan=1 → null sah = Tahun 1).
```

---

### `Services/GradingService.cs` — T6 Drop ValidUntil Hardcode (service, transform)

**Analog (parity target):** `GradeAndCompleteAsync:282-287` — normal pass set NomorSertifikat SAJA (tidak set ValidUntil).

**Yang dibuang di `RegradeAfterEditAsync` Fail→Pass** (`:516-521`):
```csharp
var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);   // :516 ← HAPUS
var updated = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.NomorSertifikat, nomor)
        .SetProperty(r => r.ValidUntil, validUntil));            // :521 ← HAPUS SetProperty ValidUntil; sisakan NomorSertifikat saja
```
**JANGAN sentuh:** blok Pass→Fail revoke (`:479-483`, set null benar), hook bypass `:494/:496/:549` (Phase 360 ortogonal). **Catat edge case** Pass→Fail→Pass (Pitfall 6): ValidUntil bisa tetap null — keputusan D-10 terkunci, verifikasi UAT regrade flip ganda.

---

### `Services/ProtonCompletionService.cs` — T4 Surface Miss (service, CRUD + event-driven)

**Analog komposit:** (1) broadcast HC `CDPController.CreateHCNotificationAsync:1111-1140`, (2) `AuditLogService.LogAsync:21-42`, (3) registrasi template baru `NotificationService._templates` (`PROTON_BYPASS_READY:51-56` sebagai contoh entry tipe baru).

**Surface point — branch no-assignment** (`ProtonCompletionService.cs:40-44`):
```csharp
if (assignment == null)
{
    _logger.LogWarning("ProtonCompletion.EnsureAsync: tidak ada assignment aktif untuk Coachee={CoacheeId} Track={TrackId} (Origin={Origin}). Penanda tidak dibuat.", coacheeId, protonTrackId, origin);
    return false;   // ← D-08: di SINI tambah audit + notif HC (uniform untuk 4 call-site)
}
// ... :48  if (exists) return false;   ← JANGAN surface di sini (idempotent normal, Pitfall 3 false-alarm)
```

**Ctor change WAJIB** (`:23-29`) — inject 2 dep (keduanya registered scoped, no Program.cs change):
```csharp
// SEKARANG: public ProtonCompletionService(ApplicationDbContext context, ILogger<ProtonCompletionService> logger)
// JADI:     + INotificationService notificationService, + AuditLogService auditLog
```

**Audit pattern** (`AuditLogService.LogAsync:21-27` signature; contoh panggil dari controller `AssessmentAdminController:3988-3990`):
```csharp
await _auditLog.LogAsync(actorUserId: "system" /* atau session.CreatedBy */, actorName: "system/grading",
    actionType: "PROTON_PENANDA_MISS",
    description: $"Lulus exam tapi tidak ada assignment aktif — Coachee={coacheeId}, Track={protonTrackId}, Origin={origin}. Gunakan BackfillProtonPenanda.",
    targetId: protonTrackId, targetType: "ProtonTrackAssignment");
```

**Notif HC broadcast** (pola `CreateHCNotificationAsync:1115-1137` — dedup exact-message + loop HC). Di service tanpa UserManager: resolve HC via `_context.Users.Where(u => u.RoleLevel == 2 && u.IsActive)` (RESEARCH Contoh 6; CAVEAT A2 — RoleLevel denormalisasi, sumber otoritatif `GetUsersInRoleAsync(HC)`). Lalu `_notificationService.SendAsync(hcId, "PROTON_PENANDA_MISS", title, msg, "/Admin/ManageAssessment")`.

```csharp
// SendAsync signature (NotificationService.cs:108): Task<bool> SendAsync(userId, type, title, message, actionUrl=null)
// COACH_ALL_COMPLETE TIDAK terdaftar di _templates dict → dikirim via SendAsync string langsung.
// Jadi tipe baru PROTON_PENANDA_MISS boleh ikut pola SendAsync langsung (tak wajib daftar _templates).
```

**Discretion (D-08 / RESEARCH Open Q 3):** nama tipe `PROTON_PENANDA_MISS` + title/body. Notif actionable: sebut nama coachee + track + sessionId, arahkan ke BackfillProtonPenanda.

**Impact ctor change → 3 test file + DI:**
- `HcPortal.Tests/ProtonCompletionServiceTests.cs:109-110` (`NewSvc`)
- `HcPortal.Tests/ProtonYearGateIntegrationTests.cs:26-27` (`NewSvc`)
- `HcPortal.Tests/ProtonBypassServiceTests.cs:44` (di dalam `NewBypassSvc`)
- `Program.cs:57` auto-resolve (no change — dep sudah scoped).

---

### `Controllers/AssessmentAdminController.cs` — T10 Komentar By-Design (controller, doc)

**Analog:** comment-only. `BackfillProtonPenanda` (`:3909-4007`) — tambah komentar di method bahwa tanpa year-gate = by-design (tambal historis pre-358). Sudah ada header komentar `:3909-3911`; tambah catatan eksplisit "no year-gate by-design". Nol perubahan logic. Catat juga di `363-FINDINGS.md`.

---

## Shared Patterns

### Broadcast Notif ke role HC (dedup)
**Source:** `CDPController.cs:1111-1140` (`CreateHCNotificationAsync`)
**Apply to:** T1 (allApproved → notif HC, sudah reuse method ini), T4 (pola untuk notif PROTON_PENANDA_MISS di service)
```csharp
var expectedMessage = $"...{coacheeName}...";   // dedup by exact message (D-14)
var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);
foreach (var hc in hcUsers) {
    bool alreadyNotified = await _context.UserNotifications
        .AnyAsync(n => n.UserId == hc.Id && n.Type == "<TYPE>" && n.Message == expectedMessage);
    if (alreadyNotified) continue;
    await _notificationService.SendAsync(hc.Id, "<TYPE>", title, expectedMessage, "<url>");
}
```
**Catatan T4 (di service, tanpa UserManager):** resolve HC via `RoleLevel==2` (caveat A2).

### SendAsync (titik tunggal kirim notif)
**Source:** `NotificationService.cs:108-134`. Tipe baru boleh kirim string langsung (seperti COACH_ALL_COMPLETE) tanpa daftar `_templates`. Template terdaftar (contoh `PROTON_BYPASS_READY:51-56`) hanya untuk `SendByTemplateAsync`.

### AuditLogService.LogAsync
**Source:** `Services/AuditLogService.cs:21-42` (SaveChanges internal). **Apply to:** T4 (actor=system/grading), pola panggil di `AssessmentAdminController.cs:3988-3990`.

### RecordStatusHistory (deliverable)
**Source:** `CDPController.cs:3559-3571` (private, butuh `_context` saja). **Apply to:** helper approve-core/reject-core (T1/T2) — sudah dipakai kedua endpoint.

### Race-guard reload-fresh (D-10)
**Source:** `CDPController.cs:882-901`. **Apply to:** approve-core (T7 — kedua endpoint terlindungi).

### Pesan error gate generik (no info-leak)
**Source:** `CoachMappingController.cs:550-551` (hard-block 359). **Apply to:** T3 gate reaktivasi. Detail teknis → `_logger`, pesan ramah → user (preseden Phase 334/356 D6).

### Section-check DIVERGEN antar endpoint (Pitfall 1 — JANGAN masuk helper)
**Source:** `ApproveDeliverable:858-862` (Admin exempt cross-section) vs `ApproveFromProgress:1973-1974` (tanpa Admin exempt). **Keputusan planner:** pertahankan per-endpoint apa adanya (status quo low-risk) kecuali user minta unifikasi — T1/T2 tidak menyangkut section-check.

### Real-SQL disposable fixture (test)
**Source:** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61` (`ProtonCompletionFixture`) + `NewSvc:109-110`. **Apply to:** semua Wave-0 test. Tag `[Trait("Category","Integration")]` → skip via `dotnet test --filter "Category!=Integration"`. ProtonTracks di-seed migration (reuse `ctx.ProtonTracks.First...`, jangan insert). CoacheeId unik per fact (`$"prefix-{Guid.NewGuid():N}"`).

### Fake notif + predikat-replikasi (test)
**Source:** `ProtonBypassServiceTests.cs:24-47` (`FakeNotificationService` track `Sent` + `NewBypassSvc`) untuk assert notif tanpa DB; `ProtonYearGateIntegrationTests.cs:68-78` (`SkippedByCrossYearGateAsync` mereplikasi gate controller-embedded). **Apply to:** T4 (fake notif assert PROTON_PENANDA_MISS terkirim), T3 (predikat reaktivasi). Kelemahan predikat-replikasi: menguji COPY logic — kuatkan dengan ekstrak ke method testable bila memungkinkan.

---

## No Analog Found

Tidak ada. Semua fix punya analog konkret di codebase (fase "menyamakan logic existing", bukan greenfield). Catatan: untuk T1/T2/T7/T8/T5, "analog" adalah method gold-standard di FILE YANG SAMA yang divergen darinya — itulah akar drift yang fase ini bunuh.

## Metadata

**Analog search scope:** `Controllers/` (CDPController, CoachMappingController, AssessmentAdminController), `Services/` (ProtonCompletionService, GradingService, NotificationService, AuditLogService), `Views/CDP/`, `HcPortal.Tests/`, `Program.cs`.
**Files scanned:** 11 source/test files + 1 view + Program.cs DI.
**Pattern extraction date:** 2026-06-11
**Line numbers:** terverifikasi langsung dari source 2026-06-11 (sinkron dengan scout CONTEXT/RESEARCH; valid sampai CDPController/CoachMapping/Grading diubah fase lain).
