using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

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

        public GradingService(
            ApplicationDbContext context,
            IWorkerDataService workerDataService,
            ILogger<GradingService> logger)
        {
            _context = context;
            _workerDataService = workerDataService;
            _logger = logger;
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
        /// 6. Generate NomorSertifikat jika session.GenerateCertificate && isPassed (retry 3x)
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
                        var mcResponse = allResponses
                            .FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
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
                            var etMcResponse = allResponses
                                .FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
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

                var essayRowsAffected = await _context.AssessmentSessions
                    .Where(s => s.Id == session.Id && s.Status != "Completed" && s.Status != "Menunggu Penilaian")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Score, interimPercentage)
                        .SetProperty(r => r.Status, "Menunggu Penilaian")
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
            // ExecuteUpdateAsync dengan WHERE Status != "Completed" sebagai status guard (D-04)
            var rowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.Status != "Completed")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Score, finalPercentage)
                    .SetProperty(r => r.Status, "Completed")
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

            // ---- 5. Buat TrainingRecord (dengan duplicate guard) ----
            var judul = $"Assessment: {session.Title}";
            bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
                t.UserId == session.UserId &&
                t.Judul == judul &&
                t.Tanggal == session.Schedule);

            if (!trainingRecordExists)
            {
                _context.TrainingRecords.Add(new TrainingRecord
                {
                    UserId = session.UserId,
                    Judul = judul,
                    Kategori = session.Category ?? "Assessment",
                    Tanggal = session.Schedule,
                    TanggalSelesai = DateTime.UtcNow,
                    Penyelenggara = "Internal",
                    Status = isPassed ? "Passed" : "Failed"
                });
                await _context.SaveChangesAsync();
            }

            // ---- 6. Generate NomorSertifikat (jika applicable) ----
            // Kondisi: session.GenerateCertificate && isPassed (T-296-03: retry 3x + WHERE NomorSertifikat == null)
            if (session.GenerateCertificate && isPassed)
            {
                var certNow = DateTime.Now;
                int certYear = certNow.Year;
                int certAttempts = 0;
                const int maxCertAttempts = 3;
                bool certSaved = false;

                while (!certSaved && certAttempts < maxCertAttempts)
                {
                    certAttempts++;
                    try
                    {
                        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
                        await _context.AssessmentSessions
                            .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow))
                            );
                        certSaved = true;
                    }
                    catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
                    {
                        // Retry dengan sequence baru (T-296-03)
                    }
                }
            }

            // ---- 7. Notifikasi grup completion ----
            await _workerDataService.NotifyIfGroupCompleted(session);

            return true;
        }
    }
}
