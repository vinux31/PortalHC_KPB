// Record track-progresi.html to WebM via Playwright
// Usage: node docs/assets/proton-video/record-track-progresi.mjs
//
// Output: docs/assets/proton-video/track-progresi.webm (36s, 1920x1080)
// Convert to MP4: ffmpeg -i track-progresi.webm -c:v libx264 -crf 18 -preset slow track-progresi.mp4

import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import http from 'node:http';

const here = path.dirname(fileURLToPath(import.meta.url));
const PORT = 8767;
const URL = `http://127.0.0.1:${PORT}/track-progresi.html`;
const OUT_DIR = here;
const RECORD_DURATION_MS = 36_000; // 35s animation + 1s buffer

// 1. Start http server
const server = spawn(
  process.platform === 'win32' ? 'python.exe' : 'python',
  ['-m', 'http.server', String(PORT), '--bind', '127.0.0.1'],
  { cwd: here, stdio: 'ignore' }
);

async function waitForServer(url, timeout = 5000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    try {
      await new Promise((resolve, reject) => {
        const req = http.get(url, res => { res.resume(); resolve(); });
        req.on('error', reject);
        req.setTimeout(500, () => { req.destroy(); reject(new Error('timeout')); });
      });
      return;
    } catch {
      await new Promise(r => setTimeout(r, 200));
    }
  }
  throw new Error('Server did not start in time');
}

try {
  await waitForServer(URL);
  console.log('[record] Server ready, launching browser...');

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    recordVideo: {
      dir: OUT_DIR,
      size: { width: 1920, height: 1080 }
    }
  });

  const page = await context.newPage();
  await page.goto(URL, { waitUntil: 'domcontentloaded' });

  console.log(`[record] Recording ${RECORD_DURATION_MS / 1000}s animation...`);
  await page.waitForTimeout(RECORD_DURATION_MS);

  const videoPath = await page.video()?.path();
  await context.close();
  await browser.close();

  console.log('[record] Video saved (raw):', videoPath);

  const fs = await import('node:fs/promises');
  const finalPath = path.join(OUT_DIR, 'track-progresi.webm');
  await fs.rename(videoPath, finalPath);
  console.log('[record] Final path:', finalPath);
} catch (err) {
  console.error('[record] ERROR:', err);
  process.exitCode = 1;
} finally {
  server.kill('SIGTERM');
}
