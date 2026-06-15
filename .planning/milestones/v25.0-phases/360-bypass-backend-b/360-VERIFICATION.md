---
phase: 360-bypass-backend-b
verified: 2026-06-10T13:00:00Z
status: passed
score: 5/5 success criteria verified
overrides_applied: 0
re_verification: false
---

# Phase 360: Bypass Backend (B) — Verification Report

**Phase Goal:** Backend Bypass Tahun PROTON lengkap — tabel PendingProtonBypass + Origin (migration#2), ProtonBypassService (4 closure mode CL-A/B(a)/B(b)/C + pending lifecycle Menunggu→Siap→Selesai/Dibatalkan + confirm/cancel + hook grading), 6 endpoint /ProtonData/Bypass*, gate exempt Origin="Bypass" (cross-year skip TAPI gate 100% target-year tetap), notif PROTON_BYPASS_READY. UI penuh = Phase 361.
**Verified:** 2026-06-10T13:00:00Z
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (Roadmap Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| SC1 | Bypass CL-A/B(a)/C eksekusi instan (deactivate asal + create target + bootstrap + audit) | VERIFIED | `ExecuteInstantBypassAsync` wraps full flow in BeginTransactionAsync: deactivate source, create target Origin="Bypass", delegate `MoveAssignmentAsync` (bootstrap via `ProtonDeliverableBootstrap.CreateProgressAsync(req.TargetUnit)` + coach E15/D-16b), audit log. UAT U1/U2/U4 PASS. |
| SC2 | Bypass CL-B(b) bikin pending "Menunggu"; exam lulus → "Siap" + notif HC; konfirmasi → pindah | VERIFIED | `ExecutePendingBypassAsync` inserts PendingProtonBypass Status="Menunggu" + bare session. `MarkPendingReadyIfAnyAsync` flips ke "Siap" + notif PROTON_BYPASS_READY ke InitiatedById (hook GradingService 4 titik). `ConfirmBypassAsync` D-11+D-12+MoveAssignmentAsync. UAT U3 full lifecycle PASS. |
| SC3 | Batal pending auto-cancel exam (belum-kerjakan→hapus, sudah-lulus→pertahankan hasil) | VERIFIED | `CancelPendingAsync` §8.1: workerLulus branch — pertahankan session; !workerLulus branch — ExecuteUpdate session WHERE Status != "Completed" → "Dibatalkan" (D-14/I-03: soft-cancel "Dibatalkan" memenuhi "hapus", didokumentasikan eksplisit di 360-UAT.md). UAT U5 3-cabang PASS. |
| SC4 | Bypass exempt gate antar-tahun; coach mapping aktif lama dideactivate sebelum create baru | VERIFIED | Gate (a): AssessmentAdminController.cs:1372-1375 `isBypassAssignment` — Origin=="Bypass" exempt cross-year prereq. Gate (b): CoachMappingController.cs:535-538 `isExemptFromCrossYear` (defense-in-depth). Gate 100% target-year TIDAK tersentuh (CoacheeEligibilityCalculator utuh, D-05). Coach E15: deactivate mapping lama → flush → create baru (MoveAssignmentAsync:426-441). D-16b: update AssignmentUnit saat keep-coach+ganti-unit (:444-450). Integration tests ProtonYearGateIntegrationTests 3 test + UAT U3/U4 bootstrap verified. |
| SC5 | dotnet build 0 error + dotnet test hijau + UAT lokal:5277 | VERIFIED | Build: 0 error (confirmed SUMMARY 08). Suite: 206/206 (169 unit + 37 integration). UAT: 6/6 skenario PASS live @5277 (HTTP endpoint + SQL cross-check, AD off, login admin), user approved. |

**Score: 5/5 truths verified**

---

### Required Artifacts

| Artifact | Provides | Status | Detail |
|----------|----------|--------|--------|
| `Models/ProtonModels.cs` | class PendingProtonBypass (12 kolom, tanpa Mode) + ProtonTrackAssignment.Origin | VERIFIED | PendingProtonBypass baris 234-251; Origin (nullable nvarchar(20)) baris 85-86 ProtonTrackAssignment; Origin baris 228-229 ProtonFinalAssessment (pre-existing) |
| `Data/ApplicationDbContext.cs` | DbSet<PendingProtonBypass> + index IX_PendingProtonBypasses_CoacheeId_Status | VERIFIED | DbSet baris 45-46; HasIndex baris 423-426 |
| `Services/NotificationService.cs` | template PROTON_BYPASS_READY + deep-link tab=bypass | VERIFIED | Template baris 51-56: Title/MessageTemplate/ActionUrlTemplate |
| `Migrations/20260610094950_AddPendingProtonBypassAndAssignmentOrigin.cs` | migration#2 — 12 kolom PendingProtonBypasses + kolom Origin ProtonTrackAssignments, tanpa Sql seed | VERIFIED | Up() baris 14-47: AddColumn Origin + CreateTable PendingProtonBypasses (12 kolom, no Mode). Tanpa `migrationBuilder.Sql(...)` |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | snapshot updated PendingProtonBypass + Origin | VERIFIED (inferred) | Migration Designer file ada; SUMMARY 01 konfirmasi snapshot ter-update |
| `Helpers/ProtonDeliverableBootstrap.cs` | static CreateProgressAsync(ctx, assignmentId, trackId, coacheeId, resolvedUnit) + B-06 guard | VERIFIED | Method signature baris 24-26; resolvedUnit parameter; existingDeliverableIds guard anti-dobel |
| `Controllers/AssessmentAdminController.cs` | gate exempt isBypassAssignment (D-06a) + essay hook titik 4 | VERIFIED | isBypassAssignment baris 1372-1374; essay hook baris 3769 MarkPendingReadyIfAnyAsync; gate 100% CoacheeEligibilityCalculator utuh |
| `Controllers/CoachMappingController.cs` | isExemptFromCrossYear Origin=="Bypass" (defense-in-depth D-06b) | VERIFIED | isExemptFromCrossYear baris 535-538; komentar W-07/I-08 defense-in-depth |
| `Services/ProtonBypassService.cs` | full bypass orchestrator — validator pure, ExecuteInstant, BypassSave, ExecutePending, Confirm, Cancel, 2 hooks | VERIFIED | 641 baris: BypassValidator, ExecuteInstantBypassAsync, BypassSaveAsync, ExecutePendingBypassAsync, MoveAssignmentAsync (shared helper W-02), ConfirmBypassAsync, CancelPendingAsync, MarkPendingReadyIfAnyAsync, RevertPendingToMenungguAsync |
| `Program.cs` | AddScoped<ProtonBypassService> DI | VERIFIED | Baris 60: `builder.Services.AddScoped<HcPortal.Services.ProtonBypassService>()` |
| `Services/GradingService.cs` | inject ProtonBypassService + 3 hook grading (titik 1/2/3) | VERIFIED | Ctor inject baris 29; titik 1 baris 314; titik 2 baris 549; titik 3 baris 496; semua di dalam guard braced (W-09) |
| `Controllers/ProtonDataController.cs` | 6 endpoint Bypass* + DTO + TempData D-02 | VERIFIED | BypassList, BypassPendingList, BypassDetail (GET); BypassSave, BypassConfirm, BypassCancelPending (POST); BypassSaveRequest + BypassPendingActionRequest DTO; TempData["Warning"] baris 1629-1630 |
| `HcPortal.Tests/ProtonBypassValidationTests.cs` | unit tests validasi pure §5 (B-03 CL-A allApproved+final) | VERIFIED | 7 [Fact] + 4 [Theory] = minimal 17 exec; B-03 HasFinal test ada |
| `HcPortal.Tests/ProtonBypassServiceTests.cs` | integration tests §5.1+§5.2+confirm+cancel (19 test) | VERIFIED | 19 [Fact] [Trait("Category","Integration")] IClassFixture<ProtonCompletionFixture>; mencakup Pitfall1, B-06, D-16b, D-10, E8, D-11, D-12, W-03 |
| `HcPortal.Tests/ProtonBypassEndpointTests.cs` | reflection test Authorize+AntiForgery | VERIFIED | 2 [Fact] + 2 [Theory] = 8 xunit exec; assert class-level [Authorize(Roles="Admin,HC")] + [ValidateAntiForgeryToken] pada 3 POST mutator |
| `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` | test exempt Origin="Bypass" + gate 100% tetap + regresi | VERIFIED | Exempt_BypassOrigin_LolosCrossYear + Exempt_BypassOrigin_GateSeratusPersenTetap (D-05) + NoBypass_NormalAssignment_KeblokCrossYear |
| `.planning/phases/360-bypass-backend-b/360-UAT.md` | checklist 6 skenario UAT + hasil PASS live | VERIFIED | 6/6 skenario PASS; hasil tabel baris 84-97; catatan W-12, I-03, B-06 live |

---

### Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| `ProtonBypassService.ExecuteInstantBypassAsync` | `ProtonCompletionService.EnsureAsync` | penanda Origin=Bypass SEBELUM deactivate (Pitfall 1) | WIRED | Baris 179-181: EnsureAsync dipanggil sebelum `MoveAssignmentAsync` (yang deactivate source). Integration test CL_BSatuA_TerbitPenandaSource_SebelumDeactivate membuktikan urutan. |
| `MoveAssignmentAsync` | `ProtonDeliverableBootstrap.CreateProgressAsync` | bootstrap unit dari form (PBYP-05) | WIRED | Baris 417: dipanggil dengan `req.TargetUnit` (bukan dari mapping). Dipakai ExecuteInstant+Confirm (W-02 shared helper). |
| `MarkPendingReadyIfAnyAsync` | `INotificationService.SendByTemplateAsync` | PROTON_BYPASS_READY ke InitiatedById | WIRED | Baris 625: `SendByTemplateAsync(pending.InitiatedById, "PROTON_BYPASS_READY", ...)`. Notif ke HC, bukan worker (T-360-13). |
| `GradingService.GradeAndCompleteAsync` (titik 1) | `ProtonBypassService.MarkPendingReadyIfAnyAsync` | hook exam lulus setelah EnsureAsync | WIRED | GradingService.cs baris 314. Di dalam guard braced Category==Proton && isPassed (W-09). |
| `GradingService.RegradeAfterEditAsync` Fail→Pass (titik 2) | `ProtonBypassService.MarkPendingReadyIfAnyAsync` | re-grade lulus → flip Siap | WIRED | GradingService.cs baris 549. Braced block (W-09 konversi). |
| `GradingService.RegradeAfterEditAsync` Pass→Fail (titik 3) | `ProtonBypassService.RevertPendingToMenungguAsync` | D-15 re-grade gagal → balik Menunggu | WIRED | GradingService.cs baris 496. Braced block (W-09 konversi). |
| `AssessmentAdminController.FinalizeEssayGrading` (titik 4) | `ProtonBypassService.MarkPendingReadyIfAnyAsync` | Pitfall 2: essay early-return tak lewat hook utama | WIRED | AssessmentAdminController.cs baris 3769. Menutup celah hasEssay. |
| `ProtonDataController.BypassSave` | `ProtonBypassService.BypassSaveAsync` | delegasi + audit | WIRED | Baris 1627: `var result = await _protonBypassService.BypassSaveAsync(bypassReq)`. |
| POST endpoint (BypassSave/Confirm/CancelPending) | `[ValidateAntiForgeryToken]` | CSRF guard | WIRED | Reflection test `PostMutator_HasHttpPost_AndValidateAntiForgeryToken` + `GetReadEndpoint_Exists_NoAntiForgery` semua passed. |
| `AssessmentAdminController` gate cross-year | `ProtonTrackAssignments.Origin == "Bypass"` | exempt prereq antar-tahun (D-06a) | WIRED | Baris 1372-1375: isBypassAssignment AnyAsync. Integration test Exempt_BypassOrigin_LolosCrossYear. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `ProtonBypassService.ExecuteInstantBypassAsync` | source assignment, sourceComplete, sourceHasFinal | `_context.ProtonTrackAssignments` / `ProtonDeliverableProgresses` / `ProtonFinalAssessments` (real EF queries) | Ya — EF LINQ queries DB nyata; integration tests real-SQL confirm | FLOWING |
| `ProtonBypassService.MarkPendingReadyIfAnyAsync` | pending record, workerName | `PendingProtonBypasses` + `Users` | Ya — ExecuteUpdateAsync atomik flip + notif ke real HC userId | FLOWING |
| `ProtonDataController.BypassDetail` | sourceComplete, sourceHasFinal, eligibleModes | `ProtonTrackAssignments` + `ProtonDeliverableProgresses` + `ProtonFinalAssessments` | Ya — LINQ queries per coacheeId; eligibleModes B-03 (CL-A hanya jika sourceComplete && sourceHasFinal) | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Verification | Result | Status |
|----------|-------------|--------|--------|
| CL-A instan pindah + gate lolos | UAT U1 live @5277: SQL cross-check source inactive + target Origin='Bypass' + gate CreateAssessment pass | PASS | PASS |
| CL-B(b) pending + bare session UserId+AssessmentType | UAT U3 SQL: session 163 UserId=worker + AssessmentType='Standard' + no package | PASS | PASS |
| Exam lulus → pending Siap + notif HC | UAT U3 step 5: pending 'Siap' + UserNotification HC PROTON_BYPASS_READY (worker tidak dapat) | PASS | PASS |
| Confirm → bootstrap target ber-deliverable | UAT U3 step 6: target punya 2 ProtonDeliverableProgress (W-02) | PASS | PASS |
| Batal pending soft-cancel (D-14) | UAT U5a: session 'Dibatalkan'; U5b: session DIPERTAHANKAN; U5c: Completed tetap Completed (W-03) | PASS | PASS |
| Re-grade fail → revert D-15 | UAT U6: SubmitEditAnswers → Score 100→0 → pending Siap→Menunggu + penanda Exam dihapus | PASS | PASS |
| 206/206 full suite hijau | `dotnet test` result per SUMMARY 08 | 206/206 passed | PASS |

---

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|---------------|-------------|--------|----------|
| PBYP-01 | 01, 04 | Tabel PendingProtonBypass (migration#2) + lifecycle Menunggu→Siap→Selesai/Dibatalkan | SATISFIED | Migration 20260610094950 ada 12 kolom; model PendingProtonBypass; DbSet; lifecycle diimplementasi 4 method service; 19 integration test. REQUIREMENTS.md checkbox masih [ ] — artifact dokumentasi (tidak diupdate), bukan gap kode. |
| PBYP-02 | 01, 02, 03, 04, 05, 08 | 4 closure mode CL-A/B(a)/B(b)/C + validasi |Δtahun|≤1 + 1-assignment-aktif | SATISFIED | BypassValidator.Validate (pure), ExecuteInstantBypassAsync (CL-A/B(a)/C), ExecutePendingBypassAsync (CL-B(b)); E8 B-04 di BypassSaveAsync + ExecuteInstant; 17+ test validasi + integration. |
| PBYP-03 | 01, 04, 06 | Exam CL-B(b) lulus → pending "Siap" + notif PROTON_BYPASS_READY ke HC (GradingService hook) | SATISFIED | Template PROTON_BYPASS_READY; MarkPendingReadyIfAnyAsync; 4 hook titik (GradingService 1/2/3 + AssessmentAdminController essay 4); UAT U3+U6 E2E. |
| PBYP-04 | 02, 03 | Coach: deactivate mapping aktif lama → create baru (E15); dropdown ganti coach | SATISFIED | MoveAssignmentAsync baris 426-441: deactivate endDate + flush + create baru StartDate+AssignmentSection (W-13). D-16b: update AssignmentUnit saat keep-coach. Integration test Coach_DeactivateLamaCreateBaru_E15 + KeepCoach_GantiUnit. REQUIREMENTS.md checkbox [ ] = doc artifact. |
| PBYP-05 | 02, 03 | Bootstrap deliverable target pakai Unit dari form bypass (bukan dari mapping) | SATISFIED | MoveAssignmentAsync baris 417-418: `CreateProgressAsync(_context, ..., req.TargetUnit)`. Integration test Bootstrap_PakaiUnitForm_BukanMapping. REQUIREMENTS.md checkbox [ ] = doc artifact. |
| PBYP-06 | 04, 05 | HC batal pending sebelum pindah (auto-cancel exam: belum-dikerjakan→hapus/Dibatalkan, sudah-lulus→pertahankan hasil) | SATISFIED | CancelPendingAsync §8.1: dua branch workerLulus/!workerLulus + W-03 guard Completed-gagal; D-14 soft-cancel="Dibatalkan" dinyatakan eksplisit di 360-UAT.md (I-03). 6 integration test cancel branch. |
| PBYP-07 | 07 | 6 endpoint bypass [Authorize(Admin,HC)] + AntiForgery 3 POST + audit | SATISFIED | BypassList/BypassPendingList/BypassDetail (GET) + BypassSave/BypassConfirm/BypassCancelPending (POST); class-level [Authorize]; [ValidateAntiForgeryToken] 3 POST; audit LogAsync per mutator; reflection test ProtonBypassEndpointTests 8 exec. |

**Orphaned requirements check:** PBYP-08, PBYP-09, PBYP-10 terdaftar di REQUIREMENTS.md untuk Phase 361 (UI penuh) — bukan untuk Phase 360. Tidak ada orphaned requirement.

---

### Anti-Patterns Found

| File | Issue | Severity | Impact |
|------|-------|----------|--------|
| `Services/ProtonBypassService.cs:252-256` | D-10 dobel-pending cek di luar transaksi — 2 request konkuren bisa lolos (WR-01) | Warning | Jika HC dobel-klik BypassSave CL-B(b), bisa menghasilkan 2 pending + 2 bare session zombie. Perbaikan: filtered unique index DB + catch DbUpdateException di ExecutePending. Advisory, tidak blocking Phase 360. UI Phase 361 bisa mitigasi via disable-after-submit. |
| `Controllers/ProtonDataController.cs:1615-1622` | TargetUnit tidak divalidasi non-kosong (WR-02) | Warning | TargetUnit="" lolos validasi V5 → bootstrap di-skip + cabang D-16b mengkorup AssignmentUnit mapping. Perbaikan: tambah `string.IsNullOrWhiteSpace(req.TargetUnit)` guard. Advisory. |
| `Services/ProtonBypassService.cs:159-177, 303-321` | Force-approve menimpa ApprovedById/ApprovedAt pada progress yang sudah Approved sah (WR-03) | Warning | Provenance approval coach bisa tertimpa. Perbaikan: filter `progresses.Where(p => p.Status != "Approved")`. Advisory. |

Catatan: 0 critical findings (per 360-REVIEW.md). 3 warning di atas bersifat advisory — tidak memblokir phase goal. Tidak ada TODO/FIXME/placeholder yang menghambat fungsi.

---

### Human Verification

Sudah selesai. 6 skenario UAT (U1-U6) PASS live @5277 (HTTP endpoint + SQL cross-check, AD off, login admin@pertamina.com). User approved. Hasil terdokumentasi di `.planning/phases/360-bypass-backend-b/360-UAT.md` baris 84-97.

Tidak ada item yang perlu verifikasi manusia tambahan.

---

## Gaps Summary

Tidak ada gap yang memblokir pencapaian tujuan phase. Semua 5 Success Criteria roadmap terverifikasi. Semua PBYP-01..07 diimplementasi di kode.

**Catatan dokumentasi (bukan gap kode):** REQUIREMENTS.md masih menampilkan [ ] (belum dicentang) untuk PBYP-01, PBYP-04, PBYP-05. Ini artifact dokumentasi — implementasi ketiga requirement sudah jelas ada di kode (model PendingProtonBypass, MoveAssignmentAsync coach E15+D-16b, CreateProgressAsync dengan TargetUnit). Disarankan update centang di sesi berikutnya.

**Advisory untuk Phase 361/363:** WR-01 (race D-10), WR-02 (TargetUnit validation), WR-03 (force-approve scope) dan IN-06 (re-grade setelah Selesai) didokumentasikan di 360-REVIEW.md sebagai backlog teknis.

---

_Verified: 2026-06-10T13:00:00Z_
_Verifier: Claude (gsd-verifier)_
