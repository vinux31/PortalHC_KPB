# Phase 315 Wave 0 Investigation Output

**Investigation date:** 2026-05-11
**Investigator:** Wave 0 executor (Plan 01)
**Resolves:** A1 (AssessmentPackage cardinality), A2 (SubmitExam Essay branch), A6 (UserPackageAssignment lifecycle)
**Method:** Source-code read (per CONTEXT D-04), bukan runtime probe.

---

## A1: AssessmentPackage Cardinality with Sibling Sessions

**Question:** Apakah 1 AssessmentSession punya 1 AssessmentPackage (1-to-1) atau bisa share AssessmentPackage di multiple sibling sessions?

**Source citations:**

- `Models/AssessmentPackage.cs:11` — Field FK `public int AssessmentSessionId { get; set; }`. **Type adalah `int` (NOT NULL)** — bukan `int?`. Satu row AssessmentPackage WAJIB punya tepat satu `AssessmentSessionId`. Tidak ada join table; tidak ada many-to-many.
- `Models/AssessmentPackage.cs:12-13` — Navigation `[ForeignKey("AssessmentSessionId")] public virtual AssessmentSession AssessmentSession { get; set; } = null!;`. Single-reference navigation (bukan `ICollection<AssessmentSession>`). Cardinality di sisi Package: many-Packages-to-one-Session.
- `Controllers/CMPController.cs:905-917` — StartExam query pattern: build `siblingSessionIds = AssessmentSessions.Where(same Title+Category+Schedule.Date).Select(Id)`, lalu `AssessmentPackages.Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))`. Pola ini secara explicit mengasumsikan setiap sibling session BISA punya packages sendiri (filter `.Contains` mengembalikan packages dari MULTIPLE sessions). Tidak ada lookup "shared package row antar sibling".
- `Controllers/CMPController.cs:947-953` — Saat lazy-create UserPackageAssignment: `var sentinelPackage = packages.First(); ... AssessmentPackageId = sentinelPackage.Id`. Komentar di line 947: "store first package ID (no schema change — AssessmentPackageId still required by FK)". Artinya, walau cross-package-pool dipakai untuk question shuffle, UPA hanya menyimpan 1 package_id (sentinel) — Package tidak dibagi/di-share sebagai "single row untuk dua peserta".

**Verdict:** **1-PER-SESSION** (one AssessmentPackage row hanya punya satu AssessmentSessionId; tidak shared antar sibling sessions di database level)

**Rationale:** `Models/AssessmentPackage.cs:11` mendefinisikan `AssessmentSessionId` sebagai `int` non-nullable FK tunggal — tidak ada mekanisme schema untuk satu package row dipakai dua session. Di `Controllers/CMPController.cs:912-917`, query `siblingSessionIds.Contains(p.AssessmentSessionId)` membuktikan controller menyapu packages dari MULTIPLE sibling sessions (cross-package pool), yang berarti setiap session punya rangkaian package row sendiri. Pattern produksi: HC bikin package dari satu "representative session", lalu sibling sessions lain (peserta2 dst.) reuse same Title+Category+Schedule.Date, masing-masing dengan package row tersendiri yang di-INSERT terpisah.

**Implication for seed:**
- **Verdict: 1-PER-SESSION** → 18 sibling sessions × 1 package per session = **18 AssessmentPackage rows** (ID range 9001-9018). Question + Option rows ikut: bila 4 questions × 4 options per package, maka 18 × 4 = **72 PackageQuestion** dan 72 × 4 = **288 PackageOption**.
- Alternatif "share package row antar peserta" DITOLAK — bukan model schema yang ada.
- Catatan kompatibilitas dengan recommendation Pitfall 3 (RESEARCH.md line 733): rekomendasi "single Package + N Sessions" SECARA SCHEMA tidak mungkin karena FK direction. Yang dimaksud author RESEARCH.md mungkin "single CONTENT-EQUIVALENT package per scenario, duplicated INSERT per peserta sibling" — yang itu yang akan kita seed.

---

## A6: UserPackageAssignment Auto-create vs Pre-seed

**Question:** Apakah seed SQL harus INSERT UserPackageAssignment row eksplisit, atau auto-created saat user pertama kali hit StartExam endpoint?

**Source citations:**

- `Controllers/CMPController.cs:926-927` — Lookup: `var assignment = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == id);`. Filter HANYA by `AssessmentSessionId` (bukan composite with UserId) — karena 1 session = 1 user, secara natural UPA per session juga unique per user.
- `Controllers/CMPController.cs:929-960` — Branch `if (assignment == null)`: build shuffle (questions + options), set `AssessmentSessionId = id`, `AssessmentPackageId = sentinelPackage.Id`, `UserId = user.Id`, lalu `_context.UserPackageAssignments.Add(assignment); await _context.SaveChangesAsync();`. **Lazy create pattern explicit** — UPA dibuat on first hit ke StartExam.
- `Controllers/CMPController.cs:961-974` — Race-condition guard: `catch (DbUpdateException) { _context.ChangeTracker.Clear(); assignment = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == id); if (assignment == null) throw; }`. Confirms auto-create pattern is robust (handles double-click race naturally).
- `Models/UserPackageAssignment.cs:12-25` — Field definitions menunjukkan UPA tidak punya unique constraint level DB selain PK `Id`. Tidak ada `IsSeededRow` atau marker yang membedakan auto-create vs pre-seed.

**Verdict:** **AUTO-CREATE-LAZY** (seed SQL TIDAK perlu INSERT UserPackageAssignment row — app akan auto-create saat peserta pertama kali buka `/CMP/StartExam/{sessionId}`)

**Rationale:** `Controllers/CMPController.cs:929-960` menunjukkan StartExam selalu cek `assignment == null` → jika belum ada, buat baru dengan shuffle determined oleh `Random.Shared` (line 931). Pre-seed UPA hanya akan memaksa shuffle deterministik yang sudah kita pilih saat seed time — TIDAK ada keuntungan testing (Playwright tidak butuh shuffle order spesifik selama setiap peserta dapat shuffle yang valid). Pre-seed JUGA tidak harmful selama `ShuffledQuestionIds` JSON valid, tapi menambah baris seed yang bisa salah → KISS principle: skip UPA dari seed.

**Implication for seed:**
- **Verdict: AUTO-CREATE-LAZY** → Seed SQL **SKIP** tabel `UserPackageAssignments` sepenuhnya. Saat peserta1 buka StartExam pertama, controller bikin UPA-nya. Saat peserta2 buka, controller bikin UPA-nya (untuk sibling session yang lain dengan `AssessmentSessionId` berbeda).
- Total UPA rows after full Playwright run = **18** (auto-generated by app, satu per session); pre-seed = **0** rows.
- Layer 1 validation post-seed: `SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId BETWEEN 9001 AND 9018` = **0** (belum ada exam taken).
- Layer 4 cleanup: UPA rows yang ter-create selama test akan ikut ter-RESTORE bersih karena BACKUP diambil sebelum app touch UPA.

---

## A2: SubmitExam Essay Branch — Form Value vs DB-Persisted

**Question:** Untuk question type Essay, saat SubmitExam dipanggil, apakah controller membaca jawaban dari form payload `answers[questionId]=text` atau dari DB record `PackageUserResponse.TextAnswer` yang sudah disimpan via SignalR hub?

**Source citations:**

- `Controllers/CMPController.cs:1569` — Signature: `public async Task<IActionResult> SubmitExam(int id, [FromForm(Name = "answers")] Dictionary<int, int>? answers, ...)`. **Type adalah `Dictionary<int, int>`** — value `int`, BUKAN `string`. Form payload Essay (`answers[essayQId]=text-jawaban`) akan gagal binding ke int → field tersebut di-skip oleh ModelBinder. Konfirmasi: Form value tidak pernah menjadi sumber jawaban Essay.
- `Controllers/CMPController.cs:1672-1717` — Loop grading di SubmitExam. Branch per QuestionType:
  - MC (line 1678-1704): baca dari form `answers[q.Id]`, upsert MC answer.
  - MA (line 1705-1715): baca dari DB `allExistingResponses` (line 1665-1670, sudah di-load batch dari `PackageUserResponses`) — form value IGNORED.
  - Essay (line 1716): komentar `// Essay: scored manually by HC (EssayScore), skip here`. **TIDAK ADA read dari form**, TIDAK ADA upsert TextAnswer di SubmitExam.
- `Hubs/AssessmentHub.cs:134` — Signature `public async Task SaveTextAnswer(int sessionId, int questionId, string textAnswer)`. Hub method yang menerima string `textAnswer` dari client side (textarea blur/debounce 2s).
- `Hubs/AssessmentHub.cs:167` — Upsert path existing response: `existing.TextAnswer = textAnswer;`. SignalR-only persistence — satu-satunya jalur Essay text masuk DB.
- `Services/GradingService.cs:113-117` — Branch Essay: `case "Essay": // Essay: skor 0 sementara — akan di-grade manual oleh HC ... break;`. **GradingService DOES NOT read TextAnswer at submit time** — Essay disimpan score 0 dulu, status session jadi `PendingGrading` (line 189-227). TextAnswer baru dibaca HC manual nanti.
- `Controllers/AssessmentAdminController.cs:2856` — Admin grading view memuat: `TextAnswer = essayRespMap.TryGetValue(q.Id, out var resp) ? resp.TextAnswer : null,`. HC baca dari `PackageUserResponse.TextAnswer` (DB authoritative) untuk grading.

**Verdict:** **DB-PERSISTED-AUTHORITATIVE** (form value Essay tidak pernah dipakai; satu-satunya sumber kebenaran adalah `PackageUserResponse.TextAnswer` yang ditulis via SignalR `SaveTextAnswer`)

**Rationale:** Signature `Dictionary<int, int>` di `Controllers/CMPController.cs:1569` secara struktural menutup pintu untuk form-binding Essay text (cast `string → int` gagal, ModelBinder skip). Bahkan jika binding berhasil, loop grading di line 1672-1717 tidak pernah menyentuh form `answers` untuk branch Essay (komentar `// Essay: scored manually by HC` di line 1716). Essay text persistence 100% melalui `Hubs/AssessmentHub.cs:134 SaveTextAnswer` (debounce 2s blur trigger), dan HC grading membaca dari `PackageUserResponse.TextAnswer` di `Controllers/AssessmentAdminController.cs:2856`. Tidak ada hybrid path.

**Implication for test:**
- **Verdict: DB-PERSISTED-AUTHORITATIVE** → Helper `examMatrix.takeExam` WAJIB tunggu `#saveIndicatorText` ber-state `saved` SEBELUM submit (positive confirmation SignalR upsert sukses). Form `<textarea>` value boleh apa saja di waktu submit — server abaikan.
- Tidak perlu trigger `blur` paksa sebelum `click Submit` (asal sudah ada save confirm) — debounce 2s di SignalR client adalah satu-satunya gate.
- Sentinel `[META-AllWrong]` untuk Essay = peserta input string kosong / hanya whitespace → SaveTextAnswer tetap upsert (TextAnswer empty). Saat HC grading manual, skor 0 → final score < passPercentage → finding "Essay all-wrong → fail" tervalidasi.
- Sentinel `[META-AllCorrect]` untuk Essay = peserta input non-empty answer + HC kasih full score → final score lulus.

---

## Final Seed Dimensions

Berdasarkan A1 (1-PER-SESSION), A6 (AUTO-CREATE-LAZY), Pitfall 3 (sibling session pattern), tabel final:

| Tabel | Count | ID Range | Rationale |
|-------|-------|----------|-----------|
| AssessmentSessions | 18 | 9001-9018 | 9 scenarios × 2 peserta (coachee + coachee2) — Pitfall 3 sibling sessions per Models/AssessmentSession.cs:10 (UserId NOT NULL single FK) |
| AssessmentPackages | 18 | 9001-9018 | 1-per-session per A1 verdict (FK NOT NULL, no sharing) — Models/AssessmentPackage.cs:11 |
| PackageQuestions | 72 | 50001-50072 | 18 packages × 4 questions per package (rata-rata; scenario-spesifik bisa 3-5, total terkalibrasi 72) — content duplicate between sibling sessions (deterministic per scenario) |
| PackageOptions | 288 | 80001-80288 | 72 questions × 4 options (MC) atau ~3-4 (MA); Essay tidak punya option (PackageOption tidak di-INSERT untuk question Essay). Estimasi konservatif 4 per question, total 288. **Plan 02 author bisa adjust ±10% kalau scenario design pakai mix Essay-heavy (lebih sedikit options) atau MC-heavy (lebih banyak).** |
| UserPackageAssignments | 0 (pre-seed) → 18 (post-test) | (auto-generated saat StartExam) | Per A6 verdict — app auto-create lazy. Seed SQL skip tabel ini. |
| PackageUserResponses | 0 (pre-seed) | (auto-generated saat peserta jawab) | App auto-insert via /CMP/SaveAnswer (MC) + AssessmentHub.SaveMultipleAnswer/SaveTextAnswer (MA/Essay). Seed SQL skip. |

**Catatan dimensi PackageOption:** angka 288 adalah upper-bound. Actual count tergantung distribusi question type per scenario:
- Scenario yang Essay-only: option count = 0 per question.
- Scenario yang MC/MA-only dengan 4 options: option count = 4 per question.
- Plan 02 (Wave 1 — seed SQL drafting) WAJIB recount berdasarkan scenario config final. **Layer 1 validation di Plan 03 harus update angka ini setelah seed SQL ditulis.**

**Layer 1 expected counts (Plan 03 globalSetup post-seed validation):**
- `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%'` = **18**
- `SELECT COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%')` = **18**
- `SELECT COUNT(*) FROM PackageQuestions WHERE AssessmentPackageId BETWEEN 9001 AND 9018` ≈ **72** (recalibrate after Plan 02 seed drafting)
- `SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId BETWEEN 50001 AND 50072` ≈ **288** (recalibrate after Plan 02 seed drafting)
- `SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId BETWEEN 9001 AND 9018` = **0** (per A6 — UPA dibuat saat peserta hit StartExam, bukan pre-seed)
- `SELECT COUNT(*) FROM PackageUserResponses WHERE AssessmentSessionId BETWEEN 9001 AND 9018` = **0** (response insert saat peserta jawab soal)

**Layer 4 expected counts (Plan 03 globalTeardown post-RESTORE validation):**
- Semua query di atas (termasuk UserPackageAssignments dan PackageUserResponses yang di-create selama test) = **0** post-RESTORE.
- Confirmation BACKUP/RESTORE bekerja end-to-end clean.

---

## Investigation Method

Pendekatan: source-code read (per CONTEXT D-04). File yang dibaca:

- `Models/AssessmentPackage.cs` (lines 1-25) — schema FK direction
- `Models/UserPackageAssignment.cs` (lines 1-50) — UPA fields
- `Controllers/CMPController.cs:820-1050` — StartExam lifecycle (sibling lookup + lazy UPA create)
- `Controllers/CMPController.cs:1569-1756` — SubmitExam full body (Dictionary<int,int> signature + per-type grading branch)
- `Services/GradingService.cs:60-227` — GradeAndCompleteAsync (Essay branch + PendingGrading status)
- `Hubs/AssessmentHub.cs:130-250` — SaveTextAnswer upsert pattern
- `Controllers/AssessmentAdminController.cs:2856` — HC grading TextAnswer load

**Tidak dilakukan:** runtime DB probe (tidak ada Kestrel running, tidak ada test execution). Per spec § Open Questions resolution methodology — source-code read sufficient.

---

## Cross-references

- `RESEARCH.md` § Assumptions Log A1, A2, A6 — resolved (this document)
- `RESEARCH.md` § Common Pitfalls Pitfall 3 — confirmed (18 sibling sessions, separate packages per session)
- `RESEARCH.md` § Open Questions item 1 (AssessmentPackage cardinality) — resolved as A1
- `RESEARCH.md` § Open Questions item 2 (SubmitExam Essay handling) — resolved as A2
- `CONTEXT.md` D-04 (Wave 0 investigation approach via source read) — followed
- `CONTEXT.md` D-05 (Marker strategy fallback Title prefix) — confirmed: `Models/AssessmentSession.cs` does not have `Notes` field (grep returned 0 matches), so fallback `[MATRIX_TEST_2026_05_11]` Title prefix is the active marker

## Deviation from RESEARCH.md

Tidak ada deviasi. RESEARCH.md `Pitfall 3` line 733 recommendation "single Package + N Sessions + N UserPackageAssignments" SECARA INTENT-LEVEL benar (logical sharing per scenario), tetapi SECARA SCHEMA tidak feasible — `AssessmentPackage.AssessmentSessionId` adalah FK NOT NULL ke 1 session saja. Yang feasibel: 18 package row terpisah, dengan content-equivalent per pasangan sibling. Plan 02 author WAJIB INSERT 18 packages (bukan 9 shared).

RESEARCH.md `Common Pitfalls Pitfall 3` line 731 mengantisipasi pertanyaan ini ("or 50001-50037 if shared across sibling sessions — investigate") — Wave 0 ini definitif menjawab: **TIDAK shared, 50001-50072 (atau range sesuai distribusi question type final)**.
