# Phase 422: SamePackage & Shuffle Integrity - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 422-samepackage-shuffle-integrity
**Areas discussed:** Toggle SamePackage, PackageNumber integrity, Shuffle+SamePackage warning, Helper/Lock scope

---

## Toggle SamePackage pasca-create (SHFX-02 / FLOW-07)

| Option | Description | Selected |
|--------|-------------|----------|
| Sync + guard no-started | ON→overwrite+lock; OFF→lepas lock, keep clone. Guard: tolak bila ada peserta sudah-mulai | ✓ |
| Sync + guard no-participants | Sama, guard ketat: tolak bila ada peserta apa pun | |
| Toggle ON one-way | Boleh ON, tak boleh OFF setelah clone | |

**User's choice:** Sync + guard no-started

---

## PackageNumber integrity (SHFX-05 / SHUF-ISS-08)

| Option | Description | Selected |
|--------|-------------|----------|
| Opsi 1: MAX+1 + ThenBy(Id) | Anti-dup nomor-tertinggi+1 + tie-breaker Id, gap dibiarkan. migration=FALSE | |
| Opsi 2: + renumber-on-delete | Opsi 1 + nomori-ulang sisa tiap delete (kontigu). migration=FALSE | |
| Opsi 3: + unique index DB | Opsi 1 + filtered unique index (AssessmentSessionId, PackageNumber). migration=TRUE | ✓ |

**User's choice:** Opsi 3 — + filtered unique index DB (migration=TRUE)
**Notes:** User minta penjelasan simpel ketiga opsi dulu (analogi paket soal + duplikat count-based) sebelum memilih → pilih jaring pengaman level-DB terkuat. Konsekuensi: butuh pra-migration dedup data existing + koordinasi IT deploy.

---

## Shuffle + SamePackage (SHFX-07 / SHUF-ISS-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Warn-only non-blocking | Peringatan UI saat Shuffle ON pada Post SamePackage, HC tetap boleh simpan | ✓ |
| Soft-block + konfirmasi | Auto-OFF + konfirmasi bila override ON | |
| Hard-block | Larang Shuffle ON saat SamePackage | |

**User's choice:** Warn-only non-blocking

---

## Helper/Lock scope (SHFX-01/03 / SHUF-ISS-03/02)

| Option | Description | Selected |
|--------|-------------|----------|
| Helper penuh + lock tolak-keras | SyncToLinkedPostIfSamePackageAsync 6 jalur + IsSessionEditLocked guard 5 endpoint tolak keras | ✓ |
| Minimal Import-only | Sync di Import saja + lock guard 5 endpoint | |

**User's choice:** Helper penuh + lock tolak-keras

## Claude's Discretion

- SHFX-04 (newPost warisi SamePackage), SHFX-06 (sibling type-aware key), nama/posisi helper, presisi teks peringatan, bentuk filtered index.

## Deferred Ideas

- FLOW-10 write-on-GET → fase 425; E-01/FORM-PP → fase 420; Scoped-Shuffle v32.6 (main) out-of-scope.
