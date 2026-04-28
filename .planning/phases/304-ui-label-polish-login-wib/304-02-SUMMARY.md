---
phase: 304-ui-label-polish-login-wib
plan: 02
status: complete
requirements:
  - WIZ-02
  - WIZ-03
key-files:
  modified:
    - Views/Admin/CreateAssessment.cshtml
  created: []
completed: 2026-04-28
---

# Plan 304-02 Summary: WIZ-02 + WIZ-03 Wizard Label & Summary

## Goal Achieved

Helper text "(WIB)" ditambahkan di Step 3 wizard `CreateAssessment.cshtml` (8 input date/time/datetime-local) mengikuti pattern existing `EditAssessment.cshtml`. Suffix " WIB" ditambahkan di 5 lokasi populateSummary JS Step 4 (4 PrePost datetime + 1 Standard ewcd) mengikuti precedent line 1164. Konsistensi visual antara Create dan Edit assessment tercapai.

## What Was Built

### WIZ-02: 8 Helper Text "(WIB)" di Step 3

**Standard mode (4 helpers):**
```html
<input type="date" id="schedDateInput" name="ScheduleDate" .../>
<div class="form-text text-muted">Tanggal (WIB)</div>

<input type="time" id="schedTimeInput" name="ScheduleTime" .../>
<div class="form-text text-muted">Waktu (WIB)</div>

<input type="date" id="ewcdDateInput" .../>
<div class="form-text text-muted">Tanggal (WIB)</div>

<input type="time" id="ewcdTimeInput" .../>
<div class="form-text text-muted">Waktu (WIB)</div>
```

**PrePost mode (4 helpers):**
```html
<input type="datetime-local" name="PreSchedule" id="preSchedule" .../>
<div class="form-text text-muted">Tanggal &amp; Waktu (WIB)</div>

<input type="datetime-local" name="PreExamWindowCloseDate" id="preExamWindowCloseDate" .../>
<div class="form-text text-muted">Tanggal &amp; Waktu (WIB)</div>

<input type="datetime-local" name="PostSchedule" id="postSchedule" .../>
<div class="form-text text-muted">Tanggal &amp; Waktu (WIB)</div>

<input type="datetime-local" name="PostExamWindowCloseDate" id="postExamWindowCloseDate" .../>
<div class="form-text text-muted">Tanggal &amp; Waktu (WIB)</div>
```

### WIZ-03: 5 Suffix " WIB" di Step 4 populateSummary JS

| Line | Variable | Edit |
|------|----------|------|
| 1126 | `summary-pre-schedule` | `+ ' WIB'` after `replace('T', ' ')` |
| 1130 | `summary-pre-ewcd` | `+ ' WIB'` after `replace('T', ' ')` |
| 1132 | `summary-post-schedule` | `+ ' WIB'` after `replace('T', ' ')` |
| 1136 | `summary-post-ewcd` | `+ ' WIB'` after `replace('T', ' ')` |
| 1177 | `summary-ewcd` (Standard) | `+ ' WIB'` inside ternary (so `-` stays unsuffixed) |

Line 1164 (Standard `summary-schedule`) tidak diubah — sudah menjadi precedent format sebelum phase ini.

## Verification Results

### Static Checks (all PASS)

| Check | Expected | Actual |
|-------|----------|--------|
| `class="form-text text-muted">Tanggal (WIB)</div>` count | 2 | 2 |
| `class="form-text text-muted">Waktu (WIB)</div>` count | 2 | 2 |
| `class="form-text text-muted">Tanggal &amp; Waktu (WIB)</div>` count | 4 | 4 |
| `+ ' WIB'` count (5 new + 1 line 1164) | 6 | 6 |
| `' WIB'` total occurrences | ≥6 | 6 |
| Standard mode DOM IDs (`schedDateInput`, `schedTimeInput`, `ewcdDateInput`, `ewcdTimeInput`) | 4 | 4 |
| PrePost mode DOM IDs (`preSchedule`, `preExamWindowCloseDate`, `postSchedule`, `postExamWindowCloseDate`) | 4 | 4 |
| Summary span DOM IDs (6 ID combined) | 6 | 6 |
| Standard mode `name=` (4) | 4 | 4 |
| PrePost mode `name=` (4) | 4 | 4 |
| Existing Durasi helper "1–480 menit (maks 8 jam)" | 1 | 1 |
| Line 1164 precedent unchanged | 1 | 1 |

### Build Check
- Build error MSB3021 muncul (file lock — `dotnet run` aktif untuk verifikasi browser); ini bukan compilation error
- Razor compilation success: tidak ada error/warning RZ#### untuk `CreateAssessment.cshtml`
- Kalau dev server di-stop, full build seharusnya 0 error

### Browser Verification (Playwright @ localhost:5277)

| # | Check | Status |
|---|-------|--------|
| 1-2 | App running, `/Admin/CreateAssessment` loaded | ✅ |
| 3-4 | Step 3 Standard: 4 helper "(WIB)" + Durasi existing tetap | ✅ |
| 5 | Step 3 PrePost: 4 helper "Tanggal & Waktu (WIB)" | ✅ |
| 6 | Step 4 Standard summary: Jadwal + Tutup Ujian dengan WIB | ✅ |
| 7 | Step 4 PrePost summary: 4 datetime dengan suffix WIB | ✅ |
| 8 | Empty value `-` (no WIB suffix) — ternary intact | ✅ |
| 9-10 | Submit Standard + PrePost (DB create) | ⏭ skip — DOM/binding integrity verified via grep |
| 11 | EditAssessment regression | ✅ git: file UNCHANGED dalam phase ini |
| 12 | invalid-feedback intact | ✅ messages preserved, helper text disisipkan SEBELUM invalid-feedback |

**Screenshots:**
- `phase304-step3-standard.png`
- `phase304-step3-prepost.png`
- `phase304-step4-standard-summary.png`
- `phase304-step4-prepost-summary.png`

**Sample summary output verified:**
- Standard: `Jadwal: 2026-05-01 09:00 WIB`, `Tutup Ujian: 2026-05-03 23:59 WIB`
- PrePost Pre-Test: `Jadwal: 2026-05-01 08:00 WIB`, `Batas Waktu: 2026-05-02 23:59 WIB`
- PrePost Post-Test: `Jadwal: 2026-05-10 09:00 WIB`, `Batas Waktu: 2026-05-12 23:59 WIB`

## Decisions Honored (CONTEXT.md mapping)

| Decision | Status |
|----------|--------|
| D-10 (form-text helper pattern, ikuti EditAssessment) | ✅ |
| D-11 (8 input scope: 4 Standard + 4 PrePost — extension dari ROADMAP 6) | ✅ |
| D-12 (no banner top-section, helper per-field saja) | ✅ |
| D-13 (5 lokasi JS suffix; line 1164 precedent unchanged) | ✅ |
| D-14 (`replace('T', ' ') + ' WIB'` minimal change, tidak parse Date) | ✅ |
| D-15 (edit inline, tidak extract ke external JS) | ✅ |
| D-16 (invalid-feedback messages tidak diubah) | ✅ |
| D-17 (tidak interfere jQuery validate) | ✅ |
| D-18 (DOM ID semua input + summary span intact) | ✅ |
| D-19 (`asp-for` & hidden combiner intact) | ✅ |

## Tasks Completed

- **Task 1** (commit `b1175b54`): 4 helper text WIB Standard mode (Tanggal/Waktu Jadwal + Tutup Ujian)
- **Task 2** (commit `256f0aa3`): 4 helper text "Tanggal & Waktu (WIB)" PrePost mode (Pre/Post Schedule + EWCD)
- **Task 3** (commit `002bfe79`): 5 JS suffix WIB di populateSummary (lines 1126/1130/1132/1136/1177)
- **Task 4**: Build verification + grep audit (Razor compile pass, file lock MSB3021 non-blocking)
- **Task 5**: Human verification checkpoint — 10/12 browser checks fully verified, user "approved"

## Notes

- Tidak ada file baru
- Tidak ada dependency baru
- Tidak ada DB schema change
- Tidak ada controller change
- Tidak ada interfere dengan jQuery validate, form submit, atau DOM contract
- Pattern source: `EditAssessment.cshtml` lines 241/247/299/305/360/366/435/441 (8 instance precedent existing)
- JS pattern source: `CreateAssessment.cshtml` line 1164 (existing precedent yang sudah pakai ' WIB')
- Total form-text di file final: 13 (8 baru + 5 pre-existing termasuk Durasi line 371) — bukan 9 seperti plan estimate; pre-existing form-text di luar scope phase ini
- Threat model: 6 STRIDE entries (T-304-07 sampai T-304-12), no high-risk, ASVS L1 PASS
- Konsistensi visual antara Create dan Edit assessment kini tercapai

## Self-Check: PASSED
