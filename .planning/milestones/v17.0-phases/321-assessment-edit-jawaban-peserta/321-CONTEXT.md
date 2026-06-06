# Phase 321: Assessment Edit Jawaban Peserta - Context

**Gathered:** 2026-05-21
**Status:** Ready for planning
**Milestone:** v17.0 Assessment Admin Power Tools

<domain>
## Phase Boundary

Bangun halaman admin/HC `/AssessmentAdmin/EditPesertaAnswers/{id}` untuk edit jawaban MC/MA peserta `Status == Completed` dengan:

1. **Model + Migration baru** — `AssessmentEditLog` (audit granular per-question) + migration `AddAssessmentEditLogs` (index `IX_AssessmentEditLogs_SessionId_EditedAt`).
2. **Service layer** — Refactor `GradingService` extract `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` no-side-effect + add `RegradeAfterEditAsync(session)` (DELETE existing `SessionElemenTeknisScores` + recompute + ExecuteUpdateAsync status guard).
3. **Controller `AssessmentAdminController`** — Add `GET EditPesertaAnswers`, `POST SubmitEditAnswers` (transaction scope edit+audit+regrade+cascade), `GET PreviewEditScore` (dry-run flip detection).
4. **View + JS** — `Views/AssessmentAdmin/EditPesertaAnswers.cshtml` dedicated page + `wwwroot/js/edit-peserta-answers.js` (dirty state + reason validation + flip modal AJAX).
5. **UI Dropdown ⋮ hybrid** — Refactor `AssessmentMonitoringDetail.cshtml` action column: `View Results` + `Activity Log` 🕐 inline, sisanya (Edit Jawaban, Reset, Akhiri Ujian, Reshuffle) pindah ke dropdown ⋮ dengan ARIA + Bootstrap `dropdown-menu-end` + auto-flip mobile. Item Edit Jawaban conditional render via `IsEditable(session)`.
6. **SignalR signal baru** `workerAnswerEdited` ke group `monitor-{batchKey}` + frontend handler row update + toast.
7. **Activity Log tab baru "Edit History"** — Lazy-load partial filtered by SessionId, sort EditedAt DESC.

**Cascade Pass↔Fail flip:**
- Pass→Fail: cabut `NomorSertifikat` + `ValidUntil` + update `TrainingRecord.Status = "Failed"`.
- Fail→Pass: generate `NomorSertifikat` baru (retry 3x via `CertNumberHelper`, hanya kalau `GenerateCertificate && AssessmentType != "PreTest"`) + upsert `TrainingRecord.Status = "Passed"`.

**Out of scope (other phase / deferred):**
- Edit Essay (Phase 321 hanya MC/MA — Essay grading di flow existing).
- Bulk edit (1 sesi 1 admin edit per save).
- Edit untuk session `Status != Completed`, `IsManualEntry == true`, atau Assessment Proton Tahun 3 (gating eksplisit via `IsEditable`).
- Manual override sertifikat/TrainingRecord langsung tanpa edit jawaban (DB direct edit IT job).

</domain>

<decisions>
## Implementation Decisions

### PLAN Sub-Numbering Strategy
- **D-01:** Pecah 13 task RESEARCH jadi **4 PLAN file atomic per layer**:
  - `321-01-PLAN.md` **model+foundation** — Task 1 (Model `AssessmentEditLog` + DbSet + migration apply/rollback) + Task 2 (Helper `IsEditable`)
  - `321-02-PLAN.md` **service** — Task 3 (Refactor `ComputeScoreAndETInternalAsync`) + Task 4 (`RegradeAfterEditAsync`)
  - `321-03-PLAN.md` **controller+view+frontend+dropdown** — Task 5 (GET) + Task 6 (View) + Task 7 (JS dirty state) + Task 8 (POST + transaction) + Task 9 (PreviewEditScore dry-run) + Task 10 (Dropdown ⋮) + Task 11 (SignalR frontend handler)
  - `321-04-PLAN.md` **activity-log+uat** — Task 12 (Activity Log tab "Edit History") + Task 13 (Manual UAT full checklist + tag + handoff)
- **D-02:** **Sequential strict** — `01 → 02 → 03 → 04` wajib urut, no paralelisasi antar PLAN. Rationale: PLAN 02 service signature dipakai PLAN 03 controller; PLAN 03 multi-file edit di `AssessmentAdminController.cs` (sama file Phase 320) — sequential hindari race.

### Branch Strategy
- **D-03:** **Feature branch + merge** — `feature/phase-321-edit-jawaban` (beda dari Phase 320 yang langsung main). Rationale: migration baru = risk lebih tinggi, isolasi mudah revert seluruh phase kalau gagal UAT. Merge ke main setelah Task 13 UAT pass + tag `v17.0-p321-complete`. Squash atau merge-commit ditentukan saat execute (default merge-commit untuk preserve task granularity).

### Testing Strategy
- **D-04:** **Hybrid Playwright + manual UAT** — split by automation feasibility.
  - **Playwright (4 test, automated):**
    1. **Auth gate (Admin/HC/Worker)** — Login 3 role → GET `/AssessmentAdmin/EditPesertaAnswers/{id}` → assert 200 (Admin/HC) vs 403/redirect-login (Worker). REQ EDIT-01 permission verify.
    2. **Happy-path edit save** — Login Admin → GET edit page → ubah 1 jawaban MC → isi reason preset `SoalSalah` → POST submit → assert redirect monitoring + DB `AssessmentEditLog` count++ + score recompute. End-to-end edit flow.
    3. **Concurrency stale stage** — 2 browser context (Admin A + Admin B) buka edit page sama, A submit dulu → B coba submit → assert B kena TempData error "Sesi sudah diubah admin lain." REQ EDIT-07 verify.
    4. **Flip preview dry-run AJAX** — POST `PreviewEditScore` dengan draft answers → assert JSON response shape `{oldScore, newScore, oldIsPassed, newIsPassed, hasCert, willGenerateCert}`. REQ EDIT-10 contract test.
  - **Manual UAT (4 area, Playwright tidak praktis):**
    1. **DB verify cascade flip** — Pass→Fail check `NomorSertifikat NULL` + `ValidUntil NULL` + `TrainingRecord.Status = "Failed"`. Fail→Pass check `NomorSertifikat` baru generated + `TrainingRecord` upsert `Status = "Passed"`. Pakai `sqlcmd` query langsung ke DB lokal. REQ EDIT-04.
    2. **SignalR cross-tab live update** — Buka 2 tab `AssessmentMonitoringDetail`, edit di tab 1 → tab 2 row score+result cell auto-update + toast verbose muncul tanpa refresh. REQ EDIT-09.
    3. **Activity Log Edit History tab** — Buka modal Activity Log → klik tab "Edit History" → verify list timeline entries (timestamp, soal, old→new, actor, reason) sesuai edit yang baru dilakukan. REQ EDIT-11.
    4. **Migration rollback lokal** — `dotnet ef database update {PrevMigration}` → verify table `AssessmentEditLogs` drop → `dotnet ef database update` lagi → verify re-create. WAJIB lulus sebelum commit migration file. REQ EDIT-13.

### UX Copy (Bahasa Indonesia)
- **D-05:** **Reason preset labels verbose user-friendly** (code value tetap PascalCase):
  - `SoalSalah` → "Soal salah / typo"
  - `KunciSalah` → "Kunci jawaban salah"
  - `BugSistem` → "Bug sistem / glitch"
  - `PermintaanPeserta` → "Permintaan koreksi peserta"
  - `Lainnya` → "Lainnya (jelaskan)"
- **D-06:** **Flip modal eksplisit konsekuensi** (Pass↔Fail confirmation):
  - Pass→Fail: "Perubahan ini akan **menggagalkan peserta**. NomorSertifikat akan dicabut dan TrainingRecord di-set Failed. Lanjutkan?"
  - Fail→Pass: "Perubahan ini akan **meluluskan peserta**. NomorSertifikat baru akan di-generate (kalau eligible: GenerateCertificate && bukan PreTest). Lanjutkan?"
  - Tombol: `[Batal]` + `[Lanjutkan, simpan perubahan]` (no checkbox layer).
- **D-07:** **Toast SignalR verbose audit-style** — Template: `"{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}, {oldResult}→{newResult}"` (contoh: `"Admin Rino edit jawaban Budi Santoso: Score 65→75, Fail→Pass"`). Bootstrap toast top-right, auto-dismiss 8 detik, klik untuk persist.

### Migration + Handoff
- **D-08:** **IT notify preemptif + final**:
  - **Preemptif** (saat PLAN 01 selesai, sebelum push remote): heads-up channel IT "Phase 321 ada migration baru `AddAssessmentEditLogs`, target deploy ~X hari, siapin slot maintenance DB Dev."
  - **Final** (setelah tag `v17.0-p321-complete` + push remote): notify commit hash + tag + flag MIGRATION ADA + post-deploy verify checklist (akses `/AssessmentAdmin/EditPesertaAnswers/{id}` Admin role).
- **D-09:** **Phase order** — Phase 320 commits + tag `v17.0-p320-complete` sudah di `origin/main` (verified `git log origin/main` + `git ls-remote --tags`). Phase 321 jalan paralel tanpa blocker IT promo Dev 320 (decoupled). Push 321 setelah selesai → IT promo 320 + 321 bareng atau berurutan terserah jadwal IT.
- **D-10:** **Commit cadence 1-task-1-commit** (pola Phase 320) — 13 task = 13 commit, message format `feat|refactor|perf|chore(v17.0-p321): ...`. Migration apply/rollback test ikut commit Task 1 (single atomic). Pre-commit checklist `docs/DEV_WORKFLOW.md §5` per commit.

### Carrying Forward (Prior Phase Patterns)
- **D-11:** Project test infra = `dotnet build` + browser UAT + Playwright opportunistic (per Phase 320 hybrid pattern + CLAUDE.md). Tidak buat xUnit project baru.
- **D-12:** No new NuGet dep (Phase 321 pakai lib existing: EF Core 8 + SignalR + Bootstrap 5 + vanilla JS/jQuery). SkiaSharp/ClosedXML dari Phase 320 tidak digunakan Phase 321.
- **D-13:** Pre-commit checklist `docs/DEV_WORKFLOW.md §5` wajib per commit (build + run + browser verify + DB lokal verify + notify IT post-push). Migration test apply+rollback wajib di Task 1 commit.

### Claude's Discretion
- **CD-01:** **Proton Tahun 3 detection** — Verify codebase saat plan/execute untuk identifikasi field/relasi yang menandai Assessment Proton Tahun 3 (kemungkinan via `Assessment.SubKategori`, `Package.SubKategori`, atau `AssessmentType` field — TBD). Helper `IsEditable` block kalau match.
- **CD-02:** **TrainingRecord Fail→Pass upsert behavior** — Claude verify pola existing di `GradingService.OnSessionComplete` / setara, ikuti behavior yang sudah ada (insert kalau missing, update kalau exist). Recommendation: upsert defensive — kalau session Completed yang Fail awal tidak generate TR, edit Fail→Pass perlu insert.
- **CD-03:** Dirty-state JS approach (vanilla addEventListener + form serialize compare snapshot) — pilih implementasi efficient.
- **CD-04:** Activity Log "Edit History" tab lazy-load strategy — pakai AJAX load partial saat tab di-klik (avoid load semua saat modal open). Cache sederhana session-scoped OK.
- **CD-05:** SignalR `monitor-{batchKey}` group naming — verify `batchKey` source dari `AssessmentHub` existing pattern, ikuti convention.
- **CD-06:** Anti-forgery token, error boundary controller (try-catch + log + TempData), ARIA dropdown a11y — ikuti spec section 5.4/5.6/5.7 + research code blocks tanpa rework.
- **CD-07:** Merge strategy feature branch → main saat ship: default `git merge --no-ff` (preserve task granularity); fallback squash kalau user request.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Research (codebase-verified, 13-task breakdown + full code blocks)
- `.planning/phases/321-assessment-edit-jawaban-peserta/321-RESEARCH.md` — Task 1-13 full breakdown, file structure map, spec coverage matrix line 1632-1656 (zero gap).

### Project Spec (4-patch codebase-verified)
- `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` (commit `c37e55ef`):
  - §4.1-4.10 — Edit Jawaban scope, helper, UI dropdown, page, POST flow, modal flip, `AssessmentEditLog`, regrade, activity log, permission
  - §5.1 — SignalR `workerAnswerEdited` payload
  - §5.2-5.8 — Cache invalidation, transaction, anti-forgery, concurrency, error handling, a11y, logging
  - §5.10 — Manual UAT checklist Phase 2
  - §5.11 — Migration `AddAssessmentEditLogs` schema

### Source Files (target untuk modifikasi/create)
- `Models/AssessmentEditLog.cs` — **create** (schema spec §4.7)
- `Data/ApplicationDbContext.cs` — **modify** (DbSet + fluent index `(AssessmentSessionId, EditedAt DESC)`)
- `Migrations/{timestamp}_AddAssessmentEditLogs.cs` — **create** via `dotnet ef migrations add`
- `Helpers/AssessmentEditEligibility.cs` — **create** (static `IsEditable(AssessmentSession)`)
- `Services/GradingService.cs` — **modify** (extract `ComputeScoreAndETInternalAsync` + add `RegradeAfterEditAsync`)
- `Controllers/AssessmentAdminController.cs` — **modify** (add GET/POST `EditPesertaAnswers` + GET `PreviewEditScore`) — SAMA FILE Phase 320, awareness merge conflict potential
- `Views/AssessmentAdmin/EditPesertaAnswers.cshtml` — **create**
- `Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml` — **modify** (dropdown ⋮ + IsEditable gating)
- `Views/Shared/_ActivityLogModal.cshtml` (atau equivalent) — **modify** (tab "Edit History")
- `wwwroot/js/edit-peserta-answers.js` — **create** (dirty state + reason validation + flip modal AJAX)
- `wwwroot/js/assessment-monitoring-detail.js` (atau inline view) — **modify** (SignalR handler `workerAnswerEdited`)

### Codebase Maps
- `.planning/codebase/ARCHITECTURE.md` — overall layering (Models/Services/Controllers/Views/Helpers)
- `.planning/codebase/STACK.md` — .NET 8 + EF Core 8 + SignalR + Bootstrap 5
- `.planning/codebase/TESTING.md` — confirm manual UAT + Playwright hybrid pattern
- `.planning/codebase/CONVENTIONS.md` — commit message format, namespace, helper naming
- `.planning/codebase/INTEGRATIONS.md` — SignalR AssessmentHub existing patterns

### Workflow Refs
- `docs/DEV_WORKFLOW.md §5` — pre-commit checklist (build + run + browser UAT + DB verify + notify IT post-push)
- `docs/DEV_WORKFLOW.md §4` — migration SOP (apply + rollback lokal wajib sebelum commit)
- `docs/SEED_WORKFLOW.md` — seed data lokal (kalau perlu test scenario edit untuk session Pass↔Fail)
- `CLAUDE.md` — Bahasa Indonesia, Develop Workflow, Seed Data Workflow

### Requirements
- `.planning/REQUIREMENTS.md` EDIT-01..13 — 13 acceptance criteria untuk Phase 321

### Prior Phase Context (carry-forward pattern)
- `.planning/phases/320-assessment-export-per-peserta-excel/320-CONTEXT.md` — Phase 320 decisions (testing hybrid pattern, commit cadence, IT notify format) yang carry ke Phase 321

### Promoted Plan Source
- `docs/superpowers/plans/2026-05-21-v318-phase2-edit-jawaban.md` (commit `594cfd95`) — superpowers `writing-plans` output original yang di-promote jadi RESEARCH

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/GradingService.cs` — existing compute method (target refactor extract no-side-effect internal). `OnSessionComplete` / setara → pattern referensi untuk `RegradeAfterEditAsync` cascade.
- `Controllers/AssessmentAdminController.cs` — Phase 320 baru-baru edit (line 3651 `ExportAssessmentResults`); Phase 321 add 3 action baru. SAMA file → awareness merge.
- `Hubs/AssessmentHub.cs` (atau setara) — SignalR existing dengan group `monitor-{batchKey}` pattern. Tambah broadcast `workerAnswerEdited`.
- `_ActivityLogModal.cshtml` existing — tab framework Bootstrap, tambah 1 tab baru lazy-load.
- `CertNumberHelper` — generate NomorSertifikat retry 3x, dipakai Fail→Pass cascade.
- `Models/AssessmentSession.cs` — `UpdatedAt` field existing untuk concurrency token + `IsManualEntry` + `Status` enum.
- `Models/AuditLog.cs` — generic audit existing (`ActionType="EditAssessmentAnswer"` dual-write).

### Established Patterns
- **Helpers folder** — `Helpers/*.cs` static class, `namespace HcPortal.Helpers`, no DI registration.
- **Controller authorization** — `[Authorize(Roles = "Admin, HC")]` di action level, Worker 403/redirect.
- **EF concurrency** — `UpdatedAt` hidden field pattern dipakai di entity lain (check existing).
- **Commit per task** — 1 task = 1 commit, message `feat|refactor(v17.0-p321): ...`, pre-commit checklist wajib.
- **Manual UAT documentation** — checklist inline di PLAN.md Step, ticked saat execute.
- **No frontend framework** — vanilla JS + jQuery (existing pattern, no React/Vue).

### Integration Points
- Tombol "Edit Jawaban" muncul di dropdown ⋮ per-user row di `AssessmentMonitoringDetail.cshtml` (Task 10) — conditional render via `@if (AssessmentEditEligibility.IsEditable(session))`.
- Modal Activity Log existing → tab baru "Edit History" (Task 12), AJAX lazy-load partial.
- SignalR `monitor-{batchKey}` group existing — broadcast event baru, frontend handler tambah di `assessment-monitoring-detail.js` atau inline.
- Migration `AddAssessmentEditLogs` perlu apply IT di DB Dev (mirip pola migration history existing project).

### Creative Options Constrained
- DB schema baru = WAJIB scope phase ini (REQ EDIT-13). 1 table baru saja (`AssessmentEditLogs`), no FK cascade complex.
- No new NuGet (SkiaSharp/ClosedXML dari Phase 320 NOT relevan Phase 321).
- Frontend tetap vanilla JS — no React/Vue/etc migration di scope phase ini.

</code_context>

<specifics>
## Specific Ideas

- **User minta verbose user-friendly reason labels** — bukan pakai code value PascalCase langsung. Code value (`ReasonCode` field DB) tetap konsisten, display label diterjemahkan ke bahasa Indonesia readable.
- **Flip modal eksplisit konsekuensi** — user concern: admin tidak sadar cabut sertifikat kalau modal cuma sebut "Score X→Y". Modal wajib sebut "NomorSertifikat akan dicabut" / "NomorSertifikat baru akan di-generate" eksplisit.
- **Toast verbose audit-style** — sesuai spec line 478 ROADMAP, format: `{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}, {flip}`.
- **Feature branch dipilih (beda dari Phase 320)** — Phase 321 ada migration + cascade DB write = risk lebih tinggi. Isolasi feature branch mudah revert seluruh phase. Phase 320 langsung main aman karena no DB change.
- **Phase 320 sudah remote** (verified `git ls-remote --tags origin v17.0-p320-complete` → commit `5f2306ba`). Phase 321 jalan tanpa blocker.

</specifics>

<deferred>
## Deferred Ideas

- **Bulk edit (multi-question single save UI revamp)** — Phase 321 sudah 1-form-N-question, tapi UX "edit semua session sekaligus" (mis. recompute mass setelah kunci jawaban diubah) = scope terpisah. Future phase kalau ada request.
- **Edit Essay** — Phase 321 hanya MC/MA. Essay edit (rubrik + score manual override) = phase terpisah (kompleks NLP grading interplay).
- **Manual override sertifikat tanpa edit jawaban** — Kalau admin perlu cabut/generate cert tanpa ubah jawaban, perlu UI/endpoint terpisah. Sekarang job IT direct DB.
- **AssessmentEditLog → CSV export** — Audit trail export untuk compliance. Future kalau ada request audit eksternal.
- **Webhook notification per edit ke channel Slack/Teams** — Saat ini cuma SignalR in-app toast. External notification = future phase.
- **Reshuffle history tab** (analog Edit History) — Activity log existing sudah cover, tapi tab dedicated Reshuffle bisa improvement future.
- **Brand color modal/toast Pertamina** — Sekarang pakai Bootstrap default warning/success. Theming brand = global UI hygiene task.

### Reviewed Todos (not folded)
- _Tidak ada todo backlog yang spesifik match Phase 321 selain `realtime-assessment.md` (sudah di-defer di Phase 320 review) — re-evaluate setelah Phase 321 SignalR live update implemented._

</deferred>

---

*Phase: 321-assessment-edit-jawaban-peserta*
*Context gathered: 2026-05-21*
