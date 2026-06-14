---
phase: 360-bypass-backend-b
asvs_level: L1
threats_total: 35
threats_closed: 35
threats_open: 0
audited: 2026-06-11
auditor: gsd-security-auditor
---

# SECURITY.md — Phase 360 Bypass Backend (B)

## Result: SECURED

All 35 threats closed. 0 open threats.

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-360-01 | Tampering | mitigate | CLOSED | `Migrations/20260610094950_...cs:13-47` — hanya AddColumn nullable + CreateTable, zero `migrationBuilder.Sql("UPDATE…")`. Origin nullable tanpa backfill. |
| T-360-02 | DoS | mitigate | CLOSED | `docs/SEED_JOURNAL.md:166` — snapshot `C:\Temp\HcPortalDB_Dev_pre360migration_20260610.bak` status=active tercatat sebelum `dotnet ef database update`. |
| T-360-03 | Repudiation | mitigate | CLOSED | `Migrations/ApplicationDbContextModelSnapshot.cs:1419-1475` — entity `PendingProtonBypass` + index `IX_PendingProtonBypasses_CoacheeId_ActiveUnique` + `IX_PendingProtonBypasses_CoacheeId_Status` ter-update. |
| T-360-04 | EoP | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:1372-1374` — `isBypassAssignment` cek `IsActive && Origin=="Bypass"` per uid+track. Gate 100% deliverable `:1380-1396` tidak disentuh. |
| T-360-05 | Tampering | mitigate | CLOSED | `Helpers/ProtonDeliverableBootstrap.cs:38` — filter `.Unit!.Trim() == resolvedUnit.Trim()` 2-sisi identik gate 100%. Integration test `ProtonBypassServiceTests` mengunci unit-from-form. |
| T-360-06 | Tampering | accept | CLOSED | Gate Phase 359 tidak dihapus; hanya `!isBypassAssignment` ditambah via `&&` kondisi. Integration test `ProtonYearGateIntegrationTests.NoBypass_NormalAssignment_KeblokCrossYear` memverifikasi regresi. |
| T-360-07 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:101` `ExecuteInstantBypassAsync` — `await using var tx = await _context.Database.BeginTransactionAsync()`. `ExecutePendingBypassAsync:274` dan `ConfirmBypassAsync:480` juga wrapped tx. |
| T-360-08 | Info Disclosure | mitigate | CLOSED | `Services/ProtonBypassService.cs:213` catch generic — `return new BypassResult(false, "Gagal eksekusi bypass. Operasi dibatalkan.")` tanpa `ex.Message`. `_logger.LogError(ex, ...)` ke logger saja. |
| T-360-09 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:182-183` — `EnsureAsync(...)` dipanggil SEBELUM `MoveAssignmentAsync` (deactivate source). Komentar `// (1) [tutup tahun asal] — SEBELUM deactivate (Pitfall 1)`. |
| T-360-10 | EoP | mitigate | CLOSED | `Services/ProtonBypassService.cs:249` `BypassSaveAsync` — `BypassValidator.Validate(...)` dipanggil di awal setelah E8. `Controllers/ProtonDataController.cs:1616-1626` V5 server-side sebelum delegasi. |
| T-360-31 | Tampering | mitigate | CLOSED | `Helpers/ProtonDeliverableBootstrap.cs:55-65` — `existingDeliverableIds` guard B-06 anti-dobel; progress CL-C turun: skip deliverable yang sudah ada → count tetap N. |
| T-360-11 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:626-655` — `MarkPendingReadyIfAnyAsync` dan `RevertPendingToMenungguAsync` TANPA `BeginTransactionAsync`. |
| T-360-12 | Tampering | mitigate | CLOSED | App-level D-10 `Services/ProtonBypassService.cs:256-258`; DB-level WR-01 `Migrations/20260611001939_...cs:13-18` filtered unique index `IX_PendingProtonBypasses_CoacheeId_ActiveUnique`. Catch `DbUpdateException` `ExecutePendingBypassAsync:378-384`. |
| T-360-13 | Info Disclosure | mitigate | CLOSED | `Services/ProtonBypassService.cs:641` — `SendByTemplateAsync(pending.InitiatedById, "PROTON_BYPASS_READY", ...)` ke HC inisiator (bukan worker). |
| T-360-14 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:274` `ExecutePendingBypassAsync` — `await using var tx = await _context.Database.BeginTransactionAsync()`. Catch generic `:388-390` tanpa `ex.Message`. |
| T-360-32 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:226-231` `BypassSaveAsync` — `activeCount != 1` tolak di entry SEBELUM dispatch semua mode. Integration test `E8_DobelAssignment_Tolak` dalam `ProtonBypassServiceTests`. |
| T-360-33 | DoS | mitigate | CLOSED | `Services/ProtonBypassService.cs:331-333` — `UserId = req.CoacheeId` (B-05/W-06) dan `AssessmentType = "Standard"` (B-05) eksplisit pada bare session. Komentar `// B-05: kolom DB NOT NULL`. |
| T-360-15 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:509-517` `ConfirmBypassAsync` — `ExecuteUpdateAsync WHERE Status="Siap"` + `rows == 0` guard (D-12). |
| T-360-16 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:490-506` — D-11 re-check `sourceStillActive`, `examPassed`, `penandaExamAda` SEBELUM `ExecuteUpdateAsync`. |
| T-360-17 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:406-409` `MoveAssignmentAsync` — `&& (excludeSessionId == null || s.Id != excludeSessionId)` EXCLUDE `LinkedAssessmentSessionId` dari cancel-exam. |
| T-360-18 | Info Disclosure | mitigate | CLOSED | `Services/ProtonBypassService.cs:550` `ConfirmBypassAsync` catch — `return new BypassResult(false, "Gagal konfirmasi bypass. Operasi dibatalkan.")` tanpa `ex.Message`. `:614` `CancelPendingAsync` sama. |
| T-360-34 | Tampering | mitigate | CLOSED | `Services/ProtonBypassService.cs:597-599` `CancelPendingAsync` — `ExecuteUpdateAsync WHERE s.Id == linkedId && s.Status != "Completed"` (W-03 guard). |
| T-360-19 | DoS | mitigate | CLOSED | `Services/ProtonBypassService.cs:626-643` `MarkPendingReadyIfAnyAsync` — TANPA tx, no-throw; `RevertPendingToMenungguAsync:650-655` sama. |
| T-360-20 | Tampering | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:3767-3769` — `MarkPendingReadyIfAnyAsync(session.Id)` di titik 4 FinalizeEssayGrading setelah `EnsureAsync` (Pitfall 2 essay path). |
| T-360-21 | Tampering | accept | CLOSED | DI satu arah: `Services/GradingService.cs:22,29` inject `ProtonBypassService`; `ProtonBypassService.cs:69-88` ctor TANPA GradingService. Build 0 error memverifikasi no circular. |
| T-360-35 | Tampering | mitigate | CLOSED | `Services/GradingService.cs:492` titik 3 — `if (session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue) {` braced (W-09). `:543` titik 2 sama braced. Hook dijamin dalam guard Proton. |
| T-360-22 | EoP | mitigate | CLOSED | `Controllers/ProtonDataController.cs:97` — `[Authorize(Roles = "Admin,HC")]` class-level men-gate semua 6 endpoint. Reflection test `ProtonBypassEndpointTests.ProtonDataController_ClassLevel_AuthorizeAdminHC` pass. |
| T-360-23 | CSRF | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1609,1652,1670` — `[ValidateAntiForgeryToken]` pada BypassSave, BypassConfirm, BypassCancelPending. Reflection test `PostMutator_HasHttpPost_AndValidateAntiForgeryToken` (3 theory) pass. |
| T-360-24 | Tampering | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1616-1626` V5: `CoacheeId`, `Reason`, `TargetUnit` (WR-02), mode whitelist server-side. Service `BypassValidator.Validate` tambahan layer di `:249`. |
| T-360-25 | Info Disclosure | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1640-1641` komentar `// D6: result.Message dari service (ramah, tanpa ex.Message)`. Service tidak expose `ex.Message` ke return value. |
| T-360-26 | Repudiation | mitigate | CLOSED | `Controllers/ProtonDataController.cs:1636-1638,1661-1663,1679-1681` — `_auditLog.LogAsync(...)` per mutator BypassSave/BypassConfirm/BypassCancelPending. Service juga audit di `:190,366,527,603`. |
| T-360-27 | SQLi | accept | CLOSED | EF Core LINQ parameterized — no raw SQL concat di bypass region. Tidak ada `_context.Database.ExecuteSqlRaw(...)` dalam service/controller bypass. |
| T-360-28 | Tampering | mitigate | CLOSED | `HcPortal.Tests/ProtonYearGateIntegrationTests.cs:81-161` — 3 integration test: `Exempt_BypassOrigin_LolosCrossYear`, `Exempt_BypassOrigin_GateSeratusPersenTetap` (D-05), `NoBypass_NormalAssignment_KeblokCrossYear` (regresi). |
| T-360-29 | DoS | mitigate | CLOSED | `docs/SEED_JOURNAL.md:166` — snapshot pre-migration `cleaned=active`; `:167` UAT seed `cleaned` (RESTORE WITH REPLACE 1954 pages verified). SEED_WORKFLOW diikuti. |
| T-360-30 | EoP | accept | CLOSED | UAT menggunakan akun admin/HC sah. Endpoint diverifikasi auth `[Authorize(Roles="Admin,HC")]` oleh reflection test plan 07. |

---

## Accepted Risks Log

| Threat ID | Rationale |
|-----------|-----------|
| T-360-06 | Gate Phase 359 tidak dihapus — kondisi `!isBypassAssignment` ditambah via `&&`, sehingga logika gate existing dipertahankan utuh. Regression test mengunci. |
| T-360-21 | DI satu arah GradingService→ProtonBypassService terbukti via build 0 error; circular DI tidak mungkin secara struktural karena ProtonBypassService ctor tidak menerima GradingService. |
| T-360-27 | EF Core parameterized LINQ — tidak ada raw SQL concat di seluruh bypass region. Risiko SQLi ditransfer ke framework ORM yang sudah mature. |
| T-360-30 | UAT scope: endpoint hanya dapat diakses oleh role Admin/HC yang sudah terautentikasi. Bypass akun melalui UAT tidak relevan karena test menggunakan kredensial sah. |

---

## Unregistered Flags (dari SUMMARY.md ## Threat Flags)

Tidak ada unregistered threat flag. Semua deviasi yang dilaporkan di SUMMARY.md (360-01..08) bersifat implementasi atau test-seed — tidak ada attack surface baru yang tidak terpetakan ke threat register.

Catatan informatif dari SUMMARY.md:
- **360-08 observasi non-blocking**: `BypassValidator` D-B pakai `ProtonTrack.Urutan` global untuk |Δ|≤1 — semantik benar dalam satu TrackType tapi berperilaku tak terduga lintas TrackType. Dicatat sebagai rekomendasi UI Phase 361 (batasi dropdown target ke TrackType sama), bukan threat baru.

---

## Test Coverage (bypass-related)

| Suite | Count | Scope |
|-------|-------|-------|
| ProtonBypassValidationTests (unit) | 17 | BypassValidator pure §5 (B-03, D-D, E14, D-B, mode whitelist, alasan) |
| ProtonBypassServiceTests (integration) | 19 | ExecuteInstant + ExecutePending + MarkReady/Revert + ConfirmBypass + CancelPending |
| ProtonBypassEndpointTests (reflection) | 8 | Authorize class-level, AntiForgery 3 POST, GET count 3 |
| ProtonYearGateIntegrationTests (integration) | 4 | Exempt Bypass lolos cross-year, gate 100% tetap, normal keblok, prevYear |
| **Total bypass-related** | **48** | — |
| Full suite | 206/206 | Build 0 error |
