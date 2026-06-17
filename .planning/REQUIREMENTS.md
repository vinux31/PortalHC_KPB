# Requirements: Portal HC KPB — v32.1 Perbaikan Teks & Desain

**Defined:** 2026-06-17
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

> **Milestone scope:** murni perbaikan teks & tampilan (UI) pada 3 surface — halaman hasil assessment + 2 halaman Admin coach. **0 perubahan backend, 0 migration, behavior parity wajib.** Arah desain CoachCoacheeMapping = accordion card per coach (opsi "B", hasil brainstorm + visual-companion); CoachWorkload = polish-only. main pegang v32.0 di branch terpisah; branch ITHandoff ini = v32.1.

## v1 Requirements

Requirements untuk milestone v32.1. Tiap REQ map ke satu phase.

### Label / Teks (LBL)

- [ ] **LBL-03**: Pengguna melihat label **"Batas Nilai Kelulusan"** (bukan "Nilai Kelulusan") pada kartu ringkasan di halaman hasil assessment (`Views/CMP/Results.cshtml`). Nilai persen (`@Model.PassPercentage%`) di bawahnya tidak berubah.

### Desain — Coach-Coachee Mapping (DSN)

- [ ] **DSN-01**: Admin/HC melihat daftar mapping coach-coachee sebagai **accordion card per coach** — header tiap card menampilkan avatar inisial coach, nama coach, section, dan badge jumlah coachee aktif dengan warna mengikuti ambang beban (hijau normal / kuning mendekati / merah overload, konsisten dengan logika badge yang sudah ada).
- [ ] **DSN-02**: Admin/HC dapat mengklik header card coach untuk **membuka/menutup** daftar coachee (tabel/list mini di dalam card). Semua kolom data coachee yang ada saat ini (Nama, NIP, Bagian/Unit penugasan, Jabatan, Proton Track, Status, Mulai, Aksi) tetap tampil.
- [ ] **DSN-03**: Toolbar header halaman tampil **rapi & konsisten** (gaya/ukuran tombol Download Template / Import / Export / Tambah Mapping diseragamkan) dan **dead-code `onclick` sampah dihapus** pada tombol "Tambah Mapping" tanpa mengubah fungsi tombol.

### Desain — Coach Workload (DSN)

- [ ] **DSN-04**: Admin/HC melihat **filter bar** dan section **"Saran Penyeimbangan"** di halaman CoachWorkload terbungkus dalam **card** yang konsisten dengan section lain di halaman tersebut (summary cards / chart / tabel).
- [ ] **DSN-05**: Halaman CoachWorkload **bebas dari inline magic-number font-size** (11px/12px dst dipindah ke kelas/util konsisten) dan spacing antar elemen diselaraskan.

### Regresi / Parity (DSN)

- [ ] **DSN-06**: Setelah redesign, **semua aksi existing tetap berfungsi** — CoachCoacheeMapping: tambah/edit/nonaktifkan/graduated/hapus/aktifkan-kembali mapping + import & export Excel + modal assign/edit/deactivate/delete; CoachWorkload: filter section, export Excel, set threshold (Admin), setujui & lewati saran. Tidak ada perubahan endpoint/JS-contract di luar yang dibutuhkan untuk render baru.

## v2 Requirements

Tidak ada (scope kosmetik fokus; perluasan UX coach ditahan sampai ada kebutuhan nyata).

## Out of Scope

Eksklusi eksplisit untuk cegah scope creep.

| Feature | Reason |
|---------|--------|
| Perubahan backend/controller (CoachMapping/Admin/CMP) | Milestone ini murni view/JS; logika & data tak disentuh |
| Migration / perubahan skema DB | 0 migration by design |
| Redesign penuh master–detail / kanban (arah "C") | Ditolak saat brainstorm — risiko & rombak terlalu besar untuk kebutuhan |
| Halaman admin lain di luar 3 surface (Results, CoachCoacheeMapping, CoachWorkload) | Bukan keluhan; jaga scope kecil |
| Perubahan kolom data / fungsi baru (mis. sort/pencarian baru) | Bukan keluhan; hanya tata-letak & teks |

## Traceability

Diisi saat pembuatan roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| LBL-03 | TBD | Pending |
| DSN-01 | TBD | Pending |
| DSN-02 | TBD | Pending |
| DSN-03 | TBD | Pending |
| DSN-04 | TBD | Pending |
| DSN-05 | TBD | Pending |
| DSN-06 | TBD | Pending |

**Coverage:**
- v1 requirements: 7 total
- Mapped to phases: 0 (roadmap belum dibuat)
- Unmapped: 7 ⚠️ (akan diselesaikan roadmapper)

---
*Requirements defined: 2026-06-17*
*Last updated: 2026-06-17 after initial definition (milestone v32.1)*
