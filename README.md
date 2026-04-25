# 🌻 PvZ Ecology Translator Mod

![Mod Version](https://img.shields.io/badge/Version-0.2.0-brightgreen?style=for-the-badge)
![BepInEx](https://img.shields.io/badge/Requires-BepInEx_5-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?style=for-the-badge)

**PvZ Ecology Translator** is an advanced and comprehensive (All-in-One) mod built using the BepInEx framework for the game *Plants vs. Zombies: Ecological Edition*.

This mod goes beyond simple text replacement. It translates animated images (sprites/textures), manipulates UI layouts without touching the original game code, loads custom fonts, and features a highly *powerful* In-Game Developer Toolkit!

---

## ✨ Key Features

Designed with ultimate flexibility, this mod allows you to modify things that were previously impossible to change.

### 1. 📖 Universal Text Translation

* Real-time text translation from Chinese to any language using clean JSON files (`translation_strings.json`).
* Supports all Unity text engines: **UGUI Text**, **TextMeshPro (TMP)**, and **Legacy TextMesh 3D**.
* **Regex Support:** Translate dynamic text (e.g., changing player names, wave numbers) via `translation_regexs.json`.

### 2. 🖼️ Dynamic Image & Animation Translation

* Replace Chinese text baked into UI images (Sprites/Textures) on-the-fly.
* **Universal Animation Watcher:** The mod detects and intercepts animated image frames (even if the particle/explosion effects don't use a native Unity `Animator` component).

### 3. 🛠️ Ultimate UI Overrides

Translated text is often longer than the original and may break the UI boundaries. With `ui_overrides.json`, you can instantly modify any UI component:

* **Position & Rotation:** Adjust `posx`, `posy`, and `rotation`.
* **Scaling & RectTransform:** Set `width` and `height` with absolute precision.
* **Smart Typography:** Adjust font `size`, disable forced auto-sizing (`bestfit`), and control text wrapping (`wrap` / `nowrap`).
* **Hacking Commands:**
  * `oneline=true` : Forces text (like UGUI) to stay on a single line by injecting *Non-Breaking Spaces*.
  * `nativesize=true` : Forces images to adapt to their original resolution to prevent distortion/stretching.
  * `tabsize=X` : Converts `\t` tab characters into *X* normal spaces for perfect column/table alignments (e.g., in cheat menus).

### 4. 📗 Smart Almanac Dumper & Formatter

* **Auto-Indent (Hanging Indent):** Automatically aligns Almanac descriptions (Plants & Zombies) containing colons (`:`), making them look like perfectly formatted tables!
* **Auto Dumper:** Extracts text directly from the in-game Almanac into ready-to-translate JSON formats.

### 5. 🔠 Custom Font Loader

* Load your own custom fonts (`.ttf`, `.otf`, or Unity `.bundle` assets) to replace the game's default font, ensuring support for various character sets (Latin, Cyrillic, etc.).
* Fonts are recursively applied to UGUI, TextMesh, and TextMeshPro.

### 6. 🧰 In-Game Developer Toolkit (F12 Menu)

Press **F12** in-game to open a dedicated Developer GUI panel:

* **Hot-Reload (F5 & F6):** Reload JSON files and custom PNG images instantly while the game is running without restarting!
* **Absolute Screen-Space Scanner (Ctrl + Right Click):** Hover your mouse over any text/image and press Ctrl+Right Click. The mod will print its exact "Path", "Size", and original "Text" to the console, bypassing the developers' Raycast Blockers!
* **Live Settings:** Adjust Almanac indent spacing, toggle Google API Auto-Translate, real-time Currency Conversion, and more.

---

## 🌍 Included Translations

The release `.zip` file includes the `.dll` mod along with pre-configured translation folders. The currently available default languages are:
* 🇬🇧 **English**
* 🇮🇩 **Indonesian**

> **Want to request another language?** Feel free to request it by sending me a DM on Discord!

---

## 📥 Installation Guide

To use this mod, you need the base game and **BepInEx 5** (the standard Unity mod loader).

### Step 1: Downloading the Game

If you don't have the game yet, you can download it for free via the official/related Discord community:
1. Join the PvZ Ecology Discord server here: **[Join Discord PvZ Ecology](https://discord.gg/AeSQTQGG)**
2. Once joined, visit this download channel to get the latest game files: **[Game Download Channel Link](https://discord.com/channels/1350949592866361394/1350954749259812895)**
3. Extract the downloaded game files into a folder on your PC.

### Step 2: Installing BepInEx

1. Download BepInEx version 5 (x86 or x64, matching your game's architecture) from the [BepInEx GitHub Releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract the downloaded `.zip` file.
3. Move the extracted contents (`BepInEx` folder, `doorstop_config.ini`, and `winhttp.dll`) into your game's root directory (the same folder as the PvZ Ecology `.exe`).
4. **Run the game once.** The game will show a black console window. Wait until you reach the Main Menu, then close the game. *(This step is required so BepInEx can generate its configuration folders).*

### Step 3: Installing the Translator Mod

1. Download the latest `.zip` release file from the **[Releases]** section on the right side of this GitHub page.
2. Extract the contents of the `.zip` file.
3. Open your game folder and navigate to: `BepInEx/plugins/`
4. Move the `PvZEcologyTranslator.dll` and the `PvZ Ecology Translator` folder (which contains the English & Indonesian translations) into the `plugins` folder.
5. Run the game! (Dump folders and configurations will be created automatically).

---

## ⌨️ Quick Start Guide

* **Adding Text Translations:** Open `BepInEx/plugins/PvZ Ecology Translator/Localization/English/Strings/translation_strings.json`. Write the Chinese text on the left, and your translation on the right. Save it, then press **F5** in-game.
* **Adding Custom Images:** Place your custom PNG files in the `Textures/` folder. The file name must exactly match the original game sprite name (use the Scanner/Dumper to find the original names). Press **F6** in-game to load them.
* **Opening the Mod Menu:** Press **F12** anytime in-game.
* **Finding Paths for UI Overrides:** Hover over a messy text/button, hold **Left Ctrl**, and **Right-Click**. Open the black BepInEx console, copy the printed `Path`, and paste it into your `ui_overrides.json`.

---

## 📸 Screenshots
### English Version
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/bbdb110f-788f-4852-b29d-630f88e634a2" alt="Screenshot 1"></td>
    <td><img src="https://github.com/user-attachments/assets/d2b33628-0822-46ed-af00-81bfb2b0ecd6" alt="Screenshot 2"></td>
    <td><img src="https://github.com/user-attachments/assets/ace371b1-e3e5-41d9-9d29-08ca74dc6e46" alt="Screenshot 3"></td>
  </tr>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/8881c8e6-22e2-4126-8798-49ebdf6476ef" alt="Screenshot 4"></td>
    <td><img src="https://github.com/user-attachments/assets/4f5bb861-30c8-4682-a62c-70c02082084a" alt="Screenshot 5"></td>
    <td></td>
  </tr>
</table>

### Indonesian Version
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/ff5f88b8-53ab-4c74-8fd5-9ea76dd69634" alt="Screenshot 1"></td>
    <td><img src="https://github.com/user-attachments/assets/75dbc83e-dc11-4787-945f-3ec8ff266a09" alt="Screenshot 2"></td>
    <td><img src="https://github.com/user-attachments/assets/225d7b2f-acfb-4236-a61f-81ce3f37ab4d" alt="Screenshot 3"></td>
  </tr>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/80c97eba-52a5-472a-9b36-f2d9ce622e0f" alt="Screenshot 4"></td>
    <td><img src="https://github.com/user-attachments/assets/b64a117d-9260-4b5b-9760-2d54983e327b" alt="Screenshot 5"></td>
    <td></td>
  </tr>
</table>
## 👨‍💻 Credits

---

* **Author/Developer:** [Ilham Gimank / Ilham Nurjaman]
* This mod is created with high dedication to the *Plants vs. Zombies: Ecological Edition* community.

Happy translating and modding! 🧟‍♂️🌱
