# Phase 185: ViewModel and Data Model Foundation - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Define SertifikatRow and CertificationManagementViewModel classes with RecordType discriminator, server-side CertificateStatus derivation, and canonical date mapping for TrainingRecord and AssessmentSession data sources.

</domain>

<decisions>
## Implementation Decisions

### Status Derivation
- CertificateStatus as **C# enum** (not string) — values: Aktif, AkanExpired, Expired, Permanent
- Threshold for AkanExpired: **30 hari** (konsisten dengan TrainingRecord.IsExpiringSoon)
- Permanent status: sertifikat dengan CertificateType="Permanent" ATAU ValidUntil null
- Assessment yang belum lulus (IsPassed != true) **tidak ditampilkan** di Certification Management

### Data Source Filtering
- **TrainingRecord**: hanya yang punya SertifikatUrl (bukan null/empty)
- **AssessmentSession**: hanya yang GenerateCertificate=true AND IsPassed=true

### Field Mapping (SertifikatRow)
- NamaWorker, **Bagian**, Unit, Judul, Kategori, RecordType (Training/Assessment), NomorSertifikat, TanggalTerbit, ValidUntil, CertificateStatus
- TanggalTerbit mapping: Training → Tanggal, Assessment → CompletedAt
- Kategori: copy apa adanya dari source (TrainingRecord.Kategori / AssessmentSession.Category)

### Display & Sorting
- Default sort: TanggalTerbit descending (terbaru dulu)
- RecordType badge: biru "Training", ungu "Assessment"
- Status badge warna: hijau=Aktif, kuning=Akan Expired, merah=Expired, abu-abu=Permanent
- Summary cards: 4 cards (Total, Aktif, Akan Expired, Expired) — Permanent masuk hitungan Total

### Claude's Discretion
- Exact enum value naming convention
- ViewModel internal structure (flat vs nested)
- Helper method placement

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Models
- `Models/TrainingRecord.cs` — Source model: ValidUntil, NomorSertifikat, CertificateType, SertifikatUrl, IsExpiringSoon, Tanggal
- `Models/AssessmentSession.cs` — Source model: ValidUntil, NomorSertifikat, GenerateCertificate, IsPassed, CompletedAt, Category

### Existing Patterns
- `Models/CDPDashboardViewModel.cs` — ViewModel pattern reference (POCO in Models/ namespace)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `TrainingRecord.IsExpiringSoon` — 30-day threshold logic (reuse same threshold for CertificateStatus.AkanExpired)
- `TrainingRecord.DaysUntilExpiry` — computed property pattern
- ViewModel POCO pattern in `Models/` folder

### Established Patterns
- ViewModels use simple POCO classes in `HcPortal.Models` namespace
- Nullable fields use `?` suffix with explicit null handling
- Status strings are pre-computed in existing models, but new CertificateStatus uses enum

### Integration Points
- Phase 186 (Role-Scoped Query) will populate SertifikatRow list
- Phase 187 (View) will consume CertificationManagementViewModel for Razor rendering
- Phase 188 (Filter Bar) needs sort option "Akan Expired dulu" — sort logic prepared in ViewModel

</code_context>

<specifics>
## Specific Ideas

- Sort opsi "Akan Expired" harus tersedia di Phase 188 — prepare sort-friendly data di ViewModel
- Badge warna sesuai roadmap: hijau/kuning/merah/abu-abu

</specifics>

<deferred>
## Deferred Ideas

- **Unified categories managed by HC** — user ingin kategori Training dan Assessment pakai set yang sama, dikelola HC di Kelola Data. Ini fitur baru (CRUD kategori + migrasi data existing) → calon phase tersendiri.

</deferred>

---

*Phase: 185-viewmodel-and-data-model-foundation*
*Context gathered: 2026-03-18*
