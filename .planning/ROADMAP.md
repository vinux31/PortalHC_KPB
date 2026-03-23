# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- ✅ **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (shipped 2026-03-21)
- ✅ **v8.0 Assessment Integrity & Analytics** - Phases 223–227 (shipped 2026-03-22)
- ✅ **v8.1 Renewal & Assessment Ecosystem Audit** - Phases 228–232 (shipped 2026-03-22)
- 🚧 **v8.2 Proton Coaching Ecosystem Audit** - Phases 233–238 (in progress)

---

<details>
<summary>✅ v1.0–v8.1 (Phases 1–232) - SHIPPED</summary>

All prior milestones shipped. See MILESTONES.md for full detail.

Last completed phase: 232 (v8.1 — Audit Assessment Flow Worker Side)

</details>

---

### 🚧 v8.2 Proton Coaching Ecosystem Audit (In Progress)

**Milestone Goal:** Riset best practices coaching/mentoring platform → audit seluruh ekosistem Proton coaching end-to-end (setup, execution, completion, monitoring) → fix bug dan implement differentiator enhancement berdasarkan temuan riset.

## Phases

- [x] **Phase 233: Riset & Perbandingan Coaching Platform** - Browse platform coaching luar, dokumen perbandingan UX/flow, rekomendasi improvement (completed 2026-03-22)
- [x] **Phase 234: Audit Setup Flow** - Audit silabus delete safety, guidance file management, coach-coachee mapping, track assignment, import/export (completed 2026-03-22)
- [x] **Phase 235: Audit Execution Flow** - Audit evidence submission, approval chain, status history, notifikasi, PlanIdp view (completed 2026-03-22)
- [x] **Phase 236: Audit Completion** - Audit final assessment, coaching sessions, HistoriProton, 3-year journey lifecycle (completed 2026-03-23)
- [x] **Phase 237: Audit Monitoring & Differentiator Enhancement** - Audit dashboard, tracking, override, export, plus workload indicator, batch approval, bottleneck analysis (completed 2026-03-23)

## Phase Details

### Phase 233: Riset & Perbandingan Coaching Platform
**Goal**: Menghasilkan dokumen riset perbandingan platform coaching industri vs portal KPB sebagai lens untuk audit Phases 234-237
**Depends on**: Phase 232 (v8.1 complete)
**Requirements**: RSCH-01, RSCH-02, RSCH-03
**Success Criteria** (what must be TRUE):
  1. Screenshot dan dokumentasi UX/flow dari minimal 3 platform coaching (360Learning, BetterUp, CoachHub) tersedia sebagai referensi riset
  2. Dokumen perbandingan menjabarkan gap portal KPB vs platform luar per area Proton (Setup, Execution, Monitoring, Completion) secara konkret
  3. Daftar rekomendasi improvement berisi prioritas yang terurut berdasarkan nilai bisnis dan kompleksitas implementasi
**Plans**: 1 plan
Plans:
- [x] 233-01-PLAN.md — Dokumen HTML riset perbandingan coaching platform (360Learning, BetterUp, CoachHub vs Portal KPB)

### Phase 234: Audit Setup Flow
**Goal**: Memastikan fondasi data Proton (silabus, mapping, assignment) integritas — tidak ada setup yang bisa menghasilkan data corrupt di fase execution berikutnya
**Depends on**: Phase 233
**Requirements**: SETUP-01, SETUP-02, SETUP-03, SETUP-04, SETUP-05
**Success Criteria** (what must be TRUE):
  1. Admin yang mencoba hard delete silabus dengan progress aktif mendapat modal warning dengan impact count dan opsi soft delete
  2. File management guidance (upload, replace, delete) berjalan tanpa file orphan, validasi tipe file berfungsi di server side
  3. Cascade deactivation coach-coachee mapping terbungkus dalam DB transaction atomik, duplikasi mapping terdeteksi dan ditolak
  4. Track assignment memvalidasi progression Tahun 1→2→3 di server side — Tahun 2 tidak bisa di-assign sebelum Tahun 1 selesai
  5. Import/export silabus dan mapping menghasilkan data yang akurat, error per-baris dilaporkan, template tidak menyebabkan data salah saat diisi
**Plans**: 3 plans
Plans:
- [x] 234-01-PLAN.md — Audit silabus delete safety + guidance file management (ProtonDataController)
- [x] 234-02-PLAN.md — Audit coach-coachee mapping cascade + track assignment progression (AdminController)
- [x] 234-03-PLAN.md — Audit import/export robustness silabus dan mapping

### Phase 235: Audit Execution Flow
**Goal**: Memastikan alur operasional harian Proton (evidence submission, approval chain, notifikasi) aman dari sisi server dan state-nya selalu konsisten
**Depends on**: Phase 234
**Requirements**: EXEC-01, EXEC-02, EXEC-03, EXEC-04, EXEC-05
**Success Criteria** (what must be TRUE):
  1. Coachee dapat submit evidence, menerima reject dengan komentar, dan resubmit — semua tanpa kehilangan file sebelumnya
  2. Approval chain tidak bisa menghasilkan state tidak konsisten pada concurrent approve, override admin, atau partial approval — setiap transisi memanggil helper yang sama
  3. DeliverableStatusHistory memiliki insert di setiap state transition termasuk initial Pending saat progress pertama kali di-seed
  4. Semua notification trigger Proton terpanggil pada: evidence submit, approve, reject, HC review, dan final assessment — verifikasi di server bukan hanya UI
  5. PlanIdp menampilkan silabus dan guidance tab dengan akurasi, role-based access berjalan benar (coachee tidak bisa akses admin tab)
**Plans**: 3 plans
Plans:
- [x] 235-01-PLAN.md — Audit evidence submission + StatusHistory completeness (CDPController, AdminController, ProtonDataController)
- [x] 235-02-PLAN.md — Audit approval chain race condition + notifikasi gaps (CDPController)
- [x] 235-03-PLAN.md — Audit PlanIdp view accuracy + human verification keseluruhan

### Phase 236: Audit Completion
**Goal**: Memastikan fase akhir perjalanan coachee (final assessment, coaching sessions, history) akurat dan tidak bisa menghasilkan data duplikat atau inkonsisten
**Depends on**: Phase 235
**Requirements**: COMP-01, COMP-02, COMP-03, COMP-04
**Success Criteria** (what must be TRUE):
  1. ProtonFinalAssessment tidak bisa di-create duplikat untuk ProtonTrackAssignmentId yang sama — unique constraint di DB dan guard di controller aktif
  2. Coaching sessions ter-linked ke deliverable progress yang benar, action items menampilkan status tracking yang akurat
  3. HistoriProton menampilkan timeline lengkap per coachee termasuk data legacy CoachingLog tanpa duplikasi atau gap
  4. Lifecycle Tahun 1→2→3 berjalan end-to-end: assignment, transisi antar tahun, dan completion flow menghasilkan competency level yang benar
**Plans**: 4 plans
Plans:
- [x] 236-01-PLAN.md — DB migration: unique constraint ProtonFinalAssessment + IsCompleted/CompletedAt di CoachCoacheeMapping
- [x] 236-02-PLAN.md — Controller fixes: query scope fix, session edit/delete, MarkMappingCompleted
- [x] 236-03-PLAN.md — HistoriProton: fix Lulus logic + section separator per tahun
- [x] 236-04-PLAN.md — Gap closure: EditCoachingSession view + MarkMappingCompleted button

### Phase 237: Audit Monitoring & Differentiator Enhancement
**Goal**: Memastikan dashboard dan monitoring akurat setelah semua data upstream bersih, plus menambahkan differentiator fitur yang meningkatkan nilai portal vs platform luar
**Depends on**: Phase 236
**Requirements**: MON-01, MON-02, MON-03, MON-04, DIFF-01, DIFF-02, DIFF-03
**Success Criteria** (what must be TRUE):
  1. Dashboard menampilkan stats yang akurat per role — HC melihat semua, coach melihat coachee-nya saja, Chart.js data konsisten dengan query
  2. CoachingProton tracking menampilkan filter cascade yang benar, pagination berfungsi, kolom role-specific hanya tampil untuk role yang berwenang
  3. Override admin mencatat audit trail lengkap per transaksi, validasi status transition mencegah transisi ilegal
  4. Semua export action menggunakan projection (bukan over-fetch) dan memiliki role attribute yang benar
  5. Mapping page dan dashboard menampilkan jumlah coachee aktif per coach sebagai workload indicator
  6. HC dapat approve multiple deliverables sekaligus dari monitoring view via batch approval
  7. Dashboard menampilkan bottleneck analysis — deliverable yang paling lama pending teridentifikasi dan visible
**Plans**: 3 plans
Plans:
- [x] 237-01-PLAN.md — Audit CoachingProton tracking + Override transition validation
- [x] 237-02-PLAN.md — Audit dashboard stats + bottleneck chart + workload indicator
- [x] 237-03-PLAN.md — Export audit + 3 export baru + batch HC approval + UAT

### Phase 238: Gap Closure — UI Wiring untuk Endpoint yang Belum Terhubung
**Goal**: Menghubungkan 3 backend endpoint/response yang sudah ada ke UI — progression warning override, coaching session Edit/Delete, dan 3 export baru
**Depends on**: Phase 237
**Requirements**: SETUP-04, COMP-02, MON-04, DIFF-01, DIFF-03
**Gap Closure:** Closes gaps from v8.2 milestone audit
**Success Criteria** (what must be TRUE):
  1. AJAX handler CoachCoacheeMapping menampilkan confirm dialog saat `warning:true` dan mengirim ulang dengan `ConfirmProgressionWarning=true`
  2. Deliverable.cshtml menampilkan tombol Edit dan Delete untuk coaching sessions (role-gated coach pemilik + HC/Admin)
  3. UI memiliki link/tombol untuk ExportBottleneckReport, ExportCoachingTracking, dan ExportWorkloadSummary yang accessible oleh HC/Admin
**Plans**: 1 plan
Plans:
- [x] 238-01-PLAN.md — Wire progression warning, session Edit/Delete, 3 export buttons

## Progress

**Execution Order:** 233 → 234 → 235 → 236 → 237

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 233. Riset & Perbandingan Coaching Platform | v8.2 | 1/1 | Complete    | 2026-03-22 |
| 234. Audit Setup Flow | v8.2 | 3/3 | Complete    | 2026-03-22 |
| 235. Audit Execution Flow | v8.2 | 4/4 | Complete    | 2026-03-23 |
| 236. Audit Completion | v8.2 | 4/4 | Complete    | 2026-03-23 |
| 237. Audit Monitoring & Differentiator Enhancement | v8.2 | 3/3 | Complete   | 2026-03-23 |
| 238. Gap Closure — UI Wiring | v8.2 | 1/1 | Complete    | 2026-03-23 |
