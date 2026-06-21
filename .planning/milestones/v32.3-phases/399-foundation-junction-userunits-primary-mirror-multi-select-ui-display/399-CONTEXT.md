# Phase 399: Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Fondasi multi-unit (Wave 0, SOLO — semua fase berikut nunggu). Pekerja boleh anggota **>1 Unit dalam 1 Bagian** (Section tetap scalar). Yang di-deliver Phase 399:

1. Tabel junction `UserUnits` (model + DbSet + index filtered-unique primary) + migration `AddUserUnitsTable` + backfill 1 primary-row/pekerja existing. **migration=TRUE.**
2. Kontrak **write-through primary-mirror**: tiap write (Create/Edit/Import Worker) tulis baris `UserUnits` + sinkron `ApplicationUser.Unit = <unit primary>`; hapus primary → promote/blok.
3. UI **Bagian single + Unit multi-select** (cascade Bagian→unit-anak) di Create/Edit + format Bulk Import multi-unit.
4. Display **semua unit** (primary ditandai) lintas surface.
5. Validasi tiap junction-write `Unit ∈ unit-Bagian` + audit set-diff + guard hapus-unit (MU-07).

**Requirements:** MU-01, MU-02, MU-03, MU-04, MU-05, MU-07.

**OUT of scope (fase lain / milestone lain):** membership listing set-aware + rollup dedup (MU-06 → Phase 400); PROTON unit-resolution (401); coaching cross-unit mapping (402); OrganizationController cascade (403); test SQL-riil + UAT (404). PROTON paralel, cert per-unit akurat, multi-Bagian, multi-role = out-of-scope milestone (spec §8).

</domain>

<decisions>
## Implementation Decisions

### Widget multi-select Unit + penanda primary (Create/Edit Worker) — MU-01/MU-02
- **D-01:** Widget = **checkbox-list unit + radio "Primary" per baris**. Pilih Bagian → `initSectionUnitCascade` (varian) render checkbox-list semua unit Bagian itu dari `ViewBag.SectionUnitsJson` (reuse dict yg sama, client-side, no AJAX baru). Centang checkbox = anggota unit; radio "Primary" per baris menandai tepat 1 unit primary.
- **D-02:** Radio "Primary" hanya enabled untuk unit yg checkbox-nya tercentang (uncheck → radio disable/clear). Bila ≥1 unit dipilih tapi belum ada primary → default primary = unit tercentang pertama (operator boleh ubah). 0 unit dipilih → tak ada primary (valid, `ApplicationUser.Unit=null`).
- **D-03:** TIDAK pakai native `<select multiple>` (UX ctrl-click buruk) maupun chip/tag input (tak ada preseden codebase, risk konsistensi). Checkbox-list dipilih karena reuse penuh dict existing + primary eksplisit + no lib baru.

### Format Bulk Import Excel multi-unit — MU-04
- **D-04:** **1 sel delimiter, unit-pertama = primary.** Kolom Unit existing (`Cell(6)`) TIDAK bergeser; isi `"UnitA|UnitB|UnitC"` (delimiter **pipe `|`**), unit PERTAMA = primary. Parser `split('|')` + trim + dedup; tiap unit divalidasi **∈ unit-Bagian baris itu** (pakai `sectionUnitsDict[bagian]` seperti validasi existing).
- **D-05:** **Backward-compat WAJIB** — template lama (1 unit tanpa delimiter) tetap valid (split menghasilkan 1 elemen = primary). TIDAK pakai kolom "Primary" terpisah (geser layout) maupun multi-baris-per-pekerja (redundansi field non-unit + rawan konflik antar-baris).
- **D-06:** Update worker import template/docs: catat format `UnitA|UnitB` + aturan "unit pertama = primary".

### Display multi-unit lintas surface — MU-03
- **D-07:** **Semua surface tampil SEMUA unit** (primary ditandai), **termasuk `_PSign`** (kartu tanda tangan / cert-print). Keputusan sadar operator: `_PSign` ikut tampil semua unit, BUKAN primary-only. (Catatan tradeoff: cert atribusi tetap primary [D1=b] sementara kartu print list semua unit — diterima sengaja; lihat Specifics.)
- **D-08:** Format penanda: surface HTML (Profil, WorkerDetail, Settings, ManageWorkers, Home) tampil unit sbg badge/chip dgn primary di-highlight (mis. badge "Utama"/bintang pada primary). `_PSign` (print) = teks unit primary-first dipisah koma (badge tak cocok cetak). Excel export = 1 sel, unit primary-first dipisah koma. **Styling badge presisi = Claude's Discretion** (ikut idiom Bootstrap 5 existing).
- **D-09:** Pekerja tanpa unit (0 baris `UserUnits`, `Unit=null`) tetap tampil "Belum diisi"/"—" sesuai fallback existing tiap surface.

### Guard hapus-unit — MU-07
- **D-10:** Saat Edit/Import **menghapus** unit (atau pindah primary) yg masih dirujuk **`CoachCoacheeMapping.AssignmentUnit` aktif** → pola **konfirmasi → auto-deactivate**: tampilkan dampak (coach/mapping aktif mana yg terdampak) → bila operator setuju, deactivate mapping itu + hapus baris unit dalam **1 transaksi** (atomic + audit-log).
- **D-11:** **TAPI bila unit masih punya `ProtonTrackAssignment` aktif (PROTON tahun-berjalan) → HARD-BLOCK** (bukan auto-deactivate). PROTON tahun-berjalan terlalu penting utk dibatalkan diam-diam lewat form Worker; pesan jelas instruksikan operator tutup/bypass PROTON dulu lewat surface PROTON. Jaga Invariant #4 (`AssignmentUnit ∈ UserUnits`) tanpa abandon PROTON tak sengaja.
- **D-12:** Audit-log catat **set-diff** unit (ditambah/dihapus/primary berubah) + aksi auto-deactivate mapping bila terjadi — BUKAN sekadar `if user.Unit != model.Unit`.

### Claude's Discretion
- **Lokasi backfill:** migration `Up` (raw SQL idempotent, jalan sekali, IT-friendly) vs `SeedData` idempotent. Lean ke **migration `Up`** (backfill 1 primary-row/pekerja `Unit` non-null; pekerja `Unit` null → 0 baris). Spec §9 izinkan keduanya.
- **Mekanisme konfirmasi MU-07** (D-10): server round-trip re-prompt (POST → server deteksi referensi → balik ke form dgn flag `confirmedDeactivate` + daftar dampak → resubmit) **vs** AJAX pre-check sebelum submit. Lean ke server round-trip (cocok pola confirm existing, tak butuh endpoint baru). Planner/researcher putuskan final.
- Styling presisi badge/chip primary + cascade JS varian checkbox-list (struktur DOM, id naming) — ikut idiom existing `shared-cascade.js` + Bootstrap 5.
- Index opsional unique `(UserId, Unit)` (cegah duplikat unit/user) — rekomendasi pasang (low-cost, dedup di DB-level).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca sebelum plan/implement.**

### Spec & Requirements (AUTHORITATIVE)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — design spec. **§2** keputusan locked (D1=b primary, sekuensial, NAME-string, keep mirror); **§4** data model (`UserUnit` schema, index filtered-unique primary, mirror write-through, backfill); **§5 Fase 399** breakdown (file touchpoints: `WorkerController` Create `:269`/Edit `:405-413`/Import `:1026`/export `:186`; `AccountController`; ViewModels; views); **§7** invariant WAJIB (#1 Section scalar, #2 PROTON single-active, #3 primary mirror, #4 `AssignmentUnit ∈ UserUnits`, #5 `ProtonKompetensi.Unit` 1:1); **§9** migration & IT notes (migration flag IT=TRUE).
- `.planning/REQUIREMENTS.md` — MU-01..07 acceptance criteria (lihat juga MU-02 recompute deterministik, MU-05 validasi tiap junction-write, MU-07 guard hapus-unit).
- `.planning/ROADMAP.md` §"Phase 399" — 6 Success Criteria + migration note (TRUE only Phase 399).

### Project workflow (WAJIB ikut)
- `CLAUDE.md` — Develop Workflow (Lokal→Dev→Prod; gate `dotnet build`+`dotnet run` localhost:5277 + cek DB lokal + Playwright sebelum commit; notify IT commit-hash + migration flag) + Seed Data Workflow (snapshot/restore DB lokal utk seed temporary).
- `docs/DEV_WORKFLOW.md` — environment map + SOP migration lengkap.

### Backfill / migration data
- `Data/SeedData.cs` — pola seed idempotent existing (bila backfill via SeedData dipilih).
- `Migrations/` — pola migration existing (`AddShuffleTogglesToAssessmentSession`, `AddOrganizationLevelLabel` sbg contoh add-table + index).

</canonical_refs>

<code_context>
## Existing Code Insights (hasil scout codebase)

### Reusable Assets
- **`ApplicationDbContext.GetSectionUnitsDictAsync()`** (`Data/ApplicationDbContext.cs:118-133`) — balikin `Dictionary<Bagian, List<Unit>>` dari `OrganizationUnits` (ParentId hierarchy). Dipakai populate cascade. `GetUnitsForSectionAsync(section)` enumerate unit anak 1 Bagian → primitif validasi `Unit ∈ Bagian`.
- **`wwwroot/js/shared-cascade.js:1-49` `initSectionUnitCascade(opts)`** — cascade Section→Unit existing (client-side, JSON dict via `ViewBag.SectionUnitsJson`). Diperluas jadi varian checkbox-list (D-01).
- **Pola junction existing:** `CoachCoacheeMapping` (int Id, string UserId, string Unit-name, bool IsActive) = template bentuk `UserUnit`.
- **Validasi import existing:** `WorkerController.cs:976-983` sudah cek `Unit ∈ sectionUnitsDict[bagian]` — diperluas utk loop multi-unit split('|').
- **ClosedXML** dipakai import (`WorkerController.cs:924-948`, `row.Cell(n).GetString()`) + export (`:186`).

### Established Patterns
- `ApplicationUser.Section` (`Models/ApplicationUser.cs:28`) + `.Unit` (`:33`) = `string?` polos, no FK. **Dipertahankan** (mirror).
- ViewModels scalar: `ManageUserViewModel` (`:33-37`), `ProfileViewModel` (`:14-15`), `SettingsViewModel` (`:19-21`) — `Section`/`Unit` `string?`. → tambah `List<string> Units` + `string? PrimaryUnit` (Section tetap scalar).
- Audit existing: `WorkerController.cs:406` `if (user.Unit != model.Unit) changes.Add(...)` → ganti set-diff (D-12).
- **Authz Section AMAN, 0 perubahan** (`IsResultsAuthorized` `CMPController.cs:2503-2510` + SectionHead L4) — 100% berbasis Section scalar; Section tetap scalar → tak tersentuh (de-risk terbesar).

### Integration Points (display surface MU-03)
- Profile `Views/Account/Profile.cshtml:80,86` (+ `_PSign` `:126`); Settings `Views/Account/Settings.cshtml:99,117` (+ `_PSign` `:134`); WorkerDetail `:89-104`; ManageWorkers `:261-262`; Excel `WorkerController.cs:186-187`; `_PSign` `Views/Shared/_PSign.cshtml:40-42` (`PSignViewModel.Unit`); Home dashboard (cari render `user.Unit`).
- Import column: `Cell(5)`=Bagian, `Cell(6)`=Unit (D-04 jaga indeks ini).

</code_context>

<specifics>
## Specific Ideas

- **D-07 tradeoff sengaja diterima:** `_PSign` (kartu tanda tangan / cert-print) tampil SEMUA unit pekerja, walau cert atribusi semantik = primary unit (D1=b). Operator memilih konsistensi "tampil semua unit di setiap surface" mengalahkan default "cert-print primary-only". Downstream JANGAN balikkan ke primary-only — ini keputusan eksplisit. (Format print = primary-first koma-join, D-08.)
- **D-04 delimiter pipe `|`** dipilih (bukan koma) karena lebih aman dari nama unit yg mungkin mengandung koma; unit pertama = primary (konvensi posisional, no kolom ekstra).
- **MU-07 asimetris (D-10 vs D-11):** coach-mapping aktif = auto-deactivate-after-confirm (mulus); PROTON aktif = hard-block (lindungi PROTON tahun-berjalan). Bukan satu perilaku seragam.

</specifics>

<deferred>
## Deferred Ideas

- **MU-06 (membership listing set-aware + rollup dedup)** — Phase 400 (Wave 1).
- **Migrasi ~30+ pembaca scalar `user.Unit` ke `UserUnits`** — bertahap, selektif per-fase (spec §4.2). Phase 399 hanya pasang mirror; pembaca lama jalan via mirror.
- **CMP analytics/renewal per-unit akurat** — out-of-scope (D1=b primary; butuh kolom unit-at-issue + migration ke-2, milestone terpisah, spec §8).

### Reviewed Todos (not folded)
- **"One-time cleanup data test/audit lokal setelah Phase 367 ship"** (`2026-06-11-...md`, area: database) — matched keyword (data/audit/lokal/delete) tapi **tidak relevan** scope foundation multi-unit. Bukan bagian Phase 399. Tetap di backlog todo.

</deferred>

---

*Phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display*
*Context gathered: 2026-06-18*
