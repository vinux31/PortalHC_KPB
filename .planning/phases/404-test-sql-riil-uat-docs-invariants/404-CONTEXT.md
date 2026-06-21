# Phase 404: Test (SQL Riil) + UAT + Docs + Invariants - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Penutup milestone v32.3 (Akun Multi-Unit). Buktikan seluruh kemampuan **multi-unit + coaching cross-unit + PROTON sekuensial** benar di **SQL riil (SQLEXPRESS)** — bukan EF-InMemory yang tak meng-enforce filtered-unique index. Assert invariant (single-active, `AssignmentUnit ∈ UserUnits`, B-06 anti-dobel, `ProtonKompetensi.Unit` 1:1), UAT lokal lulus, docs batasan D1=b. Requirements: **QA-01, QA-02, QA-03, QA-04**.

**Migration: FALSE** untuk Phase 404 (test/UAT/docs murni — tak ada schema/write produksi). Catatan: milestone keseluruhan membawa **migration=TRUE Phase 399** (`AddUserUnits` junction) saat deploy.

**Bukan scope 404** (tetap di fase masing-masing): perbaikan fitur, perubahan controller/service produksi (kecuali ekstraksi seam test-only bila benar-benar perlu — hindari), re-review/secure Phase 402 (lihat Deferred).
</domain>

<decisions>
## Implementation Decisions

### Fixture SQL-Riil (QA-01/03/04)
- **D-01:** **1 shared `MultiUnitSqlFixture`** (`IClassFixture`) — spin SQLEXPRESS sekali, seed dataset multi-unit kanonik dipakai semua Fact QA. Ikut pola fixture SQL-riil existing (`AbandonGuardFixture`, `ImageCleanupFixture`, `EssayFinalizeRecomputeFixture`, `OrgLabelMigrationFixture`) — `new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options`. Jangan reinvent harness.
- **D-02:** Seed fixture kanonik = pekerja **{X, Y} dalam 1 Bagian** + coach cross-unit (megang coachee di unit X dan Y) + PROTON **Tahun1@X → Tahun2@Y** sekuensial (cert tiap unit tersimpan utuh sebagai histori).
- **D-03:** Build schema fixture pakai **`db.Database.Migrate()`** (BUKAN `EnsureCreated`) — jalankan migration riil termasuk **399 `AddUserUnits`** + semua filtered-unique index. Bonus: buktikan migration=TRUE Phase 399 apply bersih di SQL-real (de-risk deploy milestone).
- **D-04:** Connection-string + lifecycle (create/teardown DB disposable) ikut konvensi fixture existing (`_cs`, sqlcmd `-C -I`, SQLEXPRESS). DB fixture **terpisah** dari DB app lokal — tak kena seed UAT.

### Invariant Assertion Depth (QA-03/04)
- **D-05:** Assert invariant di **SEMUA jalur-write** (per roadmap QA-03/04): **Assign + Edit + Import + bypass-TargetUnit + Reactivate + Import-reactivate**. Target constraint DB: filtered-unique `[IsActive]=1` (single-active `ProtonTrackAssignment` + `CoachCoacheeMapping`), `[IsPrimary]=1` (one-primary UserUnits) di `Data/ApplicationDbContext.cs`.
- **D-06:** Assert **single-active**: coachee multi-unit T1@X → bypass/reassign T2@Y → **tepat 1 `ProtonTrackAssignment` aktif + 1 `CoachCoacheeMapping` aktif** (filtered-unique terjaga), termasuk jalur Reactivate + Import-reactivate.
- **D-07:** Assert **`AssignmentUnit ∈ coachee.UserUnits`** di tiap junction-write.
- **D-08:** Assert **B-06 anti-dobel** ikut QA-04: `ProtonDeliverableBootstrap` lintas-unit (CoacheeId sama, deliverable unit X vs Y **tak saling skip**) + **`ProtonKompetensi.Unit` 1:1 per deliverable** (filtered-unique `ApplicationDbContext.cs:429`) — di-assert SQL-riil.
- **D-09:** Stub existing `CrossUnitAssignTests.cs:105 SingleActive_invariant_is_sql_real_phase404()` WAJIB di-implement (jangan dibiarkan body kosong). Ini titik-jangkar carry dari 402 VALIDATION (single-active dideferred ke 404 karena InMemory tak bisa enforce filtered-unique).

### UAT Browser Lokal (QA-02)
- **D-10:** UAT **fokus alur paling berisiko**: **PROTON sekuensial cross-unit T1@X → T2@Y** + cert histori per-unit utuh + coach multi-unit lihat/export coachee lintas-unit. Invariant DB diserahkan ke xUnit SQL-riil (bukan diulang manual). Cross-unit assign round-trip sudah di-UAT di Phase 402 (`7f5b6a17`) — tak diulang penuh.
- **D-11:** UAT di **`localhost:5270`** (branch ITHandoff — bukan 5277) per CLAUDE.md Develop Workflow §1. Gate: `dotnet build` 0 error + `dotnet run` + cek DB lokal + Playwright bila ada.
- **D-12:** Seed data UAT = **temporary local-only** per SEED_WORKFLOW: snapshot DB lokal → insert seed {X,Y}+coach+PROTON → UAT → **restore** + tandai journal `cleaned` (sukses ATAU gagal). Catat di `docs/SEED_JOURNAL.md`.

### Deliverable Docs (QA-02)
- **D-13:** **HTML handoff IT** ikut pola existing (`docs/milestone-v*/index.html`) — isi: notice **migration=TRUE Phase 399** (`AddUserUnits` + backfill), **commit hash**, langkah deploy Dev, daftar fase 399-404. Plus catatan **batasan D1=b** ringkas di **markdown** `docs/`.
- **D-14:** Dokumentasikan batasan **D1=b**: cert/analytics **atribusi ke primary unit** (`ApplicationUser.Unit` mirror), bukan per-unit penuh — keterbatasan sadar-desain milestone, dicatat agar IT/HC paham.

### Milestone-Close Gate (semua REQ)
- **D-15:** Definition-of-done 404: `dotnet build` 0 error + `dotnet test` hijau (suite multi-unit SQL-riil **baru** + suite existing **tak regresi**, baseline ~547/0/6) + UAT browser sign-off → milestone siap **1 push** → notify IT re-deploy Dev (migration=TRUE Phase 399 + commit hash).

### Claude's Discretion
- Penamaan file/kelas test (selama ikut konvensi `*Tests.cs` + `*Fixture`).
- Detail teknis connection-string, teardown DB disposable, helper seed fixture.
- Struktur internal HTML handoff (selama memuat info D-13).
- Apakah pisah jadi beberapa test class per-invariant atau 1 class dengan banyak `[Fact]` (selama share `MultiUnitSqlFixture` per D-01).

### Folded Todos
*(none — todo cleanup-367 di-review tapi tak di-fold; lihat Deferred)*
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Roadmap (kontrak invariant + scope)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — spec induk milestone v32.3: invariant #1-#4, kontrak `AssignmentUnit ∈ UserUnits`, single-active, B-06, batasan D1=b.
- `.planning/ROADMAP.md` §Phase 404 — definisi QA-01..04 + 5 success criteria + Coverage Validation (24/24 REQ mapping).
- `.planning/REQUIREMENTS.md` — REQ QA-01/02/03/04 acceptance.

### Carry-in dari Phase 402 (titik jangkar 404)
- `.planning/phases/402-coaching-cross-unit-mapping/402-VALIDATION.md` — disposisi Option 2 (passed 2026-06-21); single-active SQL-real **dideferred ke 404**; stub `SingleActive_invariant_is_sql_real_phase404` `[Skip]`; **flag re-trigger code-review+secure 402** (2 seam baru).

### Constraint DB (yang dibuktikan SQL-riil)
- `Data/ApplicationDbContext.cs` — filtered-unique index: `[IsActive]=1` single-active (~L334-335), `[IsPrimary]=1` one-primary UserUnits (~L351-352, L357), `ProtonKompetensi` 1:1 (~L429), dobel-pending (~L455-456). InMemory tak meng-enforce ini → alasan 404 ada.

### Pola test SQL-riil (precedent — IKUTI)
- `HcPortal.Tests/AbandonGuardTests.cs` — `IClassFixture<AbandonGuardFixture>` + `UseSqlServer(_cs)` (pola fixture SQLEXPRESS disposable).
- `HcPortal.Tests/ImageCleanupIntegrationTests.cs` — `ImageCleanupFixture`, isolasi DB disposable.
- `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` , `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` — `Migrate()`-style integration fixture.
- `HcPortal.Tests/CrossUnitAssignTests.cs` (L105 stub) — titik implement QA-03; existing InMemory CXU tests sebagai referensi assert.

### Unit-test multi-unit existing (referensi assert, jangan duplikat semantik)
- `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs`, `ReactivateUnitValidationTests.cs`, `CleanupNoClobberTests.cs`, `ProtonUnitResolveTests.cs`, `RemoveUnitGuardTests.cs`, `UserUnitsWriteThroughTests.cs`, `PrimaryMirrorTests.cs`, `ImportMultiUnitParseTests.cs`.

### Workflow proyek (WAJIB patuh)
- `docs/SEED_WORKFLOW.md` — klasifikasi seed + SOP snapshot/restore SQL Server (D-12).
- `docs/DEV_WORKFLOW.md` §1 — port 5270 branch ITHandoff, gate verifikasi lokal, SOP migration + notify IT.
- `docs/SEED_JOURNAL.md` — catat seed temporary UAT + tandai cleaned.
- `CLAUDE.md` — Develop Workflow + Seed Data Workflow ringkas.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola fixture SQL-riil**: `IClassFixture<XFixture>` + `new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options` sudah dipakai ~6 test class. `MultiUnitSqlFixture` tinggal ikut bentuk ini (D-01).
- **Stub jangkar**: `CrossUnitAssignTests.cs:105` `SingleActive_invariant_is_sql_real_phase404()` — placeholder siap diisi (D-09).
- **Filtered-unique index** sudah live di `ApplicationDbContext.cs` — 404 hanya membuktikan, tak menambah constraint.
- **HTML handoff pattern**: `docs/milestone-v*/index.html` (precedent v31.0 dll) buat handoff IT (D-13).

### Established Patterns
- Test project `HcPortal.Tests` punya 2 mode: InMemory (mayoritas) + SqlServer (fixture integration). 404 = mode SqlServer.
- Seed temporary lokal: snapshot → insert → restore (SEED_WORKFLOW), DB app terpisah dari DB fixture xUnit.
- Migration→notify IT: SOP DEV_WORKFLOW (developer tak edit Dev/Prod; IT deploy).

### Integration Points
- `MultiUnitSqlFixture` → `ApplicationDbContext` via `Migrate()` (jalankan migration 399 AddUserUnits di SQL-riil).
- xUnit Facts memanggil seam/controller existing (CoachMapping/CDP/ProtonBypass) yang invariant-nya dibuktikan — tanpa ubah produksi.
- Suite 404 masuk `dotnet test` keseluruhan; jangan regresi baseline ~547/0/6.
</code_context>

<specifics>
## Specific Ideas

- Dataset kanonik fixture HARUS persis skenario roadmap: {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X→T2@Y. Ini satu sumber kebenaran semua Fact QA.
- `Migrate()` dipilih sengaja agar 404 sekaligus jadi **smoke-test migration 399** di SQL-real sebelum IT deploy (migration=TRUE).
- UAT sempit-tapi-dalam: 1 alur PROTON sekuensial cross-unit end-to-end lebih bernilai dari banyak smoke dangkal.
</specifics>

<deferred>
## Deferred Ideas

- **Re-trigger Phase 402 code-review + secure** — 402 VALIDATION (Option 2) mengekstraksi 2 seam produksi baru (`CDPController.CoerceCoachUnitScope` + `CoachMappingController.FilterEligibleCoachees`). Policy minta re-run `/gsd-code-review 402` + `/gsd-secure-phase 402` (seam pure, behavior-identik → fast confirm). **Disposisi: task Phase 402 terpisah, dikerjakan sebelum/paralel 404 (pre-milestone-close), BUKAN scope 404.** Jangan lupa sebelum `/gsd-complete-milestone v32.3`.

### Reviewed Todos (not folded)
- **"One-time cleanup data test/audit lokal setelah Phase 367 ship"** (`.planning/todos/pending/2026-06-11-...`, area database, score 0.6) — di-review tapi **tak di-fold**: legacy cleanup era Phase 367 (cascade delete records), tak terkait seed UAT 404 (yang dikelola sendiri via snapshot→restore D-12). Tetap di backlog todo.
</deferred>

---

*Phase: 404-test-sql-riil-uat-docs-invariants*
*Context gathered: 2026-06-21*
