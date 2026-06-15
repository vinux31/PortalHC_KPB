---
phase: 371
slug: sesi-online-tampil-di-tab-input-records-visibility-only
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-12
---

# Phase 371 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

Phase = visibility-only display loosening (1 file Razor view `Views/Admin/Shared/_TrainingRecordsTab.cshtml`, migration=false, code `d1d03e13`). Tanpa endpoint baru, tanpa perubahan autentikasi, tanpa write-path baru. Verifikasi mitigasi oleh gsd-security-auditor terhadap kode aktual (4/4 CLOSED).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser (admin/HC) → GET /Admin/ManageAssessmentTab_Training | Request admin terautentikasi; route `[Authorize(Roles="Admin, HC")]` existing (tak berubah Phase 371) | Daftar record training/manual/online per worker dalam scope filter Bagian/Unit admin |
| View tombol "Lihat hasil" → GET /CMP/Results?id={sessionId} | Link client menuju endpoint server-gated; visibility tombol ≠ kontrol akses | AssessmentSession.Id (int) ke endpoint hasil assessment |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-371-01 | Information Disclosure | onlineRows render — sesi online belum-selesai terlihat admin/HC | accept | By design (D-01). Route `[Authorize(Roles="Admin, HC")]` tak berubah; view hanya membaca `worker.AssessmentSessions` yang sudah di-load service per-section existing. Zero endpoint baru, zero perluasan scope data. | closed |
| T-371-02 | Elevation of Privilege (IDOR) | Tombol "Lihat hasil" → `CMP/Results?id={sessionId}` | mitigate | Server-side `IsResultsAuthorized(...)` [CMPController.cs:2229] + `IsAssessmentSubmitted(Status)` [CMPController.cs:2233] gate Forbid/redirect. View `CanViewResult = (Status==PendingGrading \|\| CompletedAt!=null)` [_TrainingRecordsTab.cshtml:302], tombol hanya dirender saat true [:372-378]. Ubah id ke sesi tak-berwenang tetap di-block server. Tombol = kenyamanan, otorisasi server-side. | closed |
| T-371-03 | Tampering (XSS) | Render `@row.Title` / `@row.Detail` data online | mitigate | `@row.Title`/`@row.Detail` [_TrainingRecordsTab.cshtml:358] = `@expression` Razor auto HTML-encode (bukan `Html.Raw`). Branch online [:368-379] TIDAK render `hx-vals`/`antiToken`/interpolasi atribut. `antiToken` hanya di branch Training [:389] + Manual [:404], bukan online. Zero `Html.Raw` di path render online. | closed |
| T-371-04 | Spoofing | Autentikasi route | accept | Tak ada perubahan autentikasi. View partial dari action `ManageAssessmentTab_Training` tetap `[Authorize(Roles="Admin, HC")]` existing. Phase = display loosening murni, zero auth change. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-371-01 | T-371-01 | Admin/HC sudah authorized lihat record worker dalam scope filter Bagian/Unit; menampilkan sesi online belum-selesai tidak menambah ekspos data luar-scope (service load per-section existing). By-design visibility goal URG-03. | Rino (developer) + gsd-security-auditor | 2026-06-12 |
| AR-371-02 | T-371-04 | Phase view-only; route auth `[Authorize(Admin, HC)]` existing tak disentuh. | Rino (developer) + gsd-security-auditor | 2026-06-12 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-12 | 4 | 4 | 0 | gsd-security-auditor (sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-12
