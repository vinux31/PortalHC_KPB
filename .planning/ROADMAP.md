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
- ✅ **v15.0 Audit Findings 27 April 2026** — Phases 304-314 + 313.1 (shipped 2026-05-11) — [archive](milestones/v15.0-ROADMAP.md)
- ✅ **v16.0 QA Test Coverage** — Phases 315-319 (shipped 2026-05-12) — [archive](milestones/v16.0-ROADMAP.md)
- ✅ **v17.0 Assessment Admin Power Tools** — Phases 320-322 (shipped 2026-05-22, archived 2026-05-23) — [archive](milestones/v17.0-ROADMAP.md)
- 🚧 **v18.0 Cascade Delete Hardening + Duplicate TR Fix** — Phases 323-324 (started 2026-05-26)
- 📋 **v19.0 Portal HC Bug Fixes (Sertifikat Ecosystem Audit)** — Phases 325-328 (planned 2026-05-26) — [spec](../docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md)

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

<details>
<summary>✅ v15.0 Audit Findings 27 April 2026 (Phases 304-314 + 313.1) — SHIPPED 2026-05-11</summary>

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
- [x] 313.1-02-PLAN.md — Wave 1 Playwright FLOW 313 finalize: helper module exam313.ts (4 function exports) + replace 7 test bodies (313.1-313.7) dengan flow assertion + UAT.md annotation Phase 313.1 update (F-313-UAT-01)
 (completed 2026-05-08)

#### Phase 314: Fix Regenerate Token untuk Status Upcoming

- [x] **Phase 314: Fix Regenerate Token untuk Status Upcoming** — Investigative bug fix (repro → root cause → patch minimal)
 (completed 2026-05-08)
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
  - **Plans:** 2/2 plans complete
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

Full details: [milestones/v15.0-ROADMAP.md](milestones/v15.0-ROADMAP.md) • Requirements: [milestones/v15.0-REQUIREMENTS.md](milestones/v15.0-REQUIREMENTS.md)

</details>

### ✅ v16.0 QA Test Coverage (Phases 315-319) — SHIPPED 2026-05-12

**Goal:** Membangun automated test infrastructure untuk Portal HC sebagai tooling discovery bug end-to-end.

**Started:** 2026-05-11 | **Shipped:** 2026-05-12 | **Phases:** 315-319 (5 phases, 22 plans) | **Active REQ:** 4 (QA-01, QA-02, QA-08, QA-09)

**Outcome:**
- `tests/e2e/exam-types.spec.ts` 73 sub-tests baseline (15 FLOW A-X coverage)
- `tests/e2e/assessment-matrix.spec.ts` discovery matrix (10 scenarios + sentinels)
- 2 production fixes (SURF-317-A CMPController MA-aware + SURF-317-A1 test fixture)
- Reusable helpers (`examTypes.ts`, `wizardSelectors.ts`, `dbSnapshot.ts`)
- 3 closure reports di `docs/test-reports/2026-05-1[12]-*.md`

Full details: [milestones/v16.0-ROADMAP.md](milestones/v16.0-ROADMAP.md) • Requirements: [milestones/v16.0-REQUIREMENTS.md](milestones/v16.0-REQUIREMENTS.md) • Audit: [v16.0-MILESTONE-AUDIT.md](v16.0-MILESTONE-AUDIT.md)

<details>
<summary>v16.0 phase-level details (collapsed for context efficiency)</summary>

**Goal:** Membangun automated test infrastructure untuk Portal HC sebagai tooling discovery bug end-to-end. Fokus pertama: assessment flow (tipe assessment × tipe soal). Foundation untuk expand test coverage di milestone berikutnya.

**Started:** 2026-05-11 | **Phases:** 315, 316, 317, 318, 319 (5 phases) | **Active REQ:** 1 (QA-01)

#### Phase 315: Assessment Matrix Test

- [x] **Phase 315: Assessment Matrix Test** — Automated Playwright spec yang sweep kombinasi (tipe assessment × tipe soal) end-to-end dengan DB seed temporary + cleanup + bug report markdown
 (completed 2026-05-11)
  - **REQ:** QA-01
  - **Goal:** Build `tests/e2e/assessment-matrix.spec.ts` yang loop 7 discovery skenario (4 mixed per tipe assessment + 3 single-type Online per tipe soal) + 3 sentinel meta-validation. Setiap skenario: peserta1 + peserta2 kerjakan exam → submit → grading manual essay (jika ada) → verify score di result page. Continue-on-fail; semua finding ke `docs/test-reports/2026-05-11-assessment-matrix.md`. DB seed via `tests/sql/assessment-matrix-seed.sql` + RESTORE cleanup di `globalTeardown`.
  - **Success Criteria:**
    1. 7 skenario discovery + 3 sentinel jalan end-to-end di lokal tanpa human intervention via `npx playwright test assessment-matrix`
    2. Report markdown ter-generate dengan struktur sesuai spec (severity, screenshot, hypothesis per finding)
    3. DB lokal kembali ke state pre-test setelah teardown (Layer 4 validation: post-RESTORE row count = 0)
    4. Smoke run protocol lewat sebelum full run (1 skenario via `--grep "Scenario 5"`)
    5. 4-layer meta-validasi (setup, helper, collector, cleanup) semua pass di clean run
    6. Finding (jika ada) actionable: severity + screenshot + URL/lokasi + hypothesis
    7. 5 open questions di spec (MA save flow, Essay save flow, Notes field, ID collision check, URL encoding) terjawab di Wave 0 investigation
  - **Risk:** Medium (test infra baru, seed SQL hand-written) | **Effort:** M-L
  - **Spec:** `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` (commit `94bacecf`) — akan jadi input CONTEXT.md
  - **Plans:** 5/5 plans complete
    - [x] 315-01-PLAN.md — Wave 0 source-code investigation (A1+A2+A6 resolution → 315-INVESTIGATION.md final seed dimensions)
    - [x] 315-02-PLAN.md — Wave 1 helpers foundation (matrixTypes + dbSnapshot + matrixReport collector + examMatrix POM-flat + tests/.gitignore)
    - [x] 315-03-PLAN.md — Wave 1 seed SQL + lifecycle (assessment-matrix-seed.sql + global.setup extend + global.teardown new + playwright.config + SEED_JOURNAL append)
    - [x] 315-04-PLAN.md — Wave 2 spec utama (assessment-matrix.spec.ts 10 test blocks: 7 discovery + 3 sentinel)
    - [x] 315-05-PLAN.md — Wave 3 polish + manual UAT gate (hypothesis renderer refine + whitelist + full run + checkpoint approval)

#### Coverage Validation

| REQ | Phase | Status |
|-----|-------|--------|
| QA-01 | 315 | Pending |

**Active mapped: 1/1 ✓ — Orphans: 0 — Duplicates: 0**

### Phase 316: Fix SubmitExam page-closed bug + matrix test infra polish — resolve cascade fail dari Phase 315 yang block sentinel S8/S9/S10 verification

**Goal:** Surgical hardening Playwright matrix test helper (Promise.all submit race fix + page.isClosed gate + defensive screenshot dengan fallback path renderer) supaya 3 acknowledged gaps Phase 315 UAT tertutup (GAP-315-1 sentinel S8/S9/S10 verifiable, GAP-315-2 screenshot path konsisten, GAP-315-3 full inter-scenario continue-on-fail demonstrated E2E).
**Requirements**: GAP-315-1, GAP-315-2, GAP-315-3 (anchor IDs dari 315-UAT.md lines 82-86)
**Depends on:** Phase 315
**Plans:** 6/6 plans complete

Plans:
- [x] 316-01-PLAN.md — Helper hardening (softAssert re-throw + Promise.all submit + isClosed gate + screenshot fallback)
- [x] 316-02-PLAN.md — Staged validation (S5 + full run) + D-02 server smoke + 316-UAT.md

### Phase 317: Fix SURF-316-A + MA/Essay/Mixed E2E via UI — close exam-type test gap via HC wizard creation

**Goal:** Tutup SURF-316-A (submit selector match dropdown-item hidden + 2-step submit flow incomplete) + buat `tests/e2e/exam-types.spec.ts` 5 FLOW baru via HC UI creation (FLOW K MA, FLOW L Essay+HC grading, FLOW M Mixed, FLOW N AllowAnswerReview=false, FLOW O AddExtraTime) untuk coverage tipe soal yang belum di-test FLOW A-J `exam-taking.spec.ts`. Regression smoke FLOW A-J catat baseline pass rate.
**Requirements:** QA-02 (exam-types coverage)
**Depends on:** Phase 316
**Plans:** 2 plans

Plans:
- [ ] 317-01-PLAN.md — Wave 0 smoke (A4 question order + A5 timer var) + FLOW K MA + FLOW L Essay+HC grading (QA-02)
- [ ] 317-02-PLAN.md — FLOW M Mixed + FLOW N AllowAnswerReview=false + FLOW O AddExtraTime SignalR + regression smoke FLOW A-J baseline (QA-02)

### Phase 318: PreTest/PostTest full cycle + ExamWindowCloseDate + Certificate PDF E2E

**Goal:** Test coverage untuk PreTest/PostTest workflow (paired assessment auto-generated), ExamWindowCloseDate enforcement (server-side reject submit setelah window tutup), AllowAnswerReview=true vs false comparison di Results page, Certificate PDF download verification (NomorSertifikat generated + downloadable). Plus SURF-317 carryover fixes — SURF-317-A1 test fixture (exam-taking.spec.ts:40 selector form-check compat) + SURF-317-A production code (CMPController.cs:2190 MA Results ToLookup refactor).
**Requirements:** QA-08 (advanced exam features E2E coverage)
**Depends on:** Phase 317
**Plans:**
- [x] 318-01-PLAN.md — SURF-317-A1 test fixture patch (exam-taking.spec.ts:40 selector form-check) + Phase 317 regression gate
- [x] 318-02-PLAN.md — SURF-317-A production fix (CMPController.cs ToLookup + MA-aware refactor) + Phase 317 regression rerun gate
- [x] 318-03-PLAN.md — FLOW P PreTest/PostTest paired (P1-P6) + FLOW Q ExamWindowCloseDate reject (Q1-Q4)
- [x] 318-04-PLAN.md — FLOW R Certificate PDF + NomorSertifikat (R1-R5) + FLOW S AllowAnswerReview true vs false paired comparison (S1-S6)
- [x] 318-05-PLAN.md — REQUIREMENTS QA-08 + ROADMAP Phase 318 closure + final regression gate 49/49

### Phase 319: ManualAssessment + Export Excel + Analytics + CertificationManagement E2E

**Goal:** Test coverage untuk ManualAssessment workflow (HC manual entry skor tanpa peserta exam), ManageCategories CRUD, Export Excel endpoint (re-query independent vs API), Analytics dashboard charts (Chart.js v4 indexAxis:'y'), CertificationManagement page (sertifikat lookup + reissue).
**Requirements:** QA-09 (admin features E2E coverage)
**Depends on:** Phase 318
**Plans:**
4/4 plans complete
- [x] 319-02-PLAN.md — FLOW U ManageCategories CRUD + duplicate-reject negative (QA-09)
- [x] 319-03-PLAN.md — W0.V0+W0.W0 smoke + FLOW V Export Excel + FLOW W Analytics dashboard (QA-09)
- [x] 319-04-PLAN.md — W0.X0 smoke + FLOW X CertificationManagement CDP variant + REQUIREMENTS QA-09 + ROADMAP Phase 319 closure + final regression gate ≥73 (72 pass + 1 skip)

</details>

---

<details>
<summary>✅ v17.0 Assessment Admin Power Tools (Phases 320-322) — SHIPPED 2026-05-22</summary>

Full details: [milestones/v17.0-ROADMAP.md](milestones/v17.0-ROADMAP.md) • Requirements: [milestones/v17.0-REQUIREMENTS.md](milestones/v17.0-REQUIREMENTS.md)

</details>

<details>
<summary>v17.0 phase-level details (collapsed for context efficiency)</summary>

**Goal:** Power tools admin/HC untuk assessment — Excel export per-peserta lengkap (Summary + N sheet per peserta, info detail, ElemenTeknis, PNG radar chart, Detail Jawaban) + edit jawaban MC/MA peserta Completed dengan auto-recompute Score/IsPassed/ElemenTeknis + cascade NomorSertifikat & TrainingRecord saat Pass↔Fail flip + audit dual-write (AuditLog generic + AssessmentEditLog granular) + SignalR live monitor update.

**Started:** 2026-05-21 | **Phases:** 320-321 (2 phases, **paralel-able**) | **Active REQ:** 21 (EXP-01..08 + EDIT-01..13)

**Spec:** `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` (commit `c37e55ef`, 4 patch codebase-verified)
**Research per phase:** `.planning/phases/320-assessment-export-per-peserta-excel/320-RESEARCH.md` + `.planning/phases/321-assessment-edit-jawaban-peserta/321-RESEARCH.md` (commit `f442220b`)

#### Phase 320: Assessment Export Per-Peserta Excel

- [x] **Phase 320: Assessment Export Per-Peserta Excel** — Extend `ExportAssessmentResults` jadi 1 sheet "Summary" + N sheet per peserta dengan info detail, tabel ElemenTeknis, PNG spider chart (SkiaSharp), dan Detail Jawaban MC/MA
 (completed 2026-05-21)
  - **REQ:** EXP-01, EXP-02, EXP-03, EXP-04, EXP-05, EXP-06, EXP-07, EXP-08
  - **Goal:** Refactor `AssessmentAdminController.ExportAssessmentResults` (line 3651) — rename sheet "Results"→"Summary" (breaking) + per-peserta loop yang generate sheet content via 2 helper baru (`Helpers/SpiderChartRenderer.cs` PNG via SkiaSharp, `Helpers/SheetNameSanitizer.cs` `{NIP}_{FullName}` format). PNG generate paralel `Task.WhenAll` dengan `MaxDegreeOfParallelism = Environment.ProcessorCount`. No DB schema change.
  - **Success Criteria:**
    1. Export grup assessment menghasilkan workbook dengan tab "Summary" (data tabel ringkas existing) + N tab `{NIP}_{FullName}` untuk peserta Completed + Abandoned (filter exact)
    2. Tab peserta Online: header + tabel ElemenTeknis + PNG radar 500×500 (skip kalau < 3 elemen) + tabel Detail Jawaban MC/MA dengan ✓/✗ dan "Tidak dijawab" untuk soal tanpa response
    3. Tab peserta Manual Entry: header + section Info Sertifikasi Manual + hyperlink `ManualSertifikatUrl` (no chart/ET/detail jawaban)
    4. Sheet name truncated tepat 31 char tanpa collision (NIP unique guarantee), exclude `\ / ? * [ ] :`
    5. Login Admin atau HC export sukses (403 untuk role lain); Worker tidak punya akses
    6. Benchmark 50 peserta < 30 detik response time di lokal (file 3–5 MB)
  - **Risk:** Medium (lib baru SkiaSharp, native asset Win32, performance) | **Effort:** M
  - **Dependencies:** Tidak ada (paralel-able dengan Phase 321)
  - **Research:** `320-RESEARCH.md` 12 task breakdown (full code blocks)
  - **Plans:** 3/3 plans complete
    - [x] 320-01-PLAN.md — Helpers foundation: SkiaSharp PackageReference + SpiderChartRenderer.cs + SheetNameSanitizer.cs (EXP-03, EXP-06)
    - [x] 320-02-PLAN.md — Controller refactor: rename Summary + filter eligible + per-peserta loop + ET section + chart embed + Detail Jawaban + Variant B Manual Entry (EXP-01..07)
    - [x] 320-03-PLAN.md — Perf + UAT: Parallel.ForEachAsync PNG pre-compute + Playwright 4-test (Admin/HC/Worker/benchmark) + manual UAT 8-step + tag v17.0-p320-complete (EXP-07, EXP-08)

#### Phase 321: Assessment Edit Jawaban Peserta

- [x] **Phase 321: Assessment Edit Jawaban Peserta** — Halaman admin/HC untuk edit jawaban MC/MA peserta Completed dengan auto-recompute + cascade cert/TR + audit granular + SignalR live update (completed 2026-05-22)
  - **REQ:** EDIT-01, EDIT-02, EDIT-03, EDIT-04, EDIT-05, EDIT-06, EDIT-07, EDIT-08, EDIT-09, EDIT-10, EDIT-11, EDIT-12, EDIT-13
  - **Goal:** 3 layer baru — (1) Model + migration `AssessmentEditLog`, (2) `GradingService.RegradeAfterEditAsync` + refactor extract `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` no-side-effect, (3) Controller `EditPesertaAnswers` (GET/POST/PreviewEditScore) + View dedicated + JS dirty state + flip modal + dropdown ⋮ di `AssessmentMonitoringDetail.cshtml` + Activity Log "Edit History" tab. Transaction scope membungkus edit+audit+regrade+cascade. SignalR signal baru `workerAnswerEdited`.
  - **Success Criteria:**
    1. Admin/HC dapat akses `/AssessmentAdmin/EditPesertaAnswers/{id}` untuk session Completed, edit MC/MA, simpan dengan reason wajib (5 preset + Lainnya freetext)
    2. POST save auto-recompute: Score+IsPassed updated, `SessionElemenTeknisScores` DELETE+recompute, AuditLog + AssessmentEditLog granular entries tertulis (snapshot text + Actor + Reason)
    3. Pass↔Fail flip cascade: cabut NomorSertifikat + TrainingRecord="Failed" (Pass→Fail) atau generate NomorSertifikat baru + TrainingRecord="Passed" (Fail→Pass, kalau `GenerateCertificate && !PreTest`). Modal konfirmasi muncul via dry-run `PreviewEditScore` sebelum submit
    4. 2 admin edit session sama bersamaan → admin kedua kena stale "Sesi sudah diubah admin lain" (concurrency token UpdatedAt)
    5. Session non-Completed / IsManualEntry / Assessment Proton Tahun 3 → Edit page block + UI dropdown item hidden (IsEditable gating)
    6. SignalR broadcast: monitor di tab/browser lain auto-update score+result cell + toast `{actorRole} {actorName} edit jawaban {workerName}: {oldScore}→{newScore}, {flip}`
    7. Tab "Edit History" di modal Activity Log menampilkan timeline lengkap (timestamp, soal, old→new, actor, reason)
    8. Migration `AddAssessmentEditLogs` apply + rollback test lokal lulus
  - **Risk:** High (transaction + cascade + concurrency + audit + UI dropdown refactor + new migration) | **Effort:** L
  - **Dependencies:** Tidak ada (paralel-able dengan Phase 320; perlu koordinasi merge di `AssessmentAdminController.cs` karena kedua phase edit file ini)
  - **Research:** `321-RESEARCH.md` 13 task breakdown (full code blocks)
  - **Plans:** 5/5 plans complete
    - [x] 321-01-PLAN.md — Model + Migration + Helper + ViewModels foundation (EDIT-02, EDIT-06, EDIT-13)
    - [x] 321-02-PLAN.md — Service layer: ComputeScoreAndETInternalAsync + RegradeAfterEditAsync + PreviewScoreAsync (EDIT-03, EDIT-04)
    - [x] 321-03-PLAN.md — Controller GET + View + JS dirty/flip + PreviewEditScore dry-run (EDIT-01, EDIT-02, EDIT-05, EDIT-10)
    - [x] 321-04-PLAN.md — POST SubmitEditAnswers (transaction + audit + regrade) + Dropdown ⋮ hybrid + SignalR workerAnswerEdited handler (D-07 8s LOCKED) (EDIT-02, EDIT-03, EDIT-04, EDIT-06, EDIT-07, EDIT-08, EDIT-09, EDIT-12)
    - [x] 321-05-PLAN.md — Activity Log Edit History tab + Playwright spec HARD GATE 4/4 + Manual UAT (SEED_WORKFLOW pre/cleanup) + Tag + Merge main + IT notify (EDIT-04, EDIT-07, EDIT-09, EDIT-11, EDIT-13)

#### Coverage Validation v17.0

| REQ | Phase | Status |
|-----|-------|--------|
| EXP-01..08 | 320 | ✅ SHIPPED |
| EDIT-01..13 | 321 | ✅ SHIPPED |
| FILTER-01..03 (Bug 1 double filter + Bug 2 cross-tab + Bug 3 pagination) | 322 | ✅ SHIPPED |

**Active mapped: 24/24 ✓ — Orphans: 0 — Duplicates: 0**

### ✅ Phase 322: filter-scope-per-tab-manage-assessment — SHIPPED 2026-05-22

**Goal:** Rollback Phase 311 Plan 02 shared filter shell; per-tab native filter (Tab 1 search+kategori+status, Tab 2 bagian+kategori-training+unit+status+nama/nopeg, Tab 3 sub-tab client-side). Bug 1 double filter + Bug 2 cross-tab contamination + Bug 3 pagination filter state eliminated.
**Requirements**: 3 bug (double filter, cross-tab contamination, pagination)
**Depends on:** Phase 321
**Plans:** 3 plans (all SHIPPED)
**UAT:** 11/12 PASS + 1 N/A (`322-UAT.md`)
**Tag:** `v17.0-p322-complete` (pending push)

Plans:
- [x] 322-01-PLAN.md — Partial Views Filter HTMX Refactor (Tab 1 filter+pagination, Tab 2 5-field, Tab 3 sub-tab DOM hooks) — 4 commit atomic
- [x] 322-02-PLAN.md — Shell View Cleanup + Controller Cleanup (delete shared form + cross-tab listener + endpoint updater; add filterTrainingRows JS; wrapper hx-vals D-21 Strategy D Hybrid; ViewBag.Categories cache drop di shell action) — 2 commit
- [x] 322-03-PLAN.md — Manual UAT 12-step + Handoff (Playwright automation; 2 critical bug discovered + fixed: ViewBag null coalesce + wrapper hx-vals → URL migration)

**Post-shipping fix (2026-05-23):** Browser visual verification discovery — CSS dead-code Phase 311.1 (commit `b17292f7`) hide Tab 2+3 filter. 2 follow-up fix: `b0b4049b` hoist `_HistoryTab` filter outside `@if/@else` + `3cdccfb4` delete `site.css:93-122` dead rules. UAT `13046757` amend Step 4+7 false-positive. See `milestones/v17.0-ROADMAP.md` Post-Verification Discovery section.

</details>

## 🚧 v18.0 Cascade Delete Hardening (Phase 323) — STARTED 2026-05-26

**Goal:** Tutup oversight Phase 321 (model `AssessmentEditLog` baru, FK Restrict ke `AssessmentSession`) di Phase 312 cascade. 3 endpoint `DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup` tidak hapus `AssessmentEditLogs` duluan → session yang pernah di-edit soal exception "Gagal menghapus assessment".

**Started:** 2026-05-26 | **Phases:** 323 (1 phase) | **Active REQ:** 1 (CASCADE-01)

**Repro evidence (Dev 10.55.3.3, 2026-05-26):**
- AssessmentSession Id 1 (`[TEST] Online Assessment Audit`, 0 edit logs) — DELETED OK
- AssessmentSession Id 2 (same title, has edit logs) — EXCEPTION caught
- AssessmentSession Id 5 (`[Test] Tes Lagi`, has edit logs) — EXCEPTION caught

### Phase 323: Fix cascade bug AssessmentEditLogs di 3 endpoint delete assessment

- [x] **Phase 323: Cascade AssessmentEditLogs di 3 endpoint delete assessment** (completed 2026-05-26)
  - **REQ:** CASCADE-01
  - **Depends on:** Phase 322 (Phase 321 `AssessmentEditLog` model + Phase 312 cascade pattern existing)
  - **Goal:** Tambah `RemoveRange(AssessmentEditLogs)` block sebelum cascade existing di 3 endpoint di `Controllers/AssessmentAdminController.cs` (~line 2071, ~2215, ~2348). Wrap di transaction scope existing (line 2040, 2184, 2313). Logging info per cascade — sama pola dengan `PackageUserResponses` / `AttemptHistory` / `AssessmentPackages`.
  - **Success Criteria:**
    1. Hapus session belum pernah di-edit → tetap sukses (no regression)
    2. Hapus session sudah di-edit ≥1 soal → sukses, `AssessmentEditLogs` ikut terhapus
    3. Hapus group dengan campuran sibling no-edits + edits → sukses
    4. Audit log `DeleteAssessment*` tercatat normal (description sebelumnya tidak berubah)
    5. Transaction rollback bersih kalau exception lain terjadi
    6. Smoke test 3 skenario di lokal: (a) no-edits delete OK, (b) 1+ edits delete OK, (c) group campuran delete OK
    7. Tidak ubah schema DB / model / migration / endpoint signature
  - **Risk:** Low | **Effort:** S
  - **Plans:** 1/2 plans complete
    - [x] 323-01-PLAN.md — Wave 1 controller cascade patch 3 endpoint (DeleteAssessment + DeleteAssessmentGroup + DeletePrePostGroup) + snapshot preDeleteEditLogsCount + audit description EditLogsCount token (CASCADE-01)
    - [ ] 323-02-PLAN.md — Wave 2 Playwright E2E spec Phase323_CascadeAssessmentEditLogs 3 test (no-edits / with-edits / group-mixed) + seed SEED_WORKFLOW lifecycle + audit log DB verify + manual UAT 3 skenario + commit + IT notify (CASCADE-01)
  - **Files affected:** `Controllers/AssessmentAdminController.cs` (3 spot) + `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` (NEW) + `docs/SEED_JOURNAL.md` (append)

**Active mapped: 1/1 ✓ — Orphans: 0 — Duplicates: 0**

### Phase 324: Fix duplicate TrainingRecord auto-create on assessment completion

- [x] **Phase 324: Fix duplicate TrainingRecord auto-create on assessment completion**
 (completed 2026-05-26)
  - **REQ:** DUPL-01, DUPL-02, DUPL-03, DUPL-04, DUPL-05
  - **Depends on:** Phase 323
  - **Goal:** Hapus mekanisme auto-create `TrainingRecord` saat session assessment completed di 3 lokasi production (`Services/GradingService.cs:255-285` GradeAndCompleteAsync + `Controllers/AssessmentAdminController.cs:3404-3421` FinalizeEssayGrading + `Services/GradingService.cs:483-567` RegradeAfterEditAsync Pass↔Fail cascade). Resolve regression dari commit `766011b6` (2026-04-10) yang re-introduce auto-create TR setelah commit `79284609` (2026-03-18) menghapusnya — visual duplicate 2 row di `/CMP/Records` hilang. Cleanup data legacy lokal (SEED_WORKFLOW) + IT handoff HTML untuk Dev/Prod cleanup. Subtract phase: NO migration, NO model change, NO schema change.
  - **Success Criteria:**
    1. Worker submit assessment biasa (non-essay) → `/CMP/Records` hanya tampil 1 row "Assessment Online" (bukan 2)
    2. Block insert TR di 3 lokasi production HILANG (cross-grep `TrainingRecords.(Add|AddAsync|AddRange)` di `Services/` + `Controllers/AssessmentAdminController.cs` + `Controllers/CMPController.cs` returns 0 hit)
    3. `dotnet build` 0 Error setelah 3 file edit
    4. Cert generate logic (`NomorSertifikat` di `GradeAndCompleteAsync` + `RegradeAfterEditAsync` Fail→Pass) TETAP UTUH
    5. Cert revoke logic (`NomorSertifikat=null` + `ValidUntil=null` di `RegradeAfterEditAsync` Pass→Fail) TETAP UTUH
    6. Playwright UAT 7 scenario (S1 worker submit non-essay + S2 PreTest skip + S3 Essay finalize + S4 AkhiriUjian + S5 AkhiriSemuaUjian + S6 Regrade Pass→Fail + S7 Regrade Fail→Pass) — minimum S1+S2 green
    7. Data legacy cleanup lokal: pre-count > 0, post-count = 0, idempotent re-run safe
    8. `docs/SEED_JOURNAL.md` entry baru status `cleaned`
    9. `docs/DB_HANDOFF_IT_2026-05-26.html` exists dengan Pertamina branding + embedded SQL script + ordering callout (Step 1 deploy code DULU)
    10. AssessmentSessions TIDAK ter-touch (sole source-of-truth utuh)
  - **Risk:** Low (subtract phase) | **Effort:** S-M (3 file edit + UAT + cleanup + handoff)
  - **Plans:** 4/4 plans complete
    - [x] 324-01-PLAN.md — Wave 1 code edit: 3 lokasi block hapus (GradeAndComplete + FinalizeEssay + RegradeAfterEdit Pass↔Fail) + cross-grep audit final (DUPL-01)
    - [x] 324-02-PLAN.md — Wave 2 Playwright UAT 7 scenario + helper module phase324.ts + checkpoint user verify (DUPL-02)
    - [x] 324-03-PLAN.md — Wave 3 data cleanup lokal: schema verify A3 + orphan check OQ#3 + SQL script + BACKUP/RESTORE + SEED_JOURNAL + checkpoint (DUPL-03, DUPL-05)
    - [x] 324-04-PLAN.md — Wave 3 IT handoff HTML doc Pertamina-branded (DUPL-04)
  - **Files affected:** `Services/GradingService.cs` (2 spot) + `Controllers/AssessmentAdminController.cs` (1 spot) + `tests/e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` (NEW) + `tests/e2e/helpers/phase324.ts` (NEW) + `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql` (NEW) + `docs/SEED_JOURNAL.md` (append) + `docs/DB_HANDOFF_IT_2026-05-26.html` (NEW)
  - **Wave structure:** Wave 1 (Plan 01) → Wave 2 (Plan 02) → Wave 3 (Plan 03 + Plan 04 parallel — no file conflict)

#### Coverage Validation v18.0 (updated 2026-05-26 setelah Phase 324 planned)

| REQ | Phase | Status |
|-----|-------|--------|
| CASCADE-01 | 323 | Pending |
| DUPL-01 | 324 | Pending |
| DUPL-02 | 324 | Pending |
| DUPL-03 | 324 | Pending |
| DUPL-04 | 324 | Pending |
| DUPL-05 | 324 | Pending |

**Active mapped: 6/6 ✓ — Orphans: 0 — Duplicates: 0**

---

## v19.0 Portal HC Bug Fixes (Sertifikat Ecosystem Audit) — Phases 325-328

**Source:** `docs/sertifikat-ecosystem/bug-findings.html` — 6 bug Portal HC actionable (1 HIGH + 5 MED) + Phase 323 deferred audit sweep
**Spec:** `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` + `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` (Phase 328)
**Strategy:** Sequential strict 325 → 326 → 327 (code fix) + 328 (audit-only, parallel-safe). IT promo Dev 1× batch akhir setelah Phase 327 ship. Phase 328 deliverable RESEARCH.md mungkin spawn fix phase di v20.0.
**Est. total effort:** ~3-4 hari coding + UAT batch akhir + ~1-2 jam audit Phase 328
**Started:** Pending v18.0 completion

### Phase 325: Security Hardening (P01 + P02 + P05 quick patch)

- [x] **Phase 325: Security Hardening upload + delete error UX** ✅ SHIPPED 2026-05-27 (commit range 7069ead2..77a9c375)
  - **Bug:** P01 (HIGH path traversal), P02 (MED MIME magic byte), P05 (MED hard delete FK quick patch)
  - **Depends on:** Phase 324
  - **Goal:** Tutup security gap upload file (path traversal + magic byte validation) + perbaiki UX delete error 500 dengan pre-check referencing + try/catch + TempData error friendly. Soft delete proper defer ke v20.0.
  - **Success Criteria:** ALL PASS (browser-verified Playwright MCP + xUnit 10/10)
    1. ✅ SC-1: `../../malicious.pdf` strip flat di uploads/certificates/ + LogWarning audit (xUnit SaveFileAsync)
    2. ✅ SC-2: notepad.exe rename .pdf REJECT verbatim "Isi file tidak cocok dengan ekstensi (magic byte mismatch)." (browser)
    3. ✅ SC-3: PDF + JPG + PNG asli lolos 3/3 (browser)
    4. ✅ SC-4: Parent A delete BLOCKED "Tidak bisa hapus: 1 sertifikat lain..." (browser + sqlcmd seed child)
    5. ✅ SC-5: Standalone delete sukses (browser)
    6. ✅ SC-6: dotnet test 10/10 pass
  - **Risk:** Low | **Effort:** S (~2-3 jam)
  - **Files affected:** `Helpers/FileUploadHelper.cs` + `Controllers/TrainingAdminController.cs` (line 206-215, 221-233, 459-471, 527-548, 732-753) + `Controllers/AssessmentAdminController.cs` (line 2011-2169) + `Models/AssessmentConstants.cs` + `HcPortal.Tests/` (NEW xUnit project) + `HcPortal.sln` + `HcPortal.csproj` (DefaultItemExcludes)
  - **Migration:** ❌ Tidak ada
  - **Plans:** 5 plans
    - [x] 325-01-PLAN.md — xUnit project HcPortal.Tests/ bootstrap + 5 GREEN + 2 SKIP test
    - [x] 325-02-PLAN.md — Helper P01 + P02 + AssessmentConstants.MagicBytes + ILogger? opsional + flip 7/7 GREEN
    - [x] 325-03-PLAN.md — Refactor 3 inline duplicate site di TrainingAdminController → helper
    - [x] 325-04-PLAN.md — P05 pre-check + catch DbUpdateException di 3 endpoint delete
    - [x] 325-05-PLAN.md — UAT 5/5 SC PASS (xUnit + Playwright MCP browser)

### Phase 326: Validator Hardening (P03 + P06)

- [ ] **Phase 326: Validator hardening Add/Edit Training form**
  - **Bug:** P03 (MED cycle detection via DAG enforcement), P06 (MED Permanent + ValidUntil reject)
  - **Depends on:** Phase 325
  - **Goal:** Cegah data kontradiktif tersimpan via form Add/Edit Training. DAG enforcement: tanggal renewal harus > tanggal source (cycle otomatis ditolak via monotonic constraint). Permanent + ValidUntil isi → reject di ModelState.
  - **Success Criteria:**
    1. Add TR renewal dengan tanggal lebih awal dari source → form error display (P03)
    2. Add TR renewal dengan tanggal valid > source → lolos (P03 no regression)
    3. Add TR Permanent + ValidUntil isi → form error display field ValidUntil (P06)
    4. Add TR Permanent + ValidUntil null → lolos (P06 no regression)
    5. Add TR Annual + ValidUntil valid → lolos (P06 no regression)
    6. Edit case: tidak boleh renewal dirinya sendiri (P03 self-renewal check)
  - **Risk:** Low | **Effort:** S (~1-2 jam)
  - **Files affected:** `Controllers/TrainingAdminController.cs` (Add + Edit POST handler, sekitar line 253-265) + `Models/EditTrainingRecordViewModel.cs` (extend +3 field) + `Views/Admin/EditTraining.cshtml` (section card + span) + `Views/Admin/AddTraining.cshtml` (span)
  - **Migration:** ❌ Tidak ada
  - **Plans:** 3 plans
    - [ ] 326-01-PLAN.md — Backend validator (P03 DAG TR+AS branch + self-renewal + P06 Permanent/ValidUntil) di Add+Edit POST + VM extension 3 field
    - [ ] 326-02-PLAN.md — Razor view tweaks (section card "Renewal Source" + clear button di EditTraining + 2 span asp-validation-for ValidUntil di Edit/Add)
    - [ ] 326-03-PLAN.md — UAT 6 SC manual repro (browser localhost:5277) + commit + push approval gate

### Phase 327: Timezone DateOnly Refactor (P04)

- [ ] **Phase 327: Migrate ValidUntil DateTime → DateOnly (eliminasi timezone drift permanen)**
  - **Bug:** P04 (MED DateTime.Now vs UtcNow mix)
  - **Depends on:** Phase 326
  - **Goal:** Eliminasi timezone drift permanen untuk `ValidUntil` dengan migrasi `DateTime?` → `DateOnly?`. Cert validity semantik harian — komponen jam tidak relevan. `DateTime.Now` di lokasi non-ValidUntil tidak disentuh (defer v20.0).
  - **Success Criteria:**
    1. EF migration `ChangeValidUntilToDateOnly` apply sukses (datetime2 → date di 2 tabel)
    2. Pre-migration SQL check confirm zero row punya komponen jam non-zero
    3. `DeriveCertificateStatus` unit test 5 case pass (Expired / AkanExpired / Aktif / Permanent / null)
    4. Add training Annual + ValidUntil today+1 → status display "AkanExpired" benar
    5. Display ValidUntil di 5 halaman wajib (ManageAssessment, RenewalCertificate, CMP/Records, CDP/CertificationManagement, Worker dashboard) — tanggal benar tanpa jam
    6. PDF generation `/CMP/CertificatePdf/{id}` — format tanggal di PDF tetap correct
    7. Rollback EF `Down()` migration siap kalau ada drama
  - **Risk:** Medium (EF migration + multi-view binding) | **Effort:** M (~1 hari)
  - **Files affected:** `Models/TrainingRecord.cs` + `Models/AssessmentSession.cs` + `Models/CertificationManagementViewModel.cs` + `Migrations/{ts}_ChangeValidUntilToDateOnly.cs` (NEW) + Razor view minor adjust
  - **Migration:** ✅ `ChangeValidUntilToDateOnly`

### Phase 328: Cascade Audit Sweep — Delete* Endpoints (Audit-Only)

- [ ] **Phase 328: Enumerate + audit Delete* endpoints terhadap 7-dimension cascade-safety checklist**
  - **Source:** Post-Phase-323 deferred per `323-CONTEXT.md:122` (DeleteCategory/Package/Question/Worker/Training/dll out-of-scope 323)
  - **Depends on:** Phase 327 (sequential within v19.0); Phase 323 (gold-standard pattern reference)
  - **Goal:** Enumerate every `Delete*` method di `Controllers/*.cs` + `Services/*.cs` dan audit terhadap 7-dimension cascade-safety checklist (FK risk, file-DB atomicity, audit log, role check, renewal chain null-clear, error handling, transaction wrap). Severity tag per row. Produce `328-RESEARCH.md` deliverable. No code change, no fix phase spawn (potential fix phase di v20.0).
  - **Success Criteria:**
    1. RESEARCH.md 9-section deliverable: enumeration table + 7-dim audit per row + severity per row + cross-ref Phase 323 pattern + remediation recommendation
    2. Coverage: 100% Delete* methods di Controllers/ + Services/ enumerated (grep verified)
    3. Severity tag distribusi: HIGH/MED/LOW count summary
    4. No code change committed (audit-only enforcement)
  - **Risk:** Very Low (read-only) | **Effort:** S (~1-2 jam single session)
  - **Reference spec:** `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` (commit `02f620be`)
  - **Files affected:** `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` (NEW, audit-only)
  - **Migration:** ❌ Tidak ada
  - **Plans:** 1 plan
    - [ ] 328-01-PLAN.md — Enumerate Delete* endpoints + audit 7-dim cascade-safety checklist per row + write 9-section RESEARCH.md (10 task, audit-only, no code change)

#### Coverage Validation v19.0

| Bug | Phase | Status |
|-----|-------|--------|
| P01 Path Traversal | 325 | ✅ SHIPPED |
| P02 MIME Magic Byte | 325 | ✅ SHIPPED |
| P03 Cycle Detection | 326 | Pending |
| P04 Timezone DateOnly | 327 | Pending |
| P05 Hard Delete FK quick patch | 325 | ✅ SHIPPED |
| P06 Permanent+ValidUntil | 326 | Pending |
| Cascade audit sweep (N/A, audit-only) | 328 | Pending |

**Active mapped: 6/6 bug ✓ + 1 audit phase — Orphans: 0 — Duplicates: 0**

---

## Backlog

Unsequenced ideas captured untuk future milestone planning. Promote via `/gsd-review-backlog` saat siap masuk active milestone.

### Phase 999.1: Realtime Assessment SignalR (BACKLOG)

**Goal:** HC monitoring action (reset / forceClose / progress) auto-update worker exam page tanpa reload, plus worker progress live ke HC monitor — eliminasi UX gap real-time 2-arah HC ↔ Worker selama assessment lifecycle.

**Context:**
- Foundation existing: Phase 302 (`extraTime` SignalR) + Phase 321 (`workerAnswerEdited` broadcast)
- Tambah event baru: `resetExam`, `forceCloseExam`, `examProgressUpdate`
- Source todo: `.planning/todos/pending/realtime-assessment.md` (created 2026-03-09, phase-133-checkpoint)

**Requirements:** TBD (perlu scope event list final + grading impact + reconnection strategy + UAT 2-sisi matrix)

**Effort estimate:** M-L (3+ event SignalR + UAT 2-sisi + reconnection handling)

**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd-review-backlog when ready)

---

*Roadmap updated: 2026-05-27 (Phase 328 promoted dari backlog → v19.0 active, depends on Phase 327; Coverage table updated P01/P02/P05 = SHIPPED).*
*Prev: 2026-05-27 (Phase 328 plan generated — 328-01-PLAN.md, 10 task audit-only single wave).*
*Prev: 2026-05-27 (Phase 328 added — Cascade Audit Sweep Delete* endpoints, audit-only, spec commit 02f620be).*
*Prev: 2026-05-27 (backlog Phase 999.1 Realtime Assessment SignalR added).*
*Prev: 2026-05-26 (v19.0 planned — 6 bug Portal HC actionable dari sertifikat-ecosystem audit, 3 phase sequential, IT promo batch akhir).*
