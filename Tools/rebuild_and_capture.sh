#!/bin/bash
# Usage: rebuild_and_capture.sh "Scene1,Scene2" delay shots [scenes|noscenes]
cd /c/code-shaurya/Glitch
U="/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Unity.exe"
if [ "$4" != "noscenes" ]; then
  "$U" -batchmode -quit -buildTarget Win64 -projectPath "C:/code-shaurya/Glitch" -executeMethod Glitched.EditorTools.GlitchedBuilder.RebuildScenesMenuBatch -logFile "C:/code-shaurya/Glitch/Logs/scenes.log"
  grep -E "\[GLITCHED\] (Built|Scene .* failed)|error CS" Logs/scenes.log | head
fi
"$U" -batchmode -buildTarget Win64 -projectPath "C:/code-shaurya/Glitch" -executeMethod Glitched.EditorTools.BuildTools.BuildWindowsDevBatch -logFile "C:/code-shaurya/Glitch/Logs/win.log"
grep -E "\[GLITCHED\] Build|Shader error|error CS" Logs/win.log | sort -u | head
S=${GLITCHED_SHOTS:-$(dirname "$0")/../Temp/shots}
rm -rf $S; mkdir -p $S
timeout 400 ./Builds/WindowsDev/GLITCHED.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -capture "$1" -captureDir "$(cygpath -w $S)" -captureDelay ${2:-7} -captureShots ${3:-1} -captureTier ${5:-High} -captureWalk 1 -logFile "$(cygpath -w $S)/player.log" >/dev/null 2>&1
ls $S
grep -iE "exception|error" $S/player.log | grep -v "Configuration" | sort | uniq -c | sort -rn | head -20
