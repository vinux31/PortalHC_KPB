# Phase 405: Backend Core - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-21
**Phase:** 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
**Areas discussed:** Hitungan attempt legacy, Eligibility retroaktif, Paket migration, Retensi arsip snapshot

---

## Hitungan attempt legacy

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya era-retake (abaikan legacy) | Arsip HC-reset lama tak pre-consume cap; aman saat launch; mekanisme = planner | ✓ |
| Hitung semua arsip historis (strict) | Semua arsip lama ikut hitung; pekerja pernah reset bisa langsung di cap; HC override katup | |

**User's choice:** Hanya era-retake (abaikan legacy)
**Notes:** Deviasi dari spec (yang count `archivedCount(UserId,Title,Category)` polos). Planner WAJIB implement diskriminator era-retake (rekomendasi: snapshot-presence di `AssessmentAttemptResponseArchive`, atau date-cutoff).

---

## Eligibility retroaktif

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, retroaktif | Nyalakan AllowRetake → kegagalan existing batch jadi eligible (tunduk cooldown+cap) | ✓ |
| Hanya kegagalan setelah ON | Butuh bandingkan enabled-at vs CompletedAt; lebih ketat | |

**User's choice:** Ya, retroaktif
**Notes:** CanRetake cukup cek flag AllowRetake current + cooldown dari CompletedAt. Konsisten desain sibling-propagation.

---

## Paket migration

| Option | Description | Selected |
|--------|-------------|----------|
| Satu migration gabung | `AddRetakeColumnsAndArchive` = 3 kolom + tabel atomik; 1 notify-IT; pola 399 | ✓ |
| Dua migration terpisah | Kolom + tabel terpisah; rollback granular; 2 notify-IT | |

**User's choice:** Satu migration gabung
**Notes:** —

---

## Retensi arsip snapshot

| Option | Description | Selected |
|--------|-------------|----------|
| Simpan selamanya (cascade only) | Retain-all audit/ISO 17024 sesuai D10; hapus hanya via FK cascade | ✓ |
| Pruning cap N attempt | Hemat ruang tapi hilang histori; bertentangan D10 | |

**User's choice:** Simpan selamanya (cascade only)
**Notes:** —

## Claude's Discretion

- Mekanisme konkret D-01 (snapshot-presence vs date-cutoff)
- `IRetakeService` DI lifetime + namespace + struktur file test
- Status transient saat claim-transisi (`Completed→"Open"` per spec)

## Deferred Ideas

None — diskusi dalam scope Phase 405. UI = 406/407, test menyeluruh = 408.
