# Phase 99: Hapus Deliverable Card dari CDP Index - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove the "Deliverable & Evidence" navigation card from CDP Index page because Deliverable is a detail page that requires an ID parameter, not a standalone list page. Users should access deliverable details through the Coaching Proton page by clicking "Lihat Detail" button.
</domain>

<decisions>
## Implementation Decisions

### Navigation Fix
- Remove the "Deliverable & Evidence" card from Views/CDP/Index.cshtml (lines 79-98)
- Do not create a replacement page or redirect
- Users access deliverable details via: CDP Index → Coaching Proton → "Lihat Detail" button

### Scope Clarification
- This is a UI cleanup fix, NOT a workflow change
- Coaching Proton page remains the primary entry point for deliverable management
- Deliverable detail page (CDP/Deliverable?id={x}) continues to work as before

### Claude's Discretion
- Bootstrap grid adjustment after card removal (whether to expand other cards or leave gap)
- No other changes to CDP Index page unless user explicitly requests
</decisions>

<specifics>
## Specific Ideas

- User reported: "page yang tiba tiba muncul, http://localhost:5277/CDP/Deliverable, di menu CMP Index, ada card ini"
- Investigation revealed: Deliverable card links to `/CDP/Deliverable` without required `id` parameter → 404 error
- User decision: Remove card entirely, access deliverable details through Coaching Proton page only

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- None - this is a removal task

### Established Patterns
- CDP Index uses Bootstrap row/col grid for navigation cards
- Cards are self-contained div blocks that can be removed without breaking layout

### Integration Points
- File to modify: `Views/CDP/Index.cshtml`
- Lines to remove: 79-98 (the "Deliverable & Evidence" card div block)
- No controller changes needed (CDP/Deliverable action remains)
- No database changes needed

</code_context>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 99-99*
*Context gathered: 2026-03-05*
