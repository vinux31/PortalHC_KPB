# Phase 314: Fix Regenerate Token untuk Status Upcoming - Context

**Gathered:** 2026-05-08
**Status:** Ready for planning
**Mode:** interactive discuss — 7 gray areas selected, 24 decisions captured

<domain>
## Phase Boundary

Investigative bug fix endpoint `Controllers/AssessmentAdminController.cs` `RegenerateToken(int id)` (line 2427-2475) yang gagal saat trigger condition: **Status=`Upcoming` + IsTokenRequired=true + 0 worker yang sudah masuk ujian**. Pendekatan 2-plan:

- **Plan 01 (Repro+RESEARCH):** Repro bug di Dev, capture exception/stacktrace dari server log, dokumentasikan 4 hipotesis (NRE Schedule.Date / AuditLog FK / Concurrency / Frontend handler) dengan status verifikasi (CONFIRMED/RULED OUT/INCONCLUSIVE) → root cause + fix proposal di `314-RESEARCH.md`.
- **Plan 02 (Patch+test):** Apply defensive backend patch (root cause + 3 hipotesis lain sebagai guard), frontend error propagation di 3 view, Playwright E2E smoke test 3 skenario.

**Acceptance criteria (dari ROADMAP.md):**
1. Investigation phase: repro bug di environment dev sesuai trigger condition; capture exception/log/HTTP response
2. Root cause documented di `314-RESEARCH.md`
3. Patch minimal sesuai root cause (defensive null check / audit log try-catch granular / retry / frontend fix)
4. Logging granular `_logger.LogError(ex, "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}", id, status, hasStarted)`
5. Frontend `AssessmentMonitoring.cshtml` line 396–419 & `AssessmentMonitoringDetail.cshtml` line 981–1009 — error message dari server JSON dipropagasi ke `alert()` (bukan generik)
6. Smoke test 3 skenario: Upcoming+0-peserta OK, Upcoming+sebagian-start OK, Open running OK

**In-scope (per discussion 2026-05-08):**
- Defensive backend patch coverage 4 hipotesis (D-05)
- Audit log try-catch swallow + warning fallback (D-06)
- Transaction wrap loop sibling update (D-17)
- Frontend `.catch()` parse server error body + `response.ok` check + `r.text()` fallback di 3 view (D-07/D-08/D-09/D-11)
- Server-side error specific by exception type (D-12)
- Logging extended structured + LogInformation success path (D-19/D-20/D-21)
- Warn confirmation dialog saat Open + active worker (D-22/D-23)
- Playwright E2E 3 skenario via UI Admin setup hook seeding (D-13/D-14/D-15/D-16)

**Out-of-scope:**
- TOCTOU re-fetch / version check (D-18 — race window narrow + last-writer-wins acceptable)
- Concurrency token (RowVersion) — schema change required, deferred
- Refactor extract helper JS function — scope creep
- Schema migration (no DB schema change needed)

</domain>

<decisions>
## Implementation Decisions

### Repro & Investigation Strategy
- **D-01:** **Repro via URL Dev** (`http://10.55.3.3/KPB-PortalHC`). Saya buat assessment sendiri (Status=Upcoming, IsTokenRequired=true, peserta=admin@pertamina.com → 0 worker yang sudah masuk ujian). Confirm bug exists di Dev dulu, baru replicate kondisi minimal di lokal untuk fix (sesuai CLAUDE.md workflow: cek di Dev → fix di lokal).
- **D-02:** **Capture exception dari server log Dev** sebagai langkah pertama. Stacktrace pinpoint root cause langsung — no guessing. Fallback: kalau log tidak accessible, iterate 4 hipotesis manual.
- **D-03:** **RESEARCH.md format Standard** — tabel 4 hipotesis (NRE Schedule / AuditLog FK / Concurrency / Frontend) dengan kolom Evidence/Status (CONFIRMED/RULED OUT/INCONCLUSIVE) + root cause section + fix proposal.
- **D-04:** **Plan structure split** sesuai ROADMAP literal: Plan 01 (Repro+RESEARCH) + Plan 02 (Patch backend + frontend + smoke test).

### Patch Philosophy (Backend)
- **D-05:** **Defensive coverage 4 hipotesis** — fix root cause yang terbukti dari stacktrace + tambah guard untuk 3 hipotesis lain: (a) defensive null check siblings group key, (b) audit log try-catch independent (D-06), (c) `IsTokenRequired` re-check + sibling list non-empty guard, (d) frontend handler propagate (D-07). Tradeoff: lebih banyak diff tapi prevent regression similar bugs di masa depan.
- **D-06:** **Audit log atomicity = try-catch swallow.** Wrap `_auditLog.LogAsync` dalam try/catch independent. Audit failure log ke `_logger.LogWarning("Audit log failed for RegenerateToken session {Id}", id)` tanpa block response success. Pattern Phase 306 D-10. Plus pastikan `regenUser?.Id ?? ""` defensive untuk FK violation.
- **D-17:** **Transaction wrap.** Wrap loop sibling update + `SaveChangesAsync` dalam `_context.Database.BeginTransactionAsync()` → atomic commit/rollback. Try-catch swallow audit log (D-06) berlaku **di luar** transaction utama (audit fail tidak rollback token).
- **D-18:** **Tidak perlu re-fetch / TOCTOU guard.** Race window sempit (admin action, frequency rare) + last-writer-wins acceptable (no data corruption). Phase 312 WR-01 pattern tidak applicable di sini.

### Frontend Response Handler & Error UX
- **D-07:** **Fix `.catch()` agar parse server error body** — di `.then(r => r.json())` chain, kalau response `success: false` → throw error dengan `data.message` agar `.catch()` dapat detail server.
- **D-08:** **Patch 3 view sekaligus** — `Views/Admin/AssessmentMonitoring.cshtml` line 396-419, `AssessmentMonitoringDetail.cshtml` line 1004-1033, **plus** `ManageAssessment.cshtml` line 456 (yang di-find scout, tidak di-mention ROADMAP eksplisit). Konsisten UX semua entry point.
- **D-09:** **alert() native + propagate server message.** Format: `alert('Gagal regenerate token: ' + serverMessage)`. Existing pattern, minimal diff. Hindari toast/banner Bootstrap (scope creep).
- **D-10:** **Wording = server message langsung passthrough** (technical untuk Admin/HC role). Format: `'Gagal regenerate token: {pesan server}. Coba lagi atau hubungi IT.'`
- **D-11:** **Handle non-JSON 5xx** via `response.ok` check + fallback `r.text()`. Pattern:
  ```js
  .then(function(r) {
    if (!r.ok) {
      return r.text().then(function(t) {
        return Promise.reject(t || ('HTTP ' + r.status));
      });
    }
    return r.json();
  })
  ```
- **D-12:** **Server-side return error specific by exception type:**
  - `DbUpdateException ex` → `"Database error: " + ex.Message`
  - `NullReferenceException ex` → `"Data assessment tidak lengkap (sibling/Schedule null). Hubungi IT."`
  - generic `Exception ex` → `ex.Message` (sudah disanitize EF Core)

### Logging & Telemetry
- **D-19:** **`hasStarted` definition** = `siblings.Any(s => s.StartedAt != null)` (boolean aggregate dari sibling group). Bug trigger condition: `Status=Upcoming + hasStarted=false`.
- **D-20:** **Extended structured logging** di catch block:
  ```csharp
  _logger.LogError(ex,
    "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
    id, assessment.Status, hasStarted, siblings.Count, assessment.IsTokenRequired);
  ```
- **D-21:** **`LogInformation` di success path:**
  ```csharp
  _logger.LogInformation(
    "RegenerateToken success for session {Id}, {Count} siblings updated by {ActorName}",
    id, siblings.Count, regenActorName);
  ```

### Open + Active Worker Behavior
- **D-22:** **Allow regen tapi warn admin di confirm dialog.** Frontend detect (a) `assessment.Status === 'Open'` AND (b) `siblingStartedCount > 0` → tampilkan warning dialog sebelum POST. Server tetap proceed kalau admin confirm — preserve SC #6 (Open running OK).
- **D-23:** **Warning wording:** `"PERINGATAN: {N} worker sudah masuk ujian. Regenerate token akan invalidate session token mereka — mereka harus login ulang dengan token baru. Jawaban yang sudah disimpan tidak hilang. Lanjutkan?"`. Angka {N} dari sibling count `StartedAt != null`. Frontend butuh data ini — endpoint GET ringan atau inline `data-started-count` attribute saat render Monitoring view.

### Smoke Test (E2E Playwright)
- **D-13:** **Mechanism = Playwright E2E** dengan dedicated fixture (Phase 312/313 pattern). 3 skenario sesuai SC #6.
- **D-14:** **Seeding via UI Admin setup hook** — login admin → navigate `/Admin/CreateAssessment` → buat 3 assessment dengan title `Phase 314 Token Fixture {Upcoming0|UpcomingPartial|OpenRunning}` + assign admin sebagai peserta. Setup ~30s tapi real-flow (verify UI Admin tidak regress di sisi lain).
- **D-15:** **Assertion comprehensive:** (a) `AccessToken` di DB berubah ke value baru, (b) sibling sessions ter-update, (c) `AuditLogs` row `ActionType='RegenerateToken'` exists, (d) UI alert `'Token baru: {6-char}'` muncul.
- **D-16:** **File baru** `tests/e2e/admin-assessment-token.spec.ts` (konsisten Phase 312 pattern `admin-assessment-delete.spec.ts`).
- **D-24:** **Skenario test #3 (Open running)** verify (a) admin success regen + (b) AccessToken DB berubah + (c) worker dengan token lama dapat error invalid saat next request. **Asumsi perlu di-verify research:** apakah token lama benar-benar invalidate worker session, atau session worker sudah established via cookie/identity sehingga token cuma untuk login flow. Planner/researcher harus konfirmasi behavior ini sebelum tulis test.

### Claude's Discretion
- **Stacktrace parsing logic** untuk RESEARCH.md — exact format tabel/markdown adjustment sesuai temuan investigasi.
- **Error wording kontekstual** untuk SC #5 — exact phrasing bahasa Indonesia bisa adjust ke tone existing.
- **Spinner/disable button visual** saat regen in-flight — line 1008-1010 di AssessmentMonitoringDetail sudah ada pattern, replicate ke 2 view lain kalau perlu.
- **Test fixture cleanup** — Playwright afterEach delete fixture vs leave-as-is untuk re-use, implementer pilih.
- **Inline `data-started-count` rendering** vs lightweight GET endpoint untuk D-23 wording — pilih yang lebih simple sesuai existing markup.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Codebase (existing logic to fix/preserve)
- `Controllers/AssessmentAdminController.cs` line 2427-2475 — `RegenerateToken(int id)` primary fix target
- `Views/Admin/AssessmentMonitoring.cshtml` line 396-419 — frontend handler #1 (button-list view)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` line 1004-1033 — frontend handler #2 (regenToken function dengan spinner)
- `Views/Admin/ManageAssessment.cshtml` line 456 — frontend handler #3 (added per D-08, NOT in ROADMAP literal)
- `Services/AuditLogService.cs` line 21-42 — `LogAsync` API + `_context` shared instance
- `Models/AssessmentSession.cs` line 18 — `Schedule` is DateTime non-nullable (NRE on `.Date` impossible at struct level)
- `Models/AssessmentSession.cs` line 40 — `StartedAt` is DateTime? (used for `hasStarted` D-19)
- `Models/AuditLog.cs` — `ActorUserId` required string (FK behavior unknown — verify research)

### Phase patterns (carry-forward)
- `.planning/phases/306-score-editable-per-question-type/` — D-10 AuditLog try/catch + LogWarning fallback pattern (referenced D-06)
- `.planning/phases/312-admin-full-delete-assessment-room/312-CONTEXT.md` — D-03 AuditLog `{Action}Blocked` naming, WR-01 TOCTOU context (NOT applicable — D-18)
- `.planning/phases/312-admin-full-delete-assessment-room/312-01-SUMMARY.md` — `EnsureCanDeleteAsync` helper pattern (analog template kalau guard logic complex enough)
- `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-CONTEXT.md` — D-07/D-08 fixture title pattern + D-13 banner reload behavior

### Project & requirements
- `.planning/REQUIREMENTS.md` line 63 — TKN-01 acceptance + hipotesis dari user
- `.planning/REQUIREMENTS.md` line 112 — TKN-01 → Phase 314 mapping
- `.planning/ROADMAP.md` line 254-271 — Phase 314 SC 1-6 + Plans 314-01/314-02
- `CLAUDE.md` — Pertamina dev workflow (lokal → Dev → Prod), repro di Dev URL `10.55.3.3`
- `docs/DEV_WORKFLOW.md` — environment map + dotnet build verify

### Memory references
- `reference_dev_credentials.md` — admin@pertamina.com login lokal/Dev untuk UAT/Playwright
- Audit-29Apr T4 — root issue triggering TKN-01

### Test infrastructure
- `tests/e2e/helpers/wizardSelectors.ts` — Phase 307 NEW folder convention untuk e2e selectors
- `tests/helpers/accounts.ts` — login fixture untuk admin/HC role
- `tests/e2e/` existing structure — feature-namespaced files (admin-assessment-delete.spec.ts pattern Phase 312)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`AuditLogService._auditLog.LogAsync()`** — established API, accept `actorUserId/actorName/actionType/description/targetId/targetType`. Reuse untuk RegenerateToken success entry (existing line 2459-2465 sudah pakai).
- **`appUrl()` helper** di Views/Shared layout — frontend URL injection. Existing kode sudah pakai (line 400, 1012, 456). No path-prefix bug risk (Phase 312 WR-02 already mitigated).
- **`GenerateSecureToken()` private helper** line 2478-2492 — 6-char alphanumeric (exclude 0/O/1/I/L). Tidak perlu modify, reuse as-is.
- **`_userManager.GetUserAsync(User)`** — existing pattern untuk capture actor. Plus null-safe access dengan `regenUser?.NIP` / `regenUser?.FullName`.
- **`_logger.LogError(ex, ...)`** — existing pattern di catch block (line 2472), extend per D-20.
- **EF Core implicit transaction** via `SaveChangesAsync` — D-17 wrap explicit BeginTransactionAsync untuk multi-step atomicity.

### Established Patterns
- **AuditLog ActionType verbatim string** — `"RegenerateToken"` (15 chars, fits MaxLength(50)). Konsisten dengan ActionType existing di AuditLog.cs comment (CreateAssessment/EditAssessment/dst).
- **Defensive `?? ""` fallback** untuk required string fields (existing line 2460: `regenUser?.Id ?? ""`).
- **Try-catch + generic message return** existing line 2469-2474 — modify per D-12 specific by exception type.
- **alert() native + location.reload()** UX pattern (line 410) — preserve untuk consistency.
- **Spinner-border + button.disabled toggle** (line 1008-1010) — existing pattern di Detail view, bisa replicate ke 2 view lain.

### Integration Points
- **`RegenerateToken` POST endpoint** — single backend modification site
- **3 `.btn-regenerate-token` / `regenToken(btn)` JS handlers** — frontend modification sites di 3 view
- **`_logger` ILogger<AssessmentAdminController>** — already injected (line 2471), extend per D-20/D-21
- **AuditLogs table schema** — no schema change needed
- **Sibling group key** = `Title + Category + Schedule.Date` (line 2445-2447) — preserve existing matching logic

### Patterns to Avoid (anti-patterns dari prior phase reviews)
- **Path-prefix bug `appUrl()`** (Phase 312 WR-02) — kode existing sudah pakai `appUrl()`, jangan regress ke hardcoded path.
- **Playwright selector substring match** (Phase 312 WR-03) — gunakan badge-scoped/test-id untuk row selector kalau test pakai title fixture matching.
- **Dedicated fixture title** (Phase 312 WR-04) — pakai exact title pattern `Phase 314 Token Fixture {Scenario}`, jangan generic.
- **Audit log throw block business action** (Phase 306 D-10) — D-06 wrap try-catch swallow + LogWarning.
- **Generic catch swallow detail** — D-12 split per exception type, jangan return generic message untuk semua case.

</code_context>

<specifics>
## Specific Ideas

- **Repro env user-specified** (D-01): URL Dev `http://10.55.3.3/KPB-PortalHC`, account admin@pertamina.com sebagai peserta. User explicitly said "buat assessmentnya sendiri" — admin punya wewenang Admin+peserta sekaligus.
- **Hipotesis prioritization via stacktrace** (D-02): Skip nebak; biarkan log server pinpoint. Strategi efisien dari user response "Capture exception (Recommended)".
- **Defensive cumulative philosophy** (D-05): User pilih full coverage — sesuai dengan habit dari Phase 312 D-04 (security-first cumulative defense).
- **Audit failure non-blocking** (D-06): Pattern Phase 306 D-10 explicitly cited, user setuju.
- **Inline data attribute untuk warning count** (D-23): Frontend butuh sibling startedCount untuk wording. Render dari Monitoring view markup sebagai `data-started-count` per row → no extra GET. Detail view bisa hit endpoint terpisah kalau perlu.
- **Test seeding via UI Admin** (D-14): User pilih over DB manipulation — real flow, also smoke-test UI Admin tidak break.

</specifics>

<deferred>
## Deferred Ideas

- **TOCTOU re-fetch + version check** (D-18 alternative) — race window narrow, last-writer-wins acceptable. Bisa di-add kalau frequency tinggi atau ada audit minta strict isolation.
- **Concurrency token (RowVersion / [Timestamp])** — schema change required, deferred ke milestone v16.0+ kalau race detection dibutuhkan.
- **Pessimistic lock SQL `WITH (UPDLOCK)`** — SQL Server-specific, hard to test, deferred.
- **Toast/Bootstrap banner UX** untuk error message (D-09 alternative) — modern UX tapi scope creep. Bisa di-do as separate UI polish phase.
- **Extract reusable `regenerateTokenWithErrorHandling()` JS helper** (D-08 alternative) — DRY tapi refactor di luar bug fix scope.
- **Server-side cookie/session invalidate on token regen** (D-24 followup) — kalau research confirm worker session NOT auto-invalidated by token change, ini jadi separate enhancement (force logout active sessions).
- **`HttpContext.TraceIdentifier` correlation ID di logging** (D-20 alternative) — useful untuk cross-reference ASP.NET Core request log, tapi scope creep untuk fix ini.
- **Hybrid Playwright + manual UAT** (D-13 alternative) — kalau Playwright fixture too brittle, fallback manual.

</deferred>

---

*Phase: 314-fix-regenerate-token-untuk-status-upcoming*
*Context gathered: 2026-05-08*
