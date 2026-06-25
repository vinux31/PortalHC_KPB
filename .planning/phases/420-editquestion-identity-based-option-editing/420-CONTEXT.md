# Phase 420: EditQuestion Identity-Based Option Editing - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Ganti mekanisme upsert opsi jawaban di `AssessmentAdminController.EditQuestion` POST dari **posisional** (`existing[i]` OrderBy Id) menjadi **identity-based** (match baris form input ke `PackageOption` existing by stable `Id`). Tujuan: menghapus/mengedit opsi (termasuk opsi TENGAH) pada soal yang sudah dijawab peserta tidak lagi me-relabel jawaban peserta secara senyap. Guard answered-option (D-418-02) menyala untuk delete posisi MANAPUN (bukan hanya ekor). Pertahankan regression-lock 999.14 (FK-Restrict 500 tetap tertutup).

**Bukan scope:** perubahan skema/migration (FALSE), reorder opsi via drag, data-repair response historis yang sudah ter-relabel, form Inject. Kontrak `List<OptionInput>` + `correctIndex` + validator min-2/max-6 (Phase 418) tetap; HANYA cara match input→existing yang berubah (posisi → identity).
</domain>

<decisions>
## Implementation Decisions

### Carrier identity & anti-tamper (D-01)
- **D-01:** Tambah properti `int? Id` ke `Models/OptionInput.cs` sebagai carrier identity (di-bind `options[i].Id` via indexed convention). **Ini secara sadar MEREVISI catatan T-418-06** ("JANGAN tambah properti Id … tidak boleh disuplai client") — rasional lama (Id ditentukan server, match posisional) sudah tidak berlaku karena identity-based WAJIB menerima Id dari client.
- **D-01a (anti-tamper, WAJIB):** Server memvalidasi SETIAP `Id` non-null yang disuplai harus ∈ `q.Options.Select(o => o.Id)` untuk soal ini. Bila ada `Id` asing/tak-dikenal → **TOLAK seluruh edit** (fail-closed) via `TempData["Error"]` + `RedirectToAction("ManagePackageQuestions")`, JANGAN diam-diam diperlakukan opsi baru. Ini menutup IDOR/mass-assignment yang dulu dijaga T-418-06 — kini lewat validasi eksplisit, bukan via melarang properti.
- **D-01b (mekanisme upsert):** Baris dengan `Id` non-null (valid) → UPDATE record existing itu (teks + IsCorrect + gambar via `ApplyOptionImageIntent`). Baris dengan `Id` null/kosong + teks terisi → ADD opsi baru. Existing `PackageOption` yang Id-nya TIDAK muncul di himpunan Id submit → kandidat DELETE.
- **D-01c (kill-drift):** Himpunan `removedOptionIds` (untuk guard) HARUS dihitung dengan logika IDENTIK dengan loop upsert — yaitu **set-difference by Id**: `existing.Id \ submittedNonNullIds` (plus Essay = SEMUA existing). Update komentar in-code `AAC:8022-8035` + `Models/OptionInput.cs` (cabut larangan T-418-06, ganti dengan kontrak validasi-server).

### Edit teks/kebenaran opsi terjawab (D-02)
- **D-02:** Mengedit teks dan/atau `IsCorrect` opsi yang SUDAH dijawab peserta **DIIZINKAN** (match by Id → jawaban peserta tetap merujuk opsi yang sama secara semantik di Results/grading/PDF). Memenuhi SC#3.
- **D-02a:** TIDAK ada gerbang baru untuk ini. Andalkan modal peringatan `affectedSessions` (D-09, sudah ada — GET `EditQuestion` JSON `:7901` + trigger client) yang muncul saat soal punya jawaban. Re-grading historis adalah tanggung jawab HC (peringatan sudah cukup). Ubah-kebenaran TIDAK diblok.

### Definisi "sudah dijawab" untuk guard (D-03)
- **D-03:** Guard answered-option menghitung **SEMUA** `PackageUserResponse` yang `PackageOptionId == opsi` (apa pun status sesi — in-progress/belum-submit + selesai/dinilai), konsisten dengan guard existing (`:8055-8059`). Fail-safe: makna jawaban manapun tak boleh berubah, apa pun status sesi. Tidak dipersempit.

### Pesan & pelabelan opsi terblok (D-04)
- **D-04:** Saat hapus opsi terjawab ditolak, label huruf A–F dihitung dari **urutan tersimpan** (`OrderBy(o => o.Id)` pada `q.Options`) opsi yang diblok + **sertakan cuplikan teks** opsi tsb (opsi terblok kemungkinan sudah HILANG dari form karena baru saja dihapus HC). Pertahankan tone pesan ramah existing (`:8071`).

### Claude's Discretion
- Bentuk persis cuplikan teks (panjang truncate) di pesan D-04.
- Apakah validasi anti-tamper D-01a dijalankan sebagai blok awal terpisah atau menyatu di loop upsert (selama hasilnya fail-closed sebelum SaveChanges).
- Reuse vs ekstrak helper untuk hitung set-difference `removedOptionIds` (boleh inline; OptionShrinkGuard.FindBlockedOptionIds tetap dipakai apa adanya).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Roadmap
- `.planning/REQUIREMENTS.md` — OPTEDIT-01..05 + VRF-01, konteks bug, Out of Scope, Future (data-repair historis).
- `.planning/ROADMAP.md` §"Phase 420" — Goal + 5 Success Criteria.

### Kode yang diubah (jalur bug)
- `Controllers/AssessmentAdminController.cs:7926-8160` — `EditQuestion` POST: guard edit-shrink D-418-02 (`:8022-8075`, hitung `removedOptionIds` posisional → ganti set-difference by Id) + loop upsert opsi (`:8126-8159`, posisional `existing[i]` → match by Id).
- `Controllers/AssessmentAdminController.cs:7876-7921` — `EditQuestion` GET (AJAX JSON): blok `options` (`:7908-7914`) **belum emit `o.Id`** → TAMBAH `id = o.Id` agar form bisa isi hidden OptionId.
- `Controllers/AssessmentAdminController.cs:7707` — `CreateQuestion` POST (regресi-check OPTEDIT-05: soal baru, semua opsi Id-null → ADD; harus tetap normal).
- `Models/OptionInput.cs` — tambah `int? Id`; revisi komentar T-418-06 (cabut larangan Id, tulis kontrak validasi-server).
- `Views/Admin/ManagePackageQuestions.cshtml` — form authoring: render hidden `options[i].Id` per baris (`:398-442` blok `#optionRows`), populate dari GET JSON saat edit (`:716` populate field), dan **JS reindex (`:763-828` `renumberOptionRows`/`addOptionRow`) HARUS mempertahankan hidden Id per baris** saat tambah/hapus baris (baris baru = Id kosong).

### Guard & validator (reuse, signature locked)
- `Helpers/OptionShrinkGuard.cs` — `FindBlockedOptionIds(removed, answered)` = set-intersection; **dipakai apa adanya**, hanya input `removedOptionIds` yang dihitung beda (by Id).
- `Helpers/QuestionOptionValidator.cs` (ValidateQuestionOptions) — validator min-2/max-6 + checked-correct ber-teks; tetap.
- `Data/ApplicationDbContext.cs:561-564` — FK `PackageUserResponse → PackageOption` = Restrict (sumber hazard 999.14; JANGAN diubah).

### Arsip Phase 418 (konteks keputusan yang direvisi)
- `.planning/milestones/v32.6-phases/418-opsi-jawaban-dinamis-2-6/418-RESEARCH.md` — Pattern 2 (positional upsert) yang kini diganti.
- `.planning/milestones/v32.6-phases/418-opsi-jawaban-dinamis-2-6/418-02-PLAN.md` — asal D-418-02 + guard edit-shrink.

### Workflow wajib
- `CLAUDE.md` + `docs/DEV_WORKFLOW.md` — verifikasi lokal (`dotnet build`/`run` @5277 + cek DB) sebelum commit; jangan push.
- `docs/SEED_WORKFLOW.md` — snapshot→seed→test→restore untuk UAT (VRF-01).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OptionShrinkGuard.FindBlockedOptionIds` — pure guard intersection; reuse, beri `removedOptionIds` versi set-difference-by-Id.
- `QuestionOptionValidator.ValidateQuestionOptions` — validasi opsi (min-2/max-6) sudah jalan sebelum guard; tetap.
- `ApplyOptionImageIntent(slot, image, alt, removeImage, packageId, imagePathsToDelete)` — apply gambar opsi per-record; tetap dipakai di cabang UPDATE/ADD.
- Modal `affectedSessions` (D-09) — sudah memperingatkan HC saat edit soal yang sudah dijawab; menutup kebutuhan D-02a tanpa kode baru.
- Pola pesan ramah `TempData["Error"] = "..."` + `RedirectToAction("ManagePackageQuestions", new { packageId })` — dipakai untuk semua penolakan (anti-tamper D-01a + blok D-04).

### Established Patterns
- Indexed model binding `options[i].Text/.IsCorrect/.Image/.ImageAlt/.RemoveImage` (OptionInput) — `Id` baru ikut pola: `options[i].Id`.
- Urutan opsi KONSISTEN `OrderBy(o => o.Id)` di GET JSON, guard, dan upsert — pertahankan untuk pelabelan huruf A–F (D-04).
- JS reindex baris (`renumberOptionRows`, `:770-828`) menulis ulang `name="options[i].*"` saat tambah/hapus baris — **wajib** ikut menangani hidden `options[i].Id`.

### Integration Points
- GET `EditQuestion` JSON → tambah `id` → dipakai client populate hidden Id saat buka modal edit.
- POST `EditQuestion` → terima `options[i].Id`, validasi ∈ q.Options (fail-closed), pakai untuk match upsert + hitung removedOptionIds set-difference.
- Guard D-418-02 → input `removedOptionIds` kini = existing.Id yang tak ada di submit (+ Essay=all); output diblok → pesan D-04 (huruf urutan-tersimpan + teks).
</code_context>

<specifics>
## Specific Ideas

- Mekanisme set-difference: `submittedIds = options.Where(o => o.Id.HasValue).Select(o => o.Id.Value)`. `removedOptionIds = existing.Select(Id) \ submittedIds` (untuk non-Essay; Essay = semua existing). Ini otomatis menangkap delete TENGAH (record yang Id-nya hilang dari submit) yang dulu lolos guard.
- Upsert match-by-Id: untuk tiap baris input dengan Id valid → cari `existing.First(e => e.Id == input.Id)` → UPDATE. Baris Id-null + teks → ADD. Existing tak ter-referensi → Remove (sudah lolos guard, aman FK-Restrict).
- Reproduksi RED (VRF-01): soal 4-opsi A,B,C,D; peserta jawab "B"; HC hapus B (form kirim A,C,D dengan Id masing-masing). Sebelum fix → record B di-relabel jadi "C" senyap. Sesudah fix → guard menyala (Id B ∈ answered, ∉ submit) → ditolak dgn pesan menyebut "B" + teksnya.
</specifics>

<deferred>
## Deferred Ideas

- **Reorder opsi (drag-to-reorder)** — identity-based membuka kemungkinan ini, tapi DI LUAR scope (roadmap tak menyebut; PackageOption tak punya kolom Order eksplisit). Milestone terpisah bila diminta.
- **Data-repair response historis** — perbaikan `PackageUserResponse` yang TERLANJUR ter-relabel sebelum fix. Defer s/d ada bukti korupsi nyata di Dev/Prod (REQUIREMENTS §Future). Butuh tooling audit + kemungkinan migration data.
- **Form Inject (`InjectAssessment`) opsi** — client-side membuat soal baru, tak lewat upsert EditQuestion; di luar jalur bug (REQUIREMENTS Out of Scope).

### Reviewed Todos (not folded)
None — tidak ada pending todo yang cocok scope Phase 420.
</deferred>

---

*Phase: 420-editquestion-identity-based-option-editing*
*Context gathered: 2026-06-25*
