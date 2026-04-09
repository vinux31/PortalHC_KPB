# Smoke Test Result — Quick Wins Bundle (MED-02, MED-03, LOW-02)

**Tanggal:** 2026-04-09
**Branch:** main
**Tester:** Claude (code review + dotnet build)
**App:** http://localhost:5277

**Scope:** Tiga finding medium/low yang digrup dalam satu commit karena scope-nya kecil dan terkait input-validation / role-authorization.

---

## Findings Covered

| ID | Judul | File | Fix |
|----|-------|------|-----|
| MED-02 | Acuan & CatatanCoach tanpa max length | `Controllers/CDPController.cs:2169-2185` (SubmitEvidenceWithCoaching), `:2352-2358` (EditCoachingSession) | Validasi panjang server-side: CatatanCoach ≤ 4000, Acuan* ≤ 2000, Kesimpulan/Result ≤ 100. |
| MED-03 | `Date` parameter CoachingSession tanpa bound | `Controllers/CDPController.cs:2169-2172` | `today.AddYears(-2) ≤ date ≤ today.AddDays(1)`; reject di luar range. |
| LOW-02 | ExportProgressPdf tidak mengizinkan Coach | `Controllers/CDPController.cs:2564` | Sudah ditutup sebelumnya oleh fix HIGH-07 (`[Authorize(Roles = "Coach, Sr Supervisor, Section Head, HC, Admin")]`). |

Build: `dotnet build` → **0 errors**, 80 warnings (semua pre-existing CA1416 di `LdapAuthService.cs`, tidak terkait perubahan ini).

---

## Fix Detail

### MED-03 — Date bound

```csharp
// CDPController.cs:2169-2172 (SubmitEvidenceWithCoaching, setelah status validation)
var today = DateTime.Today;
if (date.Date > today.AddDays(1) || date.Date < today.AddYears(-2))
    return Json(new { success = false, message = "Tanggal coaching tidak valid (maksimal H+1 dari hari ini, minimal 2 tahun ke belakang)." });
```

**Range rationale:**
- Upper bound `today.AddDays(1)` → toleransi zona waktu (client mengirim besok lokal masih OK).
- Lower bound `today.AddYears(-2)` → cakup satu siklus appraisal penuh tanpa membuka pintu salah-ketik tahun (mis. `1900`, `2002`).

Rekomendasi asli `date >= mappingStartDate` di-downgrade: lookup mapping start date per progress menambah query tanpa nilai tambah signifikan karena business flow tidak pernah melampaui 2 tahun ke belakang. Bound 2 tahun cukup konservatif.

### MED-02 — String length bounds

```csharp
// CDPController.cs:2174-2185 (SubmitEvidenceWithCoaching)
const int MaxCatatan = 4000;
const int MaxAcuan = 2000;
const int MaxShort = 100;
if ((catatanCoach?.Length ?? 0) > MaxCatatan)
    return Json(new { success = false, message = $"Catatan Coach melebihi {MaxCatatan} karakter." });
if ((kesimpulan?.Length ?? 0) > MaxShort || (result?.Length ?? 0) > MaxShort)
    return Json(new { success = false, message = $"Kesimpulan/Result melebihi {MaxShort} karakter." });
if ((acuanPedoman?.Length ?? 0) > MaxAcuan || (acuanTko?.Length ?? 0) > MaxAcuan ||
    (acuanBestPractice?.Length ?? 0) > MaxAcuan || (acuanDokumen?.Length ?? 0) > MaxAcuan)
    return Json(new { success = false, message = $"Field Acuan melebihi {MaxAcuan} karakter." });
```

```csharp
// CDPController.cs:2352-2358 (EditCoachingSession)
if ((catatanCoach?.Length ?? 0) > 4000 || (kesimpulan?.Length ?? 0) > 100 || (result?.Length ?? 0) > 100)
{
    TempData["Error"] = "Input melebihi batas panjang (Catatan 4000, Kesimpulan/Result 100).";
    return RedirectToAction("Deliverable", new { id = session.ProtonDeliverableProgressId });
}
```

**Catatan arsitektur:** Tidak menambahkan `[StringLength]` ke `Models/CoachingSession.cs` untuk menghindari generate migration EF Core (kolom existing `nvarchar(max)`). Enforcement sepenuhnya controller-side → cepat + zero-risk DB change. Jika di masa depan migration dilakukan, attribute model bisa ditambah tanpa mengubah kontrol.

### LOW-02 — ExportProgressPdf Coach role

Verifikasi cepat di `Controllers/CDPController.cs:2564`:
```csharp
[Authorize(Roles = "Coach, Sr Supervisor, Section Head, HC, Admin")]
public async Task<IActionResult> ExportProgressPdf(string coacheeId)
```
Sudah include `Coach`. Ini ditutup incidentally oleh commit HIGH-07. Tidak ada perubahan kode di sesi ini untuk LOW-02 — hanya update status di bug report.

---

## Scenario Matrix

| # | Scenario | Expected | Actual (code review) | Verdict |
|---|----------|----------|---------------------|---------|
| A | MED-03: submit dengan `date = 2099-01-01` | Reject | `date > today.AddDays(1)` → return error JSON | ✅ PASS |
| B | MED-03: submit dengan `date = 1900-01-01` | Reject | `date < today.AddYears(-2)` → return error JSON | ✅ PASS |
| C | MED-03: submit dengan `date = today` | Accept | Range check lolos | ✅ PASS |
| D | MED-03: submit dengan `date = today.AddDays(1)` | Accept (toleransi TZ) | Upper bound inclusive | ✅ PASS |
| E | MED-02: submit dengan `catatanCoach` 5000 char | Reject | `length > 4000` → error JSON | ✅ PASS |
| F | MED-02: submit dengan `acuanPedoman` 2001 char | Reject | `length > 2000` → error JSON | ✅ PASS |
| G | MED-02: submit dengan `kesimpulan = "Kompeten"` (8 char) | Accept | `length ≤ 100` | ✅ PASS |
| H | MED-02: EditCoachingSession dengan `catatanCoach` 4001 char | Reject (TempData Error + redirect) | Guard di line 2352-2358 | ✅ PASS |
| I | LOW-02: Coach hit `ExportProgressPdf?coacheeId=X` | 200 / 204 (bukan 403) | Coach ada di Authorize list | ✅ PASS (via HIGH-07) |

---

## Cleanup

Tidak ada state mutasi runtime (verifikasi via code review + build). Tidak perlu cleanup DB/filesystem.

---

## Verdict

**MED-02, MED-03, LOW-02 FIXED ✅**

- **MED-02:** Server-side length guard di dua entry point (bulk submit + edit). DB bloat via mega-payload tertutup. Jika di masa depan `[StringLength]` attribute ditambah ke model, enforcement menjadi berlapis.
- **MED-03:** Date di-bound 2 tahun ke belakang s/d H+1. Typo tahun (`1900`, `2099`) tidak lagi masuk ke audit/export.
- **LOW-02:** Sudah ditutup oleh fix HIGH-07; status bug report di-update.

**Residual risk:** MED-02 bound hanya di controller — jika ada endpoint baru yang menulis ke `CoachingSession.CatatanCoach/Acuan*` tanpa melewati validasi ini, bound tidak berlaku. Mitigasi: audit grep `CatatanCoach =` / `Acuan*` saat code review PR baru.
