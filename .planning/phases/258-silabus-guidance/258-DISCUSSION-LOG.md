# Phase 258: Silabus & Guidance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-25
**Phase:** 258-silabus-guidance
**Areas discussed:** Orphan Cleanup, Import vs Inline Edit, Guidance Access, Deactivate Scope, Export Silabus, Template Download, Status Tab

---

## Orphan Cleanup

| Option | Description | Selected |
|--------|-------------|----------|
| Hard delete (Recommended) | Deliverable tidak ada di Excel baru langsung dihapus. Block jika ada active DeliverableProgress. | ✓ |
| Soft delete (deactivate) | Set IsActive=false saja, tidak hapus. | |
| You decide | Claude tentukan berdasarkan code | |

**User's choice:** Hard delete (Recommended)
**Notes:** -

| Option | Description | Selected |
|--------|-------------|----------|
| Otomatis saat import (Recommended) | Import Excel baru → orphan langsung di-cleanup | ✓ |
| Preview + konfirmasi | Tampilkan daftar orphan dulu, admin confirm | |
| You decide | Claude tentukan | |

**User's choice:** Otomatis saat import (Recommended)
**Notes:** -

---

## Import vs Inline Edit

| Option | Description | Selected |
|--------|-------------|----------|
| Import + Inline keduanya (Recommended) | Test ImportSilabus DAN SilabusSave | ✓ |
| Import Excel saja | Fokus hanya ke ImportSilabus | |
| Import prioritas, inline nice-to-have | Import fokus utama, inline kalau sempat | |

**User's choice:** Import + Inline keduanya (Recommended)

| Option | Description | Selected |
|--------|-------------|----------|
| Header mismatch | File Excel dengan kolom salah/kurang | ✓ |
| Duplicate detection | Row yang sudah ada di DB | ✓ |
| Required field kosong | Row dengan field wajib kosong | ✓ |
| File format salah | Upload file non-Excel | ✓ |

**User's choice:** Semua error scenario dipilih
**Notes:** -

---

## Guidance Access

| Option | Description | Selected |
|--------|-------------|----------|
| Test keduanya (Recommended) | ProtonDataController (Admin/HC) + CDPController (Coach/Coachee) | ✓ |
| CDPController saja | Fokus Coach/Coachee access | |
| You decide | Claude tentukan | |

**User's choice:** Test keduanya (Recommended)

| Option | Description | Selected |
|--------|-------------|----------|
| Happy path + file type (Recommended) | Upload/replace/delete valid + file type validation | ✓ |
| Happy path saja | Cukup test fungsi dasar | |
| Full edge cases | Termasuk file >10MB, unicode, concurrent | |

**User's choice:** Happy path + file type (Recommended)
**Notes:** -

---

## Deactivate Scope

| Option | Description | Selected |
|--------|-------------|----------|
| UI hide/show + basic cascade (Recommended) | Deactivate → hilang, reactivate → muncul. Cek SubKompetensi/Deliverable ikut. | ✓ |
| Deep cascade test | Test sampai impact ke DeliverableProgress | |
| You decide | Claude tentukan | |

**User's choice:** UI hide/show + basic cascade (Recommended)
**Notes:** DeliverableProgress impact di Phase 259+

---

## Export Silabus

| Option | Description | Selected |
|--------|-------------|----------|
| Masuk Phase 258 (Recommended) | Export terkait langsung silabus management | ✓ |
| Defer ke Phase 261 | Phase 261 sudah cover export | |

**User's choice:** Masuk Phase 258 (Recommended)
**Notes:** -

---

## Template Download + Status Tab

| Option | Description | Selected |
|--------|-------------|----------|
| Happy path keduanya (Recommended) | Template: file valid. Status tab: tampilkan status per kombinasi. | ✓ |
| Template saja | Status tab skip | |
| You decide | Claude tentukan | |

**User's choice:** Happy path keduanya (Recommended)
**Notes:** -

---

## Claude's Discretion

- Urutan test scenario per requirement
- Detail teknis orphan cleanup implementation
- Prioritas bug fix jika ditemukan multiple issues

## Deferred Ideas

None — discussion stayed within phase scope
