# Smoke Test CRIT-02 — Playwright Automation Plan

**Status:** PAUSED — menunggu jawaban user sebelum eksekusi
**Branch:** `bugfix/proton-coaching`
**Worktree:** `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Desktop\worktree-2`
**Related files:**
- `SMOKE-TEST-CRIT-02.md` — checklist manual original (SQL + langkah UI)
- `Controllers/CoachMappingController.cs` — file yang di-fix (CRIT-02)
- `BUG-HUNT-REPORT-PROTON-COACHING.md` — laporan bug hunt keseluruhan

---

## Konteks singkat

Plan CRIT-02 sudah selesai diimplementasi & build hijau. Perubahan:
1. `CleanupProgressForAssignment` → return `List<string>` (tidak lagi delete disk)
2. `CoachCoacheeMappingEdit` → satu transaction membungkus semua mutasi + audit log, file deletion post-commit, catch block return Json
3. Menutup 4 bug: A (mapping flush sebelum tx), B (file delete bukan tx-safe), C (ProtonTrack branch tanpa proteksi), D (regresi `RedirectToAction` dari JSON endpoint)

Sekarang perlu smoke test manual (5 skenario) tetapi user mau pakai Playwright + sqlcmd otomatis.

---

## Blocker — pertanyaan yang harus dijawab user sebelum mulai

1. **URL app lokal** + apakah sudah running? Atau Claude yang start via `dotnet run` background?
2. **Kredensial login Admin/HC** untuk DB dev (`HcPortalDB_Dev`)
3. **`sqlcmd` tersedia di PATH?** (cek: `sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT 1"`)
4. **Cara memicu failure untuk Skenario 2 & 4:**
   - (a) Inject `throw new Exception("smoke-test-injection")` sementara di awal `AutoCreateProgressForAssignment`, lalu revert — RECOMMENDED
   - (b) Cari unit "kosong" dari data real — tidak reliable
5. **Izin untuk start/stop `dotnet run` background** di sesi ini

---

## Rencana eksekusi bertahap

### Fase 0 — Setup & sanity
1. Cek `sqlcmd` available via Bash
2. Start app (jika belum) — `dotnet run` background
3. Query persiapan (lihat `SMOKE-TEST-CRIT-02.md` bagian Persiapan) → pilih `MAPPING_ID` kandidat dengan evidence file
4. Snapshot state awal: mapping row, progress rows, isi folder `wwwroot/uploads/evidence/<id>/`

### Fase 1 — Skenario 1: Happy path Phase 129 unit change
5. Playwright: `browser_navigate` → login page
6. Playwright: `browser_fill_form` kredensial → submit
7. Playwright: navigate ke halaman mapping admin, `browser_snapshot`
8. Playwright: klik Edit `MAPPING_ID`, ubah `AssignmentUnit`, Save
9. Playwright: `browser_network_requests` → tangkap response `CoachCoacheeMappingEdit`
10. Assert: status 200, `Content-Type: application/json`, body `{"success":true,...}`
11. sqlcmd: verifikasi mapping unit baru, progress lama hilang, progress baru ada
12. Bash: cek folder evidence lama sudah terhapus

### Fase 2 — Skenario 2: Failure path Phase 129 rollback
13. Edit source: inject `throw` di `AutoCreateProgressForAssignment` (line ~1280)
14. Kill background dotnet, rebuild, restart
15. Snapshot ulang state pre-test
16. Playwright: ulangi flow edit unit
17. Assert: response `success=false`, status 200 (bukan 302), JSON valid
18. sqlcmd: mapping unit **tidak berubah**, progress lama **utuh**
19. Bash: folder evidence lama **masih ada**
20. Revert injection, rebuild, restart

### Fase 3 — Skenario 3: Happy path ProtonTrack change
21. Playwright: edit mapping, ubah `ProtonTrackId`
22. Assert: assignment lama `IsActive=0`, baru `IsActive=1`, progress baru ada, folder lama terhapus

### Fase 4 — Skenario 4: Failure ProtonTrack rollback (Bug C)
23. Inject ulang, ulangi flow ProtonTrack
24. Assert rollback total: assignment lama tetap aktif, progress lama utuh, assignment baru tidak ada, folder lama masih ada
25. Revert injection

### Fase 5 — Skenario 5: AJAX response shape
Implicit di Fase 1-4 via `browser_network_requests`. Assertion eksplisit per request: status 200, `application/json`, body parseable JSON.

### Fase 6 — Report
26. Tulis `SMOKE-TEST-RESULT-CRIT-02.md` dengan status per skenario + bukti (response body, SQL result, file listing)
27. Jika semua hijau → siap commit & lanjut ke bug berikut

---

## Tools yang dipakai

- **Playwright MCP**: `browser_navigate`, `browser_fill_form`, `browser_snapshot`, `browser_click`, `browser_network_requests`, `browser_take_screenshot`
- **Bash**: `dotnet run` background, `dotnet build`, cek file/folder di disk, `sqlcmd`
- **Edit**: untuk inject/revert throw di `AutoCreateProgressForAssignment`

## Cara recall plan ini nanti

Di sesi berikutnya, cukup katakan ke Claude:
> "Baca `SMOKE-TEST-CRIT-02-PLAYWRIGHT-PLAN.md` dan lanjutkan dari fase yang belum selesai."

atau lebih spesifik:
> "Lanjut smoke test CRIT-02 Playwright, mulai dari Fase X."

Claude akan Read file ini, cek status, dan lanjut.
