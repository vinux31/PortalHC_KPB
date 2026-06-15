---
phase: 385
slug: exam-taking-image-render-hotfix
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-15
---

# Phase 385 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Hotfix render-time (PXF-01 image PathBase) + JS-side essay flush (PXF-03). No DB/Hub/Controller change (migration=FALSE).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| stored image path → rendered `<img src>` | path tersimpan di DB di-emit ke atribut HTML via partial `_QuestionImage.cshtml` | path file gambar (server-generated, non-sensitive) |
| client JS essay flush → `Hub.SaveTextAnswer` | input essay peserta menyeberang ke server saat flush/blur/submit | teks jawaban essay peserta (own session) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-385-01 | Information Disclosure / Tampering | `_QuestionImage.cshtml` `Url.Content("~"+path)` | mitigate | `Url.Content` hanya prepend PathBase + URL-encode; tak buka path traversal. Source path disanitasi `FileUploadHelper.cs:90` (`Path.GetFileName`) + filename generated (L100) + leading-slash terkontrol (L107). Verified di commit `c5b0a478` (L39-40 `Url.Content("~"+p)`). | closed |
| T-385-02 | Tampering (XSS via path) | atribut `src`/`data-img-src` | accept | imagePath dari upload server-side (filename generated `yyyyMMddHHmmssfff_uniqueId_originalName`), bukan free-text user; Razor `@` auto-encode atribut. Low risk. | closed |
| T-385-03 | Elevation / Tampering | `Hub.SaveTextAnswer` dipanggil ekstra (flush/blur) | accept | Hub sudah cek ownership (`s.UserId == userId`) + `Status == "InProgress"` (AssessmentHub.cs:143-149) + truncate MaxChars (L157-159). Flush hanya menambah pemanggilan endpoint SAMA yang sudah ter-authz — tak buka jalur baru. Tak di-bypass. | closed |
| T-385-04 | Tampering (post-deadline write) | flush saat timeout menulis essay | accept | F-22 (guard timer di SaveTextAnswer) di-DEFER (LOW, pasca-acara). Timeout flush best-effort happy-path; window di luar deadline sangat sempit (submit fire bersamaan). Diterima untuk hotfix. | closed |
| T-385-05 | DoS (submit hang) | guard pre-submit/changePage menunggu `essayInFlight` | mitigate | Guard tetap punya fallback 5s `SAVE_TIMEOUT_MS` (`navTimeout` L970/977 + `rTimeout` L1032) — submit/navigasi tak pernah hang permanen. Timeout-path flush fire-and-forget (no await, tak blokir deadline). Verified di commit `242e8d2e`. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-385-01 | T-385-02 | imagePath server-generated (bukan free-text), Razor auto-encode; XSS via path low-risk untuk hotfix | Rino | 2026-06-15 |
| AR-385-02 | T-385-03 | Flush re-use endpoint Hub yang sudah ter-authz (ownership + InProgress + truncate); tak buka jalur baru | Rino | 2026-06-15 |
| AR-385-03 | T-385-04 | F-22 deferred (LOW); window post-deadline sangat sempit, flush best-effort; diterima untuk hotfix pra-ujian | Rino | 2026-06-15 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-15 | 5 | 5 | 0 | Claude (inline verify, /gsd-secure-phase) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-15
