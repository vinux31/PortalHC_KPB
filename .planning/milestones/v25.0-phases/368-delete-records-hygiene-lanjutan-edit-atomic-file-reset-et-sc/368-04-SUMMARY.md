---
phase: 368-delete-records-hygiene-lanjutan
plan: 04
subsystem: models
tags: [cert-dedup, groupby-helper, anti-drift, uat]
requires:
  - "TrainingAdminController CleanupAttemptHistory + label (368-02)"
provides:
  - "SertifikatRow.BuildParentNameLookup GroupBy-dedup helper (#25)"
  - "CMP + CDP CertificationManagement konsumsi helper shared (#25)"
  - "UAT sign-off fase 368 (#23 browser + #27 label + #25 no-500)"
affects:
  - Models/CertificationManagementViewModel.cs
  - Controllers/CMPController.cs
  - Controllers/CDPController.cs
tech-stack:
  added: []
  patterns: ["static shared helper di lokasi netral (Models) dikonsumsi controller non-AdminBase"]
key-files:
  created:
    - HcPortal.Tests/CertDedupTests.cs
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/CMPController.cs
    - Controllers/CDPController.cs
key-decisions:
  - "Helper di SertifikatRow (Models) — home netral; CMP/CDP plain Controller tak inherit AdminBase (D-03)"
  - "Callsite project anon-type {Id,Name,ParentId} → tuple sebelum panggil helper (anon tak lintas-method)"
  - "UAT diverifikasi developer via Playwright MCP (user mendelegasikan) — 3 perilaku PASS; 2 temuan minor + 1 pre-existing dicatat"
requirements-completed: ["#25", "#23", "#27"]
duration: "~40 min (incl UAT browser + Seed Workflow)"
completed: 2026-06-13
---

# Phase 368 Plan 04: CertificationManagement Dedup Helper (#25) + UAT Sign-Off Summary

Ekstrak GroupBy dedup ke `SertifikatRow.BuildParentNameLookup` (anti-drift, dikonsumsi CMP+CDP) + UAT browser end-to-end fase 368.

**Tasks:** 3/3 (T1 helper+test, T2 rewire, T3 UAT checkpoint) | **Files:** 1 created + 3 modified | **Tests:** 3 [Fact] unit + full suite 306/306

## What was built

- **#25 helper:** `SertifikatRow.BuildParentNameLookup(IEnumerable<(int Id, string Name, int? ParentId)>)` — GroupBy(Name)-dedup ganti `ToDictionary(c=>c.Name)` yang `throw ArgumentException` (500) pada duplicate child Name lintas parent. Home NETRAL di `Models/CertificationManagementViewModel.cs` (CMP/CDP plain `Controller`, bukan AdminBase). CMP + CDP callsite rewire konsumsi helper SAMA (single-source). 3 [Fact]: duplicate-tidak-throw, normal-map, root-empty.

## Verification (otomatis)

- `dotnet build` 0 error; `--filter "ParentNameLookup"` 3/3; quick suite 215/215; **full suite 306/306** (215 quick + 91 integration real-SQL).
- Greps: helper di SertifikatRow; CMP+CDP konsumsi `SertifikatRow.BuildParentNameLookup` (×1 masing-masing); `ToDictionary(c=>c.Name` = 0; inline `GroupBy(c=>c.Name)` di CMP/CDP = 0. Migration=FALSE.

## UAT (Task 3 — diverifikasi developer via Playwright MCP @5277, AD off; user delegasi)

**Seed Workflow:** snapshot `C:\Temp\HcPortalDB_Dev_pre368uat.bak` (1970 pages) → seed → RESTORE WITH REPLACE → journal `cleaned`.

- **#23 cleanup orphan — PASS (end-to-end + DB):** seed 3 orphan `AssessmentAttemptHistory` SessionId dangling (999990-992, UserId admin). `GET /Admin/CleanupAttemptHistory` → preview **"Ditemukan 3 baris orphan"** + tombol "Hapus 3 Orphan". Klik → confirm dialog → POST execute → flash **"Success: Selesai: 3 orphan AttemptHistory dihapus."** → re-render idempotent **"Tidak ada orphan — DB bersih"** (tombol hilang). DB cross-check: orphan=0, TOTAL_AH=5 (non-orphan utuh, **no over-delete**), audit `CleanupAttemptHistory` ter-persist. Restore → pristine (AH=5, UAT368=0, audit=0).
- **#27 label — PASS:** `/Admin/BulkBackfill` title+breadcrumb+h2+subtitle = **"Bulk Import Nilai (Excel)"**; Admin Index card = "Bulk Import Nilai (Excel)".
- **#25 no-500 — PASS (path nyata):** `/CDP/CertificationManagement` (entry point asli via `Url.Action(...,"CDP")`) render OK, 8 sertifikat, **0 console error, no 500/ArgumentException** — dedup helper jalan.

## Temuan UAT (dicatat — NON-blocking)

1. **[Minor, di luar scope plan] Residu kata "Backfill"/"Restore":** label heading sudah benar, tapi masih ada wording lama di: tombol **"Execute Backfill"** (BulkBackfill.cshtml), `<small>` subtitle card Admin Index **"Pulihkan AssessmentSession... emergency restore tool"**, dan default Audit Tag **"ManualImport-Backfill"**. Plan #27 acceptance = heading/breadcrumb/h2/card-label saja (semua PASS); item ini tak di-spec. Kandidat polish ringan bila ingin label 100% konsisten.
2. **[PRE-EXISTING, BUKAN regresi 368] `/CMP/CertificationManagement` → 500 "view 'CertificationManagement' not found":** `Views/CMP/CertificationManagement.cshtml` tidak ada (hanya `Views/CDP/...` yang ada). CMP action = duplikat orphaned ("dipindah dari CDP" per komentar); link asli (`Views/CMP/Index.cshtml:98`) route ke **CDP**, bukan CMP. Perubahan #25 = 1 baris LINQ (tak bisa sebabkan view-not-found); `git diff 358eb610..HEAD` = **0 .cshtml** disentuh. Callsite #25 CMP tetap ter-eksekusi (capai `return View` tanpa ArgumentException). **Kandidat backlog:** hapus action CMP.CertificationManagement orphaned ATAU tambah view redirect → CDP.
3. **[Lesson lokal] `dotnet run --no-launch-profile` → environment Production** → pakai `appsettings.json` (conn string placeholder `YOUR_SQL_SERVER_NAME`) → DB error. WAJIB set `ASPNETCORE_ENVIRONMENT=Development` agar `appsettings.Development.json` (HcPortalDB_Dev) terpakai.

## Self-Check: PASSED

- Helper GroupBy-dedup di SertifikatRow ✓; CMP+CDP konsumsi shared, 0 ToDictionary(c=>c.Name), 0 inline GroupBy ✓.
- UAT 3/3 PASS browser-verified (SC #23 idempotent DB-verified + #27 label + #25 no-500) ✓; Seed Workflow journal cleaned ✓.
- build 0 err; full suite 306/306; Migration=FALSE ✓.

**Phase 368 (4/4 plan) COMPLETE — 7 temuan hygiene #21-27 ditutup: edit atomic file, reset ET cleanup, orphan cleanup endpoint, import audit, cert dedup, renewal validation, BulkBackfill label.**
