---
phase: 369
slug: 369-sync-h1-search-drop-fix-main-ithandoff
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-11
---

# Phase 369 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser → Controller (`ManageAssessmentTab_Training`) | Input `search`/`section`/`unit` dari user; route ber-`[Authorize(Roles="Admin, HC")]` | Query string user-supplied (search term, filter) |
| Controller → Service (`GetWorkersInSection`) → EF Core | `search` dipakai di LINQ `.Where(... Contains(search))` — query read-only | Search term → SQL parameter (data pekerja: nama, NIP) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-369-01 | Tampering (SQL injection via `search`) | `WorkerDataService.GetWorkersInSection` LINQ `.Contains(search)` | accept | EF Core LINQ `.Contains()` → parameterized query, bukan string concat / raw SQL. Verified: `Services/WorkerDataService.cs:261-268` — guard H1 hanya mengubah kondisi boolean, pola query tidak berubah. ASVS V5 by-design. | closed |
| T-369-02 | Information Disclosure (search expose worker lain) | hasil `GetWorkersInSection` | accept | Fix MENYEMPITKAN hasil (filter by nama/NIP); scope section/unit (`WorkerDataService.cs:251-255`) + `[Authorize(Admin, HC)]` tidak dilonggarkan. Risiko menurun — sebelum fix justru balikin SEMUA row di section. UAT: 7 row → 1 row terfilter. | closed |
| T-369-03 | Elevation of Privilege (akses tanpa role) | route `ManageAssessmentTab_Training` | mitigate | `[Authorize(Roles = "Admin, HC")]` verified ada di `Controllers/AssessmentAdminController.cs:251` tepat di atas action (line 253). Fix service-layer (cherry-pick `5210e4d4`) tidak menyentuh otorisasi. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-369-01 | T-369-01 | EF Core parameterized LINQ — tidak ada jalur injeksi; fix H1 tidak mengubah pola query. Tidak perlu kontrol tambahan. | GSD secure-phase (plan disposition) | 2026-06-11 |
| AR-369-02 | T-369-02 | Perubahan murni menyempitkan result set dalam scope authorize + section yang sudah ada; exposure netto turun. | GSD secure-phase (plan disposition) | 2026-06-11 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-11 | 3 | 3 | 0 | gsd-secure-phase (direct evidence verification, State B) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-11
