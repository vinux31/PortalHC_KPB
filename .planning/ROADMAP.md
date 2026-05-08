# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** — Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** — Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** — Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** — Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** — Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** — Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** — Phases 264-280 (shipped)
- ⏸️ **v11.2 Admin Platform Enhancement** — Phases 281-285 (paused — closed early)
- ✅ **v12.0 Controller Refactoring** — Phases 286-291 (shipped 2026-04-02)
- ✅ **v13.0 Redesign Struktur Organisasi** — Phases 292-295 (shipped 2026-04-06)
- ✅ **v14.0 Assessment Enhancement** — Phases 296-303 (shipped 2026-04-24) — [archive](milestones/v14.0-ROADMAP.md)
- 🚧 **v15.0 Audit Findings 27 April 2026** — Phases 304-314 (planning, started 2026-04-28; Wave 5 added 2026-04-29)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v12.0, Phases 1-291) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>⏸️ v11.2 Admin Platform Enhancement (Phases 281-285) — PAUSED</summary>

- [ ] **Phase 281: System Settings** — Admin dapat mengelola konfigurasi aplikasi dari UI
- [x] **Phase 282: Maintenance Mode** — Admin dapat mengaktifkan mode pemeliharaan
- [x] **Phase 283: User Impersonation** — Admin dapat melihat aplikasi dari perspektif role/user lain
- [ ] **Phase 285: Dedicated Impersonation Page** — Halaman admin tersendiri untuk impersonation

</details>

<details>
<summary>✅ v13.0 Redesign Struktur Organisasi (Phases 292-295) — SHIPPED 2026-04-06</summary>

- [x] **Phase 292: Backend AJAX Endpoints** — GetOrganizationTree JSON + dual-response pada CRUD actions + CSRF utility
- [x] **Phase 293: View Shell & Tree Rendering** — Ganti 520-baris view dengan ~130-baris shell + recursive tree dari JSON
- [x] **Phase 294: AJAX CRUD Lengkap** — Modal add/edit, toggle, delete, action dropdown via orgTree.js tanpa page reload
- [x] **Phase 295: Drag-drop Reorder** — SortableJS reorder sibling-only, cross-parent diblokir

</details>

<details>
<summary>✅ v14.0 Assessment Enhancement (Phases 296-303) — SHIPPED 2026-04-24</summary>

- [x] **Phase 296: Data Foundation + GradingService Extraction** — Migrasi DB backward-compatible + GradingService single source of truth (2026-04-06)
- [x] **Phase 297: Admin Pre-Post Test** — HC membuat, mengelola, memonitor assessment Pre-Post Test (2026-04-07)
- [x] **Phase 298: Question Types** — 4 tipe soal baru (TF/MA/Essay/FiB) dengan auto/manual grading (2026-04-07)
- [x] **Phase 299: Worker Pre-Post Test + Comparison** — Pekerja mengerjakan Pre-Post Test + melihat gain score (2026-04-07)
- [x] **Phase 300: Mobile Optimization** — Exam UI responsif mobile untuk pekerja lapangan (2026-04-07)
- [x] **Phase 301: Advanced Reporting** — Item analysis, gain score report, Excel export (2026-04-07)
- [x] **Phase 302: Accessibility WCAG Quick Wins** — Keyboard nav, skip link, extra time via SignalR (2026-04-07)
- [x] **Phase 303: Rasio Coach-Coachee + Balanced Mapping** — Coach Workload dashboard + saran reassign + auto-suggest (shipped 2026-04-24, UAT deferred)

Full details: [milestones/v14.0-ROADMAP.md](milestones/v14.0-ROADMAP.md) • Requirements: [milestones/v14.0-REQUIREMENTS.md](milestones/v14.0-REQUIREMENTS.md)

</details>

### 🚧 v15.0 Audit Findings 27 April 2026 (Active)

**Goal:** Tindak lanjut 11 temuan audit pada flow assessment & login PortalHC_KPB — bug-fix + UX enhancements + 1 perf improvement, tanpa migrasi DB (kecuali 1 EF migration kecil untuk DB index di PERF-01).

**Started:** 2026-04-28 | **Phases:** 304-311 (8 phase) | **Active REQ:** 10 | **Deferred REQ:** 1 (EPRV-01)

#### Wave 1 — UI Label & Polish (parallel-safe label changes)

#### Phase 304: UI Label Polish (Login + WIB)

- [x] **Phase 304: UI Label Polish (Login + WIB)** — Eye-icon toggle login + label "(WIB)" di Step 3 wizard + suffix "WIB" di Step 4 summary (completed 2026-04-28)
  - **REQ:** AUTH-01, WIZ-02, WIZ-03
  - **Success Criteria:**
    1. Login `/Account/Login` menampilkan eye icon yang toggle `type="password"` ↔ `type="text"`, keyboard accessible (Tab+Space), button `type="button"` (tidak men-submit form)
    2. Step 3 `CreateAssessment.cshtml`: semua label time (baris 362, 383, 404, 412, 425, 432) menampilkan suffix "(WIB)"
    3. Step 4 summary baris 1177 menampilkan "{date} {time} WIB" konsisten dengan baris 1164 ("Jam Mulai")
    4. PrePost summary di blok 1117–1130 juga menampilkan "WIB" jika menampilkan datetime
    5. Tidak ada regresi pada flow login (local + AD) atau wizard create assessment
  - **Risk:** Low | **Effort:** S
  - **Plans:** 2 plans
    - [x] 304-01-PLAN.md — Eye-icon toggle password Login (AUTH-01)
    - [x] 304-02-PLAN.md — Label '(WIB)' Step 3 wizard + suffix ' WIB' Step 4 summary CreateAssessment (WIZ-02, WIZ-03)

#### Phase 305: Question Type Naming Clarity

- [x] **Phase 305: Question Type Naming Clarity** — Rename label MC/MA agar tidak rancu (UI saja, enum/DB tetap)
 (completed 2026-04-28)
  - **REQ:** LBL-01
  - **Success Criteria:**
    1. Form admin `ManagePackageQuestions.cshtml` dropdown menampilkan "Single Choice (1 jawaban benar)" + "Multiple Answers (≥2 jawaban benar)" (wording final per CONTEXT.md D-01 — Moodle/Canvas LMS standard)
    2. Preview `_PreviewQuestion.cshtml` badge label sesuai ("Single Choice" / "Multiple Answers" / "Essay")
    3. Worker exam `StartExam.cshtml` (asimetris→simetris D-09 D-16: badge MC ditambah) + summary `ExamSummary.cshtml` (SCOPE EXTENSION D-10: badge tipe baru di kolom Pertanyaan) menampilkan label baru
    4. Documentation cross-cutting: 8 file HTML/MD/PY di `wwwroot/documents/` + `docs/` di-update context-aware (D-13). PDF panduan + screenshot training di-flag deferred manual user task (D-14). E2E Playwright tests di `tests/e2e/` ZERO match label tipe (D-15 verified, no edit needed). Excel import template binary tetap pakai enum value internal (D-18 backward compat).
    5. DB query verifikasi: `SELECT DISTINCT QuestionType FROM PackageQuestions` returns hanya `MultipleChoice`/`MultipleAnswer`/`Essay` (D-17 D-20)
  - **Risk:** Low (UI), Medium (docs cross-cutting) | **Effort:** S
  - **Plans:** 2 plans
    - [x] 305-01-PLAN.md — Helper class `QuestionTypeLabels` + 5 view edits + controller flash error (LBL-01)
    - [x] 305-02-PLAN.md — 8 dokumentasi context-aware sed-replace + DB query verifikasi enum lock + grep audit final (LBL-01)

#### Wave 2 — UI Behavior (file conflict di CreateAssessment.cshtml — sequential)

#### Phase 306: Score Editable per Question Type

- [x] **Phase 306: Score Editable per Question Type** — Skor 1–100 untuk MC/MA/Essay (completed 2026-04-28)
  - **REQ:** QSCR-01
  - **Success Criteria:**
    1. Input `scoreValue` di `ManagePackageQuestions.cshtml` baris 188 tidak `disabled` default
    2. JS baris 299–300 tidak paksa `scoreInput.disabled = (qtype !== 'Essay')` dan tidak reset value=10
    3. Server-side `AssessmentAdminController.CreateQuestion` baris 4681 dan `EditQuestion` baris 4822: hapus override `if (questionType != "Essay") scoreValue = 10`
    4. Server-side validation: range 1–100 tetap di-enforce (Range attribute atau ModelState)
    5. AuditLog entry saat score diubah pada soal yang sudah punya session associated (warning + log, bukan block)
  - **Risk:** Medium | **Effort:** M
  - **Plans:** 2 plans
    - [x] 306-01-PLAN.md — Server-side: range validation, hapus force-override, audit log EditQuestion-ScoreChange + CreateQuestion-CustomScore + JSON GET extend affectedSessions (QSCR-01)
    - [x] 306-02-PLAN.md — View: header total points, scoreValue input enabled, modal Peringatan Ubah Skor + JS submit handler + populateEditForm extension + manual UAT 10-step (QSCR-01)

#### Phase 307: Selected Participants Inline View

- [x] **Phase 307: Selected Participants Inline View** — Real-time list peserta di Step 2 (COMPLETE 2026-04-29)
  - **REQ:** WIZ-01
  - **Success Criteria:**
    1. Step 2 `CreateAssessment.cshtml` (setelah baris 309) menampilkan panel "Peserta Terpilih" dengan badge count + nama 5 pertama + tombol expand "...dan N lainnya"
    2. Real-time update saat checkbox toggle (event delegation di container)
    3. DRY: extract `renderSelectedParticipants(targetEl, checkboxes)` dari `populateSummary` (1062–1095), reuse di Step 2 & Step 4
    4. Performance: 50+ peserta render < 200ms (DocumentFragment + debounce 100ms)
    5. Step 2 list = Step 4 summary list (no divergence)
  - **Risk:** Low | **Effort:** S
  - **Plans:** 2 plans
    - [x] 307-01-PLAN.md — Wave 0 test infrastructure: selectors helper + Phase 307 E2E describe block + opportunistic rot fix line 45 + manual UAT 5-step (WIZ-01)
    - [x] 307-02-PLAN.md — Wave 1 implementasi: panel markup Step 2 + Step 4 markup consolidation + helper renderSelectedParticipants top-level + hoist updateSelectedCount + populateSummary refactor + Proton IIFE replace + AJAX hydrate + reset handler edit (WIZ-01) — UAT PASSED 2026-04-29

#### Phase 308: PrePost Wizard Validation Fix

- [x] **Phase 308: PrePost Wizard Validation Fix** — Status field tidak reset wizard
 (completed 2026-04-29)
  - **REQ:** WIZ-04
  - **Success Criteria:**
    1. JS handler baris 1790–1807 saat `value === 'PrePostTest'` set `document.getElementById('Status').value = 'Upcoming'`
    2. Server-side POST `CreateAssessment` (~baris 778): conditional `if (isPrePostMode) ModelState.Remove("Status")`
    3. jQuery validate re-parse setelah dynamic show/hide statusFieldWrapper
    4. Test matrix 4 kombinasi pass: Standard saja, S→PP→S, PP saja, PP→S→PP — semua submit sukses tanpa reset ke Step 1
    5. Regresi check: Standard mode tanpa pilih Status tetap menampilkan "Status wajib dipilih"
  - **Risk:** Medium | **Effort:** M
  - **Plans:** 2 plans
    - [x] 308-01-PLAN.md — Wave 0 test infrastructure: extend wizardSelectors.ts dengan 5 selector baru + FLOW 8 describe block (4 tests 8.1-8.4) + 308-UAT.md 4-step Bahasa Indonesia (WIZ-04)
    - [x] 308-02-PLAN.md — Wave 1 implementasi: JS value assignment D-01/D-02 di handler line 1872-1889 + server ModelState.Remove(Status) D-04 antara line 779-782 + checkpoint manual UAT (WIZ-04). RESEARCH-corrected: form ID #createAssessmentForm, jQuery validate re-parse N/A (Pitfall 2)

#### Wave 3 — Defensive + State Machine (no file conflict, parallel-eligible)

#### Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling

- [x] **Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling** — Try-catch + structured log + null-safe + status `Menunggu Penilaian` valid
 (completed 2026-05-01)
  - **REQ:** WCRT-01, **SUB-01** (bundled 2026-04-29)
  - **Success Criteria:**
    1. *(WCRT-01)* `CMPController.Certificate` baris 1771–1811 dibungkus try-catch mirror pattern `CertificatePdf` (baris 2078–2083)
    2. *(WCRT-01)* Specific exception catches (DbException, FormatException, NRE) sebelum generic catch
    3. *(WCRT-01)* Structured logging: `_logger.LogError(ex, "Certificate view failed for session {Id}", id)`
    4. *(WCRT-01)* View `Certificate.cshtml`: null-safe accessor `Model.User?.FullName ?? "(Nama tidak tersedia)"`
    5. *(WCRT-01)* Helper `ResolveCategorySignatory` (1813–1838) wrapped try-catch dengan fallback signatory
    6. *(WCRT-01)* Worker dengan exotic Category (null/empty) tetap bisa view sertifikat, fallback "HC Manager"
    7. *(WCRT-01)* Post-deploy: monitor `_logger.LogError` di production untuk pin-point root cause aktual
    8. *(SUB-01)* Helper baru `IsAssessmentSubmitted(string status)` di `AssessmentConstants.cs` returns true untuk `"Completed"` ATAU `"Menunggu Penilaian"`
    9. *(SUB-01)* Tiga lokasi cek di `CMPController.cs` (line 1792, 1858, 2105) ganti dari `assessment.Status != "Completed"` menjadi `!IsAssessmentSubmitted(assessment.Status)`
    10. *(SUB-01)* Branch khusus `Menunggu Penilaian` di `Certificate()` & `CertificatePdf()` → `TempData["Info"]` (bukan Error) "Sertifikat akan tersedia setelah penilaian essay selesai." `Results()` render hasil sementara untuk status `Menunggu Penilaian`
    11. *(SUB-01)* Worker submit assessment ber-essay tidak menerima popup merah `Error: Assessment not completed yet.` di alur manapun
  - **Risk:** Medium-High | **Effort:** M
  - **Parallel-eligible:** dengan Phase 310
  - **Plans:** 3/3 plans complete
    - 309-01-PLAN.md — WCRT-01 defensive (try-catch, null-safe, fallback signatory)
    - 309-02-PLAN.md — SUB-01 helper + 3 lokasi update + Info branch + Essay items dengan IsEssayPending flag (D-08)
    - 309-03-PLAN.md — GradingService PendingGrading constant refactor (opportunistic SUB-01 OQ#2 — split iter-1; depends_on=[309-02])

#### Phase 310: Essay Finalize Idempotency

- [x] **Phase 310: Essay Finalize Idempotency** — Friendly no-op + UI hide + dedupe notif
 (completed 2026-05-05)
  - **REQ:** ESCG-01
  - **Success Criteria:**
    1. `AssessmentAdminController.FinalizeEssayGrading` baris 2713: ganti pesan "session tidak dalam status..." menjadi explisit, jika `Status == "Completed"` return success/no-op message ramah
    2. UI tombol "Create Sertifikasi" (di CDP `CertificationManagement` atau panel detail) hide saat `Status == "Completed"` && `NomorSertifikat != null`
    3. Idempotency: klik 2x tidak menduplikasi `TrainingRecord`, `NomorSertifikat`, atau `NotifyIfGroupCompleted` — dedupe via guard atau `NotificationSentAt` field
    4. AuditLog entries: distinct (tidak spam) per session — gunakan WHERE clause guard
    5. Integration test: scenario `Task.WhenAll` parallel finalize → tidak corrupt state
  - **Risk:** Medium-High | **Effort:** M
  - **Sequential after Phase 309** (per user decision 2026-04-29 saat discuss-phase 310 — tunggu `AssessmentConstants.AssessmentStatus.PendingGrading` constant dari Phase 309 D-04 merged dulu untuk hindari coordination complexity)
  - **Plans:** 2/2 plans complete
    - [x] 310-01-PLAN.md — Backend idempotency: FinalizeEssayGrading capture rowsAffected + D-03/D-04 BI branching + NotifyIfGroupCompleted dedup + AuditLog gated + ViewModel extend (ESCG-01)
    - [x] 310-02-PLAN.md — Frontend UI gate D-02 + JS handler D-03/D-04 + showAlert helper + Playwright FLOW 9 scaffold + 310-UAT.md draft + manual UAT 6-step (ESCG-01)

#### Wave 4 — Performance (measurement-driven, last)

#### Phase 311: ManageAssessment Performance

- [x] **Phase 311: ManageAssessment Performance** — HTMX lazy load architecture + opportunistic backend (REFRAMED 2026-05-07: backend bukan bottleneck, proxy wifi kantor adalah)
 (completed 2026-05-07)
  - **REQ:** PERF-01
  - **Depends on:** 310
  - **Success Criteria (revised 2026-05-07 — supersedes original SC #1-7 per CONTEXT.md):**
    1. Baseline per-segment Stopwatch terdokumentasi sebelum patch (DONE — commit a4ce556e Plan 01)
    2. Initial response document <14 KB (TCP first roundtrip)
    3. End-to-end load wifi kantor ≤40 detik (≥50% reduction dari baseline ~1.4 menit)
    4. Tab switching post-initial ≤2 detik
    5. TTFB tetap ≤500ms (no regression backend)
    6. Smoke test parity per tab (Assessment/Training/History) — kolom, row count, ordering identik pre/post
    7. Backward compat: filter form, pagination, ViewBag contract preserved
    8. (Plan 03 opportunistic) AsNoTracking + IX_AssessmentSessions_LinkedGroupId + IX_AssessmentSessions_ExamWindowCloseDate + IMemoryCache TTL 5min Categories cache + 3x invalidation di Add/Edit/DeleteCategory
  - **Risk:** Medium | **Effort:** M-L
  - **Plans:** 4/4 plans complete
    - [x] 311-01-PLAN.md — Wave 0 baseline: per-segment Stopwatch instrumentation (T1..T5) — DONE commit a4ce556e (preserved as ongoing telemetry)
    - [x] 311-02-PLAN.md — Wave 1 HTMX lazy load: REQUIREMENTS update + vendor HTMX 2.0.x + shell action refactor + 3 partial actions + shell view HTMX attrs + skeleton + filter form + error template + manual UAT 5-step BI (D-01..D-10) — paused-at-checkpoint pending Plan 04 gap closure
    - [x] 311-03-PLAN.md — Wave 2 backend opportunistic: 2 indexes migration + AsNoTracking + Include removal + Categories cache + 3 invalidation hooks (D-04..D-07)
    - [x] 311-04-PLAN.md — Wave 3 GAP CLOSURE: BUG-1 hide legacy filter rows via CSS (D-10 preserve) + BUG-2A invalidation filter-form-only + BUG-2B drop once on restore + BUG-5A retry htmx.ajax direct (PERF-01)

#### Wave 5 — Audit Findings 29 April 2026 (parallel-safe, post-Wave 4)

Empat temuan audit lapangan tambahan (29 April 2026). Phase 309 di Wave 3 di-expand dengan REQ SUB-01 (bundled). Tiga phase baru di Wave 5 ini independen di file level dan parallel-eligible.

#### Phase 312: Admin Full-Delete Assessment Room

- [x] **Phase 312: Admin Full-Delete Assessment Room** — Role tier guard (Admin override status guard, HC blocked dari Completed/with-response) + UI conditional render
 (completed 2026-05-07)
  - **REQ:** DEL-01
  - **Depends on:** 311
  - **Success Criteria:**
    1. Role tier guard di `DeleteAssessment()` & `DeleteAssessmentGroup()` body: `if (!User.IsInRole("Admin"))` cek status Completed atau hasResponses → block dengan TempData error
    2. Authorize attribute existing `[Authorize(Roles = "Admin, HC")]` (line 1929, 2034) tidak diubah
    3. `ManageAssessment.cshtml` tombol Hapus conditional: Admin selalu tampil, HC hidden untuk Completed atau participant_count > 0
    4. AuditLog entry sertakan `Status` & `ResponseCount` di description
    5. Cascade delete tetap utuh (PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages, UserPackageAssignments)
    6. Smoke test 5 skenario: Admin+Open OK, Admin+Completed OK, HC+Open(no-response) OK, HC+Completed BLOCK, HC+Open(with-response) BLOCK
  - **Risk:** Medium | **Effort:** M
  - **Plans:** 2/2 plans complete
    - 312-01-PLAN.md — Backend role guard + audit log extension
    - 312-02-PLAN.md — Frontend conditional render + smoke test

#### Phase 313: Block Manual Submit Saat Waktu Habis

- [x] **Phase 313: Block Manual Submit Saat Waktu Habis** — Modify LIFE-03 jadi 2-tier (manual reject tanpa grace, auto reject setelah grace)
 (completed 2026-05-08)
  - **REQ:** TMR-01
  - **Depends on:** 311
  - **Success Criteria:**
    1. Modify `CMPController.SubmitExam()` LIFE-03 block (line ~1618–1631) jadi 2-tier branching `isAutoSubmit`
    2. Tier 1: `!isAutoSubmit && elapsed > allowed` → reject manual dengan TempData error + redirect Assessment
    3. Tier 2: `elapsed > allowed + 2min grace` → reject auto-submit telat (existing LIFE-03 behavior preserved)
    4. Frontend `StartExam.cshtml`: countdown=0 disable tombol Submit manual; auto-submit handler tetap aktif
    5. AuditLog entry rejection alasan `manual_after_timeup` dengan `{UserId, SessionId, ElapsedMin, AllowedMin}`
    6. Verifikasi 3 tipe ber-timer: Online, PreTest, PostTest (Manual exclude)
    7. E2E test 6 skenario manual/auto × before-time/at-time/in-grace/after-grace
  - **Risk:** Medium-High | **Effort:** M-L
  - **Plans:** 3/3 plans complete
    - 313-01-PLAN.md — Wave 0 test infrastructure: SQL seed 7 fixture (.planning/seeds/313-timer-fixtures.sql) + FLOW 313 Playwright 7-test RED state + 313-UAT.md 7-step manual checklist (TMR-01)
    - 313-02-PLAN.md — Wave 1 backend: EnsureCanSubmitExamAsync helper + WriteSubmitBlockedAuditAsync + replace LIFE-03 inline block (2-tier branching D-09 + D-15 AssessmentType exclude C-01) (TMR-01)
    - 313-03-PLAN.md — Wave 1 frontend: ExamSummary.cshtml 3-branch button + retry handler D-10/D-11 + StartExam.cshtml modal info-only spinner C-03 + JS timer flow no setTimeout 10s (TMR-01)

### Phase 313.1: Gap closure Phase 313 - extend seed dengan AssessmentPackages+PackageQuestions+PackageOptions clone supaya fixture 150-156 self-contained untuk live UAT; finalize Playwright FLOW 313 assertion bodies. Resolves F-313-UAT-01 (INSERTED)

**Goal:** Resolve F-313-UAT-01 — extend .planning/seeds/313-timer-fixtures.sql dengan AssessmentPackages(7)+PackageQuestions(21)+PackageOptions(84) supaya CMPController.StartExam packages.Any() resolve true (fixture 150-156 self-contained). Finalize 7 Playwright FLOW 313 test bodies (replace targetRow.toBeVisible() placeholder dengan flow lengkap: click Resume → assert StartExam/ExamSummary navigation → fill answer ATAU verify Tier-1/Tier-2 banner). Hasil: UAT 7-step Phase 313 dapat di-re-run end-to-end via fixture (bukan session-hijack pivot).
**Requirements**: F-313-UAT-01, TMR-01 (carry-over Phase 313)
**Depends on:** Phase 313
**Plans:** 2/2 plans complete

Plans:
- [x] 313.1-01-PLAN.md — Wave 0 SQL seed extend: cleanup chain 6-step FK-respecting + hierarchical INSERT (Sessions OUTPUT identity → Packages cross-join → Questions cross-join × 3 template → Options cross-join × 4 template) + snapshot DB lokal + journal entry (F-313-UAT-01)
- [x] 313.1-02-PLAN.md — Wave 1 Playwright FLOW 313 finalize: helper module exam313.ts (4 function exports) + replace 7 test bodies (313.1-313.7) dengan flow assertion + UAT.md annotation Phase 313.1 update (F-313-UAT-01) (completed 2026-05-08)

#### Phase 314: Fix Regenerate Token untuk Status Upcoming

- [ ] **Phase 314: Fix Regenerate Token untuk Status Upcoming** — Investigative bug fix (repro → root cause → patch minimal)
  - **REQ:** TKN-01
  - **Depends on:** 311
  - **Trigger Condition (dari user):** Status `Upcoming` + `IsTokenRequired=true` + 0 worker yang sudah masuk ujian
  - **Success Criteria:**
    1. Investigation phase: repro bug di environment dev sesuai trigger condition; capture exception/log/HTTP response
    2. Root cause documented di `314-RESEARCH.md` (hipotesis: NRE Schedule.Date / AuditLog FK / concurrency / frontend response handler)
    3. Patch minimal sesuai root cause (defensive null check / audit log try-catch granular / retry / frontend fix)
    4. Logging granular: `_logger.LogError(ex, "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}", id, status, hasStarted)`
    5. Frontend `AssessmentMonitoring.cshtml` line 396–419 & `AssessmentMonitoringDetail.cshtml` line 981–1009: error message dari server JSON dipropagasi ke `alert()` (bukan generik)
    6. Smoke test 3 skenario: Upcoming+0-peserta OK, Upcoming+sebagian-start OK, Open running OK
  - **Risk:** Low-Medium | **Effort:** S-M (investigative)
  - **Plans:** 2 plans
    - 314-01-PLAN.md — Repro & RESEARCH.md (root cause documentation)
    - 314-02-PLAN.md — Patch backend + frontend error propagation + smoke test

> **Wave 5 Sequencing:** Phase 312, 313, 314 independen di file level (AssessmentAdminController vs CMPController vs RegenerateToken endpoint) — bisa dikerjakan parallel. Phase 309 di Wave 3 menyerap SUB-01 jadi tidak ada konflik file dengan Wave 5.

#### Deferred (menunggu klarifikasi user)

- [ ] **EPRV-01** (Preview Essay rubrik/jawaban) — **DEFERRED**, due **2026-05-12**
  - **Action sebelum implementasi:** Smoke test save/load Rubrik. Jika muncul = Jalur A (label fix). Jika kosong padahal di-input = bug binding (perbaiki dulu).
  - Jika user pilih Jalur B (field baru EssayAnswerKey + migrasi DB), defer ke milestone v16.0 karena bertentangan dengan goal v15.0 "tanpa migrasi DB".

#### Wave Sequencing & File Conflicts

- **Wave 1 → Wave 2 → Wave 3 → Wave 4 → Wave 5** (strict sequential per wave)
- **File conflict di `Views/Admin/CreateAssessment.cshtml`:** Phase 304 (label) → Phase 307 (peserta list) → Phase 308 (PrePost validation) — wajib serialize
- **Phase 309 & 310 parallel-eligible** (different files: `CMPController.cs` vs `AssessmentAdminController.cs`)
- **Phase 305 (LBL-01)** menyentuh 4 view berbeda — bisa parallel dengan Phase 304 jika ada kapasitas
- **Wave 5 phases (312, 313, 314) parallel-eligible** — file level independen (AssessmentAdminController.Delete vs CMPController.SubmitExam vs AssessmentAdminController.RegenerateToken)
- **Phase 309 ↔ Wave 5:** SUB-01 di-bundle ke Phase 309 untuk menghindari konflik file di `CMPController.Certificate/CertificatePdf/Results`

#### Coverage Validation

| REQ | Phase | Status |
|-----|-------|--------|
| AUTH-01 | 304 | Pending |
| WIZ-02 | 304 | Pending |
| WIZ-03 | 304 | Pending |
| LBL-01 | 305 | Pending |
| QSCR-01 | 306 | ✅ Complete |
| WIZ-01 | 307 | Pending |
| WIZ-04 | 308 | Pending |
| WCRT-01 | 309 | Pending |
| ESCG-01 | 310 | Pending |
| PERF-01 | 311 | Pending |
| EPRV-01 | DEFERRED | Pending klarifikasi user (due 2026-05-12) |
| DEL-01 | 312 | Pending (added 2026-04-29) |
| TMR-01 | 313 | Pending (added 2026-04-29) |
| SUB-01 | 309 (bundled) | Pending (added 2026-04-29) |
| TKN-01 | 314 | Pending (added 2026-04-29) |

**Active mapped: 14/14 ✓ — Orphans: 0 — Duplicates: 0 — Coverage 15 temuan audit (11 audit 27 April + 4 audit 29 April): 100%**

---

*Roadmap updated: 2026-04-29 (v15.0 Wave 5 added — DEL-01, TMR-01, SUB-01, TKN-01 / Phase 312, 313, 314 + Phase 309 expand)*
