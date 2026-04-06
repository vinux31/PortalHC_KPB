# Quick Task 260406-dkv: Persist auto-transition Upcoming→Open

## Description
Buat helper method untuk persist auto-transition Upcoming→Open ke DB, dan gunakan di semua controller views (CMPController + AssessmentAdminController).

## Tasks

### Task 1: Buat helper method dan terapkan di semua controller
- **files:** `Controllers/AssessmentAdminController.cs`, `Controllers/CMPController.cs`
- **action:**
  1. Buat private async method `AutoTransitionUpcomingSessions` di `AssessmentAdminController` yang:
     - Filter sessions dengan Status == "Upcoming" && Schedule <= now WIB
     - Set Status = "Open", UpdatedAt = DateTime.UtcNow
     - SaveChangesAsync jika ada perubahan
  2. Panggil di: ManageAssessment, AssessmentMonitoring, AssessmentMonitoringDetail
  3. Di CMPController: ganti display-only transition (line 228-234) dengan persist ke DB juga
  4. Di CMPController StartExam: biarkan yang sudah ada (sudah persist)
- **verify:** Cek semua 4 tempat menggunakan auto-transition yang persist
- **done:** Status Upcoming otomatis berubah ke Open di DB saat Schedule sudah lewat, dari view manapun
