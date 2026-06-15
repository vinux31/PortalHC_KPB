# Design Spec — Toggle Acak Soal & Acak Pilihan (Shuffle Toggle)

**Tanggal:** 2026-06-13
**Milestone target:** v27.0
**Status:** Approved (brainstorm selesai)

## 1. Ringkasan

Saat ini sistem ujian berbasis paket punya 2 perilaku pengacakan yang **hardcoded aktif** dan tidak bisa dimatikan HC:

1. **Acak Soal** — urutan/pemilihan soal per peserta.
2. **Acak Pilihan** — urutan opsi jawaban (A/B/C/D) per soal per peserta.

> Catatan temuan: komentar `CMPController.cs:1054` ("option shuffle removed per user decision") **STALE/menyesatkan**. Acak pilihan sebenarnya **AKTIF** — dicabut di commit `d777d6b9` lalu dihidupkan lagi di `e6ddffd6` (fix 91-01) via jalur `ViewBag.OptionShuffle`. Grading aman karena pakai `PackageOption.Id` (bukan posisi huruf — lihat `PackageOption.cs:84-85`).

Fitur ini menambahkan **2 toggle independen** (Acak Soal, Acak Pilihan) yang bisa di-ON/OFF HC, dengan scope **per-assessment**.

## 2. Keputusan Brainstorm (locked)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| Scope | Level efek toggle | **Per-assessment** (field di `AssessmentSession`, di-propagate ke semua sibling) |
| Lokasi UI | Tempat tombol | **Halaman ManagePackages** (hub paket per grup) |
| Lock timing | Kapan toggle dikunci | Editable selama **belum ada peserta mulai**; lock begitu ada peserta start/punya assignment |
| Default data lama | Migration | **Dua-duanya ON** (`defaultValue: true`) — perilaku existing tidak berubah |
| Default data baru | Form create | **Dua-duanya ON** (form default checked) |
| Independensi | Acak Soal vs Acak Pilihan | **Independen** — boleh beda |
| Pre/Post | Toggle di Pre & Post | **Independen** + reminder visual (opsi Z), no auto-cascade |
| SamePackage | Lokasi setting | **TIDAK dipindah** (tetap di wizard); toggle shuffle TIDAK ikut ke-lock |

## 3. Arsitektur Relevan (hasil investigasi codebase)

- `AssessmentSession` = **per-peserta** (punya `UserId`, `Score`, `StartedAt` per orang).
- Satu "assessment" logis = **grup sibling sessions** dengan key `(Title, Category, Schedule.Date)`.
- `AssessmentPackage` nempel ke **satu** session "representative" (yang pertama dibuat / `OrderBy(CreatedAt).First()`). Saat ujian, `StartExam` cari paket **lintas sibling** (`CMPController.cs:949-961`).
- Field assessment-level (PassPercentage, AllowAnswerReview, dll) disimpan di **SETIAP** baris sibling, di-propagate via `foreach siblings` di `EditAssessment` POST (`AssessmentAdminController.cs:2007-2031`), di-set di loop create.
- Assignment per-peserta (`UserPackageAssignment.ShuffledQuestionIds` + `ShuffledOptionIdsPerQuestion`) dibangun **lazy** saat peserta pertama buka ujian (`StartExam`, `CMPController.cs:975-1000`).
- **Non-package path SUDAH MATI** (`CMPController.cs:1161`, Phase 227 CLEN-02) — semua ujian wajib paket.

## 4. Data Model

Tambah 2 kolom di `AssessmentSession`:

```csharp
[Display(Name = "Acak Soal")]
public bool ShuffleQuestions { get; set; } = true;

[Display(Name = "Acak Pilihan Jawaban")]
public bool ShuffleOptions { get; set; } = true;
```

**Migration:**
- `AddShuffleTogglesToAssessmentSession`
- Kolom `bit NOT NULL DEFAULT 1` untuk kedua kolom → semua baris LAMA otomatis `true` (perilaku existing dipertahankan, janji "data tidak berubah").

**EF bool trap (WAJIB):** C# `bool` default = `false`. Migration `defaultValue:true` hanya benerin baris LAMA. Untuk assessment BARU lewat form, kode WAJIB set nilai eksplisit dari form (default checked) — kalau tidak, assessment baru malah OFF. Set di SEMUA loop create session (standard, Pre, Post).

## 5. Propagasi (ikut pola existing)

- **Create** (`CreateAssessment` POST): set `ShuffleQuestions`/`ShuffleOptions` dari form di tiap loop create session — standard loop (`~1424`), Pre loop (`~1216`), Post loop (`~1250`).
- **Ubah toggle**: endpoint baru (lihat §7) propagate ke SEMUA sibling via `foreach` (pola `EditAssessment` POST).
- Pre & Post = grup terpisah (Schedule beda) → toggle independen, tidak saling tarik.

## 6. Logika Baca Shuffle (inti — di `StartExam` saat bangun `UserPackageAssignment`)

Baca `assessment.ShuffleQuestions` / `assessment.ShuffleOptions` (dari session peserta sendiri; sudah ter-propagate).

### 6.1 Acak Soal

**ON** (perilaku sekarang, tidak berubah):
- 1 paket → semua soal paket, urutan **diacak** (Fisher-Yates).
- ≥2 paket → sampling `K = min(jumlah soal antar paket)` soal lintas paket, seimbang per ElemenTeknis, lalu diacak (`BuildCrossPackageAssignment`).

**OFF** (baru):
- 1 paket → **semua soal paket, urutan tetap** (`q.Order`), tanpa Fisher-Yates.
- ≥2 paket → **distribusi 1 paket utuh per worker, urutan tetap**:
  - Round-robin **by index session stabil**: urutkan sibling session `OrderBy(Id)`, posisi worker = index → `paket[ posisi % jumlahPaketBerSoal ]`.
  - **Deterministik** (worker X selalu paket sama) + **seimbang** + tahan reshuffle & resume.
  - **JANGAN** pakai "urutan buka" / `assignmentCount % n` — cacat: geser saat reshuffle/resume → bisa false-error "soal berubah".
  - **Guard paket kosong**: round-robin hanya antar paket dengan `Questions.Count > 0`.
  - Isi paket urutan `q.Order` (tak teracak).

### 6.2 Acak Pilihan (independen)

- **ON** → bangun `optionShuffleDict` (acak opsi per soal), simpan ke `ShuffledOptionIdsPerQuestion`.
- **OFF** → simpan `ShuffledOptionIdsPerQuestion = "{}"` → view fallback urutan DB (`StartExam.cshtml:126-162`).
- Berlaku di SEMUA mode acak soal (boleh ON walau Acak Soal OFF).

### 6.3 Resume stale-count guard

`StartExam:1027` bandingkan `SavedQuestionCount` vs hitung-sekarang. Karena §6.1 OFF pakai index session stabil, rekomputasi deterministik → guard tidak salah-trigger. Untuk OFF multi-paket, `currentQuestionCount` = jumlah soal paket worker itu.

## 7. UI — Halaman ManagePackages

- Tampilkan 2 toggle (Acak Soal, Acak Pilihan) di header `ManagePackages` (per session/grup).
- **Aktif walau editing paket di-lock** (`SamePackage` lock hanya isi paket, bukan shuffle).
- **Lock toggle** (read-only) jika ada peserta sudah mulai: cek ada sibling `StartedAt != null` ATAU ada `UserPackageAssignment` untuk paket grup. Tampilkan alasan.
- Endpoint POST baru `UpdateShuffleSettings(int assessmentId, bool shuffleQuestions, bool shuffleOptions)`:
  - Guard lock (tolak jika sudah ada peserta mulai).
  - Propagate ke semua sibling (`foreach`).
  - Anti-forgery + `[Authorize(Roles="Admin,HC")]`.
- **Warning non-blocking** (§9) bila multi-paket + Acak Soal OFF + ukuran paket beda.
- **Sembunyikan toggle** untuk Proton Tahun 3 / Manual entry (bukan ujian online; mereka tak lewat flow ini, hide defensif).

### 7.1 Pre/Post reminder (opsi Z)

- Toggle independen di Pre & Post (masing-masing halaman ManagePackages-nya).
- Kalau Pre OFF tapi Post masih ON (cek via `LinkedSessionId`), tampilkan **reminder visual** di halaman Post: *"Pre diatur OFF, Post masih ON — sengaja?"*. Tidak ada auto-cascade, tidak ada state tersembunyi.

## 8. Reshuffle Endpoints (hormati flag — M2)

`ReshufflePackage` (`AssessmentAdminController.cs:5040`) & `ReshuffleAll` (`5121`):
- Saat ini **selalu** panggil `BuildCrossPackageAssignment` (selalu acak) + hard-code `ShuffledOptionIdsPerQuestion = "{}"` (line 5094) → **bug existing**: peserta yang di-reshuffle dapat opsi TAK teracak walau jalur normal mengacak.
- **Fix**: reshuffle bangun-ulang assignment **sesuai `ShuffleQuestions` DAN `ShuffleOptions`**:
  - `ShuffleQuestions OFF` → rebuild deterministik (index-stabil, §6.1).
  - `ShuffleOptions ON` → bangun `optionShuffleDict`; OFF → `"{}"`.
- Guard "hanya Not started/Abandoned" yang sudah ada (line 5058) dipertahankan — sejalan konsep lock.

## 9. UI Warning (non-blocking)

Multi-paket + Acak Soal OFF + jumlah soal antar paket berbeda → warning di ManagePackages:
> "Jumlah soal antar paket berbeda — nilai maksimal & pass% bisa tak setara antar peserta."

Tidak diblok (HC mungkin sengaja, mis. exam gaya baris A/B). Konsisten pola project (warning, bukan hard-block).

## 10. Cleanup

Perbaiki komentar stale `CMPController.cs:1054` ("option shuffle removed per user decision") — sekarang opsi digerbang `ShuffleOptions`.

## 11. Testing

- **Ekstrak core pure** (pola extract-static-core project): fungsi bangun urutan soal + distribusi paket diberi flag (`ShuffleQuestions`, daftar paket, index worker) → unit-testable tanpa DB.
  - Test: ON 1 paket (acak), ON ≥2 (sampling K), OFF 1 paket (urut), OFF ≥2 (round-robin index-stabil deterministik), guard paket kosong.
- **Option shuffle core**: ON bangun dict, OFF "{}".
- **Migration default**: baris lama → ON dua-duanya.
- **Propagasi**: ubah toggle → semua sibling ikut.
- **Lock**: tolak ubah saat ada peserta mulai.
- **Reshuffle**: hormati kedua flag (incl. fix bug opsi).
- **Playwright UAT**: toggle ON/OFF + lock state + reminder Pre/Post + warning ukuran paket.

## 12. Out of Scope

- Memindah setting `SamePackage` ke halaman package (phase terpisah bila diperlukan).
- Auto-cascade Pre→Post (pakai reminder Z).
- Non-package legacy path (sudah mati).

## 13. Grading Safety

Grading pakai `PackageOption.Id` (bukan posisi huruf). Acak posisi opsi **tidak pernah** mempengaruhi nilai. `GetShuffledQuestionIds()` tetap dipakai grading — assignment menyimpan daftar ID baik mode ON maupun OFF, jadi jalur grading tidak berubah.
