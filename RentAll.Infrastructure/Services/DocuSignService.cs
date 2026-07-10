using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DocuSign.eSign.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.ESignature;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
        string returnUrl,
        string senderEmail,
        string senderName,
        Guid? userId = null,
        Guid? apiAccountId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        if (string.IsNullOrWhiteSpace(companyName))
            throw new InvalidOperationException("Organization name is required for DocuSign configuration.");

        if (signers.Count == 0)
            throw new ArgumentException("At least one signer is required.", nameof(signers));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
            throw new ArgumentException("A valid return URL is required.", nameof(returnUrl));

        if (string.IsNullOrWhiteSpace(senderEmail))
            throw new ArgumentException("Sender email is required.", nameof(senderEmail));

        if (string.IsNullOrWhiteSpace(senderName))
            throw new ArgumentException("Sender name is required.", nameof(senderName));

        var docuSignSecretName = BuildDocuSignTenantSecretName(companyName);
        var credentials = await GetCredentialsAsync(docuSignSecretName, userId, apiAccountId, cancellationToken);
        var accessToken = await RequestAccessTokenAsync(credentials, cancellationToken);

        var envelopeId = await CreateEnvelopeAsync(
            credentials,
            accessToken,
            pdfBytes,
            fileName,
            subject,
            signers,
            cancellationToken);

        var senderViewUrl = await CreateSenderViewAsync(
            credentials,
            accessToken,
            envelopeId,
            returnUrl.Trim(),
            senderEmail.Trim(),
            senderName.Trim(),
            cancellationToken);

        return new DocuSignEnvelopeResult
        {
            EnvelopeId = envelopeId,
            Status = "created",
            SenderViewUrl = senderViewUrl
        };
    }

    private async Task<DocuSignCredentials> GetCredentialsAsync(string tenantDocuSignSecretName, Guid? requestUserId, Guid? requestApiAccountId, CancellationToken cancellationToken)
    {
        SecretClient? keyVaultClient = null;

        async Task<SecretClient> GetKeyVaultClientAsync()
        {
            if (keyVaultClient != null)
                return keyVaultClient;

            if (string.IsNullOrWhiteSpace(_settings.KeyVaultUri))
            {
                throw new InvalidOperationException(
                    "DocuSignSettings:KeyVaultUri is not set and one or more DocuSign credentials are missing from appsettings.");
            }

            keyVaultClient = new SecretClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());
            return keyVaultClient;
        }

        var clientId = await ResolveCredentialValueAsync(
            _settings.ClientId,
            RentAllClientIdSecretName,
            GetKeyVaultClientAsync,
            cancellationToken);

        var privateKey = NormalizePrivateKeyText(
            await ResolveCredentialValueAsync(
                _settings.PrivateKey,
                RentAllPrivateKeySecretName,
                GetKeyVaultClientAsync,
                cancellationToken));

        var userId = FirstNonEmpty(ToGuidStringOrNull(requestUserId), _settings.UserId);
        var accountId = FirstNonEmpty(ToGuidStringOrNull(requestApiAccountId), _settings.AccountId);
        var baseUri = ResolveBaseUri(_settings.BaseUri, null);

        DocuSignTenantCredentials? tenantCredentials = null;
        if (NeedsTenantCredentialsFromKeyVault(userId, accountId, baseUri))
        {
            var client = await GetKeyVaultClientAsync();
            var tenantSecret = await client.GetSecretAsync(tenantDocuSignSecretName, cancellationToken: cancellationToken);
            var tenantSecretValue = tenantSecret.Value.Value;

            if (string.IsNullOrWhiteSpace(tenantSecretValue))
            {
                throw new InvalidOperationException(
                    $"DocuSign tenant Key Vault secret '{tenantDocuSignSecretName}' is empty.");
            }

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

            if (tenantCredentials == null)
            {
                throw new InvalidOperationException(
                    $"DocuSign tenant Key Vault secret '{tenantDocuSignSecretName}' is missing required fields.");
            }

            userId = FirstNonEmpty(userId, tenantCredentials.UserId);
            accountId = FirstNonEmpty(accountId, tenantCredentials.AccountId);
            baseUri = ResolveBaseUri(_settings.BaseUri, tenantCredentials.BaseUri);
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "DocuSign userId is not configured. Set DocuSignSettings:UserId in appsettings or provide it in the tenant Key Vault secret.");
        }

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new InvalidOperationException(
                "DocuSign accountId is not configured. Set DocuSignSettings:AccountId in appsettings or provide it in the tenant Key Vault secret.");
        }

        if (string.IsNullOrWhiteSpace(baseUri))
        {
            throw new InvalidOperationException(
                "DocuSign baseUri is not configured. Set DocuSignSettings:BaseUri in appsettings or provide it in the tenant Key Vault secret.");
        }

        ValidateDocuSignGuid(userId, "userId");
        ValidateDocuSignGuid(accountId, "accountId");
        ValidateDocuSignGuid(clientId, "clientId");

        _logger.LogInformation(
            "DocuSign credentials resolved. AuthServer={AuthServer}, ClientId source: {ClientIdSource}, PrivateKey source: {PrivateKeySource}, UserId source: {UserIdSource}, AccountId source: {AccountIdSource}, Tenant source: {TenantSource}, {KeyDiagnostics}",
            _settings.AuthServer,
            GetCredentialSource(_settings.ClientId),
            GetCredentialSource(_settings.PrivateKey),
            GetDocuSignIdentitySource(requestUserId, _settings.UserId, tenantCredentials?.UserId),
            GetDocuSignIdentitySource(requestApiAccountId, _settings.AccountId, tenantCredentials?.AccountId),
            tenantCredentials != null ? $"KeyVault:{tenantDocuSignSecretName}" : "NotUsed",
            DescribePrivateKeyDiagnostics(privateKey));

        return new DocuSignCredentials
        {
            ClientId = clientId.Trim(),
            PrivateKey = privateKey,
            UserId = userId.Trim(),
            AccountId = accountId.Trim(),
            BaseUri = baseUri.Trim().TrimEnd('/')
        };
    }

    private static bool NeedsTenantCredentialsFromKeyVault(string? userId, string? accountId, string? baseUri)
    {
        return string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(baseUri);
    }

    private static string? ToGuidStringOrNull(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty
            ? value.Value.ToString()
            : null;
    }

    private static string GetDocuSignIdentitySource(Guid? requestValue, string? settingsValue, string? tenantValue)
    {
        if (requestValue.HasValue && requestValue.Value != Guid.Empty)
            return "Office";

        if (!string.IsNullOrWhiteSpace(settingsValue))
            return "AppSettings";

        if (!string.IsNullOrWhiteSpace(tenantValue))
            return "KeyVault";

        return "Missing";
    }

    private async Task<string> ResolveCredentialValueAsync(
        string? localValue,
        string keyVaultSecretName,
        Func<Task<SecretClient>> getKeyVaultClientAsync,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(localValue))
            return localValue;

        var client = await getKeyVaultClientAsync();
        var secret = await client.GetSecretAsync(keyVaultSecretName, cancellationToken: cancellationToken);
        var value = secret.Value.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"DocuSign Key Vault secret '{keyVaultSecretName}' is empty.");
        }

        return value;
    }

    private string ResolveBaseUri(string? settingsBaseUri, string? tenantBaseUri)
    {
        var baseUri = FirstNonEmpty(settingsBaseUri, tenantBaseUri);
        if (!string.IsNullOrWhiteSpace(baseUri))
            return baseUri;

        if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
            return string.Empty;

        var apiBaseUrl = _settings.ApiBaseUrl.Trim().TrimEnd('/');
        const string restApiSuffix = "/restapi";
        if (apiBaseUrl.EndsWith(restApiSuffix, StringComparison.OrdinalIgnoreCase))
            return apiBaseUrl[..^restApiSuffix.Length];

        return apiBaseUrl;
    }

    private static string? FirstNonEmpty(string? primary, string? secondary)
    {
        return !string.IsNullOrWhiteSpace(primary)
            ? primary
            : string.IsNullOrWhiteSpace(secondary) ? null : secondary;
    }

    private static string GetCredentialSource(string? localValue)
    {
        return string.IsNullOrWhiteSpace(localValue) ? "KeyVault" : "AppSettings";
    }

    private async Task<string> RequestAccessTokenAsync(
        DocuSignCredentials credentials,
        CancellationToken cancellationToken)
    {
        try
        {
            var privateKey = NormalizePrivateKeyText(credentials.PrivateKey);
            var privateKeyBytes = Encoding.UTF8.GetBytes(privateKey);
            var scopes = new List<string> { "signature", "impersonation" };

            var docuSignClient = new DocuSignClient();
            var token = await docuSignClient.RequestJWTUserTokenAsync(
                credentials.ClientId,
                credentials.UserId,
                _settings.AuthServer,
                privateKeyBytes,
                1,
                scopes,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(token?.access_token))
                throw new InvalidOperationException("DocuSign SDK returned an empty access token.");

            return token.access_token;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DocuSign SDK JWT failed. AuthServer={AuthServer}, ClientId={ClientId}, UserId={UserId}, AccountId={AccountId}, {KeyDiagnostics}",
                _settings.AuthServer,
                credentials.ClientId,
                credentials.UserId,
                credentials.AccountId,
                DescribePrivateKeyDiagnostics(credentials.PrivateKey));

            var detail = GetExceptionDetail(ex);
            var message = $"DocuSign JWT authentication failed. {detail}";

            if (detail.Contains("consent_required", StringComparison.OrdinalIgnoreCase))
            {
                message +=
                    " Grant consent once by signing in to DocuSign as the user whose GUID is in userId, then visit: "
                    + BuildDocuSignConsentUrl(_settings.AuthServer, credentials.ClientId);
            }
            else
            {
                message +=
                    $" Verify AuthServer '{_settings.AuthServer}' matches your DocuSign environment (demo vs production) "
                    + "and userId is the full API Username GUID from Apps and Keys.";
            }

            throw new InvalidOperationException(message, ex);
        }
    }

    private static void ValidateDocuSignGuid(string value, string fieldName)
    {
        if (!Guid.TryParse(value, out _))
        {
            throw new InvalidOperationException(
                $"DocuSign {fieldName} '{value}' is not a valid GUID. " +
                "Copy the full value from DocuSign Admin → Apps and Keys → My Account Information.");
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
            RoutingOrder = signer.RoutingOrder.ToString()
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
            Status = "created"
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

    private async Task<string> CreateSenderViewAsync(
        DocuSignCredentials credentials,
        string accessToken,
        string envelopeId,
        string returnUrl,
        string senderEmail,
        string senderName,
        CancellationToken cancellationToken)
    {
        var apiBaseUrl = $"{credentials.BaseUri}/restapi";
        var senderViewRequest = new DocuSignSenderViewRequest
        {
            ReturnUrl = returnUrl,
            AuthenticationMethod = "email",
            Email = senderEmail,
            UserName = senderName
        };

        var httpClient = _httpClientFactory.CreateClient(nameof(DocuSignService));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{apiBaseUrl}/v2.1/accounts/{credentials.AccountId}/envelopes/{envelopeId}/views/sender")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(senderViewRequest, JsonSerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "DocuSign sender view failed. StatusCode: {StatusCode}; Response: {ResponseBody}",
                response.StatusCode,
                body);

            throw new InvalidOperationException(
                FormatDocuSignApiError("sender view API", response.StatusCode, body));
        }

        var senderViewResponse = JsonSerializer.Deserialize<DocuSignSenderViewResponse>(body, JsonSerializerOptions);

        if (senderViewResponse == null || string.IsNullOrWhiteSpace(senderViewResponse.Url))
            throw new InvalidOperationException("DocuSign sender view returned an empty URL.");

        return senderViewResponse.Url;
    }

    private string CreateJwtAssertion(DocuSignCredentials credentials)
    {
        try
        {
            using var rsa = RSA.Create();
            ImportDocuSignPrivateKey(rsa, credentials.PrivateKey);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var headerJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["alg"] = "RS256",
                ["typ"] = "JWT"
            });

            var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["iss"] = credentials.ClientId,
                ["sub"] = credentials.UserId,
                ["aud"] = _settings.AuthServer,
                ["iat"] = now,
                ["exp"] = now + 3600,
                ["scope"] = "signature impersonation"
            });

            var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var unsignedToken = $"{header}.{payload}";

            var signatureBytes = rsa.SignData(
                Encoding.UTF8.GetBytes(unsignedToken),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var signature = Base64UrlEncode(signatureBytes);
            return $"{unsignedToken}.{signature}";
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
                $"DocuSign JWT signing failed before the token HTTP call. {keyDiagnostics} Details: {GetExceptionDetail(ex)}",
                ex);
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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
    }

    private sealed class DocuSignEnvelopeResponse
    {
        public string EnvelopeId { get; set; } = string.Empty;
    }

    private sealed class DocuSignSenderViewRequest
    {
        public string ReturnUrl { get; set; } = string.Empty;
        public string AuthenticationMethod { get; set; } = "email";
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    private sealed class DocuSignSenderViewResponse
    {
        public string Url { get; set; } = string.Empty;
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
