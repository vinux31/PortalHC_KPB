---
phase: 320-assessment-export-per-peserta-excel
plan: 03
status: complete-scope-b-modified
completed: 2026-05-21
tag: v17.0-p320-complete
---

# Plan 320-03 SUMMARY — Perf + Tests + Tag

## Scope: B-Modified

Per user approval (interactive checkpoint): perf refactor + 3-test Playwright (HC skipped pre-existing infra, benchmark deferred staging), abbreviated UAT via XLSX introspection, tag based on automated verification. User manual UAT (Variant B + visual chart + cross-format) deferred.

## Commits

| # | Hash | Subject |
|---|------|---------|
| 11 | `f6c03706` | perf(v17.0-p320): parallel PNG pre-compute (cap MaxDegreeOfParallelism=Cores) |
| 12 | `6c292083` | test(v17.0-p320): Playwright auth+download regression + manual UAT 8-step |

## Tag

**`v17.0-p320-complete`** → commit `6c29208341d9928fc4d418e5a6a4a582d2c55d00`

## Files Touched

| Path | Action | Note |
|------|--------|------|
| `Controllers/AssessmentAdminController.cs` | modified | Task 11 refactor inline → ConcurrentDictionary cache + Parallel.ForEachAsync |
| `tests/e2e/export-per-peserta.spec.ts` | created (105 LOC) | 4 test block (3 active + 1 skipped benchmark) |
| `.planning/phases/320-assessment-export-per-peserta-excel/320-UAT.md` | created (~125 LOC) | 8-step checklist + Variant B seed guide |

## Build Verification

- `dotnet build` pass — 0 errors
- Smoke test post-refactor (Playwright MCP fetch): OJT Semarang 2p export, response 200 OK 1677ms, **58325 bytes identical Plan 02 baseline** → refactor correctness verified (same output, parallel infrastructure transparent)

## Playwright Test Result

**Command:** `cd tests && npx playwright test e2e/export-per-peserta.spec.ts --reporter=list`
**Run time:** 14.5s (setup 2.4s + tests 4s + teardown 8s)

| Test | Status | Time | Detail |
|------|--------|------|--------|
| `[setup]` matrix seed + Layer 1 validation | ✅ PASS | 2.4s | BACKUP + seed + state.json + journal |
| Admin: .xlsx download + content-type | ✅ PASS | 2.9s | Filename `_Summary.xlsx`, size > 1KB |
| HC: .xlsx download (REQ EXP-07 parity) | ⏭ SKIP | — | HC login broken di test infra (assessment.spec.ts HC tests juga fail). Pre-existing, not Phase 320. REQ EXP-07 verified via code review attribute unchanged sejak commit `c94e645d` |
| Coachee: 403/redirect (REQ EXP-07 negative) | ✅ PASS | 1.0s | Auth gate enforced |
| Benchmark 50p <30s | ⏭ SKIP | — | Requires seed 50p destructive per CLAUDE.md SEED_WORKFLOW. Smoke 2p 1677ms → linear extrap parallel 50p ~6-10s, SLA 30s headroom comfortable. Defer staging |
| `[teardown]` RESTORE + cleanup | ✅ PASS | ~8s | DB restored, journal updated cleaned |

**Net:** 3 active passed, 2 skipped dengan justifikasi documented, 0 failed.

## Manual UAT 8-step Outcome

See `320-UAT.md`. Summary table:

| Step | Status | Method |
|------|--------|--------|
| 1 Summary tab + `_Summary.xlsx` filename | ✅ PASS | XLSX archive introspection |
| 2 Sheet name `{NIP}_{FullName}` 31-char | ✅ PASS | XLSX archive introspection |
| 3 ET tabel 4 kolom | ✅ PASS | XLSX archive introspection (5 elemen) |
| 4 PNG radar visual (warna + axis + alpha) | ⚠️ PARTIAL | PNG existence verified; visual code-reviewed (warna locked di helper); human Excel inspection pending |
| 5 Detail Jawaban ✓/✗ MC/MA + Tidak dijawab + Essay | ⚠️ PARTIAL | 6 kolom present, MC verified; Abandoned + Essay path code-reviewed only (no live trigger di test group) |
| 6 Variant B Manual Entry + hyperlink | ❌ NOT VERIFIED | Zero `IsManualEntry=true` di DB Dev. Code path exists. Seed guide di 320-UAT.md |
| 7 Excel + LibreOffice cross-format | ❌ NOT VERIFIED | Human-only GUI |
| 8 Edge case <3 ET skip chart | ⚠️ PARTIAL | Code-reviewed (`if (sessionEt.Count >= 3)` guard); no live trigger di test group |

**Outcome:** PASS (Variant A + auth + perf) / PARTIAL (Variant B + visual + cross-format).

## Benchmark Numbers (Smoke + Extrapolation)

| Dataset | Method | Result | Note |
|---------|--------|--------|------|
| 2 peserta (OJT Semarang) | Playwright fetch smoke | 1677ms | Inline baseline Plan 02 ≈ same (no measurable diff at small N) |
| 10p (hypothetical linear) | Extrapolation | ~8.4s sequential | Plan 02 inline serial |
| 10p (hypothetical parallel) | Extrapolation | ~1-2s | 8-core machine, Parallel.ForEachAsync |
| 50p (hypothetical linear) | Extrapolation | ~42s sequential | Would BREACH SLA 30s |
| 50p (hypothetical parallel) | Extrapolation | ~6-10s | 8-core machine, comfortable headroom |
| 50p actual benchmark | — | NOT EXECUTED | Defer staging (needs seed 50p) |

## File Size Baseline

- 2 peserta + 5 ET each + 2 spider chart PNG: **58325 bytes (~57 KB)**
- Linear extrapolation 50p: ~1.45 MB (estimate)
- Plan 02 baseline identical (refactor produces same XLSX bytes — verified)

## IT Notification Message Draft (User Manual Send)

```
Subject: Phase 320 Ready Promo Dev — v17.0 Assessment Export Per-Peserta

Body:
Phase 320 (Export Assessment Per-Peserta Excel) selesai di lokal.

Commit: 6c292083 (test+UAT) | Tag: v17.0-p320-complete
Branch: main (12 commit Phase 320: Plan 01 + Plan 02 + Plan 03)

Status: code complete, build pass, Playwright 3 test pass (HC skipped
pre-existing infra issue, benchmark deferred staging). Manual UAT
Variant A (Online) verified via XLSX introspection; Variant B Manual
Entry untested di DB Dev (zero IsManualEntry seed).

Migration: NONE — Plan 320 tidak touch schema, tidak butuh EF migration.

Yang dibutuhkan:
1. Pull latest main + checkout tag v17.0-p320-complete
2. Build + restart IIS Dev
3. Smoke verify: Admin trigger Export 1 grup assessment → verify file
   filename _Summary.xlsx + tab Summary + N tab per-peserta

Kalau ada IsManualEntry session di DB Dev/Prod, mohon spot-check tab
Manual Entry render OK (Info Sertifikasi Manual + hyperlink).

Push remote = aman setelah verify lokal (sesuai DEV_WORKFLOW §5).
```

## Phase 320 Closure

Phase 320 **CODE COMPLETE + TAGGED**. Pending:
- User manual UAT Step 4 (visual chart Excel inspection)
- User manual UAT Step 6 (Variant B Manual Entry seed + verify) — optional, gap accepted
- User manual UAT Step 7 (Excel + LibreOffice cross-format) — optional, gap accepted
- User `git push origin main && git push origin v17.0-p320-complete` (setelah konfirmasi UAT)
- IT promo Dev — `ROADMAP.md` update `🚧 STARTED` → `✅ SHIPPED` setelah IT confirm

## DEV_WORKFLOW §5 Pre-commit Checklist Plan 03

- [x] `dotnet build` pass (0 error)
- [x] `dotnet run` smoke test (refactor correctness — same bytes output)
- [x] Playwright 3 active test pass
- [x] No DB migration
- [ ] Manual visual Excel/LibreOffice — defer user
- [ ] IT notify — defer user post-push

## Total Phase 320 Commit Count

| Plan | Tasks | Commits | Tag |
|------|-------|---------|-----|
| 01 | 3 (csproj+helpers) | 3 task + 1 summary = 4 | — |
| 02 | 7 (controller refactor) | 7 task + 1 summary + 1 UAT-update = 9 | — |
| 03 | 2 (perf+test) | 2 task + 1 summary = 3 | `v17.0-p320-complete` |
| **Total** | **12 task** | **16 commit** | **1 tag** |

Per CONTEXT.md D-06 commit format `feat/refactor/perf/test(v17.0-p320): ...` — all atomic.
