# Phase 229: Audit Renewal Logic & Edge Cases - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 229-audit-renewal-logic-edge-cases
**Areas discussed:** FK Chain Fix Strategy, Badge Count Sync, Status Edge Cases, MapKategori Canonical, Grouping URL-safety, Double Renewal Prevention, Bulk Mixed-Type Flow, Empty State Handling

---

## FK Chain Fix Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Fix kode saja | Perbaiki kode agar FK selalu benar ke depan. Data lama dibiarkan. | |
| Fix kode + migration script | Perbaiki kode DAN buat migration untuk fix data lama. | |
| Fix kode + audit report | Perbaiki kode, generate laporan data bermasalah untuk review manual. | ✓ |

**User's choice:** Fix kode + audit report
**Notes:** User juga memilih HTML report format (seperti audit report v7.7).

### FK Validation Level

| Option | Description | Selected |
|--------|-------------|----------|
| Controller-level saja | Validasi di action method sebelum save. | |
| Model + Controller | IValidatableObject di model DAN validasi di controller. | |
| Database constraint | CHECK constraint via migration. | |

**User's choice:** Claude's discretion — "lakukan analisa, menurutmu pilih terbaik yang mana"

---

## Badge Count Sync

| Option | Description | Selected |
|--------|-------------|----------|
| Refactor ke single source | Semua tempat harus pakai BuildRenewalRowsAsync, hapus counting duplicate. | ✓ |
| Biarkan jika hasilnya sama | Tidak perlu refactor kalau logic menghasilkan angka sama. | |
| Consolidate + cache | Single source + caching layer. | |

**User's choice:** Refactor ke single source

---

## Status Edge Cases

### DeriveCertificateStatus null handling

| Option | Description | Selected |
|--------|-------------|----------|
| Benar, Expired | Non-permanent tanpa tanggal expiry = treat as expired. | ✓ |
| Ubah ke Unknown/TBD | Status baru supaya admin bisa bedakan. | |
| Ubah ke Aktif | Anggap masih aktif. | |

**User's choice:** Benar, Expired (keep existing behavior)

### Assessment CertificateType

| Option | Description | Selected |
|--------|-------------|----------|
| Memang null, by design | Assessment tidak punya CertificateType. | |
| Audit apakah perlu ditambah | Periksa apakah assessment seharusnya punya CertificateType. | ✓ |

**User's choice:** Audit apakah perlu ditambah

---

## MapKategori Canonical

| Option | Description | Selected |
|--------|-------------|----------|
| Database canonical | AssessmentCategories.Name jadi sumber kebenaran. | |
| MapKategori tetap hardcode | Hardcode cukup, kategori jarang berubah. | |
| Claude's discretion | Biarkan Claude analisa. | |

**User's initial response:** "bukannya terkait kategori assessment maupun training sudah ada di page manage categories?"
**Clarification:** User mengingatkan bahwa kategori sudah dikelola lewat ManageCategories page (AssessmentCategories CRUD).

### Follow-up: Fix scope

| Option | Description | Selected |
|--------|-------------|----------|
| Fix di Phase 229 | Ini logic issue (LDAT-05), fix sekarang. | ✓ |
| Defer ke Phase 230 | Ini display issue, fix saat UI audit. | |

**User's choice:** Fix di Phase 229

---

## Grouping URL-safety

| Option | Description | Selected |
|--------|-------------|----------|
| Verifikasi decode konsisten | Pastikan semua tempat decode GroupKey pakai logika yang sama. | ✓ |
| Claude's discretion | Biarkan Claude audit dan fix. | |

**User's choice:** Verifikasi decode konsisten

---

## Double Renewal Prevention

| Option | Description | Selected |
|--------|-------------|----------|
| Verifikasi server-side check | Pastikan action RenewCertificate juga check IsRenewed di server-side. | ✓ |
| Claude's discretion | Biarkan Claude audit. | |

**User's choice:** Verifikasi server-side check

---

## Bulk Mixed-Type Flow

| Option | Description | Selected |
|--------|-------------|----------|
| Tolak mixed batch | Bulk renew hanya satu tipe per batch. Error kalau mixed. | ✓ |
| Allow mixed, split otomatis | Terima mixed, internal split jadi 2 proses. | |
| Claude's discretion | Biarkan Claude tentukan. | |

**User's choice:** Tolak mixed batch

---

## Empty State Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Pesan informatif sederhana | "Tidak ada sertifikat yang perlu di-renew saat ini" + icon checkmark. | ✓ |
| Redirect ke halaman lain | Redirect otomatis. | |
| Claude's discretion | Biarkan Claude tentukan. | |

**User's choice:** Pesan informatif sederhana

---

## Claude's Discretion

- FK validation level (controller, model, atau DB constraint)
- Pendekatan sinkronisasi MapKategori dengan DB
- Detail HTML audit report

## Deferred Ideas

None — discussion stayed within phase scope
