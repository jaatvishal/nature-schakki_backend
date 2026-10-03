param(
    [Parameter(Mandatory = $false)]
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Set ConnectionStrings__DefaultConnection or pass -ConnectionString. The value is never written to source control."
}

$previousConnection = $env:ConnectionStrings__DefaultConnection

try {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString

    dotnet restore Natures_Chakki_Backend.sln
    dotnet tool restore
    dotnet build Natures_Chakki_Backend.sln --configuration Release --no-restore

    Write-Host "Applying commerce migrations..."
    dotnet ef database update `
        --project Infrastructure `
        --startup-project API `
        --context StoreContext `
        --no-build `
        --configuration Release

    Write-Host "Applying Identity migrations..."
    dotnet ef database update `
        --project Infrastructure `
        --startup-project API `
        --context AppIdentityDbContext `
        --no-build `
        --configuration Release

    Write-Host "Azure SQL schema deployment completed."
}
finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
}
