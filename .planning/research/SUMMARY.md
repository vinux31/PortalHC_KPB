# Project Research Summary

**Project:** Pre-deployment Audit & Finalization — Portal HC KPB (v9.0)
**Domain:** ASP.NET Core MVC intranet portal, production deployment ke IIS + SQL Server + AD
**Researched:** 2026-03-25
**Confidence:** HIGH

## Executive Summary

Portal HC KPB adalah intranet web app yang dibangun di atas ASP.NET Core 8 MVC dengan 252+ phase sudah shipped. Semua stack yang diperlukan untuk production sudah ada: IIS in-process hosting, SQL Server via EF Core, Active Directory via HybridAuthService, SignalR untuk assessment real-time, ClosedXML + QuestPDF untuk dokumen. Tidak ada package baru yang diperlukan — milestone ini sepenuhnya tentang konfigurasi, audit, dan hardening kode yang sudah ada.

Pendekatan yang direkomendasikan adalah audit berbasis risiko secara bertahap: mulai dari SQL compatibility audit (prerequisite untuk semua langkah lain), kemudian production config, lalu security hardening, dan diakhiri dengan deployment checklist dan runbook. Pekerjaan dibagi dua jenis: (1) codebase audit yang bisa dikerjakan tanpa server production, dan (2) environment verification yang membutuhkan akses ke infrastruktur Pertamina.

Risiko terbesar milestone ini adalah konfigurasi yang salah, bukan bug kode: AD toggle yang lupa diaktifkan akan membuat siapapun bisa login tanpa credential Pertamina, connection string placeholder yang belum diganti akan membuat app crash saat startup, dan SeedProtonData tanpa environment guard bisa contaminate database production dengan data test. Semua risiko ini terdeteksi dari inspeksi kode langsung dan memiliki mitigasi spesifik yang bisa diselesaikan sebelum go-live.

## Key Findings

### Recommended Stack

Stack sudah lengkap dan tidak perlu package baru. Server target (Windows Server + IIS 10+ + SQL Server 2016+) sudah sesuai dengan tech stack yang dipakai. Yang perlu disiapkan di sisi server adalah .NET 8.0 Hosting Bundle (bukan full SDK) agar AspNetCoreModuleV2 tersedia untuk IIS in-process hosting.

**Core technologies (existing, tidak ada perubahan):**
- ASP.NET Core 8 MVC (.NET LTS sampai Nov 2026): web framework — sudah production-ready, 252+ phases shipped
- EF Core 8 + SQL Server: ORM + production DB — connection string template sudah ada, tinggal isi credential via env var
- ASP.NET Core Identity + HybridAuthService: auth/authz dengan AD toggle — sudah built, tinggal aktifkan di production config
- SignalR (built-in): real-time assessment hub — butuh WebSocket enabled di IIS, 401 handling sudah ada
- IIS in-process (AspNetCoreModuleV2): hosting model — web.config perlu dibuat dengan environment variable dan WebSocket

**Perubahan konfigurasi kritis (bukan package baru):**
- `appsettings.Production.json`: tambahkan `UseActiveDirectory: true`, isi connection string real via env var
- `web.config`: tambahkan `ASPNETCORE_ENVIRONMENT=Production`, enable WebSocket
- `Program.cs`: tambahkan HSTS, status code pages, security headers global

### Expected Features

Milestone ini bukan feature development — ini audit dan hardening. "Fitur" yang dimaksud adalah production readiness items.

**Must have (table stakes — blocker sebelum deploy):**
- Seed data cleanup — data UAT di production membingungkan user nyata
- Production appsettings + AD toggle aktif — app tidak jalan atau security bypass tanpa ini
- Security hardening (error pages, HTTPS, cookie security, anti-forgery completeness)
- Authorization completeness audit — satu endpoint terbuka = data breach
- Database migration script + backup strategy — rollback wajib ada sebelum deploy
- IIS deployment runbook — tim infra harus bisa deploy mandiri
- Tech debt closure v4.3 (5 item outstanding dari known gaps)

**Should have (tingkatkan production readiness, bukan blocker):**
- Deployment runbook dokumen lengkap dengan step-by-step
- Response caching headers untuk static assets
- Graceful startup validation (meaningful error log jika DB unreachable)

**Defer ke post-deployment:**
- Health check endpoint — bisa ditambah kapan saja tanpa breaking change
- Rate limiting — intranet, risiko brute force lebih rendah
- Database index review — optimasi setelah ada real usage pattern
- CI/CD pipeline, Docker, APM, load balancer

### Architecture Approach

Arsitektur sudah tepat untuk target deployment (single-server IIS, on-premise). Perubahan utama adalah switch dari development setup (Kestrel + SQLite + local auth) ke production setup (IIS in-process + SQL Server + AD LDAP). Semua komponen untuk switch ini sudah dibangun — tinggal konfigurasi dan verifikasi.

**Major components dan status production-readiness:**
1. **HybridAuthService / LdapAuthService** — Built, perlu `UseActiveDirectory: true` di config dan LDAP path divalidasi dari server production
2. **ApplicationDbContext + EF Core** — Dual-provider (SQLite dev / SQL Server prod) via connection string, perlu SQL compatibility audit untuk raw SQL SQLite-specific
3. **SeedData + SeedProtonData** — SeedData sudah ada IsDevelopment guard; SeedProtonData BELUM ada guard, perlu audit classify reference vs test data
4. **AssessmentHub (SignalR)** — Production-ready, 401 handling sudah ada, butuh WebSocket enabled di IIS
5. **File storage (uploads/)** — Perlu dipastikan di luar publish directory agar tidak hilang saat redeploy

**Dependency order yang benar (dari ARCHITECTURE.md):**
SQL compatibility audit → Production config → Seed data safety → Security & logging → Deployment checklist

### Critical Pitfalls

1. **AD toggle tidak aktif di production** — `appsettings.Production.json` tidak mengandung `Authentication:UseActiveDirectory`. Base config set `false`. Di production, siapapun bisa login tanpa credential Pertamina. Fix: tambahkan section Authentication ke production appsettings.

2. **Connection string placeholder belum diganti** — Template berisi literal `Server=YOUR_SQL_SERVER_NAME`. App crash saat startup. Fix: gunakan environment variable di IIS, JANGAN hardcode credential di git.

3. **SeedProtonData jalan di semua environment** — Tidak ada IsDevelopment guard. Jika berisi test data, akan contaminate production DB. Fix: audit isi, tambahkan guard jika perlu.

4. **Database.Migrate() silent failure** — Catch block di Program.cs line 137-141 log error lalu continue. App bisa berjalan dengan schema lama, error acak saat user hit fitur baru. Fix: jalankan migration manual sebelum deploy ATAU ubah catch jadi fail-fast.

5. **Upload folder tidak persistent setelah redeploy** — File evidence dan guidance hilang jika folder ada di dalam publish directory. Fix: pastikan upload folder di luar publish dir, masukkan ke backup strategy.

## Implications for Roadmap

Berdasarkan dependency order dari ARCHITECTURE.md dan prioritas risiko dari PITFALLS.md, berikut struktur phase yang disarankan:

### Phase 1: SQL Compatibility & Codebase Audit

**Rationale:** Prerequisite untuk semua langkah lain. Harus tahu raw SQL apa yang perlu difix sebelum bisa jalankan migration ke SQL Server. Bisa dikerjakan tanpa akses server production.

**Delivers:** Daftar semua raw SQL (SQLite-specific vs SQL Server compatible), konfirmasi migration files bersih, audit SeedProtonData (classify reference vs test data), closure 5 tech debt items v4.3.

**Addresses:** Table stakes — migration script, seed data cleanup, tech debt closure

**Avoids:** Pitfall 3 (SeedProtonData tanpa guard), Pitfall 5 (silent migration failure), SQLite pragma cleanup

### Phase 2: Production Configuration

**Rationale:** Setelah tahu apa yang perlu difix di codebase, konfigurasi production bisa diselesaikan dengan lengkap dan benar. Phase ini menghasilkan semua file config yang siap deploy.

**Delivers:** `appsettings.Production.json` final (dengan AD toggle, tanpa credential), `web.config` dengan environment variable dan WebSocket, dokumentasi environment variable yang harus diset di IIS server.

**Addresses:** Production appsettings, AD toggle aktif, ASPNETCORE_ENVIRONMENT, AllowedHosts spesifik

**Avoids:** Pitfall 1 (AD off), Pitfall 2 (placeholder creds), Pitfall 3 (wrong environment), Pitfall 12 (AllowedHosts wildcard)

### Phase 3: Security Hardening

**Rationale:** Setelah config benar, lakukan hardening keamanan di level kode. Tidak membutuhkan akses server production — murni perubahan kode.

**Delivers:** HSTS aktif di production block, custom error pages (404/403/500), status code pages, security headers global (X-Frame-Options, X-Content-Type-Options), cookie security audit (Secure/HttpOnly/SameSite), anti-forgery completeness check, authorization audit semua controller/action, admin fallback password policy diperketat, logging level production-appropriate.

**Addresses:** HTTPS enforcement, error handling, cookie security, authorization completeness, file upload validation audit

**Avoids:** Pitfall 6 (weak admin password), Pitfall 9 (HTTPS config missing), Pitfall 11 (verbose logging)

### Phase 4: Deployment Runbook & Checklist

**Rationale:** Phase terakhir sebelum go-live. Mengkonsolidasikan semua temuan dari phase sebelumnya menjadi dokumen operasional. Beberapa langkah membutuhkan akses ke server production (LDAP validation, IIS app pool setup).

**Delivers:** Deployment runbook step-by-step (IIS setup, DB migration, AD config, file permissions), pre-deployment checklist 17 item, rollback procedure, post-deploy verification steps, upload folder persistence strategy dan backup runbook.

**Addresses:** IIS deployment configuration, LDAP path validation, database backup strategy, file storage persistence

**Avoids:** Pitfall 7 (LDAP unvalidated), Pitfall 8 (upload not persistent), Pitfall 10 (session memory cache awareness), Pitfall 13 (HcPortal.db in repo), Pitfall 15 (build artifacts in repo)

### Phase Ordering Rationale

- Phase 1 harus pertama karena SQL audit adalah prerequisite — tidak bisa finalisasi migration script tanpa tahu raw SQL apa yang ada di codebase
- Phase 2 setelah Phase 1 karena production config bisa mencakup hasil temuan audit (misalnya disable auto-migrate jika migration berisiko)
- Phase 3 setelah Phase 2 karena security hardening di Program.cs bergantung pada environment detection yang sudah dikonfigurasi benar
- Phase 4 terakhir karena membutuhkan output dari semua phase sebelumnya untuk deployment runbook yang lengkap dan akurat

### Research Flags

Tidak ada phase yang perlu `/gsd:research-phase` — semua research sudah selesai dengan confidence HIGH. Semua pattern sudah well-documented dari inspeksi kode langsung dan official ASP.NET Core docs.

Phases dengan standard patterns (skip research-phase):
- **Phase 1:** SQL Server migration patterns established, analisis dari kode langsung
- **Phase 2:** IIS web.config dan appsettings patterns documented, template sudah ada di STACK.md dan ARCHITECTURE.md
- **Phase 3:** ASP.NET Core security middleware patterns standard dan well-documented
- **Phase 4:** Deployment runbook content sudah di-derive dari pitfall analysis

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Analisis langsung dari source code: Program.cs, csproj, appsettings. Semua fakta dikonfirmasi dari file aktual |
| Features | HIGH | Derived dari codebase audit + ASP.NET Core official deployment docs + v4.3 known gaps registry |
| Architecture | HIGH | Semua komponen ditemukan di code dengan line numbers (HybridAuthService, LdapAuthService, SeedData, Program.cs) |
| Pitfalls | HIGH | Setiap pitfall dikonfirmasi dengan line number di source code aktual — bukan asumsi |

**Overall confidence:** HIGH

### Gaps to Address

- **LDAP path validation**: Tidak bisa dikonfirmasi tanpa akses ke AD server Pertamina. Path `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` mungkin benar atau placeholder. Harus diverifikasi saat Phase 4 dengan test login dari server production.
- **SQL Server version aktual**: EF Core 8 kompatibel dengan SQL Server 2016+, fitur tertentu butuh 2019+. Konfirmasi versi SQL Server di environment Pertamina saat Phase 4.
- **IIS SSL certificate**: HTTPS binding di luar scope codebase — tanggung jawab tim infra Pertamina. Dokumentasikan requirement di runbook Phase 4.
- **Upload folder path aktual**: Belum dikonfirmasi apakah `/uploads/evidence/` dan `/uploads/guidance/` sudah di luar publish directory. Perlu verifikasi saat Phase 1.

## Sources

### Primary (HIGH confidence)
- Source code langsung: `Program.cs`, `HcPortal.csproj`, `appsettings.*.json`, `Services/HybridAuthService.cs`, `Services/LdapAuthService.cs`, `Data/SeedData.cs`
- ASP.NET Core 8 IIS in-process hosting documentation
- EF Core SQL Server provider patterns

### Secondary (MEDIUM confidence)
- IIS-specific behavior (app pool recycling, WebSocket) — training data knowledge, konfirmasi saat deployment
- AD LDAP path format — berdasarkan pattern di codebase, perlu validasi saat Phase 4

---
*Research completed: 2026-03-25*
*Ready for roadmap: yes*
