// Phase 418 D-418-02 — pure-logic test untuk edit-shrink guard (RED Wave 0).
//
// Mengunci aturan: saat EditQuestion menghapus/menyusutkan PackageOption, opsi yang sudah dijawab
// peserta TIDAK boleh dihapus (FK PackageUserResponse→PackageOption = Restrict; menghapusnya = 500
// mentah, hazard 999.14). Helper murni OptionShrinkGuard.FindBlockedOptionIds memutuskan opsi mana
// yang terblok via irisan set removedOptionIds ∩ answeredOptionIds (distinct).
//
// KONTRAK yang Plan 418-02 WAJIB implementasikan (signature TIDAK boleh berubah):
//   public static IReadOnlyList<int> FindBlockedOptionIds(
//       IEnumerable<int> removedOptionIds, IEnumerable<int> answeredOptionIds);
//   return = removedOptionIds ∩ answeredOptionIds (distinct). Empty = boleh hapus.
//
// RED Wave 0 (Plan 418-01): Helpers/OptionShrinkGuard.cs DIBUAT sebagai STUB (NotImplementedException)
// agar test project tetap compile, tapi keempat Fact ini GAGAL di runtime (throw) = true RED.
// Plan 418-02 (GREEN) mengganti body stub dengan implementasi irisan nyata → keempat Fact hijau.
//
// Pure xUnit: TANPA [Trait] / DbContext → selalu jalan tanpa SQLEXPRESS (pola QuestionOptionValidator).
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

public class EditShrinkGuardLogicTests
{
    // D-418-02: opsi 11 akan dihapus DAN sudah dijawab → terblok; daftar memuat 11.
    [Fact]
    public void Blocked_WhenRemovedOptionWasAnswered()
    {
        var blocked = OptionShrinkGuard.FindBlockedOptionIds(
            removedOptionIds: new[] { 10, 11 },
            answeredOptionIds: new[] { 11, 99 });

        Assert.NotEmpty(blocked);
        Assert.Contains(11, blocked);
    }

    // D-418-02: opsi 12 dihapus tapi TAK ada di daftar jawaban → tidak terblok (boleh hapus).
    [Fact]
    public void Allowed_WhenNoRemovedOptionAnswered()
    {
        var blocked = OptionShrinkGuard.FindBlockedOptionIds(
            removedOptionIds: new[] { 12 },
            answeredOptionIds: new[] { 11, 99 });

        Assert.Empty(blocked);
    }

    // D-418-02: tidak ada opsi yang dihapus → tidak ada yang bisa terblok (boleh lanjut).
    [Fact]
    public void Allowed_WhenNothingRemoved()
    {
        var blocked = OptionShrinkGuard.FindBlockedOptionIds(
            removedOptionIds: new int[0],
            answeredOptionIds: new[] { 11 });

        Assert.Empty(blocked);
    }

    // D-418-02: irisan = {11,12} (distinct) — hasil HANYA berisi opsi yang dihapus-DAN-dijawab,
    // bukan 10 (dihapus tak dijawab) maupun 13 (dijawab tak dihapus).
    [Fact]
    public void Blocked_ListContainsOnlyIntersection()
    {
        var blocked = OptionShrinkGuard.FindBlockedOptionIds(
            removedOptionIds: new[] { 10, 11, 12 },
            answeredOptionIds: new[] { 11, 12, 13 });

        Assert.Equal(new[] { 11, 12 }, blocked.OrderBy(x => x).ToArray());
    }
}
