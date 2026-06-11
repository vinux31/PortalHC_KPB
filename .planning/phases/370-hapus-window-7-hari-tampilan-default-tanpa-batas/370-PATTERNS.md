# Phase 370: Hapus Window 7-Hari (Tampilan Default Tanpa Batas) - Pattern Map

**Mapped:** 2026-06-11
**Files analyzed:** 2 (1 modified, 1 deleted)
**Analogs found:** 2 / 2

> **Catatan jenis fase:** Ini fase **REMOVAL/refactor**, bukan penambahan file baru. "Analog" di sini = (a) preseden cara fase lain menghapus dead-code secara bersih, dan (b) pola in-file existing yang sudah hidup di file yang sama (mis. `.AsNoTracking()` Phase 311). Planner copy *teknik penghapusan*, bukan struktur file baru.

---

## File Classification

| File | Aksi | Role | Data Flow | Closest Analog | Match Quality |
|------|------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` | MODIFY (hapus helper + 2 call site + 2 var + komentar stale; tambah 1 `.AsNoTracking()`) | controller | request-response (read-only query + in-memory group) | In-file: `ManageAssessmentTab_Assessment:120-122` (pola `.AsNoTracking()` Phase 311) + cross-phase: Phase 359-04 atomic prune | exact (in-file) |
| `HcPortal.Tests/AssessmentSearchWindowTests.cs` | DELETE (utuh) | test (xUnit unit, 3 [Fact]) | n/a (pure LINQ-to-Objects) | Phase 359-04 atomic prune precedent (compile-coupling) | role-match |

---

## Pattern Assignments

### `Controllers/AssessmentAdminController.cs` (controller, request-response)

**Empat edit terpisah dalam 1 file. Sumber kebenaran = state kode SAAT INI (di bawah), bukan parafrase RESEARCH.**

---

#### Edit A — `ManageAssessmentTab_Assessment` (hapus var + call + komentar stale)

**State SAAT INI** (`AssessmentAdminController.cs:114-125`):
```csharp
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);                          // :116 — HAPUS

            // Phase 311 Plan 03: AsNoTracking di chain start read-only partial action (PATTERNS Cross-cutting Pattern 2)
            // Quick fix (260611-m9r): window 7-hari via helper — di-skip saat search non-empty (cari sesi lama >7 hari).  // :119 — HAPUS baris ini
            var managementQuery = _context.AssessmentSessions
                .AsNoTracking()
                .AsQueryable();
            managementQuery = ApplySevenDayWindow(managementQuery, search, sevenDaysAgo);  // :123 — HAPUS

            if (!string.IsNullOrEmpty(search))
```

**State TARGET** (Phase 370):
```csharp
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Phase 370 (URG-02): window 7-hari dihapus — tampilan default tanpa batas umur.
            // Phase 311 Plan 03: AsNoTracking di chain start read-only partial action.
            var managementQuery = _context.AssessmentSessions
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
```

**Yang HARUS dipertahankan verbatim (JANGAN sentuh):**
- `var sw = Stopwatch.StartNew();` (:115) — telemetry Phase 311, INDEPENDEN dari `sevenDaysAgo`. (Open Question RESEARCH: hapus HANYA baris `sevenDaysAgo`.)
- `.AsNoTracking().AsQueryable()` (:120-122) — sudah ada sejak Phase 311, TETAP.
- Baris `if (!string.IsNullOrEmpty(search))` (:125+) dst — TETAP (search behavior tidak regresi).

---

#### Edit B — Definisi helper `ApplySevenDayWindow` (hapus blok utuh + header komentar)

**State SAAT INI** (`AssessmentAdminController.cs:2816-2826`):
```csharp
        // Quick fix (260611-m9r): window 7-hari hanya berlaku saat search KOSONG.
        // Search eksplisit dari user mengalahkan penyempitan default (preseden CIL-02 Phase 338),
        // supaya assessment lama (>7 hari, mis. Post Test OJT) bisa ditemukan via search.
        // Static + pure → testable (xUnit) via LINQ-to-Objects.
        public static IQueryable<AssessmentSession> ApplySevenDayWindow(
            IQueryable<AssessmentSession> query, string? search, DateTime cutoff)
        {
            if (string.IsNullOrEmpty(search))
                return query.Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= cutoff);
            return query;
        }
```

**State TARGET:** seluruh blok `:2816-2826` (4 baris komentar + method body) **DIHAPUS**.

**Konteks aman:** blok sebelumnya berakhir `:2814` (`return "Not started";` + `}`); blok sesudahnya `:2828` (`// MAM-06: Tab2 Input Records...` → `IsTrainingInitialState`). Hapus tanpa menyentuh dua tetangga ini. `IsTrainingInitialState` (:2830) adalah helper static lain yang TETAP.

---

#### Edit C — `AssessmentMonitoring` (hapus var + call + komentar 90-review/m9r + tambah `.AsNoTracking()`)

**State SAAT INI** (`AssessmentAdminController.cs:2864-2873`):
```csharp
            // 7-day window — same as ManageAssessment.                                    // :2865 — HAPUS (90-review stale)
            // Quick fix (260611-m9r): window HANYA saat search kosong. Saat search non-empty,  // :2866 — HAPUS
            // window di-skip supaya assessment lama (>7 hari) bisa ditemukan via search.        // :2867 — HAPUS
            // Abandoned sessions tanpa ExamWindowCloseDate fallback ke Schedule untuk cek window // :2868 — HAPUS
            // (relevan hanya saat search kosong).                                                // :2869 — HAPUS
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);                                       // :2870 — HAPUS

            var query = _context.AssessmentSessions.AsQueryable();                                // :2872 — UBAH (+ .AsNoTracking)
            query = ApplySevenDayWindow(query, search, sevenDaysAgo);                             // :2873 — HAPUS

            // Text search by title
            if (!string.IsNullOrEmpty(search))
```

**State TARGET** (Phase 370):
```csharp
            // Phase 370 (URG-02): window 7-hari dihapus — tampilan default tanpa batas umur.
            // D-05: .AsNoTracking() — read-only method (no SaveChanges), selaras Tab Assessment Phase 311.
            var query = _context.AssessmentSessions
                .AsNoTracking()
                .AsQueryable();

            // Text search by title
            if (!string.IsNullOrEmpty(search))
```

**Yang HARUS dipertahankan verbatim (JANGAN sentuh):**
- `if (!string.IsNullOrEmpty(search))` + blok MAP-23 (`:2876-2881`) — TETAP.
- Category filter (`:2884-2885`) — TETAP.
- Seluruh grouping + CIL-01 badge (`:3030-3033`) + CIL-02 hide-Closed + MAP-15 status="All" (`:3035-3053`) — TETAP utuh (downstream dari window, tidak terdampak).

---

### `HcPortal.Tests/AssessmentSearchWindowTests.cs` (test, DELETE)

**Analog/preseden:** Phase 359-04 atomic-prune (`.planning/phases/359-gate-berurutan-cleanup-a/359-04-SUMMARY.md`).

**Kenapa harus dihapus, bukan diedit:** file ini me-reference `AssessmentAdminController.ApplySevenDayWindow` di 3 tempat (`:33`, `:44`, `:55`). Setelah helper dihapus (Edit B), test project gagal compile (`CS0117`). File berisi 3 [Fact] yang menguji perilaku helper yang sudah tidak ada — tidak di-repurpose (D-02).

**Pola compile-coupling (dari Phase 359-04):**
> "Task 1 (model/controller) dan Task 2 (view) di-commit dalam 1 commit atomik. Model prune sendiri memecah build... Commit terpisah akan meninggalkan intermediate broken-build."

**Terjemahan ke Phase 370:** hapus file test (Edit-D) **dalam commit yang sama** dengan hapus helper (Edit B), supaya tiap commit build+suite hijau. Suite turun 229 → 226 (3 [Fact] hilang). Gunakan `git rm` untuk file ini (bukan Edit/Write).

---

## Shared Patterns

### `.AsNoTracking()` pada read-only IQueryable (Phase 311)
**Source (in-file, sudah hidup):** `Controllers/AssessmentAdminController.cs:120-122` (`ManageAssessmentTab_Assessment`)
**Apply to:** `AssessmentMonitoring` query start (`:2872`) — D-05.
```csharp
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .AsQueryable();
```
Monitoring murni read (tidak ada `SaveChanges` di method) → aman. Ini bukan migration, bukan dependency baru — 1 baris chain.

### Atomic prune saat compile-coupling (Phase 359-04)
**Source:** `.planning/phases/359-gate-berurutan-cleanup-a/359-04-SUMMARY.md`
**Apply to:** koordinasi commit hapus-helper (Edit B) + hapus-test-file (Edit-D).
**Aturan:** jangan tinggalkan intermediate broken-build. Helper + file test hilang dalam satu commit logis. Discretion D-01 mengizinkan urutan, tapi gate tiap commit = `dotnet build` 0 error + `dotnet test` hijau.

### Komentar stale wajib dibersihkan (D-01 + Pitfall 4)
**Apply to:** 3 lokasi komentar yang menyebut window/quick-fix:
- `:119` (Tab Assessment) — baris "Quick fix (260611-m9r): window 7-hari..."
- `:2816-2819` (header helper) — 4 baris komentar quick-fix
- `:2865-2869` (Monitoring) — komentar "7-day window — same as ManageAssessment" (90-review) + 260611-m9r + Abandoned-fallback
**Guard:** grep `7-day|260611-m9r|90-review|ApplySevenDayWindow|sevenDaysAgo` di `Controllers/` → zero hit setelah edit.

---

## No Analog Found

Tidak ada. Kedua file punya preseden langsung:
- Penghapusan filter query + tambah `.AsNoTracking()` → pola in-file Phase 311 (`:120-122`) + Phase 359-04 prune teknik.
- Penghapusan test file karena compile-coupling → pola atomic-prune Phase 359-04.

---

## Verifikasi (grep-guard baseline — sudah dijalankan)

Grep `ApplySevenDayWindow|sevenDaysAgo|7-day window|260611-m9r` (scope `**/*.{cs,cshtml,ts,js}`) SAAT INI mengembalikan tepat:
- `Controllers/AssessmentAdminController.cs` — 9 hit (`:116, :119, :123, :2816, :2820, :2865, :2866, :2870, :2873`)
- `HcPortal.Tests/AssessmentSearchWindowTests.cs` — 4 hit (`:1, :33, :44, :55`)
- **Zero hit di `Views/`, `wwwroot/`, `tests/e2e/`** — blast radius minimal terkonfirmasi.

**Target setelah fase:** zero hit di kedua file di atas (controller hits hilang via edit, test hits hilang via `git rm`). `.planning/` docs historis SENGAJA tidak digrep (Pitfall 3 — bukan compiled code).

---

## Metadata

**Analog search scope:** `Controllers/`, `HcPortal.Tests/`, `Views/`, `wwwroot/`, `tests/`, `.planning/phases/359-*` (preseden prune)
**Files scanned:** AssessmentAdminController.cs (regions :105-234, :2805-2934, :3020-3068), AssessmentSearchWindowTests.cs (utuh), 359-04-SUMMARY.md
**Pattern extraction date:** 2026-06-11
