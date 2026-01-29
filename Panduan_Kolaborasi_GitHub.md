# 🤝 Panduan Kolaborasi GitHub - PortalHC_KPB

Panduan lengkap untuk Anda dan rekan kerja berkolaborasi menggunakan Git & GitHub.

---

## 📌 Informasi Repository

- **URL Repository:** https://github.com/vinux31/PortalHC_KPB
- **Owner:** vinux31 (Anda)
- **Branch Utama:** main

---

## 👤 BAGIAN 1: LANGKAH UNTUK ANDA (Rinoa - Owner)

### ✅ Step 1: Tambahkan Rekan Kerja sebagai Collaborator

1. Buka browser dan masuk ke: https://github.com/vinux31/PortalHC_KPB
2. Klik tab **Settings** (di menu atas)
3. Di sidebar kiri, klik **Collaborators and teams**
4. Klik tombol **Add people**
5. Masukkan **username GitHub** atau **email** rekan kerja Anda
6. Klik **Add [username] to this repository**
7. Rekan kerja akan menerima email invitation

> [!IMPORTANT]
> Pastikan rekan kerja sudah memiliki akun GitHub. Jika belum, minta mereka membuat akun di https://github.com/signup

### ✅ Step 2: Pastikan Perubahan Lokal Anda Sudah di GitHub

Sebelum rekan kerja mulai, pastikan semua perubahan terbaru Anda sudah di-push:

```bash
# Buka PowerShell/Terminal di folder project
cd C:\Users\rinoa\Desktop\PortalHC_KPB

# Cek status
git status

# Jika ada perubahan, commit dan push
git add .
git commit -m "Perubahan terbaru sebelum kolaborasi"
git push origin main
```

### ✅ Step 3: Workflow Harian Anda

**SETIAP KALI SEBELUM MULAI BEKERJA:**

```bash
# 1. Buka PowerShell di folder project
cd C:\Users\rinoa\Desktop\PortalHC_KPB

# 2. Ambil perubahan terbaru dari GitHub
git pull origin main
```

**SETELAH SELESAI BEKERJA:**

```bash
# 1. Lihat file apa saja yang berubah
git status

# 2. Tambahkan semua perubahan
git add .

# 3. Commit dengan pesan yang jelas
git commit -m "Deskripsi perubahan yang Anda buat"

# 4. Push ke GitHub
git push origin main
```

**Contoh Pesan Commit yang Baik:**
- ✅ "Menambahkan fitur login di halaman utama"
- ✅ "Memperbaiki bug pada form assessment"
- ✅ "Update tampilan dashboard dengan chart baru"
- ❌ "update" (terlalu singkat)
- ❌ "fix" (tidak jelas)

---

## 👥 BAGIAN 2: LANGKAH UNTUK REKAN KERJA

### ✅ Step 1: Persiapan Awal (HANYA SEKALI)

#### A. Pastikan Git Sudah Terinstall

```bash
# Cek apakah Git sudah terinstall
git --version
```

Jika belum terinstall, download dari: https://git-scm.com/download/win

#### B. Konfigurasi Git (Pertama Kali)

```bash
# Set nama Anda
git config --global user.name "Nama Rekan Kerja"

# Set email GitHub Anda
git config --global user.email "email@rekankerja.com"
```

#### C. Terima Invitation dari GitHub

1. Cek email Anda
2. Buka email dari GitHub dengan subject "You've been invited to collaborate..."
3. Klik tombol **Accept invitation**
4. Atau buka: https://github.com/vinux31/PortalHC_KPB/invitations

### ✅ Step 2: Clone Repository ke Komputer

```bash
# 1. Buka PowerShell/Terminal
# 2. Pindah ke folder tempat Anda ingin menyimpan project
cd C:\Users\[username]\Desktop

# 3. Clone repository
git clone https://github.com/vinux31/PortalHC_KPB.git

# 4. Masuk ke folder project
cd PortalHC_KPB
```

Sekarang Anda memiliki salinan lengkap project di komputer Anda!

### ✅ Step 3: Workflow Harian Rekan Kerja

**SETIAP KALI SEBELUM MULAI BEKERJA:**

```bash
# 1. Buka PowerShell di folder project
cd C:\Users\[username]\Desktop\PortalHC_KPB

# 2. Ambil perubahan terbaru dari GitHub
git pull origin main
```

**SETELAH SELESAI BEKERJA:**

```bash
# 1. Lihat file apa saja yang berubah
git status

# 2. Tambahkan semua perubahan
git add .

# 3. Commit dengan pesan yang jelas
git commit -m "Deskripsi perubahan yang dibuat"

# 4. Push ke GitHub
git push origin main
```

---

## 🔄 BAGIAN 3: SKENARIO KOLABORASI

### Skenario 1: Bekerja di File yang Berbeda (AMAN ✅)

**Anda:** Edit `Views/Home/Index.cshtml`
**Rekan:** Edit `Controllers/HomeController.cs`

**Hasil:** Tidak ada konflik, perubahan akan digabung otomatis.

**Langkah:**
1. Anda: `git pull` → edit → `git add .` → `git commit` → `git push`
2. Rekan: `git pull` → edit → `git add .` → `git commit` → `git push`

### Skenario 2: Bekerja di File yang Sama, Baris Berbeda (AMAN ✅)

**Anda:** Edit baris 10-20 di `_Layout.cshtml`
**Rekan:** Edit baris 50-60 di `_Layout.cshtml`

**Hasil:** Git akan menggabungkan otomatis.

### Skenario 3: Bekerja di File yang Sama, Baris yang Sama (KONFLIK ⚠️)

**Anda:** Edit baris 37 di `_Layout.cshtml`
**Rekan:** Edit baris 37 di `_Layout.cshtml`

**Hasil:** Akan terjadi konflik yang harus diselesaikan manual.

**Cara Mengatasi Konflik:**

```bash
# Saat git pull, akan muncul pesan konflik
git pull origin main
# Output: CONFLICT (content): Merge conflict in Views/Shared/_Layout.cshtml

# 1. Buka file yang konflik di editor
# 2. Cari tanda konflik:
<<<<<<< HEAD
Kode versi Anda
=======
Kode versi rekan kerja
>>>>>>> [commit-hash]

# 3. Edit file, hapus tanda konflik, pilih kode yang benar
# 4. Simpan file

# 5. Tandai konflik sudah selesai
git add Views/Shared/_Layout.cshtml

# 6. Commit hasil penyelesaian konflik
git commit -m "Menyelesaikan konflik di _Layout.cshtml"

# 7. Push
git push origin main
```

---

## 💡 BAGIAN 4: TIPS & BEST PRACTICES

### ✅ DO (Lakukan):

1. **Selalu `git pull` sebelum mulai bekerja**
2. **Commit sesering mungkin** (jangan menumpuk banyak perubahan)
3. **Push segera setelah selesai** (jangan ditunda)
4. **Tulis pesan commit yang jelas dan deskriptif**
5. **Komunikasi dengan rekan kerja** (siapa mengerjakan apa)
6. **Backup pekerjaan penting** sebelum melakukan operasi Git yang kompleks

### ❌ DON'T (Jangan):

1. ❌ Jangan edit file yang sama secara bersamaan tanpa koordinasi
2. ❌ Jangan lupa `git pull` sebelum bekerja
3. ❌ Jangan push kode yang error/belum ditest
4. ❌ Jangan gunakan pesan commit yang tidak jelas
5. ❌ Jangan panik saat konflik (tenang dan selesaikan step by step)

---

## 🆘 BAGIAN 5: TROUBLESHOOTING

### Problem 1: "Permission denied" saat push

**Penyebab:** Rekan kerja belum menjadi collaborator

**Solusi:**
- Pastikan Anda (owner) sudah menambahkan rekan sebagai collaborator
- Rekan kerja harus accept invitation terlebih dahulu

### Problem 2: "Your local changes would be overwritten"

**Penyebab:** Ada perubahan lokal yang belum di-commit

**Solusi:**
```bash
# Simpan perubahan dulu
git add .
git commit -m "Menyimpan perubahan lokal"

# Baru pull
git pull origin main
```

### Problem 3: "Merge conflict"

**Penyebab:** Anda dan rekan edit baris yang sama

**Solusi:** Lihat "Cara Mengatasi Konflik" di Bagian 3 Skenario 3

### Problem 4: Lupa password GitHub

**Solusi:**
- GitHub sekarang menggunakan **Personal Access Token** bukan password
- Buat token di: https://github.com/settings/tokens
- Gunakan token sebagai password saat diminta

---

## 📞 BAGIAN 6: KOMUNIKASI TIM

### Koordinasi Harian (Disarankan):

**Pagi:**
- Diskusikan siapa mengerjakan fitur/file apa hari ini
- Pastikan semua sudah `git pull` untuk mendapat update terbaru

**Siang/Sore:**
- Update progress ke rekan kerja
- Push perubahan yang sudah selesai

**Sebelum Pulang:**
- Pastikan semua perubahan sudah di-commit dan push
- Informasikan ke rekan apa yang sudah dikerjakan

### Tools Komunikasi:

- **WhatsApp/Telegram:** Koordinasi cepat
- **GitHub Issues:** Tracking bug dan fitur
- **GitHub Projects:** Manajemen task

---

## 🎯 RINGKASAN PERINTAH PENTING

### Perintah Harian:

```bash
# SEBELUM KERJA
git pull origin main

# SETELAH KERJA
git status
git add .
git commit -m "Deskripsi perubahan"
git push origin main
```

### Perintah Berguna Lainnya:

```bash
# Lihat history commit
git log --oneline

# Lihat siapa yang edit file terakhir
git log -p [nama-file]

# Batalkan perubahan yang belum di-commit
git checkout -- [nama-file]

# Lihat perbedaan sebelum commit
git diff
```

---

## ✨ KESIMPULAN

Dengan mengikuti panduan ini, Anda dan rekan kerja dapat:
- ✅ Bekerja bersamaan tanpa saling mengganggu
- ✅ Melacak semua perubahan yang dibuat
- ✅ Mengatasi konflik dengan mudah
- ✅ Berkolaborasi secara profesional

> [!TIP]
> **Kunci sukses kolaborasi:** Komunikasi yang baik + `git pull` sebelum bekerja + commit & push secara teratur!

---

**Selamat Berkolaborasi! 🚀**

Jika ada pertanyaan atau masalah, jangan ragu untuk bertanya!
