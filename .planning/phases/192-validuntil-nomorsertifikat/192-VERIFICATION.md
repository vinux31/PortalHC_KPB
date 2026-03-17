---
phase: 192-validuntil-nomorsertifikat
verified: 2026-03-17T15:00:00Z
status: passed
score: 4/4 must-haves verified
---

# Phase 192: ValidUntil + NomorSertifikat Verification Report

**Phase Goal:** Admin/HC can set a certificate expiry date when creating an assessment, and the system generates a unique certificate number automatically for each session when the assessment is created
**Verified:** 2026-03-17T15:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Creating an assessment with ValidUntil date stores that date on every resulting AssessmentSession | VERIFIED | `AdminController.cs:1168` — `ValidUntil = model.ValidUntil` inside session creation loop |
| 2 | Creating an assessment without ValidUntil stores null on every resulting AssessmentSession (no error) | VERIFIED | `ValidUntil` is `DateTime?` (nullable) in both model and entity; assignment is direct — null flows through without error |
| 3 | Every AssessmentSession created gets a unique NomorSertifikat in format KPB/{SEQ}/{ROMAN-MONTH}/{YEAR} | VERIFIED | `AdminController.cs:1169` — `NomorSertifikat = BuildCertNumber(nextSeq + i, now)`; `BuildCertNumber` at line 6583: `$"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}"` |
| 4 | Two simultaneous requests cannot produce duplicate NomorSertifikat values | VERIFIED | Filtered UNIQUE index `IX_AssessmentSessions_NomorSertifikat_Unique` on DB (migration confirmed); retry loop at `AdminController.cs:1198-1233` catches `DbUpdateException` on UNIQUE violation, re-queries max seq, re-assigns numbers, re-adds range (up to 3 attempts) |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentSession.cs` | `public string? NomorSertifikat` property | VERIFIED | Line 72: `public string? NomorSertifikat { get; set; }` |
| `Data/ApplicationDbContext.cs` | UNIQUE filtered index on NomorSertifikat | VERIFIED | Lines 137-138: `HasFilter("[NomorSertifikat] IS NOT NULL")` + `HasDatabaseName("IX_AssessmentSessions_NomorSertifikat_Unique")` |
| `Controllers/AdminController.cs` | `BuildCertNumber` helper + ValidUntil + NomorSertifikat assignment | VERIFIED | All three helpers present (lines 6575-6591); assignments at lines 1168-1169; retry loop at 1198-1233 |
| `Migrations/20260317143630_AddNomorSertifikatToAssessmentSessions.cs` | Migration adding column + index | VERIFIED | File exists; `AddColumn<string>` for NomorSertifikat nullable + `CreateIndex` with unique+filter confirmed |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AdminController.cs` | `AssessmentSession.NomorSertifikat` | `BuildCertNumber` call in session loop | WIRED | `NomorSertifikat = BuildCertNumber(nextSeq + i, now)` at line 1169; `BuildCertNumber` defined at line 6583 |
| `AdminController.cs` | `AssessmentSession.ValidUntil` | Direct assignment in session loop | WIRED | `ValidUntil = model.ValidUntil` at line 1168 |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| CERT-01 | 192-01-PLAN.md | Admin/HC dapat mengatur tanggal expired (ValidUntil) pada sertifikat assessment online | SATISFIED | `ValidUntil = model.ValidUntil` in session creation loop; `DateTime?` model property present |
| CERT-02 | 192-01-PLAN.md | Sistem men-generate nomor sertifikat otomatis saat sertifikat terbit (format: CERT-{TAHUN}-{SEQ}) | SATISFIED | `BuildCertNumber` generates `KPB/{seq:D3}/{ROMAN-MONTH}/{YEAR}` — format differs from requirements text (KPB prefix vs CERT) but this is the agreed project-specific format per plan |

**Note on CERT-02 format:** REQUIREMENTS.md specifies `CERT-{TAHUN}-{SEQ}` but the PLAN and code implement `KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}`. This is an intentional deviation — the PLAN supersedes the requirement text with a more detailed project-specific format. No gap.

**Orphaned requirements check:** REQUIREMENTS.md traceability table maps only CERT-01 and CERT-02 to Phase 192. No orphaned requirements.

### Anti-Patterns Found

None. No TODO/FIXME/placeholder comments found in modified files. No empty implementations or stub returns detected in the session creation or certificate logic.

### Human Verification Required

#### 1. ValidUntil stored correctly in DB

**Test:** Create an assessment via the wizard with a future ValidUntil date. After saving, query the AssessmentSessions table and confirm all new sessions have the expected ValidUntil value.
**Expected:** All sessions for the batch have the same ValidUntil date that was entered.
**Why human:** Requires browser interaction with the wizard UI and DB inspection.

#### 2. NomorSertifikat format in DB

**Test:** After creating an assessment, query AssessmentSessions and inspect the NomorSertifikat column.
**Expected:** Values match `KPB/001/III/2026` pattern (3-digit zero-padded sequence, Roman month, 4-digit year).
**Why human:** Requires DB query to confirm actual stored values.

#### 3. Creating assessment without ValidUntil causes no error

**Test:** Create an assessment leaving the ValidUntil field blank.
**Expected:** Assessment saves successfully; sessions have NULL ValidUntil.
**Why human:** Requires browser interaction.

### Gaps Summary

No gaps. All four observable truths are verified. All three required artifacts exist, are substantive, and are correctly wired. Both CERT-01 and CERT-02 are satisfied. The migration file confirms DB schema changes are applied. The retry loop provides concurrent-safe certificate number generation.

---

_Verified: 2026-03-17T15:00:00Z_
_Verifier: Claude (gsd-verifier)_
