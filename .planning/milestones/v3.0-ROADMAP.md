# Roadmap: Portal HC KPB

## Current Milestone: v3.6 Histori Proton

### Phase 107: Backend & Worker List Page

**Goal:** Build CDPController actions, role-scoped access, and worker list page with search/filter.

**Requirements:** HIST-01 through HIST-08

**Success Criteria:**
1. CDPController has HistoriProton (list) and HistoriProtonDetail (timeline) actions
2. CDP navbar shows "Histori Proton" menu item
3. Role-scoped access: Coachee redirects to own detail, Coach/SrSpv/SH sees section, HC/Admin sees all
4. Worker list page displays workers with Proton history (from ProtonTrackAssignment)
5. Search by nama/NIP and filter by unit/section work correctly
6. Each row shows summary: nama, NIP, tahun Proton terakhir, status terakhir

### Phase 108: Timeline Detail Page & Styling

**Goal:** Build vertical timeline detail page with Proton year nodes and responsive styling.

**Requirements:** HIST-09 through HIST-17

**Success Criteria:**
1. Vertical timeline with distinct node per Proton year (filled/empty based on status)
2. Each node displays: Tahun (1/2/3), Unit, Coach name, Status badge, Competency Level, Dates
3. Timeline ordered chronologically (Tahun 1 -> 2 -> 3)
4. Status badges: Lulus (green), Dalam Proses (yellow), Belum Mulai (gray)
5. Design consistent with portal design system (Bootstrap 5, CSS variables)
6. Responsive mobile layout

---
*Roadmap created: 2026-03-06*
*Last updated: 2026-03-06 after v3.6 milestone definition*
