# Build offline zip distribution
# Usage: powershell -File build-zip.ps1
$version = "v1.0"
$outputName = "sosialisasi-portalhc-$version.zip"
$sourceDir = $PSScriptRoot

if (Test-Path (Join-Path $sourceDir $outputName)) {
    Remove-Item (Join-Path $sourceDir $outputName) -Force
    Write-Host "Removed existing $outputName"
}

$items = @(
    "index.html",
    "sosialisasi.html",
    "panduan.html",
    "praktik.html",
    "README.md",
    "CHANGELOG.md",
    "assets"
)

$itemPaths = $items | ForEach-Object { Join-Path $sourceDir $_ } | Where-Object { Test-Path $_ }

Compress-Archive -Path $itemPaths -DestinationPath (Join-Path $sourceDir $outputName) -CompressionLevel Optimal

$size = (Get-Item (Join-Path $sourceDir $outputName)).Length / 1MB
Write-Host "Built $outputName ($([math]::Round($size, 2)) MB)"

if ($size -gt 50) {
    Write-Warning "Zip lebih dari 50MB. Pertimbangkan optimize assets."
}
