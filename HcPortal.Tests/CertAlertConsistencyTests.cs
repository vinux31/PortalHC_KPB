// Phase 382 CERT-01 (WSE-11) — single-source coherence lock.
// Membuktikan bahwa cert lulus ValidUntil=null (certificateType null, seperti AssessmentSession cert)
// menghasilkan Status=Aktif sehingga predikat tally consumer `Status==Expired || Status==AkanExpired`
// MENGECUALIKANNYA (anti-undercount/kontradiksi badge + anti-pollute worklist renewal).
//
// Test ini mereproduksi predicate-mirror yang dipakai consumer TANPA mengedit controller:
//   - AdminBaseController.cs L200 worklist:  !IsRenewed && (Status==Expired || Status==AkanExpired)
//   - RenewalController.cs L217/277/300/351 tally: Count(Status==Expired) / Count(Status==AkanExpired)
//   - CDPController.cs L3734/3793 tally:     Count(Status==Expired && !IsRenewed) dst.
// Karena semua consumer mengonsumsi Status enum yang sama dari DeriveCertificateStatus, mengunci
// helper di sini = mengunci koherensi lintas-surface (Pattern 7).

using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CertAlertConsistencyTests
{
    private static DateOnly Today(int offset) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

    // Bangun satu set baris cert mewakili kondisi nyata dashboard, termasuk SATU cert lulus
    // ValidUntil=null (AssessmentSession-style, certificateType null) yang sebelum CERT-01 salah jadi Expired.
    private static List<SertifikatRow> BuildMixedRows() => new()
    {
        // AssessmentSession cert lulus tanpa kedaluwarsa — INI yang sedang diperbaiki (null → Aktif).
        new SertifikatRow
        {
            Judul = "Cert Lulus Tanpa Kedaluwarsa",
            ValidUntil = null,
            Status = SertifikatRow.DeriveCertificateStatus(null, null),
        },
        // Cert masih lama berlaku (> 30 hari) → Aktif.
        new SertifikatRow
        {
            Judul = "Cert Aktif",
            ValidUntil = Today(100),
            Status = SertifikatRow.DeriveCertificateStatus(Today(100), null),
        },
        // Cert akan kedaluwarsa (<= 30 hari) → AkanExpired (HARUS tetap masuk worklist).
        new SertifikatRow
        {
            Judul = "Cert Akan Expired",
            ValidUntil = Today(10),
            Status = SertifikatRow.DeriveCertificateStatus(Today(10), null),
        },
        // Cert sudah kedaluwarsa → Expired (HARUS tetap masuk worklist).
        new SertifikatRow
        {
            Judul = "Cert Expired",
            ValidUntil = Today(-5),
            Status = SertifikatRow.DeriveCertificateStatus(Today(-5), null),
        },
        // Cert permanent admin → Permanent.
        new SertifikatRow
        {
            Judul = "Cert Permanent",
            ValidUntil = null,
            Status = SertifikatRow.DeriveCertificateStatus(null, "Permanent"),
        },
    };

    [Fact]
    public void NullValidUntilCert_DerivesAktif_NotExpired()
    {
        var status = SertifikatRow.DeriveCertificateStatus(null, null);
        Assert.Equal(CertificateStatus.Aktif, status);
        Assert.NotEqual(CertificateStatus.Expired, status);
    }

    [Fact]
    public void RenewalTallyPredicate_DoesNotCountNullCert_AsExpiredOrAkanExpired()
    {
        var rows = BuildMixedRows();

        // Mirror RenewalController.cs L217/218 tally predicate.
        var expiredCount = rows.Count(r => r.Status == CertificateStatus.Expired);
        var akanExpiredCount = rows.Count(r => r.Status == CertificateStatus.AkanExpired);

        // Hanya 1 cert benar-benar Expired + 1 AkanExpired di set — cert null TIDAK menambah keduanya.
        Assert.Equal(1, expiredCount);
        Assert.Equal(1, akanExpiredCount);

        // Eksplisit: baris cert null tidak ber-Status Expired/AkanExpired.
        var nullCert = rows.Single(r => r.ValidUntil == null && r.Judul == "Cert Lulus Tanpa Kedaluwarsa");
        Assert.NotEqual(CertificateStatus.Expired, nullCert.Status);
        Assert.NotEqual(CertificateStatus.AkanExpired, nullCert.Status);
    }

    [Fact]
    public void RenewalWorklistPredicate_ExcludesNullCert()
    {
        var rows = BuildMixedRows();

        // Mirror AdminBaseController.cs L200 worklist predicate (IsRenewed default false di test).
        var worklist = rows
            .Where(r => !r.IsRenewed &&
                        (r.Status == CertificateStatus.Expired || r.Status == CertificateStatus.AkanExpired))
            .ToList();

        // Cert null tidak boleh muncul di worklist renewal (tidak ada item ber-judul "Cert Lulus Tanpa Kedaluwarsa").
        Assert.DoesNotContain(worklist, r => r.Judul == "Cert Lulus Tanpa Kedaluwarsa");
        // Worklist berisi tepat cert Expired + AkanExpired yang sah.
        Assert.Equal(2, worklist.Count);
    }

    [Fact]
    public void CDPTallyPredicate_NullCert_NotCountedAsExpired()
    {
        var rows = BuildMixedRows();

        // Mirror CDPController.cs L3734/3735 tally (with !IsRenewed; default false di test).
        var cdpExpired = rows.Count(r => r.Status == CertificateStatus.Expired && !r.IsRenewed);
        var cdpAkanExpired = rows.Count(r => r.Status == CertificateStatus.AkanExpired && !r.IsRenewed);

        Assert.Equal(1, cdpExpired);
        Assert.Equal(1, cdpAkanExpired);
    }
}
