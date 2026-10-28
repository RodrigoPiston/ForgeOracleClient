#!/usr/bin/env bash
# Builds the plugin in Release and assembles a Thunderstore / r2modman / Gale package:
#   artifacts/ForgeOracleClient-<version>.zip
# Requires the local Valheim install for the game references (VALHEIM_DIR or the default Steam path).
# Game assemblies are referenced at build time only; they are never copied into the package.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=$(python3 -c "import json;print(json.load(open('$ROOT/packaging/manifest.json'))['version_number'])")
OUT="$ROOT/artifacts"
STAGE="$OUT/stage"
BIN="$ROOT/src/ForgeOracle.Plugin/bin/Release/net48"

dotnet build "$ROOT/src/ForgeOracle.Plugin/ForgeOracle.Plugin.csproj" -c Release -p:Version="$VERSION" > "$OUT.build.log" 2>&1 \
  || { tail -30 "$OUT.build.log"; exit 1; }

rm -rf "$STAGE" && mkdir -p "$STAGE"
cp "$BIN/ForgeOracleClient.dll" "$BIN/ForgeOracle.Core.dll" "$STAGE/"
cp "$ROOT/packaging/manifest.json" "$ROOT/packaging/icon.png" "$STAGE/"
cp "$ROOT/CHANGELOG.md" "$ROOT/LICENSE" "$ROOT/THIRD_PARTY_NOTICES.md" "$STAGE/"
# Thunderstore shows the README on its own site, where relative links would break: point them at the repository.
REPO_URL=$(python3 -c "import json;print(json.load(open('$ROOT/packaging/manifest.json'))['website_url'])")
python3 - "$ROOT/README.md" "$STAGE/README.md" "$REPO_URL" <<'PY'
import re, sys
src, dst, repo = sys.argv[1], sys.argv[2], sys.argv[3].rstrip('/')
text = open(src, encoding='utf-8').read()
text = re.sub(r'\]\((?!https?://|#)([^)]+)\)', lambda m: '](' + repo + '/blob/main/' + m.group(1) + ')', text)
open(dst, 'w', encoding='utf-8').write(text)
PY

# Guard: nothing from the game, BepInEx, Harmony or the dev harness may be packaged.
if ls "$STAGE" | grep -E -i 'assembly_|UnityEngine|BepInEx|Harmony|LiveHarness' ; then
  echo "refusing to package third-party or dev-only assemblies" >&2
  exit 1
fi

ZIP="$OUT/ForgeOracleClient-$VERSION.zip"
rm -f "$ZIP"
(cd "$STAGE" && python3 -c "
import os, zipfile, sys
with zipfile.ZipFile(sys.argv[1], 'w', zipfile.ZIP_DEFLATED) as z:
    for name in sorted(os.listdir('.')):
        z.write(name, name)
" "$ZIP")
rm -rf "$STAGE" "$OUT.build.log"
echo "$ZIP"
unzip -l "$ZIP"
