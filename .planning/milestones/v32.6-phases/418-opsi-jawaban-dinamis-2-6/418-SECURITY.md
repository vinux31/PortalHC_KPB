---
phase: 418-opsi-jawaban-dinamis-2-6
secured_at: 2026-06-24
asvs_level: 1
block_on: open
threats_total: 16
threats_closed: 16
threats_open: 0
status: SECURED
---

# Phase 418: Laporan Keamanan — Opsi Jawaban Dinamis 2–6

**Diverifikasi:** 2026-06-24
**Metode:** Verifikasi retroaktif — setiap ancaman `<threat_model>` (T-418-01..16) dicocokkan dengan kode terimplementasi (bukan scan buta).
**ASVS Level:** 1
**Verdict:** **SECURED** — 16/16 ancaman tertutup (mitigated). `threats_open: 0`.

## Ringkasan

Fase 418 me-refactor kontrak HTTP authoring soal dari 16 parameter diskret A–D menjadi binding `List<OptionInput>` (2–6 opsi dinamis) + render huruf A–F di 5 view. Seluruh 16 ancaman STRIDE yang dideklarasikan di empat plan (`mitigate` semua) **terbukti hadir di kode**. Tidak ada ancaman terbuka, tidak ada flag tak-terdaftar.

**Temuan kritis yang dikonfirmasi DITUTUP:** Plan 418-02 mengidentifikasi bahwa atribut `[HttpPost]` + `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` sebelumnya menempel pada action yang salah, meninggalkan `CreateQuestion` POST tanpa CSRF/authz. Verifikasi memastikan ketiga atribut kini menempel **langsung** di atas deklarasi `CreateQuestion` (baris 7708-7711) dan `EditQuestion` (baris 7927-7930). Lubang pre-existing TERTUTUP.

**WR-01 / backlog 999.15 (relabel opsi-tengah):** dikarakterisasi ulang dan dikonfirmasi sebagai **batasan terdokumentasi pre-existing**, BUKAN ancaman baru yang diperkenalkan 418 (lihat catatan di akhir). Bukan FK-Restrict 500 (tidak crash); konsekuensi sah dari upsert-posisional yang dikunci spec (RESEARCH Pattern 2).

## Tabel Verifikasi Ancaman (STRIDE)

| Threat ID | Kategori | Disposisi | Verdikt | Bukti (file:line) |
|-----------|----------|-----------|---------|-------------------|
| T-418-01 | Tampering | mitigate | **mitigated** | `Helpers/QuestionOptionValidator.cs:31-32` — `if (filled > 6) return (false, "Maksimal 6 opsi per soal.")` server-side; dikunci `OptionValidationTests` (Fact MaxSix). |
| T-418-02 | DoS / Tampering | mitigate | **mitigated** | `Helpers/OptionShrinkGuard.cs:30-34` — `FindBlockedOptionIds` irisan set murni; dikunci `EditShrinkGuardLogicTests` (4 Fact). |
| T-418-03 | Tampering (CSRF) | mitigate | **mitigated** | `Controllers/AssessmentAdminController.cs:7710` (CreateQuestion) + `:7929` (EditQuestion) — `[ValidateAntiForgeryToken]`; `@Html.AntiForgeryToken()` di form `ManagePackageQuestions.cshtml:328`. **Lubang pre-existing tertutup.** |
| T-418-04 | Tampering (file upload V12) | mitigate | **mitigated** | `AssessmentAdminController.cs:7738-7746` (Create) + `:7958-7966` (Edit) — loop `new[]{questionImage}.Concat(options.Select(o=>o.Image))` → `FileUploadHelper.ValidateImageFile(f)` fail-fast, mencakup E/F; tidak hand-rolled. |
| T-418-05 | DoS / Tampering (FK-Restrict) | mitigate | **mitigated** | `AssessmentAdminController.cs:8040-8079` — guard edit-shrink query `PackageUserResponses` + `OptionShrinkGuard.FindBlockedOptionIds` SEBELUM SaveChanges → `TempData["Error"]` + redirect (bukan 500). Tutup hazard 999.14. |
| T-418-06 | Tampering / Elevation (mass-assignment) | mitigate | **mitigated** | `Models/OptionInput.cs:17-33` — whitelist eksplisit 5 properti (Text/IsCorrect/Image/ImageAlt/RemoveImage), **TIDAK ada `Id`**. Id PackageOption ditentukan server via `existing[i]` (`:8041`). |
| T-418-07 | Elevation / Info Disclosure (IDOR) | mitigate | **mitigated** | `AssessmentAdminController.cs:7754-7763` (Create) + `:7969-7978` (Edit) — `AssessmentPackageSections.AnyAsync(s => s.Id == sectionId && s.AssessmentPackageId == packageId)`; packageId scoping utuh. |
| T-418-08 | Spoofing / Elevation (authz) | mitigate | **mitigated** | `AssessmentAdminController.cs:7709` (Create) + `:7928` (Edit) — `[Authorize(Roles="Admin, HC")]` langsung di atas action. |
| T-418-09 | Tampering (bypass max-6) | mitigate | **mitigated** | Validator `QuestionOptionValidator.cs:31` (server-authoritative) + persist `options.Take(6)` defensif `AssessmentAdminController.cs:7827`. |
| T-418-10 | Tampering (XSS OptionText) | mitigate | **mitigated** | Razor auto-encode: `StartExam.cshtml:158/191`, `Results.cshtml:388`, `ExamSummary.cshtml`, `_PreviewQuestion.cshtml:64`, `PreviewPackage.cshtml:63` — semua `@option.OptionText`/`@opt.OptionText`, TANPA `Html.Raw`. JS inject pakai `.textContent`/`createTextNode` (`InjectAssessment.cshtml:943/1012`, `ManagePackageQuestions.cshtml:776`). |
| T-418-11 | Tampering (antiforgery hilang via JS) | mitigate | **mitigated** | Manipulasi baris dilakukan di DALAM `#questionForm` existing (`reletterRows`/`addOptionRow` ManagePackageQuestions.cshtml:769+); token `@Html.AntiForgeryToken()` (:328) tetap; tidak ada form baru dibangun via JS. |
| T-418-12 | Tampering (bypass max-6 dari client) | accept | **accepted** | Disposisi rencana = `accept` (client addBtn disabled@6 = nicety UX); otoritas sebenarnya = validator server max-6 (T-418-09, `QuestionOptionValidator.cs:31`). Server-authoritative — accepted-by-design. |
| T-418-13 | Info Disclosure (reasosiasi gambar) | mitigate | **mitigated** | `ManagePackageQuestions.cshtml:806-819` — blok gambar terikat node baris; `reletterRows` rename id prefix `opt{letter}` + name index-based saat hapus baris; e2e flag#4 (`option-dynamic-418.spec.ts`). |
| T-418-14 | DoS (FK-Restrict 500) | mitigate | **mitigated** | Sama dgn T-418-05 (`:8040-8079`) + dibuktikan runtime `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` (real-SQL, answered→no-DbUpdateException+preserved, unanswered→removed) + e2e edit-shrink-blocked. |
| T-418-15 | Tampering (single-select MC rusak) | mitigate | **mitigated** | Radio MC satu name grup `name="correctIndex"` (`ManagePackageQuestions.cshtml:407`; `reletterRows` :795-796 set value=index); server `ResolveCorrectness` (`AssessmentAdminController.cs:7698-7703`) override `IsCorrect=(correctIndex==i)`; e2e + UAT langkah 2. |
| T-418-16 | Info Disclosure (gambar salah-baris) | mitigate | **mitigated** | Sama dgn T-418-13 (hapus node baris utuh + reletter, `ManagePackageQuestions.cshtml:806-819`) + e2e imageReassoc flag#4. |

## Flag Tak-Terdaftar (Unregistered Flags)

**Tidak ada.** Ketiga SUMMARY (`418-02/03/04`) mencantumkan `## Threat Flags: None` secara eksplisit — semua surface keamanan baru sudah tercakup di register `<threat_model>` plan (T-418-03..16) dan termitigasi. Tidak ditemukan attack-surface tak-terpetakan.

## Verifikasi Defensif Tambahan (di luar register, dikonfirmasi utuh)

- **Guard H3 DIHAPUS dengan benar:** grep `q.Options.Count > 4` / `menyusul` / `belum dapat diedit` di controller → 0 hasil. Komentar penjelas di `:7985-7987`. Soal 5–6 opsi import (415) kini editable tanpa membuka lubang (guard edit-shrink yang melindungi, bukan blanket-block).
- **GET JSON shape utuh:** `AssessmentAdminController.cs:7912-7918` — `options = q.Options.OrderBy(o => o.Id).Select(...)` emit `optionText/isCorrect/imagePath/imageAlt`; **tidak membocorkan `Id`** ke client (konsisten dgn whitelist OptionInput, mendukung T-418-06).
- **Whitelist QuestionType + bounds scoreValue 1–100:** dipertahankan di kedua POST (`:7729-7734`/`:7766-7770` Create; `:7949-7954`/`:7990-7994` Edit) — V5 ASVS.
- **TruncateAlt(255):** dipertahankan untuk alt soal + alt opsi (`:7819`, `:7838`) — tidak hilang saat refactor.
- **a11y konsistensi:** `#authError` (ManagePackageQuestions:396) + `#injAuthError` (_InjectQuestionForm:88) keduanya `role="alert"` (IN-03 fix terverifikasi).

## Catatan: WR-01 / Backlog 999.15 (batasan terdokumentasi, BUKAN ancaman terbuka)

Reviewer (418-REVIEW.md) menandai 1 Warning: penghapusan opsi di **tengah** yang sudah dijawab tidak diblokir guard — record-Id bertahan tapi teksnya di-relabel diam-diam (jawaban peserta dapat berubah makna secara senyap). **Karakterisasi sebagai pre-existing dikonfirmasi AKURAT:**

1. **Bukan ancaman baru 418.** Mekanisme upsert-posisional (preserve `PackageOption.Id` by posisi `OrderBy Id`, RESEARCH Pattern 2) sudah ada sejak soal 4-opsi. 418 hanya menggeneralisasi loop A–D → A–F; pola relabel-on-middle-delete identik dengan perilaku lama.
2. **Bukan FK-Restrict 500 / crash.** Hazard 999.14 (T-418-05/14) tetap tertutup — guard edit-shrink mencegah penghapusan slot ekor terjawab; relabel-tengah tidak melempar `DbUpdateException`. Grading per `PackageOption.Id` tetap konsisten teknis.
3. **Terdokumentasi eksplisit:** komentar kode `AssessmentAdminController.cs:8031-8039` menjelaskan batasan + larangan perketat-tanpa-konfirmasi; entry backlog `ROADMAP.md:266` (Phase 999.15, severity MED, pre-existing). Fix penuh = editing berbasis identitas = keputusan produk di luar scope 418.

Disposisi: **accepted-documented** (pre-existing), tidak dihitung sebagai `threats_open`.

## Kesimpulan

**SECURED.** 16/16 ancaman tertutup (15 mitigated + 1 accepted-by-design T-418-12). `threats_open: 0`. Tidak ada flag tak-terdaftar. WR-01/999.15 = batasan pre-existing terdokumentasi (bukan terbuka). Tidak ada gap keamanan yang memblokir ship pada `block_on: open`.

---
_Secured: 2026-06-24_
_Auditor: Claude (gsd-security-auditor)_
_ASVS Level: 1 | Metode: verifikasi retroaktif per-disposisi_
