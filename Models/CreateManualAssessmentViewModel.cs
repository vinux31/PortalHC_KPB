using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HcPortal.Models
{
    public class CreateManualAssessmentViewModel
    {
        [Required(ErrorMessage = "Judul assessment harus diisi")]
        [Display(Name = "Judul Assessment")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Kategori harus dipilih")]
        [Display(Name = "Kategori")]
        public string Category { get; set; } = "";

        [Display(Name = "Score")]
        [Range(0, 100, ErrorMessage = "Score harus antara 0–100")]
        public int? Score { get; set; }

        [Display(Name = "Pass Percentage (%)")]
        [Range(0, 100)]
        public int PassPercentage { get; set; } = 70;

        [Display(Name = "Lulus")]
        public bool IsPassed { get; set; } = true;

        [Required(ErrorMessage = "Tanggal selesai harus diisi")]
        [Display(Name = "Tanggal Selesai")]
        [DataType(DataType.Date)]
        public DateTime CompletedAt { get; set; } = DateTime.Today;

        [Display(Name = "Berlaku Sampai")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Penyelenggara")]
        public string? Penyelenggara { get; set; }

        [Display(Name = "Kota")]
        public string? Kota { get; set; }

        [Display(Name = "Sub-Kategori")]
        public string? SubKategori { get; set; }

        [Display(Name = "Tipe Sertifikat")]
        public string? CertificateType { get; set; }

        [Display(Name = "Nomor Sertifikat")]
        public string? NomorSertifikat { get; set; }

        [Display(Name = "File Sertifikat")]
        public IFormFile? CertificateFile { get; set; }

        // Multi-worker support (same pattern as AddTraining)
        public List<ManualAssessmentWorkerCert>? WorkerCerts { get; set; }
    }

    public class ManualAssessmentWorkerCert
    {
        public string UserId { get; set; } = "";
        public IFormFile? CertificateFile { get; set; }
        public string? NomorSertifikat { get; set; }
    }

    public class EditManualAssessmentViewModel
    {
        public int Id { get; set; }
        public string WorkerId { get; set; } = "";
        public string WorkerName { get; set; } = "";

        [Required(ErrorMessage = "Judul assessment harus diisi")]
        [Display(Name = "Judul Assessment")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Kategori harus dipilih")]
        [Display(Name = "Kategori")]
        public string Category { get; set; } = "";

        [Display(Name = "Score")]
        [Range(0, 100, ErrorMessage = "Score harus antara 0–100")]
        public int? Score { get; set; }

        [Display(Name = "Pass Percentage (%)")]
        [Range(0, 100)]
        public int PassPercentage { get; set; } = 70;

        [Display(Name = "Lulus")]
        public bool IsPassed { get; set; }

        [Required(ErrorMessage = "Tanggal selesai harus diisi")]
        [Display(Name = "Tanggal Selesai")]
        [DataType(DataType.Date)]
        public DateTime CompletedAt { get; set; }

        [Display(Name = "Berlaku Sampai")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Penyelenggara")]
        public string? Penyelenggara { get; set; }

        [Display(Name = "Kota")]
        public string? Kota { get; set; }

        [Display(Name = "Sub-Kategori")]
        public string? SubKategori { get; set; }

        [Display(Name = "Tipe Sertifikat")]
        public string? CertificateType { get; set; }

        [Display(Name = "Nomor Sertifikat")]
        public string? NomorSertifikat { get; set; }

        [Display(Name = "File Sertifikat")]
        public IFormFile? CertificateFile { get; set; }

        public string? ExistingSertifikatUrl { get; set; }
    }
}
