# Phase 192: ValidUntil & NomorSertifikat - Research

**Researched:** 2026-03-17
**Domain:** EF Core migration, C# concurrency-safe sequence generation, ASP.NET Core controller logic
**Confidence:** HIGH

## Summary

This phase adds two fields to `AssessmentSession`: `ValidUntil` (already a model property from Phase 191, just needs propagation in the POST loop) and `NomorSertifikat` (new property, new DB column with UNIQUE constraint, auto-generated at session creation time).

The certificate number format is `KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}` where SEQ is 3-digit zero-padded and resets per calendar year. Concurrency is handled via a retry loop (max 3 attempts) that catches `DbUpdateException` on UNIQUE constraint violation and increments the sequence.

All work is confined to two files: `Models/AssessmentSession.cs` (add NomorSertifikat property) and `Controllers/AdminController.cs` (assign ValidUntil + NomorSertifikat in session creation loop), plus EF migration + DbContext configuration.

**Primary recommendation:** Add NomorSertifikat as nullable string to AssessmentSession model, configure UNIQUE index in ApplicationDbContext, generate certificate numbers before `SaveChangesAsync` using a MAX(seq)+1 query scoped to the current year, and wrap the batch save in a retry loop on `DbUpdateException`.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Format: `KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}` — e.g., `KPB/042/III/2026`
- Sequence: 3 digits, zero-padded (001–999)
- Sequence resets per year (starts at 001 every January)
- Roman month based on assessment creation date (bulan saat Admin buat assessment)
- All sessions in same batch get same month (bulan pembuatan)
- NomorSertifikat generated at assessment creation time (saat Admin klik "Buat Assessment")
- Every AssessmentSession gets a number immediately — regardless of whether user passes exam later
- Concurrency handling: retry with next sequence number on UNIQUE constraint violation (max 3 retries)
- All sessions in a batch get the same ValidUntil date from the wizard
- ValidUntil is optional — null means no expiry
- ValidUntil property already exists on AssessmentSession (Phase 191)
- POST action already has ModelState.Remove("ValidUntil") (Phase 191)
- Existing sessions (pre-Phase 192) keep NomorSertifikat = null — no backfill
- NomorSertifikat stored in DB only — no UI display in this phase
- New column: `NomorSertifikat` (string, nullable, UNIQUE constraint) on AssessmentSessions table

### Claude's Discretion
- Exact retry loop implementation for concurrency handling
- Roman numeral conversion helper method placement
- Whether to use a helper method or inline for sequence number generation
- Migration naming convention

### Deferred Ideas (OUT OF SCOPE)
- Tampilkan NomorSertifikat di halaman CMP Records — phase selanjutnya
- Backfill nomor sertifikat untuk session lama — tidak dilakukan
- NomorSertifikat di PDF sertifikat — Phase 194
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CERT-01 | Admin/HC dapat mengatur tanggal expired (ValidUntil) pada sertifikat assessment online | ValidUntil already exists on model (Phase 191); POST loop must assign `session.ValidUntil = model.ValidUntil` for each created session |
| CERT-02 | Sistem men-generate nomor sertifikat otomatis saat sertifikat terbit (format per CONTEXT: KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}) | Sequence query + generation logic + UNIQUE index + retry loop in CreateAssessment POST |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core (Microsoft.EntityFrameworkCore) | Already in project | DB migration, UNIQUE index | Project ORM — all schema changes go through EF migrations |
| dotnet ef CLI | Already in project | Generate migration | Standard project workflow |

No new NuGet packages required. All work uses existing project dependencies.

**Migration command:**
```bash
dotnet ef migrations add AddNomorSertifikatToAssessmentSessions
dotnet ef database update
```

## Architecture Patterns

### Existing Session Creation Loop (lines 1136–1168, AdminController.cs)

The loop iterates `UserIds`, constructs `AssessmentSession` objects, adds them all to `_context.AssessmentSessions`, then calls `SaveChangesAsync` inside a transaction. NomorSertifikat assignment must happen inside this loop, and the save must be wrapped in a retry for UNIQUE conflicts.

### Pattern 1: Sequence Number Query

Query the highest existing sequence for the current year from DB before building sessions:

```csharp
// Source: project convention — EF LINQ query on AssessmentSessions
int year = DateTime.Now.Year;
// Extract seq from "KPB/042/III/2026" — last segment is year, second is seq
var maxSeq = await _context.AssessmentSessions
    .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
    .Select(s => s.NomorSertifikat!)
    .ToListAsync();
// Parse seq from each string: split by '/', take index 1, parse int
int nextSeq = maxSeq.Count == 0 ? 1 :
    maxSeq.Select(n => { var parts = n.Split('/'); return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0; })
          .Max() + 1;
```

**Alternative (simpler):** Use `LIKE 'KPB/%/{year}'` via raw SQL to get MAX seq if LINQ string parsing feels fragile.

### Pattern 2: Retry Loop on DbUpdateException

```csharp
// Source: project decision in STATE.md — "retry with next sequence number on UNIQUE constraint violation (max 3 retries)"
int attempt = 0;
const int maxAttempts = 3;
while (attempt < maxAttempts)
{
    attempt++;
    // Assign certificate numbers to all sessions
    for (int i = 0; i < sessions.Count; i++)
    {
        sessions[i].NomorSertifikat = GenerateCertNumber(nextSeq + i, creationDate);
    }
    _context.AssessmentSessions.AddRange(sessions);
    try
    {
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        break; // success
    }
    catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
    {
        // Rollback, re-query max seq, retry
        await transaction.RollbackAsync();
        if (attempt >= maxAttempts) throw;
        nextSeq = await QueryMaxSeq(year) + 1;
        // Re-open transaction
        transaction = await _context.Database.BeginTransactionAsync();
        // Detach previously tracked sessions so they can be re-added
        foreach (var s in sessions) _context.Entry(s).State = EntityState.Detached;
    }
}
```

**Key detail:** After rollback, the sessions already in `_context`'s change tracker must be detached before re-adding with new certificate numbers. Use `_context.Entry(s).State = EntityState.Detached`.

### Pattern 3: UNIQUE Constraint in ApplicationDbContext

```csharp
// Source: project convention — same pattern as other UNIQUE indexes in ApplicationDbContext.cs
entity.HasIndex(a => a.NomorSertifikat)
    .IsUnique()
    .HasFilter("[NomorSertifikat] IS NOT NULL"); // Partial index: nulls excluded from uniqueness
```

The `HasFilter` is critical — NULL values must be excluded so pre-Phase-192 sessions (with `NomorSertifikat = null`) don't violate the constraint against each other.

### Pattern 4: Roman Numeral Helper

```csharp
private static string ToRomanMonth(int month) => month switch
{
    1 => "I", 2 => "II", 3 => "III", 4 => "IV",
    5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII",
    9 => "IX", 10 => "X", 11 => "XI", 12 => "XII",
    _ => throw new ArgumentOutOfRangeException(nameof(month))
};

private static string GenerateCertNumber(int seq, DateTime date)
    => $"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}";
```

Place these as private static methods in AdminController (same file as the POST action).

### ValidUntil Propagation

Already in model. Just add to session initializer inside the `foreach` loop:

```csharp
var session = new AssessmentSession
{
    // ... existing fields ...
    ValidUntil = model.ValidUntil,  // ADD THIS LINE
    NomorSertifikat = null,          // will be set after seq query
};
```

### Anti-Patterns to Avoid

- **Generating seq by count:** `SELECT COUNT(*) + 1` is wrong — gaps from deleted records cause collisions.
- **Generating seq without year filter:** Must scope to current year since sequence resets annually.
- **No partial index on UNIQUE:** Without `HasFilter("[NomorSertifikat] IS NOT NULL")`, SQL Server treats two NULLs as duplicates at index level (varies by DB behavior — safest to always use filter).
- **Not detaching sessions after rollback:** Change tracker retains old state — re-Add without detach causes EF tracking conflicts.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| UNIQUE enforcement | Application-level duplicate check | DB UNIQUE index + catch DbUpdateException | Race condition between check and insert |
| Sequence numbering | Application counter / Guid | DB MAX query + retry | Only source of truth is the DB |

## Common Pitfalls

### Pitfall 1: Partial Index on UNIQUE Constraint
**What goes wrong:** Without `HasFilter`, multiple existing sessions with `NomorSertifikat = null` violate the UNIQUE index — migration applies but DB throws on existing data.
**Why it happens:** SQL Server UNIQUE index treats NULL = NULL by default (unlike ANSI standard).
**How to avoid:** Always use `.HasFilter("[NomorSertifikat] IS NOT NULL")` on the UNIQUE index.
**Warning signs:** Migration applies but `dotnet ef database update` fails or app throws on load.

### Pitfall 2: Change Tracker State After Rollback
**What goes wrong:** On retry, EF tries to INSERT sessions that are still in `Added` state from previous attempt — duplicate tracking exception.
**Why it happens:** `RollbackAsync` reverts DB transaction but does NOT clear EF change tracker.
**How to avoid:** After rollback, loop through sessions and set `_context.Entry(s).State = EntityState.Detached`.

### Pitfall 3: Sequence Race Condition Window
**What goes wrong:** Two simultaneous requests both query MAX seq = 42, both try to insert 43 — one gets UNIQUE violation.
**Why it happens:** No DB-level locking on the MAX query.
**How to avoid:** This is expected and handled by the retry loop. The losing request retries with MAX+1 after the winner commits.

### Pitfall 4: Wrong Year for Roman Month
**What goes wrong:** Using `DateTime.UtcNow` vs `DateTime.Now` can give different month/year near midnight UTC if server is in a non-UTC timezone.
**How to avoid:** Decide explicitly — `DateTime.Now` (local server time) is more intuitive for Indonesian admin context. Be consistent with how `Schedule` is handled.

## Code Examples

### Full GenerateCertNumber implementation
```csharp
// Place in AdminController.cs as private static methods
private static string ToRomanMonth(int month) => month switch
{
    1 => "I", 2 => "II", 3 => "III", 4 => "IV",
    5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII",
    9 => "IX", 10 => "X", 11 => "XI", 12 => "XII",
    _ => throw new ArgumentOutOfRangeException(nameof(month))
};

private static string BuildCertNumber(int seq, DateTime date)
    => $"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}";
```

### DbContext UNIQUE index configuration
```csharp
// In ApplicationDbContext.cs, inside AssessmentSession entity configuration block
entity.HasIndex(a => a.NomorSertifikat)
    .IsUnique()
    .HasFilter("[NomorSertifikat] IS NOT NULL")
    .HasDatabaseName("IX_AssessmentSessions_NomorSertifikat_Unique");
```

### IsDuplicateKeyException helper
```csharp
private static bool IsDuplicateKeyException(DbUpdateException ex)
{
    // SQL Server error 2601 (unique index) or 2627 (unique constraint)
    return ex.InnerException?.Message.Contains("IX_AssessmentSessions_NomorSertifikat") == true
        || ex.InnerException?.Message.Contains("2601") == true
        || ex.InnerException?.Message.Contains("2627") == true;
}
```

### AssessmentSession model addition
```csharp
// Add to Models/AssessmentSession.cs after ValidUntil property
/// <summary>
/// Auto-generated certificate number in format KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}.
/// Generated at assessment creation time. Null for sessions created before Phase 192.
/// UNIQUE constraint enforced at DB level (partial index excludes nulls).
/// </summary>
public string? NomorSertifikat { get; set; }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| N/A (new feature) | Sequence + UNIQUE + retry | Phase 192 | First certificate numbering in project |

## Open Questions

1. **DateTime.Now vs DateTime.UtcNow for certificate date**
   - What we know: Server is likely in WIB (UTC+7); `DateTime.Now` gives local time
   - What's unclear: Whether existing code uses UTC or local consistently for scheduling
   - Recommendation: Use `DateTime.Now` for the certificate date — it matches the admin's visible creation date

2. **Batch session count vs sequence slots**
   - What we know: All sessions in a batch need consecutive unique numbers
   - What's unclear: Whether to reserve all N slots before inserting (e.g., nextSeq to nextSeq+N-1)
   - Recommendation: Pre-assign consecutive numbers in the loop (`nextSeq + i`), then retry the whole batch if any collision occurs — simpler than individual retries

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project pattern) |
| Config file | none |
| Quick run command | Browser: create assessment, verify DB |
| Full suite command | Browser: multi-user batch, check NomorSertifikat uniqueness |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CERT-01 | ValidUntil date stored on all sessions in batch | manual-smoke | Create assessment with ValidUntil set; query AssessmentSessions WHERE Title=X; confirm ValidUntil = expected | N/A |
| CERT-02 | NomorSertifikat generated in KPB/SEQ/ROMAN/YEAR format | manual-smoke | Create assessment; query DB; verify format and uniqueness | N/A |
| CERT-02 | Null ValidUntil (no expiry) does not error | manual-smoke | Create assessment without ValidUntil; confirm sessions created successfully | N/A |
| CERT-02 | NomorSertifikat UNIQUE — duplicate impossible | manual-smoke | Two browser tabs simultaneously; verify no duplicate numbers in DB | N/A |

### Sampling Rate
- **Per task commit:** Verify in browser after migration + code change
- **Per wave merge:** Full assessment creation flow with multi-user batch
- **Phase gate:** All 4 manual checks green before `/gsd:verify-work`

### Wave 0 Gaps
None — no automated test infrastructure needed; project uses browser verification pattern.

## Sources

### Primary (HIGH confidence)
- Direct code read: `Models/AssessmentSession.cs` — confirmed ValidUntil at line 65, NomorSertifikat absent
- Direct code read: `Data/ApplicationDbContext.cs` — confirmed AssessmentSession entity config block (lines 106–133), existing UNIQUE index patterns (e.g., lines 184, 274, 285)
- Direct code read: `Controllers/AdminController.cs` lines 946–1224 — confirmed session creation loop structure, existing ModelState.Remove patterns, transaction pattern
- Direct read: `192-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence)
- EF Core partial index syntax (`.HasFilter()`) — verified against multiple project uses of `.HasFilter` at line 273 (CoachCoacheeMapping)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — existing project tools, no new packages
- Architecture: HIGH — based on direct code inspection of modification targets
- Pitfalls: HIGH — partial index NULL behavior is a known SQL Server characteristic, change tracker behavior is EF Core documented behavior

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (stable domain — EF Core + SQL Server behavior)
