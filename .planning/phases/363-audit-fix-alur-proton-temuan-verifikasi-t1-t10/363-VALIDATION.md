---
phase: 363
slug: audit-fix-alur-proton-temuan-verifikasi-t1-t10
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-11
---

# Phase 363 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0) — verified csproj |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (isolasi via `[Trait("Category","Integration")]`) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` + `dotnet build` |
| **Full suite command** | `dotnet test` (butuh `localhost\SQLEXPRESS`) |
| **Estimated runtime** | quick ~15s; full ~40-60s (211 existing + baru) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + `dotnet test --filter "Category!=Integration"`
- **After every plan wave:** `dotnet test` penuh (Integration + unit)
- **Before `/gsd-verify-work`:** Full suite hijau + UAT Playwright @5277 (approve TERAKHIR via CoachingProton → notif HC; reject modal → chain reset; reaktivasi cross-year diblok; HistoriProton "Belum Mulai")
- **Max feedback latency:** ~60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (diisi planner) | — | 0 | T1/T2/T7 pin | — | pin perilaku existing pre-refactor | integration | `dotnet test --filter ProtonApproveRejectParity` | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T1 notif paritas | — | allApproved → notif HC kedua jalur | integration | assert UserNotification HC | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T2 chain reset | — | SrSpv/SH/HC → Pending + null | integration | assert HCApprovalStatus=="Pending" | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T3 gate reaktivasi | access control | inactive Tahun N tanpa N-1 → blocked; Bypass inactive → exempt | integration | extend `ProtonYearGateIntegrationTests` | ⚠️ extend | ⬜ pending |
| (diisi planner) | — | — | T4 surface | info disclosure HC-only | miss → AuditLog + notif HC; idempotent guard | integration | `ProtonCompletionMissTests` | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T5 Belum Mulai | — | mapping aktif tanpa assignment → row status | integration | assert worker row | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T6 ValidUntil | — | regrade tidak hardcode +3thn | integration | assert ValidUntil unset | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | T8 history append | — | path lama masuk EvidencePathHistory | integration | assert JSON history | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ProtonApproveRejectParityTests.cs` — pin ApproveDeliverable/RejectDeliverable existing + paritas pasangan (T1/T2/T7). **WAJIB sebelum refactor helper D-01.**
- [ ] Extend `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — reaktivasi blocked + exempt Bypass inactive (T3/D-06)
- [ ] `HcPortal.Tests/ProtonCompletionMissTests.cs` — surface T4 + idempotent guard
- [ ] (opsional gabung) test T5 query + T8 append history di file Proton existing
- [ ] Framework install: TIDAK perlu — xUnit + real-SQL fixture (pola Phase 344 TEST-05) sudah ada

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| T9 log warning Urutan tidak kontigu | T9 | assert log sulit; nilai rendah utk ILogger mock | code review + opsional verifikasi manual log saat UAT |
| T10 komentar by-design | T10 | dokumentasi, bukan behavior | review komentar di BackfillProtonPenanda |
| UAT alur lengkap @5277 | T1/T2/T3/T5 | bukti end-to-end UI | Playwright: approve terakhir via modal → lonceng HC; reject modal → chain reset; assign reaktivasi cross-year → blocked; HistoriProton filter Belum Mulai |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
