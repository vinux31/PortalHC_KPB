---
phase: 381
slug: worker-entry-startexam-integrity
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-14
---

# Phase 381 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail signal/dimensi lihat `381-RESEARCH.md` §"Validation Architecture".

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/e2e/playwright.config.ts |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~Sibling\|FullyQualifiedName~StartExam\|FullyQualifiedName~Impersonation"` |
| **Full suite command** | `dotnet build` + `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~30–60s xUnit; e2e per-spec ~60–120s (`--workers=1`) |

---

## Sampling Rate

- **After every task commit:** Run quick command (filtered xUnit)
- **After every plan wave:** Run full suite (`dotnet build` + `dotnet test`)
- **Before `/gsd-verify-work`:** Full suite green + e2e #4/#7 green (headed, local AD off)
- **Max feedback latency:** ~60s (xUnit); e2e deferred to wave-end

---

## Per-Task Verification Map

> Diisi planner per task. Signal kunci per Success Criterion (lihat RESEARCH §Validation Architecture):

| Plan/Task | Success Criterion | Requirement | Automated Verify | Observable Signal |
|-----------|-------------------|-------------|------------------|-------------------|
| 381-01 T1 | Type-aware legacy safety (D-09) | WSE-04 | `dotnet test --filter ~SiblingPrePostFilter` | Pre↔Pre, Post↔Post; Standard/''/null satu grup (5 [Fact]) |
| 381-01 T2 | Determinisme StartExam==reshuffle | WSE-04 | `dotnet test --filter ~SiblingDeterminism` | OrderBy(x=>x).IndexOf(id) identik untuk set sama |
| 381-01 T3 | Reshuffle type-aware (no regression) | WSE-04 | `dotnet build` + `dotnet test HcPortal.Tests` | build 0 err; full xUnit hijau |
| 381-02 T1 | Guard write-site 1+2 (no mutasi impersonate) | WSE-05 | `dotnet build` | guard `justStarted && !IsImpersonating()` ≥2× |
| 381-02 T2 | Guard write-site 3 + in-memory preview (D-06) | WSE-05 | `dotnet build` + `dotnet test HcPortal.Tests` | persist ter-guard; build hijau; no regression |
| 381-03 T1 | SC#1 entry-pool Pre/Post (#4) | WSE-04 | `npx playwright test exam-taking -g "WSE-04" --workers=1` | jumlah & teks soal == paket type-sendiri |
| 381-03 T2 | SC#2 + SC#3 impersonate read-only (#7) | WSE-05 | `npx playwright test impersonation -g "WSE-05" --workers=1` | StartedAt null→set, 0→1 UserPackageAssignment via queryScalar |
| 381-03 T3 | Preview render + migration:false (manual) | WSE-05 | checkpoint:human-verify (manual — render runtime + `dotnet ef migrations add`) | no NRE/500; migration body kosong + removed |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

> Tertutup di 381-01 (unit RED-first) + 381-03 (e2e extend). Tak ada framework baru.

- [x] Test sibling-helper filter (`HcPortal.Tests/SiblingPrePostFilterTests.cs`) — 381-01 Task 1 (RED-first)
- [x] Test sibling determinism (`HcPortal.Tests/SiblingDeterminismTests.cs`) — 381-01 Task 2 (RED-first)
- [x] e2e: extend `tests/e2e/exam-taking.spec.ts` (#4 PrePost same-day pool-only) — 381-03 Task 1
- [x] e2e: extend `tests/e2e/impersonation.spec.ts` (#7 read-only + deferred-start, dbSnapshot) — 381-03 Task 2

*Catatan: harness existing menutup mayoritas; tak ada framework baru. Local e2e SQL quirks: SQLBrowser + `--workers=1` + AD off (`Authentication__UseActiveDirectory=false dotnet run`).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Preview soal saat impersonate render (vm.AssignmentId=0, D-06) | WSE-05 | Razor dynamic runtime — grep+build tak cukup (lesson Phase 354) | 381-03 Task 3 Bagian A: Playwright/manual headed — impersonate worker X → StartExam ujian Open belum-mulai → halaman render soal tanpa NRE/500; DB assert no-mutation |
| migration: false (zero schema diff) | WSE-04/05 | EF scaffold inspeksi — perlu eksekusi `dotnet ef migrations add` lalu remove | 381-03 Task 3 Bagian B: `dotnet ef migrations add` → body kosong → `migrations remove --force` → git status bersih |

*Selain itu: semua behavior punya automated verification.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (T3 = checkpoint:human-verify, manual-only justified)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (T1/T2 auto setiap plan; T3 checkpoint)
- [x] Wave 0 covers all MISSING references (sibling/determinism unit + e2e #4/#7)
- [x] No watch-mode flags
- [x] Feedback latency < 60s (xUnit quick command)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** planner — 2026-06-14
