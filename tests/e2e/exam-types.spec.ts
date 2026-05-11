import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today } from '../helpers/utils';
import {
  createAssessmentViaWizard,
  addQuestionViaForm,
  submitExamTwoStep,
  type QuestionInput,
} from './helpers/examTypes';
import { verifyResultPage } from './helpers/examMatrix';

// Sequential mode — per-flow describe shares state (assessmentId/packageId/sessionId) antar sub-tests.
test.describe.configure({ mode: 'serial' });

const FLOW_TIMEOUT_MS = 120_000;

test.describe('smoke wave-0 (verify RESEARCH A4 + A5 assumptions)', () => {
  // BODY ditulis di Task 2 (Wave 0 smoke verification)
  test('placeholder', async () => {
    expect(true).toBe(true);
  });
});

test.describe('FLOW K — MA Full Cycle', () => {
  // BODY ditulis di Task 3
  test('placeholder', async () => {
    expect(true).toBe(true);
  });
});

test.describe('FLOW L — Essay Full Cycle + HC Grading', () => {
  // BODY ditulis di Task 4
  test('placeholder', async () => {
    expect(true).toBe(true);
  });
});

// Suppress unused-import warnings — these symbols dipakai di Task 2-4 bodies.
void login;
void uniqueTitle;
void today;
void createAssessmentViaWizard;
void addQuestionViaForm;
void submitExamTwoStep;
void verifyResultPage;
void ({} as QuestionInput);
