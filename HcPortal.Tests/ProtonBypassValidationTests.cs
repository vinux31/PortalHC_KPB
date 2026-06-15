using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests
{
    /// <summary>
    /// Phase 360 (PBYP-02) — unit test validasi pure §5 bypass (BypassValidator.Validate).
    /// Pure predicate tanpa DB — input nilai sudah di-resolve caller (pola ProtonYearGateTests).
    /// E8 (tepat 1 assignment aktif) TIDAK di sini — butuh DB, dicek di ExecuteInstant + BypassSave (B-04).
    /// </summary>
    public class ProtonBypassValidationTests
    {
        private static BypassValidationInput ValidInput(
            string reason = "Mutasi unit kilang",
            int activeSourceTrackId = 1,
            int targetTrackId = 2,
            int sourceTahun = 1,
            int targetTahun = 2,
            string mode = "CL-C",
            bool sourceComplete = false,
            bool sourceHasFinal = false)
            => new(reason, activeSourceTrackId, targetTrackId, sourceTahun, targetTahun, mode, sourceComplete, sourceHasFinal);

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void AlasanKosong_Invalid(string reason)
        {
            var (valid, message) = BypassValidator.Validate(ValidInput(reason: reason));
            Assert.False(valid);
            Assert.Contains("Alasan wajib diisi", message);
        }

        [Fact]
        public void TargetSamaDenganTrackAktif_Invalid_E14()
        {
            var (valid, message) = BypassValidator.Validate(ValidInput(activeSourceTrackId: 5, targetTrackId: 5));
            Assert.False(valid);
            Assert.Contains("Target sama dengan track aktif", message);
        }

        [Fact]
        public void LompatDuaTahun_Invalid_DB()
        {
            var (valid, message) = BypassValidator.Validate(ValidInput(sourceTahun: 1, targetTahun: 3));
            Assert.False(valid);
            Assert.Contains("maksimal 1", message);
        }

        [Fact]
        public void SelisihSatuTahun_Valid()
        {
            var (valid, _) = BypassValidator.Validate(ValidInput(sourceTahun: 2, targetTahun: 1));
            Assert.True(valid);
        }

        [Fact]
        public void TahunSama_TrackBeda_Valid_KoreksiLateral()
        {
            // Δtahun == 0 dengan track berbeda (beda TrackType) = koreksi/lateral — valid.
            var (valid, _) = BypassValidator.Validate(ValidInput(sourceTahun: 2, targetTahun: 2));
            Assert.True(valid);
        }

        [Theory]
        [InlineData("CL-X")]
        [InlineData("")]
        [InlineData("cl-a")]
        public void ModeTidakDikenal_Invalid(string mode)
        {
            var (valid, message) = BypassValidator.Validate(ValidInput(mode: mode));
            Assert.False(valid);
            Assert.Contains("Mode", message);
        }

        [Fact]
        public void CLA_SourceBelumKomplit_Invalid_B03()
        {
            var (valid, message) = BypassValidator.Validate(
                ValidInput(mode: "CL-A", sourceComplete: false, sourceHasFinal: false));
            Assert.False(valid);
            Assert.Contains("belum komplit", message);
        }

        [Fact]
        public void CLA_KomplitTapiTanpaPenandaFinal_Invalid_B03_HasFinal()
        {
            // B-03 (review): CL-A WAJIB allApproved DAN final ada (spec §4/§5).
            // Komplit tanpa penanda → arahkan CL-B(a) supaya penanda terbit Origin="Bypass".
            var (valid, message) = BypassValidator.Validate(
                ValidInput(mode: "CL-A", sourceComplete: true, sourceHasFinal: false));
            Assert.False(valid);
            Assert.Contains("Penanda Lulus tahun asal belum terbit", message);
        }

        [Fact]
        public void CLA_KomplitDanFinalAda_Valid_B03()
        {
            var (valid, _) = BypassValidator.Validate(
                ValidInput(mode: "CL-A", sourceComplete: true, sourceHasFinal: true));
            Assert.True(valid);
        }

        [Theory]
        [InlineData("CL-B(a)")]
        [InlineData("CL-B(b)")]
        public void CLB_FinalSudahAda_Invalid_DD(string mode)
        {
            var (valid, message) = BypassValidator.Validate(
                ValidInput(mode: mode, sourceComplete: true, sourceHasFinal: true));
            Assert.False(valid);
            Assert.Contains("Final tahun asal sudah ada", message);
        }

        [Theory]
        [InlineData("CL-B(a)")]
        [InlineData("CL-B(b)")]
        [InlineData("CL-C")]
        public void InputValidLengkap_Valid(string mode)
        {
            var (valid, message) = BypassValidator.Validate(ValidInput(mode: mode));
            Assert.True(valid);
            Assert.Equal("", message);
        }
    }
}
