using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 360 (PBYP-05) — bootstrap deliverable progress untuk sebuah ProtonTrackAssignment
    /// dengan unit EKSPLISIT dari caller (form bypass / resolve mapping), BUKAN resolve sendiri
    /// dari active mapping (Pitfall 3). Diekstrak dari CoachMappingController.AutoCreateProgressForAssignment.
    /// </summary>
    public static class ProtonDeliverableBootstrap
    {
        /// <summary>
        /// Bootstrap ProtonDeliverableProgress (Status="Pending") + initial DeliverableStatusHistory
        /// untuk assignment, FILTER unit-deliverable pakai <paramref name="resolvedUnit"/> eksplisit
        /// (.Trim() 2-sisi, identik gate 100% AssessmentAdminController).
        /// EXCLUDE deliverableIds yang SUDAH punya progress untuk coachee (B-06 guard anti-dobel —
        /// tanpa ini, CL-C turun ke track/unit yang pernah dijalani membuat count progress 2N dan
        /// CoacheeEligibilityCalculator.IsEligiblePerUnit (count != expectedCount) false selamanya).
        /// Return list warning (unit kosong / track 0 deliverable / semua sudah ter-bootstrap).
        /// Caller WAJIB memanggil di dalam transaksi (method ini SaveChangesAsync internal).
        /// </summary>
        public static async Task<List<string>> CreateProgressAsync(
            ApplicationDbContext context, int assignmentId, int protonTrackId,
            string coacheeId, string resolvedUnit)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(resolvedUnit))
            {
                warnings.Add($"resolvedUnit kosong untuk coachee {coacheeId} — progress tidak dibuat.");
                return warnings;
            }

            var deliverableIds = await context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
                         && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
                .Select(d => d.Id)
                .ToListAsync();

            if (!deliverableIds.Any())
            {
                var trackName = await context.ProtonTracks
                    .Where(t => t.Id == protonTrackId)
                    .Select(t => t.DisplayName)
                    .FirstOrDefaultAsync() ?? protonTrackId.ToString();
                warnings.Add($"Tidak ada deliverable untuk unit {resolvedUnit} di track {trackName}.");
                return warnings;
            }

            // B-06 guard anti-dobel: skip deliverable yang sudah punya progress untuk coachee ini
            // (track pernah dijalani). D-E tetap dihormati — assignment baru tetap fresh, hanya
            // progress duplikat yang di-skip supaya counting gate eligibility konsisten.
            var existingDeliverableIds = await context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == coacheeId)
                .Select(p => p.ProtonDeliverableId)
                .ToListAsync();
            var toCreate = deliverableIds.Where(dId => !existingDeliverableIds.Contains(dId)).ToList();

            if (!toCreate.Any())
            {
                warnings.Add($"Semua deliverable unit {resolvedUnit} sudah ter-bootstrap untuk coachee — skip duplikat.");
                return warnings;
            }

            var progresses = toCreate.Select(dId => new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = dId,
                ProtonTrackAssignmentId = assignmentId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            context.ProtonDeliverableProgresses.AddRange(progresses);
            await context.SaveChangesAsync(); // flush to get IDs for StatusHistory

            // D-17: Insert initial "Pending" StatusHistory for each new progress
            foreach (var p in progresses)
            {
                context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                {
                    ProtonDeliverableProgressId = p.Id,
                    StatusType = "Pending",
                    ActorId = "system",
                    ActorName = "System",
                    ActorRole = "System",
                    Timestamp = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            return warnings;
        }
    }
}
