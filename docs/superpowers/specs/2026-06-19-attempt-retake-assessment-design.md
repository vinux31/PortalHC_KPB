# Attempt / Retake Assessment — Design Spec (Milestone v32.4)

- **Tanggal:** 2026-06-19
- **Milestone:** v32.4 "Ujian Ulang (Attempt Assessment)"
- **Branch:** ITHandoff
- **Status:** design approved, ready for `/gsd-new-milestone` → writing-plans
- **Migration:** TRUE (399-style: tambah kolom + 1 tabel junction baru)

---

## 1. Tujuan

Pekerja boleh **mengulang assessment** kalau gagal (skor di bawah `PassPercentage`), dengan kontrol penuh oleh Admin/HC:

- Toggle **on/off** per-assessment.
- Kalau on, batasi **berapa kali** (MaxAttempts).
- Self-service: pekerja memicu sendiri dari halaman Hasil; tidak perlu HC me-reset manual tiap kali.

Fitur ini memproduktisasi mesin `ResetAssessment` HC yang sudah ada menjadi alur self-service ber-guard, sambil menutup lubang kehilangan data jawaban per-soal yang ada di reset existing.

---

## 2. Keputusan yang Sudah Dikunci

| # | Keputusan | Pilihan |
|---|-----------|---------|
| D1 | Pemicu ujian ulang | **Self-service pekerja** (HC tetap bisa override via Reset existing) |
| D2 | Attempt mana jadi catatan resmi | **Attempt terakhir** (in-place reset; sesi current = attempt terbaru) |
| D3 | Cooldown antar percobaan | **Configurable per-assessment**, default **24 jam** (`0` = tanpa jeda) |
| D4 | Lokasi setting | **Per-assessment saja** (tanpa default kategori) |
| D5 | Feedback sebelum ulang | **Skor + tanda soal salah saja** (kunci jawaban DITAHAN sampai lulus atau attempt habis) |
| D6 | Cakupan | **Hanya assessment ber-pass-grade** (`AssessmentType != "PreTest"`); pre-test diagnostik dikecualikan |
| D7 | Saat cap habis | **Lock + HC override** ("Batas percobaan tercapai, hubungi HC"; HC Reset masih jalan, tercatat history) |
| D8 | Default MaxAttempts | **2** (1× ulang); admin bisa ubah (range 1–5) |
| D9 | Riwayat percobaan | Ditampilkan ke **pekerja DAN HC** |
| D10 | Kedalaman riwayat | **Full snapshot per-soal** (tabel baru `AssessmentAttemptResponseArchive`, snapshot sebelum delete) |

---

## 3. Arsitektur

**Pendekatan: reuse mesin `ResetAssessment`, bungkus jadi `RetakeService` bersama.**

Inti archive+reset existing diekstrak jadi service yang dipanggil dua jalur:
- **Worker path** (`CMP/RetakeExam`) — enforce guard (AllowRetake, cap, cooldown, eligibility).
- **HC path** (`ResetAssessment` existing, di-refactor) — bypass cap/cooldown (override), tetap archive.

Komponen baru:

| Komponen | Tipe | Tanggung jawab |
|----------|------|----------------|
| `Helpers/RetakeRules.cs` | static, pure | `CanRetake(...)` + `ShouldHideRetakeToggle(...)` — fungsi murni, unit-testable (mirror `ShuffleToggleRules.cs`) |
| `Services/RetakeService.cs` (+ `IRetakeService`) | service | `CanRetakeAsync(...)` + `ExecuteAsync(sessionId, initiatedBy, bypassGuards)` — archive (full snapshot) → reset → clear token TempData → audit |
| `Controllers/CMPController.RetakeExam` | action POST | endpoint worker self-service (CSRF + ownership + eligibility) |
| `AssessmentAdminController.UpdateRetakeSettings` | action POST | config per-assessment + sibling propagation (mirror `UpdateShuffleSettings:5556`) |
| `Models/AssessmentAttemptResponseArchive.cs` | entity | snapshot jawaban per-soal tiap attempt |

`ResetAssessment` di-refactor agar memanggil `RetakeService.ExecuteAsync(..., bypassGuards: true)` — guard (TempData/redirect) tetap di controller, logic inti pindah ke service yang return `(bool success, string? error)`.

---

## 4. Perubahan Model Data (migration=TRUE)

### 4.1 `AssessmentSession` — 3 kolom baru (mirror shuffle, `AssessmentSession.cs:38-42`)

```csharp
[Display(Name = "Izinkan Ujian Ulang")]
public bool AllowRetake { get; set; } = false;

[Range(1, 5)]
[Display(Name = "Maksimal Percobaan")]
public int MaxAttempts { get; set; } = 2;

[Range(0, 168)] // 0 = tanpa jeda; cap 1 minggu
[Display(Name = "Jeda Ujian Ulang (jam)")]
public int RetakeCooldownHours { get; set; } = 24;
```

EF default (false / 2 / 24) menutup SEMUA jalur create (standard, Pre/Post, `EditAssessment` bulk-add, `ProtonBypassService.cs:329`) — tidak ada raw SQL insert. **Tetap set eksplisit** di jalur bulk-add `EditAssessment` (mengcopy dari `savedAssessment`, mirror cara shuffle dicopy) agar tidak membingungkan.

### 4.2 `AssessmentAttemptResponseArchive` — tabel baru

Snapshot jawaban per-soal sebelum `PackageUserResponses` dihapus saat reset/retake. Menutup lubang data-loss existing.

```csharp
public class AssessmentAttemptResponseArchive
{
    public int Id { get; set; }
    public int AttemptHistoryId { get; set; }       // FK → AssessmentAttemptHistory.Id (cascade)
    public int QuestionId { get; set; }             // soal asli (plain int, no FK — soal bisa terhapus)
    public string? SelectedOptionIds { get; set; }  // CSV/JSON opsi terpilih (MC/MA)
    public string? EssayText { get; set; }          // jawaban essay
    public decimal? AwardedScore { get; set; }      // skor per-soal (termasuk nilai essay manual)
    public bool IsCorrect { get; set; }             // hasil grading per-soal (untuk "tanda soal salah")
    public DateTime ArchivedAt { get; set; }
}
```

- FK `AttemptHistoryId` → `AssessmentAttemptHistory` (sudah ada, punya `AttemptNumber`). Index di `AttemptHistoryId`.
- `QuestionId` plain int (konsisten pola `AssessmentAttemptHistory.SessionId` yang juga plain int).
- Snapshot di-build dari `PackageUserResponses` + hasil grading **sebelum** `RemoveRange` (`ResetAssessment:4261-4266`).

---

## 5. Aturan Kelayakan (`RetakeRules.cs`)

```
CanRetake =
     AllowRetake == true
  && AssessmentType != "PreTest"        // D6: graded only, exclude diagnostik
  && IsManualEntry == false             // MUST-FIX #2: hasil inject tidak retakeable
  && Status == "Completed"              // exclude InProgress/Abandoned/Cancelled/PendingGrading
  && IsPassed == false                  // gagal (null = PendingGrading → tidak eligible)
  && attemptsUsed < MaxAttempts         // D7: cap
  && cooldownElapsed                    // D3
```

- `attemptsUsed = archivedCount(UserId, Title, Category) + 1`
  **MUST-FIX #3:** grouping pakai `(UserId, Title, Category)` (bukan `(UserId, Title)` saja) agar Pre+Post ber-Title sama tidak terkonflasi (`SiblingPrePostFilterTests` membuktikan keduanya coexist). Konsisten dgn `WorkerDataService.cs:160-185` + tambahan filter Category.
- `cooldownElapsed = (RetakeCooldownHours == 0) || (DateTime.UtcNow >= session.CompletedAt.Value.AddHours(RetakeCooldownHours))`
  `CompletedAt` reliably di-set saat gagal oleh `GradingService` (verified `:4254`). Pakai UTC konsisten.
- `ShouldHideRetakeToggle(assessmentType, isManualEntry)` → sembunyikan card config kalau `AssessmentType == "PreTest"` ATAU `IsManualEntry == true`. (Proton TETAP boleh retakeable — beda dari shuffle yang hide Proton T3.)

Cooldown countdown ke pekerja = `session.CompletedAt + RetakeCooldownHours − now` (menit), dihitung server-side; juga **divalidasi ulang server-side** di `RetakeExam` (jangan percaya client).

---

## 6. Alur Self-Service Pekerja

1. Pekerja submit → gagal → `Status="Completed"`, `IsPassed=false`, `CompletedAt` set.
2. Halaman **Hasil** (`Results.cshtml`): kalau `CanRetake` → tombol **"Ujian Ulang"** + "Percobaan ke-X dari N" + countdown cooldown (kalau belum lewat, tombol disabled). Kalau attempt habis → "Batas percobaan tercapai, hubungi HC".
3. POST `CMP/RetakeExam(id)` — `[ValidateAntiForgeryToken]`, ownership (`session.UserId == user.Id`), re-cek `CanRetakeAsync` server-side (copy pola `SaveAnswer:348`).
4. `RetakeService.ExecuteAsync`:
   a. **Claim transisi atomik** (MUST-FIX #4): `ExecuteUpdateAsync WHERE Status=="Completed" → "Open"`, cek `rowsAffected`. Kalau 0 → race kalah, abort (anti double-archive dari double-click).
   b. **Snapshot per-soal** ke `AssessmentAttemptResponseArchive` (D10) — sebelum delete.
   c. Archive ke `AssessmentAttemptHistory` (`AttemptNumber = archivedCount+1`).
   d. Delete `PackageUserResponses` + `UserPackageAssignment` + `SessionElemenTeknisScores` (existing).
   e. Reset field sesi (Score/IsPassed/Progress/StartedAt/CompletedAt/ElapsedSeconds/LastActivePage = null/0).
   f. **Clear `TempData["TokenVerified_{id}"]`** (MUST-FIX #1) — paksa verifikasi ulang token.
   g. Audit `ActionType="RetakeAssessment"` (MUST-FIX #6), pesan: "Pekerja {nip} ulang assessment '{title}' (percobaan {n}/{max})".
   h. SignalR `sessionReset { reason: "worker_retake" }` (parameterize `reason`, dulu hardcode "hc_reset").
5. Redirect `StartExam(id)` — re-entry bersih (verified `:901`, `StartedAt==null` trigger fresh; token-required minta token lagi via `session.AccessToken` existing).

**HC override:** `ResetAssessment` existing memanggil `ExecuteAsync(..., bypassGuards: true, reason: "hc_reset")` — tetap snapshot + archive, lewati cap/cooldown.

---

## 7. Gating Review Jawaban + Tier Feedback Baru

**MUST-FIX #7:** Saat ini `Results` cuma punya 2 mode — `AllowAnswerReview=true` (tampil kunci penuh) atau `false` (skor saja, tanpa tanda salah). D5 butuh **tier ketiga**: per-soal benar/salah, **tanpa** kunci.

Logic di `Results` action (`CMPController:2243`) + ViewModel:
```
attemptsRemain    = AllowRetake && (attemptsUsed < MaxAttempts)
canRetakeNow      = failed && attemptsRemain          // masih bisa ulang
showWrongFlagsOnly = canRetakeNow                     // skor + ✓/✗ per-soal, kunci tersembunyi
showFullReview     = AllowAnswerReview && !canRetakeNow  // lulus ATAU attempt habis → review normal
```
- `showFullReview` → render `QuestionReviews` lengkap (existing `Results.cshtml:316`).
- `showWrongFlagsOnly` → render per-soal hanya benar/salah (ikon ✓/✗), sembunyikan teks kunci & pembahasan.
- Lainnya → skor saja.

Setelah pekerja LULUS atau attempt HABIS → `AllowAnswerReview` normal berlaku.

---

## 8. UI Config Admin (mirror shuffle)

- **Card "Ujian Ulang"** di `ManagePackages.cshtml` (mirror card shuffle `:84-132`): toggle `AllowRetake` + input `MaxAttempts` (1–5) + `RetakeCooldownHours` (0–168). Sembunyikan via `ShouldHideRetakeToggle` (Pre-Test / Manual Entry). Tidak ada hard-lock (lihat catatan "Lock" di bawah).
- **Binding** di `CreateAssessment.cshtml` (Step 3, antara Scoring & Certificate, setelah card shuffle ~`:536`) + `EditAssessment.cshtml` (dekat PassPercentage `:392`).
- **Endpoint `UpdateRetakeSettings(int assessmentId, bool allowRetake, int maxAttempts, int retakeCooldownHours)`** — `[ValidateAntiForgeryToken]`, **sibling propagation** key `(Title, Category, Schedule.Date)` (mirror `:5564-5566`), audit. Config = policy batch-level (semua peserta satu batch sama); `attemptsUsed` tetap per-user.
- **Lock:** beda dari shuffle (yang lock saat ujian mulai). Config retake = policy yang boleh berubah kapan saja. Catatan: jangan turunkan `MaxAttempts` di bawah `attemptsUsed` peserta mana pun yang sedang aktif — tampilkan warning (non-blocking), tidak hard-lock.

---

## 9. UI Riwayat Percobaan (D9 — pekerja + HC)

- **Pekerja** (`Results.cshtml` / `Records.cshtml`): daftar attempt ("Percobaan 1: 65% — Gagal", "Percobaan 2: 80% — Lulus") + drill-down per-soal benar/salah dari `AssessmentAttemptResponseArchive` (tunduk gating §7). Tandai mana attempt current.
- **HC** (`AssessmentMonitoringDetail.cshtml`): kolom/drill-down "Riwayat Percobaan" per-pekerja — semua attempt (archived + current) dengan skor, pass/fail, tanggal, dan detail per-soal. Saat ini halaman cuma tampil sesi current → net-new view component.
- `AllWorkersHistoryRow` ditambah flag `IsCurrentAttempt` agar export/Records tidak ambigu attempt mana yang current (gap #9 downstream).

---

## 10. Grading & Sertifikat (tanpa perubahan inti)

- Guard anti-double-cert existing aman untuk retake-lalu-lulus: unique index `IX_AssessmentSessions_NomorSertifikat` + `WHERE NomorSertifikat==null` (`GradingService:302`) + retry 3×. Retake yang lulus → 1 cert.
- **HC reset sesi yang sudah LULUS:** cert lama TIDAK dicabut (perilaku existing) — kalau re-pass, `WHERE NomorSertifikat==null` menjaga tidak terbit dobel. Didokumentasikan sebagai perilaku sengaja.
- **Retake ≠ Renewal:** retake tidak men-set `RenewsSessionId`/`RenewsTrainingId`; terminal attempt, tidak ikut rantai renewal cert.
- **Pre/Post comparison:** in-place reset (D2) → `LinkedSessionId` tetap valid, perbandingan pakai skor attempt terkini. Karena Pre = diagnostik (AllowRetake=false), hanya Post yang retakeable; tidak ada cross-pair propagation.

---

## 11. Ringkasan 7 Must-Fix (dari verifikasi adversarial)

| # | Isu | File:line | Penanganan |
|---|-----|-----------|------------|
| 1 | Token stale `TempData.Peek` | `StartExam:944` | clear `TokenVerified_{id}` di `ExecuteAsync` |
| 2 | `IsManualEntry` retakeable | `IsResettable:4186` | `CanRetake` exclude IsManualEntry |
| 3 | Conflation count Pre+Post | `WorkerDataService:160-185` | grouping `(UserId,Title,Category)` |
| 4 | Race double-archive | `ResetAssessment:4286-4288` | claim transisi atomik dulu, baru archive |
| 5 | Essay PendingGrading | `GradingService` FinalizeEssayGrading | eligibility `Status=="Completed" && IsPassed==false` exclude null |
| 6 | Audit type belum ada | `:4318` "ResetAssessment" | define `"RetakeAssessment"` |
| 7 | Tier feedback tengah belum ada | `Results:2243` | tambah `showWrongFlagsOnly` |

---

## 12. Requirements (RTK-01 … RTK-14)

| ID | Requirement | Fase |
|----|-------------|------|
| RTK-01 | Kolom config `AllowRetake`/`MaxAttempts`/`RetakeCooldownHours` di AssessmentSession + migration + binding semua jalur create | 405 |
| RTK-02 | Tabel `AssessmentAttemptResponseArchive` + migration (snapshot per-soal) | 405 |
| RTK-03 | `RetakeRules.CanRetake` + `ShouldHideRetakeToggle` (pure, unit-tested) | 405 |
| RTK-04 | Endpoint `UpdateRetakeSettings` + sibling propagation | 405 |
| RTK-05 | `RetakeService.ExecuteAsync` — snapshot+archive+reset+clear-token+audit, claim atomik | 405 |
| RTK-06 | Refactor `ResetAssessment` → panggil service (HC override bypass) | 405 |
| RTK-07 | Card config UI (ManagePackages + Create/Edit) + warning MaxAttempts<used | 406 |
| RTK-08 | View riwayat percobaan HC (Monitoring Detail drill-down) | 406 |
| RTK-09 | Endpoint worker `CMP/RetakeExam` (CSRF+ownership+eligibility) | 407 |
| RTK-10 | Results UI: tombol Ujian Ulang + "Percobaan X/N" + cooldown countdown + lock message | 407 |
| RTK-11 | Gating review + tier feedback baru (skor+tanda salah) | 407 |
| RTK-12 | View riwayat percobaan pekerja (Results/Records) + flag IsCurrentAttempt | 407 |
| RTK-13 | Guards komprehensif (PreTest/IsManualEntry/PendingGrading/Cancelled/Abandoned) | 405+407 |
| RTK-14 | Test & UAT (unit RetakeRules+RetakeService, Playwright lifecycle, integration, security) | 408 |

---

## 13. Pecahan Fase & Dependency

```
405 (Backend Core: data+migration + RetakeRules + RetakeService + refactor Reset + config endpoint)
  ├─→ 406 (Admin Config UI + riwayat HC)                    ┐
  └─→ 407 (Worker Self-Service + gating + riwayat pekerja)  ┘  ← 406 ∥ 407 PARALEL (depend 405)
          └─→ 408 (Test & UAT)
```

- **405** — Backend Core. Migration=TRUE (3 kolom + tabel `AssessmentAttemptResponseArchive`); `RetakeRules` (pure); `RetakeService.ExecuteAsync` (snapshot+archive+reset+clear-token+audit, claim atomik); refactor `ResetAssessment`→service (HC override bypass); `UpdateRetakeSettings` + sibling propagation; binding Create/Edit. No UI. **Depends: —**
- **406** — Admin config UI (card ManagePackages + binding Create/Edit) + view riwayat HC (Monitoring Detail). **Depends: 405.**
- **407** — Worker self-service: `CMP/RetakeExam` + Results UI (tombol/counter/cooldown/lock) + gating tier-feedback baru + view riwayat pekerja. **Depends: 405.**
- **408** — Test & UAT menyeluruh (unit RetakeRules+RetakeService, Playwright lifecycle, integration, security). **Depends: 406+407.**

Wave eksekusi: `405` → `(406 ∥ 407)` → `408`. (4 fase; backend digabung, UI/worker tetap paralel.)

---

## 14. Strategi Test

- **Unit (xUnit):** `RetakeRulesTests` (semua cabang CanRetake + ShouldHideRetakeToggle), `RetakeServiceTests` (snapshot lengkap, claim atomik anti double-archive, clear token, bypass HC, cooldown boundary).
- **Integration:** retake-then-pass → 1 cert; counting `(UserId,Title,Category)` tidak konflasi Pre/Post; sibling propagation config.
- **Playwright (port 5270):** lifecycle penuh — gagal → lihat skor+tanda-salah (kunci tersembunyi) → tombol Ujian Ulang → cooldown gate → ulang → lulus → cert; attempt habis → lock message; riwayat tampil di pekerja + HC.
- **Security:** RBAC (worker hanya sesi sendiri), antiforgery, server-side cooldown/cap revalidation, no answer-key leak saat retake-eligible.

---

## 15. Out of Scope (YAGNI)

- Grading method selain "attempt terakhir" (highest/average) — D2 sudah putuskan latest.
- Cooldown escalating (ISC2-style 30/60/90) — flat configurable cukup.
- Default MaxAttempts per-kategori — D4 putuskan per-assessment saja.
- Pre-retake remediation/reflection gate.
- Rotasi AccessToken per-attempt (model token batch existing diterima apa adanya).
- Cap attempt per-tahun (per-assessment cap cukup untuk konteks internal).

---

## 16. Risiko Tersisa (low, dipantau)

- Clock skew/DST pada cooldown — mitigasi: konsisten `DateTime.UtcNow`.
- Impersonation user-mode memicu retake — `RetakeService` pakai identitas efektif (`ImpersonationService.GetEffectiveTargetUserId`) konsisten dgn ownership; ditest.
- Two-phase commit (snapshot/archive vs status-flip) — diperkuat claim-transisi-dulu (#4); sisa window teoretis sangat kecil (sama DB, satu sesi).
