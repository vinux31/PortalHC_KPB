# Phase 403: OrganizationController Cascade/Guard UserUnits-Aware - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 403-organizationcontroller-cascade-guard-userunits-aware
**Areas discussed:** Granularitas blok reparent, Cakupan + pesan guard, Tampilan Preview UserUnits, Atomicity + mirror recompute

---

## Gray area selection

| Option | Selected |
|--------|----------|
| Granularitas blok reparent | ✓ |
| Cakupan + pesan guard | ✓ |
| Tampilan Preview UserUnits | ✓ |
| Atomicity + mirror recompute | ✓ |

**User's choice:** Semua 4 area.

---

## Granularitas blok reparent (ORG-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Blok hanya saat split | Reparent diizinkan + auto-update Section utk pekerja single-unit; HARD-BLOCK hanya bila pekerja punya unit lain di Bagian berbeda (UserUnits terpecah). Spec literal, minimal change. | ✓ |
| Blok bila ada anggota | Reparent unit ber-anggota selalu ditolak; operator mutasi manual dulu. | |
| Blok bila ada multi-unit | Izinkan single-unit; blok bila ada pekerja multi-unit apa pun (walau unit lain Bagian sama). | |

**User's choice:** Blok hanya saat split (→ D-01/D-01a/D-01b)
**Notes:** Pilih spec literal — hard-block hanya pada split nyata >1 Bagian; deteksi berbasis UserUnits aktif; reparent tak ubah baris UserUnits (cuma Section mirror).

---

## Cakupan + pesan delete/deactivate guard (ORG-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Delete + Deactivate, pesan spesifik | Kedua guard (:447 delete + :391 toggle) scan UserUnits sekunder; pesan sebut alasan (sekunder). | ✓ |
| Delete + Deactivate, pesan generik | Kedua guard scan UserUnits tapi pesan generik existing. | |
| Delete saja | Hanya :447 UserUnits-aware; :391 biarkan scalar. | |

**User's choice:** Delete + Deactivate, pesan spesifik (→ D-02/D-02a/D-02b)
**Notes:** Sekunder tak terlihat di scalar Users.Unit → pesan harus jelaskan. Guard berbasis UserUnits aktif; guard existing lain dipertahankan.

---

## Tampilan PreviewEditCascade untuk baris UserUnits (ORG-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Line terpisah | Field affectedUserUnitsCount + baris sendiri di modal ("X baris keanggotaan unit"). Transparan, preview==actual. | ✓ |
| Gabung ke user count | Fold ke affectedUsersCount (union dedup). Ringkas tapi kabur. | |

**User's choice:** Line terpisah (→ D-03/D-03a)
**Notes:** Konsep beda dari scalar mirror count; hitungan WAJIB persis = yg di-update aktual.

---

## Atomicity rename/reparent cascade + sinkron primary-mirror (ORG-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Transaksi + recompute inline | Wrap BeginTransactionAsync (pola 399); rename UserUnits rows + verifikasi mirror inline di OrganizationController (jaga isolasi Wave-1). | ✓ |
| Transaksi + reuse helper 399 | Wrap transaksi + panggil helper WorkerController 399. Hindari drift, tapi compile-dependency lintas-controller. | |
| Claude's discretion | Serahkan planner pilih inline vs reuse. | |

**User's choice:** Transaksi + recompute inline (→ D-04/D-04a)
**Notes:** Jaga file terisolasi Wave-1 (400/401/403 paralel worktree). Mirror primary-holder sudah ter-update existing L219; rename tak ubah IsPrimary → cukup rename string + jaga mirror==baris-primary.

## Claude's Discretion

- Bentuk query deteksi-split, wording pesan blok, markup baris preview, gaya scan guard (correlated vs join).

## Deferred Ideas

- Mutasi-Bagian first-class (auto-pindah split) — out-of-scope (spec §8).
- Test invariant SQL-riil reparent/delete multi-unit — Phase 404.
- CMP analytics per-unit — out-of-scope (D1=b).
