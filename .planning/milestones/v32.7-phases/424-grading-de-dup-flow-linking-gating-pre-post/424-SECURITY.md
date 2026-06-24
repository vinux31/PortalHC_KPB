---
phase: 424
slug: grading-de-dup-flow-linking-gating-pre-post
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-24
---

# Phase 424 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Verdict: **SECURED 11/11** (ASVS L1, block_on=high). migration=FALSE.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| worker browser → StartExam (GET) | entry-point ujian; akses Post di-gate server-side | session id (worker-owned) |
| worker browser → SubmitExam (POST) | submit ujian; antiforgery + server-authoritative validation | jawaban (MC/MA/essay) |
| worker submit → GradingService | response peserta di-skor server-side; client tak menentukan skor | PackageUserResponse, skor |
| display pairing query → DB | grouping Pre/Post; harus terfilter UserId (cegah cross-user) | sesi peserta lain |
| HC create POST → DB | pembuatan assessment; auto-link tak boleh membuat pasangan semu | LinkedGroupId |
| export (Admin/HC) → file | Durasi Aktual / hasil; RBAC dipertahankan | data sesi |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-424-01 | Tampering | Aggregator/ComputeScoreAndET MC selection | mitigate | Dedupe last-write-wins deterministik (`Aggregator.cs:40-43`, `GradingService.cs:405-413`); `mcSel.First()`=0 (grep) | closed |
| T-424-02 | Tampering | Paritas sesi Completed | mitigate | D-07 forward-only no-recompute; 3 path konvergen (`GradingService:87-90`/`:402-413`, `Aggregator:40-43`); parity-characterization tests | closed |
| T-424-03 | Information Disclosure | scoring server-authoritative | accept | Helper pure in-memory; tak ada answer-key ke client; tak ada surface EF baru | closed |
| T-424-04 | Elevation of Privilege | StartExam gate GRDF-01 | mitigate | Gate link-eksplisit terfilter UserId (`CMPController:944-956`), bukan judul; owner-check Forbid (:916) dipertahankan; worker-only | closed |
| T-424-05 | Information Disclosure | pairing display :292-297 | mitigate | `s.UserId == userId` ditambah (`CMPController:294`); `PrePostPairing.cs:27,32` filter UserId kedua cabang | closed |
| T-424-06 | Tampering | SubmitExam essay validation | mitigate | Reject essay kosong on-time di dalam `if(!serverTimerExpired)` (`CMPController:1646`); content-check `!IsNullOrWhiteSpace` (:4624); antiforgery POST utuh | closed |
| T-424-07 | Information Disclosure | pesan essay-rejection | mitigate | Pesan "Isi semua jawaban essay terlebih dahulu sebelum submit." (`:1676-1678`) — tanpa kunci/opsi benar | closed |
| T-424-08 | Tampering | submit replay sesi terminal | accept | STAT-01 guard (Completed/Abandoned/Cancelled/PendingGrading, `:1624-1625`) dipertahankan, tak diubah | closed |
| T-424-09 | Tampering | CreateAssessment auto-pair GRDF-04 | mitigate | Auto-pair judul dimatikan forward-only (`AssessmentAdminController:876-881`); call-site=0; gate hanya percaya link eksplisit | closed |
| T-424-10 | Information Disclosure | export Durasi Aktual | accept | `[Authorize(Roles="Admin, HC")]` dipertahankan; math `:4923` unchanged; tak ada surface baru | closed |
| T-424-11 | Elevation/Tampering | gate + essay (live) | mitigate | `EvaluateOnTimeCompletion` pure (`:4611-4638`); clamp `ExamTimeRules` (:471); UAT live membuktikan server-authoritative gate + essay reject di browser | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-01 | T-424-03 | Scoring server-authoritative, helper pure in-memory; tak ada answer-key dikembalikan ke client; tak ada surface EF/endpoint baru | Phase 424 audit | 2026-06-24 |
| AR-02 | T-424-08 | Submit replay sesi terminal sudah dijaga STAT-01 (existing, tak diubah di fase ini) | Phase 424 audit | 2026-06-24 |
| AR-03 | T-424-10 | Export confirm-only; RBAC Admin/HC existing dipertahankan; math export tak diubah | Phase 424 audit | 2026-06-24 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-24 | 11 | 11 | 0 | gsd-security-auditor (sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-24
