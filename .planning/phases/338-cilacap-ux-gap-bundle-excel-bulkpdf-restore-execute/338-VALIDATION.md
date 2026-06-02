---
phase: 338
slug: cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
status: partial
nyquist_compliant: false
wave_0_complete: true
created: 2026-06-02
mode: state-b-reconstruction
shipped_commit: f359a745
verification_ref: 338-VERIFICATION.md
---

# Phase 338 — Validation Strategy

> State B reconstruction — Phase sudah SHIPPED LOCAL (2026-05-30, commit f359a745).
> VALIDATION.md ini dibuat retroaktif untuk memetakan coverage aktual dan gap yang tersisa.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --no-build` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~115 ms (18 test baseline) |
| **E2E framework** | Playwright (MCP — Playwright MCP browser automation) |
| **E2E spec dir** | `tests/e2e/` — tidak ada spec Cilacap-specific (gap) |

---

## Sampling Rate

- **After every task commit:** `dotnet test --no-build` (18 baseline harus PASS)
- **After every plan wave:** `dotnet test` (full suite)
- **Before `/gsd-verify-work`:** Full suite must be green + VERIFICATION.md code-checks PASS
- **Max feedback latency:** ~5 detik (dotnet test --no-build runtime 114 ms)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | Coverage Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-----------------|
| 338-01-T1 | 01 | 1 | CIL-02 | T-338-01-01 | Guard `&& string.IsNullOrEmpty(search)` — Closed tidak disembunyikan saat search non-empty | unit/grep | `dotnet build --no-restore` | ✅ green (code-verified L205) |
| 338-01-T1b | 01 | 1 | CIL-01 | T-338-01-03 | ViewBag.OpenCount/UpcomingCount/ClosedCount di-set SEBELUM filter apply | unit/grep | `dotnet build --no-restore` | ✅ green (code-verified L199-201 + L2835-2837) |
| 338-01-T2 | 01 | 1 | CIL-01 | T-338-01-05 | Badge render di `_AssessmentGroupsTab.cshtml` + cil01-pulse CSS | manual-browser | N/A — butuh Dev DB data Cilacap | ⚠️ manual-only |
| 338-01-T3 | 01 | 1 | CIL-01 | T-338-01-05 | Badge render di `AssessmentMonitoring.cshtml` + cil01-pulse CSS | manual-browser | N/A — butuh Dev DB data Cilacap | ⚠️ manual-only |
| 338-02-T1 | 02 | 2 | CIL-03 | T-338-02-01 | `AllWorkersHistoryRow.SessionId int?` field additive non-breaking | unit/grep | `dotnet build --no-restore` | ✅ green (field ada di Models) |
| 338-02-T2 | 02 | 2 | CIL-03 | T-338-02-02 | Row clickable `data-href=/CMP/Results/{sessionId}` + JS handler + a11y | manual-browser | N/A — butuh Dev DB riil | ⚠️ manual-only (UAT lokal 32 row PASS) |
| 338-02-T3 | 02 | 2 | CIL-04 | T-338-02-03 | Banner role-gated: Admin/HC → admin history; worker → Records | manual-browser | N/A — butuh login worker di Dev | ⚠️ manual-only (Admin variant Playwright PASS; worker code-verified) |
| 338-03-T1 | 03 | 3 | CIL-05 | T-338-01 | `AddDetailPerSoalSheet` static helper ada di ExcelExportHelper.cs | unit/grep | `dotnet build --no-restore` | ✅ green (L47 present) |
| 338-03-T2 | 03 | 3 | CIL-05 | T-338-01 | `AddElemenTeknisSheet` static helper ada di ExcelExportHelper.cs | unit/grep | `dotnet build --no-restore` | ✅ green (L139 present) |
| 338-03-T3 | 03 | 3 | CIL-05 | T-338-01 | 2 helper call terintegrasi di `ExportAssessmentResults` L4296-4297 | unit/grep | `dotnet build --no-restore` | ✅ green (call wired) |
| 338-03-UAT | 03 | 3 | CIL-05 | — | xlsx 4 sheet order: Summary/Detail Per Soal/Elemen Teknis/per-peserta; Avg math benar | manual-browser | N/A — butuh Excel app nyata | ⚠️ manual-only (JSZip UAT lokal PASS; Excel app verify pending) |
| 338-04-T1 | 04 | 4 | CIL-06 | T-338-02 | `BulkExportPdf` endpoint ada di AssessmentAdminController.cs L4499 | unit/grep | `dotnet build --no-restore` | ✅ green (endpoint present + DoS guard max 50) |
| 338-04-T1b | 04 | 4 | CIL-06 | T-338-02 | curl GET BulkExportPdf → 200 + application/zip + %PDF signature valid | smoke/curl | `curl "http://localhost:5277/Admin/BulkExportPdf?title=UAT+v14+Standard&category=OJT&scheduleDate=2026-04-10" --cookie "..." -o bundle.zip -s -w "%{http_code}"` | ✅ green (UAT lokal curl PASS: 200 + 76544 bytes + %PDF) |
| 338-04-T2 | 04 | 4 | REST-04 | T-338-03 | `BulkBackfillAssessment` POST endpoint ada, tx atomic, AuditLog per row | unit/grep | `dotnet build --no-restore` | ✅ green (L728-861 present, build PASS) |
| 338-04-T2b | 04 | 4 | REST-04 | T-338-03 | Data restore 13 peserta Cilacap PreTest 30 Mar 2026 via upload Excel | manual-execute | N/A — WAJIB di Dev DB (KRITIS) | ⚠️ manual-only (defer ke IT promo) |
| 338-05-T1 | 05 | 5 | REST-05 | T-338-04 | `docs/templates/DB_HANDOFF_IT.template.md` exist + 6 section valid | smoke/grep | `dotnet build --no-restore` + file exist check | ✅ green (108 LOC, 6 section verified) |
| 338-05-T1b | 05 | 5 | REST-05 | T-338-04 | `scripts/backup-dev-pre-migration.ps1` exist, no hardcoded credential | smoke/grep | file exist + grep credential | ✅ green (90 LOC, Windows Auth -E, no hardcode) |
| 338-05-T2 | 05 | 5 | REST-06 | T-338-06 | `TryAutoDetectCounterpartGroup` helper L6599 + auto-pair call L839 | unit/grep | `dotnet build --no-restore` | ✅ green (count=2: call + definition) |
| 338-05-T3 | 05 | 5 | REST-07 | — | Section `Pre-Deploy Backup SOP` ada di `docs/DEV_WORKFLOW.md` | smoke/grep | `grep "Pre-Deploy Backup SOP" docs/DEV_WORKFLOW.md` | ✅ green (L142 present) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ manual-only (not automated)*

---

## Wave 0 Requirements

Infrastruktur test sudah ada sebelum Phase 338 dimulai. Tidak ada Wave 0 setup yang diperlukan.

- [x] `HcPortal.Tests/` — 18 baseline test suite (xUnit) sudah tersedia
- [x] `dotnet test` runner dikonfigurasi via `HcPortal.Tests.csproj`
- [x] Playwright MCP browser automation tersedia untuk UAT manual

**Existing infrastructure covers all phase requirements** — tidak ada penambahan test file selama Phase 338.

**Gap Cilacap E2E spec:** `tests/e2e/Cilacap.spec.ts` TIDAK dibuat selama Phase 338. CIL-01..04 verified via Playwright MCP interaktif (bukan spec file permanen). Gap ini dikategorikan **Manual-Only** karena memerlukan data Cilacap yang hanya ada di Dev DB.

---

## Manual-Only Verifications

*7 item dari 338-VERIFICATION.md human verification section.*

| # | Behavior | Requirement | Why Manual | Test Instructions |
|---|----------|-------------|------------|-------------------|
| M-01 | Badge counter Open/Upcoming/Closed tampil akurat di `/Admin/ManageAssessment` + `/Admin/AssessmentMonitoring` di Dev environment | CIL-01 | Data Cilacap (Closed sessions) hanya ada di Dev DB, bukan lokal. UAT lokal PASS dengan seed temp (sudah di-clean). | Login sebagai Admin → `http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment`. Verifikasi 3 badge (Open/Upcoming/Closed) tampil dengan angka akurat. Badge Closed harus ada animasi pulse first-load. Default filter Open+Upcoming aktif — baris Closed tidak tampil tanpa filter/search. |
| M-02 | Search "Cilacap" di tab Assessment tanpa filter status → Closed group muncul (tidak 0 result) | CIL-02 | Data Cilacap Closed sessions hanya ada di Dev DB. UAT lokal PASS via seed temp. | Di `/Admin/ManageAssessment` → ketik "Cilacap" di search box tanpa pilih status filter → expected: grup Cilacap berstatus Closed MUNCUL di hasil. Pre-fix behavior: 0 result. |
| M-03 | History tab — row Riwayat Assessment clickable navigasi ke `/CMP/Results/{SessionId}`. Enter/Space keyboard berfungsi. Archived row tampil "—". | CIL-03 | UAT lokal 32 row PASS via Playwright MCP seed temp. Verifikasi final dengan data riil di Dev diperlukan. | Buka `/Admin/ManageAssessment` tab History → klik salah satu baris Riwayat Assessment yang punya SessionId → expected: navigasi ke `/CMP/Results/{SessionId}`. Test keyboard: Enter/Space di row. Cek archived row: kolom Actions tampil "—" + tooltip. |
| M-04 | Login sebagai worker (bukan Admin/HC), buka `/CMP/Assessment` → banner worker tampil dengan link ke `/CMP/Records` | CIL-04 | UAT lokal hanya verify Admin variant via Playwright MCP. Worker variant di-verify via code `@if` branch inspection saja, bukan runtime browser. | Login worker biasa → buka `/CMP/Assessment` → expected: banner menampilkan "Looking for completed assessments? View your Training Records" dengan link `/CMP/Records`. Admin/HC tidak boleh melihat worker variant. |
| M-05 | Download Excel untuk session dengan peserta → buka di Microsoft Excel → 4 tab sheet dengan format terbaca | CIL-05 | UAT struktur xlsx PASS via JSZip parse lokal. Verifikasi format visual + readability untuk HC user butuh Excel app nyata. | `GET /Admin/ExportAssessmentResults?title={judul}&category={kategori}&scheduleDate={tanggal}` untuk session yang punya peserta. Buka hasil di Microsoft Excel. Expected: Sheet 1=Summary, Sheet 2="Detail Per Soal" (grid NIP/Nama/per soal), Sheet 3="Elemen Teknis" (matrix NIP/Nama/per elemen/Avg), Sheet 4+=per-peserta. Verifikasi header bold/gray, Avg arithmetic benar. |
| M-06 | `curl` GET BulkExportPdf dengan data Cilacap actual di Dev → ZIP berisi PDF per peserta valid | CIL-06 | UAT lokal PASS dengan session UAT v14 Standard. Data PostTest Cilacap 20 Mei 2026 hanya ada di Dev DB pasca IT promo. | `curl "http://10.55.3.3/KPB-PortalHC/Admin/BulkExportPdf?title=Post+Test+OJT+GAST+GTO+SRU+di+Unit+RU+IV+Cilacap&category=OJT&scheduleDate=2026-05-20" --cookie "..." -o cilacap_bundle.zip -L`. Expected: 200 OK + Content-Type application/zip + ZIP berisi `{NIP}_{NamaSlug}_{TitleSlug}.pdf` per peserta. Verify PDF signature: `xxd cilacap_bundle.zip` atau unzip + head per file = dimulai dengan `%PDF`. |
| M-07 **(KRITIS)** | Admin upload Excel 13 peserta Cilacap PreTest 30 Mar 2026 via `/Admin/BulkBackfill` di Dev → 13 AssessmentSession ter-insert + AuditLog 13 baris `ManualImport-Backfill` | REST-04 | REST-04 = code-only deploy di lokal. DB lokal tidak punya Cilacap PostTest counterpart untuk `linkedGroupId`. Eksekusi data restore HARUS di Dev DB oleh admin dengan data aktual. Ini adalah FINAL deliverable Phase 338 — Cilacap PreTest 30 Mar 2026 belum ter-restore. | **Prasyarat:** IT promo commit batch v19.0+v20.0 (~78 commit) ke Dev + Dev DB online. **Langkah:** (1) Cari ID PostTest Cilacap existing di Dev DB via SQL: `SELECT Id, Title FROM AssessmentSessions WHERE Title LIKE '%Post Test%Cilacap%' AND CompletedAt IS NOT NULL`. (2) Login Admin → `http://10.55.3.3/KPB-PortalHC/Admin/BulkBackfill`. (3) Upload `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`. (4) Isi field: title="Pre Test OJT GAST GTO SRU di Unit RU IV Cilacap", category="OJT", completedAt=2026-03-30, linkedGroupId={ID dari step 1}, auditTag="ManualImport-Backfill". (5) Submit + konfirmasi dialog. **Expected:** TempData Success muncul. SQL verify: `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '%Pre Test%Cilacap%'` = 13. `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='ManualImport-Backfill'` = 13. **Rollback bila error:** Semua 13 row otomatis di-rollback (tx atomic). |

---

## Code-Verified Spot-Checks (Tanpa Manual Browser)

Semua item berikut di-verified via grep/build/run di codebase — tidak butuh browser atau Dev DB.

| Check | Command | Expected | Status |
|-------|---------|----------|--------|
| Build 0 error | `dotnet build --no-restore` | `Build succeeded. 0 Error(s).` | ✅ PASS (Phase 338 close) |
| 18/18 test baseline | `dotnet test --no-build` | `Passed! Failed: 0, Passed: 18` | ✅ PASS (114 ms) |
| CIL-01 counter (ManageAssessment) | `grep "ViewBag.ClosedCount" Controllers/AssessmentAdminController.cs` | Ditemukan L201 | ✅ PASS |
| CIL-01 counter (AssessmentMonitoring) | `grep "ViewBag.ClosedCount" Controllers/AssessmentAdminController.cs` | Ditemukan L2837 | ✅ PASS |
| CIL-02 guard pattern | `grep "string.IsNullOrEmpty.*statusFilter.*string.IsNullOrEmpty.*search" Controllers/AssessmentAdminController.cs` | Ditemukan L205 | ✅ PASS |
| CIL-03 SessionId field | `grep "SessionId" Models/AllWorkersHistoryRow.cs` | `public int? SessionId` | ✅ PASS |
| CIL-03 JS handler | `grep "cil03-row-link" Views/Admin/Shared/_HistoryTab.cshtml` | Ditemukan L131+ | ✅ PASS |
| CIL-05 helper AddDetailPerSoalSheet | `grep "AddDetailPerSoalSheet" Helpers/ExcelExportHelper.cs` | Ditemukan L47 | ✅ PASS |
| CIL-05 helper AddElemenTeknisSheet | `grep "AddElemenTeknisSheet" Helpers/ExcelExportHelper.cs` | Ditemukan L139 | ✅ PASS |
| CIL-05 integration call | `grep "AddDetailPerSoalSheet\|AddElemenTeknisSheet" Controllers/AssessmentAdminController.cs` | Ditemukan L4296-4297 | ✅ PASS |
| CIL-06 endpoint | `grep "BulkExportPdf" Controllers/AssessmentAdminController.cs` | Ditemukan L4499 | ✅ PASS |
| CIL-06 DoS guard | `grep "max 50\|eligibleSessions.Count > 50" Controllers/AssessmentAdminController.cs` | DoS guard present L4515 | ✅ PASS |
| REST-04 GET endpoint | `grep "BulkBackfill" Controllers/TrainingAdminController.cs` | Ditemukan L720 (GET) | ✅ PASS |
| REST-04 POST endpoint | `grep "BulkBackfillAssessment" Controllers/TrainingAdminController.cs` | Ditemukan L728 (POST) | ✅ PASS |
| REST-05 template file | file exist `docs/templates/DB_HANDOFF_IT.template.md` | 108 LOC Markdown | ✅ PASS |
| REST-05 script file | file exist `scripts/backup-dev-pre-migration.ps1` | 90 LOC PowerShell | ✅ PASS |
| REST-05 no hardcode cred | `grep -i "password\|Password\|pwd" scripts/backup-dev-pre-migration.ps1` | Tidak ditemukan credential hardcode | ✅ PASS |
| REST-06 auto-pair call | `grep "TryAutoDetectCounterpartGroup" Controllers/AssessmentAdminController.cs` | count=2 (call L839 + def L6599) | ✅ PASS |
| REST-07 SOP section | `grep "Pre-Deploy Backup SOP" docs/DEV_WORKFLOW.md` | Ditemukan L142 | ✅ PASS |
| .gitignore script exception | `grep "backup-dev-pre-migration" .gitignore` | `!scripts/backup-dev-pre-migration.ps1` | ✅ PASS |

---

## UAT Lokal yang Sudah Selesai (Referensi dari SUMMARY)

| Wave | REQ | Method | Result | Evidence |
|------|-----|--------|--------|----------|
| 1 | CIL-01 | Playwright MCP (seed temp 3 session) | PASS | Badge Open/Upcoming/Closed + animasi pulse visible; default filter Open+Upcoming aktif |
| 1 | CIL-02 | Playwright MCP (search "Cilacap") | PASS | 1 grup Cilacap Closed muncul (sebelumnya 0 result) |
| 2 | CIL-03 | Playwright MCP (32 row Riwayat Assessment) | PASS | Row clickable + data-href + Actions "Lihat" button; archived row "—" |
| 2 | CIL-04 | Playwright MCP (login admin) | PASS (Admin variant); CODE-VERIFIED (worker variant) | Banner Admin/HC tampil → link admin history; @if branch worker code-verified |
| 3 | CIL-05 | JSZip xlsx parse via Playwright | PASS | 4 sheet workbook.xml: Summary/Detail Per Soal/Elemen Teknis/per-peserta; Avg (55.6+100+100+0)/4=63.9 ✓ |
| 4 | CIL-06 | curl direct GET | PASS | 200 OK + 76544 bytes + application/zip + %PDF signature |
| 4 | REST-04 | build + form render | CODE-PASS (data exec defer) | Build 0 error; /Admin/BulkBackfill form 7 field render; data restore defer ke Dev |
| 5 | REST-05 | file exist + parse | PASS | Kedua file exist; PowerShell parse OK; no hardcoded credential |
| 5 | REST-06 | build + grep | PASS | TryAutoDetectCounterpartGroup count=2; build 0 error |
| 5 | REST-07 | grep | PASS | Section "Pre-Deploy Backup SOP" L142 present + developer steps + IT steps |

---

## Validation Sign-Off

- [x] Semua 10 REQ memiliki verifikasi (kode, build, atau UAT lokal)
- [x] Build 0 error diverifikasi per wave (18 test baseline PASS)
- [x] Wave 0 tidak diperlukan — existing infra covers semua kebutuhan Phase 338
- [ ] **BELUM:** 7 item manual-only pending IT promo ke Dev + eksekusi manual
- [ ] **BELUM:** `nyquist_compliant: true` — tidak dapat di-set sampai M-07 REST-04 data restore selesai
- [ ] **BELUM:** Excel app visual verify (M-05) dan worker banner runtime verify (M-04)
- [ ] **BELUM:** BulkExportPdf dengan data Cilacap riil di Dev (M-06)

**Approval:** partial — 10/10 code-verified; 7 item human verification pending IT promo ke Dev (2026-06-02)

**Catatan REST-04 KRITIS:** M-07 adalah deliverable akhir Phase 338. Cilacap PreTest 30 Mar 2026 (13 peserta) belum ter-restore ke DB sampai langkah ini dieksekusi oleh Admin di Dev environment pasca IT promo. Eksekusi M-07 sebelum milestone v20.0 dinyatakan fully closed.
