# 🚀 Langkah Terakhir - Push ke GitHub

## ✅ Yang Sudah Saya Lakukan:

1. ✅ Initialize git repository
2. ✅ Add semua file project
3. ✅ Commit pertama
4. ✅ Update semua file dengan username GitHub Anda: **argonz-dev**
5. ✅ Commit konfigurasi
6. ✅ Set remote repository
7. ✅ Rename branch ke main

---

## 📋 Yang Perlu Anda Lakukan:

### 1️⃣ Buat Repository di GitHub

1. Buka browser dan login ke GitHub
2. Buka: https://github.com/new
3. Isi form:
   - **Repository name:** `FiveMConfigEditor`
   - **Description:** "Modern WPF application untuk mengelola konfigurasi FiveM"
   - **Visibility:** Private atau Public (terserah Anda)
   - **❌ JANGAN centang** "Add a README file"
   - **❌ JANGAN centang** "Add .gitignore"
   - **❌ JANGAN centang** "Choose a license"
4. Klik **"Create repository"**

---

### 2️⃣ Push ke GitHub

Setelah repository dibuat, buka PowerShell/Terminal di folder project ini dan jalankan:

```bash
git push -u origin main
```

**Jika diminta login:**
- Username: `argonz-dev`
- Password: Gunakan **Personal Access Token** (bukan password biasa)

**Cara buat Personal Access Token:**
1. Buka: https://github.com/settings/tokens
2. Klik "Generate new token" → "Generate new token (classic)"
3. Beri nama: "FiveM Config Editor"
4. Centang scope: `repo` (full control)
5. Klik "Generate token"
6. **COPY token** (hanya muncul sekali!)
7. Paste sebagai password saat git push

---

### 3️⃣ Buat Release Pertama

Setelah push berhasil:

1. **Buka repository di GitHub:**
   https://github.com/argonz-dev/FiveMConfigEditor

2. **Klik tab "Releases"** → **"Create a new release"**

3. **Isi form release:**
   - **Choose a tag:** Ketik `v1.0.0` (buat tag baru)
   - **Release title:** `FiveM Config Editor v1.0.0`
   - **Description:** 
     ```
     ## 🎉 Initial Release
     
     ### ✨ Fitur:
     - ✅ Manajemen Preset Konfigurasi
     - ✅ Auto-detect FiveM folder dan CitizenFX.ini
     - ✅ Mod Manager (mods & plugins)
     - ✅ Export/Import Mod Pack
     - ✅ Graphics Presets
     - ✅ Auto-Update dari GitHub
     - ✅ Tema Orange Modern
     
     ### 📦 Download:
     Download file `FiveMConfigEditorWPF.exe` di bawah ini.
     ```

4. **Upload file:**
   - Klik "Attach binaries"
   - Upload file: `d:\FiveM\FiveM.app\FiveMConfigEditorWPF\bin\publish_compact\FiveMConfigEditorWPF.exe`

5. **Klik "Publish release"**

---

### 4️⃣ Test Auto-Update

1. Jalankan aplikasi yang sudah di-deploy
2. Klik button "🔄" di title bar
3. Aplikasi akan cek update dari GitHub
4. Jika setup benar, akan muncul "No update available" (karena versi sama)

---

## 🎯 Update Versi Berikutnya

Ketika ada update baru:

### 1. Update versi di `FiveMConfigEditorWPF.csproj`:
```xml
<Version>1.0.1.0</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

### 2. Build aplikasi:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin/publish_compact
```

### 3. Buat GitHub Release baru:
- Tag: `v1.0.1`
- Upload file .exe baru

### 4. Update `update.xml`:
```xml
<version>1.0.1.0</version>
<url>https://github.com/argonz-dev/FiveMConfigEditor/releases/download/v1.0.1/FiveMConfigEditorWPF.exe</url>
<changelog>https://github.com/argonz-dev/FiveMConfigEditor/releases/tag/v1.0.1</changelog>
```

### 5. Commit dan push:
```bash
git add .
git commit -m "Bump version to 1.0.1"
git push
```

User yang pakai versi lama akan otomatis dapat notifikasi update! 🎉

---

## 📞 Troubleshooting

**Q: Git push gagal dengan error "authentication failed"**
A: Gunakan Personal Access Token sebagai password, bukan password GitHub biasa.

**Q: Repository sudah ada di GitHub**
A: Hapus dulu repository lama, atau gunakan nama lain.

**Q: Auto-update tidak bekerja**
A: Pastikan:
- File `update.xml` sudah di-push ke GitHub
- Release sudah dibuat dengan tag yang benar
- File .exe sudah di-upload ke release

---

## 🎉 Selesai!

Setelah langkah-langkah di atas, aplikasi Anda akan:
- ✅ Tersimpan di GitHub
- ✅ Punya sistem auto-update
- ✅ Bisa di-download dari GitHub Releases
- ✅ Otomatis notify user saat ada update baru

**Repository URL:** https://github.com/argonz-dev/FiveMConfigEditor

---

**Good luck! 🚀**
