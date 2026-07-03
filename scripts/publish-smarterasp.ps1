param(
    [string]$OutputRoot = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$modernizationRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $modernizationRoot "publish\smarterasp"
}

$apiOut = Join-Path $OutputRoot "api"
$webOut = Join-Path $OutputRoot "web"
$apiProject = Join-Path $modernizationRoot "backend\src\IdealWeightNutrition.Api\IdealWeightNutrition.Api.csproj"
$frontendRoot = Join-Path $modernizationRoot "frontend"
$webConfig = Join-Path $frontendRoot "deploy\smarterasp\web.config"

Write-Host "Publishing API to $apiOut"
if (Test-Path $apiOut) { Remove-Item -Recurse -Force $apiOut }
dotnet publish $apiProject -c $Configuration -o $apiOut /p:UseAppHost=false
if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

Write-Host "Building Angular storefront"
Push-Location $frontendRoot
try {
    if (-not (Test-Path "node_modules")) {
        npm ci
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }
    }
    npm run build:smarterasp
    if ($LASTEXITCODE -ne 0) { throw "Angular build failed." }

    $browserDist = Join-Path $frontendRoot "dist\ideal-weight-nutrition-web\browser"
    if (-not (Test-Path $browserDist)) {
        throw "Build output not found: $browserDist"
    }

    Write-Host "Copying storefront to $webOut"
    if (Test-Path $webOut) { Remove-Item -Recurse -Force $webOut }
    New-Item -ItemType Directory -Path $webOut | Out-Null
    Copy-Item -Path (Join-Path $browserDist "*") -Destination $webOut -Recurse -Force
    Copy-Item -Path $webConfig -Destination (Join-Path $webOut "web.config") -Force

    # Angular SSR builds emit index.csr.html; IIS needs index.html as the default document.
    $indexHtml = Join-Path $webOut "index.html"
    $indexCsr = Join-Path $webOut "index.csr.html"
    if (-not (Test-Path $indexHtml) -and (Test-Path $indexCsr)) {
        Copy-Item -Path $indexCsr -Destination $indexHtml -Force
        Write-Host "Created index.html from index.csr.html for IIS."
    }
    if (-not (Test-Path $indexHtml)) {
        throw "No index.html in publish output. Upload would cause IIS 403."
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Done. Upload folders:"
Write-Host "  API site root  <= $apiOut"
Write-Host "  Web site root  <= $webOut"
Write-Host ""
Write-Host "Next: see docs/DEPLOY-SMARTERASP.md for SmarterASP control panel steps."
