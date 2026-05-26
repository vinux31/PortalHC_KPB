# Phase 323: Fix Cascade Bug AssessmentEditLogs - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-05-26
**Phase:** 323-fix-cascade-bug-assessmenteditlogs-di-3-endpoint-delete-asse
**Areas discussed:** Insertion order cascade chain, Audit log description format, Smoke test method, Logging zero-edits case

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Insertion order cascade chain | Posisi block EditLogs di cascade chain (sebelum Responses / di tengah / mirror FK order) | ✓ |
| Audit log description format | Apakah audit description tambah EditLogsCount field | ✓ |
| Smoke test method (lokal) | Playwright E2E vs manual SQL+UI vs xUnit integration | ✓ |
| Logging zero-edits case | Skip log saat 0 edits vs selalu log | ✓ |

**User's choice:** Semua 4 area dipilih untuk dibahas.

---

## Area 1: Insertion Order Cascade Chain

**Original question:** "Posisi block `RemoveRange(AssessmentEditLogs)` di cascade chain?"

| Option | Description | Selected |
|--------|-------------|----------|
| Paling awal (sebelum PackageUserResponses) | Urutan: EditLogs → Responses → AttemptHistory → Packages → Session. Audit-trail dependency clean. | ✓ |
| Setelah AttemptHistory (sebelum Packages) | Group session-level data dulu, baru audit, baru struktur. | |
| Mirror urutan FK declaration order di model | Konsisten dengan model, no technical significance. | |

**User response (round 1):** "maksutnya apa ini saya tidak paham, jelaskan dengan simple"
**Re-explanation:** Diberikan analogi bongkar rumah + jelaskan 4 langkah cascade existing + apa itu AssessmentEditLogs (catatan riwayat admin edit soal dari Phase 321) + alasan posisi (audit trail snapshot dulu sebelum data parent dihapus).

**Re-ask (round 2):**
| Option | Description | Selected |
|--------|-------------|----------|
| Paling awal, sebelum semua cascade lain (Saran saya) | Block paling atas, sebelum hapus jawaban peserta. | ✓ |
| Bebas, terserah Claude (you decide) | Claude pilih rekomendasi default. | |
| Setelah hapus jawaban peserta, sebelum hapus paket soal | Block di tengah, group data peserta dulu. | |

**User's choice:** "Paling awal, sebelum semua cascade lain (Saran saya)"
**Notes:** User butuh penjelasan plain language; setelah analogi rumah diberi, langsung pilih saran. Decision lock D-01.

---

## Area 2: Audit Log Description Format

**Question:** "Audit log description — tambah info jumlah edit logs yang ikut terhapus?"

| Option | Description | Selected |
|--------|-------------|----------|
| Tambah `EditLogsCount=N` di description (Recommended) | Auditor masa depan bisa lihat berapa edit log ikut terhapus. | ✓ |
| Biarkan unchanged (no EditLogsCount) | Aman dari Success Criteria #4 strict interpretation. | |
| Bebas, terserah Claude (you decide) | Claude tambah `EditLogsCount=N`. | |

**User's choice:** "Tambah `EditLogsCount=N` di description (Recommended)"
**Notes:** Interpretasi Success Criteria #4 "tetap tercatat normal" = audit tetap ada (bukan unchanged). Decision lock D-02.

---

## Area 3: Smoke Test Method (Lokal)

**Question:** "Cara verifikasi 3 skenario smoke test lokal sebelum push?"

| Option | Description | Selected |
|--------|-------------|----------|
| Playwright E2E test (Recommended) | Login Admin, navigate, click Delete, assert success+DB. Repeatable. | ✓ |
| Manual SQL seed + UI click + sqlcmd verify | Seed via sqlcmd, klik manual, verify. Cepat tapi sekali pakai. | |
| xUnit integration test (controller-level) | Test panggil action method dengan in-memory DB. Cepat tapi skip HTTP layer. | |

**User response:** "playwright local ya?" (clarifying question)
**Re-explanation:** Confirm Playwright jalan lokal (port 5277), connect ke `dotnet run` di localhost, login admin@pertamina.com kredensial UAT dev, tidak menyentuh server Dev/Prod, DB lokal di-restore dari snapshot setelah test selesai.

**User's choice:** Playwright E2E test (implicit confirmation after clarification).
**Notes:** Seed temporary AssessmentEditLog untuk skenario (b) + (c) wajib ikut SEED_WORKFLOW (snapshot+journal+restore). Decision lock D-03.

---

## Area 4: Logging Zero-Edits Case

**Question:** "Kalau session 0 edit logs (kasus normal), log apa?"

| Option | Description | Selected |
|--------|-------------|----------|
| Skip log kalau 0 — ikuti pola existing (Recommended) | `if (editLogs.Any()) { LogInformation; RemoveRange; }` — sama pola PackageUserResponses Phase 312. | ✓ |
| Selalu log walaupun 0 | LogInformation tanpa guard. Debug-friendly tapi noise. | |
| Bebas, terserah Claude (you decide) | Claude ikut pola existing. | |

**User's choice:** "Skip log kalau 0 — ikuti pola existing (Recommended)"
**Notes:** Konsistensi pattern Phase 312, tidak introduce noise log production. Decision lock D-04.

---

## Claude's Discretion

- Variable naming `preDeleteEditLogsCount` (ikut pola `preDeleteResponseCount`)
- Comment header `// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)`
- Atomic commit strategy: 1 commit covering 3 endpoint + 1 commit untuk Playwright test (planner boleh override)

## Deferred Ideas

- Refactor `CascadeAssessmentSessionDependents` helper (out-of-scope per REQUIREMENTS.md L33)
- Audit endpoint delete lain (out-of-scope per REQUIREMENTS.md L32)
- Migration FK Restrict → Cascade DB-level (out-of-scope per REQUIREMENTS.md L34)
- UI filter old assessment (separate UX issue, backlog)
