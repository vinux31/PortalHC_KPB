---
phase: 397-link-pre-post-ke-room-existing
verified: 2026-06-18T13:00:00Z
status: passed
score: 9/9
overrides_applied: 0
---

# Phase 397: Link Pre/Post ke Room Existing — Verification Report

**Phase Goal:** HC dapat menautkan sesi inject Pre/Post ke assessment room existing lewat search picker, mendukung skenario silang inject↔online tanpa merusak grouping Pre/Post.
**Verified:** 2026-06-18T13:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Saat tipe inject Pre/Post, HC dapat mencari & memilih assessment room existing via search picker | VERIFIED | `SearchLinkTargets` GET terdapat di `Controllers/InjectAssessmentController.cs` dengan RBAC `[Authorize(Roles="Admin, HC")]`; `#roomPickerModal`, `#btnCariRoom` ada di view; fetch ke `@Url.Action("SearchLinkTargets")` terkonfirmasi di JS |
| 2 | Memilih room men-set `LinkedGroupId`+`LinkedSessionId` per pekerja (bukan broadcast) | VERIFIED | `InjectBatchAsync` menghapus broadcast `req.LinkedSessionId`; resolusi per-UserId via `siblingByUserId`; `Assert.NotEqual` pada LinkedSessionId 2 pekerja ada di `InjectLinkPrePostTests.cs:161`; suite 15/15 green (SUMMARY 397-02) |
| 3 | Skenario silang inject↔online tampil sebagai satu pasangan Pre/Post utuh di Records/gain-score | VERIFIED | `InjectCrossGroupingTests.CrossLink_GainScore_Intact_InjectPre_OnlinePost` mengunci invarian via GetGainScoreData-equivalent EF query (LinkedGroupId+UserId); UAT live §13 KRITIS inject Pre (Id 174) + online Post (Id 173) share LinkedGroupId=173; Score=85/Status=Completed online UNCHANGED |
| 4 | Build 0 error + test suite green + picker tampil Pre/Post → pilih room → inject tertaut | VERIFIED | SUMMARY 397-02: fast suite 389/389; SUMMARY 397-03: 390/390; Playwright 6/6 (e2e `inject-assessment-397.spec.ts`); UAT live 9/9 PASS |
| 5 | Kasus A mengadopsi LinkedGroupId target tanpa menyentuh data online; Kasus B menulis stiker ke SEMUA sesi target + audit LinkPrePost | VERIFIED | `ResolveLinkContextAsync` adalah satu sumber kebenaran Kasus A/B; `mutatedOnlineSessionIds` gated `!IsManualEntry`; audit `"LinkPrePost"` terkonfirmasi di service (:366); UAT §8 audit LinkPrePost x1 hadir |
| 6 | UnlinkInjectGroupAsync merevert link + stiker Kasus B (one-sided heuristic), atomic + audit LinkPrePostUndo, tanpa menyentuh Score online | VERIFIED | Method hadir di `InjectAssessmentService.cs:736`; audit `"LinkPrePostUndo"` (:800); `UnlinkInjectGroupTests` (3 tes) green; UAT §9 Link columns NULL + Score utuh + audit LinkPrePostUndo x2 |
| 7 | PreviewPairingAsync adalah dry-run tanpa DB write; `PreviewPairing` endpoint mengembalikan InjectPairingPreview | VERIFIED | XML-doc service `:657` menyatakan "TIDAK memanggil SaveChangesAsync"; grep body PreviewPairingAsync = 0 SaveChanges; endpoint `PreviewPairing` POST di controller (:246) memanggil `PreviewPairingAsync` dan `return Json(summary)` |
| 8 | Anti-double-link menolak FULL LIST pekerja yang punya sibling tipe-sama di grup target | VERIFIED | `InjectAntiDoubleLinkTests.SameTypeSibling_RejectFullList_AllOffendingWorkers` ada dan green; pesan Bahasa Indonesia "sudah memiliki" terkonfirmasi di preflight service dan view N4 |
| 9 | 0 migration: tidak ada perubahan schema atau seed data dipromosikan | VERIFIED | `git diff --name-only b465a5ab..HEAD -- Migrations/ Data/` = kosong (0 baris output); SUMMARY 397-04 menyatakan probe `_verify397` kosong |

**Score:** 9/9 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `HcPortal.Tests/InjectLinkPrePostTests.cs` | RED→GREEN: per-worker anti-broadcast, Kasus A/B, atomic rollback | VERIFIED | File ada; `[Trait("Category","Integration")]`; `Assert.NotEqual` pada LinkedSessionId (:161); `InjectAssessmentFixture` |
| `HcPortal.Tests/InjectAntiDoubleLinkTests.cs` | RED→GREEN: D-08 full-list reject | VERIFIED | File ada; `[Trait("Category","Integration")]`; "sudah memiliki" ada |
| `HcPortal.Tests/UnlinkInjectGroupTests.cs` | RED→GREEN: D-12 atomic revert | VERIFIED | File ada; referensi `UnlinkInjectGroupAsync` dan `"LinkPrePostUndo"` |
| `HcPortal.Tests/InjectPreviewPairingTests.cs` | RED→GREEN: D-07 dry-run NO write | VERIFIED | File ada; referensi `PreviewPairingAsync` dan `InjectPairingPreview` |
| `HcPortal.Tests/InjectCrossGroupingTests.cs` | RED→GREEN: spec §13 KRITIS cross inject↔online | VERIFIED | File ada; `CrossLink_GainScore_Intact_InjectPre_OnlinePost`; query GetGainScoreData-equivalent |
| `Models/InjectAssessmentDtos.cs` | LinkTargetRepId + InjectPairingPreview (6 field) | VERIFIED | `LinkTargetRepId` (:69); kelas `InjectPairingPreview` (:76); HasLink/Paired/Unpaired/WillTouchOnline/DateWarn/DoubleLinkErrors ada |
| `ViewModels/InjectAssessmentViewModel.cs` | LinkedTargetRepId hidden field | VERIFIED | Field `LinkedTargetRepId` (:47); XML-doc server re-resolve |
| `Services/InjectAssessmentService.cs` | per-worker linking + Kasus A/B + UnlinkInjectGroupAsync + PreviewPairingAsync + ResolveLinkContextAsync | VERIFIED | Semua method ada; `siblingByUserId` (3 match); `resolvedGroupId` ada; `"LinkPrePost"` (:366); `"LinkPrePostUndo"` (:800); `ResolveLinkContextAsync` (5 match = shared helper) |
| `Controllers/InjectAssessmentController.cs` | SearchLinkTargets + MapToRequest link wiring + PreviewPairing + UnlinkInjectGroup | VERIFIED | `SearchLinkTargets` (:177) GET RBAC `return Json`; `LinkTargetRepId = vm.LinkedTargetRepId` (:473); `PreviewPairing` (:246) POST RBAC+CSRF; `UnlinkInjectGroup` (:265) POST RBAC+CSRF `return Json({ ok, message })` |
| `Views/Admin/InjectAssessment.cshtml` | roomPickerModal + btnCariRoom + selectedRoomChip + LinkedTargetRepId + previewPairingSummary + unlinkConfirmModal | VERIFIED | Semua ID ada; placeholder note "tersedia pada fase berikutnya" dihapus (0 match); `aria-live="polite"` pada previewPairingSummary; Bootstrap confirm modal (bukan native `confirm()` untuk unlink) |
| `tests/e2e/inject-assessment-397.spec.ts` | Playwright 6/6: modal/picker/chip/preview/unlink + cross-grouping §13 | VERIFIED | File ada; referensi `roomPickerModal`, `selectedRoomChip`, `previewPairingSummary`, `unlinkConfirmModal`; Contract 8 cross-grouping + Contract 9 unlink ada |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `InjectAssessment.cshtml (#btnCariRoom)` | `/Admin/SearchLinkTargets` | fetch debounced di JS | WIRED | `@Url.Action("SearchLinkTargets", "InjectAssessment")` terkonfirmasi di view JS block |
| `InjectAssessment.cshtml (chip)` | `#LinkedTargetRepId` → `MapToRequest` | chip JS set hidden input; MapToRequest baca `vm.LinkedTargetRepId` | WIRED | `id="LinkedTargetRepId"` ada; `LinkTargetRepId = vm.LinkedTargetRepId` di MapToRequest (:473) |
| `InjectAssessment.cshtml (Pratinjau)` | `/Admin/PreviewPairing` | fetch POST saat room terhubung | WIRED | `@Url.Action("PreviewPairing", "InjectAssessment")` di view JS (:2244) |
| `InjectAssessmentController.SearchLinkTargets` | `AssessmentSession` (opposite-type, Kasus A/B) | EF query → `return Json` | WIRED | `return Json(rows)` (:236); grouped by `LinkedGroupId` + standalone by `Title+Category+Schedule.Date` |
| `InjectAssessmentController.MapToRequest` | `InjectRequest.LinkTargetRepId` | `vm.LinkedTargetRepId → req.LinkTargetRepId` | WIRED | `:473`: `LinkTargetRepId = vm.LinkedTargetRepId`; raw `LinkedGroupId`/`LinkedSessionId` dari `vm.` tidak di-assign |
| `InjectAssessmentController.UnlinkInjectGroup` | `InjectAssessmentService.UnlinkInjectGroupAsync` | POST handler | WIRED | `:269`: `await _injectService.UnlinkInjectGroupAsync(injectGroupId, ...)` |
| `InjectAssessmentService.InjectBatchAsync` | `AssessmentSession.LinkedGroupId/LinkedSessionId` (inject + online) | per-UserId sibling resolution inside tx | WIRED | `siblingByUserId.TryGetValue`; Kasus B `mutatedOnlineSessionIds` |
| `InjectAssessmentService` | `AuditLog` (LinkPrePost / LinkPrePostUndo) | `_context.AuditLogs.Add` in-tx | WIRED | `:366` `"LinkPrePost"`; `:800` `"LinkPrePostUndo"` |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|-------------|--------|-------------------|--------|
| `InjectAssessment.cshtml` (`#roomPickerModal` rows) | JSON dari `SearchLinkTargets` | EF query `AssessmentSessions` opposite-type | Ya — query DB real | FLOWING |
| `InjectAssessment.cshtml` (`#previewPairingSummary`) | `InjectPairingPreview` dari `PreviewPairing` | `PreviewPairingAsync` — EF AsNoTracking query | Ya — query DB real | FLOWING |
| `InjectAssessmentService.InjectBatchAsync` | `resolvedGroupId`, `siblingByUserId` | `ResolveLinkContextAsync` EF query | Ya — re-resolve dari DB per `LinkTargetRepId` | FLOWING |
| `InjectAssessmentService.UnlinkInjectGroupAsync` | inject sessions dimuat, sibling dimuat | EF query `IsManualEntry` filter | Ya — DB real | FLOWING |

---

### Behavioral Spot-Checks

Step 7b tidak dijalankan via shell (app memerlukan server aktif), tetapi digantikan oleh UAT live yang dikonfirmasi orchestrator: 9/9 kontrak browser PASS + DB SQL queries verified.

| Behavior | Check Type | Result | Status |
|----------|-----------|--------|--------|
| Cari Room Pasangan hanya muncul untuk Pre/Post | Playwright Contract 1 | Terkonfirmasi | PASS |
| Modal menampilkan hanya tipe-lawan | Playwright Contract 2 + UAT §2 | Terkonfirmasi | PASS |
| Kasus A/B badge di picker | Playwright Contract 3 + UAT §3 | Terkonfirmasi | PASS |
| Chip + hidden field setelah pick | Playwright Contract 4 + UAT §4 | Terkonfirmasi | PASS |
| Pairing summary Pratinjau | Playwright Contract 5 + UAT §5-§7 | Terkonfirmasi | PASS |
| KRITIS §13 cross-grouping + online untouched | Playwright Contract 8 + UAT §8 (DB: LinkedGroupId=173, Score=85 UNCHANGED, audit LinkPrePost x1) | Terkonfirmasi | PASS |
| Unlink Bootstrap modal (bukan native confirm) + link reverted + audit | Playwright Contract 9 + UAT §9 (DB: 173/174 NULL, audit LinkPrePostUndo x2) | Terkonfirmasi | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| INJ-12 | 397-01/02/03/04 | HC dapat mencari & memilih assessment room existing untuk menautkan sesi inject Pre/Post (`LinkedGroupId`+`LinkedSessionId`) — skenario silang inject↔online | SATISFIED | SearchLinkTargets picker + per-worker linking + Kasus A/B + unlink + Playwright e2e 6/6 + UAT 9/9; REQUIREMENTS.md: `[x] INJ-12 Phase 397 Complete` |

---

### Anti-Patterns Found

| File | Pattern | Severity | Assessment |
|------|---------|----------|-----------|
| `Controllers/InjectAssessmentController.cs:270-271` | `TempData["Success"/"Error"]` diset pada JSON-only action `UnlinkInjectGroup` | Warning (WR-03 dari REVIEW) | NON-blocking — TempData bertahan ke navigasi berikutnya, UX membingungkan namun integritas data tidak rusak; terdokumentasi di REVIEW |
| `Services/InjectAssessmentService.cs:130-132` | `g.First()` tanpa `OrderBy` untuk `siblingByUserId` — non-deterministik bila >1 sibling tipe-lawan per UserId | Warning (WR-02 dari REVIEW) | NON-blocking — dampak terbatas pada pointer `LinkedSessionId` saja; display gain-score tetap benar via `LinkedGroupId+UserId`; terdokumentasi di REVIEW |
| `Services/InjectAssessmentService.cs:743-745` | `UnlinkInjectGroupAsync` berpotensi melepas LINTAS-BATCH pada Kasus A | Warning (WR-01 dari REVIEW) | NON-blocking — integritas Score/Status/audit terjaga; efek samping operasional (bukan data loss); terdokumentasi di REVIEW |

Semua 3 Warning dari REVIEW dikonfirmasi NON-blocking, NON-security. Tidak ada pola 🛑 Blocker.

---

### Human Verification Required

Tidak diperlukan — semua kontrak telah diverifikasi via Playwright e2e 6/6 + UAT live browser 9/9 (dilaksanakan orchestrator sebelum verifikasi ini). Termasuk:
- §13 KRITIS cross inject↔online grouping (DB-verified)
- Unlink Bootstrap modal behavior
- Data online Score/Status UNCHANGED setelah link/unlink

---

### Gaps Summary

Tidak ada gap. Semua 9 truths VERIFIED. INJ-12 tercapai penuh.

**3 Warning dari REVIEW (WR-01/WR-02/WR-03) dikonfirmasi NON-blocking, NON-security, dan sesuai praktik yang ada (terdokumentasi di 397-REVIEW.md). Tidak memerlukan closure sebelum phase dinyatakan passed.**

---

## Summary

Phase 397 mencapai goal-nya: HC dapat menautkan sesi inject Pre/Post ke assessment room existing via search picker, dengan per-worker bidirectional linking (bukan broadcast), Kasus A adopt / Kasus B write-to-all + audit, atomic rollback, anti-double-link full-list, preview pairing dry-run, dan unlink confirm modal Bootstrap. Skenario silang inject↔online (KRITIS §13) terbukti berfungsi via Playwright e2e + UAT live DB-verified. 0 migration. Suite 390/390 + integration 15/15 + Playwright 6/6.

INJ-12 COMPLETE — Phase 397 siap advance ke Phase 398 (Test + UAT "seakan online", INJ-13).

---

_Verified: 2026-06-18T13:00:00Z_
_Verifier: Claude (gsd-verifier)_
