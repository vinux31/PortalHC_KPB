# Phase 391: Penambahan Peserta Fleksibel saat Ujian Berjalan - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat menambah peserta baru ke sebuah assessment yang sedang berjalan (ada peserta `InProgress`) secara **fleksibel dan aman**: penambahan tidak diblokir selama window ujian terbuka, peserta baru dapat langsung mengerjakan, peserta yang sedang ujian tidak terganggu, dan HC mendapat pemberitahuan informatif. Perilaku dikunci regression test.

Surface utama: `Controllers/AssessmentAdminController.cs` → `EditAssessment` POST (blok BULK ASSIGN ~L2114-2226, edit-loop sibling ~L2058-2075, guard `Completed` ~L1992, warning TempData ~L2077-2085). Cabang Pre-Post (~L1806-1988) diperiksa untuk konsistensi.

**Bukan scope:** hard-block / cegah penambahan saat InProgress (keputusan user = fleksibel), perubahan controller/model CreateWorker (itu Phase 392), migration (0 migration).
</domain>

<decisions>
## Implementation Decisions

### Status sesi peserta baru (PART-01)
- **D-01:** Peserta baru **TIDAK mewarisi status induk** (`Status = savedAssessment.Status` di L2161 diganti). Status di-set **siap-mulai berdasarkan jadwal**: `Open` jika window ujian sudah buka (Schedule sudah tiba & `ExamWindowCloseDate` belum lewat), `Upcoming` jika jadwal belum tiba. Tujuan: peserta baru bisa `StartExam` normal + monitoring akurat (tidak muncul "InProgress" padahal `Progress=0`). Konsisten dengan cabang Pre-Post yang sudah set `Upcoming` untuk sesi baru.

### Aturan boleh-tambah / guard Completed (PART-02)
- **D-02:** HC boleh menambah peserta **selama window ujian belum tutup** — `ExamWindowCloseDate` belum lewat (jika null, fallback ke jadwal + durasi). Guard `Completed` (L1992) **tidak boleh memblokir operasi penambahan peserta** walau sesi representatif atau sebagian sesi sudah `Completed`. Cek berbasis **window**, bukan status satu sesi representatif. (Guard `Completed` untuk operasi EDIT murni tanpa penambahan boleh tetap; yang dijamin = jalur penambahan lolos selama window terbuka.)

### Proteksi sesi yang sedang berjalan (PART-01c / PART-04)
- **D-03:** Saat menyimpan Edit+Tambah, **JANGAN overwrite** `Status` / `Schedule` / `DurationMinutes` pada sesi yang **sedang berjalan** (`StartedAt != null && CompletedAt == null`). Hanya sesi belum-mulai yang ikut update field bersama. Lindungi timer & integritas ujian peserta berjalan. Perbaiki/selaraskan teks warning L2082-2084 yang saat ini menyesatkan ("tidak berlaku untuk sesi berjalan" padahal kode menimpa). Default aman: untuk sesi berjalan, jangan ubah field volatil apa pun (lihat Claude's Discretion untuk field non-volatil).

### Notice UX (PART-03)
- **D-04:** Ganti `TempData["Warning"]` kosmetik (L2082) menjadi **info non-blocking** di halaman tujuan (ManageAssessment): pesan netral menjelaskan "N peserta ditambahkan; ada peserta sedang mengerjakan — peserta baru tetap bisa langsung mulai selama window terbuka." **Tanpa konfirmasi/friksi tambahan** (sesuai keputusan fleksibel). Gaya = info/success, bukan warning kesan-error.

### Cakupan & test
- **D-05:** Surface utama = `EditAssessment` POST standar. Cabang **Pre-Post** (L1806-1988) diperiksa konsistensi: sudah set sesi baru `Upcoming` (sesuai D-01) + `return` sebelum guard `Completed` (jadi D-02 tak terblokir di sini); terapkan proteksi sesi-berjalan (D-03) bila ada overwrite serupa, dan notice (D-04) bila relevan. `CreateAssessment` (pembuatan awal) di luar fokus phase ini.
- **D-06 (PART-04):** Regression test xUnit **integration real-SQL** (pola disposable `HcPortalDB_Test_{guid}`, `[Trait Category=Integration]`, seperti `HcPortal.Tests/PostLisensorPolishTests.cs` Phase 387) mengunci: (a) tambah peserta saat ada sesi `InProgress` **berhasil** membuat sesi baru; (b) sesi baru ber-status **siap-mulai** (Open/Upcoming, bukan InProgress); (c) sesi `InProgress` existing **tidak berubah** Status/Schedule/Duration; (d) penambahan **tidak terblokir** saat sebagian sesi `Completed` selama window terbuka.

### Claude's Discretion
- Penentuan persis `Open` vs `Upcoming` untuk peserta baru (bandingkan `Schedule`/`ExamWindowCloseDate` vs now; ikuti pola `StartExam` CMPController ~L914-924 / ~L952-957).
- Penempatan cek window (helper vs inline) + perlakuan `ExamWindowCloseDate == null` (fallback jadwal+durasi).
- Bentuk persis proteksi sesi-berjalan (filter sibling sebelum loop update vs guard per-field).
- Untuk sesi berjalan, apakah field non-volatil (AllowAnswerReview/GenerateCertificate/PassPercentage/Token) ikut diubah — default aman = jangan ubah apa pun pada sesi berjalan; boleh dilonggarkan jika ada alasan kuat.
- Notice pakai `TempData["Info"]` baru atau gabung ke `Success`; wording final.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

Tidak ada spec/ADR eksternal — requirements tertangkap penuh di keputusan di atas + REQUIREMENTS.md. Anchor kode wajib dibaca:

### Requirements
- `.planning/REQUIREMENTS.md` — PART-01..04 (definisi + Out of Scope: hard-block, controller/model CreateWorker, AD-sync, migration).

### Kode surface (AssessmentAdminController.cs)
- `Controllers/AssessmentAdminController.cs` ~L1992 — guard `if (assessment.Status == "Completed")` (target D-02).
- `Controllers/AssessmentAdminController.cs` ~L2058-2075 — edit-loop overwrite SEMUA sibling Status/Schedule/Duration (target D-03).
- `Controllers/AssessmentAdminController.cs` ~L2077-2085 — `TempData["Warning"]` InProgress (target D-03 teks + D-04 notice).
- `Controllers/AssessmentAdminController.cs` ~L2114-2226 — blok BULK ASSIGN: insert sesi baru per user (L2155-2174), `Status = savedAssessment.Status` L2161 (target D-01), filter duplikat L2134-2137, notif + audit BulkAssign.
- `Controllers/AssessmentAdminController.cs` ~L1806-1988 — cabang Pre-Post (D-05 konsistensi; sesi baru `Upcoming`, return sebelum guard).

### Kode pendukung
- `Models/AssessmentConstants.cs:13-22` — `AssessmentStatus` (Open/Upcoming/InProgress/Completed/PendingGrading/Cancelled/Abandoned).
- `Controllers/CMPController.cs` ~L914-924 (auto Upcoming→Open saat jadwal tiba) & ~L952-957 (`ExamWindowCloseDate` "Ujian sudah ditutup") & ~L999-1004 (set InProgress saat StartExam pertama) — pola window/status untuk D-01/D-02.
- `HcPortal.Tests/PostLisensorPolishTests.cs` — pola disposable integration test (D-06).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentConstants.AssessmentStatus.*` — konstanta status (jangan hardcode string).
- Pola query sibling group-key (`Title == ... && Category == ... && Schedule.Date == ...`) sudah dipakai L2050-2054 / L2125-2131 — reuse untuk filter sesi berjalan (D-03).
- Window/status logic di `CMPController.StartExam` (L914-957) — sumber kebenaran untuk derive Open vs Upcoming (D-01) + cek window (D-02).
- `PostLisensorPolishTests.cs` — harness disposable real-SQL untuk D-06.

### Established Patterns
- `AssessmentSession` = **per-peserta** (1 baris = 1 user). "Satu assessment" = sibling session (Title+Category+Schedule.Date). "Tambah peserta" = INSERT sesi baru.
- Messaging via `TempData["Success"/"Warning"/"Error"]` → ditampilkan di halaman tujuan (ManageAssessment).
- BULK ASSIGN sudah idempotent (filter user yang sudah ter-assign L2134-2137) — jadi PART tidak perlu menangani duplikat lagi.

### Integration Points
- `EditAssessment` POST (`AssessmentAdminController.cs` ~L1790-2229) — titik utama perubahan D-01..D-04.
- Halaman tujuan ManageAssessment / Monitoring — tampilan notice (D-04) + akurasi status peserta baru (D-01).
- `StartExam` (CMPController) — peserta baru harus bisa masuk (validasi D-01 menghasilkan status yang lolos StartExam).
</code_context>

<specifics>
## Specific Ideas

- User tegas ingin **fleksibel tanpa friksi**: HC boleh tambah peserta kapan saja selama window terbuka, tanpa dialog konfirmasi yang menghambat.
- Notice harus terasa **informatif/menenangkan**, bukan peringatan yang membuat HC ragu.
</specifics>

<deferred>
## Deferred Ideas

- Dialog konfirmasi opsional sebelum tambah ke ujian live (friksi) — sudah di REQUIREMENTS Future; user pilih tanpa friksi.
- Bulk import peserta-ke-assessment via Excel — REQUIREMENTS Future.

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (area: database, score 0.6) — **tidak di-fold**: tidak terkait penambahan peserta; sisa housekeeping Phase 367. Tetap di backlog todo.

</deferred>

---

*Phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan*
*Context gathered: 2026-06-17*
