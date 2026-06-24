using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;  // Phase 382 STAT-01 — guard pakai konstanta (v22.0 discipline)

namespace HcPortal.Services
{
    /// <summary>
    /// Centralized grading service for package-based assessments.
    /// Extracted from AssessmentAdminController.GradeFromSavedAnswers() and CMPController.SubmitExam()
    /// to eliminate duplication (Phase 296 D-01, D-02).
    ///
    /// SCOPE: Package-based assessments only. Does NOT handle Proton Tahun 3 interview (D-03).
    /// Does NOT handle SignalR push or cache invalidation — those remain in controllers.
    /// </summary>
    public class GradingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkerDataService _workerDataService;
        private readonly ILogger<GradingService> _logger;
        private readonly ProtonCompletionService _protonCompletionService;
        private readonly ProtonBypassService _protonBypassService;

        public GradingService(
            ApplicationDbContext context,
            IWorkerDataService workerDataService,
            ILogger<GradingService> logger,
            ProtonCompletionService protonCompletionService,
            ProtonBypassService protonBypassService)
        {
            _context = context;
            _workerDataService = workerDataService;
            _logger = logger;
            _protonCompletionService = protonCompletionService;
            // Satu arah grading → bypass (Open Q3): ProtonBypassService TIDAK inject GradingService.
            _protonBypassService = protonBypassService;
        }

        /// <summary>
        /// Grade a completed package-based assessment session and persist all results.
        ///
        /// Handles (per D-02):
        /// 1. Hitung skor dari PackageUserResponses yang sudah tersimpan di DB
        /// 2. Hitung SessionElemenTeknisScores per group elemen teknis
        /// 3. Update session (race-condition-safe via ExecuteUpdateAsync + status guard)
        /// 4. Update PackageAssignment.IsCompleted
        /// 5. Buat TrainingRecord (dengan duplicate guard)
        /// 6. Generate NomorSertifikat via CertIssuanceRules.ShouldIssueCertificate gate (Phase 423 CERT-01) + TryAssignNextSeqAsync
        /// 7. Kirim notifikasi grup completion
        ///
        /// PENTING: Method ini selalu grade dari DB (bukan dari form POST) — per RESEARCH.md anti-pattern.
        /// PENTING: Method ini tidak memanggil SignalR push atau _cache.Remove — biarkan di controller.
        /// </summary>
        /// <param name="session">AssessmentSession yang akan di-grade. Harus sudah ada di DB.</param>
        /// <returns>True jika session berhasil di-grade. False jika race condition terjadi (session sudah Completed).</returns>
        public async Task<bool> GradeAndCompleteAsync(AssessmentSession session)
        {
            // ---- 1. Load PackageAssignment dan hitung skor ----
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);

            if (packageAssignment == null)
            {
                _logger.LogWarning(
                    "GradingService: session {SessionId} tidak punya PackageAssignment — tidak bisa di-grade.",
                    session.Id);
                return false;
            }

            var shuffledIds = packageAssignment.GetShuffledQuestionIds();

            var packageQuestions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => shuffledIds.Contains(q.Id))
                .ToListAsync();
            var questionLookup = packageQuestions.ToDictionary(q => q.Id);

            var allResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == session.Id)
                .ToListAsync();

            // SAVE-01 (D-01): final answer per soal (last-write-wins by SubmittedAt) — in-memory pada list
            // yang sudah ToListAsync (aman, A1). Tanpa ini FirstOrDefault tanpa ORDER BY bisa ambil baris
            // BASI saat ada response duplikat (race multi-tab) → Score salah.
            // MC/single-answer only — MultipleAnswer dibaca penuh (multi-row), JANGAN masuk dedupe ini.
            var finalByQuestion = allResponses
                .Where(r => r.PackageOptionId.HasValue)
                .GroupBy(r => r.PackageQuestionId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());

            int totalScore = 0;
            int maxScore = 0;

            foreach (var qId in shuffledIds)
            {
                if (!questionLookup.TryGetValue(qId, out var q)) continue;

                maxScore += q.ScoreValue;

                // Switch-case per QuestionType (D-08): hanya MultipleChoice diimplementasi di Phase 296.
                // MA + Essay diimplementasi di Phase 298.
                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice":
                        // SAVE-01 (D-01): baca jawaban FINAL per soal (dedupe by SubmittedAt) — bukan baris arbitrer.
                        var mcResponse = finalByQuestion.TryGetValue(q.Id, out var fr) ? fr : null;
                        if (mcResponse != null)
                        {
                            var selectedOption = q.Options.FirstOrDefault(o => o.Id == mcResponse.PackageOptionId!.Value);
                            if (selectedOption != null && selectedOption.IsCorrect)
                                totalScore += q.ScoreValue;
                        }
                        break;

                    case "MultipleAnswer":
                        var selectedOptionIds = allResponses
                            .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                            .Select(r => r.PackageOptionId!.Value)
                            .ToHashSet();
                        var correctOptionIds = q.Options
                            .Where(o => o.IsCorrect)
                            .Select(o => o.Id)
                            .ToHashSet();
                        // All-or-nothing: harus pilih semua correct dan tidak ada incorrect
                        if (selectedOptionIds.SetEquals(correctOptionIds))
                            totalScore += q.ScoreValue;
                        break;

                    case "Essay":
                        // Essay: skor 0 sementara — akan di-grade manual oleh HC
                        // EssayScore di PackageUserResponse belum diisi di tahap ini
                        // maxScore tetap include q.ScoreValue (denominator total)
                        break;

                    default:
                        _logger.LogWarning(
                            "GradingService: QuestionType tidak dikenal '{QuestionType}' untuk question {QuestionId} — dilewati.",
                            q.QuestionType, q.Id);
                        break;
                }
            }

            int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
            bool isPassed = finalPercentage >= session.PassPercentage;
            bool hasEssay = packageQuestions.Any(q => (q.QuestionType ?? "MultipleChoice") == "Essay");

            // ---- 2. Hitung SessionElemenTeknisScores ----
            var etGroups = packageQuestions
                .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

            foreach (var etGroup in etGroups)
            {
                int etCorrect = 0;
                int etTotal = etGroup.Count();
                foreach (var q in etGroup)
                {
                    switch (q.QuestionType ?? "MultipleChoice")
                    {
                        case "MultipleChoice":
                            // SAVE-01 (D-01): ET MC scoring juga baca jawaban FINAL per soal (dedupe).
                            var etMcResponse = finalByQuestion.TryGetValue(q.Id, out var efr) ? efr : null;
                            if (etMcResponse != null)
                            {
                                var sel = q.Options.FirstOrDefault(o => o.Id == etMcResponse.PackageOptionId!.Value);
                                if (sel != null && sel.IsCorrect) etCorrect++;
                            }
                            break;

                        case "MultipleAnswer":
                            var maSelected = allResponses
                                .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                                .Select(r => r.PackageOptionId!.Value)
                                .ToHashSet();
                            var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                            if (maSelected.SetEquals(maCorrect)) etCorrect++;
                            break;

                        case "Essay":
                            // Essay: skip ET scoring (manual grading)
                            break;
                    }
                }
                _context.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore
                {
                    AssessmentSessionId = session.Id,
                    ElemenTeknis = etGroup.Key,
                    CorrectCount = etCorrect,
                    QuestionCount = etTotal
                });
            }

            // ---- 3. Update session (race-condition-safe) ----
            // SaveChanges dulu untuk ET scores (per CMPController pattern — answer persistence sebelum status claim)
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race: AkhiriUjian sudah insert ET scores untuk session ini — aman diabaikan.
                // Kedua pihak menghasilkan skor yang sama dari jawaban yang sama.
                _context.ChangeTracker.Clear();
            }

            // ---- 3a. Essay flow: status "Menunggu Penilaian", tidak generate sertifikat/TrainingRecord ----
            if (hasEssay)
            {
                // Interim score = hanya dari MC + MA (Essay skor 0)
                int interimPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                // STAT-01 (D-02): selain Completed/PendingGrading (yang sudah dijaga), TOLAK juga commit
                // PendingGrading pada sesi terminal Abandoned/Cancelled (tak boleh di-resurrect).
                var essayRowsAffected = await _context.AssessmentSessions
                    .Where(s => s.Id == session.Id
                        && s.Status != S.Completed && s.Status != S.PendingGrading
                        && s.Status != S.Abandoned && s.Status != S.Cancelled)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Score, interimPercentage)
                        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.PendingGrading)
                        .SetProperty(r => r.HasManualGrading, true)
                        .SetProperty(r => r.IsPassed, (bool?)null)
                        .SetProperty(r => r.Progress, 100)
                        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
                    );

                if (essayRowsAffected == 0)
                {
                    _logger.LogWarning(
                        "GradingService: race condition session {SessionId} — sudah Completed/Menunggu Penilaian.",
                        session.Id);
                    return false;
                }

                // Update PackageAssignment.IsCompleted (ujian selesai, hanya grading pending)
                await _context.UserPackageAssignments
                    .Where(a => a.AssessmentSessionId == session.Id)
                    .ExecuteUpdateAsync(a => a.SetProperty(r => r.IsCompleted, true));

                // TIDAK generate TrainingRecord dan sertifikat (D-18)
                // TIDAK kirim notifikasi grup completion

                _logger.LogInformation(
                    "GradingService: session {SessionId} status Menunggu Penilaian — {EssayCount} soal Essay perlu dinilai HC.",
                    session.Id, packageQuestions.Count(q => (q.QuestionType ?? "MultipleChoice") == "Essay"));

                return true;
            }

            // ---- 3b. Non-essay flow: status "Completed" (existing logic) ----
            // STAT-01 (D-02): guard diperluas dari `!= "Completed"` ke terminal/non-resurrectable set.
            // Tanpa ini sesi Abandoned/Cancelled/PendingGrading bisa di-resurrect jadi Completed-lulus + cert.
            // rowsAffected==0 → return false (branch di bawah, SUDAH ADA — kini juga tangkap resurrection blocked).
            var rowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == session.Id
                    && s.Status != S.Completed && s.Status != S.Abandoned
                    && s.Status != S.Cancelled && s.Status != S.PendingGrading)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Score, finalPercentage)
                    .SetProperty(r => r.Status, S.Completed)
                    .SetProperty(r => r.Progress, 100)
                    .SetProperty(r => r.IsPassed, isPassed)
                    .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
                );

            if (rowsAffected == 0)
            {
                // Race condition: session sudah di-complete oleh request lain
                _logger.LogWarning(
                    "GradingService: race condition untuk session {SessionId} — session sudah Completed, skip.",
                    session.Id);
                return false;
            }

            // ---- 4. Update PackageAssignment.IsCompleted ----
            await _context.UserPackageAssignments
                .Where(a => a.AssessmentSessionId == session.Id)
                .ExecuteUpdateAsync(a => a.SetProperty(r => r.IsCompleted, true));

            // Phase 324 D-01: TrainingRecord auto-create removed.
            // AssessmentSession (Status=Completed) is sole source for "Assessment Online" row
            // di /CMP/Records via WorkerDataService.GetUnifiedRecords. Regression dari commit 766011b6
            // (re-add 2026-04-10) - original removal di 79284609 (2026-03-18).

            // ---- 6. Generate NomorSertifikat (jika applicable) ----
            // Phase 423 CERT-01 (SITE 1): gate kelayakan cert TUNGGAL via CertIssuanceRules.ShouldIssueCertificate
            // (tolak PreTest + wajib GenerateCertificate && lulus). Loop seq terkonsolidasi -> TryAssignNextSeqAsync (CERT-03).
            if (CertIssuanceRules.ShouldIssueCertificate(session))
            {
                var certNow = DateTime.Now;
                bool certSaved = await CertNumberHelper.TryAssignNextSeqAsync(_context, session.Id, certNow);

                if (certSaved)
                {
                    // CERT-02/06: derive ValidUntil dari CompletedAt utk CertificateType kanonik (Permanent->null/Annual->+1y/3-Year->+3y).
                    var validUntil = CertIssuanceRules.DeriveValidUntil(session.CertificateType, session.CompletedAt);
                    if (validUntil != null)
                    {
                        await _context.AssessmentSessions
                            .Where(s => s.Id == session.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ValidUntil, (DateOnly?)validUntil));
                    }
                }
                else
                {
                    // CERT-03 (D-03): seq gagal setelah retry. NON-DESTRUKTIF — sesi tetap Completed/IsPassed (JANGAN rollback).
                    // Stamp UpdatedAt + log + audit "CertIssuanceFailed" -> predikat queryable utk HC:
                    // (IsPassed==true && GenerateCertificate && AssessmentType!=PreTest && NomorSertifikat==null).
                    _logger.LogError("Cert gagal terbit SessionId={SessionId} setelah retry — sesi tetap Completed (non-destruktif), tandai HC.", session.Id);
                    await _context.AssessmentSessions
                        .Where(s => s.Id == session.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
                    try
                    {
                        // Audit best-effort via _context (GradingService tak inject AuditLogService — hindari ubah ctor/DI).
                        _context.AuditLogs.Add(new AuditLog
                        {
                            ActorUserId = "system",
                            ActorName = "Sistem (Grading)",
                            ActionType = "CertIssuanceFailed",
                            Description = $"Nomor sertifikat gagal terbit (auto-grade) utk SessionId={session.Id} setelah retry — tandai HC untuk terbit/retry manual.",
                            TargetId = session.Id,
                            TargetType = "AssessmentSession",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();
                    }
                    catch { /* audit best-effort: jangan blok lifecycle worker */ }
                }
            }

            // ---- 7. Notifikasi grup completion ----
            await _workerDataService.NotifyIfGroupCompleted(session);

            // ---- 8. PCOMP-01 (D-06): exam Proton lulus → penanda Origin="Exam" (guard D-05) ----
            // Cabang hasEssay (L190-227) early-return TIDAK lewat sini → di-cover defensive hook FinalizeEssayGrading (Plan 04, D-05a).
            if (session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue)
            {
                await _protonCompletionService.EnsureAsync(
                    session.UserId, session.ProtonTrackId.Value, session.CreatedBy ?? "",
                    "Exam", $"Exam Proton lulus (skor {finalPercentage}%).");
                // §7 titik 1: exam CL-B(b) lulus → pending Menunggu→Siap + notif HC (no-tx, idempotent).
                await _protonBypassService.MarkPendingReadyIfAnyAsync(session.Id);
            }

            return true;
        }

        /// <summary>
        /// Pure compute: hitung total/max score + IsPassed + ElemenTeknis breakdown TANPA side effect (tidak insert DB).
        /// Dipakai oleh RegradeAfterEditAsync (re-grade post-edit) + PreviewScoreAsync (dry-run).
        /// `GradeAndCompleteAsync` initial grading KEEP inline logic existing (TIDAK refactor) supaya regression risk = 0.
        /// </summary>
        /// <param name="session">Session target.</param>
        /// <param name="overrideAnswers">
        /// Optional. Dict (PackageQuestionId -> List of selected PackageOption.Id).
        /// Null -> baca semua dari PackageUserResponses normal.
        /// Non-null -> pakai override untuk question yang ada di dict, fallback DB untuk sisanya. Path PreviewEditScore.
        /// </param>
        private async Task<(int totalScore, int maxScore, bool isPassed, List<SessionElemenTeknisScore> etScores)>
            ComputeScoreAndETInternalAsync(AssessmentSession session, IDictionary<int, List<int>>? overrideAnswers = null)
        {
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);
            if (packageAssignment == null)
                return (0, 0, false, new List<SessionElemenTeknisScore>());

            var shuffledIds = packageAssignment.GetShuffledQuestionIds();
            var packageQuestions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => shuffledIds.Contains(q.Id))
                .ToListAsync();
            var questionLookup = packageQuestions.ToDictionary(q => q.Id);

            var dbResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == session.Id)
                .ToListAsync();

            HashSet<int> SelectedOptions(int qId)
            {
                if (overrideAnswers != null && overrideAnswers.TryGetValue(qId, out var ov))
                    return ov.ToHashSet();
                return dbResponses
                    .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue)
                    .Select(r => r.PackageOptionId!.Value)
                    .ToHashSet();
            }

            int totalScore = 0, maxScore = 0;
            foreach (var qId in shuffledIds)
            {
                if (!questionLookup.TryGetValue(qId, out var q)) continue;
                maxScore += q.ScoreValue;

                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice":
                        var mcSel = SelectedOptions(qId);
                        if (mcSel.Count > 0)
                        {
                            var optId = mcSel.First();
                            var opt = q.Options.FirstOrDefault(o => o.Id == optId);
                            if (opt?.IsCorrect == true) totalScore += q.ScoreValue;
                        }
                        break;
                    case "MultipleAnswer":
                        var maSel = SelectedOptions(qId);
                        var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                        if (maSel.SetEquals(maCorrect)) totalScore += q.ScoreValue;
                        break;
                    case "Essay":
                        break;
                }
            }

            int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
            bool isPassed = pct >= session.PassPercentage;

            var etScores = new List<SessionElemenTeknisScore>();
            var etGroups = packageQuestions
                .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

            foreach (var etGroup in etGroups)
            {
                int etCorrect = 0;
                int etTotal = etGroup.Count();
                foreach (var q in etGroup)
                {
                    switch (q.QuestionType ?? "MultipleChoice")
                    {
                        case "MultipleChoice":
                            var mcSel = SelectedOptions(q.Id);
                            if (mcSel.Count > 0)
                            {
                                var opt = q.Options.FirstOrDefault(o => o.Id == mcSel.First());
                                if (opt?.IsCorrect == true) etCorrect++;
                            }
                            break;
                        case "MultipleAnswer":
                            var maSel = SelectedOptions(q.Id);
                            var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                            if (maSel.SetEquals(maCorrect)) etCorrect++;
                            break;
                        case "Essay":
                            break;
                    }
                }
                etScores.Add(new SessionElemenTeknisScore
                {
                    AssessmentSessionId = session.Id,
                    ElemenTeknis = etGroup.Key,
                    CorrectCount = etCorrect,
                    QuestionCount = etTotal
                });
            }

            return (totalScore, maxScore, isPassed, etScores);
        }

        /// <summary>
        /// Re-grade session yang sudah Completed setelah edit jawaban oleh Admin/HC.
        /// DELETE existing SessionElemenTeknisScores -> recompute -> update session + cascade sertifikat/TR.
        /// CALLER bertanggung jawab: open transaction sebelum invoke, commit setelahnya.
        /// </summary>
        /// <returns>(newScore, newIsPassed, oldScore, oldIsPassed)</returns>
        public async Task<(int newScore, bool newIsPassed, int? oldScore, bool? oldIsPassed)>
            RegradeAfterEditAsync(AssessmentSession session)
        {
            int? oldScore = session.Score;
            bool? oldIsPassed = session.IsPassed;

            // 1. DELETE existing ET scores
            await _context.SessionElemenTeknisScores
                .Where(et => et.AssessmentSessionId == session.Id)
                .ExecuteDeleteAsync();

            // 2. Recompute (overrideAnswers = null -> baca DB which is already updated)
            var (totalScore, maxScore, isPassed, etScores) = await ComputeScoreAndETInternalAsync(session);
            int newPct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

            // 3. Insert new ET scores
            _context.SessionElemenTeknisScores.AddRange(etScores);

            // 4. Update session — status guard WHERE Status == "Completed"
            var rowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.Status == "Completed")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Score, newPct)
                    .SetProperty(r => r.IsPassed, isPassed)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
                );

            if (rowsAffected == 0)
            {
                _logger.LogWarning(
                    "GradingService.RegradeAfterEditAsync: session {SessionId} bukan Completed (race).",
                    session.Id);
                throw new InvalidOperationException("Session bukan dalam status Completed saat re-grade.");
            }

            await _context.SaveChangesAsync();

            // 5. Cascade sertifikat + TrainingRecord (only when flip)
            bool wasPassed = oldIsPassed ?? false;
            if (wasPassed && !isPassed)
            {
                // Pass -> Fail: revoke sertifikat (Phase 324 D-03: TR cascade removed)
                await _context.AssessmentSessions
                    .Where(s => s.Id == session.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.NomorSertifikat, (string?)null)
                        .SetProperty(r => r.ValidUntil, (DateOnly?)null));  // Phase 327 — cast DateOnly?

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Pass->Fail - sertifikat dicabut (Phase 324 D-03).",
                    session.Id);

                // PCOMP-02 (D-06/A-M9): flip Pass→Fail hapus penanda HANYA Origin="Exam" (Bypass/Interview kebal).
                // Guard TANPA isPassed (cabang ini isPassed==false): hapus = saat gagal.
                // W-09: braced — hook WAJIB di dalam guard Proton (T-360-35).
                if (session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue)
                {
                    await _protonCompletionService.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
                    // §7 titik 3 (D-15): re-grade Pass→Fail → pending Siap balik Menunggu (no-tx).
                    await _protonBypassService.RevertPendingToMenungguAsync(session.Id);
                }
            }
            else if (!wasPassed && isPassed)
            {
                // Fail -> Pass: generate sertifikat (Phase 324 D-03: TR cascade removed)
                // Phase 423 CERT-01 (SITE 2): seragamkan gate ke CertIssuanceRules.ShouldIssueCertificate
                // (cabang ini sudah flip Fail->Pass jadi IsPassed==true). Loop seq -> TryAssignNextSeqAsync (CERT-03).
                if (CertIssuanceRules.ShouldIssueCertificate(session))
                {
                    var certNow = DateTime.Now;
                    bool certSaved = await CertNumberHelper.TryAssignNextSeqAsync(_context, session.Id, certNow);

                    if (certSaved)
                    {
                        // CERT-02/06 (D-10): ValidUntil derive dari CompletedAt utk CertificateType kanonik (paritas SITE 1).
                        var validUntil = CertIssuanceRules.DeriveValidUntil(session.CertificateType, session.CompletedAt);
                        if (validUntil != null)
                        {
                            await _context.AssessmentSessions
                                .Where(s => s.Id == session.Id)
                                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ValidUntil, (DateOnly?)validUntil));
                        }
                    }
                    else
                    {
                        // CERT-03 non-destruktif: stamp UpdatedAt + audit (predikat sinyal HC, paritas SITE 1).
                        _logger.LogError("RegradeAfterEditAsync: cert gagal terbit session {SessionId} setelah retry — sesi tetap Completed (non-destruktif), tandai HC.",
                            session.Id);
                        await _context.AssessmentSessions
                            .Where(s => s.Id == session.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
                        try
                        {
                            _context.AuditLogs.Add(new AuditLog
                            {
                                ActorUserId = "system",
                                ActorName = "Sistem (Regrade)",
                                ActionType = "CertIssuanceFailed",
                                Description = $"Nomor sertifikat gagal terbit (re-grade Fail->Pass) utk SessionId={session.Id} setelah retry — tandai HC.",
                                TargetId = session.Id,
                                TargetType = "AssessmentSession",
                                CreatedAt = DateTime.UtcNow
                            });
                            await _context.SaveChangesAsync();
                        }
                        catch { /* audit best-effort */ }
                    }
                }

                _logger.LogInformation(
                    "RegradeAfterEditAsync: session {SessionId} flip Fail->Pass - sertifikat dibuat (jika applicable, Phase 324 D-03).",
                    session.Id);

                // PCOMP-01/02 (D-06): flip Fail→Pass terbit penanda Origin="Exam" (guard D-05).
                // W-09: braced — hook WAJIB di dalam guard Proton (T-360-35).
                if (session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue)
                {
                    await _protonCompletionService.EnsureAsync(
                        session.UserId, session.ProtonTrackId.Value, session.CreatedBy ?? "",
                        "Exam", $"Re-grade Fail→Pass (skor {newPct}%).");
                    // §7 titik 2: re-grade Fail→Pass → pending Menunggu→Siap + notif HC (no-tx).
                    await _protonBypassService.MarkPendingReadyIfAnyAsync(session.Id);
                }
            }
            // Pass->Pass, Fail->Fail: no cascade

            return (newPct, isPassed, oldScore, oldIsPassed);
        }

        /// <summary>
        /// Public wrapper for PreviewEditScore (dry-run) — exposes ComputeScoreAndETInternalAsync without ET breakdown.
        /// Consumed by PLAN 03 Task 9 PreviewEditScore controller action.
        /// </summary>
        public async Task<(int newScore, bool newIsPassed)> PreviewScoreAsync(
            AssessmentSession session,
            IDictionary<int, List<int>> overrideAnswers)
        {
            var (totalScore, maxScore, isPassed, _) = await ComputeScoreAndETInternalAsync(session, overrideAnswers);
            int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
            return (pct, isPassed);
        }
    }
}
