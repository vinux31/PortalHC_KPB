# Testing Patterns

**Analysis Date:** 2026-04-02

## Test Framework

**Runner:**
- Playwright 1.58.2 (E2E browser tests)
- Config: `tests/playwright.config.ts`
- No unit test framework (no xUnit/NUnit/MSTest project exists)

**Assertion Library:**
- Playwright's built-in `expect()` assertions

**Run Commands:**
```bash
cd tests
npx playwright test              # Run all tests
npx playwright test --headed     # Run with visible browser
npx playwright test --ui         # Interactive UI mode
npx playwright show-report       # View HTML report
```

## Test File Organization

**Location:**
- All tests in `tests/e2e/` directory (separate from main .NET project)
- Test helpers in `tests/helpers/`
- Separate `tests/package.json` with its own Node.js dependencies

**Naming:**
- `{feature}.spec.ts` — `assessment.spec.ts`, `exam-taking.spec.ts`, `impersonation.spec.ts`

**Structure:**
```
tests/
├── e2e/
│   ├── global.setup.ts           # Global setup (auth bootstrap)
│   ├── assessment.spec.ts        # 277 lines - Assessment CRUD flows
│   ├── exam-taking.spec.ts       # 1593 lines - Full exam lifecycle
│   └── impersonation.spec.ts     # 320 lines - Impersonation feature
├── helpers/
│   ├── accounts.ts               # Test account credentials (10 roles)
│   ├── auth.ts                   # login()/logout() helpers
│   └── utils.ts                  # uniqueTitle(), today(), autoConfirm()
├── playwright.config.ts
├── package.json
└── tsconfig.json
```

## Test Structure

**Suite Organization:**
```typescript
// Tests run serially within a describe block (shared state)
test.describe.configure({ mode: 'serial' });

// Organized by USE-CASE FLOWS, not pages or roles
test.describe('Flow A: Legacy Exam Full Lifecycle', () => {
  let title: string;

  test('A1 - HC creates assessment for coachee', async ({ page }) => {
    await login(page, 'hc');
    // ... test steps
  });

  test('A2 - HC navigates to ManageQuestions', async ({ page }) => {
    await login(page, 'hc');
    // ... uses title from A1
  });
});
```

**Key pattern: Serial flow tests with shared state.**
Tests within a `describe` block share variables (e.g., `assessmentTitle`) and must run in order. Each test logs in fresh (no shared browser session).

**Naming convention:**
- Flow prefix + step number: `'1.1 - HC can navigate to Create Assessment page'`
- Or letter prefix: `'A1 - HC creates assessment for coachee'`

## Test Helpers

**Authentication — `tests/helpers/auth.ts`:**
```typescript
export async function login(page: Page, account: AccountKey) {
  const { email, password } = accounts[account];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL('**/Home/**', { timeout: 15_000 });
}
```

**Account keys (in `tests/helpers/accounts.ts`):**
`admin`, `hc`, `coachee`, `coachee2`, `direktur`, `vp`, `manager`, `sectionHead`, `srSupervisor`, `coach`

**Utility helpers — `tests/helpers/utils.ts`:**
```typescript
export function uniqueTitle(prefix = 'E2E Test'): string {
  return `${prefix} ${Date.now()}`;           // Test isolation via unique names
}

export function today(): string {
  return new Date().toISOString().split('T')[0]; // YYYY-MM-DD format
}

export function autoConfirm(page: Page) {
  page.once('dialog', dialog => dialog.accept()); // Accept browser confirm()
}
```

## Playwright Configuration

**Key settings in `tests/playwright.config.ts`:**
- `baseURL`: `http://localhost:5277` (local dev server)
- `timeout`: 60 seconds per test
- `expect.timeout`: 10 seconds for assertions
- `fullyParallel`: false (tests are serial by design)
- `retries`: 0 (no automatic retries)
- `screenshot`: 'on' (always capture)
- `trace`: 'on-first-retry'
- `viewport`: 1280x720
- Browser: Chromium only
- Projects: `setup` (global.setup.ts) → `chromium`

## Common Test Patterns

**Page navigation + assertion:**
```typescript
await login(page, 'hc');
await page.goto('/Admin/ManageAssessment');
await expect(page.locator('h2')).toContainText('Manage Assessment');
```

**Form filling:**
```typescript
await page.fill('#Title', assessmentTitle);
await page.selectOption('#Category', 'OJT');
await page.fill('#ScheduleDate', today());
await page.fill('#DurationMinutes', '30');
await page.click('#submitBtn');
```

**Waiting for success (modal or alert):**
```typescript
await page.waitForTimeout(3_000);
const successVisible = await page.locator('#successModal')
  .evaluate(el => el.classList.contains('show')).catch(() => false);
const alertVisible = await page.locator('.alert-success').isVisible().catch(() => false);
expect(successVisible || alertVisible).toBeTruthy();
```

**Search and verify:**
```typescript
const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
await searchInput.fill(term);
await searchInput.press('Enter');
await page.waitForLoadState('networkidle');
await expect(page.locator('body')).toContainText(assessmentTitle);
```

**Checkbox interaction (force click for custom styled checkboxes):**
```typescript
await page.locator('.user-check-item', { hasText: 'rino.prasetyo' })
  .locator('input').click({ force: true });
```

**Error testing (dialog handling):**
```typescript
export function autoConfirm(page: Page) {
  page.once('dialog', dialog => dialog.accept());
}
```

## Ad-hoc UAT Scripts

**Location:** Root directory — `uat-265-test.js`, `uat-266-test.js`, `uat-267-*.js`
- One-off Playwright scripts for specific phase UAT
- Not part of the formal test suite (not in `tests/e2e/`)
- Used for manual verification during development
- Pattern: Claude writes script -> user runs in browser context -> Claude fixes bugs

## Coverage

**Requirements:** None enforced — no coverage tool configured
**Unit test coverage:** No unit tests exist
**E2E coverage areas:**
- Assessment creation, management, and monitoring
- Exam lifecycle (create -> add questions -> take exam -> submit -> results -> certificate)
- Impersonation feature

## Test Types

**Unit Tests:**
- Not present. No xUnit/NUnit/MSTest project in the solution.

**Integration Tests:**
- Not present as a separate layer.

**E2E Tests (Playwright):**
- Primary testing strategy
- 3 spec files, ~2190 total lines
- Tests the full stack: browser -> ASP.NET Core -> SQL Server
- Requires running local dev server at `http://localhost:5277`

**Manual UAT:**
- User preference: Claude analyzes code -> user verifies in browser -> Claude fixes bugs
- UAT results tracked in planning docs with PASS/FAIL per test case
- Some flows need pre-seeded data (e.g., Proton coaching requires mapped coach-coachee pairs)
- Bug handling rules: low -> fix immediately, medium -> fix if simple, critical -> document first

## Adding New Tests

**New E2E test file:**
1. Create `tests/e2e/{feature}.spec.ts`
2. Import helpers: `import { login } from '../helpers/auth';` and `import { uniqueTitle, today } from '../helpers/utils';`
3. Use `test.describe.configure({ mode: 'serial' });` for flow-based tests
4. Name tests with flow prefix: `'F1 - Description'`
5. Each test should `login()` fresh (no shared auth state between tests)
6. Use `uniqueTitle()` for test data isolation

**New test account:**
1. Add entry to `tests/helpers/accounts.ts` with email, password, role
2. Ensure account exists in database seed data

**New helper function:**
1. Add to `tests/helpers/utils.ts` for generic utilities
2. Create new helper file in `tests/helpers/` for feature-specific utilities

---

*Testing analysis: 2026-04-02*
