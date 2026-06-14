---
phase: 370
slug: hapus-window-7-hari-tampilan-default-tanpa-batas
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-12
audited: 2026-06-12
auditor: gsd-security-auditor
---

# Phase 370 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| client (admin browser) → controller endpoint | GET ke `/Admin/ManageAssessmentTab_Assessment` + `/Admin/AssessmentMonitoring`; input `search`/`category`/`statusFilter` user-controlled | parameter query (string) |
| controller → EF Core / SQL Server | Query `_context.AssessmentSessions` — parameterized via EF Core; window filter dilepas (perubahan fase ini) | data sesi assessment (judul, jadwal, status) — sensitivitas internal, admin-only |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-370-01 | Information Disclosure | Window dilepas → sesi historis >7 hari tampil di default view (ManageAssessmentTab_Assessment + AssessmentMonitoring) | accept | `[Authorize(Roles = "Admin, HC")]` :110 + :2845 (tak diubah) — window murni perf/UX, bukan kontrol akses | closed |
| T-370-02 | Denial of Service | AssessmentMonitoring tanpa window → full-table load + in-memory grouping tanpa pagination | accept | `.AsNoTracking()` :2853-2854 (D-05); admin-only low-volume, skala 58 row; pagination deferred | closed |
| T-370-03 | Tampering | Jalur search `.Where(Title/Category Contains)` | mitigate (existing) | EF Core LINQ `.Contains()` :2861-2862 parameterized otomatis; zero `FromSqlRaw\|ExecuteSqlRaw` | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

### T-370-01 — Information Disclosure (accepted)

- **Risiko:** Window 7-hari dilepas → sesi historis >7 hari kini tampil di default view `ManageAssessmentTab_Assessment` + `AssessmentMonitoring`.
- **Justifikasi:** Kedua endpoint dilindungi `[Authorize(Roles = "Admin, HC")]` — tidak ada PII baru ter-expose ke pihak tak berhak. Admin/HC memang berhak melihat semua sesi assessment. Window murni pembatas perf/UX, bukan kontrol akses. Selaras T-m9r-01 quick task sebelumnya.
- **Bukti kontrol akses:** `Controllers/AssessmentAdminController.cs:110` + `:2845` (verified gsd-security-auditor 2026-06-12).
- **Accepted By:** user (Rino) — keputusan verbal "7 hari jadi tanpa batas".
- **Date:** 2026-06-11

### T-370-02 — Denial of Service (accepted)

- **Risiko:** `AssessmentMonitoring` tanpa window → full-table load + in-memory grouping tanpa pagination.
- **Justifikasi:** Skala saat ini 58 row lokal; endpoint admin-only low-volume. `.AsNoTracking()` (D-05) ditambahkan untuk kurangi overhead EF tracking. Pagination Monitoring = Deferred Idea bila row membengkak.
- **Bukti D-05:** `Controllers/AssessmentAdminController.cs:2853-2854` (verified gsd-security-auditor 2026-06-12).
- **Deferred Action:** Tambah pagination `AssessmentMonitoring` bila total sesi lewati ambang perf (lihat 370-CONTEXT.md Deferred Ideas).
- **Accepted By:** user (Rino)
- **Date:** 2026-06-11

> T-370-03 disposition = **mitigate (existing)**, bukan accepted risk — kontrol EF Core parameterization sudah ada & terverifikasi di kode.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-12 | 3 | 3 | 0 | gsd-security-auditor (model sonnet) |

### Grep Guard Results (cross-check)

- `ApplySevenDayWindow\|sevenDaysAgo` di `Controllers/Views/wwwroot/tests/HcPortal.Tests`: **0 hit** (CLEAN).
- `FromSqlRaw\|ExecuteSqlRaw\|ExecuteSqlInterpolated` di `AssessmentAdminController.cs`: **0 hit** (CLEAN).
- `[Authorize(Roles = "Admin, HC")]` :110 (ManageAssessmentTab_Assessment): **CONFIRMED**.
- `[Authorize(Roles = "Admin, HC")]` :2845 (AssessmentMonitoring): **CONFIRMED**.
- `.AsNoTracking()` di Monitoring chain :2853-2854: **CONFIRMED**.
- EF LINQ `.Contains()` jalur search :2861-2862: **CONFIRMED** (bukan raw SQL).

### Unregistered Flags

Tidak ada `## Threat Flags` di SUMMARY.md fase ini. Deviasi tercatat (race commit sesi paralel 364, komentar `90-review` dipertahankan di :3477) bersifat prosedural/non-security — bukan threat baru.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-12
