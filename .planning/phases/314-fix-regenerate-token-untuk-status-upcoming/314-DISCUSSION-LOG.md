# Phase 314: Fix Regenerate Token untuk Status Upcoming - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-08
**Phase:** 314-fix-regenerate-token-untuk-status-upcoming
**Areas discussed:** Repro & investigation strategy, Patch philosophy, Frontend error UX format, Smoke test mechanism, Concurrency / transaction wrap, Logging telemetry detail format SC #4, Behavior token regen saat ada session running

---

## Repro & Investigation Strategy

### Q1: Bagaimana strategi repro bug di lokal?

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid (seed lokal + fallback DB Dev) | Coba bersih dulu, escalate kalau perlu | |
| Seed lokal fresh | Buat fixture lokal saja | |
| Snapshot DB Dev | Restore subset DB Dev | |
| **Custom (user-specified)** | **Repro via URL Dev (10.55.3.3/KPB-PortalHC), buat assessment sendiri, peserta admin@pertamina.com** | ✓ |

**User's choice:** "check nya via web dev saja, url nya sudah tahu kan?. nah nanti untuk token assessmentnya kamu bisa buat assessmentnya sendiri. dengan pesertanya admin juga"
**Notes:** User clarification — repro langsung di Dev (sesuai CLAUDE.md tahap 1), admin sebagai peserta sendiri (Admin + Worker dual-role).

### Q2: Hipotesis mana yang di-investigate dulu?

| Option | Description | Selected |
|--------|-------------|----------|
| **Capture exception dari log server dulu (Recommended)** | Stacktrace pinpoint root cause | ✓ |
| Iterate semua 4 hipotesis paralel | Validate satu per satu | |
| Audit log FK / concurrency dulu | Highest-likelihood per analisa kode | |
| Capture exception dulu, baru hipotesis (asli) | Fokus repro → server log | (merged ke #1) |

**User's choice:** Capture exception dari log server dulu (Recommended)
**Notes:** User minta analisa+rekomendasi → saya recommend capture exception (efisien). User setuju.

### Q3: Format RESEARCH.md — apa yang harus didokumentasi?

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal — root cause + fix proposal | Hanya hipotesis terbukti | |
| **Standard — 4 hipotesis + status verifikasi** | Tabel Evidence/Status (CONFIRMED/RULED OUT/INCONCLUSIVE) | ✓ |
| Comprehensive — add reproduction guide + patches | Standard + reproduction steps + alternatives | |

**User's choice:** Standard — 4 hipotesis + status verifikasi
**Notes:** —

### Q4: Plan structure — split per ROADMAP atau merge?

| Option | Description | Selected |
|--------|-------------|----------|
| **Split 01 (Repro+RESEARCH) + 02 (Patch+test)** | Sesuai ROADMAP literal | ✓ |
| Single plan — repro + patch + test combined | Atomic flat plan | |
| Split 01 (Patch backend) + 02 (Frontend+test) | Domain-split | |

**User's choice:** Split 01 + 02 sesuai ROADMAP
**Notes:** —

---

## Patch Philosophy

### Q5: Setelah root cause ditemukan, scope patch backend?

| Option | Description | Selected |
|--------|-------------|----------|
| **Defensive coverage 4 hipotesis (Recommended)** | Fix root cause + guard 3 hipotesis lain | ✓ |
| Minimal targeted | Hanya hipotesis terbukti | |
| Targeted + 1 high-confidence guard | Sweet spot | |

**User's choice:** Defensive coverage 4 hipotesis
**Notes:** —

### Q6: Audit log atomicity — kalau audit log gagal, behavior?

| Option | Description | Selected |
|--------|-------------|----------|
| **Try-catch swallow (token tetap regen sukses)** | Pattern Phase 306 D-10 | ✓ |
| Atomic transaction — rollback semua | Konsisten tapi user-impact tinggi | |
| Two-phase — audit dulu, baru token | Test write upfront | |

**User's choice:** "check dulu, dan sesuai saran kamu" → Try-catch swallow (saya recommend per Phase 306 D-10 pattern, user setuju)
**Notes:** Saya analisa → konsisten Phase 306 D-10, business action protect, audit failure rare → swallow. User setuju.

### Q7: Frontend response handler — patch apa?

| Option | Description | Selected |
|--------|-------------|----------|
| **Fix .catch() agar parse server error body (Recommended)** | Throw error dengan data.message di .then chain | ✓ |
| Add response.ok check + propagate detail | Robust untuk non-JSON 5xx | |
| Both — ok check + json fallback + catch | Defense in depth | (merged into D-11) |

**User's choice:** Fix .catch() (Recommended) → ditingkatkan dengan D-11 untuk non-JSON 5xx
**Notes:** Final implementation = combine D-07 (parse server body) + D-11 (response.ok + r.text() fallback).

### Q8: 3 view yang panggil RegenerateToken — patch semua atau selektif?

| Option | Description | Selected |
|--------|-------------|----------|
| **Patch 3 view sekaligus (Recommended)** | Monitoring + MonitoringDetail + ManageAssessment | ✓ |
| Hanya 2 view di ROADMAP | Skip ManageAssessment.cshtml | |
| Extract helper JS function | DRY tapi scope creep | |

**User's choice:** Patch 3 view sekaligus
**Notes:** ManageAssessment.cshtml line 456 ditemukan saat scout, tidak di-mention ROADMAP eksplisit tapi konsisten UX butuh inclusion.

---

## Frontend Error UX Format

### Q9: Format error message saat RegenerateToken gagal?

| Option | Description | Selected |
|--------|-------------|----------|
| **Tetap alert() native + propagate server message (Recommended)** | Minimal diff, existing pattern | ✓ |
| Toast Bootstrap (alert-warning) | Modern UX, scope creep | |
| Existing toast helper kalau ada | Cek codebase dulu | |

**User's choice:** alert() native + propagate server message
**Notes:** —

### Q10: Wording error message untuk user — detail seberapa?

| Option | Description | Selected |
|--------|-------------|----------|
| **Server message langsung (technical, recommended)** | Passthrough untuk Admin/HC role | ✓ |
| User-friendly only | Sembunyikan technical detail | |
| Hybrid generic + 'Detail' button | Compromise tapi alert() tidak support button | |

**User's choice:** Server message langsung
**Notes:** —

### Q11: Scenario non-JSON 5xx response — handle bagaimana?

| Option | Description | Selected |
|--------|-------------|----------|
| **Detect via response.ok, fallback r.text() (Recommended)** | Robust handling | ✓ |
| Hanya parse JSON, fail show generic | Simple tapi kehilangan context | |
| Show HTTP status code | Status code aja | |

**User's choice:** response.ok + r.text() fallback
**Notes:** —

### Q12: Error message di server side — apa yang di-return?

| Option | Description | Selected |
|--------|-------------|----------|
| **Specific by exception type (Recommended)** | DbUpdateException/NullReference/generic branches | ✓ |
| Generic + log detail | Aman security tapi tidak informatif | |
| Include exception message tapi sanitize | Manual sanitize per type | |

**User's choice:** Specific by exception type
**Notes:** —

---

## Smoke Test Mechanism

### Q13: Mechanism smoke test 3 skenario?

| Option | Description | Selected |
|--------|-------------|----------|
| **Playwright E2E dengan dedicated fixture (Recommended)** | Phase 312/313 pattern | ✓ |
| Manual UAT checklist | Cepat tapi tidak regression-proof | |
| Hybrid — Playwright happy + manual edge | Compromise | |

**User's choice:** Playwright E2E
**Notes:** —

### Q14: Seeding fixture untuk Playwright?

| Option | Description | Selected |
|--------|-------------|----------|
| DB manipulation back-dated/forced state | Phase 313 D-07 pattern | |
| **Buat via UI Admin di setup hook** | Real flow, ~30s setup | ✓ |
| SQL script terpisah | External dependency | |

**User's choice:** UI Admin setup hook (deviation dari Phase 313 pattern)
**Notes:** User pilih real flow — verify UI Admin tidak break sambil seed fixture.

### Q15: Test assertion — verify apa?

| Option | Description | Selected |
|--------|-------------|----------|
| **Token + sibling DB + AuditLog row + UI alert (Recommended)** | End-to-end coverage | ✓ |
| Hanya UI assert | Tidak verify backend | |
| Backend-only via API call | Skip JS handler patch | |

**User's choice:** Comprehensive
**Notes:** —

### Q16: Dimana Playwright test file ditempatkan?

| Option | Description | Selected |
|--------|-------------|----------|
| **tests/e2e/admin-assessment-token.spec.ts (NEW, Recommended)** | Konsisten Phase 312 pattern | ✓ |
| Append ke admin-assessment.spec.ts existing | Less file proliferation | |
| tests/e2e/regression/phase-314.spec.ts | Phase-namespaced | |

**User's choice:** NEW file
**Notes:** —

---

## Concurrency / Transaction Wrap

### Q17: Concurrency guard untuk loop sibling update?

| Option | Description | Selected |
|--------|-------------|----------|
| **Wrap BeginTransactionAsync (Recommended)** | Atomicity loop multi-row | ✓ |
| Tidak perlu — SaveChangesAsync sudah atomic | Acceptable as-is | |
| Concurrency token (RowVersion) | Schema change | |

**User's choice:** Wrap BeginTransactionAsync
**Notes:** —

### Q18: Re-fetch sebelum update (TOCTOU guard)?

| Option | Description | Selected |
|--------|-------------|----------|
| **Tidak perlu — acceptable race window** | Last-writer-wins safe | ✓ |
| Add re-fetch + version check inside transaction | Phase 312 WR-01 pattern | |
| Pessimistic lock WITH (UPDLOCK) | SQL Server-specific | |

**User's choice:** "check dulu. sesuai saran kamu" → Tidak perlu (saya recommend per analisa: race window narrow + last-writer-wins safe + admin action rare)
**Notes:** Saya analisa Phase 312 WR-01 context (delete high-concurrency) berbeda dengan RegenerateToken (admin rare action). User setuju.

---

## Logging Telemetry Detail (SC #4)

### Q19: Definisi 'hasStarted' field di logging?

| Option | Description | Selected |
|--------|-------------|----------|
| **Cek apakah ADA sibling dengan StartedAt!=null (Recommended)** | Boolean aggregate | ✓ |
| Cek StartedAt single row | Single row may misleading | |
| Count sessions started | Numeric granular | |

**User's choice:** Boolean aggregate `siblings.Any(s => s.StartedAt != null)`
**Notes:** —

### Q20: Format logging SC #4 — exact ROADMAP atau extended?

| Option | Description | Selected |
|--------|-------------|----------|
| **Extended structured logging (Recommended)** | + siblingCount, isTokenRequired | ✓ |
| Exact ROADMAP literal (3 field) | Minimal scope | |
| Add correlation ID + RequestId | Cross-reference tapi scope creep | |

**User's choice:** Extended structured
**Notes:** —

### Q21: Logging selain LogError di catch — log point lain?

| Option | Description | Selected |
|--------|-------------|----------|
| **LogInformation di success path (Recommended)** | Audit operasional + observability | ✓ |
| Hanya LogError di catch | Audit cover semuanya | |
| LogWarning untuk validation reject | Detect misuse | |

**User's choice:** LogInformation di success path
**Notes:** —

---

## Behavior Token Regen saat Open + Active Worker

### Q22: Behavior saat regen Status=Open dengan worker active?

| Option | Description | Selected |
|--------|-------------|----------|
| **Allow regen tapi warn admin di confirm dialog (Recommended)** | Frontend confirm + server proceed | ✓ |
| Block regen saat Open + worker active | Mengubah behavior, regression risk | |
| Allow regen tanpa warning (existing) | Tetap behavior existing | |

**User's choice:** Allow + warn dialog
**Notes:** —

### Q23: Wording warning specific?

| Option | Description | Selected |
|--------|-------------|----------|
| **Konfirmasi dengan jumlah worker affected** | "{N} worker sudah masuk ujian..." | ✓ |
| Konfirmasi singkat tanpa jumlah | Generic | |
| Skip warning | Not applicable | |

**User's choice:** Wording dengan jumlah worker
**Notes:** Frontend butuh sibling startedCount → inline data attribute atau lightweight GET endpoint.

### Q24: Skenario test 'Open running' — verify apa?

| Option | Description | Selected |
|--------|-------------|----------|
| **Token regen + worker dapat error 'token invalid' (Recommended)** | End-to-end behavior | ✓ |
| Token regen sukses, worker session aktif | Worker session preserved | |
| Skip Open running test | Focus Upcoming saja | |

**User's choice:** Token regen + worker error
**Notes:** Asumsi behavior token invalidate worker session perlu di-konfirmasi research (cookie-based session vs token-based) sebelum tulis test.

---

## Claude's Discretion

- **Stacktrace parsing logic** untuk RESEARCH.md format adjustment
- **Error wording kontekstual** SC #5 phrasing bahasa Indonesia
- **Spinner/disable button visual** saat regen in-flight (existing pattern di Detail view)
- **Test fixture cleanup** strategy (afterEach delete vs leave-as-is)
- **Inline `data-started-count` rendering** vs lightweight GET endpoint untuk D-23 wording

## Deferred Ideas

- TOCTOU re-fetch + version check (D-18 alternative)
- Concurrency token (RowVersion / [Timestamp]) — schema change
- Pessimistic lock SQL `WITH (UPDLOCK)` — SQL Server-specific
- Toast/Bootstrap banner UX (D-09 alternative) — modern UX scope creep
- Extract reusable JS helper function (D-08 alternative) — refactor scope
- Server-side cookie/session invalidate on token regen (D-24 followup)
- `HttpContext.TraceIdentifier` correlation ID di logging (D-20 alternative)
- Hybrid Playwright + manual UAT (D-13 alternative)
