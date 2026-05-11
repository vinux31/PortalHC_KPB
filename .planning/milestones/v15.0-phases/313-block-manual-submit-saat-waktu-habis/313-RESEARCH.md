# Phase 313: Block Manual Submit Saat Waktu Habis - Research

**Researched:** 2026-05-08
**Domain:** ASP.NET Core MVC server-side timer enforcement + Razor frontend countdown handling + AuditLog blocked entry pattern + Playwright E2E timer scenario
**Confidence:** HIGH (semua claim utama VERIFIED dari codebase melalui Read/Grep)

## Summary

Phase 313 mengimplementasikan 2-tier branching server-side timer enforcement di `CMPController.SubmitExam()` (line 1619-1628 — single-tier saat ini) untuk menutup celah TMR-01: worker bisa manual submit setelah waktu habis dalam window grace 2 menit. Tier 1 (`!isAutoSubmit && elapsed > Duration+ExtraTime`) reject manual immediately. Tier 2 (`elapsed > Duration+ExtraTime+2min grace`) reject auto-submit telat (existing behavior preserved). Plus AuditLog blocked entry mengikuti pattern Phase 312 D-03 (`{Action}Blocked` ActionType). Plus frontend disable Submit button di **`Views/CMP/ExamSummary.cshtml`** (NOT StartExam.cshtml — verified) saat `timerExpired=true`. Plus retry 3x backoff JS handler greenfield.

**Key finding penting:** CONTEXT.md menyebut `assessment.Type` — field aktual di model `AssessmentSession` adalah **`AssessmentType`** (string nullable: `"PreTest"` | `"PostTest"` | `"Manual"` | `"Online"` | `null`). Constants tersedia di `Models/AssessmentConstants.cs::AssessmentType`. Submit button utama untuk SubmitExam berada di `ExamSummary.cshtml` line 139-143 (bukan StartExam — itu form ke ExamSummary).

**Primary recommendation:** Ekstrak helper privat `EnsureCanSubmitExamAsync(assessment, isAutoSubmit)` di CMPController sebagai analog `EnsureCanDeleteAsync` Phase 312, ditempatkan dekat blok LIFE-03 line 1616. Frontend modifikasi terbatas di ExamSummary.cshtml (button visual + retry handler) plus minor di StartExam.cshtml (auto-submit timeout di line 471 → enrich dengan retry/backoff). UAT-driven (Path B Phase 312), Playwright tests pakai dedicated fixture title pattern + `test.skip` graceful.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Manual reject UX (pesan + redirect):**
- **D-01:** Pesan TempData explanatory: `"Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman."`
- **D-02:** Redirect back ke `StartExam(id)` (konsisten dengan existing tier-2 behavior).

**Frontend disable visual saat countdown=0:**
- **D-03:** Button greyed-out + label "Waktu Habis - Submit Otomatis Berjalan..." + tooltip + spinner kecil (optional).
- **D-04:** Auto-submit fire immediate at countdown=0 (no delay 3-5s, server grace 2min sudah cover).

**AuditLog ActionType naming:**
- **D-05:** ActionType = `SubmitExamBlocked` (mengikuti Phase 312 `{Action}Blocked` convention). Description format: `"HC/User role manual submit blocked after timeup. Type={Online|PreTest|PostTest} ElapsedMin={X} AllowedMin={Y} SessionId={id}"`
- **D-06:** Success path TIDAK di-modify (scope minimal, hanya tambah blocked entry).

**E2E test seeding strategy:**
- **D-07:** DB manipulation `StartedAt` back-dated (no real-time wait, no IDateTimeProvider mock).
- **D-08:** Dedicated fixture title pattern: `"Phase 313 Timer Fixture {Type} {Scenario}"` (e.g. `"Phase 313 Timer Fixture Online ManualAfterGrace"`). Tests cari fixture, `test.skip` kalau tidak ada.

**Race condition (manual click di detik akhir):**
- **D-09:** Strict 0-grace untuk manual (per ROADMAP literal). Auto-submit fallback handle answer save.

**Network failure recovery:**
- **D-10:** Client retry 3x dengan exponential backoff (1s, 2s, 4s = total ~7s recovery). Logging error console + telemetry.
- **D-11:** Banner-only fallback kalau retry 3x semua fail dan grace habis: `"Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser."` (server-side last-resort save deferred ke v16.0+).

**Page reload behavior:**
- **D-12:** Recompute timer dari server `StartedAt` di page load. `remaining = (StartedAt + Duration + ExtraTime) - NOW`. Server adalah source-of-truth.
- **D-13:** Reload after timeup → button disabled + banner info (bukan error): `"Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan."` + frontend TIDAK fire auto-submit lagi (idempotency).

**3 timer type consistency:**
- **D-14:** Pesan, redirect, ActionType sama persis untuk Online/PreTest/PostTest. Type info di-embed di Description metadata, bukan di ActionType.
- **D-15:** Manual type exclude via explicit field check (defense-in-depth):
  ```csharp
  if (assessment.AssessmentType == "Online" || assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest") {
    // 2-tier guard
  }
  ```
  **NOTE:** CONTEXT.md menulis `assessment.Type` — field aktual adalah **`AssessmentType`** (verified `Models/AssessmentSession.cs:154`). Pakai `Models/AssessmentConstants.cs::AssessmentType.Online/PreTest/PostTest` constants.

### Claude's Discretion

- Tooltip wording detail untuk disabled button (D-03 spec hanya outline)
- Spinner/icon visual di disabled button label
- Banner styling untuk reload after timeup (D-13) dan retry-fail fallback (D-11) — Bootstrap alert-warning vs alert-info, exact icon
- Test fixture seed script lokasi & format (SQL script di `.planning/seeds/` atau Playwright setup helper)

### Deferred Ideas (OUT OF SCOPE)

- **Server-side last-resort save** (D-11 alternative) — auto-save endpoint untuk partial responses, defer ke milestone v16.0+
- **`IDateTimeProvider` mockable injection** untuk SubmitExam — clean refactor untuk testability, out-of-scope (DB manipulation StartedAt sudah cukup untuk E2E)
- **PreTest → PostTest landing redirect** (Area 8 alternative) — sub-phase di milestone berikut
- **AuditLog success entry dengan IsAutoSubmit field** — traceability detail submit manual vs auto, milestone v16.0+
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TMR-01 | Server menolak submit manual worker pada assessment ber-timer (Online, PreTest, PostTest) saat `elapsed > DurationMinutes + ExtraTimeMinutes`; hanya jalur auto-submit (`isAutoSubmit=true`) yang sah setelah waktu habis dengan grace 2 menit. Frontend disable tombol Submit saat countdown=0. Tipe Manual exclude. | (1) `Controllers/CMPController.cs:1619-1628` — current LIFE-03 single-tier block (verified). (2) `Controllers/CMPController.cs:1556` — `SubmitExam(int id, Dictionary<int,int> answers, bool isAutoSubmit = false)` signature (verified). (3) `Models/AssessmentSession.cs:154` — `AssessmentType` field (verified). (4) `Models/AssessmentConstants.cs:5-11` — `AssessmentType` constants (verified). (5) `Views/CMP/ExamSummary.cshtml:106-145` — Final submit form with `isAutoSubmit` hidden field tied to `timerExpired` ViewBag (verified). (6) Phase 312 `EnsureCanDeleteAsync` + `{Action}Blocked` AuditLog pattern (verified, `.planning/phases/312-.../312-01-SUMMARY.md:60-80`). |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| 2-tier timer enforcement (manual vs auto reject) | API/Backend (`CMPController.SubmitExam`) | — | Server adalah source-of-truth time (`DateTime.UtcNow`); enforcement harus tahan client-side bypass |
| Submit button disable saat timeup | Frontend (Razor + JS, `ExamSummary.cshtml`) | — | UX feedback realtime; defense-in-depth dengan backend block |
| Auto-submit fire immediate at countdown=0 | Frontend (`StartExam.cshtml:445-480`) | — | Sudah ada (timer interval + `submitForm.submit()`); tambah retry handler |
| AuditLog blocked entry | API/Backend (`AuditLogService.LogAsync`) | DB (AuditLogs table) | Audit trail butuh persisted record; service write via DbContext |
| Page reload remaining time recomputation | API/Backend (`CMPController.StartExam` GET) | Frontend (consume via `ViewBag.RemainingSeconds`) | Server compute, frontend display; sudah ada infrastructure (verified `StartExam.cshtml:410`) |
| Timer fixture seeding (StartedAt back-date) | DB Manipulation (test setup script) | E2E test (Playwright `tests/e2e/`) | DB direct UPDATE adalah pattern Phase 312 `test.skip` graceful — D-07 |
| Retry 3x backoff network resilience | Frontend JS (vanilla setTimeout chain) | — | Greenfield; tidak ada existing retry helper di codebase (D-10) |

## Standard Stack

### Core (Already in Use — No New Dependencies)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Controller `SubmitExam` action handler [VERIFIED: HcPortal.csproj `<TargetFramework>net8.0</TargetFramework>`] | Stack project; existing pattern di CMPController |
| Entity Framework Core | 8.0 | `_context.AssessmentSessions.FirstOrDefaultAsync` + `_context.AuditLogs.Add` [VERIFIED: line 1558, AuditLogService:40] | Sudah injected via DI |
| Razor View Engine | 8.0 | `ExamSummary.cshtml` conditional render disabled button [VERIFIED: line 117-143] | Existing pattern |
| Bootstrap 5 | (vendored) | `alert-warning`/`alert-info` classes untuk banner [VERIFIED: existing `examExpiredModal:294`] | Already in `_Layout` |
| Bootstrap Icons | (vendored) | `bi-clock-fill`, `bi-hourglass-bottom`, `bi-exclamation-triangle-fill` [VERIFIED: ExamSummary.cshtml:120, StartExam.cshtml:299] | Already in use |
| AuditLogService | (custom) | `LogAsync(actorUserId, actorName, actionType, description, targetId, targetType)` [VERIFIED: Services/AuditLogService.cs:21-43] | Phase 312 reuse pattern (D-05) |

### Supporting (Test Infrastructure)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Playwright Test | ^1.58.2 [VERIFIED: tests/package.json] | E2E browser automation untuk 6 skenario | Phase 313 SC #7 — manual/auto × before/at/in-grace/after-grace matrix |
| TypeScript | ^5.9.3 [VERIFIED: tests/package.json] | Spec types | Existing `tests/e2e/*.spec.ts` |
| `tests/helpers/auth.ts::login()` | (custom) | Worker login fixture untuk `coachee` role [VERIFIED: tests/helpers/accounts.ts:4 — rino.prasetyo@pertamina.com] | Reuse Phase 312 pattern |
| `tests/helpers/utils.ts::uniqueTitle/today/autoConfirm` | (custom) | Test fixture creators [VERIFIED: imported di exam-taking.spec.ts:3] | Reuse |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `_auditLog.LogAsync` swallow exceptions | `try/catch` wrap dengan `_logger.LogWarning` fallback | Phase 312 confirmed pattern (T-306-02 mitigation precedent: `if audit fails, don't block primary action`). Use try/catch (D-05 implicit). |
| Inline 2-tier branching dalam SubmitExam | Extract `EnsureCanSubmitExamAsync` helper | Helper memberikan testability + readability; Phase 312 precedent (`EnsureCanDeleteAsync`). **Recommended.** |
| Vanilla `fetch` retry chain | Use existing `saveAnswerAsync` retry pattern (StartExam.cshtml:614+) | Existing pattern hanya untuk SaveAnswer (single-shot) tanpa exponential backoff. Greenfield retry untuk SubmitExam form (D-10). |
| Disable button via `@if` Razor | Disable via JS post-load (countdown=0) | Combination: Razor untuk page-load state (timerExpired), JS untuk runtime transition (countdown→0 selama active session). |
| Mock IDateTimeProvider | DB UPDATE StartedAt back-dated | D-07 lock — DB manipulation. No new abstraction. |

**Installation:** Tidak ada dependency baru. Semua infrastruktur sudah tersedia.

**Version verification:**
- ASP.NET Core 8.0 [VERIFIED: HcPortal.csproj]
- Playwright 1.58.2 [VERIFIED: tests/package.json devDependencies]
- TypeScript 5.9.3 [VERIFIED: tests/package.json]
- Bootstrap & Bootstrap Icons (vendored di `wwwroot/lib/`)

## Architecture Patterns

### System Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│  WORKER BROWSER (StartExam → ExamSummary → SubmitExam Flow)          │
│                                                                      │
│  StartExam.cshtml                  ExamSummary.cshtml                │
│  ┌───────────────────┐             ┌──────────────────────────┐     │
│  │ countdown timer   │  countdown=0│ #autoSubmitFlag hidden   │     │
│  │ (line 445-481)    │────────────▶│ value=true if timerExpd  │     │
│  │ updateTimer()     │             │ (line 110)               │     │
│  │ +retry 3x JS NEW  │             │ Submit btn disabled      │     │
│  │ examForm.submit() │             │ when timerExpired NEW    │     │
│  └─────────┬─────────┘             └─────────┬────────────────┘     │
│            │ POST /CMP/ExamSummary           │ POST /CMP/SubmitExam │
│            │   (review)                       │   (final)            │
└────────────┼──────────────────────────────────┼──────────────────────┘
             │                                  │
             ▼                                  ▼
┌──────────────────────────────────────────────────────────────────────┐
│  CMPController.cs                                                    │
│                                                                      │
│  ExamSummary(id, answers, assignmentId)  SubmitExam(id, answers,    │
│  ┌───────────────────────────┐           ┌──────────────────────┐   │
│  │ compute timerExpired      │           │ TIER GUARD (NEW):    │   │
│  │ (line 1536-1543)          │           │ EnsureCanSubmitExam  │   │
│  │ → ViewBag.TimerExpired    │           │ ───────────────────  │   │
│  └───────────┬───────────────┘           │ if AssessmentType in │   │
│              │                           │  [Online/PreTest/    │   │
│              │                           │   PostTest]:         │   │
│              │                           │  Tier 1: !isAuto &&  │   │
│              │                           │   elapsed > allowed  │   │
│              │                           │   → AuditLog Blocked │   │
│              │                           │   → TempData D-01    │   │
│              │                           │   → RedirectStartExam│   │
│              │                           │  Tier 2: elapsed >   │   │
│              │                           │   allowed + 2min     │   │
│              │                           │   → existing reject  │   │
│              │                           │ ───────────────────  │   │
│              │                           │ (line 1619-1628)     │   │
│              │                           └─────────┬────────────┘   │
└──────────────┴─────────────────────────────────────┼────────────────┘
                                                     │
                                                     ▼
                                  ┌─────────────────────────────────┐
                                  │ AuditLogService.LogAsync        │
                                  │ ActionType=SubmitExamBlocked    │
                                  │ Description="HC/User role..."   │
                                  │ → AuditLogs table               │
                                  └─────────────────────────────────┘
```

### Recommended Project Structure

Tidak ada folder baru — semua edits di file existing:
```
Controllers/CMPController.cs              # Modify line 1619-1628 + extract helper
Views/CMP/ExamSummary.cshtml              # Modify button block + add retry JS
Views/CMP/StartExam.cshtml                # Modify line 462-478 timer-up flow + retry helper
tests/e2e/exam-taking.spec.ts             # Append FLOW: "Phase 313 Timer Block" describe
tests/helpers/utils.ts                    # (optional) helper baru untuk DB back-date
.planning/seeds/313-timer-fixtures.sql    # (optional D-08) — Wave 0 seed script
.planning/phases/313-.../313-UAT.md       # Manual UAT script (mandatory per Phase 312 precedent)
```

### Pattern 1: Helper Extraction `EnsureCanSubmitExamAsync` (Phase 312 Precedent)

**What:** Privat helper di CMPController yang centralize tier-guard logic + AuditLog blocked write + TempData/redirect return.

**When to use:** Saat guard logic > 5 lines dan dipanggil dari 1+ tempat (atau berpotensi di-test isolation). Phase 312 D-04 lock confirmed body-method placement.

**Example signature (analog Phase 312):**
```csharp
// Source: pattern from Controllers/AssessmentAdminController.cs:5474 (Phase 312 EnsureCanDeleteAsync)
private async Task<IActionResult?> EnsureCanSubmitExamAsync(
    AssessmentSession assessment,
    bool isAutoSubmit)
{
    // Manual type exclude — D-15 defense-in-depth
    if (assessment.AssessmentType != AssessmentConstants.AssessmentType.Online &&
        assessment.AssessmentType != AssessmentConstants.AssessmentType.PreTest &&
        assessment.AssessmentType != AssessmentConstants.AssessmentType.PostTest)
    {
        return null; // Manual type — skip guard
    }
    
    if (!assessment.StartedAt.HasValue) return null; // Legacy session
    
    var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
    int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
    int graceLimit = allowedMinutes + 2;
    
    // Tier 1: manual reject (NEW per phase 313)
    if (!isAutoSubmit && elapsed.TotalMinutes > allowedMinutes)
    {
        await WriteBlockedAuditAsync(assessment, elapsed, allowedMinutes);
        TempData["Error"] = "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman.";
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }
    
    // Tier 2: auto reject after grace (existing LIFE-03 behavior preserved)
    if (elapsed.TotalMinutes > graceLimit)
    {
        TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
        return RedirectToAction("StartExam", new { id = assessment.Id });
    }
    
    return null; // Pass — caller continue
}
```

### Pattern 2: AuditLog Blocked Entry (Phase 312 D-03)

**Source:** `Services/AuditLogService.cs:21` (verified signature) + `.planning/phases/312-.../312-01-SUMMARY.md:74`

```csharp
// Source: Phase 312 EnsureCanDeleteAsync line 5524-5536 pattern
private async Task WriteBlockedAuditAsync(AssessmentSession assessment, TimeSpan elapsed, int allowedMinutes)
{
    try
    {
        var user = await _userManager.GetUserAsync(User);
        var actorName = user != null ? $"{user.NIP ?? ""} {user.FullName ?? ""}".Trim() : "(unknown)";
        var description = $"HC/User role manual submit blocked after timeup. " +
                          $"Type={assessment.AssessmentType} " +
                          $"ElapsedMin={(int)elapsed.TotalMinutes} " +
                          $"AllowedMin={allowedMinutes} " +
                          $"SessionId={assessment.Id}";
        await _auditLog.LogAsync(
            actorUserId: user?.Id ?? "",
            actorName: actorName,
            actionType: "SubmitExamBlocked",   // D-05
            description: description,
            targetId: assessment.Id,
            targetType: "AssessmentSession");
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "AuditLog SubmitExamBlocked write failed for session {SessionId}", assessment.Id);
        // Swallow — audit failure tidak boleh block primary action
    }
}
```

### Pattern 3: Frontend Conditional Disable + Retry Backoff

**Razor (ExamSummary.cshtml replace line 130-143):**
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
    @* existing disabled state — preserved *@
}
else
{
    @* existing submit button — preserved *@
}
```

**JS retry pattern (vanilla, append ke ExamSummary.cshtml @section Scripts):**
```javascript
// Source: greenfield per D-10 — exponential backoff [1s, 2s, 4s]
function submitWithRetry(form, maxAttempts) {
    var attempt = 0;
    var delays = [1000, 2000, 4000];
    function attemptSubmit() {
        attempt++;
        var fd = new FormData(form);
        return fetch(form.action, { method: 'POST', body: fd })
            .then(function(r) {
                if (r.redirected) { window.location.href = r.url; return; }
                if (!r.ok) throw new Error('HTTP ' + r.status);
                window.location.href = r.url;
            })
            .catch(function(err) {
                console.error('Submit attempt ' + attempt + ' failed', err);
                if (attempt < maxAttempts) {
                    setTimeout(attemptSubmit, delays[attempt - 1]);
                } else {
                    showRetryFailBanner(); // D-11 banner permanent
                }
            });
    }
    return attemptSubmit();
}
```

### Anti-Patterns to Avoid

- **`assessment.Type` (typo).** CONTEXT.md menulis `assessment.Type` — TIDAK exist di model. Pakai `assessment.AssessmentType`. Use constants `AssessmentConstants.AssessmentType.Online/PreTest/PostTest` untuk hindari magic string.
- **Modify `StartExam.cshtml` Submit button.** Tombol Submit final ada di `ExamSummary.cshtml`. `StartExam.cshtml` hanya punya `#reviewSubmitBtn` (line 200) yang submit ke ExamSummary review (bukan SubmitExam endpoint). Modifikasi disable button D-03 utamanya target ExamSummary.cshtml.
- **Audit blocking AuditLog write.** Wrap dalam try/catch dengan logger.LogWarning fallback (Phase 312 T-306-02 precedent). Audit failure ≠ block submit reject.
- **Modify success path AuditLog (D-06).** Scope creep. Hanya tambah blocked entry.
- **Hardcode magic string "Online"/"PreTest"/"PostTest".** Pakai `AssessmentConstants.AssessmentType.*` constants (Models/AssessmentConstants.cs:5-11).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Audit log write | Custom INSERT INTO AuditLogs | `_auditLog.LogAsync(...)` (Services/AuditLogService.cs) | DI-injected, transaction-safe, Phase 312 precedent |
| User context retrieval | `User.Claims.FirstOrDefault()` | `_userManager.GetUserAsync(User)` (already used line 1564) | Existing pattern, NIP+FullName accessible |
| Tier guard composition | Inline 2-tier `if/else` di SubmitExam | `EnsureCanSubmitExamAsync` private helper | Phase 312 D-04 lock + testability + readability |
| Time computation | Manual second arithmetic | `TimeSpan.TotalMinutes` (existing line 1623) | Existing pattern, type-safe |
| Form anti-forgery | Custom token check | `[ValidateAntiForgeryToken]` attribute (already on SubmitExam line 1555) | ASP.NET Core built-in |
| Server time source | Inject IDateTimeProvider | `DateTime.UtcNow` (existing line 1621) | D-07 deferred refactor, current pattern works |

**Key insight:** 90% reuse — Phase 313 adalah extension dari existing LIFE-03 logic + Phase 312 AuditLog patterns. Tidak ada library/framework baru, hanya 1 helper baru + minor refactor.

## Common Pitfalls

### Pitfall 1: Field Name Mismatch (`Type` vs `AssessmentType`)
**What goes wrong:** Implementer literal copy CONTEXT.md `assessment.Type == "Online"` → build error CS1061 (no such field).
**Why it happens:** CONTEXT.md drafter kemungkinan mental shortcut "Type"; field actual `AssessmentType` (Models/AssessmentSession.cs:154 verified).
**How to avoid:** Pakai constants `AssessmentConstants.AssessmentType.Online` (Models/AssessmentConstants.cs:8). Plan file harus eksplisit field name.
**Warning signs:** Build fail CS1061 'AssessmentSession' does not contain a definition for 'Type'.

### Pitfall 2: Modifying StartExam.cshtml Submit Button (Wrong File)
**What goes wrong:** Implementer modify `#reviewSubmitBtn` (StartExam.cshtml:200) thinking itu Submit final → user UX bingung karena Review-and-Submit button masih aktif (itu navigasi ke ExamSummary, not destructive).
**Why it happens:** CONTEXT.md/ROADMAP menyebut "frontend StartExam.cshtml" — sebenarnya yang relevant adalah `examForm` line 71 (`asp-action="ExamSummary"`) NOT SubmitExam endpoint. Submit final di ExamSummary.cshtml line 139.
**How to avoid:** Modifikasi disable button D-03 PRIMARILY di `Views/CMP/ExamSummary.cshtml:130-143`. StartExam.cshtml hanya tambah retry handler di `setTimeout(...examForm.submit())` line 471-475 (auto-submit fire path).
**Warning signs:** UAT user laporan "tombol Submit/Kumpulkan masih bisa di-klik di review page".

### Pitfall 3: TempData Lost on Redirect Loop
**What goes wrong:** Tier-1 reject set TempData["Error"] → redirect StartExam → StartExam GET handler tidak read TempData → user tidak lihat banner.
**Why it happens:** TempData dipassed sekali via cookie/session, harus diakses di view destination.
**How to avoid:** Verifikasi `Views/CMP/StartExam.cshtml` render TempData["Error"] sebagai banner (cek existing `_Layout.cshtml` partial). Atau eksplisit add Razor `@if (TempData["Error"] != null)` block di top StartExam.cshtml.
**Warning signs:** UAT user laporan "tombol Submit reject tapi tidak ada pesan error".

### Pitfall 4: Idempotency After Auto-Submit
**What goes wrong:** Auto-submit fire success → redirect Results → user click browser Back → Submit button masih ada → second submit attempt → Tier 2 reject (correct behavior tapi alert error redundant).
**Why it happens:** Browser navigation history.
**How to avoid:** D-13 explicit: reload after timeup → button disabled + banner info "Submit otomatis sudah/sedang berjalan." TIDAK fire auto-submit lagi. Server tier-2 reject duplicate sebagai safety net (existing).
**Warning signs:** Multiple AuditLog Blocked entries dengan SessionId sama dalam window pendek.

### Pitfall 5: Playwright Selector Substring Match (Phase 312 WR-03)
**What goes wrong:** `tr:has-text("Phase 313 Timer Fixture Online ManualBeforeTime")` match row lain karena substring di kolom (e.g., judul mirip).
**Why it happens:** Playwright `has-text` substring + Bahasa Indonesia common text → false positive.
**How to avoid:** Pakai badge-scoped selector atau test-id. Kalau menggunakan title fixture, pakai exact match `tr:has-text(/^Phase 313 Timer Fixture Online ManualBeforeTime$/)` atau scope ke kolom judul spesifik.
**Warning signs:** Test pass tapi assert content fixture salah (different row).

### Pitfall 6: TOCTOU Race in Audit Write Path (Phase 312 WR-01)
**What goes wrong:** Tier-1 reject path: read assessment → check elapsed → write AuditLog → TempData → redirect. Antara check dan write, parallel auto-submit landing → 2x AuditLog (Blocked + success).
**Why it happens:** Multiple browser tabs / retry race.
**How to avoid:** Phase 313 guard logic adalah read-only check + audit write (no state mutation). Audit-write idempotency tidak critical (multiple Blocked entries OK — informational, query downstream bisa dedupe). Tidak perlu BeginTransactionAsync untuk guard itself.
**Warning signs:** Acceptable noise di AuditLog. Bisa di-dedupe via query `DISTINCT TargetId, ActorUserId, CAST(CreatedAt AS DATE)`.

### Pitfall 7: Timer Drift After Tab Hidden (Existing Phase Decision)
**What goes wrong:** Browser throttle setInterval di background tab → timer display lambat → countdown=0 fire telat → server elapsed sudah > grace.
**Why it happens:** Browser power saving.
**How to avoid:** Sudah ada `visibilitychange` listener (StartExam.cshtml:487) yang re-anchor timer dari `Date.now()` wall clock. Phase 313 TIDAK perlu modify ini.
**Warning signs:** Timer display jump dari "00:30" ke "00:00" saat tab focus.

## Runtime State Inventory

> **Trigger:** Phase 313 BUKAN rename/refactor/migration — primary code modification. Section ini di-include karena helpful untuk validate "no migration data needed".

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — no schema change. Existing AuditLogs table cukup untuk new ActionType `SubmitExamBlocked` (varchar 50, fits). AssessmentSessions tidak butuh field baru. | None — no migration |
| Live service config | None — no external service registration | None |
| OS-registered state | None — no scheduled tasks/services | None |
| Secrets/env vars | None — no new keys | None |
| Build artifacts | None — pure code changes (Controllers, Views, tests) | Standard `dotnet build` |

**Verified by:** `Models/AuditLog.cs` (line 30: `[MaxLength(50)] ActionType`); `SubmitExamBlocked` = 18 chars, fits.

## Code Examples

### Verified Pattern: Existing LIFE-03 Block (To Replace)
```csharp
// Source: Controllers/CMPController.cs:1616-1628 [VERIFIED via Read]
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

### Verified Pattern: Existing isAutoSubmit Hidden Field (ExamSummary.cshtml)
```razor
@* Source: Views/CMP/ExamSummary.cshtml:107-114 [VERIFIED via Read] *@
<form asp-action="SubmitExam" method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@assessmentId" />
    <input type="hidden" name="isAutoSubmit" value="@(timerExpired ? "true" : "false")" id="autoSubmitFlag" />
    @foreach (var kvp in answers)
    {
        <input type="hidden" name="answers[@kvp.Key]" value="@kvp.Value" />
    }
    ...
</form>
```

### Verified Pattern: Existing Auto-Submit Fire (StartExam.cshtml)
```javascript
// Source: Views/CMP/StartExam.cshtml:462-478 [VERIFIED via Read]
if (remaining <= 0) {
    clearInterval(timerInterval);
    clearInterval(saveInterval);
    window.onbeforeunload = null;
    if (!submitted) {
        var timeupModal = new bootstrap.Modal(document.getElementById('timeUpWarningModal'));
        timeupModal.show();
        // Auto-submit setelah 10 detik jika user tidak klik OK
        setTimeout(function() {
            if (!submitted) {
                submitted = true;
                document.getElementById('examForm').submit(); // → POST /CMP/ExamSummary
            }
        }, 10000);
    }
}
```

**Important nuance:** `examForm.submit()` POST ke `ExamSummary` (review page), BUKAN langsung SubmitExam. ExamSummary menerima `isAutoSubmit` dari ViewBag.TimerExpired calculated server-side, lalu auto-render hidden field, dan `confirm()` button click → submit final ke SubmitExam. Auto-submit flow saat ini PERLU 1 user click di "OK" timeUpWarningModal modal → form submit → ExamSummary GET → user click "Kumpulkan Ujian". **Phase 313 D-04 explicit: "fire immediate at countdown=0"** — implementer harus VERIFIKASI apakah modal-confirm step di-skip (jadi auto-submit chain langsung tanpa user click). Recommend: ubah `setTimeout(10000)` → eksekusi auto-chain dengan retry langsung, OR tambah auto-click handler di ExamSummary saat `timerExpired=true`.

### Verified Pattern: AssessmentConstants Usage (Phase 309 precedent)
```csharp
// Source: Models/AssessmentConstants.cs:5-11 [VERIFIED]
public static class AssessmentType
{
    public const string Manual = "Manual";
    public const string Online = "Online";
    public const string PreTest = "PreTest";
    public const string PostTest = "PostTest";
}
```

### New Pattern: Playwright Test Skeleton (FLOW Phase 313)
```typescript
// Source: pattern from tests/e2e/assessment.spec.ts FLOW 12 (Phase 312)
test.describe('FLOW 313: Phase 313 Timer Block (manual after timeup)', () => {
  test('313.1 - Manual + Before-time + Online → submit OK', async ({ page }) => {
    const targetRow = page.locator('tr', { hasText: 'Phase 313 Timer Fixture Online ManualBeforeTime' }).first();
    if (await targetRow.count() === 0) {
      test.skip(true, 'Seed "Phase 313 Timer Fixture Online ManualBeforeTime" tidak ditemukan — Wave 0 manual seed required');
    }
    await login(page, 'coachee');
    // ... navigate to StartExam → ExamSummary → click Submit
    // assert redirect to /CMP/Results, AuditLog entry NOT contains SubmitExamBlocked
  });
  
  test('313.2 - Manual + After-grace + Online → reject + AuditLog Blocked', async ({ page }) => {
    const targetRow = page.locator('tr', { hasText: 'Phase 313 Timer Fixture Online ManualAfterGrace' }).first();
    if (await targetRow.count() === 0) test.skip(true, '...');
    // ... assert TempData banner D-01, redirect StartExam, AuditLog Blocked entry exists
  });
  // ... 313.3 - 313.6 cover matrix
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single-tier timer reject (manual & auto same threshold) | 2-tier branching (`isAutoSubmit` discriminator) | Phase 313 (this) | Closes TMR-01 manual bypass; auto-submit safety preserved |
| AuditLog tanpa Blocked entries untuk failed validation | `{Action}Blocked` ActionType convention | Phase 312 D-03 (precedent) | Audit trail untuk denied actions |
| Hardcode magic string "Online"/"PreTest" | `AssessmentConstants.AssessmentType.*` | Phase 309 introduced (Models/AssessmentConstants.cs) | Type-safe, refactorable |
| Inline guard logic | Extracted helper (e.g., `EnsureCanDeleteAsync`) | Phase 312 (precedent) | Testability, locality |

**Deprecated/outdated:**
- Single-tier `elapsed > allowed + grace` for manual (current line 1623) — replaced by 2-tier dengan `isAutoSubmit` (this phase).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8.0 | Build verification (D-15 lock CLAUDE.md) | ✓ | 8.0 [VERIFIED: HcPortal.csproj] | None — required |
| `dotnet run` localhost:5277 | Manual UAT (Phase 312 precedent) | ✓ | (per CLAUDE.md DEV_WORKFLOW) | None |
| Playwright 1.58.2 + chromium | E2E SC #7 | ✓ | 1.58.2 [VERIFIED: tests/package.json] | `test.skip` graceful (Phase 312 pattern) |
| TypeScript 5.9.3 | Spec compile | ✓ | 5.9.3 [VERIFIED] | None |
| MS SQL Server (DB akses) | Manual UAT seed `StartedAt` back-date + AuditLog spot-check | ✓ (per Phase 312 UAT precedent) | (per env) | None |
| SSMS / DBeaver / sqlcmd | Direct DB UPDATE for fixture seed (D-07) | ✓ (assumed per Phase 312 UAT) | — | Bisa pakai admin UI ManageAssessment ExtraTime feature untuk indirect manipulation (limited) |
| Test users | `coachee@pertamina.com` (rino.prasetyo@pertamina.com) | ✓ [VERIFIED: tests/helpers/accounts.ts:4] | — | None |
| HC user | `meylisa.tjiang@pertamina.com` | ✓ [VERIFIED: accounts.ts:3] | — | None |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** None.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Backend test framework | None (no .NET unit tests in project — verified via existing phase summaries) |
| E2E framework | Playwright 1.58.2 + TypeScript 5.9.3 |
| Config file | `tests/playwright.config.ts` [VERIFIED] |
| Quick run command | `cd tests && npx playwright test e2e/exam-taking.spec.ts --grep "Phase 313" --reporter=list` |
| Full suite command | `cd tests && npx playwright test --reporter=list` |
| Manual UAT | `.planning/phases/313-.../313-UAT.md` (mandatory per Phase 312 precedent — checkpoint:human-verify) |

### Phase Requirements → Test/Validation Map (per Acceptance Criterion)

| SC # | Behavior | Validation Type | Concrete Signal | Wave 0 Gap? |
|------|----------|-----------------|-----------------|-------------|
| **SC 1** | Modify LIFE-03 block jadi 2-tier `isAutoSubmit` | Code grep + dotnet build | grep `EnsureCanSubmitExamAsync` count == 1 (decl) + ≥ 1 (call); `Controllers/CMPController.cs:1619-1628` substituted with helper call; `dotnet build` exit 0 | None (file exists) |
| **SC 2** | Tier 1: `!isAutoSubmit && elapsed > allowed` reject manual + TempData + redirect StartExam | Playwright + UAT | Test 313.2: POST `/CMP/SubmitExam` form `isAutoSubmit=false` + back-dated `StartedAt = NOW - (Duration+1)` → assert `page.url()` matches `/CMP/StartExam/{id}`; assert `page.locator('.alert')` contains `"Waktu ujian Anda sudah habis"` | New test file gap — append to `tests/e2e/exam-taking.spec.ts` |
| **SC 3** | Tier 2: `elapsed > allowed + 2min` reject auto-submit (existing preserved) | Playwright regression + UAT | Test 313.3: `isAutoSubmit=true` + `StartedAt = NOW - (Duration+ExtraTime+5)` → assert redirect StartExam; assert TempData "Pengiriman jawaban tidak dapat diproses" (existing message preserved) | Append test |
| **SC 4** | Frontend `ExamSummary.cshtml`: countdown=0 disable Submit manual; auto-submit handler tetap aktif | Playwright DOM assert + UAT | Selector `#manualSubmitDisabledBtn:disabled` exists when `timerExpired=true` ViewBag set; assert text contains `"Waktu Habis - Submit Otomatis Berjalan..."`; assert spinner element visible. **For StartExam.cshtml retry handler:** assert console log `"Submit attempt 2 failed"` after network mock. | Append test + JS retry handler implementation |
| **SC 5** | AuditLog entry `SubmitExamBlocked` dengan `{UserId, SessionId, ElapsedMin, AllowedMin}` | DB SQL spot-check (UAT) + Playwright optional API check | SQL: `SELECT TOP 5 * FROM AuditLogs WHERE ActionType = 'SubmitExamBlocked' ORDER BY CreatedAt DESC` → assert ≥ 1 row with `Description LIKE '%SessionId={fixtureId}%'` AND contains `ElapsedMin=` AND `AllowedMin=` AND `Type=Online|PreTest|PostTest`; `TargetId == fixtureId`; `TargetType == 'AssessmentSession'` | Add SQL block to 313-UAT.md (Phase 312 precedent) |
| **SC 6** | Verifikasi 3 tipe ber-timer: Online, PreTest, PostTest (Manual exclude) | Playwright matrix + UAT | Tests 313.4 (PreTest), 313.5 (PostTest), 313.6 (Manual must-not-block: `isAutoSubmit=false, elapsed > allowed`, AssessmentType=Manual → success path, no AuditLog Blocked entry) | Append 3 tests; need 4 fixtures (Online, PreTest, PostTest, Manual) |
| **SC 7** | E2E 6 skenario manual/auto × before-time/at-time/in-grace/after-grace | Playwright FLOW 313 describe block | 6+ tests covering matrix; each uses dedicated fixture title `Phase 313 Timer Fixture {Type} {Scenario}` (D-08); `test.skip` graceful if seed missing | Append all tests; create seed script `.planning/seeds/313-timer-fixtures.sql` (D-08 Claude discretion) |

### Sampling Rate (per .planning/config.json `nyquist_validation: true`)
- **Per task commit:** `dotnet build --no-incremental` (existing pattern Phase 312)
- **Per wave merge:** `cd tests && npx playwright test e2e/exam-taking.spec.ts --grep "Phase 313" --reporter=list` (smoke only, fast)
- **Phase gate (`/gsd-verify-work`):** Full suite + Manual UAT 313-UAT.md sign-off (PASS rows ≥ 6) + DB AuditLog spot-check verified

### Wave 0 Gaps
- [ ] `.planning/phases/313-.../313-UAT.md` — Manual UAT script with 6+ steps, sign-off rows, DB spot-check SQL (mirror Phase 312 structure)
- [ ] `tests/e2e/exam-taking.spec.ts` — Append `FLOW 313` describe block (6+ tests with `test.skip` graceful)
- [ ] `.planning/seeds/313-timer-fixtures.sql` — (Optional Claude discretion) seed script untuk 6 fixture: Online/PreTest/PostTest × ManualBeforeTime/ManualAfterGrace/AutoInGrace/AutoAfterGrace + 1 Manual-type fixture untuk SC #6 exclude verification
- [ ] `Controllers/CMPController.cs` — Add `EnsureCanSubmitExamAsync` helper (Phase 312 location pattern: end of class before closing brace) + `WriteBlockedAuditAsync` helper
- [ ] `Views/CMP/ExamSummary.cshtml` — Modify button block (line 130-143) + append retry JS handler section
- [ ] `Views/CMP/StartExam.cshtml` — Modify timeup flow (line 462-478) untuk: (a) auto-fire fast path tanpa modal-confirm step jika timerExpired (D-04), (b) reload-after-timeup banner (D-13)
- [ ] (Verify) `Views/CMP/StartExam.cshtml` — Confirm TempData["Error"] banner render exists or add `@if` block at top (Pitfall 3)
- [ ] Test framework install: NOT required (Playwright already installed per `tests/package.json`)

## Security Domain

> Required (`security_enforcement` not explicitly false in config; default enabled).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity (existing) — `_userManager.GetUserAsync(User)` already used line 1564 |
| V3 Session Management | yes | Cookie auth + AntiForgery token — `[ValidateAntiForgeryToken]` already on SubmitExam line 1555 |
| V4 Access Control | yes | Owner check line 1566 (`assessment.UserId != user.Id && !User.IsInRole(...)`) — preserved unchanged |
| V5 Input Validation | yes | Model binding `Dictionary<int, int> answers` + `bool isAutoSubmit` — type-safe (existing) |
| V6 Cryptography | no | No new crypto. AntiForgery uses ASP.NET Core built-in. |
| V7 Error Handling | yes | Try/catch around AuditLog write + `_logger.LogWarning` fallback (Phase 312 T-306-02 precedent) |
| V8 Data Protection | yes | TempData uses cookie — no PII new exposure (only generic error message, no SessionId in user-facing text) |
| V11 Business Logic | yes | **Tier-1 manual reject IS the new business logic control** for TMR-01 |
| V13 API & Web Service | yes | POST endpoint `[HttpPost]` + AntiForgery — existing |

### Known Threat Patterns for ASP.NET Core MVC + Razor

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Manual submit bypass via DevTools (set `isAutoSubmit=true` in form) | Tampering / Elevation | **Server-side guard ignores client claim authoritatively when `elapsed > allowed`** — Tier 2 still enforces `+2min grace`. Even if attacker sets `isAutoSubmit=true`, after `Duration+ExtraTime+2min` server rejects. Defense-in-depth: client-side disable button + server tier-1 reject + tier-2 grace cap. |
| Replay attack (re-submit form via Back button) | Repudiation | `assessment.Status == "Completed"` check line 1572 (existing) — second submit redirected to Assessment with "already completed" message. Preserved. |
| TOCTOU race (multi-tab parallel submit) | Tampering | Existing `assessment.Status` check + GradingService idempotency (Phase 309 precedent). Audit Blocked write may have noise (multiple entries) — acceptable per Pitfall 6. |
| CSRF on POST `/CMP/SubmitExam` | Tampering | `[ValidateAntiForgeryToken]` (line 1555) — preserved |
| AuditLog bypass via exception | Repudiation | Try/catch + `_logger.LogWarning` (Phase 312 T-306-02 precedent — primary action proceeds, audit logged separately) |
| Information disclosure via TempData["Error"] | Information Disclosure | Generic message D-01 (no SessionId, no UserId, no internal state) — safe to display |
| Network sniffing of submit POST | Information Disclosure | HTTPS enforced at infra layer (Pertamina dev/prod) — out of phase scope |

### Threat Coverage Summary
| Threat ID | Description | Mitigated By |
|-----------|-------------|--------------|
| T-313-01 | Manual submit after timeup bypasses LIFE-03 grace window (TMR-01 root) | Tier-1 strict 0-grace reject in `EnsureCanSubmitExamAsync` (PRIMARY) |
| T-313-02 | DevTools force `isAutoSubmit=true` to skip Tier-1 | Tier-2 enforces hard cap `Duration+ExtraTime+2min` regardless of `isAutoSubmit` flag |
| T-313-03 | Manual type assessment incorrectly blocked | Defense-in-depth `AssessmentType` field check (D-15) — Manual type bypasses guard entirely |
| T-313-04 | Audit gap for blocked attempts (no traceability) | AuditLog `SubmitExamBlocked` entry with `{Type, ElapsedMin, AllowedMin, SessionId, ActorUserId}` (D-05) |
| T-313-05 | Network failure causes auto-submit data loss | Client retry 3x exponential backoff (D-10) + banner-only fallback (D-11) |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `_userManager` available in `WriteBlockedAuditAsync` helper context (CMPController) | Pattern 2 example | LOW — `_userManager` is field already used line 1564 |
| A2 | `_logger` field exists on CMPController for `LogWarning` fallback | Pattern 2 example | LOW — Phase 312 precedent confirms ILogger DI pattern; verify field name `_logger` exists in CMPController constructor (not verified — implementer confirm) |
| A3 | `ApplicationUser` exposes `NIP` and `FullName` properties for actor name | Pattern 2 example | LOW — `Models/ApplicationUser.cs` standard fields per Phase 312 audit description |
| A4 | TempData["Error"] is rendered by existing layout/StartExam.cshtml view | Pitfall 3 | MEDIUM — verify before locking plan; if not rendered, add explicit `@if` block (Pitfall 3 mitigation) |
| A5 | `_context.AssessmentSessions` query in CMPController is the canonical entry — no service-layer abstraction needed | Pattern 1 | LOW — verified line 1558 directly uses `_context` |
| A6 | DB `StartedAt` UPDATE manipulation works without breaking SignalR or other listeners | D-07 / Pitfall 4 | MEDIUM — Phase 310 WR-04 pattern showed dedicated fixture isolation prevents cross-contamination; explicit dedicated fixtures (D-08) mitigate |
| A7 | Auto-submit fire-immediate (D-04) implies removing or bypassing 10s `setTimeout` modal-confirm step at StartExam.cshtml:471 | Code Examples > Existing Auto-Submit | HIGH — IMPLEMENTER must verify with discuss-phase / user. Current behavior shows 10-second modal-confirm before auto-submit; D-04 says "fire immediate" — contradiction or refinement? Recommend planner clarify. |
| A8 | `Models/ApplicationUser.cs` has `NIP` and `FullName` (or equivalent) properties for AuditLog ActorName format | Pattern 2 | LOW — Phase 312 SUMMARY uses identical pattern (`actorName: $"NIP={user.NIP} {user.FullName}"`-style). Implementer should verify exact field names. |

**If this table is empty:** N/A — 8 assumptions flagged. **A4 and A7 are MEDIUM-HIGH risk** and warrant planner verification before locking task descriptions. A7 specifically requires user/discuss confirmation about whether to remove the 10s `timeUpWarningModal` confirm-click step.

## Open Questions

1. **Auto-submit immediate vs modal-confirm step (A7)**
   - What we know: Current `StartExam.cshtml:467-477` shows `timeUpWarningModal` then 10s `setTimeout` → form.submit. D-04 says "fire immediate at countdown=0".
   - What's unclear: Apakah modal-confirm step di-skip seluruhnya, atau modal tetap muncul tapi auto-fire setelah 0s delay (effectively modal becomes informational only)?
   - Recommendation: Planner clarify dengan user di plan-check phase. Default reasonable interpretation: ubah `setTimeout(10000)` → `setTimeout(0)` and update modal text agar reflect "Mengirim sekarang..." vs "Akan dikirim 10 detik".

2. **TempData rendering in StartExam.cshtml (A4)**
   - What we know: Existing tier-2 reject pakai TempData["Error"] + redirect ke StartExam (line 1626). Behavior assumed working.
   - What's unclear: Apakah _Layout.cshtml render TempData["Error"] global, atau spesifik per-view?
   - Recommendation: Plan harus include task untuk verify (grep `TempData["Error"]` rendering di Views/Shared/_Layout.cshtml). Kalau tidak ada, tambah explicit `@if (TempData["Error"] != null) { <div class="alert alert-danger">@TempData["Error"]</div> }` di top StartExam.cshtml (line ~10 sebelum sticky header).

3. **Where to place new `_logger` field if not exists (A2)**
   - What we know: Phase 312 SUMMARY references `_logger.LogWarning` pattern.
   - What's unclear: Apakah CMPController constructor sudah inject ILogger?
   - Recommendation: Implementer verify via grep `private readonly ILogger` di CMPController.cs. Tambah inject kalau missing (constructor + readonly field), dependency available via DI.

4. **Test fixture seed strategy: SQL script vs Playwright setup helper (D-08 discretion)**
   - What we know: Phase 312 used title-based fixtures + `test.skip` graceful + manual UAT authoritative. D-08 confirms similar.
   - What's unclear: Apakah tim prefer SQL script di `.planning/seeds/` (admin runs once) atau Playwright `globalSetup` (auto-create on first run)?
   - Recommendation: SQL script approach simpler + reusable for manual UAT (test fixture setup tab di SSMS sekali, dipakai ulang). Plan dapat include sample SQL untuk 6 fixture: 4 timed (Online/PreTest/PostTest) + 1 Manual + 1 control.

## Sources

### Primary (HIGH confidence — VERIFIED via Read/Grep)
- `Controllers/CMPController.cs` line 1530-1730 — SubmitExam method body, LIFE-03 block, isAutoSubmit signature
- `Models/AssessmentSession.cs` — full file (AssessmentType field at line 154)
- `Models/AssessmentConstants.cs` — AssessmentType constants (line 5-11)
- `Models/AuditLog.cs` — ActionType MaxLength(50), Description schema
- `Services/AuditLogService.cs` — LogAsync signature
- `Views/CMP/ExamSummary.cshtml` — full file (form line 107-145, isAutoSubmit hidden field line 110, timerExpired conditional line 117-143)
- `Views/CMP/StartExam.cshtml` — line 1-200 + 440-500 (timer logic, timeUpWarningModal, examForm action)
- `tests/e2e/exam-taking.spec.ts` — line 1-300 (Flow A pattern, login fixture usage)
- `tests/helpers/accounts.ts` — coachee/hc credentials
- `tests/playwright.config.ts` — config (testDir, baseURL)
- `tests/package.json` — Playwright 1.58.2 version
- `HcPortal.csproj` — .NET 8.0 target framework
- `.planning/REQUIREMENTS.md` — TMR-01 full text
- `.planning/STATE.md` — milestone v15.0 state
- `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-CONTEXT.md` — 15 decisions D-01..D-15
- `.planning/phases/312-admin-full-delete-assessment-room/312-01-SUMMARY.md` — EnsureCanDeleteAsync helper pattern
- `.planning/phases/312-admin-full-delete-assessment-room/312-02-SUMMARY.md` — Playwright FLOW 12 + UAT.md pattern
- `.planning/config.json` — workflow flags (nyquist_validation: true)

### Secondary (MEDIUM confidence)
- (None — all critical claims VERIFIED via codebase Read/Grep)

### Tertiary (LOW confidence)
- (None — no WebSearch performed; all claims sourced from codebase, locked CONTEXT.md, or direct file Read)

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia mandatory** — semua user-facing text (error messages, banner, button label, AuditLog Description) harus Bahasa Indonesia. Helper variable names + code comments boleh English.
- **Develop workflow:** Lokal → Dev (10.55.3.3) → Prod. Edit kode HANYA di lokal, push hanya setelah `dotnet build` + `dotnet run` + cek `localhost:5277` lulus. Migration files (jika ada) wajib di-commit. Phase 313 = no migration (verified Runtime State Inventory).
- **Verification gate:** `dotnet build` lokal pass + manual smoke `dotnet run` + cek behavior di `http://localhost:5277/CMP/StartExam/{id}` SEBELUM commit/push.
- **No direct edit di server Dev/Prod** — promosi ke Dev/DB Dev = tanggung jawab Team IT. Notify dengan commit hash.
- **Playwright UAT supplemental, not primary** — manual UAT 313-UAT.md tetap mandatory sign-off untuk closure (Phase 312 precedent).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependencies verified existing (no new packages)
- Architecture: HIGH — Phase 312 EnsureCanDeleteAsync pattern direct precedent + verified field names
- Pitfalls: HIGH — Pitfall 1 (`assessment.Type` typo) and Pitfall 2 (wrong file) caught via direct codebase verification; Pitfall 5-7 inherited from Phase 312 review
- Validation: HIGH — Phase 312 FLOW 12 + UAT.md structure direct precedent
- Open Question A7 (auto-submit immediate timing): MEDIUM-HIGH risk, planner clarification needed

**Research date:** 2026-05-08
**Valid until:** 2026-06-08 (30 days — stable .NET 8 + repo internal patterns; no fast-moving external dependencies)
