using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers;

/// <summary>
/// Phase 424 GRDF-03 / GRDF-01 — single source of truth untuk pemasangan Pre→Post PER-PESERTA.
/// Menggantikan tiga jalur pairing divergen menjadi satu, semua TERFILTER UserId:
///   - CMPController.cs:292-297 (display gain-score) — DULU TANPA filter UserId (FLOW-01 root) → kini WAJIB UserId.
///   - CMPController.cs:3505-3523 (GetGainScoreData) — sudah filter UserId.
///   - CMPController.cs:2404-2413 (LinkedSessionId) — sudah filter UserId.
/// Dikonsumsi gate GRDF-01 (StartExam, Plan 02): Post tidak boleh dimulai bila pasangan Pre belum Completed.
/// Pemasangan HANYA via link eksplisit (LinkedSessionId → fallback LinkedGroupId), TIDAK PERNAH via pola judul (D-08, GRDF-04).
/// Standard / Pre / Post-tanpa-link (orphan) → null (pass-through, D-02).
/// </summary>
public static class PrePostPairing
{
    public static async Task<AssessmentSession?> FindPairedPreAsync(ApplicationDbContext ctx, AssessmentSession post)
    {
        if (post.AssessmentType != "PostTest") return null;                 // Standard/Pre → bukan target gate (D-02 pass-through)

        if (post.LinkedSessionId.HasValue)                                  // (1) kanonik: Post→Pre eksplisit
            return await ctx.AssessmentSessions.FirstOrDefaultAsync(s =>
                s.Id == post.LinkedSessionId.Value
                && s.UserId == post.UserId
                && s.AssessmentType == "PreTest");

        if (post.LinkedGroupId.HasValue)                                    // (2) fallback: group + UserId + type
            return await ctx.AssessmentSessions.FirstOrDefaultAsync(s =>
                s.LinkedGroupId == post.LinkedGroupId.Value
                && s.UserId == post.UserId
                && s.AssessmentType == "PreTest");

        return null;                                                        // no link → orphan, pass-through (D-02)
    }
}
