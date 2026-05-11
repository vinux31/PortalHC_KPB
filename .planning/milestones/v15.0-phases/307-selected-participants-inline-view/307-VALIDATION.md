---
phase: 307
slug: selected-participants-inline-view
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-29
---

# Phase 307 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Sumber: Validation Architecture di `307-RESEARCH.md` (line 527-570).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.58.2 (E2E only — no .NET unit test project, no Jest/Vitest) |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307"` |
| **Full suite command** | `cd tests && npx playwright test` |
| **Estimated runtime** | ~30 seconds (quick) / ~2-4 menit (full) |

**Catatan critical:**
- TIDAK ADA framework unit test untuk JS — semua verification client-side via E2E atau manual UAT.
- Helper `renderSelectedParticipants()` adalah pure DOM function — bisa diuji via Playwright `page.evaluate()` (assert helper output) atau via E2E full-flow (toggle checkbox → assert panel).
- Performance budget #4 (50+ peserta < 200ms) **wajib manual UAT** dengan `performance.now()` instrumentation; E2E `expect.soft` toLess Than tidak deterministic untuk perf budget di CI.

---

## Sampling Rate

- **After every task commit:** Manual smoke browser (load CreateAssessment Create mode → toggle 2 checkbox → verify panel update visual). Optional `npx playwright test --grep "Phase 307"` kalau test sudah di-Wave-0.
- **After every plan wave:** Full suite `cd tests && npx playwright test` — pastikan zero regresi `assessment.spec.ts` existing.
- **Before `/gsd-verify-work`:** Full suite green + manual UAT 5-step Bahasa Indonesia (Phase 306 punya 10-step UAT pattern → mirror style).
- **Max feedback latency:** ~30 detik (smoke browser) per task; ~4 menit (full E2E) per wave.

---

## Per-Task Verification Map

> Plan IDs (`307-01`, `307-02`, dst) akan di-finalize oleh planner. Map di bawah berdasar logical task units yang muncul dari RESEARCH "Implementation Sketch" — planner WAJIB tarik daftar lengkap dan isi command konkret per task.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 307-W0-01 | Wave 0 | 0 | WIZ-01 #1-#5 | — | N/A (no auth path / no input handling) | E2E scaffold | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --list` | ❌ Wave 0 — extend `tests/e2e/assessment.spec.ts` dengan describe block "Phase 307 Selected Participants Panel" | ⬜ pending |
| 307-W0-02 | Wave 0 | 0 | WIZ-01 #1-#5 | — | N/A | Selectors helper | `grep -n "selected-participants" tests/e2e/helpers/wizardSelectors.ts` (or new file) | ❌ Wave 0 | ⬜ pending |
| 307-W0-03 | Wave 0 | 0 | (cleanup) | — | N/A | Test text fix | `grep -n "'2 terpilih'" tests/e2e/assessment.spec.ts` (must replace `'2 selected'`) | ❌ Wave 0 — pre-existing rot | ⬜ pending |
| 307-XX-01 | (TBD planner) | 1 | WIZ-01 #1 | — | N/A | E2E selector + text | `cd tests && npx playwright test --grep "panel renders after userCheckboxContainer"` | ❌ depends on W0 | ⬜ pending |
| 307-XX-02 | (TBD planner) | 1 | WIZ-01 #2 | — | N/A | E2E click + assert | `cd tests && npx playwright test --grep "real-time toggle"` | ❌ depends on W0 | ⬜ pending |
| 307-XX-03 | (TBD planner) | 2 | WIZ-01 #3, #5 | — | N/A | E2E parity (compare innerHTML) | `cd tests && npx playwright test --grep "Step 2 = Step 4 parity"` | ❌ depends on W0 | ⬜ pending |
| 307-XX-04 | (TBD planner) | 2 | WIZ-01 #4 | — | N/A | Manual UAT + `performance.now()` | (manual — see UAT script) | ❌ Wave 0 (UAT.md) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/assessment.spec.ts` — extend dengan `describe('Phase 307 Selected Participants Panel', () => { ... })` covering 5 success criteria (panel exists / real-time toggle / parity Step 2 vs Step 4 / count badge updates / expand button toggles)
- [ ] `tests/e2e/helpers/wizardSelectors.ts` (create or extend) — selector constants:
  - `SELECTED_PARTICIPANTS_PANEL = '#selected-participants-panel'`
  - `SELECTED_PARTICIPANTS_COUNT = '#selected-participants-count'`
  - `SELECTED_PARTICIPANTS_LIST = '#selected-participants-list'`
  - `SELECTED_PARTICIPANTS_EXPAND = '#selected-participants-expand'`
  - `SELECTED_PARTICIPANTS_EXTRA = '#selected-participants-extra'`
- [ ] Pre-existing test rot fix opportunistic: `tests/e2e/assessment.spec.ts:45` ganti `'2 selected'` → `'2 terpilih'`
- [ ] `.planning/phases/307-selected-participants-inline-view/307-UAT.md` — manual UAT 5-step Bahasa Indonesia (mirror Phase 306 10-step pattern):
  1. Load Create mode (kosong) → verify panel show "Belum ada peserta dipilih"
  2. Toggle 3 checkbox → verify badge `3 peserta`, list 3 nama, no expand button
  3. Toggle 8 checkbox → verify badge `8 peserta`, list 5 nama + button "...dan 3 lainnya"; click button → verify `<span class="d-none">` reveal
  4. Click "Pilih Semua" 50+ user → verify single render (debounce), capture `performance.now()` delta < 200ms
  5. Switch ke Proton mode → verify panel update sumber data; click "Buat lagi" reset → verify panel kembali ke empty state

(Tidak ada framework install needed — Playwright sudah ter-set di `tests/package.json`.)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Performance budget 50+ peserta < 200ms render | WIZ-01 #4 | Performance assertion di E2E flaky di CI (variable load). Manual UAT dengan `performance.now()` lebih reliable untuk capture render delta deterministik. | UAT.md Step 4 — instrumented click "Pilih Semua" + log delta + screenshot |
| Visual parity Step 2 vs Step 4 (rendering identik secara visual) | WIZ-01 #5 | E2E `innerHTML` compare verify struktur DOM, tapi visual parity (font, spacing, color) butuh mata manusia. | UAT.md Step 5 — side-by-side screenshot Step 2 panel vs Step 4 summary, sama 5 nama, sama format badge |
| Aria-live screen reader announce count (CD-07 if implemented) | (optional) | Screen reader behavior tidak bisa di-assert via Playwright headless. | UAT optional — open NVDA/JAWS, toggle checkbox, verify announcement "3 peserta dipilih" |
| Browser compat fallback `replaceChildren` (kalau planner pilih conservative path A3) | A3 (RESEARCH Assumption Log) | Browser version detection di test runner kompleks; manual verify di Edge legacy lebih cepat. | Manual — open Chrome <86 atau IE Mode di Edge, smoke test panel render |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (planner enforce ini saat task split)
- [ ] Wave 0 covers all MISSING references (3 items: spec extend, selectors helper, pre-existing rot fix)
- [ ] No watch-mode flags (Playwright run sekali, not `--ui` mode)
- [ ] Feedback latency < 30s per task (smoke browser) / < 4m per wave (full E2E)
- [ ] Manual UAT 5-step (UAT.md) di-create dan di-execute pre-merge
- [ ] Performance budget delta logged < 200ms (UAT Step 4 evidence)
- [ ] `nyquist_compliant: true` set in frontmatter setelah semua check ✅

**Approval:** pending
