# Requirements: Portal HC KPB

**Defined:** 2026-03-16
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v6.0 Requirements

Requirements for deployment preparation. Each maps to roadmap phases.

### Release Package

- [ ] **REL-01**: Release folder created at `C:\Users\Administrator\Desktop\PortalHC_KPB_Release` with only necessary project files (excluding `.planning/`, `.playwright-mcp/`, `docs/`, `bin/`, `obj/`, `HcPortal.db`, root `.md/.ps1/.bat` files)
- [ ] **REL-02**: Copied `appsettings.json` has `UseActiveDirectory` set to `true`

### Documentation

- [ ] **DOC-01**: SSMS database export guide with step-by-step instructions for IT team
- [ ] **DOC-02**: Deployment guide covering IIS setup, configuration, database restore, and AD integration

## Future Requirements

None — this is a terminal deployment milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| CI/CD pipeline | Manual deployment for initial release |
| Docker containerization | IIS deployment target per IT infrastructure |
| Automated database migration | Manual SSMS export/import for IT control |
| Code changes to source project | Original project folder must remain unchanged |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| REL-01 | TBD | Pending |
| REL-02 | TBD | Pending |
| DOC-01 | TBD | Pending |
| DOC-02 | TBD | Pending |

**Coverage:**
- v6.0 requirements: 4 total
- Mapped to phases: 0
- Unmapped: 4

---
*Requirements defined: 2026-03-16*
*Last updated: 2026-03-16 after initial definition*
