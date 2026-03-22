# Phase 235: Audit Execution Flow - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 235-audit-execution-flow
**Areas discussed:** Evidence & Resubmit, Approval Chain Consistency, Status History & Notifikasi, PlanIdp View Accuracy

---

## Evidence & Resubmit

| Option | Description | Selected |
|--------|-------------|----------|
| Pertahankan file lama | File lama tetap di server sebagai history, evidence baru sebagai versi terbaru | ✓ |
| Ganti file lama | File lama dihapus, diganti file baru | |
| Claude decides | Biarkan Claude tentukan | |

**User's choice:** Pertahankan file lama
**Notes:** User klarifikasi bahwa Coach yang upload evidence, bukan coachee.

| Option | Description | Selected |
|--------|-------------|----------|
| Last-write-wins | Upload terakhir yang masuk DB tersimpan | ✓ |
| Block duplicate upload | Tolak upload kedua dengan error | |

**User's choice:** Last-write-wins

| Option | Description | Selected |
|--------|-------------|----------|
| Single file saja | Satu evidence file per deliverable | ✓ |
| Multi-file support | Beberapa file per deliverable | |

**User's choice:** Single file saja

| Option | Description | Selected |
|--------|-------------|----------|
| Wajib | Rejection reason wajib diisi | ✓ |
| Opsional | Boleh reject tanpa alasan | |

**User's choice:** Wajib (sudah ada di codebase)

| Option | Description | Selected |
|--------|-------------|----------|
| Tanpa batas | Resubmit berapa kali pun | ✓ |
| Ada limit | Setelah N kali reject, perlu intervensi | |

**User's choice:** Tanpa batas

| Option | Description | Selected |
|--------|-------------|----------|
| Reset dari awal | Semua level approval review ulang | ✓ |
| Lanjut dari rejector | Hanya rejector ke atas yang review | |

**User's choice:** Reset dari awal (sudah di-implement)

| Option | Description | Selected |
|--------|-------------|----------|
| Validasi server-side | Cek tipe file dan max size di server | ✓ |
| Sesuai existing | Ikuti apa yang ada | |

**User's choice:** Validasi server-side (sudah ada)

| Option | Description | Selected |
|--------|-------------|----------|
| Rollback + error message | Jangan update status, tampilkan error | ✓ |
| Claude decides | Biarkan Claude tentukan | |

**User's choice:** Rollback + error message

---

## Approval Chain Consistency

**User requested:** Analisa existing approval chain di codebase terlebih dahulu.

**Analisa findings:**
1. Co-sign pattern — SrSpv dan SH approve independen
2. Overall Status langsung "Approved" saat salah satu approve (prematur)
3. No race condition guard
4. File lama path hilang saat resubmit
5. Notification dedup fragile (Message.Contains)

| Temuan | Di-fix? |
|--------|---------|
| Status Approved prematur | Tidak |
| Race condition guard | Tidak |
| File lama path hilang | ✓ |
| Notification dedup fragile | ✓ |

**User's choice:** Hanya fix file path history dan notification dedup. Status prematur dan race condition acceptable.

---

## Status History & Notifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Initial Pending (saat seed) | Insert Pending saat progress dibuat | ✓ |
| Evidence Upload/Submit | Insert Submitted saat upload | ✓ |
| Resubmit after reject | Insert Resubmitted saat upload ulang | ✓ |
| Sesuai existing + initial Pending | Hanya tambah Pending | ✓ |

**User's choice:** Semua 4 trigger dipilih — completeness maksimal.

| Option | Description | Selected |
|--------|-------------|----------|
| Audit existing saja | Verifikasi trigger yang ada | |
| Tambah notif resubmit | Notif khusus saat resubmit | ✓ |

**User's choice:** Tambah notifikasi resubmit

---

## PlanIdp View Accuracy

| Option | Description | Selected |
|--------|-------------|----------|
| Audit general saja | Verifikasi semua benar | ✓ |
| Coach role access | Coach hanya lihat mapped coachee | ✓ |
| Inactive silabus filtering | Tidak tampilkan IsActive=false | ✓ |
| Guidance tab access | Coachee tidak akses admin tab | ✓ |

**User's choice:** Semua 4 concern dipilih

---

## Claude's Discretion

- Evidence path history storage mechanism
- Notification dedup structured field approach
- First-write-wins implementation details
- PlanIdp audit detail findings

## Deferred Ideas

None
