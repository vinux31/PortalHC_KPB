# Evaluasi Fitur Pre-Test / Post-Test — Portal HC KPB

**Tanggal:** 2026-06-22
**Branch:** `ITHandoff` (v32.4 Ujian Ulang/Attempt-Retake)
**Lingkup:** Pemetaan as-built + deteksi divergensi untuk alur Pre-Test → training → Post-Test → grading → sertifikat, audit field form assessment, dan logika/validasi.
**Metodologi:** Pemetaan faktual berbasis bukti `file:line` dari 8 sub-sistem (form Create/Edit, linking Pre/Post, lifecycle exam-taking, grading/sertifikat, retake, shuffle/paket, validasi/SignalR, penugasan peserta). Setiap temuan kandidat diverifikasi adversarial; 14 kandidat ditolak (rejected) dan tidak masuk laporan.

> **Catatan scope penting:** Fitur **Inject Assessment** (`InjectBatchAsync`/`PreviewInjectScore`) **TIDAK ADA** di branch `ITHandoff` — fitur tersebut hidup di branch `main` (milestone v32.2, belum di-push ke origin). Lihat temuan **VAL-01**. Audit inject tidak applicable di sini.

---

## Ringkasan Eksekutif

Sistem Pre-Test/Post-Test Portal HC KPB secara **arsitektur dasar selaras** dengan konteks bisnis yang diharapkan: Pre-Test sebagai baseline sebelum training, Post-Test sebagai pengukur peningkatan, opsi `SamePackage` untuk komparasi paket-soal identik, dan penerbitan sertifikat yang dirancang hanya pada kelulusan Post-Test (Pre dipaksa `GenerateCertificate=false` via keputusan D-20). Mesin grading server-authoritative, validasi anti-forgery lengkap di seluruh endpoint POST mutasi, dan broadcast SignalR konsisten dilakukan setelah commit — fondasi keamanan/integritas kuat.

Namun audit menemukan **32 temuan terkonfirmasi** yang membutuhkan perhatian. Terdapat **3 temuan High** yang berdampak langsung pada integritas perilaku ujian/data: (1) flag **Acak Soal/Acak Pilihan ter-reset OFF secara diam-diam** setiap kali admin menyimpan via form Edit (field tidak dirender di Edit, binding checkbox absen → `false`); (2) **cooldown retake dapat melewati `ExamWindowCloseDate`** sehingga retake menghapus sesi live lalu `StartExam` memblok — dead-end destruktif; (3) **auto-sync `SamePackage` tidak terpasang di `ImportPackageQuestions`** (jalur impor massal paling lazim), membuat Post-Test out-of-sync secara senyap.

Pola sistemik yang berulang: **field UI dirender tapi tidak di-persist** (retake config di Create & Edit, `ValidUntil` di cabang standard Edit) — input HC diabaikan diam-diam. Selain itu terdapat **inkonsistensi penegakan aturan antar-jalur** (gate cert `AssessmentType != PreTest` ada di 2 jalur, hilang di 2 jalur grading-time lain; kunci sibling type-aware vs type-agnostic; dedupe MC last-write-wins hanya di satu jalur grading). Banyak temuan Low/Medium bersifat redundansi/penamaan/dead-field yang merupakan utang teknis terdokumentasi (komentar kode kerap mengakui duplikasi). Konteks bisnis sebagian besar **selaras**, dengan divergensi utama pada jalur manual (`AddManualAssessment` selalu `GenerateCertificate=true`) dan penegakan invariant Pre-non-cert yang bertumpu pada default + warning JS, bukan guard domain.

### Hitungan Temuan

**Per Severity:**

| Severity | Audit awal | + Adendum 2026-06-22 | Total |
|----------|-----------|----------------------|-------|
| High     | 3         | +1 (FLOW-04 naik)    | **4** |
| Medium   | 14        | +4 FORM-PP, +1 FLOW-07 naik | **19** |
| Low      | 15        | +3 FORM-PP, −2 (naik) | **16** |
| **Total**| **32**    | **+7 FORM-PP**       | **39** |

*Adendum: +7 temuan `FORM-PP-01..07` (audit form Pre-Post, §5.2.5) + 2 kenaikan severity dari keputusan bisnis (FLOW-04→High, FLOW-07→Medium). Reklasifikasi bukan penambahan.*

**Per Bucket Tugas:**

| Bucket | Jumlah | ID |
|--------|--------|-----|
| 5.1 Flow Mapping (`5.1-flow`) | 9 | FLD-5.2-10, FLOW-01, FLOW-03, FLOW-04 (×2), FLOW-06 (dead-field), FLOW-07 (SamePackage final), FLOW-02, FLOW-05, FLOW-06 (timer), FLOW-07 (write-on-GET), PA-07 |
| 5.2 Field Form (`5.2-field`) | 13 | FLD-5.2-01, -02, -04, -06, -07, -09, -08, E-01, E-03, E-05, E-07, GRD-09, SHUF-ISS-01, SHUF-ISS-04, SHUF-ISS-07, SHUF-ISS-08, PA-02, PA-04, PA-05 |
| 5.3 Logika & Validasi (`5.3-validation`) | 10 | FLD-5.2-05, E-04, E-08, GRD-01, GRD-02, GRD-03, GRD-05, GRD-06, GRD-08, GRD-10, RTK-LOGIC-01, RTK-LOGIC-02, RTK-LOGIC-03, RTK-LOGIC-04, SHUF-ISS-02, SHUF-ISS-03, SHUF-ISS-05, VAL-01, VAL-03, VAL-04, VAL-06, VAL-07, PA-06, PA-08 |

*(Catatan: beberapa ID muncul di kategori berdekatan karena lintas-bucket; klasifikasi mengikuti `task_bucket` tiap temuan.)*

### ⚠️ Catatan Verifikasi & ID Cleanup (re-check 2026-06-22)

Re-check: seluruh ID di body diekstrak & di-dedupe per-family terhadap mapping 6-fase. Hasil:

**1. Coverage mapping = LENGKAP.** Setiap temuan ber-ID terdokumentasi sudah diassign ke salah satu fase P1–P6 (cek per-family di bawah); **VAL-01** sengaja out-of-scope (branch main). **Tidak ada temuan yang tak ter-fase.**

**2. Hitungan headline UNDERSTATED.** Tally synthesizer ("32"→adendum "39") **lebih kecil dari jumlah ID yang benar-benar didokumentasikan di body**. Tally distinct sebenarnya (in-scope, exclude VAL-01):

| Family | Distinct in-scope | Catatan |
|--------|-------------------|---------|
| E- | 6 | E-01,03,04,05,07,08 |
| FLD-5.2- | 9 | 01,02,04,05,06,07,08,09,10 |
| GRD- | 8 | 01,02,03,05,06,08,09,10 |
| RTK-LOGIC- | 3 | 01,02,03 (RTK-LOGIC-04 ≡ FLD-5.2-08, alias) |
| SHUF-ISS- | 7 | 01,02,03,04,05,07,08 |
| FLOW- | 10 | 7 nomor, 3 dipakai ganda (lihat #3) |
| PA- | 6 | 02,04,05,06,07,08 |
| VAL- | 4 | 03,04,06,07 (VAL-01 out) |
| FORM-PP- | 7 | 01–07 |
| **TOTAL in-scope** | **≈60** | (synth headline 39 → undercount ~21) |

Angka "39" tetap dipertahankan di adendum sebagai *tally synth*, TAPI jumlah temuan kerja sebenarnya ≈ **60 in-scope**. Distribusi severity ikut bergeser ke Low (mayoritas yang tak ter-tally = Low).

**3. Tabrakan ID (WAJIB pakai kanonik sebelum `/gsd-plan-phase`):** tiga nomor FLOW dipakai untuk DUA temuan berbeda. Kanonik:
- `FLOW-04` = **gating Pre→Post (HIGH, P5)** [tetap]; token `TempData.Peek` → **rename `FLOW-08` (P6)**.
- `FLOW-06` = **AssessmentPhase dead-field (P6)** [tetap]; `serverTimerExpired` dihitung 2× → **rename `FLOW-09` (P6)**.
- `FLOW-07` = **SamePackage final/editable (P3)** [tetap]; auto-transisi write-on-GET → **rename `FLOW-10` (P6)**.
- `RTK-LOGIC-04` ≡ `FLD-5.2-08` (satu temuan, dua label; kanonik = **FLD-5.2-08**, P1).

**4. Gap penomoran** (tak ada E-02/06, FLD-5.2-03, GRD-04/07, SHUF-ISS-06, PA-01/03, VAL-02/05) = **14 kandidat yang DITOLAK** saat verifikasi adversarial → wajar, bukan miss.

**Verdict re-check:** mapping & isi BENAR + lengkap; hanya metadata (hitungan headline + keunikan 3 ID FLOW) yang perlu dirapikan — sudah diklarifikasi di sini. Saat plan-phase, P6 memuat FLOW-08/09/10 (eks-tabrakan).

---

## 5.1 Pemetaan Alur Kerja (Flow Mapping)

### 5.1.1 Diagram Alur Tekstual Berurutan (End-to-End)

Alur lengkap dirangkai dari lima sub-sistem (Linking, Lifecycle, Grading/Sertifikat, Retake, Penugasan Peserta):

```
[A. AUTHORING / PEMBUATAN GRUP]
  A1. HC submit CreateAssessment, AssessmentTypeInput='PrePostTest'  (AssessmentAdminController.cs:925)
        └─ isPrePostMode=true → lewati validasi Schedule/Duration standar
  A2. Validasi jadwal: PostSchedule > PreSchedule (D-06/T-297-02)       (:1067-1074)
  A3. Buat Pre sessions per UserId (AssessmentType='PreTest',
        GenerateCertificate=false [D-20]) → SaveChanges (dapat Id)      (:1241-1271)
  A4. linkedGroupId = preSessions[0].Id  (satu nilai grup utk SEMUA peserta)  (:1273)
  A5. Buat Post sessions (AssessmentType='PostTest',
        GenerateCertificate=pilihan HC [D-21], SamePackage=flag,
        RenewsSessionId hanya di Post [D-24]) → SaveChanges             (:1277-1308)
  A6. Cross-link 1:1: Pre[i].LinkedSessionId=Post[i].Id & sebaliknya;
        LinkedGroupId disetel sama di kedua sisi → Commit tx            (:1310-1318)
  A7. (SamePackage=true) Sinkron paket Pre→Post via SyncPackagesToPost
        (deep-clone paket+soal+opsi); ManagePackages Post di-LOCK       (:5722-5730, 5793-5851)

[B. PENUGASAN PESERTA]
  B1. Tambah peserta (Pre-Post Edit D-31): buat newPre+newPost cross-link  (:1882-1996)
        ⚠ newPost TIDAK mewarisi SamePackage (PA-02)
  B2. Hapus peserta (Pre-Post Edit D-32): hard-delete pasangan Pre+Post,
        guard tolak jika InProgress/Completed                          (:1888-1937)
        ⚠ guard tak menolak Abandoned / sesi ber-attempt-history (PA-06)

[C. LIFECYCLE EXAM-TAKING — IDENTIK untuk Pre & Post]
  C1. Lobby: Upcoming→Open in-memory saat Schedule<=now WIB; persist di StartExam  (CMPController.cs:249-250, 918-927)
  C2. VerifyToken POST: AccessTokenMatches (ToUpper, auto-heal); TempData TokenVerified  (:863-900)
  C3. StartExam GET: gerbang berlapis (authz→auto Open→tolak Upcoming/Completed→token
        gate→ExamWindowCloseDate→Duration>0→tolak Abandoned→WSE-01 paket-kosong)  (:904-999)
        ⚠ write-on-GET: persist Open/InProgress di GET (FLOW-07)
  C4. justStarted: Status=InProgress + StartedAt=UtcNow; broadcast workerStarted  (:977-1019)
  C5. Bangun assignment paket + shuffle (UserPackageAssignment) atau resume        (:1025-1126)
        ⚠ AssessmentPackageId = sentinel (paket pertama), bukan paket aktual (PA-05)
  C6. Auto-save: SaveAnswer (upsert+broadcast); UpdateSessionProgress (clamp ElapsedSeconds)  (:360-484)
        ⚠ clamp tanpa ExtraTimeMinutes (FLOW-02)
  C7. Timer server-authoritative 2-tier (no-grace + 120s grace), +ExtraTime         (:1623-1628, 4547-4634)
  C8. ExamSummary review + AutoSubmitToken one-shot bila timer habis                (:1543-1571)
  C9. SubmitExam POST: authz→STAT-01 anti-resurrection→block incomplete→timer gate
        →persist PackageUserResponses SEBELUM grading                              (:1576-1665, 1751-1753)

[D. GRADING & TRANSISI STATUS]
  D1. GradeAndCompleteAsync grade dari DB (bukan form POST):
        MC=opsi IsCorrect (dedupe last-write-wins by SubmittedAt);
        MA=all-or-nothing SetEquals; Essay=skor 0 sementara                        (GradingService.cs:95-142)
  D2. finalPercentage=(int)(totalScore/maxScore*100); isPassed=pct>=PassPercentage  (:144-146)
  D3a. Ada Essay → Status=PendingGrading, IsPassed=null, cert/TR DITUNDA            (:206-248)
  D3b. Non-essay → Status=Completed, IsPassed, Progress=100, CompletedAt            (:250-273)

[E. PENERBITAN SERTIFIKAT]
  E1. Gate: GenerateCertificate && isPassed → mint NomorSertifikat
        KPB/{seq:D3}/{ROMAN}/{YEAR}, retry 3x WHERE NomorSertifikat==null           (:287-319)
        ⚠ gate TIDAK cek AssessmentType!=PreTest (GRD-01)
        ⚠ ValidUntil TIDAK di-set di sini; jika kosong saat create → cert tanpa expiry (GRD-06)
        ⚠ GetNextSeqAsync MAX+1 non-atomic; race → cert gagal terbit (GRD-08)
  E2. (Essay) FinalizeEssayGrading manual HC → Compute + cert block sama            (AssessmentAdminController.cs:3677-3911)
        ⚠ tidak ada timeout/rekonsiliasi bila HC tak pernah finalize (GRD-05)
  E3. Download/View cert: PendingGrading→ditunda; !GenerateCertificate→NotFound;
        IsPassed!=true→error; render NomorSertifikat + ValidUntil ('Berlaku Hingga') (CMPController.cs:1856-1882, 2122-2135)

[F. RETAKE / UJIAN ULANG (opsional, v32.4)]
  F1. Entry HC ResetAssessment ATAU worker self-service RetakeExam (CanRetakeAsync)  (AssessmentAdminController.cs:4248 / CMPController.cs:2527)
  F2. RetakeService claim-atomik: Status→Open, nol-kan Score/IsPassed/Progress/...   (RetakeService.cs:101-120)
        ⚠ NomorSertifikat TIDAK ikut dinol-kan (RTK-LOGIC-01)
  F3. Snapshot arsip per-soal (bila wasCompleted) ke AttemptHistory + Archive        (:125-171)
  F4. Delete live (responses/assignment/ET) + Commit; audit + SignalR sessionReset   (:174-220)
  F5. Re-entry StartExam → ExamWindowCloseDate ditegakkan (+7h WIB)                   (CMPController.cs:955-960)
        ⚠ cooldown bisa lewat window → dead-end destruktif (RTK-LOGIC-02, HIGH)

[G. ANALITIK / KOMPARASI PRE-POST]
  G1. CMP pair skor per-peserta via LinkedSessionId (trend)                          (CMPController.cs:267-271, 2786-2822)
  G2. Dashboard pair grup via LinkedGroupId + FirstOrDefault                         (:292-308)
        ⚠ tiga jalur pairing berdampingan (LinkedGroupId/UserId/LinkedSessionId) (FLOW-01)
```

### 5.1.2 Tabel Transisi Status (State Machine)

Kolom `AssessmentSession.Status` adalah string; nilai kanonik di `AssessmentConstants.AssessmentStatus` (7 status). Komentar inline `AssessmentSession.cs:20` hanya menyebut 3 dari 7 (**FLOW-05**).

| Dari | Ke | Pemicu | Lokasi | Catatan |
|------|-----|--------|--------|---------|
| (create) | `Upcoming` | PrePost mode dipaksa Upcoming | AssessmentAdminController.cs:1143-1146 | Standard default `Open` |
| `Upcoming` | `Open` | Schedule <= now WIB (lobby/StartExam) | CMPController.cs:249-250, 918-927 | Write-on-GET (FLOW-07) |
| `Open` | `InProgress` | StartExam first load (StartedAt==null) | CMPController.cs:1001-1007 | Set StartedAt=UtcNow |
| `InProgress` | `Completed` | SubmitExam non-essay → grading | GradingService.cs:250-273 | + Score + IsPassed + CompletedAt |
| `InProgress` | `PendingGrading` ("Menunggu Penilaian") | SubmitExam ada Essay | GradingService.cs:206-248 | IsPassed=null, cert ditunda |
| `PendingGrading` | `Completed` | FinalizeEssayGrading (HC manual) | AssessmentAdminController.cs:3677-3911 | Tak ada auto-timeout (GRD-05) |
| `InProgress`/`Open` | `Abandoned` | AbandonExam (worker sendiri) | CMPController.cs:1286-1291 | Reachable; lolos guard hapus D-32 (PA-06) |
| (any non-Cancelled/Open) | `Open` | Retake/Reset (claim-atomik) | RetakeService.cs:101-120 | Nol-kan skor; NomorSertifikat tetap (RTK-LOGIC-01) |
| (any) | `Cancelled` | Admin cancel | AssessmentAdminController.cs:4423 | Terminal |

### 5.1.3 Penilaian Alignment dengan Business-Process & Divergensi/Asumsi

| Aspek Business-Process Diharapkan | Status As-Built | Verdict |
|-----------------------------------|-----------------|---------|
| Pre-Test sebelum training (baseline) | Pre/Post = sesi independen via LinkedGroupId/LinkedSessionId; `PostSchedule>PreSchedule` divalidasi | **DIVERGEN (dikonfirmasi bisnis 2026-06-22 → BUG)** — bisnis WAJIB Pre dulu, tapi pelaksanaan tidak di-gate; Post bisa mulai tanpa Pre Completed (FLOW-04, naik HIGH) |
| Post-Test setelah training (peningkatan) | Post bisa dibuat tanpa Pre di mode Standard; tidak ada penegakan Post butuh Pre | **DIVERGEN** (FLOW-03) |
| Paket soal sama untuk komparasi | `SamePackage` deep-clone Pre→Post + lock | **SELARAS** — tapi shuffle ON bisa kaburkan komparasi (SHUF-ISS-04); sync absen di Import (SHUF-ISS-03) |
| Sertifikat terbit pada kelulusan Post-Test | Pre dipaksa `GenerateCertificate=false` (D-20); cert gate `GenerateCertificate && isPassed` | **SEBAGIAN SELARAS** — gate grading-time tak konsisten cek PreTest (GRD-01); manual selalu cert=true (FLD-5.2-02); penegakan Pre-non-cert hanya default+warning JS (FLD-5.2-10) |

**Asumsi yang ditandai → DIKONFIRMASI PEMILIK BISNIS (2026-06-22):**
- **~~Asumsi A~~ → KEPUTUSAN BISNIS (a): WAJIB Pre dulu.** Peserta HARUS menyelesaikan Pre-Test sebelum boleh mengerjakan Post-Test. Status as-built TIDAK menegakkan ini (StartExam Post tak gate Pre Completed) → **FLOW-04 menjadi BUG nyata, dinaikkan ke HIGH.** Fix: blok `StartExam` Post bila Pre belum `Completed`.
- **Asumsi B (tetap by-design):** `AssessmentPhase` ('Phase1'/'Phase2') yang disebut di XML-doc TIDAK pernah diimplementasikan — linking nyata bertumpu pada `AssessmentType`+`LinkedGroupId`+`LinkedSessionId` (FLOW-06, dead-field).
- **~~Asumsi C~~ → KEPUTUSAN BISNIS (b): SamePackage FLEKSIBEL.** Aturan "paket Post sama dengan Pre" boleh diubah setelah grup dibuat. Status as-built `final-at-create` (tak ada toggle pasca-create) → **FLOW-07 menjadi BUG nyata, dinaikkan ke MEDIUM.** Fix: tambah toggle ubah `SamePackage` pada Post existing + guard pra-peserta + sync/unsync paket.
- **KEPUTUSAN BISNIS (c): fasilitas "soal Post = Pre" SUDAH ADA** — ya, via `SamePackage` (deep-clone Pre→Post + lock + auto-sync di 6 call-site). Namun belum lengkap/konsisten: bocor di jalur Import (**SHUF-ISS-03, HIGH**), lock hanya view-side (**SHUF-ISS-02**), peserta-baru tak warisi (**PA-02**), dan shuffle ON mengaburkan komparasi (**SHUF-ISS-04**).

---

## 5.2 Audit Field Form Assessment

Terdapat **DUA jalur pembuatan** dengan controller & view berbeda:
- **ONLINE:** `AssessmentAdminController.CreateAssessment` (cs:861-1676) ↔ `CreateAssessment.cshtml`; edit via `EditAssessment` ↔ `EditAssessment.cshtml`.
- **MANUAL:** `TrainingAdminController.AddManualAssessment` (cs:689-774) ↔ `AddManualAssessment.cshtml`; edit via `EditManualAssessment`.

### 5.2.1 Tabel Field

| Field | Lokasi (view ; controller ; model) | Tujuan | Default | Interaksi | Catatan |
|-------|-----------------------------------|--------|---------|-----------|---------|
| **UserIds** | CreateAssessment.cshtml:319 ; cs:865,907-922,1464 | Daftar peserta (1 sesi/user online) | kosong (min 1, max 50) | AssessmentTypeInput, ProtonTrackId gate | Online checkbox max 50; manual `WorkerCerts[]` TomSelect max 20 |
| **Title** | CreateAssessment.cshtml:189 ; cs:876-887,995-1007 ; VM:8-10 | Nama; auto-pairing & deteksi duplikat | kosong (wajib) | ConfirmDuplicateTitle, LinkedGroupId, preTestWarning JS | Auto-detect counterpart via pola judul (rapuh, FLD-5.2-10) |
| **Category** | CreateAssessment.cshtml:130 ; cs:913-916,1193,1371 | Kategori; cabang Proton | kosong (wajib) | ProtonTrackId/TahunKe, SubKategori (manual) | Manual punya SubKategori dependent-dropdown |
| **AssessmentTypeInput** | CreateAssessment.cshtml:215-218 ; cs:867,925,1041-1044,1237 | Mode: Standard / Pre-Post | Standard | Pre*/Post*, SamePackage, Status, GenerateCertificate | **Penamaan membingungkan** vs kolom DB AssessmentType (FLD-5.2-01) |
| **ProtonTrackId** | CreateAssessment.cshtml:235 ; cs:1193-1218,1371-1442 | FK Proton; gate eligibility | kosong | Category, TahunKe (T3 Duration=0) | TahunKe resolve dari track; T3=interview, Duration dipaksa 0 |
| **Schedule** | CreateAssessment.cshtml:424 ; cs:934-945,1471 | Jadwal sesi (standard) | kosong (wajib std) | EWCD>=Schedule, PreSchedule override | Manual: Schedule = CompletedAt (REDUNDAN, FLD-5.2-04) |
| **DurationMinutes** | CreateAssessment.cshtml:400 ; cs:948-964,1472 | Lama ujian | kosong/0 (max 480) | AssessmentTypeInput, Proton T3 (dipaksa 0) | Tidak ada di manual |
| **ExamWindowCloseDate** | CreateAssessment.cshtml:425 ; cs:972-984,1482 | Batas mulai/lanjut ujian | kosong (wajib std, >=Schedule) | Schedule, AccessToken, RetakeCooldown (RTK-LOGIC-02) | Beda peran dari token |
| **PreSchedule/PreDuration/PreEWCD** | CreateAssessment.cshtml:437-446 ; cs:868,1051-1078,1247-1249 | Konfig Pre (Pre-Post) | kosong (wajib PrePost) | Post* (Post>Pre), SamePackage | Pre selalu GenerateCertificate=false (D-20) |
| **PostSchedule/PostDuration/PostEWCD** | CreateAssessment.cshtml:460-469 ; cs:869,1059-1074,1283-1285 | Konfig Post (Pre-Post) | kosong (wajib PrePost) | Pre*, GenerateCertificate, Renewal FK | Post warisi GenerateCertificate dari pilihan HC |
| **SamePackage** | CreateAssessment.cshtml:475 ; cs:870,1302 ; model:198-203 | Paket Post=Pre utk komparasi | false | Shuffle (kaburkan komparasi), ManagePackages lock | Hanya ditulis ke Post; final-at-create (FLOW-07) |
| **Status** | CreateAssessment.cshtml:498 ; cs:927-931,1143-1146 | Status awal | 'Open' (PrePost dipaksa Upcoming) | AssessmentTypeInput | Manual di-hardcode Completed |
| **PassPercentage** | CreateAssessment.cshtml:512 ; cs:966-970,1477 ; VM:20-22 | Ambang lulus | 70 | Score & IsPassed (manual tak cross-validated) | Ada di kedua jalur |
| **IsTokenRequired** | CreateAssessment.cshtml:522 ; cs:891-904,1475 | Aktifkan token | false | AccessToken (wajib jika true), EWCD | Jika false → AccessToken dikosongkan paksa |
| **AccessToken** | CreateAssessment.cshtml:528 ; cs:1130-1133 ; model:93-102 | Token bersama 1 batch ruang | kosong | IsTokenRequired, EWCD | By-design shared token; di-uppercase; tidak ada di manual |
| **ShuffleQuestions** | CreateAssessment.cshtml:542 ; cs:1479,1253,1289 ; model:38-39 | Acak urutan soal | **true** | SamePackage, ShuffleOptions | **Tidak dirender di Edit → ter-reset OFF (E-01, HIGH)** |
| **ShuffleOptions** | CreateAssessment.cshtml:547 ; cs:1480,1254,1290 ; model:41-42 | Acak opsi jawaban | **true** | SamePackage, ShuffleQuestions | **Tidak dirender di Edit → ter-reset OFF (E-01, HIGH)** |
| **AllowRetake** | CreateAssessment.cshtml:558 ; model:45-46 | Izinkan ujian ulang | false | MaxAttempts, RetakeCooldownHours | **Di-input tapi tidak disalin ke sesi baru (FLD-5.2-08/RTK-LOGIC-04)** |
| **MaxAttempts** | CreateAssessment.cshtml:565 ; model:48-50 | Batas percobaan | 2 (1-5) | AllowRetake | Sama: bind-but-drop di Create; no-op di Edit (E-03) |
| **RetakeCooldownHours** | CreateAssessment.cshtml:571 ; model:52-54 | Jeda antar percobaan | 24 (0-168) | AllowRetake | 0=tanpa jeda; bisa lewat EWCD (RTK-LOGIC-02) |
| **GenerateCertificate** | CreateAssessment.cshtml:591 ; cs:1481,1257,1293 ; model:35-36 | Terbitkan sertifikat | false (online) | AssessmentType (Pre=false D-20), ValidUntil, Title JS | **Manual di-hardcode true (FLD-5.2-02)** |
| **ValidUntil** | CreateAssessment.cshtml:621 ; cs:985-991,1483 ; VM:32-34 | Masa berlaku sertifikat | kosong=permanen | GenerateCertificate, Renewal, CertificateType | Label beda online/manual (FLD-5.2-06); tak dipropagasi cabang std Edit (E-05) |
| **AllowAnswerReview** | CreateAssessment.cshtml:638 ; cs:1478,1252,1288 ; model:32-33 | Izinkan review jawaban | true | AllowRetake (tier feedback v32.4) | Tidak ada di manual |
| **RenewsSessionId/TrainingId/RenewalFkMap** | CreateAssessment.cshtml:104-116 ; cs:866,986-1036,1300-1301 | Tandai perpanjangan cert | null | ValidUntil (wajib renewal), Title (skip dup), Pre-Post (FK hanya Post) | XOR; bulk tak boleh campur tipe |
| **ConfirmDuplicateTitle** | CreateAssessment.cshtml:204 ; cs:871,995-1007 | Override soft-block judul kembar | false | Title, isRenewalMode | Bisa bypass anti double-cert (VAL-04) |
| **Score** (manual) | AddManualAssessment.cshtml:162 ; VM:16-18 ; cs:744 | Nilai hasil (manual) | null | PassPercentage & IsPassed (tak cross-validated) | Online di-grade saat SubmitExam |
| **IsPassed** (manual) | AddManualAssessment.cshtml:189 ; VM:24-25 ; cs:746 | Penentuan lulus manual HC | true (checked) | Score, PassPercentage, GenerateCertificate | HC bisa Lulus walau Score<Pass (FLD-5.2-05) |
| **CompletedAt** (manual) | AddManualAssessment.cshtml:212 ; VM:27-30 ; cs:747-748 | Tgl penyelesaian; juga Schedule | DateTime.Today | Schedule (di-set sama), guard duplikat | Label UI ≠ Display (FLD-5.2-04) |
| **Penyelenggara/Kota/SubKategori/CertificateType** (manual) | AddManualAssessment.cshtml:112,121,134,235 ; VM:36-46 | Metadata eksternal | null | Category, ValidUntil | CertificateType overlap ValidUntil (FLD-5.2-09) |
| **WorkerCerts[].UserId/CertificateFile/NomorSertifikat** (manual) | AddManualAssessment.cshtml:331-340 ; VM:58-63 ; cs:733-763 | Upload cert & nomor per peserta | file/nomor opsional | NomorSertifikat (manual free-text vs online auto-gen) | Dua sumber penomoran (FLD-5.2-07) |

### 5.2.2 Redundansi (Terkonfirmasi)

- **FLD-5.2-04 (Low):** Manual `Schedule` di-set sama dengan `CompletedAt` (satu input dipakai dua kolom). `TrainingAdminController.cs:747-748` (create), `:1062` (edit). Backfill `:935` malah pakai `CompletedAt.AddHours(-1)` — konvensi berbeda untuk kolom sama, membuktikan `Schedule` hanya filler pada entry manual.
- **FLD-5.2-07 (Medium):** Penomoran sertifikat dua sumber — manual `NomorSertifikat` input-bebas (`TrainingAdminController.cs:750`, tanpa try/catch `DbUpdateException` di `:765`) vs online auto-generate canonical `KPB/{seq}/{ROMAN}/{YEAR}` (`CertNumberHelper.cs:20-21`). Satu unique filtered index `IX_AssessmentSessions_NomorSertifikat_Unique` untuk SEMUA non-null — kolisi manual→online bisa 500 (bukan pesan ramah).
- **FLD-5.2-09 (Low):** `CertificateType` (Permanent/Annual/3-Year) overlap konseptual dengan `ValidUntil` tanpa sinkronisasi. `DeriveCertificateStatus` (`CertificationManagementViewModel.cs:54-66`) short-circuit "Permanent" sebelum cek ValidUntil → ValidUntil diabaikan diam-diam; "Annual"/"3-Year" tak pernah dibaca konsumen status.
- **GRD-09 (Medium):** Logika scoring MC/MA/Essay diduplikasi di 3+ tempat (`GradeAndCompleteAsync` switch inline `:103-142`+ET `:158-182`; `ComputeScoreAndETInternalAsync` `:384-403`+`:418-435`; `AssessmentScoreAggregator.Compute` `:33-57`). Komentar Aggregator `:13-17` mengklaim "single source of truth/kill-drift" **faktual keliru** — `Compute` hanya dipakai jalur essay-finalize, bukan initial grading.
- **SHUF-ISS-07 (Low):** `hasMismatch`/`referenceCount` dihitung di controller (`:5763-5774`) DAN di view (`ManagePackages.cshtml:72-78`) — dua sumber kebenaran; komentar `:5762` "mirror view L70-81" mengakui duplikasi.

### 5.2.3 Penamaan Membingungkan (Terkonfirmasi)

- **FLD-5.2-01 (Medium):** Label/parameter input **`AssessmentTypeInput`** (nilai Standard/PrePostTest) BEDA dari kolom DB **`AssessmentType`** (nilai Standard/PreTest/PostTest/Manual). Satu input `PrePostTest` fan-out jadi DUA sesi. XML-doc `AssessmentSession.cs:170-171` usang (hanya sebut PreTest/PostTest/null).
- **FLD-5.2-06 (Low):** Label "Tanggal Expired Sertifikat" (`CreateAssessment.cshtml:611`) vs "Berlaku Sampai" (`AddManualAssessment.cshtml:218`) untuk konsep sama (`ValidUntil`).
- **FLD-5.2-04 (Low):** Label UI "Tanggal Pelaksanaan" ≠ `[Display(Name="Tanggal Selesai")]` di VM (`CreateManualAssessmentViewModel.cs:28`).
- **E-07 (Low):** Dua "Edit Assessment" (online vs manual) field-set divergen, judul mirip ("Edit Assessment" vs "Edit Assessment Manual"); `EditAssessment` GET tak filter `IsManualEntry` (`:1684-1686`) → entri manual bisa terbuka di form online.
- **PA-05 (Low):** `UserPackageAssignment.AssessmentPackageId` bernama "paket yang ditugaskan" tapi nyatanya **sentinel** (paket pertama); soal aktual dari `ShuffledQuestionIds` (bisa lintas paket). Komentar BUG-05 (`CMPController.cs:1107-1109`) mengakui inkonsistensi.
- **PA-04 (Low):** XML-doc `LinkedSessionId` (`AssessmentSession.cs:188`) klaim "ON DELETE SET NULL" padahal TIDAK ada FK terkonfigurasi — null-clear sebenarnya app-level (`RecordCascadeDeleteService.cs:235-237`).
- **FLOW-05 (Low):** Komentar inline `Status` (`AssessmentSession.cs:20`) hanya sebut 3 dari 7 status aktual.

### 5.2.4 Logic Antar-Field Bertabrakan (Terkonfirmasi)

- **E-01 (HIGH):** `ShuffleQuestions`/`ShuffleOptions` **tidak dirender** di `EditAssessment.cshtml` (grep "shuffle"/"acak" = 0 — diverifikasi), tetapi POST Edit menulis `sibling.ShuffleQuestions=model.ShuffleQuestions` (`:2084-2085` standard, `:1852-1853` Pre-Post). Karena checkbox absen total (tanpa hidden fallback), binding → `false` → **shuffle ter-reset OFF tiap simpan**, menimpa endpoint `UpdateShuffleSettings`. Silent data-loss perilaku ujian.
- **FLD-5.2-08 / RTK-LOGIC-04 (Medium):** `AllowRetake`/`MaxAttempts`/`RetakeCooldownHours` di-input di form Create (`CreateAssessment.cshtml:558-571`) dan ter-POST, tapi ketiga jalur konstruksi sesi (`:1467-1491` standard, `:1243-1263` Pre, `:1279-1303` Post) TIDAK menyalinnya → selalu jatuh ke default model (false/2/24). Input HC diabaikan diam-diam (bind-but-drop). Bandingkan bulk-add `:2184-2186` yang menyalin eksplisit ("RTK-01").
- **E-03 (Medium):** Field retake juga dirender di `EditAssessment.cshtml:420/427/433` tapi tidak ditulis di POST Edit — no-op; satu-satunya penulis = `UpdateRetakeSettings` (`:5641-5643`) di ManagePackages.
- **E-05 (Medium):** `ValidUntil` dirender editable di `EditAssessment.cshtml:484-490` tapi TIDAK dipropagasi di cabang standard Edit (`:2072-2089` tak menulis `sibling.ValidUntil`); hanya cabang Pre-Post postGroup `:1878`. Field GenerateCertificate & EWCD di card sama JUSTRU dipropagasi → inkonsistensi.
- **PA-02 (Medium):** `newPost` pada tambah-peserta D-31 (`:1968-1989`) tidak mewarisi `SamePackage` → default false → Post peserta-baru tak ter-lock & tak auto-sync, divergen dari peserta lama grup yang sama.
- **SHUF-ISS-08 (Medium):** `DeletePackage` tidak me-renumber `PackageNumber` sisa; `CreatePackage` pakai `existingCount+1` (count-based) → bisa **duplikat PackageNumber**; OFF-path round-robin `OrderBy(PackageNumber)` tanpa `.ThenBy(Id)` → worker bisa bergeser paket lintas reshuffle.
- **SHUF-ISS-01 (Low):** Kunci sibling lock/save shuffle type-agnostic (`:5559-5564`, `:5733-5738`) BEDA dari kunci StartExam/Reshuffle type-aware (`SiblingSessionQuery.cs`). Komentar `:5558` "key identik StartExam/Reshuffle" **faktual SALAH**. Akibat: over-lock (Pre mulai → toggle Post terkunci) — fail-safe, bukan korupsi.
- **SHUF-ISS-04 (Low):** `SamePackage` + `ShuffleQuestions=ON` membuat instrumen Pre/Post tak identik per peserta (undian shuffle independen per session). UI hanya warn untuk kasus Pre-OFF/Post-ON, bukan ON/ON.

---

### 5.2.5 Audit Khusus Form `CreateAssessment` → Mode Pre-Post (temuan user-driven, 2026-06-22)

Permintaan eksplisit: cek field di **Create Assessment > Pre-Post Test** — duplikat, letak salah, membingungkan. Diverifikasi dari source `Views/Admin/CreateAssessment.cshtml` (layout = ground-truth) + handler JS toggle `:1986-2033`. Struktur wizard: **Step 1 Kategori&Judul → Step 2 Peserta → Step 3 Settings (Group A Jadwal&Waktu / B Pengaturan Ujian / C Sertifikat / D Opsi Lainnya) → Step 4 Konfirmasi**. Saat `AssessmentTypeInput=PrePostTest`: `ppt-jadwal-section` (kartu Pre + kartu Post) ditampilkan, `standard-jadwal-section` di-`d-none`, `statusFieldWrapper` di-`d-none` (Status dipaksa `Upcoming`), `prePostCertNote` tampil. **Group B/C/D TIDAK berubah** — tetap satu-instance.

**Akar masalah:** mode Pre-Post membuat **DUA sesi** (Pre & Post), tetapi seluruh Group B/C/D (PassPercentage, Token, Shuffle, Retake, Sertifikat, ValidUntil, Review Jawaban) **hanya satu set kontrol tanpa label scope** — tidak jelas mana berlaku untuk Pre, Post, atau keduanya. Realita kode: PassPercentage/Sertifikat/ValidUntil/Retake efektif hanya relevan untuk **Post**; Shuffle/ReviewJawaban berlaku **keduanya**; Token di-share. Form tidak mengomunikasikan ini.

| ID | Jenis | Severity | Temuan | Bukti |
|----|-------|----------|--------|-------|
| **FORM-PP-01** | letak-salah | Medium | Checkbox **`SamePackage`** terkubur DI DALAM kartu Post (di bawah "Batas Waktu Post"), padahal keputusan tingkat-PASANGAN (relasi Pre↔Post). Terbaca seolah setelan Post saja; sulit ditemukan. | `CreateAssessment.cshtml:475-476` (di dalam `ppt-jadwal-section` kartu Post `:452-478`) |
| **FORM-PP-02** | membingungkan/scope | Medium | Group B/C/D satu-instance untuk DUA sesi tanpa penanda scope. User set "Pass %", "Sertifikat", "Ujian Ulang" tak tahu itu berlaku Pre/Post/keduanya. | `:507-512` PassPct, `:552-576` Retake, `:588-624` Cert/ValidUntil, `:635-641` Review — semua di luar kartu Pre/Post |
| **FORM-PP-03** | duplikat | Medium | Konsep Jadwal/Durasi/BatasWaktu ADA DUA KALI: input standard (`ScheduleDate/Time`, `DurationMinutes`, `ewcd*` + hidden `Schedule`/`ExamWindowCloseDate` combiner) tetap di DOM & ter-POST walau mode Pre-Post (cuma `d-none`), berdampingan dengan `Pre*`/`Post*`. Risiko nilai standard basah/stale ikut terkirim. | std `:382-425`; hidden combiner `:424-425`; Pre `:437-446`; Post `:460-469` |
| **FORM-PP-04** | membingungkan + rusak | Medium | Kontrol **Ujian Ulang** tampil di form Pre-Post, tapi nilainya **tidak disalin** ke sesi Pre/Post saat create (overlap **FLD-5.2-08/RTK-LOGIC-04** bind-but-drop) → input HC menguap diam-diam. Plus retake pada Pre (baseline) secara konsep tak bermakna. | UI `:552-576`; jalur create Pre/Post `:1243-1303` tak menyalin |
| **FORM-PP-05** | membingungkan | Low | **Token** tunggal untuk dua sesi yang jadwalnya berjauhan (Pre lalu Post setelah training). Satu token dipakai bersama Pre+Post membingungkan pengawas ruang. | `:516-533` (Group B), shared utk kedua sesi |
| **FORM-PP-06** | letak/konsistensi | Low | Saat Pre-Post, `statusFieldWrapper` disembunyikan tapi tetangganya `PassPercentage` (satu baris `row g-3`) tetap → baris setengah-kosong asimetris. PassPercentage juga muncul untuk konteks yang mencakup Pre (baseline tak butuh ambang lulus). | `:496-512` (Status & PassPct sebaris); toggle `:2005` |
| **FORM-PP-07** | penamaan | Low | Lihat **FLD-5.2-01**: dropdown "Tipe Assessment" nilai `Standard`/`PrePostTest` (param `AssessmentTypeInput`) ≠ kolom DB `AssessmentType` (`PreTest`/`PostTest`). Satu pilihan fan-out jadi dua tipe sesi. | `:215-217`; XML-doc usang `AssessmentSession.cs:170-171` |

**Rekomendasi tata-letak (ringkas):** (1) pindah `SamePackage` ke header section Pre-Post (dekat pilihan Tipe), bukan di kartu Post; (2) beri **label scope eksplisit** tiap setelan Group B/C/D di mode Pre-Post ("berlaku untuk Post-Test" / "Pre & Post") atau pisah jadi sub-kartu "Setelan Post"; (3) bersihkan input standard tersembunyi dari POST saat Pre-Post (atau jangan kirim `Schedule`/`Duration`/`EWCD` standard); (4) perbaiki bind-but-drop retake (lihat 5.3) sebelum/berbarengan; (5) sembunyikan/relabel Retake & PassPercentage agar jelas tak berlaku ke Pre baseline. Catatan: temuan ini **diverifikasi dari source**, belum dari sesi UI live — bila diinginkan, screenshot UI bisa menyusul.

---

## 5.3 Logika & Validasi

Daftar temuan validation-gap / logic-error / business-mismatch / conflict, severity-tagged, dengan evidence `file:line` dan recommended_fix.

### HIGH

**RTK-LOGIC-02 — Cooldown bisa melewati ExamWindowCloseDate → retake destruktif lalu StartExam memblok (dead-end)** · `conflict`
- **Evidence:** `RetakeService.cs:174-194,232-248`; `RetakeRules.cs:29-52`; `CMPController.cs:956,2470-2489,2537-2554`; `AssessmentAdminController.cs:5616-5660`. Eligibilitas retake TIDAK pernah cek `ExamWindowCloseDate` (grep NO MATCH di RetakeService & RetakeRules). `CanRetake` hanya gate cooldown `nowUtc >= completedAt.AddHours(retakeCooldownHours)`. `RetakeExam` re-check hanya `CanRetakeAsync` lalu `ExecuteAsync` → RemoveRange responses/assignment/ET + null skor/cert/status. `StartExam:956` lalu menolak `now+7h > ExamWindowCloseDate` ("Ujian sudah ditutup").
- **Dampak:** Pekerja gagal dengan cooldown (24-168h) yang mendorong eligibilitas melewati window tutup → retake menghapus pekerjaan live + status Lulus/skor → redirect StartExam → blok → sesi Open kosong non-completable, butuh intervensi HC. Snapshot history tetap tersimpan, tapi sesi LIVE jadi shell.
- **Fix:** Tambahkan gate window di jalur retake SEBELUM destruksi — di `RetakeRules.CanRetake` terima `examWindowCloseDate` dan return false bila `nowLocal > examWindowCloseDate` (konvensi +7h sama dengan StartExam); ATAU minimal di `RetakeService.ExecuteAsync` abort sebelum `RemoveRange`/`ExecuteUpdateAsync` bila window tutup. Plus validasi non-blocking di `UpdateRetakeSettings` saat cooldown > sisa window.

**SHUF-ISS-03 — Auto-sync SamePackage TIDAK terpasang di ImportPackageQuestions** · `logic-error`
- **Evidence:** `AssessmentAdminController.cs:6117-6484` (method `ImportPackageQuestions`, terminal `return RedirectToAction("ManagePackages")` `:6483` TANPA `SyncPackagesToPost` — diverifikasi). Grep `SyncPackagesToPost` = 6 call-site (helper `:5793`, CopyPackagesFromPre `:5866`, CreatePackage `:5907`, DeletePackage `:5992`, CreateQuestion `:6701`, EditQuestion `:6964`, DeleteQuestion `:7057`) — Import TIDAK ada. Pola guard yang hilang terlihat di CreateQuestion `:6691-6704` (`parentSession.AssessmentType=="PreTest"` && `linkedPost.SamePackage` → sync).
- **Dampak:** Invariant yang dijanjikan (`AssessmentSession.cs:201` "package otomatis disalin dari Pre-Test"; banner `ManagePackages.cshtml:34-35`) dilanggar senyap. Skenario lazim (HC impor bank-soal massal ke paket Pre yang punya Post linked SamePackage) → Post menjalankan paket lama/kosong sampai ada edit per-soal berikutnya atau klik manual "Copy dari Pre-Test". Komparasi Pre/Post gagal. Import bersifat terminal → HC bisa berhenti tanpa memicu sync.
- **Fix:** Tambahkan blok auto-sync identik CreateQuestion (`:6691-6704`) sebelum `return :6483`: jika `targetSession?.AssessmentType=="PreTest"` && `LinkedSessionId.HasValue`, ambil linkedPost dan jika `linkedPost.SamePackage` panggil `SyncPackagesToPost`. Idealnya ekstrak helper `SyncToLinkedPostIfSamePackageAsync(preSessionId)` untuk ke-6 jalur.

**E-01 — ShuffleQuestions/ShuffleOptions ter-reset OFF setiap simpan via form Edit** · `logic-error`
- *(detail di 5.2.4)* · **Evidence:** `EditAssessment.cshtml` (0 kemunculan shuffle, diverifikasi); `AssessmentAdminController.cs:2084-2085`/`1852-1853`; model default true `AssessmentSession.cs:39,42`.
- **Fix:** Render `<input asp-for="ShuffleQuestions"/>` & `ShuffleOptions` di EditAssessment.cshtml (populated dari Model); ATAU hapus baris `1852-1853`/`2084-2085` agar shuffle dikelola eksklusif `UpdateShuffleSettings`.

### MEDIUM

**GRD-06 — ValidUntil tidak di-set saat penerbitan cert grading-time; cert bisa terbit tanpa expiry** · `validation-gap`
- **Evidence:** `GradingService.cs:287-319` (ExecuteUpdateAsync hanya set NomorSertifikat, ValidUntil tak disentuh); `AssessmentAdminController.cs:3815-3844` (FinalizeEssayGrading sama); `:985-990` (`ModelState.Remove("ValidUntil")` + wajib HANYA saat renewal); `CMPController.cs:2122-2135` ("Berlaku Hingga" dirender hanya jika `ValidUntil.HasValue`). Cert dengan NomorSertifikat valid + ValidUntil null tercetak TANPA baris masa berlaku.
- **Fix:** Validasi POST Create/Edit (~`:985-990`): bila `GenerateCertificate==true && AssessmentType!="PreTest"`, require `ValidUntil.HasValue`. Komplementer: render fallback "Berlaku Hingga: -" di `CMPController.cs:2129`.

**GRD-08 — GetNextSeqAsync race: sequence MAX+1 non-atomic** · `logic-error`
- **Evidence:** `CertNumberHelper.cs:23-35` (read-compute-write tanpa lock/tx); `GradingService.cs:295-318` (retry max 3x; gagal → LogError, NomorSertifikat NULL padahal Status=Completed+IsPassed sudah commit). Hanya dijaga unique index + retry.
- **Dampak:** Beban tinggi/burst finalize simultan (inject batch/grup serentak) → >3 collision → cert gagal terbit (degraded, bukan duplikat — index melindungi).
- **Fix:** Generasi seq atomic (DB SEQUENCE / `UPDATE...OUTPUT` / `SELECT...WITH(UPDLOCK,HOLDLOCK)`) atau retry tanpa cap keras + backoff. Terapkan ke 3 callsite via helper bersama.

**RTK-LOGIC-01 — HC reset sesi LULUS tidak menghapus NomorSertifikat** · `logic-error`
- **Evidence:** `RetakeService.cs:101-112` (9 kolom dinol-kan, NomorSertifikat tidak — grep 0 match, diverifikasi); `AssessmentAdminController.cs:4248-4327` (ResetAssessment tanpa guard isPassed/cert). Worker path aman (`RetakeRules.cs:45` blok sesi lulus); hanya jalur HC.
- **Dampak:** Sesi inkonsisten (IsPassed=false/null + NomorSertifikat≠null). Download PDF di-guard `IsPassed!=true→NotFound`, jadi bukan cert-leak; tapi nomor menggantung mengisi unique index + `certCount` proxy by `NomorSertifikat!=null` (`:4547`) ter-inflasi.
- **Fix:** Tambah `.SetProperty(r => r.NomorSertifikat, (string?)null)` pada ExecuteUpdateAsync `RetakeService.cs:103-112`.

**RTK-LOGIC-03 — Counting attempt divergen: cap (snapshot-presence) vs warning ManagePackages (GroupBy UserId tanpa filter)** · `conflict`
- **Evidence:** `RetakeService.cs:145-150,237-242` & `CMPController.cs:2472-2475` (snapshot-filtered, sumber kebenaran cap) vs `AssessmentAdminController.cs:5757-5760` (`GroupBy(UserId).Count()` TANPA snapshot filter → `ViewBag.RetakeMaxAttemptsUsedInGroup`). Konsumen `ManagePackages.cshtml:157-163`.
- **Dampak:** Count tak-terfilter ≥ terfilter → warning over-count/false-positive menyesatkan HC (non-blocking).
- **Fix:** Tambahkan predikat snapshot-presence yang sama ke Where sebelum GroupBy di `:5757-5759`; ekstrak helper `CountEraRetakeArchives` untuk keempat situs.

**FLD-5.2-08 / RTK-LOGIC-04 — Retake config bind-but-drop di Create** · `logic-error`/`validation-gap` · *(detail di 5.2.4)*
- **Fix:** Salin eksplisit `AllowRetake = model.AllowRetake`, `MaxAttempts = Math.Clamp(model.MaxAttempts,1,5)`, `RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours,0,168)` di jalur standard `:1467-1491`; Pre set false; Post salin dari model.

**E-03 — Retake config no-op di Edit** · `logic-error` · *(detail 5.2.4)* · **Fix:** Tulis sibling retake di loop standard `:2072-2089` (postGroup utk Pre-Post) dengan guard `ShouldHideRetakeToggle` + Clamp; ATAU jadikan read-only dan arahkan ke ManagePackages.

**E-05 — ValidUntil tidak dipropagasi di cabang standard Edit** · `validation-gap` · *(detail 5.2.4)* · **Fix:** Tambah `sibling.ValidUntil = model.ValidUntil;` di loop sibling standard `:2072-2089`.

**E-04 — Lock 'Completed' tidak konsisten: cabang Pre-Post POST return sebelum guard Status=='Completed'** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:1821-2002` (cabang Pre-Post mutasi metadata lalu return `:2001-2002`) sebelum guard `:2006`. Helper `AssessmentEditEligibility.IsEditableAsync` hanya dipakai EditPesertaAnswers (semantik berlawanan), bukan EditAssessment.
- **Dampak:** Sesi Pre/Post anchor-Completed tetap bisa diubah PassPercentage/GenerateCertificate/ValidUntil padahal sesi single-mode ditolak.
- **Fix:** Pindahkan guard `Status=="Completed"` (idealnya group-wide) sebelum cabang Pre-Post `:1821`, dan tambah cek di GET `:1682`.

**E-08 — EditAssessment GET memuat sesi IsManualEntry tanpa redirect** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:1682-1686` (no IsManualEntry filter) vs `TrainingAdminController.cs:994` (filter benar). Mitigasi: manual selalu Status=Completed → guard POST `:2006-2010` mencegah mutasi (jadi UX trap, bukan korupsi).
- **Fix:** Di GET `:1684-1686`, jika `IsManualEntry` → `RedirectToAction("EditManualAssessment","TrainingAdmin")`.

**FLD-5.2-07 — Penomoran sertifikat dua sumber** · `redundancy` · *(detail 5.2.2)* · **Fix:** Bungkus `SaveChangesAsync` manual (`:765`) dengan try/catch `DbUpdateException`+`IsDuplicateKeyException` → ModelState error ramah; dokumentasikan namespace "KPB/..." dipesan untuk auto-gen.

**FLD-5.2-01 — Label AssessmentTypeInput vs kolom DB AssessmentType** · `naming` · *(detail 5.2.3)* · **Fix:** Rename input → CreationMode; pakai konstanta `AssessmentConstants.AssessmentType.*`; perbarui XML-doc.

**GRD-09 — Logika scoring diduplikasi 3+ tempat** · `redundancy` · *(detail 5.2.2)* · **Fix:** Ekstrak skor per-soal + ET ke fungsi murni bersama; wire semua jalur; karakterisasi-test paritas terutama strategi seleksi MC >1 response.

**PA-02 — newPost D-31 tidak mewarisi SamePackage** · `logic-error` · *(detail 5.2.4)* · **Fix:** `SamePackage = repPost.SamePackage` di inisialisasi newPost (~`:1988`).

**PA-06 — Guard hapus peserta D-32 hanya tolak InProgress/Completed, bukan Abandoned/ber-attempt-history** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:1896-1905` (guard); hard-delete `:1914-1934`. `Abandoned` reachable via `CMPController.cs:1286-1291`. `Open` pasca-reset bawa `AttemptHistory`+`Archive` (`RetakeService.cs:152-170`). Delete D-32 tidak bersihkan `AttemptResponseArchives` (anak by AttemptHistoryId) → orphan.
- **Fix:** Perluas guard tolak/peringatkan bila sesi Abandoned atau punya AttemptHistory/StartedAt; bila tetap hapus, bersihkan `AssessmentAttemptResponseArchives` by AttemptHistoryId.

**SHUF-ISS-02 — Lock SamePackage hanya di View; endpoint POST tak menolak edit Post terkunci** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:5730` (lock view-only); CreatePackage `:5876`/DeletePackage `:5917`/CreateQuestion `:6543`/EditQuestion/DeleteQuestion `:7002` tanpa cek SamePackage. `ManagePackages.cshtml:317-320` tombol "Kelola Soal" unconditional; `ManagePackageQuestions.cshtml` 0 lock.
- **Dampak:** Admin/HC bisa edit paket Post via UI normal → divergen sampai ter-overwrite saat sync Pre berikutnya (data-integrity, bukan privilege-escalation).
- **Fix:** Guard server-side di awal kelima endpoint: jika `AssessmentType=="PostTest" && SamePackage` → tolak. Ekstrak helper `IsSessionEditLocked`.

**SHUF-ISS-08 — DeletePackage tak renumber PackageNumber** · `logic-error` · *(detail 5.2.4)* · **Fix:** Ubah CreatePackage `:5894` ke `MAX(PackageNumber)+1` per session; tambah `.ThenBy(p=>p.Id)` di semua query paket; pertimbangkan filtered unique index `(AssessmentSessionId,PackageNumber)`.

**FLOW-01 — LinkedGroupId vs LinkedSessionId tumpang-tindih; tiga jalur pairing berdampingan** · `conflict`
- **Evidence:** `AssessmentAdminController.cs:1273,1297,1313-1315`; `CMPController.cs:267-271` (FirstOrDefault by LinkedGroupId), `:3511-3517,3705-3711` (by UserId), `:2785-2822` (by LinkedSessionId). Smell nyata: `:292-297` completedPreSessions TIDAK di-filter UserId → bisa pair Pre pekerja lain dengan Post user (hanya di display grouping personal, bukan skor/cert).
- **Fix:** Seragamkan ke satu sumber (LinkedSessionId atau LinkedGroupId+UserId); tambah filter `s.UserId==userId` di `:292-297`; pertimbangkan filtered-unique index `(LinkedGroupId,UserId,AssessmentType)`.

**FLOW-03 — Post-Test bisa dibuat tanpa Pre (mode Standard); auto-detect tempel LinkedGroupId ke Standard (link semu)** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:1487` (Standard hardcoded); `:878-888,7344-7370` (auto-pair regex judul); `:1066-1068` (validasi Post>Pre HANYA di PrePost). Query hilir filter ketat `AssessmentType=="PreTest"/"PostTest"` → sesi Standard ber-LinkedGroupId jadi orphan-link/kosmetik.
- **Fix:** Untuk Standard, jangan turunkan AssessmentType dari pola judul: jika counterpart ditemukan, set AssessmentType eksplisit Pre/Post; ATAU jangan tempel LinkedGroupId bila bukan Pre/Post. Validasi opsional saat membuat "Post Test ..." tanpa Pre.

**FLD-5.2-10 — Penegakan 'Pre tidak terbit sertifikat' hanya default; auto-pair berbasis judul rapuh** · `business-mismatch`
- **Evidence:** `AssessmentAdminController.cs:1257` (D-20 hardcode HANYA PrePost), `:1481` (Standard persist apa adanya), `:3284` (display cek PreTest), `:3815` (essay finalize TANPA cek), `:7344-7370` (regex); `GradingService.cs:287` (TANPA cek) vs `:520` (re-grade ADA cek). `CreateAssessment.cshtml:595-601` warning JS-only (tak blok submit).
- **Dampak:** Sesi Standard berjudul "Pre Test" dengan `GenerateCertificate=true` yang lulus AKAN benar-benar terbit cert di jalur utama — penegakan tidak konsisten.
- **Fix:** Tegakkan di domain via helper `ShouldIssueCertificate(session)` di SEMUA jalur (samakan `:287`/`:3815` dengan `:520`/`:3284`); server-side coercion `GenerateCertificate=false` bila judul/type Pre-Test; ganti auto-pair regex dengan pemasangan eksplisit.

**VAL-01 — Fitur Inject Assessment tidak ada di branch ITHandoff** · `business-mismatch`
- **Evidence:** `git branch --show-current = ITHandoff` (diverifikasi). Grep `InjectBatchAsync|PreviewInjectScore|InjectAssessmentService` = 0 match. Fitur hidup di LOCAL `main` (git ls-tree main menampilkan 6 file `InjectAssessment*`), belum di-push origin.
- **Dampak:** Audit inject tidak applicable di ITHandoff; klaim validasi inject harus diverifikasi di branch main.
- **Fix:** Pisahkan scope per branch; tandai inject out-of-scope untuk ITHandoff; re-route audit inject ke main.

**VAL-03 — Essay kosong hanya diblok client-side (flushEssay); submit server tak menolak** · `validation-gap`
- **Evidence:** `StartExam.cshtml:483,1027` (flushEssay best-effort try/catch, tak blok); `AssessmentHub.cs:134-194` (SaveTextAnswer terima ""); `CMPController.cs:1574-1656` (gate incomplete hitung baris-ada bukan isi-ada, di-SKIP saat serverTimerExpired); `GradingService.cs:146,207-225` (PendingGrading struktural). Downstream `:4926` render "Tidak dijawab" by-design.
- **Dampak:** Aturan "essay wajib diisi" tak ditegakkan server-side untuk submit on-time manual. Data tidak korup (hilir toleran).
- **Fix:** Di cabang `!serverTimerExpired` (`:1630-1656`), hitung essay "terjawab" hanya bila `!IsNullOrWhiteSpace(TextAnswer)`. Auto-submit/deadline pertahankan commit ke PendingGrading.

**VAL-04 — Soft-block judul kembar bisa di-bypass via ConfirmDuplicateTitle; di-skip total pada renewal** · `validation-gap`
- **Evidence:** `AssessmentAdminController.cs:993-1007` (ModelState error hanya bila `!ConfirmDuplicateTitle && !isRenewalModePost`); kontras hard-guard double-renewal `:1014-1028` (unconditional, query IsPassed). `FindTitleDuplicatesAsync` (`AdminBaseController.cs:278-293`) hanya GroupBy Title — TIDAK cek IsPassed/UserId/cert terbit.
- **Dampak:** Tidak ada hard-guard mencegah dua sertifikat aktif untuk peserta+judul sama; bertumpu kewaspadaan operator.
- **Fix:** Hard-guard server-side berbasis (UserId, Title-ternormalisasi) terhadap cert aktif (IsPassed && ValidUntil>=now), TIDAK bisa di-bypass ConfirmDuplicateTitle; pertahankan skip judul renewal tapi tetap jalankan guard kelulusan-aktif. Pertimbangkan filtered unique index DB.

### LOW

**FLD-5.2-02 — GenerateCertificate hardcode true di AddManualAssessment** · `business-mismatch` · **Evidence:** `TrainingAdminController.cs:759` (=true) vs `:746` (IsPassed bisa false); sibling Excel `:1371` sudah `=isPassed` ("#24"). Hilir filter `GenerateCertificate && IsPassed==true` (AdminBaseController:148, HomeController:141/233, dll) → cert non-lulus tak pernah terbit (flag dormant). **Fix:** Ubah `:759` ke `= model.IsPassed`.

**FLD-5.2-04 — Manual Schedule=CompletedAt + label mismatch** · `redundancy` · *(detail 5.2.2/5.2.3)* · **Fix:** Samakan teks label dengan `[Display]`; dokumentasikan mirror.

**FLD-5.2-05 — Manual IsPassed tak divalidasi silang vs Score/PassPercentage** · `validation-gap` · **Evidence:** `CreateManualAssessmentViewModel.cs:16-25` (3 field independen tanpa cross-validation); `TrainingAdminController.cs:744-746` (simpan verbatim). **Fix:** Validasi silang non-blocking saat simpan: jika `(Score>=PassPercentage)!=IsPassed` tampilkan peringatan (jangan auto-override — surface entri historis).

**FLD-5.2-06 — Label ValidUntil beda online/manual** · `naming` · **Fix:** Standarkan satu istilah; tambah `[Display]` pada `AssessmentSession.ValidUntil`.

**FLD-5.2-09 — CertificateType overlap ValidUntil tanpa sinkronisasi** · `redundancy` · **Fix:** Validasi server tolak `Permanent`+`ValidUntil!=null`; auto-derive ValidUntil utk Annual/3-Year; luruskan semantik (jenis vs durasi).

**E-07 — Dua 'Edit Assessment' field-set divergen** · `naming` · *(detail 5.2.3)* · **Fix:** Guard `IsManualEntry` di EditAssessment GET; filter tab Assessment `.Where(a=>!a.IsManualEntry)`; bedakan judul/rute.

**GRD-01 — Cert gate grading-time tak cek AssessmentType!=PreTest** · `business-mismatch` · **Evidence:** `GradingService.cs:287` & `AssessmentAdminController.cs:3815` (tanpa cek) vs `GradingService.cs:520` & `:3284` (ada cek). Laten — invariant hulu (`:1257,1961,1481/1487`) cegah Pre+cert=true. **Fix:** Samakan keempat callsite via helper `ShouldIssueCertificate`.

**GRD-02 — Drift dedupe MC: GradeAndCompleteAsync last-write-wins, Aggregator+ComputeScoreAndETInternalAsync TIDAK** · `logic-error` · **Evidence:** `GradingService.cs:87-90` (OrderByDescending SubmittedAt) vs `AssessmentScoreAggregator.cs:39` (FirstOrDefault) vs `GradingService.cs:390` (mcSel.First). Call-site Aggregator `AssessmentAdminController.cs:3762-3768,4187-4199` feed seluruh response tanpa dedupe. **Fix:** Sentralisasi dedupe last-write-wins; test paritas; opsional filtered unique index PackageUserResponse.

**GRD-03 — Inkonsistensi strategi pemilihan opsi MC antar 4 path** · `logic-error` · **Evidence:** `GradingService.cs:106-113,387-392`; `AssessmentScoreAggregator.cs:39-44,92-95`. Pemicu >1-baris di-guard upsert app-level (jarang). **Fix:** Strategi kanonik "FINAL response per soal" reuse helper; backstop filtered-unique index.

**GRD-05 — PendingGrading tanpa rekonsiliasi/timeout bila HC tak finalize** · `validation-gap` · **Evidence:** `GradingService.cs:206-248`; `AssessmentAdminController.cs:3705-3779` (satu-satunya jalur cert, manual HC); grep `AddHostedService/BackgroundService` = 0. Visibility pasif ada (kartu MenungguPenilaianCount `:3407`). **Fix:** Badge "Menunggu Penilaian (umur N hari)" + reminder ringan; hindari auto-finalize.

**GRD-10 — Idempotensi cert via WHERE NomorSertifikat==null; revoke→re-issue membakar nomor** · `validation-gap` · **Evidence:** `GradingService.cs:300-305,497-501,535-538`; `CertNumberHelper.cs:23-35` (MAX+1, tak reuse revoked). Unique index pada NILAI nomor bukan per-peserta. **Fix:** Simpan nomor revoked & utamakan reuse; hard-guard anti double-cert kompetensi.

**RTK config / VAL-06 — MaxAttempts retroaktif < terpakai = warning non-blocking** · `validation-gap` · **Evidence:** `AssessmentAdminController.cs:5616-5660` (simpan apa pun setelah clamp; komentar `:5640` D-02 retroaktif eksplisit); `RetakeRules.cs:46` tutup logika. By-design terdokumentasi. **Fix:** Opsional toast/konfirmasi di titik POST; pertahankan non-blocking.

**VAL-07 — Hanya 1 cek ModelState.IsValid di controller (7373 baris)** · `validation-gap` · **Evidence:** grep `ModelState.IsValid`=1 (`:1082`); tanpa `[ApiController]`; endpoint scalar pakai if-guard manual. Bukan bug aktif — risiko drift. **Fix:** Bungkus param scalar ke DTO ber-[Range]/[Required] + satu cek; atau ekstrak guard helper.

**SHUF-ISS-01 — Kunci sibling shuffle type-agnostic vs type-aware** · `conflict` · *(detail 5.2.4)* · **Fix:** Pakai `SiblingPrePostAwarePredicate` di lock/save; koreksi komentar `:5558`.

**SHUF-ISS-04 — SamePackage+ShuffleQuestions ON → instrumen Pre/Post tak identik** · `business-mismatch` · *(detail 5.2.4)* · **Fix:** Warning UI saat ON pada Post SamePackage; sarankan OFF untuk komparasi item-level.

**SHUF-ISS-05 — ON-path K=min truncation diam tanpa peringatan UI** · `validation-gap` · **Evidence:** `ShuffleEngine.cs:117` (K=min, diverifikasi); `ShuffleToggleRules.cs:18-20` (warning HANYA saat OFF); reachable via CreateQuestion `:6543`. **Fix:** Tampilkan warning saat ON juga (teks berbeda: ON="soal dipangkas ke K=min"); opsional guard hitung-soal-sibling di CreateQuestion.

**SHUF-ISS-07 — hasMismatch dihitung di controller & view** · `redundancy` · *(detail 5.2.2)* · **Fix:** Satu sumber kebenaran di controller via ViewBag/helper `PackageSizeAnalysis.Compute`.

**FLOW-04 — Urutan Pre-sebelum-Post hanya divalidasi nilai jadwal, bukan pelaksanaan; edit per-fase bisa tulis Post<=Pre bila satu schedule** · `validation-gap` · **Evidence:** `AssessmentAdminController.cs:1067-1068,1825-1829,1861-1880`; `CMPController.cs:904-974` (StartExam tak gate Pre-Completed). Ter-mitigasi UI (hidden PreSchedule selalu terkirim). **Fix:** Bandingkan nilai EFEKTIF (`effPost ?? stored`) di `:1825`; konfirmasi pemilik bisnis utk gating pelaksanaan.

**FLOW-06 (dead-field) — AssessmentPhase tak pernah di-set/dibaca** · `redundancy` · **Evidence:** `AssessmentSession.cs:175-178` (deklarasi); `Migrations/...AddAssessmentV14Columns.cs` (persist); grep di Controllers/Services/Views = 0. **Fix:** Drop kolom (nullable, aman) ATAU tandai RESERVED di XML-doc.

**FLOW-07 (SamePackage final) — Tidak dapat diubah setelah grup dibuat** · `business-mismatch` · **Evidence:** `AssessmentAdminController.cs:870` (param hanya di Create); grep SamePackage 9 kemunculan pasca-create semua READ. **Fix:** Tambah toggle di Post existing dengan guard pra-peserta; ATAU dokumentasikan final-at-create.

**FLOW-02 — UpdateSessionProgress clamp ElapsedSeconds tanpa ExtraTimeMinutes** · `logic-error` · **Evidence:** `CMPController.cs:469` (tanpa extra) vs `:1175,1548,1626,4590-4592` (dengan). Resume ter-mitigasi `Math.Max` wall-clock; dampak nyata = export "Durasi Aktual" (`AssessmentAdminController.cs:4831`) under-report. **Fix:** Samakan clamp; ekstrak helper `AllowedExamSeconds`.

**FLOW-04 (token) — Token gate StartExam pakai TempData.Peek (non-consume)** · `logic-error` · **Evidence:** `CMPController.cs:945-953` (Peek); re-arm di RetakeExam `:2552-2553` & ResetAssessment `:4318-4319`. By-design + dimitigasi. **Fix:** Opsional ganti dengan kolom server-authoritative `TokenVerifiedAt`.

**FLOW-05 — Komentar Status hanya sebut 3 dari 7** · `naming` · *(detail 5.2.3)* · **Fix:** Ganti komentar `AssessmentSession.cs:20` rujuk `AssessmentConstants.AssessmentStatus`.

**FLOW-06 (timer) — serverTimerExpired dihitung dua kali di SubmitExam** · `redundancy` · **Evidence:** `CMPController.cs:1623-1628` (inline) & `:4589-4593` (EnsureCanSubmitExamAsync). **Fix:** Ekstrak pure helper `ComputeExamTiming`/`IsServerTimerExpired`.

**FLOW-07 (write-on-GET) — Auto-transisi dipersist pada GET StartExam** · `validation-gap` · **Evidence:** `CMPController.cs:903 [HttpGet]`; `:917-927,1001-1007` (SaveChanges). Ter-lindung [Authorize]+owner-check+idempoten time-gated. **Fix:** Pindah side-effect ke POST ber-antiforgery; atau Cache-Control no-store + dokumentasikan.

**PA-04 — Doc-comment LinkedSessionId klaim FK SET NULL** · `conflict` · *(detail 5.2.3)* · **Fix:** Koreksi XML-doc; null-clear app-level di RecordCascadeDeleteService.

**PA-05 — AssessmentPackageId sentinel** · `naming` · *(detail 5.2.3)* · **Fix:** Rename `SeedPackageId`/`PrimaryPackageId` + XML-doc; rujuk-silang ShuffledQuestionIds.

**PA-07 — Asimetri manajemen peserta: standard hanya tambah, Pre-Post tambah+hapus** · `conflict` · **Evidence:** `AssessmentAdminController.cs:1882-1937` (Pre-Post add+remove) vs `:2128-2244` (standard add saja); `:1737-1766` (CanDelete=false hardcoded standard); view `EditAssessment.cshtml:692` rujuk action `DeleteAssessmentPeserta` yang TIDAK ADA (dead ref, inert). **Fix:** Samakan CanDelete; implementasi `DeleteAssessmentPeserta`; ATAU hapus tombol mati.

**PA-08 — Dedup tambah-peserta D-31 basis berbeda dari standard** · `redundancy` · **Evidence:** `:1885-1886` (dedup hanya preGroup, tanpa .Distinct()) vs `:2139-2151` (query DB seluruh sibling + .Distinct() ganda). **Fix:** Tambah .Distinct() + dedup union Pre+Post user; ekstrak helper bersama.

---

## Rekomendasi Milestone

Temuan dikelompokkan menjadi kandidat fase milestone baru berdasarkan kohesi domain & dependensi. Prioritas: fase yang memuat temuan High didahulukan.

> **PEMBARUAN 2026-06-22 (jawaban bisnis + temuan form):** SCOPE milestone = **SEMUA temuan** (32 awal + 7 `FORM-PP` form Pre-Post + 2 kenaikan severity). Total efektif **39 temuan** (4 High, 19 Medium, 16 Low). Severity diperbarui: **FLOW-04 → HIGH** (Pre-wajib-dulu, gating pelaksanaan), **FLOW-07 → MEDIUM** (SamePackage wajib bisa diubah). Ditambah **Fase Kandidat 8** untuk redesign tata-letak form Pre-Post.

### ✅ STRUKTUR FINAL MILESTONE — Konsolidasi 6 Fase (disetujui pemilik 2026-06-22)

Semua **39 temuan** dipetakan ke 6 fase (VAL-01 out-of-scope → branch main). Fase yang beririsan domain/file digabung. Tanda 🔴 = memuat temuan High.

**P1 — Form Create/Edit: Persistensi Field + UX Pre-Post** 🔴 (~14)
> Gabungan kandidat 1 + 8. Domain: `CreateAssessment.cshtml`/`EditAssessment.cshtml` + binding controller. Pola "field dirender tapi tak tersimpan" + tata-letak Pre-Post.
> **E-01🔴** shuffle reset-OFF · **FLD-5.2-08/RTK-LOGIC-04** retake bind-but-drop Create · **E-03** retake no-op Edit · **E-05** ValidUntil cabang std Edit · **E-04** lock Completed Pre-Post · **E-08** redirect manual entry · **E-07** dua Edit form · **FORM-PP-01..06** (SamePackage letak, scope-label, input-standard-dobel, retake tampil, token, baris asimetris) · **FORM-PP-07/FLD-5.2-01** rename AssessmentTypeInput.

**P2 — Retake Lifecycle Hardening** 🔴 (~5)
> Domain `RetakeService`/`RetakeRules`/`CMPController`.
> **RTK-LOGIC-02🔴** cooldown lewat ExamWindow=dead-end · **RTK-LOGIC-01** reset tak hapus NomorSertifikat · **RTK-LOGIC-03** counting divergen · **PA-06** guard hapus Abandoned/attempt-history · **VAL-06** MaxAttempts retroaktif warning.

**P3 — SamePackage & Shuffle Integrity** 🔴 (~9)
> Domain `ManagePackages`/`ShuffleEngine`/sync paket.
> **SHUF-ISS-03🔴** sync absen di Import · **FLOW-07** toggle SamePackage editable (keputusan bisnis b) · **SHUF-ISS-02** lock view-only · **PA-02** newPost tak warisi · **SHUF-ISS-08** PackageNumber renumber · **SHUF-ISS-01** kunci sibling type-agnostic · **SHUF-ISS-04** SamePackage+Shuffle ON · **SHUF-ISS-05** K=min truncation warning · **SHUF-ISS-07** hasMismatch dobel.

**P4 — Certificate Issuance Consistency** (~10)
> Konsolidasi aturan terbit cert ke satu helper `ShouldIssueCertificate` + kelengkapan data.
> **GRD-06** ValidUntil kosong saat issue · **GRD-08** race GetNextSeq · **GRD-01** gate AssessmentType laten · **GRD-10** revoke/re-issue burn nomor · **FLD-5.2-10** penegakan Pre-non-cert · **FLD-5.2-07** dua sumber penomoran · **FLD-5.2-02** manual cert hardcode true · **VAL-04** anti double-cert · **FLD-5.2-09** CertificateType overlap ValidUntil · **GRD-05** PendingGrading tanpa timeout.

**P5 — Grading De-dup + Flow/Linking + Gating Pre→Post** 🔴 (~10)
> Gabungan kandidat 5 + 6. Hapus duplikasi scoring + integritas linking + **gate Pre-wajib-dulu**.
> **FLOW-04🔴** gating Post butuh Pre Completed (keputusan bisnis a) · **GRD-09** scoring dobel 3+ · **GRD-02** dedupe MC drift · **GRD-03** strategi seleksi MC 4-path · **FLOW-01** pairing 3 jalur · **FLOW-03** Post tanpa Pre + link semu · **FLOW-02** clamp tanpa ExtraTime · **PA-07** asimetri manajemen peserta · **PA-08** dedup D-31 · **VAL-03** essay server-side validation.

**P6 — Cosmetic / Naming / Tech-Debt Cleanup** (~9 low, no-migration)
> **FLD-5.2-04** Schedule mirror+label · **FLD-5.2-05** IsPassed cross-validation · **FLD-5.2-06** label ValidUntil · **FLOW-05** komentar Status · **FLOW-06** dead-field AssessmentPhase · **FLOW-09** (eks FLOW-06-timer) redundansi timing · **FLOW-08** (eks FLOW-04-token) TempData · **FLOW-10** (eks FLOW-07-write-on-GET) side-effect GET · **PA-04** doc FK · **PA-05** sentinel naming · **VAL-07** konvensi ModelState.

*Catatan: kandidat granular (1–8) di bawah adalah grouping mentah yang menurunkan struktur final di atas — dipertahankan untuk rujukan. **Urutan eksekusi:** P1→P2→P3 (3 High dulu) → P4→P5 (1 High) → P6 (cleanup terakhir). Migration kemungkinan di P3 (toggle SamePackage) & P6 (drop AssessmentPhase) — TBD saat plan-phase.*

---

### Fase Kandidat 1 — "Edit Form Fidelity & Persistensi Field" (≈6 temuan)
**Rasional:** Pola sistemik "field dirender tapi tidak di-persist" + reset diam-diam adalah risiko integritas tertinggi (memuat 1 High). Satu fase koheren memperbaiki binding form Create/Edit.
Mencakup: **E-01 (HIGH)** shuffle reset OFF; **FLD-5.2-08/RTK-LOGIC-04** retake bind-but-drop Create; **E-03** retake no-op Edit; **E-05** ValidUntil cabang standard; **E-04** lock Completed Pre-Post; **E-08** redirect manual entry.

### Fase Kandidat 2 — "Retake Lifecycle Hardening" (≈4 temuan)
**Rasional:** Memuat 1 High destruktif + konsistensi cert/counting retake; domain RetakeService/RetakeRules terisolasi.
Mencakup: **RTK-LOGIC-02 (HIGH)** cooldown vs ExamWindowCloseDate dead-end; **RTK-LOGIC-01** reset tak hapus NomorSertifikat; **RTK-LOGIC-03** counting divergen; **PA-06** guard hapus peserta Abandoned/attempt-history.

### Fase Kandidat 3 — "SamePackage & Shuffle Integrity" (≈5 temuan)
**Rasional:** Memuat 1 High (sync absen di Import) + invariant SamePackage/PackageNumber/lock; domain ManagePackages/ShuffleEngine.
Mencakup: **SHUF-ISS-03 (HIGH)** auto-sync absen di Import; **FLOW-07 (MEDIUM, naik dari Low — keputusan bisnis b)** toggle SamePackage bisa diubah pasca-create + sync/unsync + guard pra-peserta; **SHUF-ISS-02** lock SamePackage view-only; **PA-02** newPost tak warisi SamePackage; **SHUF-ISS-08** PackageNumber tak renumber; **SHUF-ISS-01** kunci sibling type-agnostic.

### Fase Kandidat 4 — "Certificate Issuance Consistency" (≈6 temuan)
**Rasional:** Konsolidasi aturan penerbitan sertifikat ke satu helper domain (`ShouldIssueCertificate`) + kelengkapan data cert.
Mencakup: **GRD-06** ValidUntil kosong saat issue; **GRD-08** race GetNextSeqAsync; **GRD-01** gate AssessmentType laten; **GRD-10** revoke/re-issue burn nomor; **FLD-5.2-10** penegakan Pre-non-cert; **FLD-5.2-07** dua sumber penomoran (manual vs online); **VAL-04** anti double-cert.

### Fase Kandidat 5 — "Grading De-duplication & Refactor" (≈4 temuan)
**Rasional:** Hapus duplikasi logika scoring (4 salinan) → satu fungsi murni, kunci dengan test paritas.
Mencakup: **GRD-09** duplikasi scoring 3+ tempat; **GRD-02** drift dedupe MC; **GRD-03** strategi seleksi MC 4 path; **FLOW-02** clamp ElapsedSeconds tanpa ExtraTime.

### Fase Kandidat 6 — "Flow & Linking Correctness + Gating Pre→Post" (≈4 temuan)
**Rasional:** Integritas alur Pre/Post linking & pairing analytics. **Memuat 1 High (FLOW-04) hasil keputusan bisnis (a).**
Mencakup: **FLOW-04 (HIGH, naik dari Low — keputusan bisnis a)** tegakkan Pre WAJIB Completed sebelum Post bisa `StartExam` (gate pelaksanaan, bukan cuma jadwal); **FLOW-01** pairing tiga jalur; **FLOW-03** Post tanpa Pre + link semu; **PA-07** asimetri manajemen peserta.

### Fase Kandidat 7 — "Cosmetic, Naming & Tech-Debt Cleanup" (≈9 temuan, low-risk)
**Rasional:** Kumpulan Low non-fungsional (naming/redundancy/dead-field/dokumentasi) — aman dikerjakan batch, tanpa migration berisiko.
Mencakup: **FLD-5.2-01** AssessmentTypeInput naming; **FLD-5.2-02** manual cert hardcode; **FLD-5.2-04** Schedule mirror + label; **FLD-5.2-05** IsPassed cross-validation; **FLD-5.2-06** label ValidUntil; **FLD-5.2-09** CertificateType overlap; **E-07** dua Edit form; **FLOW-05** komentar Status; **FLOW-06** AssessmentPhase dead-field; **FLOW-07** SamePackage final + write-on-GET; **PA-04** doc FK; **PA-05** sentinel naming; **PA-08** dedup D-31; **SHUF-ISS-04/05/07** shuffle warning/duplikasi; **GRD-05** PendingGrading visibility; **VAL-03** essay server-side; **VAL-06** retake warning; **VAL-07** ModelState konvensi; **FLOW-06** timer redundansi; **FLOW-04** token TempData.

### Fase Kandidat 8 — "Redesign Tata-Letak Form Create Pre-Post (UX)" (≈7 temuan, user-driven)
**Rasional:** Permintaan eksplisit pemilik (2026-06-22): form `CreateAssessment` mode Pre-Post banyak field duplikat / letak salah / membingungkan. Cluster UX terisolasi di `CreateAssessment.cshtml` (+ sedikit binding controller); aman dikerjakan setelah/berbarengan Fase 1 (bind-but-drop) karena beririsan field.
Mencakup: **FORM-PP-01** pindah `SamePackage` ke header section; **FORM-PP-02** label scope per-setelan (Pre/Post/keduanya) atau sub-kartu "Setelan Post"; **FORM-PP-03** bersihkan input standard tersembunyi dari POST; **FORM-PP-04** perbaiki + perjelas Retake (overlap Fase 1); **FORM-PP-05** token per-sesi vs shared; **FORM-PP-06** rapikan baris Status/PassPct asimetris; **FORM-PP-07** rename `AssessmentTypeInput` (= FLD-5.2-01).
*Dependensi: beririsan dengan Fase 1 (E-01/FLD-5.2-08/E-03/E-05) — pertimbangkan gabung atau urut Fase 1 → Fase 8.*

### Catatan Scope
- **VAL-01:** Audit Inject Assessment harus dijalankan terpisah di branch `main` (v32.2), bukan ITHandoff. Tandai out-of-scope untuk milestone ITHandoff.
- **Asumsi by-design** (FLOW-04 gating pelaksanaan, FLOW-07 SamePackage final, FLOW-06/07 write-on-GET/token TempData): wajib konfirmasi pemilik bisnis sebelum diperlakukan sebagai bug.
