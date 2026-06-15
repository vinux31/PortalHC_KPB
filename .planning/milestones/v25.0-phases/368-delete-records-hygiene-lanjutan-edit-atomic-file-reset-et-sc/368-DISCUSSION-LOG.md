# Phase 368: Delete Records Hygiene Lanjutan - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-13
**Phase:** 368-delete-records-hygiene-lanjutan-edit-atomic-file-reset-et-sc
**Areas discussed:** #23 AttemptHistory cleanup, #25 Cert dedup, #26 EditTraining renewal validation, #21/#22 Edit-atomic + Reset ET

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| #23 Mekanisme cleanup AttemptHistory orphan | One-time cleanup mechanism (endpoint/script/migration) | ✓ |
| #25 Pendekatan dedup CertificationManagement | Shared helper vs inline GroupBy | ✓ |
| #26 Ketat-nya validasi renewal EditTraining | Hard-block vs validate-on-change | ✓ |
| #21/#22 Scope edit-atomic + reset ET | Confirm file fields + ET cleanup scope | ✓ |

**User's choice:** Semua 4 area.

---

## #23 — AttemptHistory orphan cleanup

### Q1: Mekanisme delivery + trigger
| Option | Description | Selected |
|--------|-------------|----------|
| Endpoint admin idempotent + preview | GET preview count → POST execute + audit; no direct DB edit; migration=false utuh | ✓ |
| SQL script serah ke IT | Standalone DELETE, IT jalankan manual | |
| EF data migration | Raw SQL DELETE saat startup | |

### Q2: Definisi orphan
| Option | Description | Selected |
|--------|-------------|----------|
| AttemptHistory tanpa AssessmentSession induk (FK dangling) | Definisi sempit + aman | ✓ |
| Tentukan saat planning (riset skema dulu) | Serahkan ke planner | |

**Notes:** Endpoint dipilih karena selaras Dev Workflow (no edit DB Dev/Prod langsung, IT promosi via UI) + pertahankan migration=false yang di-declare fase 368.

---

## #25 — CertificationManagement dedup

**Temuan scout:** CMPController/CDPController = plain `Controller`, TIDAK inherit AdminBaseController (hanya Training/AssessmentAdmin inherit). "ala AdminBase" = samakan POLA, bukan literal inherit.

| Option | Description | Selected |
|--------|-------------|----------|
| Helper static shared netral, CMP+CDP panggil sama | Single-source anti-drift; lokasi netral (bukan AdminBase) | ✓ |
| CDP delegasi ke CMP | CDP panggil helper CMP existing; coupling lintas-controller | |
| Inline GroupBy copy per controller | 2 copy, risiko drift | |

**User's choice:** Helper static shared netral.

---

## #26 — EditTraining renewal validation

### Q1: Kapan validasi dijalankan
| Option | Description | Selected |
|--------|-------------|----------|
| Hanya saat field renewal berubah (toleran legacy) | Cegah link buruk baru, tak rusak edit data lama | ✓ |
| Selalu validasi tiap save (ketat) | Jamin integritas tapi bisa blokir edit legacy invalid | |

### Q2: Aksi saat invalid
| Option | Description | Selected |
|--------|-------------|----------|
| ModelState error — tolak save, pesan jelas | Block + pesan jelas, sesuai spec | ✓ |
| Auto-null + warning | Permisif tapi diam-diam putus link (lawan honesty 367) | |

**User's choice:** Validate-on-change + ModelState block.

---

## #21/#22 — Edit-atomic + Reset ET

### Q1: #21 Atomic file scope + perilaku
| Option | Description | Selected |
|--------|-------------|----------|
| Keduanya; hapus-lama HANYA jika file baru di-upload | EditTraining + EditManualAssessment, pola 331 | ✓ |
| EditTraining saja | Hanya 1 endpoint | |

### Q2: #22 Reset ET scope
| Option | Description | Selected |
|--------|-------------|----------|
| Tambah RemoveRange SessionElemenTeknisScores ke reset existing | Tepat spec #22, no scope creep | ✓ |
| Reset ET + audit analytics stale lain saat planning | Lebih luas, risiko creep | |

**User's choice:** Keduanya (file conditional) + RemoveRange ET ke reset existing.

---

## Claude's Discretion
- Lokasi tepat helper static shared #25 (static util vs promote CMP helper).
- Wording pesan ModelState #26 (jelas + tak leak, pola V7 generik).
- Bentuk endpoint admin #23 (route/view/partial preview) — kontrak: preview-count + idempotent + audit.

## Deferred Ideas
- Impersonate identity → backlog 999.6.
- Soft-delete/undo → opsi C ditolak.
- #22 perluasan analytics stale lain → tidak diambil (strictly ET scores).
- #27 residu identitas sesi backfill → accepted by design.
