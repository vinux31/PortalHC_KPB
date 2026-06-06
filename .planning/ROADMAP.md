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
- ✅ **v18.0 Cascade Delete Hardening + Duplicate TR Fix** — Phases 323-324 (shipped 2026-05-29) — [archive](milestones/v18.0-ROADMAP.md)
- ✅ **v19.0 Portal HC Bug Fixes (Cascade Hardening)** — Phases 325-335 (shipped local 2026-05-28, audited 2026-05-29) — [audit](v19.0-MILESTONE-AUDIT.md) — [spec](../docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md)
- ✅ **v20.0 CMP Records Overhaul + Cilacap UX/Restore** — Phases 336-339 (shipped local + archived 2026-06-02, 39/39 REQ) — [archive](milestones/v20.0-ROADMAP.md) — [audit](milestones/v20.0-MILESTONE-AUDIT.md)
- ✅ **v21.0 ManageOrganization Overhaul + Level Label CRUD** — Phases 340-344 (shipped local + closed 2026-06-04, 26/26 REQ) — [roadmap](milestones/v21.0-ROADMAP.md) — [audit](milestones/v21.0-MILESTONE-AUDIT.md)
- ✅ **v22.0 CMP-06 + Assessment/Monitoring Audit Fixes** — Phases 345-349 (shipped local + audited 2026-06-05, 60/60 REQ) — [archive](milestones/v22.0-ROADMAP.md) — [audit](milestones/v22.0-MILESTONE-AUDIT.md)
- ✅ **v23.0 CMP/Records Search & Filter Consistency Audit** — Phases 350-351 (shipped local + audited 2026-06-06, 7/7 REQ SF-01..07) — [archive](milestones/v23.0-ROADMAP.md) — [audit](milestones/v23.0-MILESTONE-AUDIT.md)

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
  - **Plans:** 4 plans (0 complete) -- generated 2026-06-04
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
  - **Plans:** 4 plans (0 complete) -- generated 2026-06-04
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

> **Wave 5 Sequencing:** Phase 312, 313, 314 independen di file level (AssessmentAdminController vs CMPController vs RegenerateToken endpoint) — bisa dikerjakan parallel. Phase 309 di Wave 3 di-expand dengan REQ SUB-01 jadi tidak ada konflik file dengan Wave 5.

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
  - **Plans:** 4 plans (0 complete) -- generated 2026-06-04
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

## v19.0 Portal HC Bug Fixes (Cascade Hardening) — Phases 325-335 ✅ ARCHIVED

**Status:** SHIPPED LOCAL 2026-05-29 (push pending IT availability). Audit PASSED 16/16 REQ + 11/11 phase + integration COHERENT.
**Archive:** [v19.0-ROADMAP.md](milestones/v19.0-ROADMAP.md) | [v19.0-REQUIREMENTS.md](milestones/v19.0-REQUIREMENTS.md) | [v19.0-MILESTONE-AUDIT.md](milestones/v19.0-MILESTONE-AUDIT.md)
**Phase dirs:** `.planning/milestones/v19.0-phases/` (11 phase dir 325-335 moved 2026-05-30)
**Highlights:** SEC-01..03 (path traversal + magic byte + hard delete FK) + VAL-01..02 (DAG cycle + Permanent/ValidUntil) + TZ-01 (DateOnly refactor) + CSCD-AUDIT (19 endpoint sweep) + CSCD-01..07 (7 cascade hardening).

---

## Backlog

Unsequenced ideas captured untuk future milestone planning. Promote via `/gsd-review-backlog` saat siap masuk active milestone.

### Phase 999.2: CMP/Records Team View search extend ke Assessment title (PROMOTED -> v23.0 Phase 350, 2026-06-05)

**Goal:** Search Team View di `CMP/Records` (`searchScope`="Keduanya") ikut mencocokkan judul **assessment**, bukan hanya Nama/NIP + judul Training. User cari nama assessment (mis. "ojt v14.2") → saat ini 0 worker meski worker punya assessment itu.

**Context:**
- Ditemukan saat UAT Phase 349 (2026-06-05): search "ojt v14.2" (assessment title) di `CMP/Records` Team View "Keduanya" → "Showing 0 workers".
- Root cause: `WorkerDataService.GetWorkersInSection` (`Services/WorkerDataService.cs:401-417`) — scope "Keduanya" = union Nama/NIP **OR Training.Judul**, TIDAK termasuk Assessment judul. Desain REC-06 D-07 (Phase 346) sengaja scope Training-only.
- BUKAN regresi Phase 349 (page Phase 345-347; commit 349 tak sentuh CMPController/GetWorkersInSection).

**Requirements:** TBD (perlu keputusan: extend "Keduanya" jadi Nama/NIP + Training + Assessment, ATAU tambah scope "Assessment" eksplisit di dropdown Lingkup; cek dampak Export Assessment/Training + badge count per-worker tetap utuh per D-07).

**Effort estimate:** S (1 predicate cabang di GetWorkersInSection + opsi dropdown Lingkup + test)

**Plans:** 0 plans

Plans:
- [x] PROMOTED 2026-06-05 -> v23.0 SF-01/SF-02/SF-06 (Phase 350). Decision: extend scope + dropdown Lingkup jujur + export parity; preserve REC-06 D-07. See spec 2026-06-05-cmp-records-search-filter-audit.md.

### Phase 356: Audit Fix Assign Coach-Coachee (pastikan fungsi Assign benar)

**Goal:** Memastikan fitur HC/Admin Assign Coach×Coachee berfungsi benar — perbaiki 7 temuan audit 2026-06-06 (`Controllers/CoachMappingController.cs`). Independen, jalur file beda dari kerja lain.

**Context:**
- Asalnya addon off-theme ke v24.0 (dibuat sesi paralel). **v24.0 di-revert 2026-06-06** → entry dipindah ke Backlog (decoupled dari image-work). Tarik ke active milestone via `/gsd-review-backlog` saat siap.
- Spec code+data-verified lengkap (AF-1..7). Restore dari backup branch `backup/v24.0-pre-revert`.

**Requirements (audit findings AF-1..7):**
- **AF-1 (HIGH, confirmed):** `GetEligibleCoachees` L1291-1322 bandingkan progress unit-coachee vs total deliverable SEMUA unit track → coachee track multi-unit (terbukti track id=4, 2 unit) tak pernah eligible Assessment Proton. Fix: hitung expected deliverable per-unit coachee.
- **AF-2 (MED):** batch-assign paksa 1 Section/Unit → AutoCreateProgress salah unit bila coachee beda unit.
- **AF-3 (MED):** `MarkMappingCompleted` set IsCompleted tapi IsActive tetap true → coachee graduated terblok re-assign. Semantik graduated **OPEN** (D-2 belum final).
- **AF-4 (LOW-MED):** `Reactivate` korelasi via DeactivatedAt ±5s window rapuh.
- **AF-5 (LOW):** `ApproveReassignSuggestion` tak kirim notifikasi.
- **AF-6 (LOW):** pesan error duplikat-coachee generic saat race.
- **AF-7 (INFO):** progression-warning loop N+1 query.

**Effort estimate:** M (AF-1 eligibility per-unit logic-bearing + xUnit + UAT; AF-2..7 bertahap)

**Spec:** `docs/superpowers/specs/2026-06-06-coach-coachee-assign-audit-fix.md` (AF-1..7 code+data-verified)

**Plans:** 0 plans (run `/gsd-plan-phase 356` setelah dipromote ke milestone aktif)

## v20.0 CMP Records Overhaul + Cilacap UX/Restore — Phases 336-339 ✅ ARCHIVED

**Status:** SHIPPED LOCAL 2026-06-02 (push pending IT availability). Audit PASSED 39/39 REQ + 4/4 phase + integration COHERENT.
**Archive:** [v20.0-ROADMAP.md](milestones/v20.0-ROADMAP.md) | [v20.0-REQUIREMENTS.md](milestones/v20.0-REQUIREMENTS.md) | [v20.0-MILESTONE-AUDIT.md](milestones/v20.0-MILESTONE-AUDIT.md)
**Highlights:** REST-01..03 (PreTest loss root cause + Strategy A locked) + CMP-01..26 (Records overhaul Approach C: 15 bug + 7 UX + 5 quality + 3 arch SQL push-down + pagination) + CIL-01..06 (6 Cilacap admin UX gap) + REST-04..07 (restore execute + guardrail backup + naming + DEV_WORKFLOW SOP) + Phase 339 gap closure (CIL-06 UI + REST-04 dual nav + REST-06 regex validator).

---

## v21.0 ManageOrganization Overhaul + Level Label CRUD — Phases 340-344 ✅ SHIPPED LOCAL (2026-06-04, push pending IT)

**Status:** SHIPPED LOCAL 2026-06-04. 5/5 phase, 16 plan complete. Audit passed (26/26 REQ) → `milestones/v21.0-MILESTONE-AUDIT.md`. Tag `v21.0`. Bundle push pending IT. Started 2026-06-02.
**Spec:** [docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md](../docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md)
**Milestone roadmap:** [v21.0-ROADMAP.md](milestones/v21.0-ROADMAP.md) | [v21.0-REQUIREMENTS.md](milestones/v21.0-REQUIREMENTS.md) — 26 REQ
**Phase range:** 340-344 (5 phase sequential, ~5 hari)

### Phase 340: Foundation — Tabel + Service + Cache

- [ ] **Phase 340: Foundation — OrgLabel Table + Service + Cache + Endpoint**
  - **REQ:** ORG-LABEL-01, ORG-LABEL-02, ORG-LABEL-03, ORG-LABEL-07
  - **Depends on:** Tidak ada (foundation, paralel-able dgn Phase 341+342+343 setelah Phase 340 selesai)
  - **Goal:** Layer dasar yang dipakai phase berikutnya untuk akses label dynamic. Deliverables: (1) Entity `Models/OrganizationLevelLabel.cs` + EF migration `AddOrganizationLevelLabel` (CreateTable only per D-01, NO HasData) + idempotent runtime seed di `Data/SeedData.cs` (3 baris default Level 0/1/2 -> Bagian/Unit/Sub-unit klasifikasi permanent+prod-required). (2) `IOrgLabelService` + `OrgLabelService` Scoped (D-06 captive dep avoid) dgn IMemoryCache no-TTL manual invalidate + fallback `"Level {N}"` (D-07) + reuse `AuditLogService.LogAsync` (D-04 field mapping `OrgLabel-{Add|Update|Delete}`). (3) NEW controller `OrgLabelController` + endpoint `GET /Admin/GetLevelLabels` `[Authorize]` JSON dict (D-03). (4) D-12 SeedData convention fix: `SeedData.cs:90` Level 1->0 + `:99` Level 2->1 (align actual DB 0-indexed + cascade `OrganizationController.AddOrganizationUnit:95`). (5) xUnit `OrgLabelServiceTests` 2 [Fact] (TEST-01 happy + fallback). (6) `docs/DB_HANDOFF_IT_2026-06-03.html` formal handoff IT (D-09).
  - **Success Criteria:**
    1. EF migration `AddOrganizationLevelLabel` apply tanpa error + seed default 3 baris hadir di tabel (verified via sqlcmd).
    2. `OrgLabelService.GetLabel(0..2)` mengembalikan `"Bagian"`, `"Unit"`, `"Sub-unit"`. `GetLabel(99)` mengembalikan fallback `"Level 99"`.
    3. Endpoint `GET /Admin/GetLevelLabels` mengembalikan JSON dict dari 3 level seed (200 OK authenticated).
    4. Cache invalidation triggered saat label di-update (verified via service mutation method audit `_cache.Remove` x3).
    5. `GetMaxUsedLevelAsync()` mengembalikan max level dari OrganizationUnits saat ini (sesuai DB lokal).
  - **Risk:** Low (pattern terbukti di repo) | **Effort:** S (1 hari, ~600 LoC delta)
  - **Plans:** 3 plans
    - [ ] 340-01-PLAN.md — Wave 1 Model + DbContext + Migration + Seed integration + D-12 fix (ORG-LABEL-01, ORG-LABEL-07)
    - [ ] 340-02-PLAN.md — Wave 2 Service interface + impl + DI Scoped + Controller endpoint (ORG-LABEL-02, ORG-LABEL-03, ORG-LABEL-07)
    - [ ] 340-03-PLAN.md — Wave 3 xUnit OrgLabelServiceTests + DB_HANDOFF_IT HTML (TEST-01 minimal, ORG-LABEL-01, ORG-LABEL-02)
  - **Files affected:** `Models/OrganizationLevelLabel.cs` (NEW) + `Data/ApplicationDbContext.cs` (DbSet+Fluent) + `Data/SeedData.cs` (seed method + D-12 fix) + `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.cs` (NEW) + `Services/IOrgLabelService.cs` (NEW) + `Services/OrgLabelService.cs` (NEW) + `Program.cs` (DI registration) + `Controllers/OrgLabelController.cs` (NEW) + `HcPortal.Tests/HcPortal.Tests.csproj` (InMemory package) + `HcPortal.Tests/OrgLabelServiceTests.cs` (NEW) + `docs/DB_HANDOFF_IT_2026-06-03.html` (NEW)
  - **Wave structure:** Wave 1 (Plan 01) -> Wave 2 (Plan 02) -> Wave 3 (Plan 03) sequential strict

### Phase 341: Label CRUD Page
  - **Goal:** HC/Admin dapat rename label tier via browser tanpa edit kode atau restart aplikasi (page `/Admin/ManageOrgLevelLabels` Admin+HC CRUD + xUnit + manual UAT).
  - **Requirements:** ORG-LABEL-04, ORG-LABEL-05, ORG-LABEL-06
  - **Depends on:** Phase 340 (consume IOrgLabelService 7 methods)
  - **Plans:** 4 plans (0 complete) -- generated 2026-06-04
    - [x] 341-01-PLAN.md — Wave 1 OrgLabelController 4 actions + DI expansion + View() override + ManageOrgLevelLabelsViewModel POCO (ORG-LABEL-04, 05, 06)
    - [x] 341-02-PLAN.md — Wave 2 Razor view Views/Admin/ManageOrgLevelLabels.cshtml + admin card Views/Admin/Index.cshtml + browser smoke UAT (ORG-LABEL-04)
    - [x] 341-03-PLAN.md — Wave 3 xUnit OrgLabelControllerTests 7 [Fact] + manual UAT Coach 403 + audit log row inspection (ORG-LABEL-04, 05, 06)
  - **Files affected:** Controllers/OrgLabelController.cs (extend +110 LoC) + Models/ViewModels/ManageOrgLevelLabelsViewModel.cs (NEW ~25 LoC) + Views/Admin/ManageOrgLevelLabels.cshtml (NEW ~210 LoC) + Views/Admin/Index.cshtml (+14 LoC card) + HcPortal.Tests/OrgLabelControllerTests.cs (NEW ~250 LoC)
  - **Wave structure:** Wave 1 (Plan 01) -> Wave 2 (Plan 02 — has checkpoint) -> Wave 3 (Plan 03 — has checkpoint) sequential strict
  - **Risk:** Low (semua pattern verified di codebase via PATTERNS.md 5/5 analog match) | **Effort:** S-M (~1 hari, ~600 LoC delta)
### Phase 342: ManageOrganization Page Fixes
  - **Goal:** Page `Admin/ManageOrganization` clean dari 4 bug + 4 inovasi UX — dropdown induk pre-order DFS, validasi nama per-parent, parent nonaktif visible, modal title + badge + legend dynamic via OrgLabelService, cascade impact preview sebelum edit.
  - **Requirements:** ORG-TREE-01, ORG-TREE-02, ORG-TREE-03, ORG-TREE-04, ORG-TREE-05, ORG-TREE-06, ORG-TREE-07, ORG-TREE-08, ORG-TREE-09, ORG-TREE-10
  - **Depends on:** Phase 340 (consume IOrgLabelService GetLabel/GetAll)
  - **Plans:** 4 plans (0 complete) -- generated 2026-06-04
    - [x] 342-01-PLAN.md — Wave 1 backend OrganizationController: dup-name per-parent (2 edit) + PreviewEditCascade read-only count A1 full-accuracy (ORG-TREE-02, 07)
    - [x] 342-02-PLAN.md — Wave 2 frontend orgTree.js + ManageOrganization.cshtml: pre-order DFS + parent nonaktif + escape + level palette + path + cascade modal + legend + title + badge + browser smoke (ORG-TREE-01, 03, 04, 05, 06, 07, 08, 09, 10)
    - [x] 342-03-PLAN.md — Wave 3 xUnit OrganizationControllerTests 6 [Fact] (dup-name per-parent + preview==actual + early-return) + manual UAT 10-skenario (ORG-TREE-02, 07)
  - **Success criteria:**
    1. Modal Tambah Unit dropdown induk menampilkan urutan pre-order DFS (parent → keturunannya → sibling), bukan flat per level (ORG-TREE-01).
    2. Validasi name `Operations` bisa dibuat di 2 Bagian beda; ditolak bila ada di parent yang sama — per-parent unique bukan global (ORG-TREE-02).
    3. Parent nonaktif tetap muncul di dropdown dengan suffix " (nonaktif)" + grey style; user bisa pilih (ORG-TREE-03).
    4. Modal title dynamic ("Tambah Bagian"/"Tambah Unit"/"Tambah Sub-unit") + tree row badge per level + legend di card header, sumber OrgLabelService (ORG-TREE-08/09/10).
    5. Edit nama unit yang punya >0 user terkait → endpoint PreviewEditCascade + modal warning count akurat sebelum aktual submit (ORG-TREE-07).
    6. Bug fix: openDeleteModal pakai data-name + event delegation (ORG-TREE-04); icon color palette extend level 3-5 cycling (ORG-TREE-05); breadcrumb path real-time on select (ORG-TREE-06).
  - **Files affected (estimate):** Views/Admin/ManageOrganization.cshtml + wwwroot/js/orgTree.js + Controllers/OrganizationController.cs (per-parent dup validation + PreviewEditCascade endpoint) — confirm via research
  - **Risk:** Medium (tree DFS ordering + cascade preview query) | **Effort:** M (~1.5 hari, ORG-TREE-01..10)
  - **Canonical refs:** `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` (tree fixes section) + `.planning/milestones/v21.0-ROADMAP.md` §"Phase 342" + `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-TREE-01..10
### Phase 343: Integrasi App-wide
  - **Goal:** Label tier dynamic ter-apply di SEMUA page Portal HC (CMP/CDP/Worker/CoachMapping/ProtonData/Renewal/DocumentAdmin), bukan hanya page ManageOrganization. Setelah label "Bagian" diubah jadi "Direktorat" via page CRUD (Phase 341), label baru muncul app-wide.
  - **Requirements:** ORG-INTEG-01, ORG-INTEG-02
  - **Depends on:** Phase 340 (consume IOrgLabelService GetLabel)
  - **Success criteria:**
    1. Audit grep `"Bagian"|"Unit"` per file di 7 area target selesai dengan keputusan eksplisit per occurrence (ganti display vs skip audit-log/test/literal).
    2. Setelah label "Bagian"→"Direktorat" via page CRUD, label baru muncul di minimal 3 page integrasi (CMP filter + Worker form + CDP assignment).
    3. View Razor pakai `@inject IOrgLabelService` consistent — tidak ada hardcode display string tersisa di 7 area target.
    4. Audit log message body + literal di xUnit test TETAP string statis "Bagian"/"Unit" (konsistensi debug + deterministik).
    5. Controller string yang masuk response/TempData/ViewBag (display) dynamic via service; audit/test literal statis (ORG-INTEG-02).
  - **Files affected (estimate):** Views di 7 area (CMP/CDP/Worker/CoachMapping/ProtonData/Renewal/DocumentAdmin) + controllers terkait (display string) — confirm scope via audit grep
  - **Risk:** Medium (broad surface 7 area, audit-grep per occurrence ganti-vs-skip decision) | **Effort:** M (~1.5 hari, ORG-INTEG-01/02)
  - **Canonical refs:** `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` (integrasi app-wide section) + `.planning/milestones/v21.0-ROADMAP.md` §"Phase 343" + `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-INTEG-01/02
  - **Plans:** 4/4 plans complete
    - [x] 343-01-PLAN.md — Global @inject `_ViewImports.cshtml` (D-01) + SC1 audit deliverable 343-AUDIT.md + ORG-INTEG-02 controller verdict (ORG-INTEG-01, ORG-INTEG-02)
    - [x] 343-02-PLAN.md — REPLACE swaps CMP (2) + CDP (5 view + 2 partial) → @OrgLabels.GetLabel(N) (ORG-INTEG-01) [depends 343-01]
    - [x] 343-03-PLAN.md — REPLACE swaps ProtonData (2) + Admin worker-domain (CoachCoacheeMapping/CreateWorker/EditWorker/ManageWorkers/WorkerDetail/RenewalCertificate/_TrainingRecordsTab = 7) → @OrgLabels.GetLabel(N); 9 files (ORG-INTEG-01) [depends 343-01]
    - [x] 343-04-PLAN.md — REPLACE swaps Admin assessment/upload (EditAssessment/CreateAssessment/CpdpUpload/KkjUpload/CpdpFiles/KkjMatrix = 6) + Account (Profile/Settings = 2); AMBIGUOUS resolved button-text REPLACE / JS-toast SKIP + Lainnya-Tanpa-Bagian REPLACE; 8 files (ORG-INTEG-01) [depends 343-01]
### Phase 344: Test + UAT (planning pending — see v21.0-ROADMAP.md)

---

## 🚧 **v22.0 CMP-06 Residual Fix + CMP/Records + ManageAssessment/Monitoring Audit** — Phases 345-349 🚀 ACTIVE

**Status:** Started 2026-06-04. Requirements `.planning/REQUIREMENTS.md` (CMP06R-01..05 + REC-01..10 + POL-01..10 + MAM-01..13 + MAP-01..23).
**Spec 346/347:** `docs/superpowers/specs/2026-06-04-cmp-records-enhancement-design.md` (audit 7-lens, 37 confirmed). Sequential 345→346→347.
**Spec 348/349:** `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` (audit 6×5-lens, 44 confirmed). Sequential 347→348→349.
**Goal:** Assessment `Status="Completed"` + `IsPassed==null` (essay belum dinilai) tampil **"Menunggu Penilaian"** di SEMUA surface, bukan "Fail/Failed/Tidak Lulus". Tutup 3 surface kelewat Phase 337 CMP-06 + unify label + fix passRate stats. No migration.
**Source:** verifikasi Playwright + code sweep 2026-06-04 (memory `project_cmp06_residual_recordsworkerdetail`).
**Keputusan terkunci:** label "Menunggu Penilaian" (unified); passRate exclude pending.

### Phase 345: assessment-pending-grade-display-fix

- [x] **Phase 345: Assessment pending-grade display correctness**
 (completed 2026-06-04)
  - **REQ:** CMP06R-01, CMP06R-02, CMP06R-03, CMP06R-04, CMP06R-05
  - **Depends on:** Tidak ada (independen v21.0; file beda)
  - **Goal:** 3-way status (`null→"Menunggu Penilaian"`) di RecordsWorkerDetail + UserAssessmentHistory (ctrl+VM+view+stats) + BulkExportPdf, unify label via GetUnifiedRecords + Records.cshtml, regression test.
  - **Success Criteria:**
    1. Sesi Completed+IsPassed-null tampil "Menunggu Penilaian" di `/CMP/RecordsWorkerDetail`, `/Admin/UserAssessmentHistory`, dan PDF `BulkExportPdf` (bukan Fail/Failed/Tidak Lulus).
    2. My Records `/CMP/Records` konsisten label "Menunggu Penilaian" (ganti "Completed"); sesi graded Pass/Fail tetap normal (no regression).
    3. `UserAssessmentHistory` passRate exclude pending dari denominator.
    4. `dotnet build` 0 error (VM `bool`→`bool?` ripple); `dotnet test` hijau + test baru passRate.
    5. Playwright UAT 3 surface PASS (SEED_WORKFLOW snapshot/restore).
  - **Risk:** Low | **Effort:** S-M (~setengah–1 hari, no migration)
  - **Plans:** 4/4 plans complete
    - [x] 345-01-PLAN.md — CMP06R-01 + CMP06R-04 + MINOR-A: RecordsWorkerDetail 3-way + GetUnifiedRecords label + Records.cshtml switch + Excel ExportRecords
    - [x] 345-02-PLAN.md — CMP06R-02: UserAssessmentHistory VM bool? + ctrl drop ?? false + view 3-way + stats exclude-pending + grup PassedCount
    - [x] 345-03-PLAN.md — CMP06R-03: GeneratePerPesertaPdf 3-way "Menunggu Penilaian" + warna netral
    - [x] 345-04-PLAN.md — CMP06R-05: xUnit + Playwright UAT 3 surface
  - **Files affected:** `Views/CMP/RecordsWorkerDetail.cshtml` + `Views/CMP/Records.cshtml` + `Services/WorkerDataService.cs` + `Controllers/AssessmentAdminController.cs` (4737/4744-4745 + 4620-4621 + 2759-2821) + `Controllers/CMPController.cs` (694) + `Models/CDPDashboardViewModel.cs` (AssessmentReportItem.IsPassed bool-to-bool?, C-1) + `Models/ReportsDashboardViewModel.cs` (UserAssessmentHistoryViewModel +GradedCount/PendingCount) + `Views/Admin/UserAssessmentHistory.cshtml` + `HcPortal.Tests/` (NEW) + `tests/e2e/` (NEW)
  - **Wave structure:** 345-01 ∥ 345-02 ∥ 345-03 (region independen) → 345-04 (test, depends all)

### Phase 346: cmp-records-detail-search-logic

- [x] **Phase 346: CMP/Records Detail, Search & Logic Fix**
 (completed 2026-06-04)
  - **REQ:** REC-01, REC-02, REC-03, REC-04, REC-05, REC-06, REC-07, REC-08, REC-09 (REC-10 DROP)
  - **Depends on:** Phase 345 (REC-07 butuh label "Menunggu Penilaian"; REC-01/02/03/05 sentuh `Records.cshtml`+`RecordsWorkerDetail.cshtml` baris berdekatan — sequential)
  - **Goal:** Pekerja & atasan bisa lihat detail assessment (hasil) + training (modal), Worker Detail buka hasil assessment, Team View search adaptif (Nama/Training/Keduanya), assessment PendingGrading tak hilang.
  - **Success Criteria:**
    1. My Records `/CMP/Records` punya kolom "Aksi": Assessment→tombol `Lihat Hasil`→`/CMP/Results`, Training→tombol `Detail`→modal (Penyelenggara/Kota/tgl/No.Sertifikat/Kategori/SubKategori/Status/ValidUntil + PDF); row tetap clickable.
    2. Worker Detail `/CMP/RecordsWorkerDetail` row Assessment punya tombol `Lihat Hasil`→`/CMP/Results`; modal training tambah Kategori/SubKategori.
    3. 🔐 `Results`+`Certificate`+`CertificatePdf` authz: owner ∥ L≤3 full ∥ L4 section-scoped (`assessment.User.Section==user.Section`, guard Section non-null). L3/L4 atasan buka hasil tim PASS; L4 beda section + L5/L6 non-owner Forbid.
    4. Team View search box + selektor scope (Nama/Training/Keduanya, server-side); training-search via join `TrainingRecords.Judul`; export links ikut ke-filter.
    5. Assessment esai PendingGrading tampil di My Records + export team dengan label "Menunggu Penilaian" (WHERE pakai `AssessmentConstants.AssessmentStatus.PendingGrading`).
    6. Date range `dateTo<dateFrom`→warning; badge "Assessment" diperjelas (header/tooltip, BUKAN rename field).
    7. `dotnet build` 0 error + `dotnet test` hijau (authz matrix + search scope + PendingGrading tests) + Playwright UAT PASS.
  - **Risk:** Medium (REC-04 authz security-sensitive; REC-06 service query) | **Effort:** M-L (no migration)
  - **Plans:** 6/6 plans complete
    - [x] 346-01-PLAN.md — REC-01/02: My Records kolom Aksi + tombol Lihat Hasil + modal training (11 field + PDF) + fix colspan
    - [x] 346-02-PLAN.md — REC-03/05: Worker Detail tombol Lihat Hasil (un-gated sertifikat) + modal Kategori/SubKategori
    - [x] 346-03-PLAN.md — REC-04 (security): authz 3 action via static IsResultsAuthorized (L<=3 full, L4 section-scoped, guard null) + threat model
    - [x] 346-04-PLAN.md — REC-06: Team search adaptif (Nama/Training/Keduanya) service+controller+UI/JS + export filter
    - [x] 346-05-PLAN.md — REC-07/08/09: include PendingGrading (konstanta) + date-range warning + relabel Assessment Lulus
    - [x] 346-06-PLAN.md — Tests + UAT: xUnit authz matrix 8-case + searchScope + include-pending; Playwright semua surface (+ Tab3 History)
  - **Wave structure:** Wave1 [346-01 || 346-02 || 346-03] -> Wave2 [346-04] -> Wave3 [346-05] -> Wave4 [346-06] (file-cluster serialize: CMPController 03->04; WorkerDataService+RecordsTeam 04->05)
  - **Files affected:** `Views/CMP/Records.cshtml` + `Views/CMP/RecordsWorkerDetail.cshtml` + `Views/CMP/RecordsTeam.cshtml` + `Controllers/CMPController.cs` (Results 2169, Certificate 1815, CertificatePdf 1926, RecordsTeamPartial 753, Export* 652/704) + `Services/WorkerDataService.cs` (GetUnifiedRecords 28, GetAllWorkersHistory 92, GetWorkersInSection 242) + `Models/WorkerTrainingStatus.cs` + `HcPortal.Tests/` (NEW) + `tests/e2e/` (NEW)
  - **Pitfalls (spec §):** colspan My Records 6→7 (L227+JS L381); konstanta PendingGrading (bukan literal "PendingGrading"); `.Include(a=>a.User)` di Certificate+CertificatePdf; sequential 345→346→347.

### Phase 347: cmp-records-i18n-a11y-polish

- [x] **Phase 347: CMP/Records i18n + a11y Polish** (completed 2026-06-04)
  - **REQ:** POL-01, POL-02, POL-03, POL-04, POL-05, POL-06, POL-07, POL-08, POL-09, POL-10
  - **Depends on:** Phase 346 (sequential — sentuh `Records.cshtml`+`RecordsWorkerDetail.cshtml`); koordinasi POL-01 dgn Phase 345 (case null jangan ditimpa)
  - **Goal:** Konsistensi Bahasa Indonesia + aksesibilitas + DRY pada halaman Records (15 finding LOW).
  - **Success Criteria:**
    1. Badge `Passed/Failed`→`Lulus/Tidak Lulus` (case true/false; null tetap "Menunggu Penilaian" dari Phase 345).
    2. Header `Score`→`Nilai`; `Position`→`Jabatan`; `Section`→`@OrgLabels.GetLabel(0)`; `All Categories/Sub/Types`→`Semua ...`; subtitle Inggris→Indonesia.
    3. a11y: modal `aria-labelledby`+`role=dialog`+btn-close `aria-label`; label `for=` semua filter (3 view); pagination `aria-current`.
    4. DRY: `<style>` duplikat (.stat-card/.sticky-header/@keyframes) → 1 file CSS; mobile grid filter responsif; `type="button"` reset.
    5. `dotnet build` 0 error + no visual regression (Playwright spot-check).
  - **Risk:** Low | **Effort:** S-M (no migration)
  - **Plans:** 4/4 plans complete
    - [x] 347-01-PLAN.md — i18n teks: badge Lulus/Tidak Lulus (null intact) + Nilai/Jabatan/Section(OrgLabels) + Semua Kategori/Sub/Tipe + subtitle ID + label tombol [W1]
    - [x] 347-02-PLAN.md — a11y: modal aria(role/labelledby/Tutup) + label for= semua filter + My Records visible label + grid responsif + type=button reset + pagination aria-current [W2]
    - [x] 347-03-PLAN.md — POL-08 DRY: ekstrak <style> verbatim -> wwwroot/css/records.css + _Layout RenderSection Styles + @section 2 full-page + RecordsTeam partial style-removal-only [W3]
    - [x] 347-04-PLAN.md — verifikasi: dotnet build 0-error + grep sweep 10 POL + Playwright spot-check no-visual-regression (no xUnit) [W4]
  - **Wave structure:** W1 [347-01] -> W2 [347-02] -> W3 [347-03] -> W4 [347-04] (serial penuh: ketiga view di-edit bersama di tiap plan, hindari konflik write file)
  - **Files affected:** `Views/CMP/Records.cshtml` + `Views/CMP/RecordsWorkerDetail.cshtml` + `Views/CMP/RecordsTeam.cshtml` + `Views/CMP/_RecordsTeamBody.cshtml` + `wwwroot/css/records.css` (NEW)

### Phase 348: manageassessment-monitoring-med-fix

- [x] **Phase 348: ManageAssessment + Monitoring MED Correctness Fix** (completed 2026-06-05)
  - **REQ:** MAM-01, MAM-02, MAM-03, MAM-04, MAM-05, MAM-06, MAM-07, MAM-08, MAM-09, MAM-10, MAM-11, MAM-12, MAM-13
  - **Depends on:** Phase 347 (sequential v22.0; MAM-04/05 pakai konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` + label dari 345)
  - **Goal:** Pre-Post group konsisten (token/export/badge), essay pending tak salah-label "Completed", Tab2 empty-state+pagination+filter benar, status-badge match filter, Monitoring filter data-driven (13 finding MED).
  - **Success Criteria:**
    1. Pre-Post: RegenerateToken + link Monitoring/Export by LinkedGroupId; badge "X belum dinilai" muncul (MAM-01/02/03).
    2. Essay PendingGrading tak salah "Completed" di Monitoring Detail (server + live SignalR); CompletedCount/passRate benar (MAM-04/05).
    3. Tab2: empty-state hidup (skip full-roster), pagination/param benar, delete preserve filter, Status filter jujur (MAM-06/07/08/09).
    4. Badge status Tab1 = GroupStatus (match filter); dropdown Kategori Monitoring data-driven (buang "Proton"); tooltip Closed jujur; Reshuffle selector scoped (MAM-10/11/12/13).
    5. `dotnet build` 0 error + xUnit + Playwright UAT per surface.
  - **Risk:** Medium (logic shared grading/token; initial-load behavior) | **Effort:** L (no migration)
  - **Plans:** 5/5 plans complete
    - [x] 348-01-PLAN.md — Tema A Pre-Post: RegenerateToken LinkedGroupId + Export/PDF both-half + MenungguPenilaianCount (MAM-01/02/03)
    - [x] 348-02-PLAN.md — Tema B essay PendingGrading (ISOLASI): status derivation Detail + SignalR workerSubmitted reload + handler view (MAM-04/05)
    - [x] 348-03-PLAN.md — Tema C Tab2 struktural: isInitialState + pagination + delete hx-post re-swap + relabel Status Training (MAM-06/07/08/09)
    - [x] 348-04-PLAN.md — Tema D/E/F: badge GroupStatus + dropdown Kategori data-driven + tooltip jujur + reshuffle selector (MAM-10/11/12/13)
    - [x] 348-05-PLAN.md — Verify gate: dotnet build + xUnit (PaginationHelper/status/initialState) + Playwright UAT 5 SC + checkpoint human-verify
  - **Files affected:** `AssessmentAdminController.cs` + `CMPController.cs` + `TrainingAdminController.cs` + `_AssessmentGroupsTab.cshtml` + `_TrainingRecordsTab.cshtml` + `AssessmentMonitoring.cshtml` + `AssessmentMonitoringDetail.cshtml` + `HcPortal.Tests`
  - **Dedup:** M4 (Tab3 History PendingGrading) dicakup REC-07/346 — tak diduplikat; tambah Tab3 History ke UAT 346.

### Phase 349: manageassessment-monitoring-low-polish

- [x] **Phase 349: ManageAssessment + Monitoring LOW Polish** (completed 2026-06-05)
  - **REQ:** MAP-01..23 (29 LOW; D-02 semua masuk)
  - **Depends on:** Phase 348 (sequential — file sama: `_AssessmentGroupsTab`/`AssessmentMonitoring*`/`ManageAssessment.cshtml`)
  - **Goal:** i18n Monitoring + a11y (aria/chevron) + empty-state/feedback + code-hygiene (dead param, magic-number, dead var).
  - **Success Criteria:**
    1. i18n Monitoring Detail Indonesia penuh; "NIP" konsisten History.
    2. a11y: chevron+aria-label toggle; Tab3 drill-down tanpa ARIA nested.
    3. Empty-state/feedback: Tab1 filter-aware, Tab3 "no results" message, skeleton match.
    4. Display nits: Abandoned card, progress bar bisa 100% (exclude Cancelled), "real-time" subtitle, kategori dobel.
    5. Code-hygiene: magic-number `20` → ViewBag, param mati drop. `dotnet build` 0 error + no visual regression.
  - **Risk:** Low | **Effort:** M (no migration)
  - **Plans:** 5/5 plans complete
    - [x] 349-01-PLAN.md — Tab3 History: i18n NIP + drop ARIA nested + 0-match/counter + skeleton (MAP-02/04/07/08/09/20)
    - [x] 349-02-PLAN.md — Tab1/Tab2: chevron+aria + empty-state filter-aware/Reset Semua Filter + tri-state/CompletionDisplayText + paging.Take (MAP-03/05/06/18/19/21)
    - [x] 349-03-PLAN.md — Monitoring list: buang real-time/kategori-dobel + Pre-Post Regenerate Token + TotalCount exclude Cancelled + Status jujur + search Category (MAP-13/14/15/16/17/23)
    - [x] 349-04-PLAN.md — Monitoring Detail: i18n ID + 7-kartu summary (Abandoned+Pending) + InProgressCount/drop dead var + Akhiri Semua conditional (MAP-01/10/11/12)
    - [x] 349-05-PLAN.md — MAP-22 drop param mati History + Nyquist test (MAP-13/23) + PHASE GATE (full suite + Playwright UAT 5 SC + browser-verify card-sum/progress-100%) (MAP-13/22/23)
  - **Wave structure:** W1 [349-01] -> W2 [349-02] -> W3 [349-03] -> W4 [349-04] -> W5 [349-05] (serial penuh — overlap AssessmentAdminController.cs di W2/W3/W4/W5 + file partisi sama; D-B sequential strict)
  - **Files affected:** sama Phase 348 + `ManageAssessment.cshtml` + `_HistoryTab.cshtml`

**Active mapped: 60/60 ✓ (CMP06R-01..05 + REC-01..09 + POL-01..10 + MAM-01..13 + MAP-01..23) — Orphans: 0 — Duplicates: 0 — REC-10 dropped — M4 dedup→REC-07/346 — No migration**


---

<details>
<summary>✅ v23.0 CMP/Records Search & Filter Consistency Audit (Phases 350-351) — SHIPPED LOCAL 2026-06-06, 7/7 REQ — <a href="milestones/v23.0-ROADMAP.md">archive</a> · <a href="milestones/v23.0-MILESTONE-AUDIT.md">audit</a></summary>

## v23.0 CMP/Records Search & Filter Consistency Audit — Phases 350-351

**Status:** SHIPPED LOCAL 2026-06-06 (audit-driven, 7/7 REQ SF-01..07 passed). Detail penuh: archive.
**Spec/audit:** `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` (3 surface audited, 7 confirmed findings, code-verified file:line).
**Origin:** Backlog Phase 999.2 (bug UAT 2026-06-05: search "ojt v14.2" → 0 worker di Team View "Keduanya"). 999.1 Realtime SignalR DROPPED.
**Goal:** Konsistensi + kelengkapan perilaku search/filter di seluruh permukaan CMP/Records (My Records + Team View + Worker Detail) — user tak lagi gagal menemukan data yang seharusnya muncul.
**Keputusan terkunci:** Preserve REC-06 D-07 (Phase 346) — filter di level worker (post-load), badge count per-worker tetap utuh; **no migration** (search/filter predicate + view + export saja).

### Phases

- [x] **Phase 350: Team View Server-Side Search Scope + Export Parity** — Cari di Team View ikut cakup judul Assessment (fix 999.2) + dropdown Lingkup jujur + export WYSIWYG identik tabel on-screen (completed 2026-06-05)
- [x] **Phase 351: Worker Detail + Cross-Surface Filter Consistency** — 0-match feedback + counter di Worker Detail + filter Kategori match record aktual + paritas My Records ↔ Worker Detail + back-nav preserve param (completed 2026-06-06)

### Phase Details

### Phase 350: Team View Server-Side Search Scope + Export Parity

**Goal:** HC/admin dapat menemukan worker pemilik **assessment** (bukan hanya Training) saat search di Team View CMP/Records, dengan dropdown "Lingkup" + placeholder yang jujur mencerminkan apa yang dicari, dan tombol Export menghasilkan data identik dengan tabel on-screen. **Preserve REC-06 D-07:** predikat baru memfilter *worker mana yang muncul* di level worker (post-load), badge/count per-worker tetap utuh — tidak menyentuh agregasi per-record.
**Depends on:** Tidak ada (fase pertama v23.0; foundation predicate `GetWorkersInSection`)
**Requirements:** SF-01 (HIGH), SF-02 (MED), SF-06 (MED)
**Success Criteria** (what must be TRUE):
  1. User cari "ojt v14.2" (judul assessment) di Team View dengan Lingkup "Keduanya" → worker pemilik assessment itu **tampil** (sebelumnya 0 worker). Worker yang hanya cocok via Training tetap muncul (tidak ter-regresi).
  2. Dropdown "Lingkup" punya opsi yang eksplisit mencakup pencarian Assessment (mis. opsi "Assessment" baru ATAU relabel "Keduanya" = Nama/NIP + Training + Assessment), dan placeholder/label search jujur (tidak lagi "...atau judul training" yang menyesatkan).
  3. Tombol Export Team View (Assessment + Training) menghasilkan baris **identik** dengan tabel on-screen (WYSIWYG) — search/filter/scope yang sama diterapkan; Export Assessment **tidak kosong** saat user search judul assessment (konsekuensi SF-01).
  4. Badge count Assessment Lulus / Training per worker **tidak berubah** akibat search (REC-06 D-07 invariant) — search hanya menyaring worker yang muncul, bukan mengubah angka badge.
  5. `dotnet build` 0 error + `dotnet test` hijau termasuk test predikat baru `GetWorkersInSection` (assessment-title match) yang sebelumnya absen di `WorkerDataServiceSearchTests.cs`.
**Plans:** 3/3 plans complete
  - [x] 350-01-PLAN.md — Wave 0 test scaffold: +4 xUnit [Fact] (RED) + cmp350-seed.sql + cmp-records-350.spec.ts (SF-01, SF-06)
  - [x] 350-02-PLAN.md — SF-01 predikat assessment-title (post-load, D-07 utuh) + SF-02 micro-copy jujur "Judul Kegiatan" (value tetap) (SF-01, SF-02)
  - [x] 350-03-PLAN.md — SF-06 export Category symmetry (project a.Category + controller narrow + drop archived) + phase gate UAT (SF-06)
**UI hint:** yes

### Phase 351: Worker Detail + Cross-Surface Filter Consistency

**Goal:** Pekerja & atasan mendapat feedback jelas saat filter Worker Detail tidak menghasilkan baris (0-match), filter Kategori benar-benar mencocokkan kategori record aktual (bukan exact-equals ke master yang bisa miss record legacy/free-text + opsi mati), serta pengalaman filter konsisten antara melihat data sendiri (My Records) vs data pekerja lain (Worker Detail), dan tombol kembali ke Team View mempertahankan seluruh konteks filter.
**Depends on:** Phase 350 (sequential — SF-04 menyentuh `GetUnifiedRecords` di `WorkerDataService.cs` yang juga di-touch Phase 350 `GetWorkersInSection`; hindari konflik write file)
**Requirements:** SF-03 (MED), SF-04 (MED), SF-05 (LOW), SF-07 (LOW)
**Success Criteria** (what must be TRUE):
  1. Saat client-filter Worker Detail menyembunyikan semua baris (0 match), tampil pesan "Tidak ada hasil untuk filter ini." (`aria-live="polite"`) + counter "Menampilkan X dari Y" yang ikut filter aktif — bukan tabel kosong tanpa keterangan (reuse pola My Records / v22 MAP-07/08).
  2. Filter Kategori di Worker Detail mencocokkan kategori **record aktual** (assessment + training rows) — record free-text/legacy tetap terfilter benar, dan opsi dropdown tidak menyertakan kategori "mati" (master yang tak punya record).
  3. Field search/filter di My Records dan Worker Detail konsisten — tidak ada gap "satu surface bisa filter X, satunya tidak" tanpa alasan; user yang melihat data dirinya sendiri tidak lebih miskin alat filter dibanding saat melihat record orang lain.
  4. Tombol "Back to Team View" di Worker Detail kembali ke state Team View yang sama — preserve param filter (`subCategory`, `dateFrom`, `dateTo`, `searchScope`) selain `section`/`unit`/`category`/`statusFilter`/`search` — bukan hanya sebagian.
  5. `dotnet build` 0 error + `dotnet test` hijau (termasuk test pencocokan Kategori actual-records SF-04) + Playwright UAT per surface PASS (My Records + Worker Detail + back-nav round-trip).
**Plans:** 4/4 plans complete
  - [x] 351-01-PLAN.md — Wave 0 test infra: cmp351-seed.sql (off-master Kategori) + cmp-records-351.spec.ts (SF-03/04/05/07) + SEED_JOURNAL
  - [x] 351-02-PLAN.md — Backend: BuildActualCategories helper + ViewBag.ActualCategoriesJson di RecordsWorkerDetail (SF-04) + Records (SF-05) + xUnit; authz preserve
  - [x] 351-03-PLAN.md — Worker Detail view: counter + filtered-empty-state (SF-03) + Kategori actual-source (SF-04)
  - [x] 351-04-PLAN.md — My Records view: Kategori+Tipe parity + data-category (SF-05) + hash-to-tab activator back-nav (SF-07)
**UI hint:** yes

**Active mapped: 7/7 ✓ (SF-01..07) — Orphans: 0 — Duplicates: 0 — No migration — Preserves REC-06 D-07**

### Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 350. Team View Search Scope + Export Parity | 3/3 | Complete    | 2026-06-05 |
| 351. Worker Detail + Cross-Surface Consistency | 4/4 | Complete   | 2026-06-06 |

### Coverage Validation

| REQ | Sev | Phase | Surface | Status |
|-----|-----|-------|---------|--------|
| SF-01 | HIGH | 350 | Team View (search predicate) | Pending |
| SF-02 | MED | 350 | Team View (Lingkup dropdown + placeholder) | Pending |
| SF-06 | MED | 350 | Team View Export (parity) | Pending |
| SF-03 | MED | 351 | Worker Detail (0-match + counter) | Pending |
| SF-04 | MED | 351 | Worker Detail (Kategori actual-match) | Pending |
| SF-05 | LOW | 351 | My Records ↔ Worker Detail (parity) | Pending |
| SF-07 | LOW | 351 | Worker Detail → Team View (back-nav state) | Pending |

**Active mapped: 7/7 ✓ — Orphans: 0 — Duplicates: 0**

</details>

---

*Roadmap updated: 2026-06-06 (v23.0 CLOSED — 350+351 shipped local + audited 7/7 SF-01..07, integration 7/7 WIRED; collapsed to archive. NOT PUSHED bundle v19-v23.)*

*Prev: 2026-06-05 (v23.0 added — CMP/Records Search & Filter Consistency Audit; 2 phase 350-351 dari audit 3-surface 7 confirmed [1 HIGH/4 MED/2 LOW]; 350 = Team View server-side search scope + export parity SF-01/02/06 [fix 999.2, preserve REC-06 D-07], 351 = Worker Detail + cross-surface filter consistency SF-03/04/05/07; sequential strict [file-overlap WorkerDataService.cs]; tests folded per phase [reuse v22 xUnit predicate-mirror + Playwright UAT]; no migration; spec 2026-06-05-cmp-records-search-filter-audit.md; backlog Phase 999.2 promoted → SF-01/02/06; 999.1 SignalR dropped).*

---

*Roadmap updated: 2026-06-04 (Phase 348+349 added — ManageAssessment+Monitoring audit 6×5-lens 44 confirmed [0 HIGH/15 MED/29 LOW]; 348 = 13 MED correctness depends 347, 349 = 29 LOW polish depends 348; sequential strict; M4 dedup→REC-07/346; no migration; spec 2026-06-04-manageassessment-monitoring-audit-design.md).*
*Prev: 2026-06-04 (Phase 346+347 added — CMP/Records Enhancement dari audit 7-lens 37 confirmed; 346 fitur+logic REC-01..09 [REC-10 drop] depends 345, 347 i18n+a11y polish POL-01..10 depends 346; sequential strict; no migration; spec 2026-06-04-cmp-records-enhancement-design.md @ 22759cad).*
*Prev: 2026-06-04 (v22.0 added — Phase 345 CMP-06 residual fix, 5 REQ CMP06R-01..05, 4 plan, no migration; sumber Playwright+sweep verify 3 surface kelewat Phase 337).*
*Prev: 2026-06-02 (Phase 340 plans generated — 3 plan 3 wave sequential strict, 7 task total; Foundation v21.0 P1 milestone start; depends_on=[]; ORG-LABEL-01/02/03/07 mapped; D-12 SeedData convention fix included).*
*Prev: 2026-06-02 (v20.0 ARCHIVED — milestone close, 39/39 REQ satisfied, 4 phase + 10 plan + 56 commit + 14,768/-323 LOC. Archive: milestones/v20.0-*.md. Bundle ~155 commit lokal v19.0+v20.0 pending push origin/main + IT promo Dev).*
*Prev: 2026-06-02 (Phase 339 added — gap closure dari `/gsd-audit-milestone v20.0` 2026-06-02; 3 partial REQ CIL-06+REST-04+REST-06 → orphan UI link + Title regex validator; 1 plan 1 wave 3 task, effort S half day; depends Phase 338).*
*Prev: 2026-05-30 (v20.0 milestone + Phase 336-338 added — 3 PR bundle Opsi 2 sequential strict; 39 REQ CMP-01..26 + CIL-01..06 + REST-01..07; total estimate ~2.5 minggu; locked decision Approach C CMP Records).*
*Prev: 2026-05-28 (Phase 331-335 added — 5 HIGH proposal Phase 328 §9 #1+#3+#4+#5+#6 spawned per user batch-create. Phase 331-334 mechanical atomicity, Phase 335 complex worker lifecycle).*
*Prev: 2026-05-28 (Phase 330 plan generated — 330-01-PLAN.md, 3 task single wave, ~75 LoC delta Controllers/AssessmentAdminController.cs + Controllers/OrganizationController.cs + Services/NotificationService.cs).*
*Prev: 2026-05-28 (Phase 329 plan generated — 329-01-PLAN.md, 4 task single wave, ~60 LoC delta Controllers/AssessmentAdminController.cs; verbatim D-02 pattern Phase 325 P05).*
*Prev: 2026-05-28 (Phase 328 RESEARCH.md SHIPPED LOCAL — commit `41f1eef2`, 14 endpoint mutator + 5 preview, 8 HIGH + 5 MED + 0 LOW; 7 next-phase fix proposals di Section 9 PROPOSAL ONLY).*
*Prev: 2026-05-27 (Phase 328 promoted dari backlog → v19.0 active, depends on Phase 327; Coverage table updated P01/P02/P05 = SHIPPED).*
*Prev: 2026-05-27 (Phase 328 plan generated — 328-01-PLAN.md, 10 task audit-only single wave).*
*Prev: 2026-05-27 (Phase 328 added — Cascade Audit Sweep Delete* endpoints, audit-only, spec commit 02f620be).*
*Prev: 2026-05-27 (backlog Phase 999.1 Realtime Assessment SignalR added).*
*Prev: 2026-05-26 (v19.0 planned — 6 bug Portal HC actionable dari sertifikat-ecosystem audit, 3 phase sequential, IT promo batch akhir).*
