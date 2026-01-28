# Panduan Update & Upload ke GitHub

Berikut adalah langkah-langkah mudah untuk meng-update kode Anda ke GitHub menggunakan Terminal di VS Code.

## 1. Buka Terminal
Pastikan Anda berada di folder project. Anda bisa membuka terminal di VS Code dengan menekan `` Ctrl + ` `` (backtick) atau pilih menu **Terminal > New Terminal**.

## 2. Cek Status (Opsional tapi Disarankan)
Untuk melihat file apa saja yang berubah:
```bash
git status
```
*File yang berwarna merah artinya belum siap dikirim (belum di-add).*

## 3. Siapkan Perubahan (Git Add)
Untuk memasukkan **semua** perubahan file yang ada ke dalam daftar kirim:
```bash
git add .
```
*Tanda titik (.) artinya "semua file".*

## 4. Simpan Perubahan (Git Commit)
Berikan catatan/pesan tentang apa yang Anda ubah. Ini penting agar Anda ingat apa yang dilakukan di update ini.
```bash
git commit -m "Tulis catatan perubahan disini"
```
*Contoh: `git commit -m "Update halaman dashboard"`*

## 5. Kirim ke GitHub (Git Push)
Langkah terakhir adalah mengirim paket tersebut ke server GitHub (Repo Anda).
```bash
git push origin main
```
*Tunggu proses upload selesai (biasanya 100%).*

---

## Ringkasan Perintah Cepat
```bash
git add .
git commit -m "Update fitur X"
git push origin main
```
