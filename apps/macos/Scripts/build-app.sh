#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${ROOT_DIR}"

APP_NAME="Defaulty"
APP_BUNDLE="${ROOT_DIR}/build/${APP_NAME}.app"
CONTENTS="${APP_BUNDLE}/Contents"

swift build -c release --product "${APP_NAME}"

BIN_PATH="$(swift build -c release --show-bin-path)/${APP_NAME}"
if [[ ! -x "${BIN_PATH}" ]]; then
    echo "error: executable not found at ${BIN_PATH}" >&2
    exit 1
fi

rm -rf "${APP_BUNDLE}"
mkdir -p "${CONTENTS}/MacOS" "${CONTENTS}/Resources"

cp "${BIN_PATH}" "${CONTENTS}/MacOS/${APP_NAME}"
chmod +x "${CONTENTS}/MacOS/${APP_NAME}"
cp "${ROOT_DIR}/Resources/Info.plist" "${CONTENTS}/Info.plist"
printf 'APPL????' > "${CONTENTS}/PkgInfo"

if [[ -f "${ROOT_DIR}/Resources/AppIcon.icns" ]]; then
    cp "${ROOT_DIR}/Resources/AppIcon.icns" "${CONTENTS}/Resources/AppIcon.icns"
fi

codesign --force --sign - --timestamp=none "${APP_BUNDLE}"

if codesign --verify --strict "${APP_BUNDLE}" 2>/dev/null; then
    echo "Ad-hoc signature verified."
else
    echo "error: ad-hoc signature verification failed" >&2
    exit 1
fi

echo "Built ${APP_BUNDLE}"
