# Requirements — v29.0 Assessment E2E Worker-Success Fix

**Milestone goal:** Worker bisa ujian + lulus end-to-end untuk assessment **Normal + PrePost**, soal **single-answer**, **NON-Proton**.

**Source:** `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` (audit-driven). Verdict awal: **CONDITIONAL** — happy-path umum jalan; show-stopper di luar happy-path.

**Eksekusi:** 3 phase SEQUENTIAL (380→381→382), merge A→B→C. No paralel (semua sentuh `CMPController.cs`).

---

## v29.0 Requirements

### Phase 380 (A) — Admin/Engine Integrity (worker bisa MULAI dgn soal+token benar)

- [ ] **WSE-01**: Worker tetap menerima set soal non-kosong saat satu paket sibling kosong + shuffle ON (default). *(SHF-01, BLOCKER, v27-baru. `Helpers/ShuffleEngine.cs` ON-path filter paket kosong sebelum hitung K — mirror OFF-path.)*
- [ ] **WSE-02**: Worker dgn ujian Pre/Post token-required bisa masuk pakai token setelah admin meng-edit token (tersimpan uppercase). *(TOK-01, BLOCKER. `AssessmentAdminController.EditAssessment` Pre/Post branch `.ToUpper()` di 3 titik tulis token.)*
- [ ] **WSE-03**: Hanya Admin/HC yang boleh memberi extra time ujian, dan total extra time dibatasi. *(RST-01 HIGH-security + RST-04 cap. `AddExtraTime` tambah `[Authorize(Roles="Admin,HC")]` + ceiling.)*

### Phase 381 (B) — Worker Entry (StartExam integrity) — depends A

- [x] **WSE-04**: Worker yang mengerjakan Pre/Post same-day menerima HANYA paket ujian itu (Pre & Post tak tercampur). *(NEW-same-day-PrePost. `CMPController.StartExam` sibling query + filter `AssessmentType`/LinkedGroupId.)*
- [ ] **WSE-05**: Admin yang impersonate/membuka ujian worker TIDAK memulai ujian atau membakar waktu/mengunci shuffle worker. *(OPS-01 + TOK-03. Wrap semua write-on-GET StartExam dgn `if(!IsImpersonating())`.)*

### Phase 382 (C) — Grading / Lifecycle / Cert (nilai & hasil benar) — depends B · **MIGRATION**

- [ ] **WSE-06**: Nilai worker single-answer dihitung dari jawaban FINAL tersimpan (tanpa baris duplikat/stale). *(SAVE-01. Filtered-unique-index PackageUserResponse single-answer [MIGRATION] atau dedupe last-write-wins; grading ORDER BY.)*
- [ ] **WSE-07**: Percobaan Abandoned/Cancelled tak bisa di-resurrect jadi hasil Completed-lulus + sertifikat. *(STAT-01. Guard grading + SubmitExam exclude Abandoned/Cancelled.)*
- [ ] **WSE-08**: Hasil Completed/graded tak ketimpa/hilang oleh AbandonExam telat. *(STAT-02. ExecuteUpdate ber-guard `Where(Status==InProgress||Open)`.)*
- [ ] **WSE-09**: Batas waktu ditegakkan untuk ujian Normal ("Standard"); submit telat ditolak. *(TMR-01 + TMR-02 client-flag + TMR-03 auto-submit-token. `EnsureCanSubmitExamAsync` cakup Standard.)*
- [ ] **WSE-10**: Ujian token-required tak bisa diselesaikan dgn mem-bypass gate token (SaveAnswer/SubmitExam). *(TOK-02. Gate `StartedAt != null` di SaveAnswer/SubmitExam.)*
- [ ] **WSE-11**: Sertifikat worker lulus tanpa tanggal kedaluwarsa tampil konsisten "aktif" di semua surface (dashboard/badge/notif). *(CERT-01. Satukan definisi `ValidUntil==null` di `DeriveCertificateStatus` + HomeController + Renewal/CDP.)*

---

## Future Requirements (deferred — backlog 999.x)

- **RES-02** (MED, display-drift): hitungan "X/Y benar" tak ter-bobot vs Score% (skor tersimpan benar; cuma tampilan). → backlog.
- **GRD-02** (LOW, MA-only): empty-MA SetEquals full-credit — tak terjangkau via validasi create/edit (correctCount≥2), out single-answer scope. → backlog.

## Out of Scope (eksplisit)

- **Proton** (eligibility gate, year-gate, bypass, Tahun3 interview, coach mapping) — milestone terpisah; bukan jalur worker normal/prepost.
- **Essay grading** (finalize/recompute lifecycle, essay-blind regrade GRD-01/EDT-01) — exam single-answer murni tak kena; tetap HIGH di backlog audit penuh.
- **Multi-answer** grading (MA SetEquals all-or-nothing) — di luar single-answer.
- **Admin data-governance** (manual/bulk entry MAN-*, cascade-delete REC-*, category CRUD CAT-*, cert renewal chain) — bukan jalur worker-ujian.
- **UI/visual audit pass** — belum dijalankan (audit ini code-first).

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| WSE-01 | 380 | pending |
| WSE-02 | 380 | pending |
| WSE-03 | 380 | pending |
| WSE-04 | 381 | Complete |
| WSE-05 | 381 | pending |
| WSE-06 | 382 | pending |
| WSE-07 | 382 | pending |
| WSE-08 | 382 | pending |
| WSE-09 | 382 | pending |
| WSE-10 | 382 | pending |
| WSE-11 | 382 | pending |

**Coverage:** 11/11 mapped. (Filled/verified by roadmap + execution.)
