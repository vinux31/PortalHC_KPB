---
phase: 306
slug: score-editable-per-question-type
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-28
---

# Phase 306 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Source: `306-RESEARCH.md` § Validation Architecture (Nyquist).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright v1.58.2 (E2E browser tests) — no C# unit test framework in this codebase |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `dotnet build -c Debug` (build smoke) |
| **Full suite command** | `cd tests && npx playwright test e2e/assessment.spec.ts` |
| **Estimated runtime** | ~2 menit (build) + ~3 menit (Playwright assessment.spec.ts regression) |

**Codebase precedent (phase 304/305):** Admin form changes verified via manual UAT script — automated E2E author untuk QSCR-01 marked as Wave 0 deferred (medium effort, dapat ditunda ke phase follow-up).

---

## Sampling Rate

- **After every task commit:** `dotnet build -c Debug` (must pass — compilation gate)
- **After every plan wave:** `cd tests && npx playwright test e2e/assessment.spec.ts` (regression — verify Phase 305 LBL labels masih intact)
- **Before `/gsd-verify-work`:** Manual UAT script 10 checks (lihat below) — full pass
- **Max feedback latency:** ~120 detik (build) per commit

---

## Per-Task Verification Map

> Plans akan populate kolom Task ID + Plan + Wave saat planning. Tabel ini adalah requirement-level baseline untuk planner reference.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | QSCR-01 | T-306-01 | Server reject scoreValue < 1 atau > 100 dengan TempData["Error"] | manual + DevTools | Manual: open DevTools, hapus `min/max`, submit `-5` → flash error muncul | ❌ Wave 0 (manual UAT script) | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | T-306-02 | Audit log entry created saat score change pada question dengan sessions | DB query | `SELECT * FROM AuditLogs WHERE ActionType LIKE 'EditQuestion-ScoreChange%' ORDER BY CreatedAt DESC LIMIT 5` | ✅ DB existing | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | T-306-03 | AuditLog entry capture oldScore, newScore, affectedSessionsCount | DB query | Same as above — verify Description column berisi `ScoreValue: {old} → {new}` | ✅ DB existing | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Modal "Peringatan Ubah Skor" muncul untuk question dengan completed sessions, score change | manual UAT | Manual step 4 di UAT script | ❌ Wave 0 (manual UAT) | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Modal TIDAK muncul untuk question tanpa sessions | manual UAT | Manual step 10 di UAT script | ❌ Wave 0 (manual UAT) | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Stored Score di Completed sessions tidak retroactively recalculate (D-19) | DB query | Manual: catat `AssessmentSessions.Score` sebelum edit, edit ScoreValue, re-query — value unchanged | ✅ DB existing | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Total Points header tampil sum benar (D-17) | manual visual | Manual step 1 di UAT script | ❌ Wave 0 (manual UAT) | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Switch dropdown tipe DI-PRESERVE user-entered value (D-02) | manual visual | Manual step 9 di UAT script | ❌ Wave 0 (manual UAT) | ⬜ pending |
| TBD | TBD | TBD | QSCR-01 | — | Existing Phase 305 LBL labels regression-clean | E2E Playwright | `cd tests && npx playwright test e2e/assessment.spec.ts` exit 0 | ✅ Existing test suite | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Author manual UAT script (10 checks) embedded di final SUMMARY.md atau `/gsd-verify-work` runtime
- [ ] No new test files needed — existing `tests/e2e/assessment.spec.ts` covers regression baseline
- [ ] No framework install needed — `dotnet build` + Playwright sudah established

*(Decision: ZERO automated test author untuk QSCR-01 — match phase 304/305 precedent. Manual UAT adalah default untuk admin form changes di codebase ini.)*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Modal "Peringatan Ubah Skor" muncul saat affectedSessions > 0 dan score change | QSCR-01 | Memerlukan seeded data (completed session dengan answers) — efforts authoring Playwright fixture tinggi vs benefit | UAT step 4: edit question dengan completed session, ubah ScoreValue, expect modal popup |
| HTML5 native validation tooltip (Chrome/Edge) | QSCR-01 | Browser-rendered, sulit di-assert via Playwright tanpa visual snapshot | UAT step 1: type "150" di input scoreValue, focus blur atau submit → expect Chrome tooltip "Value must be ≤ 100" |
| Bootstrap modal styling (bg-warning header, btn-warning button) konsisten dengan editTypeWarningModal existing | QSCR-01 | Visual consistency check, subjective | UAT: open both modals (Ubah Tipe + Ubah Skor), verify visual parity |
| Score audit log Description format readable (admin-facing) | QSCR-01 | UI display di `/Admin/AuditLog` view — subjective readability | Manual: open AuditLog page setelah edit, verify Description "ScoreValue: 10 → 20 (3 sessions affected)" tampil readable |
| DevTools bypass test (ASVS V5 server-side defense) | QSCR-01 / T-306-01 | Memerlukan DevTools manipulation — tidak otomatis | UAT step 8: F12 → hapus `min/max`, submit `-5`, expect flash error |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify (compile gate via `dotnet build`) atau Wave 0 manual UAT mapping
- [ ] Sampling continuity: tiap task commit triggered build; tiap wave triggered Playwright regression
- [ ] Wave 0 covers all MISSING references — confirmed manual UAT script lengkap (10 steps)
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s (build) per task
- [ ] `nyquist_compliant: true` set in frontmatter setelah planner finalize task IDs

**Approval:** pending (akan di-approve setelah planner populate task IDs)
