# Phase 402: Coaching Cross-Unit Mapping - Context

**Gathered:** 2026-06-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Wave 2 — **SERIAL setelah 401** (dua-duanya berat di `CoachMappingController` + `CDPController`, dan 402 menulis `AssignmentUnit` yang aturan validasinya [∈ coachee.UserUnits, PSU-03] dibuat di 401). **0 migration** — eligible-list/guard/payload-reshape/self-scope; tidak ada schema/write DB baru. Depends on 401 (done).

Yang di-deliver Phase 402 (CXU-01/02/03/04/05):

1. **Eligible-coachee set-aware** — daftar coachee eligible = semua coachee di **Bagian coach** (lintas unit), bukan hanya unit coach (`coachee.Section == coach.Section`). *(CXU-01)*
2. **Server guard cross-Bagian** — endpoint assign menolak coachee yang `coachee.Section != coach.Section` (saat ini TAK ADA perbandingan coach.Section vs coachee.Section). *(CXU-02)*
3. **`AssignmentUnit` per-coachee** — payload di-reshape dari 1 nilai batch → map `coacheeId→unit`; dropdown unit per-baris bersumber `coachee.UserUnits`; tiap unit divalidasi per PSU-03 (helper 401). *(CXU-03)*
4. **Relax JS lock** "satu batch = satu unit" → level Bagian (boleh multi-unit dalam 1 Bagian dalam satu batch). *(CXU-04)*
5. **Self-scope coach multi-unit** — `CDPController` self-scope `unit = user.Unit` (305/326/647) → union `IN(coach.UserUnits)` sehingga coach akun multi-unit melihat & meng-export **semua** coachee-nya di seluruh unit dalam Bagian. *(CXU-05)*

**File cluster:** `CoachMappingController` (assign/eligible), `CDPController` (self-scope), view `Views/Admin/CoachCoacheeMapping.cshtml` (modal assign + JS).

**OUT of scope:** kolom Unit di Excel import coach-coachee (per-coachee bulk-set unit non-primary) — tetap defer (401 D-04); import tetap single-unit primary-default. Kolom Unit di `ProtonTrackAssignment` / PROTON paralel (migration, spec §8). Multi-Bagian per akun (mutasi). Cert/analytics per-unit (D1=b primary). Test SQL-riil + UAT cross-unit coaching = Phase 404.

</domain>

<decisions>
## Implementation Decisions

### CXU-03 / CXU-04 — UI picker unit per-coachee + relax lock
- **D-01: Inline per-baris KONDISIONAL.** Dropdown unit muncul di baris coachee **hanya bila** `coachee.UserUnits` aktif > 1 unit; coachee single-unit **auto-pakai** unit-nya tanpa dropdown (minim UI churn — mayoritas coachee single-unit). Expose `coachee.UserUnits` (aktif) ke client menggantikan `data-unit="@coachee.Unit"` scalar (`CoachCoacheeMapping.cshtml:442`) — bentuk (data-attr JSON vs render server-side) = Claude's discretion.
- **D-01b: Relax JS lock (CXU-04).** Hapus AF-2 lock single-unit (`updateAssignmentDefaults` :715-765, terutama lock `:729-738`) + backstop submit `selectedUnits.size > 1` (:777-784); ganti aturan ke **level Bagian** (semua coachee tercentang harus 1 Bagian = coach.Section; multi-unit dibolehkan). Hint teks lock lama (:463-465) diganti pesan baru sesuai per-coachee picker.

### CXU-03 — Default unit coachee multi-unit
- **D-02: Default PRIMARY, bisa diubah.** Saat coachee >1 unit, dropdown pre-select unit primary coachee (konsisten D1=b); operator bisa ganti ke unit lain ∈ `coachee.UserUnits` aktif. Tiap pilihan divalidasi server-side `∈ coachee.UserUnits` via helper **`ValidateAssignmentUnitInUserUnits`** (dibuat 401-01) per-coachee dalam batch (perketat loop existing `:531-535` yang kini pakai 1 `req.AssignmentUnit` untuk semua → jadi per-coachee).

### CXU-01 / CXU-02 — Alur modal + scoping Bagian
- **D-03: Coach-first auto-scope + lock Section.** Pilih coach → coachee checklist **auto-filter** ke `coachee.Section == coach.Section` (eligible set-aware CXU-01) + `AssignmentSection` **auto-set = coach.Section** (dropdown Bagian di-lock/sembunyi — Invariant #1: 1 batch = 1 Bagian = Bagian coach). **Guard server backstop (CXU-02):** endpoint `CoachCoacheeMappingAssign` menolak coachee `coachee.Section != coach.Section` (tambah perbandingan baru — saat ini absen).
- **D-03b: Eligible loader set-aware.** Loader eligible-coachee saat ini **global** (`CoachMappingController.cs:172-175` — `coacheeRoleUsers` filter `IsActive && !activeCoacheeIds.Contains`, tanpa Section). Sediakan eligible scoped ke `coach.Section` — mekanisme (client-filter by `data-section` + server-enforce, atau AJAX endpoint per-coach yang return coachee Bagian coach + UserUnits-nya) = Claude's discretion; **server WAJIB tetap enforce** (CXU-02) apa pun mekanisme client.

### CXU-05 — Self-scope coach multi-unit
- **D-04: Union semua unit by DEFAULT.** Self-scope coaching-role `unit = user.Unit` (scalar primary) diganti agar coach multi-unit lihat **union** coachee `IN(coach.UserUnits)`. Site riil self-scope = **`CDPController.cs:305`** (`FilterCoachingProton`), **`:326`** (`ExportDashboardProgress`), **`:647`** (`lockedUnit`). Bila operator pilih unit spesifik (filter), filter per-unit **tetap jalan** (post-filter `:490-503` sudah `AssignmentUnit`-aware dari 401/PSU-02 — TIDAK diubah). Mekanisme union-expand (mis. saat `unit` kosong utk coaching-role → resolve semua AssignmentUnit ∈ coach.UserUnits) = Claude's discretion.
  - **Catatan akurasi:** ROADMAP/spec sebut "636" — line `:636` aktual = `statusData` doughnut chart (BUKAN self-scope). Self-scope sebenarnya di `:647` (`lockedUnit`). Planner pakai `:305/:326/:647`, verifikasi runtime.

### Payload reshape (CXU-03)
- **D-05: `CoachAssignRequest` (`CoachMappingController.cs:1863`) tambah map per-coachee.** Saat ini `AssignmentUnit` tunggal (`:1870`) dipakai untuk semua coachee. Tambah field map `coacheeId→unit` (Dictionary<string,string> atau List<{coacheeId,unit}> = Claude's discretion); validasi per-coachee per PSU-03. JS `submitAssign` (:767-795) kirim map, bukan 1 `AssignmentUnit`. `AssignmentSection` tetap tunggal (= coach.Section).

### Claude's Discretion
- Bentuk expose `coachee.UserUnits` ke client (data-attr JSON vs server-render dropdown per-baris).
- Shape payload map `coacheeId→unit` (Dictionary vs List of pairs) di `CoachAssignRequest`.
- Mekanisme eligible-loader set-aware (client-filter + server-enforce vs AJAX endpoint per-coach) — server-enforce CXU-02 WAJIB ada apa pun pilihannya.
- Mekanisme union-expand self-scope CDP (handling `unit` kosong utk coaching-role).
- Teks hint pengganti pesan lock lama (:463-465).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca sebelum plan/implement.**

### Spec & Requirements (AUTHORITATIVE)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — **§5 "Fase 402 — Fitur A: coach × coachee cross-unit mapping"** (relax JS lock `CoachCoacheeMapping.cshtml:717-726,765-772`; guard unit coachee ⊆ Bagian coach via `GetSectionUnitsDictAsync`; coach-assign unit-picker per-coachee `CoachMappingController.cs:572-580`; eligible set-aware; self-scope `CDPController.cs:305,326,636`); **§6** dependency (402 serial setelah 401, critical path 399→401→402→404); **§7** invariant (#1 Section scalar 1-Bagian, #4 `AssignmentUnit ∈ coachee.UserUnits`); **§8** out-of-scope (PROTON paralel + kolom unit = migration, JANGAN).
- `.planning/REQUIREMENTS.md` — **CXU-01/02/03/04/05** acceptance criteria.
- `.planning/ROADMAP.md` §"Phase 402" — Goal + 6 Success Criteria + migration=false.

### Dependency (Phase 401 — sudah COMPLETE, WAJIB baca)
- `.planning/phases/401-proton-unit-resolution-hardening/401-CONTEXT.md` — helper **`ValidateAssignmentUnitInUserUnits`** (401-01, reuse per-coachee CXU-03); PSU-03 validasi `∈ coachee.UserUnits`; PSU-02 filter-axis CDP post-filter (`:490-503` sudah AssignmentUnit-aware — JANGAN ubah); D-04 import single-unit primary-default (alasan import tetap out-of-scope 402).

### Foundation (Phase 399/400 — COMPLETE)
- `.planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-CONTEXT.md` — junction `UserUnits` (Name-string anak Bagian, IsPrimary/IsActive), kontrak primary-mirror; **sumber `coachee.UserUnits` untuk dropdown + validasi**.
- `.planning/phases/400-membership-listing-set-aware-rollup-dedup/400-CONTEXT.md` — D-03 (scope keanggotaan `IsActive=true` saja); pola predikat `uu.Unit == X && uu.IsActive` konsisten.

### Project workflow (WAJIB ikut)
- `CLAUDE.md` — Develop Workflow (gate `dotnet build` + `dotnet run` **localhost:5270 di branch ITHandoff** [bukan 5277 — hindari tabrakan worktree main] + cek DB lokal + Playwright bila ada UI sebelum commit; notify IT commit-hash + migration=FALSE) + Seed Data Workflow (snapshot/restore DB lokal utk fixture multi-unit).
- `docs/DEV_WORKFLOW.md` — environment map + SOP.

</canonical_refs>

<code_context>
## Existing Code Insights (hasil scout codebase)

### Endpoint assign (CXU-02/03 — guard + per-coachee unit)
- `Controllers/CoachMappingController.cs:510-554` **`CoachCoacheeMappingAssign`** (POST, [FromBody] `CoachAssignRequest`). Saat ini: validasi `AssignmentSection`/`AssignmentUnit` tunggal (`:522-528`) + loop PSU-03 `ValidateAssignmentUnitInUserUnits` per coachee pakai **1 `req.AssignmentUnit`** (`:531-535`) + cek duplikat active mapping (`:537-554`). **Belum ada** perbandingan `coach.Section == coachee.Section` (CXU-02 tambah). Per-coachee unit → ubah loop `:531-535` pakai map.
- `Controllers/CoachMappingController.cs:1863-1873` **`CoachAssignRequest`** — `AssignmentUnit` tunggal `:1870` → tambah map per-coachee (D-05).
- `Controllers/CoachMappingController.cs:525` `GetSectionUnitsDictAsync()` (validasi unit ∈ org-tree Bagian; reuse).

### Eligible-coachee loader (CXU-01 — set-aware)
- `Controllers/CoachMappingController.cs:148-178` modal data. `:172-175` `ViewBag.EligibleCoachees = coacheeRoleUsers.Where(IsActive && !activeCoacheeIds.Contains)` — **global, tanpa Section** → scoped ke coach.Section (D-03b).

### View modal assign + JS lock (CXU-03/04)
- `Views/Admin/CoachCoacheeMapping.cshtml:428-482` modal: coachee checklist `:439-462` (`coachee-item` `data-section`/`data-unit` **primary scalar** `:442` → expose UserUnits set); AssignmentSection dropdown `:467-475`; AssignmentUnit dropdown `:477-481` (→ per-baris).
- `:463-465` hint lock lama (ganti). `:715-765` `updateAssignmentDefaults` (AF-2 lock `:729-738`, auto-fill `:751-763`). `:767-795` `submitAssign` (payload `:788-795` AssignmentUnit tunggal → map; backstop `:777-784` hapus/relax).

### CDP self-scope (CXU-05 — union)
- `Controllers/CDPController.cs:305` `FilterCoachingProton` `else if (IsCoachingRole) { section=user.Section; unit=user.Unit; }` → union UserUnits.
- `Controllers/CDPController.cs:326` `ExportDashboardProgress` (sama).
- `Controllers/CDPController.cs:647` `lockedUnit = user.Unit` (coaching-role).
- `Controllers/CDPController.cs:490-503` **post-filter coachee by unit** — SUDAH `AssignmentUnit`-aware (Phase 401/PSU-02). **JANGAN ubah** — tetap jalan saat operator filter per-unit.

### Reuse / jangan-sentuh
- Helper `ValidateAssignmentUnitInUserUnits(_context, coacheeId, unit)` (401-01) — reuse per-coachee.
- `UserRoles.IsCoachingRole(roleLevel)` / `HasSectionAccess` / `HasFullAccess` — gate scope existing.
- Single-active index + invariant #2 (PROTON single-active) — tak tersentuh (402 tak ubah ProtonTrackAssignment).

</code_context>

<specifics>
## Specific Ideas

- **AssignmentSection tetap 1 per batch = coach.Section** (Invariant #1: 1 Bagian/akun). Hanya **Unit** yang per-coachee. Cross-Bagian = mutasi (out-of-scope).
- **Dropdown unit kondisional (D-01):** mayoritas coachee single-unit → tampil tanpa dropdown (auto unit-nya); hanya coachee multi-unit yang dapat picker. Mengurangi noise UI.
- **Default primary (D-02)** konsisten D1=b (atribusi primary) tapi bisa override — operator yang tahu unit penugasan riil bisa pilih.
- **Self-scope post-filter sudah AssignmentUnit-aware (401):** 402 cuma **expand self-scope axis** (305/326/647) dari primary scalar ke union; filter per-unit downstream tak perlu disentuh.
- **Server-enforce CXU-02 WAJIB** apa pun mekanisme client eligible-list — client filter cuma UX, bukan security boundary.

</specifics>

<deferred>
## Deferred Ideas

- **Kolom Unit di Excel import coach-coachee** (operator bulk-set unit non-primary saat import) — di-defer (401 D-04); 402 fokus UI assign interaktif. Import tetap single-unit primary-default.
- **Kolom `Unit` di `ProtonTrackAssignment` + PROTON paralel** — out-of-scope milestone (spec §8, butuh migration ke-2).
- **Multi-Bagian per akun / mutasi cross-Bagian** — proses terpisah, out-of-scope (Invariant #1).
- **Test SQL-riil cross-unit coaching + UAT + docs D1=b** — Phase 404 (Wave 3).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship` (area database, score 0.2) — chore cleanup DB lokal, **tidak masuk scope** coaching cross-unit 402.

</deferred>

---

*Phase: 402-coaching-cross-unit-mapping*
*Context gathered: 2026-06-19*
