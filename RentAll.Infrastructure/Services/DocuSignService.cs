using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.ESignature;

namespace RentAll.Infrastructure.Services;

public class DocuSignService : IDocuSignService
{
    private const string RentAllClientIdSecretName = "docusign-client-id";
    private const string RentAllPrivateKeySecretName = "docusign-private-key";

    private readonly DocuSignSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DocuSignService> _logger;

    public DocuSignService(
        IOptions<DocuSignSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<DocuSignService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DocuSignEnvelopeResult> SendEnvelopeAsync(
        string? companyName,
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        if (string.IsNullOrWhiteSpace(companyName))
            throw new InvalidOperationException("Organization name is required for DocuSign configuration.");

        if (signers.Count == 0)
            throw new ArgumentException("At least one signer is required.", nameof(signers));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        var docuSignSecretName = BuildDocuSignTenantSecretName(companyName);
        var credentials = await GetCredentialsFromKeyVaultAsync(docuSignSecretName, cancellationToken);
        var accessToken = await RequestAccessTokenAsync(credentials, cancellationToken);

        var envelopeId = await CreateEnvelopeAsync(
            credentials,
            accessToken,
            pdfBytes,
            fileName,
            subject,
            signers,
            cancellationToken);

        return new DocuSignEnvelopeResult
        {
            EnvelopeId = envelopeId,
            Status = "sent"
        };
    }

    public static string AppendDocuSignAnchors(string htmlContent, int signerCount)
    {
        if (signerCount <= 0 || string.IsNullOrWhiteSpace(htmlContent))
            return htmlContent;

        var missingAnchorIndexes = Enumerable.Range(1, signerCount)
            .Where(index => !ContainsDocuSignAnchor(htmlContent, index))
            .ToList();

        if (missingAnchorIndexes.Count == 0)
            return htmlContent;

        var anchors = string.Concat(missingAnchorIndexes.Select(index =>
            $"<div style=\"color:transparent;font-size:1px;line-height:1px;height:1px;overflow:hidden;\">/ds{index}/</div>"));

        if (htmlContent.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.Replace(
                htmlContent,
                "</body>",
                $"{anchors}</body>",
                RegexOptions.IgnoreCase);
        }

        return htmlContent + anchors;
    }

    private static bool ContainsDocuSignAnchor(string htmlContent, int signerIndex)
    {
        return htmlContent.Contains($"/ds{signerIndex}/", StringComparison.Ordinal);
    }

    private async Task<DocuSignCredentials> GetCredentialsFromKeyVaultAsync(
        string tenantDocuSignSecretName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.KeyVaultUri))
            throw new InvalidOperationException("DocuSignSettings:KeyVaultUri is not set.");

        var client = new SecretClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());

        var clientIdSecret = await client.GetSecretAsync(RentAllClientIdSecretName, cancellationToken: cancellationToken);
        var privateKeySecret = await client.GetSecretAsync(RentAllPrivateKeySecretName, cancellationToken: cancellationToken);
        var tenantSecret = await client.GetSecretAsync(tenantDocuSignSecretName, cancellationToken: cancellationToken);

        var clientId = clientIdSecret.Value.Value;
        var privateKey = privateKeySecret.Value.Value;
        var tenantSecretValue = tenantSecret.Value.Value;

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException($"DocuSign Key Vault secret '{RentAllClientIdSecretName}' is empty.");

        if (string.IsNullOrWhiteSpace(privateKey))
            throw new InvalidOperationException($"DocuSign Key Vault secret '{RentAllPrivateKeySecretName}' is empty.");

        if (string.IsNullOrWhiteSpace(tenantSecretValue))
            throw new InvalidOperationException($"DocuSign tenant Key Vault secret '{tenantDocuSignSecretName}' is empty.");

        DocuSignTenantCredentials? tenantCredentials;
        try
        {
            tenantCredentials = JsonSerializer.Deserialize<DocuSignTenantCredentials>(
                tenantSecretValue,
                CredentialJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"DocuSign tenant Key Vault secret '{tenantDocuSignSecretName}' must be JSON with userId, accountId, and baseUri.",
                ex);
        }

        if (tenantCredentials == null
            || string.IsNullOrWhiteSpace(tenantCredentials.UserId)
            || string.IsNullOrWhiteSpace(tenantCredentials.AccountId)
            || string.IsNullOrWhiteSpace(tenantCredentials.BaseUri))
        {
            throw new InvalidOperationException(
                $"DocuSign tenant Key Vault secret '{tenantDocuSignSecretName}' is missing required fields.");
        }

        return new DocuSignCredentials
        {
            ClientId = clientId.Trim(),
            PrivateKey = privateKey,
            UserId = tenantCredentials.UserId.Trim(),
            AccountId = tenantCredentials.AccountId.Trim(),
            BaseUri = tenantCredentials.BaseUri.Trim().TrimEnd('/')
        };
    }

    private async Task<string> RequestAccessTokenAsync(
        DocuSignCredentials credentials,
        CancellationToken cancellationToken)
    {
        try
        {
            var jwt = CreateJwtAssertion(credentials);
            var httpClient = _httpClientFactory.CreateClient(nameof(DocuSignService));
            var tokenUrl = $"https://{_settings.AuthServer}/oauth/token";

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = jwt
                })
            };

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    FormatDocuSignApiError(
                        "token endpoint",
                        response.StatusCode,
                        body,
                        credentials.ClientId,
                        _settings.AuthServer));
            }

            var tokenResponse = JsonSerializer.Deserialize<DocuSignTokenResponse>(body, JsonSerializerOptions);

            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                throw new InvalidOperationException($"DocuSign token endpoint returned an empty access token. Response: {body}");

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DocuSign RequestAccessTokenAsync failed. AuthServer: {AuthServer}, ClientId: {ClientId}, UserId: {UserId}, AccountId: {AccountId}",
                _settings.AuthServer,
                credentials.ClientId,
                credentials.UserId,
                credentials.AccountId);

            if (ex is InvalidOperationException)
                throw;

            var detail = ex.InnerException == null
                ? ex.Message
                : $"{ex.Message} Inner: {ex.InnerException.Message}";

            throw new InvalidOperationException(
                $"DocuSign token request failed: {detail}",
                ex);
        }
    }

    private async Task<string> CreateEnvelopeAsync(
        DocuSignCredentials credentials,
        string accessToken,
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        CancellationToken cancellationToken)
    {
        var orderedSigners = signers
            .OrderBy(signer => signer.RoutingOrder)
            .ThenBy(signer => signer.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var envelopeSigners = orderedSigners.Select((signer, index) => new DocuSignEnvelopeSigner
        {
            Email = signer.Email.Trim(),
            Name = signer.Name.Trim(),
            RecipientId = (index + 1).ToString(),
            RoutingOrder = signer.RoutingOrder.ToString(),
            Tabs = new DocuSignSignerTabs
            {
                SignHereTabs =
                [
                    new DocuSignSignHereTab
                    {
                        DocumentId = "1",
                        AnchorString = $"/ds{index + 1}/",
                        AnchorUnits = "pixels",
                        AnchorXOffset = "20",
                        AnchorYOffset = "10"
                    }
                ]
            }
        }).ToList();

        var envelopeRequest = new DocuSignEnvelopeRequest
        {
            EmailSubject = subject.Trim(),
            Documents =
            [
                new DocuSignDocument
                {
                    DocumentBase64 = Convert.ToBase64String(pdfBytes),
                    Name = string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName,
                    FileExtension = "pdf",
                    DocumentId = "1"
                }
            ],
            Recipients = new DocuSignRecipients
            {
                Signers = envelopeSigners
            },
            Status = "sent"
        };

        var apiBaseUrl = $"{credentials.BaseUri}/restapi";

        var httpClient = _httpClientFactory.CreateClient(nameof(DocuSignService));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{apiBaseUrl}/v2.1/accounts/{credentials.AccountId}/envelopes")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(envelopeRequest, JsonSerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "DocuSign envelope create failed. StatusCode: {StatusCode}; Response: {ResponseBody}",
                response.StatusCode,
                body);

            throw new InvalidOperationException(
                FormatDocuSignApiError("envelope API", response.StatusCode, body));
        }

        var envelopeResponse = JsonSerializer.Deserialize<DocuSignEnvelopeResponse>(body, JsonSerializerOptions);

        if (envelopeResponse == null || string.IsNullOrWhiteSpace(envelopeResponse.EnvelopeId))
            throw new InvalidOperationException("DocuSign envelope creation returned an empty envelope id.");

        return envelopeResponse.EnvelopeId;
    }

    private string CreateJwtAssertion(DocuSignCredentials credentials)
    {
        try
        {
            using var rsa = RSA.Create();
            ImportDocuSignPrivateKey(rsa, credentials.PrivateKey);

            // Copy key material so signing is not tied to the disposable RSA instance.
            var signingCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true)),
                SecurityAlgorithms.RsaSha256);

            var now = DateTime.UtcNow;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = credentials.ClientId,
                Audience = _settings.AuthServer,
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, credentials.UserId),
                    new Claim("scope", "signature impersonation")
                ]),
                IssuedAt = now,
                Expires = now.AddMinutes(55),
                SigningCredentials = signingCredentials
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(tokenDescriptor));
        }
        catch (Exception ex)
        {
            var keyDiagnostics = DescribePrivateKeyDiagnostics(credentials.PrivateKey);

            _logger.LogError(
                ex,
                "Failed to build DocuSign JWT assertion. PrivateKey secret '{SecretName}'. {KeyDiagnostics}",
                RentAllPrivateKeySecretName,
                keyDiagnostics);

            throw new InvalidOperationException(
                $"DocuSign JWT signing failed before the token HTTP call. {keyDiagnostics} " +
                $"Store the full RSA private key from DocuSign in Key Vault secret '{RentAllPrivateKeySecretName}' " +
                "(including -----BEGIN ... PRIVATE KEY----- headers). Details: {GetExceptionDetail(ex)}",
                ex);
        }
    }

    private static void ImportDocuSignPrivateKey(RSA rsa, string privateKey)
    {
        if (string.IsNullOrWhiteSpace(privateKey))
            throw new InvalidOperationException("DocuSign private key is empty.");

        var normalized = NormalizePrivateKeyText(privateKey);
        var importErrors = new List<string>();

        if (normalized.Contains("BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                rsa.ImportFromPem(normalized);
                return;
            }
            catch (Exception ex)
            {
                importErrors.Add($"ImportFromPem failed: {GetExceptionDetail(ex)}");
            }

            var pemBody = ExtractPemBody(normalized);
            if (!string.IsNullOrWhiteSpace(pemBody))
            {
                try
                {
                    ImportRawPrivateKey(rsa, pemBody);
                    return;
                }
                catch (Exception ex)
                {
                    importErrors.Add($"PEM body import failed: {GetExceptionDetail(ex)}");
                }
            }
        }
        else
        {
            try
            {
                ImportRawPrivateKey(rsa, normalized);
                return;
            }
            catch (Exception ex)
            {
                importErrors.Add($"Raw base64 import failed: {GetExceptionDetail(ex)}");
            }
        }

        throw new CryptographicException(
            "DocuSign private key could not be imported as RSA. " + string.Join(" ", importErrors));
    }

    private static void ImportRawPrivateKey(RSA rsa, string base64KeyMaterial)
    {
        var rawKey = Convert.FromBase64String(StripBase64Whitespace(base64KeyMaterial));
        try
        {
            rsa.ImportRSAPrivateKey(rawKey, out _);
        }
        catch (CryptographicException)
        {
            rsa.ImportPkcs8PrivateKey(rawKey, out _);
        }
    }

    private static string ExtractPemBody(string pemText)
    {
        var match = Regex.Match(
            pemText,
            @"-----BEGIN [^-]+-----\s*(?<body>[A-Za-z0-9+/=\s]+?)\s*-----END [^-]+-----",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["body"].Value : string.Empty;
    }

    private static string DescribePrivateKeyDiagnostics(string privateKey)
    {
        if (string.IsNullOrWhiteSpace(privateKey))
            return "KeyVault value is empty.";

        var normalized = NormalizePrivateKeyText(privateKey);
        var hasPemHeader = normalized.Contains("BEGIN", StringComparison.OrdinalIgnoreCase);
        var hasPemFooter = normalized.Contains("END", StringComparison.OrdinalIgnoreCase);
        var lineCount = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        return
            $"Key length: {privateKey.Length} chars; PEM header: {hasPemHeader}; PEM footer: {hasPemFooter}; line count: {lineCount}.";
    }

    private static string GetExceptionDetail(Exception ex)
    {
        return ex.InnerException == null
            ? ex.Message
            : $"{ex.Message} ({ex.InnerException.Message})";
    }

    private static string NormalizePrivateKeyText(string privateKey)
    {
        return privateKey
            .Trim()
            .Trim('\uFEFF')
            .Trim('"')
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static string StripBase64Whitespace(string value)
    {
        return value
            .Replace("\n", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal)
            .Trim();
    }

    private static string FormatDocuSignApiError(
        string operation,
        System.Net.HttpStatusCode statusCode,
        string body,
        string? clientId = null,
        string? authServer = null)
    {
        var message =
            $"DocuSign {operation} returned {(int)statusCode} ({statusCode}). Response: {body}";

        if (body.Contains("user_not_found", StringComparison.OrdinalIgnoreCase))
        {
            message +=
                " The tenant secret userId is not a valid DocuSign user GUID for this environment. "
                + "In DocuSign Admin (demo: https://apps-d.docusign.com), open Apps and Keys → your app → "
                + "My Account Information and copy API Username (a GUID). Put that in the tenant secret as userId. "
                + "Do not use an email address, Account ID, or a production user ID when AuthServer is account-d.docusign.com.";
        }
        else if (body.Contains("consent_required", StringComparison.OrdinalIgnoreCase)
                 && !string.IsNullOrWhiteSpace(clientId)
                 && !string.IsNullOrWhiteSpace(authServer))
        {
            message +=
                " Grant consent once by signing in as the impersonated DocuSign user and visiting: "
                + BuildDocuSignConsentUrl(authServer, clientId);
        }
        else if (body.Contains("invalid_request", StringComparison.OrdinalIgnoreCase))
        {
            message +=
                " Verify JWT settings: iss=Integration Key (docusign-client-id), sub=User GUID (tenant userId), "
                + $"aud={authServer ?? "account-d.docusign.com or account.docusign.com"} (no https), "
                + "scope=signature impersonation, demo vs prod environment matches your DocuSign account.";
        }

        return message;
    }

    private static string BuildDocuSignConsentUrl(string authServer, string clientId)
    {
        var scope = Uri.EscapeDataString("signature impersonation");
        var redirectUri = Uri.EscapeDataString("https://www.docusign.com");
        return $"https://{authServer}/oauth/auth?response_type=code&scope={scope}&client_id={clientId}&redirect_uri={redirectUri}";
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CredentialJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private sealed class DocuSignCredentials
    {
        public string ClientId { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string BaseUri { get; set; } = string.Empty;
    }

    private sealed class DocuSignTenantCredentials
    {
        public string UserId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string BaseUri { get; set; } = string.Empty;
    }

    private sealed class DocuSignTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class DocuSignEnvelopeRequest
    {
        public string EmailSubject { get; set; } = string.Empty;
        public List<DocuSignDocument> Documents { get; set; } = [];
        public DocuSignRecipients Recipients { get; set; } = new();
        public string Status { get; set; } = "sent";
    }

    private sealed class DocuSignDocument
    {
        public string DocumentBase64 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileExtension { get; set; } = "pdf";
        public string DocumentId { get; set; } = "1";
    }

    private sealed class DocuSignRecipients
    {
        public List<DocuSignEnvelopeSigner> Signers { get; set; } = [];
    }

    private sealed class DocuSignEnvelopeSigner
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string RoutingOrder { get; set; } = "1";
        public DocuSignSignerTabs Tabs { get; set; } = new();
    }

    private sealed class DocuSignSignerTabs
    {
        public List<DocuSignSignHereTab> SignHereTabs { get; set; } = [];
    }

    private sealed class DocuSignSignHereTab
    {
        public string DocumentId { get; set; } = "1";
        public string AnchorString { get; set; } = string.Empty;
        public string AnchorUnits { get; set; } = "pixels";
        public string AnchorXOffset { get; set; } = "0";
        public string AnchorYOffset { get; set; } = "0";
    }

    private sealed class DocuSignEnvelopeResponse
    {
        public string EnvelopeId { get; set; } = string.Empty;
    }

    public static string BuildDocuSignTenantSecretName(string companyName)
    {
        var slug = Regex.Replace(companyName.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("Organization name cannot be used to build a DocuSign tenant secret name.");

        return $"docusign-tenant-{slug}";
    }
}
