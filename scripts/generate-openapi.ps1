param(
    [string]$OutputPath = "$PSScriptRoot/../artifacts/openapi/swagger.json",
    [ValidateSet("2.0", "3.0")]
    [string]$OpenApiVersion = "3.0"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/..").Path
$output = [System.IO.Path]::GetFullPath($OutputPath)

Push-Location $root
try {
    New-Item -ItemType Directory -Force -Path (Split-Path $output) | Out-Null
    dotnet restore Natures_Chakki_Backend.sln
    dotnet tool restore
    dotnet build API/API.csproj --configuration Release --no-restore
    dotnet tool run swagger tofile `
        --openapiversion $OpenApiVersion `
        --output $output `
        "API/bin/Release/net10.0/API.dll" `
        v1
    Write-Host "OpenAPI document generated at: $output"
}
finally {
    Pop-Location
}
