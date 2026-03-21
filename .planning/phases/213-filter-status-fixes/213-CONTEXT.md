# Phase 213: Filter & Status Fixes - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaiki 3 filter bug inti di CMP Records Team View: (1) Category+Status filter menghitung status per-kategori yang dipilih bukan global, (2) "Permanent" dihitung sebagai completed, (3) NIP search case-insensitive.

</domain>

<decisions>
## Implementation Decisions

### Category+Status Filter (FLT-01)
- **D-01:** Client-side JS filter logic harus mencocokkan status dengan kategori yang sedang difilter — saat ini status dihitung global semua kategori
- **D-02:** Behavior harus konsisten dengan personal view (Records.cshtml) yang sudah benar

### Completed Count (FLT-02)
- **D-03:** Tambah "Permanent" ke set status yang dihitung sebagai completed di WorkerDataService `completedTrainings` count
- **D-04:** Set status completed = {"Passed", "Valid", "Permanent"} — sama seperti yang sudah digunakan di `isCompleted` check (line 237)

### NIP Search (FLT-03)
- **D-05:** Lowercase `data-nip` attribute di RecordsTeam.cshtml agar konsisten dengan search filter logic yang sudah `.toLowerCase()`

### Claude's Discretion
- Exact JS refactoring approach untuk filter logic
- Apakah perlu extract shared status constants atau inline saja

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Filter logic
- `Views/CMP/RecordsTeam.cshtml` — Team View dengan client-side JS filter, data attributes, dan filter dropdowns
- `Views/CMP/Records.cshtml` — Personal view sebagai reference behavior yang benar

### Service layer
- `Services/WorkerDataService.cs` — `CompletedTrainings` count dan `isCompleted` check
- `Models/WorkerTrainingStatus.cs` — Model yang digunakan untuk worker training data

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RecordsTeam.cshtml` sudah punya filter infrastructure (dropdowns, JS filter function)
- `WorkerDataService.cs` sudah punya "Permanent" di `isCompleted` check — tinggal align `completedTrainings` count

### Established Patterns
- Client-side filtering dengan data attributes di table rows
- `.ToLower()` pattern sudah digunakan untuk `data-name` attribute

### Integration Points
- Filter JS function di RecordsTeam.cshtml — single function yang handle semua filter criteria
- `WorkerDataService.GetWorkerTrainingStatusAsync` — source of truth untuk completed count

</code_context>

<specifics>
## Specific Ideas

No specific requirements — semua fix sudah terdefinisi jelas di requirements dan success criteria.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 213-filter-status-fixes*
*Context gathered: 2026-03-21*
