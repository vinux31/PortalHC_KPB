using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>Request bypass tahun (spec §5). Mode in-memory saja — TIDAK dipersistensikan (W-05).</summary>
    public record BypassRequest(
        string CoacheeId, int SourceProtonTrackId, int TargetProtonTrackId,
        string TargetUnit, string? TargetCoachId, string Reason, string Mode,  // "CL-A"|"CL-B(a)"|"CL-B(b)"|"CL-C"
        int? DurationMinutes, string InitiatedById);

    public record BypassResult(bool Success, string Message, int? PendingId = null, bool ShowAttachPackageReminder = false);

    /// <summary>
    /// Input validasi pure §5 — semua nilai SUDAH di-resolve caller (tanpa DB di predikat).
    /// SourceComplete = allApproved deliverable tahun asal; SourceHasFinal = penanda ProtonFinalAssessment ada.
    /// </summary>
    public record BypassValidationInput(string Reason, int ActiveSourceTrackId, int TargetTrackId,
        int SourceTahun, int TargetTahun, string Mode, bool SourceComplete, bool SourceHasFinal);

    /// <summary>
    /// Phase 360 (PBYP-02) — predikat MURNI validasi bypass §5. Tanpa DbContext/IO (pola ProtonYearGate).
    /// E8 (tepat 1 assignment aktif) butuh DB → dicek di ExecuteInstantBypassAsync + BypassSaveAsync (B-04),
    /// BUKAN di sini.
    /// </summary>
    public static class BypassValidator
    {
        private static readonly string[] ValidModes = { "CL-A", "CL-B(a)", "CL-B(b)", "CL-C" };

        public static (bool Valid, string Message) Validate(BypassValidationInput v)
        {
            if (string.IsNullOrWhiteSpace(v.Reason))
                return (false, "Alasan wajib diisi.");

            if (!ValidModes.Contains(v.Mode))
                return (false, "Mode bypass tidak dikenal.");

            // E14: target tidak boleh sama dengan track aktif source.
            if (v.TargetTrackId == v.ActiveSourceTrackId)
                return (false, "Target sama dengan track aktif.");

            // D-B: |Δtahun| ≤ 1 (naik/turun/lateral 1 langkah).
            if (Math.Abs(v.SourceTahun - v.TargetTahun) > 1)
                return (false, "Lompat tahun maksimal 1 langkah.");

            // B-03 (spec §4/§5): CL-A WAJIB allApproved DAN penanda final ada.
            if (v.Mode == "CL-A")
            {
                if (!v.SourceComplete)
                    return (false, "Tahun asal belum komplit, gunakan CL-B.");
                if (!v.SourceHasFinal)
                    return (false, "Penanda Lulus tahun asal belum terbit, gunakan CL-B(a).");
            }

            // D-D: CL-B(a)/(b) hanya bila final tahun asal BELUM ada.
            if ((v.Mode == "CL-B(a)" || v.Mode == "CL-B(b)") && v.SourceHasFinal)
                return (false, "Final tahun asal sudah ada.");

            return (true, "");
        }
    }

    /// <summary>
    /// Phase 360 (D-08) — orkestrator Bypass Tahun. Eksekusi instan §5.1 (CL-A/B(a)/C) di sini;
    /// jalur pending CL-B(b) + confirm + cancel = plan 04/05. JANGAN inject layanan grading
    /// di ctor (DI WAJIB satu arah: grading → bypass; hindari circular — Open Q3).
    /// </summary>
    public class ProtonBypassService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProtonCompletionService _protonCompletionService;
        private readonly INotificationService _notificationService;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<ProtonBypassService> _logger;

        public ProtonBypassService(
            ApplicationDbContext context,
            ProtonCompletionService protonCompletionService,
            INotificationService notificationService,
            AuditLogService auditLog,
            ILogger<ProtonBypassService> logger)
        {
            _context = context;
            _protonCompletionService = protonCompletionService;
            _notificationService = notificationService;
            _auditLog = auditLog;
            _logger = logger;
        }

        /// <summary>
        /// §5.1 — pindah-instan all-or-nothing untuk mode CL-A / CL-B(a) / CL-C (D-09, 1 transaksi).
        /// URUTAN WAJIB (Pitfall 1): penanda source (EnsureAsync, resolve WHERE IsActive) SEBELUM
        /// deactivate source assignment. CL-B(b) TIDAK lewat sini (jalur pending, plan 04).
        /// </summary>
        public async Task<BypassResult> ExecuteInstantBypassAsync(BypassRequest req)
        {
            if (req.Mode == "CL-B(b)")
                return new BypassResult(false, "Mode CL-B(b) lewat jalur pending (BypassSave), bukan eksekusi instan.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // (0a) E8 (B-04): worker WAJIB punya TEPAT 1 assignment aktif.
                var activeAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == req.CoacheeId && a.IsActive)
                    .ToListAsync();
                if (activeAssignments.Count != 1)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, $"Worker punya {activeAssignments.Count} assignment aktif (harus tepat 1).");
                }
                var source = activeAssignments[0];
                if (source.ProtonTrackId != req.SourceProtonTrackId)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, "Assignment aktif worker tidak sesuai track asal yang dipilih.");
                }

                // (0b) Resolve flag + validasi pure §5.
                var sourceTrack = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == req.SourceProtonTrackId);
                var targetTrack = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == req.TargetProtonTrackId);
                if (sourceTrack == null || targetTrack == null)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, "Track asal/tujuan tidak ditemukan.");
                }
                var sourceStatuses = await _context.ProtonDeliverableProgresses
                    .Where(p => p.ProtonTrackAssignmentId == source.Id)
                    .Select(p => p.Status)
                    .ToListAsync();
                bool sourceComplete = sourceStatuses.Count > 0 && sourceStatuses.All(s => s == "Approved");
                bool sourceHasFinal = await _context.ProtonFinalAssessments
                    .AnyAsync(fa => fa.ProtonTrackAssignmentId == source.Id);

                var (valid, message) = BypassValidator.Validate(new BypassValidationInput(
                    req.Reason, source.ProtonTrackId, req.TargetProtonTrackId,
                    sourceTrack.Urutan, targetTrack.Urutan, req.Mode, sourceComplete, sourceHasFinal));
                if (!valid)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, message);
                }

                var hcUser = await _context.Users
                    .Where(u => u.Id == req.InitiatedById)
                    .Select(u => new { u.FullName, u.UserName })
                    .FirstOrDefaultAsync();
                var hcName = !string.IsNullOrWhiteSpace(hcUser?.FullName)
                    ? hcUser!.FullName
                    : (hcUser?.UserName ?? req.InitiatedById);

                // (1) [tutup tahun asal] — SEBELUM deactivate (Pitfall 1: EnsureAsync resolve WHERE IsActive).
                //     CL-A: penanda sudah ada (B-03 menjamin SourceHasFinal) → no-op.
                //     CL-C: tinggalkan tanpa nilai → no-op.
                if (req.Mode == "CL-B(a)")
                {
                    // Force-approve SEMUA progress source (pola OverrideSave) + history per progress (D-13).
                    var progresses = await _context.ProtonDeliverableProgresses
                        .Where(p => p.ProtonTrackAssignmentId == source.Id)
                        .ToListAsync();
                    foreach (var p in progresses)
                    {
                        p.Status = "Approved";
                        p.ApprovedAt = DateTime.UtcNow;
                        p.ApprovedById = req.InitiatedById;
                        p.HCApprovalStatus = "Reviewed";
                        _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                        {
                            ProtonDeliverableProgressId = p.Id,
                            StatusType = "Bypassed-AutoApprove",
                            ActorId = req.InitiatedById,
                            ActorName = hcName,
                            ActorRole = "HC",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                    await _protonCompletionService.EnsureAsync(
                        req.CoacheeId, req.SourceProtonTrackId, req.InitiatedById, "Bypass", req.Reason);
                }

                // (2) [cancel exam aktif S] (E5/D-14): "Dibatalkan" — nilai non-completable, reversible,
                //     tak bentrok Status existing {Open, Upcoming, Completed, Menunggu Penilaian}.
                var openExams = await _context.AssessmentSessions
                    .Where(s => s.Category == "Assessment Proton" && s.ProtonTrackId == req.SourceProtonTrackId
                             && s.UserId == req.CoacheeId && s.Status != "Completed")
                    .ToListAsync();
                foreach (var exam in openExams) exam.Status = "Dibatalkan";

                // (3) [deactivate assignment S].
                source.IsActive = false;
                source.DeactivatedAt = DateTime.UtcNow;

                // (4) [aktifkan target T] — stempel Origin="Bypass" (D-04, exempt gate cross-year).
                var newAssignment = new ProtonTrackAssignment
                {
                    CoacheeId = req.CoacheeId,
                    ProtonTrackId = req.TargetProtonTrackId,
                    IsActive = true,
                    AssignedById = req.InitiatedById,
                    AssignedAt = DateTime.UtcNow,
                    Origin = "Bypass"
                };
                _context.ProtonTrackAssignments.Add(newAssignment);
                await _context.SaveChangesAsync(); // flush → newAssignment.Id

                // (5) [bootstrap deliverable T] — unit dari FORM (PBYP-05/Pitfall 3); guard anti-dobel B-06 di helper.
                var bootstrapWarnings = await Helpers.ProtonDeliverableBootstrap.CreateProgressAsync(
                    _context, newAssignment.Id, req.TargetProtonTrackId, req.CoacheeId, req.TargetUnit);

                // (6) [coach D-16 + D-16b + E15].
                var mappingLama = await _context.CoachCoacheeMappings
                    .FirstOrDefaultAsync(m => m.CoacheeId == req.CoacheeId && m.IsActive);
                if (!string.IsNullOrWhiteSpace(req.TargetCoachId))
                {
                    // GANTI COACH (E15 filtered-unique): deactivate lama DULU + flush, baru create.
                    if (mappingLama != null)
                    {
                        mappingLama.IsActive = false;
                        mappingLama.EndDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    _context.CoachCoacheeMappings.Add(new CoachCoacheeMapping
                    {
                        CoacheeId = req.CoacheeId,
                        CoachId = req.TargetCoachId!,
                        AssignmentUnit = req.TargetUnit,
                        // W-13/I-04: StartDate non-nullable + Section warisi (default(DateTime) merusak view).
                        StartDate = DateTime.UtcNow,
                        AssignmentSection = mappingLama?.AssignmentSection,
                        IsActive = true
                    });
                }
                else if (mappingLama != null
                         && (mappingLama.AssignmentUnit ?? "").Trim() != req.TargetUnit.Trim())
                {
                    // D-16b (review #2) — KEEP COACH + GANTI UNIT: jangan deactivate/recreate (D-16, E15 aman).
                    // Update unit mapping supaya gate 100% (resolve unit dari active mapping) konsisten
                    // dengan bootstrap unit-FORM (W-04/W-08/W-10).
                    mappingLama.AssignmentUnit = req.TargetUnit;
                }
                // else: keep coach + unit sama → JANGAN sentuh mapping (D-16 murni).

                // (7) [audit].
                await _auditLog.LogAsync(req.InitiatedById, hcName, "ProtonBypass",
                    $"Bypass {req.Mode} {req.CoacheeId}: track {req.SourceProtonTrackId}→{req.TargetProtonTrackId}, unit {req.TargetUnit}. Alasan: {req.Reason}",
                    targetType: "PendingProtonBypass");

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var warnSuffix = bootstrapWarnings.Any() ? $" Catatan: {string.Join(" ", bootstrapWarnings)}" : "";
                return new BypassResult(true, $"Bypass berhasil.{warnSuffix}");
            }
            catch (DbUpdateException dbEx) when (
                dbEx.InnerException?.Message.Contains("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique") == true
                || dbEx.InnerException?.Message.Contains("2601") == true
                || dbEx.InnerException?.Message.Contains("2627") == true)
            {
                _logger.LogWarning(dbEx, "Bypass coach unique-index violation Coachee={CoacheeId}", req.CoacheeId);
                await tx.RollbackAsync();
                return new BypassResult(false, "Coachee sudah punya coach aktif. Nonaktifkan mapping lama dulu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteInstantBypassAsync gagal Coachee={CoacheeId}", req.CoacheeId);
                await tx.RollbackAsync();
                return new BypassResult(false, "Gagal eksekusi bypass. Operasi dibatalkan."); // D6: tanpa ex.Message
            }
        }

        /// <summary>
        /// Entry point bypass SEMUA mode (spec §5): E8 (B-04) + validasi pure + D-10, lalu dispatch —
        /// CL-B(b) → jalur pending (ExecutePendingBypassAsync §5.2), lainnya → eksekusi instan §5.1.
        /// </summary>
        public async Task<BypassResult> BypassSaveAsync(BypassRequest req)
        {
            // E8 (B-04): worker WAJIB punya TEPAT 1 assignment aktif — dicek di entry SEBELUM dispatch.
            // Tanpa ini worker dobel-assignment bisa bikin pending CL-B(b) yang saat Confirm hanya
            // cek source masih aktif → berakhir 2 assignment aktif, mematahkan jaminan E8.
            var activeAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == req.CoacheeId && a.IsActive)
                .ToListAsync();
            int activeCount = activeAssignments.Count;
            if (activeCount != 1)
                return new BypassResult(false, $"Worker punya {activeCount} assignment aktif (harus tepat 1).");
            var source = activeAssignments[0];
            if (source.ProtonTrackId != req.SourceProtonTrackId)
                return new BypassResult(false, "Assignment aktif worker tidak sesuai track asal yang dipilih.");

            // Resolve flag → validasi pure §5 (validator B-03 menjamin CL-A wajib SourceComplete+SourceHasFinal).
            var sourceTrack = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == req.SourceProtonTrackId);
            var targetTrack = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == req.TargetProtonTrackId);
            if (sourceTrack == null || targetTrack == null)
                return new BypassResult(false, "Track asal/tujuan tidak ditemukan.");
            var sourceStatuses = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == source.Id)
                .Select(p => p.Status)
                .ToListAsync();
            bool sourceComplete = sourceStatuses.Count > 0 && sourceStatuses.All(s => s == "Approved");
            bool sourceHasFinal = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.ProtonTrackAssignmentId == source.Id);

            var (valid, message) = BypassValidator.Validate(new BypassValidationInput(
                req.Reason, source.ProtonTrackId, req.TargetProtonTrackId,
                sourceTrack.Urutan, targetTrack.Urutan, req.Mode, sourceComplete, sourceHasFinal));
            if (!valid)
                return new BypassResult(false, message);

            // D-10: BLOK DOBEL PENDING — semua mode, SEBELUM eksekusi.
            bool hasActivePending = await _context.PendingProtonBypasses
                .AnyAsync(p => p.CoacheeId == req.CoacheeId && (p.Status == "Menunggu" || p.Status == "Siap"));
            if (hasActivePending)
                return new BypassResult(false, "Worker sudah punya rencana bypass aktif. Selesaikan/batalkan dulu.");

            return req.Mode == "CL-B(b)"
                ? await ExecutePendingBypassAsync(req)
                : await ExecuteInstantBypassAsync(req);
        }

        /// <summary>
        /// §5.2 — jalur pending CL-B(b) (buat-tunggu): force-approve deliverable source (D-13, TANPA
        /// terbit penanda — penanda terbit oleh GradingService saat exam bare lulus, Origin="Exam"),
        /// buat AssessmentSession BARE tanpa paket (D-01), insert PendingProtonBypass "Menunggu".
        /// 1 transaksi all-or-nothing (D-09).
        /// </summary>
        public async Task<BypassResult> ExecutePendingBypassAsync(BypassRequest req)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var source = await _context.ProtonTrackAssignments
                    .FirstOrDefaultAsync(a => a.CoacheeId == req.CoacheeId && a.IsActive
                                           && a.ProtonTrackId == req.SourceProtonTrackId);
                if (source == null)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, "Assignment aktif worker tidak sesuai track asal yang dipilih.");
                }

                var sourceTrack = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == req.SourceProtonTrackId);
                if (sourceTrack == null)
                {
                    await tx.RollbackAsync();
                    return new BypassResult(false, "Track asal tidak ditemukan.");
                }

                var hcUser = await _context.Users
                    .Where(u => u.Id == req.InitiatedById)
                    .Select(u => new { u.FullName, u.UserName })
                    .FirstOrDefaultAsync();
                var hcName = !string.IsNullOrWhiteSpace(hcUser?.FullName)
                    ? hcUser!.FullName
                    : (hcUser?.UserName ?? req.InitiatedById);
                var workerName = await _context.Users
                    .Where(u => u.Id == req.CoacheeId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? req.CoacheeId;

                // [force-approve deliverable source] (D-13) — pola sama CL-B(a) §5.1.
                var progresses = await _context.ProtonDeliverableProgresses
                    .Where(p => p.ProtonTrackAssignmentId == source.Id)
                    .ToListAsync();
                foreach (var p in progresses)
                {
                    p.Status = "Approved";
                    p.ApprovedAt = DateTime.UtcNow;
                    p.ApprovedById = req.InitiatedById;
                    p.HCApprovalStatus = "Reviewed";
                    _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                    {
                        ProtonDeliverableProgressId = p.Id,
                        StatusType = "Bypassed-AutoApprove",
                        ActorId = req.InitiatedById,
                        ActorName = hcName,
                        ActorRole = "HC",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // [buat AssessmentSession bare] — TANPA paket soal + TANPA assignment paket (D-01);
                // HC lampirkan paket via Kelola Assessment (D-02).
                var bareSession = new AssessmentSession
                {
                    UserId = req.CoacheeId,      // B-05/W-06: per-worker key — tanpa ini worker tak pernah lihat exam
                    AssessmentType = "Standard", // B-05: kolom DB NOT NULL (ISS-04) — NULL → SqlException 515
                    Category = "Assessment Proton",
                    ProtonTrackId = req.SourceProtonTrackId,
                    TahunKe = $"Tahun {sourceTrack.Urutan}",
                    Title = $"Bypass Exam Tahun {sourceTrack.Urutan} — {workerName}",
                    Schedule = DateTime.Now,
                    DurationMinutes = req.DurationMinutes ?? 60,
                    PassPercentage = 70,
                    Status = "Upcoming",
                    GenerateCertificate = true,
                    CreatedBy = req.InitiatedById,
                    IsTokenRequired = false
                };
                _context.AssessmentSessions.Add(bareSession);
                await _context.SaveChangesAsync(); // flush → bareSession.Id

                // [insert pending] — TANPA field Mode (W-05): jalur ini selalu CL-B(b) by construction.
                var pending = new PendingProtonBypass
                {
                    CoacheeId = req.CoacheeId,
                    SourceProtonTrackId = req.SourceProtonTrackId,
                    TargetProtonTrackId = req.TargetProtonTrackId,
                    TargetUnit = req.TargetUnit,
                    TargetCoachId = req.TargetCoachId,
                    Reason = req.Reason,
                    LinkedAssessmentSessionId = bareSession.Id,
                    Status = "Menunggu",
                    InitiatedById = req.InitiatedById,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PendingProtonBypasses.Add(pending);

                await _auditLog.LogAsync(req.InitiatedById, hcName, "ProtonBypass",
                    $"Bypass CL-B(b) pending {req.CoacheeId}: track {req.SourceProtonTrackId}→{req.TargetProtonTrackId}, unit {req.TargetUnit}, exam session #{bareSession.Id}. Alasan: {req.Reason}",
                    targetType: "PendingProtonBypass");

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new BypassResult(true,
                    "Sesi exam dibuat — lampirkan paket soal di Kelola Assessment sebelum worker bisa ujian.",
                    PendingId: pending.Id, ShowAttachPackageReminder: true); // D-02 reminder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecutePendingBypassAsync gagal Coachee={CoacheeId}", req.CoacheeId);
                await tx.RollbackAsync();
                return new BypassResult(false, "Gagal membuat rencana bypass. Operasi dibatalkan."); // D6: tanpa ex.Message
            }
        }
    }
}
