---
title: "Readiness Ujian Lisensor — Profil & Peta Risiko (gladi-bersih E2E)"
date: "2026-06-15"
promoted: false
source: "/gsd-explore session 2026-06-15"
---

# Readiness Ujian Lisensor — Profil & Peta Risiko

Tujuan: pastikan fitur Assessment **berjalan lancar di ujian REAL** (kategori
Training **lisensor**, tipe **standard**). Bukan audit kode lagi — code-audit full
Assessment sudah dilakukan 2026-06-13 (lihat
`.planning/notes/2026-06-13-audit-full-sistem-assessment.md`) dan jadi pendorong
v29.0 + v30.0. Yang kurang = **verifikasi E2E "gladi-bersih"**: jalankan satu ujian
lisensor realistis end-to-end di kondisi nyata.

## Profil ujian (dari sesi explore 2026-06-15)

| Aspek | Nilai |
|---|---|
| Kategori | Training **lisensor** (berujung kelulusan/sertifikat) |
| Tipe assessment | **Standard** |
| Skala | **≤ 30 peserta serentak**, 1 window |
| Jenis soal | **Single Answer + Multiple Answer + Essay + soal bergambar** (keempatnya) |
| Cakupan "harus lancar" | **Full lifecycle + monitoring admin real-time** |

Cakupan penuh = peserta start → jawab (4 jenis soal) → submit → scoring auto →
grading essay manual admin → skor akhir → lulus/tidak → **sertifikat lisensor terbit**
→ **admin pantau progress real-time** saat ujian berlangsung.

Beban server **bukan** risiko utama (≤30 peserta). Fokus = **correctness scoring +
kelancaran alur lifecycle**.

## Peta risiko → area bug historis

Tiap jalur scoring di ujian ini menyentuh area yang historisnya paling sering bug.
Prioritas verifikasi:

| Jalur | Risiko / referensi historis |
|---|---|
| Essay grading & correctness | **v30.0** (ECG-01..06, helper `IsQuestionCorrect` essay>0). Area bug terbaru — prioritas tinggi |
| Multiple Answer scoring | Partial vs all-or-nothing — pastikan aturan benar untuk lisensor |
| Single Answer scoring | exact-match auto — risiko rendah, tetap diverifikasi |
| Soal bergambar (render+upload) | **v24.0** (IMG-01..07, RND-04, SYN). Razor dynamic WAJIB diuji Playwright runtime — grep+build tak cukup (lesson Phase 354) |
| Sertifikat lisensor | terbit setelah lulus — template + data benar |
| Monitoring admin real-time | **v30.0** UIG-01..04 (page /Admin/EssayGrading + worker-list) |

## Constraint kerja (WAJIB — dari user 2026-06-15)

Code **baru di-deploy IT ke Dev**. Saat gladi-bersih:

1. **Report-first, BUKAN auto-fix.** Temuan bug/error/issue → **laporkan ke user dulu**.
2. Sertakan klasifikasi tiap temuan: tingkat (HIGH/MED/LOW), apakah butuh **fix lokal**,
   apakah butuh **re-deploy ke Dev IT**.
3. **User yang putuskan** fix & re-deploy. Jangan langsung patch tanpa persetujuan.
4. ❌ Jangan edit kode/DB langsung di Dev/Prod (sesuai
   [`docs/DEV_WORKFLOW.md`](../../docs/DEV_WORKFLOW.md)). Fix = lokal → verify → push →
   notify IT.

Output gladi-bersih = **checklist readiness PASS/FAIL + daftar temuan terklasifikasi**,
bukan diff.

## Verifikasi lokal (lihat CLAUDE.md)

- Seed realistis = klasifikasi `temporary + local-only`, snapshot DB dulu, catat
  `docs/SEED_JOURNAL.md`, restore setelah test (SEED_WORKFLOW.md).
- E2E gambar/dynamic → Playwright runtime, `--workers=1` (reference_local_e2e_sql_env_fix).
- AD lokal: `Authentication__UseActiveDirectory=false` saat `dotnet run` (Phase 355 lesson).
- Admin login lokal: `admin@pertamina.com` (reference_dev_credentials).

## Temuan (report-first log)

> Format: ID | tingkat | deskripsi | fix lokal? | re-deploy IT? | status. Tak ada auto-fix.

- **F-01 — MED (UX/komunikasi).** UI exam (`Views/CMP/StartExam.cshtml:122`) kasih tahu
  worker MA "Pilih semua yang benar" TAPI tak ada peringatan jawaban sebagian / ada salah =
  **0 poin** (all-or-nothing). Tak ada banner aturan poin di mana pun view exam (cek penuh
  StartExam.cshtml). Risiko lisensor high-stakes: worker kira ada partial credit → sengketa
  nilai. **Bukan bug logika** — scoring all-or-nothing benar (`AssessmentScoreAggregator.cs:50`,
  10/10 test PASS). Gap komunikasi. **Fix lokal?** Ya, tambah 1 baris teks peringatan di L122
  (low risk). **Re-deploy IT?** Ya (perubahan view). **Status:** PENDING keputusan user.

### Sweep multi-agent 2026-06-15 (4 area, adversarial-verified) — wf_d1fe62d4

Inti tiap area **bekerja benar secara kode** (essay v30 kill-drift OK, sertifikat gate IsPassed OK,
render gambar struktur Razor OK, monitoring SignalR push tahan ≤30 tanpa N+1). Temuan:

| ID | Sev | Area | Lokasi | Masalah | bug? | fix lokal | re-deploy |
|----|-----|------|--------|---------|------|-----------|-----------|
| **F-09** | **HIGH** | gambar | `_QuestionImage.cshtml:38` | `src="@imagePath"` leading-slash `/uploads/..` **bypass PathBase `/KPB-PortalHC`** → gambar soal+opsi **broken di Dev** (404). Lokal no-repro (no PathBase). e2e tak nangkap (cek regex src, bukan load). **Exam-blocking utk soal bergambar** | ✅ | ✅ wrap `Url.Content("~"+path)` | ✅ + UAT browser Dev |
| **F-02** | MED | essay | `ExcelExportHelper.cs:110` | Excel "Detail Per Soal" pakai aturan essay lama `>= ScoreValue/2` vs helper `>0` → label "Benar/Salah" kontradiksi web/PDF utk skor parsial 1..(SV/2−1). Bukan scoring, label only | ✅ | ✅ ganti ke `IsQuestionCorrect` | ✅ |
| **F-03** | MED | essay | `AssessmentAdminController.cs:3525` | `SubmitEssayScore` tanpa status-guard → edit skor essay pasca-Completed (sesi gagal / no-cert) ubah count/ET live tapi `Score`/`IsPassed` tersimpan basi → divergen di 1 halaman. Re-finalize no-op | ✅ | ✅ guard status / recompute | ✅ |
| **F-04** | MED | essay | `AssessmentAdminController.cs:3500` | Essay dikosongkan worker (no response row) → pending-count beda antara monitoring (row-based, =0 "siap") vs page EssayGrading (hitung pending>0 → tombol Selesaikan disembunyikan) + HC tak bisa nilai essay kosong ("Jawaban tidak ditemukan") = **dead-end finalize**. Data benar (0 poin) tapi UI macet. Realistis (worker skip essay) | ✅ | ✅ samakan basis hitung / buat row 0 | ✅ |
| **F-06** | LOW | cert | `AssessmentAdminController.cs:3697` | Generate nomor cert essay **single-attempt no-retry** (vs GradingService 3x), catch telan semua `DbUpdateException` silent. Collision multi-HC → lulus tanpa nomor cert, no log. Komentar "same pattern" salah | ✅ | ✅ retry-loop+log | ✅ |
| **F-13** | LOW | monitoring | `AssessmentAdminController.cs:3753` | `FinalizeEssayGrading` tak broadcast monitor group → tab admin LAIN yang buka monitoring detail stale s/d refresh. 1-operator ≈ nihil | ✗ UX | opsional | opsional |
| **F-11** | LOW | gambar | `Results.cshtml:388` | Gambar opsi di Results/ExamSummary `AriaContext="opsi"` tanpa huruf A/B/C/D (a11y minor) | ✗ | opsional | opsional |
| F-05/07/08/10/12/14/15/16 | INFO | — | — | catatan (essay >0=hijau by-design; cert seq 1-tabel; baris cert no-nomor by-design; monitoring detail no-image; daftar item wajib UAT browser; no polling fallback SignalR; index monitoring statis; **verifikasi positif push tahan ≤30 no-N+1**) | ✗ | — | — |

**Status semua: PENDING keputusan user (batch).** Plus F-01 (MED, UI tak warn MA partial=0) dari sebelumnya.

**Catatan F-09 (penting):** verifier read-only, **belum** konfirmasi browser Dev (tak boleh akses Dev). Analisis kuat (PathBase di `appsettings.json:9` tak di-override Development; leading-slash bypass) → keyakinan tinggi reproduce. **WAJIB UAT browser 1× di `http://10.55.3.3/KPB-PortalHC` layar StartExam bergambar sebelum ujian.**

### Verifikasi browser DI DEV (2026-06-15) — assessment nyata `GLADI LISENSOR`

Dijalankan langsung di `http://10.55.3.3/KPB-PortalHC` (login Admin KPB, assessment buatan user, sessionId=30, packageId=10, 4 soal). Hasil:

**F-09 — CONFIRMED HARD (HIGH, exam-blocking soal bergambar).** Soal "Komponen ini berfungsi sebagai?" (Single, ada gambar) → gambar **404**. Network: `GET http://10.55.3.3/uploads/questions/10/...download.jpg → 404`. URL **drop prefix `/KPB-PortalHC`** (semua resource lain pakai prefix → 200). Screenshot = ikon gambar rusak. **Prediksi sweep tepat.** Fix = `_QuestionImage` src PathBase-aware + re-deploy IT.

**F-DEV-01 — NEW (HIGH, exam-blocking SEMUA submit).** Soal 3 "Pilih simbol APD yang benar" (Single Answer, 10 poin) **tersimpan dengan 0 opsi** (edit form: A/B/C/D semua kosong, tanpa teks & tanpa gambar; preview tanpa opsi; exam render `<div class="list-group"></div>` kosong). Akibat: peserta **tak bisa jawab** → ExamSummary "1 soal belum dijawab" → tombol **"Kumpulkan Ujian" DISABLED permanen** (incomplete-gate tak pernah clear) → **seluruh ujian tak bisa di-submit manual**. Akar = **validasi create/edit soal tak mewajibkan ≥1 opsi** (idealnya ≥2 + ≥1 benar) untuk tipe Single/Multiple. is_logic_bug. Fix lokal (tambah validasi server+client) + re-deploy. **Lebih berbahaya dari kelihatannya: 1 soal salah-konfig membekukan submit untuk SEMUA peserta.**

**F-01 — confirmed visual (MED).** Soal MA "Pilih semua yang benar", tanpa warn "sebagian = 0".

**Positif terverifikasi di Dev:** token jalan (AAC4EX), exam load, **SignalR Live + autosave "saved" jalan**, Acak Pilihan relabel by-identity benar (pilih Avtur/Bensin/Solar → summary canonical "A,C,D"), monitoring detail + Access Token tampil, menu Aksi grup lengkap. Lifecycle **submit→grade→cert BELUM ter-test** (ke-blok F-DEV-01).

> ⚠️ State Dev: sesi admin (id=30) **InProgress nyangkut** (tak bisa submit). Untuk lanjut smoke: soal 3 harus diberi opsi (user fix data di admin) ATAU matikan soal 3. Aku TIDAK ubah data Dev.

### RE-CHECK adversarial 2026-06-15 (wf_cc0dd4b7) — REGISTER FINAL

4 lane re-verifikasi. Koreksi + temuan baru. **Register di bawah MENGGANTIKAN yang lama.**

**Koreksi:**
- **F-DEV-01 dampak dikoreksi:** BUKAN "ujian tak bisa selesai total". Manual "Kumpulkan" memang disabled permanen, TAPI **timer-expiry auto-submit tetap fire** (`examForm.submit()` bypass gate saat `serverTimerExpired=true`, CMPController:1619-1653) → ujian SELESAI saat timeout, soal 0-opsi auto-0. Tetap HIGH (peserta terjebak nunggu timeout, tak bisa submit awal, kehilangan poin soal cacat). Root cause: CreateQuestion/EditQuestion (`AssessmentAdminController.cs:6440/6647`) validasi `correctCount` tapi **tak validasi jumlah opsi ber-teks** — opsi cuma ditambah `if(!IsNullOrWhiteSpace(text))`.
- **F-01 severity MED → LOW** (murni UX, backend all-or-nothing benar).
- **REFUTE:** Import Excel TAK bisa hasilkan 0-opsi (wajib A-D terisi) → F-DEV-01 eksklusif jalur form manual. Tak ada surface PathBase lain selain F-09 (cert pakai `~/` PathBase-aware; PDF = download server-side).

**Temuan BARU:**
- **F-21 (HIGH, exam-blocker, essay):** Jawaban Essay HANYA tersimpan via SignalR **debounce 2 detik, tanpa flush saat submit/blur**. Timer habis → `examForm.submit()` langsung tanpa flush → teks essay ~2 detik terakhir **HILANG** (ExamSummary baca dari DB). Lebih buruk: gate incomplete hitung "terjawab" dari baris DB → peserta yang **sudah ngetik** essay tapi baris belum ke-save bisa **ditolak submit** "Masih ada N soal belum dijawab" di menit akhir. `Views/CMP/StartExam.cshtml:903-911,472-484,980-1001`. **Lisensor ada essay → relevan.**
- **F-17 (MED):** `BulkExportPdf` (PDF per-peserta) menilai soal **Multiple Answer SALAH** — baca 1 baris response (`FirstOrDefault`), tandai Benar/Salah by 1 opsi, BUKAN `SetEquals`. PDF bukti lisensor mis-label soal MA. `AssessmentAdminController.cs:5070-5086`.
- **F-18 (MED):** Export PDF & Excel resolve soal dari **semua soal paket (`AssessmentPackageId`)** bukan `ShuffledQuestionIds` → salah/hilang soal saat **≥2 paket + Acak Soal ON**. Single-paket aman. `AssessmentAdminController.cs:4984/4801`.
- **F-19 (LOW):** Excel "Detail Jawaban" (BulkExport) essay selalu "—" (tak pernah tampil skor/teks walau sudah dinilai). `AssessmentAdminController.cs:4828-4838`.
- **F-20 (LOW):** `SubmitExam` upsert MC bisa **timpa jawaban tersimpan jadi null** bila soal tak ada di form answers (data-loss laten; happy-path terlindungi). `CMPController.cs:1712-1716`.
- **F-22 (LOW):** `Hub.SaveTextAnswer` tak ada guard timer-expired (beda dari `SaveMultipleAnswer`) → essay bisa ditulis lewat batas waktu. `Hubs/AssessmentHub.cs:134-182`.

**Positif diverifikasi SOLID (bukan bug):** scoring-on-submit (grading by `PackageOption.Id`, dedup, MA SetEquals, essay→PendingGrading); cert generation (GradingService retry 3x + unique index, QuestPDF guard); shuffle TIDAK pengaruhi skor; concurrency ≤30 aman (Reshuffle/Akhiri/AddExtraTime skip InProgress, broadcast benar); timer resume server-authoritative + auto-submit token one-shot; double-submit ter-guard. **INFO:** Acak Soal multi-paket = tiap peserta dapat K=min(soal) **sampel** (by-design, bukan semua soal — operator lisensor wajib sadar).

#### Register final actionable: 3 HIGH · 5 MED · 7 LOW

| ID | Sev | Temuan | Lokasi | blocker |
|----|-----|--------|--------|---------|
| F-09 | HIGH | Gambar broken di Dev (PathBase) | `_QuestionImage.cshtml:38` | ✅ confirmed Dev |
| F-DEV-01 | HIGH | Soal 0-opsi (validasi kurang) → submit awal mati, auto-0 saat timeout | `AssessmentAdminController.cs:6440/6647` | ✅ confirmed Dev |
| F-21 | HIGH | Essay ~2s terakhir hilang + reject submit menit akhir | `StartExam.cshtml:903/472/980` | code |
| F-02 | MED | Excel matrix label essay drift (`≥SV/2` vs `>0`) | `ExcelExportHelper.cs:110` | — |
| F-03 | MED | Edit essay pasca-finalize desync Score | `AssessmentAdminController.cs:3525` | — |
| F-04 | MED | Essay kosong → dead-end finalize | `AssessmentAdminController.cs:3500` | — |
| F-17 | MED | PDF per-peserta mis-label MA (no SetEquals) | `AssessmentAdminController.cs:5070` | — |
| F-18 | MED | Export soal by-paket bukan ShuffledQuestionIds (≥2 paket) | `AssessmentAdminController.cs:4984/4801` | — |
| F-01 | LOW | UI MA tanpa warn "sebagian=0" | `StartExam.cshtml:122` | — |
| F-06 | LOW | Cert nomor no-retry (essay finalize) | `AssessmentAdminController.cs:3697` | — |
| F-11 | LOW | a11y aria opsi | `Results.cshtml:388` | — |
| F-13 | LOW | Finalize tak broadcast monitor | `AssessmentAdminController.cs:3753` | — |
| F-19 | LOW | Excel BulkExport essay selalu "—" | `AssessmentAdminController.cs:4828` | — |
| F-20 | LOW | SubmitExam MC null-overwrite laten | `CMPController.cs:1712` | — |
| F-22 | LOW | SaveTextAnswer tanpa guard timer | `Hubs/AssessmentHub.cs:134` | — |

Semua **0 migration**. Fix = view/controller/validasi + re-deploy IT.

## Fakta scoring terverifikasi (2026-06-15)

- Lulus: `percentage >= PassPercentage`; **default 70%** per-assessment (`AssessmentSession.cs:29`).
- Skor: `totalScore/maxScore×100` integer truncate (`AssessmentScoreAggregator.cs:58`); tiap soal = `ScoreValue`-nya.
- MC: 1 opsi benar = poin. MA: **all-or-nothing** `SetEquals` (DECISION user: pertahankan). Essay: manual HC 0..ScoreValue.
- 14 unit test `AssessmentScoreAggregatorTests` PASS (termasuk 4 skenario MA benar={A,C,D}).

## Next

- Eksekusi = **verifikasi browser/UAT (report-first)**, BUKAN GSD phase — ini test UI,
  bukan kerjaan building kode. (Phase 385 sempat dibuat lalu dibatalkan 2026-06-15.)
- Jalankan via sesi browser Playwright langsung (pakai checklist note ini) atau
  `/gsd-verify-work`.
- Jawab open unknown dulu → `.planning/research/questions.md` (terutama tanggal ujian +
  aturan kelulusan/scoring).
