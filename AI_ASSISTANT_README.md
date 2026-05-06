# AI Chat Assistant - FiveM Config Editor

## 🤖 Fitur Baru: AI Assistant

AI Chat Assistant adalah fitur baru yang memungkinkan Anda berkomunikasi dengan AI untuk mendapatkan bantuan optimasi GTA V dan FiveM.

### ✨ Fitur Utama

1. **Chat Interaktif**
   - Tanya jawab langsung dengan AI tentang optimasi GTA V
   - Mendukung bahasa Indonesia dan Inggris
   - History percakapan tersimpan selama sesi

2. **Analisis Konfigurasi Otomatis**
   - Klik tombol "📊 Analyze Config" untuk analisis otomatis
   - AI akan menganalisis CitizenFX.ini Anda
   - Memberikan rekomendasi optimasi spesifik

3. **Quick Actions**
   - 💡 Optimization Tips - Tips optimasi umum
   - 🎮 Best Settings for RTX - Rekomendasi untuk GPU RTX
   - 🔧 Fix Low FPS - Solusi untuk masalah FPS rendah

### 🔧 Setup

#### 1. Konfigurasi API Key

Saat pertama kali membuka AI Assistant, Anda akan diminta memasukkan:

- **API Base URL**: `http://localhost:1430/v1` (default untuk serper.ai proxy)
- **API Key**: API key Anda dari serper.ai

#### 2. Mendapatkan API Key

1. Buka https://serper.ai atau https://localhost:1431
2. Login/Register
3. Copy API key Anda
4. Paste ke dialog setup di aplikasi

### 📖 Cara Menggunakan

#### Chat Biasa
1. Klik tombol **🤖 AI Chat** di sidebar
2. Ketik pertanyaan Anda di kotak input
3. Tekan **Enter** atau klik **Send**
4. AI akan merespons dengan jawaban

Contoh pertanyaan:
- "Apa setting terbaik untuk RTX 3060 di 1080p?"
- "Bagaimana cara meningkatkan FPS di FiveM?"
- "Apa perbedaan antara MSAA dan FXAA?"

#### Analisis Konfigurasi
1. Pastikan Anda sudah load file CitizenFX.ini
2. Klik tombol **📊 Analyze Config**
3. AI akan menganalisis konfigurasi Anda
4. Anda akan mendapat rekomendasi spesifik

#### Quick Actions
Klik salah satu tombol quick action untuk pertanyaan umum:
- **💡 Optimization Tips**: Tips optimasi umum
- **🎮 Best Settings for RTX**: Rekomendasi untuk RTX series
- **🔧 Fix Low FPS**: Troubleshooting FPS rendah

### 🎯 Contoh Penggunaan

**Scenario 1: Optimasi untuk Hardware Spesifik**
```
User: "Saya punya RTX 3070 dan Ryzen 5 5600X. Setting apa yang optimal untuk 1440p 60 FPS?"
AI: [Memberikan rekomendasi detail untuk hardware tersebut]
```

**Scenario 2: Troubleshooting**
```
User: "FPS saya drop drastis saat di kota. Apa yang harus saya lakukan?"
AI: [Menganalisis dan memberikan solusi step-by-step]
```

**Scenario 3: Analisis Config**
```
[Klik "Analyze Config"]
AI: "Berdasarkan konfigurasi Anda, saya menemukan:
- PoolSize untuk vehicle terlalu rendah
- Shadow quality bisa diturunkan untuk performa lebih baik
- Rekomendasi: [detail rekomendasi]"
```

### ⚙️ Pengaturan Lanjutan

#### Mengubah API Settings
1. Buka file `settings.json` di folder aplikasi
2. Edit nilai `AiApiBaseUrl` dan `AiApiKey`
3. Restart aplikasi

Contoh `settings.json`:
```json
{
  "IniPath": "d:\\FiveM\\FiveM.app\\CitizenFX.ini",
  "FiveMPath": "d:\\FiveM\\FiveM.app",
  "AiApiBaseUrl": "http://localhost:1430/v1",
  "AiApiKey": "snc_your_api_key_here"
}
```

### 🔒 Keamanan

- API key disimpan secara lokal di `settings.json`
- Tidak ada data yang dikirim ke server selain query Anda
- Semua komunikasi melalui API proxy lokal Anda

### 🐛 Troubleshooting

**Problem: "Error: Request timeout"**
- Pastikan API proxy berjalan di `localhost:1430`
- Cek koneksi internet Anda
- Verifikasi API key masih valid

**Problem: "AI Setup Required"**
- Klik "Yes" untuk setup
- Masukkan API key yang valid
- Pastikan base URL benar

**Problem: "No configuration loaded"**
- Load file CitizenFX.ini terlebih dahulu
- Klik "Config" di sidebar dan pilih file

### 💡 Tips

1. **Pertanyaan Spesifik**: Semakin spesifik pertanyaan, semakin baik jawabannya
2. **Konteks Hardware**: Sebutkan spesifikasi hardware untuk rekomendasi lebih akurat
3. **Gunakan Analyze Config**: Fitur ini memberikan insight berdasarkan config aktual Anda
4. **Clear Chat**: Gunakan tombol 🗑️ untuk memulai percakapan baru

### 🚀 Fitur Mendatang

- [ ] Auto-apply recommendations
- [ ] Preset generator berdasarkan AI
- [ ] Performance prediction
- [ ] Multi-language support yang lebih baik
- [ ] Integration dengan benchmark tools

---

**Developed by**: ARGONZ
**Version**: 1.0.0
**Last Updated**: 2026-05-06
