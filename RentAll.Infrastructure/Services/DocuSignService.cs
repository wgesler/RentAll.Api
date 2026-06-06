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
        string? docuSignSecretName,
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        if (string.IsNullOrWhiteSpace(docuSignSecretName))
            throw new InvalidOperationException("Organization DocuSign configuration is not set.");

        if (signers.Count == 0)
            throw new ArgumentException("At least one signer is required.", nameof(signers));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

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

        var anchors = string.Concat(
            Enumerable.Range(1, signerCount).Select(index =>
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

    private async Task<DocuSignCredentials> GetCredentialsFromKeyVaultAsync(
        string secretName,
        CancellationToken cancellationToken)
    {
        var kvUri = _settings.KeyVaultUri;
        if (string.IsNullOrWhiteSpace(kvUri))
            throw new InvalidOperationException("DocuSignSettings:KeyVaultUri is not set.");

        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        var secretValue = secret.Value.Value;

        if (string.IsNullOrWhiteSpace(secretValue))
            throw new InvalidOperationException($"DocuSign Key Vault secret '{secretName}' is empty.");

        DocuSignCredentials? credentials;
        try
        {
            credentials = JsonSerializer.Deserialize<DocuSignCredentials>(secretValue, CredentialJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"DocuSign Key Vault secret '{secretName}' must be JSON with integrationKey, userId, accountId, and privateKey.",
                ex);
        }

        if (credentials == null
            || string.IsNullOrWhiteSpace(credentials.IntegrationKey)
            || string.IsNullOrWhiteSpace(credentials.UserId)
            || string.IsNullOrWhiteSpace(credentials.AccountId)
            || string.IsNullOrWhiteSpace(credentials.PrivateKey))
        {
            throw new InvalidOperationException(
                $"DocuSign Key Vault secret '{secretName}' is missing required fields.");
        }

        return credentials;
    }

    private async Task<string> RequestAccessTokenAsync(
        DocuSignCredentials credentials,
        CancellationToken cancellationToken)
    {
        var jwt = CreateJwtAssertion(credentials);
        var httpClient = _httpClientFactory.CreateClient(nameof(DocuSignService));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_settings.AuthServer}/oauth/token")
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
            _logger.LogError(
                "DocuSign token request failed. StatusCode: {StatusCode}; Response: {ResponseBody}",
                response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"DocuSign authentication failed with status code {(int)response.StatusCode}.");
        }

        var tokenResponse = JsonSerializer.Deserialize<DocuSignTokenResponse>(body, JsonSerializerOptions);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            throw new InvalidOperationException("DocuSign authentication returned an empty access token.");

        return tokenResponse.AccessToken;
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

        var httpClient = _httpClientFactory.CreateClient(nameof(DocuSignService));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.ApiBaseUrl.TrimEnd('/')}/v2.1/accounts/{credentials.AccountId}/envelopes")
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
                $"DocuSign envelope creation failed with status code {(int)response.StatusCode}.");
        }

        var envelopeResponse = JsonSerializer.Deserialize<DocuSignEnvelopeResponse>(body, JsonSerializerOptions);
        if (envelopeResponse == null || string.IsNullOrWhiteSpace(envelopeResponse.EnvelopeId))
            throw new InvalidOperationException("DocuSign envelope creation returned an empty envelope id.");

        return envelopeResponse.EnvelopeId;
    }

    private string CreateJwtAssertion(DocuSignCredentials credentials)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(credentials.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal));

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = credentials.IntegrationKey,
            Audience = _settings.AuthServer,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, credentials.UserId)
            ]),
            NotBefore = now,
            Expires = now.AddMinutes(55),
            SigningCredentials = signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
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
}
