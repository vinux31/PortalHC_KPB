# Phase 355: Test & UAT - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-09
**Phase:** 355-test-uat
**Areas discussed:** Metode UAT (TST-02), Scope konsolidasi xUnit (TST-01), Setup data UAT + fixture, Regression guard (SC#3)

---

## Metode UAT (TST-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Spec Playwright committed | File baru `tests/e2e/image-in-assessment.spec.ts`, repeatable/CI, pola global.setup. Deliverable durable + selaras v16.0 + lesson 354 runtime. | ✓ |
| Claude-driven Playwright MCP | UAT live via MCP (seperti Phase 354). Cepat, tanpa maintenance, tapi tak repeatable/committed. | |
| Keduanya | Spec committed + MCP exploratory smoke. Paling lengkap, paling mahal. | |

**User's choice:** Spec Playwright committed ("sesuai reko")
**Notes:** REQ TST-02 menamai UAT sebagai deliverable; spec committed = artifact durable. MCP boleh ad-hoc debugging saja.

---

## Scope konsolidasi xUnit (TST-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Gap-audit + suite hijau | Verifikasi 3 file tes existing cover SC#1; tambah hanya gap nyata (mis. replace hapus file LAMA). Minimal net-baru. | ✓ |
| + Tes integrasi controller | Tambah tes controller-level CreateQuestion/EditQuestion HTTP → ImagePath persist DB. | |
| Konsolidasi 1 kelas | Gabung tes tersebar jadi 1 kelas image. Risiko churn. | |

**User's choice:** Gap-audit + suite hijau
**Notes:** Coverage TST-01 sudah ada dari fold 352/353 (FileUploadHelper/PackageImageSync/PackageImageDelete). Cek spesifik: assert eksplisit file LAMA di-File.Delete saat replace.

---

## Setup data UAT + fixture gambar

| Option | Description | Selected |
|--------|-------------|----------|
| Admin-UI create + fixtures | Spec admin buat soal+opsi ber-gambar via form upload nyata (setInputFiles) → sekalian uji upload → peserta ujian. Commit 2 fixture JPG+PNG. + snapshot/restore. | ✓ |
| Seed SQL + restore | Paket pre-seeded via tests/sql/355-image-seed.sql + global.setup BACKUP/restore. Lewati upload UI. | |
| Hybrid | Seed paket dasar via SQL, upload gambar via UI. | |

**User's choice:** Admin-UI create + fixtures
**Notes:** Jalur upload admin = bagian SC#2 TST-02, jadi diuji live. Guardrail Seed Workflow wajib (DB snapshot/restore + cleanup file wwwroot/uploads/questions + journal).

---

## Regression guard (SC#3)

| Option | Description | Selected |
|--------|-------------|----------|
| Rerun suite + assert null | Rerun dotnet test + spec exam existing sbg baseline + 1 assert "soal tanpa gambar → tak render <img>" (guard null RND-07). | ✓ |
| Rerun suite existing saja | Bukti regresi cukup dari tes lama hijau. | |
| Blok regresi khusus tipe | Tambah blok regresi end-to-end MC/MA/Essay tanpa gambar. | |

**User's choice:** Rerun suite + assert null
**Notes:** Murah & terarah; guard cabang null partial _QuestionImage (Phase 354 L-02).

---

## Claude's Discretion

- Nama file spec & fixture, path fixture, reuse helper Playwright vs baru.
- Spec buat assessment penuh via wizard vs paket dasar minimal + upload-via-UI (syarat: upload diuji live + guardrail snapshot/restore).
- Bentuk assert `<img>` (src/img-fluid/lazy/alt/lightbox).
- Apakah surface admin essay-monitoring (RND-05) + EditPesertaAnswers (RND-06) ikut spec — opsional.

## Deferred Ideas

- Tes integrasi controller-level (angkat bila gap-audit butuh).
- Konsolidasi/refactor kelas image.
- UAT MCP-only sebagai deliverable.
- UAT essay-monitoring + EditPesertaAnswers dalam spec (nice-to-have).
