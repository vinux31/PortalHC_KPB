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
            // TODO Plan 02: pre-flight (D-03) → dedup (D-01/D-02) → tx (D-04) → per-worker insert+grade+finalize+backdate+audit.
            await Task.CompletedTask;
            return new InjectResult { Success = false, Message = "Belum diimplementasikan (Plan 02)." };
        }
    }
}
