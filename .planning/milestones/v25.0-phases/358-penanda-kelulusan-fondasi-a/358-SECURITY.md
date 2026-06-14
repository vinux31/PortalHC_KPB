---
phase: 358
slug: penanda-kelulusan-fondasi-a
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-12
---

# Phase 358 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Verifikasi retroaktif (State B) via gsd-security-auditor terhadap kode terimplementasi. 10/10 threat closed (8 mitigate grep-verified + 2 accept-documented).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| developer → DB lokal | migration DDL `Origin` + data-seed UPDATE dijalankan lokal saja; promosi Dev/Prod = Team IT | DDL + data-seed (no external input) |
| caller (GradingService/Controller) → ProtonCompletionService | input coacheeId/trackId/origin dari kode internal terpercaya | in-process, no direct user input |
| grading flow → penanda hook | session sudah ter-grade (Completed) sebelum hook | internal grading state |
| HTTP admin client → POST /Admin/BackfillProtonPenanda | endpoint maintenance admin-only, trigger batch mutasi data | anti-forgery token + admin authz |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation / Evidence | Status |
|-----------|----------|-----------|-------------|------------------------|--------|
| T-358-01 | Tampering | migration data-seed UPDATE ProtonFinalAssessments | accept | Dev-only; snapshot `pre358origin.bak` + SEED_JOURNAL update; no external input (358-01-SUMMARY). | closed |
| T-358-02 | Denial | apply migration ke Dev/Prod tanpa snapshot | mitigate | `migration=true` di-flag (358-01-SUMMARY L7+L27-28, commit `34ac03e0`); no auto-apply-to-prod logic; CLAUDE.md promosi=IT. | closed |
| T-358-03 | Tampering | RemoveExamOriginAsync hapus penanda salah-origin | mitigate | `Services/ProtonCompletionService.cs:122` filter `&& fa.Origin == "Exam"` verbatim → Bypass/Interview kebal. | closed |
| T-358-04 | Elevation | EnsureAsync buat penanda tanpa assignment valid | mitigate | `Services/ProtonCompletionService.cs:44-46` guard `assignment == null` → return false (0 penanda) sebelum Add. | closed |
| T-358-05 | Tampering | hook terbit penanda untuk exam non-Proton | mitigate | `Services/GradingService.cs:308` (Hook A), `:542` (Hook C Fail→Pass), `:492` (Hook B Pass→Fail): guard `Category=="Assessment Proton" && ProtonTrackId.HasValue` (+`isPassed` Hook A/C). | closed |
| T-358-06 | Repudiation | re-grade hapus penanda tanpa jejak | accept | Re-grade ber-audit di controller pemanggil (Phase 296/324, pre-358); `Origin=="Exam"` selektif cegah hapus jalur lain. | closed |
| T-358-07 | Elevation/IDOR | BackfillProtonPenanda diakses non-admin | mitigate | `Controllers/AssessmentAdminController.cs:3948` `[Authorize(Roles = "Admin")]` (ketat, bukan "Admin, HC"). | closed |
| T-358-08 | Tampering/CSRF | POST backfill cross-site | mitigate | `Controllers/AssessmentAdminController.cs:3949` `[ValidateAntiForgeryToken]`; UAT live: POST tanpa token → HTTP 400. | closed |
| T-358-09 | Information Disclosure | error backfill bocorkan detail DB | mitigate | `AssessmentAdminController.cs:4040` `_logger.LogError(ex, ...)`; TempData pesan generik "Cek log untuk detail" — no `ex.Message` di response. | closed |
| T-358-10 | Tampering | backfill terbit penanda tanpa deliverable 100% | mitigate | `AssessmentAdminController.cs:3996` `statuses.Count == 0 || !statuses.All(s => s == "Approved")` → `notEligible++; continue` (D-08). | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| R-358-01 | T-358-01 | UPDATE data-seed dev-only di `HcPortalDB_Dev`; no external input; rollback via snapshot BAK; promosi+verifikasi migration = Team IT. | Rino | 2026-06-12 |
| R-358-02 | T-358-06 | Re-grade ber-audit di controller pemanggil sejak Phase 296/324 (pre-358); `RemoveExamOriginAsync` selektif `Origin="Exam"` → penanda Interview/Bypass tak terhapus tak sengaja. | Rino | 2026-06-12 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-12 | 10 | 10 (8 mitigate-verified + 2 accept) | 0 | gsd-security-auditor (file:line evidence) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-12
