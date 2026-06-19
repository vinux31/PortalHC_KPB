---
phase: 393
slug: backend-core-inject
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
updated: 2026-06-17
---

# Phase 393 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 393-RESEARCH.md "## Validation Architecture" + ROADMAP 5 Success Criteria + CONTEXT D-01..D-12.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8) — project `HcPortal.Tests` |
| **Config file** | existing `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter Category!=Integration` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | quick ~30s; full (incl. Integration real-SQL) ~2-4 min |

> Inject grading tests REQUIRE real SQL Server (`localhost\SQLEXPRESS`, disposable DB `HcPortalDB_Test_{guid}`, `[Trait("Category","Integration")]`). EF Core 8 InMemory does NOT support `ExecuteUpdateAsync` (used by GradingService + essay-finalize) → InMemory cannot validate this phase. Wire a REAL `GradingService` instance (pattern: `HcPortal.Tests/SubmitResurrectionTests.cs`).

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category!=Integration` (fast guard — no regression)
- **After every plan wave:** Run `dotnet test` (full, incl. inject Integration tests)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error + `dotnet ef migrations add _verify` → 0 model diff (then discard) to confirm 0 migration
- **Max feedback latency:** ~30s (quick), ~4 min (full)

---

## Per-Task Verification Map

> Filled during planning / Wave 0. Each row maps a task to its automated proof. Success criteria (SC) from ROADMAP Phase 393.

| Task ID | Plan | Wave | Requirement | SC | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|----|-----------------|-----------|-------------------|-------------|--------|
| 393-01-01 | 01 | 1 | INJ-01 | SC1 | Grading byte-identik online (MC/MA/Essay): Score/IsPassed/SessionElemenTeknisScore identik vs jalur online untuk input sama | integration | `dotnet test --filter "FullyQualifiedName~InjectAssessment&Category=Integration"` | ✅ | ✅ green |
| 393-01-02 | 01 | 1 | INJ-01 | SC2 | NIP invalid / error mid-batch → rollback total (0 sesi tertulis); pre-flight invalid → 0 tulisan + per-row error | integration | `dotnet test --filter "FullyQualifiedName~InjectAtomic"` | ✅ | ✅ green |
| 393-01-03 | 01 | 1 | INJ-01 | SC3 | Sesi essay ber-EssayScore → Status=Completed (bukan PendingGrading) setelah finalize-block; backdate CompletedAt ter-preserve pasca-grade (pitfall: grading overwrite ke UtcNow) | integration | `dotnet test --filter "FullyQualifiedName~InjectEssayCompleted"` | ✅ | ✅ green |
| 393-01-04 | 01 | 1 | INJ-02 | SC4 | Tiap sesi sukses → IsManualEntry=true + 1 AuditLog ActionType="ManualInject" (count=jumlah sesi sukses); skip/reject ActionType terpisah (tak menggembungkan count ManualInject) | integration | `dotnet test --filter "FullyQualifiedName~InjectAudit"` | ✅ | ✅ green |
| 393-01-05 | 01 | 1 | INJ-01/02 | SC5 | Cert auto pakai backdate (D-12: KPB/seq/ROMAN-bulan-ujian/tahun-ujian); cert suppress bila !IsPassed (D-08); cert manual collision → reject (D-09); EssayScore di luar 0..ScoreValue → invalid (D-07); tanggal > hari ini → invalid (D-06) | integration | `dotnet test --filter "FullyQualifiedName~InjectCertPolicy"` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/InjectAssessmentServiceTests.cs` — test class (disposable real-SQL fixture `IAsyncLifetime`, `[Trait("Category","Integration")]`, real `GradingService` wiring per `SubmitResurrectionTests.cs:68-76`) — 6 fact assertion nyata SC1..SC5 (Plan 03)
- [x] Shared fixture/builder helper authored package (`BuildSampleRequest`/`OneMcQuestion`/`LoadGradedAsync` — PackageQuestion MC/MA/Essay + PackageOption + ScoreValue + ElemenTeknis) reusable lintas fact
- [x] `HcPortalDB_Dev` TIDAK disentuh — fixture pakai `HcPortalDB_Test_{guid}` + `EnsureDeletedAsync` (verified gsd-verifier runtime spot-check)

*xUnit + HcPortal.Tests sudah ada — tak perlu install framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 0-migration confirm ✅ DONE | INJ-01/02 | EF model-diff bukan unit test | `dotnet ef migrations add _verify` → **Up()/Down() kosong (0 model diff)** → removed + snapshot restored (2026-06-17) |

*Sisanya: semua perilaku phase punya verifikasi otomatis xUnit Integration. Visibility /CMP/Records + /CMP/Results = ranah Phase 398 E2E (bukan 393).*

---

## Validation Sign-Off

- [x] Semua task punya `<automated>` verify atau Wave 0 dependency
- [x] Sampling continuity: tak ada 3 task beruntun tanpa automated verify
- [x] Wave 0 cover semua reference MISSING (test class + fixture) — terisi Plan 03
- [x] Tak ada watch-mode flag
- [x] Feedback latency < 240s (full Integration ~2 min)
- [x] `nyquist_compliant: true` di-set di frontmatter

**Approval:** ✅ COMPLIANT 2026-06-17

---

## Validation Audit 2026-06-17

| Metric | Count |
|--------|-------|
| Requirements (SC) | 5 |
| Gaps found | 0 |
| COVERED (automated) | 5 (SC1..SC5) |
| Resolved | 0 (no gaps — tests written + green di Plan 03) |
| Escalated / manual-only | 1 (0-migration confirm — DONE) |

**Evidence:** 6 fact xUnit Integration (`InjectAssessment_ByteIdentik`/`_PartialMA` SC1, `InjectAtomic` SC2, `InjectEssayCompleted` SC3, `InjectAudit` SC4, `InjectCertPolicy` SC5) — combined run **6/6 PASS** real SQLEXPRESS; full suite **492/492**; 0-migration confirmed. State A audit: no gaps, no auditor spawn needed (workflow Step 3). nyquist_compliant=true.
