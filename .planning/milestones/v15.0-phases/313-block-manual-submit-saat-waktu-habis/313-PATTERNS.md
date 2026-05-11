# Phase 313: Block Manual Submit Saat Waktu Habis — Pattern Map

**Mapped:** 2026-05-08
**Files analyzed:** 6 (3 modified + 3 created)
**Analogs found:** 6 / 6 (semua exact / role-match in-repo)
**Stack target:** ASP.NET Core MVC 8 (Razor + Bootstrap 5 + bootstrap-icons) + Playwright 1.58.2 (TypeScript)
**Bahasa:** Bahasa Indonesia (per `CLAUDE.md` — semua user-facing copy + AuditLog Description)

> Konsumen: `gsd-planner`. Tujuan dokumen ini supaya planner tidak perlu re-grep analog — semua excerpt + line number sudah disiapkan untuk di-copy langsung ke `## Action` section per plan. Honor post-research corrections C-01 (`AssessmentType` bukan `Type`), C-02 (Submit utama di ExamSummary.cshtml), C-03 (popup tetap muncul + submit paralel langsung).

> **Catatan koreksi C-01:** Field di model adalah `AssessmentType` (Models/AssessmentSession.cs:154 — verified). **WAJIB pakai constants** `AssessmentConstants.AssessmentType.{Online, PreTest, PostTest, Manual}` (Models/AssessmentConstants.cs:5-11). Jangan magic string.

---

## File Classification

| File | Type | Role | Data Flow | Closest Analog | Match Quality |
|------|------|------|-----------|----------------|---------------|
| `Controllers/CMPController.cs` (modified) | C# controller action | controller (HttpPost guard + AuditLog blocked write) | request-response (form POST → guard → 2-tier reject branch atau pass) | `Controllers/AssessmentAdminController.cs:5535-5612` (`EnsureCanDeleteAsync` Phase 312) + in-file `:1616-1628` (existing single-tier LIFE-03) | **exact** (helper extraction precedent + in-file existing block) |
| `Views/CMP/ExamSummary.cshtml` (modified) | Razor view | view (Submit button conditional render + retry handler JS) | client form POST + JS retry chain (D-10) | self `:106-145` (existing form + timerExpired conditional) + Phase 312 modal AJAX retry pattern (greenfield JS) | **exact** (in-file conditional pattern) |
| `Views/CMP/StartExam.cshtml` (modified) | Razor view | view (timer countdown handler + auto-submit fire flow) | client-side setInterval → modal popup → form submit | self `:440-481` (existing `updateTimer()` + `timeUpWarningModal` show + setTimeout) | **exact** (in-file modify, NOT new pattern) |
| `tests/e2e/exam-taking.spec.ts` (modified — append FLOW 313) | Playwright E2E spec | test (multi-scenario timer matrix + AuditLog verify via API) | request-response browser automation | `tests/e2e/assessment.spec.ts:584-757` (FLOW 12 Phase 312 — describe block + test.skip + dedicated fixture title) | **exact** (in-repo FLOW pattern carry-forward) |
| `.planning/seeds/313-timer-fixtures.sql` (created) | SQL seed script | seed/fixture (DB direct INSERT/UPDATE for back-dated StartedAt) | DB seed manipulation (D-07 + D-08) | (none — no precedent in repo) — analog komposit dari Phase 312 fixture title pattern (D-08) + existing DB schema | **role-match (komposit)** |
| `.planning/phases/313-.../313-UAT.md` (created) | Markdown UAT checklist | doc (manual UAT BI step-by-step) | n/a | `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` | **exact** (template precedent) |

> **Catatan:** Phase 313 RESEARCH.md menyebut potensi `KPB-PortalHC.Tests/Controllers/CMPControllerTests.cs` (unit test). **VERIFIED: tidak ada .NET test project di repo** (`ls KPB-PortalHC.Tests` = not found; Phase 312 confirmed "Backend test framework: None"). Skip unit test — coverage via Playwright + Manual UAT (Phase 312 Path B precedent).

---

## Pattern Assignments

### 1. `Controllers/CMPController.cs` (modified)

**Role:** controller HttpPost action + private helper extraction
**Data flow:** form POST `/CMP/SubmitExam` → existing auth/PrePost/incomplete checks UNCHANGED → **NEW guard call `EnsureCanSubmitExamAsync`** → existing grading flow UNCHANGED

**Analogs (in-repo, no need to read other files):**
- `Controllers/AssessmentAdminController.cs:5535-5612` — `EnsureCanDeleteAsync` (direct template untuk helper signature + try/catch swallow + TempData + RedirectToAction pattern)
- `Controllers/AssessmentAdminController.cs:2054-2063` — call site EnsureCanDeleteAsync (template untuk inline guard call)
- `Controllers/CMPController.cs:1616-1628` — existing LIFE-03 single-tier block (yang AKAN di-replace dengan helper call)
- `Controllers/CMPController.cs:1556` — `SubmitExam` signature (`isAutoSubmit = false` parameter sudah ada — verified)
- `Controllers/CMPController.cs:27-72` — DI fields tersedia: `_userManager`, `_auditLog`, `_logger` (semua sudah injected — A2/A3 confirmed; tidak perlu inject baru)
- `Services/AuditLogService.cs:21-42` — `LogAsync` signature stable (`actorUserId, actorName, actionType, description, targetId?, targetType?`)
- `Models/AssessmentConstants.cs:5-11` — `AssessmentType` constants (Online/PreTest/PostTest/Manual)

#### A. Method attribute pattern (preserved verbatim)

Source: `Controllers/CMPController.cs:1554-1556` [VERIFIED]

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitExam(int id, Dictionary<int, int> answers, bool isAutoSubmit = false)
```

> **Rule planner:** signature TIDAK diubah. `isAutoSubmit` parameter sudah exists. Guard hidup di body method (mirror Phase 312 D-04 pattern).

#### B. Existing LIFE-03 block (TARGET REPLACEMENT)

Source: `Controllers/CMPController.cs:1616-1628` [VERIFIED]

```csharp
// ---- Server-side timer enforcement (LIFE-03) ----
// Grace period: 2 minutes to account for network latency and slow connections.
// Skip check if StartedAt is null (legacy sessions that existed before Phase 21).
if (assessment.StartedAt.HasValue)
{
    var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
    int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0) + 2; // 2-minute grace
    if (elapsed.TotalMinutes > allowedMinutes)
    {
        TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
        return RedirectToAction("StartExam", new { id });
    }
}
```

> **Rule planner:** ganti seluruh blok `if (assessment.StartedAt.HasValue) { ... }` (line 1619-1628) dengan **call ke helper baru**:
> ```csharp
> // ---- Server-side timer enforcement (LIFE-03 + Phase 313 2-tier) ----
> var timerBlockResult = await EnsureCanSubmitExamAsync(assessment, isAutoSubmit);
> if (timerBlockResult != null) return timerBlockResult;
> ```
> Insert call **setelah `serverTimerExpired` calculation block (line 1580-1614)** dan **sebelum `packageAssignment` query (line 1631)**. Posisi sama persis dengan blok LIFE-03 yang diganti.

#### C. Helper signature & body (NEW — analog Phase 312 `EnsureCanDeleteAsync`)

Analog source: `Controllers/AssessmentAdminController.cs:5535-5612` [VERIFIED — 78 lines]

**Skeleton (planner finalisasi parameter set):**

```csharp
// Tempatkan sebagai private helper di akhir class CMPController, sebelum closing brace
// (mirror Phase 312 placement at line 5535 di AssessmentAdminController)
private async Task<IActionResult?> EnsureCanSubmitExamAsync(
    AssessmentSession assessment,
    bool isAutoSubmit)
{
    // D-15 / C-01: Manual type exclude via explicit field check (defense-in-depth)
    // Field: AssessmentSession.AssessmentType (Models/AssessmentSession.cs:154 verified)
    // Constants: Models/AssessmentConstants.cs:5-11
    if (assessment.AssessmentType != AssessmentConstants.AssessmentType.Online &&
        assessment.AssessmentType != AssessmentConstants.AssessmentType.PreTest &&
        assessment.AssessmentType != AssessmentConstants.AssessmentType.PostTest)
    {
        return null; // Manual type / null — skip guard, caller lanjut
    }

    // Legacy session: StartedAt null → skip (existing convention preserved)
    if (!assessment.StartedAt.HasValue) return null;

    var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
    int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
    int graceLimitMinutes = allowedMinutes + 2; // 2-minute grace untuk auto-submit (preserved)

    // Tier 1 (NEW Phase 313): manual reject tanpa grace (D-09 strict 0-grace)
    if (!isAutoSubmit && elapsed.TotalMinutes > allowedMinutes)
    {
        await WriteSubmitBlockedAuditAsync(assessment, elapsed, allowedMinutes);
        TempData["Error"] = "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman."; // D-01
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }

    // Tier 2 (existing LIFE-03 preserved): auto reject setelah grace
    if (elapsed.TotalMinutes > graceLimitMinutes)
    {
        TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses."; // existing copy preserved (D-06)
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }

    return null; // pass — caller lanjut grading
}
```

> **Rule planner / D-06:** Tier 2 message copy **preserved verbatim** dari existing line 1625 (jangan invent baru). Tier 1 message dari D-01 (Bahasa Indonesia, no PII).
>
> **Rule planner / Audit on Tier 2?** D-05 + D-06 spec hanya minta Blocked entry untuk **Tier 1 (manual after timeup)**. Tier 2 (auto-submit telat) **TIDAK** tulis Blocked entry — preserved existing behavior. Helper di atas hanya panggil `WriteSubmitBlockedAuditAsync` di Tier 1 path.

#### D. AuditLog blocked entry helper (NEW — analog Phase 312 try/catch swallow pattern)

Analog source: `Controllers/AssessmentAdminController.cs:5563-5605` [VERIFIED]

```csharp
// Tempatkan sebagai private helper di akhir class CMPController, dekat EnsureCanSubmitExamAsync
private async Task WriteSubmitBlockedAuditAsync(
    AssessmentSession assessment,
    TimeSpan elapsed,
    int allowedMinutes)
{
    try
    {
        var blockUser = await _userManager.GetUserAsync(User);
        // Pattern actor name: NIP + FullName (Phase 312 `EnsureCanDeleteAsync` line 5567-5570)
        var blockActor = string.IsNullOrWhiteSpace(blockUser?.NIP)
            ? (blockUser?.FullName ?? "Unknown")
            : $"{blockUser.NIP} - {blockUser.FullName}";

        // Description format: D-05 spec
        // "HC/User role manual submit blocked after timeup. Type={...} ElapsedMin={X} AllowedMin={Y} SessionId={id}"
        var description =
            $"HC/User role manual submit blocked after timeup. " +
            $"Type={assessment.AssessmentType} " +
            $"ElapsedMin={(int)elapsed.TotalMinutes} " +
            $"AllowedMin={allowedMinutes} " +
            $"SessionId={assessment.Id}";

        await _auditLog.LogAsync(
            actorUserId: blockUser?.Id ?? "",
            actorName: blockActor,
            actionType: "SubmitExamBlocked",   // D-05 — Phase 312 {Action}Blocked convention
            description: description,
            targetId: assessment.Id,
            targetType: "AssessmentSession");
    }
    catch (Exception auditEx)
    {
        // Swallow — audit failure tidak boleh block primary action (Phase 312 T-306-02 precedent)
        _logger.LogWarning(auditEx,
            "AuditLog SubmitExamBlocked write failed for SessionId={SessionId}",
            assessment.Id);
    }
}
```

> **Rule planner / DI fields:**
> - `_userManager` (line 27) — verified
> - `_auditLog` (line 32) — verified
> - `_logger` (line 34, type `ILogger<CMPController>`) — verified
>
> Semua sudah injected — TIDAK perlu modify constructor. (Lihat `Controllers/CMPController.cs:42-72` constructor body.)
>
> **Field name confirmation:** `ApplicationUser.FullName` (verified line 13) dan `NIP` (referenced di Phase 312 helper line 5568) — pakai field name yang sama persis.

#### E. NO transaction wrapper (intentional deviation from Phase 312 WR-01)

Source rationale: CONTEXT.md `code_context > Patterns to Avoid > TOCTOU race`

Phase 312 `EnsureCanDeleteAsync` di-wrap dalam `BeginTransactionAsync` karena ada cascade DELETE yang perlu rollback. **Phase 313 guard adalah read-only check + audit write only** — tidak ada state mutation di guard itself. Transaction wrap tidak perlu.

> **Rule planner:** **JANGAN tambah `using var tx = await _context.Database.BeginTransactionAsync()`** wrap di sekitar guard call. Existing transactional context downstream (GradingService) sudah handle write path. Audit Blocked entry idempotency tidak critical (multiple entries OK — informational, query downstream bisa dedupe).

---

### 2. `Views/CMP/ExamSummary.cshtml` (modified)

**Role:** Razor view (modify Submit button block + append retry JS handler section)
**Data flow:** Razor render conditional disabled → form POST `/CMP/SubmitExam` (existing) → optional D-10 fetch retry JS chain

**Analogs (in-file + cross-view):**
- `Views/CMP/ExamSummary.cshtml:107-114` — existing form + `isAutoSubmit` hidden field tied ke `timerExpired` ViewBag (verified — **JANGAN diubah**)
- `Views/CMP/ExamSummary.cshtml:117-143` — existing 3-branch conditional render (`timerExpired` / `unanswered>0 && !timerExpired` / `else submit`) — **TARGET REFINE**
- Existing pattern: spinner + disabled state — RESEARCH.md sudah produce skeleton (lihat Pattern 3 di research line 277-296)

#### A. Existing button conditional structure (TARGET REFINE)

Source: `Views/CMP/ExamSummary.cshtml:107-145` [VERIFIED full Read]

```razor
<!-- Final submit form -->
<form asp-action="SubmitExam" method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@assessmentId" />
    <input type="hidden" name="isAutoSubmit" value="@(timerExpired ? "true" : "false")" id="autoSubmitFlag" />
    @foreach (var kvp in answers)
    {
        <input type="hidden" name="answers[@kvp.Key]" value="@kvp.Value" />
    }

    <div class="d-flex justify-content-between align-items-center">
        @if (timerExpired)
        {
            <span class="text-danger fw-semibold">
                <i class="bi bi-clock-fill me-1"></i>Waktu habis — ujian harus dikumpulkan
            </span>
        }
        else
        {
            <a asp-action="StartExam" asp-route-id="@assessmentId" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i>Kembali ke Ujian
            </a>
        }

        @if (unanswered > 0 && !timerExpired)
        {
            <button type="button" class="btn btn-success btn-lg fw-bold" disabled
                    title="Jawab semua soal terlebih dahulu">
                <i class="bi bi-check-circle-fill me-2"></i>Kumpulkan Ujian
            </button>
        }
        else
        {
            <button type="submit" class="btn btn-success btn-lg fw-bold"
                    onclick="return confirm('Kumpulkan ujian? Tindakan ini tidak dapat dibatalkan.')">
                <i class="bi bi-check-circle-fill me-2"></i>Kumpulkan Ujian
            </button>
        }
    </div>
</form>
```

> **Rule planner / D-03 + C-02:** modifikasi hanya cabang **"else (timerExpired || unanswered=0)"** untuk **branch ketika `timerExpired == true`**. Existing `timerExpired ? "true" : "false"` hidden field (line 110) **PRESERVED** — sudah benar.
>
> **Pattern refine (D-03):**
> - Saat `timerExpired == true`: render **disabled button greyed-out** dengan label `"Waktu Habis - Submit Otomatis Berjalan..."` + spinner kecil + tooltip
> - Saat `unanswered > 0 && !timerExpired`: existing disabled + "Jawab semua soal terlebih dahulu" — preserved
> - Saat `!timerExpired && unanswered == 0`: existing submit + confirm — preserved

**Replacement skeleton (insert ke line ~130-143):**

```razor
@if (timerExpired)
{
    <button type="button" class="btn btn-secondary btn-lg fw-bold" disabled
            id="manualSubmitDisabledBtn"
            title="Auto-submit sedang berjalan, mohon tunggu">
        <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        Waktu Habis - Submit Otomatis Berjalan...
    </button>
}
else if (unanswered > 0)
{
    <button type="button" class="btn btn-success btn-lg fw-bold" disabled
            title="Jawab semua soal terlebih dahulu">
        <i class="bi bi-check-circle-fill me-2"></i>Kumpulkan Ujian
    </button>
}
else
{
    <button type="submit" class="btn btn-success btn-lg fw-bold"
            onclick="return confirm('Kumpulkan ujian? Tindakan ini tidak dapat dibatalkan.')">
        <i class="bi bi-check-circle-fill me-2"></i>Kumpulkan Ujian
    </button>
}
```

> **Rule planner / D-13 banner:** kalau `timerExpired == true` AND user landing fresh di ExamSummary (bukan dari redirect StartExam), pertimbangkan tambah banner info di atas form: `<div class="alert alert-info"><i class="bi bi-info-circle me-2"></i>Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan.</div>`. **Optional — Claude discretion** (CONTEXT.md `Claude's Discretion` "Banner styling").

#### B. Retry JS handler (D-10 — greenfield)

**Analog:** None in-repo. Pattern derived dari Pattern 3 RESEARCH.md (line 298-323) + Phase 312 modal AJAX `fetch().then().catch()` style (`Views/Admin/AssessmentMonitoringDetail.cshtml:937-962`).

**Apply for `@section Scripts { ... }` block (append ke ExamSummary.cshtml — section saat ini TIDAK exist, perlu tambah):**

```html
@section Scripts {
<script>
    (function () {
        // D-10: auto-submit retry 3x exponential backoff [1s, 2s, 4s]
        // Hanya aktif kalau timerExpired (form auto-fired dari StartExam.cshtml redirect chain)
        var timerExpired = @Json.Serialize(ViewBag.TimerExpired as bool? ?? false);
        if (!timerExpired) return; // happy path — pakai native form submit + confirm

        var form = document.querySelector('form[action$="/SubmitExam"]');
        if (!form) return;

        var maxAttempts = 3;
        var delays = [1000, 2000, 4000];
        var attempt = 0;
        var submitted = false;

        function attemptSubmit() {
            attempt++;
            var fd = new FormData(form);
            return fetch(form.action, { method: 'POST', body: fd, redirect: 'follow' })
                .then(function (r) {
                    if (r.redirected) { window.location.href = r.url; return; }
                    if (!r.ok) throw new Error('HTTP ' + r.status);
                    // Server returned non-redirect 2xx — fallback: reload to current
                    window.location.reload();
                })
                .catch(function (err) {
                    console.error('[Phase 313] Submit attempt ' + attempt + ' failed', err);
                    if (attempt < maxAttempts) {
                        setTimeout(attemptSubmit, delays[attempt - 1]);
                    } else {
                        showRetryFailBanner();
                    }
                });
        }

        function showRetryFailBanner() {
            // D-11: banner permanent fallback (server-side last-resort save deferred ke v16.0+)
            var container = document.querySelector('.container.py-4');
            if (!container) return;
            var banner = document.createElement('div');
            banner.className = 'alert alert-warning fw-semibold';
            banner.innerHTML =
                '<i class="bi bi-exclamation-triangle-fill me-2"></i>' +
                'Submit gagal karena masalah jaringan. Hubungi admin. ' +
                'Jawaban Anda tersimpan di tab ini, jangan tutup browser.';
            container.prepend(banner);
        }

        // Auto-fire on page-ready kalau timerExpired (chain dari StartExam)
        if (document.readyState === 'complete') {
            attemptSubmit();
        } else {
            window.addEventListener('load', attemptSubmit);
        }
    })();
</script>
}
```

> **Rule planner / Pitfall 4 (Phase 312 WR-02 path-prefix `appUrl()`):** Form `action` attribute sudah di-render oleh Razor `asp-action="SubmitExam"` → ASP.NET Core auto-prefix base path (path-prefix safe). JS pakai `form.action` (DOM property) yang return absolute URL — **tidak perlu `appUrl()` helper**. Hindari hardcode `/CMP/SubmitExam`.
>
> **Rule planner / Anti-pattern hardcoded URL:** kalau perlu URL endpoint lain di JS, pakai `data-url` attribute via `@Url.Action(...)` injection dari Razor (Phase 312 WR-02 mitigation pattern).

---

### 3. `Views/CMP/StartExam.cshtml` (modified)

**Role:** Razor view (modify timer countdown auto-submit flow + modal text)
**Data flow:** client-side `setInterval(updateTimer, 1000)` → countdown=0 → modal popup show + form submit fire **paralel** (C-03)

**Analogs (in-file only):**
- `Views/CMP/StartExam.cshtml:376-391` — existing `timeUpWarningModal` markup (TARGET MODIFY: hapus button OK, ganti dengan spinner indicator)
- `Views/CMP/StartExam.cshtml:440-481` — existing `updateTimer()` function dengan modal show + setTimeout 10s (TARGET MODIFY: hapus setTimeout 10s, fire submit paralel)
- `Views/CMP/StartExam.cshtml:71` — existing `examForm` submit ke ExamSummary (PRESERVED — bukan ke SubmitExam)
- `Views/CMP/StartExam.cshtml:441` — existing `submitted` flag (PRESERVED — guard double-submit)

#### A. Existing modal markup (TARGET MODIFY per C-03)

Source: `Views/CMP/StartExam.cshtml:376-391` [VERIFIED]

```razor
<!-- Time up warning modal (D-08: tampil sebelum auto-submit saat timer habis) -->
<div class="modal fade" id="timeUpWarningModal" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-hidden="true"
     style="z-index: 9999;">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-body text-center p-4">
                <i class="bi bi-clock-history text-danger" style="font-size:3rem"></i>
                <h5 class="mt-3 fw-bold text-danger">Waktu Habis!</h5>
                <p class="text-muted">Waktu pengerjaan ujian telah habis. Jawaban Anda akan dikirimkan secara otomatis.</p>
                <button type="button" class="btn btn-danger px-4 fw-bold" id="timeUpOkBtn">
                    <i class="bi bi-send me-1"></i>OK — Kirim Jawaban
                </button>
            </div>
        </div>
    </div>
</div>
```

> **Rule planner / C-03:** **Modal tetap muncul** untuk awareness, **TAPI** hapus button "OK — Kirim Jawaban", ganti dengan **disabled spinner indicator**. Body text adjust ke "Jawaban Anda sedang dikirim otomatis...". `data-bs-backdrop="static"` + `data-bs-keyboard="false"` PRESERVED (user tidak bisa close modal sampai redirect).

**Replacement skeleton:**

```razor
<!-- Phase 313 C-03: modal tetap muncul (info-only), submit fire paralel langsung -->
<div class="modal fade" id="timeUpWarningModal" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-hidden="true"
     style="z-index: 9999;">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-body text-center p-4">
                <i class="bi bi-clock-history text-danger" style="font-size:3rem"></i>
                <h5 class="mt-3 fw-bold text-danger">Waktu Habis!</h5>
                <p class="text-muted mb-3">Jawaban Anda sedang dikirim otomatis. Mohon tunggu, jangan refresh halaman.</p>
                <div class="d-flex justify-content-center align-items-center gap-2">
                    <span class="spinner-border text-danger" role="status" aria-hidden="true"></span>
                    <span class="fw-semibold text-danger">Mengirim...</span>
                </div>
            </div>
        </div>
    </div>
</div>
```

#### B. Existing auto-submit timer flow (TARGET MODIFY per C-03)

Source: `Views/CMP/StartExam.cshtml:462-478` [VERIFIED]

```javascript
if (remaining <= 0) {
    clearInterval(timerInterval);
    clearInterval(saveInterval);
    window.onbeforeunload = null;
    // D-08: tampilkan warning modal dulu, bukan langsung submit
    if (!submitted) {
        var timeupModal = new bootstrap.Modal(document.getElementById('timeUpWarningModal'));
        timeupModal.show();
        // Auto-submit setelah 10 detik jika user tidak klik OK
        setTimeout(function() {
            if (!submitted) {
                submitted = true;
                document.getElementById('examForm').submit();
            }
        }, 10000);
    }
}
```

> **Rule planner / C-03:** **HAPUS `setTimeout(..., 10000)` wrapper.** Fire `examForm.submit()` **langsung paralel** setelah `timeupModal.show()`. Modal tetap muncul untuk awareness, tapi tidak block submit chain. Server grace 2min (Tier 2) tetap intact untuk cover network latency.

**Replacement skeleton:**

```javascript
if (remaining <= 0) {
    clearInterval(timerInterval);
    clearInterval(saveInterval);
    window.onbeforeunload = null;
    // Phase 313 C-03: modal muncul (info awareness) + submit fire PARALEL langsung (no setTimeout delay)
    if (!submitted) {
        submitted = true;
        var timeupModal = new bootstrap.Modal(document.getElementById('timeUpWarningModal'));
        timeupModal.show();
        // Fire submit immediate — modal stays visible during POST → ExamSummary chain
        document.getElementById('examForm').submit();
    }
}
```

> **Catatan submitted flag:** set `submitted = true` SEBELUM modal show untuk prevent race re-trigger dari `visibilitychange` listener (line 487-498). Existing flag pattern preserved.

#### C. TempData rendering — VERIFIED (Pitfall 3 mitigated)

Source: `Views/Shared/_Layout.cshtml:199-224` [VERIFIED via grep]

```razor
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show">
        <strong>Error:</strong> @TempData["Error"]
        ...
    </div>
}
@if (TempData["Info"] != null) { ... }
@if (TempData["Success"] != null) { ... }
```

> **Rule planner:** TempData["Error"] **sudah di-render global di `_Layout.cshtml`**. Tier-1 reject redirect ke StartExam → banner muncul automatically. **TIDAK perlu tambah `@if` block manual di StartExam.cshtml** (Pitfall 3 di RESEARCH.md sudah mitigated by existing layout). Assumption A4 RESEARCH.md ↦ resolved LOW risk.

---

### 4. `tests/e2e/exam-taking.spec.ts` (modified — append FLOW 313)

**Role:** Playwright E2E spec (append `FLOW 313` describe block)
**Data flow:** browser → login coachee → DB seed back-dated StartedAt fixture → navigate StartExam/ExamSummary → assert button state + redirect + AuditLog via API

**Analogs (in-repo):**
- `tests/e2e/assessment.spec.ts:584-757` — FLOW 12 Phase 312 (describe block + 6 tests + dedicated fixture title + `test.skip` graceful) — **direct template**
- `tests/helpers/auth.ts:4-11` — `login(page, 'coachee' | 'hc' | 'admin')` (verified)
- `tests/helpers/accounts.ts:4` — `coachee: rino.prasetyo@pertamina.com / 123456` (verified — sesuai memory `reference_dev_credentials.md`)
- `tests/helpers/utils.ts:4-31` — `tomorrow`, `today`, `uniqueTitle`, `waitForNav`, `autoConfirm` (verified)

#### A. Describe block boilerplate (analog FLOW 12)

Source: `tests/e2e/assessment.spec.ts:584-595` [VERIFIED]

```ts
// ============================================================
// FLOW 12: Phase 312 — Admin Full-Delete Role Guard
// REQ: DEL-01 (6 success criteria)
// 12.0 — GetDeleteImpact JSON helper test (smoke endpoint)
// 12.1 — Admin + Open + 0 response → DELETE OK
// ...
// ============================================================
test.describe('Assessment - Phase 312 Admin Full-Delete Role Guard', () => {
  // tidak pakai beforeEach login — tiap test login berbeda role
});
```

**Apply for FLOW 313 (target file: `tests/e2e/exam-taking.spec.ts` — append):**

```ts
// ============================================================
// FLOW 313: Phase 313 — Block Manual Submit Saat Waktu Habis
// REQ: TMR-01 (7 success criteria)
// 313.1 — Manual + before-time + Online → submit OK (regression)
// 313.2 — Manual + after-time (in grace) + Online → BLOCKED + AuditLog SubmitExamBlocked + redirect
// 313.3 — Auto + after-time (in grace) + Online → submit OK (Tier 2 grace covers)
// 313.4 — Auto + after-grace + Online → BLOCKED Tier 2 (existing preserved)
// 313.5 — Manual + after-time + PreTest → BLOCKED (3 timer types verify)
// 313.6 — Manual + after-time + PostTest → BLOCKED
// 313.7 — Manual + after-time + Manual type → submit OK (D-15 exclude verify)
// ============================================================
test.describe('Exam Taking - Phase 313 Block Manual Submit', () => {
  // login coachee per-test (semua sebagai worker)
});
```

#### B. Login + dedicated fixture title pattern (D-08, analog FLOW 12.5)

Source: `tests/e2e/assessment.spec.ts:721-742` [VERIFIED]

```ts
test('12.5 - HC + Open + has-response → modal opens, submit BLOCKED + flash error', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    // Asumsi: ada seed row Open dengan responseCount>0 — title fixture
    const targetRow = page.locator('tr', { hasText: 'Phase 312 HC Block Fixture' }).first();
    if (await targetRow.count() === 0) test.skip(true, 'Seed "Phase 312 HC Block Fixture" tidak ditemukan — Wave 1 manual seed required');

    // ... assertions ...
});
```

**Apply for 313.2 (analog template):**

```ts
test('313.2 - Manual + after-time (in grace) + Online → BLOCKED + AuditLog SubmitExamBlocked', async ({ page }) => {
    await login(page, 'coachee');

    // D-08: dedicated fixture title pattern (mirror Phase 312 WR-04)
    const fixtureTitle = 'Phase 313 Timer Fixture Online ManualAfterGrace';
    await page.goto('/CMP/Assessment');
    const targetRow = page.locator('tr', { hasText: fixtureTitle }).first();
    if (await targetRow.count() === 0) {
        test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Wave 0 manual seed required (jalankan .planning/seeds/313-timer-fixtures.sql)`);
    }

    // Navigate ke StartExam — server back-dated StartedAt akan trigger expired state
    // (atau langsung POST /CMP/SubmitExam dengan isAutoSubmit=false via page.request)
    // Detail flow: planner finalisasi sesuai UAT script

    // Assert: redirect ke StartExam dengan TempData banner D-01
    await expect(page).toHaveURL(/\/CMP\/StartExam/);
    await expect(page.locator('.alert-danger')).toContainText('Waktu ujian Anda sudah habis');

    // Optional: AuditLog spot-check via DB SQL (di Manual UAT — Playwright API non-trivial)
});
```

> **Rule planner / Pitfall 5 (Phase 312 WR-03 selector substring):** `tr:has-text("Phase 313 Timer Fixture Online ManualAfterGrace")` rentan substring match kalau ada title lain mengandung prefix sama. Pakai **status badge-scoped selector** atau **regex exact match** `hasText: /^Phase 313 Timer Fixture Online ManualAfterGrace$/`. Mirror Phase 312 WR-03 mitigation.

#### C. test.skip graceful pattern (Wave 0 fixture missing)

Source: `tests/e2e/assessment.spec.ts:602-604, 634-636, 728` [VERIFIED]

```ts
const fixtureTitle = 'Phase 313 Timer Fixture {Type} {Scenario}';
const targetRow = page.locator('tr', { hasText: fixtureTitle }).first();
if (await targetRow.count() === 0) {
    test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Wave 0 manual seed required`);
}
```

> **Rule planner:** **SEMUA 7 test 313.x WAJIB pakai `test.skip` graceful** untuk Wave 0 RED state (sebelum SQL seed dijalankan). Pattern Phase 312 — Playwright supplemental, manual UAT primary.

#### D. NO `beforeEach` login (per-test login pattern)

> **Rule planner:** Mirror FLOW 12 (line 595 — comment "tidak pakai beforeEach"). Test 313.x semua login `coachee` (worker role) karena scenario worker-side. Hanya bedanya per-test fixture berbeda.
>
> Catatan: kalau perlu role HC untuk verify AuditLog read (out-of-scope karena audit verify via SQL Manual UAT), bisa pakai `await login(page, 'hc')` — pattern existing.

---

### 5. `.planning/seeds/313-timer-fixtures.sql` (created)

**Role:** SQL seed script untuk Wave 0 setup (D-08 Claude discretion)
**Data flow:** SSMS / sqlcmd → INSERT/UPDATE 7 fixture: 6 timer matrix (3 types × manual/auto for blocked scenarios) + 1 Manual-type for D-15 exclude verification
**Analog:** No precedent (`.planning/seeds/` directory baru). Komposit dari:
- D-08 fixture title pattern: `Phase 313 Timer Fixture {Type} {Scenario}`
- D-07 back-dated StartedAt strategy
- Existing schema reference: `Models/AssessmentSession.cs` (verified field names)

**Skeleton template:**

```sql
-- Phase 313 Timer Fixtures - Block Manual Submit Saat Waktu Habis
-- Run via SSMS atau sqlcmd. Idempotent: pakai IF NOT EXISTS pattern (atau wipe by title prefix dulu).
-- D-07: back-date StartedAt untuk trigger kondisi target tanpa real-time wait.
-- D-08: dedicated fixture title pattern.

-- ============================================================
-- Pre-condition: ada minimal 1 user dengan Role=Coachee (rino.prasetyo@pertamina.com)
-- + minimal 1 Schedule active. Adjust UserId/ScheduleId di-bawah sesuai env lokal.
-- ============================================================

DECLARE @UserId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE Email = 'rino.prasetyo@pertamina.com');
DECLARE @ScheduleId INT = (SELECT TOP 1 Id FROM Schedules ORDER BY Date DESC); -- adjust per env
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Skenario matrix: title fixture | AssessmentType | StartedAt offset | Expected
-- ManualBeforeTime  | Online   | NOW - 5 min                    | Submit OK
-- ManualAfterGrace  | Online   | NOW - (Duration + 1) min       | Tier-1 BLOCK
-- AutoInGrace       | Online   | NOW - (Duration + 1) min       | Submit OK (auto + in grace)
-- AutoAfterGrace    | Online   | NOW - (Duration + Extra + 5)   | Tier-2 BLOCK
-- ManualAfterGrace  | PreTest  | NOW - (Duration + 1) min       | Tier-1 BLOCK
-- ManualAfterGrace  | PostTest | NOW - (Duration + 1) min       | Tier-1 BLOCK
-- ManualAfterTime   | Manual   | NOW - (Duration + 100) min     | Submit OK (D-15 exclude)

-- Contoh insert 1 fixture (planner finalisasi 7 fixtures):
INSERT INTO AssessmentSessions (
    Title, Category, UserId, ScheduleId, AssessmentType,
    DurationMinutes, ExtraTimeMinutes, StartedAt, Status,
    CreatedAt, ...
) VALUES (
    'Phase 313 Timer Fixture Online ManualAfterGrace',
    'Test Phase 313',
    @UserId,
    @ScheduleId,
    'Online',
    60,                                                     -- DurationMinutes
    NULL,                                                   -- ExtraTimeMinutes
    DATEADD(MINUTE, -61, @Now),                             -- StartedAt back-dated → elapsed > allowed (60 min)
    'InProgress',                                           -- Status (per AssessmentConstants)
    @Now,
    ...
);
-- Repeat for 6 lainnya dengan offset + AssessmentType yang berbeda.

-- Verify:
SELECT Title, AssessmentType, StartedAt, DurationMinutes, ExtraTimeMinutes,
       DATEDIFF(MINUTE, StartedAt, SYSUTCDATETIME()) AS ElapsedMinutes
FROM AssessmentSessions
WHERE Title LIKE 'Phase 313 Timer Fixture%'
ORDER BY Title;
```

> **Rule planner:**
> - Schema field nama (verified `Models/AssessmentSession.cs`): `Title`, `Category`, `UserId`, `ScheduleId`, `AssessmentType` (line 154), `DurationMinutes` (line 19), `ExtraTimeMinutes` (line 192), `StartedAt` (line 40), `Status`. Field lain (CreatedAt, etc.) — planner cek model file lengkap untuk required vs nullable.
> - **Anti-pattern A6 (Phase 309 — direct DB UserId=NULL update blocked by FK):** JANGAN set `UserId=NULL`. Pakai `UserId` valid dari coachee fixture. Mirror Phase 309 mitigation pattern.
> - **Idempotency:** boleh prepend `DELETE FROM AssessmentSessions WHERE Title LIKE 'Phase 313 Timer Fixture%'; ` sebelum INSERT. ATAU pakai `IF NOT EXISTS` guard. Planner pilih.
> - **Cascade FK:** kalau seed butuh PackageUserResponses dummy (untuk SC #5 verify session has responses), tambah INSERT separate setelah session insert. Optional — D-08 hanya butuh AssessmentSessions row dengan back-dated StartedAt.

---

### 6. `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md` (created)

**Role:** Manual UAT checklist Bahasa Indonesia (mandatory per Phase 312 precedent)
**Template precedent:** `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` [VERIFIED]

#### A. Frontmatter + header (analog 312)

Source: `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md:1-9`

```markdown
# Phase 312 — Manual UAT Script

**Phase:** 312-admin-full-delete-assessment-room
**REQ:** DEL-01 (Admin Full-Delete Assessment Room — role tier guard di body method + UI conditional render)
**Created:** 2026-05-07
**Mandatory before:** `/gsd-verify-work` Phase 312

> Manual UAT cover 6 skenario smoke (5 dari ROADMAP SC #6 + 1 extra D-04 untuk Pre-Post path). ...
```

**Apply for 313:**

```markdown
# Phase 313 — Manual UAT Script

**Phase:** 313-block-manual-submit-saat-waktu-habis
**REQ:** TMR-01 (Server menolak manual submit saat elapsed > Duration+ExtraTime; auto-submit grace 2min preserved; UI disable button + AuditLog blocked)
**Created:** 2026-05-08
**Mandatory before:** `/gsd-verify-work` Phase 313

> Manual UAT cover 7 skenario smoke (6 timer matrix manual/auto × before/in-grace/after-grace + 1 Manual-type exclude verify D-15). Eksekusi di environment lokal (`http://localhost:5277/CMP/StartExam/{id}`). UAT di Dev/Prod adalah tanggung jawab Team IT (per CLAUDE.md DEV_WORKFLOW). Wave 0 fixtures via `.planning/seeds/313-timer-fixtures.sql`.
```

#### B. Coverage Matrix (analog 312 line 10-19)

Source: `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md:11-19`

```markdown
| Step | Skenario | Role | Status seed | ResponseCount | Expected Result | Sign-off |
|------|----------|------|-------------|---------------|-----------------|----------|
| 1 | Admin + Open + 0 response | Admin | Open | 0 | Modal terbuka, ... | ⬜ |
```

**Apply for 313 (7 row matrix):**

```markdown
| Step | Skenario | AssessmentType | isAutoSubmit | StartedAt offset | Expected Result | Sign-off |
|------|----------|----------------|--------------|-------------------|-----------------|----------|
| 1 | Manual + before-time | Online | false | NOW - 5 min | Submit OK → grading + redirect Results | ⬜ |
| 2 | Manual + after-time (in grace) | Online | false | NOW - 61 min (Duration=60) | **Tier-1 BLOCK** → TempData D-01 + redirect StartExam + AuditLog `SubmitExamBlocked` (Type=Online) | ⬜ |
| 3 | Auto + after-time (in grace) | Online | true | NOW - 61 min | Submit OK (Tier-2 grace covers) → grading + redirect Results | ⬜ |
| 4 | Auto + after-grace | Online | true | NOW - 67 min (Duration+Extra+5) | **Tier-2 BLOCK** (existing preserved) → TempData "Pengiriman jawaban tidak dapat diproses" + redirect StartExam | ⬜ |
| 5 | Manual + after-time + PreTest | PreTest | false | NOW - 61 min | **Tier-1 BLOCK** + AuditLog Type=PreTest | ⬜ |
| 6 | Manual + after-time + PostTest | PostTest | false | NOW - 61 min | **Tier-1 BLOCK** + AuditLog Type=PostTest | ⬜ |
| 7 | Manual + after-time + Manual type (D-15 exclude) | Manual | false | NOW - 161 min | Submit OK (no guard apply) → grading + redirect Results | ⬜ |
```

#### C. Pre-conditions (analog 312 line 21-30)

Apply for 313:

```markdown
## Pre-conditions

- App di-build & running lokal: `dotnet build` + `dotnet run` (cek `http://localhost:5277/`)
- Login fixture: **Coachee:** `rino.prasetyo@pertamina.com / 123456` (per `tests/helpers/accounts.ts:4`)
- Wave 0 fixtures: jalankan `.planning/seeds/313-timer-fixtures.sql` di SSMS / sqlcmd terhadap DB lokal
- Browser modern + DevTools (Network tab — verify POST `/CMP/SubmitExam` redirect status)
- DB akses (SSMS / DBeaver) untuk verify AuditLog post-action via SQL spot-check
```

#### D. Step format + DB SQL spot-check (analog 312 line 32-56)

Source: `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md:32-56`

Apply for Step 2 (Tier-1 manual block):

```markdown
## Step 2 — Manual + after-time (in grace) + Online → Tier-1 BLOCK

**Pre-condition:** Login coachee, fixture `Phase 313 Timer Fixture Online ManualAfterGrace` ada (StartedAt back-dated 61 min ago, Duration=60).

**Action:**
1. Navigasi ke `/CMP/Assessment`, pilih fixture row.
2. Klik "Mulai Ujian" → masuk StartExam (timer akan langsung tampil 00:00 atau negative — server compute D-12).
3. Modal `timeUpWarningModal` muncul (info-only spinner, no OK button — C-03).
4. Form auto-submit fire paralel ke ExamSummary.
5. Di ExamSummary, klik **manual** Submit "Kumpulkan Ujian" (simulasi user race click — actually disabled per D-03; tester pakai DevTools `removeAttribute('disabled')` untuk force klik).
6. POST `/CMP/SubmitExam` dengan `isAutoSubmit=false`.

**Expect:**
- Redirect ke `/CMP/StartExam/{id}`
- Banner danger: "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman."
- DB query:
  ```sql
  SELECT TOP 5 ActionType, Description, TargetId, CreatedAt
  FROM AuditLogs
  WHERE ActionType = 'SubmitExamBlocked'
  ORDER BY CreatedAt DESC;
  ```
  - 1+ row dengan `Description LIKE '%SessionId={fixtureId}%'` AND contains `Type=Online`, `ElapsedMin=`, `AllowedMin=`
  - `TargetType = 'AssessmentSession'`

**Result:** ⬜ PASS / ⬜ FAIL — _catatan:_ ___________
```

> **Rule planner:** semua 7 step pakai format identik dengan **DB SQL block** untuk AuditLog verify (SC #5 acceptance). Step 7 Manual exclude → assert TIDAK ada AuditLog `SubmitExamBlocked` entry baru (negative assertion).

---

## Shared Patterns

### Authentication / Authorization (PRESERVED — DON'T TOUCH)

**Source:** `Controllers/CMPController.cs:1554-1569` [VERIFIED]
**Apply to:** SubmitExam attribute + auth check unchanged
- `[HttpPost] [ValidateAntiForgeryToken]` (line 1554-1555)
- `_userManager.GetUserAsync(User)` + owner/Admin/HC role check (line 1564-1569)

### Error Handling (TempData + RedirectToAction)

**Source:** `Controllers/CMPController.cs:1625-1626` (existing tier-2) [VERIFIED]
**Apply to:** Tier-1 + Tier-2 guard returns
```csharp
TempData["Error"] = "...";  // Bahasa Indonesia per CLAUDE.md
return RedirectToAction("StartExam", new { id = assessment.Id });
```

> Layout `_Layout.cshtml:199-208` auto-render TempData["Error"] — Pitfall 3 mitigated.

### AuditLog Service Call (Phase 312 try/catch swallow pattern)

**Source:** `Controllers/AssessmentAdminController.cs:5563-5605` [VERIFIED] + `Services/AuditLogService.cs:21` [VERIFIED]
**Signature:** `LogAsync(actorUserId, actorName, actionType, description, targetId, targetType)`
**Apply to:** Tier-1 blocked path **only** (D-06 — tier-2 + success path tidak modify AuditLog)

```csharp
try { await _auditLog.LogAsync(...); }
catch (Exception auditEx) { _logger.LogWarning(auditEx, "..."); }  // swallow — audit failure jangan break user flow
```

### Constants Usage (anti-pattern: hardcoded magic string)

**Source:** `Models/AssessmentConstants.cs:5-11` [VERIFIED]
**Apply to:** semua `assessment.AssessmentType` comparison
```csharp
assessment.AssessmentType == AssessmentConstants.AssessmentType.Online
// JANGAN: assessment.AssessmentType == "Online"  ← magic string anti-pattern
```

### DI Field Reuse (NO new constructor injection needed)

**Source:** `Controllers/CMPController.cs:27-72` [VERIFIED]
**Available fields:**
- `_userManager` (UserManager<ApplicationUser>) — line 27
- `_context` (ApplicationDbContext) — line 30
- `_auditLog` (AuditLogService) — line 32
- `_logger` (ILogger<CMPController>) — line 34

> **Rule planner:** TIDAK perlu modify constructor. A2/A3 RESEARCH.md ↦ resolved (LOW risk → CONFIRMED).

### Server Time Source (`DateTime.UtcNow`)

**Source:** `Controllers/CMPController.cs:1583, 1621` [VERIFIED]
**Apply to:** elapsed calculation di guard helper. JANGAN inject IDateTimeProvider (deferred per CONTEXT.md `## Deferred Ideas`).

### Anti-Pattern Reminders (Phase 312 carry-forward)

| Anti-pattern | Phase 312 ref | Phase 313 risk | Mitigation |
|--------------|---------------|----------------|------------|
| TOCTOU race (BeginTransactionAsync wrap) | WR-01 | LOW — guard read-only + audit write only, no state mutation | NO transaction wrap (intentional deviation) |
| Path-prefix `appUrl()` bug | WR-02 | MEDIUM — JS retry handler URL injection | Pakai `form.action` DOM property atau `data-url` via `@Url.Action()` |
| Playwright selector substring match | WR-03 | MEDIUM — fixture title prefix collision | Pakai regex exact `hasText: /^Phase 313 Timer Fixture {...}$/` atau status badge-scoped |
| Direct DB UserId=NULL FK violation | Phase 309 | MEDIUM — seed script fixture | Pakai valid `UserId` dari coachee subquery |
| Field name `Type` (typo) vs `AssessmentType` | C-01 RESEARCH | HIGH — build CS1061 fail | Pakai `AssessmentConstants.AssessmentType.*` constants |
| Modify wrong file (`StartExam.cshtml` Submit button) | C-02 RESEARCH | HIGH — UX bug | Submit utama di **ExamSummary.cshtml:130-143** (NOT StartExam) |
| `setTimeout(10000)` delay auto-submit | C-03 user override | HIGH — defeat phase purpose | Hapus setTimeout, fire submit paralel |

---

## No Analog Found

| File | Role | Reason |
|------|------|--------|
| `KPB-PortalHC.Tests/Controllers/CMPControllerTests.cs` | unit test | **Tidak ada .NET test project di repo** (verified `ls KPB-PortalHC.Tests` not found; Phase 312 confirmed). Phase 313 follow Phase 312 Path B precedent — coverage via Playwright + Manual UAT. **OUT OF SCOPE.** |

**Catatan partial precedent:**
- `.planning/seeds/313-timer-fixtures.sql` — directory `.planning/seeds/` baru. Pattern komposit dari D-08 fixture title + D-07 back-dated StartedAt + verified schema. Bukan greenfield, tapi tidak ada precedent SQL seed file di repo (Phase 312 fixture seed via UI manual, bukan SQL script).
- D-10 retry chain JS — greenfield (no in-repo retry pattern). Pattern fully spec'd di RESEARCH.md Pattern 3 + section ini.

---

## Metadata

**Analog search scope:**
- `Controllers/CMPController.cs` (full file scan: DI fields, SubmitExam method, existing LIFE-03 block)
- `Controllers/AssessmentAdminController.cs` (EnsureCanDeleteAsync helper :5535-5612, call sites :2054, :2198, :2327)
- `Services/AuditLogService.cs` (LogAsync signature)
- `Models/AssessmentSession.cs` (AssessmentType field name verify line 154)
- `Models/AssessmentConstants.cs` (constants verify line 5-11)
- `Models/ApplicationUser.cs` (FullName + NIP fields verify line 13)
- `Views/CMP/ExamSummary.cshtml` (full file: form, conditional render, hidden field)
- `Views/CMP/StartExam.cshtml` (line 1-200 + 370-500: examForm + timeUpWarningModal + updateTimer)
- `Views/Shared/_Layout.cshtml` (TempData rendering verify line 199-224)
- `tests/e2e/assessment.spec.ts` (FLOW 12 Phase 312 line 584-757)
- `tests/helpers/{auth,accounts,utils}.ts`
- `.planning/phases/312-admin-full-delete-assessment-room/{312-PATTERNS, 312-01-SUMMARY, 312-UAT}.md` (Phase 312 carry-forward references)

**Files scanned:** 15 (5 C# Models/Controllers/Services + 3 Razor views + 1 layout + 3 test helpers + 1 spec + 2 Phase 312 reference docs)

**Pattern extraction date:** 2026-05-08

---

*Phase: 313-block-manual-submit-saat-waktu-habis*
*PATTERNS drafted: 2026-05-08*
*Stack: ASP.NET Core MVC 8 + Razor + Bootstrap 5 + bootstrap-icons + Playwright 1.58.2 (TS)*
*Bahasa: Bahasa Indonesia (per CLAUDE.md)*
*Honors: C-01 (`AssessmentType` field) + C-02 (ExamSummary.cshtml primary) + C-03 (popup tetap muncul + submit paralel)*
