---
phase: 365
slug: test-hardening-coach-coachee-af-3-xunit
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-12
---

# Phase 365 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Phase TEST-ONLY behavior-preserving refactor (extract static core). Zero new attack surface — satu-satunya perubahan produksi = pindah logika DB ke method static yang dipanggil wrapper yang sama; sisanya file test xUnit.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| HTTP client → POST /Admin/MarkMappingCompleted | Wrapper memegang authz + anti-forgery; boundary TIDAK berubah pasca-refactor | `mappingId` (int route/form) |
| Wrapper → static core `MarkMappingCompletedCore` | Pemanggilan internal in-process; core TIDAK ter-route sebagai HTTP action | `(ApplicationDbContext, int)` in-memory |
| Test → real-SQL fixture `ProtonCompletionFixture` | DB disposable `HcPortalDB_Test_<guid>`; `HcPortalDB_Dev` tak tersentuh | Seed data sintetik (no PII produksi) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-365-01 | Elevation of Privilege | `MarkMappingCompletedCore` (static, public) | mitigate | Core BUKAN HTTP endpoint — grep konfirmasi 0 atribut routing pada core. Authz `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` + `[HttpPost]` tetap di wrapper VERBATIM (grep: ketiga ada tepat di atas `MarkMappingCompleted(int mappingId)`). Satu-satunya jalur HTTP tak berubah. | closed |
| T-365-02 | Tampering | Atomicity mutasi DB | accept | Transaksi tetap di wrapper (D-03): `BeginTransactionAsync` sebelum core, `CommitAsync`/`RollbackAsync` sesudah. Core `SaveChangesAsync` dalam scope transaksi wrapper → atomicity dipertahankan. Zero behavior change vs produksi lama yang juga ber-transaksi (bukti: full suite 236/236). | closed |
| T-365-03 | Information Disclosure | Pesan error endpoint | accept | Catch generik `"Operasi gagal. Semua perubahan dibatalkan."` (grep konfirmasi, no `ex.Message` leak). Error guard pakai token domain-safe ("Tahun 3"/"belum lulus"/"Mapping tidak ditemukan."), tak bocorkan internal. Preserved verbatim dari produksi. | closed |
| T-365-04 | Information Disclosure | Connection string fixture test | accept | `localhost\SQLEXPRESS` + Integrated Security (NO secret/env var) di fixture EXISTING yang di-REUSE — bukan ditambah phase ini. DB disposable; produksi/dev tak tersentuh. | closed |
| T-365-05 | Tampering | DB lokal test | accept | Fixture `DisposeAsync` → `EnsureDeletedAsync` drop DB disposable per run (sukses & gagal-mid). Tidak menyentuh `HcPortalDB_Dev`/produksi. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| R-365-01 | T-365-02 | Atomicity di-preserve apa adanya (transaksi wrapper); zero behavior change dibuktikan parity 236/236. Bukan risiko baru — perilaku produksi lama identik. | Rino | 2026-06-12 |
| R-365-02 | T-365-03 | Pesan error domain-safe, generik di catch — pola produksi existing, tak diubah phase ini. | Rino | 2026-06-12 |
| R-365-03 | T-365-04 | Connection string hardcoded localhost\SQLEXPRESS Integrated Security di fixture EXISTING (ProtonCompletionServiceTests) — tidak ditambah, tidak ada secret. Test-only, tak ter-deploy. | Rino | 2026-06-12 |
| R-365-04 | T-365-05 | DB test disposable di-drop per run; produksi/dev tak tersentuh. Test-only. | Rino | 2026-06-12 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-12 | 5 | 5 | 0 | Claude (grep-verified, no auditor — zero new attack surface) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-12
