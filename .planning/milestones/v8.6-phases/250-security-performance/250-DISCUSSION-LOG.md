# Phase 250: Security & Performance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-24
**Phase:** 250-security-performance
**Areas discussed:** XSS Escape Strategy, Throttle Mechanism, Console.log Removal
**Mode:** Auto (all decisions auto-selected)

---

## XSS Escape Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Html.Encode() in interpolation | Escape approverName/approvedAt sebelum interpolasi ke HTML | ✓ |
| Refactor to Tag Helper | Ubah @functions helper ke proper Razor Tag Helper | |
| HttpUtility.HtmlEncode | System.Web utility — lebih verbose | |

**User's choice:** [auto] Html.Encode() in interpolation (recommended default — simplest, consistent with Razor)
**Notes:** XSS vector ada di `title` attribute tooltip. approverName dari database bisa mengandung karakter HTML.

---

## Throttle Mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| IMemoryCache | Cache key + 1hr expiry, sudah di DI container | ✓ |
| Static timestamp | Static field di controller — simpler tapi tidak DI-friendly | |
| Distributed cache | Redis/SQL — overkill untuk single-server portal | |

**User's choice:** [auto] IMemoryCache (recommended default — already used by AdminController & CMPController)
**Notes:** IMemoryCache sudah tersedia di DI. Pattern inject + TryGetValue/Set sudah established.

---

## Console.log Removal

| Option | Description | Selected |
|--------|-------------|----------|
| Remove entirely | Hapus 4 console.log tanpa pengganti | ✓ |
| Conditional debug | Ganti dengan if(DEBUG) console.log | |

**User's choice:** [auto] Remove entirely (recommended default — debug logs mengekspos token/response)
**Notes:** 4 lokasi di Assessment.cshtml: line 639, 651, 682, 694.

---

## Claude's Discretion

- Cache key format dan expiration type untuk PERF-01

## Deferred Ideas

None
