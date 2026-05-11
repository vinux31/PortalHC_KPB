---
phase: 312
slug: admin-full-delete-assessment-room
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-07
---

# Phase 312 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Sumber: 312-RESEARCH.md §"Validation Architecture" (line 856-895).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright `^1.58.2` (e2e) + `dotnet build` (backend type/build check) + manual UAT (DB-side AuditLog verification) |
| **Config file** | `tests/playwright.config.ts` (existing) |
| **Quick run command** | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 312" --reporter=list` |
| **Full suite command** | `cd tests && npx playwright test` |
| **Build verify** | `dotnet build` |
| **Estimated runtime (quick)** | ~30–60 s (6 Playwright tests, depends on browser warmup) |
| **Estimated runtime (full)** | ~3–5 min (full Playwright e2e suite) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (zero new warnings) + `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 312" --reporter=list`
- **After every plan wave:** Run full Playwright suite (`cd tests && npx playwright test`) + manual UAT check (DB AuditLog query)
- **Before `/gsd-verify-work`:** Full suite GREEN + 312-UAT.md sign-off
- **Max feedback latency:** ~60 s (quick run pada FLOW 12)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 312-01-01 | 01 | 1 | DEL-01 (AC#1) | T-312-01 (Privilege Escalation) | HC POST direct ke `/Admin/DeleteAssessment` dengan session Completed → reject + AuditLog blocked entry | E2E (Playwright) | `cd tests && npx playwright test --grep "12.4\|12.5\|12.6"` | ❌ Wave 0 | ⬜ pending |
| 312-01-02 | 01 | 1 | DEL-01 (AC#2) | — | `[Authorize(Roles = "Admin, HC")]` attribute literal di line 2020/2125/2230 unchanged | grep verify | `grep -n 'Authorize.*Admin, HC' Controllers/AssessmentAdminController.cs` | ✅ existing | ⬜ pending |
| 312-01-03 | 01 | 1 | DEL-01 (AC#4) | T-312-03 (Audit) | AuditLog row dengan ActionType="DeleteAssessment" + Description LIKE '%Status=%ResponseCount=%' (success); ActionType ending "Blocked" untuk failed attempts | DB query (manual SQL post-test) | `SELECT TOP 5 ActionType, Description, CreatedAt FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC` | ✅ DB exists | ⬜ pending |
| 312-01-04 | 01 | 1 | DEL-01 (AC#5) | — | Cascade utuh: pre-delete count(PackageUserResponses, AttemptHistory, AssessmentPackages, UserPackageAssignments, AssessmentSessions); post-delete = 0 untuk session_id terkait | E2E + DB sanity | manual SQL pre/post counts | ✅ existing pattern | ⬜ pending |
| 312-01-05 | 01 | 1 | DEL-01 (AC#1) | T-312-01 | `GetDeleteImpact(int id)` HttpGet returns valid JSON (Status, ResponseCount, CertificateCount, PackageCount, AttemptHistoryCount; PrePost: per-session breakdown) | E2E (assert AJAX response) | `cd tests && npx playwright test --grep "12.0"` (helper) | ❌ Wave 0 | ⬜ pending |
| 312-02-01 | 02 | 2 | DEL-01 (AC#3) | — | UI conditional: HC user lihat ManageAssessment → tombol Hapus tidak ada untuk row Status=Completed | E2E (Playwright login `hc` + assert button absence) | `cd tests && npx playwright test --grep "12.2"` | ❌ Wave 0 | ⬜ pending |
| 312-02-02 | 02 | 2 | DEL-01 (AC#3) | — | Admin user lihat ManageAssessment → tombol Hapus selalu render untuk semua row regardless Status | E2E (login `admin` + assert button presence) | `cd tests && npx playwright test --grep "12.1"` | ❌ Wave 0 | ⬜ pending |
| 312-02-03 | 02 | 2 | DEL-01 (D-02 modal) | — | Modal 2-panel impact preview: panel 1 menampilkan Status/ResponseCount/PackageCount; panel 2 final confirm dengan warning text "Tidak bisa di-undo" | E2E (Playwright drive modal flow) | `cd tests && npx playwright test --grep "12.3"` | ❌ Wave 0 | ⬜ pending |
| 312-02-04 | 02 | 2 | DEL-01 (AC#6) | T-312-01,T-312-03 | 6 skenario: Admin+Open OK / Admin+Completed OK / HC+Open(no-response) OK / HC+Completed BLOCK / HC+Open(with-response) BLOCK / HC+PrePost+Completed BLOCK | E2E (Playwright FLOW 12 full) | `cd tests && npx playwright test --grep "Phase 312"` | ❌ Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Threat references** (defined in PLAN.md `<threat_model>` blocks):
- **T-312-01** Privilege escalation: HC bypass via direct POST (skip UI) — Mitigation: server-side `EnsureCanDeleteAsync` body guard (PRIMARY defense)
- **T-312-02** CSRF on destructive action — Mitigation: existing `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` di modal form
- **T-312-03** Audit log gaps (missing failure audit) — Mitigation: D-03 log blocked attempts dengan ActionType `{Action}Blocked`
- **T-312-04** Information disclosure via GetDeleteImpact endpoint (HC bisa enumerate session data via JSON) — Mitigation: apply `[Authorize(Roles = "Admin, HC")]` (sudah dibatasi tier; tidak expose ke Worker/Coach)

---

## Wave 0 Requirements

- [ ] `tests/e2e/assessment.spec.ts` — extend dengan `test.describe('FLOW 12: Phase 312 - Admin Full-Delete Role Guard')` block (6 tests: 12.1–12.6 + 12.0 helper untuk GetDeleteImpact JSON)
- [ ] `tests/e2e/helpers/wizardSelectors.ts` (atau inline di test) — selectors untuk `#deleteAssessmentModal`, `#dam-status`, `#dam-response-count`, `#dam-next-btn`, `#dam-back-btn`
- [ ] Test fixture seeding: setup data via `AkhiriUjian` endpoint via Playwright admin login (NOT direct DB write) — pattern parity dengan existing test seeding
- [ ] `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` — manual UAT 6-step Bahasa Indonesia (template: `308-UAT.md` / `310-UAT.md`)

**No framework install needed** — Playwright already at `^1.58.2`, dotnet SDK 8 sudah ada, accounts (`admin@pertamina.com`, `meylisa.tjiang@pertamina.com`) sudah di `tests/helpers/accounts.ts`.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| AuditLog Description includes `Status=` + `ResponseCount=` (success); `{Action}Blocked` ActionType + reason (failed) | DEL-01 AC#4, D-03 | DB-side assertion sulit di-Playwright tanpa direct SQL connection helper; lebih reliable manual SQL post-action | After running FLOW 12 quick suite, query `SELECT TOP 10 ActionType, Description, CreatedAt FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC`. Assert: success rows contain Description text matching `Status=...` AND `ResponseCount=...`; blocked rows contain ActionType ending `Blocked` AND reason text |
| Cascade integrity: PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages, UserPackageAssignments fully removed | DEL-01 AC#5 | Pre/post row count assertion butuh transactional snapshot — easier manual via SSMS / DBeaver | Pre-test: capture counts dengan `SELECT COUNT(*) FROM PackageUserResponses WHERE AssessmentSessionId=@id` (repeat for 4 child tables). Run delete via UI. Post-test: re-run counts, expect 0 |
| Modal accessibility (keyboard navigation, focus trap, screen reader) | D-02 polish | Playwright keyboard tests reliable tapi accessibility tooling (axe-core) tidak terintegrasi di repo saat ini | Manual UAT step di 312-UAT.md: Tab through modal panels, verify focus stays inside modal, Esc dismisses, screen reader announces panel content |
| Bahasa Indonesia copy review (modal text, error TempData, blocked reason) | CLAUDE.md project instruction | Subjective phrasing review; tidak suitable untuk automated assertion | Manual UAT step: read all user-visible text di modal + TempData["Error"], confirm formal Bahasa Indonesia tone (precedent: Phase 304 polish) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify command OR documented Wave 0 dependency
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (current: every task has Playwright/grep/SQL automated step)
- [ ] Wave 0 covers all MISSING references (FLOW 12 describe block, selectors, UAT.md)
- [ ] No watch-mode flags (`--reporter=list` non-interactive default)
- [ ] Feedback latency < 60 s on quick run
- [ ] `nyquist_compliant: true` set in frontmatter setelah Wave 0 implementation

**Approval:** pending — set `nyquist_compliant: true` setelah Wave 0 tests merged + sign-off di 312-UAT.md
