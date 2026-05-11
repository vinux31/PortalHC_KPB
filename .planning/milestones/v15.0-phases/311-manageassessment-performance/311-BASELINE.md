---
phase: 311
artifact: baseline
captured: <ISO date — fill saat run>
captured_by: <user fullname>
db_snapshot: C:\temp\HcPortalDB_Dev_311_baseline.bak
captured_runs: 5
samples_used: runs 2-5 (run 1 dropped per D-12 JIT warmup)
build_commit: a4ce556e
status: pending-user-input
---

# Phase 311 — Baseline Measurement (Pre-Patch)

> **Status:** Scaffold tertulis oleh executor (Wave 0 Task 2). Numbers cells masih placeholder `...` — user wajib fill setelah eksekusi 5x cold runs (Task 3 checkpoint).

## Test Configuration

| Property | Value |
|----------|-------|
| Endpoint | `GET /Admin/ManageAssessment?tab=assessment&page=1&pageSize=20` |
| Filter | no search, no category, no statusFilter (default Open+Upcoming) |
| Cold runs | 5 (3s sleep antar request via curl, atau Ctrl+F5 hard refresh via browser) |
| Drop run | 1 (JIT warmup per D-12) |
| Samples used | runs 2-5 |
| Dataset snapshot | `C:\temp\HcPortalDB_Dev_311_baseline.bak` |
| Build commit | `a4ce556e` (Stopwatch instrumentation Phase 311 Wave 0 Task 1) |
| App command | `dotnet run --no-launch-profile` (port 5000) |
| Auth | `admin@pertamina.com / 123456` (UseActiveDirectory=false di appsettings.json) |

## Snapshot Procedure (Pitfall 9 mitigation)

Wajib backup Dev DB sebelum baseline run supaya post-patch run pakai dataset yang sama:

```bash
mkdir C:\temp 2>NUL
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE [HcPortalDB_Dev] TO DISK = 'C:\temp\HcPortalDB_Dev_311_baseline.bak' WITH INIT;"
```

Catat path backup di frontmatter `db_snapshot:` di atas. Plan 02 Task 8 akan restore dataset ini supaya delta T1..T5 murni dari patch.

## Run Procedure

1. **Start app fresh** (cold start untuk JIT warmup di run 1):
   ```bash
   dotnet run --no-launch-profile > baseline-311.log 2>&1
   ```
   Tunggu sampai "Now listening on: http://localhost:5000" muncul di `baseline-311.log`.

2. **Login admin** via browser ke `http://localhost:5000/Account/Login`.

3. **Hit endpoint 5x cold** dengan filter default `tab=assessment&page=1&pageSize=20`. Cara cold:
   - Browser: Ctrl+F5 hard refresh (bypass HTTP cache) atau close+reopen browser
   - Curl (preferred reproducibility): pakai cookie session, sleep 3s antar request

4. **Extract log entries:**
   ```bash
   grep "ManageAssessment perf breakdown" baseline-311.log
   ```
   Expected 5 baris dengan format:
   ```
   ManageAssessment perf breakdown: t1=...ms t2=...ms t3=...ms t4=...ms t5=...ms total=...ms tab=assessment search_present=False page=1
   ```

5. **Compute median (skip Run 1 per D-12):** ambil run 2-5 (4 samples), untuk tiap segment median = average dua nilai tengah setelah sort.

## Raw Log Entries

> Paste 5 baris hasil `grep "ManageAssessment perf breakdown" baseline-311.log` di bawah ini.

```
<run 1 raw line — JIT warmup, dropped>
<run 2 raw line>
<run 3 raw line>
<run 4 raw line>
<run 5 raw line>
```

## Per-Segment Numbers (ms)

| Run | T1 | T2 | T3 | T4 | T5 | Total | Wall-clock (curl/browser) |
|-----|----|----|----|----|----|-------|---------------------------|
| 1 (drop) | ... | ... | ... | ... | ... | ... | ... |
| 2 | ... | ... | ... | ... | ... | ... | ... |
| 3 | ... | ... | ... | ... | ... | ... | ... |
| 4 | ... | ... | ... | ... | ... | ... | ... |
| 5 | ... | ... | ... | ... | ... | ... | ... |
| **Median (2-5)** | **...** | **...** | **...** | **...** | **...** | **...** | **...** |

## Bottleneck Analysis

| Segment | Source Range | Median (ms) | % of Total | Scope (D-14) | Notes |
|---------|--------------|-------------|------------|--------------|-------|
| T1 (Assessment query) | L66-176 | ... | ...% | **IN** | Target patch D-08 (AsNoTracking) + D-09 (remove Include) + D-05/D-06 (indexes) |
| T2 (GetWorkersInSection) | L210 / Services/WorkerDataService.cs | ... | ...% | OUT | Default out-of-scope D-14; surface kalau Skenario B |
| T3 (GetAllWorkersHistory) | L212 / Services/WorkerDataService.cs | ... | ...% | OUT | Default out-of-scope D-14; surface kalau Skenario B |
| T4 (Sections + Units) | L220-223 / Data/ApplicationDbContext extensions | ... | ...% | OUT | Default out-of-scope D-14; biasanya cepat |
| T5 (Distinct Categories) | L172-176 (sekarang L181-185) | ... | ...% | **IN** | Target patch D-04 (IMemoryCache 5min TTL) + D-03 (CRUD invalidation) |

## Decision Gate (D-16)

**Skenario:** ☐ A (T1 dominan >60%) | ☐ B (T2/T3 dominan >50% combined) | ☐ C (mixed)

**User sign-off:** ___________________  **Date:** ___________

**Rationale:**
> _<user fills 1-2 paragraph rationale based on % of total per segment>_

**Decision:**
- ☐ **PROCEED** to Plan 02 Wave 1 (apply patches D-04..D-09 sesuai rencana — T1+T5 optimization sufficient)
- ☐ **STOP** — return ke `/gsd-discuss-phase` untuk:
  - Revisi D-14 scope: tambah `.AsNoTracking()` di `Services/WorkerDataService.cs` `GetWorkersInSection` + `GetAllWorkersHistory`, **ATAU**
  - Defer Phase 311 perf gain target, document hasil informational, bikin phase baru lazy-load architecture

## Skenario Decision Matrix (reference D-16)

| Skenario | Trigger | Rationale | Action |
|----------|---------|-----------|--------|
| A | T1 > 60% total | Assessment query benar-benar dominan; patch D-04..D-09 valid scope | PROCEED Plan 02 |
| B | T2 + T3 > 50% total combined | Training/History query bottleneck dominan; patch hanya T1+T5 TIDAK akan capai SC #6 ≥30% | STOP, expand scope atau bikin phase baru |
| C | Mixed (T1 ~ T2/T3) | Tidak jelas; user butuh decide explicit dengan numbers | User decide A path atau B path |

## Next Step

- Skenario A → executor commit baseline + lanjut Plan 02 (`311-02-PLAN.md`).
- Skenario B/C → orchestrator pause, surface decision ke user, balik ke `/gsd-discuss-phase` revisi D-14.

---

*Phase: 311-manageassessment-performance*
*Wave: 0 (baseline measurement)*
*Created: 2026-05-05 by gsd-executor (Plan 01 Task 2 scaffold)*
*User input pending: Run 5x cold + fill numbers + tick Skenario + sign-off*
