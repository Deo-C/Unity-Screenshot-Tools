# 📸 Unity Screenshot Tool

A lightweight Unity Editor tool that lets you capture screenshots during Play Mode with a single keypress — and browse, preview, or delete them directly inside the Editor.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-active-brightgreen)

---

## ✨ Features

- **One-key capture** — Press `F12` (configurable) during Play Mode to save a screenshot instantly
- **Auto-save** — Screenshots are saved automatically to a `Screenshots/` folder next to your project
- **Editor Window** — Browse all captures with thumbnail previews from `Tools > Screenshot Tool`
- **Open in Explorer/Finder** — Click any screenshot to reveal it in your OS file manager
- **Delete individually or all at once** — Clean up captures without leaving the Editor
- **Resolution multiplier** — Capture at 1x, 2x, 3x, or 4x your game's native resolution
- **Persistent settings** — Your folder name, prefix, and key binding are remembered via `EditorPrefs`
- - **Auto Shot** — Takes a photo automatically at the selected interval (5, 10 or 15 seconds).

---

## 🚀 Installation

1. Copy the `ScreenshotTool` folder into your Unity project's `Assets/` directory.
2. Unity will compile the scripts automatically.
3. Open the tool from the menu: **Tools → Screenshot Tool**

```
Assets/
└── ScreenshotTool/
    ├── Runtime/
    │   └── ScreenshotCapture.cs
    └── Editor/
        └── ScreenshotToolWindow.cs
```

---

## 🎮 Usage

### Step 1 — Add the component
Add `ScreenshotCapture` to any GameObject in your scene (e.g. a `_Tools` empty GameObject).

### Step 2 — Configure (optional)
In the Inspector, set:
| Property | Default | Description |
|---|---|---|
| Capture Key | `F12` | Key to trigger capture |
| Super Size | `1` | Resolution multiplier |
| Folder Name | `Screenshots` | Output folder name |
| File Prefix | `screenshot` | File name prefix |

### Step 3 — Play & capture
Enter Play Mode → press your capture key → screenshot is saved and appears in the Editor Window instantly.

### Step 4 — Browse in the Editor Window
Open **Tools → Screenshot Tool** to see all captures with thumbnails. From there you can:
- Click **Open** to reveal the file in Explorer / Finder
- Click **Delete** to remove a single capture
- Click **Clear All Screenshots** to wipe the folder

---

## 📁 Output

Screenshots are saved outside the `Assets/` folder to avoid triggering Unity's asset import pipeline:

```
YourProject/
├── Assets/
├── Screenshots/          ← saved here
│   ├── screenshot_2024-01-15_14-32-01.png
│   └── screenshot_2024-01-15_14-35-22.png
└── ...
```

The `Screenshots/` folder is excluded via `.gitignore` by default.

---

## 🛠 Requirements

- Unity **2021.3 LTS** or newer
- No external packages required
- **Tested Versions** — 6000.3.9f1 LTS
---

## 📄 License

MIT — free to use in personal and commercial projects.
