$ErrorActionPreference = "Stop"

Write-Host "Publishing SwiftCare..." -ForegroundColor Cyan

dotnet publish -c Release -o publish

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $PWD "publish\PlaywrightBrowsers"

Write-Host "Installing Playwright Chromium..." -ForegroundColor Cyan

& ".\publish\playwright.ps1" install chromium

if ($LASTEXITCODE -ne 0) {
    throw "Playwright Chromium installation failed."
}

$chrome = Get-ChildItem `
    ".\publish\PlaywrightBrowsers" `
    -Filter "chrome.exe" `
    -Recurse `
    -ErrorAction SilentlyContinue

if (-not $chrome) {
    throw "chrome.exe was not found after Playwright installation."
}

Write-Host ""
Write-Host "SUCCESS!" -ForegroundColor Green
Write-Host "Chromium: $($chrome.FullName)" -ForegroundColor Green