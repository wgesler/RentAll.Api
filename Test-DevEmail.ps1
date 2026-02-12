param(
	[Parameter(Mandatory = $true)]
	[string]$BaseUrl,

	[Parameter(Mandatory = $true)]
	[string]$Username,

	[Parameter(Mandatory = $true)]
	[string]$Password,

	[Parameter(Mandatory = $true)]
	[string]$ToEmail,

	[string]$ToName = "",
	[string]$Subject = "RentAll SendGrid Local Test",
	[string]$PlainTextContent = "This is a test email from RentAll.",
	[string]$HtmlContent = "<p>This is a test email from <strong>RentAll</strong>.</p>",
	[switch]$SkipCertificateCheck
)

$ErrorActionPreference = "Stop"

function Invoke-RentAllRequest {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Method,
		[Parameter(Mandatory = $true)]
		[string]$Uri,
		[object]$Body,
		[hashtable]$Headers
	)

	$params = @{
		Method      = $Method
		Uri         = $Uri
		ContentType = "application/json"
	}

	if ($null -ne $Body) {
		$params.Body = ($Body | ConvertTo-Json -Depth 8)
	}

	if ($null -ne $Headers) {
		$params.Headers = $Headers
	}

	if ($SkipCertificateCheck -and $PSVersionTable.PSVersion.Major -ge 7) {
		$params.SkipCertificateCheck = $true
	}

	return Invoke-RestMethod @params
}

try {
	Write-Host "Logging in to RentAll API at $BaseUrl ..."
	$loginResponse = Invoke-RentAllRequest -Method "Post" -Uri "$BaseUrl/api/auth/login" -Body @{
		username = $Username
		password = $Password
	}

	if (-not $loginResponse.accessToken) {
		throw "Login succeeded but no accessToken was returned."
	}

	$accessToken = $loginResponse.accessToken
	Write-Host "Login successful. Sending test email to $ToEmail ..."

	$emailResponse = Invoke-RentAllRequest -Method "Post" -Uri "$BaseUrl/api/dev/email/test" -Headers @{
		Authorization = "Bearer $accessToken"
	} -Body @{
		toEmail          = $ToEmail
		toName           = $ToName
		subject          = $Subject
		plainTextContent = $PlainTextContent
		htmlContent      = $HtmlContent
	}

	Write-Host "Success!"
	$emailResponse | ConvertTo-Json -Depth 8
	exit 0
}
catch {
	Write-Error "Test failed: $($_.Exception.Message)"
	exit 1
}
