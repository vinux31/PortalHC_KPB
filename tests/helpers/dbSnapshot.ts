// Phase 315 — Generic sqlcmd subprocess wrapper untuk SQL Server Express lokal.
// REJECT non-localhost target (compliance CLAUDE.md § Develop Workflow — local-only test infra).
// Source-of-truth: docs/SEED_WORKFLOW.md § 5.1 (BACKUP) + § 5.2 (RESTORE).
// Connection string mirrored from appsettings.Development.json (Server=localhost\SQLEXPRESS;
// Database=HcPortalDB_Dev; Integrated Security=True; TrustServerCertificate=True).
// Pattern dasar: RESEARCH.md § Pattern 2 (lines 415-477).
//
// CRITICAL: -b flag wajib supaya T-SQL error / RAISERROR / THROW return exit code non-zero.
// Tanpa -b, syntax error di seed silent-fail (return 0) dan caller mengira sukses.

import { spawn } from 'child_process';

/**
 * Base args untuk semua sqlcmd invocations.
 * - `-S localhost\SQLEXPRESS`  : connect ke instance lokal (hostname guard di runSqlcmd validate).
 * - `-d HcPortalDB_Dev`        : default database; restore() strip flag ini karena USE master required.
 * - `-E`                       : Windows Integrated Security (no user/password literal).
 * - `-C`                       : TrustServerCertificate (matches appsettings).
 * - `-I`                       : QUOTED_IDENTIFIER ON (required oleh sebagian T-SQL).
 * - `-b`                       : exit non-zero on T-SQL error (CRITICAL — Phase 313.1 seed assumes this).
 */
const SQLCMD_BASE_ARGS: string[] = [
  '-S', 'localhost\\SQLEXPRESS',
  '-d', 'HcPortalDB_Dev',
  '-E',
  '-C',
  '-I',
  '-b',
];

/**
 * Internal helper: spawn sqlcmd subprocess dengan localhost-only guard.
 * Throw `Refusing to target non-localhost SQL Server: <host>` kalau ada `-S` arg dengan
 * hostname non-localhost (T-315-01 mitigation per PLAN threat model).
 */
function runSqlcmd(args: string[]): Promise<{ stdout: string; stderr: string }> {
  return new Promise((resolve, reject) => {
    // Safety guard: REJECT non-localhost target (CLAUDE.md compliance, T-315-01 mitigation).
    const sIdx = args.indexOf('-S');
    if (sIdx >= 0 && sIdx + 1 < args.length && !/^localhost/i.test(args[sIdx + 1])) {
      return reject(
        new Error(`Refusing to target non-localhost SQL Server: ${args[sIdx + 1]}`)
      );
    }

    const proc = spawn('sqlcmd', args, { windowsHide: true });
    let stdout = '';
    let stderr = '';
    proc.stdout.on('data', (d) => { stdout += d.toString(); });
    proc.stderr.on('data', (d) => { stderr += d.toString(); });
    proc.on('error', reject);
    proc.on('close', (code) => {
      if (code !== 0) {
        reject(new Error(`sqlcmd exit ${code}: ${stderr || stdout}`));
      } else {
        resolve({ stdout, stderr });
      }
    });
  });
}

/**
 * BACKUP DATABASE HcPortalDB_Dev ke `snapshotPath` (.bak).
 * WITH INIT, FORMAT — overwrite snapshot lama, fresh media set.
 * Caller bertanggung jawab pilih path (e.g. C:/Temp/...bak) di disk lokal yang ada.
 */
export async function backup(snapshotPath: string): Promise<void> {
  const tsql = `BACKUP DATABASE HcPortalDB_Dev TO DISK='${snapshotPath}' WITH INIT, FORMAT;`;
  await runSqlcmd([...SQLCMD_BASE_ARGS, '-Q', tsql]);
}

/**
 * RESTORE DATABASE HcPortalDB_Dev dari `snapshotPath`.
 * Wajib SINGLE_USER + WITH ROLLBACK IMMEDIATE supaya kill koneksi aktif (Kestrel, dotnet test, dst.)
 * sebelum restore. Setelah restore selesai → kembali MULTI_USER.
 *
 * CATATAN: -d HcPortalDB_Dev di-drop dari args karena USE master required (locked database
 * tidak bisa dipakai sebagai default database saat operasi RESTORE).
 */
export async function restore(snapshotPath: string): Promise<void> {
  const tsql = `
    USE master;
    ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    RESTORE DATABASE HcPortalDB_Dev FROM DISK='${snapshotPath}' WITH REPLACE;
    ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;
  `.trim();

  // Strip `-d` flag dan value-nya (USE master required oleh RESTORE).
  const args: string[] = [];
  for (let i = 0; i < SQLCMD_BASE_ARGS.length; i++) {
    if (SQLCMD_BASE_ARGS[i] === '-d') {
      i++; // skip value juga
      continue;
    }
    args.push(SQLCMD_BASE_ARGS[i]);
  }

  await runSqlcmd([...args, '-Q', tsql]);
}

/**
 * Execute SQL script file (`-i <path>`). Pakai untuk seed SQL multi-statement.
 * sqlcmd parser handle GO batch separator natively.
 */
export async function execScript(sqlPath: string): Promise<void> {
  await runSqlcmd([...SQLCMD_BASE_ARGS, '-i', sqlPath]);
}

/**
 * Run single SELECT scalar query, return integer hasil first numeric line.
 * Pakai untuk Layer 1 / Layer 4 validation COUNT(*) post-seed dan post-restore.
 * `-h -1` suppress header; `-W` strip trailing whitespace.
 *
 * Throw kalau output tidak punya numeric line (e.g. SQL syntax error before SELECT).
 */
export async function queryScalar(sql: string): Promise<number> {
  const { stdout } = await runSqlcmd([
    ...SQLCMD_BASE_ARGS,
    '-Q', `SET NOCOUNT ON; ${sql}`,
    '-h', '-1',
    '-W',
  ]);
  const match = stdout.trim().match(/^-?\d+/m);
  if (!match) {
    throw new Error(`queryScalar: no numeric output from "${sql}"\nStdout: ${stdout}`);
  }
  return parseInt(match[0], 10);
}

/**
 * Run single SELECT string-scalar query, return first non-empty trimmed line.
 * Pakai untuk resolve `SERVERPROPERTY('InstanceDefaultBackupPath')` saat setup BACKUP
 * — `C:\\Temp\\` blocked oleh SQL Server service account, kita harus pakai default
 * backup directory yang SQL Server sudah owned (e.g. `C:\\Program Files\\Microsoft SQL
 * Server\\MSSQL17.SQLEXPRESS\\MSSQL\\Backup\\`).
 *
 * Throw kalau output kosong (e.g. SERVERPROPERTY returns NULL atau syntax error).
 */
export async function queryString(sql: string): Promise<string> {
  const { stdout } = await runSqlcmd([
    ...SQLCMD_BASE_ARGS,
    '-Q', `SET NOCOUNT ON; ${sql}`,
    '-h', '-1',
    '-W',
  ]);
  const trimmed = stdout.trim();
  if (!trimmed) {
    throw new Error(`queryString: empty output dari "${sql}"`);
  }
  // Ambil baris non-empty pertama (sqlcmd kadang append baris-baris kosong).
  const firstLine = trimmed.split(/\r?\n/).map((l) => l.trim()).find((l) => l.length > 0);
  if (!firstLine) {
    throw new Error(`queryString: tidak ada baris non-empty dari "${sql}"\nStdout: ${stdout}`);
  }
  return firstLine;
}
