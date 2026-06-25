---
phase: 426
slug: audit-log-editorganizationunit
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-24
---

# Phase 426 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| browser (Admin/HC) → POST /Admin/EditOrganizationUnit | Input rename/reparent unit. `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (TIDAK diubah fase ini) | `name` (string), `parentId` (int?) |
| controller → AuditLog DB | Penulisan baris audit. Description di-construct SERVER-SIDE; actor di-resolve server-side dari principal terautentikasi | AuditLog row (ActorUserId, ActorName, Description, TargetId, CreatedAt) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-426-01 | Tampering | Description AuditLog (input `name` dari HC) | accept | Description = data teks (kolom `Description`), bukan HTML/SQL sink; EF parameterized (`AuditLogService.cs:40-41`); server-constructed `OrganizationController.cs:321-323`. Sama-aman pola Delete shipped | closed |
| T-426-02 | Repudiation | Actor identity baris audit | mitigate | Actor server-resolved `_userManager.GetUserAsync(User)` (`OrganizationController.cs:316`); ActorUserId/ActorName/CreatedAt=UtcNow di-stamp service (`AuditLogService.cs:31-37`), bukan input client. Fase MENUTUP gap audit Edit | closed |
| T-426-03 | Denial of Service | Blok audit (extra SaveChanges I/O) | accept | Single insert; guarded only-on-change (`OrganizationController.cs:312`); swallow-on-failure try/catch (`:314-326`) → kegagalan audit tak memblokir respons edit. No amplification | closed |
| T-426-04 | Elevation of Privilege | Surface authz/CSRF | accept | Aditif-traceability murni: tak ada endpoint/param/authz baru. `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` utuh (`:127-128`) | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-426-01 | T-426-01 | Description audit adalah data teks tersimpan, bukan HTML/SQL sink; EF parameterized; identik pola Delete yang sudah ship | Phase 426 (autopilot, owner-confirmed CONTEXT D-01..03) | 2026-06-24 |
| AR-426-02 | T-426-03 | I/O ekstra satu insert per Edit ber-perubahan; swallow-on-failure mencegah dampak ke respons; risiko DoS dapat diabaikan | Phase 426 (autopilot) | 2026-06-24 |
| AR-426-03 | T-426-04 | Tidak ada surface authz/CSRF baru; atribut existing tak berubah | Phase 426 (autopilot) | 2026-06-24 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-24 | 4 | 4 | 0 | gsd-security-auditor (ASVS L1) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-24
