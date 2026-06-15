# Phase 385: Exam-Taking & Image Render Hotfix - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-06-15
**Phase:** 385-exam-taking-image-render-hotfix
**Areas discussed:** F-09 lokasi fix, F-21 mekanisme flush, F-21 jalur timeout, Verifikasi

---

## F-09 lokasi fix

| Option | Description | Selected |
|--------|-------------|----------|
| Render-time di _QuestionImage | Bungkus src+data-img-src Url.Content. 1 file, tak ubah data | ✓ (re-verified) |
| Storage-time FileUploadHelper | Ubah path + backfill DB lama | |
| base href di Layout | Risiko efek samping | |

**User's choice:** "check ulang sesuai reco" → re-verifikasi render-time. **Hasil:** CONFIRMED — `_QuestionImage.cshtml` L38 `src` + L45 `data-img-src` mentah; path leading-slash dari FileUploadHelper.cs:107; `Url.Content("~"+path)` benar, tak ada dobel-prefix. Locked render-time.

---

## F-21 mekanisme flush

| Option | Description | Selected |
|--------|-------------|----------|
| Flush sebelum submit + save on blur | Flush pending + await; blur handler; tetap debounce | ✓ |
| Essay masuk form POST | Server baca dari form | |
| Gabung keduanya | JS + form POST | |

**User's choice:** Flush sebelum submit + save on blur.

---

## F-21 jalur timeout

| Option | Description | Selected |
|--------|-------------|----------|
| Best-effort flush lalu submit | Tembak save, tak menunggu; pastikan baris essay ada; tak menunda deadline | ✓ |
| Tunggu flush selesai baru submit | Blokir submit sampai sukses | |

**User's choice:** Best-effort flush lalu submit.

---

## Verifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Prefix lokal + Playwright + Dev UAT | localhost prefix + Playwright; UAT Dev oleh IT | ✓ |
| Cukup unit/code-level | UAT diserahkan IT penuh | |

**User's choice:** unit/code + Playwright local, UAT Dev nanti oleh IT.

## Claude's Discretion
- Detail JS flush, helper path-resolve inline, struktur test.

## Deferred Ideas
- F-22 (SaveTextAnswer timer guard), F-20 (MC null-overwrite), refactor save-engine — Future.
