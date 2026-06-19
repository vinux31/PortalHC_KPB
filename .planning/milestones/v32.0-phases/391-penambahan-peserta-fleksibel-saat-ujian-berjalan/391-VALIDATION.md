---
phase: 391
slug: penambahan-peserta-fleksibel-saat-ujian-berjalan
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
---

# Phase 391 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Audited 2026-06-17: 4 perilaku substantive (PART-01/02/04) automated-COVERED (4 fact integration green); PART-03 manual-only (UAT browser 2/2).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (`HcPortal.Tests`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (fast suite) |
| **Full suite command** | `dotnet test` (includes Integration real-SQL; needs SQLEXPRESS up) |
| **Integration-only** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAdd"` |
| **Runtime** | fast ~30s · full ~3-4 min |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error + `dotnet test --filter "Category!=Integration"` (347/347 green).
- **After every plan wave:** `dotnet test` full (incl. Integration; disposable `HcPortalDB_Test_{guid}`, `HcPortalDB_Dev` untouched).
- **Phase gate:** full suite green (**486/486 achieved**) + UAT browser notice/guard.

---

## Per-Task Verification Map

| Requirement | Behavior | Test | Command | Status |
|-------------|----------|------|---------|--------|
| PART-01 | Tambah saat ada `InProgress` → sesi baru tercipta | `FlexibleParticipantAddTests.AddParticipant_WithInProgressSibling_CreatesNewSession` | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAdd"` | ✅ green |
| PART-01 | Sesi baru siap-mulai (Open/Upcoming, BUKAN InProgress) | `...HasReadyStatus_NotInProgress` | idem | ✅ green |
| PART-02 | Tak terblokir saat sebagian `Completed` + window terbuka | `...SomeCompleted_NotBlocked_WhileWindowOpen` | idem | ✅ green |
| PART-04(c) | Sesi `InProgress` existing Status/Schedule/Duration UNCHANGED | `...InProgressSibling_StatusScheduleDurationUnchanged` | idem | ✅ green |
| PART-03 | Notice `Info` (bukan Warning) informatif | **manual-only** (lihat di bawah) | grep + UAT browser | ✅ verified (manual) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/FlexibleParticipantAddTests.cs` — fixture disposable + 4 facts (a/b/c/d) mengunci PART-01/02/04. **DONE (commit `31e71a3e`)**, 4/4 green.

*Tidak ada framework install — xUnit + SqlServer provider sudah ada.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Result |
|----------|-------------|------------|-------------------|--------|
| Notice `Info` (alert biru) tampil informatif | PART-03 | TempData render via `_Layout` (visual); xUnit controller-TempData harness tidak ada di project (pola data-level, bukan WebApplicationFactory). Guard automated tambahan = grep acceptance (`TempData["Info"]` ada, wording Warning lama 0×). | localhost:5277 → tambah peserta ke assessment dgn peserta InProgress → cek alert **biru Info** (bukan kuning Warning) + teks menenangkan | ✅ PASS (UAT browser 2026-06-17: alert Info + "1 new user assigned"; lihat 391-HUMAN-UAT.md) |

---

## Validation Audit 2026-06-17

| Metric | Count |
|--------|-------|
| Requirements | 4 (PART-01..04) |
| Automated COVERED | 3 REQ (PART-01/02/04 via 4 fact integration) |
| Manual-only | 1 REQ (PART-03 notice — UAT browser 2/2) |
| Gaps unresolved | 0 |

---

## Validation Sign-Off

- [x] All requirements have automated verify OR documented manual-only
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covered (FlexibleParticipantAddTests.cs created + green)
- [x] No watch-mode flags
- [x] Feedback latency < 60s (fast suite)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-17 (PART-01/02/04 automated; PART-03 manual-only UAT-verified)
