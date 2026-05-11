# Phase 308: PrePost Wizard Validation Fix - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-29
**Phase:** 308-prepost-wizard-validation-fix
**Mode:** auto (--auto flag)
**Areas discussed:** Status default value, server-side ModelState removal, jQuery validate re-parse, mode-switching state, regression guard, test scaffolding, file conflict mitigation, manual UAT approach

---

## Status Field Default Value untuk PrePost Mode

| Option | Description | Selected |
|--------|-------------|----------|
| `'Upcoming'` (set in JS handler) | Konsisten dengan server-side default Pre-Post sessions di line 1078/1112/1170. ROADMAP success criteria #1 explicit. | ✓ |
| `'Open'` (set in JS handler) | Konsisten dengan defensive fallback line 977 untuk empty Status. Mismatch dengan server PrePost session creation default. | |
| Empty string (rely entirely on server default) | Minimal client touch; risk: kalau server default fallback line 975-978 di-skip karena ModelState invalid → tetap fail. | |

**Auto-selected:** `'Upcoming'` (recommended — matches ROADMAP success criteria #1 and server-side PrePost session creation pattern)
**Notes:** D-01 captures this decision. Server default fallback "Open" line 977 tetap untuk Standard mode defensive.

---

## Server-Side Conditional ModelState Removal

| Option | Description | Selected |
|--------|-------------|----------|
| `if (isPrePostMode) ModelState.Remove("Status");` di line 780-781 | Mirror pattern existing `ModelState.Remove("UserId")` line 742, `ModelState.Remove("AccessToken")` line 756. Minimal touch. | ✓ |
| Hapus `[Required]` attribute dari model `Status` property | Risk regresi untuk Standard mode (akan butuh validasi manual gantinya). Out of scope (touch model file). | |
| Always set `model.Status = "Upcoming"` jika empty di line 975 | Modifying defensive default; risk silent coercion untuk Standard mode kalau JS Phase 308 fail. | |

**Auto-selected:** Conditional `ModelState.Remove` (recommended — pattern alignment + minimal blast radius)
**Notes:** D-04, D-05 capture insertion point. Defensive default line 975-978 UNCHANGED.

---

## jQuery Validate Re-Parse Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| `removeData('validator').removeData('unobtrusiveValidation')` + `unobtrusive.parse()` | Standard pattern untuk dynamic field show/hide dengan ASP.NET unobtrusive validation. Idempotent. | ✓ |
| Manual class manipulation (`$('#Status').rules('remove')`) | More targeted but fragile — depends on validator metadata structure. Not recommended for dynamic show/hide. | |
| Skip re-parse, rely on HTML5 default | Won't clear stale unobtrusive validation cache; "Status field is required" can persist as stale message. | |

**Auto-selected:** removeData + unobtrusive.parse (recommended — standard ASP.NET pattern)
**Notes:** D-07, D-08, D-09 capture call site, ordering, dan defensive guard for plugin availability.

---

## Mode-Switching State Preservation

| Option | Description | Selected |
|--------|-------------|----------|
| S→PP: auto-set "Upcoming"; PP→S: clear value (force re-pick) | Cleanest UX — user explicitly re-picks Status when they revert to Standard mode. Stale "Upcoming" from accidental switch cleared. | ✓ |
| Always preserve last value (no clear on switch) | Risk: user goes S→PP→S, "Upcoming" stays selected → submit Standard with stale Status. Acceptable but less defensive. | |
| Always clear on any switch | Force user re-pick even after S→PP→S where they had picked "Open" first. Annoying for power users. | |

**Auto-selected:** Asymmetric (S→PP set, PP→S clear) (recommended — defensive without UX friction)
**Notes:** D-02, D-10 capture both paths and the 4-test matrix.

---

## Regression Guard untuk Standard Mode

| Option | Description | Selected |
|--------|-------------|----------|
| Conditional `ModelState.Remove` only fires when `isPrePostMode == true` | Standard mode validator tetap fully active. ROADMAP success criteria #5 satisfied. | ✓ |
| Always remove + manual server check | Adds cognitive complexity; mirror Standard validation logic in two places. | |
| Trust HTML5 `required` di markup | Bypassed if user crafts custom POST. Defense-in-depth violated. | |

**Auto-selected:** Conditional only (recommended — mirrors existing pattern, ROADMAP-aligned)
**Notes:** D-11, D-12 capture this. Verify via test 8.X regression check (Standard tanpa Status → "Status wajib dipilih" tetap muncul).

---

## Test Scaffolding (Wave 0 Mirror Phase 307 Pattern)

| Option | Description | Selected |
|--------|-------------|----------|
| Wave 0 (test scaffold) → Wave 1 (impl) split | Phase 307 precedent: provides RED → GREEN cycle, eliminates ad-hoc test risk, reuses wizardSelectors centrally. | ✓ |
| Single wave (impl + test inline) | Lower overhead untuk small phase; risk: less rigorous test coverage. | |
| Manual UAT only, no E2E | Faster; risk: no automated regression for 4-test matrix; future refactor breaks invisible. | |

**Auto-selected:** Wave 0 + Wave 1 split (recommended — Phase 307 precedent works)
**Notes:** D-13, D-14, D-18, D-19 capture scaffold scope (FLOW 8 describe with 4 tests + UAT.md 4-step + selector extension).

---

## File Conflict Mitigation (Phase 307 Adjacency)

| Option | Description | Selected |
|--------|-------------|----------|
| Re-grep anchors before edit (verify line numbers) | Phase 307 added +47 lines; ROADMAP refs (1790-1807, 778) shifted. Researcher WAJIB grep untuk locate `value === 'PrePostTest'` dan `applyProtonMode`. | ✓ |
| Trust ROADMAP line numbers | High risk — Phase 307 just landed today; +47 lines net push shifts everything below. Edit at wrong line will break file. | |
| Wait for ROADMAP normalization | Out of scope — ROADMAP refs are advisory, not canonical. Phase 308 should self-locate anchors. | |

**Auto-selected:** Re-grep before edit (recommended — defensive after recent Phase 307)
**Notes:** D-15, D-16, D-17 capture sequencing, conflict-zero with helper area, dan anchor patterns.

---

## Manual UAT Approach

| Option | Description | Selected |
|--------|-------------|----------|
| 4-step UAT mirror Phase 307 5-step pattern | Match D-10 test matrix (Standard saja, S→PP→S, PP saja, PP→S→PP). Sign-off section + tester name. | ✓ |
| Skip manual UAT, rely on E2E only | Risk: UI subtle behavior (visual state, transitions) tidak ter-cover otomatis. | |
| 10-step deep UAT (Phase 306 pattern) | Overkill untuk single-scenario fix; matrix sudah cover 4 paths. | |

**Auto-selected:** 4-step matrix UAT (recommended — matches success criteria #4 directly)
**Notes:** D-18, D-19 capture step structure dan sign-off pattern.

---

## Claude's Discretion

- **CD-01:** Form `id` selector verification — assume `#createForm` based on antiforgery wrapper, but planner re-grep untuk konfirmasi.
- **CD-02:** Comment style untuk `ModelState.Remove("Status")` — match line 755 pattern.
- **CD-03:** Wave 0 selector test extension scope — extend wizardSelectors centrally vs test-local. Default: extend wizardSelectors (DRY).

## Deferred Ideas

- **Wizard return-to-step-1 UX enhancement** (T11 differentiator) — explicit Out of Scope table in REQUIREMENTS.md
- **Wizard state machine refactor** (resumeStep/goToStep generalization) — di luar audit scope
- **jQuery removal / vanilla validation migration** — di luar audit scope, milestone v16+ kandidat

---

*Auto mode — all gray areas resolved with recommended defaults. Review CONTEXT.md before plan-phase if any decision needs override.*
