# Flow Sistem Sertifikat §J Audit Findings — Design Spec

**Tanggal:** 2026-05-27
**Output file:** `docs/sertifikat-ecosystem/flow-sistem-sertifikat.html` (modify — add §J section)
**Audience:** Staff HC + IT/Dev Pertamina (memperluas audience awam → mixed dengan technical reader untuk section ini)
**Tujuan:** Extend `flow-sistem-sertifikat.html` v1.0 dengan section §J Audit Findings & Maturity — surface deep technical audit (race condition, fire-and-forget, zero unit test, missing observability) yang belum ter-cover di 50 gap + 25 flow existing.

---

## 1. Latar Belakang

Audit deep (caveman-investigator agent 2026-05-27, post-shipped flow-sistem v1.0) menemukan **temuan teknis BARU** yang belum ada di dokumentasi existing:

- 50 gap di `analisa-gap-benchmark.html` cover feature gap (capability missing)
- 25 flow di `flow-sistem-sertifikat.html` cover operational flow
- Audit baru cover **CODE-LEVEL findings**: race condition, anti-pattern, schema issue, test coverage, operational maturity

**Maturity overall: 2.5/5** — siap Dev/UAT, BELUM siap audit BPK / production-grade.

3 KRITIS:
1. Race condition ET Score insert (`GradingService.cs:178`)
2. Fire-and-forget audit log (`CMPController.cs:1763`)
3. ZERO unit test untuk core service layer

11 PENTING + 7 strengths confirmed.

Dokumen ini surface ke audience HC + Dev sebagai pengingat "lubang teknis yang belum di-cover dokumen lain".

---

## 2. Scope

### IN SCOPE

- Add 1 section baru `§J` di akhir `flow-sistem-sertifikat.html` (setelah `§H+I`)
- Update mini-nav: tambah link `§J Audit` (jadi 10 link)
- Update footer versi: `1.0` → `1.1`
- Bump audit source attribution di footer
- ~120-150 baris HTML tambahan (488 → 608-638 baris)

### OUT OF SCOPE

- Tidak update master diagram `§0` (keep simple, audit cross-cutting bukan lifecycle phase)
- Tidak split jadi file terpisah (`audit-deep-v1.0.html`) — keep single source of truth
- Tidak ulang 50 gap atau 25 flow detail
- Tidak provide remediation code (hanya findings + rekomendasi effort)
- Tidak update file companion lain (`overview-awam.html`, `analisa-gap-benchmark.html`, `ekosistem-sertifikat.html`) di patch ini — defer ke v1.2

---

## 3. Struktur Konten §J

### §J Intro

Paragraph + verdict singkat:
- Penjelasan audit deep dilakukan post-shipped flow-sistem v1.0
- Sumber: caveman-investigator agent 2026-05-27
- Verdict: "Siap Dev/UAT, BELUM siap audit BPK / production-grade tanpa hardening 8-10 minggu"

### §J.1 Maturity Scorecard

6 mini-card grid (2×3 atau 3×2 responsive):
- Code Quality — **2/5** — Race condition + fire-and-forget + DTO no validation
- Database/Schema — **3/5** — Index OK, missing composite + NoAction orphan
- Security — **4/5** — Strong, minor edge case
- Test Coverage — **1/5** — ZERO unit test
- Operational — **2/5** — No structured log, no /health, no /metrics
- Documentation — **3/5** — ARCHITECTURE.md ada, missing ADR

Aggregate: **2.5/5** badge + 1 paragraph context.

### §J.2 3 Temuan Kritis

3 `gap-callout` merah parallel (col-md-4 grid):
1. **Race Condition ET Score Insert** — `GradingService.cs:178` — 2 request simultan bisa duplicate insert
2. **Fire-and-Forget Audit Log** — `CMPController.cs:1763` — exception swallow, audit log bisa hilang silent
3. **ZERO Unit Test** — entire `Services/` — core state machine untested

Tiap callout: nama issue + path:line + 2-3 kalimat risiko awam.

### §J.3 11 Temuan Penting (Tabel Compact)

Tabel scroll-responsive 4 kolom (Issue / Lokasi `path:line` / Risiko / Severity badge 🟡):
1. DTO no validation — Models/EditAnswersSubmission.cs
2. Loose exception handling — Services/NotificationService.cs (7×)
3. N+1 query batch — Controllers/RenewalController.cs:246
4. Optimistic lock 1-detik grace — Controllers/AssessmentAdminController.cs:2837
5. Missing composite index — Data/ApplicationDbContext.cs:180-188
6. NoAction FK orphan risk — Data/ApplicationDbContext.cs:152-170,214-232
7. Notif MarkAsRead no user context — Services/NotificationService.cs:152-175
8. No PII masking audit log — Controllers/AssessmentAdminController.cs:2915-2925
9. Plain logging (no Serilog) — appsettings.json
10. No /health endpoint — startup
11. No /metrics endpoint — startup

### §J.4 Strengths

Alert success dengan 7 bullet:
- ✅ Zero raw SQL (LINQ-to-EF auto-parameterized)
- ✅ CSRF protected (`[ValidateAntiForgeryToken]` semua POST admin)
- ✅ Authorization granular (`[Authorize(Roles=...)]` + ownership check)
- ✅ 67 HasIndex + filtered index
- ✅ Check constraints (Progress 0-100, PassPercentage 0-100, DurationMinutes ≥0)
- ✅ Scoped DI (no singleton state leak)
- ✅ Phase 323 cascade hardening DeleteAssessment

### §J.5 Rekomendasi Prioritas Hardening

Tabel 9-baris (Prioritas # / Action / Effort estimate):
1. Wajib — Unit test suite GradingService + NotificationService + RenewalChain (60% coverage min) — 4-6 minggu
2. Wajib — Fix race condition (DB transaction + unique constraint) — 1 minggu
3. Wajib — Fix fire-and-forget (IHostedService background channel) — 3 hari
4. Wajib — `/health` + `/metrics` + Serilog + correlation ID — 1 minggu
5. Penting — DTO validation `[Required]`/`[MaxLength]` — 2 hari
6. Penting — Composite index `(Status, ValidUntil)` migration — 1 hari
7. Penting — NoAction FK application orphan cleanup — 3 hari
8. Penting — Optimistic lock EF row version — 2 hari
9. Penting — NotificationService MarkAsRead pakai `HttpContext.User` — 1 hari

Footer alert: "**Total estimasi 8-10 minggu hardening** untuk production-ready audit BPK-grade."

---

## 4. Style Decisions

| Aspek | Pilihan | Alasan |
|-------|---------|--------|
| Section style | Konsisten existing (h2 badge + intro + sub-h5) | Match pattern §A-§I |
| 3 Kritis | `gap-callout` (merah) col-md-4 parallel | Match style §H+I |
| 11 Penting | Tabel scroll-responsive 4 kolom | Match style detail flow per fase |
| Scorecard | Card grid badge color (1-2=danger, 3=warning, 4=info, 5=success) | Visual maturity at-a-glance |
| Strengths | `.alert-success` dengan bullet list | Standard Bootstrap pattern |
| Rekomendasi | Tabel 9-baris dengan effort badge | Konsisten flow detail table |
| Print | Section inherit print CSS existing (page-break-before, color-adjust:exact) | No tambahan rule |

---

## 5. Mini-Nav Update

Existing: 9 link (`§0 Master` + `§A` .. `§I`).
After: 10 link, tambah `§J Audit` setelah `§H+I`.

---

## 6. Footer Update

Existing:
```
Versi 1.0 · 2026-05-27
Audit codebase: 25 flow distinct (22 ada + 3 gap kritis). Sumber: caveman-investigator agent 2026-05-27.
```

After:
```
Versi 1.1 · 2026-05-27 (patch: +§J Audit Findings)
Audit codebase: 25 flow distinct (22 ada + 3 gap kritis) + 14 audit findings (3 kritis + 11 penting). Sumber: caveman-investigator agent 2026-05-27 (2× run: flow + maturity audit).
```

---

## 7. Success Criteria

Staff HC + Dev bisa:
1. Buka file, click mini-nav `§J Audit` → langsung scroll ke section audit
2. Lihat scorecard 6 kategori dalam 5 detik (visual color badge)
3. Pahami 3 issue paling kritis tanpa baca code (callout awam)
4. Lihat 11 penting + lokasi `path:line` untuk dev follow-up
5. Lihat rekomendasi 9 action dengan estimasi effort untuk planning meeting

---

## 8. Implementation Notes

- 1 file modify only (`flow-sistem-sertifikat.html`)
- Append §J di posisi sebelum `</footer>` opening
- Estimasi 2 task atomic: Task 1 = add §J content + mini-nav update, Task 2 = footer version bump + visual verify browser
- Visual verify via Playwright HTTP server (re-use pattern session sebelumnya)
- Commit per task atomic
- No test framework (konsisten file lain di folder)
