# Phase 400: Membership Listing Set-Aware + Rollup Dedup - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Wave 1 (PARALEL dgn 401 & 403, depends 399). **0 migration** — murni read/filter/display-path.

Yang di-deliver Phase 400 (MU-06):
1. **Listing set-aware** — pekerja anggota >1 unit muncul saat difilter per **tiap** unit-nya (bukan hanya `u.Unit==unitFilter` scalar = primary saja). Berlaku di:
   - `WorkerDataService.GetWorkersInSection` (sumber tabel CMP records team + export + Team View)
   - `WorkerController.ManageWorkers` + `ExportWorkers` (filter unit — 399 sengaja tunda ke 400)
2. **Rollup Bagian dedup** — completion%/pass-rate/denominator hitung tiap pekerja multi-unit **sekali** (no double-count).
3. Kolom Unit di tabel CMP records team jadi **kontekstual** (lihat D-02).

**OUT of scope:** CMP analytics/renewal (tetap primary, D1=b — TIDAK disentuh); PROTON unit-resolution (401); coaching cross-unit (402); Org cascade (403); test SQL-riil + UAT (404). Cert/analytics per-unit akurat = out-of-scope milestone (spec §8, D1=b).

</domain>

<decisions>
## Implementation Decisions

### Set-aware filter — cara & scope (MU-06)
- **D-01: 1 baris per pekerja (predikat `.Any`, BUKAN gandakan baris).** Set-aware = ubah predikat filter unit dari scalar `u.Unit == unitFilter` jadi keanggotaan `u.UserUnits.Any(uu => uu.Unit == unitFilter && uu.IsActive)`. Pekerja {X,Y}: filter unit-X tampil, filter unit-Y tampil, tanpa filter tampil **1 baris**. **Dedup rollup otomatis** (1 pekerja = 1 baris → denominator/completion% inheren benar; tak ada JOIN fan-out yang menggelembungkan count). Baris-per-unit (gandakan) **DITOLAK** — tabel CMP records team & ManageWorkers itu list datar 1 dropdown unit (tak group-by-unit), gandakan butuh JOIN+dedup eksplisit+pagination-adjust+view-rework tanpa manfaat.
- **D-03: Scope keanggotaan = `IsActive = true` saja.** Unit yg sudah di-deactivate (jalur MU-07 auto-deactivate Phase 399) **TIDAK** muncul di roster. Predikat: `uu.Unit == unitFilter && uu.IsActive`.

### Kolom Unit di tabel CMP records team (`WorkerTrainingStatus.Unit`) — kontekstual (MU-06)
- **D-02: Kontekstual.** Saat **difilter** unit-X → kolom Unit tampil **`unitFilter`** (unit yg cocok, mis. "X"). Saat **tanpa filter unit** (seluruh Bagian / My-Records) → tampil **semua unit primary-first comma-join** (mis. "X, Y" — konsisten pola display 399 D-07/D-08). Ringkas: `Unit = !string.IsNullOrEmpty(unitFilter) ? unitFilter : <semua unit aktif primary-first comma-join>`.
- **D-04:** Karena D-02 butuh daftar unit per pekerja, `GetWorkersInSection` **load `UserUnits`** untuk users hasil filter (query dict `userId → units`, pola sama `WorkerController.ManageWorkers` `ViewBag.UserUnitsDict` di 399). Primary-first ordering: `OrderByDescending(uu => uu.IsPrimary).ThenBy(uu => uu.Unit)`.
- **D-05:** Pekerja **0 baris UserUnits aktif** (legacy/`Unit=null`) → fallback `WorkerTrainingStatus.Unit = user.Unit ?? "---"` (perilaku existing dipertahankan, D-09 Phase 399).

### Scope display ManageWorkers vs CMP records team
- **D-06:** Kontekstual (D-02) berlaku **hanya** untuk `WorkerTrainingStatus.Unit` (kolom tabel CMP records team `_RecordsTeamBody.cshtml:27`). **ManageWorkers TIDAK di-rework display-nya** — sudah tampil semua unit via badge `ViewBag.UserUnitsDict` (399 D-08). Di ManageWorkers Phase 400 cukup ubah **predikat filter** `unitFilter` jadi set-aware active-only (`WorkerController.cs:202-204` + `ExportWorkers:300-301`); badge display biarkan apa adanya.

### Claude's Discretion
- Bentuk perubahan `WorkerTrainingStatus` (set `.Unit` kontekstual in-place vs tambah field `UnitsCsv`/`AllUnits`) — ikut idiom model existing, minimal.
- Atribut `data-unit` (`_RecordsTeamBody.cshtml:18`) — **tak ada pembaca client-side** (filter unit dilakukan server-side via `RecordsTeamPartial`). Boleh ikut nilai display kontekstual atau biarkan; verifikasi tak ada JS yg baca saat planning.
- Apakah perlu OR-fallback scalar `u.Unit == unitFilter` di predikat untuk robust thd baris belum-backfill — backfill 399 sudah cover semua `Unit` non-null + invariant #3 mirror dijaga write-through, jadi `.Any()` murni sudah cukup & benar. Tambah fallback hanya bila researcher temukan baris anomali. Lean: `.Any()` murni.
- Styling/format koma-join ikut idiom Phase 399 (primary-first comma-join, teks polos).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & requirements
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` §5 "Fase 400 — Membership listing set-aware" (touchpoints: `GetWorkersInSection:255`, `WorkerTrainingStatus.Unit:347`, `WorkerController` role-filter `:78,160`, dedup rollup, **CMP analytics/renewal TIDAK diubah**) + §2 (D1=b primary locked) + §7 invariant + §8 out-of-scope.
- `.planning/REQUIREMENTS.md` — MU-06 (line 19): listing set-aware + rollup dedup; MU-01..05/07 (Phase 399, done) untuk konteks junction.
- `.planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-CONTEXT.md` — D-07/D-08 (pola display semua unit primary-first comma-join), D-09 (fallback 0-unit), D1=b (analytics/cert primary).

### Kode touchpoints
- `Services/WorkerDataService.cs:244` `GetWorkersInSection` — predikat unit `:254-255` (scalar → set-aware active-only); set `WorkerTrainingStatus.Unit` `:347` (kontekstual D-02).
- `Services/IWorkerDataService.cs` — signature `GetWorkersInSection` (cek bila perlu param tambahan; lean tak berubah, kontekstual pakai `unitFilter` yg sudah ada).
- `Controllers/WorkerController.cs:165` `ManageWorkers` predikat `:202-204` + `:271` `ExportWorkers` `:300-301` (set-aware active-only); pola `UserUnitsDict` `:221-231` (reuse untuk load UserUnits).
- `Controllers/CMPController.cs` konsumen `GetWorkersInSection`: `:543` Team View list (no unit filter → tak terdampak predikat), `:710`/`:771` export team, `:819` `RecordsTeamPartial` (pagination by `workerList.Count` — aman krn 1-baris/pekerja).
- `Models/WorkerTrainingStatus.cs` — field `Unit` (kontekstual).
- `Views/CMP/_RecordsTeamBody.cshtml:27` kolom Unit (`@worker.Unit`) + `:18` `data-unit` (tak ada pembaca client-side).
- `HcPortal.Tests/FakeWorkerDataService.cs` + `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — pola test set-aware (tambah test multi-unit filter + dedup).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`WorkerController` UserUnitsDict pattern (399, `:221-231`)** — load `UserUnits` group-by userId, primary-first ordering. Reuse persis di `GetWorkersInSection` untuk bangun kolom Unit kontekstual (D-04).
- **`_context.GetSectionUnitsDictAsync()`** — sudah dipakai di CMPController `:561`; validasi/daftar unit per Bagian bila perlu.
- **Pola display Phase 399 (D-07/D-08)** — primary-first comma-join (`OrderByDescending(IsPrimary).ThenBy(Unit)`), teks polos.

### Established Patterns
- `GetWorkersInSection` load users lalu hydrate per-user (trainings/sessions dict) — tambahkan satu dict `UserUnits` (1 query, group-by userId) konsisten gaya CMP-25 batch-load (no N+1).
- Filter unit di 3 tempat scalar identik (`u.Unit == unitFilter`) — ganti ketiganya jadi `u.UserUnits.Any(uu => uu.Unit == unitFilter && uu.IsActive)`.

### Integration Points
- **Pagination** `RecordsTeamPartial` (`CMPController.cs:824`) pakai `workerList.Count` → 1-baris/pekerja jaga count akurat (no skew). Tak perlu adjust.
- **Export** team (`:710`,`:771`) pakai `filteredWorkers.Select(w => w.WorkerId)` → set-aware filter otomatis ikut; export harus konsisten dgn on-screen.
- **Analytics** (`CMPController` GroupBy `s.User.Section` `:2589+`) + `:543` Team View call **tanpa** unitFilter → predikat set-aware tak menyala → **0 drift D1=b** (SC#3 terverifikasi by-construction).

</code_context>

<specifics>
## Specific Ideas

- D-02 kontekstual dipilih (bukan "primary saja" maupun "selalu semua unit"): saat operator filter unit-X mereka ingin lihat "X" di kolom (relevansi), saat lihat seluruh Bagian ingin lihat semua keanggotaan ("X, Y"). Kombinasi paling informatif tanpa gandakan baris.
- D-01 + dedup: kunci milestone — implement via `.Any()` predikat (no JOIN fan-out) membuat dedup rollup **gratis/by-construction**, bukan post-hoc `Distinct(WorkerId)`.

</specifics>

<deferred>
## Deferred Ideas

- **Baris-per-unit (gandakan)** — ditolak (D-01). Bila kelak butuh roster grouped-by-unit eksplisit, itu fitur/phase tersendiri (butuh JOIN + dedup eksplisit + view group baru).
- **CMP analytics/renewal per-unit akurat** — out-of-scope milestone (D1=b primary; butuh kolom unit-at-issue + migration ke-2, spec §8). Phase 400 hanya verifikasi **tak ada drift** (analytics tetap primary).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` (score 0.4) — chore cleanup DB test lokal pasca-367; **tidak di-fold** (di luar scope MU-06 read/filter-path, bukan kebutuhan listing set-aware).

</deferred>

---

*Phase: 400-membership-listing-set-aware-rollup-dedup*
*Context gathered: 2026-06-18*
