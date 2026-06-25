---
phase: 420-editquestion-identity-based-option-editing
verified: 2026-06-25T00:00:00Z
status: passed
score: 6/6
resolved_note: "Human-verification leg (VRF-01 Playwright real-browser) executed in Plan 03 and PASSED 3/3 DB-verified — see 420-UAT.md (status: passed). All 3 human_verification items below confirmed. Status flipped human_needed → passed at v32.9 milestone close 2026-06-25."
overrides_applied: 0
human_verification_resolved: true
human_verification:
  - test: "Jalankan Playwright UAT real-browser: buka ManagePackageQuestions, edit soal MC 4-opsi yang sudah dijawab peserta di posisi tengah (B), hapus baris B, klik Simpan. Assert: pesan error memuat 'sudah dijawab' dan 'B'; DB opsi B masih ada."
    expected: "Alert/TempData error muncul dengan wording guard; soal tetap 4 opsi di DB."
    why_human: "Lesson 354: Razor+JS DOM (hidden Id carrier, reletter, clone-reset) hanya dapat diverifikasi di browser asli; controller integration tests tidak bisa menguji reindex client-side."
  - test: "Playwright: edit soal MC 4-opsi BELUM dijawab, hapus opsi tengah B, Simpan. Reload form edit, assert teks opsi = A, C, D (C TIDAK ter-relabel jadi B)."
    expected: "Sukses; 3 opsi tersisa; teks opsi C tetap 'C' (no silent relabel)."
    why_human: "Verifikasi no-relabel di UI live membuktikan hidden Id melewati JS reindex dengan benar sampai ke controller."
  - test: "Playwright: edit soal MC 4-opsi, klik 'Tambah Opsi', isi teks 'E', set salah satu benar, klik Simpan. Assert 5 opsi tersimpan; opsi A teks tetap 'A' (TIDAK ter-overwrite oleh baris baru)."
    expected: "5 PackageOption di DB; opsi A.Id stabil teks 'A'; opsi baru teks 'E'."
    why_human: "Skenario ini membuktikan GOTCHA §2c (clone-reset hidden Id) berjalan di DOM nyata — sangat kritis dan harus ditangkap Playwright (analog lesson 413 ReferenceError yang hanya ketahuan di browser)."
---

# Phase 420: EditQuestion Identity-Based Option Editing — Verification Report

**Phase Goal:** Ganti upsert opsi EditQuestion POST dari posisional ke identity-based (match PackageOption by stable Id), sehingga hapus/edit opsi termasuk tengah pada soal terjawab tidak me-relabel jawaban peserta senyap; guard answered-option (D-418-02) menyala untuk delete posisi manapun; regression-lock 999.14 dipertahankan. Migration=FALSE.
**Verified:** 2026-06-25
**Status:** human_needed — semua 5 SC automated terverifikasi oleh kode + 12/12 integration test (SQLEXPRESS); SC#5 VRF-01 sebagian (integration pass, Playwright UAT belum dijalankan — autonomous:false direncanakan di Plan 03 Task 2).
**Re-verification:** No — initial verification.

---

## Goal Achievement

### Observable Truths (Success Criteria)

| # | SC / REQ | Truth | Status | Evidence |
|---|----------|-------|--------|----------|
| 1 | SC#1 OPTEDIT-01 | Hapus opsi tengah BELUM dijawab → record yang benar terhapus, opsi tersisa tidak ter-relabel | VERIFIED | `IdentityEdit_MiddleDelete_Unanswered_NoRelabel_Succeeds`: DB 3 opsi, C (optionIds[2]) teks masih "C" bukan "B"; identity upsert konfirmasi via `keptIds.Contains(o.Id)` di AAC:8141 |
| 2 | SC#2 OPTEDIT-02 | Hapus opsi SUDAH dijawab (posisi MANAPUN, termasuk tengah) → ditolak dengan pesan ramah menyebut huruf + cuplikan teks | VERIFIED | `EditShrinkGuard_AnsweredOption_NotRemoved_NoException` (ported TEST1): B omitted from submit → removedOptionIds={B} via set-difference → guard fires; pesan menyebut "sudah dijawab" + "B"; controller AAC:8069 `OptionShrinkGuard.FindBlockedOptionIds` reused, AAC:8086 D-04 letter+snippet |
| 3 | SC#3 OPTEDIT-03 | Edit teks/kebenaran opsi terjawab → UPDATE record by Id; jawaban peserta tetap merujuk opsi yang sama | VERIFIED | `IdentityEdit_EditAnsweredOption_TextAndCorrectness_UpdatesById`: opsi B (optionIds[1]) teks "B-rev", IsCorrect=true; PackageUserResponse.PackageOptionId masih optionIds[1]; controller path `keptIds.Contains(o.Id) → UPDATE` AAC:8141-8146 |
| 4 | SC#4 OPTEDIT-04 | Konversi MC/MA→Essay + shrink opsi terjawab → ditolak tanpa error 500 (regression-lock 999.14) | VERIFIED | `IdentityEdit_ConvertAnsweredMcToEssay_Blocked_NoException`: `Record.ExceptionAsync==null` (no FK-Restrict 500); Essay branch: removedOptionIds=semua existing → guard menyala → blocked; soal tetap MultipleChoice 4 opsi di DB |
| 5 | SC#5 OPTEDIT-05 + VRF-01 (partial) | CreateQuestion + edit soal belum-dijawab + import Excel tetap normal; integration test mereproduksi relabel-senyap lalu membuktikan diblokir; Playwright UAT real-browser PASS | PARTIAL | Integration: `EditShrinkGuard_UnansweredOption_Removed_Succeeds` (TEST2), `IdentityEdit_AddOption_NullId_Adds_NotOverwriteExisting`, `IdentityEdit_AntiTamper_ForeignOptionId_Rejected_NoMutation`, `IdentityEdit_DuplicateSubmittedId_Rejected` — 12/12 integration green; CreateQuestion tidak disentuh kode (grep konfirmasi). **Playwright UAT: belum dijalankan (autonomous:false, Plan 03 Task 2 pending — spec file tests/e2e/identity-option-edit-420.spec.ts BELUM DIBUAT).** |

**Score:** 5/6 truths verified (SC#5 VRF-01 partial — integration pass, Playwright pending)

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/OptionInput.cs` | Carrier identity `int? Id`; komentar T-418-06 direvisi | VERIFIED | `public int? Id { get; set; }` line 29; komentar T-418-06 larangan dicabut, diganti kontrak validasi-server; "JANGAN tambah properti Id" = 0 match |
| `Controllers/AssessmentAdminController.cs` | GET emit `id = o.Id`; POST anti-tamper + set-difference + upsert identity; loop posisional dihapus | VERIFIED | `id = o.Id` line 7910; `existingIds.Except(keptIds)` line 8059; `!existingIds.Contains(id)` line 8037; `keptIds.Contains(o.Id)` line 8141; `OptionShrinkGuard.FindBlockedOptionIds` line 8069; `for (int i = 0; i < bound` = 0 match (loop posisional removed) |
| `Views/Admin/ManagePackageQuestions.cshtml` | Hidden `options[i].Id` per baris + reletterRows rename + populateEditForm isi dari opt.id + addOptionRow clone-reset hapus hidden | VERIFIED | `opt-id-input` lines 404 (template) + 791 (querySelector); `options[@i].Id` line 404; `idInput.name = 'options[' + i + '].Id'` line 793; `(opt.id != null)` line 720; `inp.type === 'hidden'` line 857 |
| `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | TEST1/TEST2 di-port ke identity contract + 6 test identity baru | VERIFIED | 8 [Fact] total (2 ported + 6 new identity tests); semua menggunakan `Id = optionIds[i]` pattern; class memiliki semua 6 named tests (MiddleDelete_Unanswered, EditAnswered, ConvertToEssay, AntiTamper, AddOption, DuplicateId) |
| `tests/e2e/identity-option-edit-420.spec.ts` | Playwright UAT real-browser 3 skenario | MISSING | File belum dibuat (ls konfirmasi NOT FOUND). Plan 03 Task 2 autonomous:false — direncanakan tapi belum dieksekusi. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| EditQuestion GET JSON options projection | Client hidden `options[i].Id` | `id = o.Id` field di anonymous object | WIRED | AAC:7910 confirmed; JS `idEl.value = (opt.id != null) ? String(opt.id) : ''` (view line 720) reads `opt.id` |
| EditQuestion POST submitted Id | `OptionShrinkGuard.FindBlockedOptionIds` | `removedOptionIds = existingIds.Except(keptIds)` | WIRED | AAC:8059 set-difference; AAC:8069 guard call; kill-drift: guard + upsert share same `keptIds`/`removedOptionIds` |
| EditQuestion POST anti-tamper | Fail-closed reject pre-mutation | `submittedIds.Any(id => !existingIds.Contains(id))` | WIRED | AAC:8037-8041; reject SEBELUM `SaveChangesAsync` (AAC:8165) |
| reletterRows() JS rename | hidden name `options[i].Id` | `idInput.name = 'options[' + i + '].Id'` | WIRED | View line 793-794; value preserved (reletter hanya rename) |
| addOptionRow clone | hidden Id cleared | `if (inp.type === 'hidden') inp.value = ''` | WIRED | View line 857; mencegah clone mewarisi Id baris[0] |
| populateEditForm GET JSON | hidden Id per baris terisi | `idEl.value = (opt.id != null) ? String(opt.id) : ''` | WIRED | View line 720 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| EditQuestion POST guard | `removedOptionIds` | `existingIds.Except(keptIds)` (set-difference by Id dari DB options) | DB query `_context.PackageUserResponses.Where(...)` AAC:8064 | FLOWING |
| EditQuestion POST upsert | `existing`, `keptIds`, `newRows` | `q.Options.OrderBy(o => o.Id)` (EF DB query) + submitted `options` list | Real DB rows via EF Include + form binding | FLOWING |
| EditQuestion GET JSON | `options` array (includes `id = o.Id`) | `q.Options.OrderBy(o => o.Id).Select(...)` via DB | Real PackageOption records | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Method | Result | Status |
|----------|--------|--------|--------|
| Loop posisional dihapus | `grep -c "for (int i = 0; i < bound"` AAC | 0 matches | PASS |
| Identity guard wired | `grep "existingIds.Except(keptIds)"` AAC | Found line 8059 | PASS |
| Anti-tamper wired | `grep "!existingIds.Contains(id)"` AAC | Found line 8037 | PASS |
| Guard reused | `grep "OptionShrinkGuard.FindBlockedOptionIds"` AAC | Found line 8069 (1 match) | PASS |
| View hidden carrier | `grep "opt-id-input"` view | Lines 404 + 791 | PASS |
| Clone-reset gotcha | `grep "inp.type === 'hidden'"` view | Line 857 | PASS |
| Playwright spec exists | `ls tests/e2e/identity-option-edit-420.spec.ts` | NOT FOUND | SKIP (pending) |
| Integration tests (12/12) | `dotnet test --filter EditShrinkGuard` (per SUMMARY.md evidence) | 12/12 PASS SQLEXPRESS | PASS (claimed; code matches test bodies) |

Step 7b: Spot-check untuk build/test tidak dapat dijalankan secara langsung dari verifier (memerlukan SQLEXPRESS live), namun kode test dan implementasi controller diverifikasi struktural secara menyeluruh.

---

### Requirements Coverage

| REQ | Phase Plan | Description | Status | Evidence |
|-----|-----------|-------------|--------|----------|
| OPTEDIT-01 | Plan 01 | Hapus opsi posisi manapun (belum dijawab) → record tepat terhapus, tak ter-relabel | SATISFIED | `IdentityEdit_MiddleDelete_Unanswered_NoRelabel_Succeeds` green; upsert identity remove by Id |
| OPTEDIT-02 | Plan 01 | Hapus opsi sudah dijawab (posisi manapun) → diblokir pesan ramah | SATISFIED | `EditShrinkGuard_AnsweredOption_NotRemoved_NoException` (ported TEST1) green; guard set-difference |
| OPTEDIT-03 | Plan 01 | Edit teks/kebenaran opsi terjawab → UPDATE by Id; jawaban peserta semantik utuh | SATISFIED | `IdentityEdit_EditAnsweredOption_TextAndCorrectness_UpdatesById` green |
| OPTEDIT-04 | Plan 01 | Konversi MC→Essay + shrink terjawab → ditolak, no 500 (regression-lock 999.14) | SATISFIED | `IdentityEdit_ConvertAnsweredMcToEssay_Blocked_NoException` green; FK-Restrict tak ter-trip |
| OPTEDIT-05 | Plan 01 + 02 | Alur existing (CreateQuestion, edit belum-dijawab, import Excel) tak regresi; form authoring identity-based | SATISFIED (automated) | `AddOption_NullId_Adds_NotOverwriteExisting`, `UnansweredOption_Removed_Succeeds`, `AntiTamper_*`, `DuplicateId_Rejected` green; CreateQuestion tak disentuh (grep); view carrier + clone-reset implemented |
| VRF-01 | Plan 03 | Integration test reproduksi relabel-senyap + Playwright UAT real-browser | PARTIAL | Integration 12/12 green membuktikan relabel-senyap kini diblokir; **Playwright spec belum dibuat/dijalankan** |

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` | Test comment header masih menyebut mekanisme posisional ("Mekanika shrink: kontrak loop upsert + guard memakai aturan index-aligned OrderBy(Id)... posisi i dihapus bila i>=keep ATAU options[i].Text kosong") di TEST1 header (line 186-188) | Info | Komentar stale dari sebelum port ke identity; tidak mempengaruhi fungsionalitas (kode actual sudah identity-based); cosmetic only |

Tidak ditemukan: `TODO/FIXME/PLACEHOLDER` blocker, `return null/return {}` stub, hardcoded empty data yang flows ke rendering, anti-tamper bypass.

---

### Human Verification Required

#### 1. Delete Middle Answered — Pesan Ramah di Browser

**Test:** App running @5277. Seed (SEED_WORKFLOW): 1 sesi + paket + soal MC 4-opsi (A,B,C,D) + PackageUserResponse untuk opsi B (tengah). Buka ManagePackageQuestions paket itu → klik edit soal → di form edit klik tombol hapus baris opsi B (tengah) → klik Simpan.
**Expected:** Alert/TempData error muncul memuat "sudah dijawab" dan "B" (dengan cuplikan teks opsi B). DB: soal masih 4 opsi, B masih ada, response peserta utuh.
**Why human:** Lesson 354 — Razor+JS DOM behavior (hidden Id carrier via `populateEditForm`, reletter via `reletterRows`, submit via hidden `options[i].Id`) harus diverifikasi di browser nyata; controller integration tests tidak bisa menguji reindex client-side.

#### 2. Delete Middle Unanswered — No Relabel di Browser

**Test:** Soal MC 4-opsi BELUM dijawab. Hapus baris opsi tengah B di form edit → Simpan → buka form edit lagi.
**Expected:** Sukses; form edit menampilkan 3 opsi dengan teks A, C, D — C TIDAK ter-relabel jadi "B".
**Why human:** Membuktikan hidden Id bertahan melewati JS reindex (reletterRows) saat hapus baris dari DOM, sehingga C (Id stabil) tetap terikat ke opsi aslinya di submit.

#### 3. Add Option — No Overwrite Opsi A (GOTCHA §2c) di Browser

**Test:** Edit soal MC 4-opsi → klik "Tambah Opsi" → isi teks "E" → set salah satu benar valid → klik Simpan.
**Expected:** 5 opsi tersimpan; opsi A teks tetap "A" (tidak ter-overwrite oleh opsi baru yang mewarisi Id dari clone). Verifikasi SQL: PackageOption A.Id stabil teks "A"; opsi baru teks "E".
**Why human:** GOTCHA §2c (clone-reset hidden Id) adalah risiko implementasi tertinggi. `if (inp.type === 'hidden') inp.value = ''` ada di view (line 857), tapi efektivitasnya di DOM live HARUS dibuktikan Playwright (analog lesson 413 — runtime-only bug yang lolos grep/controller test).

---

### Gaps Summary

Tidak ada gap blocker terhadap goal delivery. Satu item outstanding:

**VRF-01 Playwright UAT (planned, not yet run):** File `tests/e2e/identity-option-edit-420.spec.ts` belum dibuat. Plan 03 Task 2 ditandai `autonomous: false` — memerlukan app live @5277 + SEED_WORKFLOW snapshot/restore. Ini bukan kegagalan implementasi; ini adalah gerbang verifikasi yang sengaja dijadwalkan untuk UAT manual. Seluruh behavior sudah dibuktikan oleh 12/12 integration test; Playwright mengunci 3 skenario yang tidak bisa dijangkau controller test (reindex DOM, clone gotcha).

Status: **human_needed** — kode + integration tests membuktikan goal tercapai secara teknis; Playwright UAT 3 skenario diperlukan sebelum fase bisa dinyatakan fully passed.

---

## VERIFICATION PASSED (conditional)

Kode yang dikirim (Plans 01 + 02) secara teknis **mencapai goal fase**: upsert posisional diganti identity-based dengan benar, anti-tamper fail-closed, guard set-difference menyala untuk posisi manapun, regression-lock 999.14 terjaga, dan alur existing tidak regresi — **dibuktikan oleh 12/12 integration test (SQLEXPRESS) yang mereproduksi bug 999.15 dan membuktikan kini diblokir**.

Satu-satunya yang tersisa adalah **Playwright UAT real-browser** (Plan 03 Task 2, autonomous:false) yang memverifikasi behavior Razor+JS DOM di browser nyata — sesuai pola wajib lesson 354. Ini adalah planned gate, bukan gap implementasi.

**Langkah selanjutnya:** Jalankan Plan 03 Task 2 (Playwright UAT 3 skenario + SEED_WORKFLOW + tulis 420-UAT.md). Setelah 3/3 PASS, fase dapat ditutup.

---

_Verified: 2026-06-25_
_Verifier: Claude (gsd-verifier)_
