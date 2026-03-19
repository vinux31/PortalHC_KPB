# Phase 204: CDP Certification Management Enhancement - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Enhance CDP CertificationManagement page to hide renewed certificates by default and fix summary card counts. No new pages or endpoints — modifications to existing controller logic and views only.

</domain>

<decisions>
## Implementation Decisions

### Visual Behavior (Tabel & Toggle)
- Baris sertifikat yang sudah di-renew ditampilkan dengan opacity 50% (redup) saat toggle aktif
- Toggle "Tampilkan Riwayat Renewal" diposisikan di atas tabel, sejajar dengan filter existing
- Label toggle: "Tampilkan Riwayat Renewal"
- Toggle default state: OFF (sertifikat renewed tersembunyi secara default)

### Summary Cards (Penghitungan)
- Card Expired hanya menghitung sertifikat expired yang belum di-renew
- Card Akan Expired juga exclude sertifikat yang sudah di-renew (konsisten)
- Angka card TIDAK berubah saat toggle ON — card selalu menampilkan yang belum di-renew saja
- Card Aktif tidak filter renewed — sertifikat Aktif yang sudah di-renew tetap dihitung sebagai Aktif

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SertifikatRow.IsRenewed` field sudah ada — diisi oleh `BuildSertifikatRowsAsync` di CDPController
- `CertificateStatus` enum dengan `Aktif`, `AkanExpired`, `Expired` sudah ada
- Summary card count logic di CDPController `CertificationManagement` action (line ~3060)

### Established Patterns
- Summary cards menggunakan `@Model.ExpiredCount`, `@Model.AkanExpiredCount` di view
- Table partial: `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml`
- Filter menggunakan select dropdown di atas tabel

### Integration Points
- CDPController.BuildSertifikatRowsAsync already populates IsRenewed
- CertificationManagement action computes counts from allRows
- Table partial renders rows — needs IsRenewed-aware rendering

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard filter/toggle pattern using existing IsRenewed field.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>
