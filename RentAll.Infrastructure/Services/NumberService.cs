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
    // Number API routes.
    private const string NumberApiBaseUrl = "https://easypay5.com/APIcardProcNumber/v1.0.0/";
    private const string RestApiBaseUrl = "https://easypay5.com/APIcardProcREST/v1.0.0/";
    private const string AuthenticateUrl = NumberApiBaseUrl + "Authenticate";
    private const string PayFormInitUrl = NumberApiBaseUrl + "CardSale/InitForm";
    private const string PfsFormInitUrl = RestApiBaseUrl + "CardSale/InitForm";
    private const string TransactionDetailUrl = NumberApiBaseUrl + "Query/Transaction_FullDetail";
    private const string ConsentDetailUrl = NumberApiBaseUrl + "Query/ConsentAnnual_FullDetail";
    private const string ChargeStoredCardUrl = NumberApiBaseUrl + "ConsentAnnual/ChargeStoredCard";
    private const string SubscriptionModifyUrl = RestApiBaseUrl + "ConsentSubscription/Modify";
    private const string ConsentGeneralQueryUrl = RestApiBaseUrl + "Query/ConsentGeneral";
    private const string AchComboUrl = NumberApiBaseUrl + "ACH/Combo";
    private const string AchProcPaymentUrl = NumberApiBaseUrl + "ACH/ProcPayment";

    // Form types: PF is the standard PayForm; PFS is the custom PayForm that Number says creates a consent (saved card).
    private const string PayFormType = "PF";
    private const string PayFormEndPoint = "PayForm/PF.aspx";
    private const string PfsFormType = "PFS";
    private const string PfsFormEndPoint = "PayForm/PFS.aspx";

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

        AuthenticateResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var response = await client.PostAsJsonAsync(AuthenticateUrl, new AuthenticateRequest { AcctCode = acctCode, Token = token }, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[EasyPay AuthenticateNoResponse]");
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.AuthenticateResult;
        if (result == null)
            throw new InvalidOperationException("Number Authenticate returned an empty response.");

        if (!result.FunctionOk)
        {
            _logger.LogError("[EasyPay AuthenticateRejected] ErrCode={ErrCode} ErrMsg={ErrMsg}", result.ErrCode, result.ErrMsg);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.ErrMsg) ? $"Number Authenticate failed ({result.ErrCode})." : result.ErrMsg);
        }

        if (!result.AuthSuccess || string.IsNullOrWhiteSpace(result.SessKey))
        {
            _logger.LogError("[EasyPay AuthenticateDeclined] RespMsg={RespMsg}", result.RespMsg);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.RespMsg) ? "Number Authenticate was declined." : result.RespMsg);
        }

        var merchId = result.MerchantList?.FirstOrDefault()?.Id ?? 0;
        if (merchId <= 0)
            throw new InvalidOperationException("Number Authenticate did not return a merchant.");

        return new NumberSession { SessKey = result.SessKey, MerchId = merchId, ExpiresOn = ReadSessionExpiry(result.RespMsg) };
    }

    /// <summary>
    /// Save Card on File PayForm (PF) for the payment email: $0, no subscription. The payer enters a card and Number
    /// saves it as an annual consent that staff charge later with ConsentAnnual/ChargeStoredCard.
    /// </summary>
    public Task<string> CreatePayFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payForm);
        var initRequest = BuildInitFormRequest(session, payForm, PayFormType, PayFormEndPoint, new InitAmounts(), SaveCardWidOptions());
        return InitFormAsync(session, PayFormInitUrl, initRequest, cancellationToken);
    }

    /// <summary>Custom PayForm (PFS) on the REST API, per Number's sample. Charges payForm.Amount and saves the card (consent).</summary>
    public Task<string> CreatePfsFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payForm);
        var amounts = new InitAmounts { Amount = payForm.Amount, Surcharge = 0, TotalAmt = payForm.Amount };
        var initRequest = BuildInitFormRequest(session, payForm, PfsFormType, PfsFormEndPoint, amounts, PfsWidOptions());
        // Match Number's PFS sample: blank EINDEX (ours points at a PF form design) and a blank Country.
        initRequest.InitParams.EINDEX = string.Empty;
        initRequest.InitParams.Payer.BillingAddress.Country = string.Empty;
        if (payForm.Subscription != null)
            initRequest.InitParams.Subscription = new InitSubscription
            {
                Amount = payForm.Subscription.Amount,
                Period = payForm.Subscription.Period,
                FirstPayDate = payForm.Subscription.FirstPayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        return InitFormAsync(session, PfsFormInitUrl, initRequest, cancellationToken);
    }

    private async Task<string> InitFormAsync(NumberSession session, string url, InitFormRequest initRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.SessKey))
            throw new InvalidOperationException("Number session key is missing.");

        var requestJson = JsonSerializer.Serialize(initRequest, JsonOptions);

        InitFormResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("SessKey", session.SessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(initRequest, options: JsonOptions);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<InitFormResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[EasyPay CreateCardFormNoResponse] Body={Body}", requestJson);
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.PaymentInitResult;
        if (result == null)
        {
            _logger.LogError("[EasyPay CreateCardFormEmptyResponse] Body={Body}", requestJson);
            throw new InvalidOperationException("Number PayForm Init returned an empty response.");
        }
        if (NumberSessionExpiredException.IsSessionError(result.ErrCode))
        {
            _logger.LogError("[EasyPay CreateCardFormSessionRejected] ErrCode={ErrCode} ErrMsg={ErrMsg}", result.ErrCode, result.ErrMsg);
            throw new NumberSessionExpiredException(result.ErrCode, result.ErrMsg);
        }
        if (!result.FunctionOk || string.IsNullOrWhiteSpace(result.PaymentUrl))
        {
            _logger.LogError("[EasyPay CreateCardFormRejected] FunctionOk={FunctionOk} ErrCode={ErrCode} ErrMsg={ErrMsg} RespMsg={RespMsg} Body={Body}", result.FunctionOk, result.ErrCode, result.ErrMsg, result.RespMsg, requestJson);
            var message = string.IsNullOrWhiteSpace(result.ErrMsg) ? result.RespMsg : result.ErrMsg;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"Number PayForm Init failed ({result.ErrCode})." : message);
        }

        return result.PaymentUrl;
    }

    private InitFormRequest BuildInitFormRequest(NumberSession session, NumberPayForm payForm, string formType, string endPoint, InitAmounts amounts, InitWidOptions widOptions)
    {
        return new InitFormRequest
        {
            InitParams = new InitParamsBody
            {
                MerchID = session.MerchId,
                PostURL = Blank(_settings.PostUrl),
                // The caller sets the organization's pay-response page here; Number adds ?TXID=..&CID=.. to it.
                RedirectURL = AbsoluteUrl(Blank(payForm.RedirectUrl) ?? Blank(_settings.RedirectUrl)),
                REF_ID = payForm.ReferenceId,
                RPGUID = payForm.ReferenceGuid.ToString(),
                WTYPE = formType,
                EndPoint = endPoint,
                EINDEX = Blank(_settings.EIndex),
                Amounts = amounts,
                WidOptions = widOptions,
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

    public async Task<NumberTransaction?> GetTransactionAsync(NumberSession session, long transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.SessKey))
            throw new InvalidOperationException("Number session key is missing.");

        TransactionDetailResponse? body;
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var request = new HttpRequestMessage(HttpMethod.Post, TransactionDetailUrl);
            request.Headers.TryAddWithoutValidation("SessKey", session.SessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(new { TxID = transactionId }, options: JsonOptions);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadFromJsonAsync<TransactionDetailResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[EasyPay GetTransactionNoResponse] TxID={TxId}", transactionId);
            throw new InvalidOperationException("Number could not be reached.", ex);
        }

        var result = body?.Transaction_FullDetailResult;
        if (result != null && NumberSessionExpiredException.IsSessionError(result.ErrCode))
        {
            _logger.LogError("[EasyPay GetTransactionSessionRejected] TxID={TxId} ErrCode={ErrCode} ErrMsg={ErrMsg}", transactionId, result.ErrCode, result.ErrMsg);
            throw new NumberSessionExpiredException(result.ErrCode, result.ErrMsg);
        }
        if (result == null || !result.FunctionOk || result.Transaction == null)
        {
            _logger.LogError("[EasyPay GetTransactionNotFound] TxID={TxId} ErrCode={ErrCode} ErrMsg={ErrMsg}", transactionId, result?.ErrCode, result?.ErrMsg);
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
            TxnCode = tx.TXN_CODE,
            Flags = JsonText(tx.Flags),
            AvsResult = JsonText(tx.AVSr),
            CvvResult = JsonText(tx.CVVr),
            IsPartialApproval = JsonBool(tx.IsPartialApproval),
            PartialAuthApproved = JsonDecimal(tx.PartialAuthApproved),
            HAuthorizedAmount = JsonDecimal(tx.HAuthorizedAmount)
        };
    }

    public async Task<NumberConsent?> GetConsentAsync(NumberSession session, long consentId, CancellationToken cancellationToken = default)
    {

        var body = await PostAsync<ConsentDetailResponse>(session, ConsentDetailUrl, new { ConsentID = consentId }, "ConsentAnnual_FullDetail", cancellationToken);
        var result = body?.ConsentAnnual_FullDetailResult;
        if (result != null && NumberSessionExpiredException.IsSessionError(result.ErrCode))
            throw new NumberSessionExpiredException(result.ErrCode, result.ErrMsg);
        if (result == null || !result.FunctionOk || result.ConsentAnnual == null)
        {
            // Subscription (Type S) consents from the PFS form aren't annual consents (ErrCode 6046); look them up in the general list.
            _logger.LogWarning("[EasyPay GetConsentNotAnnual] Trying subscription consents. ConsentID={ConsentId} ErrCode={ErrCode} ErrMsg={ErrMsg}", consentId, result?.ErrCode, result?.ErrMsg);
            return await GetSubscriptionConsentAsync(session, consentId, cancellationToken);
        }

        var consent = result.ConsentAnnual;
        // Number's sample spells this object "AccounttHolder"; accept either spelling.
        var holder = result.AccounttHolder ?? result.AccountHolder;
        return new NumberConsent
        {
            Id = consent.ID,
            MerchId = consent.MerchID,
            IsEnabled = consent.IsEnabled,
            RpGuid = Blank(consent.RPGUID),
            CustomerRefId = Blank(consent.CustomerRefID),
            LastFour = LastFourDigits(consent.AcctNo) ?? LastFourDigits(holder?.AcctMask),
            CardType = Blank(holder?.CardType),
            ExpDate = Blank(holder?.ExpDate),
            FirstName = Blank(consent.AcctHolderFirstName) ?? Blank(holder?.Firstname),
            LastName = Blank(consent.AcctHolderLastName) ?? Blank(holder?.LastName)
        };
    }

    public async Task<NumberStoredCardChargeResult> ChargeStoredCardAsync(NumberSession session, NumberStoredCardCharge charge, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(charge);
        if (charge.Amount <= 0)
            throw new InvalidOperationException("Charge amount must be greater than zero.");

        var request = new
        {
            ConsentID = charge.ConsentId,
            Amounts = new { BaseAmt = charge.Amount, Surcharge = 0m, TotalAmt = charge.Amount },
            purchDetails = new
            {
                ServiceDescrip = charge.ServiceDescription ?? string.Empty,
                ClientRefID = charge.ClientRefId ?? string.Empty,
                RPGUID = charge.RpGuid ?? string.Empty
            },
            AlternateMerchID = 0,
            User = charge.User ?? string.Empty,
            AuthOnly = 0
        };

        var body = await PostAsync<ChargeStoredCardResponse>(session, ChargeStoredCardUrl, request, "ChargeStoredCard", cancellationToken);
        var result = body?.ConsentAnnual_ChargeStoredCardResult
            ?? throw new InvalidOperationException("Number ChargeStoredCard returned an empty response.");
        if (NumberSessionExpiredException.IsSessionError(result.ErrCode))
            throw new NumberSessionExpiredException(result.ErrCode, result.ErrMsg);

        _logger.LogInformation("[EasyPay ChargeStoredCardResult] ConsentID={ConsentId} Requested={Requested} FunctionOk={FunctionOk} TxApproved={TxApproved} TxID={TxId} Approved={Approved} Authorized={Authorized} Partial={Partial} ErrCode={ErrCode} RespMsg={RespMsg}",
            charge.ConsentId, charge.Amount, result.FunctionOk, result.TxApproved, result.TxID, result.ApprovedAmounts?.TotalAmt, result.ResponseAuthorizedAmount, result.IsPartialApproval, result.ErrCode, result.RespMsg);

        return new NumberStoredCardChargeResult
        {
            Approved = result.FunctionOk && result.TxApproved,
            TxId = result.TxID,
            TxnCode = Blank(result.TxnCode),
            Message = Blank(result.ErrMsg) ?? Blank(result.RespMsg),
            ErrMsg = Blank(result.ErrMsg),
            RespMsg = Blank(result.RespMsg),
            FunctionOk = result.FunctionOk,
            ErrCode = result.ErrCode,
            IsPartialApproval = result.IsPartialApproval,
            ApprovedTotal = result.ApprovedAmounts?.TotalAmt ?? 0,
            AuthorizedAmount = result.ResponseAuthorizedAmount,
            AvsResult = Blank(result.AVSresult),
            CvvResult = Blank(result.CVVresult)
        };
    }

    /// <summary>
    /// Finds a subscription consent with Query/ConsentGeneral. Its query language is undocumented: "(G=4)" (G = consent
    /// type, 4 = subscription) returns all subscription consents; no filter by consent ID was found, so match it here.
    /// The list has no card type, and EndDate is the card's expiration.
    /// </summary>
    private async Task<NumberConsent?> GetSubscriptionConsentAsync(NumberSession session, long consentId, CancellationToken cancellationToken)
    {
        var body = await PostAsync<JsonElement>(session, ConsentGeneralQueryUrl, new { Query = "(G=4)" }, "ConsentGeneral_Query", cancellationToken);
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("ConsentGeneral_QueryResult", out var result))
        {
            _logger.LogError("[EasyPay FindSubscriptionConsentEmpty] ConsentID={ConsentId} Response={Response}", consentId, body.ToString());
            return null;
        }
        var errCode = result.TryGetProperty("ErrCode", out var e) && e.TryGetInt32(out var code) ? code : 0;
        if (NumberSessionExpiredException.IsSessionError(errCode))
            throw new NumberSessionExpiredException(errCode, result.TryGetProperty("ErrMsg", out var m) ? m.ToString() : null);

        if (!result.TryGetProperty("Consents", out var consents) || consents.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var c in consents.EnumerateArray())
        {
            if (!c.TryGetProperty("ID", out var id) || !id.TryGetInt64(out var idValue) || idValue != consentId)
                continue;

            string? Text(string name) => c.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? Blank(v.ToString()) : null;

            // The list has no card type; the form's card check (AuthTxID) does, so read it from that transaction.
            string? cardType = null;
            if (c.TryGetProperty("AuthTxID", out var authTx) && authTx.TryGetInt64(out var authTxId) && authTxId > 0)
            {
                var tx = await PostAsync<JsonElement>(session, TransactionDetailUrl, new { TxID = authTxId }, "Transaction_FullDetail", cancellationToken);
                cardType = FindText(tx, "CardType");
                if (cardType == null)
                    _logger.LogError("[EasyPay GetTransactionNoCardType] ConsentID={ConsentId} AuthTxID={AuthTxId}", consentId, authTxId);
            }

            return new NumberConsent
            {
                Id = idValue,
                IsSubscription = true,
                MerchId = c.TryGetProperty("MerchID", out var merch) && merch.TryGetInt32(out var merchId) ? merchId : 0,
                IsEnabled = c.TryGetProperty("IsEnabled", out var enabled) && enabled.ValueKind == JsonValueKind.True,
                RpGuid = Text("RPGUID"),
                CustomerRefId = Text("CustomerRefID"),
                LastFour = LastFourDigits(Text("AcctNo")),
                CardType = cardType,
                ExpDate = WcfDateToMonthYear(Text("EndDate")),
                FirstName = Text("AcctHolderFirstName"),
                LastName = Text("AcctHolderLastName")
            };
        }
        _logger.LogError("[EasyPay FindSubscriptionConsentNotFound] ConsentID={ConsentId}", consentId);
        return null;
    }

    // First non-empty value of a property with this name anywhere in a JSON response.
    private static string? FindText(JsonElement element, string name)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(name) && property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    {
                        var text = Blank(property.Value.ToString());
                        if (text != null)
                            return text;
                    }
                    var nested = FindText(property.Value, name);
                    if (nested != null)
                        return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindText(item, name);
                    if (nested != null)
                        return nested;
                }
                break;
        }
        return null;
    }

    // Number's dates look like "/Date(1851307200000-0400)/" (ms since 1970). Returns "MM/yy".
    private static string? WcfDateToMonthYear(string? value)
    {
        if (value == null)
            return null;
        var match = System.Text.RegularExpressions.Regex.Match(value, @"Date\((-?\d+)([+-]\d{4})?\)");
        if (!match.Success || !long.TryParse(match.Groups[1].Value, out var ms))
            return null;
        var date = DateTimeOffset.FromUnixTimeMilliseconds(ms);
        if (match.Groups[2].Success)
        {
            var offset = match.Groups[2].Value;
            var hours = int.Parse(offset.Substring(1, 2), CultureInfo.InvariantCulture);
            var minutes = int.Parse(offset.Substring(3, 2), CultureInfo.InvariantCulture);
            var span = new TimeSpan(hours, minutes, 0);
            date = date.ToOffset(offset[0] == '-' ? span.Negate() : span);
        }
        return date.ToString("MM/yy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Saves a bank account as an ACH consent without debiting it: ACH/Combo with Amount 0 ("To create a consent only
    /// without processing the sale, set the Amount to zero"). Debit it later with ACH/ProcPayment and the ConsentID.
    /// Field names follow Number's sample exactly, including its "BillIngAdress" spelling.
    /// </summary>
    public async Task<NumberAchConsentResult> CreateAchConsentAsync(NumberSession session, NumberAchConsentRequest ach, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ach);
        var holder = new
        {
            Firstname = ach.FirstName,
            Lastname = ach.LastName,
            Company = string.Empty,
            Title = string.Empty,
            Url = string.Empty,
            BillIngAdress = new
            {
                Address1 = ach.Address1 ?? string.Empty,
                Address2 = string.Empty,
                City = ach.City ?? string.Empty,
                State = ach.State ?? string.Empty,
                ZIP = ach.Zip ?? string.Empty,
                Country = "USA"
            },
            Email = ach.Email ?? string.Empty,
            Phone = ach.Phone ?? string.Empty
        };
        var request = new
        {
            ChargeDetails = new
            {
                AccountNumber = ach.AccountNumber,
                RoutingNumber = ach.RoutingNumber,
                Amount = 0m,
                AccountType = (int)ach.AccountType
            },
            AcctHolder = holder,
            EndCustomer = holder,
            PurchItems = new
            {
                ServiceDescrip = ach.ServiceDescription ?? string.Empty,
                ClientRefID = ach.ClientRefId ?? string.Empty,
                RPGUID = ach.RpGuid ?? string.Empty
            },
            MerchID = session.MerchId
        };

        var body = await PostAsync<JsonElement>(session, AchComboUrl, request, "ACH_Combo", cancellationToken);
        var result = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("ACHTransaction_ComboResult", out var r) ? r : default;
        long ReadLong(string name) => result.ValueKind == JsonValueKind.Object && result.TryGetProperty(name, out var v) && v.TryGetInt64(out var n) ? n : 0;
        bool ReadBool(string name) => result.ValueKind == JsonValueKind.Object && result.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
        string? ReadText(string name) => result.ValueKind == JsonValueKind.Object && result.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? Blank(v.ToString()) : null;

        var errCode = (int)ReadLong("ErrCode");
        if (NumberSessionExpiredException.IsSessionError(errCode))
            throw new NumberSessionExpiredException(errCode, ReadText("ErrMsg"));

        return new NumberAchConsentResult
        {
            FunctionOk = ReadBool("FunctionOk"),
            TxApproved = ReadBool("TxApproved"),
            ConsentId = ReadLong("ConsentID"),
            TxId = ReadLong("TxID"),
            ErrCode = errCode,
            ErrMsg = ReadText("ErrMsg"),
            RespMsg = ReadText("RespMsg")
        };
    }

    /// <summary>
    /// Debits a saved bank account (the ACH consent from ACH/Combo) with ACH/ProcPayment. Number's answer has no
    /// amounts, so an approval is for the amount requested. Approved means Number accepted the debit; the bank can
    /// still return it later (for example for insufficient funds).
    /// </summary>
    public async Task<NumberStoredCardChargeResult> ChargeBankAccountAsync(NumberSession session, long consentId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (consentId <= 0)
            throw new InvalidOperationException("There is no saved bank account to debit.");
        if (amount <= 0)
            throw new InvalidOperationException("Debit amount must be greater than zero.");

        var body = await PostAsync<JsonElement>(session, AchProcPaymentUrl, new { ConsentID = consentId, ProcessAmount = amount }, "ACH_ProcPayment", cancellationToken);
        var result = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("ACHConsentAnnual_ProcPaymentResult", out var r) ? r : default;
        if (result.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Number ACH ProcPayment returned an empty response.");
        long ReadLong(string name) => result.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : 0;
        bool ReadBool(string name) => result.TryGetProperty(name, out var v) && JsonBool(v);
        string? ReadText(string name) => result.TryGetProperty(name, out var v) ? JsonText(v) : null;

        var errCode = (int)ReadLong("ErrCode");
        if (NumberSessionExpiredException.IsSessionError(errCode))
            throw new NumberSessionExpiredException(errCode, ReadText("ErrMsg"));

        var functionOk = ReadBool("FunctionOk");
        var approved = functionOk && ReadBool("TxApproved");
        return new NumberStoredCardChargeResult
        {
            Approved = approved,
            FunctionOk = functionOk,
            TxId = ReadLong("TxID"),
            TxnCode = ReadText("uniqueTranID"),
            Message = ReadText("ErrMsg") ?? ReadText("RespMsg"),
            ErrMsg = ReadText("ErrMsg"),
            RespMsg = ReadText("RespMsg"),
            ErrCode = errCode,
            ApprovedTotal = approved ? amount : 0
        };
    }

    /// <summary>
    /// Puts a PFS subscription on hold so it never bills on its own; the saved card stays usable.
    /// Number overwrites every field sent, so all others are -1 ("keep the existing value"), except PaymentAdjustDate, which is omitted.
    /// </summary>
    public async Task<bool> HoldSubscriptionAsync(NumberSession session, long consentId, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            ConsentID = consentId,
            ConsentMods = new
            {
                ExpMonth = -1,
                ExpYear = -1,
                Email = "-1",
                Zip = "-1",
                RPGUID = "-1",
                CustomerRefID = "-1",
                ServiceDescrip = "-1",
                PaymentAmt = -1,
                // PaymentAdjustDate is left out: Number rejects "-1" there (ErrCode 7123, not a valid date).
                OnHold = true
            }
        };

        var body = await PostAsync<JsonElement>(session, SubscriptionModifyUrl, request, "ConsentSubscription_Modify", cancellationToken);
        // Number's docs don't show this response; find the *Result object and read FunctionOk/ErrCode from it.
        var result = body.ValueKind == JsonValueKind.Object
            ? body.EnumerateObject().Select(p => p.Value).FirstOrDefault(v => v.ValueKind == JsonValueKind.Object && v.TryGetProperty("FunctionOk", out _))
            : default;
        var errCode = result.ValueKind == JsonValueKind.Object && result.TryGetProperty("ErrCode", out var e) && e.TryGetInt32(out var code) ? code : 0;
        if (NumberSessionExpiredException.IsSessionError(errCode))
            throw new NumberSessionExpiredException(errCode, result.TryGetProperty("ErrMsg", out var m) ? m.ToString() : null);

        var ok = result.ValueKind == JsonValueKind.Object && result.TryGetProperty("FunctionOk", out var f) && f.ValueKind == JsonValueKind.True;
        _logger.Log(ok ? LogLevel.Warning : LogLevel.Error, "[EasyPay HoldSubscriptionResult] ConsentID={ConsentId} Ok={Ok} Response={Response}", consentId, ok, body.ToString());
        return ok;
    }

    private async Task<T?> PostAsync<T>(NumberSession session, string url, object requestBody, string callName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.SessKey))
            throw new InvalidOperationException("Number session key is missing.");

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NumberService));
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("SessKey", session.SessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(requestBody, options: JsonOptions);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Number {Call} failed before a response.", callName);
            throw new InvalidOperationException("Number could not be reached.", ex);
        }
    }

    // PF Save Card on File form, from Number's PayForm builder (Consent Widget, Credit Card):
    // eVisible 3623 = First and Last Name, Zip, Email, card number, Expiration, CVV and the "Agree to Save Card on File"
    //   box (required to save the card). Name, zip and email are pre-filled from the contact.
    // eSubmission 1421 = Save Card On File widget + Post Message to iFrame Parent + Redirect External + Send Only
    //   TXID/ConsentID (without it Number appends an encrypted string instead of ?TXID=..&CID=..).
    // eFeatures 0000 = no Subscription. eStyles 4402 = the PFS form's dark style with labels above the boxes.
    private static InitWidOptions SaveCardWidOptions() => new()
    {
        eVisible = "3623",
        eReadOnly = "0000",
        eStyles = "4402",
        eSubmission = "1421",
        eFeatures = "0000"
    };

    // PFS form: Number's sample values for the custom PayForm. eSubmission 0221 posts the result to the parent page and redirects.
    private static InitWidOptions PfsWidOptions() => new()
    {
        eVisible = "0600",
        eReadOnly = "0000",
        eStyles = "4402", // Number's dark "champ" theme with labels on top (0002 is the light theme)
        eSubmission = "0221"
    };

    // Number's docs don't give these fields' types, so read text, numbers and booleans alike.
    private static string? JsonText(JsonElement? value)
    {
        if (value is not { } element || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        var text = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static bool JsonBool(JsonElement? value)
    {
        if (value is not { } element)
            return false;
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => element.TryGetDecimal(out var number) && number == 1,
            JsonValueKind.String => element.GetString()?.Trim() is { } text
                && (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1" || text.Equals("Y", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static decimal? JsonDecimal(JsonElement? value)
    {
        if (value is not { } element)
            return null;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
            return number;
        if (element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    // "www.site.com" is read as a relative path by the browser; Number needs a full address.
    private static string? AbsoluteUrl(string? url)
    {
        if (url == null)
            return null;
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : "https://" + url.TrimStart('/');
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? LastFourDigits(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits.Length <= 4 ? digits : digits[^4..];
    }

    // Number's docs say a key lasts 24 hours. Its "SessKey Expires|..." time has no time zone, so it can't be
    // trusted on a server in another zone: keep whichever is sooner, that time or 23 hours from now.
    private static DateTimeOffset ReadSessionExpiry(string? respMsg)
    {
        var latest = DateTimeOffset.UtcNow.AddHours(23);
        var value = respMsg ?? string.Empty;
        const string marker = "SessKey Expires|";
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
            value = value[(index + marker.Length)..].Trim();
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeLocal, out var parsed))
        {
            var reported = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Local)).AddMinutes(-30);
            if (reported > DateTimeOffset.UtcNow && reported < latest)
                return reported;
        }
        return latest;
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
        public string WTYPE { get; set; } = PayFormType;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PostURL { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RedirectURL { get; set; }
        public string REF_ID { get; set; } = string.Empty;
        public string RPGUID { get; set; } = string.Empty;
        public string EndPoint { get; set; } = PayFormEndPoint;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EINDEX { get; set; }
        public InitAmounts Amounts { get; set; } = new();
        public InitPayer Payer { get; set; } = new();
        public InitWidOptions WidOptions { get; set; } = new();
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InitSubscription? Subscription { get; set; }
    }

    private sealed class InitSubscription
    {
        public decimal Amount { get; set; }
        public string Period { get; set; } = string.Empty;
        public string FirstPayDate { get; set; } = string.Empty;
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
        public string eVisible { get; set; } = string.Empty;
        public string eReadOnly { get; set; } = string.Empty;
        public string eStyles { get; set; } = string.Empty;
        public string eSubmission { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? eFeatures { get; set; }
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
        public JsonElement? Flags { get; set; }
        public JsonElement? AVSr { get; set; }
        public JsonElement? CVVr { get; set; }
        public JsonElement? IsPartialApproval { get; set; }
        public JsonElement? PartialAuthApproved { get; set; }
        public JsonElement? HAuthorizedAmount { get; set; }
    }

    private sealed class ConsentDetailResponse
    {
        public ConsentDetailResult? ConsentAnnual_FullDetailResult { get; set; }
    }

    private sealed class ConsentDetailResult
    {
        public int ErrCode { get; set; }
        public string? ErrMsg { get; set; }
        public bool FunctionOk { get; set; }
        public string? RespMsg { get; set; }
        public ConsentAccountHolder? AccounttHolder { get; set; }
        public ConsentAccountHolder? AccountHolder { get; set; }
        public ConsentAnnualBody? ConsentAnnual { get; set; }
    }

    private sealed class ConsentAccountHolder
    {
        public string? AcctMask { get; set; }
        public string? CardType { get; set; }
        public string? ExpDate { get; set; }
        public string? Firstname { get; set; }
        public string? LastName { get; set; }
    }

    private sealed class ConsentAnnualBody
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long ID { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int MerchID { get; set; }
        public bool IsEnabled { get; set; }
        public string? RPGUID { get; set; }
        public string? CustomerRefID { get; set; }
        public string? AcctNo { get; set; }
        public string? AcctHolderFirstName { get; set; }
        public string? AcctHolderLastName { get; set; }
    }

    private sealed class ChargeStoredCardResponse
    {
        public ChargeStoredCardResult? ConsentAnnual_ChargeStoredCardResult { get; set; }
    }

    private sealed class ChargeStoredCardResult
    {
        public int ErrCode { get; set; }
        public string? ErrMsg { get; set; }
        public bool FunctionOk { get; set; }
        public string? RespMsg { get; set; }
        public bool TxApproved { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long TxID { get; set; }
        public string? TxnCode { get; set; }
        public string? AVSresult { get; set; }
        public string? CVVresult { get; set; }
        public bool IsPartialApproval { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal ResponseAuthorizedAmount { get; set; }
        public ChargeApprovedAmounts? ApprovedAmounts { get; set; }
    }

    private sealed class ChargeApprovedAmounts
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal BaseAmt { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal TotalAmt { get; set; }
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
