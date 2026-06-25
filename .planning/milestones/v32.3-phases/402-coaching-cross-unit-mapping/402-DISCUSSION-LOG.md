# Phase 402: Coaching Cross-Unit Mapping - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-19
**Phase:** 402-coaching-cross-unit-mapping
**Areas discussed:** UI picker unit per-coachee, Default unit coachee multi-unit, Alur modal + scoping Bagian, View self-scope coach multi-unit

---

## UI picker unit per-coachee (CXU-03/04)

| Option | Description | Selected |
|--------|-------------|----------|
| Inline per-baris (kondisional) | Dropdown unit muncul hanya bila coachee >1 unit; single-unit auto-pakai unitnya | ✓ |
| Inline per-baris (selalu) | Tiap coachee tercentang tampilkan dropdown walau 1 opsi | |
| Panel langkah-2 | Centang coachee dulu → panel ke-2 daftar coachee + dropdown unit masing-masing | |

**User's choice:** Inline per-baris (kondisional)
**Notes:** Minim UI churn; mayoritas coachee single-unit. Relax JS lock single-unit-per-batch (`:717-784`) ke level Bagian.

---

## Default unit coachee multi-unit (CXU-03)

| Option | Description | Selected |
|--------|-------------|----------|
| Default primary, bisa diubah | Pre-select unit primary (D1=b), operator bisa ganti | ✓ |
| Paksa pilih eksplisit | Tak ada default; wajib pilih unit tiap coachee multi-unit | |

**User's choice:** Default primary, bisa diubah
**Notes:** Konsisten D1=b; validasi tiap pilihan ∈ coachee.UserUnits via helper `ValidateAssignmentUnitInUserUnits` (401-01).

---

## Alur modal + scoping Bagian (CXU-01/02)

| Option | Description | Selected |
|--------|-------------|----------|
| Coach-first auto-scope + lock Section | Pilih coach → coachee auto-filter ke coach.Section + AssignmentSection auto-lock + guard server | ✓ |
| Filter manual + guard server | Pertahankan dropdown Bagian manual; server enforce coachee ⊆ coach.Section | |

**User's choice:** Coach-first auto-scope + lock Section
**Notes:** Paling cegah-error. Eligible loader saat ini global (`:172-175`) → scoped ke coach.Section. Server guard CXU-02 WAJIB backstop apa pun mekanisme client.

---

## View self-scope coach multi-unit (CXU-05)

| Option | Description | Selected |
|--------|-------------|----------|
| Union semua unit (default) | Default tampil semua coachee gabungan unit coach; filter per-unit tetap jalan | ✓ |
| Default primary + switcher | Default unit primary; dropdown switch antar unit coach | |

**User's choice:** Union semua unit (default)
**Notes:** Self-scope `unit=user.Unit` (CDP `:305/:326/:647`) → `IN(coach.UserUnits)`. Post-filter `:490-503` sudah AssignmentUnit-aware (401) — tak diubah. Catatan: "636" di spec = statusData, bukan self-scope; site riil = `:647`.

## Claude's Discretion

- Bentuk expose `coachee.UserUnits` ke client (data-attr JSON vs server-render).
- Shape payload map `coacheeId→unit` (Dictionary vs List) di `CoachAssignRequest:1863`.
- Mekanisme eligible-loader set-aware (client-filter + server-enforce vs AJAX per-coach).
- Mekanisme union-expand self-scope CDP (handling unit kosong coaching-role).
- Teks hint pengganti lock lama (`:463-465`).

## Deferred Ideas

- Kolom Unit Excel import coach-coachee (defer 401 D-04; import tetap single-unit primary-default).
- Kolom Unit `ProtonTrackAssignment` + PROTON paralel (spec §8, migration).
- Multi-Bagian per akun / mutasi cross-Bagian (Invariant #1).
- Test SQL-riil cross-unit + UAT + docs D1=b → Phase 404.
- Todo cleanup-data-test-lokal (score 0.2) — out of scope.
