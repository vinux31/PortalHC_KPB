---
phase: 359
slug: gate-berurutan-cleanup-a
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-10
---

# Phase 359 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

**Auditor:** gsd-security-auditor (Sonnet) — 2026-06-10
**Build status:** 0 error / 0 warning; dotnet test 148/148 + ProtonYearGate 7/7
**Human UAT:** 4/4 PASS — Playwright MCP @ localhost:5277 (2026-06-10T05:05Z, `359-HUMAN-UAT.md`)

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser → POST /Admin/CreateAssessment | Admin/HC kirim UserIds + ProtonTrackId; filter JS bisa di-bypass via request manual → server wajib re-validate | UserIds, ProtonTrackId (identitas pekerja) |
| Browser → POST CoachCoacheeMappingAssign | Admin/HC kirim CoacheeIds + ProtonTrackId; `ConfirmProgressionWarning` sebelumnya escape klien-controlled (di-drop Phase 359) | CoacheeIds, ProtonTrackId |
| Browser → POST MarkMappingCompleted | Admin/HC menandai graduated | MappingId |

Semua endpoint di atas: `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` (terverifikasi — lihat Auth Verification di bawah).

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-359-01 | Tampering | `ProtonYearGate.IsAllowed` | accept | Pure static predicate, no I/O/state (`ProtonCompletionService.cs:120-131`); 6 [Fact] + 1 integration hijau | closed |
| T-359-02 | Information Disclosure | `IsPrevYearPassedAsync` | accept | Baca penanda coachee+track saja, no PII (`ProtonCompletionService.cs:107-112`); semua caller endpoint `[Authorize(Roles="Admin, HC")]` | closed |
| T-359-03 | Elevation of Privilege / BAC | CreateAssessment POST (Proton) | mitigate | Pre-pass server-side re-validate tiap UserId — cross-year + 100% per-unit (`AssessmentAdminController.cs:1336-1392`); loop iterasi `eligibleUserIds` (:1414), bukan JS filter | closed |
| T-359-04 | Tampering (IDOR via UserId) | UserIds list di body | mitigate | Setiap UserId melewati gate yang sama (`AssessmentAdminController.cs:1365-1392`); gate tidak percaya input klien | closed |
| T-359-05 | Elevation via renewal side-door | RenewsSessionId path | mitigate | `isRenewal` hanya exempt gate cross-year (:1368); gate 100% deliverable (:1373-1388) tetap berjalan | closed |
| T-359-06 | Information Disclosure | catch block gate | mitigate | Audit catch hanya `_logger.LogWarning` (:1547-1558); TempData warning pakai counter integer, tidak surface `ex.Message` (pola Phase 334 D6) | closed |
| T-359-07 | Elevation of Privilege / BAC | CoachCoacheeMappingAssign cross-year | mitigate | Hard-block server-side penanda-based (`CoachMappingController.cs:533-548`); JSON `success=false` tanpa field `warning`; UAT re-test `ConfirmProgressionWarning:true` TETAP blocked | closed |
| T-359-08 | Broken Access Control | MarkMappingCompleted | mitigate | Gate `IsYearCompletedAsync(Tahun 3)` (:1117); `IsCompleted = true` single setter :1126 (single-door) | closed |
| T-359-09 | Information Disclosure | catch DbUpdateException | accept | `_logger.LogError(ex, ...)` log penuh, pesan user generik (`CoachMappingController.cs:640-645`); convention Phase 334 D6, no eksposur baru | closed |
| T-359-10 | Tampering (exempt side-door) | `isExemptFromCrossYear` flag | mitigate | `= false` hardcoded (:533) + komentar: Phase 360 wajib jaga gate 100% saat mengisi kondisi exempt | closed |
| T-359-11 | Information Disclosure | CDP views (level numeric removal) | accept | Penghapusan tampilan angka mengurangi permukaan data; 0 match field level/trend di `CDPDashboardViewModel.cs` | closed |
| T-359-12 | Denial of Service (render error) | View binding setelah prune | mitigate | ViewModel + binding dihapus berpasangan (0 orphan match); build 0 error; UAT render 4/4 PASS tanpa error 500 | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

**Score: 12/12 CLOSED**

## Auth Verification (constraint check)

| Endpoint | File:Line | Attribute |
|----------|-----------|-----------|
| POST CreateAssessment | `AssessmentAdminController.cs:841-842` | `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` |
| POST CoachCoacheeMappingAssign | `CoachMappingController.cs:457-458` | `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` |
| POST MarkMappingCompleted | `CoachMappingController.cs:1096-1098` | `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` |

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-359-01 | T-359-01 | Pure predicate tanpa I/O; kekeliruan logika tertangkap 6 [Fact], bukan vektor runtime | Design (PLAN 359-01) | 2026-06-10 |
| AR-359-02 | T-359-02 | Data penanda (boolean) coachee+track, bukan PII; akses dikontrol `[Authorize(Roles="Admin, HC")]` di semua caller | Design (PLAN 359-01) | 2026-06-10 |
| AR-359-03 | T-359-09 | Mengikuti pola Phase 334 D6 yang sudah diaudit; tidak ada perubahan yang menambah eksposur | Convention (PLAN 359-03) | 2026-06-10 |
| AR-359-04 | T-359-11 | Perubahan = penghapusan tampilan angka; permukaan informasi berkurang | Design (PLAN 359-04) | 2026-06-10 |

*Accepted risks do not resurface in future audit runs.*

---

## Non-Blocking Notes (info-level, bukan threat)

| ID | File | Deskripsi | Severity |
|----|------|-----------|----------|
| IN-01 | `Views/Admin/CoachCoacheeMapping.cshtml:779-796` | Dead JS `else if (data.warning)` — unreachable setelah hard-block; falls through ke `else { alert }` (benar) | Info |
| IN-02 | `AssessmentAdminController.cs` / `CoachMappingController.cs` | Cross-year silently allows bila prev track missing (`prevTahunKe = null` → Tahun-1 semantics); hanya pada katalog misconfigured; fail-open by design | Info |
| IN-03 | `AssessmentAdminController.cs:1210-1333` | Pre-Post mode return sebelum gate — Proton struktural tidak pernah pre-post; by design | Info |
| IN-04 | `wwwroot/documents/guides/*.html` | Panduan user masih menyebut "Competency Level" — docs follow-up, di luar scope code-prune | Info |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-10 | 12 | 12 | 0 | gsd-security-auditor (Sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter
