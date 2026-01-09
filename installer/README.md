# PostmanClone Installer

This directory contains the installer project for PostmanClone, a cross-platform API testing tool.

## Prerequisites

### Windows Installer (.msi)
- **WiX Toolset 3.11+** - Download from [https://wixtoolset.org/releases/](https://wixtoolset.org/releases/)
- **.NET 10.0 SDK** - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** (optional) - For building with MSBuild

### Linux Package (.deb, .tar.gz)
- **.NET 10.0 SDK**
- **dpkg-deb** (optional, for .deb packages) - Install via `apt-get install dpkg`

### macOS Package (.app, .dmg)
- **.NET 10.0 SDK**
- **Xcode Command Line Tools** - Install via `xcode-select --install`

## Building the Installer

### Windows

Run the PowerShell script from the repository root:

```powershell
.\installer\build-installer.ps1
```

Optional parameters:
```powershell
.\installer\build-installer.ps1 -Configuration Release -Platform win-x64 -Version 1.0.0
```

This will:
1. Restore NuGet packages
2. Publish the application for Windows x64
3. Generate WiX file manifest using Heat
4. Build the Windows Installer (.msi)

Output: `installer\bin\Release\win-x64\PostmanClone-Setup.msi`

### Linux

Run the bash script from the repository root:

```bash
./installer/build-package.sh Release 1.0.0 linux-x64
```

This will:
1. Restore NuGet packages
2. Publish the application for Linux x64
3. Create a .tar.gz archive
4. Create a .deb package (if dpkg-deb is available)

Output: `packages/postmanclone-1.0.0-linux-x64.tar.gz` and `packages/postmanclone_1.0.0_amd64.deb`

### macOS

Run the bash script from the repository root:

```bash
./installer/build-package.sh Release 1.0.0 osx-x64
```

For Apple Silicon (ARM64):
```bash
./installer/build-package.sh Release 1.0.0 osx-arm64
```

This will:
1. Restore NuGet packages
2. Publish the application for macOS
3. Create a .app bundle
4. Create a .dmg installer

Output: `packages/PostmanClone.app` and `packages/PostmanClone-1.0.0.dmg`

## Supported Platforms

| Platform | Runtime ID | Installer Format |
|----------|-----------|------------------|
| Windows x64 | win-x64 | .msi |
| Windows ARM64 | win-arm64 | .msi |
| Linux x64 | linux-x64 | .tar.gz, .deb |
| Linux ARM64 | linux-arm64 | .tar.gz, .deb |
| macOS x64 | osx-x64 | .app, .dmg |
| macOS ARM64 (Apple Silicon) | osx-arm64 | .app, .dmg |

## Manual Build Steps

If you prefer to build manually:

### 1. Publish the Application

```bash
dotnet publish src/PostmanClone.App/PostmanClone.App.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -o publish/win-x64
```

### 2. Build Windows Installer (WiX)

```bash
# Generate file manifest
heat.exe dir publish/win-x64 -cg ProductComponents -dr APPFOLDER -gg -sfrag -srd -sreg -var var.PublishDir -out installer/Files.wxs

# Build installer
msbuild installer/PostmanClone.Installer.wixproj /p:Configuration=Release /p:Platform=x64 /p:DefineConstants="ProductVersion=1.0.0;PublishDir=publish/win-x64"
```

### 3. Create Linux Package

```bash
# Create tarball
cd publish/linux-x64
tar -czf ../../packages/postmanclone-1.0.0-linux-x64.tar.gz .
```

### 4. Create macOS Bundle

```bash
# Create .app structure
mkdir -p packages/PostmanClone.app/Contents/MacOS
cp -r publish/osx-x64/* packages/PostmanClone.app/Contents/MacOS/

# Make executable
chmod +x packages/PostmanClone.app/Contents/MacOS/PostmanClone.App
```

## File Structure

```
installer/
├── Assets/
│   ├── LICENSE.rtf          # License text for installer
│   └── logo.ico             # Application icon
├── PostmanClone.Installer.wixproj  # WiX project file
├── Product.wxs              # Main product definition
├── UI.wxs                   # Installer UI customization
├── Files.wxs                # File manifest (auto-generated)
├── build-installer.ps1      # Windows build script
├── build-package.sh         # Linux/macOS build script
└── README.md               # This file
```

## Customization

### Change Product Version

Edit `Product.wxs` and update:
```xml
<?define ProductVersion = "1.0.0" ?>
```

Or pass as parameter to build script:
```powershell
.\installer\build-installer.ps1 -Version 2.0.0
```

### Change Install Location

Edit `Product.wxs` and modify:
```xml
<Directory Id="INSTALLFOLDER" Name="PostmanClone">
```

### Add/Remove Shortcuts

Edit `Product.wxs` to add or remove shortcut components.

### Customize Installer UI

Edit `UI.wxs` to customize dialogs and appearance.

## Troubleshooting

### WiX Toolset Not Found

Install WiX Toolset from [https://wixtoolset.org/releases/](https://wixtoolset.org/releases/)

Verify installation:
```powershell
heat.exe /?
```

### .NET Runtime Not Found

Ensure .NET 10.0 SDK is installed:
```bash
dotnet --version
```

Should return `10.0.x`

### Permission Denied (Linux/macOS)

Make sure the build script is executable:
```bash
chmod +x installer/build-package.sh
```

### Missing Dependencies

For Linux .deb packages:
```bash
sudo apt-get install dpkg
```

For macOS .dmg creation:
```bash
xcode-select --install
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build Installers

on:
  push:
    tags:
      - 'v*'

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build Windows Installer
        run: .\installer\build-installer.ps1 -Version ${{ github.ref_name }}
      - name: Upload Installer
        uses: actions/upload-artifact@v3
        with:
          name: windows-installer
          path: installer/bin/Release/win-x64/*.msi

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build Linux Package
        run: ./installer/build-package.sh Release ${{ github.ref_name }} linux-x64
      - name: Upload Package
        uses: actions/upload-artifact@v3
        with:
          name: linux-package
          path: packages/*

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build macOS Package
        run: ./installer/build-package.sh Release ${{ github.ref_name }} osx-x64
      - name: Upload Package
        uses: actions/upload-artifact@v3
        with:
          name: macos-package
          path: packages/*
```

## License

MIT License - See LICENSE.rtf for details
