# Phase 244: UAT Monitoring & Analytics - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT murni untuk fitur monitoring ujian real-time, token management, export hasil ujian, dan analytics dashboard. Semua fitur sudah terbangun — fase ini memverifikasi bahwa semuanya berfungsi end-to-end sesuai requirements MON-01 s/d MON-04.

</domain>

<decisions>
## Implementation Decisions

### Skenario SignalR Real-time (MON-01)
- **D-01:** Gunakan pendekatan dual browser — worker mengerjakan ujian di browser A, HC membuka AssessmentMonitoringDetail di browser B. Verifikasi stat cards dan status per-user diperbarui secara live tanpa refresh halaman.

### Token Management Flow (MON-02)
- **D-02:** Tes dengan linear sequence satu flow panjang berurutan: copy token → regenerate token → verifikasi token lama invalid → force close ujian peserta → reset peserta → peserta bisa ujian ulang. Satu skenario realistis sesuai workflow HC.

### Export Excel (MON-03)
- **D-03:** Level validasi export Excel diserahkan ke Claude's discretion — pilih pendekatan yang paling masuk akal antara cek struktur saja atau full data match.

### Analytics Dashboard Filter (MON-04)
- **D-04:** Test semua kombinasi cascading filter: Bagian saja, Bagian+Unit, Bagian+Unit+Kategori, plus reset filter. Verifikasi fail rate, trend skor, ET breakdown, dan expiring soon tampil dengan benar di setiap kombinasi.

### Claude's Discretion
- Level validasi data Export Excel (D-03) — Claude memilih antara structural check atau full data match

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §Monitoring — MON-01 s/d MON-04

### Existing Implementation
- `Controllers/AdminController.cs` — AssessmentMonitoring (line ~2225), AssessmentMonitoringDetail (line ~2325), RegenerateToken (line ~2155), ResetExamSession, ForceCloseExam, ForceCloseAllExams, ExportAssessmentResults (line ~3019)
- `Hubs/AssessmentHub.cs` — SignalR hub: JoinBatch, LeaveBatch, JoinMonitor, LeaveMonitor
- `Controllers/CMPController.cs` — AnalyticsDashboard (line ~2051)
- `Views/CMP/AnalyticsDashboard.cshtml` — Analytics dashboard view dengan cascading filter
- `Models/AnalyticsDashboardViewModel.cs` — ViewModel analytics

### Seed Data
- `.planning/REQUIREMENTS.md` §Seed Data — SEED-01 s/d SEED-07 (sudah shipped di phase 241)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentHub` (SignalR): Sudah ada group management untuk batch dan monitor channels
- `AssessmentMonitoring` view: Halaman list assessment dengan search, status, category filter
- `AssessmentMonitoringDetail` view: Detail per-assessment dengan stat cards, status badge per-user, timer info
- `ExportAssessmentResults`: Export ke Excel sudah implemented
- `AnalyticsDashboard`: Dashboard dengan Chart.js, cascading filter via AJAX, heatmap styling

### Established Patterns
- Monitoring actions di `AdminController` dengan `[Authorize(Roles = "Admin, HC")]`
- SignalR groups: `batch-{batchKey}` untuk exam takers, `monitor-{batchKey}` untuk HC monitoring
- TempData pattern untuk success/error messages setelah actions (force close, reset, etc.)

### Integration Points
- SignalR hub di `/assessmentHub` (mapped di Program.cs)
- Analytics dashboard diakses via CMP hub
- Monitoring diakses via Admin controller

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

*Phase: 244-uat-monitoring-analytics*
*Context gathered: 2026-03-24*
