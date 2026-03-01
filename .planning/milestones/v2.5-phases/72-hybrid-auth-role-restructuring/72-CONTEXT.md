# Phase 74: Hybrid Auth & Role Restructuring - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Enable hybrid authentication (AD fallback to local) for the dedicated Admin KPB account, restructure role hierarchy (add Supervisor role, move SectionHead to level 3), fix role display code, restrict upload evidence to Coach only, and enforce strict one-role-per-user policy. This phase does NOT add new capabilities — it fixes and restructures existing auth and role infrastructure.

</domain>

<decisions>
## Implementation Decisions

### Hybrid Auth Fallback
- **Fallback scope:** Admin KPB only (admin@pertamina.com). All other users must authenticate via AD — if AD is down, they cannot login.
- **Identification:** By email — hardcode admin@pertamina.com as the local fallback account.
- **Login UX:** Same login form for everyone. System detects admin@pertamina.com and tries local auth after AD fails. Regular users only go through AD.
- **AD priority:** Always try AD first for admin@pertamina.com, then fall back to local if AD fails.
- **Fallback UX:** Completely silent — no indication of which auth path was used. Just works.
- **Error handling (AD down, regular user):** Generic error "Login gagal. Silakan coba lagi nanti." — do not reveal AD is the issue.
- **Error handling (admin wrong password):** Same generic error as regular users — no distinction.
- **Password storage:** ASP.NET Identity PasswordHash field in database. Standard UserManager handles hashing and verification.

### Strict One-Role-Per-User Policy
- **Rule:** Every user has exactly ONE role. Roles are mutually exclusive — Coach cannot also be Coachee.
- **Form validation:** Edit profile/worker forms use single-select dropdown for role (not checkbox). Submit auto-replaces old role with new.
- **Import validation:** When importing from Excel, if user already exists → skip that user. Show report at end: "X user sudah ada, di-skip" with list of skipped users below.
- **Current data:** Already clean — all 10 users have single role (verified via database query).

### Role Display Fix
- **Problem:** `_Layout.cshtml` line 7 uses `GetRolesAsync().FirstOrDefault()` which is unpredictable for multi-role users.
- **Fix:** Replace `FirstOrDefault()` with more explicit approach. Since strict one-role policy is enforced, this becomes safe, but code should be explicit.
- **Source for role display:** Claude's discretion — pick the approach that fits the current architecture best (claim-based, dedicated field, or explicit single-role query).

### Role Hierarchy Restructuring
New hierarchy (changes marked with **bold**):

| Level | Role | SelectedView | Data Scope |
|-------|------|-------------|------------|
| 1 | Admin | "Admin" | Full (all sections) |
| 2 | HC | "HC" | Full (all sections) |
| 3 | Direktur, VP, Manager, **Section Head** | "Atasan" | Full (all sections) |
| 4 | Sr Supervisor | "Atasan" | Section only |
| 5 | **Supervisor** (NEW), Coach | "Coach" | Unit only |
| 6 | Coachee | "Coachee" | Self only |

Changes from current:
- **Section Head:** level 4 → level 3. Now has full access (all sections), same as management.
- **Supervisor:** NEW role at level 5. Same access/menu as Coach but never assigned as coach (no coachee mapping). SelectedView = "Coach".
- **Sr Supervisor:** Stays level 4. Role = approve/reject. No coachee, no evidence upload.

### Role Registration (Supervisor)
- Add "Supervisor" to `UserRoles.AllRoles`, `GetRoleLevel()` (return 5), and `GetDefaultView()` (return "Coach")
- Create role in database (SeedData/CreateRolesAsync)
- No seed user needed — real users assigned via admin

### Database Migration
- Update existing SectionHead users: RoleLevel 4 → 3 (migration script)
- Update seed data to reflect new hierarchy

### Evidence Upload Access
- **Restricted to Coach role only** — not by RoleLevel but by role name check
- Current bug: `user.RoleLevel <= 5` allows SrSupervisor, SectionHead, Management, HC, Admin to upload
- Fix: Change check to role == Coach specifically
- Non-Coach users see same view as SectionHead/HC now — form upload simply doesn't appear

### EligibleCoaches Logic
- **Filter by role name "Coach" only** — not by RoleLevel
- Current bug: `u.RoleLevel <= 5` includes everyone from Admin to Coach
- Fix: Only users with role "Coach" appear in eligible coaches dropdown

### Proton/CDP Data Scope (applies to all scope-filtered features)
- Level 1-3 (Admin, HC, Direktur, VP, Manager, SectionHead): **Full access** — all coachees, all sections
- Level 4 (Sr Supervisor): **Section scope** — coachees in their section
- Level 5 (Supervisor, Coach): **Unit scope** — coachees in their unit
- Level 6 (Coachee): **Self only** — own data via separate code path
- CanAccessProton logic: Remains `RoleLevel <= 5` (all except Coachee)

### Coaching Flow
- Coach: inputs coaching data, uploads evidence, has coachee mapping
- Supervisor: same view/menu as Coach, but no coachee mapping (pages show empty)
- Sr Supervisor: approves/rejects, no coachee, no evidence upload

### Claude's Discretion
- Exact approach for role display fix (claim vs field vs query)
- Migration strategy for SectionHead RoleLevel update
- How to add Supervisor role to existing code paths that use role name checks
- Exact scope filtering implementation in CDPController

</decisions>

<specifics>
## Specific Ideas

- "Samakan Supervisor full dengan Coach, cuman dia gak punya coachee" — Supervisor is functionally a Coach without coaching assignments
- "Di form edit profile, cegah agar cuma bisa isi 1 role" — use single-select dropdown
- "Import Excel: skip user existing, laporkan setelahnya dengan list" — skip + detailed report
- "Semua role bisa akses Proton, perbedaannya di filter scope data" — confirmed for all roles

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 74-hybrid-auth-role-restructuring*
*Context gathered: 2026-02-28*
