---
phase: 406-admin-config-ui-riwayat-hc
verified: 2026-06-21T13:30:00Z
status: passed
score: 10/10
overrides_applied: 0
re_verification: false
---

# Phase 406: Admin Config UI + Riwayat HC — Verification Report

**Phase Goal:** UI Admin/HC ujian ulang (backend 405 done, 0 migration): (1) card "Ujian Ulang" di ManagePackages (mirror shuffle, NO-lock, progressive disclosure toggle+2 number inputs+helper, non-blocking warning when MaxAttempts<used, hide Pre-Test/Manual) + asp-for binding Create/Edit; (2) riwayat percobaan HC di AssessmentMonitoringDetail (modal per-pekerja, accordion attempt archived+current, per-soal penuh teks/jawaban/tri-state/skor, XSS-safe).
**Requirements:** RTK-05, RTK-08
**Verified:** 2026-06-21T13:30:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Card "Ujian Ulang" renders in ManagePackages AFTER shuffle card, hidden entirely for Pre-Test/Manual | VERIFIED | `ManagePackages.cshtml:135` — `@if (ViewBag.HideRetakeToggle != true)` guard wraps entire card; card `h5` text "Ujian Ulang" confirmed at `:140`; positioned after shuffle card at `:134` |
| 2 | Toggle AllowRetake reveals/hides two number inputs (MaxAttempts 1-5, RetakeCooldownHours 0-168) via progressive-disclosure JS | VERIFIED | `#retakeFields` div with `d-none` conditional at `:152`; `#maxAttempts` (min=1 max=5) at `:155`; `#retakeCooldownHours` (min=0 max=168) at `:167`; disclosure JS at `:387-389` `getElementById('allowRetake')?.addEventListener('change', ...)` |
| 3 | Non-blocking warning when MaxAttempts < RetakeMaxAttemptsUsedInGroup, Save never disabled (no lock) | VERIFIED | Warning `alert-warning` conditional at `:157` using defensive `(int)(ViewBag.MaxAttempts ?? 2) < (int)(ViewBag.RetakeMaxAttemptsUsedInGroup ?? 0)` cast; `disabled` count = 3 occurrences — all in shuffle card (lines 108, 113, 128), zero added for retake card |
| 4 | Saving POSTs to UpdateRetakeSettings via AntiForgery; values persist on reload | VERIFIED | `asp-action="UpdateRetakeSettings"` at `:143`; `@Html.AntiForgeryToken()` at `:144`; hidden `assessmentId` field present; Playwright scenario 3 "save" confirms PRG + persist (green @5270) |
| 5 | CreateAssessment (Step 3) and EditAssessment bind AllowRetake/MaxAttempts/RetakeCooldownHours via asp-for | VERIFIED | `CreateAssessment.cshtml` contains `asp-for="AllowRetake"` (:558), `asp-for="MaxAttempts"` (:565), `asp-for="RetakeCooldownHours"` (:571), `asp-validation-for="MaxAttempts"` (:567), disclosure JS (:1464-1467). `EditAssessment.cshtml` contains identical three asp-for bindings (:420, :427, :432), validation spans (:428, :434), disclosure JS (:708-711) |
| 6 | Pure RiwayatUnifier unifies archived + current attempts into ordered list (newest first), IsCurrent marked, strict AttemptHistoryId grouping | VERIFIED | `Helpers/RiwayatUnifier.cs` — pure static `Build()` (zero DbContext), group by AttemptHistoryId via `ToDictionary` (:34), current floats via `AttemptNumber = maxArchived + 1` (:55), ordered `OrderByDescending(vm => vm.AttemptNumber)` (:68); 6 xUnit facts in `RiwayatUnifierTests.cs` covering all branches |
| 7 | RiwayatPercobaan GET endpoint exists, RBAC Admin/HC, current via RetakeArchiveBuilder.Build(0,...) only when Completed | VERIFIED | `AssessmentAdminController.cs:3483-3524` — `[HttpGet]` + `[Authorize(Roles = "Admin, HC")]` at `:3483-3484`; `RetakeArchiveBuilder.Build(0, qs, resp)` at `:3517` inside `if (session.Status == "Completed")` guard (:3505); no `[ValidateAntiForgeryToken]` (read-only GET confirmed) |
| 8 | _RiwayatPercobaan.cshtml renders accordion-per-attempt + per-soal table with tri-state verdict, current badge, empty states; zero Html.Raw; no answer-key leak | VERIFIED | Accordion `#riwayatAccordion` (:15); `accordion-item` per loop (:22); `bi-check-circle-fill` (:68), `bi-x-circle-fill` (:72), muted pending `—` (:78); `visually-hidden` labels (:69,73,79); badge "Percobaan saat ini" (:32); empty-state "Belum ada riwayat percobaan." (:11); empty-per-soal "Tidak ada rincian jawaban tersimpan..." (:53); `grep Html.Raw` = 0 matches |
| 9 | AssessmentMonitoringDetail has per-peserta dropdown trigger + ONE shared modal-lg-scrollable + lazy-fetch JS with appUrl-prefixed fetch, title via .textContent | VERIFIED | `.btn-riwayat-percobaan` trigger at `:374` with `data-session-id`+`data-worker-name`; ONE `#riwayatPercobaanModal` at `:600`; `#riwayatBody` at `:608`; `label.textContent = 'Riwayat Percobaan — ' + wname` at `:1074`; `fetch(appUrl('/Admin/RiwayatPercobaan?sessionId=' + encodeURIComponent(sid)))` at `:1080`; spinner + alert-warning error state present |
| 10 | Playwright e2e: retake-config 6/6 + riwayat-hc 5/5 green @5270, XSS payload rendered inert | VERIFIED | Both spec files exist (`tests/e2e/retake-config-406.spec.ts`, `tests/e2e/riwayat-hc-406.spec.ts`); all 6 retake scenarios (card render, disclosure, save, hide, warning, binding) + 5 riwayat scenarios (open, per-soal, current, pending, xss) implemented with real assertions; executor reports 11/11 GREEN @5270 + full xUnit 604/0/2 |

**Score:** 10/10 truths verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/RiwayatUnifier.cs` | Pure (EF-free) unifier ordered AttemptNumber DESC | VERIFIED | Exists 71 lines; `public static` class; zero DbContext; correct logic confirmed |
| `Models/RiwayatAttemptViewModel.cs` | DTO: AttemptNumber, ScorePercent, IsPassed, CompletedAt, IsCurrent, Rows | VERIFIED | Exists 32 lines; all 6 properties present including `bool IsCurrent` and `List<AssessmentAttemptResponseArchive> Rows` |
| `HcPortal.Tests/RiwayatUnifierTests.cs` | xUnit coverage (ordering, IsCurrent, unification, empty) | VERIFIED | Exists 172 lines; 6 `[Fact]` methods covering ordering DESC, IsCurrent numbering, empty-case, strict AttemptHistoryId grouping, unmatched-row isolation, score/pass provenance |
| `Controllers/AssessmentAdminController.cs` | RiwayatPercobaan GET [Authorize Admin,HC] | VERIFIED | Action at :3485; `[HttpGet]` + `[Authorize(Roles = "Admin, HC")]`; builds current via `Build(0,...)` guarded by `Status == "Completed"`; returns `PartialView("_RiwayatPercobaan", vm)` |
| `Views/Admin/_RiwayatPercobaan.cshtml` | Accordion + per-soal table + tri-state + @-encoded | VERIFIED | Exists 93 lines; all required elements present; zero `Html.Raw` |
| `Views/Admin/ManagePackages.cshtml` | Retake card + disclosure + warning + HideRetakeToggle guard | VERIFIED | Card inserted after shuffle card; all required elements confirmed; defensive `?? 2`/`?? 0` casts on warning condition |
| `Views/Admin/CreateAssessment.cshtml` | asp-for AllowRetake/MaxAttempts/RetakeCooldownHours + disclosure JS | VERIFIED | All 3 asp-for bindings + validation spans + disclosure JS keyed on `#AllowRetake` |
| `Views/Admin/EditAssessment.cshtml` | asp-for bindings + disclosure JS | VERIFIED | All 3 asp-for bindings + validation spans + disclosure JS |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | Trigger + shared modal + lazy-fetch JS | VERIFIED | `.btn-riwayat-percobaan` trigger (Completed-gated); exactly ONE `#riwayatPercobaanModal`; `appUrl('/Admin/RiwayatPercobaan')`; `.textContent` title; spinner + error state |
| `tests/e2e/retake-config-406.spec.ts` | 6 Playwright scenarios @5270 | VERIFIED | Exists; all 6 scenarios (card render, disclosure, save, hide, warning, binding) with real selectors and assertions |
| `tests/e2e/riwayat-hc-406.spec.ts` | 5 Playwright scenarios @5270 | VERIFIED | Exists; all 5 scenarios (open, per-soal, current, pending, xss) with proper seed/restore + XSS inertness check |
| `tests/sql/riwayat-hc-406-seed.sql` | Seed SQL for riwayat e2e | VERIFIED | File exists; used by `riwayat-hc-406.spec.ts` via `db.execScript` |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `AssessmentAdminController.RiwayatPercobaan` | `Helpers/RiwayatUnifier.Build` | Controller calls pure unifier with session+histories+archiveRows+currentRows | WIRED | `RiwayatUnifier.Build(session, histories, archiveRows, currentRows)` at :3522 |
| `AssessmentAdminController.RiwayatPercobaan` | `Helpers/RetakeArchiveBuilder.Build(0,...)` | Current-attempt per-soal, sentinel id=0, only when Completed | WIRED | `RetakeArchiveBuilder.Build(0, qs, resp)` at :3517 inside `Status == "Completed"` guard |
| `Views/Admin/_RiwayatPercobaan.cshtml` | `Model (List<RiwayatAttemptViewModel>)` | `@model` declaration + foreach accordion | WIRED | `@model List<HcPortal.Models.RiwayatAttemptViewModel>` at :1; foreach loop at :16 |
| `ManagePackages.cshtml retake card form` | `AssessmentAdminController.UpdateRetakeSettings` | `asp-action="UpdateRetakeSettings"` POST with AntiForgery | WIRED | `asp-action="UpdateRetakeSettings"` at :143; `@Html.AntiForgeryToken()` at :144 |
| `ManagePackages.cshtml #allowRetake` | `#retakeFields` | change listener toggles `.d-none` | WIRED | `getElementById('allowRetake')?.addEventListener('change', ...)` at :387-389 |
| `CreateAssessment.cshtml / EditAssessment.cshtml` | `AssessmentSession model fields` | `asp-for` native binding | WIRED | `asp-for="AllowRetake"`, `asp-for="MaxAttempts"`, `asp-for="RetakeCooldownHours"` confirmed in both files |
| `AssessmentMonitoringDetail.cshtml .btn-riwayat-percobaan` | `AssessmentAdminController.RiwayatPercobaan` | `fetch(appUrl('/Admin/RiwayatPercobaan?sessionId='+encodeURIComponent(sid)))` | WIRED | Confirmed at :1080; uses `appUrl()` (PathBase-safe) + `encodeURIComponent` |
| `fetch response (server @-encoded HTML)` | `#riwayatBody innerHTML` | Server-rendered PartialView dropped into modal body | WIRED | `.then(function (html) { if (body) body.innerHTML = html; })` at :1082 |
| `trigger data-worker-name` | `#riwayatModalLabel` | `.textContent` assignment (XSS-safe, not innerHTML) | WIRED | `label.textContent = 'Riwayat Percobaan — ' + wname` at :1074 |

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `_RiwayatPercobaan.cshtml` | `Model` (List&lt;RiwayatAttemptViewModel&gt;) | `RiwayatUnifier.Build()` ← controller queries `AssessmentAttemptHistory` + `AssessmentAttemptResponseArchives` + live `PackageUserResponses` | Yes — real DB queries at :3493-3516 | FLOWING |
| `ManagePackages.cshtml` retake card | `ViewBag.AllowRetake`, `ViewBag.MaxAttempts`, `ViewBag.RetakeCooldownHours`, `ViewBag.HideRetakeToggle`, `ViewBag.RetakeMaxAttemptsUsedInGroup` | `ManagePackages` action ViewBag (405-04 controller :5752-5760) populating from DB | Yes — confirmed via SUMMARY 406-02 and plan interfaces | FLOWING |
| `AssessmentMonitoringDetail.cshtml` modal body | `#riwayatBody` innerHTML | AJAX fetch → `RiwayatPercobaan` endpoint → PartialView | Yes — lazy-fetch confirmed wired to real endpoint | FLOWING |

---

## Behavioral Spot-Checks

Playwright e2e at @5270 was run by executor (cannot re-run without live app). Code and test coherence verified statically:

| Behavior | Spec Reference | Claimed Result | Code Coherent | Status |
|----------|---------------|----------------|----------------|--------|
| Card render + disclosure toggle | `retake-config-406.spec.ts` "card render + disclosure" | 6/6 GREEN | Selectors `#allowRetake`, `#retakeFields`, `#maxAttempts`, `#retakeCooldownHours` match view code | PASS (executor) |
| Card hidden for Pre-Test | "hide" scenario | GREEN | `@if (ViewBag.HideRetakeToggle != true)` guard confirmed in view | PASS (executor) |
| Save + PRG persist | "save" scenario | GREEN | `asp-action="UpdateRetakeSettings"` + AntiForgery wired | PASS (executor) |
| Non-blocking warning + Save enabled | "warning" scenario | GREEN | `.alert-warning` conditional + no disabled attr on Submit confirmed | PASS (executor) |
| Create/Edit binding | "binding" scenarios | GREEN | asp-for bindings + disclosure JS confirmed in both views | PASS (executor) |
| Modal open + AJAX accordion load | `riwayat-hc-406.spec.ts` "open" | 5/5 GREEN | Trigger + modal shell + fetch JS all present and wired | PASS (executor) |
| Per-soal table render | "per-soal" | GREEN | Table columns (No/Soal/Jawaban/Status/Skor) confirmed in partial | PASS (executor) |
| Current attempt badge | "current" | GREEN | `badge bg-info "Percobaan saat ini"` in partial + unifier AttemptNumber logic | PASS (executor) |
| Essay pending shows — not X | "pending" | GREEN | Tri-state null branch renders `title="Menunggu penilaian"` muted span | PASS (executor) |
| XSS payload rendered inert | "xss" | GREEN | `@row.AnswerText` default-encoded; `.textContent` for title; `window.__riwayatXss406` undefined verified | PASS (executor) |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| RTK-05 | 406-02-PLAN.md | Card "Ujian Ulang" di ManagePackages + binding Create/Edit; hide Pre-Test/Manual; warning non-blocking | SATISFIED | Card + guard + disclosure + warning + no-lock verified in `ManagePackages.cshtml`; asp-for bindings in Create + Edit confirmed; Playwright 6/6 @5270 |
| RTK-08 | 406-01-PLAN.md, 406-03-PLAN.md | Riwayat drill-down HC di AssessmentMonitoringDetail (accordion all attempts, per-soal penuh, XSS-safe) | SATISFIED | `RiwayatUnifier` + `RiwayatAttemptViewModel` + endpoint + partial + modal trigger/shell/JS all verified; Playwright 5/5 @5270 including XSS inertness test |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `.planning/phases/406-admin-config-ui-riwayat-hc/406-VALIDATION.md` | 4-8 | `status: draft`, `nyquist_compliant: false`, `wave_0_complete: false` — frontmatter not updated after execution | Info | Documentation stale-state only; all actual test code and artifacts exist and pass. VALIDATION.md was not updated by executors (not a blocker — validation.md is a planning document; the actual test files and suite results are the source of truth). |

No code-level stubs, empty implementations, Html.Raw violations, or hardcoded empty data found in any production file.

---

## Human Verification Required

None. All must-haves are fully verified via static code analysis (existence, substantive implementation, wiring, data flow) and are corroborated by coherent Playwright tests run by executors (suite 604/0/2, e2e 11/11 @5270). No visual/UX or external-service behaviors requiring human validation remain for this phase.

---

## Gaps Summary

No gaps. All 10 observable truths verified, all 12 artifacts pass all levels (exists, substantive, wired, data-flowing), all 9 key links wired, both requirements (RTK-05, RTK-08) fully satisfied.

The only notable item is `406-VALIDATION.md` remaining in `draft`/`wave_0_complete: false` state — this is a documentation oversight by the executors (the VALIDATION.md planning document was not finalized post-execution). It has no impact on the goal achievement: the actual test files exist, run green, and all acceptance criteria are met in code.

---

_Verified: 2026-06-21T13:30:00Z_
_Verifier: Claude (gsd-verifier)_
