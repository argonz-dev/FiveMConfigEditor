# 🎮 FiveM Config Editor

Modern WPF application untuk mengelola konfigurasi FiveM dengan mudah dan efisien.

![Version](https://img.shields.io/badge/version-1.0.3-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-blue)

## ⚠️ Antivirus False Positive Warning

**This application may be flagged by some antivirus software as a false positive.** This is common for self-contained .NET applications with auto-update functionality.

✅ **The app is completely safe and open source.**

- All source code is available for review
- No malicious code or behavior
- Used by the FiveM community

**How to fix:** Add the executable to your antivirus exclusions or [read more about why this happens](ANTIVIRUS_FALSE_POSITIVE.md).

---

## ✨ Fitur Utama

### 🎯 Manajemen Preset
- Buat, edit, dan hapus preset konfigurasi
- Terapkan preset dengan satu klik
- Simpan snapshot otomatis setiap perubahan
- Thumbnail preview untuk setiap preset

### 🔧 Konfigurasi Fleksibel
- Auto-detect folder FiveM dan CitizenFX.ini
- Atur Pool Sizes per preset
- Konfigurasi Build Number
- Support ReShade5 addon

### 🎨 Mod Manager
- Kelola mods (.rpf files)
- Kelola plugins (.asi, .dll)
- Enable/disable mods dengan mudah
- Graphics Presets untuk kombinasi mods
- Export/Import Mod Pack (.fmpack)
- Backup otomatis sebelum import

### 📦 Mod Pack System
- Export mods dan plugins aktif ke file .fmpack
- Import mod pack dengan konfirmasi detail
- Backup otomatis kondisi saat ini
- Restore backup kapan saja

### 🔄 Auto-Update
- Cek update otomatis saat aplikasi dibuka
- Download dan install update dengan satu klik
- Changelog terintegrasi
- Skip atau remind later

### 🎨 UI Modern
- Tema oranye yang eye-catching
- Dark mode untuk kenyamanan mata
- Animasi smooth dan responsive
- Custom window dengan rounded corners

## 🚀 Quick Start

### First Run Setup
1. Jalankan aplikasi
2. Dialog setup akan muncul otomatis
3. Pilih folder FiveM Anda
4. Aplikasi akan auto-detect:
   - Folder mods
   - Folder plugins
   - File CitizenFX.ini
5. Klik "Lanjutkan" untuk mulai

### Membuat Preset
1. Klik "**+ Buat Preset Baru**"
2. Isi nama dan deskripsi
3. Upload gambar thumbnail (opsional)
4. Klik "**⚙ Config**" untuk atur Pool Sizes dan Build Number
5. Klik "**▶ Terapkan**" untuk menggunakan preset

### Mengelola Mods
1. Buka menu "**🎮 Mods**"
2. Lihat semua mods dan plugins
3. Toggle enable/disable dengan satu klik
4. Simpan kombinasi sebagai Graphics Preset
5. Export/Import mod pack untuk sharing

## 📋 System Requirements

- **OS:** Windows 10/11 (64-bit)
- **.NET:** 8.0 Runtime (included in self-contained build)
- **RAM:** 100 MB minimum
- **Storage:** 200 MB

## 🛠️ Development

### Tech Stack
- **Framework:** .NET 8.0 WPF
- **Language:** C#
- **UI:** XAML
- **Auto-Update:** AutoUpdater.NET

### Build dari Source

```bash
# Clone repository
git clone https://github.com/argonz-dev/FiveMConfigEditor.git
cd FiveMConfigEditor

# Restore packages
dotnet restore

# Build
dotnet build -c Release

# Publish (single file)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin/publish_compact
```

## 📁 Struktur Project

```
FiveMConfigEditorWPF/
├── Dialogs/              # Dialog windows
│   ├── FirstRunSetupDialog.xaml
│   ├── ImportPackConfirmDialog.xaml
│   ├── PresetConfigDialog.xaml
│   └── ...
├── Models/               # Data models
│   ├── IniData.cs
│   ├── ModManager.cs
│   ├── ModPack.cs
│   └── ...
├── Views/                # Main views
│   ├── HomeView.xaml
│   ├── ConfigView.xaml
│   ├── ModManagerView.xaml
│   └── ...
├── MainWindow.xaml       # Main window
├── App.xaml              # Application resources & theme
└── update.xml            # Auto-update configuration
```

## 🎨 Tema & Styling

Aplikasi menggunakan tema oranye custom dengan:
- **Primary Color:** `#FFFF8C00` (Dark Orange)
- **Background:** `#FF1A0F0A` (Dark Brown)
- **Accent:** `#FFAA8866` (Warm Gray)
- **Cards:** `#FF2A1810` (Brown Card)

## 📝 File Formats

### `.fmpack` - Mod Pack Format
ZIP archive berisi:
- `pack.json` - Metadata (nama, author, file list)
- `mods/` - Folder mods (.rpf files)
- `plugins/` - Folder plugins (.asi, .dll)

### `presets.json` - Preset Configuration
```json
{
  "Name": "Preset Name",
  "Description": "Description",
  "ImagePath": "path/to/thumbnail.png",
  "Data": {
    "SavedBuildNumber": "2944",
    "PoolSizes": { "PoolKey": 1000 }
  }
}
```

## 🔄 Auto-Update Setup

Lihat [GITHUB_SETUP.md](GITHUB_SETUP.md) untuk instruksi lengkap setup auto-update dengan GitHub.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License.

## 🙏 Credits

- **AutoUpdater.NET** - Auto-update functionality
- **FiveM** - Game modification framework

## 📞 Support

Jika ada masalah atau pertanyaan:
1. Buka [Issues](https://github.com/argonz-dev/FiveMConfigEditor/issues)
2. Jelaskan masalah dengan detail
3. Sertakan screenshot jika perlu

---

**Made with ❤️ for FiveM Community**
