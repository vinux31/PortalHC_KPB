---
phase: 416
slug: scoped-shuffle-acak-per-section
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-23
---

# Phase 416 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Scoped Shuffle (acak per-Section). migration=FALSE (reuse kolom Phase 415 `AssessmentPackageSection.ShuffleEnabled`).
> Phase 416 menambah **NOL endpoint baru** dan **NOL surface input user baru** di luar toggle per-Section `ShuffleEnabled` yang sudah divalidasi+persist di Phase 415. Assignment ordering server-authoritative; grading by `PackageOption.Id` (urutan opsi tak pengaruhi skor).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| (engine internal) | `ShuffleEngine` = fungsi murni server-side (no EF/DB). Tidak ada input user baru; `ShuffleEnabled` (bool) sudah divalidasi+persist Phase 415. | Daftar soal in-memory → urutan terserialisasi `ShuffledQuestionIds` |
| client → StartExam / Reshuffle* / AddParticipantsLive | Endpoint sudah ada (Phase 373/410/415). 416 hanya menambah `.ThenInclude(q => q.Section)` + ganti pemanggilan option-shuffle. Tak ada endpoint/input baru. | sessionId / title+category+schedule (server-authoritative ordering) |
| HC → ManagePackageQuestions GET | View read-only. Peringatan ET = output server-rendered saja; tak menerima input baru. | `ViewBag.SectionEtWarnings` (Section name/number/K/distinct-ET) |
| test harness → HcPortalDB_Dev | e2e seed + BACKUP/RESTORE DB lokal. localhost-only guard di `dbSnapshot.ts`. Seed temporary+local-only (SEED_WORKFLOW). | snapshot `.bak` lokal |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-416-01 | Tampering / Information Disclosure | `ShuffleEngine` section-partition (soal bisa "bocor" antar-Section bila partisi setelah sampling) | mitigate | Partisi SEBELUM sampling (`ShuffleEngine.cs:74-88`); `SlicePackagesBySection` (`:129-147`) memberi tiap section HANYA soal section-nya; fallback "ambil sisa" `BuildCrossPackageAssignment` (`:344`) dibatasi `allQuestions` lokal slice. Unit `ScopedShuffle_NoCrossSectionLeak` PASS. | closed |
| T-416-02 | Tampering | Manipulasi posisi opsi → ubah nilai | accept | Grading by `PackageOption.Id` (bukan posisi huruf) — acak opsi TIDAK pengaruhi skor (`ShuffleEngine.cs:169,191`). By design aman. | closed |
| T-416-03 | Robustness (bukan STRIDE) | Engine crash saat section slice kosong / K=0 / drift lolos guard | mitigate | Guard defensif `count==0 → return new List<int>()` (`ShuffleEngine.cs:102,109,118,231,246`), JANGAN throw (best-effort D-416-03). | closed |
| T-416-04 | Tampering / Information Disclosure | Soal bocor antar-Section karena Section tak ter-load (engine jatuh ke "Lainnya") | mitigate | `.ThenInclude(q => q.Section)` di 4 lokasi load assignment: `CMPController.cs:1054` (StartExam) + `:1090` (re-guard 415), `AssessmentAdminController.cs:2550` (EagerAssign), `:6029` (ReshufflePackage), `:6111` (ReshuffleAll). e2e S1 runtime PASS. | closed |
| T-416-05 | Elevation of Privilege | Reshuffle / StartExam / kelola Section oleh pengguna tak berwenang | accept (existing) | Auth tak diubah 416: `ReshufflePackage`/`ReshuffleAll`/`ManagePackageQuestions` = `[Authorize(Roles="Admin, HC")]` (+`[ValidateAntiForgeryToken]` di POST) `AssessmentAdminController.cs:5994,6085,7643`; `AddParticipantsLive` (`:2585`) idem; `CMPController` class-level `[Authorize]` (`:25`) untuk StartExam. | closed |
| T-416-06 | Drift / robustness | AddParticipantsLive (EagerAssign) memakai jalur berbeda → distribusi peserta baru drift | mitigate | 4 lokasi load di-update SERAGAM (load Section + `BuildSectionAwareOptionShuffle`). Signature `BuildQuestionAssignment` dipertahankan → konsistensi cross-call otomatis. e2e S4 parity PASS. | closed |
| T-416-07 | DoS (komputasi ET-warning) | Komputasi warning di GET per-Section | accept | O(soal) sederhana di halaman admin terautentikasi (`AssessmentAdminController.cs:7673-7680`); non-blocking; tak ada loop tak terbatas. | closed |
| T-416-08 | Tampering (data lokal) | Seed e2e menempel di DB lokal pasca-run | mitigate | `beforeAll` BACKUP / `afterAll` RESTORE + `unlink` (`scoped-shuffle.spec.ts:183-199`); localhost-only guard menolak target non-localhost (`dbSnapshot.ts:36-44`); RESTORE `SINGLE_USER WITH ROLLBACK IMMEDIATE` + `WITH REPLACE`. SEED_JOURNAL 416-03 = `cleaned` (COUNT '%SCOPED416%'=0). Tak menyentuh Dev/Prod. | closed |
| T-416-09 | Information Disclosure | Bukti runtime soal tak bocor antar-Section (integritas konten ujian) | mitigate | e2e S1 (`scoped-shuffle.spec.ts:204`): assert DB `ShuffledQuestionIds == DOM qcard order` (`:273`) + blok kontigu per-Section / tak interleave (`:157-170,275`), "Lainnya" terakhir. Verifikasi runtime untuk T-416-01/T-416-04. 5/5 e2e PASS. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

### XSS note (ET-warning render, ASVS V5.3.3)
Peringatan ET dirender `ManagePackageQuestions.cshtml:188-197` memakai interpolasi Razor standar (`@secLabel`, `@wt.Name` via secLabel, `@wt.DistinctEt`, `@wt.K`) → **auto HTML-encoded**. TIDAK ada `@Html.Raw` pada nilai terkendali-pengguna (Section name). Tidak ada reflected-XSS baru.

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-416-01 | T-416-02 | Acak posisi opsi tak pengaruhi skor — grading by `PackageOption.Id` bukan posisi huruf (by design, PackageOption.cs). | Planner (PLAN 416-01 threat_model) | 2026-06-23 |
| AR-416-05 | T-416-05 | Auth Admin/HC + antiforgery sudah ada (Phase 373/410/415); 416 tak menambah/melemahkan endpoint. | Planner (PLAN 416-02 threat_model) | 2026-06-23 |
| AR-416-07 | T-416-07 | Komputasi ET-warning O(soal) di halaman admin terautentikasi, non-blocking; bukan vektor DoS realistis. | Planner (PLAN 416-02 threat_model) | 2026-06-23 |

*Accepted risks do not resurface in future audit runs.*

---

## Non-Security Awareness (NOT a threat — tidak memblokir)

- **DEF-416-01 / WR-01 (dead-feature ET-warning):** predikat `DistinctEt > K` di `AssessmentAdminController.cs:7680` tak pernah fire untuk paket tunggal (1 soal = 1 string ET → `DistinctEt ≤ K` selalu). Ini isu maintenance/dead-nicety, **NON-BLOCKING by design**, **bukan ancaman keamanan** (tidak ada kerusakan/leak/auth-bypass). Dilacak di `deferred-items.md` untuk owner Plan 02 / Phase 419. Tidak mempengaruhi disposisi keamanan apa pun.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-23 | 9 | 9 | 0 | gsd-security-auditor |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-23
