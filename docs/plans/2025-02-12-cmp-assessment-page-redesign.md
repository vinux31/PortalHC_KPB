# CMP Assessment Page Redesign Design

**Date:** 2025-02-12
**Status:** Approved
**Author:** Claude Sonnet (with user collaboration)

---

## Overview

Redesign the CMP Assessment page (`/CMP/Assessment`) to simplify the user interface and focus on enabling users to quickly access and start their assessments. The redesign removes complexity (statistics, filters, table) and replaces them with a clean card-based grid layout with simple tab filtering.

---

## Current State

**Existing Features to Remove:**
- Summary Statistics Cards (Total, Open, Completed, Upcoming)
- Search & Filter Bar (search input, category dropdown, status dropdown)
- Main Assessment Table

**Existing Features to Keep:**
- Assessment data model (`AssessmentSession`)
- Controller logic (`CMPController.Assessment()`)
- Token verification modal
- Navigation and authentication

---

## Design Goals

1. **Simplicity**: Minimal UI with focus on essential information
2. **Action-Oriented**: Enable users to quickly start open assessments
3. **Visual Clarity**: Use cards with clear status indicators and category badges
4. **Mobile-First**: Responsive design that works on all devices
5. **Maintained Functionality**: Keep token verification and certificate viewing

---

## New Design Specification

### 1. Page Layout

```
┌─────────────────────────────────────────────┐
│  Header: "My Assessments"                    │
├─────────────────────────────────────────────┤
│                                             │
│   [All] [Open] [Completed]  ← Tabs         │
│   ═════════                                  │
│                                             │
│  ┌─────┐ ┌─────┐ ┌─────┐   (Grid 3 col)   │
│  │Card1│ │Card2│ │Card3│                   │
│  └─────┘ └─────┘ └─────┘                   │
│                                             │
│  ┌─────┐ ┌─────┐ ┌─────┐                   │
│  │Card4│ │Card5│ │Card6│                   │
│  └─────┘ └─────┘ └─────┘                   │
└─────────────────────────────────────────────┘
```

### 2. Card Design

Each assessment card displays:

**Header Row:**
- **Left**: Category badge (colored by type)
- **Right**: Status badge

**Content:**
- **Title**: Assessment name (bold, max 2-3 lines with ellipsis)

**Footer:**
- **Action Button**: Based on status (see Button Matrix below)

**Card Styling:**
- White background, light gray border
- Box shadow with hover elevation
- Border radius: 8px
- Min-height: 180px
- Hover: Scale 1.02 + increased shadow

**Responsive Grid:**
- Desktop (>992px): 3 columns
- Tablet (768-992px): 2 columns
- Mobile (<768px): 1 column

### 3. Card Information Display

**Displayed Information:**
- ✅ Title (Assessment name)
- ✅ Category (with color-coded badge)
- ✅ Status (Open/Upcoming/Completed)

**Hidden Information (not in card):**
- Schedule date
- Duration minutes
- Progress percentage
- Banner color

**Rationale:** Minimalist design focusing on what users need to quickly identify and start assessments.

### 4. Category Badge Colors

| Category | Bootstrap Class | Color |
|----------|---------------|-------|
| OJT | `badge-primary` | Blue 🔵 |
| IHT | `badge-success` | Green 🟢 |
| OTS | `badge-warning` | Yellow 🟡 |
| Training Licencor | `badge-danger` | Red 🔴 |
| Mandatory HSSE Training | `badge-info` | Cyan 🔷 |
| Proton | Custom gradient | Purple 🟣 |

### 5. Status Badges

| Status | Badge Style | Label |
|--------|-------------|-------|
| Open | Green, solid | "OPEN" |
| Upcoming | Orange, solid | "UPCOMING" |
| Completed | Blue, solid | "COMPLETED" |

### 6. Button Matrix

| Status | Button Style | Label | Behavior |
|--------|-------------|-------|----------|
| Open | Primary, prominent | "Start Assessment →" | Redirect to assessment OR open token modal |
| Open (Token Required) | Primary with shield icon 🛡️ | "Start Assessment →" | Open token modal, then redirect |
| Upcoming | Disabled, gray | "🔒 Available on [date]" | No action, locked |
| Completed | Outline, secondary | "View Certificate (Score: 85)" | Redirect to certificate page |

### 7. Tab Filtering

**Tabs:**
- **All**: Show all assessments (Open + Upcoming + Completed)
- **Open**: Show only Open status assessments
- **Completed**: Show only Completed status assessments

**Tab Styling:**
- Bootstrap tabs or custom implementation
- Active: Primary background, white text
- Inactive: Light background, muted text
- Underline indicator for active tab

**Behavior:**
- Client-side JavaScript filtering
- All cards rendered, hidden/shown via CSS
- Smooth transitions between tabs
- Default: "All" tab active

### 8. Token Verification Flow

For `IsTokenRequired = true` assessments:

```
User clicks "Start Assessment"
    ↓
Check: IsTokenRequired?
    ↓
YES → Open Token Modal
    ↓
User enters 6-digit token
    ↓
Validate token
    ↓
Correct → Redirect to assessment page
Incorrect → Show error, keep modal open
```

**Modal Features (existing, maintain):**
- Title: "Enter Access Token"
- Input: 6-digit token field
- Buttons: "Verify" + "Cancel"
- Error message display
- Proton warning: "NSO Room only" (if applicable)

### 9. Empty States

| Scenario | Display |
|----------|---------|
| No assessments at all | Icon + "No assessments available" |
| Tab filter has no results | Icon + "No assessments in this category" |

### 10. Data Flow

```
User navigates to /CMP/Assessment
    ↓
CMPController.Assessment() executes
    ↓
Query: SELECT * FROM AssessmentSessions WHERE UserId = @UserId ORDER BY Schedule
    ↓
Return List<AssessmentSession> to View
    ↓
View renders:
  - Header
  - Tab navigation
  - All assessment cards (hidden/shown via tabs)
    ↓
JavaScript handles:
  - Tab switching
  - Token modal
  - Button actions
```

### 11. Accessibility

- Keyboard navigation for tabs
- Focus indicators on buttons
- ARIA labels for screen readers
- Color contrast compliance (WCAG AA)
- Semantic HTML structure

---

## Files to Modify

| File | Changes |
|------|---------|
| `Views/CMP/Assessment.cshtml` | Complete redesign: remove stats/filters/table, add tabs and card grid |
| `wwwroot/css/cmp.css` (or inline) | Add card styling, tab styling, animations |
| `wwwroot/js/cmp-assessment.js` (or inline) | Tab filtering logic, maintain token modal logic |

**Files NOT Modified:**
- `Controllers/CMPController.cs` - Keep existing logic
- `Models/AssessmentSession.cs` - No model changes needed
- `Data/ApplicationDbContext.cs` - No DB changes needed

---

## Implementation Checklist

### Phase 1: Structure
- [ ] Remove summary statistics cards section
- [ ] Remove search & filter bar section
- [ ] Remove main assessment table section
- [ ] Add page header: "My Assessments"
- [ ] Add tab navigation (All, Open, Completed)

### Phase 2: Card Component
- [ ] Create card HTML template
- [ ] Display category badge (left of header)
- [ ] Display status badge (right of header)
- [ ] Display title in body
- [ ] Add action button (based on status)
- [ ] Apply card styling (shadow, border, hover)

### Phase 3: Grid Layout
- [ ] Implement responsive grid (3/2/1 columns)
- [ ] Add card spacing (gap/padding)
- [ ] Test on desktop, tablet, mobile

### Phase 4: Tab Filtering
- [ ] Add tab click event handlers
- [ ] Implement show/hide logic by status
- [ ] Add active state styling
- [ ] Set default tab (All)
- [ ] Handle edge cases (empty results)

### Phase 5: Token Modal
- [ ] Verify existing modal works
- [ ] Ensure token modal opens for `IsTokenRequired = true`
- [ ] Test token validation flow
- [ ] Test Proton-specific warning

### Phase 6: Polish
- [ ] Add loading state
- [ ] Add empty states
- [ ] Test all button actions
- [ ] Accessibility audit
- [ ] Cross-browser testing

---

## Success Criteria

1. ✅ Page loads with all user assessments visible
2. ✅ Cards display Title, Category, and Status correctly
3. ✅ Tab filtering works smoothly (All, Open, Completed)
4. ✅ "Start Assessment" button works for Open assessments
5. ✅ Token modal opens for token-required assessments
6. ✅ "View Certificate" button works for Completed assessments
7. ✅ Design is responsive (desktop, tablet, mobile)
8. ✅ Empty states display correctly
9. ✅ No JavaScript errors in console
10. ✅ Existing functionality (records, certificates, etc.) unaffected

---

## Future Enhancements (Out of Scope)

- Add sorting options (by date, category, etc.)
- Add pagination for large assessment lists
- Add assessment search functionality
- Add progress indicators for in-progress assessments
- Add assessment preview/description modal
- Add assessment reminder notifications

---

## References

- Original Implementation: `Views/CMP/Assessment.cshtml`
- Controller: `Controllers/CMPController.cs` (lines 70-83)
- Model: `Models/AssessmentSession.cs`
- Database Context: `Data/ApplicationDbContext.cs`
- Existing Token Modal: In current Assessment.cshtml

---

*End of Design Document*
