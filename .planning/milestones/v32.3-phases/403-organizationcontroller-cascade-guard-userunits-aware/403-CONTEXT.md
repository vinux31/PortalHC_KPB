# Phase 403: OrganizationController Cascade/Guard UserUnits-Aware - Context

**Gathered:** 2026-06-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Buat operasi struktur organisasi di `OrganizationController` **sadar junction `UserUnits`** (bukan cuma scalar `Users.Unit`). Wave 1 — PARALEL dgn Phase 400 & 401 (file `OrganizationController` terisolasi, cluster disjoint). Yang di-deliver:

1. **Rename cascade (ORG-01):** rename unit (`EditOrganizationUnit` POST) meng-update **semua baris `UserUnits.Unit`** (termasuk membership **sekunder**, bukan cuma pemegang primary via scalar) + jaga primary-mirror `ApplicationUser.Unit` konsisten.
2. **Delete/deactivate guard (ORG-01):** `DeleteOrganizationUnit` (:447) **dan** `ToggleOrganizationUnitActive` (:391) scan `UserUnits` (membership sekunder) — unit yg masih dipakai sbg membership tak bisa dihapus/dinonaktifkan.
3. **Reparent cross-Bagian hard-BLOCK (ORG-02):** reparent unit ke Bagian lain di-blok **bila ada pekerja yg `UserUnits`-nya akan terpecah ke >1 Bagian** (jaga Invariant #1 "1 Bagian/akun").
4. **`PreviewEditCascade` (ORG-02):** hitung baris `UserUnits` terdampak → preview == actual.

**Requirements:** ORG-01, ORG-02.

**OUT of scope (fase lain / milestone lain):** foundation junction + write-through (399, DONE); listing set-aware (400); PROTON unit-resolution (401); coaching cross-unit (402); test SQL-riil + UAT (404). migration=false (cascade/guard pada data existing; tak ada schema/migration baru). File LAIN selain `OrganizationController` JANGAN disentuh (jaga isolasi Wave-1 paralel). Multi-Bagian/mutasi-Bagian sbg fitur = out-of-scope milestone (spec §8).

</domain>

<decisions>
## Implementation Decisions

### Granularitas blok reparent lintas-Bagian — ORG-02
- **D-01:** **Blok HANYA saat split.** Reparent unit ke Bagian lain tetap **diizinkan** + auto-update `Section` utk pekerja **single-unit** (pertahankan perilaku cascade existing `EditOrganizationUnit` L235-265). **HARD-BLOCK** hanya bila ada pekerja yg, selain unit yg di-reparent, **juga punya `UserUnits` aktif lain di Bagian berbeda** dari Bagian tujuan → membership-nya akan terpecah >1 Bagian (langgar Invariant #1). Sesuai spec literal ("blok bila `UserUnits` terpecah >1 Bagian"), minimal behavior change.
- **D-01a:** Deteksi split berbasis **`UserUnits` aktif** (`IsActive=true`) saja — membership ter-deactivate tak dihitung. Pesan blok sebut pekerja mana yg akan terpecah (NIP/nama + unit lintas-Bagian-nya) supaya operator bisa selesaikan manual dulu.
- **D-01b:** Reparent **tidak** mengubah nama unit → baris `UserUnits.Unit` tak berubah isinya saat reparent; satu-satunya efek data pada pekerja single-unit = `Section` mirror berubah (tak ada kolom Section di `UserUnits`, jadi tak ada update baris junction saat reparent). Tambahan 403 utk reparent = **logika deteksi-split + blok** (cascade Section existing dipertahankan).

### Cakupan + pesan delete/deactivate guard — ORG-01
- **D-02:** **Delete + Deactivate, pesan spesifik.** Kedua guard di-UserUnits-aware: `DeleteOrganizationUnit` (:447, saat ini `Users.AnyAsync(u => u.Section==name || u.Unit==name)`) **dan** `ToggleOrganizationUnitActive` deactivate-branch (:391, saat ini `Users.AnyAsync(u => u.Unit==name)`). Tambah scan `UserUnits` aktif (membership **sekunder**) — unit yg jadi membership sekunder pekerja **tak bisa** dihapus/dinonaktifkan.
- **D-02a:** Pesan blok **spesifik** (sebut alasan: pekerja masih anggota — eksplisit bila karena membership **sekunder**, krn sekunder TAK terlihat di scalar `Users.Unit` → operator butuh tahu kenapa terblok). Boleh tetap pakai gaya pesan existing tapi diperjelas konteks sekunder.
- **D-02b:** Guard berbasis `UserUnits` **aktif** (`IsActive=true`). Pertahankan guard existing lain (children aktif, KKJ/CPDP files, ProtonKompetensi/CoachingGuidance) tanpa perubahan.

### Tampilan PreviewEditCascade untuk baris UserUnits — ORG-02
- **D-03:** **Line terpisah.** Tambah field `affectedUserUnitsCount` di payload JSON `PreviewEditCascade` (:283-334) + tampil baris sendiri di modal preview (mis. "X baris keanggotaan unit"). Konsep beda dari scalar user-count (mirror) → jangan digabung; transparan & **preview == actual**.
- **D-03a:** Hitungan UserUnits di preview WAJIB persis sama dgn yg di-update `EditOrganizationUnit` aktual (rename: count `UserUnits.Unit==oldName`; bila reparent juga ubah keanggotaan, count sesuai). Hitung hanya saat `nameChanged` mengubah baris `UserUnits` (rename Level>=1). Reparent (tanpa rename) tak mengubah baris `UserUnits` → 0 (D-01b).

### Atomicity rename/reparent cascade + sinkron primary-mirror — ORG-01
- **D-04:** **Transaksi + recompute inline.** Wrap seluruh cascade `EditOrganizationUnit` (rename scalar existing + rename baris `UserUnits` + reparent Section + deteksi-split) dalam `BeginTransactionAsync` (pola atomic 399 Edit) — semua sukses atau rollback bareng, tak ada window primary-mirror desync.
- **D-04a:** **Recompute mirror inline** di `OrganizationController` (JANGAN reuse helper di `WorkerController` — jaga file terisolasi Wave-1, hindari compile-dependency lintas-controller). Catatan: mirror utk pemegang **primary** sudah ter-update oleh baris existing L219 (`Users.Unit = name.Trim()` saat Unit scalar==oldName); 403 menambah **rename baris `UserUnits.Unit==oldName` (semua, termasuk sekunder)** + verifikasi mirror tetap konsisten (Invariant #3). Rename TAK mengubah `IsPrimary` flag → tak perlu promote/recompute primary, cukup rename string + jaga mirror==baris-primary.

### Claude's Discretion
- Bentuk persis query deteksi-split (map unit-lain pekerja → Bagian via `OrganizationUnits`/`GetSectionUnitsDictAsync`) — selama berbasis `UserUnits` aktif + bandingkan Bagian tujuan (root ancestor L237-247) vs Bagian unit-lain.
- Wording final pesan blok (D-01a, D-02a) — ikut idiom pesan Indonesia existing di controller.
- Markup/teks baris baru preview UserUnits di view (D-03) — ikut idiom modal preview existing (ORG-TREE-07).
- Apakah scan guard pakai `_context.UserUnits.AnyAsync(...)` correlated atau join — bebas, asal benar (PITFALL: gunakan `_context.UserUnits`, BUKAN nav-prop `u.UserUnits` bila tak dideklarasi — lihat lesson 400-01).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner) WAJIB baca sebelum plan/implement.**

### Spec & Requirements (AUTHORITATIVE)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — **§5 Fase 403** (touchpoints: rename `:218`/reparent `:251`, delete-guard `:391,447`, reparent cross-Bagian blok); **§6** dependency (Wave-1 paralel {400,401,403}, file `OrganizationController` terisolasi); **§7** invariant (#1 Section scalar 1 Bagian/akun ← inti ORG-02; #3 primary mirror write-through ← D-04); **§10** risiko (b) reparent lintas-Bagian = split-brain Section → hard-block.
- `.planning/REQUIREMENTS.md` — **ORG-01** (rename/reparent cascade `UserUnits.Unit` + recompute mirror; delete-guard scan `UserUnits` sekunder) + **ORG-02** (reparent cross-Bagian hard-BLOCK + `PreviewEditCascade` hitung baris `UserUnits`, preview==actual).
- `.planning/ROADMAP.md` §"Phase 403" — 5 Success Criteria + migration note (false).

### Fondasi multi-unit (dari fase sebelumnya)
- `.planning/phases/399-foundation-junction-userunits-primary-mirror-multi-select-ui-display/399-CONTEXT.md` — kontrak primary-mirror (D-01..12), `UserUnit` schema, write-through pola; **399 COMPLETE**.
- STATE.md "Accumulated Context" → entri `[399-01..04]` + `[400-01]` (lesson `_context.UserUnits` correlated, BUKAN nav-prop; backfill lengkap; mirror `Users.Unit` dipertahankan).

### Project workflow (WAJIB ikut)
- `CLAUDE.md` — Develop Workflow (gate `dotnet build`+`dotnet run` localhost:5277 + cek DB lokal + Playwright bila ada UI sebelum commit; ❌ no edit Dev/Prod; notify IT migration flag — **403 = FALSE**).
- `docs/DEV_WORKFLOW.md` — environment map + SOP.

</canonical_refs>

<code_context>
## Existing Code Insights (hasil scout `OrganizationController.cs`)

### File & method touchpoints (semua di `Controllers/OrganizationController.cs`)
- **`EditOrganizationUnit` POST** (~L130-277): dup-check (L150), circular guard `IsDescendantAsync` (L162-177), set ParentId/Level + `UpdateChildrenLevelsAsync` (L180-191). **Rename cascade** L198-232: Level==0 → `Users.Section`/`AssignmentSection`/`Bagian` (Kompetensi+Guidance); **Level>=1 → `Users.Unit` (L218-219)/`AssignmentUnit`/`ProtonKompetensi.Unit`/`CoachingGuidanceFiles.Unit`**. **Reparent cascade** L235-265: hitung root-ancestor Bagian baru (L237-247) → set `Users.Section`=newSectionName utk `Users.Unit==oldName` (L251-252) + `AssignmentSection`/`Bagian`. SaveChanges L268. **→ 403:** tambah rename baris `UserUnits.Unit==oldName` (Level>=1) + deteksi-split blok reparent (D-01) + wrap transaksi (D-04).
- **`PreviewEditCascade` POST** (L283-334): server-authoritative nameChanged/parentChanged; count `affectedUsers/Mappings/Kompetensi/Guidance` (nameChanged Level-aware + reparent Level>=1). **→ 403:** tambah `affectedUserUnitsCount` (D-03).
- **`DeleteOrganizationUnit` POST** (L416-473+): guard children aktif (L431), KKJ/CPDP files (L439), **`hasUsers = Users.AnyAsync(u => u.Section==name || u.Unit==name)` (L447)**, ProtonData (L456). **→ 403:** scan `UserUnits` aktif (D-02).
- **`ToggleOrganizationUnitActive` POST** (L364-410): deactivate-branch guard children-aktif (L377) + **`hasActiveUsers = Users.AnyAsync(u => u.Unit==name)` (L391, Level>=1)**. **→ 403:** scan `UserUnits` aktif (D-02).
- `IsAjaxRequest()` helper — dua jalur respons (AJAX `Json` + non-AJAX `TempData`+redirect); pesan guard ikut pola ini.

### Reusable assets
- **`_context.UserUnits`** (DbSet, ada sejak 399) — kolom `UserId`, `Unit` (NAME-string), `IsPrimary`, `IsActive`. Scan via `_context.UserUnits.AnyAsync(uu => uu.Unit==oldName && uu.IsActive)` / batch ke list utk rename. **Mirror** `ApplicationUser.Unit` = baris `IsPrimary`.
- **`ApplicationDbContext.GetSectionUnitsDictAsync()` / `GetUnitsForSectionAsync(section)`** — map Bagian→unit-anak (dipakai deteksi-split: tentukan Bagian dari unit-lain pekerja).
- Root-ancestor walk (L237-247) sudah ada — reuse utk tentukan "Bagian tujuan" reparent.

### Established patterns
- Authz: `[Authorize(Roles="Admin, HC")]` + `[HttpPost]` + `[ValidateAntiForgeryToken]` di semua mutasi (pertahankan).
- `Users.Unit`/`Section` scalar = `string?` polos no-FK, **dipertahankan** sbg mirror.
- Transaksi atomic pola 399 Edit: `using var tx = await _context.Database.BeginTransactionAsync(); ... await tx.CommitAsync();`.

### Integration points
- View `Views/Admin/ManageOrganization.cshtml` (modal Edit + preview cascade ORG-TREE-07) — tambah baris tampil `affectedUserUnitsCount` (D-03). Surface UI (UI hint = yes).

</code_context>

<specifics>
## Specific Ideas

- **D-01 = pilih spec literal, bukan over-block:** reparent single-unit worker tetap auto-mutasi Section (perilaku existing), hard-block HANYA pada split nyata >1 Bagian. Downstream JANGAN naikkan jadi "blok semua reparent ber-anggota" (ditolak sbg opsi).
- **Isolasi Wave-1 (D-04a):** recompute mirror **inline** di `OrganizationController`; JANGAN import/panggil helper `WorkerController` (399) — 400/401/403 jalan paralel di worktree terpisah, hindari kopling lintas-file yg bikin merge-conflict / compile-dependency.
- **Reparent tak ubah baris `UserUnits` (D-01b):** rename = ubah string baris; reparent = ubah Section mirror saja (UserUnits tak punya Section). Ini bikin scope reparent ringan: cuma deteksi-split + blok.

</specifics>

<deferred>
## Deferred Ideas

- **Mutasi-Bagian sbg fitur first-class** (pindahkan pekerja split antar-Bagian otomatis saat reparent) — out-of-scope milestone (spec §8 "pindah Bagian = mutasi, proses terpisah"). 403 cukup hard-block + arahkan operator.
- **Test invariant SQL-riil reparent/delete/rename multi-unit** — Phase 404 (QA-01..04), WAJIB SQLEXPRESS (EF-InMemory tak enforce filtered-unique). 403 cukup gate lokal `dotnet build`+`dotnet run`+DB lokal+Playwright (CLAUDE.md).
- **CMP analytics/renewal per-unit** — out-of-scope (D1=b primary).

### Reviewed Todos (not folded)
- Tak ada todo pending yg match scope OrganizationController/cascade (cross-reference todo kosong utk phase ini).

</deferred>

---

*Phase: 403-organizationcontroller-cascade-guard-userunits-aware*
*Context gathered: 2026-06-18*
