# Proton Completion Logic — Design Spec (Diskusi A)

> **STATUS:** Design final 2026-06-09, siap review. Ini **fondasi** logic kelulusan Proton.
> Fitur **Bypass Tahun** (Diskusi B) DEPENDS ON spec ini —
> lihat `2026-06-09-proton-bypass-tahun-DRAFT-PAUSED.md`.

## 1. Masalah

Logic kelulusan Proton tidak konsisten antara maksud bisnis dan kode:

- **`ProtonFinalAssessment`** (penanda "lulus tahun", dipakai dashboard CDP:3204 = `allApproved && penanda ada`) **cuma dibuat 1 tempat**: `AssessmentAdminController.cs:3753` (`SubmitInterviewResults`) = **interview Tahun 3 saja**.
- **Tahun 1/2 = exam online** (DurationMinutes>0, di-grade `GradingService`). Lulus → dapat `NomorSertifikat` di `AssessmentSession`, TAPI **tidak pernah** bikin `ProtonFinalAssessment` → dashboard **tidak pernah** menandai "Lulus" untuk Tahun 1/2. **(BUG)**
- **`CompetencyLevelGranted` (0-5)** tidak pernah diisi selain hardcoded `0` (interview). Fitur level setengah jadi — matriks KKJ sudah di-drop (`KkjMatrixItem` mati, KKJ sekarang file upload).
- **Urutan** (deliverable 100% → baru final assessment) tidak dipaksa eksplisit di POST.
- **2 sinyal** kelulusan (`AssessmentSession.IsPassed`+cert vs `ProtonFinalAssessment`) tidak sinkron.

## 2. Alur kanonik yang dituju (maksud bisnis)

```
Per tahun (Tahun 1/2/3):
  1. Coaching → semua deliverable tahun itu di-APPROVE (100%)
  2. Baru boleh ikut FINAL ASSESSMENT:
       - Tahun 1 & 2 = exam online (worker kerjakan soal, auto-grade)
       - Tahun 3     = interview offline (HC input nilai)
  3. LULUS final assessment → "Lulus Proton Tahun X" + sertifikat (NomorSertifikat)

Keseluruhan:
  Lulus Tahun 1 → 2 → 3 → GRADUATED (tamat PROTON)
```

## 3. Keputusan terkunci

| # | Keputusan |
|---|-----------|
| **A-1** | Lulus = **berurutan/gated** (deliverable 100% DULU → baru final assessment). Gate sudah ada utk Tahun 1/2 via `GetEligibleCoachees` |
| **A-2** | "100%" = **1 approver cukup** (Sr SPV *atau* SH; yang pertama approve → Status="Approved", co-sign opsional). HC = final review, **bukan** approver deliverable. *(= perilaku kode sekarang, tidak diubah)* |
| **A-3** | **Matikan `CompetencyLevelGranted`** — `ProtonFinalAssessment` jadi penanda "Lulus/Selesai" murni. Hapus tampilan level + grafik tren. Kolom DB **dibiarkan dormant** (default 0, tidak di-drop, tanpa migration) |
| **A-4** | **Pertahankan penanda** — exam Tahun 1/2 lulus → auto-terbit `ProtonFinalAssessment` via **helper bersama** (3 jalur: exam, interview, bypass). Dashboard/HistoriProton **tidak berubah** (tetap baca penanda) |
| **A-5.1** | **Gate antar-tahun keras** — blok assign/eligible Tahun N kalau Tahun N-1 (TrackType sama) belum lulus. Aturan tetap di kode (no config page). Tahun 1 = entry, tanpa prasyarat |
| **A-5.2** | **Tahun 3 punya deliverable** — gate data-driven (silabus Tahun 3 diisi → gate jalan otomatis). Tahun 3 final tetap **interview** |

## 4. Perubahan kode

### 4.1 Helper bersama (A-4) — `EnsureProtonFinalAssessment`
Buat 1 helper idempotent: `(coacheeId, protonTrackId, createdById, origin, notes) → create ProtonFinalAssessment jika belum ada` (hormati duplicate-check `ProtonTrackAssignmentId`). Resolve assignment aktif via `(CoacheeId, ProtonTrackId, IsActive)`. `CompetencyLevelGranted` di-set 0 (dormant, A-3). Set `Origin` (A-M9, lihat 4.8).

Dipanggil dari:
- `GradingService.GradeAsync` (worker submit) — saat `isPassed` & session Proton (ada `ProtonTrackId`). `Origin="Exam"`.
- `GradingService.RegradeAfterEditAsync` (re-grade, L420-516):
  - `!wasPassed && isPassed` (jadi lulus, L471) → **create** penanda `Origin="Exam"`.
  - `wasPassed && !isPassed` (jadi gagal, L458) → **HAPUS** penanda **HANYA jika `Origin=="Exam"`** (penanda Bypass/Interview kebal — A-M9). *(A-M1)*
- `SubmitInterviewResults` (Tahun 3) — refactor ke helper yang sama (logic L3740-3766 sekarang). `Origin="Interview"`.
- Bypass (Diskusi B) → `Origin="Bypass"`.

### 4.2 Gate eligibility server-side (A-M2)
Di **POST `CreateAssessment`** (AssessmentAdminController:842), untuk `Category=="Assessment Proton"`: re-validate **tiap `UserId`** server-side SEBELUM bikin session:
- worker punya assignment aktif di track tsb,
- semua deliverable (per-unit) Status="Approved" *(reuse `CoacheeEligibilityCalculator.IsEligiblePerUnit`)*,
- **gate antar-tahun**: Tahun N-1 (TrackType sama) sudah ada penanda lulus (A-5.1).
Tolak (ModelState error) UserId yang tidak lolos. Gate keras **harus di server**, bukan cuma JS.

> **Exempt (A-M4):** assignment hasil **bypass** (Diskusi B) tidak kena gate antar-tahun — admin/HC sengaja lompat.

### 4.3 Gate antar-tahun (A-5.1)
Aturan: "Tahun N-1 lulus" = ada `ProtonFinalAssessment` utk assignment Tahun N-1, TrackType sama, worker sama. Tahun 1 = tanpa prasyarat. Dipasang di: (a) titik assign normal (CoachMapping), (b) POST CreateAssessment (4.2). No tabel/page baru.

### 4.4 Matikan level (A-3)
Hapus tampilan `CompetencyLevelGranted`: CDP `:376`, `:542`, grafik tren `:592`, `:3517`. Kolom DB tetap (dormant). `ProtonFinalAssessment` ditampilkan sebagai status "Lulus/Selesai" tanpa angka.

### 4.5 Tahun 3 data-driven (A-5.2 / A-M7)
- Netralin shortcut "Tahun 3 no deliverable → all eligible" (`GetEligibleCoachees` CoachMapping:1363) jadi **murni data-driven** (kalau track punya deliverable → wajib 100%, apapun tahunnya).
- Pastikan bootstrap (`AutoCreateProgressForAssignment`) bikin progress deliverable Tahun 3 saat di-assign.
- Deteksi tipe final (Duration=0 → interview) tetap jalan utk Tahun 3.
- **Data**: silabus Tahun 3 wajib diisi deliverable (tugas admin/data; di luar kode).

### 4.6 Graduation gate ringan (A-M8)
Tombol "Mark graduated" (`CoachMapping:1138`, set `IsCompleted`) hanya boleh jika **Tahun 3 sudah lulus** (penanda Tahun 3 ada utk worker tsb). Tolak + pesan kalau belum.

### 4.7 Backfill data lama (A-M3 / A-M10)
Script/migration data 1x: untuk tiap `AssessmentSession` `Category=="Assessment Proton"` Tahun 1/2 yang `IsPassed==true` + deliverable track 100% approved + belum ada penanda → bikin `ProtonFinalAssessment` (`Origin="Exam"`). (Idempotent, pakai helper 4.1.) Snapshot DB sebelum jalan (SEED/DEV workflow).
- **A-M10**: nyantol ke assignment yang match `(coachee, exam.ProtonTrackId)` — bisa **inactive** (worker udah pindah) & bisa **>1** (akibat create-baru D-E). Pilih assignment yang paling sesuai era exam (mis. by AssignedAt terdekat sebelum exam.CompletedAt). Logback yang di-skip.

### 4.8 Marker `Origin` (A-M9) — schema migration
Tambah kolom **nullable `Origin`** di `ProtonFinalAssessment`: values `"Exam"` / `"Interview"` / `"Bypass"`.
- Migration set existing rows → `"Interview"` (semua penanda lama = interview Tahun 3).
- Backfill (4.7) set `"Exam"`.
- Guna: re-grade Pass→Fail cuma hapus penanda `Origin=="Exam"` (4.1); dipakai ulang Bypass (undo/audit, Diskusi B).
- ⚠️ **MIGRATION** — satu-satunya schema change Diskusi A. Notify IT dgn flag migration (CLAUDE.md DEV_WORKFLOW). Kolom `CompetencyLevelGranted` tetap dormant (tidak di-drop, A-M5).

### 4.9 Guard helper (A-M11)
Helper cuma jalan kalau `Category=="Assessment Proton"` **&&** `IsPassed` **&&** `ProtonTrackId.HasValue`. Jangan kepicu di exam non-Proton / Pre-Test (Proton = Standard-only).

### 4.10 Renewal interaction (A-M13)
Gate antar-tahun cek Tahun N-1 → **renewal Tahun N** (`RenewsSessionId`) tidak keblok. Pastikan jalur renewal exam Proton tetap lewat gate eligibility server-side yang sama (4.2) — tidak ada pintu samping yang skip gate.

## 5. HC review channel & yang TIDAK berubah (A-M8)
- `HCApprovalStatus` (HC review per-deliverable) = **informational** — completion tidak pakai. Dibiarkan apa adanya.
- Logic approve deliverable (Sr SPV/SH, 1 approver) **tidak diubah** (A-2).
- Notifikasi "AllDeliverablesComplete" ke HC (CDP:945) tetap (jadi pemicu HC bikin final assessment).
- Sertifikat (`NomorSertifikat`) tetap di `AssessmentSession`, terbit saat lulus (tidak pindah ke penanda).

## 6. Edge cases

| # | Edge | Resolusi |
|---|------|----------|
| A-M1 | Re-grade exam jadi gagal | helper HAPUS penanda |
| A-M1 | Multi jalur lulus (submit/re-grade/interview) | semua panggil helper bersama |
| A-M2 | Request manual nyelipin worker belum 100% | gate server-side tolak |
| A-M3 | Worker lulus exam sebelum fix | backfill |
| A-M4 | Bypass CL-C tanpa penanda Tahun N-1 vs gate | bypass-assignment exempt dari gate antar-tahun |
| A-M7 | Tahun 3 belum ada deliverable di silabus | shortcut data-driven → fallback all-eligible (transisi), sampai silabus diisi |
| A-M9 | Re-grade Pass→Fail hapus penanda bypass/interview | hapus HANYA `Origin=="Exam"`; Bypass/Interview kebal |
| A-M12 | Matikan level tidak bersih | hapus juga grafik tren (CDP:592) + ViewModel + view + export yg bind level, bukan cuma 4 baris baca |
| — | Deliverable di-unapprove setelah penanda terbit | penanda tetap (historis); dashboard `allApproved` jadi false → status turun. Acceptable; override/bypass utk koreksi |

## 7. Dependency → Diskusi B (Bypass)
- Bypass M-1: pakai helper 4.1 utk terbit penanda semua tahun. ✔ selaras.
- Bypass M-3: A-3 matiin level → form bypass CL-B(a) **tanpa input level** (cukup penanda + catatan). **Update draft bypass.**
- Bypass exempt dari gate antar-tahun (A-M4).
- Resume brainstorm bypass setelah spec ini di-approve + diimplementasi.

## 8. Out of scope
- Menghidupkan kembali level kompetensi (dibuang, A-3).
- Drop kolom `CompetencyLevelGranted` (dibiarkan dormant).
- Konfigurasi gate via UI (gate = aturan tetap).
- Fitur Bypass Tahun (Diskusi B terpisah).
