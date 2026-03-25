# Stack Research

**Domain:** Pre-deployment readiness — ASP.NET Core MVC intranet portal
**Researched:** 2026-03-25
**Confidence:** HIGH (analisis langsung source code + established deployment patterns)

## Recommended Stack

### Core Technologies (Sudah Ada - Tidak Perlu Diubah)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| ASP.NET Core MVC | net8.0 | Web framework | LTS sampai Nov 2026, 252+ phases shipped |
| EF Core + SqlServer | 8.0.0 | ORM + production DB | Connection string template sudah ada |
| ASP.NET Core Identity | 8.0.0 | Auth/authz | Role-based auth lengkap (Admin/HC/SrSpv/SectionHead/Coach/Coachee) |
| System.DirectoryServices | 10.0.0 | LDAP/AD authentication | HybridAuthService sudah built dengan toggle config |
| SignalR | built-in 8.0 | Real-time assessment hub | AssessmentHub production-ready |
| ClosedXML | 0.105.0 | Excel import/export | Fitur inti portal |
| QuestPDF | 2026.2.2 | PDF generation | Community license, sudah aktif |
| IIS in-process | AspNetCoreModuleV2 | Hosting | web.config sudah ada |

### Supporting Libraries

Tidak perlu package baru. Semua kebutuhan deployment dapat dipenuhi dengan konfigurasi stack yang ada.

### Server Requirements (Install di Server Target)

| Tool | Purpose | Notes |
|------|---------|-------|
| .NET 8.0 Hosting Bundle | Runtime + IIS module | Download dari dotnet.microsoft.com/download/dotnet/8.0 — BUKAN SDK, cukup Hosting Bundle |
| IIS 10+ | Web server | Windows Server 2016+ dengan AspNetCoreModuleV2 (otomatis dari Hosting Bundle) |
| SQL Server 2016+ | Production database | Sudah tersedia di environment Pertamina |

## Configuration Changes for Production

### 1. appsettings.Production.json — Perlu Diisi

File sudah ada dengan template placeholder. Yang harus dikonfigurasi:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "HcPortal": "Information"
    }
  },
  "AllowedHosts": "portalhc.pertamina.com",
  "ConnectionStrings": {
    "DefaultConnection": "Server=NAMA_SERVER;Database=HcPortalDB;User Id=hcportal_app;Password=GANTI_VIA_ENV_VAR;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30"
  },
  "Authentication": {
    "UseActiveDirectory": true,
    "LdapPath": "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com"
  }
}
```

**Perubahan dari template saat ini:**
- `AllowedHosts`: ganti `*` ke hostname aktual
- `Encrypt=True`: tambahkan untuk keamanan koneksi DB
- `Connect Timeout=30`: tambahkan, default 15s terlalu pendek untuk cold start
- `Logging` default: turunkan ke Warning, hanya HcPortal namespace yang Information
- `Authentication.UseActiveDirectory`: set `true`

### 2. web.config — Perlu Tuning

Current state di publish output:
```xml
<aspNetCore processPath="dotnet" arguments=".\HcPortal.dll"
            stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

**Yang perlu diubah:**

| Setting | Current | Production | Why |
|---------|---------|------------|-----|
| `stdoutLogEnabled` | `false` | `true` saat deploy pertama | Troubleshoot startup failures, matikan setelah stabil |
| Environment variable | tidak ada | `ASPNETCORE_ENVIRONMENT=Production` | Wajib agar error page aktif, dev middleware mati |

Tambahkan `<environmentVariables>` block:
```xml
<aspNetCore processPath="dotnet" arguments=".\HcPortal.dll"
            stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

### 3. Program.cs — Production Hardening

Yang perlu ditambahkan (kode, bukan package):

| Item | Status | Action |
|------|--------|--------|
| HSTS | Belum ada | Tambahkan `app.UseHsts()` di production block |
| Status code pages | Belum ada | Tambahkan `app.UseStatusCodePagesWithReExecute("/Home/Error/{0}")` |
| Security headers | Parsial (PDF saja) | Middleware global untuk X-Frame-Options, X-Content-Type-Options |
| Password policy | Dev-mode (RequireDigit=false dll) | Perketat untuk production |

### 4. IIS App Pool Configuration

| Setting | Value | Why |
|---------|-------|-----|
| .NET CLR Version | "No Managed Code" | Wajib untuk in-process hosting |
| Start Mode | AlwaysRunning | Hindari cold start delay |
| Idle Timeout | 0 (disabled) | Jangan shutdown app saat idle |
| Recycling | Regular time (default 1740 min) | OK, in-process akan graceful restart |

### 5. Active Directory Verification

Sudah built (`HybridAuthService`), yang perlu diverifikasi saat deploy:
- LDAP path cocok dengan AD structure aktual
- IIS server bisa akses domain controller (port 389 LDAP / 636 LDAPS)
- App pool identity punya read permission ke AD OU
- Fallback ke local auth untuk `admin@pertamina.com` tetap berfungsi

### 6. Database Preparation

- Buat database `HcPortalDB` di SQL Server target
- Buat SQL login `hcportal_app` dengan role `db_owner` (untuk auto-migration)
- `context.Database.Migrate()` di Program.cs akan apply semua migration saat startup pertama
- Setelah stabil, bisa turunkan ke `db_datareader` + `db_datawriter`

## Deployment Command

```bash
# Build
dotnet publish -c Release -o ./publish

# Buat folder logs manual (IIS tidak auto-create)
mkdir ./publish/logs

# Copy publish/ ke IIS site physical path
# First request triggers: EF migration → seed data → app ready
```

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Serilog/NLog | Overkill untuk intranet tanpa log aggregation | Built-in logging + stdout file |
| Health checks package | Tidak ada orchestrator/LB yang poll | Manual monitoring + Event Viewer |
| Application Insights | Bukan Azure deployment | IIS logs + stdout |
| Docker | Target IIS on-premises | Direct IIS publish |
| Swagger/OpenAPI | MVC portal, bukan API | Tidak relevan |
| Hangfire | Tidak ada background job requirement | Tidak perlu |
| Rate limiting middleware | Intranet + throttling sudah ada di v8.6 | Existing implementation |
| Kestrel standalone | Intranet butuh Windows Auth passthrough | IIS in-process |
| Redis | Volume data kecil, single server | In-memory cache yang sudah ada |
| Reverse proxy (nginx) | Windows Server + IIS environment | IIS native |

## Environment Variable untuk Sensitive Config

Lebih aman daripada menyimpan di file:

```
# Set via IIS > Site > Configuration Editor > environmentVariables
ConnectionStrings__DefaultConnection=Server=...;Database=...;Password=...
Authentication__UseActiveDirectory=true
```

## SQLite Cleanup

`Program.cs` baris 129-135 ada SQLite WAL mode pragma. Kode ini harmless di production (conditional check `ProviderName == "Sqlite"`), tapi sebaiknya dihapus untuk kebersihan karena production pakai SQL Server.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| IIS in-process | Kestrel + reverse proxy | Jika deploy ke Linux, tapi tidak relevan di sini |
| Built-in logging | Serilog + Seq | Jika nanti perlu centralized log aggregation |
| SQL Server auth | Windows Integrated Auth ke DB | Jika app pool identity bisa diberi DB access langsung |
| Environment variables | Azure Key Vault | Jika migrate ke cloud |

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| EF Core 8.0 | SQL Server 2016+ | Fitur terbaru butuh 2019+ tapi 2016 cukup |
| .NET 8.0 Hosting Bundle | IIS 10 (Windows Server 2016+) | In-process hosting butuh AspNetCoreModuleV2 |
| System.DirectoryServices 10.0.0 | .NET 8.0, Windows only | Sesuai target deployment |
| QuestPDF 2026.2.2 | Community license gratis < $1M revenue | Internal tool = aman |
| .NET 8.0 LTS | Support sampai November 2026 | Masih dalam support window |

## Sources

- Source code: `Program.cs`, `HcPortal.csproj`, `appsettings.*.json`, `publish/web.config` (HIGH confidence)
- ASP.NET Core 8.0 IIS hosting model documentation (HIGH confidence)
- EF Core SQL Server provider patterns (HIGH confidence)

---
*Stack research untuk: Pre-deployment Audit & Finalization (v9.0)*
*Researched: 2026-03-25*
