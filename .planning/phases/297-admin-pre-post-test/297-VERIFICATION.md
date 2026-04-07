---
phase: 297-admin-pre-post-test
verified: 2026-04-07T10:45:00Z
status: human_needed
score: 11/11 must-haves verified
re_verification: false
human_verification:
  - test: "Buka form CreateAssessment, pilih 'Pre-Post Test' di dropdown"
    expected: "Dual-section jadwal Pre dan Post muncul; field jadwal standar tersembunyi"
    why_human: "Toggle CSS/JS tidak bisa diverifikasi secara programatik"
  - test: "Submit form Pre-Post Test dengan jadwal Post lebih awal dari Pre"
    expected: "Validasi server-side gagal dengan pesan 'Jadwal Post-Test harus setelah jadwal Pre-Test'"
    why_human: "Perlu POST request ke controller yang berjalan"
  - test: "Klik baris Pre-Post di AssessmentMonitoring untuk expand sub-rows"
    expected: "Sub-row Pre-Test dan Post-Test muncul dengan stat masing-masing; chevron berputar"
    why_human: "Behavior Bootstrap collapse + JavaScript perlu browser"
  - test: "Di ManageAssessment, verifikasi badge 'Pre-Post Test' muncul pada card Pre-Post group"
    expected: "Badge rounded-pill bg-primary terpasang, tombol 'Hapus Grup' menggantikan tombol delete standar"
    why_human: "Rendering view dinamis dengan List<dynamic> perlu server berjalan"
  - test: "Klik 'Copy dari Pre-Test' di ManagePackages pada sesi Post-Test"
    expected: "Konfirmasi inline muncul; setelah konfirmasi, paket soal Pre ter-clone ke Post"
    why_human: "Perlu sesi Post-Test aktif dengan LinkedSessionId valid di database"
  - test: "Coba reset Pre-Test yang Post-Test-nya sudah Completed"
    expected: "Blokir dengan pesan 'Reset Post-Test terlebih dahulu'"
    why_human: "Perlu data DB dengan sesi Post-Test status Completed"
  - test: "Verifikasi EditAssessment untuk sesi Pre-Post menampilkan tab Pre/Post"
    expected: "Dua tab Bootstrap muncul, shared fields di luar tab, per-phase jadwal di dalam tab"
    why_human: "Rendering bergantung ViewBag.IsPrePostGroup dari server"
---

# Phase 297: Admin Pre-Post Test — Laporan Verifikasi

**Tujuan Phase:** HC dapat membuat assessment Pre-Post Test, mengatur jadwal dan paket soal terpisah untuk Pre dan Post, serta memonitor keduanya dari satu tampilan
**Diverifikasi:** 2026-04-07T10:45:00Z
**Status:** HUMAN_NEEDED
**Re-verifikasi:** Tidak — verifikasi awal

## Pencapaian Tujuan

### Kebenaran yang Dapat Diamati

| # | Kebenaran | Status | Bukti |
|---|-----------|--------|-------|
| 1 | HC dapat memilih tipe 'Pre-Post Test' di dropdown saat buat assessment baru | TERVERIFIKASI | `CreateAssessment.cshtml` baris 195: `id="assessmentTypeInput"`, option `value="PrePostTest"` |
| 2 | Dual-section jadwal Pre dan Post muncul saat tipe Pre-Post dipilih | TERVERIFIKASI | `CreateAssessment.cshtml` baris 375: `id="ppt-jadwal-section"`, input `PreSchedule` dan `PostSchedule` tersedia |
| 3 | 2 session per peserta (Pre+Post) tersimpan di DB dengan LinkedGroupId dan LinkedSessionId benar | TERVERIFIKASI | `AssessmentAdminController.cs` baris 1107: `preSessions[i].LinkedSessionId = postSessions[i].Id`, baris 1090: `AssessmentType = "PostTest"`, `LinkedGroupId = linkedGroupId` |
| 4 | Pre session selalu GenerateCertificate=false | TERVERIFIKASI | `AssessmentAdminController.cs` baris 1053: `GenerateCertificate = false, // D-20` |
| 5 | Checkbox 'Gunakan paket soal yang sama' muncul dan menampilkan info badge | TERVERIFIKASI | `CreateAssessment.cshtml` baris 419-420: `id="samePackageCheck"`, label "Gunakan paket soal yang sama untuk Pre dan Post" |
| 6 | Monitoring menampilkan Pre-Post group sebagai 1 baris expandable | TERVERIFIKASI | `AssessmentMonitoring.cshtml` baris 291-293: `ppt-sub-@group.RepresentativeId class="collapse"`, `ppt-expand-btn` pada baris 230-231 |
| 7 | Sub-row Pre dan Post menampilkan stat masing-masing | TERVERIFIKASI | `AssessmentMonitoring.cshtml` baris 305: badge `bg-info` "Pre-Test", baris 331: badge `bg-secondary` "Post-Test" |
| 8 | ManageAssessment menampilkan Pre-Post sebagai 1 card dengan badge | TERVERIFIKASI | `_AssessmentGroupsTab.cshtml` baris 170-172: `IsPrePostGroup` badge "Pre-Post Test" |
| 9 | EditAssessment Pre-Post menampilkan tab Pre/Post dengan jadwal terpisah | TERVERIFIKASI | `EditAssessment.cshtml` baris 216-339: `nav nav-tabs`, `#tab-pre`, `#tab-post` |
| 10 | DeletePrePostGroup menghapus semua Pre+Post sessions + data terkait tanpa orphan | TERVERIFIKASI | `AssessmentAdminController.cs` baris 2076: action `DeletePrePostGroup`, cascade delete PackageUserResponses→AttemptHistory→Packages→Sessions |
| 11 | Reset Pre diblokir jika Post sudah Completed | TERVERIFIKASI | `AssessmentAdminController.cs` baris 2694: `linkedPost.Status == "Completed"`, pesan "Reset Post-Test terlebih dahulu" |

**Skor: 11/11 kebenaran terverifikasi**

### Artefak yang Diperlukan

| Artefak | Deskripsi | Status | Detail |
|---------|-----------|--------|--------|
| `Models/AssessmentMonitoringViewModel.cs` | Pre-Post fields di MonitoringGroupViewModel | TERVERIFIKASI | `IsPrePostGroup`, `LinkedGroupId`, `PreSubRow`, `PostSubRow`, class `MonitoringSubRowViewModel` tersedia |
| `Controllers/AssessmentAdminController.cs` | CreateAssessment POST Pre-Post logic | TERVERIFIKASI | `isPrePostMode`, `AssessmentType = "PreTest"/"PostTest"`, `GenerateCertificate = false` |
| `Views/Admin/CreateAssessment.cshtml` | Dropdown AssessmentType + dual-section jadwal | TERVERIFIKASI | `id="assessmentTypeInput"`, `id="ppt-jadwal-section"`, `PreSchedule`, `PostSchedule` |
| `Controllers/AssessmentAdminController.cs` | Monitoring grouping by LinkedGroupId | TERVERIFIKASI | `prePostSessions`, `IsPrePostGroup = true`, `PreSubRow`, `PostSubRow` |
| `Views/Admin/AssessmentMonitoring.cshtml` | Expandable parent row + sub-rows | TERVERIFIKASI | `ppt-expand-btn`, `ppt-sub-@group.RepresentativeId`, badge Pre-Post |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | Badge Pre-Post pada card ManageAssessment | TERVERIFIKASI | `IsPrePostGroup`, badge "Pre-Post Test", `DeletePrePostGroup` form |
| `Controllers/AssessmentAdminController.cs` | EditAssessment Pre-Post GET/POST + CopyPackagesFromPre | TERVERIFIKASI | `ViewBag.IsPrePostGroup`, `CopyPackagesFromPre`, peserta sync |
| `Views/Admin/EditAssessment.cshtml` | Tab Pre/Post layout | TERVERIFIKASI | `nav nav-tabs`, `#tab-pre`, `#tab-post` |
| `Views/Admin/ManagePackages.cshtml` | Tombol Copy dari Pre-Test | TERVERIFIKASI | `Copy dari Pre-Test`, `copyPreConfirm`, `CopyPackagesFromPre` |
| `Controllers/AssessmentAdminController.cs` | DeletePrePostGroup + guards | TERVERIFIKASI | `DeletePrePostGroup`, guard `Gunakan 'Hapus Grup'`, ResetAssessment guard |

### Verifikasi Key Links

| Dari | Ke | Via | Status | Detail |
|------|----|-----|--------|--------|
| `CreateAssessment.cshtml` | `AssessmentAdminController.cs` | form POST `PreSchedule`, `PostSchedule` | TERHUBUNG | Input name `PreSchedule`/`PostSchedule` di view, parameter `DateTime? PreSchedule` di controller |
| `AssessmentAdminController.cs` | `Models/AssessmentSession` | `AssessmentType = "PreTest"/"PostTest"`, `LinkedGroupId`, `LinkedSessionId` | TERHUBUNG | Baris 1056, 1090, 1107 di controller |
| `AssessmentAdminController.cs` | `AssessmentMonitoring.cshtml` | `MonitoringGroupViewModel` dengan `IsPrePostGroup` dan `PreSubRow`/`PostSubRow` | TERHUBUNG | `IsPrePostGroup = true` baris 2330, view menggunakan `group.IsPrePostGroup` baris 227 |
| `EditAssessment.cshtml` | `AssessmentAdminController.cs` | form POST `PreSchedule`/`PostSchedule` | TERHUBUNG | `EditAssessment.cshtml` memiliki input jadwal pre/post, controller menerima `DateTime? PreSchedule` |
| `ManagePackages.cshtml` | `AssessmentAdminController.cs` | form POST `CopyPackagesFromPre` | TERHUBUNG | `asp-action="CopyPackagesFromPre"` di view, action `CopyPackagesFromPre(int postSessionId)` di controller |
| `AssessmentAdminController.cs DeletePrePostGroup` | DB `AssessmentSessions` | `LinkedGroupId == linkedGroupId` query | TERHUBUNG | Baris 2096: query `a.LinkedGroupId == linkedGroupId` |
| `AssessmentAdminController.cs ResetAssessment` | DB `AssessmentSessions LinkedSessionId` | cek Post Completed sebelum allow reset Pre | TERHUBUNG | Baris 2689-2696: guard check `linkedPost.Status == "Completed"` |

### Verifikasi Aliran Data (Level 4)

| Artefak | Variabel Data | Sumber | Menghasilkan Data Nyata | Status |
|---------|---------------|--------|------------------------|--------|
| `AssessmentMonitoring.cshtml` | `group.PreSubRow`, `group.PostSubRow` | EF Core query `_context.AssessmentSessions` + grouping by `LinkedGroupId` | Ya — query DB nyata | MENGALIR |
| `_AssessmentGroupsTab.cshtml` | `group.IsPrePostGroup`, `group.LinkedGroupId` | EF Core query ManageAssessment, `List<dynamic>` Concat | Ya — query DB nyata | MENGALIR |
| `EditAssessment.cshtml` | `ViewBag.PreSession`, `ViewBag.PostSession` | EF Core `.Include(a => a.User).Where(a => a.LinkedGroupId == ...)` | Ya — query DB nyata | MENGALIR |
| `ManagePackages.cshtml` | `ViewBag.IsPostSession` | `assessment.AssessmentType == "PostTest"` dari DB | Ya — dari DB session | MENGALIR |

### Pemeriksaan Perilaku (Step 7b)

Step 7b: DILEWATI — verifikasi perilaku interaktif memerlukan server berjalan (lihat bagian Verifikasi oleh Manusia).

### Cakupan Persyaratan

| Persyaratan | Plan Sumber | Deskripsi | Status | Bukti |
|-------------|-------------|-----------|--------|-------|
| PPT-01 | 297-01 | HC dapat memilih tipe assessment "Pre-Post Test" saat membuat assessment baru | TERPENUHI | `assessmentTypeInput` dropdown + `AssessmentTypeInput` controller parameter |
| PPT-02 | 297-01 | HC dapat mengatur jadwal dan durasi berbeda untuk Pre dan Post | TERPENUHI | `PreSchedule`, `PostSchedule`, `PreDurationMinutes`, `PostDurationMinutes` di view dan controller |
| PPT-03 | 297-03 | HC dapat mencentang "Gunakan paket soal yang sama" untuk copy paket Pre ke Post | TERPENUHI | `samePackageCheck` + `CopyPackagesFromPre` action + tombol di ManagePackages |
| PPT-04 | 297-03 | HC dapat memilih paket soal berbeda untuk Pre dan Post secara independen | TERPENUHI | Tab Pre/Post di EditAssessment masing-masing memiliki link ke ManagePackages terpisah |
| PPT-05 | 297-02 | AssessmentMonitoring menampilkan grup Pre-Post Test sebagai satu entri expandable | TERPENUHI | `ppt-expand-btn`, `ppt-sub-@group.RepresentativeId` di AssessmentMonitoring |
| PPT-06 | 297-04 | Reset Pre-Test TIDAK cascade ke Post-Test; reset Pre diblokir jika Post sudah Completed | TERPENUHI | Guard baris 2689-2696 + desain existing hanya reset 1 session (D-16) |
| PPT-07 | 297-04 | Hapus grup Pre-Post menghapus kedua sesi tanpa orphan record | TERPENUHI | `DeletePrePostGroup` cascade delete + audit log |
| PPT-08 | 297-01 | Sertifikat hanya digenerate dari hasil Post-Test | TERPENUHI | Pre session: `GenerateCertificate = false` (baris 1053); Post session: `GenerateCertificate = model.GenerateCertificate` |
| PPT-09 | 297-01 | Training Record hanya dari Post-Test | TERPENUHI | Pre session: `ValidUntil = null` (baris 1054), tidak ada path sertifikat; Post session memiliki `ValidUntil = model.ValidUntil` |
| PPT-10 | 297-02 | Pre-Post Test muncul di monitoring dengan status per-phase (Pre/Post) | TERPENUHI | Sub-row Pre-Test + Post-Test dengan `GroupStatus` masing-masing di `MonitoringSubRowViewModel` |
| PPT-11 | 297-04 | Renewal assessment bebas pilih tipe (Standard atau PrePostTest) | TERPENUHI | Dropdown `AssessmentTypeInput` tersedia di CreateAssessment; `RenewsSessionId` hanya di Post session (D-24) |

**Semua 11 persyaratan PPT-01 hingga PPT-11 TERPENUHI.**

### Anti-Pattern yang Ditemukan

| File | Baris | Pola | Tingkat Keparahan | Dampak |
|------|-------|------|-------------------|--------|
| `Controllers/AssessmentAdminController.cs` | 2293-2394 | `IEnumerable<dynamic>` cast di `SubRowStatus` local function | Info | Berpotensi runtime error jika properti tidak ada — sudah dimitigasi dengan `.Cast<dynamic>()` |
| `Controllers/AssessmentAdminController.cs` | 128-132 | `List<dynamic>` Concat ManageAssessment | Info | Type-unsafe tapi fungsional — trade-off yang diterima karena C# anonymous type limitation |

Tidak ada anti-pattern blocker atau warning yang mencegah pencapaian tujuan.

### Verifikasi oleh Manusia yang Diperlukan

#### 1. Toggle UI Pre-Post Test Form

**Tes:** Buka `/Admin/AssessmentAdmin/CreateAssessment`, pilih "Pre-Post Test" di dropdown "Tipe Assessment"
**Ekspektasi:** Section `ppt-jadwal-section` muncul (Bootstrap collapse show), field jadwal standar tersembunyi (`standard-jadwal-section` mendapat class `d-none`)
**Alasan perlu manusia:** JavaScript DOM manipulation + Bootstrap collapse tidak bisa diuji tanpa browser

#### 2. Validasi Backend PostSchedule > PreSchedule

**Tes:** Submit form Pre-Post Test dengan jadwal Post sebelum atau sama dengan Pre
**Ekspektasi:** Form dikembalikan dengan pesan error ModelState "Jadwal Post-Test harus setelah jadwal Pre-Test."
**Alasan perlu manusia:** Perlu POST request ke server yang berjalan dengan data form lengkap

#### 3. Monitoring Expandable Pre-Post Row

**Tes:** Buka `/Admin/AssessmentAdmin/AssessmentMonitoring` setelah ada data Pre-Post di DB
**Ekspektasi:** Baris Pre-Post memiliki badge "Pre-Post" dan tombol expand; klik expand menampilkan sub-row Pre-Test dan Post-Test dengan stat terpisah
**Alasan perlu manusia:** Perlu data DB aktual + interaksi browser untuk Bootstrap collapse

#### 4. ManageAssessment Badge Pre-Post

**Tes:** Buka `/Admin/AssessmentAdmin/ManageAssessment` setelah ada Pre-Post assessment
**Ekspektasi:** Card Pre-Post menampilkan badge "Pre-Post Test", tombol delete mengarah ke "Hapus Grup" bukan delete individual
**Alasan perlu manusia:** Rendering `List<dynamic>` di Razor view perlu server berjalan untuk memastikan properti `IsPrePostGroup` dibaca dengan benar

#### 5. Copy Paket Soal dari Pre ke Post

**Tes:** Buka ManagePackages untuk sesi Post-Test, klik "Copy dari Pre-Test", konfirmasi
**Ekspektasi:** Paket soal Pre ter-clone ke Post (deep clone tanpa FK ke Pre); TempData "Berhasil menyalin N paket soal dari Pre-Test"
**Alasan perlu manusia:** Perlu sesi Post-Test aktif dengan `LinkedSessionId` valid dan paket soal Pre yang sudah diisi

#### 6. Guard Reset Pre saat Post Completed

**Tes:** Coba reset Pre-Test yang sesi Post-Test-nya berstatus "Completed"
**Ekspektasi:** Redirect dengan TempData Error "Post-Test sudah selesai. Reset Post-Test terlebih dahulu sebelum mereset Pre-Test."
**Alasan perlu manusia:** Perlu data DB dengan sesi Post berstatus Completed

#### 7. EditAssessment Tab Pre/Post

**Tes:** Buka EditAssessment untuk sesi Pre-Test atau Post-Test yang punya LinkedGroupId
**Ekspektasi:** Dua tab Bootstrap muncul (Pre-Test, Post-Test) dengan badge jumlah paket; shared fields (Title, Category) di luar tab; per-phase jadwal di dalam masing-masing tab
**Alasan perlu manusia:** Rendering bergantung `ViewBag.IsPrePostGroup = true` dari server dengan data DB valid

### Ringkasan

Semua 11 persyaratan PPT-01 hingga PPT-11 telah diimplementasikan secara substansial dan terhubung dengan benar ke codebase. Implementasi mencakup:

- **Fondasi (Plan 01):** ViewModel extension, CreateAssessment POST transaksional (Pre+Post per peserta), form view dengan dropdown dan dual-section jadwal
- **Monitoring (Plan 02):** Grouping by LinkedGroupId, expandable sub-rows Pre/Post, badge ManageAssessment
- **Edit & Paket Soal (Plan 03):** Tab Pre/Post di EditAssessment, sinkronisasi peserta dengan cascade delete, CopyPackagesFromPre deep-clone
- **Integritas Data (Plan 04):** DeletePrePostGroup atomik, guard DeleteAssessment individual, guard ResetAssessment Pre-Post, RenewsSessionId hanya di Post (D-24)

Semua pemeriksaan programatik lulus. Terdapat 7 item yang memerlukan verifikasi manusia karena bergantung pada interaksi browser, server berjalan, atau data DB aktual — namun tidak ada indikasi kode yang cacat atau stub yang terdeteksi.

---

_Diverifikasi: 2026-04-07T10:45:00Z_
_Verifikator: Claude (gsd-verifier)_
