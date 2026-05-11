---
phase: 313-block-manual-submit-saat-waktu-habis
plan: 02
subsystem: backend / controller / audit
tags: [tmr-01, life-03, timer-enforcement, audit-log, server-side-guard, phase-313, helper-extraction]
requirements: [TMR-01]
dependency-graph:
  requires:
    - "Controllers/CMPController.cs (existing SubmitExam method, DI fields)"
    - "Models/AssessmentConstants.cs (AssessmentType constants)"
    - "Models/AssessmentSession.cs (AssessmentType field — line 154)"
    - "Services/AuditLogService.cs (LogAsync signature)"
    - "Models/ApplicationUser.cs (NIP + FullName fields)"
  provides:
    - "EnsureCanSubmitExamAsync(AssessmentSession, bool) — 2-tier timer guard helper"
    - "WriteSubmitBlockedAuditAsync(AssessmentSession, TimeSpan, int) — AuditLog blocked entry helper dengan try/catch swallow"
    - "AuditLog ActionType=SubmitExamBlocked dengan Description format key=value (Type/ElapsedMin/AllowedMin/SessionId)"
  affects:
    - "POST /CMP/SubmitExam — manual submit setelah waktu habis sekarang strict 0-grace reject (Tier-1 NEW)"
    - "POST /CMP/SubmitExam — auto submit grace 2min preserved (Tier-2 existing LIFE-03)"
    - "Manual AssessmentType + null AssessmentType di-skip guard (D-15 defense-in-depth)"
tech-stack:
  added: []
  patterns:
    - "Helper extraction (private async Task<IActionResult?>) — Phase 312 EnsureCanDeleteAsync precedent"
    - "Try/catch swallow audit write dengan _logger.LogWarning fallback (Phase 312 T-306-02)"
    - "AssessmentConstants.AssessmentType.* (anti-magic-string per C-01)"
    - "AuditLog ActionType `{Action}Blocked` convention (Phase 312 D-03)"
key-files:
  created: []
  modified:
    - "Controllers/CMPController.cs (line 1616-1621: invocation; line 4527-4621: 2 helper privat)"
decisions:
  - "Helper diletakkan di akhir class sebelum closing brace — mirror Phase 312 D-04 placement convention"
  - "NO transaction wrap di helper — guard read-only check + audit write only, no state mutation (intentional deviation per RESEARCH)"
  - "Tier-2 audit entry TIDAK ditulis — D-06 scope minimal hanya tier-1 (NEW behavior)"
  - "DI fields existing (_userManager, _auditLog, _logger) reused — no constructor modification"
  - "Tier-2 message verbatim preserved dari existing line 1625 ('Pengiriman jawaban tidak dapat diproses')"
metrics:
  duration: "~10 minutes"
  completed: "2026-05-08T01:53:18Z"
  tasks_completed: 2
  files_modified: 1
  lines_added: 95 (Task 1) + 6 (Task 2 invocation)
  lines_removed: 13 (Task 2 inline block)
  net_lines_delta: "+88 lines"
---

# Phase 313 Plan 02: Backend Implementation EnsureCanSubmitExamAsync Summary

Backend SubmitExam dipasang 2-tier timer guard via helper extraction: tier-1 strict 0-grace reject manual + AuditLog SubmitExamBlocked, tier-2 grace 2min preserved untuk auto-submit.

## Outcomes

### Task 1: Helper privat `EnsureCanSubmitExamAsync` + `WriteSubmitBlockedAuditAsync`

- **File:** `Controllers/CMPController.cs`
- **Lines added:** 4527-4621 (95 baris) — di akhir class sebelum closing brace `}` (mirror Phase 312 placement convention)
- **Commit:** `677b3fc7` — `feat(313-02): add EnsureCanSubmitExamAsync + WriteSubmitBlockedAuditAsync helpers`
- **Helpers:**
  - `private async Task<IActionResult?> EnsureCanSubmitExamAsync(AssessmentSession assessment, bool isAutoSubmit)` — returns `null` (pass) atau `RedirectToAction` IActionResult (reject)
  - `private async Task WriteSubmitBlockedAuditAsync(AssessmentSession assessment, TimeSpan elapsed, int allowedMinutes)` — try/catch swallow audit write
- **Branching logic:**
  1. **D-15 / C-01 Manual exclude:** kalau `assessment.AssessmentType` BUKAN salah satu dari `Online/PreTest/PostTest` constants → return `null` (skip guard, caller lanjut)
  2. **Legacy session:** `StartedAt == null` → return `null` (skip)
  3. **Tier 1 NEW (D-09):** `!isAutoSubmit && elapsed > Duration+ExtraTime` → write AuditLog Blocked + TempData D-01 + redirect StartExam
  4. **Tier 2 existing (D-06):** `elapsed > Duration+ExtraTime+2min grace` → TempData verbatim "Pengiriman jawaban tidak dapat diproses" + redirect StartExam (TIDAK tulis Blocked entry — scope minimal)
  5. **Pass:** return `null` (caller lanjut grading flow)

### Task 2: Replace existing LIFE-03 inline block dengan helper invocation

- **File:** `Controllers/CMPController.cs`
- **Lines modified:** 1616-1621 (replace 13-line inline block dengan 6-line invocation + comment header)
- **Commit:** `4712acf7` — `refactor(313-02): replace LIFE-03 inline block with EnsureCanSubmitExamAsync invocation`
- **Posisi:** SETELAH `serverTimerExpired` calculation block (line 1580-1614, preserved unchanged) dan SEBELUM `packageAssignment` query (line 1623, preserved unchanged)
- **Invocation:**
  ```csharp
  // ---- Server-side timer enforcement (LIFE-03 + Phase 313 2-tier TMR-01) ----
  // Phase 313: 2-tier branching — manual reject tanpa grace (D-09), auto reject setelah grace (existing).
  // Helper extraction mirror Phase 312 EnsureCanDeleteAsync pattern (D-04 lock, body-method placement).
  // AssessmentType Manual exclude di-handle dalam helper (D-15 defense-in-depth).
  var timerBlockResult = await EnsureCanSubmitExamAsync(assessment, isAutoSubmit);
  if (timerBlockResult != null) return timerBlockResult;
  ```

## `dotnet build` Final Output

```
92 Warning(s)
0 Error(s)
Time Elapsed 00:00:23.67
```

- **Errors:** 0 ✓ (compile gate satisfied)
- **Warnings:** 92 — IDENTIK dengan baseline Phase 307+308 (LDAP CA1416 platform-specific + CS8602 nullable di file lain + MVC1000 RecordsTeam.cshtml). **Tidak ada warning baru di CMPController.cs dari helper Phase 313.**

## D-01, D-05 Verbatim String Verification (grep results)

| String | Expected | Actual | Status |
|--------|----------|--------|--------|
| `Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman.` (D-01) | 1 | 1 | ✓ |
| `HC/User role manual submit blocked after timeup. Type=` (D-05 prefix) | 1 | 1 | ✓ |
| `SubmitExamBlocked` (D-05 ActionType) | 3 (1 ActionType literal + 2 string log/comment) | 3 | ✓ |
| `Pengiriman jawaban tidak dapat diproses` (D-06 tier-2 message preserved) | 1 (di helper only, inline block di-replace) | 1 | ✓ |

## C-01 Compliance: Field Name Correctness

| Check | Expected | Actual | Status |
|-------|----------|--------|--------|
| `assessment.Type ==` (anti-pattern, field salah) | 0 | 0 | ✓ |
| `assessment.AssessmentType` references di helper | ≥3 (Manual exclude check + Description format) | 4 | ✓ |

Field name correct per `Models/AssessmentSession.cs:154` — bukan `Type`.

## D-15 Compliance: AssessmentConstants Usage (Anti-Magic-String)

| Constant | Count | Status |
|----------|-------|--------|
| `AssessmentConstants.AssessmentType.Online` | 1 | ✓ |
| `AssessmentConstants.AssessmentType.PreTest` | 1 | ✓ |
| `AssessmentConstants.AssessmentType.PostTest` | 1 | ✓ |

Tidak ada hardcoded magic string `"Online"` / `"PreTest"` / `"PostTest"` di guard logic. All comparisons via constants.

## Helper Invocation Counts

| Symbol | Expected | Actual | Status |
|--------|----------|--------|--------|
| `EnsureCanSubmitExamAsync` | 2 (declaration + 1 call site) | 2 | ✓ |
| `WriteSubmitBlockedAuditAsync` | 2 (declaration + 1 call dari Tier-1 branch) | 2 | ✓ |
| `Phase 313 2-tier TMR-01` (anchor traceability comment) | 1 | 1 | ✓ |

## NO Transaction Wrap (Intentional Deviation)

**Verified:** `BeginTransactionAsync` count di window line 4520-4625 (helper area) = **0**.

Rasional (per RESEARCH.md Pitfall 6 + Pattern 1):
- Phase 313 guard adalah **read-only check + audit write only** — tidak ada state mutation di guard itself
- Multiple Blocked entries dari concurrent attempts = informational noise (acceptable)
- Existing GradingService idempotency cover write path (Phase 309 precedent)
- Phase 312 `EnsureCanDeleteAsync` pakai BeginTransactionAsync karena ada cascade DELETE yang perlu rollback — Phase 313 tidak applicable

## Threat Mitigation Status

| Threat ID | Description | Mitigation | Status |
|-----------|-------------|-----------|--------|
| T-313-01 | Manual submit bypass via `isAutoSubmit=false` after timeup | Tier-1 strict 0-grace reject di `EnsureCanSubmitExamAsync` (PRIMARY) | ✓ Mitigated |
| T-313-02 | DevTools force `isAutoSubmit=true` to skip Tier-1 | Tier-2 enforces hard cap `Duration+ExtraTime+2min` regardless of `isAutoSubmit` flag | ✓ Mitigated (defense-in-depth) |
| T-313-03 | Audit gap untuk blocked attempts (no traceability) | AuditLog `SubmitExamBlocked` entry per Tier-1 reject dengan Description key=value (D-05) | ✓ Mitigated |
| T-313-04 | AuditLog DB exception block primary action | Try/catch swallow di `WriteSubmitBlockedAuditAsync` dengan `_logger.LogWarning` fallback | ✓ Mitigated |
| T-313-06 | Manual type assessment incorrectly blocked | D-15 defense-in-depth: `AssessmentType` field check via constants → Manual / null skip | ✓ Mitigated |
| T-313-08 | CSRF replay POST `/CMP/SubmitExam` via stale form | `[ValidateAntiForgeryToken]` (line 1555 — preserved unchanged) | ✓ Preserved |

## Deviations from Plan

**None — plan executed exactly as written.**

Semua spec di Plan 02 PATTERNS section 1.B/1.C/1.D di-implementasi verbatim:
- Helper signature, body, dan placement match Pattern 1.C
- AuditLog helper match Pattern 1.D
- Invocation match Pattern 1.B
- NO transaction wrap intentional deviation di-honor (Pattern 1.E)
- D-01 + D-05 string literals verbatim
- AssessmentConstants usage konsisten

## Note untuk Plan 03 Executor

**Backend ready.** Frontend modifications next:
- `Views/CMP/ExamSummary.cshtml`: modify Submit button conditional render saat `timerExpired=true` (D-03 disable + spinner + label "Waktu Habis - Submit Otomatis Berjalan...") + retry 3x backoff JS handler (D-10)
- `Views/CMP/StartExam.cshtml`: modify timer countdown auto-submit flow per C-03 (modal tetap muncul info-only + submit fire paralel langsung, hapus `setTimeout(10000)`)
- TempData["Error"] rendering sudah verified via `_Layout.cshtml:199-208` (Pitfall 3 already mitigated — no work needed)

## Note untuk UAT Operator

**Pre-conditions:**
1. App running lokal: `dotnet build` + `dotnet run` (cek `http://localhost:5277/`)
2. Login fixture: **Coachee:** `rino.prasetyo@pertamina.com / 123456`
3. Wave 0 fixtures: jalankan `.planning/seeds/313-timer-fixtures.sql` (Plan 01 deliverable) untuk seed 7 fixture timer matrix
4. DB akses (SSMS / DBeaver) untuk AuditLog spot-check via SQL

**FLOW 313 GREEN transition expected** untuk skenario:
- 313.2 (Manual + after-time + Online): Tier-1 BLOCK + redirect StartExam + TempData D-01 banner + AuditLog `SubmitExamBlocked` row
- 313.5 (Manual + after-time + PreTest): Tier-1 BLOCK + Type=PreTest di Description
- 313.6 (Manual + after-time + PostTest): Tier-1 BLOCK + Type=PostTest di Description
- 313.7 (Manual + after-time + Manual type): submit OK (D-15 exclude verify) — TIDAK ada Blocked entry

**AuditLog spot-check SQL:**
```sql
SELECT TOP 5 ActionType, Description, TargetId, CreatedAt
FROM AuditLogs
WHERE ActionType = 'SubmitExamBlocked'
ORDER BY CreatedAt DESC;
```

Expect rows dengan format Description: `HC/User role manual submit blocked after timeup. Type={Online|PreTest|PostTest} ElapsedMin={X} AllowedMin={Y} SessionId={id}`.

## Self-Check: PASSED

Files verified to exist:
- FOUND: `Controllers/CMPController.cs` (modified, 4613 lines after Task 1+2 — start 4525, +88 net)

Commits verified to exist:
- FOUND: `677b3fc7` (Task 1 — `feat(313-02): add EnsureCanSubmitExamAsync + WriteSubmitBlockedAuditAsync helpers`)
- FOUND: `4712acf7` (Task 2 — `refactor(313-02): replace LIFE-03 inline block with EnsureCanSubmitExamAsync invocation`)

Acceptance criteria all checked (see grep results above):
- [x] 2 helper privat ada di akhir CMPController class
- [x] AssessmentConstants constants used (no magic string)
- [x] D-01 + D-05 EXACT strings present (verbatim verified)
- [x] Try/catch swallow pattern present (`_logger.LogWarning(auditEx,` count=1)
- [x] Existing LIFE-03 inline block replaced dengan helper invocation
- [x] Comment trail updated (LIFE-03 + Phase 313 2-tier TMR-01)
- [x] `dotnet build --nologo` exit 0, no warnings baru di CMPController.cs (92 baseline preserved)
- [x] `grep -c "EnsureCanSubmitExamAsync"` = 2 (declaration + call)
- [x] `grep -c "WriteSubmitBlockedAuditAsync"` = 2 (declaration + 1 call dari helper)
- [x] `grep -c "SubmitExamBlocked"` = 3 (1 ActionType literal + 2 string log/comment)
- [x] `grep -c "assessment.Type =="` = 0 (anti-pattern absent — C-01 honored)
- [x] NO BeginTransactionAsync di helper area (intentional deviation honored)
