# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- ✅ **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (shipped 2026-03-21)
- ✅ **v8.0 Assessment Integrity & Analytics** - Phases 223–227 (shipped 2026-03-22)
- 🚧 **v8.1 Renewal & Assessment Ecosystem Audit** - Phases 228–232 (in progress)

---

<details>
<summary>✅ v1.0–v8.0 (Phases 1–227) - SHIPPED</summary>

All prior milestones shipped. See MILESTONES.md for full detail.

Last completed phase: 227 (v8.0 cleanup — remove dead ManageQuestions link from assessment dropdown)

</details>

---

### 🚧 v8.1 Renewal & Assessment Ecosystem Audit (In Progress)

**Milestone Goal:** Riset best practices platform sejenis → audit seluruh ekosistem renewal certificate dan assessment (logic, UI, cross-page integration, management, worker flow) → fix bug dan improve UX berdasarkan temuan riset.

## Phases

- [x] **Phase 228: Best Practices Research** - Riset renewal, assessment, dan monitoring best practices dari platform sejenis (completed 2026-03-22)
- [x] **Phase 229: Audit Renewal Logic & Edge Cases** - Audit dan fix renewal chain FK, badge sync, status derivation, grouping, dan edge cases (completed 2026-03-22)
- [ ] **Phase 230: Audit Renewal UI & Cross-Page Integration** - Audit dan fix renewal UI grouped view, filter, modal, dan integrasi lintas halaman
- [ ] **Phase 231: Audit Assessment Management & Monitoring** - Audit dan fix ManageAssessment dan AssessmentMonitoring (admin/HC side)
- [ ] **Phase 232: Audit Assessment Flow — Worker Side** - Audit dan fix worker-side exam flow end-to-end

## Phase Details

### Phase 228: Best Practices Research
**Goal**: Riset best practices dari platform sejenis untuk renewal certificate, assessment management, exam monitoring, dan exam flow — hasilkan dokumen perbandingan dan rekomendasi improvement.
**Depends on**: Phase 227 (v8.0 complete)
**Requirements**: RSCH-01, RSCH-02, RSCH-03, RSCH-04
**Success Criteria** (what must be TRUE):
  1. Dokumen riset renewal certificate best practices mencakup minimal 3 platform dibandingkan (Coursera, LinkedIn Learning, HR portals sejenis)
  2. Dokumen riset assessment/exam management best practices mencakup minimal 3 platform (Moodle, Google Forms Quiz, Examly)
  3. Dokumen riset real-time exam monitoring best practices dengan contoh konkret UX patterns
  4. Dokumen perbandingan fitur portal vs best practices dengan rekomendasi improvement per halaman (RenewalCertificate, ManageAssessment, AssessmentMonitoring, exam flow)
**Plans**: 2 plans
Plans:
- [x] 228-01-PLAN.md — Tulis 3 dokumen riset (renewal, assessment, monitoring)
- [x] 228-02-PLAN.md — Tulis dokumen exam flow + ringkasan perbandingan

### Phase 229: Audit Renewal Logic & Edge Cases
**Goal**: Audit kode renewal logic dengan lens best practices, fix semua bug pada FK chain, badge sync, status derivation, grouping, dan edge case handling.
**Depends on**: Phase 228
**Requirements**: LDAT-01, LDAT-02, LDAT-03, LDAT-04, LDAT-05, EDGE-01, EDGE-02, EDGE-03
**Success Criteria** (what must be TRUE):
  1. Semua 4 kombinasi FK renewal (AS→AS, AS→TR, TR→TR, TR→AS) dapat diverifikasi set dengan benar saat renew
  2. Badge count di Admin/Index sinkron dengan BuildRenewalRowsAsync sebagai single source of truth
  3. DeriveCertificateStatus menangani null ValidUntil, Permanent, expired, dan akan-expired tanpa error
  4. Grouping by Judul berjalan case-insensitive dan karakter khusus URL-safe
  5. MapKategori konsisten dengan AssessmentCategories naming di seluruh codebase
  6. Bulk mixed-type validation berfungsi, double renewal dicegah, empty state ditangani dengan benar
**Plans**: 2 plans
Plans:
- [x] 229-01-PLAN.md — Fix MapKategori DB lookup, double renewal guard, FK XOR, mirror CDPController
- [x] 229-02-PLAN.md — Mixed-type bulk validation, empty state verifikasi, HTML audit report

### Phase 230: Audit Renewal UI & Cross-Page Integration
**Goal**: Audit renewal UI dan semua integrasi lintas halaman, fix dan improve berdasarkan temuan riset best practices.
**Depends on**: Phase 229
**Requirements**: UIUX-01, UIUX-02, UIUX-03, UIUX-04, XPAG-01, XPAG-02, XPAG-03, XPAG-04
**Success Criteria** (what must be TRUE):
  1. Grouped view RenewalCertificate tampil benar dengan data aktual per grup
  2. Filter cascade Bagian/Unit/Kategori/Tipe berfungsi dan saling terhubung
  3. Renewal method modal (single + bulk) menampilkan pilihan yang benar berdasarkan tipe sertifikat
  4. Certificate history modal menampilkan chain grouping yang akurat
  5. CreateAssessment dan AddTraining menerima renewal pre-fill (judul, kategori, peserta) dari RenewalCertificate
  6. CDP CertificationManagement toggle renewed certs berfungsi, Admin/Index badge count sinkron
**Plans**: 2 plans
Plans:
- [x] 230-01-PLAN.md — Audit grouped view, filter cascade, modals, certificate history
- [ ] 230-02-PLAN.md — Audit cross-page pre-fill, CDP toggle, badge count, HTML report

### Phase 231: Audit Assessment Management & Monitoring
**Goal**: Audit halaman ManageAssessment dan AssessmentMonitoring (sisi admin/HC), fix semua bug, dan improve berdasarkan riset.
**Depends on**: Phase 230
**Requirements**: AMGT-01, AMGT-02, AMGT-03, AMGT-04, AMGT-05, AMON-01, AMON-02, AMON-03, AMON-04
**Success Criteria** (what must be TRUE):
  1. CreateAssessment form validasi lengkap (judul, kategori, tanggal, peserta, passing grade) sebelum submit
  2. EditAssessment mempertahankan data existing dan menampilkan warning jika ada package terkait
  3. DeleteAssessment melakukan cascade cleanup benar (packages, questions, sessions, responses)
  4. Package assignment ke peserta berfungsi untuk single dan bulk assign
  5. AssessmentMonitoring menampilkan stats real-time (participant count, completed, passed, status) per group
  6. Semua HC actions berfungsi: Reset, Force Close, Bulk Close, Close Early, Regenerate Token
  7. Token card copy dan regenerate berfungsi dari halaman monitoring detail
**Plans**: 2 plans
Plans:
- [ ] 231-01-PLAN.md — [to be planned]
- [ ] 231-02-PLAN.md — [to be planned]

### Phase 232: Audit Assessment Flow — Worker Side
**Goal**: Audit worker-side assessment flow end-to-end, fix semua bug, dan improve UX berdasarkan riset.
**Depends on**: Phase 231
**Requirements**: AFLW-01, AFLW-02, AFLW-03, AFLW-04, AFLW-05
**Success Criteria** (what must be TRUE):
  1. Worker melihat daftar assessment (Open/Upcoming) sesuai assignment — tidak ada assessment yang tidak relevan tampil
  2. StartExam flow lengkap berfungsi: token entry → exam page → timer berjalan → auto-save per-click
  3. SubmitExam menghasilkan score, IsPassed, NomorSertifikat (jika lulus), dan competency level update
  4. Session resume berfungsi dengan ElapsedSeconds, LastActivePage, dan pre-populated answers akurat
  5. Results page menampilkan score, pass/fail status, dan answer review (jika diaktifkan HC)
**Plans**: 2 plans
Plans:
- [ ] 232-01-PLAN.md — [to be planned]
- [ ] 232-02-PLAN.md — [to be planned]

## Progress

**Execution Order:** 228 → 229 → 230 → 231 → 232

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 228. Best Practices Research | v8.1 | 2/2 | Complete    | 2026-03-22 |
| 229. Audit Renewal Logic & Edge Cases | v8.1 | 2/2 | Complete    | 2026-03-22 |
| 230. Audit Renewal UI & Cross-Page Integration | v8.1 | 1/2 | In Progress|  |
| 231. Audit Assessment Management & Monitoring | v8.1 | 0/? | Not started | - |
| 232. Audit Assessment Flow — Worker Side | v8.1 | 0/? | Not started | - |
