# Phase 416: Scoped Shuffle (Acak per-Section) - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Pengacakan soal & opsi terjadi **hanya di dalam lingkup Section** (soal tak melompat antar-Section), dengan assessment **tanpa Section** berperilaku **persis seperti sekarang** (kompatibel-mundur). Refactor `ShuffleEngine` jadi acak per-Section (kunci komposit `(SectionNumber, ET)`), wire ke `StartExam`, dan jadikan reshuffle section-aware.

**REQ:** SHF-01..04. **Migration: FALSE** (pakai kolom `AssessmentPackageSection.ShuffleEnabled` yang sudah ada dari 415).

OUT (fase lain): pagination section-aware (417), opsi dinamis A–F render/grading (418), export label Section (419).
</domain>

<decisions>
## Implementation Decisions

### Toggle granularity acak per-Section
- **D-416-01:** **SATU saklar `ShuffleEnabled` per-Section** (kolom yang sudah ada dari 415) meng-gate acak **SOAL + OPSI sekaligus** untuk Section itu (ON = soal & opsi Section diacak; OFF = urut `Question.Order`). **TIDAK** dipecah jadi 2 toggle terpisah (soal vs opsi per-Section) — ditolak user setelah tahu ongkos: butuh kolom DB baru → migration + UI nambah, manfaat kecil (acak-opsi-per-Section-independen jarang dipakai). **migration=FALSE dipertahankan.**
- Induk (assessment) tetap **2 toggle terpisah**: `ShuffleQuestions` + `ShuffleOptions` (tak berubah).
- **Precedence (D-14, locked spec):** induk = saklar utama. Induk `ShuffleQuestions` OFF → SEMUA section terurut (toggle per-Section diabaikan). Induk ON → tiap Section ikut `ShuffleEnabled`-nya. Sama untuk `ShuffleOptions` (di-gate oleh `ShuffleEnabled` per-Section yang sama).

### Semantik Reshuffle (>1 paket)
- **D-416-02:** Reshuffle (`ReshufflePackage` & `ReshuffleAll`) = **RE-ROLL** — ambil sampling baru lintas-paket **dalam batas Section**, deterministik by `workerIndex` (urutan sibling sorted). Peserta bisa dapat **SET soal berbeda** per-Section (bukan cuma urutan). Konsisten dengan engine existing. Section-aware: soal tetap tak bocor antar-Section.

### ET-coverage saat Section sempit
- **D-416-03:** **Best-effort + peringatan ke HC.** Jamin 1 soal per Elemen Teknis sampai kuota K (jumlah soal Section) habis, sisanya balanced (sama perilaku K-min existing). **TAMBAH peringatan ke HC saat kelola Section** bila `K < jumlah distinct ET` di Section (cakupan ET tak penuh). **TIDAK blokir** mulai ujian (Section kecil itu konfigurasi sah). Peringatan = sinyal, bukan error.

### Section opsional (UX inti — non-negotiable)
- **D-416-04:** Section tetap **OPSIONAL**. Assessment **tanpa Section** (semua `SectionId=null`) = **perilaku lama identik** (acak global ET-aware, 1 toggle ngocok semua). HC yang mau gampang cukup **tak isi Section** (kolom Excel boleh kosong → input soal TIDAK jadi lebih susah). Kerumitan scoped-shuffle hanya muncul saat HC **sengaja** pakai Section. Grup "Lainnya" (null section di paket bersection) ikut toggle induk, selalu di urutan terakhir, ET-aware komposit `(null, ET)` (D-15, locked).

### Verifikasi (cakupan test)
- **D-416-05:** Backward-compat WAJIB dibuktikan **PENUH**: (1) **golden-order regression** — all-null = urutan IDENTIK baseline pra-416; (2) determinisme `workerIndex`; (3) reshuffle section-aware tak bocor antar-Section; (4) **Playwright UAT real-browser** acak per-Section. Bukan cuma smoke. (Catatan: 419 punya UAT milestone; 416 tetap UAT sendiri untuk scoped-shuffle.)

### Claude's Discretion
- Bentuk refactor `ShuffleEngine` (signature persis `BuildSectionQuestionAssignment`, apakah `BuildQuestionAssignment` lama dipertahankan sebagai jalur all-null vs di-wrap) — planner/researcher putuskan, asal kunci komposit `(SectionNumber, ET)` + jalur all-null = output identik baseline.
- Tempat & wording peringatan ET-coverage (D-416-03) di UI kelola Section.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Desain milestone (utama)
- `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` — desain v32.6. Untuk 416: **§6** (Alur Acak / Scoped Shuffle: `BuildSectionQuestionAssignment`, ON/OFF, precedence D-14), **§6.4** (definisi K = jumlah soal Section per paket, dijamin sama oleh D-13, K>0), **§13** (rencana fase + urutan), **§15.A** (resume saat config berubah), **§15.F** (reshuffle endpoints section-aware), **§15.G** (cross-feature: AddParticipantsLive eager-assignment WAJIB pakai per-section assignment sama; MultiUnit orthogonal). Keputusan D-09 (pooling lintas-paket per-Section), D-14 (precedence induk/anak), D-15 ("Lainnya" terakhir).
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` — desain toggle shuffle existing (induk `ShuffleQuestions`/`ShuffleOptions` + propagasi Pre→Post). Baseline yang di-generalisasi.

### Konteks fase sebelumnya
- `.planning/phases/415-section-foundation-import-excel-diperluas/415-CONTEXT.md` — data model Section + `ShuffleEnabled` per-Section (kolom yang dipakai 416, no new migration).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets / yang di-refactor
- `Helpers/ShuffleEngine.cs` — `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` (cross-package ET-aware K-min sampling, Phase1 1-per-ET + Phase2 balanced) + `BuildOptionShuffle(questions, shuffleOptions, rng)` + `Shuffle<T>` (Fisher-Yates). **Generalisasi → `BuildSectionQuestionAssignment(...)` dengan kunci komposit `(SectionNumber, ET)`.** Jalur all-null HARUS hasilkan output identik baseline.
- `Models/AssessmentPackage.cs` — `AssessmentPackageSection.ShuffleEnabled` (bit default 1, dari 415) = saklar per-Section (D-416-01, no new column). `PackageQuestion.SectionId` (int? nullable) + `ElemenTeknis` + `Order`.

### Integration Points
- `Controllers/CMPController.StartExam` (~L1127) — saat ini panggil `ShuffleEngine.BuildQuestionAssignment` + `BuildOptionShuffle`; wire ke jalur section-aware. Re-guard struktur Section (415 SEC-04) sudah ada sebelum assignment.
- Reshuffle endpoints `ReshufflePackage` / `ReshuffleAll` — jadikan section-aware lewat engine yang di-refactor (D-416-02).
- `AddParticipantsLive` (v32.5 Phase 410) — eager-assignment peserta baru WAJIB pakai per-section assignment sama (seed `workerIndex` konsisten) — jangan drift.

### Constraints
- Determinisme `workerIndex` (urutan sibling sorted) = invariant (Phase 373). Reshuffle deterministik.
- `K` per-Section dijamin sama antar-paket-saudara + K>0 (D-13, sudah di-enforce 415 import + StartExam guard).
</code_context>

<specifics>
## Specific Ideas

- Nilai HC: **"fleksibel TAPI mudah input soal."** Diterjemahkan: base flow (tanpa Section) tetap segampang sekarang; Section + per-Section toggle = penyempurnaan **opt-in**, bukan beban. Default masuk akal (`ShuffleEnabled`=ON per-Section), HC yang gak mau ribet cukup pakai saklar induk.
- User minta penjelasan hierarki Section vs Elemen Teknis vs acak-soal vs acak-opsi (lihat DISCUSSION-LOG) — pastikan UI kelola Section + dokumentasi HC jelaskan beda Section (kelihatan, blok) vs ET (tersembunyi, penyeimbang sampling).
</specifics>

<deferred>
## Deferred Ideas

- **2 saklar terpisah per-Section** (acak-soal vs acak-opsi independen per-Section) — ditolak di 416 (butuh kolom DB baru → migration + UI nambah, manfaat kecil). Angkat lagi hanya bila HC benar-benar butuh kontrol acak-opsi per-Section independen.

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (area: database, score 0.6) — **tidak di-fold**: tugas maintenance/ops data lokal, bukan scope scoped-shuffle. Tetap di backlog todo.
</deferred>

---

*Phase: 416-scoped-shuffle-acak-per-section*
*Context gathered: 2026-06-23*
