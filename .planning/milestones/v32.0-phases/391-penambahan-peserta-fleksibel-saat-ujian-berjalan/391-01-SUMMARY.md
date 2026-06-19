---
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
plan: 01
status: complete
requirements: [PART-01, PART-02, PART-03]
migration: false
date: 2026-06-17
---

# Plan 391-01 Summary — EditAssessment Flexible-Add Logic

**Status:** ✅ Complete · 3/3 tasks · 0 migration · 0 view change · branch `main`

## What was built

5 titik bedah di `Controllers/AssessmentAdminController.cs` (EditAssessment POST + Pre-Post branch) mengimplementasikan D-01..D-05:

| Decision | Change | Commit |
|----------|--------|--------|
| D-01 | Helper `DeriveReadyStatus` + BULK ASSIGN set Status siap-mulai (bukan inherit) | `d87381a1` |
| D-02 | Guard `Completed` hanya blokir EDIT murni (`!hasAddition`) | `7273f37e` |
| D-03 | Edit-loop standar skip sesi berjalan (`StartedAt!=null && CompletedAt==null`) | `7273f37e` |
| D-04 | `TempData["Warning"]` → `TempData["Info"]` (wording menenangkan) | `746fed94` |
| D-05 | Pre-Post per-phase loop (pre+post) skip sesi berjalan | `746fed94` |

## Key decisions / details (read by Plan 02)

- **Helper `DeriveReadyStatus`** — `private static string` di kelas `AssessmentAdminController`, diletakkan tepat SETELAH method `EditAssessment` POST (sebelum `// --- DELETE ASSESSMENT ---`). Signature: `DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)`. Logika **byte-identik untuk replikasi di test**:
  ```csharp
  var nowWib = DateTime.UtcNow.AddHours(7);
  if (schedule <= nowWib) return AssessmentConstants.AssessmentStatus.Open;
  return AssessmentConstants.AssessmentStatus.Upcoming;
  ```
  WIB = `DateTime.UtcNow.AddHours(7)` (mirror StartExam L915; BUKAN DateTime.Now/UTC polos — regresi d844c552). Param `examWindowCloseDate` saat ini tidak dipakai di body (disediakan untuk ekstensi; status hanya bergantung Schedule vs now) — Plan 02 replikasi signature + body apa adanya.
- **Fallback `ExamWindowCloseDate == null` (Open Question A1):** LONGGAR — null = boleh tambah. Guard D-02 hanya cek `!hasAddition`; tidak ada penolakan berbasis window di guard. (Kunci di test fact (d).)
- **Pre-Post (D-05): DIUBAH (bukan sekadar diverifikasi).** Per-phase loop preGroup (L~1849) & postGroup (L~1858) meng-overwrite `Schedule`/`DurationMinutes`/`ExamWindowCloseDate` SEMUA sesi grup termasuk yang berjalan → ditambah filter `if (s.StartedAt != null && s.CompletedAt == null) continue;` (konsisten D-03). Shared-field loop L1832 hanya set field non-volatil (Title/Category/PassPercentage/Review/Shuffle/Token) → dibiarkan. Sesi baru Pre/Post sudah `Status="Upcoming"` (L1940/1961) → sudah sesuai D-01, tidak diubah. Branch Pre-Post `return` sebelum guard Completed → D-02 sudah aman, tidak diubah.

## Verification

- `dotnet build HcPortal.csproj` → **0 error** (tiap task).
- `dotnet test --filter "Category!=Integration"` → **347/347 green** (setelah Task 2 & Task 3).
- Grep acceptance semua task PASS (helper 1×, WIB 1×, new-status line 1×, old inherit 0×, hasAddition+guard 1×, skip-line standar 1×, Pre-Post skip 2×, Info wording 1×, old Warning wording 0×).
- `_Layout.cshtml` render `TempData["Info"]` (2×) → **0 perubahan view** dikonfirmasi.
- **Migration = FALSE** — hanya `Controllers/AssessmentAdminController.cs` tersentuh; tidak ada `Migrations/`.

## key-files
- created: (none)
- modified: `Controllers/AssessmentAdminController.cs` (helper DeriveReadyStatus + 5 titik bedah)

## Self-Check: PASSED
