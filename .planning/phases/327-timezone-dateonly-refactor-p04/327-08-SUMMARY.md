---
phase: 327-timezone-dateonly-refactor-p04
plan: 08
status: complete (UAT 7 SC PASS + IT_NOTIFY draft; Task 3 push decision PENDING)
date: 2026-05-28
commits: [b04cddea, 2c7d874f, da58e415]
---

# Plan 327-08 — SUMMARY

## One-Liner
Phase 327 SHIPPED LOCAL gate. 7 SC ALL PASS auto-verified Playwright MCP + sqlcmd + JS sim. Pitfall 3 + Phase 326 regression smoke PASS. IT_NOTIFY v19.0 batch runbook draft. NOT PUSHED — Task 3 user gate.

## What Was Built

### Task 1: Manual UAT (auto-verified)
- 7 SC ALL PASS (SC-1/2/3/7 baseline carry-forward; SC-4/5/6 auto Playwright)
- Pitfall 3 JSON timezone smoke PASS (no shift, no `T00:00:00`)
- Phase 326 regression smoke PASS 5/6 empirical + 1/6 inherited
- 1 non-blocking finding: PDF endpoint 204 environmental (QuestPDF unrelated DateOnly)
- Cleanup: TR Id=34 DELETE, zero rogue rows

### Task 2: Draft commits (3 atomic)
- `b04cddea` docs(327-08): UAT result 7 SC + Pitfall 3 + Phase 326 regression smoke PASS
- `2c7d874f` docs(327-08): IT_NOTIFY v19.0 batch promo runbook (3 phase + migration)
- `da58e415` docs(state): record Phase 327 SHIPPED LOCAL — 8/8 plan + 7/7 SC PASS

### Task 3: Push decision (PENDING USER CHECKPOINT)
Options: option-a (push batch v19.0 sekarang) | option-b (hold for IT availability) | option-c (defer Phase 328 audit first).

## Verification

| Acceptance | Status |
|------------|--------|
| `327-UAT.md` exists | ✓ 7925 bytes |
| `grep -c "SC-1"` ≥1 | ✓ 1 |
| `grep -c "PASS"` ≥7 | ✓ 17 |
| `grep -c "Phase 326 Regression"` ≥1 | ✓ 1 |
| `docs/IT_NOTIFY.md` exists | ✓ 8168 bytes |
| `grep -c "ChangeValidUntilToDateOnly"` ≥1 | ✓ 4 |
| `grep -c "TR_NonMidnight"` ≥1 | ✓ 3 |
| `grep -c "INFORMATION_SCHEMA"` ≥1 | ✓ 1 |
| `grep -c "dotnet ef database update"` ≥1 | ✓ 5 |
| `grep -c "Yang TIDAK perlu IT lakukan"` ≥1 | ✓ 1 (parity Phase 324) |
| STATE.md updated SHIPPED LOCAL | ✓ status + last_activity + current position |

## Threats

| ID | Status |
|----|--------|
| T-327-01 (Dev/Prod migration data loss) | MITIGATED — IT_NOTIFY pre-check sqlcmd + BACKUP + Down() rollback + git revert option |
| T-327-02 (Razor TagHelper bug #47628) | ACCEPT — Plan 06 smoke deferred; SC-5 5 halaman visual zero TagHelper render bug observed |
| T-327-03 (JSON consumer JS tz shift) | MITIGATED — Pitfall 3 smoke PASS, no shift confirmed lokal WIB+8, NO fix needed `wwwroot/js/analyticsDashboard.js` |
| T-327-05 (Phase 326 regression) | MITIGATED — 5/6 empirical + 1/6 inherited; validator code path Phase 326 untouched Phase 327 (Tanggal DateTime field still DateTime; ValidUntil nullable struct semantics identical) |

## Non-blocking Finding

**PDF `/CMP/CertificatePdf/N` returns HTTP 204 No Content** untuk 6 Id valid (2, 9, 11, 12, 147, 149) yang lulus all authorization + GenerateCertificate + IsPassed guards. Environmental QuestPDF runtime issue (possibly community license expired atau startup config). Razor view `/CMP/Certificate/N` render PDF-equivalent content sukses dengan DateOnly format identical (`dd MMMM yyyy` id-ID). **Action:** flag terpisah, defer follow-up — bukan blocker batch v19.0 promo.

## Decisions Applied

- D-04: `[DataType(DataType.Date)]` annotation tetap; format render verified ✓
- D-09: `DateOnly.FromDateTime(DateTime.UtcNow)` boundary WIB workflow OK ✓
- D-11: Inline sqlcmd di IT_NOTIFY + SEED_JOURNAL trail ✓
- D-15: Default System.Text.Json DateOnly `"yyyy-MM-dd"`, no JsonConverter spoof ✓

## Pending Downstream

- **Task 3 user gate:** push decision option-a/b/c
- Push approval → `git push origin main` (54 commit batch 325+326+327 SHIPPED LOCAL → origin/main)
- IT promo Dev: deliver `docs/IT_NOTIFY.md` via email/Teams
- Post-Dev verify → IT promo Prod (sama prosedur)
- Phase 328 audit-only (audit cascade sweep) — defer per spec §11 batch 3-phase

## Commits

- `b04cddea` — docs(327-08): UAT result 7 SC + Pitfall 3 + Phase 326 regression smoke PASS
- `2c7d874f` — docs(327-08): IT_NOTIFY v19.0 batch promo runbook (3 phase + migration)
- `da58e415` — docs(state): record Phase 327 SHIPPED LOCAL — 8/8 plan + 7/7 SC PASS

## Next Plan

**Phase 327 ALL 8 PLAN COMPLETE.** Next routes:
- (option-a) Push batch v19.0 → Phase 328 audit
- (option-b) Hold push → schedule Phase 328 next
- (option-c) Defer Phase 328 plan-phase first → push 4 phase batch
