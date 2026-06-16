# Phase 385: Exam-Taking & Image Render Hotfix - Context

**Gathered:** 2026-06-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaiki 2 bug yang muncul **saat ujian berlangsung** untuk ujian lisensor (~2026-06-17):
- **PXF-01 (F-09):** gambar soal & pilihan **tampil benar** saat aplikasi jalan di sub-path `/KPB-PortalHC` (Dev/Prod) — tidak 404.
- **PXF-03 (F-21):** jawaban **essay tersimpan utuh** saat submit / pindah halaman / waktu habis — keystroke terakhir tidak hilang, peserta yang sudah mengisi tidak ditolak submit.

File-disjoint dari Phase 386 (`AssessmentAdminController.cs`). **0 migration.** OUT: validasi opsi (386), PDF export (386), semua temuan Future.
</domain>

<decisions>
## Implementation Decisions

### F-09 — Gambar PathBase (PXF-01)
- **D-01 (lokasi fix = render-time):** Perbaiki di `Views/Shared/_QuestionImage.cshtml` — bungkus **`src` (L38)** DAN **`data-img-src` (L45, lightbox)** dengan `Url.Content("~" + imagePath)` agar PathBase-aware. **BUKAN** storage-time (`FileUploadHelper`) — itu butuh backfill baris DB lama. **BUKAN** `<base href>` — risiko efek samping. **Re-verified 2026-06-15:** path tersimpan selalu leading-slash `/uploads/..` (`FileUploadHelper.cs:107`), jadi `Url.Content("~" + "/uploads/..")` = `Url.Content("~/uploads/..")` → benar; tak ada risiko dobel-prefix (request 404 membuktikan path bare tanpa prefix). 1 file = cover SEMUA surface (StartExam/ExamSummary/Results/grading) + lightbox sekaligus, **tanpa ubah data**.
- **D-01a (defensive):** tambah guard kecil agar robust bila suatu path tak diawali `/` (prepend `/`), tapi tetap satu helper inline di partial. Jangan double-resolve bila sudah diawali `~`.

### F-21 — Essay tersimpan utuh (PXF-03)
- **D-02 (mekanisme = flush-on-submit + save-on-blur):** Flush timer essay pending + **await save SEBELUM** form dikirim / pindah halaman, PLUS tambah handler **`blur`** pada textarea essay (simpan langsung saat keluar field). Tetap pertahankan debounce 2s untuk autosave normal. (BUKAN cuma masukkan essay ke form POST — pilihan JS-side dipilih; kalau planner lihat perlu jaring server, boleh tambah, tapi bukan keharusan.)
- **D-03 (jalur timeout = best-effort, non-blocking):** Saat timer habis (`StartExam.cshtml:472` auto-submit), **tembak save essay lalu submit — JANGAN blokir/menunda deadline**. WAJIB pastikan **baris response essay sudah ada** (atau dibuat) sehingga gate incomplete (`CMPController` SubmitExam) tidak menolak palsu "soal belum dijawab" untuk peserta yang sudah mengetik. Terima bahwa cutoff 0-detik mungkin tetap kehilangan keystroke paling akhir (best-effort, bukan jaminan transaksional).
- **D-04 (pre-submit wait):** sertakan jalur save essay ke dalam guard pre-submit yang sekarang hanya menunggu MC (`StartExam.cshtml:980-1001 pendingSaves/inFlightSaves`) — sehingga tombol Review/Submit & pindah-halaman juga menunggu essay ter-flush.

### Verifikasi (D-05)
- F-09: verify LOKAL via URL **ber-prefix** `http://localhost:5277/KPB-PortalHC/CMP/...` (bukan bare) + Playwright assert gambar `naturalWidth>0` / network 200 (bukan cuma regex src). Bare localhost TIDAK reproduce.
- F-21: Playwright — ketik essay → submit (manual) → assert jawaban tersimpan di DB/Results; test reject-palsu (sudah ketik tapi belum debounce → tetap bisa submit).
- **UAT final di Dev** (`http://10.55.3.3/KPB-PortalHC`) diserahkan ke kamu/IT setelah re-deploy (CLAUDE.md: tak ada edit langsung di Dev).

### Claude's Discretion
- Detail implementasi JS flush (Promise.all pending essay saves vs SignalR invoke await), helper inline path-resolve, struktur unit/Playwright test → planner/executor.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Register temuan + root cause (WAJIB baca)
- `.planning/notes/2026-06-15-readiness-ujian-lisensor.md` — register final adversarial-verified; detail F-09 (PathBase) + F-21 (essay debounce) dengan file:line + bukti Dev (404).

### Requirement
- `.planning/REQUIREMENTS.md` — PXF-01, PXF-03 (acceptance).

### File yang disentuh fase ini
- `Views/Shared/_QuestionImage.cshtml` (L38 `src`, L45 `data-img-src`) — fix F-09.
- `Views/CMP/StartExam.cshtml` (L903-911 essay debounce, L472-484 timer auto-submit, L980-1001 pre-submit MC-only wait) — fix F-21.
- `Helpers/FileUploadHelper.cs:107` — sumber path leading-slash (referensi, TIDAK diubah).
- `Controllers/CMPController.cs` (SubmitExam incomplete-gate L1627-1653, ExamSummary autoSubmitToken) — referensi gate; ubah hanya bila perlu pastikan baris essay (D-03).
- `Hubs/AssessmentHub.cs` (SaveTextAnswer L134-182) — endpoint simpan essay (referensi).

### Konteks env (PathBase)
- `appsettings.json:9` PathBase `/KPB-PortalHC` (tak di-override Development); `Program.cs:195-198` `UsePathBase`.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_QuestionImage.cshtml` = single-source partial gambar (6 surface). Fix di sini otomatis menyebar.
- `Url.Content("~"+path)` (Razor `IUrlHelper`) = mekanisme PathBase-aware standar; logo cert (`Certificate.cshtml`) sudah pakai `~/` → preseden aman.

### Established Patterns
- Path upload selalu leading-slash `/uploads/..` (`FileUploadHelper.cs:107`). Form/nav pakai tag-helper/`@Url.Action` (PathBase-aware) — hanya `<img src>` mentah yang putus.
- Autosave exam = SignalR (`SaveTextAnswer`/`SaveAnswer`/`SaveMultipleAnswer`) + debounce; pre-submit guard pakai `pendingSaves`/`inFlightSaves` (saat ini MC-only).

### Integration Points
- Timer auto-submit `examForm.submit()` (StartExam.cshtml:472) — titik sisip flush essay.
- Incomplete-gate SubmitExam (CMPController:1627) skip saat `serverTimerExpired` — pastikan baris essay agar manual-submit tak tolak palsu.
</code_context>

<specifics>
## Specific Ideas

- F-09 CONFIRMED HARD di Dev 2026-06-15 (browser): `GET http://10.55.3.3/uploads/questions/10/...jpg → 404`, prefix `/KPB-PortalHC` hilang; resource lain (css/js/signalr) ber-prefix → 200. Screenshot ikon rusak.
- F-21: lisensor pakai essay → data-loss nyata; prioritas peserta tidak kehilangan jawaban + tidak ditolak submit menit akhir.
</specifics>

<deferred>
## Deferred Ideas

- **F-22** (Hub.SaveTextAnswer tanpa guard timer-expired) — terkait essay tapi severity LOW, Future pasca-acara. JANGAN tambahkan di fase ini kecuali trivial saat menyentuh SaveTextAnswer.
- **F-20** (SubmitExam MC upsert null-overwrite laten) — Future.
- Reduce/hapus debounce global, refactor save-engine — Future (over-scope untuk hotfix).
</deferred>

---

*Phase: 385-exam-taking-image-render-hotfix*
*Context gathered: 2026-06-15*
