---
phase: 406
slug: 406-admin-config-ui-riwayat-hc
status: verified
threats_total: 13
threats_closed: 13
threats_open: 0
asvs_level: 1
created: 2026-06-21
---

# Phase 406 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> ASVS Level 1. block_on: high (threats_open:0 = SECURED).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser (Admin/HC) -> RiwayatPercobaan GET | Authenticated Admin/HC requests another worker's attempt history via sessionId (untrusted int param) | Worker exam answers (QuestionText, AnswerText, verdict, score) |
| Archive/live DB -> PartialView HTML | Stored worker/author content (QuestionText, AnswerText) rendered into HTML | Potentially user-authored content susceptible to stored XSS |
| Browser (Admin/HC) -> UpdateRetakeSettings POST | Untrusted form values (maxAttempts, retakeCooldownHours) cross into config persistence | Numeric config values that could be out-of-range if bypassed |
| Client number inputs (min/max) | Client-side range is advisory only; bypassed by direct form submission | Numeric bounds for MaxAttempts (1-5) and RetakeCooldownHours (0-168) |
| Browser fetch -> RiwayatPercobaan GET | Client requests worker riwayat by sessionId; server-side RBAC re-checks on every fetch | Worker exam answer data |
| Server partial HTML -> modal innerHTML | Server-rendered (@-encoded) HTML injected into the DOM via innerHTML | Pre-encoded HTML (safe if encoding is correct) |
| data-worker-name -> modal title | Worker name (user content) crosses into DOM title element | Worker name string |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-406-01 | Information disclosure | RiwayatPercobaan GET (answer history of any worker) | mitigate | `[Authorize(Roles = "Admin, HC")]` on the action — Controllers/AssessmentAdminController.cs:3484. Identical RBAC to AssessmentMonitoringDetail (:3290). | closed |
| T-406-02 | Information disclosure | Per-soal cells leaking the correct-answer KEY | mitigate | Partial renders ONLY worker AnswerText + verdict glyph + AwardedScore — never PackageOption.IsCorrect or correct option text. Views/Admin/_RiwayatPercobaan.cshtml:73-74 (`@row.QuestionText`, `@row.AnswerText`). Verdict pre-computed server-side in RetakeArchiveBuilder. No answer-key field present in AssessmentAttemptResponseArchive. | closed |
| T-406-03 | Tampering (stored XSS) | QuestionText / AnswerText in _RiwayatPercobaan.cshtml | mitigate | Razor `@` default HTML-encoding on every user-content field at _RiwayatPercobaan.cshtml:73,74. Zero `Html.Raw` occurrences confirmed (grep returns 0). Playwright "xss" scenario in riwayat-hc-406.spec.ts proves `<script>` payload inert (`window.__riwayatXss406` stays undefined). | closed |
| T-406-04 | Elevation / IDOR | sessionId param addresses an arbitrary session | accept | Admin/HC role gates the entire monitoring surface — any Admin/HC reviewing any worker's riwayat is the intended capability. Per-worker ownership check is a worker-endpoint concern (Phase 407, out of scope). Consistent with existing monitoring surfaces (EssayGrading, EditHistoryPartial, AssessmentMonitoringDetail all role-only). Documented in Accepted Risks Log. | closed |
| T-406-05 | Information disclosure | Current-attempt rows for an in-progress (not-Completed) session | mitigate | Current rows built ONLY when `session.Status == "Completed"` — Controllers/AssessmentAdminController.cs:3505. In-progress sessions yield empty currentRows (no partial-answer leak). | closed |
| T-406-06 | Tampering (CSRF) | UpdateRetakeSettings config save POST | mitigate | `@Html.AntiForgeryToken()` on the retake card form at Views/Admin/ManagePackages.cshtml:144. `[ValidateAntiForgeryToken]` on UpdateRetakeSettings endpoint at Controllers/AssessmentAdminController.cs:5615. | closed |
| T-406-07 | Tampering | Client bypass of input min/max -> out-of-range MaxAttempts/cooldown | mitigate | Server-side `Math.Clamp(maxAttempts, 1, 5)` and `Math.Clamp(retakeCooldownHours, 0, 168)` at Controllers/AssessmentAdminController.cs:5629-5630. Input min/max attributes are UX hints only; clamp is the real guard. | closed |
| T-406-08 | Elevation | Non-Admin/HC reaching the config endpoint | accept (mitigated upstream) | `[Authorize(Roles = "Admin, HC")]` on UpdateRetakeSettings (Controllers/AssessmentAdminController.cs:5614) and on ManagePackages action. View only renders inside the authorized page; no new endpoint introduced. Documented in Accepted Risks Log. | closed |
| T-406-09 | Information disclosure | Retake card shown where retake is invalid (Pre-Test/Manual) | mitigate | `@if (ViewBag.HideRetakeToggle != true)` guard at Views/Admin/ManagePackages.cshtml:135 (backed by RetakeRules.ShouldHideRetakeToggle). Card not rendered for Pre-Test/Manual assessment types. Playwright "hide" scenario verifies absence of `#allowRetake` for Pre-Test. | closed |
| T-406-10 | Tampering (DOM XSS) | Modal title set from data-worker-name attribute | mitigate | Title set via `.textContent` (NOT innerHTML) at Views/Admin/AssessmentMonitoringDetail.cshtml:1074: `label.textContent = 'Riwayat Percobaan — ' + wname;`. Worker name never parsed as HTML. | closed |
| T-406-11 | Tampering (stored XSS) | Partial HTML dropped into #riwayatBody via innerHTML | mitigate | Body HTML comes from the SERVER PartialView (_RiwayatPercobaan.cshtml) where every user field is Razor `@`-encoded. innerHTML of already-encoded server HTML is safe; no raw client string assembly. AssessmentMonitoringDetail.cshtml:1082: `body.innerHTML = html` (comment confirms "server-rendered @@-encoded HTML"). Playwright "xss" e2e proves inertness. | closed |
| T-406-12 | Information disclosure | fetch reaching the riwayat of any session without RBAC re-check | accept (mitigated upstream) | Endpoint carries `[Authorize(Admin,HC)]` (Controllers/AssessmentAdminController.cs:3484) — re-checked on every AJAX fetch independently of the page. Page itself is Admin/HC-only. Per-worker ownership is Phase 407 worker-endpoint concern. Documented in Accepted Risks Log. | closed |
| T-406-13 | Denial of service | appUrl missing -> 404 on Dev sub-path (broken modal) | mitigate | fetch uses `appUrl('/Admin/RiwayatPercobaan?sessionId=' + encodeURIComponent(sid))` at Views/Admin/AssessmentMonitoringDetail.cshtml:1080. No bare `/Admin/...` string. `encodeURIComponent` prevents sessionId injection in query string. PathBase-aware (Lesson 385 PXF-01). | closed |

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-406-01 | T-406-04 | IDOR by-design: Admin/HC can view any worker's riwayat by sessionId without per-worker ownership check. Consistent with all existing monitoring surfaces (EssayGrading, EditHistoryPartial, AssessmentMonitoringDetail — all role-only, no unit-scope). Per-worker ownership is a Phase 407 worker-endpoint concern; if HC unit-scoping is activated app-wide in v32.3+, this endpoint must be updated alongside AssessmentMonitoringDetail. | Phase executor (406-01 PLAN disposition) | 2026-06-21 |
| AR-406-02 | T-406-08 | Non-Admin/HC reaching UpdateRetakeSettings is mitigated upstream by `[Authorize(Roles = "Admin, HC")]` on the endpoint (confirmed). Logged as accepted because the plan disposition is "accept (already mitigated upstream)" — the mitigation is present, no separate control needed in this plan. | Phase executor (406-02 PLAN disposition) | 2026-06-21 |
| AR-406-03 | T-406-12 | RBAC re-check on AJAX fetch: endpoint `[Authorize(Admin,HC)]` re-checked by ASP.NET authorization middleware on every GET request independently, including AJAX calls. Accepted with same rationale as AR-406-01 (role-only, no per-unit ownership, consistent with existing surfaces). | Phase executor (406-03 PLAN disposition) | 2026-06-21 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-21 | 13 | 13 | 0 | Claude (gsd-secure-phase), ASVS L1 |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-21
