using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RentAll.Api.Services;

/// <summary>
/// Logs every request RentAll sends to Number and every response, in full, so they can be copied into an
/// email for Number's support. Two Logging.ApplicationLog rows per call, named for the action and the direction:
/// "[Number CreateCardFormRequest] POST ..." (Properties.Request) and "[Number CreateCardFormResponse] 200 ..."
/// (Properties.Response); "[Number CreateCardFormFailed]" when no response came back. Sign-in credentials and session
/// keys are masked, and bank account and routing numbers are cut to their last four digits.
/// Logged at Error level only so it is kept (ApplicationLogging:MinimumLevel is Error); it is not an error.
/// Not in RentAll.Api.Logging: the application log ignores that namespace (so it never logs itself).
/// </summary>
public class NumberHttpLoggingHandler : DelegatingHandler
{
    // Authenticate sends AcctCode/Token and returns SessKey; never write those to the log.
    private static readonly Regex SecretFields = new(@"(""(?:AcctCode|Token|SessKey)""\s*:\s*"")[^""]*("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // ACH sends full bank numbers: keep only the last four digits ("AccountNumber":"****0256").
    private static readonly Regex BankNumbers = new(@"(""(?:AccountNumber|RoutingNumber)""\s*:\s*"")[^""]*?(\d{0,4})("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // What each Number endpoint does, as named in the log.
    private static readonly Dictionary<string, string> Actions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Authenticate"] = "Authenticate",
        ["CardSale/InitForm"] = "CreateCardForm",
        ["Query/Transaction_FullDetail"] = "GetTransaction",
        ["Query/ConsentAnnual_FullDetail"] = "GetConsent",
        ["Query/ConsentGeneral"] = "FindSubscriptionConsent",
        ["ConsentSubscription/Modify"] = "HoldSubscription",
        ["ConsentAnnual/ChargeStoredCard"] = "ChargeStoredCard",
        ["ACH/Combo"] = "SaveBankAccount"
    };

    private readonly ILogger<NumberHttpLoggingHandler> _logger;

    public NumberHttpLoggingHandler(ILogger<NumberHttpLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var action = ResolveAction(request.RequestUri);
        _logger.LogError("[EasyPay {Action}Request] {Method} {Url} Request={Request}", action, request.Method.Method, url, Mask(requestBody));
        var watch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("[EasyPay {Action}Failed] No response after {ElapsedMs}ms: {Error}", action, watch.ElapsedMilliseconds, ex.Message);
            throw;
        }

        // Read the body into memory so the caller can still read it after we do.
        string? responseBody = null;
        if (response.Content != null)
        {
            await response.Content.LoadIntoBufferAsync();
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        _logger.LogError("[EasyPay {Action}Response] {Status} {ElapsedMs}ms Response={Response}", action, (int)response.StatusCode, watch.ElapsedMilliseconds, Mask(responseBody));
        return response;
    }

    // The endpoint after the version segment (".../v1.0.0/CardSale/InitForm" gives "CardSale/InitForm"), named by Actions.
    private static string ResolveAction(Uri? uri)
    {
        var path = uri?.AbsolutePath ?? string.Empty;
        var marker = path.IndexOf("/v1.0.0/", StringComparison.OrdinalIgnoreCase);
        var endpoint = marker >= 0 ? path[(marker + "/v1.0.0/".Length)..].Trim('/') : path.Trim('/');
        if (Actions.TryGetValue(endpoint, out var action))
            return action;

        var last = endpoint.Split('/').LastOrDefault();
        return string.IsNullOrWhiteSpace(last) ? "Call" : last.Replace("_", string.Empty);
    }

    private static string? Mask(string? body)
    {
        return string.IsNullOrEmpty(body) ? body : BankNumbers.Replace(SecretFields.Replace(body, "$1***$2"), "$1****$2$3");
    }
}
