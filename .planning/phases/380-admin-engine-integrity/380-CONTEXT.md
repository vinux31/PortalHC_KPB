# Phase 380: Admin/Engine Integrity - Context

**Gathered:** 2026-06-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Worker bisa **MULAI** ujian (Normal + PrePost, single-answer, non-Proton) dengan **set soal & token yang benar**, dan kontrol admin "tambah waktu" ter-otorisasi & terbatas. Fix di sisi **engine** (`Helpers/ShuffleEngine.cs`) + **admin controller** (`Controllers/AssessmentAdminController.cs`) + **token gate** (`Controllers/CMPController.cs:VerifyToken`).

Tiga fix (REQ): **WSE-01** (SHF-01 paket kosong + shuffle ON), **WSE-02** (TOK-01 token Pre/Post lockout), **WSE-03** (RST-01/04 AddExtraTime authz + cap).

OUT (scope dikunci roadmap): Proton, essay, multi-answer, perubahan UI ujian peserta, data-governance admin. Worker-entry StartExam mutation (same-day Pre/Post, impersonasi) = Phase 381. Grading/lifecycle/cert = Phase 382.
</domain>

<decisions>
## Implementation Decisions

### TOK-01 — Token Pre/Post (WSE-02)
- **D-01:** Fix **defensive compare + write** (BUKAN forward-only, BUKAN repair-script). Dua sisi:
  1. **Compare:** `VerifyToken` bandingkan UPPERCASE dua sisi — `(assessment.AccessToken ?? "").Trim().ToUpper() == (token ?? "").Trim().ToUpper()`. Ini **auto-heal SEMUA token lowercase lama** tanpa migration/script — worker yang terkunci sekarang langsung pulih.
  2. **Write:** Tetap simpan token uppercase di `EditAssessment` Pre/Post branch (3 titik tulis: ~1812/1916/1937) untuk kebersihan data, mirror pola `CreateAssessment` (~1107).
- **Rationale:** Zero DB touch (no migration, no IT action), heal instan, defensif terhadap drift masa depan.

### RST-01/04 — AddExtraTime (WSE-03)
- **D-02:** AddExtraTime → `[Authorize(Roles="Admin,HC")]` (match sibling action ResetAssessment/AkhiriUjian). HC tetap punya akses operasional; peserta/worker terblokir.
- **D-03:** Cap total extra time **≤ durasi asli ujian** (`DurationMinutes`). Akumulasi `ExtraTimeMinutes` tak boleh melebihi durasi asli (ujian 60 mnt → max +60, total 2×). Tolak grant yang akan melampaui, dgn pesan jelas. Skala otomatis ikut panjang ujian.

### SHF-01 — Paket kosong + shuffle ON (WSE-01)
- **D-04:** Fix di **engine** `BuildCrossPackageAssignment` ON-path — filter paket kosong (`p.Questions != null && p.Questions.Count > 0`) **sebelum** hitung `K = Min(...)`, mirror OFF-path (ShuffleEngine.cs:53-57). Fix di engine → otomatis kena **StartExam + ReshufflePackage + ReshuffleAll** (3 caller).
- **D-05:** Edge **all-empty** (semua paket sibling 0 soal): StartExam **BLOKIR + pesan ramah** ("Ujian belum siap / belum ada soal — hubungi admin"), JANGAN tulis StartedAt/Status=InProgress, JANGAN buat UserPackageAssignment, JANGAN auto-grade. Cegah 0% Fail palsu. (Kasus umum 1-paket-kosong-di-antara-beberapa tetap jalan: worker dapat soal dari paket berisi.)

### Claude's Discretion
- Wording persis pesan all-empty (BI, ramah, arahkan ke admin).
- Penempatan guard all-empty (di StartExam setelah engine kembalikan list kosong — sebelum write apa pun).
- Apakah cap RST-04 dicek di server action saja (cukup) atau juga hint di UI (opsional).
- Bentuk pesan tolak cap extra-time.

### Folded Todos
(none)
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Audit source (fix detail + bukti + test plan)
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — per-finding SHF-01/TOK-01/RST-01/04: masalah, file:line, usulan fix, + E2E test plan (Playwright scenario #5 token, #6 empty-package).
- `docs/assessment-audit/2026-06-14-code-audit-findings.md` — master finding (severity, verifikasi adversarial).

### Requirements
- `.planning/REQUIREMENTS.md` — WSE-01, WSE-02, WSE-03 (+ traceability).

### Code (anchors)
- `Helpers/ShuffleEngine.cs` — `BuildCrossPackageAssignment` ON-path K compute (~107-110, lokasi fix) vs OFF-path empty-filter (~53-57, template).
- `Controllers/CMPController.cs` — `VerifyToken` (~850-884, compare ~876) + StartExam consume BuildQuestionAssignment (~1019-1042, lokasi guard all-empty).
- `Controllers/AssessmentAdminController.cs` — EditAssessment Pre/Post token write (1812/1916/1937) + CreateAssessment uppercase template (~1107) + `AddExtraTime` (~6866-6910) + `ReshufflePackage` (~5188) + `ReshuffleAll` (~5308).

### Decision carry-forward
- [v27.0/SHUF] engine pure ON canonical / OFF q.Order / OFF≥2 round-robin + **guard paket kosong** (OFF sudah; SHF-01 = ON belum) — fix samakan ON ke OFF.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **OFF-path empty-package guard** (`ShuffleEngine.cs:53-57`) = template persis untuk fix ON-path (D-04).
- **CreateAssessment token uppercase** (~1107) = template write-side (D-01).
- **Sibling `[Authorize(Roles="Admin,HC")]`** (ResetAssessment dll) = pola authz (D-02).

### Established Patterns
- Defensive normalization (trim+upper) di gate; `[Authorize(Roles=...)]` di action; server-side validation reject + TempData message.

### Integration Points
- `VerifyToken` dikonsumsi worker token-entry (`Views/CMP/Assessment.cshtml`, input sudah force-uppercase client-side).
- `AddExtraTime` propagate via SignalR (`ExtraTimeMinutes`) — cap dicek server sebelum broadcast.
- `BuildCrossPackageAssignment` dipakai 3 caller (StartExam + ReshufflePackage + ReshuffleAll) — fix engine = 1 titik.
</code_context>

<specifics>
## Specific Ideas

- **TOK-01 compare** (defensif, null+trim-safe): `(assessment.AccessToken ?? "").Trim().ToUpper() == (token ?? "").Trim().ToUpper()`.
- **RST-04 cap**: `currentExtra + requested <= session.DurationMinutes` else reject.
- **SHF-01 ON-path**: filter `.Where(p => p.Questions != null && p.Questions.Count > 0)` sebelum `K = filtered.Min(p => p.Questions.Count)`; jika `filtered` kosong → return empty; StartExam: jika assignment kosong → block + message (no write).
- Test (dari audit E2E): #5 admin edit token lowercase → worker masuk sukses; #6 ≥2 paket satu kosong + shuffle ON → worker dapat soal > 0; + unit ShuffleEngine all-empty → empty + StartExam block; + reflection-authz AddExtraTime non-admin → 403.
</specifics>

<deferred>
## Deferred Ideas

### Reviewed Todos (not folded)
- **One-time cleanup data test/audit lokal pasca-367** (`.planning/todos/pending/2026-06-11-...md`) — chore pembersihan DB lokal, **tak terkait** scope fix Phase 380. Biarkan di pending.

(Milestone-level defer RES-02/GRD-02 dicatat di REQUIREMENTS.md, bukan phase ini.)
</deferred>

---

*Phase: 380-admin-engine-integrity*
*Context gathered: 2026-06-14*
