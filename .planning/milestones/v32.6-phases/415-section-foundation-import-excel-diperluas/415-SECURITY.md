---
phase: 415-section-foundation-import-excel-diperluas
secured: 2026-06-24
asvs_level: 1
block_on: high
threats_total: 18
threats_closed: 18
threats_open: 0
unregistered_flags: 0
status: secured
---

# Phase 415: Section Foundation + Import Excel Diperluas — Security Verification

**Phase:** 415 — Section Foundation + Import Excel Diperluas (v32.6 keystone)
**ASVS Level:** L1
**Block on:** high
**Threats Closed:** 18/18
**Threats Open:** 0

Verifikasi mitigasi threat dari `<threat_model>` Plan 01–04 terhadap kode yang sudah diimplementasi. Implementation files READ-ONLY; tidak ada perubahan kode pada gerbang ini. Tidak ditemukan gap.

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-415-01 | Tampering — Migration on live data | mitigate (process/transfer) | CLOSED | `Migrations/20260622124217_AddAssessmentPackageSection.cs` — `AddColumn SectionId nullable:true` (no backfill/transform), `Down()` simetris (drop FK→table→index→column). Committed `2391257c` "migration=TRUE"; SUMMARY 01/03/04 mencatat notify-IT hanya Plan 01. Applied local-only (HcPortalDB_Dev) per CLAUDE.md; Dev/Prod = IT. |
| T-415-02 | DoS — Multiple-cascade-path FK | mitigate | CLOSED | `Data/ApplicationDbContext.cs:487-494` Question→Section = `OnDelete(DeleteBehavior.SetNull)`; `:498-507` Section→Package = `OnDelete(DeleteBehavior.Restrict)` (deviasi aman dari plan Cascade — hapus jalur cascade kedua ke PackageQuestions, hindari SQL Server error 1785). Migration FK: `ReferentialAction.SetNull` (line 59) + `ReferentialAction.Restrict` (line 39). Komentar kode mendokumentasikan rasional. |
| T-415-03 | Repudiation — Migration provenance to IT | mitigate (transfer) | CLOSED | Migration + snapshot di-commit bersama (`2391257c`); commit message + SUMMARY 01/03/04 flag `migration=TRUE` (`AddAssessmentPackageSection`). IT-handoff terdokumentasi (commit hash + migration flag). |
| T-415-04 | Tampering — EF tool version stamp | accept→mitigate | CLOSED | `Migrations/ApplicationDbContextModelSnapshot.cs:20` `HasAnnotation("ProductVersion", "8.0.0")` (bukan 10.x). Pinned local dotnet-ef 8.0.0 efektif. |
| T-415-05 | Spoofing/Elevation — Section CRUD endpoints (RBAC) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs` CreateSection (6228-6231), EditSection (6284-6287), DeleteSection (6344-6347), SetAllSectionsNewPage (6374-6377): semua `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]`. CreateSection/SetAll juga package-existence guard (6233/6381). |
| T-415-06 | Tampering/CSRF — All Section POST forms | mitigate | CLOSED | `[ValidateAntiForgeryToken]` pada 4 endpoint (lihat T-415-05). View `Views/Admin/ManagePackageQuestions.cshtml`: `@Html.AntiForgeryToken()` di sectionForm (206), delete-form (163), + quick-button form. |
| T-415-07 | XSS — Nama Section render (panel, group headers, dropdown) | mitigate | CLOSED | `ManagePackageQuestions.cshtml`: `sectionLabel` (10-11) string interpolasi di-render via `@sectionLabel(s)` (289, 349 — Razor auto-encode); `@(...s.Name)` (127); `data-section-name="@s.Name"` (155, attribute-encoded). Zero `@Html.Raw`. JS `loadSectionEdit` set `.value` (1043) + `.textContent` (1047), TIDAK innerHTML. Dropdown prefill via `.value` (706). |
| T-415-08 | InfoDisclosure/IDOR — sectionId CreateQuestion/EditQuestion + EditSection/DeleteSection | mitigate | CLOSED | CreateQuestion IDOR-guard (7750-7758) `AnyAsync(s.Id==sectionId && s.AssessmentPackageId==packageId)`; EditQuestion idem (7965-7973). EditSection (6293-6297) + DeleteSection (6353-6357) tolak bila `section.AssessmentPackageId != packageId`. |
| T-415-09 | Tampering — Duplicate SectionNumber | mitigate | CLOSED | CreateSection `AnyAsync` pre-check (6236-6242) + DbUpdateException unique-index fallback (6257-6269, hanya 2601/2627, lain di-propagate). EditSection idem (6299-6329). Defense-in-depth: unique index `(AssessmentPackageId, SectionNumber)` (DbContext 512-513). |
| T-415-10 | Tampering — Format auto-detect | mitigate | CLOSED | `AssessmentAdminController.cs:7044-7060` deteksi format dari worksheet HEADER row server-read; M2-hardened: bukan sekadar `colCount>9` tapi marker nama header ("opsi e"/"opsi f"/"no. section"/"nama section") harus cocok. Tidak ada field client `isNewFormat`. |
| T-415-11 | Spoofing/Elevation — ImportPackageQuestions endpoint | mitigate | CLOSED | ImportPackageQuestions POST (6968-6970) `[Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]`. File-type whitelist .xlsx/.xls (6977-6983) + 5MB size guard (6987-6991). |
| T-415-12 | Tampering/integrity — Per-Section count mismatch (D-13 hard-block) | mitigate | CLOSED | Mismatch list dibangun LENGKAP (never stop-at-first) 7263-7291; on any mismatch `TempData["SectionMismatch"]=JSON` + return SEBELUM persist (7296-7301), ZERO write. Persist atomic dalam `BeginTransactionAsync`/Commit/Rollback (7461-7471). Server-authoritative via shared `SectionStructureComparer`. |
| T-415-13 | XSS — Imported Nama Section + mismatch error list render | mitigate | CLOSED | `Views/Admin/ImportPackageQuestions.cshtml`: mismatch `<li>@line</li>` (47) Razor-encoded; deserialize TempData (36) try/catch. Zero `@Html.Raw`. Nama Section yang di-import dirender di panel/dropdown (XSS-safe, lihat T-415-07). |
| T-415-14 | DoS — Malformed/large Excel | accept | CLOSED (accepted risk — lihat log) | 5MB size guard (6987-6991) + try/catch workbook read; endpoint admin-only authenticated. Residual risk diterima per disposition `accept`. Lihat Accepted Risks Log. |
| T-415-15 | Tampering/integrity — SyncPackagesToPost SectionId remap | mitigate | CLOSED | `SyncPackagesToPost` (6526) build `sectionMap` old→new (6592-6607); remap via NAV property `newQ.Section = mappedSection` (6636-6637) — BUKAN `newQ.SectionId = q.SectionId` (grep konfirmasi pola naif ABSEN). Defensif: section tak ter-map → biarkan null. |
| T-415-16 | Tampering/DoS — StartExam re-guard over-broad | mitigate | CLOSED | `Controllers/CMPController.cs:1096` fire HANYA bila `guardPackages.Count >= 2 && guardAnySections` (legacy all-null + single-pkg lolos). H1-hardened: sibling via `SiblingPrePostAwarePredicate` (1082-1086) agar Pre/Post se-tanggal tak salah-banding. Guard di jalur `assignment == null` (1071). |
| T-415-17 | Repudiation/integrity — Exam starts on drifted structure | mitigate | CLOSED | Re-guard server-authoritative (CMPController 1071-1127) JALAN SEBELUM `ShuffleEngine.BuildQuestionAssignment` (1134); pada drift → block + `TempData["Error"]` pesan UI-SPEC + `RedirectToAction("Assessment")` (1124-1125), assignment TIDAK dibangun. Comparison via shared `SectionStructureComparer` (parity dgn import). |
| T-415-18 | InfoDisclosure — Stale Post sections after re-sync | mitigate | CLOSED | `SyncPackagesToPost` (6552-6560) explicit `RemoveRange` Section records paket Post lama sebelum clone (kompensasi FK Restrict yang tak cascade). Komentar SEC-01 mendokumentasikan. |

## Disposition Methods Applied

- **mitigate (code):** T-415-02, 05, 06, 07, 08, 09, 10, 11, 12, 13, 15, 16, 17, 18 — pattern mitigasi diverifikasi hadir di file yang dikutip (file:line).
- **mitigate (process/transfer):** T-415-01, 03 — migration additif + provenance IT-handoff (commit `2391257c`, migration=TRUE flag terdokumentasi di SUMMARY).
- **accept→mitigate:** T-415-04 — EF tool pinned + snapshot ProductVersion 8.0.0 diverifikasi.
- **accept:** T-415-14 — residual DoS Excel malformed/besar diterima (admin-only + 5MB + try/catch). Lihat Accepted Risks Log.

## Accepted Risks Log

| Threat ID | Risk | Justification | Residual Control |
|-----------|------|---------------|------------------|
| T-415-14 | DoS via Excel malformed/besar pada ImportPackageQuestions | Endpoint hanya untuk Admin,HC ter-autentikasi (bukan publik); surface serangan kecil. Disposition plan = `accept`. | 5MB size guard (AssessmentAdminController.cs:6987-6991) + file-type whitelist (6977-6983) + try/catch pada pembacaan workbook ClosedXML. |

## Deviation Note (security-positive)

PLAN 01 mendeklarasikan FK Section→Package = **Cascade**. Implementasi menggunakan **Restrict** (`ApplicationDbContext.cs:498-507`, migration `ReferentialAction.Restrict`). Ini adalah perbaikan keamanan/integritas, bukan gap: Cascade akan menciptakan jalur multiple-cascade-path kedua ke `PackageQuestions` (langsung Cascade dari Package + via Section) → SQL Server error 1785 (T-415-02). Restrict + explicit `RemoveRange` pada DeletePackage dan SyncPackagesToPost (6552-6560) menutup T-415-02 lebih konservatif dan sekaligus T-415-18 (stale Post sections). Diterima.

## Unregistered Flags

None. SUMMARY Plan 03 & 04 `## Threat Flags` = "None" (semua surface keamanan terpetakan ke threat ID yang ada). SUMMARY Plan 01 & 02 tidak punya seksi `## Threat Flags` (data-layer + UI; surface mereka tercakup oleh T-415-01..09). Tidak ada attack-surface baru tak-teregistrasi.

## Verdict

**SECURED** — 18/18 threat tertutup (16 mitigate verified in code, 2 process/transfer terdokumentasi, 1 accepted risk dengan residual control, 1 EF-stamp verified). threats_open = 0. Tidak ada blocker pada `block_on: high`.

_Secured: 2026-06-24 — gsd-security-auditor (ASVS L1)_
