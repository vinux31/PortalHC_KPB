# Phase 196: Shared Service Extraction - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Extract 4 duplicated private helper methods from AdminController and CMPController into a single shared service class. Both controllers delegate to the service with identical results. No new features — purely internal refactoring.

Methods to extract: GetUnifiedRecords(), GetAllWorkersHistory(), GetWorkersInSection(), NotifyIfGroupCompleted().

</domain>

<decisions>
## Implementation Decisions

### NotifyIfGroupCompleted — Logic Divergence Resolution (SVC-04)
- Use the Admin version: group is "done" when all sessions are `Completed` OR `Cancelled`
- Rationale: if a worker is cancelled (resign, sakit, dll), the group should not be stuck forever waiting for a notification that will never fire
- The CMP-only version (require all Completed) is discarded as incorrect behavior

### GetUnifiedRecords (SVC-01)
- Straightforward extraction — both Admin (line 5545) and CMP (line 1037) have identical logic
- No divergence to resolve

### GetAllWorkersHistory (SVC-02)
- Straightforward extraction — both Admin (line 5593) and CMP (line 1090) have identical logic (~80 lines)
- No divergence to resolve

### GetWorkersInSection (SVC-03)
- Straightforward extraction — both Admin (line 5667) and CMP (line 1176) have identical logic (~100 lines)
- No divergence to resolve

### Claude's Discretion
- Service class naming and interface design
- Constructor injection pattern (follow existing Services/ conventions)
- Whether to create one service class or split by concern
- Method signatures (keep same or optimize parameters)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Source methods (Admin)
- `Controllers/AdminController.cs` line 5545 — GetUnifiedRecords()
- `Controllers/AdminController.cs` line 5593 — GetAllWorkersHistory()
- `Controllers/AdminController.cs` line 5667 — GetWorkersInSection()
- `Controllers/AdminController.cs` line 2722 — NotifyIfGroupCompleted() (CORRECT version — allows Cancelled)

### Source methods (CMP)
- `Controllers/CMPController.cs` line 1037 — GetUnifiedRecords()
- `Controllers/CMPController.cs` line 1090 — GetAllWorkersHistory()
- `Controllers/CMPController.cs` line 1176 — GetWorkersInSection()
- `Controllers/CMPController.cs` line 2866 — NotifyIfGroupCompleted() (INCORRECT version — to be replaced)

### Existing service patterns
- `Services/NotificationService.cs` — Reference for DI registration and service pattern
- `Services/AuditLogService.cs` — Reference for service conventions

### Requirements
- `.planning/REQUIREMENTS.md` — SVC-01 through SVC-04 definitions

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/` folder with established DI pattern (interface + implementation)
- `NotificationService` already injected in both controllers — new service follows same pattern
- `Models/WorkerTrainingStatus.cs` — shared model used by GetWorkersInSection()
- UnifiedTrainingRecord, AllWorkersHistoryRow — existing model classes used by these methods

### Established Patterns
- Services use constructor injection via `Program.cs` registration
- Interface + concrete class pattern (e.g., INotificationService / NotificationService)
- Both controllers already inject `ApplicationDbContext`, `UserManager`, `ILogger`, `INotificationService`

### Integration Points
- New service registered in `Program.cs` (DI container)
- AdminController and CMPController replace private methods with service calls
- NotifyIfGroupCompleted uses `_notificationService` internally — new service needs NotificationService injected

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 196-shared-service-extraction*
*Context gathered: 2026-03-18*
