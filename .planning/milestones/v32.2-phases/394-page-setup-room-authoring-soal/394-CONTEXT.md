# Phase 394: Page + Setup Room + authoring soal - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Bangun UI inject manual: page baru `/Admin/InjectAssessment` (`Controllers/InjectAssessmentController.cs` BARU, RBAC `Admin,HC`) + kartu di `Views/Admin/Index.cshtml` Section C + view wizard `Views/Admin/InjectAssessment.cshtml` (BARU) + `ViewModels/InjectAssessmentViewModel.cs` (BARU). Page = **wizard multi-langkah mirror `CreateAssessment`** yang memungkinkan HC/Admin: atur setting room (mirror CreateAssessment, backdate), tulis soal inline (reuse authoring `ManagePackages`), pilih pekerja penerima (reuse picker CreateAssessment), dan pilih mode sertifikat (toggle 3-mode) — semua dalam satu alur tanpa akses DB.

**Scope-lock:** Cakupan REQ INJ-03..07. **Jalur FORM/tulis-manual saja** (Import Excel = Phase 396; jawaban per-pekerja + auto-generate = Phase 395; link Pre/Post = Phase 397). **0 migration** (tak ada write DB / schema baru — semua data 394 ditahan di state form sampai commit final). Page 394 = kerangka wizard langkah 1-4 fungsional + placeholder langkah Jawaban (diisi 395) + Konfirmasi; **commit inject aktual belum terjadi di 394** (belum ada jawaban → `InjectAssessmentService` [393] dipanggil setelah 395). Reuse mesin existing — nol duplikasi authoring/grading.

</domain>

<decisions>
## Implementation Decisions

### Struktur & alur page
- **D-01:** Page = **wizard multi-langkah mirror `CreateAssessment`** — `nav-pills` (`#wizardStepNav`) + `step-panel` + **satu `<form>`** membungkus semua langkah; reuse pola JS `showStep()`/pill-state (`CreateAssessment.cshtml:889+`). Bukan single-page-scroll, bukan accordion.
- **D-02:** Urutan **6 langkah**: **1. Setup Room → 2. Pilih Pekerja → 3. Authoring Soal → 4. Sertifikat → 5. [Jawaban per-pekerja] → 6. Konfirmasi**. Pekerja **sebelum** Soal (pilih audience dulu, baru tulis soal). **Langkah 5 (Jawaban) = disisipkan Phase 395** — di 394 cukup placeholder/kerangka + navigasi; jawaban diisi 395. Langkah 3 (Soal) & 2 (Pekerja) wajib sebelum langkah 5 (jawaban butuh soal + pekerja).

### Setup Room (INJ-04)
- **D-03:** Field mirror `CreateAssessment`: judul, kategori, tipe (`Standard`/`PreTest`/`PostTest`), jadwal/`CompletedAt` **backdate ≤ hari ini** (carry D-06 dari 393), durasi, `PassPercentage`, `AllowAnswerReview` (default `true`, carry 393 Claude-discretion agar rincian per-soal tampil di `/CMP/Results`). Sertakan **tombol "Cek" judul** reuse `GET /Admin/CheckTitleAvailability` (carry D-02 dari 393 — tombol = UI 394).

### Authoring soal (INJ-05)
- **D-04:** **Inline authoring SAJA** (tulis soal baru di dalam flow) — TIDAK ada opsi pilih/clone paket existing (clone = deferred). Komponen authoring identik `ManagePackages`/`ManagePackageQuestions`: tipe MC/MA/Essay + opsi + `IsCorrect` + `ScoreValue` + `ElemenTeknis` + `Rubrik`. **Catatan reuse:** authoring asli = page terpisah `/Admin/ManagePackageQuestions?packageId=X` (terikat `packageId` existing), BUKAN partial inline → mekanisme embedding (extract shared partial vs replikasi inline) = Claude/researcher discretion; **wajib nol-duplikasi semantik** (field & validasi soal identik ManagePackages). Paket draft dibentuk di flow (state form), tak commit DB sampai final.

### Worker picker (INJ-06)
- **D-05:** **Reuse picker `CreateAssessment` step-2 apa-adanya**: filter Section (org) + **search "Cari nama atau email…"** (`#userSearchInput`) + Pilih Semua/Batalkan Semua + checkbox list (`#userCheckboxContainer`, `name="UserIds"`) + panel "Peserta Terpilih" (`CreateAssessment.cshtml:271-349`). **NIP tak dikenal mustahil by-construction** — picker hanya menampilkan `AspNetUsers` existing → D-03 (393, "NIP wajib ada") terpenuhi otomatis tanpa kode tolak khusus. Import Excel batch = **Phase 396** (jalur kedua paralel-fungsi), BUKAN 394.

### Sertifikat (INJ-07)
- **D-06:** UX toggle = **radio 3-mode** (Auto / Manual / Tanpa) + field kondisional:
  - **Auto** → tampil **preview format** `KPB/xxx/{ROMAN}/{year}` (tahun = backdate `CompletedAt`, carry D-12 393) + field `ValidUntil` + checkbox **Permanent**.
  - **Manual** → input `NomorSertifikat` (wajib **unik**, carry D-09 393) + field `ValidUntil` + checkbox **Permanent**.
  - **Tanpa** → tak ada field.
  - `ValidUntil` `null` = permanent (carry D-10 393), berlaku mode Auto **dan** Manual. Perilaku grading (suppress cert bila tak lulus, carry D-08 393) di-handle pipeline 393 — UI cukup kirim pilihan.

### Persistence & handoff ke Phase 395
- **D-07:** Semua data 394 **ditahan di state form/session** (hidden fields / view-model serialized) sampai **commit final** — **tak ada write DB / draft DB di 394**. Patuh 0-migration. End-state 394 = page wizard langkah 1-4 fungsional + data tertangkap di form + kerangka langkah 5 (Jawaban) & 6 (Konfirmasi). **Commit inject aktual** (panggil `InjectAssessmentService` 393) terjadi **setelah 395** mengisi jawaban — 394 belum meng-inject (belum ada jawaban). Tidak ada fitur "simpan draft & lanjut nanti".

### Claude's Discretion (teknis — diserahkan ke researcher/planner)
- **Mekanisme reuse authoring soal:** extract shared partial dari `ManagePackageQuestions` vs replikasi markup+JS authoring inline vs embed. Pilihan = planner; **wajib field/validasi soal identik** ManagePackages (nol-duplikasi semantik).
- **Bentuk holding form-state:** hidden JSON field vs view-model bound vs JS in-memory model serialize-on-submit. Discretion.
- **Kontrak controller→`InjectAssessmentService`:** ikut signature/DTO dari Phase 393 (`packageSpec/authored questions` + `room settings` + worker list); jawaban menyusul 395.
- Penamaan/ikon tiap langkah, styling kartu, copy notice, debounce tombol Cek-judul, wiring DI controller.
- Penanganan langkah 5 placeholder (disabled pill vs panel kosong "diisi tahap berikut") — selaras agar 395 menyisipkan tanpa refactor besar.

### Folded Todos
[Tak ada todo yang di-fold.]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — design spec penuh (alur end-to-end §5, mode jawaban §7, sertifikat §8, audit §10, risiko §13).
- `.planning/REQUIREMENTS.md` — INJ-03 (page + RBAC Admin,HC + Section C), INJ-04 (setup room mirror CreateAssessment + backdate), INJ-05 (authoring soal reuse ManagePackages), INJ-06 (worker picker, NIP wajib ada), INJ-07 (toggle sertifikat 3-mode).
- `.planning/ROADMAP.md` — Phase 394 details + 6 Success Criteria + UI hint:yes.
- `.planning/phases/393-backend-core-inject/393-CONTEXT.md` — **keputusan carry-forward** D-01..D-12 (dedup, atomic, essay, backdate ≤today D-06, cert suppress D-08 / manual-unik D-09 / ValidUntil-permanent D-10 / auto-tahun-backdate D-12) + kontrak `InjectAssessmentService`.

### Pola UI di-reuse (mirror / scaffold)
- `Views/Admin/CreateAssessment.cshtml` — **scaffold wizard**: `nav-pills #wizardStepNav` (:77), step-panel (:119/:271/:370/:639), JS `showStep`/pill-state (:889+); **worker picker step-2** (:271-349: filter Section + search `#userSearchInput` + checkbox `#userCheckboxContainer name="UserIds"` + panel "Peserta Terpilih"); kartu **Sertifikat** di Settings (:558-605); **Jadwal & Waktu** (:377+); Konfirmasi step-4 (:639+).
- `Views/Admin/ManagePackages.cshtml` (340 baris) + action **`ManagePackageQuestions`** (link `:273`, `asp-route-packageId`) — authoring soal asli (page terpisah terikat packageId). Backend authoring: `Controllers/AssessmentAdminController.cs:~5641+` (researcher verifikasi line + bentuk partial) + `Models/AssessmentPackage.cs` (struktur Question/Option/ElemenTeknis/Rubrik).
- `Controllers/AssessmentAdminController.cs` — `GET /Admin/CheckTitleAvailability` (:846) + `NormalizeTitleForDup`/`FindTitleDuplicatesAsync` (`AdminBaseController.cs:276-293`) untuk tombol "Cek".
- `Views/Admin/Index.cshtml` — kartu Kelola Data Section C (tempat sisip kartu inject baru).

### Service & helper di-konsumsi
- `Services/InjectAssessmentService.cs` (BARU Phase 393) — controller 394 wiring DI + panggil (commit setelah 395). Signature/DTO dari 393.
- `Helpers/CertNumberHelper.cs` — `Build(seq, date)` → `KPB/{seq:D3}/{ROMAN}/{year}` (basis preview format mode Auto).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Wizard scaffold** `CreateAssessment.cshtml` (nav-pills + step-panel + single-form + `showStep` JS) → kerangka 6-langkah inject.
- **Worker picker** `CreateAssessment` step-2 (search nama/email + filter Section + checkbox + panel terpilih) → reuse apa-adanya; hanya tampil user existing (D-05).
- **Authoring soal** `ManagePackageQuestions` (page terpisah) + `AssessmentAdminController:~5641+` → komponen tulis MC/MA/Essay (mekanisme embed = discretion).
- **`CheckTitleAvailability`** (GET :846) → tombol "Cek" judul.
- **`CertNumberHelper.Build`** → preview format cert mode Auto.
- **`InjectAssessmentService`** (393) → orchestrator yang dipanggil controller (commit fase berikut).

### Established Patterns
- Wizard: `nav-pills #wizardStepNav` + `.step-panel` (`d-none` toggle) + 1 `<form>` + JS `showStep(n)` + edit-from-confirm.
- Picker: filter dropdown + search input + `Pilih/Batalkan Semua` + checkbox `name="UserIds"` + live "Peserta Terpilih" (aria-live).
- Sertifikat card: di Settings CreateAssessment (kita pisah jadi langkah-4 sendiri, D-02) — radio + field kondisional (D-06).
- Razor + JS runtime-driven → **Playwright wajib** (pelajaran Phase 354): grep+build tak cukup untuk verifikasi wizard/picker/toggle.

### Integration Points
- `Views/Admin/Index.cshtml` Section C → kartu → route `/Admin/InjectAssessment` (`InjectAssessmentController` BARU, `[Authorize(Roles="Admin,HC")]`).
- Controller 394 → DI `InjectAssessmentService` (393) — commit jawaban di 395.
- **File-overlap SEQUENTIAL:** `InjectAssessmentController.cs` + `InjectAssessment.cshtml` + `InjectAssessmentService.cs` di-extend Phase **395** (jawaban/auto-gen, sisip langkah 5), **396** (Import Excel — jalur kedua picker/import), **397** (link Pre/Post — picker room saat tipe Pre/Post). 395/396/397 jalan **setelah** 394, tidak paralel (hindari merge-clobber).

</code_context>

<specifics>
## Specific Ideas

- User (klarifikasi worker picker): *"saya ingin ada dua fasilitas import excel atau tulis manual. phase 394 ini khusus yang ngisi form ya?, untuk excel import di phase 396 ya?. saya ingin formnya itu kayak create assessment, ada search dan org-tree picker (checkbox)"* → **dikonfirmasi**: 394 = jalur form/tulis-manual; Import Excel = Phase 396 (jalur kedua); picker = gaya CreateAssessment (search + filter org + checkbox).
- Wizard ditata **Pekerja sebelum Soal** (pilih audience dulu, baru authoring) — pilihan eksplisit user.
- Sertifikat = **langkah wizard tersendiri** (bukan digabung Settings) — menonjolkan pilihan 3-mode per-room.
- Prinsip menyeluruh (carry 393): hasil inject byte-identik online via reuse mesin — UI cukup menangkap input, jangan hitung skor/cert sendiri.

</specifics>

<deferred>
## Deferred Ideas

- **Clone/pilih paket soal existing** sebagai basis authoring — ditolak 394 (inline-only, D-04). Bila butuh reuse bank-soal → milestone berikut.
- **Draft tersimpan DB** (tutup & lanjut nanti) — ditolak (butuh store/state baru → risiko migration; milestone dipatok 0-migration, D-07).
- **Import Excel** = Phase 396. **Jawaban per-pekerja + auto-generate** = Phase 395 (langkah 5 wizard). **Link Pre/Post ke room existing** = Phase 397 (picker room saat tipe Pre/Post).
- **Single-page-scroll / accordion** layout — ditolak demi mirror CreateAssessment (D-01).
- **Toggle cert dropdown / switch-2-tingkat** — ditolak; radio 3-mode (D-06).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` ("One-time cleanup data test/audit lokal setelah Phase 367") — **false-positive** keyword match (data/controllers); tugas cleanup DB lokal, bukan scope page inject 394. Tidak di-fold.

</deferred>

---

*Phase: 394-page-setup-room-authoring-soal*
*Context gathered: 2026-06-17*
