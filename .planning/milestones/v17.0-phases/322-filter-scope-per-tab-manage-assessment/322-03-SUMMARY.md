---
phase: 322-filter-scope-per-tab-manage-assessment
plan: 03
subsystem: testing
tags: [uat, playwright, htmx, verification]

# Dependency graph
requires:
  - phase: 322-01
    provides: 3 partial views filter HTMX
  - phase: 322-02
    provides: Shell view cleanup + wrapper hx-vals D-21 Strategy D
provides:
  - "322-UAT.md verdict 11/12 PASS + 1 N/A"
  - "2 bug critical post-UAT fix: ViewBag null coalesce + wrapper hx-vals → URL migration"
  - "Phase 322 ready for tag + IT handoff"
affects: []

tech-stack:
  added: []
  patterns:
    - "HTMX hx-vals inheritance gotcha — ancestor hx-vals override descendant form data"
    - "Wrapper-initiated-only param: bake into hx-get URL query string (avoid hx-vals inheritance)"

key-files:
  created:
    - .planning/phases/322-filter-scope-per-tab-manage-assessment/322-UAT.md
  modified:
    - Controllers/AssessmentAdminController.cs (post-UAT fix coalesce nulls)
    - Views/Admin/ManageAssessment.cshtml (post-UAT fix wrapper hx-vals → URL)

key-decisions:
  - "UAT execution via Playwright MCP automation (vs manual browser) — repeatable + verifiable"
  - "Critical bug discovery: HTMX hx-vals inherits ke descendant HTMX triggers (undocumented gotcha) — caused all filter dropdowns to send empty params"
  - "Fix migrated wrapper hx-vals → URL query string; URL params hanya apply ke wrapper's own request (no inheritance)"
  - "Step 3 (pagination) N/A — DB lokal 1 grup insufficient; bonus fix verified via code review only"

patterns-established:
  - "Playwright MCP automated UAT pattern: navigate → snapshot → evaluate DOM + form data + network → assert"
  - "HTMX form data + ancestor hx-vals interaction: use URL query string for wrapper-only params, hx-include='closest form' + descendant triggers for user-driven params"

requirements-completed: []

# Metrics
duration: ~45min (UAT execution + 2 bug fix iterations)
completed: 2026-05-22
---

# Phase 322 Plan 03: Manual UAT + Handoff

**11/12 PASS + 1 N/A via Playwright MCP. 2 critical bug discovered + fixed during UAT (ViewBag null coalesce + wrapper hx-vals inheritance). Phase 322 ready for IT handoff.**

## Performance

- **Duration:** ~45 menit (UAT execution + 2 bug iteration)
- **Tasks:** 1/1 (Task 7 UAT)
- **Files created:** 1 (`322-UAT.md`)
- **Bug fixes during UAT:** 2 (commit `6ecb7a50` + `773c970c`)

## Accomplishments

### Task 7 — Playwright Automated UAT (12-step)

**Golden Path (Step 1-8):**
- Step 1 PASS: no double filter Tab 1
- Step 2 PASS: HTMX filter Tab 1 (bug discovery → fix `773c970c`)
- Step 3 N/A: pagination (DB lokal 1 grup, bonus fix verified by code review)
- Step 4 PASS: Tab 2 5-field filter granular
- Step 5 PASS: cascade Bagian→Unit
- Step 6 PASS: Tab 3 sub-tab structure
- Step 7 PASS: Riwayat Training filter NEW (DOM hooks Task 4 deliverable verified)
- Step 8 PASS: client-side filter Training (xhrChange=0)

**Edge (Step 9-12):**
- Step 9 PASS: reset filter Tab 1
- Step 10 PASS: error state via `renderHtmxError` simulation
- Step 11 PASS: lazy `once` tab switch preserved
- Step 12a PASS: URL bookmark Tab 1 backward compat
- Step 12b PASS: D-21 Strategy D Bug 2 prevention by-design CONFIRMED

### 2 Critical Bug Fixed During UAT

**1. Bug: ViewBag string nulls → JSON null → URL-encoded "null" literal**
- Discovery: Textbox `value="null"` (string) saat initial load, Hapus Pencarian link visible despite no actual search term
- Root cause: `@Json.Serialize(new { search = ViewBag.SearchTerm })` produces `{"search":null}` → HTMX hx-vals URL-encode `null` JSON value sebagai string `"null"`
- Fix commit `6ecb7a50`: Controller coalesce `?? ""` semua ViewBag string field (search/section/unit)

**2. Bug: Wrapper hx-vals inherit ke descendant HTMX triggers (CRITICAL)**
- Discovery: Dropdown change "OJT" → XHR sent `category=` empty despite form FormData showing `category=OJT`
- Root cause: HTMX hx-vals attribute INHERITS dari ancestor element ke descendants. Wrapper `<div hx-vals='{"category":""}'>` override descendant form's `category=OJT` via attribute inheritance
- Impact tanpa fix: ALL filter dropdowns send empty params; filter functionality broken
- Fix commit `773c970c`: Migrate wrapper `hx-vals` → wrapper `hx-get` URL query string. URL params hanya apply untuk wrapper's own request, tidak inherit ke descendants

## Task Commits

1. **Task 7 init: UAT checklist 12-step** — `54f7eac7` (docs)
2. **Bug fix 1: ViewBag null coalesce** — `6ecb7a50` (fix)
3. **Bug fix 2: wrapper hx-vals → URL** — `773c970c` (fix)
4. **Task 7 verdict: UAT result PASS** — `7921813d` (docs)

## Files Modified

- `Controllers/AssessmentAdminController.cs` — post-UAT bug fix coalesce nulls
- `Views/Admin/ManageAssessment.cshtml` — post-UAT bug fix wrapper hx-vals → URL query string
- `.planning/phases/322-filter-scope-per-tab-manage-assessment/322-UAT.md` — verdict per step + handoff section

## Key Learnings (untuk Phase berikutnya)

**HTMX hx-vals inheritance gotcha:**
- `hx-vals` attribute inherits dari ancestor ke descendant HTMX triggers
- Inheritance applies even untuk descendant requests yang punya `hx-include="closest form"`
- ancestor hx-vals MENANG over descendant form data
- **Solution untuk wrapper-only params:** bake ke wrapper `hx-get` URL query string (URL hanya apply ke that wrapper's own request)
- **Solution untuk user-driven params (form/dropdown):** descendant pakai `hx-include="closest form"` tanpa ancestor hx-vals interference

**JSON.Serialize null handling:**
- `@Json.Serialize(new { x = nullableValue })` produces `{"x":null}` JSON
- HTMX hx-vals dengan JSON null → URL-encode jadi literal string `"null"`
- **Solution:** Coalesce nullable values ke empty string `?? ""` di server-side, OR strip null fields dari object sebelum serialize

## Next Step

Tag `v17.0-p322-complete` + STATE.md update phase 322 SHIPPED + ROADMAP.md mark complete + user push origin/main + IT handoff (commit hash range + tag + NO-MIGRATION flag).
