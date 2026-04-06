# Quick Task 260406-dkv: Persist auto-transition Upcoming→Open

## Problem
Status assessment "Upcoming" tidak otomatis berubah ke "Open" di halaman HC/Admin (ManageAssessment, AssessmentMonitoring, MonitoringDetail) meskipun Schedule sudah lewat. Hanya worker view yang punya display-only transition.

## Solution
- Tambah helper `AutoTransitionUpcomingSessions()` di `AssessmentAdminController` yang bulk-update semua session Upcoming dengan Schedule <= now WIB, persist ke DB
- Panggil di: ManageAssessment, AssessmentMonitoring, AssessmentMonitoringDetail
- CMPController worker view: ubah dari display-only ke persist ke DB
- StartExam (CMPController): sudah persist, tidak diubah

## Files Changed
- `Controllers/AssessmentAdminController.cs` — helper method + 3 call sites
- `Controllers/CMPController.cs` — display-only → persist

## Commit
08ed4d6b
