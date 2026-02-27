# Phase 67: Dynamic Profile Page - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Profile page (Views/Account/Profile.cshtml) menampilkan data real user login dari database — no more hardcoded placeholders. Controller already passes ApplicationUser to view; this phase rewrites the view to use @Model. Avatar initials already dynamic in _Layout.cshtml — profile page avatar must match.

</domain>

<decisions>
## Implementation Decisions

### Field organization
- 2 sections with dividers: **Identitas** + **Organisasi**
- Identitas: Nama, NIP, Email, Phone (PhoneNumber from IdentityUser)
- Organisasi: Directorate, Section, Unit, Position, Role
- Layout: label-value rows with section heading (bold) + thin divider between sections — professional corporate style, no cards
- JoinDate skipped — not displayed on profile page

### Profile header
- Header area at top: large avatar initials circle + Nama Lengkap + Position subtitle
- Single fixed color for avatar background (primary/blue) — same for all users
- No badge — "Active Employee" badge removed; Role is in the fields below

### Empty field handling
- Null/empty fields display "—" (em dash) — not "Belum diisi", not blank
- Sections always shown even if all fields in section are null
- Em dash styled in muted color to differentiate from real data

### Field additions and removals
- Phone (IdentityUser.PhoneNumber) added to Identitas section
- Location removed — no field in ApplicationUser
- "Active Employee" status badge removed — hardcoded, no backing data
- Role value comes from UserManager.GetRolesAsync() — first role displayed

### Claude's Discretion
- Exact typography (font sizes, weights, spacing between rows)
- Section divider styling (border-bottom, hr, or spacing)
- Avatar circle size on profile page
- Responsive behavior on mobile

</decisions>

<specifics>
## Specific Ideas

- Style should feel like a professional corporate/enterprise HRIS page — clean, not flashy
- No cards — use flat label-value rows with section dividers
- AccountController.Profile already fetches user + roles from DB — view just needs to use @Model instead of hardcoded strings

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 67-dynamic-profile-page*
*Context gathered: 2026-02-27*
