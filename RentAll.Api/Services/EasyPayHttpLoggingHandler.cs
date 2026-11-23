using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RentAll.Api.Services;

/// <summary>
/// Logs every request RentAll sends to EasyPay (Number) and every response, in full, so they can be copied into an
/// email for EasyPay's support. One Logging.ApplicationLog row per call, Message "[EasyPayLog] POST .../InitForm 200";
/// the full JSON is in Properties (Request, Response). Sign-in credentials and session keys are masked, and bank
/// account and routing numbers are cut to their last four digits.
/// To get them: SELECT CreatedOn, Message, JSON_VALUE(Properties, '$.Request') AS Request,
///   JSON_VALUE(Properties, '$.Response') AS Response FROM Logging.ApplicationLog WHERE Message LIKE '[[]EasyPayLog]%' ORDER BY Id DESC
/// Logged at Error level only so it is kept (ApplicationLogging:MinimumLevel is Error); it is not an error.
/// Not in RentAll.Api.Logging: the application log ignores that namespace (so it never logs itself).
/// </summary>
public class EasyPayHttpLoggingHandler : DelegatingHandler
{
    // Authenticate sends AcctCode/Token and returns SessKey; never write those to the log.
    private static readonly Regex SecretFields = new(@"(""(?:AcctCode|Token|SessKey)""\s*:\s*"")[^""]*("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // ACH sends full bank numbers: keep only the last four digits ("AccountNumber":"****0256").
    private static readonly Regex BankNumbers = new(@"(""(?:AccountNumber|RoutingNumber)""\s*:\s*"")[^""]*?(\d{0,4})("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<EasyPayHttpLoggingHandler> _logger;

    public EasyPayHttpLoggingHandler(ILogger<EasyPayHttpLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var watch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("[EasyPayLog] {Method} {Url} FAILED after {ElapsedMs}ms: {Error} Request={Request}",
                request.Method.Method, url, watch.ElapsedMilliseconds, ex.Message, Mask(requestBody));
            throw;
        }

        // Read the body into memory so the caller can still read it after we do.
        string? responseBody = null;
        if (response.Content != null)
        {
            await response.Content.LoadIntoBufferAsync();
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        _logger.LogError("[EasyPayLog] {Method} {Url} {Status} {ElapsedMs}ms Request={Request} Response={Response}",
            request.Method.Method, url, (int)response.StatusCode, watch.ElapsedMilliseconds, Mask(requestBody), Mask(responseBody));
        return response;
    }

    private static string? Mask(string? body)
    {
        return string.IsNullOrEmpty(body) ? body : BankNumbers.Replace(SecretFields.Replace(body, "$1***$2"), "$1****$2$3");
    }
}
