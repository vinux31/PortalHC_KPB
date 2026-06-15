# Phase 359: Gate Berurutan + Cleanup (A) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-10
**Phase:** 359-gate-berurutan-cleanup-a
**Areas discussed:** Gate reject batch (PCOMP-06), Matikan level UI (PCOMP-10), Tahun 3 fallback (PCOMP-08), Gate antar-tahun + graduation (PCOMP-07/09)

---

## Gate reject batch (PCOMP-06)

| Option | Description | Selected |
|--------|-------------|----------|
| Skip + buat utk yg lolos | Buat session hanya utk eligible, skip tak-eligible + ringkasan | ✓ |
| Tolak seluruh batch | All-or-nothing ModelState error | |

**User's choice:** Skip + buat utk yg lolos + ringkasan "X dibuat, Y di-skip (alasan)".
**Notes:** Operasional — admin tak ulang batch. Reuse IsEligiblePerUnit.

---

## Matikan level — UI (PCOMP-10)

| Option | Description | Selected |
|--------|-------------|----------|
| Status 'Lulus/Selesai' saja | Badge tanpa angka | ✓ |
| Buang section no replacement | Hapus total | |

| Option (scope) | Description | Selected |
|--------|-------------|----------|
| Prune ViewModel + view + export | Hapus binding level menyeluruh (A-M12), DB dormant | ✓ |
| Hide-only di view | Comment-out Razor saja | |

**User's choice:** Badge "Lulus/Selesai" + prune ViewModel/view/export. DB CompetencyLevelGranted dormant.

---

## Tahun 3 fallback (PCOMP-08)

| Option | Description | Selected |
|--------|-------------|----------|
| Fallback all-eligible (transisi) | 0 deliverable → eligible (A-M7) | ✓ |
| Blok sampai deliverable diisi | Strict | |

| Option (bootstrap) | Description | Selected |
|--------|-------------|----------|
| Ya, pastikan jalan Tahun 3 | Extend AutoCreateProgressForAssignment | ✓ |
| Audit-only | Cek doang | |

**User's choice:** Fallback transisi (data-driven) + ensure bootstrap Tahun 3.

---

## Gate antar-tahun + graduation (PCOMP-07/09)

| Option | Description | Selected |
|--------|-------------|----------|
| Keduanya: assign + CreateAssessment | Hard-block 2 titik (spec 4.3 a+b) | ✓ |
| CreateAssessment saja | Longgar | |

| Option (style) | Description | Selected |
|--------|-------------|----------|
| Hard-block + pesan jelas | Tolak + pesan, bypass exempt | ✓ |
| Warning + override admin | Fleksibel, lemahkan gate | |

**User's choice:** Enforce di assign + CreateAssessment; hard-block + pesan jelas; bypass exempt (A-M4); graduation block bila Tahun 3 belum lulus.

## Claude's Discretion

- Mekanis helper gate antar-tahun (method baru vs reuse GetPassedYearsAsync).
- Bentuk pesan ringkasan skip (TempData/JSON).
- Strategi test (xUnit gate + Playwright CDP render).

## Deferred Ideas

- Bypass Tahun (Phase 360/361).
- Audit Tab1 Override (backlog 999.x).
- Drop kolom CompetencyLevelGranted (dormant by decision).
