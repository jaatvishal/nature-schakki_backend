param(
    [string]$OutputPath = "$PSScriptRoot/../artifacts/database"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/..").Path
$output = [System.IO.Path]::GetFullPath($OutputPath)
$previousConnection = $env:ConnectionStrings__DefaultConnection

Push-Location $root
try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    # EF needs a syntactically valid provider connection to construct each context.
    # Script generation does not connect to this value.
    $env:ConnectionStrings__DefaultConnection =
        "Server=localhost;Database=NaturesChakkiScriptGeneration;Trusted_Connection=True;TrustServerCertificate=True"

    dotnet restore Natures_Chakki_Backend.sln
    dotnet tool restore
    dotnet build Natures_Chakki_Backend.sln --configuration Release --no-restore

    dotnet ef migrations script --idempotent `
        --project Infrastructure `
        --startup-project API `
        --context StoreContext `
        --no-build `
        --configuration Release `
        --output (Join-Path $output "01-store-context.sql")

    dotnet ef migrations script --idempotent `
        --project Infrastructure `
        --startup-project API `
        --context AppIdentityDbContext `
        --no-build `
        --configuration Release `
        --output (Join-Path $output "02-identity-context.sql")

    Write-Host "Idempotent Azure SQL scripts generated in: $output"
}
finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
    Pop-Location
}
