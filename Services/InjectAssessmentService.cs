using System.Text.Json;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Service inject hasil assessment manual "seakan online" (Phase 393 INJ-01/02).
    ///
    /// Orkestrasi TIPIS: menyusun AssessmentSession + Package + Responses dari <see cref="InjectRequest"/>,
    /// lalu MENDELEGASIKAN perhitungan skor/lulus/sertifikat ke <see cref="GradingService"/> +
    /// <see cref="Helpers.AssessmentScoreAggregator"/> + <see cref="Helpers.CertNumberHelper"/> —
    /// nol duplikasi logic (sumber kebenaran SAMA dengan jalur online).
    ///
    /// SCOPE Plan 01: hanya KONTRAK (ctor DI + signature). Body diisi Plan 02.
    /// Service TIDAK punya HttpContext — identitas actor dilewatkan sebagai parameter (RESEARCH A4).
    /// </summary>
    public class InjectAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly GradingService _gradingService;
        private readonly ILogger<InjectAssessmentService> _logger;

        public InjectAssessmentService(
            ApplicationDbContext context,
            GradingService gradingService,
            ILogger<InjectAssessmentService> logger)
        {
            _context = context;
            _gradingService = gradingService;
            _logger = logger;
        }

        /// <summary>
        /// Inject satu batch (1 room, 1 paket, banyak worker) "seakan online".
        /// </summary>
        /// <param name="req">Spesifikasi room + soal authored + worker beserta jawaban.</param>
        /// <param name="actorUserId">Id user yang melakukan inject (untuk audit, dari controller terotentikasi).</param>
        /// <param name="actorName">Nama actor (untuk audit).</param>
        public async Task<InjectResult> InjectBatchAsync(InjectRequest req, string actorUserId, string actorName)
        {
            var result = new InjectResult();
            var actorDisplay = string.IsNullOrWhiteSpace(actorName) ? actorUserId : actorName;

            // ---- Pre-flight (D-03 reject-all) — nol tulisan bila ada baris invalid ----
            var (errors, usersByNip) = await PreflightValidateAsync(req);
            if (errors.Count > 0)
            {
                result.Rejected = true;
                result.Success = false;
                result.PerRowErrors = errors;
                result.Message = "Batch ditolak: ada baris tidak valid. Tidak ada data ditulis.";
                // TODO Task 3: audit ManualInjectRejected (D-11c) sebelum return
                return result;
            }

            // ---- Dedup (D-01/D-02) — skip+lapor (bukan gagalkan batch); intra-batch via seenUserIds ----
            var dupUserIds = await FindDuplicateNipsAsync(req, usersByNip);
            var toProcess = new List<(InjectWorkerSpec spec, ApplicationUser user)>();
            var skippedNips = new List<string>();
            var seenUserIds = new HashSet<string>();
            foreach (var w in req.Workers)
            {
                var user = usersByNip[w.Nip];
                if (dupUserIds.Contains(user.Id) || !seenUserIds.Add(user.Id))
                {
                    skippedNips.Add(w.Nip);
                    continue;
                }
                toProcess.Add((w, user));
            }
            result.SkippedNips = skippedNips;

            // ---- Atomic batch (D-04) ----
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool hasEssay = req.Questions.Any(q => (q.QuestionType ?? "MultipleChoice") == "Essay");
                int? sentinelPackageId = null;
                var qMap = new Dictionary<int, int>();    // question TempId -> real Id
                var optMap = new Dictionary<int, int>();  // option TempId -> real Id
                var realQuestionIdsInOrder = new List<int>();
                var successSessions = new List<(int sessionId, string nip)>();

                foreach (var (spec, user) in toProcess)
                {
                    var startedAt = req.StartedAt ?? req.CompletedAt;
                    var schedule = req.Schedule ?? req.CompletedAt;

                    // a. Session — ⚠ deviasi BulkBackfill: AccessToken="INJECT" + tipe assessment dari req (lihat catatan field di bawah)
                    var session = new AssessmentSession
                    {
                        UserId = user.Id,
                        Title = req.Title,
                        Category = req.Category,
                        IsManualEntry = true,                                 // INJ-02
                        AccessToken = "INJECT",                               // BUKAN "BACKFILL"
                        IsTokenRequired = false,
                        AssessmentType = req.AssessmentType,                  // "Standard"/PreTest/PostTest (lihat catatan field di bawah)
                        AllowAnswerReview = req.AllowAnswerReview,
                        GenerateCertificate = req.CertMode != InjectCertMode.None,
                        ValidUntil = spec.CertValidUntil,                     // null = permanent (D-10)
                        NomorSertifikat = req.CertMode == InjectCertMode.Manual ? spec.ManualCertNumber : null,  // D-09
                        PassPercentage = req.PassPercentage,
                        DurationMinutes = req.DurationMinutes,
                        LinkedGroupId = req.LinkedGroupId,
                        LinkedSessionId = req.LinkedSessionId,
                        Schedule = schedule,
                        StartedAt = startedAt,
                        CompletedAt = req.CompletedAt,                        // backdate (D-06) — grade overwrite → re-apply [g]
                        Status = "Open",
                        Progress = 0,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = actorUserId
                    };
                    _context.AssessmentSessions.Add(session);
                    await _context.SaveChangesAsync();   // dapat session.Id

                    // b. Sentinel package + questions + options (1× per batch, anchored ke sesi pertama)
                    if (sentinelPackageId == null)
                    {
                        var package = new AssessmentPackage
                        {
                            AssessmentSessionId = session.Id,
                            PackageName = "Paket A",
                            PackageNumber = 1,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.AssessmentPackages.Add(package);
                        await _context.SaveChangesAsync();
                        sentinelPackageId = package.Id;

                        foreach (var qSpec in req.Questions.OrderBy(q => q.Order))
                        {
                            var q = new PackageQuestion
                            {
                                AssessmentPackageId = package.Id,
                                QuestionText = qSpec.QuestionText,
                                QuestionType = qSpec.QuestionType,
                                ScoreValue = qSpec.ScoreValue,
                                Order = qSpec.Order,
                                ElemenTeknis = qSpec.ElemenTeknis,
                                Rubrik = qSpec.Rubrik
                            };
                            _context.PackageQuestions.Add(q);
                            await _context.SaveChangesAsync();
                            qMap[qSpec.TempId] = q.Id;
                            realQuestionIdsInOrder.Add(q.Id);

                            foreach (var oSpec in qSpec.Options)
                            {
                                var o = new PackageOption { PackageQuestionId = q.Id, OptionText = oSpec.OptionText, IsCorrect = oSpec.IsCorrect };
                                _context.PackageOptions.Add(o);
                                await _context.SaveChangesAsync();
                                optMap[oSpec.TempId] = o.Id;
                            }
                        }
                    }

                    // c. Assignment sentinel — ShuffledQuestionIds = ID apa-adanya (TAK shuffle); IsCompleted=true
                    var assignment = new UserPackageAssignment
                    {
                        AssessmentSessionId = session.Id,
                        AssessmentPackageId = sentinelPackageId.Value,
                        UserId = user.Id,
                        ShuffledQuestionIds = JsonSerializer.Serialize(realQuestionIdsInOrder),
                        ShuffledOptionIdsPerQuestion = "{}",
                        AssignedAt = DateTime.UtcNow,
                        IsCompleted = true,
                        SavedQuestionCount = realQuestionIdsInOrder.Count
                    };
                    _context.UserPackageAssignments.Add(assignment);
                    await _context.SaveChangesAsync();

                    // d. Responses (map TempId→realId; MA = banyak baris; Essay = TextAnswer+EssayScore) — DULU sebelum grade (Pitfall 6)
                    foreach (var ans in spec.Answers)
                    {
                        if (!qMap.TryGetValue(ans.QuestionTempId, out var realQId)) continue;
                        var qSpec = req.Questions.First(q => q.TempId == ans.QuestionTempId);
                        if ((qSpec.QuestionType ?? "MultipleChoice") == "Essay")
                        {
                            _context.PackageUserResponses.Add(new PackageUserResponse
                            {
                                AssessmentSessionId = session.Id,
                                PackageQuestionId = realQId,
                                TextAnswer = ans.TextAnswer,
                                EssayScore = ans.EssayScore,
                                SubmittedAt = req.CompletedAt
                            });
                        }
                        else
                        {
                            foreach (var tempOpt in ans.SelectedOptionTempIds)
                            {
                                if (!optMap.TryGetValue(tempOpt, out var realOptId)) continue;
                                _context.PackageUserResponses.Add(new PackageUserResponse
                                {
                                    AssessmentSessionId = session.Id,
                                    PackageQuestionId = realQId,
                                    PackageOptionId = realOptId,
                                    SubmittedAt = req.CompletedAt
                                });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();

                    // e. Delegasi grade (D-08 cert gate, ET, status) — mesin existing, NOL duplikasi skor/lulus
                    await _gradingService.GradeAndCompleteAsync(session);

                    // f. Essay finalize-block (D-05) — recompute + PendingGrading→Completed.
                    //    Cert ditunda ke step h (unified D-12) agar basis tanggal backdate konsisten.
                    if (hasEssay)
                    {
                        var allQuestions = await _context.PackageQuestions.Include(q => q.Options)
                            .Where(q => realQuestionIdsInOrder.Contains(q.Id)).ToListAsync();
                        var allResponses = await _context.PackageUserResponses
                            .Where(r => r.AssessmentSessionId == session.Id).ToListAsync();
                        var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
                        await _context.AssessmentSessions
                            .Where(s => s.Id == session.Id && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(r => r.Score, agg.Percentage)
                                .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
                                .SetProperty(r => r.IsPassed, agg.IsPassed)
                                .SetProperty(r => r.CompletedAt, DateTime.UtcNow));
                    }

                    // g. Backdate re-apply (D-06, Pitfall 1) — grade/finalize overwrite CompletedAt=UtcNow
                    await _context.AssessmentSessions
                        .Where(s => s.Id == session.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.CompletedAt, req.CompletedAt)
                            .SetProperty(r => r.StartedAt, startedAt)
                            .SetProperty(r => r.Schedule, schedule));

                    // h. Cert auto backdate (D-12) — ROMAN/year = tanggal ujian (req.CompletedAt), BUKAN DateTime.Now.
                    //    GradeAndCompleteAsync (non-essay) sudah generate cert basis today → release & regenerate basis backdate.
                    //    Cert manual (D-09): NomorSertifikat ter-set pra-grade, guard WHERE NomorSertifikat==null tak menimpa → skip di sini.
                    //    !isPassed → tak ada cert (D-08, gratis dari gate).
                    if (req.CertMode == InjectCertMode.Auto && session.GenerateCertificate)
                    {
                        var passedNow = await _context.AssessmentSessions.AsNoTracking()
                            .Where(s => s.Id == session.Id).Select(s => s.IsPassed).FirstOrDefaultAsync();
                        if (passedNow == true)
                        {
                            await _context.AssessmentSessions.Where(s => s.Id == session.Id)
                                .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, (string?)null));
                            var certNow = req.CompletedAt;
                            int certYear = certNow.Year;
                            int certAttempts = 0; const int maxCertAttempts = 3; bool certSaved = false;
                            while (!certSaved && certAttempts < maxCertAttempts)
                            {
                                certAttempts++;
                                try
                                {
                                    var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
                                    await _context.AssessmentSessions
                                        .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
                                    certSaved = true;
                                }
                                catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex)) { }
                            }
                        }
                    }

                    successSessions.Add((session.Id, spec.Nip));
                    result.SuccessSessionIds.Add(session.Id);
                }

                // TODO Task 3: audit in-tx (ManualInject sukses per sesi + ManualInjectSkipped per dup) sebelum SaveChanges/Commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.Message = $"Inject selesai: {successSessions.Count} sesi dibuat, {skippedNips.Count} dilewati.";
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "InjectBatch gagal untuk title={Title}", req.Title);
                return new InjectResult { Success = false, Rejected = false, Message = "Terjadi kesalahan saat menyimpan; seluruh batch dibatalkan (rollback)." };
            }
        }

        /// <summary>
        /// Pre-flight validasi seluruh batch (D-03 reject-all). Kumpulkan SEMUA error per-baris
        /// (JANGAN early-return) agar HC lihat semua sekaligus. Tidak menulis apa pun.
        /// Mengembalikan daftar error + map NIP→user (dipakai dedup + insert bila lolos).
        /// </summary>
        private async Task<(List<InjectRowError> errors, Dictionary<string, ApplicationUser> usersByNip)>
            PreflightValidateAsync(InjectRequest req)
        {
            var errors = new List<InjectRowError>();

            // 1. NIP exists (D-03) — resolve semua NIP up-front
            var nips = req.Workers.Select(w => w.Nip).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
            var usersByNip = await _context.Users
                .Where(u => u.NIP != null && nips.Contains(u.NIP))
                .ToDictionaryAsync(u => u.NIP!);

            var qByTemp = req.Questions.ToDictionary(q => q.TempId);

            // 4. Tanggal ≤ today (D-06) — batch-level (CompletedAt + optional StartedAt/Schedule)
            var today = DateTime.Today;
            if (req.CompletedAt.Date > today || req.CompletedAt.Year < 2000)
                errors.Add(new InjectRowError { Nip = "", Message = "Tanggal ujian (CompletedAt) tidak boleh di masa depan atau tahun tidak masuk akal." });
            if (req.StartedAt.HasValue && (req.StartedAt.Value.Date > today || req.StartedAt.Value.Year < 2000))
                errors.Add(new InjectRowError { Nip = "", Message = "Tanggal mulai (StartedAt) tidak boleh di masa depan atau tahun tidak masuk akal." });
            if (req.Schedule.HasValue && (req.Schedule.Value.Date > today || req.Schedule.Value.Year < 2000))
                errors.Add(new InjectRowError { Nip = "", Message = "Tanggal jadwal (Schedule) tidak boleh di masa depan atau tahun tidak masuk akal." });

            // per-worker: NIP + opsi valid + EssayScore range
            foreach (var w in req.Workers)
            {
                if (string.IsNullOrWhiteSpace(w.Nip) || !usersByNip.ContainsKey(w.Nip))
                {
                    errors.Add(new InjectRowError { Nip = w.Nip, Message = $"NIP {w.Nip} tidak ditemukan di sistem." });
                    continue;   // tanpa user, validasi lanjutan tak relevan
                }

                foreach (var ans in w.Answers)
                {
                    if (!qByTemp.TryGetValue(ans.QuestionTempId, out var qSpec))
                    {
                        errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Opsi/soal tidak valid untuk NIP {w.Nip} (soal tidak dikenal)." });
                        continue;
                    }
                    var validOptTempIds = qSpec.Options.Select(o => o.TempId).ToHashSet();
                    var qType = qSpec.QuestionType ?? "MultipleChoice";

                    if (qType == "Essay")
                    {
                        // Essay: tanpa opsi; wajib EssayScore (D-05); range 0..ScoreValue (D-07, BUKAN 0..persen)
                        if (ans.SelectedOptionTempIds.Count > 0)
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Soal essay tidak boleh punya opsi untuk NIP {w.Nip}." });
                        if (!ans.EssayScore.HasValue)
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Skor essay NIP {w.Nip} wajib diisi." });
                        else if (ans.EssayScore.Value < 0 || ans.EssayScore.Value > qSpec.ScoreValue)
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Skor essay NIP {w.Nip} di luar rentang 0..{qSpec.ScoreValue}." });
                    }
                    else
                    {
                        foreach (var optTemp in ans.SelectedOptionTempIds)
                            if (!validOptTempIds.Contains(optTemp))
                                errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Opsi/soal tidak valid untuk NIP {w.Nip}." });

                        if (qType == "MultipleChoice" && ans.SelectedOptionTempIds.Count != 1)
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Soal pilihan ganda NIP {w.Nip} wajib tepat 1 jawaban." });
                        else if (qType == "MultipleAnswer" && ans.SelectedOptionTempIds.Count < 1)
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Soal multi-jawaban NIP {w.Nip} wajib minimal 1 jawaban." });
                    }
                }
            }

            // 5. Cert manual unik (D-09) — intra-batch + DB collision, mode Manual wajib nomor
            if (req.CertMode == InjectCertMode.Manual)
            {
                var manualPairs = req.Workers
                    .Select(w => (w.Nip, Num: (w.ManualCertNumber ?? "").Trim()))
                    .ToList();

                foreach (var (nip, num) in manualPairs)
                    if (string.IsNullOrWhiteSpace(num))
                        errors.Add(new InjectRowError { Nip = nip, Message = $"Mode sertifikat manual: NIP {nip} wajib mengisi nomor sertifikat." });

                var nonEmpty = manualPairs.Where(p => !string.IsNullOrWhiteSpace(p.Num)).ToList();
                foreach (var n in nonEmpty.GroupBy(p => p.Num).Where(g => g.Count() > 1).Select(g => g.Key))
                    errors.Add(new InjectRowError { Nip = "", Message = $"Nomor sertifikat manual {n} duplikat dalam batch." });

                var numbers = nonEmpty.Select(p => p.Num).Distinct().ToList();
                if (numbers.Count > 0)
                {
                    var existing = await _context.AssessmentSessions
                        .Where(s => s.NomorSertifikat != null && numbers.Contains(s.NomorSertifikat))
                        .Select(s => s.NomorSertifikat!)
                        .ToListAsync();
                    foreach (var n in existing)
                        errors.Add(new InjectRowError { Nip = "", Message = $"Nomor sertifikat {n} sudah dipakai." });
                }
            }

            return (errors, usersByNip);
        }

        /// <summary>
        /// Cari UserId yang SUDAH punya sesi inject duplikat di DB (D-01/D-02 skip+lapor, bukan gagalkan batch).
        /// Kunci dedup = UserId + NormalizeTitleForDup(Title) + Category + CompletedAt.Date. Cert-aware (D-02):
        /// bila CertMode!=None ATAU sesi existing punya NomorSertifikat dgn judul+tanggal sama → juga dianggap dup
        /// (cegah double-cert). Normalizer C#-only (tak EF-translatable) → tarik kandidat lalu banding in-memory.
        /// </summary>
        private async Task<HashSet<string>> FindDuplicateNipsAsync(
            InjectRequest req, Dictionary<string, ApplicationUser> usersByNip)
        {
            var norm = HcPortal.Controllers.AdminBaseController.NormalizeTitleForDup(req.Title);
            var dateOnly = req.CompletedAt.Date;
            var relevantUserIds = usersByNip.Values.Select(u => u.Id).ToList();

            var candidates = await _context.AssessmentSessions
                .Where(s => s.IsManualEntry && relevantUserIds.Contains(s.UserId))
                .Select(s => new { s.UserId, s.Title, s.Category, s.CompletedAt, s.NomorSertifikat })
                .ToListAsync();

            var dupUserIds = new HashSet<string>();
            bool certAware = req.CertMode != InjectCertMode.None;
            foreach (var c in candidates)
            {
                bool titleDateMatch = HcPortal.Controllers.AdminBaseController.NormalizeTitleForDup(c.Title) == norm
                    && c.CompletedAt?.Date == dateOnly;
                bool sameKey = titleDateMatch && c.Category == req.Category;
                bool certDup = titleDateMatch && (certAware || c.NomorSertifikat != null);
                if (sameKey || certDup)
                    dupUserIds.Add(c.UserId);
            }
            return dupUserIds;
        }
    }
}
