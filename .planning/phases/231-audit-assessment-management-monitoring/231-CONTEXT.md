# Phase 231: Audit Assessment Management & Monitoring - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit halaman ManageAssessment dan AssessmentMonitoring (sisi admin/HC), fix semua bug, dan improve berdasarkan riset Phase 228. Mencakup CRUD assessment, package management, monitoring real-time, HC actions, dan special handling Assessment Proton. Worker-side exam flow adalah Phase 232 — di luar scope.

</domain>

<decisions>
## Implementation Decisions

### Strategi Audit
- **D-01:** Pendekatan hybrid: gunakan rekomendasi Phase 228 sebagai checklist PLUS audit independen dari kode untuk cari bug/issue yang tidak terdeteksi riset
- **D-02:** Prioritas fix: must-fix + should-improve. Nice-to-have di-defer ke backlog
- **D-03:** Output: HTML report di `docs/` (konsisten dengan audit docs existing)
- **D-04:** Pembagian 2 plans: Plan 1 = ManageAssessment CRUD + filter/list. Plan 2 = Monitoring + Package + Proton special case

### ManageAssessment List View
- **D-05:** Full list audit: filter kategori/status, search by judul, pagination, sorting, column display, empty state, performance
- **D-06:** Phase 228 flag filter sebagai must-fix — prioritaskan

### CreateAssessment
- **D-07:** Audit ulang renewal integration dari perspektif CreateAssessment (renewSessionId/renewTrainingId params) — independen dari fix Phase 229-230
- **D-08:** Validasi lengkap: judul, kategori, tanggal, peserta, passing grade

### EditAssessment
- **D-09:** Standard audit: data preservation, package warning, field validation, edge cases (edit saat exam berlangsung)

### DeleteAssessment
- **D-10:** Audit KEDUA variant: DeleteAssessment (single) dan DeleteAssessmentGroup — cascade cleanup packages, questions, sessions, responses

### AssessmentMonitoring (Group List)
- **D-11:** Audit akurasi stats (participant count, completed, passed), filter kategori/status, dan group status derivation

### AssessmentMonitoringDetail (Real-time)
- **D-12:** Monitoring detail sudah punya SignalR real-time (progressUpdate, workerStarted, workerSubmitted). Audit fungsionalitas handlers + reconnection behavior saat connection lost + fallback
- **D-13:** Audit semua HC actions dengan kedalaman sama: Reset session, Force Close, Bulk Close, Regenerate Token
- **D-14:** Token card: copy dan regenerate berfungsi, token lama invalidated

### Package Management
- **D-15:** Audit lengkap: CRUD package, ImportPackageQuestions (Excel), assignment peserta, PreviewPackage
- **D-16:** ImportPackageQuestions: validasi format, error handling, duplicate detection, partial import behavior
- **D-17:** PreviewPackage: pastikan soal render benar, gambar/media, jawaban tersembunyi
- **D-18:** Assignment conflict handling: audit behavior saat assign ke peserta yang sudah punya active session atau reassign

### Assessment Proton (Audit Mendalam Tersendiri)
- **D-19:** Interview mode Tahun 3: 5 aspek interview (Pengetahuan Teknis, Kemampuan Operasional, Keselamatan Kerja, Komunikasi & Kerjasama, Sikap Profesional), scoring per aspek, total score, special UI
- **D-20:** Proton exam flow Tahun 1-2: audit special handling vs assessment reguler
- **D-21:** Proton package/soal: audit format khusus vs assessment reguler
- **D-22:** Proton monitoring: badge "Assessment Proton", group status, pass rate calculation

### Audit Log & Error Handling
- **D-23:** Pastikan SEMUA CRUD dan HC actions punya audit log, format konsisten, warning-only untuk audit log failure

### Authorization
- **D-24:** Verify semua assessment actions punya [Authorize(Roles)] yang benar dan konsisten (Admin, HC)

### UserAssessmentHistory
- **D-25:** Basic verification: data akurat (score, status, tanggal), link dari monitoring detail berfungsi

### Claude's Discretion
- Urutan audit per-action dalam setiap plan
- Detail level HTML report layout
- Pendekatan fix untuk issue yang ditemukan (refactor vs patch)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Riset Best Practices (Phase 228 output)
- `docs/audit-assessment-training-v8.html` — Dokumen riset assessment management best practices dan perbandingan
- `.planning/phases/228-best-practices-research/228-CONTEXT.md` — Keputusan riset dan rekomendasi mapping per phase

### Requirements
- `.planning/ROADMAP.md` — Phase 231 success criteria (AMGT-01 s/d AMGT-05, AMON-01 s/d AMON-04)
- `.planning/REQUIREMENTS.md` — Requirement definitions AMGT dan AMON

### Kode Assessment (audit targets)
- `Controllers/AdminController.cs` — ManageAssessment (line ~633), CreateAssessment (~957), EditAssessment (~1581), DeleteAssessment (~1899), DeleteAssessmentGroup (~1979), RegenerateToken (~2067), AssessmentMonitoring (~2125), AssessmentMonitoringDetail (~2225), ManagePackages (~6050), ImportPackageQuestions (~6257)
- `Views/Admin/ManageAssessment.cshtml` — List view dengan filter/search
- `Views/Admin/CreateAssessment.cshtml` — Form create termasuk renewal mode
- `Views/Admin/EditAssessment.cshtml` — Form edit assessment
- `Views/Admin/AssessmentMonitoring.cshtml` — Group list monitoring
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — Per-participant detail dengan SignalR real-time
- `Views/Admin/ManagePackages.cshtml` — Package CRUD per assessment
- `Views/Admin/ImportPackageQuestions.cshtml` — Excel import soal
- `Views/Admin/PreviewPackage.cshtml` — Preview soal package
- `Views/Admin/UserAssessmentHistory.cshtml` — Riwayat assessment per worker

### Prior Phase Context
- `.planning/phases/229-audit-renewal-logic-edge-cases/` — Fix renewal logic (context untuk audit ulang CreateAssessment renewal integration)
- `.planning/phases/230-audit-renewal-ui-cross-page-integration/` — Fix renewal UI cross-page

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- SignalR hub (`window.assessmentHub`) — sudah terintegrasi di MonitoringDetail untuk real-time updates
- Excel import pattern — ImportPackageQuestions menggunakan pattern yang sama dengan ImportWorkers
- Audit log pattern — sudah ada di CRUD actions, perlu verifikasi konsistensi

### Established Patterns
- AdminController class-level `[Authorize]` dengan per-action `[Authorize(Roles = "Admin, HC")]` untuk assessment actions
- TempData untuk success/error messages di semua assessment actions
- MonitoringGroupViewModel sebagai model grouping assessment

### Integration Points
- CreateAssessment renewal integration: `renewSessionId` dan `renewTrainingId` query params dari RenewalCertificate
- AssessmentMonitoringDetail: SignalR events (progressUpdate, workerStarted, workerSubmitted)
- ManagePackages: linked dari assessment detail via `assessmentId`
- UserAssessmentHistory: linked dari monitoring detail

</code_context>

<specifics>
## Specific Ideas

- Assessment Proton harus diaudit tersendiri dan mendalam — bukan sekadar bagian dari monitoring
- CloseEarly sudah dihapus di Phase 162, diganti AkhiriSemuaUjian dengan auto-grading — pastikan tidak ada dead reference
- "Assessment Proton" Tahun 3 punya interview mode dengan 5 aspek — ini perlu special attention karena berbeda dari exam reguler

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 231-audit-assessment-management-monitoring*
*Context gathered: 2026-03-22*
