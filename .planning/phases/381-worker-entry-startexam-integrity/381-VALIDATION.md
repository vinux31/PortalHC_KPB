---
phase: 381
slug: worker-entry-startexam-integrity
status: draft
nyquist_compliant: false
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

| Success Criterion | Requirement | Secure/Correct Behavior | Test Type | Observable Signal |
|-------------------|-------------|-------------------------|-----------|-------------------|
| SC#1 entry-pool Pre/Post (#4) | WSE-04 | StartExam sesi Pre → question set == paket Pre saja; Post tak tercampur | integration + e2e | jumlah & teks soal == paket type-sendiri; assignment ShuffledQuestionIds ⊆ paket type itu |
| Determinisme StartExam==reshuffle | WSE-04 | sibling-set + workerIndex identik antara StartExam & ReshufflePackage/ReshuffleAll | unit | helper kembalikan list+order sama untuk input sama (mirror `SiblingFilterTests`) |
| Type-aware legacy safety (D-09) | WSE-04 | Standard/''/null tak terpecah; hanya Pre/Post diisolasi | unit | grup non-PrePost utuh; Pre↔Pre, Post↔Post |
| SC#2 impersonate read-only (#7) | WSE-05 | impersonate buka Open StartedAt==null → no mutation | integration + e2e | StartedAt==null, Status=="Open", 0 UserPackageAssignment, 0 SignalR workerStarted, 0 ExamActivityLog "started" |
| SC#3 deferred-start (#7 lanjutan) | WSE-05 | stop impersonate + worker login asli → StartExam → StartedAt ter-set | integration + e2e | StartedAt != null SETELAH worker asli; assignment ter-create persist |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Test file/fixture untuk sibling-helper determinism (analog `HcPortal.Tests/SiblingFilterTests.cs` / `ShuffleReshuffleTests.cs`)
- [ ] Integration fixture impersonate-no-mutation (analog `ImpersonationIdentityTests.cs`)
- [ ] e2e: extend `tests/e2e/exam-taking.spec.ts` (#4 PrePost same-day pool-only) + `impersonation.spec.ts` (#7 read-only) pakai helper `examTypes.ts` + `dbSnapshot`

*Catatan: harness existing menutup mayoritas; tak ada framework baru. Local e2e SQL quirks: SQLBrowser + `--workers=1` + AD off (`Authentication__UseActiveDirectory=false dotnet run`).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Preview soal saat impersonate render (vm.AssignmentId=0, D-06) | WSE-05 | Razor dynamic runtime — grep+build tak cukup (lesson Phase 354) | Playwright headed: impersonate worker X → StartExam ujian Open belum-mulai → assert halaman render soal tanpa NRE/500; DB assert no-mutation |

*Selain itu: semua behavior punya automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
