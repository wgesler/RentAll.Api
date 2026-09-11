# Creates Azure Document Intelligence (Form Recognizer) and stores endpoint + API key in Key Vault.
# Matches RentAll secrets: document-intelligence-api-key, document-intelligence-endpoint
#
# Usage (from repo root):
#   pwsh -File RentAll.Api/scripts/Setup-DocumentIntelligence.ps1
#
# Optional overrides:
#   -ResourceGroup rg-rentall-dev
#   -AccountName rentall-document-intelligence
#   -Location centralus
#   -KeyVaultName rentall-kv-dev

[CmdletBinding()]
param(
    [string] $ResourceGroup = "rg-rentall-dev",
    [string] $AccountName = "rentall-document-intelligence",
    [string] $Location = "centralus",
    [string] $KeyVaultName = "rentall-kv-dev",
    [string] $ApiKeySecretName = "document-intelligence-api-key",
    [string] $EndpointSecretName = "document-intelligence-endpoint"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking Azure login..."
$account = az account show -o json | ConvertFrom-Json
if (-not $account) {
    throw "Run 'az login' first."
}

Write-Host "Subscription: $($account.name)"

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    $existingJson = az cognitiveservices account show `
        --name $AccountName `
        --resource-group $ResourceGroup `
        -o json 2>$null
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($existingJson)) {
    Write-Host "Ensuring Microsoft.CognitiveServices provider is registered..."
    az provider register --namespace Microsoft.CognitiveServices --wait | Out-Null

    Write-Host "Creating Document Intelligence account '$AccountName' in '$ResourceGroup'..."
    az cognitiveservices account create `
        --name $AccountName `
        --resource-group $ResourceGroup `
        --kind FormRecognizer `
        --sku S0 `
        --location $Location `
        --yes | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Document Intelligence account '$AccountName'."
    }
}
else {
    Write-Host "Document Intelligence account '$AccountName' already exists."
}

$accountInfoJson = az cognitiveservices account show `
    --name $AccountName `
    --resource-group $ResourceGroup `
    -o json

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accountInfoJson)) {
    throw "Unable to load Document Intelligence account '$AccountName'."
}

$accountInfo = $accountInfoJson | ConvertFrom-Json

$endpoint = $accountInfo.properties.endpoint.TrimEnd('/')
$keys = az cognitiveservices account keys list `
    --name $AccountName `
    --resource-group $ResourceGroup `
    -o json | ConvertFrom-Json

$apiKey = $keys.key1
if ([string]::IsNullOrWhiteSpace($apiKey)) {
    throw "Unable to read key1 from cognitive services account."
}

Write-Host "Storing secrets in Key Vault '$KeyVaultName'..."
az keyvault secret set --vault-name $KeyVaultName --name $EndpointSecretName --value $endpoint | Out-Null
az keyvault secret set --vault-name $KeyVaultName --name $ApiKeySecretName --value $apiKey | Out-Null

Write-Host ""
Write-Host "Done."
Write-Host "Endpoint secret: $EndpointSecretName"
Write-Host "API key secret:  $ApiKeySecretName"
Write-Host ""
Write-Host "appsettings DocumentIntelligenceSettings can keep Endpoint/ApiKey empty and use Key Vault."
Write-Host "Optional local override for Development:"
Write-Host "  Endpoint: $endpoint"
Write-Host "  ApiKey:   (stored in Key Vault only)"
