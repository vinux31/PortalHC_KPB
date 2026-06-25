# Phase 404: Test (SQL Riil) + UAT + Docs + Invariants - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-21
**Phase:** 404-test-sql-riil-uat-docs-invariants
**Areas discussed:** Fixture SQL-riil, Cakupan UAT browser, Deliverable docs, Depth invariant assert

---

## Fixture SQL-Riil

| Option | Description | Selected |
|--------|-------------|----------|
| 1 shared MultiUnitSqlFixture | Spin SQLEXPRESS sekali via IClassFixture, seed {X,Y}+coach+PROTON, semua Fact QA share. Ikut pola AbandonGuardFixture | ✓ |
| Per-test-class fixture | DB sendiri per class, isolasi penuh, lebih lambat | |
| Reuse fixture existing | Extend ProtonCompletionFixture dll | |

**User's choice:** 1 shared MultiUnitSqlFixture
**Notes:** D-01/D-02 — dataset kanonik {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X→T2@Y.

---

## Schema Build Fixture

| Option | Description | Selected |
|--------|-------------|----------|
| db.Database.Migrate() | Jalankan migration riil incl 399 AddUserUnits + filtered-unique; bonus smoke migration deploy | ✓ |
| db.EnsureCreated() | Bangun schema dari model EF, lebih cepat, tak uji migration | |

**User's choice:** db.Database.Migrate()
**Notes:** D-03 — sekalian de-risk migration=TRUE Phase 399 di SQL-real.

---

## Cakupan UAT Browser

| Option | Description | Selected |
|--------|-------------|----------|
| Fokus PROTON sekuensial cross-unit | Live-UAT T1@X→T2@Y + cert histori per-unit + coach multi-unit view; invariant ke xUnit | ✓ |
| Full lifecycle multi-unit | UAT menyeluruh create→listing→assign→PROTON→cert; overlap UAT 399/402 | |
| xUnit-only, UAT smoke ringan | Andalkan xUnit; UAT cuma smoke 1 halaman | |

**User's choice:** Fokus PROTON sekuensial cross-unit
**Notes:** D-10/D-11/D-12 — @5270, seed temporary local-only (snapshot→restore).

---

## Deliverable Docs

| Option | Description | Selected |
|--------|-------------|----------|
| HTML handoff + md limitasi | HTML handoff IT (pola milestone-v*) migration=TRUE+commit+deploy; D1=b di md docs/ | ✓ |
| Markdown saja | Semua di markdown, lebih ringan, kurang formal | |
| Inline di milestone-summary | Tanpa doc baru, masuk /gsd-milestone-summary | |

**User's choice:** HTML handoff + md limitasi
**Notes:** D-13/D-14 — notice migration=TRUE Phase 399 + commit hash + batasan D1=b (atribusi primary unit).

---

## Cakupan Jalur-Write Invariant

| Option | Description | Selected |
|--------|-------------|----------|
| Semua jalur (roadmap) | Assign+Edit+Import+bypass-TargetUnit+Reactivate+Import-reactivate | ✓ |
| Jalur inti saja | Assign+bypass+Reactivate; Edit/Import andalkan InMemory existing | |

**User's choice:** Semua jalur (roadmap)
**Notes:** D-05/D-06/D-07 — sesuai QA-03/04, anti-celah filtered-unique.

---

## B-06 + ProtonKompetensi 1:1 di SQL-riil

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, ikut QA-04 | Anti-dobel ProtonDeliverableBootstrap lintas-unit + ProtonKompetensi.Unit 1:1 di SQL-riil | ✓ |
| Cukup InMemory existing | SQL-riil fokus single-active+AssignmentUnit saja | |

**User's choice:** Ya, ikut QA-04
**Notes:** D-08 — filtered-unique ApplicationDbContext.cs:429.

---

## Seed Data UAT

| Option | Description | Selected |
|--------|-------------|----------|
| Temporary local-only | Snapshot→insert→UAT→restore+journal cleaned (SEED_WORKFLOW) | ✓ |
| Pakai data lokal existing | Cari pekerja multi-unit existing, tanpa seed baru | |

**User's choice:** Temporary local-only
**Notes:** D-12 — DB fixture xUnit terpisah dari DB app.

---

## Disposisi Re-trigger Phase 402 (code-review + secure)

| Option | Description | Selected |
|--------|-------------|----------|
| Pre-close terpisah, sebelum 404 | Re-review+secure 402 sebagai task 402; 404 tetap murni QA | ✓ |
| Fold ke checklist 404 | Masukkan ke plan 404 sebagai langkah pre-close | |

**User's choice:** Pre-close terpisah, sebelum 404
**Notes:** Deferred CONTEXT — task 402, jangan lupa sebelum /gsd-complete-milestone v32.3.

## Claude's Discretion

- Penamaan file/kelas test, detail connection-string/teardown, struktur internal HTML handoff, pemecahan test class per-invariant.

## Deferred Ideas

- Re-trigger 402 code-review + secure (2 seam baru) — task 402 pre-close.
- Todo cleanup-367 — reviewed, not folded (legacy, tak terkait 404).
