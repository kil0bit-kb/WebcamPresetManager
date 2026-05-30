# WebcamPresetManager 🎥

A lightweight, dark-themed Windows utility built on **.NET 8.0** and **DirectShow COM interfaces** that permanently saves, restores, and locks your preferred webcam settings. 

Windows and external communication tools regularly reset webcams to factory auto-modes. This app runs silently to ensure your custom visual look remains completely locked.

---

## ✨ Features & What It Does

* **🎛️ Dynamic Webcam Control Panel**: Automatically scans your webcam hardware on launch and builds custom sliders matching *only* what your camera supports (Exposure, Focus, Zoom, Brightness, Contrast, Saturation, Sharpness, Gamma, Backlight Compensation, Pan, Tilt, Roll).
* **⚡ Integrated Anti-Flicker Toggle**: Replaces standard glitchy sliders with a clean dropdown selection to lock your sensor grid to **Disabled, 50 Hz, or 60 Hz**, instantly eliminating rolling black lines caused by overhead room lights.
* **📂 Profile Management System**: Save your customized configurations into separate, portable `.ccp` (Camera Control Preset) files. You can load different presets for day, night, or streaming profiles via a dropdown menu, or overwrite existing ones.
* **🛡️ Continuous 1.5s Watchdog Enforcer**: Runs an automated background synchronization loop every 1.5 seconds. If an external app (like Zoom, Teams, or Discord) forces your camera back to auto-mode, this app instantly overrides it and locks your manual adjustments back in place.
* **🛠️ Native Properties Hook**: Includes an advanced option button that opens the classic, hidden Windows unmanaged webcam properties frame. Adjustments made inside that windows system screen will instantly sync back onto the app sliders.
* **🚀 Auto-Start & System Tray Minimization**: Toggle the login checkbox to write a silent background initialization switch (`--silent`) to your Windows registry. It boots directly into your taskbar tray at user login without interrupting your desktop.

---

## 🛠️ Built With

* **Framework**: .NET 8.0 (Windows Forms)
* **API Wrapper**: DirectShowLib (Standard COM Subsystem Interfaces)
* **Architecture**: 64-bit safe File-Scoped Namespace Engine

---

## 🚀 How to Run and Build From Source

Since this repository contains the raw development project source files, you will need the [.NET 8.0 SDK Engine](https://microsoft.com) installed on your machine to build and execute the application workspace.

### 1. Clone the Repository
Open your command prompt or PowerShell terminal and run:
```shell
git clone https://github.com
cd WebcamPresetManager
```

### 2. Restore Dependencies
Download the single required native hardware DirectShow media layer wrapper package:
```shell
dotnet restore
```

### 3. Check for Compilation Errors
To verify that the code compiles perfectly cleanly on your machine architecture:
```shell
dotnet build
```

### 4. Run the Application Workspace
Compile and fire up the main dark theme master control dashboard instantly:
```shell
dotnet run
```

---

## 📖 Quick Usage Instructions

1. Select your target webcam input source from the top dropdown layout frame.
2. Calibrate your ideal lighting look using the custom generated trackbars, and choose your local power line frequency grid from the **Anti Flicker** dropdown element.
3. Click **"Save As New Profile"** to select a folder pathway and give your preset custom file folder configuration a portable title.
4. Check the **"Automatically apply fallback configuration profile at system user login"** box to enable silent, continuous background monitoring whenever your machine reboots.
