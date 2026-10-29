# GLITCHED Server launcher (the desktop shortcut runs this through "GLITCHED Server.vbs", without a console).
# Starts the server, opens the website in its own window and stops the server when that window is closed.
# If the server is already running it only opens another window.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$server = Split-Path -Parent $PSScriptRoot
$data = Join-Path $server 'data'
New-Item -ItemType Directory -Force $data | Out-Null
$infoFile = Join-Path $data 'server.json'

function Show-Error($text) {
    [System.Windows.Forms.MessageBox]::Show($text, 'GLITCHED Server', 'OK', 'Error') | Out-Null
}

function Get-RunningServer {
    if (-not (Test-Path $infoFile)) { return $null }
    try {
        $info = Get-Content $infoFile -Raw | ConvertFrom-Json
        if (-not (Get-Process -Id $info.pid -ErrorAction SilentlyContinue)) { return $null }
        $hello = Invoke-RestMethod -Uri "http://127.0.0.1:$($info.port)/api/v1/hello" -TimeoutSec 2
        if ($hello.glitched -eq 1) { return $info }
    } catch { }
    return $null
}

function Find-Browser {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
    return $null
}

$node = (Get-Command node -ErrorAction SilentlyContinue).Source
if (-not $node) {
    Show-Error "Node.js is not installed.`n`nInstall the LTS version from https://nodejs.org, then click the GLITCHED Server shortcut again."
    exit 1
}

$info = Get-RunningServer
$startedHere = $false
$stopToken = [guid]::NewGuid().ToString('N')
if (-not $info) {
    Remove-Item $infoFile -ErrorAction SilentlyContinue
    $env:GLITCHED_STOP_TOKEN = $stopToken
    $proc = Start-Process -FilePath $node -ArgumentList 'server.js', '--stop-with-window' -WorkingDirectory $server -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $data 'server.log') -RedirectStandardError (Join-Path $data 'server-errors.log')
    $startedHere = $true
    for ($i = 0; $i -lt 60 -and -not $info; $i++) {
        Start-Sleep -Milliseconds 250
        if ($proc.HasExited) { break }
        $info = Get-RunningServer
    }
    if (-not $info) {
        $err = ''
        try { $err = Get-Content (Join-Path $data 'server-errors.log') -Raw } catch { }
        Show-Error "The GLITCHED server did not start.`n`n$err"
        exit 1
    }
}

$url = "http://localhost:$($info.port)/?host=1"
$browser = Find-Browser
if (-not $browser) {
    # no Edge/Chrome: the default browser opens the site; the server stops ~12 s after its tab is closed
    Start-Process $url
    exit 0
}

$profileDir = Join-Path $data 'window-profile'
$browserArgs = @("--app=$url", "--user-data-dir=`"$profileDir`"", '--no-first-run', '--no-default-browser-check', '--start-maximized', '--disable-features=Translate')
$win = Start-Process -FilePath $browser -ArgumentList $browserArgs -PassThru
if (-not $startedHere) { exit 0 }

# wait until every process of this window's private browser profile has closed
$win.WaitForExit()
do {
    Start-Sleep -Seconds 1
    $left = Get-CimInstance Win32_Process -Filter "Name='msedge.exe' OR Name='chrome.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine.Contains($profileDir) }
} while ($left)

try {
    Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:$($info.port)/api/local/stop" -Headers @{ 'X-Stop-Token' = $stopToken } -TimeoutSec 3 | Out-Null
} catch { }
Start-Sleep -Seconds 2
if (Get-Process -Id $info.pid -ErrorAction SilentlyContinue) { Stop-Process -Id $info.pid -Force }
