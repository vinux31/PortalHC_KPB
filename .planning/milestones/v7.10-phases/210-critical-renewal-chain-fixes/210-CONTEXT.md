# Phase 210: Critical Renewal Chain Fixes - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaikan 3 bug kritikal di renewal chain: bulk FK assignment, badge count sync, dan verifikasi IsPassed filter pada TrainingRecord. Scope terbatas pada backend logic di AdminController.cs — tidak ada perubahan UI/UX.

</domain>

<decisions>
## Implementation Decisions

### FIX-01: Bulk FK Assignment
- Hapus kondisi `(i == 0)` di CreateAssessment POST (line ~1330-1331)
- Semua AssessmentSession baru dalam bulk renew harus mendapat RenewsSessionId/RenewsTrainingId yang identik (point ke source sertifikat yang sama)
- Fix: `RenewsSessionId = model.RenewsSessionId` dan `RenewsTrainingId = model.RenewsTrainingId` untuk semua iterasi

### FIX-02: Badge Count Sync
- Pendekatan: **reuse BuildRenewalRowsAsync()** di Admin/Index action
- Ganti query manual di line 59-82 dengan panggilan `BuildRenewalRowsAsync().Count`
- Alasan: jamin konsistensi 100% antara badge count dan jumlah baris RenewalCertificate — satu source of truth
- Hapus query lightweight yang terpisah (renewedSessionIds, renewedTrainingIds, expiredTrainingCount, expiredAssessmentCount)

### FIX-03: IsPassed Filter Verification
- TrainingRecord **tidak punya field IsPassed** — jadi Set 2 & Set 4 di BuildRenewalRowsAsync tanpa IsPassed filter sudah benar
- Item ini menjadi **verifikasi saja**: pastikan kode sudah benar, tidak perlu diubah
- Success criteria di-update: verifikasi bahwa TR-based renewal sets (Set 2 & 4) memang tidak memerlukan IsPassed filter

### Claude's Discretion
- Optimasi performance jika reuse BuildRenewalRowsAsync terasa berat di Admin/Index (misal: extract count-only variant)
- Error handling dan edge cases

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Renewal chain logic
- `Controllers/AdminController.cs` line 1320-1335 — CreateAssessment POST loop yang membuat AssessmentSession per user (FIX-01 bug location)
- `Controllers/AdminController.cs` line 55-85 — Admin/Index badge count query (FIX-02 bug location)
- `Controllers/AdminController.cs` line 6605-6660 — BuildRenewalRowsAsync renewal exclusion sets (FIX-03 verification target)

### Data model
- `Models/AssessmentSession.cs` — RenewsSessionId, RenewsTrainingId FK fields
- `Models/TrainingRecord.cs` — RenewsSessionId, RenewsTrainingId FK fields (no IsPassed field)
- `Migrations/20260319001833_AddRenewalChainFKs.cs` — Migration yang menambah renewal FK columns

### Requirements
- `.planning/REQUIREMENTS.md` — FIX-01, FIX-02, FIX-03 definitions

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BuildRenewalRowsAsync()` (AdminController:6605) — method yang sudah menghitung renewal rows lengkap dengan 4 exclusion sets. Akan di-reuse untuk badge count.

### Established Patterns
- Renewal exclusion menggunakan 4 sets: AS→AS, TR→AS, AS→TR, TR→TR (union-based)
- AssessmentSession sets filter IsPassed==true; TrainingRecord sets tidak (karena TR tidak punya IsPassed)
- Bulk create di CreateAssessment POST menggunakan loop `for (int i = 0; i < userIds.Count; i++)`

### Integration Points
- Admin/Index action → badge count (ViewBag.RenewalCount)
- CreateAssessment POST → bulk AssessmentSession creation
- RenewalCertificate view → memanggil BuildRenewalRowsAsync via controller actions

</code_context>

<specifics>
## Specific Ideas

- Badge count harus **identik** dengan jumlah baris di halaman RenewalCertificate — satu source of truth via BuildRenewalRowsAsync
- TrainingRecord selalu dianggap valid (tidak ada konsep gagal/lulus) — ini mempengaruhi interpretasi FIX-03

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 210-critical-renewal-chain-fixes*
*Context gathered: 2026-03-21*
