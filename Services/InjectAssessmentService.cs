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
            // TODO Plan 02 Task 2/3: orkestrasi tx + insert + grade + finalize + backdate + cert + audit.
            await Task.CompletedTask;
            return new InjectResult { Success = false, Message = "Belum diimplementasikan (Plan 02)." };
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
