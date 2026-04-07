# Phase 298: Question Types - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 298-question-types
**Areas discussed:** HC Question Management, Excel Import Format, Essay Grading UI, Worker Exam UI, Status & Notifikasi, Mixed Assessment, Sertifikat & Training Record

---

## HC Question Management

### Pemilihan tipe soal
| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown di form soal | Tambah dropdown QuestionType di form create/edit soal. Form berubah dinamis | ✓ |
| Tab terpisah per tipe | Tab MC, Tab MA, Tab Essay di halaman manage questions | |
| Radio button di atas form | Radio MC/MA/Essay di atas form | |

### Penandaan opsi benar MA
| Option | Description | Selected |
|--------|-------------|----------|
| Checkbox per opsi | Ganti radio IsCorrect jadi checkbox, HC centang semua opsi benar | ✓ |
| Multi-select dropdown | Dropdown multi-pilih untuk memilih huruf opsi benar | |

### Referensi jawaban Essay
| Option | Description | Selected |
|--------|-------------|----------|
| Ya, textarea rubrik/kunci jawaban | HC bisa isi rubrik sebagai referensi saat menilai | ✓ |
| Tidak perlu | HC langsung menilai tanpa referensi | |

### Opsi untuk Essay
| Option | Description | Selected |
|--------|-------------|----------|
| Tidak perlu opsi | Essay hanya punya QuestionText + rubrik | ✓ |
| Tetap tampilkan opsi (kosong) | Opsi tetap ada tapi opsional | |

### Jumlah opsi MA
| Option | Description | Selected |
|--------|-------------|----------|
| Tetap 4 opsi (A-D) | Sama seperti MC, bedanya HC bisa centang >1 | ✓ |
| Fleksibel 2-6 opsi | HC bisa tambah/hapus opsi | |

**User's note:** Awalnya bingung perbedaan MC vs MA — dijelaskan bahwa MC = 1 jawaban benar, MA = >1 jawaban benar

### Edit tipe soal
| Option | Description | Selected |
|--------|-------------|----------|
| Boleh, dengan warning | HC bisa ubah tipe, warning bahwa jawaban peserta bisa tidak valid | ✓ |
| Tidak boleh ubah tipe | Sekali dibuat, tipe locked | |
| Boleh tanpa warning | HC bebas ubah tanpa notifikasi | |

### Validasi create/edit
| Option | Description | Selected |
|--------|-------------|----------|
| Ya, setuju (rules ketat) | MC: tepat 1 correct. MA: min 2 correct. Essay: harus ada rubrik, tidak boleh punya opsi | ✓ |
| Lebih longgar | Hanya warning, tidak block save | |
| Lebih ketat | Tambahan: teks min 10 char, opsi tidak boleh duplikat | |

### ScoreValue per tipe
| Option | Description | Selected |
|--------|-------------|----------|
| Sama semua default 10 | Semua tipe soal default 10, HC bisa ubah per soal | |
| Bobot berbeda hanya untuk Essay | MC/MA tetap 10. Hanya Essay yang bisa diubah bobotnya | ✓ |
| Tetap semua 10 | Bobot fixed, tapi Essay bisa skor parsial | |

**User's note:** HC perlu bisa set bobot Essay per soal karena jawaban Essay bisa setengah benar (skor parsial)

### Preview soal
| Option | Description | Selected |
|--------|-------------|----------|
| Defer ke phase lain | Fokus Phase 298 pada fungsionalitas inti | |
| Tambah preview sederhana | Modal preview di halaman manage questions | ✓ |

---

## Excel Import Format

### Format kolom
| Option | Description | Selected |
|--------|-------------|----------|
| Tambah kolom QuestionType + multi Correct | 1 template universal. MA Correct: 'A,B'. Essay: kosongkan Correct | ✓ |
| Sheet terpisah per tipe | 3 sheets berbeda untuk MC, MA, Essay | |
| Auto-detect dari data | Tipe otomatis berdasarkan pola Correct | |

### Backward compatibility
| Option | Description | Selected |
|--------|-------------|----------|
| Default ke MC | File lama tanpa kolom QuestionType otomatis jadi MC | ✓ |
| Tolak, minta format baru | Error jika kolom QuestionType tidak ada | |

### Download template
| Option | Description | Selected |
|--------|-------------|----------|
| 1 template universal | Template tetap 1 file dengan kolom QuestionType | |
| 4 tombol download | MC, MA, Essay, Universal — sesuai kebutuhan HC | ✓ |

**User's note:** User ingin 4 tipe template, termasuk 1 universal

---

## Essay Grading UI

### Lokasi grading
| Option | Description | Selected |
|--------|-------------|----------|
| Inline di AssessmentMonitoringDetail | Soal Essay tampil dengan jawaban + rubrik + input skor langsung di halaman | ✓ |
| Modal popup per soal | Klik tombol 'Nilai Essay' buka modal | |
| Halaman terpisah | Tombol buka halaman khusus grading | |

### Post-grading behavior
| Option | Description | Selected |
|--------|-------------|----------|
| Auto recalculate + update IsPassed | Langsung hitung ulang skor total, update IsPassed, status → Completed | ✓ |
| HC konfirmasi dulu | Preview skor total sebelum finalisasi | |

**User's note:** User minta penjelasan flow lengkap Question Types sebelum memutuskan. Dijelaskan 6-step flow: create → import → exam → grading → status → recalculate

---

## Worker Exam UI

### MA tampilan
| Option | Description | Selected |
|--------|-------------|----------|
| Checkbox list | Layout sama MC, ganti radio → checkbox + label "Pilih semua yang benar" | ✓ |
| Chip/tag style | Opsi sebagai chip toggle | |
| Dropdown multi-select | Dropdown dengan checkbox | |

### Essay tampilan
| Option | Description | Selected |
|--------|-------------|----------|
| Textarea sederhana | Plain text, placeholder, counter karakter | ✓ |
| Rich text editor | TinyMCE/Quill dengan formatting | |

### Auto-save
| Option | Description | Selected |
|--------|-------------|----------|
| Sama seperti MC | MA: save per checkbox change. Essay: debounce 2 detik | ✓ |
| Essay save manual saja | MC/MA auto-save, Essay hanya saat pindah halaman | |

### Nav panel badge tipe
| Option | Description | Selected |
|--------|-------------|----------|
| Ya, badge tipe per nomor | MC/MA/E badge di panel navigasi | |
| Tidak perlu | Hanya nomor + status terjawab/belum | ✓ |

### ExamSummary
| Option | Description | Selected |
|--------|-------------|----------|
| Ringkas per tipe | MC: "A", MA: "A, C", Essay: "50 char..." | ✓ |
| Expand/collapse | Klik untuk lihat jawaban lengkap | |

### Char limit Essay
| Option | Description | Selected |
|--------|-------------|----------|
| Ya, default 2000 karakter | Counter + HC bisa set per soal | ✓ |
| Tidak ada batas | Pekerja bebas tulis | |
| Fixed, tidak bisa diubah HC | 2000 untuk semua | |

### Badge tipe di card soal
| Option | Description | Selected |
|--------|-------------|----------|
| Ya, badge kecil di samping nomor | "Pilihan Ganda" / "Multi Jawaban" / "Essay" | ✓ |
| Tidak perlu badge | Tipe terlihat dari UI-nya | |

---

## Status & Notifikasi

### Status display
| Option | Description | Selected |
|--------|-------------|----------|
| Badge kuning + counter Essay | Badge "Menunggu Penilaian" + "2 Essay belum dinilai" | ✓ |
| Badge saja tanpa counter | Hanya badge status | |

### Notifikasi
| Option | Description | Selected |
|--------|-------------|----------|
| Tidak perlu notifikasi khusus | HC lihat dari monitoring page | ✓ |
| Notifikasi in-app | Bell icon notification | |

---

## Mixed Assessment

### Urutan soal
| Option | Description | Selected |
|--------|-------------|----------|
| Urutan sesuai import/create order | Tidak dikelompokkan per tipe. Shuffle tetap berlaku | ✓ |
| Kelompokkan per tipe | MC dulu, lalu MA, lalu Essay | |
| Essay selalu di akhir | MC+MA normal, Essay di halaman terakhir | |

---

## Sertifikat & Training Record

### Timing generate
| Option | Description | Selected |
|--------|-------------|----------|
| Setelah HC selesai nilai semua Essay | Sertifikat + TrainingRecord hanya setelah "Completed" | ✓ |
| TrainingRecord saat submit, sertifikat setelah | Split timing | |

---

## Claude's Discretion

- Auto-save implementation detail (SignalR vs AJAX)
- Exact debounce timing untuk Essay auto-save
- CSS styling untuk badge dan status
- Preview modal layout
- Error handling saat grading Essay

## Deferred Ideas

- Delegasi Essay ke atasan/supervisor — phase terpisah
- Notifikasi in-app untuk Essay pending
- Rich text editor untuk Essay
- Badge tipe soal di panel navigasi sidebar
