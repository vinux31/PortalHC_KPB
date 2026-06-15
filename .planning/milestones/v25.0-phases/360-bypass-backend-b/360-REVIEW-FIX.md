---
phase: 360-bypass-backend-b
fixed_at: 2026-06-11T00:26:35Z
review_path: .planning/phases/360-bypass-backend-b/360-REVIEW.md
iteration: 1
findings_in_scope: 3
fixed: 3
skipped: 0
status: all_fixed
---

# Phase 360: Code Review Fix Report

**Fixed at:** 2026-06-11T00:26:35Z
**Source review:** .planning/phases/360-bypass-backend-b/360-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 3 (fix_scope=critical_warning — WR-01, WR-02, WR-03)
- Fixed: 3
- Skipped: 0
- Verifikasi: `dotnet build` 0 error + full suite `dotnet test` **211/211 PASS** (206 baseline + 5 test baru)
- Migration: **1 migration baru** (`20260611001939_AddPendingProtonBypassActiveUniqueIndex`) — **sudah di-apply ke DB lokal** via `dotnet ef database update`. Flag migration=TRUE untuk handoff IT.

## Fixed Issues

### WR-01: Cek dobel-pending D-10 tidak race-safe — ditegakkan via filtered unique index DB

**Files modified:** `Data/ApplicationDbContext.cs`, `Services/ProtonBypassService.cs`, `Migrations/20260611001939_AddPendingProtonBypassActiveUniqueIndex.cs`, `Migrations/20260611001939_AddPendingProtonBypassActiveUniqueIndex.Designer.cs`, `Migrations/ApplicationDbContextModelSnapshot.cs`, `HcPortal.Tests/ProtonBypassServiceTests.cs`
**Commit:** `be7fbe0e`
**Applied fix:**
- Tambah filtered unique index `IX_PendingProtonBypasses_CoacheeId_ActiveUnique` pada `PendingProtonBypasses.CoacheeId` dengan filter `[Status] IN (N'Menunggu', N'Siap')` — pola `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (E15). Index non-unique `(CoacheeId, Status)` existing dipertahankan.
- Tambah `catch (DbUpdateException)` spesifik (filter nama index) di `ExecutePendingBypassAsync` dengan pesan ramah D6 "Worker sudah punya rencana bypass aktif." — rollback transaksi memastikan bare session ikut batal (tanpa yatim).
- Migration `AddPendingProtonBypassActiveUniqueIndex` dibuat via `dotnet ef migrations add` dan di-apply ke DB lokal.
- +2 test integrasi: `D10_RaceDobelPending_UniqueIndexTolakRequestKedua` (simulasi race 2 context — request kedua ditolak index, tetap 1 pending + 1 bare session) dan `D10_SetelahDibatalkan_BolehBuatPendingBaru` (filter index tidak memblokir pending Dibatalkan/Selesai).

### WR-02: `TargetUnit` kosong lolos validasi server — validasi V5 + guard defensif D-16b

**Files modified:** `Controllers/ProtonDataController.cs`, `Services/ProtonBypassService.cs`, `HcPortal.Tests/ProtonBypassServiceTests.cs`
**Commit:** `99854e7d`
**Applied fix:**
- Blok validasi V5 `BypassSave`: tambah `if (string.IsNullOrWhiteSpace(req.TargetUnit)) return Json(new { success = false, message = "Unit tujuan wajib diisi." });`
- Guard defensif di `MoveAssignmentAsync` cabang D-16b: kondisi kini `mappingLama != null && !string.IsNullOrWhiteSpace(req.TargetUnit) && (mappingLama.AssignmentUnit ?? "").Trim() != req.TargetUnit.Trim()` — melindungi semua caller service (termasuk `ConfirmBypassAsync`), `AssignmentUnit` mapping coach aktif tidak bisa tertimpa string kosong.
- +1 test integrasi: `KeepCoach_TargetUnitKosong_MappingTidakDikorupsi`.

### WR-03: Force-approve menimpa `ApprovedById`/`ApprovedAt` progress yang sudah Approved sah

**Files modified:** `Services/ProtonBypassService.cs`, `HcPortal.Tests/ProtonBypassServiceTests.cs`
**Commit:** `de8e407a`
**Applied fix:**
- Kedua loop force-approve D-13 — CL-B(a) §5.1 (`ExecuteInstantBypassAsync`) dan CL-B(b) §5.2 (`ExecutePendingBypassAsync`) — kini `foreach (var p in progresses.Where(p => p.Status != "Approved"))`. Progress yang sudah Approved sah oleh coach tidak disentuh: provenance `ApprovedById`/`ApprovedAt` utuh, tanpa history `Bypassed-AutoApprove` bising.
- +2 test integrasi: `CL_BSatuA_ForceApprove_SkipYangSudahApproved_ProvenanceUtuh` dan `CL_BSatuB_ForceApprove_SkipYangSudahApproved`.

## Skipped Issues

Tidak ada — semua temuan in-scope berhasil di-fix.

## Out-of-Scope (Info — tidak di-fix per fix_scope=critical_warning)

| ID | Judul | Catatan |
|----|-------|---------|
| IN-01 | Deteksi duplicate-key via `Message.Contains("2601"/"2627")` kondisi mati | Filter nama index tetap bekerja; catch WR-01 baru memakai nama index saja |
| IN-02 | Audit log dobel service + controller | Redundansi disengaja/didokumentasi — keputusan desain |
| IN-03 | `TempData["Warning"]` di endpoint JSON AJAX | Menunggu UI Phase 361 |
| IN-04 | `BypassList` inner-join vs `BypassPendingList` left-join inkonsisten | Kosmetik daftar Tab2 |
| IN-05 | 3 POST `[FromBody]` tanpa null-guard `req` | NRE 500 hanya pada body invalid |
| IN-06 | Re-grade Pass→Fail setelah pending "Selesai" | Spec: undo-executed = C, out of scope; masuk audit Phase 363 |
| IN-07 | Test exempt gate mereplikasi predikat controller | Usulan ekstrak helper — refactor terpisah |

## Verifikasi

- `dotnet build HcPortal.sln` — **0 error** (setelah tiap fix).
- `dotnet test` full suite — **211/211 PASS** (baseline 206 + 5 test baru; dijalankan setelah fix terakhir, mencakup ketiga fix).
- Migration `20260611001939_AddPendingProtonBypassActiveUniqueIndex` **applied ke DB lokal** (`dotnet ef database update` sukses) — build/test mencerminkan skema nyata; fixture test `MigrateAsync` membuktikan migration apply bersih di DB fresh.
- Tidak ada perubahan uncommitted tersisa (kecuali `.claude/settings.local.json` pre-existing, bukan bagian fix).

---

_Fixed: 2026-06-11T00:26:35Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
