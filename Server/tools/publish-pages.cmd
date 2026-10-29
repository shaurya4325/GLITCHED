@echo off
rem Builds the GitHub Pages copy of the website and pushes it to the gh-pages branch of the repository.
rem Upload the release files first (see Docs/WEBSITE.md), so the download buttons work.
setlocal
cd /d "%~dp0.."
node tools\build-pages.js %* || exit /b 1
for /f "delims=" %%u in ('git -C .. remote get-url origin') do set ORIGIN=%%u
cd pages-dist
if exist .git rmdir /s /q .git
git init -q -b gh-pages
git add -A
git commit -q -m "Publish the GLITCHED website"
git push -f "%ORIGIN%" gh-pages
rmdir /s /q .git
echo Published. The site updates on GitHub Pages within a minute or two.
