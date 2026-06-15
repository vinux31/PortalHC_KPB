---
phase: 360
slug: bypass-backend-b
status: ready
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-10
revised: 2026-06-10
---

# Phase 360 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> W-01 (review): diisi planner dari 360-RESEARCH.md §Validation Architecture. Wave 0 (test creation) DIGABUNG ke Plan 03 Task 3 + Plan 04 Task 3 + Plan 05 Task 3 (bukan plan terpisah).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~90 detik (unit) / ~120+ detik (dengan integration real-SQL) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "Category!=Integration"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-T1 | 01 | 1 | PBYP-01/03 | T-360-01 | Model+DbSet+index+template, no seed UPDATE | build | `dotnet build` | ✅ src | ⬜ pending |
| 01-T2 | 01 | 1 | PBYP-01 | T-360-01/02/03 | Migration#2 apply + snapshot, snapshot DB pre-apply | integration (MigrateAsync) | `dotnet test --filter "Category!=Integration"` + sqlcmd cols=12 | ✅ migration | ⬜ pending |
| 02-T1 | 02 | 2 | PBYP-05 | T-360-05 | Bootstrap unit-form + guard anti-dobel (B-06) | unit/build | `dotnet test --filter "Category!=Integration"` | ✅ helper | ⬜ pending |
| 02-T2 | 02 | 2 | PBYP-02 | T-360-04/06 | 2 titik exempt Origin=Bypass, gate 100% tetap (D-05) | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 03-T1 | 03 | 3 | PBYP-02 | T-360-10 | Validasi pure §5 (B-03 CL-A allApproved+final) | unit (no DB) | `dotnet test --filter "FullyQualifiedName~ProtonBypassValidation"` | ✅ Wave 0 → ProtonBypassValidationTests.cs | ⬜ pending |
| 03-T2 | 03 | 3 | PBYP-02/04/05 | T-360-07/08/09 | §5.1 instan + E8 (B-04) + Pitfall 1 + D-16b + no info-leak | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 03-T3 | 03 | 3 | PBYP-04/05 | T-360-09/31 | Integration §5.1 (Pitfall 1, bootstrap unit-form, coach E15, B-06 CL-C turun, D-16b) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ProtonBypassServiceTests"` | ✅ Wave 0 → ProtonBypassServiceTests.cs | ⬜ pending |
| 04-T1 | 04 | 4 | PBYP-02/03/06 | T-360-12/32/33 | BypassSave + E8 (B-04) + D-10 + §5.2 bare session UserId+AssessmentType (B-05) | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 04-T2 | 04 | 4 | PBYP-03 | T-360-11/13 | MarkPendingReady/Revert hook (NO tx) + notif HC | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 04-T3 | 04 | 4 | PBYP-02/03/06 | T-360-12/32 | Integration CL-B(b) pending + flip + revert + D-10 + E8 (B-04) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ProtonBypassServiceTests"` | ✅ extend | ⬜ pending |
| 05-T1 | 05 | 5 | PBYP-02 | T-360-15/16/17 | ConfirmBypass D-11 stale + D-12 atomic + bootstrap (W-02) | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 05-T2 | 05 | 5 | PBYP-06 | T-360-34 | CancelPending §8.1 dua branch + W-03 guard !=Completed | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 05-T3 | 05 | 5 | PBYP-02/06 | T-360-15/16/34 | Integration confirm + cancel (D-11/D-12/W-02/W-03) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ProtonBypassServiceTests"` | ✅ extend | ⬜ pending |
| 06-T1 | 06 | 6 | PBYP-03 | T-360-19/21/35 | 3 hook GradingService (braced W-09), satu-arah DI | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 06-T2 | 06 | 6 | PBYP-03 | T-360-20 | Essay hook titik 4 (Pitfall 2) | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 07-T1 | 07 | 7 | PBYP-07 | T-360-22 | 3 GET endpoint + BypassDetail eligibleModes B-03 | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 07-T2 | 07 | 7 | PBYP-07 | T-360-23/24/25/26 | 3 POST + AntiForgery + validasi + audit + D-02 TempData (W-12) | build | `dotnet test --filter "Category!=Integration"` | ✅ src | ⬜ pending |
| 07-T3 | 07 | 7 | PBYP-07 | T-360-22/23 | Reflection test Authorize + AntiForgery | unit (reflection) | `dotnet test --filter "FullyQualifiedName~ProtonBypassEndpoint"` | ✅ Wave 0 → ProtonBypassEndpointTests.cs | ⬜ pending |
| 08-T1 | 08 | 8 | PBYP-02 | T-360-28 | Gate exempt Origin=Bypass (cross-year skip, 100% tetap, regresi) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ProtonYearGate"` | ✅ extend existing | ⬜ pending |
| 08-T2 | 08 | 8 | PBYP-02/07 | T-360-28/29 | Full suite + 360-UAT.md (U3 TempData W-12, U5 hapus=Dibatalkan I-03) | full suite | `dotnet test` | ✅ doc | ⬜ pending |
| 08-T3 | 08 | 8 | PBYP-02/07 | T-360-30 | UAT manusia 6 skenario @5277 | manual checkpoint | (manual — Playwright MCP + sqlcmd) | ✅ UAT.md | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

> Wave 0 test creation TIDAK dijadikan plan terpisah — DIGABUNG ke task test di plan implementasi (catatan W-01).

- [x] `HcPortal.Tests/ProtonBypassValidationTests.cs` — dibuat di **Plan 03 Task 1** (TDD RED→GREEN, pure-unit validasi §5; pola `ProtonYearGateTests` no-DB). Covers validasi |Δtahun|/1-aktif/alasan/mode/B-03.
- [x] `HcPortal.Tests/ProtonBypassServiceTests.cs` — dibuat di **Plan 03 Task 3**, di-extend **Plan 04 Task 3** + **Plan 05 Task 3** (reuse `ProtonCompletionFixture` disposable real-SQL `[Trait("Category","Integration")]`, MigrateAsync membuktikan migration#2 apply). Covers PBYP-02..06 + B-06 + D-16b + W-02 + W-03 + B-04/B-05.
- [x] `HcPortal.Tests/ProtonBypassEndpointTests.cs` — dibuat di **Plan 07 Task 3** (reflection attr test, no DB; pola Phase 344 TEST-05). Covers PBYP-07 Authorize/AntiForgery.
- [x] `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — **extend existing** (dari Phase 359) di **Plan 08 Task 1** untuk kasus exempt Origin="Bypass". Covers gate exempt D-05/D-06.
- Framework sudah ada (xUnit + fixture `ProtonCompletionFixture` reusable apa adanya) — tidak perlu install [VERIFIED: ProtonCompletionServiceTests.cs:25-61].

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UAT lokal:5277 end-to-end bypass | PBYP-02/03/06 | Render banner + notif + state machine butuh server jalan | Playwright MCP @ localhost:5277, AD off; 6 skenario U1-U6 (Plan 08 Task 3) |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (semua task auto punya `<automated>`; checkpoint UAT 08-T3 manual sesuai sifatnya)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (tiap task code-producing punya build/test command; tak ada 3 berturutan tanpa automated)
- [x] Wave 0 covers all MISSING references (4 test file di atas — semua dibuat/extend di plan tasks)
- [x] No watch-mode flags (semua perintah one-shot `dotnet test`, tanpa `--watch`)
- [x] Feedback latency < 120s (unit <5s per commit; full suite ~120s per wave)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** ready (W-01 resolved — map terisi dari RESEARCH §Validation Architecture, Wave 0 digabung ke plan tasks, sampling continuity terverifikasi)
</content>
