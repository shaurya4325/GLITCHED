' Runs GLITCHED-Server.ps1 without a console window (the desktop shortcut points here).
Set fso = CreateObject("Scripting.FileSystemObject")
here = fso.GetParentFolderName(WScript.ScriptFullName)
CreateObject("WScript.Shell").Run "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & here & "\GLITCHED-Server.ps1""", 0, False
