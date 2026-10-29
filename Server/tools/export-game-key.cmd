@echo off
rem Writes the public half of this server's licence key into the Unity project, so new builds trust this server.
rem Run it once before building the game. The private key stays in Server\data\keys (never commit or share it).
cd /d "%~dp0.."
node server.js --export-game-key "..\Assets\_Glitched\Resources\Licence\licence_key.json"
pause
