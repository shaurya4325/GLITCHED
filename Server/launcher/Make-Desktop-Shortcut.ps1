# Puts a "GLITCHED Server" shortcut on the desktop (run once on the stall laptop).
$here = $PSScriptRoot
$desk = [Environment]::GetFolderPath('Desktop')
$lnk = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $desk 'GLITCHED Server.lnk'))
$lnk.TargetPath = Join-Path $env:WINDIR 'System32\wscript.exe'
$lnk.Arguments = '"' + (Join-Path $here 'GLITCHED Server.vbs') + '"'
$lnk.WorkingDirectory = Split-Path -Parent $here
$lnk.IconLocation = (Join-Path $here 'glitched-server.ico') + ',0'
$lnk.Description = 'Start the GLITCHED website and copy verification. Close its window to stop it.'
$lnk.Save()
Write-Host "Shortcut created on the desktop: GLITCHED Server"
