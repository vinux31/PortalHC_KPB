# Phase 378: Fix CMP CertificationManagement Route 500 - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix `GET /CMP/CertificationManagement` yang 500 (view-not-found). Action `CMPController.CertificationManagement` adalah **orphan duplikat** — `return View(vm)` mencari `Views/CMP/CertificationManagement.cshtml` yang tidak pernah dibuat. Entry produktif sudah route ke **CDP** canonical.

Scope: routing/controller fix + cleanup dead helper di `CMPController.cs` + tighten 1 e2e test. **Tidak** menyentuh path CDP (canonical) maupun model/view shared. Migration=false. REQ: CMPRT-01.

**Bukan scope:** redesign cert-management, ubah CDP, fitur baru.
</domain>

<decisions>
## Implementation Decisions

### Pendekatan Fix (Q1 → "sesuai reko" = Redirect + cleanup / hybrid)
- **D-01:** `CMPController.CertificationManagement(int page)` diubah jadi **thin redirect** ke CDP canonical: `RedirectToAction("CertificationManagement", "CDP")`. URL lama tetap kerja (no regression SC3), tak ada 404 mengejutkan untuk caller tak terduga.
- **D-02:** Gunakan **redirect default `RedirectToAction` (302 temporary)**, BUKAN 301 permanent — ini konsolidasi route internal; 302 hindari browser-cache lock-in kalau suatu saat route CMP dihidupkan lagi.

### Dead-Helper Cleanup (audit dead-set sudah dikonfirmasi 0 caller produktif)
- **D-03:** Hapus method dead di `CMPController.cs` (cluster cert-mgmt orphan, semua 0 caller `.cshtml`/JS — verified grep):
  - `FilterCertificationManagement` (~3639)
  - `CertificationManagementDetail` (~3666)
  - `FilterCertificationManagementDetail` (~3699)
  - `ExportSertifikatExcel` (~3738)
  - `GetCascadeOptions` (~3590) — semua fetch produktif panggil `/CDP/GetCascadeOptions`, CMP version dead
  - `GetSubCategories` (~3604) — idem, `/CDP/GetSubCategories`
- **D-04:** Hapus juga private builders yang jadi yatim setelah cluster dihapus (usage 3618–3783 SEMUA di dalam cluster, tak ada caller lain di controller):
  - `BuildSertifikatGroups` (~3830)
  - `BuildGroupViewModel` (~3845)
  - `BuildSertifikatRowsAsync` (~3868)
  - ⚠️ Planner WAJIB re-grep ketiga builder ini setelah hapus cluster untuk pastikan benar-benar 0 caller tersisa sebelum delete (defensive — file 3600+ baris).
- **D-05:** **KEEP** semua model class (`CertificationManagementViewModel`, `SertifikatGroupViewModel`, `SertifikatRow`, dll) — **shared dengan CDP**. Jangan hapus.

### Test Regression Net (Q2 → "sesuai reko" = Tegaskan assert)
- **D-06:** Test e2e **Y0** (`tests/e2e/exam-types.spec.ts` ~2044, saat ini documenting-only no-assert) di-**tighten jadi assert**: navigate `/CMP/CertificationManagement` → follow redirect → assert resolve ke `/CDP/CertificationManagement` dengan status **200** dan status **≠ 500**. Kunci SC2. Hapus komentar "documenting"/cabang 500.

### Claude's Discretion
- View candidate-dead `Views/CDP/Shared/_SertifikatGroupTablePartial.cshtml` (hanya dipakai `CMP.FilterCertificationManagement` yang dihapus) — boleh dihapus bila planner konfirmasi 0 referensi tersisa, ATAU dibiarkan (inert). Bukan blocker; bukan source action route.
- Cleanup komentar misleading `// CertificationManagement — dipindah dari CDPController` (CMP:3613) — hapus saat refactor.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & success criteria
- `.planning/ROADMAP.md` Phase 378 (baris ~47-50, ~73-77) — SC1 audit, SC2 no-500, SC3 CDP no-regression.

### Source — yang diubah
- `Controllers/CMPController.cs` — orphan action `CertificationManagement` (~3616) → ubah jadi redirect; dead helper cluster (~3590-3789) + builders (~3830/3845/3868) → hapus.

### Source — canonical (JANGAN ubah, referensi no-regression)
- `Controllers/CDPController.cs` — `CertificationManagement` (~3704) + helper CDP (canonical, working).
- `Views/CDP/CertificationManagement.cshtml` — view canonical (JS panggil `/CDP/GetCascadeOptions`, `/CDP/FilterCertificationManagement`, `/CDP/ExportSertifikatExcel`).
- `Views/CMP/Index.cshtml` (baris 98) — entry produktif: `@Url.Action("CertificationManagement", "CDP")` → bukti entry route ke CDP, bukan CMP.

### Audit trail — keputusan asli
- `.planning/quick/260406-l2i-pindahkan-menu-certification-management-/260406-l2i-SUMMARY.md` — keputusan eksplisit: *"Controller tetap CDPController... tidak perlu duplikasi action"*. Menu pindah ke CMP/Index page, link tetap ke CDP. CMP action = pelanggaran keputusan ini (duplikat botched).

### Test
- `tests/e2e/exam-types.spec.ts` — Y0 (~2044) `/CMP/...` documenting → tighten assert (D-06); FLOW X (~1951), W0.X0 (~1938), Y1/Y2 (~2059-2082) semua `/CDP/...` (canonical, harus tetap hijau = SC3).

### Shared (KEEP)
- `Models/CertificationManagementViewModel.cs` — shared CMP+CDP.
- `Views/CDP/Shared/_SertifikatGroupTablePartial.cshtml` — candidate-dead (D-05/discretion).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RedirectToAction(...)` pola standar ASP.NET MVC — fix utama D-01 satu baris.
- CDP cert-management lengkap & teruji (action+view+JS+test) = target redirect.

### Established Patterns
- Pemisahan controller per-domain (v12.0 refactor): CMP vs CDP. CertMgmt sah milik CDP.
- Cleanup dead/orphan code adalah pola berulang proyek (banyak phase hapus action orphan / extract helper). Hybrid redirect+cleanup konsisten dgn preferensi codebase-bersih.

### Integration Points
- Route table MVC default (`{controller}/{action}`) — tak ada custom route map untuk CertificationManagement (verified: tak ada `MapControllerRoute` khusus). Fix murni di action body + delete method.
- Tak ada migration, tak ada perubahan DB, tak ada perubahan view CMP/CDP yang dirender.

### Audit Status (SC1 — praktis sudah terkonfirmasi saat discuss)
- Entry produktif → CDP (`CMP/Index.cshtml:98`). ✅
- 0 link/test produktif butuh view CMP. Hanya Y0 (non-asserting). ✅
- Helper cluster CMP 0 caller (grep `.cshtml`/JS bersih). ✅
- Caveat: `319-RESEARCH.md` pernah list `/CMP/ExportSertifikatExcel` sbg kandidat — TAPI 0 test aktual wire (grep `tests/` ExportSertifikatExcel = 0). Aman; planner tetap re-verify.
</code_context>

<specifics>
## Specific Ideas

- User: "check dulu, sesuai reko" untuk kedua keputusan → defer ke rekomendasi setelah audit diverifikasi. Audit sudah diverifikasi saat discuss (dead-set bersih) → reko di-lock (D-01..D-06).
- Komentar `// CertificationManagement — dipindah dari CDPController` menyesatkan: nyatanya DUPLIKAT (view tak ikut dibuat), bukan move. Quick-task 260406-l2i membuktikan keputusan = no-duplicate.
</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase. (Cleanup `_SertifikatGroupTablePartial.cshtml` ada di D-05 sbg Claude's discretion dalam scope, bukan deferred.)
</deferred>

---

*Phase: 378-fix-cmp-certificationmanagement-route-500*
*Context gathered: 2026-06-14*
