---
phase: 427
slug: exam-token-gate-server-authoritative
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-25
---

# Phase 427 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> FLOW-08 / EXSEC-01 hardening — token-gate verifikasi ujian dipindah dari TempData (client round-trip) ke kolom DB server-authoritative `AssessmentSession.TokenVerifiedAt`.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| browser (worker) → POST /CMP/VerifyToken | Input token akses. `[HttpPost][ValidateAntiForgeryToken]` + authz owner/Admin/HC (tidak diubah). Token compare via `AccessTokenMatches` (tidak diubah). | token akses (shared per batch, by-design) |
| browser (worker) → GET /CMP/StartExam | Gate masuk ujian. Sebelumnya baca TempData (client round-trip, dapat dimanipulasi). Kini baca kolom DB server-authoritative `TokenVerifiedAt`. | gate decision (server-side) |
| controller/service → AssessmentSession.TokenVerifiedAt (DB) | Stamp (VerifyToken POST, sukses token-required) + reset (RetakeService.ExecuteAsync). Keduanya server-side write. | timestamp verifikasi (datetime2 NULL) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-427-01 | Tampering | Token-gate state (TempData round-trip dapat dimanipulasi/bocor lintas sesi) | mitigate | Gate StartExam membaca `assessment.TokenVerifiedAt == null` dari entity DB (`Controllers/CMPController.cs:967`), BUKAN TempData. VerifyToken men-stamp via DB write (`CMPController.cs:902-903`). Reset single-source `RetakeService.ExecuteAsync` (`Services/RetakeService.cs:127`). `grep TokenVerified_` di seluruh `.cs` = 0 hit → tak ada jalur TempData access-token gate tersisa yang bisa dipenuhi nilai client-controlled. | closed |
| T-427-02 | Spoofing / Elevation of Privilege | Akses ujian tanpa token | accept (no new surface) | VerifyToken mempertahankan `[ValidateAntiForgeryToken]` (`CMPController.cs:866`) + authz owner/Admin/HC (`CMPController.cs:881-883`) + `AccessTokenMatches` (`CMPController.cs:895`). Stamp HANYA pada jalur token-required + token valid (`CMPController.cs:900-903`); jalur not-required tinggalkan kolom null (`CMPController.cs:886-891`) — gate hanya cek kolom saat `IsTokenRequired` (`CMPController.cs:964`). Tidak ada endpoint/param/role baru. | closed |
| T-427-03 | Denial of Service | Satu SaveChanges ekstra di VerifyToken POST | accept | Satu `SaveChangesAsync` per verifikasi token (`CMPController.cs:903`), bukan loop. Minor; tak ada amplifikasi. | closed |
| T-427-04 | Tampering (lockout regresi) | Sesi InProgress lama (TokenVerifiedAt null) | mitigate | Guard `assessment.StartedAt == null` dipertahankan PERSIS di gate StartExam (`CMPController.cs:964`) → sesi yang sudah dimulai melewati gate tanpa cek `TokenVerifiedAt` → tak terkunci pasca-deploy. Migration `AddTokenVerifiedAt` nullable (`datetime2 NULL`), no backfill, reversible Down (`Migrations/20260624133656_AddTokenVerifiedAt.cs:14-26`) → zero-downtime. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

### Mitigation Evidence Detail

- **T-427-01 — gate read (server-authoritative):** `Controllers/CMPController.cs:964-972` — di dalam guard `IsTokenRequired && UserId==user.Id && StartedAt==null`, gate membaca `if (assessment.TokenVerifiedAt == null)` dari entity DB; tidak ada `TempData.Peek` token tersisa.
- **T-427-01 — stamp (persist):** `Controllers/CMPController.cs:902-903` — `assessment.TokenVerifiedAt = DateTime.UtcNow; await _context.SaveChangesAsync();` HANYA pada jalur token-required sukses (setelah `AccessTokenMatches`).
- **T-427-01 — reset completeness (single source D-01):** `Services/RetakeService.cs:127` — `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` di chain `ExecuteUpdateAsync`. Kedua entry point delegasi: worker `RetakeExam` (`Controllers/CMPController.cs:2580`) + HC `ResetAssessment` (`Controllers/AssessmentAdminController.cs:4392`). Cleanup TempData token dihapus di kedua jalur (`CMPController.cs:2587-2588`, `AssessmentAdminController.cs:4409-4410`).
- **T-427-01 — full replacement (D-02):** `grep -rn "TokenVerified_" **/*.cs` = 0 hit (seluruh repo, bukan hanya Controllers/ + Services/). `AutoSubmitToken_*` adalah concern terpisah (bukan access-token gate) dan di luar scope.
- **T-427-04 — migration zero-downtime:** `Up` = `AddColumn datetime2 nullable:true` (no backfill); `Down` = `DropColumn` (reversible). Snapshot `Migrations/ApplicationDbContextModelSnapshot.cs:563` + Designer `Migrations/20260624133656_AddTokenVerifiedAt.Designer.cs:566` memuat `b.Property<DateTime?>("TokenVerifiedAt")`. Applied ke `HcPortalDB_Dev` (SUMMARY: `sqlcmd COL_LENGTH=8`).

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-427-01 | T-427-02 | Tidak ada surface baru. Gate, CSRF, authz owner/Admin/HC, dan `AccessTokenMatches` tidak diubah — fase ini hanya memindah state gate dari TempData ke kolom DB. Token akses by-design shared per batch (lihat `Models/AssessmentSession.cs:99-104`); identitas peserta ditangani ASP.NET Core Identity. | gsd-security-auditor (verified existing controls) | 2026-06-25 |
| AR-427-02 | T-427-03 | Satu `UPDATE` per verifikasi token (POST one-shot, bukan loop/amplifikasi). Beban dapat diabaikan; setara satu mutasi state per aksi user. | gsd-security-auditor | 2026-06-25 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-25 | 4 | 4 | 0 | gsd-security-auditor |

**Unregistered flags:** Tidak ada. SUMMARY.md (`427-01-SUMMARY.md`) tidak memuat section `## Threat Flags`; deviasi yang dilaporkan (NoOpHubContext + StubUrlHelper) murni wiring harness test, bukan attack surface.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-25
