# Phase 323: Fix Cascade Bug AssessmentEditLogs - Context

**Gathered:** 2026-05-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Tambah `RemoveRange(AssessmentEditLogs)` block di 3 endpoint cascade delete di `Controllers/AssessmentAdminController.cs` — `DeleteAssessment` (~L2071), `DeleteAssessmentGroup` (~L2215), `DeletePrePostGroup` (~L2348). Wrap di transaction scope existing (L2040, L2184, L2313). Tutup oversight Phase 321 (model `AssessmentEditLog` baru, FK Restrict ke `AssessmentSession`) yang membuat session yang pernah di-edit soal exception "Gagal menghapus assessment" di Dev (Id 2, Id 5).

**Tidak include:** schema/model/migration change, refactor cascade helper, audit endpoint delete lain, FK Restrict→Cascade DB-level migration, UI filter old assessment.

</domain>

<decisions>
## Implementation Decisions

### Cascade Chain Order
- **D-01:** Block `RemoveRange(AssessmentEditLogs)` taruh **paling awal** di cascade chain — sebelum `PackageUserResponses` block. Urutan final per endpoint: `1.EditLogs → 2.PackageUserResponses → 3.AttemptHistory → 4.Packages(+Questions+Options) → 5.Session`. Alasan: edit logs adalah snapshot soal yang akan dihapus di step 4; hapus snapshot dulu paling clean buat audit trail.

### Audit Log Description
- **D-02:** Audit log description tambah `EditLogsCount=N` field. Format final:
  - `DeleteAssessment`: `"Deleted assessment '{title}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}"`
  - `DeleteAssessmentGroup`: `"Deleted assessment group '{title}' ({category}) [RepId={id}] SessionCount={N} Status={...} ResponseCount={...} EditLogsCount={...}"`
  - `DeletePrePostGroup`: ikuti pola yang sama
  - Capture `preDeleteEditLogsCount` SEBELUM cascade (sama pola `preDeleteResponseCount` existing).
  - Interpretasi Success Criteria #4 "tetap tercatat normal" = audit tetap ada (bukan unchanged) — auditor masa depan dapat lihat berapa edit log ikut terhapus.

### Logging Pattern
- **D-04:** Skip `LogInformation` kalau `editLogs.Count == 0`. Pakai guard `if (editLogs.Any()) { logger.LogInformation($"Deleting {editLogs.Count} edit logs"); _context.AssessmentEditLogs.RemoveRange(editLogs); }` — sama persis pola `PackageUserResponses` di L2076-2080 (Phase 312). Konsisten dengan pattern existing, tidak bikin noise log saat kasus no-edits.

### Smoke Test Method
- **D-03:** Playwright E2E test lokal (`tests/`) — login `admin@pertamina.com` (kredensial UAT dev), navigate Manage Assessment, click Delete, assert success toast + DB row gone. Connect ke `localhost:5277` (`dotnet run`), tidak menyentuh Dev/Prod. Repeatable + otomatis.
  - **3 skenario wajib:**
    - (a) Session no-edits → delete OK (no regression)
    - (b) Session 1+ edits → delete OK, EditLogs ikut terhapus
    - (c) Group campuran sibling no-edits + edits → delete OK
  - **Seed temporary:** AssessmentEditLog rows untuk skenario (b) + (c) — ikut SEED_WORKFLOW: snapshot DB sebelum (`sqlcmd BACKUP DATABASE`), catat di `docs/SEED_JOURNAL.md` klasifikasi `temporary + local-only`, restore setelah test selesai, tandai journal `cleaned`.

### Claude's Discretion
- Variable naming untuk count snapshot (`preDeleteEditLogsCount` vs `editLogsCount` — saya pilih `preDeleteEditLogsCount` ikut pola existing `preDeleteResponseCount`).
- Comment header per block `RemoveRange(AssessmentEditLogs)` — ikut pola comment Phase 312 `// Delete AssessmentEditLogs (Restrict FK — must be removed before session)`.
- Apakah pisah commit per endpoint atau 1 commit covering 3 endpoint — saya akan rekomendasikan 1 commit + 1 test commit (atomic per logical change), tapi planner boleh override.

### Folded Todos
Tidak ada todo dilipat ke scope ini. Match `realtime-assessment.md` (score 0.6) tidak relevan — file tidak exist di filesystem (false-positive matcher).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap & Requirements
- `.planning/ROADMAP.md` §"v18.0 Cascade Delete Hardening" (L529-568) — Phase 323 goal, success criteria 1-7, file affected 3 spot
- `.planning/REQUIREMENTS.md` §CASCADE-01 (L15-24) — acceptance criteria, scope/out-of-scope boundary

### Existing Code (prerequisites)
- `Controllers/AssessmentAdminController.cs` L2017-2146 — `DeleteAssessment` (target endpoint #1, cascade pattern source)
- `Controllers/AssessmentAdminController.cs` L2147-2284 — `DeleteAssessmentGroup` (target endpoint #2)
- `Controllers/AssessmentAdminController.cs` L2286-2410+ — `DeletePrePostGroup` (target endpoint #3)
- `Models/AssessmentEditLog.cs` — model definition (FK `AssessmentSessionId` → `AssessmentSession`, soft `virtual` nav prop)

### Prior Phase Context
- `.planning/phases/312-*/312-CONTEXT.md` — cascade pattern Phase 312 (PackageUserResponses + AttemptHistory + Packages chain)
- `.planning/phases/321-assessment-edit-jawaban-peserta/321-CONTEXT.md` — Phase 321 yang INTRODUCE `AssessmentEditLog` (root cause oversight)
- `.planning/phases/321-assessment-edit-jawaban-peserta/321-01-SUMMARY.md` ... `321-05-SUMMARY.md` — apa yang Phase 321 ship (FK Restrict declaration kemungkinan di Plan 01/02)

### Workflows (project-level)
- `CLAUDE.md` — Develop Workflow + Seed Data Workflow ringkasan
- `docs/DEV_WORKFLOW.md` — environment map (Lokal → Dev 10.55.3.3 → Prod), SOP migration, push checklist
- `docs/SEED_WORKFLOW.md` — klasifikasi temporary/permanent, format `docs/SEED_JOURNAL.md`, sqlcmd BACKUP/RESTORE
- `docs/SEED_JOURNAL.md` — journal aktif untuk catat seed temporary Phase 323

### Memory References
- `~/.claude/projects/.../memory/reference_dev_credentials.md` — kredensial `admin@pertamina.com` untuk UAT Playwright lokal

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Cascade pattern Phase 312** — `RemoveRange` block dengan `if (.Any())` guard + `LogInformation` — directly applicable, copy struktur untuk AssessmentEditLogs.
- **Transaction scope** — `using var tx = await _context.Database.BeginTransactionAsync();` sudah wrap guard + cascade + audit di 3 endpoint (L2040, L2184, L2313). Tinggal sisip block baru di dalam scope ini.
- **Audit log helper** — `_auditLog.LogAsync(userId, actorName, action, description, entityId, entityType)` standard signature, sudah dipakai 3 endpoint.
- **Logger** — `HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>()` per-method instance.

### Established Patterns
- **Pre-delete snapshot capture** — count snapshot diambil SEBELUM cascade supaya value tersedia untuk audit description setelah SaveChanges (referenced entity sudah hilang post-delete). Mandatory ikut pola untuk `preDeleteEditLogsCount`.
- **Restrict FK handling** — soft FK Restrict (default EF Core `OnDelete(DeleteBehavior.Restrict)`) wajib explicit `RemoveRange` sebelum parent delete; DB cascade tidak akan jalan. AssessmentEditLog `AssessmentSessionId` ikut pola ini (no explicit `OnDelete` override = default Restrict).
- **Logging info per cascade chain** — `$"Deleting {N} {entityName}"` template, sama pola untuk readability log.

### Integration Points
- **Per endpoint:** sisip block baru SETELAH transaction `BeginTransactionAsync` + guard + responseCount re-check, SEBELUM `PackageUserResponses` block.
- **Per audit log description:** tambah `EditLogsCount={preDeleteEditLogsCount}` token di akhir string.
- **Test integration:** Playwright config existing `tests/` (Phase 311+), reuse login fixture admin@pertamina.com.

### Pitfall: Snapshot timing
- `preDeleteEditLogsCount` HARUS di-capture sebelum `RemoveRange(editLogs)` call. Sequence di endpoint:
  1. Capture `preDeleteResponseCount` (existing)
  2. **Capture `preDeleteEditLogsCount` (NEW)** — `await _context.AssessmentEditLogs.CountAsync(e => e.AssessmentSessionId == id)`
  3. `RemoveRange(editLogs)` (NEW block)
  4. ... rest of existing cascade ...

</code_context>

<specifics>
## Specific Ideas

- **Comment pattern per block:** `// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)` — referensi phase explicit biar future audit traceability.
- **Playwright test naming:** `tests/Phase323_CascadeAssessmentEditLogs.spec.ts` (atau `.cs` kalau Playwright .NET) — discoverable per phase number.
- **SEED_JOURNAL entry format:** mengikuti convention existing di `docs/SEED_JOURNAL.md`, klasifikasi `temporary + local-only`, sebut entitas tersentuh: `AssessmentSession`, `AssessmentEditLog`, `PackageUserResponses` (untuk skenario campuran).

</specifics>

<deferred>
## Deferred Ideas

- **Extract `CascadeAssessmentSessionDependents(sessionIds)` helper** — already explicit out-of-scope per REQUIREMENTS.md L33. Reuse Phase 312+323 pattern total 3 endpoint x 4-5 blocks = ~12 RemoveRange call sites; refactor candidate kalau muncul cascade ke-4 atau lebih. Backlog untuk milestone berikutnya.
- **Audit endpoint delete lain (DeleteCategory, DeletePackage, DeleteQuestion, DeleteWorker, DeleteTraining, dll.)** — out-of-scope per REQUIREMENTS.md L32. Milestone berikutnya bisa scope `v19.0 Cascade Audit Sweep`.
- **Migration FK Restrict → Cascade DB-level** — out-of-scope per REQUIREMENTS.md L34. Trade-off (DB silent cascade vs endpoint explicit + audit-friendly) sudah diputuskan: tetap endpoint explicit.
- **UI surface old assessment filter** di `ManageAssessmentTab_Assessment` line 115 — separate UX issue, backlog tersendiri per REQUIREMENTS.md L35.

### Reviewed Todos (not folded)
- `realtime-assessment.md` (match score 0.6) — file tidak exist di filesystem (matcher false-positive); tidak relevan dengan cascade fix scope.

</deferred>

---

*Phase: 323-fix-cascade-bug-assessmenteditlogs-di-3-endpoint-delete-asse*
*Context gathered: 2026-05-26*
