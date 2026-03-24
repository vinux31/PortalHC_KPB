# Phase 242: UAT Setup Flow - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Verifikasi bahwa Admin/HC dapat melakukan seluruh alur setup assessment tanpa error — dari membuat kategori hierarchy hingga melihat ET coverage matrix. Ini adalah UAT phase (bukan development fitur baru). Semua fitur sudah terbangun di v1.0-v8.4.

</domain>

<decisions>
## Implementation Decisions

### Pendekatan UAT
- **D-01:** Hybrid approach — Claude quick-scan code setiap flow untuk identifikasi & fix obvious bugs, kemudian user verifikasi di browser dengan checklist

### Scope Verifikasi
- **D-02:** Happy path + basic validation — test 4 success criteria (SETUP-01 s/d SETUP-04) PLUS verifikasi validation error (input kosong, duplicate name, format salah) karena goal phase adalah "tanpa error"

### Bug Handling
- **D-03:** Fix blocking bugs langsung di phase ini agar UAT flow tidak terhambat. Bug minor/cosmetic dicatat untuk Phase 247 (Bug Fix Pasca-UAT)

### Data UAT
- **D-04:** Verifikasi seed data dari Phase 241 tampil benar, PLUS buat data baru dari nol untuk test create flow end-to-end (sesuai success criteria yang meminta "membuat baru")

### Urutan Test Flow
- **D-05:** Berurutan sesuai dependency alami: Kategori → Assessment → Paket Soal & Import → ET Coverage Matrix. Setiap step harus pass sebelum lanjut ke step berikutnya

### Dokumentasi Hasil
- **D-06:** Checklist pass/fail per success criteria dan validation case, disertai catatan deskripsi bug yang ditemukan. Tidak perlu screenshot

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §Setup — SETUP-01, SETUP-02, SETUP-03, SETUP-04

### Controllers
- `Controllers/AdminController.cs` — ManageCategories (~line 841), CreateAssessment (~line 987), ManagePackages (~line 6289), ImportPackageQuestions (~line 6496)

### Views
- `Views/Admin/ManageCategories.cshtml` — Category hierarchy with parent-child indent
- `Views/Admin/CreateAssessment.cshtml` — 4-step wizard (Kategori → Peserta → Settings → Konfirmasi)
- `Views/Admin/ManagePackages.cshtml` — Package list + ET coverage matrix
- `Views/Admin/ImportPackageQuestions.cshtml` — Excel upload + paste tab
- `Views/Admin/PreviewPackage.cshtml` — Question preview per package

### Seed Data
- `.planning/phases/241-seed-data-uat/` — Seed data UAT (Phase 241 complete)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminController: Full CRUD untuk kategori, assessment, paket soal, import soal sudah ada
- ManageCategories.cshtml: Parent hierarchy dengan indent visual sudah implemented
- CreateAssessment.cshtml: 4-step wizard dengan token, jadwal, durasi, sertifikat
- ImportPackageQuestions.cshtml: Dual-mode import (file upload + paste) dengan template download
- ManagePackages.cshtml: ET coverage matrix (rows=ET groups, cols=packages)

### Established Patterns
- Authorization: ManageWorkers actions `[Authorize(Roles = "Admin, HC")]`, other Admin actions `[Authorize(Roles = "Admin")]`
- Import pattern: Download template button + file upload + process + redirect to list

### Integration Points
- Seed data Phase 241: assessment "OJT Proses Alkylation Q1-2026", kategori "OJT Operasi Kilang", Paket A + 15 soal
- Navigation: Admin/Index hub > section cards > respective management pages

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

### Reviewed Todos (not folded)
- **Real-time Assessment System** (todo: realtime-assessment.md) — SignalR real-time updates untuk monitoring. Lebih relevan ke Phase 244 (UAT Monitoring & Analytics), bukan Phase 242

None — discussion stayed within phase scope

</deferred>

---

*Phase: 242-uat-setup-flow*
*Context gathered: 2026-03-24*
