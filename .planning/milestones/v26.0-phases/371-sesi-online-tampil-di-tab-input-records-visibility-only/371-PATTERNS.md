# Phase 371: Sesi Online Tampil di Tab Input Records (visibility-only) - Pattern Map

**Mapped:** 2026-06-11
**Files analyzed:** 1 modified (`Views/Admin/Shared/_TrainingRecordsTab.cshtml`)
**Analogs found:** 6 / 6 (all in-codebase, all verbatim-verified)

> Phase 371 = recombination, bukan creation. Satu file diubah. Semua pattern di bawah ini adalah excerpt VERBATIM dari kode existing (file:line). Planner menyalin/extend, tidak menemukan ulang. Lesson Phase 354: Razor anon-shape mismatch lolos `dotnet build`+grep tapi crash runtime (`RuntimeBinderException`) — `.Concat()` butuh shape IDENTIK (nama+tipe+urutan) di SEMUA operand.

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (:262-380) | view (HTMX partial) | transform (anon-LINQ projection → table render) | self (in-file analog: training/manual branch :263-369) | exact (extend existing pattern in same file) |

**Sub-region map (single file):**

| Region | Lines | What changes (Phase 371) |
|--------|-------|--------------------------|
| Anon row projection | :263-268 | Add `onlineRows` projection; unify anon shape across 3 sources for `.Concat()` |
| Empty-state | :270-281 | Copy "Belum ada record **manual** untuk pekerja ini." → "Belum ada record untuk pekerja ini." (D-04, drop "manual") |
| Badge Tipe | :309-318 | Extend 2-way (Training/Assessment Manual) → 3-way (+ "Assessment Online") |
| Status switch | :322-333 | Add online derivation (6-way label + warna; D-03). Manual mapping UNCHANGED |
| Kolom Aksi | :338-369 | Add online branch: "Lihat hasil" → `CMP/Results` (Completed/PendingGrading only); NO Edit/Hapus (placeholder 367) |

---

## Pattern Assignments

### `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (view, transform)

**Analog:** self (extend existing in-file pattern)

---

#### 1. CURRENT anon-type shape — `trainingRows` + `assessmentRows` + `allRows` (VERBATIM :263-268)

> **CRITICAL (Pitfall 1 + lesson Phase 354):** `.Concat()` di :268 menuntut shape anon IDENTIK (nama, tipe, URUTAN property sama persis) di semua operand. `trainingRows` dan `assessmentRows` saat ini SUDAH identik (8 property, urutan sama). Menambah `onlineRows` WAJIB pakai shape yang sama persis ATAU semua 3 proyeksi di-rewrite ke satu shape baru. `ValidUntil` bertipe `DateOnly?` di KEDUA sumber (verified §5) — JANGAN campur `DateTime?`.

Current shape = 8 property, urutan: `Type, Date, Title, Detail, Status, ValidUntil, Id, Score, IsPassed` (9 — note: training punya `Score=(int?)null`).

```razor
                                    @{
                                        var trainingRows = (worker.TrainingRecords ?? new List<HcPortal.Models.TrainingRecord>())
                                            .Select(r => new { Type = "Training", Date = r.Tanggal, Title = r.Judul, Detail = r.Penyelenggara ?? "—", Status = r.Status ?? "", ValidUntil = r.ValidUntil, Id = r.Id, Score = (int?)null, IsPassed = (bool?)(r.Status == "Passed" || r.Status == "Valid") });
                                        var assessmentRows = (worker.AssessmentSessions ?? new List<HcPortal.Models.AssessmentSession>())
                                            .Where(a => a.IsManualEntry)
                                            .Select(a => new { Type = "Assessment", Date = a.CompletedAt ?? a.Schedule, Title = a.Title, Detail = a.Score.HasValue ? $"{a.Score} - {(a.IsPassed == true ? "Lulus" : a.IsPassed == false ? "Tidak Lulus" : AssessmentConstants.AssessmentStatus.PendingGrading)}" : "—", Status = a.IsPassed == true ? "Passed" : a.IsPassed == false ? "Failed" : AssessmentConstants.AssessmentStatus.PendingGrading, ValidUntil = a.ValidUntil, Id = a.Id, Score = a.Score, IsPassed = a.IsPassed });
                                        var allRows = trainingRows.Concat(assessmentRows).OrderByDescending(r => r.Date).ToList();
                                    }
```

**Exact field-by-field shape (the contract `onlineRows` must satisfy):**

| # | Property | Type (training) | Type (manual asm) | Notes |
|---|----------|-----------------|-------------------|-------|
| 1 | `Type` | `string` `"Training"` | `string` `"Assessment"` | string penanda — branch badge & aksi. Online add `"AssessmentOnline"` (planner choice of literal) |
| 2 | `Date` | `DateTime` (`r.Tanggal`) | `DateTime` (`a.CompletedAt ?? a.Schedule`) | sort key `OrderByDescending`. Online: `a.CompletedAt ?? a.Schedule` (D-05) |
| 3 | `Title` | `string` (`r.Judul`) | `string` (`a.Title`) | Razor auto-encode |
| 4 | `Detail` | `string` (`r.Penyelenggara ?? "—"`) | `string` (interpolated) | Online belum-selesai = `"—"` (D-05) |
| 5 | `Status` | `string` (`r.Status ?? ""`) | `string` (Passed/Failed/PendingGrading) | label MENTAH; switch :323 derives warna. Online butuh label final 6-way (D-03) |
| 6 | `ValidUntil` | `DateOnly?` (`r.ValidUntil`) | `DateOnly?` (`a.ValidUntil`) | **MUST be `DateOnly?`** (Pitfall 2). Online: `a.ValidUntil` apa adanya (umumnya null) |
| 7 | `Id` | `int` (`r.Id`) | `int` (`a.Id`) | Online: `a.Id` = AssessmentSession.Id → dipakai `CMP/Results?id=` |
| 8 | `Score` | `(int?)null` | `int?` (`a.Score`) | Online: `a.Score` |
| 9 | `IsPassed` | `(bool?)(...)` | `bool?` (`a.IsPassed`) | Online: `a.IsPassed` (null saat belum selesai — JANGAN map ke "Tidak Lulus", Anti-Pattern D-03) |

> **Planner note (research Pattern 1 + Code Examples):** RESEARCH merekomendasikan menambah field umum (`StatusClass`/`StatusLabel`/`IsOnline`/`CanViewResult`) ke KETIGA proyeksi sehingga branch render lebih bersih. Itu mengubah shape ketiga proyeksi serentak (boleh, asal SEMUA 3 identik). Alternatif minimal: pertahankan 9-field shape existing + tambah online dengan `Status` = label final + branch render via `row.Type`. Apapun pilihannya: **shape ketiga operand `.Concat()` HARUS identik persis.**

---

#### 2. Empty-state block (VERBATIM :270-281) — D-04 target edit

> **Pitfall 5:** Copy "manual" hanya diubah DI SINI (:273). JANGAN sentuh empty-state level-list di :201 ("Tidak ada pekerja ditemukan" — beda konteks). Tombol Tambah Training/Assessment TETAP (D-04).

```razor
                                    @if (allRows.Count == 0)
                                    {
                                        <div class="text-center py-3 text-muted">
                                            <i class="bi bi-inbox me-2"></i>Belum ada record manual untuk pekerja ini.
                                            <a href="@Url.Action("AddTraining", "TrainingAdmin")" class="btn btn-sm btn-primary ms-3">
                                                <i class="bi bi-plus-lg me-1"></i>Tambah Training
                                            </a>
                                            <a href="@Url.Action("AddManualAssessment", "TrainingAdmin")" class="btn btn-sm btn-info text-white ms-1">
                                                <i class="bi bi-plus-lg me-1"></i>Tambah Assessment
                                            </a>
                                        </div>
                                    }
```

**Single change:** `Belum ada record manual untuk pekerja ini.` → `Belum ada record untuk pekerja ini.` (drop kata "manual"). Sisanya verbatim.

---

#### 3. Badge Tipe branch (VERBATIM :309-318) — IN-FILE ANALOG for 3-way extend

> Claude's Discretion: posisi badge "Assessment Online" mengikuti pola ini (kolom Tipe). Current = 2-way (Training → `bg-success`; else Manual → `bg-info text-white`). Online add a 3rd branch (RESEARCH suggests `bg-secondary`).

```razor
                                                            <td>
                                                                @if (row.Type == "Training")
                                                                {
                                                                    <span class="badge bg-success">Training Manual</span>
                                                                }
                                                                else
                                                                {
                                                                    <span class="badge bg-info text-white">Assessment Manual</span>
                                                                }
                                                            </td>
```

**Extend pattern (research §"Badge Tipe 3-way"):** keep Training (`bg-success`), AssessmentManual (`bg-info text-white`), add AssessmentOnline (`bg-secondary` "Assessment Online"). Branch on `row.Type`.

---

#### 4. Status switch (VERBATIM :322-333) — IN-FILE ANALOG for status badge

> **D-03:** Manual rows mapping di sini TIDAK berubah. Online butuh derivasi 6-way TERPISAH (label + warna), karena switch ini IsPassed-/string-status-only dan tak punya cabang "Belum Mulai"/"Sedang Dikerjakan"/"Dibatalkan"/"Abandoned"/"Lulus"/"Tidak Lulus".

```razor
                                                            <td>
                                                                @{
                                                                    var statusClass = row.Status switch
                                                                    {
                                                                        "Passed" => "bg-success",
                                                                        "Valid" => "bg-primary",
                                                                        "Failed" => "bg-danger",
                                                                        "Expired" => "bg-warning text-dark",
                                                                        AssessmentConstants.AssessmentStatus.PendingGrading => "bg-warning text-dark",
                                                                        _ => "bg-secondary"
                                                                    };
                                                                }
                                                                <span class="badge @statusClass">@(string.IsNullOrEmpty(row.Status) ? "—" : row.Status)</span>
                                                            </td>
```

**For online (D-03 6-way — see §5 DeriveUserStatus for the mirror logic + research Pattern 2 for color map):**

| Label (final) | Warna (Bootstrap class) | Derivation condition (mirror DeriveUserStatus + IsPassed layer) |
|---------------|-------------------------|------------------------------------------------------------------|
| Lulus | `bg-success` (hijau) | `CompletedAt != null && IsPassed == true` |
| Tidak Lulus | `bg-danger` (merah) | `CompletedAt != null && IsPassed != true` |
| Menunggu Penilaian | `bg-warning text-dark` (kuning) | `Status == PendingGrading` (cek PERTAMA — punya CompletedAt terisi) |
| Sedang Dikerjakan | `bg-primary` (biru) | `StartedAt != null` (belum complete) |
| Dibatalkan | `bg-dark` (abu gelap) | `Status == "Cancelled"` |
| Abandoned | `bg-dark` (abu gelap) | `Status == "Abandoned"` |
| Belum Mulai | `bg-secondary` (abu) | else (Open + Upcoming jatuh ke sini — Pitfall 3) |

---

#### 5. Kolom Aksi branch (VERBATIM :338-369) — IN-FILE ANALOG + extension point Phase 367

> Current = 2 branch: Training (Edit + HTMX delete MAM-08) / else Manual (Edit + HTMX delete MAM-08). Online add a 3rd branch: HANYA "Lihat hasil" (Completed/PendingGrading); NO Edit, NO Hapus (placeholder 367). `antiToken` declared di :4 (`var antiToken = Antiforgery.GetAndStoreTokens(Context).RequestToken;`).

```razor
                                                            <td>
                                                                <div class="d-flex gap-1">
                                                                    @if (row.Type == "Training")
                                                                    {
                                                                        <a href="@Url.Action("EditTraining", "TrainingAdmin", new { id = row.Id })"
                                                                           class="btn btn-sm btn-outline-primary" title="Edit">
                                                                            <i class="bi bi-pencil"></i>
                                                                        </a>
                                                                        @* MAM-08: hx-post delete (re-swap Tab2 preserve filter); token di body. *@
                                                                        <button type="button" class="btn btn-sm btn-outline-danger" title="Hapus"
                                                                                hx-post="@Url.Action("DeleteTraining", "TrainingAdmin")"
                                                                                hx-vals='@Html.Raw($"{{\"id\": {row.Id}, \"__RequestVerificationToken\": \"{antiToken}\"}}")'
                                                                                hx-confirm="Hapus training record ini?"
                                                                                hx-swap="none">
                                                                            <i class="bi bi-trash"></i>
                                                                        </button>
                                                                    }
                                                                    else
                                                                    {
                                                                        <a href="@Url.Action("EditManualAssessment", "TrainingAdmin", new { id = row.Id })"
                                                                           class="btn btn-sm btn-outline-primary" title="Edit">
                                                                            <i class="bi bi-pencil"></i>
                                                                        </a>
                                                                        @* MAM-08: hx-post delete (re-swap Tab2 preserve filter); token di body. *@
                                                                        <button type="button" class="btn btn-sm btn-outline-danger" title="Hapus"
                                                                                hx-post="@Url.Action("DeleteManualAssessment", "TrainingAdmin")"
                                                                                hx-vals='@Html.Raw($"{{\"id\": {row.Id}, \"__RequestVerificationToken\": \"{antiToken}\"}}")'
                                                                                hx-confirm="Hapus assessment record ini?"
                                                                                hx-swap="none">
                                                                            <i class="bi bi-trash"></i>
                                                                        </button>
                                                                    }
                                                                </div>
                                                            </td>
```

**Online branch to add (D-02): "Lihat hasil" anchor, Completed/PendingGrading only.** See §"Lihat hasil link pattern" below for exact anchor shape.

---

## Shared Patterns / Analog Excerpts

### A. `DeriveUserStatus` — the 6-way derivation the inline online switch must MIRROR (VERBATIM)

**Source:** `Controllers/AssessmentAdminController.cs:2799-2812` (static, pure, testable)

> **CRITICAL urutan cek:** `PendingGrading` dicek PERTAMA — session ber-essay punya `Status == "Menunggu Penilaian"` DAN `CompletedAt != null` BERSAMAAN; cek `CompletedAt` duluan = salah-map "Completed". `Upcoming` (Pitfall 3) tidak punya cabang eksplisit → fall through ke `return "Not started"` → D-03 "Belum Mulai" (abu).

```csharp
        // MAM-04: derivasi UserStatus untuk Monitoring Detail. PendingGrading WAJIB dicek pertama —
        // session ber-essay punya Status="Menunggu Penilaian" + CompletedAt terisi BERSAMAAN,
        // jadi cek CompletedAt duluan akan salah-map "Completed". Static + pure → testable (xUnit).
        public static string DeriveUserStatus(string? status, DateTime? completedAt, DateTime? startedAt)
        {
            if (status == AssessmentConstants.AssessmentStatus.PendingGrading)
                return "Menunggu Penilaian";
            if (completedAt != null)
                return "Completed";
            if (status == "Cancelled")
                return "Dibatalkan";
            if (status == "Abandoned")
                return "Abandoned";
            if (startedAt != null)
                return "InProgress";
            return "Not started";
        }
```

**The gap this leaves for the view (D-03):** `DeriveUserStatus` returns `"Completed"` for BOTH pass and fail (no IsPassed layer) and returns RAW labels `"InProgress"`/`"Not started"` (not D-03 display labels "Sedang Dikerjakan"/"Belum Mulai") and has NO color map. View MUST add: (1) `CompletedAt != null` → split `IsPassed==true ? "Lulus" : "Tidak Lulus"`, (2) translate "InProgress"→"Sedang Dikerjakan", "Not started"→"Belum Mulai", (3) color map (§4 table). RESEARCH Pattern 2 recommends an INLINE switch mirroring the order above (PendingGrading → CompletedAt+IsPassed → Cancelled → Abandoned → StartedAt → else) rather than `@using HcPortal.Controllers` + calling the static (because the color+Lulus/Tidak layer is needed on top regardless). Planner decides (D-03 explicitly allows inline duplication OR static call).

> **Optional test hook (Wave 0 gap):** If planner extracts online derivation to a static helper (e.g. `DeriveOnlineRowLabel`), mirror the existing xUnit pattern in `HcPortal.Tests/MonitoringUserStatusTests.cs` (tests `DeriveUserStatus`, 6 branches). Inline derivation = not unit-testable → rely on UAT Playwright.

---

### B. "Lihat hasil" link pattern → `CMP/Results` (D-02)

**Route (VERBATIM signature + guards):** `Controllers/CMPController.cs:2217-2237`

```csharp
        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // REC-04 (D-01/D-09): owner || roleLevel<=3 || (L4 section-scoped, Section non-null)
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return Challenge();
            bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);
            if (!isAuthorized) return Forbid();

            // SUB-01 D-06: normalize submitted status (Completed OR PendingGrading)
            if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
            {
                TempData["Error"] = "Assessment belum selesai.";
                return RedirectToAction("Assessment");
            }
```

**Key facts for planner:**
- Accepts `int id` = `AssessmentSession.Id` = `row.Id` of the online row.
- Server-enforces authorization (`IsResultsAuthorized`, Admin/HC pass by design) — NO new IDOR (V4 access control already covered).
- Server gate `IsAssessmentSubmitted(Status)` = Completed OR PendingGrading — IDENTICAL to D-02 button visibility condition. So showing the button only for those states matches the server gate (clicking a non-submitted session would redirect anyway).

**Existing anchor analogs (choose icon/title per Claude's Discretion):**

`Views/Admin/UserAssessmentHistory.cshtml:200-204` — closest match (icon `bi-eye`, `btn-outline-info`):
```razor
                                        <a href="@Url.Action("Results", "CMP", new { id = item.Id })"
                                           class="btn btn-sm btn-outline-info"
                                           title="View results">
                                            <i class="bi bi-eye me-1"></i>View Results
                                        </a>
```

`Views/Admin/AssessmentMonitoringDetail.cshtml:299-303` — Completed-gated, `target="_blank"`:
```razor
                                            @if (session.UserStatus == "Completed")
                                            {
                                                <a href="@Url.Action("Results", "CMP", new { id = session.Id })" target="_blank"
                                                   class="btn btn-success btn-sm">View Results</a>
                                            }
```

**Recommended for Phase 371 (RESEARCH Pattern 3 — `Url.Action`, gated, icon `bi-eye`, `target="_blank"`, Indonesian title "Lihat hasil"):**
```razor
@if (row.Type == "AssessmentOnline" && /* Completed || PendingGrading */)
{
    <a href="@Url.Action("Results", "CMP", new { id = row.Id })"
       class="btn btn-sm btn-outline-info" title="Lihat hasil" target="_blank">
        <i class="bi bi-eye"></i>
    </a>
}
@* online belum-selesai: kolom Aksi kosong. NO Edit/Hapus untuk semua online (placeholder Phase 367). *@
```

---

### C. `AssessmentConstants.AssessmentStatus` (VERBATIM :13-21) — anti-drift constants

**Source:** `Models/AssessmentConstants.cs:13-21` (already used in target view at :267, :329)

```csharp
        public static class AssessmentStatus
        {
            public const string Open = "Open";
            public const string Upcoming = "Upcoming";
            public const string Completed = "Completed";
            public const string PendingGrading = "Menunggu Penilaian"; // Phase 309 D-04 — set by GradingService L199 untuk session ber-essay
            public const string InProgress = "InProgress"; // Phase 310 WR-04 — peserta sedang mengerjakan ujian
            public const string Cancelled = "Cancelled";   // Phase 310 WR-04 — session dibatalkan
        }
```

**Use `AssessmentConstants.AssessmentStatus.PendingGrading` (not literal `"Menunggu Penilaian"`)** for the PendingGrading check — consistent with existing view usage at :267 and :329. Note `Open`, `Upcoming`, `Completed`, `InProgress`, `Cancelled` available; raw DB also has `"Abandoned"` (no constant — used as literal in DeriveUserStatus :2807).

---

### D. AssessmentSession model fields (VERBATIM types, `Models/AssessmentSession.cs`)

| Field | Line | Type | Note for online projection |
|-------|------|------|----------------------------|
| `Id` | :7 | `int` | → `row.Id` → `CMP/Results?id=` |
| `Title` | :13 | `string` | `Title = a.Title` |
| `Schedule` | :18 | `DateTime` | `Date = a.CompletedAt ?? a.Schedule` (D-05 fallback) |
| `Status` | :20 | `string` | comment: `"Open", "Upcoming", "Completed"` (also InProgress/Cancelled/Abandoned/PendingGrading at runtime) |
| `Score` | :26 | `int?` | Detail = `$"{a.Score}"` only when CompletedAt+HasValue (D-05) |
| `IsPassed` | :38 | `bool?` | null until graded — DO NOT map null→"Tidak Lulus" |
| `CompletedAt` | :39 | `DateTime?` | derivation: Completed/Lulus/Tidak split |
| `StartedAt` | :40 | `DateTime?` | derivation: "Sedang Dikerjakan" |
| `ValidUntil` | :66 | **`DateOnly?`** | **MUST match training/manual `DateOnly?`** (Pitfall 2). Online usually null. Render `.Value.ToString("dd MMM yyyy")` |
| `IsManualEntry` | :131 | `bool` (default false) | filter: online = `.Where(a => !a.IsManualEntry)` (loosen existing `.Where(a => a.IsManualEntry)` at :266) |
| `LinkedGroupId` | :166 | `int?` | D-07: Pre-Post pairs render as 2 separate rows — NO grouping |

> `Status`/`StartedAt`/`CompletedAt`/`IsPassed`/`Schedule`/`Title`/`Score`/`ValidUntil` all present and typed as above — confirms RESEARCH §5 verification. No new model field needed (Migration=false).

---

## No Analog Found

None. Every pattern Phase 371 needs has a verbatim in-codebase analog (badge, status switch, aksi branch, CMP/Results link, DeriveUserStatus, constants). Phase 371 = recombination of existing building blocks in one file.

---

## Metadata

**Analog search scope:**
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (target, in-file analogs)
- `Controllers/AssessmentAdminController.cs` (DeriveUserStatus :2799)
- `Controllers/CMPController.cs` (Results route :2217-2237)
- `Models/AssessmentSession.cs`, `Models/AssessmentConstants.cs`
- `Views/Admin/UserAssessmentHistory.cshtml`, `Views/Admin/AssessmentMonitoringDetail.cshtml`, `Views/CMP/Records.cshtml` (CMP/Results link usage)

**Files scanned:** 7 source + grep across all views for `Url.Action("Results","CMP")`

**Verified facts:**
- `.Concat()` :268 needs identical anon shape across all 3 operands (training+manual already identical 9-field).
- `ValidUntil` = `DateOnly?` in both TrainingRecord projection and AssessmentSession.cs:66 — must NOT mix DateTime?.
- `DeriveUserStatus` order: PendingGrading → CompletedAt → Cancelled → Abandoned → StartedAt → else; Upcoming/Open fall to else ("Belum Mulai").
- `CMP/Results(int id)` accepts session Id, server-gated (auth + IsAssessmentSubmitted) — matches D-02 visibility condition exactly.
- `antiToken` declared at :4 — NOT used for online (no delete; Phase 367).
- Empty-state "manual" copy at :273 ONLY (do not touch level-list empty-state at :201).

**Pattern extraction date:** 2026-06-11
