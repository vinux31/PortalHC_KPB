using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HcPortal.Models
{
    public class CreateTrainingRecordViewModel
    {
        [Required(ErrorMessage = "Pekerja harus dipilih")]
        [Display(Name = "Pekerja")]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "Nama Pelatihan harus diisi")]
        [Display(Name = "Nama Pelatihan")]
        public string Judul { get; set; } = "";

        [Required(ErrorMessage = "Penyelenggara harus diisi")]
        [Display(Name = "Penyelenggara")]
        public string Penyelenggara { get; set; } = "";

        [Display(Name = "Kota")]
        public string? Kota { get; set; }

        [Required(ErrorMessage = "Kategori harus dipilih")]
        [Display(Name = "Kategori")]
        public string Kategori { get; set; } = "";

        [Required(ErrorMessage = "Tanggal harus diisi")]
        [Display(Name = "Tanggal")]
        [DataType(DataType.Date)]
        public DateTime Tanggal { get; set; } = DateTime.Today;

        [Display(Name = "Tanggal Mulai")]
        [DataType(DataType.Date)]
        public DateTime? TanggalMulai { get; set; }

        [Display(Name = "Tanggal Selesai")]
        [DataType(DataType.Date)]
        public DateTime? TanggalSelesai { get; set; }

        [Required(ErrorMessage = "Status harus dipilih")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Passed";

        [Display(Name = "Nomor Sertifikat")]
        public string? NomorSertifikat { get; set; }

        [Display(Name = "Berlaku Sampai")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Tipe Sertifikat")]
        public string? CertificateType { get; set; }

        [Display(Name = "File Sertifikat")]
        public IFormFile? CertificateFile { get; set; }
    }
}
