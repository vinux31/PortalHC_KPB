# Design Spec — Inject Hasil Assessment Manual ("Seakan Online")

- **Tanggal:** 2026-06-17
- **Status:** Draft untuk review user → feed ke `/gsd-new-milestone`
- **Target milestone:** v33.0 (tentatif), ~6 fase
- **Tipe:** Fitur baru (page baru di Kelola Data) + retire tool lama (BulkBackfill)

---

## 1. Tujuan

HC/Admin dapat **meng-inject hasil assessment secara manual** (untuk ujian offline/kertas, data migrasi, atau acara lisensor luring) sehingga hasilnya **identik dengan assessment online**: muncul di riwayat pekerja, punya rincian jawaban per-soal, breakdown elemen teknis, dan (opsional) sertifikat ber-nomor resmi. Bagi pekerja, hasil inject **tak bisa dibedakan** dari assessment yang dikerjakan online.

Input via **form manual (per pekerja)** maupun **import Excel (batch)**. Soal di-tulis langsung di page inject (alur self-contained), dengan dua sumber jawaban: **input jawaban asli** atau **auto-generate dari skor target**.

---

## 2. Konteks & Temuan Kode (hasil research)

### 2.1 Yang sudah ada — `BulkBackfill` ("Bulk Import Nilai (Excel)")
- `Controllers/TrainingAdminController.cs:787` (GET), `:836` (POST `BulkBackfillAssessment`); view `Views/Admin/BulkBackfill.cshtml`; menu Kelola Data Section D, **Admin-only**.
- Lahir Phase 338 (v20.0) untuk restore data PreTest OJT GAST Cilacap yang hilang.
- Hanya menulis **skor agregat** ke `AssessmentSessions` (`Status="Completed"`, `Score`, `IsPassed`, `IsManualEntry=true`, `AssessmentType=Manual`, `AccessToken="BACKFILL"`) + AuditLog per row. Atomic.
- **Tidak** ada jawaban per-soal, **tidak** ada `UserPackageAssignment`/`PackageUserResponses`, **tidak** ada sertifikat.
- **Keputusan:** karena Admin-only & terbatas (emergency restore), fitur ini akan **diabsorb/dipensiunkan** ke page inject baru jika sudah tak terpakai.

### 2.2 Visibility di riwayat pekerja — sudah otomatis
- `Services/WorkerDataService.cs:28` `GetUnifiedRecords()` **tidak** memfilter `IsManualEntry`. Sesi dengan `Status="Completed"` atau `"Menunggu Penilaian"` langsung muncul di `/CMP/Records` & `RecordsWorkerDetail`, dilabel **"Assessment Online"**.
- Konsekuensi: requirement "tampil di /CMP/Records seperti online" **terpenuhi otomatis** asal status & data benar. Tidak perlu kode visibility baru.

### 2.3 Detail per-soal di `/CMP/Results` — butuh 3 data
- `Controllers/CMPController.cs:2184` action `Results`. Render rincian jawaban **hanya jika** sesi punya:
  1. `UserPackageAssignment` (JSON `ShuffledQuestionIds`, `ShuffledOptionIdsPerQuestion`),
  2. `PackageUserResponses` (jawaban: `PackageOptionId` untuk MC/MA, `TextAnswer`+`EssayScore` untuk Essay),
  3. paket soal `AssessmentPackage` → `PackageQuestion` → `PackageOption`,
  4. `AllowAnswerReview = true`.
- Tanpa itu → empty state badge **"Tinjauan jawaban tidak tersedia"** (`Views/CMP/Results.cshtml:413`).
- **Inilah inti pekerjaan baru** — yang BulkBackfill tidak punya.

### 2.4 "Assessment room" = grouping (bukan tabel)
- Listing room: `AssessmentAdminController.ManageAssessmentTab_Assessment:112`.
- **Standard room** = `GroupBy (Title, Category, Schedule.Date)`, `AccessToken` shared.
- **Pre/Post room** = `GroupBy LinkedGroupId`; pasangan Pre↔Post via `LinkedSessionId`; `AssessmentType` ∈ {`"PreTest"`,`"PostTest"`,`"Standard"`}.
- Identifikasi room: `RepresentativeId` (sesi pertama) + `AllIds`.

### 2.5 Mesin yang akan di-reuse
- **Authoring soal:** `ManagePackages` + CRUD soal/opsi (`AssessmentAdminController.cs:~5641+`), partial view authoring, model `AssessmentPackage`/`PackageQuestion`/`PackageOption` (`Models/AssessmentPackage.cs`).
- **Grading:** `Services/GradingService.GradeAndCompleteAsync` (MC/MA all-or-nothing, persen, elemen teknis, sertifikat) + jalur essay `FinalizeEssayGrading` + `AssessmentScoreAggregator`.
- **Sertifikat:** `Helpers/CertNumberHelper` (`GetNextSeqAsync` + `Build` → `KPB/{seq:D3}/{ROMAN}/{year}`).

---

## 3. Keputusan Desain (final)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| D-1 | Page baru vs tool lama | **Page baru**; BulkBackfill diabsorb/pensiun |
| D-2 | Fidelity | **Full** — skor + jawaban per-soal + elemen teknis + sertifikat |
| D-3 | Sumber soal | **Standalone secara alur, tapi REUSE kode di belakang** (authoring partial + GradingService + CertNumberHelper). Nol duplikasi logic. |
| D-4 | Sumber jawaban | **Campuran**: input jawaban asli **atau** auto-generate dari skor target, per-kasus |
| D-5 | Metode input | **Form manual (per pekerja)** + **Import Excel (batch)** |
| D-6 | Visibility | Muncul di `/CMP/Records` & `/CMP/Results` persis online (gratis via §2.2/§2.3) |
| D-7 | Sertifikat | **Toggle per-room**: auto-generate (nomor resmi) / input manual / tanpa sertifikat |
| D-8 | Pre/Post | Bisa **link silang ke room existing** (mis. Pre di-inject, Post real online) via search picker → wiring `LinkedGroupId`+`LinkedSessionId` |
| D-9 | RBAC | **Admin + HC** (BulkBackfill dulu Admin-only — itu sebabnya HC tak pernah lihat) |
| D-10 | Transparansi | `IsManualEntry=true` + AuditLog `"ManualInject"` (actor+NIP+sesi). Terlihat online ke pekerja, terlacak penuh ke admin. |

---

## 4. Arsitektur

**Prinsip:** page inject = **orchestrator tipis** yang menyusun input (paket soal + jawaban per pekerja) lalu **melewatkannya ke pipeline grading yang sama** dengan online. Skor/lulus/sertifikat/elemen-teknis **dihitung mesin existing**, bukan ditulis tangan → dijamin identik online.

Komponen baru:
- `Controllers/InjectAssessmentController.cs` (route `/Admin/InjectAssessment`, `[Authorize(Roles="Admin,HC")]`).
- `Services/InjectAssessmentService.cs` — inti: terima `(packageSpec, settings, List<workerAnswers>)` → buat session set lewat reuse `GradingService`/`FinalizeEssayGrading`.
- View `Views/Admin/InjectAssessment.cshtml` (alur berlangkah) + partial authoring soal yang di-reuse.
- ViewModel `InjectAssessmentViewModel` + Excel template/parser (ClosedXML, sama lib BulkBackfill).
- Card baru di `Views/Admin/Index.cshtml` Section C (Assessment & Training).

Tidak diubah (reuse apa adanya): `GradingService`, `AssessmentScoreAggregator`, `CertNumberHelper`, `WorkerDataService.GetUnifiedRecords`, `CMPController.Results`, model paket soal.

---

## 5. Alur End-to-End (HC)

```
PAGE: /Admin/InjectAssessment  (Kelola Data → Section C, Admin+HC)

Langkah 1 — Setup Room  (form = mirror CreateAssessment)
  Title, Category, Schedule/CompletedAt (backdate ke tgl ujian luring),
  DurationMinutes, PassPercentage, AllowAnswerReview (default true),
  AssessmentType: Standard | Pre | Post,
  Sertifikat: [auto-generate | input manual | tanpa]  (D-7),
    └─ jika manual: field NomorSertifikat + ValidUntil
  └─ jika Pre/Post: [Cari Room Existing] → pilih room online asli
        → wiring LinkedGroupId + LinkedSessionId  (inject-Pre ↔ real-Post)

Langkah 2 — Tulis Soal  (reuse partial authoring)
  soal + opsi + IsCorrect + QuestionType (MC/MA/Essay)
  + ScoreValue + ElemenTeknis + Rubrik(essay)
  → AssessmentPackage + PackageQuestion + PackageOption

Langkah 3 — Pilih Pekerja  (reuse worker picker)

Langkah 4 — Jawaban per pekerja  (2 mode, boleh dicampur per pekerja):
  A. Input asli  → form render soal; HC pilih jawaban tiap pekerja
                   (MC/MA: opsi; Essay: teks + skor manual)
                   ATAU Excel matrix (baris=NIP, kolom=soal, sel=jawaban)
  B. Auto-generate → HC kasih skor target → sistem bikin pola benar/salah
                     konsisten dgn skor (MC/MA). Essay: set EssayScore langsung.

Langkah 5 — Konfirmasi → JALANKAN pipeline grading existing (per pekerja):
  buat AssessmentSession (IsManualEntry=true, AccessToken="INJECT",
        IsTokenRequired=false, Schedule/StartedAt/CompletedAt = tgl luring)
  + UserPackageAssignment (ShuffledQuestionIds JSON)
  + PackageUserResponses (jawaban nyata / hasil auto-gen)
  → panggil GradingService.GradeAndCompleteAsync
       (essay → "Menunggu Penilaian" lalu set EssayScore + FinalizeEssayGrading)
  → Score / IsPassed / NomorSertifikat / SessionElemenTeknisScore (dihitung)
  + AuditLog "ManualInject" per sesi
  Atomic per batch (rollback all jika ada NIP invalid / error).
```

**Hasil akhir:** `/CMP/Records` pekerja menampilkan baris "Assessment Online"; `/CMP/Results` menampilkan rincian per-soal benar/salah + elemen teknis + tombol sertifikat — identik online.

---

## 6. Data yang Ditulis (per pekerja per sesi inject)

| Entity | Field kunci | Sumber |
|--------|-------------|--------|
| `AssessmentSession` | `IsManualEntry=true`, `Status` (via grading), `Score`/`IsPassed` (dihitung), `Schedule/StartedAt/CompletedAt`, `AssessmentType`, `LinkedGroupId`/`LinkedSessionId`, `GenerateCertificate`, `NomorSertifikat`, `AllowAnswerReview` | form + grading |
| `AssessmentPackage` (+`PackageQuestion`+`PackageOption`) | soal/opsi/kunci/ScoreValue/ElemenTeknis/Rubrik | Langkah 2 (1× per room) |
| `UserPackageAssignment` | `AssessmentPackageId`=sentinel (paket pertama), `ShuffledQuestionIds` (JSON), `ShuffledOptionIdsPerQuestion`="{}" (urut DB), `SavedQuestionCount`, `IsCompleted=true` | Langkah 5 |
| `PackageUserResponse` | `PackageOptionId` (MC/MA) / `TextAnswer`+`EssayScore` (Essay) | Langkah 4 |
| `SessionElemenTeknisScore` | `CorrectCount`/`QuestionCount` per elemen | dihitung grading |
| `AuditLog` | `ActionType="ManualInject"`, actor, NIP, sessionId | Langkah 5 |

---

## 7. Mode Jawaban — detail

### 7.1 Input asli
- **Form per pekerja:** render soal authored; HC pilih opsi (MC: 1, MA: ≥1) atau ketik teks essay + skor.
- **Excel matrix:** baris = NIP, kolom = nomor soal. Sel MC/MA = huruf opsi (mis. `A` atau `A,C`); kolom essay = skor (0..ScoreValue) atau skor+teks. Template di-generate dari paket soal yang sudah di-authored. Validasi: NIP wajib ada di `AspNetUsers`, huruf opsi valid, atomic rollback.

### 7.2 Auto-generate dari skor target
- HC kasih skor target (mis. 80) per pekerja (atau seragam 1 batch).
- Sistem memilih subset soal jadi "benar" sehingga `Σ(ScoreValue benar)/Σ(ScoreValue) ≈ target` (catat pembulatan; jika target tak persis tercapai, ambil terdekat & laporkan).
- MC → pilih opsi benar untuk soal "benar", opsi salah untuk lainnya. MA → set persis kunci untuk benar, set tak-cocok untuk salah.
- Essay → `EssayScore` di-set untuk menyumbang ke target (essay tak bisa "auto-benar/salah" karena dinilai manual).
- **Catatan integritas:** jawaban auto-gen bersifat **sintetis** (bukan jawaban asli pekerja). Tetap ditandai `IsManualEntry` + audit. Cocok untuk kasus "hanya punya skor akhir".

---

## 8. Sertifikat (D-7, toggle per-room)

1. **Auto-generate** → `CertNumberHelper.GetNextSeqAsync` + `Build` (retry 3× anti-collision), nomor masuk sekuens resmi `KPB/xxx/ROMAN/year`. Sama persis online.
2. **Input manual** → HC ketik `NomorSertifikat` sendiri (mis. nomor sertifikat luring yang sudah ada). Tidak ikut sekuens auto.
3. **Tanpa** → `GenerateCertificate=false`, tidak ada nomor.

---

## 9. Link Pre/Post ke Room Existing (D-8)

- Saat `AssessmentType=Pre` atau `Post`, tampilkan **search picker room existing** (reuse query `ManageAssessmentTab_Assessment`: cari by Title/Category/jadwal, tampil sebagai daftar room dengan `RepresentativeId`+`LinkedGroupId`).
- Pilih room → set `LinkedGroupId` sesi inject = `LinkedGroupId` room target (atau buat group baru jika room target standalone), dan `LinkedSessionId` ke sesi pasangan yang sesuai per pekerja.
- Mendukung skenario silang: **Pre di-inject, Post = assessment online asli** (atau sebaliknya).
- Reuse logika PrePost existing; jangan bikin grouping baru.

---

## 10. Transparansi, Audit & Keamanan

- Setiap sesi inject `IsManualEntry=true` + AuditLog `"ManualInject"` (actor, NIP, sessionId, skor, tgl). Wajib — data sertifikasi harus terlacak.
- Ke pekerja terlihat sebagai "Assessment Online" (sesuai tujuan), tapi panel Admin & audit dapat membedakan.
- RBAC `Admin,HC`. Validasi server-side: NIP valid, skor 0..100, opsi valid, tanggal masuk akal.
- Atomic per batch (transaction) — rollback semua bila ada error (pola BulkBackfill).
- Anti-double-cert: hormati guard duplikat existing (cek sesi/cert duplikat sebelum insert).

---

## 11. Pemecahan Fase (milestone ~v33.0)

| Fase | Judul | Deliverable | Disjoint? |
|------|-------|-------------|-----------|
| 1 | **Backend core inject** | `InjectAssessmentService` + reuse GradingService/Finalize + buat session set full (assignment+responses+ET+cert) + AuditLog + atomic. xUnit. | fondasi |
| 2 | **Page + Setup Room + authoring soal** | Controller+view berlangkah, form setting (mirror CreateAssessment), reuse partial authoring soal, worker picker, card menu Section C | depends F1 |
| 3 | **Mode jawaban (form)** | Input asli per pekerja + auto-generate dari skor target (MC/MA/Essay) | depends F2 |
| 4 | **Import Excel + absorb BulkBackfill** | Template generator + matrix parser + validasi atomic; pensiun/redirect BulkBackfill | depends F3 |
| 5 | **Link Pre/Post ke room existing** | Search picker room + wiring LinkedGroupId/LinkedSessionId (silang inject↔real) | depends F2 |
| 6 | **Test + UAT** | E2E "seakan online" (inject → cek /CMP/Records + /CMP/Results per-soal + sertifikat), regression | depends F1-5 |

Catatan: F5 file-disjoint relatif terhadap F3/F4 (boleh paralel setelah F2). Penomoran fase aktual diserahkan ke `/gsd-new-milestone` (lanjut dari 392).

---

## 12. Out of Scope v1 (YAGNI)

- Edit massal sesi inject pasca-buat (sudah ada via tab "Input Records" untuk hapus/edit dasar).
- Import gambar soal via Excel (gambar via authoring UI saja).
- Multi-paket variasi per room inject (cukup 1 paket per room dulu).
- Notifikasi/broadcast SignalR (inject = data historis, tak perlu real-time).

---

## 13. Risiko & Pertimbangan

- **Integritas data sertifikasi:** inject = data "seakan online". Mitigasi: audit wajib + flag `IsManualEntry` + RBAC. Perlu kebijakan organisasi siapa boleh inject.
- **Auto-generate skor tak persis:** pembulatan bisa bikin skor target tak tercapai persis; tampilkan skor final aktual sebelum commit.
- **Reuse GradingService:** pastikan jalur essay (`FinalizeEssayGrading`) dipanggil benar agar `Status` jadi `Completed` (bukan tertinggal "Menunggu Penilaian").
- **Anchor paket soal (VERIFIED):** Pola online (`CMPController.cs:1034-1090`) sudah jelas — `AssessmentPackage.AssessmentSessionId` meng-anchor paket ke salah satu sesi sibling room; tiap pekerja dapat `UserPackageAssignment` ber-`AssessmentPackageId=sentinelPackage.Id` (paket pertama) + `ShuffledQuestionIds`. Grading (`GradingService`) & Results (`CMPController.Results`) memuat soal **by question ID via `ShuffledQuestionIds`**, bukan by session. → Inject: anchor paket ke sesi representatif room inject, assign semua pekerja via `UserPackageAssignment`. Bukan blocker.
- **Link silang Pre/Post:** verifikasi grouping `LinkedGroupId` tak rusak saat satu sisi inject & satu sisi real online.

---

## 14. Asumsi

- 1 room inject = 1 paket soal, semua pekerja paket sama.
- Pekerja (NIP) sudah ada di sistem (`AspNetUsers`); inject tak membuat pekerja baru.
- Tanggal ujian luring di-backdate manual oleh HC.
- `AllowAnswerReview` default `true` agar detail per-soal tampil (bisa di-toggle).
