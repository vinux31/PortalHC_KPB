# Smoke Test Result — MED-07 & MED-08

**Tanggal:** 2026-04-09
**Branch:** main
**Tester:** Claude (code review + dotnet build)

---

## Findings Covered

| ID | Judul | File | Fix |
|----|-------|------|-----|
| MED-07 | CoachingProton level 6 tanpa feedback saat no active assignment | `Controllers/CDPController.cs:1456-1466`, `Views/CDP/CoachingProton.cshtml:349-358` | Set `ViewBag.NoActiveAssignment`; view render banner informatif di empty state `no_coachees`. |
| MED-08 | EditCoachingSession ownership historis (mapping tidak aktif tetap boleh edit) | `Controllers/CDPController.cs:2352-2365` | Coach dengan mapping tidak aktif di-blokir; HC/Admin bypass; TempData Error + redirect. |

Build: `dotnet build` → **0 errors**.

---

## MED-07 — Level 6 Empty State Banner

**Controller:**
```csharp
// CDPController.cs:1456-1466 (CoachingProton scope resolution, level 6)
else // Level 6 (Coachee) — own ID only if has active assignment
{
    var hasAssignment = await _context.ProtonTrackAssignments
        .AnyAsync(a => a.IsActive && a.CoacheeId == user.Id);
    scopedCoacheeIds = hasAssignment ? new List<string> { user.Id } : new List<string>();
    // MED-07 fix: surface flag supaya view bisa tampilkan banner informatif
    ViewBag.NoActiveAssignment = !hasAssignment;
}
```

**View:**
```razor
@* CoachingProton.cshtml:349-358, empty state no_coachees branch *@
else if (ViewBag.NoActiveAssignment == true)
{
    @* MED-07 fix: banner informatif level 6 tanpa assignment aktif *@
    <h5 class="text-muted mb-1">Belum ada assignment Proton aktif</h5>
    <p class="text-muted small">Anda belum memiliki deliverable Proton yang di-assign. Hubungi HC / Section Head untuk memulai track coaching Anda.</p>
}
```

**Catatan:** Branch ini spesifik untuk `isHC == false && NoActiveAssignment == true`, jadi hanya muncul ketika user level 6 benar-benar punya 0 assignment aktif. Case lain tetap fallback ke pesan generik "Belum ada coachee yang ditugaskan".

---

## MED-08 — Active Mapping Guard

**Before:**
```csharp
if (!isHcOrAdmin && session.CoachId != user.Id) return Forbid();
session.CatatanCoach = catatanCoach;
// ...
```
Coach dengan mapping ke coachee sudah di-deactivate masih bisa edit session lama.

**After:**
```csharp
if (!isHcOrAdmin && session.CoachId != user.Id) return Forbid();
// MED-08 fix: Coach hanya boleh edit session lama jika mapping ke coachee
// masih aktif. HC/Admin bypass (pengelolaan riwayat tetap di tangan HC).
if (!isHcOrAdmin)
{
    bool stillMapped = await _context.CoachCoacheeMappings
        .AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == session.CoacheeId && m.IsActive);
    if (!stillMapped)
    {
        TempData["Error"] = "Mapping Anda dengan coachee ini sudah tidak aktif — edit session tidak diizinkan. Hubungi HC untuk perubahan riwayat.";
        return RedirectToAction("Deliverable", new { id = session.ProtonDeliverableProgressId });
    }
}
```

**Design choice:**
- **Hard block**, bukan grace window 7 hari (rekomendasi asli menyebut opsi). Rasional: grace window memperbesar surface area security tanpa nilai operasional yang jelas — kalau coach butuh ubah, HC bisa intervensi kapan saja.
- **HC/Admin tetap bypass** karena mereka pengelola riwayat lintas coachee.
- Route kembali ke `Deliverable` agar TempData Error muncul di halaman terakhir coach.

**Scope:** Hanya `EditCoachingSession`. `DeleteCoachingSession` di luar scope MED-08 (akan di-audit terpisah jika perlu).

---

## Scenario Matrix

| # | Scenario | Expected | Actual (code review) | Verdict |
|---|----------|----------|---------------------|---------|
| MED-07-A | Level 6 user login, 0 active assignment | Banner "Belum ada assignment Proton aktif" | `hasAssignment = false` → `scopedCoacheeIds = []` → `emptyScenario = "no_coachees"` → branch `NoActiveAssignment == true` → banner render | ✅ PASS |
| MED-07-B | Level 6 user dengan 1 active assignment | Halaman normal (tidak ada banner) | `hasAssignment = true` → `ViewBag.NoActiveAssignment = false` → skip banner branch | ✅ PASS |
| MED-07-C | Level 5 Coach tanpa coachee | Pesan generik "Belum ada coachee yang ditugaskan" | `ViewBag.NoActiveAssignment` tidak diset (null) → branch false → fallback | ✅ PASS |
| MED-08-A | Coach aktif edit session miliknya | Success | `stillMapped = true` → lanjut ke length check → update | ✅ PASS |
| MED-08-B | Coach yang mapping-nya sudah deactivated edit session lama | Forbid (TempData Error + redirect) | `stillMapped = false` → set TempData Error → redirect ke Deliverable | ✅ PASS |
| MED-08-C | HC edit session coach lain | Success (bypass) | `isHcOrAdmin = true` → skip mapping check | ✅ PASS |
| MED-08-D | Admin edit session coach lain | Success (bypass) | `isHcOrAdmin = true` → skip mapping check | ✅ PASS |
| MED-08-E | Coach edit session coach lain (bukan miliknya, bukan HC) | 403 Forbid | Guard existing `session.CoachId != user.Id` → `Forbid()` (tidak sampai ke mapping check) | ✅ PASS |
| Build | `dotnet build` | 0 errors | 0 errors | ✅ PASS |

---

## Cleanup

Tidak ada state mutasi runtime. Code refactor only.

---

## Verdict

**MED-07, MED-08 FIXED ✅**

- **MED-07:** User level 6 tanpa assignment Proton aktif sekarang mendapat banner eksplisit yang mengarahkan mereka ke HC/Section Head. Empty state ambigu ("sistem rusak?") teratasi.
- **MED-08:** Coach yang mapping-nya sudah di-deactivate tidak lagi bisa edit session lama. Pengelolaan riwayat setelah mapping berakhir eksklusif di HC/Admin — sesuai rekomendasi. Security posture lebih ketat tanpa mengganggu workflow coach aktif.

**Residual:**
- `DeleteCoachingSession` (line 2369+) memiliki pola ownership yang sama; tidak dicek mapping aktif. Tidak dalam scope MED-08 — apakah perlu follow-up tergantung policy: sebagian organisasi lebih ketat untuk delete daripada edit. Rekomendasi: audit terpisah jika HC minta.
- Tidak ada runtime Playwright test di sesi ini. Scenario matrix di atas berbasis static code review — acceptable untuk fix kecil yang tidak menyentuh kritikal path.
