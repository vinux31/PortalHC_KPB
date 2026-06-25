---
phase: 400
slug: membership-listing-set-aware-rollup-dedup
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-18
---

# Phase 400 — Security

> Kontrak keamanan per-fase: threat register, accepted risks, dan jejak audit.
> Phase 400 = perubahan jalur **baca/filter** (set-aware unit membership via correlated EXISTS terhadap junction `UserUnits`). **0 endpoint baru, 0 input baru, 0 perubahan auth.** Semua mitigasi diverifikasi ADA di kode terimplementasi.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser → CMP/Worker controller | Operator (Admin/HC/Section Head L4) mengirim `unitFilter`/`sectionFilter` sebagai query param ke listing read-path | Query param string (`unitFilter`, `sectionFilter`) — non-sensitif, sudah divalidasi vs Section |
| Controller/Service → EF Core → SQL Server | `unitFilter` masuk ke predikat correlated subquery terhadap junction `UserUnits` | Filter string ter-bind sebagai parameter SQL (`@p`) dalam klausa `EXISTS` |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-400-01 | Tampering (SQLi) | `unitFilter` di predikat `_context.UserUnits.Any(uu => uu.Unit == unitFilter && uu.IsActive)` | mitigate | EF Core LINQ ter-parameterisasi — `unitFilter` di-bind sebagai parameter SQL (`@p`), BUKAN string-concat. Tidak ada `FromSql`/`ExecuteSql`/interpolasi `$"..."` pada jalur predikat. | closed |
| T-400-02 | Elevation of Privilege (scope-widening / broken access control) | Predikat set-aware memperluas match dari primary → semua-unit | mitigate | Perluasan TETAP dalam Section yang sudah ter-otorisasi (junction = anak 1 Bagian, invariant v32.3). Predikat predicate-only, TIDAK menyentuh authz/Section filter. RBAC existing utuh. | closed |
| T-400-03 | Information Disclosure (kolom Unit bocor unit pekerja lain) | Kolom kontekstual `WorkerTrainingStatus.Unit` (comma-join all-active units) | mitigate | `unitsByUser` di-key per `userId` baris itu sendiri — tidak ada cross-user leak. Case filtered hanya menampilkan `unitFilter` yang sudah lolos predikat keanggotaan. | closed |
| T-400-04 | Information Disclosure (unit deactivated muncul) | Predikat + batch-load dict (3 predikat + 3 batch-load) | mitigate | `&& uu.IsActive` (D-03) ada di KEDUA predikat dan SEMUA batch-load (termasuk fix WR-02 + IN-01). Unit yang di-deactivate (jalur MU-07) tidak muncul di roster, badge, maupun export xlsx. | closed |

*Status: open · closed*
*Disposition: mitigate (implementasi wajib) · accept (risiko terdokumentasi) · transfer (pihak ketiga)*

---

## Bukti Verifikasi (per threat)

**T-400-01 — Tampering (SQLi):** Ketiga predikat set-aware memakai EF Core LINQ `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)`:
- `Services/WorkerDataService.cs:261-263` (GetWorkersInSection)
- `Controllers/WorkerController.cs:205-208` (ManageWorkers)
- `Controllers/WorkerController.cs:308-310` (ExportWorkers)

EF Core menerjemahkan ke `WHERE EXISTS(... AND [Unit] = @p)` dengan `unitFilter` ter-bind sebagai parameter. Grep memastikan TIDAK ada `FromSql`/`ExecuteSql`/`string.Format`/interpolasi `$"..."` yang menyentuh `unitFilter` pada kedua file. Idiom by-default safe. **CLOSED.**

**T-400-02 — Elevation of Privilege:** Atribut authz utuh, tidak disentuh Phase 400 (predicate-only):
- `Controllers/WorkerController.cs:14` `[Route("Admin/[action]")]` + extends `AdminBaseController` yang ber-`[Authorize]` class-level (`Controllers/AdminBaseController.cs:12`) → ManageWorkers butuh autentikasi.
- `Controllers/WorkerController.cs:276` `ExportWorkers` ber-`[Authorize(Roles = "Admin, HC")]` (method-level) — utuh.
- `Controllers/CMPController.cs:810` `RecordsTeamPartial` `if (roleLevel >= 5) return Forbid();` + L4 section-lock server-side `:812-813`.

Predikat hanya mengubah klausa `WHERE` keanggotaan unit di dalam Section yang SUDAH ter-otorisasi (junction `UserUnits` = anak 1 Bagian per invariant v32.3, tidak lintas-Bagian). Validasi `unitFilter`-vs-Section (`WorkerController.cs:171-176`/`:280-289`) TIDAK diubah — `unitFilter` di luar Section di-null-kan sebelum predikat. Tidak ada atribut `[Authorize]` yang ditambah atau dihapus. **CLOSED.**

**T-400-03 — Information Disclosure (cross-user unit leak):** Assign kontekstual `Services/WorkerDataService.cs:371-375` memakai `unitsByUser.TryGetValue(user.Id, ...)` — di-key per `user.Id` baris yang sedang di-hidrasi di dalam `foreach (var user in users)`. Dict dibangun dari batch-load `Services/WorkerDataService.cs:287-294` yang di-`GroupBy(uu => uu.UserId)`, jadi tiap entri hanya berisi unit milik pekerja itu sendiri — tidak ada fan-out cross-user. Case filtered (`unitFilter`) hanya tampil bagi pekerja yang sudah lolos predikat keanggotaan aktif. **CLOSED.**

**T-400-04 — Information Disclosure (unit deactivated muncul):** `&& uu.IsActive` (D-03) ada di SEMUA titik (2 predikat WorkerController + 1 predikat service + 3 batch-load):
- Predikat: `WorkerDataService.cs:263`, `WorkerController.cs:208` (ManageWorkers), `WorkerController.cs:310` (ExportWorkers).
- Batch-load: `WorkerDataService.cs:288` (`unitsByUser`), `WorkerController.cs:231` (`userUnitsDict`, fix **IN-01** commit `764397c6`), `WorkerController.cs:326` (`exportUnitsByUser`, fix **WR-02** commit `a0b9468d`).

Fix code-review **WR-02** + **IN-01** menambahkan `&& uu.IsActive` pada dua batch-load yang sebelumnya tidak memfilter aktif, sehingga unit yang di-deactivate via MU-07 disembunyikan di SEMUA permukaan (roster, badge "semua unit", export xlsx). Diverifikasi UAT browser 3/3 PASS (`400-UAT.md`): unit non-aktif **"Sulfur Recovery Unit (169)"** ABSENT di filter (totalRows=0), badge ManageWorkers, dan `sharedStrings.xml` file Excel. **CLOSED.**

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|

No accepted risks.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-18 | 4 | 4 | 0 | gsd-security-auditor |

---

## Sign-Off

- [x] Semua threat punya disposition (4/4 mitigate)
- [x] Accepted risks terdokumentasi di Accepted Risks Log (none)
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set di frontmatter

**Approval:** verified 2026-06-18
