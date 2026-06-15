# Phase 366: Cascade Image File Cleanup - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-12
**Phase:** 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
**Areas discussed:** Lokasi & bentuk helper, Kedalaman test SC#4, Mekanisme batch-aware, Cleanup folder kosong

---

## Lokasi & bentuk helper ref-count

| Option | Description | Selected |
|--------|-------------|----------|
| Static di Helpers/ | Helper static (DbContext+webRoot+logger+paths). Mudah di-test, 1 sumber kebenaran, menggantikan mirror test palsu. | ✓ |
| Private method di controller | Private instance method, akses _context/_env/_logger langsung, signature persis SC#1. Tak bisa di-test tanpa instansiasi controller. | |

**User's choice:** Static di Helpers/ (rekomendasi)
**Notes:** Sejalan SC#4 (dotnet test) + mirror `DeleteIfUnreferenced` di `PackageImageDeleteTests.cs` dipromosikan jadi helper produksi nyata.

---

## Kedalaman test SC#4

| Option | Description | Selected |
|--------|-------------|----------|
| Integration real-SQL | xUnit disposable real-SQL (pola Phase 344 TEST-05): assert file fisik terhapus + shared-path Pre/Post selamat. + UAT manual. | ✓ |
| Unit-test helper + UAT manual | Unit-test logika ref-count + UAT manual file-op. Lebih ringan, file-op tak ter-assert otomatis. | |
| UAT manual + build hijau | Cukup UAT manual @5277 + build/test existing hijau. Paling ringan. | |

**User's choice:** Integration real-SQL (rekomendasi)
**Notes:** Kasus shared-path Pre/Post (SC#3) paling rawan → wajib ter-assert otomatis.

---

## Mekanisme batch-aware ref-count

| Option | Description | Selected |
|--------|-------------|----------|
| Post-commit AnyAsync | Ref-count AnyAsync ke DB SETELAH CommitAsync; baris batch sudah hilang → AnyAsync false otomatis. Reuse pola inline. | ✓ |
| Explicit exclusion-set | Bangun set Id/path sedang dihapus, ref-count abaikan anggota set. Duplikasi logika, rawan drift. | |

**User's choice:** Post-commit AnyAsync (rekomendasi)
**Notes:** Memenuhi SC#2 (sadar batch) + SC#3 (shared selamat) tanpa kode tambahan.

---

## Cleanup folder kosong {packageId}

| Option | Description | Selected |
|--------|-------------|----------|
| Defer / skip | Jangan hapus folder. Scope ROADMAP = file saja. Hindari scope-creep + race. → Deferred Idea. | ✓ |
| Hapus folder kalau kosong | Directory.Delete setelah File.Delete bila kosong. Tambah edge-case race. | |

**User's choice:** Defer / skip (rekomendasi)
**Notes:** Dicatat sebagai Deferred Idea di CONTEXT.md.

---

## Claude's Discretion

- Nama/signature exact helper (selama static + di Helpers/).
- Refactor penuh `PackageImageDeleteTests.cs` vs tambah integration test baru.
- Label pesan log warn-only per call-site.

## Deferred Ideas

- Cleanup folder kosong `wwwroot/uploads/questions/{packageId}` — kandidat backlog hygiene.
