---
phase: quick-16
plan: 16
subsystem: admin-hub
tags: [inventory, kelola-data, menu, read-only]
key_files:
  read:
    - Views/Admin/Index.cshtml
decisions: []
metrics:
  duration: 2 min
  completed_date: "2026-03-03"
  tasks_completed: 1
  files_modified: 0
---

# Quick Task 16: Inventaris Menu Kelola Data Hub

**One-liner:** Seluruh 9 menu card di Kelola Data Hub terdaftar lengkap per section beserta visibilitas role.

## Task 1: Daftar Semua Title Menu di Kelola Data Hub

Sumber: `Views/Admin/Index.cshtml`

Verifikasi: `grep "fw-bold"` mengembalikan 13 baris — 1 judul halaman (h2), 3 heading section (h5), 9 span menu card.

---

### Section A — Data Management

| # | Title | Deskripsi | Visibilitas |
|---|-------|-----------|------------|
| 1 | Manajemen Pekerja | Tambah, edit, hapus, dan kelola data pekerja sistem | Selalu tampil (semua role) |
| 2 | KKJ Matrix | Upload dan kelola dokumen KKJ Matrix (PDF/Excel) per bagian | Admin / HC only |
| 3 | CPDP File Management | Upload dan kelola dokumen CPDP per bagian (PDF/Excel) | Admin / HC only |
| 4 | Silabus & Coaching Guidance | Kelola silabus Proton dan file coaching guidance | Selalu tampil (semua role) |

### Section B — Proton

| # | Title | Deskripsi | Visibilitas |
|---|-------|-----------|------------|
| 5 | Coach-Coachee Mapping | Atur assignment coach ke coachee | Admin / HC only |
| 6 | Deliverable Progress Override | Override status progress deliverable | Selalu tampil (semua role) |

### Section C — Assessment & Training

| # | Title | Deskripsi | Visibilitas |
|---|-------|-----------|------------|
| 7 | Manage Assessment & Training | Kelola assessment dan training record pekerja (buat, edit, hapus, monitoring) | Admin / HC only |
| 8 | Assessment Monitoring | Pantau progress assessment real-time — lihat status grup, peserta, dan regenerate token | Admin / HC only |
| 9 | Audit Log | Lihat riwayat aktivitas pengelolaan assessment oleh Admin dan HC | Admin / HC only |

---

## Ringkasan Visibilitas

- **Selalu tampil (2 menu):** Manajemen Pekerja, Deliverable Progress Override
- **Admin/HC only (7 menu):** KKJ Matrix, CPDP File Management, Coach-Coachee Mapping, Manage Assessment & Training, Assessment Monitoring, Audit Log
- **Silabus & Coaching Guidance** tampil selalu karena berada di luar blok `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` — meskipun target action-nya di ProtonDataController yang sudah di-authorize `[Authorize(Roles="Admin,HC")]`

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Views/Admin/Index.cshtml: read and verified
- 9 menu card titles confirmed via grep (13 fw-bold total: 1 h2 + 3 h5 + 9 span)
