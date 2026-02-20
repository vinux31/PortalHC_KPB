# Phase 20: Edit, Delete, and RecordsWorkerList Wiring - Context

**Gathered:** 2026-02-20
**Status:** Ready for planning

<domain>
## Phase Boundary

HC or Admin can edit any existing manual training record (all fields except Pekerja, including certificate replace) and delete any manual training record. Both actions are accessible from the WorkerDetail page (where training records are displayed). Assessment session rows are not affected by this phase.

</domain>

<decisions>
## Implementation Decisions

### Edit/Delete location
- Edit and Delete links appear on manual training record rows in **WorkerDetail** only (where records are physically displayed)
- Assessment session rows get **no** Edit/Delete links — manual training rows only
- Edit stays **in-page** on WorkerDetail (no separate edit page navigation) — Claude decides between modal and inline row expansion based on what fits better
- Navigation after save: Claude decides (stay on WorkerDetail or redirect)

### Edit form design
- All fields editable **except Pekerja** — worker assignment cannot be changed on an existing record
- Editable fields: Nama Pelatihan, Penyelenggara, Kategori, Kota, Tanggal, Status, Nomor Sertifikat, Berlaku Sampai, Certificate file
- Navigation after successful save: Claude decides (WorkerDetail reload or redirect)

### Certificate replace on edit
- Edit form shows: **download link to current certificate** + a new file upload input
- Uploading a new file **replaces** the old one; leaving the upload empty keeps the existing certificate unchanged
- When replaced: old certificate file is **physically deleted** from `wwwroot/uploads/certificates/` on disk
- No explicit "remove without replacing" option — HC can only replace, not clear a certificate

### Delete confirmation
- Browser native `confirm()` dialog — no modal, no separate page
- When deleted: the training record DB row AND the associated certificate file (if any) are both deleted
- Navigation after delete: Claude decides (stay on WorkerDetail or redirect)

### Claude's Discretion
- In-page edit pattern: modal dialog vs inline row expand — use whichever is simpler given existing Razor view patterns
- Navigation after save and after delete — stay on WorkerDetail with success flash, or redirect to RecordsWorkerList
- Exact wording of the confirm() dialog
- Button/link styling for Edit and Delete actions within the table row

</decisions>

<specifics>
## Specific Ideas

No specific UI references provided — open to standard Bootstrap patterns consistent with the rest of the app.

</specifics>

<deferred>
## Deferred Ideas

- Edit/Delete for assessment session rows in WorkerDetail — new capability with different data model and ripple effects on competency history; own future phase

</deferred>

---

*Phase: 20-edit-delete-recordsworkerlist-wiring*
*Context gathered: 2026-02-20*
