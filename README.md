# CooldownReady

[English](README.md) | [한국어](README.ko.md)

## Introduction

CooldownReady displays a countdown and plays a sound alert at the configured time when a specific keyboard key is pressed.
Use it to track cooldowns for repeated actions by sound without continuously watching the screen.

### Main Features

- Register multiple keys, each with its own countdown
- Per-key settings: enabled toggle, monitoring key, alert sound, cooldown time (seconds, up to 999), alert time, repeat-press prevention
- Add key rows with the `＋ Add Key` button (the window grows per row and scrolls past a certain size)
- Configure the alert timing before the timer ends
- Use multiple alert sounds
- Countdown rows use the full key label as a progress bar (remaining time in seconds, up to 999)
- Keep the window always on top
- Switch between Korean and English

### Requirements

- Windows 10 version 1809, build 17763, or later

### Usage

1. Run CooldownReady.
2. Select the key input box in a key row and press the key to monitor.
3. Configure the row: alert sound, cooldown time (sec), and alert time (sec). The checkbox at the front of each row toggles it on and off.
   - Example: cooldown `30` seconds, alert time `5` seconds.
   - The alert sound plays when the remaining time reaches `5` seconds.
4. Press `＋ Add Key` to add more key rows if needed.
5. Press `Save Settings`.
6. Press `Start`.
7. Press a configured key to start that key's countdown.

Pressing the same key again restarts that key's countdown from the beginning (each row's repeat-press prevention option can block this). Press `Stop` to stop monitoring and all timers.

### Saved Settings

All settings are saved in a single file: `%LOCALAPPDATA%\CooldownReady\settings.json`.
Settings from previous versions (`CooldownReadySettings` in Windows app local settings, `language.txt`, and `prevent-duplicate-input.txt`) are migrated automatically on first launch and then cleaned up.
If no language preference exists, the app uses the system language when supported, then falls back to English.

Saved values:

- Key binding list (per key: monitoring key, cooldown time, alert time, alert sound, repeat-press prevention)
- Always-on-top setting
- Language

Error logs are written to:

```text
%LOCALAPPDATA%\CooldownReady\error.log
```

### License

This project is licensed under the [MIT License](LICENSE).

## 2. Development

### Stack

- .NET 8
- WinUI 3
- Windows App SDK
- MSIX package support
- Unpackaged desktop execution

### Run from Source

Visual Studio:

1. Open `CooldownReady.slnx`.
2. Select a launch profile.
   - `CooldownReady (Unpackaged)`: runs as a desktop app.
   - `CooldownReady (Package)`: runs as an MSIX package.
3. Run with `F5`.

.NET CLI:

```powershell
dotnet restore .\CooldownReady.slnx
dotnet run --project .\CooldownReady.csproj -c Debug -p:Platform=x64
```

### Build

```powershell
dotnet build .\CooldownReady.slnx -c Debug -p:Platform=x64
dotnet build .\CooldownReady.slnx -c Release -p:Platform=x64
dotnet publish .\CooldownReady.csproj -c Release -p:Platform=x64 -r win-x64
```

### Project Structure

```text
CooldownReady.csproj        Project configuration
CooldownReady.slnx          Solution file
App.xaml                    App resources and shared styles
App.xaml.cs                 App startup and global exception handling
MainWindow.xaml             Main window UI
MainWindow.xaml.cs          Orchestrates keyboard hook, timer, and services
GlobalKeyboardHook.cs       Global keyboard hook
Controls\                   Reusable UI components (SettingGroup, CountdownDisplay)
Services\                   Settings persistence, localization, sound, asset paths, logging
Strings\                    Language resources (ko-KR, en-US Resources.resw)
Assets\                     Icons, images, and sounds
Package.appxmanifest        MSIX package manifest
```

### Notes

- `WindowsPackageType` is set to `None` to support unpackaged execution.
- Sound files are loaded from `Assets` in both packaged and direct execution modes.
