# Feature Landscape: Pre-deployment Audit & Finalization

**Domain:** Pre-deployment readiness untuk ASP.NET Core MVC portal (intranet)
**Researched:** 2026-03-25
**Context:** Portal HC sudah 252+ phase shipped, v8.6 sudah hardening (XSS, null safety, validation). Fokus pada gap yang belum dicakup sebelum production.

---

## Table Stakes

Fitur/check yang WAJIB ada sebelum production. Tanpa ini = risiko downtime, data loss, atau security incident.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Seed data cleanup | Data UAT (user dummy, soal contoh) masuk production = kebingungan user nyata | Low | Review `DbInitializer`/seed, conditional on `IsDevelopment()` |
| Production appsettings | Connection string, AD config, logging level salah = app tidak jalan | Low | `appsettings.Production.json` terpisah dari Development |
| Error handling & custom error pages | Stack trace terexpose = security risk + UX buruk | Low | `UseExceptionHandler`, custom 404/403/500 pages |
| Logging level production | Debug logging di production = disk penuh, performance hit | Low | `Warning` untuk Microsoft.*, `Information` untuk app |
| HTTPS enforcement | HTTP di intranet pun = credentials terexpose | Low | `UseHttpsRedirection()`, HSTS header |
| Connection string security | Plain text sa/password di config = credential leak | Low | Windows Integrated Auth atau env variable |
| Anti-forgery completeness | CSRF pada form POST tanpa token | Low | v8.6 sudah partial — verify ALL POST actions |
| Session/cookie security | Cookie tanpa Secure/HttpOnly = session hijack | Low | `CookieSecurePolicy.Always`, `HttpOnly = true`, `SameSite` |
| Authorization completeness audit | Missing `[Authorize]` pada satu action = data breach | Med | Scan semua controller/action, verify role requirements |
| Database migration script | Schema mismatch di production = crash on startup | Med | SQL script dari dev schema, tested against clean DB |
| Database backup strategy | Deploy gagal tanpa rollback = data loss | Low | Pre-deploy backup script + restore runbook |
| IIS deployment configuration | Salah config app pool/web.config = 502 error | Med | Publish profile, app pool .NET Core, `web.config` transform |
| File upload validation | Malicious upload = potential RCE | Med | Whitelist extensions, size limits, content-type check — audit existing |
| Remove debug/dev middleware | Swagger, Developer Exception Page di production = info leak | Low | Guard with `IsDevelopment()` checks |
| Pending tech debt closure | Bare catch at AdminController:1072, null-forgiving ops | Low | 5 items dari v4.3 known gaps |

## Differentiators

Meningkatkan production readiness tapi bukan hard blocker.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Health check endpoint | `/health` untuk monitoring/alerting infra | Low | `MapHealthChecks` built-in ASP.NET Core |
| Deployment runbook document | Tim infra bisa deploy tanpa developer | Low | Step-by-step: IIS setup, DB restore, AD config, verify |
| Environment-specific feature flags | Disable UAT-only features/seed di production | Low | `IWebHostEnvironment.IsProduction()` guards |
| Rate limiting pada login | Brute force prevention | Low | Simple IP-based throttle atau account lockout |
| Response caching headers | Static assets tanpa cache = slow page loads | Low | Cache-Control headers untuk CSS/JS/images |
| Database index review | Query lambat di production dengan data banyak | Med | Review query plan untuk endpoint yang sering diakses |
| Graceful startup validation | App crash diam-diam jika DB unreachable | Low | Startup health check, meaningful error log |

## Anti-Features

Fitur yang JANGAN dibangun di milestone ini.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Full CI/CD pipeline | Scope creep — ini finalisasi, bukan DevOps setup | Manual deployment runbook cukup untuk v1 |
| Automated test suite | Bagus tapi bukan blocker; user sudah UAT manual 252+ phases | Defer ke post-deployment |
| EF Core Migrations framework | Terlalu berisiko refactor migration menjelang deploy | Manual SQL script lebih aman dan predictable |
| Docker/containerization | Target = IIS on-premise, Docker tidak relevan | IIS publish profile |
| APM integration (App Insights) | Nice-to-have, bisa tambah post-deploy | File logging cukup untuk awal |
| SSL certificate provisioning | Tanggung jawab infra/network team | Dokumentasikan requirement saja |
| Load balancer setup | Single server untuk awal | Defer sampai ada kebutuhan scale |
| Comprehensive penetration test | Membutuhkan external security team | Self-audit checklist cukup untuk intranet app |

## Feature Dependencies

```
appsettings.Production.json --> IIS deployment config (IIS butuh config benar)
Seed data cleanup --> Database migration script (migration harus exclude seed UAT)
Authorization audit --> Custom error pages (unauthorized harus redirect proper)
Health check endpoint --> IIS deployment (health check perlu accessible)
Tech debt closure --> Final integration check (pastikan fix tidak break fitur)
Remove debug middleware --> appsettings.Production.json (environment detection)
```

## MVP Recommendation

Prioritaskan (harus selesai sebelum deploy):

1. **Seed data cleanup** — Risiko tertinggi: data dummy di production membingungkan user nyata
2. **Production appsettings + connection string** — App tidak jalan tanpa config benar
3. **Security hardening** — Error pages, HTTPS, cookie security, anti-forgery completeness
4. **Authorization completeness audit** — Satu endpoint terbuka = data breach
5. **Database migration script + backup strategy** — Rollback wajib ada
6. **IIS deployment runbook** — Tim infra harus bisa deploy mandiri
7. **Tech debt closure (v4.3)** — Bersihkan sebelum production freeze

Defer ke post-deployment:
- **Health check endpoint**: Bisa ditambah kapan saja tanpa breaking change
- **Rate limiting**: Intranet app, risiko brute force lebih rendah
- **Database index review**: Optimasi setelah ada real usage pattern

## Sources

- ASP.NET Core deployment best practices (official docs) — HIGH confidence
- v8.6 codebase audit scope (PROJECT.md) — existing hardening coverage
- v4.3 known gaps (MEMORY.md) — outstanding tech debt items
- Project v9.0 target features (PROJECT.md) — alignment dengan user goals
