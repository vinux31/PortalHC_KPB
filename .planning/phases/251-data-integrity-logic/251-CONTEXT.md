# Phase 251: Data Integrity & Logic - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning
**Source:** Auto-generated (targeted bug fixes — all decisions clear from requirements)

<domain>
## Phase Boundary

Fix 6 data integrity dan logic bugs: timezone consistency (DateTime.Now → UtcNow), unique index yang terlalu ketat, validasi ValidUntil untuk bulk renewal, relax past-date validasi di EditAssessment, log warning pada bare catch RenewalFkMap, dan thread-safety refactor `_lastScopeLabel`.

</domain>

<decisions>
## Implementation Decisions

### DATA-01: DateTime.Now → DateTime.UtcNow
- **D-01:** Ganti `DateTime.Now` ke `DateTime.UtcNow` di 3 lokasi:
  - `Models/TrainingRecord.cs` line 77 (`DaysUntilExpiry` getter)
  - `Models/TrainingRecord.cs` line 91 (`IsExpiringSoon` computed)
  - `Models/CertificationManagementViewModel.cs` line 59 (`DeriveCertificateStatus`)
- **D-02:** Hanya ganti di 3 lokasi tersebut — tidak perlu audit seluruh codebase untuk DateTime.Now lainnya (scope phase ini spesifik)

### DATA-02: Composite Unique Index Migration
- **D-03:** Ubah unique index `OrganizationUnit.Name` (ApplicationDbContext.cs line 532) dari `.HasIndex(u => u.Name).IsUnique()` ke `.HasIndex(u => new { u.ParentId, u.Name }).IsUnique()`
- **D-04:** Ubah unique index `AssessmentCategory.Name` (ApplicationDbContext.cs line 559) dari `.HasIndex(c => c.Name).IsUnique()` ke `.HasIndex(c => new { c.ParentId, c.Name }).IsUnique()`
- **D-05:** Buat EF Core migration baru — naming convention: `ChangeUniqueIndexToComposite`

### DATA-03: ValidUntil Validation untuk Bulk Renewal
- **D-06:** Bug: `isRenewalModePost` (AdminController.cs line 1254) hanya cek `model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue` — ini hanya mendeteksi single renewal. Bulk renewal menggunakan `RenewalFkMap` parameter, bukan model FK fields
- **D-07:** Fix: tambahkan cek `!string.IsNullOrEmpty(RenewalFkMap)` ke `isRenewalModePost` detection agar bulk renewal juga wajib isi ValidUntil

### DATA-04: Relax Past-Date Validation di EditAssessment
- **D-08:** Hapus atau relax validasi `model.Schedule < DateTime.Today` di EditAssessment POST (AdminController.cs line 1727) — HC harus bisa edit assessment yang jadwalnya sudah lewat
- **D-09:** Pendekatan: skip past-date check jika assessment sudah ada (edit mode) — existing schedule date yang sudah lewat seharusnya tidak menghalangi edit field lain (title, duration, pass percentage)

### DATA-05: Log Warning pada Bare Catch RenewalFkMap
- **D-10:** Ganti bare `catch { /* ignore malformed map */ }` di AdminController.cs line 1437 dengan `catch (Exception ex) { _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap"); }`
- **D-11:** `_logger` sudah tersedia di AdminController — tidak perlu inject baru

### DATA-06: Thread-Safe _lastScopeLabel
- **D-12:** Refactor `_lastScopeLabel` (CDPController.cs line 661) dari private instance field ke return value dari `BuildProtonProgressSubModelAsync`
- **D-13:** Method `BuildProtonProgressSubModelAsync` (line 655) saat ini set `_lastScopeLabel = scopeLabel` — ubah agar return tuple atau tambah `ScopeLabel` ke return model
- **D-14:** Caller di `Dashboard()` (line 286) ubah dari `model.ScopeLabel = _lastScopeLabel` ke ambil dari return value

### Claude's Discretion
- Exact migration name dan timestamp
- Apakah DATA-04 hapus validasi sepenuhnya atau hanya skip untuk existing assessments — yang penting HC bisa edit assessment past-date
- Apakah DATA-06 pakai tuple return atau tambah property ke existing model — yang penting field dihapus

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — DATA-01 through DATA-06 definitions

### Source Files (targets)
- `Models/TrainingRecord.cs` line 77, 91 — `DateTime.Now` usage (DATA-01)
- `Models/CertificationManagementViewModel.cs` line 59 — `DateTime.Now` usage (DATA-01)
- `Data/ApplicationDbContext.cs` line 532, 559 — unique index definitions (DATA-02)
- `Controllers/AdminController.cs` line 1254 — `isRenewalModePost` detection (DATA-03)
- `Controllers/AdminController.cs` line 1179 — CreateAssessment POST signature with `RenewalFkMap` param (DATA-03)
- `Controllers/AdminController.cs` line 1437 — bare catch on RenewalFkMap deserialize (DATA-05)
- `Controllers/AdminController.cs` line 1727 — past-date validation in EditAssessment POST (DATA-04)
- `Controllers/CDPController.cs` line 286, 655, 661 — `_lastScopeLabel` field and usage (DATA-06)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_logger` sudah tersedia di AdminController dan CDPController — langsung pakai untuk LogWarning
- EF Core migration infrastructure sudah established — `dotnet ef migrations add` workflow standard

### Established Patterns
- Composite unique index sudah ada di beberapa entity lain — pattern `HasIndex(e => new { e.X, e.Y }).IsUnique()` sudah familiar
- `ModelState.AddModelError` pattern sudah dipakai di CreateAssessment POST untuk validasi renewal

### Integration Points
- Migration baru akan di-apply saat startup via `Database.Migrate()` — tidak perlu manual step
- `BuildProtonProgressSubModelAsync` dipanggil hanya dari `Dashboard()` — refactor scope terbatas

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard bug fix patterns apply. Semua 6 fix sudah jelas dari audit findings.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 251-data-integrity-logic*
*Context gathered: 2026-03-24 via auto mode*
