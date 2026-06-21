# Phase 401: PROTON Unit-Resolution Hardening - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 401-proton-unit-resolution-hardening
**Areas discussed:** Visibilitas skip PSU-05, Channel audit-warn, Sumber unit Import mapping, Reactivation unit-match PSU-07c

---

## Visibilitas skip PSU-05 (coachee ber-AssignmentUnit kosong)

| Option | Description | Selected |
|--------|-------------|----------|
| Indikator UI on-demand | Alert/badge di CoachCoacheeMapping (query on-demand mapping aktif AssignmentUnit kosong/∉UserUnits), reuse pola CleanupReport. Operator lihat & perbaiki tanpa baca log. | ✓ |
| Skip senyap + log saja | Skip + audit-warn ke log; tak ada indikator UI. Operator cek log manual. | |
| You decide | Claude tentukan saat planning. | |

**User's choice:** Indikator UI on-demand
**Notes:** → D-01. Skip dua tingkat (D-02): read-path = exclude, gate-eligibility-exam = BLOCK penerbitan session/cert.

---

## Channel audit-warn PSU-05

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid by-path | Read-path skip → ILogger.LogWarning saja (hindari banjir AuditLog tiap page-load). Gate-eligibility-exam BLOCK → AuditLog persisted + ILogger. | ✓ |
| ILogger semua | Semua skip → app-log saja; tak ada jejak DB queryable utk gate-block. | |
| AuditLog persisted semua | ⚠️ read-path sering dipanggil → tabel AuditLog banjir. Tidak disarankan. | |

**User's choice:** Hybrid by-path
**Notes:** → D-03. Sadar-volume: read-path murah (ILogger), gate-block langka & penting (AuditLog persisted, queryable compliance).

---

## Sumber unit Import mapping (PSU-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Default primary + preserve reactivate | Baris BARU → primary tervalidasi ∈UserUnits; reactivate → preserve existing (no-clobber). Tanpa ubah template. Non-primary picking → 402 UI. | ✓ (via "You decide") |
| Tambah kolom Unit di template | Kolom Unit opsional di Excel; kosong→primary, diisi→validasi ∈UserUnits. Ubah template + parser. | |
| You decide | Claude tentukan saat planning. | ✓ |

**User's choice:** You decide → diresolusi ke "Default primary + preserve reactivate (no template change)"
**Notes:** → D-04. Rasional: primary-default baris baru SELALU sah (invariant #3); no-clobber riil = preserve-on-reactivate; non-primary per-coachee = jalur 402 (CXU-03); backward-compat penuh, scope 401 minimal.

---

## Reactivation unit-match PSU-07c (reconcile PTA-no-Unit + AF-4)

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse korelasi + validasi unit | Pertahankan korelasi DeactivatedAt ±5s existing (respect AF-4) + tambah validasi AssignmentUnit ∈ coachee.UserUnits aktif + preserve AssignmentUnit. True per-unit match = out-of-scope (butuh migration). | ✓ |
| Re-architect unit-match PTA | ⚠️ Butuh kolom Unit di ProtonTrackAssignment = migration + langgar 0-migration & PROTON-paralel out-of-scope. Tolak. | |
| You decide | Claude tentukan saat planning. | |

**User's choice:** Reuse korelasi + validasi unit
**Notes:** → D-05. PTA tak punya kolom Unit (spec §3) + AF-4 "JANGAN ubah window logic" (`:1043-1051`). Validasi unit ditambah di sisi mapping, bukan PTA.

## Claude's Discretion

- Granularity kegagalan validasi PSU-03 (per-row reject + report utk Import; reject coachee bermasalah utk Assign) — ikut pola existing.
- Bentuk/lokasi indikator UI D-01 (badge vs alert daftar) — idiom Bootstrap 5 + pola CleanupReport.
- Filter-axis swap PSU-02 (mekanis, pertahankan semantik).
- Validasi bypass TargetUnit ∈ worker.UserUnits + org (`ProtonDataController.cs:1638`).

## Deferred Ideas

- Kolom Unit di Excel import coach-coachee → 402 (CXU-03).
- Kolom Unit di ProtonTrackAssignment / PROTON paralel / true per-unit PTA-match → out-of-scope milestone (spec §8).
- AF-4 proper fix (DeactivatedByMappingEventId) → backlog (butuh migration).
