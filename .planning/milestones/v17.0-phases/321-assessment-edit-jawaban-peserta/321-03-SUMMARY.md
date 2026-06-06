---
phase: 321-assessment-edit-jawaban-peserta
plan: 03
type: execute
wave: 3
status: complete
completed_at: 2026-05-22
commits:
  - 1caac0ce
  - e1c7617c
  - aef805b4
  - 403a6e46
---

# PLAN 03 — Controller GET + View + JS + Preview (SUMMARY)

## Commits

| Hash | Message |
|------|---------|
| `1caac0ce` | feat(v17.0-p321): GET EditPesertaAnswers controller (Admin/HC gate + IsEditable check) |
| `e1c7617c` | feat(v17.0-p321): EditPesertaAnswers.cshtml + JS stub (UI-SPEC D-05/D-06 reconciled + A11y) |
| `aef805b4` | feat(v17.0-p321): edit-peserta-answers.js (dirty state + reason validation + D-06 flip modal + a11y focus restore) |
| `403a6e46` | feat(v17.0-p321): POST PreviewEditScore endpoint (dry-run flip detection, no DB mutation) |

## Endpoints

- `GET /Admin/EditPesertaAnswers/{id}` — render edit page (Admin/HC only, IsEditable gate)
- `POST /Admin/PreviewEditScore?sessionId={id}` — JSON dry-run preview

## Threat Mitigations Verified

| ID | Threat | Verification |
|----|--------|--------------|
| T-321-04 | Auth elevation | `[Authorize(Roles = "Admin, HC")]` di GET + POST PreviewEditScore (Worker → 403/redirect login) |
| T-321-05 | CSRF | `[ValidateAntiForgeryToken]` di POST + `@Html.AntiForgeryToken()` di view form |
| T-321-08 | Preview contract violation | Grep `PreviewEditScore` body untuk SaveChanges/Add/Update/Remove/ExecuteUpdateAsync/ExecuteDeleteAsync → **kosong** (no DB mutation) |

## Manual UAT Status

Smoke UAT browser SKIPPED per user decision (interactive mode option "Execute (skip smoke test)"). Build sebagai gate primary — 0 compile error setelah setiap task. User manual UAT akan dilakukan bareng PLAN 04 setelah POST SubmitEditAnswers existing (UAT submit Pass↔Fail full path).

## Files Created

- `Views/Admin/EditPesertaAnswers.cshtml` — 158 lines, A11y full (`aria-required`, `aria-labelledby`, `aria-describedby`)
- `wwwroot/js/edit-peserta-answers.js` — 166 lines, vanilla JS (no jQuery), snapshot+compare dirty detection

## Files Modified

- `Controllers/AssessmentAdminController.cs` — +2 actions (104 lines added, near `AssessmentMonitoringDetail` line 2761)

## UI Contract Honored

- D-05 verbose reason labels: "Soal salah / typo", "Kunci jawaban salah", "Bug sistem / glitch", "Permintaan koreksi peserta", "Lainnya (jelaskan)"
- D-06 flip modal copy: "menggagalkan peserta" / "meluluskan peserta" eksplisit
- A11y: modal `aria-labelledby="flipConfirmModalLabel"` + `aria-describedby="flipModalBody"` + focus restore via `hidden.bs.modal` once-listener

## JSON Contract PreviewEditScore

```json
{
  "oldScore": <int?>,
  "oldIsPassed": <bool?>,
  "newScore": <int>,
  "newIsPassed": <bool>,
  "hasCert": <bool>,
  "nomorSertifikat": <string?>,
  "willGenerateCert": <bool>
}
```

## Build Status

0 error, 22 warning (pre-existing) di tiap task commit.

## Handoff ke PLAN 04

- GET + PreviewEditScore READY untuk consume di JS preview flow.
- BELUM ada: `POST SubmitEditAnswers` (write path) — Form `editAnswersForm` action attr `asp-action="SubmitEditAnswers"` akan resolve setelah PLAN 04 Task 1 selesai.
- BELUM ada: Dropdown ⋮ entry di `AssessmentMonitoringDetail.cshtml` — link manual `/Admin/EditPesertaAnswers/{id}` sementara untuk UAT smoke.
- BELUM ada: SignalR handler `workerAnswerEdited` di `assessment-hub.js`.
- Manual UAT submit Pass↔Fail flow di-defer ke PLAN 04 ketika full path (POST + cascade + audit log) tersedia.

## Self-Check: PASSED

- 4 task committed atomically.
- T-321-04, T-321-05, T-321-08 mitigations grep-verified.
- 0 compile error per task.
- D-05/D-06 copy verified via grep.
