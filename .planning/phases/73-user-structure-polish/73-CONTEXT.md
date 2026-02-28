# Phase 73: User Structure Polish - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Finalize user structure: standardize SelectedView mapping via GetDefaultView(), clean up SeedData, and document dual auth architecture in ARCHITECTURE.md. This is a cleanup/finalization phase — no new capabilities.

</domain>

<decisions>
## Implementation Decisions

### Documentation Content (ARCHITECTURE.md)
- Audience: **both developers and ops/deployment team** — separate sections for each
- Detail level: **menengah** — section terpisah untuk dev (code flow, interfaces) dan ops (config, setup)
- Include **architectural decisions** from Phase 71-72 (why AuthSource was removed, why config toggle over per-user flag, etc.)
- Language: **English** — consistent with existing codebase and ARCHITECTURE.md
- Diagrams: **text-based** (ASCII/Mermaid in Markdown) — 2 diagrams:
  1. Login flow diagram (user login → config check → Local/AD → result)
  2. Service architecture diagram (IAuthService → LocalAuthService / LdapAuthService)
- Location: update existing `.planning/codebase/ARCHITECTURE.md`

### SeedData Modernization
- **Replace all hardcoded SelectedView** with `GetDefaultView(role)` — computed from role after user creation, not set manually in constructor
- User changes:
  - **Rino (Admin)**: Position "System Administrator" → "Operator", add second role Coachee (dual role: Admin + Coachee via AddToRoleAsync). Section/Unit remain null (Admin has full access).
  - **Rustam (Coach)**: Position "Coach" → "Shift Supervisor" (Coach is a role, not a position)
- All other seed users remain unchanged (9 total users, all roles covered)
- Password "123456" stays for development
- For dual-role users (Rino), SelectedView uses **role tertinggi** (Admin, level 1) via GetDefaultView — actual "choose view at login" feature deferred to Phase 74

### GetDefaultView() Sweep Scope
- **Full codebase sweep** — find ALL places that hardcode SelectedView strings
- Replace all hardcoded instances with GetDefaultView() calls
- Controllers already use GetDefaultView() (3 call sites from Phase 69) — expect SeedData to be the main remaining location
- Login flow: **do NOT reset SelectedView** — login uses existing DB value, GetDefaultView() only for create/edit user
- Edge cases: unknown role → default "Coachee" (least privilege, already implemented)
- User without role → treated as Coachee (consistent with default)

### Claude's Discretion
- Exact placement of dual auth section in ARCHITECTURE.md
- Mermaid vs ASCII format for diagrams
- How SeedData computes SelectedView post-creation (could be in the loop after AddToRoleAsync, or as a separate step)

</decisions>

<specifics>
## Specific Ideas

- View constants (UserRoles.Views.Admin, etc.) were discussed and explicitly **declined** — string literals stay for now
- Coach is a **role, not a position** — SeedData should reflect actual job positions (Shift Supervisor, Operator, etc.)
- Admin is an **additional role** — in real usage, admin users have real positions (Operator, etc.) and get Admin as extra access. SeedData should reflect this.

</specifics>

<deferred>
## Deferred Ideas

### Phase 74: Role & Access Restructuring (new phase)
1. **Mandatory dual role for Admin** — Admin role always requires a second "real" role. UI changes in CreateWorker/EditWorker needed.
2. **RoleLevel restructuring** — SectionHead should be level 3 (full access, can see all sections) instead of level 4. Currently SectionHead and SrSupervisor share level 4, but SectionHead should have broader access.
3. **Fix upload evidence access** — Currently RoleLevel <= 5 check allows SrSupervisor to upload Proton evidence. Only Coach should be able to upload evidence.
4. **Choose view at login** — Dual-role users should be able to select which view they want at login (Admin view vs Coachee view, etc.). Requires login UI changes.

</deferred>

---

*Phase: 73-user-structure-polish*
*Context gathered: 2026-02-28*
