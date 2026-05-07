# Project Instructions

Always respond in Bahasa Indonesia.

## Develop Workflow (Wajib Dibaca)

Status website: **Lokal → Development (10.55.3.3) → Production**.

Aturan ringkas saat fix bug / development:

1. Cek bug via URL Dev (`http://10.55.3.3/KPB-PortalHC`)
2. Reproduce & fix di lokal
3. Verifikasi lokal: `dotnet build` + `dotnet run` (cek di `http://localhost:5277`) + cek DB lokal (+ Playwright bila ada)
4. Commit & push (sertakan file migration kalau ada)
5. Promosi ke server Dev & DB Dev = **tanggung jawab Team IT**, bukan developer. Notifikasi IT dengan commit hash + flag migration.

❌ Jangan edit kode/DB langsung di server Dev/Prod. ❌ Jangan push tanpa verifikasi lokal.

Detail lengkap (environment map, SOP migration, checklist): lihat [`docs/DEV_WORKFLOW.md`](docs/DEV_WORKFLOW.md).
