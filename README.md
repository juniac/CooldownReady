# CooldownReady

[English](README.md) | [한국어](README.ko.md)

## Introduction

CooldownReady displays a countdown and plays a sound alert at the configured time when a specific keyboard key is pressed.
Use it to track cooldowns for repeated actions by sound without continuously watching the screen.

### Main Features

- Start a countdown on key input
- Configure timer duration
- Configure the alert timing before the timer ends
- Use multiple alert sounds
- Display remaining time and progress
- Keep the window always on top
- Switch between Korean and English

### Requirements

- Windows 10 version 1809, build 17763, or later

### Usage

1. Run CooldownReady.
2. Select the `Monitoring Key` input.
3. Press the key to monitor.
4. Set the `Cooldown Time`.
5. Set the `Alert Time`.
   - Example: cooldown `30` seconds, alert time `5` seconds.
   - The alert sound plays when the remaining time reaches `5` seconds.
6. Select a sound from `Alert Sound`.
7. Press `Save Settings`.
8. Press `Start Monitoring`.
9. Press the configured key to start the countdown.

Pressing the same key again restarts the countdown from the beginning. Press `Stop` to stop monitoring and the timer.

### Saved Settings

Settings are saved in Windows app local settings under `CooldownReadySettings`.
The language preference is also saved to `%LOCALAPPDATA%\CooldownReady\language.txt`.
If no language preference exists, the app uses the system language when supported, then falls back to English.

Saved values:

- Monitoring key
- Cooldown time
- Alert time
- Selected alert sound
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
App.xaml                    App resources
App.xaml.cs                 App startup and global exception handling
MainWindow.xaml             Main window UI
MainWindow.xaml.cs          Timer, settings, sound, and window logic
GlobalKeyboardHook.cs       Global keyboard hook
Assets\                     Icons, images, and sounds
Package.appxmanifest        MSIX package manifest
```

### Notes

- `WindowsPackageType` is set to `None` to support unpackaged execution.
- Sound files are loaded from `Assets` in both packaged and direct execution modes.
