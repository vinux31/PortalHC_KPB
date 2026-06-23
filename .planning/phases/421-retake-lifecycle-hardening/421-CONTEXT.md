# Phase 421: Retake Lifecycle Hardening - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Tutup 5 lubang lifecycle ujian-ulang (retake) atas mesin retake v32.4 yang sudah ada — **bukan fitur baru**, murni hardening integritas data & perilaku. Cakupan: gate window sebelum destruksi retake (anti dead-end), pencabutan NomorSertifikat saat HC reset sesi lulus, konsistensi penghitungan percobaan (cap vs warning), guard hapus peserta untuk sesi Abandoned/ber-riwayat + cleanup arsip, dan peringatan saat MaxAttempts diturunkan di bawah pemakaian.

**In-scope:** RTH-01..05. Domain kode: `Helpers/RetakeRules.cs`, `RetakeService` (`RetakeService.cs`), `CMPController.RetakeExam`/`StartExam`, `AssessmentAdminController` (`ResetAssessment` ~`:4286`, `UpdateRetakeSettings` ~`:5654`, guard hapus peserta EditAssessment POST ~`:1896-1934`, warning count ~`:5798`), `ManagePackages.cshtml`. **migration=FALSE** (semua perubahan logika + binding + view; tak ada schema baru).

**Out-of-scope (fase lain):** persistensi/UX form retake (sudah di **420** FORM); SamePackage/shuffle (422); aturan terbit cert end-to-end (423); grading/gating Pre→Post (424); cosmetic/naming (425). Tidak menambah strategi grading retake baru (highest/avg), cooldown escalating, atau rotasi token per-attempt (YAGNI v32.4).
</domain>

<decisions>
## Implementation Decisions

### RTH-01 — Cooldown lewat ExamWindowCloseDate = dead-end destruktif (RTK-LOGIC-02, HIGH)
- **D-01 (gate dua lapis):** Pasang gate window di **eligibility DAN eksekusi**.
  - **Eligibility:** `RetakeRules.CanRetake` menerima parameter `examWindowCloseDate` (+ konvensi waktu +7h WIB sama dgn `StartExam:~956`) → return `false` bila window sudah tutup. Efek: tombol "Ujian Ulang" otomatis disembunyikan/disabled & tier-feedback (`ResolveReviewMode`) konsisten — pekerja **tak pernah masuk jalur destruktif**.
  - **Eksekusi (defense-in-depth):** `RetakeService.ExecuteAsync` **abort SEBELUM** `RemoveRange`/`ExecuteUpdateAsync` (destruksi responses/assignment/ET + null skor/cert/status) bila window tutup. Cegah race antara eligibility-check dan klaim atomik.
- **D-02 (peringatan dini HC):** Di `UpdateRetakeSettings` (~`:5654`), bila `RetakeCooldownHours` bisa mendorong eligibility lewat sisa window sampai `ExamWindowCloseDate`, tampilkan **warning non-blocking** ke HC ("cooldown bisa melewati batas tutup ujian"). Setelan tetap tersimpan (non-blocking), hanya memperingatkan.

### RTH-02 — HC reset sesi LULUS tidak menghapus NomorSertifikat (RTK-LOGIC-01)
- **D-03 (nol-kan cert):** Tambah `.SetProperty(r => r.NomorSertifikat, (string?)null)` pada `ExecuteUpdateAsync` di jalur reset HC (`RetakeService.cs:~103-112`) **dan** pastikan `ResetAssessment` (`AssessmentAdminController.cs:~4286`) menol-kan NomorSertifikat juga. Hilangkan nomor menggantung yang mengisi unique index + meng-inflasi `certCount` proxy (`NomorSertifikat!=null`). Jalur worker tetap aman (sudah blok sesi lulus via `RetakeRules.cs:45`).
- **D-04 (konfirmasi cabut cert):** Sebelum HC reset sesi yang **sudah LULUS / ber-NomorSertifikat**, peringatkan + minta **konfirmasi** bahwa sertifikat akan dicabut. Konsisten dgn pola confirm-before RTH-04/05. Cegah pencabutan sertifikat tak sengaja (terutama saat reset massal). Pencabutan/nol-kan tetap dieksekusi server-side; konfirmasi = lapis UX di titik aksi `ResetAssessment`.

### RTH-03 — Counting attempt divergen: cap (snapshot-presence) vs warning (RTK-LOGIC-03)
- **D-05 (samakan counting):** Tambahkan predikat **snapshot-presence** yang sama (dipakai cap di `RetakeService`/`CMPController.cs:~2472-2475`) ke `Where` sebelum `GroupBy` di warning ManagePackages (`AssessmentAdminController.cs:~5757-5798`, konsumen `ManagePackages.cshtml:157-163`). Ekstrak helper bersama **`CountEraRetakeArchives`** dan wire ke keempat situs hitung agar tak divergen lagi (pola kill-drift mirip `RetakeRules`). Mekanisme & nama final = diskresi Claude asal satu sumber kebenaran.

### RTH-04 — Guard hapus peserta abaikan Abandoned/ber-riwayat + orphan arsip (PA-06)
- **D-06 (soft-confirm + cleanup, server round-trip):** Perluas guard hapus peserta (EditAssessment POST `removedUserIds` loop, `AssessmentAdminController.cs:~1896-1934`) agar mendeteksi sesi **Abandoned** atau punya **AttemptHistory/StartedAt** (bukan hanya InProgress/Completed).
  - **Soft-confirm via server round-trip + flag:** POST deteksi peserta ber-riwayat → **batalkan penghapusan itu** + tampilkan peringatan ("peserta X punya riwayat ujian — konfirmasi untuk hapus"); HC submit ulang dengan **flag konfirmasi** → baru hapus. Server-authoritative (bukan hanya JS).
  - **Cleanup wajib:** saat penghapusan benar-benar dijalankan, **cascade hapus `AssessmentAttemptResponseArchives` by AttemptHistoryId** (anak by AttemptHistory) agar tak ada orphan. Berlaku untuk semua jalur delete D-32.

### RTH-05 — MaxAttempts diturunkan di bawah pemakaian (VAL-06)
- **D-07 (konfirmasi pra-simpan, non-blocking):** Saat HC menurunkan `MaxAttempts` di bawah jumlah percobaan yang sudah terpakai (di `UpdateRetakeSettings` / titik POST setelan retake), tampilkan **modal konfirmasi pra-simpan** ("X peserta sudah memakai lebih dari batas baru, lanjutkan?"). Tetap **non-blocking** — bila dikonfirmasi, simpan apa adanya (perilaku retroaktif existing `RetakeRules.cs:46` dipertahankan; attempt yang sudah berjalan tak dibatalkan). Hitung "terpakai" pakai helper snapshot-presence yang sama (D-05).

### Claude's Discretion
- **RTH-03** nama & posisi helper `CountEraRetakeArchives`, selama keempat situs hitung memakai predikat snapshot-presence yang identik.
- Presisi pesan/teks peringatan & konfirmasi (Bahasa Indonesia), posisi guard, dan bentuk modal — ikuti idiom existing (TempData untuk toast non-blocking; JS confirm/modal Bootstrap untuk konfirmasi pra-aksi).
- Konvensi +7h WIB untuk perbandingan window pada D-01 — reuse pola persis `StartExam` agar tak ada drift timezone.
- **Backward-compat WAJIB:** jalur worker retake yang sudah ada (eligibility, tier-feedback leak-safe A1, cap, cooldown) tak berubah perilaku kecuali penambahan gate window. Assessment Standard (non Pre-Post) tetap retakeable seperti existing.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit sumber (temuan + bukti file:line)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.3 — RTK-LOGIC-02 (HIGH, ~baris 288-290), RTK-LOGIC-01 (~313), RTK-LOGIC-03 (~318), PA-06 (~347), VAL-06/RTK config (~410)
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §F (lifecycle states ~baris 105-170) — F5 re-entry StartExam window enforcement; transisi Retake/Reset claim-atomik; Abandoned reachable
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` (tabel field ~baris 207-219) — peran ExamWindowCloseDate vs RetakeCooldownHours

### Requirements & roadmap
- `.planning/REQUIREMENTS.md` — RTH-01..05 (traceability Phase 421, baris 25-31)
- `.planning/ROADMAP.md` — Phase 421 (goal + 5 success criteria observable)

### Spec mesin retake (v32.4 — fondasi yang di-harden)
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` — desain RetakeService/RetakeRules, snapshot per-soal, 10 keputusan terkunci (D1-D10), eligibility & cap

### Out-of-scope (jangan duplikasi)
- v32.6 branch `main` (Section/Scoped-Shuffle, fase 415-419) — tak menyentuh retake; tak relevan di sini.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`Helpers/RetakeRules.cs`** — `CanRetake` PURE/EF-free (urutan guard fail-fast; cooldown UTC-injected). **Akar D-01 eligibility:** tambah param `examWindowCloseDate`. `ShouldHideRetakeToggle`, `ResolveReviewMode` (tier feedback leak-safe A1) juga di sini — gate window otomatis merembet ke tombol & feedback.
- **`RetakeService.cs`** — `ExecuteAsync` klaim atomik → snapshot → archive → reset (`RemoveRange` responses/assignment/ET + null skor/cert/status via `ExecuteUpdateAsync` `:~103-112`) → audit. `CanRetakeAsync` punya counting era-retake DB-aware (snapshot-presence) = sumber kebenaran cap (`:~145-150,237-242`). **D-01 eksekusi:** abort sebelum `RemoveRange`. **D-03:** SetProperty NomorSertifikat=null. **D-05:** sumber predikat snapshot.
- **`AssessmentAdminController.cs`**:
  - `ResetAssessment` (~`:4286`, single-source guard `:4279`) — jalur reset HC; tempat D-03 (nol-kan cert) + D-04 (konfirmasi cabut cert).
  - `UpdateRetakeSettings` (~`:5654`) — clamp + simpan setelan retake; tempat D-02 (warning cooldown>window) + D-07 (konfirmasi MaxAttempts turun).
  - Warning count `ViewBag.RetakeMaxAttemptsUsedInGroup` (~`:5798`, `GroupBy(UserId).Count()` tanpa filter) — tempat D-05 (tambah predikat snapshot + helper).
  - Guard hapus peserta EditAssessment POST `removedUserIds` loop (~`:1896-1934`) — saat ini hanya tolak InProgress/Completed via TempData+continue; tempat D-06 (perluas Abandoned/attempt-history + soft-confirm flag + cleanup arsip).
- **`CMPController.cs`** — `RetakeExam` (~`:2470-2554`, re-check `CanRetakeAsync` lalu `ExecuteAsync`) + `StartExam` window enforcement (`:~955-960`, `now+7h > ExamWindowCloseDate` → blok). Konvensi +7h WIB untuk reuse di D-01.
- **`ManagePackages.cshtml`** (`:157-163`) — konsumen warning count RTH-03.

### Established Patterns
- **Pure-rules kill-drift:** `RetakeRules` / `ShuffleToggleRules` — caller suplai fakta, keputusan terpusat. RTH-03 helper `CountEraRetakeArchives` ikut pola ini.
- **Toast non-blocking:** `TempData["Error"]`/`TempData["Warning"]` (mirror guard existing `:1896-1905`).
- **Snapshot-presence = sumber kebenaran cap** (D-01 v32.4): hanya arsip ber-snapshot menghitung cap; legacy HC-reset pre-v32.4 natural-excluded. Konsisten di semua counting (D-05).
- **Konvensi waktu +7h WIB** di StartExam — reuse persis untuk gate window (hindari drift TZ).

### Integration Points
- `RetakeRules.CanRetake` signature berubah (+`examWindowCloseDate`) → semua caller (RetakeService `CanRetakeAsync`, ViewModel/controller Phase 407) wajib menyuplai fakta baru. Cek kompilasi semua call-site.
- Eligibility (CanRetake) → tombol Results + tier feedback otomatis ikut gate (tak perlu perubahan view terpisah untuk hide tombol).
- EditAssessment POST = satu submit form besar; soft-confirm RTH-04 butuh round-trip + hidden flag konfirmasi (bukan dedicated remove-endpoint — v32.5 FlexibleParticipantRemove ada di branch main, BUKAN di ITHandoff).
</code_context>

<specifics>
## Specific Ideas

- **Dead-end RTH-01 = HIGH, anti-destruktif:** gate WAJIB sebelum `RemoveRange`. Lebih baik pekerja tak pernah lihat tombol (eligibility) daripada klik lalu sesi live jadi shell kosong non-completable.
- **Pola confirm-before konsisten:** user memilih konfirmasi eksplisit untuk aksi destruktif/retroaktif (RTH-02 cabut cert, RTH-04 hapus ber-riwayat, RTH-05 turunkan cap). Jaga konsistensi UX di tiga titik.
- **Satu sumber counting:** keempat situs hitung percobaan (cap RetakeService, re-check CMP, warning ManagePackages, "terpakai" RTH-05) harus pakai helper snapshot-presence yang sama.
- Backend-heavy phase (logika/guard); komponen UI minimal (konfirmasi/toast). Kemungkinan tak butuh UI-SPEC penuh — planner nilai apakah `/gsd-ui-phase` perlu untuk modal konfirmasi.
</specifics>

<deferred>
## Deferred Ideas

- Strategi grading retake selain "attempt terakhir" (highest/avg), cooldown escalating, rotasi AccessToken per-attempt, cap per-tahun — YAGNI v32.4, tetap out-of-scope.
- Dedicated participant-management endpoint (v32.5 FlexibleParticipantRemove) ada di branch `main` — JANGAN tarik ke ITHandoff; soft-confirm RTH-04 dikerjakan di alur EditAssessment POST existing. Rekonsiliasi saat merge.
- Tidak ada scope creep lain — diskusi tetap dalam batas 5 REQ RTH.

</deferred>

---

*Phase: 421-retake-lifecycle-hardening*
*Context gathered: 2026-06-23*
