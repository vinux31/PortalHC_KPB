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

  // Phase 308 — PrePost Wizard Validation Fix (D-13 selectors per CONTEXT)
  // Form ID correction per RESEARCH: '#createAssessmentForm' (BUKAN '#createForm' yang ada di CONTEXT.md)
  createForm: '#createAssessmentForm',
  // FORM-10 (Phase 420): DOM id di-rename assessmentTypeInput -> creationMode (penanda mode Standard/PrePostTest).
  assessmentTypeInput: '#creationMode',
  statusFieldWrapper: '#statusFieldWrapper',
  statusSelect: '#Status',
  submitBtn: '#createAssessmentForm button[type="submit"]',
} as const;

// ============================================================
// Phase 317 — Wizard 4-step lengkap + ManagePackageQuestions form + ExtraTime
// Source: Views/Admin/CreateAssessment.cshtml lines 77-815 (verified 2026-05-11)
// Source: Views/Admin/ManagePackageQuestions.cshtml lines 117-458 (verified 2026-05-11)
// Pattern: extend additive — JANGAN refactor `selectors` existing (preserve Phase 307/308 blame)
// ============================================================

export const wizardSelectors = {
  // Step navigation
  step1: '#step-1', step2: '#step-2', step3: '#step-3', step4: '#step-4',
  pill1: '#pill-1', pill2: '#pill-2', pill3: '#pill-3', pill4: '#pill-4',
  btnNext1: '#btnNext1', btnNext2: '#btnNext2', btnNext3: '#btnNext3',
  btnPrev2: '#btnPrev2', btnPrev3: '#btnPrev3', btnPrev4: '#btnPrev4',
  btnSubmit: '#btnSubmit',

  // Step 1 fields
  category: '#Category',
  title: '#Title',
  // FORM-10 (Phase 420): DOM id di-rename assessmentTypeInput -> creationMode.
  assessmentType: '#creationMode',

  // Step 2 fields
  userContainer: '#userCheckboxContainer',
  userCheckItem: '.user-check-item',
  userCheckbox: 'input.user-checkbox',
  selectedCountBadge: '#selectedCountBadge',
  selectAllBtn: '#selectAllBtn',
  deselectAllBtn: '#deselectAllBtn',

  // Step 3 fields
  schedDateInput: '#schedDateInput',
  schedTimeInput: '#schedTimeInput',
  durationMinutes: '#DurationMinutes',
  ewcdDateInput: '#ewcdDateInput',
  ewcdTimeInput: '#ewcdTimeInput',
  status: '#Status',
  passPercentage: '#PassPercentage',
  allowAnswerReview: '#AllowAnswerReview',
  generateCertificate: '#GenerateCertificate',
  isTokenRequired: '#IsTokenRequired',
  accessToken: '#AccessToken',
  validUntil: '#ValidUntil',

  // Phase 379 — additive (markup current; JANGAN refactor existing key). Verified 2026-06-14.
  // Source: CreateAssessment.cshtml token #tokenSection:509; proton #protonFieldsSection:210, #protonTrackSelect:219 (opsi data-tahun:225)
  tokenSection: '#tokenSection',                 // Step 3 — token panel (gantikan drift lama #tokenInputContainer)
  protonFieldsSection: '#protonFieldsSection',   // Step 1 — show saat Category='Assessment Proton'
  protonTrackSelect: '#protonTrackSelect',       // Step 1 — name=ProtonTrackId; opsi punya data-tahun

  // Submit modal
  successModal: '#successModal',
  modalManageBtn: '#modal-manage-btn',
  createdAssessmentData: '#createdAssessmentData',
} as const;

// Phase 317 Plan 02 — AddExtraTime modal
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml lines 132-143 + 1462-1495 (verified 2026-05-11)
export const extraTimeSelectors = {
  triggerBtn: 'button[data-bs-target="#extraTimeModal"]',
  modal: '#extraTimeModal',
  select: '#extraTimeSelect',           // option values: 5, 10, 15, 20, 25, 30, 45, 60, 90, 120
  confirmBtn: '#btnConfirmExtraTime',
  // Effect on HC page: .alert-success/.alert-danger di .container-fluid
} as const;

// ============================================================
// Phase 318 Plan 03 — PrePostTest wizard fields
// Source: Views/Admin/CreateAssessment.cshtml lines 411-465 (verified 2026-05-12)
// Behavior: select #creationMode='PrePostTest' → change event → #ppt-jadwal-section show
// ============================================================
export const prePostWizardSelectors = {
  jadwalSection: '#ppt-jadwal-section',
  preSchedule: '#preSchedule',
  preDurationMinutes: '#preDurationMinutes',
  preExamWindowCloseDate: '#preExamWindowCloseDate',
  postSchedule: '#postSchedule',
  postDurationMinutes: '#postDurationMinutes',
  postExamWindowCloseDate: '#postExamWindowCloseDate',
  samePackageCheck: '#samePackageCheck',
} as const;

export const questionFormSelectors = {
  formCard: '#questionFormCard',
  formTitle: '#formTitle',
  questionForm: '#questionForm',
  editQuestionId: '#editQuestionId',
  questionType: '#QuestionType',
  questionText: '#questionText',
  optionsSection: '#optionsSection',
  maLabel: '#maLabel',
  rubrikSection: '#rubrikSection',
  rubrik: '#rubrik',
  maxCharacters: '#maxCharacters',
  scoreValue: '#scoreValue',
  elemenTeknis: '#elemenTeknis',
  submitBtn: '#submitBtn',
  cancelEditBtn: '#cancelEditBtn',
  optionA: '#option_A', optionB: '#option_B', optionC: '#option_C', optionD: '#option_D',
  correctA: '#correct_A', correctB: '#correct_B', correctC: '#correct_C', correctD: '#correct_D',
  // Phase 355 — image upload fields (Views/Admin/ManagePackageQuestions.cshtml:145-211, hidden file inputs)
  questionImgField: '#questionImgField',
  questionImageAlt: '#questionImageAlt',
  removeQuestionImage: '#removeQuestionImage',
  optAImgField: '#optAImgField', optBImgField: '#optBImgField', optCImgField: '#optCImgField', optDImgField: '#optDImgField',
  optAImageAlt: '#optAImageAlt', optBImageAlt: '#optBImageAlt', optCImageAlt: '#optCImageAlt', optDImageAlt: '#optDImageAlt',
} as const;
