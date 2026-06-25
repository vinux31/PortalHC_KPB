# Phase 424: Grading De-dup + Flow/Linking + Gating Pre→Post - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Rapikan integritas grading & alur Pre/Post tanpa mengubah hasil yang sudah ada:
1. **Gating Pre→Post (GRDF-01, HIGH/FLOW-04):** sisipkan gate pelaksanaan di `StartExam` — Post-Test tak boleh dimulai bila pasangan Pre belum `Completed`.
2. **Dedupe scoring satu sumber (GRDF-02 / GRD-09, GRD-02, GRD-03):** ekstrak skor per-soal (MC/MA/Essay) + Elapsed Time ke satu fungsi murni; strategi dedupe response seragam (last-write-wins) di semua jalur grading.
3. **Pairing satu sumber kebenaran (GRDF-03 / FLOW-01):** satukan tiga jalur pairing divergen (LinkedGroupId / UserId / LinkedSessionId) menjadi satu, terfilter per-peserta (UserId).
4. **Stop link semu Standard (GRDF-04 / FLOW-03):** assessment mode Standard tak lagi dapat link Pre/Post otomatis dari pola judul.
5. **ElapsedSeconds konsisten (GRDF-05 / FLOW-02):** perhitungan durasi aktif memperhitungkan `ExtraTimeMinutes` seragam (helper) — fix under-report export "Durasi Aktual".
6. **Validasi essay server-side (GRDF-07 / VAL-03):** submit on-time menolak essay kosong di server (bukan hanya client `flushEssay`).

**migration=FALSE** (refactor scoring/pairing + gate StartExam + validasi server; tidak ada schema/write DB baru).

**Bukan cakupan fase ini:**
- **GRDF-06 (manajemen peserta simetris / PA-07, PA-08) — DIBUANG dari 424.** Sudah dikerjakan PENUH oleh **v32.5 di branch `main`** (`RemoveParticipantCoreAsync` + `RemoveParticipantLive`/`RestoreParticipantLive`/`AddParticipantsLive`; fix dead-ref `DeleteAssessmentPeserta` persis di `AssessmentAdminController.cs:2719` komentar "D-04 fix stub mati"; simetri Pre/Post = PRMV-04). Garap di 424 = duplikasi + dijamin konflik merge. Ditunda penuh ke merge v32.5. Lihat `<deferred>`.
- Penamaan/dead-field cosmetic, tech-debt timing token/write-on-GET (Phase 425).
</domain>

<decisions>
## Implementation Decisions

### A. Gating Pre→Post (GRDF-01, HIGH — keputusan bisnis a)
- **D-01 (definisi "Pre selesai"):** Syarat = status Pre = **`Completed` saja** (sudah submit/dinilai), TANPA cek lulus/`IsPassed`. Pre = baseline; tak ada konsep lulus. Selaras "menyelesaikan Pre-Test" di keputusan bisnis (a).
- **D-02 (orphan/Standard lewat):** Gate **hanya aktif bila pasangan Pre untuk peserta itu MEMANG ADA** tapi belum `Completed`. Post tanpa pasangan Pre (berdiri sendiri / mode Standard) → **lewat** (tak diblok). Non-destruktif, hindari false-block/dead-end. Pairing pasangan Pre dipakai = hasil GRDF-03 (satu sumber, terfilter UserId).
- **D-03 (UX terblok):** Blok di `StartExam` mengikuti **pola gate existing** — `TempData["Error"] = "Selesaikan Pre-Test dulu sebelum mulai Post-Test."` → `RedirectToAction("Assessment")`. Konsisten dgn gate Upcoming/token/window/Abandoned yang sudah ada (`CMPController.cs:904-975`). TIDAK menambah perubahan view lobby (disable tombol) di fase ini.

### C. Validasi essay kosong server-side (GRDF-07 / VAL-03)
- **D-04 (timeout vs on-time):** Server menolak essay kosong **HANYA saat submit on-time** (waktu ujian masih ada). **Timeout / auto-submit → tetap finalize** walau essay kosong (essay kosong = pending/0), mempertahankan perilaku Phase 386 PXF-04 (hindari dead-end saat waktu habis).
- **D-05 (cakupan tolak):** Saat on-time + ada ≥1 essay kosong → **blokir SELURUH submit** (semua essay wajib terisi) + pesan ramah ("Isi semua jawaban essay dulu"). Cermin perilaku client `flushEssay` yang sudah ada; server jadi otoritas (client bisa gagal — referensi bug handler mati Phase 413).

### D. Dedupe scoring & link semu (GRDF-02 + GRDF-04)
- **D-06 (kanonik dedupe response):** Saat ada >1 response utk soal sama → pakai **last-write-wins** = response dgn `SubmittedAt` paling baru (jawaban FINAL peserta). Strategi ini SERAGAM di semua jalur (sekarang `GradeAndCompleteAsync` last-write-wins, `Aggregator` FirstOrDefault, `ComputeScoreAndET` `.First()` — drift ditutup ke last-write-wins). Berlaku konsisten utk MC, MA (set), Essay.
- **D-07 (paritas — sesi lama):** Unifikasi **TIDAK me-recompute** sesi yang sudah `Completed`. Hasil numerik harus identik dgn jalur dominan; dijaga **characterization test paritas** (fokus seleksi MC >1 response). Nilai & sertifikat lama AMAN (selaras filosofi forward-only Phase 423 D-06).
- **D-08 (link semu — forward-only):** Hentikan auto-deteksi Pre/Post dari pola judul utk assessment **Standard ke depan**. Baris Standard lama yg terlanjur ber-link semu **TIDAK disentuh** (non-destruktif). Tidak ada cleanup retroaktif.

### Claude's Discretion (teknis — tak perlu keputusan user)
- **GRDF-03 (pairing satu sumber):** mekanik penyatuan tiga jalur (LinkedGroupId/UserId/LinkedSessionId) → satu helper terfilter per-UserId; researcher/planner tentukan bentuk konkret. Kanonik: pasangan ditentukan via link eksplisit (LinkedGroupId/LinkedSessionId), BUKAN pola judul.
- **GRDF-05 (ElapsedSeconds):** ekstrak helper (mis. `AllowedExamSeconds`/`ActiveDurationSeconds`) yang konsisten memasukkan `ExtraTimeMinutes`; samakan clamp di `CMPController.cs:469` dgn situs lain (`:1175,1548,1626,4590-4592`); fix under-report export "Durasi Aktual" (`AssessmentAdminController.cs:4831`).
- Penempatan & nama kelas/fungsi murni baru (analog `CertIssuanceRules`/`SessionEditLockRules`/`ShuffleToggleRules` dari 422/423; testable real-SQL-free).
- Bentuk pesan ramah, format teks, dll. (selama non-destruktif & server-authoritative).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit & requirements
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` — sumber temuan GRDF:
  - **FLOW-04** (gating Pre→Post, HIGH, keputusan bisnis a) — §5.1-flow + detail `:422`; StartExam tak gate Pre-Completed (`CMPController.cs:904-974`)
  - **GRD-09** (scoring duplikat 3+ tempat) — detail §5.2.2 + `:236`, `:343`
  - **GRD-02** (drift dedupe MC last-write-wins vs FirstOrDefault vs .First) — `:402`
  - **GRD-03** (strategi seleksi opsi MC inkonsisten 4-path) — `:404`
  - **FLOW-01** (pairing 3 jalur LinkedGroupId/UserId/LinkedSessionId) — `:358`
  - **FLOW-03** (Post tanpa Pre + link semu judul ke Standard) — `:362`
  - **FLOW-02** (clamp ElapsedSeconds tanpa ExtraTimeMinutes → export under-report) — `:428`
  - **VAL-03** (essay kosong hanya diblok client `flushEssay`) — `:376`
  - PA-07/PA-08 (manajemen peserta) — `:442`, `:444` — **DEFERRED ke v32.5, bukan scope 424**
- `.planning/REQUIREMENTS.md` §GRDF — GRDF-01..07 (definisi acceptance). ⚠️ GRDF-06 perlu di-rekonsiliasi (covered by v32.5; lihat deferred).
- `.planning/ROADMAP.md` §"Phase 424" — goal + 4 Success Criteria.

### Prior phase (pola yang diikuti)
- `.planning/phases/423-certificate-issuance-consistency/423-CONTEXT.md` — pola pure-helper + guard server-side + test real-SQL + **forward-only non-destruktif** (carry-forward D-06/D-07).
- `.planning/phases/422-samepackage-shuffle-integrity/422-CONTEXT.md` — pola ekstrak pure-rules + test paritas.

### Cross-branch (koordinasi merge — JANGAN duplikasi)
- branch `main` `Controllers/AssessmentAdminController.cs:2358-2880` (v32.5) — `AddParticipantsLive` / `RemoveParticipantCoreAsync` / `RemoveParticipantLive` / `RestoreParticipantLive` / `DeleteAssessmentPeserta`. Ini yang menutup GRDF-06; fase 424 TIDAK menyentuh area ini.

[Tidak ada ADR eksternal — aturan bisnis tercakup di audit + decisions di atas.]
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Pola pure-helper dari 422/423: `Helpers/CertIssuanceRules.cs`, `Helpers/SessionEditLockRules.cs`, `Helpers/ShuffleToggleRules.cs` + fixture test real-SQL (`IClassFixture`, `NoOpHubContext`, ctor recipe `GradingService`).
- `Services/AssessmentScoreAggregator.cs` `IsQuestionCorrect` (essay Benar=`EssayScore>0`, null=pending; helper correctness dari v30.0/383) — basis untuk fungsi murni per-soal.
- `GradeAndCompleteAsync` sudah pakai dedupe `OrderByDescending(SubmittedAt)` last-write-wins (`GradingService.cs:87-90`) = strategi kanonik yang dipilih (D-06).

### Established Patterns
- Rantai gate `StartExam` (`CMPController.cs:904-975`): `TempData["Error"]=...; return RedirectToAction("Assessment")` untuk Upcoming/Completed/token/window/duration/Abandoned → gate Pre-Completed disisipkan pola sama (setelah cek `Completed`, sebelum/sekitar gate token).
- Filosofi forward-only non-destruktif (Phase 421/423): tak utak-atik baris lama / sesi Completed.

### Integration Points
- `Services/GradingService.cs` — `GradeAndCompleteAsync` (switch inline `:103-142` + ET `:158-182`), `ComputeScoreAndETInternalAsync` (`:384-403` + `:418-435`, `mcSel.First` `:390`), dedupe `:87-90`.
- `Services/AssessmentScoreAggregator.cs` — `Compute` (`:33-57`, `FirstOrDefault` `:39` = drift); komentar `:13-17` klaim "single source of truth" FAKTUAL KELIRU (hanya dipakai jalur essay-finalize). Call-site di `AssessmentAdminController.cs:3762-3768, 4187-4199` feed seluruh response tanpa dedupe.
- `Controllers/CMPController.cs` — `StartExam` (`:904-975`, sisip gate Pre-Completed); `UpdateSessionProgress` clamp `:469` (tanpa ExtraTime) vs `:1175,1548,1626,4590-4592` (dengan); submit essay (`SubmitExam`/`SaveTextAnswer` — titik validasi server essay kosong); pairing Pre/Post sibling.
- `Controllers/AssessmentAdminController.cs` — FLOW-04 validasi jadwal `:1067-1068,1825-1829,1861-1880`; export "Durasi Aktual" `:4831` (FLOW-02 under-report).
- Pairing 3 jalur: `LinkedGroupId` / `UserId` / `LinkedSessionId` — satukan, filter per UserId (GRDF-03).

### New (dibuat di fase ini)
- Fungsi murni skor per-soal (MC/MA/Essay) + ET, dedupe last-write-wins — single source, wire ke semua jalur grading (GRDF-02).
- Helper durasi aktif konsisten dgn `ExtraTimeMinutes` (GRDF-05).
- Helper/util pairing Pre/Post satu sumber terfilter UserId (GRDF-03) — sekaligus stop link-semu judul utk Standard (GRDF-04).
- Gate Pre-Completed di `StartExam` (GRDF-01).
- Validasi server-side essay kosong on-time (GRDF-07).
</code_context>

<specifics>
## Specific Ideas

- Gate Pre-Completed harus pakai pairing yang SAMA dgn GRDF-03 (link eksplisit, bukan pola judul) — supaya orphan/Standard (D-02) tak salah-gate.
- Strategi kanonik dedupe = last-write-wins (`SubmittedAt` terbaru) sudah jadi perilaku jalur grading utama → menyatukan ke arah itu = perubahan paling kecil + paritas terjaga.
- Validasi essay server = otoritas; client `flushEssay` tetap ada sebagai UX cepat tapi tak boleh jadi satu-satunya penjaga (lesson Phase 413 handler mati).
</specifics>

<deferred>
## Deferred Ideas

- **GRDF-06 (manajemen peserta simetris — PA-07 + PA-08):** DIBUANG dari fase 424. Sudah diselesaikan penuh oleh **v32.5 (branch `main`, fase 409-413)**: `RemoveParticipantCoreAsync` shared + wrapper JSON/redirect (incl `DeleteAssessmentPeserta` yang persis menutup dead-ref audit) + simetri Pre/Post (PRMV-04) + dedup add-flow. **Akan otomatis terpenuhi saat merge bundle deploy.** ⚠️ Action item: rekonsiliasi `.planning/REQUIREMENTS.md` + `.planning/ROADMAP.md` Success Criteria #4 fase 424 → tandai GRDF-06 "covered by v32.5 merge" (bukan dikerjakan di 424). Surface di milestone audit.
- Cleanup retroaktif link semu Standard lama (D-08 menolak ini) — bila analytics historis terganggu, bisa jadi item backlog terpisah (butuh identifikasi hati-hati mana yg semu).
- Tech-debt timing (timer satu sumber, token server-authoritative, write-on-GET StartExam) — Phase 425 (FLOW-08/09/10, CLN-04).

None lain — diskusi tetap dalam scope fase (selain GRDF-06 yang sengaja didepak ke v32.5).
</deferred>

---

*Phase: 424-grading-de-dup-flow-linking-gating-pre-post*
*Context gathered: 2026-06-24*
