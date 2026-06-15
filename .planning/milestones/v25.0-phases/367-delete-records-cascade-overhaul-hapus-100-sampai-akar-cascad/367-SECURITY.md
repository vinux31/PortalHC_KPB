---
phase: 367
slug: delete-records-cascade-overhaul
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-13
---

# Phase 367 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Delete Records Cascade Overhaul — hapus 100% sampai akar, no-blocker, UI jujur.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Admin/HC browser → delete endpoints (POST) | Authenticated POST memicu cascade rekursif high-impact (DeleteAssessment/Group/PrePost tab-1; DeleteTraining/DeleteManualAssessment tab-2) | `id`, `type`, `mirrorTrainingIds`, antiforgery token |
| Admin/HC browser → GET DeletePreview / badge | Read-only preview pohon korban cascade + badge per-worker | `type`, `id` |
| Controller → RecordCascadeDeleteService | Aktor + ids diteruskan ke engine cascade (1-tx) | actorId, rootType, rootId, mirror-ids |
| Endpoint/engine → filesystem (post-commit) | File.Delete sertifikat (#19) + image soal 366 (Opsi B) | confined webroot path |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-367-01 | Tampering | CollectCascadeIds rootType arbitrer | mitigate | Whitelist rootType ∈ {session,training}; ArgumentException (V5) | closed |
| T-367-02 | DoS | Traversal cycle infinite loop | mitigate | HashSet `visited` cycle-guard | closed |
| T-367-03 | Info disclosure | Preview bocorkan data user lain | accept | Preview admin/HC-only `[Authorize]`; mirror-ID divalidasi milik-user saat execute | closed |
| T-367-04 | Tampering | Mutasi tak sengaja di preview | mitigate | BuildPreviewAsync read-only (zero SaveChanges/RemoveRange) | closed |
| T-367-05 | Elevation (IDOR) | mirror-IDs ter-inject di param | mitigate | Validasi FindMirrorCandidates (UserId==session.UserId) server-side sebelum hapus | closed |
| T-367-06 | Tampering/DoS | Over-deletion cascade nyasar | mitigate | preview==execute (CollectCascadeIds sama) + 1-tx rollback | closed |
| T-367-07 | Info disclosure | ex.Message bocor ke caller | mitigate | CascadeResult.ErrorMessage generik; detail ke _logger (V7) | closed |
| T-367-08 | Tampering | Path traversal File.Delete | mitigate | Path.Combine(WebRootPath, TrimStart('/')) confined (V12) | closed |
| T-367-09 | Repudiation | Hapus tanpa jejak | mitigate | AuditLog 1 entri/operasi + soft-cancel PendingBypass | closed |
| T-367-10 | Info disclosure | Badge bocorkan count worker lain | accept | Tab admin/HC-only existing authz; scope filter admin sah | closed |
| T-367-11 | Tampering | Formula badge salah | mitigate | BadgeRecomputeTests angka==baris tampil; read-only | closed |
| T-367-12 | Tampering (over-deletion) | DeleteAssessmentGroup sibling over-match | mitigate | StandardGroupSiblingPredicate (LinkedGroupId==null && !Pre/Post && !manual) | closed |
| T-367-13 | Tampering | ResetAssessment reset record manual | mitigate | IsResettable guard tolak IsManualEntry; test reject | closed |
| T-367-14 | Info disclosure | Pesan error bocorkan detail | mitigate | Pesan ramah (V7), no ex.Message | closed |
| T-367-15 | Spoofing/CSRF | POST tanpa token (engine read-only plan) | mitigate | `[ValidateAntiForgeryToken]`+`[Authorize(Admin,HC)]` preserved | closed |
| T-367-16 | Tampering (over-deletion) | Cascade tab-1 nyasar saat no-blocker | mitigate | Engine ExecuteAsync (preview==execute) + cycle guard + 1-tx + sibling filter #18 | closed |
| T-367-17 | Tampering | Path traversal File.Delete cert | mitigate | Confined webroot (V12); DeleteCertFiles AdminBaseController | closed |
| T-367-18 | Repudiation | Hapus cascade tab-1 tanpa jejak | mitigate | AuditLog 1 entri/operasi (engine CascadeDelete + endpoint) | closed |
| T-367-19 | Tampering | Image 366 hilang/dobel saat refactor | mitigate | Opsi B separasi; ImageFileCleanup preserve 3 endpoint + tab-2; cert scoped deletedSet (partial-safe) | closed |
| T-367-20 | Spoofing/CSRF | POST tanpa token | mitigate | `[ValidateAntiForgeryToken]`+`[Authorize(Admin,HC)]` preserved | closed |
| T-367-21 | Spoofing/CSRF | POST hapus generik (L-07) tanpa token | mitigate | Attrs preserved saat gate IsManualEntry dihapus | closed |
| T-367-22 | Tampering | type param arbitrer DeletePreview | mitigate | Whitelist type ∈ {training,session} → BadRequest (V5) | closed |
| T-367-23 | Elevation (IDOR) | mirrorTrainingIds ter-inject POST | mitigate | Engine validasi mirror-ID milik-user; controller teruskan saja | closed |
| T-367-24 | Info disclosure | ex.Message bocor flash gagal | mitigate | DeleteTabFailure pesan generik (V7) | closed |
| T-367-25 | Tampering (false-success) | Gagal tampil sukses (#1) | mitigate | Honest split DeleteTabFailure → event recordDeleteFailed + flash MERAH (08: via event DOM, respons 200) | closed |
| T-367-26 | Tampering (data integrity) | Insert duplikat data kotor | mitigate | ManualDuplicatePredicate EXACT 3-pintu (reject/skip); 9 [Fact] | closed |
| T-367-27 | DoS (false-positive) | Guard terlalu longgar blok re-entry | mitigate | EXACT (CompletedAt==), bukan ±1 hari; re-entry tanggal beda lolos | closed |
| T-367-28 | Spoofing/CSRF | POST tanpa token (guard plan) | accept | Endpoint existing ber-antiforgery/authz, tidak dilonggarkan | closed |
| T-367-29 | Spoofing/CSRF | Tombol hapus online POST tanpa token | mitigate | Modal form @Html.AntiForgeryToken + endpoint `[ValidateAntiForgeryToken]` | closed |
| T-367-30 | Tampering (false-success) | Flash hijau saat gagal (#1) | mitigate | Listener event DOM recordDeleteFailed → alert-danger; UAT SC1 + honest split | closed |
| T-367-31 | DoS/UX | RuntimeBinderException shape mismatch (Pitfall 6) | mitigate | Anon 10-prop dijaga; Playwright UAT runtime render clean (0 console err) | closed |
| T-367-32 | Repudiation | Hapus dari UI tanpa konfirmasi sadar | accept | Preview eksplisit (Hapus Semua D-03) + AuditLog + snapshot UAT | closed |
| T-367-D1 | Elevation (privilege) | **DISCOVERED** (adversarial 05): engine tanpa role-guard → HC hapus turunan Completed/ber-jawaban via ancestor | mitigate | EnsureCanDeleteAsync atas FULL cascade set (tab-1) + CascadeHasCompletedOrAnsweredAsync gate (tab-2) sebelum engine; Admin override. Verified | closed |
| T-367-D2 | Tampering (over-deletion) | **DISCOVERED** (adversarial 06): generic DeleteManualAssessment hapus sesi Pre/Post satuan → orphan pasangan | mitigate | Shared IsPrePostSession blok satuan di tab-1 (D-19) + tab-2 generik. Verified | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-367-01 | T-367-03 | Preview cascade hanya untuk Admin/HC (`[Authorize]`); admin sah lihat semua record; mirror-ID tetap divalidasi milik-user saat EXECUTE | User (spec L-01) | 2026-06-13 |
| AR-367-02 | T-367-10 | Badge per-worker di tab admin/HC-only; scope filter admin yang sah | User (spec) | 2026-06-13 |
| AR-367-03 | T-367-28 | Endpoint AddManual/Import/BulkBackfill existing sudah ber-antiforgery+authz; guard duplikat tidak melonggarkan | User (spec) | 2026-06-13 |
| AR-367-04 | T-367-32 | Hapus high-impact via UI dimitigasi preview eksplisit (Hapus Semua) + AuditLog + (UAT) snapshot DB; no type-to-confirm per keputusan user D-03 | User (D-03) | 2026-06-13 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-13 | 32 | 32 | 0 | Adversarial verify workflow wf_e460c1e0-6c3 (6 auditor + re-verify per non-CLOSED) |

Catatan: 30 ancaman ter-register di PLAN (T-367-01..32) + 2 ancaman DISCOVERED saat eksekusi (adversarial verify 05/06) yang LANGSUNG difix (D1 HC-bypass, D2 Pre/Post half-orphan). Semua mitigasi diverifikasi ADA di shipped code (bukan klaim plan) + adversarial re-check. Refinement: T-367-25/30 honest-fail kini via event DOM HTMX + respons 200 (bukan 400) — UAT membuktikan flash merah saat gagal.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-13
