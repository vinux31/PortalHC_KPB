---
phase: 327
slug: timezone-dateonly-refactor-p04
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-28
---

# Phase 327 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate xunit.runner.json) |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo --filter "FullyQualifiedName~CertificateStatusTests"` |
| **Full suite command** | `dotnet test HcPortal.sln --nologo` |
| **Estimated runtime** | ~5s quick, ~10s full (Phase 325 FileUploadHelperTests + Phase 327 CertificateStatusTests) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo --filter "FullyQualifiedName~CertificateStatusTests"`
- **After every plan wave:** Run `dotnet test HcPortal.sln --nologo`
- **Before `/gsd-verify-work`:** Full suite must be green + manual UAT 7 SC pass
- **Max feedback latency:** ~10s

---

## Per-SC Verification Map

(Phase 327 has no formal REQ-IDs; SCs documented di `.planning/ROADMAP.md:663-672`.)

| SC ID | Plan | Wave | Behavior | Test Type | Automated Command | File Exists |
|-------|------|------|----------|-----------|-------------------|-------------|
| SC-1 | TBD | TBD | EF migration apply sukses (datetime2 → date 2 tabel) | manual-only (DB schema verify) | `sqlcmd -Q "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='TrainingRecords' AND COLUMN_NAME='ValidUntil';"` returns `date` | ❌ Wave 0 procedure-based |
| SC-2 | TBD | TBD | Pre-migration zero row jam non-zero | manual-only (sqlcmd inline IT_NOTIFY.md per D-11) | sqlcmd CONTEXT.md L90-93 query | ❌ Wave 0 procedure |
| SC-3 | TBD | TBD | `DeriveCertificateStatus` 5 case + boundary days=30 + null + Permanent override pass | unit (xUnit Theory + Fact) | `dotnet test --filter "FullyQualifiedName~CertificateStatusTests" -v normal` | ❌ NEW `HcPortal.Tests/CertificateStatusTests.cs` |
| SC-4 | TBD | TBD | Add training Annual + ValidUntil today+1 → "AkanExpired" display | manual smoke (browser) + covered SC-3 | Browser POST `/Admin/AddTraining` + view `/Admin/ManageAssessment` tab Training | Manual UAT |
| SC-5 | TBD | TBD | Display 5 halaman wajib (ManageAssessment, RenewalCertificate, /CMP/Records, /CDP/CertificationManagement, Worker dashboard) tanpa jam | manual smoke (browser visual) | Browser navigate 5 routes + visual verify tanggal "15 Mar 2027" tanpa "00:00:00" | Manual UAT |
| SC-6 | TBD | TBD | PDF `/CMP/CertificatePdf/{id}` format tetap correct | manual smoke (PDF visual) | Browser `/CMP/CertificatePdf/{id}` + open PDF + visual cek "Berlaku Hingga: 15 Maret 2027" | Manual UAT |
| SC-7 | TBD | TBD | Rollback EF `Down()` migration siap (manual procedure kalau drama) | manual procedure | `dotnet ef database update {prev-migration}` di lokal pre-promo | Procedure-based |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/CertificateStatusTests.cs` — covers SC-3 (8 test method: 6 Theory `[InlineData]` + 2 Fact). 7 minimum test case per CONTEXT.md D-14:
  1. `validUntil = today + 100, type = "Annual"` → Aktif
  2. `validUntil = today + 30, type = "Annual"` → AkanExpired (boundary inclusive)
  3. `validUntil = today + 1, type = "Annual"` → AkanExpired
  4. `validUntil = today, type = "Annual"` → AkanExpired (days = 0)
  5. `validUntil = today - 1, type = "Annual"` → Expired
  6. `validUntil = null, type = null` → Expired
  7. `validUntil = today + 100, type = "Permanent"` → Permanent (validUntil ignored)
  8. `validUntil = null, type = "Permanent"` → Permanent
- [ ] Pre-migration sqlcmd inline IT_NOTIFY.md per D-11 (NOT separate file).
- [ ] Manual UAT checklist `327-UAT.md` — SC-4 + SC-5 + SC-6 + SC-7 step-by-step (planner output di final wave plan).
- [ ] Framework install: skip — Phase 325 already bootstrapped HcPortal.Tests.csproj (Microsoft.NET.Sdk + xUnit 2.9.3).

---

## Manual-Only Verifications

| Behavior | SC | Why Manual | Test Instructions |
|----------|----|------------|-------------------|
| EF migration apply schema verify | SC-1 | Schema verify hanya bisa via SQL Server INFORMATION_SCHEMA query, bukan code-level test | `sqlcmd -Q "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('TrainingRecords','AssessmentSessions') AND COLUMN_NAME='ValidUntil';"` expect `date` x2 |
| Pre-migration jam non-zero check | SC-2 | Production data integrity check, sqlcmd-only per D-11 | Per IT_NOTIFY.md draft (CONTEXT.md L90-93 reference) |
| Form Add Training + display verify | SC-4 | Browser interaction + visual render | Browser login admin → `/Admin/AddTraining` POST Annual + ValidUntil today+1 → navigate `/Admin/ManageAssessment` tab Training → cek badge "AkanExpired" |
| 5 halaman wajib display | SC-5 | Visual render verification across 5 routes | Browser navigate + visual checklist per route, verify tanggal tanpa "00:00:00" suffix |
| PDF QuestPDF render | SC-6 | PDF binary output visual verify | Browser `/CMP/CertificatePdf/{id}` → open PDF → cek "Berlaku Hingga: 15 Maret 2027" |
| Rollback EF Down() | SC-7 | Procedure-based, only if drama | Lokal `dotnet ef database update {prev-migration}` → snapshot restore → fix + retry |
| JSON consumer JS timezone smoke | (pitfall mitigation) | Frontend JS render verify | Browser `/CMP/AnalyticsDashboard` → cek badge "sisa hari" off-by-one absence |
| Razor TagHelper DateOnly format smoke | (pitfall mitigation) | Form binding value attribute verify | Browser inspect `<input asp-for="ValidUntil">` value="2027-03-15" not "15-3-2027" |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify command or Wave 0 dependency
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (CertificateStatusTests.cs + IT_NOTIFY.md + 327-UAT.md)
- [ ] No watch-mode flags (xUnit single-run only)
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter after Wave 0 complete

**Approval:** pending
