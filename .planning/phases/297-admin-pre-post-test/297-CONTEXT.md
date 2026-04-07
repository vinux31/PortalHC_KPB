# Phase 297: Admin Pre-Post Test - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat membuat assessment Pre-Post Test, mengatur jadwal dan paket soal terpisah untuk Pre dan Post, serta memonitor keduanya dari satu tampilan. Tidak termasuk worker-side flow (Phase 299) atau question types baru (Phase 298).

</domain>

<decisions>
## Implementation Decisions

### Create Assessment Flow
- **D-01:** Dropdown AssessmentType di form CreateAssessment: "Standard" (default, behavior existing) dan "Pre-Post Test". Saat pilih Pre-Post, form expand menampilkan dual-section jadwal
- **D-02:** Dual-section expand untuk jadwal — 2 section: "Pre-Test" dan "Post-Test", masing-masing punya input Schedule, DurationMinutes, dan ExamWindowCloseDate sendiri
- **D-03:** Field sharing: Title, Category, PassPercentage, IsTokenRequired, AccessToken, AllowAnswerReview, Status = shared (1x input). Schedule, DurationMinutes, ExamWindowCloseDate = per Pre/Post. GenerateCertificate dan ValidUntil = hanya relevan untuk Post
- **D-04:** Peserta sama untuk Pre dan Post — HC pilih peserta sekali, otomatis di-assign ke Pre DAN Post
- **D-05:** 2 session per peserta — setiap peserta dapat 1 Pre session + 1 Post session. 10 peserta = 20 session total
- **D-06:** Validasi jadwal: Schedule Pre harus sebelum Schedule Post. Frontend disable tanggal Post < Pre, backend enforce

### Paket Soal
- **D-07:** Paket soal dikelola via ManagePackages (flow existing) — CreateAssessment hanya buat session, paket dikelola terpisah
- **D-08:** Di ManagePackages Post ada tombol "Copy dari Pre-Test" untuk clone semua paket Pre (Questions + Options) ke Post
- **D-09:** Checkbox "Gunakan paket soal yang sama" di CreateAssessment — saat checked, UI realtime mirror menampilkan paket Post sama dengan Pre

### Monitoring Display
- **D-10:** AssessmentMonitoring menampilkan grup Pre-Post sebagai 1 baris parent expandable dengan badge "Pre-Post". Klik expand menampilkan 2 sub-row: Pre-Test dan Post-Test
- **D-11:** Parent row stat gabungan: total peserta, completed (Post saja), passed (Post). Badge progress menunjukkan overall
- **D-12:** Sub-row Pre/Post masing-masing punya link ke AssessmentMonitoringDetail (halaman existing per-peserta)
- **D-13:** Aksi bulk (Akhiri Semua Ujian, Reshuffle All) berlaku per-phase, bukan per-grup. HC harus expand lalu klik aksi di sub-row yang diinginkan
- **D-14:** ManageAssessment menampilkan Pre-Post sebagai 1 card dengan badge khusus
- **D-15:** EditAssessment untuk Pre-Post menggunakan Tab Pre / Post di dalam halaman edit. HC switch tab untuk edit jadwal masing-masing dan akses ManagePackages per phase

### Cascade & Delete
- **D-16:** Reset Pre-Test TIDAK otomatis cascade ke Post-Test. Hanya Pre yang di-reset, Post tetap utuh
- **D-17:** TAPI: block reset Pre jika Post sudah Completed. HC harus reset Post dulu baru bisa reset Pre
- **D-18:** Delete grup = hapus kedua session (Pre + Post) + semua paket soal, jawaban, assignment, dan responses. Modal konfirmasi menampilkan dampak
- **D-19:** Tidak bisa hapus Pre saja atau Post saja — hapus harus per-grup. Tombol delete individual tidak muncul untuk session bagian Pre-Post pair

### Sertifikat & TrainingRecord
- **D-20:** Pre session: GenerateCertificate = false, NomorSertifikat = null. Pre tidak pernah generate sertifikat
- **D-21:** Post session: GenerateCertificate = pilihan HC. NomorSertifikat di-generate oleh GradingService saat Post selesai dan IsPassed=true (flow existing, zero change)
- **D-22:** TrainingRecord hanya dari Post-Test. Pre tidak buat TrainingRecord (otomatis karena Pre GenerateCertificate=false)

### Renewal
- **D-23:** Renewal dari sertifikat Pre-Post: HC bebas pilih tipe (Standard atau Pre-Post Test). Tidak dipaksa Pre-Post lagi
- **D-24:** RenewsSessionId pada renewal Post session baru = Id Post session lama. Pre session baru tidak punya RenewsSessionId

### Data Model & Linking
- **D-25:** Pakai kedua kolom: LinkedGroupId (batch group ID, shared semua Pre+Post) + LinkedSessionId (pair lookup per peserta, Pre↔Post)
- **D-26:** AssessmentType values: 'PreTest' untuk Pre session, 'PostTest' untuk Post session, null untuk Standard
- **D-27:** AssessmentPhase tidak dipakai untuk Phase 297 — tetap null untuk Pre-Post sessions
- **D-28:** 2 session per peserta: Pre dan Post session terpisah di tabel AssessmentSessions

### Status Lifecycle
- **D-29:** Status grup di monitoring derived (dihitung dinamis), bukan disimpan sebagai field
- **D-30:** Pre-Test status ikuti lifecycle standard: Upcoming/Open/InProgress/Completed/Cancelled. Tidak ada logic khusus

### Edit & Peserta Management
- **D-31:** Tambah peserta via EditAssessment = otomatis buat Pre+Post session baru untuk peserta tsb
- **D-32:** Hapus peserta = hapus kedua session (Pre+Post). Validasi: tidak bisa hapus jika Pre atau Post sudah InProgress/Completed
- **D-33:** Monitoring grouping: Standard assessment tetap GROUP BY Title+Category+ScheduleDate (existing). Pre-Post pakai GROUP BY LinkedGroupId

### Claude's Discretion
- LinkedGroupId value strategy (ID Pre session pertama vs counter terpisah)
- Exact UI layout dual-section expand di CreateAssessment
- Tab styling di EditAssessment Pre-Post
- Badge visual design untuk Pre-Post di monitoring dan manage
- Copy paket soal implementation detail (deep clone vs reference)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment Admin Controller
- `Controllers/AssessmentAdminController.cs` — CreateAssessment GET/POST (baris 517-1157), EditAssessment (baris 1164-1489), DeleteAssessment (baris 1490-1587), DeleteAssessmentGroup (baris 1588-1660), AssessmentMonitoring (baris 1763+), ManagePackages (baris 3002+)

### Models
- `Models/AssessmentSession.cs` — Target model. Kolom v14.0 sudah ada: AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading
- `Models/AssessmentPackage.cs` — PackageQuestion, PackageOption structure. Target copy paket Pre→Post
- `Models/AssessmentMonitoringViewModel.cs` — MonitoringGroupViewModel yang harus di-extend untuk Pre-Post display

### Views
- `Views/Admin/CreateAssessment.cshtml` — Form yang akan ditambah dropdown AssessmentType + dual-section jadwal
- `Views/Admin/EditAssessment.cshtml` — Target tab Pre/Post layout
- `Views/Admin/AssessmentMonitoring.cshtml` — Target expandable Pre-Post grup display
- `Views/Admin/ManageAssessment.cshtml` — Target 1-card Pre-Post badge

### Services
- `Services/GradingService.cs` — GradingService yang sudah terekstrak di Phase 296. Handle sertifikat dan TrainingRecord generation

### Requirements
- `.planning/REQUIREMENTS.md` — PPT-01 sampai PPT-11

### Prior Context
- `.planning/phases/296-data-foundation-gradingservice-extraction/296-CONTEXT.md` — D-01 sampai D-09, kolom baru, GradingService design

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GradingService.GradeAndCompleteAsync()` — handle sertifikat + TrainingRecord. Pre-Post hanya perlu set GenerateCertificate=false pada Pre session
- `CertNumberHelper.Build()` + `GetNextSeqAsync()` — generate NomorSertifikat, sudah dipanggil dari GradingService
- `MonitoringGroupViewModel` — viewmodel monitoring existing, perlu extend untuk Pre-Post sub-rows
- `ManagePackages` action + view — halaman kelola paket soal existing, perlu tambah tombol "Copy dari Pre"

### Established Patterns
- Assessment grouping di monitoring: GROUP BY Title+Category+ScheduleDate — Pre-Post akan pakai LinkedGroupId
- CreateAssessment POST: loop per UserId, buat session per user dalam transaction with retry
- DeleteAssessment/DeleteAssessmentGroup: cascade cleanup PackageUserResponses → AssessmentPackages → Questions → Options → Sessions
- Renewal pre-fill: model pre-populated dari source session/training

### Integration Points
- `CreateAssessment POST` — harus buat 2 session per user (Pre+Post) dengan LinkedGroupId dan LinkedSessionId
- `EditAssessment` — harus support tab Pre/Post, sinkron peserta
- `AssessmentMonitoring` — harus detect Pre-Post via AssessmentType/LinkedGroupId dan render expandable
- `ManagePackages` — harus tambah "Copy dari Pre" button untuk Post session

</code_context>

<specifics>
## Specific Ideas

- Checkbox "Gunakan paket soal yang sama" dengan UI realtime mirror (bukan copy saat submit)
- Reset Pre di-block jika Post Completed — HC harus reset Post dulu
- Delete harus per-grup, tidak bisa delete Pre/Post individual
- Peserta management sinkron — tambah/hapus peserta otomatis apply ke Pre+Post

</specifics>

<deferred>
## Deferred Ideas

- **Detail gabungan Pre vs Post side-by-side per peserta** — bisa jadi enhancement di Phase 299 (Worker Pre-Post Test + Comparison)
- **AssessmentPhase multi-tahap (Phase1/Phase2/Phase3)** — kolom sudah ada, tapi use case belum ada. Bisa jadi phase future

### Reviewed Todos (not folded)
- `realtime-assessment.md` (score 0.6) — file kosong/tanpa detail, tidak relevan untuk Phase 297

</deferred>

---

*Phase: 297-admin-pre-post-test*
*Context gathered: 2026-04-07*
