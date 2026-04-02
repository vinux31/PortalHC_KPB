# Testing Patterns

**Analysis Date:** 2026-04-02

## Test Framework

**Runner:**
- Playwright v1.58.2 (E2E browser tests)
- Config: `tests/playwright.config.ts`
- TypeScript 5.9.3

**Assertion Library:**
- Playwright built-in `expect` API

**Run Commands:**
```bash
cd tests
npx playwright test              # Run all tests
npx playwright test --headed     # Watch mode (visible browser)
npx playwright test --ui         # Playwright UI mode
npx playwright show-report       # View HTML report
```

**No unit test framework.** There are no C#-side unit tests (no xUnit, NUnit, or MSTest). All testing is E2E via Playwright.

## Test File Organization

**Location:** `tests/e2e/` (separate from source)

**Naming:** `{feature}.spec.ts`

**Structure:**
```
tests/
├── e2e/
│   ├── global.setup.ts          # Verify app is running
│   ├── assessment.spec.ts       # Assessment CRUD flow (277 lines)
│   ├── exam-taking.spec.ts      # Exam flow E2E (1593 lines)
│   └── impersonation.spec.ts    # Impersonation feature (320 lines)
├── helpers/
│   ├── accounts.ts              # Test account credentials
│   ├── auth.ts                  # Login/logout helpers
│   └── utils.ts                 # Date helpers, unique title generator, dialog handler
├── playwright.config.ts
├── package.json
└── tsconfig.json
```

## Test Configuration

**Playwright config (`tests/playwright.config.ts`):**
- `testDir: './e2e'`
- `timeout: 60_000` (60 seconds per test)
- `expect.timeout: 10_000` (10 seconds for assertions)
- `fullyParallel: false` — tests run serially
- `retries: 0` — no retries
- Screenshots: `'on'` (always captured)
- Trace: `'on-first-retry'`
- Viewport: 1280x720
- Base URL: `http://localhost:5277`

**Projects:**
1. `setup` — runs `global.setup.ts` first (verifies app is running)
2. `chromium` — main test project, depends on setup

## Test Structure

**Suite Organization:**
```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';

// Shared state across tests in file
let assessmentTitle: string;

test.describe.configure({ mode: 'serial' });

test.describe('Assessment - Admin Creates & Manages', () => {
  test('1.1 - HC can navigate to Create Assessment page', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    await expect(page.locator('h2')).toContainText('Manage Assessment');
  });
});
```

**Patterns:**
- Tests organized by use-case flows (not by page or role)
- Serial mode within `describe` blocks — tests share state via file-level variables
- Each test logs in fresh (no persistent auth state between tests)
- Numbered test names: `'1.1 - ...'`, `'1.2 - ...'` for flow ordering

## Test Helpers

**Authentication (`tests/helpers/auth.ts`):**
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

**Test Accounts (`tests/helpers/accounts.ts`):**
- `admin` — Admin role
- `hc` — HC role
- `coachee` / `coachee2` — Coachee role
- `direktur`, `vp`, `manager` — Management roles
- `sectionHead`, `srSupervisor` — Supervisor roles
- `coach` — Coach role
- All use password `123456`

**Utilities (`tests/helpers/utils.ts`):**
- `uniqueTitle(prefix)` — generates `{prefix} {timestamp}` for test isolation
- `today()` / `tomorrow()` — date formatters (YYYY-MM-DD)
- `waitForNav(page, action)` — wait for navigation after click
- `autoConfirm(page)` — accept browser `confirm()` dialog

## Global Setup

**`tests/e2e/global.setup.ts`:**
- Verifies the app is running by navigating to `/Account/Login`
- Checks response is OK and submit button is visible
- Runs before all test projects

## Mocking

**No mocking framework.** Tests run against a live local application with real database. No API mocking or service stubs.

## Coverage

**Requirements:** None enforced. No coverage tooling configured.

**Current coverage by feature:**
- Assessment CRUD: covered (`tests/e2e/assessment.spec.ts`)
- Exam-taking flow: covered (`tests/e2e/exam-taking.spec.ts`)
- Impersonation: covered (`tests/e2e/impersonation.spec.ts`)
- Other features (CDP, CMP, coaching, training, workers, notifications): **not covered by automated tests**

## Test Types

**Unit Tests:**
- Not used. No C# test project exists.

**Integration Tests:**
- Not used.

**E2E Tests:**
- Playwright against live local app
- Covers critical user flows end-to-end
- Tests create real data (assessments, etc.) using unique titles for isolation

## Common Patterns

**Page Navigation:**
```typescript
await page.goto('/Admin/ManageAssessment');
await expect(page.locator('h2')).toContainText('Manage Assessment');
```

**Form Filling:**
```typescript
await page.fill('#Title', assessmentTitle);
await page.selectOption('#Category', 'OJT');
await page.fill('#ScheduleDate', today());
await page.click('#submitBtn');
```

**Waiting for Results:**
```typescript
await page.waitForLoadState('networkidle');
await page.waitForTimeout(3_000); // used when networkidle is insufficient
```

**Checkbox Interaction:**
```typescript
const checkbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
await checkbox.click({ force: true });
```

**Search Pattern:**
```typescript
const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
await searchInput.fill(term);
await searchInput.press('Enter');
await page.waitForLoadState('networkidle');
```

## Ad-Hoc UAT Scripts

In addition to Playwright specs, there are ad-hoc JavaScript UAT scripts in the project root:
- `uat-265-test.js`, `uat-266-test.js`, `uat-267-test.js`, etc.
- These are one-off browser console scripts used during UAT phases
- Not part of the automated test suite

## Adding New Tests

**New E2E spec:**
1. Create `tests/e2e/{feature}.spec.ts`
2. Import from `../helpers/auth` and `../helpers/utils`
3. Use `test.describe.configure({ mode: 'serial' })` for flow-based tests
4. Name tests with numbered flow format: `'{N}.{step} - description'`
5. Login at start of each test (no shared auth state)

**New test account:**
- Add to `tests/helpers/accounts.ts`
- Ensure account exists in `Data/SeedData.cs`

---

*Testing analysis: 2026-04-02*
