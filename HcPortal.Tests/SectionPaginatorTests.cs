using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 417 PAG-01/02/03 — pure unit tests untuk <see cref="SectionPaginator.ComputePages"/> +
/// <see cref="SectionPaginator.ClampResumePage"/>. No DB, no fixture, no [Trait("Category","Integration")] —
/// fungsi murni, list <see cref="ExamQuestionItem"/> dibangun in-memory (pola identik
/// <see cref="SectionScopedShuffleTests"/>).
///
/// Membuktikan: flow 10/halaman (PAG-01), golden no-Section backward-compat (PAG-01), StartNewPage
/// page-break sebelum Section (PAG-02), Section panjang auto-split + IsSectionContinuation (PAG-02),
/// grup "Lainnya" tak paksa break (PAG-02), resume clamp/fallback (PAG-03), mobile perPage=5 (cross).
/// </summary>
public class SectionPaginatorTests
{
    // Bangun List<ExamQuestionItem> urut (DisplayNumber 1..N). qs: (sectionNumber, startNewPage).
    // Soal "Lainnya" pakai sectionNumber=null.
    private static List<ExamQuestionItem> Build(params (int? sectionNumber, bool startNewPage)[] qs)
    {
        var list = new List<ExamQuestionItem>();
        int n = 1;
        foreach (var (sec, snp) in qs)
        {
            list.Add(new ExamQuestionItem
            {
                QuestionId = n,
                DisplayNumber = n,
                SectionNumber = sec,
                SectionName = sec == null ? null : $"Section {sec}",
                SectionStartNewPage = snp,
            });
            n++;
        }
        return list;
    }

    // Helper: N soal pada satu Section (atau Lainnya), snp seragam.
    private static List<ExamQuestionItem> BuildSameSection(int count, int? sectionNumber, bool startNewPage)
    {
        var qs = new (int?, bool)[count];
        for (int i = 0; i < count; i++) qs[i] = (sectionNumber, startNewPage);
        return Build(qs);
    }

    [Fact] // PAG-01
    public void PageNumber_FlowsTenPerPage()
    {
        // 25 soal Section 1 (StartNewPage=false), perPage=10 → 10/10/5 di page 0/1/2.
        var list = BuildSameSection(25, sectionNumber: 1, startNewPage: false);

        SectionPaginator.ComputePages(list, perPage: 10);

        Assert.Equal(2, list.Max(q => q.PageNumber));
        Assert.Equal(10, list.Count(q => q.PageNumber == 0));
        Assert.Equal(10, list.Count(q => q.PageNumber == 1));
        Assert.Equal(5, list.Count(q => q.PageNumber == 2));
    }

    [Fact] // PAG-01 GOLDEN backward-compat
    public void NoSection_IdenticalToFlatBaseline()
    {
        // Golden backward-compat — kalau merah perbaiki ComputePages, JANGAN ubah assert ini.
        // 23 soal SEMUA SectionNumber=null, perPage=10 → PageNumber == (DisplayNumber-1)/10 (index/perPage lama).
        var list = BuildSameSection(23, sectionNumber: null, startNewPage: false);

        SectionPaginator.ComputePages(list, perPage: 10);

        Assert.All(list, q => Assert.Equal((q.DisplayNumber - 1) / 10, q.PageNumber));
    }

    [Fact] // PAG-02
    public void StartNewPage_BreaksBeforeSection()
    {
        // Section 1 (3 soal, snp=false) lalu Section 2 (2 soal, snp=true), perPage=10.
        // Section2 mulai halaman baru meski page0 belum penuh.
        var list = Build(
            (1, false), (1, false), (1, false),   // 3 soal Section1
            (2, true), (2, true));                  // 2 soal Section2 ber-StartNewPage

        SectionPaginator.ComputePages(list, perPage: 10);

        // 3 soal Section1 di page 0
        Assert.All(list.Take(3), q => Assert.Equal(0, q.PageNumber));
        // soal pertama Section2 di page 1 + IsSectionStart
        var firstSec2 = list[3];
        Assert.Equal(1, firstSec2.PageNumber);
        Assert.True(firstSec2.IsSectionStart);
    }

    [Fact] // PAG-02 + continuation
    public void LongSection_AutoSplitsTenPerPage()
    {
        // Section 1 = 12 soal (snp=false), perPage=10 → soal #11 auto-split ke page1 sebagai continuation.
        var list = BuildSameSection(12, sectionNumber: 1, startNewPage: false);

        SectionPaginator.ComputePages(list, perPage: 10);

        var q1 = list.Single(q => q.DisplayNumber == 1);
        Assert.True(q1.IsSectionStart);
        Assert.False(q1.IsSectionContinuation);

        var q11 = list.Single(q => q.DisplayNumber == 11);
        Assert.Equal(1, q11.PageNumber);
        Assert.True(q11.IsSectionContinuation);
        Assert.False(q11.IsSectionStart);
    }

    [Fact] // PAG-02
    public void LainnyaGroup_NoForcedBreak()
    {
        // Section 1 (5 soal, snp=false) lalu Lainnya (3 soal null, snp=false), perPage=10.
        // Semua 8 soal di page 0 (Lainnya tak paksa break, page belum penuh).
        var list = Build(
            (1, false), (1, false), (1, false), (1, false), (1, false),  // 5 soal Section1
            (null, false), (null, false), (null, false));                 // 3 soal Lainnya

        SectionPaginator.ComputePages(list, perPage: 10);

        Assert.All(list, q => Assert.Equal(0, q.PageNumber));
        // soal Lainnya pertama (index 5) = section berubah 1→null → IsSectionStart
        Assert.True(list[5].IsSectionStart);
    }

    [Fact] // PAG-02 — kombinasi StartNewPage + auto-split (skenario produksi: Section B StartNewPage + panjang)
    public void StartNewPageSection_LongerThanPerPage_BreaksThenAutoSplits()
    {
        // Section 1 (3 soal, snp=false) lalu Section 2 (12 soal, snp=TRUE), perPage=10.
        // PAG-02 menggabungkan DUA perilaku yang sebelumnya hanya diuji terpisah:
        //   (a) Section 2 ber-StartNewPage → page-break SEBELUM soal pertamanya (walau page0 belum penuh),
        //   (b) Section 2 panjang (>perPage) → auto-split per-10 DI DALAM Section, soal ke-11 = continuation.
        var list = Build(
            (1, false), (1, false), (1, false),                        // 3 soal Section1 → page 0
            (2, true), (2, true), (2, true), (2, true), (2, true),      // Section2 soal 1-5
            (2, true), (2, true), (2, true), (2, true), (2, true),      // Section2 soal 6-10  (10 soal di page 1)
            (2, true), (2, true));                                       // Section2 soal 11-12 → page 2 (continuation)

        SectionPaginator.ComputePages(list, perPage: 10);

        // (a) page-break: 3 soal Section1 di page 0; soal pertama Section2 di page 1 (bukan menyambung page 0).
        Assert.All(list.Take(3), q => Assert.Equal(0, q.PageNumber));
        var firstSec2 = list[3];                                         // DisplayNumber 4 = soal pertama Section2
        Assert.Equal(1, firstSec2.PageNumber);
        Assert.True(firstSec2.IsSectionStart);
        Assert.False(firstSec2.IsSectionContinuation);

        // 10 soal Section2 (soal 1-10) penuh di page 1.
        Assert.Equal(10, list.Count(q => q.PageNumber == 1));

        // (b) auto-split: soal ke-11 Section2 (DisplayNumber 14) di page 2 = continuation (bukan section-start).
        var q11ofSec2 = list.Single(q => q.DisplayNumber == 14);
        Assert.Equal(2, q11ofSec2.PageNumber);
        Assert.True(q11ofSec2.IsSectionContinuation);
        Assert.False(q11ofSec2.IsSectionStart);
        Assert.Equal(2, list.Max(q => q.PageNumber));
    }

    [Fact] // PAG-01/02/03 — determinisme/idempotensi (must_haves Plan 01 Task 2)
    public void ComputePages_IsIdempotent()
    {
        // Plan 01 Task 2 menjanjikan ComputePages "deterministik, NON-RNG, idempotent (panggil 2x → hasil sama)".
        // Pin invarian: dua pemanggilan berturut atas list yang sama menghasilkan PageNumber/IsSectionStart/
        // IsSectionContinuation IDENTIK (tak akumulasi state, tak bergeser saat dipanggil ulang saat re-render).
        var list = Build(
            (1, false), (1, false), (1, false), (1, false), (1, false),  // Section1
            (2, true), (2, true),                                        // Section2 (StartNewPage)
            (null, false), (null, false));                                // Lainnya

        SectionPaginator.ComputePages(list, perPage: 3);
        var snapshot = list
            .Select(q => (q.PageNumber, q.IsSectionStart, q.IsSectionContinuation))
            .ToList();

        // Panggil kedua kali atas LIST YANG SAMA (sudah ber-PageNumber dari panggilan pertama).
        SectionPaginator.ComputePages(list, perPage: 3);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.Equal(snapshot[i].PageNumber, list[i].PageNumber);
            Assert.Equal(snapshot[i].IsSectionStart, list[i].IsSectionStart);
            Assert.Equal(snapshot[i].IsSectionContinuation, list[i].IsSectionContinuation);
        }
    }

    [Fact] // PAG-03
    public void Resume_ClampsToValidRange()
    {
        Assert.Equal(2, SectionPaginator.ClampResumePage(2, 4));
        Assert.Equal(0, SectionPaginator.ClampResumePage(0, 4));
        // WR-02 boundary: batas atas tepat (requested == maxPage = halaman terakhir) HARUS lolos apa adanya,
        // bukan di-clamp ke 0 / over. Server guard pakai `> maxPage` (inklusif maxPage), client guard pakai
        // `< TOTAL_PAGES` (== `<= maxPage`) — pin boundary ini supaya regresi (mis. `>` jadi `>=`) tertangkap.
        Assert.Equal(4, SectionPaginator.ClampResumePage(4, 4));
    }

    [Fact] // PAG-03
    public void Resume_OutOfRange_FallsBackToZero()
    {
        Assert.Equal(0, SectionPaginator.ClampResumePage(99, 4));
        Assert.Equal(0, SectionPaginator.ClampResumePage(-1, 4));
    }

    [Fact] // mobile perPage=5
    public void MobileFivePerPage_SectionAware()
    {
        // 12 soal Section 1 (snp=false), perPage=5 → 5/5/2 di page 0/1/2.
        var list = BuildSameSection(12, sectionNumber: 1, startNewPage: false);

        SectionPaginator.ComputePages(list, perPage: 5);

        Assert.Equal(2, list.Max(q => q.PageNumber));
        var q6 = list.Single(q => q.DisplayNumber == 6);
        Assert.Equal(1, q6.PageNumber);
        Assert.True(q6.IsSectionContinuation);
    }
}
