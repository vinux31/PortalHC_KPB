# Phase 326: Validator Hardening (P03 + P06) - Context

**Gathered:** 2026-05-27
**Status:** Ready for planning
**Discuss mode:** interactive (4 gray areas + 1 follow-up resolved by user)

<domain>
## Phase Boundary

Cegah data kontradiktif tersimpan via validator form Add/Edit Training. DAG enforcement (monotonic renewal date) + Permanent+ValidUntil mutual exclusion.

**In-scope:**
- P03 (MED): DAG enforcement — tanggal renewal harus strictly > tanggal source. Cycle otomatis ditolak via monotonic constraint. Self-renewal check di Edit-side.
- P06 (MED): Reject `CertificateType="Permanent"` + `ValidUntil != null` (data kontradiktif: sertifikat permanen tidak boleh punya expiry).

**Out-of-scope (defer v20.0):**
- DateOnly migration `ValidUntil` (Phase 327 separate, sequential strict v19.0 plan).
- Renewal source change full UX di Edit form (radio + dropdown picker mirror AddTraining renewal-mode). Edit form Phase 326 hanya display read-only + clear button (D-12).
- Async validator client-side (real-time AJAX cycle check saat user ketik). Server-side ModelState saja sufficient untuk milestone v19.0.
- DB CHECK constraint untuk Permanent+ValidUntil (P09) — app-level mitigated sudah cukup.

</domain>

<decisions>
## Implementation Decisions

### Locked di Spec (`docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §6)

- **D-01:** P03 fix Add — tambah validator setelah existing `srcAlreadyRenewed` check di `Controllers/TrainingAdminController.cs:254`. Pattern:
  ```csharp
  if (model.RenewsTrainingId.HasValue)
  {
      var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
      if (src != null && src.Tanggal >= model.Tanggal)
          ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
  }
  ```
- **D-02:** P06 fix Add — tambah validator sebelum `if (!ModelState.IsValid)` di AddTraining POST (TrainingAdminController.cs:264):
  ```csharp
  if (model.CertificateType == "Permanent" && model.ValidUntil != null)
      ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");
  ```
- **D-03:** Error message strings locked verbatim per spec:
  - P03 monotonic: `"Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew."`
  - P03 self-renewal: `"Sertifikat tidak boleh renewal dirinya sendiri."`
  - P06: `"Sertifikat Permanent tidak boleh punya tanggal expired."`
- **D-04:** No EF migration. Pure validator additions di POST handler + minor VM extension + view tambahan minor.
- **D-05:** ModelState binding key — P06 pakai `"ValidUntil"` (field-level error display via `asp-validation-for`). P03 pakai `""` (summary-level error display via `asp-validation-summary="All"`) konsisten dengan existing `srcAlreadyRenewed` pattern di L246+L253.

### Gray Area Decisions (user-selected interactive)

#### Edit-side P03 Scope
- **D-06:** Extend `EditTrainingRecordViewModel` + Edit handler (Q1 jawaban: "Extend EditTrainingRecordViewModel + Edit handler").
  - **Rasional:** Honor SC-6 spec "Edit case self-renewal check". Tanpa extend, SC-6 vacuous karena field tidak ada di VM.
  - **Scope:** Tambah `RenewsTrainingId` + `RenewsSessionId` + `RenewalSourceTitle` (display) ke VM. Edit handler accept + validate.

#### Edit-side P03 UX Form
- **D-07:** Display read-only + Clear button (Q1 follow-up jawaban: "A — Display read-only + Clear button").
  - **Rasional:** Minimal effort (~15 menit Razor), kasih user kontrol "hapus link renewal salah" tanpa bangun UI picker lengkap. User TIDAK bisa change ke source berbeda — kalau perlu change, user delete record + add baru.
  - **View tambahan (EditTraining.cshtml):** Kalau `RenewsTrainingId != null` atau `RenewsSessionId != null`, render section read-only:
    - Text: `"Renewal dari: {RenewalSourceTitle}"`
    - Hidden inputs: `RenewsTrainingId` + `RenewsSessionId` passthrough current value
    - Button: `[Hapus link renewal]` — JavaScript clear kedua hidden input (set to empty), submit-time backend treat empty as null
  - **Controller GetTraining Edit (TrainingAdminController.cs:408 area):** Populate `RenewalSourceTitle` via lookup TR/AS source kalau renewal FK present, mirror pattern existing AddTraining renewal-mode di L278-284.
  - **Self-renewal guard (D-08 below):** Handler validate `RenewsTrainingId != model.Id` (sertifikat tidak boleh renewal dirinya sendiri) — defense kalau form tampering.

#### P03 Symmetric Both FK Branches
- **D-08:** Apply DAG check symmetric ke kedua FK — `RenewsTrainingId` (TR source) DAN `RenewsSessionId` (AS source) (Q2 jawaban: "Ya, symmetric kedua FK").
  - **Rasional:** Bug-findings P03 implicit cover both — renewal chain bisa AS→TR atau TR→AS. Parity dengan existing `srcAlreadyRenewed` double-check pattern di L243+L250. Risk gap: cycle AS→TR→AS lolos kalau hanya TR branch divalidasi.
  - **Code pattern AS branch (insertion setelah D-01):**
    ```csharp
    if (model.RenewsSessionId.HasValue)
    {
        var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
        // AssessmentSession date field: research-phase confirm field nama (TanggalMulai? StartTime? CreatedAt?)
        if (srcAs != null && srcAs.{DateField} >= model.Tanggal)
            ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
    }
    ```
  - **Open item plan-phase:** Verify field nama tanggal di `AssessmentSession` model. Kandidat: `TanggalMulai`, `StartTime`, `CreatedAt`. Pilih yang setara semantik dengan `TrainingRecord.Tanggal` (tanggal sertifikat issued).

#### Test Approach
- **D-09:** Manual repro per spec §6.3 (Q3 jawaban: "Manual repro saja per spec").
  - **Rasional:** Zero xUnit overhead untuk Phase 326. Validator P03 butuh DB query (`_context.TrainingRecords.FindAsync`) — InMemory provider setup overhead tidak proportional vs 1-2 jam phase effort.
  - **Scope:** 6 scenario manual via UI Add/Edit Training (cover 6 SC roadmap):
    - SC-1 (P03): Add TR renewal tanggal lebih awal dari source → form error display
    - SC-2 (P03 no-regression): Add TR renewal tanggal valid > source → lolos
    - SC-3 (P06): Add TR Permanent + ValidUntil isi → form error display field ValidUntil
    - SC-4 (P06 no-regression): Add TR Permanent + ValidUntil null → lolos
    - SC-5 (P06 no-regression): Add TR Annual + ValidUntil valid → lolos
    - SC-6 (P03 Edit self-renewal): Edit TR yang IS renewal, klik [Hapus link renewal] → sukses clear; form tampering set RenewsTrainingId=model.Id → error display
  - **Anti-scope:** TIDAK extend `HcPortal.Tests/` Phase 326. Phase 327 lebih cocok karena `DeriveCertificateStatus` pure function (test-friendly).
  - **Playwright optional:** Cek `tests/` directory untuk existing scenario sentuh form Add/Edit Training — kalau ada, run pre-IT-promo (Phase 327 batch).

#### Same-day Renewal Edge Case
- **D-10:** Strict `>` reject same-day renewal per spec literal `src.Tanggal >= model.Tanggal` (Q4 jawaban: "Strict > per spec literal").
  - **Rasional:** Renewal semantik = sertifikat baru pasti hari berikutnya minimum. Same-day renewal = duplicate (bukan renewal). Tidak ada workflow legit di Portal HC untuk same-day input.
  - **Code:** `src.Tanggal >= model.Tanggal` reject. Equivalent `src.Tanggal == model.Tanggal` juga reject.

### Implementation Order (Plan-phase guidance)

Sequential strict per spec §6:
1. P06 Add — pure synchronous validator (no DB query), gampang test manual
2. P03 Add `RenewsTrainingId` branch — async DB query, follow `srcAlreadyRenewed` pattern
3. P03 Add `RenewsSessionId` branch — symmetric, butuh confirm date field nama
4. EditTrainingRecordViewModel extension — tambah 3 field (RenewsTrainingId, RenewsSessionId, RenewalSourceTitle)
5. GetTraining Edit handler — populate RenewalSourceTitle via lookup mirror AddTraining L278-284
6. EditTraining.cshtml — tambah section read-only renewal display + clear button JS
7. EditTraining POST handler — terima RenewsTrainingId/RenewsSessionId, validate self-renewal (D-07), P06 ValidUntil check (mirror Add)
8. Manual UAT 6 SC via browser lokal `http://localhost:5277`

### Claude's Discretion

- **Razor section markup style** — pakai Bootstrap card border + read-only text format konsisten dengan section "Data Training" existing (card border-0 shadow-sm pattern).
- **JavaScript clear-button approach** — inline onclick handler vs separate `<script>` block. Pakai inline `onclick="document.getElementById('RenewsTrainingId').value=''; document.getElementById('RenewsSessionId').value=''; this.parentElement.style.display='none'; return false;"` untuk minimal footprint.
- **GetTraining Edit handler lookup query** — pakai `Include(t => t.User)` kalau ingin display "Renewal dari: {Judul} ({UserName})" lengkap, atau title saja kalau cukup. Mirror existing AddTraining renewal-mode L278-284 yang pakai full include.

### Folded Todos

Tidak ada todo difold. Pending todos tidak relevan ke validator hardening scope.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca file ini sebelum planning/implementing.**

### Spec Utama (sumber decision lock)
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §6 — Phase 326 detail (P03 + P06) full implementation code, verifikasi step, testing strategy.

### Bug Source
- `docs/sertifikat-ecosystem/bug-findings.html` — 6 bug actionable v19.0 (P03 cycle detection §, P06 Permanent+ValidUntil §). Repro step + fix snippet.

### Workflow Wajib
- `docs/DEV_WORKFLOW.md` — Lokal → Dev → Prod promo SOP. Phase 326 commit + push, IT promo batch akhir setelah Phase 327 ship.
- `CLAUDE.md` (project root) — Develop Workflow section: cek bug Dev URL, reproduce + fix lokal, verifikasi lokal `dotnet build` + `dotnet run`, commit, IT promo.

### Codebase Existing (touch points)
- `Controllers/TrainingAdminController.cs:188` — AddTraining POST handler. Insertion P03 validator setelah L254 (`srcAlreadyRenewed` check). P06 validator sebelum L264 (`if (!ModelState.IsValid)`).
- `Controllers/TrainingAdminController.cs:408` — GetTraining Edit handler (GET). Populate `RenewalSourceTitle` mirror AddTraining renewal-mode L278-284.
- `Controllers/TrainingAdminController.cs:442` — EditTraining POST handler. Tambah P03 + P06 validators + self-renewal guard. Pattern existing pakai `TempData["Error"] = firstError` translation dari `ModelState` (L453-461) — ModelState.AddModelError tetap work karena translation flow existing.
- `Models/EditTrainingRecordViewModel.cs` — Extend dengan `RenewsTrainingId` (int?), `RenewsSessionId` (int?), `RenewalSourceTitle` (string?) display.
- `Models/CreateTrainingRecordViewModel.cs` — Reference: existing FK fields `RenewsTrainingId` + `RenewsSessionId` (line 59-60). Mirror untuk Edit VM.
- `Models/TrainingRecord.cs` — Entity dengan `Tanggal` (DateTime) field, `RenewsTrainingId` + `RenewsSessionId` FK.
- `Models/AssessmentSession.cs` — Entity dengan FK renewal. **Plan-phase confirm:** field nama tanggal yang setara dengan `TrainingRecord.Tanggal` (kandidat: `TanggalMulai`, `StartTime`, `CreatedAt`).
- `Views/Admin/EditTraining.cshtml` — Full-page form view. Tambah section read-only renewal display + clear button antara section "Data Training" (L42+) atau bawah section sertifikat. Lihat hidden inputs existing pattern L37-40.
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — Reference EditTraining link/button. Tidak perlu ubah.

### Roadmap & State
- `.planning/ROADMAP.md:636-651` — Phase 326 goal + 6 SC + files affected + migration flag.
- `.planning/STATE.md` — Current focus Phase 326 validator-hardening-p03-p06.
- `.planning/phases/325-security-hardening-p01-p02-p05/325-CONTEXT.md` — Prior phase context: pattern existing untuk error string verbatim (D-06), xUnit project setup (D-08).

### Memory Snapshot Sesi
- v19.0 strategy: sequential strict 325 → 326 → 327, IT promo Dev 1× batch akhir setelah Phase 327 ship.
- Phase 325 SHIPPED 2026-05-27 (5/5 plan + 5/5 SC PASS) — AddTraining POST handler L188 sudah disentuh untuk FileUploadHelper integration; Phase 326 insertion P03/P06 di L240+ existing renewal validators no conflict.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`CreateTrainingRecordViewModel.RenewsTrainingId/RenewsSessionId`** (Models/CreateTrainingRecordViewModel.cs:59-60): Pattern existing FK renewal di VM. Mirror untuk extend Edit VM (D-06).
- **AddTraining renewal-mode populate** (`Controllers/TrainingAdminController.cs:278-284`): `ViewBag.RenewalSourceTitle = src.Judul ?? "";` pattern — reuse untuk GetTraining Edit handler populate `RenewalSourceTitle` field VM.
- **`srcAlreadyRenewed` pattern** (TrainingAdminController.cs:241-254): Existing double-renewal prevention dengan symmetric kedua FK branches. P03 mirror struktur ini — branch `RenewsTrainingId.HasValue` + branch `RenewsSessionId.HasValue`.
- **`TempData["Error"] = firstError` translation** (TrainingAdminController.cs:453-461): Edit handler existing translate `ModelState` errors ke `TempData["Error"]` untuk display via RedirectToAction. P03/P06 di Edit pakai `ModelState.AddModelError(...)` standard — translation flow existing auto-handle.
- **`asp-validation-summary="All"` di view** (Views/Admin/EditTraining.cshtml:30): Existing summary display untuk ModelState empty-key errors. P03 summary-level (key=`""`) langsung tampil di sini.
- **`asp-validation-for` per field** (Views/Admin/EditTraining.cshtml:52,59,78 dll): Field-level error display pattern. P06 pakai key `"ValidUntil"` → tampil dekat input ValidUntil (L153 area).

### Established Patterns
- **Renewal FK mutual exclusion**: Existing validator di AddTraining L236 — `RenewsTrainingId.HasValue && RenewsSessionId.HasValue` ditolak. Pattern reuse di Edit kalau perlu (D-07 hidden input passthrough hanya 1 dari 2 FK populated normally).
- **Async DB lookup di validator**: `await _context.TrainingRecords.AnyAsync(...)` pattern (L243). P03 pakai `FindAsync(id)` untuk lookup single source record + `.Tanggal` field.
- **GetTraining Edit ViewBag populate**: Pattern existing di EditTraining GET handler (L408+) — populate `ViewBag.KategoriOptions`, `ViewBag.SubKategoriOptions`. Extend untuk populate `RenewalSourceTitle` via lookup TR/AS source.
- **ModelState error display flow Add vs Edit**:
  - AddTraining (L264-288): `if (!ModelState.IsValid) return View(model)` — error display di view dengan `asp-validation-*` tag helper. P06 `asp-validation-for="ValidUntil"` langsung work.
  - EditTraining (L453-461): `if (!ModelState.IsValid) TempData["Error"] = firstError; RedirectToAction(...)` — error compress jadi 1 toast message. P06 di Edit tampil sebagai toast, BUKAN field-level. Acceptable tradeoff (Edit form full-page redirect-back UX).

### Integration Points
- **EditTrainingRecordViewModel extension** — Backward compatible: tambah 3 nullable field. Existing form POST tidak break (hidden input absent → bind null OK).
- **EditTraining.cshtml renewal section** — Insertion antara section "Data Training" akhir (~L130 area) sebelum section "Sertifikat" (CertificateFile L158). Render conditional kalau `Model.RenewsTrainingId != null || Model.RenewsSessionId != null`.
- **JavaScript clear button** — Inline onclick handler cukup. Tidak perlu jQuery/external script. Pattern existing di view = mostly static, JS minimal.
- **GetTraining Edit lookup** — Tambah lookup TR/AS source kalau renewal FK present:
  ```csharp
  if (model.RenewsTrainingId != null)
  {
      var src = await _context.TrainingRecords.FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
      model.RenewalSourceTitle = src?.Judul ?? "(sertifikat sumber tidak ditemukan)";
  }
  else if (model.RenewsSessionId != null)
  {
      var srcAs = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
      model.RenewalSourceTitle = srcAs?.Title ?? "(sertifikat sumber tidak ditemukan)";
  }
  ```

### Constraint
- **EditTrainingRecordViewModel TIDAK punya renewal FK fields saat ini** (Models/EditTrainingRecordViewModel.cs:6-66) — sumber gray area Q1. Extension wajib untuk D-06.
- **EditTraining handler pattern beda dari AddTraining** — Edit redirect-back ke ManageAssessment dengan TempData (L450), Add return View(model) dengan ModelState (L287). P06 di Edit tampil sebagai toast (compressed firstError), Add tampil field-level. Acceptable per existing UX convention.
- **AssessmentSession date field nama** — Plan-phase confirm field setara `TrainingRecord.Tanggal`. Kandidat: `TanggalMulai`, `StartTime`, `CreatedAt`. Salah pilih → P03 AS branch validator semantik salah.

</code_context>

<specifics>
## Specific Ideas

- **Spec line 203** `src.Tanggal >= model.Tanggal` — strict `>` reject same-day (D-10 confirmed).
- **Spec line 217** self-renewal check `model.RenewsTrainingId == model.Id` — di Edit handler saja (Add tidak punya `model.Id` di submit). D-07 implementasi sebagai defense kalau form tampering.
- **Spec line 234** P06 message `"Sertifikat Permanent tidak boleh punya tanggal expired."` — locked verbatim D-03.
- **Bug-findings P03 implicit cover both FK** — D-08 symmetric kedua branch (TR + AS). Tidak menunggu post-ship audit "kenapa cycle AS→TR→AS lolos".

</specifics>

<deferred>
## Deferred Ideas

- **Full picker UI Edit renewal** (radio + dropdown + typeahead) — defer ke milestone improvement UX kalau user complaint sering salah set renewal. Q1 follow-up rejected option B.
- **Async client-side validator** (real-time AJAX cycle check) — defer indefinitely. Server-side sufficient v19.0.
- **DB CHECK constraint Permanent+ValidUntil** (P09) — defer v20.0 backlog, app-level mitigated cukup.
- **xUnit controller test extension untuk P03/P06** — defer Phase 327 kalau pure function `DeriveCertificateStatus` jadi target test (lebih cocok unit test).
- **DateOnly migration ValidUntil** — Phase 327 separate.
- **Multi-step renewal chain depth validator** (cek depth > N reject) — out-of-scope, monotonic constraint sudah cukup untuk Portal HC use case.

### Reviewed Todos (not folded)

Tidak ada todo direview-dan-tidak-difold sesi ini.

</deferred>

---

*Phase: 326-validator-hardening-p03-p06*
*Context gathered: 2026-05-27*
*Mode: interactive (4 gray areas + 1 follow-up: D-06 extend Edit VM, D-07 read-only + clear button UX, D-08 symmetric kedua FK, D-09 manual repro, D-10 strict > reject same-day)*
