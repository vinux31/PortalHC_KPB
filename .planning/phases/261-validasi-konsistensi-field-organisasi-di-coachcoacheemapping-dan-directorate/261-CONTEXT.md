# Phase 261: Validasi Konsistensi Field Organisasi di CoachCoacheeMapping dan Directorate - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Validasi dan perbaikan konsistensi field `AssignmentSection` dan `AssignmentUnit` di `CoachCoacheeMapping` terhadap data `OrganizationUnit` aktif. Tambah runtime validation agar inkonsistensi tidak terjadi lagi. Field `Directorate` di `ApplicationUser` **out of scope**.

</domain>

<decisions>
## Implementation Decisions

### Scope validasi
- **D-01:** One-time cleanup data existing + runtime validation pada create/edit/import — keduanya dikerjakan
- **D-02:** Hanya `CoachCoacheeMapping.AssignmentSection` dan `AssignmentUnit` yang divalidasi. `ApplicationUser.Directorate` di-skip.

### One-time cleanup
- **D-03:** Scan semua CoachCoacheeMapping yang `AssignmentSection` atau `AssignmentUnit` tidak cocok dengan OrganizationUnit aktif
- **D-04:** Auto-fix dari coachee's current User record (Section/Unit yang sudah di-cascade Phase 260)
- **D-05:** Jika coachee's User record juga invalid (Section/Unit tidak ada di OrganizationUnit), masukkan ke report — tidak auto-fix
- **D-06:** Report hasil cleanup: jumlah auto-fixed + daftar yang tidak bisa di-fix (perlu manual intervention)

### Runtime validation
- **D-07:** Saat create (CoachCoacheeMappingAssign): validasi AssignmentSection & AssignmentUnit exist di OrganizationUnit aktif
- **D-08:** Saat edit (CoachCoacheeMappingEdit): validasi sama seperti create
- **D-09:** Saat import (ImportCoachCoacheeMapping): validasi Section/Unit coachee exist di OrganizationUnit aktif sebelum assign ke mapping

### Claude's Discretion
- Mekanisme cleanup (migration script, admin action, atau startup task)
- Format report inkonsistensi (log, TempData, atau file)
- Exact error messages untuk runtime validation

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above

### Related code
- `Controllers/AdminController.cs` lines 4092-4255 — CoachCoacheeMappingAssign (create flow, validation point)
- `Controllers/AdminController.cs` lines 4282-4400 — CoachCoacheeMappingEdit (edit flow, validation point)
- `Controllers/AdminController.cs` lines 3901-4090 — ImportCoachCoacheeMapping (import flow, validation point)
- `Controllers/AdminController.cs` lines 7794-7908 — EditOrganizationUnit (Phase 260 cascade logic, reference)
- `Models/CoachCoacheeMapping.cs` — Entity definition with AssignmentSection/Unit fields
- `Models/OrganizationUnit.cs` — Hierarchy model (Level 0=Section, Level≥1=Unit)
- `Data/ApplicationDbContext.cs` — GetSectionUnitsDictAsync helper

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetSectionUnitsDictAsync()` — Returns Dictionary<string, List<string>> of active Section→Units, reusable for validation
- Phase 260 cascade pattern — Already cascades renames to CoachCoacheeMapping, same query patterns reusable

### Established Patterns
- Trim-before-save pattern already used in assign/edit flows
- Import uses atomic transaction (BEGIN...COMMIT) — cleanup should follow same pattern
- Audit logging pattern (AuditLog entries) for tracking changes

### Integration Points
- CoachCoacheeMappingAssign — Add validation after existing required-field check (line ~4104)
- CoachCoacheeMappingEdit — Add validation in update block (line ~4313)
- ImportCoachCoacheeMapping — Add validation in per-row processing loop (line ~3940)
- Phase 129 side-effect: Unit change triggers ProtonDeliverableProgress rebuild — validation prevents invalid unit names from propagating

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 261-validasi-konsistensi-field-organisasi-di-coachcoacheemapping-dan-directorate*
*Context gathered: 2026-03-26*
