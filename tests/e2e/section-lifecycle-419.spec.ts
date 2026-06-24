// Phase 419 D-04.1 (skeleton — Plan 05 mengisi) — Lifecycle Section inti UAT real-browser @5277.
// Analog: tests/e2e/scoped-shuffle.spec.ts (serial + db.backup/restore + wizard) +
//         tests/e2e/export-per-peserta.spec.ts (download-assert label Section di Excel/PDF).
// Helpers Plan 05: import * as db from '../helpers/dbSnapshot'; import { login } from '../helpers/auth';
//                  import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
//
// Langkah yang akan diisi Plan 05 (lesson 354 — WAJIB real-browser):
//   1. db.backup() di beforeAll; db.restore() di afterAll (SEED_WORKFLOW snapshot->restore).
//   2. login admin@pertamina.com; createAssessmentViaWizard; buat package + Section (1,2) via panel kelola.
//   3. assign soal ke Section; soal dgn 5-6 opsi (Phase 418); ambil ujian sbg worker.
//   4. ASSERT: render huruf A-F dinamis di StartExam; header Section + pagination (Phase 417); resume.
//   5. ASSERT: export per-soal (Excel band-header "Section {n}: {Nama}" + PDF heading) — PAG-04.
import { test } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Phase 419 D-04.1 — Lifecycle Section inti (Section + shuffle + pagination + opsi 2-6)', () => {
  test.fixme('create -> assign Section -> ujian render A-F -> resume -> export label Section', async () => {
    // TODO Plan 05 — lihat header.
  });
});
