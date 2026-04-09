# Smoke Test Result — MED-05 N+1 Query di NotifyReviewers

**Tanggal:** 2026-04-09
**Branch:** main
**Tester:** Claude (code review + dotnet build)
**App:** http://localhost:5277

**Scope:** `NotifyReviewersAsync` di `Controllers/CDPController.cs:1052` sebelumnya melakukan 2 round-trip query DB per panggilan (mapping lookup + reviewers lookup). Dipanggil dari loop di `SubmitEvidenceWithCoaching`. Dengan HIGH-05 fix, outer loop sudah dibatasi 1 coachee, tapi query dalam helper masih bisa dikompakkan.

---

## Fix Summary

| File | Change |
|------|--------|
| `Controllers/CDPController.cs:1052-1078` — `NotifyReviewersAsync` | Dua query sekuensial (CoachCoacheeMappings → Users) digabung menjadi satu LINQ Join query. Filter mapping aktif + section-matched reviewer dilakukan dalam satu round-trip. `Distinct()` dipakai karena satu coachee bisa punya > 1 mapping aktif (teoritis). |
| `BUG-HUNT-REPORT-PROTON-COACHING.md` | MED-05 ditandai ✅ FIXED 2026-04-09 |

Build: `dotnet build` → **0 errors**.

---

## Fix Detail

**Sebelum:**
```csharp
var mapping = await _context.CoachCoacheeMappings
    .FirstOrDefaultAsync(m => m.CoacheeId == coacheeId && m.IsActive);
if (mapping == null) return;

var section = mapping.AssignmentSection;
var reviewers = await _context.Users
    .Where(u => u.IsActive && u.Section == section && u.RoleLevel == 4)
    .Select(u => u.Id)
    .ToListAsync();
```
→ 2 round-trip per call.

**Sesudah:**
```csharp
var reviewers = await (
    from m in _context.CoachCoacheeMappings
    where m.CoacheeId == coacheeId && m.IsActive
    join u in _context.Users on m.AssignmentSection equals u.Section
    where u.IsActive && u.RoleLevel == 4
    select u.Id
).Distinct().ToListAsync();

if (reviewers.Count == 0) return;
```
→ 1 round-trip per call. `Distinct()` untuk safety jika coachee memiliki > 1 mapping aktif (edge case yang tidak seharusnya terjadi tapi tidak dicegah constraint).

---

## Impact Analysis

Sebelum fix:
- Per call `NotifyReviewersAsync`: **2 query** DB.
- Konteks asli (bug report): bulk submit 30 deliverable lintas 10 coachee → `2 × 10 = 20 query` hanya untuk lookup reviewer.
- Setelah HIGH-05 guard (`coacheeIds.Count > 1 → reject`), outer loop max 1 coachee → `2 query` per request. Overhead minim tapi tetap bisa dikompakkan.

Sesudah fix:
- Per call: **1 query** DB.
- Kombinasi HIGH-05 + MED-05: max **1 query** reviewer-lookup per bulk submit.
- Jika di masa depan HIGH-05 dilonggarkan (tidak direkomendasikan), overhead tetap linear 1 query per coachee alih-alih 2.

---

## Scenario Matrix

| # | Scenario | Expected | Actual (code review) | Verdict |
|---|----------|----------|---------------------|---------|
| A | Coachee punya mapping aktif, section punya reviewer level 4 | Notifikasi dikirim ke semua reviewer | Join menghasilkan reviewer IDs, `SendAsync` loop | ✅ PASS |
| B | Coachee tidak punya mapping aktif | Return tanpa kirim notif | Join hasilkan 0 row → `reviewers.Count == 0` → return | ✅ PASS |
| C | Coachee ada mapping tapi section tidak punya reviewer level 4 | Return tanpa kirim notif | Join inner filter → 0 row → return | ✅ PASS |
| D | Coachee punya 2 mapping aktif ke section sama dengan 1 reviewer | Notifikasi 1 kali (bukan duplikat) | `Distinct()` eliminates duplikat user ID | ✅ PASS |
| E | Exception di query (DB down dll.) | Log warning, tidak throw | `try/catch` existing di line 1054 tetap | ✅ PASS |
| F | Build | 0 errors | `dotnet build` → 0 errors | ✅ PASS |

---

## Cleanup

Tidak ada state mutasi. Code refactor only.

---

## Verdict

**MED-05 FIXED ✅** — `NotifyReviewersAsync` sekarang satu query saja (LINQ Join), kompatibel dengan kontrak existing (return ID list, kirim notifikasi via `SendAsync`). HIGH-05 guard + fix ini membuat reviewer-lookup overhead di `SubmitEvidenceWithCoaching` minimal: 1 DB round-trip per request terlepas dari jumlah deliverable yang di-submit bulk.

**Residual:** `SendAsync` masih dipanggil dalam loop per reviewer (N notifikasi untuk N reviewer). Ini bukan N+1 query DB tetapi N HTTP/SignalR push — acceptable karena per-reviewer fan-out memang diperlukan. Optimisasi lebih lanjut (batch notification dispatch) di luar scope MED-05.
