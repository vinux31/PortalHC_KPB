---
phase: 403
slug: organizationcontroller-cascade-guard-userunits-aware
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-19
---

# Phase 403 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> OrganizationController UserUnits-aware cascade/guard + cascade-confirm modal row. 0 migration.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| browser (Admin/HC) → OrganizationController POST | Edit/Preview/Toggle/Delete input `id`/`name`/`parentId` di belakang RBAC + antiforgery | unit name/id, parent id |
| OrganizationController → DB (UserUnits/Users/OrganizationUnits) | cascade rename / split-detect / guard mutations | unit names, NIP/FullName (split msg), membership rows |
| server (PreviewEditCascade JSON) → browser (orgTree.js) | `affectedUserUnitsCount` ditampilkan read-only di modal | aggregate integer count |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-403-01 | Tampering (mass-assignment) | reparent `parentId` → unit pindah Bagian tak sah / split-brain | mitigate | `OrganizationController.cs:189` parentId `FindAsync` + `:170` `IsDescendantAsync` circular guard + `:261-286` split-detect hard-block (`GetSectionUnitsDictAsync`, `splitUserIds.Any()` rollback-in-tx) | closed |
| T-403-02 | Elevation (privilege bypass) | 4 mutation actions | mitigate | `[Authorize(Roles = "Admin, HC")]` ×9 (≥4 req): Edit `:127`, Preview `:321`, Toggle `:409`, Delete `:466` | closed |
| T-403-03 | Tampering (CSRF) | all POST | mitigate | `[ValidateAntiForgeryToken]` ×7 (≥4 req): Edit `:128`, Preview `:322`, Toggle `:410`, Delete `:467` | closed |
| T-403-04 | Tampering (SQL injection) | UserUnits/split-detect queries | mitigate | 0 raw-SQL; parameterized EF LINQ e.g. `:230` `Where(uu => uu.Unit == oldName)` | closed |
| T-403-05 | Info Disclosure | block msg menampilkan NIP/nama | accept | by-design D-01a; msg `:276-282` hanya di action Admin/HC-gated (RBAC inherit T-403-02); no public path | closed |
| T-403-06 | DoS (over-block) | hard-block semua reparent ber-anggota | accept (avoided) | block hanya saat `splitUserIds.Any()` `:274`; single-unit reparent `:288-303` diizinkan | closed |
| T-403-07 | Tampering (client count manipulation) | modal count dihitung klien | accept (avoided) | klien no-compute; tampil server value via `.textContent` `orgTree.js:365`; server-authoritative `:369`(preview)+`:230`(actual) | closed |
| T-403-08 | XSS | render `affectedUserUnitsCount` ke DOM | mitigate | `.textContent` (bukan innerHTML), nilai integer — `orgTree.js:365` | closed |
| T-403-09 | Info Disclosure | modal menampilkan jumlah keanggotaan ke operator | accept | modal hanya di `/Admin/ManageOrganization` (RBAC Admin/HC inherit); count agregat non-PII — `ManageOrganization.cshtml:226` | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-403-01 | T-403-05 | Pesan blok split menampilkan NIP/nama hanya ke role Admin/HC (sudah authorized); operator butuh info ini untuk menyelesaikan keanggotaan lintas-Bagian | Rino (UAT sign-off) | 2026-06-19 |
| AR-403-02 | T-403-06 | Over-block sengaja dihindari: hanya split nyata >1 Bagian yang diblok; single-unit reparent tetap jalan (Test 5 regresi) | Rino | 2026-06-19 |
| AR-403-03 | T-403-07 | Klien tidak menghitung apa pun — hanya menampilkan `affectedUserUnitsCount` server-authoritative; mutasi tetap divalidasi server | Rino | 2026-06-19 |
| AR-403-04 | T-403-09 | Modal hanya muncul di surface Admin/HC; count agregat integer non-PII | Rino | 2026-06-19 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-19 | 9 | 9 | 0 | gsd-security-auditor (verify-all, ASVS L1) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-19
