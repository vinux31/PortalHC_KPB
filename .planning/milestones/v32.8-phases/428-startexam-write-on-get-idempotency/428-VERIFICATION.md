---
phase: 428-startexam-write-on-get-idempotency
verified: 2026-06-25T02:05:00Z
status: passed
score: 4/4 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: none
  previous_score: none
---

# Phase 428: StartExam Write-on-GET Idempotency Verification Report

**Phase Goal:** GET `CMP/StartExam(id)` menjadi idempoten — tidak ada mutasi status `Upcoming→Open` yang di-persist saat GET; transisi dihitung in-memory (effective-status by-schedule); GRDF-01 + time-gate + token-gate (EXSEC-01) tetap utuh; alur exam-taking worker utuh end-to-end. migration=FALSE.
**Verified:** 2026-06-25T02:05:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria)

| #   | Truth (SC) | Status | Evidence |
| --- | ---------- | ------ | -------- |
| 1   | GET StartExam tidak lagi mem-persist transisi `Upcoming→Open` (tidak ada `SaveChangesAsync` write-on-GET untuk transisi status itu). (EXSEC-02) | ✓ VERIFIED | grep `assessment.Status = "Open"` di CMPController.cs = **0 match** (blok persist lama 922-932 terhapus). Satu-satunya `= "Open"` tersisa di lobby `Assessment` :250 (display-only, no SaveChanges) + filter query :217. T1 reload DB → Status tetap `Upcoming` pasca-GET impersonate. |
| 2   | Transisi `Upcoming→Open` via jalur in-memory/guarded (idempotensi GET dipulihkan). (EXSEC-02) | ✓ VERIFIED | CMPController.cs:925-933 — `var nowWib = DateTime.UtcNow.AddHours(7);` + time-gate `Status=="Upcoming" && Schedule > nowWib` (in-memory, NO write). Mirror lobby :245-251. T2 double-GET impersonate → Status stabil `Upcoming` (idempoten). Transisi status aktual hanya saat start (justStarted, SC#4). |
| 3   | GRDF-01 (Post butuh Pre Completed) + time-gate tetap berfungsi di GET. (EXSEC-02) | ✓ VERIFIED | T3 (time-gate masa-depan → Redirect Assessment + Status tetap Upcoming), T4 (GRDF-01 Post→Pre InProgress → Redirect + "Selesaikan Pre-Test dulu..." + Status Upcoming), T6 (token-gate 427 regresi → Redirect + Status Upcoming) — semua PASS. Urutan gate R-1 terjaga (CMPController.cs:929→935→944-953→958-966). |
| 4   | Worker yang waktunya tiba tetap dapat mulai (exam-taking utuh end-to-end); impersonation guard tetap read-only. (EXSEC-02) | ✓ VERIFIED | T5 (owner non-impersonate, waktu tiba → ViewResult + reload Status=`InProgress` + StartedAt!=null + UserPackageAssignments ter-create). justStarted write (:1015-1020) + assignment-create (:1100-1117) PRESERVED + di-guard `!IsImpersonating()`. T1 (impersonate → no write, StartedAt null) buktikan read-only. |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Controllers/CMPController.cs` | StartExam GET effective-status in-memory (blok persist Upcoming→Open dihapus; time-gate `Schedule > nowWib`) | ✓ VERIFIED | Exists + substantive + wired. :925 `nowWib` deklarasi, :929 time-gate `Schedule > nowWib` (tepat 1 match grep). Blok persist lama (`Status="Open"` + SaveChangesAsync) terhapus (0 match). Gate order R-1 utuh. |
| `HcPortal.Tests/StartExamIdempotencyTests.cs` | 6 test integrasi real-SQL membuktikan idempotensi GET + regresi gate | ✓ VERIFIED | Exists. `grep -c '[Fact]'` = **6**. `[Trait("Category","Integration")]` + `IClassFixture<RetakeServiceFixture>` + `ImpersonationKeys.Mode` hadir. 6 test PASS real-SQL. |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| CMPController.StartExam time-gate | `assessment.Schedule` / `nowWib` | in-memory effective-status (NO SaveChangesAsync) | ✓ WIRED | :929 `assessment.Status == "Upcoming" && assessment.Schedule > nowWib` — tepat 1 match; tidak ada SaveChangesAsync pada blok ini. |
| StartExamIdempotencyTests impersonation path | CMPController.StartExam GET | session `Impersonate_Mode=user` + reload DB Status assert | ✓ WIRED | `MakeCmpImpersonating` set `ImpersonationKeys.Mode="user"` + TargetUserId=owner.Id + StartedAt (Ticks) → T1/T2 reload DB Status==Upcoming. |

### Gate Ordering Verification (R-1)

| Order | Gate | Line | Status |
| ----- | ---- | ---- | ------ |
| 1 | time-gate `Status=="Upcoming" && Schedule > nowWib` (NO write) | :929 | ✓ |
| 2 | Completed check | :935 | ✓ |
| 3 | GRDF-01 Post-needs-Pre-Completed (owner-only) | :944-953 | ✓ |
| 4 | token-gate EXSEC-01 (`IsTokenRequired && UserId==user.Id && StartedAt==null && TokenVerifiedAt==null`) | :958-966 | ✓ |
| 5 | exam-window close | :969 | ✓ |
| 6 | duration > 0 | :976 | ✓ |
| 7 | abandoned block | :983 | ✓ |
| 8 | justStarted InProgress write (PRESERVED, guard `!IsImpersonating()`) | :1015-1020 | ✓ |
| 9 | assignment-create (PRESERVED, guard `!IsImpersonating()`) | :1100-1117 | ✓ |

Urutan kemunculan: time-gate → Completed → GRDF-01 → token-gate. Sesuai R-1 (merge-safety vs main). D-01 scope terjaga: justStarted + assignment-create TIDAK dihapus.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| 6 test idempotensi real-SQL | `dotnet test --filter "FullyQualifiedName~StartExamIdempotencyTests"` | Failed: 0, Passed: 6, Total: 6 (2s) | ✓ PASS |
| Tidak ada persist Upcoming→Open di StartExam | grep `assessment.Status = "Open"` CMPController.cs | 0 match | ✓ PASS |
| Time-gate effective by-schedule | grep `assessment.Schedule > nowWib` CMPController.cs | 1 match (:929) | ✓ PASS |
| migration=FALSE | latest Migrations/ = `AddTokenVerifiedAt` (427, 2026-06-24); tidak ada file Phase 428 | no new migration | ✓ PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| EXSEC-02 | 428-01-PLAN.md | GET StartExam idempoten (no write-on-GET transisi Upcoming→Open); gate GRDF-01/time/token tetap; exam-taking utuh | ✓ SATISFIED | 4/4 SC verified; 6/6 test PASS; static grep + gate-order + migration=FALSE konfirmasi. |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| — | — | (none material untuk fase ini) | — | Build warnings pre-existing (CS86xx nullable) tak terkait Phase 428; tidak ada TODO/FIXME/stub baru di file yang dimodifikasi. |

### Human Verification Required

None. Tidak ada perubahan view/UI (UI hint=no di ROADMAP). Idempotensi + gate-regresi sepenuhnya terverifikasi via 6 test integrasi real-SQL (jalur impersonation = GET non-starting membuktikan no-write-on-GET). Tidak ada visual/real-time/external-service yang perlu pengujian manusia.

### Gaps Summary

Tidak ada gap. Keempat Success Criteria ROADMAP + seluruh must-have PLAN frontmatter terverifikasi terhadap codebase aktual:
- SC#1/#2 (idempotensi GET): blok persist `Upcoming→Open` benar-benar terhapus (0 grep match), diganti effective-status in-memory; T1/T2 membuktikan Status DB tetap `Upcoming` pasca-GET (single + double) di jalur impersonate read-only.
- SC#3 (gate utuh): T3 (time-gate), T4 (GRDF-01), T6 (token-gate 427) tetap memblok dengan pesan error & Status tak berubah; urutan gate R-1 terjaga.
- SC#4 (exam-taking utuh): T5 membuktikan owner asli → InProgress + StartedAt + assignment ter-create (justStarted/assignment-create PRESERVED per D-01); impersonation guard read-only (T1 StartedAt null).
- migration=FALSE dikonfirmasi (tidak ada file migrasi baru; latest = AddTokenVerifiedAt dari 427).

---

_Verified: 2026-06-25T02:05:00Z_
_Verifier: Claude (gsd-verifier)_
