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
                // Audit reject (D-11c) — reject terjadi SEBELUM tx (no tulisan sesi), jadi commit terpisah.
                // ActionType khusus-reject terpisah agar count sesi-sukses (SC4) tetap bersih.
                _context.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = actorUserId,
                    ActorName = actorDisplay,
                    ActionType = "ManualInjectRejected",
                    Description = $"Inject ditolak (pre-flight): {errors.Count} baris invalid. Judul={req.Title}.",
                    TargetType = "AssessmentSession",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
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

                // ---- 397 D-01/D-02: resolve konteks link SEKALI per batch (server-otoritas, T-397-06) ----
                // Drive off req.LinkTargetRepId — JANGAN trust req.LinkedGroupId mentah dari client.
                // Hanya Pre/Post yang menautkan (D-06 — Standard tak pernah link).
                int? resolvedGroupId = null;
                string? oppositeType = null;
                var siblingByUserId = new Dictionary<string, AssessmentSession>();   // 1 sibling per UserId (tipe-lawan)
                var targetRoomSessions = new List<AssessmentSession>();              // Kasus B: tulis stiker ke SEMUA
                var mutatedOnlineSessionIds = new HashSet<int>();                    // sesi ONLINE dimutasi → audit "LinkPrePost"
                bool kasusB = false;
                if (req.LinkTargetRepId.HasValue
                    && (req.AssessmentType == AssessmentConstants.AssessmentType.PreTest
                        || req.AssessmentType == AssessmentConstants.AssessmentType.PostTest))
                {
                    var (groupId, opposite, isKasusB, targetSessions, rep) =
                        await ResolveLinkContextAsync(req.LinkTargetRepId, req.AssessmentType);
                    if (rep == null)
                    {
                        await transaction.RollbackAsync();
                        return new InjectResult { Rejected = true, Success = false, Message = "Room pasangan tidak valid atau bukan tipe lawan." };
                    }
                    resolvedGroupId = groupId;
                    oppositeType = opposite;
                    kasusB = isKasusB;
                    targetRoomSessions = targetSessions;

                    // Peta sibling tipe-lawan by-UserId di SELURUH grup (online + inject ter-commit). Guard >1 (A2).
                    var siblingQuery = await _context.AssessmentSessions
                        .Where(s => s.AssessmentType == oppositeType
                                 && (kasusB
                                        ? targetRoomSessions.Select(t => t.Id).Contains(s.Id)
                                        : s.LinkedGroupId == resolvedGroupId))
                        .ToListAsync();
                    // WR-02 (398.1): OrderBy(CreatedAt).ThenBy(Id) — sibling pick deterministik antar-request
                    // (CreatedAt bisa tabrakan dalam 1 batch → ThenBy(Id) pemecah-seri). Byte-identik dgn preview.
                    siblingByUserId = siblingQuery
                        .GroupBy(s => s.UserId)
                        .ToDictionary(g => g.Key, g => g.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id).First());
                }

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
                        // 397 D-02: JANGAN broadcast req.LinkedSessionId/LinkedGroupId (Pitfall 1) —
                        // di-wire per-pekerja SETELAH SaveChanges (session.Id ada) di bawah.
                        LinkedGroupId = null,
                        LinkedSessionId = null,
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

                    // a2. 397 D-02/D-03: wiring link per-pekerja (session.Id sudah ada). Resolve sibling by-UserId
                    //     (BUKAN broadcast). LinkedGroupId SELALU di-set bila tertaut (gabung grup, D-03).
                    if (resolvedGroupId.HasValue)
                    {
                        session.LinkedGroupId = resolvedGroupId.Value;
                        if (siblingByUserId.TryGetValue(user.Id, out var sib) && sib.Id != session.Id)
                        {
                            session.LinkedSessionId = sib.Id;        // inject → sibling
                            sib.LinkedSessionId = session.Id;        // write-back sibling → inject (tracked, in-tx, D-02)
                            if (!sib.IsManualEntry) mutatedOnlineSessionIds.Add(sib.Id);   // online sibling write-back → audit
                        }
                        else
                        {
                            session.LinkedSessionId = null;          // unpaired (D-03) — sisi tunggal
                        }
                        await _context.SaveChangesAsync();
                    }

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

                    // e. Delegasi grade (D-08 cert gate, ET, status) — mesin existing, NOL duplikasi skor/lulus.
                    //    Tangkap bool return: false (race/terminal-status) → throw → rollback batch (defensive atomicity).
                    //    Sesi fresh Status="Open" normalnya selalu true; throw menjaga 0-parsial bila grading menolak.
                    var graded = await _gradingService.GradeAndCompleteAsync(session);
                    if (!graded)
                        throw new InvalidOperationException($"Grading menolak sesi inject {session.Id} (NIP={spec.Nip}).");

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

                    // MERGE FIX (v32.2 inject ↔ v32.7 Phase 423): GradeAndCompleteAsync kini MEN-SINKRON in-memory
                    // tracked `session.CompletedAt = UtcNow` setelah bulk ExecuteUpdate-nya (Phase 423 Rule-1). Backdate
                    // di atas hanya menulis DB (ExecuteUpdate BYPASS change-tracker) → tracked entity tetap UtcNow.
                    // Tanpa sinkron ini, SaveChanges di akhir transaksi mem-FLUSH tracked CompletedAt=UtcNow dan
                    // MENIMPA backdate (regresi: dedup gagal match tanggal, cert/ROMAN salah tahun, tanggal tampil hari ini).
                    // Sinkronkan tracked entity ke nilai backdate (mirror pola in-memory-sync Phase 423 itu sendiri).
                    session.CompletedAt = req.CompletedAt;
                    session.StartedAt = startedAt;
                    session.Schedule = schedule;

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

                // ---- 397 D-01 Kasus B: tulis stiker LinkedGroupId ke SEMUA sesi room target (Pitfall 2) ----
                // resolvedGroupId == RepresentativeId room target. SKOR/STATUS/JAWABAN ONLINE TIDAK DISENTUH (T-397-04).
                if (kasusB && resolvedGroupId.HasValue)
                {
                    foreach (var s in targetRoomSessions)
                    {
                        s.LinkedGroupId = resolvedGroupId.Value;          // stiker grup (HANYA kolom link)
                        if (!s.IsManualEntry) mutatedOnlineSessionIds.Add(s.Id);   // sesi online → audit "LinkPrePost"
                    }
                    await _context.SaveChangesAsync();
                }

                // ---- 397 D-09: audit "LinkPrePost" per sesi ONLINE dimutasi (stiker Kasus B / write-back bidirectional) ----
                // Inject↔inject (sibling IsManualEntry==true) TIDAK menambah mutatedOnlineSessionIds → 0 audit (D-10).
                foreach (var onlineSessionId in mutatedOnlineSessionIds)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = actorUserId,
                        ActorName = actorDisplay,
                        ActionType = "LinkPrePost",   // ⚠ 11 char ≤ MaxLength(50)
                        Description = $"Penanda grup ditulis ke sesi online {onlineSessionId} (LinkedGroupId={resolvedGroupId}). Skor/jawaban/status tidak diubah. Room target={req.Title}.",
                        TargetId = onlineSessionId,
                        TargetType = "AssessmentSession",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Audit in-tx (D-11) — _context.AuditLogs.Add LANGSUNG (BUKAN AuditLogService.LogAsync yang
                // SaveChanges sendiri → commit parsial). 3 ActionType TERPISAH agar count "ManualInject" = jumlah sesi sukses (SC4).
                foreach (var (sessionId, nip) in successSessions)
                {
                    var sState = await _context.AssessmentSessions.AsNoTracking()
                        .Where(s => s.Id == sessionId)
                        .Select(s => new { s.Score, s.CompletedAt })
                        .FirstAsync();
                    _context.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = actorUserId,
                        ActorName = actorDisplay,
                        ActionType = "ManualInject",   // ⚠ string TEPAT — count entri ini = jumlah sesi sukses (SC4)
                        Description = $"Inject hasil assessment manual: NIP={nip}, SessionId={sessionId}, Skor={sState.Score}, Tanggal={sState.CompletedAt:yyyy-MM-dd}.",
                        TargetId = sessionId,
                        TargetType = "AssessmentSession",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                foreach (var nip in skippedNips)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = actorUserId,
                        ActorName = actorDisplay,
                        ActionType = "ManualInjectSkipped",   // ⚠ TERPISAH — tak menggembungkan count "ManualInject" (SC4)
                        Description = $"Inject dilewati (duplikat): NIP={nip}, Judul={req.Title}, Tanggal={req.CompletedAt:yyyy-MM-dd}.",
                        TargetType = "AssessmentSession",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.LinkedGroupId = resolvedGroupId;   // 397 N5: surface group id bila commit tertaut (host unlink pasca-commit)
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

                        // Phase 395 D-04 (mode-guarded) — teks essay WAJIB bila skor diisi (essay "engaged" mode input-asli).
                        // Guard EssayScore.HasValue: essay di-skip (D-05) = OMIT spec → tak masuk loop ini → tak terblokir.
                        // Auto-gen TIDAK pernah meng-emit answer essay (D-08 — HC isi manual via form yang sama, hybrid),
                        // jadi rule ini hanya menyentuh essay yang benar-benar diisi skornya. BUKAN validasi global.
                        // Phase 396 D-05: teks essay OPSIONAL di jalur Excel (EssayTextRequired=false); WAJIB di jalur Form (default true).
                        if (req.EssayTextRequired && ans.EssayScore.HasValue && string.IsNullOrWhiteSpace(ans.TextAnswer))
                            errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Teks jawaban essay NIP {w.Nip} wajib diisi (mode input asli)." });
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

            // 6. Anti-dobel-link per-pekerja (D-08) — bila pekerja sudah punya sibling TIPE-SAMA di grup target
            //    (2 Pre / 1 user = ambigu gain-score by UserId), tolak pekerja itu. Daftar LENGKAP (no early-return).
            //    Default BLOK (UI-SPEC N4) → masuk reject-all path InjectBatchAsync (:49-68).
            if (req.LinkTargetRepId.HasValue
                && (req.AssessmentType == AssessmentConstants.AssessmentType.PreTest
                    || req.AssessmentType == AssessmentConstants.AssessmentType.PostTest))
            {
                var (groupId, _, _, _, rep) = await ResolveLinkContextAsync(req.LinkTargetRepId, req.AssessmentType);
                if (rep != null && groupId.HasValue)
                {
                    var injectUserIds = usersByNip.Values.Select(u => u.Id).ToList();
                    var nipByUserId = usersByNip.ToDictionary(kv => kv.Value.Id, kv => kv.Key);
                    var existingSameType = await _context.AssessmentSessions
                        .Where(s => s.LinkedGroupId == groupId.Value
                                 && s.AssessmentType == req.AssessmentType   // TIPE SAMA = ambigu
                                 && injectUserIds.Contains(s.UserId))
                        .Select(s => s.UserId).Distinct().ToListAsync();
                    var labelType = req.AssessmentType == AssessmentConstants.AssessmentType.PreTest ? "Pre-Test" : "Post-Test";
                    foreach (var userId in existingSameType)
                    {
                        var nip = nipByUserId.TryGetValue(userId, out var n) ? n : userId;
                        errors.Add(new InjectRowError
                        {
                            Nip = nip,
                            Message = $"Pekerja NIP {nip} sudah memiliki {labelType} di grup target — tidak dapat ditautkan dua kali."
                        });
                    }
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
                // certDup SENGAJA abaikan Category (D-02 anti double-cert): satu sertifikat per judul+tanggal,
                // lintas kategori. JANGAN tambah cek Category di sini — itu mengizinkan double-cert beda-kategori.
                bool certDup = titleDateMatch && (certAware || c.NomorSertifikat != null);
                if (sameKey || certDup)
                    dupUserIds.Add(c.UserId);
            }
            return dupUserIds;
        }

        // =====================================================================
        // Phase 397 INJ-12 — Link Pre/Post ke room existing (D-01..D-12)
        // =====================================================================

        /// <summary>
        /// Phase 397 D-01 — Resolusi konteks link (READ-ONLY, side-effect-free). Server RE-RESOLVE dari
        /// <paramref name="linkTargetRepId"/> (RepresentativeId room target) — JANGAN trust LinkedGroupId
        /// mentah dari client (Tampering guard T-397-06). Dipakai bersama oleh <see cref="InjectBatchAsync"/>
        /// (commit), <see cref="PreviewPairingAsync"/> (dry-run), dan anti-double preflight (D-08) → satu
        /// sumber kebenaran Kasus A/B (no drift).
        ///
        /// <para><b>Kasus A</b> (rep.LinkedGroupId != null): ADOPT — groupId = rep.LinkedGroupId; kasusB=false;
        /// targetSessions kosong (tak menulis stiker online).</para>
        /// <para><b>Kasus B</b> (rep.LinkedGroupId == null, standalone): groupId = rep.Id (konvensi
        /// AssessmentAdminController.cs:1270); kasusB=true; targetSessions = SEMUA sesi room target standalone
        /// (LOCKED key = Title + Category + Schedule.Date, tipe-lawan, LinkedGroupId==null) — Pitfall 2:
        /// stiker ditulis ke SEMUA, bukan hanya yang ter-pair. Key WAJIB sama persis dengan picker standalone
        /// (Plan 03-T1).</para>
        ///
        /// <para>rep == null bila linkTargetRepId tak valid / bukan tipe lawan.</para>
        /// </summary>
        private async Task<(int? groupId, string? opposite, bool kasusB,
            List<AssessmentSession> targetSessions, AssessmentSession? rep)>
            ResolveLinkContextAsync(int? linkTargetRepId, string assessmentType)
        {
            if (!linkTargetRepId.HasValue
                || (assessmentType != AssessmentConstants.AssessmentType.PreTest
                    && assessmentType != AssessmentConstants.AssessmentType.PostTest))
                return (null, null, false, new List<AssessmentSession>(), null);

            var opposite = assessmentType == AssessmentConstants.AssessmentType.PreTest
                ? AssessmentConstants.AssessmentType.PostTest
                : AssessmentConstants.AssessmentType.PreTest;

            // Server re-resolve sesi representatif + validasi tipe-lawan (IDOR/Tampering guard T-397-06).
            var rep = await _context.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == linkTargetRepId.Value && s.AssessmentType == opposite);
            if (rep == null)
                return (null, opposite, false, new List<AssessmentSession>(), null);

            if (rep.LinkedGroupId.HasValue)
            {
                // Kasus A — adopt, JANGAN sentuh online grouping value.
                return (rep.LinkedGroupId.Value, opposite, false, new List<AssessmentSession>(), rep);
            }

            // Kasus B — stiker = rep.Id; kumpulkan SEMUA sesi room target standalone (Pitfall 2).
            // ⚠ Key LOCKED ke Title + Category + Schedule.Date — WAJIB cocok dgn picker standalone Plan 03-T1.
            var repDate = rep.Schedule.Date;
            var targetSessions = await _context.AssessmentSessions
                .Where(s => s.LinkedGroupId == null
                         && s.AssessmentType == opposite
                         && s.Title == rep.Title && s.Category == rep.Category
                         && s.Schedule.Date == repDate)
                .ToListAsync();
            return (rep.Id, opposite, true, targetSessions, rep);
        }

        /// <summary>
        /// Phase 397 D-07 — Ringkasan PAIRING dry-run (NO write DB). Reuse <see cref="ResolveLinkContextAsync"/>
        /// (sumber kebenaran Kasus A/B sama dengan commit → preview == commit). Hitung berapa pekerja akan
        /// ter-pair ke sibling tipe-lawan, berapa unpaired (D-03), apakah menyentuh online (Kasus B, D-07),
        /// peringatan tanggal janggal (D-11, skip bila CompletedAt sibling null — Open Q 2), dan daftar
        /// anti-dobel-link (D-08 daftar LENGKAP). TIDAK memanggil SaveChangesAsync.
        /// </summary>
        public async Task<InjectPairingPreview> PreviewPairingAsync(
            int? linkTargetRepId, string injectAssessmentType,
            IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)
        {
            var result = new InjectPairingPreview();
            if (!linkTargetRepId.HasValue
                || (injectAssessmentType != AssessmentConstants.AssessmentType.PreTest
                    && injectAssessmentType != AssessmentConstants.AssessmentType.PostTest))
                return result;   // HasLink=false (D-04 skip)

            result.HasLink = true;
            var ctx = await ResolveLinkContextAsync(linkTargetRepId, injectAssessmentType);
            if (ctx.rep == null) return result;   // target tak valid → no pairing

            // Sesi tipe-lawan di grup target (Kasus A: by LinkedGroupId; Kasus B: SEMUA sesi room standalone).
            var targetSessions = await _context.AssessmentSessions.AsNoTracking()
                .Where(s => ctx.kasusB
                        ? ctx.targetSessions.Select(t => t.Id).Contains(s.Id)
                        : (s.LinkedGroupId == ctx.groupId && s.AssessmentType == ctx.opposite))
                .Select(s => new { s.UserId, s.CompletedAt, s.CreatedAt, s.Id })   // WR-02: CreatedAt+Id untuk pick deterministik
                .ToListAsync();

            var injectIdSet = injectUserIds.ToHashSet();
            // 1 sibling per UserId (online normalnya 1 tipe-lawan per user per grup).
            // WR-02 (398.1): OrderBy(CreatedAt).ThenBy(Id) — pick deterministik (oldest), byte-identik dgn commit (preview==commit; jaga DateWarn :697 konsisten).
            var siblingByUser = targetSessions
                .GroupBy(t => t.UserId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).First());

            result.Paired = injectUserIds.Count(uid => siblingByUser.ContainsKey(uid));   // LinkedSessionId terisi
            result.Unpaired = injectUserIds.Count - result.Paired;                        // D-03 sisi tunggal
            result.WillTouchOnline = ctx.kasusB;                                          // Kasus B banner (D-07)

            // D-11 date warn — bandingkan tanggal Pre vs Post; warn HANYA bila keduanya non-null & Pre > Post.
            //   inject Pre  → Pre date = injectCompletedAt ; Post date = sibling.CompletedAt
            //   inject Post → Post date = injectCompletedAt ; Pre date = sibling.CompletedAt
            bool injectIsPre = injectAssessmentType == AssessmentConstants.AssessmentType.PreTest;
            foreach (var uid in injectUserIds)
            {
                if (!siblingByUser.TryGetValue(uid, out var sib) || !sib.CompletedAt.HasValue) continue;
                var preDate = injectIsPre ? injectCompletedAt : sib.CompletedAt.Value;
                var postDate = injectIsPre ? sib.CompletedAt.Value : injectCompletedAt;
                if (preDate > postDate) { result.DateWarn = true; break; }
            }

            // D-08 anti-dobel-link — pekerja punya sibling TIPE-SAMA di grup target (daftar LENGKAP).
            var sameTypeUserIds = await _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.LinkedGroupId == ctx.groupId
                         && s.AssessmentType == injectAssessmentType
                         && injectIdSet.Contains(s.UserId))
                .Select(s => s.UserId).Distinct().ToListAsync();
            if (sameTypeUserIds.Count > 0)
            {
                var nipByUserId = await _context.Users.AsNoTracking()
                    .Where(u => sameTypeUserIds.Contains(u.Id) && u.NIP != null)
                    .ToDictionaryAsync(u => u.Id, u => u.NIP!);
                var labelType = injectIsPre ? "Pre-Test" : "Post-Test";
                foreach (var uid in sameTypeUserIds)
                {
                    var nip = nipByUserId.TryGetValue(uid, out var n) ? n : uid;
                    result.DoubleLinkErrors.Add(new InjectRowError
                    {
                        Nip = nip,
                        Message = $"Pekerja NIP {nip} sudah memiliki {labelType} di grup target — tidak dapat ditautkan dua kali."
                    });
                }
            }

            return result;   // NO SaveChanges — dry-run
        }

        /// <summary>
        /// Phase 397 D-12 — Lepas tautan grup inject (atomic + audit "LinkPrePostUndo"). Revert bidirectional
        /// LinkedSessionId + (heuristik konservatif Open Q 1) revert stiker LinkedGroupId Kasus B bila grup jadi
        /// single-type pasca-unlink. JANGAN cascade-delete (mirror pola atomic+audit DeleteAssessmentGroup —
        /// referensi shape saja). Skor/jawaban/status online TIDAK disentuh (T-397-04). IDOR guard: hanya sesi
        /// IsManualEntry yang di-load/revert sebagai sumber unlink (T-397-07).
        /// </summary>
        public async Task<InjectResult> UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)
        {
            var actorDisplay = string.IsNullOrWhiteSpace(actorName) ? actorUserId : actorName;
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Sesi INJECT di grup (IDOR guard: hanya manual-entry sebagai sumber unlink).
                var injectSessions = await _context.AssessmentSessions
                    .Where(s => s.LinkedGroupId == injectGroupId && s.IsManualEntry)
                    .ToListAsync();
                if (injectSessions.Count == 0)
                {
                    await tx.RollbackAsync();
                    return new InjectResult { Success = false, Message = "Grup inject tidak ditemukan." };
                }

                var mutated = new HashSet<int>();

                // 2. Revert bidirectional: untuk tiap sesi inject ber-sibling, null-kan sibling.LinkedSessionId (D-02).
                foreach (var inj in injectSessions)
                {
                    if (inj.LinkedSessionId.HasValue)
                    {
                        var sib = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == inj.LinkedSessionId.Value);
                        if (sib != null)
                        {
                            sib.LinkedSessionId = null;
                            if (!sib.IsManualEntry) mutated.Add(sib.Id);   // online write-back → audit
                        }
                    }
                }

                // 3. Lepas kolom link sesi inject sendiri.
                var injectIds = injectSessions.Select(s => s.Id).ToHashSet();
                foreach (var inj in injectSessions)
                {
                    inj.LinkedGroupId = null;
                    inj.LinkedSessionId = null;
                    mutated.Add(inj.Id);
                }

                // 4. Kasus B revert stiker (Open Q 1 — heuristik konservatif single-type): setelah sesi inject
                //    dilepas, bila grup hanya tersisa SATU tipe (single-type), stiker online tak lagi bermakna →
                //    revert LinkedGroupId pada sesi ONLINE tersisa. Skor/status TIDAK disentuh.
                var remaining = await _context.AssessmentSessions
                    .Where(s => s.LinkedGroupId == injectGroupId && !injectIds.Contains(s.Id))
                    .ToListAsync();
                if (remaining.Count > 0
                    && remaining.Select(r => r.AssessmentType).Distinct().Count() <= 1)
                {
                    foreach (var r in remaining.Where(r => !r.IsManualEntry))
                    {
                        r.LinkedGroupId = null;
                        mutated.Add(r.Id);
                    }
                }

                // 5. Audit "LinkPrePostUndo" per sesi dimutasi (in-tx _context.AuditLogs.Add — Pitfall 3).
                foreach (var sessionId in mutated)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = actorUserId,
                        ActorName = actorDisplay,
                        ActionType = "LinkPrePostUndo",   // ⚠ 15 char ≤ MaxLength(50)
                        Description = $"Tautan dilepas pada sesi {sessionId} (grup inject {injectGroupId}). Skor/jawaban/status tidak diubah.",
                        TargetId = sessionId,
                        TargetType = "AssessmentSession",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return new InjectResult { Success = true, Message = "Tautan dilepas." };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "UnlinkInjectGroup gagal untuk groupId={GroupId}", injectGroupId);
                return new InjectResult { Success = false, Message = "Gagal melepas tautan; dibatalkan." };
            }
        }

        // =====================================================================
        // Phase 395 INJ-09 — Auto-generate layer (static-pure, EF-free, reuse Phase 396 Import Excel)
        // =====================================================================

        /// <summary>
        /// Hasil auto-generate pola jawaban MC/MA dari skor target (Phase 395 INJ-09).
        /// <para><b>Answers</b>: MC/MA SAJA (benar/salah eksplisit). Essay TIDAK disentuh (D-08 — HC isi manual,
        /// hybrid). Caller (controller) menggabungkan essay-answers manual.</para>
        /// <para><b>CeilingPercent</b>: floor(Σ(ScoreValue MC/MA) / maxScore × 100) — maks yang dapat dicapai auto-gen
        /// (denominator selalu termasuk essay, sama dengan AssessmentScoreAggregator.cs:35/58).</para>
        /// <para><b>TargetReachable</b>: false bila targetPercent &gt; CeilingPercent (D-08.3 BLOCKING —
        /// JANGAN cap diam-diam; controller emit warning + arahkan switch input-asli).</para>
        /// </summary>
        public sealed record AutoGenResult(
            List<InjectAnswerSpec> Answers,
            int CeilingPercent,
            int MaxScoreIncludingEssay,
            bool TargetReachable);

        /// <summary>
        /// Phase 395 D-07 — Seed deterministik LINTAS-PROSES untuk variasi pola auto-gen per-pekerja yang
        /// reproducible (preview == commit). Seed = SHA-256 atas string kanonik (NIP + identitas room
        /// [Title + Category + CompletedAt tanggal-saja] + targetPercent).
        ///
        /// <para><b>KRITIS:</b> memakai <see cref="System.Security.Cryptography.SHA256"/>, BUKAN
        /// <c>string.GetHashCode()</c> — GetHashCode di-randomize per-proses di .NET Core+ → preview &amp; commit
        /// (request HTTP berbeda, mungkin proses berbeda) akan beda seed → pola beda → preview ≠ commit.</para>
        ///
        /// <para><b>CompletedAt</b> dipakai HANYA komponen tanggal (yyyy-MM-dd) agar tahan beda jam preview vs commit.
        /// Pemisah '' (unit separator) mencegah tabrakan concat (mis. "12"+"3" vs "1"+"23").</para>
        ///
        /// <para>SHA-256 di sini = NON-secret (hanya determinisme, BUKAN kontrol keamanan).</para>
        /// </summary>
        public static int ComputeAutoGenSeed(string nip, string title, string category, DateTime completedAt, int targetPercent)
        {
            const char SEP = ''; // unit separator — cegah tabrakan concat (encoding-safe escape)
            var canonical = string.Join(SEP,
                (nip ?? "").Trim(),
                (title ?? "").Trim(),
                (category ?? "").Trim(),
                completedAt.ToString("yyyy-MM-dd"),
                targetPercent.ToString());

            using var shaInit = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToInt32(shaInit.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical)), 0) & 0x7FFFFFFF;
        }

        /// <summary>
        /// Phase 395 INJ-09/D-06 — Translasi skor target → pola jawaban MC/MA eksplisit (benar/salah) sehingga,
        /// setelah di-grade pipeline (<see cref="AssessmentScoreAggregator.Compute"/>), skor = nilai TERKECIL yang
        /// &gt;= <paramref name="targetPercent"/> (jamin capaian ≥ target — bias jamin-lulus, D-06).
        ///
        /// <para>Deterministik via <paramref name="seed"/> (dari <see cref="ComputeAutoGenSeed"/>): WHICH soal dibuat
        /// benar bervariasi antar pekerja tapi reproducible (D-07). Urutan soal di-stabilkan internal
        /// (OrderBy Order ThenBy TempId) agar preview == commit.</para>
        ///
        /// <para><b>Essay TIDAK disentuh</b> (D-08): hanya MC/MA yang di-emit. Denominator (maxScore) tetap termasuk
        /// ScoreValue essay → bila bobot essay besar, ceiling MC/MA bisa &lt; target → <c>TargetReachable=false</c>
        /// (D-08.3 BLOCKING, jangan cap diam-diam).</para>
        ///
        /// <para><b>Re-cek floor()</b> SETELAH seleksi subset (formula identik Aggregator.cs:58 truncation int) —
        /// JANGAN percaya <c>ceil(target×N/100)</c> di mixed-weight (boundary off-by-one).</para>
        /// </summary>
        public static AutoGenResult BuildAutoGenAnswers(IReadOnlyList<InjectQuestionSpec> questions, int targetPercent, int seed)
        {
            questions ??= new List<InjectQuestionSpec>();

            // 1) Denominator = Σ ScoreValue SEMUA soal (termasuk essay — Aggregator.cs:35).
            int maxScore = questions.Sum(q => q.ScoreValue);

            // 2) Soal MC/MA saja, urutan STABIL (samakan persist InjectAssessmentService.cs:146).
            var flexQuestions = questions
                .Where(q => (q.QuestionType ?? "MultipleChoice") != "Essay")
                .OrderBy(q => q.Order).ThenBy(q => q.TempId)
                .ToList();

            // 3) Ceiling MC/MA-only = floor(Σ(ScoreValue MC/MA) / maxScore × 100). Formula SAMA Aggregator.cs:58.
            int mcMaPoints = flexQuestions.Sum(q => q.ScoreValue);
            int ceilingPercent = maxScore > 0 ? (int)((double)mcMaPoints / maxScore * 100) : 0;

            // Pre-scan: soal "forced-correct" = MC semua-opsi-benar ATAU soal 1-opsi → tak bisa dibuat salah.
            // Sisanya = "fleksibel" (bisa dibuat benar/salah secara terkendali).
            var forced = new List<InjectQuestionSpec>();
            var flexible = new List<InjectQuestionSpec>();
            foreach (var q in flexQuestions)
            {
                if (IsForcedCorrect(q)) forced.Add(q);
                else flexible.Add(q);
            }

            int forcedPoints = forced.Sum(q => q.ScoreValue);

            // 4) Target unreachable (D-08.3): bahkan semua MC/MA benar < target → best-effort = SEMUA benar,
            //    TargetReachable=false. Controller WAJIB BLOCKING + tak commit worker itu (jangan cap diam-diam).
            if (targetPercent > ceilingPercent)
            {
                var allCorrect = new List<InjectAnswerSpec>();
                foreach (var q in flexQuestions)
                    allCorrect.Add(MakeAnswer(q, makeCorrect: true, seed));
                return new AutoGenResult(allCorrect, ceilingPercent, maxScore, TargetReachable: false);
            }

            // 5) Pilih subset soal FLEKSIBEL yang dibuat benar (forced selalu benar → menyumbang poin).
            //    Acak urutan kandidat fleksibel dgn seed (variasi pola D-07).
            var rng = new Random(seed);
            // Greedy: prioritaskan ScoreValue besar agar cepat mencapai target dengan sedikit soal (smallest-such),
            // acak DALAM grup ScoreValue-sama agar tetap hit target tapi variasi antar pekerja.
            var orderedFlex = flexible
                .GroupBy(q => q.ScoreValue)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => g.OrderBy(_ => rng.Next()))
                .ToList();

            // Akumulasi soal benar sampai floor((forcedPoints + acc)/maxScore × 100) >= target.
            var chosenCorrect = new List<InjectQuestionSpec>(forced); // forced selalu benar
            int acc = forcedPoints;
            int i = 0;
            for (; i < orderedFlex.Count && FloorPercent(acc, maxScore) < targetPercent; i++)
            {
                chosenCorrect.Add(orderedFlex[i]);
                acc += orderedFlex[i].ScoreValue;
            }

            // 6) Smallest-such trim: coba buang soal fleksibel terkecil yang sudah dipilih bila floor tetap >= target.
            //    (Greedy bisa overshoot karena memilih ScoreValue besar dulu.)
            var trimmableAdded = orderedFlex.Take(i).OrderBy(q => q.ScoreValue).ToList(); // hanya yang baru ditambah
            foreach (var q in trimmableAdded)
            {
                if (FloorPercent(acc - q.ScoreValue, maxScore) >= targetPercent)
                {
                    chosenCorrect.Remove(q);
                    acc -= q.ScoreValue;
                }
            }

            // 7) WAJIB re-cek floor SETELAH seleksi (boundary off-by-one). Bila < target, tambah 1 soal benar & ulang.
            var chosenSet = chosenCorrect.Select(q => q.TempId).ToHashSet();
            while (FloorPercent(acc, maxScore) < targetPercent)
            {
                var next = orderedFlex.FirstOrDefault(q => !chosenSet.Contains(q.TempId));
                if (next == null) break; // tak ada lagi yang bisa ditambah (seharusnya tak terjadi krn target<=ceiling)
                chosenCorrect.Add(next);
                chosenSet.Add(next.TempId);
                acc += next.ScoreValue;
            }

            // 8) Konstruksi jawaban per soal MC/MA (benar bila terpilih, salah jika tidak; forced selalu benar).
            var answers = new List<InjectAnswerSpec>();
            foreach (var q in flexQuestions)
            {
                bool makeCorrect = chosenSet.Contains(q.TempId);
                answers.Add(MakeAnswer(q, makeCorrect, seed));
            }

            return new AutoGenResult(answers, ceilingPercent, maxScore, TargetReachable: true);
        }

        /// <summary>floor(total/max × 100) — formula truncation int IDENTIK AssessmentScoreAggregator.cs:58.</summary>
        private static int FloorPercent(int total, int max) => max > 0 ? (int)((double)total / max * 100) : 0;

        /// <summary>
        /// Soal "forced-correct" = tak bisa dibuat salah secara deterministik:
        /// MC dengan SEMUA opsi IsCorrect, ATAU soal apa pun dengan ≤1 opsi (terpaksa pilih satu-satunya/benar).
        /// (MA selalu bisa dibuat salah bila ada ≥2 opsi via subset/opsi-salah; lihat MakeAnswer.)
        /// </summary>
        private static bool IsForcedCorrect(InjectQuestionSpec q)
        {
            var type = q.QuestionType ?? "MultipleChoice";
            if (q.Options.Count <= 1) return true; // 1-opsi (atau 0) → terpaksa benar
            if (type == "MultipleChoice")
                return q.Options.All(o => o.IsCorrect); // semua opsi benar → MC tak bisa salah
            return false;
        }

        /// <summary>
        /// Bangun <see cref="InjectAnswerSpec"/> untuk satu soal MC/MA, benar/salah deterministik (Pattern 7).
        /// Forced-correct (semua benar / 1-opsi) selalu menghasilkan jawaban yang di-grade BENAR.
        /// </summary>
        private static InjectAnswerSpec MakeAnswer(InjectQuestionSpec q, bool makeCorrect, int seed)
        {
            var type = q.QuestionType ?? "MultipleChoice";
            var ordered = q.Options.OrderBy(o => o.TempId).ToList();
            var correctIds = ordered.Where(o => o.IsCorrect).Select(o => o.TempId).ToList();
            var wrongIds = ordered.Where(o => !o.IsCorrect).Select(o => o.TempId).ToList();

            var selected = new List<int>();

            if (type == "MultipleChoice")
            {
                if (makeCorrect || wrongIds.Count == 0)
                {
                    // benar: 1 opsi IsCorrect (deterministik: TempId terkecil). Bila tak ada (mustahil normal), ambil opsi pertama.
                    selected.Add(correctIds.Count > 0 ? correctIds[0] : (ordered.Count > 0 ? ordered[0].TempId : 0));
                }
                else
                {
                    // salah: 1 opsi !IsCorrect (deterministik: TempId terkecil).
                    selected.Add(wrongIds[0]);
                }
            }
            else // MultipleAnswer
            {
                if (makeCorrect)
                {
                    // benar: SEMUA opsi IsCorrect (SetEquals kunci). Bila tak ada opsi benar (degenerate), pilih semua opsi.
                    selected.AddRange(correctIds.Count > 0 ? correctIds : ordered.Select(o => o.TempId));
                }
                else
                {
                    if (wrongIds.Count > 0)
                    {
                        // salah: pilih {1 opsi salah} → SetEquals(correct) gagal.
                        selected.Add(wrongIds[0]);
                    }
                    else if (correctIds.Count > 1)
                    {
                        // semua opsi benar & >1 → proper-subset kunci (buang 1) → SetEquals gagal.
                        selected.AddRange(correctIds.Skip(1));
                    }
                    else
                    {
                        // degenerate (≤1 opsi benar, tak ada salah) — forced-correct sebenarnya; pilih kunci.
                        selected.AddRange(correctIds.Count > 0 ? correctIds : ordered.Select(o => o.TempId));
                    }
                }
            }

            return new InjectAnswerSpec { QuestionTempId = q.TempId, SelectedOptionTempIds = selected };
        }
    }
}
