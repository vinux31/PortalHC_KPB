# Smoke Test Result — HIGH-07 ExportProgress Scope & Authorize Consistency

**Tanggal:** 2026-04-09
**Branch:** main (after fix HIGH-07)
**Tester:** Claude (Playwright + sqlcmd)
**App:** http://localhost:5277
**DB:** `localhost\SQLEXPRESS` / `HcPortalDB_Dev`

**Scope:** Verifikasi `ExportProgressExcel` dan `ExportProgressPdf` sekarang (1) konsisten mengizinkan role `Coach` (PDF dulu tidak include Coach), (2) scope-check berbasis `CoachCoacheeMapping` aktif (bukan `coacheeUser.Section`), (3) untuk Coach → harus punya mapping aktif; untuk SectionHead/SrSpv → harus ada mapping aktif dengan `AssignmentSection` matching user.Section.

---

## Fix Summary

| File | Change |
|------|--------|
| `Controllers/CDPController.cs` — helper baru `CanExportCoacheeProgressAsync(ApplicationUser user, string coacheeId)` | Full access (level ≤ 3) bypass. Coach (level 5) → cek `CoachCoacheeMappings.AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == coacheeId && m.IsActive)`. Section-scoped (level 4: SectionHead, SrSpv) → cek `m.CoacheeId == coacheeId && m.IsActive && m.AssignmentSection == user.Section`. |
| `ExportProgressExcel` | Scope check lama `coacheeUser.Section != user.Section` diganti `if (!await CanExportCoacheeProgressAsync(user, coacheeId)) return Forbid();`. Authorize list tidak berubah (sudah include Coach). |
| `ExportProgressPdf` | Authorize list diperluas dari `"Sr Supervisor, Section Head, HC, Admin"` → `"Coach, Sr Supervisor, Section Head, HC, Admin"`. Scope check sama seperti Excel: `CanExportCoacheeProgressAsync`. |
| `BUG-HUNT-REPORT-PROTON-COACHING.md` | HIGH-07 ditandai ✅ FIXED 2026-04-09 |

Build: `dotnet build` → **0 errors, 0 warnings**.

**Catatan scope:** Title HIGH-07 menyebut "N+1" tetapi description tidak mendetail item tsb. Inspeksi kode Export menunjukkan satu query `ToListAsync()` untuk progresses + satu `ToDictionaryAsync()` untuk coaching sessions (single batch query dengan `GroupBy`) — bukan N+1 nyata. Fix fokus ke scoping/authorization yang jelas dideskripsikan.

---

## Setup

- Login: Coach Rustam Santiko (`rustam.nugroho@pertamina.com` / `123456`)
- Mapping aktif Rustam: 1 entry → Rino (`4a624dbc-3241-4207-92d7-d1d5784c7137`, GAST / Alkylation Unit 065)
- Iwan (`66227777-1974-43ca-8bdd-e5586fa4a5b8`) tidak punya mapping aktif dengan Rustam (mapping pre-test sudah dibersihkan di cleanup HIGH-04)
- Request dikirim via Playwright `page.evaluate` → `fetch(url)` dengan session cookie Rustam

---

## Scenario Matrix

| # | Scenario | URL | Expected | Actual | Verdict |
|---|----------|-----|----------|--------|---------|
| A | Coach export Excel coachee **mapped** | `/CDP/ExportProgressExcel?coacheeId={Rino}` | 200 + xlsx | 200 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | ✅ PASS |
| B | Coach export PDF coachee **mapped** | `/CDP/ExportProgressPdf?coacheeId={Rino}` | 200/204 (bukan 403/AccessDenied) | 204 (empty body — pre-existing QuestPDF behavior karena Rino punya 0 `ProtonDeliverableProgress` rows; endpoint reached, no auth/scope rejection) | ✅ PASS (scoping OK, 204 unrelated pre-existing) |
| C | Coach export Excel coachee **tidak mapped** | `/CDP/ExportProgressExcel?coacheeId={Iwan}` | 403 / AccessDenied redirect | Redirect ke `/Account/AccessDenied?ReturnUrl=...ExportProgressExcel%3FcoacheeId%3D{Iwan}` | ✅ PASS |
| D | Coach export PDF coachee **tidak mapped** | `/CDP/ExportProgressPdf?coacheeId={Iwan}` | 403 / AccessDenied redirect | Redirect ke `/Account/AccessDenied?ReturnUrl=...ExportProgressPdf%3FcoacheeId%3D{Iwan}` | ✅ PASS |

**Penjelasan sebelum-sesudah:**

- **Sebelum fix:**
  - Scenario B akan 403 dari `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]` — Coach tidak di daftar role → framework reject sebelum masuk method. PDF endpoint inaccessible untuk Coach.
  - Scenario C mungkin lolos (false positive). Iwan section-nya ada di data lama, jika match section Rustam pribadi (`user.Section`) maka `coacheeUser.Section != user.Section` false → allow — memungkinkan Coach export coachee yang bahkan tidak dimapping ke dia.

- **Sesudah fix:**
  - Scenario B: Coach masuk Authorize list → helper `CanExportCoacheeProgressAsync` dipanggil → Coach path cek mapping → Rino mapped → allow → method tereksekusi.
  - Scenario C/D: Coach path cek mapping → Iwan tidak mapped → return false → `Forbid()` → framework redirect ke AccessDenied.

---

## Scenario E — SectionScoped via AssignmentSection (code review)

Tidak dijalankan runtime (butuh seeding SectionHead user + mapping). Code review:
- Jika user adalah SectionHead (level 4) dengan `Section = "GAST"`, dan coachee target punya mapping aktif dengan `AssignmentSection = "GAST"`, helper akan return `true`.
- Jika SectionHead memiliki `Section = "DHT-HMU"` tapi coachee di-mapping ke `AssignmentSection = "GAST"`, helper return `false` → 403, meskipun `coacheeUser.Section` (section pribadi coachee, bukan assignment) mungkin match — karena yang penting adalah assignment context, bukan organizational membership coachee.
- Ini konsisten dengan scoping `CoachingProton` (level ≤3/4 memakai `AssignmentSection` untuk filter daftar coachee).

**Result: ✅ PASS by code review**

---

## Cleanup

Tidak ada state mutasi — hanya GET requests. Tidak perlu cleanup DB.

---

## Verdict

**HIGH-07 FIXED ✅** — Export Progress (Excel & PDF) sekarang:
1. Konsisten role-wise: kedua endpoint mengizinkan `Coach, Sr Supervisor, Section Head, HC, Admin` (PDF sebelumnya tidak include Coach).
2. Scope check berbasis `CoachCoacheeMapping` aktif:
   - Full access (Admin/HC/level ≤ 3) bypass seperti sebelumnya.
   - Coach hanya bisa export coachee yang dimapping aktif kepadanya (terverifikasi: Rino ✅, Iwan ❌).
   - SectionScoped hanya bisa export coachee dengan `AssignmentSection` matching section mereka (code review).
3. `coacheeUser.Section` tidak lagi dipakai — menghindari false positive ketika section pribadi coachee kebetulan match tapi assignment context sudah berbeda.
4. Helper terisolasi di `CanExportCoacheeProgressAsync` → satu titik perubahan jika kebijakan berevolusi.
