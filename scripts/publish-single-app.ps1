param(
    [string]$OutputPath = "$PSScriptRoot/../artifacts/publish"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/..").Path
$output = [System.IO.Path]::GetFullPath($OutputPath)

Push-Location $root
try {
    dotnet restore Natures_Chakki_Backend.sln
    dotnet build Natures_Chakki_Backend.sln --configuration Release --no-restore
    dotnet test Tests/Tests.csproj --configuration Release --no-build
    dotnet publish API/API.csproj `
        --configuration Release `
        --no-build `
        --output $output

    $index = Join-Path $output "wwwroot/index.html"
    if (-not (Test-Path $index)) {
        throw "Angular index.html was not included in the ASP.NET Core publish output."
    }

    Write-Host "Single-App-Service package ready at: $output"
}
finally {
    Pop-Location
}
