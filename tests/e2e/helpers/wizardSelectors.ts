// Phase 307 — DOM ID selectors untuk Step 2 panel "Peserta Terpilih"
// Centralized selector constants — single source of truth untuk Playwright tests.
// IDs match production markup di Views/Admin/CreateAssessment.cshtml.

export const selectors = {
  // Phase 307 panel (Wave 1 markup — sesudah line 309 #userCheckboxContainer)
  panelWrapper: '#selected-participants-panel-wrapper',
  panelBody: '#selected-participants-panel',
  panelCount: '#selected-participants-count',

  // Step 4 summary (parity check target — Phase 307 markup refactor Option A)
  summaryListContainer: '#summary-peserta-list-container',
  summaryCount: '#summary-peserta-count',

  // Existing Step 2 (Phase 304 D-18 stability — TIDAK di-touch)
  filterBarBadge: '#selectedCountBadge',
  userContainer: '#userCheckboxContainer',
  protonContainer: '#protonUserCheckboxContainer',
} as const;
