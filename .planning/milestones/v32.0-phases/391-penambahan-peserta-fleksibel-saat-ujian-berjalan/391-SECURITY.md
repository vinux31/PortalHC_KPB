---
phase: 391
slug: penambahan-peserta-fleksibel-saat-ujian-berjalan
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-17
---

# Phase 391 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Verified by gsd-security-auditor against implemented code 2026-06-17. **8/8 threats CLOSED.**

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| HC/Admin browser → EditAssessment POST | Form input (model + NewUserIds[]) dari klien terautentikasi ke server. Endpoint `[Authorize(Roles="Admin, HC")]` — bukan permukaan publik. | Konfigurasi assessment + daftar UserId peserta |
| EditAssessment POST → AssessmentSessions (DB) | Insert sesi baru + update sibling. Server-authoritative untuk status & window. | Sesi assessment per-peserta |
| Test process → SQL Server (localhost\SQLEXPRESS) | Test buat & hapus DB disposable `HcPortalDB_Test_{guid}`; tidak menyentuh `HcPortalDB_Dev`. | Skema + data test fana |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-391-01 | Tampering | Status sesi baru ditentukan klien | mitigate | `DeriveReadyStatus` hitung status server-side dari Schedule vs now-WIB (`AssessmentAdminController.cs:2249`, WIB `UtcNow.AddHours(7)` L2251); BULK ASSIGN `Status = DeriveReadyStatus(...)` L2177 — tak ada path form→Status | closed |
| T-391-02 | Tampering | D-03 disalahgunakan merusak sesi berjalan | mitigate | `if (sibling.StartedAt != null && sibling.CompletedAt == null) continue;` L2067 (+ Pre-Post per-phase loops) MELINDUNGI sesi berjalan; tak ada path korupsi baru | closed |
| T-391-03 | Elevation of Privilege | Authorization endpoint | accept | GET (L1667) + POST (L1792) EditAssessment tetap `[Authorize(Roles="Admin, HC")]`; tak ada permukaan publik baru | closed |
| T-391-04 | Information Disclosure / IDOR | Penambahan ke grup assessment lain | accept | Query sibling scoped group-key (Title+Category+Schedule.Date) L2135-2138; `assessment` di-`FindAsync(id)` route-param L1799; `AssessmentType` (fix UAT L2174) warisi dari `assessment` (bukan form) — tak ada injection/IDOR; idempotency filter dipertahankan L2144-2147 | closed |
| T-391-05 | Denial of Service | Pelonggaran guard Completed | mitigate | Guard di-bypass hanya untuk jalur penambahan (`&& !hasAddition` L1998); pure-edit Completed tetap ditolak; rate-limit `NewUserIds.Count > 50` utuh L2005 | closed |
| T-391-T1 | Tampering | Test menulis ke DB Dev | mitigate | Connection hardcode `HcPortalDB_Test_{Guid}` (`FlexibleParticipantAddTests.cs:22,29`); `EnsureDeletedAsync` Dispose L50-51; `[Trait Category=Integration]` exclude fast suite | closed |
| T-391-T2 | Repudiation | Hijau palsu (replica drift) | mitigate | Replica `DeriveReadyStatus` byte-identik `UtcNow.AddHours(7)` (L89-93); fact (c) positive-control (sesi belum-mulai berubah → filter selektif, bukan no-op) | closed |
| T-391-T3 | Denial of Service | MigrateAsync gagal misleading | accept | `InitializeAsync` wrap try/catch (L36-45) → `XunitException` pesan eksplisit "MIGRATION-CHAIN break, BUKAN bug fix" + cleanup best-effort | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-391-01 | T-391-03 | Endpoint tetap Admin/HC-only; tak ada perubahan atribut authz / permukaan publik baru | gsd-security-auditor | 2026-06-17 |
| AR-391-02 | T-391-04 | Query ter-scope group-key + route-param id; tak ada parameter id bebas baru; AssessmentType warisi server-side | gsd-security-auditor | 2026-06-17 |
| AR-391-03 | T-391-T3 | Kegagalan MigrateAsync test = indikasi migration-chain break, di-surface eksplisit; bukan jalur bug fix | gsd-security-auditor | 2026-06-17 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-17 | 8 | 8 | 0 | gsd-security-auditor (sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-17
