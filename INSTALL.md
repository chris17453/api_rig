# PostmanClone Installation Guide

This guide provides instructions for installing PostmanClone on Windows, Linux, and macOS.

## Table of Contents

- [Windows Installation](#windows-installation)
- [Linux Installation](#linux-installation)
- [macOS Installation](#macos-installation)
- [Running from Source](#running-from-source)
- [System Requirements](#system-requirements)
- [Troubleshooting](#troubleshooting)

---

## Windows Installation

### Method 1: Windows Installer (.msi) - Recommended

1. **Download** the latest `PostmanClone-Setup.msi` installer from the [Releases](https://github.com/chris17453/api_rig/releases) page

2. **Run** the installer by double-clicking the `.msi` file

3. **Follow** the installation wizard:
   - Accept the license agreement
   - Choose installation directory (default: `C:\Program Files\PostmanClone`)
   - Select whether to create shortcuts
   - Click Install

4. **Launch** PostmanClone from:
   - Start Menu → PostmanClone
   - Desktop shortcut (if selected)
   - Installation directory → `PostmanClone.App.exe`

### Method 2: Manual Installation from ZIP

1. **Download** the `PostmanClone-win-x64.zip` archive

2. **Extract** the archive to your desired location (e.g., `C:\Program Files\PostmanClone`)

3. **Run** `PostmanClone.App.exe` from the extracted folder

4. **(Optional)** Create a shortcut:
   - Right-click on `PostmanClone.App.exe`
   - Select "Create shortcut"
   - Move the shortcut to your Desktop or Start Menu

### Prerequisites

- **Windows 10 version 1809 or later** (or Windows 11)
- **.NET 10.0 Runtime** - The installer will check and prompt if not installed
  - Download from: [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## Linux Installation

### Method 1: Debian/Ubuntu (.deb) - Recommended

For Debian-based distributions (Ubuntu, Debian, Linux Mint, etc.):

1. **Download** the latest `.deb` package:
   ```bash
   wget https://github.com/chris17453/api_rig/releases/latest/download/postmanclone_1.0.0_amd64.deb
   ```

2. **Install** the package:
   ```bash
   sudo dpkg -i postmanclone_1.0.0_amd64.deb
   ```

3. **Install dependencies** (if prompted):
   ```bash
   sudo apt-get install -f
   ```

4. **Launch** PostmanClone:
   ```bash
   postmanclone
   ```
   Or search for "PostmanClone" in your application menu

### Method 2: Generic Linux (.tar.gz)

For all Linux distributions:

1. **Download** the `.tar.gz` archive:
   ```bash
   wget https://github.com/chris17453/api_rig/releases/latest/download/postmanclone-1.0.0-linux-x64.tar.gz
   ```

2. **Extract** the archive:
   ```bash
   mkdir -p ~/Applications/postmanclone
   tar -xzf postmanclone-1.0.0-linux-x64.tar.gz -C ~/Applications/postmanclone
   ```

3. **Make executable**:
   ```bash
   chmod +x ~/Applications/postmanclone/PostmanClone.App
   ```

4. **Run** PostmanClone:
   ```bash
   ~/Applications/postmanclone/PostmanClone.App
   ```

5. **(Optional)** Add to PATH:
   ```bash
   echo 'export PATH="$HOME/Applications/postmanclone:$PATH"' >> ~/.bashrc
   source ~/.bashrc
   ```

6. **(Optional)** Create desktop entry:
   ```bash
   cat > ~/.local/share/applications/postmanclone.desktop << EOF
   [Desktop Entry]
   Version=1.0
   Type=Application
   Name=PostmanClone
   Comment=API Testing Tool
   Exec=$HOME/Applications/postmanclone/PostmanClone.App
   Icon=postmanclone
   Terminal=false
   Categories=Development;
   EOF
   ```

### Prerequisites

- **Linux kernel 4.4 or later**
- **.NET 10.0 Runtime** (usually installed automatically with the package)
  - Manual installation:
    ```bash
    # Ubuntu/Debian
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-runtime-10.0
    ```

---

## macOS Installation

### Method 1: DMG Installer - Recommended

1. **Download** the latest `PostmanClone-1.0.0.dmg` from the [Releases](https://github.com/chris17453/api_rig/releases) page

2. **Open** the DMG file by double-clicking it

3. **Drag** the PostmanClone.app to your Applications folder

4. **First Launch**:
   - Open Finder → Applications
   - Right-click on PostmanClone.app and select "Open"
   - Click "Open" in the security dialog
   - (This is required for unsigned applications on macOS)

5. **Subsequent Launches**:
   - Double-click PostmanClone.app from Applications
   - Or use Spotlight (Cmd+Space) and type "PostmanClone"

### Method 2: Manual Installation

1. **Download** the `.app.zip` archive

2. **Extract** the archive:
   ```bash
   unzip PostmanClone.app.zip
   ```

3. **Move** to Applications:
   ```bash
   mv PostmanClone.app /Applications/
   ```

4. **Remove quarantine** (for unsigned apps):
   ```bash
   xattr -cr /Applications/PostmanClone.app
   ```

5. **Launch** from Applications folder or Spotlight

### Prerequisites

- **macOS 10.15 (Catalina) or later**
- **.NET 10.0 Runtime**
  - Download from: [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
  - Or install via Homebrew:
    ```bash
    brew install --cask dotnet-sdk
    ```

---

## Running from Source

If you prefer to build and run from source:

### Prerequisites

- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Git**

### Steps

1. **Clone** the repository:
   ```bash
   git clone https://github.com/chris17453/api_rig.git
   cd api_rig
   ```

2. **Restore** dependencies:
   ```bash
   dotnet restore
   ```

3. **Build** the solution:
   ```bash
   dotnet build -c Release
   ```

4. **Run** the application:
   ```bash
   dotnet run --project src/PostmanClone.App/PostmanClone.App.csproj -c Release
   ```

---

## System Requirements

### Minimum Requirements

| Component | Requirement |
|-----------|-------------|
| **OS** | Windows 10 1809+, macOS 10.15+, Linux (kernel 4.4+) |
| **CPU** | x64 or ARM64 processor, 1 GHz or faster |
| **RAM** | 512 MB minimum, 2 GB recommended |
| **Disk Space** | 200 MB for application files |
| **Display** | 1024x768 minimum resolution |
| **.NET Runtime** | .NET 10.0 or later |

### Recommended Requirements

| Component | Recommendation |
|-----------|----------------|
| **RAM** | 4 GB or more |
| **Disk Space** | 1 GB for application and data |
| **Display** | 1920x1080 or higher |
| **Network** | Internet connection for API testing |

---

## Troubleshooting

### Windows

#### "This app can't run on your PC"
- **Solution**: Install .NET 10.0 Runtime from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)

#### Application doesn't start
- **Check**: Windows Defender or antivirus may be blocking the application
- **Solution**: Add PostmanClone to the exclusion list

#### "Windows protected your PC" SmartScreen warning
- **Solution**: Click "More info" → "Run anyway"
- This appears because the application is not digitally signed

### Linux

#### "Permission denied" when running
- **Solution**: 
  ```bash
  chmod +x PostmanClone.App
  ```

#### Application doesn't start or crashes
- **Check**: Ensure .NET runtime is installed
  ```bash
  dotnet --list-runtimes
  ```
- **Solution**: Install .NET 10.0 runtime if missing

#### Missing dependencies
- **Solution** for Debian/Ubuntu:
  ```bash
  sudo apt-get install -y libicu-dev libssl-dev
  ```

### macOS

#### "PostmanClone.app is damaged and can't be opened"
- **Solution**: Remove the quarantine flag:
  ```bash
  xattr -cr /Applications/PostmanClone.app
  ```

#### Application doesn't appear in Launchpad
- **Solution**: Restart the Dock:
  ```bash
  killall Dock
  ```

#### "PostmanClone can't be opened because Apple cannot check it for malicious software"
- **Solution**: 
  1. System Preferences → Security & Privacy
  2. Click "Open Anyway" for PostmanClone
  3. Or right-click the app and select "Open"

---

## Uninstallation

### Windows

1. **Control Panel** → Programs and Features → PostmanClone → Uninstall
2. Or run the uninstaller: `C:\Program Files\PostmanClone\Uninstall.exe`
3. Delete any remaining data: `%APPDATA%\PostmanClone`

### Linux (Debian/Ubuntu)

```bash
sudo dpkg -r postmanclone
# Remove configuration and data
rm -rf ~/.config/postmanclone
```

### Linux (Generic)

```bash
rm -rf ~/Applications/postmanclone
rm ~/.local/share/applications/postmanclone.desktop
rm -rf ~/.config/postmanclone
```

### macOS

1. Move `PostmanClone.app` to Trash
2. Remove data:
   ```bash
   rm -rf ~/Library/Application\ Support/PostmanClone
   ```

---

## Getting Help

- **Documentation**: [https://github.com/chris17453/api_rig/blob/main/README.MD](https://github.com/chris17453/api_rig/blob/main/README.MD)
- **Issues**: [https://github.com/chris17453/api_rig/issues](https://github.com/chris17453/api_rig/issues)
- **Discussions**: [https://github.com/chris17453/api_rig/discussions](https://github.com/chris17453/api_rig/discussions)

---

## License

PostmanClone is licensed under the MIT License. See [LICENSE](LICENSE) for details.
