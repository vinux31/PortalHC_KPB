using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Phase 367 — Cascade engine penghapusan record worker dari tab Input Records.
    /// ORKESTRASI (bukan primitif baru): traversal renewal lintas TrainingRecords↔AssessmentSessions
    /// (kolom RenewsTrainingId/RenewsSessionId) dengan cycle guard. Plan 01 = bagian READ-ONLY
    /// (CascadeNode, CollectCascadeIds, BuildPreviewAsync, mirror heuristik). Plan 02 menambah ExecuteAsync (mutasi).
    ///
    /// Invariant KRITIS: preview set == execute set — keduanya pakai <see cref="CollectCascadeIds"/> yang SAMA,
    /// jadi admin lihat X di preview, hapus X persis (memperbaiki kasus Rino #3: turunan renewal tak ikut terhapus).
    ///
    /// Catatan visibilitas: CollectCascadeIds/FindMirrorCandidates public (konvensi proyek — reachable dari
    /// HcPortal.Tests tanpa InternalsVisibleTo, sama dengan CMPController:3969).
    /// </summary>
    public class RecordCascadeDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecordCascadeDeleteService> _logger;

        public RecordCascadeDeleteService(
            ApplicationDbContext context,
            ILogger<RecordCascadeDeleteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Node korban cascade untuk preview. Type ∈ {"session","training"}.
        /// IsMirrorCandidate=true → kandidat mirror legacy (#15) — read-only di preview, default tercentang
        /// (opt-out), BUKAN turunan renewal yang ter-traverse.
        /// </summary>
        public record CascadeNode(string Type, int Id, string Title, DateTime Date, string OwnerName, bool IsRoot, bool IsMirrorCandidate);

        /// <summary>
        /// Traversal BFS BERSAMA (dipakai preview DAN execute — invariant preview==execute) lintas tabel
        /// via Renews*Id, dengan cycle guard (HashSet visited). Mengembalikan (Type,Id) terurut BFS, root dulu.
        /// GOTCHA (Pitfall 2): node session → anak via RenewsSessionId; node training → anak via RenewsTrainingId.
        /// </summary>
        public async Task<List<(string Type, int Id)>> CollectCascadeIds(string rootType, int rootId)
        {
            if (rootType != "session" && rootType != "training")
                throw new ArgumentException($"rootType harus 'session' atau 'training', bukan '{rootType}'.", nameof(rootType));

            HashSet<(string, int)> visited = new();
            var ordered = new List<(string, int)>();
            var queue = new Queue<(string Type, int Id)>();
            queue.Enqueue((rootType, rootId));
            visited.Add((rootType, rootId));
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                ordered.Add(node);

                List<int> childTrainingIds;
                List<int> childSessionIds;
                if (node.Type == "session")
                {
                    childTrainingIds = await _context.TrainingRecords
                        .Where(t => t.RenewsSessionId == node.Id).Select(t => t.Id).ToListAsync();
                    childSessionIds = await _context.AssessmentSessions
                        .Where(a => a.RenewsSessionId == node.Id).Select(a => a.Id).ToListAsync();
                }
                else // "training"
                {
                    childTrainingIds = await _context.TrainingRecords
                        .Where(t => t.RenewsTrainingId == node.Id).Select(t => t.Id).ToListAsync();
                    childSessionIds = await _context.AssessmentSessions
                        .Where(a => a.RenewsTrainingId == node.Id).Select(a => a.Id).ToListAsync();
                }

                foreach (var tid in childTrainingIds)
                    if (visited.Add(("training", tid))) queue.Enqueue(("training", tid));
                foreach (var sid in childSessionIds)
                    if (visited.Add(("session", sid))) queue.Enqueue(("session", sid));
            }
            return ordered;
        }
    }
}
