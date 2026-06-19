# Deferred Items — Phase 392

Out-of-scope discoveries during execution. NOT fixed in this phase (view-only scope; shared infra frozen).

## DEF-392-01 — `initFormLoading` disables submit button on a validation-rejected submit (shared infra)

- **Found during:** Plan 392-02 Task 1 (Playwright e2e create flow).
- **File:** `wwwroot/js/shared-loading.js` (`initFormLoading`, pre-existing — last touched commit `8c504bc3`, long before Phase 392).
- **Symptom:** `initFormLoading` registers a native `submit` listener that disables the submit button + shows "Menyimpan…". When jQuery-unobtrusive validation cancels an INVALID submit (`preventDefault`), the native listener still fires (preventDefault does not stop other listeners on the same event) → the button is disabled even though no navigation happened. A user who fixes the validation error and resubmits hits a permanently-disabled button (must reload to recover).
- **Why deferred:** OUT OF SCOPE for Phase 392 (view-only — `Views/Admin/CreateWorker.cshtml`; controller/model/EditWorker frozen). `shared-loading.js` is shared infra used by many forms across the app; changing its disable semantics is an app-wide behavior change requiring its own phase + regression sweep (Rule 4 territory — not a CreateWorker-local bug).
- **Test handling:** The e2e spec reloads `/Admin/CreateWorker` (fresh enabled button) between the validation-rejection assertion and the real create submission — separating the two assertions cleanly and avoiding the stale-disable interaction. This does NOT mask the issue (it is logged here); it isolates the two behaviors the spec verifies.
- **Suggested fix (future):** Make `initFormLoading` only disable the button when the form is actually valid — e.g. gate on `$(form).valid()` if jQuery-validate is present, or disable in a listener registered AFTER validation that checks `event.defaultPrevented`. Re-enable on validation failure.
- **Severity:** LOW (cosmetic recovery friction; reload fixes it; does not corrupt data).
