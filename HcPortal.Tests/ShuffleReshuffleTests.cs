using System;
using System.Collections.Generic;
using System.Text.Json;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 373 SHUF-09 regression — closes the existing reshuffle bug where
/// <c>ShuffledOptionIdsPerQuestion</c> was hard-coded to <c>"{}"</c> in both
/// ReshufflePackage and ReshuffleAll, so reshuffled participants never got shuffled
/// options even when the normal StartExam path did. Reshuffle now builds the dict via
/// the same <see cref="ShuffleEngine.BuildOptionShuffle"/> the controllers call, so a
/// non-empty serialization when ShuffleOptions is ON is guaranteed. Pure unit (no DB);
/// full reshuffle mode-matrix + Playwright UAT = Phase 375.
/// </summary>
public class ShuffleReshuffleTests
{
    private static List<PackageQuestion> QuestionsWithOptions() => new()
    {
        new PackageQuestion { Id = 1, Order = 1,
            Options = { new PackageOption { Id = 10 }, new PackageOption { Id = 11 }, new PackageOption { Id = 12 } } },
        new PackageQuestion { Id = 2, Order = 2,
            Options = { new PackageOption { Id = 20 }, new PackageOption { Id = 21 } } },
    };

    [Fact] // SHUF-09: reshuffle with ShuffleOptions ON → dict must NOT serialize to "{}"
    public void OptionShuffle_On_DoesNotSerializeToEmptyObject()
    {
        var dict = ShuffleEngine.BuildOptionShuffle(QuestionsWithOptions(), shuffleOptions: true, new Random(7));
        var json = JsonSerializer.Serialize(dict);
        Assert.NotEqual("{}", json);
        Assert.Equal(2, dict.Count);
    }

    [Fact] // SHUF-09: ShuffleOptions OFF → "{}" (view DB-order fallback) — preserved, intentional
    public void OptionShuffle_Off_SerializesToEmptyObject()
    {
        var dict = ShuffleEngine.BuildOptionShuffle(QuestionsWithOptions(), shuffleOptions: false, new Random(7));
        Assert.Equal("{}", JsonSerializer.Serialize(dict));
    }
}
