#!/usr/bin/env bash
# Builds the plugin in Release and assembles the Thunderstore / r2modman / Gale package:
#   artifacts/ForgeOracleClient-<version>.zip
#
#   manifest.json
#   icon.png                256x256
#   README.md               this repository's README, links and screenshots made absolute
#   CHANGELOG.md, LICENSE, THIRD_PARTY_NOTICES.md
#   plugins/ForgeOracleClient.dll, plugins/ForgeOracle.Core.dll
#
# Needs the local Valheim install for the game references (VALHEIM_DIR or the default Steam path). Game assemblies
# are referenced at build time only; they are never copied into the package.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
MANIFEST="$ROOT/thunderstore/manifest.json"
OUT="$ROOT/artifacts"
STAGE="$OUT/stage"
BIN="$ROOT/src/ForgeOracle.Plugin/bin/Release/net48"
field() { python3 -c "import json,sys;print(json.load(open(sys.argv[1]))[sys.argv[2]])" "$MANIFEST" "$1"; }
VERSION=$(field version_number)
REPO_URL=$(field website_url)

# The manifest, the assembly version and the BepInPlugin version must agree.
PROPS_VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props")
PLUGIN_VERSION=$(sed -n 's:.*const string Version = "\(.*\)";.*:\1:p' "$ROOT/src/ForgeOracle.Plugin/ForgeOracleClientPlugin.cs")
if [ "$VERSION" != "$PROPS_VERSION" ] || [ "$VERSION" != "$PLUGIN_VERSION" ]; then
  echo "version mismatch: manifest $VERSION, Directory.Build.props $PROPS_VERSION, plugin $PLUGIN_VERSION" >&2
  exit 1
fi

mkdir -p "$OUT"
dotnet build "$ROOT/src/ForgeOracle.Plugin/ForgeOracle.Plugin.csproj" -c Release > "$OUT/build.log" 2>&1 \
  || { tail -30 "$OUT/build.log"; exit 1; }

rm -rf "$STAGE" && mkdir -p "$STAGE/plugins"
cp "$BIN/ForgeOracleClient.dll" "$BIN/ForgeOracle.Core.dll" "$STAGE/plugins/"
cp "$MANIFEST" "$ROOT/thunderstore/icon.png" "$STAGE/"
cp "$ROOT/CHANGELOG.md" "$ROOT/LICENSE" "$ROOT/THIRD_PARTY_NOTICES.md" "$STAGE/"

# Thunderstore shows the README on its own site and only renders images from absolute URLs: images point at the raw
# files of the repository, other relative links at the repository pages.
python3 - "$ROOT/README.md" "$STAGE/README.md" "$REPO_URL" <<'PY'
import os, re, sys
src, dst, repo = sys.argv[1], sys.argv[2], sys.argv[3].rstrip('/')
raw = repo.replace('https://github.com/', 'https://raw.githubusercontent.com/') + '/main/'
text = open(src, encoding='utf-8').read()
for image in re.findall(r'!\[[^\]]*\]\((?!https?://)([^)]+)\)', text):
    if not os.path.exists(os.path.join(os.path.dirname(src), image)):
        print(f'warning: {image} is missing; it will not show on the mod page', file=sys.stderr)
text = re.sub(r'(!\[[^\]]*\])\((?!https?://)([^)]+)\)', lambda m: f'{m.group(1)}({raw}{m.group(2)})', text)
text = re.sub(r'(?<!!)(\[[^\]]*\])\((?!https?://|#)([^)]+)\)', lambda m: f'{m.group(1)}({repo}/blob/main/{m.group(2)})', text)
open(dst, 'w', encoding='utf-8').write(text)
PY

# Guard: nothing from the game, BepInEx, Harmony or the dev harness may be packaged.
if find "$STAGE" -type f | grep -E -i 'assembly_|UnityEngine|BepInEx|Harmony|LiveHarness'; then
  echo "refusing to package third-party or dev-only assemblies" >&2
  exit 1
fi

ZIP="$OUT/ForgeOracleClient-$VERSION.zip"
rm -f "$ZIP"
(cd "$STAGE" && python3 -c "
import os, zipfile, sys
with zipfile.ZipFile(sys.argv[1], 'w', zipfile.ZIP_DEFLATED) as z:
    for folder, _, files in sorted(os.walk('.')):
        for name in sorted(files):
            path = os.path.join(folder, name)[2:]
            z.write(path, path)
" "$ZIP")
rm -rf "$STAGE" "$OUT/build.log"
echo "$ZIP"
unzip -l "$ZIP"
