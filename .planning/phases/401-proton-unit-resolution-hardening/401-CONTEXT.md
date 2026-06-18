# Phase 401: PROTON Unit-Resolution Hardening - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Wave 1 (PARALEL dgn 400 & 403, depends 399). **0 migration** — resolusi/validasi/filter read-path; `ProtonTrackAssignment` tetap 7 kolom TANPA Unit; tidak ada schema/write DB baru.

Yang di-deliver Phase 401 (PSU-01/02/03/04/05/07):

1. **Resolusi unit PROTON dari `AssignmentUnit` eksplisit** — buang fallback `User.Unit` ambigu di 5 resolver. Coachee dapat menjalani Tahun1@unitX → (setelah selesai) Tahun2@unitY **sekuensial**; cert tiap unit tersimpan utuh sbg histori (invariant single-active terjaga). *(PSU-01)*
2. **Switch filter axis** coachee-scope/tim/BypassList PROTON ke `AssignmentUnit` (bukan `User.Unit`) — coachee di-PROTON-kan di unit non-primary tetap tampil di unit benar. *(PSU-02)*
3. **Validasi `AssignmentUnit ∈ coachee.UserUnits`** di Assign/Edit/Import + bypass `TargetUnit ∈ worker.UserUnits` + valid org-tree (bukan cuma non-empty). *(PSU-03)*
4. **No-clobber** `CleanupCoachCoacheeMappingOrg` + Import (jangan reset `AssignmentUnit` ke primary, tutup data-loss vector). *(PSU-04)*
5. **6 read-path skip + audit-warn** bila `AssignmentUnit` kosong — dilarang diam-diam pakai `User.Unit` primary; gate eligibility exam **tak boleh menerbitkan** session/cert dgn unit ter-resolve dari primary. *(PSU-05)*
6. **Reactivation guard** (`CoachCoacheeMappingReactivate` + Import-reactivate): no-clobber + validasi unit aktif + reaktivasi `ProtonTrackAssignment` cocok unit dgn mapping. *(PSU-07)*

**File cluster (disjoint Wave 1):** `CoachMappingController`, `CDPController`, `ProtonDataController`, `ProtonBypassService`, `AssessmentAdminController`.

**OUT of scope:** kolom Unit di `ProtonTrackAssignment` / PROTON paralel (spec §8 — butuh migration); per-coachee unit-picker interaktif di UI assign batch (= CXU-03 Phase 402); coaching cross-unit eligible-list/self-scope (402); membership listing (400); Org cascade (403); test SQL-riil + UAT (404). Cert/analytics per-unit akurat = out-of-scope milestone (D1=b primary).

</domain>

<decisions>
## Implementation Decisions

### PSU-05 — Visibilitas coachee ber-`AssignmentUnit` kosong (di-skip)
- **D-01: Indikator UI on-demand (BUKAN skip senyap).** Selain audit-warn server-side, tampilkan indikator/alert di halaman **CoachCoacheeMapping** (Admin) yang dihitung **on-demand** dari query: mapping **aktif** yg `AssignmentUnit` kosong **atau** `∉ coachee.UserUnits` aktif. Operator (HC/Admin) langsung lihat coachee yg "hilang" dari surface PROTON & bisa perbaiki — tanpa harus baca log. **Reuse pola `CleanupReport` existing** (`CoachMappingController.cs:911` `TempData["CleanupReport"]` + tampilan di view) — bentuk indikator (badge count / alert daftar) = Claude's discretion, ikut idiom Bootstrap 5 existing.
- **D-02: Skip = exclude dari read-path; gate-eligibility = BLOCK penerbitan.** Untuk read-path filter/listing (BypassList, coachee-scope, defensive-filter) → coachee `AssignmentUnit` kosong **di-exclude** dari hasil (tidak tampil). Untuk **gate-eligibility-exam** (`AssessmentAdminController` + `CoachMappingController.GetEligibleCoachees` — penentu penerbitan session/cert) → coachee **tidak eligible** (tak boleh terbit session/cert dgn unit ter-resolve dari primary). Catatan: resolver `:1467-1473` (`AutoCreateProgress`) + gate `:1414-1418` **saat ini punya fallback `User.Unit`** + sudah skip bila dua-duanya kosong → perketat jadi **skip bila `AssignmentUnit` kosong saja** (abaikan primary).

### PSU-05 — Channel audit-warn (hybrid by-path)
- **D-03: Hybrid by-path** (sadar volume).
  - **Read-path skip** (filter/listing/defensive — dipanggil tiap page-load, volume tinggi) → **`_logger.LogWarning` saja**. JANGAN tulis `AuditLog` persisted per-skip (akan membanjiri tabel `AuditLogs` tiap render halaman).
  - **Gate-eligibility-exam BLOCK** (penerbitan session/cert di-tahan krn `AssignmentUnit` kosong — event **langka & signifikan**) → **`AuditLog` persisted** (`_auditLog.LogAsync`, queryable utk review compliance) **+ `_logger.LogWarning`**. Sejalan pola `_auditLog.LogAsync` existing (`CoachMappingController.cs:1081`).

### PSU-04 / PSU-07a — Sumber & preservasi `AssignmentUnit` di Import mapping
- **D-04: Default-primary utk baris BARU + preserve utk reactivate; TANPA ubah template.** Template import coach-coachee **tak punya kolom Unit** (cuma NIP Coach + NIP Coachee; `AssignmentUnit` saat ini diturunkan dari `coacheeUser.Unit` = primary).
  - **Baris BARU** (`CoachMappingController.cs:364-373`) → `AssignmentUnit = coachee primary`, **validasi `∈ coachee.UserUnits`** (primary selalu valid by Invariant #3). Tetap pakai primary (tidak ada sumber unit lain di import; selalu sah).
  - **Reactivate** (`:350-362`, baris `:356`) → **PRESERVE `inactiveMapping.AssignmentUnit` existing** (no-clobber, hapus reset `= coacheeUser.Unit.Trim()`) + validasi `∈ coachee.UserUnits` aktif. Bila unit existing sudah dilepas (`∉ UserUnits`) → tolak/skip baris dgn pesan (jangan diam-diam reset ke primary).
  - **TIDAK menambah kolom Unit di template** (defer ke 402). Pemilihan unit non-primary per-coachee = jalur UI Edit mapping (CXU-03, Phase 402). Import 401 = bulk fast-path single-unit, **backward-compat penuh**, scope minimal. *(Resolusi atas pertanyaan "You decide".)*

### PSU-07c — Reactivation `ProtonTrackAssignment` unit-match (reconcile PTA-no-Unit + AF-4)
- **D-05: Reuse korelasi `DeactivatedAt` existing + tambah validasi unit (no re-architecture).** PSU-07c minta reaktivasi PTA "cocok unit dgn mapping", TAPI **`ProtonTrackAssignment` tak punya kolom Unit** (7 kolom, spec §3) + komentar **AF-4** (`:1043-1051`) "JANGAN ubah logic window di phase ini". Interpretasi yg diadopsi:
  - **Pertahankan** korelasi `DeactivatedAt ±5s` existing di `CoachCoacheeMappingReactivate` (`:1052-1076`) — **JANGAN re-architect window** (respect AF-4).
  - **Tambah** sebelum reaktivasi: validasi `mapping.AssignmentUnit ∈ coachee.UserUnits` aktif (PSU-07b — tolak bila unit sudah dilepas) + **preserve** `AssignmentUnit` (PSU-07a — tak reset ke primary).
  - **True per-unit PTA-match** (memilih PTA spesifik per-unit) butuh kolom `Unit` di `ProtonTrackAssignment` = **migration + langgar 0-migration & PROTON-paralel out-of-scope** → tidak dikerjakan. Korelasi DeactivatedAt sudah meng-ikat PTA ke **event deaktivasi mapping yg sama** (konteks unit yg sama secara de-facto).

### Claude's Discretion
- **Granularity kegagalan validasi** (PSU-03 Assign/Import bila `AssignmentUnit ∉ coachee.UserUnits`): ikut pola existing — **Import** sudah per-row (`results` status `Error`/`Skip`/`Success`) → reject baris + report; **Assign interaktif** → reject coachee bermasalah dgn pesan. Tidak gandakan/ubah pola.
- **Bentuk indikator UI D-01** (badge count vs alert daftar coachee) + lokasi presisi di view CoachCoacheeMapping — ikut idiom Bootstrap 5 + pola CleanupReport.
- **Filter-axis swap PSU-02** (`User.Unit` → `AssignmentUnit`) di site-site read — mekanis ikut spec §5; pertahankan semantik filter, hanya ganti sumbu.
- **Validasi bypass `TargetUnit`** (PSU-03): `∈ worker.UserUnits` + valid org-tree, ganti cek non-empty di `ProtonDataController.cs:1638`. Pola validasi ikut existing.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca sebelum plan/implement.**

### Spec & Requirements (AUTHORITATIVE)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — **§5 "Fase 401 — PROTON unit-resolution hardening"** (touchpoints lengkap: filter-axis `CDPController.cs:491,1586,1596,4248` + `ProtonDataController.cs:1517`; 5 resolver fallback; validasi ∈UserUnits; bypass `TargetUnit` `ProtonDataController.cs:1638`; `CleanupCoachCoacheeMappingOrg:880-907`; Import-mapping `:356,372`); **§3** arsitektur (PROTON unit di-resolve TIDAK disimpan; `ProtonTrackAssignment` 0 kolom Unit; single-active index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`); **§7** invariant (#2 PROTON single-active, #4 `AssignmentUnit ∈ coachee.UserUnits` pasca-401); **§8** out-of-scope (PROTON paralel + kolom unit-at-issue = migration, JANGAN); **§10** risiko (Cleanup clobber = data-loss; mitigasi Fase 401 UserUnits-aware/gated).
- `.planning/REQUIREMENTS.md` lines 24-29 — **PSU-01/02/03/04/05/07** acceptance criteria (PSU-05: 6 read-path skip + audit-warn, gate tak boleh terbit dgn primary; PSU-07: reactivation a/b/c).
- `.planning/ROADMAP.md` §"Phase 401" — Goal + 6 Success Criteria + migration=false.

### Foundation (Phase 399 — sudah COMPLETE)
- `.planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-CONTEXT.md` — junction `UserUnits` (Name-string anak Bagian), kontrak primary-mirror write-through, invariant #3 (primary ∈ UserUnits selalu). **Sumber `coachee.UserUnits` utk validasi PSU-03.**
- `.planning/phases/400-membership-listing-set-aware-rollup-dedup/400-CONTEXT.md` — D-03 (scope keanggotaan `IsActive=true` saja) — pola predikat `uu.Unit == X && uu.IsActive` konsisten utk validasi ∈UserUnits aktif.

### Project workflow (WAJIB ikut)
- `CLAUDE.md` — Develop Workflow (gate `dotnet build`+`dotnet run` localhost:5277 + cek DB lokal + Playwright bila ada UI sebelum commit; notify IT commit-hash + migration flag=FALSE) + Seed Data Workflow (snapshot/restore DB lokal utk fixture multi-unit).
- `docs/DEV_WORKFLOW.md` — environment map + SOP.

</canonical_refs>

<code_context>
## Existing Code Insights (hasil scout codebase)

### 5 Resolver fallback `AssignmentUnit ?? User.Unit` (PSU-01 — buang fallback)
- `Controllers/CoachMappingController.cs:1409-1418` — **GetEligibleCoachees** (gate eligibility — penentu eligible utk exam/session). Fallback `:1414-1418`.
- `Controllers/CoachMappingController.cs:1461-1473` — **AutoCreateProgressForAssignment** (bootstrap deliverable progress). Fallback `:1467-1473`; **sudah skip+warn bila dua-duanya kosong** (`:1475-1479`) → perketat ke AssignmentUnit-only.
- `Controllers/AssessmentAdminController.cs:1411-1414` — gate eligibility exam (penerbitan session/cert — PSU-05 BLOCKING).
- `Controllers/CDPController.cs:515-526` + `:1708-1719` — resolver coachee-scope.

### 6 Read-path skip + audit-warn (PSU-05)
GetEligibleCoachees + AutoCreateProgress (`CoachMappingController`), gate eligibility exam (`AssessmentAdminController`), defensive-filter ×2 (`CDPController`), `ProtonDataController`. Pola skip existing = `if (string.IsNullOrWhiteSpace(resolvedUnit)) continue;` (`:1419`) — perketat: skip bila `AssignmentUnit` kosong (sebelum fallback primary).

### Filter-axis swap (PSU-02)
- `Controllers/CDPController.cs:491` (post-filter coachee), `:1586`, `:1596`, `:4248` (`User.Unit` → `AssignmentUnit`).
- `Controllers/ProtonDataController.cs:1517` — **BypassList** action surface (coachee/worker scope).

### No-clobber sites (PSU-04 / PSU-07a — data-loss vectors)
- `Controllers/CoachMappingController.cs:880-907` **`CleanupCoachCoacheeMappingOrg`** — `:900` `m.AssignmentUnit = userUnit;` reset ke primary coachee bila invalid → jadikan UserUnits-aware/gated (jangan reset ke primary bila unit existing sah `∈ UserUnits`).
- `Controllers/CoachMappingController.cs:356` Import-**reactivate** `inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim();` (clobber) → **preserve existing**.
- `Controllers/CoachMappingController.cs:372` Import-**baru** `AssignmentUnit = coacheeUser.Unit` → default primary + validasi ∈UserUnits (D-04).

### Validasi ∈ UserUnits (PSU-03)
- Assign `:468-473` — saat ini validasi `req.AssignmentUnit ∈ validUnits` (unit org-tree Bagian) → tambah `∈ coachee.UserUnits` per-coachee dalam batch (assign batch single-unit; per-coachee picker = 402).
- Edit `:721,:747-749` (deteksi AssignmentUnit change) — validasi `∈ coachee.UserUnits`.
- Import `:326-328` (validasi Section/Unit coachee vs org) — tambah lapis `∈ coachee.UserUnits`.
- Bypass `ProtonDataController.cs:1638` — saat ini cek non-empty → `∈ worker.UserUnits` + org-tree. (Lihat juga `ProtonBypassService.cs:104-118,:226-231` invariant E8.)

### Reactivation (PSU-07)
- `Controllers/CoachMappingController.cs:1017-1097` **`CoachCoacheeMappingReactivate`** — korelasi PTA via `DeactivatedAt ±5s` (`:1052-1076`); komentar **AF-4** (`:1043-1051`) "JANGAN ubah window logic". D-05: tambah validasi unit SAJA, pertahankan window.
- Import-reactivate `:350-362` (lihat D-04).

### Audit infrastruktur (dua channel)
- `AuditLogService _auditLog` → `_auditLog.LogAsync(actorId, actorName, action, detail, targetId, targetType)` persisted ke `AuditLogs` (`:1081`). **Utk gate-block PSU-05 (D-03).**
- `ILogger<CoachMappingController> _logger` → `_logger.LogWarning(...)` (`:512`). **Utk read-path skip PSU-05 (D-03).**

### De-risk (TIDAK tersentuh)
- Authz Section (`IsResultsAuthorized` `CMPController.cs:2503-2510` + SectionHead L4) 100% Section scalar → 0 perubahan.
- `ProtonTrackAssignment` schema (7 kolom, no Unit) — tetap; PSU-07c TIDAK menambah kolom (D-05).
- Single-active index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` — dipertahankan (invariant #2).

</code_context>

<specifics>
## Specific Ideas

- **PSU-05 skip dua tingkat (D-02):** read-path = exclude (coachee hilang dari list); gate-eligibility-exam = BLOCK (tak terbit session/cert). Bukan perilaku seragam — gate lebih keras krn menyangkut penerbitan cert dgn unit salah.
- **Channel audit by volume (D-03):** keputusan sadar — read-path sering dipanggil → ILogger (app-log, murah); gate-block langka → AuditLog persisted (queryable, mahal-OK). Hindari banjir tabel `AuditLogs`.
- **PSU-07c batasan jujur (D-05):** "PTA cocok unit dgn mapping" hanya bisa di-approx via korelasi DeactivatedAt krn PTA tak punya kolom Unit. True match = migration (out-of-scope). Validasi unit ditambah di sisi mapping (`AssignmentUnit ∈ UserUnits`), bukan di PTA.
- **Import 401 minimal (D-04):** primary-default utk baris baru SELALU sah (invariant #3 primary∈UserUnits); no-clobber yg riil = preserve-on-reactivate. Non-primary picking ditunda 402 (UI). Tak ubah template Excel.

</specifics>

<deferred>
## Deferred Ideas

- **Kolom Unit di Excel import coach-coachee** (operator bulk-set unit non-primary saat import) — di-defer; pemilihan unit non-primary per-coachee = jalur UI Edit/Assign Phase 402 (CXU-03 per-coachee unit-picker).
- **Kolom `Unit` di `ProtonTrackAssignment` + PROTON paralel + true per-unit PTA-match** — out-of-scope milestone (spec §8, butuh migration ke-2 + re-key ~21 `FirstOrDefault(aktif)`).
- **Re-architect window-korelasi reactivation (AF-4 proper fix via `DeactivatedByMappingEventId`)** — parked backlog (butuh migration); JANGAN sentuh di 401 (D-05).
- **Coaching cross-unit eligible-list + self-scope + per-coachee unit-picker (CXU-01..05)** — Phase 402 (Wave 2, serial setelah 401).

### Reviewed Todos (not folded)
- Tidak ada todo pending yg match scope PROTON unit-resolution 401 (todo cleanup-data-test-lokal = chore DB, out of scope).

</deferred>

---

*Phase: 401-proton-unit-resolution-hardening*
*Context gathered: 2026-06-18*
