# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- 🚧 **v6.0 Deployment Preparation** - Phases 173–174 (in progress)

## Phases

<details>
<summary>✅ v1.0 – v5.0 (Phases 1–172) - SHIPPED 2026-03-16</summary>

43 milestones shipped. Full history in .planning/MILESTONES.md.

</details>

### 🚧 v6.0 Deployment Preparation (In Progress)

**Milestone Goal:** Produce a production-ready release package and documentation so the IT team can deploy Portal HC KPB to IIS with Active Directory integration.

- [ ] **Phase 173: Release Package** - Create the release folder with only necessary files and production config
- [ ] **Phase 174: Deployment Documentation** - Write SSMS database export guide and full IT deployment guide

## Phase Details

### Phase 173: Release Package
**Goal**: A clean, deployable release folder exists at the target path with production configuration enabled
**Depends on**: Nothing (first phase of v6.0)
**Requirements**: REL-01, REL-02
**Success Criteria** (what must be TRUE):
  1. Folder `C:\Users\Administrator\Desktop\PortalHC_KPB_Release` exists and contains all project source files needed to build and run the application
  2. Dev artifacts (`.planning/`, `.playwright-mcp/`, `docs/`, `bin/`, `obj/`, `HcPortal.db`, root `.md/.ps1/.bat` files) are absent from the release folder
  3. `appsettings.json` inside the release folder has `UseActiveDirectory` set to `true`
  4. Original source project folder (`PortalHC_KPB`) is unchanged
**Plans**: TBD

### Phase 174: Deployment Documentation
**Goal**: IT team has step-by-step written guides to export the database from SSMS and deploy the application to IIS with AD integration
**Depends on**: Phase 173
**Requirements**: DOC-01, DOC-02
**Success Criteria** (what must be TRUE):
  1. SSMS database export guide exists with numbered steps covering: connecting to the local database, exporting a `.bacpac` or backup file, and transferring it to the production server
  2. Deployment guide covers IIS site setup (application pool, .NET version, physical path) step by step
  3. Deployment guide covers production `appsettings.json` configuration (connection string, AD settings)
  4. Deployment guide covers database restore on the production SQL Server
  5. IT team can follow both guides without needing to ask the developer for clarification
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 173. Release Package | v6.0 | 0/? | Not started | - |
| 174. Deployment Documentation | v6.0 | 0/? | Not started | - |
