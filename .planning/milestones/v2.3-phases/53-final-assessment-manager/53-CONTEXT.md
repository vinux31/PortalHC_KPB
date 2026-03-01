# Phase 53: Proton Assessment Exam - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

**SCOPE CHANGE from roadmap:** Phase 53 changed from "Admin manage ProtonFinalAssessment records" to "Proton Assessment Exam" — add Proton exam capability to existing Assessment/Exam system.

Ujian Proton **menggantikan** CreateFinalAssessment (level 0-5). Menggunakan infrastruktur assessment yang sudah ada (AssessmentSession, PackageQuestion, dll) dengan kategori baru "Assessment Proton" dan business rules khusus Proton.

**In scope:**
- Add "Assessment Proton" category to assessment system
- Form adaptif di CreateAssessment (track, tahun, eligible coachee filter)
- 2 tipe ujian: online (Tahun 1-2) dan interview offline (Tahun 3)
- HC + Admin akses ManageAssessment
- Move HC review to ProtonProgress, delete HCApprovals page
- Delete CreateFinalAssessment page & ProtonFinalAssessment data

**Out of scope:**
- History riwayat Proton (deferred — phase terpisah)
- Sistem notifikasi (deferred — phase terpisah)
- Export PDF riwayat per coachee (deferred)

</domain>

<decisions>
## Implementation Decisions

### Exam Types (LOCKED)
- **Tahun 1 & 2 (Operator/Panelman):** Ujian pilihan ganda online — menggunakan assessment system existing (packages, shuffling, timer, auto-save, scoring otomatis)
- **Tahun 3:** Interview offline — HC input hasil manual di monitoring detail (juri, penilaian per aspek fixed, catatan, upload dokumen pendukung)
- Soal per track berbeda (Operator vs Panelman) — HC yang tentukan soal saat create
- Passing grade bisa diatur per ujian (sama seperti assessment lain, default 70%)
- Coachee yang gagal bisa ujian ulang (tanpa batas)

### Eligibility (LOCKED)
- Hanya coachee dengan 100% deliverable penugasan yang bisa di-assign ke ujian Proton
- Saat create ujian Proton, daftar coachee otomatis di-filter hanya yang eligible (berdasarkan track & tahun yang dipilih)

### Form Adaptif di CreateAssessment (LOCKED)
- Saat category = "Assessment Proton", form menampilkan field tambahan:
  - **Track:** dropdown Operator / Panelman
  - **Tahun:** dropdown 1 / 2 / 3
- Jika Tahun 1 atau 2: field Duration, Pass %, Schedule muncul (ujian online)
- Jika Tahun 3: hanya field Schedule (tanggal interview), tanpa Duration/soal
- User picker berubah: hanya tampilkan coachee eligible (100% deliverable, track & tahun sesuai)
- Semua coachee eligible bisa dipilih (tidak dibatasi section)

### Interview Tahun 3 — Input Hasil (LOCKED)
- HC input hasil di halaman monitoring yang sama (AssessmentMonitoringDetail)
- Form: daftar juri (nama), penilaian per aspek (aspek fixed — Claude's discretion), catatan, upload dokumen
- Lulus/tidak: HC tentukan manual (bukan dari skor otomatis)
- Skor aspek bersifat informatif saja

### Access & Permissions (LOCKED)
- HC dan Admin punya akses penuh yang sama ke ManageAssessment (create, edit, delete, monitoring, export, reset, force close)
- Akses via Kelola Data Hub (Admin/Index)
- Role lain (Coach, SrSpv, SectionHead) tidak bisa manage assessment

### Category Integration (LOCKED)
- Nama kategori: "Assessment Proton"
- Di ManageAssessment: campur dengan semua assessment, ada dropdown filter category
- Di CMP/Assessment (pekerja): ujian Proton muncul bersama assessment lain, dengan badge "Assessment Proton"
- Ujian Tahun 3 muncul di CMP dengan status "Interview" (tidak ada tombol "Mulai Ujian")
- Saat coachee klik ujian Tahun 3: sebelum interview = info jadwal, setelah HC input = lihat hasil

### Hapus Page & Data (LOCKED)
- **CreateFinalAssessment** (`/CDP/CreateFinalAssessment`): hapus halaman dan controller action
- **HCApprovals** (`/CDP/HCApprovals`): hapus halaman sepenuhnya
  - HC review deliverable dilakukan via ProtonProgress (`/CDP/ProtonProgress`)
  - Notifikasi → deferred ke phase terpisah
- **ProtonFinalAssessment data**: hapus data lama di database. Tabel bisa dihapus.
- Section "Siap untuk Final Assessment" di HCApprovals: dihapus (tidak dipindah)

### Acknowledge Flow
- **Dibatalkan** — tidak ada mekanisme acknowledge dari SectionHead/HC setelah ujian

### Claude's Discretion
- Aspek penilaian fixed untuk interview Tahun 3 (Claude tentukan aspek yang sesuai)
- Exact UI implementation detail (form layout, badge styling, dll)
- Monitoring detail adaptation untuk input hasil interview
- Teknis auto-filter eligible coachee (query optimization)

</decisions>

<specifics>
## Specific Ideas

- Ujian Proton = pengganti CreateFinalAssessment (level 0-5). Bukan pelengkap, tapi pengganti.
- Form adaptif: saat pilih "Assessment Proton", form berubah (muncul Track + Tahun + eligible coachee). Saat pilih kategori lain, form normal.
- Interview Tahun 3: HC input hasil di monitoring page, termasuk upload dokumen (form interview fisik, rekaman, dll)
- Tahun 3 di sisi coachee: muncul di CMP/Assessment sebagai "Interview", klik → info jadwal, lalu setelah selesai bisa lihat hasil

</specifics>

<deferred>
## Deferred Ideas

- **History riwayat Proton** — Halaman di CDP menampilkan timeline lengkap per coachee (kapan mulai track, progress deliverable, hasil ujian). Semua role CDP bisa lihat (dengan filter section). Include export PDF. → Phase terpisah
- **Sistem notifikasi** — Alert ke HC saat coachee 100% deliverable. Menggantikan fungsi notifikasi yang ada di HCApprovals (dihapus di Phase 53). → Phase terpisah
- **Eligible list page** — Page "List Coachee Complete Penugasan Proton" di Kelola Data Hub → Tidak diperlukan, cukup filter di ProtonProgress atau implicit dari CreateAssessment eligible picker

</deferred>

---

*Phase: 53-final-assessment-manager*
*Context gathered: 2026-02-28*
