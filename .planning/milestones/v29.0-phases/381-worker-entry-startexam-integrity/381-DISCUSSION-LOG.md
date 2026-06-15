# Phase 381: Worker Entry (StartExam integrity) - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input plan/research/execute.
> Keputusan ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-14
**Phase:** 381-worker-entry-startexam-integrity
**Areas discussed:** Diskriminator pool Pre/Post, Determinisme reshuffle, Render impersonasi, Cakupan guard & VerifyToken

---

## Pemilihan Area (multiSelect)

User memilih SEMUA 4 area untuk didiskusikan.

---

## Diskriminator pool sibling Pre/Post (WSE-04)

| Option | Description | Selected |
|--------|-------------|----------|
| AssessmentType saja | `s.AssessmentType == assessment.AssessmentType`. Minimal & cukup; LinkedGroupId tak memisahkan Pre/Post (share nilai sama) | ✓ |
| AssessmentType + LinkedGroupId | Tambah isolasi antar-group edge Title+Category+Date identik | |

**User's choice:** AssessmentType saja
**Notes:** Temuan kode mengonfirmasi LinkedGroupId di-set sama untuk Pre & Post (`preSessions[0].Id`) → tidak bisa memisahkan; AssessmentType satu-satunya pemisah. Normal exam (Standard) tak terpengaruh.

---

## Determinisme reshuffle (scope filter) (WSE-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Extract helper + mirror reshuffle | Satu helper sibling dipakai StartExam + 3 endpoint reshuffle; filter identik; determinisme aman | ✓ |
| Mirror filter inline (no extract) | Tambah AssessmentType manual tiap query; risiko copy-paste drift | |
| StartExam-only + dokumen limitasi | Biarkan reshuffle; terima divergensi workerIndex combo langka; melanggar invariant LOCKED | |

**User's choice:** Extract helper + mirror reshuffle
**Notes:** Scope expansion eksplisit ke AssessmentAdminController reshuffle dicatat (D-03) sebagai konsekuensi-benar, bukan creep. Invariant Phase 373 (sibling-set+order StartExam = reshuffle) dijaga.

---

## Render ujian saat impersonasi (WSE-05)

| Option | Description | Selected |
|--------|-------------|----------|
| Assignment in-memory non-persist | Build shuffle di memori, no Add/SaveChanges; admin lihat preview read-only, zero mutasi | ✓ |
| Blokir render + pesan | Redirect 'mode lihat'; aman tapi tak bisa preview | |
| Render packages urutan-default | Soal urutan DB tanpa assignment; simpel tapi beda dari yang worker terima | |

**User's choice:** Assignment in-memory non-persist
**Notes:** Saat worker asli mulai → assignment ter-create persist normal (SC#3 timer dari nol). Stale-check (1065) tak terpengaruh (dijaga StartedAt!=null).

---

## Cakupan guard & VerifyToken (WSE-05)

| Option | Description | Selected |
|--------|-------------|----------|
| 3 write site, VerifyToken dibiarkan | Guard justStarted(962-967)+SignalR/log(969-978)+assignment(1012-1056); VerifyToken hanya TempData | ✓ |
| 3 write site + VerifyToken di-guard | Tambah guard kosmetik skip TempData; nilai marginal | |
| Minimal: assignment + StartedAt saja | Biarkan SignalR/log; TIDAK direkomendasikan (salah-notif HC monitor) | |

**User's choice:** 3 write site, VerifyToken dibiarkan
**Notes:** VerifyToken tak menulis DB worker (TempData session-scoped); guard tak perlu, alasan dicatat di CONTEXT D-05.

## Claude's Discretion

- Nama/signature/lokasi helper sibling.
- Stabilitas RNG preview impersonasi (seed stabil vs acak).
- Cara cabang in-memory mengonsumsi IsImpersonating().
- Wording pesan mode impersonate.

## Deferred Ideas

- Full PrePost pass/grade E2E (#4 lanjutan) = acceptance pasca-382.
- Reshuffle hygiene SHF-02/03, grading/cert (→382), Proton/essay/multi-answer.
- Cleanup data test lokal pasca-367 (todo pending, tak terkait).
