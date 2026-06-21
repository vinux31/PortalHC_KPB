# Phase 409: Data Foundation + Re-entry Guards + Exclude-Removed Query - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-21
**Phase:** 409-data-foundation-re-entry-guards-exclude-removed-query
**Areas discussed:** Cakupan exclude-removed, UX peserta-dihapus (server guard), Detail kolom removal

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Cakupan exclude-removed | Peserta bersertifikat soft-removed: hilang dari semua hitungan vs hanya hitungan aktif | ✓ |
| UX peserta-dihapus (server guard) | Redirect destination + pesan saat removed coba StartExam/SubmitExam | ✓ |
| Detail kolom removal | RemovalReason length + RemovedBy identity (migration-affecting) | ✓ |

**User's choice:** Semua 3 area dibahas.

---

## Cakupan exclude-removed

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya surface admin batch (Recommended) | Exclude di Monitoring :2815/:3273 + InProgressCount + grouping :179 + pass-rate/cert-count hasil-batch (spec §D). /CMP/Records pekerja + sertifikat TIDAK disentuh | ✓ |
| Exclude total termasuk /CMP/Records | Removed lenyap dari semua surface termasuk riwayat pekerja sampai restore | |
| Count agregat saja, baris tetap ber-badge | Hanya angka agregat exclude; baris tetap tampil ber-badge "Dikeluarkan" | |

**User's choice:** Hanya surface admin batch (Recommended)
**Notes:** Selaras prinsip "sertifikat utuh & reversibel"; blast-radius 409 minimal; hindari over-exclude (STATE Open Concern (c)). Worker-facing records di luar daftar spec §D.

---

## UX peserta-dihapus (server guard)

| Option | Description | Selected |
|--------|-------------|----------|
| Redirect Assessment + TempData (Recommended) | Konvensi existing semua block StartExam; SubmitExam guard sebelum grading, jawaban discard | ✓ |
| Halaman dedicated "Anda dikeluarkan" | View baru, selaras force-kick SignalR 412 | |

**User's choice:** Redirect Assessment + TempData (Recommended)
**Notes:** Nol komponen baru, konsisten. Halaman dedicated force-kick = scope Phase 412.

---

## Detail kolom removal

| Option | Description | Selected |
|--------|-------------|----------|
| userId + Reason dibatasi 500 (Recommended) | RemovedBy=userId (cermin CreatedBy); RemovalReason=nvarchar(500) | ✓ |
| userId + Reason nvarchar(max) | RemovedBy=userId; RemovalReason=nvarchar(max) default EF | |
| Serahkan ke planner (Claude discretion) | Default wajar saat planning | |

**User's choice:** userId + Reason dibatasi 500 (Recommended)
**Notes:** Konsisten pola audit existing; hindari nvarchar(max) tak perlu untuk alasan modal pendek.

---

## Claude's Discretion

- Penempatan presisi guard line di StartExam (rekomendasi: sebelum mark-InProgress).
- Fluent API vs Data Annotation untuk HasMaxLength(500).
- Bentuk exact query exclude (.Where tambahan vs predikat gabungan).
- Cakupan unit/integration test guard + exclude.

## Deferred Ideas

- Endpoint add/remove/restore + RBAC/Proton-reject → Phase 410/411.
- Panel UI "Peserta Dikeluarkan" + SignalR + halaman dedicated force-kick → Phase 412.
- Test+UAT (xUnit + Playwright) → Phase 413.
- Exclude di /CMP/Records pekerja → sengaja TIDAK (D-01a).
- Todo cleanup data test pasca-367 (skor 0.6) → tetap backlog, bukan scope 409.
