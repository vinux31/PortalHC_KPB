using System.Text.Json;
using HcPortal.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 425 (CLN-05 / VAL-07) — pure parity tests untuk <see cref="ControllerGuards"/>.
/// Membuktikan shape JSON helper byte-identik dengan pola inline Json(new { success = false, message })
/// yang dibaca frontend JS (data.success / data.message). No DB, no fixture, no [Trait].
/// Serialisasi via opsi camelCase memirror MVC default (Program.cs tanpa AddJsonOptions kustom).
/// Analog CertIssuanceRulesTests (fase 423).
/// </summary>
public class ControllerGuardsTests
{
    private class DummyController : Controller { }

    private static readonly JsonSerializerOptions CamelCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void JsonFail_ShapeIdenticalToInlinePattern()
    {
        var c = new DummyController();

        var helperResult = c.JsonFail("Soal tidak ditemukan");
        var inline = new JsonResult(new { success = false, message = "Soal tidak ditemukan" });

        var helperJson = JsonSerializer.Serialize(helperResult.Value, CamelCase);
        var inlineJson = JsonSerializer.Serialize(inline.Value, CamelCase);

        // Parity: helper == pola inline existing.
        Assert.Equal(inlineJson, helperJson);
        // Byte-eksak: {"success":false,"message":"..."} camelCase, urutan success lalu message.
        Assert.Equal("{\"success\":false,\"message\":\"Soal tidak ditemukan\"}", helperJson);
    }

    // Parity untuk kelima/keenam pesan cluster SubmitEssayScore — termasuk pesan dinamis (interpolasi).
    [Theory]
    [InlineData("Session tidak ditemukan")]
    [InlineData("Penilaian hanya bisa dilakukan saat status Menunggu Penilaian.")]
    [InlineData("Soal tidak ditemukan")]
    [InlineData("Skor harus antara 0 dan 5")]
    [InlineData("Soal ini bukan tipe Essay.")]
    [InlineData("Soal bukan milik sesi ini.")]
    public void JsonFail_MatchesInlineForClusterMessages(string message)
    {
        var c = new DummyController();

        var helperJson = JsonSerializer.Serialize(c.JsonFail(message).Value, CamelCase);
        var inlineJson = JsonSerializer.Serialize(
            new JsonResult(new { success = false, message }).Value, CamelCase);

        Assert.Equal(inlineJson, helperJson);
    }
}
