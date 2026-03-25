# Requirements: Portal HC KPB v9.0

**Defined:** 2026-03-25
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v9.0 Requirements

Requirements for pre-deployment audit & finalization. Each maps to roadmap phases.

### Seed & Data Cleanup

- [ ] **SEED-01**: Seed data UAT (Phase 241) hanya berjalan di environment Development, tidak di Production
- [ ] **SEED-02**: SeedProtonData dipanggil dengan environment guard (IsDevelopment)
- [ ] **SEED-03**: Review semua method Seed* dan pastikan idempotent + production-safe

### Production Configuration

- [ ] **CONF-01**: appsettings.Production.json lengkap dengan logging level Warning untuk Microsoft.*, Information untuk app
- [ ] **CONF-02**: Connection string production menggunakan environment variable (bukan hardcode placeholder)
- [ ] **CONF-03**: HTTPS enforcement aktif (UseHttpsRedirection + HSTS header)
- [ ] **CONF-04**: Debug/development middleware di-guard dengan IsDevelopment() check
- [ ] **CONF-05**: AllowedHosts dikonfigurasi (bukan wildcard "*")

### Security Hardening

- [ ] **SEC-01**: Custom error pages untuk 404/403/500 (tidak expose stack trace)
- [ ] **SEC-02**: Cookie security: Secure=Always, HttpOnly=true, SameSite configured
- [ ] **SEC-03**: Anti-forgery token lengkap di semua POST actions
- [ ] **SEC-04**: Authorization completeness audit — semua controller/action punya atribut yang benar
- [ ] **SEC-05**: File upload validation lengkap (whitelist extension, size limit, content-type)

### Deployment Preparation

- [ ] **DEPL-01**: web.config untuk IIS (AspNetCoreModuleV2, WebSocket enable untuk SignalR)
- [ ] **DEPL-02**: Database migration script (SQL script dari dev schema, tested)
- [ ] **DEPL-03**: Pre-deploy backup strategy documented
- [ ] **DEPL-04**: Deployment runbook step-by-step (IIS setup, DB, config, verify)
- [ ] **DEPL-05**: Publish profile untuk IIS deployment

### Tech Debt Closure

- [ ] **DEBT-01**: Fix bare catch at AdminController:1072
- [ ] **DEBT-02**: Fix null-forgiving operator (Authorize guarantee)
- [ ] **DEBT-03**: Clean up 3 orphaned KkjMatrixItemId columns
- [ ] **DEBT-04**: Address 5 near-duplicate code pairs
- [ ] **DEBT-05**: Fix SUMMARY counting error (27 vs 35 DbSets)

## Future Requirements

### Post-deployment Optimization

- **OPT-01**: Health check endpoint (/health) untuk monitoring
- **OPT-02**: Rate limiting pada login endpoint
- **OPT-03**: Database index review berdasarkan real usage pattern
- **OPT-04**: Response caching headers untuk static assets
- **OPT-05**: Graceful startup validation (DB unreachable handling)

## Out of Scope

| Feature | Reason |
|---------|--------|
| CI/CD pipeline | Scope creep — manual deployment cukup untuk v1 |
| Automated test suite | User sudah UAT manual 252+ phases |
| Docker/containerization | Target = IIS on-premise |
| APM integration (App Insights) | File logging cukup untuk awal |
| Active Directory activation | User akan aktifkan manual — bukan bagian milestone ini |
| SSL certificate provisioning | Tanggung jawab infra/network team |
| Load balancer setup | Single server untuk awal |
| Penetration test | Butuh external security team |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEED-01 | — | Pending |
| SEED-02 | — | Pending |
| SEED-03 | — | Pending |
| CONF-01 | — | Pending |
| CONF-02 | — | Pending |
| CONF-03 | — | Pending |
| CONF-04 | — | Pending |
| CONF-05 | — | Pending |
| SEC-01 | — | Pending |
| SEC-02 | — | Pending |
| SEC-03 | — | Pending |
| SEC-04 | — | Pending |
| SEC-05 | — | Pending |
| DEPL-01 | — | Pending |
| DEPL-02 | — | Pending |
| DEPL-03 | — | Pending |
| DEPL-04 | — | Pending |
| DEPL-05 | — | Pending |
| DEBT-01 | — | Pending |
| DEBT-02 | — | Pending |
| DEBT-03 | — | Pending |
| DEBT-04 | — | Pending |
| DEBT-05 | — | Pending |

**Coverage:**
- v9.0 requirements: 23 total
- Mapped to phases: 0
- Unmapped: 23 ⚠️

---
*Requirements defined: 2026-03-25*
*Last updated: 2026-03-25 after initial definition*
