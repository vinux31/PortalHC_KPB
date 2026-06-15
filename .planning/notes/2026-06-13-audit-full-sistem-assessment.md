---
date: "2026-06-13 17:52"
promoted: false
---

sekarang ada pekerjaan sisa milestone v25.0, tapi saya ingin kamu audit sehingga saya tahu apakah sistemnya bekerja dengan benar. auditnya full code dulu,
  setelah ok baru lanjut full UI, terkait sistem Assessment. saya ingin kamu audit full sistem assessment:
  1. tipe assessment
  2. jenis assessment
  3. tipe tipe kategori assessment dan sub kategori
  4. jenis/tipe  soal, termasuk fitur image di soal
  5. logic shuffle soal, toogle shuffle soal, pre-post soal
  6. sertifikat assessment
  6. dan lainnya yang berhubungan dengan assessment

---

## SCOPE GAP-CHECK (ditambah 2026-06-14)

Hasil map kode (6 agent paralel) vs 7-item scope di atas. Verdict: 7-item ini sebut **struktur** assessment tapi **lewatkan mesin correctness runtime** — justru itu yg dijawab "apakah sistemnya bekerja dengan benar". Audit literal item 1-6 akan skip inti. Item 7 "dan lainnya" harus diperlakukan **wajib-luas**.

### Sudah ke-cover scope asli
- Item 1 tipe → AssessmentType Manual/Online/PreTest/PostTest [Models/AssessmentConstants.cs]
- Item 3 kategori+sub → AssessmentCategory + CRUD [AssessmentAdminController.cs:388-587]
- Item 4 soal+image → QuestionType + image upload/render/delete/sync [QuestionTypeLabels.cs, _QuestionImage.cshtml, FileUploadHelper.cs]
- Item 5 shuffle+pre/post → ShuffleEngine + toggle UpdateShuffleSettings + LinkedGroup [CMPController.cs:982-1056, AssessmentAdminController.cs:5160-5352]
- Item 6 sertifikat → NomorSertifikat + PDF + cert dashboard [CertNumberHelper.cs, CMPController.cs:1754-2101, CDPController.cs:3718-4085]

### WAJIB DITAMBAH — HIGH (8)
1. Scoring/grading engine — MC IsCorrect, MA SetEquals all-or-nothing, maxScore denominator [Services/GradingService.cs:56-318]
2. Pass/fail formula + PassPercentage threshold — `(score/max*100)>=threshold`; formula ke-duplikat di aggregator (rawan drift) [GradingService.cs:134, AssessmentScoreAggregator.cs:58]
3. Essay manual-grade lifecycle — PendingGrading→SubmitEssayScore→FinalizeEssayGrading→RecomputeEssayScores (bug-prone, Score=0 bug Phase 376) [AssessmentAdminController.cs:3428-3990]
4. AssessmentSession status lifecycle + race-safe finalize [GradingService.cs:238-246, Models/AssessmentSession.cs:20]
5. Timer/duration server-authoritative + 2-tier submit block + auto-submit token [CMPController.cs:4382-4444, 1126-1147]
6. Token + StartExam access gate (AccessToken compare, ownership/window) [CMPController.cs:848-958]
7. Answer persistence/auto-save (SaveAnswer upsert, SignalR, resume) [CMPController.cs:346-417, Hubs/AssessmentHub.cs:134-252]
8. Proton cross-year eligibility/gating (enforce saat create-session; Phase 358-363) [AssessmentAdminController.cs:1376-1404, ProtonCompletionService.cs:163, CoachMappingController.cs:527-565]

### WAJIB DITAMBAH — MED (6)
9. Proton year-bypass workflow (PendingProtonBypass + BypassSave/Confirm/CancelPending + CreateAssessment exempt) [ProtonDataController.cs:1509-1687]
10. Proton Tahun 3 interview scoring (SubmitInterviewResults, di LUAR GradingService) [AssessmentAdminController.cs:3669-3786]
11. Manual/bulk score entry (TrainingAdmin manual + BulkBackfill + ImportTraining, bypass grading) [TrainingAdminController.cs:744-1497]
12. Admin edit-jawaban + regrade + Pass↔Fail cert/Proton cascade [AssessmentAdminController.cs:2941-3216, GradingService.cs:437-554]
13. Per-ElemenTeknis score + Pre/Post gain-score `(Post-Pre)/(100-Pre)*100` [GradingService.cs:139-181, CMPController.cs:2342-2407]
14. Records aggregation + cascade-delete engine [WorkerDataService.cs:28-88, RecordCascadeDeleteService.cs:20-54]

### PERTIMBANGKAN — LOW (4)
15. Notifikasi assessment (ASMT_ASSIGNED / ALL_COMPLETED / CERT_EXPIRED login-passive) [WorkerDataService.cs:431-483, HomeController.cs:120-173]
16. HC live monitoring + SignalR force-close/reset + ExamActivityLog [AssessmentAdminController.cs:2732-3243, Hubs/AssessmentHub.cs:21-287]
17. Audit log admin actions + delete-impact cascade (DeleteAssessment/Group/PrePostGroup) [AuditLog.cs, AssessmentAdminController.cs:2205-2487]
18. Impersonation effective-user di surface assessment + write-on-GET guard (Phase 377) [AdminController.cs:124-215, CMPController.cs:904-911]

### PERTANYAAN — perlu jawab user sebelum audit mulai
- A. "tipe" (item 1) vs "jenis" (item 2) — beda apa? Satu = AssessmentType, satu = Category string?
- B. Proton in-scope? Note tak sebut "Proton" sama sekali, tapi itu bagian terkompleks (track/tahun/gating/bypass/interview).
- C. Pre/post item 5 = pairing soal saja, atau termasuk gain-score computation?
- D. Manual/bulk entry (TrainingAdmin) masuk audit?
- E. Sertifikat item 6 termasuk renewal chain + expiry + cascade-delete?
- F. Code-first vs UI pass — scoring/timer/token = code-side, harus di pass code INI, jangan jatuh ke celah antar-2-pass.

---

## STATUS: AUDIT CODE-PASS SELESAI (2026-06-14)

Dijalankan asumsi default-luas (B=Proton in-scope, F=semua mesin correctness di pass code ini). Multi-agent workflow 21 area → verifikasi adversarial → sintesis (99 agent, ~6.6M token).

- **Verdict:** MATERIAL-BUGS (mendekati kritis)
- **Temuan:** 77 raw → **72 confirmed** (HIGH=13, MED=28, LOW=30, NONE=1), 5 refuted
- **Laporan lengkap:** `docs/assessment-audit/2026-06-14-code-audit-findings.md`
- **3 BLOCKER:** (1) regrade essay → edit 1 jawaban MC pada assessment ber-essay menghapus nilai essay, Pass→Fail, sertifikat+penanda Proton dicabut, tak pulih otomatis [GRD-01/PASS-01/EDT-01]; (2) timer server-side = dead code utk AssessmentType="Standard" (guard cuma cocok "Online") → submit lewat waktu diterima [TMR-01/CAT-01]; (3) authz hole: CreateQuestion atribut yatim [QTY-01] + AddExtraTime tanpa Roles [RST-01].
- **UI pass: BELUM** (sesuai instruksi "code dulu, ok baru UI").
- **Cross-ref test + origin/main (2026-06-14):** 71 finding → **55 ADA DI PRODUKSI** (origin/main), 14 unshipped (ITHandoff-only). Coverage test: **0 covered, 40 gap, 31 untested** → tiap fix wajib bawa test baru.
- **Rencana fix MINIMIZED: 7 phase** (dipangkas dari ~13-16). Gabung root-cause duplikat (essay triplet GRD-01/PASS-01/EDT-01 = 1 fix), batch fix kecil regression-safe, defer 14 LOW/unshipped → backlog 999.x. Detail §7 laporan.
  - P1 Essay grading & cert-status core (12 finding, prod-urgent)
  - P2 Authz hole + reflection-authz test (prod-urgent)
  - P3 Timer & client-flag enforcement (prod-urgent)
  - P4 Lifecycle race + impersonation/token gate (prod-urgent)
  - P5 Answer persistence + shuffle + results review (prod-urgent, +migration)
  - P6 Admin manual-records + category (prod-urgent, +migration)
  - P7 Proton flow integrity (prod-urgent, +migration)
  - 3 phase butuh migration → flag IT.
- **Next:** `/gsd-new-milestone` v29.0 "Assessment Audit-Fix" (7 phase) ATAU jalankan UI pass dulu sebelum buka milestone.

---

## RE-FILTER URGENT: E2E worker-success (Normal+PrePost, single-answer, NON-Proton) — 2026-06-14

User re-scope: fokus worker **bisa ujian + sukses E2E**, assessment **Normal + PrePost**, soal **single answer**, **Proton DROP**, lensa v25-v28. Trace ulang happy-path (6 agent) + filter.

- **Laporan fokus:** `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md`
- **Verdict: CONDITIONAL** — happy-path paling umum (1 paket penuh, token uppercase/auto, 1 tab, dalam waktu) JALAN. Tapi 3 show-stopper di luar happy-path.
- **18 in-scope, 53 out** (proton/essay/MA/admin). Essay triplet GUGUR (single-answer murni).
- **3 BLOCKER/WRONG-RESULT teratas:**
  1. **SHF-01** [v27 BARU, prod] — default shuffle ON + ada paket kosong (≥2 paket) → K=Min=0 → worker dapat 0 soal → auto-grade 0% Fail. Paling bahaya.
  2. **TOK-01** [lama, prod] — admin edit token Pre/Post ketik huruf kecil → tersimpan lowercase, input worker dipaksa uppercase → 'Token tidak valid' permanen, worker terkunci.
  3. **NEW same-day Pre/Post** [lama] — query sibling StartExam (Title+Category+Date, TANPA filter AssessmentType) → pool paket Pre+Post tercampur → set soal salah + denominator grading tercemar.
  + WRONG-RESULT: SAVE-01 (dup baris MC race → grade opsi basi), STAT-01/02 (resurrect cancelled→lulus / abandon timpa graded), TMR-01 (timer Standard dead-code), CERT-01 (cert lulus ValidUntil=null tampil Expired).
- **Rencana URGENT 3 phase:** P1 Entry-integrity (SHF-01/TOK-01/same-day/OPS-01/TOK-03) · P2 Grading+lifecycle (SAVE-01/STAT-01/STAT-02/TMR-01, +MIGRATION filtered-unique-index) · P3 Cert-visibility (CERT-01). + 12 Playwright e2e scenario bukti.
- **RST-01** (AddExtraTime no-Roles) = 1-baris atribut authz, tambal cepat di luar jalur.
- **Next:** buka v29.0 (3 phase urgent ini) ATAU langsung plan Phase 1.

### VERIFIKASI PRE-MILESTONE (2026-06-14) — coverage + paralelisme
Plan benar, coverage 18/18 OK. Re-struktur jadi **5 phase / 4 wave** biar ada paralel:
- **Wave 1 (PARALEL, zero overlap):** P0a SHF-01 (ShuffleEngine.cs) ‖ P0b RST-01+RST-04 (AddExtraTime authz). Deliver blocker #1 + HIGH-security duluan.
- **Wave 2:** P1 Entry (TOK-01, same-day Pre/Post, OPS-01, TOK-03) — depends P0a. Semua di `CMPController.StartExam` GET (1 atomic edit).
- **Wave 3:** P2 Grading+lifecycle +MIGRATION (SAVE-01, STAT-01/02, TMR-01/02/03, **TOK-02 pindah ke sini**) — depends P1.
- **Wave 4:** P3 Cert (CERT-01) — depends P2.
- **Kenapa tak full-paralel:** P1/P2/P3 semua sentuh `CMPController.cs` (soft overlap) → sequential di axis itu. Cuma Wave 1 yg benar-benar paralel.
- **Miss/risiko ditemukan:** (1) SHF-01 WAJIB land sebelum P1 (test P1 butuh engine ter-fix). (2) TOK-02 ke P2 bukan P1 (konflik same-method SaveAnswer/SubmitExam). (3) Migration filtered-unique-index butuh discriminator QuestionType di PackageUserResponses — kalau tak ada, raw-SQL index ATAU fallback dedupe last-write-wins (no migration). Putuskan saat plan P2. (4) CERT-01 keputusan null-semantics lintas-surface (renewal worklist) — kunci intent saat plan P3. (5) 1× notify IT migration setelah P2; pastikan P1/P3 migration=false.
- Detail penuh di laporan FOCUS §VERIFIKASI.
