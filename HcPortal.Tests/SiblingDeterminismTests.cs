// Phase 381 Plan 01 Task 2 — WSE-04 determinism invariant (Phase 373 / Pitfall 2).
// Mengunci alasan helper sibling HARUS dipakai IDENTIK di StartExam + reshuffle: workerIndex =
// sortedSiblingIds.OrderBy(x=>x).IndexOf(id). SQL tak jamin order tanpa ORDER BY; OrderBy(x=>x)
// menjadikan workerIndex stabil terlepas urutan baris yang dikembalikan DB. Bila sibling-SET identik
// dua sisi (kini dijamin oleh SiblingPrePostAwarePredicate yang sama), workerIndex juga identik.
// Pure unit — no DB, no Moq.
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HcPortal.Tests;

public class SiblingDeterminismTests
{
    // workerIndex = OrderBy(x=>x).IndexOf(target) stabil untuk ISI sama, terlepas urutan input
    // (mensimulasikan "SQL tak jamin order"). Matriks: {1 id, >=2 id} x posisi {pertama, tengah, terakhir}.
    [Fact]
    public void WorkerIndex_IsOrderInvariant_AcrossInputPermutations()
    {
        // Single-id grup → index selalu 0.
        Assert.Equal(0, new List<int> { 5 }.OrderBy(x => x).ToList().IndexOf(5));

        // Content set {1,3,7,9} → sorted {1,3,7,9}: target pertama(1)=0, tengah(7)=2, terakhir(9)=3.
        var permutations = new[]
        {
            new List<int> { 7, 3, 9, 1 },
            new List<int> { 1, 3, 7, 9 },
            new List<int> { 9, 7, 3, 1 },
            new List<int> { 3, 9, 1, 7 },
        };
        foreach (var perm in permutations)
        {
            var sorted = perm.OrderBy(x => x).ToList();
            Assert.Equal(0, sorted.IndexOf(1));  // pertama
            Assert.Equal(2, sorted.IndexOf(7));  // tengah
            Assert.Equal(3, sorted.IndexOf(9));  // terakhir
        }
    }

    // Dua list ISI sama URUTAN beda → OrderBy keduanya identik DAN IndexOf(target) sama.
    // Membuktikan StartExam-side workerIndex == reshuffle-side workerIndex bila sibling-SET identik.
    [Fact]
    public void TwoListsSameContentDifferentOrder_YieldIdenticalIndex()
    {
        var startExamSide = new List<int> { 42, 7, 19, 3 };
        var reshuffleSide = new List<int> { 3, 19, 7, 42 };

        var a = startExamSide.OrderBy(x => x).ToList();
        var b = reshuffleSide.OrderBy(x => x).ToList();

        Assert.True(a.SequenceEqual(b));
        foreach (var target in new[] { 42, 7, 19, 3 })
            Assert.Equal(a.IndexOf(target), b.IndexOf(target));
    }
}
