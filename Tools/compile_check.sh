#!/bin/bash
# Compiles the GLITCHED scripts outside Unity (same Roslyn compiler) to catch errors fast.
E="/c/Program Files/Unity/Hub/Editor/6000.0.85f1/Editor/Data"
P=/c/code-shaurya/Glitch
OUT=${GLITCHED_CC_OUT:-$P/Temp/compile_check}
mkdir -p $OUT
win() { cygpath -w "$1"; }
NJ=$(find $P/Library/PackageCache -path "*newtonsoft-json*/Runtime/Newtonsoft.Json.dll" | grep -v AOT | head -1)
{
  echo "-nologo -target:library -langversion:9 -nowarn:0618,0649,0414,0219,0067,0162,0168 -define:UNITY_STANDALONE_WIN -unsafe-"
  echo "-out:\"$(win $OUT/Runtime.dll)\""
  for f in "$E"/NetStandard/ref/2.1.0/*.dll; do echo "-r:\"$(win "$f")\""; done
  for f in "$E"/Managed/UnityEngine/*.dll; do case "$f" in *UnityEditor*) ;; *) echo "-r:\"$(win "$f")\"";; esac; done
  for f in $P/Library/ScriptAssemblies/*.dll; do case "$f" in *Editor*|*Assembly-CSharp*|*Tests*|*TestFramework*|*CodeGen*|*DocCodeSamples*) ;; *) echo "-r:\"$(win $f)\"";; esac; done
  echo "-r:\"$(win $NJ)\""
  find $P/Assets/_Glitched/Scripts -name "*.cs" -not -path "*/Editor/*" | while read f; do echo "\"$(win "$f")\""; done
} > $OUT/runtime.rsp
"$E/NetCoreRuntime/dotnet.exe" "$E/DotNetSdkRoslyn/csc.dll" @"$(win $OUT/runtime.rsp)" 2>&1 | grep -E "error|warning CS0(108|114)" | sed -E 's/.*Scripts.//' | sort -u | head -${1:-80}
echo "runtime done: $(ls -la $OUT/Runtime.dll 2>/dev/null | awk '{print $5}')"
if [ -f $OUT/Runtime.dll ] && [ "$2" != "noeditor" ]; then
{
  echo "-nologo -target:library -langversion:9 -nowarn:0618,0649,0414,0219,0067 -define:UNITY_EDITOR"
  echo "-out:\"$(win $OUT/Editor.dll)\""
  for f in "$E"/NetStandard/ref/2.1.0/*.dll; do echo "-r:\"$(win "$f")\""; done
  for f in "$E"/Managed/UnityEngine/*.dll; do echo "-r:\"$(win "$f")\""; done
  for f in $P/Library/ScriptAssemblies/*.dll; do case "$f" in *Assembly-CSharp*|*Tests*|*TestFramework*|*CodeGen*|*DocCodeSamples*) ;; *) echo "-r:\"$(win $f)\"";; esac; done
  echo "-r:\"$(win $NJ)\""
  echo "-r:\"$(win $OUT/Runtime.dll)\""
  find $P/Assets/_Glitched/Scripts/Editor -name "*.cs" | while read f; do echo "\"$(win "$f")\""; done
} > $OUT/editor.rsp
"$E/NetCoreRuntime/dotnet.exe" "$E/DotNetSdkRoslyn/csc.dll" @"$(win $OUT/editor.rsp)" 2>&1 | grep -E "error" | sed -E 's/.*Scripts.//' | sort -u | head -60
echo "editor done"
fi
