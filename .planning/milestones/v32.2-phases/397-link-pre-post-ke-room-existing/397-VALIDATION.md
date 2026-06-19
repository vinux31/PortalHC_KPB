---
phase: 397
slug: link-pre-post-ke-room-existing
status: finalized
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-18
updated: 2026-06-18
---

# Phase 397 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail kontrak validasi (testable contracts) ada di `397-RESEARCH.md` §Validation Architecture.
> **Post-execution audit 2026-06-18:** semua Wave 0 test ditulis (RED) lalu GREEN; 0 gap. Lihat audit trail di bawah.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests`) + Playwright (`tests/e2e`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` + `playwright.config.ts` |
| **Quick run command** | `dotnet test --filter Category!=Integration` (fast suite) |
| **Full suite command** | `dotnet test` (+ `cd tests && npx playwright test --workers=1` untuk e2e) |
| **Estimated runtime** | fast ~3s (390 test); integration real-SQL + e2e lebih lama |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category!=Integration`
- **After every plan wave:** Run `dotnet test` (full, termasuk Integration real-SQL)
- **Before `/gsd-verify-work`:** Full suite + Playwright e2e must be green
- **Max feedback latency:** ~3s (fast suite)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 397-01-01 | 01 | 1 | INJ-12 | T-397-01 | DTO/VM carry link contract (server re-resolves) | build | `dotnet build HcPortal.csproj` | ✅ | ✅ green |
| 397-01-02 | 01 | 1 | INJ-12 | T-397-03 | RED→GREEN: per-worker link, Kasus A/B, atomic rollback, anti-double, unlink revert | integration | `dotnet test --filter "FullyQualifiedName~InjectLink\|FullyQualifiedName~AntiDoubleLink\|FullyQualifiedName~UnlinkInject"` | ✅ | ✅ green |
| 397-01-03 | 01 | 1 | INJ-12 | T-397-02 | RED→GREEN: preview==commit pairing + cross inject↔online grouping intact (§13 KRITIS) | integration | `dotnet test --filter "FullyQualifiedName~PreviewPairing\|FullyQualifiedName~CrossGrouping"` | ✅ | ✅ green |
| 397-02-01 | 02 | 2 | INJ-12 | T-397-04 | Per-worker bidirectional link + Kasus A/B write-to-online (online Score/Status untouched), atomic | integration | `dotnet test --filter "FullyQualifiedName~InjectLink"` | ✅ | ✅ green |
| 397-02-02 | 02 | 2 | INJ-12 | T-397-06 | Anti-double reject full list (D-08) + PreviewPairingAsync dry-run no-write (D-07) | integration | `dotnet test --filter "FullyQualifiedName~AntiDoubleLink\|FullyQualifiedName~PreviewPairing"` | ✅ | ✅ green |
| 397-02-03 | 02 | 2 | INJ-12 | T-397-05, T-397-07 | UnlinkInjectGroupAsync atomic revert + audit "LinkPrePostUndo" (D-12), IDOR guard | integration | `dotnet test --filter "FullyQualifiedName~UnlinkInject\|FullyQualifiedName~CrossGrouping"` | ✅ | ✅ green |
| 397-03-01 | 03 | 3 | INJ-12 | T-397-09, T-397-11, T-397-13 | SearchLinkTargets JSON (RBAC, opposite-type whitelist, parameterized) + MapToRequest link wiring | build/unit | `dotnet build HcPortal.csproj` + `dotnet test --filter "FullyQualifiedName~InjectViewModelMap"` | ✅ | ✅ green |
| 397-03-02 | 03 | 3 | INJ-12 | T-397-10, T-397-12 | PreviewPairing POST dry-run (CSRF) + UnlinkInjectGroup POST (RBAC+CSRF, Json shape) | build/e2e | `dotnet build HcPortal.csproj` + e2e (397-04-03) | ✅ | ✅ green |
| 397-04-01 | 04 | 4 | INJ-12 | T-397-14 | Step-1 trigger + chip + room-picker modal (XSS-safe .textContent) | build + e2e | `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` | ✅ | ✅ green |
| 397-04-02 | 04 | 4 | INJ-12 | T-397-14, T-397-15, T-397-16 | Pairing summary + anti-double entries + unlink confirm modal (Bootstrap, not native) | build + e2e | `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` | ✅ | ✅ green |
| 397-04-03 | 04 | 4 | INJ-12 | T-397-17 | Playwright runtime (modal/picker/chip/preview/unlink) + cross-grouping intact + 0-migration gate | e2e (Playwright) | `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` | ✅ | ✅ green (6/6) |
| 397-04-04 | 04 | 4 | INJ-12 | T-397-17 | Human-verify link Pre/Post end-to-end (checkpoint, exempt from automated verify) | manual | manual (checkpoint:human-verify — see Manual-Only) | n/a | ✅ done (UAT 9/9) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Note: every `auto` task has an `<automated>` verify (build / `dotnet test` / Playwright). The single checkpoint task (397-04-04, `checkpoint:human-verify`) is exempt from the Nyquist automated-verify requirement → `nyquist_compliant: true`. Coverage state confirmed green this session: integration 15/15, fast suite 390/390 (incl. InjectViewModelMapTests 5), Playwright e2e 6/6.*

---

## Wave 0 Requirements

- [x] xUnit suite untuk wiring link (LinkedGroupId adopt/Kasus-B-write, per-pekerja LinkedSessionId bidirectional, atomic rollback, anti-double, preview==commit pairing, cross-grouping §13, unlink revert) — `HcPortal.Tests/` (5 integration files + InjectViewModelMapTests, 20 test) ✅ GREEN
- [x] Playwright e2e (modal picker + chip + ringkasan pairing + unlink confirm — runtime Razor/JS, lesson Phase 354) — `tests/e2e/inject-assessment-397.spec.ts` (6 contract) ✅ GREEN

*wave_0_complete: true — semua RED→GREEN.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Outcome |
|----------|-------------|------------|-------------------|---------|
| Pasangan silang inject↔online tampil utuh di /CMP/Records + gain-score | INJ-12 | UAT browser live (data online disentuh — Kasus B) | localhost:5277 AD-off: inject Pre → link ke Post online standalone → cek pasangan + gain-score; snapshot+restore DB (CLAUDE.md Seed) | ✅ DONE 2026-06-18 (orchestrator live browser UAT 9/9; §13: inject Pre 174 + online Post 173 share LinkedGroupId=173 + UserId, online Score=85/Completed UNCHANGED, audit LinkPrePost×1; unlink revert + LinkPrePostUndo×2; SEED_JOURNAL Phase-397 CLEANED) |

*Sebagian besar perilaku punya automated verify; UAT browser konfirmasi "seakan online" — selesai 9/9.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s (~3s fast suite)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (post-execution audit 2026-06-18 — 0 gaps)

---

## Validation Audit 2026-06-18
| Metric | Count |
|--------|-------|
| Requirements (INJ-12) | 1 |
| COVERED (automated) | 11/11 task rows (10 auto + 1 manual-done) |
| Gaps found | 0 |
| Resolved | 0 (none needed — TDD built all tests upfront) |
| Escalated | 0 |
| Manual-only (done) | 1 (cross-grouping UAT — DONE 9/9) |

**Verdict:** NYQUIST-COMPLIANT, wave_0_complete:true, 0 gaps. Coverage corroborated by verifier (PASSED 9/9) + secure (SECURED 17/17) + live UAT (9/9). State A audit: no test generation needed.
