# Pitfalls Research

**Domain:** Brownfield Assessment Enhancement — ASP.NET Core MVC
**Researched:** 2026-04-06
**Confidence:** HIGH (berbasis analisis kodebase aktual + pola umum brownfield)

---

## Critical Pitfalls

### Pitfall 1: Grading Engine Asumsi Single-Correct Hancur oleh Multiple Answer

**What goes wrong:**
`GradeFromSavedAnswers` di `AssessmentAdminController.cs` (baris ~2508–2516) iterasi soal,
cari satu `PackageOptionId` per soal dari `PackageUserResponses`, lalu cek `selectedOption.IsCorrect`.
Struktur ini berasumsi jawaban = satu option. Jika Multiple Answer ditambahkan dengan cara yang
sama (satu row per pilihan), loop akan menghitung salah — hanya option pertama yang match, atau
bahkan 0 jika kode tidak diubah. Score bisa jadi 0% untuk seluruh ujian yang berisi soal Multiple Answer.

**Why it happens:**
`PackageUserResponse` punya kolom `PackageOptionId` (single FK, nullable). Developer menambah
tipe soal baru tapi lupa bahwa tabel response menyimpan 1 baris per soal, bukan 1 baris per pilihan.
Grading path di `EndExam`, `EndAllExams`, `SubmitExam` (worker-side), dan `GradeFromSavedAnswers`
semua share asumsi ini.

**How to avoid:**
Sebelum menambah Multiple Answer, putuskan skema storage-nya dulu:
- **Opsi A (Rekomendasi):** Tambah kolom `AnswerText` (nullable string) di `PackageUserResponse`
  untuk menyimpan comma-separated option IDs (`"12,15,18"`). Grading path baca kolom ini bila diisi.
- **Opsi B:** Ubah constraint ke allow multiple rows per (SessionId, QuestionId). Perlu unique index
  diubah dan semua query response di-refactor.

Pilih Opsi A — backward-compatible, tidak perlu migrasi data lama.

Setelah skema fix, update **semua** titik grading:
1. `GradeFromSavedAnswers` (~baris 2484)
2. Worker-side `SubmitExam`
3. `EndExam` (admin end single)
4. `EndAllExams` (admin end batch)
5. Item analysis reporting (jika ditambah di milestone ini)

**Warning signs:**
- Ada soal tipe Multiple Answer tapi score selalu 0 atau lebih rendah dari expected
- Unit test grading menunjukkan maxScore benar tapi totalScore = 0
- Review answer di admin menampilkan hanya satu pilihan dipilih padahal worker pilih banyak

**Phase to address:**
Phase pertama yang menambah Question Types — harus fix storage + semua grading path sebelum
fitur Multiple Answer bisa di-test.

---

### Pitfall 2: Essay HasManualGrading Flag Tidak Di-Guard di Semua Jalur Auto-Grade

**What goes wrong:**
Sistem saat ini auto-grade segera saat worker submit (`SubmitExam`) dan saat admin `EndExam`.
Jika soal Essay ditambahkan dan `HasManualGrading` flag dibuat, tapi lupa ditambahkan guard di
`GradeFromSavedAnswers`, maka sesi yang mengandung Essay akan auto-grade dengan maxScore yang
salah (soal essay tidak punya option, jadi `q.Options` kosong, `maxScore` tidak bertambah untuk
soal essay tersebut). Hasil: persentase skor lebih tinggi dari seharusnya (inflated score).

**Why it happens:**
`GradeFromSavedAnswers` loop terhadap semua soal tanpa discriminasi tipe. Tidak ada `QuestionType`
field di `PackageQuestion` model sekarang. Developer menambah essay tapi lupa bahwa grading path
tidak tau soal mana yang perlu skip auto-grade.

**How to avoid:**
1. Tambah `QuestionType` enum ke `PackageQuestion` (MultipleChoice, TrueFalse, MultipleAnswer,
   Essay, FillInTheBlank) sebagai langkah pertama, sebelum menambah soal baru apapun.
2. Di `GradeFromSavedAnswers`: bila `q.QuestionType == Essay`, tambah ke `maxScore` tapi
   jangan auto-grade — set pending manual.
3. `AssessmentSession` perlu flag `HasPendingManualGrading` (bool) agar workflow HC tahu
   bahwa session belum final meski worker sudah submit.
4. Sertifikat generation guard: cek `!session.HasPendingManualGrading` sebelum issue cert.

**Warning signs:**
- Sesi dengan Essay langsung muncul "Completed" dan ada sertifikat terbit, padahal HC belum review
- Score% lebih tinggi dari jumlah soal non-essay yang bisa dijawab benar
- ManageAssessment view tidak menampilkan indikator "Menunggu Review Manual"

**Phase to address:**
Phase Question Types — Model `PackageQuestion` harus dapat `QuestionType` sebelum Essay UI dibuat.

---

### Pitfall 3: Pre-Post LinkedGroupId Cascade Tidak Konsisten di Semua Delete/Reset Path

**What goes wrong:**
Pre-Post Test dilink via `LinkedGroupId` (berdasarkan decision di PROJECT.md). Cascade saat ini
hanya direncanakan: "Reset Pre → Post ikut reset." Jika implementasi hanya mengupdate session
yang di-reset tapi tidak mengecek linked partner, maka:
- Reset Pre → Post tetap `Completed` dengan score lama → hasil comparison Pre vs Post tidak valid
- Delete Pre-Test session → Post-Test session menjadi orphan dengan `LinkedGroupId` yang menunjuk
  ke row yang sudah tidak ada (jika FK bukan nullable)
- Renew Pre-Test → Post-Test tidak ikut renew → worker lihat Pre baru tapi Post dari periode lama

**Why it happens:**
Cascade logic di AssessmentAdminController sudah sangat panjang (~3800 baris). Developer
menambah cascade baru tapi tidak audit semua action yang bisa trigger-nya:
1. `ResetAssessment` action
2. `DeleteAssessment` action
3. `CreateRenewal` action
4. `EndExam` / `EndAllExams` (jika Pre belum complete, Post tidak boleh bisa dimulai)
5. Import worker batch (jika ada)

**How to avoid:**
Buat private helper `GetLinkedPartner(AssessmentSession session)` yang selalu dipanggil di setiap
action yang bisa mengubah state sesi. Dokumentasikan tabel state machine Pre-Post:

| Event | Pre State | Post Action |
|-------|-----------|-------------|
| Reset Pre | * → Open | Reset Post juga (jika ada) |
| Delete Pre | Completed | Orphan guard: set Post.LinkedGroupId = null OR block delete |
| Delete Post | Completed | Pre tidak terpengaruh |
| Worker start Post | Pre != Completed | Block with error message |

**Warning signs:**
- Comparison report menampilkan Pre score vs Post score dari tanggal berbeda tanpa peringatan
- Post-Test bisa dimulai meski Pre-Test belum selesai
- Query `WHERE LinkedGroupId = X` return 1 hasil (seharusnya selalu 2 atau 0)

**Phase to address:**
Phase Assessment Type & Pre-Post — state machine harus didefinisikan dan tested sebelum UI dibuat.

---

### Pitfall 4: AssessmentAdminController 3828 Baris Membuat Perubahan Berisiko Tinggi

**What goes wrong:**
File `AssessmentAdminController.cs` sudah 3828 baris. Menambah feature baru langsung di sini
(multiple question types, pre-post logic, manual grading workflow) akan membuat file semakin
susah di-navigate dan meningkatkan risiko merge conflict atau side-effect tidak disengaja.
Khususnya, grading logic tersebar di minimal 4 action method — update satu tapi lupa yang lain
adalah pola failure yang sudah terbukti di codebase ini (lihat v12.0 refactoring AdminController
8514 baris).

**Why it happens:**
Milestone baru ditambah incremental ke controller yang sudah ada karena lebih cepat. Tapi di
ambang 3800+ baris, setiap tambahan meningkatkan risiko secara non-linear.

**How to avoid:**
Sebelum menambah feature baru, ekstrak `GradingService` (atau `AssessmentGradingHelper`) sebagai
private service/helper class yang berisi:
- `GradeFromSavedAnswers`
- `CalculateScore`
- `HandleManualGradingPending`
- `HandlePrePostCascade`

Controller kemudian tinggal call service. Ini adalah pola yang sudah dibuktikan berhasil di v12.0
(AdminController 8514 baris → 108 baris) dan v7.6 (IWorkerDataService).

**Warning signs:**
- Grading bug fix di satu action tapi bug yang sama masih ada di action lain
- PR diff untuk satu fitur baru menyentuh 500+ baris di satu file
- Developer harus scroll panjang untuk menemukan method yang relevan

**Phase to address:**
Fase paling awal milestone ini — sebaiknya jadi Phase 1 sebelum feature apapun ditambah.

---

### Pitfall 5: Mobile Touch Events Konflik dengan Anti-Copy JavaScript

**What goes wrong:**
Anti-copy protection (Phase 280) menggunakan event listeners pada `contextmenu`, `copy`, `select`,
dan kemungkinan `touchstart`/`touchend` untuk deteksi screenshot atau selection di mobile.
Menambah swipe navigation (touchstart/touchend/touchmove) untuk mobile exam UI bisa conflict:
- Swipe kanan/kiri untuk ganti soal ter-intercept oleh anti-copy handler
- Anti-copy handler `preventDefault()` pada touch events mematikan swipe
- Hasil: swipe tidak berfungsi di mobile, atau anti-copy tidak efektif

**Why it happens:**
Anti-copy JS ditulis untuk desktop, tidak mengantisipasi touch events mobile. Developer mobile
feature menambah touch handlers tanpa mengecek apakah ada handler yang sudah ada pada event
yang sama.

**How to avoid:**
Sebelum menambah mobile touch navigation:
1. Audit semua event listeners di exam view JS (cari `addEventListener` + `on` property assignments)
2. Identifikasi apakah anti-copy menggunakan touch events
3. Jika iya: refactor anti-copy menjadi bisa detect swipe (movement > threshold) vs. selection
   (stationary touch) — tidak block semua touch events
4. Test di device fisik, bukan hanya browser DevTools mobile emulator (DevTools tidak selalu
   replicate touch event bubbling yang sama)

**Warning signs:**
- Swipe navigation berfungsi di browser desktop (mouse drag) tapi tidak di HP
- Anti-copy alert muncul saat user coba swipe soal
- Console error "Unable to preventDefault inside passive event listener"

**Phase to address:**
Phase Mobile Optimization — sebelum touch event code ditulis, audit anti-copy JS.

---

### Pitfall 6: `aria-live` Timer Konflik dengan SignalR Update

**What goes wrong:**
Timer countdown ujian saat ini di-update via JavaScript polling/countdown dan ditampilkan di DOM.
Untuk accessibility (WCAG), timer perlu `aria-live="polite"` agar screen reader announce waktu.
SignalR (`AssessmentHub`) juga push updates ke DOM yang sama (monitoring dashboard). Jika
`aria-live` region overlap dengan area yang SignalR update, screen reader akan announce setiap
SignalR message — termasuk data admin monitoring yang tidak relevan untuk worker.

Sebaliknya, jika `aria-live="assertive"` dipakai untuk timer, setiap countdown tick (tiap detik)
akan interrupt screen reader — membuat ujian tidak bisa digunakan oleh tunanetra.

**Why it happens:**
Implementor accessibility menambah `aria-live` tanpa tahu bahwa ada komponen real-time lain
yang update DOM di sekitar area yang sama. SignalR dan accessibility diimplementasi oleh dua
developer berbeda atau di dua fase berbeda.

**How to avoid:**
1. Gunakan `aria-live="polite"` HANYA pada element timer countdown itu sendiri (bukan parent
   container besar)
2. Timer announce hanya pada interval bermakna: setiap 5 menit, lalu tiap menit saat < 5 menit
   tersisa, bukan tiap detik
3. SignalR update target element yang berbeda dari `aria-live` region
4. Test dengan screen reader aktual (NVDA/VoiceOver) — jangan hanya cek aria attribute di HTML

**Warning signs:**
- Screen reader terus-menerus berbicara setiap detik selama ujian
- SignalR monitoring panel dan timer ada dalam satu parent element dengan `aria-live`
- Accessibility audit tool report "aria-live region updated too frequently"

**Phase to address:**
Phase Accessibility — sebelum `aria-live` ditambahkan, audit DOM structure dan SignalR targets.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Multiple Answer storage sebagai CSV string di kolom `AnswerText` | Tidak perlu ubah unique constraint atau migrate data lama | Query item analysis lebih complex (perlu parse CSV) | Acceptable di MVP, refactor ke proper table jika item analysis butuh per-option analytics |
| Essay manual grading sebagai flag di `AssessmentSession` tanpa workflow state machine | Cepat diimplementasi | State fragile — edge case: partial-grade lalu session timeout tidak ter-handle | Acceptable hanya jika edge case didokumentasikan explicit |
| Simpan `LinkedGroupId` sebagai nullable int tanpa FK constraint | Fleksibel | Orphan data tidak terdeteksi oleh DB | Tidak acceptable — gunakan FK nullable dengan ON DELETE SET NULL |
| Tambah Pre-Post logic langsung ke AssessmentAdminController | Tidak perlu refactor | Controller makin panjang, makin susah test | Tidak acceptable — ekstrak GradingService dulu |
| Gunakan CSS media query saja untuk mobile tanpa touch event redesign | Cepat | Exam layout responsif tapi tidak usable di mobile (small tap targets, no swipe) | Tidak acceptable untuk milestone yang explicit targetkan mobile optimization |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| SignalR + Accessibility | `aria-live` pada container besar yang di-update SignalR | Buat element terpisah: satu untuk SignalR target, satu untuk `aria-live` timer/status |
| Essay Manual Grading + Certificate Generation | Terbitkan sertifikat saat `SubmitExam` meskipun ada soal Essay pending review | Guard `NomorSertifikat` generation dengan `!session.HasPendingManualGrading` |
| Pre-Post Test + Renewal | Renewal hanya create 1 sesi baru, padahal Pre-Post butuh 2 sesi sekaligus | Deteksi `AssessmentType == PrePostTest` di `CreateRenewal` action, create kedua sesi |
| Multiple Answer + Item Analysis | Item analysis hitung difficulty index dari `PackageUserResponse` yang menyimpan single option | Extend item analysis query untuk parse `AnswerText` bila `QuestionType == MultipleAnswer` |
| Fill in the Blank + Grading | String matching case-sensitive — "pompa" vs "Pompa" dianggap salah | Normalisasi answer saat grading: lowercase + trim, support alias jawaban |
| Mobile Swipe + Question Navigation | Swipe navigation berfungsi tapi tidak ada visual indicator soal mana yang sudah dijawab | Bottom nav harus update answered state saat swipe, sama seperti saat klik option |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| `GradeFromSavedAnswers` load semua Options untuk semua soal | Lambat untuk paket besar | Sudah di-`.Include(q => q.Options)` — OK untuk single-correct. Multiple Answer perlu parse `AnswerText` per soal | Terasa saat paket > 200 soal |
| Item analysis query full-scan `PackageUserResponses` | Dashboard item analysis lambat | Buat index composite pada `(PackageQuestionId, PackageOptionId)` | Terasa saat total responses > 10.000 rows |
| Gain score comparison query join Pre + Post sessions per worker | Lambat di report page | Pastikan `LinkedGroupId` di-index | Terasa saat batch > 100 peserta Pre-Post |
| Essay manual grading list query tanpa pagination | HC membuka halaman grading, semua essay dimuat sekaligus | Tambah server-side pagination sejak awal | Terasa saat > 50 peserta per batch |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Essay response disimpan tanpa sanitasi, lalu ditampilkan ke HC sebagai HTML | XSS — worker sisipkan script tag di jawaban essay | Gunakan `@` syntax Razor (bukan `Html.Raw`) saat render essay answer |
| Pre-Post result comparison accessible tanpa auth check | Worker bisa akses hasil rekan | Guard comparison report dengan ownership check: `session.UserId == currentUserId` |
| Manual grading submit tanpa concurrency check | Dua HC grade essay yang sama secara bersamaan — race condition | Tambah `UpdatedAt` concurrency token di grading submit |
| Fill in the Blank correct answer exposed via public API | Worker bisa lihat kunci jawaban via network tab | Jangan expose `CorrectAnswerText` di response JSON ke client sebelum session selesai |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Multiple Answer tidak ada indikator visual "pilih lebih dari satu" | Worker hanya pilih satu jawaban, kehilangan poin | Label soal harus jelas: "Pilih semua jawaban yang benar" + checkbox (bukan radio button) |
| Essay textarea tanpa karakter counter | Worker tidak tahu apakah ada batas panjang | Tampilkan counter karakter + max limit yang jelas |
| Pre-Post Test 2 card worker tanpa penjelasan urutan | Worker bingung mana yang harus dikerjakan dulu | Card Post-Test harus disabled/locked dengan tooltip "Selesaikan Pre-Test dahulu" |
| Font size control mengubah ukuran seluruh halaman | Layout exam rusak di mobile saat font besar | Scope font size change hanya ke area question text + options, bukan seluruh viewport |
| Timer di mobile tidak mudah dibaca (kecil, pojok) | Worker tidak sadar waktu hampir habis | Timer di mobile harus sticky, ukuran cukup besar, berubah warna merah saat < 5 menit |
| Swipe navigation tanpa auto-save | Worker swipe sebelum pilih, mengira jawaban sudah tersimpan | Auto-save jawaban saat swipe, bukan hanya saat klik "Simpan" |

---

## "Looks Done But Isn't" Checklist

- [ ] **Multiple Answer Grading:** Tampilan exam menampilkan checkbox dan bisa dipilih banyak — tapi verifikasi grading engine benar-benar menghitung partial credit dengan benar, bukan hanya all-or-nothing
- [ ] **Essay Manual Grading:** HC bisa input nilai — tapi verifikasi bahwa sertifikat belum terbit sebelum HC submit grade, dan session status di monitoring menunjukkan "Menunggu Grading Manual"
- [ ] **Pre-Post Cascade Reset:** Reset Pre di admin berfungsi — tapi cek apakah Post session ikut di-reset, bukan hanya Pre
- [ ] **Mobile Exam UI:** Tampilan responsive di DevTools — tapi test di device fisik: swipe, keyboard virtual overlap timer, dan orientation change
- [ ] **Accessibility Timer:** `aria-live` ada di HTML — tapi test dengan NVDA/VoiceOver bahwa screen reader tidak announce tiap detik
- [ ] **Gain Score Report:** Angka Pre dan Post score tampil side-by-side — tapi verifikasi gain score dihitung dari sesi Pre dan Post yang benar-benar linked (bukan hanya dua sesi terakhir milik user)
- [ ] **Fill in the Blank:** Jawaban bisa diketik — tapi verifikasi bahwa normalisasi (lowercase, trim, alias) diimplementasi di grading, bukan hanya di validasi input
- [ ] **Item Analysis:** Chart difficulty/discrimination tampil — tapi cek bahwa soal Essay dan Multiple Answer tidak di-include dalam calculation yang mengasumsikan binary correct/incorrect

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Multiple Answer grading salah sudah jalan di production | HIGH | Stop auto-grade, manual review semua affected sessions, regrade dengan logic baru, notify peserta |
| Essay sertifikat terbit sebelum HC review | MEDIUM | Revoke NomorSertifikat (set null), kirim notifikasi ke worker, HC grade ulang, re-issue cert dengan nomor baru |
| Pre-Post orphan data (Post tanpa Pre) | MEDIUM | Migration script: set `LinkedGroupId = null` untuk orphan Post sessions, update monitoring query |
| Mobile swipe tidak berfungsi karena anti-copy conflict | LOW | Feature flag: disable swipe untuk browser yang tidak support passive touch listeners, fallback ke button navigation |
| `aria-live` terlalu verbose | LOW | Ubah `aria-live="assertive"` ke `"polite"` + interval announce, tidak perlu data migration |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Grading engine asumsi single-correct | Phase 1: Model + GradingService Extraction | Unit test `GradeFromSavedAnswers` dengan soal Mixed type, pastikan score benar |
| Essay HasManualGrading tidak di-guard | Phase 1: Model + GradingService Extraction | Test submit exam dengan Essay → verifikasi sertifikat tidak terbit otomatis |
| AssessmentAdminController terlalu panjang | Phase 1 (paling awal) | GradingService extracted, controller bisa compile tanpa grading logic inline |
| Pre-Post cascade tidak konsisten | Phase 2: Assessment Type & Pre-Post | Test matrix: Reset Pre, Delete Pre, Worker start Post sebelum Pre selesai |
| Mobile touch konflik anti-copy | Phase 4: Mobile Optimization | Test swipe di iPhone Safari + Android Chrome dengan anti-copy aktif |
| aria-live konflik SignalR | Phase 5: Accessibility | Test dengan NVDA screen reader selama ujian aktif + SignalR monitoring berjalan |

---

## Sources

- Analisis kodebase aktual: `Controllers/AssessmentAdminController.cs` (3828 baris, 2026-04-06)
- Analisis kodebase aktual: `Models/AssessmentPackage.cs`, `Models/PackageUserResponse.cs`, `Models/AssessmentSession.cs`
- PROJECT.md: Key decisions Pre-Post Test (LinkedGroupId Opsi A, cascade spec)
- Pola yang sudah terbukti dalam proyek ini: v12.0 Controller Refactoring (8514 baris -> 108 baris), v7.6 IWorkerDataService extraction
- WCAG 2.1 aria-live best practices: assertive vs polite untuk real-time content
- Pola umum brownfield assessment system: grading engine assumptions, mobile touch event conflicts

---
*Pitfalls research untuk: Portal HC KPB v14.0 Assessment Enhancement*
*Researched: 2026-04-06*
