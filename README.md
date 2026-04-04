# 🌻 PvZ Ecology Translator Mod

![Mod Version](https://img.shields.io/badge/Version-0.14.7-brightgreen?style=for-the-badge)
![BepInEx](https://img.shields.io/badge/Requires-BepInEx_5-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?style=for-the-badge)

**PvZ Ecology Translator** adalah mod canggih dan komprehensif (All-in-One) yang dibangun menggunakan kerangka BepInEx untuk game *Plants vs. Zombies: Ecological Edition*.

Mod ini tidak hanya sekadar mengubah teks, tetapi juga mampu menerjemahkan gambar (tekstur/sprite) animasi, memanipulasi tata letak UI tanpa menyentuh kode asli game, memuat font kustom, dan dilengkapi dengan alat *Developer* (In-Game Menu) yang sangat *powerful*!

---

## ✨ Fitur Utama (Features)

Mod ini dirancang dengan fleksibilitas tingkat tinggi. Segala hal yang tidak mungkin dimodifikasi sebelumnya, kini bisa diubah dengan mudah.

### 1. 📖 Universal Text Translation (Terjemahan Teks Cerdas)

* Menerjemahkan teks secara *real-time* dari bahasa Mandarin ke bahasa apa pun menggunakan file JSON yang rapi (`translation_strings.json`).
* Mendukung semua mesin teks Unity: **UGUI Text**, **TextMeshPro (TMP)**, dan **Legacy TextMesh 3D**.
* **Regex Support:** Mendukung terjemahan teks dinamis (seperti nama pemain yang berubah-ubah, hitungan angka, dll) melalui `translation_regexs.json`.

### 2. 🖼️ Dynamic Image & Animation Translation (Terjemahan Gambar & Animasi)

* Mengganti teks/tulisan yang menyatu dengan gambar UI (Sprite/Texture) secara *on-the-fly*.
* **Universal Animation Watcher:** Mod dapat mendeteksi dan menimpa *frame* gambar yang sedang dianimasikan (meskipun efek partikel/ledakan tersebut tidak memiliki komponen `Animator` bawaan).

### 3. 🛠️ Ultimate UI Overrides (Manipulasi UI Ekstrem)

Seringkali teks terjemahan lebih panjang dari aslinya dan keluar dari batas kotak. Melalui file `ui_overrides.json`, Anda bisa memodifikasi komponen UI mana pun secara instan:

* **Position & Rotation:** Geser `posx`, `posy`, dan `rotation`.
* **Scaling & RectTransform:** Atur lebar (`width`) dan tinggi (`height`) secara presisi (*Absolute Width*).
* **Smart Typography:** Atur ukuran font (`size`), matikan pemaksaan pengecilan font (`bestfit`), dan kontrol perpotongan teks (`wrap` / `nowrap`).
* **Hacking Commands:**
  * `oneline=true` : Memaksa teks (seperti UGUI) agar tidak pernah turun ke baris baru menggunakan trik *Non-Breaking Space*.
  * `nativesize=true` : Memaksa gambar (Image) menyesuaikan ukurannya sendiri agar tidak gepeng/distorsi.
  * `tabsize=X` : Mengubah karakter tab `\t` menjadi *X* buah spasi biasa untuk perataan kolom teks (*cheat menu*) yang sempurna.

### 4. 📗 Smart Almanac Dumper & Formatter (Otomatisasi Almanak)

* **Auto-Indent (Hanging Indent):** Secara otomatis merapikan teks deskripsi Almanak (Tanaman & Zombie) yang memiliki tanda titik dua (`:`). Teks yang turun ke baris baru akan sejajar sempurna layaknya tabel!
* **Auto Dumper:** Mengekstrak teks dari buku Almanak langsung menjadi format JSON siap terjemah.

### 5. 🔠 Custom Font Loader (Mod Font)

* Memuat file font kustom Anda sendiri (`.ttf`, `.otf`, atau `.bundle` Unity Asset) untuk menggantikan font default game agar mendukung berbagai karakter bahasa (misal: alfabet Latin, Cyrillic, dll).
* Font akan diaplikasikan ke UGUI, TextMesh, dan TextMeshPro secara bersamaan.

### 6. 🧰 In-Game Developer Toolkit (F12 Menu)

Tekan **F12** di dalam game untuk memunculkan panel GUI khusus *Developer*:

* **Hot-Reload (F5 & F6):** Memuat ulang file JSON dan Gambar PNG secara langsung saat game berjalan tanpa perlu *restart* game!
* **Absolute Screen-Space Scanner (Ctrl + Klik Kanan):** Arahkan *mouse* ke teks/gambar apa pun di layar dan tekan Ctrl+Klik Kanan. Mod akan mencetak "Jalur (Path)", "Ukuran", dan "Teks" asli dari objek tersebut ke konsol, menembus lapisan pelindung (*Raycast Blockers*) dari developer aslinya!
* **Live Settings:** Mengatur jarak Indent Almanak, saklar Auto-Translate (Google API), Konversi Mata Uang Otomatis, dan fitur lainnya.

---

## 🌍 Bahasa yang Tersedia (Included Translations)

Dalam file rilis `.zip`, sudah disematkan mod `.dll` beserta folder terjemahan yang sudah selesai dikerjakan. Saat ini, bahasa bawaan yang sudah siap digunakan adalah:
* us **English**
* 🇮🇩 **Indonesian**

> **Ingin me-request bahasa lain?** Silakan ajukan *request* dengan menghubungi saya via DM di Discord!

---

## 📥 Panduan Instalasi (Installation Guide)

Untuk menggunakan mod ini, kamu memerlukan game aslinya dan **BepInEx 5**, yang merupakan program pemuat mod (Mod Loader) standar untuk game berbasis Unity.

### Langkah 1: Mengunduh Game PvZ Ecology

Jika kamu belum memiliki gamenya, kamu bisa mengunduhnya secara gratis melalui komunitas Discord resmi/terkait:
1. Bergabunglah dengan server Discord melalui tautan ini: **[Join Discord PvZ Ecology](https://discord.gg/AeSQTQGG)**
2. Setelah bergabung, kunjungi *channel* unduhan ini untuk mendapatkan file game terbaru: **[Link Channel Unduhan Game](https://discord.com/channels/1350949592866361394/1350954749259812895)**
3. Ekstrak file game yang sudah diunduh ke dalam sebuah folder di PC kamu.

### Langkah 2: Memasang BepInEx

1. Unduh BepInEx versi 5 (x86 atau x64, sesuaikan dengan bit game PvZ Ecology Anda) dari [Halaman Rilis GitHub BepInEx](https://github.com/BepInEx/BepInEx/releases).
2. Ekstrak file `.zip` yang sudah diunduh.
3. Pindahkan isi ekstraksi (folder `BepInEx`, file `doorstop_config.ini`, dan file `winhttp.dll`) ke dalam folder utama game Anda (folder yang sama dengan file `.exe` PvZ Ecology).
4. **Jalankan game-nya satu kali.** Game akan memunculkan konsol hitam (jendela CMD). Tunggu sampai game masuk ke Main Menu, lalu tutup gamenya. *(Langkah ini wajib agar BepInEx membuat folder-folder konfigurasi otomatis).*

### Langkah 3: Memasang Mod Translator

1. Unduh file rilis `.zip` terbaru dari menu **[Releases]** di sebelah kanan halaman GitHub ini.
2. Ekstrak isi file `.zip` tersebut.
3. Buka folder game Anda, lalu arahkan ke: `BepInEx/plugins/`
4. Pindahkan file `PvZEcologyTranslator.dll` beserta folder `PvZ Ecology Translator` (yang berisi data terjemahan English & Indonesian) ke dalam folder `plugins` tersebut.
5. Jalankan gamenya! (Struktur *folder* *dump* dan lainnya akan dibuat secara otomatis saat game dimuat).

---

## ⌨️ Cara Penggunaan Cepat (Quick Start)

* **Menambah Terjemahan Teks:** Buka `BepInEx/plugins/PvZ Ecology Translator/Localization/Indonesian/Strings/translation_strings.json`. Tulis teks Mandarin di kiri, dan terjemahanmu di kanan. Simpan, lalu tekan **F5** di dalam game.
* **Menambah Gambar Mod:** Letakkan file PNG buatanmu di folder `Textures/`. Nama file PNG harus sama persis dengan nama gambar aslinya di game (kamu bisa mengecek nama aslinya menggunakan fitur *Dumper* atau *Ctrl+Klik Kanan*). Tekan **F6** di dalam game untuk memuat gambarnya.
* **Membuka Menu Mod:** Tekan **F12** kapan saja di dalam game.
* **Mencari Jalur (Path) untuk UI Overrides:** Arahkan mouse ke tombol/teks yang berantakan, tahan tombol **Ctrl Kiri**, lalu **Klik Kanan**. Buka layar konsol BepInEx hitam, dan salin `Path` yang tertera di sana untuk ditempel ke `ui_overrides.json`.

---

## 👨‍💻 Credits

* **Author/Developer:** [Ilham Gimank / Ilham Nurjaman]
* Mod ini dibuat dengan dedikasi tinggi untuk komunitas *Plants vs. Zombies Ecological Edition*.

Selamat menerjemahkan dan memodifikasi! 🧟‍♂️🌱
