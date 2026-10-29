@echo off
rem Forgets the GLITCHED admin password. Then open http://localhost:8080/admin on this PC to create a new one.
rem Verified copies, settings and the licence key are kept.
cd /d "%~dp0.."
node server.js --reset-password
pause
