# Phase 426: Audit-Log EditOrganizationUnit - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 426-audit-log-editorganizationunit
**Areas discussed:** Pemicu audit, Format deskripsi, Representasi parent

---

## Pemicu Audit

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya saat ada perubahan | Audit ditulis hanya jika oldName≠newName ATAU oldParentId≠parentId; no-op edit tak menulis audit | ✓ |
| Setiap Edit sukses | Tulis audit tiap POST Edit berhasil commit, walau nilai identik | |

**User's choice:** Hanya saat ada perubahan
**Notes:** Selaras "rename atau reparent" di SC; hindari noise.

---

## Format Deskripsi

| Option | Description | Selected |
|--------|-------------|----------|
| Satu baris gabungan (mirror Delete) | Single string: nama lama→baru, parent lama→baru, + cascade counts | ✓ |
| Pisah per aspek | Baris terpisah untuk rename vs reparent | |

**User's choice:** Satu baris gabungan (mirror Delete) — "sesuai reko kamu"
**Notes:** Konsisten pola DeleteOrganizationUnit (1 baris/aksi).

---

## Representasi Parent

| Option | Description | Selected |
|--------|-------------|----------|
| ID mentah (sesuai SC) | oldParentId→parentId sebagai angka, tanpa query DB tambahan | ✓ |
| Resolve ke nama unit | Query nama parent lama+baru (+2 query DB) | |

**User's choice:** ID mentah (sesuai SC) — "sesuai reko kamu"
**Notes:** Jaga blok swallow ringan; persis bunyi SC.

## Claude's Discretion

- Format string deskripsi final (asal memuat semua field D-02).

## Deferred Ideas

None.
