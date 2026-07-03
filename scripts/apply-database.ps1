# Applies legacy EF Core migrations to your local SQL Server database (SSMS).
# Default: Windows auth, database db_ac153b_idealweightdb on (local).

param(
    [string]$Server = ".",
    [string]$Database = "db_ac153b_idealweightdb",
    [string]$SqlUser,
    [string]$SqlPassword
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dataAccess = Join-Path $repoRoot "IdealWeightNutrition.DataAccess\IdealWeightNutrition.DataAccess.csproj"
$startup = Join-Path $repoRoot "IdealWeightNutrition\IdealWeightNutrition.csproj"

if ($SqlUser) {
    $connection = "Server=$Server;Database=$Database;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
} else {
    $connection = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}

Write-Host "Applying migrations to $Server / $Database ..." -ForegroundColor Cyan
dotnet ef database update `
    --project $dataAccess `
    --startup-project $startup `
    --connection $connection

Write-Host "Done. Connect in SSMS with server '$Server' and database '$Database'." -ForegroundColor Green
