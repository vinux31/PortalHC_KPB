# Phase 364: Restore Baseline Regresi e2e Exam - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

2 spec e2e exam lama (`tests/e2e/exam-taking.spec.ts` + `tests/e2e/exam-types.spec.ts`) hidup lagi sebagai baseline regresi. Patah sejak v20.0 karena validator naming REST-06 (`AssessmentAdminController.cs:874-881`, regex `^(Pre|Post)\s*Test\s+.+$`, case-SENSITIVE) menolak judul lama di langkah create.

**Test-only. Zero kode produksi. Migration=false.** Zero overlap file dengan 358-363 (verified 2026-06-10) — bisa paralel kapan saja.

**9 flow patah** (mode standard, kena validator):
- `exam-taking.spec.ts`: `Legacy Exam` (:35), `Token Exam` (:316), `ForceClose Exam` (:402)
- `exam-types.spec.ts`: `[317-SMOKE-W0] Order Verify` (:37), `[317-K] MA Exam` (:191), `[317-L] Essay Exam` (:310), `[317-M] Mixed Exam` (:435), `[317-N] NoReview Exam` (:588), `[317-O] ExtraTime Exam` (:690)

**FLOW P `[318-P] PrePost Exam`** (exam-types :860) pakai mode PrePostTest → **exempt** validator (:861/:874) — kemungkinan tidak patah, verifikasi via full run.

</domain>

<decisions>
## Implementation Decisions

### Pola judul baru
- **D-01:** Prefix seragam **"Pre Test"** untuk semua 9 flow standard. Zero risiko auto-pair — counterpart "Post Test" dengan remainder sama tak pernah ada.
- **D-02:** Marker flow dipertahankan **setelah prefix**: `Pre Test [317-K] MA Exam {ts}`. Marker tetap grep-able, regex lolos.
- **D-03:** Implementasi **edit per-call** di 2 file spec (9 argumen `uniqueTitle()`). Helper `tests/helpers/utils.ts` TIDAK disentuh — dipakai spec lain yang sudah jalan.
- **D-04:** Comply level = **lolos regex saja**. Nama lama dipertahankan sebagai suffix. Tidak perlu konvensi penuh `{Stage} Test {Track} {Lokasi}` — itu cuma teks pesan error, server hanya validasi regex.

### Definisi selesai (SC#4)
- **D-05:** Failure NON-judul (selector drift, fitur berubah v18-v25) **di-fix di test-code sampai kedua spec hijau**. Spirit fase = baseline hidup lagi, bukan sekadar judul. Tetap zero kode produksi.
- **D-06:** Kalau ketemu **bug produksi nyata**: catat sebagai temuan → backlog/fase lain, JANGAN fix di 364. Flow yang kena dapat `test.fixme('alasan + ref backlog')`.
- **D-07:** Dokumentasi failure tersisa: **deviasi di SUMMARY + entry backlog 999.x** kalau actionable (pola 355 Deviasi 2). Tidak perlu file FINDINGS terpisah.
- **D-08:** "PASS penuh @5277" = **1x full run hijau** live, retry policy default Playwright config.
- **D-09:** Run yang mengandung `fixme` = **BUKAN "PASS penuh"** — gate jatuh ke jalur SC#4 alternatif "failure terdokumentasi bukan-karena-judul". Jujur ke verifier; jangan hitung skip sebagai hijau.

### Guard auto-pair + exempt (SC#2/SC#3)
- **D-10:** **Run baseline diagnosa DULU** sebelum edit: kedua spec as-is @5277, catat failure per-flow (judul vs non-judul). Sesuai SC#2 "fix per-flow, bukan ganti buta".
- **D-11:** Guard auto-pair SC#3 = struktural (prefix seragam "Pre Test" + timestamp `uniqueTitle`) **+ 1 asersi DB** `LinkedGroupId IS NULL` di salah satu flow standard. exam-types sudah punya pola query DB — reuse.
- **D-12:** FLOW P [318-P]: **ikut full run saja**, tanpa asersi exempt khusus. PASS = bukti exempt benar; kalau patah non-judul → ikuti D-05/D-06.

### Eksekusi & cleanup
- **D-13:** Pola eksekusi 355 **keep as-is**: `Authentication__UseActiveDirectory=false dotnet run` (env override, tanpa edit file) + SEED snapshot DB sebelum run + restore & cek residue setelah + catat `docs/SEED_JOURNAL.md`.
- **D-14:** Cleanup data test = **snapshot/restore saja**. Tidak ada teardown delete tambahan di spec.
- **D-15:** Gate akhir = **2 spec target PASS @5277 + `dotnet test` suite hijau**. Tanpa full e2e suite — spec lain tak disentuh (utils.ts tak diubah), zero impact.

### Baseline ke depan + asersi
- **D-16:** Status baseline pasca-restore: **on-demand** — dipakai saat fase relevan sentuh area exam. BUKAN gate wajib tiap fase. Catat di SUMMARY sebagai baseline tersedia.
- **D-17:** Asersi sensitif judul panjang (card `hasText`, history row, certificate `toContainText`): **partial-match boleh** — substring unik (marker + timestamp) kalau UI truncate. Identitas tetap unik berkat timestamp.

### Claude's Discretion
- Urutan run spec, workers/serial config Playwright, timeout tuning.
- Bentuk pencatatan hasil run baseline diagnosa (cukup di SUMMARY plan).
- Pemilihan flow mana yang dapat asersi DB LinkedGroupId (D-11) — pilih yang paling murah.
- Detail teknis fix selector drift per-flow.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Temuan asal + acuan roadmap
- `.planning/phases/355-test-uat/355-03-SUMMARY.md` — Deviasi 2 = temuan asal breakage (validator v20 tolak "Legacy Exam"); Deviasi 1 = pola fix judul yang sudah terbukti (`Pre Test OJT IMG355 {ts}`); Deviasi 3 = pola AD off env override.
- `.planning/ROADMAP.md` §Phase 364 — goal + 4 success criteria + line refs validator.

### Kode yang menentukan perilaku (read-only — JANGAN diubah)
- `Controllers/AssessmentAdminController.cs:859-881` — auto-pair `TryAutoDetectCounterpartGroup` (:865, exempt PrePostTest :861) + validator REST-06 (:874-881, regex :876).
- `Controllers/AssessmentAdminController.cs:7111` — implementasi `TryAutoDetectCounterpartGroup` (IgnoreCase).

### Test infra
- `tests/helpers/utils.ts:23` — `uniqueTitle(prefix)` = `${prefix} ${Date.now()}` (timestamp guard, JANGAN diubah).
- `tests/e2e/helpers/examTypes.ts` — `createAssessmentViaWizard` (:51) + `createPrePostAssessmentViaWizard` (:534).
- `docs/SEED_WORKFLOW.md` + `docs/SEED_JOURNAL.md` — SOP snapshot/restore DB lokal.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `tests/helpers/utils.ts` `uniqueTitle` — sudah kasih timestamp; cukup ubah argumen prefix per-call.
- `tests/helpers/dbSnapshot.ts` + pola query SQL langsung di exam-types (:888-906) — reuse untuk asersi `LinkedGroupId IS NULL` (D-11).
- `tests/e2e/image-in-assessment.spec.ts` — contoh hidup spec yang judulnya sudah comply (`Pre Test OJT IMG355 {ts}`, fix commit `d4edae7c` Phase 355) — pola acuan.
- `tests/e2e/global.setup.ts` / `global.teardown.ts` — auth state setup existing.

### Established Patterns
- Run lokal: `Authentication__UseActiveDirectory=false dotnet run` @5277 (Phase 355 Deviasi 3).
- SEED workflow: snapshot sebelum, restore + cek residue sesudah, journal entry.
- Judul test pakai marker fase `[317-K]` dsb untuk traceability — dipertahankan.

### Integration Points
- `tests/e2e/exam-taking.spec.ts` — 3 flow (Legacy/Token/ForceClose), judul di :35/:316/:402, asersi hasText/search di banyak baris.
- `tests/e2e/exam-types.spec.ts` — 6 flow standard + FLOW P exempt; judul di :37/:191/:310/:435/:588/:690/:860.
- Validator hanya aktif di create (POST) mode standard — judul baru harus lolos di langkah create wizard.

</code_context>

<specifics>
## Specific Ideas

- Pola judul final: `Pre Test [marker] {nama lama} {ts}` — contoh: `Pre Test [317-K] MA Exam 1718064000000`, `Pre Test Legacy Exam 1718064000000`.
- Urutan kerja yang diminta: (1) run baseline diagnosa as-is → catat bukti per-flow, (2) edit 9 judul, (3) fix failure non-judul di test-code, (4) full run gate.

</specifics>

<deferred>
## Deferred Ideas

- Bug produksi apa pun yang ketemu saat restore → temuan ke backlog 999.x / fase lain (D-06), bukan di-fix di 364.
- Menjadikan 2 spec ini gate wajib tiap fase UAT → ditolak untuk sekarang (D-16: on-demand); bisa dipertimbangkan ulang kalau area exam sering regresi.

</deferred>

---

*Phase: 364-restore-baseline-regresi-e2e-exam*
*Context gathered: 2026-06-11*
