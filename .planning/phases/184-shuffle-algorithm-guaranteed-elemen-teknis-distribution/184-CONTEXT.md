# Phase 184: Shuffle Algorithm — Guaranteed Elemen Teknis Distribution - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Modify the cross-package shuffle algorithm to guarantee at least one question per Elemen Teknis group in the shuffled result. Reshuffles must preserve this guarantee. Fix spider web (radar chart) to always render when ET score data exists. Add ET coverage visibility on ManagePackages page and upload warning when distribution is incomplete.

Single-package shuffle does NOT need changes — all questions are included (only order is shuffled), so ET coverage is inherently guaranteed if the package has questions per group.

</domain>

<decisions>
## Implementation Decisions

### NULL ElemenTeknis Handling
- Soal tanpa ElemenTeknis (NULL) tetap masuk ujian tapi **tidak ikut logika distribusi** — hanya soal ber-tag yang dijamin
- Kalau **semua** soal di paket tidak punya ElemenTeknis, fallback ke shuffle biasa tanpa error (algoritma lama tetap jalan)

### Insufficient Questions Edge Case
- Kalau jumlah soal ber-tag lebih sedikit dari jumlah grup ET: **best-effort** — ambil 1 soal per grup sebanyak mungkin, sisanya acak. Tidak block ujian
- **Warning saat upload Excel** kalau distribusi ET tidak lengkap — upload tetap berhasil (warning saja, tidak ditolak)

### Algorithm Priority (Cross-Package Only)
- **ElemenTeknis coverage diprioritaskan** di atas distribusi rata antar paket — pastikan setiap grup ET terwakili dulu, baru sisa kuota dibagi rata antar paket
- Single-package: tidak perlu perubahan logika — semua soal sudah masuk, hanya urutan yang diacak

### Spider Web / Radar Chart Fix
- Bug saat ini: kadang data ET ada tapi chart/tabel tidak muncul — perlu investigasi di Results controller (kemungkinan legacy path tidak menghitung ElemenTeknisScores)
- Kalau ≥3 grup ET: tampilkan radar chart + tabel
- Kalau <3 grup ET: tampilkan tabel saja (radar chart butuh minimal 3 titik)

### ET Coverage Table di ManagePackages
- Tambah section **tabel gabungan cross-package** di bawah summary panel (setelah card Mode/Status, sebelum form Create Package)
- Format: baris = grup ET, kolom = per paket + total
- Soal tanpa ET ditampilkan sebagai baris "(Tanpa ET)"
- Warning per paket yang missing grup ET tertentu

### Claude's Discretion
- Algoritma detail untuk menjamin ET coverage sambil tetap mendistribusikan sisa kuota antar paket
- Cara implementasi warning di upload Excel (TempData, inline validation, dll)
- Styling tabel ET coverage di ManagePackages
- Fix spesifik untuk bug spider web di legacy path

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Shuffle Algorithm
- `Controllers/CMPController.cs` L1686-1743 — `BuildCrossPackageAssignment()` — current cross-package shuffle (slot-list algorithm, no ET awareness)
- `Controllers/CMPController.cs` L1670-1677 — `Shuffle<T>()` Fisher-Yates helper
- `Controllers/CMPController.cs` L1440-1466 — Exam start: builds ShuffledQuestionIds and option shuffle

### Reshuffle
- `Controllers/AdminController.cs` L2783-2855 — `ReshufflePackage()` — single worker reshuffle (calls BuildCrossPackageAssignment)
- `Controllers/AdminController.cs` L2864-2954 — `ReshuffleAll()` — bulk reshuffle

### ElemenTeknis Scoring (Spider Web)
- `Controllers/CMPController.cs` L2408-2437 — ElemenTeknis scoring logic in Results action
- `Controllers/CMPController.cs` L2460+ — Legacy Results path (may NOT compute ElemenTeknisScores — investigate)
- `Views/CMP/Results.cshtml` L107-148 — Radar chart + table rendering (≥3 check at L114)
- `Models/AssessmentResultsViewModel.cs` L19,39-44 — ElemenTeknisScore model

### Data Model
- `Models/AssessmentPackage.cs` L44 — `PackageQuestion.ElemenTeknis` nullable string field

### ManagePackages Page
- `Controllers/AdminController.cs` L5522-5548 — `ManagePackages()` GET action
- `Views/Admin/ManagePackages.cshtml` — Current page (summary panel L42-73, package list L100-155)

### Requirements
- `.planning/REQUIREMENTS.md` — SHUF-01, SHUF-02, SHUF-03

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Shuffle<T>()` (CMPController:1670) — Fisher-Yates helper, reusable for new algorithm
- `BuildCrossPackageAssignment()` (CMPController:1686) — needs modification, not replacement
- Summary panel pattern in ManagePackages.cshtml (L42-73) — extend for ET coverage table

### Established Patterns
- Cross-package assignment uses slot-list algorithm with even package distribution
- ElemenTeknis scoring already groups by `q.ElemenTeknis` with NULL → "Lainnya" fallback
- Upload Excel uses TempData for success/error messages
- ManagePackages controller already loads packages with `.Include(p => p.Questions)`

### Integration Points
- `BuildCrossPackageAssignment()` is called from 3 places: exam start (CMPController:1441), ReshufflePackage (AdminController:2828), ReshuffleAll (AdminController:2920s) — all must use updated algorithm
- ManagePackages GET action needs to compute ET coverage data and pass via ViewBag
- Upload Excel action (ImportPackageQuestions) needs ET distribution validation + warning

</code_context>

<specifics>
## Specific Ideas

- Tabel ET coverage di ManagePackages: format baris=grup ET, kolom=per paket, dengan warning highlight per paket yang missing grup
- Warning saat upload Excel: informasikan HC kalau ada grup ET yang tidak punya cukup soal, tapi tetap izinkan upload

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 184-shuffle-algorithm-guaranteed-elemen-teknis-distribution*
*Context gathered: 2026-03-17*
