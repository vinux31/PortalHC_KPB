# Phase 310: Essay Finalize Idempotency - Context

**Gathered:** 2026-05-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 310 menambahkan **idempotency + UI gate + dedupe** ke Essay finalize flow:

1. **Backend no-op behavior** untuk `AssessmentAdminController.FinalizeEssayGrading` (L2716) saat session sudah `Status='Completed'`: ganti error generic jadi friendly success no-op (per SC #1).
2. **UI button gate** untuk tombol "Selesaikan Penilaian" di `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419: disable + tooltip saat `Status='Completed'` && `NomorSertifikat != null` (per SC #2).
3. **Notification dedup** untuk `WorkerDataService.NotifyIfGroupCompleted` (L314): cek existing UserNotifications sebelum send (per SC #3).
4. **AuditLog entry** untuk FinalizeEssayGrading + dedup natural via WHERE clause guard (per SC #4).
5. **Parallel finalize protection** via existing EF Core `ExecuteUpdateAsync` WHERE-clause guards + idempotent return value (per SC #5).

**Scope EXPLICIT exclusions:**
- TIDAK menambahkan tombol baru di CDP CertificationManagement (ROADMAP wording "atau panel detail" = alternative, bukan dua tempat).
- TIDAK schema migration baru (notif dedup pakai field UserNotifications yang sudah ada).
- TIDAK SemaphoreSlim app-level lock atau DB serializable transaction (overkill — EF WHERE-clause guards proven cukup di Phase 309 + GradingService).
- TIDAK refactor literal status check di controller (kalau sempat, opportunistic via constant `AssessmentConstants.AssessmentStatus.Completed` dari Phase 309 D-04 — tapi bukan core SC).

</domain>

<decisions>
## Implementation Decisions

### UI Gate (SC #2)

- **D-01:** Scope UI button = `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419 (`.btn-finalize-grading` "Selesaikan Penilaian"). TIDAK touch CDP CertificationManagement (view CDP cuma list sertifikat existing — tidak ada tombol Create Sertifikasi terpisah). Ditetapkan oleh planner berdasarkan kondisi kode aktual; user defer ke analisa.

- **D-02:** UI gate behavior = **Disable button + tooltip** saat `session.Status == "Completed"` && `session.NomorSertifikat != null`. Tooltip pakai BI: "Sudah selesai pada [tanggal CompletedAt format dd MMM yyyy HH:mm]". Affordance jelas: tombol tetap visually present (bukan hidden) tapi greyed out + cursor not-allowed + Bootstrap `disabled` attribute. Existing wrapper div display: block/none (dari `EssayPendingCount == 0`) tetap, tambah second guard untuk disabled state.

### Backend Response (SC #1)

- **D-03:** Saat `session.Status == "Completed"` (klik 2x atau race), API `FinalizeEssayGrading` return **success no-op friendly**:
  ```json
  {
    "success": true,
    "alreadyFinalized": true,
    "message": "Penilaian sudah diselesaikan sebelumnya pada [tanggal CompletedAt]",
    "score": <int>,
    "isPassed": <bool>,
    "nomorSertifikat": <string|null>
  }
  ```
  UI handler di `AssessmentMonitoringDetail.cshtml` L1339 (fetch handler) baca field `alreadyFinalized`: kalau `true`, render **toast info biru** (Bootstrap `alert-info` + icon `bi-info-circle-fill`, pakai TempData[Info] convention dari Phase 309 D-07 atau inline alert). BUKAN error merah. Semantically idempotent: 2x klik = 2x success, no side-effect.

- **D-04:** Saat `session.Status` adalah **non-terminal dan non-Completed** (`Open`, `InProgress`, `Cancelled`), API return **pesan spesifik per status** (per SC #1 "ganti pesan jadi explisit"):
  - `Open` → `{ success: false, message: "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian." }`
  - `InProgress` → `{ success: false, message: "Belum bisa di-finalize. Peserta sedang mengerjakan ujian." }`
  - `Cancelled` → `{ success: false, message: "Tidak bisa di-finalize. Session sudah dibatalkan." }`
  - Status lain (defensive) → fallback message generic dengan nama status di-interpolate.

  UI tetap render error toast (alert-danger) untuk case-case ini — actionable feedback ke Admin troubleshooting.

### Notification Dedup (SC #3)

- **D-05:** `WorkerDataService.NotifyIfGroupCompleted` (L314) tambah dedup via **lookup UserNotifications existing** sebelum send loop:
  - Sebelum kirim ke setiap recipient, query: `_context.UserNotifications.AnyAsync(n => n.UserId == recipientId && n.Type == "ASMT_ALL_COMPLETED" && n.SourceTitle == completedSession.Title && n.SourceDate == completedSession.Schedule.Date)`.
  - Kalau sudah ada → skip recipient.
  - **No DB schema migration**. Reuse field UserNotifications yang sudah ada (Type, plus identifier dari Source title+date).
  - User defer ke recommendation berdasarkan simplicity vs. NotificationSentAt column tradeoff.
  - **Note:** kalau UserNotifications schema TIDAK punya field SourceTitle/SourceDate, planner pakai field equivalent (mis. metadata JSON) atau extend ke approach (b) "tambah column NotificationSentAt di AssessmentSessions" sebagai fallback. Verify schema saat planning.

### Audit Log Dedup (SC #4)

- **D-07:** Tambah `_auditLog.LogAsync(currentUser?.Id, actorName, "FinalizeEssayGrading", $"Session {sessionId} finalized")` di FinalizeEssayGrading **DI DALAM if-block "rowsAffected > 0"** dari status update ExecuteUpdateAsync (L2784-2790). Otomatis dedup karena 2 thread paralel cuma 1 yang dapat row affected dari WHERE clause `Status == "Menunggu Penilaian"` — thread ke-2 hit 0 row, skip log. User defer ke recommendation. Reuse pattern existing AuditLog di method lain (mis. AddCategory L326). **PENTING:** capture rowsAffected dari ExecuteUpdateAsync (return int — current code throws away return value), pakai untuk gate audit log + cert generation + notif call.

### Concurrency Protection (SC #5)

- **D-06:** Tidak tambah lock baru. Andalkan kombinasi guard yang sudah ada:
  1. **Status WHERE-clause guard** (L2785) `WHERE Status == AssessmentConstants.AssessmentStatus.PendingGrading` — thread ke-2 dapat 0 row affected.
  2. **Cert WHERE-clause guard** (L2820) `WHERE NomorSertifikat == null` — thread ke-2 0 row, no double cert.
  3. **TrainingRecord AnyAsync guard** (L2794) `AnyAsync(t => UserId+Judul+Tanggal match)` — explicit duplicate check.
  4. **Notification dedup baru** dari D-05 — skip kalau notif sudah ada.
  5. **Audit log dedup baru** dari D-07 — gated oleh rowsAffected > 0.
  6. **Idempotent return value** — thread ke-2 baca state final session lalu return success no-op (D-03 alreadyFinalized: true response). User experience: kedua thread "berhasil", state correct, no spam.

  **Integration test (SC #5):** akan tes scenario `Task.WhenAll(...)` 5-10 parallel finalize ke same sessionId dan assert: (a) cuma 1 row di TrainingRecord, (b) NomorSertifikat di-set sekali, (c) cuma 1 audit log entry, (d) cuma 1 notif per recipient, (e) semua thread return success.

### Claude's Discretion

- **Implementation pattern detail**: Razor conditional vs. JS guard untuk D-02 disable, exact tooltip styling (Bootstrap `data-bs-toggle="tooltip"` vs. native `title`), JSON contract field naming convention untuk D-03 — defer ke planner berdasarkan codebase convention.
- **Audit log payload format** D-07: `Detail` field content (verbose vs. minimal) — defer ke planner.
- **Toast vs. inline alert** D-03 implementation: pakai existing global toast helper (kalau ada) atau inline alert injection — defer ke planner berdasarkan UI convention existing di AssessmentMonitoringDetail.cshtml.
- **Exception handling**: D-03/D-04 wrap di try-catch pattern Phase 309 WCRT-01 (DbException → FormatException → Exception berlapis) — recommended tapi planner discretion.

### Folded Todos

[None — tidak ada todo backlog yang relevan dengan Phase 310 idempotency scope.]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 309 Lockings (constants & helpers WAJIB di-pakai)
- `Models/AssessmentConstants.cs` L18 — `public const string PendingGrading = "Menunggu Penilaian"` constant; L43-44 helper `IsAssessmentSubmitted(string?)`. Phase 310 WAJIB pakai constant, BUKAN literal.
- `.planning/phases/309-worker-cert-defensive-submitted-status/309-CONTEXT.md` — Decisions D-01..D-08 (defensive try-catch order, structured logging, null-safe accessor, TempData[Info] convention, helper introduction). Pattern reference untuk D-03 friendly response + D-04 status branching.

### Existing Code Anchors (refactor target)
- `Controllers/AssessmentAdminController.cs` L2712-2833 — `FinalizeEssayGrading` method body (refactor target SC #1, #4, #5).
- `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419 (.btn-finalize-grading button) + L1331-1380 (JS fetch handler dispatch ke `/Admin/FinalizeEssayGrading`) — refactor target SC #2.
- `Services/WorkerDataService.cs` L314-345 — `NotifyIfGroupCompleted` method (refactor target SC #3 dedup).
- `Services/AuditLogService.cs` L9-40 — pattern reference untuk audit log call (D-07).

### Project Standards
- `CLAUDE.md` — Bahasa Indonesia mandatory untuk user-facing copy.
- `.planning/PROJECT.md` — project vision + non-negotiables.
- `.planning/REQUIREMENTS.md` ESCG-01 (line ~25-26) — original requirement statement.
- `.planning/ROADMAP.md` Phase 310 entry — 5 success criteria source-of-truth.

### Schema Verification Required (planning step)
- Schema `UserNotifications` table — verify field availability untuk D-05 dedup query (SourceTitle, SourceDate, Type, UserId). Kalau tidak ada, fallback ke approach (b) "NotificationSentAt column di AssessmentSessions" + DB migration.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`AssessmentConstants.AssessmentStatus.Completed`** (Models/AssessmentConstants.cs L17) — pakai untuk status check di FinalizeEssayGrading guard, replace literal `"Completed"`.
- **`AssessmentConstants.AssessmentStatus.PendingGrading`** (L18) — pakai untuk WHERE clause guard di ExecuteUpdateAsync (L2785).
- **`AuditLogService.LogAsync(userId, userName, action, detail)`** (Services/AuditLogService.cs L29-40) — pattern dependable untuk D-07 audit entry.
- **`TempData["Info"]` + Layout alert block** (Views/Shared/_Layout.cshtml L209-216 dari Phase 309) — convention untuk friendly toast info biru di D-03 (kalau pakai TempData; kalau JS inline-injected toast, pakai existing helper).
- **EF Core `ExecuteUpdateAsync` return int (rows affected)** — kunci untuk D-06 idempotency. Capture return value, gate audit log + cert + notif call dengan `if (rowsAffected > 0)`.
- **`Bootstrap .disabled` + `data-bs-toggle="tooltip"`** (existing pattern di views lain) — pakai untuk D-02 button disable + tooltip.

### Established Patterns

- **EF WHERE-clause guard untuk race condition** (di GradingService L196 setelah Phase 309-03 refactor; di FinalizeEssayGrading L2785 + L2820) — **proven dependable** untuk concurrency protection. Pattern: `Where(s => s.Id == id && s.Status == X).ExecuteUpdateAsync(...)` returns rows affected; thread ke-2 dapat 0 = skip side-effect.
- **Try-catch berlapis dengan structured logging** (Controllers/CMPController.cs L1822-1845 dari Phase 309 WCRT-01): order DbException → FormatException → NullReferenceException → Exception. Pattern bisa di-replicate untuk FinalizeEssayGrading defensive wrap.
- **Status check via helper** (`AssessmentConstants.IsAssessmentSubmitted(status)`) — pakai untuk readability check status terminal (Completed OR PendingGrading).
- **Bahasa Indonesia user-facing** (CLAUDE.md mandate) — semua message di D-03/D-04 + tooltip D-02 dalam BI.

### Integration Points

- **JSON contract `FinalizeEssayGrading`** — handler di JS (AssessmentMonitoringDetail.cshtml L1331-1380) consume `success`, `score`, `pendingCount`, `allGraded` field. Tambah field `alreadyFinalized` + `message` + `nomorSertifikat` (D-03) — backwards-compatible (handler bisa branch kalau field ada).
- **TrainingRecord generation** (L2792-2809) — sudah idempotent via AnyAsync guard. Tambah audit log di sini? NO — audit di status update gate (D-07).
- **NotifyIfGroupCompleted call** (L2828-2830) — refactor service-side (D-05), caller-side tetap simple.
- **Bootstrap toast / TempData[Info] alert** — pilih satu approach untuk D-03 friendly UI; kalau ada existing global toast helper di JS, pakai itu; kalau tidak, inject inline alert.

</code_context>

<specifics>
## Specific Ideas

- **Audit log Detail field content** untuk D-07: format `"Session {sessionId} finalized: score={finalPercentage}%, isPassed={isPassed}, cert={nomorSertifikat ?? 'none'}"` — actionable untuk forensic.
- **Tooltip text format** D-02: `"Sudah selesai pada {CompletedAt:dd MMM yyyy HH:mm} WIB"` (consistent dengan WIB convention dari Phase 304).
- **Toast positioning** D-03: top-right corner Bootstrap default, auto-dismiss 5s untuk friendly success.
- **Status name → BI mapping** D-04 boleh dimasukkan ke `AssessmentConstants` sebagai static helper kalau bisa di-reuse di tempat lain (Claude's discretion).

</specifics>

<deferred>
## Deferred Ideas

- **Tombol "Create Sertifikasi" baru di CDP CertificationManagement** — out of scope Phase 310 (scope creep dari ROADMAP wording ambigu). Kalau dibutuhkan kemudian, jadi phase tersendiri untuk CDP CertificationManagement Workflow Enhancement.
- **NotificationSentAt column migration** — fallback approach D-05 kalau lookup UserNotifications tidak feasible (schema tidak punya field identifier yang cukup). Defer ke planner verification step.
- **AssessmentConstants.IsAssessmentSubmitted reuse di FinalizeEssayGrading** — opportunistic refactor (replace literal `"Menunggu Penilaian"` di L2719 + L2785). Boleh di-include planner discretion, tapi bukan core SC.
- **SemaphoreSlim per-session lock** — kalau scale-out ke multi-instance dibutuhkan kemudian (saat ini KPB single instance), pertimbangkan distributed lock (Redis SETNX). Defer.
- **Reviewed Todos (not folded):** [None — tidak ada todo backlog cross-reference]

</deferred>

---

*Phase: 310-essay-finalize-idempotency*
*Context gathered: 2026-05-01*
