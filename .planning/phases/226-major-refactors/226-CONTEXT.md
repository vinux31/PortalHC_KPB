# Phase 227: Major Refactors - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Migrasi legacy question path ke package format, cleanup orphan tables (AssessmentCompetencyMap, UserCompetencyLevel), dan fix timing NomorSertifikat generation. **Question Bank CRUD terpisah (QBNK-01/02/03) di-skip** — soal tetap dikelola lewat ManagePackages yang sudah ada.

Scope aktif: CLEN-02, CLEN-03, CLEN-04 saja.

</domain>

<decisions>
## Implementation Decisions

### Question Bank Scope
- **D-01:** QBNK-01, QBNK-02, QBNK-03 di-SKIP seluruhnya. Tidak ada halaman Question Bank terpisah.
- **D-02:** Soal tetap dikelola lewat ManagePackages + ImportPackageQuestions yang sudah ada (terikat ke assessment session).

### Migrasi Legacy Path (CLEN-02)
- **D-03:** Strategi: data migration script — convert semua legacy session data (AssessmentQuestion/AssessmentOption/UserResponse) ke PackageQuestion/PackageOption/PackageUserResponse format.
- **D-04:** Perlu cek session legacy yang masih aktif (belum submitted) sebelum migrasi dan handle secara khusus.
- **D-05:** Post-migrasi: drop tabel legacy (AssessmentQuestion, AssessmentOption, UserResponse) setelah data terverifikasi lengkap. Hapus semua legacy code path dari CMPController.

### Cleanup Orphan Tables (CLEN-03)
- **D-06:** Drop tabel AssessmentCompetencyMap dan UserCompetencyLevel — tidak ada data aktif, tabel orphan.

### NomorSertifikat Timing (CLEN-04)
- **D-07:** Pindahkan NomorSertifikat generation dari CreateAssessment ke SubmitExam + IsPassed = true.
- **D-08:** Bad data handling: migration script set NomorSertifikat = NULL untuk semua session dengan IsPassed != true.
- **D-09:** Manual entry (AddTraining/ImportTraining) tetap boleh input NomorSertifikat custom. Auto-generate hanya untuk assessment flow.

### Claude's Discretion
- Urutan eksekusi (migrasi dulu vs NomorSertifikat fix dulu)
- Detail migration script (batch size, error handling, rollback strategy)
- Exact verification query sebelum drop tables

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Legacy question path
- `Models/AssessmentQuestion.cs` — Legacy model yang akan dimigrasi
- `Models/UserResponse.cs` — Legacy user response model
- `Models/AssessmentPackage.cs` — Target format (PackageQuestion, PackageOption)
- `Models/PackageUserResponse.cs` — Target user response format
- `Controllers/CMPController.cs` lines 324-370 — SaveLegacyAnswer action (to be removed)
- `Controllers/CMPController.cs` lines 954-1002 — Legacy exam path branching (to be removed)

### Orphan tables
- `Models/Competency/AssessmentCompetencyMap.cs` — Orphan model to drop
- `Models/Competency/UserCompetencyLevel.cs` — Orphan model to drop
- `Data/SeedCompetencyMappings.cs` — Seed data to remove

### NomorSertifikat
- `Controllers/AdminController.cs` lines 1362-1401 — Current generation at CreateAssessment (to be moved)
- `Controllers/CMPController.cs` — SubmitExam action (target location for generation)
- `Controllers/AdminController.cs` lines 5676-5875 — AddTraining/EditTraining manual entry (keep as-is)

### Database context
- `Data/ApplicationDbContext.cs` — DbSet registrations to update

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ImportPackageQuestions` (AdminController:6399) — Existing Excel import for package questions, pattern reusable
- `PackageQuestion`/`PackageOption` models — Target format sudah mature dan digunakan production
- `BuildCertNumber` (AdminController) — Existing NomorSertifikat generation logic, needs relocation

### Established Patterns
- Migration scripts via EF Core Migrations — established pattern for schema changes
- Audit logging pattern di AdminController — migration actions should log

### Integration Points
- `CMPController.TakeExam` — Dual-path branching (legacy vs package) yang harus di-unify
- `CMPController.SubmitExam` — Target untuk NomorSertifikat generation
- `ApplicationDbContext` — DbSet removal untuk orphan + legacy tables

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- **Question Bank terpisah (QBNK-01/02/03)** — User memutuskan tidak perlu saat ini. Soal tetap dikelola per assessment session lewat ManagePackages. Bisa jadi phase terpisah di masa depan jika kebutuhan berubah.

</deferred>

---

*Phase: 227-major-refactors*
*Context gathered: 2026-03-22*
