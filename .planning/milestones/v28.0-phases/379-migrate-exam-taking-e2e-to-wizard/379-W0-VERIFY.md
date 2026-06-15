# 379 Wave-0 Verification (Open Questions Q1/Q2/Q3 + drift selectors)

**Verified:** 2026-06-14 (Plan 379-01 Task 1)
**Method:** sqlcmd localhost (W0-1) + static source read Views/ (W0-2, W0-3, token, proton) — NOL perubahan kode produksi.
**Env:** SQL `localhost\SQLEXPRESS` + `HcPortalDB_Dev` reachable (queryScalar OK).

## Hasil 3 Open Question

| W0 item | Query/Check | Hasil | Keputusan fallback |
|---------|-------------|-------|--------------------|
| **W0-1 ProtonTrack Tahun 3** (Open Q1 / A1) | `SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'` | **2** (≥1) | **TIDAK seed.** Flow E pakai track existing, pilih via `#protonTrackSelect option[data-tahun="Tahun 3"]` (opsi punya `data-tahun="@track.TahunKe"`, CreateAssessment.cshtml:225). Task 2 = SKIP seed, journal note saja. |
| **W0-2 paste-import route** (Open Q2) | grep Views: `textarea[name="pasteText"]` + link "Import Questions" | **VALID** | `Views/Admin/ImportPackageQuestions.cshtml:102` punya `textarea[name="pasteText"]`; `Views/Admin/ManagePackages.cshtml:279` punya link "Import Questions". Pola D3 lama (`exam-taking.spec.ts:599-621`) MASIH cocok: `a:has-text("Import Questions")` → `textarea[name="pasteText"]` → `button:has-text("Import")`. → Build helper `importQuestionsViaPaste` (Task 3e). |
| **W0-3 `#examExpiredModal`** (Open Q3 / A3) | grep `Views/CMP/StartExam.cshtml` | **ADA** | `#examExpiredModal` (StartExam.cshtml:300), di-`show()` saat expired (L1136). JS var `timerStartRemaining` (L453) + `timeRemaining` (L429). → Flow G (Plan 04) pakai event-driven `waitForFunction`: tunggu `#examExpiredModal` visible ATAU URL→Results ATAU DB Status flip. BUKAN `waitForTimeout(70_000)`. |

## Drift selector tambahan (konfirmasi untuk Task 3 helper extend)

| Selector | Status | Lokasi |
|----------|--------|--------|
| `#tokenSection` (markup current) | ADA | CreateAssessment.cshtml:509 (`d-none`, show via `tokenSection.classList.remove('d-none')` L1374) |
| `#IsTokenRequired` / `#AccessToken` | ADA (sudah di wizardSelectors L67-68) | CreateAssessment.cshtml:506 / 512 |
| Generate token button | ADA | `<button ... onclick="generateToken()">...Generate</button>` (L513) — selector `button:has-text("Generate"), button[onclick*="generateToken"]` |
| `#protonFieldsSection` | ADA (`d-none`, show saat Category='Assessment Proton') | CreateAssessment.cshtml:210 |
| `#protonTrackSelect` (name=`ProtonTrackId`) | ADA, opsi `data-tahun` | CreateAssessment.cshtml:219, opsi L225 |

## Keputusan per flow (ringkas)

- **Flow B (token):** extend `CreateAssessmentOpts` +`isTokenRequired`/`accessToken`; STEP 3 blok kondisional check `#IsTokenRequired` → tunggu `#tokenSection` → fill `#AccessToken` atau klik Generate. Drift `#tokenInputContainer`→`#tokenSection` confirmed.
- **Flow E (proton T3):** track Tahun 3 sudah ada (2 baris) → TIDAK seed. extend opts +`protonTrackTahun`/`protonTrackId`; STEP 1 (Category='Assessment Proton') tunggu `#protonFieldsSection` → pilih option by `data-tahun="Tahun 3"`. Interview form E3 (`SubmitInterviewResults`) di-verify Plan 03.
- **Flow D3 (paste):** route VALID → helper `importQuestionsViaPaste` (navigate ManagePackages → Import Questions → fill pasteText → Import). TIDAK fallback ke addQuestionViaForm.
- **Flow G (timer expiry):** `#examExpiredModal` ADA → event-driven assert (Plan 04), tanpa seed timer fixture, tanpa sleep-buta 70s.

## Temuan backlog (bug produksi terungkap saat verify)

*(tidak ada — verifikasi statis + 1 DB query, tak ada anomali route/500. Scope test-infra only dipatuhi.)*

## Acceptance (Task 1)

- [x] File ada, tabel 3 W0 item terisi, ≥20 baris.
- [x] W0-1 angka eksplisit: **2**.
- [x] W0-2 status: **VALID** + selector aktual.
- [x] W0-3 status: **ADA** (`#examExpiredModal`) + mekanisme Flow G (event-driven waitForFunction).
- [x] 0 perubahan Controllers/ atau Views/ (read-only).
