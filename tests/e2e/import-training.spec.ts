import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import {
  uploadAndProcessImport,
  verifyImportSummary,
  verifyTrainingInList,
  verifyAssessmentInList
} from '../helpers/import-helpers';

// Serial mode untuk menghindari conflicts
test.describe.configure({ mode: 'serial' });

test.describe('Import Training - Training Type', () => {
  // Tests will be added in next tasks
});

test.describe('Import Training - Assessment Type', () => {
  // Tests will be added in next tasks
});
