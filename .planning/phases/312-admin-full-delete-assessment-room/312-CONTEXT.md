# Phase 312: Admin Full-Delete Assessment Room - Context

**Gathered:** 2026-05-07
**Status:** Ready for planning
**Mode:** interactive discuss — 4 gray areas selected, all answered

<domain>
## Phase Boundary

Tambah role-tier guard di body method `DeleteAssessment()` (Controllers/AssessmentAdminController.cs:2022) dan `DeleteAssessmentGroup()` (line 2127) sehingga akun **Admin** dapat full-delete assessment room termasuk yang berstatus `Completed` atau yang sudah punya response peserta, sementara akun **HC** dilarang melakukan delete pada kondisi tersebut. Authorize attribute existing `[Authorize(Roles = "Admin, HC")]` (line 2020, 2125) **tidak diubah** — guard ditambah di body method, bukan di attribute.

Plus UI conditional render di `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (line 266-272) tombol Hapus: Admin selalu tampil, HC tombol hidden untuk assessment Completed atau dengan response peserta. AuditLog entry pada delete sukses sertakan field `Status` & `ResponseCount` di description.

**Acceptance criteria (dari ROADMAP.md):**
1. Role tier guard di body method (Admin override, HC blocked)
2. Authorize attribute tidak diubah
3. UI conditional render tombol Hapus
4. AuditLog +Status +ResponseCount di description
5. Cascade delete tetap utuh (PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages, UserPackageAssignments)
6. Smoke test 5 skenario: Admin+Open OK, Admin+Completed OK, HC+Open(no-response) OK, HC+Completed BLOCK, HC+Open(with-response) BLOCK

**In-scope (per discussion 2026-05-07):**
- 3 method delete (lihat D-04 di bawah): `DeleteAssessment()`, `DeleteAssessmentGroup()`, `DeletePrePostGroup()`
- Frontend conditional render + impact preview confirm step (D-02)
- AuditLog entries success + failure (D-03)
- Smoke test ekstra 1 skenario untuk Pre-Post path (total 6 skenario)

**Out-of-scope:**
- Soft delete / archive flag (cascade hard-delete tetap)
- Restore / undo functionality (sengaja irreversible)
- Phase modifikasi peran lain (Worker, Coach) — hanya Admin & HC tier
</domain>

<decisions>
## Implementation Decisions

### HC button visibility (saat blocked)
- **D-01:** **Hide entirely.** Tombol Hapus untuk role HC tidak di-render sama sekali jika `assessment.Status == "Completed"` ATAU `responseCount > 0`. Konsisten dengan SC #3 literal. Pakai pattern `@if (User.IsInRole("Admin") || (canHcDelete))` di `_AssessmentGroupsTab.cshtml` — tidak disabled-with-tooltip karena UI clean dan SC eksplisit "hidden".
- Konsekuensi: HC user yang lihat row Completed tidak akan melihat tombol Hapus sama sekali. Tidak ada explanation in-place — alasan delete dilarang HARUS ada di documentation atau help, BUKAN di UI.

### Confirm dialog: context-aware + multi-step impact preview
- **D-02:** **Confirm dialog 2-step dengan impact summary.** Untuk semua 3 delete actions (regardless role/status), tampilkan modal/dialog yang enumerate dampak SEBELUM final destructive confirm:
  - Step 1 — **Impact preview**: tampilkan jumlah peserta affected (response count), jumlah sertifikat affected (rows dengan `IsCertificateGenerated == true` atau setara), jumlah packages, current Status, jumlah attempt history rows. Untuk PrePost: tampilkan kedua sesi (Pre + Post) terpisah.
  - Step 2 — **Final confirm**: setelah user lihat impact preview, tombol "Hapus permanen" dengan warning text "Tidak bisa di-undo" + cascade enumeration. User klik untuk submit POST.
- Replace plain `onclick="return confirm('Hapus...')"` (current state) dengan modal-based flow di view layer. Backend tidak perlu berubah untuk impact preview (read-only JSON endpoint atau via initial render data attributes).
- Rationale (user note): "lebih aman, dan logic lebih bagus" — destructive action butuh transparency tentang scope dampak.

### AuditLog: blocked attempts logging
- **D-03:** **Log failed attempts juga.** Ketika role tier guard reject (HC POST direct ke endpoint dengan status Completed atau hasResponses), tulis AuditLog entry:
  - Action name: `"DeleteAssessmentBlocked"` / `"DeleteAssessmentGroupBlocked"` / `"DeletePrePostGroupBlocked"`
  - Description harus include: actor NIP + name, target SessionId, reason (Status atau ResponseCount yang trigger block), endpoint yang dipanggil
  - Entity type: `"AssessmentSession"` (atau `"AssessmentGroup"` untuk group methods)
- Successful delete (per SC #4): AuditLog action `"DeleteAssessment"` / `"DeleteAssessmentGroup"` / `"DeletePrePostGroup"` dengan description yang sertakan `Status=...` dan `ResponseCount=...`
- Pattern existing: `_auditLog.LogAsync(...)` dengan signature `(userId, actorName, action, description, entityId, entityType)` (lihat AssessmentAdminController.cs:2098-2104 untuk pola).

### Scope: include DeletePrePostGroup
- **D-04:** **Apply guard ke 3 method delete:** `DeleteAssessment()` (line 2022), `DeleteAssessmentGroup()` (line 2127), DAN `DeletePrePostGroup()` (action terpisah, dipakai tombol "Hapus Grup Pre-Post" di view line 253). Walaupun ROADMAP SC menyebut hanya 2 method pertama, `DeletePrePostGroup()` adalah method ketiga yang sama-sama destructive (hapus 2 sesi Pre+Post + responses). Tanpa guard di sini, ada **celah security**: HC role bisa bypass dengan pakai endpoint Pre-Post.
- Smoke test extra: tambah 1 skenario "HC+PrePost+Completed BLOCK" → total 6 skenario (5 dari SC + 1 extra).

### Claude's Discretion
- Exact modal markup (Bootstrap 5 modal vs custom). Tetap ikut existing `Views/Admin/Shared/_*` Bootstrap 5 conventions.
- Helper method untuk impact summary — boleh inline di partial view (compute via ViewBag) atau extract ke action endpoint JSON. Researcher/planner pilih berdasarkan parity dengan existing patterns.
- Helper method `IsAdmin()` di controller — boleh inline `User.IsInRole("Admin")` cek atau extract ke private method untuk DRY across 3 methods.
- Exact text wording confirm dialog (Bahasa Indonesia, formal-ish, ikut tone Phase 304 polish).
</decisions>

<specifics>
## Specific Ideas

- **D-01 hide pattern**: pakai precedent `User.IsInRole("Admin")` yang sudah ada di `Views/Admin/CoachWorkload.cshtml:37,63,251,275` dan `Views/Admin/Index.cshtml:19,35,51,90`. Tidak perlu invent pattern baru.
- **D-02 impact preview**: contoh tipe info yang harus ditampilkan saat confirm:
  - Status saat ini (Open/InProgress/Completed/dst.)
  - Jumlah peserta yang akan kehilangan akses & data response
  - Jumlah sertifikat yang akan terhapus (jika ada)
  - Jumlah packages + questions yang akan terhapus
  - Untuk PrePost: tampilkan kedua sesi (Pre + Post) dengan breakdown masing-masing
- **D-03 AuditLog naming**: ikut existing convention "{Action}" / "{Action}Blocked" untuk distinguish success vs failure. Pattern ini mirip dengan how existing `EditAssessment` / `LegacyEditBlocked` (cek apakah ada precedent — kalau tidak, "{Action}Blocked" suffix adalah konvensi baru yang reasonable).
- **D-04 codepath**: 3 method delete saat ini sudah pakai pattern try/catch + cascade RemoveRange pattern yang sama. Guard bisa di-DRY via private method `EnsureCanDelete(AssessmentSession assessment, int responseCount)` yang return TempData error + redirect kalau blocked.
</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/ROADMAP.md` §"Phase 312: Admin Full-Delete Assessment Room" — 6 success criteria, dependencies, plan count
- `.planning/REQUIREMENTS.md` §"DEL-01" — full requirement statement, role tier rules, AuditLog spec
- `.planning/REQUIREMENTS.md` §"v15.0" — milestone-level requirement coverage (DEL-01 maps Audit-29Apr T1)

### Existing implementation surface (impl files)
- `Controllers/AssessmentAdminController.cs:2018-2121` — `DeleteAssessment(int id)` action, current cascade logic
- `Controllers/AssessmentAdminController.cs:2123-2218` — `DeleteAssessmentGroup(int id)` action, sibling/group cascade
- `Controllers/AssessmentAdminController.cs:1929` — Authorize attribute reference for ManageAssessment endpoint group
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:240-275` — Action dropdown menu where 3 delete buttons live (Hapus, Hapus Grup, Hapus Grup Pre-Post)

### Pattern precedents (read for consistency)
- `Views/Admin/CoachWorkload.cshtml:37,63,251,275` — `@if (User.IsInRole("Admin"))` conditional render pattern
- `Views/Admin/Index.cshtml:19,35,51,90` — Role-based UI gating dengan multi-role check
- `Controllers/AssessmentAdminController.cs:2098-2104` — `_auditLog.LogAsync()` signature dan typical description format
- `.planning/phases/309-worker-cert-defensive-submitted-status/309-CONTEXT.md` — Phase paling recent yang juga gabung role-tier behavior + UI conditional + audit log (jika ada section relevan)

### Prior phase decisions to honor
- `.planning/phases/311-manageassessment-performance/311-CONTEXT.md` — Phase 311 baru saja refactor `ManageAssessment` controller + view jadi shell + 3 partial actions. **Implication:** delete buttons sekarang ada di `_AssessmentGroupsTab.cshtml` partial (post-Phase 311 split), BUKAN di `ManageAssessment.cshtml` shell. Modifikasi UI conditional render targetkan partial.

### Architecture / project conventions
- `.planning/PROJECT.md` — Vision, principles, security posture, Bahasa Indonesia messaging convention
- `./CLAUDE.md` — Project instructions (Always respond in Bahasa Indonesia)
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`User.IsInRole("Admin")` Razor pattern**: 8 occurrences di Views/Admin/. Apply yang sama di `_AssessmentGroupsTab.cshtml` untuk D-01 conditional render. Tidak perlu helper baru.
- **`_auditLog.LogAsync(...)` service**: existing dependency-injected, signature stable, dipakai 8 kali di AssessmentAdminController. Re-use untuk D-03 success + blocked entries.
- **Cascade delete pattern di `DeleteAssessment()`**: `PackageUserResponses` → `AssessmentAttemptHistory` → `AssessmentPackages` (with Questions+Options) → `AssessmentSessions.Remove`. Pattern ini sudah teruji — guard ditambah **sebelum** cascade block, tidak mengubah cascade itu sendiri.
- **Bootstrap 5 modal infrastructure**: `_Layout.cshtml` sudah include Bootstrap 5 + bootstrap-icons. Modal untuk D-02 impact preview bisa pakai pattern existing modal di Views/Admin/ (cek presedent terdekat saat plan-phase).

### Established Patterns
- **TempData pattern untuk redirect-after-action**: sukses → `TempData["Success"]`, error → `TempData["Error"]`. Pattern existing dipakai di line 2034, 2044, 2112, 2118. Guard reject pakai pattern yang sama.
- **try/catch wrap full action body**: existing 3 methods semua wrap try/catch around delete body. Guard ditambah di awal try block sebelum cascade — kalau guard reject, return RedirectToAction tanpa hit cascade.
- **AntiForgeryToken di delete forms**: existing 3 forms pakai `@Html.AntiForgeryToken()`. Pertahankan saat refactor view.

### Integration Points
- **AssessmentSession.Status field**: existing field, values dari `AssessmentConstants.AssessmentStatus` (Open, Upcoming, InProgress, Completed, dst.). Guard cek `assessment.Status == "Completed"`.
- **Response count computation**: existing pattern `_context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).CountAsync()` — pakai untuk guard cek `responseCount > 0`. Untuk impact preview D-02 panggil .CountAsync() di action handler atau in-view via ViewBag.
- **PrePost group linking**: `AssessmentSession.LinkedGroupId` (introduced di Phase 311 Plan 03 index). `DeletePrePostGroup(int linkedGroupId)` query 2 sesi by LinkedGroupId. Guard cek apply ke kedua sesi (jika salah satu Completed atau ada response, block).
</code_context>

<deferred>
## Deferred Ideas

- **Soft delete / archive flag** — Out of scope; cascade hard-delete tetap sesuai SC #5. Catat untuk backlog kalau requirement future ada audit-restore use case.
- **Restore / undo destructive action** — Eksplisit out of scope (operasi sengaja irreversible). Tidak ada slot di milestone v15.0.
- **Worker / Coach role permissions** — Hanya Admin & HC tier yang masuk Phase 312 scope. Modifikasi role lain backlog.
- **Phase 313 (Block Manual Submit) integration test sharing** — Phase 313 juga modifikasi behavior auth/validation. Plan-phase nanti bisa cek apakah ada smoke test infrastructure yang bisa di-share, tapi tidak block Phase 312.
- **Bulk delete UI** — Tidak ada di SC. Phase masa depan kalau muncul kebutuhan.

</deferred>

---

*Phase: 312-admin-full-delete-assessment-room*
*Context gathered: 2026-05-07*
