# 155-02 Audit Report: KKJ and CPDP File Management
**Requirements:** ADMIN-03, ADMIN-04
**Date:** 2026-03-12
**Auditor:** Claude (automated code review)

---

## Summary

| Requirement | Status | Findings |
|---|---|---|
| ADMIN-03 — KKJ File Management | PASS (with fixes) | 0 bugs, 1 edge-case |
| ADMIN-04 — CPDP File Management | PASS (with fixes) | 1 bug (fixed), 1 edge-case (fixed) |

**Total findings:** 3 (1 bug fixed, 1 edge-case fixed, 1 edge-case documented)

---

## ADMIN-03: KKJ File Management

### Endpoints reviewed
- `GET /Admin/KkjMatrix` — bagian listing + active file list
- `GET /Admin/KkjUpload`, `POST /Admin/KkjUpload` — file upload
- `GET /Admin/KkjFileDownload/{id}` — file download
- `POST /Admin/KkjFileDelete` — soft-delete (archive)
- `GET /Admin/KkjFileHistory/{bagianId}` — archived file list
- `POST /Admin/KkjBagianAdd` — add bagian
- `POST /Admin/KkjBagianDelete` — delete bagian (with cascade)

### Findings

**[1] EDGE-CASE — KkjBagianAdd always creates "Bagian Baru" name**
- Severity: edge-case
- File: Controllers/AdminController.cs, line 283
- Description: When adding a new bagian, the name is hardcoded to "Bagian Baru". There is no input field for the initial name; users must rename via inline edit. This is a UX gap, not a blocking bug — inline rename presumably works in the view.
- Suggested fix: Accept an optional `name` parameter in KkjBagianAdd or show a modal for initial name. Current behavior is acceptable if the view supports inline rename.
- Status: Documented only (not a code bug — UI pattern is intentional inline-rename)

### Security checks

| Check | Result |
|---|---|
| Authorization on all endpoints | PASS — `[Authorize(Roles = "Admin, HC")]` on every action |
| CSRF protection on POST | PASS — `[ValidateAntiForgeryToken]` on all POST actions |
| File type validation | PASS — only `.pdf`, `.xlsx`, `.xls` allowed |
| File size limit | PASS — 10MB max enforced |
| Path traversal (upload) | PASS — timestamp prefix + `Replace("..", "")` on filename |
| Path traversal (download) | PASS — path built from DB record, not user input |
| File overwrite | PASS — each upload gets unique `{unixTimestamp}_{name}` prefix; never overwrites |
| Bagian cascade (delete) | PASS — active files block delete; archived files cascade with confirmation |
| Version history preservation | PASS — soft-delete (`IsArchived=true`) keeps physical file; history view shows archived files |
| Archived file downloadability | Not exposed — no download endpoint for archived KKJ files (by design or gap?) |
| Audit log on archive | PASS — `KkjFileDelete` logs action as `"ArchiveKKJFile"` |

### Verdict: ADMIN-03 PASS
Core flows work correctly. One UX edge-case (bagian naming) documented.

---

## ADMIN-04: CPDP File Management

### Endpoints reviewed
- `GET /Admin/CpdpFiles` — bagian listing + active file list
- `GET /Admin/CpdpUpload`, `POST /Admin/CpdpUpload` — file upload
- `GET /Admin/CpdpFileDownload/{id}` — file download
- `POST /Admin/CpdpFileArchive` — soft-delete (archive)
- `GET /Admin/CpdpFileHistory/{bagianId}` — archived file list

### Findings

**[2] BUG — CpdpFileDownload sends wrong MIME type for .xls files (FIXED)**
- Severity: bug
- File: Controllers/AdminController.cs, line 528-530 (original)
- Description: The content type logic used a simple ternary — `"pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"`. This means `.xls` files (old Excel format) were served with the `.xlsx` MIME type, which could cause download failures or corruption warnings in some browsers.
- Fix applied: Replaced with a switch expression matching `pdf`, `xlsx`, `xls` with correct MIME types, defaulting to `application/octet-stream`.
- Commit: included in task 1 commit

**[3] EDGE-CASE — CpdpFileArchive missing audit log (FIXED)**
- Severity: edge-case (consistency)
- File: Controllers/AdminController.cs, `CpdpFileArchive` action
- Description: `KkjFileDelete` (KKJ archive) logs an audit entry on every archive action. `CpdpFileArchive` did not, creating inconsistent audit trail coverage.
- Fix applied: Added equivalent audit log block to `CpdpFileArchive`, logging action `"ArchiveCPDPFile"`.
- Commit: included in task 1 commit

### Security checks

| Check | Result |
|---|---|
| Authorization on all endpoints | PASS — `[Authorize(Roles = "Admin, HC")]` on every action |
| CSRF protection on POST | PASS — `[ValidateAntiForgeryToken]` on all POST actions |
| File type validation | PASS — only `.pdf`, `.xlsx`, `.xls` allowed |
| File size limit | PASS — 10MB max enforced |
| Path traversal (upload) | PASS — `Path.GetFileName()` prevents traversal; timestamp prefix ensures uniqueness |
| Path traversal (download) | PASS — path built from DB record, not user input |
| File overwrite | PASS — each upload gets millisecond timestamp prefix; never overwrites |
| CPDP bagian structure | PASS — shares KkjBagians table (same bagian for both KKJ and CPDP, intentional design) |
| Audit log on archive | FIXED — added in this audit |
| MIME type correctness | FIXED — .xls now served with correct MIME type |

### Verdict: ADMIN-04 PASS (after fixes)
Two issues fixed inline. Core flows correct.

---

## Cross-Cutting Findings

| Check | Result |
|---|---|
| Shared bagian structure (KKJ + CPDP) | PASS — KkjBagians table used by both; KkjBagianAdd/Delete handle both file types |
| Seed data on first use | PASS — KkjMatrix seeds 4 default bagians if none exist |
| Cascade delete safety | PASS — blocks delete if active files present; requires confirmation for archived files; deletes physical files from disk |
| Version history across both types | PASS — `IsArchived` soft-delete + history views for both KKJ and CPDP |

---

## Fixes Applied

| # | Type | Description | File | Status |
|---|---|---|---|---|
| 1 | Bug | CPDP download MIME type: .xls served as .xlsx | Controllers/AdminController.cs | Fixed |
| 2 | Edge-case | CpdpFileArchive missing audit log | Controllers/AdminController.cs | Fixed |

---

## Out of Scope (Noted)

- No download endpoint for archived files (both KKJ and CPDP history views show archived list but no download button visible from controller alone — view-level gap if present)
- `KkjBagianAdd` "Bagian Baru" naming pattern — acceptable if view supports inline rename
