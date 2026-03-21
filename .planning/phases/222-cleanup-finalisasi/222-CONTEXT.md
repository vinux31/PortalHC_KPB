# Phase 222: Cleanup & Finalisasi - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Hapus static class OrganizationStructure.cs yang sudah tidak dipakai, pastikan seed data mencakup OrganizationUnits, dan validasi ImportWorkers terhadap database OrganizationUnit. Ini phase cleanup akhir milestone v7.12.

</domain>

<decisions>
## Implementation Decisions

### Hapus OrganizationStructure.cs
- **D-01:** Hapus file `Models/OrganizationStructure.cs` sepenuhnya
- **D-02:** Hapus komentar referensi `OrganizationStructure` di 3 file (CMPController.cs, RecordsTeam.cshtml, HistoriProton.cshtml) — komentar legacy tidak perlu dipertahankan

### Seed Data
- **D-03:** Seed OrganizationUnits di SeedData.cs — ensure 4 Bagian + 19 Unit exist (idempotent, skip jika sudah ada). Migration sudah seed data awal, tapi SeedData.cs menjadi safety net untuk fresh deployment
- **D-04:** Pattern sama dengan existing seed methods — async, idempotent, self-guarded

### ImportWorkers Validasi
- **D-05:** Validasi Section dan Unit di ImportWorkers terhadap OrganizationUnit aktif di database — reject baris yang tidak cocok (tambahkan ke error list, bukan skip silent)
- **D-06:** Error message jelas: "Baris X: Section '{value}' tidak ditemukan di database" / "Unit '{value}' bukan child dari Section '{section}'"
- **D-07:** Validasi cascade — Unit harus child dari Section yang dipilih (bukan hanya exist di DB)

### Claude's Discretion
- Exact implementation detail seed method (bulk insert vs individual)
- Error message formatting di ImportWorkers

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above and REQUIREMENTS.md.

### Existing implementation references
- `Models/OrganizationStructure.cs` — File yang akan dihapus
- `Data/SeedData.cs` — Target untuk seed OrganizationUnits
- `Controllers/AdminController.cs` — ImportWorkers action yang perlu validasi
- `Data/ApplicationDbContext.cs` — Helper methods GetSectionUnitsDictAsync dll

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ApplicationDbContext.GetSectionUnitsDictAsync()` — sudah ada, bisa dipakai untuk validasi ImportWorkers
- `ApplicationDbContext.GetActiveSectionsAsync()` / `GetActiveUnitsAsync()` — helper methods dari Phase 221
- `SeedData` pattern — async, idempotent, self-guarded (lihat DeduplicateProtonTrackAssignments sebagai contoh)

### Established Patterns
- ImportWorkers sudah punya error collection pattern — tambahkan validasi Section/Unit ke flow yang sama
- Seed data di SeedData.cs menggunakan pattern check-then-create

### Integration Points
- `OrganizationStructure.cs` referensi tersisa hanya komentar (3 file) — safe to remove
- Migration `AddOrganizationUnitsAndConsolidateKkjBagian` sudah seed data awal

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

*Phase: 222-cleanup-finalisasi*
*Context gathered: 2026-03-21*
