---
phase: 377
slug: impersonation-identity-across-surfaces
status: verified
threats_open: 0
asvs_level: 2
created: 2026-06-14
---

# Phase 377 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Root cause: bug 999.6 — admin impersonates worker X but read surfaces leak admin's OWN data instead of X's (IDOR / privilege confusion). Fix = single-source effective-user resolver (fail-closed) konsumsi lintas CMP/CDP/Home + middleware D-04.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| admin principal → impersonated identity (session state) | Principal TIDAK di-swap; impersonasi = state session. Audit menentukan surface mana yang HARUS resolve X. | identitas efektif (PII worker X) |
| session impersonation state → effective identity (resolver) | `ResolveEffectiveUserDecision` menerjemahkan state jadi identitas query. Salah resolve = data-leak. | userId/roleLevel/section efektif |
| middleware → controller | Gate fail-closed satu-satunya sebelum controller (expired / target-deleted). | request authorization |
| GET request (impersonate) → CMP/CDP/Home read surfaces | Surface baca worker-data; harus effective X, bukan admin (IDOR bila salah). | records/cert/progress PII |
| GET StartExam → DB write (auto-transition) | Write tersembunyi di GET; saat read-only impersonation = tampering. | AssessmentSession status |
| e2e seed (temporary) → DB lokal | Seed harus restored (CLAUDE.md SEED_WORKFLOW). | data uji lokal |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-377-01 | Information Disclosure | Audit tak lengkap → surface self-data ter-skip | mitigate | `377-AUDIT.md` parity: 11 IN-SCOPE + 5 BORDERLINE + OUT-per-pola; grep CMP(25)/CDP(33)/Home(3) exhaustive | closed |
| T-377-02 | Information Disclosure | Borderline (DokumenKkj/CertificationManagement) di-drop diam-diam | mitigate | `377-AUDIT.md` Tabel 2: DokumenKkj A3-OUT + CertificationManagement CDP A4-OUT, dienumerasi + rationale | closed |
| T-377-03 | Tampering (preview) | Test kontrak resolver lemah → fail-open lolos | mitigate | `ImpersonationIdentityTests.cs:21` `(true,false,"user",null)→RoleModeEmpty`; 7/7 matrix GREEN | closed |
| T-377-04 | Information Disclosure | Resolver fallback admin saat mode-role/target-null | mitigate | `ImpersonationService.cs:150-155` fail-closed RoleModeEmpty, no admin fallback | closed |
| T-377-05 | Elevation of Privilege / V3 | Sesi impersonasi expired tapi resolver tetap pakai X | mitigate | `ImpersonationService.cs:149` `!isImpersonating\|\|isExpired→UseRealUser`; `ImpersonationMiddleware.cs:56-66` auto-expire pre-controller | closed |
| T-377-06 | Information Disclosure / DoS-soft | Target user terhapus → request lanjut identitas admin | mitigate (block: high) | `ImpersonationMiddleware.cs:137-148` targetUser==null → `Stop()`+TempData+redirect `/Admin/Index`+`return false` pre-controller | closed |
| T-377-07 | Regression (SC4) | Resolver rewrite ubah non-impersonate | mitigate | `ImpersonationService.cs:149` branch UseRealUser; xUnit 372/372 GREEN | closed |
| T-377-08 | Information Disclosure / IDOR | Records/Assessment/Results pakai admin.Id (akar 999.6) | mitigate (block: high) | `CMPController.cs:2424-2446` 9 self-read caller pakai effective X.Id via `GetEffectiveUserAsync` | closed |
| T-377-09 | Tampering | StartExam write-on-GET `SaveChangesAsync` menulis DB X | mitigate (block: high) | `CMPController.cs:905` `if(!IsImpersonating())` wrap Status="Open"+Save | closed |
| T-377-10 | Information Disclosure | Mode-role redirect Login / fallback admin di Records | mitigate | `CMPController.cs:484-500` mode-role+user==null → hint+emptyVm(User=null)+View; bukan redirect/admin | closed |
| T-377-11 | Privilege confusion (V4) | Impersonate Coachee X lihat cert worker Y | mitigate | `CMPController.cs:2450-2458` `IsResultsAuthorized` pakai roleLevel+section efektif X; `ResultsAuthorizationTests.cs:27-28` lock owner-X OK / data-Y Forbid | closed |
| T-377-12 | Regression (SC4) | Caller non-impersonate berubah | mitigate | `CMPController.cs:2429-2434` UseRealUser branch unchanged; 372/372 GREEN | closed |
| T-377-13 | Information Disclosure | BuildSertifikatRowsAsync(l5OwnDataOnly) pakai admin.Id | mitigate | `CDPController.cs:3875-3878` null-guard + L5 `scopedUserIds={user.Id}` effective X | closed |
| T-377-14 | Build break (DoS) | CDP resolver `_impersonationService` belum inject | mitigate (block: high) | `CDPController.cs:40-51` field + ctor inject sebelum dipakai | closed |
| T-377-15 | NRE/500 (Availability) | Nullable signature, caller `.User.Section` NRE | mitigate | `CDPController.cs:3756` `cmUser?.Section` null-conditional | closed |
| T-377-16 | Information Disclosure | CertificationManagement `ViewBag.UserBagian` bocor section admin | mitigate | `CDPController.cs:3755-3756` `cmUser?.Section`→null saat mode-role | closed |
| T-377-17 | Regression (SC4) | Resolver non-impersonate berubah (CDP) | mitigate | `CDPController.cs:3703-3708` UseRealUser identik; 372/372 GREEN | closed |
| T-377-18 | Information Disclosure | GetProgress/GetUpcomingEvents pakai admin.Id | mitigate (block: high) | `HomeController.cs:38-52` `GetEffectiveUserAsync`; `userId=user?.Id??""`; progress/events pakai X.Id; e2e SC3 + UAT step 3 | closed |
| T-377-19 | Information Disclosure | Mode-role → fallback admin progress | mitigate | `HomeController.cs:52` `user?.Id??""` → 0 record bukan admin | closed |
| T-377-20 | Scope creep / regression | Menyentuh Guide/GuideDetail (OUT) | mitigate | `HomeController.cs:339-344` Guide/GuideDetail `GetUserAsync(User)` UTUH; AUDIT Tabel 3 OUT rationale | closed |
| T-377-21 | Regression (SC4) | Index non-impersonate berubah | mitigate | `HomeController.cs:41-44` UseRealUser `GetUserAsync(User)`+Challenge() identik | closed |
| T-377-22 | Information Disclosure (verification) | Fix diklaim selesai tapi surface masih bocor (uji lemah) | mitigate (block: high) | `tests/e2e/impersonation.spec.ts:234-247` SC2 (Records data X) + `:250-264` SC3 (Home 'Iwan'); 13/13 GREEN; UAT 7/7 live | closed |
| T-377-23 | Privilege confusion (V4) | Fidelity D-01 salah (X lihat data Y) tak teruji | mitigate | `ResultsAuthorizationTests.cs:27-30` 4 D-01 fidelity InlineData (15/15 GREEN); UAT step 5 | closed |
| T-377-24 | Tampering (StartExam) | Write-on-GET masih menulis saat impersonasi | mitigate | `CMPController.cs:905` guard + UAT step 6 (Status DB tetap Upcoming) | closed |
| T-377-25 | Data hygiene | Seed temporary nempel di DB lokal | accept→mitigate | `docs/SEED_JOURNAL.md:198` Phase 377: temporary+local-only, REUSE no-insert, e2e auto snapshot→restore | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-377-01 | T-377-25 | Seed e2e bersifat temporary+local-only. Risiko diterima lalu di-mitigate via snapshot+restore otomatis + SEED_JOURNAL `cleaned` (CLAUDE.md SEED_WORKFLOW). Tak ada dampak prod. | Rino | 2026-06-14 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-14 | 25 | 25 | 0 | gsd-security-auditor (sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-14
