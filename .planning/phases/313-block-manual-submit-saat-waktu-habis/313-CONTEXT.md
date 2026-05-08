# Phase 313: Block Manual Submit Saat Waktu Habis - Context

**Gathered:** 2026-05-07
**Status:** Ready for planning
**Mode:** interactive discuss — 8 gray areas selected, all answered (15 decisions)

<domain>
## Phase Boundary

Modify LIFE-03 server-side timer enforcement di `Controllers/CMPController.cs` `SubmitExam()` method (line 1616-1628) dari single-tier (`elapsed > allowed + 2min grace` reject all) jadi **2-tier branching** berdasarkan `isAutoSubmit` parameter:

- **Tier 1 — manual reject tanpa grace:** `!isAutoSubmit && elapsed > (Duration + ExtraTime)` → reject manual submit immediately, redirect StartExam dengan TempData error explanatory.
- **Tier 2 — auto reject setelah grace:** `elapsed > (Duration + ExtraTime + 2min grace)` → reject auto-submit telat (existing LIFE-03 behavior preserved untuk network latency edge case).

Plus frontend `Views/CMP/StartExam.cshtml` modify Submit button: saat countdown=0, disable button + ubah label ke "Waktu Habis - Submit Otomatis Berjalan...". Auto-submit handler tetap fire immediate at countdown=0 (existing behavior).

Plus AuditLog blocked entries dengan ActionType `SubmitExamBlocked` + Description fields `{UserId, SessionId, ElapsedMin, AllowedMin, Type}` untuk traceability.

**Acceptance criteria (dari ROADMAP.md):**
1. Modify LIFE-03 block jadi 2-tier branching `isAutoSubmit`
2. Tier 1: `!isAutoSubmit && elapsed > allowed` reject manual + TempData + redirect StartExam
3. Tier 2: `elapsed > allowed + 2min grace` reject auto-submit (existing preserved)
4. Frontend `StartExam.cshtml`: countdown=0 disable Submit manual, auto-submit handler tetap aktif
5. AuditLog rejection alasan `manual_after_timeup` dengan `{UserId, SessionId, ElapsedMin, AllowedMin}`
6. Verifikasi 3 tipe ber-timer: Online, PreTest, PostTest (Manual exclude)
7. E2E test 6 skenario manual/auto × before-time/at-time/in-grace/after-grace

**In-scope (per discussion 2026-05-07):**
- 2-tier branching di `SubmitExam()` LIFE-03 block
- Frontend countdown handler + button disable di `StartExam.cshtml`
- AuditLog blocked entries (success path TIDAK di-modify per D-06)
- 3 timer type verification (uniform message/redirect/ActionType per D-14)
- Manual type exclude via explicit Type field check (per D-15)
- E2E 6 skenario via DB manipulation StartedAt back-dated + dedicated fixture title (D-07/D-08)
- Frontend retry 3x dengan backoff untuk auto-submit network failure (D-10)
- Page reload behavior: recompute timer dari server StartedAt (D-12)

**Out-of-scope:**
- Server-side last-resort save (deferred ke milestone v16.0+ per D-11)
- Auto-save partial responses periodic (deferred)
- Refactor `IDateTimeProvider` injection (deviasi scope)
- Phase modifikasi flow Manual type assessment

</domain>

<decisions>
## Implementation Decisions

### Manual reject UX (pesan + redirect)
- **D-01:** **Pesan TempData explanatory.** Saat tier-1 reject manual submit, set TempData["Error"] = `"Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman."` — menjelaskan kenapa di-block dan apa yang akan terjadi (auto-submit handler frontend). User tidak bingung.
- **D-02:** **Redirect back ke `StartExam(id)`.** Konsisten dengan existing LIFE-03 tier-2 behavior. Auto-submit handler frontend masih aktif dari sana, jadi auto-submit tetap bisa fire setelah user lihat banner. Tidak redirect ke `/CMP/Index` (kehilangan handler) atau ExamSummary (asumsi salah submit sudah berhasil).

### Frontend disable visual saat countdown=0
- **D-03:** **Greyed out + label berubah.** Saat countdown reach 0 di `StartExam.cshtml`:
  - Set button `disabled` (cursor: not-allowed)
  - Ubah label dari "Submit" jadi "Waktu Habis - Submit Otomatis Berjalan..."
  - Tooltip on hover: "Auto-submit sedang berjalan, mohon tunggu"
  - Bisa tambah spinner kecil untuk feedback visual aktif
- **D-04:** **Auto-submit fire immediate at countdown=0.** Tidak delay 3-5 detik atau tunggu user action. Server grace 2 menit sudah cover network latency. Existing handler structure preserved.

### AuditLog ActionType naming
- **D-05:** **`SubmitExamBlocked`** (mengikuti Phase 312 D-03 pattern `{Action}Blocked`). Description format:
  ```
  HC/User role manual submit blocked after timeup. Type={Online|PreTest|PostTest} ElapsedMin={X} AllowedMin={Y} SessionId={id}
  ```
  Konsisten dengan convention yang sudah established phase 312.
- **D-06:** **Success path tidak di-modify.** Phase 313 hanya tambah blocked entry. Existing logging untuk submit valid (sebelum/dalam grace) tidak di-touch. Scope minimal — SC eksplisit hanya minta blocked entry. Hindari scope creep.

### E2E test seeding strategy
- **D-07:** **DB manipulation StartedAt back-dated.** Test setup hook update `AssessmentSessions.StartedAt` ke value yang trigger kondisi target:
  - "before time": StartedAt = NOW - 5 menit (untuk Duration 60 menit)
  - "at time": StartedAt = NOW - (Duration) menit
  - "in grace": StartedAt = NOW - (Duration + 1) menit
  - "after grace": StartedAt = NOW - (Duration + ExtraTime + 5) menit
  - Test cepat (<1 menit per skenario), reliable, no real-time wait. Tidak perlu mock IDateTimeProvider (deviasi scope).
- **D-08:** **Dedicated fixture title pattern.** Pakai pattern Phase 312 WR-04: assessment dengan title `Phase 313 Timer Fixture {Type} {Scenario}` (mis. "Phase 313 Timer Fixture Online ManualAfterGrace"). Test cari fixture, skip kalau tidak ada (bukan fail). Setup script terpisah untuk seed 6 fixture saat dev awal.

### Race condition (manual click di detik akhir)
- **D-09:** **Strict 0-grace untuk manual** (per ROADMAP literal). User klik manual at 59:55 yang sampai server at 60:05 → tier-1 reject. User di-redirect ke StartExam dengan banner explanatory (D-01). Auto-submit handler sudah fire di client at countdown=0, jadi jawaban tetap masuk via auto-submit. Defense-in-depth: 2 path (manual + auto), strict guard di manual aman karena auto fallback. Tidak relax grace untuk manual (akan defeat purpose phase).

### Network failure recovery
- **D-10:** **Client retry 3x dengan backoff (1s, 2s, 4s).** Auto-submit POST gagal (timeout / 5xx) → retry sampai 3x dengan exponential backoff. Total ~7 detik recovery window. Implementation di JS handler `StartExam.cshtml`. Logging error di console + telemetry.
- **D-11:** **Banner-only fallback (server-side save deferred).** Kalau retry 3x semua fail dan grace habis → tampil banner permanent di StartExam: `"Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser."` + display draft answers from local state (existing client form data). Server-side last-resort save (auto-save endpoint) deferred ke milestone v16.0+ — out of scope phase 313.

### Page reload behavior
- **D-12:** **Recompute timer dari server StartedAt.** Frontend di page load hit endpoint atau read inline data: remaining = `(StartedAt + Duration + ExtraTime) - NOW`. Resume countdown dari nilai aktual. Server adalah source-of-truth. Tidak rely on localStorage (rentan tampering).
- **D-13:** **Reload after timeup → button disabled + banner info.** Kalau saat reload server elapsed > allowed:
  - Page render dengan Submit button disabled (label: "Waktu Habis")
  - Banner info (bukan error): `"Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan."`
  - Frontend TIDAK fire auto-submit lagi (asumsi sudah dikirim sebelum reload, idempotency). Backend tier-2 reject duplicate kalau ada.
  - Tidak auto-redirect ke ExamSummary — user lihat status jelas dulu.

### 3 timer type consistency
- **D-14:** **Pesan, redirect, ActionType sama persis untuk Online/PreTest/PostTest.** Type info di-embed di Description metadata AuditLog (`Type=PreTest`), bukan di ActionType. Simple, DRY, audit query masih bisa filter via Description. Tidak ada redirect khusus per type (mis. PreTest → PostTest landing) — itu out-of-scope dan complicate logic.
- **D-15:** **Manual type exclude via explicit Type field check.** Tier-1 + Tier-2 guard wrap dalam:
  ```csharp
  if (assessment.Type == "Online" || assessment.Type == "PreTest" || assessment.Type == "PostTest") {
    // existing 2-tier guard
  }
  ```
  Defense-in-depth — jangan rely on StartedAt null sebagai implicit exclude (asumsi rentan jika Manual type pernah set StartedAt by mistake). Verify dengan unit test (input Manual type + elapsed > allowed → no reject).

### Claude's Discretion
- **Tooltip wording detail** untuk disabled button — D-03 spec hanya outline, exact wording bisa adjust untuk fit existing UI tone.
- **Spinner/icon visual** di disabled button label — D-03 mention "spinner kecil" tapi tidak mandate. Implementer pilih sesuai design system existing.
- **Banner styling** untuk reload after timeup (D-13) dan retry-fail fallback (D-11) — Bootstrap alert-warning vs alert-info, exact icon, dll.
- **Test fixture seed script** lokasi & format — bisa SQL script di `.planning/seeds/` atau Playwright setup helper, implementer pilih.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Codebase (existing logic to modify/preserve)
- `Controllers/CMPController.cs` — line 1556-1628 (SubmitExam method + LIFE-03 block) — primary modification target
- `Views/CMP/StartExam.cshtml` — frontend countdown timer + Submit button + auto-submit handler — secondary modification target
- `Models/AssessmentSession.cs` — `StartedAt`, `DurationMinutes`, `ExtraTimeMinutes`, `Type` fields used in guard
- `Services/AuditLogService.cs` (atau equivalent) — AuditLog write API used in Phase 312, mengikuti pattern yang sama
- `tests/e2e/exam-taking.spec.ts` — existing FLOW pattern reference untuk SubmitExam scenarios (kalau ada)
- `tests/helpers/accounts.ts` — login fixture untuk worker role testing

### Phase 312 patterns (carry-forward)
- `.planning/phases/312-admin-full-delete-assessment-room/312-CONTEXT.md` — D-03 (AuditLog blocked entry pattern), D-04 (scope decision pattern)
- `.planning/phases/312-admin-full-delete-assessment-room/312-01-SUMMARY.md` — `EnsureCanDeleteAsync` helper pattern (analog for `EnsureCanSubmitAsync` di phase 313 jika perlu)

### Project & requirements
- `.planning/REQUIREMENTS.md` — TMR-01 acceptance criteria
- `.planning/ROADMAP.md` — Phase 313 success criteria 1-7
- `CLAUDE.md` — Pertamina dev workflow (lokal → Dev → Prod), dotnet build verify
- `docs/DEV_WORKFLOW.md` — environment map + migration SOP

### Audit context
- Audit-29Apr T2 — root issue triggering TMR-01 (manual submit setelah waktu habis bypass server enforcement)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **TempData["Error"] + RedirectToAction("StartExam", new { id })** — existing LIFE-03 tier-2 pattern (line 1625-1626). Reuse for tier-1 dengan pesan berbeda (D-01).
- **AuditLog write pattern** — Phase 312 established `_auditLog.LogAsync(action, description, ...)` helper. Reuse untuk `SubmitExamBlocked` entry (D-05).
- **`EnsureCanDeleteAsync` helper pattern** (Phase 312) — bisa dijadikan template untuk `EnsureCanSubmitAsync` jika tier guard logic complex enough untuk extract.
- **Server time `DateTime.UtcNow`** — existing usage di line 1621. Konsisten, no need IDateTimeProvider.
- **Existing auto-submit handler** di `StartExam.cshtml` (need to read) — preserve, hanya add disable button + retry logic.

### Established Patterns
- **Defense-in-depth via UI hide + backend block** (Phase 312 D-01 + D-03 pattern). Phase 313 apply: UI disable button (D-03) + backend tier-1 strict reject (D-09).
- **TempData error → RedirectToAction → user flash banner** — used across CMPController + AdminController. Konsisten.
- **AuditLog ActionType `{Action}Blocked` naming** — Phase 312 D-03 pattern. D-05 mengikuti.
- **Test fixture title pattern (Phase 312 WR-04)** — used pattern di tests/e2e. D-08 mengikuti.

### Integration Points
- **`SubmitExam()` LIFE-03 block** (line 1616-1628) — single modification site untuk 2-tier logic.
- **`StartExam.cshtml` countdown JS** — modification site untuk button disable + auto-submit retry.
- **AuditLogs table schema** — no schema change needed (existing ActionType varchar + Description varchar fits new entries).

### Patterns to Avoid (anti-patterns from Phase 312 review)
- **TOCTOU race** (Phase 312 WR-01) — kalau guard logic butuh re-fetch + re-check, wrap in `BeginTransactionAsync`. Untuk Phase 313 tier guard simple (timer arithmetic from in-memory `assessment` object), tidak perlu transaction extra di guard itu sendiri (existing transactional context dari grading flow sudah cover write path).
- **Path-prefix bug `appUrl()`** (Phase 312 WR-02) — kalau frontend perlu URL injection (mis. retry endpoint), pakai `data-url` attribute via `@Url.Action`. JANGAN hardcode atau pakai relative path tanpa base.
- **Playwright selector substring match** (Phase 312 WR-03) — gunakan badge-scoped selector atau test-id. Hindari `tr:has-text("X")` kalau text bisa substring di kolom lain.

</code_context>

<specifics>
## Specific Ideas

- **Server-side log alasan** — User mention via D-05: format Description `"HC/User role manual submit blocked after timeup. Type={...} ElapsedMin={X} AllowedMin={Y} SessionId={id}"` — strukturnya jelas ada parsing-friendly key=value.
- **Banner permanen retry-fail (D-11)** — wording "Jawaban Anda tersimpan di tab ini, jangan tutup browser" eksplisit untuk prevent user panic action (close browser → lose state).
- **Idempotency via duplicate detection di server** — saat reload setelah auto-submit fire (D-13), kalau second auto-submit POST datang, server tier-2 reject (existing behavior). No new logic needed.

</specifics>

<deferred>
## Deferred Ideas

- **Server-side last-resort save** (D-11 alternative) — auto-save endpoint untuk partial responses, frontend periodic sync tiap N detik. Butuh schema/table additions atau modify existing PackageUserResponses untuk support draft state. Defer ke milestone v16.0+ jika prioritas.
- **`IDateTimeProvider` mockable injection** untuk SubmitExam — clean refactor untuk testability. Out-of-scope phase 313 (DB manipulation StartedAt back-dated sudah cukup untuk E2E).
- **PreTest → PostTest landing redirect** (Area 8 alternative) — kalau type-specific redirect dibutuhkan, jadi sub-phase di milestone berikut.
- **AuditLog success entry dengan IsAutoSubmit field** (D-06 alternative) — traceability detail submit manual vs auto. Bisa tambah di milestone v16.0+ kalau audit query butuh distinct.

</deferred>

---

<post_research_corrections>
## Post-Research Corrections (2026-05-08)

Setelah `gsd-phase-researcher` membaca codebase aktual, ada 3 koreksi yang **OVERRIDE** keputusan di atas. Planner & executor MUST honor section ini di atas D-XX original kalau ada konflik.

### C-01: Field name `assessment.Type` → `assessment.AssessmentType`
**Override:** D-15 dan semua referensi `assessment.Type` di CONTEXT.md.
**Source-of-truth:** `Models/AssessmentSession.cs:154` field name adalah `AssessmentType`. Constants tersedia di `Models/AssessmentConstants.cs::AssessmentType.{Online,PreTest,PostTest,Manual}`.
**Apply:** Pakai constants, jangan magic string. Tier guard jadi:
```csharp
if (assessment.AssessmentType == AssessmentType.Online ||
    assessment.AssessmentType == AssessmentType.PreTest ||
    assessment.AssessmentType == AssessmentType.PostTest) {
    // 2-tier guard
}
```

### C-02: Submit button utama di `Views/CMP/ExamSummary.cshtml`, bukan StartExam.cshtml saja
**Override:** D-03, D-13 file targets.
**Source-of-truth:** `StartExam.cshtml` examForm submit ke `ExamSummary` (review page) line 71 — bukan langsung ke SubmitExam. Submit button utama dengan POST ke SubmitExam ada di `Views/CMP/ExamSummary.cshtml:139-143`. Hidden field `isAutoSubmit` sudah ada di ExamSummary.cshtml line 110 (tied ke `ViewBag.TimerExpired`).
**Apply:** D-03 disable button UI scope = **PRIMARILY** `ExamSummary.cshtml` Submit button. `StartExam.cshtml` modify hanya untuk timer auto-submit modal flow (per C-03).

### C-03: D-04 adjustment — popup TETAP MUNCUL + submit paralel langsung
**Override:** D-04 "auto-submit fire immediate at countdown=0" original.
**User decision (2026-05-08):** Popup notifikasi `timeUpWarningModal` (existing di StartExam.cshtml line 377-391) **tetap muncul** untuk awareness, **TAPI** POST submit fire **langsung paralel** (tidak nunggu klik OK, tidak ada `setTimeout(10000)` delay).
**Apply:**
- Modal text adjust jadi info-only: header "Waktu Habis!", body "Jawaban Anda sedang dikirim otomatis...", **hapus button "OK — Kirim Jawaban"** (atau replace dengan disabled spinner indicator).
- JS code di `StartExam.cshtml` line 462-478 modify: hapus `setTimeout(10000)`, fire `examForm.submit()` immediate setelah `timeupModal.show()` (atau invoke paralel via async pattern).
- Modal `data-bs-backdrop="static"` preserved — user tidak bisa close modal sampai redirect ke ExamSummary selesai.
- Server grace 2min (tier-2) tetap intact untuk cover network latency.

**Visual flow:**
```
Timer 00:00 → modal muncul ("Waktu Habis! Jawaban sedang dikirim...") → POST fire paralel → server respond → redirect ExamSummary
```

</post_research_corrections>

---

*Phase: 313-block-manual-submit-saat-waktu-habis*
*Context gathered: 2026-05-07*
*Post-research corrections: 2026-05-08 (C-01, C-02, C-03)*
