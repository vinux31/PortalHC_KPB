# Phase 394: Page + Setup Room + authoring soal - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 394-page-setup-room-authoring-soal
**Areas discussed:** Struktur alur page, Authoring soal di flow, Worker picker, Persistence & handoff ke 395, Urutan langkah wizard, UX toggle sertifikat

---

## Struktur alur page

| Option | Description | Selected |
|--------|-------------|----------|
| Wizard multi-langkah (mirror CreateAssessment) | nav-pills + step-panel + 1 form; reuse pola JS showStep | ✓ |
| Single-page panjang (scroll + section) | Semua section 1 halaman scroll | |
| Accordion (section collapsible) | Tiap bagian dilipat/buka | |

**User's choice:** Wizard multi-langkah (mirror CreateAssessment)
**Notes:** Konsisten dgn CreateAssessment; reuse `#wizardStepNav` + `.step-panel` + `showStep()`.

---

## Authoring soal di flow

| Option | Description | Selected |
|--------|-------------|----------|
| Inline authoring saja (tulis soal baru di flow) | Tulis MC/MA/Essay + opsi/IsCorrect/ScoreValue/ElemenTeknis/Rubrik; paket draft di flow | ✓ |
| Hybrid: tulis baru ATAU clone paket existing | Tambah opsi clone paket existing sbg basis | |
| Pilih paket existing saja | Tak authoring inline (bentrok INJ-05) | |

**User's choice:** Inline authoring saja
**Notes:** Sesuai INJ-05 ("di dalam alur page inject"). Catatan scout: authoring asli = page terpisah `ManagePackageQuestions` (terikat packageId) → mekanisme embed = discretion planner; clone existing = deferred.

---

## Worker picker

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse org-tree picker CreateAssessment apa-adanya | Checkbox + panel "Peserta Terpilih" + search nama/email + filter Section | ✓ |
| Picker ramah-roster (search + paste banyak NIP) | Textarea tempel banyak NIP | |
| Keduanya (org-tree + input NIP) | org-tree + field tambah-via-NIP | |

**User's choice:** Reuse picker CreateAssessment (search + org-tree checkbox)
**Notes (free-text user):** *"maksutnya bagaimana ya ini, saya ingin ada dua fasilitas import excel atau tulis manual. phase 394 ini khusus yang ngisi form ya?, untuk excel import di phase 396 ya?. saya ingin formnya itu kayak create assessment, ada search dan org-tree picker (checkbox)"* → Dikonfirmasi: 394 = jalur form/manual; Import Excel = Phase 396; picker = gaya CreateAssessment. Verifikasi scout: `CreateAssessment.cshtml:271-349` punya filter Section + search `#userSearchInput` + checkbox + panel terpilih; hanya tampil user existing → NIP-tak-dikenal mustahil by-construction.

---

## Persistence & handoff ke 395

| Option | Description | Selected |
|--------|-------------|----------|
| Tahan di state form sampai commit final (no draft DB) | 394 kerangka di form/session; commit sekali via InjectAssessmentService (393) setelah 395 | ✓ |
| Draft tersimpan DB (bisa tutup & lanjut nanti) | Butuh store/state baru → risiko migration | |

**User's choice:** Tahan di state form sampai commit final (no draft DB)
**Notes:** Patuh 0-migration. 394 belum meng-inject (belum ada jawaban) — commit setelah 395.

---

## Urutan langkah wizard

| Option | Description | Selected |
|--------|-------------|----------|
| Sertifikat = langkah sendiri (6 langkah): Setup→Soal→Pekerja→Sertifikat→[Jawaban 395]→Konfirmasi | Sertifikat menonjol | |
| Sertifikat digabung Setup/Settings (5 langkah) | Paling mirror CreateAssessment | |
| Pekerja sebelum Soal (6 langkah): Setup→Pekerja→Soal→Sertifikat→[Jawaban 395]→Konfirmasi | Pilih audience dulu baru tulis soal | ✓ |

**User's choice:** Pekerja sebelum Soal (6 langkah)
**Notes:** Urutan final: 1.Setup Room → 2.Pilih Pekerja → 3.Authoring Soal → 4.Sertifikat → 5.[Jawaban=Phase 395] → 6.Konfirmasi. Langkah 5 placeholder di 394.

---

## UX toggle sertifikat

| Option | Description | Selected |
|--------|-------------|----------|
| Radio 3-mode + field kondisional | Auto (preview KPB/xxx/ROMAN/year + ValidUntil + Permanent) / Manual (NomorSertifikat + ValidUntil + Permanent) / Tanpa | ✓ |
| Dropdown 3-mode + field kondisional | Mode via dropdown | |
| Toggle switch + reveal minimal | Switch on/off lalu sub-pilihan | |

**User's choice:** Radio 3-mode + field kondisional
**Notes:** Perilaku dikunci carry 393: D-08 suppress-tak-lulus, D-09 manual-unik, D-10 ValidUntil-permanent (null), D-12 auto tahun-backdate.

## Claude's Discretion

- Mekanisme reuse authoring soal (extract partial vs replikasi inline vs embed) — nol-duplikasi semantik wajib.
- Bentuk holding form-state (hidden JSON / view-model / JS serialize-on-submit).
- Kontrak controller→InjectAssessmentService (ikut 393).
- Penamaan/ikon langkah, styling, copy, debounce Cek-judul, penanganan placeholder langkah 5.

## Deferred Ideas

- Clone/pilih paket existing; draft tersimpan DB; single-page/accordion; toggle dropdown/switch — semua ditolak (lihat CONTEXT deferred).
- Import Excel = 396; Jawaban + auto-gen = 395; Link Pre/Post = 397.
- Reviewed todo (not folded): cleanup data test lokal pasca-367 (false-positive match).
