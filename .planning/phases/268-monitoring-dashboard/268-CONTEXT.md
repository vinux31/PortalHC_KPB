# Phase 268: Monitoring Dashboard - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT fase terakhir v10.0: menguji monitoring dashboard assessment di server development — Admin/HC memantau progress ujian real-time, lifecycle status, timer/elapsed, dan hasil setelah worker submit. Temukan bug, catat, fix batch di project lokal.

</domain>

<decisions>
## Implementation Decisions

### Real-Time Testing (MON-01, MON-03)
- **D-01:** Test full real-time dengan 2 browser bersamaan — 1 browser worker mengerjakan soal, 1 browser admin buka monitoring dashboard. Verifikasi progress update otomatis tanpa refresh (SignalR push).
- **D-02:** Jika SignalR real-time tidak berfungsi, catat sebagai bug. Fallback: test dengan manual refresh untuk verifikasi data tetap benar.

### Data Test (MON-01 sampai MON-04)
- **D-03:** Kombinasi data existing + ujian baru:
  - Data existing dari Phase 264-267 untuk verifikasi session Completed menampilkan skor & pass/fail (MON-04)
  - 1 worker mulai ujian baru supaya bisa test lifecycle lengkap Open → InProgress → Completed secara real-time (MON-02)
- **D-04:** Pakai akun yang sama dari Phase 264: admin@pertamina.com (monitor) dan rino.prasetyo@pertamina.com (worker)

### Timer/Elapsed Sinkronisasi (MON-03)
- **D-05:** Verifikasi eyeball — buka monitoring, pastikan timer/elapsed tampil, bergerak, tidak nol/negatif. Tidak perlu perbandingan eksak milidetik antara worker dan monitor.

### Kriteria Pass/Fail
- **D-06:** Sama dengan fase sebelumnya: verifikasi manual oleh user di browser, Claude analisa kode dan catat potensi bug
- **D-07:** Alur: jalankan semua skenario → kumpulkan bug → fix batch di project lokal

### Claude's Discretion
- Urutan langkah test spesifik (Claude tentukan berdasarkan analisa kode monitoring)
- Detail query/check apa yang perlu dijalankan untuk verifikasi data

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Monitoring Controller
- `Controllers/AdminController.cs` — AssessmentMonitoring (list view), AssessmentMonitoringDetail (per-group detail)

### Monitoring Views
- `Views/Admin/AssessmentMonitoring.cshtml` — List semua assessment groups dengan status ringkasan
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — Detail per-group: progress per worker, status, timer, skor

### Monitoring Models
- `Models/AssessmentMonitoringViewModel.cs` — MonitoringGroupViewModel (group summary) + MonitoringSessionViewModel (per-worker detail)

### SignalR Real-Time
- `Hubs/AssessmentHub.cs` — JoinMonitor/LeaveMonitor methods, activity logging (page_nav, reconnected, disconnected)

### Prior Phase Context
- `.planning/phases/264-admin-setup-assessment-ojt/264-CONTEXT.md` — Data test accounts & assessment setup decisions

</canonical_refs>

<deferred_ideas>
## Deferred Ideas

(none)

</deferred_ideas>
