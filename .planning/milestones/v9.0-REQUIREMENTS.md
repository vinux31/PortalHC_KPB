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
- [ ] **CONF-02**: Connection string production menggunakan placeholder yang jelas (bukan hardcode credential)
- [ ] **CONF-04**: Debug/development middleware di-guard dengan IsDevelopment() check

### Security Hardening

- [ ] **SEC-01**: Custom error pages untuk 404/403/500 (tidak expose stack trace)
- [ ] **SEC-02**: Cookie security: Secure=Always, HttpOnly=true, SameSite configured
- [ ] **SEC-03**: Anti-forgery token lengkap di semua POST actions
- [ ] **SEC-04**: Authorization completeness audit — semua controller/action punya atribut yang benar
- [ ] **SEC-05**: File upload validation lengkap (whitelist extension, size limit, content-type)

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
| HTTPS enforcement (CONF-03) | Konfigurasi server — tanggung jawab tim IT |
| AllowedHosts (CONF-05) | Konfigurasi server — tanggung jawab tim IT |
| Deployment runbook (DEPL-01~05) | Sudah dibuat sebagai HTML serah terima (docs/deployment-planning.html), setup server urusan tim IT |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEED-01 | Phase 254 | Pending |
| SEED-02 | Phase 254 | Pending |
| SEED-03 | Phase 254 | Pending |
| CONF-01 | Phase 255 | Pending |
| CONF-02 | Phase 255 | Pending |
| CONF-04 | Phase 255 | Pending |
| SEC-01 | Phase 256 | Pending |
| SEC-02 | Phase 256 | Pending |
| SEC-03 | Phase 256 | Pending |
| SEC-04 | Phase 256 | Pending |
| SEC-05 | Phase 256 | Pending |
| DEBT-01 | Phase 254 | Pending |
| DEBT-02 | Phase 254 | Pending |
| DEBT-03 | Phase 254 | Pending |
| DEBT-04 | Phase 254 | Pending |
| DEBT-05 | Phase 254 | Pending |

**Coverage:**
- v9.0 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0

---
*Requirements defined: 2026-03-25*
*Last updated: 2026-03-25 after roadmap revision (removed Phase 257, trimmed Phase 255)*
