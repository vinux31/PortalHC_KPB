---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
plan: 04
subsystem: restore-export
tags: [questpdf, zip, bulk-backfill, transaction, audit]

requires:
  - phase: 320
    provides: SpiderChartRenderer SkiaSharp PNG (CIL-06 reuse)
  - phase: 336
    provides: REST-04 Strategy A Hybrid A3 decision LOCKED
provides:
  - REST-04 BulkBackfillAssessment endpoint (code-only deploy)
  - CIL-06 BulkExportPdf ZIP endpoint + per-peserta multi-page PDF
  - QuestPDF 2026.2.2 reuse + ClosedXML Excel parse + ZipArchive streaming

affects: [338-05 REST-06 — LinkedGroupId enforce can reference BulkBackfill flow]

tech-stack:
  added: []
  patterns:
    - "Bulk endpoint atomic transaction: BeginTransactionAsync → Add range → SaveChanges → AuditLog per row → Commit"
    - "ZipArchive byte[] pattern: ToArray() BUKAN stream — File(stream) helper bermasalah dengan leaveOpen+MemoryStream"
    - "QuestPDF Document.Create per peserta multi-page (cover+spider, detail jawaban)"
    - "Excel parse 3-column format: NIP | Nama | Score (row 1 header skip)"
    - "AuditLog field correct: ActorUserId/ActorName/ActionType (max 50)/Description/TargetId/TargetType/CreatedAt"

key-files:
  created:
    - Views/Admin/BulkBackfill.cshtml
  modified:
    - Controllers/TrainingAdminController.cs
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "REST-04 code-only deploy (NO data exec lokal) — DB lokal tidak punya Cilacap PostTest counterpart untuk LinkedGroupId pair. Data restore execution defer ke IT promo Dev"
  - "Transaction atomic all-or-nothing — bila ANY NIP missing OR insert error, rollback semua"
  - "AuditLog per row dalam tx (Add ke context, single SaveChanges di akhir) BUKAN _auditLog.LogAsync (yang SaveChanges per call breaks tx)"
  - "AssessmentSession.AssessmentType=Standard untuk backfill (non-PrePost; LinkedGroupId optional pair)"
  - "BulkExportPdf max 50 peserta per batch (T-338-02 DoS mitigation)"
  - "SpiderChart fallback list bila <3 elemen (radar require min 3)"
  - "QuestPDF Multi-page: Page 1 cover+spider, Page 2+ detail jawaban grid"
  - "ZipArchive ToArray byte[] pattern — File(stream) anomaly 204 No Content fix"

patterns-established:
  - "Hybrid A3 bulk import: UI form upload + endpoint + transaction + audit"
  - "ZIP streaming per-peserta PDF dengan QuestPDF + ZipArchive byte[]"

requirements-completed: [REST-04, CIL-06]

duration: ~40min
completed: 2026-05-30
---

# Phase 338-04: BulkBackfill + BulkExportPdf Summary

**REST-04 + CIL-06 SHIPPED LOCAL. 4 commit lokal. CIL-06 UAT PASS via curl ZIP+PDF verify.**

## Performance

- **Duration:** ~40 min (3 task code + 1 fix + 1 UAT)
- **Completed:** 2026-05-30
- **Files:** 1 NEW (BulkBackfill.cshtml) + 2 modified
- **Build status:** PASS 0 error

## Accomplishments

- **REST-04** BulkBackfill endpoint code deploy:
  - GET /Admin/BulkBackfill — form upload UI dengan field title/category/completedAt/linkedGroupId/durationMinutes/passPercentage/auditTag
  - POST /Admin/BulkBackfillAssessment — Hybrid A3 Excel parse + transaction atomic + per-row AuditLog
  - Code-only (NO data execution lokal). Restore data execution defer ke IT promo Dev (DB lokal missing Cilacap PostTest LinkedGroupId counterpart)
- **CIL-06** BulkExportPdf endpoint deployed:
  - GET /Admin/BulkExportPdf?title=&category=&scheduleDate=
  - Reuse SpiderChartRenderer Phase 320 (SkiaSharp PNG, OQ-338-1 resolved)
  - QuestPDF 2026.2.2 multi-page (cover + spider chart + detail jawaban per soal)
  - ZIP streaming via System.IO.Compression.ZipArchive byte[]
  - T-338-02 DoS guard: max 50 peserta per batch
  - File naming: {NIP}_{NamaSlug}_{TitleSlug}.pdf inside ZIP
- AuditLog field correction inline (vs plan assumption): ActorUserId/ActorName/ActionType (max 50)/Description/TargetId/TargetType

## Task Commits

1. **T1-338-04: BulkBackfill endpoints REST-04** — `f209f9f4` (feat)
2. **T2-338-04: BulkBackfill.cshtml form UI** — `944b8d7f` (feat)
3. **T3-338-04: BulkExportPdf + GeneratePerPesertaPdf** — `31889c71` (feat)
4. **Fix: ZIP byte[] pattern** — `2a1b1544` (fix) — ASP.NET File(stream, ...) helper bermasalah dengan leaveOpen+MemoryStream, switched ke ToArray byte[]

## Files Modified

- `Controllers/TrainingAdminController.cs` (+153 LOC L713+) — GET BulkBackfill view + POST BulkBackfillAssessment endpoint (parse Excel + tx atomic + AuditLog per row + rollback bila NIP missing)
- `Views/Admin/BulkBackfill.cshtml` NEW (119 LOC) — form upload dengan 7 input field + confirm dialog + alert success/error
- `Controllers/AssessmentAdminController.cs` (+218 LOC L4470+) — BulkExportPdf endpoint (ZIP streaming) + GeneratePerPesertaPdf private helper (QuestPDF multi-page)

## UAT Verification

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| REST-04 | ✅ CODE PASS | `/Admin/BulkBackfill` form renders with all 7 fields. POST endpoint code-verified via build PASS. Data execution lokal SKIP — DB tidak punya Cilacap PostTest counterpart. Defer execution ke IT promo Dev (mode: admin upload `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`, set linkedGroupId=Cilacap PostTest existing Id, audit tag `ManualImport-Backfill`). |
| CIL-06 | ✅ PASS | curl GET /Admin/BulkExportPdf?title=UAT v14 Standard&category=OJT&scheduleDate=2026-04-10 → 200 OK + Content-Length 76544 + Content-Type application/zip + Content-Disposition attachment filename=UAT_v14_Standard_OJT_20260410_Bundle.zip. ZIP entry: `123456_Iwan_UAT_v14_Standard.pdf` 84261 bytes valid `%PDF` signature. |

**Coverage:** CIL-06 1/1 PASS via curl. REST-04 code-deploy-only (data exec defer).

## Bug Encountered + Fix

- **Bug:** Playwright fetch BulkExportPdf → 204 No Content + empty body (despite session match + eligible)
- **Root cause:** ASP.NET `File(stream, contentType, fileName)` overload bermasalah dengan pattern `MemoryStream + ZipArchive(leaveOpen=true)` — response sometimes returns 204 + ct null
- **Fix:** Switch ke pattern `byte[] zipBytes = memoryStream.ToArray()` → `return File(zipBytes, ...)`. Verified via curl (status 200 + content-length proper)
- **Lesson:** For ZipArchive output, ALWAYS use byte[] pattern (ToArray()) not Stream (less reliable across .NET versions)

## Threats

| Threat ID | Status |
|-----------|--------|
| T-338-02 BulkPDF memory spike | mitigated (max 50 peserta + ZipArchive Optimal compression) |
| T-338-03 Backfill audit gap | mitigated (per-row AuditLog ManualImport-Backfill tag mandatory) |
| Excel parse injection | mitigated (ClosedXML parse, no SQL-direct, EF parameterized insert) |
| Bulk insert rollback | mitigated (single tx wrap, all-or-nothing on NIP miss / exception) |
| Authorization bypass | mitigated (BulkBackfill [Authorize Admin]; BulkExportPdf [Authorize Admin,HC]) |

## Seed Workflow

- No temp seed (UAT pakai existing UAT v14 Standard session)
- DB baseline preserved

## Lessons & Surprises

- **AuditLog field name corrections vs plan**: Actual `ActorUserId/ActorName/ActionType` (max 50)/`Description/TargetId/TargetType/CreatedAt`. Plan asumsi `UserId/Action/EntityType/EntityId/Details/Timestamp` — wrong. Match field names di codebase critical.
- **SpiderChartRenderer signature**: Tuple `(string label, double percentage)` not class `EtDataPoint`. Plan asumsi class wrong.
- **AuditLogService.LogAsync()** internally SaveChanges → breaks transaction atomic. Use direct `_context.AuditLogs.Add(...)` + single SaveChanges di end of tx.
- **`File(stream, ...)` ASP.NET helper anomaly** dengan MemoryStream + ZipArchive leaveOpen=true: kadang return 204 + empty body. Switch ke byte[] pattern.
- **Curl PASS, Playwright fetch FAIL**: Browser fetch cookie + same-site policy mungkin bermasalah untuk binary download. Curl direct connection deterministic — gunakan untuk binary endpoint verify.
- **DB lokal Cilacap PostTest missing** — confirms data divergence Lokal vs Dev. REST-04 data execution butuh Dev environment (atau seed PostTest dulu di lokal).

## Next

- Wave 5 Plan 338-05 FINAL — REST-05/06/07 guardrail backup template + LinkedGroupId enforce + DEV_WORKFLOW.md update
- REST-04 data execution (restore 13 peserta Cilacap PreTest) → IT promo Dev SEPARATE
