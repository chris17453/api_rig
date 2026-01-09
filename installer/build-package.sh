#!/bin/bash
# Build script for Linux/macOS packages

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"
PLATFORM="${3:-linux-x64}"

echo -e "${CYAN}=================================="
echo "PostmanClone Package Builder"
echo -e "==================================${NC}"
echo ""

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
SRC_DIR="$ROOT_DIR/src"
APP_PROJECT="$SRC_DIR/PostmanClone.App/PostmanClone.App.csproj"
PUBLISH_DIR="$ROOT_DIR/publish/$PLATFORM"
OUTPUT_DIR="$ROOT_DIR/packages"

echo -e "${CYAN}Root Directory: $ROOT_DIR${NC}"
echo -e "${CYAN}Platform: $PLATFORM${NC}"
echo -e "${CYAN}Publish Directory: $PUBLISH_DIR${NC}"
echo ""

# Step 1: Clean previous build
echo -e "${YELLOW}[1/4] Cleaning previous build...${NC}"
rm -rf "$PUBLISH_DIR"
mkdir -p "$OUTPUT_DIR"

# Step 2: Restore dependencies
echo -e "${YELLOW}[2/4] Restoring dependencies...${NC}"
cd "$ROOT_DIR"
dotnet restore

# Step 3: Publish the application
echo -e "${YELLOW}[3/4] Publishing application for $PLATFORM...${NC}"
dotnet publish "$APP_PROJECT" \
    -c "$CONFIGURATION" \
    -r "$PLATFORM" \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$PUBLISH_DIR"

echo -e "${GREEN}Application published successfully to: $PUBLISH_DIR${NC}"
echo ""

# Step 4: Create platform-specific package
echo -e "${YELLOW}[4/4] Creating package...${NC}"

if [[ "$PLATFORM" == linux-* ]]; then
    # Create tarball for Linux
    PACKAGE_NAME="postmanclone-$VERSION-$PLATFORM.tar.gz"
    PACKAGE_PATH="$OUTPUT_DIR/$PACKAGE_NAME"
    
    cd "$PUBLISH_DIR"
    tar -czf "$PACKAGE_PATH" .
    
    echo -e "${GREEN}Linux package created: $PACKAGE_PATH${NC}"
    
    # Optionally create .deb package (requires dpkg-deb)
    if command -v dpkg-deb &> /dev/null; then
        echo -e "${YELLOW}Creating .deb package...${NC}"
        
        DEB_DIR="$OUTPUT_DIR/postmanclone-deb"
        mkdir -p "$DEB_DIR/DEBIAN"
        mkdir -p "$DEB_DIR/usr/local/bin/postmanclone"
        mkdir -p "$DEB_DIR/usr/share/applications"
        mkdir -p "$DEB_DIR/usr/share/icons/hicolor/256x256/apps"
        
        # Copy files
        cp -r "$PUBLISH_DIR"/* "$DEB_DIR/usr/local/bin/postmanclone/"
        
        # Create control file
        cat > "$DEB_DIR/DEBIAN/control" << EOF
Package: postmanclone
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: PostmanClone Team <support@postmanclone.dev>
Description: A cross-platform Postman clone for API testing
 PostmanClone is a full-featured API testing tool that supports
 importing Postman collections, executing HTTP requests, managing
 environments, and running pre/post scripts with assertions.
EOF

        # Create desktop entry
        cat > "$DEB_DIR/usr/share/applications/postmanclone.desktop" << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=PostmanClone
Comment=API Testing Tool
Exec=/usr/local/bin/postmanclone/PostmanClone.App
Icon=postmanclone
Terminal=false
Categories=Development;
EOF

        # Build .deb package
        DEB_PACKAGE="$OUTPUT_DIR/postmanclone_${VERSION}_amd64.deb"
        dpkg-deb --build "$DEB_DIR" "$DEB_PACKAGE"
        
        # Clean up
        rm -rf "$DEB_DIR"
        
        echo -e "${GREEN}Debian package created: $DEB_PACKAGE${NC}"
    fi
    
elif [[ "$PLATFORM" == osx-* ]]; then
    # Create .app bundle for macOS
    echo -e "${YELLOW}Creating macOS .app bundle...${NC}"
    
    APP_BUNDLE="$OUTPUT_DIR/PostmanClone.app"
    rm -rf "$APP_BUNDLE"
    
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"
    
    # Copy application files
    cp -r "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"
    
    # Create Info.plist
    cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>PostmanClone</string>
    <key>CFBundleDisplayName</key>
    <string>PostmanClone</string>
    <key>CFBundleIdentifier</key>
    <string>com.postmanclone.app</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleExecutable</key>
    <string>PostmanClone.App</string>
    <key>CFBundleIconFile</key>
    <string>logo.icns</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>
EOF

    # Make executable
    chmod +x "$APP_BUNDLE/Contents/MacOS/PostmanClone.App"
    
    # Create DMG (requires hdiutil)
    echo -e "${YELLOW}Creating DMG installer...${NC}"
    DMG_NAME="PostmanClone-$VERSION.dmg"
    DMG_PATH="$OUTPUT_DIR/$DMG_NAME"
    
    hdiutil create -volname "PostmanClone" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$DMG_PATH" 2>/dev/null || {
        echo -e "${YELLOW}Warning: Could not create DMG (hdiutil not available)${NC}"
    }
    
    echo -e "${GREEN}macOS app bundle created: $APP_BUNDLE${NC}"
    if [ -f "$DMG_PATH" ]; then
        echo -e "${GREEN}DMG installer created: $DMG_PATH${NC}"
    fi
fi

echo ""
echo -e "${GREEN}=================================="
echo "Build completed successfully!"
echo -e "==================================${NC}"
echo ""
echo -e "${CYAN}Published application: $PUBLISH_DIR${NC}"
echo -e "${CYAN}Packages: $OUTPUT_DIR${NC}"
echo ""
