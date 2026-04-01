# Phase 278: Cari Bug, Block, Error, Miss di Website - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Scan kode sumber area Assessment/Exam (CMPController) dan Admin/HC (AdminController) untuk menemukan potensi bug, error handling yang miss, fitur yang block, atau logic yang salah. Output: daftar temuan yang dilaporkan ke user untuk dipilih mana yang perlu di-fix.

</domain>

<decisions>
## Implementation Decisions

### Scope
- **D-01:** Fokus pada 2 area saja: **Assessment/Exam flow** (CMPController, exam views, SignalR hub) dan **Admin/HC** (AdminController, admin views)
- **D-02:** Area lain (CDP/Coaching, Home/Dashboard, Account/Profile) di luar scope

### Metode
- **D-03:** Claude scan kode secara proaktif — user tidak punya daftar temuan spesifik dari browser testing
- **D-04:** Claude laporkan daftar temuan dulu ke user, user pilih mana yang perlu di-fix, baru Claude fix
- **D-05:** Bukan "fix semua" — user memutuskan prioritas

### Jenis Temuan yang Dicari
- **D-06:** Bug logic (kondisi salah, edge case tidak di-handle, race condition)
- **D-07:** Error handling yang miss (exception tidak di-catch, null reference risk)
- **D-08:** Fitur yang block (user tidak bisa proceed karena logic/UI issue)
- **D-09:** Missing functionality (fitur yang seharusnya ada tapi belum diimplementasi)
- **D-10:** Data integrity issues (data bisa corrupt/inkonsisten)

### Format Laporan
- **D-11:** Setiap temuan dilaporkan dengan: lokasi kode, deskripsi masalah, severity (HIGH/MEDIUM/LOW), dan dampak ke user

### Claude's Discretion
- Urutan scan (controller dulu vs view dulu)
- Cara mengategorikan severity
- Depth of analysis per area

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard code audit approaches

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above

### Kode yang harus di-scan
- `Controllers/CMPController.cs` — Assessment CRUD, exam flow, grading, sertifikat
- `Controllers/AdminController.cs` — Kelola pekerja, unit, mapping, import/export
- `Views/CMP/` — Semua view terkait assessment dan exam
- `Views/Admin/` — Semua view terkait admin/HC
- `Hubs/AssessmentHub.cs` — SignalR real-time monitoring

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Logging pattern: `_logger.LogWarning(ex, "Context: {param}", value)` — konsisten di seluruh codebase
- Null check pattern: `if (x == null) return NotFound()` — sudah diterapkan luas

### Established Patterns
- ViewBag digunakan extensif (209 instances di AdminController) — bukan bug, tapi area rawan typo
- `.First()` tanpa null check di beberapa LINQ grouping — potential risk
- Async/await properly used, no `.Result` blocking

### Integration Points
- CMPController ↔ AssessmentHub (SignalR) untuk monitoring real-time
- AdminController ↔ multiple services (AuditLog, Notification, WorkerData)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 278-cari-bug-block-error-miss-di-website-ini*
*Context gathered: 2026-04-01*
