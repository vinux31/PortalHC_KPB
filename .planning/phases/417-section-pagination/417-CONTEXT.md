# Phase 417: Section Pagination - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Tampilan ujian default **10 soal/halaman mengalir** dengan **header Section** saat berganti Section, Section ber-`StartNewPage` **mulai di halaman baru**, Section panjang **auto-pecah per 10 soal**, tombol cepat **"semua section pisah halaman"**, dan **resume** (`LastActivePage`) mengarah ke halaman yang benar saat pagination Section aktif — dengan identitas soal stabil (by question id) dan fallback aman ke halaman 0.

**REQ:** PAG-01, PAG-02, PAG-03. **Migration: FALSE** (`LastActivePage` tetap `int?`; page-number dihitung saat render, BUKAN disimpan per-soal — D-11; kolom `StartNewPage` per-Section sudah ada dari Fase 415).

OUT (fase lain): scoped shuffle (416, sudah selesai), opsi dinamis A–F (418), export label Section di Excel/PDF (419 — PAG-04).
</domain>

<decisions>
## Implementation Decisions

### Header Section saat render ujian
- **D-417-01:** Header Section = **NAMA Section saja** (tanpa nomor) di atas grup soal Section saat render. Muncul saat berganti Section (boleh muncul di tengah halaman pada mode default, sesuai §7.1).
- **D-417-02:** Saat Section panjang **auto-pecah** ke halaman lanjutan (>10 soal), header Section **DIULANG** dengan tanda **"(lanjutan)"** di halaman sambungan, supaya peserta tetap tahu konteks Section.

### Navigator / Palette Soal (grid nomor lompat-ke-soal)
- **D-417-03:** Grid nomor soal **DIKELOMPOKKAN per-Section** dengan **label Section** di atas tiap grup (bukan flat 1..N). Konsisten dengan struktur ujian section-aware. Untuk assessment **tanpa Section** (semua `SectionId=null`) → tetap flat 1..N (perilaku lama, backward-compat).

### Indikator Halaman + Section aktif
- **D-417-04:** Navigasi halaman menampilkan **Section aktif + halaman**, mis. `"<Nama Section> — Halaman 2/5"`. Selaras D-417-01: pakai **nama Section saja** (tanpa nomor) agar konsisten dengan header. Assessment tanpa Section → `"Halaman 2/5"` saja (tanpa label Section).

### Resume saat halaman bergeser
- **D-417-05:** Saat config Section diubah HC pasca-lock → nomor halaman **dihitung ulang dari config** (locked §15.A, §7.2). Identitas soal stabil by question id; `LastActivePage` null/di luar rentang → **fallback aman ke halaman 0**.
- **D-417-06:** Saat peserta **resume** & diarahkan ke halaman terhitung → tampilkan **toast informatif** `"Lanjut dari soal no. X"` (X = nomor soal pertama di halaman tujuan). Konsisten dengan pola `showResumeFailureToast` yang sudah ada di `StartExam.cshtml`. Berlaku saat resume mengarah ke halaman >0.

### Claude's Discretion
- Bentuk persis perhitungan `PageNumber` per-soal di controller (`StartExam`): iterasi soal urut, naikkan counter halaman bila (a) `StartNewPage=true` untuk Section soal, atau (b) halaman sudah berisi `questionsPerPage` soal (§7.2). Apakah dipindah penuh ke controller via `ViewBag.SectionConfig`/precomputed page-map atau tetap dihitung di view — planner putuskan, asal output page-grouping section-aware & deterministik.
- Wording & penempatan tombol cepat **"Semua section mulai halaman baru"** di UI Kelola Section (admin surface; set `StartNewPage=true` semua Section).
- Mobile **5 soal/halaman** mengikuti aturan Section yang sama (header + `StartNewPage` + auto-pecah). Implementasi `ViewBag.QuestionsPerPage` sudah ada.
- Styling visual header Section, label grup navigator, dan toast.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Desain milestone (utama)
- `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` — desain v32.6. Untuk 417: **§7** (Pagination per-Section: aturan §7.1 + implementasi render §7.2 — hitung `PageNumber` saat render, `ViewBag.SectionConfig`, `LastActivePage` int? global, fallback page 0), **D-10** (default 10/halaman + header + StartNewPage per-section + tombol cepat + auto-pecah per 10), **D-11** (kontrol halaman di tingkat Section, BUKAN page-number per soal), **§15.A** (resume saat config berubah pasca-lock — hitung ulang, identitas soal stabil by id, fallback page 0; "Lainnya" tak punya toggle, ikut induk, urutan terakhir, tak paksa page-break), **§15.E** (mobile 5/halaman ikut aturan section; `UpdateSessionProgress` simpan currentPage; Abandoned rekam LastActivePage), **§15.D** (UI Kelola Section: toggle `StartNewPage` + tombol "Semua section mulai halaman baru").

### Konteks fase sebelumnya
- `.planning/phases/415-section-foundation-import-excel-diperluas/415-CONTEXT.md` — data model Section + kolom `StartNewPage` per-Section (dipakai 417, no new migration).
- `.planning/phases/416-scoped-shuffle-acak-per-section/416-CONTEXT.md` — urutan section-aware (`BuildSectionQuestionAssignment`, kunci komposit `(SectionNumber, ET)`) yang menghasilkan `ShuffledQuestionIds` yang dipaginate 417. Grup "Lainnya" selalu di urutan terakhir (D-15).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets / yang disesuaikan
- `Views/CMP/StartExam.cshtml` — **sudah** paginate flat per-10: `questionsPerPage` (default 10, mobile override via `ViewBag.QuestionsPerPage`), `totalPages = Ceiling(TotalQuestions/perPage)`, loop `for page` render `exam-page` div (`Skip(page*perPage).Take(perPage)`), `pageQuestionIds` map (page→question ids), `changePage()`, nav Previous/Next + mobile nav, `RESUME_PAGE = ViewBag.LastActivePage ?? 0`, `showResumeFailureToast`. → **Generalisasi ke section-aware**: page-break per-Section (`StartNewPage`) + header Section + auto-pecah per-10 dalam Section + navigator per-Section.
- `Controllers/CMPController.StartExam` (~L1266) — set `ViewBag.LastActivePage = assessment.LastActivePage ?? 0`. Tambah `ViewBag.SectionConfig` (daftar Section + `StartNewPage`) + (opsional) precomputed page-map per-soal section-aware.

### Integration Points
- `CMPController.cs:482` — `UpdateSessionProgress` set `LastActivePage = currentPage` (autosave antar-halaman). Tetap dipakai; nilai = page-index global terhitung.
- `Controllers/CMPController.cs:1195` & `AssessmentAdminController.cs:4947` — reset `LastActivePage = null` (reshuffle/restore). Hitung ulang saat render (D-417-05).
- `wwwroot/js/assessment-hub.js` — autosave SignalR flush antar-halaman; pastikan flush konsisten saat page-break section-aware (guard pre-submit/changePage menunggu pendingSaves sudah ada).
- `Models/AssessmentSession.cs:59` — `LastActivePage int?` (TAK diubah; migration=FALSE).

### Constraints
- Page-number **TIDAK** disimpan per-soal (acak merusaknya, D-11) — selalu dihitung saat render.
- Backward-compat: assessment **tanpa Section** (semua `SectionId=null`) → pagination flat per-10 IDENTIK perilaku lama (header & navigator flat, indikator tanpa label Section).
- Identitas soal stabil **by question id** lintas perubahan struktur pasca-lock.
- `LastActivePage` di luar rentang / null → fallback aman **halaman 0**.
</code_context>

<specifics>
## Specific Ideas

- Konsistensi label: header Section = **nama saja** (D-417-01) → indikator halaman juga pakai **nama Section saja** (bukan "Section N: ...") agar selaras (D-417-04).
- Toast resume (D-417-06) reuse pola `showResumeFailureToast` yang sudah ada — jangan bikin mekanisme toast baru.
- Nilai HC tetap **"fleksibel TAPI mudah"**: assessment tanpa Section = pagination lama identik; kompleksitas section-pagination hanya muncul saat HC sengaja pakai Section (D-416-04 dibawa terus).
</specifics>

<deferred>
## Deferred Ideas

- Header dengan jumlah soal / progress per-Section (mis. "3/8 soal Section ini") — ditolak di 417 (header = nama saja, D-417-01). Angkat bila HC minta progress granular per-Section.
- PAG-04 (label/header Section di export Excel/PDF) = **Fase 419** (bukan scope render ujian 417).

[Tidak ada scope creep — diskusi tetap dalam batas render pagination ujian.]
</deferred>

---

*Phase: 417-section-pagination*
*Context gathered: 2026-06-23*
