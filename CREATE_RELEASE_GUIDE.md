# Panduan Membuat GitHub Release

## Langkah-langkah:

### 1. Buka GitHub Repository
Buka: https://github.com/argonz-dev/FiveMConfigEditor

### 2. Buat Release Baru
1. Klik tab **"Releases"** di sidebar kanan
2. Klik tombol **"Create a new release"** atau **"Draft a new release"**

### 3. Isi Form Release
- **Tag version:** `v1.0.0`
- **Release title:** `FiveM Config Editor v1.0.0`
- **Description:** Copy dari `RELEASE_NOTES.md` atau tulis:
  ```
  🎉 Initial Release
  
  Features:
  - CitizenFX.ini configuration editor
  - Graphics settings management
  - Preset system (save/load configurations)
  - Snapshot history with auto-detection
  - Mod manager
  - AI Assistant for optimization tips
  - Auto-update functionality
  - Dark theme UI
  
  Requirements:
  - Windows 10/11
  - FiveM installed
  ```

### 4. Upload File
Drag & drop atau klik "Attach binaries" dan upload file:
- **File:** `D:\FiveM\FiveM.app\FiveMConfigEditorWPF\bin\Release\publish\FiveMConfigEditorWPF.exe`
- **Nama file harus:** `FiveMConfigEditorWPF.exe` (jangan diubah!)

### 5. Publish Release
1. Centang **"Set as the latest release"**
2. Klik **"Publish release"**

### 6. Upload update.xml ke Repository
Setelah release dibuat, upload file `update.xml` ke root repository:
1. Buka repository di GitHub
2. Klik **"Add file"** → **"Upload files"**
3. Upload file `update.xml` dari folder project
4. Commit dengan message: "Add update.xml for auto-update"

---

## Link Download Setelah Release:
Setelah release dibuat, link download akan menjadi:
```
https://github.com/argonz-dev/FiveMConfigEditor/releases/download/v1.0.0/FiveMConfigEditorWPF.exe
```

## Verifikasi Auto Update:
1. Download dan jalankan aplikasi dari release
2. Klik tombol 🔄 di title bar
3. Jika ada versi baru, akan muncul notifikasi update

---

## Untuk Release Berikutnya (v1.0.1, v1.1.0, dll):
1. Update versi di `FiveMConfigEditorWPF.csproj`
2. Update versi di `update.xml`
3. Build ulang: `dotnet publish ...`
4. Buat release baru di GitHub dengan tag versi baru
5. Upload executable baru
6. Push `update.xml` yang sudah diupdate ke repository
