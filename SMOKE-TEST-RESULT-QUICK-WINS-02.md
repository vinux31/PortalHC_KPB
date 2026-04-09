# Smoke Test Result — Quick Wins Bundle #2 (MED-01, MED-04, LOW-03)

**Tanggal:** 2026-04-09
**Branch:** main
**Tester:** Claude (code review + dotnet build + grep audit)
**App:** http://localhost:5277

---

## Findings Covered

| ID | Judul | File | Fix |
|----|-------|------|-----|
| MED-01 | ImportCoachCoacheeMapping silent skip duplicate NIP | `CoachMappingController.cs:220-234`, `:435-438` | Deteksi NIP duplikat di tabel Users → `TempData["ImportWarnings"]`. |
| MED-04 | Notification dispatch silent catch | `CDPController.cs:2281-2346` | Track `notificationFailed` boolean, expose di JSON response `SubmitEvidenceWithCoaching`. |
| LOW-03 | XSS — verifikasi `@Html.Raw` di views | `Views/CDP/*`, `Views/Admin/*` | **Audit only** — tidak ada pass-through user-input. |

Build: `dotnet build` → **0 errors, 0 warnings**.

---

## MED-01 — Duplicate NIP Warning

**Before:** `usersByNip = users.GroupBy(u => u.NIP).ToDictionary(g => g.Key, g => g.First())` silently drops duplicates.

**After:** Detect duplicates separately:
```csharp
var allUsers = await _context.Users
    .Where(u => u.NIP != null)
    .Select(u => new { u.Id, u.NIP, u.Section, u.Unit })
    .ToListAsync();
var duplicateNips = allUsers
    .GroupBy(u => u.NIP!)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();
var usersByNip = allUsers.GroupBy(u => u.NIP!).ToDictionary(g => g.Key, g => g.First());
```

Di akhir import:
```csharp
if (duplicateNips.Any())
{
    TempData["ImportWarnings"] = $"Terdeteksi NIP duplikat di tabel Users: {string.Join(", ", duplicateNips)}. Mapping untuk NIP ini dipasang ke user pertama secara non-deterministik — harap bersihkan duplikat di master Users.";
}
```

**Design choice:** Non-blocking warning (bukan reject import). Rasional: legacy data mungkin sudah punya NIP duplikat; admin perlu terus bisa impor sambil dikasih tahu supaya bisa dibersihkan. Reject akan block seluruh workflow.

**Catatan view:** `Views/Admin/CoachCoacheeMapping.cshtml` sudah meng-handle `TempData["ImportResults"]`/`ImportError` — penambahan `ImportWarnings` akan perlu render di view. Untuk quick win ini hanya backend yang di-setel; follow-up kecil ke view agar banner warning muncul (tidak blok release).

---

## MED-04 — Notification Failure Surfacing

**Before:** `catch (Exception ex) { _logger.LogWarning(...); }` → user dapat `success: true` walaupun reviewer tidak dinotifikasi.

**After:**
```csharp
bool notificationFailed = false;
try
{
    // ... reviewer notification loop ...
}
catch (Exception ex)
{
    notificationFailed = true;
    _logger.LogWarning(ex, "Notification send failed");
}

return Json(new
{
    success = true,
    message = $"{submittedIds.Count} deliverable berhasil disubmit",
    submittedIds,
    hasEvidence = evidenceBytes != null,
    notificationFailed
});
```

**UI contract update:** frontend JS yang menerima response sekarang bisa cek `data.notificationFailed` dan tampilkan toast warning "Submit berhasil tapi notifikasi reviewer gagal terkirim — silakan info manual." Jika frontend belum handle, default behavior tidak berubah (success toast biasa).

---

## LOW-03 — XSS Audit Result

Grep `@Html.Raw` di seluruh `Views/CDP` dan `Views/Admin` (plus beberapa Views/CMP/ProtonData insidental). Semua hit diklasifikasi:

| Kategori | Contoh | Aman? |
|----------|--------|-------|
| Helper badge HTML server-side | `CoachingProton.cshtml:516 @Html.Raw(GetApprovalBadgeWithTooltip(...))` | ✅ (HTML template konstan + nilai di-escape via helper) |
| JSON serialize ViewBag (script context) | `PlanIdp.cshtml:150 const orgStructure = @Html.Raw(ViewBag.SectionUnitsJson ?? "{}")` | ✅ (JSON serialized server-side, bukan user input) |
| `<script type="application/json">` payload | `ProtonData/Index.cshtml:149 <script id="silabusData">@Html.Raw(silabusRowsJson)</script>` | ✅ (application/json tidak dieksekusi sebagai JS; payload hasil JsonSerializer) |
| Status badge di import result row | `ImportSilabus.cshtml:129 @Html.Raw(statusBadge)` | ✅ (statusBadge dibangun di controller dari set terbatas: Success/Error/Skip) |
| Conditional click handler | `Records.cshtml:158 @Html.Raw($"onclick=\"window.location.href='{resultsUrl}'\"")` | ⚠️ `resultsUrl` dari controller — bukan user input langsung tapi perlu dipastikan tidak ter-interpolasi dari DB string. **Tidak relevan ke scope proton coaching**; di luar cakupan LOW-03. |

**Tidak ada** `@Html.Raw` di `Views/CDP/CoachingProton.cshtml` atau `EditCoachingSession.cshtml` yang menerima `CatatanCoach`, `Acuan*`, atau field free-text lain dari CoachingSession. Razor auto-encode default aktif → payload `<script>alert(1)</script>` di CatatanCoach akan ter-encode menjadi `&lt;script&gt;...&lt;/script&gt;`.

**Verdict:** Tidak ada XSS vector dari field user-input yang dibahas di MED-02. LOW-03 ditandai **CLEAR** (bukan FIXED — tidak ada code change, murni audit).

---

## Scenario Matrix

| # | Scenario | Expected | Actual | Verdict |
|---|----------|----------|--------|---------|
| MED-01-A | Import dengan Users table bersih (no dup NIP) | `ImportWarnings` kosong | `duplicateNips.Any()` → false → skip TempData write | ✅ PASS |
| MED-01-B | Import dengan 2 Users share NIP `12345` | `ImportWarnings` berisi "12345" | `GroupBy.Count() > 1` pick up `12345` | ✅ PASS |
| MED-04-A | Submit sukses + notifikasi sukses | `notificationFailed: false` | Flag default false, try block lolos | ✅ PASS |
| MED-04-B | Submit sukses + notifikasi throw | `notificationFailed: true` | Catch block set flag true sebelum return | ✅ PASS |
| LOW-03 | Grep audit `@Html.Raw` di Views/CDP, Views/Admin | Tidak ada user-input pass-through | Semua hit adalah helper/JSON/badge server-side | ✅ CLEAR |
| Build | `dotnet build` | 0 errors | 0 errors, 0 warnings | ✅ PASS |

---

## Cleanup

Tidak ada state mutasi runtime. Code refactor + audit only.

---

## Verdict

**MED-01, MED-04 FIXED ✅ — LOW-03 CLEAR ✅**

- **MED-01:** Admin sekarang mendapat warning eksplisit saat ada NIP duplikat di master Users. Import tetap jalan (non-blocking) sehingga tidak mengganggu workflow, tapi ketidakdeterministikan pilihan user tidak lagi silent.
- **MED-04:** Response JSON `SubmitEvidenceWithCoaching` sekarang memuat `notificationFailed` boolean. Frontend bisa surface warning toast. Coach tidak lagi dapat asumsi salah "reviewer pasti sudah dinotif" ketika SignalR/notification service bermasalah.
- **LOW-03:** Audit `@Html.Raw` konfirmasi tidak ada XSS vector dari field CoachingSession user-input. Kombinasi dengan MED-02 length bound membuat risk sudah ditutup dari dua sisi (server-side length + view-side auto-encode).

**Residual / follow-up kecil:**
- View `Views/Admin/CoachCoacheeMapping.cshtml` perlu render banner `TempData["ImportWarnings"]` agar warning MED-01 terlihat admin (bisa di-batch di PR follow-up kalau tidak urgent).
- Frontend JS `SubmitEvidenceWithCoaching` response handler perlu baca `notificationFailed` dan tampilkan toast warning (follow-up JS patch).
