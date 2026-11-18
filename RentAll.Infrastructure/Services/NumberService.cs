using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Accounting;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentAll.Infrastructure.Services;

public class NumberService : INumberService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly NumberSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NumberService> _logger;

    public NumberService(IOptions<NumberSettings> settings, IHttpClientFactory httpClientFactory, ILogger<NumberService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<NumberSession> CreateSessionAsync(string? acctCodeSecretName, string? tokenSecretName, CancellationToken cancellationToken = default)
    {
        var acctCode = await ReadSecretAsync(acctCodeSecretName, cancellationToken);
        var token = await ReadSecretAsync(tokenSecretName, cancellationToken);
        if (string.IsNullOrWhiteSpace(_settings.AuthenticateUrl))
            throw new InvalidOperationException("NumberSettings:AuthenticateUrl is not set.");

        AuthenticateResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var response = await client.PostAsJsonAsync(_settings.AuthenticateUrl, new AuthenticateRequest { AcctCode = acctCode, Token = token }, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Number Authenticate failed before a response.");
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.AuthenticateResult;
        if (result == null)
            throw new InvalidOperationException("Number Authenticate returned an empty response.");

        if (!result.FunctionOk)
        {
            _logger.LogError("Number Authenticate FunctionOk=false ErrCode={ErrCode} ErrMsg={ErrMsg}", result.ErrCode, result.ErrMsg);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.ErrMsg) ? $"Number Authenticate failed ({result.ErrCode})." : result.ErrMsg);
        }

        if (!result.AuthSuccess || string.IsNullOrWhiteSpace(result.SessKey))
        {
            _logger.LogError("Number Authenticate declined. RespMsg={RespMsg}", result.RespMsg);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.RespMsg) ? "Number Authenticate was declined." : result.RespMsg);
        }

        var merchId = result.MerchantList?.FirstOrDefault()?.Id ?? 0;
        if (merchId <= 0)
            throw new InvalidOperationException("Number Authenticate did not return a merchant.");

        return new NumberSession { SessKey = result.SessKey, MerchId = merchId, ExpiresOn = ReadSessionExpiry(result.RespMsg) };
    }

    public async Task<string> CreatePayFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(payForm);
        if (string.IsNullOrWhiteSpace(session.SessKey))
            throw new InvalidOperationException("Number session key is missing.");
        if (string.IsNullOrWhiteSpace(_settings.InitFormUrl))
            throw new InvalidOperationException("NumberSettings:InitFormUrl is not set.");

        var initRequest = BuildInitFormRequest(session, payForm);
        var requestJson = JsonSerializer.Serialize(initRequest, JsonOptions);
        _logger.LogError("[NumberPayFormTrace] Step=InitFormRequest Url={Url} MerchId={MerchId} HasSessKey={HasSessKey} Body={Body}", _settings.InitFormUrl, session.MerchId, !string.IsNullOrWhiteSpace(session.SessKey), requestJson);

        InitFormResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.InitFormUrl);
            request.Headers.TryAddWithoutValidation("SessKey", session.SessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(initRequest, options: JsonOptions);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<InitFormResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[NumberPayFormTrace] Step=InitFormTransportFailed Body={Body}", requestJson);
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.PaymentInitResult;
        if (result == null)
        {
            _logger.LogError("[NumberPayFormTrace] Step=InitFormEmptyResponse Body={Body}", requestJson);
            throw new InvalidOperationException("Number PayForm Init returned an empty response.");
        }
        if (!result.FunctionOk || string.IsNullOrWhiteSpace(result.PaymentUrl))
        {
            _logger.LogError("[NumberPayFormTrace] Step=InitFormRejected FunctionOk={FunctionOk} ErrCode={ErrCode} ErrMsg={ErrMsg} RespMsg={RespMsg} Body={Body}", result.FunctionOk, result.ErrCode, result.ErrMsg, result.RespMsg, requestJson);
            var message = string.IsNullOrWhiteSpace(result.ErrMsg) ? result.RespMsg : result.ErrMsg;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"Number PayForm Init failed ({result.ErrCode})." : message);
        }

        return result.PaymentUrl;
    }

    private InitFormRequest BuildInitFormRequest(NumberSession session, NumberPayForm payForm)
    {
        return new InitFormRequest
        {
            InitParams = new InitParamsBody
            {
                MerchID = session.MerchId,
                PostURL = BuildPostUrl(payForm),
                RedirectURL = Blank(_settings.RedirectUrl),
                REF_ID = payForm.ReferenceId,
                RPGUID = payForm.ReferenceGuid.ToString(),
                EINDEX = Blank(_settings.EIndex),
                Amounts = new InitAmounts { Amount = payForm.Amount, Surcharge = 0, TotalAmt = payForm.Amount },
                Payer = new InitPayer
                {
                    Firstname = Blank(payForm.FirstName),
                    Lastname = Blank(payForm.LastName),
                    Email = Blank(payForm.Email),
                    Phone = Blank(payForm.Phone),
                    BillingAddress = new InitBillingAddress
                    {
                        StreetAddress = Blank(payForm.StreetAddress),
                        City = Blank(payForm.City),
                        State = Blank(payForm.State),
                        ZIP = Blank(payForm.Zip)
                    }
                }
            }
        };
    }

    // Number posts the payment result here. The organization and office ride on the query
    // so the callback can find the reservation and the office's stored Number session.
    private string? BuildPostUrl(NumberPayForm payForm)
    {
        var postUrl = Blank(_settings.PostUrl);
        if (postUrl == null || payForm.OrganizationId == Guid.Empty || payForm.OfficeId <= 0)
            return postUrl;

        var separator = postUrl.Contains('?') ? '&' : '?';
        return $"{postUrl}{separator}organizationId={payForm.OrganizationId}&officeId={payForm.OfficeId}";
    }

    public async Task<NumberTransaction?> GetTransactionAsync(NumberSession session, long transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.SessKey))
            throw new InvalidOperationException("Number session key is missing.");
        if (string.IsNullOrWhiteSpace(_settings.TransactionDetailUrl))
            throw new InvalidOperationException("NumberSettings:TransactionDetailUrl is not set.");

        TransactionDetailResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.TransactionDetailUrl);
            request.Headers.TryAddWithoutValidation("SessKey", session.SessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(new { TxID = transactionId }, options: JsonOptions);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<TransactionDetailResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Number Transaction_FullDetail failed before a response. TxID={TxId}", transactionId);
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.Transaction_FullDetailResult;
        if (result == null || !result.FunctionOk || result.Transaction == null)
        {
            _logger.LogError("Number Transaction_FullDetail found no transaction. TxID={TxId} ErrCode={ErrCode} ErrMsg={ErrMsg}", transactionId, result?.ErrCode, result?.ErrMsg);
            return null;
        }

        var tx = result.Transaction;
        return new NumberTransaction
        {
            Id = tx.ID,
            CreatedOn = tx.CreatedOn?.ToString(),
            Status = tx.TxSTATUS?.ToString(),
            TxType = tx.TxType?.ToString(),
            Amount = tx.AMOUNT,
            ConsentId = tx.ConsentID,
            RefId = tx.REF_ID,
            RpGuid = tx.RPGUID,
            TxnCode = tx.TXN_CODE
        };
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset ReadSessionExpiry(string? respMsg)
    {
        var value = respMsg ?? string.Empty;
        const string marker = "SessKey Expires|";
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
            value = value[(index + marker.Length)..].Trim();
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeLocal, out var parsed))
            return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Local));
        return DateTimeOffset.UtcNow.AddHours(24);
    }

    private async Task<string> ReadSecretAsync(string? secretName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new InvalidOperationException("Number Key Vault secret name is not configured.");
        if (string.IsNullOrWhiteSpace(_settings.KeyVaultUri))
            throw new InvalidOperationException("NumberSettings:KeyVaultUri is not set.");

        var client = new SecretClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(secret.Value.Value))
            throw new InvalidOperationException($"Number Key Vault secret {secretName} is empty.");
        return secret.Value.Value;
    }

    private sealed class AuthenticateRequest
    {
        public string AcctCode { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    private sealed class AuthenticateResponse
    {
        public AuthenticateResultBody? AuthenticateResult { get; set; }
    }

    private sealed class AuthenticateResultBody
    {
        public bool AuthSuccess { get; set; }
        public bool FunctionOk { get; set; }
        public int ErrCode { get; set; }
        public string? ErrMsg { get; set; }
        public string? RespMsg { get; set; }
        public string? SessKey { get; set; }
        public List<MerchantItem>? MerchantList { get; set; }
    }

    private sealed class MerchantItem
    {
        public int Id { get; set; }
    }

    private sealed class InitFormRequest
    {
        public InitParamsBody InitParams { get; set; } = new();
    }

    private sealed class InitParamsBody
    {
        public int MerchID { get; set; }
        public string WTYPE { get; set; } = "PF";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PostURL { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RedirectURL { get; set; }
        public string REF_ID { get; set; } = string.Empty;
        public string RPGUID { get; set; } = string.Empty;
        public string EndPoint { get; set; } = "PayForm/PF.aspx";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EINDEX { get; set; }
        public InitAmounts Amounts { get; set; } = new();
        public InitPayer Payer { get; set; } = new();
        public InitWidOptions WidOptions { get; set; } = new();
    }

    private sealed class InitAmounts
    {
        public decimal Amount { get; set; }
        public decimal Surcharge { get; set; }
        public decimal TotalAmt { get; set; }
    }

    private sealed class InitPayer
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Firstname { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Lastname { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Email { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Phone { get; set; }
        public InitBillingAddress BillingAddress { get; set; } = new();
    }

    private sealed class InitBillingAddress
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StreetAddress { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? City { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? State { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ZIP { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Country { get; set; }
    }

    private sealed class InitWidOptions
    {
        public string eVisible { get; set; } = "0665";
        public string eReadOnly { get; set; } = "0040";
        public string eStyles { get; set; } = "0001";
        public string eSubmission { get; set; } = "0A01";
        public string eFeatures { get; set; } = "0000";
        public string eColors { get; set; } = "#ffffff,#428bca,#007bff,#212121,#ffffff,#212121,#ffffff";
    }

    private sealed class TransactionDetailResponse
    {
        public TransactionDetailResult? Transaction_FullDetailResult { get; set; }
    }

    private sealed class TransactionDetailResult
    {
        public int ErrCode { get; set; }
        public string? ErrMsg { get; set; }
        public bool FunctionOk { get; set; }
        public string? RespMsg { get; set; }
        public TransactionDetailBody? Transaction { get; set; }
    }

    private sealed class TransactionDetailBody
    {
        public long ID { get; set; }
        public JsonElement? CreatedOn { get; set; }
        // The docs don't say whether these are numbers or text, so accept either.
        public JsonElement? TxSTATUS { get; set; }
        public JsonElement? TxType { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal AMOUNT { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long ConsentID { get; set; }
        public string? REF_ID { get; set; }
        public string? RPGUID { get; set; }
        public string? TXN_CODE { get; set; }
    }

    private sealed class InitFormResponse
    {
        public PaymentInitResultBody? PaymentInitResult { get; set; }
    }

    private sealed class PaymentInitResultBody
    {
        public int ErrCode { get; set; }
        public string? ErrMsg { get; set; }
        public bool FunctionOk { get; set; }
        public string? PaymentUrl { get; set; }
        public string? RespMsg { get; set; }
    }
}
