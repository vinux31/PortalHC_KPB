# Requirements — Milestone v32.0 Manajemen Peserta

**Milestone:** v32.0 — Manajemen Peserta
**Created:** 2026-06-17
**Goal:** HC dapat mengelola peserta assessment dengan lancar — penambahan peserta tetap fleksibel saat ujian berjalan (dengan pemberitahuan jelas, dikunci regression test), dan halaman `/Admin/CreateWorker` kembali bisa dipakai (field Nama Lengkap & Email tidak lagi terkunci) dengan semua field terverifikasi berfungsi.
**Sumber:** Investigasi multi-agent 2026-06-17 (workflow `manajemen-peserta-investigasi`) — root cause & file ter-peta, adversarial-verified.
**Konteks:** `AssessmentSession` = per-peserta ("tambah peserta" = INSERT sesi baru via blok BULK ASSIGN `EditAssessment`). `/Admin/CreateWorker` = buat akun pegawai (bukan peserta assessment) → fix view-only. **0 migration.** Branch ITHandoff; verifikasi lokal `dotnet build` + Playwright.

---

## Requirements v32.0

### Penambahan Peserta Fleksibel (PART) — fitur 1.1

- [ ] **PART-01**: HC dapat menambah peserta baru ke sebuah assessment yang **sedang berjalan** (ada peserta lain berstatus `InProgress` / sudah masuk ujian) tanpa diblokir — penambahan tetap berhasil membuat `AssessmentSession` baru per peserta dan peserta baru mewarisi status induk sehingga bisa langsung mengerjakan (selama window ujian belum ditutup).
- [ ] **PART-02**: Guard status `Completed` pada sesi representatif di `EditAssessment` **tidak salah-memblokir penambahan peserta** ketika sebagian peserta sudah selesai sementara grup assessment masih aktif — HC tetap dapat menambah peserta selama window ujian (`ExamWindowCloseDate`) belum lewat.
- [ ] **PART-03**: Saat HC menambah peserta ke assessment yang ada peserta `InProgress`, sistem menampilkan **notice informatif** (bernuansa informasi, bukan peringatan kesan-error) yang menjelaskan bahwa peserta baru tetap bisa ditambah walau ujian sedang berjalan — menggantikan warning kosmetik yang ambigu.
- [ ] **PART-04**: Perilaku penambahan-peserta-saat-berjalan **dikunci oleh automated regression test** — memverifikasi: (a) penambahan saat ada `InProgress` berhasil, (b) peserta baru mewarisi status induk, (c) jawaban/sesi peserta existing tidak ter-overwrite oleh proses BULK ASSIGN.

### Perbaikan Halaman CreateWorker (WRKR) — fitur 1.2

- [ ] **WRKR-01**: HC/Admin dapat **mengetik** field "Nama Lengkap" dan "Email" di `/Admin/CreateWorker` di **semua environment** (termasuk saat `Authentication:UseActiveDirectory=true` di Dev/Prod) — field tidak lagi `readonly`, AD auth tetap aktif, sehingga halaman dapat dipakai membuat pekerja baru.
- [ ] **WRKR-02**: Field Email memvalidasi format email (`type="email"`) dan setiap field menampilkan **pesan validasi inline per-field** (Nama Lengkap, Email, Jabatan, Directorate, Bagian, Unit) — bukan hanya muncul di ringkasan error atas halaman.
- [ ] **WRKR-03**: **Semua field** di `/Admin/CreateWorker` berfungsi end-to-end terverifikasi runtime — NIP, Tanggal Bergabung, Jabatan, Directorate, cascade Bagian→Unit, Role (default + level), Password/Konfirmasi (mode lokal) / info auto-generate (mode AD) — dan HC dapat menyelesaikan **submission membuat pekerja baru dengan sukses** (record tersimpan, redirect ke daftar pekerja).

---

## Future Requirements (deferred)

- **Konfirmasi opsional saat tambah peserta ke ujian live** — dialog konfirmasi "ada peserta sedang mengerjakan, lanjut tambah?" sebagai opsi UX. Ditangguhkan: user memilih perilaku **fleksibel tanpa friksi** untuk v32.0 (PART-01/03 cukup notice informatif).
- **Bulk import peserta langsung ke assessment via Excel** — saat ini Excel import hanya untuk akun pegawai (`ImportWorkers`); assign-ke-assessment manual pilih user. Out of current scope.

---

## Out of Scope (eksklusi eksplisit)

- **Hard-block / cegah penambahan peserta saat `InProgress`** — bertentangan dengan keputusan user (fleksibel). Tidak diimplementasikan.
- **Perubahan controller/model `CreateWorker`** — POST handler (`WorkerController.CreateWorker`) sudah benar memetakan `FullName`/`Email` dari form + auto-generate password di mode AD; fix 1.2 = **view-only**. Model `ManageUserViewModel` tak diubah.
- **Provisioning/sinkronisasi akun AD otomatis** — pembuatan akun manual via CreateWorker tetap; integrasi penuh AD-sync di luar scope.
- **Migration / perubahan skema DB** — milestone ini **0 migration** (semua view + logic).

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| PART-01 | TBD | pending |
| PART-02 | TBD | pending |
| PART-03 | TBD | pending |
| PART-04 | TBD | pending |
| WRKR-01 | TBD | pending |
| WRKR-02 | TBD | pending |
| WRKR-03 | TBD | pending |

_(Phase mapping diisi oleh roadmapper.)_
