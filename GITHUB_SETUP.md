# 🚀 Setup Auto-Update dengan GitHub

## 📋 Langkah-langkah Setup

### 1️⃣ Buat Repository GitHub

1. Buka https://github.com/new
2. Buat repository baru:
   - **Nama:** `FiveMConfigEditor` (atau nama lain)
   - **Visibility:** Private atau Public (terserah Anda)
   - **Jangan** centang "Add README" atau file lainnya
3. Klik **Create repository**

---

### 2️⃣ Upload Project ke GitHub

Buka terminal/PowerShell di folder project ini, lalu jalankan:

```bash
# Initialize git (jika belum)
git init

# Add semua file
git add .

# Commit pertama
git commit -m "Initial commit - FiveM Config Editor v1.0.0"

# Tambahkan remote
git remote add origin https://github.com/argonz-dev/FiveMConfigEditor.git

# Push ke GitHub
git branch -M main
git push -u origin main
```

---

### 3️⃣ File Sudah Dikonfigurasi ✅

File `update.xml` dan `MainWindow.xaml.cs` sudah dikonfigurasi dengan username GitHub Anda: **argonz-dev**

Tidak perlu edit manual lagi!

---

### 4️⃣ Buat GitHub Release (Setiap Update)

Setiap kali ada versi baru:

1. **Build aplikasi:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o bin/publish_compact
   ```

2. **Buka GitHub repository** → **Releases** → **Create a new release**

3. **Isi form:**
   - **Tag:** `v1.0.0` (sesuai versi di update.xml)
   - **Title:** `FiveM Config Editor v1.0.0`
   - **Description:** Tulis changelog/perubahan
   - **Upload file:** `bin/publish_compact/FiveMConfigEditorWPF.exe`

4. **Klik "Publish release"**

5. **Update file update.xml** untuk versi berikutnya:
   ```xml
   <version>1.0.1.0</version>
   <url>https://github.com/argonz-dev/FiveMConfigEditor/releases/download/v1.0.1/FiveMConfigEditorWPF.exe</url>
   ```

6. **Commit dan push update.xml:**
   ```bash
   git add update.xml
   git commit -m "Bump version to 1.0.1"
   git push
   ```

---

## 🎯 Cara Kerja Auto-Update

1. **Saat aplikasi dibuka** → Cek update otomatis
2. **Jika ada update baru** → Tampilkan dialog
3. **User klik "Update"** → Download dan install otomatis
4. **User klik "Skip"** → Tidak update
5. **User klik "Remind Later"** → Tanya lagi besok

---

## 📝 Update Versi untuk Release Berikutnya

### Di `FiveMConfigEditorWPF.csproj`:
```xml
<Version>1.0.1.0</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

### Di `update.xml`:
```xml
<version>1.0.1.0</version>
<url>https://github.com/argonz-dev/FiveMConfigEditor/releases/download/v1.0.1/FiveMConfigEditorWPF.exe</url>
<changelog>https://github.com/argonz-dev/FiveMConfigEditor/releases/tag/v1.0.1</changelog>
```

---

## 🔧 Testing Auto-Update

1. Build versi 1.0.0 dan buat release
2. Update versi ke 1.0.1 di project
3. Build versi 1.0.1 dan buat release baru
4. Jalankan aplikasi versi 1.0.0
5. Dialog update akan muncul otomatis

---

## ❓ FAQ

**Q: Apakah repository harus public?**
A: Tidak, bisa private. Tapi file release tetap bisa diakses public.

**Q: Berapa biaya GitHub?**
A: Gratis selamanya untuk unlimited repositories (public & private).

**Q: Bagaimana jika user tidak punya internet?**
A: Auto-update akan silent fail, aplikasi tetap jalan normal.

**Q: Apakah update otomatis atau manual?**
A: User harus klik "Update" di dialog. Tidak otomatis tanpa konfirmasi.

---

## 🎨 Customization

Anda bisa custom dialog update dengan mengubah properties di `CheckForUpdates()`:

```csharp
AutoUpdater.Mandatory = true;  // Paksa update (tidak bisa skip)
AutoUpdater.ShowSkipButton = false;  // Sembunyikan tombol Skip
AutoUpdater.ShowRemindLaterButton = false;  // Sembunyikan Remind Later
```

---

## 📞 Support

Jika ada masalah, cek:
1. URL di `update.xml` sudah benar
2. File release sudah di-upload ke GitHub
3. Tag version di release sama dengan di `update.xml`
4. Internet connection aktif

---

**Selamat! Auto-update sudah siap! 🎉**
