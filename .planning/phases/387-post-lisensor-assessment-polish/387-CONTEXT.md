# Phase 387: Post-Lisensor Assessment Polish - Context

**Gathered:** 2026-06-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Bereskan **9 temuan readiness sisa** (PXF-06..14: 2 MED + 6 LOW + 1 MED same-file fold) dari register gladi-bersih E2E 2026-06-15 — kategori: correctness label/export, data-integrity exam-taking, a11y/monitoring. Tidak menghambat hari-H ujian lisensor; dikerjakan **PASCA-acara** sebagai deploy IT **kedua** (terpisah dari bundle urgent Phase 385-386).

**Depends on Phase 386** (PXF-06/08/09/10 menyentuh `Controllers/AssessmentAdminController.cs` yang sama → kerjakan setelah 386 selesai untuk hindari konflik write). **0 migration.** Satu fase sekuensial.

**OUT of scope:** F-01 (UX MA-warn — mitigasi briefing), F-18 (kondisional multi-paket), fitur baru / refactor besar / overhaul modul export.
</domain>

<decisions>
## Implementation Decisions

### PXF-06 (F-03) — Edit skor essay pasca-finalize
- **D-01:** **BLOCK + pesan jelas.** Bila HC memanggil `SubmitEssayScore` saat sesi sudah **terminal/finalized** (post-`FinalizeEssayGrading`, mis. `Status == "Completed"` / "Failed"), tolak dengan pesan jelas (mis. "Sesi sudah final, skor essay tidak dapat diubah"). TIDAK recompute, TIDAK re-issue cert. Alasan: lisensor high-stakes, sertifikat = bukti resmi yang tak boleh berubah diam; BLOCK = nol risiko divergen skor/cert + paling simpel + 0 migration.
- **D-01a (KRITIS — jangan rusak alur normal):** Guard HANYA aktif saat status **terminal pasca-finalize**. Saat window grading normal (sesi `PendingGrading` / sebelum `FinalizeEssayGrading`), `SubmitEssayScore` HARUS tetap jalan — itu jalur HC menilai essay yang sah. Salah menempatkan guard = membekukan grading normal. Planner/researcher WAJIB konfirmasi enum status pasca-finalize yang persis sebelum implement.

### Strictness data-integrity (PXF-12 F-20 + PXF-13 F-22)
- **D-02:** **Strict — mirror `SaveMultipleAnswer`.**
  - **PXF-13 (F-22):** `Hub.SaveTextAnswer` tambah guard timer-expired persis seperti `SaveMultipleAnswer:205-212` (tolak tulis essay bila timer sudah lewat, log warning). Konsisten antar tipe soal.
  - **PXF-12 (F-20):** `SubmitExam` upsert MC HANYA update jawaban untuk key yang ADA di form `answers`; jangan timpa jawaban tersimpan menjadi null untuk soal yang absent di form. Cegah data-loss laten.

### PXF-08 (F-06) — Nomor sertifikat gagal generate
- **D-03:** **Retry 3× + log + surface ke HC.** Match pola `GradingService` (retry-loop 3× dengan unique-index). Bila tetap gagal setelah retry → log error + kembalikan pesan ke HC (mis. "Nomor sertifikat gagal dibuat, coba lagi"). HC sadar; hapus catch silent yang bisa loloskan lulus tanpa nomor cert tanpa siapa pun tahu.

### Mekanis (locked oleh carry-forward, Claude's Discretion pada detail)
- **D-04 (PXF-07 F-02):** Excel "Detail Per Soal" label essay pakai helper kanonik `IsQuestionCorrect` (`>0`), bukan `EssayScore >= ScoreValue/2`. Konsisten dengan v30.0.
- **D-05 (PXF-14 F-DEV-02):** Di method/file yang SAMA (`ExcelExportHelper.cs:78-115`), nilai soal Multiple Answer dengan `SetEquals` (baca SEMUA opsi terpilih, bukan `FirstOrDefault` 1-baris). **Fold satu kali edit blok 78-115 bersama PXF-07.**
- **D-06 (PXF-09 F-19):** Excel BulkExport "Detail Jawaban" tampilkan skor/teks essay yang sudah dinilai (bukan selalu "—").
- **D-07 (PXF-10 F-13):** `FinalizeEssayGrading` broadcast ke monitor group — ikuti pola SignalR broadcast yang sudah ada di flow monitoring (Claude pilih event/group yang konsisten).
- **D-08 (PXF-11 F-11):** Gambar opsi Results/ExamSummary sertakan label huruf A/B/C/D pada `AriaContext` — derive huruf dari urutan/index opsi (Claude's discretion implementasi).

### Verifikasi
- **D-09:** **Proporsional (pasca-acara).** Unit test untuk yang ada logika scoring/label: **PXF-06, PXF-07, PXF-09, PXF-12, PXF-14**. `dotnet build` 0 error + `dotnet run` localhost:5277. **Playwright HANYA PXF-11** (a11y render butuh runtime Razor — pelajaran Phase 354). LOW lain (PXF-08/10/13) cukup unit/manual + build. Bukan full-e2e semua.

### Folded Todos
Tidak ada todo difold. (Match `2026-06-11-one-time-cleanup-data-test...` = false-positive keyword, tidak relevan → lihat Deferred.)
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Register temuan (sumber kebenaran PXF-06..14)
- `.planning/notes/2026-06-15-readiness-ujian-lisensor.md` — register final adversarial-verified; lihat "RE-CHECK adversarial 2026-06-15 (wf_cc0dd4b7) — REGISTER FINAL", tabel "Register final actionable", + catatan F-DEV-02. Tiap F-ID → lokasi file:line + deskripsi bug + fix yang disarankan.

### Requirements & roadmap
- `.planning/REQUIREMENTS.md` §"Post-Lisensor Polish (PXF-06..14)" — definisi REQ + traceability F-ID.
- `.planning/ROADMAP.md` §"Phase 387: Post-Lisensor Assessment Polish" — goal, files, success criteria, depends 386.

### Pola kode acuan (verified live session ini)
- `Controllers/AssessmentAdminController.cs` — `SubmitEssayScore` (~3525, tambah guard status, D-01), `FinalizeEssayGrading` (~3566+: cert nomor ~3697 retry D-03, broadcast monitor ~3753 D-07), BulkExport "Detail Jawaban" (~4797/4828, essay skor/teks D-06).
- `Helpers/ExcelExportHelper.cs:78-115` — blok "Detail Per Soal" (essay label ~111 D-04 + MA SetEquals ~83 D-05, satu edit).
- `Hubs/AssessmentHub.cs` — `SaveTextAnswer:134` (tambah guard, D-02) vs `SaveMultipleAnswer:188` guard timer di `:205-212` (POLA ACUAN).
- `Controllers/CMPController.cs:1712` — `SubmitExam` upsert MC (D-02 no null-overwrite).
- `Views/CMP/Results.cshtml:388` — `_QuestionImage` partial call `AriaContext="opsi"` (D-08 tambah huruf).
- Pola scoring kanonik: helper `IsQuestionCorrect` (essay `>0`) + MA `SetEquals` — v30.0 (lihat register "Fakta scoring terverifikasi").
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SaveMultipleAnswer:205-212` guard timer-expired = pola siap-tiru untuk `SaveTextAnswer` (PXF-13).
- `GradingService` retry-loop 3× + unique-index = pola siap-tiru untuk cert nomor essay (PXF-08).
- Helper `IsQuestionCorrect` (kanonik `>0`) + `SetEquals` MA = sudah dipakai jalur web/Excel-per-session; tinggal diterapkan ke surface Excel "Detail Per Soal" (PXF-07/14) & BulkExport (PXF-09).
- Pola SignalR broadcast monitoring sudah ada di flow lain (PXF-10 ikut pola).

### Established Patterns
- Status sesi: `Open` / `InProgress` / `Upcoming` / `Completed` (+ `PendingGrading` di alur essay). Guard PXF-06 harus bedakan terminal-finalized vs window-grading (D-01a).
- `ExcelExportHelper.cs:78-115` satu blok foreach soal → PXF-07 + PXF-14 satu kali edit (hindari dua pass file sama).

### Integration Points
- PXF-06/08/09/10 di `AssessmentAdminController.cs` → **depends Phase 386** (file-overlap, sekuensial setelah 386).
- 0 migration — semua controller/hub/helper/view/validasi.
</code_context>

<specifics>
## Specific Ideas

- PXF-06 BLOCK pakai pesan Bahasa Indonesia jelas ke HC; jangan diam.
- PXF-07 + PXF-14 = satu edit blok `ExcelExportHelper.cs:78-115` (jangan dipecah dua plan terpisah yang menulis blok sama).
- Verifikasi pasca-acara proporsional: jangan over-engineer e2e untuk LOW yang sudah ter-cover unit (D-09).
</specifics>

<deferred>
## Deferred Ideas

- **F-01** (UX MA-warn "jawaban sebagian = 0") — OUT, mitigasi briefing peserta; backend all-or-nothing sudah benar.
- **F-18** (export by-paket bukan ShuffledQuestionIds) — OUT kondisional; relevan hanya bila ujian >1 paket (mitigasi: pakai 1 paket).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` (area: database) — false-positive keyword match ("test/audit/phase"); tentang cleanup data test lokal pasca-Phase 367, tidak terkait polish assessment 387. Tidak difold.
</deferred>

---

*Phase: 387-post-lisensor-assessment-polish*
*Context gathered: 2026-06-15*
