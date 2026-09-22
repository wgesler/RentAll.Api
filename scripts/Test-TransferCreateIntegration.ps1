param(
    [switch]$PreflightOnly
)

$ErrorActionPreference = "Stop"
$env:RENTALL_INTEGRATION_TESTS = "1"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$testProject = Join-Path $repoRoot "RentAll.Api\RentAll.Test\RentAll.Test.csproj"
$filter = "AccountingManagerTransferCreateIntegrationTests"

Write-Host "Running transfer create integration tests against local GESLER database..."
Write-Host "Filter: $filter"
Write-Host ""

if ($PreflightOnly) {
    dotnet test $testProject --filter "FullyQualifiedName~SqlPreflight_SelectedDepositsAndPy953Path_IsReady"
} else {
    dotnet test $testProject --filter $filter
}

exit $LASTEXITCODE
