# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0** - Phases 223-227 (shipped)
- ✅ **v8.1** - Phases 228-232 (shipped)
- ✅ **v8.2** - Phases 233-238 (shipped)
- ✅ **v8.3** - Phase 239 (shipped)
- ✅ **v8.4** - Phase 240 (shipped)
- ✅ **v8.5** - Phases 241-247 (shipped)
- ✅ **v8.6 Codebase Audit & Hardening** - Phases 248-252 (shipped 2026-03-24)
- ✅ **v8.7** - Phase 253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred — dikerjakan setelah v9.1)
- 🚧 **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (in progress)

## Phases

### 🚧 v9.1 UAT Coaching Proton End-to-End (Phases 257-261)

**Milestone Goal:** Verifikasi end-to-end flow Coaching Proton — temukan dan perbaiki semua bug sebelum production deployment.

- [ ] **Phase 257: Setup & Mapping** - Test CRUD coach-coachee mapping, import Excel, assign track, deactivate/reactivate, progression warning
- [ ] **Phase 258: Silabus & Guidance** - Test upload/edit/delete silabus hierarchy, upload/replace/delete guidance files
- [ ] **Phase 259: Evidence & Coaching Session** - Test submit evidence + coaching session, edit/delete session, resubmit after rejection
- [ ] **Phase 260: Approval Chain** - Test SrSpv approve/reject → SH approve/reject → HC review, audit trail
- [ ] **Phase 261: Dashboard, Export & Completion** - Test dashboard per role, export Excel/PDF, final assessment, graduation

## Phase Details

### Phase 257: Setup & Mapping
**Goal**: Semua flow coach-coachee mapping berjalan tanpa error — CRUD, import, assign track, deactivate/reactivate
**Depends on**: Nothing (first phase v9.1)
**Requirements**: MAP-01, MAP-02, MAP-03, MAP-04, MAP-05, MAP-06, MAP-07, MAP-08
**Success Criteria** (what must be TRUE):
  1. Halaman CoachCoacheeMapping tampil dengan data, pagination, dan search berfungsi
  2. Assign coach ke coachee via modal berhasil + ProtonTrackAssignment terbuat jika TrackId dipilih
  3. Import Excel berhasil (create, reactivate, skip duplicate) dengan feedback yang jelas
  4. Deactivate mapping → TrackAssignment ikut deactivate; Reactivate → reuse assignment lama
  5. Warning D-09 muncul saat assign Tahun 2+ dengan Tahun sebelumnya belum selesai
**Plans**: 2 plans
Plans:
- [ ] 257-01-PLAN.md — Code review + bug fix MAP-01..05 (list, assign, import, template, track assignment)
- [ ] 257-02-PLAN.md — Code review + bug fix MAP-06..08 (deactivate, reactivate, progression warning)

### Phase 258: Silabus & Guidance
**Goal**: Struktur kompetensi bisa dikelola lengkap dan guidance files bisa diakses oleh coach/coachee
**Depends on**: Phase 257 (mapping harus ada untuk konteks Bagian/Unit)
**Requirements**: SIL-01, SIL-02, SIL-03, SIL-04, SIL-05, SIL-06
**Success Criteria** (what must be TRUE):
  1. Upload silabus Excel berhasil membuat/update hierarki Kompetensi → SubKompetensi → Deliverable
  2. Deliverable yang dihapus dari payload baru ikut terhapus (orphan cleanup)
  3. Deactivate/reactivate kompetensi berfungsi
  4. Upload, replace, delete guidance file berfungsi
  5. Coach dan Coachee bisa download guidance file
**Plans**: TBD

### Phase 259: Evidence & Coaching Session
**Goal**: Flow submit evidence + coaching session berjalan end-to-end tanpa error
**Depends on**: Phase 258 (silabus + deliverable harus ada)
**Requirements**: EVI-01, EVI-02, EVI-03, EVI-04, EVI-05
**Success Criteria** (what must be TRUE):
  1. Coach berhasil submit evidence + catatan coaching → CoachingSession terbuat dengan status Submitted
  2. Validasi mapping aktif berfungsi (coach tanpa mapping tidak bisa submit)
  3. Edit coaching session berhasil update CatatanCoach, Kesimpulan, Result
  4. Delete coaching session berhasil + ActionItems ikut terhapus
  5. Resubmit setelah rejection → approval SrSpv/SH reset ke Pending, evidence history tersimpan
**Plans**: TBD

### Phase 260: Approval Chain
**Goal**: Multi-stage approval (SrSpv → SH → HC) berjalan benar dengan audit trail lengkap
**Depends on**: Phase 259 (evidence harus ada dalam status Submitted)
**Requirements**: APR-01, APR-02, APR-03, APR-04, APR-05, APR-06, APR-07
**Success Criteria** (what must be TRUE):
  1. SrSpv bisa approve dan reject (dengan alasan) deliverable
  2. SectionHead bisa approve dan reject deliverable
  3. HC bisa mark deliverable sebagai "Reviewed"
  4. Setiap status change tercatat di DeliverableStatusHistory dengan actor, role, timestamp
  5. Role scoping benar — SrSpv/SH hanya lihat coachee di section mereka
**Plans**: TBD

### Phase 261: Dashboard, Export & Completion
**Goal**: Dashboard, export, dan completion flow berfungsi benar untuk semua role
**Depends on**: Phase 260 (butuh data approved untuk test completion)
**Requirements**: DSH-01, DSH-02, DSH-03, DSH-04, DSH-05, DSH-06
**Success Criteria** (what must be TRUE):
  1. CoachingProton dashboard menampilkan data sesuai role (Admin/HC=all, Coach=mapped, SrSpv/SH=section, Coachee=own)
  2. Filter dan pagination berfungsi
  3. Export Excel dan PDF berisi data yang benar dan lengkap
  4. HC bisa buat final assessment setelah semua deliverable approved
  5. Graduation flag IsCompleted bisa di-set dan tersimpan
**Plans**: TBD

<details>
<summary>⏸️ v9.0 Pre-deployment Audit & Finalization (Phases 254-256) — DEFERRED</summary>

- [ ] Phase 254: Seed Cleanup & Tech Debt Closure
- [ ] Phase 255: Production Configuration
- [ ] Phase 256: Security Hardening

Backup: `.planning/milestones/v9.0-REQUIREMENTS.md`, `.planning/milestones/v9.0-ROADMAP.md`

</details>

<details>
<summary>✅ v8.5 UAT Assessment System End-to-End (Phases 241-247) — SHIPPED 2026-03-24</summary>

- [x] Phase 241: Seed Data UAT (2/2 plans)
- [x] Phase 242: UAT Setup Flow (2/2 plans)
- [x] Phase 243: UAT Exam Flow (2/2 plans)
- [x] Phase 244: UAT Monitoring & Analytics (2/2 plans)
- [x] Phase 245: UAT Proton Assessment (2/2 plans)
- [x] Phase 246: UAT Edge Cases & Records (2/2 plans)
- [x] Phase 247: Bug Fix Pasca-UAT (2/2 plans)

</details>

<details>
<summary>✅ v8.6 Codebase Audit & Hardening (Phases 248-252) — SHIPPED 2026-03-24</summary>

- [x] Phase 248: UI & Annotations (1/1 plans)
- [x] Phase 249: Null Safety & Input Validation (2/2 plans)
- [x] Phase 250: Security & Performance (1/1 plans)
- [x] Phase 251: Data Integrity & Logic (2/2 plans)
- [x] Phase 252: XSS Escape AJAX Approval Badge (1/1 plans)

</details>

<details>
<summary>✅ v8.7 AddTraining Multi-Select (Phase 253) — SHIPPED 2026-03-25</summary>

- [x] Phase 253: AddTraining multi-select pekerja dan perbaikan form (2/2 plans)

</details>

## Progress

**Execution Order:**
Phases execute in numeric order: 257 → 258 → 259 → 260 → 261

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 257. Setup & Mapping | v9.1 | 0/2 | Planning | - |
| 258. Silabus & Guidance | v9.1 | 0/? | Not started | - |
| 259. Evidence & Coaching Session | v9.1 | 0/? | Not started | - |
| 260. Approval Chain | v9.1 | 0/? | Not started | - |
| 261. Dashboard, Export & Completion | v9.1 | 0/? | Not started | - |
