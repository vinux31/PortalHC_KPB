# Phase 200: Renewal Chain Foundation - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

AssessmentSession dan TrainingRecord memiliki kolom renewal chain (RenewsSessionId, RenewsTrainingId). BuildSertifikatRowsAsync dapat menentukan apakah suatu sertifikat sudah di-renew secara akurat via flag IsRenewed di SertifikatRow.

</domain>

<decisions>
## Implementation Decisions

### Renewal Chain Logic
- Semua renewal attempt tercatat di chain, baik gagal maupun lulus. IsRenewed hanya true jika ada renewal yang IsPassed==true (AssessmentSession) atau ada TrainingRecord renewal (selalu dianggap lulus)
- Satu renewal session/record hanya menunjuk ke SATU sertifikat asal — RenewsSessionId ATAU RenewsTrainingId, tidak keduanya sekaligus
- Renewal bisa multi-level chain (A → B → C). Sertifikat hasil renewal bisa di-renew lagi
- Renewal selalu via assessment baru ATAU TrainingRecord baru — kedua jalur didukung
- TrainingRecord juga punya RenewsTrainingId (FK self) dan RenewsSessionId (FK ke AssessmentSession), mirror dari AssessmentSession. Chain bisa: Training → Training, Training → Assessment, Assessment → Assessment, Assessment → Training
- TrainingRecord selalu dianggap "lulus" (data manual HC, tidak punya konsep IsPassed)

### FK Design
- Kedua FK nullable di masing-masing tabel (AssessmentSession dan TrainingRecord)
- Constraint salah-satu-saja (XOR) divalidasi di application code, bukan CHECK constraint di DB — konsisten dengan pattern existing
- ON DELETE SET NULL — jika sertifikat asal dihapus, renewal FK jadi NULL, chain putus tapi data tetap ada
- Index strategy: Claude's discretion berdasarkan query pattern

### IsRenewed Flag Behavior
- Hanya bool IsRenewed di SertifikatRow, tanpa info tambahan (detail via Certificate History modal Phase 203)
- BuildSertifikatRowsAsync cek kedua tabel: sertifikat renewed jika ada AssessmentSession (IsPassed==true) ATAU TrainingRecord yang me-renew-nya
- Sertifikat tanpa renewal yang lulus tetap IsRenewed=false meskipun ada attempt gagal

### Claude's Discretion
- Index strategy untuk FK columns
- Query optimization di BuildSertifikatRowsAsync (batch vs per-row lookup)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Model
- `Models/AssessmentSession.cs` — Current AssessmentSession model, target for RenewsSessionId/RenewsTrainingId addition
- `Models/CertificationManagementViewModel.cs` — SertifikatRow class, target for IsRenewed flag addition

### Query Logic
- `Controllers/CDPController.cs` lines 3187-3336 — BuildSertifikatRowsAsync, target for renewal chain resolution

### Requirements
- `.planning/REQUIREMENTS.md` — RENEW-01 (FK fields), RENEW-02 (IsRenewed logic)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BuildSertifikatRowsAsync` (CDPController.cs:3187): Already queries both AssessmentSessions and TrainingRecords, merges into SertifikatRow list. Natural place to add renewal chain resolution.
- `SertifikatRow.DeriveCertificateStatus()`: Existing status derivation logic. IsRenewed is orthogonal to this — a certificate can be Expired AND Renewed.

### Established Patterns
- EF Core migrations with nullable FK columns (seen in ProtonTrackId pattern on AssessmentSession)
- No CHECK constraints in existing migrations — all validation in application code
- Anonymous projection → DTO mapping pattern in BuildSertifikatRowsAsync

### Integration Points
- `AssessmentSession` model: Add RenewsSessionId, RenewsTrainingId
- `TrainingRecord` model: Add RenewsTrainingId (self-FK), RenewsSessionId (FK to AssessmentSession)
- `SertifikatRow`: Add IsRenewed property
- `BuildSertifikatRowsAsync`: Add renewal chain resolution query
- `AppDbContext.OnModelCreating`: Configure FK relationships and ON DELETE SET NULL

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 200-renewal-chain-foundation*
*Context gathered: 2026-03-19*
