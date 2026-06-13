# Phase 374: UI ManagePackages + Lock + Pre/Post - Context

**Gathered:** 2026-06-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Frontend + endpoint untuk fitur Shuffle Toggle v27.0 di halaman **ManagePackages**. Phase ini HANYA:

1. **2 toggle** (Acak Soal / `ShuffleQuestions` + Acak Pilihan Jawaban / `ShuffleOptions`) di halaman ManagePackages per grup assessment — aktif walau `SamePackage` lock isi paket.
2. **Endpoint POST `UpdateShuffleSettings(int assessmentId, bool shuffleQuestions, bool shuffleOptions)`** — `[Authorize(Roles="Admin, HC")]` + AntiForgery + audit log + propagate ke SEMUA sibling.
3. **Lock toggle** (read-only + guard server-side) saat ada peserta mulai (`StartedAt != null` ATAU ada `UserPackageAssignment` grup).
4. **Warning non-blocking §9** (multi-paket + Acak Soal OFF + ukuran paket beda).
5. **Reminder visual Pre/Post (opsi Z)** — Pre OFF tapi Post ON (via `LinkedSessionId`); no auto-cascade.
6. **Hide toggle** untuk Proton Tahun 3 / Manual entry.

REQ: SHUF-10, SHUF-11, SHUF-12, SHUF-13, SHUF-14. Migration: **false** (kolom sudah ada Phase 372). UI hint: yes.

**BUKAN bagian phase ini:**
- Engine baca shuffle + reshuffle wiring → sudah selesai **Phase 372/373**.
- xUnit mode-matrix penuh + Playwright UAT → **Phase 375**.
- Memindah setting `SamePackage` → out of scope (spec §12).

</domain>

<decisions>
## Implementation Decisions

### Interaksi Simpan (gray area dibahas)
- **D-01:** **Tombol "Simpan Pengaturan" eksplisit** — BUKAN auto-save-on-flip. HC ubah satu/dua toggle → klik Simpan sekali → POST `UpdateShuffleSettings`. Alasan: perubahan kena SEMUA sibling (propagate), tombol eksplisit hindari salah-klik massal.
- **D-01a:** Implikasi return endpoint → **form POST + Redirect (PRG) ke ManagePackages + `TempData` sukses/gagal**, konsisten pola form existing di halaman ini (`CreatePackage`/`DeletePackage` pakai form POST + RedirectToAction). BUKAN AJAX Json. (Bentuk return exact = diskresi planner, tapi explicit-save mengarah ke form-POST-redirect; scout sempat usul Json — abaikan, pilih PRG.)

### Penempatan & Layout (gray area dibahas)
- **D-02:** **Card khusus "Pengacakan Soal & Jawaban"** sendiri, di bawah header ManagePackages (L8-17) dan **sebelum** panel ringkasan paket (L83-114). Host terkumpul: 2 toggle + tombol Simpan + status/alert lock + warning §9 + reminder Pre/Post. Reuse pola `card` + `card-header bg-light` existing + `form-check form-switch` (pola `IsTokenRequired` 372-UI-SPEC) + ikon `bi-shuffle text-primary`.

### Warning §9 + Reminder Pre/Post (gray area dibahas)
- **D-03:** Warning §9 + reminder Pre/Post = **alert di DALAM card Pengacakan** (bukan digabung ke panel ringkasan paket). Copy final dari spec §9 + §7.1 — JANGAN dipangkas/diubah.
- **D-03a:** Warning §9 **live JS recompute** saat Acak Soal di-flip (muncul/hilang real-time berdasar state checkbox saat ini) + saat page load. Trigger: jumlah paket-ber-soal ≥2 + Acak Soal OFF + ukuran paket beda. Mismatch ukuran paket sudah dihitung controller (≈L72-78 / `ViewBag` mismatch existing) — reuse, jangan hitung ulang dari nol.
- **D-03b:** Reminder Pre/Post (opsi Z) **HANYA di halaman Post** (`ViewBag.IsPostSession`), berdasar **SAVED state Pre** via `LinkedSessionId` (Pre.`ShuffleQuestions`==false && Post.`ShuffleQuestions`==true). Saved-state-driven (bukan live JS lintas-halaman), no auto-cascade, no hidden state.

### Affordance Lock (gray area dibahas)
- **D-04:** Saat terkunci: **switch `disabled` + alert banner di card** jelaskan alasan (mis. "Pengaturan pengacakan terkunci — sudah ada peserta yang memulai ujian"). Pola konsisten dgn lock `SamePackage` (ManagePackages.cshtml L29-44).
- **D-04a:** **Defense-in-depth** — UI disabled BUKAN satu-satunya guard. Endpoint `UpdateShuffleSettings` WAJIB tolak perubahan server-side saat lock-condition true (SHUF-11), kembalikan TempData error. Disabled UI = UX; server guard = enforcement.

### Locked dari spec (carry-forward — JANGAN diubah, JANGAN ditanya ulang)
- **Per-assessment + propagate sibling** key `(Title, Category, Schedule.Date)` — spec §2/§5; pola `EditAssessment` POST `foreach siblings`.
- **Endpoint** `UpdateShuffleSettings(int,bool,bool)` `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` + audit (`AuditLogService.LogAsync(actorUserId, actorName "NIP - FullName", "UpdateShuffleSettings", desc, assessmentId, "AssessmentSession")`, try/catch warn-only) + propagate foreach sibling — spec §7.
- **Lock condition:** ada sibling `StartedAt != null` ATAU ada `UserPackageAssignment` dgn `AssessmentSessionId ∈ siblingSessionIds` — spec §7.
- **Hide toggle:** Proton Tahun 3 (`Category == "Assessment Proton" && TahunKe == "Tahun 3"`) ATAU `IsManualEntry == true` — spec §7.
- **SamePackage TIDAK dipindah**; toggle shuffle tetap aktif walau `SamePackage` lock isi paket (`ViewBag.IsSamePackageLocked` hanya lock create/delete paket) — spec §2/§7.
- **Copy toggle** "Acak Soal" / "Acak Pilihan Jawaban" + help-text edukatif Bahasa Indonesia = **372-UI-SPEC** (reuse verbatim di ManagePackages; frasa "jawaban benar tetap dinilai dengan benar" WAJIB ada).
- **Default ON**; grading by `PackageOption.Id` (acak opsi tak pengaruh nilai) — carried 372/373.
- **Migration = false** (kolom `ShuffleQuestions`/`ShuffleOptions` sudah live sejak Phase 372).

### Claude's Discretion
- Bentuk return exact endpoint (form POST+Redirect PRG vs partial render) — lean PRG karena D-01 explicit-save.
- Markup/ID exact card + toggle + alert (selama reuse pola Bootstrap existing + copy 372-UI-SPEC verbatim).
- Apakah jalankan `/gsd-ui-phase 374` (UI-SPEC formal) sebelum plan — **opsional**; 372-UI-SPEC + spec §7/§9 sudah cukup detail untuk kontrak visual. Planner/user putuskan.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec utama (semua keputusan terkunci)
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` — relevan Phase 374: **§2** (keputusan locked: lokasi UI, lock timing, independensi, Pre/Post opsi Z, SamePackage), **§7** (UI ManagePackages + endpoint + lock + hide Proton/Manual), **§7.1** (Pre/Post reminder opsi Z), **§9** (UI warning non-blocking — copy final), **§5** (propagasi sibling), **§3** (arsitektur grup sibling).

### Requirements
- `.planning/REQUIREMENTS.md:68-72` — SHUF-10 (toggle+endpoint), SHUF-11 (lock+guard), SHUF-12 (warning), SHUF-13 (reminder Pre/Post via LinkedSessionId), SHUF-14 (hide Proton Th3/Manual).

### Roadmap
- `.planning/ROADMAP.md:150-160` — Phase 374 goal + 4 Success Criteria.
- `.planning/ROADMAP.md:114` + `:93` — ⚠️ koordinasi file-overlap v25.0 (`AssessmentAdminController.cs`) + STATE v25.0 append-only (WAJIB cek sebelum execute).

### UI contract (reuse copy + pola toggle)
- `.planning/phases/372-data-foundation-propagasi-toggle/372-UI-SPEC.md` — copy final toggle + help-text Bahasa Indonesia + pola `form-check form-switch` (IsTokenRequired `CreateAssessment.cshtml:505-508`) + ikon `bi-shuffle text-primary`. Reuse verbatim di ManagePackages.

### Phase sebelumnya (engine sudah jadi)
- `.planning/phases/373-shuffle-engine-read-logic-reshuffle/373-CONTEXT.md` — D-04 reshuffle guard "Not started/Abandoned" sejalan konsep lock; engine `ShuffleEngine` sudah baca flag.

### Kode existing (verified scout 2026-06-13 — re-grep line di execute-time, 367/368 bisa drift)
- `Views/Admin/ManagePackages.cshtml` — header/toolbar (L8-17), lock SamePackage alert (L29-44), CopyFromPre (L46-68), panel ringkasan paket + mismatch warning (L83-114), ET distribution (L120-166), create form + package list (L168-257).
- `Controllers/AssessmentAdminController.cs` — `ManagePackages` GET (L5264-5324: `ViewBag.IsPostSession` L313-area, `ViewBag.PreSessionId` dari `LinkedSessionId`, `ViewBag.IsSamePackageLocked` L5320-5321, `ViewBag.AssignmentCounts`); `ReshufflePackage`/`ReshuffleAll` (L5062-5251: started-detection L5078-5084, sibling query L5086-5091, UserPackageAssignment lookup L5103); `EditAssessment` sibling propagate (L1803-1814 / spec sebut 2007-2031).
- `Services/AuditLogService.cs:9-43` — `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)`.
- `Models/AssessmentSession.cs` — `ShuffleQuestions`/`ShuffleOptions` (L38-42), `SamePackage` (L191), `TahunKe` (L108), `Category` (L16), `IsManualEntry` (L137), `LinkedSessionId`, `StartedAt`, `AssessmentType`.
- `Models/UserPackageAssignment.cs` — lock detection (assignment existence per sibling).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola `form-check form-switch`** — `CreateAssessment.cshtml:505-508` (IsTokenRequired); 372-UI-SPEC sudah kontrak copy + ikon. Reuse identik di card ManagePackages.
- **Lock alert SamePackage** — `ManagePackages.cshtml:29-44` (alert + ikon + alasan). Cetakan untuk alert lock toggle (D-04).
- **Sibling query** `(Title, Category, Schedule.Date)` — `AssessmentAdminController.cs:5086-5091`. Reuse untuk lock-detection + propagate di `UpdateShuffleSettings`.
- **Started-detection** — `:5078-5084` (`StartedAt != null` → InProgress) + `UserPackageAssignment` lookup `:5103`. Basis lock-condition SHUF-11.
- **Audit log** — `AuditLogService.LogAsync(...)`, try/catch warn-only (pola controller `:5136-5142`).
- **Sibling propagate `foreach`** — `EditAssessment` POST. Pola untuk update 2 kolom ke semua sibling.
- **Mismatch ukuran paket** — sudah dihitung controller (≈L72-78 / panel ringkasan L83-114). Reuse untuk trigger warning §9 (D-03a).

### Established Patterns
- ManagePackages = ViewBag-driven (bukan strongly-typed VM). `ViewBag.Packages`, `ViewBag.IsPostSession`, `ViewBag.PreSessionId`, `ViewBag.IsSamePackageLocked`, `ViewBag.AssignmentCounts`. Tambah `ViewBag` baru untuk shuffle state + lock + hide.
- Form di halaman ini = POST + RedirectToAction + TempData (PRG). D-01a ikut pola ini.
- `SamePackage` lock = isi paket saja; toggle shuffle terpisah & independen (scout note 1).
- Pre/Post = grup terpisah (Schedule.Date beda) → toggle truly independen; reminder lewat `LinkedSessionId` saja.

### Integration Points
- `ManagePackages` GET — tambah ViewBag: `ShuffleQuestions`/`ShuffleOptions` (sudah di model), `IsShuffleLocked` (hitung started/assignment), `HideShuffleToggle` (Proton Th3/Manual), `ShowSizeMismatchWarning`, reminder Pre/Post (`PreShuffleQuestions` via LinkedSessionId).
- `Views/Admin/ManagePackages.cshtml` — sisip card Pengacakan setelah header, sebelum panel ringkasan paket.
- **Endpoint baru** `UpdateShuffleSettings` di `AssessmentAdminController.cs` — guard lock + propagate sibling + audit.

### ⚠️ Constraint Koordinasi (WAJIB planner/executor)
- **File-overlap v25.0:** Phase 374 sentuh `AssessmentAdminController.cs` (area sibuk v25.0 367/368). 367/368 sudah SHIPPED LOCAL — cek konflik lintas-sesi clear sebelum `/gsd-execute-phase 374`. **Re-grep semua line number di execute-time.**
- **STATE.md sengaja pinned v25.0** (roadmap v27.0 append-only). Phase dir `374-ui-managepackages-lock-pre-post` dibuat manual. JANGAN `/gsd-new-milestone` / `/gsd-complete-milestone` vanilla.
- Sequential strict v27.0: 372 ✅ → 373 ✅ → **374** → 375.

</code_context>

<specifics>
## Specific Ideas

- **Explicit-save + live-JS-warning kompatibel:** warning §9 baca state checkbox saat ini (unsaved) via JS — tetap akurat sebelum klik Simpan. Reminder Pre/Post baca SAVED state Pre (lintas-halaman) → memang harus saved-state, tak bisa live.
- **Lock = dua lapis** (D-04a): UI `disabled` + server reject di endpoint. SHUF-11 acceptance = guard server-side ditolak, bukan cuma UI.
- **Hide ≠ Lock:** Proton Th3/Manual = toggle DISEMBUNYIKAN total (tak relevan, defensif). Started = toggle TAMPIL tapi disabled (informasikan kenapa).

</specifics>

<deferred>
## Deferred Ideas

- xUnit lock-guard + propagate test + Playwright UAT (toggle ON/OFF + lock + reminder + warning) → **Phase 375**.
- Memindah setting `SamePackage` ke halaman package → out of scope (spec §12).
- Auto-cascade Pre→Post → ditolak by design (pakai reminder opsi Z saja).

None lain — diskusi tetap dalam scope phase.

### Reviewed Todos (not folded)
- 1 todo match-phase 374 ber-`score` rendah (tak masuk threshold, tak ditampilkan tool) — tak relevan scope; tidak di-fold.

</deferred>

---

*Phase: 374-ui-managepackages-lock-pre-post*
*Context gathered: 2026-06-13*
