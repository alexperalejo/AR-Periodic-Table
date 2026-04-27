#!/usr/bin/env bash
# cleanup.sh — pre-commit cleanup for the AR Periodic Table Unity project.
#
# Removes:
#   • Unity auto-generated folders (Library, Temp, Logs, obj, MemoryCaptures,
#     UserSettings) — Unity recreates these the moment a teammate opens the
#     project.
#   • Xcode/iOS build outputs (build1, build2, …, build8, plus the nested
#     build2/build3/.../build1 chain). These were the cause of the
#     "No space left on device" Xcode failure earlier — they balloon to
#     several GB each.
#   • IDE-generated files (.vs, .idea, *.csproj, *.sln, .DS_Store).
#   • Unused asset folders (Assets/Trellis2, Assets/Trellis2Results,
#     Assets/_Recovery) — confirmed not referenced by any active script or
#     by MainARScene.unity.
#
# Keeps:
#   • Assets/ (everything except the unused folders)
#   • Packages/manifest.json, Packages/packages-lock.json
#   • Packages/com.freetoolsassociation.realtimehand   (REQUIRED — hand tracking)
#   • Packages/com.freetoolsassociation.swiftsupport   (REQUIRED — iOS Swift native)
#   • ProjectSettings/
#   • CLAUDE.md, .gitignore, the docs
#
# Usage:
#   cd "/path/to/AR-Periodic-Table-afnan-backup-work 2"
#   bash cleanup.sh
#
# Run AFTER closing Unity (Library lock issues otherwise).

set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

echo "==> Cleaning Unity auto-generated folders"
rm -rf Library Temp Logs obj MemoryCaptures UserSettings

echo "==> Cleaning Xcode/iOS build outputs"
rm -rf build1 build2 build3 build4 build5 build6 build7 build8 Build Builds

echo "==> Cleaning IDE files"
rm -rf .vs .idea
rm -f  *.csproj *.sln
find . -name ".DS_Store" -type f -delete 2>/dev/null || true

echo "==> Removing unused asset folders"
rm -rf "Assets/Trellis2"            "Assets/Trellis2.meta"
rm -rf "Assets/Trellis2Results"     "Assets/Trellis2Results.meta"
rm -rf "Assets/_Recovery"           "Assets/_Recovery.meta" 2>/dev/null || true

echo "==> Removing AI-bridge dev tooling (now unused)"
# unity-bridge / unity-mcp packages were removed from Packages/manifest.json.
# Their scratch folder and the PowerShell driver are dead weight now.
rm -rf "Assets/LLM"                 "Assets/LLM.meta" 2>/dev/null || true
rm -f  "unity-cmd.ps1"

echo "==> Done."
echo
echo "Disk usage of remaining folders:"
du -sh Assets Packages ProjectSettings 2>/dev/null

echo
echo "Next steps:"
echo "  1. Open the project in Unity once — it will rebuild Library/ from"
echo "     the manifest. This may take a few minutes the first time."
echo "  2. Verify the scene runs (no console errors)."
echo "  3. git add -A && git commit -m 'cleanup: remove build artifacts and unused assets'"
echo "  4. git push"
