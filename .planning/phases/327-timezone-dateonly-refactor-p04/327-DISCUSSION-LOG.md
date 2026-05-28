# Phase 327: Timezone DateOnly Refactor (P04) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-28
**Phase:** 327-timezone-dateonly-refactor-p04
**Areas discussed:** VM Type Scope, Today Reference, Computed Properties Strategy, Pre-migration SQL Check Artifact, Rollup Props Scope, Phase 326 Followup Bundle, Excel Import Parse, xUnit Library, JSON API Audit

---

## VM Type Scope (Q1)

| Option | Description | Selected |
|--------|-------------|----------|
| Entity + ALL VMs flip (Recommended) | Konsisten type end-to-end. Form binder native. SertifikatRow.ValidUntil + DeriveCertificateStatus signature all DateOnly?. Effort +30 min. | ✓ |
| Entity only, VMs stay DateTime? | Per spec §7.2 literal. VM handler cast manual. Risk type drift codebase. | |
| Entity + VMs + SertifikatRow + computed props rewrite | Plus TrainingRecord.DaysUntilExpiry + IsExpiringSoon rewrite, UnifiedTrainingRecord.IsExpired DateTime.Now → UtcNow. Full consistency. | |

**User's choice:** Entity + ALL VMs flip (Recommended)
**Notes:** D-07 lock VM scope minimum. Q1 follow-up Q5 extends ke rollup props.

---

## Today Reference (Q2)

| Option | Description | Selected |
|--------|-------------|----------|
| DateOnly.FromDateTime(DateTime.UtcNow) per spec (Recommended) | Verbatim spec §7.3. WIB boundary 00:00-07:00 risk, Pertamina jam kerja 07:00-17:00 tidak kena. Acceptable. | ✓ |
| DateOnly.FromDateTime(DateTime.Now) | Server WIB tz. "Today" semantik user. Risk kalau server pindah tz nanti. | |
| Helper Clock.Today() di Helpers/ | Centralized + DI swap test mocking. Effort +30 min new file + 6 call site replace. Overkill. | |

**User's choice:** DateOnly.FromDateTime(DateTime.UtcNow) per spec (Recommended)
**Notes:** D-09. Rasional Pertamina jam kerja 07:00-17:00 WIB → boundary 00:00-07:00 WIB (UtcNow.Date = kemarin) tidak kena workflow.

---

## Computed Properties Strategy (Q3)

| Option | Description | Selected |
|--------|-------------|----------|
| Rewrite ke DateOnly arithmetic (Recommended) | `(ValidUntil.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber)`. Preserve behavior. | ✓ |
| Delete computed props (assume unused) | Audit call site dulu. Less code maintenance. | |
| Keep DateTime arithmetic via cast | `(ValidUntil.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days`. Minimal change tapi drift subtle, defeats purpose. | |

**User's choice:** Rewrite ke DateOnly arithmetic (Recommended)
**Notes:** D-10. Files: TrainingRecord.cs:75-78, 89-92 + UnifiedTrainingRecord.cs:40.

---

## Pre-migration SQL Check Artifact (Q4)

| Option | Description | Selected |
|--------|-------------|----------|
| Inline sqlcmd di IT_NOTIFY.md + execute lokal manual (Recommended) | Pattern Phase 323/324. Zero file artifact baru. | ✓ |
| Throwaway .sql file di docs/ + delete after merge | Reusable kalau ada migrate kedua. Risk orphan kalau lupa delete. | |
| Embed cek di EF migration Up() sebagai SELECT-only logging | Auto run saat IT promo. Risk tidak idiomatic. | |

**User's choice:** Inline sqlcmd di IT_NOTIFY.md + execute lokal manual (Recommended)
**Notes:** D-11. Capture SEED_JOURNAL.md entry + IT_NOTIFY.md "MIGRATION REQUIRED" section.

---

## Rollup Props Scope (Q5, follow-up to Q1)

| Option | Description | Selected |
|--------|-------------|----------|
| Semua rollup flip DateOnly? (Recommended) | SertifikatRow.ValidUntil + LatestValidUntil + MinValidUntil + UnifiedTrainingRecord.ValidUntil + RenewalCertificateRow.TanggalExpired. UnifiedTrainingRecord.IsExpired DateOnly UtcNow konsisten Q2. Effort +20 min. | ✓ |
| Rollup props stay DateTime?, cast saat assign | Backward-compat. Risk drift. | |
| Hybrid: SertifikatRow flip, rollup stay DateTime | SertifikatRow flip (DeriveCertificateStatus signature), rollup display-passing. Pragmatic minimum. | |

**User's choice:** Semua rollup flip DateOnly? (Recommended)
**Notes:** D-08. Files: CertificationManagementViewModel.cs:74, 108 + UnifiedTrainingRecord.cs:26,40 + RenewalCertificateRow.TanggalExpired (plan-phase audit nama exact VM).

---

## Phase 326 Followup Bundle (Q6)

| Option | Description | Selected |
|--------|-------------|----------|
| Defer ke v20.0 backlog (Recommended) | Sequential strict v19.0. Phase 327 fokus pure DateOnly + migration. | ✓ |
| Bundle ke 327 (efficiency) | Sekalian sentuh TrainingAdminController.cs Edit handler. Risk scope creep + audit-uat ribet. | |

**User's choice:** Defer ke v20.0 backlog (Recommended)
**Notes:** D-12. Item defer: validator order self-renewal Edit handler + Tom Select UX.

---

## Check Audit (Q7 — user freeform "check ulang sudah benar, ada miss?")

User minta audit ulang touchpoint. Hasil audit identifikasi 3 area belum tertutup:
- Excel import controller path (ImportTraining + ClosedXML parse)
- xUnit framework pattern (vanilla Assert vs FluentAssertions vs Theory parameterized)
- JSON API endpoint serialization (AJAX partial consumer break risk)

Lanjut 3 follow-up question (Q8, Q9, Q10).

---

## Excel Import Parse (Q8)

| Option | Description | Selected |
|--------|-------------|----------|
| Audit controller dulu, then DateOnly.FromDateTime cast (Recommended) | Plan-phase task: grep ImportTraining handler. Pattern cell.GetDateTime() → DateOnly.FromDateTime. Add manual test 1 row Excel. | ✓ |
| Reject Excel import sampai user re-template | Block plan. Tidak realistis production. | |
| Skip audit, assume ClosedXML compat auto | Risk runtime exception. Not recommended. | |

**User's choice:** Audit controller dulu, then DateOnly.FromDateTime cast (Recommended)
**Notes:** D-13. Plan-phase task: grep ImportTraining + ClosedXML cell parse loop ValidUntil column.

---

## xUnit Library (Q9)

| Option | Description | Selected |
|--------|-------------|----------|
| Match Phase 325 pattern (xUnit + Assert vanilla, no FluentAssertions) (Recommended) | Konsisten HcPortal.Tests/ existing. Zero new dependency. | ✓ |
| Add FluentAssertions package | More readable. Overkill 1 test class baru. | |
| Theory + InlineData parameterized (5 case 1 method) | Compact untuk 5+ case. Combinable dengan opsi 1. | |

**User's choice:** Match Phase 325 pattern (xUnit + Assert vanilla, no FluentAssertions) (Recommended)
**Notes:** D-14. Boleh combine dengan `[Theory] + [InlineData(...)]` parameterized untuk 8 test case minimum (5 enum + boundary days=30 + null + Permanent override).

---

## JSON API Audit (Q10)

| Option | Description | Selected |
|--------|-------------|----------|
| Audit endpoint, accept default DateOnly serialization "yyyy-MM-dd" (Recommended) | System.Text.Json default. Smoke verify 5 halaman wajib partial. | ✓ |
| Explicit JsonConverter untuk DateOnly | Format spoof "yyyy-MM-ddTHH:mm:ss". Lawan tujuan migrasi. | |
| Skip audit, trust .NET 8 default + smoke | 5 halaman wajib sudah cover. Kalau ada AJAX bug, fix saat ketemu. | |

**User's choice:** Audit endpoint, accept default DateOnly serialization "yyyy-MM-dd" (Recommended)
**Notes:** D-15. Plan-phase task: grep `return Json(...)` di Controllers/+Services/ yang touch ValidUntil. Smoke browser dev tool Network tab cek response payload.

---

## Claude's Discretion

- Test file naming (CertificateStatusTests.cs vs CertificationManagementViewModelTests.cs) — pilih konsisten Phase 325 convention
- Theory data inline vs MemberData — inline cocok 8 case
- Razor format string sweep — biarkan apa adanya kalau DateOnly compat
- EF migration generated file edit — accept generated kalau valid

## Deferred Ideas (selama discussion)

- Phase 326 sisa finding (D-12) → v20.0 backlog
- DateTime.Now standardize non-ValidUntil sites → v20.0 backlog per spec §13
- Helper Clock.Today() centralized → defer indefinitely
- Explicit JsonConverter DateOnly format spoof → defer, lawan tujuan migrasi
- Computed prop delete kalau unused → audit call site task plan-phase
