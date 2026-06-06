#!/bin/bash

# Ensure we are in the correct directory
cd "$(dirname "$0")"

echo "=== 1. Chuẩn bị tài nguyên ==="
echo "Cleaning old build artifacts..."
rm -rf build dist mun-ai.app mun-ai.dmg mun-ai.icns

# Tạo cấu trúc thư mục App Bundle với tên mới: mun-ai
echo "Creating App Bundle directory structure..."
APP_DIR="mun-ai.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
LIB_DIR="$RESOURCES_DIR/lib"

mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"
mkdir -p "$LIB_DIR"

# Tạo macOS mun-ai.icns file từ icon.png
echo "Creating macOS .icns file from icon.png..."
sips -s format png icon.png --out icon.png
mkdir -p mun-ai.iconset
sips -z 16 16     icon.png --out mun-ai.iconset/icon_16x16.png
sips -z 32 32     icon.png --out mun-ai.iconset/icon_16x16@2x.png
sips -z 32 32     icon.png --out mun-ai.iconset/icon_32x32.png
sips -z 64 64     icon.png --out mun-ai.iconset/icon_32x32@2x.png
sips -z 128 128   icon.png --out mun-ai.iconset/icon_128x128.png
sips -z 256 256   icon.png --out mun-ai.iconset/icon_128x128@2x.png
sips -z 256 256   icon.png --out mun-ai.iconset/icon_256x256.png
sips -z 512 512   icon.png --out mun-ai.iconset/icon_256x256@2x.png
sips -z 512 512   icon.png --out mun-ai.iconset/icon_512x512.png
sips -z 1024 1024 icon.png --out mun-ai.iconset/icon_512x512@2x.png
iconutil -c icns mun-ai.iconset
rm -rf mun-ai.iconset

echo "=== 2. Cài đặt Python Dependencies vào App Bundle ==="
grep -v -E "customtkinter|pyinstaller|Pillow" requirements.txt > requirements_native.txt
/usr/bin/python3 -m pip install --target "$LIB_DIR" -r requirements_native.txt
rm requirements_native.txt

echo "=== 3. Biên dịch Native SwiftUI App (mun-ai) ==="
echo "Compiling main.swift with swiftc..."
swiftc main.swift -o "$MACOS_DIR/mun-ai" -sdk $(xcrun --show-sdk-path) -parse-as-library

# Copy Info.plist vào bundle
cp Info.plist "$CONTENTS_DIR/"

# Copy tài nguyên cần thiết
cp mun-ai.icns "$RESOURCES_DIR/"
cp icon.png "$RESOURCES_DIR/"
cp trollstore.py "$RESOURCES_DIR/"
cp -R sparserestore "$RESOURCES_DIR/"

echo "=== 4. Đóng gói thành DMG ==="
mkdir -p dist
mv "$APP_DIR" dist/

# Remove existing dmg if any
rm -f dist/mun-ai.dmg

# Tạo file DMG với tên volume và file là mun-ai
hdiutil create -volname "mun-ai" -srcfolder dist/mun-ai.app -ov -format UDZO dist/mun-ai.dmg

echo "=== HOÀN TẤT ==="
echo "DMG được tạo thành công tại: dist/mun-ai.dmg"
